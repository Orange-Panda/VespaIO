namespace LMirman.VespaIO
{
	public class Word
	{
		public readonly string text;
		/// <summary>
		/// True when the <see cref="text"/> was formerly surround by quotations, meaning it should always be treated as a string.
		/// </summary>
		public readonly bool isLiteral;

		public Word(string text, bool isLiteral)
		{
			this.text = text;
			this.isLiteral = isLiteral;
		}
	}
}