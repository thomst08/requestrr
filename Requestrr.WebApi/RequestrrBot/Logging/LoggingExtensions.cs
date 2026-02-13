using System;
using System.Net;
using Microsoft.Extensions.Logging;
using Requestrr.WebApi.config;

namespace Requestrr.WebApi.RequestrrBot.Logging
{
    public static class LoggingExtensions
    {
        public static void LogHttpRequest(this ILogger logger, DiagnosticsSettings diagnostics, string clientType, string method, string endpoint, HttpStatusCode statusCode, long milliseconds)
        {
            if (diagnostics?.ShouldLogHttpFor(clientType) == true)
            {
                logger.LogInformation($"{clientType} API: {method} {endpoint} - {(int)statusCode} {statusCode} in {milliseconds}ms");
            }
        }

        public static void LogHttpRequest(this ILogger logger, DiagnosticsSettings diagnostics, string clientType, string method, string endpoint, int statusCode, long milliseconds)
        {
            if (diagnostics?.ShouldLogHttpFor(clientType) == true)
            {
                logger.LogInformation($"{clientType} API: {method} {endpoint} - {statusCode} in {milliseconds}ms");
            }
        }

        public static void LogHttpError(this ILogger logger, string clientType, string endpoint, Exception ex, int attemptNumber = 1)
        {
            if (attemptNumber > 1)
            {
                logger.LogWarning(ex, $"{clientType} API request to {endpoint} failed (attempt {attemptNumber}): {ex.Message}");
            }
            else
            {
                logger.LogError(ex, $"{clientType} API request to {endpoint} failed: {ex.Message}");
            }
        }

        public static void LogWorkflowStart(this ILogger logger, string workflowType, string userId, string itemName)
        {
            logger.LogInformation($"{workflowType} workflow started: User {userId} searching for '{itemName}'");
        }

        public static void LogWorkflowComplete(this ILogger logger, string workflowType, long milliseconds, int? resultCount = null)
        {
            if (resultCount.HasValue)
            {
                logger.LogInformation($"{workflowType} workflow completed in {milliseconds}ms, found {resultCount} results");
            }
            else
            {
                logger.LogInformation($"{workflowType} workflow completed in {milliseconds}ms");
            }
        }

        public static void LogNotificationCycle(this ILogger logger, DiagnosticsSettings diagnostics, string notificationType, int checkedCount, int availableCount, int sentCount, long milliseconds)
        {
            if (diagnostics?.ShouldLogNotificationCycle(notificationType) == true)
            {
                logger.LogInformation($"{notificationType} notification cycle: Checked {checkedCount}, Available {availableCount}, Sent {sentCount} in {milliseconds}ms");
            }
        }

        public static void LogNotificationStart(this ILogger logger, DiagnosticsSettings diagnostics, string notificationType, int pendingCount)
        {
            if (diagnostics?.ShouldLogNotificationCycle(notificationType) == true)
            {
                logger.LogInformation($"{notificationType} notification cycle started, checking {pendingCount} pending notifications");
            }
        }

        public static void LogNotificationDelivered(this ILogger logger, string notificationType, string userId, string itemTitle)
        {
            logger.LogInformation($"{notificationType} notification sent: User {userId} notified for '{itemTitle}'");
        }

        public static void LogDiscordHeartbeat(this ILogger logger, DiagnosticsSettings diagnostics, int latency)
        {
            if (diagnostics?.ShouldLogDiscordHeartbeat() == true)
            {
                logger.LogInformation($"Heartbeat received, Latency: {latency}ms");
            }
        }

        public static void LogDiscordSlashCommand(this ILogger logger, DiagnosticsSettings diagnostics, string message)
        {
            if (diagnostics?.ShouldLogDiscordSlashCommands() == true)
            {
                logger.LogInformation(message);
            }
        }

        public static void LogDiscordConnection(this ILogger logger, DiagnosticsSettings diagnostics, string message)
        {
            if (diagnostics?.ShouldLogDiscordConnections() == true)
            {
                logger.LogInformation(message);
            }
        }
    }
}
