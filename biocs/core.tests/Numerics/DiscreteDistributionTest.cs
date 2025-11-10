using System.Runtime.CompilerServices;

namespace Biocs.Numerics;

[TestClass]
public class DiscreteDistributionTest
{
    [TestMethod]
    public void InitializeTest()
    {
        double[] prob = [0.17, 0.02, 0.15, 0.01, 0.04, 0.25, 0.05, 0.03, 0.20, 0.08];
        var dist = new DiscreteDistribution(prob);
        var proxy = new Proxy(dist);

        Assert.HasCount(prob.Length, proxy.Cutoff);
        Assert.HasCount(prob.Length, proxy.Alias);

        var sum = new double[prob.Length];

        for (int i = 0; i < sum.Length; i++)
        {
            double cutoff = proxy.Cutoff[i];

            if (cutoff < 1)
            {
                sum[i] += cutoff;
                sum[proxy.Alias[i]] += 1 - cutoff;
            }
            else
                sum[i] += 1;
        }

        Assert.AreEqual(prob.Length, sum.Sum());

        for (int i = 0; i < prob.Length; i++)
            Assert.AreEqual(prob[i], sum[i] / prob.Length, 1e-15);
    }

    [TestMethod]
    public void GeneralTest()
    {
        var dist = new DiscreteDistribution();
        Assert.AreEqual(1, dist.Length);
        Assert.AreEqual(0, dist.Next(0));
        Assert.AreEqual(0, dist.Next(0.99));

        dist.Reset([0, 1, 0]);
        Assert.AreEqual(3, dist.Length);
        Assert.AreEqual(1, dist.Next(0));
        Assert.AreEqual(1, dist.Next());
        Assert.AreEqual(1, dist.Next(0.99));

        dist.Reset(default);
        Assert.AreEqual(1, dist.Length);
        Assert.AreEqual(0, dist.Next(0));
        Assert.AreEqual(0, dist.Next(0.99));
    }

    [TestMethod]
    public void ExceptionTest()
    {
        Assert.Throws<ArgumentException>(() => new DiscreteDistribution([0]));
        Assert.Throws<ArgumentException>(() => new DiscreteDistribution([double.NaN]));
        Assert.Throws<ArgumentException>(() => new DiscreteDistribution([2, -1]));

        var dist = new DiscreteDistribution();
        Assert.Throws<ArgumentOutOfRangeException>(() => dist.Next(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => dist.Next(1));
    }

    private class Proxy(DiscreteDistribution target)
    {
        public ref double[] Cutoff => ref cutoff(target);

        public ref int[] Alias => ref alias(target);

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "cutoff")]
        private static extern ref double[] cutoff(DiscreteDistribution target);

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "alias")]
        private static extern ref int[] alias(DiscreteDistribution target);
    }
}
