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
using Axiom.Graphics;
using Axiom.Math;
using Axiom.Math.Collections;

#endregion Namespace Declarations

namespace Axiom.SceneManagers.PortalConnected
{
    public class DefaultZone : PCZone
    {
        public DefaultZone(PCZSceneManager creator, string name)
            : base(creator, name)
        {
            mZoneTypeName = "ZoneType_Default";
        }

        /** Set the enclosure node for this Zone
		*/

        public override void SetEnclosureNode(PCZSceneNode node)
        {
            mEnclosureNode = node;
            // anchor the node to this zone
            node.AnchorToHomeZone(this);
        }

        // this call adds the given node to either the zone's list
        // of nodes at home in the zone, or to the list of visiting nodes
        // NOTE: The list is decided by the node's homeZone value, so
        // that must be set correctly before calling this function.
        public override void AddNode(PCZSceneNode n)
        {
            if (n.HomeZone == this)
            {
                // add a reference to this node in the "nodes at home in this zone" list
                mHomeNodeList.Add(n);
            }
            else
            {
                // add a reference to this node in the "nodes visiting this zone" list
                mVisitorNodeList.Add(n);
            }
        }

        public override void RemoveNode(PCZSceneNode n)
        {
            if (n.HomeZone == this)
            {
                mHomeNodeList.Remove(n);
            }
            else
            {
                mVisitorNodeList.Remove(n);
            }
        }

        /** Indicates whether or not this zone requires zone-specific data for
		*  each scene node
		*/

        public override bool RequiresZoneSpecificNodeData
        {
            get
            {
                // regular DefaultZones don't require any zone specific node data
                return false;
            }
        }

        /** Indicates whether or not this zone requires zone-specific data for
		*  each scene node
		*/

        /* Add a portal to the zone
		*/

        public override void AddPortal(Portal newPortal)
        {
            if (null != newPortal)
            {
                // make sure portal is unique (at least in this zone)
                if (mPortals.Contains(newPortal))
                {
                    throw new AxiomException("A portal with the name " + newPortal.getName() +
                                              "already exists. DefaultZone._addPortal");
                }

                // add portal to portals list
                mPortals.Add(newPortal);

                // tell the portal which zone it's currently in
                newPortal.setCurrentHomeZone(this);
            }
        }

        /* Remove a portal from the zone (does not erase the portal object, just removes reference)
		*/

        public override void RemovePortal(Portal removePortal)
        {
            if (null != removePortal && mPortals.Contains(removePortal))
            {
                mPortals.Remove(removePortal);
            }
        }

        /* Recursively check for intersection of the given scene node
		 * with zone portals.  If the node touches a portal, then the
		 * connected zone is assumed to be touched.  The zone adds
		 * the node to its node list and the node adds the zone to
		 * its visiting zone list.
		 *
		 * NOTE: This function assumes that the home zone of the node
		 *       is correct.  The function "_updateHomeZone" in PCZSceneManager
		 *		 takes care of this and should have been called before
		 *		 this function.
		 */

        public override void CheckNodeAgainstPortals(PCZSceneNode pczsn, Portal ignorePortal)
        {
            if (pczsn == mEnclosureNode || pczsn.AllowToVisit == false)
            {
                // don't do any checking of enclosure node versus portals
                return;
            }

            PCZone connectedZone;
            foreach (Portal portal in mPortals)
            {
                if (portal != ignorePortal && portal.intersects(pczsn) != PortalIntersectResult.NO_INTERSECT)
                {
                    connectedZone = portal.getTargetZone();

                    if (connectedZone != pczsn.HomeZone && !pczsn.IsVisitingZone(connectedZone))
                    {
                        pczsn.AddZoneToVisitingZonesMap(connectedZone);

                        connectedZone.AddNode(pczsn);

                        connectedZone.CheckNodeAgainstPortals(pczsn, portal.getTargetPortal());
                    }
                }
            }
        }

        /** (recursive) check the given light against all portals in the zone
		* NOTE: This is the default implementation, which doesn't take advantage
		*       of any zone-specific optimizations for checking portal visibility
		*/

