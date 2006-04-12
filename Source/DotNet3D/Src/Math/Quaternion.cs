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
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security.Permissions;

#endregion Namespace Declarations

namespace DotNet3D.Math
{
    /// <summary>
    ///	Implementation of a Quaternion, i.e. a rotation around an axis.
    /// </summary>
    [StructLayout( LayoutKind.Sequential )]
    public struct Quaternion : ISerializable
    {
        #region Fields and Properties

        /// <summary>W component.</summary>
        public Real w;
        /// <summary>X component.</summary>
        public Real x;
        /// <summary>Y component.</summary>
        public Real y;
        /// <summary>Z component.</summary>
        public Real z;

        /// <summary>An Identity Quaternion.</summary>
        public static readonly Quaternion Identity = new Quaternion( 1.0f, 0.0f, 0.0f, 0.0f );
        /// <summary>A Quaternion with all elements set to 0.0;</summary>
        public static readonly Quaternion Zero = new Quaternion( 0.0f, 0.0f, 0.0f, 0.0f );

        private static readonly int[] _next = new int[ 3 ] { 1, 2, 0 };
        private static readonly Real _epsilon = new Real( 1E-03f );

        /// <summary>
        ///		Squared 'length' of this quaternion.
        /// </summary>
        public Real Norm
        {
            get
            {
                return x * x + y * y + z * z + w * w;
            }
        }

