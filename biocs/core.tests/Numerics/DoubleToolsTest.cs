namespace Biocs.Numerics;

[TestClass]
public class DoubleToolsTest
{
    [TestMethod]
    public void AreCloseTest()
    {
        Assert.IsFalse(DoubleTools.AreClose(1, 1 + 1e-14, 2e-15));
        Assert.IsTrue(DoubleTools.AreClose(1, 1 + 1e-15, -2e-15));
        Assert.IsTrue(DoubleTools.AreClose(1, 1 + DoubleTools.Epsilon / 4));
    }

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
