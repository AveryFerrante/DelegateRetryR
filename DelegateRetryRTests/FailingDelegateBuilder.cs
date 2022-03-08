using System;

namespace DelegateRetry.Tests
{
    public class FailingDelegateBuilder : ISetFailAttemptsStep, ISetBehaviorStep, IBuildStep
    {
        private int callCount;
        private int failCount;
        private Exception exception;
        private Delegate? work;

        private FailingDelegateBuilder(Exception e)
        {
            callCount = 0;
            exception = e;
        }

        public static ISetFailAttemptsStep WillThrow(Exception exception)
        {
            return new FailingDelegateBuilder(exception);
        }

        public ISetBehaviorStep WithFailureCount(int failCount)
        {
            this.failCount = failCount;
            return this;
        }

        public IBuildStep PerformsWork(Delegate? work)
        {
            this.work = work;
            return this;
        }

        public Delegate Build()
        {
            return () =>
            {
                ProcessForcedFailures();
                return work?.DynamicInvoke();
            };
        }

        public Delegate BuildWithExpectedParams<TParam>()
        {
            return (TParam a) =>
            {
                ProcessForcedFailures();
                return work?.DynamicInvoke(a);
            };

        }

        public Delegate BuildWithExpectedParams<TParam1, TParam2>()
        {
            return (TParam1 a, TParam2 b) =>
            {
                ProcessForcedFailures();
                return work?.DynamicInvoke(a, b);
            };

        }

        public Delegate BuildWithReturnType<TReturnType>()
        {
            EnsureReturnType<TReturnType>();
            Func<TReturnType> returnWork = () =>
            {
                ProcessForcedFailures();
                return (TReturnType)work.DynamicInvoke();
            };
            return returnWork;
        }

        public Delegate BuildWithExpectedParamsAndReturnType<TParamType, TReturnType>()
        {
            EnsureReturnType<TReturnType>();
            Func<TParamType, TReturnType> returnWork = (TParamType a) =>
            {
                ProcessForcedFailures();
                return (TReturnType)work.DynamicInvoke(a);
            };
            return returnWork;
        }

        public Delegate BuildWithExpectedParamsAndReturnType<TParamType1, TParamType2, TReturnType>()
        {
            EnsureReturnType<TReturnType>();
            Func<TParamType1, TParamType2, TReturnType> returnWork = (TParamType1 a, TParamType2 b) =>
            {
                ProcessForcedFailures();
                return (TReturnType)work.DynamicInvoke(a, b);
            };
            return returnWork;
        }

        private void EnsureReturnType<TReturnType>()
        {
            if (work.Method.ReturnType != typeof(TReturnType))
            {
                throw new ArgumentException($"Provided return type of {typeof(TReturnType)} doesn't match the actual return type {work.Method.ReturnType.Name}");
            }
        }

        private void ProcessForcedFailures()
        {
            if (callCount < failCount)
            {
                callCount++;
                throw exception;
            }
        }

    }

    public interface ISetFailAttemptsStep
    {
        ISetBehaviorStep WithFailureCount(int failCount);
    }

    public interface ISetBehaviorStep
    {
        IBuildStep PerformsWork(Delegate? work);
    }

    public interface IBuildStep
    {
        Delegate Build();
        Delegate BuildWithExpectedParams<TParamType>();
        Delegate BuildWithExpectedParams<TParamType1, TParamType2>();
        Delegate BuildWithReturnType<TReturnType>();
        Delegate BuildWithExpectedParamsAndReturnType<TParamType1, TReturnType>();
        Delegate BuildWithExpectedParamsAndReturnType<TParamType1, TParamType2, TReturnType>();
    }
}
