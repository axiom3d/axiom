using Axiom.Graphics;

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
