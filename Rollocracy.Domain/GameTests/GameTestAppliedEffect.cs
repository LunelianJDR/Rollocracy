using System;

namespace Rollocracy.Domain.GameTests
{
    // Historique d'effet appliqué par un test.
    // Sert au rollback complet du dernier test.
    public class GameTestAppliedEffect
    {
        public Guid Id { get; set; }

        public Guid GameTestId { get; set; }

        public Guid CharacterId { get; set; }

        public TestConsequenceTargetKind TargetKind { get; set; }

        public Guid TargetDefinitionId { get; set; }

        public TestConsequenceOperationType OperationType { get; set; }

        // Utilisé pour Attribute / Gauge
        public int PreviousValue { get; set; }

        public int NewValue { get; set; }

        // Utilisé pour Talent / Item
        public bool PreviousHasTargetLink { get; set; }

        public bool NewHasTargetLink { get; set; }

        public bool PreviousIsAlive { get; set; }

        public bool NewIsAlive { get; set; }

        public DateTime? PreviousDiedAtUtc { get; set; }

        public DateTime? NewDiedAtUtc { get; set; }

        public DateTime AppliedAtUtc { get; set; }
    }
}