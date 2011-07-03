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
using Axiom.SceneManagers.PortalConnected;
using Visibility = Axiom.SceneManagers.PortalConnected.PCZFrustum.Visibility;

#endregion Namespace Declarations
namespace OctreeZone
{
    public class OctreeZone : PCZone
    {
        /// <summary>
        /// Name Generator 
        /// </summary>
        private static NameGenerator<DefaultZone> _nameGenerator = new NameGenerator<DefaultZone>("OctreeZone");

        /// The root octree
        private Octree _rootOctree = null;
        /// Max depth for the tree
        private int _maxDepth = 8;
        /// Size of the octree
        private AxisAlignedBox _box = new AxisAlignedBox(new Vector3(-10000, -10000, -10000), new Vector3(10000, 10000, 10000));

        /// <summary>
        /// Default Constructor
        /// </summary>
        /// <param name="creator"></param>
        public OctreeZone(PCZSceneManager creator)
            : this(creator, _nameGenerator.GetNextUniqueName())
        {
        }

        public OctreeZone(PCZSceneManager creator, string name)
            : base(creator, name)
        {
            ZoneTypeName = "ZoneType_Octree";
            // init octree
            AxisAlignedBox b = new AxisAlignedBox(new Vector3(-10000, -10000, -10000), new Vector3(10000, 10000, 10000));
            int depth = 8;
            _rootOctree = null;
            Init(b, depth);
        }

        public void Init(AxisAlignedBox box, int depth)
        {
            if (null != _rootOctree)
                _rootOctree = null;

            _rootOctree = new Octree(this, null);

            _maxDepth = depth;
            this._box = box;

            _rootOctree.Box = box;

            Vector3 min = box.Minimum;
            Vector3 max = box.Maximum;

            _rootOctree.HalfSize = (max - min) / 2;
        }

        public override void AddNode(PCZSceneNode n)
        {
            if (n.HomeZone == this)
            {
                // add a reference to this node in the "nodes at home in this zone" list
                HomeNodeList.Add(n);
            }
            else
            {
                // add a reference to this node in the "nodes visiting this zone" list
                VisitorNodeList.Add(n);
            }
        }

        public override void RemoveNode(PCZSceneNode n)
        {
            if (null != n)
                RemoveNodeFromOctree(n);

            if (n.HomeZone == this)
            {
                HomeNodeList.Remove(n);

            }
            else
            {
                VisitorNodeList.Remove(n);
            }
        }

        public override PCZSceneNode EnclosureNode
        {
            get { return base.EnclosureNode; }

            set
            {
                base.EnclosureNode = value;
                if (null != value)
                {
                    // anchor the node to this zone
                    base.EnclosureNode.AnchorToHomeZone(this);
                    // make sure node world bounds are up to date
                    //node._updateBounds();
                    // resize the octree to the same size as the enclosure node bounding box
                    Resize(base.EnclosureNode.WorldAABB);
                }
            }
        }

        public void ClearNodeList(NodeListType nodeListTypes)
        {
            if ((nodeListTypes | NodeListType.Home) == NodeListType.Home)
            {
                foreach (PCZSceneNode node in HomeNodeList)
                {
                    RemoveNode(node);
                }
                HomeNodeList.Clear();
            }

            if ((nodeListTypes | NodeListType.Visitor) == NodeListType.Visitor)
            {
                foreach (PCZSceneNode node in VisitorNodeList)
                {
                    RemoveNode(node);
                }
                HomeNodeList.Clear();
            }
        }

        public void Resize(AxisAlignedBox box)
        {
            // delete the octree
            _rootOctree = null;
            // create a new octree
            _rootOctree = new Octree(this, null);
            // set the octree bounding box
            _rootOctree.Box = box;
            Vector3 min = box.Minimum;
            Vector3 max = box.Maximum;
            _rootOctree.HalfSize = (max - min) * 0.5f;

            OctreeZoneData ozd;
            foreach (PCZSceneNode on in HomeNodeList)
            {
                ozd = (OctreeZoneData)(on.GetZoneData(this));
                if (ozd != null)
                {
                    ozd.Octant = null;
                    UpdateNodeOctant(ozd);
                }
            }

            foreach (PCZSceneNode on in VisitorNodeList)
            {
                ozd = (OctreeZoneData)(on.GetZoneData(this));
                ozd.Octant = null;
                UpdateNodeOctant(ozd);
            }
        }


