using System;
using System.Drawing;
using Axiom.Core;
using Axiom.ParticleSystems;
using Axiom.MathLib;

namespace AxiomParticleFX
{
	/// <summary>
	/// Summary description for BoxEmitter.
	/// </summary>
	public class BoxEmitter : AreaEmitter
	{
		public BoxEmitter() : base()
		{
			InitDefaults("Box");
		}

		public override void InitParticle(Particle particle)
		{
			Vector3 xOff, yOff, zOff;

			xOff = MathUtil.SymmetricRandom() * xRange;
			yOff = MathUtil.SymmetricRandom() * yRange;
			zOff = MathUtil.SymmetricRandom() * zRange;

			particle.Position = position + xOff + yOff + zOff;
	        
			// Generate complex data by reference
			GenerateEmissionColor(particle.Color);
			GenerateEmissionDirection(ref particle.Direction);
			GenerateEmissionVelocity(ref particle.Direction);

			// Generate simpler data
			particle.timeToLive = GenerateEmissionTTL();
		}

	}
}
