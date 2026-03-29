using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Rollocracy.Domain.Entities;
using Rollocracy.Domain.Interfaces;
using Rollocracy.Infrastructure.Persistence;

namespace Rollocracy.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly IDbContextFactory<RollocracyDbContext> _contextFactory;
        private readonly IStringLocalizer _localizer;
        private readonly PasswordHasher<UserAccount> _passwordHasher = new();

        public AuthService(
            IDbContextFactory<RollocracyDbContext> contextFactory,
            IStringLocalizerFactory localizerFactory)
        {
            _contextFactory = contextFactory;
            _localizer = localizerFactory.Create("Rollocracy.Localization.SharedTexts", "Rollocracy");
        }

        public async Task<UserAccount> RegisterAsync(
            string username,
            string password,
            string? email,
            string language)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var normalizedUsername = NormalizeUsername(username);
            var normalizedEmail = NormalizeEmail(email);
            var normalizedLanguage = NormalizeLanguage(language);

            if (string.IsNullOrWhiteSpace(normalizedUsername))
                throw new Exception(_localizer["Backend_UsernameRequired"]);

            if (string.IsNullOrWhiteSpace(password))
                throw new Exception(_localizer["Backend_PasswordRequired"]);

            if (password.Trim().Length < 6)
                throw new Exception(_localizer["Backend_PasswordTooShort"]);

            var existingUser = await context.UserAccounts
                .FirstOrDefaultAsync(u => u.Username == normalizedUsername);

            if (existingUser != null)
                throw new Exception(_localizer["Backend_UsernameAlreadyExists"]);

            if (!string.IsNullOrWhiteSpace(normalizedEmail))
            {
                var existingEmailUser = await context.UserAccounts
                    .FirstOrDefaultAsync(u => u.Email == normalizedEmail);

                if (existingEmailUser != null)
                    throw new Exception(_localizer["Backend_EmailAlreadyExists"]);
            }

            var user = new UserAccount
            {
                Id = Guid.NewGuid(),
                Username = normalizedUsername,
                Email = normalizedEmail,
                IsEmailVerified = false,
                Language = normalizedLanguage,
                IsGameMaster = false,
                WantsToBeGameMaster = false,
                MaxPlayersPerSession = 0,
                LastSensitiveChangeAtUtc = DateTime.UtcNow,
                LastSensitiveChangeType = "Account_SecurityHistory_AccountCreated"
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, password);

            context.UserAccounts.Add(user);
            await context.SaveChangesAsync();

            return user;
        }

        public async Task<UserAccount?> ValidateLoginAsync(string username, string password)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var normalizedUsername = NormalizeUsername(username);

            var user = await context.UserAccounts
                .FirstOrDefaultAsync(u => u.Username == normalizedUsername);

            if (user == null)
                return null;

            // Un compte Twitch-only peut ne pas avoir de mot de passe local.
            if (string.IsNullOrWhiteSpace(user.PasswordHash))
                return null;

            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);

            return result == PasswordVerificationResult.Success ? user : null;
        }

        public async Task<UserAccount?> GetUserByUsernameAsync(string username)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var normalizedUsername = NormalizeUsername(username);

            return await context.UserAccounts
                .FirstOrDefaultAsync(u => u.Username == normalizedUsername);
        }

        public async Task<UserAccount?> GetUserByEmailAsync(string email)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var normalizedEmail = NormalizeEmail(email);

            if (string.IsNullOrWhiteSpace(normalizedEmail))
                return null;

            return await context.UserAccounts
                .FirstOrDefaultAsync(u => u.Email == normalizedEmail);
        }

        public async Task<UserAccount?> GetUserByIdAsync(Guid userId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            return await context.UserAccounts
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task<UserAccount?> GetUserByTwitchUserIdAsync(string twitchUserId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            if (string.IsNullOrWhiteSpace(twitchUserId))
                return null;

            return await context.UserAccounts
                .FirstOrDefaultAsync(u => u.TwitchUserId == twitchUserId);
        }

        public async Task<UserAccount> CreateTwitchAccountAsync(
            string username,
            string? email,
            string language,
            string twitchUserId,
            string twitchLogin,
            string? twitchDisplayName)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var normalizedUsername = NormalizeUsername(username);
            var normalizedEmail = NormalizeEmail(email);
            var normalizedLanguage = NormalizeLanguage(language);

            if (string.IsNullOrWhiteSpace(normalizedUsername))
                throw new Exception(_localizer["Backend_UsernameRequired"]);

            var existingUser = await context.UserAccounts
                .FirstOrDefaultAsync(u => u.Username == normalizedUsername);

            if (existingUser != null)
                throw new Exception(_localizer["Backend_UsernameAlreadyExists"]);

            if (!string.IsNullOrWhiteSpace(normalizedEmail))
            {
                var existingEmailUser = await context.UserAccounts
                    .FirstOrDefaultAsync(u => u.Email == normalizedEmail);

                if (existingEmailUser != null)
                    throw new Exception(_localizer["Backend_EmailAlreadyExists"]);
            }

            var existingTwitchUser = await context.UserAccounts
                .FirstOrDefaultAsync(u => u.TwitchUserId == twitchUserId);

            if (existingTwitchUser != null)
                return existingTwitchUser;

            var user = new UserAccount
            {
                Id = Guid.NewGuid(),
                Username = normalizedUsername,
                Email = normalizedEmail,
                IsEmailVerified = !string.IsNullOrWhiteSpace(normalizedEmail),
                Language = normalizedLanguage,
                IsGameMaster = false,
                WantsToBeGameMaster = false,
                MaxPlayersPerSession = 0,

                IsTwitchLinked = true,
                TwitchUserId = twitchUserId,
                TwitchLogin = twitchLogin?.Trim(),
                TwitchDisplayName = twitchDisplayName?.Trim(),

                // Compte créé via Twitch : pas de mot de passe local au départ
                PasswordHash = string.Empty,

                LastSensitiveChangeAtUtc = DateTime.UtcNow,
                LastSensitiveChangeType = "Account_SecurityHistory_TwitchAccountCreated"
            };

            context.UserAccounts.Add(user);
            await context.SaveChangesAsync();

            return user;
        }

        public async Task LinkTwitchToExistingAccountAsync(
            Guid userAccountId,
            string twitchUserId,
            string twitchLogin,
            string? twitchDisplayName,
            string? emailFromTwitch)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var user = await context.UserAccounts
                .FirstOrDefaultAsync(u => u.Id == userAccountId);

            if (user == null)
                throw new Exception(_localizer["Backend_UserAccountNotFound"]);

            var otherLinkedAccount = await context.UserAccounts
                .FirstOrDefaultAsync(u => u.TwitchUserId == twitchUserId && u.Id != userAccountId);

            if (otherLinkedAccount != null)
                throw new Exception(_localizer["Backend_TwitchAlreadyLinkedToAnotherAccount"]);

            user.IsTwitchLinked = true;
            user.TwitchUserId = twitchUserId;
            user.TwitchLogin = twitchLogin?.Trim();
            user.TwitchDisplayName = twitchDisplayName?.Trim();

            // Si le compte local n’avait pas d’email et que Twitch en fournit un, on le récupère.
            if (string.IsNullOrWhiteSpace(user.Email) && !string.IsNullOrWhiteSpace(emailFromTwitch))
            {
                var normalizedEmail = NormalizeEmail(emailFromTwitch);

                var existingEmailOwner = await context.UserAccounts
                    .FirstOrDefaultAsync(u => u.Email == normalizedEmail && u.Id != userAccountId);

                if (existingEmailOwner == null)
                {
                    user.Email = normalizedEmail;
                    user.IsEmailVerified = true;
                }
            }

            user.LastSensitiveChangeAtUtc = DateTime.UtcNow;
            user.LastSensitiveChangeType = "Account_SecurityHistory_TwitchLinked";

            await context.SaveChangesAsync();
        }

        public async Task<bool> IsUsernameAvailableAsync(string username)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var normalizedUsername = NormalizeUsername(username);

            if (string.IsNullOrWhiteSpace(normalizedUsername))
                return false;

            return !await context.UserAccounts
                .AnyAsync(u => u.Username == normalizedUsername);
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
