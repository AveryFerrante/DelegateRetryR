using DelegateRetry.Exceptions;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace DelegateRetry
{
    public static class DelegateRetryRExtensions
    {
        //private static DelegateRetryR retryR;
        //static DelegateRetryRExtensions() => retryR = new DelegateRetryR();
        //public static async Task RetryWorkAsync<TException>(this Delegate action, object[]? parameters = null, Predicate<int>? retryConditional = null, Func<int, int>? retryDelay = null)
        //    where TException : Exception
        //{
        //    await retryR.RetryWorkAsync<TException, object?>(action, parameters, retryConditional, retryDelay);
        //}

        //public static async Task<TResult> RetryWorkAsync<TException, TResult>(this Delegate action, object[]? parameters, Predicate<int>? retryConditional = null, Func<int, int>? retryDelay = null)
        //    where TException : Exception
        //{
        //    return await retryR.RetryWorkAsync<TException, TResult>(action, parameters, retryConditional, retryDelay);
        //}

        //public static async Task RetryAsyncWorkAsync<TException>(this Delegate action, object[]? parameters = null, Predicate<int>? retryConditional = null, Func<int, int>? retryDelay = null)
        //    where TException : Exception
        //{
        //    await retryR.RetryAsyncWorkAsync<TException, object?>(action, parameters, retryConditional, retryDelay);
        //}

        //public static async Task<TResult> RetryAsyncWorkAsync<TException, TResult>(this Delegate action, object[]? parameters, Predicate<int>? retryConditional = null, Func<int, int>? retryDelay = null) where TException : Exception
        //{
        //    return await retryR.RetryAsyncWorkAsync<TException, TResult>(action, parameters, retryConditional, retryDelay);
        //}

        public static Delegate CreateDelegate(this MethodInfo methodInfo, object target)
        {
            Func<Type[], Type> getType;
            var isAction = methodInfo.ReturnType.Equals((typeof(void)));
            var types = methodInfo.GetParameters().Select(p => p.ParameterType);

            if (isAction)
            {
                getType = Expression.GetActionType;
            }
            else
            {
                getType = Expression.GetFuncType;
                types = types.Concat(new[] { methodInfo.ReturnType });
            }

            if (methodInfo.IsStatic)
            {
                return Delegate.CreateDelegate(getType(types.ToArray()), methodInfo);
            }

            return Delegate.CreateDelegate(getType(types.ToArray()), target, methodInfo.Name);
        }
    }
}
