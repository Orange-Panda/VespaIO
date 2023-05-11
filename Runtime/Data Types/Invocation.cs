using JetBrains.Annotations;
using System.Collections.Generic;
using System.Text;

namespace LMirman.VespaIO
{
	public class Invocation
	{
		public readonly ValidState validState;
		public readonly string inputText;
		public readonly Command command;
		public readonly Argument[] arguments;
		public readonly LongString longString;

		public Invocation(string inputText)
		{
			this.inputText = inputText;
		}

		/// <summary>
		/// Takes a raw input string and substitutes an alias command at the beginning with its alias definition
		/// </summary>
		/// <returns>The input string after having the alias replaced with its definition</returns>
		[Pure]
		public static AliasOutcome SubstituteAliasForCommand(string input, out string output)
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
			if (string.IsNullOrWhiteSpace(substring) || !Aliases.TryGetAlias(substring, out string aliasValue))
			{
				output = input;
				return AliasOutcome.NoChange;
			}
			else if (Commands.ContainsCommand(substring))
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
			StringBuilder stringBuilder = new StringBuilder();
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
						stringBuilder.Append(inputChar);
					}

					SubmitWord(stringBuilder, output);
				}
				else if (inputChar == ';' && isEscaped && !inQuote)
				{
					if (!includeSemicolonEscapes)
					{
						stringBuilder.Remove(stringBuilder.Length - 1, 1);
					}

					stringBuilder.Append(inputChar);
				}
				else
				{
					stringBuilder.Append(inputChar);
				}
			}

			SubmitWord(stringBuilder, output);
			return output;
		}

		public static List<string> GetWordsFromString(string input, bool removeSpecialSyntax = true)
		{
			List<string> output = new List<string>();
			bool inQuote = false;
			int escapeCount = 0;
			StringBuilder stringBuilder = new StringBuilder();
			foreach (char inputChar in input)
			{
				bool isEscaped = escapeCount % 2 == 1;

				// If we encounter an unescaped quote mark, toggle quote mode.
				if (inputChar == '"' && !isEscaped)
				{
					inQuote = !inQuote;
				}

				// If we encounter a space and are not in quote mode, begin a new word.
				if (inputChar == ' ' && !inQuote)
				{
					SubmitWord(stringBuilder, output);
					escapeCount = 0;
					continue;
				}

				escapeCount = inputChar == '\\' ? escapeCount + 1 : 0;
				if (!removeSpecialSyntax || (inputChar != '\\' && inputChar != '"') || isEscaped)
				{
					stringBuilder.Append(inputChar);
				}
			}

			SubmitWord(stringBuilder, output);
			return output;
		}

		private static void SubmitWord(StringBuilder stringBuilder, List<string> list)
		{
			if (stringBuilder.Length > 0)
			{
				string substring = stringBuilder.ToString().Trim(' ');
				if (!string.IsNullOrWhiteSpace(substring))
				{
					list.Add(substring);
				}

				stringBuilder.Clear();
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

		public enum ValidState
		{
			Valid, ErrorInvalidSyntax, ErrorInvalidCommand
		}
	}
}