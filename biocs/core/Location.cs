using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Biocs;

/// <summary>
/// Represents the region of the biological sequence.
/// </summary>
/// <remarks>
/// <para>This is a subset of location descriptors and operators in
/// [The DDBJ/ENA/GenBank Feature Table Definition](https://www.insdc.org/submitting-standards/feature-table/).</para>
/// </remarks>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public class Location : IEquatable<Location>, IComparable<Location>, ISpanParsable<Location>, IComparable
{
    private LocationOperator locOperator = LocationOperator.Span;
    private IReadOnlyList<SequenceRange>? elements;

    public int Start => Ranges.Count == 0 ? 0 : Ranges[0].Start;

    public int End => Ranges.Count == 0 ? 0 : Ranges[^1].End;

    public int Length { get; private set; }

    /// <summary>
    /// Gets a value that indicates whether the exact starting base number is known.
    /// </summary>
    public bool IsExactStart { get; private set; } = true;

    /// <summary>
    /// Gets a value that indicates whether the exact ending base number is known.
    /// </summary>
    public bool IsExactEnd { get; private set; } = true;

    /// <summary>
    /// Gets the name of the sequence to which this <see cref="Location"/> object belongs.
    /// </summary>
    public string? SequenceName { get; set; }

    public IReadOnlyList<SequenceRange> Ranges
    {
        get => elements ?? [];
        private set => elements = value;
    }

    /// <summary>
    /// Gets a value that indicates whether the current <see cref="Location"/> object represents a continuous range.
    /// </summary>
    public bool IsSpan => Ranges.Count <= 1 && locOperator != LocationOperator.Site;

    /// <summary>
    /// Gets a value that indicates whether the current <see cref="Location"/> object represents the complementary strand of
    /// the specified sequence.
    /// </summary>
    public bool IsComplement
    {
        get => locOperator is LocationOperator.Complement or LocationOperator.ComplementJoin or LocationOperator.ComplementOrder;
        set
        {
            if (IsComplement != value)
            {
                if (value)
                {
                    locOperator = locOperator == LocationOperator.Join ?
                        LocationOperator.ComplementJoin : LocationOperator.Complement;
                }
                else
                {
                    locOperator = locOperator == LocationOperator.ComplementJoin ?
                        LocationOperator.Join : LocationOperator.Span;
                }
            }
        }
    }

    private string DebuggerDisplay =>
        Ranges.Count > 3 ? $"{nameof(Ranges)}.{nameof(Ranges.Count)} = {Ranges.Count}" : ToString();

    /// <inheritdoc/>
    public bool Equals([NotNullWhen(true)] Location? other)
    {
        if (ReferenceEquals(this, other))
            return true;

        if (other is null)
            return false;

        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public override bool Equals([NotNullWhen(true)] object? obj) => Equals(obj as Location);

    /// <inheritdoc/>
    public int CompareTo(Location? other)
    {
        if (ReferenceEquals(this, other))
            return 0;

        if (other is null)
            return 1;

        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Start, End, IsComplement);

    /// <inheritdoc/>
    public override string ToString()
    {
        if (Length == 0)
            return string.Empty;

        var sb = new StringBuilder();

        if (!string.IsNullOrEmpty(SequenceName))
            sb.Append(SequenceName).Append(':');

        ElementsToString(sb);
        return sb.ToString();
    }

    /// <summary>
    /// Parses the string representation of a range to the equivalent <see cref="Location"/> instance.
    /// </summary>
    /// <param name="span">The read-only span of characters to parse.</param>
    /// <returns>The result of parsing <paramref name="span"/>.</returns>
    /// <exception cref="FormatException"><paramref name="span"/> is not in the correct format.</exception>
    [StringResourceUsage("Format.UnparsableValue", 2)]
    public static Location Parse(ReadOnlySpan<char> span)
    {
        if (!TryParse(span, out var result))
            ThrowHelper.ThrowFormat(Res.GetString("Format.UnparsableValue", nameof(span), span.ToString()));

        return result;
    }

    /// <summary>
    /// Tries to parse the string representation of a range to the equivalent <see cref="Location"/> instance.
    /// </summary>
    /// <param name="span">The read-only span of characters to parse.</param>
    /// <param name="result">
    /// When this method returns, contains the result of successfully parsing <paramref name="span"/>, or <see langword="null"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="span"/> was successfully parsed; otherwise, <see langword="false"/>.
    /// </returns>
    public static bool TryParse(ReadOnlySpan<char> span, [MaybeNullWhen(false)] out Location result)
    {
        throw new NotImplementedException();
    }

    private void ElementsToString(StringBuilder builder)
    {
        switch (locOperator)
        {
            case LocationOperator.Span:
                if (Length == 1)
                    builder.Append(Start);
                else
                {
                    if (!IsExactStart)
                        builder.Append('<');

                    builder.Append(Start).Append("..").Append(End);

                    if (!IsExactEnd)
                        builder.Append('>');
                }
                break;

            case LocationOperator.Site:
                builder.Append(Start).Append(Length == 2 ? '^' : '.').Append(End);
                break;

            case LocationOperator.Join:
                builder.Append("join(");
                AppendRanges(builder);
                builder.Append(')');
                break;

            case LocationOperator.Order:
                builder.Append("order(");
                AppendRanges(builder);
                builder.Append(')');
                break;

            case LocationOperator.Complement:
                builder.Append("complement(");
                AppendRanges(builder);
                builder.Append(')');
                break;

            case LocationOperator.ComplementJoin:
                builder.Append("complement(join(");
                AppendRanges(builder);
                builder.Append("))");
                break;

            case LocationOperator.ComplementOrder:
                builder.Append("complement(order(");
                AppendRanges(builder);
                builder.Append("))");
                break;
        }

        void AppendRanges(StringBuilder builder)
        {
            if (IsExactStart && IsExactEnd)
            {
                builder.AppendJoin(',', Ranges);
            }
            else
            {
                foreach (var range in Ranges)
                    builder.Append(range.ToString()).Append(',');

                builder.Length--;
            }
        }
    }

    #region Comparison Operators

    /// <summary>
    /// Determines whether two specified instances of <see cref="Location"/> equal.
    /// </summary>
    /// <param name="left">The first object to compare.</param>
    /// <param name="right">The second object to compare.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="left"/> and <paramref name="right"/> represent the identical region;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public static bool operator ==(Location? left, Location? right)
    {
        return left is null ? right is null : left.Equals(right);
    }

    /// <summary>
    /// Determines whether two specified instances of <see cref="Location"/> are not equal.
    /// </summary>
    /// <param name="left">The first object to compare.</param>
    /// <param name="right">The second object to compare.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="left"/> and <paramref name="right"/> do not represent the identical region;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public static bool operator !=(Location? left, Location? right) => !(left == right);

    /// <summary>
    /// Determines whether the first specified <see cref="Location"/> object is less than the second specified
    /// <see cref="Location"/> object.
    /// </summary>
    /// <param name="left">The first <see cref="Location"/> object.</param>
    /// <param name="right">The second <see cref="Location"/> object.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="left"/> is less than <paramref name="right"/>;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public static bool operator <(Location? left, Location? right)
    {
        return left is null ? right is not null : left.CompareTo(right) < 0;
    }

    /// <summary>
    /// Determines whether the first specified <see cref="Location"/> object is less than or equal to the second specified
    /// <see cref="Location"/> object.
    /// </summary>
    /// <param name="left">The first <see cref="Location"/> object.</param>
    /// <param name="right">The second <see cref="Location"/> object.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="left"/> is less than or equal to <paramref name="right"/>;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public static bool operator <=(Location? left, Location? right)
    {
        return left is null || left.CompareTo(right) <= 0;
    }

    /// <summary>
    /// Determines whether the first specified <see cref="Location"/> object is greater than the second specified
    /// <see cref="Location"/> object.
    /// </summary>
    /// <param name="left">The first <see cref="Location"/> object.</param>
    /// <param name="right">The second <see cref="Location"/> object.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="left"/> is greater than <paramref name="right"/>;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public static bool operator >(Location? left, Location? right) => right < left;

    /// <summary>
    /// Determines whether the first specified <see cref="Location"/> object is greater than or equal to the second specified
    /// <see cref="Location"/> object.
    /// </summary>
    /// <param name="left">The first <see cref="Location"/> object.</param>
    /// <param name="right">The second <see cref="Location"/> object.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="left"/> is greater than or equal to <paramref name="right"/>;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public static bool operator >=(Location? left, Location? right) => right <= left;

    #endregion

    #region Explicit Interface Implementations

    [StringResourceUsage("Arg.CompareToNotSameTypedObject")]
    int IComparable.CompareTo(object? obj) => obj switch
    {
        null => 1,
        Location other => CompareTo(other),
        _ => throw new ArgumentException(Res.GetString("Arg.CompareToNotSameTypedObject"), nameof(obj))
    };

    static Location IParsable<Location>.Parse(string s, IFormatProvider? provider) => Parse(s);

    static Location ISpanParsable<Location>.Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => Parse(s);

    static bool IParsable<Location>.TryParse(string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out Location result)
        => TryParse(s, out result);

    static bool ISpanParsable<Location>.TryParse(
        ReadOnlySpan<char> s, IFormatProvider? provider, [MaybeNullWhen(false)] out Location result) => TryParse(s, out result);

    #endregion
}

internal enum LocationOperator
{
    Span,
    Site,
    Join,
    Order,
    Complement,
    ComplementJoin,
    ComplementOrder
}
