using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using Rollocracy.Domain.Interfaces;
using Rollocracy.Infrastructure.Options;

namespace Rollocracy.Infrastructure.Services
{
    public class SmtpEmailService : IEmailService
    {
        private readonly EmailOptions _options;

        public SmtpEmailService(IOptions<EmailOptions> options)
        {
            _options = options.Value;
        }

        public async Task SendAsync(
            string toEmail,
            string subject,
            string bodyText,
            string? fromEmail = null)
        {
            if (string.IsNullOrWhiteSpace(_options.SmtpHost))
                throw new Exception("SMTP host is not configured.");

            var resolvedFrom = string.IsNullOrWhiteSpace(fromEmail)
                ? _options.FromNoReply
                : fromEmail;

            using var message = new MailMessage
            {
                From = new MailAddress(resolvedFrom),
                Subject = subject,
                Body = bodyText,
                IsBodyHtml = false
            };

            message.To.Add(new MailAddress(toEmail));

            using var client = new SmtpClient(_options.SmtpHost, _options.SmtpPort)
            {
                EnableSsl = _options.EnableSsl
            };

            if (!string.IsNullOrWhiteSpace(_options.SmtpUsername))
            {
                client.Credentials = new NetworkCredential(
                    _options.SmtpUsername,
                    _options.SmtpPassword);
            }

            await client.SendMailAsync(message);
        }
    }
}