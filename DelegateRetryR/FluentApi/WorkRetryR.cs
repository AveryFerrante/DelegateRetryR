using DelegateRetry.Exceptions;
using System;
using System.Threading.Tasks;
using System.Linq;

namespace DelegateRetry.FluentApi
{
    public class WorkRetryR : ITakesParametersStep, IThrowsExceptionStep
    {
        public Delegate Work { get; set; }
        public bool IsAsync { get; set; }
        public object[]? Parameters { get; set; }

        private WorkRetryR(Delegate work, bool isAsync)
        {
            Work = work;
            IsAsync = isAsync;
        }
        public static ITakesParametersStep WillExecuteSyncWork(Delegate work)
        {
            return new WorkRetryR(work, false);
        }

        public static ITakesParametersStep WillExecuteAsyncWork(Delegate work)
        {
            return new WorkRetryR(work, true);
        }
        public IThrowsExceptionStep WithParameters(object param1, params object[] restOfParams)
        {
            restOfParams = restOfParams.Prepend(param1).ToArray();
            Parameters = restOfParams;
            return this;
        }

        public IThrowsExceptionStep WithNoParameters()
        {
            Parameters = null;
            return this;
        }

        public IConfigurationStep AndRetryOn<TException>() where TException : Exception
        {
            return WorkRetryRWithException<TException>.Init<TException>(this);
        }
    }

    public class WorkRetryRWithException<TException> : IConfigurationStep, IDefineReturnTypeStep, IExecuteNoReturnStep where TException : Exception
    {
        protected DelegateRetryRConfiguration? Configuration { get; set; }
        protected WorkRetryR WorkRetryR { get; set; }
        protected WorkRetryRWithException(WorkRetryR workRetryR, DelegateRetryRConfiguration? config = null)
        {
            WorkRetryR = workRetryR;
            Configuration = config;
        }
        public static IConfigurationStep Init<T>(WorkRetryR workRetryR) where T : Exception
        {
            return new WorkRetryRWithException<T>(workRetryR);
        }

        public IDefineReturnTypeStep UsingConfiguration(DelegateRetryRConfiguration config)
        {
            Configuration = config;
            return this;
        }

        public IExecuteWithReturnStep<TReturn> WillReturn<TReturn>()
        {
            return WorkRetryRWithReturn<TReturn, TException>.Init<TReturn, TException>(WorkRetryR, Configuration);
        }

        public IExecuteNoReturnStep WillReturnNothing()
        {
            return this;
        }

        public virtual Task Execute()
        {
            var retryR = new DelegateRetryR();
            if (WorkRetryR.IsAsync)
            {
                return retryR.RetryAsyncWorkAsync<TException>(WorkRetryR.Work, WorkRetryR.Parameters, Configuration?.RetryConditional, Configuration?.RetryDelay);
            }
            else
            {
                return retryR.RetryWorkAsync<TException>(WorkRetryR.Work, WorkRetryR.Parameters, Configuration?.RetryConditional, Configuration?.RetryDelay);
            }
        }
    }

    public class WorkRetryRWithReturn<TReturn, TException> : WorkRetryRWithException<TException>, IExecuteWithReturnStep<TReturn> where TException : Exception
    {
        private WorkRetryRWithReturn(WorkRetryR workRetryR, DelegateRetryRConfiguration? config) : base(workRetryR, config) { }

        public static WorkRetryRWithReturn<R, E> Init<R, E>(WorkRetryR workRetryR, DelegateRetryRConfiguration? config) where E : Exception
        {
            return new WorkRetryRWithReturn<R, E>(workRetryR, config);
        }

        public new Task<TReturn> Execute()
        {
            var retryR = new DelegateRetryR();
            if (WorkRetryR.IsAsync)
            {
                EnsureReturnTypeOfWorkMatches(typeof(Task<TReturn>));
                return retryR.RetryAsyncWorkAsync<TException, TReturn>(WorkRetryR.Work, WorkRetryR.Parameters, Configuration?.RetryConditional, Configuration?.RetryDelay);
            }
            else
            {
                EnsureReturnTypeOfWorkMatches(typeof(TReturn));
                return retryR.RetryWorkAsync<TException, TReturn>(WorkRetryR.Work, WorkRetryR.Parameters, Configuration?.RetryConditional, Configuration?.RetryDelay);
            }
        }

        private void EnsureReturnTypeOfWorkMatches(Type expectedReturnType)
        {
            if (WorkRetryR.Work.Method.ReturnType != expectedReturnType)
            {
                throw new InvalidReturnTypeException(WorkRetryR.Work.Method.ReturnType, expectedReturnType);
            }
        }
    }

    public interface ITakesParametersStep
    {
        IThrowsExceptionStep WithParameters(object param1, params object[] restOfParams);
        IThrowsExceptionStep WithNoParameters();
    }

    public interface IThrowsExceptionStep
    {
        IConfigurationStep AndRetryOn<TException>() where TException : Exception;
    }

    public interface IConfigurationStep
    {
        IDefineReturnTypeStep UsingConfiguration(DelegateRetryRConfiguration config);
    }

    public interface IDefineReturnTypeStep
    {
        IExecuteWithReturnStep<TReturn> WillReturn<TReturn>();
        IExecuteNoReturnStep WillReturnNothing();
    }

    public interface IExecuteNoReturnStep
    {
        Task Execute();
    }

    public interface IExecuteWithReturnStep<TReturn>
    {
        Task<TReturn> Execute();
    }
}
