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
using System.Windows.Forms;
using Axiom.Core;
using Axiom.Utility;

namespace Axiom.SubSystems.Rendering
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
	///		
	// INC: In progress
	public abstract class RenderWindow : RenderTarget	
	{
		#region Protected member variables
		
		protected int top, left;
		protected bool isFullScreen;
		protected System.Windows.Forms.Control control;
		
		#endregion

		#region Constructor

		protected RenderWindow()
		{
		}

		#endregion

		#region Abstract methods and properties

		/// <summary>
		///		Creates & displays the new window.
		/// </summary>
		/// <param name="pName"></param>
		/// <param name="pTarget">The System.Windows.Form.Control that will be the host for this RenderWindow.</param>
		/// <param name="pWidth">The width of the window in pixels.</param>
		/// <param name="pHeight">The height of the window in pixels.</param>
		/// <param name="pColorDepth">The color depth in bits. Ignored if pFullScreen is false since the desktop depth is used.</param>
		/// <param name="pFullScreen">If true, the window fills the screen, with no title bar or border.</param>
		/// <param name="pLeft">The x-position of the window. Ignored if pFullScreen = true.</param>
		/// <param name="pTop">The y-position of the window. Ignored if pFullScreen = true.</param>
		/// <param name="pDepthBuffer">Specify true to include a depth-buffer.</param>
		/// <param name="pMiscParams">A variable number of pointers to platform-specific arguments. 
		/// The actual requirements must be defined by the implementing subclasses.</param>
		abstract public void Create(String name, System.Windows.Forms.Control target, int width, int height, int colorDepth, bool fullScreen, 
			int left, int top, bool depthBuffer, params object[] miscParams);

		/// <summary>
		///		Destroys the window.
		/// </summary>
		abstract public void Destroy();

		/// <summary>
		///		Alter the size of the window.
		/// </summary>
		/// <param name="pWidth"></param>
		/// <param name="pHeight"></param>
		abstract public void Resize(int width, int height);

		/// <summary>
		///		Reposition the window.
		/// </summary>
		/// <param name="pLeft"></param>
		/// <param name="pRight"></param>
		abstract public void Reposition(int left, int right);

		/// <summary>
		///		Determine if the window is active & visible.
		/// </summary>
		/// <remarks>
		///		This method returns true if the window is active
		///		 i.e. is visible somewhere on screen, therefore should
		///		receive more images.
		///
		///		The render system will use this to determine if it
		///		should bother rendering. If the window is minimized,
		///		the render will simply update the world and not
		///		bother rendering anything, to save CPU time.
		abstract public bool IsActive
		{
			get; set;
		}

		/// <summary>
		///		Swaps the frame buffers to display the next frame.
		/// </summary>
		/// <remarks>
        ///		All render windows are float-buffered so that no
        ///     'in-progress' versions of the scene are displayed
        ///      during rendering. Once rendering has completed (to
        ///		an off-screen version of the window) the buffers
        ///		are swapped to display the new frame.
		///	</remarks>
		/// <param name="pWaitForVSync">
		///		If true, the system waits for the
        ///		next vertical blank period (when the CRT beam turns off
        ///		as it travels from bottom-right to top-left at the
        ///		end of the pass) before flipping. If false, flipping
        ///		occurs no matter what the beam position. Waiting for
        ///		a vertical blank can be slower (and limits the
        ///		framerate to the monitor refresh rate) but results
        ///		in a steadier image with no 'tearing' (a flicker
        ///		resulting from flipping buffers when the beam is
        ///		in the progress of drawing the last frame). 
		///</param>
		abstract public void SwapBuffers(bool waitForVSync);

		/// <summary>
		///		Saves window contents to file (i.e. screenshot);
		/// </summary>
		abstract public void SaveToFile(String fileName);

		#endregion

		#region Virtual methods and properties

		/// <summary>
		///		Updates the window contents.
		/// </summary>
		/// <remarks>
         ///		The window is updated by telling each camera which is supposed
         ///		to render into this window to render it's view, and then
         ///		the window buffers are swapped via SwapBuffers()
         ///	</remarks>
		public override void Update()
		{
			// call base class Update method
			base.Update();

			// TODO: Implement this later
			// Update statistics (always on top)
			//UpdateStats();

			SwapBuffers(Engine.Instance.RenderSystem.IsVSync);
		}

		/// <summary>
		/// Returns true if window is running in fullscreen mode.
		/// </summary>
		virtual public bool IsFullScreen
		{
			get
			{
				return isFullScreen;
			}
		}

		public System.Windows.Forms.Control Control
		{
			get { return control; }
		}

		#endregion

		#region Static methods
		/// <summary>
		/// Creates a default window to use for a RenderWindow if none are provided.
		/// </summary>
		/// <param name="top"></param>
		/// <param name="left"></param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="fullScreen"></param>
		/// <returns></returns>
		public static Axiom.Utility.DefaultForm CreateDefaultForm(int top, int left, int width, int height, bool fullScreen)
		{
			DefaultForm form = new DefaultForm();

			form.Width = width;
			form.Height = height;
			form.MaximizeBox = false;
			form.MinimizeBox = false;
			form.StartPosition = FormStartPosition.CenterScreen;

			if(fullScreen)
			{
				form.Top = 0;
				form.Left = 0;
				form.FormBorderStyle = FormBorderStyle.None;
				form.WindowState = FormWindowState.Maximized;
				form.TopMost = true;
				form.TopLevel = true;
			}
			else
			{
				form.Top = top;
				form.Left = left;
				form.FormBorderStyle = FormBorderStyle.FixedSingle;
				form.WindowState = FormWindowState.Normal;
				form.Text = Engine.Instance.Name + " Window";
			}

			// make sure they cant resize it any smaller than this
			//form.MinimumSize = new System.Drawing.Size(200, 200);

			return form;
		}

		#endregion
	}
}
