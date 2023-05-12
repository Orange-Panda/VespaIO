namespace LMirman.VespaIO
{
	public class AutoFillValue
	{
		/// <summary>
		/// Unlike <see cref="Word.startIndex"/> this index is relative to the entire input, including other statements.
		/// </summary>
		public readonly int globalStartIndex;
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

		public AutoFillValue(string newWord, string insertText, int globalStartIndex)
		{
			this.newWord = newWord;
			this.insertText = insertText;
			this.globalStartIndex = globalStartIndex;
		}
	}
}