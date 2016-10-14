using System;

namespace Biocs
{
	/// <summary>
	/// Represents nucleic acids.
	/// </summary>
	public struct DnaBase : IEquatable<DnaBase>
	{
		public DnaBase(DnaBases code)
		{
			Code = code;
		}

		public DnaBases Code { get; }

		public bool IsAtomic
		{
			get
			{
				switch (Code)
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

		public DnaBase ToUpper() => new DnaBase(Code & DnaBases.Any);

		public DnaBase ToLower() => new DnaBase(Code | DnaBases.Lowercase);

		public bool Equals(DnaBase other) => Code == other.Code;

		public override bool Equals(object obj) => obj is DnaBase && Equals((DnaBase)obj);

		public override int GetHashCode() => (int)Code;

		public static bool operator ==(DnaBase one, DnaBase other) => one.Equals(other);

		public static bool operator !=(DnaBase one, DnaBase other) => !one.Equals(other);
	}

	[Flags]
	public enum DnaBases : byte
	{
		Gap = 0,
		Adenine = 1,
		Guanine = 2,
		Thymine = 4,
		Cytosine = 8,
		Any = Adenine | Guanine | Thymine | Cytosine,
		Purine = Adenine | Guanine,
		Pyrimidine = Thymine | Cytosine,
		Amino = Adenine | Cytosine,
		Keto = Guanine | Thymine,
		Weak = Adenine | Thymine,
		Strong = Guanine | Cytosine,
		Lowercase = 16,
	}
}
