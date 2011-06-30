#region MIT/X11 License
//Copyright (c) 2009 Axiom 3D Rendering Engine Project
//
//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//THE SOFTWARE.
#endregion License

#region SVN Version Information
// <file>
// <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
// <id value="$Id:$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using Axiom;
using Axiom.Core;
using Axiom.Math;
using Axiom.Math.Collections;
using Axiom.Collections;
using Axiom.Graphics;

using System.Diagnostics;
using System.Collections.Generic;
using System;
#endregion Namespace Declarations

namespace Axiom.SceneManagers.PortalConnected
{

    /// <summary>
    /// LightInfo
    /// </summary>
    public struct LightInfo
    {
        /// <summary>
        /// Just a pointer for comparison, the light might destroyed for some reason
        /// </summary>
        public Light Light;

        /// <summary>
        /// LightType
        /// </summary>
        public LightType Type; 

        /// <summary>
        /// Sets to zero if directional light
        /// </summary>
        public Real Range ; 

        /// <summary>
        /// Sets to zero if directional light
        /// </summary>
        public Vector3 Position;

        public static bool operator ==(LightInfo rhs, LightInfo b)
        {
            return b.Light == rhs.Light && b.Type == rhs.Type &&
                   b.Range == rhs.Range && b.Position == rhs.Position;
        }

        public static bool operator !=(LightInfo rhs, LightInfo b)
        {
            return !(b == rhs);
        }

        #region System.Object Implementation

        public override bool Equals(object obj)
        {
            if (!(obj is LightInfo))
                return false;

            LightInfo rhs = (LightInfo)obj;
            return this.Light == rhs.Light && this.Type == rhs.Type &&
                   this.Range == rhs.Range && this.Position == rhs.Position;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #endregion System.Object Implementation

    }

    /// <summary>
    /// PCZSceneManager
    /// </summary>
    public class PCZSceneManager : SceneManager
    {

        /// <summary>
        /// name generator
        /// </summary>
        private static NameGenerator<PCZSceneManager> nameGenerator = new NameGenerator<PCZSceneManager>("PCZSceneManager");

        /// <summary>
        /// type of default zone to be used
        /// </summary>
        private string defaultZoneTypeName = "ZoneType_Default";

        /// <summary>
        /// name of data file for default zone
        /// </summary>
        private string defaultZoneFileName = "none";

        /// <summary>
        /// list of visible nodes
        /// </summary>
        private List<PCZSceneNode> visibleNodes = new List<PCZSceneNode>();

        /// <summary>
        /// The root PCZone
        /// </summary>
        private PCZone defaultZone = null;

        /// <summary>
        /// The list of all PCZones
        /// </summary>
        private readonly List<PCZone> zones = new List<PCZone>();

        /// <summary>
        /// Master list of Portals in the world (includes all portals)
        /// </summary>
        private readonly List<Portal> portals = new List<Portal>();

        /// <summary>
        /// Portals visibility flag
        /// </summary>
        private bool showPortals = false;

        /// <summary>
        /// frame counter used in visibility determination
        /// </summary>
        private ulong frameCount = 0;

        /// <summary>
        /// ZoneFactoryManager instance
        /// </summary>
        private PCZoneFactoryManager zoneFactoryManager = null;

        /// <summary>
        /// The zone of the active camera (for shadow texture casting use)
        /// </summary>
        private PCZone activeCameraZone = null;

        /// <summary>
        /// Test LightInfo List  (private)
        /// </summary>
        private List<LightInfo> testLightInfos = new List<LightInfo>();

        /// <summary>
        /// Test LightInfo List  (protected)
        /// </summary>
        protected List<LightInfo> TestLightInfos
        {
            get { return testLightInfos; }
            set { testLightInfos = value; }
        }

        /// <summary>
        /// CachedLightInfos List (private)
        /// </summary>
        private List<LightInfo> cachedLightInfos = new List<LightInfo>();

        /// <summary>
        /// CachedLightInfos List (protected)
        /// </summary>
        protected List<LightInfo> CachedLightInfos
        {

            get { return cachedLightInfos; }
            set { cachedLightInfos = value; }
        }

        /// <summary>
        /// Constructor (auto generate name)
        /// </summary>
        public PCZSceneManager()
            : this(nameGenerator.GetNextUniqueName())
        {
        }

        /// <summary>
        /// Constructor (custom name)
        /// </summary>
        /// <param name="name"></param>
        public PCZSceneManager(string name)
            : base(name)
        {
            rootSceneNode = new PCZSceneNode(this, "Root");
            defaultRootNode = rootSceneNode;
        }

