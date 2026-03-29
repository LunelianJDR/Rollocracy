using System;

namespace Rollocracy.Domain.Account
{
    public class AccountSettingsDto
    {
        public Guid UserId { get; set; }

        public string Username { get; set; } = string.Empty;

        public string? Email { get; set; }

        public bool IsEmailVerified { get; set; }

        public string Language { get; set; } = "fr";

        public bool IsTwitchLinked { get; set; }

        public string? TwitchLogin { get; set; }

        public bool WantsToBeGameMaster { get; set; }

        public int MaxPlayersPerSession { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? LastSensitiveChangeAtUtc { get; set; }

        public string? LastSensitiveChangeType { get; set; }

        public Guid? CurrentSubscriptionPlanId { get; set; }

        public string? CurrentSubscriptionPlanCode { get; set; }

        public DateTime? SubscriptionStartedAtUtc { get; set; }

        public DateTime? NextSubscriptionRenewalAtUtc { get; set; }

        public Guid? PendingSubscriptionPlanId { get; set; }

        public string? PendingSubscriptionPlanCode { get; set; }

        public bool CancelSubscriptionAtRenewal { get; set; }

        public bool HasEmail => !string.IsNullOrWhiteSpace(Email);

        public bool CanActivateGameMasterMode => HasEmail;

        public bool HasActiveSubscription => CurrentSubscriptionPlanId.HasValue;
    }

    public class AccountSubscriptionPlanDto
    {
        public Guid PlanId { get; set; }

        public string Code { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public decimal MonthlyPriceTtc { get; set; }

        public int MaxPlayersPerSession { get; set; }

        public int DisplayOrder { get; set; }

        public bool IsCurrent { get; set; }

        public bool IsPending { get; set; }

        public bool IsFree => MonthlyPriceTtc <= 0;
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