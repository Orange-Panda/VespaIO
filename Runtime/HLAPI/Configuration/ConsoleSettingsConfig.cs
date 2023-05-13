using System;
using UnityEngine;

namespace LMirman.VespaIO
{
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
		public bool editorAutoEnableCheats;
		[Tooltip("The method by which assemblies are picked for command selection")]
		public Commands.AssemblyFilter assemblyFilter = Commands.AssemblyFilter.Standard;

		[Header("Console")]
		[Tooltip("When true will automatically create an instance of the console when the game starts.")]
		public bool instantiateConsoleOnLoad = true;
		[Tooltip("Where is the console template stored? Must be a path inside a resources folder.")]
		public string consoleResourcePath = "VespaIO/Console";
		[Range(0.1f, 4f)]
		public float consoleScale = 1f;

		[Header("Input")]
		[Tooltip("When true will require a key to be held to open/close the console.")]
		public bool requireHeldKeyToToggle;
		[Tooltip("When 'requireHeldKeyForInput' is enabled: require one of these keys to be held to open/close the console.")]
		public KeyCode[] inputWhileHeldKeycodes = new [] { KeyCode.LeftControl, KeyCode.RightControl, KeyCode.LeftCommand, KeyCode.RightCommand };
		[Space]
		public KeyCode[] openConsoleKeycodes = new [] { KeyCode.Tilde, KeyCode.BackQuote, KeyCode.Backslash, KeyCode.F10 };
		[Tooltip("Close the console when these are pressed only if the console is empty.")]
		public KeyCode[] closeEmptyConsoleKeycodes = new[] { KeyCode.Tilde, KeyCode.BackQuote, KeyCode.Backslash };
		[Tooltip("Close the console when these are pressed, regardless of if there is a pending input.")]
		public KeyCode[] closeAnyConsoleKeycodes = new [] { KeyCode.F10, KeyCode.Escape };
		public bool closeConsoleOnLeftClick = true;

		[Header("Welcome")]
		[Tooltip("When true the welcome message will include the application version, unity version, and other common metadata about the game instance.")]
		public bool printMetadataOnWelcome = true;
		[Tooltip("The text shown for the welcome message.")]
		[TextArea(3, 32)]
		public string welcomeText = "Welcome to the developer console!\nPress the tilde key (` or ~) to return to your game!\nBeware: Use of console commands can dramatically alter the gameplay experience. Use at your own discretion.";

		public ConsoleSettingsConfig DeepCopy() => new ConsoleSettingsConfig
		{
			defaultConsoleEnableEditor = defaultConsoleEnableEditor,
			defaultConsoleEnableStandalone = defaultConsoleEnableStandalone,
			editorAutoEnableCheats = editorAutoEnableCheats,
			assemblyFilter = assemblyFilter,
			instantiateConsoleOnLoad = instantiateConsoleOnLoad,
			consoleResourcePath = consoleResourcePath,
			consoleScale = consoleScale,
			openConsoleKeycodes = openConsoleKeycodes,
			closeAnyConsoleKeycodes = closeAnyConsoleKeycodes,
			inputWhileHeldKeycodes = inputWhileHeldKeycodes,
			requireHeldKeyToToggle = requireHeldKeyToToggle,
			closeEmptyConsoleKeycodes = closeEmptyConsoleKeycodes,
			closeConsoleOnLeftClick = closeConsoleOnLeftClick,
			printMetadataOnWelcome = printMetadataOnWelcome,
			welcomeText = welcomeText
		};
	}
}