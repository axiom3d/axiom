#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2006  Axiom Project Team

The overall design, and a majority of the core engine and rendering code 
contained within this library is a derivative of the open source Object Oriented 
Graphics Engine OGRE, which can be found at http://ogre.sourceforge.net.  
Many thanks to the OGRE team for maintaining such a high quality project.

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

#region Namespace Declarations

using System;
using System.Diagnostics;

#endregion Namespace Declarations

namespace DotNet3D.Math
{
    /// <summary>
    ///		A 3D box aligned with the x/y/z axes.
    /// </summary>
    /// <remarks>
    ///		This class represents a simple box which is aligned with the
    ///	    axes. Internally it only stores 2 points as the extremeties of
    ///	    the box, one which is the minima of all 3 axes, and the other
    ///	    which is the maxima of all 3 axes. This class is typically used
    ///	    for an axis-aligned bounding box (AABB) for collision and
    ///	    visibility determination.
    /// </remarks>

    public sealed class AxisAlignedBox : ICloneable
    {
        #region Fields and Properties

        #region Null Static Property

        private static readonly AxisAlignedBox _nullBox = new AxisAlignedBox();
        /// <summary>
        ///		Returns a null box
        /// </summary>
        public static AxisAlignedBox Null
        {
            get
            {
                return _nullBox;
            }
        }

        #endregion Null Static Property
			
        /// <summary>
        /// 
        /// </summary>
        public Vector3 Size
        {
            get
            {
                return maxVector - minVector;
            }
            set
            {
                Vector3 center = Center;
                Vector3 halfSize = .5f * value;
                minVector = center - halfSize;
                maxVector = center + halfSize;
            }
        }

        /// <summary>
        /// Calculate the volume of this box
        /// </summary>

        public Real Volume
        {
            get
            {
                if ( _isNull )
                {
                    return Real.Zero;
                }
                else
                {
                    Vector3 diff = maxVector - minVector;
                    return diff.x * diff.y * diff.z;
                }
            }
        }

        /// <summary>
        ///    Gets the center point of this bounding box.
        /// </summary>
        public Vector3 Center
        {
            get
            {
                return ( minVector + maxVector ) * 0.5f;
            }
            set
            {
                Vector3 halfSize = .5f * Size;
                minVector = value - halfSize;
                maxVector = value + halfSize;
            }
        }

        #region Maximum Property

        internal Vector3 maxVector = new Vector3( 0.5f, 0.5f, 0.5f );
        /// <summary>
        ///		Gets/Sets the maximum corner of the box.
        /// </summary>
        public Vector3 Maximum
        {
            get
            {
                return maxVector;
            }
            set
            {
                _isNull = false;
                maxVector = value;
                _updateCorners();
            }
        }

        #endregion Maximum Property
			
        #region Minimum Property

        internal Vector3 minVector = new Vector3( -0.5f, -0.5f, -0.5f );
        /// <summary>
        ///		Gets/Sets the minimum corner of the box.
        /// </summary>
        public Vector3 Minimum
        {
            get
            {
                return minVector;
            }
            set
            {
                _isNull = false;
                minVector = value;
                _updateCorners();
            }
        }

        #endregion Minimum Property			

        #region Corners Property

        private Vector3[] _corners = new Vector3[ 8 ];
        /// <summary>
        ///		Returns an array of 8 corner points, useful for
        ///		collision vs. non-aligned objects.
        ///	 </summary>
        ///	 <remarks>
        ///		If the order of these corners is important, they are as
        ///		follows: The 4 points of the minimum Z face (note that
        ///		because we use right-handed coordinates, the minimum Z is
        ///		at the 'back' of the box) starting with the minimum point of
        ///		all, then anticlockwise around this face (if you are looking
        ///		onto the face from outside the box). Then the 4 points of the
        ///		maximum Z face, starting with maximum point of all, then
        ///		anticlockwise around this face (looking onto the face from
        ///		outside the box). Like this:
        ///		<pre>
        ///			 1-----2
        ///		    /|     /|
        ///		  /  |   /  |
        ///		5-----4   |
        ///		|   0-|--3
        ///		|  /   |  /
        ///		|/     |/
        ///		6-----7
        ///		</pre>
        /// </remarks>
        public Vector3[] Corners
        {
            get
            {
                Debug.Assert( _isNull != true, "Cannot get the corners of a null box." );

                // return a clone of the array (not the original)
                return (Vector3[])_corners.Clone();
                //return corners;
            }
        }

        #endregion Corners Property

        #region IsNull Property

        private bool _isNull = true;
        /// <summary>
        ///		Gets/Sets the value of whether this box is null (i.e. not dimensions, etc).
        /// </summary>
        public bool IsNull
        {
            get
            {
                return _isNull;
            }
            set
            {
                _isNull = value;
            }
        }

        #endregion IsNull Property
						

        #endregion
         
        #region Constructors

