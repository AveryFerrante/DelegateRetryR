using System;

namespace DelegateRetry.Exceptions
{
    public class ReturnTypeMismatchException : Exception
    {
        public ReturnTypeMismatchException(Type expectedReturnType, Type actualReturnType, Exception innerException)
            : base($"Could not convert to expected return type of {expectedReturnType.FullName} from actual return type of {actualReturnType.FullName}", innerException) { }

    }
}
