using System;

using Rollocracy.Domain.GameRules;

namespace Rollocracy.Domain.GameTests
{
    public class GameTestConsequence
    {
        public Guid Id { get; set; }

        public Guid GameTestId { get; set; }

        public TestConsequenceApplyOn ApplyOn { get; set; }

        public TestConsequenceApplicationMode ApplicationMode { get; set; } = TestConsequenceApplicationMode.ApplyOnce;

        public TestConsequenceTargetKind TargetKind { get; set; }

        public Guid TargetDefinitionId { get; set; }

        public string TargetNameSnapshot { get; set; } = string.Empty;

        // Pour Attribute / Gauge / DerivedStat / Metric
        // Conservé pour compatibilité technique/transitoire.
        // En L-2, la valeur signée porte le sens métier.
        public TestModifierMode ModifierMode { get; set; } = TestModifierMode.Bonus;

        // Pour Add / Remove value
        public int Value { get; set; }

        public ConsequenceClampMode ClampMode { get; set; } = ConsequenceClampMode.None;

        public int ClampMinTotal { get; set; } = -100;

        public int ClampMaxTotal { get; set; } = 100;

        // 6D : valeur fixe ou metric source
        public ModifierValueMode ValueMode { get; set; } = ModifierValueMode.Fixed;

        // 6D : metric utilisée quand ValueMode = Metric
        public Guid? SourceMetricId { get; set; }

        // Nouveau : type d'opération métier
        public TestConsequenceOperationType OperationType { get; set; }
    }
}