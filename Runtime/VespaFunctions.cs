using System.Diagnostics.Contracts;
using System.Text.RegularExpressions;

namespace LMirman.VespaIO
{
	public static class VespaFunctions
	{
		private static readonly Regex KeyRegex = new Regex("[^a-z0-9_]");

		/// <summary>
		/// Cleans a string to follow the naming convention of keys used in the Vespa IO console.<br/>
		/// Those rules being the following:<br/>
		/// - Must contain characters either from a-z, 0-9, or _.<br/>
		/// - May not contain capital letters<br/>
		/// </summary>
		/// <remarks>
		/// The cleansed string will:<br/>
		/// - Have spaces replaced with the '_' character.
		/// - Have upper case characters converted to lower case.
		/// - Have non-alphanumeric characters removed entirely.
		/// </remarks>
		/// <returns>The input with invalid characters converted or removed.</returns>
		[Pure]
		public static string CleanseKey(this string inputString)
		{
			inputString = inputString.Replace(' ', '_');
			inputString = inputString.ToLower();
			inputString = KeyRegex.Replace(inputString, string.Empty);
			return inputString;
		}
	}
}