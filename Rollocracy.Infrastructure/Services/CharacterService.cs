using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Rollocracy.Domain.Characters;
using Rollocracy.Domain.Entities;
using Rollocracy.Domain.GameRules;
using Rollocracy.Domain.Interfaces;
using Rollocracy.Infrastructure.Persistence;

namespace Rollocracy.Infrastructure.Services
{
    public class CharacterService : ICharacterService
    {
        private readonly IDbContextFactory<RollocracyDbContext> _contextFactory;
        private readonly IStringLocalizer _localizer;
        private readonly IPresenceTracker _presenceTracker;

        public CharacterService(
            IDbContextFactory<RollocracyDbContext> contextFactory,
            IStringLocalizerFactory localizerFactory,
            IPresenceTracker presenceTracker)
        {
            _contextFactory = contextFactory;
            _localizer = localizerFactory.Create("Rollocracy.Localization.SharedTexts", "Rollocracy");
            _presenceTracker = presenceTracker;
        }

        public async Task<PlayerRoomStateDto> GetPlayerRoomStateAsync(Guid playerSessionId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var playerSession = await context.PlayerSessions
                .AsNoTracking()
                .FirstOrDefaultAsync(ps => ps.Id == playerSessionId);

            if (playerSession == null)
                throw new Exception(_localizer["Backend_PlayerSessionNotFound"]);

            var session = await context.Sessions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == playerSession.SessionId);

            if (session == null)
                throw new Exception(_localizer["Backend_SessionNotFound"]);

            var stats = await BuildSessionStatsAsync(context, session.Id);

            var playerCharacters = await context.Characters
                .AsNoTracking()
                .Where(c => c.PlayerSessionId == playerSessionId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            var aliveCharacter = playerCharacters.FirstOrDefault(c => c.IsAlive);

            var result = new PlayerRoomStateDto
            {
                PlayerSessionId = playerSessionId,
                SessionId = session.Id,
                HasAssignedGameSystem = session.GameSystemId.HasValue,
                SessionStats = stats
            };

            if (aliveCharacter != null)
            {
                result.AliveCharacter = await BuildCharacterSheetAsync(context, playerSessionId, aliveCharacter.Id);
                result.CanCreateNewCharacter = false;
                result.CanCreateNewCharacterAtUtc = null;
            }

            result.DeadCharacters = playerCharacters
                .Where(c => !c.IsAlive)
                .Select(c => new CharacterListItemDto
                {
                    CharacterId = c.Id,
                    Name = c.Name,
                    IsAlive = false,
                    DiedAtUtc = c.DiedAtUtc
                })
                .ToList();

            if (result.AliveCharacter is null)
            {
                var latestDeathUtc = result.DeadCharacters
                    .Where(c => c.DiedAtUtc.HasValue)
                    .Select(c => c.DiedAtUtc!.Value)
                    .OrderByDescending(x => x)
                    .FirstOrDefault();

                if (latestDeathUtc == default)
                {
                    result.CanCreateNewCharacter = true;
                    result.CanCreateNewCharacterAtUtc = null;
                }
                else
                {
                    var allowedAtUtc = latestDeathUtc.AddSeconds(30);
                    result.CanCreateNewCharacterAtUtc = allowedAtUtc;
                    result.CanCreateNewCharacter = DateTime.UtcNow >= allowedAtUtc;
                }
            }

            return result;
        }

        public async Task<CharacterCreationTemplateDto> GetCharacterCreationTemplateAsync(Guid playerSessionId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var playerSession = await context.PlayerSessions
                .AsNoTracking()
                .FirstOrDefaultAsync(ps => ps.Id == playerSessionId);

            if (playerSession == null)
                throw new Exception(_localizer["Backend_PlayerSessionNotFound"]);

            var session = await context.Sessions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == playerSession.SessionId);

            if (session == null)
                throw new Exception(_localizer["Backend_SessionNotFound"]);

            if (!session.GameSystemId.HasValue)
                throw new Exception(_localizer["Backend_SessionHasNoGameSystem"]);

            var gameSystem = await context.GameSystems
                .AsNoTracking()
                .FirstOrDefaultAsync(gs => gs.Id == session.GameSystemId.Value);

            if (gameSystem == null)
                throw new Exception(_localizer["Backend_GameSystemNotFound"]);

            var attributes = await context.AttributeDefinitions
                .AsNoTracking()
                .Where(a => a.GameSystemId == gameSystem.Id)
                .OrderBy(a => a.Name)
                .ToListAsync();

            var traits = await context.TraitDefinitions
                .AsNoTracking()
                .Where(t => t.GameSystemId == gameSystem.Id)
                .OrderBy(t => t.Name)
                .ToListAsync();

