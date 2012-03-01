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

using System;
using System.IO;
using System.Text;
using Axiom.Core;
using Axiom.CrossPlatform;
using Axiom.Media;
using FI = FreeImageAPI;
using RegisteredCodec = System.Collections.Generic.List<Axiom.Media.ImageCodec>;

#endregion Namespace Declarations

namespace Axiom.Plugins.FreeImageCodecs
{
	/// <summary>
    /// Codec specialized in images loaded using FreeImage.
	/// </summary>
    /// <remarks>
    /// The users implementing subclasses of ImageCodec are required to return
    ///a valid pointer to a ImageData class from the decode(...) function.
    /// </remarks>
	public class FreeImageCodec : ImageCodec
	{
		private string _type;
		private FI.FREE_IMAGE_TYPE _freeImageType;
		private static RegisteredCodec _codecList = new RegisteredCodec();

        [OgreVersion( 1, 7, 2 )]
        public override string Type
        {
            get
            {
                return _type;
            }
        }

        [OgreVersion( 1, 7, 2 )]
		public FreeImageCodec( string type, FI.FREE_IMAGE_TYPE freeImageType )
		{
			_type = type;
			_freeImageType = freeImageType;
		}

        [OgreVersion( 1, 7, 2 )]
        private static void _freeImageLoadErrorHandler( FI.FREE_IMAGE_FORMAT fif, string message )
        {
            // Callback method as required by FreeImage to report problems
            var format = FI.FreeImage.GetFormatFromFIF( fif );

            LogManager.Instance.Write( "FreeImage error: '{0}'{1}", message,
                string.IsNullOrEmpty( format ) ? "." : " when loading format " + format );
        }

        [OgreVersion( 1, 7, 2 )]
        private static void _freeImageSaveErrorHandler( FI.FREE_IMAGE_FORMAT fif, string message )
        {
            // Callback method as required by FreeImage to report problems
            throw new AxiomException( message );
        }

        /// <summary>
        /// Static method to startup FreeImage and register the FreeImage codecs
        /// </summary>
        [OgreVersion( 1, 7, 2, "Original name was startup" )]
        public static void Initialize()
		{
			if ( !FI.FreeImage.IsAvailable() )
			{
				LogManager.Instance.Write( "[ Warning ] No Freeimage found." );
				return;
			}

			LogManager.Instance.Write( "FreeImage Version: {0}", FI.FreeImage.GetVersion() );
			LogManager.Instance.Write( FI.FreeImage.GetCopyrightMessage() );

            // Register codecs
			var sb = new StringBuilder();
			sb.Append( " Supported formats: " );
			var first = true;
			for ( var i = 0; i < FI.FreeImage.GetFIFCount(); i++ )
			{
                // Skip DDS codec since FreeImage does not have the option 
                // to keep DXT data compressed, we'll use our own codec
				if ( (FI.FREE_IMAGE_FORMAT)i == FI.FREE_IMAGE_FORMAT.FIF_DDS )
					continue;

				var exts = FI.FreeImage.GetFIFExtensionList( (FI.FREE_IMAGE_FORMAT)i );
				if ( !first )
					sb.Append( "," );
				else
					first = false;
				sb.Append( exts );

				// Pull off individual formats (separated by comma by FI)
				var extensions = exts.Split( ',' );
				foreach ( var ext in extensions )
				{
					// FreeImage 3.13 lists many formats twice: once under their own codec and
					// once under the "RAW" codec, which is listed last. Avoid letting the RAW override
					// the dedicated codec!
                    if ( !CodecManager.Instance.IsCodecRegistered( ext ) )
					{
						var codec = new FreeImageCodec( ext, (FI.FREE_IMAGE_TYPE)i );
						_codecList.Add( codec );
                        CodecManager.Instance.RegisterCodec( codec );
					}
				}
			}

			LogManager.Instance.Write( sb.ToString() );
			FI.FreeImageEngine.Message += new FI.OutputMessageFunction( _freeImageLoadErrorHandler );
		}

