using System;

namespace Axiom.Audio
{
	/// <summary>
	/// Summary description for IAudioSystemFactory.
	/// </summary>
	public interface IAudioSystemFactory
	{
		/// <summary>
		///		
		/// </summary>
		/// <param name="name"></param>
		IAudioSystem Create();
	}
}
