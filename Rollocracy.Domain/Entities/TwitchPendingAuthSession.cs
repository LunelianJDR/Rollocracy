using System;

namespace Rollocracy.Domain.Entities
{
    public class TwitchPendingAuthSession
    {
        public Guid Id { get; set; }

        // Token brut utilisé dans les URLs de retour/confirmation
        public string PublicToken { get; set; } = string.Empty;

        // State OAuth anti-CSRF
        public string OAuthState { get; set; } = string.Empty;

        // login | link | merge | complete
        public string FlowType { get; set; } = string.Empty;

        // Utilisateur connecté au moment où il demande une liaison
        public Guid? CurrentUserAccountId { get; set; }

        // Données Twitch récupérées après callback
        public string? TwitchUserId { get; set; }
        public string? TwitchLogin { get; set; }
        public string? TwitchDisplayName { get; set; }
        public string? TwitchEmail { get; set; }

        // Utilisateur local trouvé par email si conflit potentiel
        public Guid? MatchedUserAccountId { get; set; }

        // Conserve la langue courante pour faciliter la création
        public string Language { get; set; } = "fr";

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAtUtc { get; set; }
        public DateTime? ConsumedAtUtc { get; set; }
    }
}