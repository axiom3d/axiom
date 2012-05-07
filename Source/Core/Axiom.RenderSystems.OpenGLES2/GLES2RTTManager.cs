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
using Axiom.Media;

using GLenum = OpenTK.Graphics.ES20.All;

#endregion Namespace Declarations
			
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
