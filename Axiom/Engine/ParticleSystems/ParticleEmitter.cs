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
using System.Drawing;
using Axiom.Core;
using Axiom.MathLib;
using System.Reflection;

namespace Axiom.ParticleSystems
{
	/// <summary>
	///		Abstract class defining the interface to be implemented by particle emitters.
	/// </summary>
	/// <remarks>
	///		Particle emitters are the sources of particles in a particle system. 
	///		This class defines the ParticleEmitter interface, and provides a basic implementation 
	///		for tasks which most emitters will do (these are of course overridable).
	///		Particle emitters can be  grouped into types, e.g. 'point' emitters, 'box' emitters etc; each type will 
	///		create particles with a different starting point, direction and velocity (although
	///		within the types you can configure the ranges of these parameters). 
	///		<p/>
	///		Because there are so many types of emitters you could use, the engine chooses not to dictate
	///		the available types. It comes with some in-built, but allows plugins or games to extend the emitter types available.
	///		This is done by subclassing ParticleEmitter to have the appropriate emission behavior you want,
	///		and also creating a subclass of ParticleEmitterFactory which is responsible for creating instances 
	///		of your new emitter type. You register this factory with the ParticleSystemManager using
	///		AddEmitterFactory, and from then on emitters of this type can be created either from code or through
	///		XML particle scripts by naming the type.
	///		<p/>
	///		This same approach is used for ParticleAffectors (which modify existing particles per frame).
	///		This means that the engine is particularly flexible when it comes to creating particle system effects,
	///		with literally infinite combinations of emitter and affector types, and parameters within those
	///		types.
	/// </remarks>
	public abstract class ParticleEmitter
	{
		#region Member variables

		/// <summary>Position relative to the center of the ParticleSystem</summary>
		protected Vector3 position;
		///<summary> Rate in particles per second at which this emitter wishes to emit particles</summary>
		protected float emissionRate;
		/// <summary>Name of the type of emitter, MUST be initialized by subclasses</summary>
		protected String type;
		/// <summary>Base direction of the emitter, may not be used by some emitters</summary>
		protected Vector3 direction;
		/// <summary>Notional up vector, just used to speed up generation of variant directions</summary>
		protected Vector3 up;
		/// <summary>Angle around direction which particles may be emitted, internally radians but degrees for interface</summary>
		protected float angle;
		/// <summary>Min speed of particles</summary>
		protected float minSpeed;
		/// <summary>Max speed of particles</summary>
		protected float maxSpeed;
		/// <summary>Initial time-to-live of particles (min)</summary>
		protected float minTTL;
		/// <summary>Initial time-to-live of particles (max)</summary>
		protected float maxTTL;
		/// <summary>Initial color of particles (range start)</summary>
		protected ColorEx colorRangeStart;
		/// <summary>Initial color of particles (range end)</summary>
		protected ColorEx colorRangeEnd;

		/// <summary>Whether this emitter is currently enabled (defaults to true)</summary>
		protected bool isEnabled;

		/// <summary>Start time (in seconds from start of first call to ParticleSystem to update)</summary>
		protected float startTime;
		/// <summary>Minimum length of time emitter will run for (0 = forever)</summary>
		protected float durationMin;
		/// <summary>Maximum length of time the emitter will run for (0 = forever)</summary>
		protected float durationMax;
		/// <summary>Current duration remainder</summary>
		protected float durationRemain;

		/// <summary>Time between each repeat</summary>
		protected float repeatDelayMin;
		protected float repeatDelayMax;
		/// <summary>Repeat delay left</summary>
		protected float repeatDelayRemain;

		static float remainder = 0;

		#endregion

		#region Constructors

		/// <summary>
		///		Default constructor.
		/// </summary>
		public ParticleEmitter()
		{
			// set defaults
			angle = 0.0f;
			this.Direction = Vector3.UnitX;
			emissionRate = 10;
			maxSpeed = minSpeed = 1;
			maxTTL = minTTL = 5;
			position = Vector3.Zero;
			colorRangeStart = ColorEx.FromColor(System.Drawing.Color.White);
			colorRangeEnd = ColorEx.FromColor(System.Drawing.Color.White);
			isEnabled = true;
			durationMax = 0;
		}

		#endregion

		#region Properties

		/// <summary>
		///		Gets/Sets the position of this emitter relative to the center of the particle system.
		/// </summary>
		virtual public Vector3 Position
		{
			get { return position; }
			set { position = value; }
		}

