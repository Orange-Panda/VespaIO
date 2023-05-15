using System;
using System.Reflection;

namespace LMirman.VespaIO
{
	/// <summary>
	/// Data type that associates an <see cref="Attribute"/> with a particular <see cref="MethodInfo"/>.
	/// </summary>
	public class AttributeMethod
	{
		public readonly Attribute attribute;
		public readonly MethodInfo methodInfo;

		public AttributeMethod(Attribute attribute, MethodInfo methodInfo)
		{
			this.attribute = attribute;
			this.methodInfo = methodInfo;
		}
	}
}