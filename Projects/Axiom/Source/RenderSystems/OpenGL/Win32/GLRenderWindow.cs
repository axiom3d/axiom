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
using System.Windows.Forms;

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
		private Win32Context _context;
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

		public override bool IsVisible
		{
			get
			{
				return base.IsVisible;
			}
			set
			{
				base.IsVisible = value;
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

		public override object GetCustomAttribute( string attribute )
		{
			switch ( attribute.ToLower() )
			{
				case "glcontext":
					//TODO : return _glContext;
					return null;
				case "window":
					System.Windows.Forms.Control ctrl = System.Windows.Forms.Control.FromChildHandle( _hWindow );
					return ctrl;
					break;
				default:
					return base.GetCustomAttribute( attribute );
			}

		}

		public override void Create( string name, int width, int height, bool isFullScreen, NamedParameterList miscParams )
		{
			if ( _hWindow != IntPtr.Zero )
				dispose( true );

			_hWindow = IntPtr.Zero;
			this.Name = name;
			this.IsFullScreen = isFullScreen;
			this._isClosed = false;

			// load window defaults
			this.left = this.top = -1; // centered
			this.Width = width;
			this.Height = height;
			this._displayFrequency = 0;
			this.isDepthBuffered = true;
			this.ColorDepth = IsFullScreen ? 32 : 16;

			IntPtr parentHwnd = IntPtr.Zero;
			string title = name;
			bool vsync = false;
			int fsaa = 0;
			string border;
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
							break;
						case "outerDimensions":
							break;
						case "displayFrequency":
							if ( IsFullScreen )
								_displayFrequency = Int32.Parse( entry.Value.ToString() );
							break;
						case "colorDepth":
							if ( IsFullScreen )
								ColorDepth = Int32.Parse( entry.Value.ToString() );
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
				form.StartPosition = FormStartPosition.CenterScreen;

				if ( IsFullScreen )
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
					if ( parentHwnd != IntPtr.Zero )
					{
						form.Owner = (Form)Control.FromHandle( parentHwnd );
					}
					else
					{
						//TODO : Implement "border" and "fixed" window options.
					}

					form.Top = top;
					form.Left = left;
					form.FormBorderStyle = FormBorderStyle.FixedSingle;
					form.WindowState = FormWindowState.Normal;
					form.Text = title;
				}

				form.Show();
				_hWindow = form.Handle;

				if ( isFullScreen )
				{
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
				}

			}

			IntPtr old_hdc = Wgl.wglGetCurrentDC();
			IntPtr old_context = Wgl.wglGetCurrentContext();

			Control ctrl = Form.FromHandle( _hWindow );
			Form frm = (Form)ctrl.TopLevelControl;
			this.top = frm.Top;
			this.left = frm.Left;
			this.Width = frm.ClientRectangle.Width;
			this.Height = frm.ClientRectangle.Height;

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
				IntPtr wglSwapIntervalEXT = Wgl.wglGetProcAddress( "wglSwapIntervalEXT" );
				if ( wglSwapIntervalEXT != IntPtr.Zero )
					Wgl.wglSwapIntervalEXT( wglSwapIntervalEXT, vsync ? 1 : 0 );
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
			_context = new Win32Context( _hDeviceContext, _hRenderingContext );

			// make this window active
			this.IsActive = true;
		}

		protected override void dispose( bool disposeManagedResources )
		{
			if ( !isDisposed )
			{
				if ( disposeManagedResources )
				{

					if ( _hRenderingContext != IntPtr.Zero )
					{                                        // Do We Not Have A Rendering Context?
						if ( !Wgl.wglMakeCurrent( IntPtr.Zero, IntPtr.Zero ) )
						{         // Are We Able To Release The DC And RC Contexts?
							MessageBox.Show( "Release Of DC And RC Failed.", "SHUTDOWN ERROR", MessageBoxButtons.OK, MessageBoxIcon.Information );
						}

						if ( !Wgl.wglDeleteContext( _hRenderingContext ) )
						{                            // Are We Not Able To Delete The RC?
							MessageBox.Show( "Release Rendering Context Failed.", "SHUTDOWN ERROR", MessageBoxButtons.OK, MessageBoxIcon.Information );
						}
						_hRenderingContext = IntPtr.Zero;                                          // Set RC To NULL
					}
				}

				// There are no unmanaged resources to release, but
				// if we add them, they need to be released here.
			}
			isDisposed = true;

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

		}

		public override void SwapBuffers( bool waitForVSync )
		{
			//int sync = waitForVSync ? 1: 0;
			//Wgl.wglSwapIntervalEXT((uint)sync);
			if ( !_isExternalGLControl )
			{
				// swap buffers
				Gdi.SwapBuffersFast( _hDeviceContext );
			}
		}

		/// <summary>
		///		Saves RenderWindow contents to a stream.
		/// </summary>
		/// <param name="stream">Target stream to save the window contents to.</param>
		public override void Save( Stream stream )
		{
			// create a RGB buffer
			byte[] buffer = new byte[ Width * Height * 3 ];

			// read the pixels from the GL buffer
			Gl.glReadPixels( 0, 0, Width - 1, Height - 1, Gl.GL_RGB, Gl.GL_UNSIGNED_BYTE, buffer );

			stream.Write( buffer, 0, buffer.Length );
		}

		#endregion
	}
}
