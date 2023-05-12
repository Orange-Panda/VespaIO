using UnityEngine;

namespace LMirman.VespaIO
{
	/// <summary>
	/// Asset that will serialize the <see cref="ConsoleSettingsConfig"/> to the disk.
	/// </summary>
	public class ConsoleSettingsFile : ScriptableObject
	{
		[SerializeField]
		private ConsoleSettingsConfig config;

		/// <summary>
		/// Create a deep copy of the configuration allowing it to be mutated in memory without modifying the asset's config.
		/// </summary>
		public ConsoleSettingsConfig DeepCopy() => config.DeepCopy();
	}
}