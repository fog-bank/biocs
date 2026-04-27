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
        int endIndex = IndexOf(source, ';', 0);

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
        int subtreeEndIndex = LastIndexOf(span, ')', span.Length - 1);

        if (subtreeEndIndex >= 0)
        {
            int subtreeStartIndex = IndexOf(span, '(', 0);

            if (subtreeStartIndex == -1)
            {
                HasError = true;
                return node;
            }
            ParseDescendantList(span[(subtreeStartIndex + 1)..subtreeEndIndex], node);
            span = span[(subtreeEndIndex + 1)..];
        }

        int lengthStartIndex = IndexOf(span, ':', 0);

        if (lengthStartIndex >= 0)
        {
            node.Length = double.Parse(span[(lengthStartIndex + 1)..], provider);
            span = span[..lengthStartIndex];
        }
        node.Name = ParseLabel(span);
        return node;
    }

    // ( subtree {, subtree} )
    private void ParseDescendantList(ReadOnlySpan<char> span, NonBinaryNode parent)
    {
        int commaIndex = IndexOf(span, ',', 0);
        if (HasError)
            return;

        var child = ParseSubtree(commaIndex == -1 ? span : span[..commaIndex]);
        if (HasError)
            return;

        parent.AppendChild(child);

        if (commaIndex >= 0)
            ParseDescendantList(span[(commaIndex + 1)..], parent);
    }

    private int IndexOf(ReadOnlySpan<char> span, char c, int startIndex)
    {
        bool quoted = false;
        int depth = 0;
        int comment = 0;

        for (int i = startIndex; i < span.Length; i++)
        {
            char value = span[i];
            switch (value)
            {
                // quoted
                case '\'' when comment == 0:
                    quoted = !quoted;
                    break;

                // comment
                case '[' when !quoted:
                    comment++;
                    break;

                case ']' when !quoted:
                    if (comment <= 0)
                    {
                        HasError = true;
                        return -1;
                    }
                    comment--;
                    break;

                default:
                    if (!quoted && comment == 0)
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
        return -1;
    }

    private int LastIndexOf(ReadOnlySpan<char> span, char c, int startIndex)
    {
        bool quoted = false;
        int comment = 0;

        for (int i = startIndex; i >= 0; i--)
        {
            char value = span[i];
            switch (value)
            {
                // quoted
                case '\'' when comment == 0:
                    quoted = !quoted;
                    break;

                // comment
                case '[' when !quoted:
                    if (comment <= 0)
                    {
                        HasError = true;
                        return -1;
                    }
                    comment--;
                    break;

                case ']' when !quoted:
                    comment++;
                    break;

                default:
                    if (!quoted && comment == 0 && value == c)
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
        int comment = 0;

        for (int i = 0; i < span.Length; i++)
        {
            char value = span[i];
            switch (value)
            {
                case '[' when !quoted:
                    comment++;
                    break;

                case ']' when !quoted:
                    comment--;
                    break;

                case '\'' when comment == 0:
                    // Single quote characters in a quoted label are represented by two single quotes.
                    if (quoted && span[i - 1] == '\'')
                        sb.Append('\'');

                    quoted = !quoted;
                    break;

                case '_' when comment == 0:
                    // Underscore characters in unquoted labels are converted to blanks.
                    sb.Append(quoted ? '_' : ' ');
                    break;

                default:
                    if (comment == 0)
                        sb.Append(value);
                    break;
            }
        }
        return sb.Length > 0 ? sb.ToString() : null;
    }
}
