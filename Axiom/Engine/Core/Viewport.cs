#region LGPL License
/*
Axiom Game Engine Library
Copyright (C) 2003  Axiom Project Team

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
using System.Diagnostics;
using Axiom.SubSystems.Rendering;

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
	// TESTME
	public class Viewport
	{
		#region Protected member variables
		
		protected Camera camera;
		protected RenderTarget target;
		protected float relativeLeft, relativeTop, relativeWidth, relativeHeight;
		protected int actualLeft, actualTop, actualWidth, actualHeight;
		protected int zOrder;
		protected ColorEx backColor;
		protected bool clearEveryFrame;
		protected bool isUpdated;
		protected bool showOverlays;
		
		#endregion

		#region Constructor

		/// <summary>
		///		The constructor. Dimensions of the viewport are expressed as a pecentage between
		///		0 and 100. This allows the dimensions to apply irrespective of
		///		changes in the target's size: e.g. to fill the whole area,
		///		values of 0,0,100,100 are appropriate.
		/// </summary>
		/// <param name="pCamera">Pointer to a camera to be the source for the image.</param>
		/// <param name="pTarget">Pointer to the render target to be the destination for the rendering.</param>
		/// <param name="left">Left</param>
		/// <param name="top">Top</param>
		/// <param name="width">Width</param>
		/// <param name="height">Height</param>
		/// <param name="pZOrder">Relative Z-order on the target. Lower = further to the front.</param>
		public Viewport(Camera camera, RenderTarget target, float left, float top, float width, float height, int zOrder)
		{
			Debug.Assert(camera != null, "Cannot use a null Camera to create a viewport.");
			Debug.Assert(target != null, "Cannor use a null RenderTarget to create a viewport.");

			string message;

			message = String.Format("Creating viewport rendering from camera " +
				"'{0}', relative dimensions L:{1},T:{2},W:{3},H:{4}, Z-Order:{5}",
				camera.Name, left, top, width, height, zOrder);

			System.Diagnostics.Trace.WriteLine(message);

			this.camera = camera;
			this.target = target;

			this.relativeLeft = left;
			this.relativeTop = top;
			this.relativeWidth = width;
			this.relativeHeight = height;
			this.zOrder = zOrder;

			this.backColor = ColorEx.FromColor(System.Drawing.Color.Black);
			this.clearEveryFrame = true;

			// Calculate actual dimensions
			UpdateDimensions();

			this.isUpdated = true;
			this.showOverlays = true;

		}

		#endregion

		#region Internal engine methods

		/// <summary>
		///		Notifies the viewport of a possible change in dimensions.
		/// </summary>
		///	<remarks>
		///		Used by the target to update the viewport's dimensions
		///		(usually the result of a change in target size).
		///	</remarks>
		internal void UpdateDimensions()
		{
			float height = (float)target.Height;
			float width = (float)target.Width;

			actualLeft = (int) ((relativeLeft * width) / 100.0f);
			actualTop = (int) ((relativeTop * height) / 100.0f);
			actualWidth = (int) ((relativeWidth * width) / 100.0f);
			actualHeight = (int) ((relativeHeight * height) / 100.0f);

			// Note that we don't propagate any changes to the Camera
			// This is because the Camera projects into a space with
			// range (-1,1), which then gets extrapolated to the viewport
			// dimensions. Note that if the aspect ratio of the camera
			// is not the same as that of the viewport, the image will
			// be distorted in some way.

			// This allows cameras to be used to render to many viewports,
			// which can have their own dimensions and aspect ratios.

			string message = String.Format("Viewport for camera '{0}' - actual dimensions L:{1},T:{2},W:{3},H:{4}",
				camera.Name, actualLeft, actualTop, actualWidth, actualHeight);

			System.Diagnostics.Trace.WriteLine(message);

			isUpdated = true;

		}

		#endregion

		#region Public properties

		/// <summary>
		/// Retrieves a reference to the render target for this viewport.
		/// </summary>
		public RenderTarget Target
		{
			get { return target; }
			set { target = value; }
		}

		/// <summary>
		/// Retrieves a reference to the camera for this viewport.
		/// </summary>
		public Camera Camera
		{
			get { return camera; }
			set { camera = value; }
		}

		/// <summary>
		/// Gets and sets the background color which will be used to clear the screen every frame.
		/// </summary>
		public ColorEx BackgroundColor
		{
			get { return backColor; }
			set { backColor = value; }
		}
		/// <summary>
		/// Gets one of the relative dimensions of the viewport, a value between 0.0 and 1.0.
		/// </summary>
		public float Top
		{
			get { return relativeTop; }
		}

		/// <summary>
		/// Gets one of the relative dimensions of the viewport, a value between 0.0 and 1.0.
		/// </summary>
		public float Left
		{
			get { return relativeLeft; }
		}

		/// <summary>
		/// Gets one of the relative dimensions of the viewport, a value between 0.0 and 1.0.
		/// </summary>
		public float Width
		{
			get { return relativeWidth; }
		}

		/// <summary>
		/// Gets one of the relative dimensions of the viewport, a value between 0.0 and 1.0.
		/// </summary>
		public float Height
		{
			get { return relativeHeight; }
		}

		/// <summary>
		/// Gets the ZOrder of this viewport.
		/// </summary>
		public int ZOrder
		{
			get { return zOrder; }
		}

		/// <summary>
		/// Gets one of the actual dimensions of the viewport, a value in pixels.
		/// </summary>
		public int ActualTop
		{
			get { return actualTop; }
		}

		/// <summary>
		/// Gets one of the actual dimensions of the viewport, a value in pixels.
		/// </summary>
		public int ActualLeft
		{
			get { return actualLeft; }
		}

		/// <summary>
		/// Gets one of the actual dimensions of the viewport, a value in pixels.
		/// </summary>
		public int ActualWidth
		{
			get { return actualWidth; }
		}

		/// <summary>
		/// Gets one of the actual dimensions of the viewport, a value in pixels.
		/// </summary>
		public int ActualHeight
		{
			get { return actualHeight; }
		}

		/// <summary>
		///		Determines whether to clear the viewport before rendering.
		/// </summary>
		/// <remarks>
		///		If you expecting every pixel on the viewport to be redrawn
		///		every frame, you can save a little time by not clearing the
		///		viewport before every frame. Do so by passing 'false' to this
		///		method (the default is to clear every frame).
		///	</remarks>
		public bool ClearEveryFrame
		{
			get { return clearEveryFrame; }
			set { clearEveryFrame = value; }
		}

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
		public bool OverlaysEnabled
		{
			get { return showOverlays; }
			set { showOverlays = value; }
		}

		/// <summary>
		///		Returns the number of faces rendered to this viewport during the last frame.
		/// </summary>
		public int NumRenderedFaces
		{
			get { return camera.NumRenderedFaces; }
		}

		/// <summary>
		/// Gets and sets the IsUpdated value.
		/// </summary>
		public bool IsUpdated
		{
			get { return isUpdated; }
			set { isUpdated = value; }
		}

		#endregion

		#region Public methods

		/// <summary>
		/// Instructs the viewport to updates its contents.
		/// </summary>
		public void Update()
		{
			camera.RenderScene(this, showOverlays);
		}

		/// <summary>
		///		Allows setting the dimensions of the viewport (after creation).
		/// </summary>
		/// <remarks>
		///		Dimensions relative to the size of the target,
		///		represented as real values between 0 and 1. i.e. the full
		///		target area is 0, 0, 1, 1.
		/// </remarks>
		/// <param name="left"></param>
		/// <param name="top"></param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		public void SetDimensions(float left, float top, float width, float height)
		{
			relativeLeft = left;
			relativeTop = top;
			relativeWidth = width;
			relativeHeight = height;
			
			UpdateDimensions();
		}

		#endregion
	}
}
