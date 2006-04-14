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
    /// Standard 2-Dimensional Vector.
    /// </summary>
    /// <remarks>            
    /// A direction in 2D space represented as distances along the 2
    /// orthoganal axes (x, y). Note that positions, directions and
    /// scaling factors can be represented by a vector, depending on how
    /// you interpret the values.
    ///</remarks>
    [StructLayout( LayoutKind.Sequential )]
    [Serializable]
    public struct Vector2 : ISerializable
    {
        #region Fields and Properties

        // These fields are public on purpose. They are accessed often and 
        // we want to avoid the cost of the method call to a property.
        // When C# implements an inline keyword, we will revisit these.

        /// <summary>X component.</summary>
        public Real x;
        /// <summary>Y component.</summary>
        public Real y;

        /// <summary>Gets a Vector2 with all units set to positive infinity.</summary>
        public static readonly Vector2 PositiveInfinity = new Vector2( Real.PositiveInfinity, Real.PositiveInfinity );
        /// <summary>Gets a Vector2 with all units set to negative infinity.</summary>
        public static readonly Vector2 NegativeInfinity = new Vector2( Real.NegativeInfinity, Real.NegativeInfinity );
        /// <summary>Gets a Vector2 with all units set to Invalid.</summary>
        public static readonly Vector2 Invalid = new Vector2( Real.NaN, Real.NaN );
        /// <summary>Gets a Vector2 with all components set to 0.</summary>
        public static readonly Vector2 Zero = new Vector2( 0.0f, 0.0f );
        /// <summary>Gets a Vector2 with the X set to 1, and the others set to 0.</summary>
        public static readonly Vector2 UnitX = new Vector2( 1.0f, 0.0f );
        /// <summary>Gets a Vector2 with the Y set to 1, and the others set to 0.</summary>
        public static readonly Vector2 UnitY = new Vector2( 0.0f, 1.0f );
        /// <summary>Gets a Vector2 with the X set to -1, and the others set to 0.</summary>
        public static readonly Vector2 NegativeUnitX = new Vector2( -1.0f, 0.0f );
        /// <summary>Gets a Vector2 with the Y set to -1, and the others set to 0.</summary>
        public static readonly Vector2 NegativeUnitY = new Vector2( 0.0f, -1.0f );
        /// <summary>Gets a Vector2 with all components set to 1.</summary>
        public static readonly Vector2 Unit = new Vector2( 1.0f, 1.0f );


        /// <summary>Return True if the vector is the Positive Infinity Vector </summary>
        public bool IsPostiveInfinity
        {
            get
            {
                return this == Vector2.PositiveInfinity;
            }
        }

        /// <summary>Return True if the vector is the Negative Infinity Vector </summary>
        public bool IsNegativeInfinity
        {
            get
            {
                return this == Vector2.NegativeInfinity;
            }
        }

        /// <summary>Return True if the vector is the Invalid Vector </summary>
        public bool IsInvalid
        {
            get
            {
                return this == Vector2.Invalid;
            }
        }

        /// <summary>Return True if the vector is the Zero Vector </summary>
        public bool IsZero
        {
            get
            {
                return this == Vector2.Zero;
            }
        }

        /// <summary>Return True if the vector is the Unit X Vector </summary>
        public bool IsUnitX
        {
            get
            {
                return this == Vector2.UnitX;
            }
        }

        /// <summary>Return True if the vector is the UnitY Vector </summary>
        public bool IsUnitY
        {
            get
            {
                return this == Vector2.UnitY;
            }
        }

        /// <summary>Return True if the vector is the NegativeUnitX Vector </summary>
        public bool IsNegativeUnitX
        {
            get
            {
                return this == Vector2.NegativeUnitX;
            }
        }

        /// <summary>Return True if the vector is the NegativeUnitY Vector </summary>
        public bool IsNegativeUnitY
        {
            get
            {
                return this == Vector2.NegativeUnitY;
            }
        }

        /// <summary>Return True if the vector is the Unit Vector </summary>
        public bool IsUnit
        {
            get
            {
                return this == Vector2.Unit;
            }
        }

        /// <summary>Return True if the vector is normalized </summary>
        public bool IsNormalized
        {
            get
            {
                return this == Vector2.Zero;
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
                return Utility.Sqrt( this.x * this.x + this.y * this.y );
            }
        }

        /// <summary>
        ///    Returns the length (magnitude) of the vector squared.
        /// </summary>
        public Real LengthSquared
        {
            get
            {
                return ( this.x * this.x + this.y * this.y );
            }
        }

        #endregion Fields and Properties

        #region Constructors
        //NOTE: ISerializable Constructor in ISerializable Implementation

        /// <overloads>
        /// <summary>
        ///     Creates a new Vector2
        /// </summary>
        /// </overloads>
        /// <param name="source">the source vector.</param>
        public Vector2( Vector2 source )
        {
            this.x = source.x;
            this.y = source.y;
        }

        /// <param name="x">X position.</param>
        /// <param name="y">Y position</param>
        public Vector2( Real x, Real y )
        {
            this.x = x;
            this.y = y;
        }

        /// <param name="unitDimension"></param>
        public Vector2( Real unitDimension )
            : this( unitDimension, unitDimension )
        {
        }

        /// <remarks>
        /// The parseableText parameter is a comma seperated list of values e.g. "&lt; 1.0, 1.0 &gt;" 
        /// Format : {[(&lt;} Real, Real {&gt;)]}
        /// </remarks>
        /// <param name="parsableText">a comma seperated list of values</param>
        /// <exception cref="ArgumentException" />
        /// <exception cref="FormatException" />
        public Vector2( string parsableText )
        {
            // Verfiy input
            if ( parsableText == null || parsableText.Length == 0 )
                throw new ArgumentNullException( "The parsableText parameter cannot be null or zero length." );

            // Retrieve input values from input string
            string[] vals = parsableText.TrimStart( '(', '[', '<' ).TrimEnd( ')', ']', '>' ).Split( ',' );

            // Make sure there are 2 values present
            if ( vals.Length != 2 )
                throw new FormatException( string.Format( "Cannot parse the text '{0}' because it does not have 2 parts separated by commas in the form (x,y) with optional parenthesis.", parsableText ) );

            // Attempt to assign member variables to values. 
            // Will fail if the values are not parseable into Reals.
            try
            {
                x = Real.Parse( vals[ 0 ].Trim() );
                y = Real.Parse( vals[ 1 ].Trim() );
            }
            catch ( Exception )
            {
                throw new FormatException( "The parts of the vectors must be decimal numbers." );
            }
        }

        /// <param name="coordinates">An array of 2 decimal values.</param>
        public Vector2( Real[] coordinates )
        {
            if ( coordinates.Length != 2 )
                throw new ArgumentException( "The coordinates array must be of length 2 to specify the x and y coordinates." );
            this.x = coordinates[ 0 ];
            this.y = coordinates[ 1 ];
        }

        #endregion Constructors

        #region Static Methods

        /// <summary>
        /// Parses a Vector2 from a string
        /// </summary>
        /// <remarks>
        /// The parseableText parameter is a comma seperated list of values e.g. "(###, ###)" 
        /// </remarks>
        /// <param name="text">a comma seperated list of values</param>
        /// <returns>a new instance</returns>
        public static Vector2 Parse( string text )
        {
            return new Vector2( text );
        }

        #endregion

        #region System.Object Implementation

        /// <overrides>
        /// <summary>
        ///		Overrides the Object.ToString() method to provide a text representation of 
        ///		a Vector2.
        /// </summary>
        /// <returns>A string representation of a Vector2.</returns>
        /// </overrides>
        public override string ToString()
        {
            return string.Format( "({0}, {1})", x, y );
        }

        /// <param name="decimalPlaces">number of decimal places to render</param>
        public string ToString( int decimalPlaces )
        {
            string format = "";

            format = format.PadLeft( decimalPlaces, '#' );
            format = "({0:0." + format + "}, {1:0." + format + "})";
            //NOTE: Explicit conversion used here to get proper behavior, for some reason it left as Real it will always 
            //      display all decimal places
            return string.Format( format, (float)this.x, (float)this.y );
        }

        /// <summary>
        ///		Compares this Vector to another object.  This should be done because the 
        ///		equality operators (==, !=) have been overriden by this class.
        /// </summary>
        /// <param name="obj">object to compare to</param>
        /// <returns>true or false</returns>
        public override bool Equals( object obj )
        {
            return ( obj is Vector2 && this == (Vector2)obj );
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
            return ( x.GetHashCode() ^ y.GetHashCode() );
        }

        #endregion System.Object Implementation

        #region Operator Overloads

        /// <summary>
        ///		Used when a Vector2 is added to another Vector2.
        /// </summary>
        /// <param name="left">LHS of the operator</param>
        /// <param name="right">RHS of the operator</param>
        /// <returns></returns>
        public static Vector2 operator +( Vector2 left, Vector2 right )
        {
            return new Vector2( left.x + right.x, left.y + right.y );
        }

        /// <summary>
        ///		Used to subtract a Vector2 from another Vector2.
        /// </summary>
        /// <param name="left">LHS of the operator</param>
        /// <param name="right">RHS of the operator</param>
        /// <returns></returns>
        public static Vector2 operator -( Vector2 left, Vector2 right )
        {
            return new Vector2( left.x - right.x, left.y - right.y );
        }

        /// <summary>
        ///		Used when a Vector2 is multiplied by a Vector2.
        /// </summary>
        /// <param name="left">LHS of the operator</param>
        /// <param name="right">RHS of the operator</param>
        /// <returns></returns>
        public static Vector2 operator *( Vector2 left, Vector2 right )
        {
            return new Vector2( left.x * right.x, left.y * right.y );
        }

        /// <summary>
        ///		Used when a Vector2 is multiplied by a scalar value.
        /// </summary>
        /// <param name="left">LHS of the operator</param>
        /// <param name="scalar">The scalar to multiply by</param>
        /// <returns></returns>
        public static Vector2 operator *( Vector2 left, Real scalar )
        {
            return new Vector2( left.x * scalar, left.y * scalar );
        }

        /// <summary>
        ///		Used when a scalar value is multiplied by a Vector2.
        /// </summary>
        /// <param name="scalar">The scalar to multiply by</param>
        /// <param name="right">RHS of the operator</param>
        /// <returns></returns>
        public static Vector2 operator *( Real scalar, Vector2 right )
        {
            return new Vector2( right.x * scalar, right.y * scalar );
        }

        /// <summary>
        /// Used to test equality between two Vector2s
        /// </summary>
        /// <param name="left">LHS of the operator</param>
        /// <param name="right">RHS of the operator</param>
        /// <returns></returns>
        public static bool operator ==( Vector2 left, Vector2 right )
        {
            return ( left.x == right.x && left.y == right.y );
        }

        /// <summary>
        /// Used to test inequality between two Vector2s
        /// </summary>
        /// <param name="left">LHS of the operator</param>
        /// <param name="right">RHS of the operator</param>
        /// <returns></returns>
        public static bool operator !=( Vector2 left, Vector2 right )
        {
            return ( left.x != right.x || left.y != right.y );
        }

        /// <summary>
        ///		Used to negate the elements of a vector.
        /// </summary>
        /// <param name="left">LHS of the operator</param>
        /// <returns></returns>
        public static Vector2 operator -( Vector2 left )
        {
            return new Vector2( -left.x, -left.y );
        }

        /// <summary>
        ///    Returns true if the vector's scalar components are all smaller
        ///    that the ones of the vector it is compared against.
        /// </summary>
        /// <param name="left">LHS of the operator</param>
        /// <param name="right">RHS of the operator</param>
        /// <returns></returns>
        public static bool operator >( Vector2 left, Vector2 right )
        {
            return ( left.x > right.x && left.y > right.y );
        }

        /// <summary>
        ///    Returns true if the vector's scalar components are all greater
        ///    that the ones of the vector it is compared against.
        /// </summary>
        /// <param name="left">LHS of the operator</param>
        /// <param name="right">RHS of the operator</param>
        /// <returns></returns>
        public static bool operator <( Vector2 left, Vector2 right )
        {
            return ( left.x < right.x && left.y < right.y );
        }

        /// <summary>
        ///		Used to access a Vector by index 0 = x, 1 = y. 
        /// </summary>
        /// <remarks>
        /// uses unsafe pointer arithmatic for speed
        ///	</remarks>
        ///	<exception cref="ArgumentOutOfRange" />
        public unsafe Real this[ int index ]
        {
            get
            {
                if ( index < 0 | index > 1 ) 
                    throw new ArgumentOutOfRangeException( "index" );
                fixed ( Real* v = &this.x )
                {
                    return v[ index ];
                }
            }
            set
            {
                if ( index < 0 | index > 1 )
                    throw new ArgumentOutOfRangeException( "index" );
                fixed ( Real* v = &this.x )
                {
                    v[ index ] = value;
                }
            }
        }

        #region CLSCompliant Operator Methods

        /// <summary>
        ///		Used when a Vector2 is added to another Vector2.
        /// </summary>
        /// <param name="left">LHS of the operator</param>
        /// <param name="right">RHS of the operator</param>
        /// <returns></returns>
        public static Vector2 Add( Vector2 left, Vector2 right )
        {
            return left + right;
        }

        /// <summary>
        ///		Used to subtract a Vector2 from another Vector2.
        /// </summary>
        /// <param name="left">LHS of the operator</param>
        /// <param name="right">RHS of the operator</param>
        /// <returns></returns>
        public static Vector2 Subtract( Vector2 left, Vector2 right )
        {
            return left - right;
        }

        /// <summary>
        ///		Used when a Vector2 is multiplied by a Vector2.
        /// </summary>
        /// <param name="left">LHS of the operator</param>
        /// <param name="right">RHS of the operator</param>
        /// <returns></returns>
        public static Vector2 Multiply( Vector2 left, Vector2 right )
        {
            return left * right;
        }

        /// <summary>
        ///		Used when a Vector2 is multiplied by a scalar value.
        /// </summary>
        /// <param name="left">LHS of the operator</param>
        /// <param name="scalar">The scalar to multiply by</param>
        /// <returns></returns>
        public static Vector2 Multiply( Vector2 left, Real scalar )
        {
            return left * scalar;
        }

        /// <summary>
        ///		Used when a scalar value is multiplied by a Vector2.
        /// </summary>
        /// <param name="scalar">The scalar to multiply by</param>
        /// <param name="right">RHS of the operator</param>
        /// <returns></returns>
        public static Vector2 Multiply( Real scalar, Vector2 right )
        {
            return scalar * right;
        }

        /// <summary>
        ///		Used to negate the elements of a vector.
        /// </summary>
        /// <param name="left">LHS of the operator</param>
        /// <returns></returns>
        public static Vector2 Negate( Vector2 left )
        {
            return -left;
        }

        #endregion CLSCompliant Methods

        #endregion Operator Operator Overloads

        #region Conversion Operators

        #region String Conversion

        /// <summary>
        /// Implicit conversion from string to Vector2
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        static public implicit operator Vector2( string value )
        {
            return new Vector2( value );
        }

        /// <summary>
        /// Explicit conversion from Vector2 to string
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        static public explicit operator string( Vector2 v )
        {
            return v.ToString();
        }

        #endregion String Conversions

        #region Real[] Conversion

        /// <summary>
        /// Implicit conversion from Real[] to Vector2
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        static public implicit operator Vector2( Real[] value )
        {
            return new Vector2( value );
        }

        #endregion Real[] Conversions

        #region Vector3 Conversion

        /// <summary>
        /// Explicit conversion from a Vector3 to a Vector4
        /// </summary>
        /// <param name="vec3"></param>
        /// <returns></returns>
        public static explicit operator Vector3( Vector2 v )
        {
            return new Vector3( v.x, v.y, 1.0f );
        }

        #endregion Vector4 Conversion

        #endregion Converstion Operators

        #region Public Methods

        /// <summary>
        /// Generic method to get the elments of the vector as an array.
        /// </summary>
        /// <typeparam name="K">Any value based type (Real, int, float, decimal... )</typeparam>
        /// <returns>An array of the specified type containing 2 elements</returns>
        public K[] ToArray<K>() where K : struct
        {
            return new K[] { (K)Convert.ChangeType( x, typeof( K ) ), (K)Convert.ChangeType( y, typeof( K ) ) };
        }

        /// <summary>
        /// Specific method to get the elments of the vector as an array of Reals.
        /// </summary>
        /// <returns>An array Reals containing 2 elements</returns>
        public Real[] ToArray()
        {
            return ToArray<Real>();
        }

        /// <summary>
        /// Offsets the Vector2 by the specified values.
        /// </summary>
        /// <param name="x">Amount to offset the x component.</param>
        /// <param name="y">Amount to offset the y component.</param>
        /// <remarks>This is equivilent to v += new Vector2( x, y );</remarks>
        /// <returns>the resultant Vector3</returns>
        public Vector2 Offset( Real x, Real y )
        {
            return new Vector2( this.x + x, this.y + y );
        }

        /// <summary>
        ///		Performs a Dot Product operation on 2 vectors, which produces the angle between them.
        /// </summary>
        /// <param name="vector">The vector to perform the Dot Product against.</param>
        /// <returns>The angle between the 2 vectors.</returns>       
        public Real DotProduct( Vector2 vector )
        {
            return x * vector.x + y * vector.y;
        }

        /// <summary>
        ///		Performs a Cross Product operation on 2 vectors, which returns a vector that is perpendicular
        ///		to the intersection of the 2 vectors.  Useful for finding face normals.
        /// </summary>
        /// <param name="vector">A vector to perform the Cross Product against.</param>
        /// <returns>A new Vector2 perpedicular to the 2 original vectors.</returns>
        public Vector2 CrossProduct( Vector2 vector )
        {
            return new Vector2(
                ( this.y * vector.x ) - ( this.x * vector.y ),
                ( this.x * vector.y ) - ( this.y * vector.x )
                );

        }

        /// <summary>
        ///		Finds a vector perpendicular to this one.
        /// </summary>
        /// <returns></returns>
        public Vector2 Perpendicular()
        {
            Vector2 result = this.CrossProduct( UnitX );

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
        public Vector2 MidPoint( Vector2 vector )
        {
            Real two = 2f;
            return new Vector2( ( this.x + vector.x ) / two, ( this.y + vector.y ) / two );
        }

        /// <summary>
        ///		Compares the supplied vector and updates it's x/y/z components of they are higher in value.
        /// </summary>
        /// <param name="compare"></param>
        public void ToCeiling( Vector2 compare )
        {
            if ( compare.x > x )
                x = compare.x;
            if ( compare.y > y )
                y = compare.y;
        }

        /// <summary>
        ///		Compares the supplied vector and updates it's x/y/z components of they are lower in value.
        /// </summary>
        /// <param name="compare"></param>
        /// <returns></returns>
        public void ToFloor( Vector2 compare )
        {
            if ( compare.x < x )
                x = compare.x;
            if ( compare.y < y )
                y = compare.y;
        }
        /// <summary>
        /// returns a normailized vector of the current vector.
        /// </summary>
        public Vector2 ToNormalized()
        {
            Vector2 vec = this;
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
            Real length = Utility.Sqrt( this.x * this.x + this.y * this.y );

            // Will also work for zero-sized vectors, but will change nothing
            if ( length > Real.Epsilon )
            {
                Real inverseLength = new Real( 0.1f ) * length;

                this.x *= inverseLength;
                this.y *= inverseLength;
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
        public Vector2 Reflect( Vector2 normal )
        {
            return this - 2 * this.DotProduct( normal ) * normal;
        }

        #endregion Public Methods

        #region ISerializable Implementation

        /// <summary>
        /// Deserialization contructor
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        private Vector2( SerializationInfo info, StreamingContext context )
        {
            x = (Real)info.GetValue( "x", typeof( Real ) );
            y = (Real)info.GetValue( "y", typeof( Real ) );
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
        }

        #endregion ISerializable Implementation
    }
}
