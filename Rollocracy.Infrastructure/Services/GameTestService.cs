using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Rollocracy.Domain.Characters;
using Rollocracy.Domain.GameRules;
using Rollocracy.Domain.GameTests;
using Rollocracy.Domain.Interfaces;
using Rollocracy.Infrastructure.Persistence;
using System.Security.Cryptography;
using System.Text.Json;

namespace Rollocracy.Infrastructure.Services
{
    public class GameTestService : IGameTestService
    {
        private readonly IDbContextFactory<RollocracyDbContext> _contextFactory;
        private readonly IStringLocalizer _localizer;
        private readonly IPresenceTracker _presenceTracker;
        private readonly ISessionNotifier _sessionNotifier;
        private readonly GameTestAutoRollScheduler _scheduler;
        private readonly ICharacterEffectService _characterEffectService;

        public GameTestService(
            IDbContextFactory<RollocracyDbContext> contextFactory,
            IStringLocalizerFactory localizerFactory,
            IPresenceTracker presenceTracker,
            ISessionNotifier sessionNotifier,
            GameTestAutoRollScheduler scheduler,
            ICharacterEffectService characterEffectService)
        {
            _contextFactory = contextFactory;
            _localizer = localizerFactory.Create("Rollocracy.Localization.SharedTexts", "Rollocracy");
            _presenceTracker = presenceTracker;
            _sessionNotifier = sessionNotifier;
            _scheduler = scheduler;
            _characterEffectService = characterEffectService;
        }

        public async Task<GameMasterActiveGameTestDto> CreateGameTestAsync(
            Guid sessionId,
            Guid gameMasterUserAccountId,
            GameTestCreateRequestDto request)
        {
            ValidateCreateRequest(request);

            await using var context = await _contextFactory.CreateDbContextAsync();

            var session = await context.Sessions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.GameMasterUserAccountId == gameMasterUserAccountId);

            if (session == null)
                throw new Exception(_localizer["Backend_SessionNotFound"]);

            if (!session.GameSystemId.HasValue)
                throw new Exception(_localizer["Backend_SessionHasNoGameSystem"]);

            var activeTest = await context.GameTests
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.SessionId == sessionId && !t.IsClosed);

            if (activeTest != null)
                throw new Exception(_localizer["Backend_TestAlreadyActive"]);

            var gameSystem = await context.GameSystems
                .AsNoTracking()
                .FirstOrDefaultAsync(gs => gs.Id == session.GameSystemId.Value);

            if (gameSystem == null)
                throw new Exception(_localizer["Backend_GameSystemNotFound"]);

            var targetName = await ResolveTestTargetNameAsync(
                context,
                session.GameSystemId.Value,
                request.TargetKind,
                request.TargetDefinitionId);

            if (gameSystem.TestResolutionMode == TestResolutionMode.SuccessThreshold && !request.SuccessThreshold.HasValue)
                throw new Exception(_localizer["Backend_SuccessThresholdRequired"]);

            var diceCount = request.UseSystemDefaultDice ? gameSystem.DefaultTestDiceCount : request.DiceCount;
            var diceSides = request.UseSystemDefaultDice ? gameSystem.DefaultTestDiceSides : request.DiceSides;
            var criticalSuccessValue = request.UseSystemDefaultDice ? gameSystem.CriticalSuccessValue : null;
            var criticalFailureValue = request.UseSystemDefaultDice ? gameSystem.CriticalFailureValue : null;

            var candidateRows = await context.Characters
                .AsNoTracking()
                .Join(
                    context.PlayerSessions.AsNoTracking(),
                    character => character.PlayerSessionId,
                    playerSession => playerSession.Id,
                    (character, playerSession) => new TestCandidateRow
                    {
                        Character = character,
                        PlayerSession = playerSession
                    })
                .Where(x =>
                    x.PlayerSession.SessionId == sessionId &&
                    x.Character.IsAlive &&
                    !x.PlayerSession.IsGameMaster)
                .OrderBy(x => x.Character.Name)
                .ToListAsync();

            if (request.TargetScope == TestTargetScope.OnlineLivingCharacters)
            {
                candidateRows = candidateRows
                    .Where(x => _presenceTracker.IsPlayerOnline(x.PlayerSession.Id))
                    .ToList();
            }

            var characterTraitValues = await context.CharacterTraitValues
                .AsNoTracking()
                .ToListAsync();

            candidateRows = ApplyTraitFilters(
                candidateRows,
                characterTraitValues,
                request.TraitFilters,
                request.TraitFilterMode);

            if (candidateRows.Count == 0)
                throw new Exception(_localizer["Backend_NoEligibleCharactersForTest"]);

            var test = new GameTest
            {
                Id = Guid.NewGuid(),
                SessionId = sessionId,
                TargetKind = request.TargetKind,
                TargetDefinitionId = request.TargetDefinitionId,
                TargetNameSnapshot = targetName,
                ResolutionModeSnapshot = gameSystem.TestResolutionMode,
                UseSystemDefaultDice = request.UseSystemDefaultDice,
                DiceCount = diceCount,
                DiceSides = diceSides,
                CriticalSuccessValueSnapshot = criticalSuccessValue,
                CriticalFailureValueSnapshot = criticalFailureValue,
                SuccessThreshold = request.SuccessThreshold,
                ModifierMode = request.ModifierMode,
                DifficultyValue = request.DifficultyValue,
                TargetScope = request.TargetScope,
                TraitFilterMode = request.TraitFilterMode,
                IsClosed = false,
                CreatedAtUtc = DateTime.UtcNow,
                AutoRollAtUtc = DateTime.UtcNow.AddSeconds(20)
            };

            context.GameTests.Add(test);

            foreach (var filterGroup in request.TraitFilters)
            {
                foreach (var optionId in filterGroup.SelectedOptionIds.Distinct())
                {
                    context.GameTestTraitFilters.Add(new GameTestTraitFilter
                    {
                        Id = Guid.NewGuid(),
                        GameTestId = test.Id,
                        TraitDefinitionId = filterGroup.TraitDefinitionId,
                        TraitOptionId = optionId
                    });
                }
            }

            foreach (var consequence in request.Consequences.Where(IsMeaningfulConsequence))
            {
                if (consequence.ValueMode == ModifierValueMode.Metric)
                {
                    var sourceMetricExists = consequence.SourceMetricId.HasValue && await context.MetricDefinitions
                        .AsNoTracking()
                        .AnyAsync(m => m.Id == consequence.SourceMetricId.Value && m.GameSystemId == session.GameSystemId.Value);

                    if (!sourceMetricExists)
                        throw new Exception(_localizer["Backend_InvalidTestConsequenceSourceMetric"]);
                }

                var resolvedTargetName = await ResolveConsequenceTargetNameAsync(context, consequence, session.GameSystemId.Value);

                context.GameTestConsequences.Add(new GameTestConsequence
                {
                    Id = Guid.NewGuid(),
                    GameTestId = test.Id,
                    ApplyOn = consequence.ApplyOn,
                    OperationType = consequence.OperationType,
                    TargetKind = consequence.TargetKind,
                    TargetDefinitionId = consequence.TargetDefinitionId,
                    TargetNameSnapshot = resolvedTargetName,
                    ModifierMode = consequence.ModifierMode,
                    Value = consequence.Value,
                    ValueMode = consequence.ValueMode,
                    SourceMetricId = consequence.ValueMode == ModifierValueMode.Metric
                        ? consequence.SourceMetricId
                        : null
                });
            }

