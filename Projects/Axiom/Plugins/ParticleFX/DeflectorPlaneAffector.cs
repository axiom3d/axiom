#region LGPL License

/*
Axiom Graphics Engine Library
Copyright (C) 2003-2006 Axiom Project Team

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

#region SVN Version Information

// <file>
//     <license see="http://axiomengine.sf.net/wiki/index.php/license.txt"/>
//     <id value="$Id:"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using Axiom.Core;
using Axiom.Math;
using Axiom.ParticleSystems;
using Axiom.Scripting;

#endregion Namespace Declarations

namespace Axiom.ParticleFX
{
    /// <summary>
    /// This class defines a ParticleAffector which deflects particles.
    /// <remarks>
    /// This affector <see cref="ParticleAffector"/> offers a simple (and inaccurate) physical deflection.
    /// All particles which hit the plane are reflected.
    /// </remarks>
    /// </summary>
    public class DeflectorPlaneAffector : ParticleAffector
    {

        /// deflector plane point
        private Vector3 _planePoint;
        public Vector3 PlanePoint
        {
            get
            {
                return _planePoint;
            }
            set
            {
                _planePoint = value;
            }
        }

        /// deflector plane normal vector
        private Vector3 _planeNormal;
        public Vector3 PlaneNormal
        {
            get
            {
                return _planeNormal;
            }
            set
            {
                _planeNormal = value;
            }
        }

        /// bounce factor (0.5 means 50 percent)
        private float _bounce;
        public float Bounce
        {
            get
            {
                return _bounce;
            }
            set
            {
                _bounce = value;
            }
        }

        /// <summary>
        /// Default Costructor
        /// </summary>
        public DeflectorPlaneAffector()
        {
            this.type = "DeflectorPlane";

            // defaults
            _planePoint = Vector3.Zero;
            _planeNormal = Vector3.UnitY;
            _bounce = 1.0f;
        }

        public override void AffectParticles( ParticleSystem system, float timeElapsed )
        {
            // precalculate distance of plane from origin
            float planeDistance = -_planeNormal.Dot( _planePoint ) / Utility.Sqrt( _planeNormal.Dot( _planeNormal ) );
            Vector3 directionPart;

            foreach ( Particle pi in system.Particles )
            {
                Vector3 direction = pi.Direction * timeElapsed;
                if ( _planeNormal.Dot( pi.Position + direction ) + planeDistance <= 0.0f )
                {
                    float a = _planeNormal.Dot( pi.Position ) + planeDistance;
                    if ( a > 0.0 )
                    {
                        // for intersection point
                        directionPart = direction * ( -a / direction.Dot( _planeNormal ) );
                        // set new position
                        pi.Position = ( pi.Position + ( directionPart ) ) + ( ( ( directionPart ) - direction ) * _bounce );

                        // reflect direction vector
                        pi.Direction = ( pi.Direction - ( 2.0f * pi.Direction.Dot( _planeNormal ) * _planeNormal ) ) * _bounce;
                    }
                }
            }
        }

        #region Command definition classes

        [ScriptableProperty( "plane_point", "A point on the deflector plane. Together with the normal vector it defines the plane.", typeof ( ParticleAffector ) )]
        private class PlanePointCommand : IPropertyCommand
        {
            public string Get( object target )
            {
                DeflectorPlaneAffector affector = target as DeflectorPlaneAffector;
                return StringConverter.ToString( affector.PlanePoint );
            }

            public void Set( object target, string val )
            {
                DeflectorPlaneAffector affector = target as DeflectorPlaneAffector;
                affector.PlanePoint = StringConverter.ParseVector3( val );
            }
        }

		[ScriptableProperty("plane_normal", "The normal vector of the deflector plane. Together with the point it defines the plane.", typeof(ParticleAffector))]
        private class PlaneNormalCommand : IPropertyCommand
        {
            public string Get( object target )
            {
                DeflectorPlaneAffector affector = target as DeflectorPlaneAffector;
                return StringConverter.ToString( affector.PlaneNormal );
            }

            public void Set( object target, string val )
            {
                DeflectorPlaneAffector affector = target as DeflectorPlaneAffector;
                affector.PlaneNormal = StringConverter.ParseVector3( val );
            }
        }

        [ ScriptableProperty( "bounce", "The amount of bouncing when a particle is deflected. 0 means no deflection and 1 stands for 100 percent reflection.", typeof ( ParticleAffector ) ) ]
        private class BounceCommand : IPropertyCommand
        {
            public string Get( object target )
            {
                DeflectorPlaneAffector affector = target as DeflectorPlaneAffector;
                return StringConverter.ToString( affector.Bounce );
            }

            public void Set( object target, string val )
            {
                DeflectorPlaneAffector affector = target as DeflectorPlaneAffector;
                affector.Bounce = StringConverter.ParseFloat( val );
            }
        }

        #endregion
    }
}