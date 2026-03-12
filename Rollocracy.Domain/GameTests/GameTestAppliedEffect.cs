using System;

namespace Rollocracy.Domain.GameTests
{
    public class GameTestAppliedEffect
    {
        public Guid Id { get; set; }

        public Guid GameTestId { get; set; }

        public Guid CharacterId { get; set; }

        public TestConsequenceTargetKind TargetKind { get; set; }

        public Guid TargetDefinitionId { get; set; }

        public int PreviousValue { get; set; }

        public int NewValue { get; set; }

        public bool PreviousIsAlive { get; set; }

        public bool NewIsAlive { get; set; }

        public DateTime? PreviousDiedAtUtc { get; set; }

        public DateTime? NewDiedAtUtc { get; set; }

        public DateTime AppliedAtUtc { get; set; } = DateTime.UtcNow;
    }
}