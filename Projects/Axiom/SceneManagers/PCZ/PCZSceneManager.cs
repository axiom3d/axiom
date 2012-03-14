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
using System.Diagnostics;

using Axiom.Collections;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Math;
using Axiom.Math.Collections;

#endregion Namespace Declarations

namespace Axiom.SceneManagers.PortalConnected
{
	public struct LightInfo
	{
		public Light light; // Just a pointer for comparison, the light might destroyed for some reason
		public Vector3 position; // Sets to zero if directional light
		public Real range; // Sets to zero if directional light
		public int type; // Use int instead of Light::LightTypes to avoid header file dependence

		public static bool operator ==( LightInfo rhs, LightInfo b )
		{
			return b.light == rhs.light && b.type == rhs.type && b.range == rhs.range && b.position == rhs.position;
		}

		public static bool operator !=( LightInfo rhs, LightInfo b )
		{
			return !( b == rhs );
		}

		#region System.Object Implementation

		public override bool Equals( object obj )
		{
			if ( !( obj is LightInfo ) )
			{
				return false;
			}

			var rhs = (LightInfo)obj;
			return this.light == rhs.light && this.type == rhs.type && this.range == rhs.range && this.position == rhs.position;
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		#endregion System.Object Implementation
	};

	public class PCZSceneManager : SceneManager
	{
		// type of default zone to be used

		/// Master list of Portals in the world (includes all portals)
		private readonly List<Portal> portals = new List<Portal>();

		/// The list of all PCZones
		private readonly List<PCZone> zones = new List<PCZone>();

		/// The zone of the active camera (for shadow texture casting use);
		private PCZone activeCameraZone;

		/// The root PCZone;
		private PCZone defaultZone;

		private string defaultZoneFileName;
		private string defaultZoneTypeName;

		/// frame counter used in visibility determination
		private ulong frameCount;

		protected List<LightInfo> mCachedLightInfos = new List<LightInfo>();
		protected List<LightInfo> mTestLightInfos = new List<LightInfo>();
		private bool shadowTextureConfigDirty;

		/// Portals visibility flag
		protected bool showPortals;

		private List<PCZSceneNode> visibleNodes = new List<PCZSceneNode>();
		private PCZoneFactoryManager zoneFactoryManager;

		public PCZSceneManager( string name )
			: base( name )
		{
			this.defaultZone = null;
			this.activeCameraZone = null;
			this.zoneFactoryManager = null;
			this.showPortals = false;
			this.defaultZoneTypeName = "ZoneType_Default";
			this.defaultZoneFileName = "none";
			rootSceneNode = new PCZSceneNode( this, "Root" );
			defaultRootNode = rootSceneNode;
		}

		/* Get the default zone */

		public PCZone DefaultZone
		{
			get
			{
				return this.defaultZone;
			}
		}

		public override string TypeName
		{
			get
			{
				return "PCZSceneManager";
			}
		}

		/// <summary>
		/// Sets the portal visibility flag
		/// </summary>
		public bool ShowPortals
		{
			get
			{
				return this.showPortals;
			}
			set
			{
				this.showPortals = value;
			}
		}

		public override RenderQueueGroupID WorldGeometryRenderQueueId
		{
			get
			{
				return base.WorldGeometryRenderQueueId;
			}
			set
			{
				// notify zones of new value
				foreach ( PCZone pcZone in this.zones )
				{
					pcZone.NotifyWorldGeometryRenderQueue( (int)value );
				}
				// Call base version to set property
				base.WorldGeometryRenderQueueId = value;
			}
		}

		~PCZSceneManager()
		{
			// we don't delete the root scene node here because the
			// base scene manager class does that.

			// delete ALL portals
			this.portals.Clear();

			// delete all the zones
			this.zones.Clear();
			this.defaultZone = null;
		}

		public void Init( string defaultZoneTypeName, string filename )
		{
			// delete ALL portals
			this.portals.Clear();

			// delete all the zones
			this.zones.Clear();

			this.frameCount = 0;

			this.defaultZoneTypeName = defaultZoneTypeName;
			this.defaultZoneFileName = filename;

			// create a new default zone
			this.zoneFactoryManager = PCZoneFactoryManager.Instance;
			this.defaultZone = CreateZoneFromFile( this.defaultZoneTypeName, "Default_Zone", RootSceneNode as PCZSceneNode, this.defaultZoneFileName );
		}

		// Create a portal instance
		public Portal CreatePortal( String name, PORTAL_TYPE type )
		{
			var newPortal = new Portal( name, type );
			this.portals.Add( newPortal );
			return newPortal;
		}

		// delete a portal instance by pointer
		public void DestroyPortal( Portal p )
		{
			// remove the portal from it's target portal
			Portal targetPortal = p.getTargetPortal();
			if ( null != targetPortal )
			{
				targetPortal.setTargetPortal( null ); // the targetPortal will still have targetZone value, but targetPortal will be invalid
			}
			// remove the Portal from it's home zone
			PCZone homeZone = p.getCurrentHomeZone();
			if ( null != homeZone )
			{
				// inform zone of portal change. Do here since PCZone is abstract
				homeZone.PortalsUpdated = true;
				homeZone.RemovePortal( p );
			}

			// remove the portal from the master portal list
			this.portals.Remove( p );
		}