        public override bool RequiresZoneSpecificNodeData
        {
            get
            {
                // Octree Zones have zone specific node data
                return true;
            }
        }


        /// <summary>
        /// NotifyWorldGeometryRenderQueue
        /// </summary>
        /// <param name="renderQueueGroupID"></param>
        public override void NotifyWorldGeometryRenderQueue(RenderQueueGroupID renderQueueGroupID)
        {

        }

        public override void CheckNodeAgainstPortals(PCZSceneNode pczsn, Portal ignorePortal)
        {
            if (pczsn == EnclosureNode ||
                pczsn.AllowToVisit == false)
            {
                // don't do any checking of enclosure node versus portals
                return;
            }

            PCZone connectedZone;
            foreach (Portal p in Portals)
            {
                //Check if the portal intersects the node
                if (p != ignorePortal &&
                    p.Intersects(pczsn) != PortalIntersectResult.NoIntersect)
                {
                    // node is touching this portal
                    connectedZone = p.TargetZone;
                    // add zone to the nodes visiting zone list unless it is the home zone of the node
                    if (connectedZone != pczsn.HomeZone &&
                        !pczsn.IsVisitingZone(connectedZone))
                    {
                        pczsn.AddZoneToVisitingZonesMap(connectedZone);
                        // tell the connected zone that the node is visiting it
                        connectedZone.AddNode(pczsn);
                        //recurse into the connected zone
                        connectedZone.CheckNodeAgainstPortals(pczsn, p.TargetPortal);
                    }
                }
            }
        }

        public override void CheckLightAgainstPortals(PCZLight light, ulong frameCount, PCZFrustum portalFrustum, Portal ignorePortal)
        {
            foreach (Portal p in Portals)
            {
                if (p != ignorePortal)
                {
                    // calculate the direction vector from light to portal
                    Vector3 lightToPortal = p.DerivedCP - light.DerivedPosition;
                    if (portalFrustum.IsObjectVisible(p))
                    {
                        // portal is facing the light, but some light types need to
                        // check illumination radius too.
                        PCZone targetZone = p.TargetZone;
                        switch (light.Type)
                        {
                            case LightType.Point:
                                // point lights - just check if within illumination range
                                if (lightToPortal.Length <= light.AttenuationRange)
                                {
                                    // if portal is quad portal it must be pointing towards the light
                                    if ((p.Type == PortalType.Quad && lightToPortal.Dot(p.DerivedDirection) < 0.0) ||
                                        (p.Type != PortalType.Quad))
                                    {
                                        if (!light.AffectsZone(targetZone))
                                        {
                                            light.AddZoneToAffectedZonesList(targetZone);
                                            if (targetZone.LastVisibleFrame == frameCount)
                                            {
                                                light.AffectsVisibleZone = true;
                                            }
                                            // set culling frustum from the portal
                                            portalFrustum.AddPortalCullingPlanes(p);
                                            // recurse into the target zone of the portal
                                            p.TargetZone.CheckLightAgainstPortals(light,
                                                                                        frameCount,
                                                                                        portalFrustum,
                                                                                        p.TargetPortal);
                                            // remove the planes added by this portal
                                            portalFrustum.RemovePortalCullingPlanes(p);
                                        }
                                    }
                                }
                                break;
                            case LightType.Directional:
                                // directional have infinite range, so just make sure
                                // the direction is facing the portal
                                if (lightToPortal.Dot(light.DerivedDirection) >= 0.0)
                                {
                                    // if portal is quad portal it must be pointing towards the light
                                    if ((p.Type == PortalType.Quad && lightToPortal.Dot(p.DerivedDirection) < 0.0) ||
                                        (p.Type != PortalType.Quad))
                                    {
                                        if (!light.AffectsZone(targetZone))
                                        {
                                            light.AddZoneToAffectedZonesList(targetZone);
                                            if (targetZone.LastVisibleFrame == frameCount)
                                            {
                                                light.AffectsVisibleZone = true;
                                            }
                                            // set culling frustum from the portal
                                            portalFrustum.AddPortalCullingPlanes(p);
                                            // recurse into the target zone of the portal
                                            p.TargetZone.CheckLightAgainstPortals(light,
                                                                                        frameCount,
                                                                                        portalFrustum,
                                                                                        p.TargetPortal);
                                            // remove the planes added by this portal
                                            portalFrustum.RemovePortalCullingPlanes(p);
                                        }
                                    }
                                }
                                break;
                            case LightType.Spotlight:
                                // spotlights - just check if within illumination range
                                // Technically, we should check if the portal is within
                                // the cone of illumination, but for now, we'll leave that
                                // as a future optimization.
                                if (lightToPortal.Length <= light.AttenuationRange)
                                {
                                    // if portal is quad portal it must be pointing towards the light
                                    if ((p.Type == PortalType.Quad && lightToPortal.Dot(p.DerivedDirection) < 0.0) ||
                                        (p.Type != PortalType.Quad))
                                    {
                                        if (!light.AffectsZone(targetZone))
                                        {
                                            light.AddZoneToAffectedZonesList(targetZone);
                                            if (targetZone.LastVisibleFrame == frameCount)
                                            {
                                                light.AffectsVisibleZone = true;
                                            }
                                            // set culling frustum from the portal
                                            portalFrustum.AddPortalCullingPlanes(p);
                                            // recurse into the target zone of the portal
                                            p.TargetZone.CheckLightAgainstPortals(light,
                                                                                        frameCount,
                                                                                        portalFrustum,
                                                                                        p.TargetPortal);
                                            // remove the planes added by this portal
                                            portalFrustum.RemovePortalCullingPlanes(p);
                                        }
                                    }
                                }
                                break;
                        }
                    }
                }
            }
        }

