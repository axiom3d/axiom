#region BSD License
/*
 BSD License
Copyright (c) 2002, The CsGL Development Team
http://csgl.sourceforge.net/authors.html
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions
are met:

1. Redistributions of source code must retain the above copyright notice,
   this list of conditions and the following disclaimer.

2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

3. Neither the name of The CsGL Development Team nor the names of its
   contributors may be used to endorse or promote products derived from this
   software without specific prior written permission.

   THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS 
   "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
   LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS
   FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE
   COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT,
   INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING,
   BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
   LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
   CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT
   LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN
   ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
   POSSIBILITY OF SUCH DAMAGE.
 */
#endregion BSD License

using System;
using System.Runtime.InteropServices;

namespace Axiom.Utility 
{
	/// <summary>
	/// Encapsulates the functionality of the platform's highest resolution timer available.
	/// </summary>
	/// <remarks>
	/// On Windows this will be a QueryPerformanceCounter (if available), otherwise tt will be TimeGetTime().  
	/// On Linux this will be GetTimeOfDay (not currently implemented).
	/// </remarks>
	public sealed class HighResolutionTimer : IDisposable 
	{
		// --- Fields ---
		#region Private Static Fields
		private static TimerType timerType = TimerType.None;							// The Type Of Timer Supported On This System
		private static ulong timerFrequency = 0;										// The Frequency Of The Timer
		#endregion Private Static Fields

		#region Private Instance Fields
		private bool timerIsRunning = false;											// Is This Timer Running?
		private ulong timerStartCount = 0;												// The Timer Start Count
		private ulong timerEndCount = 0;												// The Timer End Count
		private static bool isDisposed = false;											// Has Dispose Been Called?
		#endregion Private Instance Fields

		#region Public Properties
		/// <summary>
		/// Gets a <see cref="System.UInt64" /> representing the 
		/// current tick count of the timer.
		/// </summary>
		public ulong Count {
			get { 
				return GetCurrentCount();
			}
		}

		/// <summary>
		/// Gets a <see cref="System.UInt64" /> representing the 
		/// difference, in ticks, between the <see cref="StartCount"/> 
		/// and <see cref="EndCount" />.
		/// </summary>
		public ulong Difference {
			get {
				return (timerEndCount - timerStartCount);
			}
		}

		/// <summary>
		/// Gets a <see cref="System.Double" /> representing the 
		/// elapsed time, in seconds, between the <see cref="StartCount" /> 
		/// and <see cref="EndCount" />.
		/// </summary>
		public float Elapsed {
			get {
				return (((float) timerEndCount - (float) timerStartCount) / (float) timerFrequency);
			}
		}

		/// <summary>
		/// Gets a <see cref="System.UInt64" /> representing the 
		/// tick count at the end of the timer's run.
		/// </summary>
		public ulong EndCount {
			get {
				return timerEndCount;
			}
		}

		/// <summary>
		/// Gets a <see cref="System.UInt64" /> representing the 
		/// frequency of the counter in ticks-per-second.
		/// </summary>
		public ulong Frequency {
			get {
				return timerFrequency;
			}
		}

		/// <summary>
		/// Gets a <see cref="System.Boolean" /> representing whether the 
		/// timer has been started and is currently running.
		/// </summary>
		public bool IsRunning {
			get {
				return timerIsRunning;
			}
		}

		/// <summary>
		/// Gets a <see cref="System.Double" /> representing the 
		/// resolution of the timer in seconds.
		/// </summary>
		public float Resolution {
			get {
				return ((float) 1.0 / (float) timerFrequency);
			}
		}

		/// <summary>
		/// Gets a <see cref="System.UInt64" /> representing the 
		/// tick count at the start of the timer's run.
		/// </summary>
		public ulong StartCount {
			get {
				return timerStartCount;
			}
		}

		/// <summary>
		/// Gets the <see cref="HighResolutionTimer.TimerType" /> for this
		/// timer, which is based on what is supported by the underlying platform.
		/// </summary>
		public TimerType Type {
			get {
				return timerType;
			}
		}
		#endregion Public Properties

