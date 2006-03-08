using System;
using System.Collections;

namespace Axiom
{
    /// <summary>
    /// Summary description for LogManager.
    /// </summary>
    public sealed class LogManager : IDisposable
    {
        #region Singleton implementation

        /// <summary>
        ///     Singleton instance of this class.
        /// </summary>
        private static LogManager instance;

        /// <summary>
        ///     Internal constructor.  This class cannot be instantiated externally.
        /// </summary>
        internal LogManager()
        {
            if ( instance == null )
            {
                instance = this;
            }
        }

        /// <summary>
        ///     Gets the singleton instance of this class.
        /// </summary>
        public static LogManager Instance
        {
            get
            {
                return instance;
            }
        }

        #endregion Singleton implementation

        #region Fields

        /// <summary>
        ///     List of logs created by the log manager.
        /// </summary>
        private Hashtable logList = new Hashtable();
        /// <summary>
        ///     The default log to which output is done.
        /// </summary>
        private Log defaultLog;

        #endregion Fields

        #region Properties

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

        #endregion Properties

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
        /// <param name="defaultLog">
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
        /// <param name="defaultLog">
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
            if ( logList[name] == null )
            {
                throw new AxiomException( "Log with the name '{0}' not found.", name );
            }

            return (Log)logList[name];
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

        #endregion Methods

        #region IDisposable Members

        /// <summary>
        ///     Destroys all the logs owned by the log manager.
        /// </summary>
        public void Dispose()
        {
            // dispose of all the logs
            foreach ( IDisposable o in logList.Values )
            {
                o.Dispose();
            }

            logList.Clear();
        }

        #endregion
    }
}