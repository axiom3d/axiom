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

// The Real datatype is actually one of these under the covers
#if _REAL_AS_SINGLE || !( _REAL_AS_DOUBLE )
using Numeric = System.Single;
#else
using Numeric = System.Double;
#endif

using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security.Permissions;

#endregion Namespace Declarations

namespace DotNet3D.Math
{
    /// <summary>
    /// a floating point number abstraction allows the use of either a single-precision or double-precision floating point number
    /// </summary>
    /// <remarks>
    /// Use the _REAL_AS_DOUBLE condition compilation argument to use a double-precision value or
    /// _REAL_AS_SINGLE to use a single-precision value.
    /// </remarks>
    [StructLayout( LayoutKind.Sequential )]
    [Serializable]
    public struct Real : ISerializable, IComparable<Real>, IConvertible
    {
        #region Fields
        /// <summary>
        ///		Culture info to use for parsing numeric data.
        /// </summary>
        private static CultureInfo englishCulture = new CultureInfo( "en-US" );

        /// <summary>Internal storage for value</summary>
        private Numeric _value;

        public static Numeric Tolerance = 0.0001f;

        #endregion Fields

        #region Static Interface

        /// <summary>The value 0</summary>
        public readonly static Real Zero = new Real( 0 );
        /// <summary>The value of Positive Infinity</summary>
        public readonly static Real PositiveInfinity = Numeric.PositiveInfinity;
        /// <summary>The value of Negative Infinity</summary>
        public readonly static Real NegativeInfinity = Numeric.NegativeInfinity;
        /// <summary>Represents not a number</summary>
        public readonly static Real NaN = Numeric.NaN;
        /// <summary>The value of Epsilon</summary>
        public readonly static Real Epsilon = Numeric.Epsilon;
        /// <summary>The maximum possible value</summary>
        public readonly static Real MaxValue = Numeric.MaxValue;
        /// <summary>The minimum possible value</summary>
        public readonly static Real MinValue = Numeric.MinValue;

        /// <summary>
        /// Returns a value indicating whether the specified number evaluates to positive infinity
        /// </summary>
        /// <param name="number">a floating point number</param>
        /// <returns>a boolean</returns>
        public static bool IsPositiveInfinity( Real number )
        {
            return Numeric.IsPositiveInfinity( (Numeric)number );
        }

        /// <summary>
        /// Returns a value indicating whether the specified number evaluates to negative infinity
        /// </summary>
        /// <param name="number">a floating point number</param>
        /// <returns>a boolean</returns>
        public static bool IsNegativeInfinity( Real number )
        {
            return Numeric.IsNegativeInfinity( (Numeric)number );
        }

        /// <summary>
        /// Returns a value indicating whether the specified number evaluates to negative infinity
        /// </summary>
        /// <param name="number">a floating point number</param>
        /// <returns>a boolean</returns>
        public static bool IsInfinity( Real number )
        {
            return Numeric.IsInfinity( (Numeric)number );
        }

        /// <summary>
        /// Returns a value indicating whether the specified number evaluates to not a number
        /// </summary>
        /// <param name="number">a floating point number</param>
        /// <returns>a boolean</returns>
        public static bool IsNaN( Real number )
        {
            return Numeric.IsNaN( (Numeric)number );
        }
       
        /// <overloads>
        /// <summary>
        /// converts a string representation of a number in a specified style and culture-specific format
        /// to its floating point number equivilent
        /// </summary>
        /// <param name="value">a floating point number</param>
        /// <exception cref="System.ArgumentException"  />
        /// <exception cref="System.FormatException" />
        /// <exception cref="System.ArgumentNullException" />
        /// <returns>a Real</returns>
        /// </overloads>
        public static Real Parse( string value )
        {
            return new Real( Numeric.Parse( value, englishCulture ) );
        }

        /// <param name="value"></param>
        /// <param name="provider"></param>
        public static Real Parse( string value, IFormatProvider provider )
        {
            return new Real( Numeric.Parse( value, provider ) );
        }

        /// <param name="value"></param>
        /// <param name="style"></param>
        /// <param name="provider"></param>
        public static Real Parse( string value, System.Globalization.NumberStyles style, IFormatProvider provider )
        {
            return new Real( Numeric.Parse( value, style, provider ) );
        }

        /// <param name="value">a floating point number</param>
        /// <param name="style"></param>
        public static Real Parse( string value, System.Globalization.NumberStyles style )
        {
            return new Real( Numeric.Parse( value, style) );
        }

        #endregion Static Interface

        #region Constructors

        /// <overloads>
        /// <summary>
        /// initializes a Real with a specified value
        /// </summary>
        /// </overloads>
        /// <param name="value">an integer representation of the value to convert</param>
        public Real( int value )
        {
            this._value = value;
        }

        /// <param name="value">a long representation of the value to convert</param>
        public Real( long value )
        {
            this._value = value;
        }

        /// <param name="value">a float representation of the value to convert</param>
        public Real( float value )
        {
            this._value = value;
        }

