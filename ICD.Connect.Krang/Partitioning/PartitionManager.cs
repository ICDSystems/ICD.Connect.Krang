using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Services;
using ICD.Common.Services.Logging;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Partitions;
using ICD.Connect.Settings;
using ICD.Connect.Settings.Core;

namespace ICD.Connect.Krang.Partitioning
{
	public sealed class PartitionManager : AbstractOriginator<PartitionManagerSettings>, IConsoleNode, IPartitionManager
	{
		private readonly CorePartitionCollection m_Partitions;

		public CorePartitionCollection Partitions { get { return m_Partitions; } }

		IOriginatorCollection<IPartition> IPartitionManager.Partitions { get { return Partitions; } }

		/// <summary>
		/// Gets the name of the node.
		/// </summary>
		public string ConsoleName { get { return "PartitionManager"; } }

		/// <summary>
		/// Gets the help information for the node.
		/// </summary>
		public string ConsoleHelp { get { return "Tracks the opening and closing of partition walls."; } }

		/// <summary>
		/// Constructor.
		/// </summary>
		public PartitionManager()
		{
			m_Partitions = new CorePartitionCollection();

			ServiceProvider.AddService<IPartitionManager>(this);
		}

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
			base.ApplySettingsFinal(settings, factory);

			IEnumerable<IPartition> partitions = GetPartitions(settings, factory);
			Partitions.SetChildren(partitions);
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

			settings.PartitionSettings.SetRange(Partitions.Select(r => r.CopySettings()));
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
			TableBuilder builder = new TableBuilder("Id", "Partition", "Device", "Rooms");

			foreach (IPartition partition in m_Partitions.GetChildren().OrderBy(c => c.Id))
			{
				int id = partition.Id;
				int? device = partition.PartitionDevice;
				string rooms = StringUtils.ArrayFormat(partition.GetRooms().Order());

				builder.AddRow(id, partition, device, rooms);
			}

			return builder.ToString();
		}

		#endregion
	}
}
