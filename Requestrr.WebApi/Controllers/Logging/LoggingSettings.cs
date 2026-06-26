using Newtonsoft.Json;

namespace Requestrr.WebApi.Controllers.Logging
{
    public class LoggingSettings
    {
        [JsonProperty("enabled")]
        public bool Enabled { get; set; } = true;

        [JsonProperty("retentionDays")]
        public int RetentionDays { get; set; } = 90;

        [JsonProperty("discordLoggingEnabled")]
        public bool DiscordLoggingEnabled { get; set; } = false;

        [JsonProperty("discordChannelId")]
        public string DiscordChannelId { get; set; } = string.Empty;
    }
}
