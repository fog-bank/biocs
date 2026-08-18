using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Biocs.Numerics;

namespace Biocs.Trees;

/// <summary>
/// Represents the node of a multifurcating tree.
/// </summary>
public class NonBinaryNode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NonBinaryNode"/> class.
    /// </summary>
    public NonBinaryNode()
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="NonBinaryNode"/> class with the specified index and name.
    /// </summary>
    /// <param name="index">The one-based index to identify nodes.</param>
    /// <param name="name">The name assigned to the node.</param>
    public NonBinaryNode(int index, string name) => (Index, Name) = (index, name);

    /// <summary>
    /// Gets or sets the one-based index associated with this node.
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// Gets or sets the label associated with this node.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the distance from this node to its parent node.
    /// </summary>
    public double Length { get; set; }

    /// <summary>
    /// Gets or sets the parent node of this node in the tree structure.
    /// </summary>
    public NonBinaryNode? Parent { get; set; }

    /// <summary>
    /// Gets a value indicating whether this node is a leaf node with no child nodes.
    /// </summary>
    [MemberNotNullWhen(false, nameof(Nodes))]
    [MemberNotNullWhen(false, nameof(FirstChild))]
    public bool IsLeaf => Nodes == null || Nodes.Count == 0;

    /// <summary>
    /// Gets the collection view for the collection of child nodes.
    /// </summary>
    [AllowNull]
    public IReadOnlyList<NonBinaryNode> ChildNodes
    {
        get
        {
            if (IsLeaf)
                return [];

            field ??= Nodes.AsReadOnly();
            return field;
        }
        private set;
    }

    /// <summary>
    /// Gets the total length of this node and all its descendant nodes in the subtree.
    /// </summary>
    /// <remarks>Whenever this property is called, it aggregates the lengths of the node and all its descendants.</remarks>
    public double SubtreeLength => DoubleTools.SumKahan(DescendantsAndSelf().Select(node => node.Length));

    private List<NonBinaryNode>? Nodes
    {
        get;
        set
        {
            field = value;
            ChildNodes = null;
        }
    }

    private NonBinaryNode? FirstChild => IsLeaf ? null : Nodes[0];

    private NonBinaryNode? NextSibling
    {
        get
        {
            var siblings = Parent?.Nodes;
            if (siblings != null)
            {
                int index = siblings.IndexOf(this);
                if (index >= 0 && index < siblings.Count - 1)
                    return siblings[index + 1];
            }
            return null;
        }
    }

    /// <summary>
    /// Returns a collection containing this node and all of its descendant nodes.
    /// </summary>
    /// <returns>
    /// An <see cref="IEnumerable{NonBinaryNode}"/> that contains this node followed by all descendant nodes.</returns>
    public IEnumerable<NonBinaryNode> DescendantsAndSelf() => DescendantsAndSelf(true);

    /// <summary>
    /// Enumerates all descendant nodes of this node.
    /// </summary>
    /// <returns>An enumerable collection of <see cref="NonBinaryNode"/> objects representing the descendants.</returns>
    public IEnumerable<NonBinaryNode> Descendants() => DescendantsAndSelf(false);

    /// <summary>
    /// Enumerates sibling nodes for this node.
    /// </summary>
    /// <returns>An enumerable collection of <see cref="NonBinaryNode"/> objects representing the siblings.</returns>
    public IEnumerable<NonBinaryNode> Siblings()
    {
        if (Parent?.Nodes != null)
        {
            foreach (var sibling in Parent.Nodes)
            {
                if (sibling != this)
                    yield return sibling;
            }
        }
    }

    /// <summary>
    /// Appends the specified node as a child of this node.
    /// </summary>
    /// <param name="node">The node to add as a child.</param>
    /// <returns>The node that was appended as a child.</returns>
    /// <remarks>
    /// The appended node's <see cref="Parent"/> property is set to this node. If the node is already part of another tree,
    /// its previous parent is not updated.
    /// </remarks>
    public NonBinaryNode AppendChild(NonBinaryNode node)
    {
        Nodes ??= new(2);
        Nodes.Add(node);
        node.Parent = this;
        return node;
    }

    /// <summary>
    /// Removes the specified child node from this node's collection of children.
    /// </summary>
    /// <param name="node">The child node to remove from this node.</param>
    /// <returns>The removed child node if it was found and removed; otherwise, <see langword="null"/>.</returns>
    /// <remarks>
    /// If the node is not a child of this node or this node is a leaf, no action is taken and <see langword="null"/> is
    /// returned. After removal, the removed node's <see cref="Parent"/> property is set to <see langword="null"/>.
    /// </remarks>
    public NonBinaryNode? RemoveChild(NonBinaryNode node)
    {
        if (!IsLeaf && Nodes.Remove(node))
        {
            node.Parent = null;
            return node;
        }
        return null;
    }

    /// <summary>
    /// Collapses nodes with near-zero length.
    /// </summary>
    /// <param name="threshold">The maximum length of nodes to be collapsed.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="threshold"/> is negative.</exception>
    /// <remarks>
    /// Descendant nodes whose the absolute value of <see cref="Length"/> is less than or equal to <paramref name="threshold"/>
    /// are spliced out. That is, their children are re-parented to their grandparent and the short node is removed from
    /// the tree.
    /// </remarks>
    public void CollapseChild(double threshold)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(threshold);

        if (IsLeaf)
            return;

        for (int i = 0; i < Nodes.Count; i++)
        {
            var child = Nodes[i];
            if (child.IsLeaf)
                continue;

            child.CollapseChild(threshold);

            if (Math.Abs(child.Length) <= threshold)
            {
                var grandchildren = child.Nodes;

                foreach (var grandchild in grandchildren)
                {
                    Debug.Assert(grandchild.Parent == child);
                    grandchild.Parent = this;
                }
                Nodes.RemoveAt(i);
                Nodes.InsertRange(i, grandchildren);
                i += grandchildren.Count - 1;

                child.Parent = null;
                child.Nodes = null;
            }
        }
    }

    /// <inheritdoc/>
    public override string ToString() =>
        IsLeaf ? $"Name = {Name}, Length = {Length:f4}" : $"ChildNodes = {Nodes.Count}, Length = {Length:f4}";

    private IEnumerable<NonBinaryNode> DescendantsAndSelf(bool containsSelf)
    {
        if (containsSelf)
            yield return this;

        if (IsLeaf)
            yield break;

        var current = FirstChild;
        bool upward = false;
        do
        {
            if (!upward)
            {
                yield return current;

                if (!current.IsLeaf)
                    current = current.FirstChild;
                else
                    upward = true;
            }
            else
            {
                var nextSibling = current.NextSibling;

                if (nextSibling != null)
                {
                    current = nextSibling;
                    upward = false;
                }
                else
                    current = current.Parent;
            }
        }
        while (current != this && current != null);
    }
}
