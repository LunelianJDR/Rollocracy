using System;

namespace Rollocracy.Domain.Entities
{
    public class SessionSettingsDto
    {
        public Guid SessionId { get; set; }

        public string SessionName { get; set; } = string.Empty;

        public string SessionSlug { get; set; } = string.Empty;

        public string SessionPassword { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        public string JoinUrl { get; set; } = string.Empty;
    }
}