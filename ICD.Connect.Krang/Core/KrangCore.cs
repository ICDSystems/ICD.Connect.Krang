using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Permissions;
using ICD.Common.Properties;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Services;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Attributes;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Devices;
using ICD.Connect.Krang.Remote;
using ICD.Connect.Panels;
using ICD.Connect.Partitioning.PartitionManagers;
using ICD.Connect.Partitioning.Rooms;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Routing.RoutingGraphs;
using ICD.Connect.Settings;
using ICD.Connect.Settings.Core;
using ICD.Connect.Themes;

namespace ICD.Connect.Krang.Core
{
	public sealed class KrangCore : AbstractCore<KrangCoreSettings>
	{
		/// <summary>
		/// Originator ids are pushed to the stack on load, and popped on clear.
		/// </summary>
		private readonly Stack<int> m_LoadedOriginators;

		private readonly InterCoreCommunication m_InterCore;
		private readonly BroadcastSettings m_BroadcastSettings;

		#region Properties

		/// <summary>
		/// Gets the name of the node in the console.
		/// </summary>
		public override string ConsoleName { get { return "Core"; } }

		/// <summary>
		/// Gets the routing graph for the program.
		/// </summary>
		[CanBeNull]
		[ApiNode("Routing", "The routing features for the core.")]
		public RoutingGraph RoutingGraph { get { return Originators.GetChildren<RoutingGraph>().SingleOrDefault(); } }

		/// <summary>
		/// Gets the partition manager for the program.
		/// </summary>
		[CanBeNull]
		[ApiNode("Partitioning", "The partitioning features for the core.")]
		public PartitionManager PartitionManager
		{
			get { return Originators.GetChildren<PartitionManager>().SingleOrDefault(); }
		}

		[ApiNodeGroup("Themes", "The currently active themes")]
		private IApiNodeGroup Themes { get; set; }

		[ApiNodeGroup("Devices", "The currently active devices")]
		private IApiNodeGroup Devices { get; set; }

		[ApiNodeGroup("Panels", "The currently active panels")]
		private IApiNodeGroup Panels { get; set; }

		[ApiNodeGroup("Ports", "The currently active ports")]
		private IApiNodeGroup Ports { get; set; }

