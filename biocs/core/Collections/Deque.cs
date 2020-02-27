using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Biocs.Collections
{
    /// <summary>
    /// Represents a double-ended queue with a dynamic array.
    /// </summary>
    /// <typeparam name="T">The element type of the double-ended queue.</typeparam>
    /// <seealso cref="LinkedList{T}"/>
    /// <seealso cref="Queue{T}"/>
    [DebuggerDisplay("Count = {Count}"), DebuggerTypeProxy(typeof(CollectionDebugView<>))]
    [SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix")]
    public sealed class Deque<T> : IList<T>, IReadOnlyList<T>
    {
        private T[] items;
        private int head;
        private int tail;
        private int version;

        /// <summary>
        /// Initializes a new instance of the <see cref="Deque{T}"/> class that is empty and has zero capacity.
        /// </summary>
        public Deque()
            : this(0)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Deque{T}"/> class that is empty and has the specified initial capacity.
        /// </summary>
        /// <param name="capacity">The initial number of elements that the <see cref="Deque{T}"/> can contain.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> is less than 0.</exception>
        public Deque(int capacity)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));

            items = new T[capacity];
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Deque{T}"/> class that contains elements copied from the specified
        /// <see cref="IEnumerable{T}"/>.
        /// </summary>
        /// <param name="collection">
        /// The <see cref="IEnumerable{T}"/> whose elements are copied to the new <see cref="Deque{T}"/>.
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="collection"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// The elements are copied onto the <see cref="Deque{T}"/> in the same order they are read by the enumerator of
        /// <paramref name="collection"/>. If the type of <paramref name="collection"/> implements <see cref="ICollection{T}"/>,
        /// <see cref="ICollection{T}.CopyTo"/> is used to copy elements.
        /// </remarks>
        public Deque(IEnumerable<T> collection)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));

            if (collection is ICollection<T> coll)
            {
                items = new T[coll.Count];

                if (items.Length > 0)
                {
                    coll.CopyTo(items, 0);
                    Count = items.Length;
                    tail = Count - 1;
                }
            }
            else
            {
                items = Array.Empty<T>();

                foreach (var item in collection)
                    AddLast(item);
            }
        }

        /// <summary>
        /// Gets or sets the element at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get or set.</param>
        /// <returns>The element at the specified index.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <para><paramref name="index"/> is less than 0.</para> -or- 
        /// <para><paramref name="index"/> is equal to or greater than <see cref="Count"/>.</para>
        /// </exception>
        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= Count)
                    ThrowHelper.ThrowArgumentOutOfRange(nameof(index));

                return items[GetArrayIndex(index)];
            }
            set
            {
                if (index < 0 || index >= Count)
                    ThrowHelper.ThrowArgumentOutOfRange(nameof(index));

                items[GetArrayIndex(index)] = value;
                version++;
            }
        }

        /// <summary>
        /// Gets the number of elements actually contained in the <see cref="Deque{T}"/>.
        /// </summary>
        public int Count { get; private set; }

        /// <summary>
        /// Gets or sets the total number of elements the internal data structure can hold without resizing.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The value in a set operation is less than <see cref="Count"/>.
        /// </exception>
        public int Capacity
        {
            get => items.Length;
            set
            {
                if (value < Count)
                    ThrowHelper.ThrowArgumentOutOfRange(nameof(value));

                if (value != items.Length)
                    EnsureCapacity(value);
            }
        }

        /// <summary>
        /// Gets or sets the first element of the <see cref="Deque{T}"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">The <see cref="Deque{T}"/> is empty.</exception>
        public T First
        {
            [StringResourceUsage("InvalOp.EmptyCollection")]
            get
            {
                if (Count == 0)
                    ThrowHelper.ThrowInvalidOperation(Res.GetString("InvalOp.EmptyCollection"));

                return items[head];
            }
            [StringResourceUsage("InvalOp.EmptyCollection")]
            set
            {
                if (Count == 0)
                    ThrowHelper.ThrowInvalidOperation(Res.GetString("InvalOp.EmptyCollection"));

                items[head] = value;
                version++;
            }
        }

        /// <summary>
        /// Gets or sets the last element of the <see cref="Deque{T}"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">The <see cref="Deque{T}"/> is empty.</exception>
        public T Last
        {
            [StringResourceUsage("InvalOp.EmptyCollection")]
            get
            {
                if (Count == 0)
                    ThrowHelper.ThrowInvalidOperation(Res.GetString("InvalOp.EmptyCollection"));

                return items[tail];
            }
            [StringResourceUsage("InvalOp.EmptyCollection")]
            set
            {
                if (Count == 0)
                    ThrowHelper.ThrowInvalidOperation(Res.GetString("InvalOp.EmptyCollection"));

                items[tail] = value;
                version++;
            }
        }

        bool ICollection<T>.IsReadOnly => false;

        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="Deque{T}"/>.
        /// </summary>
        /// <returns>An <see cref="IEnumerator{T}"/> for the <see cref="Deque{T}"/>.</returns>
        /// <remarks>
        /// If changes are made to the collection, the next call to <see cref="IEnumerator.MoveNext"/> throws 
        /// an <see cref="InvalidOperationException"/>.
        /// </remarks>
        [StringResourceUsage("InvalOp.ModifiedCollection", ResourceCheckOnly = true)]
        public IEnumerator<T> GetEnumerator()
        {
            int version = this.version;

            for (int i = 0; i < Count; i++)
            {
                yield return items[GetArrayIndex(i)];

                // After the first time MoveNext() method returns true, changes were made to the collection.
                if (version != this.version)
                    throw new InvalidOperationException(Res.GetString("InvalOp.ModifiedCollection"));
            }
        }

        /// <summary>
        /// Copies the <see cref="Deque{T}"/> elements to an existing one-dimensional <see cref="Array"/>.
        /// </summary>
        /// <param name="array">
        /// The one-dimensional <see cref="Array"/> that is the destination of the elements copied from <see cref="Deque{T}"/>.
        /// </param>
        /// <param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param>
        /// <exception cref="ArgumentNullException"><paramref name="array"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="arrayIndex"/> is less than 0.</exception>
        /// <exception cref="ArgumentException">
        /// The number of elements in the <see cref="Deque{T}"/> is greater than the available space from
        /// <paramref name="arrayIndex"/> to the end of the destination <paramref name="array"/>.
        /// </exception>
        public void CopyTo(T[] array, int arrayIndex) => CopyTo(0, array, arrayIndex, Count);

        /// <summary>
        /// Copies a range of elements from the <see cref="Deque{T}"/> to an existing one-dimensional <see cref="Array"/>.
        /// </summary>
        /// <param name="index">The zero-based index in the <see cref="Deque{T}"/> at which copying begins.</param>
        /// <param name="array">
        /// The one-dimensional <see cref="Array"/> that is the destination of the elements copied from <see cref="Deque{T}"/>.
        /// </param>
        /// <param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param>
        /// <param name="count">The number of elements to copy.</param>
        /// <exception cref="ArgumentNullException"><paramref name="array"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="index"/>, <paramref name="arrayIndex"/> or <paramref name="count"/> is less than 0.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <para><paramref name="count"/> is greater than the number of elements from <paramref name="index"/> to the end of 
        /// the <see cref="Deque{T}"/>.</para> -or- <para><paramref name="count"/> is greater than the available space from 
        /// <paramref name="arrayIndex"/> to the end of the destination <paramref name="array"/>.</para>
        /// </exception>
        [StringResourceUsage("Arg.InvalidCopySrcRange", 2)]
        [StringResourceUsage("Arg.InvalidCopyDestRange", 2)]
        public void CopyTo(int index, T[] array, int arrayIndex, int count)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));

            if (array == null)
                throw new ArgumentNullException(nameof(array));

            if (arrayIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));

            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            if (index + count > Count)
                throw new ArgumentException(Res.GetString("Arg.InvalidCopySrcRange", count, Count - index));

            if (arrayIndex + count > array.Length)
                throw new ArgumentException(Res.GetString("Arg.InvalidCopyDestRange", count, array.Length - arrayIndex));

            if (count > 0)
            {
                // The position in array from which copy begins
                int start = GetArrayIndex(index);
                int count2 = items.Length - start;

                if (count <= count2)
                {
                    Array.Copy(items, start, array, arrayIndex, count);
                }
                else
                {
                    Array.Copy(items, start, array, arrayIndex, count2);
                    Array.Copy(items, 0, array, arrayIndex + count2, count - count2);
                }
            }
        }

        /// <summary>
        /// Determines whether an element is in the <see cref="Deque{T}"/>.
        /// </summary>
        /// <param name="item">The value to locate in the <see cref="Deque{T}"/>.</param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="item"/> is found in the <see cref="Deque{T}"/>;
        /// otherwise, <see langword="false"/>.
        /// </returns>
        public bool Contains(T item) => IndexOf(item) >= 0;

        /// <summary>
        /// Searches for the specified value and returns the zero-based index of the first occurrence within 
        /// the <see cref="Deque{T}"/>.
        /// </summary>
        /// <param name="item">The value to locate in the <see cref="Deque{T}"/>.</param>
        /// <returns>
        /// The zero-based index of the first occurrence of <paramref name="item"/> within the <see cref="Deque{T}"/>, if found;
        /// otherwise, -1.
        /// </returns>
        /// <remarks>
        /// This method determines equality using the default equality comparer <see cref="EqualityComparer{T}.Default"/>.
        /// </remarks>
        public int IndexOf(T item)
        {
            var comparer = EqualityComparer<T>.Default;

            for (int i = 0; i < Count; i++)
            {
                if (comparer.Equals(items[GetArrayIndex(i)], item))
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Adds a new element at the start of the <see cref="Deque{T}"/>.
        /// </summary>
        /// <param name="item">The value to add at the start of the <see cref="Deque{T}"/>.</param>
        public void AddFirst(T item)
        {
            if (Count == items.Length)
                EnsureCapacity();

            if (Count == 0)
            {
                head = 0;
                tail = 0;
            }
            else
                Decrement(ref head);

            items[head] = item;
            Count++;
            version++;
        }

        /// <summary>
        /// Adds a new element at the end of the <see cref="Deque{T}"/>.
        /// </summary>
        /// <param name="item">The value to add at the end of the <see cref="Deque{T}"/>.</param>
        public void AddLast(T item)
        {
            if (Count == 0)
            {
                AddFirst(item);
                return;
            }

            if (Count == items.Length)
                EnsureCapacity();

            Increment(ref tail);

            items[tail] = item;
            Count++;
            version++;
        }

        /// <summary>
        /// Inserts an element into the <see cref="Deque{T}"/> at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param>
        /// <param name="item">The value to insert.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <para><paramref name="index"/> is less than 0.</para> -or- 
        /// <para><paramref name="index"/> is greater than <see cref="Count"/>.</para>
        /// </exception>
        public void Insert(int index, T item)
        {
            if (index < 0 || index > Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            if (index == 0)
            {
                AddFirst(item);
                return;
            }

            if (index == Count)
            {
                AddLast(item);
                return;
            }

            if (Count == items.Length)
            {
                EnsureCapacityAndInsert(index, item);
                return;
            }

            int insert;

            // The direction of movement depends on the insetion position
            if (index <= Count / 2)
            {
                insert = GetArrayIndex(index - 1);

                CopyBlockStartward(head, insert, 1);
                Decrement(ref head);
            }
            else
            {
                insert = GetArrayIndex(index);

                CopyBlockEndward(insert, tail, 1);
                Increment(ref tail);
            }
            items[insert] = item;

            Count++;
            version++;
        }

        /// <summary>
        /// Inserts the elements of a collection into the <see cref="Deque{T}"/> at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which the new elements should be inserted.</param>
        /// <param name="collection">The collection whose elements should be inserted into the <see cref="Deque{T}"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="collection"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <para><paramref name="index"/> is less than 0.</para> -or- 
        /// <para><paramref name="index"/> is greater than <see cref="Count"/>.</para>
        /// </exception>
        public void InsertRange(int index, IEnumerable<T> collection)
        {
            if (index < 0 || index > Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            if (collection == null)
                throw new ArgumentNullException(nameof(collection));

            if (collection is ICollection<T> coll)
            {
                int collCount = coll.Count;

                if (collCount == 0)
                    return;

                if (Count + collCount > items.Length)
                {
                    EnsureCapacityAndInsertRange(index, coll);
                    return;
                }

                // Self assignment
                if (this == collection)
                {
                    int splitIndex = GetArrayIndex(index);

                    if (index < Count)
                    {
                        // Copy [index, Count)
                        CopyBlockEndward(splitIndex, tail, Count - index);
                        Increment(ref tail, Count - index);
                    }

                    if (index > 0)
                    {
                        // Copy [0, index)
                        Decrement(ref splitIndex);
                        CopyBlockStartward(head, splitIndex, index);
                        Decrement(ref head, index);
                    }
                    Count *= 2;
                    version++;
                    return;
                }

                int insert;

                if (Count == 0)
                {
                    insert = 0;
                    head = 0;
                    tail = collCount - 1;
                }
                else if (index == 0)
                {
                    Decrement(ref head, collCount);
                    insert = head;
                }
                else if (index == Count)
                {
                    insert = tail;
                    Increment(ref insert);
                    Increment(ref tail, collCount);
                }
                else if (index <= Count / 2)
                {
                    insert = GetArrayIndex(index);
                    Decrement(ref insert, collCount);

                    CopyBlockStartward(head, GetArrayIndex(index - 1), collCount);
                    Decrement(ref head, collCount);
                }
                else
                {
                    insert = GetArrayIndex(index);

                    CopyBlockEndward(insert, tail, collCount);
                    Increment(ref tail, collCount);
                }

                if (insert + collCount <= items.Length)
                {
                    coll.CopyTo(items, insert);
                }
                else
                {
                    foreach (var item in collection)
                    {
                        items[insert] = item;
                        Increment(ref insert);
                    }
                }
                Count += collCount;
                version++;
            }
            else
            {
                foreach (var item in collection)
                    Insert(index++, item);
            }
        }

        /// <summary>
        /// Removes the element at the start of the <see cref="Deque{T}"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">The <see cref="Deque{T}"/> is empty.</exception>
        [StringResourceUsage("InvalOp.EmptyCollection")]
        public void RemoveFirst()
        {
            if (Count == 0)
                throw new InvalidOperationException(Res.GetString("InvalOp.EmptyCollection"));

            items[head] = default!;
            Count--;
            Increment(ref head);
            version++;
        }

        /// <summary>
        /// Removes the element at the end of the <see cref="Deque{T}"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">The <see cref="Deque{T}"/> is empty.</exception>
        [StringResourceUsage("InvalOp.EmptyCollection")]
        public void RemoveLast()
        {
            if (Count == 0)
                throw new InvalidOperationException(Res.GetString("InvalOp.EmptyCollection"));

            items[tail] = default!;
            Count--;
            Decrement(ref tail);
            version++;
        }

        /// <summary>
        /// Removes the first occurrence of a specific element from the <see cref="Deque{T}"/>.
        /// </summary>
        /// <param name="item">The element to remove from the <see cref="Deque{T}"/>.</param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="item"/> is successfully removed; otherwise, <see langword="false"/>.
        /// </returns>
        public bool Remove(T item)
        {
            int index = IndexOf(item);

            if (index >= 0)
            {
                RemoveAt(index);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Removes the element at the specified index of the <see cref="Deque{T}"/>.
        /// </summary>
        /// <param name="index">The zero-based index of the element to remove.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <para><paramref name="index"/> is less than 0.</para> -or- 
        /// <para><paramref name="index"/> is equal to or greater than <see cref="Count"/>.</para>
        /// </exception>
        public void RemoveAt(int index)
        {
            if (index < 0 || index >= Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            if (index == 0)
            {
                RemoveFirst();
                return;
            }

            if (index == Count - 1)
            {
                RemoveLast();
                return;
            }

            if (index <= Count / 2)
            {
                CopyBlockEndward(head, GetArrayIndex(index - 1), 1);

                items[head] = default!;
                Increment(ref head);
            }
            else
            {
                CopyBlockStartward(GetArrayIndex(index + 1), tail, 1);

                items[tail] = default!;
                Decrement(ref tail);
            }
            Count--;
            version++;
        }

        /// <summary>
        /// Removes a range of elements from the <see cref="Deque{T}"/>.
        /// </summary>
        /// <param name="index">The zero-based starting index of the range of elements to remove.</param>
        /// <param name="count">The number of elements to remove.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <para><paramref name="index"/> is less than 0.</para> -or- <para><paramref name="count"/> is less than 0.</para>
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="index"/> and <paramref name="count"/> do not denote a valid range of elements in 
        /// the <see cref="Deque{T}"/>.
        /// </exception>
        [StringResourceUsage("Arg.InvalidRemoveRange", 3)]
        public void RemoveRange(int index, int count)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));

            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            if (index + count > Count)
                throw new ArgumentException(Res.GetString("Arg.InvalidRemoveRange", index, count, Count));

            if (count == 0)
                return;

            // Range to clear
            int start, end;

            if (index <= Count - index - count)
            {
                if (index > 0)
                    CopyBlockEndward(head, GetArrayIndex(index - 1), count);

                start = head;
                end = GetArrayIndex(count - 1);

                Increment(ref head, count);
            }
            else
            {
                if (index + count < Count)
                    CopyBlockStartward(GetArrayIndex(index + count), tail, count);

                start = GetArrayIndex(Count - count);
                end = tail;

                Decrement(ref tail, count);
            }

            if (start <= end)
            {
                Array.Clear(items, start, end - start + 1);
            }
            else
            {
                Array.Clear(items, start, items.Length - start);
                Array.Clear(items, 0, end + 1);
            }
            Count -= count;
            version++;
        }

        /// <summary>
        /// Removes all elements from the <see cref="Deque{T}"/>.
        /// </summary>
        public void Clear()
        {
            if (Count > 0)
            {
                if (head <= tail)
                {
                    Array.Clear(items, head, Count);
                }
                else
                {
                    Array.Clear(items, head, items.Length - head);
                    Array.Clear(items, 0, tail + 1);
                }
                Count = 0;
            }
            head = 0;
            tail = 0;
            version++;
        }

        private int GetArrayIndex(int dequeIndex)
        {
            Debug.Assert(dequeIndex >= 0 && dequeIndex < Count);

            return (dequeIndex + head) % items.Length;
        }

        private void Increment(ref int arrayIndex) => Increment(ref arrayIndex, 1);

        private void Increment(ref int arrayIndex, int value)
        {
            Debug.Assert(arrayIndex >= 0 && arrayIndex < items.Length);

            arrayIndex = (arrayIndex + value) % items.Length;
        }

        private void Decrement(ref int arrayIndex) => Decrement(ref arrayIndex, 1);

        private void Decrement(ref int arrayIndex, int value)
        {
            Debug.Assert(arrayIndex >= 0 && arrayIndex < items.Length);

            if (arrayIndex >= value)
                arrayIndex -= value;
            else
                arrayIndex += items.Length - value;
        }

        private void EnsureCapacity() => EnsureCapacity(items.Length < 3 ? 4 : items.Length * 2);

        private void EnsureCapacity(int capacity)
        {
            Debug.Assert(capacity >= Count);

            var dest = new T[capacity];

            if (Count > 0)
                CopyTo(dest, 0);

            items = dest;
            head = 0;
            tail = Count == 0 ? 0 : Count - 1;
            version++;
        }

        private void EnsureCapacityAndInsert(int index, T item)
        {
            Debug.Assert(index > 0 && index < Count);

            var dest = new T[items.Length * 2];

            CopyTo(0, dest, 0, index);
            dest[index] = item;
            CopyTo(index, dest, index + 1, Count - index);

            items = dest;
            Count++;
            head = 0;
            tail = Count - 1;
            version++;
        }

        private void EnsureCapacityAndInsertRange(int index, ICollection<T> collection)
        {
            Debug.Assert(index >= 0 && index <= Count);
            Debug.Assert(collection != null);

            var dest = new T[Count + collection.Count];

            CopyTo(0, dest, 0, index);
            collection.CopyTo(dest, index);
            CopyTo(index, dest, index + collection.Count, Count - index);

            items = dest;
            Count = dest.Length;
            head = 0;
            tail = Count == 0 ? 0 : Count - 1;
            version++;
        }

        // Copy [from, to] to [from - distance, to - distance] (arguments are actual indices in array)
        private void CopyBlockStartward(int start, int end, int distance)
        {
            Debug.Assert(start >= 0 && start < items.Length);
            Debug.Assert(end >= 0 && end < items.Length);
            Debug.Assert(distance > 0 && distance < items.Length);

            if (start <= end)
            {
                if (start >= distance)
                {
                    // Copy [start, end] toward the head of array
                    Array.Copy(items, start, items, start - distance, end - start + 1);
                }
                else if (end < distance)
                {
                    // Copy [start, end] toward the tail of array
                    Array.Copy(items, start, items, start - distance + items.Length, end - start + 1);
                }
                else
                {
                    // Copy [start, distance) to [*, array.Length)
                    Array.Copy(items, start, items, start - distance + items.Length, distance - start);

                    // Copy [distance, end] to [0, *]
                    Array.Copy(items, distance, items, 0, end - distance + 1);
                }
            }
            else
            {
                // Copy [start, array.Length)
                Array.Copy(items, start, items, start - distance, items.Length - start);

                // Copy [0, end]
                CopyBlockStartward(0, end, distance);
            }
        }

        // Copy [from, to] to [from + distance, to + distance] (arguments are actual indices in array)
        private void CopyBlockEndward(int start, int end, int distance)
        {
            Debug.Assert(start >= 0 && start < items.Length);
            Debug.Assert(end >= 0 && end < items.Length);
            Debug.Assert(distance > 0 && distance < items.Length);

            if (start <= end)
            {
                if (end + distance < items.Length)
                {
                    // Copy [start, end] toward the tail of array
                    Array.Copy(items, start, items, start + distance, end - start + 1);
                }
                else if (start + distance >= items.Length)
                {
                    // Copy [start, end] toward the head of array
                    Array.Copy(items, start, items, start + distance - items.Length, end - start + 1);
                }
                else
                {
                    // Copy [array.Length - distance, end] to [0, *]
                    Array.Copy(items, items.Length - distance, items, 0, end + distance - items.Length + 1);

                    // Copy [start, array.Length - distance) to [*, array.Lentgh)
                    Array.Copy(items, start, items, start + distance, items.Length - start - distance);
                }
            }
            else
            {
                // Copy [0, end]
                Array.Copy(items, 0, items, distance, end + 1);

                // Copy [start, array.Length)
                CopyBlockEndward(start, items.Length - 1, distance);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        void ICollection<T>.Add(T item) => AddLast(item);
    }
}