		// delete a portal instance by pointer
		public void DestroyPortal( String portalName )
		{
			// find the portal from the master portal list
			Portal p;
			Portal thePortal = null;
			foreach ( Portal portal in this.portals )
			{
				if ( portal.getName() == portalName )
				{
					thePortal = portal;
					this.portals.Remove( portal );
					break;
				}
			}

			if ( null != thePortal )
			{
				// remove the portal from it's target portal
				Portal targetPortal = thePortal.getTargetPortal();
				if ( null != targetPortal )
				{
					targetPortal.setTargetPortal( null );
				}

				// remove the Portal from it's home zone
				PCZone homeZOne = thePortal.getCurrentHomeZone();
				if ( null != homeZOne )
				{
					// inform zone of portal change
					homeZOne.PortalsUpdated = true;
					homeZOne.RemovePortal( thePortal );
				}
			}
		}

		//    * Create a zone from a file (type of file
		//    * depends on the zone type
		//    * ZoneType_Default uses an Ogre Model (.mesh) file
		//    * ZoneType_Octree uses an Ogre Model (.mesh) file
		//    * ZoneType_Terrain uses a Terrain.CFG file
		public PCZone CreateZoneFromFile( string zoneTypeName, string zoneName, PCZSceneNode parent, string filename )
		{
			PCZone newZone;

			// create a new default zone
			newZone = this.zoneFactoryManager.CreatePCZone( this, zoneTypeName, zoneName );
			// add to the global list of zones
			this.zones.Add( newZone );
			if ( filename != "none" )
			{
				// set the zone geometry
				newZone.SetZoneGeometry( filename, parent );
			}

			return newZone;
		}

		// Get a zone by name
		public PCZone GetZoneByName( string zoneName )
		{
			foreach ( PCZone zone in this.zones )
			{
				if ( zone.Name == zoneName )
				{
					return zone;
				}
			}

			return null;
		}

		public void SetZoneGeometry( string zoneName, PCZSceneNode parent, string filename )
		{
			foreach ( PCZone zone in this.zones )
			{
				if ( zone.Name == zoneName )
				{
					zone.SetZoneGeometry( filename, parent );
					break;
				}
			}
		}

		private SceneNode CreateSceneNodeImpl()
		{
			return new PCZSceneNode( this );
		}

		private SceneNode CreateSceneNodeImpl( string nodeName )
		{
			return new PCZSceneNode( this, nodeName );
		}

		public override SceneNode CreateSceneNode()
		{
			SceneNode on = CreateSceneNodeImpl();
			sceneNodeList.Add( on.Name, on );
			// create any zone-specific data necessary
			CreateZoneSpecificNodeData( (PCZSceneNode)on );
			// return pointer to the node
			return on;
		}

		public override SceneNode CreateSceneNode( string name )
		{
			// Check name not used
			if ( sceneNodeList.ContainsKey( name ) )
			{
				throw new AxiomException( "A scene node with the name " + name + " already exists. PCZSceneManager.CreateSceneNode" );
			}
			SceneNode on = CreateSceneNodeImpl( name );
			sceneNodeList.Add( name, on );
			// create any zone-specific data necessary
			CreateZoneSpecificNodeData( (PCZSceneNode)on );
			// return pointer to the node
			return on;
		}

		// Create a camera for the scene
		public override Camera CreateCamera( string name )
		{
			// Check name not used
			if ( cameraList.ContainsKey( name ) )
			{
				throw new AxiomException( "A camera with the name " + name + " already exists. PCZSceneManager.CreateCamera" );
			}

			Camera c = new PCZCamera( name, this );
			cameraList.Add( name, c );

			// create visible bounds aab map entry
			//TODO: would be very nice to implements shadows processing like ogre does now...
			//mCamVisibleObjectsMap[c] = VisibleObjectsBoundsInfo();

			// tell all the zones about the new camera
			foreach ( PCZone zone in this.zones )
			{
				zone.NotifyCameraCreated( c );
			}

			return c;
		}

		// Destroy a Scene Node by name.
		public override void DestroySceneNode( string name )
		{
			SceneNode on = GetSceneNode( name );

			if ( null != on )
			{
				// remove references to the node from zones
				RemoveSceneNode( on );
			}

			// destroy the node
			base.DestroySceneNode( on );
		}

		//-----------------------------------------------------------------------
		public override void ClearScene()
		{
			DestroyAllStaticGeometry();

			// Clear root node of all children
			RootSceneNode.RemoveAllChildren();
			RootSceneNode.DetachAllObjects();

			sceneNodeList.Clear();
			autoTrackingSceneNodes.Clear();

			// delete all the zones
			this.zones.Clear();
			this.defaultZone = null;

			// Clear animations
			DestroyAllAnimations();

			// Remove sky nodes since they've been deleted
			skyBoxNode = skyPlaneNode = skyDomeNode = null;
			isSkyBoxEnabled = isSkyPlaneEnabled = isSkyDomeEnabled = false;

			// Clear render queue, empty completely
			if ( null != renderQueue )
			{
				renderQueue.Clear();
			}

			// re-initialize
			Init( this.defaultZoneTypeName, this.defaultZoneFileName );
		}

