using JetBrains.Annotations;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
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

		/// <summary>
		/// The directory of the folder containing the alias preferences file.
		/// </summary>
		private static string DirectoryPath => $"{Application.persistentDataPath}/{DirectoryName}";

		/// <summary>
		/// The path to the alias preferences file.
		/// </summary>
		private static string DataPath => $"{DirectoryPath}/{PrefsFileName}";

		/// <summary>
		/// The total number of alias definitions.
		/// </summary>
		public static int AliasCount => Lookup.Count;

		/// <summary>
		/// An IEnumerable to iterate over each alias definition.
		/// </summary>
		/// <remarks>
		/// Mutating the Alias definitions while iterating is discouraged.
		/// </remarks>
		public static IEnumerable<KeyValuePair<string, string>> AllAliases => Lookup;

		private static readonly Dictionary<string, string> Lookup = new Dictionary<string, string>();

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
				ResetAliases();
				return;
			}

			try
			{
				Lookup.Clear();
				string fileData = File.ReadAllText(DataPath);
				Dictionary<string, string> fileLookup = JsonConvert.DeserializeObject<Dictionary<string, string>>(fileData);
				foreach (KeyValuePair<string, string> definition in fileLookup)
				{
					SetAlias(definition.Key, definition.Value, false);
				}
			}
			catch
			{
				ResetAliases();
				DevConsole.Log("<color=red>Critical error occurred when loading aliases. Alias definitions have been reset.</color>");
			}
		}

		/// <summary>
		/// Write all current alias definitions to the disk.
		/// </summary>
		public static void WriteToDisk()
		{
			string value = JsonConvert.SerializeObject(Lookup);
			File.WriteAllText(DataPath, value);
		}

		/// <summary>
		/// Check if there is an alias definition for a particular key.
		/// </summary>
		/// <param name="key">The alias key to be input in place of a command</param>
		/// <returns>True if there is an alias definition for this key. False of there was not.</returns>
		public static bool ContainsAlias(string key)
		{
			return Lookup.ContainsKey(key.CleanseKey());
		}

		/// <summary>
		/// Try to get an alias definition for a particular alias key.
		/// </summary>
		/// <param name="key">The alias key to be input in place of a command</param>
		/// <param name="definition">The alias definition found for the alias key</param>
		/// <returns>True if there is a definition for the alias key. False if there was not.</returns>
		public static bool TryGetAlias(string key, out string definition)
		{
			return Lookup.TryGetValue(key.CleanseKey(), out definition);
		}

		/// <summary>
		/// Get the alias definition for a particular alias key.
		/// </summary>
		/// <param name="key">The alias key to be input in place of a command</param>
		/// <param name="fallbackDefinition">The value to return if there is no such alias key present</param>
		/// <returns>The alias definition for the particular <see cref="key"/> if present. Returns <see cref="fallbackDefinition"/> if there is no alias defined with <see cref="key"/></returns>
		public static string GetAlias(string key, string fallbackDefinition = default)
		{
			return TryGetAlias(key.CleanseKey(), out string definition) ? definition : fallbackDefinition;
		}

		/// <summary>
		/// Set an alias definition for usage in the console.
		/// </summary>
		/// <remarks>
		/// This will add a new alias definition or overwrite an already existing alias definition.
		/// </remarks>
		/// <param name="key">The alias key to be input in place of a command</param>
		/// <param name="definition">The value that the alias key will be replaced with in the console</param>
		/// <param name="writeToDisk">When true the alias definitions will immediately be written to the disk. Consider making this false if you are adding multiple aliases at once.</param>
		/// <returns>True if this is a brand new alias definition, false if this is not a new alias definition and has overwritten an alias.</returns>
		public static bool SetAlias(string key, string definition, bool writeToDisk = true)
		{
			key = key.CleanseKey();
			bool hadAlias = ContainsAlias(key);
			Lookup[key] = definition;
			if (writeToDisk)
			{
				WriteToDisk();
			}

			return !hadAlias;
		}

		/// <summary>
		/// Remove an alias definition, if one exists.
		/// </summary>
		/// <param name="key">The key for the alias you would like to remove.</param>
		/// <param name="writeToDisk">When true the alias definitions will immediately be written to the disk. Consider making this false if you are removing multiple aliases at once.</param>
		/// <returns>True if there was an alias present and it was removed, false otherwise.</returns>
		public static bool RemoveAlias(string key, bool writeToDisk = true)
		{
			bool didRemove = Lookup.Remove(key.CleanseKey());
			if (writeToDisk)
			{
				WriteToDisk();
			}

			return didRemove;
		}

		/// <summary>
		/// Immediately reset <b>ALL</b> aliases defined by the user and save the empty alias dictionary to the disk.<br/>
		/// This is a permanent and irreversible action!
		/// </summary>
		public static void ResetAliases()
		{
			Lookup.Clear();
			WriteToDisk();
		}
	}
}