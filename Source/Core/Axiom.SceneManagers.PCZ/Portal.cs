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

using System;
using System.Collections.Generic;
using System.Text;
using Axiom.Core;
using Axiom.Math;

#endregion Namespace Declarations

namespace Axiom.SceneManagers.PortalConnected
{
	public enum PORTAL_TYPE
	{
		PORTAL_TYPE_QUAD,
		PORTAL_TYPE_AABB,
		PORTAL_TYPE_SPHERE,
	};

	public enum PortalIntersectResult
	{
		NO_INTERSECT,
		INTERSECT_NO_CROSS,
		INTERSECT_BACK_NO_CROSS,
		INTERSECT_CROSS
	};

	public class Portal
	{
		// Type of portal (quad, aabb, or sphere)
		protected PORTAL_TYPE mType;
		// Name (identifier) for the Portal - must be unique
		protected string mName;

		/// SceneNode (if any) this portal is attached to
		protected SceneNode mNode;

		///connected Zone
		protected PCZone mTargetZone;

		/// Zone this portal is currently owned by (in)
		protected PCZone mCurrentHomeZone;

		///zone to transfer this portal to
		protected PCZone mNewHomeZone;

		///Matching Portal in the target zone (usually in same world space
		// as this portal, but pointing the opposite direction)
		protected Portal mTargetPortal;

		/// Corners of the portal - coordinates are relative to the sceneNode
		// NOTE: there are 4 corners if the portal is a quad type
		//       there are 2 corners if the portal is an AABB type
		//       there are 2 corners if the portal is a sphere type (center and point on sphere)
		protected Vector3[] mCorners;

		/// Direction ("Norm") of the portal -
		// NOTE: For a Quad portal, determined by the 1st 3 corners.
		// NOTE: For AABB & SPHERE portals, we only have "inward" or "outward" cases.
		//       To indicate "outward", the Direction is UNIT_Z
		//		 to indicate "inward", the Direction is NEGATIVE_UNIT_Z
		protected Vector3 mDirection;

		/// Radius of the sphere enclosing the portal
		// NOTE: For aabb portals, this value is the distance from the center of the aab to a corner
		protected Real mRadius;

		// Local Centerpoint of the portal
		protected Vector3 mLocalCP;

		/// Derived (world coordinates) Corners of the portal
		// NOTE: there are 4 corners if the portal is a quad type
		//       there are 2 corners if the portal is an AABB type (min corner & max corner)
		//       there are 2 corners if the portal is a sphere type (center and point on sphere)
		protected Vector3[] mDerivedCorners;

		/// Derived (world coordinates) direction of the portal
		// NOTE: Only applicable for a Quad portal
		protected Vector3 mDerivedDirection;

		/// Derived (world coordinates) of portal (center point)
		protected Vector3 mDerivedCP;

		/// Sphere of the portal centered on the derived CP
		protected Sphere mDerivedSphere;

		/// Derived (world coordinates) Plane of the portal
		// NOTE: Only applicable for a Quad portal
		protected Plane mDerivedPlane;

		/// Previous frame portal cp (in world coordinates)
		protected Vector3 mPrevDerivedCP;

		/// Previous frame derived plane
		// NOTE: Only applicable for a Quad portal
		protected Plane mPrevDerivedPlane;

		/// flag indicating whether or not local values are up-to-date
		protected bool mLocalsUpToDate;

		// previous world transform
		protected Matrix4 prevWorldTransform;
		// flag open or closed
		private bool mOpen;


		public Portal( string name, PORTAL_TYPE type )
		{
			mType = type;
			mName = name;
			mTargetZone = null;
			mCurrentHomeZone = null;
			mNewHomeZone = null;
			mTargetPortal = null;
			mNode = null;
			mRadius = 0.0;
			mDirection = Math.Vector3.UnitZ;
			mLocalsUpToDate = false;
			mDerivedSphere = new Sphere();
			mDerivedPlane = new Plane();
			// set prevWorldTransform to a zero'd out matrix
			prevWorldTransform = Math.Matrix4.Zero;
			// default to open
			mOpen = true;
			switch ( mType )
			{
				default:
				case PORTAL_TYPE.PORTAL_TYPE_QUAD:
					mCorners = new Vector3[4];
					mDerivedCorners = new Vector3[4];
					break;
				case PORTAL_TYPE.PORTAL_TYPE_AABB:
					mCorners = new Vector3[2];
					mDerivedCorners = new Vector3[2];
					break;
				case PORTAL_TYPE.PORTAL_TYPE_SPHERE:
					mCorners = new Vector3[2];
					mDerivedCorners = new Vector3[2];
					break;
			}
		}

