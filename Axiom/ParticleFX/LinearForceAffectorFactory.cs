using System;
using System.Drawing;
using Axiom.Core;
using Axiom.ParticleSystems;
using Axiom.MathLib;

namespace AxiomParticleFX
{
	/// <summary>
	/// Summary description for LinearForceAffectorFactory.
	/// </summary>
	public class LinearForceAffectorFactory : ParticleAffectorFactory
	{
		public LinearForceAffectorFactory()
		{
		}

		public override string Name
		{
			get
			{
				return "LinearForce";
			}
		}

		public override ParticleAffector Create()
		{
			ParticleAffector p = new LinearForceAffector();
			affectorList.Add(p);

			return p;
		}


	}
}
