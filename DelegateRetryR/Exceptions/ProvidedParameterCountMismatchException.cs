using System;
using System.Collections.Generic;
using System.Text;

namespace DelegateRetryR.Exceptions
{
    public class ProvidedParameterCountMismatchException : Exception
    {
        public ProvidedParameterCountMismatchException(int expectedCount, int actualCount)
            : base($"Provided parameter list of length {actualCount} doesn't match expected count of {expectedCount}.")
        {
        }
    }
}
