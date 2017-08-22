using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.EventArguments;
using ICD.Common.Services;
using ICD.Common.Services.Logging;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Extensions;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Devices.Controls;
using ICD.Connect.Devices.Extensions;
using ICD.Connect.Krang.Partitioning.Partitions;
using ICD.Connect.Partitioning;
using ICD.Connect.Partitioning.Controls;
using ICD.Connect.Partitioning.Partitions;
using ICD.Connect.Settings;
using ICD.Connect.Settings.Core;

namespace ICD.Connect.Krang.Partitioning
{
	public sealed class PartitionManager : AbstractOriginator<PartitionManagerSettings>, IConsoleNode, IPartitionManager
	{
		private readonly PartitionsCollection m_Partitions;
		private readonly IcdHashSet<IPartitionDeviceControl> m_SubscribedPartitions;

		#region Properties

		public PartitionsCollection Partitions { get { return m_Partitions; } }

		IPartitionsCollection IPartitionManager.Partitions { get { return Partitions; } }

		/// <summary>
		/// Gets the name of the node.
		/// </summary>
		public string ConsoleName { get { return "PartitionManager"; } }

		/// <summary>
		/// Gets the help information for the node.
		/// </summary>
		public string ConsoleHelp { get { return "Tracks the opening and closing of partition walls."; } }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public PartitionManager()
		{
			m_Partitions = new PartitionsCollection(this);
			m_SubscribedPartitions = new IcdHashSet<IPartitionDeviceControl>();

			ServiceProvider.AddService<IPartitionManager>(this);
		}

		#region Controls

		/// <summary>
		/// Gets the control for the given partition.
		/// Returns null if the partition has no control specified.
		/// </summary>
		/// <param name="partition"></param>
		/// <returns></returns>
		public IPartitionDeviceControl GetControl(IPartition partition)
		{
			if (partition == null)
				throw new ArgumentNullException("partition");

			if (partition.PartitionControl == default(DeviceControlInfo))
				return null;

			return ServiceProvider.GetService<ICore>()
			                      .GetControl<IPartitionDeviceControl>(partition.PartitionControl);
		}

		#endregion

		#region Partition Callbacks

		/// <summary>
		/// Called when the partitions collection changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void PartitionsOnChildrenChanged(object sender, EventArgs eventArgs)
		{
			SubscribePartitions();
		}

		/// <summary>
		/// Subscribes to the partition controls.
		/// </summary>
		private void SubscribePartitions()
		{
			UnsubscribePartitions();

			m_SubscribedPartitions.AddRange(Partitions.Where(p => p.HasPartitionControl()).Select(p => GetControl(p)));

			foreach (IPartitionDeviceControl partition in m_SubscribedPartitions)
				Subscribe(partition);
		}

		/// <summary>
		/// Unsubscribes from the previously subscribed partitions.
		/// </summary>
		private void UnsubscribePartitions()
		{
			foreach (IPartitionDeviceControl partition in m_SubscribedPartitions)
				Unsubscribe(partition);
			m_SubscribedPartitions.Clear();
		}

		/// <summary>
		/// Subscribe to the partition events.
		/// </summary>
		/// <param name="partition"></param>
		private void Subscribe(IPartitionDeviceControl partition)
		{
			if (partition == null)
				return;

			partition.OnOpenStatusChanged += PartitionOnOpenStatusChanged;
		}

		/// <summary>
		/// Unsubscribe from the partition events.
		/// </summary>
		/// <param name="partition"></param>
		private void Unsubscribe(IPartitionDeviceControl partition)
		{
			if (partition == null)
				return;

			partition.OnOpenStatusChanged -= PartitionOnOpenStatusChanged;
		}

		/// <summary>
		/// Called when a partitions open state changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void PartitionOnOpenStatusChanged(object sender, BoolEventArgs args)
		{
			throw new NotImplementedException();
		}

		#endregion

		#region Settings

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			Partitions.Clear();
		}

		protected override void ApplySettingsFinal(PartitionManagerSettings settings, IDeviceFactory factory)
		{
			m_Partitions.OnPartitionsChanged -= PartitionsOnChildrenChanged;

			base.ApplySettingsFinal(settings, factory);

			IEnumerable<IPartition> partitions = GetPartitions(settings, factory);
			Partitions.SetPartitions(partitions);

			SubscribePartitions();

			m_Partitions.OnPartitionsChanged += PartitionsOnChildrenChanged;
		}

		private IEnumerable<IPartition> GetPartitions(PartitionManagerSettings settings, IDeviceFactory factory)
		{
			return GetOriginatorsSkipExceptions<IPartition>(settings.PartitionSettings, factory);
		}

		private IEnumerable<T> GetOriginatorsSkipExceptions<T>(IEnumerable<ISettings> originatorSettings, IDeviceFactory factory)
			where T : class, IOriginator
		{
			foreach (ISettings settings in originatorSettings)
			{
				T output;

				try
				{
					output = factory.GetOriginatorById<T>(settings.Id);
				}
				catch (Exception e)
				{
					Logger.AddEntry(eSeverity.Error, "Failed to instantiate {0} with id {1} - {2}", typeof(T).Name, settings.Id, e.Message);
					continue;
				}

				yield return output;
			}
		}

		protected override void CopySettingsFinal(PartitionManagerSettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.PartitionSettings.SetRange(Partitions.Where(c => c.Serialize).Select(r => r.CopySettings()));
		}

		#endregion

		#region Console

		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			yield break;
		}

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			addRow("Partitions Count", Partitions.Count);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			yield return new ConsoleCommand("PrintPartitions", "Prints the list of all partitions.", () => PrintPartitions());
		}

		private string PrintPartitions()
		{
			TableBuilder builder = new TableBuilder("Id", "Partition", "Device", "Control", "Rooms");

			foreach (IPartition partition in m_Partitions.OrderBy(c => c.Id))
			{
				int id = partition.Id;
				int device = partition.PartitionControl.DeviceId;
				int control = partition.PartitionControl.ControlId;
				string rooms = StringUtils.ArrayFormat(partition.GetRooms().Order());

				builder.AddRow(id, partition, device, control, rooms);
			}

			return builder.ToString();
		}

		#endregion
	}
}
