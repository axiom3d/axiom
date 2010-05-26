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
using System.IO;

using Axiom.Core;
using Axiom.Math;

#endregion Namespace Declarations



namespace Axiom.Media
{

    /// <summary>
    /// Codec specialized in loading DDS (Direct Draw Surface) images.
    /// </summary>
    /// <remarks>
    /// We implement our own codec here since we need to be able to keep DXT
    /// data compressed if the card supports it.
    /// </remarks>
    public partial class DDSCodec : ImageCodec
    {
        #region Constants

        private const UInt32 DDS_MAGIC = ( (UInt32)'D' << 0 ) + ( (UInt32)'D' << 8 ) + ( (UInt32)'S' << 16 ) + ( (UInt32)' ' << 24 );
        private const UInt32 DDS_PIXELFORMAT_SIZE = 8 * sizeof ( UInt32 );
        private const UInt32 DDS_CAPS_SIZE = 4 * sizeof ( UInt32 );
        private const UInt32 DDS_HEADER_SIZE = 19 * sizeof ( UInt32 ) + DDS_PIXELFORMAT_SIZE + DDS_CAPS_SIZE;

        private const UInt32 DDSD_CAPS = 0x00000001;
        private const UInt32 DDSD_HEIGHT = 0x00000002;
        private const UInt32 DDSD_WIDTH = 0x00000004;
        private const UInt32 DDSD_PITCH = 0x00000008;
        private const UInt32 DDSD_PIXELFORMAT = 0x00001000;
        private const UInt32 DDSD_MIPMAPCOUNT = 0x00020000;
        private const UInt32 DDSD_LINEARSIZE = 0x00080000;
        private const UInt32 DDSD_DEPTH = 0x00800000;
        private const UInt32 DDPF_ALPHAPIXELS = 0x00000001;
        private const UInt32 DDPF_FOURCC = 0x00000004;
        private const UInt32 DDPF_RGB = 0x00000040;
        private const UInt32 DDSCAPS_COMPLEX = 0x00000008;
        private const UInt32 DDSCAPS_TEXTURE = 0x00001000;
        private const UInt32 DDSCAPS_MIPMAP = 0x00400000;
        private const UInt32 DDSCAPS2_CUBEMAP = 0x00000200;
        private const UInt32 DDSCAPS2_CUBEMAP_POSITIVEX = 0x00000400;
        private const UInt32 DDSCAPS2_CUBEMAP_NEGATIVEX = 0x00000800;
        private const UInt32 DDSCAPS2_CUBEMAP_POSITIVEY = 0x00001000;
        private const UInt32 DDSCAPS2_CUBEMAP_NEGATIVEY = 0x00002000;
        private const UInt32 DDSCAPS2_CUBEMAP_POSITIVEZ = 0x00004000;
        private const UInt32 DDSCAPS2_CUBEMAP_NEGATIVEZ = 0x00008000;
        private const UInt32 DDSCAPS2_VOLUME = 0x00200000;

        // Special FourCC codes
        private const UInt32 D3DFMT_R16F = 111;
        private const UInt32 D3DFMT_G16R16F = 112;
        private const UInt32 D3DFMT_A16B16G16R16F = 113;
        private const UInt32 D3DFMT_R32F = 114;
        private const UInt32 D3DFMT_G32R32F = 115;
        private const UInt32 D3DFMT_A32B32G32R32F = 116;
        private const UInt32 D3DFMT_DXT1 = ( (UInt32)'D' << 0 ) + ( (UInt32)'X' << 8 ) + ( (UInt32)'T' << 16 ) + ( (UInt32)'1' << 24 );
        private const UInt32 D3DFMT_DXT2 = ( (UInt32)'D' << 0 ) + ( (UInt32)'X' << 8 ) + ( (UInt32)'T' << 16 ) + ( (UInt32)'2' << 24 );
        private const UInt32 D3DFMT_DXT3 = ( (UInt32)'D' << 0 ) + ( (UInt32)'X' << 8 ) + ( (UInt32)'T' << 16 ) + ( (UInt32)'3' << 24 );
        private const UInt32 D3DFMT_DXT4 = ( (UInt32)'D' << 0 ) + ( (UInt32)'X' << 8 ) + ( (UInt32)'T' << 16 ) + ( (UInt32)'4' << 24 );
        private const UInt32 D3DFMT_DXT5 = ( (UInt32)'D' << 0 ) + ( (UInt32)'X' << 8 ) + ( (UInt32)'T' << 16 ) + ( (UInt32)'5' << 24 );

        #endregion Constants

