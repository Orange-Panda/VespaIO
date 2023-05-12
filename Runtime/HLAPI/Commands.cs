using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;

namespace LMirman.VespaIO
{
	public static class Commands
	{
		public static readonly CommandSet commandSet = new CommandSet();

		public static IEnumerable<Command> AllCommands => commandSet.AllCommands;
		public static IEnumerable<KeyValuePair<string, Command>> AllDefinitions => commandSet.AllDefinitions;

		static Commands()
		{
			BuildCommandSet();
		}

		/// <summary>
		/// Does nothing but just by calling this method will call the static constructor, thus building the lookup table.
		/// </summary>
		internal static void PreloadLookup() { }

		/// <summary>
		/// Build the command set for all command attributes in the project by searching for <see cref="StaticCommandAttribute"/>.
		/// </summary>
		private static void BuildCommandSet()
		{
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();
			commandSet.UnregisterAllCommands();
			Assembly[] assemblies = GetAssembliesFromDomain(AppDomain.CurrentDomain, ConsoleSettings.Config.assemblyFilter);
			List<Type> classes = GetClassesFromAssemblies(assemblies);
			List<StaticCommand> staticCommands = GetStaticCommandsFromClasses(classes);
			foreach (StaticCommand staticCommand in staticCommands)
			{
				commandSet.RegisterCommand(staticCommand.properties, staticCommand.methodInfo);
			}

#if UNITY_EDITOR // Only done in editor since the end user should not care about this message and not checking this dramatically improves performance.
			if (ConsoleSettings.Config.warnForNonstaticMethods)
			{
				List<StaticCommand> instancedMethods = GetInstanceCommandsFromClasses(classes);
				foreach (StaticCommand staticCommand in instancedMethods)
				{
					string message =
						$"<color=red>ERROR:</color> Static command attribute with key {staticCommand.properties.Key} is a applied to non-static method {staticCommand.methodInfo.Name}, which is unsupported. The method will not be added to the console.";
					DevConsole.Log(message);
				}
			}
#endif
			stopwatch.Stop();
			DevConsole.Log($"<color=green>Command generation completed in {stopwatch.Elapsed.TotalSeconds:F3}s</color>");
		}

		#region Reflection
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

			return commands;
		}
		#endregion

		private class StaticCommand
		{
			public readonly ICommandProperties properties;
			public readonly MethodInfo methodInfo;

			public StaticCommand(StaticCommandAttribute properties, MethodInfo methodInfo)
			{
				this.properties = properties;
				this.methodInfo = methodInfo;
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