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
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Axiom.Collections;

#endregion Namespace Declarations

namespace Axiom.Core
{
	/// <summary>
	/// Summary description for LogManager.
	/// </summary>
	public sealed class LogManager : Singleton<LogManager>
	{
		#region Fields and Properties

		/// <summary>
		///     List of logs created by the log manager.
		/// </summary>
		private AxiomCollection<Log> logList = new AxiomCollection<Log>();
		/// <summary>
		///     The default log to which output is done.
		/// </summary>
		private Log defaultLog;

		/// <summary>
		///     Gets/Sets the default log to use for writing.
		/// </summary>
		/// <value></value>
		public Log DefaultLog
		{
			get
			{
				if ( defaultLog == null )
				{
					throw new AxiomException( "No logs have been created yet." );
				}

				return defaultLog;
			}
			set
			{
				defaultLog = value;
			}
		}

		/// <summary>
		///     Sets the level of detail of the default log.
		/// </summary>
		public LoggingLevel LogDetail
		{
			get
			{
				return DefaultLog.LogDetail;
			}
			set
			{
				DefaultLog.LogDetail = value;
			}
		}

		#endregion Fields and Properties

		#region Methods

		/// <summary>
		///     Creates a new log with the given name.
		/// </summary>
		/// <param name="name">Name to give to the log, i.e. "Axiom.log"</param>
		/// <returns>A newly created Log object, opened and ready to go.</returns>
		public Log CreateLog( string name )
		{
			return CreateLog( name, false, true );
		}

		/// <summary>
		///     Creates a new log with the given name.
		/// </summary>
		/// <param name="name">Name to give to the log, i.e. "Axiom.log"</param>
        /// <param name="isDefaultLog">
		///     If true, this is the default log output will be
		///     sent to if the generic logging methods on this class are
		///     used. The first log created is always the default log unless
		///     this parameter is set.
		/// </param>
		/// <returns>A newly created Log object, opened and ready to go.</returns>
		public Log CreateLog( string name, bool isDefaultLog )
		{
			return CreateLog( name, isDefaultLog, true );
		}

		/// <summary>
		///     Creates a new log with the given name.
		/// </summary>
		/// <param name="name">Name to give to the log, i.e. "Axiom.log"</param>
        /// <param name="isDefaultLog">
		///     If true, this is the default log output will be
		///     sent to if the generic logging methods on this class are
		///     used. The first log created is always the default log unless
		///     this parameter is set.
		/// </param>
		/// <param name="debuggerOutput">
		///     If true, output to this log will also be routed to <see cref="System.Diagnostics.Debug"/>
		///     Not only will this show the messages into the debugger, but also allows you to hook into
		///     it using a custom TraceListener to receive message notification wherever you want.
		/// </param>
		/// <returns>A newly created Log object, opened and ready to go.</returns>
		public Log CreateLog( string name, bool isDefaultLog, bool debuggerOutput )
		{
			Log newLog = new Log( name, debuggerOutput );

			// set as the default log if need be
			if ( defaultLog == null || isDefaultLog )
			{
				defaultLog = newLog;
			}

			if ( name == null )
				name = string.Empty;
			logList.Add( name, newLog );

			return newLog;
		}

		/// <summary>
		///     Retrieves a log managed by this class.
		/// </summary>
		/// <param name="name">Name of the log to retrieve.</param>
		/// <returns>Log with the specified name.</returns>
		public Log GetLog( string name )
		{
			if ( logList[ name ] == null )
			{
				throw new AxiomException( "Log with the name '{0}' not found.", name );
			}

			return (Log)logList[ name ];
		}

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
			DefaultLog.Write( level, maskDebug, message, substitutions );
		}

		public static string BuildExceptionString( Exception exception )
		{
			StringBuilder errMessage = new StringBuilder();

			errMessage.Append( exception.Message + Environment.NewLine + exception.StackTrace );

			while ( exception.InnerException != null )
			{
				errMessage.Append( BuildInnerExceptionString( exception.InnerException ) );
				exception = exception.InnerException;
			}

			return errMessage.ToString();
		}

		private static string BuildInnerExceptionString( Exception innerException )
		{
			string errMessage = string.Empty;

			errMessage += "\n" + " InnerException ";
			errMessage += "\n" + innerException.Message + "\n" + innerException.StackTrace;

			return errMessage;
		}

		#endregion Methods

		#region Singleton implementation

		protected override void dispose( bool disposeManagedResources )
		{
			Write( "*-*-* Axiom Shutdown Complete." );

			if ( !isDisposed )
			{
				if ( disposeManagedResources )
				{
					// Dispose managed resources.
					// dispose of all the logs
					foreach ( IDisposable o in logList.Values )
					{
						o.Dispose();
					}

					logList.Clear();

				}

				// There are no unmanaged resources to release, but
				// if we add them, they need to be released here.
			}

			// If it is available, make the call to the
			// base class's Dispose(Boolean) method
			base.dispose( disposeManagedResources );
		}


		#endregion Singleton implementation

	}
}