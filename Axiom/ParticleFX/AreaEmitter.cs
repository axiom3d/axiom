using System;
using Axiom.Core;
using Axiom.ParticleSystems;
using Axiom.MathLib;

namespace AxiomParticleFX
{
	/// <summary>
	/// Summary description for AreaEmitter.
	/// </summary>
	public abstract class AreaEmitter : ParticleEmitter
	{
		#region Member variables

		protected Vector3 size = Vector3.Zero;
		protected Vector3 xRange;
		protected Vector3 yRange;
		protected Vector3 zRange;

		#endregion

		public AreaEmitter() : base() { }

		#region Properties

		public override Axiom.MathLib.Vector3 Direction
		{
			get { return base.Direction; }
			set
			{
				base.Direction = value;

				// update the ranges
				GenerateAreaAxes();
			}
		}

		public Vector3 Size
		{
			get { return size; }
			set { size = value; GenerateAreaAxes(); }
		}

		public float Width
		{ 
			get { return size.x; }
			set { size.x = value; GenerateAreaAxes(); }
		}

		public float Height
		{ 
			get { return size.y; }
			set { size.y = value; GenerateAreaAxes(); }
		}

		public float Depth
		{ 
			get { return size.z; }
			set { size.z = value; GenerateAreaAxes(); }
		}

		#endregion

		#region Methods

		protected void GenerateAreaAxes()
		{
			Vector3 left = up.Cross(direction);

			xRange = left * (size.x * 0.5f);
			yRange = up * (size.y * 0.5f);
			zRange = direction * (size.z * 0.5f);
		}

		protected void InitDefaults(String type)
		{
			// TODO: Revisit this
			direction = Vector3.UnitZ;
			up = Vector3.UnitZ;
			this.Size = new Vector3(50, 50, 0);
			this.type = type;
		}

		#endregion

		#region Implementation of ParticleEmitter

		public override ushort GetEmissionCount(float timeElapsed)
		{
			// use basic constant emission
			return GenerateConstantEmissionCount(timeElapsed);
		}

		#endregion

	}
}
