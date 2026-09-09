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
public class Location : IEquatable<Location>, ISpanParsable<Location>
{
    private readonly List<SequenceRange> ranges = [];
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
        if (!range.IsDefault)
        {
            ranges.Add(range);
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
    public IReadOnlyList<SequenceRange> Ranges
    {
        get
        {
            view ??= ranges.AsReadOnly();
            return view;
        }
    }

    /// <summary>
    /// Gets the starting site index. The location includes this site.
    /// </summary>
    public int Start => IsEmpty ? 0 : First.Start;

    /// <summary>
    /// Gets the ending site index. The range includes this site.
    /// </summary>
    public int End => IsEmpty ? 0 : Last.End;

    /// <summary>
    /// Gets a value that indicates whether this location represents single continuous range.
    /// </summary>
    public bool IsSpan => !IsMultiple && locOperator != LocationOperator.Site;

    private bool IsEmpty => ranges.Count == 0;

    private bool IsMultiple => ranges.Count > 1;

    // Requires !IsEmpty check.
    private SequenceRange First => ranges[0];

    // Requires !IsEmpty check.
    private SequenceRange Last => ranges[^1];

    // Requires IsMultiple check.
    private SequenceRange SecondLast => ranges[^2];

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
    public override int GetHashCode() => HashCode.Combine(Start, End, Length, ranges.Count);

    /// <summary>
    /// Determines whether this location is a subset of a specified range.
    /// </summary>
    /// <param name="range">The continuous range to compare to this location.</param>
    /// <returns>
    /// <see langword="true"/> if this location is empty or a subset of <paramref name="range"/>;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public bool IsSubsetOf(SequenceRange range)
        => IsEmpty || (range.Start <= First.Start && Last.End <= range.End);

    /// <summary>
    /// Determines whether any region in the current location overlaps with the specified range.
    /// </summary>
    /// <param name="range">The range to compare to this location.</param>
    /// <returns>
    /// <see langword="true"/> if this location and <paramref name="range"/> share at least one common site;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public bool Overlaps(SequenceRange range) => BinarySearchForOverlap(0, range) >= 0;

    /// <summary>
    /// Modifies the current location so that it contains all regions that are present in the current location, in the specified
    /// range, or in both.
    /// </summary>
    /// <param name="range">The continuous range to compare to the current location.</param>
    public void UnionWith(SequenceRange range)
    {
        if (!range.IsDefault)
            UnionWithCore(0, range);
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

        int index = 0;

        foreach (var range in other.ranges)
            index = UnionWithCore(index, range);
    }

    /// <summary>
    /// Modifies the current location so that it contains only regions that are also in a specified range.
    /// </summary>
    /// <param name="range">The continuous range to compare to the current location.</param>
    /// <remarks>When <paramref name="range"/> is the default value, this method removes all regions.</remarks>
    public void IntersectWith(SequenceRange range)
    {
        if (!IsSubsetOf(range))
        {
            int index = IntersectWithCore(0, range);
            RemoveRangesFromLast(index);
        }
    }

    /// <summary>
    /// Modifies the current location so that it contains only regions that are also in a specified location.
    /// </summary>
    /// <param name="other">The location to compare to the current location.</param>
    /// <exception cref="ArgumentNullException"><paramref name="other"/> is <see langword="null"/>.</exception>
    public void IntersectWith(Location other)
    {
        ArgumentNullException.ThrowIfNull(other);

        if (ReferenceEquals(this, other) || IsEmpty)
            return;

        if (other.IsEmpty)
            ClearRanges();
        else
        {
            int index = 0;

            foreach (var range in other.ranges)
            {
                index = IntersectWithCore(index, range);
                if (index == ranges.Count)
                    break;
            }
            RemoveRangesFromLast(index);
        }
    }

    /// <summary>
    /// Removes the specified region from the current location.
    /// </summary>
    /// <param name="range">The continuous range to remove from the current location.</param>
    public void ExceptWith(SequenceRange range)
    {
        if (!IsEmpty && !range.IsDefault)
            ExceptWithCore(0, range);
    }

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

        if (IsEmpty || other.IsEmpty)
            return;

        int index = 0;

        foreach (var range in other.ranges)
        {
            index = ExceptWithCore(index, range);

            if (index == ~ranges.Count)
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

        for (int index = IndexForMergeOrInsert(0, range); index < ranges.Count; index++)
        {
            var current = ranges[index];
            Debug.Assert(!AheadOfDistantly(current, range));

            if (AheadOfDistantly(range, current))
            {
                // |← (prev) →|  |← range →|  |← current →|
                ranges.Insert(index, range);
                Length += range.Length;
                return;
            }

            if (range.End + 1 == current.Start)
            {
                // |← range →|← current →|
                ranges[index] = new(range.Start, current.End);
                Length += range.Length;
                return;
            }

            if (current.End + 1 == range.Start)
            {
                // |← current →|← range →|
                int nextIndex = index + 1;
                var next = nextIndex == ranges.Count ? default : ranges[nextIndex];

                if (nextIndex == ranges.Count || AheadOfDistantly(range, next))
                {
                    // |← current →|← range →|  |← next →|
                    ranges[index] = new(current.Start, range.End);
                    Length += range.Length;
                    return;
                }
                else if (range.End + 1 == next.Start)
                {
                    // |← current →|← range →|← next →|
                    ranges[index] = new(current.Start, next.End);
                    Length += range.Length;
                    ranges.RemoveAt(nextIndex);
                    return;
                }
                else
                {
                    // |← current →|← range  →|
                    //                |← next ~
                    ranges[index] = new(current.Start, next.Start - 1);
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
                    ranges[index] = new(range.End + 1, current.End);
                    ranges.Insert(index, before);
                    // before.Length - (range.End - current.Start + 1)
                    Length += 2 * current.Start - range.Start - range.End - 1;
                    return;
                }
                else
                {
                    // (1) |←    range      →|
                    // (2) |←    range   →|
                    //        |← current →|
                    ranges[index] = before;
                    // before.Length - current.Length
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
                    ranges[index] = new(range.End + 1, current.End);
                    Length -= range.Length;
                    return;
                }
                else if (range.End == current.End)
                {
                    // |←  range  →|
                    // |← current →|
                    ranges.RemoveAt(index);
                    Length -= range.Length;
                    return;
                }
                else
                {
                    int nextIndex = index + 1;
                    var next = nextIndex == ranges.Count ? default : ranges[nextIndex];

                    if (nextIndex == ranges.Count || AheadOfDistantly(range, next))
                    {
                        // |←    range   →|
                        // |← current →|     |← next →|
                        ranges[index] = new(current.End + 1, range.End);
                        // range.End - current.End - current.Length
                        Length += range.End + current.Start - 2 * current.End - 1;
                        return;
                    }
                    else if (range.End + 1 == next.Start)
                    {
                        // |←    range   →|
                        // |← current →|  |← next →|
                        ranges[index] = new(current.End + 1, next.End);
                        ranges.RemoveAt(index + 1);
                        // range.Length - 2 * current.Length
                        Length += range.End + current.Start - 2 * current.End - 1;
                        return;
                    }
                    else
                    {
                        // |←    range      →|
                        // |← current →|  |← next ~
                        ranges[index] = new(current.End + 1, next.Start - 1);
                        // next.Start - 1 - current.End - current.Length
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
                ranges[index] = new(current.Start, range.Start - 1);

                if (range.End < current.End)
                    ranges.Insert(index + 1, new(range.End + 1, current.End));

                Length -= range.Length;
                return;
            }
            else
            {
                //      |← range →|
                // |← current →|
                ranges[index] = new(current.Start, range.Start - 1);
                Length -= current.End - range.Start + 1;
                range = new(current.End + 1, range.End);
                continue;
            }
        }

        // |← location →|  |← range →|
        ranges.Add(range);
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

        if (other.IsEmpty)
            return;

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
        result = new Location();
        span = span.Trim();
        int nameColonIndex = span.IndexOf(':');

        if (nameColonIndex > 0)
        {
            result.SequenceName = span[..nameColonIndex].ToString();
            span = span[(nameColonIndex + 1)..];
        }

        if (span.StartsWith("complement(", StringComparison.Ordinal) && span.EndsWith(')'))
        {
            result.IsComplement = true;
            span = span[11..^1];
        }

        if (span.StartsWith("join(", StringComparison.Ordinal) && span.EndsWith(')'))
            span = span[5..^1];

        bool isFirst = true;
        bool maybeNotExactLast = false;

        foreach (var range in span.Split(','))
        {
            var rangeSpan = span[range].Trim();

            if (isFirst)
            {
                if (rangeSpan.StartsWith('<'))
                    result.IsExactStart = false;

                isFirst = false;
            }
            maybeNotExactLast = false;

            if (rangeSpan.Contains('>'))
                maybeNotExactLast = true;

            if (!SequenceRange.TryParse(rangeSpan, out var rangeResult))
            {
                result = null;
                return false;
            }
            result.UnionWith(rangeResult);
        }

        if (maybeNotExactLast)
            result.IsExactEnd = false;

        return true;
    }

    // @param index The index of an item to be target of union
    //              (if equal to size, simply add; the previous item should be the most last item ahead of `range`)
    // @return The index of an item to be target of union at next step
    private int UnionWithCore(int index, SequenceRange range)
    {
        index = IndexForMergeOrInsert(index, range);

        if (index == ranges.Count)
        {
            // AddLast
            Debug.Assert(IsEmpty || AheadOfDistantly(Last, range));

            ranges.Add(range);
            Length += range.Length;
            return ranges.Count;
        }

        int removeFrom = -1;
        do
        {
            var current = ranges[index];
            Debug.Assert(!AheadOfDistantly(current, range));

            if (AheadOfDistantly(range, current))
                break;

            // `range` can be merged with `current`
            range = new(Math.Min(current.Start, range.Start), Math.Max(current.End, range.End));

            if (removeFrom == -1)
                removeFrom = index;

            Length -= current.Length;
        }
        while (++index < ranges.Count);

        if (removeFrom == -1)
        {
            // No merge, simply insert
            ranges.Insert(index, range);
            Length += range.Length;
            return index + 1;
        }

        // Finalize merge
        if (removeFrom + 1 < index)
            ranges.RemoveRange(removeFrom + 1, index - removeFrom - 1);

        ranges[removeFrom] = range;
        Length += range.Length;
        return removeFrom;
    }

    // @param index The index of unprocessed range
    // @return The index of unprocessed range for next step
    private int IntersectWithCore(int index, SequenceRange range)
    {
        do
        {
            var current = ranges[index];

            if (AheadOf(current, range))
            {
                ranges.RemoveAt(index);
                Length -= current.Length;
                continue;
            }

            if (AheadOf(range, current))
                return index;

            // Here, current.Overlaps(range) == true
            var intersect = new SequenceRange(Math.Max(current.Start, range.Start), Math.Min(current.End, range.End));
            ranges[index] = intersect;
            Length += intersect.Length - current.Length;

            if (range.End < current.End)
            {
                // this        current ->|
                // other     range ->| |<- (next)
                var rest = new SequenceRange(range.End + 1, current.End);
                ranges.Insert(index + 1, rest);
                Length += rest.Length;
            }
            index++;
        }
        while (index < ranges.Count);

        return index;
    }

    // @param currentNode this.CurrentNode
    // @param range other.CurrentNode.Value
    // @return this.CurrentNode
    private int ExceptWithCore(int index, SequenceRange range)
    {
        index = BinarySearchForOverlap(index, range);

        while (index >= 0 && index < ranges.Count)
        {
            // `range` overlaps with `current`
            var current = ranges[index];

            if (range.End < current.End)
            {
                ranges[index] = new(range.End + 1, current.End);

                if (current.Start < range.Start)
                {
                    //   |← range →|
                    // |←  current  →|
                    ranges.Insert(index, new(current.Start, range.Start - 1));
                    Length -= range.Length;
                    return index + 1;
                }
                else
                {
                    // |←  range  →|
                    //   |← current →|
                    Length -= range.End - current.Start + 1;
                    return index;
                }
            }

            if (current.Start < range.Start)
            {
                //   |←  range  →|
                // |← current →|
                ranges[index] = new(current.Start, range.Start - 1);
                Length -= current.End - range.Start + 1;
                index++;
            }
            else
            {
                // |←    range    →|
                //   |← current →|
                ranges.RemoveAt(index);
                Length -= current.Length;
            }
            index = BinarySearchForOverlap(index, range);
        }
        return index;
    }

    private void RemoveRangesFromLast(int index)
    {
        if (index < ranges.Count)
        {
            for (int i = index; i < ranges.Count; i++)
                Length -= ranges[i].Length;

            ranges.RemoveRange(index, ranges.Count - index);
        }
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

    // @return index (>= startIndex) s.t. ranges[index - 1].End << range.Start <= ranges[index].End
    private int IndexForMergeOrInsert(int startIndex, SequenceRange range)
    {
        if (IsEmpty || AheadOfDistantly(Last, range))
        {
            // ranges[^1] < range (addlast)
            return ranges.Count;
        }

        if (IsMultiple && AheadOfDistantly(SecondLast, range))
        {
            // ranges[^2] < range <= ranges[^1] (index = ^1)
            Debug.Assert(startIndex <= ranges.Count - 1);
            return ranges.Count - 1;
        }
        Debug.Assert(startIndex == 0 || AheadOfDistantly(ranges[startIndex - 1], range));

        int index = ranges.BinarySearch(startIndex, ranges.Count - startIndex, range, null);
        if (index >= 0)
        {
            // range = ranges[index]
            return index;
        }
        index = ~index;

        if (index == 0 || AheadOfDistantly(ranges[index - 1], range))
        {
            // ranges[index - 1] < range <= ranges[index]
            return index;
        }
        else
        {
            // ranges[return] <= range
            return index - 1;
        }
    }

    // @return index (>= startIndex) s.t. range.Overlaps(ranges[index])
    //         or ~index s.t. ranges[index - 1].End < range.Start && range.End < ranges[index].Start
    private int BinarySearchForOverlap(int startIndex, SequenceRange range)
    {
        if (IsEmpty || AheadOf(Last, range))
        {
            // Not found any more
            return ~ranges.Count;
        }

        if (startIndex < 0)
            startIndex = ~startIndex;

        Debug.Assert(startIndex == 0 || AheadOf(ranges[startIndex - 1], range));

        if (AheadOf(range, ranges[startIndex]))
        {
            // Not found but still possibly overlap for following steps
            return ~startIndex;
        }
        else if (!AheadOf(ranges[startIndex], range))
        {
            // `range` overlaps with ranges[startIndex]
            return startIndex;
        }

        startIndex++;
        int index = ranges.BinarySearch(startIndex, ranges.Count - startIndex, range, null);
        if (index >= 0)
        {
            // Exact match
            return index;
        }
        index = ~index;

        // `index` satisfies ranges[index - 1].Start <= range.Start <= ranges[index].Start
        // thus, !AheadOf(range, ranges[index - 1]) && !AheadOf(ranges[index], range)
        if (!AheadOf(ranges[index - 1], range))
        {
            // `range` overlaps with ranges[index - 1] or Last
            return index - 1;
        }
        if (!AheadOf(range, ranges[index]))
        {
            // `range` overlaps with ranges[index]
            return index;
        }
        else
        {
            // ranges[index - 1] < range < ranges[index] (no overlap in both)
            return ~index;
        }
    }

    // @return index (>= startIndex) s.t. ranges[index - 1].End < point <= ranges[index].End
    private int IndexOfPoint(int startIndex, int point)
    {
        Debug.Assert(!IsEmpty);

        if (point <= ranges[startIndex].End)
            return startIndex;

        if (Last.End < point)
            return ranges.Count;

        int index = ranges.BinarySearch(startIndex, ranges.Count - startIndex, new(point), null);

        if (index >= 0)
            return index;

        // ranges[~index - 1].Start < point <= ranges[~index].Start
        index = ~index;
        Debug.Assert(index > 0);

        if (ranges[index - 1].End < point)
            return index;
        else
            return index - 1;
    }


    // @return index (>= startIndex) s.t. ranges[index - 1].Start <= point < ranges[index].Start
    private int LastIndexOfPoint(int startIndex, int point)
    {
        Debug.Assert(!IsEmpty);

        if (point < ranges[startIndex].Start)
            return startIndex;

        if (Last.Start <= point)
            return ranges.Count;

        int index = ranges.BinarySearch(startIndex, ranges.Count - startIndex, new(point), null);

        if (index >= 0)
            return index + 1;

        // ranges[~index - 1].Start < point <= ranges[~index].Start
        index = ~index;
        Debug.Assert(index < ranges.Count);

        if (ranges[index].Start == point)
            return index + 1;
        else
            return index;
    }

    // !AheadOf(x, y) && !AheadOf(y, x) == x.Overlaps(y)
    private static bool AheadOf(SequenceRange predecessor, SequenceRange successor)
        => predecessor.End < successor.Start;

    // Check if `predecessor` is ahead of `successor` and there is at least the space of 1 base between them.
    private static bool AheadOfDistantly(SequenceRange predecessor, SequenceRange successor)
        => predecessor.End + 1 < successor.Start;

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
