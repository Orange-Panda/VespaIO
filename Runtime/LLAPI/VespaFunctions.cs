using JetBrains.Annotations;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace LMirman.VespaIO
{
	[PublicAPI]
	public static class VespaFunctions
	{
		private static readonly Regex KeyRegex = new Regex("[^a-z0-9_]");
		private static readonly StringBuilder WordStringBuilder = new StringBuilder();

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

		/// <summary>
		/// Takes a raw input string and substitutes an alias command at the beginning with its alias definition
		/// </summary>
		/// <returns>The input string after having the alias replaced with its definition</returns>
		public static AliasOutcome SubstituteAliasForCommand(string input, CommandSet commandSet, AliasSet aliasSet, out string output)
		{
			int substringLength = 0;
			foreach (char inputChar in input)
			{
				if (inputChar == ' ')
				{
					break;
				}

				substringLength++;
			}

			string substring = input.Substring(0, substringLength).CleanseKey();
			if (string.IsNullOrWhiteSpace(substring) || !aliasSet.TryGetAlias(substring, out string aliasValue))
			{
				output = input;
				return AliasOutcome.NoChange;
			}
			else if (commandSet.ContainsCommand(substring))
			{
				output = substring;
				return AliasOutcome.CommandConflict;
			}
			else
			{
				output = aliasValue + input.Substring(substringLength);
				return AliasOutcome.AliasApplied;
			}
		}

		/// <summary>
		/// Split the input string by unescaped and unquoted semicolons.
		/// </summary>
		/// <example>
		/// Input `phrase;echo "semicolon is ;";echo "escape with \;";echo \;;` will output strings by default:<br/>
		/// - `phrase`<br/>
		/// - `echo "semicolon is ;"`<br/>
		/// - `echo "escape with \;"`<br/>
		/// - `echo ;`.
		/// </example>>
		/// <param name="input">The input string to split.</param>
		/// <param name="includeSemicolonEscapes">When true will include the \ character on escaped semicolons. Does not apply to quoted and escaped semicolons.</param>
		/// <param name="includeFinalSemicolon">When true includes the final semicolon in the output string.</param>
		/// <returns>A list of none to many strings output by the split. Will not include null or empty strings.</returns>
		public static List<string> SplitStringBySemicolon(string input, bool includeSemicolonEscapes = false, bool includeFinalSemicolon = false)
		{
			List<string> output = new List<string>();

			// We can skip this entire process if there isn't any semicolon in the first place.
			if (!input.Contains(";"))
			{
				output.Add(input);
				return output;
			}

			bool inQuote = false;
			int escapeCount = 0;
			WordStringBuilder.Clear();
			foreach (char inputChar in input)
			{
				bool isEscaped = escapeCount % 2 == 1;

				// If there is an unescaped quotation, we should toggle quote mode and not submit semicolons while within it.
				if (inputChar == '\"' && !isEscaped)
				{
					inQuote = !inQuote;
				}

				escapeCount = inputChar == '\\' ? escapeCount + 1 : 0;

				// Submit a new output when the ; character is reached
				if (inputChar == ';' && !isEscaped && !inQuote)
				{
					if (includeFinalSemicolon)
					{
						WordStringBuilder.Append(inputChar);
					}

					SubmitWord();
				}
				else if (inputChar == ';' && isEscaped && !inQuote)
				{
					if (!includeSemicolonEscapes)
					{
						WordStringBuilder.Remove(WordStringBuilder.Length - 1, 1);
					}

					WordStringBuilder.Append(inputChar);
				}
				else
				{
					WordStringBuilder.Append(inputChar);
				}
			}

			SubmitWord();
			return output;

			void SubmitWord()
			{
				if (WordStringBuilder.Length > 0)
				{
					string substring = WordStringBuilder.ToString().Trim(' ');
					if (!string.IsNullOrWhiteSpace(substring))
					{
						output.Add(substring);
					}

					WordStringBuilder.Clear();
				}
			}
		}

		public static List<Word> GetWordsFromString(string input, bool removeSpecialSyntax = true)
		{
			List<Word> output = new List<Word>();
			bool inQuote = false;
			bool isLiteral = false;
			int escapeCount = 0;
			WordStringBuilder.Clear();
			foreach (char inputChar in input)
			{
				bool isEscaped = escapeCount % 2 == 1;

				// If we encounter an unescaped quote mark, toggle quote mode.
				if (inputChar == '"' && !isEscaped)
				{
					inQuote = !inQuote;
					isLiteral = true;
				}

				// If we encounter a space and are not in quote mode, begin a new word.
				if (inputChar == ' ' && !inQuote)
				{
					SubmitWord();
					escapeCount = 0;
					continue;
				}

				escapeCount = inputChar == '\\' ? escapeCount + 1 : 0;
				if (!removeSpecialSyntax || (inputChar != '\\' && inputChar != '"') || isEscaped)
				{
					WordStringBuilder.Append(inputChar);
				}
			}

			SubmitWord();
			return output;

			void SubmitWord()
			{
				string substring = WordStringBuilder.ToString();
				if (isLiteral || !string.IsNullOrWhiteSpace(substring))
				{
					output.Add(new Word(substring, isLiteral));
				}

				isLiteral = false;
				WordStringBuilder.Clear();
			}
		}

		public enum AliasOutcome
		{
			/// <summary>
			/// No alias existed. Returns the input unchanged.
			/// </summary>
			NoChange,
			/// <summary>
			/// An alias was applied to the command with no issues. Returns modified input with alias definition instead of alias key.
			/// </summary>
			AliasApplied,
			/// <summary>
			/// An alias existed but there is a command that is identical to the alias. Returns the name of the command/alias that had a conflict.
			/// </summary>
			CommandConflict
		}
	}
}