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

using Axiom.Core;
using Axiom.Utilities;

using OpenTK.Graphics.ES11;

using OpenGL = OpenTK.Graphics.ES11.GL;
using OpenGLOES = OpenTK.Graphics.ES11.GL.Oes;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGLES
{
	/// <summary>
	///   Frame Buffer Object abstraction.
	/// </summary>
	public class GLESFrameBufferObject : IDisposable
	{
		private GLESFBORTTManager _manager;
		private int _numSamples;
		private int _fb;
		private int _multiSampleFB;
		private GLESSurfaceDescription _multisampleColorBuffer;
		private readonly GLESSurfaceDescription _depth;
		private readonly GLESSurfaceDescription _stencil;
		private readonly GLESSurfaceDescription[] _color = new GLESSurfaceDescription[ Configuration.Config.MaxMultipleRenderTargets ];

		/// <summary>
		///   Gets the FBO manager.
		/// </summary>
		public GLESFBORTTManager Manager
		{
			get { return this._manager; }
		}

		/// <summary>
		/// </summary>
		public int Width
		{
			get
			{
				Contract.Requires( this._color[ 0 ].Buffer != null );
				return this._color[ 0 ].Buffer.Width;
			}
		}

		/// <summary>
		/// </summary>
		public int Height
		{
			get
			{
				Contract.Requires( this._color[ 0 ].Buffer != null );
				return this._color[ 0 ].Buffer.Height;
			}
		}

		/// <summary>
		/// </summary>
		public Media.PixelFormat Format
		{
			get
			{
				Contract.Requires( this._color[ 0 ].Buffer != null );
				return this._color[ 0 ].Buffer.Format;
			}
		}

		/// <summary>
		/// </summary>
		public int FSAA
		{
			get { return this._numSamples; }
		}

		/// <summary>
		/// </summary>
		/// <param name="manager"> </param>
		/// <param name="fsaa"> </param>
		public GLESFrameBufferObject( GLESFBORTTManager manager, int fsaa )
		{
			/// Generate framebuffer object
			OpenGLOES.GenFramebuffers( 1, ref this._fb );
			GLESConfig.GlCheckError( this );
			this._depth = new GLESSurfaceDescription();
			this._stencil = new GLESSurfaceDescription();
			for ( int x = 0; x < Configuration.Config.MaxMultipleRenderTargets; x++ )
			{
				this._color[ x ] = new GLESSurfaceDescription();
			}
		}

		/// <summary>
		/// </summary>
		public void Dispose()
		{
			Manager.ReleaseRenderbuffer( this._depth );
			Manager.ReleaseRenderbuffer( this._stencil );
			Manager.ReleaseRenderbuffer( this._multisampleColorBuffer );

			/// Delete framebuffer object
			OpenGLOES.DeleteFramebuffers( 1, ref this._fb );
			GLESConfig.GlCheckError( this );

			if ( this._multiSampleFB != 0 )
			{
				OpenGLOES.DeleteFramebuffers( 1, ref this._multiSampleFB );
			}

			GLESConfig.GlCheckError( this );
		}

		/// <summary>
		///   Bind a surface to a certain attachment point.
		/// </summary>
		/// <param name="attachment"> 0..MaxMultipleRenderTargets-1 </param>
		/// <param name="target"> </param>
		public void BindSurface( int attachment, GLESSurfaceDescription target )
		{
			Contract.Requires( attachment < Configuration.Config.MaxMultipleRenderTargets );
			this._color[ attachment ] = target;
			// Re-initialise
			if ( this._color[ 0 ].Buffer != null )
			{
				Intialize();
			}
		}

		/// <summary>
		///   Unbind attachment
		/// </summary>
		/// <param name="attachment"> </param>
		public void UnbindSurface( int attachment )
		{
			Contract.Requires( attachment < Configuration.Config.MaxMultipleRenderTargets );
			this._color[ attachment ].Buffer.Dispose();
			// Re-initialise if buffer 0 still bound
			if ( this._color[ 0 ].Buffer != null )
			{
				Intialize();
			}
		}

		/// <summary>
		///   Bind FrameBufferObject
		/// </summary>
		public void Bind()
		{
			/// Bind it to FBO
			int fb = this._multiSampleFB != 0 ? this._multiSampleFB : this._fb;
			OpenGLOES.BindFramebuffer( All.FramebufferOes, fb );
			GLESConfig.GlCheckError( this );
		}

		/// <summary>
		///   Swap buffers - only useful when using multisample buffers.
		/// </summary>
		public void SwapBuffers()
		{
			//do nothing
		}

		/// <summary>
		/// </summary>
		/// <param name="attachment"> </param>
		/// <returns> </returns>
		public GLESSurfaceDescription GetSurface( int attachment )
		{
			return this._color[ attachment ];
		}

		/// <summary>
		/// </summary>
		private void Intialize()
		{
			// Release depth and stencil, if they were bound
			Manager.ReleaseRenderbuffer( this._depth );
			Manager.ReleaseRenderbuffer( this._stencil );
			Manager.ReleaseRenderbuffer( this._multisampleColorBuffer );

			/// First buffer must be bound
			if ( this._color[ 0 ].Buffer == null )
			{
				throw new AxiomException( "Attachment 0 must have surface attached" );
			}

			// If we're doing multisampling, then we need another FBO which contains a
			// renderbuffer which is set up to multisample, and we'll blit it to the final 
			// FBO afterwards to perform the multisample resolve. In that case, the 
			// mMultisampleFB is bound during rendering and is the one with a depth/stencil

			/// Store basic stats
			int width = this._color[ 0 ].Buffer.Width;
			int height = this._color[ 0 ].Buffer.Height;
			All format = this._color[ 0 ].Buffer.GLFormat;
			Media.PixelFormat axiomFormat = this._color[ 0 ].Buffer.Format;

			// Bind simple buffer to add colour attachments
			OpenGLOES.BindFramebuffer( All.FramebufferOes, this._fb );
			GLESConfig.GlCheckError( this );

			/// Bind all attachment points to frame buffer
			for ( int x = 0; x < Configuration.Config.MaxMultipleRenderTargets; x++ )
			{
				if ( this._color[ x ].Buffer != null )
				{
					if ( this._color[ x ].Buffer.Width != width || this._color[ x ].Buffer.Height != height )
					{
						string ss = string.Empty;
						ss += "Attachment " + x + " has incompatible size ";
						ss += this._color[ x ].Buffer.Width + "x" + this._color[ 0 ].Buffer.Height;
						ss += ". It must be of the same as the size of surface 0, ";
						ss += width + "x" + height;
						ss += ".";
						throw new AxiomException( ss );
					}
					if ( this._color[ x ].Buffer.GLFormat != format )
					{
						string ss = string.Empty;
						ss += "Attachment " + x + " has incompatible format.";
						throw new AxiomException( ss );
					}
					this._color[ x ].Buffer.BindToFramebuffer( All.ColorAttachment0Oes + x, this._color[ x ].ZOffset );
				}
				else
				{
					// Detach
					OpenGLOES.FramebufferRenderbuffer( All.FramebufferOes, All.ColorAttachment0Oes + x, All.RenderbufferOes, 0 );
					GLESConfig.GlCheckError( this );
				}
			} //end for x

			// Now deal with depth / stencil
			if ( this._multiSampleFB != 0 )
			{
				// Bind multisample buffer
				OpenGLOES.BindFramebuffer( All.FramebufferOes, this._multiSampleFB );
				GLESConfig.GlCheckError( this );

				// Create AA render buffer (color)
				// note, this can be shared too because we blit it to the final FBO
				// right after the render is finished
				this._multisampleColorBuffer = Manager.RequestRenderbuffer( format, width, height, this._numSamples );

				// Attach it, because we won't be attaching below and non-multisample has
				// actually been attached to other FBO
				this._multisampleColorBuffer.Buffer.BindToFramebuffer( All.ColorAttachment0Oes, this._multisampleColorBuffer.ZOffset );

				// depth & stencil will be dealt with below
			}

			/// Depth buffer is not handled here anymore.
			/// See GLESFrameBufferObject::attachDepthBuffer() & RenderSystem::setDepthBufferFor()

			/// Do glDrawBuffer calls
			var bufs = new All[ Configuration.Config.MaxMultipleRenderTargets ];
			int n = 0;
			for ( int x = 0; x < Configuration.Config.MaxMultipleRenderTargets; x++ )
			{
				// Fill attached colour buffers
				if ( this._color[ x ].Buffer != null )
				{
					bufs[ x ] = All.ColorAttachment0Oes + x;
					// Keep highest used buffer + 1
					n = x + 1;
				}
				else
				{
					bufs[ x ] = All.Never;
				}
			} //end for x

			/// Check status
			All status = OpenGLOES.CheckFramebufferStatus( All.FramebufferOes );
			GLESConfig.GlCheckError( this );
			/// Bind main buffer
#if AXIOM_PLATFORM_IPHONE
	// The screen buffer is 1 on iPhone
            OpenGLOES.BindFramebuffer(All.FramebufferOes, 1);
#else
			OpenGLOES.BindFramebuffer( All.FramebufferOes, 0 );
#endif
			GLESConfig.GlCheckError( this );

			switch ( status )
			{
				case All.FramebufferCompleteOes:
					// everything is fine
					break;
				case All.FramebufferUnsupportedOes:
					throw new AxiomException( "All framebuffer formats with this texture internal format unsupported" );
				default:
					throw new AxiomException( "Framebuffer incomplete or other FBO status error" );
			}
		}
	}
}