        public override void UpdatePortalsZoneData()
        {
            List<Portal> transferPortalList = new List<Portal>();
            // check each portal to see if it's intersecting another portal of greater size
            foreach (Portal p in Portals)
            {
                Real pRadius = p.Radius;
                // First we check against portals in the SAME zone (and only if they have a
                // target zone different from the home zone)
                foreach (Portal p2 in Portals)
                {
                    // only check against bigger portals (this will also prevent checking against self)
                    // and only against portals which point to another zone
                    if (pRadius < p2.Radius &&
                        p2.TargetZone != this)
                    {
                        // Portal#2 is bigger than Portal1, check for crossing
                        if (p.CrossedPortal(p2))
                        {
                            // portal#1 crossed portal#2 - flag portal#1 to be moved to portal#2's target zone
                            p.NewHomeZone = p2.TargetZone;
                            transferPortalList.Add(p);
                            break;
                        }
                    }
                }

                // Second we check against portals in the target zone (and only if that target
                // zone is different from the home zone)
                PCZone tzone = p.TargetZone;
                if (tzone != this)
                {
                    foreach (Portal p3 in Portals)
                    {
                        // only check against bigger portals
                        if (pRadius < p3.Radius)
                        {
                            // Portal#3 is bigger than Portal#1, check for crossing
                            if (p.CrossedPortal(p3) &&
                                p.CurrentHomeZone != p3.TargetZone)
                            {
                                // Portal#1 crossed Portal#3 - switch target zones for Portal#1
                                p.TargetZone = p3.TargetZone;
                                break;
                            }
                        }
                    }
                }
            }
            // transfer any portals to new zones that have been flagged
            foreach (Portal p in transferPortalList)
            {
                if (null != p.NewHomeZone)
                {
                    RemovePortal(p);
                    p.NewHomeZone.AddPortal(p);
                    p.NewHomeZone = null;
                }
            }
            transferPortalList.Clear();
        }

