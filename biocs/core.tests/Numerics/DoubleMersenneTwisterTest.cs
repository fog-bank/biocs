namespace Biocs.Numerics;

[TestClass]
public class DoubleMersenneTwisterTest
{
    private string PathTestData { get; } = Path.Combine("Deployments", "dSFMT.19937.out.txt");

    [TestMethod]
    public void Test()
    {
        var data = new List<string>(1000);
        var sep = new[] { ' ' };
        
        foreach (string line in File.ReadLines(PathTestData).Skip(2).Take(250))
            data.AddRange(line.Split(sep, StringSplitOptions.RemoveEmptyEntries));

        var dfmt = new DoubleMersenneTwister(0);

        for (int i = 0; i < 1000; i++)
        {
            string expected = data[i];
            double value = dfmt.Next() + 1;

            var targets = Round(value);
            Assert.IsTrue(expected == targets.Item1 || expected == targets.Item2 || expected == targets.Item3);
        }
    }

    [TestMethod]
    public void TestWithSeeds()
    {
        var data = new List<string>(80);
        var sep = new[] { ' ' };
        int lines = 0;

        foreach (string line in File.ReadLines(PathTestData).Skip(252).Take(80))
        {
            if (lines++ % 4 != 0)
               data.AddRange(line.Split(sep, StringSplitOptions.RemoveEmptyEntries));
        }

        for (int seed = 0, n = 0; seed < 20; seed++)
        {
            var dfmt = new DoubleMersenneTwister(seed);

            for (int i = 0; i < 12; i++)
            {
                string expected = data[n++];
                double value = 0;

                switch (seed % 4)
                {
                    case 0:
                        value = dfmt.Next();
                        break;

                    case 1:
                        value = 1 - dfmt.Next();
                        break;

                    case 2:
                        value = dfmt.NextOpen();
                        break;

                    case 3:
                        value = dfmt.Next() + 1;
                        break;
                }
                var targets = Round(value);
                Assert.IsTrue(expected == targets.Item1 || expected == targets.Item2 || expected == targets.Item3);
            }
        }
    }

    [TestMethod]
    public void TestWithSeedArray()
    {
        var data = new List<string>(1000);
        var sep = new[] { ' ' };

        foreach (string line in File.ReadLines(PathTestData).Skip(333))
            data.AddRange(line.Split(sep, StringSplitOptions.RemoveEmptyEntries));

        var seed = new[] { 1, 2, 3, 4 };
        var dfmt = new DoubleMersenneTwister(seed);

        for (int i = 0; i < 1000; i++)
        {
            string expected = data[i];
            double value = dfmt.Next() + 1;

            var targets = Round(value);
            Assert.IsTrue(expected == targets.Item1 || expected == targets.Item2 || expected == targets.Item3);
        }
    }

    [TestMethod]
    public void Constructor_Test()
    {
        var dfmt = new DoubleMersenneTwister();
        double value = dfmt.Next();
        Assert.IsTrue(value >= 0 && value < 1);
    }

    // Resolves the difference C# Format("f15") and C++ prinf(".15f")
    private static (string, string, string) Round(double value)
    {
        Assert.IsTrue(value >= 0 && value <= 2);

        string roundTrip = value.ToString("r");

        if (roundTrip.Length > 17)
            roundTrip = roundTrip[..17];

        if (value >= 1)
        {
            string exp = value.ToString("e15");
            string expToEven = Math.Round(value, 15, MidpointRounding.ToEven).ToString("e15");

            return (roundTrip, exp[..17], expToEven[..17]);
        }
        else
        {
            string exp = value.ToString("e14");
            string expToEven = Math.Round(value, 15, MidpointRounding.ToEven).ToString("e14");

            int pow = int.Parse(exp[18..]);

            return (roundTrip,
                "0." + new string('0', pow - 1) + exp[0] + exp.Substring(2, 15 - pow),
                "0." + new string('0', pow - 1) + expToEven[0] + expToEven.Substring(2, 15 - pow));
        }
    }
}
