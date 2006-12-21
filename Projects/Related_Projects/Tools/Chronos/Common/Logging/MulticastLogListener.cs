using System;

namespace Chronos.Diagnostics
{
	public delegate void TextChangedEventHandler(object sender, Logs catagory);

	/// <summary>
	/// Provides a base class for the listeners that monitor log catagories,
	/// and raise events when they get written to.
	/// </summary>
	public class MulticastLogListener : ILogListener
	{
		public event TextChangedEventHandler TextChanged;

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
			OnTextChanged(catagory);
		}

		public void WriteLine(Logs catagory, string format, params object[] arg)
		{
			current = string.Format(format, arg);
			OnWriteLine(catagory);
			OnTextChanged(catagory);
		}

		private void OnTextChanged(Logs catagory)
		{
			if(TextChanged != null) 
			{
				TextChanged(this, catagory);
			}
		}

		protected virtual void OnWrite(Logs catagory) {}
		protected virtual void OnWriteLine(Logs catagory) {}
	}
}
