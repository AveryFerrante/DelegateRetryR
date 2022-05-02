using System;

namespace DelegateRetry.Exceptions
{
    public class InvalidProvidedParameterTypeException : Exception
    {
        public InvalidProvidedParameterTypeException(Type expected, Type actual, int parameterIndex)
            : base($"Provided parameter at index {parameterIndex} of type {actual} does match the expected type {expected}")
        {
        }
    }
}
