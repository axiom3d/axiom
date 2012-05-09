#region LGPL License

/*
Axiom Graphics Engine Library
Copyright (C) 2003-2006  Axiom Project Team

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

#endregion

#region SVN Version Information

// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

#endregion Namespace Declarations

namespace Axiom.Core
{
	///<summary>
	///  The MeterManager creates and hands out TimingMeter instances. Those instances are looked up by meter "title", a string name for the meter. Meter instances also have a string "category", so you can turn metering on and off by category. All public methods of MeterManager are static, so the user doesn't have to worry about managing the instance of MeterManager. The workflow is that the user program creates several meters by calling the static MakeMeter method, passing the title and category of the meter. That method looks up the meter by title, creating it if it doesn't already exists, and returns the meter. Thereafter, the user invokes the TimingMeter.Enter() and TimingMeter.Exit() methods, each of which causes the MeterManager to add a record to a collection of entries and exits. The record has the identity of the meter; whether it's an entry or exit, and the time in processor ticks, captured using the assembler primitive RDTSC. At any point, the program can call the method MeterManager.Report, which produces a report based on the trace.
	///</summary>
	public class MeterManager
	{
		#region Protected MeterManager members

		internal const int ekEnter = 1;
		internal const int ekExit = 2;
		internal const int ekInfo = 3;

		protected static MeterManager instance = null;
		// Are we collecting now?
		protected bool collecting;
		// An id counter for timers
		protected short timerIdCounter;
		// The time when the meter manager was started
		protected long startTime;
		// The number of microseconds per tick; obviously a fraction
		private readonly float microsecondsPerTick;
		// The list of timing meter events
		internal List<MeterEvent> eventTrace;
		// Look up meters by title&category
		internal Dictionary<string, TimingMeter> metersByName;
		// Look up meters by id
		protected Dictionary<int, TimingMeter> metersById;

		// DEBUG
		private static readonly List<MeterStackEntry> debugMeterStack = new List<MeterStackEntry>();

		public static string MeterLogFilename = "MeterLog.txt";
		public static string MeterEventsFilename = "MeterEvents.txt";

		private static void DebugAddEvent( TimingMeter meter, MeterEvent evt )
		{
			if ( evt.eventKind == ekEnter )
			{
				debugMeterStack.Add( new MeterStackEntry( meter, 0 ) );
			}
			else if ( evt.eventKind == ekExit )
			{
				Debug.Assert( debugMeterStack.Count > 0, "Meter stack is empty during ekExit" );
				MeterStackEntry s = debugMeterStack[ debugMeterStack.Count - 1 ];
				Debug.Assert( s.meter == meter, "Entered " + s.meter.title + "; Exiting " + meter.title );
				debugMeterStack.RemoveAt( debugMeterStack.Count - 1 );
			}
			else if ( evt.eventKind == ekInfo )
			{
				// just ignore these
			}
			else
			{
				Debug.Assert( false );
			}
		}

		protected static long CaptureCurrentTime()
		{
			return Stopwatch.GetTimestamp();
		}

		protected string OptionValue( string name, Dictionary<string, string> options )
		{
			string value;
			if ( options.TryGetValue( name, out value ) )
			{
				return value;
			}
			else
			{
				return "";
			}
		}

		protected bool BoolOption( string name, Dictionary<string, string> options )
		{
			string value = OptionValue( name, options );
			return ( value != "" && value != "false" );
		}

		protected int IntOption( string name, Dictionary<string, string> options )
		{
			string value = OptionValue( name, options );
			return ( value == "" ? 0 : int.Parse( value ) );
		}

		protected static void BarfOnBadChars( string name, string nameDescription )
		{
			if ( name.IndexOf( "\n" ) >= 0 )
			{
				throw new Exception( string.Format( "Carriage returns are not allowed in {0}", nameDescription ) );
			}
			else if ( name.IndexOf( "," ) >= 0 )
			{
				throw new Exception( string.Format( "Commas are not allowed in {0}", nameDescription ) );
			}
		}

		protected MeterManager()
		{
			this.timerIdCounter = 1;
			this.eventTrace = new List<MeterEvent>();
			this.metersByName = new Dictionary<string, TimingMeter>();
			this.metersById = new Dictionary<int, TimingMeter>();
			this.startTime = CaptureCurrentTime();
			this.microsecondsPerTick = 1000000.0f / (float) Stopwatch.Frequency;
			instance = this;
		}

		protected TimingMeter GetMeterById( int id )
		{
			TimingMeter meter;
			this.metersById.TryGetValue( id, out meter );
			Debug.Assert( meter != null, string.Format( "Meter for id {0} is not in the index", id ) );
			return meter;
		}

		protected void SaveToFileInternal( string pathname )
		{
			var f = new FileStream( pathname, FileMode.Create, FileAccess.Write );
			var writer = new StreamWriter( f );
			writer.Write( string.Format( "MeterCount={0}\n", this.metersById.Count ) );
			foreach ( var pair in instance.metersById )
			{
				TimingMeter meter = pair.Value;
				writer.Write( string.Format( "{0},{1},{2}\n", meter.title, meter.category, meter.meterId ) );
			}
		}

		protected string IndentCount( int count )
		{
			if ( count > 20 )
			{
				count = 20;
			}
			string s = "|-|-|-|-|-|-|-|-|-|-|-|-|-|-|-|-|-|-|-|-";
			return s.Substring( 0, 2 * count );
		}

		protected long ToMicroseconds( long ticks )
		{
			return (long) ( ( (float) ticks ) * this.microsecondsPerTick );
		}

		protected void DumpEventLog()
		{
			if ( File.Exists( MeterEventsFilename ) )
			{
				File.Delete( MeterEventsFilename );
			}
			var f = new FileStream( MeterEventsFilename, FileMode.Create, FileAccess.Write );
			var writer = new StreamWriter( f );
			writer.Write( string.Format( "Dumping meter event log on {0} at {1}; units are usecs\r\n", DateTime.Now.ToShortDateString(), DateTime.Now.ToShortTimeString() ) );
			int indent = 0;
			var meterStack = new List<MeterStackEntry>();
			long firstEventTime = 0;
			for ( int i = 0; i < this.eventTrace.Count; i++ )
			{
				short kind = this.eventTrace[ i ].eventKind;
				long t = this.eventTrace[ i ].eventTime;
				if ( i == 0 )
				{
					firstEventTime = t;
				}
				if ( kind == ekInfo )
				{
					writer.WriteLine( string.Format( "{0,12:D} {1}{2} {3}{4}", ToMicroseconds( t - firstEventTime ), IndentCount( indent ), "Info ", " ", this.eventTrace[ i ].info ) );
					continue;
				}
				TimingMeter meter = GetMeterById( this.eventTrace[ i ].meterId );
				if ( kind == ekEnter )
				{
					indent++;
					writer.WriteLine( string.Format( "{0,12:D} {1}{2} {3}.{4}", ToMicroseconds( t - firstEventTime ), IndentCount( indent ), "Enter", meter.category, meter.title ) );
					meterStack.Add( new MeterStackEntry( meter, t ) );
				}
				else if ( kind == ekExit )
				{
					Debug.Assert( meterStack.Count > 0, "Meter stack is empty during ekExit" );
					MeterStackEntry s = meterStack[ meterStack.Count - 1 ];
					Debug.Assert( s.meter == meter, "Entered " + s.meter.title + "; Exiting " + meter.title );
					writer.WriteLine( string.Format( "{0,12:D} {1}{2} {3}.{4}", ToMicroseconds( t - s.eventTime ), IndentCount( indent ), "Exit ", meter.category, meter.title ) );
					indent--;
					meterStack.RemoveAt( meterStack.Count - 1 );
				}
			}
			writer.Close();
		}

		protected static bool dumpEventLog = true;

		protected void GenerateReport( StreamWriter writer, int start, Dictionary<string, string> options )
		{
			// For now, ignore options and just print the event trace
			if ( dumpEventLog )
			{
				DumpEventLog();
			}

			// Zero the stack depth and added time
			foreach ( var pair in instance.metersById )
			{
				TimingMeter meter = pair.Value;
				meter.stackDepth = 0;
				meter.addedTime = 0;
			}
			var meterStack = new List<MeterStackEntry>();
			int indent = 0;
			long firstEventTime = 0;
			for ( int i = 0; i < this.eventTrace.Count; i++ )
			{
				short kind = this.eventTrace[ i ].eventKind;
				long t = this.eventTrace[ i ].eventTime;
				if ( i == 0 )
				{
					firstEventTime = t;
				}
				if ( kind == ekInfo )
				{
					writer.WriteLine( string.Format( "{0,12:D} {1}{2} {3}{4}", ToMicroseconds( t - firstEventTime ), IndentCount( indent ), "Info ", " ", this.eventTrace[ i ].info ) );
					continue;
				}
				TimingMeter meter = GetMeterById( this.eventTrace[ i ].meterId );
				if ( kind == ekEnter )
				{
					if ( meter.accumulate && meter.stackDepth == 0 )
					{
						meter.addedTime = 0;
					}
					if ( i >= start && ( !meter.accumulate || meter.stackDepth == 0 ) )
					{
						// Don't display the enter and exit if the
						// exit is the very next record, and the
						// elapsed usecs is less than DontDisplayUsecs
						if ( this.eventTrace.Count > i + 1 && this.eventTrace[ i + 1 ].meterId == this.eventTrace[ i ].meterId && this.eventTrace[ i + 1 ].eventKind == ekExit && ToMicroseconds( this.eventTrace[ i + 1 ].eventTime - t ) < DontDisplayUsecs )
						{
							i++;
							continue;
						}
						writer.WriteLine( string.Format( "{0,12:D} {1}{2} {3}{4}.{5}", ToMicroseconds( t - firstEventTime ), IndentCount( indent ), "Enter", ( meter.accumulate ? "*" : " " ), meter.category, meter.title ) );
						if ( !meter.accumulate )
						{
							indent++;
						}
					}
					meter.stackDepth++;
					meterStack.Add( new MeterStackEntry( meter, t ) );
				}
				else if ( kind == ekExit )
				{
					Debug.Assert( meterStack.Count > 0, "Meter stack is empty during ekExit" );
					MeterStackEntry s = meterStack[ meterStack.Count - 1 ];
					meter.stackDepth--;
					Debug.Assert( s.meter == meter );
					if ( meter.stackDepth > 0 && meter.accumulate )
					{
						meter.addedTime += t - s.eventTime;
					}
					else if ( i >= start )
					{
						if ( !meter.accumulate )
						{
							indent--;
						}
						writer.WriteLine( string.Format( "{0,12:D} {1}{2} {3}{4}.{5}", ToMicroseconds( meter.accumulate ? meter.addedTime : t - s.eventTime ), IndentCount( indent ), "Exit ", ( meter.accumulate ? "*" : " " ), meter.category, meter.title ) );
					}
					meterStack.RemoveAt( meterStack.Count - 1 );
				}
			}
		}

		#endregion Protected MeterManager members

		#region Public MeterManager methods - - all static

		public static void Init()
		{
			if ( instance == null )
			{
				instance = new MeterManager();
			}
		}

		public static int DontDisplayUsecs = 3;

		public static bool Collecting
		{
			get { return instance.collecting; }
			set { instance.collecting = value; }
		}

		// Enable or disable meters by category
		public static void EnableCategory( string categoryName, bool enable )
		{
			Init();
			foreach ( var pair in instance.metersById )
			{
				TimingMeter meter = pair.Value;
				if ( meter.category == categoryName )
				{
					meter.enabled = enable;
				}
			}
		}

		// Enable or disable only a single category
		public static void EnableOnlyCategory( string categoryName, bool enable )
		{
			Init();
			foreach ( var pair in instance.metersById )
			{
				TimingMeter meter = pair.Value;
				meter.enabled = ( meter.category == categoryName ? enable : !enable );
			}
		}

		// Look up the timing meter by title; if it doesn't exist
		// create one with the title and category
		public static TimingMeter GetMeter( string title, string category )
		{
			string name = title + "&" + category;
			TimingMeter meter;
			Init();
			if ( instance.metersByName.TryGetValue( name, out meter ) )
			{
				return meter;
			}
			else
			{
				BarfOnBadChars( title, "TimingMeter title" );
				BarfOnBadChars( category, "TimingMeter category" );
				short id = instance.timerIdCounter++;
				meter = new TimingMeter( title, category, id );
				instance.metersByName.Add( name, meter );
				instance.metersById.Add( id, meter );
				return meter;
			}
		}

		public static TimingMeter GetMeter( string title, string category, bool accumulate )
		{
			TimingMeter meter = GetMeter( title, category );
			meter.accumulate = true;
			return meter;
		}


		public static int AddEvent( TimingMeter meter, short eventKind, string info )
		{
			long time = CaptureCurrentTime();
			short meterId = ( meter == null ) ? (short) 0 : meter.meterId;
			var meterEvent = new MeterEvent( meterId, eventKind, time, info );
#if DEBUG
			DebugAddEvent( meter, meterEvent );
#endif
			instance.eventTrace.Add( meterEvent );
			return instance.eventTrace.Count;
		}

		public static void ClearEvents()
		{
			Init();
			instance.eventTrace.Clear();
		}

		public static void SaveToFile( string pathname )
		{
			instance.SaveToFileInternal( pathname );
		}

		public static long StartTime()
		{
			Init();
			return instance.startTime;
		}

		public static void AddInfoEvent( string info )
		{
			if ( MeterManager.Collecting )
			{
				AddEvent( null, ekInfo, info );
			}
		}

		public static void Report( string title )
		{
			Report( title, null, 0, "" );
		}

		public static void Report( string title, StreamWriter writer, int start, string optionsString )
		{
			bool opened = false;
			if ( writer == null )
			{
				var f = new FileStream( MeterLogFilename, ( File.Exists( MeterLogFilename ) ? FileMode.Append : FileMode.Create ), FileAccess.Write );
				writer = new StreamWriter( f );
				writer.Write( string.Format( "\r\nStarting meter report on {0} at {1} for {2}; units are usecs.", DateTime.Now.ToShortDateString(), DateTime.Now.ToShortTimeString(), title ) );
				opened = true;
			}
			instance.GenerateReport( writer, start, null );
			if ( opened )
			{
				writer.Close();
			}
		}

		#endregion Public MeterManager methods
	}

	internal struct MeterEvent
	{
		internal short meterId;
		internal short eventKind;
		internal long eventTime;
		internal string info;

		internal MeterEvent( short meterId, short eventKind, long eventTime, string info )
		{
			this.meterId = meterId;
			this.eventKind = eventKind;
			this.eventTime = eventTime;
			this.info = info;
		}
	}

	internal struct MeterStackEntry
	{
		internal TimingMeter meter;
		internal long eventTime;

		internal MeterStackEntry( TimingMeter meter, long eventTime )
		{
			this.meter = meter;
			this.eventTime = eventTime;
		}
	}


	public class TimingMeter
	{
		internal TimingMeter( string title, string category, short meterId )
		{
			this.title = title;
			this.category = category;
			this.meterId = meterId;
			this.enabled = true;
			this.accumulate = false;
		}

		public string title;
		public string category;
		public bool enabled;
		public bool accumulate;
		public long addedTime;
		public long addStart;
		public int stackDepth;
		internal short meterId;

		public void Enter()
		{
			if ( MeterManager.Collecting && this.enabled )
			{
				MeterManager.AddEvent( this, MeterManager.ekEnter, "" );
			}
		}

		public void Exit()
		{
			if ( MeterManager.Collecting && this.enabled )
			{
				MeterManager.AddEvent( this, MeterManager.ekExit, "" );
			}
		}
	}

	public class AutoTimer : IDisposable
	{
		private TimingMeter meter;

		public AutoTimer( TimingMeter meter )
		{
			this.meter = meter;
			meter.Enter();
		}

		public void Dispose()
		{
			this.meter.Exit();
			this.meter = null;
		}
	}
}
