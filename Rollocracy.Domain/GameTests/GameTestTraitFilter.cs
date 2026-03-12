using System;

namespace Rollocracy.Domain.GameTests
{
    public class GameTestTraitFilter
    {
        public Guid Id { get; set; }

        public Guid GameTestId { get; set; }

        public Guid TraitDefinitionId { get; set; }

        public Guid TraitOptionId { get; set; }
    }
}