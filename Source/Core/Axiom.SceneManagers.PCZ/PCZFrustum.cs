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

using System.Collections.Generic;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Math;

#endregion Namespace Declarations

namespace Axiom.SceneManagers.PortalConnected
{
    public class PCZFrustum
    {
        public enum Visibility
        {
            None,
            Partial,
            Full
        }

        private Vector3 mOrigin;
        private Plane mOriginPlane;
        private bool mUseOriginPlane;
        private readonly List<PCPlane> mActiveCullingPlanes = new List<PCPlane>();
        private readonly List<PCPlane> mCullingPlaneReservoir = new List<PCPlane>();
        private Projection projType;


        public PCZFrustum()
        {
            this.projType = Projection.Perspective;
            this.mUseOriginPlane = false;
        }

        ~PCZFrustum()
        {
            RemoveAllCullingPlanes();

            // clear out the culling plane reservoir
            this.mCullingPlaneReservoir.Clear();
        }

        // set the origin value
        public void SetOrigin(Vector3 newOrigin)
        {
            this.mOrigin = newOrigin;
        }

        // tell the frustum whether or not to use the originplane
        public void SetUseOriginPlane(bool yesno)
        {
            this.mUseOriginPlane = yesno;
        }

        public bool IsObjectVisible(AxisAlignedBox bound)
        {
            // Null boxes are always invisible
            if (bound.IsNull)
            {
                return false;
            }

            // Infinite boxes are always visible
            if (bound.IsInfinite)
            {
                return true;
            }

            // Get centre of the box
            Vector3 centre = bound.Center;
            // Get the half-size of the box
            Vector3 halfSize = bound.HalfSize;

            // Check originplane if told to
            if (this.mUseOriginPlane)
            {
                PlaneSide side = this.mOriginPlane.GetSide(centre, halfSize);
                if (side == PlaneSide.Negative)
                {
                    return false;
                }
            }

            // For each extra active culling plane, see if the entire aabb is on the negative side
            // If so, object is not visible
            foreach (PCPlane plane in this.mActiveCullingPlanes)
            {
                PlaneSide xside = plane.GetSide(centre, halfSize);
                if (xside == PlaneSide.Negative)
                {
                    return false;
                }
            }
            return true;
        }

