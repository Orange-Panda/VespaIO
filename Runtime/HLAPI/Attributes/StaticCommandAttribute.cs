using JetBrains.Annotations;
using System;

namespace LMirman.VespaIO
{
	/// <summary>
	/// When added to a static method, adds it to the <see cref="Commands.commandSet"/> when it is initialized.
	/// </summary>
	/// <remarks>
	/// <b>Beware:</b> Will not function on non-static methods.
	/// </remarks>
	[PublicAPI]
	[MeansImplicitUse]
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
	public class StaticCommandAttribute : Attribute, ICommandProperties
	{
		/// <inheritdoc cref="ICommandProperties.Key"/>
		public string Key { get; private set; }
		
		/// <inheritdoc cref="ICommandProperties.Name"/>
		public string Name { get; set; } = string.Empty;
		
		/// <inheritdoc cref="ICommandProperties.Description"/>
		public string Description { get; set; } = string.Empty;
		
		/// <inheritdoc cref="ICommandProperties.Cheat"/>
		public bool Cheat { get; set; }
		
		/// <inheritdoc cref="ICommandProperties.Hidden"/>
		public bool Hidden { get; set; }

		/// <inheritdoc cref="ICommandProperties.ManualPriority"/>
		public int ManualPriority { get; set; }

		/// <summary>
		/// When added to a static method, adds it to the <see cref="Commands.commandSet"/> when it is initialized.
		/// </summary>
		/// <remarks>
		/// <b>BEWARE:</b> This attribute <b>MUST</b> be applied to static methods.
		/// Using the attribute on a non-static method will not be usable!
		/// </remarks>
		/// <param name="key">The name of the command to match in the developer console.</param>
		public StaticCommandAttribute(string key)
		{
			Key = key.CleanseKey();
		}
	}
}