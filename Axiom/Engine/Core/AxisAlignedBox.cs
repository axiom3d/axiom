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
using Axiom.MathLib;

namespace Axiom.Core
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
	// TESTME
	public class AxisAlignedBox : ICloneable
	{
		#region Protected member variables

		protected Vector3 minVector = new Vector3(-0.5f, -0.5f, -0.5f);
		protected Vector3 maxVector = new Vector3(0.5f, 0.5f, 0.5f);
		protected Vector3[] corners = new Vector3[8];
		protected readonly static Vector3[] blankCorners = new Vector3[8];
		protected bool isNull;
		protected static readonly AxisAlignedBox nullBox = new AxisAlignedBox();

		#endregion

		#region Constructors

		public AxisAlignedBox()
		{
			// init the corners array
			if(blankCorners == null)
			{
				for(int i = 0; i < 8; i++)
					blankCorners[i] = new Vector3();
			}

			//Array.Copy(blankCorners, corners, 8);

			SetExtents(minVector, maxVector);
			isNull = true;
		}

		public AxisAlignedBox(Vector3 min, Vector3 max)
		{
			SetExtents(min, max);
		}

		#endregion 

		#region Public methods

		/// <summary>
		/// 
		/// </summary>
		/// <param name="matrix"></param>
		public void Transform(Matrix4 matrix)
		{
			// do nothing for a null box
			if(isNull)
				return;

			Vector3 min = new Vector3();
			Vector3 max = new Vector3();
			Vector3 temp = new Vector3();

			bool isFirst = true;
			int i;

			for( i = 0; i < 8; ++i )
			{
				// Transform and check extents
				temp = matrix * corners[i];
				if( isFirst || temp.x > max.x )
					max.x = temp.x;
				if( isFirst || temp.y > max.y )
					max.y = temp.y;
				if( isFirst || temp.z > max.z )
					max.z = temp.z;
				if( isFirst || temp.x < min.x )
					min.x = temp.x;
				if( isFirst || temp.y < min.y )
					min.y = temp.y;
				if( isFirst || temp.z < min.z )
					min.z = temp.z;

				isFirst = false;
			}

			SetExtents(min, max);
		}

		/// <summary>
		/// 
		/// </summary>
		protected void UpdateCorners()
		{
			// The order of these items is, using right-handed co-ordinates:
			// Minimum Z face, starting with Min(all), then anticlockwise
			//   around face (looking onto the face)
			// Maximum Z face, starting with Max(all), then anticlockwise
			//   around face (looking onto the face)
			corners[0] = minVector;
			corners[1].x = minVector.x; corners[1].y = maxVector.y; corners[1].z = minVector.z;
			corners[2].x = maxVector.x; corners[2].y = maxVector.y; corners[2].z = minVector.z;
			corners[3].x = maxVector.x; corners[3].y = minVector.y; corners[3].z = minVector.z;            

			corners[4] = maxVector;
			corners[5].x = minVector.x; corners[5].y = maxVector.y; corners[5].z = maxVector.z;
			corners[6].x = minVector.x; corners[6].y = minVector.y; corners[6].z = maxVector.z;
			corners[7].x = maxVector.x; corners[7].y = minVector.y; corners[7].z = maxVector.z;            
		}

		/// <summary>
		///		Sets both Minimum and Maximum at once, so that UpdateCorners only
		///		needs to be called once as well.
		/// </summary>
		/// <param name="min"></param>
		/// <param name="max"></param>
		public void SetExtents(Vector3 min, Vector3 max)
		{
			isNull = false;

			this.minVector = min;
			this.maxVector = max;
			UpdateCorners();
		}

		#endregion

		#region Properties

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
				isNull = false;
				maxVector = value;
				UpdateCorners();
			}
		}

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
				isNull = false;
				minVector = value;
				UpdateCorners();
			}
		}

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
				Debug.Assert(isNull != true, "Cannot get the corners of a null box.");

				// return a clone of the array (not the original)
				return (Vector3[])corners.Clone();
				//return corners;
			}
		}

		/// <summary>
		///		Gets/Sets the value of whether this box is null (i.e. not dimensions, etc).
		/// </summary>
		public bool IsNull
		{
			get { return isNull; }
			set { isNull = value; }
		}

		/// <summary>
		///		Returns a null box
		/// </summary>
		public static AxisAlignedBox Null
		{
			get { return new AxisAlignedBox(); }
		}

		#endregion

		#region Operator Overloads

		/// <summary>
		///		Allows for merging two boxes together (combining).
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public void Merge(AxisAlignedBox box)
		{
			// nothing to merge with in this case, just return
			if(box.IsNull)
			{
				return;
			}
			else if (isNull)
			{
				SetExtents(box.Minimum, box.Maximum);
			}
			else
			{
				Vector3 min = minVector;
				Vector3 max = maxVector;
				min.Floor(box.Minimum);
				max.Ceil(box.Maximum);

				SetExtents(min, max);
			}
		}

		#endregion

		#region ICloneable Members

		public object Clone()
		{
			return new AxisAlignedBox(this.minVector, this.maxVector);
		}

		#endregion
	}
}
