using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace RealmForge
{
    /// <summary>
    /// A Log that writes to a log file and can listen in on Trace and Debug messages
    /// </summary>
    public class Log
    {
        #region Internal Classes
        internal class MessageTimeStamp
        {
            internal MessageTimeStamp( string message )
                : this( message, DateTime.Now )
            {
            }
            internal MessageTimeStamp( string message, DateTime time )
            {
                Message = message;
                Time = time;
            }
            public string Message;
            public DateTime Time;
        }
        #endregion

        #region Fields and Properties
        public static readonly string WarningPrefix = "WARNING: ";
        protected static TextWriterTraceListener logger = null;
        protected static int indentLevel = 0;
        public static int IndentationLevel
        {
            get
            {
                return indentLevel;
            }
            set
            {
                indentLevel = value;
            }
        }
        public static string LogFilePath = "Log.txt";
        public static bool AppendLogs = false;
        /// <summary>
        /// If the log writes verbose statements, otherwise they are filtered out
        /// </summary>
        public static bool Verbose = false;
        public static bool ThrowErrorsOnAssertionsFailed = true;
        protected const bool NEW_LINE_BEFORE_TASK_GROUP = true;
        protected const bool NEW_LINE_AFTER_TASK_GROUP = false;
        /// <summary>
        /// 
        /// </summary>
        /// <remarks>Could also be implemented with a stack, but would be easily corrupted</remarks>
        protected static Stack timedStatements = new Stack();
        #endregion

        #region Constructors
        public static void Init()
        {
            Init( LogFilePath, AppendLogs, true );	//t donauto flush by default as write statements take care of that
        }

        public static void Init( string filePath, bool append, bool buffered )
        {
            StreamWriter w = new StreamWriter( filePath, append );
            w.AutoFlush = !buffered;
            logger = new TextWriterTraceListener( w );
            //catch all writes to Trace and Debug output streams and write them to the log as well
            //Trace.Listeners.Add(logger);
            Debug.Listeners.Add( logger );
        }
        #endregion

        #region Static Methods
        #region Assert
        public static void Assert( bool shouldBeTrue, string assertionFailedMessage )
        {
            if ( !shouldBeTrue )
            {
                if ( ThrowErrorsOnAssertionsFailed )
                    throw new AssertionFailedException( assertionFailedMessage );
                else
                    Write( "INVALID: " + assertionFailedMessage );
            }
        }
        public static void Assert( bool shouldBeTrue, string assertionFailedMessage, params object[] args )
        {
            Assert( shouldBeTrue, string.Format( assertionFailedMessage, args ) );
        }

        [Conditional( "DEBUG" )]
        public static void DebugAssert( bool shouldBeTrue, string assertionFailedMessage )
        {
            Assert( shouldBeTrue, assertionFailedMessage );
        }


        [Conditional( "DEBUG" )]
        public static void DebugAssert( bool shouldBeTrue, string assertionFailedMessage, params object[] args )
        {
            Assert( shouldBeTrue, assertionFailedMessage, args );
        }
        #endregion

        #region Timer
        [Conditional( "DEBUG" )]
        public static void StartDebugTimer( string message )
        {
            StartTimer( message );
        }
        [Conditional( "DEBUG" )]
        public static void StartDebugTimer( string message, params object[] args )
        {
            StartTimer( message, args );
        }
        [Conditional( "DEBUG" )]
        public static void EndDebugTimer()
        {
            EndTimer();
        }


        public static void StartTimer()
        {
            StartTimer( null );
        }

        public static void StartTimer( string message )
        {
            MessageTimeStamp stamp = new MessageTimeStamp( message );
            timedStatements.Push( stamp );
        }

        public static void StartTimer( string messageFormat, params object[] args )
        {
            StartTimer( string.Format( messageFormat, args ) );
        }

        public static TimeSpan EndTimer()
        {
            if ( timedStatements.Peek() != null )
            {
                MessageTimeStamp stamp = (MessageTimeStamp)timedStatements.Pop();
                TimeSpan dif = DateTime.Now - stamp.Time;

                if ( stamp.Message == null )
                {
                    //if null, dont print
                }
                else if ( stamp.Message == string.Empty )
                {//print time
                    Write( string.Format( "Finished, took {0}ms", (int)dif.TotalMilliseconds ) );
                }
                else //print time and message
                    Write( string.Format( "Finished {0}, took {1}ms", stamp.Message, (int)dif.TotalMilliseconds ) );
                return dif;
            }
            return TimeSpan.Zero;

        }
        #endregion

        #region Task Groups
        [Conditional( "DEBUG" )]
        public static void StartDebugTaskGroup( string message, params object[] args )
        {
            StartTaskGroup( message, args );
        }
        [Conditional( "DEBUG" )]
        public static void EndDebugTaskGroup()
        {
            EndTaskGroup();
        }

        public static void WriteDividerBar()
        {
            Write( "****************************************************" );
        }

        public static void StartTaskGroup( string message, params object[] args )
        {
            if ( NEW_LINE_BEFORE_TASK_GROUP )
                Trace.Write( "\n" );
            if ( args.Length > 0 )
                message = string.Format( message, args );//only format once
            Log.Write( message );
            StartTimer( message );//will print when EndTimer is called by EndTaskGroup
            IncreaseIndentationLevel();
        }
        public static void EndTaskGroup()
        {
            DecreaseIndentationLevel();
            EndTimer();
            if ( NEW_LINE_AFTER_TASK_GROUP )
                Trace.Write( "\n" );//line afterwards, because took {time} is printed after message
        }
        #endregion

        #region Indentation

        public static void IncreaseIndentationLevel()
        {
            indentLevel++;
        }
        public static void DecreaseIndentationLevel()
        {
            if ( indentLevel <= 0 )
                Errors.Argument( "Cannot decrease indentation level below 0, existing level is {0} and the decrease is 1", indentLevel );
            indentLevel--;
        }


        public static void IncreaseIndentationLevel( int levelsToIncrease )
        {
            indentLevel += levelsToIncrease;
        }
        public static void DecreaseIndentationLevel( int levelsToDecrease )
        {
            if ( levelsToDecrease < 0 )
                Errors.Argument( "Decrease indent level change should be a positive value to indicate the number of levels to decrease" );
            if ( indentLevel - levelsToDecrease < 0 )
                Errors.Argument( "Cannot decrease indentation level below 0, existing level is {0} and the decrease is {1}", indentLevel, levelsToDecrease );
            indentLevel -= levelsToDecrease;
        }
        #endregion

        #region Write
        public static void SkipLine()
        {
            Trace.Write( "\n" );
        }
        public static void Write( ICollection lines )
        {
            foreach ( object line in lines )
            {
                Log.Write( line.ToString() );
            }
        }
        public static void Write( object obj )
        {
            if ( obj != null )
            {
                Write( obj.ToString() );
            }
        }
        public static void Write( Stream stream )
        {
            if ( stream.CanSeek )
                stream.Position = 0;
            StreamReader reader = new StreamReader( stream );
            string line = null;
            while ( ( line = reader.ReadLine() ) != null )
            {
                Trace.WriteLine( line );//indent level doesnt apply
            }
            if ( stream.CanSeek )
                stream.Position = 0;
            Trace.Flush();
        }

        public static void Write( string message )
        {
            WriteSameLine( message + Environment.NewLine );
        }

        public static void WriteSameLine( string message )
        {
            if ( indentLevel < 0 )
                Errors.InvalidState( "Indentation level is {0} but should not be less then 0", indentLevel );
            else if ( indentLevel == 0 )
                Trace.Write( message );
            else if ( indentLevel == 1 )
                Trace.Write( '\t' + message );
            else
            {
                string indentation = new StringBuilder( indentLevel ).Append( '\t', indentLevel ).ToString();
                Trace.Write( indentation + message );
            }
            Trace.Flush();
        }

        public static void Write( string messageFormat, params object[] args )
        {
            Write( string.Format( messageFormat, args ) );
        }

        public static void Write( string messageFormat, object arg1, object arg2, object arg3 )
        {
            Write( string.Format( messageFormat, arg1, arg2, arg3 ) );
        }

        public static void Write( string messageFormat, object arg1, object arg2 )
        {
            Write( string.Format( messageFormat, arg1, arg2 ) );
        }

        public static void Write( string messageFormat, object arg1 )
        {
            Write( string.Format( messageFormat, arg1 ) );
        }
        #endregion

        #region Error Write


        public static void Write( string message, Exception e )
        {
            Write( message, e, true );
        }

        public static void Write( string messageFormat, Exception e, params object[] args )
        {
            Write( messageFormat, e, true, args );
        }

        public static void Write( string messageFormat, Exception e, bool showStackTrace )
        {
            Write( messageFormat );
            Write( e, showStackTrace );
        }

        public static void Write( string messageFormat, Exception e, bool showStackTrace, params object[] args )
        {
            Write( messageFormat, args );
            Write( e, showStackTrace );
        }
        public static void Write( Exception e )
        {
            Write( e, true );
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        /// <param name="showStackTrace"></param>
        /// <example>
        /// ERROR: System.ArugumentException
        /// Must be a good argument
        /// at .... at..... at.....
        /// ERROR: System.NullReferenceException
        /// Parameter can not be null
        /// at .... at..... at.....
        /// 
        /// More Log...
        /// </example>
        public static void Write( Exception e, bool showStackTrace )
        {
            Write( "ERROR: " + e.GetType().Name + '\n' + e.Message );
            if ( showStackTrace )
            {
                Write( '\n' + e.StackTrace + '\n' );
            }
            if ( e.InnerException != null )
                Write( e.InnerException, showStackTrace );//NOTE: May want to indent inner exceptions
            else
                Trace.Write( "\n" );
        }
        #endregion

        #region Warn
        [Conditional( "DEBUG" )]
        public static void DebugWarn( string message )
        {
            Warn( message );
        }

        [Conditional( "DEBUG" )]
        public static void DebugWarn( string messageFormat, params object[] args )
        {
            Warn( messageFormat, args );
        }
        public static void Warn( string message )
        {
            Write( WarningPrefix + message );
        }
        public static void Warn( string messageFormat, params object[] args )
        {
            Write( WarningPrefix + messageFormat, args );
        }
        #endregion

        #region Debug Write

        [Conditional( "DEBUG" )]
        public static void WriteVerbose( string messageFormat, params object[] args )
        {
            if ( Verbose )
                Write( messageFormat, args );
        }

        [Conditional( "DEBUG" )]
        public static void WriteVerbose( string messageFormat )
        {
            if ( Verbose )
                Write( messageFormat );
        }

        [Conditional( "DEBUG" )]
        public static void DebugSkipLine()
        {
            SkipLine();
        }

        [Conditional( "DEBUG" )]
        public static void DebugWrite( string messageFormat, params object[] args )
        {
            Write( messageFormat, args );
        }

        [Conditional( "DEBUG" )]
        public static void DebugWrite( string messageFormat, object arg1, object arg2, object arg3 )
        {
            Write( messageFormat, arg1, arg2, arg3 );
        }

        [Conditional( "DEBUG" )]
        public static void DebugWrite( string messageFormat, object arg1, object arg2 )
        {
            Write( messageFormat, arg1, arg2 );
        }

        [Conditional( "DEBUG" )]
        public static void DebugWrite( string messageFormat, object arg1 )
        {
            Write( messageFormat, arg1 );
        }
        [Conditional( "DEBUG" )]
        public static void DebugWrite( string message )
        {
            Write( message );
        }

        [Conditional( "DEBUG" )]
        public static void DebugWriteSameLine( string message )
        {
            WriteSameLine( message );
        }
        #endregion
        #endregion

    }

}
