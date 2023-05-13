using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;

namespace LMirman.VespaIO
{
	public static class VespaReflection
	{
		public const BindingFlags StaticMethodBindingFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.InvokeMethod | BindingFlags.DeclaredOnly;
		public const BindingFlags CommandBindingFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.InvokeMethod | BindingFlags.DeclaredOnly;

		public static List<CommandDefinition> GetCommandDefinitionsFromClasses<T>(List<Type> classes, BindingFlags searchFlags)
			where T : Attribute, ICommandProperties
		{
			List<CommandDefinition> commands = new List<CommandDefinition>();
			foreach (Type type in classes)
			{
				foreach (MethodInfo method in type.GetMethods(searchFlags))
				{
					object[] customAttributes = method.GetCustomAttributes(typeof(T), false);
					foreach (object customAttribute in customAttributes)
					{
						if (customAttribute is T attribute)
						{
							BindingFlags bindingFlags = BindingFlags.InvokeMethod;
							if (method.IsStatic)
							{
								bindingFlags |= BindingFlags.Static;
							}
							
							commands.Add(new CommandDefinition(attribute, method, bindingFlags));
						}
					}
				}

				foreach (PropertyInfo property in type.GetProperties(searchFlags))
				{
					object[] customAttributes = property.GetCustomAttributes(typeof(T), false);
					foreach (object customAttribute in customAttributes)
					{
						if (customAttribute is T attribute)
						{
							BindingFlags bindingFlags = BindingFlags.Default;
							if (property.GetAccessors()[0].IsStatic)
							{
								bindingFlags |= BindingFlags.Static;
							}
							
							if (property.CanRead)
							{
								bindingFlags |= BindingFlags.GetProperty;
							}

							if (property.CanWrite)
							{
								bindingFlags |= BindingFlags.SetProperty;
							}

							commands.Add(new CommandDefinition(attribute, property, bindingFlags));
						}
					}
				}

				foreach (FieldInfo field in type.GetFields(searchFlags))
				{
					object[] customAttributes = field.GetCustomAttributes(typeof(T), false);
					foreach (object customAttribute in customAttributes)
					{
						if (customAttribute is T attribute)
						{
							BindingFlags bindingFlags = BindingFlags.GetField;
							if (field.IsStatic)
							{
								bindingFlags |= BindingFlags.Static;
							}

							if (!field.IsInitOnly)
							{
								bindingFlags |= BindingFlags.SetField;
							}

							commands.Add(new CommandDefinition(attribute, field, bindingFlags));
						}
					}
				}
			}

			return commands;
		}

		public static List<AttributeMethod> GetAttributeMethodsFromClasses<T>(List<Type> classes, BindingFlags bindingFlags) where T : Attribute
		{
			List<AttributeMethod> attributes = new List<AttributeMethod>();
			foreach (Type type in classes)
			{
				foreach (MethodInfo method in type.GetMethods(bindingFlags))
				{
					object[] customAttributes = method.GetCustomAttributes(typeof(T), false);
					foreach (object customAttribute in customAttributes)
					{
						if (customAttribute is T attribute)
						{
							attributes.Add(new AttributeMethod(attribute, method));
						}
					}
				}
			}

			return attributes;
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