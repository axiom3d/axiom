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

using System.Collections.Generic;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Math;

#endregion Namespace Declarations

namespace Axiom.SceneManagers.PortalConnected
{
    /// <summary>
    /// PCZFrustum
    /// </summary>
    public class PCZFrustum : DisposableObject 
    {
        public enum Visibility
        {
            None,
            Partial,
            Full
        }

        private Vector3 _origin = Vector3.Zero;
        private Plane _originPlane = new Plane();
        private bool _useOriginPlane = false;
        private List<PCZPlane> _activeCullingPlanes = new List<PCZPlane>();
        private List<PCZPlane> _cullingPlaneReservoir = new List<PCZPlane>();
        private Projection _projection = Projection.Perspective;

        public PCZFrustum()
        {
            _projection = Projection.Perspective;
            _useOriginPlane = false;
        }

        ~PCZFrustum()
        {
            RemoveAllCullingPlanes();

            // clear out the culling plane reservoir
            _cullingPlaneReservoir.Clear();
        }

        public Vector3 Origin
        {
            get { return _origin; }
            set { _origin = value; }
        }

        // tell the frustum whether or not to use the origin plane
        public void SetUseOriginPlane(bool yesno)
        {
            _useOriginPlane = yesno;
        }

        public bool IsObjectVisible(AxisAlignedBox bound)
        {
            // Null boxes are always invisible
            if (bound.IsNull)
                return false;

            // Infinite boxes are always visible
            if (bound.IsInfinite)
                return true;

            // Get centre of the box
            Vector3 centre = bound.Center;
            // Get the half-size of the box
            Vector3 halfSize = bound.HalfSize;

            // Check origin plane if told to
            if (_useOriginPlane)
            {
                PlaneSide side = _originPlane.GetSide(centre, halfSize);
                if (side == PlaneSide.Negative)
                {
                    return false;
                }
            }

            // For each extra active culling plane, see if the entire aabb is on the negative side
            // If so, object is not visible
            foreach (PCZPlane plane in _activeCullingPlanes)
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
            // Check origin plane if told to
            if (_useOriginPlane)
            {
                PlaneSide side = _originPlane.GetSide(bound.Center);
                if (side == PlaneSide.Negative)
                {
                    Real dist = _originPlane.GetDistance(bound.Center);
                    if (dist > bound.Radius)
                    {
                        return false;
                    }
                }
            }

            // For each extra active culling plane, see if the entire sphere is on the negative side
            // If so, object is not visible
            foreach (PCZPlane plane in _activeCullingPlanes)
            {
                PlaneSide xside = plane.GetSide(bound.Center);
                if (xside == PlaneSide.Negative)
                {
                    float dist = _originPlane.GetDistance(bound.Center);
                    if (dist > bound.Radius)
                    {
                        return false;
                    }
                }

            }

            return true;
        }