        /// <summary>
        /// 
        /// </summary>
        public AxisAlignedBox()
        {
            SetExtents( minVector, maxVector );
            _isNull = true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        public AxisAlignedBox( Vector3 min, Vector3 max )
        {
            SetExtents( min, max );
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="minX"></param>
        /// <param name="minY"></param>
        /// <param name="minZ"></param>
        /// <param name="maxX"></param>
        /// <param name="maxY"></param>
        /// <param name="maxZ"></param>
        public AxisAlignedBox( Real minX, Real minY, Real minZ,
                               Real maxX, Real maxY, Real maxZ )
        {
            SetExtents( new Vector3( minX, minY, minZ ), new Vector3( maxX, maxY, maxZ ) );
        }

        #endregion

        #region Public methods


        /// <summary>
        /// 
        /// </summary>
        /// <param name="matrix"></param>
        public void Transform( Matrix4 matrix )
        {
            // do nothing for a null box
            if ( _isNull )
                return;

            Vector3 min = new Vector3();
            Vector3 max = new Vector3();
            Vector3 temp = new Vector3();

            bool isFirst = true;
            int i;

            for ( i = 0; i < _corners.Length; i++ )
            {
                // Transform and check extents
                temp = matrix * _corners[i];
                if ( isFirst || temp.x > max.x )
                    max.x = temp.x;
                if ( isFirst || temp.y > max.y )
                    max.y = temp.y;
                if ( isFirst || temp.z > max.z )
                    max.z = temp.z;
                if ( isFirst || temp.x < min.x )
                    min.x = temp.x;
                if ( isFirst || temp.y < min.y )
                    min.y = temp.y;
                if ( isFirst || temp.z < min.z )
                    min.z = temp.z;

                isFirst = false;
            }

            SetExtents( min, max );
        }

        /// <summary>
        /// 
        /// </summary>
        private void _updateCorners()
        {
            // The order of these items is, using right-handed co-ordinates:
            // Minimum Z face, starting with Min(all), then anticlockwise
            //   around face (looking onto the face)
            // Maximum Z face, starting with Max(all), then anticlockwise
            //   around face (looking onto the face)
            _corners[0] = minVector;
            _corners[1].x = minVector.x;
            _corners[1].y = maxVector.y;
            _corners[1].z = minVector.z;
            _corners[2].x = maxVector.x;
            _corners[2].y = maxVector.y;
            _corners[2].z = minVector.z;
            _corners[3].x = maxVector.x;
            _corners[3].y = minVector.y;
            _corners[3].z = minVector.z;

            _corners[4] = maxVector;
            _corners[5].x = minVector.x;
            _corners[5].y = maxVector.y;
            _corners[5].z = maxVector.z;
            _corners[6].x = minVector.x;
            _corners[6].y = minVector.y;
            _corners[6].z = maxVector.z;
            _corners[7].x = maxVector.x;
            _corners[7].y = minVector.y;
            _corners[7].z = maxVector.z;
        }

        /// <summary>
        ///		Sets both Minimum and Maximum at once, so that UpdateCorners only
        ///		needs to be called once as well.
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        public void SetExtents( Vector3 min, Vector3 max )
        {
            _isNull = false;

            minVector = min;
            maxVector = max;

            _updateCorners();
        }

        /// <summary>
        ///    Scales the size of the box by the supplied factor.
        /// </summary>
        /// <param name="factor">Factor of scaling to apply to the box.</param>
        public void Scale( Vector3 factor )
        {
            Vector3 min = minVector * factor;
            Vector3 max = maxVector * factor;

            SetExtents( min, max );
        }

        #endregion

        #region Intersection Methods

        /// <summary>
        ///		Returns whether or not this box intersects another.
        /// </summary>
        /// <param name="box2"></param>
        /// <returns>True if the 2 boxes intersect, false otherwise.</returns>
        public bool Intersects( AxisAlignedBox box2 )
        {
            // Early-fail for nulls
            if ( this.IsNull || box2.IsNull )
                return false;

            // Use up to 6 separating planes
            if ( this.maxVector.x < box2.minVector.x )
                return false;
            if ( this.maxVector.y < box2.minVector.y )
                return false;
            if ( this.maxVector.z < box2.minVector.z )
                return false;

            if ( this.minVector.x > box2.maxVector.x )
                return false;
            if ( this.minVector.y > box2.maxVector.y )
                return false;
            if ( this.minVector.z > box2.maxVector.z )
                return false;

            // otherwise, must be intersecting
            return true;
        }

        /// <summary>
        ///		Tests whether the vector point is within this box.
        /// </summary>
        /// <param name="vector"></param>
        /// <returns>True if the vector is within this box, false otherwise.</returns>
        public bool Intersects( Vector3 vector )
        {
            return ( vector.x >= minVector.x && vector.x <= maxVector.x &&
                vector.y >= minVector.y && vector.y <= maxVector.y &&
                vector.z >= minVector.z && vector.z <= maxVector.z );
        }

        /// <summary>
        ///		Tests whether this box intersects a sphere.
        /// </summary>
        /// <param name="sphere"></param>
        /// <returns>True if the sphere intersects, false otherwise.</returns>
        public bool Intersects( Sphere sphere )
        {
            return DotNet3D.Math.Intersection.Test( sphere, this );
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="plane"></param>
        /// <returns>True if the plane intersects, false otherwise.</returns>
        public bool Intersects( Plane plane )
        {
            return DotNet3D.Math.Intersection.Test( plane, this );
        }


		/// <summary>
		/// Calculate the area of intersection of this box and another
		/// </summary>
		public AxisAlignedBox Intersection( AxisAlignedBox b2)
		{
			if ( !Intersects( b2 ) ) 
			{
				return new AxisAlignedBox();
			}
			Vector3 intMin, intMax;

			Vector3 b2max = b2.maxVector;
			Vector3 b2min = b2.minVector;

            if ( b2max.x > maxVector.x && maxVector.x > b2min.x )
				intMax.x = Maximum.x;
			else 
				intMax.x = b2max.x;
            if ( b2max.y > maxVector.y && maxVector.y > b2min.y )
                intMax.y = maxVector.y;
			else 
				intMax.y = b2max.y;
            if ( b2max.z > maxVector.z && maxVector.z > b2min.z )
                intMax.z = maxVector.z;
			else 
				intMax.z = b2max.z;

            if ( b2min.x < minVector.x && minVector.x < b2max.x )
                intMin.x = minVector.x;
			else
				intMin.x= b2min.x;
            if ( b2min.y < minVector.y && minVector.y < b2max.y )
                intMin.y = minVector.y;
			else
				intMin.y= b2min.y;
            if ( b2min.z < minVector.z && minVector.z < b2max.z )
                intMin.z = minVector.z;
			else
				intMin.z= b2min.z;

			return new AxisAlignedBox(intMin, intMax);

		}

        #endregion Intersection Methods


        #region Operator Overloads

        /// <summary>
        /// 
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator ==( AxisAlignedBox left, AxisAlignedBox right )
        {
            return left._isNull && right._isNull ||
                ( left._corners[0] == right._corners[0] && left._corners[1] == right._corners[1] && left._corners[2] == right._corners[2] &&
                left._corners[3] == right._corners[3] && left._corners[4] == right._corners[4] && left._corners[5] == right._corners[5] &&
                left._corners[6] == right._corners[6] && left._corners[7] == right._corners[7] );
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator !=( AxisAlignedBox left, AxisAlignedBox right )
        {
            return left._isNull != right._isNull ||
                ( left._corners[0] != right._corners[0] || left._corners[1] != right._corners[1] || left._corners[2] != right._corners[2] ||
                left._corners[3] != right._corners[3] || left._corners[4] != right._corners[4] || left._corners[5] != right._corners[5] ||
                left._corners[6] != right._corners[6] || left._corners[7] != right._corners[7] );
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals( object obj )
        {
            return obj is AxisAlignedBox && this == (AxisAlignedBox)obj;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            if ( _isNull )
                return 0;
            return _corners[0].GetHashCode() ^ _corners[1].GetHashCode() ^ _corners[2].GetHashCode() ^ _corners[3].GetHashCode() ^ _corners[4].GetHashCode() ^
                _corners[5].GetHashCode() ^ _corners[6].GetHashCode() ^ _corners[7].GetHashCode();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.minVector.ToString() + ":" + this.maxVector.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static AxisAlignedBox Parse( string text )
        {
            string[] parts = text.Split( ':' );
            return new AxisAlignedBox( Vector3.Parse( parts[0] ), Vector3.Parse( parts[1] ) );
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="center"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public static AxisAlignedBox FromDimensions( Vector3 center, Vector3 size )
        {
            Vector3 halfSize = .5f * size;
            return new AxisAlignedBox( center - halfSize, center + halfSize );
        }

        /// <summary>
        ///		Allows for merging two boxes together (combining).
        /// </summary>
        /// <param name="box">Source box.</param>
        public void Merge( AxisAlignedBox box )
        {
            // nothing to merge with in this case, just return
            if ( box.IsNull )
            {
                return;
            }
            else if ( _isNull )
            {
                SetExtents( box.Minimum, box.Maximum );
            }
            else
            {
                Vector3 min = minVector;
                Vector3 max = maxVector;
                min.ToFloor( box.Minimum );
                max.ToCeiling( box.Maximum );

                SetExtents( min, max );
            }
        }

        /// <summary>
        /// Extends the box to encompass the specified point (if needed).
        /// </summary>
        /// <param name="point">Source point.</param>
        public void Merge( Vector3 point )
		{
            if ( _isNull )
            { // if null, use this point
                SetExtents( point, point );
            }
            else
            {
                Maximum.ToCeiling( point );
                Minimum.ToFloor( point );
                _updateCorners();
            }
		}
        #endregion

        #region ICloneable Members

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public object Clone()
        {
            return new AxisAlignedBox( this.minVector, this.maxVector );
        }

        #endregion
    }
}
