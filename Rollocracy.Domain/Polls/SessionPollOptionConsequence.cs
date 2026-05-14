using System;
using Rollocracy.Domain.GameTests;
using Rollocracy.Domain.GameRules;

namespace Rollocracy.Domain.Polls
{
    public class SessionPollOptionConsequence
    {
        public Guid Id { get; set; }

        public Guid SessionPollOptionId { get; set; }

        public TestConsequenceTargetKind TargetKind { get; set; }

        public Guid TargetDefinitionId { get; set; }

        public TestConsequenceApplicationMode ApplicationMode { get; set; } = TestConsequenceApplicationMode.ApplyOnce;

        public string TargetNameSnapshot { get; set; } = string.Empty;

        // Conservé pour compatibilité technique/transitoire.
        // En L-2, la valeur signée porte le sens métier.
        public TestModifierMode ModifierMode { get; set; } = TestModifierMode.Bonus;

        public int Value { get; set; }

        public ConsequenceClampMode ClampMode { get; set; } = ConsequenceClampMode.None;

        public int ClampMinTotal { get; set; } = -100;

        public int ClampMaxTotal { get; set; } = 100;

        public ModifierValueMode ValueMode { get; set; } = ModifierValueMode.Fixed;

        public Guid? SourceMetricId { get; set; }

        public TestConsequenceOperationType OperationType { get; set; }
    }
}