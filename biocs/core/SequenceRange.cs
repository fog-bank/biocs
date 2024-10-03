using System.Diagnostics.CodeAnalysis;

namespace Biocs;

/// <summary>
/// Represents a continuous range of the presented biological sequence.
/// </summary>
/// <remarks>
/// <para><see cref="Start"/> and <see cref="End"/> must be positive. <see cref="End"/> must be greater than or equal to
/// <see cref="Start"/>.</para>
/// <para>.NET provides <see cref="Range"/> to represent a range of arrays. In addition, C# language supports the range operator
/// (e.g. ..). This syntax is similar to the biological descriptor. However, indices of <see cref="Range"/> are zero-based and
/// <see cref="Range.End"/> is exclusive, whereas biological descriptors are one-based and the ending index is inclusive. Due to
/// these difference, this library provides the dedicated structure to represent a range of biological sequences.</para>
/// </remarks>
public readonly struct SequenceRange :
    IEquatable<SequenceRange>, IComparable<SequenceRange>, ISpanParsable<SequenceRange>, IComparable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SequenceRange"/> structure.
    /// </summary>
    /// <param name="start">The starting site index (one-based, inclusive).</param>
    /// <param name="end">The ending site index (one-based, inclusive).</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <para><paramref name="start"/> or <paramref name="end"/> is equal to or less than zero.</para> -or-
    /// <para><paramref name="start"/> is greater than <paramref name="end"/>.</para>
    /// </exception>
    public SequenceRange(int start, int end)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(start);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(end);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(start, end);

        Start = start;
        End = end;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SequenceRange"/> structure to the specified single site index.
    /// </summary>
    /// <param name="siteIndex">A single site index (one-based).</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="siteIndex"/> is equal to or less than zero.</exception>
    public SequenceRange(int siteIndex) : this(siteIndex, siteIndex)
    { }

    /// <summary>
    /// Gets the starting site index. The range includes this site.
    /// </summary>
    public int Start { get; }

    /// <summary>
    /// Gets the ending site index. The range includes this site.
    /// </summary>
    public int End { get; }

    /// <summary>
    /// Gets the length of this range.
    /// </summary>
    public int Length => End - Start + 1;

    /// <summary>
    /// Gets a value indicating whether this instance is the default value.
    /// </summary>
    public bool IsDefault => Start == 0;

    /// <inheritdoc/>
    public override bool Equals([NotNullWhen(true)] object? obj) => obj is SequenceRange other && Equals(other);

    /// <inheritdoc/>
    public bool Equals(SequenceRange other) => Start == other.Start && End == other.End;

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Start, End);

    /// <inheritdoc/>
    public int CompareTo(SequenceRange other) => (Start - other.Start) switch
    {
        0 => End - other.End,
        int startComp => startComp
    };

    /// <summary>
    /// Determines if this range contains a specific site.
    /// </summary>
    /// <param name="siteIndex">A site index (one-based).</param>
    /// <returns><see langword="true"/> if contained; otherwise <see langword="false"/>.</returns>
    public bool Contains(int siteIndex) => Start <= siteIndex && siteIndex <= End;

    /// <summary>
    /// Determines whether this range overlaps with the specified range.
    /// </summary>
    /// <param name="other">The range to compare to this range.</param>
    /// <returns>
    /// <see langword="true"/> if this range and <paramref name="other"/> share at least one common site; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    public bool Overlaps(SequenceRange other) => Start <= other.End && other.Start <= End;

    /// <summary>
    /// Converts the current <see cref="SequenceRange"/> instance to its equivalent string representation.
    /// </summary>
    /// <returns>The string representation of the current instance.</returns>
    public override string ToString() => Start == End ? $"{Start}" : $"{Start}..{End}";

    /// <summary>
    /// Parses the string representation of a range to the equivalent <see cref="SequenceRange"/> instance.
    /// </summary>
    /// <param name="span">The read-only span of characters to parse.</param>
    /// <returns>The result of parsing <paramref name="span"/>.</returns>
    /// <exception cref="FormatException"><paramref name="span"/> is not in the correct format.</exception>
    [StringResourceUsage("Format.UnparsableValue", 2)]
    public static SequenceRange Parse(ReadOnlySpan<char> span)
    {
        if (!TryParse(span, out var result))
            ThrowHelper.ThrowFormat(Res.GetString("Format.UnparsableValue", nameof(span), span.ToString()));

        return result;
    }

    /// <summary>
    /// Tries to parse the string representation of a range to the equivalent <see cref="SequenceRange"/> instance.
    /// </summary>
    /// <param name="span">The read-only span of characters to parse.</param>
    /// <param name="result">
    /// When this method returns, contains the result of successfully parsing <paramref name="span"/>,
    /// or a default value on failure.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="span"/> was successfully parsed; otherwise, <see langword="false"/>.
    /// </returns>
    public static bool TryParse(ReadOnlySpan<char> span, out SequenceRange result)
    {
        var ranges = (stackalloc Range[3]);
        int nrange = span.SplitAny(ranges, ".^", StringSplitOptions.RemoveEmptyEntries);
        ranges = ranges[..nrange];

        switch (ranges.Length)
        {
            case 1:
                if (TryParseSiteIndex(span[ranges[0]], out int point))
                {
                    result = new(point);
                    return true;
                }
                break;

            case 2:
                if (TryParseSiteIndex(span[ranges[0]], out int start) &&
                    TryParseSiteIndex(span[ranges[1]], out int end) && start <= end)
                {
                    result = new(start, end);
                    return true;
                }
                break;
        }
        result = default;
        return false;
    }

    private static bool TryParseSiteIndex(ReadOnlySpan<char> span, out int siteIndex)
    {
        span = span.TrimStart("<>");
        return int.TryParse(span, out siteIndex) && siteIndex > 0;
    }

    #region Comparison Operators

    /// <summary>
    /// Determines whether two specified instances of <see cref="SequenceRange"/> equal.
    /// </summary>
    /// <param name="left">The first object to compare.</param>
    /// <param name="right">The second object to compare.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="left"/> and <paramref name="right"/> represent the identical range;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public static bool operator ==(SequenceRange left, SequenceRange right) => left.Equals(right);

    /// <summary>
    /// Determines whether two specified instances of <see cref="SequenceRange"/> are not equal.
    /// </summary>
    /// <param name="left">The first object to compare.</param>
    /// <param name="right">The second object to compare.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="left"/> and <paramref name="right"/> do not represent the identical range;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public static bool operator !=(SequenceRange left, SequenceRange right) => !left.Equals(right);

    /// <summary>
    /// Determines whether one specified <see cref="SequenceRange"/> precedes another specified <see cref="SequenceRange"/>.
    /// </summary>
    /// <param name="left">The first object to compare.</param>
    /// <param name="right">The second object to compare.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="left"/> precedes <paramref name="right"/>; otherwise, <see langword="false"/>.
    /// </returns>
    public static bool operator <(SequenceRange left, SequenceRange right) => left.CompareTo(right) < 0;

    /// <summary>
    /// Determines whether one specified <see cref="SequenceRange"/> is the same as or precedes another specified
    /// <see cref="SequenceRange"/>.
    /// </summary>
    /// <param name="left">The first object to compare.</param>
    /// <param name="right">The second object to compare.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="left"/> is the same as or precedes <paramref name="right"/>;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public static bool operator <=(SequenceRange left, SequenceRange right) => left.CompareTo(right) <= 0;

    /// <summary>
    /// Determines whether one specified <see cref="SequenceRange"/> follows another specified <see cref="SequenceRange"/>.
    /// </summary>
    /// <param name="left">The first object to compare.</param>
    /// <param name="right">The second object to compare.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="left"/> follows <paramref name="right"/>; otherwise, <see langword="false"/>.
    /// </returns>
    public static bool operator >(SequenceRange left, SequenceRange right) => left.CompareTo(right) > 0;

    /// <summary>
    /// Determines whether one specified <see cref="SequenceRange"/> is the same as or follows another specified
    /// <see cref="SequenceRange"/>.
    /// </summary>
    /// <param name="left">The first object to compare.</param>
    /// <param name="right">The second object to compare.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="left"/> is the same as or follows <paramref name="right"/>;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public static bool operator >=(SequenceRange left, SequenceRange right) => left.CompareTo(right) >= 0;

    #endregion

    #region Explicit Interface Implementations

    [StringResourceUsage("Arg.CompareToNotSameTypedObject")]
    int IComparable.CompareTo(object? obj) => obj switch
    {
        null => 1,
        SequenceRange other => CompareTo(other),
        _ => throw new ArgumentException(Res.GetString("Arg.CompareToNotSameTypedObject"), nameof(obj))
    };

    static SequenceRange IParsable<SequenceRange>.Parse(string s, IFormatProvider? provider) => Parse(s);

    static SequenceRange ISpanParsable<SequenceRange>.Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => Parse(s);

    static bool IParsable<SequenceRange>.TryParse(string? s, IFormatProvider? provider, out SequenceRange result)
        => TryParse(s, out result);

    static bool ISpanParsable<SequenceRange>.TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out SequenceRange result)
        => TryParse(s, out result);

    #endregion
}
