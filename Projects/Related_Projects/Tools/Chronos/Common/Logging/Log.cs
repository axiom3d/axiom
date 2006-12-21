using System;
using System.Text;
using System.Collections;

namespace Chronos.Diagnostics
{
	public enum Logs
	{
		Axiom,
		Empty,
		General,
		Trace,
		Notice,
		Warning,
		Error,
		Status
	}

	/// <summary>
	/// Provides global logging infrastructure.  Use LogListeners to catch messages
	/// written to the log.  The SystemLogRecorder catches messages written to
	/// the Logs catagories, and writes them to your system's EventLog. Define
	/// your own catagories and add LogListeners to those catagories for the ultimate
	/// in logging flexibility!
	/// </summary>
	public sealed class Log
	{
		#region Instance Members
  
		private static Log instance;
		private Hashtable listenerChains;
  
		private Log()
		{
			listenerChains = new Hashtable();

			// Configure the known catagories.
			foreach(int i in Enum.GetValues(typeof(Logs)))
				listenerChains.Add((Logs) i, new LogListenerCollection());

			listenerChains.Remove(Logs.Empty);
		}
  
		public static Log Instance 
		{
			get 
			{
				if(instance == null) 
				{
					instance = new Log();
				}
				return instance;
			}
		}
  
		#endregion
  
		private static LogListenerCollection GetListeners(Logs catagory, bool create)
		{
			if(!Exists(catagory)) 
			{
				if(!create) 
				{
					string message = "The catagory, {0}, does not exist.";
					throw new Exception(string.Format(message, catagory));
				} 
				else 
				{
					Instance.listenerChains.Add(catagory, new LogListenerCollection());
				}
			}
			return (Instance.listenerChains[catagory] as LogListenerCollection);
		}

		/// <summary>
		/// Creates a new catagory.
		/// </summary>
		/// <param name="catagory"></param>
		public static void CreateCatagory(Logs catagory)
		{
			GetListeners(catagory, true);
		}
  
		public static LogListenerCollection Listeners(Logs catagory)
		{
			return GetListeners(catagory, false);
		}
  
		public static bool Exists(Logs catagory)
		{
			return Instance.listenerChains.Contains(catagory);
		}

		public static ICollection Catagories 
		{
			get { return Instance.listenerChains.Keys; }
		}
  
		public static void Write(string message, params object[] arg)
		{
			LogListenerCollection list = GetListeners(Logs.General, false);
			foreach(ILogListener item in list) 
			{
				item.Write(Logs.General, message, arg);
			}
		}
  
		public static void Write(Logs catagory, string message, params object[] arg)
		{
			LogListenerCollection list = GetListeners(catagory, false);
			foreach(ILogListener item in list) 
			{
				item.Write(catagory, message, arg);
			}
		}
  
		public static void WriteLine(string message, params object[] arg)
		{
			LogListenerCollection list = GetListeners(Logs.General, false);
			foreach(ILogListener item in list) 
			{
				item.WriteLine(Logs.General, message, arg);
			}
		}
  
		public static void WriteLine(Logs catagory, string message, params object[] arg)
		{
			LogListenerCollection list = GetListeners(catagory, false);
			foreach(ILogListener item in list) 
			{
				item.WriteLine(catagory, message, arg);
			}
		}

		public static void WriteWarning(string message, params object[] arg)
		{
			WriteLine(Logs.Trace, message, arg);
		}

		public static void WriteError(string message, params object[] arg)
		{
			WriteLine(Logs.Error, message, arg);
		}

		public static void WriteError(Exception e, string message, params object[] arg)
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendFormat("Exception caught: {0} from {1}", e.GetType().Name, e.TargetSite);
			sb.AppendFormat("\nStack Trace: \n{0}", e.StackTrace);
			sb.AppendFormat("\nReason: {0}", e.Message);
			while((e = e.InnerException) != null)
				sb.AppendFormat("\n {0}", e.Message);

			WriteLine(Logs.Error, sb.ToString());
		}

		public static Logs TryParse(string logsString, Logs defaultValue)
		{
			try 
			{
				return (Logs) Enum.Parse(typeof(Logs), logsString, true);
			} 
			catch 
			{
				return defaultValue;
			}
		}
	}
}