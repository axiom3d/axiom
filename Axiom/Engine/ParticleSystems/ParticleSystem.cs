#region LGPL License
/*
Axiom Game Engine Library
Copyright (C) 2003  Axiom Project Team

This library is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 2.1 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public
License along with this library; if not, write to the Free Software
Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
*/
#endregion

using System;
using System.Collections;
using System.Drawing;
using Axiom.Core;
using Axiom.Enumerations;
using Axiom.MathLib;

namespace Axiom.ParticleSystems
{
	/// <summary>
	///		Class defining particle system based special effects.
	/// </summary>
	/// <remarks>
	///		Particle systems are special effects generators which are based on a number of moving points
	///		which are rendered perhaps using billboards (quads which always face the camera) to create
	///		the impression of things like like sparkles, smoke, blood spurts, dust etc.
	///		<p/>
	///		This class simply manages a single collection of particles with a shared local center point
	///		and a bounding box. The visual aspect of the particles is handled by the base BillboardSet class
	///		which the ParticleSystem manages automatically.
	///		<p/>
	///		Particle systems are created using the ParticleSystemManager.CreateParticleSystem method, never directly.
	///		In addition, like all subclasses of SceneObject, the ParticleSystem will only be considered for
	///		rendering once it has been attached to a SceneNode. 
	/// </summary>
	public class ParticleSystem : BillboardSet
	{
		#region Member variables

		/// <summary>List of emitters for this system.</summary>
		protected ArrayList emitterList = new ArrayList();
		/// <summary>List of affectors for this system.</summary>
		protected ArrayList affectorList = new ArrayList();

		#endregion

		#region Constructors

		/// <summary>
		///		Default constructor
		/// </summary>
		public ParticleSystem() { }

		/// <summary>
		///		Creates a particle system with no emitters or affectors.
		/// </summary>
		/// <remarks>
		///		You should use the ParticleSystemManager to create systems, rather than doing it directly.
		/// </remarks>
		/// <param name="name"></param>
		public ParticleSystem(String name)
		{
			autoExtendPool = true;
			allDefaultSize = true;
			originType = BillboardOrigin.Center;
			this.name = name;
			cullIndividual = true;
			this.DefaultDimensions = new Size(100, 100);
			this.MaterialName = "BaseWhite";
			this.PoolSize = 10;

			// TODO: Init parameters for loading from scripts
		}

		/// <summary>
		///		Adds an emitter to this particle system.
		///	 </summary>
		///	 <remarks>	
		///		Particles are created in a particle system by emitters - see the ParticleEmitter
		///		class for more details.
		/// </remarks>
		/// <param name="emitterType">
		///		String identifying the emitter type to create. Emitter types are defined
		///		by registering new factories with the manager - see ParticleEmitterFactory for more details.
		///		Emitter types can be extended by plugin authors.
		/// </param>
		/// <returns></returns>
		public ParticleEmitter AddEmitter(String emitterType)
		{
			ParticleEmitter emitter = ParticleSystemManager.Instance.CreateEmitter(emitterType);
			emitterList.Add(emitter);

			return emitter;
		}

		/// <summary>
		///		Adds an affector to this particle system.
		///	 </summary>
		///	 <remarks>	
		///		Particles are modified over time in a particle system by affectors - see the ParticleAffector
		///		class for more details.
		/// </remarks>
		/// <param name="emitterType">
		///		String identifying the affector type to create. Affector types are defined
		///		by registering new factories with the manager - see ParticleAffectorFactory for more details.
		///		Affector types can be extended by plugin authors.
		/// </param>
		/// <returns></returns>
		public ParticleAffector AddAffector(String affectorType)
		{
			ParticleAffector affector = ParticleSystemManager.Instance.CreateAffector(affectorType);
			affectorList.Add(affector);

			return affector;
		}

		#endregion

		#region Methods

		/// <summary>
		///		Updates the particles in the system based on time elapsed.
		///	 </summary>
		///	 <remarks>	
		///		This is called automatically every frame by the engine.
		/// </remarks>
		/// <param name="timeElapsed">The amount of time (in seconds) since the last frame.</param>
		internal void Update(float timeElapsed)
		{
			Expire(timeElapsed);
			TriggerEmitters(timeElapsed);
			TriggerAffectors(timeElapsed);
			ApplyMotion(timeElapsed);
			UpdateBounds();
		}

		/// <summary>
		///		Overloaded method.
		/// </summary>
		/// <param name="time"></param>
		public void FastForward(float time)
		{
			FastForward(time, 0.1f);
		}