        /// <param name="value">a double representation of the value to convert</param>
        public Real( double value )
        {
            this._value = (Numeric)value;
        }

        /// <param name="value">a decimal representation of the value to convert</param>
        public Real( decimal value )
        {
            this._value = (Numeric)value;
        }

        /// <param name="value">a string representation of the value to convert</param>
        public Real( string value )
        {
            this._value = Numeric.Parse( value );
        }

        #endregion Constructors

        #region Conversion Operators
        // Conversion Grid
        //
        //-------------------------------------------
        //|        | Int32 | Real | Single | Double |
        //-------------------------------------------
        //| Int32  |   X   |   I  |    X   |    X   |
        //-------------------------------------------
        //| Real   |   E   |      |    I   |    I   |
        //-------------------------------------------
        //| Single |   X   |   I  |    X   |    X   |
        //-------------------------------------------
        //| Double |   X   |   I  |    X   |    X   |
        //-------------------------------------------

        #region Int Conversions
        /// <summary>
        /// Implicit conversion from int to Real
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        static public implicit operator Real( int value )
        {
            return new Real( value );
        }

        /// <summary>
        /// Explicit conversion from Real to int
        /// </summary>
        /// <param name="real"></param>
        /// <returns></returns>
        static public explicit operator int( Real real )
        {
            return (int)real._value;
        }
        #endregion Int Conversions

        #region Int Conversions
        /// <summary>
        /// Implicit conversion from int to Real
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        static public implicit operator Real( long value )
        {
            return new Real( value );
        }

        /// <summary>
        /// Explicit conversion from Real to int
        /// </summary>
        /// <param name="real"></param>
        /// <returns></returns>
        static public explicit operator long( Real real )
        {
            return (long)real._value;
        }
        #endregion Int Conversions

        #region Float Conversions

        /// <summary>
        /// Implicit conversion from float to Real
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        static public implicit operator Real( float value )
        {
            return new Real( value );
        }

        /// <summary>
        /// Implicit conversion from Real to float
        /// </summary>
        /// <param name="real"></param>
        /// <returns></returns>
        static public implicit operator float( Real real )
        {
            return real._value;
        }

        #endregion Float Conversions

        #region Double Conversions
        /// <summary>
        /// Implicit conversion from double to Real
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        static public implicit operator Real( double value )
        {
            return new Real( value );
        }

        /// <summary>
        /// Explicit conversion from Real to double
        /// </summary>
        /// <param name="real"></param>
        /// <returns></returns>
        static public implicit operator double( Real real )
        {
            return real._value;
        }
        #endregion Double Conversions

        #region Decimal Conversions
        /// <summary>
        /// Implicit conversion from decimal to Real
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        static public implicit operator Real( decimal value )
        {
            return new Real( value );
        }

        /// <summary>
        /// Explicit conversion from Real to decimal
        /// </summary>
        /// <param name="real"></param>
        /// <returns></returns>
        static public explicit operator decimal( Real real )
        {
            return (decimal)real._value;
        }
        #endregion Decimal Conversions

        #region String Conversions

        /// <summary>
        /// Implicit conversion from string to Real
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        static public implicit operator Real( string value )
        {
            return new Real( value );
        }

        /// <summary>
        /// Explicit conversion from Real to string
        /// </summary>
        /// <param name="real"></param>
        /// <returns></returns>
        static public explicit operator string( Real real )
        {
            return real.ToString();
        }

        #endregion String Conversions

        #endregion Conversion Operators

        #region Operator Overrides

        #region Logical Operators

        #region Equality Operators

        /// <summary>
        /// Used to test equality between two Reals
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        /// <remarks>The == operator uses the static Tolerance value to determine equality</remarks>
        public static bool operator ==( Real left, Real right )
        {
            return ( Utility.Abs( right._value - left._value ) <= Tolerance );
        }

        /// <summary>
        /// Used to test inequality between two Reals
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        /// <remarks>The == operator uses the static Tolerance value to determine equality </remarks>
        public static bool operator !=( Real left, Real right )
        {
            return ( Utility.Abs( right._value - left._value ) >= Tolerance );
        }
        #endregion Equality Operators

