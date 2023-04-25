using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;

namespace LMirman.VespaIO
{
	public static class Commands
	{
		private static readonly Dictionary<string, Command> Lookup = new Dictionary<string, Command>();

		public static IEnumerable<Command> AllCommands => Lookup.Values;
		public static IEnumerable<KeyValuePair<string, Command>> AllDefinitions => Lookup;

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
		/// Build the <see cref="Lookup"/> Dictionary for all command attributes in the project.
		/// </summary>
		private static void BuildLookupTable()
		{
			Assembly[] assemblies = GetAssembliesFromDomain(AppDomain.CurrentDomain, ConsoleSettings.Config.assemblyFilter);
			List<Type> classes = GetClassesFromAssemblies(assemblies);
			List<StaticCommand> staticCommands = GetStaticCommandsFromClasses(classes);
			foreach (StaticCommand staticCommand in staticCommands)
			{
				RegisterCommand(staticCommand.attribute, staticCommand.methodInfo);
			}

#if UNITY_EDITOR // Only done in editor since the end user should not care about this message and not checking this dramatically improves performance.
			if (ConsoleSettings.Config.warnForNonstaticMethods)
			{
				List<StaticCommand> instancedMethods = GetInstanceCommandsFromClasses(classes);
				foreach (StaticCommand staticCommand in instancedMethods)
				{
					string message =
						$"<color=red>ERROR:</color> Static command attribute with key {staticCommand.attribute.Key} is a applied to non-static method {staticCommand.methodInfo.Name}, which is unsupported. The method will not be added to the console.";
					DevConsole.Log(message);
				}
			}
#endif
		}

		private static readonly Regex SystemAssemblyRegex = new Regex("^(?!unity|system|mscorlib|mono|log4net|newtonsoft|nunit|jetbrains)", RegexOptions.IgnoreCase);

