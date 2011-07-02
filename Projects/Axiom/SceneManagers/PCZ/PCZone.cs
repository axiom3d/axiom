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
        /// <summary>
        /// Node List Type
        /// </summary>
        public enum NodeListType : int
        {
            HomeNodeList = 1,
            VistoryNodeList = 2
        }

        /// <summary>
        /// name generator
        /// </summary>
        private static NameGenerator<PCZone> _nameGenerator = new NameGenerator<PCZone>("PCZone");

        private string _name = "";
        /// <summary>
        /// name of the zone (must be unique)
        /// </summary>
        public string Name
        {
            get { return _name; }
            set { _name = value; }

        }

        private string _zoneTypeName = "ZoneType_Undefined";
        /// <summary>
        /// Zone type name
        /// </summary>
        public string ZoneTypeName
        {
            get { return _zoneTypeName; }
            set { _zoneTypeName = value; }
        }

        private ulong _lastVisibleFrame = 0;
        /// <summary>
        /// frame counter for visibility
        /// </summary>
        public ulong LastVisibleFrame
        {
            get { return _lastVisibleFrame; }
            set { _lastVisibleFrame = value; }
        }

        private PCZCamera _lastVisibleFromCamera = null;
        /// <summary>
        /// last camera which this zone was visible to
        /// </summary>
        public PCZCamera LastVisibleFromCamera
        {
            get { return _lastVisibleFromCamera; }
            set { _lastVisibleFromCamera = value; }
        }

        // flag determining whether or not this zone has sky in it.
        private bool _hasSky = false;
        public bool HasSky
        {
            get { return _hasSky; }
            set { _hasSky = value; }
        }

        //SceneNode which corresponds to the enclosure for this zone
        private PCZSceneNode _enclosureNode = null;
        public virtual PCZSceneNode EnclosureNode
        {
            get { return _enclosureNode; }
            set { _enclosureNode = value; }
        }
        
        private List<PCZSceneNode> _homeNodeList = new List<PCZSceneNode>();
        /// <summary>
        /// list of SceneNodes contained in this particular PCZone
        /// </summary>
        public List<PCZSceneNode> HomeNodeList
        {
            get { return _homeNodeList; }
            set { _homeNodeList = value; }
        }

        // list of SceneNodes visiting this particular PCZone
        private List<PCZSceneNode> _visitorNodeList = new List<PCZSceneNode>();
        public List<PCZSceneNode> VisitorNodeList
        {
            get { return _visitorNodeList; }
            set { _visitorNodeList = value; }
        }

        private bool _portalsUpdated = false;
        /// <summary>
        /// flag recording whether any portals in this zone have moved 
        /// </summary>
        public bool PortalsUpdated
        {
            get { return _portalsUpdated; }
            set { _portalsUpdated = value; }
        }
        private List<Portal> _portals = new List<Portal>();
        /// <summary>
        /// list of Portals which this zone contains (each portal leads to another zone)
        /// </summary>
        public List<Portal> Portals
        {
            get { return _portals; }
            set { _portals = value; }
        }

        private List<AntiPortal> _antiPortals = new List<AntiPortal>();
        public List<AntiPortal> AntiPortals
        {
            get { return _antiPortals; }
            set { _antiPortals = value; }
        }

        private PCZSceneManager _pCZSM = new PCZSceneManager("");
        /// <summary>
        /// pointer to the pcz scene manager that created this zone
        /// </summary>
        public PCZSceneManager PCZSM
        {
            get { return _pCZSM; }
            set { _pCZSM = value; }
        }

        // user defined data pointer - NOT allocated or deallocated by the zone!  
        // you must clean it up yourself!
        private object _userData = null;

        /// <summary>
        /// Default constructor
        /// </summary>
        public PCZone(PCZSceneManager creator)
            : this(creator, _nameGenerator.GetNextUniqueName())
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="creator">PCZSceneManager</param>
        /// <param name="name">string</param>
        public PCZone(PCZSceneManager creator, string name)
        {
            _name = name;
            _pCZSM = creator;
        }

        #region IDisposable Implementation

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
                    ClearNodeLists();
                    this._homeNodeList = null;
                    this._visitorNodeList = null;
                }

                // There are no unmanaged resources to release, but
                // if we add them, they need to be released here.
            }

            base.dispose(disposeManagedResources);
        }

        #endregion IDisposable Implementation

        /// <summary>
        /// AddNode
        /// </summary>
        /// <param name="node">PCZSceneNode</param>
        public abstract void AddNode(PCZSceneNode node);

        /// <summary>
        /// Removes all references to a SceneNode from this PCZone.   
        /// </summary>
        /// <param name="node">PCZSceneNode</param>
        public abstract void RemoveNode(PCZSceneNode node);

        /// <summary>
        /// Remove all nodes from the node reference list and clear it
        /// </summary>
        /// <param name="type">ClearNodeLists</param>
        public void ClearNodeLists()
        {
            _homeNodeList.Clear();
            _visitorNodeList.Clear();

        }

        /// <summary>
        /// Indicates whether or not this zone requires zone-specific data for each scene node
        /// </summary>
        private bool _RequiresZoneSpecificNodeData = false;
        public virtual bool RequiresZoneSpecificNodeData
        {
            get { return _RequiresZoneSpecificNodeData; }
            set { _RequiresZoneSpecificNodeData = value; }
        }

        /// <summary>
        /// create zone specific data for a node
        /// create node specific zone data if necessary
        /// </summary>
        /// <param name="node">PCZSceneNode</param>
        public void CreateNodeZoneData(PCZSceneNode node)
        {
        }


        /// <summary>
        /// find a matching portal (for connecting portals)
        /// </summary>
        /// <param name="portal">Portal</param>
        /// <returns>Portal</returns>
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

        /// <summary>
        /// Add a portal to the zone 
        /// </summary>
        /// <param name="newPortal">Portal</param>
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

        /// <summary>
        /// Remove a portal from the zone (does not erase the portal object, just removes reference) 
        /// </summary>
        /// <param name="removePortal">Portal</param>
        public void RemovePortal(Portal removePortal)
        {
            if (removePortal != null && Portals.Contains(removePortal))
            {
                Portals.Remove(removePortal);
            }
        }

        /// <summary>
        /// Add an anti portal to the zone 
        /// </summary>
        /// <param name="newAntiPortal"></param>
        public void AddAntiPortal(AntiPortal newAntiPortal)
        {
            if (newAntiPortal != null)
            {
                // make sure portal is unique (at least in this zone)
                foreach (AntiPortal antiportal in AntiPortals)
                {
                    if (antiportal.Name == newAntiPortal.Name)
                    {
                        throw new AxiomException("An anti portal with the name {0} already exists.", newAntiPortal.Name);
                    }
                }
                // add portal to portals list
                AntiPortals.Add(newAntiPortal);

                // tell the portal which zone it's currently in
                newAntiPortal.CurrentHomeZone = this;
            }
        }

        /// <summary>
        /// Remove an anti portal from the zone 
        /// </summary>
        /// <param name="removeAntiPortal">AntiPortal</param>
        public void _removeAntiPortal(AntiPortal removeAntiPortal)
        {
            if (removeAntiPortal != null)
            {
                AntiPortals.Remove(removeAntiPortal);
            }
        }

        /// <summary>
        /// (recursive) check the given node against all portals in the zone
        /// </summary>
        /// <param name="node">PCZSceneNode</param>
        /// <param name="portal">portal</param>
        public abstract void CheckNodeAgainstPortals(PCZSceneNode node, Portal portal);

        /// <summary>
        /// (recursive) check the given light against all portals in the zone
        /// </summary>
        /// <param name="light">light</param>
        /// <param name="frameCount">frameCount</param>
        /// <param name="portalFrustum">PCZFrustum</param>
        /// <param name="ignorePortal">Portal</param>
        public abstract void CheckLightAgainstPortals(PCZLight light,
                                               ulong frameCount,
                                               PCZFrustum portalFrustum,
                                               Portal ignorePortal);

        /// <summary>
        /// Update the zone data for each portal 
        /// </summary>
        public abstract void UpdatePortalsZoneData();

        /// <summary>
        /// Mark nodes dirty base on moving portals. 
        /// </summary>
        public abstract void DirtyNodeByMovingPortals();

        /// <summary>
        /// Update a node's home zone 
        /// </summary>
        /// <param name="pczsn">PCZSceneNode/param>
        /// <param name="allowBackTouces">bool</param>
        /// <returns>PCZSceneNode</returns>
        public abstract PCZone UpdateNodeHomeZone(PCZSceneNode pczsn, bool allowBackTouces);

        /// <summary>
        ///  Find and add visible objects to the render queue.
        ///    @remarks
        ///    Starts with objects in the zone and proceeds through visible portals   
        ///    This is a recursive call (the main call should be to _findVisibleObjects)
        ///    
        /// Find and add visible objects to the render queue.
        /// @remarks
        ///     Starts with objects in the zone and proceeds through visible portals
        ///     This is a recursive call (the main call should be to _findVisibleObjects)
        /// </summary>
        /// <param name="camera">PCZCamera</param>
        /// <param name="visibleNodeList">List<PCZSceneNode></param>
        /// <param name="queue">RenderQueue</param>
        /// <param name="visibleBounds">VisibleObjectsBoundsInfo</param>
        /// <param name="onlyShadowCasters">bool</param>
        /// <param name="displayNodes">bool</param>
        /// <param name="showBoundingBoxes">bool</param>
        public abstract void FindVisibleNodes(PCZCamera camera,
                                      ref List<PCZSceneNode> visibleNodeList,
                                      RenderQueue queue,
                                      VisibleObjectsBoundsInfo visibleBounds,
                                      bool onlyShadowCasters,
                                      bool displayNodes,
                                      bool showBoundingBoxes);

        /// <summary>
        /// Function for finding Nodes that intersect an AxisAlignedBox
        /// </summary>
        /// <param name="t">AxisAlignedBox</param>
        /// <param name="nodes">List<PCZSceneNode></param>
        /// <param name="visitedPortals">List<Portal></param>
        /// <param name="includeVisitors">bool</param>
        /// <param name="recurseThruPortals">bool</param>
        /// <param name="exclude">PCZSceneNode</param>
        public abstract void FindNodes(AxisAlignedBox t,
                                 ref List<PCZSceneNode> nodes,
                                 List<Portal> visitedPortals,
                                 bool includeVisitors,
                                 bool recurseThruPortals,
                                 PCZSceneNode exclude);

        /// <summary>
        /// Function for finding Nodes that intersect a Sphere
        /// </summary>
        /// <param name="t">Sphere</param>
        /// <param name="nodes">List<PCZSceneNode> </param>
        /// <param name="portals">List<Portal> portals</param>
        /// <param name="includeVisitors">bool</param>
        /// <param name="recurseThruPortals">bool</param>
        /// <param name="exclude">PCZSceneNode</param>
        public abstract void FindNodes(Sphere t,
                                 ref List<PCZSceneNode> nodes,
                                 List<Portal> portals,
                                 bool includeVisitors,
                                 bool recurseThruPortals,
                                 PCZSceneNode exclude);

        /// <summary>
        /// Function for finding Nodes that intersect a PlaneBoundedVolume
        /// </summary>
        /// <param name="t">PlaneBoundedVolume</param>
        /// <param name="nodes">List<PCZSceneNode></param>
        /// <param name="portals">List<Portal> portals</param>
        /// <param name="includeVisitors">bool</param>
        /// <param name="recurseThruPortals">bool</param>
        /// <param name="exclude">PCZSceneNode</param>
        public abstract void FindNodes(PlaneBoundedVolume t,
                                 ref List<PCZSceneNode> list,
                                 List<Portal> visitedPortals,
                                 bool includeVisitors,
                                 bool recurseThruPortals,
                                 PCZSceneNode exclude);

        /// <summary>
        /// Function for finding Nodes that intersect a ray
        /// </summary>
        /// <param name="t"></param>
        /// <param name="nodes">List<PCZSceneNode> </param>
        /// <param name="portals">List<Portal> portals</param>
        /// <param name="includeVisitors">bool</param>
        /// <param name="recurseThruPortals">bool</param>
        /// <param name="exclude">PCZSceneNode</param>
        public abstract void FindNodes(Ray t,
                                 ref List<PCZSceneNode> nodes,
                                 List<Portal> visitedPortals,
                                 bool includeVisitors,
                                 bool recurseThruPortals,
                                 PCZSceneNode exclude);

        /// <summary>
        /// Sets the options for the Zone 
        /// </summary>
        /// <param name="NamelessParameter1">string</param>
        /// <param name="NamelessParameter2">object</param>
        /// <returns></returns>
        public abstract bool SetOption(string NamelessParameter1, object NamelessParameter2);

        /// <summary>
        /// called when the scene manager creates a camera in order to store the first camera created as the primary
        /// one, for determining error metrics and the 'home' terrain page.	
        /// </summary>
        /// <param name="c">Camera</param>
        public abstract void NotifyCameraCreated(Camera c);

        private RenderQueueGroupID worldGeometryRenderQueueId = RenderQueueGroupID.WorldGeometryOne;
        /// <summary>
        ///  worldGeometryRenderQueueId
        /// </summary>
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

        /// <summary>
        /// NotifyWorldGeometryRenderQueue
        /// </summary>
        /// <param name="renderQueueGroupID">RenderQueueGroupID</param>
        public abstract void NotifyWorldGeometryRenderQueue(RenderQueueGroupID renderQueueGroupID);

        /// <summary>
        /// Called when a _renderScene is called in the SceneManager 
        /// </summary>
        public abstract void NotifyBeginRenderScene();

        /// <summary>
        /// called by PCZSM during setZoneGeometry() 
        /// </summary>
        /// <param name="filename">string</param>
        /// <param name="parentNode">PCZSceneNode</param>
        public abstract void SetZoneGeometry(string filename, PCZSceneNode parentNode);


        /// <summary>
        /// get the world coordinate aabb of the zone 
        ///
        /// get the aabb of the zone - default implementation
        ///   uses the enclosure node, but there are other perhaps
        ///   better ways
        /// </summary>
        /// <param name="aabb"></param>
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

        /// <summary>
        /// UserData
        /// </summary>
        public object UserData
        {
            get { return _userData; }
            set { _userData = value; }
        }


        /// <summary>
        /// Binary predicate for portal <-> camera distance sorting. 
        /// </summary>
        protected class PortalSortDistance : IComparer<PortalBase>
        {
            private Vector3 _cameraPosition = Vector3.Zero;
            /// <summary>
            /// 
            /// </summary>
            public Vector3 CameraPosition
            {
                get { return _cameraPosition; }
                set { _cameraPosition = value; }
            }

            ////ORIGINAL LINE: bool _OgrePCZPluginExport operator ()(const PortalBase* p1, const PortalBase* p2) const
            ////C++ TO C# CONVERTER TODO TASK: The () operator cannot be overloaded in C#:
            //        public static bool operator ()(PortalBase p1, PortalBase p2)
            //        {
            //            Real depth1 = p1.DerivedCP.DistanceSquared(cameraPosition);
            //            Real depth2 = p2.DerivedCP.DistanceSquared(cameraPosition);
            //            return (depth1 < depth2);
            //        }

            #region IComparable Members


            int IComparer<PortalBase>.Compare(PortalBase portal1, PortalBase portal2)
            {

                Real depth1 = portal1.DerivedCP.DistanceSquared(_cameraPosition);
                Real depth2 = portal2.DerivedCP.DistanceSquared(_cameraPosition);

                if (depth1 > depth2)
                {
                    return 1;
                }
                if (depth1 < depth2)
                {
                    return -1;
                }
                else
                {
                    return 0;
                }

            }

            #endregion
        }

    }
}



