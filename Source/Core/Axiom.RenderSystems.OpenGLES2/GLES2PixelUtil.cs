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

using Axiom.Media;

using GLenum = OpenTK.Graphics.ES20.All;

#endregion Namespace Declarations
			
namespace Axiom.RenderSystems.OpenGLES2
{
	/// <summary>
	///   Class to do pixel format mapping between GL and Axiom
	/// </summary>
	internal static class GLES2PixelUtil
	{
		/// <summary>
		///   Takes the Axiom pixel format and returns the appropriate GL one
		/// </summary>
		/// <param name="format"> </param>
		/// <returns> a GLenum describing the format, GLenum.Zero if there is no exactly matching one (and a conversion is need) </returns>
		public static GLenum GetGLOriginFormat( PixelFormat format )
		{
			switch ( format )
			{
				case PixelFormat.A8:
					return GLenum.Alpha;

				case PixelFormat.L8:
				case PixelFormat.L16:
					return GLenum.Luminance;

				case PixelFormat.FLOAT16_RGB:
					return GLenum.Rgb;
				case PixelFormat.FLOAT16_RGBA:
					return GLenum.Rgba;

				case PixelFormat.BYTE_LA:
				case PixelFormat.SHORT_GR:
					return GLenum.LuminanceAlpha;

					//PVRTC compressed formats
				case PixelFormat.PVRTC_RGB2:
					return GLenum.CompressedRgbPvrtc2Bppv1Img;
				case PixelFormat.PVRTC_RGB4:
					return GLenum.CompressedRgbPvrtc4Bppv1Img;
				case PixelFormat.PVRTC_RGBA2:
					return GLenum.CompressedRgbaPvrtc2Bppv1Img;
				case PixelFormat.PVRTC_RGBA4:
					return GLenum.CompressedRgbaPvrtc4Bppv1Img;

				case PixelFormat.R5G6B5:
				case PixelFormat.B5G6R5:
				case PixelFormat.R8G8B8:
				case PixelFormat.B8G8R8:
					return GLenum.Rgb;

				case PixelFormat.A1R5G5B5:
					return GLenum.Bgra;

				case PixelFormat.A4R4G4B4:
				case PixelFormat.X8R8G8B8:
				case PixelFormat.A8R8G8B8:
				case PixelFormat.B8G8R8A8:
				case PixelFormat.X8B8G8R8:
				case PixelFormat.A8B8G8R8:
					return GLenum.Rgba;

				case PixelFormat.FLOAT32_GR:
				case PixelFormat.FLOAT32_R:

				default:
					return GLenum.Zero;
			}
		}

		/// <summary>
		///   Takes the Axiom pixel format and returns type that must be provided to GL as data type for reading it into the GPU
		/// </summary>
		/// <param name="format"> </param>
		/// <returns> a GLenum describing the data type, or GLenum.Zero if there is no exactly matching one (and conversion is needed) </returns>
		public static GLenum GetGLOriginDataType( PixelFormat format )
		{
			switch ( format )
			{
				case PixelFormat.A8:
				case PixelFormat.L8:
				case PixelFormat.L16:
				case PixelFormat.R8G8B8:
				case PixelFormat.B8G8R8:
				case PixelFormat.BYTE_LA:
					return GLenum.UnsignedByte;
				case PixelFormat.R5G6B5:
				case PixelFormat.B5G6R5:
					return GLenum.UnsignedShort565;
				case PixelFormat.A4R4G4B4:
					return GLenum.UnsignedShort4444;
				case PixelFormat.A1R5G5B5:
					return GLenum.UnsignedShort5551;

				case PixelFormat.X8B8G8R8:
				case PixelFormat.A8B8G8R8:
				case PixelFormat.X8R8G8B8:
				case PixelFormat.A8R8G8B8:
				case PixelFormat.B8G8R8A8:
				case PixelFormat.R8G8B8A8:
					return GLenum.UnsignedByte;

				case PixelFormat.FLOAT16_R:
				case PixelFormat.FLOAT16_GR:
				case PixelFormat.FLOAT16_RGB:
				case PixelFormat.FLOAT16_RGBA:
					return GLenum.HalfFloatOes;

				case PixelFormat.FLOAT32_R:
				case PixelFormat.FLOAT32_GR:
				case PixelFormat.FLOAT32_RGB:
				case PixelFormat.FLOAT32_RGBA:
					return GLenum.Float;
				case PixelFormat.DXT1:
				case PixelFormat.DXT3:
				case PixelFormat.DXT5:
				case PixelFormat.R3G3B2:
				case PixelFormat.A2R10G10B10:
				case PixelFormat.A2B10G10R10:
				case PixelFormat.SHORT_RGBA:
				case PixelFormat.SHORT_RGB:
				case PixelFormat.SHORT_GR:
					//Ogre TODO not supported
				default:
					return GLenum.Zero;
			}
		}

