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

namespace Axiom.MathLib
{
	/// <summary>
	/// This is a class which exposes static methods for various common math functions.  Currently,
	/// the methods simply wrap the methods of the System.Math class (with the exception of a few added extras).
	/// This is in case the implementation needs to be swapped out with a faster C++ implementation, if
	/// deemed that the System.Math methods are not up to far speed wise.
	/// </summary>
	/// TODO: Add overloads for all methods for all instrinsic data types (i.e. float, short, etc).
	public class MathUtil
	{

		/// <summary>
		///		Empty private constructor.  This class has nothing but static methods/properties, so a public default
		///		constructor should not be created by the compiler.  This prevents instance of this class from being
		///		created.
		/// </summary>
		private MathUtil() {}

		static Random random = new Random();

		#region Constant

		public const float PI = (float)Math.PI;
		public const float TWO_PI = (float)Math.PI * 2.0f;
		public const float RADIANS_PER_DEGREE = PI / 180.0f;
		public const float DEGREES_PER_RADIAN = 180.0f / PI;

		#endregion

		#region Static methods

		/// <summary>
		///		Converts degrees to radians.
		/// </summary>
		/// <param name="degrees"></param>
		/// <returns></returns>
		static public float DegreesToRadians(float degrees)
		{
			return degrees * RADIANS_PER_DEGREE;
		}

		/// <summary>
		///		Converts radians to degrees.
		/// </summary>
		/// <param name="radians"></param>
		/// <returns></returns>
		static public float RadiansToDegrees(float radians)
		{
			return radians * DEGREES_PER_RADIAN;
		}

		/// <summary>
		///		Returns the sine of the angle.
		/// </summary>
		/// <param name="angle"></param>
		/// <returns></returns>
		static public float Sin(float angle)
		{
			return (float)Math.Sin(angle);
		}

		/// <summary>
		///		Returns the cosine of the angle.
		/// </summary>
		/// <param name="angle"></param>
		/// <returns></returns>
		static public float Cos(float angle)
		{
			return (float)Math.Cos(angle);
		}

		/// <summary>
		///		Returns the arc cosine of the angle.
		/// </summary>
		/// <param name="angle"></param>
		/// <returns></returns>
		static public float ACos(float angle)
		{
			return (float)Math.Acos(angle);
		}

		/// <summary>
		///		Returns the arc sine of the angle.
		/// </summary>
		/// <param name="angle"></param>
		/// <returns></returns>
		static public float ASin(float angle)
		{
			return (float)Math.Asin(angle);
		}

		/// <summary>
		///		Returns the square root of a number.
		/// </summary>
		/// <remarks>This is one of the more expensive math operations.  Avoid when possible.</remarks>
		/// <param name="number"></param>
		/// <returns></returns>
		static public float Sqrt(float number)
		{
			return (float)Math.Sqrt(number);
		}

		/// <summary>
		///		Returns the absolute value of the supplied number.
		/// </summary>
		/// <param name="number"></param>
		/// <returns></returns>
		static public float Abs(float number)
		{
			return (float)Math.Abs(number);
		}

		/// <summary>
		///		Returns the tangent of the angle.
		/// </summary>
		/// <param name="angle"></param>
		/// <returns></returns>
		static public float Tan(float angle)
		{
			return (float)Math.Tan(angle);
		}

		/// <summary>
		///		Used to quickly determine the greater value between two values.
		/// </summary>
		/// <param name="value1"></param>
		/// <param name="value2"></param>
		/// <returns></returns>
		static public float Max(float value1, float value2)
		{
			return Math.Max(value1, value2);
		}

		/// <summary>
		///		Used to quickly determine the lesser value between two values.
		/// </summary>
		/// <param name="value1"></param>
		/// <param name="value2"></param>
		/// <returns></returns>
		static public float Min(float value1, float value2)
		{
			return Math.Min(value1, value2);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		static public float UnitRandom()
		{
			return (float)random.Next(Int32.MaxValue) / (float)Int32.MaxValue;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		static public float SymmetricRandom()
		{
			return 2.0f * UnitRandom() - 1.0f;
		}

		#endregion

		#region Static properties

		#endregion

	}

}
