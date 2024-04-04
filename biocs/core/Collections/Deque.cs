using System.Collections;
using System.Diagnostics;

namespace Biocs.Collections;

/// <summary>
/// Represents a double-ended queue with a dynamic array.
/// </summary>
/// <typeparam name="T">The element type of the double-ended queue.</typeparam>
/// <seealso cref="LinkedList{T}"/>
/// <seealso cref="Queue{T}"/>
[DebuggerDisplay("Count = {Count}"), DebuggerTypeProxy(typeof(CollectionDebugView<>))]
public sealed class Deque<T> : IList<T>, IReadOnlyList<T>, IList
{
    private T[] items;
    private int head;
    private int tail;
    private int version;

    /// <summary>
    /// Initializes a new instance of the <see cref="Deque{T}"/> class that is empty and has the specified initial capacity.
    /// </summary>
    /// <param name="capacity">The initial number of elements that the <see cref="Deque{T}"/> can contain.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> is less than 0.</exception>
    public Deque(int capacity = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);

        items = new T[capacity];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Deque{T}"/> class that contains elements copied from the specified
    /// collection.
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
        ArgumentNullException.ThrowIfNull(collection);

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
            items = [];

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
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Count);

            return items[GetArrayIndex(index)];
        }
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Count);

            items[GetArrayIndex(index)] = value;
            version++;
        }
    }

    /// <inheritdoc cref="IReadOnlyCollection{T}.Count"/>
    public int Count { get; private set; }

    /// <summary>
    /// Gets or sets the total number of elements the internal data structure can hold without resizing.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The value in a set operation is less than <see cref="Count"/>.</exception>
    public int Capacity
    {
        get => items.Length;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, Count);

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
        get
        {
            ThrowIfEmpty();

            return items[head];
        }
        set
        {
            ThrowIfEmpty();

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
        get
        {
            ThrowIfEmpty();

            return items[tail];
        }
        set
        {
            ThrowIfEmpty();

            items[tail] = value;
            version++;
        }
    }

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
                ThrowHelper.ThrowInvalidOperation(Res.GetString("InvalOp.ModifiedCollection"));
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
    [StringResourceUsage("Arg.InvalidCopyDestRange", 2)]
    public void CopyTo(T[] array, int arrayIndex)
    {
        ArgumentNullException.ThrowIfNull(array);
        ArgumentOutOfRangeException.ThrowIfNegative(arrayIndex);

        if (arrayIndex + Count > array.Length)
            ThrowHelper.ThrowArgument(Res.GetString("Arg.InvalidCopyDestRange", Count, array.Length - arrayIndex));

        CopyTo(0, array.AsSpan(arrayIndex), Count);
    }

    /// <summary>
    /// Copies a range of elements from the <see cref="Deque{T}"/> to a <see cref="Span{T}"/>.
    /// </summary>
    /// <param name="index">The zero-based index in the <see cref="Deque{T}"/> at which copying begins.</param>
    /// <param name="count">The number of elements to copy.</param>
    /// <param name="destination">The span that is the destination of the elements copied from <see cref="Deque{T}"/>.</param>
    /// <exception cref="ArgumentException">
    /// <para><paramref name="count"/> is greater than the number of elements from <paramref name="index"/> to the end of 
    /// the <see cref="Deque{T}"/>.</para> -or- <para>The number of elements to copy is greater than the number of elements
    /// that the <paramref name="destination"/> can contain.</para>
    /// </exception>
    [StringResourceUsage("Arg.InvalidCopySrcRange", 2)]
    [StringResourceUsage("Arg.InvalidCopyDestRange", 2)]
    public void CopyTo(int index, Span<T> destination, int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        if (index + count > Count)
            ThrowHelper.ThrowArgument(Res.GetString("Arg.InvalidCopySrcRange", count, Count - index));

        if (count > destination.Length)
            ThrowHelper.ThrowArgument(Res.GetString("Arg.InvalidCopyDestRange", count, destination.Length));

        if (count > 0)
        {
            // The position in array from which copy begins
            int start = GetArrayIndex(index);
            int count2 = items.Length - start;

            if (count <= count2)
                items.AsSpan(start, count).CopyTo(destination);
            else
            {
                items.AsSpan(start).CopyTo(destination);
                items.AsSpan(0, count - count2).CopyTo(destination[count2..]);
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
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, Count);

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
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, Count);
        ArgumentNullException.ThrowIfNull(collection);

        if (collection is ICollection<T> coll)
        {
            int collCount = coll.Count;

            if (collCount == 0)
                return;

            if (Count + collCount > items.Length)
            {
                EnsureCapacityAndInsertRange(index, coll);
            }
            else if (this == collection)
            {
                // Self assignment
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
            }
            else
            {
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
    public void RemoveFirst()
    {
        ThrowIfEmpty();

        items[head] = default!;
        Count--;
        Increment(ref head);
        version++;
    }

    /// <summary>
    /// Removes the element at the end of the <see cref="Deque{T}"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">The <see cref="Deque{T}"/> is empty.</exception>
    public void RemoveLast()
    {
        ThrowIfEmpty();

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
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Count);

        if (index <= Count / 2)
        {
            if (index > 0)
                CopyBlockEndward(head, GetArrayIndex(index - 1), 1);

            RemoveFirst();
        }
        else
        {
            if (index + 1 < Count)
                CopyBlockStartward(GetArrayIndex(index + 1), tail, 1);

            RemoveLast();
        }
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
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        if (index + count > Count)
            ThrowHelper.ThrowArgument(Res.GetString("Arg.InvalidRemoveRange", index, count, Count));

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
            items.AsSpan(start..(end + 1)).Clear();
        else
        {
            items.AsSpan(start).Clear();
            items.AsSpan(0, end + 1).Clear();
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
                items.AsSpan(head, Count).Clear();
            else
            {
                items.AsSpan(head).Clear();
                items.AsSpan(0, tail + 1).Clear();
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

    private void Increment(ref int arrayIndex, int value = 1)
    {
        Debug.Assert(arrayIndex >= 0 && arrayIndex < items.Length);

        arrayIndex = (arrayIndex + value) % items.Length;
    }

    private void Decrement(ref int arrayIndex, int value = 1)
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

        CopyTo(0, dest.AsSpan(), index);
        dest[index] = item;
        CopyTo(index, dest.AsSpan(index + 1), Count - index);

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

        CopyTo(0, dest.AsSpan(), index);
        collection.CopyTo(dest, index);
        CopyTo(index, dest.AsSpan(index + collection.Count), Count - index);

        items = dest;
        Count = dest.Length;
        head = 0;
        tail = dest.Length - 1;
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
                items.AsSpan(start..(end + 1)).CopyTo(items.AsSpan(start - distance));
            }
            else if (end < distance)
            {
                // Copy [start, end] toward the tail of array
                items.AsSpan(start..(end + 1)).CopyTo(items.AsSpan(start - distance + items.Length));
            }
            else
            {
                // Copy [start, distance) to [*, array.Length)
                items.AsSpan(start..distance).CopyTo(items.AsSpan(start - distance + items.Length));

                // Copy [distance, end] to [0, *]
                items.AsSpan(distance..(end + 1)).CopyTo(items);
            }
        }
        else
        {
            // Copy [start, array.Length)
            items.AsSpan(start).CopyTo(items.AsSpan(start - distance));

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
                items.AsSpan(start..(end + 1)).CopyTo(items.AsSpan(start + distance));
            }
            else if (start + distance >= items.Length)
            {
                // Copy [start, end] toward the head of array
                items.AsSpan(start..(end + 1)).CopyTo(items.AsSpan(start + distance - items.Length));
            }
            else
            {
                // Copy [array.Length - distance, end] to [0, *]
                items.AsSpan((items.Length - distance)..(end + 1)).CopyTo(items.AsSpan());

                // Copy [start, array.Length - distance) to [*, array.Lentgh)
                items.AsSpan(start..(items.Length - distance)).CopyTo(items.AsSpan(start + distance));
            }
        }
        else
        {
            // Copy [0, end]
            items.AsSpan(0, end + 1).CopyTo(items.AsSpan(distance));

            // Copy [start, array.Length)
            CopyBlockEndward(start, items.Length - 1, distance);
        }
    }

    [StringResourceUsage("InvalOp.EmptyCollection")]
    private void ThrowIfEmpty()
    {
        if (Count == 0)
            ThrowHelper.ThrowInvalidOperation(Res.GetString("InvalOp.EmptyCollection"));
    }

    #region Explicit Interface Implementations

    object? IList.this[int index]
    {
        get => this[index];
        [StringResourceUsage("Arg.IncompatibleObjectForCollection")]
        set
        {
            switch (value)
            {
                case T item:
                    this[index] = item;
                    return;

                case null when default(T) is null:
                    this[index] = default!;
                    return;
            }
            throw new ArgumentException(Res.GetString("Arg.IncompatibleObjectForCollection"));
        }
    }

    bool IList.IsFixedSize => false;

    bool ICollection<T>.IsReadOnly => false;

    bool IList.IsReadOnly => false;

    bool ICollection.IsSynchronized => false;

    object ICollection.SyncRoot => this;

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    [StringResourceUsage("Arg.InvalidCopyDestRange", 2)]
    void ICollection.CopyTo(Array array, int index)
    {
        ArgumentNullException.ThrowIfNull(array);
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        if (index + Count > array.Length)
            ThrowHelper.ThrowArgument(Res.GetString("Arg.InvalidCopyDestRange", Count, array.Length - index));

        if (Count > 0)
        {
            try
            {
                if (head <= tail)
                    Array.Copy(items, head, array, index, Count);
                else
                {
                    int count2 = items.Length - head;
                    Array.Copy(items, head, array, index, count2);
                    Array.Copy(items, 0, array, index + count2, Count - count2);
                }
            }
            catch (Exception ex) when (ex is ArrayTypeMismatchException or RankException or InvalidCastException)
            {
                throw new ArgumentException(null, nameof(array), ex);
            }
        }
    }

    bool IList.Contains(object? value) => value switch
    {
        T item => Contains(item),
        null when default(T) is null => Contains(default!),
        _ => false
    };

    int IList.IndexOf(object? value) => value switch
    {
        T item => IndexOf(item),
        null when default(T) is null => IndexOf(default!),
        _ => -1
    };

    void ICollection<T>.Add(T item) => AddLast(item);

    int IList.Add(object? value)
    {
        switch (value)
        {
            case T item:
                AddLast(item);
                return Count - 1;

            case null when default(T) is null:
                AddLast(default!);
                return Count - 1;

            default:
                return -1;
        }
    }

    [StringResourceUsage("Arg.IncompatibleObjectForCollection")]
    void IList.Insert(int index, object? value)
    {
        switch (value)
        {
            case T item:
                Insert(index, item);
                return;

            case null when default(T) is null:
                Insert(index, default!);
                return;
        }
        throw new ArgumentException(Res.GetString("Arg.IncompatibleObjectForCollection"));
    }

    void IList.Remove(object? value)
    {
        switch (value)
        {
            case T item:
                Remove(item);
                break;

            case null when default(T) is null:
                Remove(default!);
                break;
        }
    }

    #endregion
}
