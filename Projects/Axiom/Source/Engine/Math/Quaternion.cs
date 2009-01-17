#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2006 Axiom Project Team

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

#region SVN Version Information
// <file>
//     <license see="http://axiomengine.sf.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

//#undef MONO_SIMD

#region Namespace Declarations

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Axiom.Core;

#if (MONO_SIMD)
using Mono.Simd;
#endif

#endregion Namespace Declarations

namespace Axiom.Math
{
    /// <summary>
    ///		Summary description for Quaternion.
    /// </summary>
	[StructLayout( LayoutKind.Sequential, Pack = 0, Size = 16 )]
    public struct Quaternion
    {
        #region Private member variables and constants

        const  float EPSILON = 1e-03f;

#if (MONO_SIMD)		
		/* helper vector: 
		 * quaternion multiplication. All this does is flip the sign on the w (scalar) portion of the vector.
		 */
		private static readonly Vector4f nwIdentity = new Vector4f( 1, 1, 1, -1);
		
		/* used to find the additive inverse of a Quaternion.
		 */
		private static readonly Vector4f nIdentity = new Vector4f(-1, -1, -1, -1);
		
		private static readonly Vector4f doubleIdentity = new Vector4f(2, 2, 2, 2);
		
		/*private static readonly Vector4f nyzIdentity = new Vector4f(1, -1, -1, 1);
		
		private static readonly Vector4f nxzIdentity = new Vector4f(-1, 1, -1, 1);
		
		private static readonly Vector4f nxyIdentity = new Vector4f(-1, -1, 1, 1);
		
		private static readonly Vector4f zero = new Vector4f(0, 0, 0, 0);
		
		private static readonly Vector4f identiy =  new Vector4f(1, 1, 1, 1);
		
		private static readonly Vector4f xyzAbs = new Vector4f(~(0x1<<31), 0x7fffffffffffffff,
		                                                       0x7fffffffffffffff, ~0x0
		                                                      );
		private static readonly Vector4f xyzSign = new Vector4f(~0x7fffffffffffffff, ~0x7fffffffffffffff,
		                                                        ~0x7fffffffffffffff, 0
		                                                      );*/
		
#endif
		
		/*
		 * Note order here was changed to match the order of Vector4f. This makes it much easer to follow
		 * what is going on, and removes one SEE2 suffle operation from the Quaternion multiply code.
		 * See the explicit cast operators near the bottem of this file.
		 */
        public float x, y, z, w;

        private static readonly Quaternion identityQuat = new Quaternion( 1.0f, 0.0f, 0.0f, 0.0f );
        private static readonly Quaternion zeroQuat = new Quaternion( 0.0f, 0.0f, 0.0f, 0.0f );
        private static readonly int[] next = new int[ 3 ] { 1, 2, 0 };

        #endregion

        #region Constructors

        //		public Quaternion()
        //		{
        //			this.w = 1.0f;
        //		}

        /// <summary>
        ///		Creates a new Quaternion.
        /// </summary>
        public Quaternion( float w, float x, float y, float z )
        {
            this.w = w;
            this.x = x;
            this.y = y;
            this.z = z;
        }

        #endregion

        #region Operator Overloads + CLS compliant method equivalents

        /// <summary>
        /// Used to multiply 2 Quaternions together.
        /// </summary>
        /// <remarks>
        ///		Quaternion multiplication is not communative in most cases.
        ///		i.e. p*q != q*p
        /// </remarks>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static Quaternion Multiply( Quaternion left, Quaternion right )
        {
            return left * right;
        }

