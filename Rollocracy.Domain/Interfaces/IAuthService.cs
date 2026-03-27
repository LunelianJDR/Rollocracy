using Rollocracy.Domain.Entities;

namespace Rollocracy.Domain.Interfaces
{
    public interface IAuthService
    {
        Task<UserAccount> RegisterAsync(
            string username,
            string password,
            string? email,
            string language);

        Task<UserAccount?> ValidateLoginAsync(string username, string password);

        Task<UserAccount?> GetUserByUsernameAsync(string username);

        Task<UserAccount?> GetUserByEmailAsync(string email);

        Task<UserAccount?> GetUserByIdAsync(Guid userId);

        Task<UserAccount?> GetUserByTwitchUserIdAsync(string twitchUserId);

        Task<UserAccount> CreateTwitchAccountAsync(
            string username,
            string? email,
            string language,
            string twitchUserId,
            string twitchLogin,
            string? twitchDisplayName);

        Task LinkTwitchToExistingAccountAsync(
            Guid userAccountId,
            string twitchUserId,
            string twitchLogin,
            string? twitchDisplayName,
            string? emailFromTwitch);

        Task<bool> IsUsernameAvailableAsync(string username);
    }
}