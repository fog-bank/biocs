namespace Biocs;

[TestClass]
public class DnaBaseTest
{
    [TestMethod]
    public void ParseTest()
    {
        Test('-', "Gap", false, true, '-');
        Test('A', "Adenine", true, false, 'T');
        Test('G', "Guanine", true, false, 'C');
        Test('T', "Thymine", true, false, 'A');
        Test('C', "Cytosine", true, false, 'G');
        Test('R', "Purine", false, false, 'Y');
        Test('Y', "Pyrimidine", false, false, 'R');
        Test('M', "Amino", false, false, 'K');
        Test('K', "Keto", false, false, 'M');
        Test('W', "Weak", false, false, 'W');
        Test('S', "Strong", false, false, 'S');
        Test('B', "NotA", false, false, 'V');
        Test('H', "NotG", false, false, 'D');
        Test('V', "NotT", false, false, 'B');
        Test('D', "NotC", false, false, 'H');
        Test('N', "Any", false, false, 'N');

        Assert.ThrowsException<ArgumentException>(() => DnaBase.Parse('%'));

        var result = DnaBase.Any;
        Assert.IsFalse(DnaBase.TryParse('%', out result));
        Assert.AreEqual(default, result);
    }

    [TestMethod]
    public void DefaultAndStaticInstanceTest()
    {
        Assert.AreEqual('-', new DnaBase().Symbol);
        Assert.AreEqual('-', DnaBase.Gap.Symbol);
        Assert.AreEqual('A', DnaBase.Adenine.Symbol);
        Assert.AreEqual('G', DnaBase.Guanine.Symbol);
        Assert.AreEqual('T', DnaBase.Thymine.Symbol);
        Assert.AreEqual('C', DnaBase.Cytosine.Symbol);
        Assert.AreEqual('N', DnaBase.Any.Symbol);
    }

    private static void Test(char symbol, string name, bool atomic, bool gap, char complement)
    {
        char lowerSymbol = char.ToLowerInvariant(symbol);

        BasicTest(symbol, name, atomic, gap, complement);
        BasicTest(lowerSymbol, name, atomic, gap, char.ToLowerInvariant(complement));

        var dna = DnaBase.Parse(symbol);
        var lowerDna = DnaBase.Parse(lowerSymbol);

        // Equality
        if (symbol != lowerSymbol)
        {
            Assert.AreNotEqual(dna, lowerDna);
            Assert.IsFalse(dna.Equals(lowerDna));
            Assert.IsTrue(dna.EqualsCaseInsensitive(lowerDna));
            Assert.IsFalse(dna == lowerDna);
            Assert.IsTrue(dna != lowerDna);
        }
        Assert.AreEqual(dna, dna.ToUpper());
        Assert.AreEqual(lowerDna, dna.ToLower());
        Assert.AreEqual(dna, lowerDna.ToUpper());
        Assert.AreEqual(lowerDna, lowerDna.ToLower());
    }

    private static void BasicTest(char symbol, string name, bool atomic, bool gap, char complement)
    {
        var dna = DnaBase.Parse(symbol);

        // Properties
        Assert.AreEqual(name, dna.Name);
        Assert.AreEqual(symbol, dna.Symbol);
        Assert.AreEqual(atomic, dna.IsAtomic);
        Assert.AreEqual(gap, dna.IsGap);
        Assert.AreEqual(char.IsUpper(symbol), dna.IsUpper);
        Assert.AreEqual(char.IsLower(symbol), dna.IsLower);

        // Convert methods
        Assert.AreEqual(char.ToUpperInvariant(symbol), dna.ToUpper().Symbol);
        Assert.AreEqual(char.ToLowerInvariant(symbol), dna.ToLower().Symbol);
        Assert.AreEqual(complement, dna.Complement().Symbol);
        Assert.AreEqual(symbol.ToString(), dna.ToString());

        // Identity
        var dna2 = DnaBase.Parse(symbol);
        Assert.AreEqual(dna, dna2);
        Assert.IsTrue(dna.Equals(dna2));
        Assert.IsTrue(dna.EqualsCaseInsensitive(dna2));
        Assert.IsTrue(dna.Equals((object)dna2));
        Assert.AreEqual(dna.GetHashCode(), dna2.GetHashCode());
        Assert.IsTrue(dna == dna2);
        Assert.IsFalse(dna != dna2);

        // TryParse
        Assert.IsTrue(DnaBase.TryParse(symbol, out var result));
        Assert.AreEqual(dna, result);
    }
}
