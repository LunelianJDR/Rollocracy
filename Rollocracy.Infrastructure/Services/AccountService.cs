using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Rollocracy.Domain.Account;
using Rollocracy.Domain.Interfaces;
using Rollocracy.Infrastructure.Persistence;

namespace Rollocracy.Infrastructure.Services
{
    public class AccountService : IAccountService
    {
        private const int DefaultGameMasterCapacity = 15;

        private readonly IDbContextFactory<RollocracyDbContext> _contextFactory;
        private readonly IStringLocalizer _localizer;

        public AccountService(
            IDbContextFactory<RollocracyDbContext> contextFactory,
            IStringLocalizerFactory localizerFactory)
        {
            _contextFactory = contextFactory;
            _localizer = localizerFactory.Create("Rollocracy.Localization.SharedTexts", "Rollocracy");
        }

        public async Task<AccountSettingsDto> GetAccountSettingsAsync(Guid userAccountId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var user = await context.UserAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userAccountId);

            if (user == null)
                throw new Exception(_localizer["Backend_UserAccountNotFound"]);

            return new AccountSettingsDto
            {
                UserId = user.Id,
                Username = user.Username,
                Email = user.Email,
                IsEmailVerified = user.IsEmailVerified,
                Language = user.Language,
                IsTwitchLinked = user.IsTwitchLinked,
                TwitchLogin = user.TwitchLogin,
                WantsToBeGameMaster = user.WantsToBeGameMaster,
                MaxPlayersPerSession = user.MaxPlayersPerSession,
                CreatedAt = user.CreatedAt,
                LastSensitiveChangeAtUtc = user.LastSensitiveChangeAtUtc,
                LastSensitiveChangeType = user.LastSensitiveChangeType
            };
        }

        public async Task UpdateGeneralInfoAsync(
            Guid userAccountId,
            string? email,
            string language)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var user = await context.UserAccounts
                .FirstOrDefaultAsync(u => u.Id == userAccountId);

            if (user == null)
                throw new Exception(_localizer["Backend_UserAccountNotFound"]);

            var normalizedEmail = NormalizeEmail(email);
            var normalizedLanguage = NormalizeLanguage(language);

            if (!string.IsNullOrWhiteSpace(normalizedEmail))
            {
                var existingUserWithEmail = await context.UserAccounts
                    .FirstOrDefaultAsync(u => u.Email == normalizedEmail && u.Id != userAccountId);

                if (existingUserWithEmail != null)
                    throw new Exception(_localizer["Backend_EmailAlreadyExists"]);
            }

            var emailChanged = user.Email != normalizedEmail;
            var languageChanged = user.Language != normalizedLanguage;

            user.Email = normalizedEmail;
            user.Language = normalizedLanguage;

            if (emailChanged)
            {
                user.IsEmailVerified = false;
                user.LastSensitiveChangeAtUtc = DateTime.UtcNow;
                user.LastSensitiveChangeType = "Account_SecurityHistory_EmailChanged";
            }

            if (user.WantsToBeGameMaster && string.IsNullOrWhiteSpace(user.Email))
            {
                user.WantsToBeGameMaster = false;
                user.MaxPlayersPerSession = 0;
            }

            if (emailChanged || languageChanged)
                await context.SaveChangesAsync();
        }

        public async Task UpdateUsernameAsync(Guid userAccountId, string newUsername)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var user = await context.UserAccounts
                .FirstOrDefaultAsync(u => u.Id == userAccountId);

            if (user == null)
                throw new Exception(_localizer["Backend_UserAccountNotFound"]);

            if (string.IsNullOrWhiteSpace(user.Email))
                throw new Exception(_localizer["Backend_UsernameChangeRequiresEmail"]);

            var normalizedUsername = NormalizeUsername(newUsername);

            if (string.IsNullOrWhiteSpace(normalizedUsername))
                throw new Exception(_localizer["Backend_UsernameRequired"]);

            if (string.Equals(user.Username, normalizedUsername, StringComparison.Ordinal))
                return;

            var existingUser = await context.UserAccounts
                .FirstOrDefaultAsync(u => u.Username == normalizedUsername && u.Id != userAccountId);

            if (existingUser != null)
                throw new Exception(_localizer["Backend_UsernameAlreadyExists"]);

            user.Username = normalizedUsername;
            user.LastSensitiveChangeAtUtc = DateTime.UtcNow;
            user.LastSensitiveChangeType = "Account_SecurityHistory_UsernameChanged";

            await context.SaveChangesAsync();
        }

        public async Task SetGameMasterModeAsync(Guid userAccountId, bool enabled)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var user = await context.UserAccounts
                .FirstOrDefaultAsync(u => u.Id == userAccountId);

            if (user == null)
                throw new Exception(_localizer["Backend_UserAccountNotFound"]);

            if (enabled && string.IsNullOrWhiteSpace(user.Email))
                throw new Exception(_localizer["Backend_GameMasterModeRequiresEmail"]);

            user.WantsToBeGameMaster = enabled;

            if (enabled)
            {
                if (user.MaxPlayersPerSession <= 0)
                    user.MaxPlayersPerSession = DefaultGameMasterCapacity;
            }
            else
            {
                user.MaxPlayersPerSession = 0;
            }

            await context.SaveChangesAsync();
        }

        private static string NormalizeUsername(string username)
        {
            return username?.Trim() ?? string.Empty;
        }

        private static string? NormalizeEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return null;

            return email.Trim().ToLowerInvariant();
        }

        private static string NormalizeLanguage(string language)
        {
            if (string.IsNullOrWhiteSpace(language))
                return "fr";

            var normalized = language.Trim().ToLowerInvariant();

            return normalized switch
            {
                "fr" => "fr",
                "en" => "en",
                _ => "fr"
            };
        }
    }
}
