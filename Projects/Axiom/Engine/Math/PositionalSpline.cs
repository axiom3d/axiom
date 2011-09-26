#region LGPL License
/*
Axiom Graphics Engine Library
Copyright © 2003-2011 Axiom Project Team

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

#region SVN Version Information
// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Diagnostics;

using Axiom.Math.Collections;
using Axiom.Utilities;
using System.Collections.Generic;

#endregion Namespace Declarations

namespace Axiom.Math
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
	public sealed class PositionalSpline : Spline<Vector3>
	{
		#region Constructors

		/// <summary>
		///		Creates a new Positional Spline.
		/// </summary>
		public PositionalSpline()
			: base()
		{
			// intialize the vector collections
			pointList = new List<Vector3>();
			tangentList = new List<Vector3>();

			// do not auto calculate tangents by default
			//autoCalculateTangents = false; //[FXCop Optimization : Do not initialize unnecessarily]
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
		public override Vector3 Interpolate( Real t )
		{
			// This does not take into account that points may not be evenly spaced.
			// This will cause a change in velocity for interpolation.

			// What segment this is in?
			var segment = t * ( pointList.Count - 1 );
			var segIndex = (int)segment;

			// apportion t
			t = segment - segIndex;

			// call the overloaded method
			return Interpolate( segIndex, t );
		}

		/// <summary>
		///		Interpolates a single segment of the spline given a parametric value.
		/// </summary>
		/// <param name="index">The point index to treat as t=0. index + 1 is deemed to be t=1</param>
		/// <param name="t">Parametric value</param>
		/// <returns>An interpolated point along the spline.</returns>
		public override Vector3 Interpolate( int index, Real t )
		{
			Contract.Requires( index >= 0, "index", "Spline point index underrun." );
			Contract.Requires( index < pointList.Count, "index", "Spline point index overrun." );

			if ( ( index + 1 ) == pointList.Count )
			{
				// cant interpolate past the end of the list, just return the last point
				return pointList[ index ];
			}

			// quick special cases
			if ( t == 0.0f )
				return pointList[ index ];
			else if ( t == 1.0f )
				return pointList[ index + 1 ];

			// Time for real interpolation
			// Construct a Vector4 of powers of 2
			Real t2, t3;
			// t^2
			t2 = t * t;
			// t^3
			t3 = t2 * t;

			var powers = new Vector4( t3, t2, t, 1 );

			// Algorithm is result = powers * hermitePoly * Matrix4(point1, point2, tangent1, tangent2)
			var point1 = pointList[ index ];
			var point2 = pointList[ index + 1 ];
			var tangent1 = tangentList[ index ];
			var tangent2 = tangentList[ index + 1 ];
			var point = new Matrix4();

			// create the matrix 4 with the 2 point and tangent values
			point.m00 = point1.x;
			point.m01 = point1.y;
			point.m02 = point1.z;
			point.m03 = 1.0f;
			point.m10 = point2.x;
			point.m11 = point2.y;
			point.m12 = point2.z;
			point.m13 = 1.0f;
			point.m20 = tangent1.x;
			point.m21 = tangent1.y;
			point.m22 = tangent1.z;
			point.m23 = 1.0f;
			point.m30 = tangent2.x;
			point.m31 = tangent2.y;
			point.m32 = tangent2.z;
			point.m33 = 1.0f;

			// get the final result in a Vector4
			var result = powers * hermitePoly * point;

			// return the final result
			return new Vector3( result.x, result.y, result.z );
		}

		/// <summary>
		///		Recalculates the tangents associated with this spline. 
		/// </summary>
		/// <remarks>
		///		If you tell the spline not to update on demand by setting AutoCalculate to false,
		///		then you must call this after completing your updates to the spline points.
		/// </remarks>
		public override void RecalculateTangents()
		{
			// Catmull-Rom approach
			// tangent[i] = 0.5 * (point[i+1] - point[i-1])

			// TODO: Resize tangent list and use existing elements rather than clear/readd every time.

			tangentList.Clear();

			int i, numPoints;
			bool isClosed;

			numPoints = pointList.Count;

			// if there arent at least 2 points, there is nothing to inerpolate
			if ( numPoints < 2 )
				return;

			// closed or open?
			if ( pointList[ 0 ] == pointList[ numPoints - 1 ] )
				isClosed = true;
			else
				isClosed = false;

			// loop through the points and generate the tangents
			for ( i = 0; i < numPoints; i++ )
			{
				// special cases for first and last point in list
				if ( i == 0 )
				{
					if ( isClosed )
					{
						// Use numPoints-2 since numPoints-1 is the last point and == [0]
						tangentList.Add( 0.5f * ( pointList[ 1 ] - pointList[ numPoints - 2 ] ) );
					}
					else
						tangentList.Add( 0.5f * ( pointList[ 1 ] - pointList[ 0 ] ) );
				}
				else if ( i == numPoints - 1 )
				{
					if ( isClosed )
					{
						// Use same tangent as already calculated for [0]
						tangentList.Add( tangentList[ 0 ] );
					}
					else
						tangentList.Add( 0.5f * ( pointList[ i ] - pointList[ i - 1 ] ) );
				}
				else
					tangentList.Add( 0.5f * ( pointList[ i + 1 ] - pointList[ i - 1 ] ) );
			}
		}

		#endregion
	}
}
