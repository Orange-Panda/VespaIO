using JetBrains.Annotations;

namespace LMirman.VespaIO
{
	/// <summary>
	/// Long string is just a regular string but when provided as a parameter for a <see cref="StaticCommandAttribute"/> will send the entire console parameters (besides the command itself) to the method
	/// </summary>
	/// <remarks>In order for a long string to be received it must be the only parameter of method. The invoked method is reponsible for any parsing done after the fact.</remarks>
	public readonly struct LongString
	{
		public readonly string value;

		public static implicit operator string(LongString longString) => longString.value;
		public static implicit operator LongString(string value) => new LongString(value);
		public static readonly LongString Empty = (LongString)string.Empty;

		public LongString(string value)
		{
			this.value = value;
		}

		public override string ToString() => value;

		/// <summary>
		/// Remove the first and last instance of quotation marks around the long string if the first and last character are exactly '"'
		/// </summary>
		[Pure]
		public static LongString RemoveQuotes(LongString longString)
		{
			string value = longString.value;
			if (value.Length >= 2 && value[0] == '"' && value[value.Length - 1] == '"')
			{
				return new LongString(value.Substring(1, value.Length - 2));
			}
			else
			{
				return longString;
			}
		}
	}
}