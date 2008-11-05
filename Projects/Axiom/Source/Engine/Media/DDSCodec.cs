#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2006 Axiom Project Team

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
//     <license see="http://axiomengine.sf.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;

using Axiom.Core;
using Axiom.Graphics;
using System.IO;

#if !(XBOX || XBOX360 || SILVERLIGHT)
using Tao.DevIl;
#else
using Real = System.Single;
#endif

#endregion Namespace Declarations

namespace Axiom.Media
{

#if !(XBOX || XBOX360 || SILVERLIGHT)

    /// <summary>
    ///    Microsoft's DDS file format codec.
    /// </summary>
    public class DDSCodec : ILImageCodec
    {
        public DDSCodec()
        {
        }

        #region ILImageCodec Implementation

        public override object Decode( System.IO.Stream input, System.IO.Stream output, params object[] args )
        {
            ImageData data = new ImageData();

            int imageID;
            int format, bytesPerPixel;

            // create and bind a new image
            Il.ilGenImages( 1, out imageID );
            Il.ilBindImage( imageID );
            Il.ilSetInteger( Il.IL_KEEP_DXTC_DATA, Il.IL_TRUE );

            // create a temp buffer and write the stream into it
            byte[] buffer = new byte[ input.Length ];
            input.Read( buffer, 0, buffer.Length );

            // load the data into DevIL
            Il.ilLoadL( this.ILType, buffer, buffer.Length );

            // check for an error
            int ilError = Il.ilGetError();

            if ( ilError != Il.IL_NO_ERROR )
            {
                throw new AxiomException( "Error while decoding image data: '{0}'", Ilu.iluErrorString( ilError ) );
            }

            format = Il.ilGetInteger( Il.IL_IMAGE_FORMAT );
            bytesPerPixel = Il.ilGetInteger( Il.IL_IMAGE_BYTES_PER_PIXEL );

            // populate the image data
            data.width = Il.ilGetInteger( Il.IL_IMAGE_WIDTH );
            data.height = Il.ilGetInteger( Il.IL_IMAGE_HEIGHT );
            data.depth = Il.ilGetInteger( Il.IL_IMAGE_DEPTH );
            data.numMipMaps = Il.ilGetInteger( Il.IL_NUM_MIPMAPS );
            data.format = ConvertFromILFormat( format, bytesPerPixel );
            data.size = data.width * data.height * bytesPerPixel;

            int dxtFormat = Il.ilGetInteger( Il.IL_DXTC_DATA_FORMAT );

            // check if this dds file contains a cubemap
            bool cubeFlags = ( Il.ilGetInteger( Il.IL_IMAGE_CUBEFLAGS ) > 0 );

            if ( cubeFlags )
            {
                data.flags |= ImageFlags.CubeMap;
            }

            if ( dxtFormat != Il.IL_DXT_NO_COMP
                && Root.Instance.RenderSystem.Caps.CheckCap( Capabilities.TextureCompressionDXT ) )
            {

                // call first with null which returns the size (odd...)
                int dxtSize = Il.ilGetDXTCData( IntPtr.Zero, 0, dxtFormat );

                buffer = new byte[ dxtSize ];

                // get the data into the buffer
                Il.ilGetDXTCData( buffer, dxtSize, dxtFormat );

                // this data is still compressed
                data.size = dxtSize;
                data.format = ConvertFromILFormat( dxtFormat, bytesPerPixel );
                data.flags |= ImageFlags.Compressed;
            }
            else
            {
                int numImagePasses = cubeFlags ? 6 : 1;
                int imageSize = Il.ilGetInteger( Il.IL_IMAGE_SIZE_OF_DATA );

                // create a large enough buffer for all images
                buffer = new byte[ numImagePasses * imageSize ];

                for ( int i = 0, offset = 0; i < numImagePasses; i++, offset += imageSize )
                {
                    if ( cubeFlags )
                    {
                        // rebind and set the current image to be active
                        Il.ilBindImage( imageID );
                        Il.ilActiveImage( i );
                    }

                    // get the decoded data
                    IntPtr ptr = Il.ilGetData();

                    // copy the data into the byte array, using the offset value if this
                    // data contains multiple images
                    unsafe
                    {
                        byte* pBuffer = (byte*)ptr;
                        for ( int j = 0; j < imageSize; j++ )
                        {
                            buffer[ j + offset ] = pBuffer[ j ];
                        }
                    } // unsafe
                } // for
            } // if/else

            // write the data to the output stream
            output.Write( buffer, 0, buffer.Length );

            // we won't be needing this anymore
            Il.ilDeleteImages( 1, ref imageID );

            return data;
        }

