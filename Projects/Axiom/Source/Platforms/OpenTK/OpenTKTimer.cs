#region Namespace Declarations

using System.Diagnostics;

using Axiom.Core;

#endregion Namespace Declarations


namespace Axiom.Platforms.OpenTK
{
    /// <summary>
    ///		Encapsulates the functionality of the platform's highest resolution timer available.
    /// </summary>
    public class OpenTKTimer : ITimer
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
                return ((float)1.0 / (float)Frequency);
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
