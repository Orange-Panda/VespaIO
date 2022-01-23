using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace LMirman.VespaIO
{
	internal class Commands
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
		/// <remarks>This is VERY expensive on the garbage collector. Need a more efficent way to do this if possible. Fortunately this is only done once so even if this is the only way it can be done its not too bad.</remarks>
		private static void BuildLookupTable()
		{
			Lookup = new Dictionary<string, Command>();
			var classes = AppDomain.CurrentDomain.GetAssemblies()
				.SelectMany(x => x.GetTypes())
				.Where(x => x.IsClass);

			var staticMethods = classes.SelectMany(x => x.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.InvokeMethod | BindingFlags.DeclaredOnly))
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
				var instancedMethods = classes.SelectMany(x => x.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.InvokeMethod | BindingFlags.DeclaredOnly))
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
				Guide += $"[{parameters[i].ParameterType}] ";
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
				Guide += $"[{parameters[i].ParameterType}] ";
			}
			Methods.Add(method);
		}
	}
}