        public override PCZone UpdateNodeHomeZone(PCZSceneNode pczsn, bool allowBackTouches)
        {
            // default to newHomeZone being the current home zone
            PCZone newHomeZone = pczsn.HomeZone;

            // Check all portals of the start zone for crossings!
            foreach (Portal portal in Portals)
            {
                PortalIntersectResult pir = portal.Intersects(pczsn);
                switch (pir)
                {
                    default:
                    case PortalIntersectResult.NoIntersect: // node does not intersect portal - do nothing
                    case PortalIntersectResult.IntersectNoCross:
                        // node intersects but does not cross portal - do nothing
                        break;
                    case PortalIntersectResult.IntersectBackNoCross: // node intersects but on the back of the portal
                        if (allowBackTouches)
                        {
                            // node is on wrong side of the portal - fix if we're allowing backside touches
                            if (portal.TargetZone != this &&
                                portal.TargetZone != pczsn.HomeZone)
                            {
                                // set the home zone of the node to the target zone of the portal
                                pczsn.HomeZone = portal.TargetZone;
                                // continue checking for portal crossings in the new zone
                                newHomeZone = portal.TargetZone.UpdateNodeHomeZone(pczsn, false);
                            }
                        }
                        break;
                    case PortalIntersectResult.IntersectCross:
                        // node intersects and crosses the portal - recurse into that zone as new home zone
                        if (portal.TargetZone != this &&
                            portal.TargetZone != pczsn.HomeZone)
                        {
                            // set the home zone of the node to the target zone of the portal
                            pczsn.HomeZone = portal.TargetZone;
                            // continue checking for portal crossings in the new zone
                            newHomeZone = portal.TargetZone.UpdateNodeHomeZone(pczsn, true);
                        }
                        break;
                }
            }

            // return the new home zone
            return newHomeZone;
        }

        public override void FindVisibleNodes(PCZCamera camera, ref List<PCZSceneNode> visibleNodeList, RenderQueue queue, VisibleObjectsBoundsInfo visibleBounds, bool onlyShadowCasters, bool displayNodes, bool showBoundingBoxes)
        {
            //return immediately if nothing is in the zone.
            if (HomeNodeList.Count == 0 &&
                VisitorNodeList.Count == 0 &&
                Portals.Count == 0)
                return;

            // Else, the zone is automatically assumed to be visible since either
            // it is the camera the zone is in, or it was reached because
            // a connecting portal was deemed visible to the camera.

            // enable sky if called to do so for this zone
            if (HasSky)
            {
                // enable sky
                PCZSM.EnableSky(true);
            }

            // Recursively find visible nodes in the zone
            WalkOctree(camera,
                       ref visibleNodeList,
                       queue,
                       _rootOctree,
                       visibleBounds,
                       false,
                       onlyShadowCasters,
                       displayNodes,
                       showBoundingBoxes);

            FrustumPlane culledBy = new FrustumPlane();

            // Here we merge both portal and antiportal visible to the camera into one list.
            // Then we sort them in the order from nearest to furthest from camera.
            List<PortalBase> sortedPortalList = new List<PortalBase>();
            foreach (AntiPortal portal in AntiPortals)
            {
                if (camera.IsObjectVisible(portal, out culledBy))
                {
                    sortedPortalList.Add(portal);
                }
            }

            foreach (Portal portal in Portals)
            {
                if (camera.IsObjectVisible(portal, out culledBy))
                {
                    sortedPortalList.Add(portal);
                }

            }

            Vector3 cameraOrigin = camera.DerivedPosition;
            PortalSortDistance sorter = new PortalSortDistance();
            sorter.CameraPosition = cameraOrigin;
            sortedPortalList.Sort(sorter);

            // create a standalone frustum for anti portal use.
            // we're doing this instead of using camera because we don't need
            // to do camera frustum check again.
            PCZFrustum antiPortalFrustum = new PCZFrustum();
            antiPortalFrustum.Origin = cameraOrigin;
            antiPortalFrustum.ProjectionType = camera.ProjectionType;

            for (int Index = 0; Index <= sortedPortalList.Count; Index++)
            {
                PortalBase portalBase = sortedPortalList[Index];

                if (portalBase == null) continue;
                if (portalBase.TypeName == "Portal")
                {
                    Portal portal = (Portal)portalBase;
                    int planesAdded = camera.AddPortalCullingPlanes(portal);
                    portal.TargetZone.LastVisibleFrame = LastVisibleFrame;
                    portal.TargetZone.LastVisibleFromCamera = LastVisibleFromCamera;
                    portal.TargetZone.FindVisibleNodes(camera,
                                                          ref visibleNodeList,
                                                          queue,
                                                          visibleBounds,
                                                          onlyShadowCasters,
                                                          displayNodes,
                                                          showBoundingBoxes);

                    if (planesAdded > 0)
                    {
                        camera.RemovePortalCullingPlanes(portal);
                    }
                }
                else if (Index != sortedPortalList.Count)
                {
                    // this is an anti portal. So we use it to test preceding portals in the list.
                    AntiPortal antiPortal = (AntiPortal)portalBase;
                    int planes_added = antiPortalFrustum.AddPortalCullingPlanes(antiPortal);

                    for (int indexRemaining = Index + 1; indexRemaining <= sortedPortalList.Count; indexRemaining++)
                    {
                        PortalBase otherPortal = sortedPortalList[indexRemaining];
                        // Since this is an antiportal, we are doing the inverse of the test.
                        // Here if the portal is fully visible in the anti portal fustrum, it means it's hidden.
                        if (otherPortal != null && antiPortalFrustum.IsFullyVisible(otherPortal))
                            sortedPortalList[indexRemaining] = null;
                    }

                    if (planes_added > 0)
                    {
                        // Then remove the extra culling planes added before going to the next portal in the list.
                        antiPortalFrustum.RemovePortalCullingPlanes(antiPortal);
                    }
                }
            }

        }

