#region LGPL License
/*
Axiom Game Engine Library
Copyright (C) 2003  Axiom Project Team

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

using System;
using Axiom.MathLib.Collections;

namespace Axiom.MathLib {
    /// <summary>
    /// This is a class which exposes static methods for various common math functions.  Currently,
    /// the methods simply wrap the methods of the System.Math class (with the exception of a few added extras).
    /// This is in case the implementation needs to be swapped out with a faster C++ implementation, if
    /// deemed that the System.Math methods are not up to far speed wise.
    /// </summary>
    /// TODO: Add overloads for all methods for all instrinsic data types (i.e. float, short, etc).
    public sealed class MathUtil {
        /// <summary>
        ///		Empty private constructor.  This class has nothing but static methods/properties, so a public default
        ///		constructor should not be created by the compiler.  This prevents instance of this class from being
        ///		created.
        /// </summary>
        private MathUtil() {}

        static Random random = new Random();

        #region Constant

        public const float PI = (float)Math.PI;
        public const float TWO_PI = (float)Math.PI * 2.0f;
        public const float RADIANS_PER_DEGREE = PI / 180.0f;
        public const float DEGREES_PER_RADIAN = 180.0f / PI;

        #endregion

        #region Static Methods

        /// <summary>
        ///		Converts degrees to radians.
        /// </summary>
        /// <param name="degrees"></param>
        /// <returns></returns>
        public static float DegreesToRadians(float degrees) {
            return degrees * RADIANS_PER_DEGREE;
        }

        /// <summary>
        ///		Converts radians to degrees.
        /// </summary>
        /// <param name="radians"></param>
        /// <returns></returns>
        public static float RadiansToDegrees(float radians) {
            return radians * DEGREES_PER_RADIAN;
        }

        /// <summary>
        ///		Returns the sine of the angle.
        /// </summary>
        /// <param name="angle"></param>
        /// <returns></returns>
        public static float Sin(float angle) {
            return (float)Math.Sin(angle);
        }

        /// <summary>
        ///		Returns the cosine of the angle.
        /// </summary>
        /// <param name="angle"></param>
        /// <returns></returns>
        public static float Cos(float angle) {
            return (float)Math.Cos(angle);
        }

        /// <summary>
        ///		Returns the arc cosine of the angle.
        /// </summary>
        /// <param name="angle"></param>
        /// <returns></returns>
        public static float ACos(float angle) {
           
            // HACK: Ok, this needs to be looked at.  The decimal precision of float values can sometimes be 
            // *slightly* off from what is loaded from .skeleton files.  In some scenarios when we end up having 
            // a cos value calculated above that is just over 1 (i.e. 1.000000012), which the ACos of is Nan, thus 
            // completly throwing off node transformations and rotations associated with an animation.
           if(angle > 1)
                angle = 1.0f;
                
            return (float)Math.Acos(angle);
        }

        /// <summary>
        ///		Returns the arc sine of the angle.
        /// </summary>
        /// <param name="angle"></param>
        /// <returns></returns>
        public static float ASin(float angle) {
            return (float)Math.Asin(angle);
        }

        /// <summary>
        ///    Inverse square root.
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        public static float InvSqrt(float number) {
            return 1 / Sqrt(number);
        }

        /// <summary>
        ///		Returns the square root of a number.
        /// </summary>
        /// <remarks>This is one of the more expensive math operations.  Avoid when possible.</remarks>
        /// <param name="number"></param>
        /// <returns></returns>
        public static float Sqrt(float number) {
            return (float)Math.Sqrt(number);
        }

        /// <summary>
        ///		Returns the absolute value of the supplied number.
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        public static float Abs(float number) {
            return Math.Abs(number);
        }

        /// <summary>
        ///		Returns the tangent of the angle.
        /// </summary>
        /// <param name="angle"></param>
        /// <returns></returns>
        public static float Tan(float angle) {
            return (float)Math.Tan(angle);
        }

        /// <summary>
        ///		Used to quickly determine the greater value between two values.
        /// </summary>
        /// <param name="value1"></param>
        /// <param name="value2"></param>
        /// <returns></returns>
        public static float Max(float value1, float value2) {
            return Math.Max(value1, value2);
        }

        /// <summary>
        ///		Used to quickly determine the lesser value between two values.
        /// </summary>
        /// <param name="value1"></param>
        /// <param name="value2"></param>
        /// <returns></returns>
        public static float Min(float value1, float value2) {
            return Math.Min(value1, value2);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static float UnitRandom() {
            return (float)random.Next(Int32.MaxValue) / (float)Int32.MaxValue;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static float SymmetricRandom() {
            return 2.0f * UnitRandom() - 1.0f;
        }

        #region Intersection Methods

        /// <summary>
        ///    Tests an intersection between a ray and a box.
        /// </summary>
        /// <param name="ray"></param>
        /// <param name="box"></param>
        /// <returns>A Pair object containing whether the intersection occurred, and the distance between the 2 objects.</returns>
        public static Pair Intersects(Ray ray, AxisAlignedBox box) {
            if(box.IsNull) {
                return new Pair(false, 0);
            }

            float lowt = 0.0f;
            float t;
            bool hit = false;
            Vector3 hitPoint;
            Vector3 min = box.Minimum;
            Vector3 max = box.Maximum;
            
            // check origin inside first
            if(ray.origin > min && ray.origin < max) {
                return new Pair(true, 0.0f);
            }

            // check each face in turn, only check closest 3

            // Min X
            if(ray.origin.x < min.x && ray.direction.x > 0) {
                t = (min.x - ray.origin.x) / ray.direction.x;

                if(t > 0) {
                    // substitue t back into ray and check bounds and distance
                    hitPoint = ray.origin + ray.direction * t;

                    if(hitPoint.y >= min.y && hitPoint.y <= max.y &&
                        hitPoint.z >= min.z && hitPoint.z <= max.z &&
                        (!hit || t < lowt)) {

                        hit = true;
                        lowt = t;
                    }
                }
            }

            // Max X
            if(ray.origin.x > max.x && ray.direction.x < 0) {
                t = (max.x - ray.origin.x) / ray.direction.x;

                if(t > 0) {
                    // substitue t back into ray and check bounds and distance
                    hitPoint = ray.origin + ray.direction * t;

                    if(hitPoint.y >= min.y && hitPoint.y <= max.y &&
                        hitPoint.z >= min.z && hitPoint.z <= max.z &&
                        (!hit || t < lowt)) {

                        hit = true;
                        lowt = t;
                    }
                }
            }
                
            // Min Y
            if(ray.origin.y < min.y && ray.direction.y > 0) {
                t = (min.y - ray.origin.y) / ray.direction.y;

                if(t > 0) {
                    // substitue t back into ray and check bounds and distance
                    hitPoint = ray.origin + ray.direction * t;

                    if(hitPoint.x >= min.x && hitPoint.x <= max.x &&
                        hitPoint.z >= min.z && hitPoint.z <= max.z &&
                        (!hit || t < lowt)) {

                        hit = true;
                        lowt = t;
                    }
                }
            }

            // Max Y
            if(ray.origin.y > max.y && ray.direction.y < 0) {
                t = (max.y - ray.origin.y) / ray.direction.y;

                if(t > 0) {
                    // substitue t back into ray and check bounds and distance
                    hitPoint = ray.origin + ray.direction * t;

                    if(hitPoint.x >= min.x && hitPoint.x <= max.x &&
                        hitPoint.z >= min.z && hitPoint.z <= max.z &&
                        (!hit || t < lowt)) {

                        hit = true;
                        lowt = t;
                    }
                }
            }

            // Min Z
            if(ray.origin.z < min.z && ray.direction.z > 0) {
                t = (min.z - ray.origin.z) / ray.direction.z;

                if(t > 0) {
                    // substitue t back into ray and check bounds and distance
                    hitPoint = ray.origin + ray.direction * t;

                    if(hitPoint.x >= min.x && hitPoint.x <= max.x &&
                        hitPoint.y >= min.y && hitPoint.y <= max.y &&
                        (!hit || t < lowt)) {

                        hit = true;
                        lowt = t;
                    }
                }
            }

            // Max Z
            if(ray.origin.z > max.z && ray.direction.z < 0) {
                t = (max.z - ray.origin.z) / ray.direction.z;

                if(t > 0) {
                    // substitue t back into ray and check bounds and distance
                    hitPoint = ray.origin + ray.direction * t;

                    if(hitPoint.x >= min.x && hitPoint.x <= max.x &&
                        hitPoint.y >= min.y && hitPoint.y <= max.y &&
                        (!hit || t < lowt)) {

                        hit = true;
                        lowt = t;
                    }
                }
            }

            return new Pair(hit, lowt);
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
        ///            <description>Contains - boxA is fully within boxB.</description>
        ///         </item>
        ///        <item>
        ///            <description>Partial - boxA is partially intersecting with boxB.</description>
        ///         </item>
        ///     </list>
        /// </returns>
        /// Submitted by: romout
        public static Intersection Intersects(AxisAlignedBox boxA, AxisAlignedBox boxB) {
            // grab the max and mix vectors for both boxes for comparison
            Vector3 minA = boxA.Minimum; 
            Vector3 maxA = boxA.Maximum; 
            Vector3 minB = boxB.Minimum; 
            Vector3 maxB = boxB.Maximum; 

            if ((minB.x > minA.x) && 
                (maxB.x < maxA.x) && 
                (minB.y > minA.y) && 
                (maxB.y < maxA.y) && 
                (minB.z > minA.z) && 
                (maxB.z < maxA.z)) {

                // boxA is within boxb
                return Intersection.Contained; 
            }

            if ((minB.x > maxB.x) || 
                (minB.y > maxB.y) || 
                (minB.z > maxB.z) || 
                (maxB.x < minA.x) || 
                (maxB.y < minA.y) || 
                (maxB.z < minA.z)) {

                // not interesting at all
                return Intersection.None; 
            }

            // if we got this far, they are partially intersecting
            return Intersection.Partial; 
        }

        #endregion Intersection Methods

        #endregion Static Methods

        #region Static properties

        #endregion

    }

}
