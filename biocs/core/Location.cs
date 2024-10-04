using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Biocs.Collections;

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
    private IReadOnlyCollection<SequenceRange>? view;
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
            view ??= CollectionTools.AsReadOnly(ranges);
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
    public bool IsSpan => ranges.Count <= 1 && locOperator != LocationOperator.Site;

    [MemberNotNullWhen(false, nameof(FirstNode))]
    [MemberNotNullWhen(false, nameof(LastNode))]
    private bool IsEmpty => ranges.Count == 0;

    private LinkedListNode<SequenceRange>? FirstNode => ranges.First;

    private LinkedListNode<SequenceRange>? LastNode => ranges.Last;

    private string DebuggerDisplay =>
        ranges.Count > 3 ? $"{nameof(Ranges)}.{nameof(Ranges.Count)} = {ranges.Count}, {nameof(Length)} = {Length}" : ToString();

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

    /// <summary>
    /// Modifies the current location so that it contains all regions that are present in the current location, in the specified
    /// range, or in both.
    /// </summary>
    /// <param name="range">The continuoug range to compare to the current location.</param>
    public void UnionWith(SequenceRange range)
    {
        if (range.IsDefault)
            return;

        if (IsEmpty)
        {
            ranges.AddFirst(range);
            Length += range.Length;
            return;
        }

        if (AheadOfDistantly(LastNode.Value, range))
        {
            // |← location →|  |← range →|
            ranges.AddLast(range);
            Length += range.Length;
            return;
        }

        var currentNode = ranges.Count > 1 && AheadOfDistantly(LastNode.Previous!.Value, range) ? LastNode : FirstNode;
        while (true)
        {
            var current = currentNode.Value;

            if (AheadOfDistantly(range, current))
            {
                // |← (prev) →|  |← range →|  |← current →|
                ranges.AddBefore(currentNode, range);
                Length += range.Length;
                return;
            }

            if (AheadOfDistantly(current, range))
            {
                // |← current →|  |← range →|
                // When currentNode.Next is null (i.e. currentNode is LastNode), the condition is already covered.
                currentNode = currentNode.Next!;
                continue;
            }

            // Enable to merge current and range
            range = new(Math.Min(current.Start, range.Start), Math.Max(current.End, range.End));
            var nextNode = currentNode.Next;

            if (nextNode == null || AheadOfDistantly(range, nextNode.Value))
            {
                // |← merge →|  |← (next) →|
                currentNode.Value = range;
                Length += range.Length - current.Length;
                return;
            }
            // Need to merge new range and next
            ranges.Remove(currentNode);
            Length -= current.Length;
            currentNode = nextNode;
        }
    }

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
        if (range == default)
            return;

        if (IsEmpty)
        {
            ranges.AddFirst(range);
            Length += range.Length;
            return;
        }

        var currentNode = ranges.Count > 1 && AheadOfDistantly(LastNode.Previous!.Value, range) ? LastNode : FirstNode;

        for (; currentNode != null; currentNode = currentNode.Next)
        {
            var current = currentNode.Value;

            if (AheadOfDistantly(range, current))
            {
                // |← (prev) →|  |← range →|  |← current →|
                ranges.AddBefore(currentNode, range);
                Length += range.Length;
                return;
            }

            if (AheadOfDistantly(current, range))
                continue;

            if (range.End + 1 == current.Start)
            {
                // |← range →|← current →|
                currentNode.Value = new(range.Start, current.End);
                Length += range.Length;
                return;
            }

            if (current.End + 1 == range.Start)
            {
                // |← current →|← range →|
                var nextNode = currentNode.Next;
                var next = nextNode == null ? default : nextNode.Value;

                if (nextNode == null || AheadOfDistantly(range, next))
                {
                    // |← current →|← range →|  |← next →|
                    currentNode.Value = new(current.Start, range.End);
                    Length += range.Length;
                    return;
                }
                else if (range.End + 1 == next.Start)
                {
                    // |← current →|← range →|← next →|
                    currentNode.Value = new(current.Start, next.End);
                    Length += range.Length;
                    ranges.Remove(nextNode);
                    return;
                }
                else
                {
                    // |← current →|← range  →|
                    //                |← next ~
                    currentNode.Value = new(current.Start, next.Start - 1);
                    Length += next.Start - range.Start;
                    range = new(next.Start, range.End);
                    continue;
                }
            }

            if (range.Start < current.Start)
            {
                var before = new SequenceRange(range.Start, current.Start - 1);

                if (range.End < current.End)
                {
                    // |←  range →|
                    //    |← current →|
                    ranges.AddBefore(currentNode, before);
                    currentNode.Value = new(range.End + 1, current.End);
                    //Length += before.Length - (range.End - current.Start + 1);
                    Length += 2 * current.Start - range.Start - range.End - 1;
                    return;
                }
                else
                {
                    // (1) |←    range      →|
                    // (2) |←    range   →|
                    //        |← current →|
                    currentNode.Value = before;
                    //Length += before.Length - current.Length;
                    Length += 2 * current.Start - range.Start - current.End - 1;

                    if (current.End < range.End)
                    {
                        range = new(current.End + 1, range.End);
                        continue;
                    }
                    return;
                }
            }
            else if (range.Start == current.Start)
            {
                if (range.End < current.End)
                {
                    // |← range →|
                    // |← current  →|
                    currentNode.Value = new(range.End + 1, current.End);
                    Length -= range.Length;
                    return;
                }
                else if (range.End == current.End)
                {
                    // |←  range  →|
                    // |← current →|
                    ranges.Remove(currentNode);
                    Length -= range.Length;
                    return;
                }
                else
                {
                    var nextNode = currentNode.Next;
                    var next = nextNode == null ? default : nextNode.Value;

                    if (nextNode == null || AheadOfDistantly(range, next))
                    {
                        // |←    range   →|
                        // |← current →|     |← next →|
                        currentNode.Value = new(current.End + 1, range.End);
                        //Length += range.End - current.End - current.Length;
                        Length += range.End + current.Start - 2 * current.End - 1;
                        return;
                    }
                    else if (range.End + 1 == next.Start)
                    {
                        // |←    range   →|
                        // |← current →|  |← next →|
                        currentNode.Value = new(current.End + 1, next.End);
                        ranges.Remove(nextNode);
                        //Length += range.Length - 2 * current.Length;
                        Length += range.End + current.Start - 2 * current.End - 1;
                        return;
                    }
                    else
                    {
                        // |←    range      →|
                        // |← current →|  |← next ~
                        currentNode.Value = new(current.End + 1, next.Start - 1);
                        //Length += next.Start - 1 - current.End - current.Length;
                        Length += next.Start + current.Start - 2 * current.End - 2;
                        range = new(next.Start, range.End);
                        continue;
                    }
                }
            }
            else if (range.End <= current.End)
            {
                // (1)    |←  range →|
                // (2)    |←  range    →|
                //     |←    current   →|
                currentNode.Value = new(current.Start, range.Start - 1);

                if (range.End < current.End)
                    ranges.AddAfter(currentNode, new SequenceRange(range.End + 1, current.End));

                Length -= range.Length;
                return;
            }
            else
            {
                //      |← range →|
                // |← current →|
                currentNode.Value = new(current.Start, range.Start - 1);
                Length -= current.End - range.Start + 1;
                range = new(current.End + 1, range.End);
                continue;
            }
        }

        // |← location →|  |← range →|
        ranges.AddLast(range);
        Length += range.Length;
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

    private static bool AheadOfDistantly(SequenceRange preceding, SequenceRange succeeding)
        => preceding.End + 1 < succeeding.Start;

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