        #region Fields and Properties

        #endregion Fields and Properties

        #region Construction and Destruction

        /// <summary>
        /// Creates a new instance of <see cref="DDSCodec"/>
        /// </summary>
        protected DDSCodec()
        {
        }

        /// <summary>
        /// Finalizer
        /// </summary>
        ~DDSCodec()
        {
        }

        #endregion Construction and Destruction

        /// <summary>
        /// An 8-byte DXT color block, represents a 4x4 texel area. Used by all DXT formats
        /// </summary>
        private class DXTColorBlock
        {
            // 2 colour ranges
            public UInt16 Color0;
            public UInt16 Color1;
            // 16 2-bit indexes, each byte here is one row
            public Byte[] IndexRow = new byte[ 4 ];
        }

        /// <summary>
        /// An 8-byte DXT explicit alpha block, represents a 4x4 texel area. Used by DXT2/3
        /// </summary>
        private class DXTExplicitAlphaBlock
        {
            // 16 4-bit values, each 16-bit value is one row
            public UInt16[] AlphaRow = new ushort[ 4 ];
        }

        /// <summary>
        /// An 8-byte DXT interpolated alpha block, represents a 4x4 texel area. Used by DXT4/5
        /// </summary>
        private class DXTInterpolatedAlphaBlock
        {
            // 2 alpha ranges
            public Byte Alpha0;
            public Byte Alpha1;
            // 16 3-bit indexes. Unfortunately 3 bits doesn't map too well to row bytes
            // so just stored raw
            public Byte[] Indexes = new byte[ 6 ];
        }

		private struct FourCC
		{
			private UInt32 value;

			public static readonly UInt32 DXT1 = new FourCC('D', 'X', 'T', '1');
			public static readonly UInt32 DXT2 = new FourCC('D', 'X', 'T', '2');
			public static readonly UInt32 DXT3 = new FourCC('D', 'X', 'T', '3');
			public static readonly UInt32 DXT4 = new FourCC('D', 'X', 'T', '4');
			public static readonly UInt32 DXT5 = new FourCC('D', 'X', 'T', '5');

			public FourCC(char c0, char c1, char c2, char c3)
			{
				this.value = c0 | (UInt32)(c1 << 8) | (UInt32)(c2 << 16) | (UInt32)(c3 << 24);
			}

			static public implicit operator UInt32(FourCC lhs)
			{
				return lhs.value;
			}
		}

        private void FlipEndian( IntPtr pData, UInt32 size, UInt32 count )
        {
        }

        private void FlipEndian( IntPtr pData, UInt32 size )
        {
        }

        private PixelFormat ConvertFourCCFormat( UInt32 fourcc )
        {
		// convert dxt pixel format
			if (fourcc == FourCC.DXT1)
				return PixelFormat.DXT1;
			if (fourcc == FourCC.DXT2)
				return PixelFormat.DXT2;
			if (fourcc == FourCC.DXT3)
				return PixelFormat.DXT3;
			if (fourcc == FourCC.DXT4)
				return PixelFormat.DXT4;
			if (fourcc == FourCC.DXT5)
				return PixelFormat.DXT5;
			switch (fourcc)
			{
				case D3DFMT_R16F:
					return PixelFormat.FLOAT16_R;
				case D3DFMT_G16R16F:
					return PixelFormat.FLOAT16_GR;
				case D3DFMT_A16B16G16R16F:
					return PixelFormat.FLOAT16_RGBA;
				case D3DFMT_R32F:
					return PixelFormat.FLOAT32_R;
				case D3DFMT_G32R32F:
					return PixelFormat.FLOAT32_GR;
				case D3DFMT_A32B32G32R32F:
					return PixelFormat.FLOAT32_RGBA;
					// We could support 3Dc here, but only ATI cards support it, not nVidia
				default:
					throw new AxiomException( "Unsupported FourCC format found in DDS file" );
			}
        }

		private PixelFormat ConvertPixelFormat(UInt32 rgbBits, UInt32 rMask, UInt32 gMask, UInt32 bMask, UInt32 aMask)
		{

			// General search through pixel formats
			for ( int i = (int)PixelFormat.Unknown + 1; i < (int)PixelFormat.Count; ++i )
			{
				PixelFormat pf = (PixelFormat)( i );
				if ( PixelUtil.GetNumElemBits( pf ) == rgbBits )
				{
					UInt32[] testMasks = PixelUtil.GetBitMasks( pf );
					int[] testBits = PixelUtil.GetBitDepths( pf );
					if ( testMasks[ 0 ] == rMask && testMasks[ 1 ] == gMask &&
					     testMasks[ 2 ] == bMask &&
					     // for alpha, deal with 'X8' formats by checking bit counts
					     ( testMasks[ 3 ] == aMask || ( aMask == 0 && testBits[ 3 ] == 0 ) ) )
					{
						return pf;
					}
				}

			}

			throw new AxiomException( "Cannot determine pixel format" );
		}

