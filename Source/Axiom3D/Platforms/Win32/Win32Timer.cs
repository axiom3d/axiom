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

#region SVN Version Information
// <file>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Runtime.InteropServices;

using Axiom.Core;
using System.Diagnostics;

#endregion Namespace Declarations

namespace Axiom.Platforms.Win32
{
    /// <summary>
    ///		Encapsulates the functionality of the platform's highest resolution timer available.
    /// </summary>
    public class Win32Timer : ITimer
    {
        #region Private Fields

        private Stopwatch _timer = new Stopwatch();

        #endregion Private Fields

        #region Methods

        /// <summary>
        /// Start this instance's timer.
        /// </summary>
        public void Start()
        {
            _timer.Start();
        }

        #endregion Methods

        #region Public Properties
        /// <summary>
        /// Gets a <see cref="System.UInt64" /> representing the 
        /// current tick count of the timer.
        /// </summary>
        public long Count
        {
            get
            {
                return _timer.ElapsedTicks;
            }
        }

        /// <summary>
        /// Gets a <see cref="System.UInt64" /> representing the 
        /// frequency of the counter in ticks-per-second.
        /// </summary>
        public long Frequency
        {
            get
            {
                return Stopwatch.Frequency;
            }
        }

        /// <summary>
        /// Gets a <see cref="System.Boolean" /> representing whether the 
        /// timer has been started and is currently running.
        /// </summary>
        public bool IsRunning
        {
            get
            {
                return _timer.IsRunning;
            }
        }

        /// <summary>
        /// Gets a <see cref="System.Double" /> representing the 
        /// resolution of the timer in seconds.
        /// </summary>
        public float Resolution
        {
            get
            {
                return ( (float)1.0 / (float)Frequency );
            }
        }

        /// <summary>
        /// Gets a <see cref="System.UInt64" /> representing the 
        /// tick count at the start of the timer's run.
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

        /// <summary>
        ///		Reset this instance's timer.
        /// </summary>
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
                return _timer.ElapsedMilliseconds / 10;
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
