using System.Diagnostics.CodeAnalysis;

namespace Biocs;

// Helper methods for property body (or inlining method) to throw a exception.
internal static class ThrowHelper
{
    [DoesNotReturn]
    public static void ThrowArgument(string? message, string? paramName = null) => throw new ArgumentException(message, paramName);

    [DoesNotReturn]
    public static void ThrowInvalidOperation(string? message) => throw new InvalidOperationException(message);
}
