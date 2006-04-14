using System;
using Axiom;
using Axiom.Input;

namespace Axiom
{
    /// <summary>
    /// General behavior for a platform implementation class
    /// </summary>
    public interface IPlatformManager : IDisposable
    {

        /// <summary>
        ///		Creates a new input reader implementation specific to this platform.
        /// </summary>
        /// <returns></returns>
        InputReader CreateInputReader();

        /// <summary>
        ///		Creates a new timer implementation specific to this platform.
        /// </summary>
        /// <returns>A timer implementation.</returns>
        ITimer CreateTimer();

        /// <summary>
        ///		Implement to allow the host operating system to process pending events
        ///		for the current process.
        /// </summary>
        /// <remarks>
        ///		May not be relevant on all platforms.
        /// </remarks>
        void DoEvents();
    }
}
