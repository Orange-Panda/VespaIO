using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace LMirman.VespaIO
{
	public class Command
	{
		// ReSharper disable once InconsistentNaming
		public readonly string Key;
		private static readonly StringBuilder GuideBuilder = new StringBuilder();

		public string Name { get; private set; }
		public string Description { get; private set; }
		public string Guide { get; private set; }
		public bool Cheat { get; private set; }
		public bool Hidden { get; private set; }

		//TODO: Make private and create method to find appropriate command
		public List<MethodInfo> Methods { get; } = new List<MethodInfo>();
		public bool HasMethod => Methods.Count > 0;

		public Command(StaticCommandAttribute attribute, MethodInfo method)
		{
			Key = attribute.Key.CleanseKey();
			AddMethod(attribute, method);
		}

		public void AddMethod(StaticCommandAttribute attribute, MethodInfo method)
		{
			string attributeKey = attribute.Key.CleanseKey();
			if (attributeKey != Key)
			{
				DevConsole.Log($"Error: There was an attempt to add a method to command \"{Key}\" but the key \"{attributeKey}\" did not match");
				return;
			}
			
			// Set attribute name
			if (!string.IsNullOrWhiteSpace(attribute.Name))
			{
				Name = attribute.Name;
			}
			else if (string.IsNullOrWhiteSpace(Name))
			{
				Name = method.Name;
			}

			// Set description
			if (!string.IsNullOrWhiteSpace(attribute.Description))
			{
				Description = attribute.Description;
			}
			else if (Description == null)
			{
				Description = string.Empty;
			}

			// If any method is considered a cheat, the entire command is labeled as a cheat
			if (attribute.Cheat)
			{
				Cheat = true;
			}

			// If any method is considered hidden, the entire command is labeled as hidden
			if (attribute.Hidden)
			{
				Hidden = true;
			}

			Methods.Add(method);
			UpdateGuide();
		}

		/// <summary>
		/// Remove a particular method from this command.
		/// </summary>
		/// <param name="methodInfo">The method to remove from this command</param>
		/// <remarks>This will <b>not</b> unset any modifications to command properties this method applied such as Name, Description, Cheat, or Hidden properties.</remarks>
		internal void RemoveMethod(MethodInfo methodInfo)
		{
			Methods.Remove(methodInfo);
			UpdateGuide();
		}

		private void UpdateGuide()
		{
			GuideBuilder.Clear();
			for (int i = 0; i < Methods.Count; i++)
			{
				MethodInfo method = Methods[i];
				GuideBuilder.Append("Usage: ");
				GuideBuilder.Append(Key);

				ParameterInfo[] parameters = method.GetParameters();
				foreach (ParameterInfo parameter in parameters)
				{
					GuideBuilder.Append(' ');
					GuideBuilder.Append(TranslateParameter(parameter.ParameterType));
				}

				if (i < Methods.Count - 1)
				{
					GuideBuilder.Append('\n');
				}
			}

			Guide = GuideBuilder.ToString();
		}

		private static string TranslateParameter(Type type)
		{
			if (type == typeof(int))
			{
				return "[INTEGER]";
			}
			else if (type == typeof(float))
			{
				return "[FLOAT]";
			}
			else if (type == typeof(string))
			{
				return "[WORD]";
			}
			else if (type == typeof(LongString))
			{
				return "[PHRASE]";
			}
			else
			{
				return "[INVALID/UNKNOWN]";
			}
		}
	}
}