            var result = new CharacterCreationTemplateDto
            {
                PlayerSessionId = playerSessionId,
                SessionId = session.Id,
                GameSystemId = gameSystem.Id,
                GameSystemName = gameSystem.Name,
                Attributes = attributes.Select(a => new CharacterCreationAttributeDto
                {
                    AttributeDefinitionId = a.Id,
                    Name = a.Name,
                    MinValue = a.MinValue,
                    MaxValue = a.MaxValue,
                    DefaultValue = GenerateAttributeDefaultValue(a)
                }).ToList()
            };

            foreach (var trait in traits)
            {
                var options = await context.TraitOptions
                    .AsNoTracking()
                    .Where(o => o.TraitDefinitionId == trait.Id)
                    .OrderBy(o => o.Name)
                    .ToListAsync();

                result.Traits.Add(new CharacterCreationTraitDto
                {
                    TraitDefinitionId = trait.Id,
                    Name = trait.Name,
                    Options = options.Select(o => new CharacterCreationTraitOptionDto
                    {
                        TraitOptionId = o.Id,
                        Name = o.Name
                    }).ToList()
                });
            }

            return result;
        }

        public async Task<Character> CreateCharacterAsync(
            Guid playerSessionId,
            string name,
            string biography,
            Dictionary<Guid, int> attributeValues,
            Dictionary<Guid, Guid> traitSelections)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var playerSession = await context.PlayerSessions
                .AsNoTracking()
                .FirstOrDefaultAsync(ps => ps.Id == playerSessionId);

            if (playerSession == null)
                throw new Exception(_localizer["Backend_PlayerSessionNotFound"]);

