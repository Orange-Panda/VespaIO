using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using Object = UnityEngine.Object;

namespace LMirman.VespaIO
{
	public class Invocation
	{
		/// <summary>
		/// If this object is valid or not.
		/// Can determine is valid by checking if equal to <see cref="ValidState.Valid"/>, if it is not this invocation is not valid.
		/// More details about why it is not valid is also defined by this type.
		/// </summary>
		public readonly ValidState validState;

		/// <summary>
		/// The input command key that was found from the input.
		/// </summary>
		public readonly string inputKey;

		/// <summary>
		/// The command that was found, if any, for this invocation.
		/// </summary>
		public readonly Command command;

		private readonly object targetObject;
		private readonly Command.InvocationType invocationType;
		private readonly PropertyInfo propertyInfo;
		private readonly FieldInfo fieldInfo;
		private readonly MethodInfo methodInfo;
		private readonly Word[] arguments;
		private readonly object[] methodParameters;

		private static readonly List<Word> ArgumentList = new List<Word>(32);

		/// <summary>
		/// Create an invocation object for a command based on a character string.
		/// </summary>
		/// <remarks>
		/// At this stage semicolons are handled as a standard character since this is a single command.
		/// </remarks>
		/// <param name="input">The text that was input for command invocation</param>
		/// <param name="commandSet">The command set to search for a target command for invocation.</param>
		public Invocation(string input, CommandSet commandSet)
		{
			try
			{
				List<Word> words = VespaFunctions.GetWordsFromString(input);

				// Error Case: There was nothing input!
				if (words.Count == 0)
				{
					validState = ValidState.ErrorEmpty;
					inputKey = string.Empty;
					return;
				}

				// The input key is always the first word.
				inputKey = words[0].text.CleanseKey();

				// See if there is a valid command for this invocation
				if (!commandSet.TryGetCommand(inputKey, out command))
				{
					validState = ValidState.ErrorNoCommandFound;
					return;
				}

				// Assemble an array of Arguments from the input.
				// We reuse a static readonly list to minimize garbage collection.
				ArgumentList.Clear();
				for (int i = 1; i < words.Count; i++)
				{
					ArgumentList.Add(words[i]);
				}

				// Find target object
				if (!command.IsStatic && ArgumentList.Count == 0)
				{
					validState = ValidState.ErrorNoInstanceTarget;
					return;
				}
				else if (!command.IsStatic)
				{
					Type declaringType = command.GetDeclaringType();
					string target = ArgumentList[0].text;
					ArgumentList.RemoveAt(0);
					targetObject = VespaFunctions.GetInstanceTarget(target, declaringType);

					if (!declaringType.IsSubclassOf(typeof(Object)))
					{
						validState = ValidState.ErrorInstanceIsNotUnityEngineObject;
						return;
					}
					else if (targetObject == null)
					{
						validState = ValidState.ErrorNoInstanceTarget;
						return;
					}
				}
				else if (command.IsStatic)
				{
					targetObject = null;
				}

				arguments = ArgumentList.ToArray();
				ArgumentList.Clear();

				// See if there is a valid method for this invocation
				invocationType = command.InvokeType;
				switch (invocationType)
				{
					case Command.InvocationType.Method:
						if (!command.TryGetMethod(arguments, out methodInfo, out methodParameters))
						{
							validState = ValidState.ErrorNoMethodForParameters;
							return;
						}

						break;
					case Command.InvocationType.Property:
						if (!command.TryGetPropertyInfo(out propertyInfo))
						{
							validState = ValidState.ErrorInvalidProperty;
							return;
						}

						break;
					case Command.InvocationType.Field:
						if (!command.TryGetFieldInfo(out fieldInfo))
						{
							validState = ValidState.ErrorInvalidProperty;
							return;
						}

						break;
					default:
						throw new ArgumentOutOfRangeException();
				}

				// After all the steps have occurred successfully we have a valid invocation.
				validState = ValidState.Valid;
			}
			catch
			{
				validState = ValidState.ErrorException;
			}
		}

		public InvokeResult RunInvocation(Console console, out Exception exception)
		{
			try
			{
				exception = null;
				if (validState != ValidState.Valid)
				{
					return InvokeResult.ErrorInvocationWasInvalid;
				}
				else if (!console.Enabled)
				{
					return InvokeResult.ErrorConsoleInactive;
				}
				else if (command.Cheat && !console.CheatsEnabled)
				{
					return InvokeResult.ErrorRequiresCheats;
				}
				else if (invocationType == Command.InvocationType.Method)
				{
					methodInfo.Invoke(targetObject, methodParameters);
					return InvokeResult.Success;
				}
				else if (invocationType == Command.InvocationType.Property)
				{
					InvokeProperty(console);
					return InvokeResult.Success;
				}
				else if (invocationType == Command.InvocationType.Field)
				{
					InvokeField(console);
					return InvokeResult.Success;
				}
				else
				{
					return InvokeResult.ErrorException;
				}
			}
			catch (Exception e)
			{
				exception = e.InnerException ?? e;
				return InvokeResult.ErrorException;
			}
		}

