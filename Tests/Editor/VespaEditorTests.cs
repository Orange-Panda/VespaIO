using NUnit.Framework;

namespace LMirman.VespaIO.Editor.Tests
{
	public class VespaEditorTests
	{
		[TestCase("key name", "key_name")]
		[TestCase("k#e$y-_&na=m+e?", "key_name")]
		[TestCase("KEY_NAME", "key_name")]
		[TestCase("Key Name", "key_name")]
		[TestCase("key_name_123", "key_name_123")]
		[TestCase("KEY NAME 123", "key_name_123")]
		public void VespaFunctions_CleanseKey(string actual, string expected)
		{
			actual = actual.CleanseKey();
			Assert.AreEqual(actual, expected);
		}

		[TestCase("phrase", "phrase")]
		[TestCase("echo \"semicolon is ;\"", "echo \"semicolon is ;\"")]
		[TestCase("echo \"escape with \\;\"", "echo \"escape with \\;\"")]
		[TestCase("echo \\;", "echo ;")]
		public void Vespa_SplitStringBySemicolon(string input, string expected)
		{
			string actual = Invocation.SplitStringBySemicolon(input)[0];
			Assert.AreEqual(actual, expected);
		}
	}
}