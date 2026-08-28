using System.Collections;
using System.Numerics;

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
        bits = new(other.bits);
        LeafCount = other.LeafCount;
    }

    /// <summary>
    /// Gets the number of leave nodes in the tree associated with this instance.
    /// </summary>
    public int LeafCount { get; }

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
                union = new Split(split);
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

    public int IsTrivial()
    {
        if (bits.HasAllSet())
            return 0;

        var array = new int[(bits.Length + 31) / 32];
        bits.CopyTo(array, 0);

        var span = array.AsSpan();
        int index32 = span.IndexOfAnyExcept(0);

        if (index32 == -1)
            return -1;

        int bit = span[index32];
        if (int.PopCount(bit) > 1 || span[(index32 + 1)..].ContainsAnyExcept(0))
            return -1;

        int offset = int.TrailingZeroCount(bit);
        return index32 * 32 + offset + 1;
    }

    /// <summary>
    /// Determines whether the current split is equal to another <see cref="Split"/>.
    /// </summary>
    /// <param name="other"></param>
    /// <returns><see langword="true"/> if the two splits represent the same bipartition.</returns>
    public bool Equals(Split? other)
    {
        if (other == null || LeafCount != other.LeafCount)
            return false;

        var comp = new BitArray(bits);
        comp.Xor(other.bits);
        return !comp.HasAnySet();
    }

    /// <inheritdoc/>
    public sealed override bool Equals(object? obj) => Equals(obj as Split);

    /// <summary>
    /// Computes a hash code for this instance.
    /// </summary>
    public sealed override int GetHashCode()
    {
        int byteLength = (bits.Length + 7) / 8;
        var bytes = new byte[byteLength];
        bits.CopyTo(bytes, 0);

        var hash = new HashCode();
        hash.AddBytes(bytes);
        return hash.ToHashCode();
    }

    private bool Get(int index) => index > 0 && bits[index - 1];
}
