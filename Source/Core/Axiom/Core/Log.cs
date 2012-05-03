#region LGPL License

/*
Axiom Graphics Engine Library
Copyright © 2003-2011 Axiom Project Team

The overall design, and a majority of the core engine and rendering code
contained within this library is a derivative of the open source Object Oriented
Graphics Engine OGRE, which can be found at http://ogre.sourceforge.net.
Many thanks to the OGRE team for maintaining such a high quality project.

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

#endregion LGPL License

#region SVN Version Information

// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using System.IO;

#if SILVERLIGHT
using System.IO.IsolatedStorage;
#endif

#endregion Namespace Declarations

namespace Axiom.Core
{

	#region LogListenerEventArgs Class

	/// <summary>
	/// 
	/// </summary>
	public class LogListenerEventArgs : EventArgs
	{
		/// <summary>
		/// The message to be logged
		/// </summary>
		public string Message { get; private set; }

		/// <summary>
		/// The message level the log is using
		/// </summary>
		public LogMessageLevel Level { get; private set; }

		/// <summary>
		/// If we are printing to the console or not
		/// </summary>
		public bool MaskDebug { get; private set; }

		/// <summary>
		/// the name of this log (so you can have several listeners for different logs, and identify them)
		/// </summary>
		public string LogName { get; private set; }

		/// <summary>
		/// This is called whenever the log recieves a message and is about to write it out
		/// </summary>
		/// <param name="message">The message to be logged</param>
		/// <param name="lml">The message level the log is using</param>
		/// <param name="maskDebug">If we are printing to the console or not</param>
		/// <param name="logName">the name of this log (so you can have several listeners for different logs, and identify them)</param>
		public LogListenerEventArgs( string message, LogMessageLevel lml, bool maskDebug, string logName )
			: base()
		{
			Message = message;
			Level = lml;
			MaskDebug = maskDebug;
			LogName = logName;
		}
	}

	#endregion LogListenerEventArgs Class

	#region Log Class

	/// <summary>
	///     Log class for writing debug/log data to files.
	/// </summary>
	public sealed class Log : DisposableObject
	{
		#region Fields

#if SILVERLIGHT
		private IsolatedStorageFile file;
#endif

		/// <summary>
		///     File stream used for kepping the log file open.
		/// </summary>
		private readonly FileStream log;

		/// <summary>
		///     Writer used for writing to the log file.
		/// </summary>
		private readonly StreamWriter writer;

		/// <summary>
		///     Level of detail for this log.
		/// </summary>
		private LoggingLevel logLevel;

		/// <summary>
		///     Debug output enabled?
		/// </summary>
		private readonly bool debugOutput;

		/// <summary>
		///     LogMessageLevel + LoggingLevel > LOG_THRESHOLD = message logged.
		/// </summary>
		private const int LogThreshold = 4;

		private readonly string mLogName;

		#endregion Fields

		public event EventHandler<LogListenerEventArgs> MessageLogged;

		#region Constructors

		/// <summary>
		///     Constructor.  Creates a log file that also logs debug output.
		/// </summary>
		/// <param name="fileName">Name of the log file to open.</param>
		public Log( string fileName )
			: this( fileName, true )
		{
		}

		/// <summary>
		///     Constructor.
		/// </summary>
		/// <param name="fileName">Name of the log file to open.</param>
		/// <param name="debugOutput">Write log messages to the debug output?</param>
		public Log( string fileName, bool debugOutput )
			: base()
		{
			mLogName = fileName;
			MessageLogged = null;

			this.debugOutput = debugOutput;
			logLevel = LoggingLevel.Normal;

			if ( fileName != null )
			{
				try
				{
#if !( ANDROID )

					// create the log file, or open
#if SILVERLIGHT
					file = IsolatedStorageFile.GetUserStoreForApplication();
					log = file.OpenFile(fileName, FileMode.Create, FileAccess.Write, FileShare.Read);
#else
					log = File.Open( fileName, FileMode.Create, FileAccess.Write, FileShare.Read );
#endif

					// get a stream writer using the file stream
					writer = new StreamWriter( log );
					writer.AutoFlush = true; //always flush after write
#endif
				}
				catch
				{
				}
			}
		}

		#endregion Constructors

		#region Properties

		/// <summary>
		///     Gets/Sets the level of the detail for this log.
		/// </summary>
		/// <value></value>
		public LoggingLevel LogDetail
		{
			get
			{
				return logLevel;
			}
			set
			{
				logLevel = value;
			}
		}

		#endregion Properties

		#region Methods

		/// <summary>
		///     Write a message to the log.
		/// </summary>
		/// <remarks>
		///     Message is written with a LogMessageLevel of Normal, and debug output is not written.
		/// </remarks>
		/// <param name="message">Message to write, which can include string formatting tokens.</param>
		/// <param name="substitutions">
		///     When message includes string formatting tokens, these are the values to
		///     inject into the formatted string.
		/// </param>
		public void Write( string message, params object[] substitutions )
		{
			Write( LogMessageLevel.Normal, false, message, substitutions );
		}

		/// <summary>
		///     Write a message to the log.
		/// </summary>
		/// <remarks>
		///     Message is written with a LogMessageLevel of Normal, and debug output is not written.
		/// </remarks>
		/// <param name="maskDebug">If true, debug output will not be written.</param>
		/// <param name="message">Message to write, which can include string formatting tokens.</param>
		/// <param name="substitutions">
		///     When message includes string formatting tokens, these are the values to
		///     inject into the formatted string.
		/// </param>
		public void Write( bool maskDebug, string message, params object[] substitutions )
		{
			Write( LogMessageLevel.Normal, maskDebug, message, substitutions );
		}

		/// <summary>
		///     Write a message to the log.
		/// </summary>
		/// <param name="level">Importance of this logged message.</param>
		/// <param name="maskDebug">If true, debug output will not be written.</param>
		/// <param name="message">Message to write, which can include string formatting tokens.</param>
		/// <param name="substitutions">
		///     When message includes string formatting tokens, these are the values to
		///     inject into the formatted string.
		/// </param>
		public void Write( LogMessageLevel level, bool maskDebug, string message, params object[] substitutions )
		{
			if ( IsDisposed )
			{
				return;
			}

			if ( message == null )
			{
				throw new ArgumentNullException( "The log message cannot be null" );
			}
			if ( ( (int)logLevel + (int)level ) > LogThreshold )
			{
				return; //too verbose a message to write
			}

			// construct the log message
			if ( substitutions != null && substitutions.Length > 0 )
			{
				message = string.Format( message, substitutions );
			}

			// write the the debug output if requested
			if ( debugOutput && !maskDebug )
			{
#if MONO
				if(System.Diagnostics.Debugger.IsAttached)
					System.Console.WriteLine( message );
				else
#endif
				System.Diagnostics.Debug.WriteLine( message );
			}

			if ( writer != null && writer.BaseStream != null )
			{
				// prepend the current time to the message
				message = string.Format( "[{0}] {1}", DateTime.Now.ToString( "hh:mm:ss" ), message );

				// write the message and flush the buffer
				lock ( writer )
					writer.WriteLine( message );
				//writer auto-flushes
			}

			FireMessageLogged( level, maskDebug, message );
		}

		private void FireMessageLogged( LogMessageLevel level, bool maskDebug, string message )
		{
			// Now fire the MessageLogged event
			if ( MessageLogged != null )
			{
				var args = new LogListenerEventArgs( message, level, maskDebug, mLogName );
				MessageLogged( this, args );
			}
		}

		#endregion Methods

		#region DisposableObject Members

		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				if ( disposeManagedResources )
				{
					// Dispose managed resources.
					try
					{
						if ( writer != null )
						{
							writer.Close();
						}

						if ( log != null )
						{
							log.Close();
						}

#if SILVERLIGHT
				if (file != null)
					file.Dispose();
#endif
					}
					catch
					{
					}
				}

				// There are no unmanaged resources to release, but
				// if we add them, they need to be released here.
			}

			base.dispose( disposeManagedResources );
		}

		#endregion DisposableObject Members
	}

	#endregion Log Class
}