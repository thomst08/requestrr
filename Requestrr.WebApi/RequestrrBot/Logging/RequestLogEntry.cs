using System;
using Newtonsoft.Json;

namespace Requestrr.WebApi.RequestrrBot.Logging
{
    public class RequestLogEntry
    {
        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonProperty("userId")]
        public string UserId { get; set; }

        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("logType")]
        public RequestLogType LogType { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("details")]
        public string Details { get; set; }

        [JsonProperty("category")]
        public string Category { get; set; }

        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("reason")]
        public string Reason { get; set; }

        public RequestLogEntry()
        {
            Timestamp = DateTime.UtcNow;
        }
    }
}
