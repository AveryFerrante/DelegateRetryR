using System;
using System.Collections.Generic;
using System.Text;

namespace DelegateRetry
{
    public class DelegateRetryRState
    {
        public int Attempt { get; private set; }
        public Guid JobId { get; private set; }
        public Exception? LastThrownException { get; set; }
        public bool HadSuccessfulRun { get; set; }
        private Predicate<int> RetryConditional { get; set; }
        private Func<int, int> RetryDelay { get; set; }
        private DelegateRetryRState(int attempt, Predicate<int> retryConditional, Func<int, int> retryDelay, Guid jobId)
        {
            Attempt = attempt;
            RetryConditional = retryConditional;
            RetryDelay = retryDelay;
            JobId = jobId;
            HadSuccessfulRun = false;
            LastThrownException = null;
        }

        public static DelegateRetryRState InitNewState(Predicate<int> retryConditional, Func<int, int> retryDelay)
        {
            return new DelegateRetryRState(1, retryConditional, retryDelay, Guid.NewGuid());
        }

        public int GetRetryDelay()
        {
            return RetryDelay(Attempt);
        }
        public bool ShouldRetry()
        {
            return !HadSuccessfulRun && RetryConditional(++Attempt);
        }
    }
}
