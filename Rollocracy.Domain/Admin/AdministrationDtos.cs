using System;
using System.Collections.Generic;
using Rollocracy.Domain.GameRules;

namespace Rollocracy.Domain.Admin
{
    public class AdminDashboardDto
    {
        public int UserCount { get; set; }
        public int GameMasterAccountCount { get; set; }
        public int ActiveSessionCount { get; set; }
        public int SessionCount { get; set; }
        public int GameSystemCount { get; set; }
        public int CharacterCount { get; set; }
        public int AliveCharacterCount { get; set; }
        public string LatestUserName { get; set; } = string.Empty;
        public DateTime? LatestUserCreatedAtUtc { get; set; }

        public List<AdminPatchNoteDto> PatchNotes { get; set; } = new();
        public List<AdminPlannedFeatureDto> PlannedFeatures { get; set; } = new();
        public List<AdminUserDto> Users { get; set; } = new();
        public List<AdminSubscriptionPlanDto> SubscriptionPlans { get; set; } = new();
        public List<AdminGameSystemDto> GameSystems { get; set; } = new();
        public List<AdminSessionDto> Sessions { get; set; } = new();
    }

    public class AdminPatchNoteDto
    {
        public Guid Id { get; set; }
        public string Version { get; set; } = string.Empty;
        public bool IsPublished { get; set; }
        public int DisplayOrder { get; set; }
        public DateTime PublishedAtUtc { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
        public List<AdminPatchNoteEntryDto> Entries { get; set; } = new();
    }

    public class AdminPatchNoteEntryDto
    {
        public Guid Id { get; set; }
        public string TextFr { get; set; } = string.Empty;
        public string TextEn { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
    }

    public class AdminPatchNoteSaveDto
    {
        public Guid? Id { get; set; }
        public string Version { get; set; } = string.Empty;
        public bool IsPublished { get; set; } = true;
        public int DisplayOrder { get; set; }
        public DateTime? PublishedAtUtc { get; set; }
        public List<AdminPatchNoteEntryDto> Entries { get; set; } = new();
    }

    public class AdminPlannedFeatureDto
    {
        public Guid Id { get; set; }
        public string TextFr { get; set; } = string.Empty;
        public string TextEn { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
    }

    public class AdminPlannedFeatureSaveDto
    {
        public Guid? Id { get; set; }
        public string TextFr { get; set; } = string.Empty;
        public string TextEn { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class AdminUserDto
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? Email { get; set; }
        public bool IsEmailVerified { get; set; }
        public bool IsGameMaster { get; set; }
        public bool WantsToBeGameMaster { get; set; }
        public bool IsTwitchLinked { get; set; }
        public string? TwitchLogin { get; set; }
        public string Language { get; set; } = "fr";
        public int MaxPlayersPerSession { get; set; }
        public string? CurrentSubscriptionPlanCode { get; set; }
        public string? CurrentSubscriptionPlanName { get; set; }
        public Guid? CurrentSubscriptionPlanId { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }

    public class AdminUserUpdateDto
    {
        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? Email { get; set; }
        public bool IsEmailVerified { get; set; }
        public bool IsGameMaster { get; set; }
        public bool WantsToBeGameMaster { get; set; }
        public string Language { get; set; } = "fr";
        public int MaxPlayersPerSession { get; set; }
        public Guid? CurrentSubscriptionPlanId { get; set; }
    }

    public class AdminSubscriptionPlanDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal MonthlyPriceTtc { get; set; }
        public int MaxPlayersPerSession { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
        public int UserCount { get; set; }
    }

    public class AdminSubscriptionPlanSaveDto
    {
        public Guid? Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal MonthlyPriceTtc { get; set; }
        public int MaxPlayersPerSession { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class AdminGameSystemDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string CreatorUsername { get; set; } = string.Empty;
        public TestResolutionMode TestResolutionMode { get; set; }
        public int SessionsUsingCount { get; set; }
        public bool IsGeneric { get; set; }
        public bool IsLockedToSession { get; set; }
    }

    public class AdminSessionDto
    {
        public Guid Id { get; set; }
        public string GameMasterUsername { get; set; } = string.Empty;
        public string SessionName { get; set; } = string.Empty;
        public string SessionSlug { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string? GameSystemName { get; set; }
        public Guid? GameSystemId { get; set; }
        public int AliveCharacterCount { get; set; }
        public int TotalCharacterCount { get; set; }
        public string JoinUrl { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
    }

    public class PublicPatchNoteDto
    {
        public Guid Id { get; set; }
        public string Version { get; set; } = string.Empty;
        public DateTime PublishedAtUtc { get; set; }
        public List<string> Entries { get; set; } = new();
    }

    public class PublicPlannedFeatureDto
    {
        public Guid Id { get; set; }
        public string Text { get; set; } = string.Empty;
    }

    public class HomeProjectContentDto
    {
        public List<PublicPatchNoteDto> PatchNotes { get; set; } = new();
        public List<PublicPlannedFeatureDto> PlannedFeatures { get; set; } = new();
    }
}
