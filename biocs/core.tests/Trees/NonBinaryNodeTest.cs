namespace Biocs.Trees;

[TestClass]
public class NonBinaryNodeTest
{
    [TestMethod]
    public void LeafTest()
    {
        var node = new NonBinaryNode() { Name = "OTU", Length = 0.1 };
        Assert.IsTrue(node.IsLeaf);
        Assert.IsEmpty(node.ChildNodes);
        Assert.AreEqual(node, Assert.ContainsSingle(node.DescendantsAndSelf()));
        Assert.IsEmpty(node.Descendants());
        Assert.IsEmpty(node.Siblings());

        Assert.Contains(node.Name, node.ToString());

        Assert.IsNull(node.RemoveChild(node));
        Assert.IsTrue(node.IsLeaf);

        node.CollapseChild(10);
        Assert.IsTrue(node.IsLeaf);
    }

    [TestMethod]
    public void DescendantsAndSiblingsTest()
    {
        var root = new NonBinaryNode();
        var node1 = root.AppendChild(new());
        var node2 = root.AppendChild(new());
        var node2a = node2.AppendChild(new());
        var node2a1 = node2a.AppendChild(new());
        var node2a2 = node2a.AppendChild(new());
        var node2b = node2.AppendChild(new());
        var node3 = root.AppendChild(new());

        Assert.AreSequenceEqual([node2a1, node2a2], node2a.Descendants(), SequenceOrder.InAnyOrder);
        Assert.AreSequenceEqual([node2a1, node2a2], node2a.Descendants(), SequenceOrder.InAnyOrder);
        Assert.AreSequenceEqual([node2, node2a, node2a1, node2a2, node2b], node2.DescendantsAndSelf(), SequenceOrder.InAnyOrder);
        Assert.AreSequenceEqual([node1, node2, node2a, node2a1, node2a2, node2b, node3],
            root.Descendants(), SequenceOrder.InAnyOrder);

        Assert.AreSequenceEqual([node2, node3], node1.Siblings(), SequenceOrder.InAnyOrder);
        Assert.AreSequenceEqual([node3, node1], node2.Siblings(), SequenceOrder.InAnyOrder);
        Assert.AreSequenceEqual([node1, node2], node3.Siblings(), SequenceOrder.InAnyOrder);
        Assert.AreSequenceEqual([node2b], node2a.Siblings(), SequenceOrder.InAnyOrder);
        Assert.AreSequenceEqual([node2a], node2b.Siblings(), SequenceOrder.InAnyOrder);
        Assert.AreSequenceEqual([node2a2], node2a1.Siblings(), SequenceOrder.InAnyOrder);
        Assert.AreSequenceEqual([node2a1], node2a2.Siblings(), SequenceOrder.InAnyOrder);
    }

    [TestMethod]
    public void CollapseTest()
    {
        var tree = NonBinaryTree.Parse("((A,B),(C,D,(E,F)),G);");
        Assert.IsNotNull(tree.Root);

        tree.Root.CollapseChild(0);

        Assert.HasCount(7, tree.Root.ChildNodes);
        foreach (var child in tree.Root.ChildNodes)
        {
            Assert.AreEqual(tree.Root, child.Parent);
            Assert.IsTrue(child.IsLeaf);
        }

        tree = NonBinaryTree.Parse("(((A:0.01,B:-0.01):0.01,C:0.03):0.02,(D:0,E:0):-0.02,(F:0.2,G:0.1):-0.01):0.009;");
        Assert.IsNotNull(tree.Root);
        Assert.AreEqual(0.339, tree.SumLength);

        tree.Root.CollapseChild(0.01);

        Assert.AreEqual(0.339, tree.SumLength);
        Assert.AreEqual("((A:0.01,B:-0.01,C:0.03):0.02,(D:0,E:0):-0.02,F:0.2,G:0.1):0.009;", tree.ToString());

        foreach (var parent in tree.Root.DescendantsAndSelf())
        {
            if (!parent.IsLeaf)
            {
                Assert.IsNotEmpty(parent.ChildNodes);

                foreach (var child in parent.ChildNodes)
                    Assert.AreEqual(parent, child.Parent);
            }
            else
                Assert.IsEmpty(parent.ChildNodes);
        }

        Assert.Throws<ArgumentOutOfRangeException>(() => tree.Root.CollapseChild(-1));
    }
}
