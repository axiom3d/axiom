#region LGPL License
/*
Axiom Game Engine Library
Copyright (C) 2003  Axiom Project Team

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

using System;

namespace Axiom.MathLib {
    /// <summary>
    ///     2 dimensional vector.
    /// </summary>
    //[StructLayout(LayoutKind.Sequential)]
    public struct Vector2 {
        #region Fields

        public float x, y;

        #endregion Fields

        #region Constructors

        /// <summary>
        ///     Constructor.
        /// </summary>
        /// <param name="x">X position.</param>
        /// <param name="y">Y position</param>
        public Vector2(float x, float y) {
            this.x = x;
            this.y = y;
        }

        #endregion Constructors

		/// <summary>
		///		Used when a Vector2 is added to another Vector2.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static Vector2 Add (Vector2 left, Vector2 right) 
		{
			return left + right;
		}
        
		/// <summary>
		///		Used when a Vector2 is added to another Vector2.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static Vector2 operator + (Vector2 left, Vector2 right) 
		{
			return new Vector2(left.x + right.x, left.y + right.y);
		}

		/// <summary>
		///		Used to subtract a Vector2 from another Vector2.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static Vector2 Subtract (Vector2 left, Vector2 right) 
		{
			return left - right;
		}
        
		/// <summary>
		///		Used to subtract a Vector2 from another Vector2.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static Vector2 operator - (Vector2 left, Vector2 right) 
		{
			return new Vector2(left.x - right.x, left.y - right.y);
		}

		/// <summary>
		///		Used when a Vector2 is multiplied by a scalar value.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="scalar"></param>
		/// <returns></returns>
		public static Vector2 Multiply (Vector2 left, float scalar) 
		{
			return left * scalar;
		}
        
		/// <summary>
		///		Used when a Vector2 is multiplied by a scalar value.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="scalar"></param>
		/// <returns></returns>
		public static Vector2 operator * (Vector2 left, float scalar) 
		{
			return new Vector2(left.x * scalar, left.y * scalar);
		}

		/// <summary>
		///		Used when a scalar value is multiplied by a Vector2.
		/// </summary>
		/// <param name="scalar"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static Vector2 Multiply (float scalar, Vector2 right) 
		{
			return scalar * right;
		}
        
		/// <summary>
		///		Used when a scalar value is multiplied by a Vector2.
		/// </summary>
		/// <param name="scalar"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static Vector2 operator * (float scalar, Vector2 right) 
		{
			return new Vector2(right.x * scalar, right.y * scalar);
		}

		/// <summary>
		///		Used to negate the elements of a vector.
		/// </summary>
		/// <param name="left"></param>
		/// <returns></returns>
		public static Vector2 Negate (Vector2 left) 
		{
			return -left;
		}
        
		/// <summary>
		///		Used to negate the elements of a vector.
		/// </summary>
		/// <param name="left"></param>
		/// <returns></returns>
		public static Vector2 operator - (Vector2 left) 
		{
			return new Vector2(-left.x, -left.y);
		}
	}
}