		public static GLenum GetGLInternalFormat( PixelFormat format )
		{
			return GetGLInternalFormat( format, false );
		}

		/// <summary>
		///   Takes the Axiom pixel format format and returns the type that must be provided to GL as internal format. GLenum.None if no match exists
		/// </summary>
		/// <param name="format"> The pixel format </param>
		/// <param name="hwGamma"> Whether a hardware gamma-corrected version is requests </param>
		/// <returns> </returns>
		public static GLenum GetGLInternalFormat( PixelFormat format, bool hwGamma )
		{
			switch ( format )
			{
				case PixelFormat.L8:
				case PixelFormat.L16:
					return GLenum.Luminance;

				case PixelFormat.A8:
					return GLenum.Alpha;

				case PixelFormat.BYTE_LA:
					return GLenum.LuminanceAlpha;

				case PixelFormat.PVRTC_RGB2:
					return GLenum.CompressedRgbPvrtc2Bppv1Img;
				case PixelFormat.PVRTC_RGB4:
					return GLenum.CompressedRgbPvrtc4Bppv1Img;
				case PixelFormat.PVRTC_RGBA2:
					return GLenum.CompressedRgbaPvrtc2Bppv1Img;
				case PixelFormat.PVRTC_RGBA4:
					return GLenum.CompressedRgbaPvrtc4Bppv1Img;

				case PixelFormat.X8B8G8R8:
				case PixelFormat.X8R8G8B8:
				case PixelFormat.A8B8G8R8:
				case PixelFormat.A8R8G8B8:
				case PixelFormat.B8G8R8A8:
				case PixelFormat.A1R5G5B5:
				case PixelFormat.A4R4G4B4:
					return GLenum.Rgba;
				case PixelFormat.R5G6B5:
				case PixelFormat.B5G6R5:
				case PixelFormat.R8G8B8:
				case PixelFormat.B8G8R8:
					return GLenum.Rgb;

				default:
					return GLenum.None;
			}
		}

		public static GLenum GetClosestGLInternalFormat( PixelFormat format )
		{
			return GetClosestGLInternalFormat( format, false );
		}

		/// <summary>
		///   Takes the Axiom pixel format and returns the type that must be provided to GL as internal format. If no match exists, returns the cloest match.
		/// </summary>
		/// <param name="format"> The pixel format </param>
		/// <param name="hwGamma"> Whether a hardware gamma-corrected version is requested </param>
		/// <returns> </returns>
		public static GLenum GetClosestGLInternalFormat( PixelFormat format, bool hwGamma )
		{
			GLenum glformat = GetGLInternalFormat( format, hwGamma );
			if ( glformat == GLenum.None )
			{
				if ( hwGamma )
				{
					//Ogre TODO: not supported
					return GLenum.Zero;
				}
				else
				{
					return GLenum.Rgba;
				}
			}
			else
			{
				return glformat;
			}
		}

