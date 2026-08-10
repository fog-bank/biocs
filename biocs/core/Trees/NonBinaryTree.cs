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
    private static readonly SearchValues<char> forbiddenNameChar = SearchValues.Create("()[]':;,");

    /// <summary>
    /// Initializes a new instance of the <see cref="NonBinaryTree"/> class.
    /// </summary>
    public NonBinaryTree()
    { }

    /// <summary>
    /// Gets or sets the root node of this tree.
    /// </summary>
    [DisallowNull]
    public NonBinaryNode? Root { get; set; }

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
        FormatSubtree(sb, Root, ref leafIndex,
            format != null ? CompositeFormat.Parse(format) : null, NumberFormatInfo.GetInstance(formatProvider));
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
    /// If <see cref="NonBinaryNode.Name"/> is null or empty, a temporary name (e.g. OTU1) is written as the OTU label.
    /// </para>
    /// </remarks>
    public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format)
        => ToString(format, CultureInfo.InvariantCulture);

    /// <summary>
    /// Returns a string representation of this tree in Newick format.
    /// </summary>
    /// <returns>A string of Newick format.</returns>
    public override string ToString() => ToString(null, CultureInfo.InvariantCulture);

    /// <inheritdoc/>
    public static NonBinaryTree Parse(ReadOnlySpan<char> s, IFormatProvider? provider = null)
    {
        var parser = new NonBinaryTreeParser(s, provider);
        var result = parser.Parse();

        if (parser.HasError)
            ThrowHelper.ThrowFormat(null);

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
        ref int leafIndex, CompositeFormat? format, NumberFormatInfo info)
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
            else if (format.Format.Length > 0)
                sb.Append(':').AppendFormat(info, format, node.Length);
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
