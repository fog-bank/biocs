namespace Biocs;

[TestClass]
public class LocationTest
{
    [TestMethod]
    public void UnionWithRangeTest()
    {
        var loc = new Location();

        loc.UnionWith(new SequenceRange());
        AssertRanges(loc, []);

        var range1 = new SequenceRange(100, 200);
        loc.UnionWith(range1);
        AssertRanges(loc, [range1]);

        var range2 = new SequenceRange(300, 400);
        loc.UnionWith(range2);
        AssertRanges(loc, [range1, range2]);

        var range3 = new SequenceRange(220, 240);
        loc.UnionWith(range3);
        AssertRanges(loc, [range1, range3, range2]);

        var range4 = new SequenceRange(401, 500);
        var merge1 = new SequenceRange(300, 500);
        loc.UnionWith(range4);
        AssertRanges(loc, [range1, range3, merge1]);

        var range5 = new SequenceRange(230, 290);
        var merge2 = new SequenceRange(220, 290);
        loc.UnionWith(range5);
        AssertRanges(loc, [range1, merge2, merge1]);

        var range6 = new SequenceRange(1, 270);
        var merge3 = new SequenceRange(1, 290);
        loc.UnionWith(range6);
        AssertRanges(loc, [merge3, merge1]);
    }

    [TestMethod]
    public void SymmetricExceptWithRangeTest()
    {
        Assert.Inconclusive();
    }

    private static void AssertRanges(Location loc, IReadOnlyCollection<SequenceRange> ranges)
    {
        Assert.AreEqual(ranges.Sum(range => range.Length), loc.Length);
        Assert.IsTrue(ranges.SequenceEqual(loc.Ranges));
    }
}
