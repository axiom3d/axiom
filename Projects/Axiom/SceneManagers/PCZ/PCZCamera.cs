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
using Axiom.Math;
using Axiom.Graphics;

#endregion Namespace Declarations

namespace Axiom.SceneManagers.PortalConnected
{
    public class PCZCamera : Camera
    {
        private PCZFrustum _extraCullingFrustum = new PCZFrustum();
        private static NameGenerator<PCZCamera> _nameGenerator = new NameGenerator<PCZCamera>("PCZCamera");

        public PCZCamera(SceneManager sceneManager)
            : this(_nameGenerator.GetNextUniqueName(), sceneManager)
        {
        }

        public PCZCamera(string name, SceneManager sceneManager)
            : base(name, sceneManager)
        {
            base._boundingBox = new AxisAlignedBox(new Vector3(-0.1f, -0.1f, -0.1f), new Vector3(0.1f, 0.1f, 0.1f));
            _extraCullingFrustum = new PCZFrustum();
            _extraCullingFrustum.SetUseOriginPlane(true);
        }

        // this version checks against extra culling planes
        public new bool IsObjectVisible(AxisAlignedBox bound, out FrustumPlane culledBy)
        {
            culledBy = FrustumPlane.None;

            // Null boxes always invisible
            if (bound.IsNull)
                return false;

            // infinite boxes always visible
            if (bound.IsInfinite)
                return true;

            // Make any pending updates to the calculated frustum planes
            UpdateFrustumPlanes();

            // check extra culling planes
            bool extraResults;
            extraResults = _extraCullingFrustum.IsObjectVisible(bound);
            if (!extraResults)
            {
                return false;
            }

            // check "regular" camera frustum
            bool regcamresults = base.IsObjectVisible(bound, out culledBy);

            if (!regcamresults)
            {
                // culled by regular culling planes
                return regcamresults;
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
        /// <param name="culledBy">
        /// The <see cref="FrustumPlane"/> that the Portal is in.
        /// </param>
        /// <returns>
        /// true if the Portal is visible.
        /// </returns>
        public bool IsVisible(Portal portal, out FrustumPlane culledBy)
        {
            culledBy = FrustumPlane.None;

            // if portal isn't open, it's not visible
            if (!portal.Enabled)
            {
                return false;
            }

            // check the extra frustum first
            if (!_extraCullingFrustum.IsObjectVisible(portal))
            {
                return false;
            }

            // if portal is of type AABB or Sphere, then use simple bound check against planes
            if (portal.Type == PortalType.AABB)
            {
                AxisAlignedBox aabb = new AxisAlignedBox(portal.DerivedCorners[0], portal.DerivedCorners[1]);
                return base.IsObjectVisible(aabb, out culledBy);
            }
            else if (portal.Type == PortalType.Sphere)
            {
                return base.IsObjectVisible(portal.DerivedSphere, out culledBy);
            }

            // check if the portal norm is facing the camera
            Vector3 cameraToPortal = portal.DerivedCP - DerivedPosition;
            Vector3 portalDirection = portal.DerivedDirection;
            Real dotProduct = cameraToPortal.Dot(portalDirection);
            if (dotProduct > 0)
            {
                // portal is faced away from camera
                return false;
            }
            // check against regular frustum planes
            bool visible_flag;
            if (null != CullFrustum)
            {
                // For each frustum plane, see if all points are on the negative side
                // If so, object is not visible
                // NOTE: We skip the NEAR plane (plane #0) because Portals need to
                // be visible no matter how close you get to them.

                for (int plane = 1; plane < 6; ++plane)
                {
                    // set the visible flag to false
                    visible_flag = false;
                    // Skip far plane if infinite view frustum
                    if ((FrustumPlane)plane == FrustumPlane.Far && _farDistance == 0)
                        continue;

                    // we have to check each corner of the portal
                    for (int corner = 0; corner < 4; corner++)
                    {
                        PlaneSide side = CullFrustum.FrustumPlanes[plane].GetSide(portal.DerivedCorners[corner]);
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
                        if (culledBy != FrustumPlane.None)
                            culledBy = (FrustumPlane)plane;
                        return false;
                    }
                }
            }
            else
            {
                // Make any pending updates to the calculated frustum planes
                UpdateFrustumPlanes();

                // For each frustum plane, see if all points are on the negative side
                // If so, object is not visible
                // NOTE: We skip the NEAR plane (plane #0) because Portals need to
                // be visible no matter how close you get to them.
                // BUGBUG: This can cause a false positive situation when a portal is
                // behind the camera but close.  This could be fixed by having another
                // culling plane at the camera location with normal same as camera direction.
                for (int plane = 1; plane < 6; ++plane)
                {
                    // set the visible flag to false
                    visible_flag = false;
                    // Skip far plane if infinite view frustum
                    if ((FrustumPlane)plane == FrustumPlane.Far && _farDistance == 0)
                        continue;

                    // we have to check each corner of the portal
                    for (int corner = 0; corner < 4; corner++)
                    {
                        PlaneSide side = _planes[plane].GetSide(portal.DerivedCorners[corner]);
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
                        if (culledBy != FrustumPlane.None)
                            culledBy = (FrustumPlane)plane;
                        return false;
                    }
                }
            }
            // no plane culled all the portal points and the norm
            // was facing the camera, so this portal is visible
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
        public PCZFrustum.Visibility GetVisibility(AxisAlignedBox bound)
        {
            // Null boxes always invisible
            if (bound.IsNull)
                return PCZFrustum.Visibility.None;

            // Get centre of the box
            Vector3 centre = bound.Center;
            // Get the half-size of the box
            Vector3 halfSize = bound.HalfSize;

            bool all_inside = true;

            for (int plane = 0; plane < 6; ++plane)
            {
                // Skip far plane if infinite view frustum
                if (plane == (int)FrustumPlane.Far && Far == 0)
                    continue;

                // This updates frustum planes and deals with cull frustum
                PlaneSide side = FrustumPlanes[plane].GetSide(centre, halfSize);
                if (side == PlaneSide.Negative)
                    return PCZFrustum.Visibility.None;
                // We can't return now as the box could be later on the negative side of a plane.
                if (side == PlaneSide.Both)
                    all_inside = false;
            }

            switch (_extraCullingFrustum.GetVisibility(bound))
            {
                case PCZFrustum.Visibility.None:
                    return PCZFrustum.Visibility.None;
                case PCZFrustum.Visibility.Partial:
                    return PCZFrustum.Visibility.Partial;
                case PCZFrustum.Visibility.Full:
                    break;
            }

            if (all_inside)
                return PCZFrustum.Visibility.Full;
            else
                return PCZFrustum.Visibility.Partial;

        }

        public new Projection ProjectionType
        {
            get
            {
                return base.ProjectionType;
            }
            set
            {
                base.ProjectionType = value;
                _extraCullingFrustum.ProjectionType = value;
            }
        }

        // calculate extra culling planes from portal and camera
        // origin and add to list of extra culling planes
        // NOTE: returns 0 if portal was completely culled by existing planes
        //		 returns > 0 if culling planes are added (# is planes added)
        public int AddPortalCullingPlanes(Portal portal)
        {
            // add the extra culling planes from the portal
            return _extraCullingFrustum.AddPortalCullingPlanes(portal);
        }

        // remove extra culling planes created from the given portal
        // NOTE: This should only be used during visibility traversal (backing out of a recursion)
        public void RemovePortalCullingPlanes(Portal portal)
        {
            _extraCullingFrustum.RemovePortalCullingPlanes(portal);
        }

        // remove all extra culling planes
        public void RemoveAllExtraCullingPlanes()
        {
            _extraCullingFrustum.RemoveAllCullingPlanes();
        }

        public void Update()
        {
            // make sure the extra culling frustum origin stuff is up to date
            if (_extraCullingFrustum.ProjectionType == Projection.Perspective)
            //if (!mCustomViewMatrix)
            {
                _extraCullingFrustum.SetUseOriginPlane(true);
                _extraCullingFrustum.Origin = DerivedPosition;
                _extraCullingFrustum.SetOriginPlane(DerivedDirection, DerivedPosition);
            }
            else
            {
                // In ortho mode, we don't want to cull things behind camera.
                // This helps for back casting which is useful for texture shadow projection on directional light.
                _extraCullingFrustum.SetUseOriginPlane(false);
            }

        }

    }
}