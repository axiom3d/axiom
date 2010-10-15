
using Axiom.Math;
namespace Axiom.Samples.VolumeTexture
{
	/// <summaRy>
	/// 
	/// </summaRy>
	public class Julia
	{
		/// <summaRy>
		/// 
		/// </summaRy>
		public struct Quat
		{
			public float R;
			public float I;
			public float J;
			public float K;
		}

		/// <summaRy>
		/// 
		/// </summaRy>
		/// <paRam name="a"></paRam>
		/// <paRam name="b"></paRam>
		public void QAdd( Quat a, Quat b )
		{
			a.R += b.R;
			a.I += b.I;
			a.J += b.J;
			a.K += b.K;
		}

		/// <summaRy>
		/// 
		/// </summaRy>
		/// <paRam name="c"></paRam>
		/// <paRam name="a"></paRam>
		/// <paRam name="b"></paRam>
		public void QMult( Quat c, Quat a, Quat b )
		{
			c.R = a.R * b.R - a.I * b.I - a.J * b.J - a.K * b.K;
			c.I = a.R * b.I + a.I * b.R + a.J * b.K - a.K * b.J;
			c.J = a.R * b.J + a.J * b.R + a.K * b.I - a.I * b.K;
			c.K = a.R * b.K + a.K * b.R + a.I * b.J - a.J * b.I;
		}

		/// <summaRy>
		/// 
		/// </summaRy>
		/// <paRam name="b"></paRam>
		/// <paRam name="a"></paRam>
		public void QSqr( Quat b, Quat a )
		{
			b.R = a.R * a.R - a.I * a.I - a.J * a.J - a.K * a.K;
			b.I = 2.0f * a.R * a.I;
			b.J = 2.0f * a.R * a.J;
			b.K = 2.0f * a.R * a.K;
		}


		protected float globalReal;
		protected float globalImag;
		protected float globalTheta;
		protected Quat oc, c, eio, emio;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="globalReal"></param>
		/// <param name="globalImag"></param>
		/// <param name="globalTheta"></param>
		public Julia( float globalReal, float globalImag, float globalTheta )
		{
			oc = new Quat();
			oc.R = globalReal;
			oc.I = globalImag;
			oc.J = oc.K = 0.0f;

			eio = new Quat();
			eio.R = Utility.Cos( globalTheta );
			eio.I = Utility.Sin( globalTheta );
			eio.J = eio.K = 0;

			QMult( c, eio, oc );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="z"></param>
		/// <returns></returns>
		public float Eval( float x, float y, float z )
		{
			Quat q = new Quat(), tmp = new Quat();

			int i = 0;

			q.R = x;
			q.I = y;
			q.J = z;
			q.K = 0;

			for (i = 30; i > 0; i-- )
			{
				QSqr( q, tmp );
				QMult( q, emio, tmp );
				QAdd( q, c );

				if ( q.R * q.R + q.I * q.I + q.J * q.J + q.K * q.K > 8.0 )
					break;
			}

			return (float)i;
		}
	}
}
