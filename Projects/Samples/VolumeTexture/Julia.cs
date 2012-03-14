#region Namespace Declarations

using Axiom.Math;

#endregion Namespace Declarations

namespace Axiom.Samples.VolumeTexture
{
	public class Julia
	{
		protected Quat c, eio, emio;
		protected float globalImag;
		protected float globalReal;
		protected float globalTheta;
		protected Quat oc;

		[OgreVersion( 1, 7, 2 )]
		public Julia( float globalReal, float globalImag, float globalTheta )
		{
			this.globalReal = globalReal;
			this.globalImag = globalImag;
			this.globalTheta = globalTheta;

			this.oc = new Quat();
			this.oc.R = globalReal;
			this.oc.I = globalImag;
			this.oc.J = this.oc.K = 0.0f;

			this.eio = new Quat();
			this.eio.R = Utility.Cos( globalTheta );
			this.eio.I = Utility.Sin( globalTheta );
			this.eio.J = this.eio.K = 0;

			this.emio = new Quat();
			this.emio.R = Utility.Cos( -globalTheta );
			this.emio.I = Utility.Sin( -globalTheta );
			this.emio.J = this.emio.K = 0;

			QMult( ref this.c, this.eio, this.oc );
		}

		[OgreVersion( 1, 7, 2 )]
		public void QAdd( ref Quat a, Quat b )
		{
			a.R += b.R;
			a.I += b.I;
			a.J += b.J;
			a.K += b.K;
		}

		[OgreVersion( 1, 7, 2 )]
		public void QMult( ref Quat c, Quat a, Quat b )
		{
			c.R = a.R * b.R - a.I * b.I - a.J * b.J - a.K * b.K;
			c.I = a.R * b.I + a.I * b.R + a.J * b.K - a.K * b.J;
			c.J = a.R * b.J + a.J * b.R + a.K * b.I - a.I * b.K;
			c.K = a.R * b.K + a.K * b.R + a.I * b.J - a.J * b.I;
		}

		[OgreVersion( 1, 7, 2 )]
		public void QSqr( ref Quat b, Quat a )
		{
			b.R = a.R * a.R - a.I * a.I - a.J * a.J - a.K * a.K;
			b.I = 2.0f * a.R * a.I;
			b.J = 2.0f * a.R * a.J;
			b.K = 2.0f * a.R * a.K;
		}

		[OgreVersion( 1, 7, 2 )]
		public float Eval( float x, float y, float z )
		{
			var q = new Quat();
			var tmp = new Quat();

			int i = 0;

			q.R = x;
			q.I = y;
			q.J = z;
			q.K = 0;

			for ( i = 30; i > 0; i-- )
			{
				QSqr( ref tmp, q );
				QMult( ref q, this.emio, tmp );
				QAdd( ref q, this.c );

				if ( q.R * q.R + q.I * q.I + q.J * q.J + q.K * q.K > 8.0 )
				{
					break;
				}
			}

			return i;
		}

		#region Nested type: Quat

		[OgreVersion( 1, 7, 2 )]
		public struct Quat
		{
			public float I;
			public float J;
			public float K;
			public float R;
		}

		#endregion
	};
}
