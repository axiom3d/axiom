#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2010 Axiom Project Team
This file is part of Axiom.RenderSystems.OpenGLES
C# version developed by bostich.

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
//     <license see="http://axiomengine.sf.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations
using System;
using Axiom.Core;
using Axiom.Collections;
using Axiom.Graphics;
using Axiom.Media;
using ResourceHandle = System.Int64;
using OpenGL = OpenTK.Graphics.ES11.GL;
using OpenTK.Graphics.ES11;
#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGLES
{
	/// <summary>
	/// GL ES-specific implementation of a TextureManager
	/// </summary>
	public class GLESTextureManager : TextureManager
	{
		protected GLESSupport _glSupport;

		protected int _warningTextureID;
		/// <summary>
		/// 
		/// </summary>
		public int WarningTextureID
		{
			get
			{
				return _warningTextureID;
			}
			protected set
			{
				_warningTextureID = value;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="support"></param>
		public GLESTextureManager( GLESSupport support )
		{
			_glSupport = support;
			WarningTextureID = 0;
			GLESConfig.GlCheckError( this );
			// Register with group manager
			ResourceGroupManager.Instance.RegisterResourceManager( base.ResourceType, this );

			CreateWarningTexture();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="disposeManagedResources"></param>
		protected override void dispose( bool disposeManagedResources )
		{
			// Unregister with group manager
			ResourceGroupManager.Instance.UnregisterResourceManager( base.ResourceType );
			// Delete warning texture
			OpenGL.DeleteTextures( 1, ref _warningTextureID );
			GLESConfig.GlCheckError( this );

			base.dispose( disposeManagedResources );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="ttype"></param>
		/// <param name="format"></param>
		/// <param name="usage"></param>
		/// <returns></returns>
		public override Media.PixelFormat GetNativeFormat( TextureType ttype, Media.PixelFormat format, TextureUsage usage )
		{
			// Adjust requested parameters to capabilities
			RenderSystemCapabilities caps = Root.Instance.RenderSystem.HardwareCapabilities;
#warning check TextureCompressionVTC == RSC_TEXTURE_COMPRESSION_PVRTC
			// Check compressed texture support
			// if a compressed format not supported, revert to A8R8G8B8
			if ( PixelUtil.IsCompressed( format ) &&
				!caps.HasCapability( Capabilities.TextureCompressionDXT ) && !caps.HasCapability( Capabilities.TextureCompressionVTC ) )
			{
				return Media.PixelFormat.A8R8G8B8;
			}
			// if floating point textures not supported, revert to A8R8G8B8
			if ( PixelUtil.IsFloatingPoint( format ) &&
				!caps.HasCapability( Capabilities.TextureFloat ) )
			{
				return Media.PixelFormat.A8R8G8B8;
			}

			// Check if this is a valid rendertarget format
			if ( ( usage & TextureUsage.RenderTarget ) != 0 )
			{
				/// Get closest supported alternative
				/// If format is supported it's returned
				return GLESRTTManager.Instance.GetSupportedAlternative( format );
			}

			// Supported
			return format;
		}

		/// <summary>
		/// Returns whether this render system has hardware filtering supported for the
		/// texture format requested with the given usage options.
		/// </summary>
		/// <param name="ttype">The texture type requested</param>
		/// <param name="format">The pixel format requested</param>
		/// <param name="usage">the kind of usage this texture is intended for, a combination of the TextureUsage flags.</param>
		/// <param name="preciseFormatOnly">
		/// Whether precise or fallback format mode is used to detecting.
		/// In case the pixel format doesn't supported by device, false will be returned
		/// if in precise mode, and natively used pixel format will be actually use to
		/// check if in fallback mode.
		/// </param>
		/// <returns>true if the texture filtering is supported.</returns>
		public override bool IsHardwareFilteringSupported( TextureType ttype, Media.PixelFormat format, int usage, bool preciseFormatOnly )
		{
			if ( format == Media.PixelFormat.Unknown )
				return false;

			// Check native format
			Media.PixelFormat nativeFormat = GetNativeFormat( ttype, format, (TextureUsage)usage );
			if ( preciseFormatOnly && format != nativeFormat )
				return false;

			// Assume non-floating point is supported always
			if ( !PixelUtil.IsFloatingPoint( nativeFormat ) )
				return true;

			return false;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <param name="handle"></param>
		/// <param name="group"></param>
		/// <param name="isManual"></param>
		/// <param name="loader"></param>
		/// <param name="createParams"></param>
		/// <returns></returns>
		protected override Resource _create( string name, ulong handle, string group, bool isManual, IManualResourceLoader loader, NameValuePairList createParams )
		{
			return new GLESTexture( this, name, handle, group, isManual, loader, _glSupport );
		}

		/// <summary>
		/// Internal method to create a warning texture (bound when a texture unit is blank)
		/// </summary>
		protected void CreateWarningTexture()
		{
			// Generate warning texture
			int width = 8;
			int height = 8;
			// TODO convert to 5_6_5
			unsafe
			{

				int* data = stackalloc int[ width * height ];// 0xXXRRGGBB

				//yellow / black stripes
				for ( int y = 0; y < height; ++y )
				{
					for ( int x = 0; x < width; ++x )
					{
						data[ y * width + x ] = ( ( ( x + y ) % 8 ) < 4 ) ? 0x000000 : 0xFFFF00;
					}
				}

				// Create GL resource
				OpenGL.GenTextures( 1, ref _warningTextureID );
				GLESConfig.GlCheckError( this );
				OpenGL.BindTexture( All.Texture2D, _warningTextureID );
				GLESConfig.GlCheckError( this );
				OpenGL.TexImage2D( All.Texture2D, 0, (int)All.Rgb, width, height, 0, All.Rgb, All.UnsignedByte, (IntPtr)data );
				GLESConfig.GlCheckError( this );
			}
		}
	}
}

