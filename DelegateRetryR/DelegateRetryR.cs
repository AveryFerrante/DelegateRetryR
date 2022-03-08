using DelegateRetry.Exceptions;
using DelegateRetryR.WorkRunner;
using Microsoft.Extensions.Logging;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace DelegateRetry
{
    public class DelegateRetryR : IDelegateRetryR
    {
        private IDelegateRetryRConfiguration? _configuration;
        private ILogger? _logger;

        public DelegateRetryR()
        {
            SetProperties(null, null);
        }
        public DelegateRetryR(IDelegateRetryRConfiguration config)
        {
            SetProperties(config, null);
        }
        public DelegateRetryR(ILogger logger)
        {
            SetProperties(null, logger);
        }
        public DelegateRetryR(IDelegateRetryRConfiguration config, ILogger logger)
        {
            SetProperties(config, logger);
        }
        public static IDelegateRetryR Configure(Action<DelegateRetryRConfiguration> configAction)
        {
            var configuration = new DelegateRetryRConfiguration();
            configAction?.Invoke(configuration);
            return new DelegateRetryR(configuration);
        }
        public static IDelegateRetryR ConfigureWithLogger(Action<DelegateRetryRConfiguration> configAction, ILogger logger)
        {
            var configuration = new DelegateRetryRConfiguration();
            configAction?.Invoke(configuration);
            return new DelegateRetryR(configuration, logger);
        }
        private void SetProperties(IDelegateRetryRConfiguration? config, ILogger? logger)
        {
            _configuration = config;
            _logger = logger;
        }
        public async Task RetryWorkAsync<TException>(Delegate action, object[]? parameters = null, Predicate<int>? retryConditional = null, Func<int, int>? retryDelay = null)
            where TException : Exception
        {
            var workRunner = new WorkRunnerWithoutReturn(action, parameters, isAsyncWork: false);
            await PerformWorkWithRetry<TException>(workRunner, retryConditional, retryDelay);
        }

        public async Task<TResult> RetryWorkAsync<TException, TResult>(Delegate action, object[]? parameters, Predicate<int>? retryConditional = null, Func<int, int>? retryDelay = null)
            where TException : Exception
        {
            var workRunner = new WorkRunnerWithReturn<TResult>(action, parameters, isAsyncWork: false);
            await PerformWorkWithRetry<TException>(workRunner, retryConditional, retryDelay);
            return workRunner.Result;
        }

        public async Task RetryAsyncWorkAsync<TException>(Delegate action, object[]? parameters = null, Predicate<int>? retryConditional = null, Func<int, int>? retryDelay = null)
            where TException : Exception
        {
            var workRunner = new WorkRunnerWithoutReturn(action, parameters, isAsyncWork: true);
            await PerformWorkWithRetry<TException>(workRunner, retryConditional, retryDelay);
        }

        public async Task<TResult> RetryAsyncWorkAsync<TException, TResult>(Delegate action, object[]? parameters, Predicate<int>? retryConditional = null, Func<int, int>? retryDelay = null) where TException : Exception
        {
            var workRunner = new WorkRunnerWithReturn<TResult>(action, parameters, isAsyncWork: true);
            await PerformWorkWithRetry<TException>(workRunner, retryConditional, retryDelay);
            return workRunner.Result;
        }
        private async Task PerformWorkWithRetry<TException>(WorkRunnerBase workRunner, Predicate<int>? retryConditional = null, Func<int, int>? retryDelay = null) where TException : Exception
        {
            Guid jobId = Guid.NewGuid();
            retryConditional = ResolveRetryConditional(retryConditional);
            retryDelay = ResolveRetryDelay(retryDelay);
            int attempt = 1;
            do
            {
                _logger?.LogDebug("[{JobId}] - Performing delegate work - attempt {Attempt}", jobId, attempt);
                try
                {
                    await workRunner.ExecuteWithDelay(retryDelay(attempt));
                    _logger?.LogInformation("[{JobId}] - Delegate work completed successfully in {Attempt} attempt(s)", jobId, attempt);
                    return;
                }
                catch (Exception e)
                {
                    e = ResolveWrappedDynamicInvokeExceptionIfApplicable(e);
                    ThrowIfUnexpectedError<TException>(e);
                    if (!retryConditional(attempt))
                    {
                        _logger?.LogInformation("[{JobId}] - Delegate work did not complete successfully in {Attempt} attempts. Bubbling up the exception.", jobId, attempt - 1);
                        throw e;
                    }
                }
            } while (retryConditional(attempt++));
        }

        private Exception ResolveWrappedDynamicInvokeExceptionIfApplicable(Exception e) 
        {
            if (e.GetType() == typeof(TargetInvocationException))
            {
                return e.InnerException;
            }
            return e;
        }

        private void ThrowIfUnexpectedError<TException>(Exception e) where TException : Exception
        {
            if (e.GetType() == typeof(TException) || e.GetType().IsSubclassOf(typeof(TException)))
            {
                _logger?.LogDebug("Thrown exception matches retry on exception type, will attempt a retry if conditional passes");
            }
            else
            {
                _logger?.LogDebug("Thrown exception does not match retry on exception type");
                throw e;
            }
        }

        private Predicate<int> ResolveRetryConditional(Predicate<int>? retryConditionalFromMethodInvocation)
        {
            if (retryConditionalFromMethodInvocation != null)
            {
                return retryConditionalFromMethodInvocation;
            }
            else if (_configuration?.RetryConditional != null)
            {
                return _configuration.RetryConditional;
            }
            else
            {
                return GetDefaultRetryConditional();
            }
        }

        private Predicate<int> GetDefaultRetryConditional()
        {
            return (attempt) => attempt < 3;
        }

        private Func<int, int> ResolveRetryDelay(Func<int, int>? retryDelay)
        {
            if (retryDelay != null)
            {
                return EnsureFirstRunIsInstantaneous(retryDelay);
            }
            else if (_configuration?.RetryDelay != null)
            {
                return EnsureFirstRunIsInstantaneous(_configuration.RetryDelay);
            }
            else
            {
                return EnsureFirstRunIsInstantaneous(GetDefaultRetryDelay());
            }
        }

        private Func<int, int> EnsureFirstRunIsInstantaneous(Func<int, int> retryDelay)
        {
            return (attempt) => attempt == 1 ? 0 : retryDelay(attempt);
        }

        private Func<int, int> GetDefaultRetryDelay()
        {
            return (attempt) => 500;
        }
    }
}
