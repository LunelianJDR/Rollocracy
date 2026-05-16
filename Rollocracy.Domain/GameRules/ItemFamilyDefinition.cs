using System;

namespace Rollocracy.Domain.GameRules
{
    public class ItemFamilyDefinition
    {
        public Guid Id { get; set; }

        public Guid GameSystemId { get; set; }

        public string Name { get; set; } = string.Empty;

        public int MaxOwned { get; set; } = 1;

        public int MaxActive { get; set; }

        public int DisplayOrder { get; set; }
    }
}
