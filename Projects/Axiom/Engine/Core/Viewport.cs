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

using Axiom.Graphics;

#endregion Namespace Declarations

namespace Axiom.Core
{
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
	public sealed class Viewport
	{
		#region Fields and Properties

		/// <summary>
		///		Should this viewport be cleared very frame?
		/// </summary>
		private bool _clearEveryFrame;

		/// <summary>
		///		Which buffers to clear every frame
		/// </summary>
		private FrameBufferType _clearBuffers;

		#region Camera Property

		/// <summary>
		///		Camera that this viewport is attached to.
		/// </summary>
		private Camera _camera;
		/// <summary>
		///		Retrieves a reference to the camera for this viewport.
		/// </summary>
		public Camera Camera
		{
			get
			{
				return _camera;
			}
			set
			{
				_camera = value;
			}
		}

		#endregion Camera Property

		#region Target Property

		/// <summary>
		///		Render target that is using this viewport.
		/// </summary>
		private RenderTarget _target;
		/// <summary>
		///		Retrieves a reference to the render target for this viewport.
		/// </summary>
		public RenderTarget Target
		{
			get
			{
				return _target;
			}
			set
			{
				_target = value;
			}
		}

		#endregion Target Property

		#region Top (Relative [0.0, 1.0]) Property

		/// <summary>
		///		Relative top [0.0, 1.0].
		/// </summary>
		private float _relativeTop;
		/// <summary>
		///		Gets the relative top edge of the viewport, a value between 0.0 and 1.0.
		/// </summary>
		public float Top
		{
			get
			{
				return _relativeTop;
			}
		}

		#endregion Top (Relative [0.0, 1.0]) Property

		#region Left (Relative [0.0, 1.0]) Property

		/// <summary>
		///		Relative left [0.0, 1.0].
		/// </summary>
		private float _relativeLeft;
		/// <summary>
		///		Gets the relative left edge of the viewport, a value between 0.0 and 1.0.
		/// </summary>
		public float Left
		{
			get
			{
				return _relativeLeft;
			}
		}

		#endregion Left (Relative [0.0, 1.0]) Property

		#region With (Relative [0.0, 1.0]) Property

		/// <summary>
		///		Relative width [0.0, 1.0].
		/// </summary>
		private float _relativeWidth;
		/// <summary>
		///		Gets the relative width of the viewport, a value between 0.0 and 1.0.
		/// </summary>
		public float Width
		{
			get
			{
				return _relativeWidth;
			}
		}

		#endregion With (Relative [0.0, 1.0]) Property

		#region Height (Relative [0.0, 1.0]) Property

		/// <summary>
		///		Relative height [0.0, 1.0].
		/// </summary>
		private float _relativeHeight;
		/// <summary>
		///		Gets the relative height of the viewport, a value between 0.0 and 1.0.
		/// </summary>
		public float Height
		{
			get
			{
				return _relativeHeight;
			}
		}

		#endregion Height (Relative [0.0, 1.0]) Property

		#region ActualTop (In Pixels) Property

		/// <summary>
		///		Absolute top edge of the viewport (in pixels).
		/// </summary>
		private int _actualTop;
		/// <summary>
		///		Gets the actual top edge of the viewport, a value in pixels.
		/// </summary>
		public int ActualTop
		{
			get
			{
				return _actualTop;
			}
		}

		#endregion ActualTop (In Pixels) Property

		#region ActualLeft (In Pixels) Property

		/// <summary>
		///		Absolute left edge of the viewport (in pixels).
		/// </summary>
		private int _actualLeft;
		/// <summary>
		///		Gets the actual left edge of the viewport, a value in pixels.
		/// </summary>
		public int ActualLeft
		{
			get
			{
				return _actualLeft;
			}
		}

		#endregion ActualLeft (In Pixels) Property

		#region ActualWidth (In Pixels) Property

		/// <summary>
		///		Absolute width of the viewport (in pixels).
		/// </summary>
		private int _actualWidth;
		/// <summary>
		///		Gets the actual width of the viewport, a value in pixels.
		/// </summary>
		public int ActualWidth
		{
			get
			{
				return _actualWidth;
			}
		}

		#endregion ActualWidth (In Pixels) Property

		#region ActualHeight (In Pixels) Property

		/// <summary>
		///		Absolute height of the viewport (in pixels).
		/// </summary>
		private int _actualHeight;
		/// <summary>
		///		Gets the actual height of the viewport, a value in pixels.
		/// </summary>
		public int ActualHeight
		{
			get
			{
				return _actualHeight;
			}
		}

		#endregion ActualHeight (In Pixels) Property

