using System;
using Axiom.MathLib;

namespace Axiom.Audio
{
	/// <summary>
	/// Summary description for ISound.
	/// </summary>
	public interface ISound
	{
		#region Methods

		/// <summary>
		/// 
		/// </summary>
		/// <param name="loop"></param>
		void Play(bool loop);

		/// <summary>
		/// 
		/// </summary>
		void Stop();

		/// <summary>
		/// 
		/// </summary>
		void Destroy();

		#endregion

		#region Properties

		/// <summary>
		/// 
		/// </summary>
		Vector3 Position { get; set; }

		/// <summary>
		/// 
		/// </summary>
		int Volumes { get; set; }

		#endregion
	}
}
