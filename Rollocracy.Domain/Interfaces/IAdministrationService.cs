using System;
using System.Threading.Tasks;
using Rollocracy.Domain.Admin;

namespace Rollocracy.Domain.Interfaces
{
    public interface IAdministrationService
    {
        Task<bool> IsAdministratorAsync(Guid userAccountId);

        Task<AdminDashboardDto> GetDashboardAsync(Guid administratorUserAccountId, string baseUri);

        Task<HomeProjectContentDto> GetHomeProjectContentAsync(string language);

        Task<AdminPatchNoteDto> SavePatchNoteAsync(Guid administratorUserAccountId, AdminPatchNoteSaveDto request);

        Task DeletePatchNoteAsync(Guid administratorUserAccountId, Guid patchNoteId);

        Task<AdminPlannedFeatureDto> SavePlannedFeatureAsync(Guid administratorUserAccountId, AdminPlannedFeatureSaveDto request);

        Task DeletePlannedFeatureAsync(Guid administratorUserAccountId, Guid plannedFeatureId);

        Task UpdateUserAsync(Guid administratorUserAccountId, AdminUserUpdateDto request);

        Task SendUserEmailVerificationAsync(Guid administratorUserAccountId, Guid userAccountId);

        Task SendUserPasswordResetAsync(Guid administratorUserAccountId, Guid userAccountId);

        Task<AdminSubscriptionPlanDto> SaveSubscriptionPlanAsync(Guid administratorUserAccountId, AdminSubscriptionPlanSaveDto request);

        Task DeleteSubscriptionPlanAsync(Guid administratorUserAccountId, Guid subscriptionPlanId);

        Task MakeGameSystemGenericAsync(Guid administratorUserAccountId, Guid gameSystemId);

        Task DeleteGameSystemAsync(Guid administratorUserAccountId, Guid gameSystemId);

        Task SetSessionActiveAsync(Guid administratorUserAccountId, Guid sessionId, bool isActive);

        Task DeleteSessionAsync(Guid administratorUserAccountId, Guid sessionId);
    }
}
