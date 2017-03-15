using System;
using System.Diagnostics;

namespace Biocs
{
	/// <summary>
	/// Specifies the usage of string resources.
	/// </summary>
	/// <remarks>This API is not intended to be used directly from your code.</remarks>
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, AllowMultiple = true, Inherited = false)]
	[Conditional("DEBUG")]
	public sealed class StringResourceUsageAttribute : Attribute
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="StringResourceUsageAttribute"/> class.
		/// </summary>
		/// <param name="name">The name of the string resource to be used.</param>
		/// <exception cref="ArgumentNullException"><paramref name="name"/> is null.</exception>
		public StringResourceUsageAttribute(string name)
			: this(name, 0)
		{ }

		/// <summary>
		/// Initializes a new instance of the <see cref="StringResourceUsageAttribute"/> class with the number of format items.
		/// </summary>
		/// <param name="name">The name of the string resource to be used.</param>
		/// <param name="formatItemCount">The number of format items contained in the value of the string resource.</param>
		/// <exception cref="ArgumentNullException"><paramref name="name"/> is null.</exception>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="formatItemCount"/> is less than 0.</exception>
		public StringResourceUsageAttribute(string name, int formatItemCount)
		{
			if (name == null)
				throw new ArgumentNullException(nameof(name));

			if (formatItemCount < 0)
				throw new ArgumentOutOfRangeException(nameof(formatItemCount));

			Name = name;
			FormatItemCount = formatItemCount;
		}

		/// <summary>
		/// Gets the name of the string resource to be used.
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Gets the number of format items contained in the value of the string resource.
		/// </summary>
		public int FormatItemCount { get; }

		/// <summary>
		/// Gets or sets a value indicating whether the body of the applied method should be checked.
		/// </summary>
		/// <remarks>
		/// If any element is generated from the applied method by a compiler, or the local resource 
		/// class is not used for the formatting operation, the value of this property is set to true. 
		/// In that case, a tester will not check the body of the applied method.
		/// </remarks>
		public bool ResourceCheckOnly { get; set; }
	}
}