		#region ZOrder Property

		/// <summary>
		///		Depth order of the viewport, for sorting.
		/// </summary>
		private int _zOrder;
		/// <summary>
		///		Gets the ZOrder of this viewport.
		/// </summary>
		public int ZOrder
		{
			get
			{
				return _zOrder;
			}
		}
		#endregion ZOrder Property

		#region BackgroundColor Property

		/// <summary>
		///		Background color of the viewport.
		/// </summary>
		private ColorEx _backColor;
		/// <summary>
		///		Gets/Sets the background color which will be used to clear the screen every frame.
		/// </summary>
		public ColorEx BackgroundColor
		{
			get
			{
				return _backColor;
			}
			set
			{
				_backColor = value;
			}
		}

		#endregion BackgroundColor Property

		#region IsUpdated Property

		/// <summary>
		///		Has this viewport been updated?
		/// </summary>
		private bool _isUpdated;
		/// <summary>
		///		Gets/Sets the IsUpdated value.
		/// </summary>
		public bool IsUpdated
		{
			get
			{
				return _isUpdated;
			}
			set
			{
				_isUpdated = value;
			}
		}

		#endregion IsUpdated Property

		#region ShowOverlays Property

		/// <summary>
		///		Should we show overlays on this viewport?
		/// </summary>
		private bool _showOverlays;
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
		public bool ShowOverlays
		{
			get
			{
				return _showOverlays;
			}
			set
			{
				_showOverlays = value;
			}
		}

		#endregion ShowOverlays Property

		#region ShowSkies Property

		/// <summary>
		///		Should we show skies on this viewport?
		/// </summary>
		private bool _showSkies;
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
		public bool ShowSkies
		{
			get
			{
				return _showSkies;
			}
			set
			{
				_showSkies = value;
			}
		}

		#endregion ShowSkies Property

		#region ShowShadows Property

		/// <summary>
		///		Should we show shadows on this viewport?
		/// </summary>
		private bool _showShadows;
		/// <summary>
		/// Tells this viewport whether it should display shadows.
		/// </summary>
		/// <remarks>
		/// This setting enables you to disable shadow rendering for a given viewport. The global
		/// shadow technique set on SceneManager still controls the type and nature of shadows,
		/// but this flag can override the setting so that no shadows are rendered for a given
		/// viewport to save processing time where they are not required.
		/// </remarks>
		public bool ShowShadows
		{
			get
			{
				return _showShadows;
			}
			set
			{
				_showShadows = value;
			}
		}

		#endregion ShowShadows Property

		#region MaterialScheme Property

		/// <summary>
		///     Which material scheme should this viewport use?
		/// </summary>
		private string _materialScheme = MaterialManager.DefaultSchemeName;
		/// <summary>
		/// the material scheme which the viewport should use.
		/// </summary>
		/// <remarks>
		/// This allows you to tell the system to use a particular
		/// material scheme when rendering this viewport, which can
		/// involve using different techniques to render your materials.
		/// <see>Technique.SchemeName</see>
		/// </remarks>
		public string MaterialScheme
		{
			get
			{
				return _materialScheme;
			}
			set
			{
				_materialScheme = value;
			}
		}

		#endregion MaterialScheme Property

		#region VisibilityMask Property

		/// <summary>
		/// the per-viewport visibility mask
		/// </summary>
		private uint _visibilityMask = unchecked( 0xFFFFFFFF );
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
		public uint VisibilityMask
		{
			get
			{
				return _visibilityMask;
			}
			set
			{
				_visibilityMask = value;
			}
		}

		#endregion VisibilityMask Property

		#region RenderedFaceCount Property

		/// <summary>
		///		Returns the number of faces rendered to this viewport during the last frame.
		/// </summary>
		public int RenderedFaceCount
		{
			get
			{
				return _camera.RenderedFaceCount;
			}
		}

		#endregion RenderedFaceCount Property

		#region RenderedBatchCount Property

		/// <summary>
		/// Gets the number of rendered batches in the last update.
		/// </summary>
		public int RenderedBatchCount
		{
			get
			{
				//TODO : Implement Camera.RenderedBatchCount
				//return Camera.RenderedBatchCount;
				return 0;
			}
		}

		#endregion RenderedBatchCount Property

		#region RenderQueueSequence Properties Property

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
		public string RenderQueueInvocationSequenceName
		{
			get
			{
				//TODO : Implement Viewport.RenderQueueSequenceName
				throw new System.NotImplementedException();
			}
			set
			{
			}
		}

