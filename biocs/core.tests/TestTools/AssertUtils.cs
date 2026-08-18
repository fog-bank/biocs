namespace Biocs.TestTools;

internal static class AssertUtils
{
    public static void TestParse<T>(T expected, ReadOnlySpan<char> span, IFormatProvider? provider = null)
        where T : IParsable<T>, IEquatable<T>
    {
        TestParse(expected, span.ToString(), provider);
    }

    public static void TestParse<T>(T expected, string input, IFormatProvider? provider = null)
        where T : IParsable<T>, IEquatable<T>
    {
        Assert.AreEqual(expected, T.Parse(input, provider));
        Assert.IsTrue(T.TryParse(input, provider, out var result));
        Assert.AreEqual(expected, result);
    }

    public static void TestSpanParse<T>(T expected, ReadOnlySpan<char> input, IFormatProvider? provider = null)
        where T : ISpanParsable<T>, IEquatable<T>
    {
        Assert.AreEqual(expected, T.Parse(input, provider));
        Assert.IsTrue(T.TryParse(input, provider, out var result));
        Assert.AreEqual(expected, result);
    }
}
