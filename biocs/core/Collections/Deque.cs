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
		private T[] array;
		private int size;
		private int head;
		private int tail;
		private int version;

		/// <summary>
		/// Initializes a new instance of the <see cref="Deque{T}"/> class that is empty.
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

			array = new T[capacity];
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Deque{T}"/> class that contains elements copied from the specified <see cref="IEnumerable{T}"/>.
		/// </summary>
		/// <param name="collection">The <see cref="IEnumerable{T}"/> whose elements are copied to the new <see cref="Deque{T}"/>.</param>
		/// <exception cref="ArgumentNullException"><paramref name="collection"/> is a null reference.</exception>
		/// <remarks>
		/// The elements are copied onto the <see cref="Deque{T}"/> in the same order they are read
		/// by the enumerator of <paramref name="collection"/>. If the type of <paramref name="collection"/>
		/// implements <see cref="ICollection{T}"/>, <see cref="ICollection{T}.CopyTo"/> is used to copy elements.
		/// </remarks>
		public Deque(IEnumerable<T> collection)
		{
			if (collection == null)
				throw new ArgumentNullException(nameof(collection));

			var coll = collection as ICollection<T>;
			if (coll != null)
			{
				array = new T[coll.Count];

				if (array.Length > 0)
				{
					coll.CopyTo(array, 0);
					size = array.Length;
					tail = size - 1;
				}
			}
			else
			{
				array = new T[0];

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
		/// <paramref name="index"/> is less than 0. -or- 
		/// <paramref name="index"/> is equal to or greater than <see cref="Count"/>.
		/// </exception>
		public T this[int index]
		{
			get
			{
				if (index < 0 || index >= size)
					throw new ArgumentOutOfRangeException(nameof(index));

				return array[GetArrayIndex(index)];
			}
			set
			{
				if (index < 0 || index >= size)
					throw new ArgumentOutOfRangeException(nameof(index));

				array[GetArrayIndex(index)] = value;
				version++;
			}
		}

		/// <summary>
		/// Gets the number of elements actually contained in the <see cref="Deque{T}"/>.
		/// </summary>
		public int Count => size;

		/// <summary>
		/// Gets or sets the total number of elements the internal data structure can hold without resizing.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">The value in a set operation is less than <see cref="Count"/>.</exception>
		public int Capacity
		{
			get { return array.Length; }
			set
			{
				if (value < size)
					throw new ArgumentOutOfRangeException(nameof(value));

				if (value != array.Length)
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
				if (size == 0)
					throw new InvalidOperationException(Res.GetString("InvalOp.EmptyCollection"));

				return array[head];
			}
			[StringResourceUsage("InvalOp.EmptyCollection")]
			set
			{
				if (size == 0)
					throw new InvalidOperationException(Res.GetString("InvalOp.EmptyCollection"));

				array[head] = value;
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
				if (size == 0)
					throw new InvalidOperationException(Res.GetString("InvalOp.EmptyCollection"));

				return array[tail];
			}
			[StringResourceUsage("InvalOp.EmptyCollection")]
			set
			{
				if (size == 0)
					throw new InvalidOperationException(Res.GetString("InvalOp.EmptyCollection"));

				array[tail] = value;
				version++;
			}
		}

		bool ICollection<T>.IsReadOnly => false;

		/// <summary>
		/// Returns an enumerator that iterates through the <see cref="Deque{T}"/>.
		/// </summary>
		/// <returns>An <see cref="IEnumerator{T}"/> for the <see cref="Deque{T}"/>.</returns>
		/// <remarks>
		/// If changes are made to the collection, the next call to <see cref="IEnumerator.MoveNext"/>
		/// throws an <see cref="InvalidOperationException"/>.
		/// </remarks>
		[StringResourceUsage("InvalOp.ModifiedCollection", ResourceCheckOnly = true)]
		public IEnumerator<T> GetEnumerator()
		{
			int version = this.version;

			for (int i = 0; i < size; i++)
			{
				yield return array[GetArrayIndex(i)];

				// After the first time MoveNext() method returns true, changes were made to the collection.
				if (version != this.version)
					throw new InvalidOperationException(Res.GetString("InvalOp.ModifiedCollection"));
			}
		}

		/// <summary>
		/// Copies the <see cref="Deque{T}"/> elements to an existing one-dimensional <see cref="Array"/>.
		/// </summary>
		/// <param name="array">The one-dimensional <see cref="Array"/> that is the destination of the elements copied from <see cref="Deque{T}"/>.</param>
		/// <param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param>
		/// <exception cref="ArgumentNullException"><paramref name="array"/> is a null reference.</exception>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="arrayIndex"/> is less than 0.</exception>
		/// <exception cref="ArgumentException">
		/// The number of elements in the <see cref="Deque{T}"/> is greater than the available space 
		/// from <paramref name="arrayIndex"/> to the end of the destination <paramref name="array"/>.
		/// </exception>
		public void CopyTo(T[] array, int arrayIndex)
		{
			CopyTo(0, array, arrayIndex, size);
		}

		/// <summary>
		/// Copies a range of elements from the <see cref="Deque{T}"/> to an existing one-dimensional <see cref="Array"/>.
		/// </summary>
		/// <param name="index">The zero-based index in the <see cref="Deque{T}"/> at which copying begins.</param>
		/// <param name="array">The one-dimensional <see cref="Array"/> that is the destination of the elements copied from <see cref="Deque{T}"/>.</param>
		/// <param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param>
		/// <param name="count">The number of elements to copy.</param>
		/// <exception cref="ArgumentNullException"><paramref name="array"/> is a null reference.</exception>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/>, <paramref name="arrayIndex"/> or <paramref name="count"/> is less than 0.</exception>
		/// <exception cref="ArgumentException">
		/// <paramref name="count"/> is greater than the number of elements from <paramref name="index"/> to the end of the <see cref="Deque{T}"/>. -or-
		/// <paramref name="count"/> is greater than the available space from <paramref name="arrayIndex"/> to the end of the destination <paramref name="array"/>. 
		/// </exception>
		[StringResourceUsage("ArgEx.InvalidCopyRange", 3)]
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

			if (index + count > size || arrayIndex + count > array.Length)
				throw new ArgumentException(Res.GetString("ArgEx.InvalidCopyRange", count, size - index, array.Length - arrayIndex));

			// The position in array from which copy begins
			int start = GetArrayIndex(index);
			int count2 = this.array.Length - start;

			if (count <= count2)
			{
				Array.Copy(this.array, start, array, arrayIndex, count);
			}
			else
			{
				Array.Copy(this.array, start, array, arrayIndex, count2);
				Array.Copy(this.array, 0, array, arrayIndex + count2, count - count2);
			}
		}

		/// <summary>
		/// Determines whether an element is in the <see cref="Deque{T}"/>.
		/// </summary>
		/// <param name="item">The value to locate in the <see cref="Deque{T}"/>.</param>
		/// <returns>true if item is found in the <see cref="Deque{T}"/>; otherwise, false.</returns>
		public bool Contains(T item) => IndexOf(item) >= 0;

		/// <summary>
		/// Searches for the specified value and returns the zero-based index of the first occurrence within the <see cref="Deque{T}"/>.
		/// </summary>
		/// <param name="item">The value to locate in the <see cref="Deque{T}"/>.</param>
		/// <returns>The zero-based index of the first occurrence of element within the <see cref="Deque{T}"/>, if found; otherwise, -1.</returns>
		/// <remarks>
		/// This method determines equality using the default equality comparer <see cref="EqualityComparer{T}.Default"/>.
		/// </remarks>
		public int IndexOf(T item)
		{
			var comparer = EqualityComparer<T>.Default;

			for (int i = 0; i < size; i++)
			{
				if (comparer.Equals(array[GetArrayIndex(i)], item))
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
			if (size == array.Length)
				EnsureCapacity();

			if (size == 0)
			{
				head = 0;
				tail = 0;
			}
			else
				Decrement(ref head);
			
			array[head] = item;
			size++;
			version++;
		}

		/// <summary>
		/// Adds a new element at the end of the <see cref="Deque{T}"/>.
		/// </summary>
		/// <param name="item">The value to add at the end of the <see cref="Deque{T}"/>.</param>
		public void AddLast(T item)
		{
			if (size == 0)
			{
				AddFirst(item);
				return;
			}

			if (size == array.Length)
				EnsureCapacity();

			Increment(ref tail);

			array[tail] = item;
			size++;
			version++;
		}

		/// <summary>
		/// Inserts an element into the <see cref="Deque{T}"/> at the specified index.
		/// </summary>
		/// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param>
		/// <param name="item">The value to insert.</param>
		/// <exception cref="ArgumentOutOfRangeException">
		/// <paramref name="index"/> is less than 0. -or-
		/// <paramref name="index"/> is greater than <see cref="Count"/>.
		/// </exception>
		public void Insert(int index, T item)
		{
			if (index < 0 || index > size)
				throw new ArgumentOutOfRangeException(nameof(index));

			if (index == 0)
			{
				AddFirst(item);
				return;
			}

			if (index == size)
			{
				AddLast(item);
				return;
			}

			if (size == array.Length)
			{
				EnsureCapacityAndInsert(index, item);
				return;
			}

			int insert;

			// The direction of movement depends on the insetion position
			if (index <= size / 2)
			{
				insert = GetArrayIndex(index - 1);

				MoveBlockStartward(head, insert);
				Decrement(ref head);
			}
			else
			{
				insert = GetArrayIndex(index);

				MoveBlockEndward(insert, tail);
				Increment(ref tail);
			}
			array[insert] = item;

			size++;
			version++;
		}

		public void InsertRange(int index, IEnumerable<T> collection)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Removes the element at the start of the <see cref="Deque{T}"/>.
		/// </summary>
		/// <exception cref="InvalidOperationException">The <see cref="Deque{T}"/> is empty.</exception>
		[StringResourceUsage("InvalOp.EmptyCollection")]
		public void RemoveFirst()
		{
			if (size == 0)
				throw new InvalidOperationException(Res.GetString("InvalOp.EmptyCollection"));

			array[head] = default(T);
			size--;
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
			if (size == 0)
				throw new InvalidOperationException(Res.GetString("InvalOp.EmptyCollection"));

			array[tail] = default(T);
			size--;
			Decrement(ref tail);
			version++;
		}

		/// <summary>
		/// Removes the first occurrence of a specific element from the <see cref="Deque{T}"/>.
		/// </summary>
		/// <param name="item">The element to remove from the <see cref="Deque{T}"/>.</param>
		/// <returns>true if <paramref name="item"/> is successfully removed; otherwise, false.</returns>
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
		/// <paramref name="index"/> is less than 0. -or-
		/// <paramref name="index"/> is equal to or greater than <see cref="Count"/>.
		/// </exception>
		public void RemoveAt(int index)
		{
			if (index < 0 || index >= size)
				throw new ArgumentOutOfRangeException(nameof(index));

			if (index == 0)
			{
				RemoveFirst();
				return;
			}

			if (index == size - 1)
			{
				RemoveLast();
				return;
			}

			if (index <= size / 2)
			{
				MoveBlockEndward(head, GetArrayIndex(index - 1));

				array[head] = default(T);
				Increment(ref head);
			}
			else
			{
				MoveBlockStartward(GetArrayIndex(index + 1), tail);

				array[tail] = default(T);
				Decrement(ref tail);
			}
			size--;
			version++;
		}

		public void RemoveRange(int index, int count)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Removes all elements from the <see cref="Deque{T}"/>.
		/// </summary>
		public void Clear()
		{
			if (size > 0)
			{
				if (head <= tail)
				{
					Array.Clear(array, head, size);
				}
				else
				{
					Array.Clear(array, head, array.Length - head);
					Array.Clear(array, 0, tail + 1);
				}
				size = 0;
			}
			head = 0;
			tail = 0;
			version++;
		}

		private int GetArrayIndex(int dequeIndex)
		{
			Debug.Assert(dequeIndex >= 0 && dequeIndex < size);

			return (dequeIndex + head) % array.Length;
		}

		private void Increment(ref int arrayIndex)
		{
			Debug.Assert(arrayIndex >= 0 && arrayIndex < array.Length);

			arrayIndex = (arrayIndex + 1) % array.Length;
		}

		private void Decrement(ref int arrayIndex)
		{
			Debug.Assert(arrayIndex >= 0 && arrayIndex < array.Length);

			if (arrayIndex > 0)
				arrayIndex--;
			else
				arrayIndex = array.Length - 1;
		}

		private void EnsureCapacity()
		{
			EnsureCapacity(array.Length < 3 ? 4 : array.Length * 2);
		}

		private void EnsureCapacity(int capacity)
		{
			Debug.Assert(capacity >= size);

			var dest = new T[capacity];

			if (size > 0)
				CopyTo(dest, 0);

			array = dest;
			head = 0;
			tail = size == 0 ? 0 : size - 1;
			version++;
		}

		private void EnsureCapacityAndInsert(int index, T item)
		{
			Debug.Assert(index > 0 && index < size);

			var dest = new T[array.Length * 2];

			CopyTo(0, dest, 0, index);
			dest[index] = item;
			CopyTo(index, dest, index + 1, size - index);

			array = dest;
			size++;
			head = 0;
			tail = size - 1;
			version++;
		}

		// Move [from, to] to [from - 1, to - 1] (arguments are actual indices in array)
		private void MoveBlockStartward(int start, int end)
		{
			Debug.Assert(start >= 0 && start < array.Length);
			Debug.Assert(end >= 0 && end < array.Length);

			if (start > 0 && start <= end)
			{
				// The block is continuous and the head of array is empty.
				Array.Copy(array, start, array, start - 1, end - start + 1);
			}
			else
			{
				// Move [start, array.Length)
				if (start > end)
					Array.Copy(array, start, array, start - 1, array.Length - start);

				array[array.Length - 1] = array[0];

				// Move [1, end]
				if (end > 0)
					Array.Copy(array, 1, array, 0, end);
			}
			version++;
		}

		// Move [from, to] to [from + 1, to + 1] (arguments are actual indices in array)
		private void MoveBlockEndward(int start, int end)
		{
			Debug.Assert(start >= 0 && start < array.Length);
			Debug.Assert(end >= 0 && end < array.Length);
			
			if (end < array.Length - 1 && start <= end)
			{
				Array.Copy(array, start, array, start + 1, end - start + 1);
			}
			else
			{
				// Move [0, end]
				if (start > end)
					Array.Copy(array, 0, array, 1, end + 1);

				array[0] = array[array.Length - 1];

				// Move [start, array.Length - 2]
				if (start < array.Length - 1)
					Array.Copy(array, start, array, start + 1, array.Length - start - 1);
			}
			version++;
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		void ICollection<T>.Add(T item) => AddLast(item);
	}
}
