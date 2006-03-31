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
    /// 4D homogeneous vector.
    /// </summary>
    [StructLayout( LayoutKind.Sequential )]
    public struct Vector4 : ISerializable
    {
        #region Fields and Properties

        /// <summary>X component.</summary>
        public Real x;
        /// <summary>Y component.</summary>
        public Real y;
        /// <summary>Z component.</summary>
        public Real z;
        /// <summary>W component.</summary>
        public Real w;

        /// <summary>Gets a Vector4 with all units set to positive infinity.</summary>
        public static readonly Vector4 PositiveInfinity = new Vector4( Real.PositiveInfinity, Real.PositiveInfinity, Real.PositiveInfinity, Real.PositiveInfinity );
        /// <summary>Gets a Vector4 with all units set to negative infinity.</summary>
        public static readonly Vector4 NegativeInfinity = new Vector4( Real.NegativeInfinity, Real.NegativeInfinity, Real.NegativeInfinity, Real.NegativeInfinity );
        /// <summary>Gets a Vector4 with all units set to Invalid.</summary>
        public static readonly Vector4 Invalid = new Vector4( Real.NaN, Real.NaN, Real.NaN, Real.NaN );
        /// <summary>Gets a Vector4 with all components set to 0.</summary>
        public static readonly Vector4 Zero = new Vector4( 0.0f, 0.0f, 0.0f, 0.0f );
        /// <summary>Gets a Vector4 with the X set to 1, and the others set to 0.</summary>
        public static readonly Vector4 UnitX = new Vector4( 1.0f, 0.0f, 0.0f, 0.0f );
        /// <summary>Gets a Vector4 with the Y set to 1, and the others set to 0.</summary>
        public static readonly Vector4 UnitY = new Vector4( 0.0f, 1.0f, 0.0f, 0.0f );
        /// <summary>Gets a Vector4 with the Z set to 1, and the others set to 0.</summary>
        public static readonly Vector4 UnitZ = new Vector4( 0.0f, 0.0f, 1.0f, 0.0f );
        /// <summary>Gets a Vector4 with the W set to 1, and the others set to 0.</summary>
        public static readonly Vector4 UnitW = new Vector4( 0.0f, 0.0f, 0.0f, 1.0f );
        /// <summary>Gets a Vector4 with the X set to -1, and the others set to 0.</summary>
        public static readonly Vector4 NegativeUnitX = new Vector4( -1.0f, 0.0f, 0.0f, 0.0f );
        /// <summary>Gets a Vector4 with the Y set to -1, and the others set to 0.</summary>
        public static readonly Vector4 NegativeUnitY = new Vector4( 0.0f, -1.0f, 0.0f, 0.0f );
        /// <summary>Gets a Vector4 with the Z set to -1, and the others set to 0.</summary>
        public static readonly Vector4 NegativeUnitZ = new Vector4( 0.0f, 0.0f, -1.0f, 0.0f );
        /// <summary>Gets a Vector4 with the W set to -1, and the others set to 0.</summary>
        public static readonly Vector4 NegativeUnitW = new Vector4( 0.0f, 0.0f, 0.0f, -1.0f );
        /// <summary>Gets a Vector4 with all components set to 1.</summary>
        public static readonly Vector4 Unit = new Vector4( 1.0f, 1.0f, 1.0f, 1.0f );

        /// <summary>Return True if the vector is the Positive Infinity Vector </summary>
        public bool IsPostiveInfinity
        {
            get
            {
                return this == Vector4.PositiveInfinity;
            }
        }

        /// <summary>Return True if the vector is the Negative Infinity Vector </summary>
        public bool IsNegativeInfinity
        {
            get
            {
                return this == Vector4.NegativeInfinity;
            }
        }

        /// <summary>Return True if the vector is the Invalid Vector </summary>
        public bool IsInvalid
        {
            get
            {
                return this == Vector4.Invalid;
            }
        }

        /// <summary>Return True if the vector is the Zero Vector </summary>
        public bool IsZero
        {
            get
            {
                return this == Vector4.Zero;
            }
        }

        /// <summary>Return True if the vector is the Unit X Vector </summary>
        public bool IsUnitX
        {
            get
            {
                return this == Vector4.UnitX;
            }
        }

        /// <summary>Return True if the vector is the UnitY Vector </summary>
        public bool IsUnitY
        {
            get
            {
                return this == Vector4.UnitY;
            }
        }

        /// <summary>Return True if the vector is the UnitZ Vector </summary>
        public bool IsUnitZ
        {
            get
            {
                return this == Vector4.UnitZ;
            }
        }

        /// <summary>Return True if the vector is the UnitW Vector </summary>
        public bool IsUnitW
        {
            get
            {
                return this == Vector4.UnitW;
            }
        }

        /// <summary>Return True if the vector is the NegativeUnitX Vector </summary>
        public bool IsNegativeUnitX
        {
            get
            {
                return this == Vector4.NegativeUnitX;
            }
        }

        /// <summary>Return True if the vector is the NegativeUnitY Vector </summary>
        public bool IsNegativeUnitY
        {
            get
            {
                return this == Vector4.NegativeUnitY;
            }
        }

        /// <summary>Return True if the vector is the NegativeUnitY Vector </summary>
        public bool IsNegativeUnitZ
        {
            get
            {
                return this == Vector4.NegativeUnitZ;
            }
        }

        /// <summary>Return True if the vector is the NegativeUnitW Vector </summary>
        public bool IsNegativeUnitW
        {
            get
            {
                return this == Vector4.NegativeUnitW;
            }
        }

        /// <summary>Return True if the vector is the Unit Vector </summary>
        public bool IsUnit
        {
            get
            {
                return this == Vector4.Unit;
            }
        }

        /// <summary>Return True if the vector is Normalized</summary>
        public bool IsNormalized
        {
            get
            {
                return this == Vector4.Zero;
            }
        }

        /// <summary>
        ///    Gets the length (magnitude) of this Vector4.  The Sqrt operation is expensive, so 
        ///    only use this if you need the exact length of the Vector.  If vector lengths are only going
        ///    to be compared, use LengthSquared instead.
        /// </summary>
        public Real Length
        {
            get
            {
                return Utility.Sqrt( this.x * this.x + this.y * this.y + this.z * this.z + this.w * this.w );
            }
        }

        /// <summary>
        ///    Returns the length (magnitude) of the vector squared.
        /// </summary>
        public Real LengthSquared
        {
            get
            {
                return ( this.x * this.x + this.y * this.y + this.z * this.z + this.w * this.w );
            }
        }

        #endregion Fields and Properties

        #region Constructors

        //NOTE: ISerializable Constructor in ISerializable Implementation

        /// <overloads>
        /// <summary>
        ///     Creates a new 4 dimensional Vector.
        /// </summary>
        /// </overloads>
        /// <param name="source">the source vector.</param>
        public Vector4( Vector4 source )
        {
            this.x = source.x;
            this.y = source.y;
            this.z = source.z;
            this.w = source.w;
        }

        /// <param name="x">X position.</param>
        /// <param name="y">Y position.</param>
        /// <param name="z">Z position.</param>
        /// <param name="w">W position.</param>
        public Vector4( Real x, Real y, Real z, Real w )
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

        /// <param name="unitDimension"></param>
        public Vector4( Real unitDimension )
            : this( unitDimension, unitDimension, unitDimension, unitDimension )
        {
        }


        /// <remarks>
        /// The parseableText parameter is a comma seperated list of values e.g. "< 1.0, 1.0, 1.0, 1.0 >" 
        /// Format : {[(<} Real, Real, Real, Real {>)]}
        /// </remarks>
        /// <param name="parsableText">a comma seperated list of values</param>
        /// <exception cref="ArgumentException" />
        /// <exception cref="FormatException" />
        private Vector4( string parsableText )
        {
            // Verfiy input
            if ( parsableText == null || parsableText.Length == 0 )
                throw new ArgumentNullException( "The parsableText parameter cannot be null or zero length." );

            // Retrieve input values from input string
            string[] vals = parsableText.TrimStart( '(', '[', '<' ).TrimEnd( ')', ']', '>' ).Split( ',' );

            if ( vals.Length != 4 )
            {
                throw new FormatException( string.Format( "Cannot parse the text '{0}' because it does not have 4 parts separated by commas in the form (x,y,z) with optional parenthesis.", parsableText ) );
            }

            // Attempt to assign member variables to values. 
            // Will fail if the values are not parseable into Reals.
            try
            {
                x = Real.Parse( vals[ 0 ].Trim() );
                y = Real.Parse( vals[ 1 ].Trim() );
                z = Real.Parse( vals[ 2 ].Trim() );
                w = Real.Parse( vals[ 3 ].Trim() );
            }
            catch ( Exception )
            {
                throw new FormatException( "The parts of the vectors must be decimal numbers." );
            }
        }

        /// <param name="coordinates">An array of 3 decimal values.</param>
        public Vector4( Real[] coordinates )
        {
            if ( coordinates.Length != 4 )
                throw new ArgumentException( "The coordinates array must be of length 4 to specify the x, y, z and w coordinates." );
            this.x = coordinates[ 0 ];
            this.y = coordinates[ 1 ];
            this.z = coordinates[ 2 ];
            this.w = coordinates[ 3 ];
        }


        #endregion

        #region Static Methods
        /// <summary>
        /// Parses a Vector4 from a string
        /// </summary>
        /// <remarks>
        /// The parseableText parameter is a comma seperated list of values e.g. "(#.##, #.##, #.##, #.##)" 
        /// </remarks>
        /// <param name="text">a comma seperated list of values</param>
        /// <returns>a new instance</returns>
        public static Vector4 Parse( string text )
        {
            return new Vector4( text );
        }

        #endregion Static Methods

        #region System.Object Implementation
        /// <overrides>
        /// <summary>
        ///		Overrides the Object.ToString() method to provide a text representation of 
        ///		a Vector4.
        /// </summary>
        /// <returns>A string representation of a Vector4.</returns>
        /// </overrides>
        public override string ToString()
        {
            return string.Format( "({0}, {1}, {2}, {3})", this.x, this.y, this.z, this.w );
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
            return x.GetHashCode() ^ y.GetHashCode() ^ z.GetHashCode() ^ w.GetHashCode();
        }

        /// <summary>
        ///		Compares this Vector to another object.  This should be done because the 
        ///		equality operators (==, !=) have been overriden by this class.
        /// </summary>
        /// <param name="obj">object to compare to</param>
        /// <returns>true or false</returns>
        public override bool Equals( object obj )
        {
            return ( obj is Vector4 ) && ( this == (Vector4)obj );
        }

        #endregion System.Object Implementation

        #region Operator Overloads

        /// <summary>
        ///		Used when a Vector4 is added to another Vector4.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static Vector4 operator +( Vector4 left, Vector4 right )
        {
            return new Vector4( left.x + right.x, left.y + right.y, left.z + right.z, left.w + right.w );
        }

        /// <summary>
        ///		Used to subtract a Vector4 from another Vector4.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static Vector4 operator -( Vector4 left, Vector4 right )
        {
            return new Vector4( left.x - right.x, left.y - right.y, left.z - right.z, left.w - right.w );
        }

        /// <summary>
        ///		Used when a Vector4 is multiplied by another vector.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static Vector4 operator *( Vector4 left, Vector4 right )
        {
            return new Vector4( left.x * right.x, left.y * right.y, left.z * right.z, left.w * right.w );
        }

        /// <summary>
        ///		Used when a Vector4 is multiplied by a scalar value.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="scalar"></param>
        /// <returns></returns>
        public static Vector4 operator *( Vector4 left, Real scalar )
        {
            return new Vector4( left.x * scalar, left.y * scalar, left.z * scalar, left.w * scalar );
        }

        /// <summary>
        ///		Used when a scalar value is multiplied by a Vector4.
        /// </summary>
        /// <param name="scalar"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static Vector4 operator *( Real scalar, Vector4 right )
        {
            return new Vector4( right.x * scalar, right.y * scalar, right.z * scalar, right.w * scalar );
        }

        ///// <summary>
        ///// Used when a Matrix4 is multiplied by a Vector4.
        ///// </summary>
        ///// <param name="matrix"></param>
        ///// <param name="vector"></param>
        ///// <returns></returns>
        //public static Vector4 operator *( Matrix4 matrix, Vector4 vector )
        //{
        //    Vector4 result = new Vector4();

        //    result.x = vector.x * matrix.m00 + vector.y * matrix.m01 + vector.z * matrix.m02 + vector.w * matrix.m03;
        //    result.y = vector.x * matrix.m10 + vector.y * matrix.m11 + vector.z * matrix.m12 + vector.w * matrix.m13;
        //    result.z = vector.x * matrix.m20 + vector.y * matrix.m21 + vector.z * matrix.m22 + vector.w * matrix.m23;
        //    result.w = vector.x * matrix.m30 + vector.y * matrix.m31 + vector.z * matrix.m32 + vector.w * matrix.m33;

        //    return result;
        //}

        /// <summary>
        /// Used when a Vector4 is multiplied by a Matrix4.
        /// </summary>
        /// <param name="vector"></param>
        /// <param name="matrix"></param>
        /// <returns></returns>
        public static Vector4 operator *( Vector4 vector, Matrix4 matrix )
        {
            Vector4 result = new Vector4();

            result.x = vector.x * matrix.m00 + vector.y * matrix.m10 + vector.z * matrix.m20 + vector.w * matrix.m30;
            result.y = vector.x * matrix.m01 + vector.y * matrix.m11 + vector.z * matrix.m21 + vector.w * matrix.m31;
            result.z = vector.x * matrix.m02 + vector.y * matrix.m12 + vector.z * matrix.m22 + vector.w * matrix.m32;
            result.w = vector.x * matrix.m03 + vector.y * matrix.m13 + vector.z * matrix.m23 + vector.w * matrix.m33;

            return result;
        }


        /// <summary>
        ///		Used when a Vector4 is divided by another vector.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static Vector4 operator /( Vector4 left, Vector4 right )
        {
            return new Vector4( left.x / right.x, left.y / right.y, left.z / right.z, left.w / right.w );
        }

        /// <summary>
        /// Used to divide a vector by a scalar value.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="scalar"></param>
        /// <returns></returns>
        public static Vector4 operator /( Vector4 left, Real scalar )
        {
            Vector4 vector = new Vector4();

            // get the inverse of the scalar up front to avoid doing multiple divides later
            Real inverse = 1.0f / scalar;

            vector.x = left.x * inverse;
            vector.y = left.y * inverse;
            vector.z = left.z * inverse;
            vector.w = left.w * inverse;

            return vector;
        }

        /// <summary>
        ///		Used to negate the elements of a vector.
        /// </summary>
        /// <param name="left"></param>
        /// <returns></returns>
        public static Vector4 operator -( Vector4 left )
        {
            return new Vector4( -left.x, -left.y, -left.z, -left.w );
        }

        /// <summary>
        ///		User to compare two Vector4 instances for equality.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns>true or false</returns>
        public static bool operator ==( Vector4 left, Vector4 right )
        {
            return ( left.x == right.x && left.y == right.y && left.z == right.z && left.w == right.w );
        }

        /// <summary>
        ///		User to compare two Vector4 instances for inequality.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns>true or false</returns>
        public static bool operator !=( Vector4 left, Vector4 right )
        {
            return ( left.x != right.x || left.y != right.y || left.z != right.z || left.w != right.w );
        }

        /// <summary>
        ///    Returns true if the vector's scalar components are all smaller
        ///    that the ones of the vector it is compared against.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator >( Vector4 left, Vector4 right )
        {
            return ( left.x > right.x && left.y > right.y && left.z > right.z && left.w > right.w );
        }

        /// <summary>
        ///    Returns true if the vector's scalar components are all greater
        ///    that the ones of the vector it is compared against.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator <( Vector4 left, Vector4 right )
        {
            return ( left.x < right.x && left.y < right.y && left.z < right.z && left.w < right.w );
        }

        /// <summary>
        ///		Used to access a Vector by index 0 = x, 1 = y, 2 = z.  
        /// </summary>
        /// <remarks>
        /// uses unsafe pointer arithmatic for speed
        ///	</remarks>
        ///	<exception cref="ArgumentOutOfRange" />
        public unsafe Real this[ int index ]
        {
            get
            {
                if ( index < 0 | index > 3 )
                    throw new ArgumentOutOfRangeException( "index" );
                fixed ( Real* v = &this.x )
                {
                    return v[ index ];
                }
            }
            set
            {
                if ( index < 0 | index > 3 )
                    throw new ArgumentOutOfRangeException( "index" );
                fixed ( Real* v = &this.x )
                {
                    v[ index ] = value;
                }
            }
        }

        #region CLSCompliant Operator Methods

        /// <summary>
        ///		Used when a Vector4 is added to another Vector4.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static Vector4 Add( Vector4 left, Vector4 right )
        {
            return left + right;
        }

        /// <summary>
        ///		Used to subtract a Vector4 from another Vector4.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static Vector4 Subtract( Vector4 left, Vector4 right )
        {
            return left - right;
        }

        /// <summary>
        ///		Used when a Vector4 is multiplied by another vector.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static Vector4 Multiply( Vector4 left, Vector4 right )
        {
            return left * right;
        }

        /// <summary>
        ///		Used when a Vector4 is multiplied by a scalar value.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="scalar"></param>
        /// <returns></returns>
        public static Vector4 Multiply( Vector4 left, Real scalar )
        {
            return left * scalar;
        }

        /// <summary>
        ///		Used when a scalar value is multiplied by a Vector4.
        /// </summary>
        /// <param name="scalar"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static Vector4 Multiply( Real scalar, Vector4 right )
        {
            return scalar * right;
        }

        /// <summary>
        ///		
        /// </summary>
        /// <param name="vector"></param>
        /// <param name="matrix"></param>
        /// <returns></returns>
        public static Vector4 Multiply( Vector4 vector, Matrix4 matrix )
        {
            return vector * matrix;
        }

        /// <summary>
        ///		Used when a Vector4 is divided by another vector.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static Vector4 Divide( Vector4 left, Vector4 right )
        {
            return left / right;
        }

        /// <summary>
        /// Used to divide a vector by a scalar value.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="scalar"></param>
        /// <returns></returns>
        public static Vector4 Divide( Vector4 left, Real scalar )
        {
            return left / scalar;
        }

        /// <summary>
        ///		Used to negate the elements of a vector.
        /// </summary>
        /// <param name="left"></param>
        /// <returns></returns>
        public static Vector4 Negate( Vector4 left )
        {
            return -left;
        }

        #endregion CLSCompliant Operator Methods

        #endregion Operator Overloads

        #region Conversion Operators

        #region String Conversion

        /// <summary>
        /// Implicit conversion from string to Vector4
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        static public implicit operator Vector4( string value )
        {
            return new Vector4( value );
        }

        /// <summary>
        /// Explicit conversion from Vector4 to string
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        static public explicit operator string( Vector4 v )
        {
            return v.ToString();
        }

        #endregion String Conversions

        #region Real[] Conversion

        /// <summary>
        /// Implicit conversion from Real[] to Vector4
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        static public implicit operator Vector4( Real[] value )
        {
            return new Vector4( value );
        }

        #endregion Real[] Conversions

        #endregion Conversion Operators

        #region Public methods

        /// <summary>
        /// Generic method to get the elments of the vector as an array.
        /// </summary>
        /// <typeparam name="K">Any value based type (Real, int, float, decimal... )</typeparam>
        /// <returns>An array of the specified type containing 3 elements</returns>
        public K[] ToArray<K>() where K : struct
        {
            return new K[] { (K)Convert.ChangeType( x, typeof( K ) ), (K)Convert.ChangeType( y, typeof( K ) ), (K)Convert.ChangeType( z, typeof( K ) ), (K)Convert.ChangeType( w, typeof( K ) ) };
        }

        /// <summary>
        /// Specific method to get the elments of the vector as an array of Reals.
        /// </summary>
        /// <returns>An array Reals containing 3 elements</returns>
        public Real[] ToArray()
        {
            return ToArray<Real>();
        }

        /// <summary>
        /// Offsets the Vector2 by the specified values.
        /// </summary>
        /// <param name="x">Amount to offset the x component.</param>
        /// <param name="y">Amount to offset the y component.</param>
        /// <param name="y">Amount to offset the z component.</param>
        /// <remarks>This is equivilent to v += new Vector2( x, y );</remarks>
        /// <returns>the resultant Vector4</returns>        
        public Vector4 Offset( Real x, Real y, Real z, Real w )
        {
            return new Vector4( this.x + x, this.y + y, this.z + z, this.w + w );
        }

        /// <summary>
        ///		Performs a Dot Product operation on 2 vectors, which produces the angle between them.
        /// </summary>
        /// <param name="vector">The vector to perform the Dot Product against.</param>
        /// <returns>The angle between the 2 vectors.</returns>       
        public Real DotProduct( Vector4 vector )
        {
            return x * vector.x + y * vector.y + z * vector.z + w * vector.w;
        }

        /// <summary>
        ///		Finds the midpoint between the supplied Vector and this vector.
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        public Vector4 MidPoint( Vector4 vector )
        {
            return new Vector4( ( this.x + vector.x ) / 2f, ( this.y + vector.y ) / 2f, ( this.z + vector.z ) / 2f, ( this.w + vector.w ) / 2f );
        }

        /// <summary>
        ///		Compares the supplied vector and updates it's x/y/z/w components of they are higher in value.
        /// </summary>
        /// <param name="compare"></param>
        public void ToCeiling( Vector4 compare )
        {
            if ( compare.x > x )
                x = compare.x;
            if ( compare.y > y )
                y = compare.y;
            if ( compare.z > z )
                z = compare.z;
            if ( compare.w > w )
                w = compare.w;
        }

        /// <summary>
        ///		Compares the supplied vector and updates it's x/y/z/w components of they are lower in value.
        /// </summary>
        /// <param name="compare"></param>
        /// <returns></returns>
        public void ToFloor( Vector4 compare )
        {
            if ( compare.x < x )
                x = compare.x;
            if ( compare.y < y )
                y = compare.y;
            if ( compare.z < z )
                z = compare.z;
            if ( compare.w < w )
                w = compare.w;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Vector4 ToNormalized()
        {
            Vector4 vec = this;
            vec.Normalize();
            return vec;
        }

        /// <summary>
        ///		Normalizes the vector.
        /// </summary>
        /// <remarks>
        ///		This method normalises the vector such that it's
        ///		length / magnitude is 1. The result is called a unit vector.
        ///		<p/>
        ///		This function will not crash for zero-sized vectors, but there
        ///		will be no changes made to their components.
        ///	</remarks>
        ///	<returns>The previous length of the vector.</returns>
        public Real Normalize()
        {
            Real length = Utility.Sqrt( this.x * this.x + this.y * this.y + this.z * this.z + this.w * this.w );

            // Will also work for zero-sized vectors, but will change nothing
            if ( length > Real.Epsilon )
            {
                Real inverseLength = 1.0f / length;

                this.x *= inverseLength;
                this.y *= inverseLength;
                this.z *= inverseLength;
                this.w *= inverseLength;
            }

            return length;
        }

        /// <summary>
        ///    Calculates a reflection vector to the plane with the given normal.
        /// </summary>
        /// <remarks>
        ///    Assumes this vector is pointing AWAY from the plane, invert if not.
        /// </remarks>
        /// <param name="normal">Normal vector on which this vector will be reflected.</param>
        /// <returns></returns>
        public Vector4 Reflect( Vector4 normal )
        {
            return this - 2 * this.DotProduct( normal ) * normal;
        }

        #endregion

        #region ISerializable Implementation

        /// <summary>
        /// Deserialization contructor
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        private Vector4( SerializationInfo info, StreamingContext context )
        {
            x = (Real)info.GetValue( "x", typeof( Real ) );
            y = (Real)info.GetValue( "y", typeof( Real ) );
            z = (Real)info.GetValue( "z", typeof( Real ) );
            w = (Real)info.GetValue( "w", typeof( Real ) );
        }

        /// <summary>
        /// Serialization Method
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        [SecurityPermission( SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter )]
        public void GetObjectData( SerializationInfo info, StreamingContext context )
        {
            info.AddValue( "x", x );
            info.AddValue( "y", y );
            info.AddValue( "z", z );
            info.AddValue( "w", w );
        }

        #endregion ISerializable Implementation

    }
}
