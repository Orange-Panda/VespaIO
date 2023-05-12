namespace LMirman.VespaIO
{
	public class AutoFillValue
	{
		/// <summary>
		/// The word that was originally present that is the target of autofill.
		/// </summary>
		public readonly Word originalWord;
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

		public AutoFillValue(string newWord, string insertText, Word originalWord)
		{
			this.newWord = newWord;
			this.insertText = insertText;
			this.originalWord = originalWord;
		}
	}
}