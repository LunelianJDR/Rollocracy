using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Rollocracy.Domain.Entities;
using Rollocracy.Domain.Interfaces;
using Rollocracy.Infrastructure.Options;
using Rollocracy.Infrastructure.Persistence;

namespace Rollocracy.Infrastructure.Services
{
    public class AccountSecurityService : IAccountSecurityService
    {
        private const string PurposePasswordReset = "password_reset";
        private const string PurposeEmailVerification = "email_verification";

        private readonly IDbContextFactory<RollocracyDbContext> _contextFactory;
        private readonly IEmailService _emailService;
        private readonly IStringLocalizer _localizer;
        private readonly EmailOptions _emailOptions;
        private readonly PasswordHasher<UserAccount> _passwordHasher = new();

        public AccountSecurityService(
            IDbContextFactory<RollocracyDbContext> contextFactory,
            IEmailService emailService,
            IStringLocalizerFactory localizerFactory,
            IOptions<EmailOptions> emailOptions)
        {
            _contextFactory = contextFactory;
            _emailService = emailService;
            _localizer = localizerFactory.Create("Rollocracy.Localization.SharedTexts", "Rollocracy");
            _emailOptions = emailOptions.Value;
        }

        public async Task ChangePasswordAsync(
            Guid userAccountId,
            string currentPassword,
            string newPassword)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var user = await context.UserAccounts
                .FirstOrDefaultAsync(x => x.Id == userAccountId);

            if (user == null)
                throw new Exception(_localizer["Backend_UserAccountNotFound"]);

            if (string.IsNullOrWhiteSpace(user.Email))
                throw new Exception(_localizer["Backend_PasswordChangeRequiresEmail"]);

            if (string.IsNullOrWhiteSpace(currentPassword))
                throw new Exception(_localizer["Backend_CurrentPasswordRequired"]);

            ValidateNewPassword(newPassword);

            var verification = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, currentPassword);
            if (verification != PasswordVerificationResult.Success)
                throw new Exception(_localizer["Backend_CurrentPasswordInvalid"]);

            user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);
            user.LastSensitiveChangeAtUtc = DateTime.UtcNow;
            user.LastSensitiveChangeType = "SecurityHistory_PasswordChanged";

            await context.SaveChangesAsync();
        }

        public async Task RequestPasswordResetAsync(string emailOrUsername)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var normalized = (emailOrUsername ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalized))
                return;

            UserAccount? user;

            if (normalized.Contains('@'))
            {
                var normalizedEmail = normalized.ToLowerInvariant();

                user = await context.UserAccounts
                    .FirstOrDefaultAsync(x => x.Email == normalizedEmail);
            }
            else
            {
                user = await context.UserAccounts
                    .FirstOrDefaultAsync(x => x.Username == normalized);
            }

            if (user == null || string.IsNullOrWhiteSpace(user.Email))
                return;

            var rawToken = CreateRawToken();
            var tokenHash = HashToken(rawToken);

            var entity = new AccountSecurityToken
            {
                Id = Guid.NewGuid(),
                UserAccountId = user.Id,
                Purpose = PurposePasswordReset,
                TokenHash = tokenHash,
                EmailSnapshot = user.Email,
                CreatedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.AddHours(2)
            };

            context.AccountSecurityTokens.Add(entity);
            await context.SaveChangesAsync();

            var resetUrl = BuildAbsoluteUrl($"/reset-password?token={Uri.EscapeDataString(rawToken)}");

            var body =
$@"{_localizer["Email_ResetPassword_BodyLine1"]}

{resetUrl}