		// Overridden from SceneManager
		public void SetWorldGeometryRenderQueue( int qid )
		{
			//TODO: Check use of this method
			//// tell all the zones about the new WorldGeometryRenderQueue
			//ZoneMap::iterator i;
			//PCZone * zone;
			//for (i = mZones.begin(); i != mZones.end(); i++)
			//{
			//    zone = i->second;
			//    zone->notifyWorldGeometryRenderQueue( qid );
			//}

			//// call the regular scene manager version
			//SceneManager::setWorldGeometryRenderQueue(qid);
		}

		// Overridden from SceneManager
		public new void RenderScene( Camera cam, Viewport vp, bool includeOverlays )
		{
			// notify all the zones that a scene render is starting
			foreach ( PCZone zone in this.zones )
			{
				zone.NotifyBeginRenderScene();
			}

			// do the regular _renderScene
			base.RenderScene( cam, vp, includeOverlays );
		}

		// Set the zone which contains the sky node
		public void SetSkyZone( PCZone zone )
		{
			if ( null == zone )
			{
				// if no zone specified, use default zone
				zone = this.defaultZone;
			}
			if ( null != skyBoxNode )
			{
				( (PCZSceneNode)skyBoxNode ).HomeZone = zone;
				( (PCZSceneNode)skyBoxNode ).AnchorToHomeZone( zone );
				zone.HasSky = true;
			}
			if ( null != skyDomeNode )
			{
				( (PCZSceneNode)skyDomeNode ).HomeZone = zone;
				( (PCZSceneNode)skyDomeNode ).AnchorToHomeZone( zone );
				zone.HasSky = true;
			}
			if ( null != skyPlaneNode )
			{
				( (PCZSceneNode)skyPlaneNode ).HomeZone = zone;
				( (PCZSceneNode)skyPlaneNode ).AnchorToHomeZone( zone );
				zone.HasSky = true;
			}
		}

		//-----------------------------------------------------------------------
		// THIS IS THE MAIN LOOP OF THE MANAGER
		//-----------------------------------------------------------------------
		// _updateSceneGraph does several things now:
		// 1) standard scene graph update (transform all nodes in the node tree)
		// 2) update the spatial data for all zones (& portals in the zones)
		// 3) Update the PCZSNMap entry for every scene node
		protected override void UpdateSceneGraph( Camera cam )
		{
			// First do the standard scene graph update
			base.UpdateSceneGraph( cam );
			// Then do the portal update.  This is done after all the regular
			// scene graph node updates because portals can move (being attached to scene nodes)
			// (also clear node refs in every zone)
			UpdatePortalSpatialData();
			// check for portal zone-related changes (portals intersecting other portals)
			UpdatePortalZoneData();
			// update all scene nodes
			UpdatePCZSceneNodes();
			// calculate zones affected by each light
			CalcZonesAffectedByLights( cam );
			// save node positions
			//_saveNodePositions();
			// clear update flags at end so user triggered updated are
			// not cleared prematurely
			ClearAllZonesPortalUpdateFlag();
		}

		//* Save the position of all nodes (saved to PCZSN->prevPosition)
		//* NOTE: Yeah, this is inefficient because it's doing EVERY node in the
		//*       scene.  A more efficient way would be override all scene node
		//*	    functions that change position/orientation and save old position
		//*	    & orientation when those functions are called, but that's more
		//*       coding work than I willing to do right now...
		//*
		public void SaveNodePositions()
		{
			foreach ( PCZSceneNode pczsn in sceneNodeList.Values )
			{
				pczsn.SavePrevPosition();
			}
		}

		// Update the spatial data for every zone portal in the scene

		public void UpdatePortalSpatialData()
		{
			foreach ( PCZone zone in this.zones )
			{
				// this call updates Portal spatials
				zone.UpdatePortalsSpatially();
				// clear the visitor node list in the zone while we're here
				zone.ClearNodeLists( PCZone.NODE_LIST_TYPE.VISITOR_NODE_LIST );
			}
		}

		// Update the zone data for every zone portal in the scene

		public void UpdatePortalZoneData()
		{
			foreach ( PCZone zone in this.zones )
			{
				// this callchecks for portal zone changes & applies zone data changes as necessary
				zone.UpdatePortalsZoneData();
			}
		}

		// Update all PCZSceneNodes.
		public void UpdatePCZSceneNodes()
		{
			foreach ( PCZSceneNode pczsn in sceneNodeList.Values )
			{
				if ( pczsn.Enabled )
				{
					// Update a single entry
					UpdatePCZSceneNode( pczsn );
				}
			}
		}

		public void CalcZonesAffectedByLights( Camera cam )
		{
			MovableObjectCollection lightList = GetMovableObjectCollection( LightFactory.TypeName );
			//HACK: i dont know if this is exactly the same...
			lock ( lightList )
			{
				foreach ( PCZLight l in lightList.Values )
				{
					if ( l.NeedsUpdate )
					{
						l.UpdateZones( ( (PCZSceneNode)( cam.ParentSceneNode ) ).HomeZone, this.frameCount );
					}
					l.NeedsUpdate = false;
				}
			}
		}

