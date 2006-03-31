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
    ///    Standard 3-dimensional vector.
    /// </summary>
    /// <remarks>
    ///	    A direction in 3D space represented as distances along the 3
    ///	    orthoganal axes (x, y, z). Note that positions, directions and
    ///	    scaling factors can be represented by a vector, depending on how
    ///	    you interpret the values.
    /// </remarks>
    [StructLayout( LayoutKind.Sequential )]
    public struct Vector3 : ISerializable
    {
        #region Fields and Properties

        /// <summary>X component.</summary>
        public Real x;
        /// <summary>Y component.</summary>
        public Real y;
        /// <summary>Z component.</summary>
        public Real z;

        /// <summary>Gets a Vector3 with all units set to positive infinity.</summary>
        public static readonly Vector3 PositiveInfinity = new Vector3( Real.PositiveInfinity, Real.PositiveInfinity, Real.PositiveInfinity );
        /// <summary>Gets a Vector3 with all units set to negative infinity.</summary>
        public static readonly Vector3 NegativeInfinity = new Vector3( Real.NegativeInfinity, Real.NegativeInfinity, Real.NegativeInfinity );
        /// <summary>Gets a Vector3 with all units set to Invalid.</summary>
        public static readonly Vector3 Invalid = new Vector3( Real.NaN, Real.NaN, Real.NaN );
        /// <summary>Gets a Vector3 with all components set to 0.</summary>
        public static readonly Vector3 Zero = new Vector3( 0.0f, 0.0f, 0.0f );
        /// <summary>Gets a Vector3 with the X set to 1, and the others set to 0.</summary>
        public static readonly Vector3 UnitX = new Vector3( 1.0f, 0.0f, 0.0f );
        /// <summary>Gets a Vector3 with the Y set to 1, and the others set to 0.</summary>
        public static readonly Vector3 UnitY = new Vector3( 0.0f, 1.0f, 0.0f );
        /// <summary>Gets a Vector3 with the Z set to 1, and the others set to 0.</summary>
        public static readonly Vector3 UnitZ = new Vector3( 0.0f, 0.0f, 1.0f );
        /// <summary>Gets a Vector3 with the X set to -1, and the others set to 0.</summary>
        public static readonly Vector3 NegativeUnitX = new Vector3( -1.0f, 0.0f, 0.0f );
        /// <summary>Gets a Vector3 with the Y set to -1, and the others set to 0.</summary>
        public static readonly Vector3 NegativeUnitY = new Vector3( 0.0f, -1.0f, 0.0f );
        /// <summary>Gets a Vector3 with the Z set to -1, and the others set to 0.</summary>
        public static readonly Vector3 NegativeUnitZ = new Vector3( 0.0f, 0.0f, -1.0f );
        /// <summary>Gets a Vector3 with all components set to 1.</summary>
        public static readonly Vector3 Unit = new Vector3( 1.0f, 1.0f, 1.0f );

        /// <summary>Return True if the vector is the Positive Infinity Vector </summary>
        public bool IsPostiveInfinity
        {
            get
            {
                return this == Vector3.PositiveInfinity;
            }
        }

        /// <summary>Return True if the vector is the Negative Infinity Vector </summary>
        public bool IsNegativeInfinity
        {
            get
            {
                return this == Vector3.NegativeInfinity;
            }
        }

        /// <summary>Return True if the vector is the Invalid Vector </summary>
        public bool IsInvalid
        {
            get
            {
                return this == Vector3.Invalid;
            }
        }

        /// <summary>Return True if the vector is the Zero Vector </summary>
        public bool IsZero
        {
            get
            {
                return this == Vector3.Zero;
            }
        }

        /// <summary>Return True if the vector is the Unit X Vector </summary>
        public bool IsUnitX
        {
            get
            {
                return this == Vector3.UnitX;
            }
        }

        /// <summary>Return True if the vector is the UnitY Vector </summary>
        public bool IsUnitY
        {
            get
            {
                return this == Vector3.UnitY;
            }
        }

        /// <summary>Return True if the vector is the UnitZ Vector </summary>
        public bool IsUnitZ
        {
            get
            {
                return this == Vector3.UnitZ;
            }
        }

        /// <summary>Return True if the vector is the NegativeUnitX Vector </summary>
        public bool IsNegativeUnitX
        {
            get
            {
                return this == Vector3.NegativeUnitX;
            }
        }

        /// <summary>Return True if the vector is the NegativeUnitY Vector </summary>
        public bool IsNegativeUnitY
        {
            get
            {
                return this == Vector3.NegativeUnitY;
            }
        }

        /// <summary>Return True if the vector is the NegativeUnitY Vector </summary>
        public bool IsNegativeUnitZ
        {
            get
            {
                return this == Vector3.NegativeUnitZ;
            }
        }

        /// <summary>Return True if the vector is the Unit Vector </summary>
        public bool IsUnit
        {
            get
            {
                return this == Vector3.Unit;
            }
        }

        /// <summary>Return True if the vector is Normalized</summary>
        public bool IsNormalized
        {
            get
            {
                return this == Vector3.Zero;
            }
        }

        /// <summary>
        ///    Gets the length (magnitude) of this Vector3.  The Sqrt operation is expensive, so 
        ///    only use this if you need the exact length of the Vector.  If vector lengths are only going
        ///    to be compared, use LengthSquared instead.
        /// </summary>
        public Real Length
        {
            get
            {
                return Utility.Sqrt( this.x * this.x + this.y * this.y + this.z * this.z );
            }
        }

        /// <summary>
        ///    Returns the length (magnitude) of the vector squared.
        /// </summary>
        public Real LengthSquared
        {
            get
            {
                return ( this.x * this.x + this.y * this.y + this.z * this.z );
            }
        }

        #endregion Fields and Properties

        #region Constructors

        //NOTE: ISerializable Constructor in ISerializable Implementation

        /// <overloads>
        /// <summary>
        ///     Creates a new 3 dimensional Vector.
        /// </summary>
        /// </overloads>
        /// <param name="source">the source vector.</param>
        public Vector3( Vector3 source )
        {
            this.x = source.x;
            this.y = source.y;
            this.z = source.z;
        }

        /// <param name="x">X position.</param>
        /// <param name="y">Y position</param>
        /// <param name="z">Z position</param>
        public Vector3( Real x, Real y, Real z )
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        /// <param name="unitDimension"></param>
        public Vector3( Real unitDimension )
            : this( unitDimension, unitDimension, unitDimension )
        {
        }

        /// <remarks>
        /// The parseableText parameter is a comma seperated list of values e.g. "< 1.0, 1.0, 1.0 >" 
        /// Format : {[(<} Real, Real, Real {>)]}
        /// </remarks>
        /// <param name="parsableText">a comma seperated list of values</param>
        /// <exception cref="ArgumentException" />
        /// <exception cref="FormatException" />
        private Vector3( string parsableText )
        {
            // Verfiy input
            if ( parsableText == null || parsableText.Length == 0 )
                throw new ArgumentNullException( "The parsableText parameter cannot be null or zero length." );

            // Retrieve input values from input string
            string[] vals = parsableText.TrimStart( '(', '[', '<' ).TrimEnd( ')', ']', '>' ).Split( ',' );

            if ( vals.Length != 3 )
            {
                throw new FormatException( string.Format( "Cannot parse the text '{0}' because it does not have 3 parts separated by commas in the form (x,y,z) with optional parenthesis.", parsableText ) );
            }

            // Attempt to assign member variables to values. 
            // Will fail if the values are not parseable into Reals.
            try
            {
                x = Real.Parse( vals[ 0 ].Trim() );
                y = Real.Parse( vals[ 1 ].Trim() );
                z = Real.Parse( vals[ 2 ].Trim() );
            }
            catch ( Exception )
            {
                throw new FormatException( "The parts of the vectors must be decimal numbers." );
            }
        }

        /// <param name="coordinates">An array of 3 decimal values.</param>
        public Vector3( Real[] coordinates )
        {
            if ( coordinates.Length != 3 )
                throw new ArgumentException( "The coordinates array must be of length 3 to specify the x, y, and z coordinates." );
            this.x = coordinates[ 0 ];
            this.y = coordinates[ 1 ];
            this.z = coordinates[ 2 ];
        }


        #endregion

        #region Static Methods
        /// <summary>
        /// Parses a Vector3 from a string
        /// </summary>
        /// <remarks>
        /// The parseableText parameter is a comma seperated list of values e.g. "(###, ###)" 
        /// </remarks>
        /// <param name="text">a comma seperated list of values</param>
        /// <returns>a new instance</returns>
        public static Vector3 Parse( string text )
        {
            return new Vector3( text );
        }

        #endregion Static Methods

        #region System.Object Implementation
        /// <overrides>
        /// <summary>
        ///		Overrides the Object.ToString() method to provide a text representation of 
        ///		a Vector3.
        /// </summary>
        /// <returns>A string representation of a Vector3.</returns>
        /// </overrides>
        public override string ToString()
        {
            return string.Format( "({0}, {1}, {2})", this.x, this.y, this.z );
        }

        /// <param name="decimalPlaces">number of decimal places to render</param>
        public string ToString( int decimalPlaces )
        {
            string format = "";

            format = format.PadLeft( decimalPlaces, '#' );
            format = "({0:0." + format + "}, {1:0." + format + "}, {2:0." + format + "})";
            //NOTE: Explicit conversion used here to get proper behavior, for some reason it left as Real it will always 
            //      display all decimal places
            return string.Format( format, (float)this.x, (float)this.y, (float)this.z );
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
            return x.GetHashCode() ^ y.GetHashCode() ^ z.GetHashCode();
        }

        /// <summary>
        ///		Compares this Vector to another object.  This should be done because the 
        ///		equality operators (==, !=) have been overriden by this class.
        /// </summary>
        /// <param name="obj">object to compare to</param>
        /// <returns>true or false</returns>
        public override bool Equals( object obj )
        {
            return ( obj is Vector3 ) && ( this == (Vector3)obj );
        }

        #endregion System.Object Implementation

        #region Operator Overloads

        /// <summary>
        ///		Used when a Vector3 is added to another Vector3.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static Vector3 operator +( Vector3 left, Vector3 right )
        {
            return new Vector3( left.x + right.x, left.y + right.y, left.z + right.z );
        }

        /// <summary>
        ///		Used to subtract a Vector3 from another Vector3.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static Vector3 operator -( Vector3 left, Vector3 right )
        {
            return new Vector3( left.x - right.x, left.y - right.y, left.z - right.z );
        }

        /// <summary>
        ///		Used when a Vector3 is multiplied by another vector.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static Vector3 operator *( Vector3 left, Vector3 right )
        {
            return new Vector3( left.x * right.x, left.y * right.y, left.z * right.z );
        }

        /// <summary>
        ///		Used when a Vector3 is multiplied by a scalar value.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="scalar"></param>
        /// <returns></returns>
        public static Vector3 operator *( Vector3 left, Real scalar )
        {
            return new Vector3( left.x * scalar, left.y * scalar, left.z * scalar );
        }

        /// <summary>
        ///		Used when a scalar value is multiplied by a Vector3.
        /// </summary>
        /// <param name="scalar"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static Vector3 operator *( Real scalar, Vector3 right )
        {
            return new Vector3( right.x * scalar, right.y * scalar, right.z * scalar );
        }

        /// <summary>
        ///		Used when a Vector3 is divided by another vector.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static Vector3 operator /( Vector3 left, Vector3 right )
        {
            return new Vector3( left.x / right.x, left.y / right.y, left.z / right.z );
        }

        /// <summary>
        /// Used to divide a vector by a scalar value.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="scalar"></param>
        /// <returns></returns>
        public static Vector3 operator /( Vector3 left, Real scalar )
        {
            Vector3 vector = new Vector3();

            // get the inverse of the scalar up front to avoid doing multiple divides later
            Real inverse = 1.0f / scalar;

            vector.x = left.x * inverse;
            vector.y = left.y * inverse;
            vector.z = left.z * inverse;

            return vector;
        }

        /// <summary>
        ///		Used to negate the elements of a vector.
        /// </summary>
        /// <param name="left"></param>
        /// <returns></returns>
        public static Vector3 operator -( Vector3 left )
        {
            return new Vector3( -left.x, -left.y, -left.z );
        }

        /// <summary>
        ///		User to compare two Vector3 instances for equality.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns>true or false</returns>
        public static bool operator ==( Vector3 left, Vector3 right )
        {
            return ( left.x == right.x && left.y == right.y && left.z == right.z );
        }

        /// <summary>
        ///		User to compare two Vector3 instances for inequality.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns>true or false</returns>
        public static bool operator !=( Vector3 left, Vector3 right )
        {
            return ( left.x != right.x || left.y != right.y || left.z != right.z );
        }

        /// <summary>
        ///    Returns true if the vector's scalar components are all smaller
        ///    that the ones of the vector it is compared against.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator >( Vector3 left, Vector3 right )
        {
            return ( left.x > right.x && left.y > right.y && left.z > right.z );
        }

        /// <summary>
        ///    Returns true if the vector's scalar components are all greater
        ///    that the ones of the vector it is compared against.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator <( Vector3 left, Vector3 right )
        {
            return ( left.x < right.x && left.y < right.y && left.z < right.z );
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
                if ( index < 0 | index > 2 )
                    throw new ArgumentOutOfRangeException( "index" );
                fixed ( Real* v = &this.x )
                {
                    return v[ index ];
                }
            }
            set
            {
                if ( index < 0 | index > 2 )
                    throw new ArgumentOutOfRangeException( "index" );
                fixed ( Real* v = &this.x )
                {
                    v[ index ] = value;
                }
            }
        }

        #region CLSCompliant Operator Methods

        /// <summary>
        ///		Used when a Vector3 is added to another Vector3.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static Vector3 Add( Vector3 left, Vector3 right )
        {
            return left + right;
        }

        /// <summary>
        ///		Used to subtract a Vector3 from another Vector3.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static Vector3 Subtract( Vector3 left, Vector3 right )
        {
            return left - right;
        }

        /// <summary>
        ///		Used when a Vector3 is multiplied by another vector.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static Vector3 Multiply( Vector3 left, Vector3 right )
        {
            return left * right;
        }

        /// <summary>
        ///		Used when a Vector3 is multiplied by a scalar value.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="scalar"></param>
        /// <returns></returns>
        public static Vector3 Multiply( Vector3 left, Real scalar )
        {
            return left * scalar;
        }

        /// <summary>
        ///		Used when a scalar value is multiplied by a Vector3.
        /// </summary>
        /// <param name="scalar"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static Vector3 Multiply( Real scalar, Vector3 right )
        {
            return scalar * right;
        }

        /// <summary>
        ///		Used when a Vector3 is divided by another vector.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static Vector3 Divide( Vector3 left, Vector3 right )
        {
            return left / right;
        }

        /// <summary>
        /// Used to divide a vector by a scalar value.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="scalar"></param>
        /// <returns></returns>
        public static Vector3 Divide( Vector3 left, Real scalar )
        {
            return left / scalar;
        }

        /// <summary>
        ///		Used to negate the elements of a vector.
        /// </summary>
        /// <param name="left"></param>
        /// <returns></returns>
        public static Vector3 Negate( Vector3 left )
        {
            return -left;
        }

        #endregion CLSCompliant Operator Methods

        #endregion Operator Overloads

        #region Conversion Operators

        #region String Conversion

        /// <summary>
        /// Implicit conversion from string to Vector3
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        static public implicit operator Vector3( string value )
        {
            return new Vector3( value );
        }

        /// <summary>
        /// Explicit conversion from Vector3 to string
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        static public explicit operator string( Vector3 v )
        {
            return v.ToString();
        }

        #endregion String Conversions

        #region Real[] Conversion

        /// <summary>
        /// Implicit conversion from Real[] to Vector3
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        static public implicit operator Vector3( Real[] value )
        {
            return new Vector3( value );
        }

        #endregion Real[] Conversions

        #region Vector4 Conversion

        /// <summary>
        /// Explicit conversion from a Vector3 to a Vector4
        /// </summary>
        /// <param name="vec3"></param>
        /// <returns></returns>
        public static explicit operator Vector4( Vector3 v )
        {
            return new Vector4( v.x, v.y, v.z, 1.0f );
        }
        #endregion Vector4 Conversion

        #endregion Conversion Operators

        #region Public methods

        /// <summary>
        /// Generic method to get the elments of the vector as an array.
        /// </summary>
        /// <typeparam name="K">Any value based type (Real, int, float, decimal... )</typeparam>
        /// <returns>An array of the specified type containing 3 elements</returns>
        public K[] ToArray<K>() where K : struct
        {
            return new K[] { (K)Convert.ChangeType( x, typeof( K ) ), (K)Convert.ChangeType( y, typeof( K ) ), (K)Convert.ChangeType( z, typeof( K ) ) };
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
        /// <returns>the resultant Vector3</returns>        
        public Vector3 Offset( Real x, Real y, Real z )
        {
            return new Vector3( this.x + x, this.y + y, this.z + z );
        }

        /// <summary>
        ///		Performs a Dot Product operation on 2 vectors, which produces the angle between them.
        /// </summary>
        /// <param name="vector">The vector to perform the Dot Product against.</param>
        /// <returns>The angle between the 2 vectors.</returns>       
        public Real DotProduct( Vector3 vector )
        {
            return x * vector.x + y * vector.y + z * vector.z;
        }

        /// <summary>
        ///		Performs a Cross Product operation on 2 vectors, which returns a vector that is perpendicular
        ///		to the intersection of the 2 vectors.  Useful for finding face normals.
        /// </summary>
        /// <param name="vector">A vector to perform the Cross Product against.</param>
        /// <returns>A new Vector3 perpedicular to the 2 original vectors.</returns>
        public Vector3 CrossProduct( Vector3 vector )
        {
            return new Vector3(
                ( this.y * vector.z ) - ( this.z * vector.y ),
                ( this.z * vector.x ) - ( this.x * vector.z ),
                ( this.x * vector.y ) - ( this.y * vector.x )
                );

        }

        /// <summary>
        ///		Finds a vector perpendicular to this one.
        /// </summary>
        /// <returns></returns>
        public Vector3 Perpendicular()
        {
            Vector3 result = this.CrossProduct( UnitX );

            // check length
            if ( result.LengthSquared < Real.Epsilon )
            {
                // This vector is the Y axis multiplied by a scalar, so we have to use another axis
                result = this.CrossProduct( UnitY );
            }

            return result;
        }

        /// <summary>
        ///		Finds the midpoint between the supplied Vector and this vector.
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        public Vector3 MidPoint( Vector3 vector )
        {
            return new Vector3( ( this.x + vector.x ) / 2f, ( this.y + vector.y ) / 2f, ( this.z + vector.z ) / 2f );
        }

        /// <summary>
        ///		Compares the supplied vector and updates it's x/y/z components of they are higher in value.
        /// </summary>
        /// <param name="compare"></param>
        public void ToCeiling( Vector3 compare )
        {
            if ( compare.x > x )
                x = compare.x;
            if ( compare.y > y )
                y = compare.y;
            if ( compare.z > z )
                z = compare.z;
        }

        /// <summary>
        ///		Compares the supplied vector and updates it's x/y/z components of they are lower in value.
        /// </summary>
        /// <param name="compare"></param>
        /// <returns></returns>
        public void ToFloor( Vector3 compare )
        {
            if ( compare.x < x )
                x = compare.x;
            if ( compare.y < y )
                y = compare.y;
            if ( compare.z < z )
                z = compare.z;
        }

        /// <summary>
        ///		Gets the shortest arc quaternion to rotate this vector to the destination vector. 
        /// </summary>
        /// <remarks>
        ///		Don't call this if you think the dest vector can be close to the inverse
        ///		of this vector, since then ANY axis of rotation is ok.
        ///	</remarks>
        public Quaternion GetRotationTo( Vector3 destination )
        {
            // Based on Stan Melax's article in Game Programming Gems
            Quaternion q = new Quaternion();

            Vector3 v0 = new Vector3( this.x, this.y, this.z );
            Vector3 v1 = destination;

            // normalize both vectors 
            v0.Normalize();
            v1.Normalize();

            // get the cross product of the vectors
            Vector3 c = v0.CrossProduct( v1 );

            // If the cross product approaches zero, we get unstable because ANY axis will do
            // when v0 == -v1
            Real d = v0.DotProduct( v1 );

            // If dot == 1, vectors are the same
            if ( d >= 1.0f )
            {
                return Quaternion.Identity;
            }

            Real s = Utility.Sqrt( ( 1 + d ) * 2 );
            Real inverse = 1 / s;

            q.x = c.x * inverse;
            q.y = c.y * inverse;
            q.z = c.z * inverse;
            q.w = s * 0.5f;

            return q;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Vector3 ToNormalized()
        {
            Vector3 vec = this;
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
            Real length = Utility.Sqrt( this.x * this.x + this.y * this.y + this.z * this.z );

            // Will also work for zero-sized vectors, but will change nothing
            if ( length > Real.Epsilon )
            {
                Real inverseLength = 1.0f / length;

                this.x *= inverseLength;
                this.y *= inverseLength;
                this.z *= inverseLength;
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
        public Vector3 Reflect( Vector3 normal )
        {
            return this - 2 * this.DotProduct( normal ) * normal;
        }

        /// <summary>
        /// Generates a new random vector which deviates from this vector by a
        /// given angle in a random direction.
        /// </summary>
        /// <remarks>
        /// This method assumes that the random number generator has already 
        /// been seeded appropriately.
        /// </remarks>
        /// <param name="angle">The angle at which to deviate</param>
        /// <param name="up">Any vector perpendicular to this one (which could generated 
        ///        by cross-product of this vector and any other non-colinear 
        ///        vector). If you choose not to provide this the function will 
        ///        derive one on it's own, however if you provide one yourself the 
        ///        function will be faster (this allows you to reuse up vectors if 
        ///        you call this method more than once) 
        /// </param>
        /// <returns>A random vector which deviates from this vector by angle. This 
        ///        vector will not be normalised, normalise it if you wish 
        ///        afterwards.
        /// </returns>
        public Vector3 RandomDeviant( Radian angle, Vector3 up )
        {
            Vector3 newUp = Zero;

            if ( up == Zero )
                newUp = this.Perpendicular();
            else
                newUp = up;

            // rotate up vector by random amount around this
            Quaternion q = new Quaternion( Utility.UnitRandom() * Utility.TWO_PI, this );
            newUp = q * newUp;

            // finally, rotate this by given angle around randomized up vector
            q = new Quaternion( angle, newUp );

            return q * this;
        }

        ///<overloads>
        ///<summary>Returns wether this vector is within a positional tolerance of another vector</summary>
        ///<param name="right">The vector to compare with</param>
        ///</overloads>
        ///<remarks>Uses a defalut tolerance of 1E-03</remarks>
        public bool PositionEquals( Vector3 right )
        {
            return PositionEquals( right, 1e-03f );
        }

        /// <param name="tolerance">The amount that each element of the vector may vary by and still be considered equal.</param>
        public bool PositionEquals( Vector3 right, Real tolerance )
        {
            return x.Equals( right.x, tolerance ) &&
                y.Equals( right.y, tolerance ) &&
                z.Equals( right.z, tolerance );
        }

        /// <summary>
        /// Returns whether this vector is within a directional tolerance of another vector.
        /// </summary>
        /// <param name="right">The vector to compare with.</param>
        /// <param name="tolerance">The maximum angle by which the vectors may vary and still be considered equal.</param>
        public bool DirectionEquals( Vector3 right, Radian tolerance )
        {
            Real dot = DotProduct( right );
            Radian angle = Utility.ACos( dot );

            return Utility.Abs( angle ) <= tolerance;

        }

        #endregion

        #region ISerializable Implementation

        /// <summary>
        /// Deserialization contructor
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        private Vector3( SerializationInfo info, StreamingContext context )
        {
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
            info.AddValue( "x", x );
            info.AddValue( "y", y );
            info.AddValue( "z", z );
        }

        #endregion ISerializable Implementation
    }

    namespace Collections
    {
        using System.Collections.Generic;

        public class Vector3List : List<Vector3>
        {
        }
    }
}