            var session = await context.Sessions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == playerSession.SessionId);

            if (session == null)
                throw new Exception(_localizer["Backend_SessionNotFound"]);

            if (!session.GameSystemId.HasValue)
                throw new Exception(_localizer["Backend_SessionHasNoGameSystem"]);

            var existingAliveCharacter = await context.Characters
                .FirstOrDefaultAsync(c => c.PlayerSessionId == playerSessionId && c.IsAlive);

            if (existingAliveCharacter != null)
                throw new Exception(_localizer["Backend_PlayerAlreadyHasAliveCharacter"]);

            if (string.IsNullOrWhiteSpace(name))
                throw new Exception(_localizer["Backend_CharacterNameRequired"]);

            var gameSystemId = session.GameSystemId.Value;

            var attributeDefinitions = await context.AttributeDefinitions
                .AsNoTracking()
                .Where(a => a.GameSystemId == gameSystemId)
                .ToListAsync();

            var traitDefinitions = await context.TraitDefinitions
                .AsNoTracking()
                .Where(t => t.GameSystemId == gameSystemId)
                .ToListAsync();

            var gaugeDefinitions = await context.GaugeDefinitions
                .AsNoTracking()
                .Where(g => g.GameSystemId == gameSystemId)
                .ToListAsync();

            var character = new Character
            {
                Id = Guid.NewGuid(),
                PlayerSessionId = playerSessionId,
                Name = name.Trim(),
                Biography = biography?.Trim() ?? string.Empty,
                IsAlive = true,
                DiedAtUtc = null
            };

            context.Characters.Add(character);

            foreach (var attributeDefinition in attributeDefinitions)
            {
                var value = GenerateAttributeDefaultValue(attributeDefinition);

                if (attributeValues.TryGetValue(attributeDefinition.Id, out var submittedValue))
                {
                    value = submittedValue;
                }

                value = Math.Clamp(value, attributeDefinition.MinValue, attributeDefinition.MaxValue);

                context.CharacterAttributeValues.Add(new CharacterAttributeValue
                {
                    Id = Guid.NewGuid(),
                    CharacterId = character.Id,
                    AttributeDefinitionId = attributeDefinition.Id,
                    Value = value
                });
            }

            foreach (var traitDefinition in traitDefinitions)
            {
                if (!traitSelections.TryGetValue(traitDefinition.Id, out var selectedOptionId))
                    throw new Exception(_localizer["Backend_MissingTraitSelection"]);

                var optionExists = await context.TraitOptions
                    .AsNoTracking()
                    .AnyAsync(o => o.Id == selectedOptionId && o.TraitDefinitionId == traitDefinition.Id);

                if (!optionExists)
                    throw new Exception(_localizer["Backend_InvalidTraitSelection"]);

                context.CharacterTraitValues.Add(new CharacterTraitValue
                {
                    Id = Guid.NewGuid(),
                    CharacterId = character.Id,
                    TraitDefinitionId = traitDefinition.Id,
                    TraitOptionId = selectedOptionId
                });
            }

            foreach (var gaugeDefinition in gaugeDefinitions)
            {
                context.CharacterGaugeValues.Add(new CharacterGaugeValue
                {
                    Id = Guid.NewGuid(),
                    CharacterId = character.Id,
                    GaugeDefinitionId = gaugeDefinition.Id,
                    Value = gaugeDefinition.DefaultValue
                });

                if (gaugeDefinition.IsHealthGauge && gaugeDefinition.DefaultValue <= 0)
                {
                    character.IsAlive = false;
                    character.DiedAtUtc = DateTime.UtcNow;
                }
            }

            await context.SaveChangesAsync();

            return character;
        }

        public async Task<CharacterSheetDto?> GetCharacterSheetAsync(Guid playerSessionId, Guid characterId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await BuildCharacterSheetAsync(context, playerSessionId, characterId);
        }

        public async Task<SessionPublicStatsDto> GetSessionPublicStatsAsync(Guid sessionId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await BuildSessionStatsAsync(context, sessionId);
        }

        public async Task<List<SessionCharacterSummaryDto>> GetSessionCharacterSummariesAsync(
            Guid sessionId,
            bool includeOffline,
            bool includeDead)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var rows = await context.Characters
                .AsNoTracking()
                .Join(
                    context.PlayerSessions.AsNoTracking(),
                    character => character.PlayerSessionId,
                    playerSession => playerSession.Id,
                    (character, playerSession) => new { character, playerSession })
                .Where(x => x.playerSession.SessionId == sessionId)
                .OrderBy(x => x.character.Name)
                .ToListAsync();

            var result = new List<SessionCharacterSummaryDto>();

            foreach (var row in rows)
            {
                var isOnline = _presenceTracker.IsPlayerOnline(row.playerSession.Id);

                if (!includeOffline && !isOnline)
                    continue;

                if (!includeDead && !row.character.IsAlive)
                    continue;

                var sheet = await BuildCharacterSheetAsync(context, row.playerSession.Id, row.character.Id);
                if (sheet == null)
                    continue;

                result.Add(new SessionCharacterSummaryDto
                {
                    CharacterId = row.character.Id,
                    CharacterName = row.character.Name,
                    PlayerName = row.playerSession.PlayerName,
                    IsAlive = row.character.IsAlive,
                    IsOnline = isOnline,
                    Attributes = sheet.Attributes,
                    DerivedStats = sheet.DerivedStats,
                    Metrics = sheet.Metrics,
                    Traits = sheet.Traits,
                    Gauges = sheet.Gauges,
                    Talents = sheet.Talents,
                    Items = sheet.Items
                });
            }

            return result;
        }

        public async Task<CharacterSheetDto?> GetCharacterSheetForSessionAsync(Guid sessionId, Guid characterId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var row = await context.Characters
                .AsNoTracking()
                .Join(
                    context.PlayerSessions.AsNoTracking(),
                    character => character.PlayerSessionId,
                    playerSession => playerSession.Id,
                    (character, playerSession) => new { character, playerSession })
                .FirstOrDefaultAsync(x =>
                    x.character.Id == characterId &&
                    x.playerSession.SessionId == sessionId);

            if (row == null)
                return null;

            return await BuildCharacterSheetAsync(context, row.playerSession.Id, row.character.Id);
        }

        public async Task<EditableCharacterDto?> GetEditableCharacterForSessionAsync(
            Guid sessionId,
            Guid characterId,
            Guid gameMasterUserAccountId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var session = await context.Sessions
                .AsNoTracking()
                .FirstOrDefaultAsync(s =>
                    s.Id == sessionId &&
                    s.GameMasterUserAccountId == gameMasterUserAccountId);

            if (session == null)
                return null;

            if (!session.GameSystemId.HasValue)
                throw new Exception(_localizer["Backend_SessionHasNoGameSystem"]);

            var row = await context.Characters
                .AsNoTracking()
                .Join(
                    context.PlayerSessions.AsNoTracking(),
                    character => character.PlayerSessionId,
                    playerSession => playerSession.Id,
                    (character, playerSession) => new { character, playerSession })
                .FirstOrDefaultAsync(x =>
                    x.character.Id == characterId &&
                    x.playerSession.SessionId == sessionId);

            if (row == null)
                return null;

            return await BuildEditableCharacterAsync(context, session, row.playerSession, row.character);
        }

        public async Task<CharacterUpdateResultDto> UpdateCharacterForSessionAsync(
            Guid sessionId,
            Guid characterId,
            Guid gameMasterUserAccountId,
            UpdateCharacterRequestDto request)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var session = await context.Sessions
                .AsNoTracking()
                .FirstOrDefaultAsync(s =>
                    s.Id == sessionId &&
                    s.GameMasterUserAccountId == gameMasterUserAccountId);

            if (session == null)
                throw new Exception(_localizer["Backend_SessionNotFound"]);

            if (!session.GameSystemId.HasValue)
                throw new Exception(_localizer["Backend_SessionHasNoGameSystem"]);

            var row = await context.Characters
                .Join(
                    context.PlayerSessions,
                    character => character.PlayerSessionId,
                    playerSession => playerSession.Id,
                    (character, playerSession) => new { character, playerSession })
                .FirstOrDefaultAsync(x =>
                    x.character.Id == characterId &&
                    x.playerSession.SessionId == sessionId);

            if (row == null)
                throw new Exception(_localizer["Backend_CharacterNotFoundForSession"]);

            if (string.IsNullOrWhiteSpace(request.Name))
                throw new Exception(_localizer["Backend_CharacterNameRequired"]);

            var gameSystemId = session.GameSystemId.Value;

            var attributeDefinitions = await context.AttributeDefinitions
                .AsNoTracking()
                .Where(a => a.GameSystemId == gameSystemId)
                .ToListAsync();

            var gaugeDefinitions = await context.GaugeDefinitions
                .AsNoTracking()
                .Where(g => g.GameSystemId == gameSystemId)
                .ToListAsync();

            var traitDefinitions = await context.TraitDefinitions
                .AsNoTracking()
                .Where(t => t.GameSystemId == gameSystemId)
                .ToListAsync();

            var traitDefinitionIds = traitDefinitions.Select(t => t.Id).ToList();

            var traitOptions = await context.TraitOptions
                .AsNoTracking()
                .Where(o => traitDefinitionIds.Contains(o.TraitDefinitionId))
                .ToListAsync();

            var talentDefinitions = await context.TalentDefinitions
                .AsNoTracking()
                .Where(t => t.GameSystemId == gameSystemId)
                .ToListAsync();

            var itemDefinitions = await context.ItemDefinitions
                .AsNoTracking()
                .Where(i => i.GameSystemId == gameSystemId)
                .ToListAsync();

            var attributeValues = await context.CharacterAttributeValues
                .Where(v => v.CharacterId == row.character.Id)
                .ToListAsync();

            var gaugeValues = await context.CharacterGaugeValues
                .Where(v => v.CharacterId == row.character.Id)
                .ToListAsync();

            var traitValues = await context.CharacterTraitValues
                .Where(v => v.CharacterId == row.character.Id)
                .ToListAsync();

            var characterTalents = await context.CharacterTalents
                .Where(x => x.CharacterId == row.character.Id)
                .ToListAsync();

            var characterItems = await context.CharacterItems
                .Where(x => x.CharacterId == row.character.Id)
                .ToListAsync();

            row.character.Name = request.Name.Trim();
            row.character.Biography = request.Biography?.Trim() ?? string.Empty;

            foreach (var definition in attributeDefinitions)
            {
                var requestedValue = request.Attributes
                    .FirstOrDefault(x => x.AttributeDefinitionId == definition.Id)?.Value;

                var clampedValue = requestedValue ?? definition.DefaultValue;
                clampedValue = Math.Clamp(clampedValue, definition.MinValue, definition.MaxValue);

                var existingValue = attributeValues.FirstOrDefault(x => x.AttributeDefinitionId == definition.Id);

                if (existingValue == null)
                {
                    context.CharacterAttributeValues.Add(new CharacterAttributeValue
                    {
                        Id = Guid.NewGuid(),
                        CharacterId = row.character.Id,
                        AttributeDefinitionId = definition.Id,
                        Value = clampedValue
                    });
                }
                else
                {
                    existingValue.Value = clampedValue;
                }
            }

            foreach (var definition in traitDefinitions)
            {
                var requestedTrait = request.Traits
                    .FirstOrDefault(x => x.TraitDefinitionId == definition.Id);

                var existingValue = traitValues.FirstOrDefault(x => x.TraitDefinitionId == definition.Id);

                if (requestedTrait?.SelectedOptionId is null)
                {
                    if (existingValue != null)
                        context.CharacterTraitValues.Remove(existingValue);

                    continue;
                }

                var optionExists = traitOptions.Any(o =>
                    o.Id == requestedTrait.SelectedOptionId.Value &&
                    o.TraitDefinitionId == definition.Id);

                if (!optionExists)
                    throw new Exception(_localizer["Backend_InvalidTraitSelection"]);

                if (existingValue == null)
                {
                    context.CharacterTraitValues.Add(new CharacterTraitValue
                    {
                        Id = Guid.NewGuid(),
                        CharacterId = row.character.Id,
                        TraitDefinitionId = definition.Id,
                        TraitOptionId = requestedTrait.SelectedOptionId.Value
                    });
                }
                else
                {
                    existingValue.TraitOptionId = requestedTrait.SelectedOptionId.Value;
                }
            }

            var selectedTalentIds = request.SelectedTalentIds
                .Distinct()
                .Where(id => talentDefinitions.Any(t => t.Id == id))
                .ToHashSet();

            var selectedItemIds = request.SelectedItemIds
                .Distinct()
                .Where(id => itemDefinitions.Any(i => i.Id == id))
                .ToHashSet();

            var existingTalentIds = characterTalents.Select(x => x.TalentDefinitionId).ToHashSet();
            var existingItemIds = characterItems.Select(x => x.ItemDefinitionId).ToHashSet();

            foreach (var talentIdToAdd in selectedTalentIds.Except(existingTalentIds))
            {
                context.CharacterTalents.Add(new CharacterTalent
                {
                    Id = Guid.NewGuid(),
                    CharacterId = row.character.Id,
                    TalentDefinitionId = talentIdToAdd
                });
            }

            foreach (var talentToRemove in characterTalents.Where(x => !selectedTalentIds.Contains(x.TalentDefinitionId)).ToList())
            {
                context.CharacterTalents.Remove(talentToRemove);
            }

            foreach (var itemIdToAdd in selectedItemIds.Except(existingItemIds))
            {
                context.CharacterItems.Add(new CharacterItem
                {
                    Id = Guid.NewGuid(),
                    CharacterId = row.character.Id,
                    ItemDefinitionId = itemIdToAdd
                });
            }

            foreach (var itemToRemove in characterItems.Where(x => !selectedItemIds.Contains(x.ItemDefinitionId)).ToList())
            {
                context.CharacterItems.Remove(itemToRemove);
            }

            var proposedGaugeValues = new Dictionary<Guid, int>();
            var previousGaugeValues = new Dictionary<Guid, int>();

            foreach (var definition in gaugeDefinitions)
            {
                var existingValue = gaugeValues.FirstOrDefault(x => x.GaugeDefinitionId == definition.Id);
                var requestedValue = request.Gauges
                    .FirstOrDefault(x => x.GaugeDefinitionId == definition.Id)?.Value;

                var previousValue = existingValue?.Value ?? definition.DefaultValue;
                var nextValue = requestedValue ?? previousValue;
                nextValue = Math.Clamp(nextValue, definition.MinValue, definition.MaxValue);

                previousGaugeValues[definition.Id] = previousValue;
                proposedGaugeValues[definition.Id] = nextValue;
            }

            var healthGauges = gaugeDefinitions.Where(g => g.IsHealthGauge).ToList();
            var wouldBeAlive = healthGauges.All(g => proposedGaugeValues[g.Id] > 0);
            var resurrectionBlocked = false;

            if (!row.character.IsAlive && wouldBeAlive)
            {
                var otherAliveCharacterExists = await context.Characters
                    .AsNoTracking()
                    .AnyAsync(c =>
                        c.PlayerSessionId == row.playerSession.Id &&
                        c.Id != row.character.Id &&
                        c.IsAlive);

                if (otherAliveCharacterExists)
                {
                    resurrectionBlocked = true;

                    foreach (var healthGauge in healthGauges)
                    {
                        var previousValue = previousGaugeValues[healthGauge.Id];
                        var proposedValue = proposedGaugeValues[healthGauge.Id];

                        if (previousValue <= 0 && proposedValue > 0)
                        {
                            proposedGaugeValues[healthGauge.Id] = 0;
                        }
                    }

                    wouldBeAlive = false;
                }
            }

            foreach (var definition in gaugeDefinitions)
            {
                var finalValue = proposedGaugeValues[definition.Id];
                var existingValue = gaugeValues.FirstOrDefault(x => x.GaugeDefinitionId == definition.Id);

                if (existingValue == null)
                {
                    context.CharacterGaugeValues.Add(new CharacterGaugeValue
                    {
                        Id = Guid.NewGuid(),
                        CharacterId = row.character.Id,
                        GaugeDefinitionId = definition.Id,
                        Value = finalValue
                    });
                }
                else
                {
                    existingValue.Value = finalValue;
                }
            }

            if (wouldBeAlive)
            {
                row.character.IsAlive = true;
                row.character.DiedAtUtc = null;
            }
            else
            {
                if (row.character.IsAlive)
                    row.character.DiedAtUtc = DateTime.UtcNow;

                row.character.IsAlive = false;
            }

            await context.SaveChangesAsync();

            return new CharacterUpdateResultDto
            {
                ResurrectionBlocked = resurrectionBlocked
            };
        }

        private async Task<SessionPublicStatsDto> BuildSessionStatsAsync(RollocracyDbContext context, Guid sessionId)
        {
            var playerSessionIds = await context.PlayerSessions
                .AsNoTracking()
                .Where(ps => ps.SessionId == sessionId)
                .Select(ps => ps.Id)
                .ToListAsync();

            var aliveCharactersCount = await context.Characters
                .AsNoTracking()
                .CountAsync(c => playerSessionIds.Contains(c.PlayerSessionId) && c.IsAlive);

            var totalCharactersCount = await context.Characters
                .AsNoTracking()
                .CountAsync(c => playerSessionIds.Contains(c.PlayerSessionId));

            return new SessionPublicStatsDto
            {
                ConnectedPlayersCount = _presenceTracker.GetConnectedPlayersCount(sessionId),
                AliveCharactersCount = aliveCharactersCount,
                TotalCharactersCount = totalCharactersCount
            };
        }

        private async Task<CharacterSheetDto?> BuildCharacterSheetAsync(
            RollocracyDbContext context,
            Guid playerSessionId,
            Guid characterId)
        {
            var character = await context.Characters
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == characterId && c.PlayerSessionId == playerSessionId);

            if (character == null)
                return null;

            var playerSession = await context.PlayerSessions
                .AsNoTracking()
                .FirstOrDefaultAsync(ps => ps.Id == playerSessionId);

            if (playerSession == null)
                return null;

            var session = await context.Sessions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == playerSession.SessionId);

            if (session == null || !session.GameSystemId.HasValue)
                return null;

            var computed = await ComputeCharacterContextAsync(context, session.GameSystemId.Value, character.Id);

            return new CharacterSheetDto
            {
                CharacterId = character.Id,
                Name = character.Name,
                Biography = character.Biography,
                IsAlive = character.IsAlive,
                DiedAtUtc = character.DiedAtUtc,
                Attributes = computed.AttributeLines,
                DerivedStats = computed.DerivedStatLines,
                Metrics = computed.MetricLines,
                Traits = computed.TraitLines,
                Gauges = computed.GaugeLines,
                Talents = computed.TalentLines,
                Items = computed.ItemLines
            };
        }

        private async Task<EditableCharacterDto> BuildEditableCharacterAsync(
            RollocracyDbContext context,
            Session session,
            PlayerSession playerSession,
            Character character)
        {
            var gameSystemId = session.GameSystemId!.Value;

            var attributeDefinitions = await context.AttributeDefinitions
                .AsNoTracking()
                .Where(a => a.GameSystemId == gameSystemId)
                .OrderBy(a => a.Name)
                .ToListAsync();

            var gaugeDefinitions = await context.GaugeDefinitions
                .AsNoTracking()
                .Where(g => g.GameSystemId == gameSystemId)
                .OrderBy(g => g.Name)
                .ToListAsync();

            var traitDefinitions = await context.TraitDefinitions
                .AsNoTracking()
                .Where(t => t.GameSystemId == gameSystemId)
                .OrderBy(t => t.Name)
                .ToListAsync();

            var traitDefinitionIds = traitDefinitions.Select(t => t.Id).ToList();

            var traitOptions = await context.TraitOptions
                .AsNoTracking()
                .Where(o => traitDefinitionIds.Contains(o.TraitDefinitionId))
                .OrderBy(o => o.Name)
                .ToListAsync();

            var attributeValues = await context.CharacterAttributeValues
                .AsNoTracking()
                .Where(v => v.CharacterId == character.Id)
                .ToListAsync();

            var gaugeValues = await context.CharacterGaugeValues
                .AsNoTracking()
                .Where(v => v.CharacterId == character.Id)
                .ToListAsync();

            var traitValues = await context.CharacterTraitValues
                .AsNoTracking()
                .Where(v => v.CharacterId == character.Id)
                .ToListAsync();

            var talentDefinitions = await context.TalentDefinitions
                .AsNoTracking()
                .Where(t => t.GameSystemId == gameSystemId)
                .OrderBy(t => t.DisplayOrder).ThenBy(t => t.Name)
                .ToListAsync();

            var itemDefinitions = await context.ItemDefinitions
                .AsNoTracking()
                .Where(i => i.GameSystemId == gameSystemId)
                .OrderBy(i => i.DisplayOrder).ThenBy(i => i.Name)
                .ToListAsync();

            var characterTalentIds = await context.CharacterTalents
                .AsNoTracking()
                .Where(x => x.CharacterId == character.Id)
                .Select(x => x.TalentDefinitionId)
                .ToListAsync();

            var characterItemIds = await context.CharacterItems
                .AsNoTracking()
                .Where(x => x.CharacterId == character.Id)
                .Select(x => x.ItemDefinitionId)
                .ToListAsync();

            var computed = await ComputeCharacterContextAsync(context, gameSystemId, character.Id);

            return new EditableCharacterDto
            {
                CharacterId = character.Id,
                PlayerSessionId = playerSession.Id,
                PlayerName = playerSession.PlayerName,
                Name = character.Name,
                Biography = character.Biography,
                IsAlive = character.IsAlive,
                DiedAtUtc = character.DiedAtUtc,
                Attributes = attributeDefinitions
                    .Select(definition =>
                    {
                        var value = attributeValues.FirstOrDefault(v => v.AttributeDefinitionId == definition.Id)?.Value
                            ?? definition.DefaultValue;

                        return new EditableCharacterAttributeDto
                        {
                            AttributeDefinitionId = definition.Id,
                            Name = definition.Name,
                            MinValue = definition.MinValue,
                            MaxValue = definition.MaxValue,
                            Value = value
                        };
                    })
                    .ToList(),
                DerivedStats = computed.DerivedStatLines,
                Metrics = computed.MetricLines,
                Gauges = gaugeDefinitions
                    .Select(definition =>
                    {
                        var value = gaugeValues.FirstOrDefault(v => v.GaugeDefinitionId == definition.Id)?.Value
                            ?? definition.DefaultValue;

                        return new EditableCharacterGaugeDto
                        {
                            GaugeDefinitionId = definition.Id,
                            Name = definition.Name,
                            MinValue = definition.MinValue,
                            MaxValue = definition.MaxValue,
                            Value = value,
                            IsHealthGauge = definition.IsHealthGauge
                        };
                    })
                    .ToList(),
                Traits = traitDefinitions
                    .Select(definition =>
                    {
                        var selectedOptionId = traitValues
                            .FirstOrDefault(v => v.TraitDefinitionId == definition.Id)?.TraitOptionId;

                        return new EditableCharacterTraitDto
                        {
                            TraitDefinitionId = definition.Id,
                            TraitName = definition.Name,
                            SelectedOptionId = selectedOptionId,
                            Options = traitOptions
                                .Where(o => o.TraitDefinitionId == definition.Id)
                                .Select(o => new EditableCharacterTraitOptionDto
                                {
                                    TraitOptionId = o.Id,
                                    Name = o.Name
                                })
                                .ToList()
                        };
                    })
                    .ToList(),
                Talents = talentDefinitions
                    .Select(t => new EditableCharacterGrantDto
                    {
                        DefinitionId = t.Id,
                        Name = t.Name,
                        IsSelected = characterTalentIds.Contains(t.Id)
                    })
                    .ToList(),
                Items = itemDefinitions
                    .Select(i => new EditableCharacterGrantDto
                    {
                        DefinitionId = i.Id,
                        Name = i.Name,
                        IsSelected = characterItemIds.Contains(i.Id)
                    })
                    .ToList()
            };
        }

        private async Task<ComputedCharacterContext> ComputeCharacterContextAsync(
            RollocracyDbContext context,
            Guid gameSystemId,
            Guid characterId)
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

            var gaugeLines = await context.CharacterGaugeValues
                .AsNoTracking()
                .Where(v => v.CharacterId == characterId)
                .Join(
                    context.GaugeDefinitions,
                    value => value.GaugeDefinitionId,
                    definition => definition.Id,
                    (value, definition) => new CharacterGaugeLineDto
                    {
                        Name = definition.Name,
                        Value = value.Value,
                        MinValue = definition.MinValue,
                        MaxValue = definition.MaxValue,
                        IsHealthGauge = definition.IsHealthGauge
                    })
                .OrderBy(x => x.Name)
                .ToListAsync();

            var traitValues = await context.CharacterTraitValues
                .AsNoTracking()
                .Where(v => v.CharacterId == characterId)
                .ToListAsync();

            var traitLines = await context.CharacterTraitValues
                .AsNoTracking()
                .Where(v => v.CharacterId == characterId)
                .Join(
                    context.TraitDefinitions,
                    value => value.TraitDefinitionId,
                    definition => definition.Id,
                    (value, definition) => new { value, definition })
                .Join(
                    context.TraitOptions,
                    left => left.value.TraitOptionId,
                    option => option.Id,
                    (left, option) => new CharacterTraitLineDto
                    {
                        TraitName = left.definition.Name,
                        OptionName = option.Name
                    })
                .OrderBy(x => x.TraitName)
                .ToListAsync();

            var traitOptionIds = traitValues.Select(v => v.TraitOptionId).Distinct().ToList();

            var choiceOptionModifiers = await context.Set<ChoiceOptionModifierDefinition>()
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

            var talentLines = await context.TalentDefinitions
                .AsNoTracking()
                .Where(t => characterTalentIds.Contains(t.Id))
                .OrderBy(t => t.DisplayOrder).ThenBy(t => t.Name)
                .Select(t => new CharacterNameLineDto
                {
                    Id = t.Id,
                    Name = t.Name
                })
                .ToListAsync();

            var itemLines = await context.ItemDefinitions
                .AsNoTracking()
                .Where(i => characterItemIds.Contains(i.Id))
                .OrderBy(i => i.DisplayOrder).ThenBy(i => i.Name)
                .Select(i => new CharacterNameLineDto
                {
                    Id = i.Id,
                    Name = i.Name
                })
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

            var allModifiers = choiceOptionModifiers
                .Select(m => new RuntimeModifier
                {
                    TargetType = m.TargetType,
                    TargetId = m.TargetId,
                    AddValue = m.AddValue
                })
                .Concat(talentModifiers.Select(m => new RuntimeModifier
                {
                    TargetType = m.TargetType,
                    TargetId = m.TargetId,
                    AddValue = m.AddValue
                }))
                .Concat(itemModifiers.Select(m => new RuntimeModifier
                {
                    TargetType = m.TargetType,
                    TargetId = m.TargetId,
                    AddValue = m.AddValue
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

            var attributeLines = attributeDefinitions
                .Select(definition => new CharacterAttributeLineDto
                {
                    Name = definition.Name,
                    Value = effectiveAttributeValues[definition.Id]
                })
                .ToList();

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

                value += allModifiers
                    .Where(m => m.TargetType == ModifierTargetType.DerivedStat && m.TargetId == definition.Id)
                    .Sum(m => m.AddValue);

                value = Math.Clamp(value, definition.MinValue, definition.MaxValue);
                derivedStatValues[definition.Id] = value;
            }

            var derivedLines = derivedDefinitions
                .Select(definition => new CharacterDerivedStatLineDto
                {
                    Name = definition.Name,
                    Value = derivedStatValues[definition.Id]
                })
                .ToList();

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

            var metricValues = new Dictionary<Guid, int>();

            foreach (var definition in metricDefinitions)
            {
                var value = ComputeWeightedValue(
                    metricComponents.Where(c => c.MetricDefinitionId == definition.Id)
                        .Select(c => new WeightedSourceValue
                        {
                            Weight = c.Weight,
                            Value = effectiveAttributeValues.TryGetValue(c.AttributeDefinitionId, out var sourceValue) ? sourceValue : 0
                        })
                        .ToList(),
                    definition.BaseValue,
                    definition.MinValue,
                    definition.MaxValue,
                    definition.RoundMode);

                value += allModifiers
                    .Where(m => m.TargetType == ModifierTargetType.Metric && m.TargetId == definition.Id)
                    .Sum(m => m.AddValue);

                value = Math.Clamp(value, definition.MinValue, definition.MaxValue);
                metricValues[definition.Id] = value;
            }

            var metricLines = metricDefinitions
                .Select(definition => new CharacterMetricLineDto
                {
                    Name = definition.Name,
                    Value = metricValues[definition.Id]
                })
                .ToList();

            return new ComputedCharacterContext
            {
                AttributeLines = attributeLines,
                DerivedStatLines = derivedLines,
                MetricLines = metricLines,
                TraitLines = traitLines,
                GaugeLines = gaugeLines,
                TalentLines = talentLines,
                ItemLines = itemLines,
                MetricValues = metricValues
            };
        }

        private static int GenerateAttributeDefaultValue(AttributeDefinition definition)
        {
            if (definition.DefaultValueMode == BaseValueGenerationMode.Fixed)
                return definition.DefaultValue;

            var total = definition.DefaultValueFlatBonus;

            for (var i = 0; i < definition.DefaultValueDiceCount; i++)
            {
                total += Random.Shared.Next(1, definition.DefaultValueDiceSides + 1);
            }

            return Math.Clamp(total, definition.MinValue, definition.MaxValue);
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

        private sealed class RuntimeModifier
        {
            public ModifierTargetType TargetType { get; set; }
            public Guid TargetId { get; set; }
            public int AddValue { get; set; }
        }

        private sealed class WeightedSourceValue
        {
            public int Weight { get; set; }
            public int Value { get; set; }
        }

        private sealed class ComputedCharacterContext
        {
            public List<CharacterAttributeLineDto> AttributeLines { get; set; } = new();
            public List<CharacterDerivedStatLineDto> DerivedStatLines { get; set; } = new();
            public List<CharacterMetricLineDto> MetricLines { get; set; } = new();
            public List<CharacterTraitLineDto> TraitLines { get; set; } = new();
            public List<CharacterGaugeLineDto> GaugeLines { get; set; } = new();
            public List<CharacterNameLineDto> TalentLines { get; set; } = new();
            public List<CharacterNameLineDto> ItemLines { get; set; } = new();
            public Dictionary<Guid, int> MetricValues { get; set; } = new();
        }
    }
}
