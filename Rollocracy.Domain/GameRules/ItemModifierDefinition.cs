
using System;

namespace Rollocracy.Domain.GameRules
{
    public class ItemModifierDefinition
    {
        public Guid Id { get; set; }

        public Guid ItemDefinitionId { get; set; }

        public ModifierTargetType TargetType { get; set; }

        public Guid TargetId { get; set; }

        public int AddValue { get; set; }

        public ItemDefinition? ItemDefinition { get; set; }
    }
}
