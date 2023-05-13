using JetBrains.Annotations;

namespace LMirman.VespaIO
{
	public class AutofillValue
	{
		/// <summary>
		/// The full word that will be autofilled into the console
		/// </summary>
		/// <remarks>
		/// Useful for previewing out of line what text will be insert
		/// </remarks>
		public readonly string newWord;
		/// <summary>
		/// The text that will be insert into the console to place the autofill word in
		/// </summary>
		/// <remarks>
		/// Useful for previewing in line what text will be insert
		/// </remarks>
		public readonly string insertText;
		/// <summary>
		/// Unlike <see cref="Word.startIndex"/> this index is relative to the entire input, including other statements.
		/// </summary>
		public readonly int globalStartIndex;

		public AutofillValue(string newWord, int oldWordLength, int globalStartIndex)
		{
			newWord = MakeLiteralIfNecessary(newWord);
			this.newWord = newWord;
			insertText = newWord.Substring(oldWordLength);
			this.globalStartIndex = globalStartIndex;
		}

		public AutofillValue(string newWord, string insertText, int globalStartIndex)
		{
			newWord = MakeLiteralIfNecessary(newWord);
			this.newWord = newWord;
			this.insertText = insertText;
			this.globalStartIndex = globalStartIndex;
		}

		public AutofillValue(string newWord, int globalStartIndex)
		{
			newWord = MakeLiteralIfNecessary(newWord);
			this.newWord = newWord;
			this.globalStartIndex = globalStartIndex;
			insertText = string.Empty;
		}

		[Pure]
		private string MakeLiteralIfNecessary(string word)
		{
			if (!word.Contains(" ") || word.StartsWith("\""))
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