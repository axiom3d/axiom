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
using Axiom.Graphics;
using Axiom.Math;

#endregion Namespace Declarations

#pragma warning disable 628

namespace Axiom.Core
{
	// Viewport has been sealed in Axiom
	// In order to maintain future changes members that have
	// been protected in Ogre have not been made private here.
	// All properties in Ogre were nonvirtual except RenderQueueSequence,
	// which has been changed to nonvirtual.

	/// <summary>
	///		Summary description for Viewport.
	///		An abstraction of a viewport, i.e. a rendering region on a render
	///		target.
	///	</summary>
	///	<remarks>
	///		A viewport is the meeting of a camera and a rendering surface -
	///		the camera renders the scene from a viewpoint, and places its
	///		results into some subset of a rendering target, which may be the
	///		whole surface or just a part of the surface. Each viewport has a
	///		single camera as source and a single target as destination. A
	///		camera only has 1 viewport, but a render target may have several.
	///		A viewport also has a Z-order, i.e. if there is more than one
	///		viewport on a single render target and they overlap, one must
	///		obscure the other in some predetermined way.
	///	</remarks>
	public sealed class Viewport : DisposableObject
	{
		#region events

		/// <summary>
		///    Delegate for Viewport update events.
		/// </summary>
		public delegate void ViewportEventHandler( ViewportEventArgs e );

		/// <summary>
		///    Event arguments for render target updates.
		/// </summary>
		public class ViewportEventArgs : EventArgs
		{
			public Viewport Source
			{
				get;
				internal set;
			}

			public ViewportEventArgs( Viewport source )
			{
				Source = source;
			}
		}

		/// <summary>
		/// Notification of when a new camera is set to target listening Viewport.
		/// </summary>
		[OgreVersion( 1, 7, 2790, "Merged from Listener subclass" )]
		public event ViewportEventHandler ViewportCameraChanged;

		/// <summary>
		/// Notification of when target listening Viewport's dimensions changed.
		/// </summary>
		[OgreVersion( 1, 7, 2790, "Merged from Listener subclass" )]
		public event ViewportEventHandler ViewportDimensionsChanged;

#pragma warning disable 67
		/// <summary>
		/// Notification of when target listening Viewport's is destroyed.
		/// </summary>
		[OgreVersion( 1, 7, 2790, "Merged from Listener subclass; This aint ever fired within Ogre" )]
		public event ViewportEventHandler ViewportDestroyed;
#pragma warning restore 67

		#endregion

		#region Fields and Properties

		#region Camera Property

		[OgreVersion( 1, 7, 2790 )]
		private Camera _camera;
		/// <summary>
		///		Retrieves a reference to the camera for this viewport.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public Camera Camera
		{
			get
			{
				return _camera;
			}
			set
			{
				if ( _camera != null )
				{
					if ( _camera.Viewport == this )
					{
						_camera.NotifyViewport( null );
					}
				}

				_camera = value;

				if ( value != null )
				{
					// update aspect ratio of new camera if needed.
					if ( value.AutoAspectRatio )
					{
						value.AspectRatio = ( (Real)ActualWidth / ActualHeight );
					}
#if !AXIOM_NO_VIEWPORT_ORIENTATIONMODE
					value.OrientationMode = OrientationMode;
#endif
					value.NotifyViewport( this );
				}

				if ( ViewportCameraChanged != null )
					ViewportCameraChanged( new ViewportEventArgs( this ) );
			}
		}

		#endregion Camera Property

		#region Target Property

		/// <summary>
		///		Retrieves a reference to the render target for this viewport.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public RenderTarget Target
		{
			get;
			protected set;
		}

		#endregion Target Property

		#region Top (Relative [0.0, 1.0]) Property

		/// <summary>
		///		Gets the relative top edge of the viewport, a value between 0.0 and 1.0.
		/// </summary>
		[OgreVersion( 1, 7, 2790, "getter returns float rather than Real" )]
		public float Top
		{
			get;
			protected set;
		}

		#endregion Top (Relative [0.0, 1.0]) Property

		#region Left (Relative [0.0, 1.0]) Property

		/// <summary>
		///		Gets the relative left edge of the viewport, a value between 0.0 and 1.0.
		/// </summary>
		[OgreVersion( 1, 7, 2790, "getter returns float rather than Real" )]
		public float Left
		{
			get;
			protected set;
		}