		#region Enums
		/// <summary>
		/// The type of timer supported by this platform.
		/// </summary>
		public enum TimerType {
			/// <summary>
			/// No timer available.
			/// </summary>
			None,
			/// <summary>
			/// The timer is a Query Performance Counter.
			/// </summary>
			QueryPerformanceCounter,
			/// <summary>
			/// The timer will use TimeGetTime.
			/// </summary>
			TimeGetTime,
			/// <summary>
			/// The timer will use GetTimeOfDay.
			/// </summary>
			GetTimeOfDay
		}
		#endregion Enums

		// --- Creation & Destruction Methods ---
		#region Constructor
		/// <summary>
		/// This static constructor determines which platform timer to use
		/// and populates the timer's <see cref="Frequency" />
		/// and <see cref="TimerType" />.
		/// </summary>
		static HighResolutionTimer() {
			bool test = false;
			ulong testTime = 0;

			// Try The Windows QueryPerformanceCounter.
			try {
				test = QueryPerformanceFrequency(ref timerFrequency);
			}
			catch(DllNotFoundException e) {
				Console.WriteLine(e.ToString());
				test = false;
			}
			catch(EntryPointNotFoundException e) {
				Console.WriteLine(e.ToString());
				test = false;
			}

			if(test && timerFrequency != 0) {											// If The QueryPerformanceCounter Is Supported
				timerType = TimerType.QueryPerformanceCounter;							// Let's Use It
			}
			else {																		// Otherwise
				try {																	// Let's Try TimeGetTime().
					test = true;
					testTime = timeGetTime();
				}
				catch(DllNotFoundException e) {
					Console.WriteLine(e.ToString());
					test = false;
				}
				catch(EntryPointNotFoundException e) {
					Console.WriteLine(e.ToString());
					test = false;
				}

				if(test && testTime != 0) {												// If TimeGetTime Is Supported
					timerType = TimerType.TimeGetTime;									// Let's Use It
					timerFrequency = 1000;
				}
			}
			// TODO: Add support for *NIX
		}
		#endregion Constructor

		#region Dispose()
		/// <summary>
		/// Disposes of this class.  Implements IDisposable.
		/// </summary>
		public void Dispose() {
			Dispose(true);																// We've Manually Called For A Dispose
			GC.SuppressFinalize(this);													// Prevent Being Added To The Finalization Queue
		}
		#endregion Dispose()

		#region Dispose(bool disposing)
		/// <summary>
		/// Cleans up either unmanaged resources or managed and unmanaged resources.
		/// </summary>
		/// <remarks>
		/// <para>
		/// If disposing equals true, the method has been called directly or indirectly by a user's 
		/// code.  Managed and unmanaged resources can be disposed.
		/// </para>
		/// <para>
		/// If disposing equals false, the method has been called by the runtime from inside the 
		/// finalizer and you should not reference other objects.  Only unmanaged resources can 
		/// be disposed.
		/// </para>
		/// </remarks>
		/// <param name="disposing">Was Dispose called manually?</param>
		public void Dispose(bool disposing) {
			if(!isDisposed) {															// Check To See If Dispose Has Already Been Called
				if(disposing) {															// If disposing Equals true, Dispose All Managed And Unmanaged Resources
				}

				// Release Any Unmanaged Resources Here, If disposing Was false, Only The Following Code Is Executed
			}
			isDisposed = true;															// Mark As disposed
		}
		#endregion Dispose(bool disposing)

		#region Finalizer
		/// <summary>
		/// This destructor will run only if the Dispose method does not get called.  It gives 
		/// the class the opportunity to finalize.  Simply calls Dispose(false).
		/// </summary>
		~HighResolutionTimer() {
			Dispose(false);																// We've Automatically Called For A Dispose
		}
		#endregion Finalizer

