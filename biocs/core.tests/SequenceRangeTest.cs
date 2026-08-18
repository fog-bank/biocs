using Biocs.TestTools;

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
        Assert.IsFalse(range.IsDefault);
        Assert.AreEqual($"{start}..{end}", range.ToString());

        range = new(start);
        Assert.AreEqual(start, range.Start);
        Assert.AreEqual(start, range.End);
        Assert.AreEqual(1, range.Length);
        Assert.IsFalse(range.IsDefault);
        Assert.AreEqual($"{start}", range.ToString());

        range = new();
        Assert.IsTrue(range.IsDefault);

        Assert.Throws<ArgumentOutOfRangeException>(() => new SequenceRange(10, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SequenceRange(0));
    }

    [TestMethod]
    public void Compare_Test()
    {
        var range1 = new SequenceRange(10, 20);
        Assert.IsFalse(range1.Equals(null));
        Assert.IsGreaterThan(0, ((IComparable)range1).CompareTo(null));
        Assert.Throws<ArgumentException>(() => ((IComparable)range1).CompareTo(new object()));
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
            Assert.AreEqual(0, ((IComparable)range1).CompareTo(range2));
        }
        {
            var range3 = new SequenceRange(1, 15);
            Assert.IsFalse(range1.Equals(range3));
            Assert.IsGreaterThan(0, range1.CompareTo(range3));
            Assert.IsTrue(range1 > range3);
            Assert.IsFalse(range1 <= range3);
        }
        {
            var range4 = new SequenceRange(10, 30);
            Assert.IsFalse(range1.Equals(range4));
            Assert.IsLessThan(0, range1.CompareTo(range4));
            Assert.IsTrue(range1 < range4);
            Assert.IsFalse(range1 >= range4);
        }
        {
            var range5 = new SequenceRange(20, 40);
            Assert.IsFalse(range1.Equals(range5));
            Assert.IsLessThan(0, range1.CompareTo(range5));
            Assert.IsTrue(range1 < range5);
            Assert.IsFalse(range1 >= range5);
        }
    }

    [TestMethod]
    public void ContainsTest()
    {
        var range = new SequenceRange(2, 3);
        Assert.IsFalse(range.Contains(1));
        Assert.IsTrue(range.Contains(2));
        Assert.IsTrue(range.Contains(3));
        Assert.IsFalse(range.Contains(4));
    }

    [TestMethod]
    public void OverlapsTest()
    {
        var target = new SequenceRange(10, 20);
        var before = new SequenceRange(1, 5);
        var startOverlaps = new SequenceRange(1, 10);
        var superset = new SequenceRange(1, 30);
        var subset = new SequenceRange(15);
        var endOverlaps = new SequenceRange(20, 30);
        var after = new SequenceRange(30, 40);

        Assert.IsFalse(target.Overlaps(before));
        Assert.IsTrue(target.Overlaps(startOverlaps));
        Assert.IsTrue(target.Overlaps(superset));
        Assert.IsTrue(target.Overlaps(subset));
        Assert.IsTrue(target.Overlaps(endOverlaps));
        Assert.IsFalse(target.Overlaps(after));
    }

    [TestMethod]
    public void ParseTest()
    {
        // Single base number
        ParseTestCore("467", new(467));

        // Base numbers delimiting a sequence span
        ParseTestCore("340..565", new(340, 565));

        // Unknown exact lower boundary
        ParseTestCore("<345..500", new(345, 500));

        // Unknown exact upper boundary
        ParseTestCore("1..>888", new(1, 888));

        // Single base chosen from within a specified range
        ParseTestCore("102.110", new(102, 110));

        // Site between two indicated adjoining bases
        ParseTestCore("123^124", new(123, 124));

        // Allow group separator and exponent
        ParseTestCore("1,000..1,500", new(1000, 1500));
        ParseTestCore("1e3..2e3", new(1000, 2000));

        // Wrong format
        Assert.Throws<FormatException>(() => SequenceRange.Parse(string.Empty));
        Assert.Throws<FormatException>(() => SequenceRange.Parse("-3"));
        Assert.Throws<FormatException>(() => SequenceRange.Parse("x..5"));
        Assert.Throws<FormatException>(() => SequenceRange.Parse("10..y"));
        Assert.Throws<FormatException>(() => SequenceRange.Parse("20..2"));
        Assert.Throws<FormatException>(() => SequenceRange.Parse("1..3..5"));
    }

    private static void ParseTestCore(ReadOnlySpan<char> span, SequenceRange expected)
    {
        Assert.IsTrue(SequenceRange.TryParse(span, out var result));
        Assert.AreEqual(expected, result);
        Assert.AreEqual(expected, SequenceRange.Parse(span));

        AssertUtils.TestParse(expected, span);
        AssertUtils.TestSpanParse(expected, span);
    }
}