		/// <summary>
		///   Function to get the closest matching Axiom format to an internal GL format. To be precise, the format will be chosen that is most efficient to transfer to the card without losing precision
		/// </summary>
		/// <remarks>
		///   It is valid for this function to always return PixelFormat.A8R8G8B8
		/// </remarks>
		/// <param name="fmt"> </param>
		/// <param name="dataType"> </param>
		/// <returns> </returns>
		public static PixelFormat GetClosestAxiomFormat( GLenum fmt, GLenum dataType )
		{
			switch ( fmt )
			{
				case GLenum.CompressedRgbPvrtc2Bppv1Img:
					return PixelFormat.PVRTC_RGB2;
				case GLenum.CompressedRgbaPvrtc2Bppv1Img:
					return PixelFormat.PVRTC_RGBA2;
				case GLenum.CompressedRgbPvrtc4Bppv1Img:
					return PixelFormat.PVRTC_RGB4;
				case GLenum.CompressedRgbaPvrtc4Bppv1Img:
					return PixelFormat.PVRTC_RGBA4;

				case GLenum.Luminance:
					return PixelFormat.L8;
				case GLenum.Alpha:
					return PixelFormat.A8;
				case GLenum.LuminanceAlpha:
					return PixelFormat.BYTE_LA;
				case GLenum.Rgb:
					switch ( dataType )
					{
						case GLenum.UnsignedShort565:
							return PixelFormat.B5G6R5;
						default:
							return PixelFormat.R8G8B8;
					}
				case GLenum.Rgba:
					switch ( dataType )
					{
						case GLenum.UnsignedShort5551:
							return PixelFormat.A1R5G5B5;
						case GLenum.UnsignedShort4444:
							return PixelFormat.A4R4G4B4;
						default:
							return PixelFormat.A8R8G8B8;
					}
				case GLenum.Bgra:
					return PixelFormat.A8B8G8R8;

				default:
					//Ogre TODO: not supported
					return PixelFormat.A8R8G8B8;
			}
		}

		/// <summary>
		///   Returns the maximum number of Mipmaps that can be generated until we reach the minimum format possible. This does not count the base level
		/// </summary>
		/// <param name="width"> The width of the area </param>
		/// <param name="height"> The height of the area </param>
		/// <param name="depth"> The depth of the area </param>
		/// <param name="format"> The format of the area </param>
		/// <remarks>
		///   In case that the format is non-compressed, this simply returns how many times we can divide this texture in 2 until we reach 1x1. For compressed formats, constraints apply on minimum size and alignment so this might differ
		/// </remarks>
		/// <returns> </returns>
		public static int GetMaxMipmaps( int width, int height, int depth, PixelFormat format )
		{
			int count = 0;
			if ( ( width > 0 ) && ( height > 0 ) )
			{
				do
				{
					if ( width > 1 )
					{
						width /= 2;
					}
					if ( height > 1 )
					{
						height /= 2;
					}
					if ( depth > 1 )
					{
						depth /= 2;
					}

					/*
				NOT needed, compressed formats will have mipmaps up to 1x1
				if(PixelUtil::isValidExtent(width, height, depth, format))
				count ++;
				else
				break;
				*/
				} while ( !( width == 1 && height == 1 && depth == 1 ) );
			}

			return count;
		}

		/// <summary>
		///   Returns next power-of-two size if required by render system, in case RenderSystemCapabilities.NonPowerOf2Textures is supported it returns value as-is
		/// </summary>
		/// <param name="value"> </param>
		/// <returns> </returns>
		public static int OptionalPO2( int value )
		{
			var caps = Core.Root.Instance.RenderSystem.Capabilities;

			if ( caps.HasCapability( Graphics.Capabilities.NonPowerOf2Textures ) )
			{
				return value;
			}
			else
			{
				return (int) Bitwise.FirstPO2From( (uint) value );
			}
		}

		public static void ConvertToGLFormat( ref PixelBox src, ref PixelBox dst )
		{
			//todo
		}
	}
}
