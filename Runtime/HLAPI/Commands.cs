using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;

namespace LMirman.VespaIO
{
	/// <summary>
	/// Stores and saves a global <see cref="CommandSet"/> for usage in the <see cref="NativeConsole"/>.
	/// </summary>
	[PublicAPI]
	public static class Commands
	{
		private static readonly Regex StandardSearchRegex = new Regex("^(?!unity|system|mscorlib|mono|log4net|newtonsoft|nunit|jetbrains)", RegexOptions.IgnoreCase);
		private static readonly Regex ExhaustiveSearchRegex = new Regex(".+");

		public static readonly CommandSet commandSet = new CommandSet();

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
		/// <remarks>
		/// This is an expensive operation and should be used sparingly.
		/// Usually desirable if you have modified code at runtime or mutated the <see cref="commandSet"/> and wish to revert to the initial state.
		/// </remarks>
		public static void BuildCommandSet()
		{
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();
			commandSet.UnregisterAllCommands();
			Assembly[] assemblies = VespaReflection.GetAssembliesFromDomain(AppDomain.CurrentDomain, GetRegexFilter(NativeSettings.Config.assemblyFilter));
			List<Type> classes = VespaReflection.GetClassesFromAssemblies(assemblies);
			List<CommandMethod> staticCommands = VespaReflection.GetCommandMethodFromClasses<StaticCommandAttribute>(classes, VespaReflection.StaticMethodBindingFlags);
			foreach (CommandMethod staticCommand in staticCommands)
			{
				commandSet.RegisterCommand(staticCommand.properties, staticCommand.methodInfo);
			}

			List<AttributeMethod> attributeMethods = VespaReflection.GetAttributeMethodsFromClasses<CommandAutofillAttribute>(classes, VespaReflection.StaticMethodBindingFlags);
			foreach (AttributeMethod attributeMethod in attributeMethods)
			{
				if (!(attributeMethod.attribute is CommandAutofillAttribute autofillAttribute))
				{
					continue;
				}

				ParameterInfo[] parameters = attributeMethod.methodInfo.GetParameters();
				if (!commandSet.TryGetCommand(autofillAttribute.Key, out Command command))
				{
#if UNITY_EDITOR
					DevConsole.Log($"Autofill attribute of key \"{autofillAttribute.Key}\" was defined but there is no such command present.", Console.LogStyling.Warning);
#endif
				}
				else if (!attributeMethod.methodInfo.IsStatic)
				{
#if UNITY_EDITOR
					DevConsole.Log($"Autofill attribute of key \"{autofillAttribute.Key}\" was defined but the method is not static!", Console.LogStyling.Warning);
#endif
				}
				else if (parameters.Length != 1 || parameters[0].ParameterType != typeof(AutofillBuilder))
				{
#if UNITY_EDITOR
					DevConsole.Log($"Autofill attribute of key \"{autofillAttribute.Key}\" was defined but the method does not take the correct parameters.", Console.LogStyling.Warning);
					DevConsole.Log("Autofill methods must have exactly one parameter of type 'AutofillBuilder'");
#endif		
				}
				else if (attributeMethod.methodInfo.ReturnType != typeof(AutofillValue))
				{
#if UNITY_EDITOR
					DevConsole.Log($"Autofill attribute of key \"{autofillAttribute.Key}\" was defined but the return type is not of type `AutofillValue`", Console.LogStyling.Warning);
#endif		
				}
				else
				{
					command.SetAutofillMethod(attributeMethod.methodInfo);
				}
			}

#if UNITY_EDITOR // Only done in editor since the end user should not care about this message and not checking this dramatically improves performance.
			if (NativeSettings.Config.warnForNonstaticMethods)
			{
				List<CommandMethod> instancedMethods = VespaReflection.GetCommandMethodFromClasses<StaticCommandAttribute>(classes, VespaReflection.InstanceMethodBindingFlags);
				foreach (CommandMethod staticCommand in instancedMethods)
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

		private static Regex GetRegexFilter(AssemblyFilter assemblyFilter)
		{
			switch (assemblyFilter)
			{
				case AssemblyFilter.Standard:
					return StandardSearchRegex;
				case AssemblyFilter.Exhaustive:
					return ExhaustiveSearchRegex;
				default:
					throw new ArgumentOutOfRangeException(nameof(assemblyFilter), assemblyFilter, null);
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