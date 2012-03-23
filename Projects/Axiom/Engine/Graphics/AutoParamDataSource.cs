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
using System.Diagnostics;

using Axiom.Configuration;
using Axiom.Controllers;
using Axiom.Core;
using Axiom.Core.Collections;
using Axiom.Math;

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
		protected IRenderable currentRenderable;

		/// <summary>
		///    Current camera being used for rendering.
		/// </summary>
		protected Camera currentCamera;

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

		protected bool viewMatrixDirty = true;

		/// <summary>
		///    Current projection matrix.
		/// </summary>
		protected Matrix4 projectionMatrix;

		protected bool projMatrixDirty = true;

		protected Matrix4 inverseTransposeWorldMatrix;
		protected bool inverseTransposeWorldMatrixDirty = true;

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

		protected Matrix4[] worldMatrixArray;

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
		///		Distance to extrude shadow volume vertices.
		/// </summary>
		protected Real dirLightExtrusionDistance;

		protected Vector4 cameraPosition;

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
		///    List of lights that are in the scene and near the current renderable.
		/// </summary>
		protected LightList currentLightList = new LightList();

		/// <summary>
		///    Blank light to use when a higher index light is requested than is available.
		/// </summary>
		protected Light blankLight = new Light();

		protected bool viewProjMatrixDirty = true;
		protected bool worldMatrixDirty = true;
		protected bool worldViewMatrixDirty = true;
		protected bool worldViewProjMatrixDirty = true;
		protected bool inverseWorldMatrixDirty = true;
		protected bool inverseWorldViewMatrixDirty = true;
		protected bool inverseViewMatrixDirty = true;
		protected bool inverseTransposeWorldViewMatrixDirty = true;
		protected bool cameraPositionDirty = true;
		protected bool cameraPositionObjectSpaceDirty = true;

		/// <summary>
		///		Current texture view projection matrix.
		/// </summary>
		protected Matrix4[] textureViewProjMatrix = new Matrix4[ Config.MaxSimultaneousLights ];

		protected Matrix4[] textureWorldViewProjMatrix = new Matrix4[ Config.MaxSimultaneousLights ];
		protected Matrix4[] spotlightViewProjMatrix = new Matrix4[ Config.MaxSimultaneousLights ];
		protected Matrix4[] spotlightWorldViewProjMatrix = new Matrix4[ Config.MaxSimultaneousLights ];
		protected bool[] textureViewProjMatrixDirty = new bool[ Config.MaxSimultaneousLights ];
		protected bool[] textureWorldViewProjMatrixDirty = new bool[ Config.MaxSimultaneousLights ];
		protected bool[] spotlightViewProjMatrixDirty = new bool[ Config.MaxSimultaneousLights ];
		protected bool[] spotlightWorldViewProjMatrixDirty = new bool[ Config.MaxSimultaneousLights ];
		protected bool[] shadowCamDepthRangesDirty = new bool[ Config.MaxSimultaneousLights ];
		protected Frustum[] currentTextureProjector = new Frustum[ Config.MaxSimultaneousLights ];

		protected Vector4 sceneDepthRange;
		protected Vector4 lodCameraPosition;
		protected Vector4 lodCameraPositionObjectSpace;
		protected bool sceneDepthRangeDirty = true;
		protected bool lodCameraPositionDirty = true;
		protected bool lodCameraPositionObjectSpaceDirty = true;
		protected Vector3 cameraRelativePosition;
		protected bool cameraRelativeRendering;

		[OgreVersion( 1, 7, 2 )]
		protected Matrix4 ProjectionClipSpace2DToImageSpacePerspective = new Matrix4( 0.5f, 0, 0, 0.5f, 0, -0.5f, 0, 0.5f, 0, 0, 1, 0, 0, 0, 0, 1 );

		protected int passNumber;
		protected Pass currentPass;
		protected ColorEx fogColor;

		#endregion Fields

		#region Constructors

		/// <summary>
		///    Default constructor.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public AutoParamDataSource()
			: base()
		{
			blankLight.Diffuse = ColorEx.Black;
			blankLight.Specular = ColorEx.Black;
			blankLight.SetAttenuation( 0, 1, 0, 0 );

			for ( int i = 0; i < Config.MaxSimultaneousLights; ++i )
			{
				textureViewProjMatrixDirty[ i ] = true;
				textureWorldViewProjMatrixDirty[ i ] = true;
				spotlightViewProjMatrixDirty[ i ] = true;
				spotlightWorldViewProjMatrixDirty[ i ] = true;
			}
		}

		#endregion Constructors

		#region Methods

		protected override void dispose( bool disposeManagedResources )
		{
			if ( !this.IsDisposed )
			{
				if ( disposeManagedResources )
				{
					if ( this.blankLight != null )
					{
						if ( !this.blankLight.IsDisposed )
						{
							this.blankLight.Dispose();
						}

						this.blankLight = null;
					}
				}
			}

			base.dispose( disposeManagedResources );
		}

		/// <summary>
		///    Get the light which is 'index'th closest to the current object 
		/// </summary>
		/// <param name="index">Ordinal value signifying the light to retreive, with 0 being closest, 1 being next closest, etc.</param>
		/// <returns>A light located near the current renderable.</returns>
		[OgreVersion( 1, 7, 2 )]
		protected Light GetLight( int index )
		{
			// If outside light range, return a blank light to ensure zeroised for program
			if ( currentLightList != null && index < currentLightList.Count )
			{
				return currentLightList[ index ];
			}
			else
			{
				return blankLight;
			}
		}

		/// <summary>
		/// Updates the current camera
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public virtual void SetCurrentCamera( Camera cam, bool useCameraRelative )
		{
			currentCamera = cam;

			// set the dirty flags to force updates
			cameraRelativeRendering = useCameraRelative;
			cameraRelativePosition = cam.DerivedPosition;
			viewMatrixDirty = true;
			projMatrixDirty = true;
			worldViewMatrixDirty = true;
			viewProjMatrixDirty = true;
			worldViewProjMatrixDirty = true;
			inverseViewMatrixDirty = true;
			inverseWorldViewMatrixDirty = true;
			inverseTransposeWorldViewMatrixDirty = true;
			cameraPositionObjectSpaceDirty = true;
			cameraPositionDirty = true;
			lodCameraPositionObjectSpaceDirty = true;
			lodCameraPositionDirty = true;
		}

		/// <summary>
		/// Sets the light list that should be used, and it's base index from the global list 
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public virtual void SetCurrentLightList( LightList lightList )
		{
			currentLightList = lightList;
			for ( int i = 0; i < lightList.Count && i < Config.MaxSimultaneousLights; ++i )
			{
				spotlightViewProjMatrixDirty[ i ] = true;
				spotlightWorldViewProjMatrixDirty[ i ] = true;
			}
		}

		/// <summary>
		/// Get the light which is 'index'th closest to the current object
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public virtual float GetLightNumber( int index )
		{
			return (float)this.GetLight( index ).IndexInFrame;
		}

		[OgreVersion( 1, 7, 2 )]
		public virtual ColorEx GetLightDiffuseColorWithPower( int index )
		{
			var l = this.GetLight( index );
			var scaled = l.Diffuse;
			Real power = l.PowerScale;
			// scale, but not alpha
			scaled.r *= power;
			scaled.g *= power;
			scaled.b *= power;
			return scaled;
		}

		[OgreVersion( 1, 7, 2 )]
		public virtual ColorEx GetLightSpecularColorWithPower( int index )
		{
			var l = this.GetLight( index );
			var scaled = l.Specular;
			Real power = l.PowerScale;
			// scale, but not alpha
			scaled.r *= power;
			scaled.g *= power;
			scaled.b *= power;
			return scaled;
		}

		[OgreVersion( 1, 7, 2 )]
		public virtual Vector3 GetLightPosition( int index )
		{
			return this.GetLight( index ).GetDerivedPosition( true );
		}

		[OgreVersion( 1, 7, 2790 )]
		public virtual Vector4 GetLightAs4DVector( int index )
		{
			return GetLight( index ).GetAs4DVector( true );
		}

		[OgreVersion( 1, 7, 2 )]
		public virtual Vector3 GetLightDirection( int index )
		{
			return this.GetLight( index ).DerivedDirection;
		}

		/// <param name="index">Ordinal value signifying the light to retreive. <see cref="GetLight"/></param>
		[OgreVersion( 1, 7, 2 )]
		public virtual Real GetLightPowerScale( int index )
		{
			return this.GetLight( index ).PowerScale;
		}

		[OgreVersion( 1, 7, 2790 )]
		public virtual ColorEx GetLightDiffuse( int index )
		{
			return this.GetLight( index ).Diffuse;
		}

		[OgreVersion( 1, 7, 2790 )]
		public virtual ColorEx GetLightSpecular( int index )
		{
			return this.GetLight( index ).Specular;
		}

		[OgreVersion( 1, 7, 2 )]
		public virtual Vector4 GetLightAttenuation( int index )
		{
			// range, const, linear, quad
			var l = this.GetLight( index );
			return new Vector4( l.AttenuationRange, l.AttenuationConstant, l.AttenuationLinear, l.AttenuationQuadratic );
		}

		[OgreVersion( 1, 7, 2 )]
		public virtual Vector4 GetSpotlightParams( int index )
		{
			// inner, outer, fallof, isSpot
			var l = this.GetLight( index );
			if ( l.Type == LightType.Spotlight )
			{
				return new Vector4( Utility.Cos( l.SpotlightInnerAngle * 0.5 ), Utility.Cos( l.SpotlightOuterAngle * 0.5 ), l.SpotlightFalloff, 1.0 );
			}
			else
			{
				// Use safe values which result in no change to point & dir light calcs
				// The spot factor applied to the usual lighting calc is 
				// pow((dot(spotDir, lightDir) - y) / (x - y), z)
				// Therefore if we set z to 0.0f then the factor will always be 1
				// since pow(anything, 0) == 1
				// However we also need to ensure we don't overflow because of the division
				// therefore set x = 1 and y = 0 so divisor doesn't change scale
				return new Vector4( 1.0, 0.0, 0.0, 1.0 ); // since the main op is pow(.., vec4.z), this will result in 1.0
			}
		}

		//TODO
		//public virtual void SetMainCamBoundsInfo(VisibleObjectsBoundsInfo* info)
		//{
		//    mMainCamBoundsInfo = info;
		//    mSceneDepthRangeDirty = true;
		//}

		[OgreVersion( 1, 7, 2 )]
		public virtual void SetWorldMatrices( Matrix4[] m, int count )
		{
			worldMatrixArray = m;
			worldMatrixCount = count;
			worldMatrixDirty = false;
		}

		[OgreVersion( 1, 7, 2 )]
		public virtual float GetLightCastsShadows( int index )
		{
			return this.GetLight( index ).CastShadows ? 1.0f : 0.0f;
		}

		[OgreVersion( 1, 7, 2 )]
		public virtual Vector4 GetTextureSize( int index )
		{
			Vector4 size = (Real)1;

			if ( index < currentPass.TextureUnitStatesCount )
			{
				Texture tex = currentPass.GetTextureUnitState( index ).Texture;
				if ( tex != null )
				{
					size.x = (Real)tex.Width;
					size.y = (Real)tex.Height;
					size.z = (Real)tex.Depth;
				}
			}

			return size;
		}

		[OgreVersion( 1, 7, 2 )]
		public virtual Vector4 GetInverseTextureSize( int index )
		{
			Vector4 size = this.GetTextureSize( index );
			return 1 / size;
		}

		[OgreVersion( 1, 7, 2 )]
		public virtual Vector4 GetPackedTextureSize( int index )
		{
			Vector4 size = this.GetTextureSize( index );
			return new Vector4( size.x, size.y, 1 / size.x, 1 / size.y );
		}

		[OgreVersion( 1, 7, 2 )]
		public virtual void SetFog( FogMode mode, ColorEx color, Real expDensity, Real linearStart, Real linearEnd )
		{
			// mode ignored
			fogColor = color;
			fogParams.x = expDensity;
			fogParams.y = linearStart;
			fogParams.z = linearEnd;
			fogParams.w = linearEnd != linearStart ? 1 / ( linearEnd - linearStart ) : 0;
		}

		public void SetTextureProjector( Frustum frust )
		{
			this.SetTextureProjector( frust, 0 );
		}

		[OgreVersion( 1, 7, 2 )]
		public virtual void SetTextureProjector( Frustum frust, int index )
		{
			if ( index < Config.MaxSimultaneousLights )
			{
				currentTextureProjector[ index ] = frust;
				textureViewProjMatrixDirty[ index ] = true;
				textureWorldViewProjMatrixDirty[ index ] = true;
				shadowCamDepthRangesDirty[ index ] = true;
			}
		}

		[OgreVersion( 1, 7, 2 )]
		public virtual Real GetTime_0_X( Real x )
		{
			return this.Time % x;
		}

		[OgreVersion( 1, 7, 2 )]
		public virtual Real GetCosTime_0_X( Real x )
		{
			return Math.Utility.Cos( this.GetTime_0_X( x ) );
		}

		[OgreVersion( 1, 7, 2 )]
		public virtual Real GetSinTime_0_X( Real x )
		{
			return Math.Utility.Sin( this.GetTime_0_X( x ) );
		}

		[OgreVersion( 1, 7, 2 )]
		public virtual Real GetTanTime_0_X( Real x )
		{
			return Math.Utility.Tan( this.GetTime_0_X( x ) );
		}

		[OgreVersion( 1, 7, 2 )]
		public virtual Vector4 GetTime_0_X_Packed( Real x )
		{
			Real t = this.GetTime_0_X( x );
			return new Vector4( t, Math.Utility.Sin( t ), Math.Utility.Cos( t ), Math.Utility.Tan( t ) );
		}

		[OgreVersion( 1, 7, 2 )]
		public virtual Real GetTime_0_1( Real x )
		{
			return this.GetTime_0_X( x ) / x;
		}

		[OgreVersion( 1, 7, 2 )]
		public virtual Real GetCosTime_0_1( Real x )
		{
			return Math.Utility.Cos( this.GetTime_0_1( x ) );
		}

		[OgreVersion( 1, 7, 2 )]
		public virtual Real GetSinTime_0_1( Real x )
		{
			return Math.Utility.Sin( this.GetTime_0_1( x ) );
		}

		[OgreVersion( 1, 7, 2 )]
		public virtual Real GetTanTime_0_1( Real x )
		{
			return Math.Utility.Tan( this.GetTime_0_1( x ) );
		}

		[OgreVersion( 1, 7, 2 )]
		public virtual Vector4 GetTime_0_1_Packed( Real x )
		{
			Real t = this.GetTime_0_1( x );
			return new Vector4( t, Math.Utility.Sin( t ), Math.Utility.Cos( t ), Math.Utility.Tan( t ) );
		}

		[OgreVersion( 1, 7, 2 )]
		public virtual Real GetTime_0_2Pi( Real x )
		{
			return this.GetTime_0_X( x ) / x * Math.Utility.TWO_PI;
		}

		[OgreVersion( 1, 7, 2 )]
		public virtual Real GetCosTime_0_2Pi( Real x )
		{
			return Math.Utility.Cos( this.GetTime_0_2Pi( x ) );
		}

		[OgreVersion( 1, 7, 2 )]
		public virtual Real GetSinTime_0_2Pi( Real x )
		{
			return Math.Utility.Sin( this.GetTime_0_2Pi( x ) );
		}

		[OgreVersion( 1, 7, 2 )]
		public virtual Real GetTanTime_0_2Pi( Real x )
		{
			return Math.Utility.Tan( this.GetTime_0_2Pi( x ) );
		}

		[OgreVersion( 1, 7, 2 )]
		public virtual Vector4 GetTime_0_2Pi_Packed( Real x )
		{
			Real t = this.GetTime_0_2Pi( x );
			return new Vector4( t, Math.Utility.Sin( t ), Math.Utility.Cos( t ), Math.Utility.Tan( t ) );
		}

		/// <summary>
		///		Gets the selected texture * view * projection matrix.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public virtual Matrix4 GetTextureViewProjectionMatrix( int index )
		{
			if ( index < Config.MaxSimultaneousLights )
			{
				if ( textureViewProjMatrixDirty[ index ] && currentTextureProjector[ index ] != null )
				{
					if ( cameraRelativeRendering )
					{
						// World positions are now going to be relative to the camera position
						// so we need to alter the projector view matrix to compensate
						Matrix4 view;
						currentTextureProjector[ index ].CalcViewMatrixRelative( currentCamera.DerivedPosition, out view );
						textureViewProjMatrix[ index ] = ProjectionClipSpace2DToImageSpacePerspective * currentTextureProjector[ index ].ProjectionMatrixRSDepth * view;
					}
					else
					{
						textureViewProjMatrix[ index ] = ProjectionClipSpace2DToImageSpacePerspective * currentTextureProjector[ index ].ProjectionMatrixRSDepth * currentTextureProjector[ index ].ViewMatrix;
					}
					textureViewProjMatrixDirty[ index ] = false;
				}

				return textureViewProjMatrix[ index ];
			}
			else
			{
				return Matrix4.Identity;
			}
		}

		[OgreVersion( 1, 7, 2 )]
		public virtual Matrix4 GetTextureWorldViewProjMatrix( int index )
		{
			if ( index < Config.MaxSimultaneousLights )
			{
				if ( textureWorldViewProjMatrixDirty[ index ] && currentTextureProjector[ index ] != null )
				{
					textureWorldViewProjMatrix[ index ] = this.GetTextureViewProjectionMatrix( index ) * this.WorldMatrix;
					textureWorldViewProjMatrixDirty[ index ] = false;
				}

				return textureWorldViewProjMatrix[ index ];
			}
			else
			{
				return Matrix4.Identity;
			}
		}

		[OgreVersion( 1, 7, 2 )]
		public virtual Matrix4 GetSpotlightViewProjMatrix( int index )
		{
			if ( index < Config.MaxSimultaneousLights )
			{
				Light l = this.GetLight( index );

				if ( l != blankLight && l.Type == LightType.Spotlight && spotlightViewProjMatrixDirty[ index ] )
				{
					Frustum frust = new Frustum();
					SceneNode dummyNode = new SceneNode( null );
					dummyNode.AttachObject( frust );

					frust.ProjectionType = Projection.Perspective;
					frust.FieldOfView = l.SpotlightOuterAngle;
					frust.AspectRatio = 1.0f;
					// set near clip the same as main camera, since they are likely
					// to both reflect the nature of the scene
					frust.Near = currentCamera.Near;
					// Calculate position, which same as spotlight position, in camera-relative coords if required
					dummyNode.Position = l.GetDerivedPosition( true );
					// Calculate direction, which same as spotlight direction
					Vector3 dir = -l.DerivedDirection; // backwards since point down -z
					dir.Normalize();
					Vector3 up = Vector3.UnitY;
					// Check it's not coincident with dir
					if ( Math.Utility.Abs( up.Dot( dir ) ) >= 1.0f )
					{
						// Use camera up
						up = Vector3.UnitZ;
					}
					// cross twice to rederive, only direction is unaltered
					Vector3 left = dir.Cross( up );
					left.Normalize();
					up = dir.Cross( left );
					up.Normalize();
					// Derive quaternion from axes
					Quaternion q = Quaternion.FromAxes( left, up, dir );
					dummyNode.Orientation = q;

					// The view matrix here already includes camera-relative changes if necessary
					// since they are built into the frustum position
					spotlightViewProjMatrix[ index ] = ProjectionClipSpace2DToImageSpacePerspective * frust.ProjectionMatrixRSDepth * frust.ViewMatrix;

					spotlightViewProjMatrixDirty[ index ] = false;
				}
				return spotlightViewProjMatrix[ index ];
			}
			else
			{
				return Matrix4.Identity;
			}
		}

		[OgreVersion( 1, 7, 2 )]
		public virtual Matrix4 GetSpotlightWorldViewProjMatrix( int index )
		{
			if ( index < Config.MaxSimultaneousLights )
			{
				Light l = this.GetLight( index );

				if ( l != blankLight && l.Type == LightType.Spotlight && spotlightWorldViewProjMatrixDirty[ index ] )
				{
					spotlightWorldViewProjMatrix[ index ] = this.GetSpotlightViewProjMatrix( index ) * this.WorldMatrix;
					spotlightWorldViewProjMatrixDirty[ index ] = false;
				}
				return spotlightWorldViewProjMatrix[ index ];
			}
			else
			{
				return Matrix4.Identity;
			}
		}

		[OgreVersion( 1, 7, 2 )]
		public virtual Matrix4 GetTextureTransformMatrix( int index )
		{
			// make sure the current pass is set
			Debug.Assert( currentPass != null, "current pass is NULL!" );
			// check if there is a texture unit with the given index in the current pass
			if ( index < currentPass.TextureUnitStatesCount )
			{
				// texture unit existent, return its currently set transform
				return currentPass.GetTextureUnitState( index ).TextureMatrix;
			}
			else
			{
				// no such texture unit, return unity
				return Matrix4.Identity;
			}
		}

		public virtual Vector4 GetShadowSceneDepthRange( int index )
		{
			throw new NotImplementedException();
			//Vector4 dummy = new Vector4( 0, 100000, 100000, 1 / 100000 );

			//if ( !CurrentSceneManager.IsShadowTechniqueTextureBased )
			//    return dummy;

			//if ( index < Config.MaxSimultaneousLights )
			//{
			//    if ( shadowCamDepthRangesDirty[ index ] && currentTextureProjector[ index ] != null )
			//    {
			//        VisibleObjectsBoundsInfo info = CurrentSceneManager.GetVisibleObjectsBoundsInfo( (Camera)currentTextureProjector[ index ] );

			//        Real depthRange = info.maxDistanceInFrustum - info.minDistanceInFrustum;
			//        if ( depthRange > Real.Epsilon )
			//        {
			//            shadowCamDepthRanges[ index ] = new Vector4(
			//                info.minDistanceInFrustum,
			//                info.maxDistanceInFrustum,
			//                depthRange,
			//                1.0f / depthRange );
			//        }
			//        else
			//        {
			//            shadowCamDepthRanges[ index ] = dummy;
			//        }

			//        shadowCamDepthRangesDirty[ index ] = false;
			//    }
			//    return shadowCamDepthRanges[ index ];
			//}
			//else
			//    return dummy;
		}

		[OgreVersion( 1, 7, 2 )]
		public virtual void UpdateLightCustomGpuParameter( GpuProgramParameters.AutoConstantEntry constantEntry, GpuProgramParameters parameters )
		{
			ushort lightIndex = (ushort)( constantEntry.Data & 0xFFFF );
			ushort paramIndex = (ushort)( ( constantEntry.Data >> 16 ) & 0xFFFF );
			if ( currentLightList != null && lightIndex < currentLightList.Count )
			{
				Light light = this.GetLight( lightIndex );
				light.UpdateCustomGpuParameter( paramIndex, constantEntry, parameters );
			}
		}

		#endregion Methods

		#region Properties

		[OgreVersion( 1, 7, 2 )]
		public virtual SceneManager CurrentSceneManager { get; set; }

		/// <summary>
		///    Gets/Sets the current renderable object.
		/// </summary>
		public virtual IRenderable CurrentRenderable
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return currentRenderable;
			}

			[OgreVersion( 1, 7, 2 )]
			set
			{
				currentRenderable = value;

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
				inverseTransposeWorldMatrixDirty = true;
				inverseTransposeWorldViewMatrixDirty = true;
				cameraPositionObjectSpaceDirty = true;
				lodCameraPositionObjectSpaceDirty = true;
				for ( int i = 0; i < Config.MaxSimultaneousLights; ++i )
				{
					textureWorldViewProjMatrixDirty[ i ] = true;
					spotlightWorldViewProjMatrixDirty[ i ] = true;
				}
			}
		}

		/// <summary>
		///		Get/Set the current active render target in use.
		/// </summary>
		public virtual RenderTarget CurrentRenderTarget
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return currentRenderTarget;
			}

			[OgreVersion( 1, 7, 2 )]
			set
			{
				currentRenderTarget = value;
			}
		}

		/// <summary>
		///		Get/Set the current active viewport in use.
		/// </summary>
		public virtual Viewport CurrentViewport
		{
			[OgreVersion( 1, 7, 2 )]
			set
			{
				currentViewport = value;
			}
		}

		/// <summary>
		///    Gets/Sets the current global ambient light color.
		/// </summary>
		public virtual ColorEx AmbientLight
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return ambientLight;
			}

			[OgreVersion( 1, 7, 2 )]
			set
			{
				ambientLight = value;
			}
		}

		public virtual float LightCount
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return (float)currentLightList.Count;
			}
		}

		/// <summary>
		/// Gets/Sets the current pass
		/// </summary>
		public virtual Pass CurrentPass
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return currentPass;
			}

			[OgreVersion( 1, 7, 2 )]
			set
			{
				currentPass = value;
			}
		}

		public virtual ColorEx SurfaceAmbient
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return currentPass.Ambient;
			}
		}

		public virtual ColorEx SurfaceDiffuse
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return currentPass.Diffuse;
			}
		}

		public virtual ColorEx SurfaceSpecular
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return currentPass.Specular;
			}
		}

		public virtual ColorEx SurfaceEmissive
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return currentPass.Emissive;
			}
		}

		public virtual Real SurfaceShininess
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return currentPass.Shininess;
			}
		}

		public virtual ColorEx DerivedAmbient
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return this.AmbientLight * this.SurfaceAmbient;
			}
		}

		public virtual ColorEx DerivedSceneColor
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				ColorEx result = this.DerivedAmbient + this.SurfaceEmissive;
				result.a = this.SurfaceDiffuse.a;
				return result;
			}
		}

		public virtual ColorEx FogColor
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return fogColor;
			}
		}

		/// <summary>
		///    Gets/Sets the current gpu fog parameters.
		/// </summary>
		public virtual Vector4 FogParams
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return fogParams;
			}
		}

		/// <summary>
		///    Gets the current world matrix.
		/// </summary>
		public virtual Matrix4 WorldMatrix
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				if ( worldMatrixDirty )
				{
					worldMatrixArray = worldMatrix;
					currentRenderable.GetWorldTransforms( worldMatrix );
					worldMatrixCount = currentRenderable.NumWorldTransforms;
					if ( cameraRelativeRendering )
					{
						for ( int i = 0; i < worldMatrixCount; ++i )
						{
							worldMatrix[ i ].Translation = worldMatrix[ i ].Translation - cameraRelativePosition;
						}
					}
					worldMatrixDirty = false;
				}
				return worldMatrixArray[ 0 ];
			}
		}

		/// <summary>
		///    Gets the number of current world matrices.
		/// </summary>
		public virtual int WorldMatrixCount
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				// trigger derivation
				Matrix4 m = this.WorldMatrix;
				return worldMatrixCount;
			}
		}

		/// <summary>
		///		Gets an array with all the current world matrix transforms.
		/// </summary>
		public virtual Matrix4[] WorldMatrixArray
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				// trigger derivation
				Matrix4 m = this.WorldMatrix;
				return worldMatrixArray;
			}
		}

		/// <summary>
		///    Gets/Sets the current view matrix.
		/// </summary>
		public virtual Matrix4 ViewMatrix
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				if ( viewMatrixDirty )
				{
					if ( currentRenderable != null && currentRenderable.UseIdentityView )
					{
						viewMatrix = Matrix4.Identity;
					}
					else
					{
						viewMatrix = currentCamera.ViewMatrix;
						if ( cameraRelativeRendering )
						{
							viewMatrix.Translation = Vector3.Zero;
						}
					}
					viewMatrixDirty = false;
				}
				return viewMatrix;
			}
		}

		/// <summary>
		///		Gets the projection and view matrices concatenated.
		/// </summary>
		public virtual Matrix4 ViewProjectionMatrix
		{
			[OgreVersion( 1, 7, 2 )]
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
		///    Gets/Sets the current projection matrix.
		/// </summary>
		public virtual Matrix4 ProjectionMatrix
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				if ( projMatrixDirty )
				{
					// NB use API-independent projection matrix since GPU programs
					// bypass the API-specific handedness and use right-handed coords
					if ( currentRenderable != null && currentRenderable.UseIdentityProjection )
					{
						// Use identity projection matrix, still need to take RS depth into account
						Root.Instance.RenderSystem.ConvertProjectionMatrix( Matrix4.Identity, out projectionMatrix, true );
					}
					else
					{
						projectionMatrix = currentCamera.ProjectionMatrixRSDepth;
					}
					if ( currentRenderTarget != null && currentRenderTarget.RequiresTextureFlipping )
					{
						projectionMatrix.m10 = -projectionMatrix.m10;
						projectionMatrix.m11 = -projectionMatrix.m11;
						projectionMatrix.m12 = -projectionMatrix.m12;
						projectionMatrix.m13 = -projectionMatrix.m13;
					}
					projMatrixDirty = false;
				}
				return projectionMatrix;
			}
		}

		/// <summary>
		///    Gets/Sets the current concatenated world and view matrices.
		/// </summary>
		public virtual Matrix4 WorldViewMatrix
		{
			[OgreVersion( 1, 7, 2 )]
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
		public virtual Matrix4 WorldViewProjMatrix
		{
			[OgreVersion( 1, 7, 2 )]
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
		public virtual Matrix4 InverseWorldMatrix
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				if ( inverseWorldMatrixDirty )
				{
					inverseWorldMatrix = this.WorldMatrix.InverseAffine();
					inverseWorldMatrixDirty = false;
				}
				return inverseWorldMatrix;
			}
		}

		/// <summary>
		///    Gets/Sets the inverse of current concatenated world and view matrices.
		/// </summary>
		public virtual Matrix4 InverseWorldViewMatrix
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				if ( inverseWorldViewMatrixDirty )
				{
					inverseWorldViewMatrix = this.WorldViewMatrix.InverseAffine();
					inverseWorldViewMatrixDirty = false;
				}
				return inverseWorldViewMatrix;
			}
		}

		/// <summary>
		///    Gets/Sets the inverse of current concatenated view matrices.
		/// </summary>
		public virtual Matrix4 InverseViewMatrix
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				if ( inverseViewMatrixDirty )
				{
					inverseViewMatrix = this.ViewMatrix.InverseAffine();
					inverseViewMatrixDirty = false;
				}

				return inverseViewMatrix;
			}
		}

		/// <summary>
		///    Gets/Sets the inverse of current concatenated world and view matrices.
		/// </summary>
		public virtual Matrix4 InverseTransposeWorldViewMatrix
		{
			[OgreVersion( 1, 7, 2 )]
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

		public virtual Matrix4 InverseViewProjMatrix
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return this.ViewProjectionMatrix.Inverse();
			}
		}

		public virtual Matrix4 InverseTransposeViewProjMatrix
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return this.InverseViewProjMatrix.Transpose();
			}
		}

		public virtual Matrix4 TransposeViewProjMatrix
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return this.ViewProjectionMatrix.Transpose();
			}
		}

		public virtual Matrix4 TransposeViewMatrix
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return this.ViewMatrix.Transpose();
			}
		}

		public virtual Matrix4 InverseTransposeViewMatrix
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return this.InverseViewMatrix.Transpose();
			}
		}

		public virtual Matrix4 TransposeProjectionMatrix
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return this.ProjectionMatrix.Transpose();
			}
		}

		public virtual Matrix4 InverseProjectionMatrix
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return this.ProjectionMatrix.Inverse();
			}
		}

		public virtual Matrix4 InverseTransposeProjectionMatrix
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return this.InverseProjectionMatrix.Transpose();
			}
		}

		public virtual Matrix4 TransposeWorldViewProjMatrix
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return this.WorldViewProjMatrix.Transpose();
			}
		}

		public virtual Matrix4 InverseWorldViewProjMatrix
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return this.WorldViewProjMatrix.Inverse();
			}
		}

		public virtual Matrix4 InverseTransposeWorldViewProjMatrix
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return this.InverseWorldViewProjMatrix.Transpose();
			}
		}

		public virtual Matrix4 TransposeWorldViewMatrix
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return this.WorldViewMatrix.Transpose();
			}
		}

		public virtual Matrix4 TransposeWorldMatrix
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return this.WorldMatrix.Transpose();
			}
		}

		public virtual Real Time
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return ControllerManager.Instance.GetElapsedTime();
			}
		}

		public virtual Real FrameTime
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return ControllerManager.Instance.FrameTimeSource.Value;
			}
		}

		public virtual Real FPS
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return currentRenderTarget.LastFPS;
			}
		}

		public virtual Real ViewportWidth
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return (Real)currentViewport.ActualWidth;
			}
		}

		public virtual Real ViewportHeight
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return (Real)currentViewport.ActualHeight;
			}
		}

		public virtual Real InverseViewportWidth
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return 1.0f / currentViewport.ActualWidth;
			}
		}

		public virtual Real InverseViewportHeight
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return 1.0f / currentViewport.ActualHeight;
			}
		}

		public virtual Real FOV
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return currentCamera.FieldOfView;
			}
		}

		public virtual Vector4 CameraPosition
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				if ( cameraPositionDirty )
				{
					Vector3 vec3 = currentCamera.DerivedPosition;
					if ( cameraRelativeRendering )
					{
						vec3 -= cameraRelativePosition;
					}

					cameraPosition = vec3;
					cameraPositionDirty = false;
				}

				return cameraPosition;
			}
		}

		/// <summary>
		///    Gets/Sets the position of the current camera in object space relative to the current renderable.
		/// </summary>
		public virtual Vector4 CameraPositionObjectSpace
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				if ( cameraPositionObjectSpaceDirty )
				{
					if ( cameraRelativeRendering )
					{
						cameraPositionObjectSpace = this.InverseWorldMatrix.TransformAffine( Vector3.Zero );
					}
					else
					{
						cameraPositionObjectSpace = this.InverseWorldMatrix.TransformAffine( currentCamera.DerivedPosition );
					}

					cameraPositionObjectSpaceDirty = false;
				}
				return cameraPositionObjectSpace;
			}
		}

		public virtual Vector4 LodCameraPosition
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				if ( lodCameraPositionDirty )
				{
					Vector3 vec3 = currentCamera.LodCamera.DerivedPosition;
					if ( cameraRelativeRendering )
					{
						vec3 -= cameraRelativePosition;
					}

					lodCameraPosition = vec3;
					lodCameraPositionDirty = false;
				}

				return lodCameraPosition;
			}
		}

		public virtual Vector4 LodCameraPositionObjectSpace
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				if ( lodCameraPositionObjectSpaceDirty )
				{
					Vector3 trans = currentCamera.LodCamera.DerivedPosition;

					if ( cameraRelativeRendering )
					{
						trans -= cameraRelativePosition;
					}

					this.InverseWorldMatrix.TransformAffine( trans );
					lodCameraPositionObjectSpaceDirty = false;
				}

				return lodCameraPositionObjectSpace;
			}
		}

		/// <summary>
		/// Get the derived camera position (which includes any parent sceneNode transforms)
		/// </summary>
		public virtual Vector3 ViewDirection
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return currentCamera.DerivedDirection;
			}
		}

		/// <summary>
		/// Get the derived camera right vector (which includes any parent sceneNode transforms)
		/// </summary>
		public virtual Vector3 ViewSideVector
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return currentCamera.DerivedRight;
			}
		}

		/// <summary>
		/// Get the derived camera up vector (which includes any parent sceneNode transforms)
		/// </summary>
		public virtual Vector3 ViewUpVector
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return currentCamera.DerivedUp;
			}
		}

		public virtual Real NearClipDistance
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return currentCamera.Near;
			}
		}

		public virtual Real FarClipDistance
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return currentCamera.Far;
			}
		}

		/// <summary>
		/// The technique pass number
		/// </summary>
		public virtual int PassNumber
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return passNumber;
			}

			[OgreVersion( 1, 7, 2 )]
			set
			{
				passNumber = value;
			}
		}

		public virtual Vector4 SceneDepthRange
		{
			get
			{
				throw new NotImplementedException();
				//Vector4 dummy = new Vector4( 0, 100000, 100000, 1 / 100000 );

				//if ( sceneDepthRangeDirty )
				//{
				//    // calculate depth information
				//    Real depthRange = mainCamBoundsInfo.MaxDistanceInFrustum - mainCamBoundsInfo.MinDistanceInFrustum;
				//    if ( depthRange > Real.Epsilon )
				//    {
				//        sceneDepthRange = new Vector4(
				//            mainCamBoundsInfo->minDistanceInFrustum,
				//            mainCamBoundsInfo->maxDistanceInFrustum,
				//            depthRange,
				//            1.0f / depthRange );
				//    }
				//    else
				//    {
				//        sceneDepthRange = dummy;
				//    }
				//    sceneDepthRangeDirty = false;
				//}

				//return sceneDepthRange;
			}
		}

		public virtual ColorEx ShadowColor
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return this.CurrentSceneManager.ShadowColor;
			}
		}

		public virtual Matrix4 InverseTransposeWorldMatrix
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				if ( inverseTransposeWorldMatrixDirty )
				{
					inverseTransposeWorldMatrix = InverseWorldMatrix.Transpose();
					inverseTransposeWorldMatrixDirty = false;
				}
				return inverseTransposeWorldMatrix;
			}
		}

		/// <summary>
		///	Get / Sets the constant extrusion distance for directional lights.
		/// </summary>
		public virtual Real ShadowExtrusionDistance
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				var l = this.GetLight( 0 ); // only ever applies to one light at once
				if ( l.Type == LightType.Directional )
				{
					// use constant
					return dirLightExtrusionDistance;
				}
				else
				{
					// Calculate based on object space light distance
					// compared to light attenuation range
					var objPos = this.InverseWorldMatrix.TransformAffine( l.GetDerivedPosition( true ) );
					return l.AttenuationRange - objPos.Length;
				}
			}

			[OgreVersion( 1, 7, 2 )]
			set
			{
				dirLightExtrusionDistance = value;
			}
		}

		#endregion Properties
	}
}
