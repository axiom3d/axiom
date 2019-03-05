#region LGPL License

/*
Axiom Graphics Engine Library
Copyright © 2003-2011 Axiom Project Team

The overall design, and a majority of the core engine and rendering code
contained within this library is a derivative of the open source Object Oriented
Graphics Engine OGRE, which can be found at http://ogre.sourceforge.net.
Many thanks to the OGRE team for maintaining such a high quality project.

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

#endregion LGPL License

#region SVN Version Information

// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections;
using Axiom.Collections;
using Axiom.Core;
using Axiom.Math;
using Axiom.Scripting;
using System.Reflection;
using static Axiom.Math.Utility;

#endregion Namespace Declarations

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
	public abstract class ParticleEmitter : Particle
	{
		#region Fields

		///<summary>
		///    Rate in particles per second at which this emitter wishes to emit particles.
		/// </summary>
		protected float emissionRate;

		/// <summary>
		/// The name of the emitter to be emitted (optional)
		/// </summary>
		protected string emittedEmitter = string.Empty;

		/// <summary>
		/// If 'true', this emitter is emitted by another emitter.
		/// NB. That doesn´t imply that the emitter itself emits other emitters (that could or could not be the case)
		/// </summary>
		protected bool emitted;

		/// <summary>
		///    Name of the type of emitter, MUST be initialized by subclasses.
		/// </summary>
		protected string type;

		/// <summary>
		///    Notional up vector, just used to speed up generation of variant directions.
		/// </summary>
		protected Vector3 up;

		/// <summary>
		///    Angle around direction which particles may be emitted, internally radians but degrees for interface.
		/// </summary>
		protected float angle;

		/// <summary>
		///    Fixed speed of particles.
		/// </summary>
		protected float fixedSpeed;

		/// <summary>
		///    Min speed of particles.
		/// </summary>
		protected float minSpeed;

		/// <summary>
		///    Max speed of particles.
		/// </summary>
		protected float maxSpeed;

		/// <summary>
		///    Initial time-to-live of particles (fixed).
		/// </summary>
		protected float fixedTTL;

		/// <summary>
		///    Initial time-to-live of particles (min).
		/// </summary>
		protected float minTTL;

		/// <summary>
		///    Initial time-to-live of particles (max).
		/// </summary>
		protected float maxTTL;

		/// <summary>
		///    Initial color of particles (fixed).
		/// </summary>
		protected ColorEx colorFixed;

		/// <summary>
		///    Initial color of particles (range start).
		/// </summary>
		protected ColorEx colorRangeStart;

		/// <summary>
		///    Initial color of particles (range end).
		/// </summary>
		protected ColorEx colorRangeEnd;

		/// <summary>
		///    Whether this emitter is currently enabled (defaults to true).
		/// </summary>
		protected bool isEnabled;

		/// <summary>
		///    Start time (in seconds from start of first call to ParticleSystem to update).
		/// </summary>
		protected float startTime;

		/// <summary>
		///    Length of time emitter will run for (0 = forever).
		/// </summary>
		protected float durationFixed;

		/// <summary>
		///    Minimum length of time emitter will run for (0 = forever).
		/// </summary>
		protected float durationMin;

		/// <summary>
		///    Maximum length of time the emitter will run for (0 = forever).
		/// </summary>
		protected float durationMax;

		/// <summary>
		///    Current duration remainder.
		/// </summary>
		protected float durationRemain;

		/// <summary>
		///    Fixed time between each repeat.
		/// </summary>
		protected float repeatDelayFixed;

		/// <summary>
		///    Minimum time between each repeat.
		/// </summary>
		protected float repeatDelayMin;

		/// <summary>
		///    Maximum time between each repeat.
		/// </summary>
		protected float repeatDelayMax;

		/// <summary>
		///    Repeat delay left.
		/// </summary>
		protected float repeatDelayRemain;

		private string _name = string.Empty;

		public string Name
		{
			get
			{
				return this._name;
			}
			set
			{
				this._name = value;
			}
		}


		protected float remainder = 0;

		protected AxiomCollection<IPropertyCommand> commandTable = new AxiomCollection<IPropertyCommand>();

		#endregion Fields

		#region Constructors

		/// <summary>
		///		Default constructor.
		/// </summary>
		public ParticleEmitter( ParticleSystem ps )
		{
			// set defaults
			parentSystem = ps;
			this.angle = 0.0f;
			Direction = Vector3.UnitX;
			this.emissionRate = 10;
			this.fixedSpeed = 1;
			this.minSpeed = float.NaN;
			this.fixedTTL = 5;
			this.minTTL = float.NaN;
			this.Position = Vector3.Zero;
			this.colorFixed = ColorEx.White;
			this.isEnabled = true;
			this.durationFixed = 0;
			this.durationMin = float.NaN;
			this.repeatDelayFixed = 0;
			this.repeatDelayMin = float.NaN;

			RegisterCommands();
		}

		#endregion Constructors

		#region Properties

		/// <summary>
		///		Gets/Sets the direction of the emitter.
		/// </summary>
		/// <remarks>
		///		Most emitters will have a base direction in which they emit particles (those which
		///		emit in all directions will ignore this parameter). They may not emit exactly along this
		///		vector for every particle, many will introduce a random scatter around this vector using
		///		the angle property.
		/// </remarks>
		public new virtual Vector3 Direction
		{
			get
			{
				return base.Direction;
			}
			set
			{
				base.Direction = value;
				base.Direction.Normalize();

				// generate an up vector
				this.up = base.Direction.Perpendicular();
				this.up.Normalize();
			}
		}

		/// <summary>
		///		Gets/Sets the maximum angle away from the emitter direction which particle will be emitted.
		/// </summary>
		/// <remarks>
		///		Whilst the direction property defines the general direction of emission for particles, 
		///		this property defines how far the emission angle can deviate away from this base direction.
		///		This allows you to create a scatter effect - if set to 0, all particles will be emitted
		///		exactly along the emitters direction vector, whereas if you set it to 180 or more, particles
		///		will be emitted in a sphere, i.e. in all directions.
		/// </remarks>
		public virtual float Angle
		{
			get
			{
				return RadiansToDegrees( (Real)this.angle );
			}
			set
			{
				this.angle = DegreesToRadians( (Real)value );
			}
		}

		/// <summary>
		///		Gets/Sets the initial velocity of particles emitted.
		/// </summary>
		/// <remarks>
		///		This property sets the range of starting speeds for emitted particles.
		///		See the alternate Min/Max properties for velocities.  This emitter will randomly
		///		choose a speed between the minimum and maximum for each particle.
		/// </remarks>
		public virtual float ParticleVelocity
		{
			get
			{
				return float.IsNaN( this.minSpeed ) ? this.fixedSpeed : float.NaN;
			}
			set
			{
				this.fixedSpeed = value;
			}
		}

		/// <summary>
		///		Gets/Sets the minimum velocity of particles emitted.
		/// </summary>
		public virtual float MinParticleVelocity
		{
			get
			{
				return this.minSpeed;
			}
			set
			{
				this.minSpeed = value;
			}
		}

		/// <summary>
		///		Gets/Sets the maximum velocity of particles emitted.
		/// </summary>
		public virtual float MaxParticleVelocity
		{
			get
			{
				return this.maxSpeed;
			}
			set
			{
				this.maxSpeed = value;
			}
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
		public virtual float EmissionRate
		{
			get
			{
				return this.emissionRate;
			}
			set
			{
				this.emissionRate = value;
			}
		}

		/// <summary>
		///
		/// </summary>
		public virtual string EmittedEmitter
		{
			get
			{
				return this.emittedEmitter;
			}
			set
			{
				this.emittedEmitter = value;
			}
		}

		/// <summary>
		/// If 'true', this emitter is emitted by another emitter.
		/// NB. That doesn´t imply that the emitter itself emits other emitters (that could or could not be the case)
		/// </summary>
		public virtual bool IsEmitted
		{
			get
			{
				return this.emitted;
			}
			set
			{
				this.emitted = value;
			}
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
		public virtual float TimeToLive
		{
			get
			{
				return float.IsNaN( this.minTTL ) ? this.fixedTTL : float.NaN;
			}
			set
			{
				this.fixedTTL = value;
			}
		}

		/// <summary>
		///		Gets/Sets the minimum time each particle will live for.
		/// </summary>
		public virtual float MinTimeToLive
		{
			get
			{
				return this.minTTL;
			}
			set
			{
				this.minTTL = value;
			}
		}

		/// <summary>
		///		Gets/Sets the maximum time each particle will live for.
		/// </summary>
		public virtual float MaxTimeToLive
		{
			get
			{
				return this.maxTTL;
			}
			set
			{
				this.maxTTL = value;
			}
		}

		/// <summary>
		///		Gets/Sets the initial color of particles emitted.
		/// </summary>
		/// <remarks>
		///		Particles have an initial color on emission which the emitter sets. This property sets
		///		this color. See the alternate Start/End versions of this property which takes 2 colors in order to establish
		///		a range of colors to be assigned to particles.
		/// </remarks>
		public new virtual ColorEx Color
		{
			get
			{
				return this.colorRangeStart;
			}
			set
			{
				this.colorFixed = value;
			}
		}

		/// <summary>
		///		Gets/Sets the color that a particle starts out when it is created.
		/// </summary>
		public virtual ColorEx ColorRangeStart
		{
			get
			{
				return this.colorRangeStart;
			}
			set
			{
				this.colorRangeStart = value;
			}
		}

		/// <summary>
		///		Gets/Sets the color that a particle ends at just before it's TTL expires.
		/// </summary>
		public virtual ColorEx ColorRangeEnd
		{
			get
			{
				return this.colorRangeEnd;
			}
			set
			{
				this.colorRangeEnd = value;
			}
		}

		/// <summary>
		///		Gets the name of the type of emitter.
		/// </summary>
		public string Type
		{
			get
			{
				return this.type;
			}
			set
			{
				this.type = value;
			}
		}

		/// <summary>
		///		Gets/Sets the flag indicating if this emitter is enabled or not.
		/// </summary>
		/// <remarks>
		///		Setting this property to false will turn the emitter off completely.
		/// </remarks>
		public virtual bool IsEnabled
		{
			get
			{
				return this.isEnabled;
			}
			set
			{
				this.isEnabled = value;
				InitDurationRepeat();
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
		public virtual float StartTime
		{
			get
			{
				return this.startTime;
			}
			set
			{
				IsEnabled = false;
				this.startTime = value;
			}
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
		public virtual float Duration
		{
			get
			{
				return float.IsNaN( this.durationMin ) ? this.durationFixed : float.NaN;
			}
			set
			{
				this.durationFixed = value;
				InitDurationRepeat();
			}
		}

		/// <summary>
		///		Gets/Sets the minimum running time of this emitter.
		/// </summary>
		public virtual float MinDuration
		{
			get
			{
				return this.durationMin;
			}
			set
			{
				this.durationMin = value;
				InitDurationRepeat();
			}
		}

		/// <summary>
		///		Gets/Sets the maximum running time of this emitter.
		/// </summary>
		public virtual float MaxDuration
		{
			get
			{
				return this.durationMax;
			}
			set
			{
				this.durationMax = value;
				InitDurationRepeat();
			}
		}

		/// <summary>
		///		Gets/Sets the maximum repeat delay for the emitter.
		/// </summary>
		public virtual float MaxRepeatDelay
		{
			get
			{
				return this.repeatDelayMax;
			}
			set
			{
				this.repeatDelayMax = value;
				InitDurationRepeat();
			}
		}

		/// <summary>
		///		Gets/Sets the minimum repeat delay for the emitter.
		/// </summary>
		public virtual float MinRepeatDelay
		{
			get
			{
				return this.repeatDelayMin;
			}
			set
			{
				this.repeatDelayMin = value;
				InitDurationRepeat();
			}
		}

		/// <summary>
		///		Gets/Sets the time between repeats of the emitter.
		/// </summary>
		public virtual float RepeatDelay
		{
			get
			{
				return float.IsNaN( this.repeatDelayMin ) ? this.repeatDelayFixed : float.NaN;
			}
			set
			{
				this.repeatDelayFixed = value;
				InitDurationRepeat();
			}
		}

		#endregion Properties

		#region Methods

		public void Move( float x, float y, float z )
		{
			Position += new Vector3( x, y, z );
		}

		public void MoveTo( float x, float y, float z )
		{
			Position = new Vector3( x, y, z );
		}

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
		public abstract ushort GetEmissionCount( float timeElapsed );

		/// <summary>
		///		Initializes a particle based on the emitter's approach and parameters.
		///	</summary>
		///	<remarks>
		///		See the GetEmissionCount method for details of why there is a separation between
		///		'requested' emissions and actual initialized particles.
		/// </remarks>
		/// <param name="particle">Reference to a particle which must be initialized based on how this emitter starts particles</param>
		public virtual void InitParticle( Particle particle )
		{
			particle.ResetDimensions();
		}

		/// <summary>
		///		Utility method for generating particle exit direction
		/// </summary>
		/// <param name="dest">Normalized vector dictating new direction.</param>
		protected virtual void GenerateEmissionDirection( ref Vector3 dest )
		{
			if ( this.angle != 0.0f )
			{
				float tempAngle = UnitRandom()*this.angle;

				// randomize direction
				dest = this.Direction.RandomDeviant( tempAngle, this.up );
			}
			else
			{
				// constant angle
				dest = this.Direction;
			}
		}

		/// <summary>
		///		Utility method to apply velocity to a particle direction.
		/// </summary>
		/// <param name="dest">The normalized vector to scale by a randomly generated scale between min and max speed.</param>
		protected virtual void GenerateEmissionVelocity( ref Vector3 dest )
		{
			float scalar;

			if ( !float.IsNaN( this.minSpeed ) )
			{
				scalar = this.minSpeed + ( UnitRandom()*( this.maxSpeed - this.minSpeed ) );
			}
			else
			{
				scalar = this.fixedSpeed;
			}

			dest *= scalar;
		}

		/// <summary>
		///		Utility method for generating a time-to-live for a particle.
		/// </summary>
		/// <returns></returns>
		protected virtual float GenerateEmissionTTL()
		{
			if ( !float.IsNaN( this.minTTL ) )
			{
				return this.minTTL + ( UnitRandom()*( this.maxTTL - this.minTTL ) );
			}
			else
			{
				return this.fixedTTL;
			}
		}

		/// <summary>
		///		Utility method for generating an emission count based on a constant emission rate.
		/// </summary>
		/// <param name="timeElapsed"></param>
		/// <returns></returns>
		public virtual ushort GenerateConstantEmissionCount( float timeElapsed )
		{
			ushort intRequest;
			var durMax = float.IsNaN( this.durationMin ) ? this.durationFixed : this.durationMax;
			var repDelMax = float.IsNaN( this.repeatDelayMin ) ? this.repeatDelayFixed : this.repeatDelayMax;

			if ( this.isEnabled )
			{
				// Keep fractions, otherwise a high frame rate will result in zero emissions!
				this.remainder += this.emissionRate*timeElapsed;
				intRequest = (ushort)this.remainder;
				this.remainder -= intRequest;

				// Check duration
				if ( durMax > 0.0f )
				{
					this.durationRemain -= timeElapsed;
					if ( this.durationRemain <= 0.0f )
					{
						// Disable, duration is out (takes effect next time)
						IsEnabled = false;
					}
				}
				return intRequest;
			}
			else
			{
				// Check repeat
				if ( repDelMax > 0.0f )
				{
					this.repeatDelayRemain -= timeElapsed;
					if ( this.repeatDelayRemain <= 0.0f )
					{
						// Enable, repeat delay is out (takes effect next time)
						IsEnabled = true;
					}
				}
				if ( this.startTime > 0.0f )
				{
					this.startTime -= timeElapsed;

					if ( this.startTime <= 0.0f )
					{
						IsEnabled = true;
						this.startTime = 0;
					}
				}

				return 0;
			}
		}

		/// <summary>
		///		Internal method for generating a color for a particle.
		/// </summary>
		/// <param name="color">
		///    The color object that will be altered depending on the method of generating the particle color.
		/// </param>
		protected virtual void GenerateEmissionColor( ref ColorEx color )
		{
			if ( this.colorRangeStart != this.ColorRangeEnd )
			{
				color.r = this.colorRangeStart.r + UnitRandom() * ( this.colorRangeEnd.r - this.colorRangeStart.r );
				color.g = this.colorRangeStart.g + UnitRandom() * ( this.colorRangeEnd.g - this.colorRangeStart.g );
				color.b = this.colorRangeStart.b + UnitRandom() * ( this.colorRangeEnd.b - this.colorRangeStart.b );
				color.a = this.colorRangeStart.a + UnitRandom() * ( this.colorRangeEnd.a - this.colorRangeStart.a );
			}
			else
			{
				color.r = this.colorFixed.r;
				color.g = this.colorFixed.g;
				color.b = this.colorFixed.b;
				color.a = this.colorFixed.a;
			}
		}

		/// <summary>
		///
		/// </summary>
		protected void InitDurationRepeat()
		{
			if ( this.isEnabled )
			{
				if ( float.IsNaN( this.durationMin ) )
				{
					this.durationRemain = this.durationFixed;
				}
				else
				{
					this.durationRemain = RangeRandom( this.durationMin, this.durationMax );
				}
			}
			else
			{
				// reset repeat
				if ( float.IsNaN( this.repeatDelayMin ) )
				{
					this.repeatDelayRemain = this.repeatDelayFixed;
				}
				else
				{
					this.repeatDelayRemain = RangeRandom( this.repeatDelayMin, this.repeatDelayMax );
				}
			}
		}

		/// <summary>
		///    Sets the min/max duration range for this emitter.
		/// </summary>
		/// <param name="min"></param>
		/// <param name="max"></param>
		public void SetDuration( float min, float max )
		{
			this.durationMin = min;
			this.durationMax = max;
			InitDurationRepeat();
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="emitter"></param>
		public virtual void CopyTo( ParticleEmitter emitter )
		{
			// loop through all registered commands and copy from this instance to the target instance
			foreach ( var key in this.commandTable.Keys )
			{
				// get the value of the param from this instance
				var val = ( (IPropertyCommand)this.commandTable[ key ] ).Get( this );

				// set the param on the target instance
				emitter.SetParam( key, val );
			}
		}

		/// <summary>
		///		Scales the velocity of the emitters by the float argument
		/// </summary>
		public void ScaleVelocity( float velocityMultiplier )
		{
			this.minSpeed *= velocityMultiplier;
			this.maxSpeed *= velocityMultiplier;
		}

		#endregion Methods

		#region Script parser methods

		public bool SetParam( string name, string val )
		{
			if ( this.commandTable.ContainsKey( name ) )
			{
				var command = (IPropertyCommand)this.commandTable[ name ];

				command.Set( this, val );

				return true;
			}
			else
			{
				return false;
			}
		}

		/// <summary>
		///		Registers all attribute names with their respective parser.
		/// </summary>
		/// <remarks>
		///		Methods meant to serve as attribute parsers should use a method attribute to
		/// </remarks>
		protected void RegisterCommands()
		{
			var baseType = GetType();

			do
			{
				var types = baseType.GetNestedTypes( BindingFlags.NonPublic | BindingFlags.Public );

				// loop through all methods and look for ones marked with attributes
				for ( var i = 0; i < types.Length; i++ )
				{
					// get the current method in the loop
					var type = types[ i ];

					// get as many command attributes as there are on this type
					var commandAtts =
						(ScriptablePropertyAttribute[])type.GetCustomAttributes( typeof ( ScriptablePropertyAttribute ), true );

					// loop through each one we found and register its command
					for ( var j = 0; j < commandAtts.Length; j++ )
					{
						var commandAtt = commandAtts[ j ];

						this.commandTable.Add( commandAtt.ScriptPropertyName, (IPropertyCommand)Activator.CreateInstance( type ) );
					} // for
				} // for

				// get the base type of the current type
				baseType = baseType.BaseType;
			}
			while ( baseType != typeof ( object ) );
		}

		#endregion Script parser methods

		#region Command definitions

		/// <summary>
		///
		/// </summary>
		[ScriptableProperty( "angle", "Angle to emit the particles at.", typeof ( ParticleEmitter ) )]
		public class AngleCommand : IPropertyCommand
		{
			public void Set( object target, string val )
			{
				var emitter = target as ParticleEmitter;
				emitter.Angle = StringConverter.ParseFloat( val );
			}

			public string Get( object target )
			{
				var emitter = target as ParticleEmitter;
				return StringConverter.ToString( emitter.Angle );
			}
		}

		/// <summary>
		///
		/// </summary>
		[ScriptableProperty( "position", "Particle emitter position.", typeof ( ParticleEmitter ) )]
		public class PositionCommand : IPropertyCommand
		{
			public void Set( object target, string val )
			{
				var emitter = target as ParticleEmitter;
				emitter.Position = StringConverter.ParseVector3( val );
			}

			public string Get( object target )
			{
				var emitter = target as ParticleEmitter;
				return StringConverter.ToString( emitter.Position );
			}
		}

		/// <summary>
		///
		/// </summary>
		[ScriptableProperty( "emission_rate", "Rate of particle emission.", typeof ( ParticleEmitter ) )]
		public class EmissionRateCommand : IPropertyCommand
		{
			public void Set( object target, string val )
			{
				var emitter = target as ParticleEmitter;
				emitter.EmissionRate = StringConverter.ParseFloat( val );
			}

			public string Get( object target )
			{
				var emitter = target as ParticleEmitter;
				return StringConverter.ToString( emitter.EmissionRate );
			}
		}

		[ScriptableProperty( "emit_emitter", "If set, this emitter will emit other emitters instead of visual particles.",
			typeof ( ParticleEmitter ) )]
		public class EmitEmitterCommand : IPropertyCommand
		{
			public void Set( object target, string val )
			{
				var emitter = target as ParticleEmitter;
				emitter.EmittedEmitter = val;
			}

			public string Get( object target )
			{
				var emitter = target as ParticleEmitter;
				return emitter.EmittedEmitter;
			}
		}

		/// <summary>
		///
		/// </summary>
		[ScriptableProperty( "time_to_live", "Constant lifespan of a particle.", typeof ( ParticleEmitter ) )]
		public class TtlCommand : IPropertyCommand
		{
			public void Set( object target, string val )
			{
				var emitter = target as ParticleEmitter;
				emitter.TimeToLive = StringConverter.ParseFloat( val );
			}

			public string Get( object target )
			{
				var emitter = target as ParticleEmitter;
				return StringConverter.ToString( emitter.TimeToLive );
			}
		}

		/// <summary>
		///
		/// </summary>
		[ScriptableProperty( "time_to_live_min", "Minimum lifespan of a particle.", typeof ( ParticleEmitter ) )]
		public class TtlMinCommand : IPropertyCommand
		{
			public void Set( object target, string val )
			{
				var emitter = target as ParticleEmitter;
				emitter.MinTimeToLive = StringConverter.ParseFloat( val );
			}

			public string Get( object target )
			{
				var emitter = target as ParticleEmitter;
				return StringConverter.ToString( emitter.MinTimeToLive );
			}
		}

		/// <summary>
		///
		/// </summary>
		[ScriptableProperty( "time_to_live_max", "Maximum lifespan of a particle.", typeof ( ParticleEmitter ) )]
		public class TtlMaxCommand : IPropertyCommand
		{
			public void Set( object target, string val )
			{
				var emitter = target as ParticleEmitter;
				emitter.MaxTimeToLive = StringConverter.ParseFloat( val );
			}

			public string Get( object target )
			{
				var emitter = target as ParticleEmitter;
				return StringConverter.ToString( emitter.MaxTimeToLive );
			}
		}

		/// <summary>
		///
		/// </summary>
		[ScriptableProperty( "direction", "Particle direction.", typeof ( ParticleEmitter ) )]
		public class DirectionCommand : IPropertyCommand
		{
			public void Set( object target, string val )
			{
				var emitter = target as ParticleEmitter;
				emitter.Direction = StringConverter.ParseVector3( val );
			}

			public string Get( object target )
			{
				var emitter = target as ParticleEmitter;
				return StringConverter.ToString( emitter.Direction );
			}
		}

		/// <summary>
		///
		/// </summary>
		[ScriptableProperty( "duration", "Constant duration.", typeof ( ParticleEmitter ) )]
		public class DurationCommand : IPropertyCommand
		{
			public void Set( object target, string val )
			{
				var emitter = target as ParticleEmitter;
				emitter.Duration = StringConverter.ParseFloat( val );
			}

			public string Get( object target )
			{
				var emitter = target as ParticleEmitter;
				return StringConverter.ToString( emitter.Duration );
			}
		}

		/// <summary>
		///
		/// </summary>
		[ScriptableProperty( "duration_min", "Minimum duration.", typeof ( ParticleEmitter ) )]
		public class MinDurationCommand : IPropertyCommand
		{
			public void Set( object target, string val )
			{
				var emitter = target as ParticleEmitter;
				emitter.MinDuration = StringConverter.ParseFloat( val );
			}

			public string Get( object target )
			{
				var emitter = target as ParticleEmitter;
				return StringConverter.ToString( emitter.MinDuration );
			}
		}

		/// <summary>
		///
		/// </summary>
		[ScriptableProperty( "duration_max", "Maximum duration.", typeof ( ParticleEmitter ) )]
		public class MaxDurationCommand : IPropertyCommand
		{
			public void Set( object target, string val )
			{
				var emitter = target as ParticleEmitter;
				emitter.MaxDuration = StringConverter.ParseFloat( val );
			}

			public string Get( object target )
			{
				var emitter = target as ParticleEmitter;
				return StringConverter.ToString( emitter.MaxDuration );
			}
		}

		/// <summary>
		///
		/// </summary>
		[ScriptableProperty( "repeat_delay", "Constant delay between repeating durations.", typeof ( ParticleEmitter ) )]
		public class RepeatDelayCommand : IPropertyCommand
		{
			public void Set( object target, string val )
			{
				var emitter = target as ParticleEmitter;
				emitter.RepeatDelay = StringConverter.ParseFloat( val );
			}

			public string Get( object target )
			{
				var emitter = target as ParticleEmitter;
				return StringConverter.ToString( emitter.RepeatDelay );
			}
		}

		/// <summary>
		///
		/// </summary>
		[ScriptableProperty( "repeat_delay_min", "Minimum delay between repeating durations.", typeof ( ParticleEmitter ) )]
		public class RepeatDelayMinCommand : IPropertyCommand
		{
			public void Set( object target, string val )
			{
				var emitter = target as ParticleEmitter;
				emitter.MinRepeatDelay = StringConverter.ParseFloat( val );
			}

			public string Get( object target )
			{
				var emitter = target as ParticleEmitter;
				return StringConverter.ToString( emitter.MinRepeatDelay );
			}
		}

		/// <summary>
		///
		/// </summary>
		[ScriptableProperty( "repeat_delay_max", "Maximum delay between repeating durations.", typeof ( ParticleEmitter ) )]
		public class RepeatDelayMaxCommand : IPropertyCommand
		{
			public void Set( object target, string val )
			{
				var emitter = target as ParticleEmitter;
				emitter.MaxRepeatDelay = StringConverter.ParseFloat( val );
			}

			public string Get( object target )
			{
				var emitter = target as ParticleEmitter;
				return StringConverter.ToString( emitter.MaxRepeatDelay );
			}
		}

		/// <summary>
		///
		/// </summary>
		[ScriptableProperty( "velocity", "Constant particle velocity.", typeof ( ParticleEmitter ) )]
		public class VelocityCommand : IPropertyCommand
		{
			public void Set( object target, string val )
			{
				var emitter = target as ParticleEmitter;
				emitter.ParticleVelocity = StringConverter.ParseFloat( val );
			}

			public string Get( object target )
			{
				var emitter = target as ParticleEmitter;
				return StringConverter.ToString( emitter.ParticleVelocity );
			}
		}

		/// <summary>
		///
		/// </summary>
		[ScriptableProperty( "velocity_min", "Minimum particle velocity.", typeof ( ParticleEmitter ) )]
		public class VelocityMinCommand : IPropertyCommand
		{
			public void Set( object target, string val )
			{
				var emitter = target as ParticleEmitter;
				emitter.MinParticleVelocity = StringConverter.ParseFloat( val );
			}

			public string Get( object target )
			{
				var emitter = target as ParticleEmitter;
				return StringConverter.ToString( emitter.MinParticleVelocity );
			}
		}

		/// <summary>
		///
		/// </summary>
		[ScriptableProperty( "velocity_max", "Maximum particle velocity.", typeof ( ParticleEmitter ) )]
		public class VelocityMaxCommand : IPropertyCommand
		{
			public void Set( object target, string val )
			{
				var emitter = target as ParticleEmitter;
				emitter.MaxParticleVelocity = StringConverter.ParseFloat( val );
			}

			public string Get( object target )
			{
				var emitter = target as ParticleEmitter;
				return StringConverter.ToString( emitter.MaxParticleVelocity );
			}
		}

		/// <summary>
		///
		/// </summary>
		[ScriptableProperty( "colour", "Color.", typeof ( ParticleEmitter ) )]
		public class ColorCommand : IPropertyCommand
		{
			public void Set( object target, string val )
			{
				var emitter = target as ParticleEmitter;
				if ( val != null )
				{
					emitter.Color = StringConverter.ParseColor( val );
				}
			}

			public string Get( object target )
			{
				var emitter = target as ParticleEmitter;
				return StringConverter.ToString( emitter.Color );
			}
		}

		/// <summary>
		///
		/// </summary>
		[ScriptableProperty( "colour_range_start", "Color range start.", typeof ( ParticleEmitter ) )]
		public class ColorRangeStartCommand : IPropertyCommand
		{
			public void Set( object target, string val )
			{
				var emitter = target as ParticleEmitter;
				if ( val != null )
				{
					emitter.ColorRangeStart = StringConverter.ParseColor( val );
				}
			}

			public string Get( object target )
			{
				var emitter = target as ParticleEmitter;
				return StringConverter.ToString( emitter.ColorRangeStart );
			}
		}


		/// <summary>
		///
		/// </summary>
		[ScriptableProperty( "colour_range_end", "Color range end.", typeof ( ParticleEmitter ) )]
		public class ColorRangeEndCommand : IPropertyCommand
		{
			public void Set( object target, string val )
			{
				var emitter = target as ParticleEmitter;
				if ( val != null )
				{
					emitter.ColorRangeEnd = StringConverter.ParseColor( val );
				}
			}

			public string Get( object target )
			{
				var emitter = target as ParticleEmitter;
				return StringConverter.ToString( emitter.ColorRangeEnd );
			}
		}

		/// <summary>
		///
		/// </summary>
		[ScriptableProperty( "name", "particle emmitter name.", typeof ( ParticleEmitter ) )]
		public class NameCommand : IPropertyCommand
		{
			public void Set( object target, string val )
			{
				var emitter = target as ParticleEmitter;
				emitter.Name = val;
			}

			public string Get( object target )
			{
				var emitter = target as ParticleEmitter;
				return emitter.Name;
			}
		}

		#endregion Command definitions
	};
}