using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Text;

namespace LMirman.VespaIO
{
	[PublicAPI]
	public class Console
	{
		protected int inputHistoryCapacity = 16;
		protected int outputCapacity = 8192;
		protected bool cheatsEnabled;

		/// <summary>
		/// True if this console is allowed to operate at all, false if it is prohibited from operating.
		/// </summary>
		public bool Enabled { get; set; }

		public CommandSet CommandSet { get; set; }
		public AliasSet AliasSet { get; set; }

		/// <summary>
		/// True when this console allows cheat commands to be run, false if it does not.
		/// </summary>
		public virtual bool CheatsEnabled => cheatsEnabled;

		/// <summary>
		/// Invoked when something is logged into the console output or if it is cleared.
		/// </summary>
		public event Action OutputUpdate = delegate { };

		public readonly List<string> recentInputs = new List<string>(32);
		protected readonly StringBuilder output = new StringBuilder(16384);
		protected string outputLog = string.Empty;
		protected bool outputDirty;

		#region Core
		public void RunInput(string input, bool silent = false)
		{
			if (!silent)
			{
				Log($"> {input}");
			}

			RecordInputInHistory(input);

			if (string.IsNullOrWhiteSpace(input))
			{
				Log("Input command was null or empty", LogStyling.Error);
				return;
			}

			if (!Enabled)
			{
				Log("This console is not enabled.", LogStyling.Error);
				return;
			}

			List<string> preAliasInputs = VespaFunctions.SplitStringBySemicolon(input);
			foreach (string preAliasInput in preAliasInputs)
			{
				VespaFunctions.AliasOutcome aliasOutcome = VespaFunctions.SubstituteAliasForCommand(preAliasInput, CommandSet, AliasSet, out string aliasCommandOutput);
				switch (aliasOutcome)
				{
					case VespaFunctions.AliasOutcome.NoChange:
						RunCommand(preAliasInput);
						break;
					case VespaFunctions.AliasOutcome.AliasApplied:
						Log($"<color=yellow>></color> {aliasCommandOutput}");
						List<string> postAliasInputs = VespaFunctions.SplitStringBySemicolon(aliasCommandOutput);
						foreach (string postAliasInput in postAliasInputs)
						{
							RunCommand(postAliasInput);
						}

						break;
					case VespaFunctions.AliasOutcome.CommandConflict:
						Log($"There is an alias defined at \"{aliasCommandOutput}\" but there is already a command with the same name. " +
							"The command is given priority so you are encouraged to remove your alias.", LogStyling.Warning);
						RunCommand(preAliasInput);
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
		}

		/// <summary>
		/// Run a command message.
		/// </summary>
		/// <remarks>
		/// Unlike <see cref="RunInput"/> will <b>not</b> separate by semicolons or replace aliases.
		/// </remarks>
		public void RunCommand(string commandText)
		{
			Invocation invocation = new Invocation(commandText, CommandSet);
			switch (invocation.validState)
			{
				case Invocation.ValidState.Valid:
					RunInvocation(invocation);
					break;
				case Invocation.ValidState.Unspecified:
					Log("An internal error occurred.", LogStyling.Critical);
					break;
				case Invocation.ValidState.ErrorException:
					Log("An exception occurred during invocation creation.", LogStyling.Error);
					break;
				case Invocation.ValidState.ErrorEmpty:
					Log("The provided command was empty or invalid.", LogStyling.Error);
					break;
				case Invocation.ValidState.ErrorNoCommandFound:
					Log($"Unrecognized command \"{invocation.inputKey}\"", LogStyling.Error);
					break;
				case Invocation.ValidState.ErrorNoMethodForParameters:
					Log("Invalid arguments provided for command.", LogStyling.Error);
					Log(invocation.command.Guide);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public virtual void RunInvocation(Invocation invocation)
		{
			Invocation.InvokeResult invokeResult = invocation.RunInvocation(this, out Exception exception);
			switch (invokeResult)
			{
				case Invocation.InvokeResult.Success:
					break;
				case Invocation.InvokeResult.ErrorException:
					Log("An internal error occurred while running an invocation.", LogStyling.Error);
					Log(exception.Message);
					break;
				case Invocation.InvokeResult.ErrorInvocationWasInvalid:
					Log("Tried to run an invalid invocation.", LogStyling.Error);
					break;
				case Invocation.InvokeResult.ErrorRequiresCheats:
					Log("Command provided can only be used when cheats are enabled", LogStyling.Error);
					break;
				case Invocation.InvokeResult.ErrorConsoleInactive:
					Log("Attempt to run command but the console is not enabled", LogStyling.Error);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private void RecordInputInHistory(string submitText)
		{
			// Add the command to history if it was not the most recent command sent.
			if (!string.IsNullOrWhiteSpace(submitText) && (recentInputs.Count <= 0 || !recentInputs[0].Equals(submitText)))
			{
				recentInputs.Insert(0, submitText);
			}

			// Restrict list to certain capacity
			while (recentInputs.Count > Math.Max(inputHistoryCapacity, 0))
			{
				recentInputs.RemoveAt(recentInputs.Count - 1);
			}
		}
		#endregion

		public void Clear()
		{
			output.Clear();
			NotifyOutputUpdate();
		}

		/// <summary>
		/// Log a message on a new line to the console.
		/// </summary>
		/// <remarks>
		/// Begins a new line before adding the message.
		/// Consider using <see cref="LogAppend"/> if you don't want to log this on a new line.
		/// </remarks>
		/// <param name="message">The message to log to the console.</param>
		/// <param name="logStyling">The styling, if any, to apply to the logged line.</param>
		public void Log(string message, LogStyling logStyling = LogStyling.Plain)
		{
			output.AppendLine();
			output.Append(StyleText(message, logStyling));
			NotifyOutputUpdate();
		}

		/// <summary>
		/// Append a message to the console output.
		/// </summary>
		/// <remarks>
		/// In most cases you should use <see cref="Log"/> instead of this method.
		/// </remarks>
		/// <seealso cref="Log"/>
		/// <param name="message">The text to append to the end of the current output. This does <b>not</b> start a new line by default.</param>
		/// <param name="logStyling">The styling, if any, to apply to the logged line.</param>
		public void LogAppend(string message, LogStyling logStyling = LogStyling.Plain)
		{
			output.Append(StyleText(message, logStyling));
			NotifyOutputUpdate();
		}

		public string GetOutputLog()
		{
			if (outputDirty)
			{
				TrimToCapacity();
				outputLog = output.ToString();
				outputDirty = false;
			}

			return outputLog;
		}

		private void TrimToCapacity()
		{
			if (output.Length > outputCapacity)
			{
				// Starting from the initial place we want to remove capacity, find the end of its line and trim output to there.
				int removeIndex = output.Length - outputCapacity;
				while (removeIndex < output.Length)
				{
					char c = output[removeIndex];
					removeIndex++;
					if (c == '\n' || c == '\r')
					{
						break;
					}
				}

				output.Remove(0, removeIndex);
			}
		}

		protected void NotifyOutputUpdate()
		{
			outputDirty = true;
			OutputUpdate.Invoke();
		}

		[Pure]
		public virtual string StyleText(string text, LogStyling logStyling)
		{
			return ApplyDefaultStyling(text, logStyling);
		}

		protected static string ApplyDefaultStyling(string text, LogStyling logStyling)
		{
			switch (logStyling)
			{
				case LogStyling.Plain:
					return text;
				case LogStyling.Debug:
					return $"<color=#6AC>[Debug]</color> {text}";
				case LogStyling.Info:
					return $"<color=#76A>[Info]</color> {text}";
				case LogStyling.Notice:
					return $"<color=#FC5>[Notice]</color> {text}";
				case LogStyling.Warning:
					return $"<color=#F94>[Warning]</color> {text}";
				case LogStyling.Error:
					return $"<color=#D55>[Error]</color> {text}";
				case LogStyling.Critical:
					return $"<color=#C22>[Critical]</color> {text}";
				default:
					throw new ArgumentOutOfRangeException(nameof(logStyling), logStyling, null);
			}
		}

		private static readonly AutofillValue DefaultAutofill = new AutofillValue("help", 0, 0);
		private readonly AutofillBuilder autofillBuilder = new AutofillBuilder();

		public AutofillValue GetAutofillValue(string input, HashSet<string> autofillExclusions, bool includeCheats = false)
		{
			if (string.IsNullOrEmpty(input))
			{
				return DefaultAutofill;
			}

			List<string> commands = VespaFunctions.SplitStringBySemicolon(input, true, true);
			if (commands.Count == 0)
			{
				return DefaultAutofill;
			}

			string lastCommand = commands[commands.Count - 1];
			int commandStartIndex = 0;
			for (int i = 0; i < commands.Count - 1; i++)
			{
				commandStartIndex += commands[i].Length;
			}

			List<Word> words = VespaFunctions.GetWordsFromString(lastCommand);

			// Don't autofill help on commands that aren't the first one.
			if (words.Count == 0)
			{
				return null;
			}
			// Autofill commands or aliases on first word
			else if (words.Count == 1 && !lastCommand.EndsWith(" "))
			{
				Word word = words[0];
				string inputCommand = word.text.CleanseKey();
				foreach (string aliasKey in AliasSet.Keys)
				{
					if (aliasKey.StartsWith(inputCommand) && !autofillExclusions.Contains(aliasKey))
					{
						return new AutofillValue(aliasKey, inputCommand.Length, commandStartIndex + word.startIndex);
					}
				}

				foreach (Command command in CommandSet.GetPublicCommands(CheatsEnabled || includeCheats))
				{
					string commandKey = command.Key;
					if (commandKey.StartsWith(inputCommand) && !autofillExclusions.Contains(commandKey))
					{
						return new AutofillValue(commandKey, inputCommand.Length, commandStartIndex + word.startIndex);
					}
				}
			}
			else if (words.Count > 0 && CommandSet.TryGetCommand(words[0].text.CleanseKey(), out Command foundCommand) && foundCommand.AutofillMethod != null)
			{
				Word lastWord = words[words.Count - 1];
				bool isNewWordRelevant = lastCommand.EndsWith(" ") && !lastWord.context.HasFlag(Word.Context.IsInOpenLiteral);
				autofillBuilder.Words = words;
				autofillBuilder.RelevantWordIndex = isNewWordRelevant ? words.Count : words.Count - 1;
				autofillBuilder.RelevantWordCharIndex = isNewWordRelevant ? commandStartIndex + lastWord.startIndex + lastWord.text.Length + 1 : commandStartIndex + lastWord.startIndex;
				autofillBuilder.Exclusions = autofillExclusions;
				return foundCommand.AutofillMethod.Invoke(null, new object[] { autofillBuilder }) as AutofillValue;
			}

			return null;
		}

		/// <summary>
		/// Get input text that was recently invoked with <see cref="RunInvocation"/>.
		/// </summary>
		/// <param name="index">The index of the input in <see cref="recentInputs"/> list. The larger this value the older the input was ran.</param>
		[NotNull]
		public string GetRecentInputByIndex(int index)
		{
			if (index <= -1 || recentInputs.Count == 0)
			{
				return string.Empty;
			}
			else
			{
				return recentInputs[Math.Min(index, recentInputs.Count - 1)];
			}
		}

		public enum LogStyling
		{
			/// <summary>
			/// Applies no special styling to the log message
			/// </summary>
			/// <remarks>
			/// Useful for when you want to apply your own styling or just send a raw message to the console.
			/// </remarks>
			Plain,
			/// <summary>
			/// Applies debug styling to the log message
			/// </summary>
			/// <remarks>
			/// Useful for messages that are exclusively for debugging purposes.
			/// </remarks>
			Debug,
			/// <summary>
			/// Applies info styling to the log message
			/// </summary>
			/// <remarks>
			/// Useful for messages that provide info about the application state.
			/// </remarks>
			Info,
			/// <summary>
			/// Applies notice styling to the log message
			/// </summary>
			/// <remarks>
			/// Useful for messages that inform about something that doesn't cause any problems but the user should be aware of.
			/// </remarks>
			Notice,
			/// <summary>
			/// Applies warning styling to the log message
			/// </summary>
			/// <remarks>
			/// Useful for messages that inform about something not functioning as expected but behavior will still be execute.
			/// </remarks>
			Warning,
			/// <summary>
			/// Applies error styling to the log message
			/// </summary>
			/// <remarks>
			/// Useful for messages that inform about an error occuring that prevents behaviors from executing at all.
			/// </remarks>
			Error,
			/// <summary>
			/// Applies critical styling to the log message
			/// </summary>
			/// <remarks>
			/// Useful for messages that inform about an error that will have consequences to other code systems.
			/// </remarks>
			Critical
		}
	}
}