        /// <summary>
        /// Class level dispose method
        /// </summary>
        /// <remarks>
        /// When implementing this method in an inherited class the following template should be used;
        /// protected override void dispose( bool disposeManagedResources )
        /// {
        /// 	if ( !isDisposed )
        /// 	{
        /// 		if ( disposeManagedResources )
        /// 		{
        /// 			// Dispose managed resources.
        /// 		}
        ///
        /// 		// There are no unmanaged resources to release, but
        /// 		// if we add them, they need to be released here.
        /// 	}
        ///
        /// 	// If it is available, make the call to the
        /// 	// base class's Dispose(Boolean) method
        /// 	base.dispose( disposeManagedResources );
        /// }
        /// </remarks>
        /// <param name="disposeManagedResources">True if Unmanaged resources should be released.</param>
        protected override void dispose(bool disposeManagedResources)
        {
            if (!this.IsDisposed)
            {
                if (disposeManagedResources)
                {
                    // we don't delete the root scene node here because the
                    // base scene manager class does that.

                    // delete ALL portals
                    portals.Clear();

                    // delete all the zones
                    zones.Clear();
                    defaultZone = null;
                }
            }

            base.dispose(disposeManagedResources);
        }

        /// <summary>
        /// the default zone
        /// </summary>
        public PCZone DefaultZone
        {
            get
            {
                return defaultZone;
            }
        }

        /// <summary>
        /// Init
        /// </summary>
        /// <param name="defaultZoneTypeName">string</param>
        /// <param name="filename">string</param>
        public void Init(string defaultZoneTypeName, string filename)
        {
            // delete ALL portals
            portals.Clear();

            // delete all the zones
            zones.Clear();

            frameCount = 0;

            this.defaultZoneTypeName = defaultZoneTypeName;
            defaultZoneFileName = filename;

            // create a new default zone
            zoneFactoryManager = PCZoneFactoryManager.Instance;
            defaultZone = CreateZoneFromFile(this.defaultZoneTypeName, "Default_Zone", RootSceneNode as PCZSceneNode, defaultZoneFileName);
        }

        /// <summary>
        /// Create a portal instance
        /// </summary>
        /// <param name="name">name (Portal Name)</param>
        /// <param name="type">PortalType</param>
        /// <returns>Portal</returns>
        public Portal CreatePortal(string name, PortalType type)
        {
            Portal newPortal = new Portal(name, type);
            portals.Add(newPortal);
            return newPortal;
        }

        /// <summary>
        /// delete a portal instance by pointer
        /// </summary>
        /// <param name="portal"></param>
        public void DestroyPortal(Portal portal)
        {
            // remove the portal from it's target portal
            Portal targetPortal = portal.TargetPortal;
            if (null != targetPortal)
            {
                targetPortal.TargetPortal = null; // the targetPortal will still have targetZone value, but targetPortal will be invalid
            }
            // remove the Portal from it's home zone
            PCZone homeZone = portal.CurrentHomeZone;
            if (null != homeZone)
            {
                // inform zone of portal change. Do here since PCZone is abstract
                homeZone.PortalsUpdated = true;
                homeZone.RemovePortal(portal);
            }

            // remove the portal from the master portal list
            portals.Remove(portal);
        }

        /// <summary>
        /// delete a portal instance by name
        /// </summary>
        /// <param name="name"></param>
        public void DestroyPortal(string name)
        {
            // find the portal from the master portal list
            Portal thePortal = null;
            foreach (Portal portal in portals)
            {
                if (portal.Name == name)
                {
                    thePortal = portal;
                    portals.Remove(portal);
                    break;
                }
            }

            if (null != thePortal)
            {
                // remove the portal from it's target portal
                Portal targetPortal = thePortal.TargetPortal;
                if (null != targetPortal)
                {
                    targetPortal.TargetPortal = null ;
                }

                // remove the Portal from it's home zone
                PCZone homeZOne = thePortal.CurrentHomeZone;
                if (null != homeZOne)
                {
                    // inform zone of portal change
                    homeZOne.PortalsUpdated = true;
                    homeZOne.RemovePortal(thePortal);
                }
            }
        }

        /// <summary>
        /// Create a zone from a file (type of file)
        /// depends on the zone type
        /// ZoneType_Default uses an Ogre Model (.mesh) file
        /// ZoneType_Octree uses an Ogre Model (.mesh) file
        /// ZoneType_Terrain uses a Terrain.CFG file
        /// </summary>
        /// <param name="zoneTypeName">string</param>
        /// <param name="zoneName">string</param>
        /// <param name="parent">PCZSceneNode</param>
        /// <param name="filename">string</param>
        /// <returns>PCZone</returns>
        public PCZone CreateZoneFromFile(string zoneTypeName,
                                          string zoneName,
                                          PCZSceneNode parent,
                                          string filename)
        {
            PCZone newZone;

            // create a new default zone
            newZone = zoneFactoryManager.CreatePCZone(this, zoneTypeName, zoneName);
            // add to the global list of zones
            zones.Add(newZone);
            if (filename != "none")
            {
                // set the zone geometry
                newZone.SetZoneGeometry(filename, parent);
            }

            return newZone;
        }

