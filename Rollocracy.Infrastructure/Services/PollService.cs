using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Rollocracy.Domain.Characters;
using Rollocracy.Domain.Entities;
using Rollocracy.Domain.GameRules;
using Rollocracy.Domain.GameTests;
using Rollocracy.Domain.Interfaces;
using Rollocracy.Domain.Polls;
using Rollocracy.Infrastructure.Persistence;
using System.Text.Json;

namespace Rollocracy.Infrastructure.Services
{
    public class PollService : IPollService
    {
        private readonly IDbContextFactory<RollocracyDbContext> _contextFactory;
        private readonly IStringLocalizer _localizer;
        private readonly ISessionNotifier _sessionNotifier;
        private readonly IPresenceTracker _presenceTracker;
        private readonly ICharacterEffectService _characterEffectService;
        private string VoteWeightMetricName => _localizer["SystemMetric_VoteWeight"];

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
                throw new Exception(_localizer["Session_NotFound"]);

            var activePoll = await context.SessionPolls
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.SessionId == sessionId && !p.IsClosed);

            if (activePoll != null)
                throw new Exception(_localizer["Backend_PollAlreadyActive"]);

            var gameSystemId = session.GameSystemId;
            if (!gameSystemId.HasValue)
                throw new Exception(_localizer["Backend_SessionHasNoGameSystem"]);

            var metricDefinition = await context.MetricDefinitions
                .AsNoTracking()
                .FirstOrDefaultAsync(m =>
                    m.GameSystemId == gameSystemId.Value &&
                    m.Name == VoteWeightMetricName);

            if (metricDefinition == null)
                throw new Exception(_localizer["Backend_VoteWeightMetricNotFound"]);

            var metricDefinitionId = metricDefinition.Id;
            var metricNameSnapshot = metricDefinition.Name;

            var eligibleCharacterIds = await _characterEffectService.ResolveTargetCharacterIdsAsync(
                sessionId,
                request.AdvancedFilter ?? new CharacterTargetFilterDto
                {
                    OnlyAlive = true,
                    OnlyDead = false,
                    OnlyOnline = false,
                    IncludeNpcs = false,
                    MatchAllConditions = true
                });

            if (eligibleCharacterIds.Count == 0)
                throw new Exception(_localizer["Backend_PollNoEligibleCharacters"]);

            var poll = new SessionPoll
            {
                Id = Guid.NewGuid(),
                SessionId = sessionId,
                Question = request.Question.Trim(),
                IsClosed = false,
                ConsequencesApplied = false,
                VoteWeightMode = PollVoteWeightMode.Metric,
                MetricDefinitionId = metricDefinitionId,
                MetricNameSnapshot = metricNameSnapshot,
                CreatedAtUtc = DateTime.UtcNow
            };

            context.SessionPolls.Add(poll);

            foreach (var characterId in eligibleCharacterIds.Distinct())
            {
                context.SessionPollEligibleCharacters.Add(new SessionPollEligibleCharacter
                {
                    Id = Guid.NewGuid(),
                    SessionPollId = poll.Id,
                    CharacterId = characterId
                });
            }

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
                    if (consequence.ValueMode == ModifierValueMode.Metric)
                    {
                        var sourceMetricExists = consequence.SourceMetricId.HasValue && await context.MetricDefinitions
                            .AsNoTracking()
                            .AnyAsync(m => m.Id == consequence.SourceMetricId.Value && m.GameSystemId == gameSystemId.Value);

                        if (!sourceMetricExists)
                            throw new Exception(_localizer["Backend_InvalidPollConsequenceSourceMetric"]);
                    }

                    var targetName = await ResolveConsequenceTargetNameAsync(
                        context,
                        consequence.TargetKind,
                        consequence.TargetDefinitionId,
                        sessionId,
                        gameSystemId.Value);