        public override void CheckLightAgainstPortals(PCZLight light, ulong frameCount, PCZFrustum portalFrustum,
                                                       Portal ignorePortal)
        {
            foreach (Portal p in mPortals)
            {
                //Portal * p = *it;
                if (p != ignorePortal)
                {
                    // calculate the direction vector from light to portal
                    Vector3 lightToPortal = p.getDerivedCP() - light.GetDerivedPosition();
                    if (portalFrustum.IsObjectVisible(p))
                    {
                        // portal is facing the light, but some light types need to
                        // check illumination radius too.
                        PCZone targetZone = p.getTargetZone();
                        switch (light.Type)
                        {
                            case LightType.Point:
                                // point lights - just check if within illumination range
                                if (lightToPortal.Length <= light.AttenuationRange)
                                {
                                    // if portal is quad portal it must be pointing towards the light
                                    if ((p.Type == PORTAL_TYPE.PORTAL_TYPE_QUAD && lightToPortal.Dot(p.getDerivedDirection()) < 0.0) ||
                                         (p.Type != PORTAL_TYPE.PORTAL_TYPE_QUAD))
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
                                            p.getTargetZone().CheckLightAgainstPortals(light, frameCount, portalFrustum, p.getTargetPortal());
                                            // remove the planes added by this portal
                                            portalFrustum.RemovePortalCullingPlanes(p);
                                        }
                                    }
                                }
                                break;
                            case LightType.Directional:
                                // directionals have infinite range, so just make sure
                                // the direction is facing the portal
                                if (lightToPortal.Dot(light.DerivedDirection) >= 0.0)
                                {
                                    // if portal is quad portal it must be pointing towards the light
                                    if ((p.Type == PORTAL_TYPE.PORTAL_TYPE_QUAD && lightToPortal.Dot(p.getDerivedDirection()) < 0.0) ||
                                         (p.Type == PORTAL_TYPE.PORTAL_TYPE_QUAD))
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
                                            p.getTargetZone().CheckLightAgainstPortals(light, frameCount, portalFrustum, p.getTargetPortal());
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
                                // as a future optimisation.
                                if (lightToPortal.Length <= light.AttenuationRange)
                                {
                                    // if portal is quad portal it must be pointing towards the light
                                    if ((p.Type == PORTAL_TYPE.PORTAL_TYPE_QUAD && lightToPortal.Dot(p.getDerivedDirection()) < 0.0) ||
                                         (p.Type != PORTAL_TYPE.PORTAL_TYPE_QUAD))
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
                                            p.getTargetZone().CheckLightAgainstPortals(light, frameCount, portalFrustum, p.getTargetPortal());
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

        /** Update the spatial data for the portals in the zone
		* NOTE: All scenenodes must be up-to-date before calling this routine.
		*/

        public override void UpdatePortalsSpatially()
        {
            // update each portal spatial data
            foreach (Portal portal in mPortals)
            {
                portal.updateDerivedValues();
            }
        }

        /** Update the zone data for the portals in the zone
		* NOTE: All portal spatial data must be up-to-date before calling this routine.
		*/

        public override void UpdatePortalsZoneData()
        {
            var transferPortalList = new List<Portal>();

            // check each portal to see if it's intersecting another portal of greater size
            foreach (Portal p in mPortals)
            {
                Real pRadius = p.getRadius();
                // First we check against portals in the SAME zone (and only if they have a
                // target zone different from the home zone)
                foreach (Portal p2 in mPortals)
                {
                    // only check against bigger portals (this will also prevent checking against self)
                    // and only against portals which point to another zone
                    if (pRadius < p2.getRadius() && p2.getTargetZone() != this)
                    {
                        // Portal#2 is bigger than Portal1, check for crossing
                        if (p.crossedPortal(p2) && p.getCurrentHomeZone() != p2.getTargetZone())
                        {
                            // portal#1 crossed portal#2 - flag portal#1 to be moved to portal#2's target zone
                            p.setNewHomeZone(p2.getTargetZone());
                            transferPortalList.Add(p);
                            break;
                        }
                    }
                }

                // Second we check against portals in the target zone (and only if that target
                // zone is different from the home zone)
                PCZone tzone = p.getTargetZone();
                if (tzone != this)
                {
                    foreach (Portal p3 in mPortals)
                    {
                        // only check against bigger portals
                        if (pRadius < p3.getRadius())
                        {
                            // Portal#3 is bigger than Portal#1, check for crossing
                            if (p.crossedPortal(p3) && p.getCurrentHomeZone() != p3.getTargetZone())
                            {
                                // Portal#1 crossed Portal#3 - switch target zones for Portal#1
                                p.setTargetZone(p3.getTargetZone());
                                break;
                            }
                        }
                    }
                }
            }
            // transfer any portals to new zones that have been flagged
            foreach (Portal p in transferPortalList)
            {
                if (null != p.getNewHomeZone())
                {
                    RemovePortal(p);
                    p.getNewHomeZone().AddPortal(p);
                    p.setNewHomeZone(null);
                }
            }
            transferPortalList.Clear();
        }

        /* The following function checks if a node has left it's current home zone.
		* This is done by checking each portal in the zone.  If the node has crossed
		* the portal, then the current zone is no longer the home zone of the node.  The
		* function then recurses into the connected zones.  Once a zone is found where
		* the node does NOT cross out through a portal, that zone is the new home zone.
		NOTE: For this function to work, the node must start out in the proper zone to
			  begin with!
		*/

        public override PCZone UpdateNodeHomeZone(PCZSceneNode pczsn, bool allowBackTouches)
        {
            // default to newHomeZone being the current home zone
            PCZone newHomeZone = pczsn.HomeZone;

            // Check all portals of the start zone for crossings!
            foreach (Portal portal in mPortals)
            {
                PortalIntersectResult pir = portal.intersects(pczsn);
                switch (pir)
                {
                    default:
                    case PortalIntersectResult.NO_INTERSECT: // node does not intersect portal - do nothing
                    case PortalIntersectResult.INTERSECT_NO_CROSS: // node intersects but does not cross portal - do nothing
                        break;
                    case PortalIntersectResult.INTERSECT_BACK_NO_CROSS: // node intersects but on the back of the portal
                        if (allowBackTouches)
                        {
                            // node is on wrong side of the portal - fix if we're allowing backside touches
                            if (portal.getTargetZone() != this && portal.getTargetZone() != pczsn.HomeZone)
                            {
                                // set the home zone of the node to the target zone of the portal
                                pczsn.HomeZone = portal.getTargetZone();
                                // continue checking for portal crossings in the new zone
                                newHomeZone = portal.getTargetZone().UpdateNodeHomeZone(pczsn, false);
                            }
                        }
                        break;
                    case PortalIntersectResult.INTERSECT_CROSS:
                        // node intersects and crosses the portal - recurse into that zone as new home zone
                        if (portal.getTargetZone() != this && portal.getTargetZone() != pczsn.HomeZone)
                        {
                            // set the home zone of the node to the target zone of the portal
                            pczsn.HomeZone = portal.getTargetZone();
                            // continue checking for portal crossings in the new zone
                            newHomeZone = portal.getTargetZone().UpdateNodeHomeZone(pczsn, true);
                        }
                        break;
                }
            }

            // return the new home zone
            return newHomeZone;
        }

        /*
		// Recursively walk the zones, adding all visible SceneNodes to the list of visible nodes.
		*/

        public override void FindVisibleNodes(PCZCamera camera, ref List<PCZSceneNode> visibleNodeList, RenderQueue queue,
                                               VisibleObjectsBoundsInfo visibleBounds, bool onlyShadowCasters,
                                               bool displayNodes, bool showBoundingBoxes)
        {
            //return immediately if nothing is in the zone.
            if (mHomeNodeList.Count == 0 && mVisitorNodeList.Count == 0 && mPortals.Count == 0)
            {
                return;
            }

            // Else, the zone is automatically assumed to be visible since either
            // it is the camera the zone is in, or it was reached because
            // a connecting portal was deemed visible to the camera.

            // enable sky if called to do so for this zone
            if (HasSky)
            {
                // enable sky
                mPCZSM.EnableSky(true);
            }

            // find visible nodes at home in the zone
            bool vis;

            foreach (PCZSceneNode pczsn in mHomeNodeList)
            {
                //PCZSceneNode pczsn = *it;
                // if the scene node is already visible, then we can skip it
                if (pczsn.LastVisibleFrame != mLastVisibleFrame || pczsn.LastVisibleFromCamera != camera)
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
                        pczsn.LastVisibleFrame = mLastVisibleFrame;
                        pczsn.LastVisibleFromCamera = camera;
                    }
                }
            }
            // find visible visitor nodes

            foreach (PCZSceneNode pczsn in mVisitorNodeList)
            {
                // if the scene node is already visible, then we can skip it
                if (pczsn.LastVisibleFrame != mLastVisibleFrame || pczsn.LastVisibleFromCamera != camera)
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
                        pczsn.LastVisibleFrame = mLastVisibleFrame;
                        pczsn.LastVisibleFromCamera = camera;
                    }
                }
            }

            // find visible portals in the zone and recurse into them
            foreach (Portal portal in mPortals)
            {
                FrustumPlane fPlane;
                // for portal, check visibility using world bounding sphere & direction
                vis = camera.IsObjectVisible(portal, out fPlane);
                if (vis)
                {
                    // portal is visible. Add the portal as extra culling planes to camera
                    int planes_added = camera.AddPortalCullingPlanes(portal);
                    // tell target zone it's visible this frame
                    portal.getTargetZone().LastVisibleFrame = mLastVisibleFrame;
                    portal.getTargetZone().LastVisibleFromCamera = camera;
                    // recurse into the connected zone
                    portal.getTargetZone().FindVisibleNodes(camera, ref visibleNodeList, queue, visibleBounds, onlyShadowCasters,
                                                             displayNodes, showBoundingBoxes);
                    if (planes_added > 0)
                    {
                        // Then remove the extra culling planes added before going to the next portal in this zone.
                        camera.RemovePortalCullingPlanes(portal);
                    }
                }
            }
        }

        // --- find nodes which intersect various types of BV's ---
        public override void FindNodes(AxisAlignedBox t, ref List<PCZSceneNode> list, List<Portal> visitedPortals,
                                        bool includeVisitors, bool recurseThruPortals, PCZSceneNode exclude)
        {
            // if this zone has an enclosure, check against the enclosure AABB first
            if (null != mEnclosureNode)
            {
                if (!mEnclosureNode.WorldAABB.Intersects(t))
                {
                    // AABB of zone does not intersect t, just return.
                    return;
                }
            }

            foreach (PCZSceneNode pczsn in mHomeNodeList)
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
                foreach (PCZSceneNode pczsn in mVisitorNodeList)
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
                foreach (Portal portal in mPortals)
                {
                    // check portal versus bounding box
                    if (portal.intersects(t))
                    {
                        // make sure portal hasn't already been recursed through
                        if (!visitedPortals.Contains(portal))
                        {
                            // save portal to the visitedPortals list
                            visitedPortals.Add(portal);
                            // recurse into the connected zone
                            portal.getTargetZone().FindNodes(t, ref list, visitedPortals, includeVisitors, recurseThruPortals, exclude);
                        }
                    }
                }
            }
        }

