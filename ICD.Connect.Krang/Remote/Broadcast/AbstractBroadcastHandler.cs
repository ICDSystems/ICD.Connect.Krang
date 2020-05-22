using System;
using System.Collections.Generic;
using ICD.Common.Utils.Services;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Protocol.Network.Broadcast;
using ICD.Connect.Protocol.Network.Broadcast.Broadcasters;
using ICD.Connect.Protocol.Network.Direct;
using ICD.Connect.Settings.Cores;

namespace ICD.Connect.Krang.Remote.Broadcast
{
	public abstract class AbstractBroadcastHandler<TData> : IBroadcastHandler
	{
		protected BroadcastManager BroadcastManager { get { return ServiceProvider.GetService<BroadcastManager>(); } }
		protected DirectMessageManager DirectMessageManager { get { return ServiceProvider.GetService<DirectMessageManager>(); } }

		private IBroadcaster m_Broadcaster;

		/// <summary>
		/// Gets the current broadcaster.
		/// </summary>
		public IBroadcaster Broadcaster { get { return m_Broadcaster; } }

		protected ICore Core { get { return ServiceProvider.GetService<ICore>(); } }

		/// <summary>
		/// Release resources.
		/// </summary>
		public virtual void Dispose()
		{
			SetBroadcaster(null);
		}

		/// <summary>
		/// Sets the broadcaster for this handler.
		/// </summary>
		/// <param name="broadcaster"></param>
		public void SetBroadcaster(IBroadcaster broadcaster)
		{
			if (broadcaster == m_Broadcaster)
				return;

			if (m_Broadcaster != null)
				BroadcastManager.DeregisterBroadcaster<TData>(m_Broadcaster);

			Unsubscribe(m_Broadcaster);
			m_Broadcaster = broadcaster;
			Subscribe(m_Broadcaster);

			if (m_Broadcaster == null)
				return;

			BroadcastManager.RegisterBroadcaster<TData>(m_Broadcaster);
			m_Broadcaster.Broadcast();
		}

		/// <summary>
		/// Subscribe to the broadcaster events.
		/// </summary>
		/// <param name="broadcaster"></param>
		protected virtual void Subscribe(IBroadcaster broadcaster)
		{
			if (broadcaster == null)
				return;

			broadcaster.OnBroadcasting += BroadcasterOnBroadcasting;
			broadcaster.OnBroadcastReceived += BroadcasterOnBroadcastReceived;
		}

		/// <summary>
		/// Unsubscribe from the broadcaster events.
		/// </summary>
		/// <param name="broadcaster"></param>
		protected virtual void Unsubscribe(IBroadcaster broadcaster)
		{
			if (broadcaster == null)
				return;

			broadcaster.OnBroadcasting -= BroadcasterOnBroadcasting;
			broadcaster.OnBroadcastReceived -= BroadcasterOnBroadcastReceived;
		}

		/// <summary>
		/// Called immediately before the broadcaster sends a broadcast.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		protected virtual void BroadcasterOnBroadcasting(object sender, EventArgs args)
		{
		}

		/// <summary>
		/// Called when the broadcaster receives a message.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		protected virtual void BroadcasterOnBroadcastReceived(object sender, BroadcastEventArgs eventArgs)
		{
		}

		#region Console

		/// <summary>
		/// Gets the name of the node.
		/// </summary>
		public abstract string ConsoleName { get; }

		/// <summary>
		/// Gets the help information for the node.
		/// </summary>
		public abstract string ConsoleHelp { get; }

		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <returns></returns>
		public virtual IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			yield break;
		}

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public virtual void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public virtual IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			yield break;
		}

		#endregion
	}
}
