using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Rollocracy.Domain.Characters;
using Rollocracy.Domain.Entities;
using Rollocracy.Domain.GameRules;
using Rollocracy.Domain.Interfaces;
using Rollocracy.Infrastructure.Persistence;

namespace Rollocracy.Infrastructure.Services
{
    // Moteur commun d'application des effets persistants sur les personnages.
    // 5A.3 ajoute la résolution des cibles à partir des filtres communs,
    // ainsi qu'un point d'entrée unique pour appliquer un lot nommé.
    public class CharacterEffectService : ICharacterEffectService
    {
        private readonly IDbContextFactory<RollocracyDbContext> _contextFactory;
        private readonly IStringLocalizer _localizer;
        private readonly IPresenceTracker _presenceTracker;

        public CharacterEffectService(
            IDbContextFactory<RollocracyDbContext> contextFactory,
            IStringLocalizerFactory localizerFactory,
            IPresenceTracker presenceTracker)
        {
            _contextFactory = contextFactory;
            _localizer = localizerFactory.Create("Rollocracy.Localization.SharedTexts", "Rollocracy");
            _presenceTracker = presenceTracker;
        }

        public async Task ApplyEffectsAsync(
            Guid sessionId,
            List<Guid> characterIds,
            List<CharacterEffectDefinitionDto> effects,
            CharacterEffectSourceType sourceType,
            Guid sourceId,
            string sourceName)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            if (characterIds.Count == 0 || effects.Count == 0)
                return;

            var normalizedCharacterIds = characterIds.Distinct().ToList();

            var session = await context.Sessions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == sessionId);

            if (session == null)
                throw new Exception(_localizer["Backend_SessionNotFound"]);

            if (!session.GameSystemId.HasValue)
                throw new Exception(_localizer["Backend_SessionHasNoGameSystem"]);

            var playerSessionIds = await context.PlayerSessions
                .AsNoTracking()
                .Where(ps => ps.SessionId == sessionId)
                .Select(ps => ps.Id)
                .ToListAsync();

            var characters = await context.Characters
                .Where(c => normalizedCharacterIds.Contains(c.Id) && playerSessionIds.Contains(c.PlayerSessionId))
                .ToListAsync();

            if (characters.Count != normalizedCharacterIds.Count)
                throw new Exception(_localizer["Backend_InvalidCharacterSelectionForSession"]);

            var gameSystemId = session.GameSystemId.Value;

            var attributeDefinitions = await context.AttributeDefinitions
                .AsNoTracking()
                .Where(a => a.GameSystemId == gameSystemId)
                .ToListAsync();

            var gaugeDefinitions = await context.GaugeDefinitions
                .AsNoTracking()
                .Where(g => g.GameSystemId == gameSystemId)
                .ToListAsync();

            var derivedDefinitions = await context.DerivedStatDefinitions
                .AsNoTracking()
                .Where(d => d.GameSystemId == gameSystemId)
                .ToListAsync();

            var metricDefinitions = await context.MetricDefinitions
                .AsNoTracking()
                .Where(m => m.GameSystemId == gameSystemId)
                .ToListAsync();

            var talentDefinitions = await context.TalentDefinitions
                .AsNoTracking()
                .Where(t => t.GameSystemId == gameSystemId)
                .ToListAsync();

            var itemDefinitions = await context.ItemDefinitions
                .AsNoTracking()
                .Where(i => i.GameSystemId == gameSystemId)
                .ToListAsync();

            ValidateEffects(
                effects,
                attributeDefinitions,
                gaugeDefinitions,
                derivedDefinitions,
                metricDefinitions,
                talentDefinitions,
                itemDefinitions);

            var allAttributeValues = await context.CharacterAttributeValues
                .Where(v => normalizedCharacterIds.Contains(v.CharacterId))
                .ToListAsync();

            var allGaugeValues = await context.CharacterGaugeValues
                .Where(v => normalizedCharacterIds.Contains(v.CharacterId))
                .ToListAsync();

            var allCharacterTalents = await context.CharacterTalents
                .Where(v => normalizedCharacterIds.Contains(v.CharacterId))
                .ToListAsync();

            var allCharacterItems = await context.CharacterItems
                .Where(v => normalizedCharacterIds.Contains(v.CharacterId))
                .ToListAsync();

            var allCharacterTraitValues = await context.CharacterTraitValues
                .Where(v => normalizedCharacterIds.Contains(v.CharacterId))
                .ToListAsync();

            var metricDefinitionIds = metricDefinitions.Select(x => x.Id).ToList();

            var metricFormulaSteps = await context.MetricFormulaSteps
                .AsNoTracking()
                .Where(x => metricDefinitionIds.Contains(x.MetricDefinitionId))
                .ToListAsync();

            var metricComponents = await context.MetricComponents
                .AsNoTracking()
                .Where(x => metricDefinitionIds.Contains(x.MetricDefinitionId))
                .ToListAsync();

