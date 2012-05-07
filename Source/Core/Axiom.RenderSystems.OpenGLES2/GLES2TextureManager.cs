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

using Axiom.Core;
using Axiom.Media;

using OpenTK.Graphics.ES20;

using GLenum = OpenTK.Graphics.ES20.All;
using PixelFormat = Axiom.Media.PixelFormat;

#endregion Namespace Declarations
			
namespace Axiom.RenderSystems.OpenGLES2
{
	internal class GLES2TextureManager : TextureManager
	{
		private readonly GLES2Support glSupport;
		private int warningTextureID;

		public GLES2TextureManager( GLES2Support support )
		{
			this.glSupport = support;
			this.warningTextureID = 0;
			//Register with group manager
			ResourceGroupManager.Instance.RegisterResourceManager( ResourceType, this );

			this.CreateWarningTexture();
		}

		protected override void dispose( bool disposeManagedResources )
		{
			//Unregister with group manager
			ResourceGroupManager.Instance.UnregisterResourceManager( ResourceType );

			//Delte warning texture
			GL.DeleteTextures( 1, ref this.warningTextureID );
			base.dispose( disposeManagedResources );
		}

		public override bool IsHardwareFilteringSupported( Graphics.TextureType ttype, Media.PixelFormat format, Graphics.TextureUsage usage, bool preciseFormatOnly )
		{
			if ( format == PixelFormat.Unknown )
			{
				return false;
			}

			//Check native format
			PixelFormat nativeFormat = this.GetNativeFormat( ttype, format, usage );
			if ( preciseFormatOnly && format != nativeFormat )
			{
				return false;
			}

			//Assume non-floaitng point is supported always
			if ( !PixelUtil.IsFloatingPoint( nativeFormat ) )
			{
				return true;
			}

			return false;
		}

		protected override Resource _create( string name, ulong handle, string group, bool isManual, IManualResourceLoader loader, Collections.NameValuePairList createParams )
		{
			return new GLES2Texture( this, name, handle, group, isManual, loader, this.glSupport );
		}

		public override Media.PixelFormat GetNativeFormat( Graphics.TextureType ttype, Media.PixelFormat format, Graphics.TextureUsage usage )
		{
			var caps = Root.Instance.RenderSystem.Capabilities;

			//Check compressed texture support
			//if a compressed formt not supported, rever to PixelFormat.A8R8G8B8
			if ( PixelUtil.IsCompressed( format ) && !caps.HasCapability( Graphics.Capabilities.TextureCompressionDXT ) && !caps.HasCapability( Graphics.Capabilities.TextureCompressionPVRTC ) )
			{
				return PixelFormat.A8R8G8B8;
			}
			//if floating point texture not supported, rever to PixelFormat.A8R8G8B8
			if ( PixelUtil.IsFloatingPoint( format ) && !caps.HasCapability( Graphics.Capabilities.TextureFloat ) )
			{
				return PixelFormat.A8R8G8B8;
			}

			//Check if this is a valid rendertarget format
			if ( ( usage & Graphics.TextureUsage.RenderTarget ) == Graphics.TextureUsage.RenderTarget )
			{
				//Get closest supported alternative
				//if format is supported it's returned
				return GLES2RTTManager.Instance.GetSupportedAlternative( format );
			}

			//Supported
			return format;
		}

		/// <summary>
		///   Internal method to create a warning texture (bound when a texture unit is blank)
		/// </summary>
		protected void CreateWarningTexture()
		{
			//Generate warning texture
			int width = 8, height = 8;
			var data = new int[ width & height ];

			//Yellow/black stripes
			for ( int y = 0; y < height; y++ )
			{
				for ( int x = 0; x < width; x++ )
				{
					data[ y * width + x ] = ( ( ( x + y ) % 8 ) < 4 ) ? 0x000000 : 0xFFFF00;
				}
			}

			//Create GL resource
			GL.GenTextures( 1, ref this.warningTextureID );
			GL.BindTexture( OpenTK.Graphics.ES20.All.Texture2D, this.warningTextureID );
			GL.TexImage2D( OpenTK.Graphics.ES20.All.Texture2D, 0, (int) GLenum.Rgb, width, height, 0, GLenum.Rgb, GLenum.UnsignedShort565, data );

			data = null;
		}

		public int WarningTextureID
		{
			get { return this.warningTextureID; }
		}
	}
}
