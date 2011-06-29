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
using System.Text;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Math;
using Axiom.Math.Collections;

#endregion Namespace Declarations

namespace Axiom.SceneManagers.PortalConnected
{
    /// <summary>
    /// Default Instance of PCZone
    /// </summary>
    public class DefaultZone : PCZone
    {

        /// <summary>
        /// Name Generator 
        /// </summary>
        private static NameGenerator<DefaultZone> _nameGenerator = new NameGenerator<DefaultZone>("DefaultZone");


        /// <summary>
        /// Default Constructor
        /// </summary>
        /// <param name="creator"></param>
        public DefaultZone(PCZSceneManager creator)
            : this(creator, _nameGenerator.GetNextUniqueName())
        {
        }

        /// <summary>
        /// Constructor with specific name
        /// </summary>
        /// <param name="creator">PCZSceneManager</param>
        /// <param name="name">string</param>
        public DefaultZone(PCZSceneManager creator, string name)
            : base(creator, name)
        {
            ZoneTypeName = "ZoneType_Default";
        }

        /// <summary>
        /// Set the enclosure node for this Zone
        /// </summary>
        public override PCZSceneNode EnclosureNode
        {
            get { return base.EnclosureNode; }
            set
            {
                base.EnclosureNode = value;
                // anchor the node to this zone
                base.EnclosureNode.AnchorToHomeZone(this);
            }
        }

        /// <summary>
        /// this call adds the given node to either the zone's list
        /// of nodes at home in the zone, or to the list of visiting nodes
        /// NOTE: The list is decided by the node's homeZone value, so
        /// that must be set correctly before calling this function.
        /// </summary>
        /// <param name="node">PCZSceneNode</param>
        public override void AddNode(PCZSceneNode node)
        {
            if (node.HomeZone == this)
            {
                // add a reference to this node in the "nodes at home in this zone" list
                HomeNodeList.Add(node);
            }
            else
            {
                // add a reference to this node in the "nodes visiting this zone" list
                VisitorNodeList.Add(node);
            }
        }

        /// <summary>
        /// Removes the node
        /// </summary>
        /// <param name="node">PCZSceneNode</param>
        public override void RemoveNode(PCZSceneNode node)
        {
            if (node.HomeZone == this)
            {
                HomeNodeList.Remove(node);
            }
            else
            {
                VisitorNodeList.Remove(node);
            }
        }
        
        /// <summary>
        /// Indicates whether or not this zone requires zone-specific data for
        /// each scene node
        /// </summary>
        public override bool RequiresZoneSpecificNodeData
        {
            get
            {
                // regular DefaultZones don't require any zone specific node data
                return false;
            }
        }

        /// <summary>
        /// Recursively check for intersection of the given scene node
        /// with zone portals.  If the node touches a portal, then the
        /// connected zone is assumed to be touched.  The zone adds
        /// the node to its node list and the node adds the zone to
        /// its visiting zone list.
        ///     NOTE: This function assumes that the home zone of the node
        ///             is correct.  The function "_updateHomeZone" in PCZSceneManager
        /// takes care of this and should have been called before
        /// this function.
        /// </summary>
        /// <param name="pczsn">PCZSceneNode</param>
        /// <param name="ignorePortal">Portal</param>
        public override void CheckNodeAgainstPortals(PCZSceneNode pczsn, Portal ignorePortal)
        {
            if (pczsn == EnclosureNode ||
                pczsn.AllowedToVisit == false)
            {
                // don't do any checking of enclosure node versus portals
                return;
            }

            PCZone connectedZone;
            foreach (Portal portal in Portals)
            {
                if (portal != ignorePortal && portal.Intersects(pczsn) != PortalIntersectResult.NoIntersect)
                {
                    connectedZone = portal.TargetZone;

                    if (connectedZone != pczsn.HomeZone &&
                        !pczsn.IsVisitingZone(connectedZone))
                    {
                        pczsn.AddZoneToVisitingZonesMap(connectedZone);

                        connectedZone.AddNode(pczsn);

                        connectedZone.CheckNodeAgainstPortals(pczsn, portal.TargetPortal);
                    }
                }
            }
        }

