using System;

namespace Rollocracy.Domain.GameTests
{
    public class GameTestConsequence
    {
        public Guid Id { get; set; }

        public Guid GameTestId { get; set; }

        public TestConsequenceApplyOn ApplyOn { get; set; }

        public TestConsequenceTargetKind TargetKind { get; set; }

        public Guid TargetDefinitionId { get; set; }

        public string TargetNameSnapshot { get; set; } = string.Empty;

        // Pour Attribute / Gauge / DerivedStat / Metric
        public TestModifierMode ModifierMode { get; set; }

        // Pour Add / Remove value
        public int Value { get; set; }

        // Nouveau : type d'opération métier
        public TestConsequenceOperationType OperationType { get; set; }
    }
}