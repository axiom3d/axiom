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
using System.ComponentModel;			// for TypeConverterAttribute
using System.Diagnostics;				// mostly for Debug.Assert(...)
using System.Runtime.InteropServices;	// for StructLayoutAttribute
using System.Xml.Serialization;			// for various Xml attributes

namespace Axiom.MathLib
{
	/// <summary>
	/// 4D homogeneous vector.
	/// </summary>
	[XmlType("Vector4"),StructLayout(LayoutKind.Sequential),
	Serializable,TypeConverter(typeof(ExpandableObjectConverter))]
	public struct Vector4
	{
		#region Member variables

		public float x, y, z ,w;

		#endregion

		#region Constructors

		public Vector4(float x, float y, float z, float w)
		{
			this.x = x;
			this.y = y;
			this.z = z;
			this.w = w;
		}

		#endregion

		#region Operator overloads

		/// <summary>
		///		
		/// </summary>
		/// <param name="vector"></param>
		/// <param name="matrix"></param>
		/// <returns></returns>
		public static Vector4 operator * (Vector4 vector, Matrix4 matrix)
		{
			Vector4 result = new Vector4();
			
			result.x = vector.x * matrix.m00 + vector.y * matrix.m10 + vector.z * matrix.m20 + vector.w * matrix.m30;
			result.y = vector.x * matrix.m01 + vector.y * matrix.m11 + vector.z * matrix.m21 + vector.w * matrix.m31;
			result.z = vector.x * matrix.m02 + vector.y * matrix.m12 + vector.z * matrix.m22 + vector.w * matrix.m32;
			result.w = vector.x * matrix.m03 + vector.y * matrix.m13 + vector.z * matrix.m23 + vector.w * matrix.m33;

			return result;
		}

		/// <summary>
		///		User to compare two Vector4 instances for equality.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns>true or false</returns>
		public static bool operator == (Vector4 left, Vector4 right)
		{
			return (left.x == right.x && 
						left.y == right.y && 
						left.z == right.z && 
						left.w == right.w);
		}

		/// <summary>
		///		User to compare two Vector4 instances for inequality.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns>true or false</returns>
		public static bool operator != (Vector4 left, Vector4 right)
		{
			return (left.x != right.x || 
						left.y != right.y || 
						left.z != right.z ||
						left.w != right.w);
		}

		/// <summary>
		///		Used to access a Vector by index 0 = this.x, 1 = this.y, 2 = this.z, 3 = this.w.  
		/// </summary>
		/// <remarks>
		///		Uses unsafe pointer arithmetic to reduce the code required.
		///	</remarks>
		public float this[int index]
		{
			get
			{
				Debug.Assert(index >= 0 && index < 4, "Indexer boundaries overrun in Vector4.");
				
				// using pointer arithmetic here for less code.  Otherwise, we'd have a big switch statement.
				unsafe
				{
					fixed(float* pX = &x)
						return *(pX + index);
				}
			}
			set
			{
				Debug.Assert(index >= 0 && index < 4, "Indexer boundaries overrun in Vector4.");

				// using pointer arithmetic here for less code.  Otherwise, we'd have a big switch statement.
				unsafe
				{
					fixed(float* pX = &x)
						*(pX + index) = value;
				}
			}
		}

		#endregion

		#region Object overloads

		/// <summary>
		///		Overrides the Object.ToString() method to provide a text representation of 
		///		a Vector4.
		/// </summary>
		/// <returns>A string representation of a Vector4.</returns>
		public override string ToString()
		{
			return String.Format("<{0},{1},{2},{3}>", this.x, this.y, this.z, this.w);
		}

		/// <summary>
		///		Provides a unique hash code based on the member variables of this
		///		class.  This should be done because the equality operators (==, !=)
		///		have been overriden by this class.
		///		<p/>
		///		The standard implementation is a simple XOR operation between all local
		///		member variables.
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
		{
			return (int)this.x ^ (int)this.y ^ (int)this.z ^ (int)this.w;
		}

		/// <summary>
		///		Compares this Vector to another object.  This should be done because the 
		///		equality operators (==, !=) have been overriden by this class.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj)
		{
			if(obj is Vector4)
				return (this == (Vector4)obj);
			else
				return false;
		}

		#endregion
	}
}
