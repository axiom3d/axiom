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
using System.Collections.Generic;
using System.Text;

using DotNet3D.Math.Collections;

#endregion Namespace Declarations
			
namespace DotNet3D.Math
{
    /// <summary>
    /// Specialized class for containing the result of an intersection.
    /// </summary>
    public sealed class IntersectionResult : Tuple< bool, Real >
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="hit"></param>
        /// <param name="distance"></param>
        public IntersectionResult( bool hit, Real distance )
        {
            first = hit;
            second = distance;
        }

        public bool Hit
        {
            get
            {
                return first;
            }
            set
            {
                first = value;
            }
        }

        public Real Distance
        {
            get
            {
                return second;
            }
            set
            {
                second = value;
            }
        }
    }


    /// <summary>
    /// This is a utility class that encapsulates all the intersection tests between the various object types
    /// </summary>
    /// <remarks>
    /// This class is used internally by the other classes, normal usage should be to call the Intersects method on 
    /// the object you want to test.
    /// </remarks>
    public sealed class Intersection
    {
        public enum Result
        {
            None,
            Contained,
            Contains,
            Partial
        }

        /// <summary>
        /// Private constructor to prevent instantiation.
        /// </summary>
        private Intersection()
        {
        }

        #region Intersection Methods

        /// <summary>
        ///    Tests an intersection between a ray and a box.
        /// </summary>
        /// <param name="ray"></param>
        /// <param name="box"></param>
        /// <returns>A Pair object containing whether the intersection occurred, and the distance between the 2 objects.</returns>
        public static IntersectionResult Test( Ray ray, AxisAlignedBox box )
        {
            if ( box.IsNull )
            {
                return new IntersectionResult( false, 0 );
            }

            float lowt = 0.0f;
            float t;
            bool hit = false;
            Vector3 hitPoint;
            Vector3 min = box.Minimum;
            Vector3 max = box.Maximum;

            // check origin inside first
            if ( ray.origin > min && ray.origin < max )
            {
                return new IntersectionResult( true, 0.0f );
            }

            // check each face in turn, only check closest 3

            // Min X
            if ( ray.origin.x < min.x && ray.direction.x > 0 )
            {
                t = ( min.x - ray.origin.x ) / ray.direction.x;

                if ( t > 0 )
                {
                    // substitue t back into ray and check bounds and distance
                    hitPoint = ray.origin + ray.direction * t;

                    if ( hitPoint.y >= min.y && hitPoint.y <= max.y &&
                        hitPoint.z >= min.z && hitPoint.z <= max.z &&
                        ( !hit || t < lowt ) )
                    {

                        hit = true;
                        lowt = t;
                    }
                }
            }

            // Max X
            if ( ray.origin.x > max.x && ray.direction.x < 0 )
            {
                t = ( max.x - ray.origin.x ) / ray.direction.x;

                if ( t > 0 )
                {
                    // substitue t back into ray and check bounds and distance
                    hitPoint = ray.origin + ray.direction * t;

                    if ( hitPoint.y >= min.y && hitPoint.y <= max.y &&
                        hitPoint.z >= min.z && hitPoint.z <= max.z &&
                        ( !hit || t < lowt ) )
                    {

                        hit = true;
                        lowt = t;
                    }
                }
            }

            // Min Y
            if ( ray.origin.y < min.y && ray.direction.y > 0 )
            {
                t = ( min.y - ray.origin.y ) / ray.direction.y;

                if ( t > 0 )
                {
                    // substitue t back into ray and check bounds and distance
                    hitPoint = ray.origin + ray.direction * t;

                    if ( hitPoint.x >= min.x && hitPoint.x <= max.x &&
                        hitPoint.z >= min.z && hitPoint.z <= max.z &&
                        ( !hit || t < lowt ) )
                    {

                        hit = true;
                        lowt = t;
                    }
                }
            }

            // Max Y
            if ( ray.origin.y > max.y && ray.direction.y < 0 )
            {
                t = ( max.y - ray.origin.y ) / ray.direction.y;

                if ( t > 0 )
                {
                    // substitue t back into ray and check bounds and distance
                    hitPoint = ray.origin + ray.direction * t;

                    if ( hitPoint.x >= min.x && hitPoint.x <= max.x &&
                        hitPoint.z >= min.z && hitPoint.z <= max.z &&
                        ( !hit || t < lowt ) )
                    {

                        hit = true;
                        lowt = t;
                    }
                }
            }

            // Min Z
            if ( ray.origin.z < min.z && ray.direction.z > 0 )
            {
                t = ( min.z - ray.origin.z ) / ray.direction.z;

                if ( t > 0 )
                {
                    // substitue t back into ray and check bounds and distance
                    hitPoint = ray.origin + ray.direction * t;

                    if ( hitPoint.x >= min.x && hitPoint.x <= max.x &&
                        hitPoint.y >= min.y && hitPoint.y <= max.y &&
                        ( !hit || t < lowt ) )
                    {

                        hit = true;
                        lowt = t;
                    }
                }
            }

            // Max Z
            if ( ray.origin.z > max.z && ray.direction.z < 0 )
            {
                t = ( max.z - ray.origin.z ) / ray.direction.z;

                if ( t > 0 )
                {
                    // substitue t back into ray and check bounds and distance
                    hitPoint = ray.origin + ray.direction * t;

                    if ( hitPoint.x >= min.x && hitPoint.x <= max.x &&
                        hitPoint.y >= min.y && hitPoint.y <= max.y &&
                        ( !hit || t < lowt ) )
                    {

                        hit = true;
                        lowt = t;
                    }
                }
            }

            return new IntersectionResult( hit, lowt );
        }


        /// <summary>
        ///    Tests an intersection between two boxes.
        /// </summary>
        /// <param name="boxA">
        ///    The primary box.
        /// </param>
        /// <param name="boxB">
        ///    The box to test intersection with boxA.
        /// </param>
        /// <returns>
        ///    <list type="bullet">
        ///        <item>
        ///            <description>None - There was no intersection between the 2 boxes.</description>
        ///        </item>
        ///        <item>
        ///            <description>Contained - boxA is fully within boxB.</description>
        ///         </item>
        ///        <item>
        ///            <description>Contains - boxB is fully within boxA.</description>
        ///         </item>
        ///        <item>
        ///            <description>Partial - boxA is partially intersecting with boxB.</description>
        ///         </item>
        ///     </list>
        /// </returns>
        /// Submitted by: romout
        public static Intersection.Result Test( AxisAlignedBox boxA, AxisAlignedBox boxB )
        {
            // grab the max and mix vectors for both boxes for comparison
            Vector3 minA = boxA.Minimum;
            Vector3 maxA = boxA.Maximum;
            Vector3 minB = boxB.Minimum;
            Vector3 maxB = boxB.Maximum;

            if ( ( minB.x < minA.x ) &&
                ( maxB.x > maxA.x ) &&
                ( minB.y < minA.y ) &&
                ( maxB.y > maxA.y ) &&
                ( minB.z < minA.z ) &&
                ( maxB.z > maxA.z ) )
            {

                // boxA is within boxB
                return Intersection.Result.Contained;
            }

            if ( ( minB.x > minA.x ) &&
                ( maxB.x < maxA.x ) &&
                ( minB.y > minA.y ) &&
                ( maxB.y < maxA.y ) &&
                ( minB.z > minA.z ) &&
                ( maxB.z < maxA.z ) )
            {

                // boxB is within boxA
                return Intersection.Result.Contains;
            }

            if ( ( minB.x > maxA.x ) ||
                ( minB.y > maxA.y ) ||
                ( minB.z > maxA.z ) ||
                ( maxB.x < minA.x ) ||
                ( maxB.y < minA.y ) ||
                ( maxB.z < minA.z ) )
            {

                // not interesting at all
                return Intersection.Result.None;
            }

            // if we got this far, they are partially intersecting
            return Intersection.Result.Partial;
        }


        /// <summary>
        ///		Ray/Sphere intersection test.
        /// </summary>
        /// <param name="ray"></param>
        /// <param name="sphere"></param>
        /// <returns>Struct that contains a bool (hit?) and distance.</returns>
        /// <remarks>Does not discard inside rays.</remarks>
        public static IntersectionResult Test( Ray ray, Sphere sphere )
        {
            return Test( ray, sphere, false );
        }

        /// <summary>
        ///		Ray/Sphere intersection test.
        /// </summary>
        /// <param name="ray"></param>
        /// <param name="sphere"></param>
        /// <param name="discardInside"></param>
        /// <returns>Struct that contains a bool (hit?) and distance.</returns>
        public static IntersectionResult Test( Ray ray, Sphere sphere, bool discardInside )
        {
            Vector3 rayDir = ray.Direction;
            //Adjust ray origin relative to sphere center
            Vector3 rayOrig = ray.Origin - sphere.Center;
            float radius = sphere.Radius;

            // check origin inside first
            if ( ( rayOrig.LengthSquared <= radius * radius ) && discardInside )
            {
                return new IntersectionResult( true, 0 );
            }

            // mmm...sweet quadratics
            // Build coeffs which can be used with std quadratic solver
            // ie t = (-b +/- sqrt(b*b* + 4ac)) / 2a
            float a = rayDir.DotProduct( rayDir );
            float b = 2 * rayOrig.DotProduct( rayDir );
            float c = rayOrig.DotProduct( rayOrig ) - ( radius * radius );

            // calc determinant
            float d = ( b * b ) - ( 4 * a * c );

            if ( d < 0 )
            {
                // no intersection
                return new IntersectionResult( false, 0 );
            }
            else
            {
                // BTW, if d=0 there is one intersection, if d > 0 there are 2
                // But we only want the closest one, so that's ok, just use the 
                // '-' version of the solver
                float t = ( -b - Utility.Sqrt( d ) ) / ( 2 * a );

                if ( t < 0 )
                {
                    t = ( -b + Utility.Sqrt( d ) ) / ( 2 * a );
                }

                return new IntersectionResult( true, t );
            }
        }

        /// <summary>
        ///		Ray/Plane intersection test.
        /// </summary>
        /// <param name="ray"></param>
        /// <param name="plane"></param>
        /// <returns>Struct that contains a bool (hit?) and distance.</returns>
        public static IntersectionResult Test( Ray ray, Plane plane )
        {
            float denom = plane.Normal.DotProduct( ray.Direction );

            if ( Utility.Abs( denom ) < float.Epsilon )
            {
                // Parellel
                return new IntersectionResult( false, 0 );
            }
            else
            {
                float nom = plane.Normal.DotProduct( ray.Origin ) + plane.Distance;
                float t = -( nom / denom );
                return new IntersectionResult( t >= 0, t );
            }
        }

        public static IntersectionResult Test( Ray ray, PlaneList planes, bool NormalIsOutside )
        {
            bool allInside = true;
            IntersectionResult ret = new IntersectionResult( false, Real.Zero );

            // derive side
            // NB we don't pass directly since that would require Plane::Side in 
            // interface, which results in recursive includes since Math is so fundamental
            Plane.Side outside = NormalIsOutside ? Plane.Side.Positive : Plane.Side.Negative;

            foreach ( Plane plane in planes )
            {

                // is origin outside?
                if ( plane.GetSide( ray.Origin ) == outside )
                {
                    allInside = false;
                    // Test single plane
                    IntersectionResult planeRes = ray.Intersects( plane );
                    if ( planeRes.first )
                    {
                        // Ok, we intersected
                        ret.first = true;
                        // Use the most distant result since convex volume
                        ret.second = Utility.Max( ret.second, planeRes.second );
                    }
                }
            }

            if ( allInside )
            {
                // Intersecting at 0 distance since inside the volume!
                ret.first = true;
                ret.second = 0.0f;
            }

            return ret;
        }

        /// <summary>
        ///		Sphere/Box intersection test.
        /// </summary>
        /// <param name="sphere"></param>
        /// <param name="box"></param>
        /// <returns>True if there was an intersection, false otherwise.</returns>
        public static bool Test( Sphere sphere, AxisAlignedBox box )
        {
            if ( box.IsNull )
                return false;

            // Use splitting planes
            Vector3 center = sphere.Center;
            float radius = sphere.Radius;
            Vector3 min = box.Minimum;
            Vector3 max = box.Maximum;

            // just test facing planes, early fail if sphere is totally outside
            if ( center.x < min.x &&
                min.x - center.x > radius )
            {
                return false;
            }
            if ( center.x > max.x &&
                center.x - max.x > radius )
            {
                return false;
            }

            if ( center.y < min.y &&
                min.y - center.y > radius )
            {
                return false;
            }
            if ( center.y > max.y &&
                center.y - max.y > radius )
            {
                return false;
            }

            if ( center.z < min.z &&
                min.z - center.z > radius )
            {
                return false;
            }
            if ( center.z > max.z &&
                center.z - max.z > radius )
            {
                return false;
            }

            // Must intersect
            return true;
        }

        /// <summary>
        ///		Plane/Box intersection test.
        /// </summary>
        /// <param name="plane"></param>
        /// <param name="box"></param>
        /// <returns>True if there was an intersection, false otherwise.</returns>
        public static bool Test( Plane plane, AxisAlignedBox box )
        {
            if ( box.IsNull )
                return false;

            // Get corners of the box
            Vector3[] corners = box.Corners;

            // Test which side of the plane the corners are
            // Intersection occurs when at least one corner is on the 
            // opposite side to another
            Plane.Side lastSide = plane.GetSide( corners[ 0 ] );

            for ( int corner = 1; corner < 8; corner++ )
            {
                if ( plane.GetSide( corners[ corner ] ) != lastSide )
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///		Sphere/Plane intersection test.
        /// </summary>
        /// <param name="sphere"></param>
        /// <param name="plane"></param>
        /// <returns>True if there was an intersection, false otherwise.</returns>
        public static bool Test( Sphere sphere, Plane plane )
        {
            return Utility.Abs( plane.Normal.DotProduct( sphere.Center ) ) <= sphere.Radius;
        }

        /// <summary>
        ///    Ray/PlaneBoundedVolume intersection test.
        /// </summary>
        /// <param name="ray"></param>
        /// <param name="volume"></param>
        /// <returns>Struct that contains a bool (hit?) and distance.</returns>
        public static IntersectionResult Test( Ray ray, PlaneBoundedVolume volume )
        {
            PlaneList planes = volume.planes;

            float maxExtDist = 0.0f;
            float minIntDist = float.PositiveInfinity;

            float dist, denom, nom;

            for ( int i = 0; i < planes.Count; i++ )
            {
                Plane plane = (Plane)planes[ i ];

                denom = plane.Normal.DotProduct( ray.Direction );
                if ( Utility.Abs( denom ) < float.Epsilon )
                {
                    // Parallel
                    if ( plane.GetSide( ray.Origin ) == volume.outside )
                        return new IntersectionResult( false, 0 );

                    continue;
                }

                nom = plane.Normal.DotProduct( ray.Origin ) + plane.Distance;
                dist = -( nom / denom );

                if ( volume.outside == Plane.Side.Negative )
                    nom = -nom;

                if ( dist > 0.0f )
                {
                    if ( nom > 0.0f )
                    {
                        if ( maxExtDist < dist )
                            maxExtDist = dist;
                    }
                    else
                    {
                        if ( minIntDist > dist )
                            minIntDist = dist;
                    }
                }
                else
                {
                    //Ray points away from plane
                    if ( volume.outside == Plane.Side.Negative )
                        denom = -denom;

                    if ( denom > 0.0f )
                        return new IntersectionResult( false, 0 );
                }
            }

            if ( maxExtDist > minIntDist )
                return new IntersectionResult( false, 0 );

            return new IntersectionResult( true, maxExtDist );
        }

        #endregion Intersection Methods


    }
}
