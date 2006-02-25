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

// The Real datatype is actually one of these three under the covers
using Numeric = System.Single;
//using Numeric = System.Double;
//using Numeric = System.Decimal;

#endregion Namespace Declarations

namespace DotNet3D.Math
{
    /// <summary>
    /// 
    /// </summary>
    [StructLayout( LayoutKind.Sequential )]
    [Serializable]
    public struct Real : ISerializable
    {
        #region Fields

        private Numeric _value;

        #endregion Fields

        #region Static Interface
        public readonly static Real PositiveInfinity = Numeric.PositiveInfinity;
        public readonly static Real NegativeInfinity = Numeric.NegativeInfinity;
        public readonly static Real NaN = Numeric.NaN;

        public static bool IsPositiveInfinity( Real number )
        {
            return Numeric.IsPositiveInfinity( (Numeric)number );
        }

        public static bool IsNegativeInfinity( Real number )
        {
            return Numeric.IsNegativeInfinity( (Numeric)number );
        }

        public static bool IsNaN( Real number )
        {
            return Numeric.IsNaN( (Numeric)number );
        }
        #endregion Static Interface

        #region Constructors

        public Real( int value )
        {
            this._value = value;
        }

        public Real( float value )
        {
            this._value = value;
        }

        public Real( double value )
        {
            this._value = (Numeric)value;
        }

        public Real( decimal value )
        {
            this._value = (Numeric)value;
        }

        public Real( string value )
        {
            this._value = Numeric.Parse( value );
        }

        #endregion Constructors

        #region Conversion Operators

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
        /// <param name="value"></param>
        /// <returns></returns>
        static public explicit operator int( Real real )
        {
            return (int)real._value;
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
        /// Explicit conversion from Real to float
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        static public explicit operator float( Real real )
        {
            return (float)real._value;
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
        /// <param name="value"></param>
        /// <returns></returns>
        static public explicit operator double( Real real )
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
        /// <param name="value"></param>
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
        /// <param name="value"></param>
        /// <returns></returns>
        static public explicit operator string( Real real )
        {
            return real.ToString();
        }

        #endregion String Conversions

        #endregion Conversion Operators

        #region Operator Overrides

        /// <summary>
        /// Used to test equality between two Reals
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator ==( Real left, Real right )
        {
            return ( left._value == right._value );
        }

        /// <summary>
        /// Used to test inequality between two Reals
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator !=( Real left, Real right )
        {
            return ( left._value != right._value );
        }

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
        /// <param name="scalar"></param>
        /// <returns></returns>
        public static Real operator *( Real left, Real right )
        {
            return new Real( left._value * right._value );
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

        #region CLSCompliant Methods

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
        /// <param name="scalar"></param>
        /// <returns></returns>
        public static Real Multiply( Real left, Real right )
        {
            return left * right;
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

        #endregion CLSCompliant Methods

        #endregion Operator Overrides

        #region System.Object Overrides

        public override string ToString()
        {
            return this._value.ToString();
        }

        public override bool Equals(object obj)
        {
            return ( obj is Real && this == (Real)obj );
        }

        public override int GetHashCode()
        {
            return this._value.GetHashCode();
        }

        #endregion System.Object Overrides

        public static Real Parse( string value )
        {
            return new Real( Numeric.Parse( value ) );
        }

        #region ISerializable Implementation

        public Real( SerializationInfo info, StreamingContext context )
        {
            _value = (Numeric)info.GetValue( "value", typeof( Numeric ) );
        }

        public void GetObjectData( SerializationInfo info, StreamingContext context )
        {
            info.AddValue( "value", _value );
        }

        #endregion ISerializable Implementation

    }
}
