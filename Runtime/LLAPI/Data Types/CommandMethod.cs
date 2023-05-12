using System.Reflection;

namespace LMirman.VespaIO
{
	public class CommandMethod
	{
		public readonly ICommandProperties properties;
		public readonly MethodInfo methodInfo;

		public CommandMethod(ICommandProperties properties, MethodInfo methodInfo)
		{
			this.properties = properties;
			this.methodInfo = methodInfo;
		}
	}
}