using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Rollocracy.Domain.Characters;
using Rollocracy.Domain.GameRules;
using Rollocracy.Domain.GameTests;
using Rollocracy.Domain.Interfaces;
using Rollocracy.Domain.Polls;
using Rollocracy.Infrastructure.Persistence;

namespace Rollocracy.Infrastructure.Services
{
    public class PollService : IPollService
    {
        private readonly IDbContextFactory<RollocracyDbContext> _contextFactory;
        private readonly IStringLocalizer _localizer;
        private readonly ISessionNotifier _sessionNotifier;
        private readonly IPresenceTracker _presenceTracker;
        private readonly ICharacterEffectService _characterEffectService;

        public PollService(
            IDbContextFactory<RollocracyDbContext> contextFactory,
            IStringLocalizerFactory localizerFactory,
            ISessionNotifier sessionNotifier,
            IPresenceTracker presenceTracker,
            ICharacterEffectService characterEffectService)
        {
            _contextFactory = contextFactory;
            _localizer = localizerFactory.Create("Rollocracy.Localization.SharedTexts", "Rollocracy");
            _sessionNotifier = sessionNotifier;
            _presenceTracker = presenceTracker;
            _characterEffectService = characterEffectService;
        }

        
        public async Task<PollForGameMasterDto> CreatePollAsync(Guid sessionId, Guid gameMasterUserAccountId, PollCreateRequestDto request)
        {
            ValidateCreateRequest(request);

            await using var context = await _contextFactory.CreateDbContextAsync();

            var session = await context.Sessions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.GameMasterUserAccountId == gameMasterUserAccountId);

            if (session == null)
                throw new Exception(_localizer["Backend_SessionNotFound"]);

            var activePoll = await context.SessionPolls
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.SessionId == sessionId && !p.IsClosed);

            if (activePoll != null)
                throw new Exception(_localizer["Backend_PollAlreadyActive"]);

            var gameSystemId = session.GameSystemId;
            if (!gameSystemId.HasValue)
                throw new Exception(_localizer["Backend_SessionHasNoGameSystem"]);

            Guid? metricDefinitionId = null;
            var metricNameSnapshot = string.Empty;

            if (request.VoteWeightMode == PollVoteWeightMode.Metric)
            {
                if (!request.MetricDefinitionId.HasValue)
                    throw new Exception(_localizer["Backend_PollMetricRequired"]);

                var metricDefinition = await context.MetricDefinitions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(m =>
                        m.Id == request.MetricDefinitionId.Value &&
                        m.GameSystemId == gameSystemId.Value);

                if (metricDefinition == null)
                    throw new Exception(_localizer["Backend_InvalidPollMetric"]);

                metricDefinitionId = metricDefinition.Id;
                metricNameSnapshot = metricDefinition.Name;
            }

            var poll = new SessionPoll
            {
                Id = Guid.NewGuid(),
                SessionId = sessionId,
                Question = request.Question.Trim(),
                IsClosed = false,
                ConsequencesApplied = false,
                VoteWeightMode = request.VoteWeightMode,
                MetricDefinitionId = metricDefinitionId,
                MetricNameSnapshot = metricNameSnapshot,
                CreatedAtUtc = DateTime.UtcNow
            };

            context.SessionPolls.Add(poll);

            var optionIdMap = new Dictionary<int, Guid>();

            for (var i = 0; i < request.Options.Count; i++)
            {
                var option = request.Options[i];
                var optionId = Guid.NewGuid();
                optionIdMap[i] = optionId;

                context.SessionPollOptions.Add(new SessionPollOption
                {
                    Id = optionId,
                    SessionPollId = poll.Id,
                    Label = option.Label.Trim(),
                    DisplayOrder = i
                });
            }

            // Compatibilité : on n'utilise plus la configuration manuelle des poids
            // dans le nouveau flux, mais on laisse la table en place pour l'historique.
            foreach (var weightRule in request.WeightRules.Where(r => r.WeightBonus > 0))
            {
                context.SessionPollWeightRules.Add(new SessionPollWeightRule
                {
                    Id = Guid.NewGuid(),
                    SessionPollId = poll.Id,
                    TraitDefinitionId = weightRule.TraitDefinitionId,
                    TraitOptionId = weightRule.TraitOptionId,
                    WeightBonus = Decimal.Round(weightRule.WeightBonus, 2, MidpointRounding.AwayFromZero)
                });
            }

