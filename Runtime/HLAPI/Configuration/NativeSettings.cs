using UnityEngine;

namespace LMirman.VespaIO
{
	/// <summary>
	/// Load and reference the developer configuration for the console at runtime.
	/// </summary>
	public static class NativeSettings
	{
		public const string SettingsPath = "VespaIO/Settings";

		internal static ConsoleSettingsConfig Config { get; private set; }

		static NativeSettings()
		{
			ConsoleSettingsFile file = Resources.Load<ConsoleSettingsFile>(SettingsPath);
			if (file == null)
			{
				Config = new ConsoleSettingsConfig();
				Debug.LogWarning(
					$"Unable to load console settings resource at {SettingsPath}. Please ensure such a file exists and is of type ConsoleSettingsConfig. Default settings will be used.\nGo to `Tools > Vespa IO > Select Console Settings` to automatically generate this asset.");
				return;
			}

			Config = file.DeepCopy();
		}
	}
}