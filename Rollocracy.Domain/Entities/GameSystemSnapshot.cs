using System;

namespace Rollocracy.Domain.Entities
{
    public class GameSystemSnapshot
    {
        public Guid Id { get; set; }

        public Guid GameSystemId { get; set; }

        public Guid OwnerUserAccountId { get; set; }

        public string SnapshotJson { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}