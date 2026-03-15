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

            var attributeDefinition = await context.AttributeDefinitions
                .AsNoTracking()
                .FirstOrDefaultAsync(a =>
                    a.Id == request.AttributeDefinitionId &&
                    a.GameSystemId == session.GameSystemId.Value);

            if (attributeDefinition == null)
                throw new Exception(_localizer["Backend_AttributeDefinitionNotFound"]);

            if (gameSystem.TestResolutionMode == TestResolutionMode.SuccessThreshold && !request.SuccessThreshold.HasValue)
                throw new Exception(_localizer["Backend_SuccessThresholdRequired"]);

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

            var attributeValues = await context.CharacterAttributeValues
                .AsNoTracking()
                .Where(v => v.AttributeDefinitionId == request.AttributeDefinitionId)
                .ToListAsync();

            var test = new GameTest
            {
                Id = Guid.NewGuid(),
                SessionId = sessionId,
                AttributeDefinitionId = request.AttributeDefinitionId,
                AttributeNameSnapshot = attributeDefinition.Name,
                ResolutionModeSnapshot = gameSystem.TestResolutionMode,
                DiceCount = request.DiceCount,
                DiceSides = request.DiceSides,
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
                var targetName = await ResolveConsequenceTargetNameAsync(context, consequence, session.GameSystemId.Value);

                context.GameTestConsequences.Add(new GameTestConsequence
                {
                    Id = Guid.NewGuid(),
                    GameTestId = test.Id,
                    ApplyOn = consequence.ApplyOn,
                    OperationType = consequence.OperationType,
                    TargetKind = consequence.TargetKind,
                    TargetDefinitionId = consequence.TargetDefinitionId,
                    TargetNameSnapshot = targetName,
                    ModifierMode = consequence.ModifierMode,
                    Value = consequence.Value
                });
            }

            foreach (var row in candidateRows)
            {
                var attributeValue = attributeValues
                    .FirstOrDefault(v => v.CharacterId == row.Character.Id)?.Value ?? attributeDefinition.DefaultValue;

                var effectiveAttributeValue = ApplyModifier(
                    attributeValue,
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
                    AttributeValueSnapshot = attributeValue,
                    EffectiveAttributeValue = effectiveAttributeValue,
                    DiceResultsJson = "[]",
                    DiceTotal = 0,
                    FinalValue = 0,
                    IsSuccess = false,
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
                AttributeName = playerRoll.test.AttributeNameSnapshot,
                ResolutionMode = playerRoll.test.ResolutionModeSnapshot,
                DiceCount = playerRoll.test.DiceCount,
                DiceSides = playerRoll.test.DiceSides,
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

            row.DiceResultsJson = JsonSerializer.Serialize(diceResults);
            row.DiceTotal = diceTotal;
            row.FinalValue = finalValue;
            row.IsSuccess = isSuccess;
            row.HasRolled = true;
            row.IsAutoRolled = isAutoRoll;
            row.RolledAtUtc = DateTime.UtcNow;

            await context.SaveChangesAsync();

            await ApplyConsequencesAsync(context, test.Id, row.CharacterId, isSuccess);

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
    bool isSuccess)
        {
            var applyOn = isSuccess ? TestConsequenceApplyOn.OnSuccess : TestConsequenceApplyOn.OnFailure;

            var consequences = await context.GameTestConsequences
                .Where(c => c.GameTestId == gameTestId && c.ApplyOn == applyOn)
                .ToListAsync();

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
                AttributeName = test.AttributeNameSnapshot,
                ResolutionMode = test.ResolutionModeSnapshot,
                DiceCount = test.DiceCount,
                DiceSides = test.DiceSides,
                SuccessThreshold = test.SuccessThreshold,
                ModifierMode = test.ModifierMode,
                DifficultyValue = test.DifficultyValue,
                TargetScope = test.TargetScope,
                TraitFilterMode = test.TraitFilterMode,
                IsClosed = test.IsClosed,
                AutoRollAtUtc = test.AutoRollAtUtc,
                TargetCount = rows.Count,
                RolledCount = rolledRows.Count,
                SuccessCount = rolledRows.Count(r => r.IsSuccess),
                FailureCount = rolledRows.Count(r => !r.IsSuccess),
                SuccessRatePercent = rolledRows.Count == 0 ? 0 : (double)rolledRows.Count(r => r.IsSuccess) * 100.0 / rolledRows.Count,
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
                Value = signedValue
            };
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
                TestConsequenceOperationType.AddValue => consequence.Value != 0,
                TestConsequenceOperationType.GrantTalent => consequence.TargetDefinitionId != Guid.Empty,
                TestConsequenceOperationType.RevokeTalent => consequence.TargetDefinitionId != Guid.Empty,
                TestConsequenceOperationType.GrantItem => consequence.TargetDefinitionId != Guid.Empty,
                TestConsequenceOperationType.RevokeItem => consequence.TargetDefinitionId != Guid.Empty,
                _ => false
            };
        }

        private void ValidateCreateRequest(GameTestCreateRequestDto request)
        {
            if (request.AttributeDefinitionId == Guid.Empty)
                throw new Exception(_localizer["Backend_AttributeDefinitionNotFound"]);

            if (request.DiceCount < 1 || request.DiceCount > 5)
                throw new Exception(_localizer["Backend_InvalidDiceCount"]);

            if (request.DiceSides < 2 || request.DiceSides > 100)
                throw new Exception(_localizer["Backend_InvalidDiceSides"]);

            if (request.DifficultyValue < 0)
                throw new Exception(_localizer["Backend_InvalidDifficultyValue"]);

            foreach (var consequence in request.Consequences)
            {
                if (consequence.ApplyOn != TestConsequenceApplyOn.OnSuccess &&
                    consequence.ApplyOn != TestConsequenceApplyOn.OnFailure)
                {
                    throw new Exception(_localizer["Backend_InvalidTestConsequenceApplyOn"]);
                }

                if (consequence.TargetDefinitionId == Guid.Empty)
                    throw new Exception(_localizer["Backend_InvalidTestConsequenceTarget"]);

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