		//-----------------------------------------------------------------------
		// Update all the zone info for a given node.  This function
		// makes sure the home zone of the node is correct, and references
		// to any zones it is visiting are added and a reference to the
		// node is added to the visitor lists of any zone it is visiting.
		//
		public void UpdatePCZSceneNode( PCZSceneNode pczsn )
		{
			// Skip if root Zone has been destroyed (shutdown conditions)
			if ( null == this.defaultZone )
			{
				return;
			}

			// Skip if the node is the sceneroot node
			if ( pczsn == RootSceneNode )
			{
				return;
			}

			// clear all references to visiting zones
			pczsn.ClearVisitingZonesMap();

			// Find the current home zone of the node associated with the pczsn entry.
			UpdateHomeZone( pczsn, false );

			//* The following function does the following:
			//* 1) Check all portals in the home zone - if the node is touching the portal
			//*    then add the node to the connected zone as a visitor
			//* 2) Recurse into visited zones in case the node spans several zones
			//*
			// (recursively) check each portal of home zone to see if the node is touching
			if ( null != pczsn.HomeZone && pczsn.AllowToVisit )
			{
				pczsn.HomeZone.CheckNodeAgainstPortals( pczsn, null );
			}

			// update zone-specific data for the node for any zones that require it
			pczsn.UpdateZoneData();
		}

		// Removes all references to the node from every zone in the scene.
		public void RemoveSceneNode( SceneNode sn )
		{
			// Skip if mDefaultZone has been destroyed (shutdown conditions)
			if ( null == this.defaultZone )
			{
				return;
			}

			var pczsn = (PCZSceneNode)sn;

			// clear all references to the node in visited zones
			pczsn.ClearNodeFromVisitedZones();

			// tell the node it's not in a zone
			pczsn.HomeZone = null;
		}

		// Set the home zone for a node
		public void AddPCZSceneNode( PCZSceneNode sn, PCZone homeZone )
		{
			// set the home zone
			sn.HomeZone = homeZone;
			// add the node
			homeZone.AddNode( sn );
		}

		//-----------------------------------------------------------------------
		// Create a zone with the given name and parent zone
		public PCZone CreateZone( string zoneType, string instanceName )
		{
			foreach ( PCZone zone in this.zones )
			{
				if ( zone.Name == instanceName )
				{
					throw new AxiomException( "A zone with the name " + instanceName + " already exists. PCZSceneManager.createZone" );
				}
			}

			PCZone newZone = this.zoneFactoryManager.CreatePCZone( this, zoneType, instanceName );
			if ( null != newZone )
			{
				// add to the global list of zones
				this.zones.Add( newZone );
				if ( newZone.RequiresZoneSpecificNodeData )
				{
					CreateZoneSpecificNodeData( newZone );
				}
			}

			return newZone;
		}

		// destroy an existing zone within the scene
		//if destroySceneNodes is true, then all nodes which have the destroyed
		//zone as their homezone are desroyed too.  If destroySceneNodes is false
		//then all scene nodes which have the zone as their homezone will have
		//their homezone pointer set to 0, which will allow them to be re-assigned
		//either by the user or via the automatic re-assignment routine
		public void DestroyZone( PCZone zone, bool destroySceneNodes )
		{
			// need to remove this zone from all lights affected zones list,
			// otherwise next frame _calcZonesAffectedByLights will call PCZLight::getNeedsUpdate()
			// which will try to access the zone pointer and will cause an access violation
			//HACK: again...
			MovableObjectCollection lightList = GetMovableObjectCollection( LightFactory.TypeName );
			lock ( lightList )
			{
				foreach ( PCZLight l in lightList.Values )
				{
					if ( l.NeedsUpdate )
					{
						// no need to check, this function does that anyway. if exists, is erased.
						l.RemoveZoneFromAffectedZonesList( zone );
					}
				}
			}

			// if not destroying scene nodes, then make sure any nodes who have
			foreach ( PCZSceneNode pczsn in sceneNodeList.Values )
			{
				if ( !destroySceneNodes )
				{
					if ( pczsn.HomeZone == zone )
					{
						pczsn.HomeZone = null;
					}
				}
				// reset all node visitor lists
				// note, it might be more efficient to only do this to nodes which
				// are actually visiting the zone being destroyed, but visitor lists
				// get cleared every frame anyway, so it's not THAT big a deal.
				pczsn.ClearNodeFromVisitedZones();
			}

			this.zones.Remove( zone );
		}

