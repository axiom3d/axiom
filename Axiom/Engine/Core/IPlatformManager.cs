using System;
using Axiom.Core;
using Axiom.Input;

namespace Axiom.Core
{
	/// <summary>
	/// Summary description for IPlatformManager.
	/// </summary>
	public interface IPlatformManager {

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
	}
}
