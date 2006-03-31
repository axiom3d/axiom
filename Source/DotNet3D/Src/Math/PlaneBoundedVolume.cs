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
using DotNet3D.Math.Collections;

#endregion Namespace Declarations

namespace DotNet3D.Math
{
    /// <summary>
    ///		Represents a convex volume bounded by planes.
    /// </summary>
    public class PlaneBoundedVolume
    {
        #region Fields

        /// <summary>
        ///		Publicly accessible plane list, you can modify this direct.
        /// </summary>
        public PlaneList planes = new PlaneList();

        /// <summary>
        ///		Side of the plane to be considered 'outside'.
        /// </summary>
        public Plane.Side outside;

        #endregion Fields

        #region Constructors

        /// <summary>
        ///		Default constructor.
        /// </summary>
        public PlaneBoundedVolume() : this( Plane.Side.Negative )
        {
        }

        /// <summary>
        ///		Constructor.
        /// </summary>
        /// <param name="outside">Side of the plane to be considered 'outside'.</param>
        public PlaneBoundedVolume( Plane.Side outside )
        {
            this.outside = outside;
        }

        #endregion Constructors

        #region Methods

        /// <summary>
        ///		Intersection test with an <see cref="AxisAlignedBox"/>.
        /// </summary>
        /// <remarks>
        ///		May return false positives but will never miss an intersection.
        /// </remarks>
        /// <param name="box">Box to test.</param>
        /// <returns>True if interesecting, false otherwise.</returns>
        public bool Intersects( AxisAlignedBox box )
        {
            if ( box.IsNull )
            {
                return false;
            }

            // If all points are on outside of any plane, we fail
            Vector3[] points = box.Corners;

            for ( int i = 0; i < planes.Count; i++ )
            {
                Plane plane = (Plane)planes[i];

                // Test which side of the plane the corners are
                // Intersection fails when at all corners are on the
                // outside of one plane
                bool splittingPlane = true;
                for ( int corner = 0; corner < 8; corner++ )
                {
                    if ( plane.GetSide( points[corner] ) != outside )
                    {
                        // this point is on the wrong side
                        splittingPlane = false;
                        break;
                    }
                }
                if ( splittingPlane )
                {
                    // Found a splitting plane therefore return not intersecting
                    return false;
                }
            }

            // couldn't find a splitting plane, assume intersecting
            return true;
        }

        /// <summary>
        ///		Intersection test with <see cref="Sphere"/>.
        /// </summary>
        /// <param name="sphere">Sphere to test.</param>
        /// <returns>True if the sphere intersects this volume, and false otherwise.</returns>
        public bool Intersects( Sphere sphere )
        {
            for ( int i = 0; i < planes.Count; i++ )
            {
                Plane plane = (Plane)planes[i];

                // Test which side of the plane the sphere is
                float d = plane.GetDistance( sphere.Center );

                // Negate d if planes point inwards
                if ( outside == Plane.Side.Negative )
                {
                    d = -d;
                }

                if ( ( d - sphere.Radius ) > 0 )
                {
                    return false;
                }
            }

            // assume intersecting
            return true;
        }


        /// <summary>
        /// Intersection test with a Ray
        /// </summary>
        /// <param name="ray">The ray to test</param>
        /// <returns>Pair of hit (bool) and distance</returns>
        /// <remarks>May return false positives but will never miss an intersection.</remarks>
        public IntersectionResult Intersects( Ray ray)
        {
            return Intersection.Test(ray, planes, outside == Plane.Side.Positive );
        }

        #endregion Methods
    }

    namespace Collections
    {
        using System.Collections.Generic;

        /// <summary>
        /// A Collection of Planes
        /// </summary>
        public class PlaneBoundedVolumeList : List<PlaneBoundedVolume>
        {
        }
    }

}
