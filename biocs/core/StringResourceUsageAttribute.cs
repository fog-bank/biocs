using System;
using System.Diagnostics;

namespace Biocs
{
	/// <summary>
	/// Specifies the usage of string resources.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, AllowMultiple = true, Inherited = false)]
	[Conditional("DEBUG")]
	public sealed class StringResourceUsageAttribute : Attribute
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="StringResourceUsageAttribute"/> class.
		/// </summary>
		/// <param name="name">The name of the string resource to be used.</param>
		/// <exception cref="ArgumentNullException">The <paramref name="name"/> parameter is null.</exception>
		public StringResourceUsageAttribute(string name)
		{
			if (name == null)
				throw new ArgumentNullException(nameof(name));

			Name = name;
		}

		/// <summary>
		/// Gets the name of the string resource to be used.
		/// </summary>
		public string Name { get; }
	}
}