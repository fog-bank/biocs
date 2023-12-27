namespace Biocs;

[TestClass]
public class CodonTest
{
    [TestMethod]
    public void Test()
    {
        var bases = "-AGTCRYMKWSBHVDNagtcrymkwsbhvdn".ToCharArray();

        foreach (char first in bases)
        {
            var firstBase = DnaBase.Parse(first);

            foreach (char second in bases)
            {
                var secondBase = DnaBase.Parse(second);

                foreach (char third in bases)
                {
                    var thirdBase = DnaBase.Parse(third);

                    // Triplet
                    var codon1 = new Codon(firstBase, secondBase, thirdBase);
                    Assert.AreEqual(firstBase, codon1.First);
                    Assert.AreEqual(secondBase, codon1.Second);
                    Assert.AreEqual(thirdBase, codon1.Third);
                    Assert.AreEqual(firstBase.IsAtomic && secondBase.IsAtomic && thirdBase.IsAtomic, codon1.IsAtomic);

                    // String representation
                    string symbol = string.Concat(first, second, third);
                    Assert.IsTrue(Codon.TryParse(symbol, out var codon2));
                    Assert.AreEqual(symbol, codon2.Symbols);
                    Assert.AreEqual(symbol, codon2.ToString());

                    // Case conversion
                    Assert.AreEqual(symbol.ToUpperInvariant(), codon1.ToUpper().Symbols);
                    Assert.AreEqual(symbol.ToLowerInvariant(), codon1.ToLower().Symbols);

                    // Equality
                    Assert.AreEqual(codon1, codon2);
                    Assert.IsTrue(codon1.Equals(codon2));
                    Assert.IsTrue(codon1.Equals((object)codon2));
                    Assert.AreEqual(codon1.GetHashCode(), codon2.GetHashCode());
                    Assert.IsTrue(codon1 == codon2);
                    Assert.IsFalse(codon1 != codon2);
                }
            }
        }
        Assert.AreEqual(Codon.Gap, new Codon());
        Assert.AreEqual("NNN", Codon.Any.Symbols);
    }
}
