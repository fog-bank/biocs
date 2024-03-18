﻿using System.Diagnostics.CodeAnalysis;

namespace Biocs;

// Helper methods for property body (or inlining method) to throw a exception.
internal static class ThrowHelper
{
    [DoesNotReturn]
    public static void ThrowInvalidOperation(string? message) => throw new InvalidOperationException(message);
}