        private void WalkOctree(PCZCamera camera,
                                    ref List<PCZSceneNode> visibleNodeList,
                                    RenderQueue queue,
                                    Octree octant,
                                    VisibleObjectsBoundsInfo visibleBounds,
                                    bool foundvisible,
                                    bool onlyShadowCasters,
                                    bool displayNodes,
                                    bool showBoundingBoxes)
        {

            //return immediately if nothing is in the node.
            if (octant.NodeList.Count == 0)
                return;

            Visibility v = Visibility.None;

            if (foundvisible)
            {
                v = Visibility.Full;
            }

            else if (octant == _rootOctree)
            {
                v = Visibility.Partial;
            }

            else
            {
                AxisAlignedBox box = octant.Box;

                v = camera.GetVisibility(box);
            }


            // if the octant is visible, or if it's the root node...
            if (v != Visibility.None)
            {
                //Add stuff to be rendered;

                bool vis = true;

                foreach (PCZSceneNode sn in octant.NodeList.Values)
                {
                    // if the scene node is already visible, then we can skip it
                    if (sn.LastVisibleFrame != LastVisibleFrame ||
                        sn.LastVisibleFromCamera != camera)
                    {
                        // if this octree is partially visible, manually cull all
                        // scene nodes attached directly to this level.
                        if (v == Visibility.Partial)
                        {
                            vis = camera.IsObjectVisible(sn.WorldAABB);
                        }
                        if (vis)
                        {
                            // add the node to the render queue
                            sn.AddToRenderQueue(camera, queue, onlyShadowCasters, visibleBounds);
                            // add it to the list of visible nodes
                            visibleNodeList.Add(sn);
                            // if we are displaying nodes, add the node renderable to the queue
                            if (displayNodes)
                            {
                                queue.AddRenderable(sn.GetDebugRenderable());
                            }
                            // if the scene manager or the node wants the bounding box shown, add it to the queue
                            if (sn.ShowBoundingBox || showBoundingBoxes)
                            {
                                sn.AddBoundingBoxToQueue(queue);
                            }
                            // flag the node as being visible this frame
                            sn.LastVisibleFrame = LastVisibleFrame;
                            sn.LastVisibleFromCamera = camera;
                        }
                    }
                }

                Octree child;
                bool childfoundvisible = (v == Visibility.Full);
                if ((child = octant.Children[0, 0, 0]) != null)
                    WalkOctree(camera, ref visibleNodeList, queue, child, visibleBounds, childfoundvisible,
                               onlyShadowCasters, displayNodes, showBoundingBoxes);

                if ((child = octant.Children[1, 0, 0]) != null)
                    WalkOctree(camera, ref visibleNodeList, queue, child, visibleBounds, childfoundvisible,
                               onlyShadowCasters, displayNodes, showBoundingBoxes);

                if ((child = octant.Children[0, 1, 0]) != null)
                    WalkOctree(camera, ref visibleNodeList, queue, child, visibleBounds, childfoundvisible,
                               onlyShadowCasters, displayNodes, showBoundingBoxes);

                if ((child = octant.Children[1, 1, 0]) != null)
                    WalkOctree(camera, ref visibleNodeList, queue, child, visibleBounds, childfoundvisible,
                               onlyShadowCasters, displayNodes, showBoundingBoxes);

                if ((child = octant.Children[0, 0, 1]) != null)
                    WalkOctree(camera, ref visibleNodeList, queue, child, visibleBounds, childfoundvisible,
                               onlyShadowCasters, displayNodes, showBoundingBoxes);

                if ((child = octant.Children[1, 0, 1]) != null)
                    WalkOctree(camera, ref visibleNodeList, queue, child, visibleBounds, childfoundvisible,
                               onlyShadowCasters, displayNodes, showBoundingBoxes);

                if ((child = octant.Children[0, 1, 1]) != null)
                    WalkOctree(camera, ref visibleNodeList, queue, child, visibleBounds, childfoundvisible,
                               onlyShadowCasters, displayNodes, showBoundingBoxes);

                if ((child = octant.Children[1, 1, 1]) != null)
                    WalkOctree(camera, ref visibleNodeList, queue, child, visibleBounds, childfoundvisible,
                               onlyShadowCasters, displayNodes, showBoundingBoxes);

            }
        }


