using Newtonsoft.Json.Linq;
using Requestrr.WebApi.RequestrrBot;

namespace Requestrr.WebApi.Controllers.Logging
{
    public class LoggingSettingsRepository
    {
        public void Save(LoggingSettings model)
        {
            SettingsFile.Write(settings =>
            {
                if (settings["Logging"] == null)
                {
                    settings["Logging"] = new JObject();
                }

                settings["Logging"]["Enabled"] = model.Enabled;
                settings["Logging"]["RetentionDays"] = model.RetentionDays;
                settings["Logging"]["DiscordLoggingEnabled"] = model.DiscordLoggingEnabled;
                settings["Logging"]["DiscordChannelId"] = model.DiscordChannelId;
            });
        }
    }
}
