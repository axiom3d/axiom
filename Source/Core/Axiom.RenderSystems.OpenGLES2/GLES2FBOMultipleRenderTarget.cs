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

using Axiom.Graphics;

#endregion Namespace Declarations
			
namespace Axiom.RenderSystems.OpenGLES2
{
	/// <summary>
	///   MultiRenderTarget for GL ES 2.x.
	/// </summary>
	internal class GLES2FBOMultiRenderTarget : MultiRenderTarget
	{
		private readonly GLES2FrameBufferObject fbo;

		public GLES2FBOMultiRenderTarget( GLES2FBOManager manager, string name )
			: base( name )
		{
			this.fbo = new GLES2FrameBufferObject( manager, 0 ); //Ogre TODO: multisampling on MRTs?
		}

		public override object this[ string attribute ]
		{
			get
			{
				if ( name == "FBO" )
				{
					return base[ attribute ] as GLES2FrameBufferObject;
				}
				return base[ attribute ];
			}
		}

		public override bool RequiresTextureFlipping
		{
			get { return true; }
		}

		public override bool AttachDepthBuffer( DepthBuffer ndepthBuffer )
		{
			bool result = false;

			if ( ( result = base.AttachDepthBuffer( depthBuffer ) ) )
			{
				this.fbo.AttachDepthBuffer( depthBuffer );
			}

			return result;
		}

		public override void DetachDepthBuffer()
		{
			this.fbo.DetachDepthBuffer();
			base.DetachDepthBuffer();
		}

		public override void _DetachDepthBuffer()
		{
			this.fbo.DetachDepthBuffer();
			base._DetachDepthBuffer();
		}

		protected override void BindSurfaceImpl( int attachment, RenderTexture target )
		{
			//Check if the render target is in the rendertarget.FBO map
			GLES2FrameBufferObject fboojb = null;
			fboojb = (GLES2FrameBufferObject) target[ "FBO" ];
			this.fbo.BindSurface( attachment, fboojb.GetSurface( 0 ) );

			width = this.fbo.Width;
			height = this.fbo.Height;
		}

		protected override void UnbindSurfaceImpl( int attachment )
		{
			this.fbo.UnbindSurface( attachment );

			//Set width and height
			width = this.fbo.Width;
			height = this.fbo.Height;
		}
	}
}