        public override void Encode( System.IO.Stream source, System.IO.Stream dest, params object[] args )
        {
            throw new NotImplementedException( "DDS file encoding is not yet supported." );
        }

        /// <summary>
        ///    DDS enum value.
        /// </summary>
        public override int ILType
        {
            get
            {
                return Il.IL_DDS;
            }
        }

        /// <summary>
        ///    Returns that this codec handles dds files.
        /// </summary>
        public override String Type
        {
            get
            {
                return "dds";
            }
        }


        #endregion ILImageCodec Implementation
    }

#else

		// Nested structure
	class DDSPixelFormat
	{
		public uint size;
		public uint flags;
		public uint fourCC;
		public uint rgbBits;
		public uint redMask;
		public uint greenMask;
		public uint blueMask;
		public uint alphaMask;
	};
	
	// Nested structure
	class DDSCaps
	{
		public uint caps1;
		public uint caps2;
		public uint[] reserved = new uint[2];
	};

	// Main header, note preceded by 'DDS '
	class DDSHeader
	{
		public uint size;		
		public uint flags;
		public uint height;
		public uint width;
		public uint sizeOrPitch;
		public uint depth;
		public uint mipMapCount;
		public uint[] reserved1 = new uint[11];
		public DDSPixelFormat pixelFormat;
		public DDSCaps caps;
		public uint reserved2;
	};

	// An 8-byte DXT colour block, represents a 4x4 texel area. Used by all DXT formats
	class DXTColourBlock
	{
		// 2 colour ranges
		public ushort colour_0;
		public ushort colour_1;
		// 16 2-bit indexes, each byte here is one row
		public byte[] indexRow = new byte[4];
	};

	// An 8-byte DXT explicit alpha block, represents a 4x4 texel area. Used by DXT2/3
	class DXTExplicitAlphaBlock
	{
		// 16 4-bit values, each 16-bit value is one row
		public ushort[] alphaRow = new ushort[4];
	};

	// An 8-byte DXT interpolated alpha block, represents a 4x4 texel area. Used by DXT4/5
	class DXTInterpolatedAlphaBlock
	{
		// 2 alpha ranges
		public byte alpha_0;
		public byte alpha_1;
		// 16 3-bit indexes. Unfortunately 3 bits doesn't map too well to row bytes
		// so just stored raw
		public byte[] indexes = new byte[6];
	};
	