		private void InvokeProperty(Console console)
		{
			object prevValue = propertyInfo.CanRead ? propertyInfo.GetValue(targetObject) : string.Empty;
			if (!propertyInfo.CanWrite)
			{
				console.Log($"{propertyInfo.Name}: {prevValue} [READONLY]");
				return;
			}
			else if (arguments.Length == 0 && !propertyInfo.CanRead)
			{
				console.Log($"Property \"{propertyInfo.Name}\" can only have its value set.", Console.LogStyling.Error);
				return;
			}
			else if (arguments.Length == 0)
			{
				console.Log($"{propertyInfo.Name}: {prevValue}");
				return;
			}

			string inputValue = arguments[0].text;
			TypeConverter typeConverter = TypeDescriptor.GetConverter(propertyInfo.PropertyType);
			if (typeConverter.IsValid(inputValue))
			{
				object newValue = typeConverter.ConvertFrom(inputValue);
				propertyInfo.SetValue(targetObject, newValue);
				console.Log($"{propertyInfo.Name}: {prevValue} -> {newValue}");
			}
			else
			{
				console.Log($"Cannot set value of property \"{propertyInfo.Name}\" ({propertyInfo.PropertyType}) to \"{inputValue}\"", Console.LogStyling.Error);
			}
		}

		private void InvokeField(Console console)
		{
			object prevValue = fieldInfo.GetValue(targetObject);
			if (fieldInfo.IsInitOnly)
			{
				console.Log($"{fieldInfo.Name}: {prevValue} [READONLY]");
				return;
			}
			else if (arguments.Length == 0)
			{
				console.Log($"{fieldInfo.Name}: {prevValue}");
				return;
			}

			string inputValue = arguments[0].text;
			TypeConverter typeConverter = TypeDescriptor.GetConverter(fieldInfo.FieldType);
			if (typeConverter.IsValid(inputValue))
			{
				object newValue = typeConverter.ConvertFrom(inputValue);
				fieldInfo.SetValue(targetObject, newValue);
				console.Log($"{fieldInfo.Name}: {prevValue} -> {newValue}");
			}
			else
			{
				console.Log($"Cannot set value of property \"{fieldInfo.Name}\" ({fieldInfo.FieldType}) to \"{inputValue}\"", Console.LogStyling.Error);
			}
		}

		/// <summary>
		/// Communicates if an <see cref="Invocation"/> object is valid and, if not, why it is invalid.
		/// </summary>
		public enum ValidState
		{
			/// <summary>
			/// Default state for an invocation. Only output if a critical error occurred, assume Invalid.
			/// </summary>
			Unspecified,
			/// <summary>
			/// The invocation is valid and will function properly.
			/// </summary>
			Valid,
			/// <summary>
			/// The invocation is invalid due to an exception occuring during method generation.
			/// </summary>
			ErrorException,
			/// <summary>
			/// The invocation is invalid due to the input being null, empty, or whitespace.
			/// </summary>
			ErrorEmpty,
			/// <summary>
			/// The invocation is invalid because there was no command found for the input text.
			/// </summary>
			ErrorNoCommandFound,
			/// <summary>
			/// The invocation is invalid because there was no method possible for the parameters provided.
			/// </summary>
			ErrorNoMethodForParameters,
			/// <summary>
			/// The invocation is invalid because there was no valid property.
			/// </summary>
			ErrorInvalidProperty,
			/// <summary>
			/// The invocation is invalid because there was no target for the instance invocation.
			/// </summary>
			ErrorNoInstanceTarget,
			/// <summary>
			/// The invocation is invalid because the declaring type for an instance command does not inherit from MonoBehaviour
			/// </summary>
			ErrorInstanceIsNotUnityEngineObject
		}

		/// <summary>
		/// Communicates the outcome of <see cref="Invocation.RunInvocation"/>.
		/// </summary>
		public enum InvokeResult
		{
			/// <summary>
			/// The invocation was run successful. 
			/// </summary>
			Success,
			/// <summary>
			/// The invocation failed due to an exception occuring.
			/// </summary>
			ErrorException,
			/// <summary>
			/// The invocation failed due to the <see cref="Invocation"/> object being in an invalid state.
			/// </summary>
			/// <seealso cref="ValidState"/>
			/// <seealso cref="Invocation.validState"/>
			ErrorInvocationWasInvalid,
			/// <summary>
			/// The invocation failed due to the associated command requiring cheats enabled, but the running console did not have adequate permission.
			/// </summary>
			ErrorRequiresCheats,
			/// <summary>
			/// The invocation failed due to the console invoking this command being inactive.
			/// </summary>
			ErrorConsoleInactive
		}
	}
}