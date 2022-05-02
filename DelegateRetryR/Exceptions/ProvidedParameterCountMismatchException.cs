using System;

namespace DelegateRetry.Exceptions
{
    public class ProvidedParameterCountMismatchException : Exception
    {
        public ProvidedParameterCountMismatchException(int expectedCount, int actualCount)
            : base($"Provided parameter list of length {actualCount} doesn't match expected count of {expectedCount}.")
        {
        }
    }
}