		/// <summary>
		/// Get an array of all assemblies that the user would like to check for commands within.
		/// </summary>
		private static Assembly[] GetAssembliesFromDomain(AppDomain domain, AssemblyFilter assemblyFilter)
		{
			Assembly[] assemblies = domain.GetAssemblies();
			switch (assemblyFilter)
			{
				case AssemblyFilter.Standard:
					List<Assembly> validAssemblies = new List<Assembly>();
					foreach (Assembly assembly in assemblies)
					{
						if (assembly != null && SystemAssemblyRegex.IsMatch(assembly.FullName))
						{
							validAssemblies.Add(assembly);
						}
					}

					return validAssemblies.ToArray();
				case AssemblyFilter.Exhaustive:
					return assemblies;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		/// <summary>
		/// Get a list of all classes within an array of assemblies
		/// </summary>
		private static List<Type> GetClassesFromAssemblies(Assembly[] assemblies)
		{
			List<Type> classes = new List<Type>();
			foreach (Assembly assembly in assemblies)
			{
				Type[] types = assembly.GetTypes();
				foreach (Type type in types)
				{
					if (type.IsClass)
					{
						classes.Add(type);
					}
				}
			}

			return classes;
		}

		private const BindingFlags StaticMethodBindingFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.InvokeMethod | BindingFlags.DeclaredOnly;

		private static List<StaticCommand> GetStaticCommandsFromClasses(List<Type> classes)
		{
			List<StaticCommand> commands = new List<StaticCommand>();
			foreach (Type type in classes)
			{
				foreach (MethodInfo method in type.GetMethods(StaticMethodBindingFlags))
				{
					object[] customAttributes = method.GetCustomAttributes(typeof(StaticCommandAttribute), false);
					foreach (object customAttribute in customAttributes)
					{
						if (customAttribute is StaticCommandAttribute staticCommandAttribute)
						{
							commands.Add(new StaticCommand(staticCommandAttribute, method));
						}
					}
				}
			}

			commands.Sort(new CommandComparer());
			return commands;
		}

		private const BindingFlags InstanceMethodBindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.InvokeMethod | BindingFlags.DeclaredOnly;

		private static List<StaticCommand> GetInstanceCommandsFromClasses(List<Type> classes)
		{
			List<StaticCommand> commands = new List<StaticCommand>();
			foreach (Type type in classes)
			{
				foreach (MethodInfo method in type.GetMethods(InstanceMethodBindingFlags))
				{
					object[] customAttributes = method.GetCustomAttributes(typeof(StaticCommandAttribute), false);
					foreach (object customAttribute in customAttributes)
					{
						if (customAttribute is StaticCommandAttribute attribute)
						{
							commands.Add(new StaticCommand(attribute, method));
						}
					}
				}
			}

			commands.Sort(new CommandComparer());
			return commands;
		}

		public static bool ContainsCommand(string key)
		{
			return Lookup.ContainsKey(key.CleanseKey());
		}

		public static bool TryGetCommand(string key, out Command command)
		{
			return Lookup.TryGetValue(key.CleanseKey(), out command);
		}

		public static Command GetCommand(string key, Command fallbackCommand = null)
		{
			return TryGetCommand(key.CleanseKey(), out Command command) ? command : fallbackCommand;
		}

		public static void RegisterCommand(StaticCommandAttribute attribute, MethodInfo methodInfo)
		{
			string key = attribute.Key.CleanseKey();
			if (TryGetCommand(key, out Command command))
			{
				command.AddMethod(attribute, methodInfo);
			}
			else
			{
				command = new Command(attribute, methodInfo);
				Lookup.Add(key, command);
			}
		}

		/// <summary>
		/// Unregister a specific method definition for a console command.
		/// </summary>
		/// <param name="key">The key of the command you would like to remove.</param>
		/// <param name="methodInfo">The method you would like to unregister for the command</param>
		public static void UnregisterCommand(string key, MethodInfo methodInfo)
		{
			key = key.CleanseKey();
			if (TryGetCommand(key, out Command command))
			{
				command.RemoveMethod(methodInfo);

				if (!command.HasMethod)
				{
					Lookup.Remove(key);
				}
			}
		}

		/// <summary>
		/// Unregister all command definitions for a particular key.
		/// </summary>
		/// <param name="key">The key of the command you would like to remove.</param>
		public static void UnregisterCommand(string key)
		{
			key = key.CleanseKey();
			Lookup.Remove(key);
		}

		private class StaticCommand
		{
			public readonly StaticCommandAttribute attribute;
			public readonly MethodInfo methodInfo;

			public StaticCommand(StaticCommandAttribute attribute, MethodInfo methodInfo)
			{
				this.attribute = attribute;
				this.methodInfo = methodInfo;
			}
		}

		private class CommandComparer : IComparer<StaticCommand>
		{
			public int Compare(StaticCommand x, StaticCommand y)
			{
				if (x == null)
				{
					return 0;
				}
				else if (y == null)
				{
					return -1;
				}
				else if (x.attribute.ManualPriority > y.attribute.ManualPriority)
				{
					return -1;
				}
				else if (x.attribute.ManualPriority < y.attribute.ManualPriority)
				{
					return 1;
				}
				else
				{
					return string.CompareOrdinal(x.attribute.Key, y.attribute.Key);
				}
			}
		}

		public enum AssemblyFilter
		{
			/// <summary>
			/// This search type will search only assemblies that are likely to contain commands.
			/// </summary>
			/// <remarks>
			/// This specifically ignores assemblies that <b>begin</b> with "unity", "system", "mscorlib", "mono", "log4net", "newtonsoft", "nunit", "jetbrains" (Case insensitive).
			/// </remarks>
			Standard,
			/// <summary>
			/// This search type will search every single assembly: including one's that <see cref="Standard"/> would have assumed to contain no commands.
			/// </summary>
			/// <remarks>
			/// This assembly filter type should only be used in cases where the <see cref="Standard"/> type is accidentally ignoring assemblies that contain commands.
			/// Using this filter will have a significant negative performance impact so use with caution!
			/// </remarks>
			Exhaustive
		}
	}
}