		//* The following function checks if a node has left it's current home zone.
		//* This is done by checking each portal in the zone.  If the node has crossed
		//* the portal, then the current zone is no longer the home zone of the node.  The
		//* function then recurses into the connected zones.  Once a zone is found where
		//* the node does NOT cross out through a portal, that zone is the new home zone.
		//* When this function is done, the node should have the correct home zone already
		//* set.  A pointer is returned to this zone as well.
		//*
		//* NOTE: If the node does not have a home zone when this function is called on it,
		//*       the function will do its best to find the proper zone for the node using
		//*       bounding box volume testing.  This CAN fail to find the correct zone in
		//*		some scenarios, so it is best for the user to EXPLICITLY set the home
		//*		zone of the node when the node is added to the scene using
		//*       PCZSceneNode::setHomeZone()
		//*
		public void UpdateHomeZone( PCZSceneNode pczsn, bool allowBackTouches )
		{
			// Skip if root PCZoneTree has been destroyed (shutdown conditions)
			if ( null == this.defaultZone )
			{
				return;
			}

			PCZone startzone;
			PCZone newHomeZone;

			// start with current home zone of the node
			startzone = pczsn.HomeZone;

			if ( null != startzone )
			{
				if ( !pczsn.IsAnchored )
				{
					newHomeZone = startzone.UpdateNodeHomeZone( pczsn, false );
				}
				else
				{
					newHomeZone = startzone;
				}

				if ( newHomeZone != startzone )
				{
					// add the node to the home zone
					newHomeZone.AddNode( pczsn );
				}
			}
			else
			{
				// the node hasn't had it's home zone set yet, so do our best to
				// find the home zone using volume testing.
				Vector3 nodeCenter = pczsn.DerivedPosition;
				PCZone bestZone = FindZoneForPoint( nodeCenter );
				// set the best zone as the node's home zone
				pczsn.HomeZone = bestZone;
				// add the node to the zone
				bestZone.AddNode( pczsn );
			}

			return;
		}

		// Find the best (smallest) zone that contains a point
		public PCZone FindZoneForPoint( Vector3 point )
		{
			PCZone bestZone = this.defaultZone;
			Real bestVolume = Real.PositiveInfinity;

			foreach ( PCZone zone in this.zones )
			{
				var aabb = new AxisAlignedBox();
				zone.GetAABB( ref aabb );
				SceneNode enclosureNode = zone.EnclosureNode;
				if ( null != enclosureNode )
				{
					// since this is the "local" AABB, add in world translation of the enclosure node
					aabb.Minimum = aabb.Minimum + enclosureNode.DerivedPosition;
					aabb.Maximum = aabb.Maximum + enclosureNode.DerivedPosition;
				}
				if ( aabb.Contains( point ) )
				{
					if ( aabb.Volume < bestVolume )
					{
						// this zone is "smaller" than the current best zone, so make it
						// the new best zone
						bestZone = zone;
						bestVolume = aabb.Volume;
					}
				}
			}

			return bestZone;
		}

		// create any zone-specific data necessary for all zones for the given node
		public void CreateZoneSpecificNodeData( PCZSceneNode node )
		{
			foreach ( PCZone zone in this.zones )
			{
				if ( zone.RequiresZoneSpecificNodeData )
				{
					zone.CreateNodeZoneData( node );
				}
			}
		}

		// create any zone-specific data necessary for all nodes for the given zone
		public void CreateZoneSpecificNodeData( PCZone zone )
		{
			if ( zone.RequiresZoneSpecificNodeData )
			{
				foreach ( PCZSceneNode node in sceneNodeList.Values )
				{
					zone.CreateNodeZoneData( node );
				}
			}
		}

		// set the home zone for a scene node
		public void SetNodeHomeZone( SceneNode node, PCZone zone )
		{
			// cast the SceneNode to a PCZSceneNode
			var pczsn = (PCZSceneNode)node;
			pczsn.HomeZone = zone;
		}

		// (optional) post processing for any scene node found visible for the frame
		public void AlertVisibleObjects()
		{
			throw new NotImplementedException( "Not implemented" );

			foreach ( PCZSceneNode node in this.visibleNodes )
			{
				// this is where you would do whatever you wanted to the visible node
				// but right now, it does nothing.
			}
		}

		//-----------------------------------------------------------------------
		public override Light CreateLight( string name )
		{
			return (Light)CreateMovableObject( name, PCZLightFactory.TypeName, null );
		}

		//-----------------------------------------------------------------------
		public override Light GetLight( string name )
		{
			return (Light)( GetMovableObject( name, PCZLightFactory.TypeName ) );
		}

		//-----------------------------------------------------------------------
		public bool HasLight( string name )
		{
			return HasMovableObject( name, PCZLightFactory.TypeName );
		}

		public bool HasSceneNode( string name )
		{
			return sceneNodeList.ContainsKey( name );
		}

		//-----------------------------------------------------------------------
		public void DestroyLight( string name )
		{
			DestroyMovableObject( name, PCZLightFactory.TypeName );
		}

		//-----------------------------------------------------------------------
		public void DestroyAllLights()
		{
			DestroyAllMovableObjectsByType( PCZLightFactory.TypeName );
		}

