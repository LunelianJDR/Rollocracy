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

        public TestModifierMode ModifierMode { get; set; }

        public int Value { get; set; }
    }
}