        public override void FindNodes(AxisAlignedBox t, ref List<PCZSceneNode> list, List<Portal> visitedPortals, bool includeVisitors, bool recurseThruPortals, PCZSceneNode exclude)
        {

            // if this zone has an enclosure, check against the enclosure AABB first
            if (null != EnclosureNode)
            {
                if (!EnclosureNode.WorldAABB.Intersects(t))
                {
                    // AABB of zone does not intersect t, just return.
                    return;
                }
            }

            // use the Octree to more efficiently find nodes intersecting the aab
            _rootOctree.FindNodes(t, ref list, exclude, includeVisitors, false);


            // if asked to, recurse through portals
            if (recurseThruPortals)
            {
                foreach (Portal portal in Portals)
                    // check portal versus bounding box
                    if (portal.Intersects(t))
                    {
                        // make sure portal hasn't already been recursed through

                        if (!visitedPortals.Contains(portal))
                        {
                            // save portal to the visitedPortals list
                            visitedPortals.Add(portal);
                            // recurse into the connected zone
                            portal.TargetZone.FindNodes(t,
                                                                ref list,
                                                                visitedPortals,
                                                                includeVisitors,
                                                                recurseThruPortals,
                                                                exclude);
                        }
                    }
            }
        }


        public void UpdateNodeOctant(OctreeZoneData zoneData)
        {
            AxisAlignedBox box = zoneData.OctreeWorldAABB;

            if (box.IsNull)
                return;

            // Skip if octree has been destroyed (shutdown conditions)
            if (null == _rootOctree)
                return;

            PCZSceneNode node = zoneData.AssociatedNode;
            if (null == zoneData.Octant)
            {
                //if outside the octree, force into the root node.
                if (!zoneData._isIn(_rootOctree.Box))
                    _rootOctree.AddNode(node);
                else
                    AddNodeToOctree(node, _rootOctree, 0);
                return;
            }

            if (!zoneData._isIn(zoneData.Octant.Box))
            {

                //if outside the octree, force into the root node.
                if (!zoneData._isIn(_rootOctree.Box))
                {
                    // skip if it's already in the root node.
                    if (((OctreeZoneData)node.GetZoneData(this)).Octant == _rootOctree)
                        return;

                    RemoveNodeFromOctree(node);
                    _rootOctree.AddNode(node);
                }
                else
                    AddNodeToOctree(node, _rootOctree, 0);
            }
        }

        /** Only removes the node from the octree.  It leaves the octree, even if it's empty.
        */
        public void RemoveNodeFromOctree(PCZSceneNode n)
        {
            // Skip if octree has been destroyed (shutdown conditions)
            if (null == _rootOctree)
                return;

            Octree oct = ((OctreeZoneData)n.GetZoneData(this)).Octant;

            if (null != oct)
            {
                oct.RemoveNode(n);
            }

            ((OctreeZoneData)n.GetZoneData(this)).Octant = null;
        }