		//---------------------------------------------------------------------
		protected override void FindLightsAffectingFrustum( Camera camera )
		{
			base.FindLightsAffectingFrustum( camera );
			return;
			// Similar to the basic SceneManager, we iterate through
			// lights to see which ones affect the frustum.  However,
			// since we have camera & lights partitioned by zones,
			// we can check only those lights which either affect the
			// zone the camera is in, or affect zones which are visible to
			// the camera

			MovableObjectCollection lights = GetMovableObjectCollection( PCZLightFactory.TypeName );

			lock ( lights )
			{
				foreach ( PCZLight l in lights.Values )
				{
					if ( l.IsVisible /* && l.AffectsVisibleZone */ )
					{
						LightInfo lightInfo;
						lightInfo.light = l;
						lightInfo.type = (int)l.Type;
						if ( lightInfo.type == (int)LightType.Directional )
						{
							// Always visible
							lightInfo.position = Vector3.Zero;
							lightInfo.range = 0;
							this.mTestLightInfos.Add( lightInfo );
						}
						else
						{
							// NB treating spotlight as point for simplicity
							// Just see if the lights attenuation range is within the frustum
							lightInfo.range = l.AttenuationRange;
							lightInfo.position = l.GetDerivedPosition();
							var sphere = new Sphere( lightInfo.position, lightInfo.range );
							if ( camera.IsObjectVisible( sphere ) )
							{
								this.mTestLightInfos.Add( lightInfo );
							}
						}
					}
				}
			} // release lock on lights collection

			base.FindLightsAffectingFrustum( camera );

			// from here on down this function is same as Ogre::SceneManager

			// Update lights affecting frustum if changed
			if ( this.mCachedLightInfos != this.mTestLightInfos )
			{
				//mLightsAffectingFrustum.resize(mTestLightInfos.size());
				//LightInfoList::const_iterator i;
				//LightList::iterator j = mLightsAffectingFrustum.begin();
				//for (i = mTestLightInfos.begin(); i != mTestLightInfos.end(); ++i, ++j)
				//{
				//    *j = i->light;
				//    // add cam distance for sorting if texture shadows
				//    if (isShadowTechniqueTextureBased())
				//    {
				//        (*j)->tempSquareDist =
				//            (camera->getDerivedPosition() - (*j)->getDerivedPosition()).squaredLength();
				//    }
				//}

				foreach ( LightInfo i in this.mTestLightInfos )
				{
					if ( IsShadowTechniqueTextureBased )
					{
						i.light.TempSquaredDist = ( camera.DerivedPosition - i.light.GetDerivedPosition() ).LengthSquared;
					}
				}

				if ( IsShadowTechniqueTextureBased ) { }

				// Sort the lights if using texture shadows, since the first 'n' will be
				// used to generate shadow textures and we should pick the most appropriate
				//if (IsShadowTechniqueTextureBased)
				//{
				//    // Allow a ShadowListener to override light sorting
				//    // Reverse iterate so last takes precedence
				//    bool overridden = false;
				//    foreach(object o in base.)
				//    for (ListenerList::reverse_iterator ri = mListeners.rbegin();
				//        ri != mListeners.rend(); ++ri)
				//    {
				//        overridden = (*ri)->sortLightsAffectingFrustum(mLightsAffectingFrustum);
				//        if (overridden)
				//            break;
				//    }
				//    if (!overridden)
				//    {
				//        // default sort (stable to preserve directional light ordering
				//        std::stable_sort(
				//            mLightsAffectingFrustum.begin(), mLightsAffectingFrustum.end(),
				//            lightsForShadowTextureLess());
				//    }

				//}

				// Use swap instead of copy operator for efficiently
				//mCachedLightInfos.swap(mTestLightInfos);
				this.mCachedLightInfos = this.mTestLightInfos;

				// notify light dirty, so all movable objects will re-populate
				// their light list next time
				//_notifyLightsDirty();
				//Check: do we have something like this here?
			}
		}

		//---------------------------------------------------------------------
		protected override void EnsureShadowTexturesCreated()
		{
			bool createSceneNode = this.shadowTextureConfigDirty;

			//base.ensureShadowTexturesCreated();

			if ( !createSceneNode )
			{
				return;
			}

			int count = shadowTextureCameras.Count;
			for ( int i = 0; i < count; ++i )
			{
				var node = (PCZSceneNode)rootSceneNode.CreateChildSceneNode( shadowTextureCameras[ i ].Name );
				node.AttachObject( shadowTextureCameras[ i ] );
				AddPCZSceneNode( node, this.defaultZone );
			}
		}

		//---------------------------------------------------------------------
		public new void DestroyShadowTextures()
		{
			int count = shadowTextureCameras.Count;
			for ( int i = 0; i < count; ++i )
			{
				SceneNode node = shadowTextureCameras[ i ].ParentSceneNode;
				rootSceneNode.RemoveAndDestroyChild( node.Name );
			}
			base.DestroyShadowTextures();
		}

		//---------------------------------------------------------------------
		protected override void PrepareShadowTextures( Camera cam, Viewport vp )
		{
			if ( ( cam.ParentSceneNode ) != null )
			{
				this.activeCameraZone = ( (PCZSceneNode)cam.ParentSceneNode ).HomeZone;
			}
			base.PrepareShadowTextures( cam, vp );
		}

