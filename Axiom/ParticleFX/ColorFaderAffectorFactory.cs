using System;
using Axiom.ParticleSystems;

namespace AxiomParticleFX
{
	/// <summary>
	/// Summary description for ColorFaderAffectorFactory.
	/// </summary>
	public class ColorFaderAffectorFactory : ParticleAffectorFactory
	{
		public ColorFaderAffectorFactory()
		{
			//
			// TODO: Add constructor logic here
			//
		}

		public override string Name
		{
			get
			{
				return "ColorFader";
			}
		}

		public override ParticleAffector Create()
		{
			ParticleAffector p = new ColorFaderAffector();
			affectorList.Add(p);

			return p;
		}
	}
}