	/// <summary>
    ///    Microsoft's DDS file format codec.
    /// </summary>
	public class DDSCodec : ImageCodec
	{
		public const uint DDS_PIXELFORMAT_SIZE = 8 * sizeof(uint);
		public const uint DDS_CAPS_SIZE = 4 * sizeof(uint);
		public const uint DDS_HEADER_SIZE = 19 * sizeof(uint) + DDS_PIXELFORMAT_SIZE + DDS_CAPS_SIZE;
		public const uint DDSD_CAPS = 0x00000001;
		public const uint DDSD_HEIGHT = 0x00000002;
		public const uint DDSD_WIDTH = 0x00000004;
		public const uint DDSD_PITCH = 0x00000008;
		public const uint DDSD_PIXELFORMAT = 0x00001000;
		public const uint DDSD_MIPMAPCOUNT = 0x00020000;
		public const uint DDSD_LINEARSIZE = 0x00080000;
		public const uint DDSD_DEPTH = 0x00800000;
		public const uint DDPF_ALPHAPIXELS = 0x00000001;
		public const uint DDPF_FOURCC = 0x00000004;
		public const uint DDPF_RGB = 0x00000040;
		public const uint DDSCAPS_COMPLEX = 0x00000008;
		public const uint DDSCAPS_TEXTURE = 0x00001000;
		public const uint DDSCAPS_MIPMAP = 0x00400000;
		public const uint DDSCAPS2_CUBEMAP = 0x00000200;
		public const uint DDSCAPS2_CUBEMAP_POSITIVEX = 0x00000400;
		public const uint DDSCAPS2_CUBEMAP_NEGATIVEX = 0x00000800;
		public const uint DDSCAPS2_CUBEMAP_POSITIVEY = 0x00001000;
		public const uint DDSCAPS2_CUBEMAP_NEGATIVEY = 0x00002000;
		public const uint DDSCAPS2_CUBEMAP_POSITIVEZ = 0x00004000;
		public const uint DDSCAPS2_CUBEMAP_NEGATIVEZ = 0x00008000;
		public const uint DDSCAPS2_VOLUME = 0x00200000;

		// Special FourCC codes
		public const uint D3DFMT_R16F = 111;
		public const uint D3DFMT_G16R16F = 112;
		public const uint D3DFMT_A16B16G16R16F = 113;
		public const uint D3DFMT_R32F = 114;
		public const uint D3DFMT_G32R32F = 115;
		public const uint D3DFMT_A32B32G32R32F = 116;
		public const uint DTDFMT_DXT1 = 827611204;
		public const uint DTDFMT_DXT2 = 844388420;
		public const uint DTDFMT_DXT3 = 861165636;
		public const uint DTDFMT_DXT4 = 877942852;
		public const uint DTDFMT_DXT5 = 894720068;

		public DDSCodec()
		{
		}

		/// <summary>
		///    Converts fourcc code to pixel format
		/// </summary>
		private PixelFormat ConvertFourCCFormat(uint fourcc)
		{
			// convert dxt pixel format
			switch (fourcc)
			{
				case DTDFMT_DXT1:
					return PixelFormat.DXT1;
				case DTDFMT_DXT2:
					return PixelFormat.DXT2;
				case DTDFMT_DXT3:
					return PixelFormat.DXT3;
				case DTDFMT_DXT4:
					return PixelFormat.DXT4;
				case DTDFMT_DXT5:
					return PixelFormat.DXT5;
				case D3DFMT_R16F:
					return PixelFormat.Unknown; //FLOAT16_R??
				case D3DFMT_G16R16F:
					return PixelFormat.Unknown; //PF_FLOAT16_GR??
				case D3DFMT_A16B16G16R16F:
					return PixelFormat.A4R4G4B4; //PF_FLOAT16_RGBA;
				case D3DFMT_R32F:
					return PixelFormat.Unknown; //PF_FLOAT32_R??
				case D3DFMT_G32R32F:
					return PixelFormat.Unknown; //PF_FLOAT32_GR??
				case D3DFMT_A32B32G32R32F:
					return PixelFormat.Unknown; //PF_FLOAT32_RGBA??
				// We could support 3Dc here, but only ATI cards support it, not nVidia
				default:
					throw new Exception("Unsupported FourCC format found in DDS file");
			};

		}

