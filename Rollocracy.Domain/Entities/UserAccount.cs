using System;

namespace Rollocracy.Domain.Entities
{
    public class UserAccount
    {
        public Guid Id { get; set; }

        public string Username { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public bool IsGameMaster { get; set; }

        public bool IsTwitchLinked { get; set; }

        public string? TwitchLogin { get; set; }

        // Langue préférée de l'utilisateur
        // On part sur "fr" par défaut
        public string Language { get; set; } = "fr";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}