using Biocs.TestTools;

namespace Biocs;

[TestClass]
public class LocationTest
{
    [TestMethod]
    public void UnionWithTest()
    {
        var loc1 = new Location();
        var loc2 = new Location();

        loc1.UnionWith(default(SequenceRange));
        AssertRanges(loc1, []);

        var range1 = new SequenceRange(10, 20);
        var range2 = new SequenceRange(40, 50);
        loc1.UnionWith(range1);
        loc1.UnionWith(range2);
        loc1.UnionWith(loc2);
        AssertRanges(loc1, [range1, range2]);
        AssertRanges(loc2, []);

        loc1.UnionWith(loc1);
        AssertRanges(loc1, [range1, range2]);

        var range3 = new SequenceRange(90, 100);
        loc2.UnionWith(range3);
        loc1.UnionWith(loc2);
        AssertRanges(loc2, [range3]); // 90..100
        AssertRanges(loc1, [range1, range2, range3]); // 10..20, 40..50, 90..100

        var range4 = new SequenceRange(60, 70);
        loc2.UnionWith(range4);
        loc1.UnionWith(loc2);
        AssertRanges(loc2, [range4, range3]); // 60..70, 90..100
        AssertRanges(loc1, [range1, range2, range4, range3]); // 10..20, 40..50, 60..70, 90..100

        var range5 = new SequenceRange(101, 110);
        var range6 = new SequenceRange(21, 39);
        var range7 = new SequenceRange(130, 150);
        var range8 = new SequenceRange(1, 9);
        var range9 = new SequenceRange(200, 300);
        var merge1 = new SequenceRange(1, 50);
        var merge2 = new SequenceRange(90, 110);
        loc2.Clear();
        loc2.UnionWith(range5);
        loc2.UnionWith(range6);
        loc2.UnionWith(range7);
        loc2.UnionWith(range8);
        loc2.UnionWith(range9);
        loc1.UnionWith(loc2);
        AssertRanges(loc2, [range8, range6, range5, range7, range9]); // 1..9, 21..39, 101..110, 130..150, 200..300
        AssertRanges(loc1, [merge1, range4, merge2, range7, range9]); // 1..50, 60..70, 90..110, 130..150, 200..300

        loc1.UnionWith(loc1);
        AssertRanges(loc1, [merge1, range4, merge2, range7, range9]);

        var range10 = new SequenceRange(30, 240);
        var merge3 = new SequenceRange(1, 300);
        loc1.UnionWith(range10);
        AssertRanges(loc1, [merge3]);

        Assert.ThrowsException<ArgumentNullException>(() => loc1.UnionWith(null!));
    }

    [TestMethod]
    public void IntersectWithTest()
    {
        var range1 = new SequenceRange(1, 100);
        var range2 = new SequenceRange(200, 290);
        var range3 = new SequenceRange(400, 480);
        var range4 = new SequenceRange(600, 670);
        var range5 = new SequenceRange(800, 860);

        var loc1 = new Location(range1);
        loc1.IntersectWith(default(SequenceRange));
        AssertRanges(loc1, []);

        loc1.UnionWith(range1);
        loc1.UnionWith(range2);
        loc1.UnionWith(range3);
        loc1.UnionWith(range4);
        loc1.UnionWith(range5);
        loc1.IntersectWith(new SequenceRange(1, 860));
        AssertRanges(loc1, [range1, range2, range3, range4, range5]);

        loc1.IntersectWith(new SequenceRange(101, 799));
        AssertRanges(loc1, [range2, range3, range4]);

        loc1.IntersectWith(new SequenceRange(290, 600));
        AssertRanges(loc1, [new(290), range3, new(600)]);

        loc1.IntersectWith(loc1);
        AssertRanges(loc1, [new(290), range3, new(600)]);

        var loc2 = new Location(new SequenceRange(1, 400));
        loc2.IntersectWith(loc1);
        AssertRanges(loc1, [new(290), range3, new(600)]);
        AssertRanges(loc2, [new(290), new(400)]);

        var loc3 = new Location(new SequenceRange(291, 599));
        loc3.IntersectWith(loc1);
        AssertRanges(loc1, [new(290), range3, new(600)]);
        AssertRanges(loc3, [range3]);

        loc1.IntersectWith(new SequenceRange(291, 399));
        AssertRanges(loc1, []);

        loc2.IntersectWith(loc1);
        AssertRanges(loc1, []);
        AssertRanges(loc2, []);

        loc3.IntersectWith(default(SequenceRange));
        AssertRanges(loc3, []);

        Assert.ThrowsException<ArgumentNullException>(() => loc1.IntersectWith(null!));
    }

