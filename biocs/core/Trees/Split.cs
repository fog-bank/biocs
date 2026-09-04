using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Biocs.Trees;

/// <summary>
/// Represents a bipartition of a phylogenetic tree.
/// </summary>
public sealed class Split : IEquatable<Split>
{
    private readonly BitArray bits;

    /// <summary>
    /// Initializes a new instance of <see cref="Split"/> that represents a exterior branch.
    /// </summary>
    /// <param name="count">The number of leaf nodes in a tree.</param>
    /// <param name="index">The zero-based index of the leaf node.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <para><paramref name="count"/> is less than 2.</para> -or- <para><paramref name="index"/> is negative.</para> -or-
    /// <para><paramref name="index"/> is greater than or equal to <paramref name="count"/>.</para>
    /// </exception>
    public Split(int count, int index)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 2);
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, count);

        LeafCount = count;

        if (index > 0)
        {
            bits = new(count - 1);
            bits.Set(index - 1, true);
        }
        else
            bits = new(count - 1, true);
    }

    /// <summary>
    /// Initializes a new instance of <see cref="Split"/> that represents a interior branch.
    /// </summary>
    /// <param name="count">The number of leaf nodes in a tree.</param>
    /// <param name="indices">The zero-based indices of leaf nodes on the same side.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <para><paramref name="count"/> is less than 2.</para> -or- <para>Any of <paramref name="indices"/> is negative.</para>
    /// -or- <para>Any of <paramref name="indices"/> is greater than or equal to <paramref name="count"/>.</para>
    /// </exception>
    public Split(int count, ReadOnlySpan<int> indices)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 2);

        LeafCount = count;
        bits = new BitArray(count - 1);
        bool not = false;

        foreach (int index in indices)
        {
            if (index == 0)
                not = true;
            else
                bits.Set(index - 1, true);
        }

        if (not)
            bits.Not();
    }

    // Deep copy constructor
    private Split(Split other)
    {
        LeafCount = other.LeafCount;
        bits = new(other.bits);
    }

    /// <summary>
    /// Gets the number of leave nodes in the tree associated with this instance.
    /// </summary>
    public int LeafCount { get; }

    /// <summary>
    /// Gets a value indicating whether this split represents a bipartition where one is an empty subset.
    /// </summary>
    public bool IsEmpty => !bits.HasAnySet();

    /// <summary>
    /// Returns a new <see cref="Split"/> that represents the parent of two splits.
    /// </summary>
    /// <param name="one">The first split.</param>
    /// <param name="other">The second split.</param>
    /// <returns>A new <see cref="Split"/>.</returns>
    /// <exception cref="InvalidOperationException">The two splits have different <see cref="LeafCount"/> values.</exception>
    [StringResourceUsage("InvalOp.NotEqualSplit", 2)]
    public static Split FromChildren(Split one, Split other)
    {
        if (one.LeafCount != other.LeafCount)
            ThrowHelper.ThrowInvalidOperation(Res.GetString("InvalOp.NotEqualSplit", one.LeafCount, other.LeafCount));

        var union = new Split(one);
        union.bits.Xor(other.bits);
        return union;
    }

    [StringResourceUsage("Arg.EmptyCollection")]
    [StringResourceUsage("InvalOp.NotEqualSplit", 2)]
    public static Split FromChildren(IEnumerable<Split> splits)
    {
        Split? union = null;

        foreach (var split in splits)
        {
            if (union == null)
                union = new(split);
            else
            {
                if (union.LeafCount != split.LeafCount)
                    ThrowHelper.ThrowInvalidOperation(Res.GetString("InvalOp.NotEqualSplit", union.LeafCount, split.LeafCount));

                union.bits.Xor(split.bits);
            }
        }

        if (union == null)
            ThrowHelper.ThrowArgument(Res.GetString("Arg.EmptyCollection"), nameof(splits));

        return union;
    }

    public bool IsSameSide(int one, int other)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(one);
        ArgumentOutOfRangeException.ThrowIfNegative(other);

        return Get(one) == Get(other);
    }

    /// <summary>
    /// Tests whether this split represents a exterior branch and gets the leaf index.
    /// </summary>
    /// <returns>The zero-based index of the leaf if this split represents a exterior branch; otherwise -1.</returns>
    /// <remarks>A trivial bipartion means that one of two subsets is one leaf and the other is the rest.</remarks>
    public int IsTrivial()
    {
        if (bits.HasAllSet())
            return 0;

        var bytes = AsBytes();
        int index8 = bytes.IndexOfAnyExcept((byte)0);

        if (index8 == -1)
            return -1;

        byte bit = bytes[index8];
        if (byte.PopCount(bit) > 1 || bytes[(index8 + 1)..].ContainsAnyExcept((byte)0))
            return -1;

        int offset = byte.TrailingZeroCount(bit);
        int index = index8 * 8 + offset;
        Debug.Assert(bits.Get(index));
        return index + 1;
    }

    /// <summary>
    /// Determines whether the current split is equal to another <see cref="Split"/>.
    /// </summary>
    /// <param name="other">The <see cref="Split"/> instance to compare.</param>
    /// <returns><see langword="true"/> if the two splits represent the same bipartition.</returns>
    public bool Equals(Split? other)
    {
        if (other == null || LeafCount != other.LeafCount)
            return false;

        var bytes = AsBytes();
        var otherBytes = other.AsBytes();
        return bytes.SequenceEqual(otherBytes);
    }

    /// <inheritdoc/>
    public sealed override bool Equals(object? obj) => Equals(obj as Split);

    /// <summary>
    /// Computes a hash code for this instance.
    /// </summary>
    public sealed override int GetHashCode()
    {
        var hash = new HashCode();
        var bytes = AsBytes();
        hash.AddBytes(bytes);
        return hash.ToHashCode();
    }

    private bool Get(int index) => index > 0 && bits[index - 1];

    private ReadOnlySpan<byte> AsBytes()
    {
        var bytes = CollectionsMarshal.AsBytes(bits);
        // Should not use MemoryMarshal.Cast<byte, int>() for this span.
        // (There is no guarantee that the length of the internal array is a multiple of 4.)
#if DEBUG
        // Validate CollectionsMarshal.AsBytes() and clearing extra bits.
        var array = new byte[(bits.Length + 7) / 8];
        bits.CopyTo(array, 0);
        Debug.Assert(bits.Length > 0);
        Debug.Assert(bytes.SequenceEqual(array));

        int offset = bits.Length % 8;
        if (offset != 0)
        {
            uint lastBit = bytes[^1];
            uint mask = (1u << offset) - 1;
            Debug.Assert((lastBit & mask) == lastBit);
        }
#endif
        return bytes;
    }
}
