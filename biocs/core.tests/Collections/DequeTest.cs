using System.Collections;
using System.Globalization;

namespace Biocs.Collections;

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
        Assert.IsFalse((target as ICollection<string>).IsReadOnly);

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new Deque<object>(-4));

        // ICollection<T>
        var target2 = new Deque<object>(Array.Empty<object>());
        Assert.AreEqual(0, target2.Count);
        Assert.AreEqual(0, target2.Capacity);

        var target3 = new Deque<object>(new object[count]);
        Assert.AreEqual(count, target3.Count);
        Assert.AreEqual(count, target3.Capacity);

        // IEnumerable<T>
        Assert.AreEqual(0, new Deque<int>(Enumerable.Empty<int>()).Count);
        Assert.AreEqual(count, new Deque<int>(Enumerable.Range(0, count)).Count);

        Assert.ThrowsException<ArgumentNullException>(() => new Deque<object>(null!));
    }

    [TestMethod]
    public void Item_Test()
    {
        const int count = 6;
        var target = new Deque<int>(Enumerable.Range(0, count));

        Assert.AreEqual(count, target.Count);

        for (int i = 0; i < target.Count; i++)
        {
            Assert.AreEqual(i, target[i]);

            target[i] += 100;
            Assert.AreEqual(i + 100, target[i]);
        }

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => target[-1]);
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => target[count]);
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => target[-1] = 0);
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => target[count] = 0);
    }

    [TestMethod]
    public void Capacity_Test()
    {
        var target = new Deque<int>();
        var compare = new List<int>();

        target.Capacity = 4;
        Assert.AreEqual(4, target.Capacity);
        Assert.AreEqual(0, target.Count);

        InsertRange(target, compare, 0, new[] { 1, 2 });
        Assert.AreEqual(4, target.Capacity);

        InsertRange(target, compare, 1, new[] { 3, 4 });
        Assert.AreEqual(4, target.Capacity);

        // No change
        target.Capacity = 4;
        Assert.AreEqual(4, target.Capacity);
        Assert.AreEqual(compare.Count, target.Count);
        Assert.IsTrue(target.SequenceEqual(compare));
        Assert.AreEqual(compare.First(), target.First);
        Assert.AreEqual(compare.Last(), target.Last);

        // Double capacity
        target.Capacity *= 2;
        Assert.AreEqual(8, target.Capacity);
        Assert.AreEqual(compare.Count, target.Count);
        Assert.IsTrue(target.SequenceEqual(compare));
        Assert.AreEqual(compare.First(), target.First);
        Assert.AreEqual(compare.Last(), target.Last);

        InsertRange(target, compare, 1, new[] { 5, 6 });
        Assert.AreEqual(8, target.Capacity);

        // TrimExcess
        target.Capacity = 6;
        Assert.AreEqual(6, target.Capacity);
        Assert.AreEqual(compare.Count, target.Count);
        Assert.IsTrue(target.SequenceEqual(compare));
        Assert.AreEqual(compare.First(), target.First);
        Assert.AreEqual(compare.Last(), target.Last);

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => target.Capacity = 5);
    }

    [TestMethod]
    public void First_Test()
    {
        const int value = 4;
        var target = new Deque<int>(Enumerable.Range(value, 2));

        Assert.AreEqual(value, target.First);

        target.First = 8;
        Assert.AreEqual(8, target.First);

        Assert.ThrowsException<InvalidOperationException>(() => new Deque<object>().First);
        Assert.ThrowsException<InvalidOperationException>(() => new Deque<object>().First = null!);
    }

    [TestMethod]
    public void Last_Test()
    {
        const int value = 4;
        var target = new Deque<int>(Enumerable.Range(value, 2).Reverse());

        Assert.AreEqual(value, target.Last);

        target.Last = 8;
        Assert.AreEqual(8, target.Last);

        Assert.ThrowsException<InvalidOperationException>(() => new Deque<object>().Last);
        Assert.ThrowsException<InvalidOperationException>(() => new Deque<object>().Last = null!);
    }

    [TestMethod]
    public void GetEnumerator_Test()
    {
        var query = Enumerable.Range(0, 4);
        {
            var target = new Deque<int>(query);
            Assert.IsTrue(query.SequenceEqual(target));

            var enumerator = (target as IEnumerable).GetEnumerator();

            for (int i = 0; i < 4; i++)
            {
                Assert.IsTrue(enumerator.MoveNext());
                Assert.AreEqual(i, enumerator.Current);
            }
            Assert.IsFalse(enumerator.MoveNext());
        }

        Assert.ThrowsException<InvalidOperationException>(() =>
        {
            var target = new Deque<int>(query);

            foreach (int item in target)
                target.Capacity *= 2;
        });

        Assert.ThrowsException<InvalidOperationException>(() =>
        {
            var target = new Deque<int>(query);

            foreach (int item in target)
                target.AddFirst(item);
        });

        Assert.ThrowsException<InvalidOperationException>(() =>
        {
            var target = new Deque<int>(query);

            foreach (int item in target)
                target.AddLast(item);
        });

        Assert.ThrowsException<InvalidOperationException>(() =>
        {
            var target = new Deque<int>(query);

            foreach (int item in target)
                target.Insert(0, item);
        });

        Assert.ThrowsException<InvalidOperationException>(() =>
        {
            var target = new Deque<int>(query);

            foreach (int item in target)
                target.InsertRange(0, new[] { item });
        });

        Assert.ThrowsException<InvalidOperationException>(() =>
        {
            var target = new Deque<int>(query);

            foreach (int item in target)
                target.RemoveFirst();
        });

        Assert.ThrowsException<InvalidOperationException>(() =>
        {
            var target = new Deque<int>(query);

            foreach (int item in target)
                target.RemoveLast();
        });

        Assert.ThrowsException<InvalidOperationException>(() =>
        {
            var target = new Deque<int>(query);

            foreach (int item in target)
                target.Remove(item);
        });

        Assert.ThrowsException<InvalidOperationException>(() =>
        {
            var target = new Deque<int>(query);

            foreach (int item in target)
                target.RemoveAt(0);
        });

        Assert.ThrowsException<InvalidOperationException>(() =>
        {
            var target = new Deque<int>(query);

            foreach (int item in target)
                target.RemoveRange(1, 2);
        });

        Assert.ThrowsException<InvalidOperationException>(() =>
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

            Assert.ThrowsException<ArgumentNullException>(() => target.CopyTo(null!, 0));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => target.CopyTo(array, -1));
            Assert.ThrowsException<ArgumentException>(() => target.CopyTo(array, array.Length - count + 1));
        }
        {
            var array2 = new int[count2 + 2];
            target.CopyTo(index, array2, 1, count2);

            Assert.AreEqual(0, array2.First());
            Assert.IsTrue(Enumerable.Range(start + index, count2).SequenceEqual(array2.Skip(1).Take(count2)));
            Assert.AreEqual(0, array2.Last());

            Assert.ThrowsException<ArgumentNullException>(() => target.CopyTo(0, null!, 0, 0));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => target.CopyTo(-1, array2, 0, 0));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => target.CopyTo(0, array2, -1, 0));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => target.CopyTo(0, array2, 0, -1));
            Assert.ThrowsException<ArgumentException>(() => target.CopyTo(count - 1, array2, 0, 2));
            Assert.ThrowsException<ArgumentException>(() => target.CopyTo(0, array2, array2.Length - 1, 2));
            Assert.ThrowsException<ArgumentException>(() => target.CopyTo(0, new int[count + 2], 0, count + 1));
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
        object value = new();
        var target = new Deque<object?>(new[] { new object(), value, new object(), null });

        Assert.AreEqual(1, target.IndexOf(value));
        Assert.AreEqual(3, target.IndexOf(null));
        Assert.AreEqual(-1, target.IndexOf(new object()));

        var target2 = new Deque<int>(new[] { 1, 3, 4, 0, 9 });

        Assert.AreEqual(1, target2.IndexOf(3));
        Assert.AreEqual(3, target2.IndexOf(0));
        Assert.AreEqual(-1, target2.IndexOf(120));

        Assert.AreEqual(-1, new Deque<object>().IndexOf(null!));
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

        Assert.IsFalse(new Deque<object>().Contains(null!));
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
        var target = new Deque<string?>();

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

        (target as ICollection<string?>).Add(null);
        Assert.AreEqual(null, target.Last);
        Assert.AreEqual(12, target.Count);
    }

    [TestMethod]
    public void Insert_Test1()
    {
        int count = 6;
        int capacity = 12;
        var target = new Deque<int>(capacity);
        var compare = new List<int>(capacity);
        compare.AddRange(Enumerable.Range(1, count));

        // [1, 2, 3, 4, 5, 6, 0, 0, 0, 0, 0, 0]
        foreach (int value in compare)
            target.AddLast(value);

        // Call MoveBlockStartward (index <= Count / 2)
        // [+, 2, 3, 4, 5, 6, 0, 0, 0, 0, 0, 1]
        Insert(target, compare, 1, ++count);

        // [7, 2, 3, 4, 5, 6, 0, 0, 0, 0, 0, 0]
        count--;
        RemoveAt(target, compare, 0);

        // [2, +, 3, 4, 5, 6, 0, 0, 0, 0, 0, 7]
        count++;
        Insert(target, compare, 2, 1);

        // [1, +, 3, 4, 5, 6, 0, 0, 0, 0, 7, 2]
        Insert(target, compare, 3, ++count);

        // [+, 8, 3, 4, 5, 6, 0, 0, 0, 7, 2, 1]
        Insert(target, compare, 3, ++count);

        // [9, 8, 3, 4, 5, 6, 0, 0, 7, 2, +, 1]
        Insert(target, compare, 2, ++count);

        // Call AddFirst
        // [9, 8, 3, 4, 5, 6, 0, +, 7, 2, 10, 1]
        Insert(target, compare, 0, ++count);

        // Call AddLast
        // [9, 8, 3, 4, 5, 6, +, 11, 7, 2, 10, 1]
        Insert(target, compare, target.Count, ++count);
        Assert.AreEqual(target.Count, target.Capacity);

        // Call EnsureSpaceAndInsert
        Insert(target, compare, 4, ++count);

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => target.Insert(-1, 0));
    }

    [TestMethod]
    public void Insert_Test2()
    {
        int count = 8;
        int capacity = 12;
        var target = new Deque<int>(capacity);
        var compare = new List<int>(capacity);
        compare.AddRange(Enumerable.Range(1, count));

        // [8, 0, 0, 0, 0, 1, 2, 3, 4, 5, 6, 7]
        foreach (int value in Enumerable.Reverse(compare))
            target.AddFirst(value);

        // [0, 0, 0, 0, 0, 1, 2, 3, 4, 5, 6, 7]
        count--;
        RemoveAt(target, compare, target.Count - 1);

        // Call MoveBlockEndward (index > Count / 2)
        // [7, 0, 0, 0, 0, 1, 2, 3, 4, 5, +, 6]
        Insert(target, compare, 5, ++count);

        // [0, 0, 0, 0, 0, 1, 2, 3, 4, 5, 8, 6]
        count--;
        RemoveAt(target, compare, target.Count - 1);

        // [6, 0, 0, 0, 0, 1, 2, 3, 4, 5, 8, +]
        count++;
        Insert(target, compare, 6, 7);

        // [7, 6, 0, 0, 0, 1, 2, 3, 4, 5, +, 8]
        Insert(target, compare, 5, ++count);

        // [8, 7, 6, 0, 0, 1, 2, 3, 4, 5, 9, +]
        Insert(target, compare, 6, ++count);

        // [8, +, 7, 6, 0, 1, 2, 3, 4, 5, 9, 10]
        Insert(target, compare, 8, ++count);

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => target.Insert(count + 1, 0));
    }

    [TestMethod]
    public void InsertRange_Test()
    {
        int capacity = 18;

        // [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0]
        var target = new Deque<int>(capacity);
        var compare = new List<int>(capacity);

        // [+, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0]
        InsertRange(target, compare, 0, new[] { 1 });

        // [1, +, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0]
        InsertRange(target, compare, 1, new[] { 2 });

        // [1, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, +]
        InsertRange(target, compare, 0, new[] { 3 });

        // [1, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3, +]
        InsertRange(target, compare, 1, new[] { 4 });

        // [1, +, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3, 4]
        InsertRange(target, compare, 3, new[] { 5 });

        // [1, +, +, 5, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3, 4]
        InsertRange(target, compare, 3, Enumerable.Range(6, 2));

        // [+, 6, 7, 5, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3, 4, 1, +]
        InsertRange(target, compare, 3, new[] { 8, 9 });

        // [+, +, +, +, +, 8, 9, 6, 7, 5, 2, 3, 4, 1, +, +, +, +]
        target.InsertRange(3, target);
        compare.InsertRange(3, compare);
        Assert.AreEqual(compare.Count, target.Count);
        Assert.IsTrue(target.SequenceEqual(compare));
        Assert.AreEqual(target.Count, target.Capacity);
        Assert.AreEqual(compare.First(), target.First);
        Assert.AreEqual(compare.Last(), target.Last);

        // Call EnsureCapacityAndInsertRange
        InsertRange(target, compare, 2, new[] { 10, 11 });
        InsertRange(target, compare, 0, new[] { 12, 13 });
        InsertRange(target, compare, target.Count, new[] { 14, 15, 16 });

        target.InsertRange(12, target);
        compare.InsertRange(12, compare);
        Assert.AreEqual(compare.Count, target.Count);
        Assert.IsTrue(target.SequenceEqual(compare));
        Assert.AreEqual(compare.First(), target.First);
        Assert.AreEqual(compare.Last(), target.Last);

        InsertRange(target, compare, 0, Array.Empty<int>());

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => target.InsertRange(-1, Array.Empty<int>()));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => target.InsertRange(target.Count + 1, Array.Empty<int>()));
        Assert.ThrowsException<ArgumentNullException>(() => target.InsertRange(0, null!));
    }

    [TestMethod]
    public void RemoveFirst_Test()
    {
        var compare = Enumerable.Range(0, 7).ToList();
        var target = new Deque<int>(compare);

        target.AddFirst(-1);
        compare.Insert(0, -1);
        target.AddFirst(-2);
        compare.Insert(0, -2);

        for (int i = target.Count; i > 0; i--)
        {
            Assert.AreEqual(compare.First(), target.First);
            Assert.AreEqual(compare.Last(), target.Last);
            Assert.AreEqual(compare.Count, target.Count);

            target.RemoveFirst();
            compare.RemoveAt(0);
        }
        Assert.AreEqual(0, target.Count);

        target.Clear();
        Assert.ThrowsException<InvalidOperationException>(target.RemoveFirst);
    }

    [TestMethod]
    public void RemoveLast_Test()
    {
        var compare = Enumerable.Range(0, 7).ToList();
        var target = new Deque<int>(compare);

        target.AddFirst(-1);
        compare.Insert(0, -1);
        target.AddFirst(-2);
        compare.Insert(0, -2);

        for (int i = target.Count; i > 0; i--)
        {
            Assert.AreEqual(compare.First(), target.First);
            Assert.AreEqual(compare.Last(), target.Last);
            Assert.AreEqual(compare.Count, target.Count);

            target.RemoveLast();
            compare.RemoveAt(compare.Count - 1);
        }
        Assert.AreEqual(0, target.Count);

        target.Clear();
        Assert.ThrowsException<InvalidOperationException>(target.RemoveLast);
    }

    [TestMethod]
    public void RemoveAt_Test()
    {
        var compare = Enumerable.Range(1, 10).ToList();
        var target = new Deque<int>(compare.Count);

        foreach (int value in Enumerable.Reverse(compare))
            target.AddFirst(value);

        RemoveAt(target, compare, 1);
        RemoveAt(target, compare, target.Count - 2);
        RemoveAt(target, compare, (target.Count - 1) / 2 + 1);
        RemoveAt(target, compare, (target.Count - 1) / 2);

        // Call RemoveFirst
        RemoveAt(target, compare, 0);

        // Call RemoveLast
        RemoveAt(target, compare, target.Count - 1);

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => target.RemoveAt(-1));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => target.RemoveAt(target.Count));
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
            Assert.IsFalse(target.Remove(null!));
            Assert.IsFalse(target.Remove(string.Empty));
            Assert.IsFalse(target.Remove("Biology"));

            string item = i.ToString(CultureInfo.InvariantCulture);
            Assert.IsTrue(target.Remove(item));
            Assert.IsFalse(target.Contains(item));
        }
        Assert.AreEqual(0, target.Count);
        Assert.IsFalse(target.Remove(null!));
        Assert.IsFalse(target.Remove(string.Empty));
        Assert.IsFalse(target.Remove("Biology"));
    }

    [TestMethod]
    public void RemoveRange_Test()
    {
        int count = 20;
        var compare = new List<int>(count);
        compare.AddRange(Enumerable.Range(1, count));

        var target = new Deque<int>(compare);

        RemoveRange(target, compare, 0, 0);
        RemoveRange(target, compare, 1, 4);
        RemoveRange(target, compare, 13, 2);
        RemoveRange(target, compare, 0, 3);
        RemoveRange(target, compare, 9, 2);
        RemoveRange(target, compare, 0, 9);
        Assert.AreEqual(0, target.Count);

        InsertRange(target, compare, 0, Enumerable.Range(1, 5));
        InsertRange(target, compare, 0, Enumerable.Range(6, 5));

        RemoveRange(target, compare, 2, 7);

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => target.RemoveRange(-1, 0));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => target.RemoveRange(0, -1));
        Assert.ThrowsException<ArgumentException>(() => new Deque<int>(Enumerable.Range(0, 10)).RemoveRange(8, 10));
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
        Assert.ThrowsException<InvalidOperationException>(() => target.First);
    }

    private static void Insert(Deque<int> target, List<int> compare, int index, int value)
    {
        target.Insert(index, value);
        compare.Insert(index, value);
        Assert.AreEqual(compare.Count, target.Count);
        Assert.IsTrue(target.SequenceEqual(compare));

        Assert.AreEqual(value, target[index]);
        Assert.AreEqual(compare.First(), target.First);
        Assert.AreEqual(compare.Last(), target.Last);
    }

    private static void InsertRange(Deque<int> target, List<int> compare, int index, IEnumerable<int> coll)
    {
        target.InsertRange(index, coll);
        compare.InsertRange(index, coll);
        Assert.AreEqual(compare.Count, target.Count);
        Assert.IsTrue(target.SequenceEqual(compare));

        Assert.AreEqual(compare.First(), target.First);
        Assert.AreEqual(compare.Last(), target.Last);
    }

    private static void RemoveAt(Deque<int> target, List<int> compare, int index)
    {
        target.RemoveAt(index);
        compare.RemoveAt(index);
        Assert.AreEqual(compare.Count, target.Count);
        Assert.IsTrue(target.SequenceEqual(compare));

        if (target.Count > 0)
        {
            Assert.AreEqual(compare.First(), target.First);
            Assert.AreEqual(compare.Last(), target.Last);
        }
    }

    private static void RemoveRange(Deque<int> target, List<int> compare, int index, int count)
    {
        target.RemoveRange(index, count);
        compare.RemoveRange(index, count);
        Assert.AreEqual(compare.Count, target.Count);
        Assert.IsTrue(target.SequenceEqual(compare));

        if (target.Count > 0)
        {
            Assert.AreEqual(compare.First(), target.First);
            Assert.AreEqual(compare.Last(), target.Last);
        }
    }
}