		/// <summary>
		///		Gets/Sets the direction of the emitter.
		/// </summary>
		/// <remarks>
		///		Most emitters will have a base direction in which they emit particles (those which
		///		emit in all directions will ignore this parameter). They may not emit exactly along this
		///		vector for every particle, many will introduce a random scatter around this vector using 
		///		the angle property.
		/// </remarks>
		virtual public Vector3 Direction
		{
			get { return direction; }
			set 
			{
				direction = value;
				direction.Normalize();

				// generate an up vector
				up = direction.Perpendicular();
				up.Normalize();
			}
		}

		/// <summary>
		///		Gets/Sets the maximum angle away from the emitter direction which particle will be emitted.
		/// </summary>
		/// <remarks>
		///		Whilst the direction property defines the general direction of emission for particles, 
		///		this property defines how far the emission angle can deviate away from this base direction.
		///		This allows you to create a scatter effect - if set to 0, all particles will be emitted
		///		exactly along the emitters direction vector, wheras if you set it to 180 or more, particles
		///		will be emitted in a sphere, i.e. in all directions.
		/// </remarks>
		virtual public float Angle
		{
			get { return MathUtil.RadiansToDegrees(angle); }
			set { angle = MathUtil.DegreesToRadians(value); }
		}

		/// <summary>
		///		Gets/Sets the initial velocity of particles emitted.
		/// </summary>
		/// <remarks>
		///		This property sets the range of starting speeds for emitted particles. 
		///		See the alternate Min/Max properties for velocities.  This emitter will randomly 
		///		choose a speed between the minimum and maximum for each particle.
		/// </remarks>
		virtual public float ParticleVelocity
		{
			get { return minSpeed; }
			set { minSpeed = maxSpeed = value; }
		}

		/// <summary>
		///		Gets/Sets the minimum velocity of particles emitted.
		/// </summary>
		virtual public float MinParticleVelocity
		{
			get { return minSpeed; }
			set { minSpeed = value; }
		}

		/// <summary>
		///		Gets/Sets the maximum velocity of particles emitted.
		/// </summary>
		virtual public float MaxParticleVelocity
		{
			get { return maxSpeed; }
			set { maxSpeed = value; }
		}

		/// <summary>
		///		Gets/Sets the emission rate for this emitter.
		/// </summary>
		/// <remarks>
		///		This tells the emitter how many particles per second should be emitted. The emitter
		///		subclass does not have to emit these in a continuous burst - this is a relative parameter
		///		and the emitter may choose to emit all of the second's worth of particles every half-second
		///		for example. This is controlled by the emitter's EmissionCount property.
		/// </remarks>
		virtual public float EmissionRate
		{
			get { return emissionRate; }
			set { emissionRate = value; }
		}

		/// <summary>
		///		Gets/Sets the lifetime of all particles emitted.
		/// </summary>
		/// <remarks>
		///		The emitter initializes particles with a time-to-live (TTL), the number of seconds a particle
		///		will exist before being destroyed. This method sets a constant TTL for all particles emitted.
		///		Note that affectors are able to modify the TTL of particles later.
		///		<p/>
		///		Also see the alternate Min/Max versions of this property which takes a min and max TTL in order to 
		///		have the TTL vary per particle.
		/// </remarks>
		virtual public float TimeToLive
		{
			get { return minTTL; }
			set { minTTL = maxTTL = value; }
		}

		/// <summary>
		///		Gets/Sets the minimum time each particle will live for.
		/// </summary>
		virtual public float MinTimeToLive
		{
			get { return minTTL; }
			set { minTTL = value; }
		}

		/// <summary>
		///		Gets/Sets the maximum time each particle will live for.
		/// </summary>
		virtual public float MaxTimeToLive
		{
			get { return maxTTL; }
			set { maxTTL = value; }
		}

		/// <summary>
		///		Gets/Sets the initial color of particles emitted.
		/// </summary>
		/// <remarks>
		///		Particles have an initial color on emission which the emitter sets. This property sets
		///		this color. See the alternate Start/End versions of this property which takes 2 colous in order to establish 
		///		a range of colors to be assigned to particles.
		/// </remarks>
		virtual public ColorEx Color
		{
			get { return colorRangeStart; }
			set { colorRangeStart = colorRangeEnd = value; }
		}

		/// <summary>
		///		Gets/Sets the color that a particle starts out when it is created.
		/// </summary>
		virtual public ColorEx ColorRangeStart
		{
			get { return colorRangeStart; }
			set { colorRangeStart = value; }
		}

		/// <summary>
		///		Gets/Sets the color that a particle ends at just before it's TTL expires.
		/// </summary>
		virtual public ColorEx ColorRangeEnd
		{
			get { return colorRangeEnd; }
			set { colorRangeEnd = value; }
		}

