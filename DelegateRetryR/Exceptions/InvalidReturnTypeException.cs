using System;

namespace DelegateRetry.Exceptions
{
    public class InvalidReturnTypeException : Exception
    {
        public InvalidReturnTypeException(Type expected, Type actual)
            : base($"Return type must be of type {expected.FullName}. Instead receieved return type of {actual.FullName}")
        { }
    }
}
