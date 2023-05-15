using JetBrains.Annotations;
using System.Collections.Generic;

namespace LMirman.VespaIO
{
	/// <summary>
	/// A helper class that makes creating autofill values much more intuitive
	/// </summary>
	[PublicAPI]
	public class AutofillBuilder
	{
		/// <summary>
		/// A hashset of strings that should not be included in autofill results.
		/// </summary>
		/// <remarks>
		/// Usually these strings are values of previous autofill inputs.
		/// Not checking if your return value is present in this hash set will cause cycling through values to not function correctly.
		/// </remarks>
		public HashSet<string> Exclusions { get; internal set; }
		/// <summary>
		/// The <b>parameter</b> index of the word currently being input by the user.
		/// </summary>
		/// <remarks>
		/// This value is the index of the parameter that actually gets passed to the invocation of the command, where the 0th index is the first parameter.
		/// </remarks>
		/// <example>
		/// Consider the following command: `static_command "First" "Second"`<br/>
		/// The parameter indices for this command invocation is [-1] [0] [1]<br/><br/>
		/// Consider the following command: `instance_command "Target" "First" "Second"`<br/>
		/// The parameter indices for this command invocation is [-2] [-1] [0] [1]
		/// </example>
		public int RelevantParameterIndex { get; internal set; }
		/// <summary>
		/// The <b>word</b> index of the word currently being input by the user.
		/// </summary>
		/// <remarks>
		/// This value is the index of the relevant word relative to all words, where 0th index is the command name itself.
		/// </remarks>
		/// <example>
		/// Consider the following command: `static_command "First" "Second"`<br/>
		/// The parameter indices for this command invocation is [0] [1] [2]<br/><br/>
		/// Consider the following command: `instance_command "Target" "First" "Second"`<br/>
		/// The parameter indices for this command invocation is [0] [1] [2] [3]
		/// </example>
		public int RelevantWordIndex { get; internal set; }
		/// <summary>
		/// The character index of the word that is currently being input by the user.
		/// </summary>
		/// <remarks>
		/// This character index is global to the entire message in the console input.
		/// </remarks>
		/// <example>
		/// Consider the following statement: `command "Parameter Here";command Relevant`<br/>
		/// The <see cref="RelevantWordCharIndex"/> value would be 33 since the relevant word is the aptly named `Relevant` and character [33] is the first character index of the word.
		/// </example>
		public int RelevantWordCharIndex { get; internal set; }
		/// <summary>
		/// A list of all the words input for the invocation being evaluated.
		/// </summary>
		/// <remarks>
		/// This only includes words for this invocation not the entire message.
		/// It is <b>not</b> possible to evaluate words for previous invocations in a single console input.
		/// </remarks>
		public List<Word> Words { get; internal set; }
		/// <summary>
		/// For instance commands, the object being targeted by the second word. If this is a static command or the target is invalid will be `null`.
		/// </summary>
		/// <remarks>
		/// For instance commands this value can be used to autofill based on the value currently set on the instance.
		/// </remarks>
		public UnityEngine.Object InstanceTarget { get; internal set; }

		/// <summary>
		/// Get the text for the word that is currently relevant to the autofill.
		/// </summary>
		/// <returns>The text of the relevant word, if there is one. If there is no relevant word returns <see cref="string.Empty"/></returns>
		public string GetRelevantWordText()
		{
			if (RelevantWordIndex < 0 || RelevantWordIndex >= Words.Count)
			{
				return string.Empty;
			}
			else
			{
				return Words[RelevantWordIndex].text;
			}
		}

		/// <summary>
		/// Create an auto fill value from text that you would like to insert in place for the currently relevant word.
		/// </summary>
		/// <remarks>
		/// Will automatically be made a literal (quotations surrounding) if it contains a space.
		/// </remarks>
		/// <param name="textToPlaceForWord">The full word to place in as the original word.</param>
		public AutofillValue CreateAutofill(string textToPlaceForWord)
		{
			return new AutofillValue(textToPlaceForWord, RelevantWordCharIndex);
		}
	}
}