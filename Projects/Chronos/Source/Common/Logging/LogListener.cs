using System;

namespace Chronos.Diagnostics
{
	public interface ILogListener
	{
		string Current { get; }
		void Write(Logs catagory, string format, params object[] arg);
		void WriteLine(Logs catagory, string format, params object[] arg);
	}

	/// <summary>
	/// Provides an abstract base class for the listeners that monitor log catagories.
	/// </summary>
	public abstract class LogListener : ILogListener
	{
		private string current;

		/// <summary>
		/// Gets the most recent text to reach this listener.
		/// </summary>
		public string Current 
		{ 
			get { return current; }
		}

		public void Write(Logs catagory, string format, params object[] arg)
		{
			current = string.Format(format, arg);
			OnWrite(catagory);
		}

		public void WriteLine(Logs catagory, string format, params object[] arg)
		{
			current = string.Format(format, arg);
			OnWriteLine(catagory);
		}

		protected abstract void OnWrite(Logs catagory);
		protected abstract void OnWriteLine(Logs catagory);}
}
