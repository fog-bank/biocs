using System.Diagnostics.CodeAnalysis;
using Biocs;
using Biocs.Collections;

namespace Benchmark;

public class LinkedListLocation
{
    private readonly LinkedList<SequenceRange> ranges = new();

    public LinkedListLocation()
    { }

    /// <summary>
    /// Gets the total length of regions that this location represents.
    /// </summary>
    public int Length { get; private set; }

    /// <summary>
    /// Gets the starting site index. The location includes this site.
    /// </summary>
    public int Start => IsEmpty ? 0 : FirstNode.Value.Start;

    /// <summary>
    /// Gets the ending site index. The range includes this site.
    /// </summary>
    public int End => IsEmpty ? 0 : LastNode.Value.End;

    [MemberNotNullWhen(false, nameof(FirstNode))]
    [MemberNotNullWhen(false, nameof(LastNode))]
    private bool IsEmpty => ranges.Count == 0;

    private LinkedListNode<SequenceRange>? FirstNode => ranges.First;

    private LinkedListNode<SequenceRange>? LastNode => ranges.Last;

    public bool IsSubsetOf(SequenceRange range) => IsEmpty || (range.Start <= Start && End <= range.End);

    [MemberNotNullWhen(true, nameof(FirstNode))]
    [MemberNotNullWhen(true, nameof(LastNode))]
    public bool Overlaps(SequenceRange range) => !IsEmpty && Start <= range.End && range.Start <= End;

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

    public void IntersectWith(SequenceRange range)
    {
        if (IsSubsetOf(range))
            return;

        if (!Overlaps(range))
        {
            ranges.Clear();
            Length = 0;
            return;
        }

        for (var currentNode = FirstNode; currentNode != null;)
        {
            var current = currentNode.Value;
            var nextNode = currentNode.Next;

            if (current.Overlaps(range))
            {
                var intersect = new SequenceRange(Math.Max(current.Start, range.Start), Math.Min(current.End, range.End));
                currentNode.Value = intersect;
                Length += intersect.Length - current.Length;
            }
            else
            {
                ranges.Remove(currentNode);
                Length -= current.Length;
            }
            currentNode = nextNode;
        }
    }

