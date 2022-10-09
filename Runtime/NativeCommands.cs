using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LMirman.VespaIO
{
	/// <summary>
	/// Default commands that are built into the console and are useful in any project.
	/// </summary>
	public static class NativeCommands
	{
		private const int HelpPageLength = 10;

		[StaticCommand("quit", Name = "Quit Application", Description = "Closes the application", ManualPriority = true)]
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

		[StaticCommand("clear", Name = "Clear Console History", Description = "Clears the entire console history including this command's execution. Usage is recommended when the history grows too large or the application freezes when logging occurs.", ManualPriority = true)]
		public static void Clear()
		{
			DevConsole.Clear();
		}

		[StaticCommand("scene", Name = "Change Scene", Description = "Changes the scene to the scene name provided by the user", Cheat = true)]
		public static void Scene()
		{
			DevConsole.Log($"Current Scene: {SceneManager.GetActiveScene().name}");
		}

		[StaticCommand("scene", Cheat = true)]
		public static void Scene(LongString target)
		{
			DevConsole.Log($"Attempting to load scene: {target}");
			SceneManager.LoadScene(target);
		}

		[StaticCommand("echo", Name = "Echo", Description = "Repeat the input back to the console.")]
		public static void Echo(LongString message)
		{
			DevConsole.Log(message);
		}

		[StaticCommand("cheats", Name = "Enable Cheats", Description = "Enable cheats for this play session.")]
		public static void EnableCheats(string value = "")
		{
			if (DevConsole.CheatsEnabled)
			{
				DevConsole.Log("Cheats are already enabled! Cheats can't be disabled on this save slot.");
			}
			else if (Application.isEditor || value == "enable")
			{
				DevConsole.Log("Cheats have been enabled!");
				DevConsole.CheatsEnabled = true;
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
		[StaticCommand("help", Name = "Help Manual", Description = "Search for commands and get assistance with particular commands.", ManualPriority = true)]
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
		public static void Help(LongString query)
		{
			string value = ((string)query).ToLower();
			if (Commands.Lookup.TryGetValue(value, out Command command))
			{
				DevConsole.LogCommandHelp(command);
			}
			else
			{
				PrintMatching(value);
			}
		}

		private static int CountPages()
		{
			List<Command> commands = new List<Command>();
			foreach (Command command in Commands.Lookup.Values)
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
			foreach (Command command in Commands.Lookup.Values)
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
			foreach (Command command in Commands.Lookup.Values)
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

			if (!string.IsNullOrWhiteSpace(command.Description))
			{
				DevConsole.Log($"= {command.Name} \"{command.Key}\"\n  - {command.Description}");
			}
			else
			{
				DevConsole.Log($"= {command.Name} \"{command.Key}\"");
			}
			return true;
		}

		private static bool IsCommandHidden(Command command)
		{
			return command.Hidden || (command.Cheat && !DevConsole.CheatsEnabled);
		}
		#endregion

		#region Alias Commands
		[StaticCommand("alias", Name = "Set Alias", Description = "Set a particular alias definition")]
		public static void SetAlias(string alias, string value)
		{
			//Validate alias name
			alias = alias.ToLower();
			if (alias.Contains(" "))
			{
				DevConsole.Log("<color=yellow>Warning:</color> Your alias contained spaces but spaces are unsupported. The spaces have been removed.");
				alias = alias.Replace(" ", string.Empty);
			}

			Regex regex = new Regex(@"^[$a-z0-9_]*");
			if (!regex.IsMatch(alias))
			{
				DevConsole.Log("<color=red>Error:</color> Your alias contained out of bounds characters. Aliases can only contain alphanumerical characters (a-z, 0-9) and the underscore character.");
				return;
			}

			if (alias.Length == 0 || value.Length == 0)
			{
				DevConsole.Log("<color=red>Error:</color> Your alias or value was empty.");
				return;
			}

			// Set alias
			if (Aliases.Lookup.ContainsKey(alias))
			{
				Aliases.Lookup[alias] = value;
				DevConsole.Log($"<color=yellow>*</color> Modified alias \"{alias}\" to represent \"{value}\"");
			}
			else
			{
				Aliases.Lookup.Add(alias, value);
				DevConsole.Log($"<color=green>+</color> Added alias \"{alias}\" to represent \"{value}\"");
			}
			Aliases.WriteLookup();
		}

		[StaticCommand("alias_delete", Name = "Delete Alias", Description = "Delete a particular alias definition")]
		public static void DeleteAlias(string alias)
		{
			alias = alias.ToLower();
			if (!Aliases.Lookup.ContainsKey(alias))
			{
				DevConsole.Log($"<color=red>Error:</color> Tried to remove alias \"{alias}\" but no such alias was found.");
				return;
			}

			Aliases.Lookup.Remove(alias);
			DevConsole.Log($"<color=red>-</color> Removed alias \"{alias}\".");
			Aliases.WriteLookup();
		}

		[StaticCommand("alias_reset_all", Name = "Reset All Aliases", Description = "Reset all alias definitions")]
		public static void ResetAllAliases()
		{
			DevConsole.Log("<color=orange>Alert:</color> This will remove <b>ALL</b> alias definitions!\nTo confirm alias reset please enter the following command: \"alias_reset_all CONFIRM\"");
		}

		[StaticCommand("alias_reset_all", Name = "Reset All Aliases", Description = "Reset all alias definitions")]
		public static void ResetAllAliases(string confirmation)
		{
			if (confirmation == "CONFIRM")
			{
				Aliases.Reset();
				DevConsole.Log("<color=red>-</color> All aliases have been removed!");
			}
			else
			{
				ResetAllAliases();
			}
		}

		[StaticCommand("alias_view", Name = "View Alias", Description = "View the definition for a particular alias")]
		public static void ViewAlias(string alias)
		{
			alias = alias.ToLower();
			if (!Aliases.Lookup.ContainsKey(alias))
			{
				DevConsole.Log($"<color=red>Error:</color> Tried to view alias \"{alias}\" but no such alias was found.");
				return;
			}

			DevConsole.Log($"\"{alias}\" -> \"{Aliases.Lookup[alias]}\"");
		}

		[StaticCommand("alias_list", Name = "List Aliases", Description = "View list of all aliases that have been defined")]
		public static void ListAlias(string filter)
		{
			string lowFilter = filter.ToLower();
			DevConsole.Log($"--- Aliases Containing \"{filter}\" ---");
			foreach (KeyValuePair<string, string> alias in Aliases.Lookup)
			{
				if (alias.Key.Contains(lowFilter) || alias.Value.ToLower().Contains(lowFilter))
				{
					DevConsole.Log($"\"{alias.Key}\" -> \"{alias.Value}\"");
				}
			}
		}

		private const int AliasPageLength = 10;
		[StaticCommand("alias_list", Name = "List Aliases", Description = "View list of all aliases that have been defined")]
		public static void ListAlias(int pageNum = 0)
		{
			int pageCount = Mathf.Max(Mathf.CeilToInt((float)Aliases.Lookup.Count / AliasPageLength), 1);
			pageNum = Mathf.Clamp(pageNum, 1, pageCount);
			int remaining = AliasPageLength;
			int ignore = (pageNum - 1) * AliasPageLength;
			DevConsole.Log($"========== Help: Page {pageNum}/{pageCount} ==========");
			foreach (KeyValuePair<string, string> alias in Aliases.Lookup)
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
					DevConsole.Log($"\"{alias.Key}\" -> \"{alias.Value}\"");
					remaining--;
				}
			}
			DevConsole.Log($"========== END OF PAGE {pageNum}/{pageCount} ==========");
			DevConsole.Log("========== Use \"help {page #}\" for more ==========");
		}
		#endregion
	}
}