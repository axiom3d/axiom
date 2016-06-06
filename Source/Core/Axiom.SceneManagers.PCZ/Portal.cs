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
//     <id value="$Id$"/>
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
			this.mType = type;
			this.mName = name;
			this.mTargetZone = null;
			this.mCurrentHomeZone = null;
			this.mNewHomeZone = null;
			this.mTargetPortal = null;
			this.mNode = null;
			this.mRadius = 0.0;
			this.mDirection = Math.Vector3.UnitZ;
			this.mLocalsUpToDate = false;
			this.mDerivedSphere = new Sphere();
			this.mDerivedPlane = new Plane();
			// set prevWorldTransform to a zero'd out matrix
			this.prevWorldTransform = Math.Matrix4.Zero;
			// default to open
			this.mOpen = true;
			switch ( this.mType )
			{
				default:
				case PORTAL_TYPE.PORTAL_TYPE_QUAD:
					this.mCorners = new Vector3[4];
					this.mDerivedCorners = new Vector3[4];
					break;
				case PORTAL_TYPE.PORTAL_TYPE_AABB:
					this.mCorners = new Vector3[2];
					this.mDerivedCorners = new Vector3[2];
					break;
				case PORTAL_TYPE.PORTAL_TYPE_SPHERE:
					this.mCorners = new Vector3[2];
					this.mDerivedCorners = new Vector3[2];
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
				return this.mType;
			}
		}

		public bool IsOpen
		{
			get
			{
				return this.mOpen;
			}
			set
			{
				this.mOpen = value;
			}
		}

		// Set the SceneNode the Portal is associated with
		public void setNode( SceneNode sn )
		{
			this.mNode = sn;
			this.mLocalsUpToDate = false;
		}

		// Set the 1st Zone the Portal connects to
		public void setTargetZone( PCZone z )
		{
			this.mTargetZone = z;
		}

		/* Returns the name of the portal
		*/

		public string getName()
		{
			return this.mName;
		}

		/* Get the scene node (if any) this portal is associated with
		*/

		public SceneNode getNode()
		{
			return this.mNode;
		}

		/** Get the Zone the Portal connects to
		*/

		public PCZone getTargetZone()
		{
			return this.mTargetZone;
		}

		/** Get the Zone the Portal is currently "in"
		*/

		public PCZone getCurrentHomeZone()
		{
			return this.mCurrentHomeZone;
		}

		/** Get the Zone the Portal should be moved to
		*/

		public PCZone getNewHomeZone()
		{
			return this.mNewHomeZone;
		}

		/** Get the connected portal (if any)
		*/

		public Portal getTargetPortal()
		{
			return this.mTargetPortal;
		}

		/// <summary>
		///     Gets/Sets the direction vector of the portal in local space
		/// </summary>
		public Vector3 Direction
		{
			get
			{
				return this.mDirection;
			}
			set
			{
				this.mDirection = value;
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
				if ( null != this.mCurrentHomeZone )
				{
					this.mCurrentHomeZone.PortalsUpdated = true;
				}
				z.PortalsUpdated = true; // inform new zone of portal change
			}
			this.mCurrentHomeZone = z;
		}

		// Set the zone this portal should be moved to
		public void setNewHomeZone( PCZone z )
		{
			this.mNewHomeZone = z;
		}

		// Set the Portal the Portal connects to
		public void setTargetPortal( Portal p )
		{
			this.mTargetPortal = p;
		}

		// Set the local coordinates of one of the portal corners
		public void setCorner( int index, Vector3 pt )
		{
			this.mCorners[ index ] = pt;
			this.mLocalsUpToDate = false;
		}

		/** Set the local coordinates of all of the portal corners
		*/
		// NOTE: there are 4 corners if the portal is a quad type
		//       there are 2 corners if the portal is an AABB type (min corner & max corner)
		//       there are 2 corners if the portal is a sphere type (center and point on sphere)
		public void setCorners( Vector3[] corners )
		{
			switch ( this.mType )
			{
				default:
				case PORTAL_TYPE.PORTAL_TYPE_QUAD:
					this.mCorners[ 0 ] = corners[ 0 ];
					this.mCorners[ 1 ] = corners[ 1 ];
					this.mCorners[ 2 ] = corners[ 2 ];
					this.mCorners[ 3 ] = corners[ 3 ];
					break;
				case PORTAL_TYPE.PORTAL_TYPE_AABB:
					this.mCorners[ 0 ] = corners[ 0 ]; // minimum corner
					this.mCorners[ 1 ] = corners[ 1 ]; // maximum corner (opposite from min corner)
					break;
				case PORTAL_TYPE.PORTAL_TYPE_SPHERE:
					this.mCorners[ 0 ] = corners[ 0 ]; // center point
					this.mCorners[ 1 ] = corners[ 1 ]; // point on sphere surface
					break;
			}
			this.mLocalsUpToDate = false;
		}

		// calculate the local direction of the portal from the corners
		public void calcDirectionAndRadius()
		{
			Vector3 radiusVector;
			Vector3 side1, side2;

			switch ( this.mType )
			{
				default:
				case PORTAL_TYPE.PORTAL_TYPE_QUAD:
					// first calculate local direction
					side1 = this.mCorners[ 1 ] - this.mCorners[ 0 ];
					side2 = this.mCorners[ 2 ] - this.mCorners[ 0 ];
					this.mDirection = side1.Cross( side2 );
					this.mDirection.Normalize();
					// calculate local cp
					this.mLocalCP = Vector3.Zero;
					for ( int i = 0; i < 4; i++ )
					{
						this.mLocalCP += this.mCorners[ i ];
					}
					this.mLocalCP *= 0.25f;
					// then calculate radius
					radiusVector = this.mCorners[ 0 ] - this.mLocalCP;
					this.mRadius = radiusVector.Length;
					break;
				case PORTAL_TYPE.PORTAL_TYPE_AABB:
					// "direction" is is either pointed inward or outward and is set by user, not calculated.
					// calculate local cp
					this.mLocalCP = Vector3.Zero;
					for ( int i = 0; i < 2; i++ )
					{
						this.mLocalCP += this.mCorners[ i ];
					}
					this.mLocalCP *= 0.5f;
					// for radius, use distance from corner to center point
					// this gives the radius of a sphere that encapsulates the aabb
					radiusVector = this.mCorners[ 0 ] - this.mLocalCP;
					this.mRadius = radiusVector.Length;
					break;
				case PORTAL_TYPE.PORTAL_TYPE_SPHERE:
					// "direction" is is either pointed inward or outward and is set by user, not calculated.
					// local CP is same as corner point 0
					this.mLocalCP = this.mCorners[ 0 ];
					// since corner1 is point on sphere, radius is simply corner1 - center point
					radiusVector = this.mCorners[ 1 ] - this.mLocalCP;
					this.mRadius = radiusVector.Length;
					break;
			}
			this.mDerivedSphere.Radius = this.mRadius;
			// locals are now up to date
			this.mLocalsUpToDate = true;
		}

		// Calculate the local bounding sphere of the portal from the corner points
		public Real getRadius()
		{
			if ( !this.mLocalsUpToDate )
			{
				calcDirectionAndRadius();
			}
			return this.mRadius;
		}

		//Get the coordinates of one of the portal corners
		public Vector3 getCorner( int index )
		{
			return this.mCorners[ index ];
		}

		// Get the derived (world) coordinates of a portal corner (assumes they are up-to-date)
		public Vector3 getDerivedCorner( int index )
		{
			return this.mDerivedCorners[ index ];
		}

		// Get the direction of the portal in world coordinates (assumes  it is up-to-date)
		public Vector3 getDerivedDirection()
		{
			return this.mDerivedDirection;
		}

		// Get the position (centerpoint) of the portal in world coordinates (assumes  it is up-to-date)
		public Vector3 getDerivedCP()
		{
			return this.mDerivedCP;
		}

		// Get the sphere (centered on DerivedCP) of the portal in world coordinates (assumes  it is up-to-date)
		public Sphere getDerivedSphere()
		{
			return this.mDerivedSphere;
		}

		// Get the plane of the portal in world coordinates (assumes  it is up-to-date)
		public Plane getDerivedPlane()
		{
			return this.mDerivedPlane;
		}

		// Get the previous position (centerpoint) of the portal in world coordinates (assumes  it is up-to-date)
		public Vector3 getPrevDerivedCP()
		{
			return this.mPrevDerivedCP;
		}

		// Get the previous plane of the portal in world coordinates (assumes  it is up-to-date)
		public Plane getPrevDerivedPlane()
		{
			return this.mPrevDerivedPlane;
		}

		// Update (Calculate) the world spatial values
		public void updateDerivedValues()
		{
			// make sure local values are up to date
			if ( !this.mLocalsUpToDate )
			{
				calcDirectionAndRadius();
			}
			int numCorners = 4;
			if ( this.mType == PORTAL_TYPE.PORTAL_TYPE_AABB )
			{
				numCorners = 2;
			}
			else if ( this.mType == PORTAL_TYPE.PORTAL_TYPE_SPHERE )
			{
				numCorners = 2;
			}

			// calculate derived values
			if ( null != this.mNode )
			{
				if ( this.prevWorldTransform != this.mNode.FullTransform )
				{
					if ( null != this.mCurrentHomeZone )
					{
						// inform home zone that a portal has been updated
						this.mCurrentHomeZone.PortalsUpdated = true;
					}
					// save world transform
					Matrix4 transform = this.mNode.FullTransform;
					Matrix3 rotation;
					// save off the current DerivedCP
					this.mPrevDerivedCP = this.mDerivedCP;
					this.mDerivedCP = transform*this.mLocalCP;
					this.mDerivedSphere.Center = this.mDerivedCP;
					switch ( this.mType )
					{
						case PORTAL_TYPE.PORTAL_TYPE_QUAD:
							for ( int i = 0; i < numCorners; i++ )
							{
								this.mDerivedCorners[ i ] = transform*this.mCorners[ i ];
							}
							rotation = transform.ExtractRotation();
							this.mDerivedDirection = rotation*this.mDirection;
							break;
						case PORTAL_TYPE.PORTAL_TYPE_AABB:
						{
							AxisAlignedBox aabb; // = new AxisAlignedBox(mCorners[0], mCorners[1]);
							//aabb.SetExtents(mCorners[0], mCorners[1]);
							aabb = this.mNode.WorldAABB;
							//aabb.transform(mNode->_getFullTransform());
							this.mDerivedCorners[ 0 ] = aabb.Minimum;
							this.mDerivedCorners[ 1 ] = aabb.Maximum;
							this.mDerivedDirection = this.mDirection;
						}
							break;
						case PORTAL_TYPE.PORTAL_TYPE_SPHERE:
						{
							this.mDerivedCorners[ 0 ] = this.mDerivedCP;
							this.mDerivedCorners[ 1 ] = transform*this.mCorners[ 1 ];
							this.mDerivedDirection = this.mDirection;
						}
							break;
					}
					if ( this.prevWorldTransform != Matrix4.Zero )
					{
						// save previous calc'd plane
						this.mPrevDerivedPlane = this.mDerivedPlane;
						// calc new plane
						this.mDerivedPlane = new Plane( this.mDerivedDirection, this.mDerivedCP );
						// only update prevWorldTransform if did not move
						// we need to add this conditional to ensure that
						// the portal fully updates when it changes position.
						if ( this.mPrevDerivedPlane == this.mDerivedPlane && this.mPrevDerivedCP == this.mDerivedCP )
						{
							this.prevWorldTransform = transform;
						}
						this.mPrevDerivedCP = this.mDerivedCP;
					}
					else
					{
						// calc new plane
						this.mDerivedPlane = new Plane( this.mDerivedDirection, this.mDerivedCP );
						// this is first time, so there is no previous, so prev = current.
						this.mPrevDerivedPlane = this.mDerivedPlane;
						this.mPrevDerivedCP = this.mDerivedCP;
						this.prevWorldTransform = Matrix4.Identity;
						this.prevWorldTransform = transform;
					}
				}
			}
			else // no associated node, so just use the local values as derived values
			{
				if ( this.prevWorldTransform != Matrix4.Zero )
				{
					// save off the current DerivedCP
					this.mPrevDerivedCP = this.mDerivedCP;
					this.mDerivedCP = this.mLocalCP;
					this.mDerivedSphere.Center = this.mDerivedCP;
					for ( int i = 0; i < numCorners; i++ )
					{
						this.mDerivedCorners[ i ] = this.mCorners[ i ];
					}
					this.mDerivedDirection = this.mDirection;
					// save previous calc'd plane
					this.mPrevDerivedPlane = this.mDerivedPlane;
					// calc new plane
					this.mDerivedPlane = new Plane( this.mDerivedDirection, this.mDerivedCP );
				}
				else
				{
					if ( null != this.mCurrentHomeZone )
					{
						// this case should only happen once
						this.mCurrentHomeZone.PortalsUpdated = true;
					}
					// this is the first time the derived CP has been calculated, so there
					// is no "previous" value, so set previous = current.
					this.mDerivedCP = this.mLocalCP;
					this.mPrevDerivedCP = this.mDerivedCP;
					this.mDerivedSphere.Center = this.mDerivedCP;
					for ( int i = 0; i < numCorners; i++ )
					{
						this.mDerivedCorners[ i ] = this.mCorners[ i ];
					}
					this.mDerivedDirection = this.mDirection;
					// calc new plane
					this.mDerivedPlane = new Plane( this.mDerivedDirection, this.mDerivedCP );
					// this is first time, so there is no previous, so prev = current.
					this.mPrevDerivedPlane = this.mDerivedPlane;
					// flag as initialized
					this.prevWorldTransform = Matrix4.Identity;
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
			if ( !this.mLocalsUpToDate )
			{
				calcDirectionAndRadius();
			}
			// move the parent node to the center point
			node.Position = this.mLocalCP;

			// move the corner points to be relative to the node
			int numCorners = 4;
			if ( this.mType == PORTAL_TYPE.PORTAL_TYPE_AABB )
			{
				numCorners = 2;
			}
			else if ( this.mType == PORTAL_TYPE.PORTAL_TYPE_SPHERE )
			{
				numCorners = 2;
			}

			for ( i = 0; i < numCorners; i++ )
			{
				this.mCorners[ i ] -= this.mLocalCP;
			}
			if ( this.mType != PORTAL_TYPE.PORTAL_TYPE_AABB && this.mType != PORTAL_TYPE.PORTAL_TYPE_SPHERE )
			{
				// NOTE: UNIT_Z is the basis for our local direction
				// orient the node to match the direction
				Quaternion q;
				q = Math.Vector3.UnitZ.GetRotationTo( this.mDirection );
				node.Orientation = q;
			}

			// set the node as the portal's associated node
			setNode( node );
		}

		// IsOpen a portal (allows scene traversal and crossing)
		public void open()
		{
			this.mOpen = true;
		}

		// Close a portal (disallows scene traversal and crossing)
		public void close()
		{
			this.mOpen = false;
		}

		// Check if a portal intersects an AABB
		// NOTE: This check is not exact.
		public bool intersects( AxisAlignedBox aab )
		{
			// Only check if portal is open
			if ( this.mOpen )
			{
				switch ( this.mType )
				{
					case PORTAL_TYPE.PORTAL_TYPE_QUAD:
						// since ogre doesn't have built in support for a quad, just check
						// if the box intersects both the sphere of the portal and the plane
						// this can result in false positives, but they will be minimal
						if ( !aab.Intersects( this.mDerivedSphere ) )
						{
							return false;
						}
						if ( aab.Intersects( this.mDerivedPlane ) )
						{
							return true;
						}
						break;
					case PORTAL_TYPE.PORTAL_TYPE_AABB:
					{
						// aab to aab check
						var aabb = new AxisAlignedBox( this.mDerivedCorners[ 0 ], this.mDerivedCorners[ 1 ] );
						return ( aab.Intersects( aabb ) );
					}
					case PORTAL_TYPE.PORTAL_TYPE_SPHERE:
						// aab to sphere check
						return ( aab.Intersects( this.mDerivedSphere ) );
				}
			}
			return false;
		}

		// Check if a portal intersects a sphere
		// NOTE: This check is not exact.
		public bool intersects( Sphere sphere )
		{
			// Only check if portal is open
			if ( this.mOpen )
			{
				switch ( this.mType )
				{
					case PORTAL_TYPE.PORTAL_TYPE_QUAD:
						// since ogre doesn't have built in support for a quad, just check
						// if the sphere intersects both the sphere of the portal and the plane
						// this can result in false positives, but they will be minimal
						if ( !sphere.Intersects( this.mDerivedSphere ) )
						{
							return false;
						}
						if ( sphere.Intersects( this.mDerivedPlane ) )
						{
							return true;
						}
						break;
					case PORTAL_TYPE.PORTAL_TYPE_AABB:
					{
						// aab to aab check
						var aabb = new AxisAlignedBox( this.mDerivedCorners[ 0 ], this.mDerivedCorners[ 1 ] );
						return ( aabb.Intersects( sphere ) );
					}
					case PORTAL_TYPE.PORTAL_TYPE_SPHERE:
						return ( this.mDerivedSphere.Intersects( sphere ) );
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
			if ( this.mOpen )
			{
				switch ( this.mType )
				{
					case PORTAL_TYPE.PORTAL_TYPE_QUAD:
					{
						// first check sphere of the portal
						if ( !pbv.Intersects( this.mDerivedSphere ) )
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
								if ( plane.GetSide( this.mDerivedCorners[ i ] ) != pbv.outside )
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
						var aabb = new AxisAlignedBox( this.mDerivedCorners[ 0 ], this.mDerivedCorners[ 1 ] );
						if ( !pbv.Intersects( aabb ) )
						{
							return false;
						}
					}
						break;
					case PORTAL_TYPE.PORTAL_TYPE_SPHERE:
						if ( !pbv.Intersects( this.mDerivedSphere ) )
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
			if ( this.mOpen )
			{
				if ( this.mType == PORTAL_TYPE.PORTAL_TYPE_QUAD )
				{
					// since ogre doesn't have built in support for a quad, I'm going to first
					// find the intersection point (if any) of the ray and the portal plane.  Then
					// using the intersection point, I take the cross product of each side of the portal
					// (0,1,intersect), (1,2, intersect), (2,3, intersect), and (3,0,intersect).  If
					// all 4 cross products have vectors pointing in the same direction, then the
					// intersection point is within the portal, otherwise it is outside.

					IntersectResult result = ray.Intersects( this.mDerivedPlane );

					if ( result.Hit )
					{
						// the ray intersects the plane, now walk around the edges
						Vector3 isect = ray.GetPoint( result.Distance );
						Vector3 cross, vect1, vect2;
						Vector3 cross2, vect3, vect4;
						vect1 = this.mDerivedCorners[ 1 ] - this.mDerivedCorners[ 0 ];
						vect2 = isect - this.mDerivedCorners[ 0 ];
						cross = vect1.Cross( vect2 );
						vect3 = this.mDerivedCorners[ 2 ] - this.mDerivedCorners[ 1 ];
						vect4 = isect - this.mDerivedCorners[ 1 ];
						cross2 = vect3.Cross( vect4 );
						if ( cross.Dot( cross2 ) < 0 )
						{
							return false;
						}
						vect1 = this.mDerivedCorners[ 3 ] - this.mDerivedCorners[ 2 ];
						vect2 = isect - this.mDerivedCorners[ 2 ];
						cross = vect1.Cross( vect2 );
						if ( cross.Dot( cross2 ) < 0 )
						{
							return false;
						}
						vect1 = this.mDerivedCorners[ 0 ] - this.mDerivedCorners[ 3 ];
						vect2 = isect - this.mDerivedCorners[ 3 ];
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
				else if ( this.mType == PORTAL_TYPE.PORTAL_TYPE_AABB )
				{
					var aabb = new AxisAlignedBox( this.mDerivedCorners[ 0 ], this.mDerivedCorners[ 1 ] );
					IntersectResult result = ray.Intersects( aabb );
					return result.Hit;
				}
				else // sphere
				{
					IntersectResult result = ray.Intersects( this.mDerivedSphere );
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
			if ( this.mOpen )
			{
				if ( pczsn == this.mNode )
				{
					// ignore the scene node if it is the node the portal is associated with
					return PortalIntersectResult.NO_INTERSECT;
				}
				// most complicated case - if the portal is a quad:
				if ( this.mType == PORTAL_TYPE.PORTAL_TYPE_QUAD )
				{
					// the node is modeled as a line segment (prevPostion to currentPosition)
					// intersection test is then between the capsule and the line segment.
					var nodeSegment = new Segment();
					nodeSegment.Set( pczsn.PreviousPosition, pczsn.DerivedPosition );

					// we model the portal as a line swept sphere (mPrevDerivedCP to mDerivedCP).
					var portalCapsule = new Capsule();
					portalCapsule.Set( this.mPrevDerivedCP, this.mDerivedCP, this.mRadius );

					if ( portalCapsule.Intersects( nodeSegment ) )
					{
						// the portal intersected the node at some time from last frame to this frame.
						// Now check if node "crossed" the portal
						// a crossing occurs if the "side" of the final position of the node compared
						// to the final position of the portal is negative AND the initial position
						// of the node compared to the initial position of the portal is non-negative
						if ( this.mDerivedPlane.GetSide( pczsn.DerivedPosition ) == PlaneSide.Negative &&
						     this.mPrevDerivedPlane.GetSide( pczsn.DerivedPosition ) != PlaneSide.Negative )
						{
							// safety check - make sure the node has at least one dimension which is
							// small enough to fit through the portal! (avoid the "elephant fitting
							// through a mouse hole" case)
							Vector3 nodeHalfVector = pczsn.WorldAABB.HalfSize;
							var portalBox = new Vector3( this.mRadius, this.mRadius, this.mRadius );
							portalBox.Floor( nodeHalfVector );
							if ( portalBox.x < this.mRadius )
							{
								// crossing occurred!
								return PortalIntersectResult.INTERSECT_CROSS;
							}
						}
					}
					// there was no crossing of the portal by the node, but it might be touching
					// the portal.  We check for this by checking the bounding box of the node vs.
					// the sphere of the portal
					if ( this.mDerivedSphere.Intersects( pczsn.WorldAABB ) &&
					     this.mDerivedPlane.GetSide( pczsn.WorldAABB ) == PlaneSide.Both )
					{
						// intersection but no crossing
						// note this means that the node is CURRENTLY touching the portal.
						if ( this.mDerivedPlane.GetSide( pczsn.DerivedPosition ) != PlaneSide.Negative )
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
				else if ( this.mType == PORTAL_TYPE.PORTAL_TYPE_AABB )
				{
					// for aabb's we check if the center point went from being inside to being outside
					// the aabb (or vice versa) for crossing.
					var aabb = new AxisAlignedBox( this.mDerivedCorners[ 0 ], this.mDerivedCorners[ 1 ] );
					//bool previousInside = aabb.contains(pczsn->getPrevPosition());
					bool currentInside = aabb.Contains( pczsn.DerivedPosition );
					if ( this.mDirection == Vector3.UnitZ )
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
					Real currentDistance2 = this.mDerivedCP.DistanceSquared( pczsn.DerivedPosition );
					Real mRadius2 = this.mRadius*this.mRadius;
					if ( this.mDirection == Vector3.UnitZ )
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
					if ( System.Math.Sqrt( System.Math.Abs( mRadius2 - currentDistance2 ) ) <= this.mRadius )
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
							if ( otherPortal.getDerivedPlane().GetSide( this.mDerivedCP ) == PlaneSide.Negative &&
							     otherPortal.getPrevDerivedPlane().GetSide( this.mPrevDerivedCP ) != PlaneSide.Negative )
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
							bool currentInside = aabb.Contains( this.mDerivedCP );
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
							Real currentDistance2 = this.mDerivedCP.DistanceSquared( otherPortal.getDerivedCP() );
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
			if ( this.mType != otherPortal.Type )
			{
				return false;
			}
			bool close = false;
			switch ( this.mType )
			{
				default:
				case PORTAL_TYPE.PORTAL_TYPE_QUAD:
				{
					// quad portals must be within 1/4 sphere of each other
					Sphere quarterSphere1 = this.mDerivedSphere;
					quarterSphere1.Radius = quarterSphere1.Radius*0.25f;
					Sphere quarterSphere2 = otherPortal.getDerivedSphere();
					quarterSphere2.Radius = quarterSphere2.Radius*0.25f;
					close = quarterSphere1.Intersects( quarterSphere2 );
				}
					break;
				case PORTAL_TYPE.PORTAL_TYPE_AABB:
					// NOTE: AABB's must match perfectly
					if ( this.mDerivedCP == otherPortal.getDerivedCP() && this.mCorners[ 0 ] == otherPortal.getCorner( 0 ) &&
					     this.mCorners[ 1 ] == otherPortal.getCorner( 1 ) )
					{
						close = true;
					}
					break;
				case PORTAL_TYPE.PORTAL_TYPE_SPHERE:
					// NOTE: Spheres must match perfectly
					if ( this.mDerivedCP == otherPortal.getDerivedCP() && this.mRadius == otherPortal.getRadius() )
					{
						close = true;
					}
					break;
			}
			return close;
		}
	}
}