    public void ExceptWith(SequenceRange range)
    {
        if (!Overlaps(range))
            return;

        var currentNode = ranges.Count > 1 && LastNode.Previous!.Value.End < range.Start ? LastNode : FirstNode;
        do
        {
            var current = currentNode.Value;

            // |← range →| |← current →|
            if (range.End < current.Start)
                return;

            // |← current →| |← range →|
            if (current.End < range.Start)
                continue;

            // Here, current.Overlaps(range) == true
            if (range.End < current.End)
            {
                if (current.Start < range.Start)
                {
                    //   |← range →|
                    // |←  current  →|
                    currentNode.Value = new(current.Start, range.Start - 1);
                    ranges.AddAfter(currentNode, new SequenceRange(range.End + 1, current.End));
                    Length -= range.Length;
                }
                else
                {
                    // |←  range  →|
                    //   |← current →|
                    currentNode.Value = new(range.End + 1, current.End);
                    Length -= range.End - current.Start + 1;
                }
                return;
            }

            var nextNode = currentNode.Next;

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
        while (currentNode != null);
    }

    public void SymmetricExceptWith(SequenceRange range)
    {
        if (range.IsDefault)
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

    private static bool AheadOfDistantly(SequenceRange preceding, SequenceRange succeeding)
        => preceding.End + 1 < succeeding.Start;
}

public class DequeLocation
{
    private readonly Deque<SequenceRange> ranges = new();

    public DequeLocation()
    { }

    public int Length { get; private set; }

    public int Start => IsEmpty ? 0 : ranges.First.Start;

    public int End => IsEmpty ? 0 : ranges.Last.End;

    private bool IsEmpty => ranges.Count == 0;

    public bool IsSubsetOf(SequenceRange range) => IsEmpty || (range.Start <= Start && End <= range.End);

    public bool Overlaps(SequenceRange range) => !IsEmpty && Start <= range.End && range.Start <= End;

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

        if (AheadOfDistantly(ranges.Last, range))
        {
            // |← location →|  |← range →|
            ranges.AddLast(range);
            Length += range.Length;
            return;
        }

        int currentIndex = ranges.Count > 1 && AheadOfDistantly(ranges[^2], range) ? 0 : ranges.Count - 1;
        while (true)
        {
            var current = ranges[currentIndex];

            if (AheadOfDistantly(range, current))
            {
                // |← (prev) →|  |← range →|  |← current →|
                ranges.Insert(currentIndex, range);
                Length += range.Length;
                return;
            }

            if (AheadOfDistantly(current, range))
            {
                // |← current →|  |← range →|
                // When currentNode.Next is null (i.e. currentNode is LastNode), the condition is already covered.
                currentIndex++;
                continue;
            }

            // Enable to merge current and range
            range = new(Math.Min(current.Start, range.Start), Math.Max(current.End, range.End));
            int nextIndex = currentIndex + 1;

            if (nextIndex == ranges.Count || AheadOfDistantly(range, ranges[nextIndex]))
            {
                // |← merge →|  |← (next) →|
                ranges[currentIndex] = range;
                Length += range.Length - current.Length;
                return;
            }
            // Need to merge new range and next
            ranges.RemoveAt(currentIndex);
            Length -= current.Length;
        }
    }

    public void IntersectWith(SequenceRange range)
    {
        if (IsSubsetOf(range))
            return;

        if (!Overlaps(range))
        {
            ranges.Clear();
            Length = 0;
            return;
        }

        for (int currentIndex = 0; currentIndex < ranges.Count;)
        {
            var current = ranges[currentIndex];

            if (current.Overlaps(range))
            {
                var intersect = new SequenceRange(Math.Max(current.Start, range.Start), Math.Min(current.End, range.End));
                ranges[currentIndex] = intersect;
                Length += intersect.Length - current.Length;
                currentIndex++;
            }
            else
            {
                ranges.RemoveAt(currentIndex);
                Length -= current.Length;
            }
        }
    }

    public void ExceptWith(SequenceRange range)
    {
        if (!Overlaps(range))
            return;

        int currentIndex = ranges.Count > 1 && ranges[^2].End < range.Start ? ranges.Count - 1 : 0;
        do
        {
            var current = ranges[currentIndex];

            // |← range →| |← current →|
            if (range.End < current.Start)
                return;

            // |← current →| |← range →|
            if (current.End < range.Start)
                continue;

            // Here, current.Overlaps(range) == true
            if (range.End < current.End)
            {
                if (current.Start < range.Start)
                {
                    //   |← range →|
                    // |←  current  →|
                    ranges[currentIndex] = new(current.Start, range.Start - 1);
                    ranges.Insert(currentIndex + 1, new SequenceRange(range.End + 1, current.End));
                    Length -= range.Length;
                }
                else
                {
                    // |←  range  →|
                    //   |← current →|
                    ranges[currentIndex] = new(range.End + 1, current.End);
                    Length -= range.End - current.Start + 1;
                }
                return;
            }

            if (current.Start < range.Start)
            {
                //   |←  range  →|
                // |← current →|
                ranges[currentIndex] = new(current.Start, range.Start - 1);
                Length -= current.End - range.Start + 1;
                currentIndex++;
            }
            else
            {
                // |←    range    →|
                //   |← current →|
                ranges.RemoveAt(currentIndex);
                Length -= current.Length;
            }
        }
        while (currentIndex < ranges.Count);
    }

    public void SymmetricExceptWith(SequenceRange range)
    {
        if (range.IsDefault)
            return;

        if (IsEmpty)
        {
            ranges.AddFirst(range);
            Length += range.Length;
            return;
        }

        int currentIndex = ranges.Count > 1 && AheadOfDistantly(ranges[^2], range) ? ranges.Count - 1 : 0;

        for (; currentIndex < ranges.Count; currentIndex++)
        {
            var current = ranges[currentIndex];

            if (AheadOfDistantly(range, current))
            {
                // |← (prev) →|  |← range →|  |← current →|
                ranges.Insert(currentIndex, range);
                Length += range.Length;
                return;
            }

            if (AheadOfDistantly(current, range))
                continue;

            if (range.End + 1 == current.Start)
            {
                // |← range →|← current →|
                ranges[currentIndex] = new(range.Start, current.End);
                Length += range.Length;
                return;
            }

            if (current.End + 1 == range.Start)
            {
                // |← current →|← range →|
                int nextIndex = currentIndex + 1;
                var next = nextIndex == ranges.Count ? default : ranges[nextIndex];

                if (nextIndex == ranges.Count || AheadOfDistantly(range, next))
                {
                    // |← current →|← range →|  |← next →|
                    ranges[currentIndex] = new(current.Start, range.End);
                    Length += range.Length;
                    return;
                }
                else if (range.End + 1 == next.Start)
                {
                    // |← current →|← range →|← next →|
                    ranges[currentIndex] = new(current.Start, next.End);
                    Length += range.Length;
                    ranges.RemoveAt(nextIndex);
                    return;
                }
                else
                {
                    // |← current →|← range  →|
                    //                |← next ~
                    ranges[currentIndex] = new(current.Start, next.Start - 1);
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
                    ranges[currentIndex] = new(range.End + 1, current.End);
                    ranges.Insert(currentIndex, before);
                    //Length += before.Length - (range.End - current.Start + 1);
                    Length += 2 * current.Start - range.Start - range.End - 1;
                    return;
                }
                else
                {
                    // (1) |←    range      →|
                    // (2) |←    range   →|
                    //        |← current →|
                    ranges[currentIndex] = before;
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
                    ranges[currentIndex] = new(range.End + 1, current.End);
                    Length -= range.Length;
                    return;
                }
                else if (range.End == current.End)
                {
                    // |←  range  →|
                    // |← current →|
                    ranges.RemoveAt(currentIndex);
                    Length -= range.Length;
                    return;
                }
                else
                {
                    int nextIndex = currentIndex + 1;
                    var next = nextIndex == ranges.Count ? default : ranges[nextIndex];

                    if (nextIndex == ranges.Count || AheadOfDistantly(range, next))
                    {
                        // |←    range   →|
                        // |← current →|     |← next →|
                        ranges[currentIndex] = new(current.End + 1, range.End);
                        //Length += range.End - current.End - current.Length;
                        Length += range.End + current.Start - 2 * current.End - 1;
                        return;
                    }
                    else if (range.End + 1 == next.Start)
                    {
                        // |←    range   →|
                        // |← current →|  |← next →|
                        ranges[currentIndex] = new(current.End + 1, next.End);
                        ranges.RemoveAt(nextIndex);
                        //Length += range.Length - 2 * current.Length;
                        Length += range.End + current.Start - 2 * current.End - 1;
                        return;
                    }
                    else
                    {
                        // |←    range      →|
                        // |← current →|  |← next ~
                        ranges[currentIndex] = new(current.End + 1, next.Start - 1);
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
                ranges[currentIndex] = new(current.Start, range.Start - 1);

                if (range.End < current.End)
                    ranges.Insert(currentIndex + 1, new SequenceRange(range.End + 1, current.End));

                Length -= range.Length;
                return;
            }
            else
            {
                //      |← range →|
                // |← current →|
                ranges[currentIndex] = new(current.Start, range.Start - 1);
                Length -= current.End - range.Start + 1;
                range = new(current.End + 1, range.End);
                continue;
            }
        }

        // |← location →|  |← range →|
        ranges.AddLast(range);
        Length += range.Length;
    }

    private static bool AheadOfDistantly(SequenceRange preceding, SequenceRange succeeding)
        => preceding.End + 1 < succeeding.Start;
}

public class ListLocation
{
    private readonly List<SequenceRange> ranges = [];

    public ListLocation()
    { }

    public int Length { get; private set; }

    public int Start => IsEmpty ? 0 : ranges[0].Start;

    public int End => IsEmpty ? 0 : ranges[^1].End;

    private bool IsEmpty => ranges.Count == 0;

    public bool IsSubsetOf(SequenceRange range) => IsEmpty || (range.Start <= Start && End <= range.End);

    public bool Overlaps(SequenceRange range) => !IsEmpty && Start <= range.End && range.Start <= End;

    public void UnionWith(SequenceRange range)
    {
        if (range.IsDefault)
            return;

        if (IsEmpty)
        {
            ranges.Insert(0, range);
            Length += range.Length;
            return;
        }

        if (AheadOfDistantly(ranges[^1], range))
        {
            // |← location →|  |← range →|
            ranges.Add(range);
            Length += range.Length;
            return;
        }

        int currentIndex = ranges.Count > 1 && AheadOfDistantly(ranges[^2], range) ? 0 : ranges.Count - 1;
        while (true)
        {
            var current = ranges[currentIndex];

            if (AheadOfDistantly(range, current))
            {
                // |← (prev) →|  |← range →|  |← current →|
                ranges.Insert(currentIndex, range);
                Length += range.Length;
                return;
            }

            if (AheadOfDistantly(current, range))
            {
                // |← current →|  |← range →|
                // When currentNode.Next is null (i.e. currentNode is LastNode), the condition is already covered.
                currentIndex++;
                continue;
            }

            // Enable to merge current and range
            range = new(Math.Min(current.Start, range.Start), Math.Max(current.End, range.End));
            int nextIndex = currentIndex + 1;

            if (nextIndex == ranges.Count || AheadOfDistantly(range, ranges[nextIndex]))
            {
                // |← merge →|  |← (next) →|
                ranges[currentIndex] = range;
                Length += range.Length - current.Length;
                return;
            }
            // Need to merge new range and next
            ranges.RemoveAt(currentIndex);
            Length -= current.Length;
        }
    }

    public void IntersectWith(SequenceRange range)
    {
        if (IsSubsetOf(range))
            return;

        if (!Overlaps(range))
        {
            ranges.Clear();
            Length = 0;
            return;
        }

        for (int currentIndex = 0; currentIndex < ranges.Count;)
        {
            var current = ranges[currentIndex];

            if (current.Overlaps(range))
            {
                var intersect = new SequenceRange(Math.Max(current.Start, range.Start), Math.Min(current.End, range.End));
                ranges[currentIndex] = intersect;
                Length += intersect.Length - current.Length;
                currentIndex++;
            }
            else
            {
                ranges.RemoveAt(currentIndex);
                Length -= current.Length;
            }
        }
    }

    public void ExceptWith(SequenceRange range)
    {
        if (!Overlaps(range))
            return;

        int currentIndex = ranges.Count > 1 && ranges[^2].End < range.Start ? ranges.Count - 1 : 0;
        do
        {
            var current = ranges[currentIndex];

            // |← range →| |← current →|
            if (range.End < current.Start)
                return;

            // |← current →| |← range →|
            if (current.End < range.Start)
                continue;

            // Here, current.Overlaps(range) == true
            if (range.End < current.End)
            {
                if (current.Start < range.Start)
                {
                    //   |← range →|
                    // |←  current  →|
                    ranges[currentIndex] = new(current.Start, range.Start - 1);
                    ranges.Insert(currentIndex + 1, new SequenceRange(range.End + 1, current.End));
                    Length -= range.Length;
                }
                else
                {
                    // |←  range  →|
                    //   |← current →|
                    ranges[currentIndex] = new(range.End + 1, current.End);
                    Length -= range.End - current.Start + 1;
                }
                return;
            }

            if (current.Start < range.Start)
            {
                //   |←  range  →|
                // |← current →|
                ranges[currentIndex] = new(current.Start, range.Start - 1);
                Length -= current.End - range.Start + 1;
                currentIndex++;
            }
            else
            {
                // |←    range    →|
                //   |← current →|
                ranges.RemoveAt(currentIndex);
                Length -= current.Length;
            }
        }
        while (currentIndex < ranges.Count);
    }

    public void SymmetricExceptWith(SequenceRange range)
    {
        if (range.IsDefault)
            return;

        if (IsEmpty)
        {
            ranges.Insert(0, range);
            Length += range.Length;
            return;
        }

        int currentIndex = ranges.Count > 1 && AheadOfDistantly(ranges[^2], range) ? ranges.Count - 1 : 0;

        for (; currentIndex < ranges.Count; currentIndex++)
        {
            var current = ranges[currentIndex];

            if (AheadOfDistantly(range, current))
            {
                // |← (prev) →|  |← range →|  |← current →|
                ranges.Insert(currentIndex, range);
                Length += range.Length;
                return;
            }

            if (AheadOfDistantly(current, range))
                continue;

            if (range.End + 1 == current.Start)
            {
                // |← range →|← current →|
                ranges[currentIndex] = new(range.Start, current.End);
                Length += range.Length;
                return;
            }

            if (current.End + 1 == range.Start)
            {
                // |← current →|← range →|
                int nextIndex = currentIndex + 1;
                var next = nextIndex == ranges.Count ? default : ranges[nextIndex];

                if (nextIndex == ranges.Count || AheadOfDistantly(range, next))
                {
                    // |← current →|← range →|  |← next →|
                    ranges[currentIndex] = new(current.Start, range.End);
                    Length += range.Length;
                    return;
                }
                else if (range.End + 1 == next.Start)
                {
                    // |← current →|← range →|← next →|
                    ranges[currentIndex] = new(current.Start, next.End);
                    Length += range.Length;
                    ranges.RemoveAt(nextIndex);
                    return;
                }
                else
                {
                    // |← current →|← range  →|
                    //                |← next ~
                    ranges[currentIndex] = new(current.Start, next.Start - 1);
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
                    ranges[currentIndex] = new(range.End + 1, current.End);
                    ranges.Insert(currentIndex, before);
                    //Length += before.Length - (range.End - current.Start + 1);
                    Length += 2 * current.Start - range.Start - range.End - 1;
                    return;
                }
                else
                {
                    // (1) |←    range      →|
                    // (2) |←    range   →|
                    //        |← current →|
                    ranges[currentIndex] = before;
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
                    ranges[currentIndex] = new(range.End + 1, current.End);
                    Length -= range.Length;
                    return;
                }
                else if (range.End == current.End)
                {
                    // |←  range  →|
                    // |← current →|
                    ranges.RemoveAt(currentIndex);
                    Length -= range.Length;
                    return;
                }
                else
                {
                    int nextIndex = currentIndex + 1;
                    var next = nextIndex == ranges.Count ? default : ranges[nextIndex];

                    if (nextIndex == ranges.Count || AheadOfDistantly(range, next))
                    {
                        // |←    range   →|
                        // |← current →|     |← next →|
                        ranges[currentIndex] = new(current.End + 1, range.End);
                        //Length += range.End - current.End - current.Length;
                        Length += range.End + current.Start - 2 * current.End - 1;
                        return;
                    }
                    else if (range.End + 1 == next.Start)
                    {
                        // |←    range   →|
                        // |← current →|  |← next →|
                        ranges[currentIndex] = new(current.End + 1, next.End);
                        ranges.RemoveAt(nextIndex);
                        //Length += range.Length - 2 * current.Length;
                        Length += range.End + current.Start - 2 * current.End - 1;
                        return;
                    }
                    else
                    {
                        // |←    range      →|
                        // |← current →|  |← next ~
                        ranges[currentIndex] = new(current.End + 1, next.Start - 1);
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
                ranges[currentIndex] = new(current.Start, range.Start - 1);

                if (range.End < current.End)
                    ranges.Insert(currentIndex + 1, new SequenceRange(range.End + 1, current.End));

                Length -= range.Length;
                return;
            }
            else
            {
                //      |← range →|
                // |← current →|
                ranges[currentIndex] = new(current.Start, range.Start - 1);
                Length -= current.End - range.Start + 1;
                range = new(current.End + 1, range.End);
                continue;
            }
        }

        // |← location →|  |← range →|
        ranges.Add(range);
        Length += range.Length;
    }

    private static bool AheadOfDistantly(SequenceRange preceding, SequenceRange succeeding)
        => preceding.End + 1 < succeeding.Start;
}