            foreach (var row in candidateRows)
            {
                var testedValue = await ResolveTestTargetValueAsync(
                    context,
                    session.GameSystemId.Value,
                    row.Character.Id,
                    request.TargetKind,
                    request.TargetDefinitionId);

                var effectiveAttributeValue = ApplyModifier(
                    testedValue,
                    request.ModifierMode,
                    request.DifficultyValue);

                context.PlayerTestRolls.Add(new PlayerTestRoll
                {
                    Id = Guid.NewGuid(),
                    GameTestId = test.Id,
                    CharacterId = row.Character.Id,
                    PlayerSessionId = row.PlayerSession.Id,
                    CharacterNameSnapshot = row.Character.Name,
                    PlayerNameSnapshot = row.PlayerSession.PlayerName,
                    AttributeValueSnapshot = testedValue,
                    EffectiveAttributeValue = effectiveAttributeValue,
                    DiceResultsJson = "[]",
                    DiceTotal = 0,
                    FinalValue = 0,
                    IsSuccess = false,
                    Outcome = GameTestOutcome.Failure,
                    HasRolled = false,
                    IsAutoRolled = false
                });
            }

            await context.SaveChangesAsync();

            _scheduler.ScheduleAutoRoll(test.Id, TimeSpan.FromSeconds(20));

            await _sessionNotifier.NotifyTestChangedAsync(sessionId);

