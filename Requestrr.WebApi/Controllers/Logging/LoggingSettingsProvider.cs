using Requestrr.WebApi.RequestrrBot;

namespace Requestrr.WebApi.Controllers.Logging
{
    public class LoggingSettingsProvider
    {
        public LoggingSettings Provide()
        {
            var settings = SettingsFile.Read();

            return new LoggingSettings
            {
                Enabled = settings.Logging?.Enabled ?? true,
                RetentionDays = settings.Logging?.RetentionDays ?? 90,
                DiscordLoggingEnabled = settings.Logging?.DiscordLoggingEnabled ?? false,
                DiscordChannelId = settings.Logging?.DiscordChannelId ?? string.Empty
            };
        }
    }
}
