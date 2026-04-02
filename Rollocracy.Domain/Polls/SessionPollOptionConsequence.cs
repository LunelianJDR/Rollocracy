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

        public string TargetNameSnapshot { get; set; } = string.Empty;

        // Conservé pour compatibilité technique/transitoire.
        // En L-2, la valeur signée porte le sens métier.
        public TestModifierMode ModifierMode { get; set; } = TestModifierMode.Bonus;

        public int Value { get; set; }

        public ModifierValueMode ValueMode { get; set; } = ModifierValueMode.Fixed;

        public Guid? SourceMetricId { get; set; }

        public TestConsequenceOperationType OperationType { get; set; }
    }
}