		//---------------------------------------------------------------------
		public void FireShadowTexturesPreCaster( Light light, Camera camera, int iteration )
		{
			var camNode = (PCZSceneNode)camera.ParentSceneNode;

			if ( light.Type == LightType.Directional )
			{
				if ( camNode.HomeZone != this.activeCameraZone )
				{
					AddPCZSceneNode( camNode, this.activeCameraZone );
				}
			}
			else
			{
				var lightNode = (PCZSceneNode)light.ParentSceneNode;
				Debug.Assert( null != lightNode, "Error, lightNode shoudn't be null" );
				PCZone lightZone = lightNode.HomeZone;
				if ( camNode.HomeZone != lightZone )
				{
					AddPCZSceneNode( camNode, lightZone );
				}
			}

			//Check: Implementation...
			//base.fireShadowTexturesPreCaster(light, camera, iteration);
		}

		// Attempt to automatically connect unconnected portals to proper target zones
		//	 by looking for matching portals in other zones which are at the same location
		public void ConnectPortalsToTargetZonesByLocation()
		{
			// go through every zone to find portals
			bool foundMatch;
			foreach ( PCZone zone in this.zones )
			{
				// go through all the portals in the zone
				foreach ( Portal portal in zone.mPortals )
				{
					//portal->updateDerivedValues();
					if ( null == portal.getTargetZone() )
					{
						// this is a portal without a connected zone - look for
						// a matching portal in another zone
						PCZone zone2;
						foundMatch = false;
						int j = 0;
						while ( !foundMatch && j != this.zones.Count )
						{
							zone2 = this.zones[ j++ ];
							if ( zone2 != zone ) // make sure we don't look in the same zone
							{
								Portal portal2 = zone2.FindMatchingPortal( portal );
								if ( null != portal2 )
								{
									// found a match!
									LogManager.Instance.Write( "Connecting portal " + portal.getName() + " to portal " + portal2.getName() );
									foundMatch = true;
									portal.setTargetZone( zone2 );
									portal.setTargetPortal( portal2 );
									portal2.setTargetZone( zone );
									portal2.setTargetPortal( portal );
								}
							}
						}
						if ( foundMatch == false )
						{
							// error, didn't find a matching portal!
							throw new AxiomException( "Could not find matching portal for portal " + portal.getName() + "PCZSceneManager.connectPortalsToTargetZonesByLocation" );
						}
					}
				}
			}
		}

		// main visibility determination & render queue filling routine
		// over-ridden from base/default scene manager.  This is *the*
		// main call.
		public void FindVisibleObjects( Camera cam, VisibleObjectsBoundsInfo visibleBounds, bool onlyShadowCasters )
		{
			// clear the render queue
			renderQueue.Clear();
			// clear the list of visible nodes
			this.visibleNodes.Clear();

			// turn off sky
			EnableSky( false );

			// remove all extra culling planes
			( (PCZCamera)cam ).RemoveAllExtraCullingPlanes();

			// increment the visibility frame counter
			//mFrameCount++;
			this.frameCount = Root.Instance.CurrentFrameCount;

			// update the camera
			( (PCZCamera)cam ).Update();

			// get the home zone of the camera
			PCZone cameraHomeZone = ( (PCZSceneNode)( cam.ParentSceneNode ) ).HomeZone;

			// walk the zones, starting from the camera home zone,
			// adding all visible scene nodes to the mVisibles list
			cameraHomeZone.LastVisibleFrame = this.frameCount;
			cameraHomeZone.FindVisibleNodes( (PCZCamera)cam, ref this.visibleNodes, renderQueue, visibleBounds, onlyShadowCasters, displayNodes, showBoundingBoxes );
		}

		public void FindNodesIn( AxisAlignedBox box, ref List<PCZSceneNode> list, PCZone startZone, PCZSceneNode exclude )
		{
			var visitedPortals = new List<Portal>();
			if ( null != startZone )
			{
				// start in startzone, and recurse through portals if necessary
				startZone.FindNodes( box, ref list, visitedPortals, true, true, exclude );
			}
			else
			{
				// no start zone specified, so check all zones
				foreach ( PCZone zone in this.zones )
				{
					zone.FindNodes( box, ref list, visitedPortals, false, false, exclude );
				}
			}
		}

		public void FindNodesIn( Sphere sphere, ref List<PCZSceneNode> list, PCZone startZone, PCZSceneNode exclude )
		{
			var visitedPortals = new List<Portal>();
			if ( null != startZone )
			{
				// start in startzone, and recurse through portals if necessary
				startZone.FindNodes( sphere, ref list, visitedPortals, true, true, exclude );
			}
			else
			{
				// no start zone specified, so check all zones
				foreach ( PCZone zone in this.zones )
				{
					zone.FindNodes( sphere, ref list, visitedPortals, false, false, exclude );
				}
			}
		}

		public void FindNodesIn( PlaneBoundedVolume volumes, ref List<PCZSceneNode> list, PCZone startZone, PCZSceneNode exclude )
		{
			var visitedPortals = new List<Portal>();
			if ( null != startZone )
			{
				// start in startzone, and recurse through portals if necessary
				startZone.FindNodes( volumes, ref list, visitedPortals, true, true, exclude );
			}
			else
			{
				// no start zone specified, so check all zones
				foreach ( PCZone zone in this.zones )
				{
					zone.FindNodes( volumes, ref list, visitedPortals, false, false, exclude );
				}
			}
		}

