using System;

namespace Rollocracy.Domain.Entities
{
    // Lot appliqué par le MJ via la distribution de masse.
    // Le snapshot JSON permet l'annulation complète du dernier lot.
    public class MassDistributionBatch
    {
        public Guid Id { get; set; }

        public Guid SessionId { get; set; }

        public Guid CreatedByUserAccountId { get; set; }

        public string Name { get; set; } = string.Empty;

        public int TargetCharacterCount { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public string FilterSnapshotJson { get; set; } = string.Empty;

        public string EffectsSnapshotJson { get; set; } = string.Empty;

        public string UndoSnapshotJson { get; set; } = string.Empty;

        public bool IsUndone { get; set; }

        public DateTime? UndoneAtUtc { get; set; }
    }
}
