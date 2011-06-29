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

using System;
using System.Collections.Generic;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Math;
using Axiom.Math.Collections;
#endregion Namespace Declarations

namespace Axiom.SceneManagers.PortalConnected
{

    //    * Portal-Connected Zone data structure for managing scene nodes.

    //    

    //ORIGINAL LINE: class _OgrePCZPluginExport PCZone : public DisposableObject
    public abstract class PCZone : DisposableObject
    {

        public enum NodeListType : int
        {
            HomeNodeList = 1,
            VistoryNodeList = 2
        }

        /// <summary>
        /// name generator
        /// </summary>
        private static NameGenerator<PCZone> _nameGenerator = new NameGenerator<PCZone>("PCZone");

        // name of the zone (must be unique)
        public string Name = "";
        /// Zone type name
        public string ZoneTypeName = "ZoneType_Undefined";
        // frame counter for visibility
        public ulong LastVisibleFrame = 0;
        // last camera which this zone was visible to
        public PCZCamera LastVisibleFromCamera = null;
        // flag determining whether or not this zone has sky in it.
        public bool HasSky = false;
        //SceneNode which corresponds to the enclosure for this zone
        private PCZSceneNode _enclosureNode = null;
        public virtual PCZSceneNode EnclosureNode
        {
            get { return _enclosureNode; }
            set { _enclosureNode = value; }
        }

        // list of SceneNodes contained in this particular PCZone
        public List<PCZSceneNode> HomeNodeList = new List<PCZSceneNode>();
        // list of SceneNodes visiting this particular PCZone
        public List<PCZSceneNode> VisitorNodeList = new List<PCZSceneNode>();
        // flag recording whether any portals in this zone have moved 
        private bool mPortalsUpdated = false;
        //* list of Portals which this zone contains (each portal leads to another zone)
        public List<Portal> Portals = new List<Portal>();
        public List<AntiPortal> AntiPortals = new List<AntiPortal>();
        // pointer to the pcz scene manager that created this zone
        public PCZSceneManager PCZSM = new PCZSceneManager("");
        // user defined data pointer - NOT allocated or deallocated by the zone!  
        // you must clean it up yourself!
        protected object mUserData = null;

        /// <summary>
        /// Default constructor
        /// </summary>
        public PCZone(PCZSceneManager creator)
            : this(creator, _nameGenerator.GetNextUniqueName())
        {
        }

        public PCZone(PCZSceneManager creator, string name)
        {
            Name = name;
            PCZSM = creator;
            HasSky = false;
        }
        public void Dispose()
        {
        }


        public abstract void AddNode(PCZSceneNode NamelessParameter);

        //    * Removes all references to a SceneNode from this PCZone.        
        public abstract void RemoveNode(PCZSceneNode NamelessParameter);

        //    * Remove all nodes from the node reference list and clear it
        public void ClearNodeLists(short type)
        {
            HomeNodeList.Clear();
            VisitorNodeList.Clear();

        }

        //    * Indicates whether or not this zone requires zone-specific data for 
        //		 *  each scene node
        //		 
        private bool _RequiresZoneSpecificNodeData = false;
        public virtual bool RequiresZoneSpecificNodeData
        {
            get { return _RequiresZoneSpecificNodeData; }
            set { _RequiresZoneSpecificNodeData = value; }
        }

        //    * create zone specific data for a node
        // create node specific zone data if necessary
        public void CreateNodeZoneData(PCZSceneNode UnnamedParameter1)
        {
        }

        //    * find a matching portal (for connecting portals)
        //		
        public Portal FindMatchingPortal(Portal portal)
        {
            // look through all the portals in zone2 for a match
            List<Portal> pi2;
            foreach (Portal portal2 in Portals)
            {
                //portal2->updateDerivedValues();
                if (portal2.TargetZone== null && portal2.closeTo(portal) && portal2.DerivedDirection.Dot(portal.DerivedDirection) < -0.9)
                {
                    // found a match!
                    return portal2;
                }
            }
            // no match
            return null;
        }

        // Add a portal to the zone 
        public void AddPortal(Portal newPortal)
        {
            if (newPortal != null)
            {
                // make sure portal is unique (at least in this zone)
                foreach (Portal portal in Portals)
                {
                    if (portal.Name == newPortal.Name)
                    {
                        throw new AxiomException("A portal with the name " + newPortal.Name + " already exists", "PCZone.AddPortal");
                    }
                }

                // add portal to portals list
                Portals.Add(newPortal);

                // tell the portal which zone it's currently in
                newPortal.CurrentHomeZone = this;
            }
        }

        // Remove a portal from the zone 

        // Remove a portal from the zone (does not erase the portal object, just removes reference) 
        public void RemovePortal(Portal removePortal)
        {
            if (removePortal != null && Portals.Contains(removePortal))
            {
                Portals.Remove(removePortal);
            }
        }

        // Add an anti portal to the zone 

        // Add an anti portal to the zone 
        public void AddAntiPortal(AntiPortal newAntiPortal)
        {
            if (newAntiPortal != null)
            {
                // make sure portal is unique (at least in this zone)
                foreach (AntiPortal antiportal in AntiPortals)
                {
                    if (antiportal.Name == newAntiPortal.Name)
                    {
                        throw new AxiomException("An anti portal with the name " + newAntiPortal.Name + " already exists", "PCZone.AddAntiPortal");
                    }
                }
                // add portal to portals list
                AntiPortals.Add(newAntiPortal);

                // tell the portal which zone it's currently in
                newAntiPortal.CurrentHomeZone = this;
            }
        }

        // Remove an anti portal from the zone 

