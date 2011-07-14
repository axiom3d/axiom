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

using System;
using System.Diagnostics;

using Axiom.Core;
using Axiom.Graphics;
using Axiom.Math;

#endregion Namespace Declarations

namespace Axiom.SceneManagers.Bsp
{
	/// <summary>
	///		Frustum used for spotlights. It is not supposed to be part of the scene, and
	///		will not be rendered. The ViewMatrix and ProjectionMatrix properties, used
	///		for projecting texturing, are updated only when a spotlight is assigned
	///		at the Light property.
	/// </summary>
	public class SpotlightFrustum : Frustum
	{
		protected Light light;
		protected Node lightNode;
		protected Quaternion lightOrientation;
		protected Vector3 lightPosition;

		public Light Spotlight
		{
			get
			{
				return light;
			}
			set
			{
				this.light = value;
				this.lightNode = light.ParentNode;
				this.lightPosition = light.DerivedPosition;
				this.lightOrientation = GetLightOrientation();

				base.FieldOfView = Utility.DegreesToRadians( light.SpotlightOuterAngle );
				base.Near = 1;
				base.Far = light.AttenuationRange;
				base.AspectRatio = 1;
				base.ProjectionType = Projection.Perspective;

				UpdateMatrices();
			}
		}

		protected void UpdateMatrices()
		{
			// grab a reference to the current render system
			RenderSystem renderSystem = Root.Instance.RenderSystem;

			if ( ProjectionType == Projection.Perspective )
			{
				// perspective transform, API specific
				ProjectionMatrix = renderSystem.MakeProjectionMatrix( FieldOfView, AspectRatio, Near, Far );
			}
			else if ( ProjectionType == Projection.Orthographic )
			{
				// orthographic projection, API specific
				ProjectionMatrix = renderSystem.MakeOrthoMatrix( FieldOfView, AspectRatio, Near, Far );
			}

			// View matrix is:
			//
			//  [ Lx  Uy  Dz  Tx  ]
			//  [ Lx  Uy  Dz  Ty  ]
			//  [ Lx  Uy  Dz  Tz  ]
			//  [ 0   0   0   1   ]
			//
			// Where T = -(Transposed(Rot) * Pos)

			// This is most efficiently done using 3x3 Matrices

			// Get orientation from quaternion
			Quaternion orientation = lightOrientation;
			Vector3 position = lightPosition;
			Matrix3 rotation = orientation.ToRotationMatrix();

			Vector3 left = rotation.GetColumn( 0 );
			Vector3 up = rotation.GetColumn( 1 );
			Vector3 direction = rotation.GetColumn( 2 );

			// make the translation relative to the new axis
			Matrix3 rotationT = rotation.Transpose();
			Vector3 translation = -rotationT * position;

			// initialize the upper 3x3 portion with the rotation
			_viewMatrix = rotationT;

			// add the translation portion, add set 1 for the bottom right portion
			_viewMatrix.m03 = translation.x;
			_viewMatrix.m13 = translation.y;
			_viewMatrix.m23 = translation.z;
			_viewMatrix.m33 = 1.0f;
		}

		protected Quaternion GetLightOrientation()
		{
			Vector3 zAdjustVec = -light.DerivedDirection;
			Vector3 xAxis, yAxis, zAxis;

			Quaternion orientation = ( lightNode == null ) ? Quaternion.Identity : lightNode.DerivedOrientation;

			// Get axes from current quaternion
			// get the vector components of the derived orientation vector
			orientation.ToAxes( out xAxis, out yAxis, out zAxis );

			Quaternion rotationQuat;

			if ( ( zAxis + zAdjustVec ).LengthSquared < 0.00001f )
			{
				// Oops, a 180 degree turn (infinite possible rotation axes)
				// Default to yaw i.e. use current UP
				rotationQuat = Quaternion.FromAngleAxis( Utility.PI, yAxis );
			}
			else
			{
				// Derive shortest arc to new direction
				rotationQuat = zAxis.GetRotationTo( zAdjustVec );
			}

			return rotationQuat * orientation;
		}
	}
}