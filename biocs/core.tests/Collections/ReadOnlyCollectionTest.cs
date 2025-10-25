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

        Assert.HasCount(array.Length, wrapper);
        Assert.IsTrue(wrapper.SequenceEqual(array));

        Assert.IsNotNull(wrapperGeneric);
        wrapperGeneric.CopyTo(copy, 0);
        CollectionAssert.AreEqual(array, copy);
        Assert.DoesNotContain(0, wrapperGeneric);
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

        Assert.Throws<ArgumentNullException>(() => CollectionTools.AsReadOnly<object>(null!));
        Assert.Throws<NotSupportedException>(() => wrapperGeneric.Add(0));
        Assert.Throws<NotSupportedException>(() => wrapperGeneric.Remove(0));
        Assert.Throws<NotSupportedException>(wrapperGeneric.Clear);
        Assert.Throws<ArgumentNullException>(() => wrapperNonGeneric.CopyTo(null!, 0));
        Assert.Throws<ArgumentException>(() => wrapperNonGeneric.CopyTo(new string[1], 0));
    }
}