		/// <summary>
		///		Fast-forwards this system by the required number of seconds.
		///	 </summary>
		///	 <remarks>
		///		This method allows you to fast-forward a system so that it effectively looks like
		///		it has already been running for the time you specify. This is useful to avoid the
		///		'startup sequence' of a system, when you want the system to be fully populated right
		///		from the start.
		/// </remarks>
		/// </summary>
		/// <param name="time">The number of seconds to fast-forward by.</param>
		/// <param name="interval">
		///		The sampling interval used to generate particles, apply affectors etc. The lower this
		///		is the more realistic the fast-forward, but it takes more iterations to do it.
		/// </param>
		public void FastForward(float time, float interval)
		{
			for(float t = 0.0f; t < time; t += interval)
				Update(interval);
		}

		/// <summary>
		///		Overriden.
		/// </summary>
		public override Axiom.MathLib.Matrix4[] WorldTransforms
		{
			get { return new Matrix4[] { Matrix4.Identity }; 	}
		}

		/// <summary>
		///		Overriden.
		/// </summary>
		public override void UpdateBounds()
		{
			base.UpdateBounds();

			if(parentNode != null)
			{
				// Have to override because bounds are supposed to be in local node space
				// but we've already put particles in world space to decouple them from the
				// node transform, so reverse transform back

				Vector3 min = new Vector3(Single.PositiveInfinity, Single.PositiveInfinity, Single.PositiveInfinity);
				Vector3 max = new Vector3(Single.NegativeInfinity, Single.NegativeInfinity, Single.NegativeInfinity);
				Vector3 temp;

				Vector3[] corners = aab.Corners;
				Quaternion invQ = parentNode.DerivedOrientation.Inverse();
				Vector3 t = parentNode.DerivedPosition;

				for(int i = 0; i < 8; i++)
				{
					// reverse transform corner
					temp = invQ * (corners[i] - t);
					min.Floor(temp);
					max.Ceil(temp);
				}

				aab.SetExtents(min, max);
			}
		}

		/// <summary>
		///		Used to expire dead particles.
		/// </summary>
		/// <param name="timeElapsed"></param>
		protected void Expire(float timeElapsed)
		{
			for(int i = 0; i < activeBillboards.Count; i++)
			{
				Particle particle = (Particle)activeBillboards[i];

				// is this particle dead?
				if(particle.timeToLive < timeElapsed)
				{
					// add back to the free queue and remove from active list
					freeBillboards.Enqueue(particle);
					activeBillboards.Remove(particle);
				}
				else
				{
					// decrement TTL
					particle.timeToLive -= timeElapsed;
				}
			}
		}

		/// <summary>
		///		Spawn new particles based on free quota and emitter requirements.
		/// </summary>
		/// <param name="timeElapsed"></param>
		protected void TriggerEmitters(float timeElapsed)
		{
			// TODO: Optimize this if possible
			ArrayList requested = new ArrayList();

			ParticleEmitter emitter = null;

			int totalRequested, emitterCount, emissionAllowed;

			emitterCount = emitterList.Count;
			// get the difference between quota and current active count
			emissionAllowed = this.ParticleQuota - activeBillboards.Count;
			totalRequested = 0;

			// count up the total requested emissions
			for(int i = 0; i < emitterCount; i++)
			{
				emitter = (ParticleEmitter)emitterList[i];
				int emissionCount = emitter.GetEmissionCount(timeElapsed);
				requested.Insert(i, emissionCount);
				
				totalRequested += emissionCount;
			}

			// check if the quota will be exceeded, if so, reduce demand
			if(totalRequested > emissionAllowed)
			{
				float ratio = (float)emissionAllowed / (float)totalRequested;

				// modify requested values
				for(int i = 0; i < emitterCount; i++)
					requested[i] = (int)requested[i] * (int)ratio;
			}

			// emission time
			for(int i = 0; i < emitterCount; i++)
			{
				// get a reference to the current emitter
				emitter = (ParticleEmitter)emitterList[i];

				for(int j = 0; j < (int)requested[i]; j++)
				{
					// create a new particle and initialize it with the current emitter
					Particle p = AddParticle();
					emitter.InitParticle(p);

					// translate position and direction into world space
					p.Position = (parentNode.DerivedOrientation * p.Position) + parentNode.DerivedPosition;
					p.Direction = parentNode.DerivedOrientation * p.Direction;
				}
			}
		}

		/// <summary>
		///		Updates existing particles based on their momentum.
		/// </summary>
		/// <param name="timeElapsed"></param>
		protected void ApplyMotion(float timeElapsed)
		{
			Particle p = null;

			for(int i = 0; i < activeBillboards.Count; i++)
			{
				p = (Particle)activeBillboards[i];
				p.Position += p.Direction * timeElapsed;
			}
		}

