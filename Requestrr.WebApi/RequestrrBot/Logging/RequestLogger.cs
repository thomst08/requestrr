using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Requestrr.WebApi.Controllers.Logging;

namespace Requestrr.WebApi.RequestrrBot.Logging
{
    public class RequestLogger : IRequestLogger
    {
        private static readonly string LogDirectory = "logs";
        private static readonly string LogFilePath = Path.Combine(LogDirectory, "requests.json");
        private static readonly SemaphoreSlim _fileLock = new SemaphoreSlim(1, 1);
        private readonly LoggingSettingsProvider _loggingSettingsProvider;
        private readonly ILogger<RequestLogger> _logger;
        private DiscordClient _discordClient;

        public RequestLogger(LoggingSettingsProvider loggingSettingsProvider, ILogger<RequestLogger> logger)
        {
            _loggingSettingsProvider = loggingSettingsProvider;
            _logger = logger;

            if (!Directory.Exists(LogDirectory))
            {
                Directory.CreateDirectory(LogDirectory);
            }

            if (!File.Exists(LogFilePath))
            {
                File.WriteAllText(LogFilePath, "[]");
            }
        }

        public void SetDiscordClient(DiscordClient client)
        {
            _discordClient = client;
        }

        public async Task LogMovieRequestAsync(string userId, string username, string movieTitle, int theMovieDbId, string category, bool success, string reason = null)
        {
            var logEntry = new RequestLogEntry
            {
                UserId = userId,
                Username = username,
                LogType = success ? RequestLogType.MovieRequest : RequestLogType.MovieRequestDenied,
                Title = movieTitle,
                Details = $"TheMovieDb ID: {theMovieDbId}",
                Category = category,
                Success = success,
                Reason = reason
            };

            await WriteLogEntryAsync(logEntry);
        }

        public async Task LogTvShowRequestAsync(string userId, string username, string tvShowTitle, int tvDbId, string seasons, string category, bool success, string reason = null)
        {
            var logEntry = new RequestLogEntry
            {
                UserId = userId,
                Username = username,
                LogType = success ? RequestLogType.TvShowRequest : RequestLogType.TvShowRequestDenied,
                Title = tvShowTitle,
                Details = $"TvDb ID: {tvDbId}, Seasons: {seasons}",
                Category = category,
                Success = success,
                Reason = reason
            };

            await WriteLogEntryAsync(logEntry);
        }

        public async Task LogMovieIssueAsync(string userId, string username, string movieTitle, int theMovieDbId, string issueDescription)
        {
            var logEntry = new RequestLogEntry
            {
                UserId = userId,
                Username = username,
                LogType = RequestLogType.MovieIssueReported,
                Title = movieTitle,
                Details = $"TheMovieDb ID: {theMovieDbId}, Issue: {issueDescription}",
                Success = true
            };

            await WriteLogEntryAsync(logEntry);
        }

        public async Task LogTvShowIssueAsync(string userId, string username, string tvShowTitle, int tvDbId, string issueDescription)
        {
            var logEntry = new RequestLogEntry
            {
                UserId = userId,
                Username = username,
                LogType = RequestLogType.TvShowIssueReported,
                Title = tvShowTitle,
                Details = $"TvDb ID: {tvDbId}, Issue: {issueDescription}",
                Success = true
            };

            await WriteLogEntryAsync(logEntry);
        }

        public async Task LogMovieNotificationAsync(string userId, string username, string movieTitle, int theMovieDbId)
        {
            var logEntry = new RequestLogEntry
            {
                UserId = userId,
                Username = username,
                LogType = RequestLogType.MovieNotificationSubscribed,
                Title = movieTitle,
                Details = $"TheMovieDb ID: {theMovieDbId}",
                Success = true
            };

            await WriteLogEntryAsync(logEntry);
        }

        public async Task LogTvShowNotificationAsync(string userId, string username, string tvShowTitle, int tvDbId, string seasons)
        {
            var logEntry = new RequestLogEntry
            {
                UserId = userId,
                Username = username,
                LogType = RequestLogType.TvShowNotificationSubscribed,
                Title = tvShowTitle,
                Details = $"TvDb ID: {tvDbId}, Seasons: {seasons}",
                Success = true
            };

            await WriteLogEntryAsync(logEntry);
        }

        public async Task LogMusicRequestAsync(string userId, string username, string artistName, string artistId, bool success, string reason = null)
        {
            var logEntry = new RequestLogEntry
            {
                UserId = userId,
                Username = username,
                LogType = success ? RequestLogType.MusicRequest : RequestLogType.MusicRequestDenied,
                Title = artistName,
                Details = $"Artist ID: {artistId}",
                Success = success,
                Reason = reason
            };

            await WriteLogEntryAsync(logEntry);
        }

        public async Task LogMusicNotificationAsync(string userId, string username, string artistName, string artistId)
        {
            var logEntry = new RequestLogEntry
            {
                UserId = userId,
                Username = username,
                LogType = RequestLogType.MusicNotificationSubscribed,
                Title = artistName,
                Details = $"Artist ID: {artistId}",
                Success = true
            };

            await WriteLogEntryAsync(logEntry);
        }

