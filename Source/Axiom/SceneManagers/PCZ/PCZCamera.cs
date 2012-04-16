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
using Axiom.Math;
using Axiom.Graphics;

#endregion Namespace Declarations

namespace Axiom.SceneManagers.PortalConnected
{
	public class PCZCamera : Camera
	{
		private AxisAlignedBox box;
		private PCZFrustum extraCullingFrustum;

		public PCZCamera( string name, SceneManager sceneManager )
			: base( name, sceneManager )
		{
			box = new AxisAlignedBox( new Vector3( -0.1f, -0.1f, -0.1f ), new Vector3( 0.1f, 0.1f, 0.1f ) );
			extraCullingFrustum = new PCZFrustum();
			extraCullingFrustum.SetUseOriginPlane( true );
		}

		public AxisAlignedBox GetBoundingBox()
		{
			return box;
		}

		// this version checks against extra culling planes
		public new bool IsObjectVisible( AxisAlignedBox bound, out FrustumPlane culledBy )
		{
			culledBy = FrustumPlane.None;

			// Null boxes always invisible
			if ( bound.IsNull )
			{
				return false;
			}

			// infinite boxes always visible
			if ( bound.IsInfinite )
			{
				return true;
			}

			// Make any pending updates to the calculated frustum planes
			UpdateFrustumPlanes();

			// check extra culling planes
			bool extraResults;
			extraResults = extraCullingFrustum.IsObjectVisible( bound );
			if ( !extraResults )
			{
				return false;
			}

			// check "regular" camera frustum
			bool regcamresults = base.IsObjectVisible( bound, out culledBy );

			if ( !regcamresults )
			{
				// culled by regular culling planes
				return regcamresults;
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
		/// <param name="culledBy">
		///     The <see cref="FrustumPlane"/> that the Portal is in.
		/// </param>
		/// <returns>
		///     true if the Portal is visible.
		/// </returns>
		public bool IsObjectVisible( Portal portal, out FrustumPlane culledBy )
		{
			culledBy = FrustumPlane.None;

			// if portal isn't open, it's not visible
			if ( !portal.IsOpen )
			{
				return false;
			}

			// check the extra frustum first
			if ( !extraCullingFrustum.IsObjectVisible( portal ) )
			{
				return false;
			}

			// if portal is of type AABB or Sphere, then use simple bound check against planes
			if ( portal.Type == PORTAL_TYPE.PORTAL_TYPE_AABB )
			{
				AxisAlignedBox aabb = new AxisAlignedBox( portal.getDerivedCorner( 0 ), portal.getDerivedCorner( 1 ) );
				return base.IsObjectVisible( aabb, out culledBy );
			}
			else if ( portal.Type == PORTAL_TYPE.PORTAL_TYPE_SPHERE )
			{
				return base.IsObjectVisible( portal.getDerivedSphere(), out culledBy );
			}

			// check if the portal norm is facing the camera
			Vector3 cameraToPortal = portal.getDerivedCP() - DerivedPosition;
			Vector3 portalDirection = portal.getDerivedDirection();
			Real dotProduct = cameraToPortal.Dot( portalDirection );
			if ( dotProduct > 0 )
			{
				// portal is faced away from camera
				return false;
			}
			// check against regular frustum planes
			bool visible_flag;
			if ( null != CullFrustum )
			{
				// For each frustum plane, see if all points are on the negative side
				// If so, object is not visible
				// NOTE: We skip the NEAR plane (plane #0) because Portals need to
				// be visible no matter how close you get to them.

				for ( int plane = 1; plane < 6; ++plane )
				{
					// set the visible flag to false
					visible_flag = false;
					// Skip far plane if infinite view frustum
					if ( (FrustumPlane)plane == FrustumPlane.Far && _farDistance == 0 )
					{
						continue;
					}

					// we have to check each corner of the portal
					for ( int corner = 0; corner < 4; corner++ )
					{
						PlaneSide side = CullFrustum.FrustumPlanes[ plane ].GetSide( portal.getDerivedCorner( corner ) );
						if ( side != PlaneSide.Negative )
						{
							visible_flag = true;
						}
					}
					// if the visible_flag is still false, then this plane
					// culled all the portal points
					if ( visible_flag == false )
					{
						// ALL corners on negative side therefore out of view
						if ( culledBy != FrustumPlane.None )
						{
							culledBy = (FrustumPlane)plane;
						}
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
				for ( int plane = 1; plane < 6; ++plane )
				{
					// set the visible flag to false
					visible_flag = false;
					// Skip far plane if infinite view frustum
					if ( (FrustumPlane)plane == FrustumPlane.Far && _farDistance == 0 )
					{
						continue;
					}

					// we have to check each corner of the portal
					for ( int corner = 0; corner < 4; corner++ )
					{
						PlaneSide side = _planes[ plane ].GetSide( portal.getDerivedCorner( corner ) );
						if ( side != PlaneSide.Negative )
						{
							visible_flag = true;
						}
					}
					// if the visible_flag is still false, then this plane
					// culled all the portal points
					if ( visible_flag == false )
					{
						// ALL corners on negative side therefore out of view
						if ( culledBy != FrustumPlane.None )
						{
							culledBy = (FrustumPlane)plane;
						}
						return false;
					}
				}
			}
			// no plane culled all the portal points and the norm
			// was facing the camera, so this portal is visible
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
		public PCZFrustum.Visibility GetVisibility( AxisAlignedBox bound )
		{
			// Null boxes always invisible
			if ( bound.IsNull )
			{
				return PCZFrustum.Visibility.None;
			}

			// Get centre of the box
			Vector3 centre = bound.Center;
			// Get the half-size of the box
			Vector3 halfSize = bound.HalfSize;

			bool all_inside = true;

			for ( int plane = 0; plane < 6; ++plane )
			{
				// Skip far plane if infinite view frustum
				if ( plane == (int)FrustumPlane.Far && Far == 0 )
				{
					continue;
				}

				// This updates frustum planes and deals with cull frustum
				PlaneSide side = FrustumPlanes[ plane ].GetSide( centre, halfSize );
				if ( side == PlaneSide.Negative )
				{
					return PCZFrustum.Visibility.None;
				}
				// We can't return now as the box could be later on the negative side of a plane.
				if ( side == PlaneSide.Both )
				{
					all_inside = false;
				}
			}

			switch ( extraCullingFrustum.GetVisibility( bound ) )
			{
				case PCZFrustum.Visibility.None:
					return PCZFrustum.Visibility.None;
				case PCZFrustum.Visibility.Partial:
					return PCZFrustum.Visibility.Partial;
				case PCZFrustum.Visibility.Full:
					break;
			}

			if ( all_inside )
			{
				return PCZFrustum.Visibility.Full;
			}
			else
			{
				return PCZFrustum.Visibility.Partial;
			}
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
				extraCullingFrustum.ProjectionType = value;
			}
		}

		// calculate extra culling planes from portal and camera
		// origin and add to list of extra culling planes
		// NOTE: returns 0 if portal was completely culled by existing planes
		//		 returns > 0 if culling planes are added (# is planes added)
		public int AddPortalCullingPlanes( Portal portal )
		{
			// add the extra culling planes from the portal
			return extraCullingFrustum.AddPortalCullingPlanes( portal );
		}

		// remove extra culling planes created from the given portal
		// NOTE: This should only be used during visibility traversal (backing out of a recursion)
		public void RemovePortalCullingPlanes( Portal portal )
		{
			extraCullingFrustum.RemovePortalCullingPlanes( portal );
		}

		// remove all extra culling planes
		public void RemoveAllExtraCullingPlanes()
		{
			extraCullingFrustum.RemoveAllCullingPlanes();
		}

		public void Update()
		{
			// make sure the extra culling frustum origin stuff is up to date
			if ( extraCullingFrustum.ProjectionType == Projection.Perspective )
				//if (!mCustomViewMatrix)
			{
				extraCullingFrustum.SetUseOriginPlane( true );
				extraCullingFrustum.SetOrigin( DerivedPosition );
				extraCullingFrustum.SetOriginPlane( DerivedDirection, DerivedPosition );
			}
			else
			{
				// In ortho mode, we don't want to cull things behind camera.
				// This helps for back casting which is useful for texture shadow projection on directional light.
				extraCullingFrustum.SetUseOriginPlane( false );
			}
		}
	}
}
