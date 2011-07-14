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
#endregion

#region SVN Version Information
// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id: AssemblyInfo.cs 1537 2009-03-30 19:25:01Z borrillis $"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Diagnostics;
using System.Text;
using FI = FreeImageAPI;
using RegisteredCodec = System.Collections.Generic.List<Axiom.Media.ImageCodec>;

using Axiom.Core;
using Axiom.Media;

#endregion Namespace Declarations

namespace Axiom.Plugins.FreeImageCodecs
{
	/// <summary>
	/// 
	/// </summary>
	public class FreeImageCodec : ImageCodec
	{
		private string _type;
		private FI.FREE_IMAGE_TYPE _freeImageType;
		private static RegisteredCodec _codecList = new RegisteredCodec();

		public FreeImageCodec( string type, FI.FREE_IMAGE_TYPE freeImageType )
		{
			_type = type;
			_freeImageType = freeImageType;
		}
		public static void Initialize()
		{
			if ( !FI.FreeImage.IsAvailable() )
			{
				LogManager.Instance.Write( "[ Warning ] No Freeimage found." );
				return;
			}

			LogManager.Instance.Write( "FreeImage Version: {0}", FI.FreeImage.GetVersion() );
			LogManager.Instance.Write( FI.FreeImage.GetCopyrightMessage() );

			StringBuilder sb = new StringBuilder();
			sb.Append( " Supported formats: " );
			bool first = true;
			for ( int i = 0; i < FI.FreeImage.GetFIFCount(); i++ )
			{
				if ( (FI.FREE_IMAGE_FORMAT)i == FI.FREE_IMAGE_FORMAT.FIF_DDS )
					continue;

				string exts = FI.FreeImage.GetFIFExtensionList( (FI.FREE_IMAGE_FORMAT)i );
				if ( !first )
				{
					sb.Append( "," );
				}
				else
					first = false;
				sb.Append( exts );
				// Pull off individual formats (separated by comma by FI)
				string[] extensions = exts.Split( ',' );
				foreach ( string extension in extensions )
				{
					// FreeImage 3.13 lists many formats twice: once under their own codec and
					// once under the "RAW" codec, which is listed last. Avoid letting the RAW override
					// the dedicated codec!
					if ( !CodecManager.Instance.IsCodecAviable( extension ) )
					{
						ImageCodec codec = new FreeImageCodec( extension, (FI.FREE_IMAGE_TYPE)i );
						_codecList.Add( codec );
						CodecManager.Instance.RegisterCodec( codec );
					}
				}

			}

			LogManager.Instance.Write( sb.ToString() );
			FI.FreeImageEngine.Message += new FI.OutputMessageFunction( FreeImageLoadErrorHandler );
		}

		/// <summary>
		/// 
		/// </summary>
		public static void Shutdown()
		{
			foreach ( ICodec codec in _codecList )
			{
				CodecManager.Instance.UnregisterCodec( codec );
			}
			_codecList.Clear();
			FI.FreeImageEngine.Message -= FreeImageLoadErrorHandler;
		}

