#if SIMPLSHARP
using Crestron.SimplSharp.Reflection;
#else
using System.Reflection;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Services;
using ICD.Common.Services.Logging;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Xml;
using ICD.Connect.Settings;
using ICD.Connect.Settings.Attributes.Factories;

namespace ICD.Connect.Krang.Settings
{
	public static class PluginFactory
	{
		/// <summary>
		/// Maps attribute type -> factory name -> factory method
		/// </summary>
		private static readonly Dictionary<Type, Dictionary<string, MethodInfo>> s_AttributeNameMethodMap;

		/// <summary>
		/// Maps settings type -> factory name
		/// </summary>
		private static readonly Dictionary<Type, string> s_SettingsFactoryNameMap;

		/// <summary>
		/// Constructor.
		/// </summary>
		static PluginFactory()
		{
			s_AttributeNameMethodMap = new Dictionary<Type, Dictionary<string, MethodInfo>>();
			s_SettingsFactoryNameMap = new Dictionary<Type, string>();

			try
			{
				BuildCache();
			}
			catch (Exception e)
			{
				IcdErrorLog.Exception(e.GetBaseException(), "Failed to cache plugins");
			}
		}

		#region Methods

		/// <summary>
		/// Finds the element in the xml document and instantiates the settings for each child.
		/// Skips and logs any elements that fail to parse.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="xml"></param>
		/// <param name="elementName"></param>
		/// <returns></returns>
		public static IEnumerable<ISettings> GetSettingsFromXml<T>(string xml, string elementName)
			where T : AbstractXmlFactoryMethodAttribute
		{
			string child;
			return XmlUtils.TryGetChildElementAsString(xml, elementName, out child)
					   ? GetSettingsFromXml<T>(child)
					   : Enumerable.Empty<ISettings>();
		}

		/// <summary>
		/// Instantiates the settings for each child element in the xml document.
		/// Skips and logs any elements that fail to parse.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="xml"></param>
		/// <returns></returns>
		public static IEnumerable<ISettings> GetSettingsFromXml<T>(string xml)
			where T : AbstractXmlFactoryMethodAttribute
		{
			foreach (string element in XmlUtils.GetChildElementsAsString(xml))
			{
				ISettings output;

				try
				{
					output = Instantiate<T>(element);
				}
				catch (Exception e)
				{
					ServiceProvider.TryGetService<ILoggerService>()
								   .AddEntry(eSeverity.Error, e, "Unable to parse settings element - {0}", e.Message);
					continue;
				}

				yield return output;
			}
		}

		/// <summary>
		/// Gets the factory name for the given settings type.
		/// </summary>
		/// <typeparam name="TSettings"></typeparam>
		/// <returns></returns>
		public static string GetFactoryName<TSettings>()
			where TSettings : ISettings
		{
			Type type = typeof(TSettings);

			if (!s_SettingsFactoryNameMap.ContainsKey(type))
				throw new KeyNotFoundException(string.Format("Unable to find factory name for {0}", type.Name));
			return s_SettingsFactoryNameMap[type];
		}

		/// <summary>
		/// Gets the available factory names.
		/// </summary>
		/// <returns></returns>
		public static IEnumerable<string> GetFactoryNames<TAttribute>()
			where TAttribute : AbstractXmlFactoryMethodAttribute
		{
			Type type = typeof(TAttribute);
			if (!s_AttributeNameMethodMap.ContainsKey(type))
				return Enumerable.Empty<string>();

			return s_AttributeNameMethodMap[typeof(TAttribute)].Keys
															   .Order()
															   .ToArray();
		}

		/// <summary>
		/// Passes the xml to an available factory method and returns the result.
		/// </summary>
		/// <param name="xml"></param>
		/// <returns></returns>
		public static ISettings Instantiate<TAttribute>(string xml)
			where TAttribute : AbstractXmlFactoryMethodAttribute
		{
			return Instantiate<ISettings, TAttribute>(xml);
		}

