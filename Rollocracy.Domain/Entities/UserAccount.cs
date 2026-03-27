using System;

namespace Rollocracy.Domain.Entities
{
    public class UserAccount
    {
        public Guid Id { get; set; }

        public string Username { get; set; } = string.Empty;

        // Mot de passe local.
        // Pour un compte créé via Twitch, on autorise une valeur vide.
        public string PasswordHash { get; set; } = string.Empty;

        // Champ historique existant (hors scope du mode MJ produit).
        public bool IsGameMaster { get; set; }

        // Liaison Twitch
        public bool IsTwitchLinked { get; set; }

        // Identifiant stable Twitch (à préférer au login pour l’unicité)
        public string? TwitchUserId { get; set; }

        // Snapshot du login Twitch courant
        public string? TwitchLogin { get; set; }

        // Snapshot du display name Twitch
        public string? TwitchDisplayName { get; set; }

        public string? Email { get; set; }

        public bool IsEmailVerified { get; set; }

        // Souhait produit MJ
        public bool WantsToBeGameMaster { get; set; }

        public string Language { get; set; } = "fr";

        // JMS réel
        public int MaxPlayersPerSession { get; set; } = 0;

        public DateTime? LastSensitiveChangeAtUtc { get; set; }

        public string? LastSensitiveChangeType { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}