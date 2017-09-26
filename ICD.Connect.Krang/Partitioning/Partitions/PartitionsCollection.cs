﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Devices.Controls;
using ICD.Connect.Partitioning.Partitions;
using ICD.Connect.Partitioning.Rooms;

namespace ICD.Connect.Krang.Partitioning.Partitions
{
	public sealed class PartitionsCollection : IPartitionsCollection
	{
		/// <summary>
		/// Raised when the contained partitions change.
		/// </summary>
		public event EventHandler OnPartitionsChanged;

		private readonly Dictionary<int, IPartition> m_Partitions;
		private readonly Dictionary<DeviceControlInfo, IcdHashSet<IPartition>> m_ControlPartitions;
		private readonly Dictionary<int, IcdHashSet<IPartition>> m_RoomAdjacentPartitions;

		private readonly SafeCriticalSection m_PartitionsSection;

		private readonly PartitionManager m_Manager;

		/// <summary>
		/// Gets the number of partitions.
		/// </summary>
		public int Count { get { return m_PartitionsSection.Execute(() => m_Partitions.Count); } }

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="manager"></param>
		public PartitionsCollection(PartitionManager manager)
		{
			m_Manager = manager;

			m_Partitions = new Dictionary<int, IPartition>();
			m_RoomAdjacentPartitions = new Dictionary<int, IcdHashSet<IPartition>>();
			m_ControlPartitions = new Dictionary<DeviceControlInfo, IcdHashSet<IPartition>>();

			m_PartitionsSection = new SafeCriticalSection();
		}

		#region Methods

		/// <summary>
		/// Clears the partitions.
		/// </summary>
		public void Clear()
		{
			SetPartitions(Enumerable.Empty<IPartition>());
		}

		/// <summary>
		/// Gets all of the partitions.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<IPartition> GetPartitions()
		{
			return m_PartitionsSection.Execute(() => m_Partitions.OrderValuesByKey().ToArray());
		}

		/// <summary>
		/// Clears and sets the partitions.
		/// </summary>
		/// <param name="partitions"></param>
		public void SetPartitions(IEnumerable<IPartition> partitions)
		{
			if (partitions == null)
				throw new ArgumentNullException("partitions");

			m_PartitionsSection.Enter();

			try
			{
				m_Partitions.Clear();
				m_Partitions.AddRange(partitions, p => p.Id);

				UpdateLookups();
			}
			finally
			{
				m_PartitionsSection.Leave();
			}

			OnPartitionsChanged.Raise(this);
		}

		/// <summary>
		/// Gets the partitions related to the given control.
		/// </summary>
		/// <param name="deviceControlInfo"></param>
		public IEnumerable<IPartition> GetPartitions(DeviceControlInfo deviceControlInfo)
		{
			m_PartitionsSection.Enter();

			try
			{
				return m_ControlPartitions.ContainsKey(deviceControlInfo)
					       ? m_ControlPartitions[deviceControlInfo]
					       : Enumerable.Empty<IPartition>();
			}
			finally
			{
				m_PartitionsSection.Leave();
			}
		}

		/// <summary>
		/// Gets the immediately adjacent partitions for the given partition.
		/// </summary>
		/// <param name="partition"></param>
		/// <returns></returns>
		public IEnumerable<IPartition> GetAdjacentPartitions(IPartition partition)
		{
			m_PartitionsSection.Enter();

			try
			{
				return partition.GetRooms()
				                .SelectMany(r => GetRoomAdjacentPartitions(r))
				                .Except(partition)
				                .Distinct();
			}
			finally
			{
				m_PartitionsSection.Leave();
			}
		}

		/// <summary>
		/// Gets the partitions adjacent to the given room.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<IPartition> GetRoomAdjacentPartitions(IRoom room)
		{
			if (room == null)
				throw new ArgumentNullException("room");

			return GetRoomAdjacentPartitions(room.Id);
		}