        /// <summary>
        /// Get a zone by name
        /// </summary>
        /// <param name="name">string</param>
        /// <returns>PCZone</returns>
        public PCZone GetZoneByName(string name)
        {
            foreach (PCZone zone in zones)
            {
                if (zone.Name == name)
                {
                    return zone;
                }
            }

            return null;
        }

        /// <summary>
        /// Sets Zone Geometry
        /// </summary>
        /// <param name="name">string</param>
        /// <param name="parent">PCZSceneNode</param>
        /// <param name="filename">string</param>
        public void SetZoneGeometry(string name,
                                     PCZSceneNode parent,
                                     string filename)
        {
            foreach (PCZone zone in zones)
            {
                if (zone.Name == name)
                {
                    zone.SetZoneGeometry(filename, parent);
                    break;
                }
            }
        }

        /// <summary>
        /// Create Scene Node Impl (auto generate node name)
        /// </summary>
        /// <returns>SceneNode</returns>
        private SceneNode CreateSceneNodeImpl()
        {
            return new PCZSceneNode(this);
        }

        /// <summary>
        /// Create Scene Node Impl (with custom node name)
        /// </summary>
        /// <param name="nodeName"></param>
        /// <returns>SceneNode</returns>
        private SceneNode CreateSceneNodeImpl(string nodeName)
        {
            return new PCZSceneNode(this, nodeName);
        }

        /// <summary>
        /// CreateSceneNode
        /// </summary>
        /// <returns></returns>
        public override SceneNode CreateSceneNode()
        {
            SceneNode on = CreateSceneNodeImpl();
            sceneNodeList.Add(on.Name, on);
            // create any zone-specific data necessary
            CreateZoneSpecificNodeData((PCZSceneNode)on);
            // return pointer to the node
            return on;
        }

        /// <summary>
        /// CreateSceneNode (custom name)
        /// </summary>
        /// <param name="name">string</param>
        /// <returns>SceneNode</returns>
        public override SceneNode CreateSceneNode(string name)
        {
            // Check name not used
            if (sceneNodeList.ContainsKey(name))
            {
                throw new AxiomException("A scene node with the name " + name + " already exists. PCZSceneManager.CreateSceneNode");
            }
            SceneNode on = CreateSceneNodeImpl(name);
            sceneNodeList.Add(name, on);
            // create any zone-specific data necessary
            CreateZoneSpecificNodeData((PCZSceneNode)on);
            // return pointer to the node
            return on;
        }

        /// <summary>
        /// Create a camera for the scene
        /// </summary>
        /// <param name="name">name</param>
        /// <returns>Camera</returns>
        public override Camera CreateCamera(string name)
        {
            // Check name not used
            if (cameraList.ContainsKey(name))
            {
                throw new AxiomException("A camera with the name " + name + " already exists. PCZSceneManager.CreateCamera");
            }

            Camera c = new PCZCamera(name, this);
            cameraList.Add(name, c);

            // create visible bounds aab map entry
            //TODO: would be very nice to implements shadows processing like ogre does now...
            //mCamVisibleObjectsMap[c] = VisibleObjectsBoundsInfo();

            // tell all the zones about the new camera
            foreach (PCZone zone in zones)
            {
                zone.NotifyCameraCreated(c);
            }

            return c;
        }

        /// <summary>
        /// Destroy a Scene Node by name
        /// </summary>
        /// <param name="name">name</param>
        public override void DestroySceneNode(string name)
        {
            SceneNode on = GetSceneNode(name);

            if (null != on)
            {
                // remove references to the node from zones
                RemoveSceneNode(on);
            }

            // destroy the node
            base.DestroySceneNode(on);
        }

        /// <summary>
        /// Clear the Scene
        /// </summary>
        public override void ClearScene()
        {
            base.ClearScene();

            // delete all the zones
            zones.Clear();
            defaultZone = null;

            // re-initialize
            Init(defaultZoneTypeName, defaultZoneFileName);
        }



        // Overridden from SceneManager
        public new void RenderScene(Camera cam, Viewport vp, bool includeOverlays)
        {
            // notify all the zones that a scene render is starting
            foreach (PCZone zone in zones)
            {
                zone.NotifyBeginRenderScene();
            }

            // do the regular _renderScene
            base.RenderScene(cam, vp, includeOverlays);
        }

