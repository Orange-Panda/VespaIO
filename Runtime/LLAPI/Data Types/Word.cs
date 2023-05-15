using System;

namespace LMirman.VespaIO
{
	/// <summary>
	/// A word is a specific input index for an <see cref="Invocation"/>.
	/// </summary>
	public class Word
	{
		/// <summary>
		/// The actual string this word contains
		/// </summary>
		public readonly string text;
		/// <summary>
		/// Various flags about the original context of the word when it was input.
		/// </summary>
		public readonly Context context;
		/// <summary>
		/// The starting index of this word relative to a statement.
		/// </summary>
		public readonly int startIndex;

		public Word(string text, Context context, int startIndex)
		{
			this.text = text;
			this.context = context;
			this.startIndex = startIndex;
		}

		[Flags]
		public enum Context
		{
			None = 0,
			/// <summary>
			/// When this flag is set: indicates the word is a literal and should always be treated as a string even if it could be parsed to other types.
			/// </summary>
			IsLiteral = 1,
			/// <summary>
			/// When this flag is set: indicates the word is in an unfinished literal.
			/// </summary>
			IsInOpenLiteral = 2
		}
	}
}