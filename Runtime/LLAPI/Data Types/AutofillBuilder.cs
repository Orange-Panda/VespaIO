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
		public HashSet<string> Exclusions { get; internal set; }
		public int RelevantWordIndex { get; internal set; }
		public int RelevantWordCharIndex { get; internal set; }
		public List<Word> Words { get; internal set; }

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
		/// Create an overwrite auto fill.
		/// This autofill value will replace the previous word even if it wasn't related to the word.
		/// </summary>
		/// <remarks>
		/// Useful for autofill for a value that doesn't care about the relevant word such as autofill for a current value.
		/// </remarks>
		/// <param name="textToPlaceForWord">The full word to place in as the original word.</param>
		public AutofillValue CreateOverwriteAutofill(string textToPlaceForWord)
		{
			return new AutofillValue(textToPlaceForWord, RelevantWordCharIndex);
		}

		/// <summary>
		/// Create a completion auto fill.
		/// This autofill value is useful for auto fills that are completing an incomplete value (similar to the command autofill)
		/// </summary>
		/// <param name="textToPlaceForWord">The full word to place in as the original word.</param>
		public AutofillValue CreateCompletionAutofill(string textToPlaceForWord)
		{
			return new AutofillValue(textToPlaceForWord, GetRelevantWordText().Length, RelevantWordCharIndex);
		}
	}
}