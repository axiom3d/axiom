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
using System.Text;

namespace Axiom.MathLib
{
	/// <summary>
	///		Class encapsulating a standard 4x4 homogenous matrix.
	/// </summary>
	/// <remarks>
	///		The engine uses column vectors when applying matrix multiplications,
	///		This means a vector is represented as a single column, 4-row
	///		matrix. This has the effect that the tranformations implemented
	///		by the matrices happens right-to-left e.g. if vector V is to be
	///		transformed by M1 then M2 then M3, the calculation would be
	///		M3 * M2 * M1 * V. The order that matrices are concatenated is
	///		vital since matrix multiplication is not cummatative, i.e. you
	///		can get a different result if you concatenate in the wrong order.
	/// 		<p/>
	///		The use of column vectors and right-to-left ordering is the
	///		standard in most mathematical texts, and id the same as used in
	///		OpenGL. It is, however, the opposite of Direct3D, which has
	///		inexplicably chosen to differ from the accepted standard and uses
	///		row vectors and left-to-right matrix multiplication.
	///		<p/>
	///		The engine deals with the differences between D3D and OpenGL etc.
	///		internally when operating through different render systems. The engine
	///		users only need to conform to standard maths conventions, i.e.
	///		right-to-left matrix multiplication, (The engine transposes matrices it
	///		passes to D3D to compensate).
	///		<p/>
	///		The generic form M * V which shows the layout of the matrix 
	///		entries is shown below:
	///		<p/>
	///		| m[0][0]  m[0][1]  m[0][2]  m[0][3] |   {x}
	///		| m[1][0]  m[1][1]  m[1][2]  m[1][3] |   {y}
	///		| m[2][0]  m[2][1]  m[2][2]  m[2][3] |   {z}
	///		| m[3][0]  m[3][1]  m[3][2]  m[3][3] |   {1}
	///	</remarks>
	// TESTME
	[XmlType("Matrix4"),StructLayout(LayoutKind.Sequential),
	Serializable,TypeConverter(typeof(ExpandableObjectConverter))]
	public struct Matrix4
	{
		#region Member variables

		//private float[,] m = new float[4,4];
		public float m00, m01, m02, m03;
		public float m10, m11, m12, m13;
		public float m20, m21, m22, m23;
		public float m30, m31, m32, m33;

		private readonly static Matrix4 zeroMatrix = new Matrix4(	0,0,0,0,
																											0,0,0,0,
																											0,0,0,0,
																											0,0,0,0);
		private readonly static Matrix4 identityMatrix = new Matrix4(	1,0,0,0,
																												0,1,0,0,
																												0,0,1,0,
																												0,0,0,1);

		#endregion

		#region Constructors

		/// <summary>
		///		Creates a new Matrix4 with all the specified parameters.
		/// </summary>
		public Matrix4(	float m00, float m01, float m02, float m03, 
									float m10, float m11, float m12, float m13,
									float m20, float m21, float m22, float m23,
									float m30, float m31, float m32, float m33)
		{
			this.m00 = m00; this.m01 = m01; this.m02 = m02; this.m03 = m03;
			this.m10 = m10; this.m11 = m11; this.m12 = m12; this.m13 = m13;
			this.m20 = m20; this.m21 = m21; this.m22 = m22; this.m23 = m23;
			this.m30 = m30; this.m31 = m31; this.m32 = m32; this.m33 = m33;
		}

		#endregion

		#region Static properties
		/// <summary>
		/// Returns a matrix with the following form:
		/// | 1,0,0,0 |
		/// | 0,1,0,0 |
		/// | 0,0,1,0 |
		/// | 0,0,0,1 |
		/// </summary>
		public static Matrix4 Identity
		{
			get { return identityMatrix; }
		}

		/// <summary>
		/// Returns a matrix with all elements set to 0.
		/// </summary>
		public static Matrix4 Zero
		{
			get { return zeroMatrix; }
		}

		#endregion

		#region Public properties

