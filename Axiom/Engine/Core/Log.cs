#region LGPL License
/*
Axiom Game Engine Library
Copyright (C) 2003  Axiom Project Team

This library is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 2.1 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public
License along with this library; if not, write to the Free Software
Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
*/
#endregion

using System;
using System.Diagnostics;
using System.IO;

namespace Axiom.Core
{
	/// <summary>
	/// Summary description for Log.
	/// </summary>
	public class Log : TraceListener, IDisposable
	{
		private static System.IO.FileStream log;
		private static System.IO.StreamWriter	writer;

		public Log(String fileName)
		{
			// create the log file, or ope
			log = File.Open(fileName, FileMode.Create);

			// get a stream writer using the file stream
			writer = new StreamWriter(log);
		}

		public override void Write(string message)
		{
			writer.WriteLine(message);
		}

		public override void WriteLine(string message)
		{
			writer.WriteLine(message);
		}
		#region IDisposable Members

		public void Dispose()
		{
			writer.Close();
			log.Close();
		}

		#endregion
	}
}
