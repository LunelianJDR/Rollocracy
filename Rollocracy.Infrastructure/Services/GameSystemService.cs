using Microsoft.EntityFrameworkCore;
using Rollocracy.Domain.Entities;
using Rollocracy.Domain.GameRules;
using Rollocracy.Domain.Interfaces;
using Rollocracy.Infrastructure.Persistence;

namespace Rollocracy.Infrastructure.Services
{
    public class GameSystemService : IGameSystemService
    {
        private readonly IDbContextFactory<RollocracyDbContext> _contextFactory;

        public GameSystemService(IDbContextFactory<RollocracyDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        // Liste uniquement les systèmes de base réutilisables
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
                .FirstOrDefaultAsync(gs =>
                    gs.Id == gameSystemId &&
                    gs.OwnerUserAccountId == ownerUserAccountId);
        }

        // Crée un système de base et lui ajoute automatiquement une jauge Vie
        public async Task<GameSystem> CreateGameSystemAsync(
            Guid ownerUserAccountId,
            string name,
            string description,
            TestResolutionMode testResolutionMode)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

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

            // Jauge créée par défaut sur tout nouveau système
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

            var system = await context.GameSystems
                .FirstOrDefaultAsync(gs =>
                    gs.Id == gameSystemId &&
                    gs.OwnerUserAccountId == ownerUserAccountId);

            if (system == null)
                throw new Exception("Système introuvable.");

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

            var system = await context.GameSystems
                .AsNoTracking()
                .FirstOrDefaultAsync(gs =>
                    gs.Id == gameSystemId &&
                    gs.OwnerUserAccountId == ownerUserAccountId);

            if (system == null)
                throw new Exception("Système introuvable.");

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
                .AnyAsync(gs =>
                    gs.Id == gameSystemId &&
                    gs.OwnerUserAccountId == ownerUserAccountId);

            if (!ownsSystem)
                throw new Exception("Système introuvable.");

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

            var system = await context.GameSystems
                .AsNoTracking()
                .FirstOrDefaultAsync(gs =>
                    gs.Id == gameSystemId &&
                    gs.OwnerUserAccountId == ownerUserAccountId);

            if (system == null)
                throw new Exception("Système introuvable.");

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
                .AnyAsync(gs =>
                    gs.Id == gameSystemId &&
                    gs.OwnerUserAccountId == ownerUserAccountId);

            if (!ownsSystem)
                throw new Exception("Système introuvable.");

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

            var traitDefinition = await context.TraitDefinitions
                .AsNoTracking()
                .FirstOrDefaultAsync(td => td.Id == traitDefinitionId);

            if (traitDefinition == null)
                throw new Exception("Attribut de choix introuvable.");

            var ownsSystem = await context.GameSystems
                .AsNoTracking()
                .AnyAsync(gs =>
                    gs.Id == traitDefinition.GameSystemId &&
                    gs.OwnerUserAccountId == ownerUserAccountId);

            if (!ownsSystem)
                throw new Exception("Accès refusé à ce système.");

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
                throw new Exception("Attribut de choix introuvable.");

            var ownsSystem = await context.GameSystems
                .AsNoTracking()
                .AnyAsync(gs =>
                    gs.Id == traitDefinition.GameSystemId &&
                    gs.OwnerUserAccountId == ownerUserAccountId);

            if (!ownsSystem)
                throw new Exception("Accès refusé à ce système.");

            return await context.TraitOptions
                .AsNoTracking()
                .Where(o => o.TraitDefinitionId == traitDefinitionId)
                .OrderBy(o => o.Name)
                .ToListAsync();
        }

        // Liste les jauges d'un système
        public async Task<List<GaugeDefinition>> GetGaugeDefinitionsAsync(Guid gameSystemId, Guid ownerUserAccountId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var ownsSystem = await context.GameSystems
                .AsNoTracking()
                .AnyAsync(gs =>
                    gs.Id == gameSystemId &&
                    gs.OwnerUserAccountId == ownerUserAccountId);

            if (!ownsSystem)
                throw new Exception("Système introuvable.");

            return await context.GaugeDefinitions
                .AsNoTracking()
                .Where(g => g.GameSystemId == gameSystemId)
                .OrderBy(g => g.Name)
                .ToListAsync();
        }

        // Ajoute une jauge à un système
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

            var system = await context.GameSystems
                .AsNoTracking()
                .FirstOrDefaultAsync(gs =>
                    gs.Id == gameSystemId &&
                    gs.OwnerUserAccountId == ownerUserAccountId);

            if (system == null)
                throw new Exception("Système introuvable.");

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

        // Crée une copie complète du système pour une session spécifique
        public async Task<GameSystem> CloneGameSystemForSessionAsync(
            Guid sourceGameSystemId,
            Guid ownerUserAccountId,
            Guid sessionId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var sourceSystem = await context.GameSystems
                .AsNoTracking()
                .FirstOrDefaultAsync(gs =>
                    gs.Id == sourceGameSystemId &&
                    gs.OwnerUserAccountId == ownerUserAccountId);

            if (sourceSystem == null)
                throw new Exception("Système source introuvable.");

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

            // Clone les caractéristiques numériques
            var sourceAttributes = await context.AttributeDefinitions
                .AsNoTracking()
                .Where(a => a.GameSystemId == sourceSystem.Id)
                .ToListAsync();

            foreach (var attribute in sourceAttributes)
            {
                context.AttributeDefinitions.Add(new AttributeDefinition
                {
                    Id = Guid.NewGuid(),
                    GameSystemId = clonedSystem.Id,
                    Name = attribute.Name,
                    MinValue = attribute.MinValue,
                    MaxValue = attribute.MaxValue,
                    DefaultValue = attribute.DefaultValue
                });
            }

            // Clone les attributs à choix et leurs options
            var sourceTraits = await context.TraitDefinitions
                .AsNoTracking()
                .Where(t => t.GameSystemId == sourceSystem.Id)
                .ToListAsync();

            foreach (var sourceTrait in sourceTraits)
            {
                var clonedTrait = new TraitDefinition
                {
                    Id = Guid.NewGuid(),
                    GameSystemId = clonedSystem.Id,
                    Name = sourceTrait.Name
                };

                context.TraitDefinitions.Add(clonedTrait);

                var sourceOptions = await context.TraitOptions
                    .AsNoTracking()
                    .Where(o => o.TraitDefinitionId == sourceTrait.Id)
                    .ToListAsync();

                foreach (var option in sourceOptions)
                {
                    context.TraitOptions.Add(new TraitOption
                    {
                        Id = Guid.NewGuid(),
                        TraitDefinitionId = clonedTrait.Id,
                        Name = option.Name
                    });
                }
            }

            // Clone les jauges
            var sourceGauges = await context.GaugeDefinitions
                .AsNoTracking()
                .Where(g => g.GameSystemId == sourceSystem.Id)
                .ToListAsync();

            foreach (var gauge in sourceGauges)
            {
                context.GaugeDefinitions.Add(new GaugeDefinition
                {
                    Id = Guid.NewGuid(),
                    GameSystemId = clonedSystem.Id,
                    Name = gauge.Name,
                    MinValue = gauge.MinValue,
                    MaxValue = gauge.MaxValue,
                    DefaultValue = gauge.DefaultValue,
                    IsHealthGauge = gauge.IsHealthGauge
                });
            }

            await context.SaveChangesAsync();

            return clonedSystem;
        }
    }
}