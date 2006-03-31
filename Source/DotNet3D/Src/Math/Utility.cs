#region LGPL License
/*
DotNet3D Library
Copyright (C) 2006 DotNet3D Project Team

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
using System.Collections.Generic;

using DotNet3D.Math.Collections;

#endregion Namespace Declarations

namespace DotNet3D.Math
{
    public sealed class Utility
    {
        public static readonly Real PI = new Real( new Real( 4.0f ) * (Real)ATan( 1.0f ) );
        public static readonly Real TWO_PI = new Real( 2.0f * PI );
        public static readonly Real HALF_PI = new Real( 0.5f * PI );

        private static Random random = new Random();

        /// <summary>
        /// 
        /// </summary>
        private Utility()
        {
        }

        public static int Sign( Real number )
        {
            return System.Math.Sign( number );
        }

        /// <summary>
        ///	Returns the sine of the specified angle.
        /// </summary>
        public static Real Sin( Radian angle )
        {
            return System.Math.Sin( (Real)angle );
        }

        /// <summary>
        ///	Returns the angle whose cosine is the specified number.
        /// </summary>
        public static Radian ASin( Real angle )
        {
            return new Radian( System.Math.Asin( angle ) );
        }

        /// <summary>
        ///	Returns the cosine of the specified angle.
        /// </summary>
        public static Real Cos( Radian angle )
        {
            return System.Math.Cos( (Real)angle );
        }

        /// <summary>
        ///	Returns the angle whose cosine is the specified number.
        /// </summary>
        public static Radian ACos( Real angle )
        {

            // HACK: Ok, this needs to be looked at.  The decimal precision of float values can sometimes be 
            // *slightly* off from what is loaded from .skeleton files.  In some scenarios when we end up having 
            // a cos value calculated above that is just over 1 (i.e. 1.000000012), which the ACos of is Nan, thus 
            // completly throwing off node transformations and rotations associated with an animation.
            if ( angle > 1 )
            {
                angle = 1.0f;
            }

            return new Radian( System.Math.Acos( angle ) ) ;
        }

        /// <summary>
        /// Returns the tangent of the specified angle.
        /// </summary>
        public static Real Tan( Radian value )
        {
            return System.Math.Tan( (Real)value );
        }

        /// <summary>
        /// Return the angle whos tangent is the specified number.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Radian ATan( Real value )
        {
            return new Radian( System.Math.Atan( value ) );
        }

        /// <summary>
        /// Returns the angle whose tangent is the quotient of the two specified numbers.
        /// </summary>
        public static Radian ATan( Real y, Real x )
        {
            return new Radian( System.Math.Atan2( y, x ) );
        }

        public static Radian ATan2( Real y, Real x )
        {
            return new Radian( System.Math.Atan2( y, x ) );
        }

        /// <summary>
        ///		Returns the square root of a number.
        /// </summary>
        /// <remarks>This is one of the more expensive math operations.  Avoid when possible.</remarks>
        /// <param name="number"></param>
        /// <returns></returns>
        public static Real Sqrt( Real number )
        {
            return (Real)System.Math.Sqrt( number );
        }

        /// <summary>
        ///    Inverse square root.
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        public static Real InvSqrt( Real number )
        {
            return 1 / Sqrt( number );
        }

        /// <summary>
        ///		Returns the absolute value of the supplied number.
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        public static Real Abs( Real number )
        {
            return new Real(System.Math.Abs( number ));
        }

        /// <summary>
        /// Returns the maximum of the two supplied values.
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        public static Real Max( Real lhs, Real rhs )
        {
            return lhs > rhs ? lhs : rhs;
        }

        /// <summary>
        /// Returns the minumum of the two supplied values.
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        public static Real Min( Real lhs, Real rhs )
        {
            return lhs < rhs ? lhs : rhs;
        }

        /// <summary>
        ///    Returns a random value between the specified min and max values.
        /// </summary>
        /// <param name="min">Minimum value.</param>
        /// <param name="max">Maximum value.</param>
        /// <returns>A random value in the range [min,max].</returns>
        public static Real RangeRandom( Real min, Real max )
        {
            return ( max - min ) * UnitRandom() + min;
        }

        /// <summary>
        ///    
        /// </summary>
        /// <returns></returns>
        public static Real UnitRandom()
        {
            return new Real( random.Next( Int32.MaxValue ) / Int32.MaxValue );
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static Real SymmetricRandom()
        {
            return new Real( 2.0f * (float)UnitRandom() - 1.0f );
        }

        /// <summary>
        ///     Builds a reflection matrix for the specified plane.
        /// </summary>
        /// <param name="plane"></param>
        /// <returns></returns>
        public static Matrix4 BuildReflectionMatrix( Plane plane )
        {
            Vector3 normal = plane.Normal;

            return new Matrix4(
                -2.0f * normal.x * normal.x + 1.0f, -2.0f * normal.x * normal.y, -2.0f * normal.x * normal.z, -2.0f * normal.x * plane.Distance,
                -2.0f * normal.y * normal.x, -2.0f * normal.y * normal.y + 1.0f, -2.0f * normal.y * normal.z, -2.0f * normal.y * plane.Distance,
                -2.0f * normal.z * normal.x, -2.0f * normal.z * normal.y, -2.0f * normal.z * normal.z + 1.0f, -2.0f * normal.z * plane.Distance,
                0.0f, 0.0f, 0.0f, 1.0f );
        }

        /// <summary>
        ///		Calculate a face normal, including the w component which is the offset from the origin.
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <param name="v3"></param>
        /// <returns></returns>
        public static Vector4 CalculateFaceNormal( Vector3 v1, Vector3 v2, Vector3 v3 )
        {
            Vector3 normal = CalculateBasicFaceNormal( v1, v2, v3 );

            // Now set up the w (distance of tri from origin
            return new Vector4( normal.x, normal.y, normal.z, -( normal.DotProduct( v1 ) ) );
        }

        /// <summary>
        ///		Calculate a face normal, no w-information.
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <param name="v3"></param>
        /// <returns></returns>
        public static Vector3 CalculateBasicFaceNormal( Vector3 v1, Vector3 v2, Vector3 v3 )
        {
            Vector3 normal = ( v2 - v1 ).CrossProduct( v3 - v1 );
            normal.Normalize();

            return normal;
        }

        /// <summary>
        ///    Calculates the tangent space vector for a given set of positions / texture coords.
        /// </summary>
        /// <remarks>
        ///    Adapted from bump mapping tutorials at:
        ///    http://www.paulsprojects.net/tutorials/simplebump/simplebump.html
        ///    author : paul.baker@univ.ox.ac.uk
        /// </remarks>
        /// <param name="position1"></param>
        /// <param name="position2"></param>
        /// <param name="position3"></param>
        /// <param name="u1"></param>
        /// <param name="v1"></param>
        /// <param name="u2"></param>
        /// <param name="v2"></param>
        /// <param name="u3"></param>
        /// <param name="v3"></param>
        /// <returns></returns>
        public static Vector3 CalculateTangentSpaceVector(
            Vector3 position1, Vector3 position2, Vector3 position3, float u1, float v1, float u2, float v2, float u3, float v3 )
        {

            // side0 is the vector along one side of the triangle of vertices passed in, 
            // and side1 is the vector along another side. Taking the cross product of these returns the normal.
            Vector3 side0 = position1 - position2;
            Vector3 side1 = position3 - position1;
            // Calculate face normal
            Vector3 normal = side1.CrossProduct( side0 );
            normal.Normalize();

            // Now we use a formula to calculate the tangent. 
            float deltaV0 = v1 - v2;
            float deltaV1 = v3 - v1;
            Vector3 tangent = deltaV1 * side0 - deltaV0 * side1;
            tangent.Normalize();

            // Calculate binormal
            float deltaU0 = u1 - u2;
            float deltaU1 = u3 - u1;
            Vector3 binormal = deltaU1 * side0 - deltaU0 * side1;
            binormal.Normalize();

            // Now, we take the cross product of the tangents to get a vector which 
            // should point in the same direction as our normal calculated above. 
            // If it points in the opposite direction (the dot product between the normals is less than zero), 
            // then we need to reverse the s and t tangents. 
            // This is because the triangle has been mirrored when going from tangent space to object space.
            // reverse tangents if necessary.
            Vector3 tangentCross = tangent.CrossProduct( binormal );
            if ( tangentCross.DotProduct( normal ) < 0.0f )
            {
                tangent = -tangent;
                binormal = -binormal;
            }

            return tangent;
        }
    }
}
