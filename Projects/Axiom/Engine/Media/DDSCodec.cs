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
using System.Runtime.InteropServices;

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
    public partial class DDSCodec : ImageCodec, IPlugin
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

        public void Shutdown()
        {
            Stop();
        }
        public void Initialize()
        {
            Startup();
        }
        #region Fields and Properties
        /// <summary>
        /// Single registered codec instance
        /// </summary>
        private static DDSCodec _instance;
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
         [StructLayout( LayoutKind.Sequential )]
        private struct DXTColorBlock
        {
            // 2 colour ranges
            public UInt16 Color0;
            public UInt16 Color1;
            // 16 2-bit indexes, each byte here is one row
            [MarshalAs( UnmanagedType.ByValArray, SizeConst = 4 )]
            public Byte[] IndexRow;
        }

        /// <summary>
        /// An 8-byte DXT explicit alpha block, represents a 4x4 texel area. Used by DXT2/3
        /// </summary>
        [StructLayout( LayoutKind.Sequential )]
        private struct DXTExplicitAlphaBlock
        {
            // 16 4-bit values, each 16-bit value is one row
            [MarshalAs( UnmanagedType.ByValArray, SizeConst = 4 )]
            public UInt16[] AlphaRow;
        }

        /// <summary>
        /// An 8-byte DXT interpolated alpha block, represents a 4x4 texel area. Used by DXT4/5
        /// </summary>
        [StructLayout( LayoutKind.Sequential )]
        private struct DXTInterpolatedAlphaBlock
        {
            // 2 alpha ranges
            public Byte Alpha0;
            public Byte Alpha1;
            // 16 3-bit indexes. Unfortunately 3 bits doesn't map too well to row bytes
            // so just stored raw
            [MarshalAs( UnmanagedType.ByValArray, SizeConst = 6 )]
            public Byte[] Indexes;
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
        [StructLayout( LayoutKind.Sequential )]
        internal struct DDSPixelFormat
        {
            internal UInt32 size;
            internal UInt32 flags;
            internal UInt32 fourCC;
            internal UInt32 rgbBits;
            internal UInt32 redMask;
            internal UInt32 greenMask;
            internal UInt32 blueMask;
            internal UInt32 alphaMask;
        }
        // Nested structure
        [StructLayout( LayoutKind.Sequential )]
        internal struct DDSCaps
        {
            internal UInt32 caps1;
            internal UInt32 caps2;
             [MarshalAs( UnmanagedType.ByValArray, SizeConst = 2 )]
            internal UInt32[] reserved;
        }
        [StructLayout( LayoutKind.Sequential )]
        internal struct DDSHeader
        {
            
            internal UInt32 size;
            internal UInt32 flags;
            internal UInt32 height;
            internal UInt32 width;
            internal UInt32 sizeOrPitch;
            internal UInt32 depth;
            internal UInt32 mipMapCount;
            [MarshalAs( UnmanagedType.ByValArray,SizeConst=11)]
            internal UInt32[] reserved1;
            internal DDSPixelFormat pixelFormat;
            internal DDSCaps caps;
            internal UInt32 reserved2;
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
            if ( _instance == null )
            {
                LogManager.Instance.Write( "DDS codec registering" );
                _instance = new DDSCodec();
                CodecManager.Instance.RegisterCodec( _instance );
            }
        }

        /// Static method to shutdown and unregister the DDS codec
        public static void Stop()
        {
            if ( _instance != null )
            {
                CodecManager.Instance.UnregisterCodec( _instance );
                _instance = null;
            }
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
            BinaryReader br = new BinaryReader( input );
            // Read 4 character code
            {
                byte[] filetype = br.ReadBytes( sizeof( uint ) );
                uint fileType = 0;
                IntPtr ptr = Memory.PinObject( fileType );
                FlipEndian( ptr, sizeof( uint ), 1 );
                Memory.UnpinObject( fileType );
                UInt32 flType = BitConverter.ToUInt32( filetype, 0 );
                if ( new FourCC( 'D', 'D', 'S', ' ' ) != flType )
                {
                    throw new AxiomException( "This is not a DDS file!" );
                }
            }
            // Read header in full
            int read = 0;
            DDSHeader header;
            byte[] buffer = br.ReadBytes( Marshal.SizeOf( typeof( DDSHeader ) ) );
            IntPtr bufferPtr = Memory.PinObject( buffer );
            FlipEndian( bufferPtr, (uint)( Marshal.SizeOf( typeof( DDSHeader ) ) / 4 ) );
            header = (DDSHeader)Marshal.PtrToStructure( bufferPtr, typeof( DDSHeader ) );
            if ( header.size != DDS_HEADER_SIZE )
            {
                LogManager.Instance.Write( "DDS header size mismatch!" );
            }
            if ( header.pixelFormat.size != DDS_PIXELFORMAT_SIZE )
            {
                LogManager.Instance.Write( "DDS header size mismatch!" );
            }
            Memory.UnpinObject( buffer );

            ImageData imgData = new ImageData();
            imgData.depth = 1; // deal with volume later
            imgData.width = (int)header.width;
            imgData.height = (int)header.height;
            int numFaces = 1;// assume one face until we know otherwise

            if ( ( header.caps.caps1 & DDSCAPS_MIPMAP ) != 0 )
            {
                imgData.numMipMaps = (int)( header.mipMapCount - 1 );
            }
            else
            {
                imgData.numMipMaps = 0;
            }
            imgData.flags = 0;
            bool decompressDXT = false;
            // Figure out basic image type
            if ( ( header.caps.caps2 & DDSCAPS2_CUBEMAP ) != 0 )
            {
                imgData.flags |= ImageFlags.CubeMap;
                numFaces = 6;
            }
            else if ( ( header.caps.caps2 & DDSCAPS2_VOLUME ) != 0 )
            {
                imgData.flags |= ImageFlags.Volume;
                imgData.depth = (int)header.depth;
            }

            //pixelFormat
            PixelFormat sourceFormat = PixelFormat.Unknown;

            if ( ( header.pixelFormat.flags & DDPF_FOURCC ) != 0 )
            {
                sourceFormat = ConvertFourCCFormat( header.pixelFormat.fourCC );
            }
            else
            {
                sourceFormat = ConvertPixelFormat( header.pixelFormat.rgbBits,
                    header.pixelFormat.redMask, header.pixelFormat.greenMask, header.pixelFormat.blueMask,
                    ( header.pixelFormat.flags & DDPF_ALPHAPIXELS ) != 0 ? header.pixelFormat.alphaMask : 0 );
            }

            if ( PixelUtil.IsCompressed( sourceFormat ) )
            {
                if ( !Root.Instance.RenderSystem.HardwareCapabilities.HasCapability( Graphics.Capabilities.TextureCompressionDXT ) )
                {
                    // We'll need to decompress
                    decompressDXT = true;
                    //convert format
                    switch ( sourceFormat )
                    {
                        case PixelFormat.DXT1:
                            // source can be either 565 or 5551 depending on whether alpha present
                            // unfortunately you have to read a block to figure out which
                            // Note that we upgrade to 32-bit pixel formats here, even 
                            // though the source is 16-bit; this is because the interpolated
                            // values will benefit from the 32-bit results, and the source
                            // from which the 16-bit samples are calculated may have been
                            // 32-bit so can benefit from this.
                            int length;
                            DXTColorBlock block = ToStructure<DXTColorBlock>( input, out length );
                            IntPtr col0 = Memory.PinObject( block.Color0 );
                            IntPtr col1 = Memory.PinObject( block.Color1 );
                            FlipEndian( col0, (uint)sizeof( ushort ) );
                            FlipEndian( col1, (uint)sizeof( ushort ) );
                            // skip back since we'll need to read this again
                            input.Position = input.Position - length;
                            // colour_0 <= colour_1 means transparency in DXT1
                            if ( block.Color0 <= block.Color1 )
                            {
                                imgData.format = PixelFormat.BYTE_RGBA;
                            }
                            else
                            {
                                imgData.format = PixelFormat.BYTE_RGB;
                            }
                            break;
                        case PixelFormat.DXT2:
                        case PixelFormat.DXT3:
                        case PixelFormat.DXT4:
                        case PixelFormat.DXT5:
                            // full alpha present, formats vary only in encoding 
                            imgData.format = PixelFormat.BYTE_RGBA;
                            break;
                        default:
                            // all other cases need no special format handling
                            break;
                    }//end switch
                }

            }
            else// not compressed
            {
                // Don't test against DDPF_RGB since greyscale DDS doesn't set this
                // just derive any other kind of format
                imgData.format = sourceFormat;
            }

            // Calculate total size from number of mipmaps, faces and size
            imgData.size = Image.CalculateSize( imgData.numMipMaps, numFaces,
                imgData.width, imgData.height, imgData.depth, imgData.format );

            // Bind output buffer
            byte[] destData = new byte[ imgData.size ];
            IntPtr destPtr = Memory.PinObject( destData );
            // all mips for a face, then each face
            for ( int i = 0; i < numFaces; i++ )
            {
                int width = imgData.width;
                int height = imgData.height;
                int depth = imgData.depth;

                for ( int mip = 0; mip <= imgData.numMipMaps; mip++ )
                {
                    int dstPitch = width * PixelUtil.GetNumElemBytes( imgData.format );

                    if ( PixelUtil.IsCompressed( sourceFormat ) )
                    {
                        // Compressed data
                        if ( decompressDXT )
                        {
                            DXTColorBlock col = new DXTColorBlock();
                            DXTInterpolatedAlphaBlock iAlpha = new DXTInterpolatedAlphaBlock();
                            DXTExplicitAlphaBlock eAlpha = new DXTExplicitAlphaBlock();

                            // 4x4 block of decompressed colour
                            ColorEx[] tempColors = new ColorEx[ 16 ];
                            int dstBpp = PixelUtil.GetNumElemBytes( imgData.format );
                            int sx = Utility.Min<int>( width, 4 );
                            int sy = Utility.Min<int>( height, 4 );
                            int destPitchMinus4 = dstPitch - dstBpp * sx;
                            // slices are done individually
                            for ( int z = 0; z < depth; ++z )
                            {
                                // 4x4 blocks in x/y
                                for ( int y = 0; y < height; y += 4 )
                                {
                                    for ( int x = 0; x < width; x += 4 )
                                    {
                                        if ( sourceFormat == PixelFormat.DXT2 ||
                                            sourceFormat == PixelFormat.DXT3 )
                                        {
                                            // explicit alpha
                                            int btRead;
                                            eAlpha = ToStructure<DXTExplicitAlphaBlock>( input, out btRead );
                                            IntPtr row = Memory.PinObject( eAlpha.AlphaRow );
                                            FlipEndian( row, (uint)sizeof( ushort ), 4 );
                                            Memory.UnpinObject( eAlpha.AlphaRow );
                                            UnpackDXTAlpha( eAlpha, tempColors );
                                        }//end if
                                        else if ( sourceFormat == PixelFormat.DXT4 ||
                                            sourceFormat == PixelFormat.DXT5 )
                                        {
                                            // interpolated alpha
                                            int btRead;
                                            iAlpha = ToStructure<DXTInterpolatedAlphaBlock>( input, out btRead );
                                            IntPtr alpha0 = Memory.PinObject( iAlpha.Alpha0 );
                                            IntPtr alpha1 = Memory.PinObject( iAlpha.Alpha1 );
                                            FlipEndian( alpha0, (uint)sizeof( byte ) );
                                            FlipEndian( alpha1, (uint)sizeof( byte ) );
                                            Memory.UnpinObject( alpha0 );
                                            Memory.UnpinObject( alpha1 );
                                            UnpackDXTAlpha( iAlpha, tempColors );
                                        }//end else if
                                        // always read color
                                        col = ToStructure<DXTColorBlock>( input );
                                        IntPtr col0 = Memory.PinObject( col.Color0 );
                                        IntPtr col1 = Memory.PinObject( col.Color1 );
                                        FlipEndian( col0, sizeof( ushort ) );
                                        FlipEndian( col1, sizeof( ushort ) );
                                        UnpackDXTColour( sourceFormat, col, tempColors );

                                        unsafe
                                        {
                                            // write 4x4 block to uncompressed version
                                            for ( int by = 0; by < sy; by++ )
                                            {
                                                for ( int bx = 0; bx < sx; bx++ )
                                                {
                                                    PixelConverter.PackColor( tempColors[ by * 4 + bx ],
                                                        imgData.format, destPtr );
                                                    destPtr = (IntPtr)( (byte*)( destPtr ) + dstBpp );
                                                }//end bx
                                                // advance to next row
                                                destPtr = (IntPtr)( (byte*)( destPtr ) + destPitchMinus4 );
                                            }//end by

                                            // next block. Our dest pointer is 4 lines down
                                            // from where it started
                                            if ( x + 4 >= width )
                                            {
                                                // Jump back to the start of the line
                                                destPtr = (IntPtr)( (byte*)( destPtr ) - destPitchMinus4 );
                                            }//end if
                                            else
                                            {
                                                // Jump back up 4 rows and 4 pixels to the
                                                // right to be at the next block to the right
                                                destPtr = (IntPtr)( (byte*)( destPtr ) - dstPitch * sy + dstBpp * sx );
                                            }
                                        }
                                    }//end for x
                                }//end for y
                            }//end for z
                        }//end if decompressDXT
                        else
                        {
                            // load directly
                            // DDS format lies! sizeOrPitch is not always set for DXT!!
                            int dstSize = PixelUtil.GetMemorySize( width, height, depth, imgData.format );
                            input.Read( destData, 0, dstSize );
                            unsafe
                            {
                                destPtr = (IntPtr)( (byte*)+dstSize );
                            }
                        }
                    }//end if compressed
                    else
                    {
                        // Final data - trim incoming pitch
                        int srcPitch;
                        if ( ( header.flags & DDSD_PITCH ) != 0 )
                        {
                            srcPitch = (int)header.sizeOrPitch / Utility.Max<int>( 1, mip * 2 );
                        }
                        else
                        {
                            // assume same as final pitch
                            srcPitch = dstPitch;
                        }
                        Utilities.Contract.Requires( dstPitch <= srcPitch );

                        long srcAdvance = (long)srcPitch - (long)dstPitch;
                        int posRead = 0;
                        unsafe
                        {
                            for ( int z = 0; z < imgData.depth; z++ )
                            {
                                for ( int y = 0; y < imgData.height; y++ )
                                {
                                    posRead += input.Read( destData, posRead, dstPitch );
                                    if ( srcAdvance > 0 )
                                        input.Seek( srcAdvance, SeekOrigin.Current );

                                    destPtr = (IntPtr)( (byte*)+dstPitch );
                                }
                            }
                        }
                    }
                }

                /// Next mip
                if ( width != 1 ) width /= 2;
                if ( height != 1 ) height /= 2;
                if ( depth != 1 ) depth /= 2;
            }
            Memory.UnpinObject( destPtr );
            output.Write( destData, 0, destData.Length );
            return imgData;
        }
        

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static int SizeOf<T>() where T : struct
        {
            return Marshal.SizeOf( typeof( T ) );
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <returns></returns>
        public static T ToStructure<T>( byte[] data ) where T : struct
        {
            IntPtr ptr = Memory.PinObject( data );
            T ret = (T)Marshal.PtrToStructure( ptr, typeof( T ) );
            Memory.UnpinObject( ptr );
            return ret;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="stream"></param>
        /// <param name="bytesRead"></param>
        /// <returns></returns>
        public static T ToStructure<T>( Stream stream, out int bytesRead )
        {
            int startPos = (int)stream.Position;
            T t = ToStructure<T>( stream );
            bytesRead = (int)stream.Position - startPos;
            return t;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static T ToStructure<T>( Stream stream )
        {
            int sizeToRead = Marshal.SizeOf( typeof( T ) );
            Utilities.Contract.Requires( sizeToRead < ( stream.Length - stream.Position ), "structure exceeds the size of the stream." );
            byte[] data = new byte[ sizeToRead  ];
            stream.Read( data, 0, data.Length );
            IntPtr ptr = Memory.PinObject( data );
            T ret = (T)Marshal.PtrToStructure( ptr, typeof( T ) );
            Memory.UnpinObject( ptr );
            return ret;
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
            ImageData imgData = codecData as ImageData;

            if ( imgData != null )
            {
                // Check size for cube map faces
                bool isCubeMap = ( imgData.size == Image.CalculateSize( imgData.numMipMaps, 6, imgData.width, imgData.height, imgData.depth, imgData.format ) );

                // Establish texture attributes
                bool isVolume = imgData.depth > 1;
                bool isFloat32r = imgData.format == PixelFormat.FLOAT32_R;
                bool hasAlpha = false;
                bool notImplemented = false;

                string notImplementedString = string.Empty;

                // Check for all the 'not implemented' conditions
                if ( imgData.numMipMaps != 0 )
                {
                    // No mip map functionality yet
                    notImplemented = true;
                    notImplementedString += " mipmaps";
                }

                if ( isVolume && ( imgData.width != imgData.height ) )
                {
                    // Square textures only
                    notImplemented = true;
                    notImplementedString += " non square textures";
                }

                int size = 1;
                while ( size < imgData.width )
                {
                    size <<= 1;
                }

                if ( size != imgData.width )
                {
                    // Power two textures only
                    notImplemented = true;
                    notImplementedString += " non power two textures";
                }

                switch ( imgData.format )
                {
                    case PixelFormat.A8R8G8B8:
                    case PixelFormat.X8R8G8B8:
                    case PixelFormat.R8G8B8:
                    case PixelFormat.FLOAT32_R:
                        break;
                    default:
                        // No crazy FOURCC or 565 et al. file formats at this stage
                        notImplemented = true;
                        notImplementedString = " unsupported pixel format";
                        break;
                }

                // Except if any 'not implemented' conditions were met
                if ( notImplemented )
                {
                    throw new AxiomException( "DDS encoding for" + notImplementedString + " not supported, DDSCodec.EncodeToFile" );
                }
                else
                {
                    // Build header and write to disk

                    // Variables for some DDS header flags
                    uint ddsHeaderFlags = 0;
                    uint ddsHeaderRgbBits = 0;
                    uint ddsHeaderSizeOrPitch = 0;
                    uint ddsHeaderCaps1 = 0;
                    uint ddsHeaderCaps2 = 0;
                    uint ddsMagic = DDS_MAGIC;

                    // Initalise the header flags
                    ddsHeaderFlags = ( isVolume ) ? DDSD_CAPS | DDSD_WIDTH | DDSD_HEIGHT | DDSD_DEPTH | DDSD_PIXELFORMAT :
                        DDSD_CAPS | DDSD_WIDTH | DDSD_HEIGHT | DDSD_PIXELFORMAT;	

                    switch ( imgData.format )
                    {
                        case PixelFormat.A8R8G8B8:
                            ddsHeaderRgbBits = 8 * 4;
                            hasAlpha = true;
                            break;
                        case PixelFormat.X8R8G8B8:
                            ddsHeaderRgbBits = 8 * 4;
                            break;
                        case PixelFormat.R8G8B8:
                            ddsHeaderRgbBits = 8 * 3;
                            break;
                        case PixelFormat.FLOAT32_R:
                            ddsHeaderRgbBits = 32;
                            break;
                        default:
                            ddsHeaderRgbBits = 0;
                            break;
                    }

                    // Initalize the SizeOrPitch flags (power two textures for now)
                    ddsHeaderSizeOrPitch = ddsHeaderRgbBits * (uint)imgData.width;

                    // Initalize the caps flags
                    ddsHeaderCaps1 = ( isVolume || isCubeMap ) ? DDSCAPS_COMPLEX | DDSCAPS_TEXTURE : DDSCAPS_TEXTURE;

                    if ( isVolume )
                    {
                        ddsHeaderCaps2 = DDSCAPS2_VOLUME;
                    }
                    else if ( isCubeMap )
                    {
                        ddsHeaderCaps2 = DDSCAPS2_CUBEMAP |
                    DDSCAPS2_CUBEMAP_POSITIVEX | DDSCAPS2_CUBEMAP_NEGATIVEX |
                    DDSCAPS2_CUBEMAP_POSITIVEY | DDSCAPS2_CUBEMAP_NEGATIVEY |
                    DDSCAPS2_CUBEMAP_POSITIVEZ | DDSCAPS2_CUBEMAP_NEGATIVEZ;
                    }

                    // Populate the DDS header information
                    DDSHeader ddsHeader = new DDSHeader();
                    ddsHeader.reserved1 = new uint[ 11 ];
                    ddsHeader.size = DDS_HEADER_SIZE;
                    ddsHeader.flags = ddsHeaderFlags;
                    ddsHeader.width = (uint)imgData.width;
                    ddsHeader.height = (uint)imgData.height;
                    ddsHeader.depth = isVolume ? (uint)imgData.depth : 0;
                    ddsHeader.depth = isCubeMap ? 6 : ddsHeader.depth;
                    ddsHeader.mipMapCount = 0;
                    ddsHeader.sizeOrPitch = ddsHeaderSizeOrPitch;

                    for ( int reserved1 = 0; reserved1 < 11; reserved1++ )
                    {
                        ddsHeader.reserved1[ reserved1 ] = 0;
                    }
                    ddsHeader.reserved2 = 0;

                    ddsHeader.pixelFormat = new DDSPixelFormat();
                    ddsHeader.pixelFormat.size = DDS_PIXELFORMAT_SIZE;
                    ddsHeader.pixelFormat.flags = hasAlpha ? DDPF_RGB | DDPF_ALPHAPIXELS : DDPF_RGB;
                    ddsHeader.pixelFormat.flags = isFloat32r ? DDPF_FOURCC : ddsHeader.pixelFormat.flags;
                    ddsHeader.pixelFormat.fourCC = isFloat32r ? D3DFMT_R32F : 0;
                    ddsHeader.pixelFormat.rgbBits = ddsHeaderRgbBits;

                    ddsHeader.pixelFormat.alphaMask = ( hasAlpha ) ? 0xFF000000 : 0x00000000;
                    ddsHeader.pixelFormat.alphaMask = ( isFloat32r ) ? 0x00000000 : ddsHeader.pixelFormat.alphaMask;
                    ddsHeader.pixelFormat.redMask = ( isFloat32r ) ? 0xFFFFFFFF : 0x00FF0000;
                    ddsHeader.pixelFormat.greenMask = ( isFloat32r ) ? (uint)0x00000000 : 0x0000FF00;
                    ddsHeader.pixelFormat.blueMask = ( isFloat32r ) ? (uint)0x00000000 : 0x000000FF;

                    ddsHeader.caps.caps1 = ddsHeaderCaps1;
                    ddsHeader.caps.caps2 = ddsHeaderCaps2;
                    ddsHeader.caps.reserved = new uint[ 2 ];
                    ddsHeader.caps.reserved[ 0 ] = 0;
                    ddsHeader.caps.reserved[ 1 ] = 0;

                    //swap endian
                    unsafe
                    {
                        IntPtr magic = Memory.PinObject( ddsMagic );
                        FlipEndian( magic, sizeof( uint ), 1 );
                        int headerSize = Marshal.SizeOf( typeof( DDSHeader ) );
                        IntPtr header = Marshal.AllocHGlobal( headerSize );///Memory.PinObject( ddsHeader );
                        FlipEndian( header, 4, (uint)Marshal.SizeOf( typeof( DDSHeader ) ) / 4 );

                        // Write the file
                        if ( File.Exists( filename ) )
                            File.Delete( filename );

                        FileStream fs = new FileStream( filename, FileMode.CreateNew );
                        BinaryWriter bw = new BinaryWriter( fs );
                        bw.Write( ddsMagic );
                        //while ( ( data = Marshal.ReadByte( header ) ) != 0 )
                        int sz = Marshal.SizeOf( typeof( DDSHeader ) );
                        for ( int i = 0; i < sz; i++ )
                        {
                            bw.Write( Marshal.ReadInt32(header) );
                        }
                        byte[] buffer = new byte[ (int)input.Length ];
                        for ( int i = 0; i < input.Length; i += 4 )
                        {
                            bw.Write( BitConverter.ToUInt32( buffer, i ) );
                        }
                        bw.Flush();
                        fs.Flush();
                        bw.Close();
                        fs.Close();
                    }
                }
            }
        }

        public String MagicNumberToFileExt( char magicNumber, UInt32 maxbytes )
        {
            return String.Empty;
        }

        #endregion
    }
}