        void AddNodeToOctree(PCZSceneNode n, Octree octant, int depth)
        {

            // Skip if octree has been destroyed (shutdown conditions)
            if (null == _rootOctree)
                return;

            AxisAlignedBox bx = n.WorldAABB;


            //if the octree is twice as big as the scene node,
            //we will add it to a child.
            if ((depth < _maxDepth) && octant.IsTwiceSize(bx))
            {
                int x = 0, y = 0, z = 0;
                octant._GetChildIndexes(bx, ref x, ref y, ref z);

                if (octant.Children[x, y, z] == null)
                {
                    octant.Children[x, y, z] = new Octree(this, octant);
                    Vector3 octantMin = octant.Box.Minimum;
                    Vector3 octantMax = octant.Box.Maximum;
                    Vector3 min, max;

                    if (x == 0)
                    {
                        min.x = octantMin.x;
                        max.x = (octantMin.x + octantMax.x) / 2;
                    }

                    else
                    {
                        min.x = (octantMin.x + octantMax.x) / 2;
                        max.x = octantMax.x;
                    }

                    if (y == 0)
                    {
                        min.y = octantMin.y;
                        max.y = (octantMin.y + octantMax.y) / 2;
                    }

                    else
                    {
                        min.y = (octantMin.y + octantMax.y) / 2;
                        max.y = octantMax.y;
                    }

                    if (z == 0)
                    {
                        min.z = octantMin.z;
                        max.z = (octantMin.z + octantMax.z) / 2;
                    }

                    else
                    {
                        min.z = (octantMin.z + octantMax.z) / 2;
                        max.z = octantMax.z;
                    }

                    octant.Children[x, y, z].Box.SetExtents(min, max);
                    octant.Children[x, y, z].HalfSize = (max - min) / 2;
                }

                AddNodeToOctree(n, octant.Children[x, y, z], ++depth);

            }
            else
            {
                if (((OctreeZoneData)n.GetZoneData(this)).Octant == octant)
                    return;

                RemoveNodeFromOctree(n);
                octant.AddNode(n);
            }
        }


        public override void FindNodes(Sphere t, ref List<PCZSceneNode> list, List<Portal> visitedPortals, bool includeVisitors, bool recurseThruPortals, PCZSceneNode exclude)
        {
            // if this zone has an enclosure, check against the enclosure AABB first
            if (null != EnclosureNode)
            {
                if (!EnclosureNode.WorldAABB.Intersects(t))
                {
                    // AABB of zone does not intersect t, just return.
                    return;
                }
            }

            // use the Octree to more efficiently find nodes intersecting the sphere
            _rootOctree.FindNodes(t, ref list, exclude, includeVisitors, false);

            // if asked to, recurse through portals
            if (recurseThruPortals)
            {
                foreach (Portal portal in Portals)
                {
                    // check portal versus bounding box
                    if (portal.Intersects(t))
                    {
                        // make sure portal hasn't already been recursed through
                        if (!visitedPortals.Contains(portal))
                        {
                            // save portal to the visitedPortals list
                            visitedPortals.Add(portal);
                            // recurse into the connected zone
                            portal.TargetZone.FindNodes(t,
                                                              ref list,
                                                              visitedPortals,
                                                              includeVisitors,
                                                              recurseThruPortals,
                                                              exclude);
                        }
                    }
                }
            }
        }

        public override void FindNodes(PlaneBoundedVolume t, ref List<PCZSceneNode> list, List<Portal> visitedPortals, bool includeVisitors, bool recurseThruPortals, PCZSceneNode exclude)
        {
            // if this zone has an enclosure, check against the enclosure AABB first
            if (null != EnclosureNode)
            {
                if (!t.Intersects(EnclosureNode.WorldAABB))
                {
                    // AABB of zone does not intersect t, just return.
                    return;
                }
            }

            // use the Octree to more efficiently find nodes intersecting the plane bounded volume
            _rootOctree.FindNodes(t, ref list, exclude, includeVisitors, false);

            // if asked to, recurse through portals
            if (recurseThruPortals)
            {
                foreach (Portal portal in Portals)
                {
                    // check portal versus bounding box
                    if (portal.Intersects(t))
                    {
                        // make sure portal hasn't already been recursed through

                        if (!visitedPortals.Contains(portal))
                        {
                            // save portal to the visitedPortals list
                            visitedPortals.Add(portal);
                            // recurse into the connected zone
                            portal.TargetZone.FindNodes(t,
                                                              ref list,
                                                              visitedPortals,
                                                              includeVisitors,
                                                              recurseThruPortals,
                                                              exclude);
                        }
                    }
                }
            }

        }

