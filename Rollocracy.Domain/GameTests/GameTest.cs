using System;
using Rollocracy.Domain.GameRules;

namespace Rollocracy.Domain.GameTests
{
    public class GameTest
    {
        public Guid Id { get; set; }

        public Guid SessionId { get; set; }

        public Guid AttributeDefinitionId { get; set; }

        public string AttributeNameSnapshot { get; set; } = string.Empty;

        public TestResolutionMode ResolutionModeSnapshot { get; set; }

        public int DiceCount { get; set; }

        public int DiceSides { get; set; }

        public int? SuccessThreshold { get; set; }

        public TestModifierMode ModifierMode { get; set; }

        public int DifficultyValue { get; set; }

        public TestTargetScope TargetScope { get; set; }

        public TestTraitFilterMode TraitFilterMode { get; set; }

        public bool IsClosed { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime AutoRollAtUtc { get; set; }

        public DateTime? ClosedAtUtc { get; set; }
    }
}