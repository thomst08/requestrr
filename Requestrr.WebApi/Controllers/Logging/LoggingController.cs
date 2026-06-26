using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Requestrr.WebApi.RequestrrBot.Logging;

namespace Requestrr.WebApi.Controllers.Logging
{
    [ApiController]
    [Authorize]
    [Route("/api/logs")]
    public class LoggingController : ControllerBase
    {
        private readonly LoggingSettingsProvider _loggingSettingsProvider;
        private readonly LoggingSettingsRepository _loggingSettingsRepository;

        public LoggingController(
            LoggingSettingsProvider loggingSettingsProvider,
            LoggingSettingsRepository loggingSettingsRepository)
        {
            _loggingSettingsProvider = loggingSettingsProvider;
            _loggingSettingsRepository = loggingSettingsRepository;
        }

        [HttpGet("settings")]
        public IActionResult GetSettings()
        {
            return Ok(_loggingSettingsProvider.Provide());
        }

        [HttpPost("settings")]
        public IActionResult SaveSettings([FromBody] LoggingSettings model)
        {
            _loggingSettingsRepository.Save(model);
            return Ok(new { ok = true });
        }

        [HttpGet]
        public IActionResult GetLogs([FromQuery] string userId = null, [FromQuery] string logType = null, [FromQuery] int days = 30)
        {
            var logs = RequestLogger.ReadLogs();

            // Filter by days
            var cutoffDate = DateTime.UtcNow.AddDays(-days);
            logs = logs.Where(l => l.Timestamp >= cutoffDate).ToList();

            // Filter by userId if provided
            if (!string.IsNullOrWhiteSpace(userId))
            {
                logs = logs.Where(l => l.UserId == userId || l.Username.Contains(userId, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // Filter by logType if provided
            if (!string.IsNullOrWhiteSpace(logType) && Enum.TryParse<RequestLogType>(logType, true, out var logTypeEnum))
            {
                logs = logs.Where(l => l.LogType == logTypeEnum).ToList();
            }

            // Return in reverse chronological order
            return Ok(logs.OrderByDescending(l => l.Timestamp).ToList());
        }

        [HttpGet("stats")]
        public IActionResult GetStats([FromQuery] int days = 30)
        {
            var logs = RequestLogger.ReadLogs();
            var cutoffDate = DateTime.UtcNow.AddDays(-days);
            var recentLogs = logs.Where(l => l.Timestamp >= cutoffDate).ToList();

            var stats = new
            {
                totalRequests = recentLogs.Count,
                movieRequests = recentLogs.Count(l => l.LogType == RequestLogType.MovieRequest),
                tvShowRequests = recentLogs.Count(l => l.LogType == RequestLogType.TvShowRequest),
                deniedRequests = recentLogs.Count(l => l.LogType == RequestLogType.MovieRequestDenied || l.LogType == RequestLogType.TvShowRequestDenied),
                issuesReported = recentLogs.Count(l => l.LogType == RequestLogType.MovieIssueReported || l.LogType == RequestLogType.TvShowIssueReported),
                topUsers = recentLogs
                    .GroupBy(l => new { l.UserId, l.Username })
                    .OrderByDescending(g => g.Count())
                    .Take(10)
                    .Select(g => new { userId = g.Key.UserId, username = g.Key.Username, count = g.Count() })
                    .ToList(),
                recentActivity = recentLogs
                    .OrderByDescending(l => l.Timestamp)
                    .Take(20)
                    .ToList()
            };

            return Ok(stats);
        }
    }
}
