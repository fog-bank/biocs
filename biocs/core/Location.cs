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
    private readonly LinkedList<SequenceRange> ranges = new();
    private IReadOnlyList<SequenceRange>? view;
    private LocationOperator locOperator = LocationOperator.SpanOrJoin;

    /// <summary>
    /// Initializes a new instance of the <see cref="Location"/> class.
    /// </summary>
    public Location()
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Location"/> class that represents the specified continuous range.
    /// </summary>
    /// <param name="range">The <see cref="SequenceRange"/> object that represents a continuous range.</param>
    public Location(SequenceRange range)
    {
        ranges.AddFirst(range);
    }

    /// <summary>
    /// Gets the total length of the region that the current location represents.
    /// </summary>
    public int Length { get; private set; }

    /// <summary>
    /// Gets or sets a value that indicates whether the current location represents the complementary strand of the specified
    /// sequence.
    /// </summary>
    public bool IsComplement { get; set; }

    /// <summary>
    /// Gets a value that indicates whether the exact starting base number is known.
    /// </summary>
    public bool IsExactStart { get; private set; } = true;

    /// <summary>
    /// Gets a value that indicates whether the exact ending base number is known.
    /// </summary>
    public bool IsExactEnd { get; private set; } = true;

    /// <summary>
    /// Gets or sets the name of the sequence to which this location belongs.
    /// </summary>
    public string? SequenceName { get; set; }

    /// <summary>
    /// Gets the read-only collection that contains each continuous range.
    /// </summary>
    public IReadOnlyCollection<SequenceRange> Ranges
    {
        get
        {
            //view ??= ranges.AsReadOnly();
            return view;
        }
    }

    /// <summary>
    /// Gets the starting site index. The location includes this site.
    /// </summary>
    public int Start => IsEmpty ? 0 : FirstNode.Value.Start;

    /// <summary>
    /// Gets the ending site index. The range includes this site.
    /// </summary>
    public int End => IsEmpty ? 0 : LastNode.Value.End;

    /// <summary>
    /// Gets a value that indicates whether the current location represents single continuous range.
    /// </summary>
    public bool IsSpan => Ranges.Count <= 1 && locOperator != LocationOperator.Site;

    [MemberNotNullWhen(false, nameof(FirstNode))]
    [MemberNotNullWhen(false, nameof(LastNode))]
    private bool IsEmpty => ranges.Count == 0;

    private LinkedListNode<SequenceRange>? FirstNode => ranges.First;

    private LinkedListNode<SequenceRange>? LastNode => ranges.Last;

    private string DebuggerDisplay =>
        ranges.Count > 3 ? $"{nameof(Ranges)}.{nameof(Ranges.Count)} = {ranges.Count}" : ToString();

    /// <inheritdoc/>
    public bool Equals([NotNullWhen(true)] Location? other)
    {
        if (ReferenceEquals(this, other))
            return true;

        if (other is null || End != other.End || SequenceName != other.SequenceName || IsComplement != other.IsComplement
            || IsExactStart != other.IsExactStart || IsExactEnd != other.IsExactEnd || locOperator != other.locOperator)
            return false;

        return ranges.SequenceEqual(other.ranges);
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
    public override int GetHashCode() => HashCode.Combine(Start, End, ranges.Count, IsComplement);

    //public void UnionWith(SequenceRange range)
    //{
    //    throw new NotImplementedException();
    //}

    //public void UnionWith(Location other)
    //{
    //    throw new NotImplementedException();
    //}

    //public void IntersectWith(SequenceRange range)
    //{
    //    throw new NotImplementedException();
    //}

    //public void IntersectWith(Location other)
    //{
    //    throw new NotImplementedException();
    //}

    //public void ExceptWith(SequenceRange range)
    //{
    //    throw new NotImplementedException();
    //}

    //public void ExceptWith(Location other)
    //{
    //    throw new NotImplementedException();
    //}

    /// <summary>
    /// Modifies the current location so that it contains only regions that are present either in the current location or in the
    /// specified range, but not both.
    /// </summary>
    /// <param name="range">The continuoug range to compare to the current location.</param>
    public void SymmetricExceptWith(SequenceRange range)
    {
        if (IsEmpty || range.End < Start)
        {
            ranges.AddFirst(range);
            Length += range.Length;
            return;
        }

        if (End < range.Start)
        {
            ranges.AddLast(range);
            Length += range.Length;
            return;
        }

        for (var node = FirstNode; node != null; )
        {
            var current = node.Value;

            if (range.End + 1 == current.Start)
            {
                node.Value = new(range.Start, current.End);
                Length += range.Length;
                break;
            }
            else if (current.End + 1 == range.Start)
            {
                if (node.Next != null && node.Next.Value.Start <= range.End + 1)
                {
                    range = new(current.Start, range.End);

                    var next = node.Next;
                    ranges.Remove(node);
                    Length -= current.Length;
                    node = next;
                    continue;
                }
                else
                {
                    node.Value = new(current.Start, range.End);
                    Length += range.Length;
                    break;
                }
            }
            else if (range.Overlaps(current))
            {
                if (range.Start < current.Start)
                {
                    var before = new SequenceRange(range.Start, current.Start - 1);

                    if (current.End <= range.End)
                    {
                        range = new(current.End + 1, range.End);

                        node.Value = before;
                        Length += before.Length - current.Length;

                        if (current.End < range.End)
                            range = new(current.End + 1, range.End);
                        else
                            break;
                    }
                    else
                    {

                    }
                    ranges.AddBefore(node, before);
                    Length += before.Length;
                }
                else
                {

                }
            }
            node = node.Next;
        }
    }

    /// <summary>
    /// Modifies the current location so that it contains only regions that are present either in the current location or in the
    /// specified location, but not both.
    /// </summary>
    /// <param name="other">The location to compare to the current location.</param>
    /// <exception cref="ArgumentNullException"><paramref name="other"/> is <see langword="null"/>.</exception>
    public void SymmetricExceptWith(Location other)
    {
        ArgumentNullException.ThrowIfNull(other);

        if (ReferenceEquals(this, other))
        {
            ranges.Clear();
            Length = 0;
            return;
        }

        foreach (var range in other.ranges)
        {
            // TODO:
        }
    }

    /// <summary>
    /// Removes all regions from this location and resets the information for the region.
    /// </summary>
    public void Clear()
    {
        ranges.Clear();
        locOperator = LocationOperator.SpanOrJoin;
        Length = 0;
        IsComplement = false;
        IsExactStart = true;
        IsExactEnd = true;
        SequenceName = null;
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        if (IsEmpty)
            return string.Empty;

        var sb = new StringBuilder();

        if (!string.IsNullOrEmpty(SequenceName))
            sb.Append(SequenceName).Append(':');

        ElementsToString(sb);
        return sb.ToString();
    }

    /// <summary>
    /// Parses the string representation of a range to the equivalent <see cref="Location"/> object.
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
    /// Tries to parse the string representation of a range to the equivalent <see cref="Location"/> object.
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
        if (IsComplement)
            builder.Append("complement(");

        switch (locOperator)
        {
            case LocationOperator.SpanOrJoin:
                if (ranges.Count > 1)
                {
                    builder.Append("join(");
                    AppendRanges(builder);
                    builder.Append(')');
                }
                else if (Length == 1)
                {
                    builder.Append(Start);
                }
                else
                {
                    if (!IsExactStart)
                        builder.Append('<');

                    builder.Append(Start).Append("..");

                    if (!IsExactEnd)
                        builder.Append('>');

                    builder.Append(End);
                }
                break;

            case LocationOperator.Site:
                builder.Append(Start).Append(Length == 2 ? '^' : '.').Append(End);
                break;

            case LocationOperator.Order:
                builder.Append("order(");
                AppendRanges(builder);
                builder.Append(')');
                break;
        }

        if (IsComplement)
            builder.Append(')');

        void AppendRanges(StringBuilder builder)
        {
            if (IsExactStart && IsExactEnd)
            {
                builder.AppendJoin(',', ranges);
            }
            else
            {
                // TODO:
                foreach (var range in ranges)
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
    /// Determines whether two specified <see cref="Location"/> objects are not equal.
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
    SpanOrJoin,
    Site,
    Order
}
