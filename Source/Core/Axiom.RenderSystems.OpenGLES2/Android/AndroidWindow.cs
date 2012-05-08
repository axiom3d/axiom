#region LGPL License

/*
Axiom Graphics Engine Library
Copyright (C) 2003-2010 Axiom Project Team
This file is part of Axiom.RenderSystems.OpenGLES
C# version developed by bostich.

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
//     <license see="http://axiomengine.sf.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;

using Axiom.Graphics;
using Axiom.Media;

using Javax.Microedition.Khronos.Egl;

using OpenTK.Graphics;
using OpenTK.Platform.Android;

using NativeWindowType = System.IntPtr;
using NativeDisplayType = System.IntPtr;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGLES2.Android
{
	internal class AndroidWindow : RenderWindow
	{
		private bool _isClosed;
		private bool _isVisible;
		private bool _isTopLevel;
		private bool _isExternal;
		private bool _isGLControl;

		private AndroidContext _glContext;
		private AndroidSupport _glSupport;
		private EGLContext _context;
		private NativeWindowType _window;
		private NativeDisplayType _nativeDisplay;
		private EGLDisplay _eglDisplay;
		private EGLConfig _eglConfig;
		private EGLSurface _eglSurface;

		/// <summary>
		/// </summary>
		/// <param name="display"> </param>
		/// <param name="win"> </param>
		/// <returns> </returns>
		protected EGLSurface CreateSurfaceFromWindow( EGLDisplay display, NativeWindowType win )
		{
			throw new NotImplementedException();
		}

		public AndroidWindow()
		{
			//OpenTK.Platform.Utilities.CreateGraphicsContext(new OpenTK.Graphics.GraphicsMode(), null, 1, 1, OpenTK.Graphics.GraphicsContextFlags.Default);
		}

		#region RenderWindow Members

		public override bool RequiresTextureFlipping
		{
			get { throw new NotImplementedException(); }
		}

		public override object this[ string attribute ]
		{
			get
			{
				switch ( attribute.ToLower() )
				{
					case "glcontext":
						return this._glContext;
					case "window":
						return this._window;
					case "nativewindow":
						return this._window;
					default:
						return null;
				}
			}
		}

		public override bool IsClosed
		{
			get { return this._window == null && this._glContext == null; }
		}

		public override void Reposition( int left, int right )
		{
			throw new NotImplementedException();
		}

		public override void Destroy()
		{
			throw new NotImplementedException();
		}

		public override void Resize( int width, int height )
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// </summary>
		/// <param name="name"> </param>
		/// <param name="width"> </param>
		/// <param name="height"> </param>
		/// <param name="fullScreen"> </param>
		/// <param name="miscParams"> </param>
		public override void Create( string name, int width, int height, bool fullScreen, Collections.NamedParameterList miscParams )
		{
			string title = name;
			bool vsync = false;
			int depthBuffer = GraphicsMode.Default.Depth;
			float displayFrequency = 60f;
			string border = "resizable";

			this.name = name;
			this.width = width;
			this.height = height;
			this.colorDepth = 32;
			IsFullScreen = fullScreen;

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
							this.fsaa = Int32.Parse( entry.Value.ToString() );
							break;
						case "colourDepth":
						case "colorDepth":
							this.colorDepth = Int32.Parse( entry.Value.ToString() );
							break;
						case "vsync":
							vsync = entry.Value.ToString() != "No";
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
							var androidContext = (AndroidGraphicsContext) entry.Value;
							this._glContext = new AndroidContext( androidContext, this._glSupport );
							break;

						case "externalWindowHandle":
							object handle = entry.Value;
							IntPtr ptr = IntPtr.Zero;
							if ( handle is IntPtr )
							{
								ptr = (IntPtr) handle;
							}
							else if ( handle is int )
							{
								ptr = new IntPtr( (int) handle );
							}
							this._window = ptr;

							fullScreen = false;
							IsActive = true;
							break;

						case "externalWindow":
							fullScreen = false;
							IsActive = true;
							break;

						default:
							break;
					}
				}
			}

			#endregion Parameter Handling
		}

		/// <summary>
		/// </summary>
		/// <param name="pb"> </param>
		/// <param name="buffer"> </param>
		public override void CopyContentsToMemory( PixelBox pb, RenderTarget.FrameBuffer buffer )
		{
			throw new NotImplementedException();
		}

		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				if ( disposeManagedResources )
				{
					//if ( _glContext != null ) // Do We Not Have A Rendering Context?
					//{
					//    _glContext.SetCurrent();
					//    _glContext.Dispose();
					//    _glContext = null;
					//}

					//if ( _window != null )
					//{
					//    if ( IsFullScreen )
					//        displayDevice.RestoreResolution();

					//    _window.Close();
					//    _window = null;
					//}
				}

				// There are no unmanaged resources to release, but
				// if we add them, they need to be released here.
			}
			// If it is available, make the call to the
			// base class's Dispose(Boolean) method
			base.dispose( disposeManagedResources );
		}

		#endregion RenderWindow Members
	}
}
