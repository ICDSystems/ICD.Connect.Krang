using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.Devices;
using ICD.Connect.Krang.SPlus.Routing.Endpoints.Sources;
using ICD.Connect.Settings;

namespace ICD.Connect.Krang.SPlus.Routing.KrangAtHomeSourceGroup
{
	public sealed class KrangAtHomeSourceGroup : AbstractDevice<KrangAtHomeSourceGroupSettings>, IKrangAtHomeSourceGroup
	{

		private readonly IcdOrderedDictionary<int, List<IKrangAtHomeSource>> m_Sources;

		public eSourceVisibility SourceVisibility { get; set; }

		public KrangAtHomeSourceGroup()
		{
			m_Sources = new IcdOrderedDictionary<int, List<IKrangAtHomeSource>>();
		}

		/// <summary>
		/// The number of sources in the group
		/// </summary>
		public int Count { get { return m_Sources.Count; } }

		public IEnumerable<IKrangAtHomeSource> GetSources()
		{
			return m_Sources.SelectMany(kvp => kvp.Value);
		}

		/// <summary>
		/// Adds a source/priority pair to the sources collection
		/// </summary>
		/// <param name="source"></param>
		/// <param name="factory"></param>
		private void AddSource(KeyValuePair<int, int> source, IDeviceFactory factory)
		{
			AddSource(source.Value, source.Key, factory);
		}

		/// <summary>
		/// Adds a source to the collection with the given priority
		/// </summary>
		/// <param name="priority"></param>
		/// <param name="sourceId"></param>
		/// <param name="factory"></param>
		private void AddSource(int priority, int sourceId, IDeviceFactory factory)
		{
			try
			{
				IKrangAtHomeSource source = factory.GetOriginatorById<IKrangAtHomeSource>(sourceId);

				AddSource(priority, source);
			}
			catch (KeyNotFoundException)
			{
				Log(eSeverity.Error, "No originator with id {0}", sourceId);
			}
			catch (InvalidCastException)
			{
				Log(eSeverity.Error, "Originator at id {0} isn't a IKrangAtHomeSource", sourceId);
			}
		}

		private void AddSource(int priority, IKrangAtHomeSource source)
		{
			List<IKrangAtHomeSource> priorityList;
			if (!m_Sources.TryGetValue(priority, out priorityList))
			{
				priorityList = new List<IKrangAtHomeSource>();
				m_Sources.Add(priority, priorityList);
			}

			priorityList.Add(source);
		}

		private Dictionary<int, int> SourcesToDictionary()
		{
			Dictionary<int, int> dictionary = new Dictionary<int, int>();

			foreach (var kvp in m_Sources)
			{
				foreach (var source in kvp.Value)
				{
					dictionary.Add(source.Id, kvp.Key);
				}
			}

			return dictionary;
		}

		#region Settings

		/// <summary>
		/// Gets the current online status of the device.
		/// </summary>
		/// <returns></returns>
		protected override bool GetIsOnlineStatus()
		{
			return true;
		}

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(KrangAtHomeSourceGroupSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			settings.Sources.ForEach(kvp => AddSource(kvp, factory));
		}

		/// <summary>
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(KrangAtHomeSourceGroupSettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.Sources = SourcesToDictionary();
		}

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			m_Sources.Clear();
		}

		#endregion

		#region Console

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			yield return new ConsoleCommand("Print Source Table", "Prints the table of sources and their priority", () => PrintSources());


		}

		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		private void PrintSources()
		{
			TableBuilder table = new TableBuilder("Priority", "Sources");
			foreach (KeyValuePair<int, List<IKrangAtHomeSource>> l in m_Sources)
			{
				foreach (IKrangAtHomeSource s in l.Value)
					table.AddRow(l.Key.ToString(),s);
			}

			IcdConsole.PrintLine(table.ToString());
		}

		#endregion
	}
}