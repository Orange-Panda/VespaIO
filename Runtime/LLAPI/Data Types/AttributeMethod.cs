using System;
using System.Reflection;

namespace LMirman.VespaIO
{
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