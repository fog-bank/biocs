using System.Diagnostics.CodeAnalysis;

namespace Biocs;

/// <summary>
/// Represents a continuous range of the presented biological sequence.
/// </summary>
public readonly struct SequenceRange : IEquatable<SequenceRange>, IComparable<SequenceRange>, IComparable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SequenceRange"/> structure.
    /// </summary>
    public SequenceRange(int start, int end)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(start, end);
        Start = start;
        End = end;
    }

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
    /// Converts the current <see cref="SequenceRange"/> instance to its equivalent string representation.
    /// </summary>
    /// <returns>The string representation of the current instance.</returns>
    public override string ToString()
    {
        return Start == End ? $"{Start}" : $"{Start}..{End}";
    }

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

    #region Explicit Interface Implementations

    [StringResourceUsage("Arg.CompareToNotSameTypedObject")]
    int IComparable.CompareTo(object? obj) => obj switch
    {
        null => 1,
        SequenceRange other => CompareTo(other),
        _ => throw new ArgumentException(Res.GetString("Arg.CompareToNotSameTypedObject"), nameof(obj))
    };

    #endregion
}
