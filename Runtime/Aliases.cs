using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace LMirman.VespaIO
{
	internal static class Aliases
	{
		private const string DirectoryName = "VespaIO";
		private const string PrefsLocation = "aliases.json";

		private static string DataPath => $"{Application.persistentDataPath}/{DirectoryName}/{PrefsLocation}";
		private static string DirectoryPath => $"{Application.persistentDataPath}/{DirectoryName}";
		
		internal static Dictionary<string, string> Lookup { get; private set; } = new Dictionary<string, string>();

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