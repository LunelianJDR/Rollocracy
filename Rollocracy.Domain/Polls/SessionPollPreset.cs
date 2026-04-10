using System;

namespace Rollocracy.Domain.Polls
{
    public class SessionPollPreset
    {
        public Guid Id { get; set; }

        public Guid SessionId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string PayloadJson { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}