using Microsoft.VisualStudio.TestTools.UnitTesting;

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

        var range3 = new SequenceRange(202, 240);
        loc.UnionWith(range3);
        AssertRanges(loc, [range1, range3, range2]);

        var range4 = new SequenceRange(401, 500);
        var merge1 = new SequenceRange(300, 500);
        loc.UnionWith(range4);
        AssertRanges(loc, [range1, range3, merge1]);

        var range5 = new SequenceRange(230, 290);
        var merge2 = new SequenceRange(202, 290);
        loc.UnionWith(range5);
        AssertRanges(loc, [range1, merge2, merge1]);

        var range7 = new SequenceRange(1, 270);
        var merge3 = new SequenceRange(1, 290);
        loc.UnionWith(range7);
        AssertRanges(loc, [merge3, merge1]);
    }

    [TestMethod]
    public void IntersectWithRangeTest()
    {
        var range1 = new SequenceRange(1, 100);
        var range2 = new SequenceRange(200, 300);
        var range3 = new SequenceRange(400, 500);
        var range4 = new SequenceRange(600, 700);
        var range5 = new SequenceRange(800, 900);

        var loc = new Location(range1);
        loc.UnionWith(range2);
        loc.UnionWith(range3);
        loc.UnionWith(range4);
        loc.UnionWith(range5);

        loc.IntersectWith(new SequenceRange(1, 900));
        AssertRanges(loc, [range1, range2, range3, range4, range5]);

        loc.IntersectWith(new SequenceRange(300, 600));
        AssertRanges(loc, [new(300), range3, new(600)]);

        loc.IntersectWith(new SequenceRange(1000, 2000));
        AssertRanges(loc, []);
    }

    [TestMethod]
    public void ExceptWithRangeTest()
    {
        var loc = new Location(new(1, 100));

        loc.ExceptWith(new SequenceRange(50));
        AssertRanges(loc, [new(1, 49), new(51, 100)]);

        var range1 = new SequenceRange(49, 51);
        var except1 = new SequenceRange(1, 48);
        var except2 = new SequenceRange(52, 100);
        loc.ExceptWith(range1);
        AssertRanges(loc, [except1, except2]);

        loc.ExceptWith(range1);
        AssertRanges(loc, [except1, except2]);

        loc.ExceptWith(new SequenceRange(49, 100));
        AssertRanges(loc, [except1]);

        loc.ExceptWith(new SequenceRange(200, 300));
        AssertRanges(loc, [except1]);
    }

    [TestMethod]
    public void SymmetricExceptWithRangeTest()
    {
        var loc = new Location();

        loc.SymmetricExceptWith(new SequenceRange());
        AssertRanges(loc, []);

        var range1 = new SequenceRange(100, 200);
        loc.SymmetricExceptWith(range1);
        AssertRanges(loc, [range1]);

        var range2 = new SequenceRange(300, 400);
        loc.SymmetricExceptWith(range2);
        AssertRanges(loc, [range1, range2]);

        var range3 = new SequenceRange(70, 80);
        loc.SymmetricExceptWith(range3);
        AssertRanges(loc, [range3, range1, range2]);

        var range4 = new SequenceRange(90, 99);
        var merge1 = new SequenceRange(90, 200);
        loc.SymmetricExceptWith(range4);
        AssertRanges(loc, [range3, merge1, range2]);

        var range5 = new SequenceRange(81, 89);
        var merge2 = new SequenceRange(70, 200);
        loc.SymmetricExceptWith(range5);
        AssertRanges(loc, [merge2, range2]);

        var range6 = new SequenceRange(401, 410);
        var merge3 = new SequenceRange(300, 410);
        loc.SymmetricExceptWith(range6);
        AssertRanges(loc, [merge2, merge3]);

        var range7 = new SequenceRange(1, 500);
        var split1 = new SequenceRange(1, 69);
        var split2 = new SequenceRange(201, 299);
        var split3 = new SequenceRange(411, 500);
        loc.SymmetricExceptWith(range7);
        AssertRanges(loc, [split1, split2, split3]);

        var range8 = new SequenceRange(300, 450);
        var merge4 = new SequenceRange(201, 410);
        var split4 = new SequenceRange(451, 500);
        loc.SymmetricExceptWith(range8);
        AssertRanges(loc, [split1, merge4, split4]);

        var range9 = new SequenceRange(1, 100);
        var split5 = new SequenceRange(70, 100);
        loc.SymmetricExceptWith(range9);
        AssertRanges(loc, [split5, merge4, split4]);

        var range10 = new SequenceRange(100, 249);
        var split6 = new SequenceRange(70, 99);
        var split7 = new SequenceRange(101, 200);
        var split8 = new SequenceRange(250, 410);
        loc.SymmetricExceptWith(range10);
        AssertRanges(loc, [split6, split7, split8, split4]);

        var range11 = new SequenceRange(50, 99);
        var split9 = new SequenceRange(50, 69);
        loc.SymmetricExceptWith(range11);
        AssertRanges(loc, [split9, split7, split8, split4]);

        var range12 = new SequenceRange(451, 510);
        var split10 = new SequenceRange(501, 510);
        loc.SymmetricExceptWith(range12);
        AssertRanges(loc, [split9, split7, split8, split10]);

        var range13 = new SequenceRange(250, 500);
        var split11 = new SequenceRange(411, 510);
        loc.SymmetricExceptWith(range13);
        AssertRanges(loc, [split9, split7, split11]);

        var range14 = new SequenceRange(101, 510);
        var split12 = new SequenceRange(201, 410);
        loc.SymmetricExceptWith(range14);
        AssertRanges(loc, [split9, split12]);

        var range15 = new SequenceRange(301, 350);
        var split13 = new SequenceRange(201, 300);
        var split14 = new SequenceRange(351, 410);
        loc.SymmetricExceptWith(range15);
        AssertRanges(loc, [split9, split13, split14]);

        var range16 = new SequenceRange(401, 410);
        var split15 = new SequenceRange(351, 400);
        loc.SymmetricExceptWith(range16);
        AssertRanges(loc, [split9, split13, split15]);
    }

    private static void AssertRanges(Location loc, IReadOnlyCollection<SequenceRange> ranges)
    {
        Assert.AreEqual(ranges.Sum(range => range.Length), loc.Length);
        Assert.IsTrue(ranges.SequenceEqual(loc.Ranges));
    }
}