		/// <summary>
		///    Gets a pixelformat based on bits and masks
		/// </summary>
		private PixelFormat ConvertPixelFormat(uint rgbBits, uint rMask,
		uint gMask, uint bMask, uint aMask)
		{
			// General search through pixel formats
			for (int i = (int)PixelFormat.Unknown + 1; i < (int)PixelFormat.Count; ++i)
			{
				PixelFormat pf = (PixelFormat)(i);
				if (PixelUtil.GetNumElemBits(pf) == rgbBits)
				{
					uint[] testMasks = PixelUtil.GetBitMasks(pf);
					int[] testBits = PixelUtil.GetBitDepths(pf);
					if (testMasks[0] == rMask && testMasks[1] == gMask &&
						testMasks[2] == bMask &&
						// for alpha, deal with 'X8' formats by checking bit counts
						(testMasks[3] == aMask || (aMask == 0 && testBits[3] == 0)))
					{
						return pf;
					}
				}

			}

			throw new Exception("Cannot determine pixel format");

		}

		//clarabie - started this but now completely broken
        unsafe private void UnpackDXTColour( PixelFormat pf, DXTColourBlock block, ColorEx[] dst )
        {
            // Note - we assume all values have already been endian swapped

            // Colour lookup table
            ColorEx[] derivedColors = new ColorEx[ 4 ];

            if ( pf == PixelFormat.DXT1 && block.colour_0 <= block.colour_1 )
            {
                // 1-bit alpha
                PixelConverter.UnpackColor( out derivedColors[ 0 ].r, out derivedColors[ 0 ].g, out derivedColors[ 0 ].b, out derivedColors[ 0 ].a, PixelFormat.R5G6B5, (byte*)block.colour_0 );
                PixelConverter.UnpackColor( out derivedColors[ 1 ].r, out derivedColors[ 1 ].g, out derivedColors[ 1 ].b, out derivedColors[ 1 ].a, PixelFormat.R5G6B5, (byte*)block.colour_1 );
                // one intermediate colour, half way between the other two
                derivedColors[ 2 ] = ( derivedColors[ 0 ] + derivedColors[ 1 ] ) / 2;
                // transparent colour
                derivedColors[ 3 ] = new ColorEx( 0f, 0f, 0f, 0f );
            }
            else
            {
                PixelConverter.UnpackColor( out derivedColors[ 0 ].r, out derivedColors[ 0 ].g, out derivedColors[ 0 ].b, out derivedColors[ 0 ].a, PixelFormat.R5G6B5, (byte*)block.colour_0 );
                PixelConverter.UnpackColor( out derivedColors[ 1 ].r, out derivedColors[ 1 ].g, out derivedColors[ 1 ].b, out derivedColors[ 1 ].a, PixelFormat.R5G6B5, (byte*)block.colour_1 );
                // first interpolated colour, 1/3 of the way along
                derivedColors[ 2 ] = ( derivedColors[ 0 ] * 2 + derivedColors[ 1 ] ) / 3;
                // second interpolated colour, 2/3 of the way along
                derivedColors[ 3 ] = ( derivedColors[ 0 ] + derivedColors[ 1 ] * 2 ) / 3;
            }

            // Process 4x4 block of texels
            for ( byte row = 0; row < 4; ++row )
            {
                for ( byte x = 0; x < 4; ++x )
                {
                    // LSB come first
                    byte colIdx = (byte)( block.indexRow[ row ] >> ( x * 2 ) & 0x3 );
                    if ( pf == PixelFormat.DXT1 )
                    {
                        // Overwrite entire colour
                        dst[ ( row * 4 ) + x ] = derivedColors[ colIdx ];
                    }
                    else
                    {
                        // alpha has already been read (alpha precedes colour)
                        ColorEx col = dst[ ( row * 4 ) + x ];
                        col.r = derivedColors[ colIdx ].r;
                        col.g = derivedColors[ colIdx ].g;
                        col.b = derivedColors[ colIdx ].b;
                    }
                }

            }


        }

