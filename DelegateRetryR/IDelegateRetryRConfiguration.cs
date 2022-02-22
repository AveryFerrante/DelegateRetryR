using System;

namespace DelegateRetry
{
    public interface IDelegateRetryRConfiguration
    {
        Predicate<int>? RetryConditional { get; }
        Func<int, int>? RetryDelay { get; }
    }

    public class DelegateRetryRConfiguration : IDelegateRetryRConfiguration
    {
        public Predicate<int>? RetryConditional { get; set; }

        public Func<int, int>? RetryDelay { get; set; }
    }
}
