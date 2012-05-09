using Axiom.Graphics;
using Axiom.Media;

namespace Axiom.RenderSystems.OpenGLES
{
	public class GLESPBRTTManager : GLESRTTManager
	{
		private struct PBRef
		{
			public GLESPBuffer Buffer;
			public int ReferenceCount;
		}

		private readonly PBRef[] _pixelBuffers = new PBRef[ (int) PixelComponentType.Count ];

		private GLESSupport _support;
		private RenderTarget _mainWindow;
		private GLESContext _mainContext;

		public void GLESRTTManager( GLESSupport support, RenderTarget mainWindow )
		{
			this._support = support;
			this._mainWindow = mainWindow;
			this._mainContext = (GLESContext) this._mainWindow[ "glcontext" ];
		}

		public void RequestPBuffer( PixelComponentType ctype, int width, int height )
		{
			PBRef current = this._pixelBuffers[ (int) ctype ];
			// Check size
			if ( current.Buffer != null )
			{
				if ( current.Buffer.Width < width || current.Buffer.Height < height )
				{
					// If the current PBuffer is too small, destroy it and create a new one					
					//current.Buffer.Dispose();
					current.Buffer = null;
				}
			}

			if ( current.Buffer == null )
			{
				// Create pbuffer via rendersystem
				current.Buffer = this._support.CreatePixelBuffer( ctype, width, height );
			}
			++current.ReferenceCount;
			this._pixelBuffers[ (int) ctype ] = current;
		}

		public void ReleasePBuffer( PixelComponentType ctype )
		{
			--this._pixelBuffers[ (int) ctype ].ReferenceCount;
			if ( this._pixelBuffers[ (int) ctype ].ReferenceCount == 0 )
			{
				//_pixelBuffers[ (int)ctype ].Buffer.Dispose();
				this._pixelBuffers[ (int) ctype ].Buffer = null;
			}
		}

		#region GLESRTTManager Implementation

		public override Graphics.RenderTexture CreateRenderTexture( string name, GLESSurfaceDescription target, bool writeGama, int fsaa )
		{
			return new GLESPBRenderTexture( this, name, target, writeGama, fsaa );
		}

		public override bool CheckFormat( Media.PixelFormat format )
		{
			return true;
		}

		public override void Bind( Graphics.RenderTarget target )
		{
			// Nothing to do here
			// Binding of context is done by GL subsystem, as contexts are also used for RenderWindows
		}

		public override void Unbind( Graphics.RenderTarget target )
		{
			// Copy on unbind
			GLESSurfaceDescription surface;
			surface.Buffer = null;
			surface = (GLESSurfaceDescription) target[ "TARGET" ];
			if ( surface.Buffer != null )
			{
				( (GLESTextureBuffer) surface.Buffer ).CopyFromFramebuffer( surface.ZOffset );
			}
		}

		#endregion GLESRTTManager Implementation
	}
}
