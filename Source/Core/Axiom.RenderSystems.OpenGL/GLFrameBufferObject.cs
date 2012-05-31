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
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Axiom.Core;
using Axiom.Media;
using Axiom.Graphics;
using Tao.OpenGl;
using Axiom.Configuration;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGL
{
	internal class GLFrameBufferObject : IDisposable
	{
		#region Fields and Properties

		private int _frameBuffer;
		private GLSurfaceDesc _depth;
		private GLSurfaceDesc _stencil;
		private readonly GLSurfaceDesc[] _color;

		public int Height
		{
			get
			{
				//assert( _color[ 0 ].buffer );
				return this._color[ 0 ].Buffer.Height;
			}
		}

		public int Width
		{
			get
			{
				//assert( _color[ 0 ].buffer );
				return this._color[ 0 ].Buffer.Width;
			}
		}

		public PixelFormat Format
		{
			get
			{
				//assert( _color[ 0 ].buffer );
				return this._color[ 0 ].Buffer.Format;
			}
		}

		public int FSAA
		{
			get
			{
				return 0; // Not implemented, yet
			}
		}

		private GLFBORTTManager _manager;

		public GLFBORTTManager Manager
		{
			get
			{
				return this._manager;
			}
		}

		private GLSurfaceDesc _surfaceDesc;

		public GLSurfaceDesc SurfaceDesc
		{
			get
			{
				return this._surfaceDesc;
			}
		}

		#endregion Fields and Properties

		#region Construction and Destruction

		public GLFrameBufferObject( GLFBORTTManager manager )
		{
			this._manager = manager;

			/// Generate framebuffer object
			Gl.glGenFramebuffersEXT( 1, out this._frameBuffer );

			/// Initialize state
			this._color = new GLSurfaceDesc[Config.MaxMultipleRenderTargets];
		}

		~GLFrameBufferObject()
		{
			dispose( false );
		}

		#endregion Construction and Destruction

		#region Methods

		/// <summary>
		/// Bind a surface to a certain attachment point.
		/// </summary>
		/// <param name="attachment">0..Config.MaxMultipleRenderTargets-1</param>
		/// <param name="target"></param>
		public void BindSurface( int attachment, GLSurfaceDesc target )
		{
			//assert( attachment < OGRE_MAX_MULTIPLE_RENDER_TARGETS );
			this._color[ attachment ] = target;
			// Re-initialise
			if ( this._color[ 0 ].Buffer != null )
			{
				_initialize();
			}
		}

		/// <summary>
		/// Unbind attachment
		/// </summary>
		/// <param name="attachment"></param>
		public void UnbindSurface( int attachment )
		{
			//assert( attachment < OGRE_MAX_MULTIPLE_RENDER_TARGETS );
			this._color[ attachment ].Buffer = null;
			// Re-initialise if buffer 0 still bound
			if ( this._color[ 0 ].Buffer == null )
			{
				_initialize();
			}
		}

		/// <summary>
		/// Bind FrameBufferObject
		/// </summary>
		public void Bind()
		{
			/// Bind it to FBO
			Gl.glBindFramebufferEXT( Gl.GL_FRAMEBUFFER_EXT, this._frameBuffer );
		}

		/// <summary>
		/// Initialize object (find suitable depth and stencil format).
		/// </summary>
		/// Must be called every time the bindings change.
		/// It fails with an exception (ArgumentException) if:
		/// - Attachment point 0 has no binding
		/// - Not all bound surfaces have the same size
		/// - Not all bound surfaces have the same internal format
		/// <remarks>
		private void _initialize()
		{
			// Release depth and stencil, if they were bound
			this._manager.ReleaseRenderBuffer( this._depth );
			this._manager.ReleaseRenderBuffer( this._stencil );
			/// First buffer must be bound
			if ( this._color[ 0 ].Buffer == null )
			{
				throw new ArgumentException( "Attachment 0 must have surface attached." );
			}

			/// Bind FBO to frame buffer
			Bind();

			/// Store basic stats
			int width = this._color[ 0 ].Buffer.Width;
			int height = this._color[ 0 ].Buffer.Height;
			int glFormat = this._color[ 0 ].Buffer.GLFormat;
			PixelFormat format = this._color[ 0 ].Buffer.Format;

			/// Bind all attachment points to frame buffer
			for ( int x = 0; x < Config.MaxMultipleRenderTargets; ++x )
			{
				if ( this._color[ x ].Buffer != null )
				{
					if ( this._color[ x ].Buffer.Width != width || this._color[ x ].Buffer.Height != height )
					{
						throw new ArgumentException(
							String.Format(
								"Attachment {0} has incompatible size {1}x{2}. It must be of the same as the size of surface 0, {3}x{4}.", x,
								this._color[ x ].Buffer.Width, this._color[ x ].Buffer.Height, width, height ) );
					}
					if ( this._color[ x ].Buffer.GLFormat != glFormat )
					{
						throw new ArgumentException( String.Format( "Attachment {0} has incompatible format.", x ) );
					}
					this._color[ x ].Buffer.BindToFramebuffer( Gl.GL_COLOR_ATTACHMENT0_EXT + x, this._color[ x ].ZOffset );
				}
				else
				{
					// Detach
					Gl.glFramebufferRenderbufferEXT( Gl.GL_FRAMEBUFFER_EXT, Gl.GL_COLOR_ATTACHMENT0_EXT + x, Gl.GL_RENDERBUFFER_EXT, 0 );
				}
			}
			// Find suitable depth and stencil format that is compatible with color format
			int depthFormat, stencilFormat;
			this._manager.GetBestDepthStencil( format, out depthFormat, out stencilFormat );

			// Request surfaces
			this._depth = this._manager.RequestRenderBuffer( depthFormat, width, height );
			if ( depthFormat == GLFBORTTManager.GL_DEPTH24_STENCIL8_EXT )
			{
				// bind same buffer to depth and stencil attachments
				this._manager.RequestRenderBuffer( this._depth );
				this._stencil = this._depth;
			}
			else
			{
				// separate stencil
				this._stencil = this._manager.RequestRenderBuffer( stencilFormat, width, height );
			}

			/// Attach/detach surfaces
			if ( this._depth.Buffer != null )
			{
				this._depth.Buffer.BindToFramebuffer( Gl.GL_DEPTH_ATTACHMENT_EXT, this._depth.ZOffset );
			}
			else
			{
				Gl.glFramebufferRenderbufferEXT( Gl.GL_FRAMEBUFFER_EXT, Gl.GL_DEPTH_ATTACHMENT_EXT, Gl.GL_RENDERBUFFER_EXT, 0 );
			}
			if ( this._stencil.Buffer != null )
			{
				this._stencil.Buffer.BindToFramebuffer( Gl.GL_STENCIL_ATTACHMENT_EXT, this._stencil.ZOffset );
			}
			else
			{
				Gl.glFramebufferRenderbufferEXT( Gl.GL_FRAMEBUFFER_EXT, Gl.GL_STENCIL_ATTACHMENT_EXT, Gl.GL_RENDERBUFFER_EXT, 0 );
			}

			/// Do glDrawBuffer calls
			var bufs = new int[Config.MaxMultipleRenderTargets];
			int n = 0;
			for ( int x = 0; x < Config.MaxMultipleRenderTargets; ++x )
			{
				// Fill attached color buffers
				if ( this._color[ x ].Buffer != null )
				{
					bufs[ x ] = Gl.GL_COLOR_ATTACHMENT0_EXT + x;
					// Keep highest used buffer + 1
					n = x + 1;
				}
				else
				{
					bufs[ x ] = Gl.GL_NONE;
				}
			}

			if ( this._manager.GLSupport.CheckExtension( "GL_ARB_draw_buffers" ) )
			{
				/// Drawbuffer extension supported, use it
				Gl.glDrawBuffers( n, bufs );
			}
			else if ( this._manager.GLSupport.CheckExtension( "GL_ATI_draw_buffers" ) )
			{
				Gl.glDrawBuffersATI( n, bufs );
			}
			else
			{
				/// In this case, the capabilities will not show more than 1 simultaneaous render target.
				Gl.glDrawBuffer( bufs[ 0 ] );
			}
			/// No read buffer, by default, if we want to read anyway we must not forget to set this.
			Gl.glReadBuffer( Gl.GL_NONE );

			/// Check status
			int status;
			status = Gl.glCheckFramebufferStatusEXT( Gl.GL_FRAMEBUFFER_EXT );

			/// Bind main buffer
			Gl.glBindFramebufferEXT( Gl.GL_FRAMEBUFFER_EXT, 0 );

			switch ( status )
			{
				case Gl.GL_FRAMEBUFFER_COMPLETE_EXT:
					// All is good
					break;
				case Gl.GL_FRAMEBUFFER_UNSUPPORTED_EXT:
					throw new ArgumentException( "All framebuffer formats with this texture internal format unsupported" );
				default:
					throw new ArgumentException( "Framebuffer incomplete or other FBO status error" );
			}
		}

		#endregion Methods

		#region IDisposable Implementation

		#region isDisposed Property

		/// <summary>
		/// Determines if this instance has been disposed of already.
		/// </summary>
		protected bool isDisposed { get; set; }

		#endregion isDisposed Property

		/// <summary>
		/// Class level dispose method
		/// </summary>
		/// <remarks>
		/// When implementing this method in an inherited class the following template should be used;
		/// protected override void dispose( bool disposeManagedResources )
		/// {
		/// 	if ( !isDisposed )
		/// 	{
		/// 		if ( disposeManagedResources )
		/// 		{
		/// 			// Dispose managed resources.
		/// 		}
		///
		/// 		// There are no unmanaged resources to release, but
		/// 		// if we add them, they need to be released here.
		/// 	}
		///
		/// 	// If it is available, make the call to the
		/// 	// base class's Dispose(Boolean) method
		/// 	base.dispose( disposeManagedResources );
		/// }
		/// </remarks>
		/// <param name="disposeManagedResources">True if Unmanaged resources should be released.</param>
		protected virtual void dispose( bool disposeManagedResources )
		{
			if ( !isDisposed )
			{
				if ( disposeManagedResources )
				{
					// Dispose managed resources.
					this._manager.ReleaseRenderBuffer( this._depth );
					this._manager.ReleaseRenderBuffer( this._stencil );
					this._manager = null;
				}

				/// Delete framebuffer object
				try
				{
					Gl.glDeleteFramebuffersEXT( 1, ref this._frameBuffer );
				}
				catch ( AccessViolationException ave )
				{
					LogManager.Instance.Write( "Error Deleting Framebuffer[{0}].", this._frameBuffer );
				}
			}
			isDisposed = true;
		}

		public void Dispose()
		{
			dispose( true );
			GC.SuppressFinalize( this );
		}

		#endregion IDisposable Implementation
	}
}