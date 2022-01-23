namespace LMirman.VespaIO
{
	/// <summary>
	/// Long string is just a regular string but when provided as a parameter for a <see cref="StaticCommandAttribute"/> will send the entire console parameters (besides the command itself) to the method
	/// </summary>
	/// <remarks>In order for a long string to be received it must be the only parameter of method. The invoked method is reponsible for any parsing done after the fact.</remarks>
	public readonly struct Longstring
	{
		public readonly string value;

		public static implicit operator string(Longstring longstring) => longstring.value;
		public static implicit operator Longstring(string value) => new Longstring(value);

		public Longstring(string value)
		{
			this.value = value;
		}
	}
}