using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
		private FieldInfo fieldInfo;
		private PropertyInfo propertyInfo;
		private readonly List<MethodInfo> methods = new List<MethodInfo>();
		private readonly string key;
		/// <summary>
		/// Used to build the guide property without as much garbage collection overhead as string concatenation
		/// </summary>
		private static readonly StringBuilder GuideBuilder = new StringBuilder();

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
		/// The invocation type of this command.
		/// Determines certain immutable properties of how this command behaves.
		/// </summary>
		public InvocationType InvokeType { get; }
		/// <summary>
		/// If the target of invocation is static.
		/// This value is immutable and is determined from the first invocation target set when this command is created.
		/// </summary>
		public bool IsStatic { get; }
		/// <summary>
		/// The <see cref="Type"/> that the invocation target of this command is defined within.
		/// Usually used for non-static commands to find targets.
		/// </summary>
		public Type DeclaringType { get; private set; }
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
		/// True if there is at least one method defined for this command. False if there are none.
		/// </summary>
		public bool HasMethod => methods.Count > 0;

		#region Constructors
		/// <summary>
		/// Create a new Method invocation type command.
		/// </summary>
		/// <param name="properties">The properties that define this command such as title, description, and cheat properties.</param>
		/// <param name="method">The first method to be added to this command definition.</param>
		public Command(ICommandProperties properties, MethodInfo method)
		{
			key = properties.Key.CleanseKey();
			InvokeType = InvocationType.Method;
			IsStatic = method.IsStatic;
			SetAttributeProperties(properties);
			AddMethod(method);
		}

		/// <summary>
		/// Create a new Property invocation type command.
		/// </summary>
		/// <param name="properties">The properties that define this command such as title, description, and cheat properties.</param>
		/// <param name="property">The property that is the target of this command.</param>
		public Command(ICommandProperties properties, PropertyInfo property)
		{
			key = properties.Key.CleanseKey();
			InvokeType = InvocationType.Property;
			IsStatic = property.GetAccessors()[0].IsStatic;
			SetAttributeProperties(properties);
			SetPropertyTarget(property);
		}

		/// <summary>
		/// Create a new Field invocation type command.
		/// </summary>
		/// <param name="properties">The properties that define this command such as title, description, and cheat properties.</param>
		/// <param name="field">The field that is the target of this command.</param>
		public Command(ICommandProperties properties, FieldInfo field)
		{
			key = properties.Key.CleanseKey();
			InvokeType = InvocationType.Field;
			IsStatic = field.IsStatic;
			SetAttributeProperties(properties);
			SetFieldTarget(field);
		}
		#endregion

		#region Setters
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

		/// <summary>
		/// Add a method to this command definition and automatically update <see cref="Guide"/> to include it.
		/// </summary>
		/// <param name="method">The method to add to the methods list for this command.</param>
		public void AddMethod(MethodInfo method)
		{
			if (InvokeType != InvocationType.Method)
			{
				Debug.LogError($"There was an attempt to add method \"{method.Name}\" to command \"{key}\" but command is of type {InvokeType}.");
				return;
			}

			bool isStatic = method.IsStatic;
			if (IsStatic != isStatic)
			{
				Debug.LogError($"There was an attempt to set {(isStatic ? "static" : "non-static")} method \"{method.Name}\" to {(IsStatic ? "static" : "non-static")} command \"{key}\".");
				return;
			}

			if (string.IsNullOrWhiteSpace(Name))
			{
				Name = method.Name.NicifyName();
			}

			if (string.IsNullOrWhiteSpace(Description))
			{
				Description = $"Invokes method \"{method.Name}\" in \"{method.DeclaringType}\"";
			}

			methods.Add(method);
			DeclaringType = method.DeclaringType;
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

		public void SetPropertyTarget(PropertyInfo property)
		{
			if (InvokeType != InvocationType.Property)
			{
				Debug.LogError($"There was an attempt to set property \"{property.Name}\" to command \"{key}\" but command is of type {InvokeType}.");
				return;
			}

			bool isStatic = property.GetAccessors()[0].IsStatic;
			if (IsStatic != isStatic)
			{
				Debug.LogError($"There was an attempt to set {(isStatic ? "static" : "non-static")} property \"{property.Name}\" to {(IsStatic ? "static" : "non-static")} command \"{key}\".");
				return;
			}

			if (string.IsNullOrWhiteSpace(Name))
			{
				Name = property.Name.NicifyName();
			}

			if (string.IsNullOrWhiteSpace(Description))
			{
				string prefix = GetAccessString(property.CanRead, property.CanWrite);
				Description = $"{prefix} property \"{property.Name}\" in \"{property.DeclaringType}\"";
			}

			propertyInfo = property;
			DeclaringType = property.DeclaringType;
			if (propertyInfo.CanRead)
			{
				AutofillMethod = delegate(AutofillBuilder builder)
				{
					try
					{
						if (builder.RelevantParameterIndex != 0)
						{
							return null;
						}

						return builder.CreateAutofill(propertyInfo.GetMethod.Invoke(builder.InstanceTarget, new object[] { })?.ToString());
					}
					catch
					{
						return null;
					}
				};
			}

			UpdateGuide();
		}

		public void SetFieldTarget(FieldInfo field)
		{
			if (InvokeType != InvocationType.Field)
			{
				Debug.LogError($"There was an attempt to set field \"{field.Name}\" to command \"{key}\" but command is of type {InvokeType}.");
				return;
			}

			bool isStatic = field.IsStatic;
			if (IsStatic != isStatic)
			{
				Debug.LogError($"There was an attempt to set {(isStatic ? "static" : "non-static")} field \"{field.Name}\" to {(IsStatic ? "static" : "non-static")} command \"{key}\".");
				return;
			}

			if (string.IsNullOrWhiteSpace(Name))
			{
				Name = field.Name.NicifyName();
			}

			if (string.IsNullOrWhiteSpace(Description))
			{
				string prefix = GetAccessString(true, !field.IsInitOnly);
				Description = $"{prefix} field \"{field.Name}\" in \"{field.DeclaringType}\"";
			}

			fieldInfo = field;
			DeclaringType = field.DeclaringType;
			AutofillMethod = delegate(AutofillBuilder builder)
			{
				try
				{
					if (builder.RelevantParameterIndex != 0)
					{
						return null;
					}

					return builder.CreateAutofill(fieldInfo.GetValue(builder.InstanceTarget)?.ToString());
				}
				catch
				{
					return null;
				}
			};

			UpdateGuide();
		}
		#endregion

		#region Getters
		/// <summary>
		/// Get all methods that have any possibility to be invoked by a set of arguments.
		/// </summary>
		/// <remarks>
		/// This omits methods that contain more non-optional parameters than arguments provided since there is no possible way to invoke them without making assumptions.<br/>
		/// This <b>does</b> include methods that have less parameters than arguments since such methods can simply not use the extra arguments if they so choose.<br/>
		/// Always includes specifically the `Argument[]` method
		/// </remarks>
		public List<MethodInfo> GetValidMethods(Word[] arguments)
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

		public bool TryGetMethod(Word[] arguments, out MethodInfo methodInfo, out object[] methodParameters)
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

					TypeConverter typeConverter = TypeDescriptor.GetConverter(parameterType);
					if (!typeConverter.IsValid(arguments[i].text) || (arguments[i].context.HasFlag(Word.Context.IsLiteral) && parameterType != typeof(string)))
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
					TypeConverter typeConverter = TypeDescriptor.GetConverter(parameters[i].ParameterType);
					methodParameters[i] = i < arguments.Length ? typeConverter.ConvertFrom(arguments[i].text) : parameters[i].DefaultValue;
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
			if (InvokeType == InvocationType.Property && propertyInfo != null)
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

		public bool TryGetFieldInfo(out FieldInfo field)
		{
			if (InvokeType == InvocationType.Field && fieldInfo != null)
			{
				field = fieldInfo;
				return true;
			}
			else
			{
				field = null;
				return false;
			}
		}

		private static bool GetIsArgumentArrayMethod(ParameterInfo[] parameters)
		{
			return parameters.Length == 1 && parameters[0].ParameterType == typeof(Word[]);
		}
		#endregion

		private string GetAccessString(bool canRead, bool canWrite)
		{
			if (canRead && canWrite)
			{
				return "Get and set";
			}
			else if (canRead)
			{
				return "Get";
			}
			else if (canWrite)
			{
				return "Set";
			}
			else
			{
				return string.Empty;
			}
		}

		private void UpdateGuide()
		{
			GuideBuilder.Clear();
			GuideBuilder.AppendFormat("[{0}] \"{1}\"\n", key, Name);
			GuideBuilder.AppendFormat(" - {0}\n", Description);

			switch (InvokeType)
			{
				case InvocationType.Method:
					for (int i = 0; i < methods.Count; i++)
					{
						MethodInfo method = methods[i];
						GuideBuilder.AppendFormat("  * Usage: {0}", key);
						AppendSubject();

						ParameterInfo[] parameters = method.GetParameters();
						foreach (ParameterInfo parameter in parameters)
						{
							GuideBuilder.Append($" [{parameter.ParameterType}]");
						}

						if (i < methods.Count - 1)
						{
							GuideBuilder.Append('\n');
						}
					}

					break;
				case InvocationType.Property:
					if (propertyInfo.CanRead)
					{
						GuideBuilder.AppendFormat("  * Get Usage: {0}", key);
						AppendSubject();

						if (propertyInfo.CanWrite)
						{
							GuideBuilder.Append('\n');
						}
					}

					if (propertyInfo.CanWrite)
					{
						GuideBuilder.AppendFormat("  * Set Usage: {0}", key);
						AppendSubject();
						GuideBuilder.Append($" [{propertyInfo.PropertyType}]");
					}

					break;
				case InvocationType.Field:
					GuideBuilder.AppendFormat("  * Get Usage: {0}", key);
					AppendSubject();

					if (!fieldInfo.IsInitOnly)
					{
						GuideBuilder.Append('\n');
						GuideBuilder.AppendFormat("  * Set Usage: {0}", key);
						AppendSubject();
						GuideBuilder.Append($" [{fieldInfo.FieldType}]");
					}

					break;
				default:
					GuideBuilder.Append($"ERROR: Unable to generate guide for invoke type {InvokeType}.");
					break;
			}

			Guide = GuideBuilder.ToString();

			void AppendSubject()
			{
				if (!IsStatic)
				{
					GuideBuilder.Append(" [Object Name]");
				}
			}
		}

		public enum InvocationType
		{
			Method,
			Property,
			Field
		}
	}
}