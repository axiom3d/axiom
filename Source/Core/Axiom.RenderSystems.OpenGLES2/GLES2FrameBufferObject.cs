using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Axiom.Graphics;
using Axiom.Media;
using GL = OpenTK.Graphics.ES20.GL;
using GLenum = OpenTK.Graphics.ES20.All;

namespace Axiom.RenderSystems.OpenGLES2
{
	class GLES2FrameBufferObject : IDisposable
	{
		private GLES2FBOManager _manager;
		private int _numSamples;
		private int _fb;
		private int _multiSampleFB;
		private GLES2SurfaceDesc _multiSampleColorBuffer;
		private GLES2SurfaceDesc _depth, _stencil;
		//Arbitrary number of texture surfaces
		private GLES2SurfaceDesc[] _color = new GLES2SurfaceDesc[Axiom.Configuration.Config.MaxMultipleRenderTargets];

		public GLES2FrameBufferObject(GLES2FBOManager manager, int fsaa)
		{
			_manager = manager;
			_numSamples = fsaa;
			GL.GenFramebuffers(1, ref _fb);

			_numSamples = 0;
			_multiSampleFB = 0;
			
			/*Port notes
			 * Ogre has a #if GL_APPLE_framebuffer_multisample
			 * conditional here for checking if multisampling is supported
			 * however GLenum doesn't contain members for what it's checking for
			 * so this will skipped by default
			 */
			
			//Will we need a second FBO to do multisampling?
			if (_numSamples > 0)
			{
				GL.GenFramebuffers(1, ref _multiSampleFB);
			}

			//Initialize state
			_depth.buffer = null;
			_stencil.buffer = null;
			for (int x = 0; x < Axiom.Configuration.Config.MaxMultipleRenderTargets; x++)
			{
				_color[x].buffer = null;
			}

		}
		/// <summary>
		/// Binds a surface to a certain attachment point.
		/// attachemnt: 0..Axiom.Config.MaxMultipleRenderTarget-1
		/// </summary>
		/// <param name="attachment"></param>
		/// <param name="target"></param>
		public void BindSurface(int attachment, GLES2SurfaceDesc target)
		{
			_color[attachment] = target;
			//Re-initialize
			if (_color[0].buffer != null)
				Initialize();
		}
		/// <summary>
		/// Unbind attachment
		/// </summary>
		/// <param name="attachment"></param>
		public void UnbindSurface(int attachment)
		{
			_color[attachment].buffer = null;
			//Re-initialize if buffer 0 still bound
			if (_color[0].buffer != null)
				Initialize();
		}
		/// <summary>
		/// Bind FrameBufferObject
		/// </summary>
		public void Bind()
		{
			//Bind it to FBO
			var fb = _multiSampleFB > 0 ? _multiSampleFB : _fb;
			GL.BindFramebuffer(GLenum.Framebuffer, fb);
		}
		/// <summary>
		/// Swap buffers - only useful when using multisample buffers
		/// </summary>
		public void SwapBuffers()
		{
			/*Port notes
			 * Left out on account of dependence on GLenum members that don't exist
			 * specifically GLenum.ReadFramebufferApple and GLenum.DrawFrameBuffer
             * 
			 */
            
		}
		/// <summary>
		/// This function acts very similar to <see cref="GLES2FBORenderTexture.AttachDepthBuffer"> 
		///	The difference between D3D & OGL is that D3D setups the DepthBuffer before rendering,
		///	while OGL setups the DepthBuffer per FBO. So the DepthBuffer (RenderBuffer) needs to
		///	be attached for OGL.
		/// </summary>
		/// <param name="depthBuffer"></param>
		public void AttachDepthBuffer(DepthBuffer depthBuffer)
		{
			GLES2DepthBuffer glDepthBuffer = depthBuffer as GLES2DepthBuffer;
			GL.BindFramebuffer(GLenum.Framebuffer, _multiSampleFB > 0 ? _multiSampleFB : _fb);

			if (glDepthBuffer != null)
			{
				GLES2RenderBuffer depthBuf = glDepthBuffer.DepthBuffer;
				GLES2RenderBuffer stencilBuf = glDepthBuffer.StencilBuffer;


				//Attach depth buffer, if it has one.
				if (depthBuf != null)
				{
					depthBuf.BindToFramebuffer(GLenum.DepthAttachment, 0);
				}

				//Attach stencil buffer, if it has one.
				if (stencilBuf != null)
					stencilBuf.BindToFramebuffer(GLenum.StencilAttachment, 0);
			}
			else
			{

				GL.FramebufferRenderbuffer(GLenum.Framebuffer, GLenum.DepthAttachment, GLenum.Renderbuffer, 0);

				GL.FramebufferRenderbuffer(GLenum.Framebuffer, GLenum.StencilAttachment, GLenum.Renderbuffer, 0);

			}
		}
		public void DetachDepthBuffer()
		{
			GL.BindFramebuffer(GLenum.Framebuffer, _multiSampleFB > 0 ? _multiSampleFB : _fb);

			GL.FramebufferRenderbuffer(GLenum.Framebuffer, GLenum.DepthAttachment, GLenum.Renderbuffer, 0);

			GL.FramebufferRenderbuffer(GLenum.Framebuffer, GLenum.StencilAttachment, GLenum.Renderbuffer, 0);
		}