		/// <summary>
		///		Gets the name of the type of emitter. 
		/// </summary>
		public String Type
		{
			get { return type; }
			set { type = value; }
		}

		/// <summary>
		///		Gets/Sets the flag indicating if this emitter is enabled or not.
		/// </summary>
		/// <remarks>
		///		Setting this property to false will turn the emitter off completely.
		/// </remarks>
		virtual public bool IsEnabled
		{
			get { return isEnabled; }
			set 
			{ 
				isEnabled = value; 
				if (isEnabled)
				{
					// Reset duration 
					if (durationMin == durationMax)
					{
						durationRemain = durationMin;
					}
					else
					{
						durationRemain =  MathUtil.UnitRandom() * (durationMax - durationMin);
					}
				}
				else
				{
					// Reset repeat
					if (repeatDelayMin == repeatDelayMax)
					{
						repeatDelayRemain = repeatDelayMin;
					}
					else
					{
						repeatDelayRemain = MathUtil.UnitRandom() * (repeatDelayMax - repeatDelayMin);
					}
				}
			}
		}

		/// <summary>
		///		Gets/Sets the start time of this emitter.
		/// </summary>
		/// <remarks>
		///		By default an emitter starts straight away as soon as a ParticleSystem is first created,
		///		or also just after it is re-enabled. This parameter allows you to set a time delay so
		///		that the emitter does not 'kick in' until later.
		/// </remarks>
		virtual public float StartTime
		{
			get { return startTime; }
			set { startTime = value; }
		}

		/// <summary>
		///		Gets/Sets the duration of time (in seconds) that the emitter should run.
		/// </summary>
		/// <remarks>
		///		By default emitters run indefinitely (unless you manually disable them). By setting this
		///		parameter, you can make an emitter turn off on it's own after a set number of seconds. It
		///		will then remain disabled until either Enabled is set to true, or if the 'repeatAfter' parameter
		///		has been set it will also repeat after a number of seconds.
		///		<p/>
		///		Also see the alternative Min/Max versions of this property which allows you to set a min and max duration for
		///		a random variable duration.
		/// </remarks>
		virtual public float Duration
		{
			get { return durationMin; }
			set { durationMin = durationMax = value; }
		}

		/// <summary>
		///		Gets/Sets the minimum running time of this emitter.
		/// </summary>
		virtual public float MinDuration
		{
			get { return durationMin; }
			set { durationMin = value; }
		}

		/// <summary>
		///		Gets/Sets the maximum running time of this emitter.
		/// </summary>
		virtual public float MaxDuration
		{
			get { return durationMax; }
			set { durationMax = value; }
		}

		/// <summary>
		///		Gets/Sets the time between repeats of the emitter.
		/// </summary>
		virtual public float RepeatDelay
		{
			get { return repeatDelayMin; }
			set { repeatDelayMin = repeatDelayMax = value; }
		}

		/// <summary>
		///		Gets/Sets the minimum repeat delay for the emitter.
		/// </summary>
		virtual public float MinRepeatDelay
		{
			get { return repeatDelayMin; }
			set { repeatDelayMin = value; }
		}

		/// <summary>
		///		Gets/Sets the maximum repeat delay for the emitter.
		/// </summary>
		virtual public float MaxRepeatDelay
		{
			get { return repeatDelayMax; }
			set { repeatDelayMax = value; }
		}

		#endregion

		#region Methods

		/// <summary>
		///		Gets the number of particles which this emitter would like to emit based on the time elapsed.
		///	 </summary>
		///	 <remarks>
		///		For efficiency the emitter does not actually create new Particle instances (these are reused
		///		by the ParticleSystem as existing particles 'die'). The implementation for this method must
		///		return the number of particles the emitter would like to emit given the number of seconds which
		///		have elapsed (passed in as a parameter).
		///		<p/>
		///		Based on the return value from this method, the ParticleSystem class will call 
		///		InitParticle once for each particle it chooses to allow to be emitted by this emitter.
		///		The emitter should not track these InitParticle calls, it should assume all emissions
		///		requested were made (even if they could not be because of particle quotas).
		///	 </remarks>
		/// <param name="timeElapsed"></param>
		/// <returns></returns>
		abstract public ushort GetEmissionCount(float timeElapsed);

		/// <summary>
		///		Initializes a particle based on the emitter's approach and parameters.
       ///	</summary>
       ///	<remarks>
		///		See the GetEmissionCount method for details of why there is a separation between
		///		'requested' emissions and actual initialized particles.
		/// </remarks>
		/// <param name="particle">Reference to a particle which must be initialized based on how this emitter starts particles</param>
		abstract public void InitParticle(Particle particle);