        private async Task WriteLogEntryAsync(RequestLogEntry logEntry)
        {
            var settings = _loggingSettingsProvider.Provide();

            if (!settings.Enabled)
            {
                return;
            }

            try
            {
                // Write to file
                await WriteToFileAsync(logEntry, settings.RetentionDays);

                // Send to Discord if configured
                if (settings.DiscordLoggingEnabled && !string.IsNullOrWhiteSpace(settings.DiscordChannelId))
                {
                    await SendToDiscordAsync(logEntry, settings.DiscordChannelId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error writing request log: {ex.Message}");
            }
        }

        private async Task WriteToFileAsync(RequestLogEntry logEntry, int retentionDays)
        {
            await _fileLock.WaitAsync();
            try
            {
                var logs = new List<RequestLogEntry>();

                if (File.Exists(LogFilePath))
                {
                    var content = await File.ReadAllTextAsync(LogFilePath);
                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        logs = JsonConvert.DeserializeObject<List<RequestLogEntry>>(content) ?? new List<RequestLogEntry>();
                    }
                }

                // Add new entry
                logs.Add(logEntry);

                // Remove old entries based on retention
                if (retentionDays > 0)
                {
                    var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
                    logs = logs.Where(l => l.Timestamp >= cutoffDate).ToList();
                }

                // Write back to file
                var serializerSettings = new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented,
                    Converters = new List<JsonConverter> { new Newtonsoft.Json.Converters.StringEnumConverter() }
                };
                var json = JsonConvert.SerializeObject(logs, serializerSettings);
                await File.WriteAllTextAsync(LogFilePath, json);
            }
            finally
            {
                _fileLock.Release();
            }
        }

        private async Task SendToDiscordAsync(RequestLogEntry logEntry, string channelId)
        {
            try
            {
                if (_discordClient == null || !ulong.TryParse(channelId, out var channelIdUlong))
                {
                    return;
                }

                var channel = await _discordClient.GetChannelAsync(channelIdUlong);
                if (channel == null)
                {
                    return;
                }

                var embed = new DiscordEmbedBuilder()
                    .WithTitle(GetLogTypeTitle(logEntry.LogType))
                    .WithColor(GetLogTypeColor(logEntry.LogType))
                    .WithTimestamp(logEntry.Timestamp)
                    .AddField("User", $"{logEntry.Username} (`{logEntry.UserId}`)", inline: true)
                    .AddField("Title", logEntry.Title, inline: true);

                if (!string.IsNullOrWhiteSpace(logEntry.Category))
                {
                    embed.AddField("Category", logEntry.Category, inline: true);
                }

                if (!string.IsNullOrWhiteSpace(logEntry.Details))
                {
                    embed.AddField("Details", logEntry.Details, inline: false);
                }

                if (!logEntry.Success && !string.IsNullOrWhiteSpace(logEntry.Reason))
                {
                    embed.AddField("Reason", logEntry.Reason, inline: false);
                }

                await channel.SendMessageAsync(embed: embed.Build());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending log to Discord: {ex.Message}");
            }
        }

        private string GetLogTypeTitle(RequestLogType logType)
        {
            return logType switch
            {
                RequestLogType.MovieRequest => "🎬 Movie Requested",
                RequestLogType.MovieRequestDenied => "🚫 Movie Request Denied",
                RequestLogType.MovieIssueReported => "⚠️ Movie Issue Reported",
                RequestLogType.MovieNotificationSubscribed => "🔔 Movie Notification Subscribed",
                RequestLogType.TvShowRequest => "📺 TV Show Requested",
                RequestLogType.TvShowRequestDenied => "🚫 TV Show Request Denied",
                RequestLogType.TvShowIssueReported => "⚠️ TV Show Issue Reported",
                RequestLogType.TvShowNotificationSubscribed => "🔔 TV Show Notification Subscribed",
                RequestLogType.MusicRequest => "🎵 Music Requested",
                RequestLogType.MusicRequestDenied => "🚫 Music Request Denied",
                RequestLogType.MusicNotificationSubscribed => "🔔 Music Notification Subscribed",
                _ => "📝 Request Log"
            };
        }

        private DiscordColor GetLogTypeColor(RequestLogType logType)
        {
            return logType switch
            {
                RequestLogType.MovieRequest or RequestLogType.TvShowRequest or RequestLogType.MusicRequest => DiscordColor.Green,
                RequestLogType.MovieRequestDenied or RequestLogType.TvShowRequestDenied or RequestLogType.MusicRequestDenied => DiscordColor.Red,
                RequestLogType.MovieIssueReported or RequestLogType.TvShowIssueReported => DiscordColor.Orange,
                RequestLogType.MovieNotificationSubscribed or RequestLogType.TvShowNotificationSubscribed or RequestLogType.MusicNotificationSubscribed => DiscordColor.Blue,
                _ => DiscordColor.Gray
            };
        }

        public static List<RequestLogEntry> ReadLogs()
        {
            _fileLock.Wait();
            try
            {
                if (!File.Exists(LogFilePath))
                {
                    return new List<RequestLogEntry>();
                }

                var content = File.ReadAllText(LogFilePath);
                if (string.IsNullOrWhiteSpace(content))
                {
                    return new List<RequestLogEntry>();
                }

                return JsonConvert.DeserializeObject<List<RequestLogEntry>>(content) ?? new List<RequestLogEntry>();
            }
            finally
            {
                _fileLock.Release();
            }
        }
    }
}
