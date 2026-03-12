using System;

namespace Rollocracy.Domain.Entities
{
    public class Character
    {
        public Guid Id { get; set; }

        public Guid PlayerSessionId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Biography { get; set; } = string.Empty;

        public bool IsAlive { get; set; }

        public DateTime? DiedAtUtc { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}