using System;

namespace LMirman.VespaIO
{
	/// <summary>
	/// When added to a static method, adds it to the <see cref="Commands.Lookup"/> when it is initialized. WARNING: Will not function on non-static methods.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
	public class StaticCommandAttribute : Attribute
	{
		/// <summary>
		/// The identifier for the command. When this is input into the console will execute this command.
		/// </summary>
		public string Key { get; private set; }
		/// <summary>
		/// The name of the command as shown in the manual. If not provided will generate with the name of the method the command is attached to.
		/// </summary>
		public string Name { get; set; } = string.Empty;
		/// <summary>
		/// The description of the command as shown in the manual. If not provided will be blank.
		/// </summary>
		public string Description { get; set; } = string.Empty;
		/// <summary>
		/// Determines if this command is a cheat command, thus requiring <see cref="DevConsole.CheatsEnabled"/> to be true for it's execution.
		/// </summary>
		/// <remarks>If multiple command methods are labeled with the same key and any of them have cheats set to true that key is marked as a cheat key. Therefore all commands with the same key are considered cheats regardless of their own value.<br/>Cheat commands are hidden from the manual while cheats are not enabled.</remarks>
		public bool Cheat { get; set; } = false;
		/// <summary>
		/// Determines if this command is hidden from help manual searches. Hidden commands can still be inspected directly and executed.
		/// </summary>
		public bool Hidden { get; set; } = false;
		/// <summary>
		/// Sort this command to the top of the manual. Should be reserved for very important commands only.
		/// </summary>
		public bool ManualPriority { get; set; } = false;

		/// <summary>
		/// When added to a static method, adds it to the <see cref="Commands.Lookup"/> when it is initialized. WARNING: Will not function on non-static methods.
		/// </summary>
		/// <param name="key">The name of the command to match in the developer console.</param>
		public StaticCommandAttribute(string key)
		{
			Key = key.ToLower().Replace(' ', '_');
		}
	}
}