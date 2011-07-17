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
using System.Collections;
using System.Diagnostics;

using Axiom.Collections;
using Axiom.Controllers;
using Axiom.Core;
using Axiom.Math;
using Axiom.Core.Collections;

#endregion Namespace Declarations

namespace Axiom.Graphics
{
	/// <summary>
	/// 	This utility class is used to hold the information used to generate the matrices
	/// 	and other information required to automatically populate GpuProgramParameters.
	/// </summary>
	/// <remarks>
	///    This class exercises a lazy-update scheme in order to avoid having to update all
	/// 	the information a GpuProgramParameters class could possibly want all the time. 
	/// 	It relies on the SceneManager to update it when the base data has changed, and
	/// 	will calculate concatenated matrices etc only when required, passing back precalculated
	/// 	matrices when they are requested more than once when the underlying information has
	/// 	not altered.
	/// </remarks>
	public class AutoParamDataSource : DisposableObject
	{
		#region Fields

		/// <summary>
		///    Current target renderable.
		/// </summary>
		protected IRenderable renderable;
		/// <summary>
		///    Current camera being used for rendering.
		/// </summary>
		protected Camera camera;
		/// <summary>
		///		Current frustum used for texture projection.
		/// </summary>
		protected Frustum currentTextureProjector;
		/// <summary>
		///		Current active render target.
		/// </summary>
		protected RenderTarget currentRenderTarget;
		/// <summary>
		///     The current viewport.  We don't really do anything with this,
		///     but Ogre uses it to determine the width and height.
		/// </summary>
		protected Viewport currentViewport;
		/// <summary>
		///    Current view matrix;
		/// </summary>
		protected Matrix4 viewMatrix;
		protected bool viewMatrixDirty;
		/// <summary>
		///    Current projection matrix.
		/// </summary>
		protected Matrix4 projectionMatrix;
		protected bool projMatrixDirty;

        protected Matrix4 inverseTransposeWorldMatrix;
        protected bool inverseTransposeWorldMatrixDirty;

		/// <summary>
		///    Current view and projection matrices concatenated.
		/// </summary>
		protected Matrix4 viewProjMatrix;
		/// <summary>
		///    Array of world matrices for the current renderable.
		/// </summary>
		protected Matrix4[] worldMatrix = new Matrix4[ 256 ];
		/// <summary>
		///		Current count of matrices in the world matrix array.
		/// </summary>
		protected int worldMatrixCount;
		/// <summary>
		///    Current concatenated world and view matrices.
		/// </summary>
		protected Matrix4 worldViewMatrix;
		/// <summary>
		///    Current concatenated world, view, and projection matrices.
		/// </summary>
		protected Matrix4 worldViewProjMatrix;
		/// <summary>
		///    Inverse of current world matrix.
		/// </summary>
		protected Matrix4 inverseWorldMatrix;
		/// <summary>
		///    Inverse of current concatenated world and view matrices.
		/// </summary>
		protected Matrix4 inverseWorldViewMatrix;
		/// <summary>
		///    Inverse of the current view matrix.
		/// </summary>
		protected Matrix4 inverseViewMatrix;
		/// <summary>
		///    Inverse Transpose of the current world view matrix.
		/// </summary>
		protected Matrix4 inverseTransposeWorldViewMatrix;

		/// <summary>
		///		Current texture view projection matrix.
		/// </summary>
		protected Matrix4 textureViewProjMatrix;
		/// <summary>
		///		Distance to extrude shadow volume vertices.
		/// </summary>
		protected float dirLightExtrusionDistance;
		/// <summary>
		///    Position of the current camera in object space relative to the current renderable.
		/// </summary>
		protected Vector4 cameraPositionObjectSpace;
		/// <summary>
		///    Current global ambient light color.
		/// </summary>
		protected ColorEx ambientLight;
		/// <summary>
		///    Parameters for GPU fog.  fogStart, fogEnd, and fogScale
		/// </summary>
		protected Vector4 fogParams;
		/// <summary>
		///   current time
		/// </summary>
		protected float time;
		/// <summary>
		///    List of lights that are in the scene and near the current renderable.
		/// </summary>
		protected LightList currentLightList = new LightList();
		/// <summary>
		///    Blank light to use when a higher index light is requested than is available.
		/// </summary>
		protected Light blankLight = new Light();