        /// <summary>
        /// (recursive) check the given light against all portals in the zone
        /// NOTE: This is the default implementation, which doesn't take advantage
        ///       of any zone-specific optimizations for checking portal visibility
        /// </summary>
        /// <param name="light">PCZLight</param>
        /// <param name="frameCount">ulong</param>
        /// <param name="portalFrustum">PCZFrustum</param>
        /// <param name="ignorePortal">Portal</param>
        public override void CheckLightAgainstPortals(PCZLight light, ulong frameCount, PCZFrustum portalFrustum, Portal ignorePortal)
        {
            foreach (Portal p in Portals)
            {
                //Portal * p = *it;
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
                                // directional's have infinite range, so just make sure
                                // the direction is facing the portal
                                if (lightToPortal.Dot(light.DerivedDirection) >= 0.0)
                                {
                                    // if portal is quad portal it must be pointing towards the light
                                    if ((p.Type == PortalType.Quad && lightToPortal.Dot(p.DerivedDirection) < 0.0) ||
                                        (p.Type == PortalType.Quad))
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

        /// <summary>
        ///  Update the zone data for the portals in the zone
        ///  NOTE: All portal spatial data must be up-to-date before calling this routine.
        /// </summary>
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
                    if (pRadius < p2.Radius && p2.TargetZone != this)
                    {
                        // Portal#2 is bigger than Portal1, check for crossing
                        if (p.CrossedPortal(p2) && p.CurrentHomeZone != p2.TargetZone)
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

        /// <summary>
        /// The following function checks if a node has left it's current home zone.
        /// This is done by checking each portal in the zone.  If the node has crossed
        /// the portal, then the current zone is no longer the home zone of the node.  The
        /// function then recurses into the connected zones.  Once a zone is found where
        /// the node does NOT cross out through a portal, that zone is the new home zone.
        /// NOTE: For this function to work, the node must start out in the proper zone to
        ///       begin with!/
        /// </summary>
        /// <param name="pczsn">pczsn</param>
        /// <param name="allowBackTouches">bool</param>
        /// <returns>PCZone</returns>
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
                    case PortalIntersectResult.IntersectNoCross:// node Intersects but does not cross portal - do nothing
                        break;
                    case PortalIntersectResult.IntersectBackNoCross:// node Intersects but on the back of the portal
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
                        // node Intersects and crosses the portal - recurse into that zone as new home zone
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

        /// <summary>
        /// Recursively walk the zones, adding all visible SceneNodes to the list of visible nodes.
        /// </summary>
        /// <param name="camera">PCZCamera</param>
        /// <param name="visibleNodeList">ref List<PCZSceneNode></param>
        /// <param name="queue">RenderQueue</param>
        /// <param name="visibleBounds">VisibleObjectsBoundsInfo</param>
        /// <param name="onlyShadowCasters">bool</param>
        /// <param name="displayNodes">bool</param>
        /// <param name="showBoundingBoxes">bool</param>
        public override void FindVisibleNodes(PCZCamera camera,
                                      ref List<PCZSceneNode> visibleNodeList,
                                      RenderQueue queue,
                                      VisibleObjectsBoundsInfo visibleBounds,
                                      bool onlyShadowCasters,
                                      bool displayNodes,
                                      bool showBoundingBoxes)
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

            // find visible nodes at home in the zone
            bool vis;

            foreach (PCZSceneNode pczsn in HomeNodeList)
            {
                //PCZSceneNode pczsn = *it;
                // if the scene node is already visible, then we can skip it
                if (pczsn.LastVisibleFrame != LastVisibleFrame ||
                    pczsn.LastVisibleFromCamera != camera)
                {
                    FrustumPlane fPlane;
                    // for a scene node, check visibility using AABB
                    vis = camera.IsObjectVisible(pczsn.WorldAABB, out fPlane);
                    if (vis)
                    {
                        // add it to the list of visible nodes
                        visibleNodeList.Add(pczsn);
                        // add the node to the render queue
                        pczsn.AddToRenderQueue(camera, queue, onlyShadowCasters, visibleBounds);
                        // if we are displaying nodes, add the node renderable to the queue
                        if (displayNodes)
                        {
                            queue.AddRenderable(pczsn.GetDebugRenderable());
                        }
                        // if the scene manager or the node wants the bounding box shown, add it to the queue
                        if (pczsn.ShowBoundingBox || showBoundingBoxes)
                        {
                            pczsn.AddBoundingBoxToQueue(queue);
                        }
                        // flag the node as being visible this frame
                        pczsn.LastVisibleFrame = LastVisibleFrame;
                        pczsn.LastVisibleFromCamera = camera;
                    }
                }
            }
            // find visible visitor nodes

            foreach (PCZSceneNode pczsn in VisitorNodeList)
            {
                // if the scene node is already visible, then we can skip it
                if (pczsn.LastVisibleFrame != LastVisibleFrame ||
                    pczsn.LastVisibleFromCamera != camera)
                {
                    FrustumPlane fPlane;
                    // for a scene node, check visibility using AABB
                    vis = camera.IsObjectVisible(pczsn.WorldAABB, out fPlane);
                    if (vis)
                    {
                        // add it to the list of visible nodes
                        visibleNodeList.Add(pczsn);
                        // add the node to the render queue
                        pczsn.AddToRenderQueue(camera, queue, onlyShadowCasters, visibleBounds);
                        // if we are displaying nodes, add the node renderable to the queue
                        if (displayNodes)
                        {
                            queue.AddRenderable(pczsn.GetDebugRenderable());
                        }
                        // if the scene manager or the node wants the bounding box shown, add it to the queue
                        if (pczsn.ShowBoundingBox || showBoundingBoxes)
                        {
                            pczsn.AddBoundingBoxToQueue(queue);
                        }
                        // flag the node as being visible this frame
                        pczsn.LastVisibleFrame = LastVisibleFrame;
                        pczsn.LastVisibleFromCamera = camera;
                    }
                }
            }

            // find visible portals in the zone and recurse into them
            foreach (Portal portal in Portals)
            {
                FrustumPlane fPlane;
                // for portal, check visibility using world bounding sphere & direction
                vis = camera.IsVisible(portal, out fPlane);
                if (vis)
                {
                    // portal is visible. Add the portal as extra culling planes to camera
                    int planes_added = camera.AddPortalCullingPlanes(portal);
                    // tell target zone it's visible this frame
                    portal.TargetZone.LastVisibleFrame = LastVisibleFrame;
                    portal.TargetZone.LastVisibleFromCamera = camera;
                    // recurse into the connected zone
                    portal.TargetZone.FindVisibleNodes(camera,
                                                              ref visibleNodeList,
                                                              queue,
                                                              visibleBounds,
                                                              onlyShadowCasters,
                                                              displayNodes,
                                                              showBoundingBoxes);
                    if (planes_added > 0)
                    {
                        // Then remove the extra culling planes added before going to the next portal in this zone.
                        camera.RemovePortalCullingPlanes(portal);
                    }
                }
            }
        }

        /// <summary>
        /// find nodes which intersect various types of BV's
        /// </summary>
        /// <param name="t">AxisAlignedBox</param>
        /// <param name="list">ref List<PCZSceneNode></param>
        /// <param name="visitedPortals">List<Portal></param>
        /// <param name="includeVisitors">bool</param>
        /// <param name="recurseThruPortals">bool</param>
        /// <param name="exclude">PCZSceneNode</param>
        public override void FindNodes(AxisAlignedBox t,
                                      ref List<PCZSceneNode> list,
                                      List<Portal> visitedPortals,
                                      bool includeVisitors,
                                      bool recurseThruPortals,
                                      PCZSceneNode exclude)
        {
            // if this zone has an enclosure, check against the enclosure AABB first
            if (null != base.EnclosureNode)
            {
                if (!base.EnclosureNode.WorldAABB.Intersects(t))
                {
                    // AABB of zone does not intersect t, just return.
                    return;
                }
            }

            foreach (PCZSceneNode pczsn in HomeNodeList)
            {
                if (pczsn != exclude)
                {
                    // make sure node is not already in the list (might have been added in another
                    // zone it was visiting)
                    if (!list.Contains(pczsn))
                    {
                        bool nsect = t.Intersects(pczsn.WorldAABB);
                        if (nsect)
                        {
                            list.Add(pczsn);
                        }
                    }
                }
            }

            if (includeVisitors)
            {
                // check visitor nodes
                foreach (PCZSceneNode pczsn in VisitorNodeList)
                {
                    if (pczsn != exclude)
                    {
                        // make sure node is not already in the list (might have been added in another
                        // zone it was visiting)
                        if (!list.Contains(pczsn))
                        {
                            bool nsect = t.Intersects(pczsn.WorldAABB);
                            if (nsect)
                            {
                                list.Add(pczsn);
                            }
                        }
                    }
                }
            }

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

        /// <summary>
        /// find nodes which intersect various types of BV's
        /// </summary>
        /// <param name="t">Sphere</param>
        /// <param name="list">ref List<PCZSceneNode></param>
        /// <param name="visitedPortals">List<Portal></param>
        /// <param name="includeVisitors">bool</param>
        /// <param name="recurseThruPortals">bool</param>
        /// <param name="exclude">PCZSceneNode</param>
        public override void FindNodes(Sphere t,
                                     ref List<PCZSceneNode> list,
                                     List<Portal> visitedPortals,
                                     bool includeVisitors,
                                     bool recurseThruPortals,
                                     PCZSceneNode exclude)
        {
            // if this zone has an enclosure, check against the enclosure AABB first
            if (null != base.EnclosureNode)
            {
                if (!base.EnclosureNode.WorldAABB.Intersects(t))
                {
                    // AABB of zone does not intersect t, just return.
                    return;
                }
            }

            // check nodes at home in this zone
            foreach (PCZSceneNode pczsn in HomeNodeList)
            {
                if (pczsn != exclude)
                {
                    // make sure node is not already in the list (might have been added in another
                    // zone it was visiting)
                    if (!list.Contains(pczsn))
                    {
                        bool nsect = t.Intersects(pczsn.WorldAABB);
                        if (nsect)
                        {
                            list.Add(pczsn);
                        }
                    }
                }
            }

            if (includeVisitors)
            {
                // check visitor nodes
                foreach (PCZSceneNode pczsn in VisitorNodeList)
                {
                    if (pczsn != exclude)
                    {
                        // make sure node is not already in the list (might have been added in another
                        // zone it was visiting)
                        if (!list.Contains(pczsn))
                        {
                            bool nsect = t.Intersects(pczsn.WorldAABB);
                            if (nsect)
                            {
                                list.Add(pczsn);
                            }
                        }
                    }
                }
            }

            // if asked to, recurse through portals
            if (recurseThruPortals)
            {
                foreach (Portal portal in Portals)
                {
                    // check portal versus boundign box
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

        /// <summary>
        /// find nodes which intersect various types of BV's
        /// </summary>
        /// <param name="t">PlaneBoundedVolume</param>
        /// <param name="list">ref List<PCZSceneNode></param>
        /// <param name="visitedPortals">List<Portal></param>
        /// <param name="includeVisitors">bool</param>
        /// <param name="recurseThruPortals">bool</param>
        /// <param name="exclude">PCZSceneNode</param>
        public override void FindNodes(PlaneBoundedVolume t,
                                      ref List<PCZSceneNode> list,
                                      List<Portal> visitedPortals,
                                      bool includeVisitors,
                                      bool recurseThruPortals,
                                      PCZSceneNode exclude)
        {
            // if this zone has an enclosure, check against the enclosure AABB first
            if (null != base.EnclosureNode)
            {
                if (!t.Intersects(base.EnclosureNode.WorldAABB))
                {
                    // AABB of zone does not intersect t, just return.
                    return;
                }
            }

            // check nodes at home in this zone
            foreach (PCZSceneNode pczsn in HomeNodeList)
            {
                if (pczsn != exclude)
                {
                    // make sure node is not already in the list (might have been added in another
                    // zone it was visiting)
                    if (!list.Contains(pczsn))
                    {
                        bool nsect = t.Intersects(pczsn.WorldAABB);
                        if (nsect)
                        {
                            list.Add(pczsn);
                        }
                    }
                }
            }

            if (includeVisitors)
            {
                // check visitor nodes
                foreach (PCZSceneNode pczsn in VisitorNodeList)
                {
                    if (pczsn != exclude)
                    {
                        // make sure node is not already in the list (might have been added in another
                        // zone it was visiting)
                        if (!list.Contains(pczsn))
                        {
                            bool nsect = t.Intersects(pczsn.WorldAABB);
                            if (nsect)
                            {
                                list.Add(pczsn);
                            }
                        }
                    }
                }
            }

            // if asked to, recurse through portals
            if (recurseThruPortals)
            {
                foreach (Portal portal in Portals)
                {
                    // check portal versus boundign box
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


        /// <summary>
        /// find nodes which intersect various types of BV's
        /// </summary>
        /// <param name="t">Ray</param>
        /// <param name="list">ref List<PCZSceneNode></param>
        /// <param name="visitedPortals">List<Portal></param>
        /// <param name="includeVisitors">bool</param>
        /// <param name="recurseThruPortals">bool</param>
        /// <param name="exclude">PCZSceneNode</param>
        public override void FindNodes(Ray t,
                                      ref List<PCZSceneNode> list,
                                      List<Portal> visitedPortals,
                                      bool includeVisitors,
                                      bool recurseThruPortals,
                                      PCZSceneNode exclude)
        {
            // if this zone has an enclosure, check against the enclosure AABB first
            if (null != base.EnclosureNode)
            {
                IntersectResult nsect = t.Intersects(base.EnclosureNode.WorldAABB);
                if (!nsect.Hit)
                {
                    // AABB of zone does not intersect t, just return.
                    return;
                }
            }

            // check nodes at home in this zone
            foreach (PCZSceneNode pczsn in HomeNodeList)
            {
                if (pczsn != exclude)
                {
                    // make sure node is not already in the list (might have been added in another
                    // zone it was visiting)
                    if (!list.Contains(pczsn))
                    {
                        IntersectResult nsect = t.Intersects(pczsn.WorldAABB);
                        if (nsect.Hit)
                        {
                            list.Add(pczsn);
                        }
                    }
                }
            }

            if (includeVisitors)
            {
                // check visitor nodes
                foreach (PCZSceneNode pczsn in VisitorNodeList)
                {
                    if (pczsn != exclude)
                    {
                        // make sure node is not already in the list (might have been added in another
                        // zone it was visiting)

                        if (!list.Contains(pczsn))
                        {
                            IntersectResult nsect = t.Intersects(pczsn.WorldAABB);
                            if (nsect.Hit)
                            {
                                list.Add(pczsn);
                            }
                        }
                    }
                }
            }

            // if asked to, recurse through portals
            if (recurseThruPortals)
            {
                foreach (Portal portal in Portals)
                {
                    // check portal versus boundign box
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

        /// <summary>
        /// Mark nodes dirty base on moving portals.
        /// </summary>
        public override void DirtyNodeByMovingPortals()
        {
            // default zone has no space partitioning also.
            // So it's impractical to do any AABB to find node of interest by portals.
            // Hence for this case, we just mark all nodes as dirty as long as there's
            // any moving portal within the zone.

            foreach (Portal portal in Portals)
            {
                if (portal.NeedUpdate)
                {
                    portal.Moved = true;

                    foreach (PCZSceneNode node in HomeNodeList)
                    {
                        node.Moved = true;
                    }

                    foreach (PCZSceneNode node in VisitorNodeList)
                    {
                        node.Moved = true;
                    }

                }
            }
        }

        /// <summary>
        /// Set option for the zone
        /// </summary>
        /// <param name="name">string</param>
        /// <param name="value">object</param>
        /// <returns>bool</returns>
        /// <remarks>Does nothing at the moment</remarks>
        public override bool SetOption(string name, object value)
        {
            return false;
        }

        /// <summary>
        /// called when the scene manager creates a camera because
        /// some zone managers (like TerrainZone) need the camera info.
        /// </summary>
        /// <param name="c">Camera</param>
        public override void NotifyCameraCreated(Camera c)
        {
        }

        /// <summary>
        /// NotifyBeginRenderScene
        /// </summary>
        /// <remarks>Does nothing at the moment</remarks>
        public override void NotifyBeginRenderScene()
        {
        }


        /// <summary>
        /// NotifyWorldGeometryRenderQueue
        /// </summary>
        /// <param name="renderQueueGroupID"></param>
        public override void NotifyWorldGeometryRenderQueue(RenderQueueGroupID renderQueueGroupID)
        {
            
        }

        /// <summary>
        /// SetZoneGeometry
        /// </summary>
        /// <param name="filename">string</param>
        /// <param name="parentNode">PCZSceneNode</param>
        public override void SetZoneGeometry(string filename, PCZSceneNode parentNode)
        {
            String entityName, nodeName;
            entityName = this.Name + "_entity";
            nodeName = this.Name + "_Node";
            Entity ent = PCZSM.CreateEntity(entityName, filename);
            // create a node for the entity
            PCZSceneNode node;
            node = (PCZSceneNode)(parentNode.CreateChildSceneNode(nodeName, Vector3.Zero, Quaternion.Identity));
            // attach the entity to the node
            node.AttachObject(ent);
            // set the node as the enclosure node
            EnclosureNode = node;
        }

    }
}