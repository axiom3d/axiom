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
using System.Collections;
using Axiom.Core;
using Axiom.Collections;

namespace Axiom.SubSystems.Rendering
{

	#region Delegate declarations
		public delegate void RenderTargetUpdateEvent(object source, RenderTargetEventArgs e);

		public struct RenderTargetEventArgs
		{
		}
	#endregion

	/// <summary>
	///		A 'canvas' which can receive the results of a rendering operation.
	/// </summary>
	/// <remarks>
	///		This abstract class defines a common root to all targets of rendering operations. A
	///		render target could be a window on a screen, or another
	///		offscreen surface like a texture or bump map etc.
	///	</remarks>
	// TESTME
	public class RenderTarget
	{
		#region Protected member variables

		protected int height, width, colorDepth;
		protected string name;
		protected ViewportCollection viewportList;
		protected int numFaces;
		protected Hashtable customAttributes;

		#endregion

		#region Constructor

		public RenderTarget()
		{
			this.viewportList = new ViewportCollection(this);
			this.customAttributes = new Hashtable();

			this.numFaces = 0;
		}

		#endregion

		#region Public properties

		/// <summary>
		/// Gets/Sets the name of this render target.
		/// </summary>
		public String Name
		{
			get { return this.name; }
			set { this.name = value; }
		}

		/// <summary>
		/// Gets/Sets the width of this render target.
		/// </summary>
		public int Width
		{
			get { return this.width; }
			set { this.width = value; }
		}

		/// <summary>
		/// Gets/Sets the height of this render target.
		/// </summary>
		public int Height
		{
			get { return this.height; }
			set { this.height = value; }
		}

		/// <summary>
		/// Gets/Sets the color depth of this render target.
		/// </summary>
		public int ColorDepth
		{
			get { return this.colorDepth; }
			set { this.colorDepth = value; }
		}

		/// <summary>
		/// Allows access to the viewportList of this RenderTarget.
		/// </summary>
		public ViewportCollection Viewports
		{
			get { return this.viewportList; }
		}  

		/// <summary>
		///		Allows for stroring and retrieving custom attributes that can be leveraged by
		///		any subclass.  For example, OpenGL and Direct3D windows may want to 
		///		store certain objects that are only relevant to that particlular window.  This keeps
		///		things generic.
		/// </summary>
		public Hashtable CustomAttributes
		{
			get { return customAttributes; }
		}

		#endregion

		#region Public methods

		/// <summary>
		///		Tells the target to update it's contents.
		/// </summary>
		/// <remarks>
         ///		If the engine is not running in an automatic rendering loop
         ///		(started using RenderSystem.StartRendering()),
         ///		the user of the library is responsible for asking each render
         ///		target to refresh. This is the method used to do this. It automatically
         ///		re-renders the contents of the target using whatever cameras have been
         ///		pointed at it (using Camera.RenderTarget).
         ///	
         ///		This allows the engine to be used in multi-windowed utilities
         ///		and for contents to be refreshed only when required, rather than
         ///		constantly as with the automatic rendering loop.
		///	</remarks>
		virtual public void Update()
		{
			// notify listeners (pre)
			OnPreUpdate();

			numFaces = 0;

			// Go through viewportList in Z-order
			// Tell each to refresh
			for(int i = 0; i < viewportList.Count; i++)
			{
				viewportList[i].Update();
				numFaces += viewportList[i].Camera.NumRenderedFaces;
			}

			// notify listeners (post)
			OnPostUpdate();
		}


		/// <summary>
		///		Used to create a viewport for this RenderTarget.
		/// </summary>
		/// <param name="camera"></param>
		/// <param name="top"></param>
		/// <param name="left"></param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="zOrder"></param>
		/// <returns></returns>
		virtual public Viewport CreateViewport(Camera camera, int left, int top, int width, int height, int zOrder)
		{
			// create a new camera and add it to our internal collection
			Viewport viewport = new Viewport(camera, this, left, top, width, height, zOrder);
			this.viewportList.Add(viewport);

			return viewport;
		}

		#endregion

		#region Protected methods

		/// <summary>
		/// Called to fire the PreUpdate event.
		/// </summary>
		protected void OnPreUpdate()
		{
			if(this.PreUpdate != null)
			{
				RenderTargetEventArgs e = new RenderTargetEventArgs();

				PreUpdate(this, e);
			}
		}

		/// <summary>
		/// Called to fire the PostUpdate event.
		/// </summary>
		protected void OnPostUpdate()
		{
			if(this.PostUpdate != null)
			{
				RenderTargetEventArgs e = new RenderTargetEventArgs();

				PostUpdate(this, e);
			}
		}

		#endregion

		#region Events

		/// <summary>
		/// 
		/// </summary>
		public event RenderTargetUpdateEvent PreUpdate;
		
		/// <summary>
		/// 
		/// </summary>
		public event RenderTargetUpdateEvent PostUpdate;

		#endregion
	}
}