		/// <summary>
		///		Gets/Sets the Translation portion of the matrix.
		///		| 0 0 0 Tx|
		///		| 0 0 0 Ty|
		///		| 0 0 0 Tz|
		///		| 0 0 0  1 |
		/// </summary>
		// TESTME
		public Vector3 Translation
		{
			get
			{
				return new Vector3(this.m03, this.m13, this.m23);
			}
			set
			{
				this.m03 = value.x;
				this.m13 = value.y;
				this.m23 = value.z;
			}
		}

		/// <summary>
		///		Gets/Sets the Translation portion of the matrix.
		///		|Sx 0  0  0 |
		///		| 0 Sy 0  0 |
		///		| 0  0 Sz 0 |
		///		| 0  0  0  0 |
		/// </summary>
		// TESTME
		public Vector3 Scale
		{
			get
			{
				return new Vector3(this.m00, this.m11, this.m22);
			}
			set
			{
				this.m00 = value.x;
				this.m11 = value.y;
				this.m22 = value.z;
			}
		}

		#endregion

		#region Public methods

		/// <summary>
		/// Swap the rows of the matrix with the columns.
		/// </summary>
		/// <returns>A transposed Matrix.</returns>
		public Matrix4 Transpose()
		{
			return new Matrix4(this.m00, this.m10, this.m20, this.m30,
				this.m01, this.m11, this.m21, this.m31,
				this.m02, this.m12, this.m22, this.m32,
				this.m03, this.m13, this.m23, this.m33);
		}

		#endregion

		#region Operator overloads

		/// <summary>
		///		Used to multiply (concatenate) two 4x4 Matrices.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static Matrix4 operator * (Matrix4 left, Matrix4 right)
		{
			Matrix4 result = new Matrix4();

			result.m00 = left.m00 * right.m00 + left.m01 * right.m10 + left.m02 * right.m20 + left.m03 * right.m30;
			result.m01 = left.m00 * right.m01 + left.m01 * right.m11 + left.m02 * right.m21 + left.m03 * right.m31;
			result.m02 = left.m00 * right.m02 + left.m01 * right.m12 + left.m02 * right.m22 + left.m03 * right.m32;
			result.m03 = left.m00 * right.m03 + left.m01 * right.m13 + left.m02 * right.m23 + left.m03 * right.m33;

			result.m10 = left.m10 * right.m00 + left.m11 * right.m10 + left.m12 * right.m20 + left.m13 * right.m30;
			result.m11 = left.m10 * right.m01 + left.m11 * right.m11 + left.m12 * right.m21 + left.m13 * right.m31;
			result.m12 = left.m10 * right.m02 + left.m11 * right.m12 + left.m12 * right.m22 + left.m13 * right.m32;
			result.m13 = left.m10 * right.m03 + left.m11 * right.m13 + left.m12 * right.m23 + left.m13 * right.m33;

			result.m20 = left.m20 * right.m00 + left.m21 * right.m10 + left.m22 * right.m20 + left.m23 * right.m30;
			result.m21 = left.m20 * right.m01 + left.m21 * right.m11 + left.m22 * right.m21 + left.m23 * right.m31;
			result.m22 = left.m20 * right.m02 + left.m21 * right.m12 + left.m22 * right.m22 + left.m23 * right.m32;
			result.m23 = left.m20 * right.m03 + left.m21 * right.m13 + left.m22 * right.m23 + left.m23 * right.m33;

			result.m30 = left.m30 * right.m00 + left.m31 * right.m10 + left.m32 * right.m20 + left.m33 * right.m30;
			result.m31 = left.m30 * right.m01 + left.m31 * right.m11 + left.m32 * right.m21 + left.m33 * right.m31;
			result.m32 = left.m30 * right.m02 + left.m31 * right.m12 + left.m32 * right.m22 + left.m33 * right.m32;
			result.m33 = left.m30 * right.m03 + left.m31 * right.m13 + left.m32 * right.m23 + left.m33 * right.m33;

			return result;
		}

