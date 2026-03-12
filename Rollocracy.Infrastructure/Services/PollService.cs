using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
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

        public PollService(
            IDbContextFactory<RollocracyDbContext> contextFactory,
            IStringLocalizerFactory localizerFactory,
            ISessionNotifier sessionNotifier,
            IPresenceTracker presenceTracker)
        {
            _contextFactory = contextFactory;
            _localizer = localizerFactory.Create("Rollocracy.Localization.SharedTexts", "Rollocracy");
            _sessionNotifier = sessionNotifier;
            _presenceTracker = presenceTracker;
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

            var poll = new SessionPoll
            {
                Id = Guid.NewGuid(),
                SessionId = sessionId,
                Question = request.Question.Trim(),
                IsClosed = false,
                ConsequencesApplied = false,
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

                foreach (var consequence in optionDraft.Consequences.Where(c => c.Value > 0))
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
                        Value = consequence.Value
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

            var weightRules = await context.SessionPollWeightRules
                .AsNoTracking()
                .Where(r => r.SessionPollId == pollId)
                .ToListAsync();

            var characterTraitValues = await context.CharacterTraitValues
                .AsNoTracking()
                .Where(v => v.CharacterId == aliveCharacter.Id)
                .ToListAsync();

            decimal voteWeight = 1.00m;

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

                foreach (var consequence in voteConsequences)
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
                                PreviousValue = previousValue,
                                NewValue = gaugeValue.Value,
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
                                PreviousValue = previousValue,
                                NewValue = attributeValue.Value,
                                PreviousIsAlive = previousCharacterAlive,
                                NewIsAlive = character.IsAlive,
                                PreviousDiedAtUtc = previousCharacterDiedAt,
                                NewDiedAtUtc = character.DiedAtUtc,
                                AppliedAtUtc = DateTime.UtcNow
                            });
                        }
                    }
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

            foreach (var effect in appliedEffects)
            {
                var character = await context.Characters
                    .FirstOrDefaultAsync(c => c.Id == effect.CharacterId);

                if (character == null)
                    continue;

                // On garde l'ancien personnage mort si un autre personnage vivant existe déjà.
                if (charactersToKeepDead.Contains(effect.CharacterId))
                    continue;

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

        private async Task<string> ResolveConsequenceTargetNameAsync(
            RollocracyDbContext context,
            TestConsequenceTargetKind targetKind,
            Guid targetDefinitionId,
            Guid gameSystemId)
        {
            if (targetKind == TestConsequenceTargetKind.Gauge)
            {
                var gauge = await context.GaugeDefinitions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(g => g.Id == targetDefinitionId && g.GameSystemId == gameSystemId);

                if (gauge == null)
                    throw new Exception(_localizer["Backend_InvalidConsequenceTarget"]);

                return gauge.Name;
            }

            var attribute = await context.AttributeDefinitions
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == targetDefinitionId && a.GameSystemId == gameSystemId);

            if (attribute == null)
                throw new Exception(_localizer["Backend_InvalidConsequenceTarget"]);

            return attribute.Name;
        }

        private void ValidateCreateRequest(PollCreateRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Question))
                throw new Exception(_localizer["Backend_PollQuestionRequired"]);

            var cleanedOptions = request.Options
                .Where(o => !string.IsNullOrWhiteSpace(o.Label))
                .ToList();

            if (cleanedOptions.Count < 2)
                throw new Exception(_localizer["Backend_PollNeedsTwoOptions"]);

            foreach (var rule in request.WeightRules)
            {
                if (rule.WeightBonus < 0)
                    throw new Exception(_localizer["Backend_InvalidPollWeightBonus"]);
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
                    TraitDefinitionName = traitDefinitions.FirstOrDefault(t => t.Id == r.TraitDefinitionId)?.Name ?? string.Empty,
                    TraitOptionName = traitOptions.FirstOrDefault(o => o.Id == r.TraitOptionId)?.Name ?? string.Empty,
                    WeightBonus = r.WeightBonus
                }).ToList(),
                Consequences = optionConsequences.Select(x => new PollOptionConsequenceDto
                {
                    SessionPollOptionId = x.option.Id,
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
    }
}