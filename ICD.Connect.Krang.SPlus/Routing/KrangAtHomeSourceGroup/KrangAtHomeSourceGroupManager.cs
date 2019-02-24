﻿using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Krang.SPlus.Rooms;
using ICD.Connect.Krang.SPlus.Routing.Endpoints.Sources;

namespace ICD.Connect.Krang.SPlus.Routing.KrangAtHomeSourceGroup
{
	public enum eSourceRoomStatus
	{
		Unused = 0,
		Assigned = 1,
		InUse = 2
		
	}

	public sealed class KrangAtHomeSourceGroupManager : IDisposable, IConsoleNode
	{

		#region Fields

		private ILoggerService m_CachedLogger;

		private readonly IcdHashSet<IKrangAtHomeSourceGroup> m_SourceGroups;
		private readonly SafeCriticalSection m_SourceGroupsSection;

		private readonly Dictionary<IKrangAtHomeSource, IcdHashSet<IKrangAtHomeSourceGroup>> m_GroupsForSource;
		private readonly SafeCriticalSection m_GroupsForSourceSection;

		private readonly Dictionary<IKrangAtHomeSource, Dictionary<IKrangAtHomeRoom, eSourceRoomStatus>> m_SourcesRoomStatus;
		private readonly Dictionary<IKrangAtHomeRoom, Dictionary<IKrangAtHomeSourceGroup, IKrangAtHomeSource>> m_RoomActiveSourceGroup;
		private readonly SafeCriticalSection m_SourcesRoomSection;


		private readonly KrangAtHomeRouting m_Routing;

		#endregion

		#region Events

		public event EventHandler<SourceInUseUpdatedEventArgs> OnSourceInUseUpdated;

		public event EventHandler<SourceRoomsUsedUpdatedEventArgs> OnSourceRoomsUsedUpdated; 

		#endregion

		#region Constructor

		public KrangAtHomeSourceGroupManager(KrangAtHomeRouting routing)
		{
			m_SourceGroups = new IcdHashSet<IKrangAtHomeSourceGroup>();
			m_SourceGroupsSection = new SafeCriticalSection();
			m_GroupsForSource = new Dictionary<IKrangAtHomeSource, IcdHashSet<IKrangAtHomeSourceGroup>>();
			m_GroupsForSourceSection = new SafeCriticalSection();
			m_SourcesRoomStatus = new Dictionary<IKrangAtHomeSource, Dictionary<IKrangAtHomeRoom, eSourceRoomStatus>>();
			m_RoomActiveSourceGroup = new Dictionary<IKrangAtHomeRoom, Dictionary<IKrangAtHomeSourceGroup, IKrangAtHomeSource>>();
			m_SourcesRoomSection = new SafeCriticalSection();

			m_Routing = routing;

			m_Routing.OnSourceRoomsUsedUpdated += RoutingOnSourceRoomsUsedUpdated;
		}

		public void Dispose()
		{

			m_Routing.OnSourceRoomsUsedUpdated -= RoutingOnSourceRoomsUsedUpdated;


			m_SourceGroupsSection.Execute(() => m_SourceGroups.Clear());
			m_GroupsForSourceSection.Execute(() => m_GroupsForSource.Clear());
			m_SourcesRoomSection.Enter();
			try
			{
				m_SourcesRoomStatus.Clear();
				m_RoomActiveSourceGroup.Clear();
			}
			finally
			{
				m_SourcesRoomSection.Leave();
			}


		}

		#endregion

		#region Methods

		[CanBeNull]
		public IKrangAtHomeSource GetAssignedSourceForRoom(IKrangAtHomeSourceGroup soureGroup, IKrangAtHomeRoom room)
		{
			m_SourcesRoomSection.Enter();

			try
			{
				Dictionary<IKrangAtHomeSourceGroup, IKrangAtHomeSource> roomActiveSources;
				if (!m_RoomActiveSourceGroup.TryGetValue(room, out roomActiveSources))
					return null;
				IKrangAtHomeSource source;
				if (!roomActiveSources.TryGetValue(soureGroup, out source))
					return null;

				return source;
			}
			finally
			{
				m_SourcesRoomSection.Leave();
			}
		}