		/// <summary>
		///		Transforms the given 3-D vector by the matrix, projecting the 
		///		result back into <i>w</i> = 1.
		///		<p/>
		///		This means that the initial <i>w</i> is considered to be 1.0,
		///		and then all the tree elements of the resulting 3-D vector are
		///		divided by the resulting <i>w</i>.
		/// </summary>
		/// <param name="matrix">A Matrix4.</param>
		/// <param name="vector">A Vector3.</param>
		/// <returns>A new vector.</returns>
		public static Vector3 operator * (Matrix4 matrix, Vector3 vector)
		{
			Vector3 result = new Vector3();

			float inverseW = 1.0f / ( matrix.m30 + matrix.m31 + matrix.m32 + matrix.m33 );

			result.x = ( (matrix.m00 * vector.x) + (matrix.m01 * vector.y) + (matrix.m02 * vector.z) + matrix.m03 ) * inverseW;
			result.y = ( (matrix.m10 * vector.x) + (matrix.m11 * vector.y) + (matrix.m12 * vector.z) + matrix.m13 ) * inverseW;
			result.z = ( (matrix.m20 * vector.x) + (matrix.m21 * vector.y) + (matrix.m22 * vector.z) + matrix.m23 ) * inverseW;

			return result;
		}

		/// <summary>
		///		Used to add two matrices together.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static Matrix4 operator + ( Matrix4 left, Matrix4 right )
		{
			Matrix4 result = new Matrix4();

			result.m00 = left.m00 + right.m00;
			result.m01 = left.m01 + right.m01;
			result.m02 = left.m02 + right.m02;
			result.m03 = left.m03 + right.m03;

			result.m10 = left.m10 + right.m10;
			result.m11 = left.m11 + right.m11;
			result.m12 = left.m12 + right.m12;
			result.m13 = left.m13 + right.m13;

			result.m20 = left.m20 + right.m20;
			result.m21 = left.m21 + right.m21;
			result.m22 = left.m22 + right.m22;
			result.m23 = left.m23 + right.m23;

			result.m30 = left.m30 + right.m30;
			result.m31 = left.m31 + right.m31;
			result.m32 = left.m32 + right.m32;
			result.m33 = left.m33 + right.m33;

			return result;
		}

		/// <summary>
		///		Used to subtract two matrices.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static Matrix4 operator - ( Matrix4 left, Matrix4 right )
		{
			Matrix4 result = new Matrix4();

			result.m00 = left.m00 - right.m00;
			result.m01 = left.m01 - right.m01;
			result.m02 = left.m02 - right.m02;
			result.m03 = left.m03 - right.m03;

			result.m10 = left.m10 - right.m10;
			result.m11 = left.m11 - right.m11;
			result.m12 = left.m12 - right.m12;
			result.m13 = left.m13 - right.m13;

			result.m20 = left.m20 - right.m20;
			result.m21 = left.m21 - right.m21;
			result.m22 = left.m22 - right.m22;
			result.m23 = left.m23 - right.m23;

			result.m30 = left.m30 - right.m30;
			result.m31 = left.m31 - right.m31;
			result.m32 = left.m32 - right.m32;
			result.m33 = left.m33 - right.m33;

			return result;
		}

		/// <summary>
		/// Compares two Matrix4 instances for equality.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns>true if the Matrix 4 instances are equal, false otherwise.</returns>
		public static bool operator == (Matrix4 left, Matrix4 right)
		{
			if( 
				left.m00 == right.m00 || left.m01 == right.m01 || left.m02 == right.m02 || left.m03 == right.m03 ||
				left.m10 == right.m10 || left.m11 == right.m11 || left.m12 == right.m02 || left.m13 == right.m03 ||
				left.m20 == right.m20 || left.m21 == right.m21 || left.m22 == right.m02 || left.m23 == right.m03 ||
				left.m30 == right.m30 || left.m31 == right.m31 || left.m32 == right.m02 || left.m33 == right.m03 )
				return false;
			return true;
		}

