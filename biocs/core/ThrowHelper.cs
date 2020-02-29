using System;
using System.Diagnostics.CodeAnalysis;

namespace Biocs
{
    internal static class ThrowHelper
    {
        [DoesNotReturn]
        public static void ThrowArgumentOutOfRange(string paramName)
        {
            throw new ArgumentOutOfRangeException(paramName);
        }

        [DoesNotReturn]
        public static void ThrowInvalidOperation(string message)
        {
            throw new InvalidOperationException(message);
        }
    }
}
