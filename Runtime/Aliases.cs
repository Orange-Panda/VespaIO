using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace LMirman.VespaIO
{
	internal static class Aliases
	{
		internal static Dictionary<string, string> Lookup = new Dictionary<string, string>();
		private const string DirectoryName = "VespaIO";
		private const string PrefsLocation = "aliases.json";

		private static string DataPath => $"{Application.persistentDataPath}/{DirectoryName}/{PrefsLocation}";
		private static string DirectoryPath => $"{Application.persistentDataPath}/{DirectoryName}";

		static Aliases()
		{
			Directory.CreateDirectory(DirectoryPath);
			ReadLookup();
		}

		public static void ReadLookup()
		{
			if (!File.Exists(DataPath))
			{
				Reset();
				return;
			}

			try
			{
				string fileData = File.ReadAllText(DataPath);
				Lookup = JsonConvert.DeserializeObject<Dictionary<string, string>>(fileData);
				//TODO: Sanitize loaded list.
			}
			catch
			{
				Reset();
				DevConsole.Log("<color=red>Critical error occurred when loading aliases. Alias definitions have been reset.</color>");
			}
		}

		public static void WriteLookup()
		{
			string value = JsonConvert.SerializeObject(Lookup);
			File.WriteAllText(DataPath, value);
		}

		public static void Reset()
		{
			Lookup.Clear();
			WriteLookup();
		}
	}
}