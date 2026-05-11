using System;

namespace Rollocracy.Domain.Characters
{
    public class CharacterItem
    {
        public Guid Id { get; set; }

        public Guid CharacterId { get; set; }

        public Guid ItemDefinitionId { get; set; }

        public int Quantity { get; set; } = 1;

        public bool IsActive { get; set; } = true;
    }
}