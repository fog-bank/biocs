using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Biocs.TestTools;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Biocs.Collections
{
    [TestClass]
    public class CounterTest
    {
		[TestMethod]
		public void Constructor_Test1()
		{
			var c1 = new Counter<object>();
			Assert.AreEqual(0, c1.TotalCount);
			Assert.AreEqual(0, c1.NumberOfItems);
			Assert.IsFalse(c1.UniqueItems.Any());

			var c2 = new Counter<int>(10);
			Assert.AreEqual(0, c2.TotalCount);
			Assert.AreEqual(0, c2.NumberOfItems);
			Assert.IsFalse(c2.UniqueItems.Any());

			var c3 = new Counter<string>(StringComparer.InvariantCulture);
			Assert.AreEqual(0, c3.TotalCount);
			Assert.AreEqual(0, c3.NumberOfItems);
			Assert.IsFalse(c3.UniqueItems.Any());
			Assert.AreEqual(StringComparer.InvariantCulture, c3.Comparer);

			var c4 = new Counter<string>(10, StringComparer.InvariantCultureIgnoreCase);
			Assert.AreEqual(0, c4.TotalCount);
			Assert.AreEqual(0, c4.NumberOfItems);
			Assert.IsFalse(c4.UniqueItems.Any());
			Assert.AreEqual(StringComparer.InvariantCultureIgnoreCase, c4.Comparer);

			BiocsAssert.Throws<ArgumentOutOfRangeException>(() => new Counter<object>(-1));
			BiocsAssert.Throws<ArgumentOutOfRangeException>(() => new Counter<object>(-1, null));

			BiocsAssert.Throws<ArgumentNullException>(() => new Counter<object>().GetCount(null));
		}

		[TestMethod]
		public void Constructor_Test2()
		{
			var counter = new Counter<string>();

			counter.Add("A", 1);
			counter.Add("B", 2);
			counter.Add("C", 3);

			var clone = new Counter<string>(counter);

			Assert.AreEqual(counter.TotalCount, clone.TotalCount);
			Assert.AreEqual(counter.NumberOfItems, clone.NumberOfItems);
			Assert.AreEqual(counter.Comparer, clone.Comparer);

			counter.Reset("C");

			Assert.AreEqual(1, clone.GetCount("A"));
			Assert.AreEqual(2, clone.GetCount("B"));
			Assert.AreEqual(3, clone.GetCount("C"));

			BiocsAssert.Throws<ArgumentNullException>(() => new Counter<object>((Counter<object>)null));
		}

		[TestMethod]
		public void Add_Test1()
		{
			var counter = new Counter<int>();

			counter.Add(1);
			Assert.AreEqual(1, counter.TotalCount);
			Assert.AreEqual(1, counter.NumberOfItems);
			Assert.IsTrue(new HashSet<int> { 1 }.SetEquals(counter.UniqueItems));
			Assert.AreEqual(1, counter.GetCount(1));
			Assert.AreEqual(0, counter.GetCount(2));
			Assert.AreEqual(0, counter.GetCount(3));
			Assert.IsTrue(counter.Contains(1));
			Assert.IsFalse(counter.Contains(2));
			Assert.IsFalse(counter.Contains(3));

			counter.Add(1, 3);
			Assert.AreEqual(4, counter.TotalCount);
			Assert.AreEqual(1, counter.NumberOfItems);
			Assert.IsTrue(new HashSet<int> { 1 }.SetEquals(counter.UniqueItems));
			Assert.AreEqual(4, counter.GetCount(1));
			Assert.AreEqual(0, counter.GetCount(2));
			Assert.AreEqual(0, counter.GetCount(3));
			Assert.IsTrue(counter.Contains(1));
			Assert.IsFalse(counter.Contains(2));
			Assert.IsFalse(counter.Contains(3));

			counter.Add(2, 2);
			Assert.AreEqual(6, counter.TotalCount);
			Assert.AreEqual(2, counter.NumberOfItems);
			Assert.IsTrue(new HashSet<int> { 1, 2 }.SetEquals(counter.UniqueItems));
			Assert.AreEqual(4, counter.GetCount(1));
			Assert.AreEqual(2, counter.GetCount(2));
			Assert.AreEqual(0, counter.GetCount(3));
			Assert.IsTrue(counter.Contains(1));
			Assert.IsTrue(counter.Contains(2));
			Assert.IsFalse(counter.Contains(3));

			counter.Add(3, 0);
			Assert.AreEqual(6, counter.TotalCount);
			Assert.AreEqual(3, counter.NumberOfItems);
			Assert.IsTrue(new HashSet<int> { 1, 2, 3 }.SetEquals(counter.UniqueItems));
			Assert.AreEqual(4, counter.GetCount(1));
			Assert.AreEqual(2, counter.GetCount(2));
			Assert.AreEqual(0, counter.GetCount(3));
			Assert.IsTrue(counter.Contains(1));
			Assert.IsTrue(counter.Contains(2));
			Assert.IsTrue(counter.Contains(3));

			BiocsAssert.Throws<ArgumentNullException>(() => new Counter<object>().Add((object)null));
			BiocsAssert.Throws<ArgumentNullException>(() => new Counter<object>().Add(null, 0));
			BiocsAssert.Throws<ArgumentOutOfRangeException>(() => new Counter<int>().Add(0, -1));
		}

		[TestMethod]
		public void Add_Test2()
		{
			string[] items = { "a", "b", "c", "d" };

			var input = new List<string>();

			for (int i = 0; i < items.Length; i++)
			{
				for (int n = 0; n <= i; n++)
					input.Add(n == 0 ? items[i] : items[i].ToUpper(CultureInfo.InvariantCulture));
			}

			var counter = new Counter<string>(StringComparer.OrdinalIgnoreCase);
			counter.AddRange(input);

			Assert.AreEqual(input.Count, counter.TotalCount);
			Assert.AreEqual(items.Length, counter.NumberOfItems);
			Assert.IsTrue(new HashSet<string>(items, StringComparer.OrdinalIgnoreCase).SetEquals(counter.UniqueItems));

			for (int i = 0; i < items.Length; i++)
			{
				Assert.AreEqual(i + 1, counter.GetCount(items[i]));
				Assert.IsTrue(counter.Contains(items[i]));
			}

			BiocsAssert.Throws<ArgumentNullException>(() => new Counter<object>().AddRange((object[])null));
			BiocsAssert.Throws<ArgumentException>(() => new Counter<object>().AddRange(new object[] { null }));
		}

		[TestMethod]
		public void CopyTo_Test()
		{
			const int count = 5;
			const int startIndex = 2;

			var query = Enumerable.Range(1, count);
			var counter = new Counter<int>();

			foreach (int num in query)
				counter.Add(num);

			Assert.AreEqual(count, counter.TotalCount);
			Assert.AreEqual(count, counter.NumberOfItems);

			var array = new int[10];
			counter.CopyTo(array, startIndex);

			for (int i = 0; i < startIndex; i++)
				Assert.AreEqual(0, array[i]);

			Assert.IsTrue(counter.UniqueItems.SequenceEqual(array.Skip(startIndex).Take(count)));

			for (int i = startIndex + count; i < array.Length; i++)
				Assert.AreEqual(0, array[i]);

			BiocsAssert.Throws<ArgumentNullException>(() => counter.CopyTo(null, 0));
			BiocsAssert.Throws<ArgumentOutOfRangeException>(() => counter.CopyTo(new int[count], -1));
		}

		[TestMethod]
		public void Reset_Test()
		{
			var query = Enumerable.Range(1, 10);
			var counter = new Counter<int>();

			foreach (int num in query)
				counter.Add(num, num);

			Assert.AreEqual(55, counter.TotalCount);
			Assert.AreEqual(10, counter.NumberOfItems);

			counter.Reset(1);

			Assert.AreEqual(54, counter.TotalCount);
			Assert.AreEqual(10, counter.NumberOfItems);
			Assert.IsTrue(counter.Contains(1));

			counter.Reset(10);

			Assert.AreEqual(44, counter.TotalCount);
			Assert.AreEqual(10, counter.NumberOfItems);
			Assert.IsTrue(counter.Contains(10));

			counter.Reset();

			Assert.AreEqual(0, counter.TotalCount);
			Assert.AreEqual(10, counter.NumberOfItems);

			foreach (int num in query)
				Assert.IsTrue(counter.Contains(num));

			BiocsAssert.Throws<ArgumentNullException>(() => new Counter<object>().Reset(null));
		}

		[TestMethod]
		public void Clear_Test()
		{
			var counter = new Counter<int>();
			counter.AddRange(Enumerable.Range(0, 10));

			counter.Clear();

			Assert.AreEqual(0, counter.TotalCount);
			Assert.AreEqual(0, counter.NumberOfItems);
		}
    }
}
