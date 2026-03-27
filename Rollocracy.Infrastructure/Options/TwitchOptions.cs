namespace Rollocracy.Infrastructure.Options
{
    public class TwitchOptions
    {
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;

        // URL publique de l’application
        public string PublicBaseUrl { get; set; } = "https://localhost:7252";

        // Callback unique Twitch
        public string CallbackPath { get; set; } = "/auth/twitch/callback";
    }
}