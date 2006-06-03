#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2006  Axiom Project Team

The overall design, and a majority of the core engine and rendering code 
contained within this library is a derivative of the open source Object Oriented 
Graphics Engine OGRE, which can be found at http://ogre.sourceforge.net.  
Many thanks to the OGRE team for maintaining such a high quality project.

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
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Globalization;
using System.Text;


using DotNet3D.Math;

#endregion Namespace Declarations
			
namespace Axiom
{
    /// <summary>
    ///     Helper class for going back and forth between strings and various types.
    /// </summary>
    public sealed class StringConverter
    {
        #region Fields

        /// <summary>
        ///		Culture info to use for parsing numeric data.
        /// </summary>
        private static CultureInfo englishCulture = new CultureInfo( "en-US" );

        #endregion Fields

        #region Constructor

        /// <summary>
        ///     Private constructor so no instances can be created.
        /// </summary>
        private StringConverter()
        {
        }

        #endregion Constructor

        #region Static Methods

        /// <summary>
        ///		Parses a boolean type value 
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static bool ParseBool( string val )
        {
            switch ( val )
            {
                case "true":
                case "on":
                    return true;
                case "false":
                case "off":
                    return false;
            }

            // make the compiler happy
            return false;
        }

        /// <summary>
        ///		Parses an array of params and returns a color from it.
        /// </summary>
        /// <param name="val"></param>
        public static ColorEx ParseColor( string[] values )
        {
            ColorEx color = new ColorEx();
            color.r = ParseFloat( values[0] );
            color.g = ParseFloat( values[1] );
            color.b = ParseFloat( values[2] );
            color.a = ( values.Length > 3 ) ? ParseFloat( values[3] ) : 1.0f;

            return color;
        }

        /// <summary>
        ///		Parses an array of params and returns a color from it.
        /// </summary>
        /// <param name="val"></param>
        public static ColorEx ParseColor( string val )
        {
            ColorEx color = new ColorEx();
            string[] vals = val.Split( ' ' );

            color.r = ParseFloat( vals[0] );
            color.g = ParseFloat( vals[1] );
            color.b = ParseFloat( vals[2] );
            color.a = ( vals.Length == 4 ) ? ParseFloat( vals[3] ) : 1.0f;

            return color;
        }

        /// <summary>
        ///		Parses an array of params and returns a color from it.
        /// </summary>
        /// <param name="val"></param>
        public static Vector3 ParseVector3( string[] values )
        {
            Vector3 vec = new Vector3();
            vec.x = ParseFloat( values[0] );
            vec.y = ParseFloat( values[1] );
            vec.z = ParseFloat( values[2] );

            return vec;
        }

        /// <summary>
        ///		Parses an array of params and returns a color from it.
        /// </summary>
        /// <param name="val"></param>
        public static Vector3 ParseVector3( string val )
        {
            string[] values = val.Split( ' ' );

            Vector3 vec = new Vector3();
            vec.x = ParseFloat( values[0] );
            vec.y = ParseFloat( values[1] );
            vec.z = ParseFloat( values[2] );

            return vec;
        }

        /// <summary>
        ///		Parses an array of params and returns a color from it.
        /// </summary>
        /// <param name="val"></param>
        public static Vector4 ParseVector4( string[] values )
        {
            Vector4 vec = new Vector4();
            vec.x = ParseFloat( values[0] );
            vec.y = ParseFloat( values[1] );
            vec.z = ParseFloat( values[2] );
            vec.w = ParseFloat( values[3] );

            return vec;
        }

        /// <summary>
        ///		Parse a float value from a string.
        /// </summary>
        /// <remarks>
        ///		Since our file formats assume the 'en-US' style format for numbers, we need to
        ///		let the framework know that where numbers are being parsed.
        /// </remarks>
        /// <param name="val">String value holding the float.</param>
        /// <returns>A float representation of the string value.</returns>
        public static float ParseFloat( string val )
        {
            return float.Parse( val, englishCulture );
        }

        /// <summary>
        ///     
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static string ToString( ColorEx color )
        {
            return string.Format( englishCulture, "{0} {1} {2} {3}", color.r, color.g, color.b, color.a );
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="vec"></param>
        /// <returns></returns>
        public static string ToString( Vector4 vec )
        {
            return string.Format( englishCulture, "{0} {1} {2} {3}", vec.x, vec.y, vec.z, vec.w );
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="vec"></param>
        /// <returns></returns>
        public static string ToString( Vector3 vec )
        {
            return string.Format( englishCulture, "{0} {1} {2}", vec.x, vec.y, vec.z );
        }

        /// <summary>
        ///     Converts a 
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static string ToString( float val )
        {
            return val.ToString( englishCulture );
        }

        #endregion Static Methods
    }
}