		public void FindNodesIn( Ray r, ref List<PCZSceneNode> list, PCZone startZone, PCZSceneNode exclude )
		{
			var visitedPortals = new List<Portal>();
			if ( null != startZone )
			{
				// start in startzone, and recurse through portals if necessary
				startZone.FindNodes( r, ref list, visitedPortals, true, true, exclude );
			}
			else
			{
				foreach ( PCZone zone in this.zones )
				{
					zone.FindNodes( r, ref list, visitedPortals, false, false, exclude );
				}
			}
		}

		// get the current value of a scene manager option
		public bool GetOptionValues( string key, ref List<string> refValueList )
		{
			//return base.Options[key] SceneManager::GetOptionValues( key, refValueList );
			return false;
		}

		// get option keys (base along with PCZ-specific)
		public bool GetOptionKeys( ref List<string> refKeys )
		{
			foreach ( string s in optionList.Keys )
			{
				refKeys.Add( s );
			}
			refKeys.Add( "ShowBoundingBoxes" );
			refKeys.Add( "ShowPortals" );

			return true;
		}

		public bool SetOption( string key, object val )
		{
			if ( key == "ShowBoundingBoxes" )
			{
				showBoundingBoxes = Convert.ToBoolean( val );
				return true;
			}

			else if ( key == "ShowPortals" )
			{
				this.showPortals = Convert.ToBoolean( val );
				return true;
			}
			// send option to each zone
			foreach ( PCZone zone in this.zones )
			{
				if ( zone.SetOption( key, val ) )
				{
					return true;
				}
			}

			// try regular scenemanager option
			if ( Options.ContainsKey( key ) )
			{
				Options[ key ] = val;
			}
			else
			{
				Options.Add( key, val );
			}

			return true;
		}

		public bool GetOption( string key, ref object val )
		{
			if ( key == "ShowBoundingBoxes" )
			{
				val = showBoundingBoxes;
				return true;
			}
			if ( key == "ShowPortals" )
			{
				val = this.showPortals;
				return true;
			}

			if ( Options.ContainsKey( key ) )
			{
				val = Options[ key ];

				return true;
			}

			return false;
		}

		//---------------------------------------------------------------------
		public override AxisAlignedBoxRegionSceneQuery CreateAABBRegionQuery( AxisAlignedBox box, uint mask )
		{
			var q = new PCZAxisAlignedBoxSceneQuery( this );
			q.Box = box;
			q.QueryMask = mask;
			return q;
		}

		//---------------------------------------------------------------------
		public override SphereRegionSceneQuery CreateSphereRegionQuery( Sphere sphere, uint mask )
		{
			var q = new PCZSphereSceneQuery( this );
			q.Sphere = sphere;
			q.QueryMask = mask;
			return q;
		}

		//---------------------------------------------------------------------
		public override PlaneBoundedVolumeListSceneQuery CreatePlaneBoundedVolumeQuery( PlaneBoundedVolumeList volumes, uint mask )
		{
			var q = new PCZPlaneBoundedVolumeListSceneQuery( this );
			q.Volumes = volumes;
			q.QueryMask = mask;
			return q;
		}

		//---------------------------------------------------------------------
		public override RaySceneQuery CreateRayQuery( Ray ray, uint mask )
		{
			var q = new PCZRaySceneQuery( this );
			q.Ray = ray;
			q.QueryMask = mask;
			return q;
		}

		//---------------------------------------------------------------------
		public override IntersectionSceneQuery CreateIntersectionQuery( uint mask )
		{
			var q = new PCZIntersectionSceneQuery( this );
			q.QueryMask = mask;
			return q;
		}

		//---------------------------------------------------------------------
		// clear portal update flag from all zones
		public void ClearAllZonesPortalUpdateFlag()
		{
			foreach ( PCZone zone in this.zones )
			{
				zone.PortalsUpdated = true;
			}
		}

		/// <summary>
		/// enable/disable sky rendering
		/// </summary>
		/// <param name="onoff"></param>
		public void EnableSky( bool onoff )
		{
			if ( null != skyBoxNode )
			{
				isSkyBoxEnabled = onoff;
			}
			else if ( null != skyDomeNode )
			{
				isSkyDomeEnabled = onoff;
			}
			else if ( null != skyPlaneNode )
			{
				isSkyPlaneEnabled = onoff;
			}
		}
	}

	public class PCZSceneManagerFactory : SceneManagerFactory
	{
		protected override void InitMetaData()
		{
			metaData.typeName = "PCZSceneManager";
			metaData.description = "Scene manager organising the scene using Portal Connected Zones.";
			metaData.sceneTypeMask = SceneType.Generic;
			metaData.worldGeometrySupported = false;
		}

		public override SceneManager CreateInstance( string name )
		{
			return new PCZSceneManager( name );
		}

		public override void DestroyInstance( SceneManager instance )
		{
			instance.ClearScene();
		}
	}
}
