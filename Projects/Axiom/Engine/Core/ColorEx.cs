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
using System.Diagnostics;
using System.Runtime.InteropServices;

using Axiom.Utilities;
using Axiom.Math;

#endregion Namespace Declarations

namespace Axiom.Core
{
	/// <summary>
	///		This class is necessary so we can store the color components as floating
	///		point values in the range [0,1].  It serves as an intermediary to System.Drawing.Color, which
	///		stores them as byte values.  This doesn't allow for slow color component
	///		interpolation, because with the values always being cast back to a byte would lose
	///		any small interpolated values (i.e. 223 - .25 as a byte is 223).
	/// </summary>
	[StructLayout( LayoutKind.Sequential )]
	public struct ColorEx : IComparable
	{
		#region Member variables

		/// <summary>
		///		Alpha value [0,1].
		/// </summary>
		public float a;

		/// <summary>
		///		Red color component [0,1].
		/// </summary>
		public float r;

		/// <summary>
		///		Green color component [0,1].
		/// </summary>
		public float g;

		/// <summary>
		///		Blue color component [0,1].
		/// </summary>
		public float b;

		#endregion Member variables

		#region Constructors

		/// <summary>
		///	Constructor taking RGB values
		/// </summary>
		/// <param name="r">Red color component.</param>
		/// <param name="g">Green color component.</param>
		/// <param name="b">Blue color component.</param>
		public ColorEx( float r, float g, float b )
			: this( 1.0f, r, g, b ) {}

		/// <summary>
		///		Constructor taking all component values.
		/// </summary>
		/// <param name="a">Alpha value.</param>
		/// <param name="r">Red color component.</param>
		/// <param name="g">Green color component.</param>
		/// <param name="b">Blue color component.</param>
		public ColorEx( float a, float r, float g, float b )
		{
			Contract.Requires( a >= 0.0f && a <= 1.0f );
			Contract.Requires( r >= 0.0f && r <= 1.0f );
			Contract.Requires( g >= 0.0f && g <= 1.0f );
			Contract.Requires( b >= 0.0f && b <= 1.0f );

			this.a = a;
			this.r = r;
			this.g = g;
			this.b = b;
		}

		/// <summary>
		/// Copy constructor.
		/// </summary>
		/// <param name="other">The ColorEx instance to copy</param>
		public ColorEx( ColorEx other )
			: this()
		{
			this.a = other.a;
			this.r = other.r;
			this.g = other.g;
			this.b = other.b;
		}

		#endregion Constructors

		#region Methods

		public int ToRGBA()
		{
			int result = 0;

			result += ( (int)( r * 255.0f ) ) << 24;
			result += ( (int)( g * 255.0f ) ) << 16;
			result += ( (int)( b * 255.0f ) ) << 8;
			result += ( (int)( a * 255.0f ) );

			return result;
		}

		/// <summary>
		///		Converts this color value to packed ABGR format.
		/// </summary>
		/// <returns></returns>
		public int ToABGR()
		{
			int result = 0;

			result += ( (int)( a * 255.0f ) ) << 24;
			result += ( (int)( b * 255.0f ) ) << 16;
			result += ( (int)( g * 255.0f ) ) << 8;
			result += ( (int)( r * 255.0f ) );

			return result;
		}

		/// <summary>
		///		Converts this color value to packed ARBG format.
		/// </summary>
		/// <returns></returns>
		public int ToARGB()
		{
			int result = 0;

			result += ( (int)( a * 255.0f ) ) << 24;
			result += ( (int)( r * 255.0f ) ) << 16;
			result += ( (int)( g * 255.0f ) ) << 8;
			result += ( (int)( b * 255.0f ) );

			return result;
		}

		/// <summary>
		///		Populates the color components in a 4 elements array in RGBA order.
		/// </summary>
		/// <remarks>
		///		Primarily used to help in OpenGL.
		/// </remarks>
		/// <returns></returns>
		public void ToArrayRGBA( float[] vals )
		{
			vals[ 0 ] = r;
			vals[ 1 ] = g;
			vals[ 2 ] = b;
			vals[ 3 ] = a;
		}

		/// <summary>
		/// Clamps color value to the range [0, 1]
		/// </summary>
		public void Saturate()
		{
			r = Utility.Clamp( r, 1.0f, 0.0f );
			g = Utility.Clamp( g, 1.0f, 0.0f );
			b = Utility.Clamp( b, 1.0f, 0.0f );
			a = Utility.Clamp( a, 1.0f, 0.0f );
		}

		/// <summary>
		/// Clamps color value to the range [0, 1] in a copy
		/// </summary>
		public ColorEx SaturateCopy()
		{
			ColorEx saturated;
			saturated.r = Utility.Clamp( r, 1.0f, 0.0f );
			saturated.g = Utility.Clamp( g, 1.0f, 0.0f );
			saturated.b = Utility.Clamp( b, 1.0f, 0.0f );
			saturated.a = Utility.Clamp( a, 1.0f, 0.0f );

			return saturated;
		}

		#endregion Methods

		#region Operators

		public static bool operator ==( ColorEx left, ColorEx right )
		{
			return left.a == right.a &&
			       left.b == right.b &&
			       left.g == right.g &&
			       left.r == right.r;
		}

		public static bool operator !=( ColorEx left, ColorEx right )
		{
			return !( left == right );
		}

		public static ColorEx operator *( ColorEx left, ColorEx right )
		{
			ColorEx retVal = left;
			retVal.a *= right.a;
			retVal.r *= right.r;
			retVal.g *= right.g;
			retVal.b *= right.b;
			return retVal;
		}