		public GLES2SurfaceDesc GetSurface(int attachment)
		{
			return _color[attachment];
		}
		/// <summary>
		/// Initializes object (find suitable depth and stencil format).
		/// Must be called every time the bindings change.
		/// It will throw an exception if:
		/// -Attachment point 0 has no binding
		/// -Not all bound surfaces have the same size
		/// -Not all bound surfaces have the same internal format
		/// </summary>
		private void Initialize()
		{
			_manager.ReleaseRenderBuffer(_depth);
			_manager.ReleaseRenderBuffer(_stencil);
			_manager.ReleaseRenderBuffer(_multiSampleColorBuffer);
			//First buffer must be bound
			if (_color[0].buffer == null)
			{
				throw new Core.AxiomException("Attachment 0 must have surface attached");
			}
			// If we're doing multisampling, then we need another FBO which contains a
			// renderbuffer which is set up to multisample, and we'll blit it to the final 
			// FBO afterwards to perform the multisample resolve. In that case, the 
			// mMultisampleFB is bound during rendering and is the one with a depth/stencil

			var maxSupportedMRTs = Core.Root.Instance.RenderSystem.Capabilities.MultiRenderTargetCount;

			//Store basic stats
			int width = _color[0].buffer.Width;
			int height = _color[0].buffer.Height;
			GLenum format = _color[0].buffer.GLFormat;

			//Bind simple buffer to add color attachments
			GL.BindFramebuffer(OpenTK.Graphics.ES20.All.Framebuffer, _fb);

			//bind all attachment points to frame buffer
			for (int x = 0; x < maxSupportedMRTs; x++)
			{
				if (_color[x].buffer != null)
				{
					if (_color[x].buffer.Width != width || _color[x].buffer.Height != height)
					{
						StringBuilder ss = new StringBuilder();
						ss.Append("Attachment " + x + " has incompatible size ");
						ss.Append(_color[x].buffer.Width + "x" + _color[x].buffer.Height);
						ss.Append(". It must be of the same as the size of surface 0, ");
						ss.Append(width + "x" + height.ToString());
						ss.Append(".");
						throw new Core.AxiomException(ss.ToString());
					}
					if (_color[x].buffer.GLFormat != format)
					{
						throw new Core.AxiomException("Attachment " + x.ToString() + " has incompatible format.");
					}
					_color[x].buffer.BindToFramebuffer((GLenum)((int)GLenum.ColorAttachment0) + x, _color[x].zoffset);
				}
				else
				{
					//Detach
					GL.FramebufferRenderbuffer(GLenum.Framebuffer, (GLenum)((int)GLenum.ColorAttachment0) + x, GLenum.Renderbuffer, 0);

				}
			}
			//Now deal with depth/stencil
			if (_multiSampleFB > 0)
			{
				//Bind multisample buffer
				GL.BindFramebuffer(GLenum.Framebuffer, _multiSampleFB);
				
				//Create AA render buffer (color)
				//note, this can be shared too because we blit it to the final FBO
				//right after the render is finished
				_multiSampleColorBuffer = _manager.RequestRenderBuffer(format, width, height, _numSamples);

				//Attach it, because we won't be attaching below and non-multisample has
				//actually been attached to other FBO
				_multiSampleColorBuffer.buffer.BindToFramebuffer(GLenum.ColorAttachment0, _multiSampleColorBuffer.zoffset);

				//depth & stencil will be dealt with below

			}

			/// Depth buffer is not handled here anymore.
			/// See GLES2FrameBufferObject::attachDepthBuffer() & RenderSystem::setDepthBufferFor()
			GLenum[] bufs = new GLenum[Configuration.Config.MaxMultipleRenderTargets];
			for (int x = 0; x < Configuration.Config.MaxMultipleRenderTargets; x++)
			{
				//Fill attached color buffers
				if (_color[x].buffer != null)
				{
					bufs[x] = (GLenum)((int)GLenum.ColorAttachment0) + x;
				}
				else
				{
					bufs[x] = GLenum.None;
				}
			}

			//Check status
			var status = GL.CheckFramebufferStatus(GLenum.Framebuffer);

			//Possible todo:
			/*Port notes
			 * Ogre leaves a comment indicating that the screen buffer for iOS 
			 * is 1 as opposed to 0 in the case of most other devices
			 * I'd like to think that OpenTK takes care of this for us and have defaulted it
			 * to 0
			 */

			GL.BindFramebuffer(GLenum.Framebuffer, 0);

			switch (status)
			{
				case GLenum.FramebufferComplete:
					//All is good
					break;
				case GLenum.FramebufferUnsupported:
					throw new Core.AxiomException("All framebuffer formats with this texture internal format unsupported");
				default:
					throw new Core.AxiomException("Framebuffer incomplete or other FBO status error");
			}
		}

		public int Width
		{
			get { return _color[0].buffer.Width; }
		}
		public int Height
		{
			get { return _color[0].buffer.Height; }
		}
		public GLenum Format
		{
            get { return _color[0].buffer.GLFormat; }
		}
		public int FSAA
		{
			get { return _numSamples; }
		}
		public GLES2FBOManager Manager
		{
			get { return _manager; }
		}

		public void Dispose()
		{
			_manager.ReleaseRenderBuffer(_depth);
			_manager.ReleaseRenderBuffer(_stencil);
			_manager.ReleaseRenderBuffer(_multiSampleColorBuffer);

			//Delete framebuffer object
			GL.DeleteFramebuffers(1, ref _fb);

			if (_multiSampleFB > 0)
			{
				GL.DeleteFramebuffers(1, ref _multiSampleFB);
			}
		}
	}
}
