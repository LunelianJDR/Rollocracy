using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Rollocracy.Domain.Characters;
using Rollocracy.Domain.Entities;
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
                    DefaultValue = a.DefaultValue
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
                var value = attributeDefinition.DefaultValue;

                if (attributeValues.TryGetValue(attributeDefinition.Id, out var submittedValue))
                {
                    value = submittedValue;
                }

                if (value < attributeDefinition.MinValue)
                    value = attributeDefinition.MinValue;

                if (value > attributeDefinition.MaxValue)
                    value = attributeDefinition.MaxValue;

                context.CharacterAttributeValues.Add(new Domain.GameRules.CharacterAttributeValue
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

                context.CharacterTraitValues.Add(new Domain.GameRules.CharacterTraitValue
                {
                    Id = Guid.NewGuid(),
                    CharacterId = character.Id,
                    TraitDefinitionId = traitDefinition.Id,
                    TraitOptionId = selectedOptionId
                });
            }

            foreach (var gaugeDefinition in gaugeDefinitions)
            {
                context.CharacterGaugeValues.Add(new Domain.GameRules.CharacterGaugeValue
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
                    Traits = sheet.Traits,
                    Gauges = sheet.Gauges
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
                .FirstOrDefaultAsync(x => x.character.Id == characterId && x.playerSession.SessionId == sessionId);

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

            var attributeValues = await context.CharacterAttributeValues
                .Where(v => v.CharacterId == row.character.Id)
                .ToListAsync();

            var gaugeValues = await context.CharacterGaugeValues
                .Where(v => v.CharacterId == row.character.Id)
                .ToListAsync();

            var traitValues = await context.CharacterTraitValues
                .Where(v => v.CharacterId == row.character.Id)
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
                    context.CharacterAttributeValues.Add(new Rollocracy.Domain.GameRules.CharacterAttributeValue
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
                    {
                        context.CharacterTraitValues.Remove(existingValue);
                    }

                    continue;
                }

                var optionExists = traitOptions.Any(o =>
                    o.Id == requestedTrait.SelectedOptionId.Value &&
                    o.TraitDefinitionId == definition.Id);

                if (!optionExists)
                    throw new Exception(_localizer["Backend_InvalidTraitSelection"]);

                if (existingValue == null)
                {
                    context.CharacterTraitValues.Add(new Rollocracy.Domain.GameRules.CharacterTraitValue
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

            var healthGauges = gaugeDefinitions
                .Where(g => g.IsHealthGauge)
                .ToList();

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
                    context.CharacterGaugeValues.Add(new Rollocracy.Domain.GameRules.CharacterGaugeValue
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
                {
                    row.character.DiedAtUtc = DateTime.UtcNow;
                }

                row.character.IsAlive = false;
            }

            await context.SaveChangesAsync();

            return new CharacterUpdateResultDto
            {
                ResurrectionBlocked = resurrectionBlocked
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
                    .ToList()
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

            var attributeLines = await context.CharacterAttributeValues
                .AsNoTracking()
                .Where(v => v.CharacterId == character.Id)
                .Join(
                    context.AttributeDefinitions,
                    value => value.AttributeDefinitionId,
                    definition => definition.Id,
                    (value, definition) => new CharacterAttributeLineDto
                    {
                        Name = definition.Name,
                        Value = value.Value
                    })
                .OrderBy(x => x.Name)
                .ToListAsync();

            var traitLines = await context.CharacterTraitValues
                .AsNoTracking()
                .Where(v => v.CharacterId == character.Id)
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

            var gaugeLines = await context.CharacterGaugeValues
                .AsNoTracking()
                .Where(v => v.CharacterId == character.Id)
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

            return new CharacterSheetDto
            {
                CharacterId = character.Id,
                Name = character.Name,
                Biography = character.Biography,
                IsAlive = character.IsAlive,
                DiedAtUtc = character.DiedAtUtc,
                Attributes = attributeLines,
                Traits = traitLines,
                Gauges = gaugeLines
            };
        }
    }
}