		[ApiNodeGroup("Rooms", "The currently active rooms")]
		private IApiNodeGroup Rooms { get; set; }

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		public KrangCore()
		{
			ServiceProvider.AddService<ICore>(this);

			m_LoadedOriginators = new Stack<int>();

			Themes = new ApiOriginatorsNodeGroup<ITheme>(Originators);
			Devices = new ApiOriginatorsNodeGroup<IDevice>(Originators);
			Panels = new ApiOriginatorsNodeGroup<IPanelDevice>(Originators);
			Ports = new ApiOriginatorsNodeGroup<IPort>(Originators);
			Rooms = new ApiOriginatorsNodeGroup<IRoom>(Originators);

			m_InterCore = new InterCoreCommunication(this);
			m_BroadcastSettings = new BroadcastSettings();
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeFinal(bool disposing)
		{
			m_InterCore.Dispose();

			base.DisposeFinal(disposing);

			DisposeOriginators();
		}

		/// <summary>
		/// Disposes the existing originators and adds the given originators.
		/// </summary>
		/// <param name="originators"></param>
		private void SetOriginators(IEnumerable<IOriginator> originators)
		{
			if (originators == null)
				throw new ArgumentNullException("originators");

			DisposeOriginators();
			Originators.SetChildren(originators);
		}

		/// <summary>
		/// Adds the given originator to the cor.
		/// </summary>
		/// <param name="originator"></param>
		private void AddOriginator(IOriginator originator)
		{
			if (originator == null)
				throw new ArgumentNullException("originator");

			Originators.AddChild(originator);
		}

		/// <summary>
		/// Unsubscribes, disposes and clears the devices.
		/// </summary>
		private void DisposeOriginators()
		{
			IcdHashSet<IOriginator> disposed = new IcdHashSet<IOriginator>();

			// First try to dispose in reverse of load order
			while (m_LoadedOriginators.Count > 0)
			{
				int id = m_LoadedOriginators.Pop();

				IOriginator originator;
				if (!Originators.TryGetChild(id, out originator))
					continue;

				TryDisposeOriginator(originator);
				disposed.Add(originator);
			}

			// Now dispose the remainder
			foreach (IOriginator originator in Originators.Where(o => !disposed.Contains(o)))
				TryDisposeOriginator(originator);

			Originators.Clear();
		}

		/// <summary>
		/// Attempts to dispose the originator if it implements IDisposable. Logs any exceptions.
		/// </summary>
		/// <param name="originator"></param>
		private void TryDisposeOriginator(IOriginator originator)
		{
			IDisposable disposable = originator as IDisposable;
			if (disposable == null)
				return;

			try
			{
				disposable.Dispose();
			}
			catch (Exception e)
			{
				Logger.AddEntry(eSeverity.Error, "{0} failed to dispose {1} - {2}", this, originator, e.Message);
			}
		}

		#endregion

		#region Settings

		/// <summary>
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(KrangCoreSettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.BroadcastSettings.Update(m_BroadcastSettings);

			settings.OriginatorSettings.Clear();
			settings.OriginatorSettings.AddRange(GetSerializableOriginators());

			RoutingGraph routingGraph = RoutingGraph;
			RoutingGraphSettings routingSettings = routingGraph == null || !routingGraph.Serialize
				                                       ? new RoutingGraphSettings
				                                       {
														   Id = IdUtils.GetNewId(Originators.GetChildrenIds(), IdUtils.ID_ROUTING_GRAPH)
				                                       }
				                                       : routingGraph.CopySettings();
			settings.OriginatorSettings.Add(routingSettings);

			settings.OriginatorSettings.AddRange(routingSettings.ConnectionSettings);
			settings.OriginatorSettings.AddRange(routingSettings.StaticRouteSettings);
			settings.OriginatorSettings.AddRange(routingSettings.SourceSettings);
			settings.OriginatorSettings.AddRange(routingSettings.DestinationSettings);
			settings.OriginatorSettings.AddRange(routingSettings.DestinationGroupSettings);

			PartitionManager partitionManager = PartitionManager;
			PartitionManagerSettings partitionSettings = partitionManager == null || !partitionManager.Serialize
				                                             ? new PartitionManagerSettings
				                                             {
																 Id = IdUtils.GetNewId(Originators.GetChildrenIds(), IdUtils.ID_PARTITION_MANAGER)
				                                             }
				                                             : partitionManager.CopySettings();
			settings.OriginatorSettings.Add(partitionSettings);

			settings.OriginatorSettings.AddRange(partitionSettings.PartitionSettings);
		}

		private IEnumerable<ISettings> GetSerializableOriginators()
		{
			return Originators.GetChildren()
			                    .Where(c => c.Serialize)
			                    .Select(p => p.CopySettings());
		}

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			SetOriginators(Enumerable.Empty<IOriginator>());

			m_InterCore.Stop();
			m_InterCore.SetBroadcastAddresses(Enumerable.Empty<string>());

			m_BroadcastSettings.Clear();

			ResetDefaultPermissions();
		}

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(KrangCoreSettings settings, IDeviceFactory factory)
		{
			m_LoadedOriginators.Clear();
			factory.OnOriginatorLoaded += FactoryOnOriginatorLoaded;

			try
			{
				base.ApplySettingsFinal(settings, factory);

				factory.LoadOriginators<IRoutingGraph>();
				factory.LoadOriginators<IPartitionManager>();
				LoadOriginatorsSkipExceptions(factory);

				ResetDefaultPermissions();

				ApplyBroadcastSettings(settings.BroadcastSettings);
			}
			finally
			{
				factory.OnOriginatorLoaded -= FactoryOnOriginatorLoaded;
			}
		}

		private void ApplyBroadcastSettings(BroadcastSettings settings)
		{
			m_BroadcastSettings.Update(settings);

			IEnumerable<string> addresses = m_BroadcastSettings.GetAddresses();
			m_InterCore.SetBroadcastAddresses(addresses);

			if (m_BroadcastSettings.Enabled)
				m_InterCore.Start();
			else
				m_InterCore.Stop();
		}

		private void LoadOriginatorsSkipExceptions(IDeviceFactory factory)
		{
			foreach (int id in factory.GetOriginatorIds())
			{
				try
				{
					// Don't care about the result, loaded originators will be handled by the load event.
					factory.GetOriginatorById(id);
				}
				catch (Exception e)
				{
					Logger.AddEntry(eSeverity.Error, e, "{0} failed to instantiate {1} with id {2} - {3}", this,
					                typeof(IOriginator).Name, id, e.Message);
				}
			}
		}

		/// <summary>
		/// Called each time an originator is loaded while applying settings.
		/// </summary>
		/// <param name="originator"></param>
		private void FactoryOnOriginatorLoaded(IOriginator originator)
		{
			m_LoadedOriginators.Push(originator.Id);
			AddOriginator(originator);
		}

		private void ResetDefaultPermissions()
		{
			PermissionsManager permissionsManager = ServiceProvider.TryGetService<PermissionsManager>();
			if (permissionsManager != null)
				permissionsManager.SetDefaultPermissions(GetPermissions());
		}

		#endregion

		#region Console

		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			foreach (IConsoleNodeBase node in GetBaseConsoleNodes())
				yield return node;

			foreach (IConsoleNodeBase node in KrangCoreConsole.GetConsoleNodes(this))
				yield return node;
		}

		/// <summary>
		/// Workaround for "unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleNodeBase> GetBaseConsoleNodes()
		{
			return base.GetConsoleNodes();
		}

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			KrangCoreConsole.BuildConsoleStatus(this, addRow);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			foreach (IConsoleCommand command in KrangCoreConsole.GetConsoleCommands(this))
				yield return command;
		}

		/// <summary>
		/// Workaround for "unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		#endregion
	}
}
