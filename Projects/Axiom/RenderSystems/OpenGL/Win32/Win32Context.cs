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
//     <id value="$Id: Win32Context.cs 1656 2009-06-10 15:43:29Z borrillis $"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using System.Text;
using Axiom.Core;
using Tao.Platform.Windows;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGL
{
	internal class Win32Context : GLContext
	{
		#region Fields and Properties

		private IntPtr _hDeviceContext;
		private IntPtr _hRenderingContext;

		#endregion Fields and Properties

		#region Construction and Destruction

		public Win32Context( IntPtr hDeviceContext, IntPtr hRenderingContext )
		{
			_hDeviceContext = hDeviceContext;
			_hRenderingContext = hRenderingContext;
		}

		~Win32Context()
		{
			dispose( false );
		}

		protected override void dispose( bool disposeManagedResources )
		{
			if ( !isDisposed )
			{
				if ( disposeManagedResources )
				{
					// Dispose managed resources.
					GLRenderSystem rs = (GLRenderSystem)Root.Instance.RenderSystem;
					rs.UnRegisterContext( this );
				}

				// There are no unmanaged resources to release, but
				// if we add them, they need to be released here.
			}

			// If it is available, make the call to the
			// base class's Dispose(Boolean) method
			base.dispose( disposeManagedResources );
		}

		#endregion Construction and Destruction

		#region GLContext Implementation

		public override void SetCurrent()
		{
			Wgl.wglMakeCurrent( _hDeviceContext, _hRenderingContext );
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

			return new Win32Context( _hDeviceContext, newCtx );
		}

        private bool vSync;
        public override bool VSync
        {
            get { return vSync; }
            set
            {
                if (Wgl.IsExtensionSupported("wglSwapIntervalEXT"))
                    Wgl.wglSwapIntervalEXT((vSync = value) ? 1 : 0);
            }
        }
        
        #endregion GLContext Implementation
    }
}
