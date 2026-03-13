using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Rollocracy.Domain.Entities;
using Rollocracy.Domain.GameRules;
using Rollocracy.Domain.Interfaces;
using Rollocracy.Infrastructure.Persistence;

namespace Rollocracy.Infrastructure.Services
{
    public class GameSystemService : IGameSystemService
    {
        private readonly IDbContextFactory<RollocracyDbContext> _contextFactory;
        private readonly IStringLocalizer _localizer;

        public GameSystemService(
            IDbContextFactory<RollocracyDbContext> contextFactory,
            IStringLocalizerFactory localizerFactory)
        {
            _contextFactory = contextFactory;
            _localizer = localizerFactory.Create("Rollocracy.Localization.SharedTexts", "Rollocracy");
        }

        public async Task<List<GameSystem>> GetGameSystemsByOwnerAsync(Guid ownerUserAccountId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            return await context.GameSystems
                .AsNoTracking()
                .Where(gs => gs.OwnerUserAccountId == ownerUserAccountId && gs.LockedToSessionId == null)
                .OrderBy(gs => gs.Name)
                .ToListAsync();
        }

        public async Task<GameSystem?> GetGameSystemByIdAsync(Guid gameSystemId, Guid ownerUserAccountId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            return await context.GameSystems
                .AsNoTracking()
                .FirstOrDefaultAsync(gs => gs.Id == gameSystemId && gs.OwnerUserAccountId == ownerUserAccountId);
        }

        public async Task<GameSystem> CreateGameSystemAsync(
            Guid ownerUserAccountId,
            string name,
            string description,
            TestResolutionMode testResolutionMode)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            await EnsureUserCanManageGameSystemsAsync(context, ownerUserAccountId);

            var system = new GameSystem
            {
                Id = Guid.NewGuid(),
                OwnerUserAccountId = ownerUserAccountId,
                Name = name.Trim(),
                Description = description.Trim(),
                TestResolutionMode = testResolutionMode,
                SourceGameSystemId = null,
                LockedToSessionId = null
            };

            context.GameSystems.Add(system);

            context.GaugeDefinitions.Add(new GaugeDefinition
            {
                Id = Guid.NewGuid(),
                GameSystemId = system.Id,
                Name = "Vie",
                MinValue = 0,
                MaxValue = 10,
                DefaultValue = 10,
                IsHealthGauge = true
            });

            await context.SaveChangesAsync();

            return system;
        }

        public async Task UpdateGameSystemAsync(
            Guid gameSystemId,
            Guid ownerUserAccountId,
            string name,
            string description,
            TestResolutionMode testResolutionMode)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            await EnsureUserCanManageGameSystemsAsync(context, ownerUserAccountId);

            var system = await context.GameSystems
                .FirstOrDefaultAsync(gs => gs.Id == gameSystemId && gs.OwnerUserAccountId == ownerUserAccountId);

            if (system == null)
                throw new Exception(_localizer["Backend_GameSystemNotFound"]);

            system.Name = name.Trim();
            system.Description = description.Trim();
            system.TestResolutionMode = testResolutionMode;

