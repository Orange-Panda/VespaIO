using UnityEngine;

namespace LMirman.VespaIO
{
	public static class DevConsole
	{
		/// <summary>
		/// Whether the console is currently enabled and open.
		/// </summary>
		public static bool ConsoleActive { get; internal set; }
		
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
			console.Enabled = ConsoleSettings.Config.defaultConsoleEnableEditor;
#else
			console.Enabled = ConsoleSettings.Config.defaultConsoleEnableStandalone;
#endif

			if (ConsoleSettings.Config.preloadType == PreloadType.RuntimeStart)
			{
				Commands.PreloadLookup();
			}

			if (ConsoleSettings.Config.instantiateConsoleOnLoad)
			{
				GameObject original = Resources.Load<GameObject>(ConsoleSettings.Config.consoleResourcePath);
				if (original != null)
				{
					Object.Instantiate(original, Vector3.zero, Quaternion.identity);
				}
				else
				{
					Debug.LogError($"Unable to instantiate console prefab from \"{ConsoleSettings.Config.consoleResourcePath}\". Please ensure that a prefab exists at this path.");
				}
			}
		}

		public static void Log(string text, Console.LogStyling logStyling = Console.LogStyling.Plain)
		{
			console.Log(text, logStyling);
		}

		/// <summary>
		/// Log the <see cref="ConsoleSettingsConfig.welcomeText"/> to the <see cref="DevConsole"/>
		/// </summary>
		public static void PrintWelcome()
		{
			console.Log(ConsoleSettings.Config.welcomeText);

			if (ConsoleSettings.Config.printMetadataOnWelcome)
			{
				console.Log($"Version: {Application.version} {(Application.genuine ? "" : "[MODIFIED]")}\nUnity Version: {Application.unityVersion}\nPlatform: {Application.platform}");
			}
		}

		public enum PreloadType
		{
			None, ConsoleOpen, ConsoleStart, RuntimeStart
		}
	}
}