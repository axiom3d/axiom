using System;
using System.Drawing;
using Axiom.Core;
using Axiom.ParticleSystems;
using Axiom.MathLib;

namespace AxiomParticleFX
{
	public enum ForceApplication
	{
		Average,
		Add
	}

	/// <summary>
	/// Summary description for LinearForceAffector.
	/// </summary>
	public class LinearForceAffector : ParticleAffector
	{
		protected ForceApplication forceApp = ForceApplication.Add;
		protected Vector3 forceVector = Vector3.Zero;

		public LinearForceAffector()
		{
			// HACK: See if there is better way to do this
			this.type = "LinearForce";
		}

		public override void AffectParticles(ParticleSystem system, float timeElapsed)
		{
			Vector3 scaledVector = Vector3.Zero;

			if(forceApp == AxiomParticleFX.ForceApplication.Add)
			{
				// scale force by time
				scaledVector = forceVector * timeElapsed;
			}

			// affect each particle
			for(int i = 0; i < system.Particles.Count; i++)
			{
				Particle p = (Particle)system.Particles[i];

				if(forceApp == AxiomParticleFX.ForceApplication.Add)
					p.Direction += scaledVector;
				else // Average
					p.Direction = (p.Direction + forceVector) / 2;
			}
		}

		public Vector3 Force
		{
			get { return forceVector; }
			set { forceVector = value; }
		}

		public ForceApplication ForceApplication
		{
			get { return forceApp; }
			set { forceApp = value; }
		}

	}
}
