using System;

namespace DelegateRetry.Exceptions
{
    public class InvalidReturnTypeException : Exception
    {
        public InvalidReturnTypeException(Type actual, Type expected)
            : base($"Return type must be of type {expected.FullName}. Instead receieved return type of {actual.FullName}")
        { }
    }
}
