using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Services;
using ICD.Common.Services.Logging;
using ICD.Connect.Krang.Settings;
using ICD.Connect.Settings;
using ICD.Connect.Settings.Core;

namespace ICD.Connect.Krang.Core
{
	/// <summary>
	/// CoreDeviceFactory wraps an ICoreSettings to provide a device factory.
	/// </summary>
	public sealed class CoreDeviceFactory : IDeviceFactory
	{
		private readonly Dictionary<int, IOriginator>  m_OriginatorCache;

		private readonly ICoreSettings m_CoreSettings;

		private readonly ICore m_Core;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="coreSettings"></param>
		/// <param name="core"></param>
		public CoreDeviceFactory(ICoreSettings coreSettings, ICore core)
		{
			m_OriginatorCache = new Dictionary<int, IOriginator>();
			m_CoreSettings = coreSettings;

			m_Core = core;
		}

		#region Methods

		public T GetOriginatorById<T>(int id) where T : class, IOriginator
		{
			return LazyLoadOriginator<T>(id, m_CoreSettings.OriginatorSettings);
		} 

		public IOriginator GetOriginatorById(int id)
		{
			return LazyLoadOriginator<IOriginator>(id, m_CoreSettings.OriginatorSettings);
		}

		public IEnumerable<int> GetOriginatorIds()
		{
			return m_CoreSettings.OriginatorSettings.Select(s => s.Id);
		}

		public ICore GetCore()
		{
			return m_Core;
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Gets the originator with the given id.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="cache"></param>
		/// <param name="id"></param>
		/// <param name="collection"></param>
		/// <returns></returns>
		[CanBeNull]
		private T LazyLoadOriginator<T>(int id, IEnumerable<ISettings> collection)
			where T : class, IOriginator
		{
			if (!m_OriginatorCache.ContainsKey(id))
			{
				T originator = InstantiateOriginatorWithId<T>(id, collection);
				if (originator == null)
				{
					ServiceProvider.TryGetService<ILoggerService>()
					               .AddEntry(eSeverity.Error, "{0} failed to get {1} with id {2}", GetType().Name, typeof(T).Name, id);
					return null;
				}

				m_OriginatorCache[id] = originator;
			}

			return (T)m_OriginatorCache[id];
		}

		/// <summary>
		/// Builds the originator from the settings collection with the given id.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="id"></param>
		/// <param name="collection"></param>
		[CanBeNull]
		private T InstantiateOriginatorWithId<T>(int id, IEnumerable<ISettings> collection)
			where T : IOriginator
		{
			ISettings settings = collection.FirstOrDefault(s => s.Id == id);
			if (settings == null)
				return default(T);
			return (T)settings.ToOriginator(this);
		}

		#endregion
	}
}
