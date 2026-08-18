namespace Biocs.Text;

[TestClass]
public class TextToolsTest
{
    [TestMethod]
    public void AsTsvTest()
    {
        // case 1
        var enumerator = string.Empty.AsSpan().AsTsv();
        ForeachEmptyTest(enumerator, 1);

        // case 2
        string value = "empty";
        enumerator = value.AsSpan().AsTsv();
        ForeachTest(enumerator, [value]);

        (var x, var y) = value.AsSpan().AsTsv();
        Assert.AreEqual(value, x.ToString());
        Assert.IsTrue(y.IsEmpty);

        // case 3
        value = "\tx\t\tyz\t";
        enumerator = value.AsSpan().AsTsv();
        string[] values = [string.Empty, "x", string.Empty, "yz", string.Empty];
        ForeachTest(enumerator, values);

        // case 4
        enumerator = value.AsSpan(1..^1).AsTsv();
        ForeachTest(enumerator, values.AsSpan(1..^1));

        (x, y, var z) = value.AsSpan(1..^1).AsTsv();
        Assert.AreEqual(values[1], x.ToString());
        Assert.AreEqual(values[2], y.ToString());
        Assert.AreEqual(values[3], z.ToString());
    }

    [TestMethod]
    public void FromSeparatedValuesTest()
    {
        // case 1
        var enumerator = string.Empty.AsSpan().AsSeparatedValues("\t");
        ForeachEmptyTest(enumerator, 1);

        // case 2
        string value = "empty";
        enumerator = value.AsSpan().AsSeparatedValues("\t");
        ForeachTest(enumerator, [value]);

        (var x, var y) = value.AsSpan().AsSeparatedValues("\t");
        Assert.AreEqual(value, x.ToString());
        Assert.IsTrue(y.IsEmpty);

        // case 3
        value = "\tx\t\tyz\t";
        enumerator = value.AsSpan().AsSeparatedValues("\t");
        string[] values = [string.Empty, "x", string.Empty, "yz", string.Empty];
        ForeachTest(enumerator, values);

        // case 4
        enumerator = value.AsSpan()[1..^1].AsSeparatedValues("\t");
        ForeachTest(enumerator, values.AsSpan(1..^1));

        // case 5
        value = "chemical synaptic transmission | chloride transmembrane transport | ion transmembrane transport";
        enumerator = value.AsSpan().AsSeparatedValues(" | ");
        values = ["chemical synaptic transmission", "chloride transmembrane transport", "ion transmembrane transport"];
        ForeachTest(enumerator, values);

        var (value0, value1, value2, value3) = value.AsSpan().AsSeparatedValues(" | ");
        Assert.AreEqual(values[0], value0.ToString());
        Assert.AreEqual(values[1], value1.ToString());
        Assert.AreEqual(values[2], value2.ToString());
        Assert.IsTrue(value3.IsEmpty);

        // case 6
        value = " |  |  |A | ";
        enumerator = value.AsSpan().AsSeparatedValues(" | ");
        ForeachTest(enumerator, [string.Empty, string.Empty, " |A", string.Empty]);
    }

    [TestMethod]
    public void FromSeparatedValuesAnyTest()
    {
        string value = " \t";
        var enumerator = value.AsSpan().AsSeparatedValuesAny(" \t");
        ForeachEmptyTest(enumerator, 3);

        value = "ID=FBsf0000411533;Name=BKN40131;Dbxref=FlyBase:FBsf0000411533;";
        enumerator = value.AsSpan().AsSeparatedValuesAny(" =:;");
        ForeachTest(enumerator,
            ["ID", "FBsf0000411533", "Name", "BKN40131", "Dbxref", "FlyBase", "FBsf0000411533", string.Empty]);
    }

    private static void ForeachTest(SeparatedValueEnumerator enumerator, ReadOnlySpan<string> expected)
    {
        int count = 0;

        foreach (var span in enumerator)
        {
            Assert.AreEqual(expected[count], span.ToString());
            count++;
        }
        Assert.AreEqual(expected.Length, count);
    }

    private static void ForeachEmptyTest(SeparatedValueEnumerator enumerator, int expectedCount)
    {
        int count = 0;

        foreach (var span in enumerator)
        {
            Assert.IsTrue(span.IsEmpty);
            count++;
        }
        Assert.AreEqual(expectedCount, count);
    }
}
