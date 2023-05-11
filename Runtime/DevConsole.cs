using System;
using System.Collections.Generic;
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
			RecordCommandInHistory(submitText);

			if (string.IsNullOrWhiteSpace(submitText))
			{
				Log("<color=red>Error:</color> Input command was null or empty.");
				return;
			}

			if (!ConsoleEnabled)
			{
				Log("<color=red>Error:</color> Console is not enabled.");
				return;
			}

			List<string> preAliasInputs = VespaFunctions.SplitStringBySemicolon(submitText);
			foreach (string preAliasInput in preAliasInputs)
			{
				VespaFunctions.AliasOutcome aliasOutcome = VespaFunctions.SubstituteAliasForCommand(preAliasInput, out string aliasCommandOutput);
				switch (aliasOutcome)
				{
					case VespaFunctions.AliasOutcome.NoChange:
						ProcessCommand(preAliasInput);
						break;
					case VespaFunctions.AliasOutcome.AliasApplied:
						Log($"<color=yellow>></color> {aliasCommandOutput}");
						List<string> postAliasInputs = VespaFunctions.SplitStringBySemicolon(aliasCommandOutput);
						foreach (string postAliasInput in postAliasInputs)
						{
							ProcessCommand(postAliasInput);
						}

						break;
					case VespaFunctions.AliasOutcome.CommandConflict:
						Log(
							$"<color=orange>Alert:</color> There is an alias defined at \"{aliasCommandOutput}\" but there is already a command with the same name. The command is given priority so you are encouraged to remove your alias.");
						ProcessCommand(preAliasInput);
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
		}

		private static void RecordCommandInHistory(string submitText)
		{
			// Add the command to history if it was not the most recent command sent.
			if (!string.IsNullOrWhiteSpace(submitText) && (RecentCommands.Count <= 0 || !RecentCommands.First.Value.Equals(submitText)))
			{
				RecentCommands.AddFirst(submitText);
			}

			// Restrict list to certain capacity
			while (RecentCommands.Count > Mathf.Max(ConsoleSettings.Config.commandHistoryCapacity, 0))
			{
				RecentCommands.RemoveLast();
			}
		}

		private static void ProcessCommand(string commandText)
		{
			Invocation invocation = new Invocation(commandText);
			switch (invocation.validState)
			{
				case Invocation.ValidState.Valid:
					RunInvocation(invocation);
					break;
				case Invocation.ValidState.Unspecified:
					Log("<color=red>Error:</color> An internal error occurred.");
					break;
				case Invocation.ValidState.ErrorException:
					Log("<color=red>Error:</color> An internal error occurred.");
					break;
				case Invocation.ValidState.ErrorEmpty:
					Log("<color=red>Error:</color> The provided command was empty or invalid.");
					break;
				case Invocation.ValidState.ErrorNoCommandFound:
					Log($"<color=red>Error:</color> Unrecognized command \"{invocation.inputKey}\"");
					break;
				case Invocation.ValidState.ErrorNoMethodForParameters:
					Log("<color=red>Error:</color> Invalid arguments provided for command");
					LogCommandHelp(invocation.command);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public static void RunInvocation(Invocation invocation)
		{
#if UNITY_EDITOR
			// Automatically enable cheats if configured to do so in the config, making quick debugging more convenient when enabled.
			if (invocation.command.Cheat && !CheatsEnabled && ConsoleSettings.Config.editorAutoEnableCheats)
			{
				Log("<color=yellow>Cheats have automatically been enabled.</color>");
				CheatsEnabled = true;
			}
#endif

			Invocation.InvokeResult invokeResult = invocation.RunInvocation(out Exception exception);
			switch (invokeResult)
			{
				case Invocation.InvokeResult.Success:
					break;
				case Invocation.InvokeResult.Exception:
					Log("<color=red>Error:</color> An internal error occurred while running an invocation.");
					Log(exception.Message);
					break;
				case Invocation.InvokeResult.ErrorInvocationWasInvalid:
					Log("<color=red>Error:</color> Tried to run an invalid invocation.");
					break;
				case Invocation.InvokeResult.ErrorRequiresCheats:
					Log("<color=red>Error:</color> Command provided can only be used when cheats are enabled");
					break;
				default:
					throw new ArgumentOutOfRangeException();
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