using System;

namespace Rollocracy.Domain.GameRules
{
    public class DerivedStatDefinition
    {
        public Guid Id { get; set; }

        public Guid GameSystemId { get; set; }

        public string Name { get; set; } = string.Empty;

        public int MinValue { get; set; }

        public int MaxValue { get; set; }

        public ComputedValueRoundMode RoundMode { get; set; } = ComputedValueRoundMode.Ceiling;

        public int DisplayOrder { get; set; }
    }
}
