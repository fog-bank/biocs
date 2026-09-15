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
        var tree = NonBinaryTree.Parse("('[A''_]':0.2,B_test:0.4,C[_]:0.3):0.1;");
        Assert.IsNotNull(tree.Root);
        Assert.AreEqual(4, tree.NodeCount);
        Assert.AreEqual(3, tree.LeafCount);
        Assert.HasCount(3, tree.Root.ChildNodes);
        Assert.AreEqual(0.1, tree.Root.Length);
        Assert.AreEqual("[A'_]", tree.Root.ChildNodes[0].Name);
        Assert.AreEqual(0.2, tree.Root.ChildNodes[0].Length);
        Assert.AreEqual("B test", tree.Root.ChildNodes[1].Name);
        Assert.AreEqual(0.4, tree.Root.ChildNodes[1].Length);
        Assert.AreEqual("C", tree.Root.ChildNodes[2].Name);
        Assert.AreEqual(0.3, tree.Root.ChildNodes[2].Length);
    }

    [TestMethod]
    public void ParseAndToStringTest()
    {
        const string Newick = "(A,B,(C,(D,E,F)));";
        var tree = NonBinaryTree.Parse(Newick);
        string result = tree.ToString("");
        Assert.AreEqual(Newick, result);
        Assert.AreEqual(6, tree.LeafCount);

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
    public void ParseFailTest()
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

    [TestMethod]
    public void ToStringTest()
    {
        var tree = new NonBinaryTree
        {
            Root = new NonBinaryNode(0, "Root") { Length = 0.1 }
        };
        tree.Root.AppendChild(new() { Length = double.PositiveInfinity });
        var node = tree.Root.AppendChild(new() { Length = 0.5 });
        node.AppendChild(new() { Name = "C D" });
        node.AppendChild(new() { Name = "E'", Length = -0.1 });

        Assert.AreEqual("(OTU1,(C_D:0.00,'E''':-0.10):0.50):0.10;", tree.ToString("f2"));
    }

    [TestMethod]
    public void ToUnrootTest()
    {
        var tree = new NonBinaryTree();
        var root = new NonBinaryNode();
        tree.Root = root;

        var a = root.AppendChild(new(0, "A") { Length = 0.1 });
        var x = root.AppendChild(new(-1, "X") { Length = 0.2 });
        var b = x.AppendChild(new(1, "B") { Length = 0.3 });
        var c = x.AppendChild(new(2, "C") { Length = 0.4 });

        tree.ToUnroot();
        Assert.AreEqual(0.3, a.Length, 1e-15);
        Assert.AreEqual(1, tree.SumLength);
        Assert.AreEqual(3, tree.LeafCount);
        Assert.HasCount(3, tree.Root.ChildNodes);
        Assert.AreSequenceEqual([a, b, c], tree.Root.ChildNodes);
        Assert.AreEqual("(A:0.3,B:0.3,C:0.4);", tree.ToString("f1"));

        tree.ToUnroot();
        Assert.AreEqual(1, tree.SumLength);
        Assert.AreEqual(3, tree.LeafCount);
        Assert.HasCount(3, tree.Root.ChildNodes);
        Assert.AreEqual("(A:0.3,B:0.3,C:0.4);", tree.ToString("f1"));

        x = tree.Root;
        tree.Root = new();
        tree.Root.AppendChild(x);
        tree.ToUnroot();
        Assert.AreEqual(1, tree.SumLength);
        Assert.AreEqual(3, tree.LeafCount);
        Assert.HasCount(3, tree.Root.ChildNodes);
        Assert.AreEqual("(A:0.3,B:0.3,C:0.4);", tree.ToString("f1"));

        var z = root;
        root = new NonBinaryNode();
        tree.Root = root;
        root.AppendChild(z);
        z.Length = 0.5;
        var y = root.AppendChild(new(-1, "Y") { Length = 0.6 });
        var d = y.AppendChild(new(3, "D") { Length = 0.7 });
        var e = y.AppendChild(new(4, "E") { Length = 0.8 });
        tree.ToUnroot();
        Assert.AreEqual(3.6, tree.SumLength);
        Assert.AreEqual(5, tree.LeafCount);
        Assert.IsGreaterThanOrEqualTo(3, tree.Root.ChildNodes.Count);
        Assert.ContainsAll([a, b, c, d, e], tree.Root.Descendants());

        tree.Root = null!;
        Assert.Throws<InvalidOperationException>(tree.ToUnroot);
        tree.Root = new();
        Assert.Throws<InvalidOperationException>(tree.ToUnroot);
        tree.Root = y;
        Assert.Throws<InvalidOperationException>(tree.ToUnroot);
    }

    [TestMethod]
    public void AsSplitsTest()
    {
        var tree = new NonBinaryTree();
        Assert.Throws<InvalidOperationException>(() => tree.AsSplits(false));

        var root = new NonBinaryNode();
        tree.Root = root;

        var right = root.AppendChild(new());
        var b = right.AppendChild(new(1, "B"));
        var c = right.AppendChild(new(2, "C"));
        var a = root.AppendChild(new(0, "A"));
        Assert.Throws<NotSupportedException>(() => tree.AsSplits(false));

        var left = root.AppendChild(new());
        var d = left.AppendChild(new(3, "D"));
        var left2 = left.AppendChild(new());
        var f = left2.AppendChild(new(5, "F"));
        var g = left2.AppendChild(new(6, "G"));
        var e = left.AppendChild(new(4, "E"));

        var splits = tree.AsSplits(false);

        var expected = new[] {
            KeyValuePair.Create(new Split(7, [1, 2]), right),
            KeyValuePair.Create(new Split(7, [0, 1, 2]), left),
            KeyValuePair.Create(new Split(7, [5, 6]), left2),
        };
        Assert.AreSequenceEqual(expected, splits, SequenceOrder.InAnyOrder);

        var leaves = new[] {
            KeyValuePair.Create(new Split(7, 0), a),
            KeyValuePair.Create(new Split(7, 1), b),
            KeyValuePair.Create(new Split(7, 2), c),
            KeyValuePair.Create(new Split(7, 3), d),
            KeyValuePair.Create(new Split(7, 4), e),
            KeyValuePair.Create(new Split(7, 5), f),
            KeyValuePair.Create(new Split(7, 6), g),
        };

        var splitsWithLeaf = tree.AsSplits(true);
        Assert.AreSequenceEqual(expected.Concat(leaves), splitsWithLeaf, SequenceOrder.InAnyOrder);

        root.AppendChild(new(-1, "Negative_index_leaf"));
        Assert.Throws<InvalidOperationException>(() => tree.AsSplits(false));
    }
}