		/// <summary>
		/// Finds the least used source from the given group
		/// </summary>
		/// <param name="sourceGroup">Search space of sources</param>
		/// <param name="source">Least used source from the group, or null if group is empty</param>
		/// <returns>true if source is unused, false if source is already in use</returns>
		public bool GetAvaliableSource(IKrangAtHomeSourceGroup sourceGroup, out IKrangAtHomeSource source)
		{
			if (sourceGroup == null)
				throw new ArgumentNullException("sources");

			IKrangAtHomeSource leastUsed = null;
			int leastUsedCount = int.MaxValue;

			m_SourcesRoomSection.Enter();

			try
			{
				foreach (IKrangAtHomeSource sourceFromList in sourceGroup.GetSources())
				{
					Dictionary<IKrangAtHomeRoom, eSourceRoomStatus> sourceUse;
					if (!m_SourcesRoomStatus.TryGetValue(sourceFromList, out sourceUse))
					{
						source = sourceFromList;
						return true;
					}

					if (leastUsedCount <= sourceUse.Count)
						continue;

					leastUsedCount = sourceUse.Count;
					leastUsed = sourceFromList;
				}
			}
			finally
			{
				m_SourcesRoomSection.Leave();
			}

			source = leastUsed;
			return false;
		}

		public IEnumerable<IKrangAtHomeRoom> GetRoomsUsedBySource(IKrangAtHomeSource source)
		{
			if (source == null)
				throw new NullReferenceException();

			m_SourcesRoomSection.Enter();

			try
			{
				Dictionary<IKrangAtHomeRoom, eSourceRoomStatus> sourceRooms;
				if (!m_SourcesRoomStatus.TryGetValue(source, out sourceRooms))
					return Enumerable.Empty<IKrangAtHomeRoom>();
				return sourceRooms.Select(kvp => kvp.Key).ToList(sourceRooms.Count);
			}
			finally
			{
				m_SourcesRoomSection.Leave();
			}
		}

		[NotNull]
		public IEnumerable<IKrangAtHomeSourceGroup> GetSourceGroupsForSource(IKrangAtHomeSource source)
		{
			IcdHashSet<IKrangAtHomeSourceGroup> sourceGroups;
			m_GroupsForSourceSection.Enter();
			try
			{
				if (!m_GroupsForSource.TryGetValue(source, out sourceGroups))
					return Enumerable.Empty<IKrangAtHomeSourceGroup>();
			}
			finally
			{
				m_GroupsForSourceSection.Leave();
			}

			return sourceGroups.ToList(sourceGroups.Count);
		}

		/// <summary>
		/// Gets the appropriate source for the room, assigns it, and returns the source
		/// </summary>
		/// <param name="room"></param>
		/// <param name="sourceBase"></param>
		/// <returns></returns>
		[CanBeNull]
		public IKrangAtHomeSource GetAndAssignSourceForRoom(IKrangAtHomeRoom room, IKrangAtHomeSourceBase sourceBase)
		{
			IKrangAtHomeSource source = sourceBase as IKrangAtHomeSource;
			IKrangAtHomeSourceGroup sourceGroup = sourceBase as IKrangAtHomeSourceGroup;

			if (sourceGroup != null)
				return GetAndAssignSourceForRoom(room, sourceGroup);
			if (source != null)
				return GetAndAssignSourceForRoom(room, source);

			return null;
		}

		/// <summary>
		/// Assigns the source to a room and returns it
		/// Raises Events
		/// </summary>
		/// <param name="room"></param>
		/// <param name="source"></param>
		/// <returns></returns>
		public IKrangAtHomeSource GetAndAssignSourceForRoom(IKrangAtHomeRoom room, IKrangAtHomeSource source)
		{
			if (room == null)
				throw new ArgumentNullException("room");
			if (source == null)
				throw new ArgumentNullException("source");

			bool sourceInUseChanged;
			bool sourceRoomsUsedChanged;

			SetRoomSourceStatusUsed(source, room, eSourceRoomStatus.Assigned,false, out sourceInUseChanged, out sourceRoomsUsedChanged);

			if (sourceInUseChanged)
				OnSourceInUseUpdated.Raise(this, new SourceInUseUpdatedEventArgs(source, true));
			if (sourceRoomsUsedChanged)
				OnSourceRoomsUsedUpdated.Raise(this, new SourceRoomsUsedUpdatedEventArgs(source, GetRoomsUsedBySource(source)));

			return source;
		}