		/// <summary>
		///    Passthrough implementation, no special code needed.
		/// </summary>
		/// <param name="input"></param>
		/// <param name="output"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		public override object Decode(System.IO.Stream input, System.IO.Stream output, params object[] args)
		{
			// nothing special needed, just pass through
			throw new NotImplementedException("DDS decoding is not yet implemented.");
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="source"></param>
		/// <param name="dest"></param>
		/// <param name="args"></param>
		public override void Encode(System.IO.Stream source, System.IO.Stream dest, params object[] args)
		{
			throw new NotImplementedException("DDS encoding is not yet implemented.");
		}

		/// <summary>
		///     Encodes data to a file.
		/// </summary>
		/// <param name="input">Stream containing data to write.</param>
		/// <param name="fileName">Filename to output to.</param>
		/// <param name="codecData">Extra data to use in order to describe the codec data.</param>
		public override void EncodeToFile(Stream input, string fileName, object codecData)
		{
			throw new NotImplementedException("DDS encoding is not yet implemented.");
		}

		/// <summary>
		///    Returns the JPG file extension.
		/// </summary>
		public override String Type
		{
			get
			{
				return "dds";
			}
		}
	}

		/*

	//---------------------------------------------------------------------
	void DDSCodec::unpackDXTAlpha(
		const DXTExplicitAlphaBlock& block, ColourValue* pCol) const
	{
		// Note - we assume all values have already been endian swapped
		
		// This is an explicit alpha block, 4 bits per pixel, LSB first
		for (size_t row = 0; row < 4; ++row)
		{
			for (size_t x = 0; x < 4; ++x)
			{
				// Shift and mask off to 4 bits
				uint8 val = static_cast<uint8>(block.alphaRow[row] >> (x * 4) & 0xF);
				// Convert to [0,1]
				pCol->a = (Real)val / (Real)0xF;
				pCol++;
				
			}
			
		}

	}
	//---------------------------------------------------------------------
	void DDSCodec::unpackDXTAlpha(
		const DXTInterpolatedAlphaBlock& block, ColourValue* pCol) const
	{
		// 8 derived alpha values to be indexed
		Real derivedAlphas[8];

		// Explicit extremes
		derivedAlphas[0] = block.alpha_0 / (Real)0xFF;
		derivedAlphas[1] = block.alpha_1 / (Real)0xFF;
		
		
		if (block.alpha_0 <= block.alpha_1)
		{
			// 4 interpolated alphas, plus zero and one			
			// full range including extremes at [0] and [5]
			// we want to fill in [1] through [4] at weights ranging
			// from 1/5 to 4/5
			Real denom = 1.0f / 5.0f;
			for (size_t i = 0; i < 4; ++i) 
			{
				Real factor0 = (4 - i) * denom;
				Real factor1 = (i + 1) * denom;
				derivedAlphas[i + 2] = 
					(factor0 * block.alpha_0) + (factor1 * block.alpha_1);
			}
			derivedAlphas[6] = 0.0f;
			derivedAlphas[7] = 1.0f;

		}
		else
		{
			// 6 interpolated alphas
			// full range including extremes at [0] and [7]
			// we want to fill in [1] through [6] at weights ranging
			// from 1/7 to 6/7
			Real denom = 1.0f / 7.0f;
			for (size_t i = 0; i < 6; ++i) 
			{
				Real factor0 = (6 - i) * denom;
				Real factor1 = (i + 1) * denom;
				derivedAlphas[i + 2] = 
					(factor0 * block.alpha_0) + (factor1 * block.alpha_1);
			}
			
		}

		// Ok, now we've built the reference values, process the indexes
		for (size_t i = 0; i < 16; ++i)
		{
			size_t baseByte = (i * 3) / 8;
			size_t baseBit = (i * 3) % 8;
			uint8 bits = static_cast<uint8>(block.indexes[baseByte] >> baseBit & 0x7);
			// do we need to stitch in next byte too?
			if (baseBit > 5)
			{
				uint8 extraBits = static_cast<uint8>(
					(block.indexes[baseByte+1] << (8 - baseBit)) & 0xFF);
				bits |= extraBits & 0x7;
			}
			pCol[i].a = derivedAlphas[bits];

		}

	}
    //---------------------------------------------------------------------
    Codec::DecodeResult DDSCodec::decode(DataStreamPtr& stream) const
    {
		// Read 4 character code
		uint32 fileType;
		stream->read(&fileType, sizeof(uint32));
		flipEndian(&fileType, sizeof(uint32), 1);
		
		if (FOURCC('D', 'D', 'S', ' ') != fileType)
		{
			OGRE_EXCEPT(Exception::ERR_INVALIDPARAMS, 
				"This is not a DDS file!", "DDSCodec::decode");
		}
		
		// Read header in full
		DDSHeader header;
		stream->read(&header, sizeof(DDSHeader));

		// Endian flip if required, all 32-bit values
		flipEndian(&header, 4, sizeof(DDSHeader) / 4);

		// Check some sizes
		if (header.size != DDS_HEADER_SIZE)
		{
			OGRE_EXCEPT(Exception::ERR_INVALIDPARAMS, 
				"DDS header size mismatch!", "DDSCodec::decode");
		}
		if (header.pixelFormat.size != DDS_PIXELFORMAT_SIZE)
		{
			OGRE_EXCEPT(Exception::ERR_INVALIDPARAMS, 
				"DDS header size mismatch!", "DDSCodec::decode");
		}

		ImageData* imgData = OGRE_NEW ImageData();
		MemoryDataStreamPtr output;

		imgData->depth = 1; // (deal with volume later)
		imgData->width = header.width;
		imgData->height = header.height;
		size_t numFaces = 1; // assume one face until we know otherwise

		if (header.caps.caps1 & DDSCAPS_MIPMAP)
		{
	        imgData->num_mipmaps = header.mipMapCount - 1;
		}
		else
		{
			imgData->num_mipmaps = 0;
		}
		imgData->flags = 0;

		bool decompressDXT = false;
		// Figure out basic image type
		if (header.caps.caps2 & DDSCAPS2_CUBEMAP)
		{
			imgData->flags |= IF_CUBEMAP;
			numFaces = 6;
		}
		else if (header.caps.caps2 & DDSCAPS2_VOLUME)
		{
			imgData->flags |= IF_3D_TEXTURE;
			imgData->depth = header.depth;
		}
		// Pixel format
		PixelFormat sourceFormat = PF_UNKNOWN;

		if (header.pixelFormat.flags & DDPF_FOURCC)
		{
			sourceFormat = convertFourCCFormat(header.pixelFormat.fourCC);
		}
		else
		{
			sourceFormat = convertPixelFormat(header.pixelFormat.rgbBits, 
				header.pixelFormat.redMask, header.pixelFormat.greenMask, 
				header.pixelFormat.blueMask, 
				header.pixelFormat.flags & DDPF_ALPHAPIXELS ? 
				header.pixelFormat.alphaMask : 0);
		}

		if (PixelUtil::isCompressed(sourceFormat))
		{
			if (!Root::getSingleton().getRenderSystem()->getCapabilities()
				->hasCapability(RSC_TEXTURE_COMPRESSION_DXT))
			{
				// We'll need to decompress
				decompressDXT = true;
				// Convert format
				switch (sourceFormat)
				{
				case PF_DXT1:
					// source can be either 565 or 5551 depending on whether alpha present
					// unfortunately you have to read a block to figure out which
					// Note that we upgrade to 32-bit pixel formats here, even 
					// though the source is 16-bit; this is because the interpolated
					// values will benefit from the 32-bit results, and the source
					// from which the 16-bit samples are calculated may have been
					// 32-bit so can benefit from this.
					DXTColourBlock block;
					stream->read(&block, sizeof(DXTColourBlock));
					flipEndian(&(block.colour_0), sizeof(uint16), 1);
					flipEndian(&(block.colour_1), sizeof(uint16), 1);
					// skip back since we'll need to read this again
					stream->skip(0 - sizeof(DXTColourBlock));
					// colour_0 <= colour_1 means transparency in DXT1
					if (block.colour_0 <= block.colour_1)
					{
						imgData->format = PF_BYTE_RGBA;
					}
					else
					{
						imgData->format = PF_BYTE_RGB;
					}
					break;
				case PF_DXT2:
				case PF_DXT3:
				case PF_DXT4:
				case PF_DXT5:
					// full alpha present, formats vary only in encoding 
					imgData->format = PF_BYTE_RGBA;
					break;
                default:
                    // all other cases need no special format handling
                    break;
				}
			}
			else
			{
				// Use original format
				imgData->format = sourceFormat;
				// Keep DXT data compressed
				imgData->flags |= IF_COMPRESSED;
			}
		}
		else // not compressed
		{
			// Don't test against DDPF_RGB since greyscale DDS doesn't set this
			// just derive any other kind of format
			imgData->format = sourceFormat;
		}

		// Calculate total size from number of mipmaps, faces and size
		imgData->size = Image::calculateSize(imgData->num_mipmaps, numFaces, 
			imgData->width, imgData->height, imgData->depth, imgData->format);

		// Bind output buffer
		output.bind(OGRE_NEW MemoryDataStream(imgData->size));

		
		// Now deal with the data
		void* destPtr = output->getPtr();

		// all mips for a face, then each face
		for(size_t i = 0; i < numFaces; ++i)
		{   
			size_t width = imgData->width;
			size_t height = imgData->height;
			size_t depth = imgData->depth;

			for(size_t mip = 0; mip <= imgData->num_mipmaps; ++mip)
			{
				size_t dstPitch = width * PixelUtil::getNumElemBytes(imgData->format);

				if (PixelUtil::isCompressed(sourceFormat))
				{
					// Compressed data
					if (decompressDXT)
					{
						DXTColourBlock col;
						DXTInterpolatedAlphaBlock iAlpha;
						DXTExplicitAlphaBlock eAlpha;
						// 4x4 block of decompressed colour
						ColourValue tempColours[16];
						size_t destBpp = PixelUtil::getNumElemBytes(imgData->format);
						size_t sx = std::min(width, (size_t)4);
						size_t sy = std::min(height, (size_t)4);
						size_t destPitchMinus4 = dstPitch - destBpp * sx;
						// slices are done individually
						for(size_t z = 0; z < depth; ++z)
						{
							// 4x4 blocks in x/y
							for (size_t y = 0; y < height; y += 4)
							{
								for (size_t x = 0; x < width; x += 4)
								{
									if (sourceFormat == PF_DXT2 || 
										sourceFormat == PF_DXT3)
									{
										// explicit alpha
										stream->read(&eAlpha, sizeof(DXTExplicitAlphaBlock));
										flipEndian(eAlpha.alphaRow, sizeof(uint16), 4);
										unpackDXTAlpha(eAlpha, tempColours) ;
									}
									else if (sourceFormat == PF_DXT4 || 
										sourceFormat == PF_DXT5)
									{
										// interpolated alpha
										stream->read(&iAlpha, sizeof(DXTInterpolatedAlphaBlock));
										flipEndian(&(iAlpha.alpha_0), sizeof(uint16), 1);
										flipEndian(&(iAlpha.alpha_1), sizeof(uint16), 1);
										unpackDXTAlpha(iAlpha, tempColours) ;
									}
									// always read colour
									stream->read(&col, sizeof(DXTColourBlock));
									flipEndian(&(col.colour_0), sizeof(uint16), 1);
									flipEndian(&(col.colour_1), sizeof(uint16), 1);
									unpackDXTColour(sourceFormat, col, tempColours);

									// write 4x4 block to uncompressed version
									for (size_t by = 0; by < sy; ++by)
									{
										for (size_t bx = 0; bx < sx; ++bx)
										{
											PixelUtil::packColour(tempColours[by*4+bx],
												imgData->format, destPtr);
											destPtr = static_cast<void*>(
												static_cast<uchar*>(destPtr) + destBpp);
										}
										// advance to next row
										destPtr = static_cast<void*>(
											static_cast<uchar*>(destPtr) + destPitchMinus4);
									}
									// next block. Our dest pointer is 4 lines down
									// from where it started
									if (x + 4 >= width)
									{
										// Jump back to the start of the line
										destPtr = static_cast<void*>(
											static_cast<uchar*>(destPtr) - destPitchMinus4);
									}
									else
									{
										// Jump back up 4 rows and 4 pixels to the
										// right to be at the next block to the right
										destPtr = static_cast<void*>(
											static_cast<uchar*>(destPtr) - dstPitch * sy + destBpp * sx);

									}

								}

							}
						}

					}
					else
					{
						// load directly
						// DDS format lies! sizeOrPitch is not always set for DXT!!
						size_t dxtSize = PixelUtil::getMemorySize(width, height, depth, imgData->format);
						stream->read(destPtr, dxtSize);
						destPtr = static_cast<void*>(static_cast<uchar*>(destPtr) + dxtSize);
					}

				}
				else
				{
					// Final data - trim incoming pitch
					size_t srcPitch;
					if (header.flags & DDSD_PITCH)
					{
						srcPitch = header.sizeOrPitch / 
							std::max((size_t)1, mip * 2);
					}
					else
					{
						// assume same as final pitch
						srcPitch = dstPitch;
					}
					assert (dstPitch <= srcPitch);
					long srcAdvance = static_cast<long>(srcPitch) - static_cast<long>(dstPitch);

					for (size_t z = 0; z < imgData->depth; ++z)
					{
						for (size_t y = 0; y < imgData->height; ++y)
						{
							stream->read(destPtr, dstPitch);
							if (srcAdvance > 0)
								stream->skip(srcAdvance);

							destPtr = static_cast<void*>(static_cast<uchar*>(destPtr) + dstPitch);
						}
					}

				}

				
				/// Next mip
				if(width!=1) width /= 2;
				if(height!=1) height /= 2;
				if(depth!=1) depth /= 2;
			}

		}

		DecodeResult ret;
		ret.first = output;
		ret.second = CodecDataPtr(imgData);
		return ret;
		


    }
    //---------------------------------------------------------------------    
    String DDSCodec::getType() const 
    {
        return mType;
    }
    //---------------------------------------------------------------------    
    void DDSCodec::flipEndian(void * pData, size_t size, size_t count) const
    {
#if OGRE_ENDIAN == OGRE_ENDIAN_BIG
		for(unsigned int index = 0; index < count; index++)
        {
            flipEndian((void *)((long)pData + (index * size)), size);
        }
#endif
    }
    //---------------------------------------------------------------------    
    void DDSCodec::flipEndian(void * pData, size_t size) const
    {
#if OGRE_ENDIAN == OGRE_ENDIAN_BIG
        char swapByte;
        for(unsigned int byteIndex = 0; byteIndex < size/2; byteIndex++)
        {
            swapByte = *(char *)((long)pData + byteIndex);
            *(char *)((long)pData + byteIndex) = *(char *)((long)pData + size - byteIndex - 1);
            *(char *)((long)pData + size - byteIndex - 1) = swapByte;
        }
#endif
    }
	//---------------------------------------------------------------------
	String DDSCodec::magicNumberToFileExt(const char *magicNumberPtr, size_t maxbytes) const
	{
		if (maxbytes >= sizeof(uint32))
		{
			uint32 fileType;
			memcpy(&fileType, magicNumberPtr, sizeof(uint32));
			flipEndian(&fileType, sizeof(uint32), 1);

			if (FOURCC('D', 'D', 'S', ' ') == fileType)
			{
				return String("dds");
			}
		}

		return StringUtil::BLANK;

	}
		 */

#endif

}
