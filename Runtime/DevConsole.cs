using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace LMirman.VespaIO
{
	public static class DevConsole
	{
		/// <summary>
		/// Whether the console is currently enabled and open.
		/// </summary>
		public static bool ConsoleActive { get; internal set; }
		/// <summary>
		/// Whether the console is allowed to be opened or utilized.
		/// </summary>
		public static bool ConsoleEnabled { get; set; }

		/// <summary>
		/// True when the user has enabled cheats, false if they have not yet.
		/// </summary>
		/// <remarks>By default cheats can not be disabled at any point during an active session. Therefore to prevent players exploiting the cheat commands you can check for cheats enabled to prevent saving game data to disk, granting achievements, sending telemetry data, etc.</remarks>
		public static bool CheatsEnabled { get; internal set; }

		internal static readonly StringBuilder Output = new StringBuilder();

		/// <summary>
		/// Invoked when something is logged into the console output or if it is cleared.
		/// </summary>
		internal static event Action OutputUpdate = delegate { };
		internal static readonly LinkedList<string> RecentCommands = new LinkedList<string>();

		public static void ProcessInput(string submitText)
		{
			if (submitText == null)
			{
				Log("<color=red>Error:</color> Input command was null.");
				return;
			}
			
			// Add the command to history if it was not the most recent command sent.
			if ((RecentCommands.Count <= 0 || !RecentCommands.First.Value.Equals(submitText)) && !string.IsNullOrWhiteSpace(submitText))
			{
				RecentCommands.AddFirst(submitText);
			}

			// Restrict list to certain capacity
			while (RecentCommands.Count > 0 && RecentCommands.Count > ConsoleSettings.Config.commandHistoryCapacity)
			{
				RecentCommands.RemoveLast();
			}

			if (!ConsoleEnabled)
			{
				Log("<color=red>Error:</color> Console is not enabled.");
				return;
			}

			List<string> preAliasInputs = Invocation.SplitStringBySemicolon(submitText);
			foreach (string preAliasInput in preAliasInputs)
			{
				Invocation.AliasOutcome aliasOutcome = Invocation.SubstituteAliasForCommand(preAliasInput, out string aliasCommandOutput);
				switch (aliasOutcome)
				{
					case Invocation.AliasOutcome.NoChange:
						ProcessCommand(preAliasInput);
						break;
					case Invocation.AliasOutcome.AliasApplied:
						Log($"<color=yellow>></color> {aliasCommandOutput}");
						List<string> postAliasInputs = Invocation.SplitStringBySemicolon(aliasCommandOutput);
						foreach (string postAliasInput in postAliasInputs)
						{
							ProcessCommand(postAliasInput);
						}
						break;
					case Invocation.AliasOutcome.CommandConflict:
						Log($"<color=orange>Alert:</color> There is an alias defined at \"{aliasCommandOutput}\" but there is already a command with the same name. The command is given priority so you are encouraged to remove your alias.");
						ProcessCommand(preAliasInput);
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
		}

		private static void ProcessCommand(string commandText)
		{
			// Remove leading space
			commandText = commandText.TrimStart(' ');
			
			// Parse input into useful variables
			if (!TryParseCommand(commandText, out string commandName, out Argument[] args, out LongString longString))
			{
				Log("<color=red>Error:</color> Invalid command syntax");
				return;
			}

			// Find the command in the Commands lookup table
			if (!Commands.TryGetCommand(commandName, out Command command))
			{
				Log($"<color=red>Error:</color> Unrecognized command \"{commandName}\"");
				return;
			}

#if UNITY_EDITOR
			// Automatically enable cheats if configured to do so in the config, making quick debugging more convenient when enabled.
			if (command.Cheat && !CheatsEnabled && ConsoleSettings.Config.editorAutoEnableCheats)
			{
				Log("<color=yellow>Cheats have automatically been enabled.</color>");
				CheatsEnabled = true;
			}
#endif

			// Test the command found that it can be executed if it is a cheat
			if (command.Cheat && !CheatsEnabled)
			{
				Log("<color=red>Error:</color> Command provided can only be used when cheats are enabled");
				return;
			}

			if (command.TryGetMethod(args, longString, out MethodInfo methodInfo, out object[] methodParameters))
			{
				methodInfo.Invoke(null, methodParameters);
			}
			else
			{
				Log("<color=red>Error:</color> Invalid arguments provided for command");
				LogCommandHelp(command);
			}
		}

		/// <summary>
		/// Parse the user input into convenient variables for the console to handle.
		/// </summary>
		/// <param name="input">The user input to parse</param>
		/// <param name="commandName">The name of the command that the user has input</param>
		/// <param name="args">None to many long array of the arguments provided by the user.</param>
		/// <param name="longString">All parameters provided by the user in a single spaced string.</param>
		/// <remarks>All output variables will be null if the parsing failed.</remarks>
		/// <returns>True if the input was parsed properly, false if the parsing failed.</returns>
		private static bool TryParseCommand(string input, out string commandName, out Argument[] args, out LongString longString)
		{
			try
			{
				// Preprocess for alias command
				List<string> splitInput = Invocation.GetWordsFromString(input);
				commandName = splitInput[0].ToLower();
				List<Argument> foundArgs = new List<Argument>();
				for (int i = 1; i < splitInput.Count; i++)
				{
					if (!string.IsNullOrWhiteSpace(splitInput[i]))
					{
						foundArgs.Add(new Argument(splitInput[i]));
					}
				}
				args = foundArgs.ToArray();
				longString = foundArgs.Count > 0 ? (LongString)input.Substring(commandName.Length) : LongString.Empty;
				return true;
			}
			catch
			{
				args = null;
				commandName = null;
				longString = null;
				return false;
			}
		}

		[RuntimeInitializeOnLoadMethod]
		private static void CreateConsole()
		{
			PrintWelcome();

#if UNITY_EDITOR
			ConsoleEnabled = ConsoleSettings.Config.defaultConsoleEnableEditor;
#else
			ConsoleEnabled = ConsoleSettings.Config.defaultConsoleEnableStandalone;
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
					UnityEngine.Object.Instantiate(original, Vector3.zero, Quaternion.identity);
				}
				else
				{
					Debug.LogError($"Unable to instantiate console prefab from \"{ConsoleSettings.Config.consoleResourcePath}\". Please ensure that a prefab exists at this path.");
				}
			}
		}

		/// <summary>
		/// Clear the output of the console.
		/// </summary>
		public static void Clear()
		{
			Output.Clear();
			OutputUpdate.Invoke();
		}

		/// <summary>
		/// Log a message to the console.
		/// </summary>
		/// <param name="message">The message to log to the console.</param>
		/// <param name="startWithNewLine">When true start a new line before logging the message.</param>
		public static void Log(string message, bool startWithNewLine = true)
		{
			if (startWithNewLine)
			{
				Output.AppendLine();
			}
			Output.Append(message);
			OutputUpdate.Invoke();
		}

		public static void LogCommandHelp(Command command)
		{
			Log($"{command.Key} \"{command.Name}\"\n{command.Guide}");
		}

		/// <summary>
		/// Log the <see cref="ConsoleSettingsConfig.welcomeText"/> to the <see cref="DevConsole"/>
		/// </summary>
		public static void PrintWelcome()
		{
			Log(ConsoleSettings.Config.welcomeText);

			if (ConsoleSettings.Config.printMetadataOnWelcome)
			{
				Log($"Version: {Application.version} {(Application.genuine ? "" : "[MODIFIED]")}\nUnity Version: {Application.unityVersion}\nPlatform: {Application.platform}");
			}
		}

		public enum PreloadType
		{
			None, ConsoleOpen, ConsoleStart, RuntimeStart
		}
	}
}