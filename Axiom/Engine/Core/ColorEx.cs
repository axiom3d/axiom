#region LGPL License
/*
Axiom Game Engine Library
Copyright (C) 2003  Axiom Project Team

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

using System;

namespace Axiom.Core
{
	/// <summary>
	///		This class is necessary so we can store the color components as floating 
	///		point values.  It serves as an intermediary to System.Drawing.Color, which
	///		stores them as byte values.  This doesn't allow for slow color component
	///		interpolation, because with the values always being cast back to a byte would lose
	///		any small interpolated values (i.e. 223 - .25 as a byte is 223).
	/// </summary>
	public class ColorEx
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

		#endregion

		/// <summary>
		///		Default constructor.
		/// </summary>
		public ColorEx()
		{
			// set the color components to a default of 1;
			a = 1.0f;
			r = 1.0f;
			g = 1.0f;
			b = 1.0f;
		}

		/// <summary>
		///		Constructor taking all component values.
		/// </summary>
		/// <param name="a">Alpha value.</param>
		/// <param name="r">Red color component.</param>
		/// <param name="g">Green color component.</param>
		/// <param name="b">Blue color component.</param>
		public ColorEx(float a, float r, float g, float b)
		{
			this.a = a;
			this.r = r;
			this.g = g;
			this.b = b;
		}

		#region Methods

		/// <summary>
		///		Converts this instance to a <see cref="System.Drawing.Color"/> structure.
		/// </summary>
		/// <returns></returns>
		// TODO: Watch for color loss.
		public System.Drawing.Color ToColor()
		{
			return System.Drawing.Color.FromArgb((int)(a * 255.0f), (int)(r * 255.0f), (int)(g * 255.0f), (int)(b * 255.0f));
		}

		/// <summary>
		///		Converts this color value to packed ABGR format.
		/// </summary>
		/// <returns></returns>
		public int ToABGR()
		{
			int result = 0;

			result += ((int)(a * 255.0f)) << 24;
			result += ((int)(b * 255.0f)) << 16;
			result += ((int)(g * 255.0f)) << 8;
			result += ((int)(r * 255.0f));

			return result;
		}

		/// <summary>
		///		Converts this color value to packed ARBG format.
		/// </summary>
		/// <returns></returns>
		public int ToARGB()
		{
			int result = 0;

			result += ((int)(a * 255.0f)) << 24;
			result += ((int)(r * 255.0f)) << 16;
			result += ((int)(g * 255.0f)) << 8;
			result += ((int)(b * 255.0f));

			return result;
		}

		/// <summary>
		///		Returns the color components in a 4 elements array in RGBA order.
		/// </summary>
		/// <remarks>
		///		Primarily used to help in OpenGL.
		/// </remarks>
		/// <returns></returns>
		public float[] ToArrayRGBA()
		{
			return new float[] {r, g, b, a};
		}

		/// <summary>
		///		Static method used to create a new <code>ColorEx</code> instance based
		///		on an existing <see cref="System.Drawing.Color"/> structure.
		/// </summary>
		/// <param name="color">.Net color structure to use as a basis.</param>
		/// <returns>A new <code>ColorEx instance.</code></returns>
		static public ColorEx FromColor(System.Drawing.Color color)
		{
			return new ColorEx((float)color.A / 255.0f, (float)color.R / 255.0f, (float)color.G / 255.0f, (float)color.B / 255.0f);
		}

		#endregion

	}
}
