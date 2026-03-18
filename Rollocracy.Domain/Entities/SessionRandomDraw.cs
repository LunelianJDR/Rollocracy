using System;

namespace Rollocracy.Domain.Entities
{
    public class SessionRandomDraw
    {
        public Guid Id { get; set; }
        public Guid SessionId { get; set; }
        public Guid CreatedByUserAccountId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int RequestedCount { get; set; }
        public string ResultSnapshotJson { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
    }
}
