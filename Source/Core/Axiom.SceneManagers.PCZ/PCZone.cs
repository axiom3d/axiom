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
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Math;
using Axiom.Math.Collections;

#endregion Namespace Declarations

namespace Axiom.SceneManagers.PortalConnected
{
	public class ZoneData
	{
		public PCZone mAssociatedZone;
		public PCZSceneNode mAssociatedNode;

		public ZoneData( PCZSceneNode node, PCZone zone )
		{
			mAssociatedZone = zone;
			mAssociatedNode = node;
		}

		public virtual void update()
		{
		}
	}

	public abstract class PCZone
	{
		// name of the zone (must be unique)
		protected string mName;

		/// Zone type name
		protected string mZoneTypeName;

		// frame counter for visibility
		protected ulong mLastVisibleFrame;
		// last camera which this zone was visible to
		// flag determining whether or not this zone has sky in it.
		protected bool mHasSky;
		//SceneNode which corresponds to the enclosure for this zone
		protected SceneNode mEnclosureNode;
		// list of SceneNodes contained in this particular PCZone
		protected List<SceneNode> mHomeNodeList = new List<SceneNode>();
		// list of SceneNodes visiting this particular PCZone
		protected List<SceneNode> mVisitorNodeList = new List<SceneNode>();
		// flag recording whether any portals in this zone have moved
		protected bool mPortalsUpdated;
		// user defined data pointer - NOT allocated or deallocated by the zone!
		// you must clean it up yourself!
		protected object mUserData;

		public List<Portal> mPortals = new List<Portal>();
		public PCZSceneManager mPCZSM;
		protected PCZone mCurrentHomeZone;

		[FlagsAttribute]
		public enum NODE_LIST_TYPE : short
		{
			HOME_NODE_LIST = 1,
			VISITOR_NODE_LIST = 2
		};

		public PCZone( PCZSceneManager creator, string name )
		{
			mLastVisibleFrame = 0;
			LastVisibleFromCamera = null;
			mName = name;
			mZoneTypeName = "ZoneType_Undefined";
			mEnclosureNode = null;
			mPCZSM = creator;
			HasSky = false;
		}

		~PCZone()
		{
			// clear list of nodes contained within the zone
			ClearNodeLists( NODE_LIST_TYPE.HOME_NODE_LIST | NODE_LIST_TYPE.VISITOR_NODE_LIST );
			// clear portal list (actual deletion of portals takes place in the PCZSM)
			mPortals.Clear();
		}

		#region Virtuals

		public abstract void AddNode( PCZSceneNode n );

		public abstract void RemoveNode( PCZSceneNode n );

		public abstract void SetEnclosureNode( PCZSceneNode n );

		/** Indicates whether or not this zone requires zone-specific data for
		 *  each scene node
		 */
		public abstract bool RequiresZoneSpecificNodeData { get; }

		/** create zone specific data for a node
		*/

		public virtual void CreateNodeZoneData( PCZSceneNode pczsn )
		{
		}

		/* Add a portal to the zone
		*/
		public abstract void AddPortal( Portal portal );

		/* Remove a portal from the zone
		*/
		public abstract void RemovePortal( Portal portal );

		/** (recursive) check the given node against all portals in the zone
		*/
		public abstract void CheckNodeAgainstPortals( PCZSceneNode pczsn, Portal ignorePortal );

		/** (recursive) check the given light against all portals in the zone
		*/

		public abstract void CheckLightAgainstPortals( PCZLight light, ulong frameCount, PCZFrustum portalFrustum,
		                                               Portal ignorePortal );

		/** Update the spatial data for the portals in the zone
		*/
		public abstract void UpdatePortalsSpatially();

		/* Update the zone data for each portal
		*/
		public abstract void UpdatePortalsZoneData();

		/* Update a node's home zone */
		public abstract PCZone UpdateNodeHomeZone( PCZSceneNode pczsn, bool allowBackTouches );

		/** Find and add visible objects to the render queue.
		@remarks
		Starts with objects in the zone and proceeds through visible portals
		This is a recursive call (the main call should be to _findVisibleObjects)
		*/

		public abstract void FindVisibleNodes( PCZCamera camera, ref List<PCZSceneNode> visibleNodeList, RenderQueue queue,
		                                       VisibleObjectsBoundsInfo visibleBounds, bool onlyShadowCasters,
		                                       bool displayNodes, bool showBoundingBoxes );

