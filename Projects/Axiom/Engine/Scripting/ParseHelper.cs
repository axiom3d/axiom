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
using System.Globalization;
using System.IO;
using System.Text;

using Axiom.Core;
using Axiom.Math;

#endregion Namespace Declarations

namespace Axiom.Scripting
{
	/// <summary>
	/// 	Class contining helper methods for parsing text files.
	/// </summary>
	public class ParseHelper
	{
		#region Methods

		/// <summary>
		///    Helper method for taking a string array and returning a single concatenated
		///    string composed of the range of specified elements.
		/// </summary>
		/// <param name="items"></param>
		/// <param name="start"></param>
		/// <param name="end"></param>
		public static string Combine( string[] items, int start, int end )
		{
			StringBuilder sb = new StringBuilder();

			for ( int i = start; i < end; i++ )
			{
				sb.AppendFormat( System.Globalization.CultureInfo.CurrentCulture, "{0} ", items[ i ] );
			}

			return sb.ToString( 0, sb.Length - 1 );
		}

		/// <summary>
		///		Helper method to log a formatted error when encountering problems with parsing
		///		an attribute.
		/// </summary>
		public static void LogParserError( string attribute, string context, string reason )
		{
			string error = string.Format( "Bad {0} attribute in block '{1}'. Reason: {2}", attribute, context, reason );

			LogManager.Instance.Write( error );
		}

		/// <summary>
		///		Helper method to nip/tuck the string before parsing it.  This includes trimming spaces from the beginning
		///		and end of the string, as well as removing excess spaces in between values.
		/// </summary>
		/// <param name="reader"></param>
		/// <returns></returns>
		public static string ReadLine( TextReader reader )
		{
			string line = reader.ReadLine();

			if ( line != null )
			{
				line = line.Replace( "\t", " " );
				line = line.Trim();

				// ignore blank lines, lines without spaces, or comments
				if ( line.Length == 0 || line.IndexOf( ' ' ) == -1 || line.StartsWith( "//" ) )
				{
					return line;
				}

				StringBuilder sb = new StringBuilder();

				string[] values = line.Split( ' ' );

				// reduce big space gaps between values down to a single space
				for ( int i = 0; i < values.Length; i++ )
				{
					string val = values[ i ];

					if ( val.Length != 0 )
					{
						sb.Append( val + " " );
					}
				}

				line = sb.ToString();
				line = line.TrimEnd();
			} // if

			return line;
		}

		/// <summary>
		///		Helper method to remove the first item from a string array and return a new array 1 element smaller
		///		starting at the second element of the original array.  This helpe to seperate the params from the command
		///		in the various script files.
		/// </summary>
		/// <returns></returns>
		public static string[] GetParams( string[] all )
		{
			// create a seperate parm list that has the command removed
			string[] parms = new string[ all.Length - 1 ];
			Array.Copy( all, 1, parms, 0, parms.Length );

			return parms;
		}

		/// <summary>
		///    Advances in the stream until it hits the next {.
		/// </summary>
		public static void SkipToNextOpenBrace( TextReader reader )
		{
			string line = "";
			while ( line != null && line != "{" )
			{
				line = ReadLine( reader );
			}
		}

		/// <summary>
		///    Advances in the stream until it hits the next }.
		/// </summary>
		/// <param name="reader"></param>
		public static void SkipToNextCloseBrace( TextReader reader )
		{
			string line = "";
			while ( line != null && line != "}" )
			{
				line = ReadLine( reader );
			}
		}

		#endregion Methods
	}
}