		protected bool viewProjMatrixDirty;
		protected bool worldMatrixDirty;
		protected bool worldViewMatrixDirty;
		protected bool worldViewProjMatrixDirty;
		protected bool inverseWorldMatrixDirty;
		protected bool inverseWorldViewMatrixDirty;
		protected bool inverseViewMatrixDirty;
		protected bool inverseTransposeWorldViewMatrixDirty;
		protected bool cameraPositionObjectSpaceDirty;
		protected bool textureViewProjMatrixDirty;

		protected Matrix4 ProjectionClipSpace2DToImageSpacePerspective = new Matrix4(
			//from ogre
			//0.5f,    0,  0, 0.5f, 
			//0, -0.5f,  0, 0.5f, 
			//0,    0,  0.5f,   0.5f,
			//0,    0,  0,   1);
			//original from axiom
			//0.5f, 0, 0, -0.5f,
			//0, -0.5f,  0, -0.5f, 
			//0,    0,  0,   1,
			//0,    0,  0,   1);
			0.5f, 0, 0, 0.5f,
			0, -0.5f, 0, 0.5f,
			0, 0, 1, 0,
			0, 0, 0, 1 );

		protected int passNumber;

		protected Vector4 mvShadowTechnique;

		#endregion Fields

		#region Constructors

		/// <summary>
		///    Default constructor.
		/// </summary>
		public AutoParamDataSource()
            : base()
		{
			worldMatrixDirty = true;
			viewMatrixDirty = true;
			projMatrixDirty = true;
			worldViewMatrixDirty = true;
			viewProjMatrixDirty = true;
			worldViewProjMatrixDirty = true;
			inverseWorldMatrixDirty = true;
			inverseWorldViewMatrixDirty = true;
			inverseViewMatrixDirty = true;
			inverseTransposeWorldMatrixDirty = true;
			inverseTransposeWorldViewMatrixDirty = true;
			cameraPositionObjectSpaceDirty = true;
			// cameraPositionDirty = true;
			textureViewProjMatrixDirty = true;
			viewMatrixDirty = true;
			projMatrixDirty = true;

			// defaults for the blank light
			blankLight.Diffuse = ColorEx.Black;
			blankLight.Specular = ColorEx.Black;
			blankLight.SetAttenuation( 0, 1, 0, 0 );
		}

		#endregion

		#region Methods

        /// <summary>
        /// 
        /// </summary>
        /// <param name="disposeManagedResources"></param>
        protected override void dispose(bool disposeManagedResources)
        {
            if (!this.IsDisposed)
            {
                if (disposeManagedResources)
                {
                    if (this.blankLight != null)
                    {
                        if (!this.blankLight.IsDisposed)
                            this.blankLight.Dispose();

                        this.blankLight = null;
                    }
                }
            }

            base.dispose(disposeManagedResources);
        }

		#region Lights

		/// <summary>
		///    Get the light which is 'index'th closest to the current object 
		/// </summary>
		/// <param name="index">Ordinal value signifying the light to retreive, with 0 being closest, 1 being next closest, etc.</param>
		/// <returns>A light located near the current renderable.</returns>
		public Light GetLight( int index )
		{
			if ( currentLightList.Count <= index )
			{
				return blankLight;
			}
			else
			{
				return currentLightList[ index ];
			}
		}

