using Biocs.TestTools;

namespace Biocs;

[TestClass]
public class LocationTest
{
    [TestMethod]
    public void EqualsAndOtherBasicsTest()
    {
        var loc1 = new Location();
        Assert.AreEqual(0, loc1.Length);
        Assert.AreEqual(0, loc1.Start);
        Assert.AreEqual(0, loc1.End);
        Assert.IsTrue(loc1.Equals(loc1));
        Assert.IsFalse(loc1.Equals(null));
        Assert.IsTrue(loc1.IsSubsetOf(default));
        Assert.IsTrue(loc1.IsSubsetOf(new(1, 100)));
        Assert.IsFalse(loc1.Overlaps(default));
        Assert.IsFalse(loc1.Overlaps(new(1, 100)));
        Assert.AreEqual(string.Empty, loc1.ToString());

        var loc2 = new Location();
        Assert.IsTrue(loc1.Equals(loc2));
        Assert.AreEqual(loc1.GetHashCode(), loc2.GetHashCode());

        loc2.Clear();
        AssertRanges(loc2, []);

        loc1.UnionWith(new SequenceRange(1, 100));
        Assert.IsFalse(loc1.Equals(loc2));
        Assert.IsTrue(loc1.IsSpan);
        Assert.IsTrue(loc1.IsSubsetOf(new(1, 100)));

        loc2.UnionWith(new SequenceRange(101, 200));
        Assert.IsFalse(loc1.Equals(loc2));
        Assert.IsFalse((loc1 as object).Equals(loc2));
        Assert.IsFalse(loc2.Overlaps(default));
        Assert.IsFalse(loc2.Overlaps(new(90, 100)));
        Assert.IsFalse(loc2.Overlaps(new(201, 210)));
        Assert.IsTrue(loc2.Overlaps(new(101, 200)));
        Assert.IsTrue(loc2.Overlaps(new(101)));
        Assert.IsTrue(loc2.Overlaps(new(200)));

        loc1.ExceptWith(new SequenceRange(31, 59));
        loc2.Clear();
        loc2.UnionWith(new SequenceRange(60, 100));
        loc2.UnionWith(new SequenceRange(1, 30));
        Assert.IsTrue(loc1.Equals(loc2));
        Assert.IsTrue((loc1 as object).Equals(loc2));
        Assert.AreEqual(loc1.GetHashCode(), loc2.GetHashCode());
        Assert.IsFalse(loc1.IsSpan);
        Assert.IsTrue(loc1.IsSubsetOf(new(1, 100)));
        Assert.IsFalse(loc1.IsSubsetOf(new(1, 30)));
        Assert.IsFalse(loc1.IsSubsetOf(new(60, 100)));
    }

    [TestMethod]
    public void OperatorsOnEmptyTest()
    {
        var empty1 = new Location();
        var empty2 = new Location();
        var loc3 = new Location();
        var range1 = new SequenceRange(1, 100);
        var range2 = new SequenceRange(201, 300);
        var rangeEmpty = new SequenceRange();
        loc3.UnionWith(range1);
        loc3.UnionWith(range2);

        // empty | empty
        empty1.UnionWith(empty2);
        AssertRanges(empty1, []);
        empty1.UnionWith(rangeEmpty);
        AssertRanges(empty1, []);
        // empty & empty
        empty1.IntersectWith(empty2);
        AssertRanges(empty1, []);
        empty1.IntersectWith(rangeEmpty);
        AssertRanges(empty1, []);
        // empty - empty
        empty1.ExceptWith(empty2);
        AssertRanges(empty1, []);
        empty1.ExceptWith(rangeEmpty);
        AssertRanges(empty1, []);
        // empty ^ empty
        empty1.SymmetricExceptWith(empty2);
        AssertRanges(empty1, []);
        empty1.SymmetricExceptWith(rangeEmpty);
        AssertRanges(empty1, []);

        // non-empty | empty
        loc3.UnionWith(empty2);
        AssertRanges(loc3, [range1, range2]);
        loc3.UnionWith(rangeEmpty);
        AssertRanges(loc3, [range1, range2]);
        // non-empty - empty
        loc3.ExceptWith(empty2);
        AssertRanges(loc3, [range1, range2]);
        loc3.ExceptWith(rangeEmpty);
        AssertRanges(loc3, [range1, range2]);
        // non-empty ^ empty
        loc3.SymmetricExceptWith(empty2);
        AssertRanges(loc3, [range1, range2]);
        loc3.SymmetricExceptWith(rangeEmpty);
        AssertRanges(loc3, [range1, range2]);
        // non-empty & empty
        loc3.IntersectWith(empty2);
        AssertRanges(loc3, []);
        loc3.UnionWith(range1);
        loc3.UnionWith(range2);
        loc3.IntersectWith(rangeEmpty);
        AssertRanges(loc3, []);
        loc3.UnionWith(range1);
        loc3.UnionWith(range2);

        // empty & non-empty
        empty1.IntersectWith(loc3);
        AssertRanges(empty1, []);
        // empty - non-empty
        empty1.ExceptWith(loc3);
        AssertRanges(empty1, []);
        // empty ^ non-empty
        empty1.SymmetricExceptWith(loc3);
        AssertRanges(empty1, [range1, range2]);
        empty1.Clear();
        // empty | non-empty
        empty1.UnionWith(loc3);
        AssertRanges(empty1, [range1, range2]);
    }

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