        public bool IsObjectVisible(Sphere bound)
        {
            // Check originplane if told to
            if (this.mUseOriginPlane)
            {
                PlaneSide side = this.mOriginPlane.GetSide(bound.Center);
                if (side == PlaneSide.Negative)
                {
                    Real dist = this.mOriginPlane.GetDistance(bound.Center);
                    if (dist > bound.Radius)
                    {
                        return false;
                    }
                }
            }

            // For each extra active culling plane, see if the entire sphere is on the negative side
            // If so, object is not visible
            foreach (PCPlane plane in this.mActiveCullingPlanes)
            {
                PlaneSide xside = plane.GetSide(bound.Center);
                if (xside == PlaneSide.Negative)
                {
                    float dist = this.mOriginPlane.GetDistance(bound.Center);
                    if (dist > bound.Radius)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        ///     IsObjectVisible() function for portals.
        /// </summary>
        /// <remarks>
        ///     Everything needs to be updated spatially before this function is
        ///     called including portal corners, frustum planes, etc.
        /// </remarks>
        /// <param name="portal">
        ///     The <see cref="Portal"/> to check visibility against.
        /// </param>
        /// <returns>
        ///     true if the Portal is visible.
        /// </returns>
        public bool IsObjectVisible(Portal portal)
        {
            // if portal isn't open, it's not visible
            if (!portal.IsOpen)
            {
                return false;
            }

            // if the frustum has no planes, just return true
            if (this.mActiveCullingPlanes.Count == 0)
            {
                return true;
            }
            // check if this portal is already in the list of active culling planes (avoid
            // infinite recursion case)
            foreach (PCPlane plane in this.mActiveCullingPlanes)
            {
                if (plane.Portal == portal)
                {
                    return false;
                }
            }

            // if portal is of type AABB or Sphere, then use simple bound check against planes
            if (portal.Type == PORTAL_TYPE.PORTAL_TYPE_AABB)
            {
                var aabb = new AxisAlignedBox();
                aabb.SetExtents(portal.getDerivedCorner(0), portal.getDerivedCorner(1));
                return IsObjectVisible(aabb);
            }
            else if (portal.Type == PORTAL_TYPE.PORTAL_TYPE_SPHERE)
            {
                return IsObjectVisible(portal.getDerivedSphere());
            }

            // check if the portal norm is facing the frustum
            Vector3 frustumToPortal = portal.getDerivedCP() - this.mOrigin;
            Vector3 portalDirection = portal.getDerivedDirection();
            Real dotProduct = frustumToPortal.Dot(portalDirection);
            if (dotProduct > 0)
            {
                // portal is faced away from Frustum
                return false;
            }

            // check against frustum culling planes
            bool visible_flag;

            // Check originPlane if told to
            if (this.mUseOriginPlane)
            {
                // set the visible flag to false
                visible_flag = false;
                // we have to check each corner of the portal
                for (int corner = 0; corner < 4; corner++)
                {
                    PlaneSide side = this.mOriginPlane.GetSide(portal.getDerivedCorner(corner));
                    if (side != PlaneSide.Negative)
                    {
                        visible_flag = true;
                    }
                }
                // if the visible_flag is still false, then the origin plane
                // culled all the portal points
                if (visible_flag == false)
                {
                    // ALL corners on negative side therefore out of view
                    return false;
                }
            }

            // For each active culling plane, see if all portal points are on the negative
            // side. If so, the portal is not visible
            foreach (PCPlane plane in this.mActiveCullingPlanes)
            {
                visible_flag = false;
                // we have to check each corner of the portal
                for (int corner = 0; corner < 4; corner++)
                {
                    PlaneSide side = plane.GetSide(portal.getDerivedCorner(corner));
                    if (side != PlaneSide.Negative)
                    {
                        visible_flag = true;
                    }
                }
                // if the visible_flag is still false, then this plane
                // culled all the portal points
                if (visible_flag == false)
                {
                    // ALL corners on negative side therefore out of view
                    return false;
                }
            }

            // no plane culled all the portal points and the norm
            // was facing the frustum, so this portal is visible
            return true;
        }

        /// <summary>
        ///     A 'more detailed' check for visibility of an AAB.
        /// </summary>
        /// <remarks>
        ///     This is useful for stuff like Octree leaf culling.
        /// </remarks>
        /// <param name="bound">the <see cref="AxisAlignedBox"/> to check visibility aginst.</param>
        /// <returns>
        ///     None, Partial, or Full for visibility of the box.
        /// </returns>
        public Visibility GetVisibility(AxisAlignedBox bound)
        {
            // Null boxes always invisible
            if (bound.IsNull)
            {
                return Visibility.None;
            }

            // Get centre of the box
            Vector3 centre = bound.Center;
            // Get the half-size of the box
            Vector3 halfSize = bound.HalfSize;

            bool all_inside = true;

            // Check originplane if told to
            if (this.mUseOriginPlane)
            {
                PlaneSide side = this.mOriginPlane.GetSide(centre, halfSize);
                if (side == PlaneSide.Negative)
                {
                    return Visibility.None;
                }
                // We can't return now as the box could be later on the negative side of another plane.
                if (side == PlaneSide.Both)
                {
                    all_inside = false;
                }
            }

            // For each active culling plane, see if the entire aabb is on the negative side
            // If so, object is not visible
            foreach (PCPlane plane in this.mActiveCullingPlanes)
            {
                PlaneSide xside = plane.GetSide(centre, halfSize);
                if (xside == PlaneSide.Negative)
                {
                    return Visibility.None;
                }
                // We can't return now as the box could be later on the negative side of a plane.
                if (xside == PlaneSide.Both)
                {
                    all_inside = false;
                }
            }

            if (all_inside)
            {
                return Visibility.Full;
            }
            else
            {
                return Visibility.Partial;
            }
        }

        // calculate  culling planes from portal and frustum
        // origin and add to list of culling planes
        // NOTE: returns 0 if portal was completely culled by existing planes
        //		 returns > 0 if culling planes are added (# is planes added)
        public int AddPortalCullingPlanes(Portal portal)
        {
            int addedcullingplanes = 0;

            // If portal is of type aabb or sphere, add a plane which is same as frustum
            // origin plane (ie. redundant).  We do this because we need the plane as a flag
            // to prevent infinite recursion
            if (portal.Type == PORTAL_TYPE.PORTAL_TYPE_AABB || portal.Type == PORTAL_TYPE.PORTAL_TYPE_SPHERE)
            {
                PCPlane newPlane = GetUnusedCullingPlane();
                newPlane.SetFromAxiomPlane(this.mOriginPlane);
                newPlane.Portal = portal;
                this.mActiveCullingPlanes.Add(newPlane);
                addedcullingplanes++;
                return addedcullingplanes;
            }

            // For portal Quads: Up to 4 planes can be added by the sides of a portal quad.
            // Each plane is created from 2 corners (world space) of the portal and the
            // frustum origin (world space).
            int i, j;
            PlaneSide pt0_side, pt1_side;
            bool visible;
            for (i = 0; i < 4; i++)
            {
                // first check if both corners are outside of one of the existing planes
                j = i + 1;
                if (j > 3)
                {
                    j = 0;
                }
                visible = true;

                foreach (PCPlane plane in this.mActiveCullingPlanes)
                {
                    pt0_side = plane.GetSide(portal.getDerivedCorner(i));
                    pt1_side = plane.GetSide(portal.getDerivedCorner(j));
                    if (pt0_side == PlaneSide.Negative && pt1_side == PlaneSide.Negative)
                    {
                        // the portal edge was actually completely culled by one of  culling planes
                        visible = false;
                    }
                }
                if (visible)
                {
                    // add the plane created from the two portal corner points and the frustum location
                    // to the  culling plane
                    PCPlane newPlane = GetUnusedCullingPlane();
                    if (this.projType == Projection.Orthographic) // use camera direction if projection is orthographic.
                    {
                        newPlane.Redefine(portal.getDerivedCorner(j) + this.mOriginPlane.Normal, portal.getDerivedCorner(j),
                                           portal.getDerivedCorner(i));
                    }
                    else
                    {
                        newPlane.Redefine(this.mOrigin, portal.getDerivedCorner(j), portal.getDerivedCorner(i));
                    }
                    newPlane.Portal = portal;
                    this.mActiveCullingPlanes.Add(newPlane);
                    addedcullingplanes++;
                }
            }
            // if we added ANY planes from the quad portal, we should add the plane of the
            // portal itself as an additional culling plane.
            if (addedcullingplanes > 0)
            {
                PCPlane newPlane = GetUnusedCullingPlane();
                newPlane.Redefine(portal.getDerivedCorner(2), portal.getDerivedCorner(1), portal.getDerivedCorner(0));
                newPlane.Portal = portal;
                this.mActiveCullingPlanes.Add(newPlane);
                addedcullingplanes++;
            }
            return addedcullingplanes;
        }

        // remove culling planes created from the given portal
        public void RemovePortalCullingPlanes(Portal portal)
        {
            for (int i = 0; i < this.mActiveCullingPlanes.Count; i++)
            {
                PCPlane plane = this.mActiveCullingPlanes[i];
                if (plane.Portal == portal)
                {
                    this.mCullingPlaneReservoir.Add(plane);
                    this.mActiveCullingPlanes.Remove(plane);
                }
            }
        }

        // remove all active extra culling planes
        // NOTE: Does not change the use of the originPlane!
        public void RemoveAllCullingPlanes()
        {
            foreach (PCPlane plane in this.mActiveCullingPlanes)
            {
                // put the plane back in the reservoir
                this.mCullingPlaneReservoir.Add(plane);
            }

            this.mActiveCullingPlanes.Clear();
        }

        // set the origin plane
        public void SetOriginPlane(Vector3 rkNormal, Vector3 rkPoint)
        {
            this.mOriginPlane.Redefine(rkNormal, rkPoint);
        }

        // get an unused PCPlane from the CullingPlane Reservoir
        // note that this removes the PCPlane from the reservoir!
        public PCPlane GetUnusedCullingPlane()
        {
            PCPlane plane = null;
            if (this.mCullingPlaneReservoir.Count > 0)
            {
                plane = this.mCullingPlaneReservoir[0];
                this.mCullingPlaneReservoir.RemoveAt(0);
                return plane;
            }
            // no available planes! create one
            plane = new PCPlane();
            return plane;
        }

        public Projection ProjectionType
        {
            get
            {
                return this.projType;
            }
            set
            {
                this.projType = value;
            }
        }
    }
}