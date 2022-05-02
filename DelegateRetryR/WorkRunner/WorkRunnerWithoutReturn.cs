using System;
using System.Threading.Tasks;

namespace DelegateRetry.WorkRunner
{
    public class WorkRunnerWithoutReturn : WorkRunnerBase
    {
        protected override Delegate Work { get; set; }
        protected override object[] Parameters { get; set; }
        protected override bool IsAsyncWork { get; set; }
        public WorkRunnerWithoutReturn(Delegate work, object[]? parameters, bool isAsyncWork)
            : base(work, parameters, isAsyncWork)
        {
        }

        public async override Task ExecuteWithDelay(int waitDurationMs)
        {
            var result = await InvokeWorkWithDelay(waitDurationMs);
            if (IsAsyncWork)
            {
                await TryConvertToResult<Task>(result);
            }
        }
    }
}
