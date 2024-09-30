using System.Collections;

namespace Biocs.Collections;

[TestClass]
public class ReadOnlyCollectionTest
{
    [TestMethod]
    public void Test()
    {
        var array = new[] { 1, 2, 3, 4 };
        var wrapper = CollectionTools.AsReadOnly(array);
        var wrapperGeneric = wrapper as ICollection<int>;
        var wrapperNonGeneric = wrapper as ICollection;
        var copy = new int[array.Length];

        Assert.AreEqual(array.Length, wrapper.Count);
        Assert.IsTrue(wrapper.SequenceEqual(array));

        Assert.IsNotNull(wrapperGeneric);
        wrapperGeneric.CopyTo(copy, 0);
        CollectionAssert.AreEqual(array, copy);
        Assert.IsFalse(wrapperGeneric.Contains(0));
        Assert.IsTrue(wrapperGeneric.IsReadOnly);

        Assert.IsNotNull(wrapperNonGeneric);
        Assert.IsFalse(wrapperNonGeneric.IsSynchronized);
        Assert.IsNotNull(wrapperNonGeneric.SyncRoot);
        {
            int i = 0;
            foreach (object value in wrapperNonGeneric)
                Assert.AreEqual(array[i++], value);
        }
        Array.Clear(copy);
        wrapperNonGeneric.CopyTo(copy, 0);
        CollectionAssert.AreEqual(array, copy);

        Assert.ThrowsException<ArgumentNullException>(() => CollectionTools.AsReadOnly<object>(null!));
        Assert.ThrowsException<NotSupportedException>(() => wrapperGeneric.Add(0));
        Assert.ThrowsException<NotSupportedException>(() => wrapperGeneric.Remove(0));
        Assert.ThrowsException<NotSupportedException>(wrapperGeneric.Clear);
        Assert.ThrowsException<ArgumentNullException>(() => wrapperNonGeneric.CopyTo(null!, 0));
        Assert.ThrowsException<ArgumentException>(() => wrapperNonGeneric.CopyTo(new string[1], 0));
    }
}
