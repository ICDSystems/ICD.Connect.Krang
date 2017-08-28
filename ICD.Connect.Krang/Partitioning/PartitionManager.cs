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
using ICD.Connect.Partitioning.Rooms;
using ICD.Connect.Settings;
using ICD.Connect.Settings.Core;

namespace ICD.Connect.Krang.Partitioning
{
	public sealed class PartitionManager : AbstractOriginator<PartitionManagerSettings>, IConsoleNode, IPartitionManager
	{
		/// <summary>
		/// Raised when a parition control opens/closes.
		/// </summary>
		public event PartitionControlOpenStateCallback OnPartitionOpenStateChange;

		private readonly PartitionsCollection m_Partitions;
		private readonly IcdHashSet<IPartitionDeviceControl> m_SubscribedPartitions;

		#region Properties

		private static ICore Core { get { return ServiceProvider.GetService<ICore>(); } }

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

		/// <summary>
		/// Override to release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
			OnPartitionOpenStateChange = null;

			base.DisposeFinal(disposing);
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

		#region Combine

		/// <summary>
		/// Returns true if the given partition is currently part of a combine room.
		/// </summary>
		/// <param name="partition"></param>
		/// <returns></returns>
		public bool CombinesRoom(IPartition partition)
		{
			if (partition == null)
				throw new ArgumentNullException("partition");

			return CombinesRoom(partition.Id);
		}

		/// <summary>
		/// Returns true if the given partition is currently part of a combine room.
		/// </summary>
		/// <param name="partitionId"></param>
		/// <returns></returns>
		public bool CombinesRoom(int partitionId)
		{
			return GetRooms().Any(r => r.Partitions.ContainsRecursive(partitionId));
		}

		/// <summary>
		/// Gets the combine room containing the given partition.
		/// </summary>
		/// <param name="partition"></param>
		/// <returns></returns>
		public IRoom GetCombineRoom(IPartition partition)
		{
			if (partition == null)
				throw new ArgumentNullException("partition");

			return GetRooms().FirstOrDefault(r => r.Partitions.ContainsRecursive(partition.Id));
		}

		/// <summary>
		/// Returns combine rooms and any individual rooms that are not part of a combined space.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<IRoom> GetTopLevelRooms()
		{
			IRoom[] rooms = GetRooms().OrderByDescending(r => r.Partitions.Count).ToArray();
			IcdHashSet<IRoom> visited = new IcdHashSet<IRoom>();

			foreach (IRoom room in rooms)
			{
				if (visited.Contains(room))
					continue;

				visited.Add(room);
				visited.AddRange(room.GetRoomsRecursive());

				yield return room;
			}
		}

		/// <summary>
		/// Toggles the given partition to create a new combine room or uncombine an existing room.
		/// </summary>
		/// <typeparam name="TRoom"></typeparam>
		/// <param name="partition"></param>
		/// <param name="func"></param>
		public void ToggleCombineRooms<TRoom>(IPartition partition, Func<TRoom> func) where TRoom : IRoom
		{
			IRoom room = GetCombineRoom(partition);

			if (room == null)
				CombineRooms(partition, func);
			else
				UncombineRooms(partition, func);
		}

		/// <summary>
		/// Creates a new room instance, or expands an existing room instance, to contain the given partitions.
		/// </summary>
		/// <typeparam name="TRoom"></typeparam>
		/// <param name="partitions"></param>
		/// <param name="constructor"></param>
		public void CombineRooms<TRoom>(IEnumerable<IPartition> partitions, Func<TRoom> constructor)
			where TRoom : IRoom
		{
			if (partitions == null)
				throw new ArgumentNullException("partitions");

			if (constructor == null)
				throw new ArgumentNullException("constructor");

			foreach (IPartition partition in partitions)
				CombineRooms(partition, constructor);
		}

		/// <summary>
		/// Creates a new room instance, or expands an existing room instance, to contain the given partition.
		/// </summary>
		/// <typeparam name="TRoom"></typeparam>
		/// <param name="partition"></param>
		/// <param name="constructor"></param>
		public void CombineRooms<TRoom>(IPartition partition, Func<TRoom> constructor)
			where TRoom : IRoom
		{
			if (partition == null)
				throw new ArgumentNullException("partition");

			if (constructor == null)
				throw new ArgumentNullException("constructor");

			// Partition doesn't actually join any rooms
			if (partition.RoomsCount <= 1)
				return;

			IRoom[] rooms = GetAdjacentCombineRooms(partition).ToArray();

			// Get the complete set of partitions through the new combine space
			IEnumerable<IPartition> partitions = rooms.SelectMany(r => r.Partitions.GetInstancesRecursive())
													  .Append(partition)
													  .Distinct();

			DestroyCombineRooms(rooms);
			CreateCombineRoom(partitions, constructor);
		}

		/// <summary>
		/// Removes the partitions from existing rooms.
		/// </summary>
		/// <param name="partitions"></param>
		/// <param name="constructor"></param>
		public void UncombineRooms<TRoom>(IEnumerable<IPartition> partitions, Func<TRoom> constructor)
			where TRoom : IRoom
		{
			if (partitions == null)
				throw new ArgumentNullException("partitions");

			if (constructor == null)
				throw new ArgumentNullException("constructor");

			foreach (IPartition partition in partitions)
				UncombineRooms(partition, constructor);
		}

