using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Biocs.Collections
{
    public class Counter<T>
    {
        private readonly Dictionary<T, int> map;
        private int totalCount;
        private int nullCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="Counter{T}"/> class that is empty and uses the default equality comparer.
        /// </summary>
        public Counter()
            : this(0)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Counter{T}"/> class that is empty, has the specified initial capacity, and uses
        /// the default equality comparer.
        /// </summary>
        public Counter(int capacity)
            : this(capacity, null)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Counter{T}"/> class that is empty, has the specified initial capacity, and uses
        /// the specified equality comparer.
        /// </summary>
        /// <param name="capacity">The initial number of elements that the <see cref="Counter{T}"/> can contain.</param>
        /// <param name="comparer">
        /// The <see cref="IEqualityComparer{T}"/> implementation to use when comparing keys, or null to use the default
        /// <see cref="IEqualityComparer{T}"/> for the type of the key.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> is less than 0.</exception>
        public Counter(int capacity, IEqualityComparer<T> comparer)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));

            map = new Dictionary<T, int>(capacity, comparer);
        }

        public int TotalCount => totalCount;

        public IEqualityComparer<T> Comparer => map.Comparer;
    }
}
