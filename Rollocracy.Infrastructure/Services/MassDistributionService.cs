using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Rollocracy.Domain.Characters;
using Rollocracy.Domain.Entities;
using Rollocracy.Domain.GameRules;
using Rollocracy.Domain.Interfaces;
using Rollocracy.Infrastructure.Persistence;
using System.Text.Json;

namespace Rollocracy.Infrastructure.Services
{
    // Service métier de distribution de masse MJ.
    // 5B.2 ajoute l'annulation complète du dernier lot appliqué.
    public class MassDistributionService : IMassDistributionService
    {
        private readonly IDbContextFactory<RollocracyDbContext> _contextFactory;
        private readonly ICharacterEffectService _characterEffectService;
        private readonly IStringLocalizer _localizer;

        public MassDistributionService(
            IDbContextFactory<RollocracyDbContext> contextFactory,
            ICharacterEffectService characterEffectService,
            IStringLocalizerFactory localizerFactory)
        {
            _contextFactory = contextFactory;
            _characterEffectService = characterEffectService;
            _localizer = localizerFactory.Create("Rollocracy.Localization.SharedTexts", "Rollocracy");
        }

        public async Task<MassDistributionEditorDto?> GetEditorAsync(Guid sessionId, Guid userAccountId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var session = await context.Sessions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.GameMasterUserAccountId == userAccountId);

            if (session == null)
                return null;

            if (!session.GameSystemId.HasValue)
                throw new Exception(_localizer["Backend_SessionHasNoGameSystem"]);

            var gameSystemId = session.GameSystemId.Value;

            var traitDefinitions = await context.TraitDefinitions
                .AsNoTracking()
                .Where(x => x.GameSystemId == gameSystemId)
                .ToListAsync();

            var traitDefinitionIds = traitDefinitions.Select(x => x.Id).ToList();

            var traitOptions = await context.TraitOptions
                .AsNoTracking()
                .Where(x => traitDefinitionIds.Contains(x.TraitDefinitionId))
                .OrderBy(x => x.Name)
                .ToListAsync();

            var talents = await context.TalentDefinitions
                .AsNoTracking()
                .Where(x => x.GameSystemId == gameSystemId)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Name)
                .ToListAsync();

