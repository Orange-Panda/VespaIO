using JetBrains.Annotations;
using System;

namespace LMirman.VespaIO
{
	[PublicAPI]
	[MeansImplicitUse]
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
	public class CommandAutofillAttribute : Attribute
	{
		/// <inheritdoc cref="ICommandProperties.Key"/>
		public string Key { get; private set; }

		public CommandAutofillAttribute(string key)
		{
			Key = key.CleanseKey();
		}
	}
}