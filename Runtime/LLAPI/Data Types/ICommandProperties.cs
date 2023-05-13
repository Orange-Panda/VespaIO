namespace LMirman.VespaIO
{
	public interface ICommandProperties
	{
		/// <summary>
		/// The key that points to this command in console.
		/// </summary>
		/// <remarks>
		/// This is immutable to avoid conflicts with other command definitions and to preserve key definitions in command dictionaries that contain this entry.
		/// </remarks>
		public string Key { get; }

		/// <summary>
		/// The name or title of the command in plain english.
		/// </summary>
		/// <example>
		/// Good command titles might be:<br/>
		/// - "Print Debug Information"<br/>
		/// - "Grant Item to Player"<br/>
		/// - "Play Mission"<br/>
		/// - "Teleport Player to Location"
		/// </example>
		/// <remarks>
		/// Not to be confused with <see cref="Key"/> which is the unique identifier for this command.<br/>
		/// If no name is provided by a command definition will default to the first method name found.
		/// </remarks>
		public string Name { get; }

		/// <summary>
		/// The description of the command.
		/// </summary>
		/// <example>
		/// Good command descriptions might be:<br/>
		/// - "Prints information about the system playing the game"<br/>
		/// - "Immediately grant an item to the player inventory"<br/>
		/// - "Set and play a particular mission for the loaded save file"<br/>
		/// - "Immediately teleport the player to the provided location"<br/>
		/// </example>
		/// <remarks>
		/// Should be a brief explanation of the functionality of the command without any detailed explanation of functionality.
		/// </remarks>
		public string Description { get; }

		/// <summary>
		/// If this command is a cheat command.<br/><br/>
		/// When true the command will be omitted from help and autofill while cheats are not enabled in addition to be unable to be executed in the console until enabling cheats.
		/// </summary>
		/// <example>
		/// Cheat commands are likely to be commands that execute gameplay impacting code not otherwise accessible such as:<br/>
		/// - A command that gives the player free items, experience, or other progression mechanics<br/>
		/// - A command that manipulates the player functionality<br/>
		/// - A command that creates targets or objects in the scene
		/// </example>
		/// <remarks>
		/// If any single method of the command is marked as a cheat the entire command is considered a cheat.
		/// Thus, it is not possible to provide non-cheat functionality to a cheat command directly and must be added as a separate command.
		/// <br/><br/>
		/// Note: Due to the nature of code still existing on the end user's system there is no absolute guarantee that the user never executes Cheat commands out of context.<br/>
		/// Therefore it is recommended that you consider the following:<br/>
		/// - Only write commands that you consider acceptable for users to execute (i.e avoid including commands that cheat on a server)<br/>
		/// - Implement platform directives such as `#if UNITY_EDITOR` around cheat commands you never want accessible to the end user<br/>
		/// - Understand the fact that players may modify code, especially JIT code, and execute code that you don't intend them to.<br/>
		/// </remarks>
		public bool Cheat { get; }

		/// <summary>
		/// If this command should be accessible by auto-fill and help functionalities of the console.<br/><br/>
		/// When true will be omitted from being displayed in such sections.
		/// </summary>
		/// <example>
		/// Hidden commands are likely to be commands that are easter eggs or commands that are not useful to the end user such as:<br/>
		/// - A command that changes the skin of the UI for fun without any gameplay consequences<br/>
		/// - A command that plays a sound effect<br/>
		/// - A command that shows a secret message to the user
		/// </example>
		/// <remarks>
		/// Hidden commands do not have any impact on boundaries on their execution what so over.
		/// Assuming the user knows the command exists they can execute it as if it were not hidden.
		/// </remarks>
		public bool Hidden { get; }

		/// <summary>
		/// The priority of this command relative to other commands.
		/// The higher this value, the earlier this command should appear in a sorted list of commands.
		/// </summary>
		/// <example>
		/// Commands that are particularly useful or frequently used may justify a high priority such as:<br/>
		/// - A cheat that teleports the player<br/>
		/// - A cheat that grants items<br/>
		/// - A command that sets volume<br/>
		/// Commands that are niche or rarely useful may justify a negative priority such as:<br/>
		/// - A cheat that only works in a specific level<br/>
		/// - A command that sets an internal value that only developers or testers would care about<br/>
		/// </example>
		/// <remarks>
		/// Native commands will only ever use the 0 to 127 inclusive range.
		/// If you want your commands to always sort before or after native commands consider these values.
		/// </remarks>
		public int ManualPriority { get; }
	}
}