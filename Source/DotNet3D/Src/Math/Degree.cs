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

#if _REAL_AS_SINGLE || !( _REAL_AS_DOUBLE )
using Numeric = System.Single;
#else
using Numeric = System.Double;
#endif

using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security.Permissions;

#endregion Namespace Declarations

namespace DotNet3D.Math
{
    /// <summary>
    /// Wrapper class which indicates a given angle value is in Degrees.
    /// </summary>
    /// <remarks>
    /// Degree values are interchangeable with Radian values, and conversions
    /// will be done automatically between them.
    /// </remarks>
    [StructLayout( LayoutKind.Sequential )]
    [Serializable]
    public struct Degree : ISerializable, IComparable< Degree >, IComparable< Radian >, IComparable< Real >
	{
        private static readonly Real _degreesToRadians = (Utility.PI / 180.0f);

		private Real _value;

        public static readonly Degree Zero = new Degree( Real.Zero );

        /// <summary>
        /// Empty static constructor
        /// DO NOT DELETE.  It needs to be here because:
        /// 
        ///     # The presence of a static constructor suppresses beforeFieldInit.
        ///     # Static field variables are initialized before the static constructor is called.
        ///     # Having a static constructor is the only way to ensure that all resources are 
        ///       initialized before other static functions are called.
        /// 
        /// (from "Static Constructors Demystified" by Satya Komatineni
        ///  http://www.ondotnet.com/pub/a/dotnet/2003/07/07/staticxtor.html)
        /// </summary>
        static Degree() { }

        public Degree ( Real d ) { _value = d; }
		public Degree ( Degree d ) { _value = d._value; }
		public Degree ( Radian r ) { _value = r.InDegrees; }

        public Radian InRadians { get { return _value * _degreesToRadians; } }

        public static implicit operator Degree( Real value )   { return new Degree( value ); }
        public static implicit operator Degree( Radian value ) { return new Degree( value ); }
        public static explicit operator Degree( int value )    { return new Degree( value ); }

        public static implicit operator Real( Degree value )    { return new Real( value._value ); }
        public static explicit operator Numeric( Degree value ) { return (Numeric)value._value; }

		public static Degree operator + ( Degree left, Real right )    { return left._value + right; }
		public static Degree operator + ( Degree left, Degree right )  { return left._value + right._value; }
		public static Degree operator + ( Degree left, Radian right )  { return left + right.InDegrees; }

		public static Degree operator - ( Degree r )                   { return -r._value; }
		public static Degree operator - ( Degree left, Real right )    { return left._value - right; }
		public static Degree operator - ( Degree left, Degree right )  { return left._value - right._value; }
		public static Degree operator - ( Degree left, Radian right )  { return left - right.InDegrees; }

		public static Degree operator * ( Degree left, Real right )    { return left._value * right; }
		public static Degree operator * ( Real left,   Degree right )  { return left * right._value; }
        public static Degree operator * ( Degree left, Degree right )  { return left._value * right._value; }
        public static Degree operator * ( Degree left, Radian right )  { return left._value * right.InDegrees; }

		public static Degree operator / ( Degree left, Real right )    { return left._value / right; }

		public static bool operator <  ( Degree left, Degree right )   { return left._value <  right._value; }
		public static bool operator == ( Degree left, Degree right )   { return left._value == right._value; }
		public static bool operator != ( Degree left, Degree right )   { return left._value != right._value; }
		public static bool operator >  ( Degree left, Degree right )   { return left._value >  right._value; }

        public override bool Equals(object obj) { return ( obj is Degree && this == (Degree)obj ); }
        public override int GetHashCode() { return _value.GetHashCode(); }

        private Degree( SerializationInfo info, StreamingContext context ) { _value = (Real)info.GetValue( "value", typeof( Real ) ); }

        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
        public void GetObjectData( SerializationInfo info, StreamingContext context ) { info.AddValue( "value", _value, typeof( Real ) ); }

        public override string ToString() { return _value.ToString(); }

        #region IComparable<T> Members

        public int CompareTo( Degree other ) { return this._value.CompareTo( other._value ); }
        public int CompareTo( Radian other ) { return this._value.CompareTo( other.InDegrees._value ); }
        public int CompareTo( Real other )   { return this._value.CompareTo( other ); }

        #endregion

	}
}
