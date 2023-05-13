using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LMirman.VespaIO
{
	/// <summary>
	/// Default commands that are built into the console and are useful in practically any project.
	/// </summary>
	public static class NativeCommands
	{
		private const int HelpPageLength = 10;

		[StaticCommand("quit", Name = "Quit Application", Description = "Closes the application", ManualPriority = 70)]
		public static void Quit()
		{
#if UNITY_EDITOR
			UnityEditor.EditorApplication.ExitPlaymode();
#endif
			Application.Quit();
		}

		[StaticCommand("welcome", Name = "Welcome Prompt", Description = "Log the welcome prompt into the console")]
		public static void Welcome()
		{
			DevConsole.PrintWelcome();
		}

		[StaticCommand("clear", Name = "Clear Console History",
			Description =
				"Clears the entire console history including this command's execution. Usage is recommended when the history grows too large or the application freezes when logging occurs.",
			ManualPriority = 80)]
		public static void Clear()
		{
			DevConsole.console.Clear();
		}

		[StaticCommand("scene", Name = "Change Scene", Description = "Changes the scene to the scene name provided by the user", Cheat = true)]
		public static void Scene()
		{
			DevConsole.Log($"Current Scene: {SceneManager.GetActiveScene().name}");
		}

		[StaticCommand("scene", Cheat = true)]
		public static void Scene(string target)
		{
			DevConsole.Log($"Attempting to load scene: {target}");
			SceneManager.LoadScene(target);
		}

		[CommandAutofill("scene")]
		// TODO: Autofill from scenes in the build target
		private static AutofillValue GetSceneAutofillValue(AutofillBuilder autofillBuilder)
		{
			return autofillBuilder.CreateOverwriteAutofill("SampleScene");
		}

		[StaticCommand("echo", Name = "Echo", Description = "Repeat the input back to the console.")]
		public static void Echo(string message)
		{
			DevConsole.Log(message);
		}

		[StaticCommand("cheats", Name = "Enable Cheats", Description = "Enable cheats for this play session.")]
		public static void EnableCheats(string value = "")
		{
			if (DevConsole.console.CheatsEnabled)
			{
				DevConsole.Log("Cheats are already enabled! Cheats can't be disabled on this save slot.", Console.LogStyling.Notice);
			}
			else if (Application.isEditor || value == "enable")
			{
				DevConsole.console.EnableCheats();
			}
			else
			{
				DevConsole.Log("WARNING: By enabling cheats you understand and agree to the following:\n" +
							   "<color=red>- You will be unable to disable cheats for this session.\n" +
							   "- Cheat commands will compromise the standard gameplay experience.\n" +
							   "- The game may become unstable with the use of cheat commands.\n" +
							   "- Some features like saving and achievements may be disabled.\n\n</color>" +
							   "It is highly recommended you don't enable cheats unless you are comfortable with the unstable experience.\n" +
							   "If you are still sure you would like to enable cheat commands please enter the following command <b>exactly</b> as follows: cheats enable");
			}
		}

		#region Help Commands
		[StaticCommand("help", Name = "Help Manual", Description = "Search for commands and get assistance with particular commands.", ManualPriority = 90)]
		public static void Help()
		{
			LogPage();
		}

		[StaticCommand("help")]
		public static void Help(int pageNum)
		{
			LogPage(pageNum);
		}

		[StaticCommand("help")]
		public static void Help(string query)
		{
			string value = query.ToLower();
			if (Commands.commandSet.TryGetCommand(value, out Command command))
			{
				DevConsole.Log(command.Guide);
			}
			else
			{
				PrintMatching(value);
			}
		}

		[CommandAutofill("help")]
		private static AutofillValue GetHelpAutofillValue(AutofillBuilder autofillBuilder)
		{
			if (autofillBuilder.RelevantWordIndex != 1)
			{
				return null;
			}

			string relevantWord = autofillBuilder.GetRelevantWordText().CleanseKey();
			foreach (string commandKey in Commands.commandSet.Keys)
			{
				if (commandKey.StartsWith(relevantWord) && !autofillBuilder.Exclusions.Contains(commandKey))
				{
					return autofillBuilder.CreateCompletionAutofill(commandKey);
				}
			}

			return null;
		}

		private static int CountPages()
		{
			List<Command> commands = new List<Command>();
			foreach (Command command in Commands.commandSet.AllCommands)
			{
				if (!commands.Contains(command) && !IsCommandHidden(command))
				{
					commands.Add(command);
				}
			}

			return Mathf.CeilToInt((float)commands.Count / HelpPageLength);
		}

		private static void LogPage(int page = 1)
		{
			int pageCount = CountPages();
			page = Mathf.Clamp(page, 1, pageCount);
			List<Command> loggedCommands = new List<Command>();
			int remaining = HelpPageLength;
			int ignore = (page - 1) * HelpPageLength;
			DevConsole.Log($"========== Help: Page {page}/{pageCount} ==========");
			foreach (Command command in Commands.commandSet.AllCommands)
			{
				//Stop if we have print out enough commands
				if (remaining <= 0)
				{
					break;
				}

				if (!loggedCommands.Contains(command) && !IsCommandHidden(command))
				{
					loggedCommands.Add(command);

					if (ignore > 0)
					{
						ignore--;
					}
					else if (PrintLookup(command))
					{
						remaining--;
					}
				}
			}

			DevConsole.Log($"========== END OF PAGE {page}/{pageCount} ==========");
			DevConsole.Log("========== Use \"help {page #}\" for more ==========");
		}

		private static void PrintMatching(string key)
		{
			DevConsole.Log($"========== Commands Containing \"{key}\" ==========");
			List<Command> commands = new List<Command>();
			foreach (Command command in Commands.commandSet.AllCommands)
			{
				if ((command.Key.Contains(key) || command.Name.ToLower().Contains(key)) && !commands.Contains(command))
				{
					commands.Add(command);
				}
			}

			foreach (Command command in commands)
			{
				PrintLookup(command);
			}
		}

		private static bool PrintLookup(Command command)
		{
			if (IsCommandHidden(command))
			{
				return false;
			}

			DevConsole.Log(!string.IsNullOrWhiteSpace(command.Description) ? $"= {command.Name} \"{command.Key}\"\n  - {command.Description}" : $"= {command.Name} \"{command.Key}\"");
			return true;
		}

		private static bool IsCommandHidden(Command command)
		{
			return command.Hidden || (command.Cheat && !DevConsole.console.CheatsEnabled);
		}
		#endregion

		#region Alias Commands
		[StaticCommand("alias", Name = "Set Alias", Description = "Set a particular alias definition")]
		public static void SetAlias(string alias, string value)
		{
			//Validate alias name
			alias = alias.CleanseKey();
			if (alias.Length == 0 || value.Length == 0)
			{
				DevConsole.Log("Your alias or value was empty.", Console.LogStyling.Notice);
				return;
			}

			// Set alias
			bool isNewAlias = Aliases.aliasSet.SetAlias(alias, value);
			Aliases.WriteToDisk();
			DevConsole.Log(isNewAlias
				? $"<color=green>+</color> Added alias \"{alias}\" to represent \"{value}\""
				: $"<color=yellow>*</color> Modified alias \"{alias}\" to represent \"{value}\"");
		}

		[StaticCommand("alias_delete", Name = "Delete Alias", Description = "Delete a particular alias definition")]
		public static void DeleteAlias(string alias)
		{
			alias = alias.CleanseKey();
			bool didRemoveAlias = Aliases.aliasSet.RemoveAlias(alias);
			if (didRemoveAlias)
			{
				Aliases.WriteToDisk();
			}

			DevConsole.Log(didRemoveAlias
				? $"<color=red>-</color> Removed alias \"{alias}\"."
				: $"<color=yellow>Warning:</color> Tried to remove alias \"{alias}\" but no such alias was found.");
		}

		[CommandAutofill("alias_delete")]
		private static AutofillValue GetAliasAutofillValue(AutofillBuilder autofillBuilder)
		{
			if (autofillBuilder.RelevantWordIndex != 1)
			{
				return null;
			}

			string relevantWord = autofillBuilder.GetRelevantWordText().CleanseKey();
			foreach (string aliasKey in Aliases.aliasSet.Keys)
			{
				if (aliasKey.StartsWith(relevantWord) && !autofillBuilder.Exclusions.Contains(aliasKey))
				{
					return autofillBuilder.CreateCompletionAutofill(aliasKey);
				}
			}

			return null;
		}

		[StaticCommand("alias_reset_all", Name = "Reset All Aliases", Description = "Reset all alias definitions")]
		public static void ResetAllAliasesWarning()
		{
			DevConsole.Log("This will remove <b>ALL</b> alias definitions!\nTo confirm alias reset please enter the following command: \"alias_reset_all CONFIRM\"", Console.LogStyling.Notice);
		}

		[StaticCommand("alias_reset_all", Name = "Reset All Aliases", Description = "Reset all alias definitions")]
		public static void ResetAllAliases(string confirmation)
		{
			if (confirmation == "CONFIRM")
			{
				Aliases.ResetAliasesAndFile();
				DevConsole.Log("<color=red>-</color> All aliases have been removed!");
			}
			else
			{
				ResetAllAliasesWarning();
			}
		}

		[StaticCommand("alias")]
		[StaticCommand("alias_view", Name = "View Alias", Description = "View the definition for a particular alias")]
		public static void ViewAlias(string alias)
		{
			alias = alias.CleanseKey();
			DevConsole.Log(Aliases.aliasSet.TryGetAlias(alias, out string definition)
				? $"\"{alias}\" -> \"{definition}\""
				: $"<color=red>Error:</color> Tried to view alias \"{alias}\" but no such alias was found.");
		}

		[StaticCommand("alias_list", Name = "List Aliases", Description = "View list of all aliases that have been defined")]
		public static void ListAlias(string filter)
		{
			filter = filter.CleanseKey();
			DevConsole.Log($"--- Aliases Containing \"{filter}\" ---");
			foreach (KeyValuePair<string, string> alias in Aliases.aliasSet.AllAliases)
			{
				if (alias.Key.Contains(filter) || alias.Value.ToLower().Contains(filter))
				{
					DevConsole.Log($"'{alias.Key}'  ->  '{alias.Value}'");
				}
			}
		}

		private const int AliasPageLength = 10;

		[StaticCommand("alias_list", Name = "List Aliases", Description = "View list of all aliases that have been defined")]
		public static void ListAlias(int pageNum = 0)
		{
			int pageCount = Mathf.Max(Mathf.CeilToInt((float)Aliases.aliasSet.AliasCount / AliasPageLength), 1);
			pageNum = Mathf.Clamp(pageNum, 1, pageCount);
			int remaining = AliasPageLength;
			int ignore = (pageNum - 1) * AliasPageLength;
			DevConsole.Log($"--- Aliases {pageNum}/{pageCount} ---");
			foreach (KeyValuePair<string, string> alias in Aliases.aliasSet.AllAliases)
			{
				//Stop if we have print out enough commands
				if (remaining <= 0)
				{
					break;
				}

				if (ignore > 0)
				{
					ignore--;
				}
				else
				{
					DevConsole.Log($"'{alias.Key}'  ->  '{alias.Value}'");
					remaining--;
				}
			}

			DevConsole.Log($"--- END OF PAGE {pageNum}/{pageCount} ---");
			DevConsole.Log("--- Use \"alias_list {page #}\" for more ---");
		}
		#endregion
	}
}