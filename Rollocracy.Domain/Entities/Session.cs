using System;

namespace Rollocracy.Domain.Entities
{
    public class Session
    {
        public Guid Id { get; set; }

        public Guid GameMasterUserAccountId { get; set; }

        // Nullable : une session peut être créée avant qu'un système soit choisi
        public Guid? GameSystemId { get; set; }

        public string SessionName { get; set; } = string.Empty;

        public string SessionSlug { get; set; } = string.Empty;

        // Mot de passe de session optionnel
        public string SessionPassword { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}