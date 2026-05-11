using System;
using System.Collections.Generic;
using System.Linq;
using Rollocracy.Domain.Characters;
using Rollocracy.Domain.GameRules;

namespace Rollocracy.Infrastructure.Services
{
    internal static class CharacterItemFamilyRules
    {
        internal static int GetOwnedCount(
            Guid characterId,
            Guid itemFamilyDefinitionId,
            IEnumerable<CharacterItem> characterItems,
            IEnumerable<ItemDefinition> itemDefinitions)
        {
            var definitionsById = itemDefinitions.ToDictionary(x => x.Id);

            return characterItems
                .Where(x => x.CharacterId == characterId)
                .Sum(characterItem =>
                {
                    if (!definitionsById.TryGetValue(characterItem.ItemDefinitionId, out var definition) ||
                        definition.ItemFamilyDefinitionId != itemFamilyDefinitionId)
                    {
                        return 0;
                    }

                    return definition.IsConsumable
                        ? Math.Max(0, characterItem.Quantity)
                        : 1;
                });
        }

        internal static int GetActiveCount(
            Guid characterId,
            Guid itemFamilyDefinitionId,
            IEnumerable<CharacterItem> characterItems,
            IEnumerable<ItemDefinition> itemDefinitions,
            Guid? excludedItemDefinitionId = null)
        {
            var definitionsById = itemDefinitions.ToDictionary(x => x.Id);

            return characterItems
                .Where(x =>
                    x.CharacterId == characterId &&
                    x.IsActive &&
                    (!excludedItemDefinitionId.HasValue || x.ItemDefinitionId != excludedItemDefinitionId.Value))
                .Count(characterItem =>
                {
                    if (!definitionsById.TryGetValue(characterItem.ItemDefinitionId, out var definition))
                        return false;

                    return !definition.IsConsumable &&
                           definition.ItemFamilyDefinitionId == itemFamilyDefinitionId;
                });
        }

        internal static bool CanAddItem(
            Guid characterId,
            ItemDefinition itemDefinition,
            int quantityToAdd,
            IEnumerable<CharacterItem> characterItems,
            IEnumerable<ItemDefinition> itemDefinitions,
            IEnumerable<ItemFamilyDefinition> itemFamilies,
            out string errorKey)
        {
            errorKey = string.Empty;
            var normalizedQuantityToAdd = Math.Max(1, quantityToAdd);
            var existingItem = characterItems.FirstOrDefault(x => x.CharacterId == characterId && x.ItemDefinitionId == itemDefinition.Id);

            if (itemDefinition.IsConsumable)
            {
                var maxQuantity = Math.Max(1, itemDefinition.MaxQuantityPerCharacter);
                var currentQuantity = existingItem?.Quantity ?? 0;

                if (currentQuantity + normalizedQuantityToAdd > maxQuantity)
                {
                    errorKey = "Backend_ItemMaxQuantityReached";
                    return false;
                }

                if (itemDefinition.ItemFamilyDefinitionId.HasValue)
                {
                    var family = itemFamilies.FirstOrDefault(x => x.Id == itemDefinition.ItemFamilyDefinitionId.Value);
                    if (family is not null)
                    {
                        var currentFamilyOwned = GetOwnedCount(characterId, family.Id, characterItems, itemDefinitions);
                        if (currentFamilyOwned + normalizedQuantityToAdd > family.MaxOwned)
                        {
                            errorKey = "Backend_ItemFamilyOwnedLimitReached";
                            return false;
                        }
                    }
                }

                return true;
            }

            if (existingItem is not null)
            {
                errorKey = "Backend_ItemAlreadyOwned";
                return false;
            }

            if (itemDefinition.ItemFamilyDefinitionId.HasValue)
            {
                var family = itemFamilies.FirstOrDefault(x => x.Id == itemDefinition.ItemFamilyDefinitionId.Value);
                if (family is not null)
                {
                    var currentFamilyOwned = GetOwnedCount(characterId, family.Id, characterItems, itemDefinitions);
                    if (currentFamilyOwned + 1 > family.MaxOwned)
                    {
                        errorKey = "Backend_ItemFamilyOwnedLimitReached";
                        return false;
                    }
                }
            }

            return true;
        }

