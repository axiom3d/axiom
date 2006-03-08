#region LGPL License
/*
Axiom Game Engine Library
Copyright (C) 2003  Axiom Project Team

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

using System;
using System.Collections;
using System.Diagnostics;

using Axiom.MathLib;
// This is coming from RealmForge.Utility
using Axiom.Core;

namespace Axiom
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
    public class AutoParamDataSource
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
        ///    Current view matrix;
        /// </summary>
        protected Matrix4 viewMatrix;
        /// <summary>
        ///    Current projection matrix.
        /// </summary>
        protected Matrix4 projectionMatrix;
        /// <summary>
        ///    Current view and projection matrices concatenated.
        /// </summary>
        protected Matrix4 viewProjMatrix;
        /// <summary>
        ///    Array of world matrices for the current renderable.
        /// </summary>
        protected Matrix4[] worldMatrix = new Matrix4[256];
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
        protected bool cameraPositionObjectSpaceDirty;
        protected bool textureViewProjMatrixDirty;

        protected Matrix4 ProjectionClipSpace2DToImageSpacePerspective = new Matrix4(
            0.5f, 0, 0, -0.5f,
            0, -0.5f, 0, -0.5f,
            0, 0, 0, 1,
            0, 0, 0, 1 );

        #endregion Fields

        #region Constructors

        /// <summary>
        ///    Default constructor.
        /// </summary>
        public AutoParamDataSource()
        {
            worldMatrixDirty = true;
            worldViewMatrixDirty = true;
            worldViewProjMatrixDirty = true;
            viewProjMatrixDirty = true;
            inverseWorldMatrixDirty = true;
            inverseWorldViewMatrixDirty = true;
            inverseViewMatrixDirty = true;
            cameraPositionObjectSpaceDirty = true;
            textureViewProjMatrixDirty = true;

            // defaults for the blank light
            blankLight.Diffuse = ColorEx.Black;
            blankLight.Specular = ColorEx.Black;
            blankLight.SetAttenuation( 0, 0, 0, 0 );
        }

        #endregion

        #region Methods

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
                return currentLightList[index];
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
                worldViewMatrixDirty = true;
                worldViewProjMatrixDirty = true;
                viewProjMatrixDirty = true;
                inverseWorldMatrixDirty = true;
                inverseViewMatrixDirty = true;
                cameraPositionObjectSpaceDirty = true;
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
                worldViewMatrixDirty = true;
                worldViewProjMatrixDirty = true;
                viewProjMatrixDirty = true;
                inverseWorldMatrixDirty = true;
                inverseViewMatrixDirty = true;
                cameraPositionObjectSpaceDirty = true;
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

                return worldMatrix[0];
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
                if ( inverseViewMatrixDirty )
                {
                    inverseViewMatrix = this.ViewMatrix.Inverse();
                    inverseViewMatrixDirty = false;
                }
                return inverseViewMatrix;
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
        ///    Gets/Sets the current projection matrix.
        /// </summary>
        public Matrix4 ProjectionMatrix
        {
            get
            {
                projectionMatrix = camera.StandardProjectionMatrix;

                // // Because we're not using setProjectionMatrix, this needs to be done here
                if ( currentRenderTarget != null && currentRenderTarget.RequiresTextureFlipping )
                {
                    projectionMatrix.m11 = -projectionMatrix.m11;
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
                return camera.ViewMatrix;
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
                        currentTextureProjector.ViewMatrix *
                        currentTextureProjector.StandardProjectionMatrix;

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
                    Vector3 objPos = this.InverseWorldMatrix * light.DerivedPosition;
                    return light.AttenuationRange - objPos.Length;
                }
            }
        }

        #endregion
    }
}