		/// <summary>
		/// Removes the partition from existing rooms.
		/// </summary>
		/// <param name="partition"></param>
		/// <param name="constructor"></param>
		public void UncombineRooms<TRoom>(IPartition partition, Func<TRoom> constructor)
			where TRoom : IRoom
		{
			if (partition == null)
				throw new ArgumentNullException("partition");

			if (constructor == null)
				throw new ArgumentNullException("constructor");

			// Partition doesn't actually join any rooms
			if (partition.RoomsCount <= 1)
				return;

			// Take rooms as an array, since we will potentially modify the core rooms collection
			IRoom[] rooms = GetRooms().ToArray();

			foreach (IRoom room in rooms)
				UncombineRoom(room, partition, constructor);
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Removes the rooms from the core and disposes them.
		/// </summary>
		/// <param name="rooms"></param>
		private void DestroyCombineRooms(IEnumerable<IRoom> rooms)
		{
			if (rooms == null)
				throw new ArgumentNullException("rooms");

			foreach (IRoom room in rooms)
				DestroyCombineRoom(room);
		}

		/// <summary>
		/// Removes the room from the core and disposes it.
		/// </summary>
		/// <param name="room"></param>
		private void DestroyCombineRoom(IRoom room)
		{
			if (room == null)
				throw new ArgumentNullException("room");

			Logger.AddEntry(eSeverity.Informational, "{0} destroying combined room {1}", this, room);

			Core.Originators.RemoveChild(room);
			IDisposable disposable = room as IDisposable;
			if (disposable != null)
				disposable.Dispose();
		}

		/// <summary>
		/// Creates a new room of the given type containing the given partitions.
		/// </summary>
		/// <typeparam name="TRoom"></typeparam>
		/// <param name="partitions"></param>
		/// <param name="constructor"></param>
		private void CreateCombineRoom<TRoom>(IEnumerable<IPartition> partitions, Func<TRoom> constructor)
			where TRoom : IRoom
		{
			if (partitions == null)
				throw new ArgumentNullException("partitions");

			if (constructor == null)
				throw new ArgumentNullException("constructor");

			TRoom room = constructor();
			room.Partitions.AddRange(partitions.Select(p => p.Id));

			Core.Originators.AddChildAssignId(room);

			Logger.AddEntry(eSeverity.Informational, "{0} created new combine room {1}", this, room);
		}

		/// <summary>
		/// Destroys the room, creating new combine rooms where necessary (e.g. splitting 4 rooms in half)
		/// </summary>
		/// <typeparam name="TRoom"></typeparam>
		/// <param name="room"></param>
		/// <param name="partition"></param>
		/// <param name="constructor"></param>
		private void UncombineRoom<TRoom>(IRoom room, IPartition partition, Func<TRoom> constructor)
			where TRoom : IRoom
		{
			if (room == null)
				throw new ArgumentNullException("room");

			if (partition == null)
				throw new ArgumentNullException("partition");

			if (constructor == null)
				throw new ArgumentNullException("constructor");

			if (!room.Partitions.ContainsRecursive(partition.Id))
				return;

			IEnumerable<IPartition[]> split = SplitPartitions(room, partition);

			// Destroy the combine room
			DestroyCombineRoom(room);

			// Build the new combine rooms
			foreach (IPartition[] group in split)
				CombineRooms(group, constructor);
		}

		/// <summary>
		/// Returns groups of partitions that result from splitting on the given partition.
		/// E.g. AB-BC-CD being split on BC would result in [AB], [CD].
		/// </summary>
		/// <param name="room"></param>
		/// <param name="partition"></param>
		/// <returns></returns>
		private IEnumerable<IPartition[]> SplitPartitions(IRoom room, IPartition partition)
		{
			if (room == null)
				throw new ArgumentNullException("room");

			if (partition == null)
				throw new ArgumentNullException("partition");

			IPartition[] partitions = room.Partitions
			                              .GetInstancesRecursive()
			                              .Except(partition)
										  .Distinct()
			                              .ToArray();

			return m_Partitions.SplitAdjacentPartitionsByPartition(partitions, partition);
		}

		/// <summary>
		/// Loops through the rooms to find combine rooms that are adjacent to the given partition.
		/// </summary>
		/// <param name="partition"></param>
		/// <returns></returns>
		private static IEnumerable<IRoom> GetAdjacentCombineRooms(IPartition partition)
		{
			if (partition == null)
				throw new ArgumentNullException("partition");

			return GetRooms().Where(room => room.IsCombineRoom() && IsCombineRoomAdjacent(room, partition));
		}

		/// <summary>
		/// Returns true if the given combined space is adjacent to the given partition.
		/// Returns false if the given combined space contains the given partition.
		/// </summary>
		/// <param name="room"></param>
		/// <param name="partition"></param>
		/// <returns></returns>
		private static bool IsCombineRoomAdjacent(IRoom room, IPartition partition)
		{
			if (room == null)
				throw new ArgumentNullException("room");

			if (partition == null)
				throw new ArgumentNullException("partition");

			// Only interested in the edge of the combined space
			if (room.Partitions.ContainsRecursive(partition.Id))
				return false;

			// Returns true if any of the rooms in the combined space overlap with the partition rooms.
			return room.Partitions
			           .GetInstancesRecursive()
			           .Any(p => partition.GetRooms()
			                              .Any(p.ContainsRoom));
		}

		/// <summary>
		/// Gets the rooms available to the core.
		/// </summary>
		/// <returns></returns>
		private static IEnumerable<IRoom> GetRooms()
		{
			return Core.Originators.GetChildren<IRoom>();
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
			PartitionControlOpenStateCallback handler = OnPartitionOpenStateChange;
			if (handler != null)
				handler(sender as IPartitionDeviceControl, args.Data);
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
