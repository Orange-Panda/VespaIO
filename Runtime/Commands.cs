using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace LMirman.VespaIO
{
	internal static class Commands
	{
		internal static Dictionary<string, Command> Lookup { get; private set; }

		static Commands()
		{
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();
			BuildLookupTable();
			stopwatch.Stop();
			DevConsole.Log($"<color=green>Command generation completed in {stopwatch.Elapsed.TotalSeconds:F3}s</color>");
		}

		/// <summary>
		/// Does nothing but just by calling this method will call the static constructor, thus building the lookup table.
		/// </summary>
		internal static void PreloadLookup() { }

		/// <summary>
		/// Build the <see cref="Lookup"/> Dictionary for the <see cref="DevConsole"/>.
		/// </summary>
		/// <remarks>This is VERY expensive on the garbage collector. Need a more efficient way to do this if possible. Fortunately this is only done once so even if this is the only way it can be done its not too bad.</remarks>
		private static void BuildLookupTable()
		{
			Lookup = new Dictionary<string, Command>();
			List<Type> classes = AppDomain.CurrentDomain.GetAssemblies()
				.SelectMany(x => x.GetTypes())
				.Where(x => x.IsClass).ToList();

			IEnumerable<MethodInfo> staticMethods = classes.SelectMany(x => x.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.InvokeMethod | BindingFlags.DeclaredOnly))
				.Where(x => x.GetCustomAttribute(typeof(StaticCommandAttribute), false) != null);

			List<KeyValuePair<StaticCommandAttribute, MethodInfo>> priorityCommands = new List<KeyValuePair<StaticCommandAttribute, MethodInfo>>();
			List<KeyValuePair<StaticCommandAttribute, MethodInfo>> commands = new List<KeyValuePair<StaticCommandAttribute, MethodInfo>>();
			foreach (MethodInfo method in staticMethods)
			{
				foreach (object attribute in method.GetCustomAttributes(typeof(StaticCommandAttribute), false))
				{
					StaticCommandAttribute command = attribute as StaticCommandAttribute;
					if (command.ManualPriority)
					{
						priorityCommands.Add(new KeyValuePair<StaticCommandAttribute, MethodInfo>(command, method));
					}
					else
					{
						commands.Add(new KeyValuePair<StaticCommandAttribute, MethodInfo>(command, method));
					}
				}
			}

			foreach (KeyValuePair<StaticCommandAttribute, MethodInfo> pair in priorityCommands.OrderBy(pair => pair.Key.Key))
			{
				StaticCommandAttribute command = pair.Key;
				MethodInfo method = pair.Value;

				if (Lookup.ContainsKey(command.Key))
				{
					Lookup[command.Key].AddMethod(command, method);
				}
				else
				{
					Lookup.Add(command.Key, new Command(command, method));
				}
			}

			foreach (KeyValuePair<StaticCommandAttribute, MethodInfo> pair in commands.OrderBy(pair => pair.Key.Key))
			{
				StaticCommandAttribute command = pair.Key;
				MethodInfo method = pair.Value;

				if (Lookup.ContainsKey(command.Key))
				{
					Lookup[command.Key].AddMethod(command, method);
				}
				else
				{
					Lookup.Add(command.Key, new Command(command, method));
				}
			}

#if UNITY_EDITOR // Only done in editor since the end user should not care about this message and not checking this dramatically improves performance.
			if (ConsoleSettings.Config.warnForNonstaticMethods)
			{
				IEnumerable<MethodInfo> instancedMethods = classes.SelectMany(x => x.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.InvokeMethod | BindingFlags.DeclaredOnly))
					.Where(x => x.GetCustomAttribute(typeof(StaticCommandAttribute), false) != null);

				foreach (MethodInfo method in instancedMethods)
				{
					foreach (object attribute in method.GetCustomAttributes(typeof(StaticCommandAttribute), false))
					{
						StaticCommandAttribute command = attribute as StaticCommandAttribute;
						string message = $"<color=red>ERROR:</color> Static command attribute with key {command.Key} is a applied to non-static method {method.Name}, which is unsupported. The method will not be added to the console.";
						DevConsole.Log(message);
					}
				}
			}
#endif
		}

		/// <summary>
		/// Find the first command that starts with the <paramref name="searchText"/>.
		/// </summary>
		/// <param name="searchText">The text to search with.</param>
		/// <param name="excludeList">Commands that are exempt, usually because they have already been filled in the console.</param>
		/// <returns>The first command that starts with the search text or null if none is found.</returns>
		internal static Command FindFirstMatch(string searchText, List<string> excludeList)
		{
			searchText = searchText.ToLower();
			foreach (KeyValuePair<string, Command> pair in Lookup)
			{
				bool hidden = pair.Value.Hidden || (pair.Value.Cheat && !DevConsole.CheatsEnabled && !(Application.isEditor && ConsoleSettings.Config.editorAutoEnableCheats));
				if (pair.Key.StartsWith(searchText) && !excludeList.Contains(pair.Key) && !hidden)
				{
					return pair.Value;
				}
			}

			return null;
		}
	}

	public class Command
	{
		public readonly string Key;

		public string Name { get; private set; }
		public string Description { get; private set; }
		public string Guide { get; private set; }
		public bool Cheat { get; private set; }
		public bool Hidden { get; private set; }

		public List<MethodInfo> Methods { get; } = new List<MethodInfo>();

		public Command(StaticCommandAttribute attribute, MethodInfo method)
		{
			Key = attribute.Key;
			Name = attribute.Name == string.Empty ? method.Name : attribute.Name;
			Description = attribute.Description;
			Guide = $"Usage: {Key} ";
			ParameterInfo[] parameters = method.GetParameters();
			for (int i = 0; i < parameters.Length; i++)
			{
				Guide += $"[{TranslateParameter(parameters[i].ParameterType)}] ";
			}
			Cheat = attribute.Cheat;
			Hidden = attribute.Hidden;
			Methods.Add(method);
		}

		public void AddMethod(StaticCommandAttribute attribute, MethodInfo method)
		{
			if (!string.IsNullOrWhiteSpace(attribute.Name))
			{
				Name = attribute.Name;
			}

			if (!string.IsNullOrWhiteSpace(attribute.Description))
			{
				Description = attribute.Description;
			}

			Cheat = Cheat || attribute.Cheat;
			Hidden = Hidden || attribute.Hidden;
			Guide += $"\nUsage: {Key} ";
			ParameterInfo[] parameters = method.GetParameters();
			for (int i = 0; i < parameters.Length; i++)
			{
				Guide += $"[{TranslateParameter(parameters[i].ParameterType)}] ";
			}
			Methods.Add(method);
		}

		private static string TranslateParameter(Type type)
		{
			if (type == typeof(int))
			{
				return "INTEGER";
			}
			else if (type == typeof(float))
			{
				return "FLOAT";
			}
			else if (type == typeof(string))
			{
				return "WORD";
			}
			else if (type == typeof(LongString))
			{
				return "PHRASE";
			}
			else
			{
				return "INVALID/UNKNOWN";
			}
		}
	}
}