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
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <id value="$Id"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using System.Text;

using Tao.OpenGl;

using Axiom.Media;
using Axiom.Graphics;
using Axiom.Core;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGL
{
	internal static class GLPixelUtil
	{
		/// <summary>
		/// Takes the Axiom pixel format and returns the appropriate GL one
		/// </summary>
		/// <param name="format">Axiom PixelFormat</param>
		/// <returns>
		/// a GLenum describing the format, or 0 if there is no exactly matching
		/// one (and conversion is needed)
		/// </returns>
		public static int GetGLOriginFormat( PixelFormat format )
		{
			switch( format )
			{
				case PixelFormat.A8:
					return Gl.GL_ALPHA;
				case PixelFormat.L8:
					return Gl.GL_LUMINANCE;
				case PixelFormat.L16:
					return Gl.GL_LUMINANCE;
				case PixelFormat.BYTE_LA:
					return Gl.GL_LUMINANCE_ALPHA;
				case PixelFormat.R3G3B2:
					return Gl.GL_RGB;
				case PixelFormat.A1R5G5B5:
					return Gl.GL_BGRA;
				case PixelFormat.R5G6B5:
					return Gl.GL_RGB;
				case PixelFormat.B5G6R5:
					return Gl.GL_BGR;
				case PixelFormat.A4R4G4B4:
					return Gl.GL_BGRA;
				case PixelFormat.R8G8B8:
					return Gl.GL_BGR;
				case PixelFormat.B8G8R8:
					return Gl.GL_RGB;
				case PixelFormat.X8R8G8B8:
				case PixelFormat.A8R8G8B8:
					return Gl.GL_BGRA;
				case PixelFormat.X8B8G8R8:
				case PixelFormat.A8B8G8R8:
					return Gl.GL_RGBA;
				case PixelFormat.B8G8R8A8:
					return Gl.GL_BGRA;
				case PixelFormat.R8G8B8A8:
					return Gl.GL_RGBA;
				case PixelFormat.A2R10G10B10:
					return Gl.GL_BGRA;
				case PixelFormat.A2B10G10R10:
					return Gl.GL_RGBA;
				case PixelFormat.FLOAT16_R:
					return Gl.GL_LUMINANCE;
				case PixelFormat.FLOAT16_GR:
					return Gl.GL_LUMINANCE_ALPHA;
				case PixelFormat.FLOAT16_RGB:
					return Gl.GL_RGB;
				case PixelFormat.FLOAT16_RGBA:
					return Gl.GL_RGBA;
				case PixelFormat.FLOAT32_R:
					return Gl.GL_LUMINANCE;
				case PixelFormat.FLOAT32_GR:
					return Gl.GL_LUMINANCE_ALPHA;
				case PixelFormat.FLOAT32_RGB:
					return Gl.GL_RGB;
				case PixelFormat.FLOAT32_RGBA:
					return Gl.GL_RGBA;
				case PixelFormat.SHORT_RGBA:
					return Gl.GL_RGBA;
				case PixelFormat.SHORT_RGB:
					return Gl.GL_RGB;
				case PixelFormat.SHORT_GR:
					return Gl.GL_LUMINANCE_ALPHA;
				case PixelFormat.DXT1:
					return Gl.GL_COMPRESSED_RGBA_S3TC_DXT1_EXT;
				case PixelFormat.DXT3:
					return Gl.GL_COMPRESSED_RGBA_S3TC_DXT3_EXT;
				case PixelFormat.DXT5:
					return Gl.GL_COMPRESSED_RGBA_S3TC_DXT5_EXT;
			}

			return 0;
		}

		/// <summary>
		/// Takes the Axiom pixel format and returns type that must be provided
		/// to GL as data type for reading it into the GPU
		/// </summary>
		/// <param name="format"></param>
		/// <returns>
		/// a GLenum describing the data type, or 0 if there is no exactly matching
		/// one (and conversion is needed)
		/// </returns>
		public static int GetGLOriginDataType( PixelFormat format )
		{
			switch( format )
			{
				case PixelFormat.A8:
				case PixelFormat.L8:
				case PixelFormat.R8G8B8:
				case PixelFormat.B8G8R8:
				case PixelFormat.BYTE_LA:
					return Gl.GL_UNSIGNED_BYTE;
				case PixelFormat.R3G3B2:
					return Gl.GL_UNSIGNED_BYTE_3_3_2;
				case PixelFormat.A1R5G5B5:
					return Gl.GL_UNSIGNED_SHORT_1_5_5_5_REV;
				case PixelFormat.R5G6B5:
				case PixelFormat.B5G6R5:
					return Gl.GL_UNSIGNED_SHORT_5_6_5;
				case PixelFormat.A4R4G4B4:
					return Gl.GL_UNSIGNED_SHORT_4_4_4_4_REV;
				case PixelFormat.L16:
					return Gl.GL_UNSIGNED_SHORT;
#if !LITTLE_ENDIAN
				case PixelFormat.X8B8G8R8:
				case PixelFormat.A8B8G8R8:
					return Gl.GL_UNSIGNED_INT_8_8_8_8_REV;
				case PixelFormat.X8R8G8B8:
				case PixelFormat.A8R8G8B8:
					return Gl.GL_UNSIGNED_INT_8_8_8_8_REV;
				case PixelFormat.B8G8R8A8:
					return Gl.GL_UNSIGNED_BYTE;
				case PixelFormat.R8G8B8A8:
					return Gl.GL_UNSIGNED_BYTE;
#else
			case PixelFormat.X8B8G8R8:
			case PixelFormat.A8B8G8R8:
                return Gl.GL_UNSIGNED_BYTE;
			case PixelFormat.X8R8G8B8:
            case PixelFormat.A8R8G8B8:
				return Gl.GL_UNSIGNED_BYTE;
            case PixelFormat.B8G8R8A8:
                return Gl.GL_UNSIGNED_INT_8_8_8_8;
			case PixelFormat.R8G8B8A8:
				return Gl.GL_UNSIGNED_INT_8_8_8_8;
#endif
				case PixelFormat.A2R10G10B10:
					return Gl.GL_UNSIGNED_INT_2_10_10_10_REV;
				case PixelFormat.A2B10G10R10:
					return Gl.GL_UNSIGNED_INT_2_10_10_10_REV;
				case PixelFormat.FLOAT16_R:
					//case PixelFormat.FLOAT16_GR:
				case PixelFormat.FLOAT16_RGB:
				case PixelFormat.FLOAT16_RGBA:
					return Gl.GL_HALF_FLOAT_ARB;
				case PixelFormat.FLOAT32_R:
					//case PixelFormat.FLOAT32_GR:
				case PixelFormat.FLOAT32_RGB:
				case PixelFormat.FLOAT32_RGBA:
					return Gl.GL_FLOAT;
				case PixelFormat.SHORT_RGBA:
					//case PixelFormat.SHORT_RGB:
					//case PixelFormat.SHORT_GR:
					return Gl.GL_UNSIGNED_SHORT;
				default:
					return 0;
			}
		}

		/// <summary>
		/// Takes the Axiom pixel format and returns the type that must be provided
		/// to GL as internal format. GL_NONE if no match exists.
		/// </summary>
		/// <param name="format"></param>
		/// <returns></returns>
		public static int GetGLInternalFormat( PixelFormat format )
		{
			switch( format )
			{
				case PixelFormat.L8:
					return Gl.GL_LUMINANCE8;
				case PixelFormat.L16:
					return Gl.GL_LUMINANCE16;
				case PixelFormat.A8:
					return Gl.GL_ALPHA8;
				case PixelFormat.A4L4:
					return Gl.GL_LUMINANCE4_ALPHA4;
				case PixelFormat.BYTE_LA:
					return Gl.GL_LUMINANCE8_ALPHA8;
				case PixelFormat.R3G3B2:
					return Gl.GL_R3_G3_B2;
				case PixelFormat.A1R5G5B5:
					return Gl.GL_RGB5_A1;
				case PixelFormat.R5G6B5:
				case PixelFormat.B5G6R5:
					return Gl.GL_RGB5;
				case PixelFormat.A4R4G4B4:
					return Gl.GL_RGBA4;
				case PixelFormat.R8G8B8:
				case PixelFormat.B8G8R8:
				case PixelFormat.X8B8G8R8:
				case PixelFormat.X8R8G8B8:
					return Gl.GL_RGB8;
				case PixelFormat.A8R8G8B8:
				case PixelFormat.B8G8R8A8:
					return Gl.GL_RGBA8;
				case PixelFormat.A2R10G10B10:
				case PixelFormat.A2B10G10R10:
					return Gl.GL_RGB10_A2;
				case PixelFormat.FLOAT16_R:
					return Gl.GL_LUMINANCE16F_ARB;
				case PixelFormat.FLOAT16_RGB:
					return Gl.GL_RGB16F_ARB;
				case PixelFormat.FLOAT16_GR:
					return Gl.GL_LUMINANCE_ALPHA16F_ARB;
				case PixelFormat.FLOAT16_RGBA:
					return Gl.GL_RGBA16F_ARB;
				case PixelFormat.FLOAT32_R:
					return Gl.GL_LUMINANCE32F_ARB;
				case PixelFormat.FLOAT32_GR:
					return Gl.GL_LUMINANCE_ALPHA32F_ARB;
				case PixelFormat.FLOAT32_RGB:
					return Gl.GL_RGB32F_ARB;
				case PixelFormat.FLOAT32_RGBA:
					return Gl.GL_RGBA32F_ARB;
				case PixelFormat.SHORT_RGBA:
					return Gl.GL_RGBA16;
				case PixelFormat.SHORT_RGB:
					return Gl.GL_RGB16;
				case PixelFormat.SHORT_GR:
					return Gl.GL_LUMINANCE16_ALPHA16;
				case PixelFormat.DXT1:
					return Gl.GL_COMPRESSED_RGBA_S3TC_DXT1_EXT;
				case PixelFormat.DXT3:
					return Gl.GL_COMPRESSED_RGBA_S3TC_DXT3_EXT;
				case PixelFormat.DXT5:
					return Gl.GL_COMPRESSED_RGBA_S3TC_DXT5_EXT;
				default:
					return Gl.GL_NONE;
			}
		}

		/// <summary>
		/// Takes the Axiom pixel format and returns the type that must be provided
		/// to GL as internal format. If no match exists, returns the closest match.
		/// </summary>
		/// <param name="format"></param>
		/// <returns></returns>
		public static int GetClosestGLInternalFormat( PixelFormat format )
		{
			int glFormat = GetGLInternalFormat( format );
			if( glFormat == Gl.GL_NONE )
			{
				return Gl.GL_RGBA8;
			}
			else
			{
				return glFormat;
			}
		}

		/// <summary>
		/// Function to get the closest matching OGRE format to an internal GL format. To be
		/// precise, the format will be chosen that is most efficient to transfer to the card
		/// without losing precision.
		/// </summary>
		/// <remarks>
		/// It is valid for this function to always return PixelFormat.A8R8G8B8.
		/// </remarks>
		/// <param name="format"></param>
		/// <returns></returns>
		public static PixelFormat GetClosestPixelFormat( int format )
		{
			switch( format )
			{
				case Gl.GL_LUMINANCE8:
					return PixelFormat.L8;
				case Gl.GL_LUMINANCE16:
					return PixelFormat.L16;
				case Gl.GL_ALPHA8:
					return PixelFormat.A8;
					//case Gl.GL_LUMINANCE4_ALPHA4:
					//    // Unsupported by GL as input format, use the byte packed format
					//    return PixelFormat.BYTE_LA;
					//case Gl.GL_LUMINANCE8_ALPHA8:
					//    return PixelFormat.BYTE_LA;
				case Gl.GL_R3_G3_B2:
					return PixelFormat.R3G3B2;
				case Gl.GL_RGB5_A1:
					return PixelFormat.A1R5G5B5;
				case Gl.GL_RGB5:
					return PixelFormat.R5G6B5;
				case Gl.GL_RGBA4:
					return PixelFormat.A4R4G4B4;
				case Gl.GL_RGB8:
					return PixelFormat.X8R8G8B8;
				case Gl.GL_RGBA8:
					return PixelFormat.A8R8G8B8;
				case Gl.GL_RGB10_A2:
					return PixelFormat.A2R10G10B10;
				case Gl.GL_RGBA16:
					return PixelFormat.SHORT_RGBA;
					//case Gl.GL_RGB16:
					//    return PixelFormat.SHORT_RGB;
					//case Gl.GL_LUMINANCE16_ALPHA16:
					//    return PixelFormat.SHORT_GR;
				case Gl.GL_LUMINANCE_FLOAT16_ATI:
					return PixelFormat.FLOAT16_R;
					//case Gl.GL_LUMINANCE_ALPHA_FLOAT16_ATI:
					//    return PixelFormat.FLOAT16_GR;
					//case Gl.GL_LUMINANCE_ALPHA_FLOAT32_ATI:
					//    return PixelFormat.FLOAT32_GR;
				case Gl.GL_LUMINANCE_FLOAT32_ATI:
					return PixelFormat.FLOAT32_R;
				case Gl.GL_RGB_FLOAT16_ATI: // Gl.GL_RGB16F_ARB
					return PixelFormat.FLOAT16_RGB;
				case Gl.GL_RGBA_FLOAT16_ATI:
					return PixelFormat.FLOAT16_RGBA;
				case Gl.GL_RGB_FLOAT32_ATI:
					return PixelFormat.FLOAT32_RGB;
				case Gl.GL_RGBA_FLOAT32_ATI:
					return PixelFormat.FLOAT32_RGBA;
				case Gl.GL_COMPRESSED_RGB_S3TC_DXT1_EXT:
				case Gl.GL_COMPRESSED_RGBA_S3TC_DXT1_EXT:
					return PixelFormat.DXT1;
				case Gl.GL_COMPRESSED_RGBA_S3TC_DXT3_EXT:
					return PixelFormat.DXT3;
				case Gl.GL_COMPRESSED_RGBA_S3TC_DXT5_EXT:
					return PixelFormat.DXT5;
				default:
					return PixelFormat.A8R8G8B8;
			}
		}

		/// <summary>
		/// Returns the maximum number of Mipmaps that can be generated until we reach
		/// the mininum format possible. This does not count the base level.
		/// </summary>
		/// <param name="width">The width of the area</param>
		/// <param name="height">The height of the area</param>
		/// <param name="depth">The depth of the area</param>
		/// <param name="format">The format of the area</param>
		/// <returns></returns>
		/// <remarks>
		/// In case that the format is non-compressed, this simply returns
		/// how many times we can divide this texture in 2 until we reach 1x1.
		/// For compressed formats, constraints apply on minimum size and alignment
		/// so this might differ.
		/// </remarks>
		public static int GetMaxMipmaps( int width, int height, int depth, PixelFormat format )
		{
			int count = 0;
			do
			{
				if( width > 1 )
				{
					width = width / 2;
				}
				if( height > 1 )
				{
					height = height / 2;
				}
				if( depth > 1 )
				{
					depth = depth / 2;
				}
				count++;
			}
			while( !( width == 1 && height == 1 && depth == 1 ) );

			return count;
		}

		/// <summary>
		/// Returns next power-of-two size if required by render system, in case
		/// RSC_NON_POWER_OF_2_TEXTURES is supported it returns value as-is.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static int OptionalPO2( int value )
		{
			RenderSystemCapabilities caps = Root.Instance.RenderSystem.HardwareCapabilities;
			if( caps.HasCapability( Capabilities.NonPowerOf2Textures ) )
			{
				return value;
			}
			else
			{
				return (int)Bitwise.FirstPO2From( (uint)value );
			}
		}
	}
}
