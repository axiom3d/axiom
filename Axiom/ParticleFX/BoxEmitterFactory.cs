using System;
using Axiom.Core;
using Axiom.ParticleSystems;

namespace ParticleFX
{
	/// <summary>
	/// Summary description for BoxEmitterFactory.
	/// </summary>
	public class BoxEmitterFactory : ParticleEmitterFactory
	{
		public BoxEmitterFactory()
		{
		}

		public override string Name
		{
			get { return "Box"; }
		}

		public override ParticleEmitter Create()
		{
			ParticleEmitter emitter = new BoxEmitter();
			emitterList.Add(emitter);

			return emitter;
		}


	}
}
