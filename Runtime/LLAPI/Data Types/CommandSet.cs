using JetBrains.Annotations;
using System.Collections.Generic;
using System.Reflection;

namespace LMirman.VespaIO
{
	[PublicAPI]
	public class CommandSet
	{
		private readonly Dictionary<string, Command> lookup = new Dictionary<string, Command>(32);
		private readonly List<Command> commands = new List<Command>(32);
		private readonly CommandPropertiesComparer commandPropertiesComparer = new CommandPropertiesComparer();
		private bool sortDirty;

		public IEnumerable<Command> AllCommands
		{
			get
			{
				if (sortDirty)
				{
					commands.Clear();
					foreach (Command command in lookup.Values)
					{
						commands.Add(command);
					}
					commands.Sort(commandPropertiesComparer);
				}
				
				return commands;
			}
		}
		public IEnumerable<KeyValuePair<string, Command>> AllDefinitions => lookup;
		
		public bool ContainsCommand(string key)
		{
			return lookup.ContainsKey(key.CleanseKey());
		}

		public bool TryGetCommand(string key, out Command command)
		{
			return lookup.TryGetValue(key.CleanseKey(), out command);
		}

		public Command GetCommand(string key, Command fallbackCommand = null)
		{
			return TryGetCommand(key.CleanseKey(), out Command command) ? command : fallbackCommand;
		}

		public void RegisterCommand(ICommandProperties properties, MethodInfo methodInfo)
		{
			string key = properties.Key.CleanseKey();
			if (TryGetCommand(key, out Command command))
			{
				command.AddMethod(methodInfo);
				command.SetAttributeProperties(properties);
			}
			else
			{
				command = new Command(properties, methodInfo);
				lookup.Add(key, command);
				sortDirty = true;
			}
		}

		/// <summary>
		/// Unregister a specific method definition for a console command.
		/// </summary>
		/// <param name="key">The key of the command you would like to remove.</param>
		/// <param name="methodInfo">The method you would like to unregister for the command</param>
		public void UnregisterCommand(string key, MethodInfo methodInfo)
		{
			key = key.CleanseKey();
			if (TryGetCommand(key, out Command command))
			{
				command.RemoveMethod(methodInfo);

				if (!command.HasMethod)
				{
					lookup.Remove(key);
					sortDirty = true;
				}
			}
		}

		/// <summary>
		/// Unregister all command definitions for a particular key.
		/// </summary>
		/// <param name="key">The key of the command you would like to remove.</param>
		public void UnregisterCommand(string key)
		{
			key = key.CleanseKey();
			lookup.Remove(key);
			sortDirty = true;
		}

		public void UnregisterAllCommands()
		{
			lookup.Clear();
			sortDirty = true;
		}
	}
}