    [TestMethod]
    public void ExceptWithTest()
    {
        var loc = new Location(new SequenceRange(1, 100));

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

        var range2 = new SequenceRange(200, 300);
        loc.ExceptWith(range2);
        AssertRanges(loc, [except1]);

        var loc2 = new Location();
        loc.ExceptWith(loc2);
        AssertRanges(loc, [except1]);

        loc2.UnionWith(range2);
        loc.ExceptWith(loc2);
        AssertRanges(loc, [except1]);

        loc2.UnionWith(new SequenceRange(10, 20));
        loc2.UnionWith(new SequenceRange(30, 40));
        var except3 = new SequenceRange(1, 9);
        var except4 = new SequenceRange(21, 29);
        var except5 = new SequenceRange(41, 48);
        loc.ExceptWith(loc2);
        AssertRanges(loc, [except3, except4, except5]);

        loc2.ExceptWith(loc2);
        AssertRanges(loc2, []);

        loc2.UnionWith(new SequenceRange(1));
        loc2.UnionWith(new SequenceRange(21));
        loc2.UnionWith(new SequenceRange(29));
        var except6 = new SequenceRange(2, 9);
        var except7 = new SequenceRange(22, 28);
        loc.ExceptWith(loc2);
        AssertRanges(loc, [except6, except7, except5]);

        var loc3 = new Location();
        loc3.UnionWith(new SequenceRange(1));
        loc3.UnionWith(new SequenceRange(10, 21));
        loc3.UnionWith(new SequenceRange(29, 40));
        loc3.UnionWith(new SequenceRange(49));
        loc.ExceptWith(loc3);
        AssertRanges(loc, [except6, except7, except5]);

        var loc4 = new Location();
        loc4.UnionWith(loc);
        loc.ExceptWith(loc4);
        AssertRanges(loc, []);

        Assert.ThrowsException<ArgumentNullException>(() => loc.ExceptWith(null!));
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

    [TestMethod]
    public void ParseTest()
    {
        var result = Location.Parse("340..565");
        AssertRanges(result, [new(340, 565)]);

        Assert.IsTrue(Location.TryParse("467", out result));
        AssertRanges(result, [new(467)]);

        Assert.IsTrue(Location.TryParse("join(12..78,134..202)", out result));
        AssertRanges(result, [new(12, 78), new(134, 202)]);

        Assert.IsTrue(Location.TryParse("join(<1..1144,1469..>2072)", out result));
        AssertRanges(result, [new(1, 1144), new(1469, 2072)]);
        Assert.IsFalse(result.IsExactStart);
        Assert.IsFalse(result.IsExactEnd);

        Assert.IsTrue(Location.TryParse("complement(34..126)", out result));
        AssertRanges(result, [new(34, 126)]);
        Assert.IsTrue(result.IsComplement);

        string input = "complement(join(2691..4571,4918..5163))";
        Assert.IsTrue(Location.TryParse(input, out result));
        AssertRanges(result, [new(2691, 4571), new(4918, 5163)]);
        Assert.IsTrue(result.IsComplement);

        AssertUtils.TestParse(result, input);
        AssertUtils.TestSpanParse(result, input);

        Assert.IsTrue(Location.TryParse("J00194.1:100..202", out result));
        AssertRanges(result, [new(100, 202)]);
        Assert.AreEqual("J00194.1", result.SequenceName);

        Assert.ThrowsException<FormatException>(() => Location.Parse(default));
    }

    private static void AssertRanges(Location loc, IReadOnlyCollection<SequenceRange> ranges)
    {
        Assert.AreEqual(ranges.Sum(range => range.IsDefault ? 0 : range.Length), loc.Length);
        Assert.IsTrue(ranges.SequenceEqual(loc.Ranges));
    }
}