        // Set the zone which contains the sky node
        public void SetSkyZone(PCZone zone)
        {
            if (null == zone)
            {
                // if no zone specified, use default zone
                zone = defaultZone;
            }
            if (null != skyBoxNode)
            {
                ((PCZSceneNode)skyBoxNode).HomeZone = zone;
                ((PCZSceneNode)skyBoxNode).AnchorToHomeZone(zone);
                zone.HasSky = true;
            }
            if (null != skyDomeNode)
            {
                ((PCZSceneNode)skyDomeNode).HomeZone = zone;
                ((PCZSceneNode)skyDomeNode).AnchorToHomeZone(zone);
                zone.HasSky = true;
            }
            if (null != skyPlaneNode)
            {
                ((PCZSceneNode)skyPlaneNode).HomeZone = zone;
                ((PCZSceneNode)skyPlaneNode).AnchorToHomeZone(zone);
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
        protected override void UpdateSceneGraph(Camera cam)
        {
            // First do the standard scene graph update
            base.UpdateSceneGraph(cam);
            // Then do the portal update.  This is done after all the regular
            // scene graph node updates because portals can move (being attached to scene nodes)
            // (also clear node refs in every zone)
            //UpdatePortalSpatialData();
            // check for portal zone-related changes (portals intersecting other portals)
            UpdatePortalZoneData();
            // update all scene nodes
            UpdatePCZSceneNodes();
            // calculate zones affected by each light
            CalcZonesAffectedByLights(cam);
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
            foreach (PCZSceneNode pczsn in sceneNodeList.Values)
            {
                pczsn.SavePrevPosition();
            }
        }

        // Update the zone data for every zone portal in the scene

        public void UpdatePortalZoneData()
        {
            foreach (PCZone zone in zones)
            {
                // this call checks for portal zone changes & applies zone data changes as necessary
                zone.UpdatePortalsZoneData();
            }
        }

        // Update all PCZSceneNodes.
        public void UpdatePCZSceneNodes()
        {
            foreach (PCZSceneNode pczsn in sceneNodeList.Values)
            {
                if (pczsn.Enabled)
                {
                    // Update a single entry
                    UpdatePCZSceneNode(pczsn);
                }
            }
        }

        public void CalcZonesAffectedByLights(Camera cam)
        {
            MovableObjectCollection lightList = GetMovableObjectCollection(LightFactory.TypeName);
            //HACK: i dont know if this is exactly the same...
            lock (lightList)
            {
                foreach (PCZLight l in lightList.Values)
                {
                    if (l.NeedsUpdate)
                    {
                        l.UpdateZones(((PCZSceneNode)(cam.ParentSceneNode)).HomeZone, frameCount);
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
        public void UpdatePCZSceneNode(PCZSceneNode pczsn)
        {
            // Skip if root Zone has been destroyed (shutdown conditions)
            if (null == defaultZone)
            {
                return;
            }

            // Skip if the node is the sceneroot node
            if (pczsn == RootSceneNode)
            {
                return;
            }

            // clear all references to visiting zones
            pczsn.ClearVisitingZonesMap();

            // Find the current home zone of the node associated with the pczsn entry.
            UpdateHomeZone(pczsn, false);

            //* The following function does the following:
            //* 1) Check all portals in the home zone - if the node is touching the portal
            //*    then add the node to the connected zone as a visitor
            //* 2) Recurse into visited zones in case the node spans several zones
            //*
            // (recursively) check each portal of home zone to see if the node is touching
            if (null != pczsn.HomeZone &&
                 pczsn.AllowToVisit)
            {
                pczsn.HomeZone.CheckNodeAgainstPortals(pczsn, null);
            }

            // update zone-specific data for the node for any zones that require it
            pczsn.UpdateZoneData();
        }

        // Removes all references to the node from every zone in the scene.
        public void RemoveSceneNode(SceneNode sn)
        {
            // Skip if mDefaultZone has been destroyed (shutdown conditions)
            if (null == defaultZone)
            {
                return;
            }

            PCZSceneNode pczsn = (PCZSceneNode)sn;

            // clear all references to the node in visited zones
            pczsn.ClearNodeFromVisitedZones();

            // tell the node it's not in a zone
            pczsn.HomeZone = null;
        }

        // Set the home zone for a node
        public void AddPCZSceneNode(PCZSceneNode sn, PCZone homeZone)
        {
            // set the home zone
            sn.HomeZone = homeZone;
            // add the node
            homeZone.AddNode(sn);
        }

        //-----------------------------------------------------------------------
        // Create a zone with the given name and parent zone
        public PCZone CreateZone(string zoneType, string instanceName)
        {
            foreach (PCZone zone in zones)
            {
                if (zone.Name == instanceName)
                {
                    throw new AxiomException("A zone with the name " + instanceName + " already exists. PCZSceneManager.createZone");
                }
            }

            PCZone newZone = zoneFactoryManager.CreatePCZone(this, zoneType, instanceName);
            if (null != newZone)
            {
                // add to the global list of zones
                zones.Add(newZone);
                if (newZone.RequiresZoneSpecificNodeData)
                {
                    CreateZoneSpecificNodeData(newZone);
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
        public void DestroyZone(PCZone zone, bool destroySceneNodes)
        {
            // need to remove this zone from all lights affected zones list,
            // otherwise next frame _calcZonesAffectedByLights will call PCZLight::getNeedsUpdate()
            // which will try to access the zone pointer and will cause an access violation
            //HACK: again...
            MovableObjectCollection lightList = GetMovableObjectCollection(LightFactory.TypeName);
            lock (lightList)
            {
                foreach (PCZLight l in lightList.Values)
                {
                    if (l.NeedsUpdate)
                    {
                        // no need to check, this function does that anyway. if exists, is erased.
                        l.RemoveZoneFromAffectedZonesList(zone);
                    }
                }
            }

            // if not destroying scene nodes, then make sure any nodes who have
            foreach (PCZSceneNode pczsn in sceneNodeList.Values)
            {
                if (!destroySceneNodes)
                {
                    if (pczsn.HomeZone == zone)
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

            zones.Remove(zone);
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
        public void UpdateHomeZone(PCZSceneNode pczsn, bool allowBackTouches)
        {
            // Skip if root PCZoneTree has been destroyed (shutdown conditions)
            if (null == defaultZone)
            {
                return;
            }

            PCZone startzone;
            PCZone newHomeZone;

            // start with current home zone of the node
            startzone = pczsn.HomeZone;

            if (null != startzone)
            {
                if (!pczsn.Anchored)
                {
                    newHomeZone = startzone.UpdateNodeHomeZone(pczsn, false);
                }
                else
                {
                    newHomeZone = startzone;
                }

                if (newHomeZone != startzone)
                {
                    // add the node to the home zone
                    newHomeZone.AddNode(pczsn);
                }
            }
            else
            {
                // the node hasn't had it's home zone set yet, so do our best to
                // find the home zone using volume testing.
                Vector3 nodeCenter = pczsn.DerivedPosition;
                PCZone bestZone = FindZoneForPoint(nodeCenter);
                // set the best zone as the node's home zone
                pczsn.HomeZone = bestZone;
                // add the node to the zone
                bestZone.AddNode(pczsn);
            }

            return;
        }

        // Find the best (smallest) zone that contains a point
        public PCZone FindZoneForPoint(Vector3 point)
        {
            PCZone bestZone = defaultZone;
            Real bestVolume = Real.PositiveInfinity;

            foreach (PCZone zone in zones)
            {
                AxisAlignedBox aabb = new AxisAlignedBox();
                zone.GetAABB( ref aabb);
                SceneNode enclosureNode = zone.EnclosureNode;
                if (null != enclosureNode)
                {
                    // since this is the "local" AABB, add in world translation of the enclosure node
                    aabb.Minimum = aabb.Minimum + enclosureNode.DerivedPosition;
                    aabb.Maximum = aabb.Maximum + enclosureNode.DerivedPosition;
                }
                if (aabb!=null && aabb.Contains(point))
                {
                    if (aabb.Volume < bestVolume)
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
        public void CreateZoneSpecificNodeData(PCZSceneNode node)
        {
            foreach (PCZone zone in zones)
            {
                if (zone.RequiresZoneSpecificNodeData)
                {
                    zone.CreateNodeZoneData(node);
                }
            }
        }

        // create any zone-specific data necessary for all nodes for the given zone
        public void CreateZoneSpecificNodeData(PCZone zone)
        {
            if (zone.RequiresZoneSpecificNodeData)
            {
                foreach (PCZSceneNode node in sceneNodeList.Values)
                {
                    zone.CreateNodeZoneData(node);
                }
            }
        }

        // set the home zone for a scene node
        public void SetNodeHomeZone(SceneNode node, PCZone zone)
        {
            // cast the SceneNode to a PCZSceneNode
            PCZSceneNode pczsn = (PCZSceneNode)node;
            pczsn.HomeZone = zone;
        }

        // (optional) post processing for any scene node found visible for the frame
        public void AlertVisibleObjects()
        {
            throw new NotImplementedException("Not implemented");

            //foreach (PCZSceneNode node in visibleNodes)
            //{
            //    // this is where you would do whatever you wanted to the visible node
            //    // but right now, it does nothing.
            //}
        }

        //-----------------------------------------------------------------------
        public override Light CreateLight(string name)
        {
            return (Light)CreateMovableObject(name, PCZLightFactory.TypeName, null);
        }

        //-----------------------------------------------------------------------
        public override Light GetLight(string name)
        {
            return (Light)(GetMovableObject(name, PCZLightFactory.TypeName));
        }

        //-----------------------------------------------------------------------
        public bool HasLight(string name)
        {
            return HasMovableObject(name, PCZLightFactory.TypeName);
        }

        public bool HasSceneNode(string name)
        {
            return sceneNodeList.ContainsKey(name);
        }

        //-----------------------------------------------------------------------
        public void DestroyLight(string name)
        {
            DestroyMovableObject(name, PCZLightFactory.TypeName);
        }

        //-----------------------------------------------------------------------
        public void DestroyAllLights()
        {
            DestroyAllMovableObjectsByType(PCZLightFactory.TypeName);
        }

        //---------------------------------------------------------------------
        protected override void FindLightsAffectingFrustum(Camera camera)
        {
            base.FindLightsAffectingFrustum(camera);
            return;
            // Similar to the basic SceneManager, we iterate through
            // lights to see which ones affect the frustum.  However,
            // since we have camera & lights partitioned by zones,
            // we can check only those lights which either affect the
            // zone the camera is in, or affect zones which are visible to
            // the camera

            MovableObjectCollection lights = GetMovableObjectCollection(PCZLightFactory.TypeName);

            lock (lights)
            {
                foreach (PCZLight l in lights.Values)
                {
                    if (l.IsVisible /* && l.AffectsVisibleZone */ )
                    {
                        LightInfo lightInfo;
                        lightInfo.Light = l;
                        lightInfo.Type = l.Type;
                        if (lightInfo.Type == LightType.Directional)
                        {
                            // Always visible
                            lightInfo.Position = Vector3.Zero;
                            lightInfo.Range = 0;
                            testLightInfos.Add(lightInfo);
                        }
                        else
                        {
                            // NB treating spotlight as point for simplicity
                            // Just see if the lights attenuation range is within the frustum
                            lightInfo.Range = l.AttenuationRange;
                            lightInfo.Position = l.DerivedPosition;
                            Sphere sphere = new Sphere(lightInfo.Position, lightInfo.Range);
                            if (camera.IsObjectVisible(sphere))
                            {
                                testLightInfos.Add(lightInfo);
                            }
                        }
                    }
                }
            } // release lock on lights collection

            base.FindLightsAffectingFrustum(camera);

            // from here on down this function is same as Ogre::SceneManager

            // Update lights affecting frustum if changed
            if (cachedLightInfos != testLightInfos)
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

                foreach (LightInfo i in testLightInfos)
                {
                    if (IsShadowTechniqueTextureBased)
                    {
                        i.Light.TempSquaredDist = (camera.DerivedPosition - i.Light.DerivedPosition).LengthSquared;
                    }
                }

                if (IsShadowTechniqueTextureBased)
                {
                }

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
                cachedLightInfos = testLightInfos;

                // notify light dirty, so all movable objects will re-populate
                // their light list next time
                //_notifyLightsDirty();
                //Check: do we have something like this here?
            }
        }

        //---------------------------------------------------------------------
        protected override void EnsureShadowTexturesCreated()
        {
            bool createSceneNode = shadowTextureConfigDirty;

            //base.ensureShadowTexturesCreated();

            if (!createSceneNode)
            {
                return;
            }

            int count = shadowTextureCameras.Count;
            for (int i = 0; i < count; ++i)
            {
                PCZSceneNode node = (PCZSceneNode)rootSceneNode.CreateChildSceneNode(shadowTextureCameras[i].Name);
                node.AttachObject(shadowTextureCameras[i]);
                AddPCZSceneNode(node, defaultZone);
            }
        }

        //---------------------------------------------------------------------
        public new void DestroyShadowTextures()
        {
            int count = shadowTextureCameras.Count;
            for (int i = 0; i < count; ++i)
            {
                SceneNode node = shadowTextureCameras[i].ParentSceneNode;
                rootSceneNode.RemoveAndDestroyChild(node.Name);
            }
            base.DestroyShadowTextures();
        }

        //---------------------------------------------------------------------
        protected override void PrepareShadowTextures(Camera cam, Viewport vp)
        {
            if (((PCZSceneNode)cam.ParentSceneNode) != null)
            {
                activeCameraZone = ((PCZSceneNode)cam.ParentSceneNode).HomeZone;
            }
            base.PrepareShadowTextures(cam, vp);
        }

        //---------------------------------------------------------------------
        public void FireShadowTexturesPreCaster(Light light, Camera camera, int iteration)
        {
            PCZSceneNode camNode = (PCZSceneNode)camera.ParentSceneNode;

            if (light.Type == LightType.Directional)
            {
                if (camNode.HomeZone != activeCameraZone)
                {
                    AddPCZSceneNode(camNode, activeCameraZone);
                }
            }
            else
            {
                PCZSceneNode lightNode = (PCZSceneNode)light.ParentSceneNode;
                Debug.Assert(null != lightNode, "Error, lightNode shoudn't be null");
                PCZone lightZone = lightNode.HomeZone;
                if (camNode.HomeZone != lightZone)
                {
                    AddPCZSceneNode(camNode, lightZone);
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
            foreach (PCZone zone in zones)
            {
                // go through all the portals in the zone
                foreach (Portal portal in zone.Portals)
                {
                    //portal->updateDerivedValues();
                    if (null == portal.TargetZone)
                    {
                        // this is a portal without a connected zone - look for
                        // a matching portal in another zone
                        PCZone zone2;
                        foundMatch = false;
                        int j = 0;
                        while (!foundMatch && j != zones.Count)
                        {
                            zone2 = zones[j++];
                            if (zone2 != zone) // make sure we don't look in the same zone
                            {
                                Portal portal2 = zone2.FindMatchingPortal(portal);
                                if (null != portal2)
                                {
                                    // found a match!
                                    LogManager.Instance.Write("Connecting portal " + portal.Name + " to portal " + portal2.Name);
                                    foundMatch = true;
                                    portal.TargetZone = zone2;
                                    portal.TargetPortal = portal2;
                                    portal2.TargetZone = zone;
                                    portal2.TargetPortal = portal;
                                }
                            }
                        }
                        if (foundMatch == false)
                        {
                            // error, didn't find a matching portal!
                            throw new AxiomException("Could not find matching portal for portal " + portal.Name +
                                                      "PCZSceneManager.connectPortalsToTargetZonesByLocation");
                        }
                    }
                }
            }
        }

        // main visibility determination & render queue filling routine
        // over-ridden from base/default scene manager.  This is *the*
        // main call.
        public void FindVisibleObjects(Camera cam, VisibleObjectsBoundsInfo visibleBounds, bool onlyShadowCasters)
        {
            // clear the render queue
            renderQueue.Clear();
            // clear the list of visible nodes
            visibleNodes.Clear();

            // turn off sky
            EnableSky(false);

            // remove all extra culling planes
            ((PCZCamera)cam).RemoveAllExtraCullingPlanes();

            // increment the visibility frame counter
            //mFrameCount++;
            frameCount = Root.Instance.CurrentFrameCount;

            // update the camera
            ((PCZCamera)cam).Update();

            // get the home zone of the camera
            PCZone cameraHomeZone = ((PCZSceneNode)(cam.ParentSceneNode)).HomeZone;

            // walk the zones, starting from the camera home zone,
            // adding all visible scene nodes to the mVisibles list
            cameraHomeZone.LastVisibleFrame = frameCount;
            cameraHomeZone.FindVisibleNodes((PCZCamera)cam,
                                             ref visibleNodes,
                                             renderQueue,
                                             visibleBounds,
                                             onlyShadowCasters,
                                             displayNodes,
                                             showBoundingBoxes);
        }

        public void FindNodesIn(AxisAlignedBox box,
                                 ref List<PCZSceneNode> list,
                                 PCZone startZone,
                                 PCZSceneNode exclude)
        {
            List<Portal> visitedPortals = new List<Portal>();
            if (null != startZone)
            {
                // start in startzone, and recurse through portals if necessary
                startZone.FindNodes(box, ref list, visitedPortals, true, true, exclude);
            }
            else
            {
                // no start zone specified, so check all zones
                foreach (PCZone zone in zones)
                {
                    zone.FindNodes(box, ref list, visitedPortals, false, false, exclude);
                }
            }
        }

        public void FindNodesIn(Sphere sphere,
                                 ref List<PCZSceneNode> list,
                                 PCZone startZone,
                                 PCZSceneNode exclude)
        {
            List<Portal> visitedPortals = new List<Portal>();
            if (null != startZone)
            {
                // start in startzone, and recurse through portals if necessary
                startZone.FindNodes(sphere, ref list, visitedPortals, true, true, exclude);
            }
            else
            {
                // no start zone specified, so check all zones
                foreach (PCZone zone in zones)
                {
                    zone.FindNodes(sphere, ref list, visitedPortals, false, false, exclude);
                }
            }
        }

        public void FindNodesIn(PlaneBoundedVolume volumes,
                                 ref List<PCZSceneNode> list,
                                 PCZone startZone,
                                 PCZSceneNode exclude)
        {
            List<Portal> visitedPortals = new List<Portal>();
            if (null != startZone)
            {
                // start in startzone, and recurse through portals if necessary
                startZone.FindNodes(volumes, ref list, visitedPortals, true, true, exclude);
            }
            else
            {
                // no start zone specified, so check all zones
                foreach (PCZone zone in zones)
                {
                    zone.FindNodes(volumes, ref list, visitedPortals, false, false, exclude);
                }
            }
        }

        public void FindNodesIn(Ray r,
                                 ref List<PCZSceneNode> list,
                                 PCZone startZone,
                                 PCZSceneNode exclude)
        {
            List<Portal> visitedPortals = new List<Portal>();
            if (null != startZone)
            {
                // start in startzone, and recurse through portals if necessary
                startZone.FindNodes(r, ref list, visitedPortals, true, true, exclude);
            }
            else
            {
                foreach (PCZone zone in zones)
                {
                    zone.FindNodes(r, ref list, visitedPortals, false, false, exclude);
                }
            }
        }

        // get the current value of a scene manager option
        public bool GetOptionValues(string key, ref List<string> refValueList)
        {
            //return base.Options[key] SceneManager::GetOptionValues( key, refValueList );
            return false;
        }

        // get option keys (base along with PCZ-specific)
        public bool GetOptionKeys(ref List<string> refKeys)
        {
            foreach (string s in optionList.Keys)
            {
                refKeys.Add(s);
            }
            refKeys.Add("ShowBoundingBoxes");
            refKeys.Add("ShowPortals");

            return true;
        }

        public bool SetOption(string key, object val)
        {
            if (key == "ShowBoundingBoxes")
            {
                showBoundingBoxes = Convert.ToBoolean(val);
                return true;
            }

            else if (key == "ShowPortals")
            {
                showPortals = Convert.ToBoolean(val);
                return true;
            }
            // send option to each zone
            foreach (PCZone zone in zones)
            {
                if (zone.SetOption(key, val) == true)
                {
                    return true;
                }
            }

            // try regular scenemanager option
            if (Options.ContainsKey(key))
            {
                Options[key] = val;
            }
            else
            {
                Options.Add(key, val);
            }

            return true;
        }

        public bool GetOption(string key, ref object val)
        {
            if (key == "ShowBoundingBoxes")
            {
                val = showBoundingBoxes;
                return true;
            }
            if (key == "ShowPortals")
            {
                val = showPortals;
                return true;
            }

            if (Options.ContainsKey(key))
            {
                val = Options[key];

                return true;
            }

            return false;
        }

        //---------------------------------------------------------------------
        public override AxisAlignedBoxRegionSceneQuery CreateAABBRegionQuery(AxisAlignedBox box, uint mask)
        {
            PCZAxisAlignedBoxSceneQuery q = new PCZAxisAlignedBoxSceneQuery(this);
            q.Box = box;
            q.QueryMask = mask;
            return q;
        }

        //---------------------------------------------------------------------
        public override SphereRegionSceneQuery CreateSphereRegionQuery(Sphere sphere, uint mask)
        {
            PCZSphereSceneQuery q = new PCZSphereSceneQuery(this);
            q.Sphere = sphere;
            q.QueryMask = mask;
            return q;
        }

        //---------------------------------------------------------------------
        public override PlaneBoundedVolumeListSceneQuery CreatePlaneBoundedVolumeQuery(PlaneBoundedVolumeList volumes, uint mask)
        {
            PCZPlaneBoundedVolumeListSceneQuery q = new PCZPlaneBoundedVolumeListSceneQuery(this);
            q.Volumes = volumes;
            q.QueryMask = mask;
            return q;
        }

        //---------------------------------------------------------------------
        public override RaySceneQuery CreateRayQuery(Ray ray, uint mask)
        {
            PCZRaySceneQuery q = new PCZRaySceneQuery(this);
            q.Ray = ray;
            q.QueryMask = mask;
            return q;
        }

        //---------------------------------------------------------------------
        public override IntersectionSceneQuery CreateIntersectionQuery(uint mask)
        {
            PCZIntersectionSceneQuery q = new PCZIntersectionSceneQuery(this);
            q.QueryMask = mask;
            return q;
        }

        //---------------------------------------------------------------------
        // clear portal update flag from all zones
        public void ClearAllZonesPortalUpdateFlag()
        {
            foreach (PCZone zone in zones)
            {
                zone.PortalsUpdated = true;
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
                return showPortals;
            }
            set
            {
                showPortals = value;
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
                foreach (PCZone pcZone in zones)
                {
                    pcZone.WorldGeometryRenderQueueId = value;
                }
                // Call base version to set property
                base.WorldGeometryRenderQueueId = value;
            }
        }

        /// <summary>
        /// enable/disable sky rendering
        /// </summary>
        /// <param name="onoff"></param>
        public void EnableSky(bool onoff)
        {
            if (null != skyBoxNode)
            {
                isSkyBoxEnabled = onoff;
            }
            else if (null != skyDomeNode)
            {
                isSkyDomeEnabled = onoff;
            }
            else if (null != skyPlaneNode)
            {
                isSkyPlaneEnabled = onoff;
            }
        }
    }

	/// Factory for PCZSceneManager
    public class PCZSceneManagerFactory : SceneManagerFactory
    {
        protected override void InitMetaData()
        {
            metaData.typeName = "PCZSceneManager";
            metaData.description = "Scene manager organizing the scene using Portal Connected Zones.";
            metaData.sceneTypeMask = SceneType.Generic;
            metaData.worldGeometrySupported = false;
        }

        public override SceneManager CreateInstance(string name)
        {
            return new PCZSceneManager(name);
        }

        public override void DestroyInstance(SceneManager instance)
        {
            instance.ClearScene();
        }
    }

}