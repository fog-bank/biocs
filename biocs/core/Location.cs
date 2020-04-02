using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Biocs
{
    /// <summary>
    /// Represents the region of the biological sequence.
    /// </summary>
    /// <remarks>
    /// Compliant with [The DDBJ/ENA/GenBank Feature Table Definition](http://www.insdc.org/files/feature_table.html)
    /// </remarks>
    public class Location : IEquatable<Location?>, IComparable<Location?>
    {
        private int start;
        private int end;
        private LocationOperator locationOperator;

        public int Start => IsSpan ? start : Elements[0].Start;

        public int End => IsSpan ? end : Elements[^1].End;

        public int Length { get; }

        public bool IsExactStart { get; }

        public bool IsExactEnd { get; }

        public string? SequenceID { get; }

        public IReadOnlyList<Location> Elements { get; }

        public bool IsSpan => locationOperator == LocationOperator.Span;

        public bool IsComplement { get; }

        public bool Equals(Location? other)
        {
            if (ReferenceEquals(this, other))
                return true;

            if (other is null)
                return false;

            throw new NotImplementedException();
        }

        public override bool Equals(object? obj) => Equals(obj as Location);

        public int CompareTo(Location? other)
        {
            if (ReferenceEquals(this, other))
                return 0;

            if (other is null)
                return 1;

            throw new NotImplementedException();
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Start, End, IsComplement);
        }

        public override string ToString()
        {
            throw new NotImplementedException();
        }

        public static Location Parse(string value)
        {
            throw new NotImplementedException();
        }

        public static bool operator ==(Location? left, Location? right)
        {
            return left is null ? right is null : left.Equals(right);
        }

        public static bool operator !=(Location? left, Location? right) => !(left == right);

        public static bool operator <(Location? left, Location? right)
        {
            return left is null ? right is object : left.CompareTo(right) < 0;
        }

        public static bool operator <=(Location? left, Location? right)
        {
            return left is null || left.CompareTo(right) <= 0;
        }

        public static bool operator >(Location? left, Location? right) => right < left;

        public static bool operator >=(Location? left, Location? right) => right <= left;
    }

    internal enum LocationOperator
    {
        Span,
        Site,
        Join,
        Complement,
        Order
    }
}
