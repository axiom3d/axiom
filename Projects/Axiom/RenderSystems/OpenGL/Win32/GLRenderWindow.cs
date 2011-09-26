#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2006 Axiom Project Team

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
//     <license see="http://axiomengine.sf.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.IO;
using System.Runtime.InteropServices;
using Axiom.CrossPlatform;
using SWF = System.Windows.Forms;

using Axiom.Core;
using Axiom.Collections;
using Axiom.Graphics;
using Axiom.Media;

using Tao.OpenGl;
using Tao.Platform.Windows;
using System.Collections.Generic;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGL
{
	/// <summary>
	/// Summary description for GLWindow.
	/// </summary>
	public class Win32Window : RenderWindow
	{
		#region Fields

		/// <summary>Window handle.</summary>
		private static IntPtr _hWindow = IntPtr.Zero;
		/// <summary>GDI Device Context</summary>
		private IntPtr _hDeviceContext = IntPtr.Zero;
		/// <summary>Rendering context.</summary>
		private IntPtr _hRenderingContext = IntPtr.Zero;
		/// <summary>Win32Context.</summary>
		private Win32Context _glContext;
		/// <summary>Retains initial screen settings.</summary>        
		private Gdi.DEVMODE _intialScreenSettings;

		private GLSupport _glSupport;

		private bool _isExternal;
		private bool _isExternalGLControl;
		private bool _isSizing;
		private bool _isClosed;
		public override bool IsClosed
		{
			get
			{
				return _isClosed;
			}
		}

		private int _displayFrequency;      // fullscreen only, to restore display

		#endregion Fields

		#region Constructor

		/// <summary>
		///		Constructor.
		/// </summary>
		internal Win32Window( GLSupport glSupport )
			: base()
		{
			_glSupport = glSupport;
		}

		#endregion Constructor

		#region Implementation of RenderWindow

		public override object this[ string attribute ]
		{
			get
			{
				switch ( attribute.ToLower() )
				{
					case "glcontext":
						return _glContext;
					case "window":
						//System.Windows.Forms.Control ctrl = System.Windows.Forms.Control.FromChildHandle( _hWindow );
						//return ctrl;
						return _hWindow;
					default:
						return null;
				}
			}
		}

		public override void Create( string name, int width, int height, bool isFullScreen, NamedParameterList miscParams )
		{
			if ( _hWindow != IntPtr.Zero )
				dispose( true );

			_hWindow = IntPtr.Zero;
			this.name = name;
			this.IsFullScreen = isFullScreen;
			this._isClosed = false;

			// load window defaults
			this.left = this.top = -1; // centered
			this.width = width;
			this.height = height;
			this._displayFrequency = 0;
			this.isDepthBuffered = true;
			this.colorDepth = IsFullScreen ? 32 : 16;

			IntPtr parentHwnd = IntPtr.Zero;
			string title = name;
			bool vsync = false;
			int fsaa = 0;
			string border = "";
			bool outerSize = false;

			#region Parameter Handling
			if ( miscParams != null )
			{
				foreach ( KeyValuePair<string, object> entry in miscParams )
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
						case "depthBuffer":
							isDepthBuffered = bool.Parse( entry.Value.ToString() );
							break;
						case "vsync":
							vsync = entry.Value.ToString() == "Yes" ? true : false;
							break;
						case "fsaa":
							fsaa = Int32.Parse( entry.Value.ToString() );
							break;
						case "externalWindowHandle":
							_hWindow = (IntPtr)entry.Value;
							if ( _hWindow != IntPtr.Zero )
							{
								_isExternal = true;
								IsFullScreen = false;
							}
							break;
						case "externalGLControl":
							break;
						case "border":
                            border = ( (string)miscParams[ "border" ] ).ToLower(); 
							break;
						case "outerDimensions":
							break;
						case "displayFrequency":
							if ( IsFullScreen )
								_displayFrequency = Int32.Parse( entry.Value.ToString() );
							break;
						case "colorDepth":
							if ( IsFullScreen )
								colorDepth = Int32.Parse( entry.Value.ToString() );
							break;
						case "parentWindowHandle":
							if ( !IsFullScreen )
								parentHwnd = (IntPtr)entry.Value;
							break;
						default:
							break;
					}
				}
			}
			#endregion Parameter Handling

			if ( !_isExternal )
			{
				DefaultForm form = new DefaultForm();

				form.ClientSize = new System.Drawing.Size( width, height );
				form.MaximizeBox = false;
				form.MinimizeBox = false;
				form.StartPosition = SWF.FormStartPosition.CenterScreen;

				if ( IsFullScreen )
				{
					// Set the display to the desired resolution
					Gdi.DEVMODE screenSettings = new Gdi.DEVMODE();
					screenSettings.dmSize = (short)Marshal.SizeOf( screenSettings );
					screenSettings.dmPelsWidth = width;                         // Selected Screen Width
					screenSettings.dmPelsHeight = height;                       // Selected Screen Height
					screenSettings.dmBitsPerPel = ColorDepth;                         // Selected Bits Per Pixel
					screenSettings.dmFields = Gdi.DM_BITSPERPEL | Gdi.DM_PELSWIDTH | Gdi.DM_PELSHEIGHT;

					// Try To Set Selected Mode And Get Results.  NOTE: CDS_FULLSCREEN Gets Rid Of Start Bar.
					int result = User.ChangeDisplaySettings( ref screenSettings, User.CDS_FULLSCREEN );

					if ( result != User.DISP_CHANGE_SUCCESSFUL )
					{
						throw new System.ComponentModel.Win32Exception( Marshal.GetLastWin32Error(), "Unable to change user display settings." );
					}

					// Adjust form to size the screen
					form.Top = 0;
					form.Left = 0;
					form.FormBorderStyle = SWF.FormBorderStyle.None;
					form.WindowState = SWF.FormWindowState.Maximized;
#if !DEBUG
					form.TopMost = true;
					form.TopLevel = true;
#endif
				}
				else
				{
					if ( parentHwnd != IntPtr.Zero )
					{
						form.Owner = (SWF.Form)SWF.Control.FromHandle( parentHwnd );
					}
					else
					{
                        if ( border == "none" )
                        {
                            form.FormBorderStyle = SWF.FormBorderStyle.None;
                        }
                        else if ( border == "fixed" )
                        {
                            form.FormBorderStyle = SWF.FormBorderStyle.FixedSingle;
                            form.MaximizeBox = false;
                        }
                    }

					form.Top = top;
					form.Left = left;
					//form.FormBorderStyle = SWF.FormBorderStyle.FixedSingle;
					form.WindowState = SWF.FormWindowState.Normal;
					form.Text = title;
				}

                WindowEventMonitor.Instance.RegisterWindow( this );

				form.RenderWindow = this;
				_hWindow = form.Handle;
				form.Show();

			}

			IntPtr old_hdc = Wgl.wglGetCurrentDC();
			IntPtr old_context = Wgl.wglGetCurrentContext();

			SWF.Control ctrl = SWF.Form.FromHandle( _hWindow );
			//Form frm = (Form)ctrl.TopLevelControl;
			this.top = ctrl.Top;
			this.left = ctrl.Left;
			this.width = ctrl.ClientRectangle.Width;
			this.height = ctrl.ClientRectangle.Height;

			_hDeviceContext = User.GetDC( _hWindow );

			// Do not change vsync if the external window has the OpenGL control
			if ( !_isExternalGLControl )
			{
				if ( !_glSupport.SelectPixelFormat( _hDeviceContext, ColorDepth, fsaa ) )
				{
					if ( fsaa == 0 )
						throw new Exception( "selectPixelFormat failed" );

					LogManager.Instance.Write( "FSAA level not supported, falling back" );
					if ( !_glSupport.SelectPixelFormat( _hDeviceContext, ColorDepth, 0 ) )
						throw new Exception( "selectPixelFormat failed" );
				}
			}

			// attempt to get the rendering context
			_hRenderingContext = Wgl.wglCreateContext( _hDeviceContext );

			if ( _hRenderingContext == IntPtr.Zero )
			{
				throw new System.ComponentModel.Win32Exception( Marshal.GetLastWin32Error(), "Unable to create a GL rendering context." );
			}

			if ( !Wgl.wglMakeCurrent( _hDeviceContext, _hRenderingContext ) )
			{
				throw new System.ComponentModel.Win32Exception( Marshal.GetLastWin32Error(), "Unable to activate the GL rendering context." );
			}

			// Do not change vsync if the external window has the OpenGL control
			if ( !_isExternalGLControl )
			{
				// Don't use wglew as if this is the first window, we won't have initialised yet
				//IntPtr wglSwapIntervalEXT = Wgl.wglGetProcAddress( "wglSwapIntervalEXT" );
				//if ( wglSwapIntervalEXT != IntPtr.Zero )
				//Wgl.wglSwapIntervalEXT( wglSwapIntervalEXT, vsync );
				if ( Wgl.IsExtensionSupported( "wglSwapIntervalEXT" ) )
					Wgl.wglSwapIntervalEXT( vsync ? 1 : 0 ); // Tao 2.0
			}

			if ( old_context != IntPtr.Zero )
			{
				// Restore old context
				if ( !Wgl.wglMakeCurrent( old_hdc, old_context ) )
					throw new Exception( "wglMakeCurrent() failed" );

				// Share lists with old context
				if ( !Wgl.wglShareLists( old_context, _hRenderingContext ) )
					throw new Exception( "wglShareLists() failed" );
			}

			// Create RenderSystem context
			_glContext = new Win32Context( _hDeviceContext, _hRenderingContext );

			// make this window active
			this.IsActive = true;
		}

		protected override void dispose( bool disposeManagedResources )
		{
            if (!IsDisposed)
			{
				if ( disposeManagedResources )
				{

					if ( _hRenderingContext != IntPtr.Zero )
					{                                        // Do We Not Have A Rendering Context?
						if ( !Wgl.wglMakeCurrent( IntPtr.Zero, IntPtr.Zero ) )
						{         // Are We Able To Release The DC And RC Contexts?
							SWF.MessageBox.Show( "Release Of DC And RC Failed.", "SHUTDOWN ERROR", SWF.MessageBoxButtons.OK, SWF.MessageBoxIcon.Information );
						}

						if ( !Wgl.wglDeleteContext( _hRenderingContext ) )
						{                            // Are We Not Able To Delete The RC?
							SWF.MessageBox.Show( "Release Rendering Context Failed.", "SHUTDOWN ERROR", SWF.MessageBoxButtons.OK, SWF.MessageBoxIcon.Information );
						}
						_hRenderingContext = IntPtr.Zero;                                          // Set RC To NULL
					}
				}

				// There are no unmanaged resources to release, but
				// if we add them, they need to be released here.
			}

			// If it is available, make the call to the
			// base class's Dispose(Boolean) method
			base.dispose( disposeManagedResources );

			// make sure this window is no longer active
			this.IsActive = false;
		}

		public override void Reposition( int left, int right )
		{

		}

		public override void Resize( int width, int height )
		{

			//Gl.glMatrixMode(Gl.GL_PROJECTION);	// Select The Projection Matrix
			//Gl.glLoadIdentity();		// Reset The Projection Matrix

			//// Calculate The Aspect Ratio Of The Window
			//Glu.gluPerspective(45.0f, width / height, 0.1f, 100.0f);

			//Gl.glMatrixMode(Gl.GL_MODELVIEW);	// Select The Modelview Matrix
			//Gl.glLoadIdentity();		// Reset The Modelview Matrix

			return;
		}

		public override void WindowMovedOrResized()
		{
            if ( _hWindow != IntPtr.Zero )
            {
                SWF.Control ctrl = SWF.Form.FromHandle( _hWindow );
                this.top = ctrl.Top;
                this.left = ctrl.Left;
                this.width = ctrl.ClientRectangle.Width;
                this.height = ctrl.ClientRectangle.Height;
            }

			// Update dimensions incase changed
            foreach ( Viewport entry in this.ViewportList.Values )
			{
                entry.UpdateDimensions();
			}

		}

		private SWF.Form GetForm( SWF.Control windowHandle )
		{
			SWF.Control tmp = windowHandle;

			if ( windowHandle == null )
				return null;
			if ( tmp is SWF.Form )
				return (SWF.Form)tmp;
			do
			{
				tmp = tmp.Parent;
			} while ( !( tmp is SWF.Form ) );

			return (SWF.Form)tmp;
		}


		public override void SwapBuffers( bool waitForVSync )
		{
			int sync = waitForVSync ? 1: 0;
			Wgl.wglSwapIntervalEXT( sync );
			if ( !_isExternalGLControl )
			{
				// swap buffers
				Gdi.SwapBuffersFast( _hDeviceContext );
			}
		}

		public override void CopyContentsToMemory( PixelBox dst, FrameBuffer buffer )
		{
			if ( ( dst.Left < 0 ) || ( dst.Right > Width ) ||
				( dst.Top < 0 ) || ( dst.Bottom > Height ) ||
				( dst.Front != 0 ) || ( dst.Back != 1 ) )
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
			rsys.Viewport = this.GetViewport( 0 );

			// Must change the packing to ensure no overruns!
			Gl.glPixelStorei( Gl.GL_PACK_ALIGNMENT, 1 );

			Gl.glReadBuffer( ( buffer == RenderTarget.FrameBuffer.Front ) ? Gl.GL_FRONT : Gl.GL_BACK );
			Gl.glReadPixels( dst.Left, dst.Top, dst.Width, dst.Height, format, type, dst.Data );

			// restore default alignment
			Gl.glPixelStorei( Gl.GL_PACK_ALIGNMENT, 4 );

			//vertical flip

			{
				int rowSpan = dst.Width * PixelUtil.GetNumElemBytes( dst.Format );
				int height = dst.Height;
				byte[] tmpData = new byte[ rowSpan * height ];
				unsafe
				{
					var dataPtr = dst.Data.ToBytePointer();
					//int *srcRow = (uchar *)dst.data, *tmpRow = tmpData + (height - 1) * rowSpan;

					for ( int row = height - 1, tmpRow = 0; row >= 0; row--, tmpRow++ )
					{
						for ( int col = 0; col < rowSpan; col++ )
						{
							tmpData[ tmpRow * rowSpan + col ] = dataPtr[ row * rowSpan + col ];
						}

					}
				}
                var tmpDataHandle = BufferBase.Wrap(tmpData);
				Memory.Copy( tmpDataHandle, dst.Data, rowSpan * height );
			}

		}

		#endregion

        public override bool RequiresTextureFlipping
        {
            get { return false; }
        }
    }
}
