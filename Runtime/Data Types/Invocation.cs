using System;
using System.Collections.Generic;
using System.Reflection;

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

		private readonly MethodInfo methodInfo;
		private readonly object[] methodParameters;

		private static readonly List<Argument> ArgumentList = new List<Argument>(32);

		/// <summary>
		/// Create an invocation object for a command based on a character string.
		/// </summary>
		/// <remarks>
		/// At this stage semicolons are handled as a standard character since this is a single command.
		/// </remarks>
		/// <param name="input">The text that was input for command invocation</param>
		public Invocation(string input)
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

				// Assemble an array of Arguments from the input.
				// We reuse a static readonly list to minimize garbage collection.
				ArgumentList.Clear();
				for (int i = 1; i < words.Count; i++)
				{
					ArgumentList.Add(new Argument(words[i]));
				}

				Argument[] arguments = ArgumentList.ToArray();
				ArgumentList.Clear();

				// See if there is a valid command for this invocation
				if (!Commands.TryGetCommand(inputKey, out command))
				{
					validState = ValidState.ErrorNoCommandFound;
					return;
				}

				// See if there is a valid method for this invocation
				if (!command.TryGetMethod(arguments, out methodInfo, out methodParameters))
				{
					validState = ValidState.ErrorNoMethodForParameters;
					return;
				}

				// After all the steps have occurred successfully we have a valid invocation.
				validState = ValidState.Valid;
			}
			catch
			{
				validState = ValidState.ErrorException;
			}
		}

		public InvokeResult RunInvocation(out Exception exception)
		{
			try
			{
				exception = null;
				if (validState != ValidState.Valid)
				{
					return InvokeResult.ErrorInvocationWasInvalid;
				}
				else if (command.Cheat && !DevConsole.CheatsEnabled)
				{
					return InvokeResult.ErrorRequiresCheats;
				}
				else
				{
					methodInfo.Invoke(null, methodParameters);
					return InvokeResult.Success;
				}
			}
			catch (Exception e)
			{
				exception = e.InnerException ?? e;
				return InvokeResult.Exception;
			}
		}

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
			ErrorNoMethodForParameters
		}

		public enum InvokeResult
		{
			Exception,
			Success,
			ErrorInvocationWasInvalid,
			ErrorRequiresCheats
		}
	}
}