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
	///		A class used to interpolate orientations (rotations) along a spline using 
    ///		derivatives of quaternions.
	/// </summary>
	/// <remarks>
	///		Like the PositionalSpline class, this class is about interpolating values 
	///		smoothly over a spline. Whilst PositionalSpline deals with positions (the normal
	///		sense we think about splines), this class interpolates orientations. The
	///		theory is identical, except we're now in 4-dimensional space instead of 3.
	///		<p/>
	///		In positional splines, we use the points and tangents on those points to generate
	///		control points for the spline. In this case, we use quaternions and derivatives
	///		of the quaternions (i.e. the rate and direction of change at each point). This is the
	///		same as PositionalSpline since a tangent is a derivative of a position. We effectively 
	///		generate an extra quaternion in between each actual quaternion which when take with 
	///		the original quaternion forms the 'tangent' of that quaternion.
	/// </remarks>
	/// TESTME: Done but not tested
	public class RotationalSpline
	{
		#region Member variables

		readonly private Matrix4 hermitePoly = new Matrix4(	2, -2,  1,  1,
																									-3,  3, -2, -1,
																									0,  0,  1,  0,
																									1,  0,  0,  0);

		/// <summary>Collection of control points.</summary>
		private QuaternionCollection pointList;
		/// <summary>Collection of generated tangents for the spline controls points.</summary>
		private QuaternionCollection tangentList;
		/// <summary>Specifies whether or not to recalculate tangents as each control point is added.</summary>
		private bool autoCalculateTangents;

		#endregion

		#region Constructors

		public RotationalSpline()
		{
			// intialize the vector collections
			pointList = new QuaternionCollection();
			tangentList = new QuaternionCollection();

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
		public QuaternionCollection Points
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
		public Quaternion Interpolate(float t)
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
		public Quaternion Interpolate(int index, float t)
		{
			Debug.Assert(index >= 0 && index < pointList.Count, "Spline point index overrun.");

			if((index + 1) == pointList.Count)
			{
				// can't interpolate past the end of the list, just return the last point
				return pointList[index];
			}

			// quick special cases
			if(t == 0.0f)
				return pointList[index];
			else if(t == 1.0f)
				return pointList[index + 1];

			// Time for real interpolation

			// Algorithm uses spherical quadratic interpolation
			Quaternion p = pointList[index];
			Quaternion q = pointList[index + 1];
			Quaternion a = tangentList[index];
			Quaternion b = tangentList[index + 1];

			// return the final result
			return Quaternion.Squad(t, p, a, b, q);
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
			// Just like Catmull-Rom, just more hardcore
			// BLACKBOX: Don't know how to derive this formula yet
			// let p = point[i], pInv = p.Inverse
			// tangent[i] = p * exp( -0.25 * ( log(pInv * point[i+1]) + log(pInv * point[i-1]) ) )
 
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

			Quaternion invp, part1, part2, preExp;

			// loop through the points and generate the tangents
			for(i = 0; i < numPoints; i++)
			{
				Quaternion p = pointList[i];

				// Get the inverse of p
				invp = p.Inverse();

				// special cases for first and last point in list
				if(i ==0)
				{
					part1 = (invp * pointList[i + 1]).Log();
					if(isClosed)
					{
						// Use numPoints-2 since numPoints-1 is the last point and == [0]
						part2 = (invp * pointList[numPoints - 2]).Log();
					}
					else
						part2 = (invp * p).Log();
				}
				else if(i == numPoints - 1)
				{
					if(isClosed)
					{
						// Use same tangent as already calculated for [0]
						part1 = (invp * pointList[1]).Log();
					}
					else
						part1 = (invp * p).Log();

					part2 = (invp * pointList[i - 1]).Log();
				}
				else
				{
					part1 = (invp * pointList[i + 1]).Log();
					part2 = (invp * pointList[i - 1]).Log();
				}
					
				preExp = -0.25f * (part1 + part2);
				tangentList.Add(p * preExp.Exp());
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