            var derivedDefinitionIds = derivedDefinitions.Select(x => x.Id).ToList();

            var derivedComponents = await context.DerivedStatComponents
                .AsNoTracking()
                .Where(x => derivedDefinitionIds.Contains(x.DerivedStatDefinitionId))
                .ToListAsync();

            var traitOptionIds = allCharacterTraitValues.Select(v => v.TraitOptionId).Distinct().ToList();
            var talentIds = allCharacterTalents.Select(v => v.TalentDefinitionId).Distinct().ToList();
            var itemIds = allCharacterItems.Select(v => v.ItemDefinitionId).Distinct().ToList();

            var choiceModifiers = await context.ChoiceOptionModifierDefinitions
                .AsNoTracking()
                .Where(x => traitOptionIds.Contains(x.TraitOptionId))
                .ToListAsync();

            var talentModifiers = await context.TalentModifierDefinitions
                .AsNoTracking()
                .Where(x => talentIds.Contains(x.TalentDefinitionId))
                .ToListAsync();

            var itemModifiers = await context.ItemModifierDefinitions
                .AsNoTracking()
                .Where(x => itemIds.Contains(x.ItemDefinitionId))
                .ToListAsync();

            var allCharacterModifiers = await context.CharacterModifiers
                .Where(v => normalizedCharacterIds.Contains(v.CharacterId))
                .ToListAsync();

            foreach (var character in characters)
            {
                var attributeValues = allAttributeValues.Where(x => x.CharacterId == character.Id).ToList();
                var gaugeValues = allGaugeValues.Where(x => x.CharacterId == character.Id).ToList();
                var characterTalents = allCharacterTalents.Where(x => x.CharacterId == character.Id).ToList();
                var characterItems = allCharacterItems.Where(x => x.CharacterId == character.Id).ToList();
                var characterModifiers = allCharacterModifiers.Where(x => x.CharacterId == character.Id).ToList();

                foreach (var effect in effects)
                {
                    await ApplySingleEffectAsync(
                        context,
                        character,
                        effect,
                        attributeDefinitions,
                        gaugeDefinitions,
                        derivedDefinitions,
                        metricDefinitions,
                        derivedComponents,
                        metricComponents,
                        metricFormulaSteps,
                        allCharacterTraitValues.Where(x => x.CharacterId == character.Id).ToList(),
                        choiceModifiers,
                        talentModifiers,
                        itemModifiers,
                        talentDefinitions,
                        itemDefinitions,
                        attributeValues,
                        gaugeValues,
                        characterTalents,
                        characterItems,
                        characterModifiers,
                        sourceType,
                        sourceId,
                        sourceName);
                }

                UpdateCharacterAliveState(character, gaugeDefinitions, gaugeValues);
            }

