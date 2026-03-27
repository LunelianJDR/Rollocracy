using System;

namespace Rollocracy.Domain.Account
{
    public class TwitchCallbackResultDto
    {
        // sign_in | redirect_merge | redirect_complete | redirect_account
        public string Outcome { get; set; } = string.Empty;

        public Guid? UserAccountId { get; set; }

        // Token de session pending utilisé pour les écrans intermédiaires
        public string? PendingToken { get; set; }
    }

    public class CompleteTwitchRegistrationRequestDto
    {
        public string PendingToken { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string Language { get; set; } = "fr";
    }

    public class ConfirmTwitchMergeRequestDto
    {
        public string PendingToken { get; set; } = string.Empty;

        // Si vrai et si le pseudo Twitch est disponible, on renomme le compte local
        public bool RenameUsernameToTwitchLogin { get; set; }
    }

    public class TwitchPendingSessionDto
    {
        public string PendingToken { get; set; } = string.Empty;
        public string FlowType { get; set; } = string.Empty;

        public string? TwitchLogin { get; set; }
        public string? TwitchDisplayName { get; set; }
        public string? TwitchEmail { get; set; }

        public string? MatchedUsername { get; set; }
        public bool SuggestedUsernameAvailable { get; set; }
        public string SuggestedUsername { get; set; } = string.Empty;
        public string Language { get; set; } = "fr";
    }
}