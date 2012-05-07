using Axiom.Graphics;

using GLenum = OpenTK.Graphics.ES20.All;

namespace Axiom.RenderSystems.OpenGLES2
{
	internal class GLES2FBORenderTexture : GLES2RenderTexture
	{
		private GLES2FrameBufferObject fb;

		public GLES2FBORenderTexture( GLES2FBOManager manager, string name, GLES2SurfaceDesc target, bool writeGamma, int fsaa )
			: base( name, target, writeGamma, fsaa )
		{
			this.fb.BindSurface( 0, target );

			width = this.fb.Width;
			height = this.fb.Height;
		}

		public override object this[ string attribute ]
		{
			get
			{
				if ( attribute == "FBO" )
				{
					return this.fb;
				}
				return base[ attribute ];
			}
		}

		public override void SwapBuffers( bool waitForVSync )
		{
			this.fb.SwapBuffers();
		}

		public override bool AttachDepthBuffer( DepthBuffer ndepthBuffer )
		{
			bool result;
			result = base.AttachDepthBuffer( ndepthBuffer );
			if ( result )
			{
				this.fb.AttachDepthBuffer( ndepthBuffer );
			}

			return result;
		}

		public override void DetachDepthBuffer()
		{
			this.fb.DetachDepthBuffer();
		}

		public override void _DetachDepthBuffer()
		{
			this.fb.DetachDepthBuffer();
			base._DetachDepthBuffer();
		}
	}
}
