#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2006  Axiom Project Team

The overall design, and a majority of the core engine and rendering code 
contained within this library is a derivative of the open source Object Oriented 
Graphics Engine OGRE, which can be found at http://ogre.sourceforge.net.  
Many thanks to the OGRE team for maintaining such a high quality project.

The math library included in this project, in addition to being a derivative of
the works of Ogre, also include derivative work of the free portion of the 
Wild Magic mathematics source code that is distributed with the excellent
book Game Engine Design.
http://www.wild-magic.com/

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

#endregion Namespace Declarations
			
namespace DotNet3D.Math
{
    /// <summary>
    /// 	Representation of a ray in space, ie a line with an origin and direction.
    /// </summary>
    public class Ray
    {
        #region Fields and Properties

        #region Origin Property

        internal Vector3 origin;
        /// <summary>
        ///    Gets/Sets the origin of the ray.
        /// </summary>
        public Vector3 Origin
        {
            get
            {
                return origin;
            }
            set
            {
                origin = value;
            }
        }

        #endregion Origin Property

        #region Direction Property

        internal Vector3 direction;
        /// <summary>
        ///    Gets/Sets the direction this ray is pointing.
        /// </summary>
        /// <remarks>
        ///    A ray has no length, so the direction goes to infinity.
        /// </remarks>
        public Vector3 Direction
        {
            get
            {
                return direction;
            }
            set
            {
                direction = value;
            }
        }

        #endregion Direction Property
			

        #endregion

        #region Constructors

        /// <summary>
        ///    Default constructor.
        /// </summary>
        public Ray()
        {
            origin = Vector3.Zero;
            direction = Vector3.UnitZ;
        }

        /// <summary>
        ///    Constructor.
        /// </summary>
        /// <param name="origin">Starting point of the ray.</param>
        /// <param name="direction">Direction the ray is pointing.</param>
        public Ray( Vector3 origin, Vector3 direction )
        {
            this.origin = origin;
            this.direction = direction;
        }

        #endregion

        #region Intersection Methods

        /// <summary>
        ///    Tests whether this ray intersects the given box.
        /// </summary>
        /// <param name="box"></param>
        /// <returns>
        ///		Struct containing info on whether there was a hit, and the distance from the 
        ///		origin of this ray where the intersect happened.
        ///	</returns>
        public IntersectionResult Intersects( AxisAlignedBox box )
        {
            return Intersection.Test( this, box );
        }

        /// <summary>
        ///		Tests whether this ray intersects the given plane. 
        /// </summary>
        /// <param name="plane"></param>
        /// <returns>
        ///		Struct containing info on whether there was a hit, and the distance from the 
        ///		origin of this ray where the intersect happened.
        ///	</returns>
        public IntersectionResult Intersects( Plane plane )
        {
            return Intersection.Test( this, plane );
        }

        /// <summary>
        ///		Tests whether this ray intersects the given sphere. 
        /// </summary>
        /// <param name="sphere"></param>
        /// <returns>
        ///		Struct containing info on whether there was a hit, and the distance from the 
        ///		origin of this ray where the intersect happened.
        ///	</returns>
        public IntersectionResult Intersects( Sphere sphere )
        {
            return Intersection.Test( this, sphere );
        }

        /// <summary>
        ///		Tests whether this ray intersects the given PlaneBoundedVolume. 
        /// </summary>
        /// <param name="volume"></param>
        /// <returns>
        ///		Struct containing info on whether there was a hit, and the distance from the 
        ///		origin of this ray where the intersect happened.
        ///	</returns>
        public IntersectionResult Intersects( PlaneBoundedVolume volume )
        {
            return Intersection.Test( this, volume.planes,  volume.outside == Plane.Side.Positive );
        }

        #endregion Intersection Methods

        #region Operator Overloads

        /// <summary>
        ///    Gets the position of a point t units along the ray.
        /// </summary>
        /// <param name="ray"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static Vector3 operator *( Ray ray, float t )
        {
            return ray.origin + ( ray.direction * t );
        }
        public static bool operator ==( Ray left, Ray right )
        {
            return left.direction == right.direction && left.origin == right.origin;
        }

        public static bool operator !=( Ray left, Ray right )
        {
            return left.direction != right.direction || left.origin != right.origin;
        }

        public override bool Equals( object obj )
        {
            return obj is Ray && this == (Ray)obj;
        }
        public override int GetHashCode()
        {
            return direction.GetHashCode() ^ origin.GetHashCode();
        }



        #endregion Operator Overloads

    }
}
