using System.Collections;

namespace Biocs.Collections;

[TestClass]
public class IndexedValueCollectionTest
{
    [TestMethod]
    public void StructTest()
    {
        var map = new Dictionary<int, int>
        {
            [1] = 10,
            [2] = 20,
            [3] = 30,
            [4] = 40
        };
        var keys = new[] { 2, 1, 5 };
        Test(map, keys, -1);
    }

    [TestMethod]
    public void ReferenceTest()
    {
        var map = new Dictionary<string, string>
        {
            ["A"] = "a",
            ["B"] = "b",
            ["C"] = "c",
            ["D"] = "d"
        };
        var keys = new[] { "B", "A", "E" };
        (var target, var expected) = Test(map, keys, "X");

        // via interfaces
        var genericList = target as IList<string>;
        var list = target as IList;

        for (int i = 0; i < expected.Length; i++)
        {
            Assert.AreEqual(expected[i], genericList[i]);
            Assert.AreEqual(expected[i], list[i]);
        }
        Assert.IsTrue(genericList.IsReadOnly);
        Assert.IsTrue(list.IsFixedSize);
        Assert.IsTrue(list.IsReadOnly);
        Assert.IsFalse(list.IsSynchronized);
        Assert.IsNotNull(list.SyncRoot);
        {
            int i = 0;
            foreach (object value in list)
                Assert.AreEqual(expected[i++], value);
        }
        Assert.IsTrue(list.Contains("a"));
        Assert.IsTrue(list.Contains(null));
        Assert.IsFalse(list.Contains(0));

        Assert.AreEqual(1, list.IndexOf("a"));
        Assert.AreEqual(2, list.IndexOf(null));
        Assert.AreEqual(-1, list.IndexOf(0));

        var array = new string?[5];
        list.CopyTo(array, 1);
        Assert.IsTrue(array.SequenceEqual([null, .. expected, null]));
    }

    [TestMethod]
    public void ArgumentTest()
    {
        var map = new Dictionary<int, int>();
        var empty = new IndexedValueCollection<int, int>(map, new int[1]);
        var genericList = empty as IList<int>;
        var list = empty as IList;

        Assert.ThrowsException<ArgumentNullException>(() => new IndexedValueCollection<int, int>(null!, []));
        Assert.ThrowsException<ArgumentNullException>(() => new IndexedValueCollection<int, int>(map, null!));

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => empty[-1]);
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => empty[1]);

        Assert.ThrowsException<ArgumentNullException>(() => empty.CopyTo(null!, 0));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => empty.CopyTo([], -1));
        Assert.ThrowsException<ArgumentException>(() => empty.CopyTo([], 0));

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => empty.TryGetValue(-1, out _));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => empty.TryGetValue(1, out _));

        Assert.ThrowsException<NotSupportedException>(() => genericList[0] = 0);
        Assert.ThrowsException<NotSupportedException>(() => list[0] = 0);
        Assert.ThrowsException<NotSupportedException>(() => genericList.Add(0));
        Assert.ThrowsException<NotSupportedException>(() => list.Add(0));
        Assert.ThrowsException<NotSupportedException>(() => genericList.Insert(0, 0));
        Assert.ThrowsException<NotSupportedException>(() => list.Insert(0, 0));
        Assert.ThrowsException<NotSupportedException>(() => genericList.Remove(0));
        Assert.ThrowsException<NotSupportedException>(() => list.Remove(0));
        Assert.ThrowsException<NotSupportedException>(() => genericList.RemoveAt(0));
        Assert.ThrowsException<NotSupportedException>(() => list.RemoveAt(0));
        Assert.ThrowsException<NotSupportedException>(genericList.Clear);
        Assert.ThrowsException<NotSupportedException>(list.Clear);

        Assert.ThrowsException<ArgumentNullException>(() => list.CopyTo(null!, 0));
        Assert.ThrowsException<ArgumentException>(() => list.CopyTo(new string[1], 0));
    }

    private static (IndexedValueCollection<T, T>, T?[]) Test<T>(Dictionary<T, T> map, T[] keys, T notContainedValue)
        where T : notnull
    {
        var expected = new T?[keys.Length];

        for (int i = 0; i < keys.Length; i++)
            expected[i] = map.GetValueOrDefault(keys[i]);

        var target = new IndexedValueCollection<T, T>(map, keys);

        Assert.AreEqual(keys.Length, target.Count);
        {
            int i = 0;
            foreach (var value in target)
                Assert.AreEqual(expected[i++], value);
        }

        for (int i = 0; i < expected.Length; i++)
        {
            Assert.AreEqual(expected[i], target[i]);
            Assert.AreEqual(i, target.IndexOf(expected[i]));
            Assert.IsTrue(target.Contains(expected[i]));
            Assert.AreEqual(map.ContainsKey(keys[i]), target.TryGetValue(i, out var value));
            Assert.AreEqual(expected[i], value);
        }
        Assert.IsFalse(target.Contains(notContainedValue));
        Assert.AreEqual(-1, target.IndexOf(notContainedValue));

        var array = new T?[5];
        target.CopyTo(array, 1);
        Assert.IsTrue(array.SequenceEqual([default, .. expected, default]));

        return (target, expected);
    }
}
