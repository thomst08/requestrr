namespace Requestrr.WebApi.config
{
    public enum DiagnosticsLevel
    {
        Default = 0,  // No diagnostic logging
        Info = 1,     // Essential diagnostics (connections, slash commands, notification cycles)
        Verbose = 2   // Everything (HTTP requests, heartbeats, all details)
    }

    public class DiagnosticsSettings
    {
        public DiagnosticsLevel Level { get; set; } = DiagnosticsLevel.Default;

        // Helper methods for level-based checks
        public bool ShouldLogHttpFor(string clientType)
        {
            return Level == DiagnosticsLevel.Verbose;
        }

        public bool ShouldLogDiscordHeartbeat()
        {
            return Level == DiagnosticsLevel.Verbose;
        }

        public bool ShouldLogDiscordSlashCommands()
        {
            return Level >= DiagnosticsLevel.Info;
        }

        public bool ShouldLogDiscordConnections()
        {
            return Level >= DiagnosticsLevel.Info;
        }

        public bool ShouldLogNotificationCycle(string type)
        {
            return Level >= DiagnosticsLevel.Info;
        }
    }
}