		/// <summary>
		/// Gets the best source for a group, assigns it for the room, and returns it.
		/// Raises Events
		/// </summary>
		/// <param name="room"></param>
		/// <param name="sourceGroup"></param>
		/// <returns></returns>
		[CanBeNull]
		public IKrangAtHomeSource GetAndAssignSourceForRoom(IKrangAtHomeRoom room, IKrangAtHomeSourceGroup sourceGroup)
		{
			if (room == null)
				throw new ArgumentNullException("room");
			if (sourceGroup == null)
				throw new ArgumentNullException("sourceGroup");

			IKrangAtHomeSource source;
			bool sourceInUseChanged;
			bool sourceRoomsUsedChanged;

			m_SourcesRoomSection.Enter();
			try
			{

				source = GetAssignedSourceForRoom(sourceGroup, room);

				if (source == null)
					if (!GetAvaliableSource(sourceGroup, out source))
						Log(eSeverity.Notice, "Source Group {0} no unused sources avaliable", sourceGroup);
				
				// Bail out if we don't have a source
				if (source == null)
				{
					Log(eSeverity.Warning, "Source Group {0} no sources found!", sourceGroup);
					return null;
				}

				AssignRoomSouce(room, sourceGroup, source, out sourceInUseChanged, out sourceRoomsUsedChanged);
			}
			finally
			{
				m_SourcesRoomSection.Leave();
			}

			if (sourceInUseChanged)
				OnSourceInUseUpdated.Raise(this, new SourceInUseUpdatedEventArgs(source, true));
			if (sourceRoomsUsedChanged)
				OnSourceRoomsUsedUpdated.Raise(this, new SourceRoomsUsedUpdatedEventArgs(source, GetRoomsUsedBySource(source)));
            
			return source;
		}

		/// <summary>
		/// Clears all assigned/used sources for the specified room
		/// Raises events
		/// </summary>
		/// <param name="room"></param>
		internal void ClearRoomAssignedSources(IKrangAtHomeRoom room)
		{
			IcdHashSet<IKrangAtHomeSource> sourcesToRemove = new IcdHashSet<IKrangAtHomeSource>();
			IcdHashSet<IKrangAtHomeSource> sourcesToUpdate = new IcdHashSet<IKrangAtHomeSource>();

			m_SourcesRoomSection.Enter();
			try
			{
				//Remove from Active Source Group
				if (m_RoomActiveSourceGroup.ContainsKey(room))
					m_RoomActiveSourceGroup.Remove(room);


				//Remove from source status list
				foreach (var kvp in m_SourcesRoomStatus.Where(kvp => kvp.Value.ContainsKey(room)))
				{
					kvp.Value.Remove(room);
					sourcesToUpdate.Add(kvp.Key);
					// If there are no other rooms, add the source to be removed from the parent dictionary
					if (kvp.Value.Count == 0)
						sourcesToRemove.Add(kvp.Key);
				}
			}
			finally
			{
				m_SourcesRoomSection.Leave();
			}

			// Remove all the sources that aren't used any more and raise event
			foreach (IKrangAtHomeSource sourceToRemove in sourcesToRemove)
			{
				m_SourcesRoomStatus.Remove(sourceToRemove);
				OnSourceInUseUpdated.Raise(this, new SourceInUseUpdatedEventArgs(sourceToRemove, false));
			}

			// Raise event for all sources that have changed
			foreach (var sourceToUpdate in sourcesToUpdate)
			{
				OnSourceRoomsUsedUpdated.Raise(this, new SourceRoomsUsedUpdatedEventArgs(sourceToUpdate, GetRoomsUsedBySource(sourceToUpdate)));
			}
		}

