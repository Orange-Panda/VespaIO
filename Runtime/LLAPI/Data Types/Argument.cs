using JetBrains.Annotations;
using System;

namespace LMirman.VespaIO
{
	/// <summary>
	/// An argument is an abstract definition of an parameter for methods defined within commands.<br/>
	/// What makes an argument special is that it can be of multiple types simultaneously and know what is or isn't a valid type.<br/>
	/// Arguments are generated from string inputs in <see cref="Invocation"/> and will try to be as many types as possible so it can work with many methods.
	/// </summary>
	/// <remarks>
	/// Arguments are primarily used internally for the console before methods are actually invoked.<br/>
	/// Unless you are extending the console functionality it is unlikely you will ever need to interact with arguments directly.
	/// </remarks>
	[PublicAPI]
	public class Argument
	{
		public readonly ArgType<int> intValue;
		public readonly ArgType<float> floatValue;
		public readonly ArgType<bool> boolValue;
		public readonly ArgType<string> stringValue;

		public Argument(Word word)
		{
			// Set string value
			stringValue = new ArgType<string>(word.text, true);

			// Set int value
			bool didParseInt = !word.isLiteral & int.TryParse(word.text, out int intParse);
			intValue = new ArgType<int>(didParseInt ? intParse : -1, didParseInt);

			// Set float value
			bool didParseFloat = !word.isLiteral & float.TryParse(word.text, out float floatParse);
			floatValue = new ArgType<float>(didParseFloat ? floatParse : -1, didParseFloat);

			// Set bool value
			if (didParseInt)
			{
				boolValue = new ArgType<bool>(intParse > 0, true);
			}
			else
			{
				bool didParseBool = !word.isLiteral & bool.TryParse(word.text, out bool boolParse);
				boolValue = new ArgType<bool>(didParseBool && boolParse, didParseBool);
			}
		}

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
			else if (type == typeof(string))
			{
				return stringValue.isValid;
			}
			else
			{
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
			else
			{
				return false;
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