		/// <summary>
		///    
		/// </summary>
		public void SetCurrentLightList( LightList lightList )
		{
			currentLightList = lightList;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="index">Ordinal value signifying the light to retreive. <see cref="GetLight"/></param>
		/// <returns></returns>
		public Real GetLightPowerScale( int index )
		{
			return this.GetLight( index ).PowerScale;
		}

		#endregion Lights

		/// <summary>
		///		Sets the constant extrusion distance for directional lights.
		/// </summary>
		/// <param name="distance"></param>
		public void SetShadowDirLightExtrusionDistance( float distance )
		{
			dirLightExtrusionDistance = distance;
		}

		#endregion

		#region Properties

		/// <summary>
		///    Gets/Sets the current renderable object.
		/// </summary>
		public IRenderable Renderable
		{
			get
			{
				return renderable;
			}
			set
			{
				renderable = value;

				// set the dirty flags to force updates
				worldMatrixDirty = true;
				viewMatrixDirty = true;
				projMatrixDirty = true;
				worldViewMatrixDirty = true;
				viewProjMatrixDirty = true;
				worldViewProjMatrixDirty = true;
				inverseWorldMatrixDirty = true;
				inverseViewMatrixDirty = true;
				inverseWorldViewMatrixDirty = true;
				// inverseTransposeWorldMatrixDirty = true;
				inverseTransposeWorldViewMatrixDirty = true;
				cameraPositionObjectSpaceDirty = true;
				viewMatrixDirty = true;
				projMatrixDirty = true;
			}
		}

		/// <summary>
		///    Gets/Sets the current camera being used for rendering.
		/// </summary>
		public Camera Camera
		{
			get
			{
				return camera;
			}
			set
			{
				camera = value;

				// set the dirty flags to force updates
				viewMatrixDirty = true;
				projMatrixDirty = true;
				worldViewMatrixDirty = true;
				viewProjMatrixDirty = true;
				worldViewProjMatrixDirty = true;
				inverseViewMatrixDirty = true;
				inverseWorldViewMatrixDirty = true;
				// inverseTransposeWorldViewMatrixDirty = true;
				cameraPositionObjectSpaceDirty = true;
				viewMatrixDirty = true;
				projMatrixDirty = true;
			}
		}

		/// <summary>
		///		Get/Set the current frustum used for texture projection.
		/// </summary>
		public Frustum TextureProjector
		{
			get
			{
				return currentTextureProjector;
			}
			set
			{
				currentTextureProjector = value;
				textureViewProjMatrixDirty = true;
			}
		}

		/// <summary>
		///		Get/Set the current active render target in use.
		/// </summary>
		public RenderTarget RenderTarget
		{
			get
			{
				return currentRenderTarget;
			}
			set
			{
				currentRenderTarget = value;
			}
		}

		/// <summary>
		///		Get/Set the current active viewport in use.
		/// </summary>
		public Viewport Viewport
		{
			get
			{
				return currentViewport;
			}
			set
			{
				currentViewport = value;
			}
		}

		/// <summary>
		///    Gets/Sets the current global ambient light color.
		/// </summary>
		public ColorEx AmbientLight
		{
			get
			{
				return ambientLight;
			}
			set
			{
				ambientLight = value;
			}
		}

		/// <summary>
		///    Gets/Sets the current gpu fog parameters.
		/// </summary>
		public Vector4 FogParams
		{
			get
			{
				return fogParams;
			}
			set
			{
				fogParams = value;
			}
		}

		/// <summary>
		///    Gets the current world matrix.
		/// </summary>
		public Matrix4 WorldMatrix
		{
			get
			{
				if ( worldMatrixDirty )
				{
					renderable.GetWorldTransforms( worldMatrix );
					worldMatrixCount = renderable.NumWorldTransforms;
					worldMatrixDirty = false;
				}

				return worldMatrix[ 0 ];
			}
		}

		/// <summary>
		///    Gets the number of current world matrices.
		/// </summary>
		public int WorldMatrixCount
		{
			get
			{
				if ( worldMatrixDirty )
				{
					renderable.GetWorldTransforms( worldMatrix );
					worldMatrixCount = renderable.NumWorldTransforms;
					worldMatrixDirty = false;
				}

				return worldMatrixCount;
			}
		}

		/// <summary>
		///		Gets an array with all the current world matrix transforms.
		/// </summary>
		public Matrix4[] WorldMatrixArray
		{
			get
			{
				if ( worldMatrixDirty )
				{
					renderable.GetWorldTransforms( worldMatrix );
					worldMatrixCount = renderable.NumWorldTransforms;
					worldMatrixDirty = false;
				}

				return worldMatrix;
			}
		}

		/// <summary>
		///    Gets/Sets the current concatenated world and view matrices.
		/// </summary>
		public Matrix4 WorldViewMatrix
		{
			get
			{
				if ( worldViewMatrixDirty )
				{
					worldViewMatrix = this.ViewMatrix * this.WorldMatrix;
					worldViewMatrixDirty = false;
				}
				return worldViewMatrix;
			}
		}

		/// <summary>
		///    Gets/Sets the current concatenated world, view, and projection matrices.
		/// </summary>
		public Matrix4 WorldViewProjMatrix
		{
			get
			{
				if ( worldViewProjMatrixDirty )
				{
					worldViewProjMatrix = this.ProjectionMatrix * this.WorldViewMatrix;
					worldViewProjMatrixDirty = false;
				}
				return worldViewProjMatrix;
			}
		}

		/// <summary>
		///    Gets/Sets the inverse of current world matrix.
		/// </summary>
		public Matrix4 InverseWorldMatrix
		{
			get
			{
				if ( inverseWorldMatrixDirty )
				{
					inverseWorldMatrix = this.WorldMatrix.Inverse();
					inverseWorldMatrixDirty = false;
				}
				return inverseWorldMatrix;
			}
		}

		/// <summary>
		///    Gets/Sets the inverse of current concatenated world and view matrices.
		/// </summary>
		public Matrix4 InverseWorldViewMatrix
		{
			get
			{
				if ( inverseWorldViewMatrixDirty )
				{
					inverseWorldViewMatrix = this.WorldViewMatrix.Inverse();
					inverseWorldViewMatrixDirty = false;
				}
				return inverseWorldViewMatrix;
			}
		}

		/// <summary>
		///    Gets/Sets the inverse of current concatenated view matrices.
		/// </summary>
		public Matrix4 InverseViewMatrix
		{
			get
			{
				return this.ViewMatrix.Inverse();
			}
		}

		/// <summary>
		///    Gets/Sets the inverse, transpose of current concatenated view matrices.
		/// </summary>
		public Matrix4 InverseTransposeViewMatrix
		{
			get
			{
				return this.InverseViewMatrix.Transpose();
			}
		}

		/// <summary>
		///    Gets/Sets the inverse of current concatenated world and view matrices.
		/// </summary>
		public Matrix4 InverseTransposeWorldViewMatrix
		{
			get
			{
				if ( inverseTransposeWorldViewMatrixDirty )
				{
					inverseTransposeWorldViewMatrix = this.InverseWorldViewMatrix.Transpose();
					inverseTransposeWorldViewMatrixDirty = false;
				}
				return inverseTransposeWorldViewMatrix;
			}
		}

		/// <summary>
		///    Gets/Sets the position of the current camera in object space relative to the current renderable.
		/// </summary>
		public Vector4 CameraPositionObjectSpace
		{
			get
			{
				if ( cameraPositionObjectSpaceDirty )
				{
					cameraPositionObjectSpace = (Vector4)( this.InverseWorldMatrix * camera.DerivedPosition );
					cameraPositionObjectSpaceDirty = false;
				}
				return cameraPositionObjectSpace;
			}
		}

		/// <summary>
		///    Gets the position of the current camera in world space.
		/// </summary>
		public Vector4 CameraPosition
		{
			get
			{
                /*
                if (mCameraPositionDirty)
                {
                    var vec3 = camera.DerivedPosition;
                    if (mCameraRelativeRendering)
                    {
                        vec3 -= mCameraRelativePosition;
                    }
                    mCameraPosition[0] = vec3[0];
                    mCameraPosition[1] = vec3[1];
                    mCameraPosition[2] = vec3[2];
                    mCameraPosition[3] = 1.0;
                    mCameraPositionDirty = false;
                }
                return mCameraPosition;
                 */

			    var v = camera.DerivedDirection;
                return new Vector4(v.x, v.y, v.z, 1.0f);
			}
		}

		/// <summary>
		///    Gets/Sets the current projection matrix.
		/// </summary>
		public Matrix4 ProjectionMatrix
		{
			get
			{
				if ( projMatrixDirty )
				{
					// NB use API-independent projection matrix since GPU programs
					// bypass the API-specific handedness and use right-handed coords
					if ( renderable != null && renderable.UseIdentityProjection )
					{
						// Use identity projection matrix, still need to take RS depth into account
                        Root.Instance.RenderSystem.ConvertProjectionMatrix(Matrix4.Identity, out projectionMatrix, true);
					}
					else
					{
						projectionMatrix = camera.ProjectionMatrixRSDepth;
					}
					if ( currentRenderTarget != null && currentRenderTarget.RequiresTextureFlipping )
					{
						projectionMatrix.m10 = -projectionMatrix.m10;
						projectionMatrix.m11 = -projectionMatrix.m11;
						projectionMatrix.m12 = -projectionMatrix.m12;
						projectionMatrix.m13 = -projectionMatrix.m12;
					}
					projMatrixDirty = false;
				}
				return projectionMatrix;
			}
		}

		/// <summary>
		///    Gets/Sets the current view matrix.
		/// </summary>
		public Matrix4 ViewMatrix
		{
			get
			{
				if ( viewMatrixDirty )
				{
					if ( renderable != null && renderable.UseIdentityView )
						viewMatrix = Matrix4.Identity;
					else
						viewMatrix = camera.ViewMatrix;
					viewMatrixDirty = false;
				}
				return viewMatrix;
			}
		}

		/// <summary>
		///		Gets the projection and view matrices concatenated.
		/// </summary>
		public Matrix4 ViewProjectionMatrix
		{
			get
			{
				if ( viewProjMatrixDirty )
				{
					viewProjMatrix = this.ProjectionMatrix * this.ViewMatrix;
					viewProjMatrixDirty = false;
				}

				return viewProjMatrix;
			}
		}

		/// <summary>
		///		Gets the current texture * view * projection matrix.
		/// </summary>
		public Matrix4 TextureViewProjectionMatrix
		{
			get
			{
				if ( textureViewProjMatrixDirty )
				{
					textureViewProjMatrix =
						ProjectionClipSpace2DToImageSpacePerspective *
						currentTextureProjector.ProjectionMatrixRSDepth *
						currentTextureProjector.ViewMatrix;

					textureViewProjMatrixDirty = false;
				}

				return textureViewProjMatrix;
			}
		}

		/// <summary>
		///		Get the extrusion distance for shadow volume vertices.
		/// </summary>
		public float ShadowExtrusionDistance
		{
			get
			{
				// only ever applies to one light at once
				Light light = GetLight( 0 );

				if ( light.Type == LightType.Directional )
				{
					// use constant value
					return dirLightExtrusionDistance;
				}
				else
				{
					// Calculate based on object space light distance
					// compared to light attenuation range
					var objPos = this.InverseWorldMatrix.TransformAffine( light.GetDerivedPosition(true) );
					return light.AttenuationRange - objPos.Length;
				}
			}
		}

		/// <summary>
		/// Get the derived camera position (which includes any parent sceneNode transforms)
		/// </summary>
		public Vector3 ViewDirection
		{
			get
			{
				return camera.DerivedDirection;
			}
		}

		/// <summary>
		/// Get the derived camera right vector (which includes any parent sceneNode transforms)
		/// </summary>
		public Vector3 ViewSideVector
		{
			get
			{
				return camera.DerivedRight;
			}
		}

		/// <summary>
		/// Get the derived camera up vector (which includes any parent sceneNode transforms)
		/// </summary>
		public Vector3 ViewUpVector
		{
			get
			{
				return camera.DerivedUp;
			}
		}

		public float NearClipDistance
		{
			get
			{
				return camera.Near;
			}
		}

		public float FarClipDistance
		{
			get
			{
				return camera.Far;
			}
		}

		public float Time
		{
			get
			{
				return ControllerManager.Instance.GetElapsedTime();
			}
		}

		public Vector4 MVShadowTechnique
		{
			get
			{
				return mvShadowTechnique;
			}
			set
			{
				mvShadowTechnique = value;
			}
		}

		/// <summary>
		/// The technique pass number
		/// </summary>
		public int PassNumber
		{
			get
			{
				return passNumber;
			}
			set
			{
				passNumber = value;
			}
		}

	    public virtual Matrix4 InverseTransposeWorldMatrix
	    {
	        get
	        {
                if (inverseTransposeWorldMatrixDirty)
                {
                    inverseTransposeWorldMatrix = InverseWorldMatrix.Transpose();
                    inverseTransposeWorldMatrixDirty = false;
                }
                return inverseTransposeWorldMatrix;
	        }
	    }

	    public IRenderable CurrentRenderable
	    {
	        get
	        {
                return renderable;
	        }
	    }

	    #endregion

        #region GetLightAs4DVector

        [OgreVersion(1, 7, 2790)]
	    public virtual Vector4 GetLightAs4DVector( int index )
	    {
            return GetLight(index).GetAs4DVector(true);
	    }

        #endregion

        #region GetLightDiffuseColor

        [OgreVersion(1, 7, 2790)]
	    public virtual ColorEx GetLightDiffuseColor( int index )
	    {
            return GetLight(index).Diffuse;
        }

        #endregion

        #region GetLightSpecularColor

        [OgreVersion(1, 7, 2790)]
	    public ColorEx GetLightSpecularColor( int index )
	    {
            return GetLight(index).Specular;
        }

        #endregion
    }
}