		// --- Private Methods ---
		#region GetCurrentCount()
		/// <summary>
		/// Gets the current tick count.
		/// </summary>
		/// <returns>
		/// Number Of Ticks (ulong).
		/// </returns>
		private ulong GetCurrentCount() {
			ulong tmp = 0;

			if(timerType == TimerType.QueryPerformanceCounter) {						// If Using QPC
				QueryPerformanceCounter(ref tmp);										// Get The Count
				return tmp;																// Return The Count
		}
			else if(timerType == TimerType.TimeGetTime) {								// If We're Using TimeGetTime
				tmp = timeGetTime();													// Get The Count
				return tmp;																// Return The Count
			}
			else {																		// Otherwise
				return 0;																// Return 0
			}
		}
		#endregion GetCurrentCount()

		// --- Methods ---
		#region Main()
		/*
		/// <summary>
		/// Test the High Resolution Timer as a console application.
		/// </summary>
		[STAThread]
		public static void Main(string[] args) {
			HighResolutionTimer timer = new HighResolutionTimer();
			Console.WriteLine("-----");
			Console.WriteLine("Timer Type: {0} in use.", timer.Type);
			Console.WriteLine(" Frequency: {0:N0} ticks per second.", timer.Frequency);
			Console.WriteLine("Resolution: {0:G} (1/{1}) seconds.", timer.Resolution, timer.Frequency);
			Console.WriteLine("   Is Running: {0}.", timer.IsRunning);
			Console.WriteLine("Current Count: {0:N0} ticks.", timer.Count);
			Console.WriteLine("-----");
			Console.WriteLine("Starting Timer...");
			timer.Start();
			Console.WriteLine("   Is Running: {0}.", timer.IsRunning);
			Console.WriteLine("Sleeping for 5 seconds...");
			System.Threading.Thread.Sleep(5000);
			timer.Stop();
			Console.WriteLine("Stopped Timer...");
			Console.WriteLine("-----");
			Console.WriteLine("Start Count: {0:N0} ticks.", timer.StartCount);
			Console.WriteLine("  End Count: {0:N0} ticks.", timer.EndCount);
			Console.WriteLine(" Difference: {0:N0} ticks.", timer.Difference);
			Console.WriteLine("    Elapsed: {0:G} seconds.", timer.Elapsed);
			Console.WriteLine("-----");
			Console.WriteLine("Timing overhead for 1,000,000 Start()/Stop() calls...");
			float tmp = 0;
			int i = 0;
			for(i=0; i < 1000000; i++) {
				timer.Start();
				timer.Stop();
				tmp += (float) timer.Difference;
			}
			tmp = (float) tmp / (float) i;
			Console.WriteLine("Overhead: {0:N0} ticks ({1:G} seconds).", tmp,  ((float) tmp / (float) timer.Frequency));

			//Make the console window wait.
			Console.WriteLine();
			Console.Write("Press Enter to finish ... ");
			Console.Read();
		}
		*/
		#endregion Main()

		#region Start()
		/// <summary>
		/// Start this instance's timer.
		/// </summary>
		public void Start() {
			this.timerStartCount = GetCurrentCount();									// Get New Start Count
			this.timerIsRunning = true;													// Mark The Timer Is Running
			this.timerEndCount = this.timerStartCount;									// Make The End Count At Least Equal To The Start Count
		}
		#endregion Start()

		#region Stop()
		/// <summary>
		/// Stop this instance's timer.
		/// </summary>
		public void Stop() {
			this.timerEndCount = GetCurrentCount();										// Get The End Count
			this.timerIsRunning = false;												// Mark The Timer As Stopped
		}
		#endregion Stop()

		#region Reset()
		/// <summary>
		/// Reset this instance's timer.
		/// </summary>
		public void Reset() {
			Start();																	// Reset By Restarting The Timer
		}
		#endregion Reset()

		// --- Externs ---
		#region Externs
		[DllImport("kernel32.dll")]
		private static extern bool QueryPerformanceFrequency(ref ulong frequencyCount);

		[DllImport("kernel32.dll")]
		private static extern bool QueryPerformanceCounter(ref ulong performanceCount);

		[DllImport("winmm.dll")]
		private static extern ulong timeGetTime();
		#endregion Externs
	}
}