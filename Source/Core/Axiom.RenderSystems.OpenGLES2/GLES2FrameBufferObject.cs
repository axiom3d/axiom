#region MIT/X11 License

//Copyright © 2003-2012 Axiom 3D Rendering Engine Project
//
//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//THE SOFTWARE.

#endregion License

#region SVN Version Information

// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Text;

using Axiom.Graphics;

using OpenTK.Graphics.ES20;

using GLenum = OpenTK.Graphics.ES20.All;

#endregion Namespace Declarations
			
namespace Axiom.RenderSystems.OpenGLES2
{
	internal class GLES2FrameBufferObject : IDisposable
	{
		private readonly GLES2FBOManager _manager;
		private readonly int _numSamples;
		private int _fb;
		private int _multiSampleFB;
		private GLES2SurfaceDesc _multiSampleColorBuffer;
		private GLES2SurfaceDesc _depth, _stencil;
		//Arbitrary number of texture surfaces
		private readonly GLES2SurfaceDesc[] _color = new GLES2SurfaceDesc[ Axiom.Configuration.Config.MaxMultipleRenderTargets ];

		public GLES2FrameBufferObject( GLES2FBOManager manager, int fsaa )
		{
			this._manager = manager;
			this._numSamples = fsaa;
			GL.GenFramebuffers( 1, ref this._fb );
			GLES2Config.GlCheckError( this );

			this._numSamples = 0;
			this._multiSampleFB = 0;

			/*Port notes
			 * Ogre has a #if GL_APPLE_framebuffer_multisample
			 * conditional here for checking if multisampling is supported
			 * however GLenum doesn't contain members for what it's checking for
			 * so this will skipped by default
			 */

			//Will we need a second FBO to do multisampling?
			if ( this._numSamples > 0 )
			{
				GL.GenFramebuffers( 1, ref this._multiSampleFB );
				GLES2Config.GlCheckError( this );
			}

			//Initialize state
			this._depth.buffer = null;
			this._stencil.buffer = null;
			for ( int x = 0; x < Axiom.Configuration.Config.MaxMultipleRenderTargets; x++ )
			{
				this._color[ x ].buffer = null;
			}
		}

		/// <summary>
		///   Binds a surface to a certain attachment point. attachemnt: 0..Axiom.Config.MaxMultipleRenderTarget-1
		/// </summary>
		/// <param name="attachment"> </param>
		/// <param name="target"> </param>
		public void BindSurface( int attachment, GLES2SurfaceDesc target )
		{
			this._color[ attachment ] = target;
			//Re-initialize
			if ( this._color[ 0 ].buffer != null )
			{
				this.Initialize();
			}
		}

		/// <summary>
		///   Unbind attachment
		/// </summary>
		/// <param name="attachment"> </param>
		public void UnbindSurface( int attachment )
		{
			this._color[ attachment ].buffer = null;
			//Re-initialize if buffer 0 still bound
			if ( this._color[ 0 ].buffer != null )
			{
				this.Initialize();
			}
		}

		/// <summary>
		///   Bind FrameBufferObject
		/// </summary>
		public void Bind()
		{
			//Bind it to FBO
			var fb = this._multiSampleFB > 0 ? this._multiSampleFB : this._fb;
			GL.BindFramebuffer( GLenum.Framebuffer, fb );
			GLES2Config.GlCheckError( this );
		}

		/// <summary>
		///   Swap buffers - only useful when using multisample buffers
		/// </summary>
		public void SwapBuffers()
		{
			/*Port notes
			 * Left out on account of dependence on GLenum members that don't exist
			 * specifically GLenum.ReadFramebufferApple and GLenum.DrawFrameBuffer
			 * 
			 */
		}

