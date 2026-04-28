namespace Biocs.Trees;

[TestClass]
public class NonBinaryTreeTest
{
    [TestMethod]
    public void ConstrcutTest()
    {
        var tree = new NonBinaryTree();
        Assert.AreEqual(0, tree.LeafCount);
        Assert.AreEqual(0, tree.NodeCount);
        Assert.AreEqual(0, tree.SumLength);
        Assert.AreEqual(string.Empty, tree.ToString());
    }

    [TestMethod]
    public void ParseTest()
    {
        var tree = NonBinaryTree.Parse("('[A''_]':0.2,B_test:0.4,C[_]:0.3):0;");
        Assert.IsNotNull(tree.Root);
        Assert.HasCount(3, tree.Root.ChildNodes);
        Assert.AreEqual("[A'_]", tree.Root.ChildNodes[0].Name);
        Assert.AreEqual("B test", tree.Root.ChildNodes[1].Name);
        Assert.AreEqual("C", tree.Root.ChildNodes[2].Name);
    }

    [TestMethod]
    public void ParseAndToStringTest()
    {
        const string Newick = "(A,B,(C,(D,E,F)));";
        var tree = NonBinaryTree.Parse(Newick);
        string result = tree.ToString("");
        Assert.AreEqual(Newick, result);

        const string Newick2 = "(((One:0.2,Two:0.3):0.3,(Three:0.5,Four:0.3):0.2):0.3,Five:0.7);";
        tree = NonBinaryTree.Parse(Newick2);
        result = tree.ToString();
        Assert.AreEqual(Newick2, result);

        const string Newick2b = "(((One:0.2,Two[id:'']:0.3):0.3,\n('Three':0.5,Four:0.3):0.2)\t:0.3,Five:0.7);";
        tree = NonBinaryTree.Parse(Newick2b);
        result = tree.ToString();
        Assert.AreEqual(Newick2, result);
    }

    [TestMethod]
    public void ParstFailTest()
    {
        // No semicolon
        const string Newick = "(A,B)";
        Assert.Throws<FormatException>(() => NonBinaryTree.Parse(Newick));
        Assert.IsFalse(NonBinaryTree.TryParse(Newick, null, out _));

        // Not matched square bracket
        Assert.IsFalse(NonBinaryTree.TryParse("(A,B]);", null, out _));
        Assert.IsFalse(NonBinaryTree.TryParse("[(A,B);", null, out _));

        // Can not parse branch lengths
        Assert.IsFalse(NonBinaryTree.TryParse("(A:0,B:x):0.2;", null, out _));
    }
}