		/// <summary>
		/// the invocation sequence - will return null if using standard
		/// </summary>
		public RenderQueueInvocationSequence RenderQueueInvocationSequence
		{
			get
			{
				//TODO : Implement Viewport.RenderQueueSequence
				return null;
			}
			set
			{
			}
		}

		#endregion RenderQueueSequence Properties Property

		/// <summary>
		/// Gets the default depth buffer value to which the viewport is cleared.
		/// </summary>
		public float ClearDepth
		{
			get;
			set;
		}
		#endregion Fields and Properties

		#region Construction and Destruction

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
			Debug.Assert( camera != null, "Cannot use a null Camera to create a viewport." );
			Debug.Assert( target != null, "Cannot use a null RenderTarget to create a viewport." );

			LogManager.Instance.Write( "Creating viewport rendering from camera '{0}', relative dimensions L:{1},T:{2},W:{3},H:{4}, Z-Order:{5}",
				camera.Name, left, top, width, height, zOrder );

			this._camera = camera;
			this._target = target;
			this._zOrder = zOrder;

			_relativeLeft = left;
			_relativeTop = top;
			_relativeWidth = width;
			_relativeHeight = height;

			_backColor = ColorEx.Black;
			_clearEveryFrame = true;
			_clearBuffers = FrameBufferType.Color | FrameBufferType.Depth;

			// Calculate actual dimensions
			UpdateDimensions();

			_isUpdated = true;
			_showOverlays = true;
			_showSkies = true;
			_showShadows = true;

			// notify camera
			camera.NotifyViewport( this );
		}

		#endregion Construction and Destruction

		#region Methods

		/// <summary>
		///		Notifies the viewport of a possible change in dimensions.
		/// </summary>
		///	<remarks>
		///		Used by the target to update the viewport's dimensions
		///		(usually the result of a change in target size).
		///	</remarks>
		public void UpdateDimensions()
		{
			float height = (float)_target.Height;
			float width = (float)_target.Width;

			_actualLeft = (int)( _relativeLeft * width );
			_actualTop = (int)( _relativeTop * height );
			_actualWidth = (int)( _relativeWidth * width );
			_actualHeight = (int)( _relativeHeight * height );

			// This will check if  the cameras getAutoAspectRation() property is set.
			// If it's true its aspect ratio is fit to the current viewport
			// If it's false the camera remains unchanged.
			// This allows cameras to be used to render to many viewports,
			// which can have their own dimensions and aspect ratios.
			if ( _camera.AutoAspectRatio )
			{
				_camera.AspectRatio = (float)_actualWidth / (float)_actualHeight;
			}

			LogManager.Instance.Write( "Viewport for camera '{0}' - actual dimensions L:{1},T:{2},W:{3},H:{4}, AR:{5}",
				_camera.Name, _actualLeft, _actualTop, _actualWidth, _actualHeight, _camera.AspectRatio );

			_isUpdated = true;
		}

		/// <summary>
		///		Determines whether to clear the viewport before rendering.
		/// </summary>
		/// <remarks>
		///		If you expecting every pixel on the viewport to be redrawn
		///		every frame, you can save a little time by not clearing the
		///		viewport before every frame. Do so by setting this property
		///		to false.
		///	</remarks>
		public bool ClearEveryFrame
		{
			get
			{
				return _clearEveryFrame;
			}
			set
			{
				_clearEveryFrame = value;
			}
		}


		/// <summary>
		/// Gets the buffers to clear every frame
		/// </summary>
		/// <returns></returns>
		public FrameBufferType ClearBuffers
		{
			get
			{
				return _clearBuffers;
			}
			set
			{
				_clearBuffers = value;
			}
		}

		/// <summary>
		///		Instructs the viewport to updates its contents from the viewpoint of
		///		the current camera.
		/// </summary>
		public void Update()
		{
			if ( _camera != null )
			{
				_camera.RenderScene( this, _showOverlays );
			}
		}

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
		public void SetDimensions( float left, float top, float width, float height )
		{
			_relativeLeft = left;
			_relativeTop = top;
			_relativeWidth = width;
			_relativeHeight = height;

			UpdateDimensions();
		}

		/// <summary>
		///		Access to actual dimensions (based on target size).
		/// </summary>
		/// <param name="left">Left edge of the viewport (in pixels).</param>
		/// <param name="top">Top edge of the viewport (in pixels).</param>
		/// <param name="width">Width of the viewport (in pixels).</param>
		/// <param name="height">Height of the viewport (in pixels).</param>
		public void GetActualDimensions( out int left, out int top, out int width, out int height )
		{
			left = _actualLeft;
			top = _actualTop;
			width = _actualWidth;
			height = _actualHeight;
		}

		#endregion Methods
	}
}