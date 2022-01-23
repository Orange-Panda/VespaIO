using System.Collections.Generic;
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
		public static void Scene(Longstring target)
		{
			DevConsole.Log($"Attempting to load scene: {target}");
			SceneManager.LoadScene(target);
		}

		[StaticCommand("echo", Name = "Echo", Description = "Repeat the input back to the console.")]
		public static void Echo(Longstring message)
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
					"If you are still sure you would like to enable cheat commands please enter the follwing command <b>exactly</b> as follows: cheats enable");
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
		public static void Help(Longstring query)
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
			return command.Hidden || command.Cheat && !DevConsole.CheatsEnabled;
		}
		#endregion
	}
}