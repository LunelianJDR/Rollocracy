namespace Rollocracy.Domain.Interfaces
{
    public interface IAccountSecurityService
    {
        Task ChangePasswordAsync(
            Guid userAccountId,
            string currentPassword,
            string newPassword);

        Task RequestPasswordResetAsync(string emailOrUsername);

        Task ResetPasswordAsync(string token, string newPassword);

        Task SendEmailVerificationAsync(Guid userAccountId);

        Task VerifyEmailAsync(string token);
    }
}