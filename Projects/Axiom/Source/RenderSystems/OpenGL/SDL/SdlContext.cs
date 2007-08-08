#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2006  Axiom Project Team

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
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using System.Text;
using Axiom.Core;
using Tao.Sdl;
using Tao.OpenGl;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGL
{
	internal class SdlContext : GLContext
	{
		#region Fields and Properties

		private IntPtr _hDeviceContext;
		private IntPtr _hRenderingContext;

		#endregion Fields and Properties

		#region Construction and Destruction

		public SdlContext( IntPtr hDeviceContext, IntPtr hRenderingContext )
		{
			_hDeviceContext = hDeviceContext;
			_hRenderingContext = hRenderingContext;
		}

		~SdlContext()
		{
			dispose( true );
		}

		protected override void dispose( bool disposeManagedResources )
		{
			if ( !isDisposed )
			{
				if ( disposeManagedResources )
				{
					// Dispose managed resources.
					GLRenderSystem rs = (GLRenderSystem)Root.Instance.RenderSystem;
					//todo rs.UnRegisterContext( this );
				}

				// There are no unmanaged resources to release, but
				// if we add them, they need to be released here.
			}
			isDisposed = true;

			// If it is available, make the call to the
			// base class's Dispose(Boolean) method
			base.dispose( disposeManagedResources );
		}

		#endregion Construction and Destruction

		#region GLContext Implementation

		public override void SetCurrent()
		{
#if WIN32
			Wgl.wglMakeCurrent( _hDeviceContext, _hRenderingContext );
#else
			Glx.glXMakeCurrent( _display, _drawable, _context );
#endif
		}

		public override void EndCurrent()
		{
			Wgl.wglMakeCurrent( IntPtr.Zero, IntPtr.Zero );
		}

		public override GLContext Clone()
		{
			// Create new context based on own HDC
			IntPtr newCtx = Wgl.wglCreateContext( _hDeviceContext );

			if ( newCtx == IntPtr.Zero )
			{
				throw new Exception( "Error calling wglCreateContext" );
			}

			Wgl.wglMakeCurrent( IntPtr.Zero, IntPtr.Zero );

			// Share lists with old context
			if ( !Wgl.wglShareLists( _hRenderingContext, newCtx ) )
			{
				Wgl.wglDeleteContext( newCtx );
				throw new Exception( "wglShareLists() failed: " );
			}

			return new SdlContext( _hDeviceContext, newCtx );
		}

		#endregion GLContext Implementation
	}
}