    	/// Unpack DXT colours into array of 16 colour values
		private void UnpackDXTColour(PixelFormat pf, DXTColorBlock block, ColorEx[] pCol)
    	{
    		// Note - we assume all values have already been endian swapped

    		// Colour lookup table
    		ColorEx[] derivedColours = new ColorEx[4];

    		IntPtr color0 = Memory.PinObject( block.Color0 );
    		IntPtr color1 = Memory.PinObject( block.Color1 );
    		if ( pf == PixelFormat.DXT1 && block.Color0 <= block.Color1 )
    		{
    			// 1-bit alpha
    			derivedColours[ 0 ] = PixelConverter.UnpackColor( PixelFormat.R5G6B5, color0 );
    			derivedColours[ 1 ] = PixelConverter.UnpackColor( PixelFormat.R5G6B5, color1 );
    			// one intermediate colour, half way between the other two
    			derivedColours[ 2 ] = ( derivedColours[ 0 ] + derivedColours[ 1 ] ) / 2;
    			// transparent colour
    			derivedColours[ 3 ] = ColorEx.Black;
    		}
    		else
    		{
    			derivedColours[ 0 ] = PixelConverter.UnpackColor( PixelFormat.R5G6B5, color0 );
    			derivedColours[ 1 ] = PixelConverter.UnpackColor( PixelFormat.R5G6B5, color1 );
    			// first interpolated colour, 1/3 of the way along
    			derivedColours[ 2 ] = ( derivedColours[ 0 ] * 2f + derivedColours[ 1 ] ) / 3;
    			// second interpolated colour, 2/3 of the way along
    			derivedColours[ 3 ] = ( derivedColours[ 0 ] + derivedColours[ 1 ] * 2f ) / 3;
    		}
    		Memory.UnpinObject( block.Color0 );
    		Memory.UnpinObject( block.Color1 );

    		// Process 4x4 block of texels
    		for ( int row = 0; row < 4; ++row )
    		{
    			for ( int x = 0; x < 4; ++x )
    			{
    				// LSB come first
    				int colIdx = ( block.IndexRow[ row ] >> ( x * 2 ) & 0x3 );
    				if ( pf == PixelFormat.DXT1 )
    				{
    					// Overwrite entire colour
    					pCol[ ( row * 4 ) + x ] = derivedColours[ colIdx ];
    				}
    				else
    				{
    					// alpha has already been read (alpha precedes color)
    					ColorEx col = pCol[ ( row * 4 ) + x ];
    					col.r = derivedColours[ colIdx ].r;
    					col.g = derivedColours[ colIdx ].g;
    					col.b = derivedColours[ colIdx ].b;
    				}
    			}

    		}
    	}


    	/// Unpack DXT alphas into array of 16 colour values
        private void UnpackDXTAlpha( DXTExplicitAlphaBlock block, ColorEx[] pCol )
        {
			// Note - we assume all values have already been endian swapped
    		int colorIndex = 0;
			// This is an explicit alpha block, 4 bits per pixel, LSB first
			for (int row = 0; row < 4; ++row)
			{
				for (int x = 0; x < 4; ++x)
				{
					// Shift and mask off to 4 bits
					int val = (block.AlphaRow[row] >> (x * 4) & 0xF);
					// Convert to [0,1]
					pCol[colorIndex++].a = (Real)val / 0xF;
				}
			}
        }

