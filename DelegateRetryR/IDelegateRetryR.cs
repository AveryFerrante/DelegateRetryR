using System;
using System.Threading.Tasks;

namespace DelegateRetry
{
    public interface IDelegateRetryR
    {
        Task RetryWorkAsync<TException>(Delegate action, object[]? parameters = null, Predicate<int>? retryConditional = null, Func<int, int>? retryDelay = null)
            where TException : Exception;
        Task<TResult> RetryWorkAsync<TException, TResult>(Delegate action, object[]? parameters, Predicate<int>? retryConditional = null, Func<int, int>? retryDelay = null)
            where TException : Exception;
        Task RetryAsyncWorkAsync<TException>(Delegate action, object[]? parameters, Predicate<int>? retryConditional = null, Func<int, int>? retryDelay = null)
            where TException : Exception;
        Task<TResult> RetryAsyncWorkAsync<TException, TResult>(Delegate action, object[]? parameters, Predicate<int>? retryConditional = null, Func<int, int>? retryDelay = null)
            where TException : Exception;
    }
}