                    context.SessionPollOptionConsequences.Add(new SessionPollOptionConsequence
                    {
                        Id = Guid.NewGuid(),
                        SessionPollOptionId = optionId,
                        TargetKind = consequence.TargetKind,
                        TargetDefinitionId = consequence.TargetDefinitionId,
                        TargetNameSnapshot = targetName,
                        ModifierMode = consequence.ValueMode == ModifierValueMode.Metric ? consequence.ModifierMode : TestModifierMode.Bonus,
                        Value = consequence.Value,
                        ValueMode = consequence.ValueMode,
                        SourceMetricId = consequence.ValueMode == ModifierValueMode.Metric
                            ? consequence.SourceMetricId
                            : null,
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

            var aliveCharacter = await context.Characters
                .AsNoTracking()
                .Where(c => c.PlayerSessionId == playerSessionId && c.IsAlive && !c.IsNpc)
                .OrderByDescending(c => c.CreatedAt)
                .FirstOrDefaultAsync();

            if (aliveCharacter == null)
                return null;

            var poll = await context.SessionPolls
                .AsNoTracking()
                .Where(p =>
                    p.SessionId == playerSession.SessionId &&
                    (
                        !p.IsClosed ||
                        (p.ClosedAtUtc.HasValue && p.ClosedAtUtc.Value >= recentLimitUtc)
                    ))
                .Join(
                    context.SessionPollEligibleCharacters.AsNoTracking(),
                    poll => poll.Id,
                    eligible => eligible.SessionPollId,
                    (poll, eligible) => new { poll, eligible })
                .Where(x => x.eligible.CharacterId == aliveCharacter.Id)
                .OrderByDescending(x => x.poll.CreatedAtUtc)
                .Select(x => x.poll)
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
                .Where(c => c.PlayerSessionId == playerSessionId && c.IsAlive && !c.IsNpc)
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

            var isEligible = await context.SessionPollEligibleCharacters
                .AsNoTracking()
                .AnyAsync(x => x.SessionPollId == pollId && x.CharacterId == aliveCharacter.Id);

            if (!isEligible)
                throw new Exception(_localizer["Backend_PlayerNotEligibleForPoll"]);

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

        private async Task<int> ComputeEffectiveGaugeMaxAsync(
    RollocracyDbContext context,
    Guid characterId,
    Guid gaugeDefinitionId)
        {
            var definition = await context.GaugeDefinitions
                .AsNoTracking()
                .FirstAsync(x => x.Id == gaugeDefinitionId);

            var traitValues = await context.CharacterTraitValues
                .AsNoTracking()
                .Where(x => x.CharacterId == characterId)
                .ToListAsync();

            var characterTalents = await context.CharacterTalents
                .AsNoTracking()
                .Where(x => x.CharacterId == characterId)
                .ToListAsync();

            var characterItems = await context.CharacterItems
                .AsNoTracking()
                .Where(x => x.CharacterId == characterId)
                .ToListAsync();

            var traitOptionIds = traitValues.Select(x => x.TraitOptionId).Distinct().ToList();
            var talentIds = characterTalents.Select(x => x.TalentDefinitionId).Distinct().ToList();
            var itemIds = characterItems.Select(x => x.ItemDefinitionId).Distinct().ToList();

            var choiceModifiers = await context.ChoiceOptionModifierDefinitions
                .AsNoTracking()
                .Where(x => traitOptionIds.Contains(x.ChoiceOptionDefinitionId))
                .ToListAsync();

            var talentModifiers = await context.TalentModifierDefinitions
                .AsNoTracking()
                .Where(x => talentIds.Contains(x.TalentDefinitionId))
                .ToListAsync();

            var itemModifiers = await context.ItemModifierDefinitions
                .AsNoTracking()
                .Where(x => itemIds.Contains(x.ItemDefinitionId))
                .ToListAsync();

            var characterModifiers = await context.CharacterModifiers
                .AsNoTracking()
                .Where(x => x.CharacterId == characterId)
                .ToListAsync();

            var choiceBonus = choiceModifiers
                .Where(x => x.TargetType == ModifierTargetType.Gauge &&
                            x.TargetId == gaugeDefinitionId &&
                            x.ValueMode != ModifierValueMode.Metric)
                .Sum(x => x.Value);

            var talentBonus = talentModifiers
                .Where(x => x.TargetType == ModifierTargetType.Gauge &&
                            x.TargetId == gaugeDefinitionId &&
                            x.ValueMode != ModifierValueMode.Metric)
                .Sum(x => x.AddValue);

            var itemBonus = itemModifiers
                .Where(x => x.TargetType == ModifierTargetType.Gauge &&
                            x.TargetId == gaugeDefinitionId &&
                            x.ValueMode != ModifierValueMode.Metric)
                .Sum(x => x.AddValue);

            var persistentBonus = characterModifiers
                .Where(x => x.TargetType == CharacterEffectTargetType.Gauge &&
                            x.TargetId == gaugeDefinitionId)
                .Sum(x => x.AddValue);

            return Math.Max(definition.MinValue, definition.MaxValue + choiceBonus + talentBonus + itemBonus + persistentBonus);
        }

        private async Task UpdateCharacterAliveStateAsync(
            RollocracyDbContext context,
            Guid? gameSystemId,
            Guid characterId,
            Character character)
        {
            if (!gameSystemId.HasValue)
                return;

            var healthGaugeDefinitions = await context.GaugeDefinitions
                .AsNoTracking()
                .Where(x => x.GameSystemId == gameSystemId.Value && x.IsHealthGauge)
                .ToListAsync();

            if (healthGaugeDefinitions.Count == 0)
                return;

            var healthGaugeDefinitionIds = healthGaugeDefinitions
                .Select(x => x.Id)
                .ToList();

            var persistedHealthGaugeValues = await context.CharacterGaugeValues
                .AsNoTracking()
                .Where(x => x.CharacterId == characterId && healthGaugeDefinitionIds.Contains(x.GaugeDefinitionId))
                .ToListAsync();

            var trackedHealthGaugeValues = context.ChangeTracker
                .Entries<CharacterGaugeValue>()
                .Where(x =>
                    x.Entity.CharacterId == characterId &&
                    healthGaugeDefinitionIds.Contains(x.Entity.GaugeDefinitionId))
                .GroupBy(x => x.Entity.GaugeDefinitionId)
                .ToDictionary(x => x.Key, x => x.Last().Entity.Value);

            var isAlive = healthGaugeDefinitions.All(definition =>
            {
                if (trackedHealthGaugeValues.TryGetValue(definition.Id, out var trackedValue))
                    return trackedValue > 0;

                var persistedValue = persistedHealthGaugeValues.FirstOrDefault(x => x.GaugeDefinitionId == definition.Id)?.Value
                    ?? definition.DefaultValue;

                return persistedValue > 0;
            });

            if (isAlive)
            {
                character.IsAlive = true;
                character.DiedAtUtc = null;
            }
            else
            {
                if (character.IsAlive)
                    character.DiedAtUtc = DateTime.UtcNow;

                character.IsAlive = false;
            }
        }

        public async Task ClosePollAsync(Guid sessionId, Guid gameMasterUserAccountId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var session = await context.Sessions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.GameMasterUserAccountId == gameMasterUserAccountId);

            if (session == null)
                throw new Exception(_localizer["Session_NotFound"]);

            var poll = await context.SessionPolls
                .FirstOrDefaultAsync(p => p.SessionId == sessionId && !p.IsClosed);

            if (poll == null)
                throw new Exception(_localizer["Backend_NoActivePoll"]);

            var metricDefinitions = await context.MetricDefinitions
                .AsNoTracking()
                .ToListAsync();

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

                var legacyConsequences = voteConsequences
                    .Where(c =>
                        c.OperationType == TestConsequenceOperationType.AddValue &&
                        c.ValueMode != ModifierValueMode.Metric &&
                        (c.TargetKind == TestConsequenceTargetKind.Attribute ||
                         c.TargetKind == TestConsequenceTargetKind.Gauge ||
                         c.TargetKind == TestConsequenceTargetKind.SessionGauge))
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

                            var gaugeDefinition = await context.GaugeDefinitions
                                .AsNoTracking()
                                .FirstOrDefaultAsync(g => g.Id == consequence.TargetDefinitionId);

                            if (gaugeDefinition != null)
                            {
                                var effectiveMax = await ComputeEffectiveGaugeMaxAsync(
                                    context,
                                    vote.CharacterId,
                                    consequence.TargetDefinitionId);

                                gaugeValue.Value = Math.Clamp(
                                    gaugeValue.Value + signedValue,
                                    gaugeDefinition.MinValue,
                                    effectiveMax);

                                await UpdateCharacterAliveStateAsync(
                                    context,
                                    session.GameSystemId,
                                    vote.CharacterId,
                                    character);
                            }

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
                    else if (consequence.TargetKind == TestConsequenceTargetKind.SessionGauge)
                    {
                        var sessionGauge = await context.SessionGauges
                            .FirstOrDefaultAsync(g => g.Id == consequence.TargetDefinitionId && g.SessionId == poll.SessionId);

                        if (sessionGauge != null)
                        {
                            var previousCharacterAlive = character.IsAlive;
                            var previousCharacterDiedAt = character.DiedAtUtc;
                            var previousValue = sessionGauge.CurrentValue;

                            sessionGauge.CurrentValue = Math.Clamp(
                                sessionGauge.CurrentValue + signedValue,
                                sessionGauge.MinValue,
                                sessionGauge.MaxValue);

                            hasAppliedEffects = true;

                            context.SessionPollAppliedEffects.Add(new SessionPollAppliedEffect
                            {
                                Id = Guid.NewGuid(),
                                SessionPollId = poll.Id,
                                CharacterId = vote.CharacterId,
                                SessionPollVoteId = vote.Id,
                                SessionPollOptionId = vote.SessionPollOptionId,
                                TargetKind = TestConsequenceTargetKind.SessionGauge,
                                TargetDefinitionId = consequence.TargetDefinitionId,
                                OperationType = TestConsequenceOperationType.AddValue,
                                PreviousValue = previousValue,
                                NewValue = sessionGauge.CurrentValue,
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

                var commonEngineConsequences = voteConsequences
                    .Where(c => !(
                        c.OperationType == TestConsequenceOperationType.AddValue &&
                        c.ValueMode != ModifierValueMode.Metric &&
                        (c.TargetKind == TestConsequenceTargetKind.Attribute ||
                         c.TargetKind == TestConsequenceTargetKind.Gauge ||
                         c.TargetKind == TestConsequenceTargetKind.SessionGauge)))
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

        public async Task<List<SessionPollPresetDto>> GetSessionPresetsAsync(Guid sessionId, Guid gameMasterUserAccountId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var sessionExists = await context.Sessions
                .AsNoTracking()
                .AnyAsync(s => s.Id == sessionId && s.GameMasterUserAccountId == gameMasterUserAccountId);

            if (!sessionExists)
                throw new Exception(_localizer["Session_NotFound"]);

            var presets = await context.SessionPollPresets
                .AsNoTracking()
                .Where(x => x.SessionId == sessionId)
                .OrderBy(x => x.Name)
                .ToListAsync();

            return presets.Select(x => new SessionPollPresetDto
            {
                PresetId = x.Id,
                SessionId = x.SessionId,
                Name = x.Name,
                CreatedAtUtc = x.CreatedAtUtc,
                UpdatedAtUtc = x.UpdatedAtUtc,
                Request = DeserializePollPresetPayload(x.PayloadJson)
            }).ToList();
        }

        public async Task SaveSessionPresetAsync(
            Guid sessionId,
            Guid gameMasterUserAccountId,
            string presetName,
            PollCreateRequestDto request,
            bool overwrite)
        {
            var normalizedName = (presetName ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalizedName))
                throw new Exception(_localizer["Backend_PollPresetNameRequired"]);

            await using var context = await _contextFactory.CreateDbContextAsync();

            var sessionExists = await context.Sessions
                .AsNoTracking()
                .AnyAsync(s => s.Id == sessionId && s.GameMasterUserAccountId == gameMasterUserAccountId);

            if (!sessionExists)
                throw new Exception(_localizer["Session_NotFound"]);

            var existing = await context.SessionPollPresets
                .FirstOrDefaultAsync(x => x.SessionId == sessionId && x.Name == normalizedName);

            var payloadJson = JsonSerializer.Serialize(request);

            if (existing is null)
            {
                context.SessionPollPresets.Add(new SessionPollPreset
                {
                    Id = Guid.NewGuid(),
                    SessionId = sessionId,
                    Name = normalizedName,
                    PayloadJson = payloadJson,
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow
                });
            }
            else
            {
                if (!overwrite)
                    throw new Exception(_localizer["Backend_PollPresetAlreadyExists"]);

                existing.PayloadJson = payloadJson;
                existing.UpdatedAtUtc = DateTime.UtcNow;
            }

            await context.SaveChangesAsync();
        }

        public async Task DeleteSessionPresetAsync(Guid sessionId, Guid gameMasterUserAccountId, Guid presetId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var sessionExists = await context.Sessions
                .AsNoTracking()
                .AnyAsync(s => s.Id == sessionId && s.GameMasterUserAccountId == gameMasterUserAccountId);

            if (!sessionExists)
                throw new Exception(_localizer["Session_NotFound"]);

            var preset = await context.SessionPollPresets
                .FirstOrDefaultAsync(x => x.Id == presetId && x.SessionId == sessionId);

            if (preset is null)
                throw new Exception(_localizer["Backend_PollPresetNotFound"]);

            context.SessionPollPresets.Remove(preset);
            await context.SaveChangesAsync();
        }

        public async Task UndoLatestPollConsequencesAsync(Guid sessionId, Guid gameMasterUserAccountId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var session = await context.Sessions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.GameMasterUserAccountId == gameMasterUserAccountId);

            if (session == null)
                throw new Exception(_localizer["Session_NotFound"]);

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
                else if (effect.TargetKind == TestConsequenceTargetKind.SessionGauge)
                {
                    var sessionGauge = await context.SessionGauges
                        .FirstOrDefaultAsync(g => g.Id == effect.TargetDefinitionId && g.SessionId == latestPoll.SessionId);

                    if (sessionGauge != null)
                    {
                        sessionGauge.CurrentValue = effect.PreviousValue;
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
                .Where(m => traitOptionIds.Contains(m.ChoiceOptionDefinitionId))
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

            var rawModifiers = choiceModifiers
                .Select(m => new RuntimeMetricModifier
                {
                    TargetType = m.TargetType,
                    TargetId = m.TargetId,
                    AddValue = m.Value,
                    ValueMode = m.ValueMode,
                    SourceMetricId = m.SourceMetricId
                })
                .Concat(talentModifiers.Select(m => new RuntimeMetricModifier
                {
                    TargetType = m.TargetType,
                    TargetId = m.TargetId,
                    AddValue = m.AddValue,
                    ValueMode = m.ValueMode,
                    SourceMetricId = m.SourceMetricId
                }))
                .Concat(itemModifiers.Select(m => new RuntimeMetricModifier
                {
                    TargetType = m.TargetType,
                    TargetId = m.TargetId,
                    AddValue = m.AddValue,
                    ValueMode = m.ValueMode,
                    SourceMetricId = m.SourceMetricId
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

            var derivedComponentIds = derivedDefinitions.Select(d => d.Id).ToList();
            var derivedComponents = await context.DerivedStatComponents
                .AsNoTracking()
                .Where(c => derivedComponentIds.Contains(c.DerivedStatDefinitionId))
                .ToListAsync();

            var gaugeDefinitions = await context.GaugeDefinitions
                .AsNoTracking()
                .Where(g => g.GameSystemId == gameSystemId)
                .ToListAsync();

            var gaugeValues = await context.CharacterGaugeValues
                .AsNoTracking()
                .Where(v => v.CharacterId == characterId)
                .ToListAsync();

            var effectiveGaugeValues = gaugeDefinitions.ToDictionary(
                definition => definition.Id,
                definition =>
                {
                    var baseValue = gaugeValues.FirstOrDefault(v => v.GaugeDefinitionId == definition.Id)?.Value
                        ?? definition.DefaultValue;

                    return Math.Clamp(baseValue, definition.MinValue, definition.MaxValue);
                });

            var preliminaryDerivedValues = new Dictionary<Guid, int>();
            foreach (var definition in derivedDefinitions)
            {
                decimal rawValue = 0m;
                foreach (var component in derivedComponents.Where(c => c.DerivedStatDefinitionId == definition.Id))
                {
                    var sourceValue = effectiveAttributeValues.TryGetValue(component.AttributeDefinitionId, out var attrValue) ? attrValue : 0;
                    rawValue += sourceValue * (component.Weight / 100m);
                }

                rawValue = ApplyComputedRoundMode(rawValue, definition.RoundMode);
                var finalValue = (int)rawValue + rawModifiers
                    .Where(m => m.ValueMode != ModifierValueMode.Metric && m.TargetType == ModifierTargetType.DerivedStat && m.TargetId == definition.Id)
                    .Sum(m => m.AddValue);

                preliminaryDerivedValues[definition.Id] = Math.Clamp(finalValue, definition.MinValue, definition.MaxValue);
            }

            var allMetricDefinitions = await context.MetricDefinitions
                .AsNoTracking()
                .Where(m => m.GameSystemId == gameSystemId)
                .OrderBy(m => m.DisplayOrder).ThenBy(m => m.Name)
                .ToListAsync();

            var allMetricIds = allMetricDefinitions.Select(m => m.Id).ToList();
            var components = await context.MetricComponents
                .AsNoTracking()
                .Where(c => allMetricIds.Contains(c.MetricDefinitionId))
                .ToListAsync();

            var formulaSteps = await context.MetricFormulaSteps
                .AsNoTracking()
                .Where(c => allMetricIds.Contains(c.MetricDefinitionId))
                .ToListAsync();

            var allModifiers = ResolveRuntimeModifiers(
                rawModifiers,
                allMetricDefinitions,
                components,
                formulaSteps,
                effectiveAttributeValues,
                effectiveGaugeValues,
                preliminaryDerivedValues);

            effectiveAttributeValues = attributeDefinitions.ToDictionary(
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

            var derivedValues = new Dictionary<Guid, int>();
            foreach (var definition in derivedDefinitions)
            {
                decimal rawValue = 0m;
                foreach (var component in derivedComponents.Where(c => c.DerivedStatDefinitionId == definition.Id))
                {
                    var sourceValue = effectiveAttributeValues.TryGetValue(component.AttributeDefinitionId, out var attrValue) ? attrValue : 0;
                    rawValue += sourceValue * (component.Weight / 100m);
                }

                rawValue = ApplyComputedRoundMode(rawValue, definition.RoundMode);
                var finalValue = (int)rawValue + allModifiers
                    .Where(m => m.TargetType == ModifierTargetType.DerivedStat && m.TargetId == definition.Id)
                    .Sum(m => m.AddValue);

                derivedValues[definition.Id] = Math.Clamp(finalValue, definition.MinValue, definition.MaxValue);
            }

            var metricValue = MetricFormulaEngine.ComputeSingle(new MetricFormulaEngine.MetricComputationRequest
            {
                MetricDefinitions = allMetricDefinitions,
                FormulaSteps = formulaSteps,
                LegacyComponents = components,
                BaseAttributeValues = effectiveAttributeValues,
                GaugeValues = effectiveGaugeValues,
                DerivedStatValues = derivedValues,
                Modifiers = allModifiers.Select(m => new MetricFormulaEngine.ModifierValue
                {
                    TargetType = m.TargetType,
                    TargetId = m.TargetId,
                    AddValue = m.AddValue
                }).ToList()
            }, metricDefinitionId);

            if (metricValue < 1)
                metricValue = 1;

            return decimal.Round(metricValue, 2, MidpointRounding.AwayFromZero);
        }

        private static List<RuntimeMetricModifier> ResolveRuntimeModifiers(
            List<RuntimeMetricModifier> rawModifiers,
            List<MetricDefinition> metricDefinitions,
            List<MetricComponent> metricComponents,
            List<MetricFormulaStep> formulaSteps,
            Dictionary<Guid, int> attributeValues,
            Dictionary<Guid, int> gaugeValues,
            Dictionary<Guid, int> derivedValues)
        {
            var fixedModifiers = rawModifiers
                .Where(x => x.ValueMode != ModifierValueMode.Metric || !x.SourceMetricId.HasValue)
                .Select(x => new RuntimeMetricModifier
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
                FormulaSteps = formulaSteps,
                LegacyComponents = metricComponents,
                BaseAttributeValues = attributeValues,
                GaugeValues = gaugeValues,
                DerivedStatValues = derivedValues,
                Modifiers = fixedModifiers.Select(x => new MetricFormulaEngine.ModifierValue
                {
                    TargetType = x.TargetType,
                    TargetId = x.TargetId,
                    AddValue = x.AddValue
                }).ToList()
            });

            return rawModifiers.Select(x => new RuntimeMetricModifier
            {
                TargetType = x.TargetType,
                TargetId = x.TargetId,
                AddValue = x.ValueMode == ModifierValueMode.Metric && x.SourceMetricId.HasValue && metricValues.TryGetValue(x.SourceMetricId.Value, out var metricValue)
                    ? metricValue
                    : x.AddValue,
                ValueMode = ModifierValueMode.Fixed
            }).ToList();
        }

        private sealed class RuntimeMetricModifier
        {
            public ModifierTargetType TargetType { get; set; }
            public Guid TargetId { get; set; }
            public int AddValue { get; set; }
            public ModifierValueMode ValueMode { get; set; }
            public Guid? SourceMetricId { get; set; }
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
                TestConsequenceTargetKind.SessionGauge => CharacterEffectTargetType.SessionGauge,
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
                Value = resolvedValue,
                ValueMode = consequence.ValueMode,
                SourceMetricId = consequence.ValueMode == ModifierValueMode.Metric
                    ? consequence.SourceMetricId
                    : null
            };
        }

        private async Task<string> ResolveConsequenceTargetNameAsync(
            RollocracyDbContext context,
            TestConsequenceTargetKind targetKind,
            Guid targetDefinitionId,
            Guid sessionId,
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

                case TestConsequenceTargetKind.SessionGauge:
                    var sessionGauge = await context.SessionGauges
                        .AsNoTracking()
                        .FirstOrDefaultAsync(g => g.Id == targetDefinitionId && g.SessionId == sessionId);

                    if (sessionGauge == null)
                        throw new Exception(_localizer["Backend_InvalidPollConsequenceTarget"]);

                    return sessionGauge.Name;

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
                        .FirstOrDefaultAsync(i => i.Id == targetDefinitionId && (i.GameSystemId == gameSystemId || i.SessionId == sessionId));

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

                    if (consequence.ValueMode == ModifierValueMode.Metric && !consequence.SourceMetricId.HasValue)
                        throw new Exception(_localizer["Backend_InvalidPollConsequenceSourceMetric"]);

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
                                consequence.TargetKind != TestConsequenceTargetKind.SessionGauge &&
                                consequence.TargetKind != TestConsequenceTargetKind.DerivedStat &&
                                consequence.TargetKind != TestConsequenceTargetKind.Metric)
                            {
                                throw new Exception(_localizer["Backend_InvalidPollConsequenceTarget"]);
                            }

                            if (consequence.ValueMode == ModifierValueMode.Fixed && consequence.Value == 0)
                                throw new Exception(_localizer["Backend_InvalidPollConsequenceValue"]);
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

            var eligibleCharacterIds = await context.SessionPollEligibleCharacters
                .AsNoTracking()
                .Where(x => x.SessionPollId == pollId)
                .Select(x => x.CharacterId)
                .ToListAsync();

            var eligibleCharacters = characters
                .Where(c => eligibleCharacterIds.Contains(c.Id))
                .ToList();

            var eligiblePlayerSessionIds = eligibleCharacters
                .Select(c => c.PlayerSessionId)
                .Distinct()
                .ToHashSet();

            var onlinePlayersCount = playerSessions.Count(ps => _presenceTracker.IsPlayerOnline(ps.Id));
            var eligiblePlayersCount = eligiblePlayerSessionIds.Count;
            var eligibleOnlinePlayersCount = playerSessions.Count(ps =>
                eligiblePlayerSessionIds.Contains(ps.Id) &&
                _presenceTracker.IsPlayerOnline(ps.Id));

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

            var metricDefinitions = await context.MetricDefinitions
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
                OnlinePlayersCount = eligibleOnlinePlayersCount,
                ParticipationPercent = eligibleOnlinePlayersCount == 0 //remplacement de eligiblePlayersCount par eligibleOnelinePlayersCount
                    ? 0
                    : (double)totalVotes * 100.0 / eligibleOnlinePlayersCount, //remplacement de eligiblePlayersCount par eligibleOnelinePlayersCount
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
                    Value = x.consequence.Value,
                    ValueMode = x.consequence.ValueMode,
                    SourceMetricId = x.consequence.SourceMetricId,
                    SourceMetricName = x.consequence.SourceMetricId.HasValue
                        ? metricDefinitions.FirstOrDefault(m => m.Id == x.consequence.SourceMetricId.Value)?.Name ?? string.Empty
                        : string.Empty,
                    OperationType = x.consequence.OperationType
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

        private static PollCreateRequestDto DeserializePollPresetPayload(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new PollCreateRequestDto();

            try
            {
                return JsonSerializer.Deserialize<PollCreateRequestDto>(json) ?? new PollCreateRequestDto();
            }
            catch
            {
                return new PollCreateRequestDto();
            }
        }
    }
}