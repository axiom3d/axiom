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
	/// Summary description for Quaternion.
	/// </summary>
	[XmlType("Quaternion"),StructLayout(LayoutKind.Sequential),
	Serializable,TypeConverter(typeof(ExpandableObjectConverter))]
	public struct Quaternion
	{
		#region Private member variables and constants

		const float EPSILON = 1e-03f;

		public float w, x, y, z;

		private static readonly Quaternion identityQuat = new Quaternion(1.0f, 0.0f, 0.0f, 0.0f);
		private static readonly Quaternion zeroQuat = new Quaternion(0.0f, 0.0f, 0.0f, 0.0f);
		private static readonly int[] next = new int[3]{ 1, 2, 0 };
		
		#endregion

		#region Constructors

//		public Quaternion()
//		{
//			this.w = 1.0f;
//		}

		public Quaternion(float w, float x, float y, float z)
		{
			this.w = w;
			this.x = x;
			this.y = y;
			this.z = z;
		}

		#endregion

		#region Operator Overloads

		/// <summary>
		/// Used to multiply 2 Quaternions together.
		/// </summary>
		/// <remarks>
		///		Quaternion multiplication is not communative in most cases.
		///		i.e. p*q != q*p
		/// </remarks>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static Quaternion operator * (Quaternion left, Quaternion right)
		{
			return new Quaternion
				(
				left.w * right.w - left.x * right.x - left.y * right.y - left.z * right.z,
				left.w * right.x + left.x * right.w + left.y * right.z - left.z * right.y,
				left.w * right.y + left.y * right.w + left.z * right.x - left.x * right.z,
				left.w * right.z + left.z * right.w + left.x * right.y - left.y * right.x
				);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="quat"></param>
		/// <param name="vector"></param>
		/// <returns></returns>
		public static Vector3 operator*(Quaternion quat, Vector3 vector)
		{
			// nVidia SDK implementation
			Vector3 uv, uuv;
			Vector3 qvec = new Vector3(quat.x, quat.y, quat.z);

			uv = qvec.Cross(vector); 
			uuv = qvec.Cross(uv); 
			uv *= (2.0f * quat.w); 
			uuv *= 2.0f; 
		
			return vector + uv + uuv;

			// get the rotation matrix of the Quaternion and multiply it times the vector
			//return quat.ToRotationMatrix() * vector;
		}

		/// <summary>
		/// Used when a float value is multiplied by a Quaternion.
		/// </summary>
		/// <param name="scalar"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static Quaternion operator*(float scalar, Quaternion right)
		{
			return new Quaternion(scalar * right.w, scalar * right.x, scalar * right.y, scalar * right.z);
		}

		/// <summary>
		/// Used when a Quaternion is multiplied by a float value.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="scalar"></param>
		/// <returns></returns>
		public static Quaternion operator*(Quaternion left, float scalar)
		{
			return new Quaternion(scalar * left.w, scalar * left.x, scalar * left.y, scalar * left.z);
		}

		/// <summary>
		/// Used when a Quaternion is added to another Quaternion.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static Quaternion operator+(Quaternion left, Quaternion right)
		{
			return new Quaternion(left.w + right.w, left.x + right.x, left.y + right.y, left.z + right.z);
		}

		public static bool operator == (Quaternion left, Quaternion right)
		{
			return (left.w == right.w && left.x == right.x && left.y == right.y && left.z == right.z);
		}

		public static bool operator != (Quaternion left, Quaternion right)
		{
			return !(left == right);
		}

		#endregion

		#region Static Properties

		/// <summary>
		/// An Identity Quaternion.
		/// </summary>
		static public Quaternion Identity
		{
			get { return identityQuat; }
		}

		/// <summary>
		/// A Quaternion with all elements set to 0.0f;
		/// </summary>
		static public Quaternion Zero
		{
			get { return zeroQuat; }
		}

		#endregion

		#region Static methods

		/// <summary>
		/// 
		/// </summary>
		/// <param name="time"></param>
		/// <param name="quatA"></param>
		/// <param name="quatB"></param>
		/// <returns></returns>
		static public Quaternion Slerp(float time, Quaternion quatA, Quaternion quatB)
		{
			float cos = quatA.Dot(quatB);

			float angle = MathUtil.ACos(cos);

			if ( MathUtil.Abs(angle) < EPSILON )
				return quatA;

			float sin = MathUtil.Sin(angle);
			float inverseSin = 1.0f / sin;
			float coeff0 = MathUtil.Sin((1.0f-time) * angle) * inverseSin;
			float coeff1 = MathUtil.Sin(time * angle) * inverseSin;
			return (coeff0 * quatA + coeff1 * quatB);
		}

		/// <summary>
		/// Creates a Quaternion from a supplied angle and axis.
		/// </summary>
		/// <param name="angle">Value of an angle in radians.</param>
		/// <param name="axis">Arbitrary axis vector.</param>
		/// <returns></returns>
		static public Quaternion FromAngleAxis(float angle, Vector3 axis)
		{
			Quaternion quat = new Quaternion();

			float halfAngle = 0.5f * angle;
			float sin = MathUtil.Sin(halfAngle);

			quat.w = MathUtil.Cos(halfAngle);
			quat.x = sin * axis.x; 
			quat.y = sin * axis.y; 
			quat.z = sin * axis.z; 

			return quat;
		}

		/// <summary>
		///		Performs spherical quadratic interpolation.
		/// </summary>
		/// <param name="t"></param>
		/// <param name="p"></param>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <param name="q"></param>
		/// <returns></returns>
		static public Quaternion Squad(float t, Quaternion p, Quaternion a, Quaternion b, Quaternion q)
		{
			float slerpT = 2.0f * t * (1.0f - t);

			// use spherical linear interpolation
			Quaternion slerpP = Slerp(t, p, q);
			Quaternion slerpQ = Slerp(t, a, b);

			// run another Slerp on the results of the first 2, and return the results
			return Slerp(slerpT, slerpP, slerpQ);
		}

		#endregion

		#region Public methods
		
		/// <summary>
		/// Performs a Dot Product operation on 2 Quaternions.
		/// </summary>
		/// <param name="quat"></param>
		/// <returns></returns>
		public float Dot(Quaternion quat)
		{
			return this.w * quat.w + this.x * quat.x + this.y * quat.y + this.z * quat.z;
		}

		/// <summary>
		/// Gets a 3x3 rotation matrix from this Quaternion.
		/// </summary>
		/// <returns></returns>
		public Matrix3 ToRotationMatrix()
		{
			Matrix3 rotation = new Matrix3();

			float tx  = 2.0f * this.x;
			float ty  = 2.0f * this.y;
			float tz  = 2.0f * this.z;
			float twx = tx * this.w;
			float twy = ty * this.w;
			float twz = tz * this.w;
			float txx = tx * this.x;
			float txy = ty * this.x;
			float txz = tz * this.x;
			float tyy = ty * this.y;
			float tyz = tz * this.y;
			float tzz = tz * this.z;

			rotation.m00 = 1.0f-(tyy+tzz);
			rotation.m01 = txy-twz;
			rotation.m02 = txz+twy;
			rotation.m10 = txy+twz;
			rotation.m11 = 1.0f-(txx+tzz);
			rotation.m12 = tyz-twx;
			rotation.m20 = txz-twy;
			rotation.m21 = tyz+twx;
			rotation.m22 = 1.0f-(txx+tyy);

			return rotation;
		}

		/// <summary>
		/// Computes the inverse of a Quaternion.
		/// </summary>
		/// <returns></returns>
		public Quaternion Inverse()
		{
			float norm = this.w * this.w + this.x * this.x + this.y * this.y + this.z * this.z;
			if ( norm > 0.0f )
			{
				float inverseNorm = 1.0f / norm;
				return new Quaternion(this.w * inverseNorm, -this.x * inverseNorm, -this.y * inverseNorm, -this.z * inverseNorm);
			}
			else
			{
				// return an invalid result to flag the error
				return Quaternion.Zero;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="xAxis"></param>
		/// <param name="yAxis"></param>
		/// <param name="zAxis"></param>
		public void ToAxes (out Vector3 xAxis, out Vector3 yAxis, out Vector3 zAxis)
		{
			xAxis = new Vector3();
			yAxis = new Vector3();
			zAxis = new Vector3();

			Matrix3 rotation = this.ToRotationMatrix();

			xAxis.x = rotation.m00;
			xAxis.y = rotation.m10;
			xAxis.z = rotation.m20;

			yAxis.x = rotation.m01;
			yAxis.y = rotation.m11;
			yAxis.z = rotation.m21;

			zAxis.x = rotation.m02;
			zAxis.y = rotation.m12;
			zAxis.z = rotation.m22;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="xAxis"></param>
		/// <param name="yAxis"></param>
		/// <param name="zAxis"></param>
		public void FromAxes(Vector3 xAxis, Vector3 yAxis, Vector3 zAxis)
		{
			Matrix3 rotation = new Matrix3();

			rotation.m00 = xAxis.x;
			rotation.m10= xAxis.y;
			rotation.m20 = xAxis.z;

			rotation.m01 = yAxis.x;
			rotation.m11 = yAxis.y;
			rotation.m21 = yAxis.z;

			rotation.m02 = zAxis.x;
			rotation.m12 = zAxis.y;
			rotation.m22 = zAxis.z;

			this.FromRotationMatrix(rotation);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="matrix"></param>
		public void FromRotationMatrix(Matrix3 matrix)
		{
			// Algorithm in Ken Shoemake's article in 1987 SIGGRAPH course notes
			// article "Quaternion Calculus and Fast Animation".

			float trace = matrix.m00 + matrix.m11 + matrix.m22;

			float root = 0.0f;

			if ( trace > 0.0f )
			{
				// |this.w| > 1/2, may as well choose this.w > 1/2
				root = MathUtil.Sqrt(trace + 1.0f);  // 2w
				this.w = 0.5f * root;
				
				root = 0.5f / root;  // 1/(4w)

				this.x = (matrix.m21 - matrix.m12) * root;
				this.y = (matrix.m02 - matrix.m20) * root;
				this.z = (matrix.m10 - matrix.m01) * root;
			}
			else
			{
				// |this.w| <= 1/2

				int i = 0;
				if ( matrix.m11 > matrix.m00 )
					i = 1;
				if ( matrix.m22 > matrix[i,i] )
					i = 2;

				int j = next[i];
				int k = next[j];

				root = MathUtil.Sqrt(matrix[i,i] - matrix[j,j] - matrix[k,k] + 1.0f);

				unsafe
				{
					fixed(float* apkQuat = &this.x)
					{
						apkQuat[i] = 0.5f * root;
						root = 0.5f / root;
					
						this.w = (matrix[k,j] - matrix[j,k]) * root;
					
						apkQuat[j] = (matrix[j,i] + matrix[i,j]) * root;
						apkQuat[k] = (matrix[k,i] + matrix[i,k]) * root;
					}
				}
			}
		}

		/// <summary>
		///		Calculates the logarithm of a Quaternion.
		/// </summary>
		/// <returns></returns>
		public Quaternion Log()
		{
			// BLACKBOX: Learn this
			// If q = cos(A)+sin(A)*(x*i+y*j+z*k) where (x,y,z) is unit length, then
			// log(q) = A*(x*i+y*j+z*k).  If sin(A) is near zero, use log(q) =
			// sin(A)*(x*i+y*j+z*k) since sin(A)/A has limit 1.

			// start off with a zero quat
			Quaternion result = Quaternion.Zero;

			if(MathUtil.Abs(w) < 1.0f)
			{
				float angle = MathUtil.ACos(w);
				float sin = MathUtil.Sin(angle);

				if(MathUtil.Abs(sin) >= EPSILON)
				{
					float coeff = angle / sin;
					result.x = coeff * x;
					result.y = coeff * y;
					result.z = coeff * z;
				}
				else
				{
					result.x = x;
					result.y = y;
					result.z = z;
				}
			}

			return result;
		}

		/// <summary>
		///		Calculates the Exponent of a Quaternion.
		/// </summary>
		/// <returns></returns>
		public Quaternion Exp()
		{
			// If q = A*(x*i+y*j+z*k) where (x,y,z) is unit length, then
			// exp(q) = cos(A)+sin(A)*(x*i+y*j+z*k).  If sin(A) is near zero,
			// use exp(q) = cos(A)+A*(x*i+y*j+z*k) since A/sin(A) has limit 1.

			float angle = MathUtil.Sqrt(x * x + y * y + z * z);
			float sin = MathUtil.Sin(angle);

			// start off with a zero quat
			Quaternion result = Quaternion.Zero;

			result.w = MathUtil.Cos(angle);

			if ( MathUtil.Abs(sin) >= EPSILON )
			{
				float coeff = sin / angle;

				result.x = coeff * x;
				result.y = coeff * y;
				result.z = coeff * z;
			}
			else
			{
				result.x = x;
				result.y = y;
				result.z = z;
			}

			return result;
		}

		#endregion

		#region Object overloads

		/// <summary>
		///		Overrides the Object.ToString() method to provide a text representation of 
		///		a Quaternion.
		/// </summary>
		/// <returns>A string representation of a Quaternion.</returns>
		public override String ToString()
		{
			return String.Format("(Angle: {0}, <{1},{2},{3})>, )", this.w, this.x, this.y, this.z);
		}
		
		public override int GetHashCode()
		{
			return (int)x ^ (int)y ^ (int)z ^ (int)w;
		}
		public override bool Equals(object obj)
		{
			Quaternion quat = (Quaternion)obj;
			
			return quat == this;
		}
		

		#endregion	
	}
}
