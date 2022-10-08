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

		internal static readonly StringBuilder output = new StringBuilder();

		/// <summary>
		/// Invoked when something is logged into the console output or if it is cleared.
		/// </summary>
		internal static event Action OutputUpdate = delegate { };
		internal static LinkedList<string> recentCommands = new LinkedList<string>();

		public static void ProcessInput(string submitText)
		{
			if (submitText == null)
			{
				Log("<color=red>Error:</color> Input command was null.");
				return;
			}
			
			// Add the command to history if it was not the most recent command sent.
			if ((recentCommands.Count <= 0 || !recentCommands.First.Value.Equals(submitText)) && !string.IsNullOrWhiteSpace(submitText))
			{
				recentCommands.AddFirst(submitText);
			}

			// Restrict list to certain capacity
			while (recentCommands.Count > 0 && recentCommands.Count > ConsoleSettings.Config.commandHistoryCapacity)
			{
				recentCommands.RemoveLast();
			}

			if (!ConsoleEnabled)
			{
				Log("<color=red>Error:</color> Console is not enabled.");
				return;
			}

			SplitInput(submitText);
		}

		private static void SplitInput(string submitText)
		{
			// If the command doesn't even have a semicolon we can immediately submit it
			if (!submitText.Contains(";"))
			{
				ProcessCommand(submitText);
				return;
			}

			List<string> inputs = new List<string>();
			bool inQuote = false;
			int escapeCount = 0;
			int substringStart = 0;
			int substringLength = 0;
			for (int i = 0; i < submitText.Length; i++)
			{
				// Group quotes into a single command.
				if (submitText[i] == '\"' && escapeCount % 2 == 0)
				{
					inQuote = !inQuote;
				}
				
				// If we encounter an unescaped semicolon, begin a new command.
				if (submitText[i] == ';' && escapeCount % 2 == 0 && !inQuote)
				{
					// This split has no content. Skip it.
					if (substringLength == 0)
					{
						substringStart = i + 1;
						continue;
					}

					inputs.Add(submitText.Substring(substringStart, substringLength));
					substringStart = i + 1;
					substringLength = 0;
					continue;
				}

				escapeCount = submitText[i] == '\\' ? escapeCount + 1 : 0;
				substringLength++;
			}

			// Add the last command input
			if (substringLength > 0)
			{
				inputs.Add(submitText.Substring(substringStart, substringLength));
			}

			// Process each split input
			foreach (string command in inputs)
			{
				ProcessCommand(command.Replace("\\;", ";"));
			}
		}

		private static void ProcessCommand(string commandText)
		{
			// Remove leading space
			commandText = commandText.TrimStart(' ');
			
			// Parse input into useful variables
			if (!TryParseCommand(commandText, out string commandName, out object[] args, out LongString longString))
			{
				Log("<color=red>Error:</color> Invalid command syntax");
				return;
			}

			// Find the command in the Commands lookup table
			if (!Commands.Lookup.TryGetValue(commandName, out Command command))
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

			// Filter out methods in the command that have more required parameters than the user has input
			List<MethodInfo> validMethods = new List<MethodInfo>();
			for (int i = 0; i < command.Methods.Count; i++)
			{
				MethodInfo method = command.Methods[i];
				ParameterInfo[] parameters = method.GetParameters();
				int requiredParams = 0;

				for (int j = 0; j < parameters.Length; j++)
				{
					if (!parameters[j].IsOptional)
					{
						requiredParams++;
					}
				}

				if (args.Length >= requiredParams)
				{
					validMethods.Add(method);
				}
			}

			// Exit if no methods are valid for the parameters provided by the user
			if (validMethods.Count <= 0)
			{
				if (args.Length > 0)
				{
					Log("<color=red>Error:</color> Not enough arguments provided for command");
				}

				LogCommandHelp(command);
				return;
			}

			// Get the best method from the arguments
			// The best method is the one with all parameters of the same type and the most parameters matched
			MethodInfo longStringMethod = null;
			MethodInfo bestMethod = null;
			int bestMethodArgCount = -1;
			for (int i = 0; i < validMethods.Count; i++)
			{
				MethodInfo method = validMethods[i];
				ParameterInfo[] parameters = method.GetParameters();
				bool canCastAll = true;
				for (int j = 0; j < parameters.Length && j < args.Length; j++)
				{
					if (parameters[j].ParameterType != args[j].GetType())
					{
						if (parameters[j].ParameterType == typeof(float) && args[j] is int)
						{
							// Since an int argument can be implicitly converted to a float we do so here to ensure a method looking for it can use it.
							int intObject = (int)args[j];
							args[j] = (float)intObject;
						}
						else
						{
							canCastAll = false;
						}
					}
				}

				if (canCastAll && parameters.Length > bestMethodArgCount)
				{
					bestMethod = method;
					bestMethodArgCount = parameters.Length;
				}

				if (parameters.Length == 1 && parameters[0].ParameterType == typeof(LongString))
				{
					longStringMethod = method;
				}
			}

			// Execute the best method found in the previous step, if one is found
			if (longStringMethod != null && !string.IsNullOrWhiteSpace(longString) && (bestMethod == null || bestMethodArgCount == 0))
			{
				longStringMethod.Invoke(null, new object[] { longString });
			}
			else if (bestMethod != null)
			{
				if (args.Length < bestMethodArgCount)
				{
					object[] newArgs = new object[bestMethodArgCount];
					ParameterInfo[] parameters = bestMethod.GetParameters();
					for (int i = 0; i < bestMethodArgCount; i++)
					{
						newArgs[i] = i < args.Length ? args[i] : parameters[i].DefaultValue;
					}

					args = newArgs;
				}
				else if (args.Length > bestMethodArgCount)
				{
					object[] newArgs = new object[bestMethodArgCount];
					for (int i = 0; i < bestMethodArgCount; i++)
					{
						newArgs[i] = args[i];
					}

					args = newArgs;
				}

				bestMethod.Invoke(null, args);
			}
			// No methods support the user input parameters.
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
		private static bool TryParseCommand(string input, out string commandName, out object[] args, out LongString longString)
		{
			try
			{
				List<string> splitInput = SplitIntoArgs(input);
				commandName = splitInput[0].ToLower();
				List<object> foundArgs = new List<object>();
				for (int i = 1; i < splitInput.Count; i++)
				{
					if (!string.IsNullOrWhiteSpace(splitInput[i]))
					{
						foundArgs.Add(ParseObject(splitInput[i]));
					}
				}
				args = foundArgs.ToArray();
				longString = foundArgs.Count > 0 ? (LongString)input.Remove(0, commandName.Length + 1) : LongString.Empty;
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

		private static List<string> SplitIntoArgs(string input)
		{
			List<string> splitInput = new List<string>();
			bool inQuote = false;
			bool hasEscapedQuote = false;
			int escapeCount = 0;
			int substringStart = 0;
			int substringLength = 0;
			for (int i = 0; i < input.Length; i++)
			{
				// If we encounter an unescaped quote mark, toggle quote mode.
				if (input[i] == '"' && escapeCount % 2 == 0)
				{
					inQuote = !inQuote;
				}
				else if (input[i] == '"' && escapeCount % 2 == 1)
				{
					hasEscapedQuote = true;
				}

				// If we encounter a space and are not in quote mode, begin a new split.
				if (input[i] == ' ' && !inQuote)
				{
					splitInput.Add(GetSubstring());
					substringStart = i + 1;
					substringLength = 0;
					hasEscapedQuote = false;
					continue;
				}

				escapeCount = input[i] == '\\' ? escapeCount + 1 : 0;
				substringLength++;
			}

			// Add the last command input
			if (substringLength > 0)
			{
				splitInput.Add(GetSubstring());
			}

			return splitInput;

			string GetSubstring()
			{
				string value = input.Substring(substringStart, substringLength);
				if (!value.Contains("\"")) // There were no quote characters so we don't have to remove them.
				{
					return value.Replace("\\\\", "\\");
				}
				if (!hasEscapedQuote) // There we no unescaped quotes so we can easily just remove them all without checking for escape characters
				{
					return value.Replace("\"", string.Empty).Replace("\\\\", "\\");
				}
				else // The worst case scenario: There are some escaped quotes that we have to manually parse.
				{
					for (int i = 0; i < value.Length - 1; i++) // Shift goal by -1 because we need to look at the next character
					{
						if (value[i] == '\\' && (value[i + 1] == '\\' || value[i + 1] == '\"'))
						{
							value = value.Remove(i, 1);
						}
					}
					return value;
				}
			}
		}

		private static object ParseObject(string arg) => int.TryParse(arg, out int intValue) ? (object)intValue : float.TryParse(arg, out float floatValue) ? (object)floatValue : (object)arg;

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
			output.Clear();
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
				output.AppendLine();
			}
			output.Append(message);
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