using DelegateRetry.Exceptions;
using DelegateRetryR.Exceptions;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DelegateRetryR.WorkRunner
{
    public abstract class WorkRunnerBase
    {
        protected abstract Delegate Work { get; set; }
        protected abstract object[] Parameters { get; set; }
        protected abstract bool IsAsyncWork { get; set; }
        protected WorkRunnerBase(Delegate work, object[]? parameters, bool isAsyncWork)
        {
            Work = work;
            Parameters = parameters ?? new object[] { };
            IsAsyncWork = isAsyncWork;
            ValidateProperties();
        }

        private void ValidateProperties()
        {
            if (IsAsyncWork)
            {
                EnsureActionReturnsTask();
            }
            EnsureParameterCount();
            EnsureParameterTypes();
        }

        private void EnsureActionReturnsTask()
        {
            var actionReturnType = Work.Method.ReturnType;
            if (!actionReturnType.FullName.Contains(typeof(Task).FullName))
            {
                throw new InvalidReturnTypeException(actionReturnType, typeof(Task));
            }
        }

        private void EnsureParameterCount()
        {
            var workParameters = Work.Method.GetParameters();
            if (workParameters.Length != Parameters.Length)
            {
                throw new ProvidedParameterCountMismatchException(workParameters.Length, Parameters.Length);
            }
        }

        private void EnsureParameterTypes()
        {
            if (Parameters.Length > 0)
            {
                CompareProvidedTypesToActualTypes();
            }
        }

        private void CompareProvidedTypesToActualTypes()
        {
            var workParameters = Work.Method.GetParameters();
            for (int i = 0; i < Parameters.Length; i++)
            {
                var workParameterType = workParameters[i].ParameterType;
                var providedParameterType = Parameters[i].GetType();
                if (AreInvalidTypes(workParameterType, providedParameterType))
                {
                    throw new InvalidProvidedParameterTypeException(workParameterType, providedParameterType, i);
                }
            }
        }

        private bool AreInvalidTypes(Type workParameterType, Type providedParameterType)
        {
            return (workParameterType != providedParameterType && !providedParameterType.IsSubclassOf(workParameterType));
        }

        protected async Task<object> InvokeWorkWithDelay(int waitDurationInMs)
        {
            await Task.Delay(waitDurationInMs);
            return Work.DynamicInvoke(Parameters);
        }

        protected TResult TryConvertToResult<TResult>(object result)
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
        public abstract Task ExecuteWithDelay(int waitDurationMs);
    }
}
