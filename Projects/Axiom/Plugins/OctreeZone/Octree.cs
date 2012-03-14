#region LGPL License

/*
Axiom Graphics Engine Library
Copyright © 2003-2011 Axiom Project Team

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

#region SVN Version Information

// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id:$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System.Collections.Generic;

using Axiom.Core;
using Axiom.Core.Collections;
using Axiom.Math;
using Axiom.SceneManagers.PortalConnected;

#endregion Namespace Declarations

namespace OctreeZone
{
	/// <summary>
	/// Summary description for Octree.
	/// </summary>
	public class Octree
	{
		public Octree( PCZone zone, Octree parent )
		{
			this.wireBoundingBox = null;
			HalfSize = new Vector3();

			this.parent = parent;
			NunodeList = 0;
			this.zone = zone;

			//initialize all children to null.
			for ( int i = 0; i < 2; i++ )
			{
				for ( int j = 0; j < 2; j++ )
				{
					for ( int k = 0; k < 2; k++ )
					{
						this.Children[ i, j, k ] = null;
					}
				}
			}
		}

		/// <summary>
		///  Creates the AxisAlignedBox used for culling this octree.
		/// </summary>
		/// <remarks>
		///     Since it's a loose octree, the culling bounds can be different than the actual bounds of the octree.
		/// </remarks>
		public AxisAlignedBox CullBounds
		{
			get
			{
				Vector3[] Corners = this.box.Corners;
				this.box.SetExtents( Corners[ 0 ] - HalfSize, Corners[ 4 ] + HalfSize );

				return this.box;
			}
		}

		public void AddNode( PCZSceneNode node )
		{
			this.nodeList[ node.Name ] = node;
			( (OctreeZoneData)node.GetZoneData( this.zone ) ).Octant = this;

			//update total counts.
			Ref();
		}

		public void RemoveNode( PCZSceneNode node )
		{
			//PCZSceneNode check;
			//int i;
			//int Index;

			//Index = NodeList.Count - 1;

			//for ( i = Index; i >= 0; i-- )
			//{
			//    check = ( PCZSceneNode ) NodeList.Values[ i ];

			//    if ( check == node )
			//    {
			//        ( ( OctreeZoneData ) node.GetZoneData( zone ) ).Octant = null;
			//        NodeList.RemoveAt( i );
			//        UnRef();
			//    }
			//}

			( (OctreeZoneData)node.GetZoneData( this.zone ) ).Octant = null;
			NodeList.Remove( node );
			UnRef();
		}

		/// <summary>
		///  Determines if this octree is twice as big as the given box.
		///@remarks
		///	This method is used by the OctreeSceneManager to determine if the given
		///	box will fit into a child of this octree.
		/// </summary>
		public bool IsTwiceSize( AxisAlignedBox box )
		{
			Vector3[] pts1 = this.box.Corners;
			Vector3[] pts2 = box.Corners;

			return ( ( pts2[ 4 ].x - pts2[ 0 ].x ) <= ( pts1[ 4 ].x - pts1[ 0 ].x ) / 2 ) && ( ( pts2[ 4 ].y - pts2[ 0 ].y ) <= ( pts1[ 4 ].y - pts1[ 0 ].y ) / 2 ) && ( ( pts2[ 4 ].z - pts2[ 0 ].z ) <= ( pts1[ 4 ].z - pts1[ 0 ].z ) / 2 );
		}

		/// <summary>
		/// Returns the appropriate indexes for the child of this octree into which the box will fit.
		///@remarks
		///	This is used by the OCtreeSceneManager to determine which child to traverse next when
		///finding the appropriate octree to insert the box.  Since it is a loose octree, only the
		///center of the box is checked to determine the octant.
		/// </summary>
		public void GetChildIndexes( AxisAlignedBox aabox, out int x, out int y, out int z )
		{
			Vector3 max = this.box.Maximum;
			Vector3 min = aabox.Minimum;

			Vector3 Center = this.box.Maximum.MidPoint( this.box.Minimum );
			Vector3 CheckCenter = aabox.Maximum.MidPoint( aabox.Minimum );

			if ( CheckCenter.x > Center.x )
			{
				x = 1;
			}
			else
			{
				x = 0;
			}


			if ( CheckCenter.y > Center.y )
			{
				y = 1;
			}
			else
			{
				y = 0;
			}


			if ( CheckCenter.z > Center.z )
			{
				z = 1;
			}
			else
			{
				z = 0;
			}
		}

		public void Ref()
		{
			this.nunodeList++;

			if ( this.parent != null )
			{
				this.parent.Ref();
			}
		}

		public void UnRef()
		{
			this.nunodeList--;

			if ( this.parent != null )
			{
				this.parent.UnRef();
			}
		}

		private static Intersection intersect( Ray one, AxisAlignedBox two )
		{
			// Null box?
			if ( two.IsNull )
			{
				return Intersection.OUTSIDE;
			}
			// Infinite box?
			if ( two.IsInfinite )
			{
				return Intersection.INTERSECT;
			}

			bool inside = true;
			Vector3 twoMin = two.Minimum;
			Vector3 twoMax = two.Maximum;
			Vector3 origin = one.Origin;
			Vector3 dir = one.Direction;

			var maxT = new Vector3( -1, -1, -1 );

			int i = 0;
			for ( i = 0; i < 3; i++ )
			{
				if ( origin[ i ] < twoMin[ i ] )
				{
					inside = false;
					if ( dir[ i ] > 0 )
					{
						maxT[ i ] = ( twoMin[ i ] - origin[ i ] ) / dir[ i ];
					}
				}
				else if ( origin[ i ] > twoMax[ i ] )
				{
					inside = false;
					if ( dir[ i ] < 0 )
					{
						maxT[ i ] = ( twoMax[ i ] - origin[ i ] ) / dir[ i ];
					}
				}
			}

			if ( inside )
			{
				return Intersection.INTERSECT;
			}
			int whichPlane = 0;
			if ( maxT[ 1 ] > maxT[ whichPlane ] )
			{
				whichPlane = 1;
			}
			if ( maxT[ 2 ] > maxT[ whichPlane ] )
			{
				whichPlane = 2;
			}

			if ( ( ( (int)maxT[ whichPlane ] ) & 0x80000000 ) != 0 )
			{
				return Intersection.OUTSIDE;
			}
			for ( i = 0; i < 3; i++ )
			{
				if ( i != whichPlane )
				{
					float f = origin[ i ] + maxT[ whichPlane ] * dir[ i ];
					if ( f < ( twoMin[ i ] - 0.00001f ) || f > ( twoMax[ i ] + 0.00001f ) )
					{
						return Intersection.OUTSIDE;
					}
				}
			}

			return Intersection.INTERSECT;
		}


		/** Checks how the axis aligned box intersects with the plane bounded volume
		*/

		private static Intersection intersect( PlaneBoundedVolume one, AxisAlignedBox two )
		{
			// Null box?
			if ( two.IsNull )
			{
				return Intersection.OUTSIDE;
			}
			// Infinite box?
			if ( two.IsInfinite )
			{
				return Intersection.INTERSECT;
			}

			// Get centre of the box
			Vector3 centre = two.Center;
			// Get the half-size of the box
			Vector3 halfSize = two.HalfSize;

			// For each plane, see if all points are on the negative side
			// If so, object is not visible.
			// If one or more are, it's partial.
			// If all aren't, full
			bool all_inside = true;


			foreach ( Plane plane in one.planes )
			{
				PlaneSide side = plane.GetSide( centre, halfSize );
				if ( side == one.outside )
				{
					return Intersection.OUTSIDE;
				}
				if ( side == PlaneSide.Both )
				{
					all_inside = false;
				}
			}

			if ( all_inside )
			{
				return Intersection.INSIDE;
			}
			else
			{
				return Intersection.INTERSECT;
			}
		}


		/** Checks how the second box intersects with the first.
		*/

		private static Intersection intersect( AxisAlignedBox one, AxisAlignedBox two )
		{
			// Null box?
			if ( one.IsNull || two.IsNull )
			{
				return Intersection.OUTSIDE;
			}
			if ( one.IsInfinite )
			{
				return Intersection.INSIDE;
			}
			if ( two.IsInfinite )
			{
				return Intersection.INTERSECT;
			}


			Vector3 insideMin = two.Minimum;
			Vector3 insideMax = two.Maximum;

			Vector3 outsideMin = one.Minimum;
			Vector3 outsideMax = one.Maximum;

			if ( insideMax.x < outsideMin.x || insideMax.y < outsideMin.y || insideMax.z < outsideMin.z || insideMin.x > outsideMax.x || insideMin.y > outsideMax.y || insideMin.z > outsideMax.z )
			{
				return Intersection.OUTSIDE;
			}

			bool full = ( insideMin.x > outsideMin.x && insideMin.y > outsideMin.y && insideMin.z > outsideMin.z && insideMax.x < outsideMax.x && insideMax.y < outsideMax.y && insideMax.z < outsideMax.z );

			if ( full )
			{
				return Intersection.INSIDE;
			}
			else
			{
				return Intersection.INTERSECT;
			}
		}

		/** Checks how the box intersects with the sphere.
		*/

		private static Intersection intersect( Sphere one, AxisAlignedBox two )
		{
			// Null box?
			if ( two.IsNull )
			{
				return Intersection.OUTSIDE;
			}
			if ( two.IsInfinite )
			{
				return Intersection.INTERSECT;
			}

			float sradius = one.Radius;

			sradius *= sradius;

			Vector3 scenter = one.Center;

			Vector3 twoMin = two.Minimum;
			Vector3 twoMax = two.Maximum;

			float s, d = 0;

			Vector3 mndistance = ( twoMin - scenter );
			Vector3 mxdistance = ( twoMax - scenter );

			if ( mndistance.LengthSquared < sradius && mxdistance.LengthSquared < sradius )
			{
				return Intersection.INSIDE;
			}

			//find the square of the distance
			//from the sphere to the box
			for ( int i = 0; i < 3; i++ )
			{
				if ( scenter[ i ] < twoMin[ i ] )
				{
					s = scenter[ i ] - twoMin[ i ];
					d += s * s;
				}

				else if ( scenter[ i ] > twoMax[ i ] )
				{
					s = scenter[ i ] - twoMax[ i ];
					d += s * s;
				}
			}

			bool partial = ( d <= sradius );

			if ( !partial )
			{
				return Intersection.OUTSIDE;
			}

			else
			{
				return Intersection.INTERSECT;
			}
		}

		public void _getCullBounds( out AxisAlignedBox b )
		{
			b = new AxisAlignedBox( this.box.Minimum - this.halfSize, this.box.Maximum + this.halfSize );
		}


		public void _findNodes( AxisAlignedBox t, ref List<PCZSceneNode> list, PCZSceneNode exclude, bool includeVisitors, bool full )
		{
			if ( !full )
			{
				AxisAlignedBox obox;
				_getCullBounds( out obox );

				Intersection isect = intersect( t, obox );

				if ( isect == Intersection.OUTSIDE )
				{
					return;
				}

				full = ( isect == Intersection.INSIDE );
			}

			foreach ( PCZSceneNode on in this.nodeList.Values )
			{
				if ( on != exclude && ( on.HomeZone == this.zone || includeVisitors ) )
				{
					if ( full )
					{
						// make sure the node isn't already on the list
						list.Add( on );
					}

					else
					{
						Intersection nsect = intersect( t, on.WorldAABB );

						if ( nsect != Intersection.OUTSIDE )
						{
							// make sure the node isn't already on the list
							list.Add( on );
						}
					}
				}
			}

			Octree child;

			if ( ( child = this.Children[ 0, 0, 0 ] ) != null )
			{
				child._findNodes( t, ref list, exclude, includeVisitors, full );
			}

			if ( ( child = this.Children[ 1, 0, 0 ] ) != null )
			{
				child._findNodes( t, ref list, exclude, includeVisitors, full );
			}

			if ( ( child = this.Children[ 0, 1, 0 ] ) != null )
			{
				child._findNodes( t, ref list, exclude, includeVisitors, full );
			}

			if ( ( child = this.Children[ 1, 1, 0 ] ) != null )
			{
				child._findNodes( t, ref list, exclude, includeVisitors, full );
			}

			if ( ( child = this.Children[ 0, 0, 1 ] ) != null )
			{
				child._findNodes( t, ref list, exclude, includeVisitors, full );
			}

			if ( ( child = this.Children[ 1, 0, 1 ] ) != null )
			{
				child._findNodes( t, ref list, exclude, includeVisitors, full );
			}

			if ( ( child = this.Children[ 0, 1, 1 ] ) != null )
			{
				child._findNodes( t, ref list, exclude, includeVisitors, full );
			}

			if ( ( child = this.Children[ 1, 1, 1 ] ) != null )
			{
				child._findNodes( t, ref list, exclude, includeVisitors, full );
			}
		}

		public void _findNodes( Ray t, ref List<PCZSceneNode> list, PCZSceneNode exclude, bool includeVisitors, bool full )
		{
			if ( !full )
			{
				AxisAlignedBox obox;
				_getCullBounds( out obox );

				Intersection isect = intersect( t, obox );

				if ( isect == Intersection.OUTSIDE )
				{
					return;
				}

				full = ( isect == Intersection.INSIDE );
			}

			foreach ( PCZSceneNode on in this.nodeList.Values )
			{
				if ( on != exclude && ( on.HomeZone == this.zone || includeVisitors ) )
				{
					if ( full )
					{
						// make sure the node isn't already on the list
						list.Add( on );
					}

					else
					{
						Intersection nsect = intersect( t, on.WorldAABB );

						if ( nsect != Intersection.OUTSIDE )
						{
							// make sure the node isn't already on the list
							list.Add( on );
						}
					}
				}
			}

			Octree child;

			if ( ( child = this.Children[ 0, 0, 0 ] ) != null )
			{
				child._findNodes( t, ref list, exclude, includeVisitors, full );
			}

			if ( ( child = this.Children[ 1, 0, 0 ] ) != null )
			{
				child._findNodes( t, ref list, exclude, includeVisitors, full );
			}

			if ( ( child = this.Children[ 0, 1, 0 ] ) != null )
			{
				child._findNodes( t, ref list, exclude, includeVisitors, full );
			}

			if ( ( child = this.Children[ 1, 1, 0 ] ) != null )
			{
				child._findNodes( t, ref list, exclude, includeVisitors, full );
			}

			if ( ( child = this.Children[ 0, 0, 1 ] ) != null )
			{
				child._findNodes( t, ref list, exclude, includeVisitors, full );
			}

			if ( ( child = this.Children[ 1, 0, 1 ] ) != null )
			{
				child._findNodes( t, ref list, exclude, includeVisitors, full );
			}

			if ( ( child = this.Children[ 0, 1, 1 ] ) != null )
			{
				child._findNodes( t, ref list, exclude, includeVisitors, full );
			}

			if ( ( child = this.Children[ 1, 1, 1 ] ) != null )
			{
				child._findNodes( t, ref list, exclude, includeVisitors, full );
			}
		}

		public void _findNodes( Sphere t, ref List<PCZSceneNode> list, PCZSceneNode exclude, bool includeVisitors, bool full )
		{
			if ( !full )
			{
				AxisAlignedBox obox;
				_getCullBounds( out obox );

				Intersection isect = intersect( t, obox );

				if ( isect == Intersection.OUTSIDE )
				{
					return;
				}

				full = ( isect == Intersection.INSIDE );
			}


			foreach ( PCZSceneNode on in this.nodeList.Values )
			{
				if ( on != exclude && ( on.HomeZone == this.zone || includeVisitors ) )
				{
					if ( full )
					{
						// make sure the node isn't already on the list
						list.Add( on );
					}

					else
					{
						Intersection nsect = intersect( t, on.WorldAABB );

						if ( nsect != Intersection.OUTSIDE )
						{
							// make sure the node isn't already on the list
							list.Add( on );
						}
					}
				}
			}

			Octree child;

			if ( ( child = this.Children[ 0, 0, 0 ] ) != null )
			{
				child._findNodes( t, ref list, exclude, includeVisitors, full );
			}

			if ( ( child = this.Children[ 1, 0, 0 ] ) != null )
			{
				child._findNodes( t, ref list, exclude, includeVisitors, full );
			}

			if ( ( child = this.Children[ 0, 1, 0 ] ) != null )
			{
				child._findNodes( t, ref list, exclude, includeVisitors, full );
			}

			if ( ( child = this.Children[ 1, 1, 0 ] ) != null )
			{
				child._findNodes( t, ref list, exclude, includeVisitors, full );
			}

			if ( ( child = this.Children[ 0, 0, 1 ] ) != null )
			{
				child._findNodes( t, ref list, exclude, includeVisitors, full );
			}

			if ( ( child = this.Children[ 1, 0, 1 ] ) != null )
			{
				child._findNodes( t, ref list, exclude, includeVisitors, full );
			}

			if ( ( child = this.Children[ 0, 1, 1 ] ) != null )
			{
				child._findNodes( t, ref list, exclude, includeVisitors, full );
			}

			if ( ( child = this.Children[ 1, 1, 1 ] ) != null )
			{
				child._findNodes( t, ref list, exclude, includeVisitors, full );
			}
		}


		public void _findNodes( PlaneBoundedVolume t, ref List<PCZSceneNode> list, PCZSceneNode exclude, bool includeVisitors, bool full )
		{
			if ( !full )
			{
				AxisAlignedBox obox;
				_getCullBounds( out obox );

				Intersection isect = intersect( t, obox );

				if ( isect == Intersection.OUTSIDE )
				{
					return;
				}

				full = ( isect == Intersection.INSIDE );
			}


			foreach ( PCZSceneNode on in this.nodeList.Values )
			{
				if ( on != exclude && ( on.HomeZone == this.zone || includeVisitors ) )
				{
					if ( full )
					{
						// make sure the node isn't already on the list
						list.Add( on );
					}

					else
					{
						Intersection nsect = intersect( t, on.WorldAABB );

						if ( nsect != Intersection.OUTSIDE )
						{
							// make sure the node isn't already on the list
							list.Add( on );
						}
					}
				}
			}

			Octree child;

			if ( ( child = this.Children[ 0, 0, 0 ] ) != null )
			{
				child._findNodes( t, ref list, exclude, includeVisitors, full );
			}

			if ( ( child = this.Children[ 1, 0, 0 ] ) != null )
			{
				child._findNodes( t, ref list, exclude, includeVisitors, full );
			}

			if ( ( child = this.Children[ 0, 1, 0 ] ) != null )
			{
				child._findNodes( t, ref list, exclude, includeVisitors, full );
			}

			if ( ( child = this.Children[ 1, 1, 0 ] ) != null )
			{
				child._findNodes( t, ref list, exclude, includeVisitors, full );
			}

			if ( ( child = this.Children[ 0, 0, 1 ] ) != null )
			{
				child._findNodes( t, ref list, exclude, includeVisitors, full );
			}

			if ( ( child = this.Children[ 1, 0, 1 ] ) != null )
			{
				child._findNodes( t, ref list, exclude, includeVisitors, full );
			}

			if ( ( child = this.Children[ 0, 1, 1 ] ) != null )
			{
				child._findNodes( t, ref list, exclude, includeVisitors, full );
			}

			if ( ( child = this.Children[ 1, 1, 1 ] ) != null )
			{
				child._findNodes( t, ref list, exclude, includeVisitors, full );
			}
		}

		/** It's assumed the the given box has already been proven to fit into
		* a child.  Since it's a loose octree, only the centers need to be
		* compared to find the appropriate node.
		*/

		public void _getChildIndexes( AxisAlignedBox box, ref int x, ref int y, ref int z )
		{
			Vector3 max = Box.Maximum;
			Vector3 min = Box.Minimum;

			Vector3 center = Box.Maximum.MidPoint( Box.Minimum );

			Vector3 ncenter = Box.Maximum.MidPoint( Box.Minimum );

			if ( ncenter.x > center.x )
			{
				x = 1;
			}
			else
			{
				x = 0;
			}

			if ( ncenter.y > center.y )
			{
				y = 1;
			}
			else
			{
				y = 0;
			}

			if ( ncenter.z > center.z )
			{
				z = 1;
			}
			else
			{
				z = 0;
			}
		}

		#region Nested type: Intersection

		private enum Intersection
		{
			OUTSIDE = 0,
			INSIDE = 1,
			INTERSECT = 2
		};

		#endregion

		#region Member Variables

		/** Returns the number of scene nodes attached to this octree
		*/

		/** The bounding box of the octree
		@remarks
		This is used for octant index determination and rendering, but not culling
		*/
		public Octree[ , , ] Children = new Octree[ 8, 8, 8 ];
		protected AxisAlignedBox box = new AxisAlignedBox();
		/** Creates the wire frame bounding box for this octant
		*/

		/** Vector containing the dimensions of this octree / 2
		*/
		protected Vector3 halfSize;
		protected NodeCollection nodeList = new NodeCollection();
		protected int nunodeList;

		/** 3D array of children of this octree.
		@remarks
		Children are dynamically created as needed when nodes are inserted in the Octree.
		If, later, the all the nodes are removed from the child, it is still kept arround.
		*/

		protected Octree parent;
		protected WireBoundingBox wireBoundingBox;

		protected PCZone zone;

		#endregion Member Variables

		#region Properties

		public int NunodeList
		{
			get
			{
				return this.nunodeList;
			}
			set
			{
				this.nunodeList = value;
			}
		}

		public NodeCollection NodeList
		{
			get
			{
				return this.nodeList;
			}
			//set{nodeList = value;}
		}

		public WireBoundingBox BoundingBox
		{
			get
			{
				// Create a WireBoundingBox if needed
				if ( this.wireBoundingBox == null )
				{
					this.wireBoundingBox = new WireBoundingBox();
				}

				this.wireBoundingBox.BoundingBox = this.box;
				return this.wireBoundingBox;
			}

			set
			{
				this.wireBoundingBox = value;
			}
		}

		public Vector3 HalfSize
		{
			get
			{
				return this.halfSize;
			}
			set
			{
				this.halfSize = value;
			}
		}

		public AxisAlignedBox Box
		{
			get
			{
				return this.box;
			}
			set
			{
				this.box = value;
			}
		}

		#endregion Properties
	}

	public class OctreeZoneData : ZoneData
	{
		///Octree this node is attached to.
		protected Octree mOctant;

		// octree-specific world bounding box (only includes attached objects, not children)
		protected AxisAlignedBox mOctreeWorldAABB;

		public OctreeZoneData( PCZSceneNode node, PCZone zone )
			: base( node, zone )
		{
			this.mOctant = null;
			this.mOctreeWorldAABB = AxisAlignedBox.Null;
		}

		/* Update the octreezone specific data for a node */

		public AxisAlignedBox OctreeWorldAABB
		{
			get
			{
				return this.mOctreeWorldAABB;
			}
		}

		/** Since we are loose, only check the center.
		*/

		public Octree Octant
		{
			get
			{
				return this.mOctant;
			}
			set
			{
				this.mOctant = value;
			}
		}

		public override void update()
		{
			this.mOctreeWorldAABB.IsNull = true;

			// need to use object iterator here.
			foreach ( PCZSceneNode m in mAssociatedNode.Children )
			{
				// merge world bounds of object
				//mOctreeWorldAABB.Merge(m.GetWorldBoundingBox(true));
				AxisAlignedBox b = m.WorldAABB;
				b.Transform( m.Parent.FullTransform );
				this.mOctreeWorldAABB.Merge( b );
			}

			// update the Octant for the node because things might have moved.
			// if it hasn't been added to the octree, add it, and if has moved
			// enough to leave it's current node, we'll update it.
			if ( !this.mOctreeWorldAABB.IsNull )
			{
				( (OctreeZone)mAssociatedZone ).UpdateNodeOctant( this );
			}
		}

		public bool _isIn( AxisAlignedBox box )
		{
			// Always fail if not in the scene graph or box is null
			if ( !mAssociatedNode.IsVisible || box.IsNull )
			{
				return false;
			}

			// Always succeed if AABB is infinite
			if ( box.IsInfinite )
			{
				return true;
			}

			Vector3 center = mAssociatedNode.WorldAABB.Maximum.MidPoint( mAssociatedNode.WorldAABB.Minimum );

			Vector3 bmin = box.Minimum;
			Vector3 bmax = box.Maximum;

			bool centre = ( bmax > center && bmin < center );
			if ( !centre )
			{
				return false;
			}

			// Even if covering the centre line, need to make sure this BB is not large
			// enough to require being moved up into parent. When added, bboxes would
			// end up in parent due to cascade but when updating need to deal with
			// bbox growing too large for this child
			Vector3 octreeSize = bmax - bmin;
			Vector3 nodeSize = mAssociatedNode.WorldAABB.Maximum - mAssociatedNode.WorldAABB.Minimum;
			return nodeSize < octreeSize;
		}
	}
}
