using System;
using System.IO;

namespace Chronos.Diagnostics
{
	/// <summary>
	/// Directs log output to a System.IO.TextWriter.
	/// </summary>
	public class TextWriterLogListener : LogListener
	{
		private TextWriter writer;
		private bool closed;
  
		public TextWriterLogListener(TextWriter writer)
		{
			if(writer == null)
				throw new ArgumentNullException("writer");

			this.writer = writer;
			closed = false;
		}

		/// <summary>
		/// Gets or sets the TextWriter which receives log output.
		/// </summary>
		public TextWriter Writer 
		{
			get { return this.writer; }
			set { this.writer = value; }
		}

		/// <summary>
		/// Gets whether the TextWriter has been closed.
		/// </summary>
		public bool IsClosed 
		{
			get { return closed; }
		}

		/// <summary>
		/// Writes the message to this instance's TextWriter.
		/// </summary>
		protected override void OnWrite(Logs catagory)
		{
			if(!closed)
				writer.Write(this.Current);
		}

		/// <summary>
		/// Writes the message to this instance's TextWriter.
		/// </summary>
		protected override void OnWriteLine(Logs catagory)
		{
			if(!closed)
				writer.WriteLine(this.Current);
		}

		/// <summary>
		/// Closes the TextWriter so that it no longer receives log output.
		/// </summary>
		public void Close()
		{
			if(!closed) 
			{
				writer.Close();
				closed = true;
			}
		}

		/// <summary>
		/// Flushes the output buffer for the TextWriter.
		/// </summary>
		public void Flush()
		{
			if(!closed) 
			{
				writer.Flush();
			}
		}
	}
}