		/// <summary>
		///		Applies the effects of particle affectors.
		/// </summary>
		/// <param name="timeElapsed"></param>
		protected void TriggerAffectors(float timeElapsed)
		{
			for(int i = 0; i < affectorList.Count; i++)
			{
				ParticleAffector affector = (ParticleAffector)affectorList[i];
				affector.AffectParticles(this, timeElapsed);
			}
		}

		/// <summary>
		///		Overriden from BillboardSet to create Particles instead of Billboards.
		/// </summary>
		/// <param name="size"></param>
		protected override void IncreasePool(int size)
		{
			// do NOT use the base class method.  want to ensure Particles get added to the pool, not Billboards
			int oldSize = billboardPool.Count;

			// expand the capacity a bit
			billboardPool.Capacity += size;

			// add fresh Billboard objects to the new slots
			for(int i = oldSize; i < size; i++)
				billboardPool.Add(new Particle());
		}

		/// <summary>
		///		Used internally for adding a new active particle.
		/// </summary>
		/// <returns></returns>
		protected Particle AddParticle()
		{
			Billboard billboard = (Billboard)freeBillboards.Dequeue();
			activeBillboards.Add(billboard);
			billboard.NotifyOwner(this);

			return (Particle)billboard;
		}

		/// <summary>
		///		Overriden from BillboardSet.
		/// </summary>
		/// <param name="camera"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="billboard"></param>
		protected override void GenerateBillboardAxes(Camera camera, ref Axiom.MathLib.Vector3 x, ref Axiom.MathLib.Vector3 y, Billboard billboard)
		{
			// Orientation different from BillboardSet
			// Billboards are in world space (to decouple them from emitters in node space)
			Quaternion camQ = Quaternion.Zero;

			switch (billboardType)
			{
				case BillboardType.Point:
					// Get camera world axes for X and Y (depth is irrelevant)
					// No inverse transform
					camQ = camera.DerivedOrientation;
					x = camQ * Vector3.UnitX;
					y = camQ * Vector3.UnitY;
		           
					break;
				case BillboardType.OrientedCommon:
					// Y-axis is common direction
					// X-axis is cross with camera direction 
					y = commonDirection;
					x = camQ * camera.DerivedDirection.Cross(y);
		           
					break;
				case BillboardType.OrientedSelf:
					// Y-axis is direction
					// X-axis is cross with camera direction 

					// Scale direction first
					y = (billboard.Direction * 0.01f);
					x = camQ * camera.DerivedDirection.Cross(y);

					break;
			}
		}

		#endregion

		#region Properties

		/// <summary>
		///		Gets the count of active particles currently in the system.
		/// </summary>
		public int ParticleCount
		{
			get { return activeBillboards.Count; }
		}

		/// <summary>
		///		Returns the maximum number of particles this system is allowed to have active at once.
		/// </summary>
		/// <remarks>
		///		Particle systems all have a particle quota, i.e. a maximum number of particles they are 
		///		allowed to have active at a time. This allows the application to set a keep particle systems
		///		under control should they be affected by complex parameters which alter their emission rates
		///		etc. If a particle system reaches it's particle quota, none of the emitters will be able to 
		///		emit any more particles. As existing particles die, the spare capacity will be allocated
		///		equally across all emitters to be as consistent to the origina particle system style as possible.
		/// </remarks>
		public int ParticleQuota
		{
			get { return this.PoolSize; }
			set { this.PoolSize = value; }
		}

		/// <summary>
		/// 
		/// </summary>
		public ArrayList Particles
		{
			get { return activeBillboards; }
		}

		#endregion

		/// <summary>
		///		Cloning will deep copy all particle emitters and effectors, but not particles. The
        ///		system's name is also not copied.
		/// </summary>
		/// <returns></returns>
		public void CopyTo(ParticleSystem system)
		{
			// remove the target's emitters and affectors
			system.emitterList.Clear();
			system.affectorList.Clear();

			// loop through emitter and affector lists and copy them over
			for(int i = 0; i < emitterList.Count; i++)
			{
				ParticleEmitter emitter = (ParticleEmitter)emitterList[i];
				ParticleEmitter newEmitter = system.AddEmitter(emitter.Type);
				emitter.CopyTo(newEmitter);
			}

			for(int i = 0; i < affectorList.Count; i++)
			{
				ParticleAffector affector = (ParticleAffector)affectorList[i];
				ParticleAffector newAffector = system.AddAffector(affector.Type);
				affector.CopyTo(newAffector);
			}

			system.PoolSize = this.PoolSize;
			system.MaterialName = this.MaterialName;
			system.originType = this.originType;
			system.defaultDimensions = this.defaultDimensions;
			system.cullIndividual = this.cullIndividual;
			system.billboardType = this.billboardType;
			system.commonDirection = this.commonDirection;
		}
	}
}
