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
//     <id value="$Id:"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using Axiom.Core;
using Axiom.Collections;
using Axiom.Graphics;
using Axiom.Media;
using OpenTK;
using OpenTK.Graphics;
using Tao.OpenGl;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGL
{
	/// <summary>
	/// Summary description for OpenTKWindow.
	/// </summary>
	public class OpenTKWindow : RenderWindow
	{
		#region Fields

		private NativeWindow _window;
		private OpenTKGLContext glContext;

		private bool fullScreen;
		private DisplayDevice displayDevice = null;

		#endregion Fields

		public OpenTKWindow()
		{
		}

		#region RenderWindow Members

		public override bool RequiresTextureFlipping
		{
			get
			{
				return false;
			}
		}

		public override object this[ string attribute ]
		{
			get
			{
				switch ( attribute.ToLower() )
				{
					case "glcontext":
						return this.glContext;
					case "window":
						return this._window.WindowInfo.WindowHandle;
					case "nativewindow":
						return this._window;
					default:
						return null;
				}
			}
		}

		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				if ( disposeManagedResources )
				{
					if ( this.glContext != null ) // Do We Not Have A Rendering Context?
					{
						this.glContext.SetCurrent();
						this.glContext.Dispose();
						this.glContext = null;
					}

					if ( this._window != null )
					{
						if ( this.fullScreen )
						{
							this.displayDevice.RestoreResolution();
						}

						this._window.Close();
						this._window = null;
					}
				}

				// There are no unmanaged resources to release, but
				// if we add them, they need to be released here.
			}
			// If it is available, make the call to the
			// base class's Dispose(Boolean) method
			base.dispose( disposeManagedResources );
		}

		/// <summary>
		/// Indicates whether the window has been closed by the user.
		/// </summary>
		/// <returns></returns>
		public override bool IsClosed
		{
			get
			{
				return this._window == null && this.glContext == null;
			}
		}

		/// <summary>
		///		Creates &amp; displays the new window.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="width">The width of the window in pixels.</param>
		/// <param name="height">The height of the window in pixels.</param>
		/// <param name="fullScreen">If true, the window fills the screen, with no title bar or border.</param>
		/// <param name="miscParams">A variable number of platform-specific arguments.
		/// The actual requirements must be defined by the implementing subclasses.</param>
		public override void Create( string name, int width, int height, bool fullScreen, NamedParameterList miscParams )
		{
			string title = name;
			bool vsync = false;
			int depthBuffer = GraphicsMode.Default.Depth;
			float displayFrequency = 60f;
			string border = "resizable";

			this.name = name;
			this.width = width;
			this.height = height;
			colorDepth = 32;
			this.fullScreen = fullScreen;
			this.displayDevice = DisplayDevice.Default;

			#region Parameter Handling

			if ( miscParams != null )
			{
				foreach ( var entry in miscParams )
				{
					switch ( entry.Key )
					{
						case "title":
							title = entry.Value.ToString();
							break;
						case "left":
							left = Int32.Parse( entry.Value.ToString() );
							break;
						case "top":
							top = Int32.Parse( entry.Value.ToString() );
							break;
						case "fsaa":
							fsaa = Int32.Parse( entry.Value.ToString() );
							break;
						case "colourDepth":
						case "colorDepth":
							colorDepth = Int32.Parse( entry.Value.ToString() );
							break;
						case "vsync":
							vsync = entry.Value.ToString() == "No" ? false : true;
							break;
						case "displayFrequency":
							displayFrequency = Int32.Parse( entry.Value.ToString() );
							break;
						case "depthBuffer":
							depthBuffer = Int32.Parse( entry.Value.ToString() );
							break;
						case "border":
							border = entry.Value.ToString().ToLower();
							break;

						case "externalWindowInfo":
							this.glContext = new OpenTKGLContext( (OpenTK.Platform.IWindowInfo)entry.Value );
							break;

						case "externalWindowHandle":
							object handle = entry.Value;
							IntPtr ptr = IntPtr.Zero;
							if ( handle.GetType() == typeof ( IntPtr ) )
							{
								ptr = (IntPtr)handle;
							}
							else if ( handle.GetType() == typeof ( System.Int32 ) )
							{
								ptr = new IntPtr( (int)handle );
							}
							//glContext = new OpenTKGLContext(Control.FromHandle(ptr), Control.FromHandle(ptr).Parent);

							WindowEventMonitor.Instance.RegisterWindow( this );
							fullScreen = false;
							IsActive = true;
							break;

						case "externalWindow":
							//glContext = new OpenTKGLContext((Control)entry.Value, ((Control)entry.Value).Parent);
							WindowEventMonitor.Instance.RegisterWindow( this );
							fullScreen = false;
							IsActive = true;
							break;

						default:
							break;
					}
				}
			}

			#endregion Parameter Handling

			if ( this.glContext == null )
			{
				// create window
				this._window = new NativeWindow( width, height, title, GameWindowFlags.Default,
				                                 new GraphicsMode( GraphicsMode.Default.ColorFormat, depthBuffer,
				                                                   GraphicsMode.Default.Stencil, FSAA ), this.displayDevice );
				this.glContext = new OpenTKGLContext( this._window.WindowInfo );

				FileSystem.FileInfoList ico =
					ResourceGroupManager.Instance.FindResourceFileInfo( ResourceGroupManager.DefaultResourceGroupName, "AxiomIcon.ico" );
				if ( ico.Count != 0 )
				{
					this._window.Icon = System.Drawing.Icon.ExtractAssociatedIcon( ico[ 0 ].Filename );
				}

				if ( fullScreen )
				{
					this.displayDevice.ChangeResolution( width, height, ColorDepth, displayFrequency );
					this._window.WindowState = WindowState.Fullscreen;
					IsFullScreen = true;
				}
				else
				{
					this._window.WindowState = WindowState.Normal;

					if ( border == "fixed" )
					{
						this._window.WindowBorder = WindowBorder.Fixed;
					}
					else if ( border == "resizable" )
					{
						this._window.WindowBorder = WindowBorder.Resizable;
					}
					else if ( border == "none" )
					{
						this._window.WindowBorder = WindowBorder.Hidden;
					}
				}

				this._window.Title = title;

				WindowEventMonitor.Instance.RegisterWindow( this );

				// lets get active!
				IsActive = true;
				this.glContext.VSync = vsync;
				this._window.Visible = true;
			}
		}

		public override void Reposition( int left, int right )
		{
			if ( this._window != null && !IsFullScreen )
			{
				this._window.Location = new System.Drawing.Point( left, right );
				WindowEventMonitor.Instance.WindowMoved( this );
			}
		}

		public override void Resize( int width, int height )
		{
			if ( this._window == null )
			{
				return;
			}
			this._window.Width = width;
			this._window.Height = height;
			WindowEventMonitor.Instance.WindowResized( this );
		}

		public override void WindowMovedOrResized()
		{
			// Update dimensions incase changed
			foreach ( Viewport entry in ViewportList.Values )
			{
				entry.UpdateDimensions();
			}
		}

		public override void CopyContentsToMemory( PixelBox dst, FrameBuffer buffer )
		{
			if ( ( dst.Left < 0 ) || ( dst.Right > Width ) || ( dst.Top < 0 ) || ( dst.Bottom > Height ) || ( dst.Front != 0 ) ||
			     ( dst.Back != 1 ) )
			{
				throw new Exception( "Invalid box." );
			}
			if ( buffer == RenderTarget.FrameBuffer.Auto )
			{
				buffer = IsFullScreen ? RenderTarget.FrameBuffer.Front : RenderTarget.FrameBuffer.Back;
			}

			int format = GLPixelUtil.GetGLOriginFormat( dst.Format );
			int type = GLPixelUtil.GetGLOriginDataType( dst.Format );

			if ( ( format == Gl.GL_NONE ) || ( type == 0 ) )
			{
				throw new Exception( "Unsupported format." );
			}

			// Switch context if different from current one
			RenderSystem rsys = Root.Instance.RenderSystem;
			rsys.Viewport = GetViewport( 0 );

			// Must change the packing to ensure no overruns!
			Gl.glPixelStorei( Gl.GL_PACK_ALIGNMENT, 1 );

			Gl.glReadBuffer( ( buffer == RenderTarget.FrameBuffer.Front ) ? Gl.GL_FRONT : Gl.GL_BACK );
			Gl.glReadPixels( dst.Left, dst.Top, dst.Width, dst.Height, format, type, dst.Data.Pin() );
			dst.Data.UnPin();

			// restore default alignment
			Gl.glPixelStorei( Gl.GL_PACK_ALIGNMENT, 4 );

			//vertical flip

			{
				int rowSpan = dst.Width*PixelUtil.GetNumElemBytes( dst.Format );
				int height = dst.Height;
				var tmpData = new byte[rowSpan*height];
#if !AXIOM_SAFE_ONLY
				unsafe
#endif
				{
					var dataPtr = dst.Data.ToBytePointer();
					//int *srcRow = (uchar *)dst.data, *tmpRow = tmpData + (height - 1) * rowSpan;

					for ( int row = height - 1, tmpRow = 0; row >= 0; row--, tmpRow++ )
					{
						for ( int col = 0; col < rowSpan; col++ )
						{
							tmpData[ tmpRow*rowSpan + col ] = dataPtr[ row*rowSpan + col ];
						}
					}
				}
				var tmpDataHandle = Memory.PinObject( tmpData );
				Memory.Copy( tmpDataHandle, dst.Data, rowSpan*height );
				Memory.UnpinObject( tmpData );
			}
		}

		/// <summary>
		///		Update the render window.
		/// </summary>
		/// <param name="waitForVSync"></param>
		public override void SwapBuffers( bool waitForVSync )
		{
			if ( this.glContext != null )
			{
				this.glContext.SwapBuffers();
				return;
			}
			if ( this._window != null )
			{
				this._window.ProcessEvents();
				if ( this._window.Exists == false /*|| _window.IsExiting == true*/ )
				{
					WindowEventMonitor.Instance.WindowClosed( this );
					return;
				}

				if ( this._window.WindowState == WindowState.Minimized || !this._window.Focused )
				{
					return;
				}
				/*_window.SwapBuffers();*/
			}
		}

		#endregion RenderWindow Members

		public override void Destroy()
		{
			throw new NotImplementedException();
		}
	}
}