		/// <summary>
		/// Clears the specific source from the assigned room
		/// Also removes it from the source group assignment collection if found
		/// Typically called when a room hasn't been using a source for an extended period
		/// Raises events
		/// </summary>
		/// <param name="room"></param>
		/// <param name="source"></param>
		internal void ClearRoomSource(IKrangAtHomeRoom room, IKrangAtHomeSource source)
		{
			if (room == null)
				throw new ArgumentNullException("room");
			if (source == null)
				throw new ArgumentNullException("source");

			bool sourceInUseChanged = false;

			m_SourcesRoomSection.Enter();

			try
			{
				Dictionary<IKrangAtHomeRoom, eSourceRoomStatus> roomsForSource;
				if (!m_SourcesRoomStatus.TryGetValue(source, out roomsForSource))
					return;
				if (!roomsForSource.ContainsKey(room))
					return;

				roomsForSource.Remove(room);
				if (roomsForSource.Count == 0)
				{
					sourceInUseChanged = true;
					m_SourcesRoomStatus.Remove(source);
				}
				
				ClearRoomSourceGroupAssignments(room, source);
			}
			finally
			{
				m_SourcesRoomSection.Leave();
			}

			if(sourceInUseChanged)
				OnSourceInUseUpdated.Raise(this, new SourceInUseUpdatedEventArgs(source, false));
			OnSourceRoomsUsedUpdated.Raise(this, new SourceRoomsUsedUpdatedEventArgs(source, GetRoomsUsedBySource(source)));
		}

		/// <summary>
		/// Removes source from any source groups for the room
		/// NOTE: Doesn't Raise Events
		/// </summary>
		/// <param name="room"></param>
		/// <param name="source"></param>
		private void ClearRoomSourceGroupAssignments(IKrangAtHomeRoom room, IKrangAtHomeSource source)
		{
			m_SourcesRoomSection.Enter();

			try
			{
				// Remove from any SourceGroup Assignments
				Dictionary<IKrangAtHomeSourceGroup, IKrangAtHomeSource> roomSourceGroups;
				if (!m_RoomActiveSourceGroup.TryGetValue(room, out roomSourceGroups))
					return;
				foreach (var sourceGroup in GetSourceGroupsForSource(source))
				{
					IKrangAtHomeSource sourceOfGroup;
					if (!roomSourceGroups.TryGetValue(sourceGroup, out sourceOfGroup))
						continue;
					if (sourceOfGroup != source)
						continue;
					
					//Remove source group mapping
					roomSourceGroups.Remove(sourceGroup);

					//Remove roomSourceGroups collection if empty
					if (roomSourceGroups.Count == 0)
						m_RoomActiveSourceGroup.Remove(room);

				}
			}
			finally
			{
				m_SourcesRoomSection.Leave();
			}
		}

		private void SetRoomsForSource(IKrangAtHomeSource source, IEnumerable<IKrangAtHomeRoom> rooms)
		{

			List<IKrangAtHomeRoom> roomsList = rooms.ToList();

			IEnumerable<IKrangAtHomeRoom> oldRoomsInUse;

			m_SourcesRoomSection.Enter();
			try
			{
				Dictionary<IKrangAtHomeRoom, eSourceRoomStatus> oldRoomsDict;
				if (m_SourcesRoomStatus.TryGetValue(source, out oldRoomsDict))
				{
					oldRoomsInUse = oldRoomsDict.Where(kvp => kvp.Value == eSourceRoomStatus.InUse).Select(kvp => kvp.Key);
				}
				else
				{
					oldRoomsInUse = Enumerable.Empty<IKrangAtHomeRoom>();
				}
			}
			finally
			{
				m_SourcesRoomSection.Leave();
			}

			List<IKrangAtHomeRoom> oldRoomsNoLongerActive = oldRoomsInUse.Except(roomsList).ToList();

			foreach (IKrangAtHomeRoom room in oldRoomsNoLongerActive)
			{
				SetSourceStatusForRoom(room, source, eSourceRoomStatus.Assigned, true);
			}

			foreach (IKrangAtHomeRoom room in roomsList)
			{
				SetSourceStatusForRoom(room, source, eSourceRoomStatus.InUse, false);
			}

		}