		~Portal()
		{
		}

		public PORTAL_TYPE Type
		{
			get
			{
				return mType;
			}
		}

		public bool IsOpen
		{
			get
			{
				return mOpen;
			}
			set
			{
				mOpen = value;
			}
		}

		// Set the SceneNode the Portal is associated with
		public void setNode( SceneNode sn )
		{
			mNode = sn;
			mLocalsUpToDate = false;
		}

		// Set the 1st Zone the Portal connects to
		public void setTargetZone( PCZone z )
		{
			mTargetZone = z;
		}

		/* Returns the name of the portal
		*/

		public string getName()
		{
			return mName;
		}

		/* Get the scene node (if any) this portal is associated with
		*/

		public SceneNode getNode()
		{
			return mNode;
		}

		/** Get the Zone the Portal connects to
		*/

		public PCZone getTargetZone()
		{
			return mTargetZone;
		}

		/** Get the Zone the Portal is currently "in"
		*/

		public PCZone getCurrentHomeZone()
		{
			return mCurrentHomeZone;
		}

		/** Get the Zone the Portal should be moved to
		*/

		public PCZone getNewHomeZone()
		{
			return mNewHomeZone;
		}

		/** Get the connected portal (if any)
		*/

		public Portal getTargetPortal()
		{
			return mTargetPortal;
		}

		/// <summary>
		///     Gets/Sets the direction vector of the portal in local space
		/// </summary>
		public Vector3 Direction
		{
			get
			{
				return mDirection;
			}
			set
			{
				mDirection = value;
			}
		}

		// Set the zone this portal is in.
		public void setCurrentHomeZone( PCZone z )
		{
			// do this here since more than one function calls setCurrentHomeZone
			// also _addPortal is abstract, so easier to do it here.
			if ( null != z )
			{
				// inform old zone of portal change.
				if ( null != mCurrentHomeZone )
				{
					mCurrentHomeZone.PortalsUpdated = true;
				}
				z.PortalsUpdated = true; // inform new zone of portal change
			}
			mCurrentHomeZone = z;
		}

		// Set the zone this portal should be moved to
		public void setNewHomeZone( PCZone z )
		{
			mNewHomeZone = z;
		}

		// Set the Portal the Portal connects to
		public void setTargetPortal( Portal p )
		{
			mTargetPortal = p;
		}

		// Set the local coordinates of one of the portal corners
		public void setCorner( int index, Vector3 pt )
		{
			mCorners[ index ] = pt;
			mLocalsUpToDate = false;
		}

		/** Set the local coordinates of all of the portal corners
		*/
		// NOTE: there are 4 corners if the portal is a quad type
		//       there are 2 corners if the portal is an AABB type (min corner & max corner)
		//       there are 2 corners if the portal is a sphere type (center and point on sphere)
		public void setCorners( Vector3[] corners )
		{
			switch ( mType )
			{
				default:
				case PORTAL_TYPE.PORTAL_TYPE_QUAD:
					mCorners[ 0 ] = corners[ 0 ];
					mCorners[ 1 ] = corners[ 1 ];
					mCorners[ 2 ] = corners[ 2 ];
					mCorners[ 3 ] = corners[ 3 ];
					break;
				case PORTAL_TYPE.PORTAL_TYPE_AABB:
					mCorners[ 0 ] = corners[ 0 ]; // minimum corner
					mCorners[ 1 ] = corners[ 1 ]; // maximum corner (opposite from min corner)
					break;
				case PORTAL_TYPE.PORTAL_TYPE_SPHERE:
					mCorners[ 0 ] = corners[ 0 ]; // center point
					mCorners[ 1 ] = corners[ 1 ]; // point on sphere surface
					break;
			}
			mLocalsUpToDate = false;
		}