        // self
        loc1.UnionWith(loc1);
        AssertRanges(loc1, [range1, range2]);

        var range3 = new SequenceRange(90, 100);
        loc2.UnionWith(range3);
        loc1.UnionWith(loc2);
        AssertRanges(loc2, [range3]);                 // 90..100
        AssertRanges(loc1, [range1, range2, range3]); // 10..20, 40..50, 90..100

        var range4 = new SequenceRange(60, 70);
        loc2.UnionWith(range4);
        loc1.UnionWith(loc2);
        AssertRanges(loc2, [range4, range3]);                 // 60..70, 90..100
        AssertRanges(loc1, [range1, range2, range4, range3]); // 10..20, 40..50, 60..70, 90..100

        loc1.UnionWith(range4);
        AssertRanges(loc1, [range1, range2, range4, range3]);

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

        loc2.UnionWith(loc1);
        AssertRanges(loc2, [merge1, range4, merge2, range7, range9]);

        loc1.UnionWith(loc2);
        AssertRanges(loc1, [merge1, range4, merge2, range7, range9]);

        var range10 = new SequenceRange(30, 240);
        var merge3 = new SequenceRange(1, 300);
        loc1.UnionWith(range10);
        AssertRanges(loc1, [merge3]);

        Assert.Throws<ArgumentNullException>(() => loc1.UnionWith(null!));
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

        Assert.Throws<ArgumentNullException>(() => loc1.IntersectWith(null!));
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
        loc2.UnionWith(range2);
        loc.ExceptWith(loc2);
        AssertRanges(loc, [except1]);

        var range3 = new SequenceRange(10, 20);
        var range4 = new SequenceRange(30, 40);
        loc2.UnionWith(range3);
        loc2.UnionWith(range4);
        var except3 = new SequenceRange(1, 9);
        var except4 = new SequenceRange(21, 29);
        var except5 = new SequenceRange(41, 48);
        loc.ExceptWith(loc2);
        AssertRanges(loc, [except3, except4, except5]);
        AssertRanges(loc2, [range3, range4, range2]);

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

        loc.ExceptWith(new SequenceRange(28));
        var except8 = new SequenceRange(22, 27);
        AssertRanges(loc, [except6, except8, except5]);

        loc.ExceptWith(except8);
        AssertRanges(loc, [except6, except5]);

        var loc4 = new Location();
        loc4.UnionWith(loc);
        loc.ExceptWith(loc4);
        AssertRanges(loc, []);

        Assert.Throws<ArgumentNullException>(() => loc.ExceptWith(null!));
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
        loc.SymmetricExceptWith(range4);
        var merge1 = new SequenceRange(90, 200);
        AssertRanges(loc, [range3, merge1, range2]);

        var range5 = new SequenceRange(81, 89);
        loc.SymmetricExceptWith(range5);
        var merge2 = new SequenceRange(70, 200);
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

        loc.SymmetricExceptWith(split13);
        AssertRanges(loc, [split9, split15]);
    }

    [TestMethod]
    public void MaxValueTest()
    {
        var loc = new Location();
        var full = new SequenceRange(1, int.MaxValue);
        loc.UnionWith(full);
        AssertRanges(loc, [full]);

        var end0 = new SequenceRange(int.MaxValue);
        loc.UnionWith(end0);
        AssertRanges(loc, [full]);

        loc.ExceptWith(end0);
        AssertRanges(loc, [new(1, int.MaxValue - 1)]);

        loc.UnionWith(full);
        var end1 = new SequenceRange(int.MaxValue - 1);
        loc.ExceptWith(end1);
        var left2 = new SequenceRange(1, int.MaxValue - 2);
        AssertRanges(loc, [left2, end0]);

        loc.IntersectWith(new SequenceRange(int.MaxValue - 1, int.MaxValue));
        AssertRanges(loc, [end0]);

        loc.UnionWith(full);
        loc.SymmetricExceptWith(end1);
        AssertRanges(loc, [left2, end0]);

        loc.SymmetricExceptWith(new SequenceRange(int.MaxValue - 10, int.MaxValue - 1));
        var range1 = new SequenceRange(1, int.MaxValue - 11);
        var right2 = new SequenceRange(int.MaxValue - 1, int.MaxValue);
        AssertRanges(loc, [range1, right2]);

        var end10 = new SequenceRange(int.MaxValue - 10, int.MaxValue);
        loc.SymmetricExceptWith(end10);
        AssertRanges(loc, [left2]);
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

        Assert.Throws<FormatException>(() => Location.Parse(default));
    }

    private static void AssertRanges(Location loc, IReadOnlyCollection<SequenceRange> ranges)
    {
        Assert.AreSequenceEqual(ranges, loc.Ranges);
        Assert.AreEqual(ranges.Sum(range => range.IsDefault ? 0 : range.Length), loc.Length);

        // sorted and not connected
        for (int i = 1; i < ranges.Count; i++)
            Assert.IsGreaterThan(loc.Ranges[i - 1].End + 1, loc.Ranges[i].Start);
    }
}
