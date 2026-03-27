namespace Rollocracy.Domain.Interfaces
{
    public interface IEmailService
    {
        Task SendAsync(
            string toEmail,
            string subject,
            string bodyText,
            string? fromEmail = null);
    }
}