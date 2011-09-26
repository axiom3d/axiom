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
#endregion LGPL License

#region SVN Version Information
// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using Axiom.Graphics;
using Axiom.Math;

#endregion Namespace Declarations

namespace Axiom.Core
{
	public class DefaultShadowCameraSetup : IShadowCameraSetup
	{
		/// <summary>
		/// Gets a default implementation of a ShadowCamera.
		/// </summary>
		/// <param name="sceneManager"></param>
		/// <param name="camera"></param>
		/// <param name="viewport"></param>
		/// <param name="light"></param>
		/// <param name="textureCamera"></param>
		/// <param name="iteration"></param>
		public void GetShadowCamera( SceneManager sceneManager, Camera camera, Viewport viewport, Light light, Camera textureCamera, int iteration )
		{
			Vector3 pos, dir;
			Quaternion q;

			// reset custom view / projection matrix in case already set
			textureCamera.SetCustomViewMatrix( false );
			textureCamera.SetCustomProjectionMatrix( false );


			// get the shadow frustum's far distance
			var shadowDist = light.ShadowFarDistance;
			if ( shadowDist == 0.0f )
			{
				// need a shadow distance, make one up
				shadowDist = camera.Near * 300;
			}
			float shadowOffset = shadowDist * sceneManager.ShadowDirectionalLightTextureOffset;

			// Directional lights
			if ( light.Type == LightType.Directional )
			{
				// set up the shadow texture
				// Set ortho projection
				textureCamera.ProjectionType = Projection.Orthographic;
				// set ortho window so that texture covers far dist
				textureCamera.SetOrthoWindow( shadowDist * 2, shadowDist * 2 );

				// Calculate look at position
				// We want to look at a spot shadowOffset away from near plane
				// 0.5 is a litle too close for angles
				var target = camera.DerivedPosition + ( camera.DerivedDirection * shadowOffset );

				// Calculate direction, which same as directional light direction
				dir = -light.DerivedDirection; // backwards since point down -z
				dir.Normalize();

				// Calculate position
				// We want to be in the -ve direction of the light direction
				// far enough to project for the dir light extrusion distance
				pos = target + dir * sceneManager.ShadowDirectionalLightExtrusionDistance;

				// Round local x/y position based on a world-space texel; this helps to reduce
				// jittering caused by the projection moving with the camera
				// Viewport is 2 * near clip distance across (90 degree fov)
				//~ Real worldTexelSize = (texCam->getNearClipDistance() * 20) / vp->getActualWidth();
				//~ pos.x -= fmod(pos.x, worldTexelSize);
				//~ pos.y -= fmod(pos.y, worldTexelSize);
				//~ pos.z -= fmod(pos.z, worldTexelSize);
				var worldTexelSize = ( shadowDist * 2 ) / textureCamera.Viewport.ActualWidth;

				//get texCam orientation

				var up = Vector3.UnitY;
				// Check it's not coincident with dir
				if ( Utility.Abs( up.Dot( dir ) ) >= 1.0f )
				{
					// Use camera up
					up = Vector3.UnitZ;
				}
				// cross twice to rederive, only direction is unaltered
				var left = dir.Cross( up );
				left.Normalize();
				up = dir.Cross( left );
				up.Normalize();
				// Derive quaternion from axes
				q = Quaternion.FromAxes( left, up, dir );

				//convert world space camera position into light space
				var lightSpacePos = q.Inverse() * pos;

				//snap to nearest texel
				lightSpacePos.x -= lightSpacePos.x % worldTexelSize; //fmod(lightSpacePos.x, worldTexelSize);
				lightSpacePos.y -= lightSpacePos.y % worldTexelSize; //fmod(lightSpacePos.y, worldTexelSize);

				//convert back to world space
				pos = q * lightSpacePos;

			}
			// Spotlight
			else if ( light.Type == LightType.Spotlight )
			{
				// Set perspective projection
				textureCamera.ProjectionType = Projection.Perspective;
				// set FOV slightly larger than the spotlight range to ensure coverage
				Radian fovy = light.SpotlightOuterAngle * 1.2f;

				// limit angle
				if ( fovy.InDegrees > 175 )
					fovy = (Degree)( 175 );
				textureCamera.FieldOfView = (float)fovy;

				// set near clip the same as main camera, since they are likely
				// to both reflect the nature of the scene
				textureCamera.Near = camera.Near;

				// Calculate position, which same as spotlight position
				pos = light.GetDerivedPosition();

				// Calculate direction, which same as spotlight direction
				dir = -light.DerivedDirection; // backwards since point down -z
				dir.Normalize();
			}
			// Point light
			else
			{
				// Set perspective projection
				textureCamera.ProjectionType = Projection.Perspective;
				// Use 120 degree FOV for point light to ensure coverage more area
				textureCamera.FieldOfView = 120.0f;
				// set near clip the same as main camera, since they are likely
				// to both reflect the nature of the scene
				textureCamera.Near = camera.Near;

				// Calculate look at position
				// We want to look at a spot shadowOffset away from near plane
				// 0.5 is a litle too close for angles
				var target = camera.DerivedPosition + ( camera.DerivedDirection * shadowOffset );

				// Calculate position, which same as point light position
				pos = light.GetDerivedPosition();

				dir = ( pos - target ); // backwards since point down -z
				dir.Normalize();
			}

			// Finally set position
			textureCamera.Position = pos;

			// Calculate orientation based on direction calculated above
			var up2 = Vector3.UnitY;

			// Check it's not coincident with dir
			if ( Utility.Abs( up2.Dot( dir ) ) >= 1.0f )
			{
				// Use camera up
				up2 = Vector3.UnitZ;
			}

			// cross twice to rederive, only direction is unaltered
			var left2 = dir.Cross( up2 );
			left2.Normalize();
			up2 = dir.Cross( left2 );
			up2.Normalize();

			// Derive quaternion from axes
			q = Quaternion.FromAxes( left2, up2, dir );
			textureCamera.Orientation = q;
		}
	}
}