using System;
using Axiom.Core;
using Axiom.Input;

namespace Axiom.Platforms.Win32
{
	/// <summary>
	/// Summary description for Class1.
	/// </summary>
	// TODO: Disposal of object create here.
	public class Win32PlatformManager : IPlatformManager {
		private IInputReader inputReader;
        private ITimer timer;

		public Win32PlatformManager() {
		}

		#region IPlatformManager Members

		public Axiom.Input.IInputReader CreateInputReader() {
			inputReader = new Win32InputReader();
			return inputReader;
		}

		public ITimer CreateTimer() {
            timer = new Win32Timer();
			return timer;
		}

		#endregion
	}
}
