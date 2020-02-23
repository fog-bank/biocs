using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Biocs.Collections
{
    /// <summary>
    /// Represents a tally counter to count the frequency of items.
    /// </summary>
    /// <typeparam name="T">The type of items to count.</typeparam>
    /// <remarks><see cref="Counter{T}"/> accepts null as a valid value for reference types.</remarks>
    [DebuggerDisplay("NumberOfItems = {NumberOfItems}, TotalCount = {TotalCount}"), DebuggerTypeProxy(typeof(CounterDebugView<>))]
    public class Counter<T>
    {
        private readonly Dictionary<T, int> map;
        private int? nullCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="Counter{T}"/> class that is empty, has zero capacity, and uses 
        /// the default equality comparer.
        /// </summary>
        public Counter() : this(0)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Counter{T}"/> class that is empty, has the specified initial capacity,
        /// and uses the default equality comparer.
        /// </summary>
        /// <param name="capacity">The initial number of items that the <see cref="Counter{T}"/> can contain.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> is less than 0.</exception>
        public Counter(int capacity) : this(capacity, null)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Counter{T}"/> class that is empty, has zero capacity, and uses 
        /// the specified equality comparer.
        /// </summary>
        /// <param name="comparer">
        /// The <see cref="IEqualityComparer{T}"/> implementation to use when comparing items, or <see langword="null"/> to
        /// use the default <see cref="IEqualityComparer{T}"/> for the type of the item.
        /// </param>
        public Counter(IEqualityComparer<T> comparer) : this(0, comparer)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Counter{T}"/> class that is empty, has the specified initial capacity,
        /// and uses the specified equality comparer.
        /// </summary>
        /// <param name="capacity">The initial number of items that the <see cref="Counter{T}"/> can contain.</param>
        /// <param name="comparer">
        /// The <see cref="IEqualityComparer{T}"/> implementation to use when comparing items, or <see langword="null"/> to
        /// use the default <see cref="IEqualityComparer{T}"/> for the type of the item.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> is less than 0.</exception>
        public Counter(int capacity, IEqualityComparer<T> comparer)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));

            map = new Dictionary<T, int>(capacity, comparer);
        }

        /// <summary>
		/// Initializes a new instance of the <see cref="Counter{T}"/> class that contains unique items and counts copied from
        /// the specified <see cref="Counter{T}"/> and uses the same equality comparer.
		/// </summary>
		/// <param name="other">
        /// The <see cref="Counter{T}"/> whose unique items and counts are copied to the new <see cref="Counter{T}"/>.
        /// </param>
		/// <exception cref="ArgumentNullException"><paramref name="other"/> is <see langword="null"/>.</exception>
		public Counter(Counter<T> other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            map = new Dictionary<T, int>(other.map, other.map.Comparer);
            nullCount = other.nullCount;
            TotalCount = other.TotalCount;
        }

        /// <summary>
        /// Gets the total count of items.
        /// </summary>
        public int TotalCount { get; private set; }

        /// <summary>
        /// Gets the number of the kinds of items that the <see cref="Counter{T}"/> contains.
        /// </summary>
        public int NumberOfItems => nullCount.HasValue ? map.Count + 1 : map.Count;

        /// <summary>
        /// Gets an enumerable collection of unique items that the <see cref="Counter{T}"/> has counted before now.
        /// </summary>
        /// <remarks>
        /// <para>This enumerable collection also contains items whose the count is 0.</para>
        /// <para>Enumerators retured by this enumerable collection cannot be used to modify the <see cref="Counter{T}"/>.
        /// For example, the following enumeration raises an <see cref="InvalidOperationException"/>.</para>
        /// <code>
        /// var counter = new Counter&lt;int&gt;();
        /// counter.AddRange(new[] { 1, 2, 3 });
        /// foreach (int item in counter.UniqueItems)
        /// {
        ///   counter.Reset(item);
        /// }
        /// </code>
        /// </remarks>
        public IEnumerable<T> UniqueItems => nullCount.HasValue ? KeysWithNull : map.Keys;

        /// <summary>
        /// Gets an enumerable collection that contains items repeated by each count.
        /// </summary>
        public IEnumerable<T> RepeatedItems
        {
            get
            {
                foreach (var pair in map)
                {
                    for (int n = pair.Value; n > 0; n--)
                        yield return pair.Key;
                }

                if (nullCount.HasValue)
                {
                    for (int n = nullCount.Value; n > 0; n--)
                        yield return default;
                }
            }
        }

        /// <summary>
        /// Gets the <see cref="IEqualityComparer{T}"/> that is used to determine equality of items for 
        /// the <see cref="Counter{T}"/>. 
        /// </summary>
        public IEqualityComparer<T> Comparer => map.Comparer;

        private IEnumerable<T> KeysWithNull
        {
            get
            {
                foreach (var item in map.Keys)
                    yield return item;

                yield return default;
            }
        }

        /// <summary>
        /// Gets the number of times that the item occurs in the <see cref="Counter{T}"/>.
        /// </summary>
        /// <param name="item">The object to get the count.</param>
        /// <returns>The number of times that <paramref name="item"/> occurs in the <see cref="Counter{T}"/>.</returns>
        /// <remarks>If <paramref name="item"/> is not contained in the <see cref="Counter{T}"/>, this method returns 0.</remarks>
        public int GetCount(T item)
        {
            if (item == null)
                return nullCount.GetValueOrDefault();

            map.TryGetValue(item, out int count);
            return count;
        }

        /// <summary>
        /// Copies the <see cref="Counter{T}"/> unique items to an existing one-dimensional <see cref="Array"/>,
        /// starting at the specified array index.
        /// </summary>
        /// <param name="array">
        /// The one-dimensional <see cref="Array"/> that is the destination of the unique items copied
        /// from <see cref="Counter{T}"/>.
        /// </param>
        /// <param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param>
        /// <exception cref="ArgumentNullException"><paramref name="array"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="arrayIndex"/> is less than 0.</exception>
        /// <exception cref="ArgumentException">
        /// The number of items in the <see cref="Counter{T}"/> is greater than the available space from
        /// <paramref name="arrayIndex"/> to the end of the destination <paramref name="array"/>.
        /// </exception>
        [StringResourceUsage("Arg.InvalidCopyDestRange", 2)]
        public void CopyTo(T[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            if (arrayIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));

            if (arrayIndex + NumberOfItems > array.Length)
            {
                throw new ArgumentException(
                    Res.GetString("Arg.InvalidCopyDestRange", NumberOfItems, array.Length - arrayIndex));
            }
            map.Keys.CopyTo(array, arrayIndex);

            if (nullCount.HasValue)
                array[arrayIndex + map.Count] = default;
        }

        /// <summary>
        /// Determines whether the <see cref="Counter{T}"/> contains the specified object.
        /// </summary>
        /// <param name="item">The object to locate in the <see cref="Counter{T}"/>.</param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="item"/> is found in the <see cref="Counter{T}"/>;
        /// otherwise, <see langword="false"/>.
        /// </returns>
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
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="times"/> is less than 0.</exception>
        public void Add(T item, int times)
        {
            if (times < 0)
                throw new ArgumentOutOfRangeException(nameof(times));

            if (item != null)
                map[item] = GetCount(item) + times;
            else
                nullCount = GetCount(item) + times;

            TotalCount += times;
        }

        /// <summary>
        /// Counts the items of the specified collection.
        /// </summary>
        /// <param name="items">The collection whose items should be counted.</param>
        /// <exception cref="ArgumentNullException"><paramref name="items"/> is <see langword="null"/>.</exception>
        public void AddRange(IEnumerable<T> items)
        {
            if (items == null)
                throw new ArgumentNullException(nameof(items));

            foreach (var item in items)
                Add(item);
        }

        /// <summary>
        /// Decreases the count of the specified item by one.
        /// </summary>
        /// <param name="item">The item to decrement the count value.</param>
        /// <returns>
        /// <see langword="true"/> if the count of <paramref name="item" /> was successfully decremented;
        /// otherwise, <see langword="false"/>.
        /// </returns>
        public bool Remove(T item) => Remove(item, 1) == 1;

        /// <summary>
        /// Decreases the count of the specified item by the specified amount.
        /// </summary>
        /// <param name="item">The item to decrement the count value.</param>
        /// <param name="times">The amount by which to decrement the counter value.</param>
        /// <returns>The amount of the count to be decreased acutually.</returns>
        public int Remove(T item, int times)
        {
            if (times < 0)
                throw new ArgumentOutOfRangeException(nameof(times));

            int count = GetCount(item);
            if (count == 0)
                return 0;

            int count2 = Math.Max(count - times, 0);

            if (item != null)
                map[item] = count2;
            else
                nullCount = count2;

            TotalCount -= count - count2;
            return count - count2;
        }

        /// <summary>
        /// Sets the number of times that each item occurs in the <see cref="Counter{T}"/> to zero.
        /// The collection of items is preserved.
        /// </summary>
        public void Reset()
        {
            var items = new T[map.Count];
            map.Keys.CopyTo(items, 0);

            foreach (var item in items)
                map[item] = 0;

            if (nullCount.HasValue)
                nullCount = 0;

            TotalCount = 0;
        }

        /// <summary>
        /// Sets the number of times that the specified item occurs in the <see cref="Counter{T}"/> to zero.
        /// </summary>
        /// <param name="item">The item to reset the count.</param>
        public void Reset(T item)
        {
            TotalCount -= GetCount(item);

            if (item == null)
                nullCount = 0;
            else
                map[item] = 0;
        }

        /// <summary>
        /// Removes all items from the <see cref="Counter{T}"/>.
        /// </summary>
        public void Clear()
        {
            map.Clear();
            nullCount = null;
            TotalCount = 0;
        }
    }

    [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
    internal sealed class CounterDebugView<T>
    {
        private readonly Counter<T> counter;

        public CounterDebugView(Counter<T> counter) => this.counter = counter;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public KeyValuePair<T, int>[] ItemsAndCounts
        {
            get
            {
                if (counter != null)
                {
                    var pairs = new KeyValuePair<T, int>[counter.NumberOfItems];
                    int i = 0;

                    foreach (var item in counter.UniqueItems)
                        pairs[i++] = new KeyValuePair<T, int>(item, counter.GetCount(item));

                    return pairs;
                }
                else
                    return CollectionTools.Empty<KeyValuePair<T, int>>();
            }
        }
    }
}