        /// <summary>
        /// Static method to shutdown FreeImage and unregister the FreeImage codecs
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
		public static void Shutdown()
		{
			foreach ( var codec in _codecList )
                CodecManager.Instance.UnregisterCodec( codec );

			_codecList.Clear();
			FI.FreeImageEngine.Message -= _freeImageLoadErrorHandler;
        }

        [OgreVersion( 1, 7, 2 )]
        private FI.FIBITMAP _encode( Stream input, CodecData codecData )
        {
            var ret = new FI.FIBITMAP();
            ret.SetNull();
            var imgData = codecData as ImageData;

            if ( imgData != null )
            {
                var data = new byte[ (int)input.Length ];
                input.Read( data, 0, data.Length );
                var dataPtr = BufferBase.Wrap( data );
                var src = new PixelBox( imgData.width, imgData.height, imgData.depth, imgData.format, dataPtr );

                // The required format, which will adjust to the format
                // actually supported by FreeImage.
                var requiredFormat = imgData.format;

                // determine the settings
                var imageType = FI.FREE_IMAGE_TYPE.FIT_UNKNOWN;
                var determiningFormat = imgData.format;

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
                                requiredFormat = PixelFormat.BYTE_BGRA;
                            else
                                requiredFormat = PixelFormat.BYTE_RGBA;
                        }
                        else
                        {
                            if ( FI.FreeImageEngine.IsLittleEndian )
                                requiredFormat = PixelFormat.BYTE_BGR;
                            else
                                requiredFormat = PixelFormat.BYTE_RGB;
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
                        throw new AxiomException( "Not Supported image format :{0}", determiningFormat.ToString() );
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

                var conversionRequired = false;
                input.Position = 0;
                var srcData = new byte[ (int)input.Length ];
                input.Read( srcData, 0, srcData.Length );
                var srcDataPtr = Memory.PinObject( srcData );

                // Check BPP
                var bpp = PixelUtil.GetNumElemBits( requiredFormat );
                if ( !FI.FreeImage.FIFSupportsExportBPP( (FI.FREE_IMAGE_FORMAT)_freeImageType, bpp ) )
                {
                    if ( bpp == 32 && PixelUtil.HasAlpha( imgData.format ) && FI.FreeImage.FIFSupportsExportBPP( (FI.FREE_IMAGE_FORMAT)_freeImageType, 24 ) )
                    {
                        // drop to 24 bit (lose alpha)
                        if ( FI.FreeImage.IsLittleEndian() )
                            requiredFormat = PixelFormat.BYTE_BGR;
                        else
                            requiredFormat = PixelFormat.BYTE_RGB;

                        bpp = 24;
                    }
                    else if ( bpp == 128 && PixelUtil.HasAlpha( imgData.format ) && FI.FreeImage.FIFSupportsExportBPP( (FI.FREE_IMAGE_FORMAT)_freeImageType, 96 ) )
                    {
                        // drop to 96-bit floating point
                        requiredFormat = PixelFormat.FLOAT32_RGB;
                    }
                }

                var convBox = new PixelBox( imgData.width, imgData.height, 1, requiredFormat );
                if ( requiredFormat != imgData.format )
                {
                    conversionRequired = true;
                    // Allocate memory
                    var convData = new byte[ convBox.ConsecutiveSize ];
                    convBox.Data = BufferBase.Wrap( convData );
                    // perform conversion and reassign source
                    var newSrc = new PixelBox( imgData.width, imgData.height, 1, imgData.format, dataPtr );
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
                    var tmp = FI.FreeImage.ConvertToGreyscale( ret );
                    FI.FreeImage.Unload( ret );
                    ret = tmp;
                }

                var dstPitch = (int)FI.FreeImage.GetPitch( ret );
                var srcPitch = imgData.width * PixelUtil.GetNumElemBytes( requiredFormat );

                // Copy data, invert scanlines and respect FreeImage pitch
                var pSrc = srcDataPtr;
                using ( var pDest = BufferBase.Wrap( FI.FreeImage.GetBits( ret ), imgData.height * srcPitch ) )
                {
                    var byteSrcData = pSrc;
                    var byteDstData = pDest;
                    for ( int y = 0; y < imgData.height; ++y )
                    {
                        byteSrcData += ( imgData.height - y - 1 ) * srcPitch;
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

        /// <see cref="Axiom.Media.Codec.Encode"/>
        [OgreVersion( 1, 7, 2 )]
        public override Stream Encode( Stream input, Codec.CodecData pData )
        {
            var fiBitmap = _encode( input, pData );

            // open memory chunk allocated by FreeImage
            var mem = FI.FreeImage.OpenMemory( IntPtr.Zero, 0 );
            // write data into memory
            FI.FreeImage.SaveToMemory( (FI.FREE_IMAGE_FORMAT)_freeImageType, fiBitmap, mem, FI.FREE_IMAGE_SAVE_FLAGS.DEFAULT );
            // Grab data information
            var data = IntPtr.Zero;
            uint size = 0;
            FI.FreeImage.AcquireMemory( mem, ref data, ref size );
            // Copy data into our own buffer
            // Because we're asking MemoryDataStream to free this, must create in a compatible way
            var ourData = new byte[ size ];
            using ( var src = BufferBase.Wrap( data, (int)size ) )
            {
                using ( var dest = BufferBase.Wrap( ourData ) )
                    Memory.Copy( src, dest, (int)size );
            }
            // Wrap data in stream, tell it to free on close 
            var outstream = new MemoryStream( ourData );
            // Now free FreeImage memory buffers
            FI.FreeImage.CloseMemory( mem );
            // Unload bitmap
            FI.FreeImage.Unload( fiBitmap );

            return outstream;
        }

        /// <see cref="Axiom.Media.Codec.EncodeToFile"/>
        [OgreVersion( 1, 7, 2 )]
        public override void EncodeToFile( Stream input, string outFileName, CodecData data )
        {
            var fiBitMap = _encode( input, data );
            FI.FreeImage.Save( (FI.FREE_IMAGE_FORMAT)_freeImageType, fiBitMap, outFileName, FI.FREE_IMAGE_SAVE_FLAGS.DEFAULT );
            FI.FreeImage.Unload( fiBitMap );
        }

        /// <see cref="Axiom.Media.Codec.Decode"/>
        [OgreVersion( 1, 7, 2 )]
        public override Codec.DecodeResult Decode( Stream input )
        {
            // Buffer stream into memory (TODO: override IO functions instead?)
            var data = new byte[ (int)input.Length ];
            input.Read( data, 0, data.Length );
            FI.FIMEMORY fiMem;
            FI.FREE_IMAGE_FORMAT ff;
            FI.FIBITMAP fiBitmap;
            using ( var datPtr = BufferBase.Wrap( data ) )
            {
                fiMem = FI.FreeImage.OpenMemory( datPtr.Pin(), (uint)data.Length );
                datPtr.UnPin();
                ff = (FI.FREE_IMAGE_FORMAT)_freeImageType;
                fiBitmap = FI.FreeImage.LoadFromMemory( (FI.FREE_IMAGE_FORMAT)_freeImageType, fiMem, FI.FREE_IMAGE_LOAD_FLAGS.DEFAULT );
            }

            if ( fiBitmap.IsNull )
                throw new AxiomException( "Error decoding image" );

            var imgData = new ImageData();

            imgData.depth = 1;// only 2D formats handled by this codec
            imgData.width = (int)FI.FreeImage.GetWidth( fiBitmap );
            imgData.height = (int)FI.FreeImage.GetHeight( fiBitmap );
            imgData.numMipMaps = 0;// no mipmaps in non-DDS

            // Must derive format first, this may perform conversions
            var imageType = FI.FreeImage.GetImageType( fiBitmap );
            var colorType = FI.FreeImage.GetColorType( fiBitmap );
            var bpp = (int)FI.FreeImage.GetBPP( fiBitmap );

            switch ( imageType )
            {
                case FI.FREE_IMAGE_TYPE.FIT_UNKNOWN:
                case FI.FREE_IMAGE_TYPE.FIT_COMPLEX:
                case FI.FREE_IMAGE_TYPE.FIT_UINT32:
                case FI.FREE_IMAGE_TYPE.FIT_INT32:
                case FI.FREE_IMAGE_TYPE.FIT_DOUBLE:
                default:
                    throw new AxiomException( "Unknown or unsupported image format" );

                case FI.FREE_IMAGE_TYPE.FIT_BITMAP:
                    // Standard image type
                    // Perform any colour conversions for greyscale
                    if ( colorType == FI.FREE_IMAGE_COLOR_TYPE.FIC_MINISWHITE || colorType == FI.FREE_IMAGE_COLOR_TYPE.FIC_MINISBLACK )
                    {
                        var newBitmap = FI.FreeImage.ConvertToGreyscale( fiBitmap );
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
                        var newBitmap = FI.FreeImage.ConvertTo24Bits( fiBitmap );
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
                                imgData.format = PixelFormat.R5G6B5;
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
                                imgData.format = PixelFormat.BYTE_BGR;
                            else
                                imgData.format = PixelFormat.BYTE_RGB;
                            break;

                        case 32:
                            if ( FI.FreeImage.IsLittleEndian() )
                                imgData.format = PixelFormat.BYTE_BGRA;
                            else
                                imgData.format = PixelFormat.BYTE_RGBA;
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

            var srcPitch = (int)FI.FreeImage.GetPitch( fiBitmap );
            // Final data - invert image and trim pitch at the same time
            var dstPitch = imgData.width * PixelUtil.GetNumElemBytes( imgData.format );
            imgData.size = dstPitch * imgData.height;
            // Bind output buffer
            var outputData = new byte[ imgData.size ];

            using ( var srcData = BufferBase.Wrap( FI.FreeImage.GetBits( fiBitmap ), imgData.height * srcPitch ) )
            {
                var pDst = BufferBase.Wrap( outputData );

                for ( var y = 0; y < imgData.height; ++y )
                {
                    using ( var pSrc = srcData + ( imgData.height - y - 1 ) * srcPitch )
                    {
                        Memory.Copy( pSrc, pDst, dstPitch );
                        pDst += dstPitch;
                    }
                }

                pDst.Dispose();
            }
            
            FI.FreeImage.Unload( fiBitmap );
            FI.FreeImage.CloseMemory( fiMem );

            return new Codec.DecodeResult( new MemoryStream( outputData ), imgData );
        }

        /// <see cref="Axiom.Media.Codec.MagicNumberToFileExt"/>
        [OgreVersion( 1, 7, 2 )]
        public override string MagicNumberToFileExt( byte[] magicBuf, int maxbytes )
        {
            FI.FIMEMORY fiMem;
            FI.FREE_IMAGE_FORMAT fif;

            using ( var ptr = BufferBase.Wrap( magicBuf ) )
            {
                fiMem = FI.FreeImage.OpenMemory( ptr.Pin(), (uint)maxbytes );
                ptr.UnPin();
                fif = FI.FreeImage.GetFileTypeFromMemory( fiMem, maxbytes );
                FI.FreeImage.CloseMemory( fiMem );
            }

            if ( fif != FI.FREE_IMAGE_FORMAT.FIF_UNKNOWN )
                return FI.FreeImage.GetFormatFromFIF( fif ).ToLower();
            else
                return string.Empty;
        }
    };
}
