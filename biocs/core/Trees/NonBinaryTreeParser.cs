using System.Text;

namespace Biocs.Trees;

// https://phylipweb.github.io/phylip/newick_doc.html
internal ref struct NonBinaryTreeParser
{
    const string IgnoreChars = " \t\r\n";

    private readonly ReadOnlySpan<char> source;
    private readonly IFormatProvider? provider;

    // subtree ;
    public NonBinaryTreeParser(ReadOnlySpan<char> span, IFormatProvider? provider)
    {
        source = span.TrimStart(IgnoreChars);
        this.provider = provider;
        int endIndex = IndexOf(source, ';');

        if (endIndex == -1)
        {
            HasError = true;
            return;
        }
        source = source[..endIndex];
    }

    public bool HasError { get; private set; }

    public NonBinaryTree Parse()
    {
        var tree = new NonBinaryTree();

        if (!HasError)
            tree.Root = ParseSubtree(source);

        return tree;
    }

    // descendant_list [label] [: branch_length]
    // label [: branch_length]
    private NonBinaryNode ParseSubtree(ReadOnlySpan<char> span)
    {
        var node = new NonBinaryNode();
        span = span.Trim(IgnoreChars);

        // descendant_list
        int subtreeEndIndex = LastIndexOf(span, ')');
        if (subtreeEndIndex >= 0)
        {
            int subtreeStartIndex = IndexOf(span, '(');
            ParseDescendantList(span[(subtreeStartIndex + 1)..subtreeEndIndex], node);
            span = span[(subtreeEndIndex + 1)..];
        }

        // branch length
        int lengthStartIndex = IndexOf(span, ':');
        if (lengthStartIndex >= 0)
        {
            if (!double.TryParse(span[(lengthStartIndex + 1)..], provider, out double length))
            {
                HasError = true;
                return node;
            }
            node.Length = length;
            span = span[..lengthStartIndex];
        }

        // label
        node.Name = ParseLabel(span);
        return node;
    }

    // ( subtree {, subtree} )
    private void ParseDescendantList(ReadOnlySpan<char> span, NonBinaryNode parent)
    {
        int commaIndex = IndexOf(span, ',');
        var child = ParseSubtree(commaIndex == -1 ? span : span[..commaIndex]);

        if (HasError)
            return;

        parent.AppendChild(child);

        if (commaIndex >= 0)
            ParseDescendantList(span[(commaIndex + 1)..], parent);
    }

    private int IndexOf(ReadOnlySpan<char> span, char c)
    {
        bool quoted = false;
        int depth = 0;
        int commentLevel = 0;

        for (int i = 0; i < span.Length; i++)
        {
            char value = span[i];
            switch (value)
            {
                // quoted
                case '\'' when commentLevel == 0:
                    quoted = !quoted;
                    break;

                // comment
                case '[' when !quoted:
                    commentLevel++;
                    break;

                case ']' when !quoted:
                    if (commentLevel <= 0)
                    {
                        HasError = true;
                        return -1;
                    }
                    commentLevel--;
                    break;

                default:
                    if (!quoted && commentLevel == 0)
                    {
                        if (value == ')')
                            depth--;

                        if (depth == 0 && value == c)
                            return i;

                        if (value == '(')
                            depth++;
                    }
                    break;
            }
        }

        if (commentLevel > 0 || quoted)
            HasError = true;

        return -1;
    }

    private static int LastIndexOf(ReadOnlySpan<char> span, char c)
    {
        bool quoted = false;
        int commentLevel = 0;

        for (int i = span.Length - 1; i >= 0; i--)
        {
            char value = span[i];
            switch (value)
            {
                // quoted
                case '\'' when commentLevel == 0:
                    quoted = !quoted;
                    break;

                // comment
                case '[' when !quoted:
                    commentLevel--;
                    break;

                case ']' when !quoted:
                    commentLevel++;
                    break;

                default:
                    if (!quoted && commentLevel == 0 && value == c)
                        return i;
                    break;
            }
        }
        return -1;
    }

    private static string? ParseLabel(ReadOnlySpan<char> span)
    {
        span = span.Trim(IgnoreChars);

        var sb = new StringBuilder(span.Length);
        bool quoted = false;
        int commentLevel = 0;

        for (int i = 0; i < span.Length; i++)
        {
            char value = span[i];
            switch (value)
            {
                case '[' when !quoted:
                    commentLevel++;
                    break;

                case ']' when !quoted:
                    commentLevel--;
                    break;

                case '\'' when commentLevel == 0:
                    // Single quote characters in a quoted label are represented by two single quotes.
                    if (i > 1 && span[i - 1] == '\'')
                        sb.Append('\'');

                    quoted = !quoted;
                    break;

                case '_' when commentLevel == 0:
                    // Underscore characters in unquoted labels are converted to blanks.
                    sb.Append(quoted ? '_' : ' ');
                    break;

                default:
                    if (commentLevel == 0)
                        sb.Append(value);
                    break;
            }
        }
        return sb.Length > 0 ? sb.ToString() : null;
    }
}
