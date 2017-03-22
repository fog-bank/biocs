using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Biocs
{
	/// <summary>
	/// Represents nucleotides for DNA.
	/// </summary>
	/// <remarks>
	/// Each member other than <see cref="Name"/> property and <see cref="EqualsCaseInsensitive"/> 
	/// method performs a case-sensitive operation. By default, each instance is uppercase except gaps.
	/// </remarks>
	public struct DnaBase : IEquatable<DnaBase>
	{
		internal DnaBase(DnaBases code)
		{
			Code = code;
		}

		/// <summary>
		/// Gets the description of this nucleotide. 
		/// </summary>
		public string Name => ToUpper(Code).ToString();

		/// <summary>
		/// Gets the character representation of this nucleotide.
		/// </summary>
		public char Symbol => Parser.Value.CodeToSymbol(Code);

		/// <summary>
		/// Gets a value indicating whether this nucleotide is completely specified.
		/// </summary>
		public bool IsAtomic
		{
			get
			{
				switch (ToUpper(Code))
				{
					case DnaBases.Adenine:
					case DnaBases.Guanine:
					case DnaBases.Thymine:
					case DnaBases.Cytosine:
						return true;
				}
				return false;
			}
		}

		/// <summary>
		/// Gets a value indicating whether this instance represents a gap.
		/// </summary>
		public bool IsGap => Code == DnaBases.Gap;

		/// <summary>
		/// Gets a value indicating whether this nucleotide has an uppercase alphabetic symbol.
		/// </summary>
		public bool IsUpper => (Code ^ DnaBases.Any) < DnaBases.Any;

		/// <summary>
		/// Gets a value indicating whether this nucleotide has a lowercase alphabetic symbol.
		/// </summary>
		public bool IsLower => (Code & DnaBases.Lowercase) == DnaBases.Lowercase;

		/// <summary>
		/// Gets the <see cref="DnaBase"/> instance for adenine.
		/// </summary>
		public static DnaBase Adenine { get; } = new DnaBase(DnaBases.Adenine);

		/// <summary>
		/// Gets the <see cref="DnaBase"/> instance for guanine.
		/// </summary>
		public static DnaBase Guanine { get; } = new DnaBase(DnaBases.Guanine);

		/// <summary>
		/// Gets the <see cref="DnaBase"/> instance for thymine.
		/// </summary>
		public static DnaBase Thymine { get; } = new DnaBase(DnaBases.Thymine);

		/// <summary>
		/// Gets the <see cref="DnaBase"/> instance for cytosine.
		/// </summary>
		public static DnaBase Cytosine { get; } = new DnaBase(DnaBases.Cytosine);

		/// <summary>
		/// Gets the <see cref="DnaBase"/> instance for a gap.
		/// </summary>
		public static DnaBase Gap { get; } = new DnaBase(DnaBases.Gap);

		/// <summary>
		/// Gets the <see cref="DnaBase"/> instance for an unknown base.
		/// </summary>
		public static DnaBase Any { get; } = new DnaBase(DnaBases.Any);

		private DnaBases Code { get; }

		private static Lazy<DnaBaseParser> Parser { get; } = new Lazy<DnaBaseParser>();

		/// <summary>
		/// Converts the value of a nucleotide to its uppercase equivalent.
		/// </summary>
		/// <returns>The uppercase equivalent of this instance.</returns>
		public DnaBase ToUpper() => new DnaBase(ToUpper(Code));

		/// <summary>
		/// Converts the value of a nucleotide to its lowercase equivalent.
		/// </summary>
		/// <returns>The lowercase equivalent of this instance.</returns>
		public DnaBase ToLower() => IsGap ? this : new DnaBase(Code | DnaBases.Lowercase);

		/// <summary>
		/// Returns a complentary nucleotide of this nucleotide.
		/// </summary>
		/// <returns>A complementary nucleotide.</returns>
		public DnaBase Complement()
		{
			int code = (int)Code;
			int complement = (code & (int)DnaBases.Lowercase) + ((code & (int)DnaBases.Purine) << 2) + ((code & (int)DnaBases.Pyrimidine) >> 2);
			return new DnaBase((DnaBases)complement);
		}

		/// <summary>
		/// Determines whether the current <see cref="DnaBase"/> instance is equal to a specified <see cref="DnaBase"/> instance.
		/// </summary>
		/// <param name="other">The nucleotide to compare to this instance.</param>
		/// <returns>true if the two instances are equal; otherwise, false.</returns>
		public bool Equals(DnaBase other) => Code == other.Code;

		/// <summary>
		/// Compares two <see cref="DnaBase"/> structures ignoring case for equality.
		/// </summary>
		/// <param name="other">The nucleotide to compare to this instance.</param>
		/// <returns>true if the two instances are equal; otherwise, false.</returns>
		public bool EqualsCaseInsensitive(DnaBase other) => ToUpper(Code) == ToUpper(other.Code);

		/// <inheritdoc cref="object.Equals(object)"/>
		public override bool Equals(object obj) => obj is DnaBase && Equals((DnaBase)obj);

		/// <inheritdoc cref="object.GetHashCode"/>
		public override int GetHashCode() => (int)Code;

		/// <inheritdoc cref="object.ToString"/>
		public override string ToString() => Symbol.ToString();

		/// <summary>
		/// Converts the character representation of a nucleotide to an equivalent <see cref="DnaBase"/> instance.
		/// </summary>
		/// <param name="value">A character to convert.</param>
		/// <returns>A <see cref="DnaBase"/> instance whose symbol is represented by <paramref name="value"/>.</returns>
		/// <exception cref="ArgumentException"><paramref name="value"/> is not one of the symbols defined for <see cref="DnaBase"/>.</exception>
		[StringResourceUsage("ArgEx.InvalidDnaBaseSymbol", 1)]
		public static DnaBase Parse(char value)
		{
			if (!Parser.Value.SymbolToCode(value, out var result))
				throw new ArgumentException(Res.GetString("ArgEx.InvalidDnaBaseSymbol", value), nameof(value));

			return result;
		}

		/// <summary>
		/// Converts the character representation of a nucleotide to an equivalent <see cref="DnaBase"/> instance.
		/// The return value indicates whether the conversion succeeded. 
		/// </summary>
		/// <param name="value">A character to convert.</param>
		/// <param name="result">
		/// When this method returns, <paramref name="result"/> contains a <see cref="DnaBase"/> instance 
		/// whose symbol is represented by <paramref name="value"/> if the parse operation succeeds.
		/// </param>
		/// <returns>true if <paramref name="value"/> was converted successfully; otherwise, false.</returns>
		public static bool TryParse(char value, out DnaBase result) => Parser.Value.SymbolToCode(value, out result);

		/// <summary>
		/// Compares two <see cref="DnaBase"/> structures for equality.
		/// </summary>
		/// <param name="one">The first instance of <see cref="DnaBase"/> to compare.</param>
		/// <param name="other">The second instance of <see cref="DnaBase"/> to compare.</param>
		/// <returns>true if the two instances are equal; otherwise, false.</returns>
		public static bool operator ==(DnaBase one, DnaBase other) => one.Equals(other);

		/// <summary>
		/// Compares two <see cref="DnaBase"/> structures for inequality.
		/// </summary>
		/// <param name="one">The first instance of <see cref="DnaBase"/> to compare.</param>
		/// <param name="other">The second instance of <see cref="DnaBase"/> to compare.</param>
		/// <returns>false if the two instances are equal; otherwise, true.</returns>
		public static bool operator !=(DnaBase one, DnaBase other) => !one.Equals(other);

		private static DnaBases ToUpper(DnaBases code) => code & DnaBases.Any;
	}

	[Flags]
	internal enum DnaBases : byte
	{
		Gap = 0,
		Adenine = 1,
		Guanine = 2,
		Thymine = 4,
		Cytosine = 8,
		Purine = Adenine | Guanine,
		Pyrimidine = Thymine | Cytosine,
		Amino = Adenine | Cytosine,
		Keto = Guanine | Thymine,
		Weak = Adenine | Thymine,
		Strong = Guanine | Cytosine,
		NotA = Guanine | Thymine | Cytosine,
		NotG = Adenine | Thymine | Cytosine,
		NotT = Adenine | Guanine | Cytosine,
		NotC = Adenine | Guanine | Thymine,
		Any = Adenine | Guanine | Thymine | Cytosine,
		Lowercase = 16,
	}

	[SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "Lazy initialization")]
	internal class DnaBaseParser
	{
		private readonly char[] codeToSymbol = new char[32];
		private readonly Dictionary<char, DnaBase> symbolToCode = new Dictionary<char, DnaBase>(31);

		public DnaBaseParser()
		{
			AddDnaBase(DnaBases.Gap, '-');
			AddDnaBase(DnaBases.Adenine, 'A');
			AddDnaBase(DnaBases.Guanine, 'G');
			AddDnaBase(DnaBases.Thymine, 'T');
			AddDnaBase(DnaBases.Cytosine, 'C');
			AddDnaBase(DnaBases.Purine, 'R');
			AddDnaBase(DnaBases.Pyrimidine, 'Y');
			AddDnaBase(DnaBases.Amino, 'M');
			AddDnaBase(DnaBases.Keto, 'K');
			AddDnaBase(DnaBases.Weak, 'W');
			AddDnaBase(DnaBases.Strong, 'S');
			AddDnaBase(DnaBases.NotA, 'B');
			AddDnaBase(DnaBases.NotG, 'H');
			AddDnaBase(DnaBases.NotT, 'V');
			AddDnaBase(DnaBases.NotC, 'D');
			AddDnaBase(DnaBases.Any, 'N');
		}

		public char CodeToSymbol(DnaBases code) => codeToSymbol[(int)code];

		public bool SymbolToCode(char symbol, out DnaBase result) => symbolToCode.TryGetValue(symbol, out result);

		private void AddDnaBase(DnaBases code, char upperSymbol)
		{
			char lowerSymbol = char.ToLowerInvariant(upperSymbol);

			codeToSymbol[(int)code] = upperSymbol;
			codeToSymbol[(int)(code | DnaBases.Lowercase)] = lowerSymbol;
			symbolToCode[upperSymbol] = new DnaBase(code);

			if (upperSymbol != lowerSymbol)
				symbolToCode[lowerSymbol] = new DnaBase(code | DnaBases.Lowercase);
		}
	}
}