            var items = await context.ItemDefinitions
                .AsNoTracking()
                .Where(x => x.GameSystemId == gameSystemId)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Name)
                .ToListAsync();

            var attributes = await context.AttributeDefinitions
                .AsNoTracking()
                .Where(x => x.GameSystemId == gameSystemId)
                .OrderBy(x => x.Name)
                .ToListAsync();

            var gauges = await context.GaugeDefinitions
                .AsNoTracking()
                .Where(x => x.GameSystemId == gameSystemId)
                .OrderBy(x => x.Name)
                .ToListAsync();

            var derivedStats = await context.DerivedStatDefinitions
                .AsNoTracking()
                .Where(x => x.GameSystemId == gameSystemId)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Name)
                .ToListAsync();

            var metrics = await context.MetricDefinitions
                .AsNoTracking()
                .Where(x => x.GameSystemId == gameSystemId)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Name)
                .ToListAsync();

            return new MassDistributionEditorDto
            {
                SessionId = session.Id,
                SessionName = session.SessionName,
                TraitOptions = traitOptions.Select(x => new NamedReferenceDto { Id = x.Id, Name = x.Name }).ToList(),
                Talents = talents.Select(x => new NamedReferenceDto { Id = x.Id, Name = x.Name }).ToList(),
                Items = items.Select(x => new NamedReferenceDto { Id = x.Id, Name = x.Name }).ToList(),
                BaseAttributes = attributes.Select(x => new NamedReferenceDto { Id = x.Id, Name = x.Name }).ToList(),
                Gauges = gauges.Select(x => new NamedReferenceDto { Id = x.Id, Name = x.Name }).ToList(),
                DerivedStats = derivedStats.Select(x => new NamedReferenceDto { Id = x.Id, Name = x.Name }).ToList(),
                Metrics = metrics.Select(x => new NamedReferenceDto { Id = x.Id, Name = x.Name }).ToList()
            };
        }

        public async Task<List<MassDistributionPreviewCharacterDto>> PreviewTargetsAsync(
            Guid sessionId,
            Guid userAccountId,
            MassDistributionRequestDto request)
        {
            await EnsureSessionOwnershipAsync(sessionId, userAccountId);

            var characters = await _characterEffectService.GetTargetCharactersAsync(sessionId, request.Filter);

            return characters.Select(x => new MassDistributionPreviewCharacterDto
            {
                CharacterId = x.Id,
                CharacterName = x.Name,
                IsAlive = x.IsAlive
            }).ToList();
        }

        public async Task<int> ApplyAsync(
            Guid sessionId,
            Guid userAccountId,
            MassDistributionRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new Exception(_localizer["Backend_MassDistributionNameRequired"]);

            await EnsureSessionOwnershipAsync(sessionId, userAccountId);

            var targetCharacters = await _characterEffectService.GetTargetCharactersAsync(sessionId, request.Filter);
            var targetCharacterIds = targetCharacters.Select(x => x.Id).ToList();

            var batchId = Guid.NewGuid();

            await using var context = await _contextFactory.CreateDbContextAsync();

            var snapshotJson = await BuildUndoSnapshotJsonAsync(context, targetCharacterIds);

            var batch = new MassDistributionBatch
            {
                Id = batchId,
                SessionId = sessionId,
                CreatedByUserAccountId = userAccountId,
                Name = request.Name.Trim(),
                TargetCharacterCount = targetCharacterIds.Count,
                CreatedAtUtc = DateTime.UtcNow,
                FilterSnapshotJson = JsonSerializer.Serialize(request.Filter),
                EffectsSnapshotJson = JsonSerializer.Serialize(request.Effects),
                UndoSnapshotJson = snapshotJson,
                IsUndone = false
            };

            context.MassDistributionBatches.Add(batch);
            await context.SaveChangesAsync();

            try
            {
                await _characterEffectService.ApplyEffectsAsync(
                    sessionId,
                    targetCharacterIds,
                    request.Effects,
                    CharacterEffectSourceType.MassDistribution,
                    batchId,
                    request.Name.Trim());

                return targetCharacterIds.Count;
            }
            catch
            {
                context.MassDistributionBatches.Remove(batch);
                await context.SaveChangesAsync();
                throw;
            }
        }

        public async Task<MassDistributionLastBatchDto?> GetLastBatchAsync(Guid sessionId, Guid userAccountId)
        {
            await EnsureSessionOwnershipAsync(sessionId, userAccountId);

            await using var context = await _contextFactory.CreateDbContextAsync();

            var batch = await context.MassDistributionBatches
                .AsNoTracking()
                .Where(x => x.SessionId == sessionId && !x.IsUndone)
                .OrderByDescending(x => x.CreatedAtUtc)
                .FirstOrDefaultAsync();

            if (batch == null)
                return null;

            return new MassDistributionLastBatchDto
            {
                BatchId = batch.Id,
                Name = batch.Name,
                TargetCharacterCount = batch.TargetCharacterCount,
                CreatedAtUtc = batch.CreatedAtUtc
            };
        }

        public async Task<string> UndoLastAsync(Guid sessionId, Guid userAccountId)
        {
            await EnsureSessionOwnershipAsync(sessionId, userAccountId);

            await using var context = await _contextFactory.CreateDbContextAsync();

            var batch = await context.MassDistributionBatches
                .FirstOrDefaultAsync(x => x.SessionId == sessionId && !x.IsUndone);

            if (batch == null)
                throw new Exception(_localizer["Backend_MassDistributionNoBatchToUndo"]);

            var latestBatch = await context.MassDistributionBatches
                .Where(x => x.SessionId == sessionId && !x.IsUndone)
                .OrderByDescending(x => x.CreatedAtUtc)
                .FirstAsync();

            if (latestBatch.Id != batch.Id)
                batch = latestBatch;

            var snapshot = JsonSerializer.Deserialize<MassDistributionUndoSnapshot>(batch.UndoSnapshotJson);

            if (snapshot == null)
                throw new Exception(_localizer["Backend_MassDistributionUndoSnapshotInvalid"]);

            var targetCharacterIds = snapshot.Characters.Select(x => x.CharacterId).ToList();

            var currentCharacters = await context.Characters
                .Where(x => targetCharacterIds.Contains(x.Id))
                .ToListAsync();

            var currentAttributeValues = await context.CharacterAttributeValues
                .Where(x => targetCharacterIds.Contains(x.CharacterId))
                .ToListAsync();

            var currentGaugeValues = await context.CharacterGaugeValues
                .Where(x => targetCharacterIds.Contains(x.CharacterId))
                .ToListAsync();

            var currentCharacterTalents = await context.CharacterTalents
                .Where(x => targetCharacterIds.Contains(x.CharacterId))
                .ToListAsync();

            var currentCharacterItems = await context.CharacterItems
                .Where(x => targetCharacterIds.Contains(x.CharacterId))
                .ToListAsync();

            var currentCharacterModifiers = await context.CharacterModifiers
                .Where(x => targetCharacterIds.Contains(x.CharacterId))
                .ToListAsync();

            context.CharacterAttributeValues.RemoveRange(currentAttributeValues);
            context.CharacterGaugeValues.RemoveRange(currentGaugeValues);
            context.CharacterTalents.RemoveRange(currentCharacterTalents);
            context.CharacterItems.RemoveRange(currentCharacterItems);
            context.CharacterModifiers.RemoveRange(currentCharacterModifiers);

            foreach (var snapshotCharacter in snapshot.Characters)
            {
                var character = currentCharacters.FirstOrDefault(x => x.Id == snapshotCharacter.CharacterId);
                if (character == null)
                    continue;

                character.IsAlive = snapshotCharacter.IsAlive;
                character.DiedAtUtc = snapshotCharacter.DiedAtUtc;
            }

            foreach (var value in snapshot.AttributeValues)
            {
                context.CharacterAttributeValues.Add(new CharacterAttributeValue
                {
                    Id = value.Id,
                    CharacterId = value.CharacterId,
                    AttributeDefinitionId = value.AttributeDefinitionId,
                    Value = value.Value
                });
            }

            foreach (var value in snapshot.GaugeValues)
            {
                context.CharacterGaugeValues.Add(new CharacterGaugeValue
                {
                    Id = value.Id,
                    CharacterId = value.CharacterId,
                    GaugeDefinitionId = value.GaugeDefinitionId,
                    Value = value.Value
                });
            }

            foreach (var value in snapshot.CharacterTalents)
            {
                context.CharacterTalents.Add(new CharacterTalent
                {
                    Id = value.Id,
                    CharacterId = value.CharacterId,
                    TalentDefinitionId = value.TalentDefinitionId
                });
            }

            foreach (var value in snapshot.CharacterItems)
            {
                context.CharacterItems.Add(new CharacterItem
                {
                    Id = value.Id,
                    CharacterId = value.CharacterId,
                    ItemDefinitionId = value.ItemDefinitionId
                });
            }

            foreach (var value in snapshot.CharacterModifiers)
            {
                context.CharacterModifiers.Add(new CharacterModifier
                {
                    Id = value.Id,
                    CharacterId = value.CharacterId,
                    TargetType = value.TargetType,
                    TargetId = value.TargetId,
                    AddValue = value.AddValue,
                    SourceType = value.SourceType,
                    SourceId = value.SourceId,
                    SourceNameSnapshot = value.SourceNameSnapshot,
                    CreatedAtUtc = value.CreatedAtUtc
                });
            }

            batch.IsUndone = true;
            batch.UndoneAtUtc = DateTime.UtcNow;

            await context.SaveChangesAsync();

            return batch.Name;
        }

        private async Task EnsureSessionOwnershipAsync(Guid sessionId, Guid userAccountId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var exists = await context.Sessions
                .AsNoTracking()
                .AnyAsync(s => s.Id == sessionId && s.GameMasterUserAccountId == userAccountId);

            if (!exists)
                throw new Exception(_localizer["Backend_SessionAccessDenied"]);
        }

        private async Task<string> BuildUndoSnapshotJsonAsync(RollocracyDbContext context, List<Guid> targetCharacterIds)
        {
            var characters = await context.Characters
                .AsNoTracking()
                .Where(x => targetCharacterIds.Contains(x.Id))
                .ToListAsync();

            var attributeValues = await context.CharacterAttributeValues
                .AsNoTracking()
                .Where(x => targetCharacterIds.Contains(x.CharacterId))
                .ToListAsync();

            var gaugeValues = await context.CharacterGaugeValues
                .AsNoTracking()
                .Where(x => targetCharacterIds.Contains(x.CharacterId))
                .ToListAsync();

            var characterTalents = await context.CharacterTalents
                .AsNoTracking()
                .Where(x => targetCharacterIds.Contains(x.CharacterId))
                .ToListAsync();

            var characterItems = await context.CharacterItems
                .AsNoTracking()
                .Where(x => targetCharacterIds.Contains(x.CharacterId))
                .ToListAsync();

            var characterModifiers = await context.CharacterModifiers
                .AsNoTracking()
                .Where(x => targetCharacterIds.Contains(x.CharacterId))
                .ToListAsync();

            var snapshot = new MassDistributionUndoSnapshot
            {
                Characters = characters.Select(x => new CharacterUndoState
                {
                    CharacterId = x.Id,
                    IsAlive = x.IsAlive,
                    DiedAtUtc = x.DiedAtUtc
                }).ToList(),
                AttributeValues = attributeValues.Select(x => new CharacterAttributeValueUndoState
                {
                    Id = x.Id,
                    CharacterId = x.CharacterId,
                    AttributeDefinitionId = x.AttributeDefinitionId,
                    Value = x.Value
                }).ToList(),
                GaugeValues = gaugeValues.Select(x => new CharacterGaugeValueUndoState
                {
                    Id = x.Id,
                    CharacterId = x.CharacterId,
                    GaugeDefinitionId = x.GaugeDefinitionId,
                    Value = x.Value
                }).ToList(),
                CharacterTalents = characterTalents.Select(x => new CharacterTalentUndoState
                {
                    Id = x.Id,
                    CharacterId = x.CharacterId,
                    TalentDefinitionId = x.TalentDefinitionId
                }).ToList(),
                CharacterItems = characterItems.Select(x => new CharacterItemUndoState
                {
                    Id = x.Id,
                    CharacterId = x.CharacterId,
                    ItemDefinitionId = x.ItemDefinitionId
                }).ToList(),
                CharacterModifiers = characterModifiers.Select(x => new CharacterModifierUndoState
                {
                    Id = x.Id,
                    CharacterId = x.CharacterId,
                    TargetType = x.TargetType,
                    TargetId = x.TargetId,
                    AddValue = x.AddValue,
                    SourceType = x.SourceType,
                    SourceId = x.SourceId,
                    SourceNameSnapshot = x.SourceNameSnapshot,
                    CreatedAtUtc = x.CreatedAtUtc
                }).ToList()
            };

            return JsonSerializer.Serialize(snapshot);
        }

        private sealed class MassDistributionUndoSnapshot
        {
            public List<CharacterUndoState> Characters { get; set; } = new();
            public List<CharacterAttributeValueUndoState> AttributeValues { get; set; } = new();
            public List<CharacterGaugeValueUndoState> GaugeValues { get; set; } = new();
            public List<CharacterTalentUndoState> CharacterTalents { get; set; } = new();
            public List<CharacterItemUndoState> CharacterItems { get; set; } = new();
            public List<CharacterModifierUndoState> CharacterModifiers { get; set; } = new();
        }

        private sealed class CharacterUndoState
        {
            public Guid CharacterId { get; set; }
            public bool IsAlive { get; set; }
            public DateTime? DiedAtUtc { get; set; }
        }

        private sealed class CharacterAttributeValueUndoState
        {
            public Guid Id { get; set; }
            public Guid CharacterId { get; set; }
            public Guid AttributeDefinitionId { get; set; }
            public int Value { get; set; }
        }

        private sealed class CharacterGaugeValueUndoState
        {
            public Guid Id { get; set; }
            public Guid CharacterId { get; set; }
            public Guid GaugeDefinitionId { get; set; }
            public int Value { get; set; }
        }

        private sealed class CharacterTalentUndoState
        {
            public Guid Id { get; set; }
            public Guid CharacterId { get; set; }
            public Guid TalentDefinitionId { get; set; }
        }

        private sealed class CharacterItemUndoState
        {
            public Guid Id { get; set; }
            public Guid CharacterId { get; set; }
            public Guid ItemDefinitionId { get; set; }
        }

        private sealed class CharacterModifierUndoState
        {
            public Guid Id { get; set; }
            public Guid CharacterId { get; set; }
            public CharacterEffectTargetType TargetType { get; set; }
            public Guid TargetId { get; set; }
            public int AddValue { get; set; }
            public CharacterEffectSourceType SourceType { get; set; }
            public Guid SourceId { get; set; }
            public string SourceNameSnapshot { get; set; } = string.Empty;
            public DateTime CreatedAtUtc { get; set; }
        }
    }
}
