using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Requestrr.WebApi.RequestrrBot.Logging
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum RequestLogType
    {
        MovieRequest,
        MovieRequestDenied,
        MovieIssueReported,
        MovieNotificationSubscribed,
        TvShowRequest,
        TvShowRequestDenied,
        TvShowIssueReported,
        TvShowNotificationSubscribed,
        MusicRequest,
        MusicRequestDenied,
        MusicNotificationSubscribed
    }
}