		// calculate the local direction of the portal from the corners
		public void calcDirectionAndRadius()
		{
			Vector3 radiusVector;
			Vector3 side1, side2;

			switch ( mType )
			{
				default:
				case PORTAL_TYPE.PORTAL_TYPE_QUAD:
					// first calculate local direction
					side1 = mCorners[ 1 ] - mCorners[ 0 ];
					side2 = mCorners[ 2 ] - mCorners[ 0 ];
					mDirection = side1.Cross( side2 );
					mDirection.Normalize();
					// calculate local cp
					mLocalCP = Vector3.Zero;
					for ( int i = 0; i < 4; i++ )
					{
						mLocalCP += mCorners[ i ];
					}
					mLocalCP *= 0.25f;
					// then calculate radius
					radiusVector = mCorners[ 0 ] - mLocalCP;
					mRadius = radiusVector.Length;
					break;
				case PORTAL_TYPE.PORTAL_TYPE_AABB:
					// "direction" is is either pointed inward or outward and is set by user, not calculated.
					// calculate local cp
					mLocalCP = Vector3.Zero;
					for ( int i = 0; i < 2; i++ )
					{
						mLocalCP += mCorners[ i ];
					}
					mLocalCP *= 0.5f;
					// for radius, use distance from corner to center point
					// this gives the radius of a sphere that encapsulates the aabb
					radiusVector = mCorners[ 0 ] - mLocalCP;
					mRadius = radiusVector.Length;
					break;
				case PORTAL_TYPE.PORTAL_TYPE_SPHERE:
					// "direction" is is either pointed inward or outward and is set by user, not calculated.
					// local CP is same as corner point 0
					mLocalCP = mCorners[ 0 ];
					// since corner1 is point on sphere, radius is simply corner1 - center point
					radiusVector = mCorners[ 1 ] - mLocalCP;
					mRadius = radiusVector.Length;
					break;
			}
			mDerivedSphere.Radius = mRadius;
			// locals are now up to date
			mLocalsUpToDate = true;
		}

		// Calculate the local bounding sphere of the portal from the corner points
		public Real getRadius()
		{
			if ( !mLocalsUpToDate )
			{
				calcDirectionAndRadius();
			}
			return mRadius;
		}

		//Get the coordinates of one of the portal corners
		public Vector3 getCorner( int index )
		{
			return mCorners[ index ];
		}

		// Get the derived (world) coordinates of a portal corner (assumes they are up-to-date)
		public Vector3 getDerivedCorner( int index )
		{
			return mDerivedCorners[ index ];
		}

		// Get the direction of the portal in world coordinates (assumes  it is up-to-date)
		public Vector3 getDerivedDirection()
		{
			return mDerivedDirection;
		}

		// Get the position (centerpoint) of the portal in world coordinates (assumes  it is up-to-date)
		public Vector3 getDerivedCP()
		{
			return mDerivedCP;
		}

		// Get the sphere (centered on DerivedCP) of the portal in world coordinates (assumes  it is up-to-date)
		public Sphere getDerivedSphere()
		{
			return mDerivedSphere;
		}

		// Get the plane of the portal in world coordinates (assumes  it is up-to-date)
		public Plane getDerivedPlane()
		{
			return mDerivedPlane;
		}

		// Get the previous position (centerpoint) of the portal in world coordinates (assumes  it is up-to-date)
		public Vector3 getPrevDerivedCP()
		{
			return mPrevDerivedCP;
		}

		// Get the previous plane of the portal in world coordinates (assumes  it is up-to-date)
		public Plane getPrevDerivedPlane()
		{
			return mPrevDerivedPlane;
		}