		///<summary>
		///  This function acts very similar to <see cref="GLES2FBORenderTexture.AttachDepthBuffer">The difference between D3D & OGL is that D3D setups the DepthBuffer before rendering,
		///                                       while OGL setups the DepthBuffer per FBO. So the DepthBuffer (RenderBuffer) needs to
		///                                       be attached for OGL.
		///</summary>
		///<param name="depthBuffer"> </param>
		public void AttachDepthBuffer( DepthBuffer depthBuffer )
		{
			var glDepthBuffer = depthBuffer as GLES2DepthBuffer;
			GL.BindFramebuffer( GLenum.Framebuffer, this._multiSampleFB > 0 ? this._multiSampleFB : this._fb );
			GLES2Config.GlCheckError( this );

			if ( glDepthBuffer != null )
			{
				GLES2RenderBuffer depthBuf = glDepthBuffer.DepthBuffer;
				GLES2RenderBuffer stencilBuf = glDepthBuffer.StencilBuffer;


				//Attach depth buffer, if it has one.
				if ( depthBuf != null )
				{
					depthBuf.BindToFramebuffer( GLenum.DepthAttachment, 0 );
				}

				//Attach stencil buffer, if it has one.
				if ( stencilBuf != null )
				{
					stencilBuf.BindToFramebuffer( GLenum.StencilAttachment, 0 );
				}
			}
			else
			{
				GL.FramebufferRenderbuffer( GLenum.Framebuffer, GLenum.DepthAttachment, GLenum.Renderbuffer, 0 );
				GLES2Config.GlCheckError( this );

				GL.FramebufferRenderbuffer( GLenum.Framebuffer, GLenum.StencilAttachment, GLenum.Renderbuffer, 0 );
				GLES2Config.GlCheckError( this );
			}
		}

		public void DetachDepthBuffer()
		{
			GL.BindFramebuffer( GLenum.Framebuffer, this._multiSampleFB > 0 ? this._multiSampleFB : this._fb );
			GLES2Config.GlCheckError( this );

			GL.FramebufferRenderbuffer( GLenum.Framebuffer, GLenum.DepthAttachment, GLenum.Renderbuffer, 0 );
			GLES2Config.GlCheckError( this );

			GL.FramebufferRenderbuffer( GLenum.Framebuffer, GLenum.StencilAttachment, GLenum.Renderbuffer, 0 );
			GLES2Config.GlCheckError( this );
		}

		public GLES2SurfaceDesc GetSurface( int attachment )
		{
			return this._color[ attachment ];
		}