		/// <summary>
		/// Compares two Matrix4 instances for inequality.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns>true if the Matrix 4 instances are not equal, false otherwise.</returns>
		public static bool operator != (Matrix4 left, Matrix4 right)
		{
			if( 
				left.m00 != right.m00 || left.m01 != right.m01 || left.m02 != right.m02 || left.m03 != right.m03 ||
				left.m10 != right.m10 || left.m11 != right.m11 || left.m12 != right.m02 || left.m13 != right.m03 ||
				left.m20 != right.m20 || left.m21 != right.m21 || left.m22 != right.m02 || left.m23 != right.m03 ||
				left.m30 != right.m30 || left.m31 != right.m31 || left.m32 != right.m02 || left.m33 != right.m03 )
				return false;
			return true;
		}

		/// <summary>
		///		Used to allow assignment from a Matrix3 to a Matrix4 object.
		/// </summary>
		/// <param name="right"></param>
		/// <returns></returns>
		public static implicit operator Matrix4(Matrix3 right)
		{
			Matrix4 result = Matrix4.Identity;

			result.m00 = right.m00; result.m01 = right.m01; result.m02 = right.m02;
			result.m10 = right.m10; result.m11 = right.m11; result.m12 = right.m12;
			result.m20 = right.m20; result.m21 = right.m21; result.m22 = right.m22;	

			return result;
		}

		/// <summary>
		/// Allows the Matrix to be accessed like a 2d array (i.e. matrix.m23)
		/// </summary>
		public float this[int row, int col]
		{
			get 
			{
				//Debug.Assert((row >= 0 && row < 4) && (col >= 0 && col < 4), "Attempt to access Matrix4 indexer out of bounds.");

				unsafe
				{
					fixed(float* pM = &m00)
					return *(pM + ((4*row) + col)); 
				}
			}
			set 
			{ 	
				//Debug.Assert((row >= 0 && row < 4) && (col >= 0 && col < 4), "Attempt to access Matrix4 indexer out of bounds.");

				unsafe
				{
					fixed(float* pM = &m00)
						*(pM + ((4*row) + col)) = value;
				}
			}
		}

		/// <summary>
		///		Allows the Matrix to be accessed linearly (m[0] -> m[15]).  
		/// </summary>
		public float this[int index]
		{
			get
			{
				//Debug.Assert(index >= 0 && index < 16, "Attempt to access Matrix4 linear indexer out of bounds.");

				unsafe
				{
					fixed(float* pMatrix = &this.m00)
					{			
						return *(pMatrix + index);
					}
				}
			}
			set
			{
				//Debug.Assert(index >= 0 && index < 16, "Attempt to access Matrix4 linear indexer out of bounds.");

				unsafe
				{
					fixed(float* pMatrix = &this.m00)
					{			
						*(pMatrix + index) = value;
					}
				}
			}
		} 

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public float[] MakeFloatArray()
		{
			float[] floats = new float[16];

			unsafe
			{
				fixed(float* p = &m00)
				{
					for(int i = 0; i < 16; i++)
						floats[i] = *(p + i);
				}
			}

			return floats;
		}

		#endregion

		#region Object overloads

		/// <summary>
		///		Overrides the Object.ToString() method to provide a text representation of 
		///		a Matrix4.
		/// </summary>
		/// <returns>A string representation of a vector3.</returns>
		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			
			sb.AppendFormat(" | {0} {1} {2} {3} |\n", this.m00, this.m01, this.m02, this.m03);
			sb.AppendFormat(" | {0} {1} {2} {3} |\n", this.m10, this.m11, this.m12, this.m13);
			sb.AppendFormat(" | {0} {1} {2} {3} |\n", this.m20, this.m21, this.m22, this.m23);
			sb.AppendFormat(" | {0} {1} {2} {3} |\n", this.m30, this.m31, this.m32, this.m33);

			return sb.ToString();
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
			int hashCode = 0;

			unsafe
			{
				fixed(float* pM = &m00)
				{
					for(int i = 0; i < 16; i++)
						hashCode ^= (int)(*(pM + i));
				}
			}
					
			return hashCode;
		}

		/// <summary>
		///		Compares this Matrix to another object.  This should be done because the 
		///		equality operators (==, !=) have been overriden by this class.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj)
		{
			if(obj is Matrix4)
				return (this == (Matrix4)obj);
			else
				return false;
		}

		#endregion
	}
}