		// Update (Calculate) the world spatial values
		public void updateDerivedValues()
		{
			// make sure local values are up to date
			if ( !mLocalsUpToDate )
			{
				calcDirectionAndRadius();
			}
			int numCorners = 4;
			if ( mType == PORTAL_TYPE.PORTAL_TYPE_AABB )
			{
				numCorners = 2;
			}
			else if ( mType == PORTAL_TYPE.PORTAL_TYPE_SPHERE )
			{
				numCorners = 2;
			}

			// calculate derived values
			if ( null != mNode )
			{
				if ( prevWorldTransform != mNode.FullTransform )
				{
					if ( null != mCurrentHomeZone )
					{
						// inform home zone that a portal has been updated
						mCurrentHomeZone.PortalsUpdated = true;
					}
					// save world transform
					Matrix4 transform = mNode.FullTransform;
					Matrix3 rotation;
					// save off the current DerivedCP
					mPrevDerivedCP = mDerivedCP;
					mDerivedCP = transform*mLocalCP;
					mDerivedSphere.Center = mDerivedCP;
					switch ( mType )
					{
						case PORTAL_TYPE.PORTAL_TYPE_QUAD:
							for ( int i = 0; i < numCorners; i++ )
							{
								mDerivedCorners[ i ] = transform*mCorners[ i ];
							}
							rotation = transform.ExtractRotation();
							mDerivedDirection = rotation*mDirection;
							break;
						case PORTAL_TYPE.PORTAL_TYPE_AABB:
						{
							AxisAlignedBox aabb; // = new AxisAlignedBox(mCorners[0], mCorners[1]);
							//aabb.SetExtents(mCorners[0], mCorners[1]);
							aabb = mNode.WorldAABB;
							//aabb.transform(mNode->_getFullTransform());
							mDerivedCorners[ 0 ] = aabb.Minimum;
							mDerivedCorners[ 1 ] = aabb.Maximum;
							mDerivedDirection = mDirection;
						}
							break;
						case PORTAL_TYPE.PORTAL_TYPE_SPHERE:
						{
							mDerivedCorners[ 0 ] = mDerivedCP;
							mDerivedCorners[ 1 ] = transform*mCorners[ 1 ];
							mDerivedDirection = mDirection;
						}
							break;
					}
					if ( prevWorldTransform != Matrix4.Zero )
					{
						// save previous calc'd plane
						mPrevDerivedPlane = mDerivedPlane;
						// calc new plane
						mDerivedPlane = new Plane( mDerivedDirection, mDerivedCP );
						// only update prevWorldTransform if did not move
						// we need to add this conditional to ensure that
						// the portal fully updates when it changes position.
						if ( mPrevDerivedPlane == mDerivedPlane && mPrevDerivedCP == mDerivedCP )
						{
							prevWorldTransform = transform;
						}
						mPrevDerivedCP = mDerivedCP;
					}
					else
					{
						// calc new plane
						mDerivedPlane = new Plane( mDerivedDirection, mDerivedCP );
						// this is first time, so there is no previous, so prev = current.
						mPrevDerivedPlane = mDerivedPlane;
						mPrevDerivedCP = mDerivedCP;
						prevWorldTransform = Matrix4.Identity;
						prevWorldTransform = transform;
					}
				}
			}
			else // no associated node, so just use the local values as derived values
			{
				if ( prevWorldTransform != Matrix4.Zero )
				{
					// save off the current DerivedCP
					mPrevDerivedCP = mDerivedCP;
					mDerivedCP = mLocalCP;
					mDerivedSphere.Center = mDerivedCP;
					for ( int i = 0; i < numCorners; i++ )
					{
						mDerivedCorners[ i ] = mCorners[ i ];
					}
					mDerivedDirection = mDirection;
					// save previous calc'd plane
					mPrevDerivedPlane = mDerivedPlane;
					// calc new plane
					mDerivedPlane = new Plane( mDerivedDirection, mDerivedCP );
				}
				else
				{
					if ( null != mCurrentHomeZone )
					{
						// this case should only happen once
						mCurrentHomeZone.PortalsUpdated = true;
					}
					// this is the first time the derived CP has been calculated, so there
					// is no "previous" value, so set previous = current.
					mDerivedCP = mLocalCP;
					mPrevDerivedCP = mDerivedCP;
					mDerivedSphere.Center = mDerivedCP;
					for ( int i = 0; i < numCorners; i++ )
					{
						mDerivedCorners[ i ] = mCorners[ i ];
					}
					mDerivedDirection = mDirection;
					// calc new plane
					mDerivedPlane = new Plane( mDerivedDirection, mDerivedCP );
					// this is first time, so there is no previous, so prev = current.
					mPrevDerivedPlane = mDerivedPlane;
					// flag as initialized
					prevWorldTransform = Matrix4.Identity;
				}
			}
		}

		// Adjust the portal so that it is centered and oriented on the given node
		// NOTE: This function will move/rotate the node as well!
		// NOTE: The node will become the portal's "associated" node (mNode).
		public void adjustNodeToMatch( SceneNode node )
		{
			int i;

			// make sure local values are up to date
			if ( !mLocalsUpToDate )
			{
				calcDirectionAndRadius();
			}
			// move the parent node to the center point
			node.Position = mLocalCP;

			// move the corner points to be relative to the node
			int numCorners = 4;
			if ( mType == PORTAL_TYPE.PORTAL_TYPE_AABB )
			{
				numCorners = 2;
			}
			else if ( mType == PORTAL_TYPE.PORTAL_TYPE_SPHERE )
			{
				numCorners = 2;
			}

			for ( i = 0; i < numCorners; i++ )
			{
				mCorners[ i ] -= mLocalCP;
			}
			if ( mType != PORTAL_TYPE.PORTAL_TYPE_AABB && mType != PORTAL_TYPE.PORTAL_TYPE_SPHERE )
			{
				// NOTE: UNIT_Z is the basis for our local direction
				// orient the node to match the direction
				Quaternion q;
				q = Math.Vector3.UnitZ.GetRotationTo( mDirection );
				node.Orientation = q;
			}

			// set the node as the portal's associated node
			setNode( node );
		}

		// IsOpen a portal (allows scene traversal and crossing)
		public void open()
		{
			mOpen = true;
		}

		// Close a portal (disallows scene traversal and crossing)
		public void close()
		{
			mOpen = false;
		}

