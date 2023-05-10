using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace LMirman.VespaIO
{
	/// <summary>
	/// A command is a definition of a method or set of methods that are meant to be invoked in a console by association with a single string key.<br/><br/>
	/// </summary>
	/// <remarks>
	/// When multiple methods use the same command key they will all be assigned to the same command.
	/// The 'best' method will be chosen from the set of methods when invoked based on the parameters provided.
	/// </remarks>
	[PublicAPI]
	public class Command
	{
		// ReSharper disable once InconsistentNaming
		/// <summary>
		/// The key that points to this command in console.
		/// </summary>
		/// <remarks>
		/// This is immutable to avoid conflicts with other command definitions and to preserve key definitions in command dictionaries that contain this entry.
		/// </remarks>
		public readonly string Key;

		/// <summary>
		/// Used to build the guide property without as much garbage collection overhead as string concatenation
		/// </summary>
		private static readonly StringBuilder GuideBuilder = new StringBuilder();

		/// <summary>
		/// The name or title of the command in plain english.
		/// </summary>
		/// <example>
		/// Good command titles might be:<br/>
		/// - "Print Debug Information"<br/>
		/// - "Grant Item to Player"<br/>
		/// - "Play Mission"<br/>
		/// - "Teleport Player to Location"
		/// </example>
		/// <remarks>
		/// Not to be confused with <see cref="Key"/> which is the unique identifier for this command.<br/>
		/// If no name is provided by a command definition will default to the first method name found.
		/// </remarks>
		public string Name { get; private set; }

		/// <summary>
		/// The description of the command.
		/// </summary>
		/// <example>
		/// Good command descriptions might be:<br/>
		/// - "Prints information about the system playing the game"<br/>
		/// - "Immediately grant an item to the player inventory"<br/>
		/// - "Set and play a particular mission for the loaded save file"<br/>
		/// - "Immediately teleport the player to the provided location"<br/>
		/// </example>
		/// <remarks>
		/// Should be a brief explanation of the functionality of the command without any detailed explanation of functionality.
		/// </remarks>
		public string Description { get; private set; } = string.Empty;
		
		/// <summary>
		/// The guide to usages of the command explaining to the user how parameters should be provided to the command.
		/// </summary>
		/// <example>
		/// Guides are usually generated in the following format: `Usage: command_name [WORD] [FLOAT]`.
		/// </example>
		/// <remarks>
		/// The guide is automatically generated when methods are added or removed from this command.
		/// It is not currently possible to override the way by which the guide is generated.
		/// </remarks>
		public string Guide { get; private set; }
		
		/// <summary>
		/// If this command is a cheat command.<br/><br/>
		/// When true the command will be omitted from help and autofill while cheats are not enabled in addition to be unable to be executed in the console until enabling cheats.
		/// </summary>
		/// <example>
		/// Cheat commands are likely to be commands that execute gameplay impacting code not otherwise accessible such as:<br/>
		/// - A command that gives the player free items, experience, or other progression mechanics<br/>
		/// - A command that manipulates the player functionality<br/>
		/// - A command that creates targets or objects in the scene
		/// </example>
		/// <remarks>
		/// If any single method of the command is marked as a cheat the entire command is considered a cheat.
		/// Thus, it is not possible to provide non-cheat functionality to a cheat command directly and must be added as a separate command.
		/// <br/><br/>
		/// Note: Due to the nature of code still existing on the end user's system there is no absolute guarantee that the user never executes Cheat commands out of context.<br/>
		/// Therefore it is recommended that you consider the following:<br/>
		/// - Only write commands that you consider acceptable for users to execute (i.e avoid including commands that cheat on a server)<br/>
		/// - Implement platform directives such as `#if UNITY_EDITOR` around cheat commands you never want accessible to the end user<br/>
		/// - Understand the fact that players may modify code, especially JIT code, and execute code that you don't intend them to.<br/>
		/// </remarks>
		public bool Cheat { get; private set; }
		
		/// <summary>
		/// If this command should be accessible by auto-fill and help functionalities of the console.<br/><br/>
		/// When true will be omitted from being displayed in such sections.
		/// </summary>
		/// <example>
		/// Hidden commands are likely to be commands that are easter eggs or commands that are not useful to the end user such as:<br/>
		/// - A command that changes the skin of the UI for fun without any gameplay consequences<br/>
		/// - A command that plays a sound effect<br/>
		/// - A command that shows a secret message to the user
		/// </example>
		/// <remarks>
		/// Hidden commands do not have any impact on boundaries on their execution what so over.
		/// Assuming the user knows the command exists they can execute it as if it were not hidden.
		/// </remarks>
		public bool Hidden { get; private set; }
		
		/// <summary>
		/// The priority of this command relative to other commands.
		/// The higher this value, the earlier this command should appear in a sorted list of commands.
		/// </summary>
		/// <example>
		/// Commands that are particularly useful or frequently used may justify a high priority such as:<br/>
		/// - A cheat that teleports the player<br/>
		/// - A cheat that grants items<br/>
		/// - A command that sets volume<br/>
		/// Commands that are niche or rarely useful may justify a negative priority such as:<br/>
		/// - A cheat that only works in a specific level<br/>
		/// - A command that sets an internal value that only developers or testers would care about<br/>
		/// </example>
		/// <remarks>
		/// Native commands will only ever use the 0 to 127 inclusive range.
		/// If you want your commands to always sort before or after native commands consider these values.
		/// </remarks>
		public int ManualPriority { get; private set; }

		private readonly List<MethodInfo> methods = new List<MethodInfo>();
		
		/// <summary>
		/// True if there is at least one method defined for this command. False if there are none.
		/// </summary>
		public bool HasMethod => methods.Count > 0;

		/// <summary>
		/// Create a brand new command by defining attributes from a <paramref name="attribute"/> and adding method <paramref name="method"/>.
		/// </summary>
		/// <param name="attribute">The properties that define this command such as title, description, and cheat properties.</param>
		/// <param name="method">The first method to be added to this command definition.</param>
		public Command(StaticCommandAttribute attribute, MethodInfo method)
		{
			Key = attribute.Key.CleanseKey();
			SetAttributeProperties(attribute);
			AddMethod(method);
		}

		/// <summary>
		/// Set attributes for this command based on a provided <see cref="StaticCommandAttribute"/>.
		/// </summary>
		/// <remarks>
		/// - Will be rejected if the key does not match <see cref="Key"/><br/>
		/// - Will overwrite <see cref="Name"/> if not null or white space<br/>
		/// - Will overwrite <see cref="Description"/> if not null or white space<br/>
		/// - Will permanently mark this command as <see cref="Hidden"/> and/or <see cref="Cheat"/> if either are set (independently).<br/>
		/// - Will overwrite <see cref="ManualPriority"/> if it is non-zero
		/// </remarks>
		/// <param name="attribute">The properties that define this command such as title, description, and cheat properties.</param>
		public void SetAttributeProperties(StaticCommandAttribute attribute)
		{
			string attributeKey = attribute.Key.CleanseKey();
			if (attributeKey != Key)
			{
				DevConsole.Log($"Error: There was an attempt to add a method to command \"{Key}\" but the key \"{attributeKey}\" did not match");
				return;
			}

			// Set attribute name
			if (!string.IsNullOrWhiteSpace(attribute.Name))
			{
				Name = attribute.Name;
			}

			// Set description
			if (!string.IsNullOrWhiteSpace(attribute.Description))
			{
				Description = attribute.Description;
			}

			Cheat = Cheat | attribute.Cheat;
			Hidden = Hidden | attribute.Hidden;
			ManualPriority = attribute.ManualPriority != default ? attribute.ManualPriority : ManualPriority;
		}

		/// <summary>
		/// Add a method to this command definition and automatically update <see cref="Guide"/> to include it.
		/// </summary>
		/// <param name="method">The method to add to the methods list for this command.</param>
		public void AddMethod(MethodInfo method)
		{
			if (string.IsNullOrWhiteSpace(Name))
			{
				Name = method.Name;
			}

			methods.Add(method);
			UpdateGuide();
		}

		/// <summary>
		/// Remove a particular method from this command.
		/// </summary>
		/// <param name="methodInfo">The method to remove from this command</param>
		/// <remarks>This will <b>not</b> unset any modifications to command properties this method applied such as Name, Description, Cheat, or Hidden properties.</remarks>
		internal void RemoveMethod(MethodInfo methodInfo)
		{
			methods.Remove(methodInfo);
			UpdateGuide();
		}

		/// <summary>
		/// Get all methods that have any possibility to be invoked by a set of arguments.
		/// </summary>
		/// <remarks>
		/// This omits methods that contain more non-optional parameters than arguments provided since there is no possible way to invoke them without making assumptions.<br/>
		/// This <b>does</b> include methods that have less parameters than arguments since such methods can simply not use the extra arguments if they so choose.
		/// </remarks>
		public List<MethodInfo> GetValidMethods(Argument[] arguments)
		{
			List<MethodInfo> validMethods = new List<MethodInfo>();
			foreach (MethodInfo method in methods)
			{
				int requiredParams = 0;
				ParameterInfo[] parameters = method.GetParameters();
				foreach (ParameterInfo parameter in parameters)
				{
					if (!parameter.IsOptional)
					{
						requiredParams++;
					}
				}

				if (arguments.Length >= requiredParams)
				{
					validMethods.Add(method);
				}
			}

			return validMethods;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="arguments"></param>
		/// <param name="longString"></param>
		/// <param name="methodInfo"></param>
		/// <param name="methodParameters"></param>
		/// <returns></returns>
		public bool TryGetMethod(Argument[] arguments, LongString longString, out MethodInfo methodInfo, out object[] methodParameters)
		{
			// Get all methods that contain at least
			List<MethodInfo> validMethods = GetValidMethods(arguments);

			int bestMethodValue = 0;
			int bestMethodArgCount = -1;
			MethodInfo bestMethod = null;
			MethodInfo longStringMethod = null;
			foreach (MethodInfo method in validMethods)
			{
				// Value is determined by the number of arguments that are a great match for the parameters of the method being checked 
				// A great match is an argument being the exact type expected for the method that specifically isn't a string
				// ---
				// The reason we ignore string is because if there were MethodA(string) and MethodA(float) for the same command...
				// we give priority to the MethodA(float) since a string parameter will practically always be fulfilled while...
				// a float parameter is much less likely to be fulfilled, therefore when it is we should use it over string.
				int value = 0;

				// True when all arguments can be cast to the parameter type of the method being checked
				bool canCastAll = true;
				ParameterInfo[] parameters = method.GetParameters();

				// Go through all parameters to make sure there is a valid type for each one.
				for (int i = 0; i < parameters.Length && i < arguments.Length; i++)
				{
					Type parameterType = parameters[i].ParameterType;
					if (parameterType != typeof(LongString) && parameterType != typeof(string))
					{
						value++;
					}

					if (!arguments[i].HasValidType(parameterType))
					{
						canCastAll = false;
						break;
					}
				}

				if (canCastAll && (parameters.Length > bestMethodArgCount || (parameters.Length >= bestMethodArgCount && value >= bestMethodValue)))
				{
					bestMethod = method;
					bestMethodArgCount = parameters.Length;
					bestMethodValue = value;
				}

				if (parameters.Length == 1 && parameters[0].ParameterType == typeof(LongString))
				{
					longStringMethod = method;
				}
			}

			// Execute the best method found in the previous step, if one is found
			if (longStringMethod != null && !string.IsNullOrWhiteSpace(longString) && (bestMethod == null || bestMethodArgCount == 0))
			{
				methodInfo = longStringMethod;
				methodParameters = new object[] { longString };
				return true;
			}
			else if (bestMethod != null)
			{
				methodParameters = new object[bestMethodArgCount];
				ParameterInfo[] parameters = bestMethod.GetParameters();
				for (int i = 0; i < bestMethodArgCount; i++)
				{
					methodParameters[i] = i < arguments.Length ? arguments[i].GetValue(parameters[i].ParameterType) : parameters[i].DefaultValue;
				}

				methodInfo = bestMethod;
				return true;
			}
			// No methods support the user input parameters.
			else
			{
				methodInfo = null;
				methodParameters = null;
				return false;
			}
		}

		private void UpdateGuide()
		{
			GuideBuilder.Clear();
			for (int i = 0; i < methods.Count; i++)
			{
				MethodInfo method = methods[i];
				GuideBuilder.Append("Usage: ");
				GuideBuilder.Append(Key);

				ParameterInfo[] parameters = method.GetParameters();
				foreach (ParameterInfo parameter in parameters)
				{
					GuideBuilder.Append(' ');
					GuideBuilder.Append(TranslateParameter(parameter.ParameterType));
				}

				if (i < methods.Count - 1)
				{
					GuideBuilder.Append('\n');
				}
			}

			Guide = GuideBuilder.ToString();
		}

		private static string TranslateParameter(Type type)
		{
			if (type == typeof(int))
			{
				return "[INTEGER]";
			}
			else if (type == typeof(float))
			{
				return "[FLOAT]";
			}
			else if (type == typeof(string))
			{
				return "[WORD]";
			}
			else if (type == typeof(LongString))
			{
				return "[PHRASE]";
			}
			else
			{
				return "[INVALID/UNKNOWN]";
			}
		}
	}
}