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
	}
}