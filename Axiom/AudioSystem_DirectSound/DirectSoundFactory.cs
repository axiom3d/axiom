using System;
using Axiom.Audio;

namespace AudioSystem_DirectSound
{
	/// <summary>
	/// Summary description for DirectSoundFactory.
	/// </summary>
	public class DirectSoundFactory : IAudioSystemFactory
	{

		#region IAudioSystemFactory Members

		public IAudioSystem Create()
		{
			return new DSoundSystem();
		}

		#endregion
	}
}
