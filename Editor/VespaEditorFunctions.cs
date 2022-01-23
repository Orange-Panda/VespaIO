using UnityEditor;
using UnityEngine;

namespace LMirman.VespaIO.Editor
{
	public static class VespaEditorFunctions
	{
		[MenuItem("Tools/Vespa IO/Select Console Settings")]
		public static void SelectSettings()
		{
			ConsoleSettingsFile file = Resources.Load<ConsoleSettingsFile>(ConsoleSettings.SettingsPath);
			if (file == null)
			{
				ConsoleSettingsFile asset = ScriptableObject.CreateInstance<ConsoleSettingsFile>();
				if (!AssetDatabase.IsValidFolder("Assets/Resources"))
				{
					AssetDatabase.CreateFolder("Assets", "Resources");
				}
				if (!AssetDatabase.IsValidFolder("Assets/Resources/VespaIO"))
				{
					AssetDatabase.CreateFolder("Assets/Resources", "VespaIO");
				}
				AssetDatabase.CreateAsset(asset, $"Assets/Resources/{ConsoleSettings.SettingsPath}.asset");
				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();
				file = Resources.Load<ConsoleSettingsFile>(ConsoleSettings.SettingsPath);
			}
			Selection.activeObject = file;
		}
	}
}