        /// <summary>
        /// Used to multiply 2 Quaternions together.
        /// </summary>
        /// <remarks>
        ///		Quaternion multiplication is not communative in most cases.
        ///		i.e. p*q != q*p
        /// </remarks>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static Quaternion operator *( Quaternion left, Quaternion right )
        {
#if (MONO_SIMD)
			if (PlatformInformation.shuffle_accel){
				unsafe {
					Vector4f vRight = *(Vector4f*) &right;
					Vector4f vLeft = *(Vector4f*) &left;
					/*
					 * Warning: SSE2 shufle opperations are slightly hard to understand at first glance.
					 * However, they speed things up as the vector stays in the registers and doesn't go back and forth
					 * from memory to registers while swapping values.
					 * 
					 * Note: In SSE2 acceleration these suffle opperations get directly mapped to single opcodes.
					 */
					
					*(Vector4f*) &right = (Vector4f.Shuffle(vLeft, ShuffleSel.ExpandW) *
					                     vRight +
					                     
					                     nwIdentity *
					                     Vector4f.Shuffle(vLeft, ShuffleSel.XFromX | ShuffleSel.YFromY | ShuffleSel.ZFromZ | ShuffleSel.WFromX  ) *
					                     Vector4f.Shuffle(vRight, ShuffleSel.XFromW | ShuffleSel.YFromW | ShuffleSel.ZFromW  | ShuffleSel.WFromX ) +
					                     
					                     nwIdentity *
					                     Vector4f.Shuffle(vLeft, ShuffleSel.XFromY | ShuffleSel.YFromZ | ShuffleSel.ZFromX | ShuffleSel.WFromY ) *
					                     Vector4f.Shuffle(vRight, ShuffleSel.XFromZ | ShuffleSel.YFromX | ShuffleSel.ZFromY | ShuffleSel.WFromY ) -
					                     
					                     Vector4f.Shuffle(vLeft,  ShuffleSel.XFromZ | ShuffleSel.YFromX | ShuffleSel.ZFromY |ShuffleSel.WFromZ  ) *
					                     Vector4f.Shuffle(vRight, ShuffleSel.XFromY | ShuffleSel.YFromZ | ShuffleSel.ZFromX | ShuffleSel.WFromZ )
					                    );
				}
				return right;
				                     	                     
			} else if (PlatformInformation.general_accel) {
				Vector4f vl1 = new Vector4f(left.w , left.w, left.w, left.w);
				unsafe {
					Vector4f vr1 = *(Vector4f*) &right;
					
					/* remember w is last and x is first in Vector4f
					 * 
					 * x - b*c
					 * x + (-1) * b * c
					 * x + ((-1) * b) * c
					 */
					Vector4f vl2 = new Vector4f(left.x, left.y, left.z, -left.x);
					Vector4f vr2 = new Vector4f(right.w, right.w, right.w, right.x);
					
					Vector4f vl3 = new Vector4f(left.y, left.z, left.x, -left.y);
					Vector4f vr3 = new Vector4f(right.z, right.x, right.y, right.y);
					
					Vector4f vl4 = new Vector4f(left.z, left.x, left.y, left.z);
					Vector4f vr4 = new Vector4f(right.y, right.z, right.x, right.z);
					
					*(Vector4f*) &right = (vl1 * vr1 + vl2 * vr2 + vl3 * vr3 - vl4 * vr4);  
				}
				return right;
			}
			else
#endif
			{
	            Quaternion q = new Quaternion();			
				

	            q.x = left.w * right.x + left.x * right.w + left.y * right.z - left.z * right.y;
	            q.y = left.w * right.y + left.y * right.w + left.z * right.x - left.x * right.z;
	            q.z = left.w * right.z + left.z * right.w + left.x * right.y - left.y * right.x;
				/* scaler part */
				q.w = left.w * right.w - left.x * right.x - left.y * right.y - left.z * right.z;

	            /*
	            return new Quaternion
	                (
	                left.w * right.w - left.x * right.x - left.y * right.y - left.z * right.z,
	                left.w * right.x + left.x * right.w + left.y * right.z - left.z * right.y,
	                left.w * right.y + left.y * right.w + left.z * right.x - left.x * right.z,
	                left.w * right.z + left.z * right.w + left.x * right.y - left.y * right.x
	                ); */

	            return q;			
			}
				
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="quat"></param>
        /// <param name="vector"></param>
        /// <returns></returns>
        public static Vector3 Multiply( Quaternion quat, Vector3 vector )
        {
            return quat * vector;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="quat"></param>
        /// <param name="vector"></param>
        /// <returns></returns>
        public static Vector3 operator *(Quaternion quat, Vector3 vector )
        {
            // nVidia SDK implementation
            Vector3 uv, uuv;
            Vector3 qvec = new Vector3( quat.x, quat.y, quat.z );

            uv = qvec.Cross( vector );
            uuv = qvec.Cross( uv );
            uv *= ( 2.0f * quat.w );
            uuv *= 2.0f;

            return vector + uv + uuv;

            // get the rotation matrix of the Quaternion and multiply it times the vector
            //return quat.ToRotationMatrix() * vector;
        }

        /// <summary>
        /// Used when a float value is multiplied by a Quaternion.
        /// </summary>
        /// <param name="scalar"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static Quaternion Multiply( float scalar, Quaternion right )
        {
            return scalar * right;
        }

        /// <summary>
        /// Used when a float value is multiplied by a Quaternion.
        /// </summary>
        /// <param name="scalar"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static Quaternion operator *( float scalar, Quaternion right )
        {
#if (MONO_SIMD)
			/* if (Utility.ACCELERATED != Acceleration.NONE) */
			{
				unsafe {
					//TODO use a "new Vector4f(float)" or "Vector4f.ExpandX(Vector4f)"
        			//TODO when they come avalible.
        			Vector4f scalarV;
        			(*(float*) &scalarV) = scalar;
        			scalarV = Vector4f.InterleaveLow(scalarV, scalarV);
        			scalarV = Vector4f.InterleaveLow(scalarV, scalarV);
					
					*(Vector4f*) &right *= scalarV;
				}
				return right;
			}
			/* else */
#else				
			{
				return new Quaternion( scalar * right.w, scalar * right.x, scalar * right.y, scalar * right.z );
			}
#endif
        }

        /// <summary>
        /// Used when a Quaternion is multiplied by a float value.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="scalar"></param>
        /// <returns></returns>
        public static Quaternion Multiply( Quaternion left, float scalar )
        {
            return left * scalar;
        }

        /// <summary>
        /// Used when a Quaternion is multiplied by a float value.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="scalar"></param>
        /// <returns></returns>
        public static Quaternion operator *( Quaternion left, float scalar )
        {
#if (MONO_SIMD)
			/*if (Utility.ACCELERATED != Acceleration.NONE) */
			 {
				unsafe {
					//TODO use a "new Vector4f(float)" or "Vector4f.ExpandX(Vector4f)"
        			//TODO when they come avalible.
        			Vector4f scalarV;
        			(*(float*) &scalarV) = scalar;
        			scalarV = Vector4f.InterleaveLow(scalarV, scalarV);
        			scalarV = Vector4f.InterleaveLow(scalarV, scalarV);
					*(Vector4f*) &left *= scalarV;
				}
				return left;
			}
			/* else */			
#else			
			{ 
				
				return new Quaternion( scalar * left.w, scalar * left.x, scalar * left.y, scalar * left.z );
			}
#endif
        }

        /// <summary>
        /// Used when a Quaternion is added to another Quaternion.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static Quaternion Add( Quaternion left, Quaternion right )
        {
            return left + right;
        }

        public static Quaternion Subtract( Quaternion left, Quaternion right )
        {
            return left - right;
        }

        /// <summary>
        /// Used when a Quaternion is added to another Quaternion.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static Quaternion operator +( Quaternion left, Quaternion right )
        {
#if (MONO_SIMD)
			/* if (Utility.ACCELERATED != Acceleration.NONE) */
			{
				unsafe {
					*(Vector4f*) &left += *(Vector4f*) &right;
				}
				return left;
			}
			/* else */
#else			
			{ 

				return new Quaternion( left.w + right.w, left.x + right.x, left.y + right.y, left.z + right.z );
			}
#endif
        }

        public static Quaternion operator -( Quaternion left, Quaternion right )
        {
#if (MONO_SIMD)
			/*if (Utility.ACCELERATED != Acceleration.NONE) */
			  {
				unsafe {
					*(Vector4f*) &left -= *(Vector4f*) &right;
				}
				return left;
			}
			/* else */
#else		
			{
				return new Quaternion( left.w - right.w, left.x - right.x, left.y - right.y, left.z - right.z );
			}
#endif
        }

        /// <summary>
        ///     Negates a Quaternion, which simply returns a new Quaternion
        ///     with all components negated.
        /// </summary>
        /// <param name="right"></param>
        /// <returns></returns>
        public static Quaternion operator -( Quaternion right )
        {
#if (MONO_SIMD)
			/*if (Utility.ACCELERATED != Acceleration.NONE) */
			{
				unsafe {
					*(Vector4f*) &right *= nIdentity;
				}
				return right;
			}			
			/* else */
#else			
			{

                return new Quaternion( -right.w, -right.x, -right.y, -right.z );
			}
#endif
        }

        public static bool operator ==( Quaternion left, Quaternion right )
        {
            return ( left.w == right.w && left.x == right.x && left.y == right.y && left.z == right.z );
        }

        public static bool operator !=( Quaternion left, Quaternion right )
        {
            return !( left == right );
        }

        #endregion

        #region Properties

        /// <summary>
        ///    An Identity Quaternion.
        /// </summary>
        public static Quaternion Identity
        {
            get
            {
                return identityQuat;
            }
        }

        /// <summary>
        ///    A Quaternion with all elements set to 0.0f;
        /// </summary>
        public static Quaternion Zero
        {
            get
            {
                return zeroQuat;
            }
        }

        /// <summary>
        ///		Squared 'length' of this quaternion.
        /// </summary>
        public float Norm
        {
            get
            {
#if (MONO_SIMD)
				if (PlatformInformation.horizontal_add_sub_accel)
				{		
	                Vector4f temp;
	                unsafe {
	                	fixed (Quaternion* thisP = &this){
	                		temp = *(Vector4f*) thisP;
	                	}
	                }
					temp *= temp;
					temp = Vector4f.HorizontalAdd(temp, temp); /* x + y, z + w , [ignore], [ignore] */
					temp = Vector4f.HorizontalAdd(temp, temp); /* (x + y) + (z + w), [ignore], [ignore], [ignore] */
					return temp.X;
				}
				else if (PlatformInformation.general_accel)
				{			
	                Vector4f temp;
	                unsafe {
	                	fixed (Quaternion* thisP = &this){
	                		temp = *(Vector4f*) thisP;
	                	}
	                }
					temp *= temp;
					return temp.X + temp.Y + temp.Z + temp.W;
				}
				else 
#endif				
				{
					return x * x + y * y + z * z + w * w;
				}
            }
        }

        /// <summary>
        ///    Local X-axis portion of this rotation.
        /// </summary>
        public Vector3 XAxis
        {
            get
            {
                float fTx = 2.0f * x;
                float fTy = 2.0f * y;
                float fTz = 2.0f * z;
				
                float fTwy = fTy * w;
                float fTwz = fTz * w;
                float fTxy = fTy * x;
				
                float fTxz = fTz * x;
                float fTyy = fTy * y;
                float fTzz = fTz * z;

                return new Vector3( 1.0f - ( fTyy + fTzz ), fTxy + fTwz, fTxz - fTwy );
            }
        }

        /// <summary>
        ///    Local Y-axis portion of this rotation.
        /// </summary>
        public Vector3 YAxis
        {
            get
            {
                float fTx = 2.0f * x;
                float fTy = 2.0f * y;
                float fTz = 2.0f * z;
                float fTwx = fTx * w;
                float fTwz = fTz * w;
                float fTxx = fTx * x;
                float fTxy = fTy * x;
                float fTyz = fTz * y;
                float fTzz = fTz * z;

                return new Vector3( fTxy - fTwz, 1.0f - ( fTxx + fTzz ), fTyz + fTwx );
            }
        }

        /// <summary>
        ///    Local Z-axis portion of this rotation.
        /// </summary>
        public Vector3 ZAxis
        {
            get
            {
                float fTx = 2.0f * x;
                float fTy = 2.0f * y;
                float fTz = 2.0f * z;
                float fTwx = fTx * w;
                float fTwy = fTy * w;
                float fTxx = fTx * x;
                float fTxz = fTz * x;
                float fTyy = fTy * y;
                float fTyz = fTz * y;

                return new Vector3( fTxz + fTwy, fTyz - fTwx, 1.0f - ( fTxx + fTyy ) );
            }
        }

        #endregion

        #region Static methods

        public static Quaternion Slerp( float time, Quaternion quatA, Quaternion quatB )
        {
            return Slerp( time, quatA, quatB, false );
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="time"></param>
        /// <param name="quatA"></param>
        /// <param name="quatB"></param>
        /// <param name="useShortestPath"></param>
        /// <returns></returns>
        public static Quaternion Slerp( float time, Quaternion quatA, Quaternion quatB, bool useShortestPath )
        {
            float cos = quatA.Dot( quatB );

            float angle = Utility.ACos( cos );

            if ( Utility.Abs( angle ) < EPSILON )
            {
                return quatA;
            }

            float sin = Utility.Sin( angle );
            float inverseSin = 1.0f / sin;
            float coeff0 = Utility.Sin( ( 1.0f - time ) * angle ) * inverseSin;
            float coeff1 = Utility.Sin( time * angle ) * inverseSin;

            Quaternion result;

            if ( cos < 0.0f && useShortestPath )
            {
                coeff0 = -coeff0;
                // taking the complement requires renormalisation
                Quaternion t = coeff0 * quatA + coeff1 * quatB;
                t.Normalize();
                result = t;
            }
            else
            {
                result = ( coeff0 * quatA + coeff1 * quatB );
            }

            return result;
        }

        /// <overloads><summary>
        /// normalised linear interpolation - faster but less accurate (non-constant rotation velocity)
        /// </summary>
        /// <param name="fT"></param>
        /// <param name="rkP"></param>
        /// <param name="rkQ"></param>
        /// <returns></returns>
        /// </overloads>
        public static Quaternion Nlerp( float fT, Quaternion rkP, Quaternion rkQ )
        {
            return Nlerp( fT, rkP, rkQ, false );
        }


        /// <param name="shortestPath"></param>
        public static Quaternion Nlerp( float fT, Quaternion rkP, Quaternion rkQ, bool shortestPath )
        {
            Quaternion result;
            float fCos = rkP.Dot( rkQ );
            if ( fCos < 0.0f && shortestPath )
            {
                result = rkP + fT * ( ( -rkQ ) - rkP );
            }
            else
            {
                result = rkP + fT * ( rkQ - rkP );

            }
            result.Normalize();
            return result;
        }

        /// <summary>
        /// Creates a Quaternion from a supplied angle and axis.
        /// </summary>
        /// <param name="angle">Value of an angle in radians.</param>
        /// <param name="axis">Arbitrary axis vector.</param>
        /// <returns></returns>
        public static Quaternion FromAngleAxis( float angle, Vector3 axis )
        {
            Quaternion quat = new Quaternion();

            float halfAngle = 0.5f * angle;
            float sin = Utility.Sin( halfAngle );

            quat.w = Utility.Cos( halfAngle );
            quat.x = sin * axis.x;
            quat.y = sin * axis.y;
            quat.z = sin * axis.z;

            return quat;
        }

        public static Quaternion Squad( float t, Quaternion p, Quaternion a, Quaternion b, Quaternion q )
        {
            return Squad( t, p, a, b, q, false );
        }

        /// <summary>
        ///		Performs spherical quadratic interpolation.
        /// </summary>
        /// <param name="t"></param>
        /// <param name="p"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="q"></param>
        /// <returns></returns>
        public static Quaternion Squad( float t, Quaternion p, Quaternion a, Quaternion b, Quaternion q, bool useShortestPath )
        {
            float slerpT = 2.0f * t * ( 1.0f - t );

            // use spherical linear interpolation
            Quaternion slerpP = Slerp( t, p, q, useShortestPath );
            Quaternion slerpQ = Slerp( t, a, b );

            // run another Slerp on the results of the first 2, and return the results
            return Slerp( slerpT, slerpP, slerpQ );
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Performs a Dot Product operation on 2 Quaternions.
        /// </summary>
        /// <param name="quat"></param>
        /// <returns></returns>
        public float Dot( Quaternion quat )
        {
#if (MONO_SIMD)
			if (PlatformInformation.horizontal_add_sub_accel)
			{		
                Vector4f temp;
                unsafe {
                	fixed (Quaternion* thisP = &this){
                		temp = *(Vector4f*) thisP;
                		temp *= (*(Vector4f*) &quat);
                	}
                }	
				temp = Vector4f.HorizontalAdd(temp, temp); /* x + y, z + w , [ignore], [ignore] */
				temp = Vector4f.HorizontalAdd(temp, temp); /* (x + y) + (z + w), [ignore], [ignore], [ignore] */
				return temp.X;
			}
			else if (PlatformInformation.general_accel)
			{
				Vector4f temp;
                unsafe {
                	fixed (Quaternion* thisP = &this){
                		temp = *(Vector4f*) thisP;
                		temp *= (*(Vector4f*) &quat);
                	}
                }		
				return temp.X + temp.Y + temp.Z + temp.W;
			}
			else 
#endif			
			{
				return x * quat.x + y * quat.y + z * quat.z + w * quat.w;
			}
        }

        /// <summary>
        ///		Normalizes elements of this quaterion to the range [0,1].
        /// </summary>
        public void Normalize()
        {
            float factor = 1.0f / Utility.Sqrt( this.Norm );

            w = w * factor;
            x = x * factor;
            y = y * factor;
            z = z * factor;
        }

        /// <summary>
        ///    
        /// </summary>
        /// <param name="angle"></param>
        /// <param name="axis"></param>
        /// <returns></returns>
        public void ToAngleAxis( ref float angle, ref Vector3 axis )
        {
            // The quaternion representing the rotation is
            //   q = cos(A/2)+sin(A/2)*(x*i+y*j+z*k)

            float sqrLength = x * x + y * y + z * z;

            if ( sqrLength > 0.0f )
            {
                angle = 2.0f * Utility.ACos( w );
                float invLength = Utility.InvSqrt( sqrLength );
                axis.x = x * invLength;
                axis.y = y * invLength;
                axis.z = z * invLength;
            }
            else
            {
                angle = 0.0f;
                axis.x = 1.0f;
                axis.y = 0.0f;
                axis.z = 0.0f;
            }
        }

        /// <summary>
        /// Gets a 3x3 rotation matrix from this Quaternion.
        /// </summary>
        /// <returns></returns>
        public Matrix3 ToRotationMatrix()
        {
            Matrix3 rotation = new Matrix3();

            float tx = 2.0f * this.x;
            float ty = 2.0f * this.y;
            float tz = 2.0f * this.z;
            float twx = tx * this.w;
            float twy = ty * this.w;
            float twz = tz * this.w;
            float txx = tx * this.x;
            float txy = ty * this.x;
            float txz = tz * this.x;
            float tyy = ty * this.y;
            float tyz = tz * this.y;
            float tzz = tz * this.z;

            rotation.m00 = 1.0f - ( tyy + tzz );
            rotation.m01 = txy - twz;
            rotation.m02 = txz + twy;
            rotation.m10 = txy + twz;
            rotation.m11 = 1.0f - ( txx + tzz );
            rotation.m12 = tyz - twx;
            rotation.m20 = txz - twy;
            rotation.m21 = tyz + twx;
            rotation.m22 = 1.0f - ( txx + tyy );

            return rotation;
        }

        /// <summary>
        /// Computes the inverse of a Quaternion.
        /// </summary>
        /// <returns></returns>
        public Quaternion Inverse()
        {
            float norm = this.w * this.w + this.x * this.x + this.y * this.y + this.z * this.z;
            if ( norm > 0.0f )
            {
                float inverseNorm = 1.0f / norm;
                return new Quaternion( this.w * inverseNorm, -this.x * inverseNorm, -this.y * inverseNorm, -this.z * inverseNorm );
            }
            else
            {
                // return an invalid result to flag the error
                return Quaternion.Zero;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="xAxis"></param>
        /// <param name="yAxis"></param>
        /// <param name="zAxis"></param>
        public void ToAxes( out Vector3 xAxis, out Vector3 yAxis, out Vector3 zAxis )
        {
            xAxis = new Vector3();
            yAxis = new Vector3();
            zAxis = new Vector3();

            Matrix3 rotation = this.ToRotationMatrix();

            xAxis.x = rotation.m00;
            xAxis.y = rotation.m10;
            xAxis.z = rotation.m20;

            yAxis.x = rotation.m01;
            yAxis.y = rotation.m11;
            yAxis.z = rotation.m21;

            zAxis.x = rotation.m02;
            zAxis.y = rotation.m12;
            zAxis.z = rotation.m22;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="xAxis"></param>
        /// <param name="yAxis"></param>
        /// <param name="zAxis"></param>
        public void FromAxes( Vector3 xAxis, Vector3 yAxis, Vector3 zAxis )
        {
            Matrix3 rotation = new Matrix3();

            rotation.m00 = xAxis.x;
            rotation.m10 = xAxis.y;
            rotation.m20 = xAxis.z;

            rotation.m01 = yAxis.x;
            rotation.m11 = yAxis.y;
            rotation.m21 = yAxis.z;

            rotation.m02 = zAxis.x;
            rotation.m12 = zAxis.y;
            rotation.m22 = zAxis.z;

            // set this quaternions values from the rotation matrix built
            FromRotationMatrix( rotation );
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="matrix"></param>
        public void FromRotationMatrix( Matrix3 matrix )
        {
        	
#if false
				
			{
				// Algorithem from 
				// http://www.euclideanspace.com/maths/geometry/rotations/conversions/matrixToQuaternion/index.htm
				// Modifed to be used on simd vectors
				unsafe {
					Vector4f left = new Vector4f					
					float trace = matrix.m00 + matrix.m11 + matrix.m22;
					if (trace > 0 )
					{
						Vector4f s;
						*(float*) &s = Utility.Sqrt(trace + 1);
						Vector4f.Shuffle(s, ShuffleSel.ExpandX);
					}
					this = *(Quaternion*) &res;
				}
            }
#else
            // Algorithm in Ken Shoemake's article in 1987 SIGGRAPH course notes
            // article "Quaternion Calculus and Fast Animation".

            float trace = matrix.m00 + matrix.m11 + matrix.m22;

            float root = 0.0f;

            if ( trace > 0.0f )
            {
                // |this.w| > 1/2, may as well choose this.w > 1/2
                root = Utility.Sqrt( trace + 1.0f );  // 2w
                this.w = 0.5f * root;

                root = 0.5f / root;  // 1/(4w)

                this.x = ( matrix.m21 - matrix.m12 ) * root;
                this.y = ( matrix.m02 - matrix.m20 ) * root;
                this.z = ( matrix.m10 - matrix.m01 ) * root;
            }
            else
            {
                // |this.w| <= 1/2

                int i = 0;
                if ( matrix.m11 > matrix.m00 )
                    i = 1;
                if ( matrix.m22 > matrix[ i, i ] )
                    i = 2;

                int j = next[ i ];
                int k = next[ j ];

                root = Utility.Sqrt( matrix[ i, i ] - matrix[ j, j ] - matrix[ k, k ] + 1.0f );

                unsafe
                {
                    fixed ( float* apkQuat = &this.x )
                    {
                        apkQuat[ i ] = 0.5f * root;
                        root = 0.5f / root;

                        this.w = ( matrix[ k, j ] - matrix[ j, k ] ) * root;

                        apkQuat[ j ] = ( matrix[ j, i ] + matrix[ i, j ] ) * root;
                        apkQuat[ k ] = ( matrix[ k, i ] + matrix[ i, k ] ) * root;
                    }
                }
            }
#endif
        }

        /// <summary>
        ///		Calculates the logarithm of a Quaternion.
        /// </summary>
        /// <returns></returns>
        public Quaternion Log()
        {
            // BLACKBOX: Learn this
            // If q = cos(A)+sin(A)*(x*i+y*j+z*k) where (x,y,z) is unit length, then
            // log(q) = A*(x*i+y*j+z*k).  If sin(A) is near zero, use log(q) =
            // sin(A)*(x*i+y*j+z*k) since sin(A)/A has limit 1.

            // start off with a zero quat
            Quaternion result = Quaternion.Zero;

            if ( Utility.Abs( w ) < 1.0f )
            {
                float angle = Utility.ACos( w );
                float sin = Utility.Sin( angle );

                if ( Utility.Abs( sin ) >= EPSILON )
                {
                    float coeff = angle / sin;
                    result.x = coeff * x;
                    result.y = coeff * y;
                    result.z = coeff * z;
                }
                else
                {
                    result.x = x;
                    result.y = y;
                    result.z = z;
                }
            }

            return result;
        }

        /// <summary>
        ///		Calculates the Exponent of a Quaternion.
        /// </summary>
        /// <returns></returns>
        public Quaternion Exp()
        {
            // If q = A*(x*i+y*j+z*k) where (x,y,z) is unit length, then
            // exp(q) = cos(A)+sin(A)*(x*i+y*j+z*k).  If sin(A) is near zero,
            // use exp(q) = cos(A)+A*(x*i+y*j+z*k) since A/sin(A) has limit 1.

            float angle = Utility.Sqrt( x * x + y * y + z * z );
            float sin = Utility.Sin( angle );

            // start off with a zero quat
            Quaternion result = Quaternion.Zero;

            result.w = Utility.Cos( angle );

            if ( Utility.Abs( sin ) >= EPSILON )
            {
                float coeff = sin / angle;

                result.x = coeff * x;
                result.y = coeff * y;
                result.z = coeff * z;
            }
            else
            {
                result.x = x;
                result.y = y;
                result.z = z;
            }

            return result;
        }

        #endregion

        #region Object overloads

        /// <summary>
        ///		Overrides the Object.ToString() method to provide a text representation of 
        ///		a Quaternion.
        /// </summary>
        /// <returns>A string representation of a Quaternion.</returns>
        public override string ToString()
        {
            return string.Format( "Quaternion({0}, {1}, {2}, {3})", this.x, this.y, this.z, this.w );
        }

        public override int GetHashCode()
        {
            return (int)x ^ (int)y ^ (int)z ^ (int)w;
        }
        public override bool Equals( object obj )
        {
            Quaternion quat = (Quaternion)obj;

            return quat == this;
        }
        public bool Equals( Quaternion rhs, float tolerance )
        {
            float fCos = Dot( rhs );
            float angle = Utility.ACos( fCos );

            return Utility.Abs( angle ) <= tolerance;
        }
#endregion
       
    }
}