        /// <summary>
        /// 
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator >( Real left, Real right )
        {
            return ( left._value > right._value );
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator <( Real left, Real right )
        {
            return ( left._value < right._value );
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator >=( Real left, Real right )
        {
            return ( left._value >= right._value );
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator <=( Real left, Real right )
        {
            return ( left._value <= right._value );
        }


        #endregion Logical Operators

        #region Arithmatic Operators

        /// <summary>
        ///		Used when a Real is added to another Real.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static Real operator +( Real left, Real right )
        {
            return new Real( left._value + right._value );
        }

        /// <summary>
        ///		Used to subtract a Real from another Real.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static Real operator -( Real left, Real right )
        {
            return new Real( left._value - right._value );
        }

        /// <summary>
        ///		Used when a Real is multiplied by a Real.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static Real operator *( Real left, Real right )
        {
            return new Real( left._value * right._value );
        }

        /// <summary>
        ///     Used when a Real is divided by a Real
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static Real operator /( Real left, Real right )
        {
            return new Real( left._value / right._value );
        }

        /// <summary>
        ///		Used to negate the elements of a Real.
        /// </summary>
        /// <param name="left"></param>
        /// <returns></returns>
        public static Real operator -( Real left )
        {
            return new Real( -left._value );
        }

        #endregion Arithmatic Operators

        #region CLSCompliant Methods

        #region Arithmatic Operations
        /// <summary>
        ///		Used when a Real is added to another Real.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static Real Add( Real left, Real right )
        {
            return left + right;
        }

        /// <summary>
        ///		Used to subtract a Real from another Real.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static Real Subtract( Real left, Real right )
        {
            return left - right;
        }

        /// <summary>
        ///		Used when a Real is multiplied by a Real.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static Real Multiply( Real left, Real right )
        {
            return left * right;
        }

        /// <summary>
        /// Used when a Real is divided by a Real.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static Real Divide( Real left, Real right )
        {
            return left / right;
        }

        /// <summary>
        ///		Used to negate the elements of a Real.
        /// </summary>
        /// <param name="left"></param>
        /// <returns></returns>
        public static Real Negate( Real left )
        {
            return -left;
        }

        #endregion Arithmatic Operations

        #endregion CLSCompliant Methods

        #endregion Operator Overrides

        #region Methods

        /// <summary>
        /// Returns the samllest integer less than or equal to the current value
        /// </summary>
        /// <returns></returns>
        public Real Floor()
        {
            return new Real( System.Math.Floor( _value ) );
        }

        /// <summary>
        /// Returns the samllest integer greater than or equal to the current value
        /// </summary>
        /// <returns></returns>
        public Real Ceiling()
        {
            return new Real( System.Math.Ceiling( _value ) );
        }

        #endregion Methods

        #region System.Object Overrides

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this._value.ToString( englishCulture );
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            return ( obj is Real && this == (Real)obj );
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <param name="tolerance"></param>
        /// <returns></returns>
        public bool Equals( Real obj )
        {
            return this.Equals( obj, Tolerance );
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <param name="tolerance"></param>
        /// <returns></returns>
        public bool Equals( Real obj, Real tolerance)
        {
            return ( Utility.Abs( obj - this ) <= tolerance );
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return this._value.GetHashCode();
        }

        #endregion System.Object Overrides

        #region ISerializable Implementation

        /// <summary>
        /// 
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        private Real( SerializationInfo info, StreamingContext context )
        {
            _value = (Numeric)info.GetValue( "value", typeof( Numeric ) );
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
        public void GetObjectData( SerializationInfo info, StreamingContext context )
        {
            info.AddValue( "value", _value );
        }

        #endregion ISerializable Implementation

        #region IComparable<Real> Members

        /// <summary>
        /// 
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo( Real other )
        {
            return this._value.CompareTo( other._value );
        }

        #endregion

        #region IConvertible Members

        public TypeCode GetTypeCode()
        {
            throw new Exception( "The method or operation is not implemented." );
        }

        public bool ToBoolean( IFormatProvider provider )
        {
            throw new Exception( "The method or operation is not implemented." );
        }

        public byte ToByte( IFormatProvider provider )
        {
            throw new Exception( "The method or operation is not implemented." );
        }

        public char ToChar( IFormatProvider provider )
        {
            throw new Exception( "The method or operation is not implemented." );
        }

        public DateTime ToDateTime( IFormatProvider provider )
        {
            throw new Exception( "The method or operation is not implemented." );
        }

        public decimal ToDecimal( IFormatProvider provider )
        {
            throw new Exception( "The method or operation is not implemented." );
        }

        public double ToDouble( IFormatProvider provider )
        {
            throw new Exception( "The method or operation is not implemented." );
        }

        public short ToInt16( IFormatProvider provider )
        {
            throw new Exception( "The method or operation is not implemented." );
        }

        public int ToInt32( IFormatProvider provider )
        {
            throw new Exception( "The method or operation is not implemented." );
        }

        public long ToInt64( IFormatProvider provider )
        {
            throw new Exception( "The method or operation is not implemented." );
        }

        public sbyte ToSByte( IFormatProvider provider )
        {
            throw new Exception( "The method or operation is not implemented." );
        }

        public float ToSingle( IFormatProvider provider )
        {
            return (float)this;
        }

        public string ToString( IFormatProvider provider )
        {
            throw new Exception( "The method or operation is not implemented." );
        }

        public object ToType( Type conversionType, IFormatProvider provider )
        {
            throw new Exception( "The method or operation is not implemented." );
        }

        public ushort ToUInt16( IFormatProvider provider )
        {
            throw new Exception( "The method or operation is not implemented." );
        }

        public uint ToUInt32( IFormatProvider provider )
        {
            throw new Exception( "The method or operation is not implemented." );
        }

        public ulong ToUInt64( IFormatProvider provider )
        {
            throw new Exception( "The method or operation is not implemented." );
        }

        #endregion
    }
}
