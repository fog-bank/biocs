namespace Biocs.Trees;

[TestClass]
public class SplitTest
{
    [TestMethod]
    public void Test()
    {
        var split = new Split(2, 0);
        Assert.AreEqual(2, split.LeafCount);
        Assert.IsFalse(split.IsEmpty);
        Assert.AreEqual(0, split.IsTrivial());
        Assert.IsTrue(split.IsSameSide(0, 0));
        Assert.IsFalse(split.IsSameSide(0, 1));
        Assert.IsFalse(split.IsSameSide(1, 0));
        Assert.IsTrue(split.IsSameSide(1, 1));

        var split2 = new Split(2, 1);
        Assert.IsFalse(split.IsEmpty);
        Assert.AreEqual(0, split.IsTrivial());
        Assert.IsTrue(split2.IsSameSide(0, 0));
        Assert.IsFalse(split2.IsSameSide(0, 1));
        Assert.IsFalse(split2.IsSameSide(1, 0));
        Assert.IsTrue(split2.IsSameSide(1, 1));

        Assert.AreEqual(split, split2);
        Assert.AreEqual<object>(split, split2);
        Assert.AreEqual(split.GetHashCode(), split2.GetHashCode());
        Assert.IsFalse(split.Equals(null));

        Assert.Throws<ArgumentOutOfRangeException>(() => new Split(1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Split(2, 2));
    }

    [TestMethod]
    public void Test2()
    {
        var split = new Split(3, 0);
        Assert.AreEqual(3, split.LeafCount);
        Assert.IsFalse(split.IsSameSide(0, 1));
        Assert.IsFalse(split.IsSameSide(0, 2));
        Assert.IsTrue(split.IsSameSide(1, 2));

        var split2 = new Split(3, 1);
        Assert.IsFalse(split2.IsSameSide(0, 1));
        Assert.IsTrue(split2.IsSameSide(0, 2));
        Assert.IsFalse(split2.IsSameSide(1, 2));

        var split3 = new Split(3, 2);
        Assert.IsTrue(split3.IsSameSide(0, 1));
        Assert.IsFalse(split3.IsSameSide(0, 2));
        Assert.IsFalse(split3.IsSameSide(1, 2));

        Assert.AreNotEqual(split, split2);
        Assert.AreNotEqual(split2, split3);
        Assert.AreNotEqual(split3, split);
    }

    [TestMethod]
    public void Test3()
    {
        var split = new Split(4, [0, 1]);
        Assert.AreEqual(4, split.LeafCount);
        Assert.IsTrue(split.IsSameSide(0, 1));
        Assert.IsFalse(split.IsSameSide(0, 2));
        Assert.IsFalse(split.IsSameSide(0, 3));
        Assert.IsFalse(split.IsSameSide(1, 2));
        Assert.IsFalse(split.IsSameSide(1, 3));
        Assert.IsTrue(split.IsSameSide(2, 3));

        var split2 = new Split(4, [2, 0]);
        Assert.IsFalse(split2.IsSameSide(0, 1));
        Assert.IsTrue(split2.IsSameSide(0, 2));
        Assert.IsFalse(split2.IsSameSide(0, 3));
        Assert.IsFalse(split2.IsSameSide(1, 2));
        Assert.IsTrue(split2.IsSameSide(1, 3));
        Assert.IsFalse(split2.IsSameSide(2, 3));
        Assert.AreNotEqual(split, split2);

        var split3 = new Split(4, [0, 3]);
        Assert.IsFalse(split3.IsSameSide(0, 1));
        Assert.IsFalse(split3.IsSameSide(0, 2));
        Assert.IsTrue(split3.IsSameSide(0, 3));
        Assert.IsTrue(split3.IsSameSide(1, 2));
        Assert.IsFalse(split3.IsSameSide(1, 3));
        Assert.IsFalse(split3.IsSameSide(2, 3));
        Assert.AreNotEqual(split2, split3);

        var split4 = new Split(4, [1, 2]);
        Assert.IsFalse(split4.IsSameSide(0, 1));
        Assert.IsFalse(split4.IsSameSide(0, 2));
        Assert.IsTrue(split4.IsSameSide(0, 3));
        Assert.IsTrue(split4.IsSameSide(1, 2));
        Assert.IsFalse(split4.IsSameSide(1, 3));
        Assert.IsFalse(split4.IsSameSide(2, 3));
        Assert.AreEqual(split3, split4);
        Assert.AreEqual(split3.GetHashCode(), split4.GetHashCode());

        var split5 = new Split(4, [1, 3]);
        Assert.IsFalse(split5.IsSameSide(0, 1));
        Assert.IsTrue(split5.IsSameSide(0, 2));
        Assert.IsFalse(split5.IsSameSide(0, 3));
        Assert.IsFalse(split5.IsSameSide(1, 2));
        Assert.IsTrue(split5.IsSameSide(1, 3));
        Assert.IsFalse(split5.IsSameSide(2, 3));
        Assert.AreEqual(split2, split5);
        Assert.AreEqual(split2.GetHashCode(), split5.GetHashCode());

        var split6 = new Split(4, [2, 3]);
        Assert.IsTrue(split6.IsSameSide(0, 1));
        Assert.IsFalse(split6.IsSameSide(0, 2));
        Assert.IsFalse(split6.IsSameSide(0, 3));
        Assert.IsFalse(split6.IsSameSide(1, 2));
        Assert.IsFalse(split6.IsSameSide(1, 3));
        Assert.IsTrue(split6.IsSameSide(2, 3));
        Assert.AreEqual(split, split6);
        Assert.AreEqual(split.GetHashCode(), split6.GetHashCode());

        var split7 = new Split(4, []);
        Assert.AreEqual(4, split.LeafCount);
        Assert.IsTrue(split7.IsEmpty);
        Assert.IsTrue(split6.IsSameSide(0, 1));
        Assert.IsTrue(split6.IsSameSide(2, 3));
    }

    [TestMethod]
    public void Test4()
    {
        var split = new Split(9, [1, 2, 1]);
        Assert.AreEqual(9, split.LeafCount);
        Assert.IsTrue(split.IsSameSide(1, 2));
        Assert.IsTrue(split.IsSameSide(0, 3));
        Assert.IsFalse(split.IsSameSide(0, 2));

        var split2 = new Split(9, [0, 8, 0]);
        Assert.IsTrue(split2.IsSameSide(0, 8));
        Assert.IsTrue(split2.IsSameSide(1, 7));
        Assert.IsFalse(split2.IsSameSide(2, 8));
        Assert.AreNotEqual(split, split2);

        var split3 = new Split(9, [0, 3, 4, 5, 6, 7, 8]);
        Assert.IsTrue(split3.IsSameSide(1, 2));
        Assert.IsTrue(split3.IsSameSide(0, 3));
        Assert.IsFalse(split3.IsSameSide(0, 2));
        Assert.AreEqual(split, split3);
        Assert.AreEqual(split.GetHashCode(), split3.GetHashCode());

        Assert.AreNotEqual(new Split(8, 7), split);
    }

    [TestMethod]
    public void FromChildrenTest()
    {
        var split1 = new Split(5, [1, 2]);
        var split2 = new Split(5, [0, 4]);
        var split = Split.FromChildren(split1, split2);

        Assert.AreEqual(5, split.LeafCount);
        Assert.IsTrue(split.IsSameSide(1, 2));
        Assert.IsTrue(split.IsSameSide(0, 4));
        Assert.IsTrue(split.IsSameSide(1, 4));
        Assert.IsFalse(split.IsSameSide(0, 3));

        var split3 = new Split(5, 3);
        Assert.AreEqual(split3, split);

        var root = Split.FromChildren(split, split3);
        Assert.IsTrue(root.IsEmpty);

        Assert.Throws<InvalidOperationException>(() => Split.FromChildren(split1, new Split(4, 0)));
    }

    [TestMethod]
    public void FromChildrenTest2()
    {
        var split1 = new Split(5, [1, 2]);
        var split2 = new Split(5, [0, 4]);
        var split = Split.FromChildren(split1, split2);

        Assert.AreEqual(5, split.LeafCount);
        Assert.IsTrue(split.IsSameSide(1, 2));
        Assert.IsTrue(split.IsSameSide(0, 4));
        Assert.IsTrue(split.IsSameSide(1, 4));
        Assert.IsFalse(split.IsSameSide(0, 3));
        Assert.AreEqual(new Split(5, 3), split);

        Assert.Throws<InvalidOperationException>(() => Split.FromChildren(split1, new Split(4, 0)));
        Assert.Throws<ArgumentException>(() => Split.FromChildren([]));
    }

    [TestMethod]
    public void IsTrivialTest()
    {
        var split = new Split(5, 0);
        Assert.AreEqual(0, split.IsTrivial());

        split = new Split(5, 1);
        Assert.AreEqual(1, split.IsTrivial());

        split = new Split(5, 4);
        Assert.AreEqual(4, split.IsTrivial());

        split = new Split(5, []);
        Assert.AreEqual(-1, split.IsTrivial());

        split = new Split(5, [0, 2]);
        Assert.AreEqual(-1, split.IsTrivial());

        split = new Split(5, [3, 4]);
        Assert.AreEqual(-1, split.IsTrivial());

        split = new Split(100, 10);
        Assert.AreEqual(10, split.IsTrivial());

        split = new Split(100, 20);
        Assert.AreEqual(20, split.IsTrivial());

        split = new Split(100, 50);
        Assert.AreEqual(50, split.IsTrivial());

        split = new Split(100, [30, 50]);
        Assert.AreEqual(-1, split.IsTrivial());
    }
}
