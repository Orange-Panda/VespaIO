using JetBrains.Annotations;
using System.Collections.Generic;

namespace LMirman.VespaIO
{
	[PublicAPI]
	public class AliasSet
	{
		private readonly Dictionary<string, string> lookup = new Dictionary<string, string>();

		/// <summary>
		/// The total number of alias definitions.
		/// </summary>
		public int AliasCount => lookup.Count;

		/// <summary>
		/// An IEnumerable to iterate over each alias definition.
		/// </summary>
		/// <remarks>
		/// Mutating the Alias definitions while iterating is discouraged.
		/// </remarks>
		public IEnumerable<KeyValuePair<string, string>> AllAliases => lookup;

		/// <summary>
		/// Check if there is an alias definition for a particular key.
		/// </summary>
		/// <param name="key">The alias key to be input in place of a command</param>
		/// <returns>True if there is an alias definition for this key. False of there was not.</returns>
		public bool ContainsAlias(string key)
		{
			return lookup.ContainsKey(key.CleanseKey());
		}

		/// <summary>
		/// Try to get an alias definition for a particular alias key.
		/// </summary>
		/// <param name="key">The alias key to be input in place of a command</param>
		/// <param name="definition">The alias definition found for the alias key</param>
		/// <returns>True if there is a definition for the alias key. False if there was not.</returns>
		public bool TryGetAlias(string key, out string definition)
		{
			return lookup.TryGetValue(key.CleanseKey(), out definition);
		}

		/// <summary>
		/// Get the alias definition for a particular alias key.
		/// </summary>
		/// <param name="key">The alias key to be input in place of a command</param>
		/// <param name="fallbackDefinition">The value to return if there is no such alias key present</param>
		/// <returns>The alias definition for the particular <see cref="key"/> if present. Returns <see cref="fallbackDefinition"/> if there is no alias defined with <see cref="key"/></returns>
		public string GetAlias(string key, string fallbackDefinition = default)
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
		/// <returns>True if this is a brand new alias definition, false if this is not a new alias definition and has overwritten an alias.</returns>
		public bool SetAlias(string key, string definition)
		{
			key = key.CleanseKey();
			bool hadAlias = ContainsAlias(key);
			lookup[key] = definition;
			return !hadAlias;
		}

		/// <summary>
		/// Remove an alias definition, if one exists.
		/// </summary>
		/// <param name="key">The key for the alias you would like to remove.</param>
		/// <returns>True if there was an alias present and it was removed, false otherwise.</returns>
		public bool RemoveAlias(string key)
		{
			return lookup.Remove(key.CleanseKey());
		}

		/// <summary>
		/// Immediately reset <b>ALL</b> alias definitions.<br/>
		/// This is a permanent and irreversible action!
		/// </summary>
		public void ResetAliases()
		{
			lookup.Clear();
		}
	}
}