		#endregion

		#region Width (Relative [0.0, 1.0]) Property

		/// <summary>
		///		Gets the relative width of the viewport, a value between 0.0 and 1.0.
		/// </summary>
		[OgreVersion( 1, 7, 2790, "getter returns float rather than Real" )]
		public float Width
		{
			get;
			protected set;
		}

		#endregion With (Relative [0.0, 1.0]) Property

		#region Height (Relative [0.0, 1.0]) Property

		/// <summary>
		///		Gets the relative height of the viewport, a value between 0.0 and 1.0.
		/// </summary>
		[OgreVersion( 1, 7, 2790, "getter returns float rather than Real" )]
		public float Height
		{
			get;
			protected set;
		}

		#endregion Height (Relative [0.0, 1.0]) Property

		#region ActualTop (In Pixels) Property

		/// <summary>
		///		Gets the actual top edge of the viewport, a value in pixels.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public int ActualTop
		{
			get;
			protected set;
		}

		#endregion ActualTop (In Pixels) Property

		#region ActualLeft (In Pixels) Property

		/// <summary>
		///		Gets the actual left edge of the viewport, a value in pixels.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public int ActualLeft
		{
			get;
			protected set;
		}

		#endregion ActualLeft (In Pixels) Property

		#region ActualWidth (In Pixels) Property

		/// <summary>
		///		Gets the actual width of the viewport, a value in pixels.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public int ActualWidth
		{
			get;
			protected set;
		}

		#endregion ActualWidth (In Pixels) Property

		#region ActualHeight (In Pixels) Property

		/// <summary>
		///		Gets the actual height of the viewport, a value in pixels.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public int ActualHeight
		{
			get;
			protected set;
		}

		#endregion ActualHeight (In Pixels) Property

		#region ZOrder Property

		/// <summary>
		///		Gets the ZOrder of this viewport.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public int ZOrder
		{
			get;
			protected set;
		}

		#endregion ZOrder Property

		#region IsAutoUpdated Property

		/// <summary>
		/// Gets/Sets whether this viewport is automatically updated if 
		/// Axioms's rendering loop or <see cref="RenderTarget.Update(bool)"/> is being used.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public bool IsAutoUpdated
		{
			get;
			set;
		}

		#endregion

		#region BackgroundColor Property

		/// <summary>
		///		Gets/Sets the background color which will be used to clear the screen every frame.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public ColorEx BackgroundColor
		{
			get;
			set;
		}

		#endregion BackgroundColor Property

		#region IsUpdated Property

		/// <summary>
		///	Gets/Sets the IsUpdated value.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public bool IsUpdated
		{
			get;
			protected set;
		}

		#endregion IsUpdated Property

		#region ShowOverlays Property

		/// <summary>
		///		Tells this viewport whether it should display Overlay objects.
		///	</summary>
		///	<remarks>
		///		Overlay objects are layers which appear on top of the scene. They are created via
		///		SceneManager.CreateOverlay and every viewport displays these by default.
		///		However, you probably don't want this if you're using multiple viewports,
		///		because one of them is probably a picture-in-picture which is not supposed to
		///		have overlays of it's own. In this case you can turn off overlays on this viewport
		///		by calling this method.
		/// </remarks>
		[OgreVersion( 1, 7, 2790, "OverlaysEnabled in Ogre" )]
		public bool ShowOverlays
		{
			get;
			set;
		}

		#endregion ShowOverlays Property

		#region ShowSkies Property

		/// <summary>
		/// Tells this viewport whether it should display skies.
		/// </summary>
		/// <remarks>
		/// Skies are layers which appear on background of the scene. They are created via
		/// SceneManager.SetSkyBox, SceneManager.SetSkyPlane and SceneManager.SetSkyDome and
		/// every viewport displays these by default. However, you probably don't want this if
		/// you're using multiple viewports, because one of them is probably a picture-in-picture
		/// which is not supposed to have skies of it's own. In this case you can turn off skies
		/// on this viewport by calling this method.
		/// </remarks>
		[OgreVersion( 1, 7, 2790, "SkiesEnabled in Ogre" )]
		public bool ShowSkies
		{
			get;
			set;
		}

