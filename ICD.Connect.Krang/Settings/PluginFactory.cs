using ICD.Common.Utils.Extensions;
using ICD.Connect.Settings.Attributes;
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
using ICD.Common.Utils.Xml;
using ICD.Connect.Settings;

namespace ICD.Connect.Krang.Settings
{
	public static class PluginFactory
	{
		/// <summary>
		/// Maps factory name -> factory method
		/// </summary>
		private static readonly Dictionary<string, MethodInfo> s_FactoryNameMethodMap;

		/// <summary>
		/// Maps settings type -> factory name
		/// </summary>
		private static readonly Dictionary<Type, string> s_SettingsFactoryNameMap;

		private static ILoggerService Logger { get { return ServiceProvider.TryGetService<ILoggerService>(); } }

		/// <summary>
		/// Constructor.
		/// </summary>
		static PluginFactory()
		{
			s_FactoryNameMethodMap = new Dictionary<string, MethodInfo>();
			s_SettingsFactoryNameMap = new Dictionary<Type, string>();

			try
			{
				BuildCache();
			}
			catch (Exception e)
			{
				Logger.AddEntry(eSeverity.Error, e, "{0} failed to cache plugins", typeof(PluginFactory).Name);
			}
		}

		#region Methods

		/// <summary>
		/// Finds the element in the xml document and instantiates the settings for each child.
		/// Skips and logs any elements that fail to parse.
		/// </summary>
		/// <param name="xml"></param>
		/// <param name="elementName"></param>
		/// <returns></returns>
		public static IEnumerable<ISettings> GetSettingsFromXml(string xml, string elementName)
		{
			string child;
			return XmlUtils.TryGetChildElementAsString(xml, elementName, out child)
					   ? GetSettingsFromXml(child)
					   : Enumerable.Empty<ISettings>();
		}

		/// <summary>
		/// Instantiates the settings for each child element in the xml document.
		/// Skips and logs any elements that fail to parse.
		/// </summary>
		/// <param name="xml"></param>
		/// <returns></returns>
		public static IEnumerable<ISettings> GetSettingsFromXml(string xml)
		{
			foreach (string element in XmlUtils.GetChildElementsAsString(xml))
			{
				ISettings output;

				try
				{
					output = Instantiate(element);
				}
				catch (Exception e)
				{
					Logger.AddEntry(eSeverity.Error, e, "Unable to parse settings element - {0}", e.Message);
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
		public static IEnumerable<string> GetFactoryNames<TSettings>()
			where TSettings : ISettings
		{
			return s_SettingsFactoryNameMap.Where(kvp => kvp.Key.IsAssignableTo(typeof(TSettings)))
			                               .Select(kvp => kvp.Value);
		}

		/// <summary>
		/// Gets all available factory names.
		/// </summary>
		/// <returns></returns>
		public static IEnumerable<string> GetFactoryNames()
		{
			return s_FactoryNameMethodMap.Keys;
		}

		/// <summary>
		/// Gets the assemblies for the loaded factories.
		/// </summary>
		/// <returns></returns>
		public static IEnumerable<Assembly> GetFactoryAssemblies()
		{
			return s_FactoryNameMethodMap.Values
			                             .Select(v => v.DeclaringType
#if !SIMPLSHARP
			                                           .GetTypeInfo()
#endif
			                                           .Assembly)
			                             .Distinct();
		}

		/// <summary>
		/// Passes the xml to an available factory method and returns the result.
		/// </summary>
		/// <param name="xml"></param>
		/// <returns></returns>
		public static ISettings Instantiate(string xml)
		{
			return Instantiate<ISettings>(xml);
		}

		/// <summary>
		/// Calls the default constructor for the class with the given factory name.
		/// </summary>
		/// <returns></returns>
		public static ISettings InstantiateDefault(string factoryName)
		{
			return InstantiateDefault<ISettings>(factoryName);
		}

		/// <summary>
		/// Gets the settings type for the given factory name.
		/// </summary>
		/// <param name="factoryName"></param>
		/// <returns></returns>
		public static Type GetType(string factoryName)
		{
			MethodInfo methodInfo;
			if (!s_FactoryNameMethodMap.TryGetValue(factoryName, out methodInfo))
				throw new KeyNotFoundException(string.Format("No factory name {0}", factoryName));

			return methodInfo.DeclaringType;
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Passes the xml to an available factory method and returns the result.
		/// </summary>
		/// <param name="xml"></param>
		/// <typeparam name="TSettings"></typeparam>
		/// <returns></returns>
		private static TSettings Instantiate<TSettings>(string xml)
			where TSettings : ISettings
		{
			MethodInfo method = GetMethodFromXml(xml);

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
		/// <returns></returns>
		private static TSettings InstantiateDefault<TSettings>(string factoryName)
			where TSettings : ISettings
		{
			if (factoryName == null)
				throw new ArgumentNullException("factoryName");

			MethodInfo method = GetMethod(factoryName);

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
		private static MethodInfo GetMethod(string name)
		{
			if (name == null)
				throw new ArgumentNullException("name");

			try
			{
				return s_FactoryNameMethodMap[name];
			}
			catch (KeyNotFoundException)
			{
				string message = string.Format("No factory found with name {0}", name);
				throw new KeyNotFoundException(message);
			}
		}

		/// <summary>
		/// Finds the "Type" child element in the xml and looks up the method.
		/// </summary>
		/// <param name="xml"></param>
		/// <returns></returns>
		private static MethodInfo GetMethodFromXml(string xml)
		{
			string type = XmlUtils.GetAttributeAsString(xml, AbstractSettings.TYPE_ATTRIBUTE);
			return GetMethod(type);
		}

		/// <summary>
		/// Lazy loads the cache.
		/// </summary>
		private static void BuildCache()
		{
			IEnumerable<Assembly> assemblies = LibraryUtils.GetPluginAssemblies();

			foreach (Assembly assembly in assemblies)
			{
				try
				{
					if (AttributeUtils.CacheAssembly(assembly))
						Logger.AddEntry(eSeverity.Informational, "Loaded plugin {0}", assembly.GetName().Name);
				}
				catch (Exception e)
				{
					Logger.AddEntry(eSeverity.Error, e, "{0} failed to load plugin {1}", typeof(PluginFactory).Name,
					                assembly.GetName().Name);
				}
			}

			foreach (
				XmlFactoryMethodAttribute attribute in
					AttributeUtils.GetMethodAttributes<XmlFactoryMethodAttribute>().OrderBy(a => a.FactoryName))
			{
				Logger.AddEntry(eSeverity.Informational, "Loaded type {0}", attribute.FactoryName);

				MethodInfo method = AttributeUtils.GetMethod(attribute);

				s_FactoryNameMethodMap.Add(attribute.FactoryName, method);

				s_SettingsFactoryNameMap[method.DeclaringType] = attribute.FactoryName;
			}
		}

		#endregion
	}
}
