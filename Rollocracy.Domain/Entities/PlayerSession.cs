using System;

namespace Rollocracy.Domain.Entities
{
    public class PlayerSession
    {
        public Guid Id { get; set; }

        public Guid SessionId { get; set; }

        public Guid UserAccountId { get; set; }

        public string PlayerName { get; set; } = string.Empty;

        public bool IsGameMaster { get; set; }

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    }
}