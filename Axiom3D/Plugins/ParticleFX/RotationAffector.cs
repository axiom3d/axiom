#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2006  Axiom Project Team

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
#endregion

#region Namespace Declarations

using System;

using Axiom;
using DotNet3D.Math;

#endregion Namespace Declarations
			
namespace Axiom.ParticleFX
{
    /// <summary>
    /// Summary description for ScaleAffector.
    /// </summary>
    public class RotationAffector : ParticleAffector
    {
        #region Fields

        /// <summary>
        ///		Initial rotation speed of particles (range start).
        /// </summary>
        Real rotationSpeedRangeStart;
        /// <summary>
        ///		Initial rotation speed of particles (range end).
        /// </summary>
        Real rotationSpeedRangeEnd;
        /// <summary>
        ///		Initial rotation angle of particles (range start).
        /// </summary>
        Real rotationRangeStart;
        /// <summary>
        ///		Initial rotation angle of particles (range end)
        /// </summary>
        Real rotationRangeEnd;

        #endregion Fields

        public RotationAffector()
        {
            this.type = "Rotator";
            rotationSpeedRangeStart = 0;
            rotationSpeedRangeEnd = 0;
            rotationRangeStart = 0;
            rotationRangeEnd = 0;
        }

        public override void InitParticle( ref Particle particle )
        {
            particle.Rotation = rotationRangeStart + ( Utility.UnitRandom() * ( rotationRangeEnd - rotationRangeStart ) );
            particle.RotationSpeed = rotationSpeedRangeStart + ( Utility.UnitRandom() * ( rotationSpeedRangeEnd - rotationSpeedRangeStart ) );
        }

        public override void AffectParticles( ParticleSystem system, Real timeElapsed )
        {
            Real ds;

            // Rotation adjustments by time
            ds = timeElapsed;

            Real newRotation;

            // loop through the particles
            for ( int i = 0; i < system.Particles.Count; i++ )
            {
                Particle p = (Particle)system.Particles[i];

                newRotation = p.Rotation + ( ds * p.RotationSpeed );
                p.Rotation = newRotation;
            }
        }

        #region Command definition classes

        [Command( "rotation_speed_range_start", "Start range of particle rotation speed.", typeof( ParticleAffector ) )]
        class RotationSpeedRangeStartCommand : ICommand
        {
            #region ICommand Members

            public string Get( object target )
            {
                RotationAffector affector = target as RotationAffector;
                return StringConverter.ToString( affector.rotationSpeedRangeStart );
            }
            public void Set( object target, string val )
            {
                RotationAffector affector = target as RotationAffector;
                affector.rotationSpeedRangeStart = Real.Parse( val );
            }

            #endregion
        }

        [Command( "rotation_speed_range_end", "End range of particle rotation speed.", typeof( ParticleAffector ) )]
        class RotationSpeedRangeEndCommand : ICommand
        {
            #region ICommand Members

            public string Get( object target )
            {
                RotationAffector affector = target as RotationAffector;
                return StringConverter.ToString( affector.rotationSpeedRangeEnd );
            }
            public void Set( object target, string val )
            {
                RotationAffector affector = target as RotationAffector;
                affector.rotationSpeedRangeEnd = Real.Parse( val );
            }

            #endregion
        }

        [Command( "rotation_range_start", "Start range of particle rotation.", typeof( ParticleAffector ) )]
        class RotationRangeStartCommand : ICommand
        {
            #region ICommand Members

            public string Get( object target )
            {
                RotationAffector affector = target as RotationAffector;
                return StringConverter.ToString( affector.rotationRangeStart );
            }
            public void Set( object target, string val )
            {
                RotationAffector affector = target as RotationAffector;
                affector.rotationRangeStart = Real.Parse( val );
            }

            #endregion
        }


        [Command( "rotation_range_end", "End range of particle rotation.", typeof( ParticleAffector ) )]
        class RotationRangeEndCommand : ICommand
        {
            #region ICommand Members

            public string Get( object target )
            {
                RotationAffector affector = target as RotationAffector;
                return StringConverter.ToString( affector.rotationRangeEnd );
            }
            public void Set( object target, string val )
            {
                RotationAffector affector = target as RotationAffector;
                affector.rotationRangeEnd = Real.Parse( val );
            }

            #endregion
        }

        #endregion Command definition classes
    }
}
