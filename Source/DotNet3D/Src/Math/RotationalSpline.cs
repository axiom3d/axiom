#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2006  Axiom Project Team

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
using System.Diagnostics;
using DotNet3D.Math.Collections;

namespace DotNet3D.Math
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
    public sealed class RotationalSpline
    {
        #region Fields and Properties

        readonly private Matrix4 _hermitePoly = new Matrix4( 2, -2, 1, 1,
            -3, 3, -2, -1,
            0, 0, 1, 0,
            1, 0, 0, 0 );

        /// <summary>Collection of control points.</summary>
        private QuaternionCollection _pointList;
        /// <summary>Collection of generated tangents for the spline controls points.</summary>
        private QuaternionCollection _tangentList;

        #region AutoCalculateTangents Property

        /// <summary>Specifies whether or not to recalculate tangents as each control point is added.</summary>
        private bool _autoCalculateTangents;
        /// <summary>
        ///		Specifies whether or not to recalculate tangents as each control point is added.
        /// </summary>
        public bool AutoCalculateTangents
        {
            get
            {
                return _autoCalculateTangents;
            }
            set
            {
                _autoCalculateTangents = value;
            }
        }
        
        #endregion AutoCalculateTangents Property
			
        /// <summary>
        ///    Gets the number of control points in this spline.
        /// </summary>
        public int PointCount
        {
            get
            {
                return _pointList.Count;
            }
        }

        #endregion Fields and Properties

        #region Constructors

        /// <summary>
        ///		Creates a new Rotational Spline.
        /// </summary>
        public RotationalSpline()
        {
            // intialize the vector collections
            _pointList = new QuaternionCollection();
            _tangentList = new QuaternionCollection();

            // do not auto calculate tangents by default
            _autoCalculateTangents = false;
        }

        #endregion

        #region Public methods

        /// <summary>
        ///    Adds a control point to the end of the spline.
        /// </summary>
        /// <param name="point"></param>
        public void AddPoint( Quaternion point )
        {
            _pointList.Add( point );

            // recalc tangents if necessary
            if ( _autoCalculateTangents )
                RecalculateTangents();
        }

        /// <summary>
        ///    Removes all current control points from this spline.
        /// </summary>
        public void Clear()
        {
            _pointList.Clear();
            _tangentList.Clear();
        }

        public Quaternion Interpolate( Real t )
        {
            return Interpolate( t, true );
        }

        public Quaternion Interpolate( int index, Real t )
        {
            return Interpolate( index, t, true );
        }

        /// <summary>
        ///		Returns an interpolated point based on a parametric value over the whole series.
        /// </summary>
        /// <remarks>
        ///		Given a t value between 0 and 1 representing the parametric distance along the
        ///		whole length of the spline, this method returns an interpolated point.
        /// </remarks>
        /// <param name="t">Parametric value.</param>
        /// <param name="useShortestPath">True forces rotations to use the shortest path.</param>
        /// <returns>An interpolated point along the spline.</returns>
        public Quaternion Interpolate( Real t, bool useShortestPath )
        {
            // This does not take into account that points may not be evenly spaced.
            // This will cause a change in velocity for interpolation.

            // What segment this is in?
            float segment = t * _pointList.Count;
            int segIndex = (int)segment;

            // apportion t
            t = segment - segIndex;

            // call the overloaded method
            return Interpolate( segIndex, t, useShortestPath );
        }

        /// <summary>
        ///		Interpolates a single segment of the spline given a parametric value.
        /// </summary>
        /// <param name="index">The point index to treat as t=0. index + 1 is deemed to be t=1</param>
        /// <param name="t">Parametric value</param>
        /// <returns>An interpolated point along the spline.</returns>
        public Quaternion Interpolate( int index, Real t, bool useShortestPath )
        {
            Debug.Assert( index >= 0 && index < _pointList.Count, "Spline point index overrun." );

            if ( ( index + 1 ) == _pointList.Count )
            {
                // can't interpolate past the end of the list, just return the last point
                return _pointList[index];
            }

            // quick special cases
            if ( t == 0.0f )
            {
                return _pointList[index];
            }
            else if ( t == 1.0f )
            {
                return _pointList[index + 1];
            }

            // Time for real interpolation

            // Algorithm uses spherical quadratic interpolation
            Quaternion p = _pointList[index];
            Quaternion q = _pointList[index + 1];
            Quaternion a = _tangentList[index];
            Quaternion b = _tangentList[index + 1];

            // return the final result
            return Quaternion.Squad( t, p, a, b, q, useShortestPath );
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

            numPoints = _pointList.Count;

            // if there arent at least 2 points, there is nothing to inerpolate
            if ( numPoints < 2 )
                return;

            // closed or open?
            if ( _pointList[0] == _pointList[numPoints - 1] )
                isClosed = true;
            else
                isClosed = false;

            Quaternion invp, part1, part2, preExp;

            // loop through the points and generate the tangents
            for ( i = 0; i < numPoints; i++ )
            {
                Quaternion p = _pointList[i];

                // Get the inverse of p
                invp = p.Inverse();

                // special cases for first and last point in list
                if ( i == 0 )
                {
                    part1 = ( invp * _pointList[i + 1] ).Log();
                    if ( isClosed )
                    {
                        // Use numPoints-2 since numPoints-1 is the last point and == [0]
                        part2 = ( invp * _pointList[numPoints - 2] ).Log();
                    }
                    else
                        part2 = ( invp * p ).Log();
                }
                else if ( i == numPoints - 1 )
                {
                    if ( isClosed )
                    {
                        // Use same tangent as already calculated for [0]
                        part1 = ( invp * _pointList[1] ).Log();
                    }
                    else
                        part1 = ( invp * p ).Log();

                    part2 = ( invp * _pointList[i - 1] ).Log();
                }
                else
                {
                    part1 = ( invp * _pointList[i + 1] ).Log();
                    part2 = ( invp * _pointList[i - 1] ).Log();
                }

                preExp = -0.25f * ( part1 + part2 );
                _tangentList.Add( p * preExp.Exp() );
            }
        }

        #endregion
    }
}
