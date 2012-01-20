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
//     <id value="$Id:"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;

using Axiom.Core;
using Axiom.Graphics;
using Axiom.Math;
using Axiom.ParticleSystems;
using Axiom.Scripting;

#endregion Namespace Declarations

namespace Axiom.ParticleFX
{
	/// <summary>
	/// This class defines a ParticleAffector which applies randomness to the movement of the particles.
	/// <remarks>
	/// This affector <see cref="ParticleAffector"/> applies randomness to the movement of the particles by
	///	changing the direction vectors.
	/// </remarks>
	/// </summary>
	public class DirectionRandomizerAffector : ParticleAffector
	{
		private float _randomness;
		/// <summary>
		///
		/// </summary>
		public float Randomness
		{
			get
			{
				return _randomness;
			}
			set
			{
				_randomness = value;
			}
		}

		private float _scope;
		/// <summary>
		///
		/// </summary>
		public float Scope
		{
			get
			{
				return _scope;
			}
			set
			{
				_scope = value;
			}
		}

		private bool _keepVelocity;

		/// <summary>
		///
		/// </summary>
		public bool KeepVelocity
		{
			get
			{
				return _keepVelocity;
			}
			set
			{
				_keepVelocity = value;
			}
		}

        public DirectionRandomizerAffector( ParticleSystem psys )
            : base( psys )
        {
            this.type = "DirectionRandomizer";

			// defaults
			_randomness = 1.0f;
			_scope = 1.0f;
			_keepVelocity = false;
		}

		/// <summary>
		///		Method called to allow the affector to 'do it's stuff' on all active particles in the system.
		/// </summary>
		/// <remarks>
		///		This is where the affector gets the chance to apply it's effects to the particles of a system.
		///		The affector is expected to apply it's effect to some or all of the particles in the system
		///		passed to it, depending on the affector's approach.
		/// </remarks>
		/// <param name="system">Reference to a ParticleSystem to affect.</param>
		/// <param name="timeElapsed">The number of seconds which have elapsed since the last call.</param>
		public override void AffectParticles( ParticleSystem system, Real timeElapsed )
		{
			float length = 0.0f;

			foreach ( Particle p in system.Particles )
			{
				if ( _scope > Utility.UnitRandom() )
				{
					if ( !p.Direction.IsZeroLength )
					{
						if ( _keepVelocity )
						{
							length = p.Direction.Length;
						}

						p.Direction += new Vector3( Utility.RangeRandom( -_randomness, _randomness ) * timeElapsed,
													Utility.RangeRandom( -_randomness, _randomness ) * timeElapsed,
													Utility.RangeRandom( -_randomness, _randomness ) * timeElapsed );

						if ( _keepVelocity )
						{
							p.Direction *= length / p.Direction.Length;
						}
					}
				}
			}
		}

		#region Command definition classes

		[ScriptableProperty( "randomness", "The amount of randomness (chaos) to apply to the particle movement.", typeof( ParticleAffector ) )]
        public class RandomnessCommand : IPropertyCommand
		{
			public string Get( object target )
			{
				DirectionRandomizerAffector affector = target as DirectionRandomizerAffector;
				return StringConverter.ToString( affector.Randomness );
			}

			public void Set( object target, string val )
			{
				DirectionRandomizerAffector affector = target as DirectionRandomizerAffector;
				affector.Randomness = StringConverter.ParseFloat( val );
			}
		}

		[ScriptableProperty( "scope", "The percentage of particles which is affected.", typeof( ParticleAffector ) )]
        public class ScopeCommand : IPropertyCommand
		{
			public string Get( object target )
			{
				DirectionRandomizerAffector affector = target as DirectionRandomizerAffector;
				return StringConverter.ToString( affector.Scope );
			}

			public void Set( object target, string val )
			{
				DirectionRandomizerAffector affector = target as DirectionRandomizerAffector;
				affector.Scope = StringConverter.ParseFloat( val );
			}
		}

		[ScriptableProperty( "keep_velocity", "Detemines whether the velocity of the particles is changed.", typeof( ParticleAffector ) )]
        public class KeepVelocityCommand : IPropertyCommand
		{
			public string Get( object target )
			{
				DirectionRandomizerAffector affector = target as DirectionRandomizerAffector;
				return affector.KeepVelocity.ToString();
			}

			public void Set( object target, string val )
			{
				DirectionRandomizerAffector affector = target as DirectionRandomizerAffector;
				affector.KeepVelocity = StringConverter.ParseBool( val );
			}
		}

		#endregion Command definition classes
	}
}