		/// <summary>
		/// Gets the partitions adjacent to the given room.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<IPartition> GetRoomAdjacentPartitions(int roomId)
		{
			m_PartitionsSection.Enter();

			try
			{
				return m_RoomAdjacentPartitions.ContainsKey(roomId)
					       ? m_RoomAdjacentPartitions[roomId].ToArray()
					       : Enumerable.Empty<IPartition>();
			}
			finally
			{
				m_PartitionsSection.Leave();
			}
		}

		/// <summary>
		/// Given a sequence of partitions and a partition to split on, returns the remaining congruous groups of adjacent partitions.
		/// </summary>
		/// <param name="partitions"></param>
		/// <param name="split"></param>
		/// <returns></returns>
		public IEnumerable<IPartition[]> SplitAdjacentPartitionsByPartition(IEnumerable<IPartition> partitions, IPartition split)
		{
			// Unique partitions except the split
			IPartition[] partitionsArray = partitions.Except(split)
			                                         .Distinct()
			                                         .ToArray();

			// First build a map of how the partitions are adjacent to each other.
			Dictionary<IPartition, IcdHashSet<IPartition>> adjacency = new Dictionary<IPartition, IcdHashSet<IPartition>>();
			
			foreach (IPartition partition in partitionsArray)
			{
				// Workaround for compiler warning
				IPartition localEnclosurePartition = partition;

				IcdHashSet<IPartition> adjacent = partitionsArray.Except(partition)
				                                                 .Where(p => p.IsAdjacent(localEnclosurePartition))
				                                                 .ToHashSet();
				adjacency.Add(partition, adjacent);
			}

			// Loop over the keys and find groups
			Dictionary<IPartition, IcdHashSet<IPartition>> groups = new Dictionary<IPartition, IcdHashSet<IPartition>>();
			RecurseAdjacencyMap(adjacency, (root, node) =>
			                               {
											   if (!groups.ContainsKey(root))
												   groups.Add(root, new IcdHashSet<IPartition>());
				                               groups[root].Add(node);
			                               });

			return groups.Values.Select(v => v.ToArray());
		}

		/// <summary>
		/// Loops through the map calling the callback for each distinct node.
		/// </summary>
		/// <param name="map"></param>
		/// <param name="rootAndNodeCallback"></param>
		private static void RecurseAdjacencyMap(IDictionary<IPartition, IcdHashSet<IPartition>> map,
		                                        Action<IPartition, IPartition> rootAndNodeCallback)
		{
			Queue<IPartition> processing = new Queue<IPartition>();
			IcdHashSet<IPartition> visited = new IcdHashSet<IPartition>();

			foreach (IPartition root in map.Keys)
			{
				processing.Enqueue(root);

				while (processing.Count > 0)
				{
					IPartition node = processing.Dequeue();
					if (visited.Contains(node))
						continue;

					visited.Add(node);

					rootAndNodeCallback(root, node);

					processing.EnqueueRange(map[node]);
				}
			}
		}

		#endregion

		/// <summary>
		/// Builds the partition lookup tables.
		/// </summary>
		private void UpdateLookups()
		{
			m_PartitionsSection.Enter();

			try
			{
				m_RoomAdjacentPartitions.Clear();
				m_ControlPartitions.Clear();

				// Build room adjacency lookup
				foreach (IPartition partition in m_Partitions.Values)
				{
					foreach (int room in partition.GetRooms())
					{
						if (!m_RoomAdjacentPartitions.ContainsKey(room))
							m_RoomAdjacentPartitions.Add(room, new IcdHashSet<IPartition>());
						m_RoomAdjacentPartitions[room].Add(partition);
					}
				}

				// Build control to partition lookup
				foreach (IPartition partition in m_Partitions.Values.Where(p => p.HasPartitionControl()))
				{
					if (!m_ControlPartitions.ContainsKey(partition.PartitionControl))
						m_ControlPartitions.Add(partition.PartitionControl, new IcdHashSet<IPartition>());
					m_ControlPartitions[partition.PartitionControl].Add(partition);
				}
			}
			finally
			{
				m_PartitionsSection.Leave();
			}
		}

		#region IEnumerable Methods

		public IEnumerator<IPartition> GetEnumerator()
		{
			return GetPartitions().GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion
	}
}
