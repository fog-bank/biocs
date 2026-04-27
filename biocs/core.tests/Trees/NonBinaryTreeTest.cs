namespace Biocs.Trees;

[TestClass]
public class NonBinaryTreeTest
{
    [TestMethod]
    public void ParseTest()
    {
        const string Newick = "(A,B,(C,(D,E,F)));";
        var tree = NonBinaryTree.Parse(Newick);
        string result = tree.ToString("");
        Assert.AreEqual(Newick, result);

        const string Newick2 = "(((One:0.2,Two:0.3):0.3,(Three:0.5,Four:0.3):0.2):0.3,Five:0.7);";
        tree = NonBinaryTree.Parse(Newick2);
        result = tree.ToString();
        Assert.AreEqual(Newick2, result);
    }
}
