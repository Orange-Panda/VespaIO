namespace LMirman.VespaIO
{
	/// <summary>
	/// A word is a specific input index for an <see cref="Invocation"/>.
	/// Another way to think of this type is that it is the raw string representation of a <see cref="Argument"/>.
	/// </summary>
	public class Word
	{
		public readonly string text;
		/// <summary>
		/// True when the <see cref="text"/> was formerly surround by quotations, meaning it should always be treated as a string.
		/// </summary>
		public readonly bool isLiteral;
		/// <summary>
		/// The starting index of this word relative to a statement.
		/// </summary>
		public readonly int startIndex;

		public Word(string text, bool isLiteral, int startIndex)
		{
			this.text = text;
			this.isLiteral = isLiteral;
			this.startIndex = startIndex;
		}
	}
}