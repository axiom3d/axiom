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

#endregion

#region SVN Version Information

// <file>
//     <id value="$Id: Timer.cs 1537 2009-03-30 19:25:01Z borrillis $"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System.Diagnostics;

#endregion Namespace Declarations

namespace Axiom.Core
{
	///<summary>
	///  Encapsulates the functionality of the platform's highest resolution timer available.
	///</summary>
	///<remarks>
	///  based on an vb.net implementation by createdbyx as posted in SourceForge Tracker #: [1612705]
	///</remarks>
	public class Timer : ITimer
	{
		#region Private Fields

		private readonly Stopwatch _timer = new Stopwatch();

		#endregion Private Fields

		#region Methods

		/// <summary>
		///   Start this instance's timer.
		/// </summary>
		public void Start()
		{
			_timer.Start();
		}

		#endregion Methods

		#region Public Properties

		/// <summary>
		///   Gets a <see cref="System.Int64" /> representing the current tick count of the timer.
		/// </summary>
		public long Count
		{
			get
			{
				return _timer.ElapsedTicks;
			}
		}

		/// <summary>
		///   Gets a <see cref="System.Int64" /> representing the frequency of the counter in ticks-per-second.
		/// </summary>
		public long Frequency
		{
			get
			{
				return Stopwatch.Frequency;
			}
		}

		/// <summary>
		///   Gets a <see cref="System.Boolean" /> representing whether the timer has been started and is currently running.
		/// </summary>
		public bool IsRunning
		{
			get
			{
				return _timer.IsRunning;
			}
		}

		/// <summary>
		///   Gets a <see cref="System.Double" /> representing the resolution of the timer in seconds.
		/// </summary>
		public float Resolution
		{
			get
			{
				return ( (float)1.0/(float)Frequency );
			}
		}

		/// <summary>
		///   Gets a <see cref="System.Int64" /> representing the tick count at the start of the timer's run.
		/// </summary>
		public long StartCount
		{
			get
			{
				return 0;
			}
		}

		#endregion Public Properties

		#region ITimer Members

		///<summary>
		///  Reset this instance's timer.
		///</summary>
		public void Reset()
		{
			// reset by restarting the timer
			_timer.Reset();
			_timer.Start();
		}

		public long Microseconds
		{
			get
			{
				return _timer.ElapsedMilliseconds/10;
			}
		}

		public long Milliseconds
		{
			get
			{
				return _timer.ElapsedMilliseconds;
			}
		}

		#endregion
	}
}