        /// <summary>
        ///    Local X-axis portion of this rotation.
        /// </summary>
        public Vector3 XAxis
        {
            get
            {
                Real fTx = 2.0f * x;
                Real fTy = 2.0f * y;
                Real fTz = 2.0f * z;
                Real fTwy = fTy * w;
                Real fTwz = fTz * w;
                Real fTxy = fTy * x;
                Real fTxz = fTz * x;
                Real fTyy = fTy * y;
                Real fTzz = fTz * z;

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
                Real fTx = 2.0f * x;
                Real fTy = 2.0f * y;
                Real fTz = 2.0f * z;
                Real fTwx = fTx * w;
                Real fTwz = fTz * w;
                Real fTxx = fTx * x;
                Real fTxy = fTy * x;
                Real fTyz = fTz * y;
                Real fTzz = fTz * z;

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
                Real fTx = 2.0f * x;
                Real fTy = 2.0f * y;
                Real fTz = 2.0f * z;
                Real fTwx = fTx * w;
                Real fTwy = fTy * w;
                Real fTxx = fTx * x;
                Real fTxz = fTz * x;
                Real fTyy = fTy * y;
                Real fTyz = fTz * y;

                return new Vector3( fTxz + fTwy, fTyz - fTwx, 1.0f - ( fTxx + fTyy ) );
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public Radian Roll
        {
            get
            {
                return Utility.ATan( 2 * ( x * y + w * z ), w * w + x * x - y * y - z * z );
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public Radian Pitch
        {
            get
            {
                return Utility.ATan( 2 * ( y * z + w * x ), w * w - x * x - y * y + z * z );
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public Radian Yaw
        {
            get
            {
                return Utility.ASin( -2 * ( x * z - w * y ) );
            }
        }

        #endregion Fields and Properties

        #region Constructors

        //NOTE: ISerializable Constructor in ISerializable Implementation

        /// <overloads>
        /// <summary>
        /// Creates a new Quaternion.
        /// </summary>
        /// </overloads>
        /// <param name="source">the source vector.</param>
        public Quaternion( Quaternion source )
        {
            this.w = source.w;
            this.x = source.x;
            this.y = source.y;
            this.z = source.z;
        }

        /// <param name="w"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        public Quaternion( Real w, Real x, Real y, Real z )
        {
            this.w = w;
            this.x = x;
            this.y = y;
            this.z = z;
        }

        /// <summary>
        /// Construct a quaternion from an rotational matrix
        /// </summary>
        /// <param name="rot"></param>
        public Quaternion( Matrix3 rot )
            : this( 0, 0, 0, 0 )
        {
            FromRotationMatrix( rot );
        }

        // 
        /// <summary>
        /// Construct a quaternion from an angle/axis
        /// </summary>
        /// <param name="angle"></param>
        /// <param name="axis"></param>
        public Quaternion( Radian angle, Vector3 axis )
            : this( 0, 0, 0, 0 )
        {
            FromAngleAxis( angle, axis );
        }

        /// <summary>
        /// Construct a quaternion from 3 orthonormal local axes
        /// </summary>
        /// <param name="xaxis"></param>
        /// <param name="yaxis"></param>
        /// <param name="zaxis"></param>
        public Quaternion( Vector3 xaxis, Vector3 yaxis, Vector3 zaxis )
            : this( 0, 0, 0, 0 )
        {
            FromAxes( xaxis, yaxis, zaxis );
        }

        /// <summary>
        /// Construct a quaternion from 3 orthonormal local axes
        /// </summary>
        /// <param name="axis"></param>
        public Quaternion( Vector3[] axis )
            : this( 0, 0, 0, 0 )
        {
            FromAxes( axis );
        }

        #endregion

        #region Static methods

        /// <summary>
        /// 
        /// </summary>
        /// <param name="time"></param>
        /// <param name="quatA"></param>
        /// <param name="quatB"></param>
        /// <returns></returns>
        public static Quaternion Slerp( Real time, Quaternion quatA, Quaternion quatB )
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
        public static Quaternion Slerp( Real time, Quaternion quatA, Quaternion quatB, bool useShortestPath )
        {
            Real cos = quatA.DotProduct( quatB );

            Radian angle = Utility.ACos( cos );

            if ( Utility.Abs( angle ) < Real.Epsilon )
            {
                return quatA;
            }

            Real sin = Utility.Sin( angle );
            Real inverseSin = 1.0f / sin;
            Real coeff0 = Utility.Sin( ( 1.0f - time ) * angle ) * inverseSin;
            Real coeff1 = Utility.Sin( time * angle ) * inverseSin;

            Quaternion result;

            if ( cos < Real.Zero && useShortestPath )
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="time"></param>
        /// <param name="quatA"></param>
        /// <param name="quatB"></param>
        /// <param name="extraSpins"></param>
        /// <returns></returns>
        public static Quaternion SlerpExtraSpins( Real time, Quaternion quatA, Quaternion quatB, int extraSpins )
        {
            Real fCos = quatA.DotProduct( quatB );
            Radian fAngle = new Radian( Utility.ACos( fCos ) );

            if ( Utility.Abs( fAngle ) < _epsilon )
                return quatA;

            Real fSin = Utility.Sin( fAngle );
            Radian fPhase = new Radian( Utility.PI * extraSpins * time );
            Real fInvSin = 1.0 / fSin;
            Real fCoeff0 = Utility.Sin( ( 1.0 - time ) * fAngle - fPhase ) * fInvSin;
            Real fCoeff1 = Utility.Sin( time * fAngle + fPhase ) * fInvSin;

            return fCoeff0 * quatA + fCoeff1 * quatB;
        }

        /// <overloads><summary>
        /// normalised linear interpolation - faster but less accurate (non-constant rotation velocity)
        /// </summary>
        /// <param name="fT"></param>
        /// <param name="rkP"></param>
        /// <param name="rkQ"></param>
        /// <returns></returns>
        /// </overloads>
        public static Quaternion Nlerp( Real fT, Quaternion rkP, Quaternion rkQ )
        {
            return Nlerp( fT, rkP, rkQ, false );
        }


        /// <param name="shortestPath"></param>
        public static Quaternion Nlerp( Real fT, Quaternion rkP, Quaternion rkQ, bool shortestPath )
        {
            Quaternion result;
            Real fCos = rkP.DotProduct( rkQ );
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
        /// setup for spherical quadratic interpolation
        /// </summary>
        /// <param name="rkQ0"></param>
        /// <param name="rkQ1"></param>
        /// <param name="rkQ2"></param>
        /// <param name="rka"></param>
        /// <param name="rkB"></param>
        public static void Intermediate( Quaternion rkQ0, Quaternion rkQ1, Quaternion rkQ2, out Quaternion rkA, out Quaternion rkB )
        {
            // assert:  q0, q1, q2 are unit quaternions

            Quaternion kQ0inv = rkQ0.UnitInverse();
            Quaternion kQ1inv = rkQ1.UnitInverse();
            Quaternion rkP0 = kQ0inv * rkQ1;
            Quaternion rkP1 = kQ1inv * rkQ2;
            Quaternion kArg = 0.25 * ( rkP0.Log() - rkP1.Log() );
            Quaternion kMinusArg = -kArg;

            rkA = rkQ1 * kArg.Exp();
            rkB = rkQ1 * kMinusArg.Exp();
        }

        /// <overloads>
        /// <summary>
        ///		Performs spherical quadratic interpolation.
        /// </summary>
        /// <param name="t"></param>
        /// <param name="p"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="q"></param>
        /// </overloads>
        /// <returns></returns>
        public static Quaternion Squad( Real t, Quaternion p, Quaternion a, Quaternion b, Quaternion q )
        {
            return Squad( t, p, a, b, q, false );
        }

        /// <param name="useShortestPath"></param>
        public static Quaternion Squad( Real t, Quaternion p, Quaternion a, Quaternion b, Quaternion q, bool useShortestPath )
        {
            Real slerpT = 2.0f * t * ( 1.0f - t );

            // use spherical linear interpolation
            Quaternion slerpP = Slerp( t, p, q, useShortestPath );
            Quaternion slerpQ = Slerp( t, a, b );

            // run another Slerp on the results of the first 2, and return the results
            return Slerp( slerpT, slerpP, slerpQ );
        }


        #endregion Static Methods

        #region System.Object Implementation

        /// <overrides>
        /// <summary>
        ///		Overrides the Object.ToString() method to provide a text representation of 
        ///		a Quaternion.
        /// </summary>
        /// <returns>A string representation of a Quaternion.</returns>
        /// </overrides>
        public override string ToString()
        {
            return string.Format( "({0}, {1}, {2}, {3})", this.w, this.x, this.y, this.z );
        }

        /// <param name="decimalPlaces">number of decimal places to render</param>
        public string ToString( int decimalPlaces )
        {
            string format = "";

            format = format.PadLeft( decimalPlaces, '#' );
            format = "({0:0." + format + "}, {1:0." + format + "}, {2:0." + format + "}, {3:0." + format + "})";
            //NOTE: Explicit conversion used here to get proper behavior, for some reason it left as Real it will always 
            //      display all decimal places
            return string.Format( format, (float)this.x, (float)this.y, (float)this.z, (float)this.w );
        }

        /// <summary>
        ///		Provides a unique hash code based on the member variables of this
        ///		class.  This should be done because the equality operators (==, !=)
        ///		have been overriden by this class.
        ///		<p/>
        ///		The standard implementation is a simple XOR operation between all local
        ///		member variables.
        /// </summary>
        /// <returns>a unique code to represent this object</returns>
        public override int GetHashCode()
        {
            return w.GetHashCode() ^ x.GetHashCode() ^ y.GetHashCode() ^ z.GetHashCode();
        }

        /// <overloads>
        /// <summary>
        ///		Compares this Vector to another object. This should be done because the 
        ///		equality operators (==, !=) have been overriden by this class.
        /// </summary>
        /// </overloads>
        /// <param name="obj">object to compare to</param>
        /// <returns>true or false</returns>
        public override bool Equals( object obj )
        {
            return obj is Quaternion && this == (Quaternion)obj;
        }

        /// <param name="right"></param>
        /// <param name="tolerance"></param>
        /// <returns></returns>
        public bool Equals( Quaternion right, Real tolerance )
        {
            Real fCos = DotProduct( right );
            Radian angle = Utility.ACos( fCos );

            return Utility.Abs( angle ) <= tolerance;
        }

        #endregion System.Object Implementation

        #region Operator Overloads

        /// <summary>
        /// Used when a Quaternion is added to another Quaternion.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static Quaternion operator +( Quaternion left, Quaternion right )
        {
            return new Quaternion( left.w + right.w, left.x + right.x, left.y + right.y, left.z + right.z );
        }

        /// <summary>
        ///     Negates a Quaternion, which simply returns a new Quaternion
        ///     with all components negated.
        /// </summary>
        /// <param name="right"></param>
        /// <returns></returns>
        public static Quaternion operator -( Quaternion right )
        {
            return new Quaternion( -right.w, -right.x, -right.y, -right.z );
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static Quaternion operator -( Quaternion left, Quaternion right )
        {
            return new Quaternion( left.w - right.w, left.x - right.x, left.y - right.y, left.z - right.z );
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
            Quaternion q = new Quaternion();

            q.w = left.w * right.w - left.x * right.x - left.y * right.y - left.z * right.z;
            q.x = left.w * right.x + left.x * right.w + left.y * right.z - left.z * right.y;
            q.y = left.w * right.y + left.y * right.w + left.z * right.x - left.x * right.z;
            q.z = left.w * right.z + left.z * right.w + left.x * right.y - left.y * right.x;

            return q;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="quat"></param>
        /// <param name="vector"></param>
        /// <returns></returns>
        public static Vector3 operator *( Quaternion quat, Vector3 vector )
        {
            // nVidia SDK implementation
            Vector3 uv, uuv;
            Vector3 qvec = new Vector3( quat.x, quat.y, quat.z );

            uv = qvec.CrossProduct( vector );
            uuv = qvec.CrossProduct( uv );
            uv *= ( 2.0f * quat.w );
            uuv *= 2.0f;

            return vector + uv + uuv;

            // get the rotation matrix of the Quaternion and multiply it times the vector
            //return quat.ToRotationMatrix() * vector;
        }

        /// <summary>
        /// Used when a Quaternion is multiplied by a Real value.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="scalar"></param>
        /// <returns></returns>
        public static Quaternion operator *( Quaternion left, Real scalar )
        {
            return new Quaternion( scalar * left.w, scalar * left.x, scalar * left.y, scalar * left.z );
        }

        /// <summary>
        /// Used when a Real value is multiplied by a Quaternion.
        /// </summary>
        /// <param name="scalar"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static Quaternion operator *( Real scalar, Quaternion right )
        {
            return new Quaternion( scalar * right.w, scalar * right.x, scalar * right.y, scalar * right.z );
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator ==( Quaternion left, Quaternion right )
        {
            return ( left.w == right.w && left.x == right.x && left.y == right.y && left.z == right.z );
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator !=( Quaternion left, Quaternion right )
        {
            return ( left.w != right.w || left.x != right.x || left.y != right.y || left.z != right.z );
        }

        #region CLSCompliant Operator Methods

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

        /// <summary>
        ///     Negates a Quaternion, which simply returns a new Quaternion
        ///     with all components negated.
        /// </summary>
        /// <param name="right"></param>
        /// <returns></returns>
        public static Quaternion Negate( Quaternion right )
        {
            return -right;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static Quaternion Subtract( Quaternion left, Quaternion right )
        {
            return left - right;
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
        public static Quaternion Multiply( Quaternion left, Quaternion right )
        {
            return left * right;
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
        /// Used when a Quaternion is multiplied by a Real value.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="scalar"></param>
        /// <returns></returns>
        public static Quaternion Multiply( Quaternion left, Real scalar )
        {
            return left * scalar;
        }

        /// <summary>
        /// Used when a Real value is multiplied by a Quaternion.
        /// </summary>
        /// <param name="scalar"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static Quaternion Multiply( Real scalar, Quaternion right )
        {
            return scalar * right;
        }

        #endregion CLSCompliant Operator Methods

        #endregion Operator Overloads

        #region Public methods

        /// <summary>
        /// Performs a Dot Product operation on 2 Quaternions.
        /// </summary>
        /// <param name="quat"></param>
        /// <returns></returns>
        public Real DotProduct( Quaternion quat )
        {
            return this.w * quat.w + this.x * quat.x + this.y * quat.y + this.z * quat.z;
        }

        /// <summary>
        ///		Normalizes elements of this quaterion to the range [0,1].
        /// </summary>
        /// <returns>The previous length of the Quaternion</returns>
        public Real Normalize()
        {
            Real len = this.Norm;
            Real factor = Utility.InvSqrt( len );

            this *= factor;

            return len;
        }

        /// <summary>
        /// Computes the inverse of a Quaternion.
        /// </summary>
        /// <returns></returns>
        public Quaternion Inverse()
        {
            Real norm = this.w * this.w + this.x * this.x + this.y * this.y + this.z * this.z;
            if ( norm > 0.0f )
            {
                Real inverseNorm = 1.0f / norm;
                return new Quaternion( this.w * inverseNorm, -this.x * inverseNorm, -this.y * inverseNorm, -this.z * inverseNorm );
            }
            else
            {
                // return an invalid result to flag the error
                return Quaternion.Zero;
            }
        }

        /// <summary>
        /// Computes the unit inverse of a Quaternion.
        /// </summary>
        /// <returns></returns>
        public Quaternion UnitInverse()
        {
            // assert: this   is a unit length
            return new Quaternion( w, -x, -y, -z );
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
                Real angle = Utility.ACos( w );
                Real sin = Utility.Sin( angle );

                if ( Utility.Abs( sin ) >= Real.Epsilon )
                {
                    Real coeff = angle / sin;
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

            Real angle = Utility.Sqrt( x * x + y * y + z * z );
            Real sin = Utility.Sin( angle );

            // start off with a zero quat
            Quaternion result = Quaternion.Zero;

            result.w = Utility.Cos( angle );

            if ( Utility.Abs( sin ) >= Real.Epsilon )
            {
                Real coeff = sin / angle;

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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="matrix"></param>
        public void FromRotationMatrix( Matrix3 matrix )
        {
            // Algorithm in Ken Shoemake's article in 1987 SIGGRAPH course notes
            // article "Quaternion Calculus and Fast Animation".

            Real trace = matrix[ 0, 0 ] + matrix[ 1, 1 ] + matrix[ 2, 2 ];

            Real root = 0.0f;

            if ( trace > 0.0f )
            {
                // |this.w| > 1/2, may as well choose this.w > 1/2
                root = Utility.Sqrt( trace + 1.0f );  // 2w
                this.w = 0.5f * root;

                root = 0.5f / root;  // 1/(4w)

                this.x = ( matrix[ 2, 1 ] - matrix[ 1, 2 ] ) * root;
                this.y = ( matrix[ 0, 2 ] - matrix[ 2, 0 ] ) * root;
                this.z = ( matrix[ 1, 0 ] - matrix[ 0, 1 ] ) * root;
            }
            else
            {
                // |this.w| <= 1/2

                int i = 0;
                if ( matrix[ 1, 1 ] > matrix[ 0, 0 ] )
                    i = 1;
                if ( matrix[ 2, 2 ] > matrix[ i, i ] )
                    i = 2;

                int j = _next[ i ];
                int k = _next[ j ];

                root = Utility.Sqrt( matrix[ i, i ] - matrix[ j, j ] - matrix[ k, k ] + 1.0f );

                unsafe
                {
                    fixed ( Real* apkQuat = &this.x )
                    {
                        apkQuat[ i ] = 0.5f * root;
                        root = 0.5f / root;

                        this.w = ( matrix[ k, j ] - matrix[ j, k ] ) * root;

                        apkQuat[ j ] = ( matrix[ j, i ] + matrix[ i, j ] ) * root;
                        apkQuat[ k ] = ( matrix[ k, i ] + matrix[ i, k ] ) * root;
                    }
                }
            }
        }

        /// <summary>
        /// Gets a 3x3 rotation matrix from this Quaternion.
        /// </summary>
        /// <returns></returns>
        public Matrix3 ToRotationMatrix()
        {
            Matrix3 rotation = new Matrix3();

            Real tx = 2.0f * this.x;
            Real ty = 2.0f * this.y;
            Real tz = 2.0f * this.z;
            Real twx = tx * this.w;
            Real twy = ty * this.w;
            Real twz = tz * this.w;
            Real txx = tx * this.x;
            Real txy = ty * this.x;
            Real txz = tz * this.x;
            Real tyy = ty * this.y;
            Real tyz = tz * this.y;
            Real tzz = tz * this.z;

            rotation[ 0 ] = 1.0f - ( tyy + tzz );
            rotation[ 1 ] = txy - twz;
            rotation[ 2 ] = txz + twy;
            rotation[ 3 ] = txy + twz;
            rotation[ 4 ] = 1.0f - ( txx + tzz );
            rotation[ 5 ] = tyz - twx;
            rotation[ 6 ] = txz - twy;
            rotation[ 7 ] = tyz + twx;
            rotation[ 8 ] = 1.0f - ( txx + tyy );

            return rotation;
        }

        /// <summary>
        /// Creates a Quaternion from a supplied angle and axis.
        /// </summary>
        /// <param name="angle">Value of an angle in radians.</param>
        /// <param name="axis">Arbitrary axis vector.</param>
        /// <returns></returns>
        public static Quaternion FromAngleAxis( Radian angle, Vector3 axis )
        {
            Quaternion q = new Quaternion();
            Real halfAngle = (Real)0.5f * angle;
            Real sin = Utility.Sin( halfAngle );

            q.w = Utility.Cos( halfAngle );
            q.x = sin * axis.x;
            q.y = sin * axis.y;
            q.z = sin * axis.z;
            return q;
        }

        /// <summary>
        ///    
        /// </summary>
        /// <param name="angle"></param>
        /// <param name="axis"></param>
        /// <returns></returns>
        public void ToAngleAxis( ref Radian angle, ref Vector3 axis )
        {
            // The quaternion representing the rotation is
            //   q = cos(A/2)+sin(A/2)*(x*i+y*j+z*k)

            Real sqrLength = x * x + y * y + z * z;

            if ( sqrLength > 0.0f )
            {
                angle = (Real)2.0f * Utility.ACos( w );
                Real invLength = Utility.InvSqrt( sqrLength );
                axis.x = x * invLength;
                axis.y = y * invLength;
                axis.z = z * invLength;
            }
            else
            {
                angle = new Real( 0.0f );
                axis.x = new Real( 1.0f );
                axis.y = new Real( 0.0f );
                axis.z = new Real( 0.0f );
            }
        }

        /// <summary>
        /// Initializes the Quaternion from a single Vector3
        /// </summary>
        /// <param name="axis">the Vector3</param>
        public void FromAxes( Vector3[] axis )
        {
            Matrix3 rotation = new Matrix3();

            for ( int iCol = 0; iCol < 3; iCol++ )
            {
                rotation[ 0, iCol ] = axis[ iCol ].x;
                rotation[ 1, iCol ] = axis[ iCol ].y;
                rotation[ 2, iCol ] = axis[ iCol ].z;
            }

            // set this quaternions values from the rotation matrix built
            FromRotationMatrix( rotation );
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

            rotation[ 0, 0 ] = xAxis.x;
            rotation[ 1, 0 ] = xAxis.y;
            rotation[ 2, 0 ] = xAxis.z;

            rotation[ 0, 1 ] = yAxis.x;
            rotation[ 1, 1 ] = yAxis.y;
            rotation[ 2, 1 ] = yAxis.z;

            rotation[ 0, 2 ] = zAxis.x;
            rotation[ 1, 2 ] = zAxis.y;
            rotation[ 2, 2 ] = zAxis.z;

            // set this quaternions values from the rotation matrix built
            FromRotationMatrix( rotation );
        }

        /// <summary>
        /// Initializes the Quaternion from a single Vector3
        /// </summary>
        /// <param name="axis">the Vector3</param>
        public void ToAxes( out Vector3[] axis )
        {
            Matrix3 rot = ToRotationMatrix();

            axis = new Vector3[ 3 ];
            for ( int col = 0; col < 3; col++ )
            {
                axis[ col ].x = rot[ 0, col ];
                axis[ col ].y = rot[ 1, col ];
                axis[ col ].z = rot[ 2, col ];
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

            xAxis.x = rotation[ 0, 0 ];
            xAxis.y = rotation[ 1, 0 ];
            xAxis.z = rotation[ 2, 0 ];

            yAxis.x = rotation[ 0, 1 ];
            yAxis.y = rotation[ 1, 1 ];
            yAxis.z = rotation[ 2, 1 ];

            zAxis.x = rotation[ 0, 2 ];
            zAxis.y = rotation[ 1, 2 ];
            zAxis.z = rotation[ 2, 2 ];
        }



        #endregion

        #region ISerializable Implementation

        /// <summary>
        /// Deserialization contructor
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        private Quaternion( SerializationInfo info, StreamingContext context )
        {
            w = (Real)info.GetValue( "w", typeof( Real ) );
            x = (Real)info.GetValue( "x", typeof( Real ) );
            y = (Real)info.GetValue( "y", typeof( Real ) );
            z = (Real)info.GetValue( "z", typeof( Real ) );
        }

        /// <summary>
        /// Serialization Method
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        [SecurityPermission( SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter )]
        public void GetObjectData( SerializationInfo info, StreamingContext context )
        {
            info.AddValue( "w", w );
            info.AddValue( "x", x );
            info.AddValue( "y", y );
            info.AddValue( "z", z );
        }

        #endregion ISerializable Implementation

    }

    namespace Collections
    {
        using System.Collections.Generic;

        /// <summary>
        /// 
        /// </summary>
        public class QuaternionCollection : List<Quaternion>
        {
        }
    }
}
