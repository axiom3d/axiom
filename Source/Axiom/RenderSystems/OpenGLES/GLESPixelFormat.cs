#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2010 Axiom Project Team
This file is part of Axiom.RenderSystems.OpenGLES
C# version developed by bostich.

The overall design, and a majority of the core engine and rendering code
contained within this library is a derivative of the open source Object Oriented
Graphics Engine Axiom, which can be found at http://Axiom.sourceforge.net.
Many thanks to the Axiom team for maintaining such a high quality project.

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
using System.Collections.Generic;
using GLES = OpenTK.Graphics.ES11;
using Axiom.Media;
using Axiom.Core;
using Axiom.Graphics;
#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGLES
{
	public class GLESPixelUtil
	{
		/// <summary>
		/// Takes the Axiom pixel format and returns the appropriate GL one
		/// </summary>
		/// <param name="fm"></param>
		/// <returns>a GLenum describing the format, or 0 if there is no exactly matching 
		/// one (and conversion is needed)</returns>
		public static GLES.All GetGLOriginFormat( PixelFormat fmt )
		{
			switch ( fmt )
			{
				case PixelFormat.A8:
					return GLES.All.Alpha;

				case PixelFormat.L8:
				case PixelFormat.L16:
				case PixelFormat.FLOAT16_R:
				case PixelFormat.FLOAT32_R:
					return GLES.All.Luminance;

				case PixelFormat.BYTE_LA:
				case PixelFormat.SHORT_GR:
				case PixelFormat.FLOAT16_GR:
				case PixelFormat.FLOAT32_GR:
					return GLES.All.LuminanceAlpha;

				// PVRTC compressed formats
#if GL_IMG_texture_compression_pvrtc
			case PixelFormat.PVRTC_RGB2:
				return GLES.All.COMPRESSED_RGB_PVRTC_2BPPV1_IMG;
			case PixelFormat.PVRTC_RGB4:
				return GLES.All.COMPRESSED_RGB_PVRTC_4BPPV1_IMG;
			case PixelFormat.PVRTC_RGBA2:
				return GLES.All.COMPRESSED_RGBA_PVRTC_2BPPV1_IMG;
			case PixelFormat.PVRTC_RGBA4:
				return GLES.All.COMPRESSED_RGBA_PVRTC_4BPPV1_IMG;
#endif
				case PixelFormat.R3G3B2:
				case PixelFormat.R5G6B5:
				case PixelFormat.FLOAT16_RGB:
				case PixelFormat.FLOAT32_RGB:
				case PixelFormat.SHORT_RGB:
					return GLES.All.Rgb;

				case PixelFormat.X8R8G8B8:
				case PixelFormat.A8R8G8B8:
				case PixelFormat.B8G8R8A8:
				case PixelFormat.A1R5G5B5:
				case PixelFormat.A4R4G4B4:
				case PixelFormat.A2R10G10B10:
				// This case in incorrect, swaps R & B channels
				//return GLES.All.BGRA;

				case PixelFormat.X8B8G8R8:
				case PixelFormat.A8B8G8R8:
				case PixelFormat.A2B10G10R10:
				case PixelFormat.FLOAT16_RGBA:
				case PixelFormat.FLOAT32_RGBA:
				case PixelFormat.SHORT_RGBA:
					return GLES.All.Rgba;

#if AXIOM_ENDIAN_BIG
				// Formats are in native endian, so R8G8B8 on little endian is
				// BGR, on big endian it is RGB.
				case PixelFormat.R8G8B8:
					return GLES.All.Rgb;
				case PixelFormat.B8G8R8:
					return 0;
#else
				case PixelFormat.R8G8B8:
					return 0;
				case PixelFormat.B8G8R8:
					return GLES.All.Rgb;
#endif
				case PixelFormat.DXT1:
				case PixelFormat.DXT3:
				case PixelFormat.DXT5:
				case PixelFormat.B5G6R5:
				default:
					return 0;
			}
		}

		/// <summary>
		/// Takes the Axiom pixel format and returns the type that must be provided
		/// to GL as internal format. 0 if no match exists.
		/// </summary>
		/// <param name="fmt"></param>
		/// <param name="hwGamma"></param>
		/// <returns></returns>
		public static GLES.All GetGLInternalFormat( PixelFormat fmt, bool hwGamma )
		{
			switch ( fmt )
			{
				case PixelFormat.L8:
					return GLES.All.Luminance;
				case PixelFormat.A8:
					return GLES.All.Alpha;
				case PixelFormat.BYTE_LA:
					return GLES.All.LuminanceAlpha;
				case PixelFormat.R8G8B8:
				case PixelFormat.B8G8R8:
				case PixelFormat.X8B8G8R8:
				case PixelFormat.X8R8G8B8:
				case PixelFormat.A8R8G8B8:
				case PixelFormat.A8B8G8R8:
				case PixelFormat.B8G8R8A8:
					{
						if ( !hwGamma )
						{
							return GLES.All.Rgba;
						}
					}
					return 0;
				case PixelFormat.A4L4:
				case PixelFormat.L16:
				case PixelFormat.A4R4G4B4:
				case PixelFormat.R3G3B2:
				case PixelFormat.A1R5G5B5:
				case PixelFormat.R5G6B5:
				case PixelFormat.B5G6R5:
				case PixelFormat.A2R10G10B10:
				case PixelFormat.A2B10G10R10:
				case PixelFormat.FLOAT16_R:
				case PixelFormat.FLOAT16_RGB:
				case PixelFormat.FLOAT16_GR:
				case PixelFormat.FLOAT16_RGBA:
				case PixelFormat.FLOAT32_R:
				case PixelFormat.FLOAT32_GR:
				case PixelFormat.FLOAT32_RGB:
				case PixelFormat.FLOAT32_RGBA:
				case PixelFormat.SHORT_RGBA:
				case PixelFormat.SHORT_RGB:
				case PixelFormat.SHORT_GR:
				case PixelFormat.DXT1:
				case PixelFormat.DXT3:
				case PixelFormat.DXT5:
				default:
					return 0;
			}
		}

		/// <summary>
		/// Takes the Axiom pixel format and returns type that must be provided
		/// to GL as data type for reading it into the GPU
		/// </summary>
		/// <param name="mFormat"></param>
		/// <returns>returns a GLenum describing the data type, or 0 if there is no exactly matching 
		/// one (and conversion is needed)</returns>
		public static GLES.All GetGLOriginDataType( PixelFormat mFormat )
		{
			switch ( mFormat )
			{
				case PixelFormat.A8:
				case PixelFormat.L8:
				case PixelFormat.R8G8B8:
				case PixelFormat.B8G8R8:
				case PixelFormat.BYTE_LA:
					return GLES.All.UnsignedByte;
				case PixelFormat.R5G6B5:
				case PixelFormat.B5G6R5:
					return GLES.All.UnsignedShort565;
				case PixelFormat.L16:
					return GLES.All.UnsignedShort;

#if AXIOM_ENDIAN_BIG
				case PixelFormat.X8B8G8R8:
				case PixelFormat.A8B8G8R8:
#warning UnsignetInt8888Rev is missing
					return GLES.All.UnsignedInt248Oes;//GL_UNSIGNED_INT_8_8_8_8_REV;
				case PixelFormat.X8R8G8B8:
				case PixelFormat.A8R8G8B8:
#warning UnsignetInt8888Rev is missing
					return GLES.All.UnsignedInt248Oes;//GL_UNSIGNED_INT_8_8_8_8_REV;
				case PixelFormat.B8G8R8A8:
					return GLES.All.UnsignedByte;//GL_UNSIGNED_BYTE;
				case PixelFormat.R8G8B8A8:
					return GLES.All.UnsignedByte;//GL_UNSIGNED_BYTE;
#else
				case PixelFormat.X8B8G8R8:
				case PixelFormat.A8B8G8R8:
				case PixelFormat.X8R8G8B8:
				case PixelFormat.A8R8G8B8:
					return GLES.All.UnsignedByte;//GL_UNSIGNED_BYTE;
				case PixelFormat.B8G8R8A8:
				case PixelFormat.R8G8B8A8:
					return 0;
#endif

				case PixelFormat.FLOAT32_R:
				case PixelFormat.FLOAT32_GR:
				case PixelFormat.FLOAT32_RGB:
				case PixelFormat.FLOAT32_RGBA:
					return GLES.All.Flat;//GL_FLOAT;
				case PixelFormat.SHORT_RGBA:
				case PixelFormat.SHORT_RGB:
				case PixelFormat.SHORT_GR:
					return GLES.All.Float;//GL_UNSIGNED_SHORT;

				case PixelFormat.A2R10G10B10:
				case PixelFormat.A2B10G10R10:
				case PixelFormat.FLOAT16_R:
				case PixelFormat.FLOAT16_GR:
				case PixelFormat.FLOAT16_RGB:
				case PixelFormat.FLOAT16_RGBA:
				case PixelFormat.R3G3B2:
				case PixelFormat.A1R5G5B5:
				case PixelFormat.A4R4G4B4:
				// TODO not supported
				default:
					return 0;
			}
		}

		/// <summary>
		/// Takes the Axiom pixel format and returns the type that must be provided
		/// to GL as internal format. If no match exists, returns the closest match.
		/// </summary>
		/// <param name="mFormat">mFormat The pixel format</param>
		/// <param name="hwGamma">hwGamma Whether a hardware gamma-corrected version is requested</param>
		/// <returns></returns>
		public static GLES.All GetClosestGLInternalFormat( PixelFormat mFormat, bool hwGamma = false )
		{
			GLES.All format = GetGLInternalFormat( mFormat, hwGamma );
			if ( format == 0 )
			{
				if ( hwGamma )
				{
					// TODO not supported
					return 0;
				}
				else
				{
					return GLES.All.Rgba;
				}
			}
			else
			{
				return format;
			}
		}

		/// <summary>
		///  Function to get the closest matching Axiom format to an internal GL format. To be
		/// precise, the format will be chosen that is most efficient to transfer to the card 
		/// without losing precision.
		/// <remarks>It is valid for this function to always return PixelFormat.A8R8G8B8.</remarks>
		/// </summary>
		/// <param name="fmt"></param>
		/// <returns></returns>
		public static PixelFormat GetClosestAxiomFormat( GLES.All fmt )
		{
			switch ( fmt )
			{
#if GL_IMG_texture_compression_pvrtc
			case GL_COMPRESSED_RGB_PVRTC_2BPPV1_IMG:
				return PixelFormat.PVRTC_RGB2;
			case GL_COMPRESSED_RGBA_PVRTC_2BPPV1_IMG:
				return PixelFormat.PVRTC_RGBA2;
			case GL_COMPRESSED_RGB_PVRTC_4BPPV1_IMG:
				return PixelFormat.PVRTC_RGB4;
			case GL_COMPRESSED_RGBA_PVRTC_4BPPV1_IMG:
				return PixelFormat.PVRTC_RGBA4;
#endif
				case GLES.All.Luminance:
					return PixelFormat.L8;
				case GLES.All.Alpha:
					return PixelFormat.A8;
				case GLES.All.LuminanceAlpha:
					return PixelFormat.BYTE_LA;

				case GLES.All.Rgb:
					return PixelFormat.A8R8G8B8;
				case GLES.All.Rgba:
#if (AXIOM_PLATFORM_IPHONE)
				// seems that in iPhone we need this value to get the right color
				return PixelFormat.A8R8G8B8;
#else
					return PixelFormat.X8B8G8R8;
#endif
#if GL_BGRA
			case GLES.All.Rgba:
#endif
				//                return PixelFormat.X8B8G8R8;
				default:
					//TODO: not supported
					return PixelFormat.A8R8G8B8;
			};
		}

		/// <summary>
		/// Returns the maximum number of Mipmaps that can be generated until we reach
		/// the mininum format possible. This does not count the base level.
		/// </summary>
		/// <remarks>
		/// In case that the format is non-compressed, this simply returns
		/// how many times we can divide this texture in 2 until we reach 1x1.
		/// For compressed formats, constraints apply on minimum size and alignment
		///  so this might differ.
		/// </remarks>
		/// <param name="width">The width of the area</param>
		/// <param name="height">The height of the area</param>
		/// <param name="depth">The depth of the area</param>
		/// <param name="format">The format of the area</param>
		/// <returns>the maximum number of Mipmaps that can be generated </returns>
		public static int GetMaxMipmaps( int width, int height, int depth, PixelFormat format )
		{
			int count = 0;

			do
			{
				if ( width > 1 )
				{
					width = width / 2;
				}
				if ( height > 1 )
				{
					height = height / 2;
				}
				if ( depth > 1 )
				{
					depth = depth / 2;
				}
				/*
				NOT needed, compressed formats will have mipmaps up to 1x1
				if(PixelUtil::isValidExtent(width, height, depth, format))
					count ++;
				else
					break;
				*/
				count++;
			} while ( !( width == 1 && height == 1 && depth == 1 ) );

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
			RenderSystemCapabilities caps =
			Root.Instance.RenderSystem.HardwareCapabilities;
			if ( caps.HasCapability( Capabilities.NonPowerOf2Textures ) )
			{
				return value;
			}
			else
			{
				uint n = (uint)value;
				--n;
				n |= n >> 16;
				n |= n >> 8;
				n |= n >> 4;
				n |= n >> 2;
				n |= n >> 1;
				++n;
				return (int)n;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="data"></param>
		/// <param name="outputFormat"></param>
		/// <returns></returns>
		public static PixelBox ConvertToGLformat( PixelBox data, out GLES.All outputFormat )
		{
			GLES.All glFormat = GetGLOriginFormat( data.Format );
			outputFormat = glFormat;
			if ( glFormat != 0 )
			{
				// format already supported
				return data;
			}

			PixelBox converted = null;

			if ( data.Format == PixelFormat.R8G8B8 )
			{
				converted = new PixelBox();
				// Convert BGR -> RGB
				converted.Format = PixelFormat.R8G8B8;
				outputFormat = GLES.All.Rgb;
				converted = new PixelBox( data.Width, data.Height, data.Depth, data.Format );
				converted.Data = data.Data;
				unsafe
				{
					uint* dataptr = (uint*)converted.Data;
					for ( uint i = 0; i < converted.Width * converted.Height; i++ )
					{
						uint* color = dataptr;
						*color = ( *color & 0x000000ff ) << 16 |
								 ( *color & 0x0000FF00 ) |
								 ( *color & 0x00FF0000 ) >> 16;
						dataptr += 1;
					}
				}
			}

			return converted;
		}
	}
}