		#endregion ShowSkies Property

		#region ShowShadows Property

		/// <summary>
		/// Tells this viewport whether it should display shadows.
		/// </summary>
		/// <remarks>
		/// This setting enables you to disable shadow rendering for a given viewport. The global
		/// shadow technique set on SceneManager still controls the type and nature of shadows,
		/// but this flag can override the setting so that no shadows are rendered for a given
		/// viewport to save processing time where they are not required.
		/// </remarks>
		[OgreVersion( 1, 7, 2790, "ShadowsEnabled in Ogre" )]
		public bool ShowShadows
		{
			get;
			set;
		}

		#endregion ShowShadows Property

		#region MaterialScheme Property


		/// <summary>
		/// the material scheme which the viewport should use.
		/// </summary>
		/// <remarks>
		/// This allows you to tell the system to use a particular
		/// material scheme when rendering this viewport, which can
		/// involve using different techniques to render your materials.
		/// <see>Technique.SchemeName</see>
		/// </remarks>
		[OgreVersion( 1, 7, 2790 )]
		public string MaterialScheme
		{
			get;
			set;
		}

		#endregion MaterialScheme Property

		#region VisibilityMask Property

		/// <summary>
		/// a per-viewport visibility mask.
		/// </summary>
		/// <remarks>
		/// The visibility mask is a way to exclude objects from rendering for
		/// a given viewport. For each object in the frustum, a check is made
		/// between this mask and the objects visibility flags
		/// <see cref="MovableObject.VisibilityFlags"/> , and if a binary 'and'
		/// returns zero, the object will not be rendered.
		/// </remarks>
		[OgreVersion( 1, 7, 2790 )]
		public uint VisibilityMask
		{
			get;
			set;
		}

		#endregion VisibilityMask Property

		#region OrientationMode

#if AXIOM_NO_VIEWPORT_ORIENTATIONMODE
		[OgreVersion(1, 7, 2790)]
		public OrientationMode OrientationMode
		{
			get
			{
				throw new AxiomException( "Getting Viewport orientation mode is not supported" );
			}
			protected set
			{
				throw new AxiomException( "Setting Viewport orientation mode is not supported" );
			}
		}
#else
		[OgreVersion( 1, 7, 2790 )]
		public OrientationMode OrientationMode
		{
			get;
			protected set;
		}
#endif

		#endregion

		#region DefaultOrientationMode

#if AXIOM_NO_VIEWPORT_ORIENTATIONMODE
		[OgreVersion(1, 7, 2790)]
		public static OrientationMode DefaultOrientationMode
		{
			get
			{
				throw new AxiomException( "Getting default Viewport orientation mode is not supported" );
			}
			protected set
			{
				throw new AxiomException( "Setting default Viewport orientation mode is not supported" );
			}
		}
#else
		[OgreVersion( 1, 7, 2790 )]
		public static OrientationMode DefaultOrientationMode
		{
			get;
			set;
		}
#endif

		#endregion

		#region NumRenderedFaces

		[OgreVersion( 1, 7, 2790, "NumRenderedFaces in Ogre" )]
		public int RenderedFaceCount
		{
			get
			{
				return Camera != null ? Camera.RenderedFaceCount : 0;
			}
		}

		#endregion

		#region RenderedBatchCount

		[OgreVersion( 1, 7, 2790, "NumRenderedBatches in Ogre" )]
		public int RenderedBatchCount
		{
			get
			{
				return Camera != null ? Camera.RenderedBatchCount : 0;
			}
		}

		#endregion

		#region RenderQueueSequence Properties Property

		[OgreVersion( 1, 7, 2790, "Protected in Ogre" )]
		private string _rqSequenceName;

		/// <summary>
		/// The name of the render queue invocation sequence for this target.
		/// </summary>
		/// <remarks>
		/// RenderQueueInvocationSequence instances are managed through Root. By
		/// setting this, you are indicating that you wish this RenderTarget to
		/// be updated using a custom sequence of render queue invocations, with
		/// potentially customised ordering and render state options. You should
		/// create the named sequence through Root first, then set the name here.
		/// </remarks>
		[OgreVersion( 1, 7, 2790, "Virtual in Ogre" )]
		public string RenderQueueInvocationSequenceName
		{
			get
			{
				return _rqSequenceName;
			}
			set
			{
				_rqSequenceName = value;
				if ( _rqSequenceName == string.Empty )
				{
					RenderQueueInvocationSequence = null;
				}
				else
				{
					throw new NotImplementedException();
					//RenderQueueInvocationSequence = Root.Instance.GetRenderQueueInvocationSequence( _rqSequenceName );
				}
			}
		}

