using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;

namespace LMirman.VespaIO
{
	public static class VespaReflection
	{
		public const BindingFlags InstanceMethodBindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.InvokeMethod | BindingFlags.DeclaredOnly;
		public const BindingFlags StaticMethodBindingFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.InvokeMethod | BindingFlags.DeclaredOnly;

		public static List<CommandMethod> GetCommandMethodFromClasses<T>(List<Type> classes, BindingFlags bindingFlags) where T : Attribute, ICommandProperties
		{
			List<CommandMethod> commands = new List<CommandMethod>();
			foreach (Type type in classes)
			{
				foreach (MethodInfo method in type.GetMethods(bindingFlags))
				{
					object[] customAttributes = method.GetCustomAttributes(typeof(T), false);
					foreach (object customAttribute in customAttributes)
					{
						if (customAttribute is T attribute)
						{
							commands.Add(new CommandMethod(attribute, method));
						}
					}
				}
			}

			return commands;
		}

		/// <summary>
		/// Get a list of all classes within an array of assemblies
		/// </summary>
		public static List<Type> GetClassesFromAssemblies(Assembly[] assemblies)
		{
			List<Type> classes = new List<Type>();
			foreach (Assembly assembly in assemblies)
			{
				Type[] types = assembly.GetTypes();
				foreach (Type type in types)
				{
					if (type.IsClass)
					{
						classes.Add(type);
					}
				}
			}

			return classes;
		}

		/// <summary>
		/// Get an array of assemblies whose <see cref="Assembly.FullName"/> is a match for a particular <see cref="Regex"/>.
		/// </summary>
		/// <param name="domain">The domain to collect assemblies from</param>
		/// <param name="regexMatch">The regular expression that the Assembly must match to be returned by this method.</param>
		public static Assembly[] GetAssembliesFromDomain(AppDomain domain, Regex regexMatch)
		{
			Assembly[] assemblies = domain.GetAssemblies();
			List<Assembly> validAssemblies = new List<Assembly>(assemblies.Length);
			foreach (Assembly assembly in assemblies)
			{
				if (assembly != null && regexMatch.IsMatch(assembly.FullName))
				{
					validAssemblies.Add(assembly);
				}
			}

			return validAssemblies.ToArray();
		}
	}
}