namespace Biocs.Collections;

[TestClass]
public class CollectionToolsTest
{
    [TestMethod]
    public void AllItemsAreEqualTest()
    {
        // struct
        var src = Enumerable.Empty<int>();

        Assert.IsFalse(src.AllItemsAreEqual());
        Assert.IsFalse(src.AllItemsAreEqual(null, out int value));
        Assert.AreEqual(0, value);

        src = new[] { 1 };
        Assert.IsTrue(src.AllItemsAreEqual());
        Assert.IsTrue(src.AllItemsAreEqual(null, out value));
        Assert.AreEqual(1, value);

        src = Enumerable.Repeat(1, 10);
        Assert.IsTrue(src.AllItemsAreEqual());
        Assert.IsTrue(src.AllItemsAreEqual(null, out value));
        Assert.AreEqual(1, value);

        src = Enumerable.Range(1, 10);
        Assert.IsFalse(src.AllItemsAreEqual());
        Assert.IsFalse(src.AllItemsAreEqual(null, out value));
        Assert.AreEqual(0, value);

        src = Enumerable.Repeat(1, 10).Concat(new[] { 2 });
        Assert.IsFalse(src.AllItemsAreEqual());
        Assert.IsFalse(src.AllItemsAreEqual(null, out value));
        Assert.AreEqual(0, value);

        // class
        var src2 = new[] { new object(), null };
        Assert.IsFalse(src2.AllItemsAreEqual());
        Assert.IsFalse(src2.AllItemsAreEqual(null, out object? value2));
        Assert.AreEqual(null, value2);

        src2[1] = src2[0];
        Assert.IsTrue(src2.AllItemsAreEqual());
        Assert.IsTrue(src2.AllItemsAreEqual(null, out value2));
        Assert.AreEqual(src2[0], value2);

        // case sensitive
        var src3 = new[] { "ABC", "aBC", "AbC", "ABc", "abC", "aBc", "Abc", "abc" };

        Assert.IsFalse(src3.AllItemsAreEqual());
        Assert.IsTrue(src3.AllItemsAreEqual(StringComparer.OrdinalIgnoreCase, out string? value3));
        Assert.AreEqual("ABC", value3);

        Assert.ThrowsException<ArgumentNullException>(() => ((IEnumerable<object>)null!).AllItemsAreEqual());
    }
}
