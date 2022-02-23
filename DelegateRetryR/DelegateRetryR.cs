using DelegateRetry.Exceptions;
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
            await RetryWorkAsync<TException, object?>(action, parameters, retryConditional, retryDelay);
        }

        public async Task<TResult> RetryWorkAsync<TException, TResult>(Delegate action, object[]? parameters, Predicate<int>? retryConditional = null, Func<int, int>? retryDelay = null)
            where TException : Exception
        {
            Func<Delegate, object[], int, Task<TResult>> taskRunner = RunWorkAsTask<TResult>;
            return await PerformRetry<TException, TResult>(action, parameters, taskRunner, retryConditional, retryDelay);
        }

        public async Task RetryAsyncWorkAsync<TException>(Delegate action, object[]? parameters = null, Predicate<int>? retryConditional = null, Func<int, int>? retryDelay = null)
            where TException : Exception
        {
            await RetryAsyncWorkAsync<TException, object?>(action, parameters, retryConditional, retryDelay);
        }

        public async Task<TResult> RetryAsyncWorkAsync<TException, TResult>(Delegate action, object[]? parameters, Predicate<int>? retryConditional = null, Func<int, int>? retryDelay = null) where TException : Exception
        {
            EnsureActionReturnsTask(action);
            Func<Delegate, object[], int, Task<TResult>> taskRunner = RunAsyncWorkAsTask<TResult>;
            return await PerformRetry<TException, TResult>(action, parameters, taskRunner, retryConditional, retryDelay);
        }

        private static void EnsureActionReturnsTask(Delegate action)
        {
            var actionReturnType = action.Method.ReturnType;
            if (!actionReturnType.FullName.Contains(typeof(Task).FullName))
            {
                throw new InvalidReturnTypeException(actionReturnType, typeof(Task));
            }
        }

        // TODO: REMOVE DEPENDENCY ON TRESULT SO DONT HAVE TO USE OBJECT PLACEHOLDER VALUE
        private async Task<TResult> PerformRetry<TException, TResult>(Delegate action, object[]? parameters, Func<Delegate, object[], int, Task<TResult>> WorkRunner, Predicate<int>? retryConditional = null, Func<int, int>? retryDelay = null) where TException : Exception
        {
            TException fault;
            Guid jobId = Guid.NewGuid();
            retryConditional = ResolveRetryConditional(retryConditional);
            retryDelay = ResolveRetryDelay(retryDelay);
            if (parameters == null)
            {
                parameters = new object[] { };
            }
            int attempt = 1;
            _logger?.LogInformation("[{JobId}] - Beginning delegate work", jobId);
            do
            {
                _logger?.LogDebug("Performing delegate work - attempt {Attempt}", attempt);
                try
                {
                    TResult result = await (Task<TResult>)WorkRunner.DynamicInvoke(new object[] { action, parameters, retryDelay(attempt) });
                    _logger?.LogInformation("[{JobId}] - Delegate work completed successfully in {Attempt} attempt(s)", jobId, attempt);
                    return result;
                }
                catch (TargetInvocationException e)
                {
                    _logger?.LogDebug("Delegate work threw an exception of type {ExceptionType}", e.InnerException.GetType());
                    if (InnerExceptionMatchesRetryException<TException>(e))
                    {
                        _logger?.LogDebug("Thrown exception matches retry on exception type, will attempt a retry if conditional passes");
                        fault = (TException)e.InnerException;
                    }
                    else
                    {
                        _logger?.LogDebug("Thrown exception does not match retry on exception type");
                        throw e.InnerException;
                    }
                }
            } while (retryConditional(attempt++));
            _logger?.LogInformation("[{JobId}] - Delegate work did not complete successfully in {Attempt} attempts. Bubbling up the exception.", jobId, attempt - 1);
            throw fault;
        }

        private async Task<TResult> RunWorkAsTask<TResult>(Delegate action, object[] parameters, int waitDurationInMs)
        {
            var result = await WaitAndPerformWork(action, parameters, waitDurationInMs);
            return TryConvertToResult<TResult>(result);
        }

        private async Task<TResult> RunAsyncWorkAsTask<TResult>(Delegate action, object[] parameters, int waitDurationInMs)
        {
            var result = await WaitAndPerformWork(action, parameters, waitDurationInMs);
            // TODO: CANNOT RELY ON PASSING OBJECT TYPE FOR NON-RETURN VALUE 
            if (typeof(TResult) != typeof(object))
            {
                return await TryConvertToResult<Task<TResult>>(result);
            }
            else
            {
                await (Task)result;
                return TryConvertToResult<TResult>(result);
            }
        }

        private async Task<object> WaitAndPerformWork(Delegate action, object[] parameters, int waitDurationInMs)
        {
            _logger?.LogDebug("Waiting {WaitDuration}ms before attempting delegate work", waitDurationInMs);
            await Task.Delay(waitDurationInMs);
            return action.DynamicInvoke(parameters);
        }

        private static TResult TryConvertToResult<TResult>(object result)
        {
            try
            {
                return (TResult)result;
            }
            catch (InvalidCastException e)
            {
                throw new ReturnTypeMismatchException(typeof(TResult), result.GetType(), e);
            }
        }

        private bool InnerExceptionMatchesRetryException<TException>(TargetInvocationException e) where TException : Exception
        {
            var actualExceptionType = e.InnerException.GetType();
            return (actualExceptionType.IsSubclassOf(typeof(TException)) || actualExceptionType == typeof(TException));
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
