using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Requestrr.WebApi.RequestrrBot.Logging
{
    public class PerformanceTimer : IDisposable
    {
        private readonly ILogger _logger;
        private readonly string _operationName;
        private readonly Stopwatch _stopwatch;
        private readonly LogLevel _logLevel;

        public PerformanceTimer(ILogger logger, string operationName, LogLevel logLevel = LogLevel.Information)
        {
            _logger = logger;
            _operationName = operationName;
            _logLevel = logLevel;
            _stopwatch = Stopwatch.StartNew();
        }

        public void Dispose()
        {
            _stopwatch.Stop();
            _logger.Log(_logLevel, $"{_operationName} completed in {_stopwatch.ElapsedMilliseconds}ms");
        }

        public long ElapsedMilliseconds => _stopwatch.ElapsedMilliseconds;
    }
}
