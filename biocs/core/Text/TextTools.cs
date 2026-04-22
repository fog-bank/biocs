namespace Biocs.Text;

/// <summary>
/// Provides utility methods to handle text data.
/// </summary>
public static class TextTools
{
    /// <summary>
    /// Splits the specified character span into values separated by the specified separator.
    /// </summary>
    /// <param name="record">The span of characters to split.</param>
    /// <param name="separator">The character used to separate values.</param>
    /// <returns>An enumerator that iterates through the separated values.</returns>
    /// <remarks>
    /// Each value is represented as a slice of the original span.
    /// This method is useful for parsing delimited text files such as CSV or TSV.
    /// </remarks>
    public static SeparatedValueEnumerator AsSeparatedValues(this ReadOnlySpan<char> record, char separator)
        => new(record, separator);

    /// <summary>
    /// Splits the specified character span into values separated by the specified separator.
    /// </summary>
    /// <param name="record">The span of characters to split.</param>
    /// <param name="separator">The span of characters used to separate values.</param>
    /// <returns>An enumerator that iterates through the separated values.</returns>
    public static SeparatedValueEnumerator AsSeparatedValues(this ReadOnlySpan<char> record, ReadOnlySpan<char> separator)
        => new(record, separator, false);

    /// <summary>
    /// Splits the specified character span into values separated by any occurrence of the specified separator sequence.
    /// </summary>
    /// <param name="recod">The span of characters to split.</param>
    /// <param name="separators">Any number of characters that may delimit the span.</param>
    /// <returns>An enumerator that iterates through the separated values.</returns>
    public static SeparatedValueEnumerator AsSeparatedValuesAny(this ReadOnlySpan<char> recod, ReadOnlySpan<char> separators)
        => new(recod, separators, true);

    /// <summary>
    /// Splits the specified character span into tab-separated values.
    /// </summary>
    /// <param name="record">The span of characters to split, where values are separated by tab characters ('\t').</param>
    /// <returns>An enumerator that iterates through the tab-separated values.</returns>
    public static SeparatedValueEnumerator AsTsv(this ReadOnlySpan<char> record) => AsSeparatedValues(record, '\t');
}
