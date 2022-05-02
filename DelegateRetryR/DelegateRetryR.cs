using DelegateRetry.Logging;
using DelegateRetry.WorkRunner;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace DelegateRetry
{
    public class DelegateRetryR : IDelegateRetryR
    {
        private IDelegateRetryRConfiguration? _configuration;
        private static IDelegateRetryRLogger? _logger;

        public DelegateRetryR()
        {
            _configuration = null; 
        }
        public DelegateRetryR(IDelegateRetryRConfiguration config)
        {
            _configuration = config;
        }
        public static IDelegateRetryR Configure(Action<IDelegateRetryRConfiguration> configAction)
        {
            var configuration = new DelegateRetryRConfiguration();
            configAction?.Invoke(configuration);
            return new DelegateRetryR(configuration);
        }
        public static void UseLogger<T>() where T : IDelegateRetryRLogger
        {
            _logger = (IDelegateRetryRLogger)Activator.CreateInstance(typeof(T));
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
            var jobState = DelegateRetryRState.InitNewState(ResolveRetryConditional(retryConditional), ResolveRetryDelay(retryDelay));
            do
            {
                jobState = await TryRunningWork<TException>(workRunner, jobState);
            } while (jobState.ShouldRetry());

            if (jobState.HadSuccessfulRun)
            {
                _logger?.Information("[{JobId}] - Delegate work completed successfully in {Attempt} attempt(s)", jobState.JobId, jobState.Attempt);
            }
            else
            {
                PropagateException(jobState);
            }
        }

        private async Task<DelegateRetryRState> TryRunningWork<TException>(WorkRunnerBase workRunner, DelegateRetryRState jobState) where TException : Exception
        {
            _logger?.Debug("[{JobId}] - Performing delegate work - attempt {Attempt}", jobState.JobId, jobState.Attempt);
            try
            {
                _logger?.Debug("[{JobId}] - Executing work with a delay of {DelayInMs}ms", jobState.JobId, jobState.GetRetryDelay());
                await workRunner.ExecuteWithDelay(jobState.GetRetryDelay());
                jobState.HadSuccessfulRun = true;
            }
            catch (Exception e)
            {
                jobState.LastThrownException = ResolveWrappedDynamicInvokeExceptionIfApplicable(e);
                ThrowIfUnexpectedError<TException>(jobState);
            }
            return jobState;
        }

        private Exception ResolveWrappedDynamicInvokeExceptionIfApplicable(Exception e) 
        {
            if (e.GetType() == typeof(TargetInvocationException))
            {
                return e.InnerException;
            }
            return e;
        }

        private void ThrowIfUnexpectedError<TException>(DelegateRetryRState jobState) where TException : Exception
        {
            var thrownException = jobState.LastThrownException;
            if (thrownException.GetType() == typeof(TException) || thrownException.GetType().IsSubclassOf(typeof(TException)))
            {
                _logger?.Debug("[{JobId}] - Thrown exception matches retry on exception type, will attempt a retry if conditional passes", jobState.JobId);
            }
            else
            {
                _logger?.Debug("[{JobId}] - Thrown exception does not match retry on exception type", jobState.JobId);
                throw thrownException;
            }
        }
        private void PropagateException(DelegateRetryRState jobState)
        {
            _logger?.Information("[{JobId}] - Delegate work did not complete successfully in {Attempt} attempt(s). Bubbling up the exception.", jobState.JobId, jobState.Attempt - 1);
            throw jobState.LastThrownException; 
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
            return (attempt) => attempt <= 3;
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