		// Check if a portal intersects an AABB
		// NOTE: This check is not exact.
		public bool intersects( AxisAlignedBox aab )
		{
			// Only check if portal is open
			if ( mOpen )
			{
				switch ( mType )
				{
					case PORTAL_TYPE.PORTAL_TYPE_QUAD:
						// since ogre doesn't have built in support for a quad, just check
						// if the box intersects both the sphere of the portal and the plane
						// this can result in false positives, but they will be minimal
						if ( !aab.Intersects( mDerivedSphere ) )
						{
							return false;
						}
						if ( aab.Intersects( mDerivedPlane ) )
						{
							return true;
						}
						break;
					case PORTAL_TYPE.PORTAL_TYPE_AABB:
					{
						// aab to aab check
						var aabb = new AxisAlignedBox( mDerivedCorners[ 0 ], mDerivedCorners[ 1 ] );
						return ( aab.Intersects( aabb ) );
					}
					case PORTAL_TYPE.PORTAL_TYPE_SPHERE:
						// aab to sphere check
						return ( aab.Intersects( mDerivedSphere ) );
				}
			}
			return false;
		}

		// Check if a portal intersects a sphere
		// NOTE: This check is not exact.
		public bool intersects( Sphere sphere )
		{
			// Only check if portal is open
			if ( mOpen )
			{
				switch ( mType )
				{
					case PORTAL_TYPE.PORTAL_TYPE_QUAD:
						// since ogre doesn't have built in support for a quad, just check
						// if the sphere intersects both the sphere of the portal and the plane
						// this can result in false positives, but they will be minimal
						if ( !sphere.Intersects( mDerivedSphere ) )
						{
							return false;
						}
						if ( sphere.Intersects( mDerivedPlane ) )
						{
							return true;
						}
						break;
					case PORTAL_TYPE.PORTAL_TYPE_AABB:
					{
						// aab to aab check
						var aabb = new AxisAlignedBox( mDerivedCorners[ 0 ], mDerivedCorners[ 1 ] );
						return ( aabb.Intersects( sphere ) );
					}
					case PORTAL_TYPE.PORTAL_TYPE_SPHERE:
						return ( mDerivedSphere.Intersects( sphere ) );
				}
			}
			return false;
		}

		// Check if a portal intersects a plane bounded volume
		// NOTE: This check is not exact.
		// NOTE: UNTESTED as of 5/30/07 (EC)
		public bool intersects( PlaneBoundedVolume pbv )
		{
			// Only check if portal is open
			if ( mOpen )
			{
				switch ( mType )
				{
					case PORTAL_TYPE.PORTAL_TYPE_QUAD:
					{
						// first check sphere of the portal
						if ( !pbv.Intersects( mDerivedSphere ) )
						{
							return false;
						}
						// if the portal corners are all outside one of the planes of the pbv,
						// then the portal does not intersect the pbv. (this can result in
						// some false positives, but it's the best I can do for now)
						foreach ( Plane plane in pbv.planes )
						{
							bool allOutside = true;
							for ( int i = 0; i < 4; i++ )
							{
								if ( plane.GetSide( mDerivedCorners[ i ] ) != pbv.outside )
								{
									allOutside = false;
								}
							}
							if ( allOutside )
							{
								return false;
							}
						}
					}
						break;
					case PORTAL_TYPE.PORTAL_TYPE_AABB:
					{
						var aabb = new AxisAlignedBox( mDerivedCorners[ 0 ], mDerivedCorners[ 1 ] );
						if ( !pbv.Intersects( aabb ) )
						{
							return false;
						}
					}
						break;
					case PORTAL_TYPE.PORTAL_TYPE_SPHERE:
						if ( !pbv.Intersects( mDerivedSphere ) )
						{
							return false;
						}
						break;
				}
			}
			return false;
		}

