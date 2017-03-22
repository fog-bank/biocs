using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Biocs.Collections
{
    /// <summary>
    /// Represents a tally counter to count the frequency of elements.
    /// </summary>
    /// <typeparam name="T">The type of elements to count.</typeparam>
    [DebuggerDisplay("NumberOfItems = {NumberOfItems}, TotalCount = {TotalCount}")]
    public class Counter<T>
    {
        private readonly Dictionary<T, int> map;
        private int? nullCount;
        private int totalCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="Counter{T}"/> class that is empty and uses the default equality comparer.
        /// </summary>
        public Counter()
            : this(0)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Counter{T}"/> class that is empty, has the specified initial capacity
        /// and uses the default equality comparer.
        /// </summary>
        /// <param name="capacity">The initial number of elements that the <see cref="Counter{T}"/> can contain.</param>
        public Counter(int capacity)
            : this(capacity, null)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Counter{T}"/> class that is empty, has the specified initial capacity,
        /// and uses the specified equality comparer.
        /// </summary>
        /// <param name="capacity">The initial number of elements that the <see cref="Counter{T}"/> can contain.</param>
        /// <param name="comparer">
        /// The <see cref="IEqualityComparer{T}"/> implementation to use when comparing elements, or null to use the default
        /// <see cref="IEqualityComparer{T}"/> for the type of the element.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> is less than 0.</exception>
        public Counter(int capacity, IEqualityComparer<T> comparer)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));

            map = new Dictionary<T, int>(capacity, comparer);
        }

        /// <summary>
        /// Gets the total of all counts.
        /// </summary>
        public int TotalCount => totalCount;

        /// <summary>
        /// Gets the number of the kinds of items that the <see cref="Counter{T}"/> contains.
        /// </summary>
        public int NumberOfItems => nullCount.HasValue ? map.Count + 1 : map.Count;

        /// <summary>
		/// Gets an enumerable collection of distinct elements that the <see cref="Counter{T}"/> has counted before now.
		/// </summary>
		/// <remarks>
		/// Enumerators retured by this enumerable collection cannot be used to modify the <see cref="Counter{T}"/>.
		/// For example, the following code will raise an <see cref="InvalidOperationException"/>.
		/// <code>
		/// var counter = new Counter&lt;int&gt;();
		/// counter.Add(new[] { 1, 2, 3 });
		/// foreach (var item in counter.Items)
		/// {
		///   counter.Add(item);
		/// }
		/// </code>
		/// </remarks>
		public IEnumerable<T> Items => nullCount.HasValue ? map.Keys.Concat(new[] { default(T) }) : map.Keys;

        /// <summary>
		/// Gets the <see cref="IEqualityComparer{T}"/> that is used to determine equality of elements for the <see cref="Counter{T}"/>. 
		/// </summary>
		public IEqualityComparer<T> Comparer => map.Comparer;

        /// <summary>
		/// Gets the number of times that the element occurs in the <see cref="Counter{T}"/>.
		/// </summary>
		/// <param name="item">The object to get the count.</param>
		/// <returns>The number of times that <paramref name="item"/> occurs in the <see cref="Counter{T}"/>.</returns>
		/// <remarks>If <paramref name="item"/> is not contained in the <see cref="Counter{T}"/>, this method will return 0.</remarks>
		public int GetCount(T item)
        {
            if (item == null)
                return nullCount.GetValueOrDefault();

            map.TryGetValue(item, out int count);
            return count;
        }

        /// <summary>
		/// Determines whether the <see cref="Counter{T}"/> contains the specified object.
		/// </summary>
		/// <param name="item">The object to locate in the <see cref="Counter{T}"/>.</param>
		/// <returns>true if <paramref name="item"/> is found in the <see cref="Counter{T}"/>; otherwise, false.</returns>
        public bool Contains(T item) => item == null ? nullCount.HasValue : map.ContainsKey(item);

        /// <summary>
		/// Counts an object once.
		/// </summary>
		/// <param name="item">The object to be counted to the <see cref="Counter{T}"/>.</param>
		public void Add(T item) => Add(item, 1);

        /// <summary>
		/// Counts an object a specified number of times.
		/// </summary>
		/// <param name="item">The object to be counted to the <see cref="Counter{T}"/>.</param>
		/// <param name="times">The number of times to count <paramref name="item"/>.</param>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="times"/> is less than zero.</exception>
		public void Add(T item, int times)
        {
            if (times < 0)
                throw new ArgumentOutOfRangeException("times");

            if (item != null)
                map[item] = GetCount(item) + times;
            else
                nullCount = GetCount(item) + times;

            totalCount += times;
        }

        /// <summary>
		/// Counts the elements of the specified collection.
		/// </summary>
		/// <param name="items">The collection whose elements should be counted.</param>
		/// <exception cref="ArgumentNullException"><paramref name="items"/> is null.</exception>
		/// <remarks>If this method throws an exception while counting, the state of <see cref="Counter{T}"/> is undefined.</remarks>
        public void Add(IEnumerable<T> items)
        {
            if (items == null)
                throw new ArgumentNullException("items");

            foreach (var item in items)
                Add(item);
        }

        /// <summary>
        /// Sets the number of times that each element occurs in the <see cref="Counter{T}"/> to zero. The collection of elements is preserved.
        /// </summary>
        public void Reset()
        {
            var items = new T[map.Count];
            map.Keys.CopyTo(items, 0);

            foreach (var item in items)
                map[item] = 0;

            nullCount = 0;
            totalCount = 0;
        }

        /// <summary>
		/// Sets the number of times that the specified element occurs in the <see cref="Counter{T}"/> to zero.
		/// </summary>
		/// <param name="item">The element to reset the count.</param>
		public void Reset(T item)
        {
            totalCount -= GetCount(item);

            if (item == null)
                nullCount = 0;
            else
                map[item] = 0;
        }

        /// <summary>
		/// Removes all elements from the <see cref="Counter{T}"/>.
		/// </summary>
		public void Clear()
        {
            map.Clear();
            nullCount = null;
            totalCount = 0;
        }
    }
}
