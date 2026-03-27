using System;

namespace Rollocracy.Domain.Account
{
    public class AccountSettingsDto
    {
        public Guid UserId { get; set; }

        public string Username { get; set; } = string.Empty;

        public string? Email { get; set; }

        public bool HasEmail => !string.IsNullOrWhiteSpace(Email);

        public bool IsEmailVerified { get; set; }

        public string Language { get; set; } = "fr";

        public bool IsTwitchLinked { get; set; }

        public string? TwitchLogin { get; set; }

        public bool WantsToBeGameMaster { get; set; }

        public int MaxPlayersPerSession { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? LastSensitiveChangeAtUtc { get; set; }

        public string? LastSensitiveChangeType { get; set; }

        public bool CanActivateGameMasterMode => HasEmail;
    }

    public class UpdateAccountGeneralRequestDto
    {
        public string? Email { get; set; }
        public string Language { get; set; } = "fr";
    }

    public class UpdateAccountUsernameRequestDto
    {
        public string NewUsername { get; set; } = string.Empty;
    }

    public class UpdateAccountGameMasterModeRequestDto
    {
        public bool Enabled { get; set; }
    }
}