        public override void FindNodes(Ray t, ref List<PCZSceneNode> list, List<Portal> visitedPortals, bool includeVisitors, bool recurseThruPortals, PCZSceneNode exclude)
        {
            // if this zone has an enclosure, check against the enclosure AABB first
            if (null != EnclosureNode)
            {
                IntersectResult nsect = t.Intersects(EnclosureNode.WorldAABB);
                if (!nsect.Hit)
                {
                    // AABB of zone does not intersect t, just return.
                    return;
                }
            }

            // use the Octree to more efficiently find nodes intersecting the ray
            _rootOctree.FindNodes(t, ref list, exclude, includeVisitors, false);

            // if asked to, recurse through portals
            if (recurseThruPortals)
            {
                foreach (Portal portal in Portals)
                {
                    // check portal versus bounding box
                    if (portal.Intersects(t))
                    {
                        // make sure portal hasn't already been recursed through

                        if (!visitedPortals.Contains(portal))
                        {
                            // save portal to the visitedPortals list
                            visitedPortals.Add(portal);
                            // recurse into the connected zone
                            portal.TargetZone.FindNodes(t,
                                                              ref list,
                                                              visitedPortals,
                                                              includeVisitors,
                                                              recurseThruPortals,
                                                              exclude);
                        }
                    }
                }
            }

        }

        public override bool SetOption(string key, object value)
        {
            if (key == "Size")
            {
                Resize((AxisAlignedBox)value);
                return true;
            }

            if (key == "Depth")
            {
                _maxDepth = (int)value;
                // copy the box since resize will delete mOctree and reference won't work
                AxisAlignedBox box = _rootOctree.Box;
                Resize(box);
                return true;
            }

            /*		else if ( key == "ShowOctree" )
                    {
                        mShowBoxes = * static_cast < const bool * > ( val );
                        return true;
                    }*/
            return false;
        }

        public override void NotifyCameraCreated(Camera c)
        {
        }

        public override void NotifyBeginRenderScene()
        {
        }

        public override void SetZoneGeometry(string filename, PCZSceneNode parentNode)
        {
            string entityName, nodeName;
            entityName = this.Name + "_entity";
            nodeName = this.Name + "_Node";
            Entity ent = PCZSM.CreateEntity(entityName, filename);
            // create a node for the entity
            PCZSceneNode node;
            node = (PCZSceneNode)parentNode.CreateChildSceneNode(nodeName);
            // attach the entity to the node
            node.AttachObject(ent);
            // set the node as the enclosure node
            EnclosureNode = node;
        }

        //public override void GetAABB(ref AxisAlignedBox aabb)
        //{
        //    // get the Octree bounding box
        //    aabb = rootOctree.Box;
        //}

        /** create zone specific data for a node
        */
        public void CreateNodeZoneData(PCZSceneNode node)
        {
            OctreeZoneData ozd = new OctreeZoneData(node, this);
            node.SetZoneData(this, ozd);
        }

        public override void DirtyNodeByMovingPortals()
        {
            foreach (Portal portal in Portals)
            {
                if (portal.NeedUpdate)
                {
                    List<PCZSceneNode> nodeList = new List<PCZSceneNode>();
                    _rootOctree.FindNodes(portal.getAAB(), ref nodeList, null, true, false);
                    foreach (PCZSceneNode pczSceneNode in nodeList)
                    {
                        pczSceneNode.Moved = true;
                    }

                }
            }
        }
    }

    public class OctreeZoneFactory : PCZoneFactory
    {
        public OctreeZoneFactory(string typeName)
            : base(typeName)
        {
            _factoryTypeName = typeName;
        }

        public override bool SupportsPCZoneType(string zoneType)
        {
            return zoneType == _factoryTypeName;
        }

        public override PCZone CreatePCZone(PCZSceneManager pczsm, string zoneName)
        {
            return new OctreeZone(pczsm, zoneName);
        }
    }
}