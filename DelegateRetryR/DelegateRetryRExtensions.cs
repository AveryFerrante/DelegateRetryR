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
