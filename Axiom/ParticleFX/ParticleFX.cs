using System;
using Axiom.Core;
using Axiom.ParticleSystems;

namespace AxiomParticleFX
{
	/// <summary>
	/// Summary description for ParticleFX.
	/// </summary>
	public class ParticleFX : IPlugin
	{
		public ParticleFX()
		{
		}
		#region IPlugin Members

		public void Start()
		{
			ParticleEmitterFactory emitterFactory;
			ParticleAffectorFactory affectorFactory;

			// box emitter factory
			emitterFactory = new BoxEmitterFactory();
			ParticleSystemManager.Instance.AddEmitterFactory(emitterFactory);

			// linear force affector factory
			affectorFactory = new LinearForceAffectorFactory();
			ParticleSystemManager.Instance.AddAffectorFactory(affectorFactory);

			// color fader affector factory
			affectorFactory = new ColorFaderAffectorFactory();
			ParticleSystemManager.Instance.AddAffectorFactory(affectorFactory);
		}

		public void Stop()
		{
			// TODO:  Add ParticleFX.Stop implementation
		}

		#endregion
	}
}