		// Check if a portal intersects a ray
		// NOTE: Kinda using my own invented routine here for quad portals... Better do a lot of testing!
		public bool intersects( Ray ray )
		{
			// Only check if portal is open
			if ( mOpen )
			{
				if ( mType == PORTAL_TYPE.PORTAL_TYPE_QUAD )
				{
					// since ogre doesn't have built in support for a quad, I'm going to first
					// find the intersection point (if any) of the ray and the portal plane.  Then
					// using the intersection point, I take the cross product of each side of the portal
					// (0,1,intersect), (1,2, intersect), (2,3, intersect), and (3,0,intersect).  If
					// all 4 cross products have vectors pointing in the same direction, then the
					// intersection point is within the portal, otherwise it is outside.

					IntersectResult result = ray.Intersects( mDerivedPlane );

					if ( result.Hit )
					{
						// the ray intersects the plane, now walk around the edges
						Vector3 isect = ray.GetPoint( result.Distance );
						Vector3 cross, vect1, vect2;
						Vector3 cross2, vect3, vect4;
						vect1 = mDerivedCorners[ 1 ] - mDerivedCorners[ 0 ];
						vect2 = isect - mDerivedCorners[ 0 ];
						cross = vect1.Cross( vect2 );
						vect3 = mDerivedCorners[ 2 ] - mDerivedCorners[ 1 ];
						vect4 = isect - mDerivedCorners[ 1 ];
						cross2 = vect3.Cross( vect4 );
						if ( cross.Dot( cross2 ) < 0 )
						{
							return false;
						}
						vect1 = mDerivedCorners[ 3 ] - mDerivedCorners[ 2 ];
						vect2 = isect - mDerivedCorners[ 2 ];
						cross = vect1.Cross( vect2 );
						if ( cross.Dot( cross2 ) < 0 )
						{
							return false;
						}
						vect1 = mDerivedCorners[ 0 ] - mDerivedCorners[ 3 ];
						vect2 = isect - mDerivedCorners[ 3 ];
						cross = vect1.Cross( vect2 );
						if ( cross.Dot( cross2 ) < 0 )
						{
							return false;
						}
						// all cross products pointing same way, so intersect
						// must be on the inside of the portal!
						return true;
					}

					return false;
				}
				else if ( mType == PORTAL_TYPE.PORTAL_TYPE_AABB )
				{
					var aabb = new AxisAlignedBox( mDerivedCorners[ 0 ], mDerivedCorners[ 1 ] );
					IntersectResult result = ray.Intersects( aabb );
					return result.Hit;
				}
				else // sphere
				{
					IntersectResult result = ray.Intersects( mDerivedSphere );
					return result.Hit;
				}
			}
			return false;
		}


		/* Test if a scene node intersected a portal during the last time delta
			* (from last frame time to current frame time).  This function checks
			* if the node "crossed over" the portal also.
		*/

