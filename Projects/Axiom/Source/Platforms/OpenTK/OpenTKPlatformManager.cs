#region Namespace Declarations


using Axiom.Core;
using Axiom.Input;

#endregion Namespace Declarations

namespace Axiom.Platforms.OpenTK
{
    public class OpenTKPlatformManager : IPlatformManager
    {
        #region Fields

        /// <summary>
        ///		Reference to the current input reader.
        /// </summary>
        private InputReader inputReader;
        /// <summary>
        ///		Reference to the current active timer.
        /// </summary>
        private ITimer timer;

        #endregion Fields

        #region IPlatformManager Members

        /// <summary>
        ///		Creates an InputReader
        /// </summary>
        /// <returns></returns>
        public Axiom.Input.InputReader CreateInputReader()
        {
            inputReader = new OpenTKInputReader();
            return inputReader;
        }

        /// <summary>
        ///		Creates a high precision timer.
        /// </summary>
        /// <returns></returns>
        public ITimer CreateTimer()
        {
            timer = new OpenTKTimer();
            return timer;
        }

        /// <summary>
        /// </summary>
        public void DoEvents()
        {
        }

        #endregion

        #region IDisposable Members
        public void Dispose()
        {
            timer.Reset();
            timer = null;

            inputReader.Dispose();
            inputReader = null;
        }
        #endregion
    }
}
