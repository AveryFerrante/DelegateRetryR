using System;
using System.Collections.Generic;
using System.Text;

namespace DelegateRetry.Logging
{
    public interface IDelegateRetryRLogger
    {
        public void Debug(string message);
        public void Debug(string message, params object[] messageParameters);
        public void Information(string message);
        public void Information(string message, params object[] messageParameters);
        public void Error(string message);
        public void Error(string message, params object[] messageParameters);

    }
}
