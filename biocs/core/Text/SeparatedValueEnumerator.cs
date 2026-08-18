namespace Biocs.Text;

/// <summary>
/// Enumerates separated values within a line.
/// </summary>
public ref struct SeparatedValueEnumerator
{
    private ReadOnlySpan<char> values;
    private readonly char charSeparator;
    private readonly ReadOnlySpan<char> spanSeparator;
    private readonly SeparatorType type;
    private bool hasNext;

    internal SeparatedValueEnumerator(ReadOnlySpan<char> record, char separator)
    {
        values = record;
        charSeparator = separator;
        type = SeparatorType.Char;
        hasNext = true;
    }

    internal SeparatedValueEnumerator(ReadOnlySpan<char> record, ReadOnlySpan<char> separator, bool any)
    {
        values = record;
        spanSeparator = separator;
        type = any ? SeparatorType.AnyChar : SeparatorType.String;
        hasNext = true;
    }

    /// <summary>
    /// Gets the current separated value.
    /// </summary>
    public ReadOnlySpan<char> Current { readonly get; private set; }

    /// <summary>
    /// Returns an enumerator that iterates through a line.
    /// </summary>
    /// <returns>An enumerator that can be used to iterate through the line.</returns>
    public readonly SeparatedValueEnumerator GetEnumerator() => this;

    /// <summary>
    /// Advances the enumerator to the next separated value of the line.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if the enumerator was successfully advanced to the next value;
    /// <see langword="false"/> if the enumerator has passed the end of the line.
    /// </returns>
    public bool MoveNext()
    {
        if (!hasNext)
        {
            Current = default;
            return false;
        }

        int index = type switch
        {
            SeparatorType.Char => values.IndexOf(charSeparator),
            SeparatorType.AnyChar => values.IndexOfAny(spanSeparator),
            SeparatorType.String => values.IndexOf(spanSeparator, StringComparison.Ordinal),
            _ => -1
        };

        if (index == -1)
        {
            Current = values;
            hasNext = false;
        }
        else
        {
            Current = values[..index];
            index += type == SeparatorType.String ? spanSeparator.Length : 1;
            values = values[index..];
        }
        return true;
    }

    /// <summary>
    /// Deconstructs <see cref="SeparatedValueEnumerator"/> into separate variables while advancing the enumerator.
    /// </summary>
    /// <param name="first">When this method returns, contains the value after the enumerator was advanced once.</param>
    /// <param name="second">When this method returns, contains the value after the enumerator was advanced twice.</param>
    public void Deconstruct(out ReadOnlySpan<char> first, out ReadOnlySpan<char> second)
    {
        MoveNext();
        first = Current;
        MoveNext();
        second = Current;
    }

    /// <summary>
    /// Deconstructs <see cref="SeparatedValueEnumerator"/> into separate variables while advancing the enumerator.
    /// </summary>
    /// <param name="first">When this method returns, contains the value after the enumerator was advanced once.</param>
    /// <param name="second">When this method returns, contains the value after the enumerator was advanced twice.</param>
    /// <param name="third">When this method returns, contains the value after the enumerator was advanced three times.</param>
    public void Deconstruct(out ReadOnlySpan<char> first, out ReadOnlySpan<char> second, out ReadOnlySpan<char> third)
    {
        MoveNext();
        first = Current;
        MoveNext();
        second = Current;
        MoveNext();
        third = Current;
    }

    /// <summary>
    /// Deconstructs <see cref="SeparatedValueEnumerator"/> into separate variables while advancing the enumerator.
    /// </summary>
    /// <param name="first">When this method returns, contains the value after the enumerator was advanced once.</param>
    /// <param name="second">When this method returns, contains the value after the enumerator was advanced twice.</param>
    /// <param name="third">When this method returns, contains the value after the enumerator was advanced three times.</param>
    /// <param name="fourth">When this method returns, contains the value after the enumerator was advanced four times.</param>
    public void Deconstruct(out ReadOnlySpan<char> first, out ReadOnlySpan<char> second, out ReadOnlySpan<char> third, out ReadOnlySpan<char> fourth)
    {
        MoveNext();
        first = Current;
        MoveNext();
        second = Current;
        MoveNext();
        third = Current;
        MoveNext();
        fourth = Current;
    }

    private enum SeparatorType
    {
        Char,
        AnyChar,
        String
    }
}