        // Remove an anti portal from the zone 
        public void _removeAntiPortal(AntiPortal removeAntiPortal)
        {
            if (removeAntiPortal != null)
            {
                AntiPortals.Remove(removeAntiPortal);
            }
        }

        //    * (recursive) check the given node against all portals in the zone
        //		
        public abstract void CheckNodeAgainstPortals(PCZSceneNode NamelessParameter1, Portal NamelessParameter2);

        //    * (recursive) check the given light against all portals in the zone
        //    
        public abstract void CheckLightAgainstPortals(PCZLight light,
                                               ulong frameCount,
                                               PCZFrustum portalFrustum,
                                               Portal ignorePortal);

        //     Update the zone data for each portal 
        //		
        public abstract void UpdatePortalsZoneData();

        //* Mark nodes dirty base on moving portals. 
        public abstract void DirtyNodeByMovingPortals();

        // Update a node's home zone 
        public abstract PCZone UpdateNodeHomeZone(PCZSceneNode pczsn, bool allowBackTouces);

        //    * Find and add visible objects to the render queue.
        //    @remarks
        //    Starts with objects in the zone and proceeds through visible portals   
        //    This is a recursive call (the main call should be to _findVisibleObjects)
        //    
        /** Find and add visible objects to the render queue.
        @remarks
        Starts with objects in the zone and proceeds through visible portals
        This is a recursive call (the main call should be to _findVisibleObjects)
        */
        public abstract void FindVisibleNodes(PCZCamera camera,
                                      ref List<PCZSceneNode> visibleNodeList,
                                      RenderQueue queue,
                                      VisibleObjectsBoundsInfo visibleBounds,
                                      bool onlyShadowCasters,
                                      bool displayNodes,
                                      bool showBoundingBoxes);

        /* Functions for finding Nodes that intersect various shapes */
        public abstract void FindNodes(AxisAlignedBox t,
                                 ref List<PCZSceneNode> list,
                                 List<Portal> visitedPortals,
                                 bool includeVisitors,
                                 bool recurseThruPortals,
                                 PCZSceneNode exclude);
        public abstract void FindNodes(Sphere t,
                                 ref List<PCZSceneNode> nodes,
                                 List<Portal> portals,
                                 bool includeVisitors,
                                 bool recurseThruPortals,
                                 PCZSceneNode exclude);
        public abstract void FindNodes(PlaneBoundedVolume t,
                                 ref List<PCZSceneNode> list,
                                 List<Portal> visitedPortals,
                                 bool includeVisitors,
                                 bool recurseThruPortals,
                                 PCZSceneNode exclude);
        public abstract void FindNodes(Ray t,
                                 ref List<PCZSceneNode> list,
                                 List<Portal> visitedPortals,
                                 bool includeVisitors,
                                 bool recurseThruPortals,
                                 PCZSceneNode exclude);

        //* Sets the options for the Zone 
        public abstract bool SetOption(string NamelessParameter1, object NamelessParameter2);
        //    * called when the scene manager creates a camera in order to store the first camera created as the primary
        //			one, for determining error metrics and the 'home' terrain page.
        //		
        public abstract void NotifyCameraCreated(Camera c);

        protected RenderQueueGroupID worldGeometryRenderQueueId = RenderQueueGroupID.WorldGeometryOne;

        public abstract void NotifyWorldGeometryRenderQueue(RenderQueueGroupID renderQueueGroupID);

        public virtual RenderQueueGroupID WorldGeometryRenderQueueId
        {
            get
            {
                return this.worldGeometryRenderQueueId;
            }

            set
            {
                this.worldGeometryRenderQueueId = value;
            }
        }

        // Called when a _renderScene is called in the SceneManager 
        public abstract void NotifyBeginRenderScene();

        // called by PCZSM during setZoneGeometry() 
        public abstract void SetZoneGeometry(string filename, PCZSceneNode parentNode);
        // get the world coordinate aabb of the zone 

        // get the aabb of the zone - default implementation
        //   uses the enclosure node, but there are other perhaps
        //   better ways
        //	
        public void GetAABB(ref AxisAlignedBox aabb)
        {
            // if there is no node, just return a null box
            if (EnclosureNode == null)
            {
                aabb = null;
            }
            else
            {
                aabb = EnclosureNode.WorldAABB;
                // since this is the "local" AABB, subtract out any translations
                aabb.Minimum = (aabb.Minimum - EnclosureNode.DerivedPosition);
                aabb.Maximum = (aabb.Minimum - EnclosureNode.DerivedPosition);
            }
            return;
        }

        public bool PortalsUpdated
        {
            get { return mPortalsUpdated; }
            set { mPortalsUpdated = value ; }
        }

        public object UserData
        {
            get { return mUserData; }
            set { mUserData = value; }
        }

        //* Binary predicate for portal <-> camera distance sorting. 
        protected class PortalSortDistance
        {
            public Vector3 cameraPosition = Vector3.Zero;
            public PortalSortDistance(Vector3 inCameraPosition)
            {
                cameraPosition = inCameraPosition;
            }


            ////ORIGINAL LINE: bool _OgrePCZPluginExport operator ()(const PortalBase* p1, const PortalBase* p2) const
            ////C++ TO C# CONVERTER TODO TASK: The () operator cannot be overloaded in C#:
            //        public static bool operator ()(PortalBase p1, PortalBase p2)
            //        {
            //            Real depth1 = p1.DerivedCP.DistanceSquared(cameraPosition);
            //            Real depth2 = p2.DerivedCP.DistanceSquared(cameraPosition);
            //            return (depth1 < depth2);
            //        }
        }

    }
}