		/// <summary>
		/// Assigns the given group/source to the room, in both collections
		/// NOTE: Doesn't Raise Events
		/// </summary>
		/// <param name="room"></param>
		/// <param name="sourceGroup"></param>
		/// <param name="source"></param>
		/// <param name="sourceInUseChanged"></param>
		/// <param name="sourceRoomsUsedChanged"></param>
		private void AssignRoomSouce(IKrangAtHomeRoom room, IKrangAtHomeSourceGroup sourceGroup, IKrangAtHomeSource source, out bool sourceInUseChanged, out bool sourceRoomsUsedChanged)
		{
			if (room == null)
				throw new ArgumentNullException("room");
			if (sourceGroup == null)
				throw new ArgumentNullException("sourceGroup");
			if (source == null)
				throw new ArgumentNullException("source");

			m_SourcesRoomSection.Enter();

			try
			{
				//Set the source as active for the room
				Dictionary<IKrangAtHomeSourceGroup, IKrangAtHomeSource> roomSources;
				if(!m_RoomActiveSourceGroup.TryGetValue(room, out roomSources))
				{
					roomSources = new Dictionary<IKrangAtHomeSourceGroup, IKrangAtHomeSource>();
					m_RoomActiveSourceGroup.Add(room, roomSources);
				}
				roomSources[sourceGroup] = source;

				SetRoomSourceStatusUsed(source, room, eSourceRoomStatus.Assigned,false, out sourceInUseChanged, out sourceRoomsUsedChanged);
			}
			finally
			{
				m_SourcesRoomSection.Leave();
			}
		}

		/// <summary>
		/// Assigns the given group/source to the room, in both collections
		/// Raises events
		/// </summary>
		/// <param name="room"></param>
		/// <param name="sourceGroup"></param>
		/// <param name="source"></param>
		internal void AssignRoomSouce(IKrangAtHomeRoom room, IKrangAtHomeSourceGroup sourceGroup, IKrangAtHomeSource source)
		{
			bool sourceInUsedChanged;
			bool sourceRoomsUsedChanged;

			AssignRoomSouce(room, sourceGroup, source, out sourceInUsedChanged, out sourceRoomsUsedChanged);

			if (sourceInUsedChanged)
				OnSourceInUseUpdated.Raise(this, new SourceInUseUpdatedEventArgs(source, true));
			if (sourceRoomsUsedChanged)
				OnSourceRoomsUsedUpdated.Raise(this, new SourceRoomsUsedUpdatedEventArgs(source, GetRoomsUsedBySource(source)));
		}

		/// <summary>
		/// Assigns the source to a room
		/// Raises Events
		/// </summary>
		/// <param name="room"></param>
		/// <param name="source"></param>
		/// <param name="status"></param>
		/// <param name="overrideInUse">if true, will override in use with assigned</param>
		/// <returns></returns>
		public void SetSourceStatusForRoom(IKrangAtHomeRoom room, IKrangAtHomeSource source, eSourceRoomStatus status, bool overrideInUse)
		{
			if (room == null)
				throw new ArgumentNullException("room");
			if (source == null)
				throw new ArgumentNullException("source");

			bool sourceInUseChanged;
			bool sourceRoomsUsedChanged;

			SetRoomSourceStatusUsed(source, room, status, overrideInUse, out sourceInUseChanged, out sourceRoomsUsedChanged);

			if (sourceInUseChanged)
				OnSourceInUseUpdated.Raise(this, new SourceInUseUpdatedEventArgs(source, true));
			if (sourceRoomsUsedChanged)
				OnSourceRoomsUsedUpdated.Raise(this, new SourceRoomsUsedUpdatedEventArgs(source, GetRoomsUsedBySource(source)));
		}

