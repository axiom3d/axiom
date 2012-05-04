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
using Axiom.Graphics;
using Axiom.Math;

#endregion Namespace Declarations

namespace Axiom.SceneManagers.PortalConnected
{
	public class PCZSceneNode : SceneNode
	{
		private Vector3 newPosition;
		private PCZone homeZone;
		private bool anchored;
		private readonly Dictionary<string, PCZone> visitingZones = new Dictionary<string, PCZone>();
		private Vector3 prevPosition;
		private readonly Dictionary<string, ZoneData> zoneData = new Dictionary<string, ZoneData>();
		private readonly Dictionary<string, MovableObject> objectsByName = new Dictionary<string, MovableObject>();


		public PCZSceneNode( SceneManager creator )
			: base( creator )
		{
			homeZone = null;
			anchored = false;
			AllowToVisit = true;
			LastVisibleFrame = 0;
			LastVisibleFromCamera = null;
			Enabled = true;
		}

		public PCZSceneNode( SceneManager creator, string name )
			: base( creator, name )
		{
			homeZone = null;
			anchored = false;
			AllowToVisit = true;
			LastVisibleFrame = 0;
			LastVisibleFromCamera = null;
			Enabled = true;
		}

		~PCZSceneNode()
		{
			// clear visiting zones list
			visitingZones.Clear();

			// delete zone data
			zoneData.Clear();

			//clear object list
			objectsByName.Clear();
		}

		#region Propertys

		public Vector3 PreviousPosition
		{
			get
			{
				return prevPosition;
			}
		}

		public bool IsAnchored
		{
			get
			{
				return anchored;
			}
			set
			{
				anchored = value;
			}
		}

		public bool AllowToVisit { get; set; }

		public ulong LastVisibleFrame { get; set; }

		public PCZCamera LastVisibleFromCamera { get; set; }

		public bool Enabled { get; set; }

		#endregion Propertys

		#region Methods

		protected override void Update( bool updateChildren, bool parentHasChanged )
		{
			base.Update( updateChildren, parentHasChanged );

			prevPosition = newPosition;
			newPosition = DerivedPosition;
			// do this way since _update is called through SceneManager::_updateSceneGraph which comes before PCZSceneManager::_updatePCZSceneNodes
		}

		//-----------------------------------------------------------------------
		public override SceneNode CreateChildSceneNode( Vector3 translate, Quaternion rotate )
		{
			var childSceneNode = (PCZSceneNode)( CreateChild( translate, rotate ) );
			if ( anchored )
			{
				childSceneNode.AnchorToHomeZone( homeZone );
				homeZone.AddNode( childSceneNode );
			}
			return childSceneNode;
		}

		//-----------------------------------------------------------------------
		public override SceneNode CreateChildSceneNode( string name, Vector3 translate, Quaternion rotate )
		{
			var childSceneNode = (PCZSceneNode)( CreateChild( name, translate, rotate ) );
			if ( anchored )
			{
				childSceneNode.AnchorToHomeZone( homeZone );
				homeZone.AddNode( childSceneNode );
			}
			return childSceneNode;
		}


		public PCZone HomeZone
		{
			get
			{
				return homeZone;
			}
			set
			{
				// if the new home zone is different than the current, remove
				// the node from the current home zone's list of home nodes first
				if ( value != homeZone && homeZone != null )
				{
					homeZone.RemoveNode( this );
				}

				homeZone = value;
			}
		}

		public void AnchorToHomeZone( PCZone zone )
		{
			homeZone = zone;
			anchored = true;
		}

		public void AddZoneToVisitingZonesMap( PCZone zone )
		{
			visitingZones[ zone.Name ] = zone;
		}

		public void ClearVisitingZonesMap()
		{
			visitingZones.Clear();
		}

		/* The following function does the following:
		 * 1) Remove references to the node from zones the node is visiting
		 * 2) Clear the node's list of zones it is visiting
		 */

		public void ClearNodeFromVisitedZones()
		{
			if ( visitingZones.Count > 0 )
			{
				// first go through the list of zones this node is visiting
				// and remove references to this node
				//PCZone zone;
				//ZoneMap::iterator it = mVisitingZones.begin();

				foreach ( PCZone zone in visitingZones.Values )
				{
					zone.RemoveNode( this );
				}

				// second, clear the visiting zones list
				visitingZones.Clear();
			}
		}

		/* Remove all references that the node has to the given zone
		*/

		public void RemoveReferencesToZone( PCZone zone )
		{
			if ( homeZone == zone )
			{
				homeZone = null;
			}

			if ( visitingZones.ContainsKey( zone.Name ) )
			{
				visitingZones.Remove( zone.Name );
			}

			// search the map of visiting zones and remove
			//ZoneMap::iterator i;
			//i = mVisitingZones.find(zone->getName());
			//if (i != mVisitingZones.end())
			//{
			//    mVisitingZones.erase(i);
			//}
		}

		/* returns true if zone is in the node's visiting zones map
		   false otherwise.
		*/

		public bool IsVisitingZone( PCZone zone )
		{
			if ( visitingZones.ContainsKey( zone.Name ) )
			{
				return true;
			}

			return false;

			//ZoneMap::iterator i;
			//i = mVisitingZones.find(zone->getName());
			//if (i != mVisitingZones.end())
			//{
			//    return true;
			//}
			//return false;
		}

		/** Adds the attached objects of this PCZSceneNode into the queue. */

		public void AddToRenderQueue( Camera cam, RenderQueue queue, bool onlyShadowCasters,
		                              VisibleObjectsBoundsInfo visibleBounds )
		{
			foreach ( var pair in objectsByName )
			{
				pair.Value.NotifyCurrentCamera( cam );

				if ( pair.Value.IsVisible && ( !onlyShadowCasters || pair.Value.CastShadows ) )
				{
					pair.Value.UpdateRenderQueue( queue );

					if ( !visibleBounds.aabb.IsNull )
					{
						visibleBounds.Merge( pair.Value.GetWorldBoundingBox( true ), pair.Value.GetWorldBoundingSphere( true ), cam );
					}
				}
			}
		}

		/** Save the node's current position as the previous position
		*/

		public void SavePrevPosition()
		{
			prevPosition = DerivedPosition;
		}

		public void SetZoneData( PCZone zone, ZoneData zoneData )
		{
			// first make sure that the data doesn't already exist
			if ( this.zoneData.ContainsKey( zone.Name ) )
			{
				throw new AxiomException( "A ZoneData associated with zone " + zone.Name +
				                          " already exists. PCZSceneNode::setZoneData" );
			}

			//mZoneData[zone->getName()] = zoneData;
			// is this equivalent? i think so...
			this.zoneData.Add( zone.Name, zoneData );
		}

		// get zone data for this node for given zone
		// NOTE: This routine assumes that the zone data is present!
		public ZoneData GetZoneData( PCZone zone )
		{
			return zoneData[ zone.Name ];
		}

		// update zone-specific data for any zone that the node is touching
		public void UpdateZoneData()
		{
			ZoneData zoneData;
			PCZone zone;

			// make sure home zone data is updated
			zone = homeZone;
			if ( zone.RequiresZoneSpecificNodeData )
			{
				zoneData = GetZoneData( zone );
				zoneData.update();
			}

			// update zone data for any zones visited
			foreach ( var pair in visitingZones )
			{
				zone = pair.Value;

				if ( zone.RequiresZoneSpecificNodeData )
				{
					zoneData = GetZoneData( zone );
					zoneData.update();
				}
			}
		}

		#endregion Methods
	}
}