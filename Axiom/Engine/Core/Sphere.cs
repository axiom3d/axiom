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
using Axiom.MathLib;

namespace Axiom.Core
{
	/// <summary>
	///		A standard sphere, used mostly for bounds checking.
	/// </summary>
	/// <remarks>
	///		A sphere in math texts is normally represented by the function
	///		x^2 + y^2 + z^2 = r^2 (for sphere's centered on the origin). We store spheres
	///		simply as a center point and a radius.
	/// </remarks>
	public class Sphere
	{
		#region Protected member variables

		protected float radius;
		protected Vector3 center;

		#endregion

		#region Constructors

		/// <summary>
		///		Creates a unit sphere centered at the origin.
		/// </summary>
		public Sphere()
		{	
			radius = 1.0f;
			center = Vector3.Zero;
		}

		/// <summary>
		/// Creates an arbitrary spehere.
		/// </summary>
		/// <param name="center">Center point of the sphere.</param>
		/// <param name="radius">Radius of the sphere.</param>
		public Sphere(Vector3 center, float radius)
		{
			this.center = center;
			this.radius = radius;
		}

		#endregion

		#region Properties

		/// <summary>
		///		Gets/Sets the center of the sphere.
		/// </summary>
		public Vector3 Center
		{
			get { return center; }
			set { center = value; }
		}

		/// <summary>
		///		Gets/Sets the radius of the sphere.
		/// </summary>
		public float Radius
		{
			get { return radius; }
			set { radius = value; }
		}

		#endregion
	}
}
