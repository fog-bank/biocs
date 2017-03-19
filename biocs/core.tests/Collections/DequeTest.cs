using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Biocs.TestTools;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Biocs.Collections
{
	[TestClass]
	public class DequeTest
	{
		[TestMethod]
		public void Constructor_Test()
		{
			const int count = 3;
			const int capacity = 10;

			// Default constructor
			Assert.AreEqual(0, new Deque<string>().Count);

			// Specify capacity
			var target = new Deque<string>(capacity);
			Assert.AreEqual(0, target.Count);
			Assert.AreEqual(capacity, target.Capacity);

			BiocsAssert.Throws<ArgumentOutOfRangeException>(() => new Deque<object>(-4));

			// ICollection<T>
			var target2 = new Deque<object>(new object[0]);
			Assert.AreEqual(0, target2.Count);
			Assert.AreEqual(0, target2.Capacity);

			var target3 = new Deque<object>(new object[count]);
			Assert.AreEqual(count, target3.Count);
			Assert.AreEqual(count, target3.Capacity);

			// IEnumerable<T>
			Assert.AreEqual(0, new Deque<int>(Enumerable.Empty<int>()).Count);
			Assert.AreEqual(count, new Deque<int>(Enumerable.Range(0, count)).Count);

			BiocsAssert.Throws<ArgumentNullException>(() => new Deque<object>(null));
		}

		[TestMethod]
		public void Item_Test()
		{
			const int count = 6;
			var target = new Deque<int>(Enumerable.Range(0, count));

			Assert.AreEqual(count, target.Count);

			for (int i = 0; i < target.Count; i++)
				Assert.AreEqual(i, target[i]);

			BiocsAssert.Throws<ArgumentOutOfRangeException>(() => target[-1]);
			BiocsAssert.Throws<ArgumentOutOfRangeException>(() => target[count]);
		}

		[TestMethod]
		public void First_Test()
		{
			const int value = 4;
			var target = new Deque<int>(Enumerable.Range(value, 2));

			Assert.AreEqual(value, target.First);

			BiocsAssert.Throws<InvalidOperationException>(() => new Deque<object>().First);
		}

		[TestMethod]
		public void Last_Test()
		{
			const int value = 4;
			var target = new Deque<int>(Enumerable.Range(value, 2).Reverse());

			Assert.AreEqual(value, target.Last);

			BiocsAssert.Throws<InvalidOperationException>(() => new Deque<object>().Last);
		}

		[TestMethod]
		public void GetEnumerator_Test()
		{
			var query = Enumerable.Range(0, 4);

			Assert.IsTrue(query.SequenceEqual(new Deque<int>(query)));

			BiocsAssert.Throws<InvalidOperationException>(() =>
			{
				var target = new Deque<int>(query);

				foreach (int item in target)
					target.AddFirst(item);
			});

			BiocsAssert.Throws<InvalidOperationException>(() =>
			{
				var target = new Deque<int>(query);

				foreach (int item in target)
					target.AddLast(item);
			});

			BiocsAssert.Throws<InvalidOperationException>(() =>
			{
				var target = new Deque<int>(query);

				foreach (int item in target)
					target.Insert(0, item);
			});

			BiocsAssert.Throws<InvalidOperationException>(() =>
			{
				var target = new Deque<int>(query);

				foreach (int item in target)
					target.InsertRange(0, new[] { item });
			});

			BiocsAssert.Throws<InvalidOperationException>(() =>
			{
				var target = new Deque<int>(query);

				foreach (int item in target)
					target.RemoveFirst();
			});

			BiocsAssert.Throws<InvalidOperationException>(() =>
			{
				var target = new Deque<int>(query);

				foreach (int item in target)
					target.RemoveLast();
			});

			BiocsAssert.Throws<InvalidOperationException>(() =>
			{
				var target = new Deque<int>(query);

				foreach (int item in target)
					target.Remove(item);
			});

			BiocsAssert.Throws<InvalidOperationException>(() =>
			{
				var target = new Deque<int>(query);

				foreach (int item in target)
					target.RemoveAt(0);
			});

			BiocsAssert.Throws<InvalidOperationException>(() =>
			{
				var target = new Deque<int>(query);

				foreach (int item in target)
					target.RemoveRange(1, 2);
			});

			BiocsAssert.Throws<InvalidOperationException>(() =>
			{
				var target = new Deque<int>(query);

				foreach (int item in target)
				{
					if (item == target.Last)
						target.Clear();
				}
			});
		}

		[TestMethod]
		public void CopyTo_Test1()
		{
			const int start = 1;
			const int count = 5;
			const int index = 1;
			const int count2 = 2;

			var query = Enumerable.Range(start, count).ToArray();
			var target = new Deque<int>(query);
			{
				var array = new int[count + 2];
				target.CopyTo(array, 1);

				Assert.AreEqual(0, array.First());
				Assert.IsTrue(query.SequenceEqual(array.Skip(1).Take(count)));
				Assert.AreEqual(0, array.Last());

				BiocsAssert.Throws<ArgumentNullException>(() => target.CopyTo(null, 0));
				BiocsAssert.Throws<ArgumentOutOfRangeException>(() => target.CopyTo(array, -1));
				BiocsAssert.Throws<ArgumentException>(() => target.CopyTo(array, array.Length - count + 1));
			}
			{
				var array2 = new int[count2 + 2];
				target.CopyTo(index, array2, 1, count2);

				Assert.AreEqual(0, array2.First());
				Assert.IsTrue(Enumerable.Range(start + index, count2).SequenceEqual(array2.Skip(1).Take(count2)));
				Assert.AreEqual(0, array2.Last());

				BiocsAssert.Throws<ArgumentNullException>(() => target.CopyTo(0, null, 0, 0));
				BiocsAssert.Throws<ArgumentOutOfRangeException>(() => target.CopyTo(-1, array2, 0, 0));
				BiocsAssert.Throws<ArgumentOutOfRangeException>(() => target.CopyTo(0, array2, -1, 0));
				BiocsAssert.Throws<ArgumentException>(() => target.CopyTo(count - 1, array2, 0, 2));
				BiocsAssert.Throws<ArgumentException>(() => target.CopyTo(0, array2, array2.Length - 1, 2));
				BiocsAssert.Throws<ArgumentException>(() => target.CopyTo(0, new int[count + 2], 0, count + 1));
			}
		}

		// In case that head and tail are linked
		[TestMethod]
		public void CopyTo_Test2()
		{
			const int start = 6;
			const int count = 5;
			const int index = count - 2;
			const int count2 = 2;

			var target = new Deque<int>(count);     // [6, 7, 8, 9, 10] (internal array: [10, 6, 7, 8, 9])

			for (int i = count - 1; i >= 0; i--)
				target.AddFirst(start + i);

			{
				var array = new[] { -1, -1 };
				target.CopyTo(0, array, 0, 0);

				Assert.IsTrue(array.All(x => x == -1));
			}
			{
				var array = new int[count + 2];     // [0, 6, 7, 8, 9, 10, 0]
				target.CopyTo(array, 1);

				Assert.AreEqual(0, array.First());
				Assert.IsTrue(Enumerable.Range(start, count).SequenceEqual(array.Skip(1).Take(count)));
				Assert.AreEqual(0, array.Last());
			}
			{
				var array2 = new int[count2 + 2];
				target.CopyTo(index, array2, 1, count2);    // [0, 9, 10, 0]

				Assert.AreEqual(0, array2.First());
				Assert.IsTrue(Enumerable.Range(start + index, count2).SequenceEqual(array2.Skip(1).Take(count2)));
				Assert.AreEqual(0, array2.Last());
			}
		}

		[TestMethod]
		public void IndexOf_Test()
		{
			var value = new object();
			var target = new Deque<object>(new[] { new object(), value, new object(), null });

			Assert.AreEqual(1, target.IndexOf(value));
			Assert.AreEqual(3, target.IndexOf(null));
			Assert.AreEqual(-1, target.IndexOf(new object()));

			var target2 = new Deque<int>(new[] { 1, 3, 4, 0, 9 });

			Assert.AreEqual(1, target2.IndexOf(3));
			Assert.AreEqual(3, target2.IndexOf(0));
			Assert.AreEqual(-1, target2.IndexOf(120));

			Assert.AreEqual(-1, new Deque<object>().IndexOf(null));
		}

		[TestMethod]
		public void Contains_Test()
		{
			var array = new double?[] { null, 9.2, 1.1, 3.7 };
			var target = new Deque<double?>(array);

			foreach (double? x in array)
				Assert.IsTrue(target.Contains(x));

			Assert.IsTrue(target.Contains(new double?()));
			Assert.IsFalse(target.Contains(1));

			Assert.IsFalse(new Deque<object>().Contains(null));
		}

		[TestMethod]
		public void AddFirst_Test()
		{
			var target = new Deque<string>();

			target.AddFirst(string.Empty);
			Assert.AreEqual(string.Empty, target.First);
			Assert.AreEqual(1, target.Count);

			for (int i = 1; i <= 10; i++)
			{
				string item = i.ToString(CultureInfo.InvariantCulture);
				target.AddFirst(item);
				Assert.AreEqual(item, target.First);
				Assert.AreEqual(i + 1, target.Count);
			}
		}

		[TestMethod]
		public void AddLast_Test()
		{
			var target = new Deque<string>();

			target.AddLast(string.Empty);
			Assert.AreEqual(string.Empty, target.Last);
			Assert.AreEqual(1, target.Count);

			for (int i = 0; i < 10; i++)
			{
				string item = i.ToString(CultureInfo.InvariantCulture);
				target.AddLast(item);
				Assert.AreEqual(item, target.Last);
				Assert.AreEqual(i + 2, target.Count);
			}
		}

		[TestMethod]
		public void Insert_Test1()
		{
			int count = 6;
			int capacity = 12;
			var compare = new List<int>(capacity);
			compare.AddRange(Enumerable.Range(1, count));

			// [1, 2, 3, 4, 5, 6, 0, 0, 0, 0, 0, 0]
			var target = new Deque<int>(capacity);

			foreach (int value in compare)
				target.AddLast(value);

			// Call MoveBlockStartward (index <= Count / 2)
			// [+, 2, 3, 4, 5, 6, 0, 0, 0, 0, 0, 1]
			count++;
			target.Insert(1, count);
			compare.Insert(1, count);
			Assert.IsTrue(target.SequenceEqual(compare));
			Assert.AreEqual(count, target.Count);

			// [7, 2, 3, 4, 5, 6, 0, 0, 0, 0, 0, 0]
			count--;
			target.RemoveFirst();
			compare.RemoveAt(0);
			Assert.IsTrue(target.SequenceEqual(compare));
			Assert.AreEqual(count, target.Count);

			// [2, +, 3, 4, 5, 6, 0, 0, 0, 0, 0, 7]
			count++;
			target.Insert(2, 1);
			compare.Insert(2, 1);
			Assert.IsTrue(target.SequenceEqual(compare));
			Assert.AreEqual(count, target.Count);

			// [1, +, 3, 4, 5, 6, 0, 0, 0, 0, 7, 2]
			count++;
			target.Insert(3, count);
			compare.Insert(3, count);
			Assert.IsTrue(target.SequenceEqual(compare));
			Assert.AreEqual(count, target.Count);

			// [+, 8, 3, 4, 5, 6, 0, 0, 0, 7, 2, 1]
			count++;
			target.Insert(3, count);
			compare.Insert(3, count);
			Assert.IsTrue(target.SequenceEqual(compare));
			Assert.AreEqual(count, target.Count);

			// [9, 8, 3, 4, 5, 6, 0, 0, 7, 2, +, 1]
			count++;
			target.Insert(2, count);
			compare.Insert(2, count);
			Assert.IsTrue(target.SequenceEqual(compare));
			Assert.AreEqual(count, target.Count);

			// Call AddFirst
			// [9, 8, 3, 4, 5, 6, 0, +, 7, 2, 10, 1]
			count++;
			target.Insert(0, count);
			compare.Insert(0, count);
			Assert.IsTrue(target.SequenceEqual(compare));
			Assert.AreEqual(count, target.Count);

			// Call AddLast
			// [9, 8, 3, 4, 5, 6, +, 11, 7, 2, 10, 1]
			count++;
			target.Insert(target.Count, count);
			compare.Insert(compare.Count, count);
			Assert.IsTrue(target.SequenceEqual(compare));
			Assert.AreEqual(count, target.Count);
			Assert.AreEqual(count, target.Capacity);

			// Call EnsureSpaceAndInsert
			count++;
			target.Insert(4, count);
			compare.Insert(4, count);
			Assert.IsTrue(target.SequenceEqual(compare));
			Assert.AreEqual(count, target.Count);

			BiocsAssert.Throws<ArgumentOutOfRangeException>(() => target.Insert(-1, 0));
		}

		[TestMethod]
		public void Insert_Test2()
		{
			int count = 8;
			int capacity = 12;
			var compare = new List<int>(capacity);
			compare.AddRange(Enumerable.Range(1, count));

			// [8, 0, 0, 0, 0, 1, 2, 3, 4, 5, 6, 7]
			var target = new Deque<int>(capacity);

			foreach (int value in Enumerable.Reverse(compare))
				target.AddFirst(value);

			// [0, 0, 0, 0, 0, 1, 2, 3, 4, 5, 6, 7]
			count--;
			target.RemoveLast();
			compare.RemoveAt(compare.Count - 1);
			Assert.IsTrue(target.SequenceEqual(compare));
			Assert.AreEqual(count, target.Count);

			// Call MoveBlockEndward (index > Count / 2)
			// [7, 0, 0, 0, 0, 1, 2, 3, 4, 5, +, 6]
			count++;
			target.Insert(5, count);
			compare.Insert(5, count);
			Assert.IsTrue(target.SequenceEqual(compare));
			Assert.AreEqual(count, target.Count);

			// [0, 0, 0, 0, 0, 1, 2, 3, 4, 5, 8, 6]
			count--;
			target.RemoveLast();
			compare.RemoveAt(compare.Count - 1);
			Assert.IsTrue(target.SequenceEqual(compare));
			Assert.AreEqual(count, target.Count);

			// [6, 0, 0, 0, 0, 1, 2, 3, 4, 5, 8, +]
			count++;
			target.Insert(6, 7);
			compare.Insert(6, 7);
			Assert.IsTrue(target.SequenceEqual(compare));
			Assert.AreEqual(count, target.Count);

			// [7, 6, 0, 0, 0, 1, 2, 3, 4, 5, +, 8]
			count++;
			target.Insert(5, count);
			compare.Insert(5, count);
			Assert.IsTrue(target.SequenceEqual(compare));
			Assert.AreEqual(count, target.Count);

			// [8, 7, 6, 0, 0, 1, 2, 3, 4, 5, 9, +]
			count++;
			target.Insert(6, count);
			compare.Insert(6, count);
			Assert.IsTrue(target.SequenceEqual(compare));
			Assert.AreEqual(count, target.Count);

			// [8, +, 7, 6, 0, 1, 2, 3, 4, 5, 9, 10]
			count++;
			target.Insert(8, count);
			compare.Insert(8, count);
			Assert.IsTrue(target.SequenceEqual(compare));
			Assert.AreEqual(count, target.Count);

			BiocsAssert.Throws<ArgumentOutOfRangeException>(() => target.Insert(count + 1, 0));
		}

		[TestMethod]
		public void RemoveFirst_Test()
		{
			var list = Enumerable.Range(0, 7).ToList();
			var target = new Deque<int>(list);

			target.AddFirst(-1);
			list.Insert(0, -1);
			target.AddFirst(-2);
			list.Insert(0, -2);

			for (int i = target.Count; i > 0; i--)
			{
				Assert.AreEqual(list[0], target.First);
				Assert.AreEqual(list[list.Count - 1], target.Last);
				Assert.AreEqual(list.Count, target.Count);

				target.RemoveFirst();
				list.RemoveAt(0);
			}
			Assert.AreEqual(0, target.Count);

			target.Clear();
			BiocsAssert.Throws<InvalidOperationException>(() => target.RemoveFirst());
		}

		[TestMethod]
		public void RemoveLast_Test()
		{
			var list = Enumerable.Range(0, 7).ToList();
			var target = new Deque<int>(list);

			target.AddFirst(-1);
			list.Insert(0, -1);
			target.AddFirst(-2);
			list.Insert(0, -2);

			for (int i = target.Count; i > 0; i--)
			{
				Assert.AreEqual(list[0], target.First);
				Assert.AreEqual(list[list.Count - 1], target.Last);
				Assert.AreEqual(list.Count, target.Count);

				target.RemoveLast();
				list.RemoveAt(list.Count - 1);
			}
			Assert.AreEqual(0, target.Count);

			target.Clear();
			BiocsAssert.Throws<InvalidOperationException>(() => target.RemoveLast());
		}

		[TestMethod]
		public void RemoveAt_Test()
		{
			var compare = Enumerable.Range(1, 10).ToList();
			var target = new Deque<int>(compare.Count);

			foreach (int value in Enumerable.Reverse(compare))
				target.AddFirst(value);

			// index = 1
			target.RemoveAt(1);
			compare.RemoveAt(1);
			Assert.IsTrue(target.SequenceEqual(compare));
			Assert.AreEqual(9, target.Count);

			// index = Count - 2
			target.RemoveAt(target.Count - 2);
			compare.RemoveAt(compare.Count - 2);
			Assert.IsTrue(target.SequenceEqual(compare));
			Assert.AreEqual(8, target.Count);

			// index = (Count - 1) / 2 + 1
			target.RemoveAt((target.Count - 1) / 2 + 1);
			compare.RemoveAt((compare.Count - 1) / 2 + 1);
			Assert.IsTrue(target.SequenceEqual(compare));
			Assert.AreEqual(7, target.Count);

			// index = (Count - 1) / 2
			target.RemoveAt((target.Count - 1) / 2);
			compare.RemoveAt((compare.Count - 1) / 2);
			Assert.IsTrue(target.SequenceEqual(compare));
			Assert.AreEqual(6, target.Count);

			// Call RemoveFirst (index = 0)
			target.RemoveAt(0);
			compare.RemoveAt(0);
			Assert.IsTrue(target.SequenceEqual(compare));
			Assert.AreEqual(5, target.Count);

			// Call RemoveLast (index = Count - 1)
			target.RemoveAt(target.Count - 1);
			compare.RemoveAt(compare.Count - 1);
			Assert.IsTrue(target.SequenceEqual(compare));
			Assert.AreEqual(4, target.Count);
		}

		[TestMethod]
		public void Remove_Test()
		{
			int count = 10;
			var target = new Deque<string>(count);

			for (int i = 1; i <= count / 2; i++)
				target.AddFirst(i.ToString(CultureInfo.InvariantCulture));

			for (int i = count / 2 + 1; i <= count; i++)
				target.AddLast(i.ToString(CultureInfo.InvariantCulture));

			foreach (int i in new[] { 2, 1, 6, 3, 4, 8, 9, 7, 10, 5 })
			{
				Assert.AreEqual(count--, target.Count);
				Assert.IsFalse(target.Remove(null));
				Assert.IsFalse(target.Remove(string.Empty));
				Assert.IsFalse(target.Remove("Biology"));

				string item = i.ToString(CultureInfo.InvariantCulture);
				Assert.IsTrue(target.Remove(item));
				Assert.IsFalse(target.Contains(item));
			}
			Assert.AreEqual(0, target.Count);
			Assert.IsFalse(target.Remove(null));
			Assert.IsFalse(target.Remove(string.Empty));
			Assert.IsFalse(target.Remove("Biology"));
		}

		[TestMethod]
		public void Clear_Test()
		{
			var target = new Deque<short>(Enumerable.Repeat<short>(89, 55).ToArray());

			target.RemoveLast();
			target.AddFirst(7);
			target.Insert(1, 2);

			target.Clear();
			Assert.AreEqual(0, target.Count);

			target.AddFirst(1);
			Assert.AreEqual(1, target.First);
			Assert.AreEqual(1, target.Last);
			Assert.AreEqual(1, target.Count);

			target.AddFirst(2);
			Assert.AreEqual(2, target.First);
			Assert.AreEqual(1, target.Last);
			Assert.AreEqual(2, target.Count);

			target.Clear();
			Assert.AreEqual(0, target.Count);
			BiocsAssert.Throws<InvalidOperationException>(() => target.First);
		}
	}
}
