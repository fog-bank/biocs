using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace Biocs.Trees;

/// <summary>
/// Represents a multifurcating tree.
/// </summary>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public class NonBinaryTree : IFormattable, ISpanParsable<NonBinaryTree>
{
    private static readonly SearchValues<char> forbiddenNameChar = SearchValues.Create("_()[]':;,");

    /// <summary>
    /// Initializes a new instance of the <see cref="NonBinaryTree"/> class.
    /// </summary>
    public NonBinaryTree()
    { }

    /// <summary>
    /// Gets or sets the root node of this tree.
    /// </summary>
    [DisallowNull]
    public NonBinaryNode? Root
    {
        get;
        set
        {
            field = value;
            field?.Parent = null;
        }
    }

    /// <summary>
    /// Counts the number of leaf nodes.
    /// </summary>
    public int LeafCount
    {
        get
        {
            int leaves = 0;

            if (Root != null)
            {
                foreach (var node in Root.DescendantsAndSelf())
                {
                    if (node.IsLeaf)
                        leaves++;
                }
            }
            return leaves;
        }
    }

    /// <summary>
    /// Counts the number of nodes in this tree, including <see cref="Root"/>.
    /// </summary>
    public int NodeCount => Root == null ? 0 : Root.DescendantsAndSelf().Count();

    /// <summary>
    /// Computes the sum of branch lengths.
    /// </summary>
    public double SumLength => Root == null ? 0 : Root.SubtreeLength;

    [DebuggerBrowsable(DebuggerBrowsableState.Never), ExcludeFromCodeCoverage]
    private string DebuggerDisplay => $"Leaves = {LeafCount}, SBL = {SumLength:f4}";

    /// <summary>
    /// Changes the tree structure so that the root of this tree has three or more child nodes if it has only two.
    /// </summary>
    /// <remarks>Before executing <see cref="AsSplits"/> method, this method ensures that the child nodes of <see cref="Root"/> have distinct splits. This method does not verify whether other grandchild nodes have a one-to-one correspondence with the splits.</remarks>
    /// <exception cref="InvalidOperationException">The number of leaf nodes in this tree is less than 3.</exception>
    public void ToUnroot()
    {
        if (Root == null || Root.IsLeaf)
            throw new InvalidOperationException();

        if (Root.ChildNodes.Count >= 3)
            return;

        if (Root.ChildNodes.Count == 1)
        {
            var child = Root.ChildNodes[0];
            Root = child;
            Root.Length = 0;
            ToUnroot();
            return;
        }

        var right = Root.ChildNodes[0];
        var left = Root.ChildNodes[1];

        if (right.ChildNodes.Count >= 2)
        {
            Root.RemoveChild(left);
            Root.RemoveChild(right);

            foreach (var grandchild in right.ChildNodes)
                Root.AppendChild(grandchild);

            left.Length += right.Length;
            Root.AppendChild(left);
            right.ReleaseFromTree();
        }
        else
        {
            if (left.ChildNodes.Count < 2)
                throw new InvalidOperationException();

            foreach (var grandchild in left.ChildNodes)
                Root.AppendChild(grandchild);

            right.Length += left.Length;
            Root.RemoveChild(left);
            left.ReleaseFromTree();
        }
    }

    /// <summary>
    /// Computes a mapping of splits to the corresponding <see cref="NonBinaryNode"/>.
    /// </summary>
    /// <param name="includeLeaves">
    /// If <see langword="true"/>, splits corresponding to leaf nodes will be included in the returned dictionary;
    /// otherwise only internal splits are included.
    /// </param>
    /// <returns>
    /// A dictionary that maps each computed <see cref="Split"/> to the <see cref="NonBinaryNode"/> that induces it.
    /// </returns>
    /// <exception cref="InvalidOperationException"><see cref="Root"/> is <see langword="null"/>.</exception>
    /// <exception cref="NotSupportedException"><see cref="Root"/> is not a multifurcating root.</exception>
    public Dictionary<Split, NonBinaryNode> AsSplits(bool includeLeaves)
    {
        if (Root == null)
            throw new InvalidOperationException();

        if (Root.ChildNodes.Count < 3)
            throw new NotSupportedException();

        int leafCount = CheckAndCountLeaves(Root);

        var splits = new Dictionary<Split, NonBinaryNode>();
        foreach (var node in Root.ChildNodes)
            ComputeSplit(node, splits, leafCount, includeLeaves);

        return splits;

        static int CheckAndCountLeaves(NonBinaryNode node)
        {
            if (node.IsLeaf)
            {
                if (node.Index < 0)
                    throw new InvalidOperationException();

                return 1;
            }
            else
            {
                if (node.ChildNodes.Count == 1)
                    throw new InvalidOperationException();

                int leaves = 0;
                foreach (var child in node.ChildNodes)
                    leaves += CheckAndCountLeaves(child);

                return leaves;
            }
        }

        static Split ComputeSplit(NonBinaryNode node,
            Dictionary<Split, NonBinaryNode> splits, int leafCount, bool includeLeaves)
        {
            if (node.IsLeaf)
            {
                var split = new Split(leafCount, node.Index);
                if (includeLeaves)
                    splits.Add(split, node);
                return split;
            }
            else
            {
                var split = Split.FromChildren(node.ChildNodes.Select(
                    child => ComputeSplit(child, splits, leafCount, includeLeaves)));
                splits.Add(split, node);
                return split;
            }
        }
    }

    /// <summary>
    /// Returns a string representation of this tree in Newick format using the specified numeric format and culture-specific
    /// formatting information.
    /// </summary>
    /// <param name="format">A numeric format string that defines how the value should be formatted.</param>
    /// <param name="formatProvider">An object that supplies culture-specific formatting information.</param>
    /// <returns>A string of Newick format.</returns>
    /// <remarks>If <paramref name="formatProvider"/> is <see langword="null"/>, the current culture is used.</remarks>
    public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format, IFormatProvider? formatProvider)
    {
        if (Root == null)
            return string.Empty;

        var sb = new StringBuilder();
        int leafIndex = 0;
        FormatSubtree(sb, Root, ref leafIndex, format, NumberFormatInfo.GetInstance(formatProvider));
        return sb.Append(';').ToString();
    }

    /// <summary>
    /// Returns a string representation of this tree in Newick format using the specified numeric format.
    /// </summary>
    /// <param name="format">A numeric format string that defines how the value should be formatted.</param>
    /// <returns>A string of Newick format.</returns>
    /// <remarks>
    /// <para>
    /// If <paramref name="format"/> is <see langword="null"/>, the default format is used.
    /// If <paramref name="format"/> is an empty string, only topology is written.
    /// </para><para>
    /// If <see cref="NonBinaryNode.Name"/> of leaf nodes is null or empty, a temporary name (e.g. OTU1) is written as the label.
    /// </para>
    /// </remarks>
    public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format)
        => ToString(format, CultureInfo.InvariantCulture);

    /// <summary>
    /// Returns a string representation of this tree in Newick format.
    /// </summary>
    /// <returns>A string of Newick format.</returns>
    /// <remarks>
    /// The labels of interior nodes are omitted. The length of infinite value is also omitted.
    /// <see cref="CultureInfo.InvariantCulture"/> is used for formatting lengths.
    /// </remarks>
    public override string ToString() => ToString(null, CultureInfo.InvariantCulture);

    /// <inheritdoc/>
    public static NonBinaryTree Parse(ReadOnlySpan<char> s, IFormatProvider? provider = null)
    {
        var parser = new NonBinaryTreeParser(s, provider);
        var result = parser.Parse();

        if (parser.HasError)
            throw new FormatException();

        return result;
    }

    /// <inheritdoc/>
    public static NonBinaryTree Parse(string s, IFormatProvider? provider = null) => Parse(s.AsSpan(), provider);

    /// <inheritdoc/>
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, [MaybeNullWhen(false)] out NonBinaryTree result)
    {
        var parser = new NonBinaryTreeParser(s, provider);
        result = parser.Parse();
        return !parser.HasError;
    }

    /// <inheritdoc/>
    public static bool TryParse(
        [NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out NonBinaryTree result)
        => TryParse(s.AsSpan(), provider, out result);

    private static void FormatSubtree(StringBuilder sb, NonBinaryNode node,
        ref int leafIndex, string? format, NumberFormatInfo info)
    {
        if (node.IsLeaf)
        {
            leafIndex++;

            if (!string.IsNullOrEmpty(node.Name))
                FormatName(sb, node.Name);
            else
                sb.Append("OTU").Append(leafIndex);
        }
        else
        {
            sb.Append('(');

            foreach (var child in node.ChildNodes)
            {
                FormatSubtree(sb, child, ref leafIndex, format, info);
                sb.Append(',');
            }
            sb.Length--;
            sb.Append(')');
        }

        if (double.IsFinite(node.Length) && (node.Parent != null || node.Length != 0))
        {
            if (format == null)
                sb.Append(info, $":{node.Length}");
            else if (format.Length > 0)
                sb.Append(':').Append(node.Length.ToString(format, info));
        }
    }

    // https://phylipweb.github.io/phylip/newick_doc.html
    private static void FormatName(StringBuilder sb, ReadOnlySpan<char> name)
    {
        if (name.ContainsAny(forbiddenNameChar))
        {
            sb.Append('\'');

            foreach (char ch in name)
            {
                sb.Append(ch);

                // Single quote in quoted label -> two single quotes
                if (ch == '\'')
                    sb.Append('\'');
            }
            sb.Append('\'');
        }
        else if (name.Contains(' '))
        {
            // Underscore character in unquoted label -> blank
            foreach (char ch in name)
                sb.Append(ch == ' ' ? '_' : ch);
        }
        else
            sb.Append(name);
    }
}
