using System.Reflection;

namespace LMirman.VespaIO
{
	public class CommandDefinition
	{
		public readonly BindingFlags bindingFlags;
		public readonly ICommandProperties properties;
		public readonly MethodInfo methodInfo;
		public readonly PropertyInfo propertyInfo;

		public CommandDefinition(ICommandProperties properties, MethodInfo methodInfo, BindingFlags bindingFlags)
		{
			this.properties = properties;
			this.methodInfo = methodInfo;
			this.bindingFlags = bindingFlags;
		}

		public CommandDefinition(ICommandProperties properties, PropertyInfo propertyInfo, BindingFlags bindingFlags)
		{
			this.properties = properties;
			this.propertyInfo = propertyInfo;
			this.bindingFlags = bindingFlags;
		}
	}
}