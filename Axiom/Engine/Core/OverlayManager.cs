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
using Axiom.SubSystems.Rendering;

namespace Axiom.Core
{
	/// <summary>
	/// Summary description for OverlayManager.
	/// </summary>
	public class OverlayManager : ResourceManager
	{
		#region Singleton implementation

		static OverlayManager() { Init(); }
		protected OverlayManager() {}
		protected static OverlayManager instance;

		public static OverlayManager Instance
		{
			get { return instance; }
		}

		public static void Init()
		{
			instance = new OverlayManager();
		}
		
		#endregion

		#region Member variables
	
		protected int lastViewportWidth;
		protected int lastViewportHeight;
		protected bool viewportDimensionsChanged;

		#endregion

		/// <summary>
		///		Creates and return a new overlay.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public override Resource Create(string name)
		{
			Overlay overlay = new Overlay(name);
			Load(overlay, 1);
			return overlay;
		}

		/// <summary>
		///		Internal method for queueing the visible overlays for rendering.
		/// </summary>
		/// <param name="camera"></param>
		/// <param name="queue"></param>
		/// <param name="viewport"></param>
		internal void QueueOverlaysForRendering(Camera camera, RenderQueue queue, Viewport viewport)
		{
		}

		#region Properties

		/// <summary>
		///		Gets if the viewport has changed dimensions. 
		/// </summary>
		/// <remarks>
		///		This is used by pixel-based GuiControls to work out if they need to reclaculate their sizes.
		///	</remarks>																				  
		public bool HasViewportChanged
		{
			get { return viewportDimensionsChanged; }
		}

		/// <summary>
		///		Gets the height of the destination viewport in pixels.
		/// </summary>
		public int ViewportHeight
		{
			get { return lastViewportHeight; } 
		}

		/// <summary>
		///		Gets the width of the destination viewport in pixels.
		/// </summary>
		public int ViewportWidth
		{
			get { return lastViewportWidth; }
		}

		#endregion
	}
}
