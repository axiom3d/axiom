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
using System.Collections.Generic;
using Axiom.Core;
using Axiom.Collections;

#endregion Namespace Declarations

namespace Axiom.Graphics
{
	/// <summary>
	///		Manages the target rendering window.
	/// </summary>
	/// <remarks>
	///		This class handles a window into which the contents
	///		of a scene are rendered. There is a many-to-1 relationship
	///		between instances of this class an instance of RenderSystem
	///		which controls the rendering of the scene. There may be
	///		more than one window in the case of level editor tools etc.
	///		This class is abstract since there may be
	///		different implementations for different windowing systems.
	///
	///		Instances are created and communicated with by the render system
	///		although client programs can get a reference to it from
	///		the render system if required for resizing or moving.
	///		Note that you can have multiple viewpoints
	///		in the window for effects like rear-view mirrors and
	///		picture-in-picture views (see Viewport and Camera).
	///	</remarks>
	public abstract class RenderWindow : RenderTarget
	{
		#region Protected member variables

		protected IntPtr targetHandle;

		#region top Property

		private int _top;
		/// <summary>
		/// 
		/// </summary>
		protected int top
		{
			get
			{
				return _top;
			}
			set
			{
				_top = value;
			}
		}

		#endregion top Property

		#region left Property

		private int _left;
		/// <summary>
		/// 
		/// </summary>
		protected int left
		{
			get
			{
				return _left;
			}
			set
			{
				_left = value;
			}
		}

		#endregion left Property

		#region IsFullScreen Property

		protected bool isFullScreen;
		/// <summary>
		/// Returns true if window is running in fullscreen mode.
		/// </summary>
		public virtual bool IsFullScreen
		{
			get
			{
				return isFullScreen;
			}
			protected set
			{
				isFullScreen = value;
			}
		}

		#endregion IsFullScreen Property

		#region IsVisible Property

		/// <summary>
		/// Indicates whether the window is visible (not minimized or obscured)
		/// </summary>
		public virtual bool IsVisible
		{
			get
			{
				return true;
			}
			set
			{

			}
		}

		#endregion IsVisible Property

		public override bool IsActive
		{
			get
			{
				return base.IsActive && IsVisible;
			}
			set
			{
				base.IsActive = value;
			}
		}

		/// <summary>
		/// Indicates whether the window has been closed by the user.
		/// </summary>
		/// <returns></returns>
		public abstract bool IsClosed
		{
			get;
		}

		#region IsPrimary Property

		private bool _isPrimary;
		/// <summary>
		/// Indicates wether the window is the primary window. The
		/// primary window is special in that it is destroyed when 
		/// ogre is shut down, and cannot be destroyed directly.
		/// This is the case because it holds the context for vertex,
		/// index buffers and textures.
		/// </summary>
		public virtual bool IsPrimary
		{
			get
			{
				return _isPrimary;
			}
			internal set // Only to be called by root
			{
				_isPrimary = value;
			}
		}

        #endregion IsPrimary Property

        public virtual bool IsHidden
        {
            get
            {
                return false;
            }
            set
            {
            }
        }

	    

		#endregion

		#region Constructor

		protected RenderWindow()
            : base()
		{

			// render windows are low priority
			this.Priority = RenderTargetPriority.Default;
		}

		#endregion

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
                }
            }

            // make sure this window is no longer active
            IsActive = false;

            base.dispose(disposeManagedResources);
        }

		#region Abstract methods and properties

		/// <summary>
		///		Creates &amp; displays the new window.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="width">The width of the window in pixels.</param>
		/// <param name="height">The height of the window in pixels.</param>
		/// <param name="fullScreen">If true, the window fills the screen, with no title bar or border.</param>
		/// <param name="miscParams">A variable number of platform-specific arguments. 
		/// The actual requirements must be defined by the implementing subclasses.</param>
		public abstract void Create( string name, int width, int height, bool fullScreen, NamedParameterList miscParams );

		/// <summary>
		///		Alter the size of the window.
		/// </summary>
		/// <param name="width"></param>
		/// <param name="height"></param>
		public abstract void Resize( int width, int height );

		/// <summary>
		///		Reposition the window.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		public abstract void Reposition( int left, int right );

		/// <summary>
		/// Notify that the window has been resized
		/// </summary>
		/// <remarks>You don't need to call this unless you created the window externally.</remarks>
		public virtual void WindowMovedOrResized()
		{
		}

		#endregion

		#region Virtual methods and properties

		/// <summary>
		/// Retrieve information about the render target.
		/// </summary>
		public virtual void GetMetrics( out int width, out int height, out int colorDepth, out int left, out int top )
		{
			GetMetrics( out width, out height, out colorDepth );
			top = _top;
			left = _left;
		}


		#endregion

	}

    public class RenderWindowList: List<RenderWindow>
    {
    }
}