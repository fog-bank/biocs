namespace Biocs;

[TestClass]
public class SequenceRangeTest
{
    [TestMethod]
    public void Constructor_Test()
    {
        int start = 1;
        int end = 20;
        var range = new SequenceRange(start, end);

        Assert.AreEqual(start, range.Start);
        Assert.AreEqual(end, range.End);
        Assert.AreEqual(end - start + 1, range.Length);
        Assert.AreEqual($"{start}..{end}", range.ToString());

        range = new(start);
        Assert.AreEqual(start, range.Start);
        Assert.AreEqual(start, range.End);
        Assert.AreEqual(1, range.Length);
        Assert.AreEqual($"{start}", range.ToString());

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new SequenceRange(10, 1));
    }

    [TestMethod]
    public void Compare_Test()
    {
        var range1 = new SequenceRange(10, 20);
        Assert.IsFalse(range1.Equals(null));
        Assert.IsTrue(((IComparable)range1).CompareTo(null) > 0);
        Assert.ThrowsException<ArgumentException>(() => ((IComparable)range1).CompareTo(new object()));
        {
            var range2 = new SequenceRange(10, 20);
            Assert.IsTrue(range1.Equals(range2));
            Assert.AreEqual(range1, range2);
            Assert.AreEqual(range1.GetHashCode(), range2.GetHashCode());
            Assert.AreEqual(0, range1.CompareTo(range2));
            Assert.IsTrue(range1 == range2);
            Assert.IsFalse(range1 != range2);
            Assert.IsFalse(range1 < range2);
            Assert.IsTrue(range1 <= range2);
            Assert.IsFalse(range1 > range2);
            Assert.IsTrue(range1 >= range2);

            Assert.IsTrue(range1.Equals((object)range2));
            Assert.IsTrue(((IComparable)range1).CompareTo(range2) == 0);
        }
        {
            var range3 = new SequenceRange(1, 15);
            Assert.IsFalse(range1.Equals(range3));
            Assert.IsTrue(range1.CompareTo(range3) > 0);
            Assert.IsTrue(range1 > range3);
            Assert.IsFalse(range1 <= range3);
        }
        {
            var range4 = new SequenceRange(10, 30);
            Assert.IsFalse(range1.Equals(range4));
            Assert.IsTrue(range1.CompareTo(range4) < 0);
            Assert.IsTrue(range1 < range4);
            Assert.IsFalse(range1 >= range4);
        }
        {
            var range5 = new SequenceRange(20, 40);
            Assert.IsFalse(range1.Equals(range5));
            Assert.IsTrue(range1.CompareTo(range5) < 0);
            Assert.IsTrue(range1 < range5);
            Assert.IsFalse(range1 >= range5);
        }
    }
}
