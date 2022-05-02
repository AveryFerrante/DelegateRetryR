using DelegateRetry.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DelegateRetry.WorkRunner
{
    public class WorkRunnerWithReturn<TResult> : WorkRunnerBase
    {
        protected override Delegate Work { get; set; }
        protected override object[] Parameters { get; set; }
        protected override bool IsAsyncWork { get; set; }
        public TResult Result { get; set; }
        public WorkRunnerWithReturn(Delegate work, object[]? parameters, bool isAsyncWork) :
            base(work, parameters, isAsyncWork)
        {
            ValidateProperties();
        }

        private void ValidateProperties()
        {
            if (IsAsyncWork)
            {
                EnsureReturnTypeOfWorkMatches(typeof(Task<TResult>));
            }
            else
            {
                EnsureReturnTypeOfWorkMatches(typeof(TResult));
            }
        }

        private void EnsureReturnTypeOfWorkMatches(Type providedReturnType)
        {
            if (Work.Method.ReturnType != providedReturnType)
            {
                throw new InvalidReturnTypeException(Work.Method.ReturnType, providedReturnType);
            }
        }

        public async override Task ExecuteWithDelay(int waitDurationMs)
        {
            var result = await InvokeWorkWithDelay(waitDurationMs);
            if (IsAsyncWork)
            {
                Result = await TryConvertToResult<Task<TResult>>(result);
            }
            else
            {
                Result = TryConvertToResult<TResult>(result);
            }
        }
    }
}
