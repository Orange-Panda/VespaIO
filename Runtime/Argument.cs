using System;

namespace LMirman.VespaIO
{
	public class Argument
	{
		public readonly ArgType<int> intValue;
		public readonly ArgType<float> floatValue;
		public readonly ArgType<bool> boolValue;
		public readonly ArgType<string> stringValue;

		public bool HasValidType(Type type)
		{
			if (type == null)
			{
				return false;
			}
			else if (type == typeof(int))
			{
				return intValue.isValid;
			}
			else if (type == typeof(float))
			{
				return floatValue.isValid;
			}
			else if (type == typeof(bool))
			{
				return boolValue.isValid;
			}
			else if (type == typeof(string) || type == typeof(LongString))
			{
				return stringValue.isValid;
			}
			else
			{
				DevConsole.Log($"<color=red>Error:</color> Command arguments of type \"{type}\" are unsupported.");
				return false;
			}
		}

		public object GetValue(Type type)
		{
			if (type == null)
			{
				return false;
			}
			else if (type == typeof(int))
			{
				return intValue.value;
			}
			else if (type == typeof(float))
			{
				return floatValue.value;
			}
			else if (type == typeof(bool))
			{
				return boolValue.value;
			}
			else if (type == typeof(string))
			{
				return stringValue.value;
			}
			else if (type == typeof(LongString))
			{
				return (LongString)stringValue.value;
			}
			else
			{
				DevConsole.Log($"<color=red>Error:</color> Command arguments of type \"{type}\" are unsupported.");
				return false;
			}
		}

		public Argument(string source)
		{
			// Set string value
			stringValue = new ArgType<string>(source, true);
				
			// Set int value
			bool didParseInt = int.TryParse(source, out int intParse);
			intValue = new ArgType<int>(didParseInt ? intParse : -1, didParseInt);
				
			// Set float value
			bool didParseFloat = float.TryParse(source, out float floatParse);
			floatValue = new ArgType<float>(didParseFloat ? floatParse : -1, didParseFloat);

			// Set bool value
			if (didParseInt)
			{
				boolValue = new ArgType<bool>(intParse > 0, true);
			}
			else
			{
				bool didParseBool = bool.TryParse(source, out bool boolParse);
				boolValue = new ArgType<bool>(didParseBool && boolParse, didParseBool);
			}
		}
		
		public class ArgType<T>
		{
			public readonly T value;
			public readonly bool isValid;

			public ArgType(T value, bool isValid)
			{
				this.value = value;
				this.isValid = isValid;
			}
		}
	}
}