            for (var i = 0; i < request.Options.Count; i++)
            {
                var optionDraft = request.Options[i];
                var optionId = optionIdMap[i];

                foreach (var consequence in optionDraft.Consequences.Where(IsMeaningfulPollConsequence))
                {
                    var targetName = await ResolveConsequenceTargetNameAsync(
                        context,
                        consequence.TargetKind,
                        consequence.TargetDefinitionId,
                        gameSystemId.Value);

                    context.SessionPollOptionConsequences.Add(new SessionPollOptionConsequence
                    {
                        Id = Guid.NewGuid(),
                        SessionPollOptionId = optionId,
                        TargetKind = consequence.TargetKind,
                        TargetDefinitionId = consequence.TargetDefinitionId,
                        TargetNameSnapshot = targetName,
                        ModifierMode = consequence.ModifierMode,
                        Value = consequence.Value,
                        OperationType = consequence.OperationType
                    });
                }
            }

            await context.SaveChangesAsync();

            await _sessionNotifier.NotifyPollChangedAsync(sessionId);

            return await BuildGameMasterDtoAsync(context, poll.Id);
        }

public async Task<PollForGameMasterDto?> GetLatestPollForGameMasterAsync(Guid sessionId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var poll = await context.SessionPolls
                .AsNoTracking()
                .Where(p => p.SessionId == sessionId)
                .OrderByDescending(p => p.CreatedAtUtc)
                .FirstOrDefaultAsync();

            if (poll == null)
                return null;

            return await BuildGameMasterDtoAsync(context, poll.Id);
        }

        public async Task<List<PollForGameMasterDto>> GetRecentPollsForGameMasterAsync(Guid sessionId, int takeCount)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var pollIds = await context.SessionPolls
                .AsNoTracking()
                .Where(p => p.SessionId == sessionId)
                .OrderByDescending(p => p.CreatedAtUtc)
                .Take(takeCount)
                .Select(p => p.Id)
                .ToListAsync();

            var result = new List<PollForGameMasterDto>();

            foreach (var pollId in pollIds)
            {
                result.Add(await BuildGameMasterDtoAsync(context, pollId));
            }

            return result;
        }

        public async Task<PollForGameMasterDto?> GetPollByIdForGameMasterAsync(Guid sessionId, Guid pollId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var pollExists = await context.SessionPolls
                .AsNoTracking()
                .AnyAsync(p => p.Id == pollId && p.SessionId == sessionId);

            if (!pollExists)
                return null;

            return await BuildGameMasterDtoAsync(context, pollId);
        }

        public async Task<PollForPlayerDto?> GetLatestPollForPlayerAsync(Guid playerSessionId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var recentLimitUtc = DateTime.UtcNow.AddSeconds(-30);

            var playerSession = await context.PlayerSessions
                .AsNoTracking()
                .FirstOrDefaultAsync(ps => ps.Id == playerSessionId);

            if (playerSession == null)
                throw new Exception(_localizer["Backend_PlayerSessionNotFound"]);

            var poll = await context.SessionPolls
                .AsNoTracking()
                .Where(p =>
                    p.SessionId == playerSession.SessionId &&
                    (
                        !p.IsClosed ||
                        (p.ClosedAtUtc.HasValue && p.ClosedAtUtc.Value >= recentLimitUtc)
                    ))
                .OrderByDescending(p => p.CreatedAtUtc)
                .FirstOrDefaultAsync();

            if (poll == null)
                return null;

            return await BuildPlayerDtoAsync(context, poll.Id, playerSessionId);
        }

        
        public async Task VoteAsync(Guid playerSessionId, Guid pollId, Guid optionId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var poll = await context.SessionPolls
                .FirstOrDefaultAsync(p => p.Id == pollId);

            if (poll == null)
                throw new Exception(_localizer["Backend_PollNotFound"]);

            if (poll.IsClosed)
                throw new Exception(_localizer["Backend_PollAlreadyClosed"]);

            var playerSession = await context.PlayerSessions
                .AsNoTracking()
                .FirstOrDefaultAsync(ps => ps.Id == playerSessionId);

            if (playerSession == null)
                throw new Exception(_localizer["Backend_PlayerSessionNotFound"]);

            var aliveCharacter = await context.Characters
                .AsNoTracking()
                .Where(c => c.PlayerSessionId == playerSessionId && c.IsAlive)
                .OrderByDescending(c => c.CreatedAt)
                .FirstOrDefaultAsync();

            if (aliveCharacter == null)
                throw new Exception(_localizer["Backend_NoAliveCharacterForVote"]);

            var option = await context.SessionPollOptions
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == optionId && o.SessionPollId == pollId);

            if (option == null)
                throw new Exception(_localizer["Backend_PollOptionNotFound"]);

            var existingVote = await context.SessionPollVotes
                .FirstOrDefaultAsync(v => v.SessionPollId == pollId && v.PlayerSessionId == playerSessionId);

            if (existingVote != null)
                throw new Exception(_localizer["Backend_PlayerAlreadyVoted"]);

            decimal voteWeight = 1.00m;

            if (poll.VoteWeightMode == PollVoteWeightMode.Metric)
            {
                if (!poll.MetricDefinitionId.HasValue)
                    throw new Exception(_localizer["Backend_InvalidPollMetric"]);

                voteWeight = await ComputeMetricVoteWeightAsync(context, poll.MetricDefinitionId.Value, aliveCharacter.Id);
            }
            else
            {
                // Compatibilité ancienne logique : si un vieux sondage possède encore des règles,
                // on les applique ; sinon poids fixe = 1.
                var weightRules = await context.SessionPollWeightRules
                    .AsNoTracking()
                    .Where(r => r.SessionPollId == pollId)
                    .ToListAsync();

                if (weightRules.Count > 0)
                {
                    var characterTraitValues = await context.CharacterTraitValues
                        .AsNoTracking()
                        .Where(v => v.CharacterId == aliveCharacter.Id)
                        .ToListAsync();

                    foreach (var rule in weightRules)
                    {
                        var matches = characterTraitValues.Any(v =>
                            v.TraitDefinitionId == rule.TraitDefinitionId &&
                            v.TraitOptionId == rule.TraitOptionId);

                        if (matches)
                        {
                            voteWeight += rule.WeightBonus;
                        }
                    }

                    voteWeight = Decimal.Round(voteWeight, 2, MidpointRounding.AwayFromZero);
                    if (voteWeight < 1.00m)
                        voteWeight = 1.00m;
                }
            }

            context.SessionPollVotes.Add(new SessionPollVote
            {
                Id = Guid.NewGuid(),
                SessionPollId = pollId,
                SessionPollOptionId = optionId,
                PlayerSessionId = playerSessionId,
                CharacterId = aliveCharacter.Id,
                VoteWeight = voteWeight,
                VotedAtUtc = DateTime.UtcNow
            });

            await context.SaveChangesAsync();

            await _sessionNotifier.NotifyPollChangedAsync(poll.SessionId);
        }

