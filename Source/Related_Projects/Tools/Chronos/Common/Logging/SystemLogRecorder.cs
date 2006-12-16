using System;
using System.Collections;
using System.Diagnostics;

namespace Chronos.Diagnostics
{
	/// <summary>
	/// Provides recording of listener output to the system's Event Log.
	/// </summary>
	public class SystemLogRecorder
	{
		public const string DEFAULT_LOG = "Application";
		public const string DEFAULT_SOURCE = "Application";
		public const string DEFAULT_MACHINE = ".";

		private static SystemLogRecorder instance;

		#region Instance Members

		private EventLog eventLog;
		private Hashtable listeners;

		private SystemLogRecorder()
		{
			listeners = new Hashtable();
		}

		private static SystemLogRecorder Instance 
		{
			get 
			{
				if(instance == null) 
				{
					instance = new SystemLogRecorder();
				}
				return instance;
			}
		}

		#endregion

		/// <summary>
		/// Starts recording all Logs catagories to the default event source.
		/// </summary>
		public static void StartRecording()
		{
			StartRecording(DEFAULT_SOURCE);
		}

		/// <summary>
		/// Starts recording all Logs catagories to a user-defined event source.
		/// </summary>
		/// <param name="eventSource"></param>
		public static void StartRecording(string eventSource)
		{
			if(!EventLog.SourceExists(eventSource))
				eventSource = DEFAULT_SOURCE;

			Instance.eventLog = new EventLog(DEFAULT_LOG, DEFAULT_MACHINE, eventSource);

			Instance.StartRecording(Logs.Error, EventLogEntryType.Error);
			Instance.StartRecording(Logs.Trace, EventLogEntryType.Warning);
			Instance.StartRecording(Logs.General, EventLogEntryType.Information);
		}

		public static void StopRecording()
		{
			Instance.StopRecording(Logs.Error);
			Instance.StopRecording(Logs.Trace);
			Instance.StopRecording(Logs.General);
		}

		private void StartRecording(Logs catagory, EventLogEntryType entryType)
		{
			if(Log.Exists(catagory) && !listeners.ContainsKey(catagory)) 
			{
				SystemMulticastLogListener item = new SystemMulticastLogListener(entryType);
				item.TextChanged += new TextChangedEventHandler(listener_TextChanged);
				Log.Listeners(catagory).Add(item);
				listeners.Add(catagory, item);
			}
		}

		private void StopRecording(Logs catagory)
		{
			if(listeners.ContainsKey(catagory)) 
			{
				SystemMulticastLogListener item = (SystemMulticastLogListener) listeners[catagory];
				Log.Listeners(catagory).Remove(item);
				listeners.Remove(catagory);
			}
		}

		private void listener_TextChanged(object sender, Logs catagory)
		{
			const string format = "\n\nCatagory: {0}\nMessage: {1}";
			SystemMulticastLogListener item = (SystemMulticastLogListener) sender;
			string message = string.Format(format, catagory, item.Current);
			eventLog.WriteEntry(message, item.EntryType);
		}

		#region SystemMulticastLogListener
    
		private class SystemMulticastLogListener : MulticastLogListener
		{
			public readonly EventLogEntryType EntryType;

			public SystemMulticastLogListener(EventLogEntryType entryType)
			{
				this.EntryType = entryType;
			}
		}
    
		#endregion
	}
}
