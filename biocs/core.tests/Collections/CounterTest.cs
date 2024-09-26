﻿namespace Biocs.Collections;

[TestClass]
public class CounterTest
{
    [TestMethod]
    public void Constructor_Test()
    {
        var c1 = new Counter<object>();
        TestProperties(c1, 0, 0, [], [], EqualityComparer<object>.Default);

        var c2 = new Counter<int>(10);
        TestProperties(c2, 0, 0, [], [], EqualityComparer<int>.Default);

        var comparer3 = StringComparer.Ordinal;
        var c3 = new Counter<string>(comparer3);
        TestProperties(c3, 0, 0, new HashSet<string>(comparer3), [], comparer3);

        var comparer4 = StringComparer.OrdinalIgnoreCase;
        var c4 = new Counter<string>(10, comparer4);
        TestProperties(c4, 0, 0, new HashSet<string>(comparer4), [], comparer4);

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new Counter<object>(-1));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new Counter<object>(-1, null));
    }

    [TestMethod]
    public void CopyConstructor_ValTest()
    {
        var set = new HashSet<int> { 1 };
        var counter = new Counter<int>();
        counter.Add(1, 2);

        var clone = new Counter<int>(counter);
        TestProperties(clone, 1, 2, set, [1, 1], EqualityComparer<int>.Default);
        TestItem(clone, 1, 2);

        counter.Add(0, 1);
        set.Add(0);

        clone = new Counter<int>(counter);
        TestProperties(clone, 2, 3, set, [0, 1, 1], EqualityComparer<int>.Default);
        TestItem(clone, 1, 2);
        TestItem(clone, 0, 1);

        Assert.ThrowsException<ArgumentNullException>(() => new Counter<object>((Counter<object>)null!));
    }

    [TestMethod]
    public void CopyConstructor_RefTest()
    {
        var comparer = StringComparer.OrdinalIgnoreCase;
        var set = new HashSet<string?>(comparer) { "A", "B", "C" };
        var counter = new Counter<string?>(comparer);
        counter.Add("A", 1);
        counter.Add("B", 2);
        counter.Add("C", 3);

        var clone = new Counter<string?>(counter);
        TestProperties(clone, 3, 6, set, ["A", "B", "B", "C", "C", "C"], comparer);
        TestItem(clone, "A", 1);
        TestItem(clone, "B", 2);
        TestItem(clone, "C", 3);

        counter.Add(null, 1);
        set.Add(null);

        clone = new Counter<string?>(counter);
        TestProperties(clone, 4, 7, set, [null, "A", "B", "B", "C", "C", "C"], comparer);
        TestItem(clone, "A", 1);
        TestItem(clone, "B", 2);
        TestItem(clone, "C", 3);
        TestItem(clone, null, 1);
    }

    [TestMethod]
    public void CopyTo_ValTest()
    {
        const int Count = 5;
        const int StartIndex = 2;
        const int DefaultValue = -1;

        var query = Enumerable.Range(0, Count);
        var counter = new Counter<int>();

        foreach (int num in query)
            counter.Add(num);

        TestProperties(counter, Count, Count, new HashSet<int>(query), query.ToArray(), EqualityComparer<int>.Default);

        var array = new int[10];

        for (int i = 0; i < array.Length; i++)
            array[i] = DefaultValue;

        counter.CopyTo(array, StartIndex);

        for (int i = 0; i < StartIndex; i++)
            Assert.AreEqual(DefaultValue, array[i]);

        Assert.IsTrue(counter.UniqueItems.SequenceEqual(array.Skip(StartIndex).Take(Count)));

        for (int i = StartIndex + Count; i < array.Length; i++)
            Assert.AreEqual(DefaultValue, array[i]);

        TestProperties(counter, Count, Count, new HashSet<int>(query), query.ToArray(), EqualityComparer<int>.Default);

        Assert.ThrowsException<ArgumentNullException>(() => counter.CopyTo(null!, 0));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => counter.CopyTo(new int[Count], -1));
        Assert.ThrowsException<ArgumentException>(() => counter.CopyTo(new int[Count], 3));
    }

    [TestMethod]
    public void Add_ValTest()
    {
        var comparer = EqualityComparer<int>.Default;
        var set = new HashSet<int>(comparer);
        var counter = new Counter<int>();

        counter.Add(1);
        set.Add(1);
        TestProperties(counter, 1, 1, set, [1], comparer);
        TestItem(counter, 0, null);
        TestItem(counter, 1, 1);
        TestItem(counter, 2, null);
        TestItem(counter, 3, null);

        counter.Add(1, 3);
        TestProperties(counter, 1, 4, set, [1, 1, 1, 1], comparer);
        TestItem(counter, 0, null);
        TestItem(counter, 1, 4);
        TestItem(counter, 2, null);
        TestItem(counter, 3, null);

        counter.Add(2, 2);
        set.Add(2);
        TestProperties(counter, 2, 6, set, [1, 1, 1, 1, 2, 2], comparer);
        TestItem(counter, 0, null);
        TestItem(counter, 1, 4);
        TestItem(counter, 2, 2);
        TestItem(counter, 3, null);

        counter.Add(3, 0);
        set.Add(3);
        TestProperties(counter, 3, 6, set, [1, 1, 1, 1, 2, 2], comparer);
        TestItem(counter, 0, null);
        TestItem(counter, 1, 4);
        TestItem(counter, 2, 2);
        TestItem(counter, 3, 0);

        counter.Add(0);
        set.Add(0);
        TestProperties(counter, 4, 7, set, [0, 1, 1, 1, 1, 2, 2], comparer);
        TestItem(counter, 0, 1);
        TestItem(counter, 1, 4);
        TestItem(counter, 2, 2);
        TestItem(counter, 3, 0);

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new Counter<int>().Add(0, -1));
    }

    [TestMethod]
    public void Add_RefTest()
    {
        var comparer = StringComparer.OrdinalIgnoreCase;
        var set = new HashSet<string?>(comparer);
        var counter = new Counter<string?>(comparer);

        counter.Add("A");
        set.Add("A");
        TestProperties(counter, 1, 1, set, ["A"], comparer);
        TestItem(counter, null, null);
        TestItem(counter, "A", 1);
        TestItem(counter, "B", null);
        TestItem(counter, "C", null);

        counter.Add("a", 3);
        TestProperties(counter, 1, 4, set, ["A", "A", "A", "A"], comparer);
        TestItem(counter, null, null);
        TestItem(counter, "A", 4);
        TestItem(counter, "B", null);
        TestItem(counter, "C", null);

        counter.Add("B", 2);
        set.Add("B");
        TestProperties(counter, 2, 6, set, ["A", "A", "A", "A", "B", "B"], comparer);
        TestItem(counter, null, null);
        TestItem(counter, "A", 4);
        TestItem(counter, "B", 2);
        TestItem(counter, "C", null);

        counter.Add("C", 0);
        set.Add("C");
        TestProperties(counter, 3, 6, set, ["A", "A", "A", "A", "B", "B"], comparer);
        TestItem(counter, null, null);
        TestItem(counter, "A", 4);
        TestItem(counter, "B", 2);
        TestItem(counter, "C", 0);

        counter.Add(null);
        set.Add(null);
        TestProperties(counter, 4, 7, set, [null, "A", "A", "A", "A", "B", "B"], comparer);
        TestItem(counter, null, 1);
        TestItem(counter, "A", 4);
        TestItem(counter, "B", 2);
        TestItem(counter, "C", 0);

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new Counter<string>().Add(string.Empty, -1));
    }

    [TestMethod]
    public void AddRange_RefTest()
    {
        var items = new[] { "a", "b", "c", "d", null };
        var input = new List<string?>();

        for (int i = 0; i < items.Length; i++)
        {
            for (int n = 0; n <= i; n++)
                input.Add(n == 0 ? items[i] : items[i]?.ToUpperInvariant());
        }

        var comparer = StringComparer.OrdinalIgnoreCase;
        var counter = new Counter<string?>(comparer);

        counter.AddRange(input);
        TestProperties(counter, items.Length, input.Count,
            new HashSet<string?>(items, comparer), input.Select(x => x?.ToLowerInvariant()).ToArray(), comparer);

        for (int i = 0; i < items.Length; i++)
            TestItem(counter, items[i], i + 1);

        Assert.ThrowsException<ArgumentNullException>(() => new Counter<object>().AddRange(null!));
    }

    [TestMethod]
    public void Remove_ValTest()
    {
        var comparer = EqualityComparer<int>.Default;
        var items = new List<int>();
        var set = new HashSet<int>();
        var counter = new Counter<int>();

        foreach (var tup in new[] { 0, 1, 2, 3 }.Zip(new[] { 1, 0, 3, 4 }, Tuple.Create))
        {
            counter.Add(tup.Item1, tup.Item2);
            set.Add(tup.Item1);
            items.AddRange(Enumerable.Repeat(tup.Item1, tup.Item2));
        }
        TestProperties(counter, 4, 8, set, [.. items], comparer);

        Assert.IsFalse(counter.Remove(1));
        TestProperties(counter, 4, 8, set, [.. items], comparer);
        TestItem(counter, 1, 0);

        Assert.IsTrue(counter.Remove(2));
        items.Remove(2);
        TestProperties(counter, 4, 7, set, [.. items], comparer);
        TestItem(counter, 2, 2);

        Assert.AreEqual(0, counter.Remove(2, 0));
        TestProperties(counter, 4, 7, set, [.. items], comparer);
        TestItem(counter, 2, 2);

        Assert.AreEqual(3, counter.Remove(3, 3));
        items.Remove(3);
        items.Remove(3);
        items.Remove(3);
        TestProperties(counter, 4, 4, set, [.. items], comparer);
        TestItem(counter, 3, 1);

        Assert.AreEqual(1, counter.Remove(0, 2));
        items.Remove(0);
        TestProperties(counter, 4, 3, set, [.. items], comparer);
        TestItem(counter, 0, 0);

        Assert.IsFalse(counter.Remove(4));
        TestProperties(counter, 4, 3, set, [.. items], comparer);
        TestItem(counter, 4, null);

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => counter.Remove(0, -1));
    }

    [TestMethod]
    public void Remove_RefTest()
    {
        var comparer = StringComparer.OrdinalIgnoreCase;
        var items = new List<string?>();
        var set = new HashSet<string?>(comparer);
        var counter = new Counter<string?>(comparer);

        foreach (var tup in new[] { "A", "a", "B", null }.Zip(new[] { 3, 2, 0, 2 }, ValueTuple.Create))
        {
            counter.Add(tup.Item1, tup.Item2);
            set.Add(tup.Item1);
            items.AddRange(Enumerable.Repeat(tup.Item1?.ToUpper(), tup.Item2));
        }
        TestProperties(counter, 3, 7, set, [.. items], comparer);

        Assert.IsFalse(counter.Remove("B"));
        TestProperties(counter, 3, 7, set, [.. items], comparer);
        TestItem(counter, "B", 0);

        Assert.IsTrue(counter.Remove("a"));
        items.Remove("A");
        TestProperties(counter, 3, 6, set, [.. items], comparer);
        TestItem(counter, "A", 4);

        Assert.AreEqual(4, counter.Remove("A", 5));
        items.RemoveAll(x => x == "A");
        TestProperties(counter, 3, 2, set, [.. items], comparer);
        TestItem(counter, "A", 0);

        Assert.AreEqual(1, counter.Remove(null, 1));
        items.Remove(null);
        TestProperties(counter, 3, 1, set, [.. items], comparer);
        TestItem(counter, null, 1);
    }

    [TestMethod]
    public void ResetCount_ValTest()
    {
        var query = Enumerable.Range(1, 10);
        var items = new List<int>();
        var comparer = EqualityComparer<int>.Default;
        var set = new HashSet<int>(query);
        var counter = new Counter<int>();

        foreach (int num in query)
        {
            items.AddRange(Enumerable.Repeat(num, num));
            counter.Add(num, num);
        }
        TestProperties(counter, 10, 55, set, [.. items], comparer);

        counter.ResetCount(1);
        items.Remove(1);
        TestProperties(counter, 10, 54, set, [.. items], comparer);
        TestItem(counter, 1, 0);

        counter.ResetCount(10);
        items.RemoveAll(x => x == 10);
        TestProperties(counter, 10, 44, set, [.. items], comparer);
        TestItem(counter, 10, 0);

        counter.ResetCounts();
        TestProperties(counter, 10, 0, set, [], comparer);

        foreach (int num in query)
            TestItem(counter, num, 0);
    }

    [TestMethod]
    public void ResetCount_RefTest()
    {
        var query = new[] { null, "A", "A", "B" };
        var items = new List<string?>(query);
        var comparer = EqualityComparer<string?>.Default;
        var set = new HashSet<string?>(query);
        var counter = new Counter<string?>();

        counter.AddRange(query);
        TestProperties(counter, 3, 4, set, query, comparer);

        counter.ResetCount("A");
        items.RemoveAll(x => x == "A");
        TestProperties(counter, 3, 2, set, [.. items], comparer);
        TestItem(counter, "A", 0);

        counter.ResetCount(null);
        items.Remove(null);
        TestProperties(counter, 3, 1, set, [.. items], comparer);
        TestItem(counter, null, 0);

        counter.ResetCounts();
        TestProperties(counter, 3, 0, set, [], comparer);
        TestItem(counter, "B", 0);
    }

    [TestMethod]
    public void Clear_Test()
    {
        var counter = new Counter<int>();
        counter.AddRange(Enumerable.Range(0, 10));

        counter.Clear();
        TestProperties(counter, 0, 0, [], [], EqualityComparer<int>.Default);
        TestItem(counter, 0, null);

        var counter2 = new Counter<string?>();
        counter2.Add(null);
        counter2.Add("A");

        counter2.Clear();
        TestProperties(counter2, 0, 0, [], [], EqualityComparer<string?>.Default);
        TestItem(counter2, null, null);
    }

    private static void TestProperties<T>(Counter<T> target, int numberOfItemsExpected, int totalCountExpected,
        HashSet<T> uniqueItemsExpected, T[] repeatedItemsExpected, IEqualityComparer<T> comparerExpected)
    {
        Assert.AreEqual(totalCountExpected, target.TotalCount);
        Assert.AreEqual(numberOfItemsExpected, target.NumberOfItems);
        Assert.IsTrue(uniqueItemsExpected.SetEquals(target.UniqueItems));
        CollectionAssert.AreEquivalent(repeatedItemsExpected, target.RepeatedItems.ToArray());
        Assert.AreEqual(comparerExpected, target.Comparer);

        var array = new T[target.NumberOfItems];
        target.CopyTo(array, 0);
        Assert.IsTrue(uniqueItemsExpected.SetEquals(array));
    }

    private static void TestItem<T>(Counter<T> target, T item, int? countExpected)
    {
        if (countExpected.HasValue)
        {
            Assert.IsTrue(target.Contains(item));
            Assert.AreEqual(countExpected.Value, target.GetCount(item));
        }
        else
        {
            Assert.IsFalse(target.Contains(item));
            Assert.AreEqual(0, target.GetCount(item));
        }
    }
}
