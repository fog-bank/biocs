namespace Biocs.Numerics;

[TestClass]
public class DoubleToolsTest
{
    [TestMethod]
    public void SumKahanTest()
    {
        double[] values = [1e40, -1e20, 1, 1e20, -1e40];

        Assert.AreNotEqual(1, values.Sum());

        Assert.AreEqual(1, DoubleTools.SumKahan(values));
        Assert.AreEqual(1, DoubleTools.SumKahan(values.AsSpan()));
        Assert.AreEqual(1, DoubleTools.SumKahan(values.AsEnumerable()));
    }
}
