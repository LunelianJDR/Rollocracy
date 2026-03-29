namespace Rollocracy.Infrastructure.Options
{
    public class EmailOptions
    {
        public string SmtpHost { get; set; } = string.Empty;
        public int SmtpPort { get; set; } = 587;
        public bool EnableSsl { get; set; } = true;

        public string? SmtpUsername { get; set; }
        public string? SmtpPassword { get; set; }

        public string FromNoReply { get; set; } = "no-reply@rollocracy.com";
        public string FromContact { get; set; } = "contact@rollocracy.com";

        public string PublicBaseUrl { get; set; } = string.Empty;
    }
}