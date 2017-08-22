using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Devices.Controls;
using ICD.Connect.Partitioning.Partitions;

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
			m_PartitionsSection = new SafeCriticalSection();

			m_ControlPartitions = new Dictionary<DeviceControlInfo, IcdHashSet<IPartition>>();
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

		#endregion

		/// <summary>
		/// Builds the partition lookup tables.
		/// </summary>
		private void UpdateLookups()
		{
			m_PartitionsSection.Enter();

			try
			{
				m_ControlPartitions.Clear();

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
