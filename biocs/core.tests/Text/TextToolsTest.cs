namespace Biocs.Text;

[TestClass]
public class TextToolsTest
{
    [TestMethod]
    public void AsTsvTest()
    {
        // case 1
        var enumerator = string.Empty.AsSpan().AsTsv();
        int count = 0;

        foreach (var span in enumerator)
        {
            Assert.IsTrue(span.IsEmpty);
            count++;
        }
        Assert.AreEqual(1, count);

        // case 2
        string value = "empty";
        enumerator = value.AsSpan().AsTsv();
        count = 0;

        foreach (var span in enumerator)
        {
            Assert.IsTrue(span.SequenceEqual("empty"));
            count++;
        }
        Assert.AreEqual(1, count);

        (var x, var y) = value.AsSpan().AsTsv();
        Assert.IsTrue(x.SequenceEqual("empty"));
        Assert.IsTrue(y.IsEmpty);

        // case 3
        value = "\tx\t\tyz\t";
        enumerator = value.AsSpan().AsTsv();
        count = 0;
        string[] values = [string.Empty, "x", string.Empty, "yz", string.Empty];

        foreach (var span in enumerator)
        {
            Assert.IsTrue(span.SequenceEqual(values[count]));
            count++;
        }
        Assert.AreEqual(5, count);

        // case 4
        enumerator = value.AsSpan(1..^1).AsTsv();
        count = 0;

        foreach (var span in enumerator)
        {
            Assert.IsTrue(span.SequenceEqual(values[count + 1]));
            count++;
        }
        Assert.AreEqual(3, count);

        (x, y, var z) = value.AsSpan(1..^1).AsTsv();
        Assert.IsTrue(x.SequenceEqual(values[1]));
        Assert.IsTrue(y.SequenceEqual(values[2]));
        Assert.IsTrue(z.SequenceEqual(values[3]));
    }

    [TestMethod]
    public void FromSeparatedValuesTest()
    {
        // case 1
        var enumerator = string.Empty.AsSpan().AsSeparatedValues("\t");
        int count = 0;

        foreach (var span in enumerator)
        {
            Assert.IsTrue(span.IsEmpty);
            count++;
        }
        Assert.AreEqual(1, count);

        // case 2
        string value = "empty";
        enumerator = value.AsSpan().AsSeparatedValues("\t");
        count = 0;

        foreach (var span in enumerator)
        {
            Assert.IsTrue(span.SequenceEqual("empty"));
            count++;
        }
        Assert.AreEqual(1, count);

        (var x, var y) = value.AsSpan().AsSeparatedValues("\t");
        Assert.IsTrue(x.SequenceEqual("empty"));
        Assert.IsTrue(y.IsEmpty);

        // case 3
        value = "\tx\t\tyz\t";
        enumerator = value.AsSpan().AsSeparatedValues("\t");
        count = 0;
        string[] values = [string.Empty, "x", string.Empty, "yz", string.Empty];

        foreach (var span in enumerator)
        {
            Assert.IsTrue(span.SequenceEqual(values[count]));
            count++;
        }
        Assert.AreEqual(5, count);

        // case 4
        enumerator = value.AsSpan()[1..^1].AsSeparatedValues("\t");
        count = 0;

        foreach (var span in enumerator)
        {
            Assert.IsTrue(span.SequenceEqual(values[count + 1]));
            count++;
        }
        Assert.AreEqual(3, count);

        // case 5
        value = "chemical synaptic transmission | chloride transmembrane transport | ion transmembrane transport";
        enumerator = value.AsSpan().AsSeparatedValues(" | ");
        count = 0;
        values = ["chemical synaptic transmission", "chloride transmembrane transport", "ion transmembrane transport"];

        foreach (var span in enumerator)
        {
            Assert.IsTrue(span.SequenceEqual(values[count]));
            count++;
        }
        Assert.AreEqual(3, count);

        // case 6

        value = " |  |  |A | ";
        enumerator = value.AsSpan().AsSeparatedValues(" | ");
        count = 0;
        values = [string.Empty, string.Empty, " |A", string.Empty];

        foreach (var span in enumerator)
        {
            Assert.IsTrue(span.SequenceEqual(values[count]));
            count++;
        }
        Assert.AreEqual(4, count);
    }

    [TestMethod]
    public void FromSeparatedValuesAnyTest()
    {
        string value = " \t";
        var enumerator = value.AsSpan().AsSeparatedValuesAny(" \t");
        int count = 0;

        foreach (var span in enumerator)
        {
            Assert.IsTrue(span.IsEmpty);
            count++;
        }
        Assert.AreEqual(3, count);

        value = "ID=FBsf0000411533;Name=BKN40131;Dbxref=FlyBase:FBsf0000411533;";
        enumerator = value.AsSpan().AsSeparatedValuesAny(" =:;");
        count = 0;
        string[] values =
            ["ID", "FBsf0000411533", "Name", "BKN40131", "Dbxref", "FlyBase", "FBsf0000411533", string.Empty];

        foreach (var span in enumerator)
        {
            Assert.IsTrue(span.SequenceEqual(values[count]));
            count++;
        }
        Assert.AreEqual(values.Length, count);
    }
}