		/// <summary>
		/// the invocation sequence - will return null if using standard
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public RenderQueueInvocationSequence RenderQueueInvocationSequence
		{
			get;
			protected set;
		}

		#endregion RenderQueueSequence Properties Property

		#region ClearDepth

		/// <summary>
		/// Gets the default depth buffer value to which the viewport is cleared.
		/// </summary>
		[OgreVersion( 1, 7, 2790, "DepthClear in Ogre" )]
		public Real ClearDepth
		{
			get;
			set;
		}

		#endregion

		#region ClearEveryFrame

		/// <summary>
		///		Determines whether to clear the viewport before rendering.
		/// </summary>
		/// <remarks>
		///		If you expecting every pixel on the viewport to be redrawn
		///		every frame, you can save a little time by not clearing the
		///		viewport before every frame. Do so by setting this property
		///		to false.
		///	</remarks>
		///	
		[OgreVersion( 1, 7, 2790 )]
		public bool ClearEveryFrame
		{
			get;
			protected set;
		}

		#endregion

		#region ClearBuffers

		/// <summary>
		/// Gets the buffers to clear every frame
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public FrameBufferType ClearBuffers
		{
			get;
			protected set;
		}

		#endregion

		#endregion Fields and Properties

		#region Constructor

		/// <summary>
		///		The constructor. Dimensions of the viewport are expressed as a pecentage between
		///		0 and 100. This allows the dimensions to apply irrespective of
		///		changes in the target's size: e.g. to fill the whole area,
		///		values of 0,0,100,100 are appropriate.
		/// </summary>
		/// <param name="camera">Reference to the camera to be the source for the image.</param>
		/// <param name="target">Reference to the render target to be the destination for the rendering.</param>
		/// <param name="left">Left</param>
		/// <param name="top">Top</param>
		/// <param name="width">Width</param>
		/// <param name="height">Height</param>
		/// <param name="zOrder">Relative Z-order on the target. Lower = further to the front.</param>
		public Viewport( Camera camera, RenderTarget target, float left, float top, float width, float height, int zOrder )
		{
			LogManager.Instance.Write( "Creating viewport rendering from camera '{0}', relative dimensions L:{1},T:{2},W:{3},H:{4}, Z-Order:{5}",
				camera.Name, left, top, width, height, zOrder );

			Camera = camera;
			Target = target;

			Left = left;
			Top = top;
			Width = width;
			Height = height;

			ZOrder = zOrder;

			BackgroundColor = ColorEx.Black;

			ClearDepth = 1.0;
			ClearEveryFrame = true;
			ClearBuffers = FrameBufferType.Color | FrameBufferType.Depth;

			IsUpdated = false;
			ShowOverlays = true;
			ShowSkies = true;
			ShowShadows = true;

			VisibilityMask = 0xFFFFFFFFu;

			IsAutoUpdated = true;

			OrientationMode = DefaultOrientationMode;

			// MaterialScheme = MaterialManager.DefaultSchemeName;
			MaterialScheme = Root.Instance.RenderSystem.DefaultViewportMaterialScheme;

			// Calculate actual dimensions
			UpdateDimensions();

			// notify camera
			if ( camera != null )
				camera.NotifyViewport( this );
		}

		#endregion

		#region Methods

		#region UpdateDimensions