        public override void FindNodes(Sphere t, ref List<PCZSceneNode> list, List<Portal> visitedPortals,
                                        bool includeVisitors, bool recurseThruPortals, PCZSceneNode exclude)
        {
            // if this zone has an enclosure, check against the enclosure AABB first
            if (null != mEnclosureNode)
            {
                if (!mEnclosureNode.WorldAABB.Intersects(t))
                {
                    // AABB of zone does not intersect t, just return.
                    return;
                }
            }

            // check nodes at home in this zone
            foreach (PCZSceneNode pczsn in mHomeNodeList)
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
                foreach (PCZSceneNode pczsn in mVisitorNodeList)
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
                foreach (Portal portal in mPortals)
                {
                    // check portal versus boundign box
                    if (portal.intersects(t))
                    {
                        // make sure portal hasn't already been recursed through
                        if (!visitedPortals.Contains(portal))
                        {
                            // save portal to the visitedPortals list
                            visitedPortals.Add(portal);
                            // recurse into the connected zone
                            portal.getTargetZone().FindNodes(t, ref list, visitedPortals, includeVisitors, recurseThruPortals, exclude);
                        }
                    }
                }
            }
        }

        public override void FindNodes(PlaneBoundedVolume t, ref List<PCZSceneNode> list, List<Portal> visitedPortals,
                                        bool includeVisitors, bool recurseThruPortals, PCZSceneNode exclude)
        {
            // if this zone has an enclosure, check against the enclosure AABB first
            if (null != mEnclosureNode)
            {
                if (!t.Intersects(mEnclosureNode.WorldAABB))
                {
                    // AABB of zone does not intersect t, just return.
                    return;
                }
            }

            // check nodes at home in this zone
            foreach (PCZSceneNode pczsn in mHomeNodeList)
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
                foreach (PCZSceneNode pczsn in mVisitorNodeList)
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
                foreach (Portal portal in mPortals)
                {
                    // check portal versus boundign box
                    if (portal.intersects(t))
                    {
                        // make sure portal hasn't already been recursed through
                        if (!visitedPortals.Contains(portal))
                        {
                            // save portal to the visitedPortals list
                            visitedPortals.Add(portal);
                            // recurse into the connected zone
                            portal.getTargetZone().FindNodes(t, ref list, visitedPortals, includeVisitors, recurseThruPortals, exclude);
                        }
                    }
                }
            }
        }

        public override void FindNodes(Ray t, ref List<PCZSceneNode> list, List<Portal> visitedPortals, bool includeVisitors,
                                        bool recurseThruPortals, PCZSceneNode exclude)
        {
            // if this zone has an enclosure, check against the enclosure AABB first
            if (null != mEnclosureNode)
            {
                IntersectResult nsect = t.Intersects(mEnclosureNode.WorldAABB);
                if (!nsect.Hit)
                {
                    // AABB of zone does not intersect t, just return.
                    return;
                }
            }

            // check nodes at home in this zone
            foreach (PCZSceneNode pczsn in mHomeNodeList)
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
                foreach (PCZSceneNode pczsn in mVisitorNodeList)
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
                foreach (Portal portal in mPortals)
                {
                    // check portal versus boundign box
                    if (portal.intersects(t))
                    {
                        // make sure portal hasn't already been recursed through
                        if (!visitedPortals.Contains(portal))
                        {
                            // save portal to the visitedPortals list
                            visitedPortals.Add(portal);
                            // recurse into the connected zone
                            portal.getTargetZone().FindNodes(t, ref list, visitedPortals, includeVisitors, recurseThruPortals, exclude);
                        }
                    }
                }
            }
        }

        /* Set option for the zone */

        public override bool SetOption(string name, object value)
        {
            return false;
        }

        /** called when the scene manager creates a camera because
			some zone managers (like TerrainZone) need the camera info.
		*/

        public override void NotifyCameraCreated(Camera c)
        {
        }

        //-------------------------------------------------------------------------
        public override void NotifyWorldGeometryRenderQueue(int qid)
        {
        }

        //-------------------------------------------------------------------------
        public override void NotifyBeginRenderScene()
        {
        }

        //-------------------------------------------------------------------------
        public override void SetZoneGeometry(string filename, PCZSceneNode parentNode)
        {
            String entityName, nodeName;
            entityName = Name + "_entity";
            nodeName = Name + "_Node";
            Entity ent = mPCZSM.CreateEntity(entityName, filename);
            // create a node for the entity
            PCZSceneNode node;
            node = (PCZSceneNode)(parentNode.CreateChildSceneNode(nodeName, Vector3.Zero, Quaternion.Identity));
            // attach the entity to the node
            node.AttachObject(ent);
            // set the node as the enclosure node
            SetEnclosureNode(node);
        }
    }
}