using System;
using System.IO;
using System.Collections;

namespace Chronos.Diagnostics
{
	/// <summary>
	/// Collects all log output, sorted by catagory.
	/// </summary>
	public sealed class UberMulticastLogListener : MulticastLogListener
	{
		private readonly Hashtable Writers;

		public UberMulticastLogListener()
		{
			this.Writers = new Hashtable();
		}

		public string Text(Logs catagory)
		{
			if(Writers.ContainsKey(catagory)) 
			{
				return (Writers[catagory] as StringWriter).ToString();
			}
			return string.Empty;
		}

		protected override void OnWrite(Logs catagory)
		{
			EnsureWriter(catagory).Write(this.Current);
		}

		protected override void OnWriteLine(Logs catagory)
		{
			EnsureWriter(catagory).WriteLine(this.Current);
		}

		private StringWriter EnsureWriter(Logs catagory)
		{
			if(!Writers.ContainsKey(catagory)) 
			{
				Writers.Add(catagory, new StringWriter());
			}
			return (StringWriter) Writers[catagory];
		}
	}
}