		public PortalIntersectResult intersects( PCZSceneNode pczsn )
		{
			// Only check if portal is open
			if ( mOpen )
			{
				if ( pczsn == mNode )
				{
					// ignore the scene node if it is the node the portal is associated with
					return PortalIntersectResult.NO_INTERSECT;
				}
				// most complicated case - if the portal is a quad:
				if ( mType == PORTAL_TYPE.PORTAL_TYPE_QUAD )
				{
					// the node is modeled as a line segment (prevPostion to currentPosition)
					// intersection test is then between the capsule and the line segment.
					var nodeSegment = new Segment();
					nodeSegment.Set( pczsn.PreviousPosition, pczsn.DerivedPosition );

					// we model the portal as a line swept sphere (mPrevDerivedCP to mDerivedCP).
					var portalCapsule = new Capsule();
					portalCapsule.Set( mPrevDerivedCP, mDerivedCP, mRadius );

					if ( portalCapsule.Intersects( nodeSegment ) )
					{
						// the portal intersected the node at some time from last frame to this frame.
						// Now check if node "crossed" the portal
						// a crossing occurs if the "side" of the final position of the node compared
						// to the final position of the portal is negative AND the initial position
						// of the node compared to the initial position of the portal is non-negative
						if ( mDerivedPlane.GetSide( pczsn.DerivedPosition ) == PlaneSide.Negative &&
						     mPrevDerivedPlane.GetSide( pczsn.DerivedPosition ) != PlaneSide.Negative )
						{
							// safety check - make sure the node has at least one dimension which is
							// small enough to fit through the portal! (avoid the "elephant fitting
							// through a mouse hole" case)
							Vector3 nodeHalfVector = pczsn.WorldAABB.HalfSize;
							var portalBox = new Vector3( mRadius, mRadius, mRadius );
							portalBox.Floor( nodeHalfVector );
							if ( portalBox.x < mRadius )
							{
								// crossing occurred!
								return PortalIntersectResult.INTERSECT_CROSS;
							}
						}
					}
					// there was no crossing of the portal by the node, but it might be touching
					// the portal.  We check for this by checking the bounding box of the node vs.
					// the sphere of the portal
					if ( mDerivedSphere.Intersects( pczsn.WorldAABB ) && mDerivedPlane.GetSide( pczsn.WorldAABB ) == PlaneSide.Both )
					{
						// intersection but no crossing
						// note this means that the node is CURRENTLY touching the portal.
						if ( mDerivedPlane.GetSide( pczsn.DerivedPosition ) != PlaneSide.Negative )
						{
							// the node is on the positive (front) or exactly on the CP of the portal
							return PortalIntersectResult.INTERSECT_NO_CROSS;
						}
						else
						{
							// the node is on the negative (back) side of the portal - it might be in the wrong zone!
							return PortalIntersectResult.INTERSECT_BACK_NO_CROSS;
						}
					}
					// no intersection CURRENTLY.  (there might have been an intersection
					// during the time between last frame and this frame, but it wasn't a portal
					// crossing, and it isn't touching anymore, so it doesn't matter.
					return PortalIntersectResult.NO_INTERSECT;
				}
				else if ( mType == PORTAL_TYPE.PORTAL_TYPE_AABB )
				{
					// for aabb's we check if the center point went from being inside to being outside
					// the aabb (or vice versa) for crossing.
					var aabb = new AxisAlignedBox( mDerivedCorners[ 0 ], mDerivedCorners[ 1 ] );
					//bool previousInside = aabb.contains(pczsn->getPrevPosition());
					bool currentInside = aabb.Contains( pczsn.DerivedPosition );
					if ( mDirection == Vector3.UnitZ )
					{
						// portal norm is "outward" pointing, look for going from outside to inside
						//if (previousInside == false &&
						if ( currentInside == true )
						{
							return PortalIntersectResult.INTERSECT_CROSS;
						}
					}
					else
					{
						// portal norm is "inward" pointing, look for going from inside to outside
						//if (previousInside == true &&
						if ( currentInside == false )
						{
							return PortalIntersectResult.INTERSECT_CROSS;
						}
					}
					// doesn't cross, but might be touching.  This is a little tricky because we only
					// care if the node aab is NOT fully contained in the portal aabb because we consider
					// the surface of the portal aabb the actual 'portal'.  First, check to see if the
					// aab of the node intersects the aabb portal
					if ( aabb.Intersects( pczsn.WorldAABB ) )
					{
						// now check if the intersection between the two is not the same as the
						// full node aabb, if so, then this means that the node is not fully "contained"
						// which is what we are looking for.
						AxisAlignedBox overlap = aabb.Intersection( pczsn.WorldAABB );
						if ( overlap != pczsn.WorldAABB )
						{
							return PortalIntersectResult.INTERSECT_NO_CROSS;
						}
					}
					return PortalIntersectResult.NO_INTERSECT;
				}
				else
				{
					// for spheres we check if the center point went from being inside to being outside
					// the sphere surface (or vice versa) for crossing.
					//Real previousDistance2 = mPrevDerivedCP.squaredDistance(pczsn->getPrevPosition());
					Real currentDistance2 = mDerivedCP.DistanceSquared( pczsn.DerivedPosition );
					Real mRadius2 = mRadius*mRadius;
					if ( mDirection == Vector3.UnitZ )
					{
						// portal norm is "outward" pointing, look for going from outside to inside
						//if (previousDistance2 >= mRadius2 &&
						if ( currentDistance2 < mRadius2 )
						{
							return PortalIntersectResult.INTERSECT_CROSS;
						}
					}
					else
					{
						// portal norm is "inward" pointing, look for going from inside to outside
						//if (previousDistance2 < mRadius2 &&
						if ( currentDistance2 >= mRadius2 )
						{
							return PortalIntersectResult.INTERSECT_CROSS;
						}
					}
					// no crossing, but might be touching - check distance
					if ( System.Math.Sqrt( System.Math.Abs( mRadius2 - currentDistance2 ) ) <= mRadius )
					{
						return PortalIntersectResult.INTERSECT_NO_CROSS;
					}
					return PortalIntersectResult.NO_INTERSECT;
				}
			}
			return PortalIntersectResult.NO_INTERSECT;
		}

		/* This function check if *this* portal "crossed over" the other portal.
		*/

