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
using System.Diagnostics;

using Axiom.Graphics;
using Axiom.Media;

using Tao.OpenGl;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGL
{
	/// <summary>
	/// Renderbuffer surface.  Needs FBO extension.
	/// </summary>
	class GLRenderBuffer : GLHardwarePixelBuffer
	{
		#region Fields and Properties

		/// <summary>
		/// In case this is a  render buffer
		/// </summary>
		private int _renderBufferId;

		#endregion Fields and Properties

		#region Construction and Destruction

		public GLRenderBuffer( int format, int width, int height )
			: base( width, height, 1, GLPixelUtil.GetClosestPixelFormat( format ), BufferUsage.WriteOnly )
		{
			this.GLFormat = format;
			/// Generate renderbuffer
			Gl.glGenRenderbuffersEXT( 1, out _renderBufferId );
			/// Bind it to FBO
			Gl.glBindRenderbufferEXT( Gl.GL_RENDERBUFFER_EXT, _renderBufferId );

			/// Allocate storage for depth buffer
			Gl.glRenderbufferStorageEXT( Gl.GL_RENDERBUFFER_EXT, format, width, height );
		}

		#endregion Construction and Destruction

		#region GLHardwarePixelBuffer Implementation

		public override void BindToFramebuffer( int attachment, int zOffset )
		{
			Debug.Assert( zOffset < Depth );
			Gl.glFramebufferRenderbufferEXT( Gl.GL_FRAMEBUFFER_EXT, attachment, Gl.GL_RENDERBUFFER_EXT, _renderBufferId );
		}

		protected override void dispose( bool disposeManagedResources )
		{
			if ( !isDisposed )
			{
				if ( disposeManagedResources )
				{
					// Dispose managed resources.
				}

				Gl.glDeleteRenderbuffersEXT( 1, ref _renderBufferId );
				// There are no unmanaged resources to release, but
				// if we add them, they need to be released here.
			}
			isDisposed = true;

			// If it is available, make the call to the
			// base class's Dispose(Boolean) method
			base.dispose( disposeManagedResources );
		}

		#endregion GLHardwarePixelBuffer Implementation
	}
}