            await context.SaveChangesAsync();
        }

        public async Task<List<Guid>> ResolveTargetCharacterIdsAsync(Guid sessionId, CharacterTargetFilterDto filter)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var session = await context.Sessions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == sessionId);

            if (session == null)
                throw new Exception(_localizer["Backend_SessionNotFound"]);

            if (!session.GameSystemId.HasValue)
                throw new Exception(_localizer["Backend_SessionHasNoGameSystem"]);

            var playerSessions = await context.PlayerSessions
                .AsNoTracking()
                .Where(ps => ps.SessionId == sessionId)
                .ToListAsync();

            var playerSessionIds = playerSessions.Select(x => x.Id).ToList();

            var characters = await context.Characters
                .AsNoTracking()
                .Where(c => playerSessionIds.Contains(c.PlayerSessionId))
                .ToListAsync();

            if (characters.Count == 0)
                return new List<Guid>();

            var characterIds = characters.Select(x => x.Id).ToList();

            var traitValues = await context.CharacterTraitValues
                .AsNoTracking()
                .Where(x => characterIds.Contains(x.CharacterId))
                .ToListAsync();

            var characterTalents = await context.CharacterTalents
                .AsNoTracking()
                .Where(x => characterIds.Contains(x.CharacterId))
                .ToListAsync();

            var characterItems = await context.CharacterItems
                .AsNoTracking()
                .Where(x => characterIds.Contains(x.CharacterId))
                .ToListAsync();

            var gameSystemId = session.GameSystemId.Value;

            var attributeDefinitions = await context.AttributeDefinitions
                .AsNoTracking()
                .Where(x => x.GameSystemId == gameSystemId)
                .ToListAsync();

            var gaugeDefinitions = await context.GaugeDefinitions
                .AsNoTracking()
                .Where(x => x.GameSystemId == gameSystemId)
                .ToListAsync();

            var derivedDefinitions = await context.DerivedStatDefinitions
                .AsNoTracking()
                .Where(x => x.GameSystemId == gameSystemId)
                .ToListAsync();

            var metricDefinitions = await context.MetricDefinitions
                .AsNoTracking()
                .Where(x => x.GameSystemId == gameSystemId)
                .ToListAsync();

            var metricDefinitionIds = metricDefinitions.Select(x => x.Id).ToList();

            var metricFormulaSteps = await context.MetricFormulaSteps
                .AsNoTracking()
                .Where(x => metricDefinitionIds.Contains(x.MetricDefinitionId))
                .ToListAsync();

            var metricComponents = await context.MetricComponents
                .AsNoTracking()
                .Where(x => metricDefinitionIds.Contains(x.MetricDefinitionId))
                .ToListAsync();

            var attributeValues = await context.CharacterAttributeValues
                .AsNoTracking()
                .Where(x => characterIds.Contains(x.CharacterId))
                .ToListAsync();

            var gaugeValues = await context.CharacterGaugeValues
                .AsNoTracking()
                .Where(x => characterIds.Contains(x.CharacterId))
                .ToListAsync();

            var characterModifiers = await context.CharacterModifiers
                .AsNoTracking()
                .Where(x => characterIds.Contains(x.CharacterId))
                .ToListAsync();

            var traitOptionIds = traitValues.Select(tv => tv.TraitOptionId).Distinct().ToList();
            var talentIds = characterTalents.Select(ct => ct.TalentDefinitionId).Distinct().ToList();
            var itemIds = characterItems.Select(ci => ci.ItemDefinitionId).Distinct().ToList();

            var choiceModifiers = await context.ChoiceOptionModifierDefinitions
                .AsNoTracking()
                .Where(x => traitOptionIds.Contains(x.TraitOptionId))
                .ToListAsync();

            var talentModifiers = await context.TalentModifierDefinitions
                .AsNoTracking()
                .Where(x => talentIds.Contains(x.TalentDefinitionId))
                .ToListAsync();

            var itemModifiers = await context.ItemModifierDefinitions
                .AsNoTracking()
                .Where(x => itemIds.Contains(x.ItemDefinitionId))
                .ToListAsync();

            var derivedDefinitionIds = derivedDefinitions.Select(d => d.Id).ToList();

            var derivedComponents = await context.DerivedStatComponents
                .AsNoTracking()
                .Where(x => derivedDefinitionIds.Contains(x.DerivedStatDefinitionId))
                .ToListAsync();

            var filtered = new List<Guid>();

            foreach (var character in characters)
            {
                if (filter.OnlyAlive && !character.IsAlive)
                    continue;

                if (filter.OnlyDead && character.IsAlive)
                    continue;

                var playerSession = playerSessions.First(x => x.Id == character.PlayerSessionId);

                if (filter.OnlyOnline && !_presenceTracker.IsPlayerOnline(playerSession.Id))
                    continue;

                if (filter.TraitOptionIds.Count > 0)
                {
                    var ownedTraitOptionIds = traitValues
                        .Where(x => x.CharacterId == character.Id)
                        .Select(x => x.TraitOptionId)
                        .ToHashSet();

                    if (!filter.TraitOptionIds.All(x => ownedTraitOptionIds.Contains(x)))
                        continue;
                }

                if (filter.TalentIds.Count > 0)
                {
                    var ownedTalentIds = characterTalents
                        .Where(x => x.CharacterId == character.Id)
                        .Select(x => x.TalentDefinitionId)
                        .ToHashSet();

                    if (!filter.TalentIds.All(x => ownedTalentIds.Contains(x)))
                        continue;
                }

                if (filter.ItemIds.Count > 0)
                {
                    var ownedItemIds = characterItems
                        .Where(x => x.CharacterId == character.Id)
                        .Select(x => x.ItemDefinitionId)
                        .ToHashSet();

                    if (!filter.ItemIds.All(x => ownedItemIds.Contains(x)))
                        continue;
                }

                var valueFilterRejected = false;

                foreach (var valueFilter in filter.ValueFilters)
                {
                    var currentValue = ResolveCharacterValue(
                        character.Id,
                        valueFilter.TargetType,
                        valueFilter.TargetId,
                        attributeDefinitions,
                        gaugeDefinitions,
                        derivedDefinitions,
                        metricDefinitions,
                        derivedComponents,
                        metricComponents,
                        metricFormulaSteps,
                        attributeValues,
                        gaugeValues,
                        traitValues,
                        characterTalents,
                        characterItems,
                        choiceModifiers,
                        talentModifiers,
                        itemModifiers,
                        characterModifiers);

                    if (!CompareValue(currentValue, valueFilter.ComparisonType, valueFilter.Value))
                    {
                        valueFilterRejected = true;
                        break;
                    }
                }

                if (valueFilterRejected)
                    continue;

                // Ces deux filtres sont volontairement préparés pour 5B.
                // On les ignore ici pour éviter de figer un stockage transitoire.
                filtered.Add(character.Id);
            }

            return filtered;
        }

        public async Task<List<Character>> GetTargetCharactersAsync(Guid sessionId, CharacterTargetFilterDto filter)
        {
            var characterIds = await ResolveTargetCharacterIdsAsync(sessionId, filter);

            await using var context = await _contextFactory.CreateDbContextAsync();

            return await context.Characters
                .AsNoTracking()
                .Where(x => characterIds.Contains(x.Id))
                .OrderBy(x => x.Name)
                .ToListAsync();
        }

        public async Task<int> ApplyNamedEffectBatchAsync(
            Guid sessionId,
            string sourceName,
            CharacterEffectSourceType sourceType,
            Guid sourceId,
            CharacterTargetFilterDto filter,
            List<CharacterEffectDefinitionDto> effects)
        {
            if (string.IsNullOrWhiteSpace(sourceName))
                throw new Exception(_localizer["Backend_CharacterEffectBatchNameRequired"]);

            var targetCharacterIds = await ResolveTargetCharacterIdsAsync(sessionId, filter);

            await ApplyEffectsAsync(
                sessionId,
                targetCharacterIds,
                effects,
                sourceType,
                sourceId,
                sourceName.Trim());

            return targetCharacterIds.Count;
        }

        private void ValidateEffects(
            List<CharacterEffectDefinitionDto> effects,
            List<AttributeDefinition> attributeDefinitions,
            List<GaugeDefinition> gaugeDefinitions,
            List<DerivedStatDefinition> derivedDefinitions,
            List<MetricDefinition> metricDefinitions,
            List<TalentDefinition> talentDefinitions,
            List<ItemDefinition> itemDefinitions)
        {
            foreach (var effect in effects)
            {
                switch (effect.OperationType)
                {
                    case CharacterEffectOperationType.AddValue:
                        ValidateAddValueTarget(
                            effect,
                            attributeDefinitions,
                            gaugeDefinitions,
                            derivedDefinitions,
                            metricDefinitions);
                        break;

                    case CharacterEffectOperationType.GrantTalent:
                    case CharacterEffectOperationType.RevokeTalent:
                        if (effect.TargetType != CharacterEffectTargetType.Talent ||
                            !talentDefinitions.Any(x => x.Id == effect.TargetId))
                        {
                            throw new Exception(_localizer["Backend_InvalidCharacterEffectTalentTarget"]);
                        }
                        break;

                    case CharacterEffectOperationType.GrantItem:
                    case CharacterEffectOperationType.RevokeItem:
                        if (effect.TargetType != CharacterEffectTargetType.Item ||
                            !itemDefinitions.Any(x => x.Id == effect.TargetId))
                        {
                            throw new Exception(_localizer["Backend_InvalidCharacterEffectItemTarget"]);
                        }
                        break;

                    default:
                        throw new Exception(_localizer["Backend_InvalidCharacterEffectOperation"]);
                }
            }
        }

        private void ValidateAddValueTarget(
            CharacterEffectDefinitionDto effect,
            List<AttributeDefinition> attributeDefinitions,
            List<GaugeDefinition> gaugeDefinitions,
            List<DerivedStatDefinition> derivedDefinitions,
            List<MetricDefinition> metricDefinitions)
        {
            var isValid = effect.TargetType switch
            {
                CharacterEffectTargetType.BaseAttribute => attributeDefinitions.Any(x => x.Id == effect.TargetId),
                CharacterEffectTargetType.Gauge => gaugeDefinitions.Any(x => x.Id == effect.TargetId),
                CharacterEffectTargetType.DerivedStat => derivedDefinitions.Any(x => x.Id == effect.TargetId),
                CharacterEffectTargetType.Metric => metricDefinitions.Any(x => x.Id == effect.TargetId),
                _ => false
            };

            if (!isValid)
                throw new Exception(_localizer["Backend_InvalidCharacterEffectTarget"]);

            if (effect.ValueMode == ModifierValueMode.Metric && (!effect.SourceMetricId.HasValue || !metricDefinitions.Any(x => x.Id == effect.SourceMetricId.Value)))
                throw new Exception(_localizer["Backend_InvalidCharacterEffectSourceMetric"]);
        }

        private async Task ApplySingleEffectAsync(
            RollocracyDbContext context,
            Character character,
            CharacterEffectDefinitionDto effect,
            List<AttributeDefinition> attributeDefinitions,
            List<GaugeDefinition> gaugeDefinitions,
            List<DerivedStatDefinition> derivedDefinitions,
            List<MetricDefinition> metricDefinitions,
            List<DerivedStatComponent> derivedComponents,
            List<MetricComponent> metricComponents,
            List<MetricFormulaStep> metricFormulaSteps,
            List<CharacterTraitValue> traitValues,
            List<ChoiceOptionModifierDefinition> choiceModifiers,
            List<TalentModifierDefinition> talentModifiers,
            List<ItemModifierDefinition> itemModifiers,
            List<TalentDefinition> talentDefinitions,
            List<ItemDefinition> itemDefinitions,
            List<CharacterAttributeValue> attributeValues,
            List<CharacterGaugeValue> gaugeValues,
            List<CharacterTalent> characterTalents,
            List<CharacterItem> characterItems,
            List<CharacterModifier> characterModifiers,
            CharacterEffectSourceType sourceType,
            Guid sourceId,
            string sourceName)
        {
            switch (effect.OperationType)
            {
                case CharacterEffectOperationType.AddValue:
                    var resolvedEffectValue = ResolveEffectValue(
                        character.Id,
                        effect,
                        attributeDefinitions,
                        gaugeDefinitions,
                        derivedDefinitions,
                        metricDefinitions,
                        derivedComponents,
                        metricComponents,
                        metricFormulaSteps,
                        traitValues,
                        attributeValues,
                        gaugeValues,
                        characterTalents,
                        characterItems,
                        choiceModifiers,
                        talentModifiers,
                        itemModifiers,
                        characterModifiers);

                    if (effect.TargetType == CharacterEffectTargetType.BaseAttribute)
                    {
                        var definition = attributeDefinitions.First(x => x.Id == effect.TargetId);
                        var value = attributeValues.FirstOrDefault(x => x.AttributeDefinitionId == effect.TargetId);

                        if (value == null)
                        {
                            value = new CharacterAttributeValue
                            {
                                Id = Guid.NewGuid(),
                                CharacterId = character.Id,
                                AttributeDefinitionId = definition.Id,
                                Value = definition.DefaultValue
                            };

                            context.CharacterAttributeValues.Add(value);
                            attributeValues.Add(value);
                        }

                        value.Value = Math.Clamp(
                            value.Value + resolvedEffectValue,
                            definition.MinValue,
                            definition.MaxValue);
                    }
                    else if (effect.TargetType == CharacterEffectTargetType.Gauge)
                    {
                        var definition = gaugeDefinitions.First(x => x.Id == effect.TargetId);
                        var value = gaugeValues.FirstOrDefault(x => x.GaugeDefinitionId == effect.TargetId);

                        if (value == null)
                        {
                            value = new CharacterGaugeValue
                            {
                                Id = Guid.NewGuid(),
                                CharacterId = character.Id,
                                GaugeDefinitionId = definition.Id,
                                Value = definition.DefaultValue
                            };

                            context.CharacterGaugeValues.Add(value);
                            gaugeValues.Add(value);
                        }

                        value.Value = Math.Clamp(
                            value.Value + resolvedEffectValue,
                            definition.MinValue,
                            definition.MaxValue);
                    }
                    else
                    {
                        var existingModifier = characterModifiers.FirstOrDefault(x =>
                            x.TargetType == effect.TargetType &&
                            x.TargetId == effect.TargetId &&
                            x.SourceType == sourceType &&
                            x.SourceId == sourceId);

                        if (existingModifier == null)
                        {
                            existingModifier = new CharacterModifier
                            {
                                Id = Guid.NewGuid(),
                                CharacterId = character.Id,
                                TargetType = effect.TargetType,
                                TargetId = effect.TargetId,
                                AddValue = resolvedEffectValue,
                                SourceType = sourceType,
                                SourceId = sourceId,
                                SourceNameSnapshot = sourceName,
                                CreatedAtUtc = DateTime.UtcNow
                            };

                            context.CharacterModifiers.Add(existingModifier);
                            characterModifiers.Add(existingModifier);
                        }
                        else
                        {
                            existingModifier.AddValue += resolvedEffectValue;
                            existingModifier.SourceNameSnapshot = sourceName;
                        }
                    }
                    break;

                case CharacterEffectOperationType.GrantTalent:
                    if (!characterTalents.Any(x => x.TalentDefinitionId == effect.TargetId))
                    {
                        var entity = new CharacterTalent
                        {
                            Id = Guid.NewGuid(),
                            CharacterId = character.Id,
                            TalentDefinitionId = effect.TargetId
                        };

                        context.CharacterTalents.Add(entity);
                        characterTalents.Add(entity);
                    }
                    break;

                case CharacterEffectOperationType.RevokeTalent:
                    var talent = characterTalents.FirstOrDefault(x => x.TalentDefinitionId == effect.TargetId);
                    if (talent != null)
                    {
                        context.CharacterTalents.Remove(talent);
                        characterTalents.Remove(talent);
                    }
                    break;

                case CharacterEffectOperationType.GrantItem:
                    if (!characterItems.Any(x => x.ItemDefinitionId == effect.TargetId))
                    {
                        var entity = new CharacterItem
                        {
                            Id = Guid.NewGuid(),
                            CharacterId = character.Id,
                            ItemDefinitionId = effect.TargetId
                        };

                        context.CharacterItems.Add(entity);
                        characterItems.Add(entity);
                    }
                    break;

                case CharacterEffectOperationType.RevokeItem:
                    var item = characterItems.FirstOrDefault(x => x.ItemDefinitionId == effect.TargetId);
                    if (item != null)
                    {
                        context.CharacterItems.Remove(item);
                        characterItems.Remove(item);
                    }
                    break;

                default:
                    throw new Exception(_localizer["Backend_InvalidCharacterEffectOperation"]);
            }

            await Task.CompletedTask;
        }

        private void UpdateCharacterAliveState(
            Character character,
            List<GaugeDefinition> gaugeDefinitions,
            List<CharacterGaugeValue> gaugeValues)
        {
            var healthGaugeDefinitions = gaugeDefinitions
                .Where(x => x.IsHealthGauge)
                .ToList();

            if (healthGaugeDefinitions.Count == 0)
                return;

            var isAlive = healthGaugeDefinitions.All(definition =>
            {
                var value = gaugeValues.FirstOrDefault(x => x.GaugeDefinitionId == definition.Id)?.Value
                    ?? definition.DefaultValue;

                return value > 0;
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

        private int ResolveCharacterValue(
            Guid characterId,
            CharacterEffectTargetType targetType,
            Guid targetId,
            List<AttributeDefinition> attributeDefinitions,
            List<GaugeDefinition> gaugeDefinitions,
            List<DerivedStatDefinition> derivedDefinitions,
            List<MetricDefinition> metricDefinitions,
            List<DerivedStatComponent> derivedComponents,
            List<MetricComponent> metricComponents,
            List<MetricFormulaStep> metricFormulaSteps,
            List<CharacterAttributeValue> attributeValues,
            List<CharacterGaugeValue> gaugeValues,
            List<CharacterTraitValue> traitValues,
            List<CharacterTalent> characterTalents,
            List<CharacterItem> characterItems,
            List<ChoiceOptionModifierDefinition> choiceModifiers,
            List<TalentModifierDefinition> talentModifiers,
            List<ItemModifierDefinition> itemModifiers,
            List<CharacterModifier> characterModifiers)
        {
            var characterTraitOptionIds = traitValues
                .Where(tv => tv.CharacterId == characterId)
                .Select(tv => tv.TraitOptionId)
                .ToHashSet();

            var characterTalentIds = characterTalents
                .Where(ct => ct.CharacterId == characterId)
                .Select(ct => ct.TalentDefinitionId)
                .ToHashSet();

            var characterItemIds = characterItems
                .Where(ci => ci.CharacterId == characterId)
                .Select(ci => ci.ItemDefinitionId)
                .ToHashSet();

            var choiceRuntimeModifiers = choiceModifiers
                .Where(x => characterTraitOptionIds.Contains(x.TraitOptionId))
                .Select(x => new RuntimeModifier
                {
                    TargetType = x.TargetType,
                    TargetId = x.TargetId,
                    AddValue = x.AddValue,
                    ValueMode = x.ValueMode,
                    SourceMetricId = x.SourceMetricId
                });

            var talentRuntimeModifiers = talentModifiers
                .Where(x => characterTalentIds.Contains(x.TalentDefinitionId))
                .Select(x => new RuntimeModifier
                {
                    TargetType = x.TargetType,
                    TargetId = x.TargetId,
                    AddValue = x.AddValue,
                    ValueMode = x.ValueMode,
                    SourceMetricId = x.SourceMetricId
                });

            var itemRuntimeModifiers = itemModifiers
                .Where(x => characterItemIds.Contains(x.ItemDefinitionId))
                .Select(x => new RuntimeModifier
                {
                    TargetType = x.TargetType,
                    TargetId = x.TargetId,
                    AddValue = x.AddValue,
                    ValueMode = x.ValueMode,
                    SourceMetricId = x.SourceMetricId
                });

            var persistentRuntimeModifiers = characterModifiers
                .Where(x => x.CharacterId == characterId &&
                            (x.TargetType == CharacterEffectTargetType.BaseAttribute ||
                             x.TargetType == CharacterEffectTargetType.DerivedStat ||
                             x.TargetType == CharacterEffectTargetType.Metric))
                .Select(x => new RuntimeModifier
                {
                    TargetType = x.TargetType switch
                    {
                        CharacterEffectTargetType.BaseAttribute => ModifierTargetType.BaseAttribute,
                        CharacterEffectTargetType.DerivedStat => ModifierTargetType.DerivedStat,
                        CharacterEffectTargetType.Metric => ModifierTargetType.Metric,
                        _ => ModifierTargetType.BaseAttribute
                    },
                    TargetId = x.TargetId,
                    AddValue = x.AddValue,
                    ValueMode = ModifierValueMode.Fixed
                });

            var rawModifiers = choiceRuntimeModifiers
                .Concat(talentRuntimeModifiers)
                .Concat(itemRuntimeModifiers)
                .Concat(persistentRuntimeModifiers)
                .ToList();

            Dictionary<Guid, int> effectiveBaseValues = attributeDefinitions.ToDictionary(
                definition => definition.Id,
                definition =>
                {
                    var baseValue = attributeValues.FirstOrDefault(x =>
                        x.CharacterId == characterId &&
                        x.AttributeDefinitionId == definition.Id)?.Value ?? definition.DefaultValue;

                    var modifierValue = rawModifiers
                        .Where(x => x.ValueMode != ModifierValueMode.Metric && x.TargetType == ModifierTargetType.BaseAttribute && x.TargetId == definition.Id)
                        .Sum(x => x.AddValue);

                    return Math.Clamp(baseValue + modifierValue, definition.MinValue, definition.MaxValue);
                });

            var effectiveGaugeValues = gaugeDefinitions.ToDictionary(
                definition => definition.Id,
                definition =>
                {
                    var value = gaugeValues.FirstOrDefault(x => x.CharacterId == characterId && x.GaugeDefinitionId == definition.Id)?.Value
                        ?? definition.DefaultValue;

                    return Math.Clamp(value, definition.MinValue, definition.MaxValue);
                });

            var preliminaryDerivedValues = derivedDefinitions.ToDictionary(
                definition => definition.Id,
                definition => ResolveDerivedStatValue(definition.Id, derivedDefinitions, derivedComponents, effectiveBaseValues, rawModifiers.Where(x => x.ValueMode != ModifierValueMode.Metric).ToList()));

            var allModifiers = ResolveRuntimeModifiers(
                rawModifiers,
                metricDefinitions,
                metricComponents,
                metricFormulaSteps,
                effectiveBaseValues,
                effectiveGaugeValues,
                preliminaryDerivedValues);

            effectiveBaseValues = attributeDefinitions.ToDictionary(
                definition => definition.Id,
                definition =>
                {
                    var baseValue = attributeValues.FirstOrDefault(x =>
                        x.CharacterId == characterId &&
                        x.AttributeDefinitionId == definition.Id)?.Value ?? definition.DefaultValue;

                    var modifierValue = allModifiers
                        .Where(x => x.TargetType == ModifierTargetType.BaseAttribute && x.TargetId == definition.Id)
                        .Sum(x => x.AddValue);

                    return Math.Clamp(baseValue + modifierValue, definition.MinValue, definition.MaxValue);
                });

            var derivedValues = derivedDefinitions.ToDictionary(
                definition => definition.Id,
                definition => ResolveDerivedStatValue(definition.Id, derivedDefinitions, derivedComponents, effectiveBaseValues, allModifiers));

            return targetType switch
            {
                CharacterEffectTargetType.BaseAttribute => effectiveBaseValues.TryGetValue(targetId, out var baseValue) ? baseValue : 0,
                CharacterEffectTargetType.Gauge => gaugeValues.FirstOrDefault(x =>
                        x.CharacterId == characterId &&
                        x.GaugeDefinitionId == targetId)?.Value
                    ?? gaugeDefinitions.FirstOrDefault(x => x.Id == targetId)?.DefaultValue
                    ?? 0,
                CharacterEffectTargetType.DerivedStat => ResolveDerivedStatValue(targetId, derivedDefinitions, derivedComponents, effectiveBaseValues, allModifiers),
                CharacterEffectTargetType.Metric => ResolveMetricValue(
                    targetId,
                    metricDefinitions,
                    metricComponents,
                    metricFormulaSteps,
                    effectiveBaseValues,
                    effectiveGaugeValues,
                    derivedValues,
                    allModifiers),
                _ => 0
            };
        }

        private static int ResolveDerivedStatValue(
            Guid targetId,
            List<DerivedStatDefinition> definitions,
            List<DerivedStatComponent> components,
            Dictionary<Guid, int> effectiveBaseValues,
            List<RuntimeModifier> allModifiers)
        {
            var definition = definitions.FirstOrDefault(x => x.Id == targetId);
            if (definition == null)
                return 0;

            decimal rawValue = 0m;

            foreach (var component in components.Where(x => x.DerivedStatDefinitionId == definition.Id))
            {
                var sourceValue = effectiveBaseValues.TryGetValue(component.AttributeDefinitionId, out var value) ? value : 0;
                rawValue += sourceValue * (component.Weight / 100m);
            }

            rawValue = ApplyRoundMode(rawValue, definition.RoundMode);

            var modifierValue = allModifiers
                .Where(x => x.TargetType == ModifierTargetType.DerivedStat && x.TargetId == definition.Id)
                .Sum(x => x.AddValue);

            var finalValue = (int)rawValue + modifierValue;
            return Math.Clamp(finalValue, definition.MinValue, definition.MaxValue);
        }

        private static int ResolveMetricValue(
            Guid targetId,
            List<MetricDefinition> definitions,
            List<MetricComponent> components,
            List<MetricFormulaStep> formulaSteps,
            Dictionary<Guid, int> effectiveBaseValues,
            Dictionary<Guid, int> effectiveGaugeValues,
            Dictionary<Guid, int> derivedStatValues,
            List<RuntimeModifier> allModifiers)
        {
            return MetricFormulaEngine.ComputeSingle(new MetricFormulaEngine.MetricComputationRequest
            {
                MetricDefinitions = definitions,
                FormulaSteps = formulaSteps,
                LegacyComponents = components,
                BaseAttributeValues = effectiveBaseValues,
                GaugeValues = effectiveGaugeValues,
                DerivedStatValues = derivedStatValues,
                Modifiers = allModifiers.Select(x => new MetricFormulaEngine.ModifierValue
                {
                    TargetType = x.TargetType,
                    TargetId = x.TargetId,
                    AddValue = x.AddValue
                }).ToList()
            }, targetId);
        }

        private int ResolveEffectValue(
            Guid characterId,
            CharacterEffectDefinitionDto effect,
            List<AttributeDefinition> attributeDefinitions,
            List<GaugeDefinition> gaugeDefinitions,
            List<DerivedStatDefinition> derivedDefinitions,
            List<MetricDefinition> metricDefinitions,
            List<DerivedStatComponent> derivedComponents,
            List<MetricComponent> metricComponents,
            List<MetricFormulaStep> metricFormulaSteps,
            List<CharacterTraitValue> traitValues,
            List<CharacterAttributeValue> attributeValues,
            List<CharacterGaugeValue> gaugeValues,
            List<CharacterTalent> characterTalents,
            List<CharacterItem> characterItems,
            List<ChoiceOptionModifierDefinition> choiceModifiers,
            List<TalentModifierDefinition> talentModifiers,
            List<ItemModifierDefinition> itemModifiers,
            List<CharacterModifier> characterModifiers)
        {
            if (effect.ValueMode != ModifierValueMode.Metric || !effect.SourceMetricId.HasValue)
                return effect.Value;

            var metricValue = ResolveCharacterValue(
                characterId,
                CharacterEffectTargetType.Metric,
                effect.SourceMetricId.Value,
                attributeDefinitions,
                gaugeDefinitions,
                derivedDefinitions,
                metricDefinitions,
                derivedComponents,
                metricComponents,
                metricFormulaSteps,
                attributeValues,
                gaugeValues,
                traitValues,
                characterTalents,
                characterItems,
                choiceModifiers,
                talentModifiers,
                itemModifiers,
                characterModifiers);

            // En 6D, pour un effet basé sur une metric, on réutilise le signe porté
            // par Value : >= 0 pour un bonus, < 0 pour un malus.
            var sign = effect.Value < 0 ? -1 : 1;
            return metricValue * sign;
        }

        private static List<RuntimeModifier> ResolveRuntimeModifiers(
            List<RuntimeModifier> rawModifiers,
            List<MetricDefinition> metricDefinitions,
            List<MetricComponent> metricComponents,
            List<MetricFormulaStep> formulaSteps,
            Dictionary<Guid, int> attributeValues,
            Dictionary<Guid, int> gaugeValues,
            Dictionary<Guid, int> derivedValues)
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

        private static decimal ApplyRoundMode(decimal rawValue, ComputedValueRoundMode roundMode)
        {
            return roundMode switch
            {
                ComputedValueRoundMode.Ceiling => Math.Ceiling(rawValue),
                ComputedValueRoundMode.Floor => Math.Floor(rawValue),
                ComputedValueRoundMode.Nearest => Math.Round(rawValue, 0, MidpointRounding.AwayFromZero),
                _ => rawValue
            };
        }

        private static bool CompareValue(int currentValue, CharacterValueComparisonType comparisonType, int expectedValue)
        {
            return comparisonType switch
            {
                CharacterValueComparisonType.Equal => currentValue == expectedValue,
                CharacterValueComparisonType.NotEqual => currentValue != expectedValue,
                CharacterValueComparisonType.GreaterThan => currentValue > expectedValue,
                CharacterValueComparisonType.GreaterThanOrEqual => currentValue >= expectedValue,
                CharacterValueComparisonType.LessThan => currentValue < expectedValue,
                CharacterValueComparisonType.LessThanOrEqual => currentValue <= expectedValue,
                _ => false
            };
        }

        private sealed class RuntimeModifier
        {
            public ModifierTargetType TargetType { get; set; }
            public Guid TargetId { get; set; }
            public int AddValue { get; set; }
            public ModifierValueMode ValueMode { get; set; }
            public Guid? SourceMetricId { get; set; }
        }
    }
}