		/// <summary>
		///		Notifies the viewport of a possible change in dimensions.
		/// </summary>
		///	<remarks>
		///		Used by the target to update the viewport's dimensions
		///		(usually the result of a change in target size).
		///	</remarks>
		[OgreVersion( 1, 7, 2790 )]
		public void UpdateDimensions()
		{
			var height = (Real)Target.Height;
			var width = (Real)Target.Width;

			ActualLeft = (int)( Left * width );
			ActualTop = (int)( Top * height );
			ActualWidth = (int)( Width * width );
			ActualHeight = (int)( Height * height );

			// This will check if  the cameras getAutoAspectRation() property is set.
			// If it's true its aspect ratio is fit to the current viewport
			// If it's false the camera remains unchanged.
			// This allows cameras to be used to render to many viewports,
			// which can have their own dimensions and aspect ratios.

			if ( Camera != null )
			{
				if ( Camera.AutoAspectRatio )
				{
					Camera.AspectRatio = (Real)ActualWidth / ActualHeight;
				}
				Camera.OrientationMode = OrientationMode;
			}

			LogManager.Instance.Write( "Viewport for camera '{0}' - actual dimensions L:{1},T:{2},W:{3},H:{4}, AR:{5}",
				Camera.Name, ActualLeft, ActualTop, ActualWidth, ActualHeight, Camera.AspectRatio );

			IsUpdated = true;

			if ( ViewportDimensionsChanged != null )
				ViewportDimensionsChanged( new ViewportEventArgs( this ) );
		}

		#endregion

		#region SetClearEveryFrame

		/// <summary>
		/// Determines whether to clear the viewport before rendering.
		/// </summary>
		/// <remarks>
		/// You can use this method to set which buffers are cleared
		/// (if any) before rendering every frame.
		/// </remarks>
		/// <param name="inClear">Whether or not to clear any buffers</param>
		[OgreVersion( 1, 7, 2790 )]
		public void SetClearEveryFrame( bool inClear )
		{
			SetClearEveryFrame( inClear, FrameBufferType.Color | FrameBufferType.Depth );
		}

		/// <summary>
		/// Determines whether to clear the viewport before rendering.
		/// </summary>
		/// <remarks>
		/// You can use this method to set which buffers are cleared
		/// (if any) before rendering every frame.
		/// </remarks>
		/// <param name="inClear">Whether or not to clear any buffers</param>
		/// <param name="inBuffers">
		/// One or more values from FrameBufferType denoting
		/// which buffers to clear, if clear is set to true. Note you should
		/// not clear the stencil buffer here unless you know what you're doing.
		/// </param>
		[OgreVersion( 1, 7, 2790 )]
		public void SetClearEveryFrame( bool inClear, FrameBufferType inBuffers )
		{
			ClearEveryFrame = inClear;
			ClearBuffers = inBuffers;
		}

		#endregion

		#region Update

		/// <summary>
		///		Instructs the viewport to updates its contents from the viewpoint of
		///		the current camera.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public void Update()
		{
			if ( Camera != null )
			{
				Camera.RenderScene( this, ShowOverlays );
			}
		}

		#endregion

		#region Clear

		/// <summary>
		/// Instructs the viewport to clear itself, without performing an update.
		/// </summary>
		/// <remarks>
		/// You would not normally call this method when updating the viewport, 
		/// since the viewport usually clears itself when updating anyway
		/// <see cref="Viewport.ClearEveryFrame"/>. However, if you wish you have the
		/// option of manually clearing the frame buffer (or elements of it)
		/// using this method.
		/// </remarks>
		/// <param name="buffers">Bitmask identifying which buffer elements to clear</param>
		/// <param name="col">The color value to clear to, if <see cref="FrameBufferType.Color"/> is included</param>
		/// <param name="depth">The depth value to clear to, if  <see cref="FrameBufferType.Depth"/> is included</param>
		/// <param name="stencil">The stencil value to clear to, if <see cref="FrameBufferType.Stencil"/> is included</param>
		[OgreVersion( 1, 7, 2790 )]
		public void Clear( FrameBufferType buffers, ColorEx col, Real depth, ushort stencil )
		{
			var rs = Root.Instance.RenderSystem;
			if ( rs == null )
				return;

			var currentvp = rs.Viewport;
			rs.Viewport = this;
			rs.ClearFrameBuffer( buffers, col, depth, stencil );
			if ( currentvp != null && currentvp != this )
				rs.Viewport = currentvp;
		}

		#endregion

		#region ClearUpdatedFlag

		[OgreVersion( 1, 7, 2790 )]
		public void ClearUpdatedFlag()
		{
			IsUpdated = false;
		}

		#endregion

		#region SetDimensions

