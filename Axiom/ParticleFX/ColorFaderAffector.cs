using System;
using Axiom.ParticleSystems;

namespace AxiomParticleFX
{
	/// <summary>
	/// Summary description for ColorFaderAffector.
	/// </summary>
	public class ColorFaderAffector : ParticleAffector
	{
		protected float alphaAdjust;
		protected float redAdjust;
		protected float greenAdjust;
		protected float blueAdjust;

		public ColorFaderAffector()
		{
			this.type = "ColorFader";
		}

		public float AlphaAdjust
		{
			get { return alphaAdjust; }
			set { alphaAdjust = value; }
		}

		public float RedAdjust
		{
			get { return redAdjust; }
			set { redAdjust = value; }
		}

		public float GreenAdjust
		{
			get { return greenAdjust; }
			set { greenAdjust = value; }
		}

		public float BlueAdjust
		{
			get { return blueAdjust; }
			set { blueAdjust = value; }
		}

		protected void AdjustWithClamp(ref float component, float adjust)
		{
			component += adjust;

			// limit to range [0,1]
			if(component < 0.0f)
				component = 0.0f;
			else if(component > 1.0f)
				component = 1.0f;
		}

		public override void AffectParticles(ParticleSystem system, float timeElapsed)
		{
			float da, dr, dg, db;

			da = alphaAdjust * timeElapsed;
			dr = redAdjust * timeElapsed;
			dg = greenAdjust * timeElapsed;
			db = blueAdjust * timeElapsed;

			// loop through the particles

			for(int i = 0; i < system.Particles.Count; i++)
			{
				Particle p = (Particle)system.Particles[i];

				// adjust the values with clamping ([0,1] in this case)
				AdjustWithClamp(ref p.Color.a, da);
				AdjustWithClamp(ref p.Color.r, dr);
				AdjustWithClamp(ref p.Color.g, dg);
				AdjustWithClamp(ref p.Color.b, db);

			}
		}

	}
}
