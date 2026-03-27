using System;

namespace Rollocracy.Domain.Entities
{
    public class AccountSecurityToken
    {
        public Guid Id { get; set; }

        public Guid UserAccountId { get; set; }

        public string Purpose { get; set; } = string.Empty;

        public string TokenHash { get; set; } = string.Empty;

        public string? EmailSnapshot { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime ExpiresAtUtc { get; set; }

        public DateTime? ConsumedAtUtc { get; set; }
    }
}