            return await BuildGameMasterDtoAsync(context, test.Id);
        }

        public async Task<GameMasterActiveGameTestDto?> GetActiveGameTestForSessionAsync(Guid sessionId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var test = await context.GameTests
                .AsNoTracking()
                .Where(t => t.SessionId == sessionId)
                .OrderByDescending(t => t.CreatedAtUtc)
                .FirstOrDefaultAsync();

            if (test == null)
                return null;

            return await BuildGameMasterDtoAsync(context, test.Id);
        }

        public async Task<ActivePlayerGameTestDto?> GetActiveGameTestForPlayerAsync(Guid playerSessionId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var nowUtc = DateTime.UtcNow;
            var recentLimitUtc = nowUtc.AddSeconds(-120);

            var playerRoll = await context.PlayerTestRolls
                .AsNoTracking()
                .Join(
                    context.GameTests.AsNoTracking(),
                    roll => roll.GameTestId,
                    test => test.Id,
                    (roll, test) => new { roll, test })
                .Where(x =>
                    x.roll.PlayerSessionId == playerSessionId &&
                    (
                        !x.test.IsClosed ||
                        (x.test.ClosedAtUtc.HasValue && x.test.ClosedAtUtc.Value >= recentLimitUtc)
                    ))
                .OrderByDescending(x => x.test.CreatedAtUtc)
                .FirstOrDefaultAsync();

            if (playerRoll == null)
                return null;

            var dto = new ActivePlayerGameTestDto
            {
                GameTestId = playerRoll.test.Id,
                TargetKind = playerRoll.test.TargetKind,
                AttributeName = playerRoll.test.TargetNameSnapshot,
                ResolutionMode = playerRoll.test.ResolutionModeSnapshot,
                UseSystemDefaultDice = playerRoll.test.UseSystemDefaultDice,
                DiceCount = playerRoll.test.DiceCount,
                DiceSides = playerRoll.test.DiceSides,
                CriticalSuccessValue = playerRoll.test.CriticalSuccessValueSnapshot,
                CriticalFailureValue = playerRoll.test.CriticalFailureValueSnapshot,
                SuccessThreshold = playerRoll.test.SuccessThreshold,
                ModifierMode = playerRoll.test.ModifierMode,
                DifficultyValue = playerRoll.test.DifficultyValue,
                AutoRollAtUtc = playerRoll.test.AutoRollAtUtc,
                AlreadyRolled = playerRoll.roll.HasRolled
            };

            if (playerRoll.roll.HasRolled)
            {
                dto.Result = new PlayerGameTestResultDto
                {
                    DiceResults = DeserializeDiceResults(playerRoll.roll.DiceResultsJson),
                    DiceTotal = playerRoll.roll.DiceTotal,
                    AttributeValue = playerRoll.roll.AttributeValueSnapshot,
                    EffectiveAttributeValue = playerRoll.roll.EffectiveAttributeValue,
                    FinalValue = playerRoll.roll.FinalValue,
                    IsSuccess = playerRoll.roll.IsSuccess,
                    Outcome = playerRoll.roll.Outcome,
                    IsAutoRolled = playerRoll.roll.IsAutoRolled
                };
            }

            return dto;
        }

        public async Task<PlayerGameTestResultDto> RollForPlayerAsync(Guid playerSessionId, Guid gameTestId, bool isAutoRoll)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var row = await context.PlayerTestRolls
                .FirstOrDefaultAsync(r => r.GameTestId == gameTestId && r.PlayerSessionId == playerSessionId);

            if (row == null)
                throw new Exception(_localizer["Backend_TestRollTargetNotFound"]);

            var test = await context.GameTests
                .FirstOrDefaultAsync(t => t.Id == gameTestId);

            if (test == null)
                throw new Exception(_localizer["Backend_TestNotFound"]);

            if (test.IsClosed)
            {
                return BuildPlayerResult(row);
            }

            if (row.HasRolled)
            {
                return BuildPlayerResult(row);
            }

            var diceResults = RollDice(test.DiceCount, test.DiceSides);
            var diceTotal = diceResults.Sum();

            int finalValue;
            bool isSuccess;

            if (test.ResolutionModeSnapshot == TestResolutionMode.SuccessThreshold)
            {
                finalValue = diceTotal + row.EffectiveAttributeValue;
                isSuccess = finalValue >= (test.SuccessThreshold ?? 0);
            }
            else
            {
                finalValue = diceTotal;
                isSuccess = finalValue <= row.EffectiveAttributeValue;
            }

            var outcome = ComputeOutcome(test, diceTotal, isSuccess);

            row.DiceResultsJson = JsonSerializer.Serialize(diceResults);
            row.DiceTotal = diceTotal;
            row.FinalValue = finalValue;
            row.IsSuccess = outcome == GameTestOutcome.Success || outcome == GameTestOutcome.CriticalSuccess;
            row.Outcome = outcome;
            row.HasRolled = true;
            row.IsAutoRolled = isAutoRoll;
            row.RolledAtUtc = DateTime.UtcNow;

            await context.SaveChangesAsync();

            await ApplyConsequencesAsync(context, test.Id, row.CharacterId, outcome);

            if (test.TargetScope == TestTargetScope.AllLivingCharacters && !isAutoRoll)
            {
                var pendingRows = await context.PlayerTestRolls
                    .AsNoTracking()
                    .Where(r => r.GameTestId == gameTestId && !r.HasRolled)
                    .ToListAsync();

                var hasPendingOnlinePlayers = pendingRows.Any(r => _presenceTracker.IsPlayerOnline(r.PlayerSessionId));

                if (!hasPendingOnlinePlayers && pendingRows.Count > 0)
                {
                    foreach (var pendingRow in pendingRows)
                    {
                        await RollForPlayerAsync(pendingRow.PlayerSessionId, gameTestId, true);
                    }
                }
            }

            await TryCloseTestIfCompleteAsync(context, test);

            await _sessionNotifier.NotifyCharacterStateChangedAsync(test.SessionId);
            await _sessionNotifier.NotifyTestChangedAsync(test.SessionId);

            return BuildPlayerResult(row);
        }

        public async Task AutoRollPendingAsync(Guid gameTestId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var test = await context.GameTests
                .FirstOrDefaultAsync(t => t.Id == gameTestId);

            if (test == null || test.IsClosed)
                return;

            var pendingPlayerSessions = await context.PlayerTestRolls
                .AsNoTracking()
                .Where(r => r.GameTestId == gameTestId && !r.HasRolled)
                .Select(r => r.PlayerSessionId)
                .ToListAsync();

            foreach (var playerSessionId in pendingPlayerSessions)
            {
                await RollForPlayerAsync(playerSessionId, gameTestId, true);
            }

            await _sessionNotifier.NotifyCharacterStateChangedAsync(test.SessionId);
            await _sessionNotifier.NotifyTestChangedAsync(test.SessionId);
        }

        public async Task RollbackLatestTestAsync(Guid sessionId, Guid gameMasterUserAccountId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var session = await context.Sessions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.GameMasterUserAccountId == gameMasterUserAccountId);

            if (session == null)
                throw new Exception(_localizer["Backend_SessionNotFound"]);

            var latestTest = await context.GameTests
                .Where(t => t.SessionId == sessionId)
                .OrderByDescending(t => t.CreatedAtUtc)
                .FirstOrDefaultAsync();

            if (latestTest == null)
                throw new Exception(_localizer["Backend_TestNotFound"]);

            var appliedEffects = await context.GameTestAppliedEffects
                .Where(e => e.GameTestId == latestTest.Id)
                .OrderByDescending(e => e.AppliedAtUtc)
                .ToListAsync();

            var testCharacterModifiers = await context.CharacterModifiers
                .Where(x => x.SourceType == CharacterEffectSourceType.Test && x.SourceId == latestTest.Id)
                .ToListAsync();

            // Liste des personnages qui seraient ressuscités par le rollback
            var charactersThatWouldBeResurrected = appliedEffects
                .Where(e => e.PreviousIsAlive && !e.NewIsAlive)
                .Select(e => e.CharacterId)
                .Distinct()
                .ToList();

            // Parmi eux, on exclut ceux dont le joueur a déjà un autre personnage vivant
            var charactersToKeepDead = new HashSet<Guid>();

            foreach (var characterId in charactersThatWouldBeResurrected)
            {
                var character = await context.Characters
                    .FirstOrDefaultAsync(c => c.Id == characterId);

                if (character == null)
                    continue;

                var otherAliveCharacterExists = await context.Characters
                    .AnyAsync(c =>
                        c.PlayerSessionId == character.PlayerSessionId &&
                        c.Id != character.Id &&
                        c.IsAlive);

                if (otherAliveCharacterExists)
                {
                    charactersToKeepDead.Add(characterId);
                }
            }

            var modifiersToRemove = testCharacterModifiers
                .Where(x => !charactersToKeepDead.Contains(x.CharacterId))
                .ToList();

            if (modifiersToRemove.Count > 0)
            {
                context.CharacterModifiers.RemoveRange(modifiersToRemove);
            }

            foreach (var effect in appliedEffects)
            {
                var character = await context.Characters
                    .FirstOrDefaultAsync(c => c.Id == effect.CharacterId);

                if (character == null)
                    continue;

                // Si ce personnage doit rester mort, on n'annule aucun effet le concernant.
                // On laisse donc son état actuel intact.
                if (charactersToKeepDead.Contains(effect.CharacterId))
                {
                    continue;
                }

                if (effect.TargetKind == TestConsequenceTargetKind.Talent)
                {
                    var existingTalent = await context.CharacterTalents
                        .FirstOrDefaultAsync(x =>
                            x.CharacterId == effect.CharacterId &&
                            x.TalentDefinitionId == effect.TargetDefinitionId);

                    if (effect.PreviousHasTargetLink)
                    {
                        if (existingTalent == null)
                        {
                            context.CharacterTalents.Add(new CharacterTalent
                            {
                                Id = Guid.NewGuid(),
                                CharacterId = effect.CharacterId,
                                TalentDefinitionId = effect.TargetDefinitionId
                            });
                        }
                    }
                    else
                    {
                        if (existingTalent != null)
                        {
                            context.CharacterTalents.Remove(existingTalent);
                        }
                    }

                    character.IsAlive = effect.PreviousIsAlive;
                    character.DiedAtUtc = effect.PreviousDiedAtUtc;
                    continue;
                }

                if (effect.TargetKind == TestConsequenceTargetKind.Item)
                {
                    var existingItem = await context.CharacterItems
                        .FirstOrDefaultAsync(x =>
                            x.CharacterId == effect.CharacterId &&
                            x.ItemDefinitionId == effect.TargetDefinitionId);

                    if (effect.PreviousHasTargetLink)
                    {
                        if (existingItem == null)
                        {
                            context.CharacterItems.Add(new CharacterItem
                            {
                                Id = Guid.NewGuid(),
                                CharacterId = effect.CharacterId,
                                ItemDefinitionId = effect.TargetDefinitionId
                            });
                        }
                    }
                    else
                    {
                        if (existingItem != null)
                        {
                            context.CharacterItems.Remove(existingItem);
                        }
                    }

                    character.IsAlive = effect.PreviousIsAlive;
                    character.DiedAtUtc = effect.PreviousDiedAtUtc;
                    continue;
                }

                if (effect.TargetKind == TestConsequenceTargetKind.Gauge)
                {
                    var gaugeValue = await context.CharacterGaugeValues
                        .FirstOrDefaultAsync(v =>
                            v.CharacterId == effect.CharacterId &&
                            v.GaugeDefinitionId == effect.TargetDefinitionId);

                    if (gaugeValue != null)
                    {
                        gaugeValue.Value = effect.PreviousValue;
                    }
                }
                else
                {
                    var attributeValue = await context.CharacterAttributeValues
                        .FirstOrDefaultAsync(v =>
                            v.CharacterId == effect.CharacterId &&
                            v.AttributeDefinitionId == effect.TargetDefinitionId);

                    if (attributeValue != null)
                    {
                        attributeValue.Value = effect.PreviousValue;
                    }
                }

                character.IsAlive = effect.PreviousIsAlive;
                character.DiedAtUtc = effect.PreviousDiedAtUtc;
            }

            var rollRows = await context.PlayerTestRolls
                .Where(r => r.GameTestId == latestTest.Id)
                .ToListAsync();

            context.PlayerTestRolls.RemoveRange(rollRows);

            var traitFilters = await context.GameTestTraitFilters
                .Where(f => f.GameTestId == latestTest.Id)
                .ToListAsync();

            context.GameTestTraitFilters.RemoveRange(traitFilters);

            var consequences = await context.GameTestConsequences
                .Where(c => c.GameTestId == latestTest.Id)
                .ToListAsync();

            context.GameTestConsequences.RemoveRange(consequences);

            context.GameTestAppliedEffects.RemoveRange(appliedEffects);

            context.GameTests.Remove(latestTest);

            await context.SaveChangesAsync();

            await _sessionNotifier.NotifyCharacterStateChangedAsync(sessionId);
            await _sessionNotifier.NotifyTestChangedAsync(sessionId);
        }

        private async Task ApplyConsequencesAsync(
    RollocracyDbContext context,
    Guid gameTestId,
    Guid characterId,
    GameTestOutcome outcome)
        {
            var applyOn = outcome switch
            {
                GameTestOutcome.CriticalSuccess => TestConsequenceApplyOn.OnCriticalSuccess,
                GameTestOutcome.CriticalFailure => TestConsequenceApplyOn.OnCriticalFailure,
                GameTestOutcome.Success => TestConsequenceApplyOn.OnSuccess,
                _ => TestConsequenceApplyOn.OnFailure
            };

            var consequences = await context.GameTestConsequences
                .Where(c => c.GameTestId == gameTestId && c.ApplyOn == applyOn)
                .ToListAsync();

            if (consequences.Count == 0 && outcome == GameTestOutcome.CriticalSuccess)
            {
                consequences = await context.GameTestConsequences
                    .Where(c => c.GameTestId == gameTestId && c.ApplyOn == TestConsequenceApplyOn.OnSuccess)
                    .ToListAsync();
            }
            else if (consequences.Count == 0 && outcome == GameTestOutcome.CriticalFailure)
            {
                consequences = await context.GameTestConsequences
                    .Where(c => c.GameTestId == gameTestId && c.ApplyOn == TestConsequenceApplyOn.OnFailure)
                    .ToListAsync();
            }

            if (consequences.Count == 0)
                return;

            var test = await context.GameTests
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == gameTestId);

            if (test == null)
                return;

            var character = await context.Characters.FirstOrDefaultAsync(c => c.Id == characterId);
            if (character == null)
                return;

            // 1) Ancien mécanisme conservé pour Attribute / Gauge,
            // afin de garder le rollback actuel entièrement fonctionnel.
            var legacyConsequences = consequences
                .Where(c =>
                    c.OperationType == TestConsequenceOperationType.AddValue &&
                    c.ValueMode != ModifierValueMode.Metric &&
                    (c.TargetKind == TestConsequenceTargetKind.Attribute ||
                     c.TargetKind == TestConsequenceTargetKind.Gauge))
                .ToList();


            foreach (var consequence in legacyConsequences)
            {
                var signedValue = consequence.ModifierMode == TestModifierMode.Bonus
                    ? consequence.Value
                    : -consequence.Value;

                if (consequence.TargetKind == TestConsequenceTargetKind.Gauge)
                {
                    var gaugeValue = await context.CharacterGaugeValues
                        .FirstOrDefaultAsync(v =>
                            v.CharacterId == characterId &&
                            v.GaugeDefinitionId == consequence.TargetDefinitionId);

                    if (gaugeValue != null)
                    {
                        var previousCharacterAlive = character.IsAlive;
                        var previousCharacterDiedAt = character.DiedAtUtc;
                        var previousValue = gaugeValue.Value;

                        gaugeValue.Value += signedValue;

                        await UpdateCharacterAliveStateAsync(context, characterId);

                        context.GameTestAppliedEffects.Add(new GameTestAppliedEffect
                        {
                            Id = Guid.NewGuid(),
                            GameTestId = gameTestId,
                            CharacterId = characterId,
                            TargetKind = TestConsequenceTargetKind.Gauge,
                            TargetDefinitionId = consequence.TargetDefinitionId,
                            OperationType = TestConsequenceOperationType.AddValue,
                            PreviousValue = previousValue,
                            NewValue = gaugeValue.Value,
                            PreviousHasTargetLink = false,
                            NewHasTargetLink = false,
                            PreviousIsAlive = previousCharacterAlive,
                            NewIsAlive = character.IsAlive,
                            PreviousDiedAtUtc = previousCharacterDiedAt,
                            NewDiedAtUtc = character.DiedAtUtc,
                            AppliedAtUtc = DateTime.UtcNow
                        });
                    }
                }
                else
                {
                    var attributeValue = await context.CharacterAttributeValues
                        .FirstOrDefaultAsync(v =>
                            v.CharacterId == characterId &&
                            v.AttributeDefinitionId == consequence.TargetDefinitionId);

                    if (attributeValue != null)
                    {
                        var previousCharacterAlive = character.IsAlive;
                        var previousCharacterDiedAt = character.DiedAtUtc;
                        var previousValue = attributeValue.Value;

                        attributeValue.Value += signedValue;

                        context.GameTestAppliedEffects.Add(new GameTestAppliedEffect
                        {
                            Id = Guid.NewGuid(),
                            GameTestId = gameTestId,
                            CharacterId = characterId,
                            TargetKind = TestConsequenceTargetKind.Attribute,
                            TargetDefinitionId = consequence.TargetDefinitionId,
                            OperationType = TestConsequenceOperationType.AddValue,
                            PreviousValue = previousValue,
                            NewValue = attributeValue.Value,
                            PreviousHasTargetLink = false,
                            NewHasTargetLink = false,
                            PreviousIsAlive = previousCharacterAlive,
                            NewIsAlive = character.IsAlive,
                            PreviousDiedAtUtc = previousCharacterDiedAt,
                            NewDiedAtUtc = character.DiedAtUtc,
                            AppliedAtUtc = DateTime.UtcNow
                        });
                    }
                }
            }

            await context.SaveChangesAsync();

            // 2) Nouveau moteur commun pour DerivedStat / Metric / Talent / Item.
            var commonEngineConsequences = consequences
                .Where(c => !(
                    c.OperationType == TestConsequenceOperationType.AddValue &&
                    c.ValueMode != ModifierValueMode.Metric &&
                    (c.TargetKind == TestConsequenceTargetKind.Attribute ||
                     c.TargetKind == TestConsequenceTargetKind.Gauge)))
                .ToList();

            if (commonEngineConsequences.Count == 0)
                return;

            foreach (var consequence in commonEngineConsequences)
            {
                var effectDto = ToCharacterEffectDefinitionDto(consequence);

                var previousCharacterAlive = character.IsAlive;
                var previousCharacterDiedAt = character.DiedAtUtc;

                var previousHasTargetLink = consequence.TargetKind switch
                {
                    TestConsequenceTargetKind.Talent => await context.CharacterTalents.AnyAsync(x =>
                        x.CharacterId == characterId &&
                        x.TalentDefinitionId == consequence.TargetDefinitionId),

                    TestConsequenceTargetKind.Item => await context.CharacterItems.AnyAsync(x =>
                        x.CharacterId == characterId &&
                        x.ItemDefinitionId == consequence.TargetDefinitionId),

                    _ => false
                };

                await _characterEffectService.ApplyEffectsAsync(
                    test.SessionId,
                    new List<Guid> { characterId },
                    new List<CharacterEffectDefinitionDto> { effectDto },
                    CharacterEffectSourceType.Test,
                    gameTestId,
                    $"GameTest:{gameTestId}");

                await context.Entry(character).ReloadAsync();

                var newHasTargetLink = consequence.TargetKind switch
                {
                    TestConsequenceTargetKind.Talent => await context.CharacterTalents.AnyAsync(x =>
                        x.CharacterId == characterId &&
                        x.TalentDefinitionId == consequence.TargetDefinitionId),

                    TestConsequenceTargetKind.Item => await context.CharacterItems.AnyAsync(x =>
                        x.CharacterId == characterId &&
                        x.ItemDefinitionId == consequence.TargetDefinitionId),

                    _ => false
                };

                if (consequence.TargetKind == TestConsequenceTargetKind.Talent ||
                    consequence.TargetKind == TestConsequenceTargetKind.Item)
                {
                    context.GameTestAppliedEffects.Add(new GameTestAppliedEffect
                    {
                        Id = Guid.NewGuid(),
                        GameTestId = gameTestId,
                        CharacterId = characterId,
                        TargetKind = consequence.TargetKind,
                        TargetDefinitionId = consequence.TargetDefinitionId,
                        OperationType = consequence.OperationType,
                        PreviousValue = 0,
                        NewValue = 0,
                        PreviousHasTargetLink = previousHasTargetLink,
                        NewHasTargetLink = newHasTargetLink,
                        PreviousIsAlive = previousCharacterAlive,
                        NewIsAlive = character.IsAlive,
                        PreviousDiedAtUtc = previousCharacterDiedAt,
                        NewDiedAtUtc = character.DiedAtUtc,
                        AppliedAtUtc = DateTime.UtcNow
                    });
                }
            }
        }

        private async Task UpdateCharacterAliveStateAsync(RollocracyDbContext context, Guid characterId)
        {
            var character = await context.Characters.FirstOrDefaultAsync(c => c.Id == characterId);

            if (character == null)
                return;

            var gaugeRows = await context.CharacterGaugeValues
                .Join(
                    context.GaugeDefinitions,
                    value => value.GaugeDefinitionId,
                    definition => definition.Id,
                    (value, definition) => new { value, definition })
                .Where(x => x.value.CharacterId == characterId && x.definition.IsHealthGauge)
                .ToListAsync();

            var wasAlive = character.IsAlive;
            var isAliveNow = !gaugeRows.Any(x => x.value.Value <= 0);

            character.IsAlive = isAliveNow;

            if (wasAlive && !isAliveNow)
            {
                character.DiedAtUtc = DateTime.UtcNow;
            }
            else if (isAliveNow)
            {
                character.DiedAtUtc = null;
            }
        }

        private async Task TryCloseTestIfCompleteAsync(RollocracyDbContext context, GameTest test)
        {
            var remaining = await context.PlayerTestRolls
                .CountAsync(r => r.GameTestId == test.Id && !r.HasRolled);

            if (remaining == 0)
            {
                test.IsClosed = true;
                test.ClosedAtUtc = DateTime.UtcNow;
                await context.SaveChangesAsync();
            }
        }

        private async Task<GameMasterActiveGameTestDto> BuildGameMasterDtoAsync(RollocracyDbContext context, Guid gameTestId)
        {
            var test = await context.GameTests
                .AsNoTracking()
                .FirstAsync(t => t.Id == gameTestId);

            var rows = await context.PlayerTestRolls
                .AsNoTracking()
                .Where(r => r.GameTestId == gameTestId)
                .OrderBy(r => r.CharacterNameSnapshot)
                .ToListAsync();

            var rolledRows = rows.Where(r => r.HasRolled).ToList();

            var allIndividualDice = rolledRows
                .SelectMany(r => DeserializeDiceResults(r.DiceResultsJson))
                .ToList();

            return new GameMasterActiveGameTestDto
            {
                GameTestId = test.Id,
                TargetKind = test.TargetKind,
                AttributeName = test.TargetNameSnapshot,
                ResolutionMode = test.ResolutionModeSnapshot,
                UseSystemDefaultDice = test.UseSystemDefaultDice,
                DiceCount = test.DiceCount,
                DiceSides = test.DiceSides,
                CriticalSuccessValue = test.CriticalSuccessValueSnapshot,
                CriticalFailureValue = test.CriticalFailureValueSnapshot,
                SuccessThreshold = test.SuccessThreshold,
                ModifierMode = test.ModifierMode,
                DifficultyValue = test.DifficultyValue,
                TargetScope = test.TargetScope,
                TraitFilterMode = test.TraitFilterMode,
                IsClosed = test.IsClosed,
                AutoRollAtUtc = test.AutoRollAtUtc,
                TargetCount = rows.Count,
                RolledCount = rolledRows.Count,
                SuccessCount = rolledRows.Count(r => r.Outcome == GameTestOutcome.Success || r.Outcome == GameTestOutcome.CriticalSuccess),
                FailureCount = rolledRows.Count(r => r.Outcome == GameTestOutcome.Failure || r.Outcome == GameTestOutcome.CriticalFailure),
                CriticalSuccessCount = rolledRows.Count(r => r.Outcome == GameTestOutcome.CriticalSuccess),
                CriticalFailureCount = rolledRows.Count(r => r.Outcome == GameTestOutcome.CriticalFailure),
                SuccessRatePercent = rolledRows.Count == 0 ? 0 : (double)rolledRows.Count(r => r.Outcome == GameTestOutcome.Success || r.Outcome == GameTestOutcome.CriticalSuccess) * 100.0 / rolledRows.Count,
                BestDiceTotal = rolledRows.Count == 0 ? null : rolledRows.Max(r => r.DiceTotal),
                WorstDiceTotal = rolledRows.Count == 0 ? null : rolledRows.Min(r => r.DiceTotal),
                AverageDiceTotal = allIndividualDice.Count == 0 ? null : allIndividualDice.Average(),
                Results = rows.Select(r => new GameMasterGameTestResultLineDto
                {
                    CharacterId = r.CharacterId,
                    CharacterName = r.CharacterNameSnapshot,
                    PlayerName = r.PlayerNameSnapshot,
                    HasRolled = r.HasRolled,
                    IsSuccess = r.IsSuccess,
                    Outcome = r.Outcome,
                    IsAutoRolled = r.IsAutoRolled,
                    DiceResults = DeserializeDiceResults(r.DiceResultsJson),
                    DiceTotal = r.DiceTotal,
                    AttributeValue = r.AttributeValueSnapshot,
                    EffectiveAttributeValue = r.EffectiveAttributeValue,
                    FinalValue = r.FinalValue
                }).ToList()
            };
        }

        private List<TestCandidateRow> ApplyTraitFilters(
            List<TestCandidateRow> candidateRows,
            List<Domain.GameRules.CharacterTraitValue> characterTraitValues,
            List<GameTestTraitFilterGroupDto> filterGroups,
            TestTraitFilterMode filterMode)
        {
            var validGroups = filterGroups
                .Where(g => g.SelectedOptionIds.Count > 0)
                .ToList();

            if (validGroups.Count == 0)
                return candidateRows;

            return candidateRows
                .Where(row =>
                {
                    var characterValues = characterTraitValues
                        .Where(v => v.CharacterId == row.Character.Id)
                        .ToList();

                    var groupMatches = validGroups.Select(group =>
                        characterValues.Any(v =>
                            v.TraitDefinitionId == group.TraitDefinitionId &&
                            group.SelectedOptionIds.Contains(v.TraitOptionId)))
                        .ToList();

                    return filterMode == TestTraitFilterMode.AndBetweenGroups
                        ? groupMatches.All(x => x)
                        : groupMatches.Any(x => x);
                })
                .ToList();
        }

        private static CharacterEffectDefinitionDto ToCharacterEffectDefinitionDto(GameTestConsequence consequence)
        {
            var operationType = consequence.OperationType;
            var targetType = consequence.TargetKind switch
            {
                TestConsequenceTargetKind.Attribute => CharacterEffectTargetType.BaseAttribute,
                TestConsequenceTargetKind.Gauge => CharacterEffectTargetType.Gauge,
                TestConsequenceTargetKind.DerivedStat => CharacterEffectTargetType.DerivedStat,
                TestConsequenceTargetKind.Metric => CharacterEffectTargetType.Metric,
                TestConsequenceTargetKind.Talent => CharacterEffectTargetType.Talent,
                TestConsequenceTargetKind.Item => CharacterEffectTargetType.Item,
                _ => CharacterEffectTargetType.BaseAttribute
            };

            var signedValue = consequence.ModifierMode == TestModifierMode.Bonus
                ? consequence.Value
                : -consequence.Value;

            var resolvedValue = consequence.ValueMode == ModifierValueMode.Metric
                ? (consequence.ModifierMode == TestModifierMode.Bonus ? 1 : -1)
                : signedValue;

            return new CharacterEffectDefinitionDto
            {
                OperationType = operationType switch
                {
                    TestConsequenceOperationType.AddValue => CharacterEffectOperationType.AddValue,
                    TestConsequenceOperationType.GrantTalent => CharacterEffectOperationType.GrantTalent,
                    TestConsequenceOperationType.RevokeTalent => CharacterEffectOperationType.RevokeTalent,
                    TestConsequenceOperationType.GrantItem => CharacterEffectOperationType.GrantItem,
                    TestConsequenceOperationType.RevokeItem => CharacterEffectOperationType.RevokeItem,
                    _ => CharacterEffectOperationType.AddValue
                },
                TargetType = targetType,
                TargetId = consequence.TargetDefinitionId,
                TargetName = consequence.TargetNameSnapshot,
                Value = resolvedValue,
                ValueMode = consequence.ValueMode,
                SourceMetricId = consequence.ValueMode == ModifierValueMode.Metric
                    ? consequence.SourceMetricId
                    : null
            };
        }

        private GameTestOutcome ComputeOutcome(GameTest test, int diceTotal, bool isSuccess)
        {
            if (test.UseSystemDefaultDice)
            {
                var isCriticalSuccess = test.CriticalSuccessValueSnapshot.HasValue &&
                    (test.ResolutionModeSnapshot == TestResolutionMode.SuccessThreshold
                        ? diceTotal >= test.CriticalSuccessValueSnapshot.Value
                        : diceTotal <= test.CriticalSuccessValueSnapshot.Value);

                if (isCriticalSuccess)
                    return GameTestOutcome.CriticalSuccess;

                var isCriticalFailure = test.CriticalFailureValueSnapshot.HasValue &&
                    (test.ResolutionModeSnapshot == TestResolutionMode.SuccessThreshold
                        ? diceTotal <= test.CriticalFailureValueSnapshot.Value
                        : diceTotal >= test.CriticalFailureValueSnapshot.Value);

                if (isCriticalFailure)
                    return GameTestOutcome.CriticalFailure;
            }

            return isSuccess ? GameTestOutcome.Success : GameTestOutcome.Failure;
        }

        private async Task<string> ResolveTestTargetNameAsync(
            RollocracyDbContext context,
            Guid gameSystemId,
            GameTestTargetKind targetKind,
            Guid targetDefinitionId)
        {
            switch (targetKind)
            {
                case GameTestTargetKind.BaseAttribute:
                    var attribute = await context.AttributeDefinitions
                        .AsNoTracking()
                        .FirstOrDefaultAsync(a => a.Id == targetDefinitionId && a.GameSystemId == gameSystemId);

                    if (attribute == null)
                        throw new Exception(_localizer["Backend_AttributeDefinitionNotFound"]);

                    return attribute.Name;

                case GameTestTargetKind.DerivedStat:
                    var derivedStat = await context.DerivedStatDefinitions
                        .AsNoTracking()
                        .FirstOrDefaultAsync(d => d.Id == targetDefinitionId && d.GameSystemId == gameSystemId);

                    if (derivedStat == null)
                        throw new Exception(_localizer["Backend_InvalidTestTargetDefinition"]);

                    return derivedStat.Name;

                default:
                    throw new Exception(_localizer["Backend_InvalidTestTargetDefinition"]);
            }
        }

        private async Task<int> ResolveTestTargetValueAsync(
            RollocracyDbContext context,
            Guid gameSystemId,
            Guid characterId,
            GameTestTargetKind targetKind,
            Guid targetDefinitionId)
        {
            var attributeDefinitions = await context.AttributeDefinitions
                .AsNoTracking()
                .Where(a => a.GameSystemId == gameSystemId)
                .OrderBy(a => a.Name)
                .ToListAsync();

            var attributeValues = await context.CharacterAttributeValues
                .AsNoTracking()
                .Where(v => v.CharacterId == characterId)
                .ToListAsync();

            var gaugeDefinitions = await context.GaugeDefinitions
                .AsNoTracking()
                .Where(g => g.GameSystemId == gameSystemId)
                .ToListAsync();

            var rawGaugeValues = await context.CharacterGaugeValues
                .AsNoTracking()
                .Where(v => v.CharacterId == characterId)
                .ToListAsync();

            var effectiveGaugeValues = gaugeDefinitions.ToDictionary(
                definition => definition.Id,
                definition =>
                {
                    var baseValue = rawGaugeValues.FirstOrDefault(v => v.GaugeDefinitionId == definition.Id)?.Value
                        ?? definition.DefaultValue;

                    return Math.Clamp(baseValue, definition.MinValue, definition.MaxValue);
                });

            var traitValues = await context.CharacterTraitValues
                .AsNoTracking()
                .Where(v => v.CharacterId == characterId)
                .ToListAsync();

            var traitOptionIds = traitValues.Select(v => v.TraitOptionId).Distinct().ToList();

            var choiceOptionModifiers = await context.ChoiceOptionModifierDefinitions
                .AsNoTracking()
                .Where(m => traitOptionIds.Contains(m.TraitOptionId))
                .ToListAsync();

            var characterTalentIds = await context.CharacterTalents
                .AsNoTracking()
                .Where(x => x.CharacterId == characterId)
                .Select(x => x.TalentDefinitionId)
                .ToListAsync();

            var characterItemIds = await context.CharacterItems
                .AsNoTracking()
                .Where(x => x.CharacterId == characterId)
                .Select(x => x.ItemDefinitionId)
                .ToListAsync();

            var talentModifiers = await context.TalentModifierDefinitions
                .AsNoTracking()
                .Where(m => characterTalentIds.Contains(m.TalentDefinitionId))
                .ToListAsync();

            var itemModifiers = await context.ItemModifierDefinitions
                .AsNoTracking()
                .Where(m => characterItemIds.Contains(m.ItemDefinitionId))
                .ToListAsync();

            var characterModifiers = await context.CharacterModifiers
                .AsNoTracking()
                .Where(m =>
                    m.CharacterId == characterId &&
                    (m.TargetType == CharacterEffectTargetType.BaseAttribute ||
                     m.TargetType == CharacterEffectTargetType.DerivedStat ||
                     m.TargetType == CharacterEffectTargetType.Metric))
                .ToListAsync();

            var rawModifiers = choiceOptionModifiers
                .Select(m => new RuntimeModifier
                {
                    TargetType = m.TargetType,
                    TargetId = m.TargetId,
                    AddValue = m.AddValue,
                    ValueMode = m.ValueMode,
                    SourceMetricId = m.SourceMetricId
                })
                .Concat(talentModifiers.Select(m => new RuntimeModifier
                {
                    TargetType = m.TargetType,
                    TargetId = m.TargetId,
                    AddValue = m.AddValue,
                    ValueMode = m.ValueMode,
                    SourceMetricId = m.SourceMetricId
                }))
                .Concat(itemModifiers.Select(m => new RuntimeModifier
                {
                    TargetType = m.TargetType,
                    TargetId = m.TargetId,
                    AddValue = m.AddValue,
                    ValueMode = m.ValueMode,
                    SourceMetricId = m.SourceMetricId
                }))
                .Concat(characterModifiers.Select(m => new RuntimeModifier
                {
                    TargetType = m.TargetType switch
                    {
                        CharacterEffectTargetType.BaseAttribute => ModifierTargetType.BaseAttribute,
                        CharacterEffectTargetType.DerivedStat => ModifierTargetType.DerivedStat,
                        CharacterEffectTargetType.Metric => ModifierTargetType.Metric,
                        _ => ModifierTargetType.BaseAttribute
                    },
                    TargetId = m.TargetId,
                    AddValue = m.AddValue,
                    ValueMode = ModifierValueMode.Fixed
                }))
                .ToList();

            Dictionary<Guid, int> effectiveAttributeValues = attributeDefinitions.ToDictionary(
                definition => definition.Id,
                definition =>
                {
                    var baseValue = attributeValues.FirstOrDefault(v => v.AttributeDefinitionId == definition.Id)?.Value
                        ?? definition.DefaultValue;

                    var modifier = rawModifiers
                        .Where(m => m.ValueMode != ModifierValueMode.Metric && m.TargetType == ModifierTargetType.BaseAttribute && m.TargetId == definition.Id)
                        .Sum(m => m.AddValue);

                    var effectiveValue = baseValue + modifier;
                    return Math.Clamp(effectiveValue, definition.MinValue, definition.MaxValue);
                });

            var derivedDefinitions = await context.DerivedStatDefinitions
                .AsNoTracking()
                .Where(d => d.GameSystemId == gameSystemId)
                .OrderBy(d => d.DisplayOrder).ThenBy(d => d.Name)
                .ToListAsync();

            var derivedDefinitionIds = derivedDefinitions.Select(d => d.Id).ToList();

            var derivedComponents = await context.DerivedStatComponents
                .AsNoTracking()
                .Where(c => derivedDefinitionIds.Contains(c.DerivedStatDefinitionId))
                .ToListAsync();

            var preliminaryDerivedValues = new Dictionary<Guid, int>();

            foreach (var definition in derivedDefinitions)
            {
                var value = ComputeWeightedValue(
                    derivedComponents.Where(c => c.DerivedStatDefinitionId == definition.Id)
                        .Select(c => new WeightedSourceValue
                        {
                            Weight = c.Weight,
                            Value = effectiveAttributeValues.TryGetValue(c.AttributeDefinitionId, out var sourceValue) ? sourceValue : 0
                        })
                        .ToList(),
                    0,
                    definition.MinValue,
                    definition.MaxValue,
                    definition.RoundMode);

                value += rawModifiers
                    .Where(m => m.ValueMode != ModifierValueMode.Metric && m.TargetType == ModifierTargetType.DerivedStat && m.TargetId == definition.Id)
                    .Sum(m => m.AddValue);

                preliminaryDerivedValues[definition.Id] = Math.Clamp(value, definition.MinValue, definition.MaxValue);
            }

            var metricDefinitions = await context.MetricDefinitions
                .AsNoTracking()
                .Where(m => m.GameSystemId == gameSystemId)
                .OrderBy(m => m.DisplayOrder).ThenBy(m => m.Name)
                .ToListAsync();

            var metricDefinitionIds = metricDefinitions.Select(m => m.Id).ToList();

            var metricComponents = await context.MetricComponents
                .AsNoTracking()
                .Where(c => metricDefinitionIds.Contains(c.MetricDefinitionId))
                .ToListAsync();

            var metricFormulaSteps = await context.MetricFormulaSteps
                .AsNoTracking()
                .Where(x => metricDefinitionIds.Contains(x.MetricDefinitionId))
                .ToListAsync();

            var resolvedModifiers = ResolveRuntimeModifiers(
                rawModifiers,
                metricDefinitions,
                metricComponents,
                metricFormulaSteps,
                effectiveAttributeValues,
                effectiveGaugeValues,
                preliminaryDerivedValues);

            effectiveAttributeValues = attributeDefinitions.ToDictionary(
                definition => definition.Id,
                definition =>
                {
                    var baseValue = attributeValues.FirstOrDefault(v => v.AttributeDefinitionId == definition.Id)?.Value
                        ?? definition.DefaultValue;

                    var modifier = resolvedModifiers
                        .Where(m => m.TargetType == ModifierTargetType.BaseAttribute && m.TargetId == definition.Id)
                        .Sum(m => m.AddValue);

                    var effectiveValue = baseValue + modifier;
                    return Math.Clamp(effectiveValue, definition.MinValue, definition.MaxValue);
                });

            var derivedStatValues = new Dictionary<Guid, int>();

            foreach (var definition in derivedDefinitions)
            {
                var value = ComputeWeightedValue(
                    derivedComponents.Where(c => c.DerivedStatDefinitionId == definition.Id)
                        .Select(c => new WeightedSourceValue
                        {
                            Weight = c.Weight,
                            Value = effectiveAttributeValues.TryGetValue(c.AttributeDefinitionId, out var sourceValue) ? sourceValue : 0
                        })
                        .ToList(),
                    0,
                    definition.MinValue,
                    definition.MaxValue,
                    definition.RoundMode);

                value += resolvedModifiers
                    .Where(m => m.TargetType == ModifierTargetType.DerivedStat && m.TargetId == definition.Id)
                    .Sum(m => m.AddValue);

                derivedStatValues[definition.Id] = Math.Clamp(value, definition.MinValue, definition.MaxValue);
            }

            return targetKind switch
            {
                GameTestTargetKind.BaseAttribute => effectiveAttributeValues.TryGetValue(targetDefinitionId, out var attributeValue) ? attributeValue : 0,
                GameTestTargetKind.DerivedStat => derivedStatValues.TryGetValue(targetDefinitionId, out var derivedValue) ? derivedValue : 0,
                _ => 0
            };
        }

        private static int ComputeWeightedValue(
            List<WeightedSourceValue> components,
            int baseValue,
            int minValue,
            int maxValue,
            ComputedValueRoundMode roundMode)
        {
            decimal rawValue = baseValue;

            foreach (var component in components)
            {
                rawValue += component.Value * (component.Weight / 100m);
            }

            rawValue = roundMode switch
            {
                ComputedValueRoundMode.Ceiling => Math.Ceiling(rawValue),
                ComputedValueRoundMode.Floor => Math.Floor(rawValue),
                ComputedValueRoundMode.Nearest => Math.Round(rawValue, 0, MidpointRounding.AwayFromZero),
                _ => rawValue
            };

            var asInt = (int)rawValue;
            return Math.Clamp(asInt, minValue, maxValue);
        }

        private static List<RuntimeModifier> ResolveRuntimeModifiers(
            List<RuntimeModifier> rawModifiers,
            List<MetricDefinition> metricDefinitions,
            List<MetricComponent> metricComponents,
            List<MetricFormulaStep> metricFormulaSteps,
            Dictionary<Guid, int> effectiveAttributeValues,
            Dictionary<Guid, int> effectiveGaugeValues,
            Dictionary<Guid, int> derivedStatValues)
        {
            var fixedModifiers = rawModifiers
                .Where(x => x.ValueMode != ModifierValueMode.Metric || !x.SourceMetricId.HasValue)
                .Select(x => new RuntimeModifier
                {
                    TargetType = x.TargetType,
                    TargetId = x.TargetId,
                    AddValue = x.AddValue,
                    ValueMode = ModifierValueMode.Fixed
                })
                .ToList();

            if (!rawModifiers.Any(x => x.ValueMode == ModifierValueMode.Metric && x.SourceMetricId.HasValue))
                return fixedModifiers;

            var metricValues = MetricFormulaEngine.ComputeAll(new MetricFormulaEngine.MetricComputationRequest
            {
                MetricDefinitions = metricDefinitions,
                FormulaSteps = metricFormulaSteps,
                LegacyComponents = metricComponents,
                BaseAttributeValues = effectiveAttributeValues,
                GaugeValues = effectiveGaugeValues,
                DerivedStatValues = derivedStatValues,
                Modifiers = fixedModifiers
                    .Select(x => new MetricFormulaEngine.ModifierValue
                    {
                        TargetType = x.TargetType,
                        TargetId = x.TargetId,
                        AddValue = x.AddValue
                    })
                    .ToList()
            });

            return rawModifiers.Select(x => new RuntimeModifier
            {
                TargetType = x.TargetType,
                TargetId = x.TargetId,
                AddValue = x.ValueMode == ModifierValueMode.Metric && x.SourceMetricId.HasValue && metricValues.TryGetValue(x.SourceMetricId.Value, out var metricValue)
                    ? metricValue
                    : x.AddValue,
                ValueMode = ModifierValueMode.Fixed
            }).ToList();
        }

        private sealed class RuntimeModifier
        {
            public ModifierTargetType TargetType { get; set; }
            public Guid TargetId { get; set; }
            public int AddValue { get; set; }
            public ModifierValueMode ValueMode { get; set; }
            public Guid? SourceMetricId { get; set; }
        }

        private sealed class WeightedSourceValue
        {
            public int Weight { get; set; }
            public int Value { get; set; }
        }

        private async Task<string> ResolveConsequenceTargetNameAsync(
    RollocracyDbContext context,
    GameTestConsequenceDraftDto consequence,
    Guid gameSystemId)
        {
            switch (consequence.TargetKind)
            {
                case TestConsequenceTargetKind.Gauge:
                    var gauge = await context.GaugeDefinitions
                        .AsNoTracking()
                        .FirstOrDefaultAsync(g => g.Id == consequence.TargetDefinitionId && g.GameSystemId == gameSystemId);

                    if (gauge == null)
                        throw new Exception(_localizer["Backend_InvalidConsequenceTarget"]);

                    return gauge.Name;

                case TestConsequenceTargetKind.Attribute:
                    var attribute = await context.AttributeDefinitions
                        .AsNoTracking()
                        .FirstOrDefaultAsync(a => a.Id == consequence.TargetDefinitionId && a.GameSystemId == gameSystemId);

                    if (attribute == null)
                        throw new Exception(_localizer["Backend_InvalidConsequenceTarget"]);

                    return attribute.Name;

                case TestConsequenceTargetKind.DerivedStat:
                    var derivedStat = await context.DerivedStatDefinitions
                        .AsNoTracking()
                        .FirstOrDefaultAsync(d => d.Id == consequence.TargetDefinitionId && d.GameSystemId == gameSystemId);

                    if (derivedStat == null)
                        throw new Exception(_localizer["Backend_InvalidConsequenceTarget"]);

                    return derivedStat.Name;

                case TestConsequenceTargetKind.Metric:
                    var metric = await context.MetricDefinitions
                        .AsNoTracking()
                        .FirstOrDefaultAsync(m => m.Id == consequence.TargetDefinitionId && m.GameSystemId == gameSystemId);

                    if (metric == null)
                        throw new Exception(_localizer["Backend_InvalidConsequenceTarget"]);

                    return metric.Name;

                case TestConsequenceTargetKind.Talent:
                    var talent = await context.TalentDefinitions
                        .AsNoTracking()
                        .FirstOrDefaultAsync(t => t.Id == consequence.TargetDefinitionId && t.GameSystemId == gameSystemId);

                    if (talent == null)
                        throw new Exception(_localizer["Backend_InvalidConsequenceTarget"]);

                    return talent.Name;

                case TestConsequenceTargetKind.Item:
                    var item = await context.ItemDefinitions
                        .AsNoTracking()
                        .FirstOrDefaultAsync(i => i.Id == consequence.TargetDefinitionId && i.GameSystemId == gameSystemId);

                    if (item == null)
                        throw new Exception(_localizer["Backend_InvalidConsequenceTarget"]);

                    return item.Name;

                default:
                    throw new Exception(_localizer["Backend_InvalidConsequenceTarget"]);
            }
        }

        private static bool IsMeaningfulConsequence(GameTestConsequenceDraftDto consequence)
        {
            return consequence.OperationType switch
            {
                TestConsequenceOperationType.AddValue => consequence.ValueMode == ModifierValueMode.Metric
                    ? consequence.SourceMetricId.HasValue
                    : consequence.Value != 0,
                TestConsequenceOperationType.GrantTalent => consequence.TargetDefinitionId != Guid.Empty,
                TestConsequenceOperationType.RevokeTalent => consequence.TargetDefinitionId != Guid.Empty,
                TestConsequenceOperationType.GrantItem => consequence.TargetDefinitionId != Guid.Empty,
                TestConsequenceOperationType.RevokeItem => consequence.TargetDefinitionId != Guid.Empty,
                _ => false
            };
        }

        private void ValidateCreateRequest(GameTestCreateRequestDto request)
        {
            if (request.TargetDefinitionId == Guid.Empty)
                throw new Exception(_localizer["Backend_AttributeDefinitionNotFound"]);

            if (!request.UseSystemDefaultDice)
            {
                if (request.DiceCount < 1 || request.DiceCount > 5)
                    throw new Exception(_localizer["Backend_InvalidDiceCount"]);

                if (request.DiceSides < 2 || request.DiceSides > 100)
                    throw new Exception(_localizer["Backend_InvalidDiceSides"]);
            }

            if (request.DifficultyValue < 0)
                throw new Exception(_localizer["Backend_InvalidDifficultyValue"]);

            foreach (var consequence in request.Consequences)
            {
                if (consequence.ApplyOn != TestConsequenceApplyOn.OnSuccess &&
                    consequence.ApplyOn != TestConsequenceApplyOn.OnFailure &&
                    consequence.ApplyOn != TestConsequenceApplyOn.OnCriticalSuccess &&
                    consequence.ApplyOn != TestConsequenceApplyOn.OnCriticalFailure)
                {
                    throw new Exception(_localizer["Backend_InvalidTestConsequenceApplyOn"]);
                }

                if (consequence.TargetDefinitionId == Guid.Empty)
                    throw new Exception(_localizer["Backend_InvalidTestConsequenceTarget"]);

                if (consequence.ValueMode == ModifierValueMode.Metric && !consequence.SourceMetricId.HasValue)
                    throw new Exception(_localizer["Backend_InvalidTestConsequenceSourceMetric"]);

                // Compatibilité transitoire :
                // l'UI actuelle des tests n'envoie pas encore OperationType.
                // On considère donc qu'une conséquence legacy est un AddValue.
                if ((int)consequence.OperationType == 0)
                {
                    consequence.OperationType = TestConsequenceOperationType.AddValue;
                }

                switch (consequence.OperationType)
                {
                    case TestConsequenceOperationType.AddValue:
                        if (consequence.TargetKind != TestConsequenceTargetKind.Attribute &&
                            consequence.TargetKind != TestConsequenceTargetKind.Gauge &&
                            consequence.TargetKind != TestConsequenceTargetKind.DerivedStat &&
                            consequence.TargetKind != TestConsequenceTargetKind.Metric)
                        {
                            throw new Exception(_localizer["Backend_InvalidTestConsequenceTarget"]);
                        }

                        if (consequence.ValueMode == ModifierValueMode.Fixed && consequence.Value == 0)
                            throw new Exception(_localizer["Backend_InvalidTestConsequenceValue"]);
                        break;

                    case TestConsequenceOperationType.GrantTalent:
                    case TestConsequenceOperationType.RevokeTalent:
                        if (consequence.TargetKind != TestConsequenceTargetKind.Talent)
                            throw new Exception(_localizer["Backend_InvalidTestConsequenceTarget"]);
                        break;

                    case TestConsequenceOperationType.GrantItem:
                    case TestConsequenceOperationType.RevokeItem:
                        if (consequence.TargetKind != TestConsequenceTargetKind.Item)
                            throw new Exception(_localizer["Backend_InvalidTestConsequenceTarget"]);
                        break;

                    default:
                        throw new Exception(_localizer["Backend_InvalidTestConsequenceOperation"]);
                }
            }
        }

        private int ApplyModifier(int baseValue, TestModifierMode mode, int difficultyValue)
        {
            return mode == TestModifierMode.Bonus
                ? baseValue + difficultyValue
                : baseValue - difficultyValue;
        }

        private PlayerGameTestResultDto BuildPlayerResult(PlayerTestRoll row)
        {
            return new PlayerGameTestResultDto
            {
                DiceResults = DeserializeDiceResults(row.DiceResultsJson),
                DiceTotal = row.DiceTotal,
                AttributeValue = row.AttributeValueSnapshot,
                EffectiveAttributeValue = row.EffectiveAttributeValue,
                FinalValue = row.FinalValue,
                IsSuccess = row.IsSuccess,
                Outcome = row.Outcome,
                IsAutoRolled = row.IsAutoRolled
            };
        }

        private List<int> RollDice(int diceCount, int diceSides)
        {
            var results = new List<int>();

            for (var i = 0; i < diceCount; i++)
            {
                results.Add(RandomNumberGenerator.GetInt32(1, diceSides + 1));
            }

            return results;
        }

        private List<int> DeserializeDiceResults(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new List<int>();

            try
            {
                return JsonSerializer.Deserialize<List<int>>(json) ?? new List<int>();
            }
            catch
            {
                return new List<int>();
            }
        }

        private class TestCandidateRow
        {
            public Rollocracy.Domain.Entities.Character Character { get; set; } = new();

            public Rollocracy.Domain.Entities.PlayerSession PlayerSession { get; set; } = new();
        }
    }
}