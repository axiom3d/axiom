using System;

namespace Axiom.Audio
{
	/// <summary>
	/// Summary description for IAudioSystem.
	/// </summary>
	public interface IAudioSystem
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		ISound CreateSound(string name);
	}
}
