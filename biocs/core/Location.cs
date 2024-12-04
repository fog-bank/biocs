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
public class Location : IEquatable<Location>, ISpanParsable<Location>
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
        if (!range.IsDefault)
        {
            ranges.AddFirst(range);
            Length = range.Length;
        }
    }

    /// <summary>
    /// Gets the total length of regions that this location represents.
    /// </summary>
    public int Length { get; private set; }

    /// <summary>
    /// Gets or sets a value that indicates whether this location represents the complementary strand of the specified
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
    /// Gets a value that indicates whether this location represents single continuous range.
    /// </summary>
    public bool IsSpan => ranges.Count <= 1 && locOperator != LocationOperator.Site;

    [MemberNotNullWhen(false, nameof(FirstNode))]
    [MemberNotNullWhen(false, nameof(LastNode))]
    private bool IsEmpty => ranges.Count == 0;

    private LinkedListNode<SequenceRange>? FirstNode => ranges.First;

    private LinkedListNode<SequenceRange>? LastNode => ranges.Last;

    [DebuggerBrowsable(DebuggerBrowsableState.Never), ExcludeFromCodeCoverage]
    private string DebuggerDisplay
        => IsEmpty || ranges.Count > 3 ? $"{nameof(Length)} = {Length}, {nameof(Ranges)}.Count = {ranges.Count}" : ToString();

    /// <inheritdoc/>
    public bool Equals([NotNullWhen(true)] Location? other)
    {
        if (ReferenceEquals(this, other))
            return true;

        if (other is null)
            return false;

        if (Length != other.Length || End != other.End)
            return false;

        return ranges.SequenceEqual(other.ranges);
    }

    /// <inheritdoc/>
    public override bool Equals([NotNullWhen(true)] object? obj) => Equals(obj as Location);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Start, End, ranges.Count, IsComplement);

    /// <summary>
    /// Determines whether this location is a subset of a specified range.
    /// </summary>
    /// <param name="range">The continuous range to compare to this location.</param>
    /// <returns>
    /// <see langword="true"/> if this location is empty or a subset of <paramref name="range"/>;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public bool IsSubsetOf(SequenceRange range)
        => IsEmpty || (range.Start <= FirstNode.Value.Start && LastNode.Value.End <= range.End);

    /// <summary>
    /// Determines whether any region in the current location overlaps with the specified range.
    /// </summary>
    /// <param name="range">The range to compare to this location.</param>
    /// <returns>
    /// <see langword="true"/> if this location and <paramref name="range"/> share at least one common site;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    [MemberNotNullWhen(true, nameof(FirstNode))]
    [MemberNotNullWhen(true, nameof(LastNode))]
    public bool Overlaps(SequenceRange range)
    {
        if (IsEmpty || range.End < FirstNode.Value.Start || LastNode.Value.End < range.Start)
            return false;

        foreach (var region in ranges)
        {
            if (region.Overlaps(range))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Modifies the current location so that it contains all regions that are present in the current location, in the specified
    /// range, or in both.
    /// </summary>
    /// <param name="range">The continuous range to compare to the current location.</param>
    public void UnionWith(SequenceRange range)
    {
        if (!range.IsDefault)
            UnionWithCore(FirstOrSkipNodesForUnion(range), range);
    }

    /// <summary>
    /// Modifies the current location so that it contains all regions that are present in the current location, in the specified
    /// location, or in both.
    /// </summary>
    /// <param name="other">The location to compare to the current location.</param>
    /// <exception cref="ArgumentNullException"><paramref name="other"/> is <see langword="null"/>.</exception>
    public void UnionWith(Location other)
    {
        ArgumentNullException.ThrowIfNull(other);

        if (ReferenceEquals(this, other) || other.IsEmpty)
            return;

        var currentNode = FirstOrSkipNodesForUnion(other.FirstNode.Value);

        foreach (var range in other.ranges)
            currentNode = UnionWithCore(currentNode, range);
    }

    /// <summary>
    /// Modifies the current location so that it contains only regions that are also in a specified range.
    /// </summary>
    /// <param name="range">The continuous range to compare to the current location.</param>
    /// <remarks>When <paramref name="range"/> is the default value, this method removes all regions.</remarks>
    public void IntersectWith(SequenceRange range)
    {
        if (!IsSubsetOf(range))
            IntersectWithCore(range, null);
    }

    /// <summary>
    /// Modifies the current location so that it contains only regions that are also in a specified location.
    /// </summary>
    /// <param name="other">The location to compare to the current location.</param>
    /// <exception cref="ArgumentNullException"><paramref name="other"/> is <see langword="null"/>.</exception>
    public void IntersectWith(Location other)
    {
        ArgumentNullException.ThrowIfNull(other);

        if (ReferenceEquals(this, other))
            return;

        if (other.IsEmpty)
            ClearRanges();
        else
            IntersectWithCore(other.FirstNode.Value, other.FirstNode.Next);
    }

    /// <summary>
    /// Removes the specified region from the current location.
    /// </summary>
    /// <param name="range">The continuous range to remove from the current location.</param>
    public void ExceptWith(SequenceRange range) => ExceptWithCore(FirstOrSkipNodesForExcept(range), range);

    /// <summary>
    /// Removes all regions in the specified location from the current location.
    /// </summary>
    /// <param name="other">The location to compare to the current location.</param>
    /// <exception cref="ArgumentNullException"><paramref name="other"/> is <see langword="null"/>.</exception>
    public void ExceptWith(Location other)
    {
        ArgumentNullException.ThrowIfNull(other);

        if (ReferenceEquals(this, other))
        {
            ClearRanges();
            return;
        }

        if (other.IsEmpty)
            return;

        var currentNode = FirstOrSkipNodesForExcept(other.FirstNode.Value);

        foreach (var range in other.ranges)
        {
            currentNode = ExceptWithCore(currentNode, range);

            if (currentNode == null)
                break;
        }
    }

    /// <summary>
    /// Modifies the current location so that it contains only regions that are present either in the current location or in the
    /// specified range, but not both.
    /// </summary>
    /// <param name="range">The continuous range to compare to the current location.</param>
    public void SymmetricExceptWith(SequenceRange range)
    {
        if (range.IsDefault)
            return;

        for (var currentNode = FirstOrSkipNodesForUnion(range); currentNode != null; currentNode = currentNode.Next)
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
            ClearRanges();
            return;
        }

        foreach (var range in other.ranges)
        {
            // TODO:
            SymmetricExceptWith(range);
        }
    }

    /// <summary>
    /// Removes all regions from this location and resets the information for the region.
    /// </summary>
    public void Clear()
    {
        ClearRanges();

        locOperator = LocationOperator.SpanOrJoin;
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

    // @param currentNode this.CurrentNode
    // @param range other.CurrentNode.Value
    // @return this.CurrentNode
    private LinkedListNode<SequenceRange>? UnionWithCore(LinkedListNode<SequenceRange>? currentNode, SequenceRange range)
    {
        while (currentNode != null)
        {
            var current = currentNode.Value;

            if (AheadOfDistantly(range, current))
            {
                // |← (prev) →|  |← range →|  |← current →|
                ranges.AddBefore(currentNode, range);
                Length += range.Length;
                return currentNode;
            }
            var nextNode = currentNode.Next;

            if (AheadOfDistantly(current, range))
            {
                // |← current →|  |← range →|
                currentNode = nextNode;
                continue;
            }

            // range can be merged with current
            range = new(Math.Min(current.Start, range.Start), Math.Max(current.End, range.End));

            if (nextNode == null || AheadOfDistantly(range, nextNode.Value))
            {
                // |← merge →|  |← (next) →|
                currentNode.Value = range;
                Length += range.Length - current.Length;
                return currentNode;
            }

            // Need to merge new range and next
            ranges.Remove(currentNode);
            Length -= current.Length;
            currentNode = nextNode;
        }
        ranges.AddLast(range);
        Length += range.Length;
        return null;
    }

    // @param range other.CurrentNode.Value
    // @param otherNextNode other.CurrentNode.Next
    private void IntersectWithCore(SequenceRange range, LinkedListNode<SequenceRange>? otherNextNode)
    {
        var currentNode = FirstNode;

        while (currentNode != null)
        {
            var current = currentNode.Value;

            if (range.IsDefault || current.End < range.Start)
            {
                var nextNode = currentNode.Next;
                ranges.Remove(currentNode);
                Length -= current.Length;
                currentNode = nextNode;
                continue;
            }

            if (range.End < current.Start)
            {
                if (otherNextNode != null)
                {
                    range = otherNextNode.Value;
                    otherNextNode = otherNextNode.Next;
                }
                else
                    range = default;

                continue;
            }

            // Here, current.Overlaps(range) == true
            var intersect = new SequenceRange(Math.Max(current.Start, range.Start), Math.Min(current.End, range.End));
            currentNode.Value = intersect;
            Length += intersect.Length - current.Length;

            if (range.End < current.End && otherNextNode != null && otherNextNode.Value.Start <= current.End)
            {
                // this        current ->|
                // other   current ->| |<- next
                currentNode = ranges.AddAfter(currentNode, new SequenceRange(range.End + 1, current.End));
                Length += currentNode.Value.Length;
            }
            else
                currentNode = currentNode.Next;
        }
    }

    // @param currentNode this.CurrentNode
    // @param range other.CurrentNode.Value
    // @return this.CurrentNode
    private LinkedListNode<SequenceRange>? ExceptWithCore(LinkedListNode<SequenceRange>? currentNode, SequenceRange range)
    {
        while (currentNode != null)
        {
            var current = currentNode.Value;

            if (range.End < current.Start)
            {
                // |← range →| |← current →|
                return currentNode;
            }
            var nextNode = currentNode.Next;

            if (current.End < range.Start)
            {
                // |← current →| |← range →|
                currentNode = nextNode;
                continue;
            }

            // Here, current.Overlaps(range) == true
            if (range.End < current.End)
            {
                var after = new SequenceRange(range.End + 1, current.End);

                if (current.Start < range.Start)
                {
                    //   |← range →|
                    // |←  current  →|
                    ranges.AddBefore(currentNode, new SequenceRange(current.Start, range.Start - 1));
                    Length -= range.Length;
                }
                else
                {
                    // |←  range  →|
                    //   |← current →|
                    Length -= range.End - current.Start + 1;
                }
                currentNode.Value = after;
                return currentNode;
            }

            if (current.Start < range.Start)
            {
                //   |←  range  →|
                // |← current →|
                currentNode.Value = new(current.Start, range.Start - 1);
                Length -= current.End - range.Start + 1;
            }
            else
            {
                // |←    range    →|
                //   |← current →|
                ranges.Remove(currentNode);
                Length -= current.Length;
            }
            currentNode = nextNode;
        }
        return null;
    }

    private void ClearRanges()
    {
        ranges.Clear();
        Length = 0;
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

    private LinkedListNode<SequenceRange>? FirstOrSkipNodesForUnion(SequenceRange range)
    {
        if (IsEmpty || AheadOfDistantly(LastNode.Value, range))
            return null;

        return ranges.Count > 1 && AheadOfDistantly(LastNode.Previous!.Value, range) ? LastNode : FirstNode;
    }

    private LinkedListNode<SequenceRange>? FirstOrSkipNodesForExcept(SequenceRange range)
    {
        if (IsEmpty || range.End < FirstNode.Value.Start || LastNode.Value.End < range.Start)
            return null;

        return ranges.Count > 1 && LastNode.Previous!.Value.End < range.Start ? LastNode : FirstNode;
    }

    private static bool AheadOfDistantly(SequenceRange preceding, SequenceRange succeeding)
        => preceding.End + 1 < succeeding.Start;

    #region Explicit Interface Implementations

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