		/// <summary>
		///		Utility method for generating particle exit direction
		/// </summary>
		/// <param name="dest">Normalized vector dictating new direction.</param>
		virtual protected void GenerateEmissionDirection(ref Vector3 dest)
		{
			if(angle != 0.0f)
			{
				float tempAngle = MathUtil.UnitRandom() * angle;

				// randomize direction
				dest = direction.RandomDeviant(tempAngle, up);
			}
			else
			{
				// constant angle
				dest = direction;
			}
		}

		/// <summary>
		///		Utility method to applu velocity to a particle direction.
		/// </summary>
		/// <param name="dest">The normalized vector to scale by a randomly generated scale between min and max speed.</param>
		virtual protected void GenerateEmissionVelocity(ref Vector3 dest)
		{
			float scalar;

			if (minSpeed != maxSpeed)
			{
				scalar = minSpeed + (MathUtil.UnitRandom() * (maxSpeed - minSpeed));
			}
			else
			{
				scalar = minSpeed;
			}

			dest *= scalar;
		}

		/// <summary>
		///		Utility method for generating a time-to-live for a particle.
		/// </summary>
		/// <returns></returns>
		virtual protected float GenerateEmissionTTL()
		{
			if (maxTTL != minTTL)
			{
				return minTTL + (MathUtil.UnitRandom() * (maxTTL - minTTL));
			}
			else
			{
				return minTTL;
			}
		}

		/// <summary>
		///		Utility method for generating an emission count based on a constant emission rate.
		/// </summary>
		/// <param name="timeElapsed"></param>
		/// <returns></returns>
		virtual public ushort GenerateConstantEmissionCount(float timeElapsed)
		{

			ushort intRequest;
	        
			if (isEnabled)
			{
				// Keep fractions, otherwise a high frame rate will result in zero emissions!
				remainder += emissionRate * timeElapsed;
				intRequest = (ushort)remainder;
				remainder -= intRequest;

				// Check duration
				if (durationMax > 0.0f)
				{
					durationRemain -= timeElapsed;
					if (durationRemain <= 0.0f) 
					{
						// Disable, duration is out (takes effect next time)
						this.IsEnabled =false;
					}
				}
				return intRequest;
			}
			else
			{
				// Check repeat
				if (repeatDelayMax > 0.0f)
				{
					repeatDelayRemain -= timeElapsed;
					if (repeatDelayRemain <= 0.0f)
					{
						// Enable, repeat delay is out (takes effect next time)
						this.IsEnabled = true;
					}
				}
				return 0;
			}
		}
		
		/// <summary>
		///		Internal method for generating a color for a particle.
		/// </summary>
		/// <param name="color"></param>
		virtual protected void GenerateEmissionColor(ColorEx color)
		{
			if (colorRangeStart != colorRangeEnd)
			{
				color.r = colorRangeStart.r + MathUtil.UnitRandom() * (colorRangeEnd.r - colorRangeStart.r);
				color.g = colorRangeStart.g + MathUtil.UnitRandom() * (colorRangeEnd.g - colorRangeStart.g);
				color.b = colorRangeStart.b + MathUtil.UnitRandom() * (colorRangeEnd.b - colorRangeStart.b);
				color.a = colorRangeStart.a + MathUtil.UnitRandom() * (colorRangeEnd.a - colorRangeStart.a);
			}
			else
			{
				color.r = colorRangeStart.r;
				color.g = colorRangeStart.g;
				color.b = colorRangeStart.b;
				color.a = colorRangeStart.a;
			}
		}

		virtual public void CopyTo(ParticleEmitter emitter)
		{
			PropertyInfo[] props = this.GetType().GetProperties();

			for(int i = 0; i < props.Length; i++)
			{
				PropertyInfo prop = props[i];

				// if the prop is not settable, then skip
				if(!prop.CanWrite || !prop.CanRead) 
				{
					Console.WriteLine(prop.Name);
					continue;
				}

				object srcVal = prop.GetValue(this, null);
				prop.SetValue(emitter, srcVal, null);
			}
			/*
			emitter.Angle = this.Angle;
			emitter.ColorRangeStart = this.ColorRangeStart;
			emitter.ColorRangeEnd = this.ColorRangeEnd;
			emitter.Direction = this.Direction;
			emitter.MinDuration = this.MinDuration;
			emitter.MaxDuration = this.MaxDuration;
			emitter.*/
		}

		#endregion
	}
}