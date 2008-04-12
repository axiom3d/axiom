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
//     <id value="$Id:"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;

using Axiom.Core;
using Axiom.Graphics;

using Tao.Sdl;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGL
{
	internal class SdlWindow
	{
		#region Fields and Properties

		static private IntPtr _hWindow;
		static private RenderWindow _renderWindow;
		static private string _title;
		static private int _top;
		static private int _left;
		static private int _width;
		static private int _height;
		static private int _colorDepth;
		static private bool _fullScreen;

		public IntPtr Handle
		{
			get
			{
				Sdl.SDL_SysWMinfo_Windows wmInfo;
				Sdl.SDL_GetWMInfo( out wmInfo );
				return new IntPtr( wmInfo.window );
			}
		}

		/// <summary>
		///		Get/Set the RenderWindow associated with this form.
		/// </summary>
		public RenderWindow RenderWindow
		{
			get
			{
				return SdlWindow._renderWindow;
			}
			set
			{
				SdlWindow._renderWindow = value;
			}
		}

		public int Top
		{
			get
			{
				return SdlWindow._top;
			}
			set
			{
				SdlWindow._top = value;
				if ( _renderWindow != null )
					_renderWindow.Reposition( _left, _top );
			}
		}

		public int Left
		{
			get
			{
				return SdlWindow._left;
			}
			set
			{
				SdlWindow._left = value;
				if ( _renderWindow != null )
					_renderWindow.Reposition( _left, _top );
			}
		}

		public int Width
		{
			get
			{
				return SdlWindow._width;
			}
			set
			{
				SdlWindow._width = value;
				if ( _renderWindow != null )
					_renderWindow.Resize( _width, _height );
			}
		}

		public int Height
		{
			get
			{
				return SdlWindow._height;
			}
			set
			{
				SdlWindow._height = value;
				if ( _renderWindow != null )
					_renderWindow.Resize( _width, _height );
			}
		}

		public int ColorDepth
		{
			get
			{
				return SdlWindow._colorDepth;
			}
			set
			{
				SdlWindow._colorDepth = value;
			}
		}

		public bool FullScreen
		{
			get
			{
				return SdlWindow._fullScreen;
			}
			set
			{
				SdlWindow._fullScreen = value;
			}
		}

		public string Title
		{
			get
			{
				return SdlWindow._title;
			}
			set
			{
				SdlWindow._title = value;
				// set the window text for windowed mode
				if ( ! FullScreen )
				{
					Sdl.SDL_WM_SetCaption( Title, null );
				}

			}
		}
		#endregion Fields and Properties

		#region Construction and Destruction

		public SdlWindow()
		{
		}

		#endregion Construction and Destruction

		#region Methods

		public void Show()
		{
			InitializeWindow();
		}

		public void Move( int top, int left )
		{
			SdlWindow._top = top;
			SdlWindow._left = left;
			InitializeWindow();
		}

		public void Resize( int width, int height )
		{
			SdlWindow._width = width;
			SdlWindow._height = height;
			InitializeWindow();
		}

		private void InitializeWindow()
		{
			int flags = Sdl.SDL_OPENGL | Sdl.SDL_HWPALETTE | Sdl.SDL_RESIZABLE;

			// full screen?
			if ( FullScreen )
			{
				flags |= Sdl.SDL_FULLSCREEN;
			}

			// set the video mode (and create the surface)
			// TODO: Grab return val once changed to the right type
			SdlWindow._hWindow = Sdl.SDL_SetVideoMode( Width, Height, ColorDepth, flags );

			if ( _hWindow == IntPtr.Zero )
				throw new Exception( "Failed to create SDL window :" + Sdl.SDL_GetError() );

		}

		internal void WndProc()
		{
            Sdl.SDL_Event sdlEvent;

			while (Sdl.SDL_PollEvent(out sdlEvent) != 0)
			{
			    switch (sdlEvent.type)
			    {
			        case Sdl.SDL_KEYDOWN:
			        case Sdl.SDL_KEYUP:						
			        break;

			        case Sdl.SDL_VIDEORESIZE:
			        {
						if ( _renderWindow != null )
							_renderWindow.Resize( sdlEvent.resize.w, sdlEvent.resize.h );
			        }
			        break;

			        case Sdl.SDL_QUIT: 
					{
						if ( _renderWindow != null )
							_renderWindow.Dispose();
					}
					break;
			    }
			}

		}

		#endregion Methods

	}
}