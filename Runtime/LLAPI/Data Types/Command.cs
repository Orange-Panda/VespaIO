using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

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
	public class Command : ICommandProperties
	{
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
		public InvocationType InvokeType { get; private set; }
		public bool IsStatic { get; private set; }

		// ReSharper disable once InconsistentNaming
		/// <inheritdoc cref="ICommandProperties.Key"/>
		public string Key => key;
		/// <inheritdoc cref="ICommandProperties.Name"/>
		public string Name { get; private set; }
		/// <inheritdoc cref="ICommandProperties.Description"/>
		public string Description { get; private set; } = string.Empty;
		/// <inheritdoc cref="ICommandProperties.Cheat"/>
		public bool Cheat { get; private set; }
		/// <inheritdoc cref="ICommandProperties.Hidden"/>
		public bool Hidden { get; private set; }
		/// <inheritdoc cref="ICommandProperties.ManualPriority"/>
		public int ManualPriority { get; private set; }
		/// <summary>
		/// A function that is invoked by the console to try to find values to autofill for parameters
		/// </summary>
		/// <remarks>
		/// You are expected to return a value that will <b>completely replace</b> the relevant word.
		/// If there is no suitable autofill value for the word that is being input return `null` to tell the console there is no relevant autofill value.
		/// </remarks>
		public Func<AutofillBuilder, AutofillValue> AutofillMethod { get; private set; }

		/// <summary>
		/// Used to build the guide property without as much garbage collection overhead as string concatenation
		/// </summary>
		private static readonly StringBuilder GuideBuilder = new StringBuilder();

		private PropertyInfo propertyInfo;
		private readonly List<MethodInfo> methods = new List<MethodInfo>();
		private readonly string key;

		/// <summary>
		/// True if there is at least one method defined for this command. False if there are none.
		/// </summary>
		public bool HasMethod => methods.Count > 0;

		/// <summary>
		/// Create a brand new command by defining attributes from a <paramref name="properties"/> and adding method <paramref name="method"/>.
		/// </summary>
		/// <param name="properties">The properties that define this command such as title, description, and cheat properties.</param>
		/// <param name="method">The first method to be added to this command definition.</param>
		public Command(ICommandProperties properties, MethodInfo method)
		{
			key = properties.Key.CleanseKey();
			SetAttributeProperties(properties);
			AddMethod(method);
		}

		public Command(ICommandProperties properties, PropertyInfo property)
		{
			key = properties.Key.CleanseKey();
			SetAttributeProperties(properties);
			SetPropertyTarget(property);
		}

		/// <summary>
		/// Set attributes for this command based on a provided <see cref="ICommandProperties"/>.
		/// </summary>
		/// <remarks>
		/// - Will be rejected if the key does not match <see cref="key"/><br/>
		/// - Will overwrite <see cref="Name"/> if not null or white space<br/>
		/// - Will overwrite <see cref="Description"/> if not null or white space<br/>
		/// - Will permanently mark this command as <see cref="Hidden"/> and/or <see cref="Cheat"/> if either are set (independently).<br/>
		/// - Will overwrite <see cref="ManualPriority"/> if it is non-zero<br/>
		/// - Will overwrite <see cref="AutofillMethod"/> if it is not null
		/// </remarks>
		/// <param name="properties">The properties that define this command such as title, description, and cheat properties.</param>
		public void SetAttributeProperties(ICommandProperties properties)
		{
			string attributeKey = properties.Key.CleanseKey();
			if (attributeKey != key)
			{
#if UNITY_EDITOR
				Debug.LogWarning($"There was an attempt to add a method to command \"{key}\" but the key \"{attributeKey}\" did not match");
#endif
				return;
			}

			// Set attribute name
			if (!string.IsNullOrWhiteSpace(properties.Name))
			{
				Name = properties.Name;
			}

			// Set description
			if (!string.IsNullOrWhiteSpace(properties.Description))
			{
				Description = properties.Description;
			}

			Cheat = Cheat | properties.Cheat;
			Hidden = Hidden | properties.Hidden;
			ManualPriority = properties.ManualPriority != default ? properties.ManualPriority : ManualPriority;
		}

		public void SetAutofillMethod(MethodInfo autofillMethodInfo)
		{
			if (autofillMethodInfo != null)
			{
				AutofillMethod = autofillMethodInfo.CreateDelegate(typeof(Func<AutofillBuilder, AutofillValue>)) as Func<AutofillBuilder, AutofillValue>;
			}
		}

		public void SetPropertyTarget(PropertyInfo property)
		{
			if (string.IsNullOrWhiteSpace(Name))
			{
				Name = property.Name;
			}

			propertyInfo = property;
			if (propertyInfo.CanRead)
			{
				AutofillMethod = delegate(AutofillBuilder builder)
				{
					try
					{
						return builder.CreateOverwriteAutofill(propertyInfo.GetMethod.Invoke(builder.InstanceTarget, new object[] { })?.ToString());
					}
					catch
					{
						return null;
					}
				};
			}

			InvokeType = InvocationType.Property;
			IsStatic = propertyInfo.GetAccessors()[0].IsStatic;
			UpdateGuide();
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

			InvokeType = InvocationType.Method;
			methods.Add(method);
			IsStatic = method.IsStatic;
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
		/// This <b>does</b> include methods that have less parameters than arguments since such methods can simply not use the extra arguments if they so choose.<br/>
		/// Always includes specifically the `Argument[]` method
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

				if (arguments.Length >= requiredParams || GetIsArgumentArrayMethod(parameters))
				{
					validMethods.Add(method);
				}
			}

			return validMethods;
		}

		public bool TryGetMethod(Argument[] arguments, out MethodInfo methodInfo, out object[] methodParameters)
		{
			if (InvokeType != InvocationType.Method)
			{
				methodInfo = null;
				methodParameters = null;
				return false;
			}

			// Get all methods that contain at least
			List<MethodInfo> validMethods = GetValidMethods(arguments);

			int bestMethodValue = 0;
			int bestMethodArgCount = -1;
			MethodInfo bestMethod = null;
			MethodInfo pureMethod = null;
			foreach (MethodInfo method in validMethods)
			{
				ParameterInfo[] parameters = method.GetParameters();
				if (GetIsArgumentArrayMethod(parameters))
				{
					pureMethod = method;
					break;
				}

				// Value is determined by the number of arguments that are a great match for the parameters of the method being checked 
				// A great match is an argument being the exact type expected for the method that specifically isn't a string
				// ---
				// The reason we ignore string is because if there were MethodA(string) and MethodB(float) for the same command...
				// we give priority to the MethodB(float) since a string parameter will practically always be fulfilled while...
				// a float parameter is much less likely to be fulfilled, therefore when it is fulfilled we should use it over string.
				int value = 0;

				// True when all arguments can be cast to the parameter type of the method being checked
				bool canCastAll = true;

				// Go through all parameters to make sure there is a valid type for each one.
				for (int i = 0; i < parameters.Length && i < arguments.Length; i++)
				{
					Type parameterType = parameters[i].ParameterType;
					if (parameterType != typeof(string))
					{
						value++;
					}

					if (!arguments[i].HasValidType(parameterType))
					{
						canCastAll = false;
						break;
					}
				}

				bool fulfillsMoreTotalParameters = parameters.Length > bestMethodArgCount;
				bool fulfillsMoreValuableParameters = parameters.Length >= bestMethodArgCount && value >= bestMethodValue;
				if (canCastAll && (fulfillsMoreTotalParameters || fulfillsMoreValuableParameters))
				{
					bestMethod = method;
					bestMethodArgCount = parameters.Length;
					bestMethodValue = value;
				}
			}

			if (pureMethod != null)
			{
				methodInfo = pureMethod;
				methodParameters = new object[] { arguments };
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
			else
			{
				methodInfo = null;
				methodParameters = null;
				return false;
			}
		}

		public bool TryGetPropertyInfo(out PropertyInfo property)
		{
			if (InvokeType == InvocationType.Property)
			{
				property = propertyInfo;
				return true;
			}
			else
			{
				property = null;
				return false;
			}
		}

		public Type GetDeclaringType()
		{
			switch (InvokeType)
			{
				case InvocationType.Method:
					return methods[0].DeclaringType;
				case InvocationType.Property:
					return propertyInfo.DeclaringType;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private void UpdateGuide()
		{
			GuideBuilder.Clear();
			GuideBuilder.AppendFormat("{0} - \"{1}\"\n", key, Name);

			for (int i = 0; i < methods.Count; i++)
			{
				MethodInfo method = methods[i];
				GuideBuilder.Append("Usage: ");
				GuideBuilder.Append(key);

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

		private static bool GetIsArgumentArrayMethod(ParameterInfo[] parameters)
		{
			return parameters.Length == 1 && parameters[0].ParameterType == typeof(Argument[]);
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
				return "[STRING]";
			}
			else if (type == typeof(Argument[]))
			{
				return "[ARGUMENTS]";
			}
			else
			{
				return "[INVALID/UNKNOWN]";
			}
		}

		public enum InvocationType
		{
			Method,
			Property
		}
	}
}