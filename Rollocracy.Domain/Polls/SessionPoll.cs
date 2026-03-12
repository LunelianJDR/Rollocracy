using System;

namespace Rollocracy.Domain.Polls
{
    public class SessionPoll
    {
        public Guid Id { get; set; }

        public Guid SessionId { get; set; }

        public string Question { get; set; } = string.Empty;

        public bool IsClosed { get; set; }

        public bool ConsequencesApplied { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime? ClosedAtUtc { get; set; }
    }
}