        /// <summary>
        /// IsObjectVisible() function for portals.
        /// </summary>
        /// <remarks>
        /// Everything needs to be updated spatially before this function is
        /// called including portal corners, frustum planes, etc.
        /// </remarks>
        /// <param name="portal">
        /// The <see cref="Portal"/> to check visibility against.
        /// </param>
        /// <returns>
        /// true if the Portal is visible.
        /// </returns>
        public bool IsObjectVisible(Portal portal)
        {
            // if portal isn't open, it's not visible
            if (!portal.Enabled)
            {
                return false;
            }

            // if the frustum has no planes, just return true
            if (_activeCullingPlanes.Count == 0)
            {
                return true;
            }
            // check if this portal is already in the list of active culling planes (avoid
            // infinite recursion case)
            foreach (PCZPlane plane in _activeCullingPlanes)
            {
                if (plane.Portal == portal)
                {
                    return false;
                }
            }

            // if portal is of type AABB or Sphere, then use simple bound check against planes
            if (portal.Type == PortalType.AABB)
            {
                AxisAlignedBox aabb = new AxisAlignedBox();
                aabb.SetExtents(portal.DerivedCorners[0], portal.DerivedCorners[1]);
                return IsObjectVisible(aabb);
            }
            else if (portal.Type == PortalType.Sphere)
            {
                return IsObjectVisible(portal.DerivedSphere);
            }

            // check if the portal norm is facing the frustum
            Vector3 frustumToPortal = portal.DerivedCP - _origin;
            Vector3 portalDirection = portal.DerivedDirection;
            Real dotProduct = frustumToPortal.Dot(portalDirection);
            if (dotProduct > 0)
            {
                // portal is faced away from Frustum
                return false;
            }

            // check against frustum culling planes
            bool visible_flag;

            // Check originPlane if told to
            if (_useOriginPlane)
            {
                // set the visible flag to false
                visible_flag = false;
                // we have to check each corner of the portal
                for (int corner = 0; corner < 4; corner++)
                {
                    PlaneSide side = _originPlane.GetSide(portal.DerivedCorners[corner]);
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
            foreach (PCZPlane plane in _activeCullingPlanes)
            {
                visible_flag = false;
                // we have to check each corner of the portal
                for (int corner = 0; corner < 4; corner++)
                {
                    PlaneSide side = plane.GetSide(portal.DerivedCorners[corner]);
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
        /// A 'more detailed' check for visibility of an AAB.
        /// </summary>
        /// <remarks>
        /// This is useful for stuff like Octree leaf culling.
        /// </remarks>
        /// <param name="bound">the <see cref="AxisAlignedBox"/> to check visibility aginst.</param>
        /// <returns>
        /// None, Partial, or Full for visibility of the box.
        /// </returns>
        public Visibility GetVisibility(AxisAlignedBox bound)
        {

            // Null boxes always invisible
            if (bound.IsNull)
                return Visibility.None;

            // Get centre of the box
            Vector3 centre = bound.Center;
            // Get the half-size of the box
            Vector3 halfSize = bound.HalfSize;

            bool all_inside = true;

            // Check origin plane if told to
            if (_useOriginPlane)
            {
                PlaneSide side = _originPlane.GetSide(centre, halfSize);
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
            foreach (PCZPlane plane in _activeCullingPlanes)
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
                return Visibility.Full;
            else
                return Visibility.Partial;

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
            if (portal.Type == PortalType.AABB ||
                portal.Type == PortalType.Sphere)
            {
                PCZPlane newPlane = GetUnusedCullingPlane();
                newPlane.SetFromAxiomPlane(_originPlane);
                newPlane.Portal = portal;
                _activeCullingPlanes.Add(newPlane);
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

                foreach (PCZPlane plane in _activeCullingPlanes)
                {
                    pt0_side = plane.GetSide(portal.DerivedCorners[i]);
                    pt1_side = plane.GetSide(portal.DerivedCorners[j]);
                    if (pt0_side == PlaneSide.Negative &&
                        pt1_side == PlaneSide.Negative)
                    {
                        // the portal edge was actually completely culled by one of  culling planes
                        visible = false;
                    }
                }
                if (visible)
                {
                    // add the plane created from the two portal corner points and the frustum location
                    // to the  culling plane
                    PCZPlane newPlane = GetUnusedCullingPlane();
                    if (_projection == Projection.Orthographic) // use camera direction if projection is orthographic.
                    {
                        newPlane.Redefine(portal.DerivedCorners[j] + _originPlane.Normal,
                            portal.DerivedCorners[j], portal.DerivedCorners[i]);
                    }
                    else
                    {
                        newPlane.Redefine(_origin, portal.DerivedCorners[j], portal.DerivedCorners[i]);
                    }
                    newPlane.Portal = portal;
                    _activeCullingPlanes.Add(newPlane);
                    addedcullingplanes++;
                }
            }
            // if we added ANY planes from the quad portal, we should add the plane of the
            // portal itself as an additional culling plane.
            if (addedcullingplanes > 0)
            {
                PCZPlane newPlane = GetUnusedCullingPlane();
                newPlane.Redefine(portal.DerivedCorners[2], portal.DerivedCorners[1], portal.DerivedCorners[0]);
                newPlane.Portal = portal;
                _activeCullingPlanes.Add(newPlane);
                addedcullingplanes++;
            }
            return addedcullingplanes;
        }

        // remove culling planes created from the given portal
        public void RemovePortalCullingPlanes(Portal portal)
        {
            for (int i = 0; i < _activeCullingPlanes.Count; i++)
            {
                PCZPlane plane = _activeCullingPlanes[i];
                if (plane.Portal == portal)
                {
                    _cullingPlaneReservoir.Add(plane);
                    _activeCullingPlanes.Remove(plane);
                }
            }
        }

        // remove all active extra culling planes
        // NOTE: Does not change the use of the originPlane!
        public void RemoveAllCullingPlanes()
        {
            foreach (PCZPlane plane in _activeCullingPlanes)
            {
                // put the plane back in the reservoir
                _cullingPlaneReservoir.Add(plane);
            }

            _activeCullingPlanes.Clear();
        }

        // set the origin plane
        public void SetOriginPlane(Vector3 rkNormal, Vector3 rkPoint)
        {
            _originPlane.Redefine(rkNormal, rkPoint);
        }

        // get an unused PCPlane from the CullingPlane Reservoir
        // note that this removes the PCPlane from the reservoir!
        public PCZPlane GetUnusedCullingPlane()
        {
            PCZPlane plane = null;
            if (_cullingPlaneReservoir.Count > 0)
            {
                plane = _cullingPlaneReservoir[0];
                _cullingPlaneReservoir.RemoveAt(0);
                return plane;
            }
            // no available planes! create one
            plane = new PCZPlane();
            return plane;
        }

        public Projection ProjectionType
        {
            get
            {
                return _projection;
            }
            set
            {
                _projection = value;
            }
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
                    _activeCullingPlanes.Clear();
                    _cullingPlaneReservoir.Clear();
                    this._activeCullingPlanes = null;
                    this._cullingPlaneReservoir = null;
                }

                // There are no unmanaged resources to release, but
                // if we add them, they need to be released here.
            }

            base.dispose(disposeManagedResources);
        }

        #endregion IDisposable Implementation

    }
}