{_localizer["Email_ResetPassword_BodyLine2"]}";

            await _emailService.SendAsync(
                user.Email,
                _localizer["Email_ResetPassword_Subject"],
                body,
                _emailOptions.FromNoReply);
        }

        public async Task ResetPasswordAsync(string token, string newPassword)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            ValidateNewPassword(newPassword);

            var tokenEntity = await GetValidTokenAsync(context, token, PurposePasswordReset);
            if (tokenEntity == null)
                throw new Exception(_localizer["Backend_ResetTokenInvalidOrExpired"]);

            var user = await context.UserAccounts
                .FirstOrDefaultAsync(x => x.Id == tokenEntity.UserAccountId);

            if (user == null)
                throw new Exception(_localizer["Backend_UserAccountNotFound"]);

            user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);
            user.LastSensitiveChangeAtUtc = DateTime.UtcNow;
            user.LastSensitiveChangeType = "SecurityHistory_PasswordReset";

            tokenEntity.ConsumedAtUtc = DateTime.UtcNow;

            var otherOpenResetTokens = await context.AccountSecurityTokens
                .Where(x =>
                    x.UserAccountId == user.Id &&
                    x.Purpose == PurposePasswordReset &&
                    x.ConsumedAtUtc == null &&
                    x.Id != tokenEntity.Id)
                .ToListAsync();

            foreach (var other in otherOpenResetTokens)
            {
                other.ConsumedAtUtc = DateTime.UtcNow;
            }

            await context.SaveChangesAsync();
        }

        public async Task SendEmailVerificationAsync(Guid userAccountId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var user = await context.UserAccounts
                .FirstOrDefaultAsync(x => x.Id == userAccountId);

            if (user == null)
                throw new Exception(_localizer["Backend_UserAccountNotFound"]);

            if (string.IsNullOrWhiteSpace(user.Email))
                throw new Exception(_localizer["Backend_EmailRequired"]);

            if (user.IsEmailVerified)
                return;

            var rawToken = CreateRawToken();
            var tokenHash = HashToken(rawToken);

            var entity = new AccountSecurityToken
            {
                Id = Guid.NewGuid(),
                UserAccountId = user.Id,
                Purpose = PurposeEmailVerification,
                TokenHash = tokenHash,
                EmailSnapshot = user.Email,
                CreatedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.AddHours(24)
            };

            context.AccountSecurityTokens.Add(entity);
            await context.SaveChangesAsync();

            var verifyUrl = BuildAbsoluteUrl($"/verify-email?token={Uri.EscapeDataString(rawToken)}");

            var body =
$@"{_localizer["Email_VerifyEmail_BodyLine1"]}

{verifyUrl}

{_localizer["Email_VerifyEmail_BodyLine2"]}";

            await _emailService.SendAsync(
                user.Email,
                _localizer["Email_VerifyEmail_Subject"],
                body,
                _emailOptions.FromNoReply);
        }

        public async Task VerifyEmailAsync(string token)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var tokenEntity = await GetValidTokenAsync(context, token, PurposeEmailVerification);
            if (tokenEntity == null)
                throw new Exception(_localizer["Backend_VerificationTokenInvalidOrExpired"]);

            var user = await context.UserAccounts
                .FirstOrDefaultAsync(x => x.Id == tokenEntity.UserAccountId);

            if (user == null)
                throw new Exception(_localizer["Backend_UserAccountNotFound"]);

            user.IsEmailVerified = true;
            user.LastSensitiveChangeAtUtc = DateTime.UtcNow;
            user.LastSensitiveChangeType = "SecurityHistory_EmailVerified";

            tokenEntity.ConsumedAtUtc = DateTime.UtcNow;

            var otherOpenVerificationTokens = await context.AccountSecurityTokens
                .Where(x =>
                    x.UserAccountId == user.Id &&
                    x.Purpose == PurposeEmailVerification &&
                    x.ConsumedAtUtc == null &&
                    x.Id != tokenEntity.Id)
                .ToListAsync();

            foreach (var other in otherOpenVerificationTokens)
            {
                other.ConsumedAtUtc = DateTime.UtcNow;
            }

            await context.SaveChangesAsync();
        }

        private async Task<AccountSecurityToken?> GetValidTokenAsync(
            RollocracyDbContext context,
            string rawToken,
            string purpose)
        {
            if (string.IsNullOrWhiteSpace(rawToken))
                return null;

            var tokenHash = HashToken(rawToken);

            return await context.AccountSecurityTokens
                .FirstOrDefaultAsync(x =>
                    x.TokenHash == tokenHash &&
                    x.Purpose == purpose &&
                    x.ConsumedAtUtc == null &&
                    x.ExpiresAtUtc > DateTime.UtcNow);
        }

        private void ValidateNewPassword(string newPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword))
                throw new Exception(_localizer["Backend_PasswordRequired"]);

            if (newPassword.Trim().Length < 6)
                throw new Exception(_localizer["Backend_PasswordTooShort"]);
        }

        private string BuildAbsoluteUrl(string relativePath)
        {
            var baseUrl = (_emailOptions.PublicBaseUrl ?? string.Empty).Trim().TrimEnd('/');

            if (string.IsNullOrWhiteSpace(baseUrl))
                throw new Exception("Email:PublicBaseUrl is not configured.");

            return $"{baseUrl}{relativePath}";
        }

        private static string CreateRawToken()
        {
            return $"{Guid.NewGuid():N}{Guid.NewGuid():N}";
        }

        private static string HashToken(string rawToken)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
            return Convert.ToHexString(bytes);
        }
    }
}