#region LGPL License
/*
Axiom Graphics Engine Library
Copyright © 2003-2011 Axiom Project Team

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
#endregion LGPL License

#region SVN Version Information
// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;

using Axiom.Math;

#endregion Namespace Declarations

namespace Axiom.Core
{
	internal class CaseInsensitiveStringComparer : IEqualityComparer<string>
	{
		#region IEqualityComparer<string> Members

		public bool Equals( string x, string y )
		{
			return string.Equals( x, y, StringComparison.CurrentCultureIgnoreCase );
		}

		public int GetHashCode( string obj )
		{
			return obj.ToLower().GetHashCode();
		}

		#endregion IEqualityComparer<string> Members
	}


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

		#region String.Split() replacements

#if !( XBOX || XBOX360 )
		public static string[] Split( string s, char[] separators )
		{
			return s.Split( separators, 0, StringSplitOptions.None );
		}

		public static string[] Split( string s, char[] separators, int count )
		{
			return s.Split( separators, count, StringSplitOptions.None );
		}
#else

		public static string[] Split( string s, char[] separators, int count )
		{
			return Split(s, separators, count, StringSplitOptions.None );
		}

		public static string[] Split( string s, char[] separators )
		{
			return s.Split( separators );
		}

        /// <summary>
        ///     Specifies whether applicable Overload:System.String.Split method overloads
        ///     include or omit empty substrings from the return value.
        /// </summary>
        [Flags]
        public enum StringSplitOptions
        {
            /// <summary>
            ///     The return value includes array elements that contain an empty string
            /// </summary>
            None = 0,

            /// <summary>
            ///     The return value does not include array elements that contain an empty string
            /// </summary>
            RemoveEmptyEntries = 1,
        }

		/// <summary>
		/// Splits a string into an Array
		/// </summary>
		/// <param name="s">The String to split</param>
		/// <param name="separators">Array of seperators to break the string at</param>
		/// <param name="count">number of elements to return in the array</param>
		/// <returns>An array containing the split strings</returns>
		/// <remarks> Adapted from code supplied by andris11
		/// <para>
		/// If the number of seperators is greater than the count parameter
		/// then the last element will contain the remainder of the string.
		/// </para>
		/// </remarks>
		public static string[] Split( string s, char[] separators, int count , StringSplitOptions options )
		{
			List<string> results;
			string[] _strings;
			bool removeEmptyEntries;
			bool separatorFound = false;

			//special cases
			Debug.Assert( s != null, "String instance not set." );

			if ( count == 0 )
			{
				_strings = new string[] { };
				return _strings;
			}

			removeEmptyEntries = ( options & StringSplitOptions.RemoveEmptyEntries ) == StringSplitOptions.RemoveEmptyEntries;
			if ( s == String.Empty )
			{
				_strings = removeEmptyEntries ? new string[] { } : new string[ 1 ] { s }; //keep same instance
				return _strings;
			}

			//init
			StringBuilder str = new StringBuilder( s.Length );
			results = new List<string>( s.Length > 10 ? 10 : s.Length );

			if ( separators == null || separators.Length == 0 )
				separators = new char[] { ' ' };

			//parse
			//TODO: how to handle \n chars? see MSDN examples of String.Split()

			for ( int i = 0; i < s.Length; ++i )
			{
				bool isSeparator = false;

				foreach ( char sep in separators ) //using foreach with arrays is optimised (.NET2.0)
				{
					if ( s[ i ] == sep )
					{
						isSeparator = true;
						break;
					}
				}

				if ( isSeparator )
				{
					separatorFound = true; //so at least one separator was found

					if ( !( removeEmptyEntries && str.Length == 0 ) )
					{
						results.Add( str.ToString() );
						str.Length = 0;
					}

				}
				else
				{
					str.Append( s[ i ] );
				}

				if ( count > 0 && results.Count == count - 1 )
				{
					str.Append( s.Substring( i+1 ) );
					break; //limit reached
				}

			}

			if ( !( count > 0 && results.Count == count ) )
			{
				if ( !( removeEmptyEntries && str.Length == 0 ) )
				{
					results.Add( str.ToString() );
				}
			}

			//result
			if ( !separatorFound )
			{
				//no separator found, return just the same string
				return new string[ 1 ] { s }; //keep same instance, see MSDN
			}
			else
			{
				return results.ToArray();
			}
		}
#endif
		#endregion String.Split() replacements
		/// <summary>
		///		Parses a boolean type value
		/// </summary>
		/// <param name="val"></param>
		/// <returns></returns>
		public static bool ParseBool( string val )
		{
			switch ( val.ToLower() )
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
		public static ColorEx ParseColor( string[] values )
		{
			ColorEx color;
			color.r = ParseFloat( values[ 0 ] );
			color.g = ParseFloat( values[ 1 ] );
			color.b = ParseFloat( values[ 2 ] );
			color.a = ( values.Length > 3 ) ? ParseFloat( values[ 3 ] ) : 1.0f;

			return color;
		}

		/// <summary>
		///		Parses an array of params and returns a color from it.
		/// </summary>
		/// <param name="val"></param>
		public static ColorEx ParseColor( string val )
		{
			ColorEx color;
			string[] vals = val.Split( ' ' );

			color.r = ParseFloat( vals[ 0 ] );
			color.g = ParseFloat( vals[ 1 ] );
			color.b = ParseFloat( vals[ 2 ] );
			color.a = ( vals.Length == 4 ) ? ParseFloat( vals[ 3 ] ) : 1.0f;

			return color;
		}

		/// <summary>
		///		Parses an array of params and returns a color from it.
		/// </summary>
		public static Vector3 ParseVector3( string[] values )
		{
			Vector3 vec = new Vector3();
			vec.x = ParseFloat( values[ 0 ] );
			vec.y = ParseFloat( values[ 1 ] );
			vec.z = ParseFloat( values[ 2 ] );

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
			vec.x = ParseFloat( values[ 0 ] );
			vec.y = ParseFloat( values[ 1 ] );
			vec.z = ParseFloat( values[ 2 ] );

			return vec;
		}

		/// <summary>
		///		Parses an array of params and returns a color from it.
		/// </summary>
		public static Vector4 ParseVector4( string[] values )
		{
			Vector4 vec = new Vector4();
			vec.x = ParseFloat( values[ 0 ] );
			vec.y = ParseFloat( values[ 1 ] );
			vec.z = ParseFloat( values[ 2 ] );
			vec.w = ParseFloat( values[ 3 ] );

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
			if ( val == float.NaN.ToString() )
				return float.NaN;
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

		public static Quaternion ParseQuaternion( string p )
		{
			return Quaternion.Identity;
		}

		public static bool ParseInt( string value, out int num )
		{
		    return int.TryParse( value, out num);
		}
	}
}