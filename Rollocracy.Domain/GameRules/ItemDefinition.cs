
using System;
using System.Collections.Generic;

namespace Rollocracy.Domain.GameRules
{
    public class ItemDefinition
    {
        public Guid Id { get; set; }
        public Guid GameSystemId { get; set; }

        public string Name { get; set; } = "";
        public string? Description { get; set; }

        public int DisplayOrder { get; set; }

        public ICollection<ItemModifierDefinition> Modifiers { get; set; } = new List<ItemModifierDefinition>();
    }
}
