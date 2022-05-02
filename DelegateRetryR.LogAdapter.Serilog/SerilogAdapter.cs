using DelegateRetry.Logging;
using Serilog;
using System;

namespace DelegateRetry.LogAdapter.Serilog
{
    public class SerilogAdapter : IDelegateRetryRLogger
    {
        private ILogger _logger;

        public SerilogAdapter()
        {
            _logger = Log.Logger.ForContext<DelegateRetryR>();
        }

        public void Debug(string message)
        {
            _logger.Debug(message);
        }
        public void Debug(string message, params object[] messageParameters)
        {
            _logger.Debug(message, messageParameters);
        }

        public void Error(string message)
        {
            _logger.Error(message);
        }
        public void Error(string message, params object[] messageParameters)
        {
            _logger.Error(message, messageParameters);
        }

        public void Information(string message)
        {
            _logger.Information(message);
        }
        public void Information(string message, params object[] messageParameters)
        {
            _logger.Information(message, messageParameters);
        }
    }
}
