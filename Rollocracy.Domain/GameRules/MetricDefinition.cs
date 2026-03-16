using System;

namespace Rollocracy.Domain.GameRules
{
    public enum MetricFormulaSourceType
    {
        BaseAttribute = 0,
        Gauge = 1,
        DerivedStat = 2,
        Metric = 3,
        Constant = 4
    }

    public enum MetricFormulaOperationType
    {
        Add = 0,
        Subtract = 1,
        Multiply = 2,
        Divide = 3
    }

    public class MetricDefinition
    {
        public Guid Id { get; set; }

        public Guid GameSystemId { get; set; }

        public string Name { get; set; } = string.Empty;

        // Legacy : conservé pour compatibilité transitoire / fallback.
        public int BaseValue { get; set; } = 0;

        public int MinValue { get; set; } = 0;

        public int MaxValue { get; set; } = 100;

        public ComputedValueRoundMode RoundMode { get; set; } = ComputedValueRoundMode.Ceiling;

        public int DisplayOrder { get; set; }
    }
}