		/// <summary>
		///   Initializes object (find suitable depth and stencil format). Must be called every time the bindings change. It will throw an exception if: -Attachment point 0 has no binding -Not all bound surfaces have the same size -Not all bound surfaces have the same internal format
		/// </summary>
		private void Initialize()
		{
			this._manager.ReleaseRenderBuffer( this._depth );
			this._manager.ReleaseRenderBuffer( this._stencil );
			this._manager.ReleaseRenderBuffer( this._multiSampleColorBuffer );
			//First buffer must be bound
			if ( this._color[ 0 ].buffer == null )
			{
				throw new Core.AxiomException( "Attachment 0 must have surface attached" );
			}
			// If we're doing multisampling, then we need another FBO which contains a
			// renderbuffer which is set up to multisample, and we'll blit it to the final 
			// FBO afterwards to perform the multisample resolve. In that case, the 
			// mMultisampleFB is bound during rendering and is the one with a depth/stencil

			var maxSupportedMRTs = Core.Root.Instance.RenderSystem.Capabilities.MultiRenderTargetCount;

			//Store basic stats
			int width = this._color[ 0 ].buffer.Width;
			int height = this._color[ 0 ].buffer.Height;
			GLenum format = this._color[ 0 ].buffer.GLFormat;

			//Bind simple buffer to add color attachments
			GL.BindFramebuffer( OpenTK.Graphics.ES20.All.Framebuffer, this._fb );
			GLES2Config.GlCheckError( this );

			//bind all attachment points to frame buffer
			for ( int x = 0; x < maxSupportedMRTs; x++ )
			{
				if ( this._color[ x ].buffer != null )
				{
					if ( this._color[ x ].buffer.Width != width || this._color[ x ].buffer.Height != height )
					{
						var ss = new StringBuilder();
						ss.Append( "Attachment " + x + " has incompatible size " );
						ss.Append( this._color[ x ].buffer.Width + "x" + this._color[ x ].buffer.Height );
						ss.Append( ". It must be of the same as the size of surface 0, " );
						ss.Append( width + "x" + height.ToString() );
						ss.Append( "." );
						throw new Core.AxiomException( ss.ToString() );
					}
					if ( this._color[ x ].buffer.GLFormat != format )
					{
						throw new Core.AxiomException( "Attachment " + x.ToString() + " has incompatible format." );
					}
					this._color[ x ].buffer.BindToFramebuffer( (GLenum) ( (int) GLenum.ColorAttachment0 ) + x, this._color[ x ].zoffset );
				}
				else
				{
					//Detach
					GL.FramebufferRenderbuffer( GLenum.Framebuffer, (GLenum) ( (int) GLenum.ColorAttachment0 ) + x, GLenum.Renderbuffer, 0 );
					GLES2Config.GlCheckError( this );
				}
			}
			//Now deal with depth/stencil
			if ( this._multiSampleFB > 0 )
			{
				//Bind multisample buffer
				GL.BindFramebuffer( GLenum.Framebuffer, this._multiSampleFB );
				GLES2Config.GlCheckError( this );

				//Create AA render buffer (color)
				//note, this can be shared too because we blit it to the final FBO
				//right after the render is finished
				this._multiSampleColorBuffer = this._manager.RequestRenderBuffer( format, width, height, this._numSamples );

				//Attach it, because we won't be attaching below and non-multisample has
				//actually been attached to other FBO
				this._multiSampleColorBuffer.buffer.BindToFramebuffer( GLenum.ColorAttachment0, this._multiSampleColorBuffer.zoffset );

				//depth & stencil will be dealt with below
			}

			/// Depth buffer is not handled here anymore.
			/// See GLES2FrameBufferObject::attachDepthBuffer() & RenderSystem::setDepthBufferFor()
			var bufs = new GLenum[ Configuration.Config.MaxMultipleRenderTargets ];
			for ( int x = 0; x < Configuration.Config.MaxMultipleRenderTargets; x++ )
			{
				//Fill attached color buffers
				if ( this._color[ x ].buffer != null )
				{
					bufs[ x ] = (GLenum) ( (int) GLenum.ColorAttachment0 ) + x;
				}
				else
				{
					bufs[ x ] = GLenum.None;
				}
			}

			//Check status
			var status = GL.CheckFramebufferStatus( GLenum.Framebuffer );
			GLES2Config.GlCheckError( this );

			//Possible todo:
			/*Port notes
			 * Ogre leaves a comment indicating that the screen buffer for iOS 
			 * is 1 as opposed to 0 in the case of most other devices
			 * I'd like to think that OpenTK takes care of this for us and have defaulted it
			 * to 0
			 */

			GL.BindFramebuffer( GLenum.Framebuffer, 0 );
			GLES2Config.GlCheckError( this );

			switch ( status )
			{
				case GLenum.FramebufferComplete:
					//All is good
					break;
				case GLenum.FramebufferUnsupported:
					throw new Core.AxiomException( "All framebuffer formats with this texture internal format unsupported" );
				default:
					throw new Core.AxiomException( "Framebuffer incomplete or other FBO status error" );
			}
		}

		public int Width
		{
			get { return this._color[ 0 ].buffer.Width; }
		}

		public int Height
		{
			get { return this._color[ 0 ].buffer.Height; }
		}

		public GLenum Format
		{
			get { return this._color[ 0 ].buffer.GLFormat; }
		}

		public int FSAA
		{
			get { return this._numSamples; }
		}

		public GLES2FBOManager Manager
		{
			get { return this._manager; }
		}

		public void Dispose()
		{
			this._manager.ReleaseRenderBuffer( this._depth );
			this._manager.ReleaseRenderBuffer( this._stencil );
			this._manager.ReleaseRenderBuffer( this._multiSampleColorBuffer );

			//Delete framebuffer object
			GL.DeleteFramebuffers( 1, ref this._fb );
			GLES2Config.GlCheckError( this );

			if ( this._multiSampleFB > 0 )
			{
				GL.DeleteFramebuffers( 1, ref this._multiSampleFB );
				GLES2Config.GlCheckError( this );
			}
		}
	}
}
