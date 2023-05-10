using JetBrains.Annotations;
using System;

namespace LMirman.VespaIO
{
	/// <summary>
	/// When added to a static method, adds it to the <see cref="Commands.Lookup"/> when it is initialized. WARNING: Will not function on non-static methods.
	/// </summary>
	[PublicAPI]
	[MeansImplicitUse]
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
	public class StaticCommandAttribute : Attribute
	{
		/// <inheritdoc cref="Command.Key"/>
		public string Key { get; private set; }
		
		/// <inheritdoc cref="Command.Name"/>
		public string Name { get; set; } = string.Empty;
		
		/// <inheritdoc cref="Command.Description"/>
		public string Description { get; set; } = string.Empty;
		
		/// <inheritdoc cref="Command.Cheat"/>
		public bool Cheat { get; set; }
		
		/// <inheritdoc cref="Command.Hidden"/>
		public bool Hidden { get; set; }

		/// <inheritdoc cref="Command.ManualPriority"/>
		public int ManualPriority { get; set; }

		/// <summary>
		/// When added to a static method, adds it to the <see cref="Commands.Lookup"/> when it is initialized.
		/// </summary>
		/// <remarks>
		/// <b>BEWARE:</b> This attribute will <b>MUST</b> be applied to static methods.
		/// Using the attribute on a non-static method will not be usable!
		/// </remarks>
		/// <param name="key">The name of the command to match in the developer console.</param>
		public StaticCommandAttribute(string key)
		{
			Key = key.CleanseKey();
		}
	}
}