		public bool crossedPortal( Portal otherPortal )
		{
			// Only check if portal is open
			if ( otherPortal.mOpen )
			{
				// we model both portals as line swept spheres (mPrevDerivedCP to mDerivedCP).
				// intersection test is then between the capsules.
				// BUGBUG! This routine needs to check for case where one or both objects
				//         don't move - resulting in simple sphere tests
				// BUGBUG! If one (or both) portals are aabb's this is REALLY not accurate.
				Capsule portalCapsule, otherPortalCapsule;

				portalCapsule = new Capsule();
				portalCapsule.Set( getPrevDerivedCP(), getDerivedCP(), getRadius() );

				otherPortalCapsule = new Capsule();
				otherPortalCapsule.Set( otherPortal.mPrevDerivedCP, otherPortal.mDerivedCP, otherPortal.mRadius );

				if ( portalCapsule.Intersects( otherPortalCapsule ) )
				{
					// the portal intersected the other portal at some time from last frame to this frame.
					// Now check if this portal "crossed" the other portal
					switch ( otherPortal.Type )
					{
						case PORTAL_TYPE.PORTAL_TYPE_QUAD:
							// a crossing occurs if the "side" of the final position of this portal compared
							// to the final position of the other portal is negative AND the initial position
							// of this portal compared to the initial position of the other portal is non-negative
							// NOTE: This function assumes that this portal is the smaller portal potentially crossing
							//       over the otherPortal which is larger.
							if ( otherPortal.getDerivedPlane().GetSide( mDerivedCP ) == PlaneSide.Negative &&
							     otherPortal.getPrevDerivedPlane().GetSide( mPrevDerivedCP ) != PlaneSide.Negative )
							{
								// crossing occurred!
								return true;
							}
							break;
						case PORTAL_TYPE.PORTAL_TYPE_AABB:
						{
							// for aabb's we check if the center point went from being inside to being outside
							// the aabb (or vice versa) for crossing.
							var aabb = new AxisAlignedBox( otherPortal.getDerivedCorner( 0 ), otherPortal.getDerivedCorner( 1 ) );
							//bool previousInside = aabb.contains(mPrevDerivedCP);
							bool currentInside = aabb.Contains( mDerivedCP );
							if ( otherPortal.getDerivedDirection() == Vector3.UnitZ )
							{
								// portal norm is "outward" pointing, look for going from outside to inside
								//if (previousInside == false &&
								if ( currentInside == true )
								{
									return true;
								}
							}
							else
							{
								// portal norm is "inward" pointing, look for going from inside to outside
								//if (previousInside == true &&
								if ( currentInside == false )
								{
									return true;
								}
							}
						}
							break;
						case PORTAL_TYPE.PORTAL_TYPE_SPHERE:
						{
							// for spheres we check if the center point went from being inside to being outside
							// the sphere surface (or vice versa) for crossing.
							//Real previousDistance2 = mPrevDerivedCP.squaredDistance(otherPortal->getPrevDerivedCP());
							Real currentDistance2 = mDerivedCP.DistanceSquared( otherPortal.getDerivedCP() );
							Real mRadius2 = System.Math.Sqrt( otherPortal.getRadius() );
							if ( otherPortal.getDerivedDirection() == Vector3.UnitZ )
							{
								// portal norm is "outward" pointing, look for going from outside to inside
								//if (previousDistance2 >= mRadius2 &&
								if ( currentDistance2 < mRadius2 )
								{
									return true;
								}
							}
							else
							{
								// portal norm is "inward" pointing, look for going from inside to outside
								//if (previousDistance2 < mRadius2 &&
								if ( currentDistance2 >= mRadius2 )
								{
									return true;
								}
							}
						}
							break;
					}
				}
			}
			// there was no crossing of the portal by this portal. It might be touching
			// the other portal (but we don't care currently)
			return false;
		}

		// check if portal is close to another portal.
		// Note, both portals are assumed to be stationary
		// and DerivedCP is the current position.
		// this function is INTENTIONALLY NOT EXACT because
		// it is used by PCZSM::connectPortalsToTargetZonesByLocation
		// which is a utility function to link up nearby portals
		//
		public bool closeTo( Portal otherPortal )
		{
			// only portals of the same type can be "close to" each other.
			if ( mType != otherPortal.Type )
			{
				return false;
			}
			bool close = false;
			switch ( mType )
			{
				default:
				case PORTAL_TYPE.PORTAL_TYPE_QUAD:
				{
					// quad portals must be within 1/4 sphere of each other
					Sphere quarterSphere1 = mDerivedSphere;
					quarterSphere1.Radius = quarterSphere1.Radius*0.25f;
					Sphere quarterSphere2 = otherPortal.getDerivedSphere();
					quarterSphere2.Radius = quarterSphere2.Radius*0.25f;
					close = quarterSphere1.Intersects( quarterSphere2 );
				}
					break;
				case PORTAL_TYPE.PORTAL_TYPE_AABB:
					// NOTE: AABB's must match perfectly
					if ( mDerivedCP == otherPortal.getDerivedCP() && mCorners[ 0 ] == otherPortal.getCorner( 0 ) &&
					     mCorners[ 1 ] == otherPortal.getCorner( 1 ) )
					{
						close = true;
					}
					break;
				case PORTAL_TYPE.PORTAL_TYPE_SPHERE:
					// NOTE: Spheres must match perfectly
					if ( mDerivedCP == otherPortal.getDerivedCP() && mRadius == otherPortal.getRadius() )
					{
						close = true;
					}
					break;
			}
			return close;
		}
	}
}