		public static ColorEx operator *( ColorEx left, float scalar )
		{
			ColorEx retVal = left;
			retVal.a *= scalar;
			retVal.r *= scalar;
			retVal.g *= scalar;
			retVal.b *= scalar;
			return retVal;
		}

		public static ColorEx operator /( ColorEx left, ColorEx right )
		{
			ColorEx retVal = left;
			retVal.a /= right.a;
			retVal.r /= right.r;
			retVal.g /= right.g;
			retVal.b /= right.b;
			return retVal;
		}

		public static ColorEx operator /( ColorEx left, float scalar )
		{
			ColorEx retVal = left;
			retVal.a /= scalar;
			retVal.r /= scalar;
			retVal.g /= scalar;
			retVal.b /= scalar;
			return retVal;
		}

		public static ColorEx operator -( ColorEx left, ColorEx right )
		{
			ColorEx retVal = left;
			retVal.a -= right.a;
			retVal.r -= right.r;
			retVal.g -= right.g;
			retVal.b -= right.b;
			return retVal;
		}

		public static ColorEx operator +( ColorEx left, ColorEx right )
		{
			ColorEx retVal = left;
			retVal.a += right.a;
			retVal.r += right.r;
			retVal.g += right.g;
			retVal.b += right.b;
			return retVal;
		}

		#endregion Operators

		#region Static color properties

		/// <summary>
		///		The color Transparent.
		/// </summary>
		public static ColorEx Transparent
		{
			get
			{
				ColorEx retVal;
				retVal.a = 0f;
				retVal.r = 1f;
				retVal.g = 1f;
				retVal.b = 1f;
				return retVal;
			}
		}