		/// <summary>
		/// Sets the source status in the m_SourcesRoomStatus collection
		/// If the specified status is less then the current status, it is not changed
		/// (ie if the current status is "InUse" and the specified status is "Assigned",
		/// the status remains "InUse")
		/// NOTE: Doesn't Raise Events!
		/// </summary>
		/// <param name="source"></param>
		/// <param name="room"></param>
		/// <param name="status"></param>
		/// <param name="overrideInUse"></param>
		/// <param name="sourceInUseChanged"></param>
		/// <param name="sourceRoomsUsedChanged"></param>
		private void SetRoomSourceStatusUsed(IKrangAtHomeSource source, IKrangAtHomeRoom room, eSourceRoomStatus status,bool overrideInUse, out bool sourceInUseChanged, out bool sourceRoomsUsedChanged)
		{
			if (room == null)
				throw new ArgumentNullException("room");
			if (source == null)
				throw new ArgumentNullException("source");
			if (status == eSourceRoomStatus.Unused)
				throw new ArgumentException("Unused status cannot be usused for this method", "status");

			sourceInUseChanged = false;
			sourceRoomsUsedChanged = false;

			m_SourcesRoomSection.Enter();

			try
			{
				//Set the source as assigned to the room, if not already in use by the room
				Dictionary<IKrangAtHomeRoom, eSourceRoomStatus> sourceRooms;
				if (!m_SourcesRoomStatus.TryGetValue(source, out sourceRooms))
				{
					sourceInUseChanged = true;
					sourceRoomsUsedChanged = true;
					sourceRooms = new Dictionary<IKrangAtHomeRoom, eSourceRoomStatus>();
					m_SourcesRoomStatus[source] = sourceRooms;
				}
				if (sourceRooms.ContainsKey(room))
				{
					// if different and (override or not in use), update
					if ((sourceRooms[room] != status) && (overrideInUse || sourceRooms[room] != eSourceRoomStatus.InUse))
					{
						sourceRooms[room] = status;
						sourceRoomsUsedChanged = true;
					}
				}
				else
				{
					sourceRooms[room] = status;
					sourceRoomsUsedChanged = true;
				}
			}
			finally
			{
				m_SourcesRoomSection.Leave();
			}
		}

		private void RoutingOnSourceRoomsUsedUpdated(object sender, Routing.SourceRoomsUsedUpdatedEventArgs args)
		{
			IKrangAtHomeSource source = args.Source as IKrangAtHomeSource;
			if (source == null)
				return;

			SetRoomsForSource(source, args.RoomsInUse.OfType<IKrangAtHomeRoom>());
		}

		#endregion

		#region SourceGroups Cache

		public void AddSourceGroup(IKrangAtHomeSourceGroup group)
		{
			m_SourceGroupsSection.Execute(() => m_SourceGroups.Add(group));
			AddSourcesToGroupsForSourceCache(group);
		}

		public void AddSourceGroup(IEnumerable<IKrangAtHomeSourceGroup> groups)
		{
			var groupsList = groups.ToList();
			m_SourceGroupsSection.Execute(() => m_SourceGroups.AddRange(groupsList));
			groupsList.ForEach(AddSourcesToGroupsForSourceCache);
		}

		private void AddSourcesToGroupsForSourceCache(IKrangAtHomeSourceGroup sourceGroup)
		{
			m_GroupsForSourceSection.Enter();

			try
			{
				foreach (var source in sourceGroup.GetSources())
				{
					IcdHashSet<IKrangAtHomeSourceGroup> sourceGroups;
					if (!m_GroupsForSource.TryGetValue(source, out sourceGroups))
					{
						sourceGroups = new IcdHashSet<IKrangAtHomeSourceGroup>();
						m_GroupsForSource.Add(source, sourceGroups);
					}
					sourceGroups.Add(sourceGroup);
				}
			}
			finally
			{
				m_GroupsForSourceSection.Leave();
			}
		}

		#endregion

		#region Log

		private ILoggerService Logger
		{
			get { return m_CachedLogger = m_CachedLogger ?? ServiceProvider.TryGetService<ILoggerService>(); }
		}

