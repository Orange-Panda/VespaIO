using JetBrains.Annotations;
using System;

namespace LMirman.VespaIO
{
	/// <summary>
	/// When added to a method, property, or field will create a command in the <see cref="Commands.commandSet"/> when the application is started.
	/// Supports static and non-static methods, properties, and fields.<br/><br/>
	/// When added to a property or field will automatically create internal get, set, and autofill functionality where possible.
	/// Supports any field or property including readonly fields, get only properties, and set only properties.
	/// </summary>
	/// <remarks>
	/// Completely ignores access modifiers.
	/// If you wish to omit get or set access to a get/set property create a separate property with this attribute that only has such access to the other property.<br/><br/>
	/// A single command can only point to a single field, property, or set of methods.
	/// Multiple methods can only be assigned to the same command if they have non-conflicting signatures and have the same static or non-static targeting.<br/><br/>
	/// Multiple attributes can be applied to a single target assuming they have different keys.
	/// Do note however you are making separate commands in doing.
	/// Consider using <see cref="Aliases"/> for command shortcuts instead of making multiple definitions for the same command.
	/// </remarks>
	[PublicAPI]
	[MeansImplicitUse]
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true, Inherited = false)]
	public class VespaCommandAttribute : Attribute, ICommandProperties
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
		public VespaCommandAttribute(string key)
		{
			Key = key.CleanseKey();
		}
	}
}