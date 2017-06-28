using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Services;
using ICD.Common.Services.Logging;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Settings;
using ICD.Connect.Settings.Core;

namespace ICD.Connect.Krang.Core
{
	/// <summary>
	/// CoreDeviceFactory wraps an ICoreSettings to provide a device factory.
	/// </summary>
	public sealed class CoreDeviceFactory : IDeviceFactory
	{
		private readonly Dictionary<int, IOriginator> m_OriginatorCache;
		private readonly ICoreSettings m_CoreSettings;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="coreSettings"></param>
		public CoreDeviceFactory(ICoreSettings coreSettings)
		{
			m_OriginatorCache = new Dictionary<int, IOriginator>();
			m_CoreSettings = coreSettings;
		}

		#region Methods

		[NotNull]
		public T GetOriginatorById<T>(int id)
			where T : class, IOriginator
		{
			return LazyLoadOriginator<T>(id);
		} 

		[NotNull]
		public IOriginator GetOriginatorById(int id)
		{
			return LazyLoadOriginator<IOriginator>(id);
		}

		public IEnumerable<int> GetOriginatorIds()
		{
			return m_CoreSettings.OriginatorSettings.Select(s => s.Id);
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Gets the originator with the given id.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="id"></param>
		/// <returns></returns>
		private T LazyLoadOriginator<T>(int id)
			where T : class, IOriginator
		{
			if (m_OriginatorCache.ContainsKey(id) && !m_OriginatorCache[id].GetType().IsAssignableTo(typeof(T)))
				throw new InvalidOperationException(string.Format("{0} with id {1} is not of type {2}",
				                                                  m_OriginatorCache[id].GetType().Name, id, typeof(T).Name));

			if (!m_OriginatorCache.ContainsKey(id))
				m_OriginatorCache[id] = InstantiateOriginatorWithId<T>(id);

			return (T)m_OriginatorCache[id];
		}

		/// <summary>
		/// Builds the originator from the settings collection with the given id.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="id"></param>
		private T InstantiateOriginatorWithId<T>(int id)
			where T : IOriginator
		{
			if (!m_CoreSettings.OriginatorSettings.ContainsId(id))
				throw new KeyNotFoundException(string.Format("No settings with id {0}", id));

			ISettings settings = m_CoreSettings.OriginatorSettings.GetById(id);
			if (!settings.OriginatorType.IsAssignableTo(typeof(T)))
				throw new InvalidOperationException(string.Format("{0} will not yield an originator of type {1}",
				                                                  settings.GetType().Name, typeof(T).Name));

			return (T)settings.ToOriginator(this);
		}

		#endregion
	}
}
