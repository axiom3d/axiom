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
using Axiom.Enumerations;
using Axiom.MathLib;

namespace Axiom.Core
{
	/// <summary>
	/// Defines a plane in 3D space.
	/// </summary>
	/// <remarks>
	/// A plane is defined in 3D space by the equation
	/// Ax + By + Cz + D = 0
	///
	/// This equates to a vector (the normal of the plane, whose x, y
	/// and z components equate to the coefficients A, B and C
	/// respectively), and a constant (D) which is the distance along
	/// the normal you have to go to move the plane back to the origin.
	/// </remarks>
	public class Plane
	{
		private Vector3 normal;
		private float d;

		#region Constructors

		public Plane()
		{
			// TODO: Implementation
		}

		public Plane(Plane plane)
		{
			this.normal = plane.normal;
			this.d = plane.d;
		}

		public Plane(Vector3 normal, float d)
		{
			this.normal = normal;
			this.d = -d;
		}

		public Plane(Vector3 normal, Vector3 point)
		{
			this.normal = normal;
			this.d = normal.Dot(point);
		}

		#endregion

		#region Public methods

		public PlaneSide GetSide(Vector3 point)
		{
			float distance = GetDistance(point);

			if ( distance < 0.0f )
				return PlaneSide.Negative;

			if ( distance > 0.0f )
				return PlaneSide.Positive;

			return PlaneSide.None;
		}

		/// <summary>
		/// This is a pseudodistance. The sign of the return value is
		/// positive if the point is on the positive side of the plane,
		/// negative if the point is on the negative side, and zero if the
		///	 point is on the plane.
		/// The absolute value of the return value is the true distance only
		/// when the plane normal is a unit length vector.
		/// </summary>
		/// <param name="point"></param>
		/// <returns></returns>
		public float GetDistance(Vector3 point)
		{ 
			return normal.Dot(point) + this.d;
		}

		#endregion

		#region Properties

		/// <summary>
		///		The normal of the plane.
		/// </summary>
		public Vector3 Normal
		{
			get { return normal; }
			set { normal = value; }
		}

		/// <summary>
		///		The distance from the origin to the plane along the Normal vector.
		/// </summary>
		public float D
		{
			get { return d; }
			set { d = value; }
		}

		#endregion 

		#region Object overrides

		public override String ToString()
		{
			// TODO: Implementation
			return "";
		}

		#endregion
	}
}