            await context.SaveChangesAsync();
        }

        public async Task<AttributeDefinition> AddAttributeDefinitionAsync(
            Guid gameSystemId,
            Guid ownerUserAccountId,
            string name,
            int minValue,
            int maxValue,
            int defaultValue)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            await EnsureUserCanManageGameSystemsAsync(context, ownerUserAccountId);

            var system = await context.GameSystems
                .AsNoTracking()
                .FirstOrDefaultAsync(gs => gs.Id == gameSystemId && gs.OwnerUserAccountId == ownerUserAccountId);

            if (system == null)
                throw new Exception(_localizer["Backend_GameSystemNotFound"]);

            var attribute = new AttributeDefinition
            {
                Id = Guid.NewGuid(),
                GameSystemId = gameSystemId,
                Name = name.Trim(),
                MinValue = minValue,
                MaxValue = maxValue,
                DefaultValue = defaultValue
            };

            context.AttributeDefinitions.Add(attribute);
            await context.SaveChangesAsync();

            return attribute;
        }

        public async Task<List<AttributeDefinition>> GetAttributeDefinitionsAsync(Guid gameSystemId, Guid ownerUserAccountId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var ownsSystem = await context.GameSystems
                .AsNoTracking()
                .AnyAsync(gs => gs.Id == gameSystemId && gs.OwnerUserAccountId == ownerUserAccountId);

            if (!ownsSystem)
                throw new Exception(_localizer["Backend_GameSystemNotFound"]);

            return await context.AttributeDefinitions
                .AsNoTracking()
                .Where(a => a.GameSystemId == gameSystemId)
                .OrderBy(a => a.Name)
                .ToListAsync();
        }

        public async Task<TraitDefinition> AddTraitDefinitionAsync(
            Guid gameSystemId,
            Guid ownerUserAccountId,
            string name)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            await EnsureUserCanManageGameSystemsAsync(context, ownerUserAccountId);

            var system = await context.GameSystems
                .AsNoTracking()
                .FirstOrDefaultAsync(gs => gs.Id == gameSystemId && gs.OwnerUserAccountId == ownerUserAccountId);

            if (system == null)
                throw new Exception(_localizer["Backend_GameSystemNotFound"]);

            var traitDefinition = new TraitDefinition
            {
                Id = Guid.NewGuid(),
                GameSystemId = gameSystemId,
                Name = name.Trim()
            };

            context.TraitDefinitions.Add(traitDefinition);
            await context.SaveChangesAsync();

            return traitDefinition;
        }

        public async Task<List<TraitDefinition>> GetTraitDefinitionsAsync(Guid gameSystemId, Guid ownerUserAccountId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var ownsSystem = await context.GameSystems
                .AsNoTracking()
                .AnyAsync(gs => gs.Id == gameSystemId && gs.OwnerUserAccountId == ownerUserAccountId);

            if (!ownsSystem)
                throw new Exception(_localizer["Backend_GameSystemNotFound"]);

            return await context.TraitDefinitions
                .AsNoTracking()
                .Where(t => t.GameSystemId == gameSystemId)
                .OrderBy(t => t.Name)
                .ToListAsync();
        }

        public async Task<TraitOption> AddTraitOptionAsync(
            Guid traitDefinitionId,
            Guid ownerUserAccountId,
            string name)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            await EnsureUserCanManageGameSystemsAsync(context, ownerUserAccountId);

            var traitDefinition = await context.TraitDefinitions
                .AsNoTracking()
                .FirstOrDefaultAsync(td => td.Id == traitDefinitionId);

            if (traitDefinition == null)
                throw new Exception(_localizer["Backend_TraitDefinitionNotFound"]);

            var ownsSystem = await context.GameSystems
                .AsNoTracking()
                .AnyAsync(gs => gs.Id == traitDefinition.GameSystemId && gs.OwnerUserAccountId == ownerUserAccountId);

            if (!ownsSystem)
                throw new Exception(_localizer["Backend_GameSystemAccessDenied"]);

            var option = new TraitOption
            {
                Id = Guid.NewGuid(),
                TraitDefinitionId = traitDefinitionId,
                Name = name.Trim()
            };

            context.TraitOptions.Add(option);
            await context.SaveChangesAsync();

            return option;
        }

        public async Task<List<TraitOption>> GetTraitOptionsAsync(Guid traitDefinitionId, Guid ownerUserAccountId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var traitDefinition = await context.TraitDefinitions
                .AsNoTracking()
                .FirstOrDefaultAsync(td => td.Id == traitDefinitionId);

            if (traitDefinition == null)
                throw new Exception(_localizer["Backend_TraitDefinitionNotFound"]);

            var ownsSystem = await context.GameSystems
                .AsNoTracking()
                .AnyAsync(gs => gs.Id == traitDefinition.GameSystemId && gs.OwnerUserAccountId == ownerUserAccountId);

            if (!ownsSystem)
                throw new Exception(_localizer["Backend_GameSystemAccessDenied"]);

            return await context.TraitOptions
                .AsNoTracking()
                .Where(o => o.TraitDefinitionId == traitDefinitionId)
                .OrderBy(o => o.Name)
                .ToListAsync();
        }

        public async Task<List<GaugeDefinition>> GetGaugeDefinitionsAsync(Guid gameSystemId, Guid ownerUserAccountId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var ownsSystem = await context.GameSystems
                .AsNoTracking()
                .AnyAsync(gs => gs.Id == gameSystemId && gs.OwnerUserAccountId == ownerUserAccountId);

            if (!ownsSystem)
                throw new Exception(_localizer["Backend_GameSystemNotFound"]);

            return await context.GaugeDefinitions
                .AsNoTracking()
                .Where(g => g.GameSystemId == gameSystemId)
                .OrderBy(g => g.Name)
                .ToListAsync();
        }

        public async Task<GaugeDefinition> AddGaugeDefinitionAsync(
            Guid gameSystemId,
            Guid ownerUserAccountId,
            string name,
            int minValue,
            int maxValue,
            int defaultValue,
            bool isHealthGauge)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            await EnsureUserCanManageGameSystemsAsync(context, ownerUserAccountId);

            var system = await context.GameSystems
                .AsNoTracking()
                .FirstOrDefaultAsync(gs => gs.Id == gameSystemId && gs.OwnerUserAccountId == ownerUserAccountId);

            if (system == null)
                throw new Exception(_localizer["Backend_GameSystemNotFound"]);

            var gauge = new GaugeDefinition
            {
                Id = Guid.NewGuid(),
                GameSystemId = gameSystemId,
                Name = name.Trim(),
                MinValue = minValue,
                MaxValue = maxValue,
                DefaultValue = defaultValue,
                IsHealthGauge = isHealthGauge
            };

            context.GaugeDefinitions.Add(gauge);
            await context.SaveChangesAsync();

            return gauge;
        }

        public async Task<GameSystem> CloneGameSystemForSessionAsync(
            Guid sourceGameSystemId,
            Guid ownerUserAccountId,
            Guid sessionId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            await EnsureUserCanManageGameSystemsAsync(context, ownerUserAccountId);

            var sourceSystem = await context.GameSystems
                .AsNoTracking()
                .FirstOrDefaultAsync(gs => gs.Id == sourceGameSystemId && gs.OwnerUserAccountId == ownerUserAccountId);

            if (sourceSystem == null)
                throw new Exception(_localizer["Backend_SourceGameSystemNotFound"]);

            var session = await context.Sessions
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.GameMasterUserAccountId == ownerUserAccountId);

            if (session == null)
                throw new Exception(_localizer["Backend_SessionNotFound"]);

            var clonedSystem = new GameSystem
            {
                Id = Guid.NewGuid(),
                OwnerUserAccountId = sourceSystem.OwnerUserAccountId,
                Name = $"{sourceSystem.Name} (copie session)",
                Description = sourceSystem.Description,
                TestResolutionMode = sourceSystem.TestResolutionMode,
                SourceGameSystemId = sourceSystem.Id,
                LockedToSessionId = sessionId
            };

            context.GameSystems.Add(clonedSystem);

            var attributeMap = new Dictionary<Guid, Guid>();
            var traitMap = new Dictionary<Guid, Guid>();
            var traitOptionMap = new Dictionary<Guid, Guid>();
            var gaugeMap = new Dictionary<Guid, Guid>();
            var derivedStatMap = new Dictionary<Guid, Guid>();

            var sourceAttributes = await context.AttributeDefinitions
                .AsNoTracking()
                .Where(a => a.GameSystemId == sourceSystem.Id)
                .ToListAsync();

            foreach (var attribute in sourceAttributes)
            {
                var clonedAttributeId = Guid.NewGuid();

                context.AttributeDefinitions.Add(new AttributeDefinition
                {
                    Id = clonedAttributeId,
                    GameSystemId = clonedSystem.Id,
                    Name = attribute.Name,
                    MinValue = attribute.MinValue,
                    MaxValue = attribute.MaxValue,
                    DefaultValue = attribute.DefaultValue,
                    DefaultValueMode = attribute.DefaultValueMode,
                    DefaultValueDiceCount = attribute.DefaultValueDiceCount,
                    DefaultValueDiceSides = attribute.DefaultValueDiceSides,
                    DefaultValueFlatBonus = attribute.DefaultValueFlatBonus
                });

                attributeMap[attribute.Id] = clonedAttributeId;
            }

            var sourceTraits = await context.TraitDefinitions
                .AsNoTracking()
                .Where(t => t.GameSystemId == sourceSystem.Id)
                .ToListAsync();

            foreach (var sourceTrait in sourceTraits)
            {
                var clonedTraitId = Guid.NewGuid();

                context.TraitDefinitions.Add(new TraitDefinition
                {
                    Id = clonedTraitId,
                    GameSystemId = clonedSystem.Id,
                    Name = sourceTrait.Name
                });

                traitMap[sourceTrait.Id] = clonedTraitId;

                var sourceOptions = await context.TraitOptions
                    .AsNoTracking()
                    .Where(o => o.TraitDefinitionId == sourceTrait.Id)
                    .ToListAsync();

                foreach (var option in sourceOptions)
                {
                    var clonedOptionId = Guid.NewGuid();

                    context.TraitOptions.Add(new TraitOption
                    {
                        Id = clonedOptionId,
                        TraitDefinitionId = clonedTraitId,
                        Name = option.Name
                    });

                    traitOptionMap[option.Id] = clonedOptionId;
                }
            }

            var sourceGauges = await context.GaugeDefinitions
                .AsNoTracking()
                .Where(g => g.GameSystemId == sourceSystem.Id)
                .ToListAsync();

            foreach (var gauge in sourceGauges)
            {
                var clonedGaugeId = Guid.NewGuid();

                context.GaugeDefinitions.Add(new GaugeDefinition
                {
                    Id = clonedGaugeId,
                    GameSystemId = clonedSystem.Id,
                    Name = gauge.Name,
                    MinValue = gauge.MinValue,
                    MaxValue = gauge.MaxValue,
                    DefaultValue = gauge.DefaultValue,
                    IsHealthGauge = gauge.IsHealthGauge
                });

                gaugeMap[gauge.Id] = clonedGaugeId;
            }

            var sourceDerivedStats = await context.DerivedStatDefinitions
                .AsNoTracking()
                .Where(d => d.GameSystemId == sourceSystem.Id)
                .ToListAsync();

            foreach (var sourceDerivedStat in sourceDerivedStats)
            {
                var clonedDerivedStatId = Guid.NewGuid();

                context.DerivedStatDefinitions.Add(new DerivedStatDefinition
                {
                    Id = clonedDerivedStatId,
                    GameSystemId = clonedSystem.Id,
                    Name = sourceDerivedStat.Name,
                    MinValue = sourceDerivedStat.MinValue,
                    MaxValue = sourceDerivedStat.MaxValue,
                    RoundMode = sourceDerivedStat.RoundMode,
                    DisplayOrder = sourceDerivedStat.DisplayOrder
                });

                derivedStatMap[sourceDerivedStat.Id] = clonedDerivedStatId;
            }

            await context.SaveChangesAsync();

            var sourceDerivedComponents = await context.DerivedStatComponents
                .AsNoTracking()
                .Where(c => derivedStatMap.Keys.Contains(c.DerivedStatDefinitionId))
                .ToListAsync();

            foreach (var component in sourceDerivedComponents)
            {
                context.DerivedStatComponents.Add(new DerivedStatComponent
                {
                    Id = Guid.NewGuid(),
                    DerivedStatDefinitionId = derivedStatMap[component.DerivedStatDefinitionId],
                    AttributeDefinitionId = attributeMap[component.AttributeDefinitionId],
                    Weight = component.Weight
                });
            }

            await context.SaveChangesAsync();

            var playerSessionIds = await context.PlayerSessions
                .Where(ps => ps.SessionId == sessionId)
                .Select(ps => ps.Id)
                .ToListAsync();

            var characterIds = await context.Characters
                .Where(c => playerSessionIds.Contains(c.PlayerSessionId))
                .Select(c => c.Id)
                .ToListAsync();

            if (characterIds.Count > 0)
            {
                var attributeValues = await context.CharacterAttributeValues
                    .Where(v => characterIds.Contains(v.CharacterId) && attributeMap.Keys.Contains(v.AttributeDefinitionId))
                    .ToListAsync();

                foreach (var value in attributeValues)
                    value.AttributeDefinitionId = attributeMap[value.AttributeDefinitionId];

                var traitValues = await context.CharacterTraitValues
                    .Where(v => characterIds.Contains(v.CharacterId) && traitMap.Keys.Contains(v.TraitDefinitionId))
                    .ToListAsync();

                foreach (var value in traitValues)
                {
                    value.TraitDefinitionId = traitMap[value.TraitDefinitionId];

                    if (traitOptionMap.TryGetValue(value.TraitOptionId, out var clonedOptionId))
                        value.TraitOptionId = clonedOptionId;
                }

                var gaugeValues = await context.CharacterGaugeValues
                    .Where(v => characterIds.Contains(v.CharacterId) && gaugeMap.Keys.Contains(v.GaugeDefinitionId))
                    .ToListAsync();

                foreach (var value in gaugeValues)
                    value.GaugeDefinitionId = gaugeMap[value.GaugeDefinitionId];
            }

            session.GameSystemId = clonedSystem.Id;

            await context.SaveChangesAsync();

            return clonedSystem;
        }

        public async Task<GameSystemEditorDto?> GetGameSystemEditorAsync(Guid gameSystemId, Guid ownerUserAccountId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            await EnsureUserCanManageGameSystemsAsync(context, ownerUserAccountId);

            var system = await context.GameSystems
                .AsNoTracking()
                .FirstOrDefaultAsync(gs => gs.Id == gameSystemId && gs.OwnerUserAccountId == ownerUserAccountId);

            if (system == null)
                return null;

            var attributes = await context.AttributeDefinitions
                .AsNoTracking()
                .Where(x => x.GameSystemId == system.Id)
                .OrderBy(x => x.Name)
                .ToListAsync();

            var derivedStats = await context.DerivedStatDefinitions
                .AsNoTracking()
                .Where(x => x.GameSystemId == system.Id)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Name)
                .ToListAsync();

            var derivedStatIds = derivedStats.Select(x => x.Id).ToList();

            var derivedComponents = await context.DerivedStatComponents
                .AsNoTracking()
                .Where(x => derivedStatIds.Contains(x.DerivedStatDefinitionId))
                .ToListAsync();

            var traits = await context.TraitDefinitions
                .AsNoTracking()
                .Where(x => x.GameSystemId == system.Id)
                .OrderBy(x => x.Name)
                .ToListAsync();

            var traitIds = traits.Select(x => x.Id).ToList();

            var options = await context.TraitOptions
                .AsNoTracking()
                .Where(x => traitIds.Contains(x.TraitDefinitionId))
                .OrderBy(x => x.Name)
                .ToListAsync();

            var gauges = await context.GaugeDefinitions
                .AsNoTracking()
                .Where(x => x.GameSystemId == system.Id)
                .OrderBy(x => x.Name)
                .ToListAsync();

            var impactedSessions = await GetSessionsUsingSystemAsync(context, system.Id);

            var hasSnapshot = await context.GameSystemSnapshots
                .AsNoTracking()
                .AnyAsync(x => x.GameSystemId == system.Id);

            return new GameSystemEditorDto
            {
                GameSystemId = system.Id,
                Name = system.Name,
                Description = system.Description,
                TestResolutionMode = system.TestResolutionMode,
                IsLockedToSessionCopy = system.LockedToSessionId.HasValue,
                CanUndoLastChange = hasSnapshot,
                ImpactedSessions = impactedSessions.Select(s => new GameSystemImpactSessionDto
                {
                    SessionId = s.Id,
                    SessionName = s.SessionName
                }).ToList(),
                AvailableBaseAttributes = attributes.Select(a => new BaseAttributeReferenceDto
                {
                    AttributeDefinitionId = a.Id,
                    Name = a.Name
                }).ToList(),
                Attributes = attributes.Select(x => new EditableAttributeDefinitionDto
                {
                    AttributeDefinitionId = x.Id,
                    Name = x.Name,
                    MinValue = x.MinValue,
                    MaxValue = x.MaxValue,
                    DefaultValue = x.DefaultValue,
                    DefaultValueMode = x.DefaultValueMode,
                    DefaultValueDiceCount = x.DefaultValueDiceCount,
                    DefaultValueDiceSides = x.DefaultValueDiceSides,
                    DefaultValueFlatBonus = x.DefaultValueFlatBonus
                }).ToList(),
                DerivedStats = derivedStats.Select(d => new EditableDerivedStatDefinitionDto
                {
                    DerivedStatDefinitionId = d.Id,
                    Name = d.Name,
                    MinValue = d.MinValue,
                    MaxValue = d.MaxValue,
                    RoundMode = d.RoundMode,
                    DisplayOrder = d.DisplayOrder,
                    Components = derivedComponents
                        .Where(c => c.DerivedStatDefinitionId == d.Id)
                        .Select(c => new EditableDerivedStatComponentDto
                        {
                            DerivedStatComponentId = c.Id,
                            AttributeDefinitionId = c.AttributeDefinitionId,
                            AttributeName = attributes.FirstOrDefault(a => a.Id == c.AttributeDefinitionId)?.Name ?? string.Empty,
                            Weight = c.Weight
                        })
                        .ToList()
                }).ToList(),
                Traits = traits.Select(t => new EditableTraitDefinitionDto
                {
                    TraitDefinitionId = t.Id,
                    Name = t.Name,
                    Options = options.Where(o => o.TraitDefinitionId == t.Id)
                        .Select(o => new EditableTraitOptionDto
                        {
                            TraitOptionId = o.Id,
                            Name = o.Name
                        })
                        .ToList()
                }).ToList(),
                Gauges = gauges.Select(x => new EditableGaugeDefinitionDto
                {
                    GaugeDefinitionId = x.Id,
                    Name = x.Name,
                    MinValue = x.MinValue,
                    MaxValue = x.MaxValue,
                    DefaultValue = x.DefaultValue,
                    IsHealthGauge = x.IsHealthGauge
                }).ToList()
            };
        }

        public async Task ApplyGameSystemChangesAsync(
            Guid gameSystemId,
            Guid ownerUserAccountId,
            GameSystemApplyChangesRequestDto request)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            await EnsureUserCanManageGameSystemsAsync(context, ownerUserAccountId);

            var system = await context.GameSystems
                .FirstOrDefaultAsync(gs => gs.Id == gameSystemId && gs.OwnerUserAccountId == ownerUserAccountId);

            if (system == null)
                throw new Exception(_localizer["Backend_GameSystemNotFound"]);

            var impactedSessions = await GetSessionsUsingSystemAsync(context, system.Id);

            if (!system.LockedToSessionId.HasValue &&
                impactedSessions.Count > 0 &&
                !request.ConfirmSharedSystemChanges)
            {
                throw new Exception(_localizer["Backend_GameSystemSharedEditConfirmationRequired"]);
            }

            ValidateBaseAttributes(request.Attributes);
            ValidateDerivedStats(request.DerivedStats, request.Attributes);
            ValidateHealthGaugeRule(request.Gauges);

            var affectedCharacters = await GetCharactersUsingSystemAsync(context, system.Id);

            await using var transaction = await context.Database.BeginTransactionAsync();

            var snapshot = await BuildSnapshotAsync(context, system, ownerUserAccountId, affectedCharacters);
            context.GameSystemSnapshots.Add(snapshot);

            system.Name = request.Name.Trim();
            system.Description = request.Description.Trim();
            system.TestResolutionMode = request.TestResolutionMode;

            await SyncAttributesAsync(context, system.Id, affectedCharacters, request.Attributes);
            await SyncDerivedStatsAsync(context, system.Id, request.DerivedStats);
            await SyncTraitsAsync(context, system.Id, request.Traits, affectedCharacters);
            await SyncGaugesAsync(context, system.Id, affectedCharacters, request.Gauges);

            await context.SaveChangesAsync();
            await TrimSnapshotsAsync(context, system.Id);
            await transaction.CommitAsync();
        }

        public async Task UndoLastGameSystemChangeAsync(Guid gameSystemId, Guid ownerUserAccountId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            await EnsureUserCanManageGameSystemsAsync(context, ownerUserAccountId);

            var system = await context.GameSystems
                .FirstOrDefaultAsync(gs => gs.Id == gameSystemId && gs.OwnerUserAccountId == ownerUserAccountId);

            if (system == null)
                throw new Exception(_localizer["Backend_GameSystemNotFound"]);

            var snapshot = await context.GameSystemSnapshots
                .Where(x => x.GameSystemId == gameSystemId)
                .OrderByDescending(x => x.CreatedAtUtc)
                .FirstOrDefaultAsync();

            if (snapshot == null)
                throw new Exception(_localizer["Backend_GameSystemNoSnapshotToUndo"]);

            var payload = JsonSerializer.Deserialize<GameSystemSnapshotPayload>(snapshot.SnapshotJson);

            if (payload == null)
                throw new Exception(_localizer["Backend_GameSystemNoSnapshotToUndo"]);

            await using var transaction = await context.Database.BeginTransactionAsync();

            system.Name = payload.System.Name;
            system.Description = payload.System.Description;
            system.TestResolutionMode = payload.System.TestResolutionMode;
            system.SourceGameSystemId = payload.System.SourceGameSystemId;
            system.LockedToSessionId = payload.System.LockedToSessionId;

            var currentDerivedStats = await context.DerivedStatDefinitions
                .Where(x => x.GameSystemId == system.Id)
                .ToListAsync();

            var currentDerivedStatIds = currentDerivedStats.Select(x => x.Id).ToList();

            var currentDerivedComponents = await context.DerivedStatComponents
                .Where(x => currentDerivedStatIds.Contains(x.DerivedStatDefinitionId))
                .ToListAsync();

            var currentTraits = await context.TraitDefinitions
                .Where(x => x.GameSystemId == system.Id)
                .ToListAsync();

            var currentTraitIds = currentTraits.Select(x => x.Id).ToList();

            var currentOptions = await context.TraitOptions
                .Where(x => currentTraitIds.Contains(x.TraitDefinitionId))
                .ToListAsync();

            var currentAttributes = await context.AttributeDefinitions
                .Where(x => x.GameSystemId == system.Id)
                .ToListAsync();

            var currentGauges = await context.GaugeDefinitions
                .Where(x => x.GameSystemId == system.Id)
                .ToListAsync();

            var currentCharacters = await GetCharactersUsingSystemAsync(context, system.Id);
            var currentCharacterIds = currentCharacters.Select(x => x.Id).ToList();

            var currentAttributeValues = await context.CharacterAttributeValues
                .Where(x => currentCharacterIds.Contains(x.CharacterId))
                .ToListAsync();

            var currentGaugeValues = await context.CharacterGaugeValues
                .Where(x => currentCharacterIds.Contains(x.CharacterId))
                .ToListAsync();

            var currentTraitValues = await context.CharacterTraitValues
                .Where(x => currentCharacterIds.Contains(x.CharacterId))
                .ToListAsync();

            context.CharacterAttributeValues.RemoveRange(currentAttributeValues);
            context.CharacterGaugeValues.RemoveRange(currentGaugeValues);
            context.CharacterTraitValues.RemoveRange(currentTraitValues);

            context.DerivedStatComponents.RemoveRange(currentDerivedComponents);
            context.DerivedStatDefinitions.RemoveRange(currentDerivedStats);
            context.TraitOptions.RemoveRange(currentOptions);
            context.TraitDefinitions.RemoveRange(currentTraits);
            context.AttributeDefinitions.RemoveRange(currentAttributes);
            context.GaugeDefinitions.RemoveRange(currentGauges);

            foreach (var attribute in payload.Attributes)
                context.AttributeDefinitions.Add(attribute);

            foreach (var derivedStat in payload.DerivedStats)
                context.DerivedStatDefinitions.Add(derivedStat);

            foreach (var component in payload.DerivedStatComponents)
                context.DerivedStatComponents.Add(component);

            foreach (var trait in payload.Traits)
                context.TraitDefinitions.Add(trait);

            foreach (var option in payload.TraitOptions)
                context.TraitOptions.Add(option);

            foreach (var gauge in payload.Gauges)
                context.GaugeDefinitions.Add(gauge);

            var currentCharactersById = currentCharacters.ToDictionary(x => x.Id, x => x);

            foreach (var characterSnapshot in payload.Characters)
            {
                if (!currentCharactersById.TryGetValue(characterSnapshot.Id, out var character))
                    continue;

                character.IsAlive = characterSnapshot.IsAlive;
                character.DiedAtUtc = characterSnapshot.DiedAtUtc;
            }

            foreach (var value in payload.AttributeValues.Where(x => currentCharactersById.ContainsKey(x.CharacterId)))
                context.CharacterAttributeValues.Add(value);

            foreach (var value in payload.GaugeValues.Where(x => currentCharactersById.ContainsKey(x.CharacterId)))
                context.CharacterGaugeValues.Add(value);

            foreach (var value in payload.TraitValues.Where(x => currentCharactersById.ContainsKey(x.CharacterId)))
                context.CharacterTraitValues.Add(value);

            await context.SaveChangesAsync();

            var restoredAttributes = await context.AttributeDefinitions
                .AsNoTracking()
                .Where(x => x.GameSystemId == system.Id)
                .ToListAsync();

            var restoredGauges = await context.GaugeDefinitions
                .AsNoTracking()
                .Where(x => x.GameSystemId == system.Id)
                .ToListAsync();

            var restoredAttributeValues = await context.CharacterAttributeValues
                .Where(x => currentCharacterIds.Contains(x.CharacterId))
                .ToListAsync();

            var restoredGaugeValues = await context.CharacterGaugeValues
                .Where(x => currentCharacterIds.Contains(x.CharacterId))
                .ToListAsync();

            foreach (var character in currentCharacters)
            {
                foreach (var attribute in restoredAttributes)
                {
                    var hasValue = restoredAttributeValues.Any(x =>
                        x.CharacterId == character.Id &&
                        x.AttributeDefinitionId == attribute.Id);

                    if (!hasValue)
                    {
                        context.CharacterAttributeValues.Add(new CharacterAttributeValue
                        {
                            Id = Guid.NewGuid(),
                            CharacterId = character.Id,
                            AttributeDefinitionId = attribute.Id,
                            Value = attribute.DefaultValue
                        });
                    }
                }

                foreach (var gauge in restoredGauges)
                {
                    var hasValue = restoredGaugeValues.Any(x =>
                        x.CharacterId == character.Id &&
                        x.GaugeDefinitionId == gauge.Id);

                    if (!hasValue)
                    {
                        context.CharacterGaugeValues.Add(new CharacterGaugeValue
                        {
                            Id = Guid.NewGuid(),
                            CharacterId = character.Id,
                            GaugeDefinitionId = gauge.Id,
                            Value = gauge.DefaultValue
                        });
                    }
                }
            }

            context.GameSystemSnapshots.Remove(snapshot);

            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        private async Task SyncAttributesAsync(
            RollocracyDbContext context,
            Guid gameSystemId,
            List<Character> affectedCharacters,
            List<EditableAttributeDefinitionDto> requestAttributes)
        {
            var currentAttributes = await context.AttributeDefinitions
                .Where(x => x.GameSystemId == gameSystemId)
                .ToListAsync();

            var currentById = currentAttributes.ToDictionary(x => x.Id, x => x);
            var requestExistingIds = requestAttributes
                .Where(x => x.AttributeDefinitionId.HasValue)
                .Select(x => x.AttributeDefinitionId!.Value)
                .ToHashSet();

            var removedIds = requestAttributes
                .Where(x => x.AttributeDefinitionId.HasValue && x.IsDeleted)
                .Select(x => x.AttributeDefinitionId!.Value)
                .Union(currentAttributes.Where(x => !requestExistingIds.Contains(x.Id)).Select(x => x.Id))
                .Distinct()
                .ToList();

            foreach (var item in requestAttributes.Where(x => x.AttributeDefinitionId.HasValue && !x.IsDeleted))
            {
                var entity = currentById[item.AttributeDefinitionId!.Value];
                entity.Name = item.Name.Trim();
                entity.MinValue = item.MinValue;
                entity.MaxValue = item.MaxValue;
                entity.DefaultValue = item.DefaultValue;
                entity.DefaultValueMode = item.DefaultValueMode;
                entity.DefaultValueDiceCount = item.DefaultValueDiceCount;
                entity.DefaultValueDiceSides = item.DefaultValueDiceSides;
                entity.DefaultValueFlatBonus = item.DefaultValueFlatBonus;
            }

            var characterIds = affectedCharacters.Select(x => x.Id).ToList();

            if (removedIds.Count > 0)
            {
                var valuesToDelete = await context.CharacterAttributeValues
                    .Where(x => characterIds.Contains(x.CharacterId) && removedIds.Contains(x.AttributeDefinitionId))
                    .ToListAsync();

                var derivedStatIdsToDelete = await context.DerivedStatComponents
                    .Where(c => removedIds.Contains(c.AttributeDefinitionId))
                    .Select(c => c.DerivedStatDefinitionId)
                    .Distinct()
                    .ToListAsync();

                var derivedComponentsToDelete = await context.DerivedStatComponents
                    .Where(c => derivedStatIdsToDelete.Contains(c.DerivedStatDefinitionId) || removedIds.Contains(c.AttributeDefinitionId))
                    .ToListAsync();

                var derivedDefinitionsToDelete = await context.DerivedStatDefinitions
                    .Where(d => derivedStatIdsToDelete.Contains(d.Id))
                    .ToListAsync();

                context.CharacterAttributeValues.RemoveRange(valuesToDelete);
                context.DerivedStatComponents.RemoveRange(derivedComponentsToDelete);
                context.DerivedStatDefinitions.RemoveRange(derivedDefinitionsToDelete);
                context.AttributeDefinitions.RemoveRange(currentAttributes.Where(x => removedIds.Contains(x.Id)));
            }

            var newDefinitions = new List<AttributeDefinition>();

            foreach (var item in requestAttributes.Where(x => !x.AttributeDefinitionId.HasValue && !x.IsDeleted && !string.IsNullOrWhiteSpace(x.Name)))
            {
                var entity = new AttributeDefinition
                {
                    Id = Guid.NewGuid(),
                    GameSystemId = gameSystemId,
                    Name = item.Name.Trim(),
                    MinValue = item.MinValue,
                    MaxValue = item.MaxValue,
                    DefaultValue = item.DefaultValue,
                    DefaultValueMode = item.DefaultValueMode,
                    DefaultValueDiceCount = item.DefaultValueDiceCount,
                    DefaultValueDiceSides = item.DefaultValueDiceSides,
                    DefaultValueFlatBonus = item.DefaultValueFlatBonus
                };

                context.AttributeDefinitions.Add(entity);
                newDefinitions.Add(entity);
            }

            foreach (var character in affectedCharacters)
            {
                foreach (var definition in newDefinitions)
                {
                    context.CharacterAttributeValues.Add(new CharacterAttributeValue
                    {
                        Id = Guid.NewGuid(),
                        CharacterId = character.Id,
                        AttributeDefinitionId = definition.Id,
                        Value = GenerateAttributeDefaultValue(definition)
                    });
                }
            }

            var remainingAttributes = currentAttributes.Where(x => !removedIds.Contains(x.Id)).ToList();
            remainingAttributes.AddRange(newDefinitions);

            var remainingIds = remainingAttributes.Select(x => x.Id).ToList();

            var valuesToClamp = await context.CharacterAttributeValues
                .Where(x => characterIds.Contains(x.CharacterId) && remainingIds.Contains(x.AttributeDefinitionId))
                .ToListAsync();

            foreach (var value in valuesToClamp)
            {
                var definition = remainingAttributes.First(x => x.Id == value.AttributeDefinitionId);
                value.Value = Math.Clamp(value.Value, definition.MinValue, definition.MaxValue);
            }
        }

        private async Task SyncDerivedStatsAsync(
    RollocracyDbContext context,
    Guid gameSystemId,
    List<EditableDerivedStatDefinitionDto> requestDerivedStats)
        {
            var currentDefinitions = await context.DerivedStatDefinitions
                .Where(x => x.GameSystemId == gameSystemId)
                .ToListAsync();

            var currentDefinitionById = currentDefinitions.ToDictionary(x => x.Id, x => x);
            var currentDefinitionIds = currentDefinitions.Select(x => x.Id).ToList();

            var currentComponents = await context.DerivedStatComponents
                .Where(x => currentDefinitionIds.Contains(x.DerivedStatDefinitionId))
                .ToListAsync();

            var requestExistingIds = requestDerivedStats
                .Where(x => x.DerivedStatDefinitionId.HasValue)
                .Select(x => x.DerivedStatDefinitionId!.Value)
                .ToHashSet();

            var removedIds = requestDerivedStats
                .Where(x => x.DerivedStatDefinitionId.HasValue && x.IsDeleted)
                .Select(x => x.DerivedStatDefinitionId!.Value)
                .Union(currentDefinitions.Where(x => !requestExistingIds.Contains(x.Id)).Select(x => x.Id))
                .Distinct()
                .ToList();

            if (removedIds.Count > 0)
            {
                context.DerivedStatComponents.RemoveRange(
                    currentComponents.Where(x => removedIds.Contains(x.DerivedStatDefinitionId)));

                context.DerivedStatDefinitions.RemoveRange(
                    currentDefinitions.Where(x => removedIds.Contains(x.Id)));
            }

            // Mise à jour des compétences existantes et de leurs composants
            foreach (var item in requestDerivedStats.Where(x => x.DerivedStatDefinitionId.HasValue && !x.IsDeleted))
            {
                var entity = currentDefinitionById[item.DerivedStatDefinitionId!.Value];
                entity.Name = item.Name.Trim();
                entity.MinValue = item.MinValue;
                entity.MaxValue = item.MaxValue;
                entity.RoundMode = item.RoundMode;
                entity.DisplayOrder = item.DisplayOrder;

                var existingComponents = currentComponents
                    .Where(x => x.DerivedStatDefinitionId == entity.Id)
                    .ToList();

                var requestExistingComponentIds = item.Components
                    .Where(x => x.DerivedStatComponentId.HasValue)
                    .Select(x => x.DerivedStatComponentId!.Value)
                    .ToHashSet();

                var removedComponentIds = item.Components
                    .Where(x => x.DerivedStatComponentId.HasValue && x.IsDeleted)
                    .Select(x => x.DerivedStatComponentId!.Value)
                    .Union(existingComponents.Where(x => !requestExistingComponentIds.Contains(x.Id)).Select(x => x.Id))
                    .Distinct()
                    .ToList();

                if (removedComponentIds.Count > 0)
                {
                    context.DerivedStatComponents.RemoveRange(
                        existingComponents.Where(x => removedComponentIds.Contains(x.Id)));
                }

                foreach (var componentDto in item.Components.Where(x => x.DerivedStatComponentId.HasValue && !x.IsDeleted))
                {
                    var component = existingComponents.First(x => x.Id == componentDto.DerivedStatComponentId!.Value);
                    component.AttributeDefinitionId = componentDto.AttributeDefinitionId;
                    component.Weight = componentDto.Weight;
                }

                foreach (var componentDto in item.Components.Where(x => !x.DerivedStatComponentId.HasValue && !x.IsDeleted))
                {
                    context.DerivedStatComponents.Add(new DerivedStatComponent
                    {
                        Id = Guid.NewGuid(),
                        DerivedStatDefinitionId = entity.Id,
                        AttributeDefinitionId = componentDto.AttributeDefinitionId,
                        Weight = componentDto.Weight
                    });
                }
            }

            // On crée d'abord les nouvelles compétences calculées...
            var newlyCreatedDefinitions = new List<(Guid DefinitionId, EditableDerivedStatDefinitionDto Dto)>();

            foreach (var item in requestDerivedStats.Where(x => !x.DerivedStatDefinitionId.HasValue && !x.IsDeleted && !string.IsNullOrWhiteSpace(x.Name)))
            {
                var definitionId = Guid.NewGuid();

                var definition = new DerivedStatDefinition
                {
                    Id = definitionId,
                    GameSystemId = gameSystemId,
                    Name = item.Name.Trim(),
                    MinValue = item.MinValue,
                    MaxValue = item.MaxValue,
                    RoundMode = item.RoundMode,
                    DisplayOrder = item.DisplayOrder
                };

                context.DerivedStatDefinitions.Add(definition);
                newlyCreatedDefinitions.Add((definitionId, item));
            }

            // ... puis on les enregistre avant d'ajouter leurs composants.
            if (newlyCreatedDefinitions.Count > 0)
            {
                await context.SaveChangesAsync();

                foreach (var created in newlyCreatedDefinitions)
                {
                    foreach (var componentDto in created.Dto.Components.Where(x => !x.IsDeleted))
                    {
                        context.DerivedStatComponents.Add(new DerivedStatComponent
                        {
                            Id = Guid.NewGuid(),
                            DerivedStatDefinitionId = created.DefinitionId,
                            AttributeDefinitionId = componentDto.AttributeDefinitionId,
                            Weight = componentDto.Weight
                        });
                    }
                }
            }
        }

        private async Task SyncTraitsAsync(
            RollocracyDbContext context,
            Guid gameSystemId,
            List<EditableTraitDefinitionDto> requestTraits,
            List<Character> affectedCharacters)
        {
            var currentTraits = await context.TraitDefinitions
                .Where(x => x.GameSystemId == gameSystemId)
                .ToListAsync();

            var currentTraitById = currentTraits.ToDictionary(x => x.Id, x => x);
            var currentTraitIds = currentTraits.Select(x => x.Id).ToList();

            var currentOptions = await context.TraitOptions
                .Where(x => currentTraitIds.Contains(x.TraitDefinitionId))
                .ToListAsync();

            var currentOptionById = currentOptions.ToDictionary(x => x.Id, x => x);
            var requestExistingTraitIds = requestTraits
                .Where(x => x.TraitDefinitionId.HasValue)
                .Select(x => x.TraitDefinitionId!.Value)
                .ToHashSet();

            var removedTraitIds = requestTraits
                .Where(x => x.TraitDefinitionId.HasValue && x.IsDeleted)
                .Select(x => x.TraitDefinitionId!.Value)
                .Union(currentTraits.Where(x => !requestExistingTraitIds.Contains(x.Id)).Select(x => x.Id))
                .Distinct()
                .ToList();

            var characterIds = affectedCharacters.Select(x => x.Id).ToList();

            foreach (var item in requestTraits.Where(x => x.TraitDefinitionId.HasValue && !x.IsDeleted))
            {
                var entity = currentTraitById[item.TraitDefinitionId!.Value];
                entity.Name = item.Name.Trim();

                var traitCurrentOptions = currentOptions.Where(x => x.TraitDefinitionId == entity.Id).ToList();
                var requestExistingOptionIds = item.Options
                    .Where(x => x.TraitOptionId.HasValue)
                    .Select(x => x.TraitOptionId!.Value)
                    .ToHashSet();

                var removedOptionIds = item.Options
                    .Where(x => x.TraitOptionId.HasValue && x.IsDeleted)
                    .Select(x => x.TraitOptionId!.Value)
                    .Union(traitCurrentOptions.Where(x => !requestExistingOptionIds.Contains(x.Id)).Select(x => x.Id))
                    .Distinct()
                    .ToList();

                if (removedOptionIds.Count > 0)
                {
                    var traitValuesToDelete = await context.CharacterTraitValues
                        .Where(x => characterIds.Contains(x.CharacterId) && removedOptionIds.Contains(x.TraitOptionId))
                        .ToListAsync();

                    context.CharacterTraitValues.RemoveRange(traitValuesToDelete);
                    context.TraitOptions.RemoveRange(traitCurrentOptions.Where(x => removedOptionIds.Contains(x.Id)));
                }

                foreach (var optionDto in item.Options.Where(x => x.TraitOptionId.HasValue && !x.IsDeleted))
                {
                    var option = currentOptionById[optionDto.TraitOptionId!.Value];
                    option.Name = optionDto.Name.Trim();
                }

                foreach (var optionDto in item.Options.Where(x => !x.TraitOptionId.HasValue && !x.IsDeleted && !string.IsNullOrWhiteSpace(x.Name)))
                {
                    context.TraitOptions.Add(new TraitOption
                    {
                        Id = Guid.NewGuid(),
                        TraitDefinitionId = entity.Id,
                        Name = optionDto.Name.Trim()
                    });
                }
            }

            if (removedTraitIds.Count > 0)
            {
                var valuesToDelete = await context.CharacterTraitValues
                    .Where(x => characterIds.Contains(x.CharacterId) && removedTraitIds.Contains(x.TraitDefinitionId))
                    .ToListAsync();

                var optionsToDelete = currentOptions.Where(x => removedTraitIds.Contains(x.TraitDefinitionId)).ToList();

                context.CharacterTraitValues.RemoveRange(valuesToDelete);
                context.TraitOptions.RemoveRange(optionsToDelete);
                context.TraitDefinitions.RemoveRange(currentTraits.Where(x => removedTraitIds.Contains(x.Id)));
            }

            foreach (var item in requestTraits.Where(x => !x.TraitDefinitionId.HasValue && !x.IsDeleted && !string.IsNullOrWhiteSpace(x.Name)))
            {
                var trait = new TraitDefinition
                {
                    Id = Guid.NewGuid(),
                    GameSystemId = gameSystemId,
                    Name = item.Name.Trim()
                };

                context.TraitDefinitions.Add(trait);

                foreach (var optionDto in item.Options.Where(x => !x.IsDeleted && !string.IsNullOrWhiteSpace(x.Name)))
                {
                    context.TraitOptions.Add(new TraitOption
                    {
                        Id = Guid.NewGuid(),
                        TraitDefinitionId = trait.Id,
                        Name = optionDto.Name.Trim()
                    });
                }
            }
        }

        private async Task SyncGaugesAsync(
            RollocracyDbContext context,
            Guid gameSystemId,
            List<Character> affectedCharacters,
            List<EditableGaugeDefinitionDto> requestGauges)
        {
            var currentGauges = await context.GaugeDefinitions
                .Where(x => x.GameSystemId == gameSystemId)
                .ToListAsync();

            var currentById = currentGauges.ToDictionary(x => x.Id, x => x);
            var requestExistingIds = requestGauges
                .Where(x => x.GaugeDefinitionId.HasValue)
                .Select(x => x.GaugeDefinitionId!.Value)
                .ToHashSet();

            var removedIds = requestGauges
                .Where(x => x.GaugeDefinitionId.HasValue && x.IsDeleted)
                .Select(x => x.GaugeDefinitionId!.Value)
                .Union(currentGauges.Where(x => !requestExistingIds.Contains(x.Id)).Select(x => x.Id))
                .Distinct()
                .ToList();

            foreach (var item in requestGauges.Where(x => x.GaugeDefinitionId.HasValue && !x.IsDeleted))
            {
                var entity = currentById[item.GaugeDefinitionId!.Value];
                entity.Name = item.Name.Trim();
                entity.MinValue = item.MinValue;
                entity.MaxValue = item.MaxValue;
                entity.DefaultValue = item.DefaultValue;
                entity.IsHealthGauge = item.IsHealthGauge;
            }

            var characterIds = affectedCharacters.Select(x => x.Id).ToList();

            if (removedIds.Count > 0)
            {
                var valuesToDelete = await context.CharacterGaugeValues
                    .Where(x => characterIds.Contains(x.CharacterId) && removedIds.Contains(x.GaugeDefinitionId))
                    .ToListAsync();

                context.CharacterGaugeValues.RemoveRange(valuesToDelete);
                context.GaugeDefinitions.RemoveRange(currentGauges.Where(x => removedIds.Contains(x.Id)));
            }

            var newDefinitions = new List<GaugeDefinition>();

            foreach (var item in requestGauges.Where(x => !x.GaugeDefinitionId.HasValue && !x.IsDeleted && !string.IsNullOrWhiteSpace(x.Name)))
            {
                var entity = new GaugeDefinition
                {
                    Id = Guid.NewGuid(),
                    GameSystemId = gameSystemId,
                    Name = item.Name.Trim(),
                    MinValue = item.MinValue,
                    MaxValue = item.MaxValue,
                    DefaultValue = item.DefaultValue,
                    IsHealthGauge = item.IsHealthGauge
                };

                context.GaugeDefinitions.Add(entity);
                newDefinitions.Add(entity);
            }

            foreach (var character in affectedCharacters)
            {
                foreach (var definition in newDefinitions)
                {
                    context.CharacterGaugeValues.Add(new CharacterGaugeValue
                    {
                        Id = Guid.NewGuid(),
                        CharacterId = character.Id,
                        GaugeDefinitionId = definition.Id,
                        Value = definition.DefaultValue
                    });
                }
            }

            var remainingGauges = currentGauges.Where(x => !removedIds.Contains(x.Id)).ToList();
            remainingGauges.AddRange(newDefinitions);

            var remainingIds = remainingGauges.Select(x => x.Id).ToList();

            var valuesToClamp = await context.CharacterGaugeValues
                .Where(x => characterIds.Contains(x.CharacterId) && remainingIds.Contains(x.GaugeDefinitionId))
                .ToListAsync();

            foreach (var value in valuesToClamp)
            {
                var definition = remainingGauges.First(x => x.Id == value.GaugeDefinitionId);
                value.Value = Math.Clamp(value.Value, definition.MinValue, definition.MaxValue);
            }
        }

        private async Task<GameSystemSnapshot> BuildSnapshotAsync(
            RollocracyDbContext context,
            GameSystem system,
            Guid ownerUserAccountId,
            List<Character> affectedCharacters)
        {
            var attributeDefinitions = await context.AttributeDefinitions
                .AsNoTracking()
                .Where(x => x.GameSystemId == system.Id)
                .ToListAsync();

            var derivedDefinitions = await context.DerivedStatDefinitions
                .AsNoTracking()
                .Where(x => x.GameSystemId == system.Id)
                .ToListAsync();

            var derivedIds = derivedDefinitions.Select(x => x.Id).ToList();

            var derivedComponents = await context.DerivedStatComponents
                .AsNoTracking()
                .Where(x => derivedIds.Contains(x.DerivedStatDefinitionId))
                .ToListAsync();

            var traitDefinitions = await context.TraitDefinitions
                .AsNoTracking()
                .Where(x => x.GameSystemId == system.Id)
                .ToListAsync();

            var traitIds = traitDefinitions.Select(x => x.Id).ToList();

            var traitOptions = await context.TraitOptions
                .AsNoTracking()
                .Where(x => traitIds.Contains(x.TraitDefinitionId))
                .ToListAsync();

            var gaugeDefinitions = await context.GaugeDefinitions
                .AsNoTracking()
                .Where(x => x.GameSystemId == system.Id)
                .ToListAsync();

            var characterIds = affectedCharacters.Select(x => x.Id).ToList();

            var attributeValues = await context.CharacterAttributeValues
                .AsNoTracking()
                .Where(x => characterIds.Contains(x.CharacterId))
                .ToListAsync();

            var gaugeValues = await context.CharacterGaugeValues
                .AsNoTracking()
                .Where(x => characterIds.Contains(x.CharacterId))
                .ToListAsync();

            var traitValues = await context.CharacterTraitValues
                .AsNoTracking()
                .Where(x => characterIds.Contains(x.CharacterId))
                .ToListAsync();

            var payload = new GameSystemSnapshotPayload
            {
                System = new GameSystemSnapshotSystemInfo
                {
                    Id = system.Id,
                    Name = system.Name,
                    Description = system.Description,
                    TestResolutionMode = system.TestResolutionMode,
                    SourceGameSystemId = system.SourceGameSystemId,
                    LockedToSessionId = system.LockedToSessionId
                },
                Attributes = attributeDefinitions.Select(CloneAttribute).ToList(),
                DerivedStats = derivedDefinitions.Select(CloneDerivedStat).ToList(),
                DerivedStatComponents = derivedComponents.Select(CloneDerivedComponent).ToList(),
                Traits = traitDefinitions.Select(CloneTrait).ToList(),
                TraitOptions = traitOptions.Select(CloneTraitOption).ToList(),
                Gauges = gaugeDefinitions.Select(CloneGauge).ToList(),
                Characters = affectedCharacters.Select(x => new CharacterSnapshotItem
                {
                    Id = x.Id,
                    IsAlive = x.IsAlive,
                    DiedAtUtc = x.DiedAtUtc
                }).ToList(),
                AttributeValues = attributeValues.Select(CloneAttributeValue).ToList(),
                GaugeValues = gaugeValues.Select(CloneGaugeValue).ToList(),
                TraitValues = traitValues.Select(CloneTraitValue).ToList()
            };

            return new GameSystemSnapshot
            {
                Id = Guid.NewGuid(),
                GameSystemId = system.Id,
                OwnerUserAccountId = ownerUserAccountId,
                SnapshotJson = JsonSerializer.Serialize(payload),
                CreatedAtUtc = DateTime.UtcNow
            };
        }

        private static AttributeDefinition CloneAttribute(AttributeDefinition x) => new()
        {
            Id = x.Id,
            GameSystemId = x.GameSystemId,
            Name = x.Name,
            MinValue = x.MinValue,
            MaxValue = x.MaxValue,
            DefaultValue = x.DefaultValue,
            DefaultValueMode = x.DefaultValueMode,
            DefaultValueDiceCount = x.DefaultValueDiceCount,
            DefaultValueDiceSides = x.DefaultValueDiceSides,
            DefaultValueFlatBonus = x.DefaultValueFlatBonus
        };

        private static DerivedStatDefinition CloneDerivedStat(DerivedStatDefinition x) => new()
        {
            Id = x.Id,
            GameSystemId = x.GameSystemId,
            Name = x.Name,
            MinValue = x.MinValue,
            MaxValue = x.MaxValue,
            RoundMode = x.RoundMode,
            DisplayOrder = x.DisplayOrder
        };

        private static DerivedStatComponent CloneDerivedComponent(DerivedStatComponent x) => new()
        {
            Id = x.Id,
            DerivedStatDefinitionId = x.DerivedStatDefinitionId,
            AttributeDefinitionId = x.AttributeDefinitionId,
            Weight = x.Weight
        };

        private static TraitDefinition CloneTrait(TraitDefinition x) => new()
        {
            Id = x.Id,
            GameSystemId = x.GameSystemId,
            Name = x.Name
        };

        private static TraitOption CloneTraitOption(TraitOption x) => new()
        {
            Id = x.Id,
            TraitDefinitionId = x.TraitDefinitionId,
            Name = x.Name
        };

        private static GaugeDefinition CloneGauge(GaugeDefinition x) => new()
        {
            Id = x.Id,
            GameSystemId = x.GameSystemId,
            Name = x.Name,
            MinValue = x.MinValue,
            MaxValue = x.MaxValue,
            DefaultValue = x.DefaultValue,
            IsHealthGauge = x.IsHealthGauge
        };

        private static CharacterAttributeValue CloneAttributeValue(CharacterAttributeValue x) => new()
        {
            Id = x.Id,
            CharacterId = x.CharacterId,
            AttributeDefinitionId = x.AttributeDefinitionId,
            Value = x.Value
        };

        private static CharacterGaugeValue CloneGaugeValue(CharacterGaugeValue x) => new()
        {
            Id = x.Id,
            CharacterId = x.CharacterId,
            GaugeDefinitionId = x.GaugeDefinitionId,
            Value = x.Value
        };

        private static CharacterTraitValue CloneTraitValue(CharacterTraitValue x) => new()
        {
            Id = x.Id,
            CharacterId = x.CharacterId,
            TraitDefinitionId = x.TraitDefinitionId,
            TraitOptionId = x.TraitOptionId
        };

        private async Task<List<Session>> GetSessionsUsingSystemAsync(RollocracyDbContext context, Guid gameSystemId)
        {
            return await context.Sessions
                .AsNoTracking()
                .Where(x => x.GameSystemId == gameSystemId)
                .OrderBy(x => x.SessionName)
                .ToListAsync();
        }

        private async Task<List<Character>> GetCharactersUsingSystemAsync(RollocracyDbContext context, Guid gameSystemId)
        {
            var gameSystem = await context.GameSystems
                .AsNoTracking()
                .FirstOrDefaultAsync(gs => gs.Id == gameSystemId);

            if (gameSystem == null)
                return new List<Character>();

            var sessionIdsUsingThisSystem = await context.Sessions
                .AsNoTracking()
                .Where(s => s.GameSystemId == gameSystemId)
                .Select(s => s.Id)
                .ToListAsync();

            if (gameSystem.LockedToSessionId.HasValue &&
                !sessionIdsUsingThisSystem.Contains(gameSystem.LockedToSessionId.Value))
            {
                sessionIdsUsingThisSystem.Add(gameSystem.LockedToSessionId.Value);
            }

            if (sessionIdsUsingThisSystem.Count == 0)
                return new List<Character>();

            var playerSessionIds = await context.PlayerSessions
                .AsNoTracking()
                .Where(ps => sessionIdsUsingThisSystem.Contains(ps.SessionId))
                .Select(ps => ps.Id)
                .ToListAsync();

            if (playerSessionIds.Count == 0)
                return new List<Character>();

            return await context.Characters
                .Where(c => playerSessionIds.Contains(c.PlayerSessionId))
                .ToListAsync();
        }

        private void ValidateBaseAttributes(List<EditableAttributeDefinitionDto> attributes)
        {
            foreach (var attribute in attributes.Where(x => !x.IsDeleted))
            {
                if (string.IsNullOrWhiteSpace(attribute.Name))
                    throw new Exception(_localizer["Backend_GameSystemBaseStatNameRequired"]);

                if (attribute.MinValue > attribute.MaxValue)
                    throw new Exception(_localizer["Backend_InvalidBaseStatRange"]);

                if (attribute.DefaultValueMode == BaseValueGenerationMode.Fixed)
                {
                    if (attribute.DefaultValue < attribute.MinValue || attribute.DefaultValue > attribute.MaxValue)
                        throw new Exception(_localizer["Backend_InvalidFixedBaseStatDefaultValue"]);
                }
                else
                {
                    if (attribute.DefaultValueDiceCount < 1 || attribute.DefaultValueDiceCount > 5)
                        throw new Exception(_localizer["Backend_InvalidAttributeDefaultDiceExpression"]);

                    if (attribute.DefaultValueDiceSides < 2 || attribute.DefaultValueDiceSides > 100)
                        throw new Exception(_localizer["Backend_InvalidAttributeDefaultDiceExpression"]);

                    var minPossible = attribute.DefaultValueFlatBonus + attribute.DefaultValueDiceCount;
                    var maxPossible = attribute.DefaultValueFlatBonus + (attribute.DefaultValueDiceCount * attribute.DefaultValueDiceSides);

                    if (minPossible < attribute.MinValue || maxPossible > attribute.MaxValue)
                        throw new Exception(_localizer["Backend_InvalidAttributeDefaultDiceExpression"]);
                }
            }
        }

        private void ValidateDerivedStats(
            List<EditableDerivedStatDefinitionDto> derivedStats,
            List<EditableAttributeDefinitionDto> attributes)
        {
            var availableAttributeIds = attributes
                .Where(x => !x.IsDeleted && x.AttributeDefinitionId.HasValue)
                .Select(x => x.AttributeDefinitionId!.Value)
                .ToHashSet();

            foreach (var attribute in attributes.Where(x => !x.IsDeleted && !x.AttributeDefinitionId.HasValue))
            {
                // Les nouveaux attributs n'ont pas encore d'ID DB à ce stade,
                // mais ils peuvent être utilisés en UI uniquement après recharge.
            }

            foreach (var derived in derivedStats.Where(x => !x.IsDeleted))
            {
                if (string.IsNullOrWhiteSpace(derived.Name))
                    throw new Exception(_localizer["Backend_DerivedStatNameRequired"]);

                if (derived.MinValue > derived.MaxValue)
                    throw new Exception(_localizer["Backend_InvalidDerivedStatRange"]);

                var activeComponents = derived.Components.Where(x => !x.IsDeleted).ToList();

                if (activeComponents.Count == 0)
                    throw new Exception(_localizer["Backend_DerivedStatMustHaveAtLeastOneComponent"]);

                foreach (var component in activeComponents)
                {
                    if (component.Weight < 1 || component.Weight > 500)
                        throw new Exception(_localizer["Backend_InvalidDerivedStatComponentWeight"]);

                    if (component.AttributeDefinitionId == Guid.Empty)
                        throw new Exception(_localizer["Backend_InvalidDerivedStatComponentAttribute"]);
                }
            }
        }

        private void ValidateHealthGaugeRule(List<EditableGaugeDefinitionDto> gauges)
        {
            var remainingHealthGaugeCount = gauges.Count(x => !x.IsDeleted && x.IsHealthGauge);

            if (remainingHealthGaugeCount <= 0)
                throw new Exception(_localizer["Backend_GameSystemMustKeepAtLeastOneHealthGauge"]);
        }

        private async Task TrimSnapshotsAsync(RollocracyDbContext context, Guid gameSystemId)
        {
            var snapshotsToDelete = await context.GameSystemSnapshots
                .Where(x => x.GameSystemId == gameSystemId)
                .OrderByDescending(x => x.CreatedAtUtc)
                .Skip(5)
                .ToListAsync();

            if (snapshotsToDelete.Count > 0)
            {
                context.GameSystemSnapshots.RemoveRange(snapshotsToDelete);
                await context.SaveChangesAsync();
            }
        }

        private async Task EnsureUserCanManageGameSystemsAsync(
            RollocracyDbContext context,
            Guid ownerUserAccountId)
        {
            var user = await context.UserAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == ownerUserAccountId);

            if (user == null)
                throw new Exception(_localizer["Backend_UserAccountNotFound"]);

            var normalizedJms = Math.Clamp(user.MaxPlayersPerSession, 0, 5000);

            if (normalizedJms <= 0)
                throw new Exception(_localizer["Backend_OnlyUsersWithPositiveJmsCanManageGameSystems"]);
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

        private sealed class GameSystemSnapshotPayload
        {
            public GameSystemSnapshotSystemInfo System { get; set; } = new();
            public List<AttributeDefinition> Attributes { get; set; } = new();
            public List<DerivedStatDefinition> DerivedStats { get; set; } = new();
            public List<DerivedStatComponent> DerivedStatComponents { get; set; } = new();
            public List<TraitDefinition> Traits { get; set; } = new();
            public List<TraitOption> TraitOptions { get; set; } = new();
            public List<GaugeDefinition> Gauges { get; set; } = new();
            public List<CharacterSnapshotItem> Characters { get; set; } = new();
            public List<CharacterAttributeValue> AttributeValues { get; set; } = new();
            public List<CharacterGaugeValue> GaugeValues { get; set; } = new();
            public List<CharacterTraitValue> TraitValues { get; set; } = new();
        }

        private sealed class GameSystemSnapshotSystemInfo
        {
            public Guid Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public TestResolutionMode TestResolutionMode { get; set; }
            public Guid? SourceGameSystemId { get; set; }
            public Guid? LockedToSessionId { get; set; }
        }

        private sealed class CharacterSnapshotItem
        {
            public Guid Id { get; set; }
            public bool IsAlive { get; set; }
            public DateTime? DiedAtUtc { get; set; }
        }
    }
}