		/// <summary>
		/// Calls the default constructor for the class with the given factory name.
		/// </summary>
		/// <returns></returns>
		public static ISettings InstantiateDefault<TAttribute>(string factoryName)
			where TAttribute : AbstractXmlFactoryMethodAttribute
		{
			return InstantiateDefault<ISettings, TAttribute>(factoryName);
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Passes the xml to an available factory method and returns the result.
		/// </summary>
		/// <param name="xml"></param>
		/// <typeparam name="TSettings"></typeparam>
		/// <typeparam name="TAttribute"></typeparam>
		/// <returns></returns>
		private static TSettings Instantiate<TSettings, TAttribute>(string xml)
			where TSettings : ISettings
			where TAttribute : AbstractXmlFactoryMethodAttribute
		{
			MethodInfo method = GetMethodFromXml<TAttribute>(xml);

			try
			{
				return (TSettings)method.Invoke(null, new object[] {xml});
			}
			catch (TargetInvocationException e)
			{
				throw e.InnerException;
			}
		}

		/// <summary>
		/// Calls the default constructor for the class with the given factory name.
		/// </summary>
		/// <param name="factoryName"></param>
		/// <typeparam name="TSettings"></typeparam>
		/// <typeparam name="TAttribute"></typeparam>
		/// <returns></returns>
		private static TSettings InstantiateDefault<TSettings, TAttribute>(string factoryName)
			where TSettings : ISettings
			where TAttribute : AbstractXmlFactoryMethodAttribute
		{
			if (factoryName == null)
				throw new ArgumentNullException("factoryName");

			MethodInfo method = GetMethod<TAttribute>(factoryName);

#if SIMPLSHARP
			CType type = method.DeclaringType;
			ConstructorInfo ctor = type.GetConstructor(new CType[0]);
#else
			Type type = method.DeclaringType;
			ConstructorInfo ctor = type.GetTypeInfo().GetConstructor(new Type[0]);
#endif

			try
			{
				return (TSettings)ctor.Invoke(new object[0]);
			}
			catch (TargetInvocationException e)
			{
				throw e.GetBaseException();
			}
		}

		/// <summary>
		/// Gets the factory method with the given type name.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		private static MethodInfo GetMethod<T>(string name)
			where T : AbstractXmlFactoryMethodAttribute
		{
			if (name == null)
				throw new ArgumentNullException("name");

			try
			{
				return s_AttributeNameMethodMap[typeof(T)][name];
			}
			catch (KeyNotFoundException)
			{
				string message = string.Format("No {0} found with name {1}", typeof(T).Name, name);
				throw new KeyNotFoundException(message);
			}
		}

		/// <summary>
		/// Finds the "Type" child element in the xml and looks up the method.
		/// </summary>
		/// <param name="xml"></param>
		/// <returns></returns>
		private static MethodInfo GetMethodFromXml<T>(string xml)
			where T : AbstractXmlFactoryMethodAttribute
		{
			string type = XmlUtils.GetAttributeAsString(xml, AbstractSettings.TYPE_ATTRIBUTE);
			return GetMethod<T>(type);
		}

		/// <summary>
		/// Lazy loads the cache.
		/// </summary>
		private static void BuildCache()
		{
			IEnumerable<Assembly> assemblies = LibraryUtils.GetPluginAssemblies();

			foreach (Assembly assembly in assemblies)
			{
				AttributeUtils.CacheAssembly(assembly);
				ServiceProvider.TryGetService<ILoggerService>()
							   .AddEntry(eSeverity.Informational, "Loaded plugin {0}", assembly.GetName().Name);
			}

			foreach (AbstractXmlFactoryMethodAttribute attribute in AttributeUtils.GetMethodAttributes<AbstractXmlFactoryMethodAttribute>().OrderBy(a => a.TypeName))
			{
				ServiceProvider.TryGetService<ILoggerService>()
							   .AddEntry(eSeverity.Informational, "Loaded type {0}", attribute.TypeName);

				MethodInfo method = AttributeUtils.GetMethod(attribute);
				Type attributeType = attribute.GetType();

				if (!s_AttributeNameMethodMap.ContainsKey(attributeType))
					s_AttributeNameMethodMap[attributeType] = new Dictionary<string, MethodInfo>();
				s_AttributeNameMethodMap[attributeType][attribute.TypeName] = method;

				s_SettingsFactoryNameMap[method.DeclaringType] = attribute.TypeName;
			}
		}

		#endregion
	}
}
