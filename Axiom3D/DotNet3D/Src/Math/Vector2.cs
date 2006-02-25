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
using System.Diagnostics;
using System.Resources;
using System.Runtime.InteropServices;

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
    [StructLayout(LayoutKind.Sequential)]
    public struct Vector2 
    {
        #region Fields

        public float x, y;

        #endregion Fields

        #region Constructors

        /// <summary>
        ///     Creates a new Vector2
        /// </summary>
        /// <param name="x">X position.</param>
        /// <param name="y">Y position</param>
        public Vector2( Vector2 source )
        {
            this.x = source.x;
            this.y = source.y;
        }

        /// <summary>
        ///     Creates a new Vector2
        /// </summary>
        /// <param name="x">X position.</param>
        /// <param name="y">Y position</param>
        public Vector2( float x, float y )
        {
            this.x = x;
            this.y = y;
        }

        /// <summary>
        /// Create a new Vector2
        /// </summary>
        /// <remarks>
        /// The parseableText parameter is a comma seperated list of values e.g. "< 1.0, 1.0 >" 
        /// Format : {[(<} float, float {>)]}
        /// </remarks>
        /// <param name="parsableText">a comma seperated list of values</param>
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
            // Will fail if the values are not parseable into floats.
            try
            {
                x = float.Parse( vals[0].Trim() );
                y = float.Parse( vals[1].Trim() );
            }
            catch ( Exception )
            {
                throw new FormatException( "The parts of the vectors must be decimal numbers." );
            }
        }

        #endregion Constructors

        #region Static Interface

        /// <summary>
        /// Parses a Vector2 from a string
        /// </summary>
        /// <remarks>
        /// The parseableText parameter is a comma seperated list of values e.g. "(###, ###)" 
        /// </remarks>
        /// <param name="parsableText">a comma seperated list of values</param>
        /// <returns>a new instance</returns>
        public static Vector2 Parse( string text )
        {
            return new Vector2( text );
        }

        #endregion

        #region System.Object Implementation

        /// <summary>
        ///		Overrides the Object.ToString() method to provide a text representation of 
        ///		a Vector2.
        /// </summary>
        /// <returns>A string representation of a Vector3.</returns>
        public override string ToString()
        {
            return string.Format( "({0}, {1})", x, y );
        }

        /// <summary>
        ///		Overrides the Object.ToString() method to provide a text representation of 
        ///		a Vector2.
        /// </summary>
        /// <returns>A string representation of a Vector2.</returns>
        public string ToString( int decimalPlaces )
        {
            string format = "";

            format = format.PadLeft( decimalPlaces, '#' );
            format = "({0:0." + format + "}, {1:0." + format + "})";
            return string.Format( format, this.x, this.y );
        }

        /// <summary>
        ///     Overrides the object.Equals method for proper equality testing
        /// </summary>
        /// <param name="obj">object to compare to</param>
        /// <returns>true or false</returns>
        public override bool Equals( object obj )
        {
            return ( obj is Vector2 && this == (Vector2)obj );
        }

        /// <summary>
        /// Provides an unique Hashcode for this Object 
        /// </summary>
        /// <returns>Uses standard XOR operation of members</returns>
        public override int GetHashCode()
        {
            return ( x.GetHashCode() ^ y.GetHashCode() );
        }

        #endregion System.Object Implementation

        #region Operator Overloads
        /// <summary>
        /// Used to test equality between two Vector2s
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator ==( Vector2 left, Vector2 right )
        {
            return ( left.x == right.x && left.y == right.y );
        }

        /// <summary>
        /// Used to test inequality between two Vector2s
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator !=( Vector2 left, Vector2 right )
        {
            return ( left.x != right.x || left.y != right.y );
        }

        /// <summary>
        ///		Used when a Vector2 is added to another Vector2.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static Vector2 operator +( Vector2 left, Vector2 right )
        {
            return new Vector2( left.x + right.x, left.y + right.y );
        }


        /// <summary>
        ///		Used to subtract a Vector2 from another Vector2.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static Vector2 operator -( Vector2 left, Vector2 right )
        {
            return new Vector2( left.x - right.x, left.y - right.y );
        }


        /// <summary>
        ///		Used when a Vector2 is multiplied by a Vector2.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="scalar"></param>
        /// <returns></returns>
        public static Vector2 operator *( Vector2 left, Vector2 right )
        {
            return new Vector2( left.x * right.x, left.y * right.y );
        }


        /// <summary>
        ///		Used when a Vector2 is multiplied by a scalar value.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="scalar"></param>
        /// <returns></returns>
        public static Vector2 operator *( Vector2 left, float scalar )
        {
            return new Vector2( left.x * scalar, left.y * scalar );
        }


        /// <summary>
        ///		Used when a scalar value is multiplied by a Vector2.
        /// </summary>
        /// <param name="scalar"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static Vector2 operator *( float scalar, Vector2 right )
        {
            return new Vector2( right.x * scalar, right.y * scalar );
        }


        /// <summary>
        ///		Used to negate the elements of a vector.
        /// </summary>
        /// <param name="left"></param>
        /// <returns></returns>
        public static Vector2 operator -( Vector2 left )
        {
            return new Vector2( -left.x, -left.y );
        }

        /// <summary>
        ///		Used to access a Vector by index 0 = x, 1 = y. 
        /// </summary>
        /// <remarks>
        ///	</remarks>
        public float this[ int index ]
        {
            get
            {
                switch ( index )
                {
                    case 0:
                        return x;
                    case 1:
                        return y;
                    default:
                        throw new ArgumentOutOfRangeException( "index" );
                }
            }
            set
            {
                switch ( index )
                {
                    case 0:
                        x = value;
                        break;
                    case 1:
                        y = value;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException( "index" );
                }
            }
        }

        #region CLSCompliant Methods

        /// <summary>
        ///		Used when a Vector2 is added to another Vector2.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static Vector2 Add( Vector2 left, Vector2 right )
        {
            return left + right;
        }

        /// <summary>
        ///		Used to subtract a Vector2 from another Vector2.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static Vector2 Subtract( Vector2 left, Vector2 right )
        {
            return left - right;
        }

        /// <summary>
        ///		Used when a Vector2 is multiplied by a Vector2.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="scalar"></param>
        /// <returns></returns>
        public static Vector2 Multiply( Vector2 left, Vector2 right )
        {
            return left * right;
        }

        /// <summary>
        ///		Used when a Vector2 is multiplied by a scalar value.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="scalar"></param>
        /// <returns></returns>
        public static Vector2 Multiply( Vector2 left, float scalar )
        {
            return left * scalar;
        }

        /// <summary>
        ///		Used when a scalar value is multiplied by a Vector2.
        /// </summary>
        /// <param name="scalar"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static Vector2 Multiply( float scalar, Vector2 right )
        {
            return scalar * right;
        }

        /// <summary>
        ///		Used to negate the elements of a vector.
        /// </summary>
        /// <param name="left"></param>
        /// <returns></returns>
        public static Vector2 Negate( Vector2 left )
        {
            return -left;
        }

        #endregion CLSCompliant Methods

        #endregion Operator Overloads

        /// <summary>
        /// Copies a Vector2 into a new Vector2 instance.
        /// </summary>
        /// <param name="source"></param>
        /// <returns>a new instance</returns>
        public Vector2 Copy()
        {
            return new Vector2( this );
        }

    }
}
