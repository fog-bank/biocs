using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Biocs;

/// <summary>
/// Represents the region of the biological sequence.
/// </summary>
/// <remarks>
/// <para>This is a subset of location descriptors in
/// [The DDBJ/ENA/GenBank Feature Table Definition](https://www.insdc.org/submitting-standards/feature-table/).</para>
/// </remarks>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public class Location : IEquatable<Location>, IComparable<Location>, ISpanParsable<Location>, IComparable
{
    private int start;
    private int end;
    private LocationOperator locOperator;
    private IReadOnlyList<Location>? elements;

    public int Start => IsSpan ? start : Elements[0].Start;

    public int End => IsSpan ? end : Elements[^1].End;

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

    public IReadOnlyList<Location> Elements
    {
        get => elements ?? [];
        private set => elements = value;
    }

    /// <summary>
    /// Gets a value that indicates whether the current <see cref="Location"/> object represents a continuous range.
    /// </summary>
    public bool IsSpan => locOperator == LocationOperator.Span;

    /// <summary>
    /// Gets a value that indicates whether the current <see cref="Location"/> object represents the complementary strand of
    /// the specified sequence.
    /// </summary>
    public bool IsComplement => locOperator == LocationOperator.Complement || locOperator == LocationOperator.ComplementJoin;

    private string DebuggerDisplay => Elements.Count > 3 ? $"{typeof(Location).Name}[{Elements.Count}]" : ToString();

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
        var sb = new StringBuilder();

        if (!string.IsNullOrEmpty(SequenceName))
            sb.Append(SequenceName).Append(':');

        ElementsToString(sb, SequenceName);
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

    internal string ToString2()
    {
        string id = string.IsNullOrEmpty(SequenceName) ? string.Empty : SequenceName + ":";

        return locOperator switch
        {
            LocationOperator.Span => Length switch
            {
                0 => GetType().ToString(),
                1 => id + start,
                // Continuous range
                _ when IsExactStart && IsExactEnd => id + start + ".." + end,
                _ => id + (IsExactStart ? string.Empty : "<") + start + ".." + end + (IsExactEnd ? string.Empty : ">")
            },
            LocationOperator.Site => Length switch
            {
                // Site between bases
                2 => id + start + "^" + end,
                // One of the bases between bases
                _ => id + start + "." + end
            },
            LocationOperator.Join => id + "join(" + string.Join(",", Elements) + ")",
            LocationOperator.Complement => id + "complement(" + string.Join(",", Elements) + ")",
            LocationOperator.Order => id + "order(" + string.Join(",", Elements) + ")",
            LocationOperator.ComplementJoin => id + "complement(join(" + string.Join(",", Elements) + "))",
            _ => GetType().ToString()
        };
    }

    private void ElementsToString(StringBuilder builder, string? parentID)
    {
        if (Length == 0)
            return;

        if (SequenceName != parentID && !string.IsNullOrEmpty(SequenceName))
            builder.Append(SequenceName).Append(':');

        switch (locOperator)
        {
            case LocationOperator.Span:
                if (Length == 1)
                    builder.Append(start);
                else
                {
                    if (!IsExactStart)
                        builder.Append('<');

                    builder.Append(start).Append("..").Append(end);

                    if (!IsExactEnd)
                        builder.Append('>');
                }
                break;

            case LocationOperator.Site:
                builder.Append(start).Append(Length == 2 ? '^' : '.').Append(end);
                break;

            case LocationOperator.Join:
                builder.Append("join(");
                AppendElements(builder, parentID);
                builder.Append(')');
                break;

            case LocationOperator.Complement:
                builder.Append("complement(");
                AppendElements(builder, parentID);
                builder.Append(')');
                break;

            case LocationOperator.Order:
                builder.Append("order(");
                AppendElements(builder, parentID);
                builder.Append(')');
                break;

            case LocationOperator.ComplementJoin:
                builder.Append("complement(join(");
                AppendElements(builder, parentID);
                builder.Append("))");
                break;
        }

        void AppendElements(StringBuilder builder, string? parentID)
        {
            if (Elements.Count == 0)
                return;

            foreach (var element in Elements)
            {
                element.ElementsToString(builder, parentID);
                builder.Append(',');
            }
            builder.Length--;
        }
    }

    #region Comparison Operators

    public static bool operator ==(Location? left, Location? right)
    {
        return left is null ? right is null : left.Equals(right);
    }

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
    Complement,
    Order,
    ComplementJoin
}
