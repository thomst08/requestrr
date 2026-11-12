namespace Requestrr.WebApi.config
{
    public class ChatClientsSettings
    {
        public DiscordSettings Discord { get; set; }
        public string Language { get; set; }
    }

    public class DiscordSettings
    {
        public string BotToken { get; set; }
        public string ClientId { get; set; }
        public string StatusMessage { get; set; }
        public string[] TvShowRoles { get; set; }
        public string[] MovieRoles { get; set; }
        public string[] MusicRoles { get; set; }
        public string[] MonitoredChannels { get; set; }
        public string[] RequestApprovalAdminRoleIds { get; set; }
        public string[] RequestApprovalAdminChannelIds { get; set; }
        public bool RequestApprovalAdminChannelAllRequests { get; set; }
        public string RequestApprovalEmojiApprove { get; set; }
        public string RequestApprovalEmojiDeny { get; set; }
        public bool RequestApprovalEnabled { get; set; }
        public bool EnableRequestsThroughDirectMessages { get; set; }
        public bool AutomaticallyNotifyRequesters { get; set; }
        public string NotificationMode { get; set; }
        public string[] NotificationChannels { get; set; }
        public bool AutomaticallyPurgeCommandMessages { get; set; }
    }
}