        internal static bool ValidateOwnedLimits(
            Guid characterId,
            IEnumerable<CharacterItem> characterItems,
            IEnumerable<ItemDefinition> itemDefinitions,
            IEnumerable<ItemFamilyDefinition> itemFamilies,
            out string errorKey)
        {
            errorKey = string.Empty;
            var definitionsById = itemDefinitions.ToDictionary(x => x.Id);

            foreach (var characterItem in characterItems.Where(x => x.CharacterId == characterId))
            {
                if (!definitionsById.TryGetValue(characterItem.ItemDefinitionId, out var definition))
                    continue;

                if (definition.IsConsumable && characterItem.Quantity > Math.Max(1, definition.MaxQuantityPerCharacter))
                {
                    errorKey = "Backend_ItemMaxQuantityReached";
                    return false;
                }
            }

            foreach (var family in itemFamilies)
            {
                if (GetOwnedCount(characterId, family.Id, characterItems, itemDefinitions) > family.MaxOwned)
                {
                    errorKey = "Backend_ItemFamilyOwnedLimitReached";
                    return false;
                }
            }

            return true;
        }

        internal static bool ShouldActivateNewNonConsumableItem(
            Guid characterId,
            ItemDefinition itemDefinition,
            IEnumerable<CharacterItem> characterItems,
            IEnumerable<ItemDefinition> itemDefinitions,
            IEnumerable<ItemFamilyDefinition> itemFamilies)
        {
            if (itemDefinition.IsConsumable || !itemDefinition.ItemFamilyDefinitionId.HasValue)
                return true;

            var family = itemFamilies.FirstOrDefault(x => x.Id == itemDefinition.ItemFamilyDefinitionId.Value);
            if (family is null)
                return true;

            var activeCount = GetActiveCount(characterId, family.Id, characterItems, itemDefinitions);
            return activeCount < family.MaxActive;
        }

        internal static bool CanActivateItem(
            Guid characterId,
            CharacterItem characterItem,
            ItemDefinition itemDefinition,
            IEnumerable<CharacterItem> characterItems,
            IEnumerable<ItemDefinition> itemDefinitions,
            IEnumerable<ItemFamilyDefinition> itemFamilies,
            out string errorKey)
        {
            errorKey = string.Empty;

            if (itemDefinition.IsConsumable)
            {
                errorKey = "Backend_ConsumableItemCannotBeActivated";
                return false;
            }

            if (!itemDefinition.ItemFamilyDefinitionId.HasValue)
                return true;

            var family = itemFamilies.FirstOrDefault(x => x.Id == itemDefinition.ItemFamilyDefinitionId.Value);
            if (family is null)
                return true;

            var activeCount = GetActiveCount(
                characterId,
                family.Id,
                characterItems,
                itemDefinitions,
                characterItem.ItemDefinitionId);

            if (activeCount >= family.MaxActive)
            {
                errorKey = "Backend_ItemFamilyActiveLimitReached";
                return false;
            }

            return true;
        }

        internal static bool CanActivateItemFromSheet(
            Guid characterId,
            Guid itemDefinitionId,
            IEnumerable<CharacterItem> characterItems,
            IEnumerable<ItemDefinition> itemDefinitions,
            IEnumerable<ItemFamilyDefinition> itemFamilies)
        {
            var characterItem = characterItems.FirstOrDefault(x => x.CharacterId == characterId && x.ItemDefinitionId == itemDefinitionId);
            var itemDefinition = itemDefinitions.FirstOrDefault(x => x.Id == itemDefinitionId);

            if (characterItem is null || itemDefinition is null || itemDefinition.IsConsumable)
                return false;

            if (characterItem.IsActive)
                return true;

            return CanActivateItem(characterId, characterItem, itemDefinition, characterItems, itemDefinitions, itemFamilies, out _);
        }

        internal static bool NormalizeActiveItems(
            Guid characterId,
            IList<CharacterItem> characterItems,
            IReadOnlyCollection<ItemDefinition> itemDefinitions,
            IReadOnlyCollection<ItemFamilyDefinition> itemFamilies)
        {
            var changed = false;
            var definitionsById = itemDefinitions.ToDictionary(x => x.Id);

            foreach (var family in itemFamilies)
            {
                var activeItems = characterItems
                    .Where(x =>
                        x.CharacterId == characterId &&
                        x.IsActive &&
                        definitionsById.TryGetValue(x.ItemDefinitionId, out var definition) &&
                        !definition.IsConsumable &&
                        definition.ItemFamilyDefinitionId == family.Id)
                    .OrderBy(x => definitionsById[x.ItemDefinitionId].DisplayOrder)
                    .ThenBy(x => definitionsById[x.ItemDefinitionId].Name)
                    .ThenBy(x => x.Id)
                    .ToList();

                foreach (var itemToDisable in activeItems.Skip(Math.Max(0, family.MaxActive)))
                {
                    itemToDisable.IsActive = false;
                    changed = true;
                }
            }

            return changed;
        }
    }
}
