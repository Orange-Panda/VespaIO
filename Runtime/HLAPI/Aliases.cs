using JetBrains.Annotations;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace LMirman.VespaIO
{
	/// <summary>
	/// Handles alias definitions for creating shortcuts for complicated commands in the <see cref="DevConsole"/>
	/// </summary>
	[PublicAPI]
	public static class Aliases
	{
		private const string DirectoryName = "VespaIO";
		private const string PrefsFileName = "aliases.json";
		public static readonly AliasSet aliasSet = new AliasSet();

		/// <summary>
		/// The directory of the folder containing the alias preferences file.
		/// </summary>
		private static string DirectoryPath => $"{Application.persistentDataPath}/{DirectoryName}";

		/// <summary>
		/// The path to the alias preferences file.
		/// </summary>
		private static string DataPath => $"{DirectoryPath}/{PrefsFileName}";

		static Aliases()
		{
			Directory.CreateDirectory(DirectoryPath);
			LoadFromDisk();
		}

		/// <summary>
		/// Load alias definitions from the disk.
		/// </summary>
		public static void LoadFromDisk()
		{
			if (!File.Exists(DataPath))
			{
				ResetAliasesAndFile();
				return;
			}

			try
			{
				aliasSet.ResetAliases();
				string fileData = File.ReadAllText(DataPath);
				Dictionary<string, string> fileLookup = JsonConvert.DeserializeObject<Dictionary<string, string>>(fileData);
				foreach (KeyValuePair<string, string> definition in fileLookup)
				{
					aliasSet.SetAlias(definition.Key, definition.Value);
				}
			}
			catch
			{
				ResetAliasesAndFile();
				DevConsole.Log("Critical error occurred when loading aliases. Alias definitions have been reset.", Console.LogStyling.Critical);
			}
		}

		/// <summary>
		/// Write all current alias definitions to the disk.
		/// </summary>
		public static void WriteToDisk()
		{
			string value = JsonConvert.SerializeObject(aliasSet.AllAliases.ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
			File.WriteAllText(DataPath, value);
		}

		/// <inheritdoc cref="AliasSet.ResetAliases"/>
		public static void ResetAliasesAndFile()
		{
			aliasSet.ResetAliases();
			WriteToDisk();
		}
	}
}