public async Task ClosePollAsync(Guid sessionId, Guid gameMasterUserAccountId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var session = await context.Sessions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.GameMasterUserAccountId == gameMasterUserAccountId);

            if (session == null)
                throw new Exception(_localizer["Backend_SessionNotFound"]);

            var poll = await context.SessionPolls
                .FirstOrDefaultAsync(p => p.SessionId == sessionId && !p.IsClosed);

            if (poll == null)
                throw new Exception(_localizer["Backend_NoActivePoll"]);

            var optionConsequences = await context.SessionPollOptionConsequences
                .Join(
                    context.SessionPollOptions,
                    consequence => consequence.SessionPollOptionId,
                    option => option.Id,
                    (consequence, option) => new { consequence, option })
                .Where(x => x.option.SessionPollId == poll.Id)
                .ToListAsync();

            var votes = await context.SessionPollVotes
                .Where(v => v.SessionPollId == poll.Id)
                .ToListAsync();

            var hasAppliedEffects = false;

            foreach (var vote in votes)
            {
                var character = await context.Characters.FirstOrDefaultAsync(c => c.Id == vote.CharacterId);
                if (character == null)
                    continue;

                var voteConsequences = optionConsequences
                    .Where(x => x.option.Id == vote.SessionPollOptionId)
                    .Select(x => x.consequence)
                    .ToList();

                // 1) Legacy : Attribute / Gauge restent sur l'ancien mécanisme
                // pour conserver le rollback actuel jusqu'à 5D.3.
                var legacyConsequences = voteConsequences
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
                                v.CharacterId == vote.CharacterId &&
                                v.GaugeDefinitionId == consequence.TargetDefinitionId);

                        if (gaugeValue != null)
                        {
                            var previousCharacterAlive = character.IsAlive;
                            var previousCharacterDiedAt = character.DiedAtUtc;
                            var previousValue = gaugeValue.Value;

                            gaugeValue.Value += signedValue;

                            await UpdateCharacterAliveStateAsync(context, vote.CharacterId);

                            hasAppliedEffects = true;

                            context.SessionPollAppliedEffects.Add(new SessionPollAppliedEffect
                            {
                                Id = Guid.NewGuid(),
                                SessionPollId = poll.Id,
                                CharacterId = vote.CharacterId,
                                SessionPollVoteId = vote.Id,
                                SessionPollOptionId = vote.SessionPollOptionId,
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
                                v.CharacterId == vote.CharacterId &&
                                v.AttributeDefinitionId == consequence.TargetDefinitionId);

                        if (attributeValue != null)
                        {
                            var previousCharacterAlive = character.IsAlive;
                            var previousCharacterDiedAt = character.DiedAtUtc;
                            var previousValue = attributeValue.Value;

                            attributeValue.Value += signedValue;

                            hasAppliedEffects = true;

                            context.SessionPollAppliedEffects.Add(new SessionPollAppliedEffect
                            {
                                Id = Guid.NewGuid(),
                                SessionPollId = poll.Id,
                                CharacterId = vote.CharacterId,
                                SessionPollVoteId = vote.Id,
                                SessionPollOptionId = vote.SessionPollOptionId,
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

                // 2) Nouveau moteur commun : DerivedStat / Metric / Talent / Item
                var commonEngineConsequences = voteConsequences
                    .Where(c => !(
                        c.OperationType == TestConsequenceOperationType.AddValue &&
                        (c.TargetKind == TestConsequenceTargetKind.Attribute ||
                         c.TargetKind == TestConsequenceTargetKind.Gauge)))
                    .ToList();

                foreach (var consequence in commonEngineConsequences)
                {
                    var effectDto = ToCharacterEffectDefinitionDto(consequence);

                    var previousCharacterAlive = character.IsAlive;
                    var previousCharacterDiedAt = character.DiedAtUtc;

                    var previousHasTargetLink = consequence.TargetKind switch
                    {
                        TestConsequenceTargetKind.Talent => await context.CharacterTalents.AnyAsync(x =>
                            x.CharacterId == vote.CharacterId &&
                            x.TalentDefinitionId == consequence.TargetDefinitionId),

                        TestConsequenceTargetKind.Item => await context.CharacterItems.AnyAsync(x =>
                            x.CharacterId == vote.CharacterId &&
                            x.ItemDefinitionId == consequence.TargetDefinitionId),

                        _ => false
                    };

                    await _characterEffectService.ApplyEffectsAsync(
                        poll.SessionId,
                        new List<Guid> { vote.CharacterId },
                        new List<CharacterEffectDefinitionDto> { effectDto },
                        CharacterEffectSourceType.Poll,
                        poll.Id,
                        $"Poll:{poll.Id}");

                    await context.Entry(character).ReloadAsync();

                    var newHasTargetLink = consequence.TargetKind switch
                    {
                        TestConsequenceTargetKind.Talent => await context.CharacterTalents.AnyAsync(x =>
                            x.CharacterId == vote.CharacterId &&
                            x.TalentDefinitionId == consequence.TargetDefinitionId),

                        TestConsequenceTargetKind.Item => await context.CharacterItems.AnyAsync(x =>
                            x.CharacterId == vote.CharacterId &&
                            x.ItemDefinitionId == consequence.TargetDefinitionId),

                        _ => false
                    };

                    if (consequence.TargetKind == TestConsequenceTargetKind.Talent ||
                        consequence.TargetKind == TestConsequenceTargetKind.Item)
                    {
                        context.SessionPollAppliedEffects.Add(new SessionPollAppliedEffect
                        {
                            Id = Guid.NewGuid(),
                            SessionPollId = poll.Id,
                            CharacterId = vote.CharacterId,
                            SessionPollVoteId = vote.Id,
                            SessionPollOptionId = vote.SessionPollOptionId,
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

                    hasAppliedEffects = true;
                }
            }

            poll.IsClosed = true;
            poll.ConsequencesApplied = hasAppliedEffects;
            poll.ClosedAtUtc = DateTime.UtcNow;

            await context.SaveChangesAsync();

            await _sessionNotifier.NotifyCharacterStateChangedAsync(sessionId);
            await _sessionNotifier.NotifyPollChangedAsync(sessionId);
        }

        public async Task UndoLatestPollConsequencesAsync(Guid sessionId, Guid gameMasterUserAccountId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var session = await context.Sessions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.GameMasterUserAccountId == gameMasterUserAccountId);

            if (session == null)
                throw new Exception(_localizer["Backend_SessionNotFound"]);

            var latestPoll = await context.SessionPolls
                .Where(p => p.SessionId == sessionId)
                .OrderByDescending(p => p.CreatedAtUtc)
                .FirstOrDefaultAsync();

            if (latestPoll == null)
                throw new Exception(_localizer["Backend_PollNotFound"]);

            if (!latestPoll.IsClosed)
                throw new Exception(_localizer["Backend_LastPollIsNotClosed"]);

            if (!latestPoll.ConsequencesApplied)
                throw new Exception(_localizer["Backend_LastPollHasNoAppliedConsequences"]);

            var appliedEffects = await context.SessionPollAppliedEffects
                .Where(e => e.SessionPollId == latestPoll.Id)
                .OrderByDescending(e => e.AppliedAtUtc)
                .ToListAsync();

            var pollCharacterModifiers = await context.CharacterModifiers
                .Where(x => x.SourceType == CharacterEffectSourceType.Poll && x.SourceId == latestPoll.Id)
                .ToListAsync();

            // Sécurité : si le booléen est incohérent avec les lignes réellement appliquées,
            // on remet juste l'état logique à false.
            if (appliedEffects.Count == 0)
            {
                latestPoll.ConsequencesApplied = false;
                await context.SaveChangesAsync();
                await _sessionNotifier.NotifyPollChangedAsync(sessionId);
                return;
            }

            // Même logique que pour le rollback de test :
            // si un ancien personnage serait ressuscité alors qu'un nouveau personnage vivant
            // existe déjà pour le même joueur, on laisse l'ancien mort.
            var charactersThatWouldBeResurrected = appliedEffects
                .Where(e => e.PreviousIsAlive && !e.NewIsAlive)
                .Select(e => e.CharacterId)
                .Distinct()
                .ToList();

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

            var modifiersToRemove = pollCharacterModifiers
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

                // On garde l'ancien personnage mort si un autre personnage vivant existe déjà.
                if (charactersToKeepDead.Contains(effect.CharacterId))
                    continue;

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

            context.SessionPollAppliedEffects.RemoveRange(appliedEffects);

            // Le sondage et les votes restent intacts ; seule l'application des conséquences disparaît.
            latestPoll.ConsequencesApplied = false;

            await context.SaveChangesAsync();

            await _sessionNotifier.NotifyCharacterStateChangedAsync(sessionId);
            await _sessionNotifier.NotifyPollChangedAsync(sessionId);
        }

        private async Task<decimal> ComputeMetricVoteWeightAsync(
    RollocracyDbContext context,
    Guid metricDefinitionId,
    Guid characterId)
        {
            var metricDefinition = await context.MetricDefinitions
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == metricDefinitionId);

            if (metricDefinition == null)
                throw new Exception(_localizer["Backend_PollMetricNotFound"]);

            var gameSystemId = metricDefinition.GameSystemId;

            var attributeDefinitions = await context.AttributeDefinitions
                .AsNoTracking()
                .Where(a => a.GameSystemId == gameSystemId)
                .ToListAsync();

            var attributeValues = await context.CharacterAttributeValues
                .AsNoTracking()
                .Where(v => v.CharacterId == characterId)
                .ToListAsync();

            var traitValues = await context.CharacterTraitValues
                .AsNoTracking()
                .Where(v => v.CharacterId == characterId)
                .ToListAsync();

            var traitOptionIds = traitValues
                .Select(v => v.TraitOptionId)
                .Distinct()
                .ToList();

            var choiceModifiers = await context.ChoiceOptionModifierDefinitions
                .AsNoTracking()
                .Where(m => traitOptionIds.Contains(m.TraitOptionId))
                .ToListAsync();

            var talentIds = await context.CharacterTalents
                .AsNoTracking()
                .Where(x => x.CharacterId == characterId)
                .Select(x => x.TalentDefinitionId)
                .ToListAsync();

            var itemIds = await context.CharacterItems
                .AsNoTracking()
                .Where(x => x.CharacterId == characterId)
                .Select(x => x.ItemDefinitionId)
                .ToListAsync();

            var talentModifiers = await context.TalentModifierDefinitions
                .AsNoTracking()
                .Where(m => talentIds.Contains(m.TalentDefinitionId))
                .ToListAsync();

            var itemModifiers = await context.ItemModifierDefinitions
                .AsNoTracking()
                .Where(m => itemIds.Contains(m.ItemDefinitionId))
                .ToListAsync();

            var allModifiers = choiceModifiers
                .Select(m => new RuntimeMetricModifier
                {
                    TargetType = m.TargetType,
                    TargetId = m.TargetId,
                    AddValue = m.AddValue
                })
                .Concat(talentModifiers.Select(m => new RuntimeMetricModifier
                {
                    TargetType = m.TargetType,
                    TargetId = m.TargetId,
                    AddValue = m.AddValue
                }))
                .Concat(itemModifiers.Select(m => new RuntimeMetricModifier
                {
                    TargetType = m.TargetType,
                    TargetId = m.TargetId,
                    AddValue = m.AddValue
                }))
                .ToList();

            var effectiveAttributeValues = attributeDefinitions.ToDictionary(
                definition => definition.Id,
                definition =>
                {
                    var baseValue = attributeValues.FirstOrDefault(v => v.AttributeDefinitionId == definition.Id)?.Value
                        ?? definition.DefaultValue;

                    var modifier = allModifiers
                        .Where(m => m.TargetType == ModifierTargetType.BaseAttribute && m.TargetId == definition.Id)
                        .Sum(m => m.AddValue);

                    var effectiveValue = baseValue + modifier;
                    return Math.Clamp(effectiveValue, definition.MinValue, definition.MaxValue);
                });

            var components = await context.MetricComponents
                .AsNoTracking()
                .Where(c => c.MetricDefinitionId == metricDefinitionId)
                .ToListAsync();

            decimal rawValue = metricDefinition.BaseValue;

            foreach (var component in components)
            {
                var effectiveValue = effectiveAttributeValues.TryGetValue(component.AttributeDefinitionId, out var sourceValue)
                    ? sourceValue
                    : 0;

                rawValue += effectiveValue * (component.Weight / 100m);
            }

            rawValue += allModifiers
                .Where(m => m.TargetType == ModifierTargetType.Metric && m.TargetId == metricDefinitionId)
                .Sum(m => m.AddValue);

            var roundedValue = ApplyComputedRoundMode(rawValue, metricDefinition.RoundMode);

            if (roundedValue < metricDefinition.MinValue)
                roundedValue = metricDefinition.MinValue;

            if (roundedValue > metricDefinition.MaxValue)
                roundedValue = metricDefinition.MaxValue;

            if (roundedValue < 1m)
                roundedValue = 1m;

            return decimal.Round(roundedValue, 2, MidpointRounding.AwayFromZero);
        }

        private sealed class RuntimeMetricModifier
        {
            public ModifierTargetType TargetType { get; set; }
            public Guid TargetId { get; set; }
            public int AddValue { get; set; }
        }

        private static decimal ApplyComputedRoundMode(decimal value, ComputedValueRoundMode roundMode)
        {
            return roundMode switch
            {
                ComputedValueRoundMode.Ceiling => Math.Ceiling(value),
                ComputedValueRoundMode.Floor => Math.Floor(value),
                ComputedValueRoundMode.Nearest => Math.Round(value, 0, MidpointRounding.AwayFromZero),
                ComputedValueRoundMode.None => value,
                _ => Math.Ceiling(value)
            };
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

        private static CharacterEffectDefinitionDto ToCharacterEffectDefinitionDto(SessionPollOptionConsequence consequence)
        {
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
                OperationType = consequence.OperationType switch
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
    TestConsequenceTargetKind targetKind,
    Guid targetDefinitionId,
    Guid gameSystemId)
        {
            switch (targetKind)
            {
                case TestConsequenceTargetKind.Gauge:
                    var gauge = await context.GaugeDefinitions
                        .AsNoTracking()
                        .FirstOrDefaultAsync(g => g.Id == targetDefinitionId && g.GameSystemId == gameSystemId);

                    if (gauge == null)
                        throw new Exception(_localizer["Backend_InvalidPollConsequenceTarget"]);

                    return gauge.Name;

                case TestConsequenceTargetKind.Attribute:
                    var attribute = await context.AttributeDefinitions
                        .AsNoTracking()
                        .FirstOrDefaultAsync(a => a.Id == targetDefinitionId && a.GameSystemId == gameSystemId);

                    if (attribute == null)
                        throw new Exception(_localizer["Backend_InvalidPollConsequenceTarget"]);

                    return attribute.Name;

                case TestConsequenceTargetKind.DerivedStat:
                    var derivedStat = await context.DerivedStatDefinitions
                        .AsNoTracking()
                        .FirstOrDefaultAsync(d => d.Id == targetDefinitionId && d.GameSystemId == gameSystemId);

                    if (derivedStat == null)
                        throw new Exception(_localizer["Backend_InvalidPollConsequenceTarget"]);

                    return derivedStat.Name;

                case TestConsequenceTargetKind.Metric:
                    var metric = await context.MetricDefinitions
                        .AsNoTracking()
                        .FirstOrDefaultAsync(m => m.Id == targetDefinitionId && m.GameSystemId == gameSystemId);

                    if (metric == null)
                        throw new Exception(_localizer["Backend_InvalidPollConsequenceTarget"]);

                    return metric.Name;

                case TestConsequenceTargetKind.Talent:
                    var talent = await context.TalentDefinitions
                        .AsNoTracking()
                        .FirstOrDefaultAsync(t => t.Id == targetDefinitionId && t.GameSystemId == gameSystemId);

                    if (talent == null)
                        throw new Exception(_localizer["Backend_InvalidPollConsequenceTarget"]);

                    return talent.Name;

                case TestConsequenceTargetKind.Item:
                    var item = await context.ItemDefinitions
                        .AsNoTracking()
                        .FirstOrDefaultAsync(i => i.Id == targetDefinitionId && i.GameSystemId == gameSystemId);

                    if (item == null)
                        throw new Exception(_localizer["Backend_InvalidPollConsequenceTarget"]);

                    return item.Name;

                default:
                    throw new Exception(_localizer["Backend_InvalidPollConsequenceTarget"]);
            }
        }


        private void ValidateCreateRequest(PollCreateRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Question))
                throw new Exception(_localizer["Backend_PollQuestionRequired"]);

            if (request.Options.Count < 2)
                throw new Exception(_localizer["Backend_PollAtLeastTwoOptions"]);

            foreach (var option in request.Options)
            {
                if (string.IsNullOrWhiteSpace(option.Label))
                    throw new Exception(_localizer["Backend_PollOptionLabelRequired"]);

                foreach (var consequence in option.Consequences)
                {
                    if (consequence.TargetDefinitionId == Guid.Empty)
                        throw new Exception(_localizer["Backend_InvalidPollConsequenceTarget"]);

                    // Compatibilité transitoire : l'UI legacy n'envoie pas encore OperationType.
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
                                throw new Exception(_localizer["Backend_InvalidPollConsequenceTarget"]);
                            }
                            break;

                        case TestConsequenceOperationType.GrantTalent:
                        case TestConsequenceOperationType.RevokeTalent:
                            if (consequence.TargetKind != TestConsequenceTargetKind.Talent)
                                throw new Exception(_localizer["Backend_InvalidPollConsequenceTarget"]);
                            break;

                        case TestConsequenceOperationType.GrantItem:
                        case TestConsequenceOperationType.RevokeItem:
                            if (consequence.TargetKind != TestConsequenceTargetKind.Item)
                                throw new Exception(_localizer["Backend_InvalidPollConsequenceTarget"]);
                            break;

                        default:
                            throw new Exception(_localizer["Backend_InvalidPollConsequenceOperation"]);
                    }
                }
            }
        }


        private async Task<PollForGameMasterDto> BuildGameMasterDtoAsync(RollocracyDbContext context, Guid pollId)
        {
            var poll = await context.SessionPolls
                .AsNoTracking()
                .FirstAsync(p => p.Id == pollId);

            var options = await context.SessionPollOptions
                .AsNoTracking()
                .Where(o => o.SessionPollId == pollId)
                .OrderBy(o => o.DisplayOrder)
                .ToListAsync();

            var votes = await context.SessionPollVotes
                .AsNoTracking()
                .Where(v => v.SessionPollId == pollId)
                .ToListAsync();

            var playerSessions = await context.PlayerSessions
                .AsNoTracking()
                .Where(ps => ps.SessionId == poll.SessionId && !ps.IsGameMaster)
                .ToListAsync();

            var characters = await context.Characters
                .AsNoTracking()
                .Where(c => playerSessions.Select(ps => ps.Id).Contains(c.PlayerSessionId))
                .ToListAsync();

            var onlinePlayersCount = playerSessions.Count(ps => _presenceTracker.IsPlayerOnline(ps.Id));
            var totalVotes = votes.Count;
            var totalWeightedVotes = votes.Sum(v => v.VoteWeight);

            var voteLines = votes
                .Join(
                    options,
                    vote => vote.SessionPollOptionId,
                    option => option.Id,
                    (vote, option) => new { vote, option })
                .Select(x =>
                {
                    var playerSession = playerSessions.FirstOrDefault(ps => ps.Id == x.vote.PlayerSessionId);
                    var character = characters.FirstOrDefault(c => c.Id == x.vote.CharacterId);

                    return new PollVoteLineDto
                    {
                        VoteId = x.vote.Id,
                        PlayerSessionId = x.vote.PlayerSessionId,
                        CharacterId = x.vote.CharacterId,
                        PlayerName = playerSession?.PlayerName ?? string.Empty,
                        CharacterName = character?.Name ?? string.Empty,
                        OptionId = x.option.Id,
                        OptionLabel = x.option.Label,
                        VoteWeight = x.vote.VoteWeight
                    };
                })
                .ToList();

            var weightRules = await context.SessionPollWeightRules
                .AsNoTracking()
                .Where(r => r.SessionPollId == pollId)
                .ToListAsync();

            var traitDefinitions = await context.TraitDefinitions
                .AsNoTracking()
                .ToListAsync();

            var traitOptions = await context.TraitOptions
                .AsNoTracking()
                .ToListAsync();

            var optionConsequences = await context.SessionPollOptionConsequences
                .AsNoTracking()
                .Join(
                    context.SessionPollOptions.AsNoTracking(),
                    consequence => consequence.SessionPollOptionId,
                    option => option.Id,
                    (consequence, option) => new { consequence, option })
                .Where(x => x.option.SessionPollId == pollId)
                .ToListAsync();

            return new PollForGameMasterDto
            {
                PollId = poll.Id,
                Question = poll.Question,
                IsClosed = poll.IsClosed,
                ConsequencesApplied = poll.ConsequencesApplied,
                VoteWeightMode = poll.VoteWeightMode,
                MetricDefinitionId = poll.MetricDefinitionId,
                MetricName = poll.MetricNameSnapshot,
                TotalVotes = totalVotes,
                TotalWeightedVotes = totalWeightedVotes,
                OnlinePlayersCount = onlinePlayersCount,
                ParticipationPercent = onlinePlayersCount == 0
                    ? 0
                    : (double)totalVotes * 100.0 / onlinePlayersCount,
                Options = options.Select(o =>
                {
                    var count = votes.Count(v => v.SessionPollOptionId == o.Id);
                    var weightedTotal = votes
                        .Where(v => v.SessionPollOptionId == o.Id)
                        .Sum(v => v.VoteWeight);

                    return new PollOptionResultDto
                    {
                        OptionId = o.Id,
                        Label = o.Label,
                        VoteCount = count,
                        WeightedVoteTotal = weightedTotal,
                        VotePercent = totalVotes == 0 ? 0 : (double)count * 100.0 / totalVotes,
                        WeightedVotePercent = totalWeightedVotes == 0 ? 0 : (double)(weightedTotal * 100m / totalWeightedVotes)
                    };
                }).ToList(),
                Votes = voteLines,
                WeightRules = weightRules.Select(r => new PollWeightRuleDto
                {
                    TraitDefinitionId = r.TraitDefinitionId,
                    TraitOptionId = r.TraitOptionId,
                    TraitDefinitionName = traitDefinitions.FirstOrDefault(td => td.Id == r.TraitDefinitionId)?.Name ?? string.Empty,
                    TraitOptionName = traitOptions.FirstOrDefault(to => to.Id == r.TraitOptionId)?.Name ?? string.Empty,
                    WeightBonus = r.WeightBonus
                }).ToList(),
                Consequences = optionConsequences.Select(x => new PollOptionConsequenceDto
                {
                    SessionPollOptionId = x.consequence.SessionPollOptionId,
                    OptionLabel = x.option.Label,
                    TargetKind = x.consequence.TargetKind,
                    TargetDefinitionId = x.consequence.TargetDefinitionId,
                    TargetName = x.consequence.TargetNameSnapshot,
                    ModifierMode = x.consequence.ModifierMode,
                    Value = x.consequence.Value
                }).ToList()
            };
        }

        private async Task<PollForPlayerDto> BuildPlayerDtoAsync(RollocracyDbContext context, Guid pollId, Guid playerSessionId)
        {
            var poll = await context.SessionPolls
                .AsNoTracking()
                .FirstAsync(p => p.Id == pollId);

            var options = await context.SessionPollOptions
                .AsNoTracking()
                .Where(o => o.SessionPollId == pollId)
                .OrderBy(o => o.DisplayOrder)
                .ToListAsync();

            var votes = await context.SessionPollVotes
                .AsNoTracking()
                .Where(v => v.SessionPollId == pollId)
                .ToListAsync();

            var playerVote = votes.FirstOrDefault(v => v.PlayerSessionId == playerSessionId);
            var totalVotes = votes.Count;
            var totalWeightedVotes = votes.Sum(v => v.VoteWeight);

            return new PollForPlayerDto
            {
                PollId = poll.Id,
                Question = poll.Question,
                IsClosed = poll.IsClosed,
                ClosedAtUtc = poll.ClosedAtUtc,
                HasVoted = playerVote is not null,
                SelectedOptionId = playerVote?.SessionPollOptionId,
                VoteWeight = playerVote?.VoteWeight,
                Options = options.Select(o =>
                {
                    var count = votes.Count(v => v.SessionPollOptionId == o.Id);
                    var weightedTotal = votes
                        .Where(v => v.SessionPollOptionId == o.Id)
                        .Sum(v => v.VoteWeight);

                    return new PollOptionResultDto
                    {
                        OptionId = o.Id,
                        Label = o.Label,
                        VoteCount = count,
                        WeightedVoteTotal = weightedTotal,
                        VotePercent = totalVotes == 0 ? 0 : (double)count * 100.0 / totalVotes,
                        WeightedVotePercent = totalWeightedVotes == 0 ? 0 : (double)(weightedTotal * 100m / totalWeightedVotes)
                    };
                }).ToList()
            };
        }

        private static bool IsMeaningfulPollConsequence(PollOptionConsequenceInlineDraftDto consequence)
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
    }
}