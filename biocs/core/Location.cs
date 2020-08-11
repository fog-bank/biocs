using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Biocs
{
    /// <summary>
    /// Represents the region of the biological sequence.
    /// </summary>
    /// <remarks>
    /// <para>Each base numbering is one-based indexing.</para>
    /// <para>Compliant with [The DDBJ/ENA/GenBank Feature Table Definition](http://www.insdc.org/files/feature_table.html)</para>
    /// </remarks>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class Location : IEquatable<Location?>, IComparable<Location?>
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

        public string? SequenceID { get; private set; }

        public IReadOnlyList<Location> Elements
        {
            get => elements ?? Array.Empty<Location>();
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

        private string DebuggerDisplay => Elements.Count > 3 ? typeof(Location).Name + "[" + Elements.Count + "]" : ToString();

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

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(Start, End, IsComplement);
        }

        /// <summary>
        /// Converts the current <see cref="Location"/> object to its equivalent <see cref="string"/> representation.
        /// </summary>
        /// <returns>The <see cref="string"/> representation of the current <see cref="Location"/> object.</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();

            if (!string.IsNullOrEmpty(SequenceID))
                sb.Append(SequenceID).Append(':');

            ElementsToString(sb, SequenceID);
            return sb.ToString();
        }

        internal string ToString2()
        {
            string id = string.IsNullOrEmpty(SequenceID) ? string.Empty : SequenceID + ":";

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

            if (SequenceID != parentID && !string.IsNullOrEmpty(SequenceID))
                builder.Append(SequenceID).Append(':');

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

        public static Location Parse(string value)
        {
            throw new NotImplementedException();
        }

        public static bool TryParse(string value, out Location location)
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
}