		/* Functions for finding Nodes that intersect various shapes */

		public abstract void FindNodes( AxisAlignedBox t, ref List<PCZSceneNode> list, List<Portal> visitedPortals,
		                                bool includeVisitors, bool recurseThruPortals, PCZSceneNode exclude );

		public abstract void FindNodes( Sphere t, ref List<PCZSceneNode> nodes, List<Portal> portals, bool includeVisitors,
		                                bool recurseThruPortals, PCZSceneNode exclude );

		public abstract void FindNodes( PlaneBoundedVolume t, ref List<PCZSceneNode> list, List<Portal> visitedPortals,
		                                bool includeVisitors, bool recurseThruPortals, PCZSceneNode exclude );

		public abstract void FindNodes( Ray t, ref List<PCZSceneNode> list, List<Portal> visitedPortals, bool includeVisitors,
		                                bool recurseThruPortals, PCZSceneNode exclude );

		/** Sets the options for the Zone */
		public abstract bool SetOption( string name, object value );
		/** called when the scene manager creates a camera in order to store the first camera created as the primary
			one, for determining error metrics and the 'home' terrain page.
		*/
		public abstract void NotifyCameraCreated( Camera c );
		/* called by PCZSM during setWorldGeometryRenderQueue() */
		public abstract void NotifyWorldGeometryRenderQueue( int qid );
		/* Called when a _renderScene is called in the SceneManager */
		public abstract void NotifyBeginRenderScene();
		/* called by PCZSM during setZoneGeometry() */
		public abstract void SetZoneGeometry( string filename, PCZSceneNode parentNode );


		/* get / set the lastVisibleFrame counter value */

		public virtual ulong LastVisibleFrame
		{
			get
			{
				return mLastVisibleFrame;
			}
			set
			{
				mLastVisibleFrame = value;
			}
		}

		#endregion Virtuals

		public bool PortalsUpdated
		{
			get
			{
				return mPortalsUpdated;
			}
			set
			{
				mPortalsUpdated = value;
			}
		}

		public string Name
		{
			get
			{
				return mName;
			}
		}

		public object UserData
		{
			get
			{
				return mUserData;
			}
			set
			{
				mUserData = value;
			}
		}

		public PCZCamera LastVisibleFromCamera { get; set; }

		public virtual bool HasSky
		{
			get
			{
				return mHasSky;
			}
			set
			{
				mHasSky = value;
			}
		}

		public virtual SceneNode EnclosureNode
		{
			get
			{
				return mEnclosureNode;
			}
			set
			{
				mEnclosureNode = value;
			}
		}


		/** Remove all nodes from the node reference list and clear it
		*/

		public virtual void ClearNodeLists( NODE_LIST_TYPE type )
		{
			if ( ( type & NODE_LIST_TYPE.HOME_NODE_LIST ) == NODE_LIST_TYPE.HOME_NODE_LIST )
			{
				mHomeNodeList.Clear();
			}
			if ( ( type & NODE_LIST_TYPE.VISITOR_NODE_LIST ) == NODE_LIST_TYPE.VISITOR_NODE_LIST )
			{
				mVisitorNodeList.Clear();
			}
		}

		public virtual Portal FindMatchingPortal( Portal portal )
		{
			// look through all the portals in zone2 for a match

			foreach ( Portal portal2 in mPortals )
			{
				//portal2 = pi2;
				//portal2->updateDerivedValues();
				if ( portal2.getTargetZone() == null && portal2.closeTo( portal ) &&
				     portal2.getDerivedDirection().Dot( portal.getDerivedDirection() ) < -0.9 )
				{
					// found a match!
					return portal2;
				}
			}

			return null;
		}


		/* get the aabb of the zone - default implementation
		   uses the enclosure node, but there are other perhaps
		   better ways
		*/

		public virtual void GetAABB( ref AxisAlignedBox aabb )
		{
			// if there is no node, just return a null box
			if ( null == mEnclosureNode )
			{
				aabb = AxisAlignedBox.Null;
			}
			else
			{
				aabb = mEnclosureNode.WorldAABB;
				// since this is the "local" AABB, subtract out any translations
				aabb.Minimum = aabb.Minimum - mEnclosureNode.DerivedPosition;
				aabb.Maximum = aabb.Maximum - mEnclosureNode.DerivedPosition;
			}
		}

		public PCZone CurrentHomeZone
		{
			get
			{
				return mCurrentHomeZone;
			}
		}
	}
}