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
using NativeWindowType = System.IntPtr;
using NativeDisplayType = System.IntPtr;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGLES.OpenTKGLES
{
	class OpenTKGLESWindow : RenderWindow
	{
		protected bool _isClosed;
		protected bool _isVisible;
		protected bool _isTopLevel;
		protected bool _isExternal;
		protected bool _isGLControl;
		protected OpenTKGLESSupport _glSupport;
		protected EGLContext _context;
		protected NativeWindowType _window;
		protected NativeDisplayType _nativeDisplay;
		protected EGLDisplay _eglDisplay;
		protected EGLConfig _eglConfig;
		protected EGLSurface _eglSurface;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="display"></param>
		/// <param name="win"></param>
		/// <returns></returns>
		protected EGLSurface CreateSurfaceFromWindow( EGLDisplay display, NativeWindowType win )
		{
			throw new NotImplementedException();
		}

		public override bool IsClosed
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public override void Reposition( int left, int right )
		{
			throw new NotImplementedException();
		}

		public override void Resize( int width, int height )
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="fullScreen"></param>
		/// <param name="miscParams"></param>
		public override void Create( string name, int width, int height, bool fullScreen, Collections.NamedParameterList miscParams )
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="pb"></param>
		/// <param name="buffer"></param>
		public override void CopyContentsToMemory( PixelBox pb, RenderTarget.FrameBuffer buffer )
		{
			throw new NotImplementedException();
		}

	}
}