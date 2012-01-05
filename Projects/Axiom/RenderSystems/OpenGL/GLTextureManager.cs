#region LGPL License

/*
Axiom Graphics Engine Library
Copyright © 2003-2011 Axiom Project Team

The overall design, and a majority of the core engine and rendering code
contained within this library is a derivative of the open source Object Oriented
Graphics Engine OGRE, which can be found at http://ogre.sourceforge.net.
Many thanks to the OGRE team for maintaining such a high quality project.

This library is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 2.1 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public
License along with this library; if not, write to the Free Software
Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
*/

#endregion LGPL License

#region SVN Version Information

// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;

using Axiom.Core;
using Axiom.Graphics;
using Axiom.Media;

using Tao.OpenGl;

using System.Runtime.InteropServices;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGL
{
	/// <summary>
	/// Summary description for GLTextureManager.
	/// </summary>
	public class GLTextureManager : TextureManager
	{
		private BaseGLSupport _glSupport;
		private int _warningTextureId;
		public int WarningTextureId { get { return _warningTextureId; } }

		internal GLTextureManager( BaseGLSupport glSupport )
			: base()
		{
			_glSupport = glSupport;
			Is32Bit = true;

			ResourceGroupManager.Instance.RegisterResourceManager( ResourceType, this );
			_createWarningTexture();
		}

		protected override Resource _create( string name, ulong handle, string group, bool isManual, IManualResourceLoader loader, Axiom.Collections.NameValuePairList createParams )
		{
			return new GLTexture( this, name, handle, group, isManual, loader, _glSupport );
		}

		private void _createWarningTexture()
		{
			// Generate warning texture
			int width = 8;
			int height = 8;
			uint[] data = new uint[width * height]; // 0xXXRRGGBB
			// Yellow/black stripes
			for( int y = 0; y < height; ++y )
			{
				for( int x = 0; x < width; ++x )
				{
					data[ y * width + x ] = ( ( ( x + y ) % 8 ) < 4 ) ? (uint)0x000000 : (uint)0xFFFF00;
				}
			}

			// Create GL resource
			Gl.glGenTextures( 1, out _warningTextureId );
			Gl.glBindTexture( Gl.GL_TEXTURE_2D, _warningTextureId );
			Gl.glTexParameteri( Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MAX_LEVEL, 0 );
			Gl.glTexImage2D( Gl.GL_TEXTURE_2D, 0, Gl.GL_RGB8, width, height, 0, Gl.GL_BGRA, Gl.GL_UNSIGNED_INT_8_8_8_8_REV, data );
		}

		public override PixelFormat GetNativeFormat( TextureType ttype, PixelFormat format, TextureUsage usage )
		{
			// Adjust requested parameters to capabilities
			RenderSystemCapabilities caps = Root.Instance.RenderSystem.HardwareCapabilities;

			// Check compressed texture support
			// if a compressed format not supported, revert to PF_A8R8G8B8
			if( PixelUtil.IsCompressed( format ) &&
			    !caps.HasCapability( Capabilities.TextureCompressionDXT ) )
			{
				return PixelFormat.A8R8G8B8;
			}
			// if floating point textures not supported, revert to PF_A8R8G8B8
			if( PixelUtil.IsFloatingPoint( format ) &&
			    !caps.HasCapability( Capabilities.TextureFloat ) )
			{
				return PixelFormat.A8R8G8B8;
			}

			// Check if this is a valid rendertarget format
			if( ( usage & TextureUsage.RenderTarget ) != 0 )
			{
				/// Get closest supported alternative
				/// If mFormat is supported it's returned
				return GLRTTManager.Instance.GetSupportedAlternative( format );
			}

			// Supported
			return format;
		}

		private bool IsHardwareFilteringSupported( TextureType ttype, PixelFormat format, TextureUsage usage, bool preciseFormatOnly )
		{
			if( format == PixelFormat.Unknown )
			{
				return false;
			}

			// Check natively format
			PixelFormat nativeFormat = GetNativeFormat( ttype, format, usage );
			if( preciseFormatOnly && format != nativeFormat )
			{
				return false;
			}

			// Assume non-floating point is supported always
			if( !PixelUtil.IsFloatingPoint( nativeFormat ) )
			{
				return true;
			}

			// Hack: there are no elegant GL API to detects texture filtering supported,
			// just hard code for cards based on vendor specifications.

			// TODO: Add cards that 16 bits floating point flitering supported by
			// hardware below
			String[] sFloat16SupportedCards = {
			                                  	// GeForce 8 Series
			                                  	"*GeForce*8800*",
			                                  	// GeForce 7 Series
			                                  	"*GeForce*7950*",
			                                  	"*GeForce*7900*",
			                                  	"*GeForce*7800*",
			                                  	"*GeForce*7600*",
			                                  	"*GeForce*7500*",
			                                  	"*GeForce*7300*",
			                                  	// GeForce 6 Series
			                                  	"*GeForce*6800*",
			                                  	"*GeForce*6700*",
			                                  	"*GeForce*6600*",
			                                  	"*GeForce*6500*",
			                                  	"*GeForce*6200*",
			                                  	"" // Empty string means end of list
			                                  };

			// TODO: Add cards that 32 bits floating point flitering supported by
			// hardware below
			String[] sFloat32SupportedCards = {
			                                  	// GeForce 8 Series
			                                  	"*GeForce*8800*",
			                                  	"" // Empty string means end of list
			                                  };

			PixelComponentType pct = PixelUtil.GetComponentType( nativeFormat );
			String[] supportedCards;
			switch( pct )
			{
				case PixelComponentType.Float16:
					supportedCards = sFloat16SupportedCards;
					break;
				case PixelComponentType.Float32:
					supportedCards = sFloat32SupportedCards;
					break;
				default:
					return false;
			}
			String pcRenderer = Gl.glGetString( Gl.GL_RENDERER ); // TAO 2.0
			//String pcRenderer = Marshal.PtrToStringAnsi( Gl.glGetString( Gl.GL_RENDERER ) );

			foreach( String str in supportedCards )
			{
				if( str == pcRenderer )
				{
					return true;
				}
			}

			return false;
		}

		protected override void dispose( bool disposeManagedResources )
		{
			if( !IsDisposed )
			{
				if( disposeManagedResources )
				{
					foreach( Resource texture in Resources )
					{
						texture.Dispose();
					}

					ResourceGroupManager.Instance.UnregisterResourceManager( ResourceType );
				}

				// There are no unmanaged resources to release, but
				// if we add them, they need to be released here.
				try
				{
					Gl.glDeleteTextures( 1, ref _warningTextureId );
				}
				catch( AccessViolationException ave )
				{
					LogManager.Instance.Write( "Error Deleting Texture[{0}].", _warningTextureId );
				}
			}

			// If it is available, make the call to the
			// base class's Dispose(Boolean) method
			base.dispose( disposeManagedResources );
		}
	}
}
