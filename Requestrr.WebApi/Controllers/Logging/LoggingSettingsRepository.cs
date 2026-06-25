using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using Requestrr.WebApi.RequestrrBot;

namespace Requestrr.WebApi.Controllers.Logging
{
    public class LoggingSettingsRepository
    {
        public void Save(LoggingSettings model)
        {
            var settings = JObject.Parse(File.ReadAllText(SettingsFile.FilePath));

            if (settings["Logging"] == null)
            {
                settings["Logging"] = new JObject();
            }

            settings["Logging"]["Enabled"] = model.Enabled;
            settings["Logging"]["RetentionDays"] = model.RetentionDays;
            settings["Logging"]["DiscordLoggingEnabled"] = model.DiscordLoggingEnabled;
            settings["Logging"]["DiscordChannelId"] = model.DiscordChannelId;

            File.WriteAllText(SettingsFile.FilePath, settings.ToString());
        }
    }
}
