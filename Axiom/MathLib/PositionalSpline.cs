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
using System.Diagnostics;
using Axiom.MathLib.Collections;

namespace Axiom.MathLib
{
	/// <summary>
	///		A Catmull-Rom spline that can be used for interpolating translation movements.
	/// </summary>
	/// <remarks>
	///		A Catmull-Rom spline is a derivitive of the Hermite spline.  The difference is that the Hermite spline
	///		allows you to specifiy 2 endpoints and 2 tangents, then the spline is generated.  A Catmull-Rom spline
	///		allows you to just supply 1-n number of points and the tangents will be automatically calculated.
	///		<p/>
	///		Derivation of the hermite polynomial can be found here: 
	///		<a href="http://www.cs.unc.edu/~hoff/projects/comp236/curves/papers/hermite.html">Hermite splines.</a>
	/// </remarks>
	public class PositionalSpline
	{
		#region Member variables

		readonly private Matrix4 hermitePoly = new Matrix4(	2, -2,  1,  1,
																								   -3,  3, -2, -1,
																									0,  0,  1,  0,
																									1,  0,  0,  0);

		/// <summary>Collection of control points.</summary>
		private Vector3Collection pointList;
		/// <summary>Collection of generated tangents for the spline controls points.</summary>
		private Vector3Collection tangentList;
		/// <summary>Specifies whether or not to recalculate tangents as each control point is added.</summary>
		private bool autoCalculateTangents;

		#endregion

		#region Constructors

		public PositionalSpline()
		{
			// intialize the vector collections
			pointList = new Vector3Collection();
			tangentList = new Vector3Collection();

			// do not auto calculate tangents by default
			autoCalculateTangents = false;

			// add event handlers for the points collection
			pointList.Cleared += new System.EventHandler(this.PointsCleared);
			pointList.ItemAdded += new System.EventHandler(this.PointAdded);
		}

		#endregion

		#region Public properties

		/// <summary>
		///		Exposes the collection of points.  Can be added to, cleared, and accessed by index.
		/// </summary>
		public Vector3Collection Points
		{
			get { return pointList; }
		}

		/// <summary>
		///		Specifies whether or not to recalculate tangents as each control point is added.
		/// </summary>
		public bool AutoCalculate
		{
			get { return autoCalculateTangents; }
			set { autoCalculateTangents = value; }
		}

		#endregion

		#region Public methods

		/// <summary>
		///		Returns an interpolated point based on a parametric value over the whole series.
		/// </summary>
		/// <remarks>
		///		Given a t value between 0 and 1 representing the parametric distance along the
		///		whole length of the spline, this method returns an interpolated point.
		/// </remarks>
		/// <param name="t">Parametric value.</param>
		/// <returns>An interpolated point along the spline.</returns>
		public Vector3 Interpolate(float t)
		{
			// This does not take into account that points may not be evenly spaced.
			// This will cause a change in velocity for interpolation.

			// What segment this is in?
			float segment = t * pointList.Count;
			int segIndex = (int)segment;

			// apportion t
			t = segment - segIndex;

			// call the overloaded method
			return Interpolate(segIndex, t);
		}

		/// <summary>
		///		Interpolates a single segment of the spline given a parametric value.
		/// </summary>
		/// <param name="index">The point index to treat as t=0. index + 1 is deemed to be t=1</param>
		/// <param name="t">Parametric value</param>
		/// <returns>An interpolated point along the spline.</returns>
		public Vector3 Interpolate(int index, float t)
		{
			Debug.Assert(index >= 0 && index < pointList.Count, "Spline point index overrun.");

			if((index + 1) == pointList.Count)
			{
				// cant interpolate past the end of the list, just return the last point
				return pointList[index];
			}

			// quick special cases
			if(t == 0.0f)
				return pointList[index];
			else if(t == 1.0f)
				return pointList[index + 1];

			// Time for real interpolation
			// Construct a Vector4 of powers of 2
			float t2, t3;
			// t^2
			t2 = t * t; 
			// t^3
			t3 = t2 * t;

			Vector4 powers = new Vector4(t3, t2, t, 1);

			// Algorithm is result = powers * hermitePoly * Matrix4(point1, point2, tangent1, tangent2)
			Vector3 point1 = pointList[index];
			Vector3 point2 = pointList[index + 1];
			Vector3 tangent1 = tangentList[index];
			Vector3 tangent2 = tangentList[index + 1];
			Matrix4 point = new Matrix4();

			// create the matrix 4 with the 2 point and tangent values
			point[0,0] = point1.x;
			point[0,1] = point1.y;
			point[0,2] = point1.z;
			point[0,3] = 1.0f;
			point[1,0] = point2.x;
			point[1,1] = point2.y;
			point[1,2] = point2.z;
			point[1,3] = 1.0f;
			point[2,0] = tangent1.x;
			point[2,1] = tangent1.y;
			point[2,2] = tangent1.z;
			point[2,3] = 1.0f;
			point[3,0] = tangent2.x;
			point[3,1] = tangent2.y;
			point[3,2] = tangent2.z;
			point[3,3] = 1.0f;

			// get the final result in a Vector4
			Vector4 result = powers * hermitePoly * point;

			// return the final result
			return new Vector3(result.x, result.y, result.z);
		}

		/// <summary>
		///		Recalculates the tangents associated with this spline. 
		/// </summary>
		/// <remarks>
		///		If you tell the spline not to update on demand by setting AutoCalculate to false,
        ///		then you must call this after completing your updates to the spline points.
		/// </remarks>
		public void RecalculateTangents()
		{
			// Catmull-Rom approach
			// tangent[i] = 0.5 * (point[i+1] - point[i-1])
 
			int i, numPoints;
			bool isClosed;

			numPoints = pointList.Count;

			// if there arent at least 2 points, there is nothing to inerpolate
			if(numPoints < 2)
				return;

			// closed or open?
			if(pointList[0] == pointList[numPoints - 1])
				isClosed = true;
			else
				isClosed = false;

			// loop through the points and generate the tangents
			for(i = 0; i < numPoints; i++)
			{
				// special cases for first and last point in list
				if(i ==0)
				{
					if(isClosed)
					{
						// Use numPoints-2 since numPoints-1 is the last point and == [0]
						tangentList.Add(0.5f * (pointList[1] - pointList[numPoints - 2]));
					}
					else
						tangentList.Add(0.5f * (pointList[1] - pointList[0]));
				}
				else if(i == numPoints - 1)
				{
					if(isClosed)
					{
						// Use same tangent as already calculated for [0]
						tangentList.Add(tangentList[0]);
					}
					else
						tangentList.Add(0.5f * (pointList[i] - pointList[i - 1]));
				}
				else
					tangentList.Add(0.5f * (pointList[i + 1] - pointList[i - 1]));
			}
		}

		#endregion

		#region Event handlers

		private void PointAdded(object source, System.EventArgs e)
		{
			// recalc tangents if necessary
			if(autoCalculateTangents)
				RecalculateTangents();
		}

		private void PointsCleared(object source, System.EventArgs e)
		{
			// clear the tangents list when the points are cleared
			tangentList.Clear();
		}

		#endregion
	}
}
