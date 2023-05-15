using JetBrains.Annotations;

namespace LMirman.VespaIO
{
	/// <summary>
	/// A definition for an autofill value that can be insert into a specific console input.
	/// </summary>
	/// <remarks>
	/// An autofill value is directly connected with the current string for console input.
	/// Therefore, an autofill value should be considered unusable between consoles and out of date when console input has changed.
	/// </remarks>
	public class AutofillValue
	{
		/// <summary>
		/// The full word that will be autofilled into the console
		/// </summary>
		/// <remarks>
		/// This word will be substituted into the current console input starting at <see cref="globalStartIndex"/>.
		/// Can also be used to preview the full word that will substitute the current word.
		/// </remarks>
		public readonly string newWord;
		/// <summary>
		/// <see cref="newWord"/> with special markup if necessary (such as quotations).
		/// </summary>
		public readonly string markupNewWord;
		/// <summary>
		/// The character index that the relevant word begins at.
		/// Essentially the starting index for where autofill values will be placed.
		/// </summary>
		/// <remarks>
		/// Unlike <see cref="Word.startIndex"/> this index is relative to the entire input, including other statements.
		/// </remarks>
		public readonly int globalStartIndex;

		/// <summary>
		/// Create an autofill value that will insert text <paramref name="newWord"/> into the console input and character index <paramref name="globalStartIndex"/>.
		/// </summary>
		/// <param name="newWord">The text that will be placed as the relevant word in the console input</param>
		/// <param name="globalStartIndex">The character index for the start of the relevant word in the console</param>
		public AutofillValue(string newWord, int globalStartIndex)
		{
			this.newWord = newWord;
			markupNewWord = MakeLiteralIfNecessary(newWord);
			this.globalStartIndex = globalStartIndex;
		}

		/// <summary>
		/// If a word contains a space it must be surround by quotations inside the console input.<br/>
		/// To make using autofill values easier we automatically add quotes if they are required and not currently present.
		/// </summary>
		[Pure]
		private string MakeLiteralIfNecessary(string word)
		{
			if (word == null)
			{
				return string.Empty;
			}
			else if (!word.Contains(" ") || word.StartsWith("\""))
			{
				return word;
			}
			else
			{
				return $"\"{word}\"";
			}
		}
	}
}