using Axiom.Graphics;
using Axiom.Media;

using GLenum = OpenTK.Graphics.ES20.All;

namespace Axiom.RenderSystems.OpenGLES2
{
	internal class GLES2RTTManager
	{
		private static GLES2RTTManager _instance = null;

		public virtual RenderTexture CreateRenderTexture( string name, GLES2SurfaceDesc target, bool writeGamme, int fsaa )
		{
			return null;
		}

		public virtual bool CheckFormat( PixelFormat format )
		{
			return false;
		}

		public virtual void Bind( RenderTarget target ) {}
		public virtual void Unbind( RenderTarget target ) {}

		public PixelFormat GetSupportedAlternative( PixelFormat format )
		{
			if ( this.CheckFormat( format ) )
			{
				return format;
			}

			//Find first alternative
			var pct = PixelUtil.GetComponentType( format );

			switch ( pct )
			{
				case PixelComponentType.Byte:
					format = PixelFormat.A8R8G8B8;
					break;
				case PixelComponentType.Short:
					format = PixelFormat.SHORT_RGBA;
					break;
				case PixelComponentType.Float16:
					format = PixelFormat.FLOAT16_RGBA;
					break;
				case PixelComponentType.Float32:
					format = PixelFormat.FLOAT32_RGBA;
					break;
				case PixelComponentType.Count:
				default:
					break;
			}

			if ( this.CheckFormat( format ) )
			{
				return format;
			}

			//If none at all, return to default
			return PixelFormat.A8R8G8B8;
		}

		public virtual void GetBestDepthStencil( GLenum internalColorFormat, ref GLenum depthFormat, ref GLenum stencilFormat )
		{
			depthFormat = GLenum.None;
			stencilFormat = GLenum.None;
		}

		public virtual Graphics.MultiRenderTarget CreateMultiRenderTarget( string name )
		{
			//Ogre TODO: Check rendersystem capabilities before throwing the exception
			throw new Core.AxiomException( "MultiRenderTarget is not supported" );
		}

		public static GLES2RTTManager Instance
		{
			get
			{
				if ( _instance == null )
				{
					_instance = new GLES2RTTManager();
				}
				return _instance;
			}
		}
	}
}
