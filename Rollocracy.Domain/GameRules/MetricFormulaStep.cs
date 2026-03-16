using System;

namespace Rollocracy.Domain.GameRules
{
    public class MetricFormulaStep
    {
        public Guid Id { get; set; }

        public Guid MetricDefinitionId { get; set; }

        public int Order { get; set; }

        public MetricFormulaOperationType OperationType { get; set; } = MetricFormulaOperationType.Add;

        public MetricFormulaSourceType SourceType { get; set; } = MetricFormulaSourceType.Constant;

        // Utilisé pour BaseAttribute / Gauge / DerivedStat / Metric.
        public Guid? SourceId { get; set; }

        // Utilisé quand SourceType = Constant.
        public decimal ConstantValue { get; set; }
    }
}
