using System;
using UnityEngine;

namespace LMirman.VespaIO
{
	/// <summary>
	/// Load and reference the developer configuration for the console at runtime.
	/// </summary>
	public static class ConsoleSettings
	{
		internal static ConsoleSettingsConfig Config { get; private set; }
		public const string SettingsPath = "VespaIO/Settings";

		static ConsoleSettings()
		{
			ConsoleSettingsFile file = Resources.Load<ConsoleSettingsFile>(SettingsPath);
			if (file == null)
			{
				Config = new ConsoleSettingsConfig();
				Debug.LogWarning($"Unable to load console settings resource at {SettingsPath}. Please ensure such a file exists and is of type ConsoleSettingsConfig. Default settings will be used.\nGo to `Tools > Vespa IO > Select Console Settings` to automatically generate this asset.");
				return;
			}

			Config = file.DeepCopy();
		}
	}

	/// <summary>
	/// Stores configuration data for the <see cref="DevConsole"/>
	/// </summary>
	[Serializable]
	public class ConsoleSettingsConfig
	{
		[Header("Commands")]
		[Tooltip("The default state of the console enabled variable in the unity editor.")]
		public bool defaultConsoleEnableEditor = true;
		[Tooltip("The default state of the console enabled variable in a standalone build (i.e non-editor)")]
		public bool defaultConsoleEnableStandalone = true;
		[Tooltip("If a cheat command is used in the editor should cheats automatically be enabled?")]
		public bool editorAutoEnableCheats = false;
		[Tooltip("When should the commands be preloaded into memory? This will take some time depending on the size of your project and will only occur once per play session. Selecting none will load commands upon the first command input.")]
		public DevConsole.PreloadType preloadType;

		[Header("Console")]
		[Tooltip("When true will automatically create an instance of the console when the game starts.")]
		public bool instantiateConsoleOnLoad = true;
		[Tooltip("Where is the console template stored? Must be a path inside a resources folder.")]
		public string consoleResourcePath = "VespaIO/Console";
		[Range(0.1f, 4f)]
		public float consoleScale = 1f;

		[Header("History")]
		[Range(1, 256)]
		public int commandHistoryCapacity = 32;

		[Header("Input")]
		public KeyCode[] openConsoleKeycodes = new KeyCode[] { KeyCode.Tilde, KeyCode.BackQuote, KeyCode.Backslash, KeyCode.F10 };
		public KeyCode[] closeConsoleKeycodes = new KeyCode[] { KeyCode.Tilde, KeyCode.BackQuote, KeyCode.Backslash, KeyCode.F10, KeyCode.Escape };
		public bool closeConsoleOnLeftClick = true;

		[Header("Welcome")]
		[Tooltip("When true the welcome message will include the application version, unity version, and other common metadata about the game instance.")]
		public bool printMetadataOnWelcome = true;
		[Tooltip("The text shown for the welcome message.")]
		[TextArea(3, 32)]
		public string welcomeText = "Welcome to the developer console!\nPress the tilde key (` or ~) to return to your game!\nBeware: Use of console commands can dramatically alter the gameplay experience. Use at your own discretion.";

		[Header("Editor")]
		[Tooltip("When true gives an error message if a non-static method is labeled with the StaticCommand attribute. Disabling this will significantly reduce the amount of time spent generating the command lookup dictionary but will not alert you when a StaticCommand is invalid.")]
		public bool warnForNonstaticMethods = true;

		public ConsoleSettingsConfig DeepCopy() => new ConsoleSettingsConfig
		{
			defaultConsoleEnableEditor = defaultConsoleEnableEditor,
			defaultConsoleEnableStandalone = defaultConsoleEnableStandalone,
			editorAutoEnableCheats = editorAutoEnableCheats,
			preloadType = preloadType,
			instantiateConsoleOnLoad = instantiateConsoleOnLoad,
			consoleResourcePath = consoleResourcePath,
			consoleScale = consoleScale,
			commandHistoryCapacity = commandHistoryCapacity,
			openConsoleKeycodes = openConsoleKeycodes,
			closeConsoleKeycodes = closeConsoleKeycodes,
			closeConsoleOnLeftClick = closeConsoleOnLeftClick,
			printMetadataOnWelcome = printMetadataOnWelcome,
			welcomeText = welcomeText,
			warnForNonstaticMethods = warnForNonstaticMethods
		};
	}

	/// <summary>
	/// Asset that will serialize the <see cref="ConsoleSettingsConfig"/> to the disk.
	/// </summary>
	public class ConsoleSettingsFile : ScriptableObject
	{
		[SerializeField]
		private ConsoleSettingsConfig config;

		/// <summary>
		/// Create a deep copy of the configuartion allowing it to be mutated in memory without modifying the asset's config.
		/// </summary>
		public ConsoleSettingsConfig DeepCopy() => config.DeepCopy();
	}
}