        /// Unpack DXT alphas into array of 16 colour values
		private void UnpackDXTAlpha(DXTInterpolatedAlphaBlock block, ColorEx[] pCol)
        {
        	// 8 derived alpha values to be indexed
        	Real[] derivedAlphas = new Real[8];

        	// Explicit extremes
        	derivedAlphas[ 0 ] = block.Alpha0 / (Real)0xFF;
        	derivedAlphas[ 1 ] = block.Alpha1 / (Real)0xFF;


        	if ( block.Alpha0 <= block.Alpha1 )
        	{
        		// 4 interpolated alphas, plus zero and one			
        		// full range including extremes at [0] and [5]
        		// we want to fill in [1] through [4] at weights ranging
        		// from 1/5 to 4/5
        		Real denom = 1.0f / 5.0f;
        		for ( int i = 0; i < 4; ++i )
        		{
        			Real factor0 = ( 4 - i ) * denom;
        			Real factor1 = ( i + 1 ) * denom;
        			derivedAlphas[ i + 2 ] =
        				( factor0 * block.Alpha0 ) + ( factor1 * block.Alpha1 );
        		}
        		derivedAlphas[ 6 ] = 0.0f;
        		derivedAlphas[ 7 ] = 1.0f;

        	}
        	else
        	{
        		// 6 interpolated alphas
        		// full range including extremes at [0] and [7]
        		// we want to fill in [1] through [6] at weights ranging
        		// from 1/7 to 6/
        		Real denom = 1.0f / 7.0f;
        		for ( int i = 0; i < 6; ++i )
        		{
        			Real factor0 = ( 6 - i ) * denom;
        			Real factor1 = ( i + 1 ) * denom;
        			derivedAlphas[ i + 2 ] =
        				( factor0 * block.Alpha0 ) + ( factor1 * block.Alpha1 );
        		}

        	}

        	// Ok, now we've built the reference values, process the indexes
        	for ( int i = 0; i < 16; ++i )
        	{
        		int baseByte = ( i * 3 ) / 8;
        		int baseBit = ( i * 3 ) % 8;
        		int bits = ( block.Indexes[ baseByte ] >> baseBit & 0x7 );
        		// do we need to stitch in next byte too?
        		if ( baseBit > 5 )
        		{
        			int extraBits = ( block.Indexes[ baseByte + 1 ] << ( 8 - baseBit ) ) & 0xFF;
        			bits |= extraBits & 0x7;
        		}
        		pCol[ i ].a = derivedAlphas[ bits ];

        	}
        }

    	/// Static method to startup and register the DDS codec
        public static void Startup()
        {
			CodecManager.Instance.RegisterCodec( new DDSCodec() );
        }

        /// Static method to shutdown and unregister the DDS codec
        public static void Shutdown()
        {
			// TODO: CodecManager.Instance.UnregisterCodec( "dds" );
        }

        #region ImageCodec Implementation

        /// <summary>
        /// Codec type
        /// </summary>
        private readonly String _type = "dds";
        /// <summary>
        /// Gets the type of data that this codec is meant to handle, typically a file extension.
        /// </summary>
        public override string Type
        {
            get
            {
                return _type;
            }
        }

        /// <summary>
        /// Codes the data from the input chunk into the output chunk.
        /// </summary>
        /// <param name="input">Input stream (encoded data).</param>
        /// <param name="output">Output stream (decoded data).</param>
        /// <param name="args">Variable number of extra arguments.</param>
        /// <returns>
        /// An object that holds data specific to the media format which this codec deal with.
        ///     For example, an image codec might return a structure that has image related details,
        ///     such as height, width, etc.
        /// </returns>
        public override object Decode( Stream input, Stream output, params object[] args )
        {
        	int filePos = 0;

			// Read 4 character code
        	{
        		byte[] filetype = new byte[sizeof(uint)];
        		filePos += input.Read( filetype, filePos, sizeof ( uint ) );
        		uint fileType = 0;
        		IntPtr ptr = Memory.PinObject( fileType );
				FlipEndian( ptr, sizeof(uint), 1);
				Memory.UnpinObject( fileType );
		
				if (new FourCC('D', 'D', 'S', ' ') != fileType)
				{
					throw new AxiomException("This is not a DDS file!");
				}
        	}
		
/*			// Read header in full
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
				imgData->num_mipmaps = static_cast<ushort>(header.mipMapCount - 1);
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
			return ret;*/

        	return null;
		
        }

        /// <summary>
        /// Encodes the data in the input stream and saves the result in the output stream.
        /// </summary>
        /// <param name="input">Input stream (decoded data).</param>
        /// <param name="output">Output stream (encoded data).</param>
        /// <param name="args">Variable number of extra arguments.</param>
        public override void Encode( Stream input, Stream output, params object[] args )
        {
        }

        /// <summary>
        /// Encodes data to a file.
        /// </summary>
        /// <param name="input">Stream containing data to write.</param>
        /// <param name="fileName">Filename to output to.</param>
        /// <param name="codecData">Extra data to use in order to describe the codec data.</param>
        public override void EncodeToFile( Stream input, string filename, object codecData )
        {
        	Stream output = File.Create( filename );
			this.Encode( input, output, codecData  );
        }

        public String MagicNumberToFileExt( char magicNumber, UInt32 maxbytes )
        {
            return String.Empty;
        }

        #endregion
    }
}
