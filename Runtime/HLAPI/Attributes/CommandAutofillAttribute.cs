using JetBrains.Annotations;
using System;

namespace LMirman.VespaIO
{
	/// <summary>
	/// When this attribute is applied to a <b>static</b> method it will be assigned as the <see cref="Command.AutofillMethod"/> for the command with the same <see cref="Key"/>
	/// </summary>
	/// <remarks>
	/// The method this is attached to should have this exact signature `[public/private] static AutofillValue MethodName(AutofillBuilder builder)`
	/// </remarks>
	/// <example>
	/// An example autofill used in the native help function to autofill command keys:
	/// <code>
	/// [CommandAutofill("help")]
	/// private static AutofillValue GetHelpAutofillValue(AutofillBuilder autofillBuilder)
	/// {
	///		// If we are not checking in the first parameter we have nothing to suggest so return null
	/// 	if (autofillBuilder.RelevantParameterIndex != 0)
	/// 	{
	/// 		return null;
	/// 	}
	///
	///		// Get the current text input for this parameter. In this case it will always be the first since we return null earlier if it were not.
	/// 	string relevantWord = autofillBuilder.GetRelevantWordText().CleanseKey();
	///		// Iterate over each command the user has access to and find one that matches the user input
	///		foreach (Command command in Commands.commandSet.GetPublicCommands())
	/// 	{
	/// 		string commandKey = command.Key;
	///			// Take special note of the `!autofillBuilder.Exclusions.Contains(commandKey)` which omits autofill values which have already been used.
	///			if (commandKey.StartsWith(relevantWord) &amp;&amp; !autofillBuilder.Exclusions.Contains(commandKey))
	/// 		{
	/// 			return autofillBuilder.CreateAutofill(commandKey);
	/// 		}
	/// 	}
	/// 
	/// 	return null;
	/// }
	/// </code>
	/// </example>
	[PublicAPI]
	[MeansImplicitUse]
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
	public class CommandAutofillAttribute : Attribute
	{
		/// <inheritdoc cref="ICommandProperties.Key"/>
		public string Key { get; private set; }

		public CommandAutofillAttribute(string key)
		{
			Key = key.CleanseKey();
		}
	}
}