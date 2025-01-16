using System.Runtime.CompilerServices;

namespace Biocs.Numerics;

[TestClass]
public class DiscreteDistributionTest
{
    [TestMethod]
    public void Test()
    {

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