		private void Log(eSeverity severity, string message)
		{
			Logger.AddEntry(severity, "{0} - {1}", this, message);
		}

		private void Log(eSeverity severity, string message, params object[] args)
		{
			message = string.Format(message, args);
			Log(severity, message);
		}

		#endregion


		#region Console
		/// <summary>
		/// Gets the name of the node.
		/// </summary>
		public string ConsoleName { get { return "SourceGroupManager"; } }

		/// <summary>
		/// Gets the help information for the node.
		/// </summary>
		public string ConsoleHelp { get { return "Manages source groups for KrangAtHome"; } }

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
			addRow("Source Groups", m_SourceGroupsSection.Execute(() => m_SourceGroups.Count));
			addRow("Sources in Groups", m_GroupsForSourceSection.Execute(() => m_GroupsForSource.Count));
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			yield return new ConsoleCommand("PrintSourceGroups", "Prints all the source groups and their sources", () => PrintSourceGroups());
			yield return
				new ConsoleCommand("PrintGroupsForSource", "Prints the groups for each source", () => PrintGroupsForSource());
			yield return
				new ConsoleCommand("PrintSourceStatus", "Prints all the source assignments for rooms",
				                   () => PrintSourceStatus());
			yield return new ConsoleCommand("PrintRoomSources", "Prints each room's Used/Assigned sources", () => PrintRoomSources());

		}

		private void PrintSourceGroups()
		{
			TableBuilder table = new TableBuilder("SourceGroup","SourceCount");
			m_SourceGroupsSection.Enter();
			try
			{
				foreach (IKrangAtHomeSourceGroup group in m_SourceGroups)
				{
					table.AddRow(group.ToString(), group.Count.ToString());
				}
			}
			finally
			{
				m_SourceGroupsSection.Leave();
			}
			IcdConsole.PrintLine(table.ToString());
		}

		private void PrintGroupsForSource()
		{
			m_GroupsForSourceSection.Enter();
			TableBuilder table = new TableBuilder("Source", "Groups");
			try
			{
				foreach (KeyValuePair<IKrangAtHomeSource, IcdHashSet<IKrangAtHomeSourceGroup>> kvp in m_GroupsForSource)
				{
					table.AddHeader(kvp.Key.ToString(), "");
					foreach (IKrangAtHomeSourceGroup sourceGroup in kvp.Value)
						table.AddRow("", sourceGroup);
				}
			}
			finally
			{
				m_GroupsForSourceSection.Leave();
			}
			IcdConsole.PrintLine(table.ToString());
		}

		private void PrintSourceStatus()
		{
			TableBuilder table = new TableBuilder("Room","Status");
			m_SourcesRoomSection.Enter();
			try
			{
				foreach (KeyValuePair<IKrangAtHomeSource, Dictionary<IKrangAtHomeRoom, eSourceRoomStatus>> sourcePair in m_SourcesRoomStatus)
				{
					table.AddHeader("Source", sourcePair.Key.ToString());
					foreach (KeyValuePair<IKrangAtHomeRoom, eSourceRoomStatus> roomPair in sourcePair.Value)
						table.AddRow(roomPair.Key.ToString(), roomPair.Value.ToString());
				}
			}
			finally
			{
				m_SourcesRoomSection.Leave();
			}
			IcdConsole.PrintLine(table.ToString());
		}

		private void PrintRoomSources()
		{
			TableBuilder table = new TableBuilder("Group","Assigned Source");
			m_SourcesRoomSection.Enter();

			try
			{
				foreach (KeyValuePair<IKrangAtHomeRoom, Dictionary<IKrangAtHomeSourceGroup, IKrangAtHomeSource>> roomPair in m_RoomActiveSourceGroup)
				{
					table.AddHeader("Room", roomPair.Key.ToString());
					foreach (var sourceGroupPair in roomPair.Value)
						table.AddRow(sourceGroupPair.Key.ToString(), sourceGroupPair.Value.ToString());
				}
			}
			finally
			{
				m_SourcesRoomSection.Leave();
			}

			IcdConsole.PrintLine(table.ToString());

		}

		#endregion

	}
}