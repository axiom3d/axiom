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
//     <license see="http://axiomengine.sf.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using Axiom.Graphics;

using GLenum = OpenTK.Graphics.ES20.All;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGLES2
{
	/// <summary>
	///   OpenGL supports 2 different methods: FBO & Copy. Each one has it's own limitations. Non-FBO methods are solved using "dummy" DepthBuffers. That is, a DepthBuffer pointer is attached to the RenderTarget (for the sake of consistency) but it doesn't actually contain a Depth surface/renderbuffer (mDepthBuffer & mStencilBuffer are null pointers all the time) Those dummy DepthBuffers are identified thanks to their GL context. Note that FBOs don't allow sharing with the main window's depth buffer. Therefore even when FBO is enabled, a dummy DepthBuffer is still used to manage the windows.
	/// </summary>
	[OgreVersion( 1, 8, 0, "It's from trunk rev.'b0d2092773fb'" )]
	public class GLES2DepthBuffer : DepthBuffer
	{
		protected int _multiSampleQuality;
		protected GLES2Context _creatorContext;
		protected GLES2RenderBuffer _depthBuffer;
		protected GLES2RenderBuffer _stencilBuffer;
		protected GLES2RenderSystem _renderSystem;

		/// <summary>
		/// </summary>
		/// <param name="poolId"> </param>
		/// <param name="renderSystem"> </param>
		/// <param name="creatorContext"> </param>
		/// <param name="depth"> </param>
		/// <param name="stencil"> </param>
		/// <param name="width"> </param>
		/// <param name="height"> </param>
		/// <param name="fsaa"> </param>
		/// <param name="multiSampleQuality"> </param>
		/// <param name="isManual"> </param>
		public GLES2DepthBuffer( PoolId poolId, GLES2RenderSystem renderSystem, GLES2Context creatorContext, GLES2RenderBuffer depth, GLES2RenderBuffer stencil, int width, int height, int fsaa, int multiSampleQuality, bool isManual )
			: base( poolId, 0, width, height, fsaa, "", isManual )
		{
			this._creatorContext = creatorContext;
			this._multiSampleQuality = multiSampleQuality;
			this._depthBuffer = depth;
			this._stencilBuffer = stencil;
			this._renderSystem = renderSystem;

			if ( this._depthBuffer != null )
			{
				switch ( this._depthBuffer.GLFormat )
				{
					case OpenTK.Graphics.ES20.All.DepthComponent16:
						bitDepth = 16;
						break;
					case GLenum.DepthComponent24Oes:
					case GLenum.DepthComponent32Oes:
					case GLenum.Depth24Stencil8Oes: //Packed depth / stencil
						bitDepth = 32;
						break;
				}
			}
		}

		protected override void dispose( bool disposeManagedResources )
		{
			if ( this._stencilBuffer != null && this._stencilBuffer != this._depthBuffer )
			{
				this._stencilBuffer.Dispose();
				this._stencilBuffer = null;
			}
			if ( this._depthBuffer == null )
			{
				this._depthBuffer.Dispose();
				this._depthBuffer = null;
			}
			base.dispose( disposeManagedResources );
		}

		/// <summary>
		/// </summary>
		/// <param name="renderTarget"> </param>
		/// <returns> </returns>
		public override bool IsCompatible( RenderTarget renderTarget )
		{
			bool retVal = false;

			//Check standard stuff first.
			if ( this._renderSystem.Capabilities.HasCapability( Capabilities.RTTDepthbufferResolutionLessEqual ) )
			{
				if ( base.IsCompatible( renderTarget ) )
				{
					return false;
				}
			}
			else
			{
				if ( Width != renderTarget.Width || Height != renderTarget.Height || Fsaa != renderTarget.FSAA )
				{
					return false;
				}
			}
			//Now check this is the appropriate format
			GLES2FrameBufferObject fbo = null;
			fbo = (GLES2FrameBufferObject) renderTarget[ "FBO" ];

			if ( fbo == null )
			{
				var windowContext = (GLES2Context) renderTarget[ "GLCONTEXT" ];

				//Non-FBO and FBO depth surfaces don't play along, only dummmies which match the same
				//context
				if ( this._depthBuffer == null && this._stencilBuffer == null && this._creatorContext == windowContext )
				{
					retVal = true;
				}
			}
			else
			{
				//Check this isn't a dummy non-FBO depth buffer with an FBO target, don't mix them.
				//If you don't want depth buffer, use a Null Depth Buffer, not a dummy one.
				if ( this._depthBuffer != null || this._stencilBuffer != null )
				{
					var internalFormat = fbo.Format;
					GLenum depthFormat = GLenum.None, stencilFormat = GLenum.None;
					this._renderSystem.GetDepthStencilFormatFor( internalFormat, ref depthFormat, ref stencilFormat );
					bool bSameDepth = false;
					if ( this._depthBuffer != null )
					{
						bSameDepth |= this._depthBuffer.GLFormat == depthFormat;
					}

					bool bSameStencil = false;
					if ( this._stencilBuffer == null || this._stencilBuffer == this._depthBuffer )
					{
						bSameDepth = stencilFormat == GLenum.None;
					}
					else
					{
						if ( this._stencilBuffer != null )
						{
							bSameStencil = stencilFormat == this._stencilBuffer.GLFormat;
						}
					}

					retVal = bSameDepth && bSameStencil;
				}
			}

			return retVal;
		}


		public GLES2Context GLContext
		{
			get { return this._creatorContext; }
		}

		public GLES2RenderBuffer DepthBuffer
		{
			get { return this._depthBuffer; }
		}

		public GLES2RenderBuffer StencilBuffer
		{
			get { return this._stencilBuffer; }
		}
	}
}