		/// <summary>
		///		Allows setting the dimensions of the viewport (after creation).
		/// </summary>
		/// <remarks>
		///		Dimensions relative to the size of the target,
		///		represented as real values between 0 and 1. i.e. the full
		///		target area is 0, 0, 1, 1.
		/// </remarks>
		/// <param name="left">Left edge of the viewport ([0.0, 1.0]).</param>
		/// <param name="top">Top edge of the viewport ([0.0, 1.0]).</param>
		/// <param name="width">Width of the viewport ([0.0, 1.0]).</param>
		/// <param name="height">Height of the viewport ([0.0, 1.0]).</param>
		[OgreVersion( 1, 7, 2790 )]
		public void SetDimensions( Real left, Real top, Real width, Real height )
		{
			Left = left;
			Top = top;
			Width = width;
			Height = height;

			UpdateDimensions();
		}

		#endregion

		#region SetOrientationMode

		/// <summary>
		/// Set the orientation mode of the viewport.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public void SetOrientationMode( OrientationMode orientationMode )
		{
			SetOrientationMode( orientationMode, true );
		}

		/// <summary>
		/// Set the orientation mode of the viewport.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public void SetOrientationMode( OrientationMode orientationMode, bool setDefault )
		{
#if AXIOM_NO_VIEWPORT_ORIENTATIONMODE
			throw new AxiomException("Setting Viewport orientation mode is not supported");,
#endif
			OrientationMode = orientationMode;

			if ( setDefault )
			{
				DefaultOrientationMode = orientationMode;
			}

			if ( Camera != null )
			{
				Camera.OrientationMode = OrientationMode;
			}

			// Update the render system config
#if AXIOM_PLATFORM == AXIOM_PLATFORM_APPLE_IOS
			var rs = Root.Instance.RenderSystem;

			switch ( OrientationMode )
			{
				case OrientationMode.LandscapeLeft:
					rs.SetConfigOption( "Orientation", "Landscape Left" );
					break;
				case OrientationMode.LandscapeRight:
					rs.SetConfigOption( "Orientation", "Landscape Right" );
					break;
				case OrientationMode.Portrait:
					rs.SetConfigOption( "Orientation", "Portrait" );
					break;
			}
#endif
		}

		#endregion

		#region PointOrientedToScreen

		[OgreVersion( 1, 7, 2790 )]
		public void PointOrientedToScreen( Vector2 v, OrientationMode orientationMode, out Vector2 outv )
		{
			PointOrientedToScreen( v.x, v.y, orientationMode, out outv.x, out outv.y );
		}


		[OgreVersion( 1, 7, 2790 )]
		public void PointOrientedToScreen( Real orientedX, Real orientedY, OrientationMode orientationMode,
										 out Real screenX, out Real screenY )
		{
			var orX = orientedX;
			var orY = orientedY;
			switch ( orientationMode )
			{
				case OrientationMode.Degree90:
					screenX = orY;
					screenY = Real.One - orX;
					break;
				case OrientationMode.Degree180:
					screenX = Real.One - orX;
					screenY = Real.One - orY;
					break;
				case OrientationMode.Degree270:
					screenX = Real.One - orY;
					screenY = orX;
					break;
				default:
					screenX = orX;
					screenY = orY;
					break;
			}
		}

		#endregion

		#region GetActualDimensions

		/// <summary>
		///		Access to actual dimensions (based on target size).
		/// </summary>
		/// <param name="left">Left edge of the viewport (in pixels).</param>
		/// <param name="top">Top edge of the viewport (in pixels).</param>
		/// <param name="width">Width of the viewport (in pixels).</param>
		/// <param name="height">Height of the viewport (in pixels).</param>
		[OgreVersion( 1, 7, 2790 )]
		public void GetActualDimensions( out int left, out int top, out int width, out int height )
		{
			left = ActualLeft;
			top = ActualTop;
			width = ActualWidth;
			height = ActualHeight;
		}

		#endregion

		#endregion Methods

		#region DisposableObject overrides

		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
			    var ri = Root.Instance;
                if (ri != null)
                {
                    var rs = ri.RenderSystem;
                    if (rs != null && rs.Viewport == this)
                    {
                        rs.Viewport = null;
                    }
                }
			}

			base.dispose( disposeManagedResources );
		}

		#endregion
	}
}

#pragma warning disable 628
