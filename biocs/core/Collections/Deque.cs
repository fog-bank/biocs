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
	/// <seealso cref="LinkedList{T}" />
	/// <seealso cref="Queue{T}" />
	[DebuggerDisplay("Count = {Count}"), DebuggerTypeProxy(typeof(CollectionDebugView<>))]
	[SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix")]
	public sealed class Deque<T> : IList<T>, IReadOnlyCollection<T>
	{
		private T[] array;
		private int head;
		private int tail;
		private int size;
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
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> is less than zero.</exception>
		public Deque(int capacity)
		{
			if (capacity < 0)
				throw new ArgumentOutOfRangeException(nameof(capacity));

			array = new T[capacity];
		}

		/// <summary>
		/// Gets or sets the element at the specified index.
		/// </summary>
		/// <param name="index">The zero-based index of the element to get or set.</param>
		/// <returns>The element at the specified index.</returns>
		/// <exception cref="ArgumentOutOfRangeException">
		/// <paramref name="index"/> is less than 0. -or- <paramref name="index"/> is equal to or greater than <see cref="Count"/>.
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
		/// Gets the number of elements actually contained in the <see cref="Deque{T}" />.
		/// </summary>
		public int Count => size;

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

		public IEnumerator<T> GetEnumerator()
		{
			throw new NotImplementedException();
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			throw new NotImplementedException();
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

		public void AddFirst(T item)
		{
			throw new NotImplementedException();
		}

		public void AddLast(T item)
		{
			throw new NotImplementedException();
		}

		public void Insert(int index, T item)
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
			head = (head + 1) % array.Length;
			size--;
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

			if (tail >= 1)
				tail--;
			else
				tail = array.Length - 1;

			size--;
			version++;
		}

		public bool Remove(T item)
		{
			throw new NotImplementedException();
		}

		public void RemoveAt(int index)
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

			return (head + dequeIndex) % array.Length;
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		void ICollection<T>.Add(T item) => AddLast(item);
	}
}
