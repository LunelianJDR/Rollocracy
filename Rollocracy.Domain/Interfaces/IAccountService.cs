using Rollocracy.Domain.Account;

namespace Rollocracy.Domain.Interfaces
{
    public interface IAccountService
    {
        Task<AccountSettingsDto> GetAccountSettingsAsync(Guid userAccountId);

        Task UpdateGeneralInfoAsync(
            Guid userAccountId,
            string? email,
            string language);

        Task UpdateUsernameAsync(
            Guid userAccountId,
            string newUsername);

        Task SetGameMasterModeAsync(
            Guid userAccountId,
            bool enabled);
    }
}