		/// <summary>
		/// 
		/// </summary>
		public override string Type
		{
			get
			{
				return _type;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="input"></param>
		/// <param name="output"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		public override object Decode( System.IO.Stream input, System.IO.Stream output, params object[] args )
		{
			// Set error handler
			FI.FreeImageEngine.Message += new FI.OutputMessageFunction( FreeImageLoadErrorHandler );
			// Buffer stream into memory (TODO: override IO functions instead?)
			byte[] data = new byte[ (int)input.Length ];
			input.Read( data, 0, data.Length );
			IntPtr datPtr = Memory.PinObject( data );
			FI.FIMEMORY fiMem = FI.FreeImage.OpenMemory( datPtr, (uint)data.Length );
			FI.FREE_IMAGE_FORMAT ff = (FI.FREE_IMAGE_FORMAT)_freeImageType;
			FI.FIBITMAP fiBitmap = FI.FreeImage.LoadFromMemory( (FI.FREE_IMAGE_FORMAT)_freeImageType, fiMem, FI.FREE_IMAGE_LOAD_FLAGS.DEFAULT );
			if ( fiBitmap.IsNull )
			{
			    Debugger.Break();
				throw new AxiomException( "Error decoding image" );
			}

			ImageData imgData = new ImageData();
			//output = new System.IO.MemoryStream();
			imgData.depth = 1;// only 2D formats handled by this codec
			imgData.width = (int)FI.FreeImage.GetWidth( fiBitmap );
			imgData.height = (int)FI.FreeImage.GetHeight( fiBitmap );
			imgData.numMipMaps = 0;// no mipmaps in non-DDS

			// Must derive format first, this may perform conversions
			FI.FREE_IMAGE_TYPE imageType = FI.FreeImage.GetImageType( fiBitmap );
			FI.FREE_IMAGE_COLOR_TYPE colorType = FI.FreeImage.GetColorType( fiBitmap );
			int bpp = (int)FI.FreeImage.GetBPP( fiBitmap );

			switch ( imageType )
			{
				case FI.FREE_IMAGE_TYPE.FIT_UNKNOWN:
				case FI.FREE_IMAGE_TYPE.FIT_COMPLEX:
				case FI.FREE_IMAGE_TYPE.FIT_UINT32:
				case FI.FREE_IMAGE_TYPE.FIT_INT32:
				case FI.FREE_IMAGE_TYPE.FIT_DOUBLE:
				default:
					throw new AxiomException( "Unknown or unsupported image format" );
					break;
				case FI.FREE_IMAGE_TYPE.FIT_BITMAP:
					// Standard image type
					// Perform any colour conversions for greyscale
					if ( colorType == FI.FREE_IMAGE_COLOR_TYPE.FIC_MINISWHITE || colorType == FI.FREE_IMAGE_COLOR_TYPE.FIC_MINISBLACK )
					{
						FI.FIBITMAP newBitmap = FI.FreeImage.ConvertToGreyscale( fiBitmap );
						// free old bitmap and replace
						FI.FreeImage.Unload( fiBitmap );
						fiBitmap = newBitmap;
						// get new formats
						bpp = (int)FI.FreeImage.GetBPP( fiBitmap );
						colorType = FI.FreeImage.GetColorType( fiBitmap );
					}
					// Perform any colour conversions for RGB
					else if ( bpp < 8 || colorType == FI.FREE_IMAGE_COLOR_TYPE.FIC_PALETTE || colorType == FI.FREE_IMAGE_COLOR_TYPE.FIC_CMYK )
					{
						FI.FIBITMAP newBitmap;
						if ( FI.FreeImage.IsTransparent( fiBitmap ) )
						{
							// convert to 32 bit to preserve the transparency 
							// (the alpha byte will be 0 if pixel is transparent)
							newBitmap = FI.FreeImage.ConvertTo32Bits( fiBitmap );
						}
						else
						{
							// no transparency - only 3 bytes are needed
							newBitmap = FI.FreeImage.ConvertTo24Bits( fiBitmap );
						}

						// free old bitmap and replace
						FI.FreeImage.Unload( fiBitmap );
						fiBitmap = newBitmap;
						// get new formats
						bpp = (int)FI.FreeImage.GetBPP( fiBitmap );
						colorType = FI.FreeImage.GetColorType( fiBitmap );
					}

					// by this stage, 8-bit is greyscale, 16/24/32 bit are RGB[A]
					switch ( bpp )
					{
						case 8:
							imgData.format = PixelFormat.L8;
							break;
						case 16:
							// Determine 555 or 565 from green mask
							// cannot be 16-bit greyscale since that's FIT_UINT16
							if ( FI.FreeImage.GetGreenMask( fiBitmap ) == FI.FreeImage.FI16_565_GREEN_MASK )
							{
								imgData.format = PixelFormat.R5G6B5;
							}
							else
							{
								// FreeImage doesn't support 4444 format so must be 1555
								imgData.format = PixelFormat.A1R5G5B5;
							}
							break;
						case 24:
							// FreeImage differs per platform
							//     PixelFormat.BYTE_BGR[A] for little endian (== PixelFormat.ARGB native)
							//     PixelFormat.BYTE_RGB[A] for big endian (== PixelFormat.RGBA native)
							if ( FI.FreeImage.IsLittleEndian() )
							{
								imgData.format = PixelFormat.BYTE_BGR;
							}
							else
							{
								imgData.format = PixelFormat.BYTE_RGB;
							}
							break;
						case 32:
							if ( FI.FreeImage.IsLittleEndian() )
							{
								imgData.format = PixelFormat.BYTE_BGRA;
							}
							else
							{
								imgData.format = PixelFormat.BYTE_RGBA;
							}
							break;


					};
					break;
				case FI.FREE_IMAGE_TYPE.FIT_UINT16:
				case FI.FREE_IMAGE_TYPE.FIT_INT16:
					// 16-bit greyscale
					imgData.format = PixelFormat.L16;
					break;
				case FI.FREE_IMAGE_TYPE.FIT_FLOAT:
					// Single-component floating point data
					imgData.format = PixelFormat.FLOAT32_R;
					break;
				case FI.FREE_IMAGE_TYPE.FIT_RGB16:
					imgData.format = PixelFormat.SHORT_RGB;
					break;
				case FI.FREE_IMAGE_TYPE.FIT_RGBA16:
					imgData.format = PixelFormat.SHORT_RGBA;
					break;
				case FI.FREE_IMAGE_TYPE.FIT_RGBF:
					imgData.format = PixelFormat.FLOAT32_RGB;
					break;
				case FI.FREE_IMAGE_TYPE.FIT_RGBAF:
					imgData.format = PixelFormat.FLOAT32_RGBA;
					break;
			}

			IntPtr srcData = FI.FreeImage.GetBits( fiBitmap );
			int srcPitch = (int)FI.FreeImage.GetPitch( fiBitmap );
			// Final data - invert image and trim pitch at the same time
			int dstPitch = imgData.width * PixelUtil.GetNumElemBytes( imgData.format );
			imgData.size = dstPitch * imgData.height;
			// Bind output buffer
			byte[] outPutData = new byte[ imgData.size ];
			unsafe
			{
				fixed ( byte* pDstPtr = outPutData )//(byte*)Memory.PinObject( outPutData );
				{
					byte* pDst = pDstPtr;
					byte* pSrc = (byte*)IntPtr.Zero;
					byte* byteSrcData = (byte*)srcData;
					for ( int y = 0; y < imgData.height; y++ )
					{
						pSrc = byteSrcData + ( imgData.height - y - 1 ) * srcPitch;
						Memory.Copy( (IntPtr)pSrc, (IntPtr)pDst, dstPitch );
						pDst += dstPitch;
					}
				}
			}
			//for ( int z = 0; z < outPutData.Length; z += 4 )
			//{
			//    byte tmp = outPutData[ z ];
			//    outPutData[ z ] = outPutData[ z + 2 ];
			//    outPutData[ z + 2 ] = tmp;
			//}
			//output = new System.IO.MemoryStream( outPutData );//.Write( outPutData, 0, outPutData.Length );
			output.Write( outPutData, 0, outPutData.Length );
			FI.FreeImage.Unload( fiBitmap );
			FI.FreeImage.CloseMemory( fiMem );


			return imgData;
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="input"></param>
		/// <param name="codecData"></param>
		/// <returns></returns>
		private FI.FIBITMAP Encode( System.IO.Stream input, object codecData )
		{
			FI.FIBITMAP ret = new FI.FIBITMAP();
			ret.SetNull();
			ImageData imgData = codecData as ImageData;
			if ( imgData != null )
			{
				byte[] data = new byte[ (int)input.Length ];
				input.Read( data, 0, data.Length );
				IntPtr dataPtr = Memory.PinObject( data );
				PixelBox src = new PixelBox( imgData.width, imgData.height, imgData.depth, imgData.format, dataPtr );

				// The required format, which will adjust to the format
				// actually supported by FreeImage.
				PixelFormat requiredFormat = imgData.format;

				// determine the settings
				FI.FREE_IMAGE_TYPE imageType = FI.FREE_IMAGE_TYPE.FIT_UNKNOWN;

				PixelFormat determiningFormat = imgData.format;

				switch ( determiningFormat )
				{
					case PixelFormat.R5G6B5:
					case PixelFormat.B5G6R5:
					case PixelFormat.R8G8B8:
					case PixelFormat.B8G8R8:
					case PixelFormat.A8R8G8B8:
					case PixelFormat.X8R8G8B8:
					case PixelFormat.A8B8G8R8:
					case PixelFormat.X8B8G8R8:
					case PixelFormat.B8G8R8A8:
					case PixelFormat.R8G8B8A8:
					case PixelFormat.A4L4:
					case PixelFormat.BYTE_LA:
					case PixelFormat.R3G3B2:
					case PixelFormat.A4R4G4B4:
					case PixelFormat.A1R5G5B5:
					case PixelFormat.A2R10G10B10:
					case PixelFormat.A2B10G10R10:
						// I'd like to be able to use r/g/b masks to get FreeImage to load the data
						// in it's existing format, but that doesn't work, FreeImage needs to have
						// data in RGB[A] (big endian) and BGR[A] (little endian), always.
						if ( PixelUtil.HasAlpha( determiningFormat ) )
						{
							if ( FI.FreeImageEngine.IsLittleEndian )
							{
								requiredFormat = PixelFormat.BYTE_BGRA;
							}
							else
							{
								requiredFormat = PixelFormat.BYTE_RGBA;
							}

						}
						else
						{
							if ( FI.FreeImageEngine.IsLittleEndian )
							{
								requiredFormat = PixelFormat.BYTE_BGR;
							}
							else
							{
								requiredFormat = PixelFormat.BYTE_RGB;
							}
						}
						imageType = FI.FREE_IMAGE_TYPE.FIT_BITMAP;
						break;
					case PixelFormat.L8:
					case PixelFormat.A8:
						imageType = FI.FREE_IMAGE_TYPE.FIT_BITMAP;
						break;
					case PixelFormat.L16:
						imageType = FI.FREE_IMAGE_TYPE.FIT_UINT16;
						break;
					case PixelFormat.SHORT_GR:
						requiredFormat = PixelFormat.SHORT_RGB;
						break;
					case PixelFormat.SHORT_RGB:
						imageType = FI.FREE_IMAGE_TYPE.FIT_RGB16;
						break;
					case PixelFormat.SHORT_RGBA:
						imageType = FI.FREE_IMAGE_TYPE.FIT_RGBA16;
						break;
					case PixelFormat.FLOAT16_R:
						requiredFormat = PixelFormat.FLOAT32_R;
						break;
					case PixelFormat.FLOAT32_R:
						imageType = FI.FREE_IMAGE_TYPE.FIT_FLOAT;
						break;
					case PixelFormat.FLOAT16_GR:
					case PixelFormat.FLOAT16_RGB:
					case PixelFormat.FLOAT32_GR:
						requiredFormat = PixelFormat.FLOAT32_RGB;
						break;
					case PixelFormat.FLOAT32_RGB:
						imageType = FI.FREE_IMAGE_TYPE.FIT_RGBF;
						break;

					case PixelFormat.FLOAT16_RGBA:
						requiredFormat = PixelFormat.FLOAT32_RGBA;
						break;
					case PixelFormat.FLOAT32_RGBA:
						imageType = FI.FREE_IMAGE_TYPE.FIT_RGBAF;
						break;
					default:
						throw new AxiomException( "Not Supported image format :" + determiningFormat.ToString() );
				}//end switch

				// Check support for this image type & bit depth
				if ( !FI.FreeImage.FIFSupportsExportType( (FI.FREE_IMAGE_FORMAT)_freeImageType, imageType ) ||
					!FI.FreeImage.FIFSupportsExportBPP( (FI.FREE_IMAGE_FORMAT)_freeImageType, PixelUtil.GetNumElemBits( requiredFormat ) ) )
				{
					// Ok, need to allocate a fallback
					// Only deal with RGBA . RGB for now
					switch ( requiredFormat )
					{
						case PixelFormat.BYTE_RGBA:
							requiredFormat = PixelFormat.BYTE_RGB;
							break;
						case PixelFormat.BYTE_BGRA:
							requiredFormat = PixelFormat.BYTE_BGR;
							break;
						default:
							break;
					}
				}

				bool conversionRequired = false;
				input.Position = 0;
				byte[] srcData = new byte[ (int)input.Length ];
				input.Read( srcData, 0, srcData.Length );
				IntPtr srcDataPtr = Memory.PinObject( srcData );
				int bpp = PixelUtil.GetNumElemBits( requiredFormat );
				if ( !FI.FreeImage.FIFSupportsExportBPP( (FI.FREE_IMAGE_FORMAT)_freeImageType, bpp ) )
				{
					if ( bpp == 32 && PixelUtil.HasAlpha( imgData.format ) && FI.FreeImage.FIFSupportsExportBPP( (FI.FREE_IMAGE_FORMAT)_freeImageType, 24 ) )
					{
						// drop to 24 bit (lose alpha)
						if ( FI.FreeImage.IsLittleEndian() )
						{
							requiredFormat = PixelFormat.BYTE_BGR;
						}
						else
						{
							requiredFormat = PixelFormat.BYTE_RGB;
						}
					}
					else if ( bpp == 128 && PixelUtil.HasAlpha( imgData.format ) && FI.FreeImage.FIFSupportsExportBPP( (FI.FREE_IMAGE_FORMAT)_freeImageType, 96 ) )
					{
						//// drop to 96-bit floating point
						requiredFormat = PixelFormat.FLOAT32_RGB;
					}
				}

				PixelBox convBox = new PixelBox( imgData.width, imgData.height, 1, requiredFormat );
				if ( requiredFormat != imgData.format )
				{
					conversionRequired = true;
					// Allocate memory
					byte[] convData = new byte[ convBox.ConsecutiveSize ];
					convBox.Data = Memory.PinObject( convData );

					PixelBox newSrc = new PixelBox( imgData.width, imgData.height, 1, imgData.format, dataPtr );
					PixelConverter.BulkPixelConversion( newSrc, convBox );
					srcDataPtr = convBox.Data;
				}

				ret = FI.FreeImage.AllocateT( imageType, imgData.width, imgData.height, bpp );
				if ( ret.IsNull )
				{
					if ( conversionRequired )
					{
						Memory.UnpinObject( srcData );
						convBox = null;
					}

					throw new AxiomException( "FreeImage.AllocateT failed - possibly out of memory. " );
				}

				if ( requiredFormat == PixelFormat.L8 || requiredFormat == PixelFormat.A8 )
				{
					// Must explicitly tell FreeImage that this is greyscale by setting
					// a "grey" palette (otherwise it will save as a normal RGB
					// palettized image).
					FI.FIBITMAP tmp = FI.FreeImage.ConvertToGreyscale( ret );
					FI.FreeImage.Unload( ret );
					ret = tmp;
				}

				int dstPitch = (int)FI.FreeImage.GetPitch( ret );
				int srcPitch = imgData.width * PixelUtil.GetNumElemBytes( requiredFormat );
				// Copy data, invert scanlines and respect FreeImage pitch
				IntPtr pSrc = srcDataPtr;
				IntPtr pDest = FI.FreeImage.GetBits( ret );
				unsafe
				{
					byte* byteSrcData = (byte*)( pSrc );
					byte* byteDstData = (byte*)( pDest );
					for ( int y = 0; y < imgData.height; y++ )
					{
						byteSrcData = byteSrcData + ( imgData.height - y - 1 ) * srcPitch;
						Memory.Copy( pSrc, pDest, srcPitch );
						byteDstData += dstPitch;
					}
				}

				if ( conversionRequired )
				{
					// delete temporary conversion area
					Memory.UnpinObject( srcData );
					convBox = null;
				}
			}
			return ret;
		}
		public override void Encode( System.IO.Stream input, System.IO.Stream output, params object[] args )
		{

		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="input"></param>
		/// <param name="fileName"></param>
		/// <param name="codecData"></param>
		public override void EncodeToFile( System.IO.Stream input, string fileName, object codecData )
		{
			// Set error handler
			FI.FreeImageEngine.Message += new FI.OutputMessageFunction( FreeImageSaveErrorHandler );

			FI.FIBITMAP fiBitMap = Encode( input, codecData );
			FI.FreeImage.Save( (FI.FREE_IMAGE_FORMAT)_freeImageType, fiBitMap, fileName, FI.FREE_IMAGE_SAVE_FLAGS.DEFAULT );
			FI.FreeImage.Unload( fiBitMap );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="fif"></param>
		/// <param name="message"></param>
		private static void FreeImageSaveErrorHandler( FI.FREE_IMAGE_FORMAT fif, string message )
		{
		}
		private static void FreeImageLoadErrorHandler( FI.FREE_IMAGE_FORMAT fif, string message )
		{
			string format = FI.FreeImage.GetFormatFromFIF( fif );

			LogManager.Instance.Write( "FreeImage error: '" + message + "'" +
				( string.IsNullOrEmpty( format ) ? "." : " when loading format " + format ) );
		}

	}
}
