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
		public event OriginatorLoadedCallback OnOriginatorLoaded;

		private readonly Dictionary<int, IOriginator> m_OriginatorCache;
		private readonly ICoreSettings m_CoreSettings;

		private ILoggerService Logger { get { return ServiceProvider.TryGetService<ILoggerService>(); } }

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

		/// <summary>
		/// Returns true if the factory contains any settings that will resolve to the given originator type.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public bool HasOriginators<T>()
			where T : class, IOriginator
		{
			return m_CoreSettings.OriginatorSettings.Any(s => s.OriginatorType.IsAssignableTo(typeof(T)));
		}

		public IEnumerable<T> GetOriginators<T>()
			where T : class, IOriginator
		{
			return m_CoreSettings.OriginatorSettings
			                     .Where(s => s.OriginatorType.IsAssignableTo(typeof(T)))
			                     .Select(s => GetOriginatorById<T>(s.Id));
		}

		[CanBeNull]
		public T GetOriginatorById<T>(int id)
			where T : class, IOriginator
		{
			return LazyLoadOriginator<T>(id);
		}

		[CanBeNull]
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
		[CanBeNull]
		private T LazyLoadOriginator<T>(int id)
			where T : class, IOriginator
		{
			try
			{
				if (!m_OriginatorCache.ContainsKey(id))
				{
					m_OriginatorCache[id] = InstantiateOriginatorWithId<T>(id);

					OriginatorLoadedCallback handler = OnOriginatorLoaded;
					if (handler != null)
						handler(m_OriginatorCache[id]);
				}

				return (T)m_OriginatorCache[id];
			}
			catch (Exception e)
			{
				Logger.AddEntry(eSeverity.Error, e, "{0} failed to load originator {1} id {2}", GetType().Name, typeof(T).Name, id);
			}

			return default(T);
		}

		/// <summary>
		/// Builds the originator from the settings collection with the given id.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="id"></param>
		[NotNull]
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
