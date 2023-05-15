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
		[Header("Dev Console")]
		[Tooltip("The default state of the console enabled variable in the unity editor.")]
		public bool defaultConsoleEnableEditor = true;
		[Tooltip("The default state of the console enabled variable in a standalone build (i.e non-editor)")]
		public bool defaultConsoleEnableStandalone = true;
		[Tooltip("If a cheat command is used in the editor should cheats automatically be enabled?")]
		public bool editorAutoEnableCheats;
		[Tooltip("The method by which assemblies are picked for command selection")]
		public Commands.AssemblyFilter assemblyFilter = Commands.AssemblyFilter.Standard;

		[Header("Instantiate On Load")]
		[Tooltip("When true will automatically create an instance of the console when the game starts.")]
		public bool instantiateConsoleOnLoad = true;
		[Tooltip("Where is the console template stored? Must be a path inside a resources folder.")]
		public string consoleResourcePath = "VespaIO/Console";
		
		[Header("Welcome Text")]
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
			printMetadataOnWelcome = printMetadataOnWelcome,
			welcomeText = welcomeText
		};
	}
}