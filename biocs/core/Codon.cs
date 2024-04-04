using System.Diagnostics.CodeAnalysis;

namespace Biocs;

/// <summary>
/// Represents a nucleotide triplet.
/// </summary>
/// <param name="first">The nucleotide in the first position.</param>
/// <param name="second">The nucleotide in the second position.</param>
/// <param name="third">The nucleotide in the third position.</param>
/// <remarks>
/// <para>The default constructor creates an object whose value is <see cref="Gap"/>.</para>
/// </remarks>
public readonly struct Codon(DnaBase first, DnaBase second, DnaBase third) : IEquatable<Codon>
{
    /// <summary>
    /// Gets the nucleotide in the first position of this codon.
    /// </summary>
    public DnaBase First { get; } = first;

    /// <summary>
    /// Gets the nucleotide in the second position of this codon.
    /// </summary>
    public DnaBase Second { get; } = second;

    /// <summary>
    /// Gets the nucleotide in the third position of this codon.
    /// </summary>
    public DnaBase Third { get; } = third;

    /// <summary>
    /// Gets the string representation of this codon.
    /// </summary>
    public string Symbols => new([First.Symbol, Second.Symbol, Third.Symbol]);

    /// <summary>
    /// Gets a value indicating whether this codon is completely specified.
    /// </summary>
    public bool IsAtomic => First.IsAtomic && Second.IsAtomic && Third.IsAtomic;

    /// <summary>
    /// Gets a codon that is filled with gaps.
    /// </summary>
    public static Codon Gap => default;

    /// <summary>
    /// Gets a codon that is filled with unknown bases.
    /// </summary>
    public static Codon Any => new(DnaBase.Any, DnaBase.Any, DnaBase.Any);

    /// <summary>
    /// Converts this codon to its uppercase equivalent.
    /// </summary>
    /// <returns>The uppercase equivalent of this instance.</returns>
    public Codon ToUpper() => new(First.ToUpper(), Second.ToUpper(), Third.ToUpper());

    /// <summary>
    /// Converts this codon to its lowercase equivalent.
    /// </summary>
    /// <returns>The lowercase equivalent of this instance.</returns>
    public Codon ToLower() => new(First.ToLower(), Second.ToLower(), Third.ToLower());

    /// <inheritdoc/>
    public bool Equals(Codon other) => First == other.First && Second == other.Second && Third == other.Third;

    /// <inheritdoc/>
    public override bool Equals([NotNullWhen(true)] object? obj) => obj is Codon other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => (First.GetHashCode() << 16) + (Second.GetHashCode() << 8) + Third.GetHashCode();

    /// <inheritdoc/>
    public override string ToString() => Symbols;

    /// <summary>
    /// Converts the string representation of a codon to an equivalent <see cref="Codon"/> instance.
    /// </summary>
    /// <param name="value">A string to convert.</param>
    /// <returns>A <see cref="Codon"/> instance whose symbol is represented by <paramref name="value"/>.</returns>
    /// <exception cref="FormatException">
    /// <paramref name="value"/> contains an unknown character in a certain position.
    /// </exception>
    [StringResourceUsage("Format.InvalidCodonSymbol", 1)]
    public static Codon Parse(ReadOnlySpan<char> value)
    {
        if (!TryParse(value, out var result))
            throw new FormatException(Res.GetString("Format.InvalidCodonSymbol", value.ToString()));

        return result;
    }

    /// <summary>
    /// Tries to convert the string representation of a codon to an equivalent <see cref="Codon"/> instance,
    /// and returns a value that indicates whether the conversion succeeded.
    /// </summary>
    /// <param name="value">A string with a length of 3 characters to convert.</param>
    /// <param name="result">
    /// When this method returns, <paramref name="result"/> contains a <see cref="Codon"/> instance that is represented by
    /// <paramref name="value"/> if the conversion succeeded, or <see cref="Gap"/> if the conversion failed.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="value"/> was converted successfully; otherwise, <see langword="false"/>.
    /// </returns>
    public static bool TryParse(ReadOnlySpan<char> value, out Codon result)
    {
        result = Gap;

        if (value.Length != 3)
            return false;

        if (!DnaBase.TryParse(value[0], out var first))
            return false;

        if (!DnaBase.TryParse(value[1], out var second))
            return false;

        if (!DnaBase.TryParse(value[2], out var third))
            return false;

        result = new(first, second, third);
        return true;
    }

    /// <summary>
    /// Compares two <see cref="Codon"/> structures for equality.
    /// </summary>
    /// <param name="one">The first instance of <see cref="Codon"/> to compare.</param>
    /// <param name="other">The second instance of <see cref="Codon"/> to compare.</param>
    /// <returns><see langword="true"/> if the two instances are equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(Codon one, Codon other) => one.Equals(other);

    /// <summary>
    /// Compares two <see cref="Codon"/> structures for inequality.
    /// </summary>
    /// <param name="one">The first instance of <see cref="Codon"/> to compare.</param>
    /// <param name="other">The second instance of <see cref="Codon"/> to compare.</param>
    /// <returns><see langword="false"/> if the two instances are equal; otherwise, <see langword="true"/>.</returns>
    public static bool operator !=(Codon one, Codon other) => !one.Equals(other);
}