		/// <summary>
		///		The color AliceBlue.
		/// </summary>
		public static ColorEx AliceBlue
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.9411765f;
				retVal.g = 0.972549f;
				retVal.b = 1.0f;
				return retVal;
			}
		}

		/// <summary>
		///		The color AntiqueWhite.
		/// </summary>
		public static ColorEx AntiqueWhite
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.9803922f;
				retVal.g = 0.9215686f;
				retVal.b = 0.8431373f;
				return retVal;
			}
		}

		/// <summary>
		///		The color Aqua.
		/// </summary>
		public static ColorEx Aqua
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.0f;
				retVal.g = 1.0f;
				retVal.b = 1.0f;
				return retVal;
			}
		}

		/// <summary>
		///		The color Aquamarine.
		/// </summary>
		public static ColorEx Aquamarine
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.4980392f;
				retVal.g = 1.0f;
				retVal.b = 0.8313726f;
				return retVal;
			}
		}

		/// <summary>
		///		The color Azure.
		/// </summary>
		public static ColorEx Azure
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.9411765f;
				retVal.g = 1.0f;
				retVal.b = 1.0f;
				return retVal;
			}
		}

		/// <summary>
		///		The color Beige.
		/// </summary>
		public static ColorEx Beige
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.9607843f;
				retVal.g = 0.9607843f;
				retVal.b = 0.8627451f;
				return retVal;
			}
		}

		/// <summary>
		///		The color Bisque.
		/// </summary>
		public static ColorEx Bisque
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 1.0f;
				retVal.g = 0.8941177f;
				retVal.b = 0.7686275f;
				return retVal;
			}
		}

		/// <summary>
		///		The color Black.
		/// </summary>
		public static ColorEx Black
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.0f;
				retVal.g = 0.0f;
				retVal.b = 0.0f;
				return retVal;
			}
		}

		/// <summary>
		///		The color BlanchedAlmond.
		/// </summary>
		public static ColorEx BlanchedAlmond
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 1.0f;
				retVal.g = 0.9215686f;
				retVal.b = 0.8039216f;
				return retVal;
			}
		}

		/// <summary>
		///		The color Blue.
		/// </summary>
		public static ColorEx Blue
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.0f;
				retVal.g = 0.0f;
				retVal.b = 1.0f;
				return retVal;
			}
		}

		/// <summary>
		///		The color BlueViolet.
		/// </summary>
		public static ColorEx BlueViolet
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.5411765f;
				retVal.g = 0.1686275f;
				retVal.b = 0.8862745f;
				return retVal;
			}
		}

		/// <summary>
		///		The color Brown.
		/// </summary>
		public static ColorEx Brown
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.6470588f;
				retVal.g = 0.1647059f;
				retVal.b = 0.1647059f;
				return retVal;
			}
		}

		/// <summary>
		///		The color BurlyWood.
		/// </summary>
		public static ColorEx BurlyWood
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.8705882f;
				retVal.g = 0.7215686f;
				retVal.b = 0.5294118f;
				return retVal;
			}
		}

		/// <summary>
		///		The color CadetBlue.
		/// </summary>
		public static ColorEx CadetBlue
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.372549f;
				retVal.g = 0.6196079f;
				retVal.b = 0.627451f;
				return retVal;
			}
		}

		/// <summary>
		///		The color Chartreuse.
		/// </summary>
		public static ColorEx Chartreuse
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.4980392f;
				retVal.g = 1.0f;
				retVal.b = 0.0f;
				return retVal;
			}
		}

		/// <summary>
		///		The color Chocolate.
		/// </summary>
		public static ColorEx Chocolate
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.8235294f;
				retVal.g = 0.4117647f;
				retVal.b = 0.1176471f;
				return retVal;
			}
		}

		/// <summary>
		///		The color Coral.
		/// </summary>
		public static ColorEx Coral
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 1.0f;
				retVal.g = 0.4980392f;
				retVal.b = 0.3137255f;
				return retVal;
			}
		}

		/// <summary>
		///		The color CornflowerBlue.
		/// </summary>
		public static ColorEx CornflowerBlue
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.3921569f;
				retVal.g = 0.5843138f;
				retVal.b = 0.9294118f;
				return retVal;
			}
		}

		/// <summary>
		///		The color Cornsilk.
		/// </summary>
		public static ColorEx Cornsilk
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 1.0f;
				retVal.g = 0.972549f;
				retVal.b = 0.8627451f;
				return retVal;
			}
		}

		/// <summary>
		///		The color Crimson.
		/// </summary>
		public static ColorEx Crimson
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.8627451f;
				retVal.g = 0.07843138f;
				retVal.b = 0.2352941f;
				return retVal;
			}
		}

		/// <summary>
		///		The color Cyan.
		/// </summary>
		public static ColorEx Cyan
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.0f;
				retVal.g = 1.0f;
				retVal.b = 1.0f;
				return retVal;
			}
		}

		/// <summary>
		///		The color DarkBlue.
		/// </summary>
		public static ColorEx DarkBlue
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.0f;
				retVal.g = 0.0f;
				retVal.b = 0.5450981f;
				return retVal;
			}
		}

		/// <summary>
		///		The color DarkCyan.
		/// </summary>
		public static ColorEx DarkCyan
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.0f;
				retVal.g = 0.5450981f;
				retVal.b = 0.5450981f;
				return retVal;
			}
		}

		/// <summary>
		///		The color DarkGoldenrod.
		/// </summary>
		public static ColorEx DarkGoldenrod
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.7215686f;
				retVal.g = 0.5254902f;
				retVal.b = 0.04313726f;
				return retVal;
			}
		}

		/// <summary>
		///		The color DarkGray.
		/// </summary>
		public static ColorEx DarkGray
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.6627451f;
				retVal.g = 0.6627451f;
				retVal.b = 0.6627451f;
				return retVal;
			}
		}

		/// <summary>
		///		The color DarkGreen.
		/// </summary>
		public static ColorEx DarkGreen
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.0f;
				retVal.g = 0.3921569f;
				retVal.b = 0.0f;
				return retVal;
			}
		}

		/// <summary>
		///		The color DarkKhaki.
		/// </summary>
		public static ColorEx DarkKhaki
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.7411765f;
				retVal.g = 0.7176471f;
				retVal.b = 0.4196078f;
				return retVal;
			}
		}

		/// <summary>
		///		The color DarkMagenta.
		/// </summary>
		public static ColorEx DarkMagenta
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.5450981f;
				retVal.g = 0.0f;
				retVal.b = 0.5450981f;
				return retVal;
			}
		}

		/// <summary>
		///		The color DarkOliveGreen.
		/// </summary>
		public static ColorEx DarkOliveGreen
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.3333333f;
				retVal.g = 0.4196078f;
				retVal.b = 0.1843137f;
				return retVal;
			}
		}

		/// <summary>
		///		The color DarkOrange.
		/// </summary>
		public static ColorEx DarkOrange
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 1.0f;
				retVal.g = 0.5490196f;
				retVal.b = 0.0f;
				return retVal;
			}
		}

		/// <summary>
		///		The color DarkOrchid.
		/// </summary>
		public static ColorEx DarkOrchid
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.6f;
				retVal.g = 0.1960784f;
				retVal.b = 0.8f;
				return retVal;
			}
		}

		/// <summary>
		///		The color DarkRed.
		/// </summary>
		public static ColorEx DarkRed
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.5450981f;
				retVal.g = 0.0f;
				retVal.b = 0.0f;
				return retVal;
			}
		}

		/// <summary>
		///		The color DarkSalmon.
		/// </summary>
		public static ColorEx DarkSalmon
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.9137255f;
				retVal.g = 0.5882353f;
				retVal.b = 0.4784314f;
				return retVal;
			}
		}

		/// <summary>
		///		The color DarkSeaGreen.
		/// </summary>
		public static ColorEx DarkSeaGreen
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.5607843f;
				retVal.g = 0.7372549f;
				retVal.b = 0.5450981f;
				return retVal;
			}
		}

		/// <summary>
		///		The color DarkSlateBlue.
		/// </summary>
		public static ColorEx DarkSlateBlue
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.282353f;
				retVal.g = 0.2392157f;
				retVal.b = 0.5450981f;
				return retVal;
			}
		}

		/// <summary>
		///		The color DarkSlateGray.
		/// </summary>
		public static ColorEx DarkSlateGray
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.1843137f;
				retVal.g = 0.3098039f;
				retVal.b = 0.3098039f;
				return retVal;
			}
		}

		/// <summary>
		///		The color DarkTurquoise.
		/// </summary>
		public static ColorEx DarkTurquoise
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.0f;
				retVal.g = 0.8078431f;
				retVal.b = 0.8196079f;
				return retVal;
			}
		}

		/// <summary>
		///		The color DarkViolet.
		/// </summary>
		public static ColorEx DarkViolet
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.5803922f;
				retVal.g = 0.0f;
				retVal.b = 0.827451f;
				return retVal;
			}
		}

		/// <summary>
		///		The color DeepPink.
		/// </summary>
		public static ColorEx DeepPink
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 1.0f;
				retVal.g = 0.07843138f;
				retVal.b = 0.5764706f;
				return retVal;
			}
		}

		/// <summary>
		///		The color DeepSkyBlue.
		/// </summary>
		public static ColorEx DeepSkyBlue
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.0f;
				retVal.g = 0.7490196f;
				retVal.b = 1.0f;
				return retVal;
			}
		}

		/// <summary>
		///		The color DimGray.
		/// </summary>
		public static ColorEx DimGray
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.4117647f;
				retVal.g = 0.4117647f;
				retVal.b = 0.4117647f;
				return retVal;
			}
		}

		/// <summary>
		///		The color DodgerBlue.
		/// </summary>
		public static ColorEx DodgerBlue
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.1176471f;
				retVal.g = 0.5647059f;
				retVal.b = 1.0f;
				return retVal;
			}
		}

		/// <summary>
		///		The color Firebrick.
		/// </summary>
		public static ColorEx Firebrick
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.6980392f;
				retVal.g = 0.1333333f;
				retVal.b = 0.1333333f;
				return retVal;
			}
		}

		/// <summary>
		///		The color FloralWhite.
		/// </summary>
		public static ColorEx FloralWhite
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 1.0f;
				retVal.g = 0.9803922f;
				retVal.b = 0.9411765f;
				return retVal;
			}
		}

		/// <summary>
		///		The color ForestGreen.
		/// </summary>
		public static ColorEx ForestGreen
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.1333333f;
				retVal.g = 0.5450981f;
				retVal.b = 0.1333333f;
				return retVal;
			}
		}

		/// <summary>
		///		The color Fuchsia.
		/// </summary>
		public static ColorEx Fuchsia
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 1.0f;
				retVal.g = 0.0f;
				retVal.b = 1.0f;
				return retVal;
			}
		}

		/// <summary>
		///		The color Gainsboro.
		/// </summary>
		public static ColorEx Gainsboro
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.8627451f;
				retVal.g = 0.8627451f;
				retVal.b = 0.8627451f;
				return retVal;
			}
		}

		/// <summary>
		///		The color GhostWhite.
		/// </summary>
		public static ColorEx GhostWhite
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.972549f;
				retVal.g = 0.972549f;
				retVal.b = 1.0f;
				return retVal;
			}
		}

		/// <summary>
		///		The color Gold.
		/// </summary>
		public static ColorEx Gold
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 1.0f;
				retVal.g = 0.8431373f;
				retVal.b = 0.0f;
				return retVal;
			}
		}

		/// <summary>
		///		The color Goldenrod.
		/// </summary>
		public static ColorEx Goldenrod
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.854902f;
				retVal.g = 0.6470588f;
				retVal.b = 0.1254902f;
				return retVal;
			}
		}

		/// <summary>
		///		The color Gray.
		/// </summary>
		public static ColorEx Gray
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.5019608f;
				retVal.g = 0.5019608f;
				retVal.b = 0.5019608f;
				return retVal;
			}
		}

		/// <summary>
		///		The color Green.
		/// </summary>
		public static ColorEx Green
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.0f;
				retVal.g = 0.5019608f;
				retVal.b = 0.0f;
				return retVal;
			}
		}

		/// <summary>
		///		The color GreenYellow.
		/// </summary>
		public static ColorEx GreenYellow
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.6784314f;
				retVal.g = 1.0f;
				retVal.b = 0.1843137f;
				return retVal;
			}
		}

		/// <summary>
		///		The color Honeydew.
		/// </summary>
		public static ColorEx Honeydew
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.9411765f;
				retVal.g = 1.0f;
				retVal.b = 0.9411765f;
				return retVal;
			}
		}

		/// <summary>
		///		The color HotPink.
		/// </summary>
		public static ColorEx HotPink
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 1.0f;
				retVal.g = 0.4117647f;
				retVal.b = 0.7058824f;
				return retVal;
			}
		}

		/// <summary>
		///		The color IndianRed.
		/// </summary>
		public static ColorEx IndianRed
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.8039216f;
				retVal.g = 0.3607843f;
				retVal.b = 0.3607843f;
				return retVal;
			}
		}

		/// <summary>
		///		The color Indigo.
		/// </summary>
		public static ColorEx Indigo
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.2941177f;
				retVal.g = 0.0f;
				retVal.b = 0.509804f;
				return retVal;
			}
		}

		/// <summary>
		///		The color Ivory.
		/// </summary>
		public static ColorEx Ivory
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 1.0f;
				retVal.g = 1.0f;
				retVal.b = 0.9411765f;
				return retVal;
			}
		}

		/// <summary>
		///		The color Khaki.
		/// </summary>
		public static ColorEx Khaki
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.9411765f;
				retVal.g = 0.9019608f;
				retVal.b = 0.5490196f;
				return retVal;
			}
		}

		/// <summary>
		///		The color Lavender.
		/// </summary>
		public static ColorEx Lavender
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.9019608f;
				retVal.g = 0.9019608f;
				retVal.b = 0.9803922f;
				return retVal;
			}
		}

		/// <summary>
		///		The color LavenderBlush.
		/// </summary>
		public static ColorEx LavenderBlush
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 1.0f;
				retVal.g = 0.9411765f;
				retVal.b = 0.9607843f;
				return retVal;
			}
		}

		/// <summary>
		///		The color LawnGreen.
		/// </summary>
		public static ColorEx LawnGreen
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.4862745f;
				retVal.g = 0.9882353f;
				retVal.b = 0.0f;
				return retVal;
			}
		}

		/// <summary>
		///		The color LemonChiffon.
		/// </summary>
		public static ColorEx LemonChiffon
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 1.0f;
				retVal.g = 0.9803922f;
				retVal.b = 0.8039216f;
				return retVal;
			}
		}

		/// <summary>
		///		The color LightBlue.
		/// </summary>
		public static ColorEx LightBlue
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.6784314f;
				retVal.g = 0.8470588f;
				retVal.b = 0.9019608f;
				return retVal;
			}
		}

		/// <summary>
		///		The color LightCoral.
		/// </summary>
		public static ColorEx LightCoral
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.9411765f;
				retVal.g = 0.5019608f;
				retVal.b = 0.5019608f;
				return retVal;
			}
		}

		/// <summary>
		///		The color LightCyan.
		/// </summary>
		public static ColorEx LightCyan
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.8784314f;
				retVal.g = 1.0f;
				retVal.b = 1.0f;
				return retVal;
			}
		}

		/// <summary>
		///		The color LightGoldenrodYellow.
		/// </summary>
		public static ColorEx LightGoldenrodYellow
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.9803922f;
				retVal.g = 0.9803922f;
				retVal.b = 0.8235294f;
				return retVal;
			}
		}

		/// <summary>
		///		The color LightGreen.
		/// </summary>
		public static ColorEx LightGreen
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.5647059f;
				retVal.g = 0.9333333f;
				retVal.b = 0.5647059f;
				return retVal;
			}
		}

		/// <summary>
		///		The color LightGray.
		/// </summary>
		public static ColorEx LightGray
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.827451f;
				retVal.g = 0.827451f;
				retVal.b = 0.827451f;
				return retVal;
			}
		}

		/// <summary>
		///		The color LightPink.
		/// </summary>
		public static ColorEx LightPink
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 1.0f;
				retVal.g = 0.7137255f;
				retVal.b = 0.7568628f;
				return retVal;
			}
		}

		/// <summary>
		///		The color LightSalmon.
		/// </summary>
		public static ColorEx LightSalmon
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 1.0f;
				retVal.g = 0.627451f;
				retVal.b = 0.4784314f;
				return retVal;
			}
		}

		/// <summary>
		///		The color LightSeaGreen.
		/// </summary>
		public static ColorEx LightSeaGreen
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.1254902f;
				retVal.g = 0.6980392f;
				retVal.b = 0.6666667f;
				return retVal;
			}
		}

		/// <summary>
		///		The color LightSkyBlue.
		/// </summary>
		public static ColorEx LightSkyBlue
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.5294118f;
				retVal.g = 0.8078431f;
				retVal.b = 0.9803922f;
				return retVal;
			}
		}

		/// <summary>
		///		The color LightSlateGray.
		/// </summary>
		public static ColorEx LightSlateGray
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.4666667f;
				retVal.g = 0.5333334f;
				retVal.b = 0.6f;
				return retVal;
			}
		}

		/// <summary>
		///		The color LightSteelBlue.
		/// </summary>
		public static ColorEx LightSteelBlue
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.6901961f;
				retVal.g = 0.7686275f;
				retVal.b = 0.8705882f;
				return retVal;
			}
		}

		/// <summary>
		///		The color LightYellow.
		/// </summary>
		public static ColorEx LightYellow
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 1.0f;
				retVal.g = 1.0f;
				retVal.b = 0.8784314f;
				return retVal;
			}
		}

		/// <summary>
		///		The color Lime.
		/// </summary>
		public static ColorEx Lime
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.0f;
				retVal.g = 1.0f;
				retVal.b = 0.0f;
				return retVal;
			}
		}

		/// <summary>
		///		The color LimeGreen.
		/// </summary>
		public static ColorEx LimeGreen
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.1960784f;
				retVal.g = 0.8039216f;
				retVal.b = 0.1960784f;
				return retVal;
			}
		}

		/// <summary>
		///		The color Linen.
		/// </summary>
		public static ColorEx Linen
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.9803922f;
				retVal.g = 0.9411765f;
				retVal.b = 0.9019608f;
				return retVal;
			}
		}

		/// <summary>
		///		The color Magenta.
		/// </summary>
		public static ColorEx Magenta
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 1.0f;
				retVal.g = 0.0f;
				retVal.b = 1.0f;
				return retVal;
			}
		}

		/// <summary>
		///		The color Maroon.
		/// </summary>
		public static ColorEx Maroon
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.5019608f;
				retVal.g = 0.0f;
				retVal.b = 0.0f;
				return retVal;
			}
		}

		/// <summary>
		///		The color MediumAquamarine.
		/// </summary>
		public static ColorEx MediumAquamarine
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.4f;
				retVal.g = 0.8039216f;
				retVal.b = 0.6666667f;
				return retVal;
			}
		}

		/// <summary>
		///		The color MediumBlue.
		/// </summary>
		public static ColorEx MediumBlue
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.0f;
				retVal.g = 0.0f;
				retVal.b = 0.8039216f;
				return retVal;
			}
		}

		/// <summary>
		///		The color MediumOrchid.
		/// </summary>
		public static ColorEx MediumOrchid
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.7294118f;
				retVal.g = 0.3333333f;
				retVal.b = 0.827451f;
				return retVal;
			}
		}

		/// <summary>
		///		The color MediumPurple.
		/// </summary>
		public static ColorEx MediumPurple
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.5764706f;
				retVal.g = 0.4392157f;
				retVal.b = 0.8588235f;
				return retVal;
			}
		}

		/// <summary>
		///		The color MediumSeaGreen.
		/// </summary>
		public static ColorEx MediumSeaGreen
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.2352941f;
				retVal.g = 0.7019608f;
				retVal.b = 0.4431373f;
				return retVal;
			}
		}

		/// <summary>
		///		The color MediumSlateBlue.
		/// </summary>
		public static ColorEx MediumSlateBlue
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.4823529f;
				retVal.g = 0.4078431f;
				retVal.b = 0.9333333f;
				return retVal;
			}
		}

		/// <summary>
		///		The color MediumSpringGreen.
		/// </summary>
		public static ColorEx MediumSpringGreen
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.0f;
				retVal.g = 0.9803922f;
				retVal.b = 0.6039216f;
				return retVal;
			}
		}

		/// <summary>
		///		The color MediumTurquoise.
		/// </summary>
		public static ColorEx MediumTurquoise
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.282353f;
				retVal.g = 0.8196079f;
				retVal.b = 0.8f;
				return retVal;
			}
		}

		/// <summary>
		///		The color MediumVioletRed.
		/// </summary>
		public static ColorEx MediumVioletRed
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.7803922f;
				retVal.g = 0.08235294f;
				retVal.b = 0.5215687f;
				return retVal;
			}
		}

		/// <summary>
		///		The color MidnightBlue.
		/// </summary>
		public static ColorEx MidnightBlue
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.09803922f;
				retVal.g = 0.09803922f;
				retVal.b = 0.4392157f;
				return retVal;
			}
		}

		/// <summary>
		///		The color MintCream.
		/// </summary>
		public static ColorEx MintCream
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.9607843f;
				retVal.g = 1.0f;
				retVal.b = 0.9803922f;
				return retVal;
			}
		}

		/// <summary>
		///		The color MistyRose.
		/// </summary>
		public static ColorEx MistyRose
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 1.0f;
				retVal.g = 0.8941177f;
				retVal.b = 0.8823529f;
				return retVal;
			}
		}

		/// <summary>
		///		The color Moccasin.
		/// </summary>
		public static ColorEx Moccasin
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 1.0f;
				retVal.g = 0.8941177f;
				retVal.b = 0.7098039f;
				return retVal;
			}
		}

		/// <summary>
		///		The color NavajoWhite.
		/// </summary>
		public static ColorEx NavajoWhite
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 1.0f;
				retVal.g = 0.8705882f;
				retVal.b = 0.6784314f;
				return retVal;
			}
		}

		/// <summary>
		///		The color Navy.
		/// </summary>
		public static ColorEx Navy
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.0f;
				retVal.g = 0.0f;
				retVal.b = 0.5019608f;
				return retVal;
			}
		}

		/// <summary>
		///		The color OldLace.
		/// </summary>
		public static ColorEx OldLace
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.9921569f;
				retVal.g = 0.9607843f;
				retVal.b = 0.9019608f;
				return retVal;
			}
		}

		/// <summary>
		///		The color Olive.
		/// </summary>
		public static ColorEx Olive
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.5019608f;
				retVal.g = 0.5019608f;
				retVal.b = 0.0f;
				return retVal;
			}
		}

		/// <summary>
		///		The color OliveDrab.
		/// </summary>
		public static ColorEx OliveDrab
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.4196078f;
				retVal.g = 0.5568628f;
				retVal.b = 0.1372549f;
				return retVal;
			}
		}

		/// <summary>
		///		The color Orange.
		/// </summary>
		public static ColorEx Orange
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 1.0f;
				retVal.g = 0.6470588f;
				retVal.b = 0.0f;
				return retVal;
			}
		}

		/// <summary>
		///		The color OrangeRed.
		/// </summary>
		public static ColorEx OrangeRed
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 1.0f;
				retVal.g = 0.2705882f;
				retVal.b = 0.0f;
				return retVal;
			}
		}

		/// <summary>
		///		The color Orchid.
		/// </summary>
		public static ColorEx Orchid
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.854902f;
				retVal.g = 0.4392157f;
				retVal.b = 0.8392157f;
				return retVal;
			}
		}

		/// <summary>
		///		The color PaleGoldenrod.
		/// </summary>
		public static ColorEx PaleGoldenrod
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.9333333f;
				retVal.g = 0.9098039f;
				retVal.b = 0.6666667f;
				return retVal;
			}
		}

		/// <summary>
		///		The color PaleGreen.
		/// </summary>
		public static ColorEx PaleGreen
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.5960785f;
				retVal.g = 0.9843137f;
				retVal.b = 0.5960785f;
				return retVal;
			}
		}

		/// <summary>
		///		The color PaleTurquoise.
		/// </summary>
		public static ColorEx PaleTurquoise
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.6862745f;
				retVal.g = 0.9333333f;
				retVal.b = 0.9333333f;
				return retVal;
			}
		}

		/// <summary>
		///		The color PaleVioletRed.
		/// </summary>
		public static ColorEx PaleVioletRed
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.8588235f;
				retVal.g = 0.4392157f;
				retVal.b = 0.5764706f;
				return retVal;
			}
		}

		/// <summary>
		///		The color PapayaWhip.
		/// </summary>
		public static ColorEx PapayaWhip
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 1.0f;
				retVal.g = 0.9372549f;
				retVal.b = 0.8352941f;
				return retVal;
			}
		}

		/// <summary>
		///		The color PeachPuff.
		/// </summary>
		public static ColorEx PeachPuff
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 1.0f;
				retVal.g = 0.854902f;
				retVal.b = 0.7254902f;
				return retVal;
			}
		}

		/// <summary>
		///		The color Peru.
		/// </summary>
		public static ColorEx Peru
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.8039216f;
				retVal.g = 0.5215687f;
				retVal.b = 0.2470588f;
				return retVal;
			}
		}

		/// <summary>
		///		The color Pink.
		/// </summary>
		public static ColorEx Pink
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 1.0f;
				retVal.g = 0.7529412f;
				retVal.b = 0.7960784f;
				return retVal;
			}
		}

		/// <summary>
		///		The color Plum.
		/// </summary>
		public static ColorEx Plum
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.8666667f;
				retVal.g = 0.627451f;
				retVal.b = 0.8666667f;
				return retVal;
			}
		}

		/// <summary>
		///		The color PowderBlue.
		/// </summary>
		public static ColorEx PowderBlue
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.6901961f;
				retVal.g = 0.8784314f;
				retVal.b = 0.9019608f;
				return retVal;
			}
		}

		/// <summary>
		///		The color Purple.
		/// </summary>
		public static ColorEx Purple
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.5019608f;
				retVal.g = 0.0f;
				retVal.b = 0.5019608f;
				return retVal;
			}
		}

		/// <summary>
		///		The color Red.
		/// </summary>
		public static ColorEx Red
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 1.0f;
				retVal.g = 0.0f;
				retVal.b = 0.0f;
				return retVal;
			}
		}

		/// <summary>
		///		The color RosyBrown.
		/// </summary>
		public static ColorEx RosyBrown
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.7372549f;
				retVal.g = 0.5607843f;
				retVal.b = 0.5607843f;
				return retVal;
			}
		}

		/// <summary>
		///		The color RoyalBlue.
		/// </summary>
		public static ColorEx RoyalBlue
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.254902f;
				retVal.g = 0.4117647f;
				retVal.b = 0.8823529f;
				return retVal;
			}
		}

		/// <summary>
		///		The color SaddleBrown.
		/// </summary>
		public static ColorEx SaddleBrown
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.5450981f;
				retVal.g = 0.2705882f;
				retVal.b = 0.07450981f;
				return retVal;
			}
		}

		/// <summary>
		///		The color Salmon.
		/// </summary>
		public static ColorEx Salmon
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.9803922f;
				retVal.g = 0.5019608f;
				retVal.b = 0.4470588f;
				return retVal;
			}
		}

		/// <summary>
		///		The color SandyBrown.
		/// </summary>
		public static ColorEx SandyBrown
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.9568627f;
				retVal.g = 0.6431373f;
				retVal.b = 0.3764706f;
				return retVal;
			}
		}

		/// <summary>
		///		The color SeaGreen.
		/// </summary>
		public static ColorEx SeaGreen
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.1803922f;
				retVal.g = 0.5450981f;
				retVal.b = 0.3411765f;
				return retVal;
			}
		}

		/// <summary>
		///		The color SeaShell.
		/// </summary>
		public static ColorEx SeaShell
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 1.0f;
				retVal.g = 0.9607843f;
				retVal.b = 0.9333333f;
				return retVal;
			}
		}

		/// <summary>
		///		The color Sienna.
		/// </summary>
		public static ColorEx Sienna
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.627451f;
				retVal.g = 0.3215686f;
				retVal.b = 0.1764706f;
				return retVal;
			}
		}

		/// <summary>
		///		The color Silver.
		/// </summary>
		public static ColorEx Silver
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.7529412f;
				retVal.g = 0.7529412f;
				retVal.b = 0.7529412f;
				return retVal;
			}
		}

		/// <summary>
		///		The color SkyBlue.
		/// </summary>
		public static ColorEx SkyBlue
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.5294118f;
				retVal.g = 0.8078431f;
				retVal.b = 0.9215686f;
				return retVal;
			}
		}

		/// <summary>
		///		The color SlateBlue.
		/// </summary>
		public static ColorEx SlateBlue
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.4156863f;
				retVal.g = 0.3529412f;
				retVal.b = 0.8039216f;
				return retVal;
			}
		}

		/// <summary>
		///		The color SlateGray.
		/// </summary>
		public static ColorEx SlateGray
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.4392157f;
				retVal.g = 0.5019608f;
				retVal.b = 0.5647059f;
				return retVal;
			}
		}

		/// <summary>
		///		The color Snow.
		/// </summary>
		public static ColorEx Snow
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 1.0f;
				retVal.g = 0.9803922f;
				retVal.b = 0.9803922f;
				return retVal;
			}
		}

		/// <summary>
		///		The color SpringGreen.
		/// </summary>
		public static ColorEx SpringGreen
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.0f;
				retVal.g = 1.0f;
				retVal.b = 0.4980392f;
				return retVal;
			}
		}

		/// <summary>
		///		The color SteelBlue.
		/// </summary>
		public static ColorEx SteelBlue
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.2745098f;
				retVal.g = 0.509804f;
				retVal.b = 0.7058824f;
				return retVal;
			}
		}

		/// <summary>
		///		The color Tan.
		/// </summary>
		public static ColorEx Tan
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.8235294f;
				retVal.g = 0.7058824f;
				retVal.b = 0.5490196f;
				return retVal;
			}
		}

		/// <summary>
		///		The color Teal.
		/// </summary>
		public static ColorEx Teal
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.0f;
				retVal.g = 0.5019608f;
				retVal.b = 0.5019608f;
				return retVal;
			}
		}

		/// <summary>
		///		The color Thistle.
		/// </summary>
		public static ColorEx Thistle
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.8470588f;
				retVal.g = 0.7490196f;
				retVal.b = 0.8470588f;
				return retVal;
			}
		}

		/// <summary>
		///		The color Tomato.
		/// </summary>
		public static ColorEx Tomato
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 1.0f;
				retVal.g = 0.3882353f;
				retVal.b = 0.2784314f;
				return retVal;
			}
		}

		/// <summary>
		///		The color Turquoise.
		/// </summary>
		public static ColorEx Turquoise
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.2509804f;
				retVal.g = 0.8784314f;
				retVal.b = 0.8156863f;
				return retVal;
			}
		}

		/// <summary>
		///		The color Violet.
		/// </summary>
		public static ColorEx Violet
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.9333333f;
				retVal.g = 0.509804f;
				retVal.b = 0.9333333f;
				return retVal;
			}
		}

		/// <summary>
		///		The color Wheat.
		/// </summary>
		public static ColorEx Wheat
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.9607843f;
				retVal.g = 0.8705882f;
				retVal.b = 0.7019608f;
				return retVal;
			}
		}

		/// <summary>
		///		The color White.
		/// </summary>
		public static ColorEx White
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 1.0f;
				retVal.g = 1.0f;
				retVal.b = 1.0f;
				return retVal;
			}
		}

		/// <summary>
		///		The color WhiteSmoke.
		/// </summary>
		public static ColorEx WhiteSmoke
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.9607843f;
				retVal.g = 0.9607843f;
				retVal.b = 0.9607843f;
				return retVal;
			}
		}

		/// <summary>
		///		The color Yellow.
		/// </summary>
		public static ColorEx Yellow
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 1.0f;
				retVal.g = 1.0f;
				retVal.b = 0.0f;
				return retVal;
			}
		}

		/// <summary>
		///		The color YellowGreen.
		/// </summary>
		public static ColorEx YellowGreen
		{
			get
			{
				ColorEx retVal;
				retVal.a = 1.0f;
				retVal.r = 0.6039216f;
				retVal.g = 0.8039216f;
				retVal.b = 0.1960784f;
				return retVal;
			}
		}

		//TODO : Move this to StringConverter
		public static ColorEx Parse_0_255_String( string parsableText )
		{
			ColorEx retVal;
			if( parsableText == null )
			{
				throw new ArgumentException( "The parsableText parameter cannot be null." );
			}
			string[] vals = parsableText.TrimStart( '(', '[', '<' ).TrimEnd( ')', ']', '>' ).Split( ',' );
			if( vals.Length < 3 )
			{
				throw new FormatException( string.Format( "Cannot parse the text '{0}' because it must of the form (r,g,b) or (r,g,b,a)",
				                                          parsableText ) );
			}
			//float r, g, b, a;
			try
			{
				retVal.r = int.Parse( vals[ 0 ].Trim() ) / 255f;
				retVal.g = int.Parse( vals[ 1 ].Trim() ) / 255f;
				retVal.b = int.Parse( vals[ 2 ].Trim() ) / 255f;
				if( vals.Length == 4 )
				{
					retVal.a = int.Parse( vals[ 3 ].Trim() ) / 255f;
				}
				else
				{
					retVal.a = 1.0f;
				}
			}
			catch( Exception e )
			{
				throw new FormatException( "The parts of the ColorEx in Parse_0_255 must be integers" );
			}
			return retVal;
		}

		//TODO : Move this to StringConverter
		public string To_0_255_String()
		{
			return string.Format( "({0},{1},{2},{3})",
			                      (int)( r * 255f ),
			                      (int)( g * 255f ),
			                      (int)( b * 255f ),
			                      (int)( a * 255f ) );
		}

		#endregion Static color properties

		#region Object overloads

		/// <summary>
		///    Override GetHashCode.
		/// </summary>
		/// <remarks>
		///    Done mainly to quash warnings, no real need for it.
		/// </remarks>
		/// <returns></returns>
		public override int GetHashCode()
		{
			return this.ToARGB();
		}

		public override bool Equals( object obj )
		{
			if( typeof( object ) is ColorEx )
			{
				return this == (ColorEx)obj;
			}
			else
			{
				return false;
			}
		}

		public override string ToString()
		{
			return this.To_0_255_String();
		}

		#endregion Object overloads

		#region IComparable Members

		/// <summary>
		///    Used to compare 2 ColorEx objects for equality.
		/// </summary>
		/// <param name="obj">An instance of a ColorEx object to compare to this instance.</param>
		/// <returns>0 if they are equal, 1 if they are not.</returns>
		public int CompareTo( object obj )
		{
			ColorEx other = (ColorEx)obj;

			if( this.a == other.a &&
			    this.r == other.r &&
			    this.g == other.g &&
			    this.b == other.b )
			{
				return 0;
			}

			return 1;
		}

		#endregion IComparable Members

		#region ICloneable Implementation

		/// <summary>
		/// Creates a new object that is a copy of the current instance.
		/// </summary>
		/// <returns>
		/// A new object that is a copy of this instance.
		/// </returns>
		/// <filterpriority>2</filterpriority>
		public ColorEx Clone()
		{
			ColorEx clone;
			clone.a = this.a;
			clone.r = this.r;
			clone.g = this.g;
			clone.b = this.b;
			return clone;
		}

		#endregion ICloneable Implementation
	}
}
