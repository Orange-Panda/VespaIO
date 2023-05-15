using UnityEngine;

namespace LMirman.VespaIO
{
	public static class DevConsole
	{
		/// <summary>
		/// Whether the console is currently enabled and open.
		/// </summary>
		public static bool ConsoleActive { get; set; }

		public static readonly NativeConsole console = new NativeConsole();

		static DevConsole()
		{
			console.CommandSet = Commands.commandSet;
			console.AliasSet = Aliases.aliasSet;
		}

		[RuntimeInitializeOnLoadMethod]
		private static void CreateConsole()
		{
			PrintWelcome();

#if UNITY_EDITOR
			console.Enabled = NativeSettings.Config.defaultConsoleEnableEditor;
#else
			console.Enabled = ConsoleSettings.Config.defaultConsoleEnableStandalone;
#endif

			if (NativeSettings.Config.instantiateConsoleOnLoad)
			{
				GameObject original = Resources.Load<GameObject>(NativeSettings.Config.consoleResourcePath);
				if (original != null)
				{
					Object.Instantiate(original, Vector3.zero, Quaternion.identity);
				}
				else
				{
					Debug.LogError($"Unable to instantiate console prefab from \"{NativeSettings.Config.consoleResourcePath}\". Please ensure that a prefab exists at this path.");
				}
			}
		}

		///<inheritdoc cref="Console.Log"/>
		public static void Log(string text, Console.LogStyling logStyling = Console.LogStyling.Plain)
		{
			console.Log(text, logStyling);
		}

		/// <summary>
		/// Log the <see cref="ConsoleSettingsConfig.welcomeText"/> to the <see cref="DevConsole"/>
		/// </summary>
		public static void PrintWelcome()
		{
			console.Log(NativeSettings.Config.welcomeText);

			if (NativeSettings.Config.printMetadataOnWelcome)
			{
				console.Log($"Version: {Application.version} {(Application.genuine ? "" : "[MODIFIED]")}\nUnity Version: {Application.unityVersion}\nPlatform: {Application.platform}");
			}
		}
	}
}