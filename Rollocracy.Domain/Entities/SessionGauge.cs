using System;

namespace Rollocracy.Domain.Entities
{
    public class SessionGauge
    {
        public Guid Id { get; set; }

        public Guid SessionId { get; set; }

        public string Name { get; set; } = string.Empty;

        public int MinValue { get; set; }

        public int MaxValue { get; set; }

        public int CurrentValue { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}