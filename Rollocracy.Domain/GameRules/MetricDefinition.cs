using System;

namespace Rollocracy.Domain.GameRules
{
    public class MetricDefinition
    {
        public Guid Id { get; set; }

        public Guid GameSystemId { get; set; }

        public string Name { get; set; } = string.Empty;

        public ComputedValueRoundMode RoundMode { get; set; } = ComputedValueRoundMode.Ceiling;

        public int DisplayOrder { get; set; }
    }
}
