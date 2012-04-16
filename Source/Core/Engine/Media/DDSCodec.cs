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

using Axiom.Core;
using Axiom.CrossPlatform;
using Axiom.Graphics;
using Axiom.Math;
using Axiom.Utilities;

#endregion Namespace Declarations

namespace Axiom.Media
{
	/// <summary>
	///   Codec specialized in loading DDS (Direct Draw Surface) images.
	/// </summary>
	/// <remarks>
	///   We implement our own codec here since we need to be able to keep DXT data compressed if the card supports it.
	/// </remarks>
	public class DDSCodec : ImageCodec
	{
		#region Nested Structures

		private struct DDSPixelFormat
		{
			public int size;
			public int flags;
			public int fourCC;
			public int rgbBits;
			public int redMask;
			public int greenMask;
			public int blueMask;
			public int alphaMask;

			internal static DDSPixelFormat Read( BinaryReader br )
			{
				var pf = new DDSPixelFormat();

				pf.size = br.ReadInt32();
				pf.flags = br.ReadInt32();
				pf.fourCC = br.ReadInt32();
				pf.rgbBits = br.ReadInt32();
				pf.redMask = br.ReadInt32();
				pf.greenMask = br.ReadInt32();
				pf.blueMask = br.ReadInt32();
				pf.alphaMask = br.ReadInt32();

				return pf;
			}

			internal void Write( BinaryWriter br )
			{
				br.Write( this.size );
				br.Write( this.flags );
				br.Write( this.fourCC );
				br.Write( this.rgbBits );
				br.Write( this.redMask );
				br.Write( this.greenMask );
				br.Write( this.blueMask );
				br.Write( this.alphaMask );
			}
		};

		private struct DDSCaps
		{
			public int caps1;
			public int caps2;
			public int[] reserved; //length = 2

			internal static DDSCaps Read( BinaryReader br )
			{
				var caps = new DDSCaps();

				caps.caps1 = br.ReadInt32();
				caps.caps2 = br.ReadInt32();

				caps.reserved = new int[ 2 ];
				caps.reserved[ 0 ] = br.ReadInt32();
				caps.reserved[ 1 ] = br.ReadInt32();

				return caps;
			}

			internal void Write( BinaryWriter br )
			{
				br.Write( this.caps1 );
				br.Write( this.caps2 );
				br.Write( this.reserved[ 0 ] );
				br.Write( this.reserved[ 1 ] );
			}
		};

		/// <summary>
		///   Main header, note preceded by 'DDS '
		/// </summary>
		private struct DDSHeader
		{
			public int size;
			public int flags;
			public int height;
			public int width;
			public int sizeOrPitch;
			public int depth;
			public int mipMapCount;
			public int[] reserved1; // length = 11
			public DDSPixelFormat pixelFormat;
			public DDSCaps caps;
			public int reserved2;

			internal static DDSHeader Read( BinaryReader br )
			{
				var h = new DDSHeader();

				h.size = br.ReadInt32();
				h.flags = br.ReadInt32();
				h.height = br.ReadInt32();
				h.width = br.ReadInt32();
				h.sizeOrPitch = br.ReadInt32();
				h.depth = br.ReadInt32();
				h.mipMapCount = br.ReadInt32();

				h.reserved1 = new int[ 11 ];
				for ( var i = 0; i < 11; ++i )
				{
					h.reserved1[ i ] = br.ReadInt32();
				}

				h.pixelFormat = DDSPixelFormat.Read( br );
				h.caps = DDSCaps.Read( br );

				h.reserved2 = br.ReadInt32();

				return h;
			}

			internal void Write( BinaryWriter br )
			{
				br.Write( this.size );
				br.Write( this.flags );
				br.Write( this.height );
				br.Write( this.width );
				br.Write( this.sizeOrPitch );
				br.Write( this.depth );
				br.Write( this.mipMapCount );

				foreach ( var cur in this.reserved1 )
				{
					br.Write( cur );
				}

				this.pixelFormat.Write( br );
				this.caps.Write( br );

				br.Write( this.reserved2 );
			}
		};

		/// <summary>
		///   An 8-byte DXT color block, represents a 4x4 texel area. Used by all DXT formats
		/// </summary>
		private struct DXTColorBlock
		{
			// 2 colour ranges
			public ushort colour_0;
			public ushort colour_1;
			// 16 2-bit indexes, each byte here is one row
			public byte[] indexRow; // length = 4

			internal static DXTColorBlock Read( BinaryReader br )
			{
				var cBlock = new DXTColorBlock();

				cBlock.colour_0 = br.ReadUInt16();
				cBlock.colour_1 = br.ReadUInt16();

				cBlock.indexRow = br.ReadBytes( 4 );

				return cBlock;
			}
		};

		/// <summary>
		///   An 8-byte DXT explicit alpha block, represents a 4x4 texel area. Used by DXT2/3
		/// </summary>
		private struct DXTExplicitAlphaBlock
		{
			// 16 4-bit values, each 16-bit value is one row
			public ushort[] alphaRow; // length = 4

			internal static DXTExplicitAlphaBlock Read( BinaryReader br )
			{
				var block = new DXTExplicitAlphaBlock();

				block.alphaRow = new ushort[ 4 ];
				for ( var i = 0; i < 4; ++i )
				{
					block.alphaRow[ i ] = br.ReadUInt16();
				}

				return block;
			}
		};

		/// <summary>
		///   An 8-byte DXT interpolated alpha block, represents a 4x4 texel area. Used by DXT4/5
		/// </summary>
		private struct DXTInterpolatedAlphaBlock
		{
			// 2 alpha ranges
			public byte alpha_0;
			public byte alpha_1;
			// 16 3-bit indexes. Unfortunately 3 bits doesn't map too well to row bytes
			// so just stored raw
			public byte[] indexes; // length = 6

			internal static DXTInterpolatedAlphaBlock Read( BinaryReader br )
			{
				var block = new DXTInterpolatedAlphaBlock();

				block.alpha_0 = br.ReadByte();
				block.alpha_1 = br.ReadByte();
				block.indexes = br.ReadBytes( 6 );

				return block;
			}
		};

		#endregion Nested Structures

		#region Constants

		private readonly int DDS_MAGIC = FOURCC( 'D', 'D', 'S', ' ' );
		private const int DDS_PIXELFORMAT_SIZE = 8 * sizeof ( int );
		private const int DDS_CAPS_SIZE = 4 * sizeof ( int );
		private const int DDS_HEADER_SIZE = 19 * sizeof ( int ) + DDS_PIXELFORMAT_SIZE + DDS_CAPS_SIZE;

		private const int DDSD_CAPS = 0x00000001;
		private const int DDSD_HEIGHT = 0x00000002;
		private const int DDSD_WIDTH = 0x00000004;
		private const int DDSD_PITCH = 0x00000008;
		private const int DDSD_PIXELFORMAT = 0x00001000;
		private const int DDSD_MIPMAPCOUNT = 0x00020000;
		private const int DDSD_LINEARSIZE = 0x00080000;
		private const int DDSD_DEPTH = 0x00800000;
		private const int DDPF_ALPHAPIXELS = 0x00000001;
		private const int DDPF_FOURCC = 0x00000004;
		private const int DDPF_RGB = 0x00000040;
		private const int DDSCAPS_COMPLEX = 0x00000008;
		private const int DDSCAPS_TEXTURE = 0x00001000;
		private const int DDSCAPS_MIPMAP = 0x00400000;
		private const int DDSCAPS2_CUBEMAP = 0x00000200;
		private const int DDSCAPS2_CUBEMAP_POSITIVEX = 0x00000400;
		private const int DDSCAPS2_CUBEMAP_NEGATIVEX = 0x00000800;
		private const int DDSCAPS2_CUBEMAP_POSITIVEY = 0x00001000;
		private const int DDSCAPS2_CUBEMAP_NEGATIVEY = 0x00002000;
		private const int DDSCAPS2_CUBEMAP_POSITIVEZ = 0x00004000;
		private const int DDSCAPS2_CUBEMAP_NEGATIVEZ = 0x00008000;
		private const int DDSCAPS2_VOLUME = 0x00200000;

		// Special FourCC codes
		private const int D3DFMT_R16F = 111;
		private const int D3DFMT_G16R16F = 112;
		private const int D3DFMT_A16B16G16R16F = 113;
		private const int D3DFMT_R32F = 114;
		private const int D3DFMT_G32R32F = 115;
		private const int D3DFMT_A32B32G32R32F = 116;

		#endregion Constants

		/// <summary>
		///   Single registered codec instance
		/// </summary>
		private static DDSCodec _instance;

		/// <summary>
		///   Returns that this codec handles dds files.
		/// </summary>
		public override string Type
		{
			get
			{
				return "dds";
			}
		}

		[OgreVersion( 1, 7, 2, "FOURCC precompiler define" )]
		private static int FOURCC( char c0, char c1, char c2, char c3 )
		{
			return ( c0 | ( c1 << 8 ) | ( c2 << 16 ) | ( c3 << 24 ) );
		}

		/// <summary>
		///   Static method to startup and register the DDS codec
		/// </summary>
		[OgreVersion( 1, 7, 2, "Original name was startup" )]
		public static void Initialize()
		{
			if ( _instance == null )
			{
				LogManager.Instance.Write( "DDS codec registering" );
				_instance = new DDSCodec();
				CodecManager.Instance.RegisterCodec( _instance );
			}
		}

		/// <summary>
		///   Static method to shutdown and unregister the DDS codec
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public static void Shutdown()
		{
			if ( _instance != null )
			{
				CodecManager.Instance.UnregisterCodec( _instance );
				_instance = null;
			}
		}

		/// <see cref="Axiom.Media.Codec.Encode" />
		[OgreVersion( 1, 7, 2 )]
		public override Stream Encode( Stream input, Codec.CodecData data )
		{
			throw new NotImplementedException( "DDS file encoding is not yet supported." );
		}

		/// <see cref="Axiom.Media.Codec.EncodeToFile" />
		[OgreVersion( 1, 7, 2 )]
		public override void EncodeToFile( Stream input, string outFileName, Codec.CodecData data )
		{
			// Unwrap codecDataPtr - data is cleaned by calling function
			var imgData = (ImageData)data;

			// Check size for cube map faces
			var isCubeMap = imgData.size == Image.CalculateSize( imgData.numMipMaps, 6, imgData.width, imgData.height, imgData.depth, imgData.format );

			// Establish texture attributes
			var isVolume = imgData.depth > 1;
			var isFloat32r = imgData.format == PixelFormat.FLOAT32_R;
			var hasAlpha = false;
			var notImplemented = false;
			var notImplementedString = string.Empty;

			// Check for all the 'not implemented' conditions
			if ( imgData.numMipMaps != 0 )
			{
				// No mip map functionality yet
				notImplemented = true;
				notImplementedString += " mipmaps";
			}

			if ( ( isVolume == true ) && ( imgData.width != imgData.height ) )
			{
				// Square textures only
				notImplemented = true;
				notImplementedString += " non square textures";
			}

			var size = 1;
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
				throw new NotImplementedException( string.Format( "DDS encoding for{0} not supported", notImplementedString ) );
			}
			else
			{
				// Build header and write to disk

				// Variables for some DDS header flags
				var ddsHeaderFlags = 0;
				var ddsHeaderRgbBits = 0;
				var ddsHeaderSizeOrPitch = 0;
				var ddsHeaderCaps1 = 0;
				var ddsHeaderCaps2 = 0;
				var ddsMagic = this.DDS_MAGIC;

				// Initalise the header flags
				ddsHeaderFlags = ( isVolume ) ? DDSD_CAPS | DDSD_WIDTH | DDSD_HEIGHT | DDSD_DEPTH | DDSD_PIXELFORMAT : DDSD_CAPS | DDSD_WIDTH | DDSD_HEIGHT | DDSD_PIXELFORMAT;

				// Initalise the rgbBits flags
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
				;

				// Initalise the SizeOrPitch flags (power two textures for now)
				ddsHeaderSizeOrPitch = ddsHeaderRgbBits * imgData.width;

				// Initalise the caps flags
				ddsHeaderCaps1 = ( isVolume || isCubeMap ) ? DDSCAPS_COMPLEX | DDSCAPS_TEXTURE : DDSCAPS_TEXTURE;
				if ( isVolume )
				{
					ddsHeaderCaps2 = DDSCAPS2_VOLUME;
				}
				else if ( isCubeMap )
				{
					ddsHeaderCaps2 = DDSCAPS2_CUBEMAP | DDSCAPS2_CUBEMAP_POSITIVEX | DDSCAPS2_CUBEMAP_NEGATIVEX | DDSCAPS2_CUBEMAP_POSITIVEY | DDSCAPS2_CUBEMAP_NEGATIVEY | DDSCAPS2_CUBEMAP_POSITIVEZ | DDSCAPS2_CUBEMAP_NEGATIVEZ;
				}

				// Populate the DDS header information
				var ddsHeader = new DDSHeader();
				ddsHeader.size = DDS_HEADER_SIZE;
				ddsHeader.flags = ddsHeaderFlags;
				ddsHeader.width = imgData.width;
				ddsHeader.height = imgData.height;
				ddsHeader.depth = isVolume ? imgData.depth : 0;
				ddsHeader.depth = isCubeMap ? 6 : ddsHeader.depth;
				ddsHeader.mipMapCount = 0;
				ddsHeader.sizeOrPitch = ddsHeaderSizeOrPitch;
				ddsHeader.reserved1 = new int[ 11 ];

				ddsHeader.reserved2 = 0;
				ddsHeader.pixelFormat.size = DDS_PIXELFORMAT_SIZE;
				ddsHeader.pixelFormat.flags = ( hasAlpha ) ? DDPF_RGB | DDPF_ALPHAPIXELS : DDPF_RGB;
				ddsHeader.pixelFormat.flags = ( isFloat32r ) ? DDPF_FOURCC : ddsHeader.pixelFormat.flags;
				ddsHeader.pixelFormat.fourCC = ( isFloat32r ) ? D3DFMT_R32F : 0;
				ddsHeader.pixelFormat.rgbBits = ddsHeaderRgbBits;

				ddsHeader.pixelFormat.alphaMask = hasAlpha ? unchecked( (int)0xFF000000 ) : 0x00000000;
				ddsHeader.pixelFormat.alphaMask = isFloat32r ? 0x00000000 : ddsHeader.pixelFormat.alphaMask;
				ddsHeader.pixelFormat.redMask = isFloat32r ? unchecked( (int)0xFFFFFFFF ) : 0x00FF0000;
				ddsHeader.pixelFormat.greenMask = isFloat32r ? 0x00000000 : 0x0000FF00;
				ddsHeader.pixelFormat.blueMask = isFloat32r ? 0x00000000 : 0x000000FF;

				ddsHeader.caps.caps1 = ddsHeaderCaps1;
				ddsHeader.caps.caps2 = ddsHeaderCaps2;
				ddsHeader.caps.reserved[ 0 ] = 0;
				ddsHeader.caps.reserved[ 1 ] = 0;

				// Swap endian
				using ( var wrap = BufferBase.Wrap( ddsMagic ) )
				{
					_flipEndian( wrap, sizeof ( uint ), 1 );
				}

				using ( var wrap = BufferBase.Wrap( ddsHeader ) )
				{
					_flipEndian( wrap, 4, Memory.SizeOf( typeof ( DDSHeader ) ) / 4 );
				}

				// Write the file
				using ( var br = new BinaryWriter( File.Open( outFileName, FileMode.OpenOrCreate, FileAccess.Write ) ) )
				{
					br.Write( ddsMagic );
					ddsHeader.Write( br );
					// XXX flipEndian on each pixel chunk written unless isFloat32r ?
					var inputData = new byte[ (int)input.Length ];
					input.Read( inputData, 0, inputData.Length );
					br.Write( inputData );
				}
			}
		}

		[OgreVersion( 1, 7, 2 )]
		private PixelFormat _convertFourCCFormat( int fourcc )
		{
			// convert dxt pixel format
			if ( fourcc == FOURCC( 'D', 'X', 'T', '1' ) )
			{
				return PixelFormat.DXT1;
			}

			else if ( fourcc == FOURCC( 'D', 'X', 'T', '2' ) )
			{
				return PixelFormat.DXT2;
			}

			else if ( fourcc == FOURCC( 'D', 'X', 'T', '3' ) )
			{
				return PixelFormat.DXT3;
			}

			else if ( fourcc == FOURCC( 'D', 'X', 'T', '4' ) )
			{
				return PixelFormat.DXT4;
			}

			else if ( fourcc == FOURCC( 'D', 'X', 'T', '5' ) )
			{
				return PixelFormat.DXT5;
			}

			else if ( fourcc == D3DFMT_R16F )
			{
				return PixelFormat.FLOAT16_R;
			}

			else if ( fourcc == D3DFMT_G16R16F )
			{
				return PixelFormat.FLOAT16_GR;
			}

			else if ( fourcc == D3DFMT_A16B16G16R16F )
			{
				return PixelFormat.FLOAT16_RGBA;
			}

			else if ( fourcc == D3DFMT_R32F )
			{
				return PixelFormat.FLOAT32_R;
			}

			else if ( fourcc == D3DFMT_G32R32F )
			{
				return PixelFormat.FLOAT32_GR;
			}

			else if ( fourcc == D3DFMT_A32B32G32R32F )
			{
				return PixelFormat.FLOAT32_RGBA;
			}

				// We could support 3Dc here, but only ATI cards support it, not nVidia
			else
			{
				throw new AxiomException( "Unsupported FourCC format found in DDS file" );
			}
		}

		[OgreVersion( 1, 7, 2 )]
		private PixelFormat _convertPixelFormat( int rgbBits, int rMask, int gMask, int bMask, int aMask )
		{
			// General search through pixel formats
			for ( var i = (int)PixelFormat.Unknown + 1; i < (int)PixelFormat.Count; ++i )
			{
				var pf = (PixelFormat)i;
				if ( PixelUtil.GetNumElemBits( pf ) == rgbBits )
				{
					var testMasks = PixelUtil.GetBitMasks( pf );
					var testBits = PixelUtil.GetBitDepths( pf );

					if ( testMasks[ 0 ] == rMask && testMasks[ 1 ] == gMask && testMasks[ 2 ] == bMask && // for alpha, deal with 'X8' formats by checking bit counts
					     ( testMasks[ 3 ] == aMask || ( aMask == 0 && testBits[ 3 ] == 0 ) ) )
					{
						return pf;
					}
				}
			}

			throw new AxiomException( "Cannot determine pixel format" );
		}

		/// <summary>
		///   Unpack DXT colors into array of 16 color values
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		private void _unpackDXTColor( PixelFormat pf, DXTColorBlock block, ColorEx[] pCol )
		{
			// Note - we assume all values have already been endian swapped

			// Colour lookup table
			var derivedColours = new ColorEx[ 4 ];

			using ( var src = BufferBase.Wrap( block.colour_0 ) )
			{
				derivedColours[ 0 ] = PixelConverter.UnpackColor( PixelFormat.R5G6B5, src );
			}

			using ( var src = BufferBase.Wrap( block.colour_1 ) )
			{
				derivedColours[ 1 ] = PixelConverter.UnpackColor( PixelFormat.R5G6B5, src );
			}

			if ( pf == PixelFormat.DXT1 && block.colour_0 <= block.colour_1 )
			{
				// 1-bit alpha

				// one intermediate colour, half way between the other two
				derivedColours[ 2 ] = ( derivedColours[ 0 ] + derivedColours[ 1 ] ) / 2;
				// transparent colour
				derivedColours[ 3 ] = new ColorEx( 0, 0, 0, 0 );
			}
			else
			{
				// first interpolated colour, 1/3 of the way along
				derivedColours[ 2 ] = ( derivedColours[ 0 ] * 2 + derivedColours[ 1 ] ) / 3;
				// second interpolated colour, 2/3 of the way along
				derivedColours[ 3 ] = ( derivedColours[ 0 ] + derivedColours[ 1 ] * 2 ) / 3;
			}

			// Process 4x4 block of texels
			for ( var row = 0; row < 4; ++row )
			{
				for ( var x = 0; x < 4; ++x )
				{
					// LSB come first
					var colIdx = (byte)block.indexRow[ row ] >> ( x * 2 ) & 0x3;
					if ( pf == PixelFormat.DXT1 )
					{
						// Overwrite entire colour
						pCol[ ( row * 4 ) + x ] = derivedColours[ colIdx ];
					}
					else
					{
						// alpha has already been read (alpha precedes colour)
						var col = pCol[ ( row * 4 ) + x ];
						col.r = derivedColours[ colIdx ].r;
						col.g = derivedColours[ colIdx ].g;
						col.b = derivedColours[ colIdx ].b;
					}
				}
			}
		}

		/// <summary>
		///   Unpack DXT alphas into array of 16 color values
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		private void _unpackDXTAlpha( DXTExplicitAlphaBlock block, ColorEx[] pCol )
		{
			// Note - we assume all values have already been endian swapped

			// This is an explicit alpha block, 4 bits per pixel, LSB first
			for ( var row = 0; row < 4; ++row )
			{
				for ( var x = 0; x < 4; ++x )
				{
					// Shift and mask off to 4 bits
					var val = (byte)block.alphaRow[ row ] >> ( x * 4 ) & 0xF;
					// Convert to [0,1]
					pCol[ row * 4 + x ].a = (Real)val / (Real)0xF;
				}
			}
		}

		/// <summary>
		///   Unpack DXT alphas into array of 16 colour values
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		private void _unpackDXTAlpha( DXTInterpolatedAlphaBlock block, ColorEx[] pCol )
		{
			// 8 derived alpha values to be indexed
			var derivedAlphas = new Real[ 8 ];

			// Explicit extremes
			derivedAlphas[ 0 ] = block.alpha_0 / (Real)0xFF;
			derivedAlphas[ 1 ] = block.alpha_1 / (Real)0xFF;

			if ( block.alpha_0 <= block.alpha_1 )
			{
				// 4 interpolated alphas, plus zero and one			
				// full range including extremes at [0] and [5]
				// we want to fill in [1] through [4] at weights ranging
				// from 1/5 to 4/5
				Real denom = 1.0f / 5.0f;
				for ( var i = 0; i < 4; ++i )
				{
					var factor0 = ( 4 - i ) * denom;
					var factor1 = ( i + 1 ) * denom;
					derivedAlphas[ i + 2 ] = ( factor0 * block.alpha_0 ) + ( factor1 * block.alpha_1 );
				}
				derivedAlphas[ 6 ] = 0.0f;
				derivedAlphas[ 7 ] = 1.0f;
			}
			else
			{
				// 6 interpolated alphas
				// full range including extremes at [0] and [7]
				// we want to fill in [1] through [6] at weights ranging
				// from 1/7 to 6/7
				Real denom = 1.0f / 7.0f;
				for ( var i = 0; i < 6; ++i )
				{
					var factor0 = ( 6 - i ) * denom;
					var factor1 = ( i + 1 ) * denom;
					derivedAlphas[ i + 2 ] = ( factor0 * block.alpha_0 ) + ( factor1 * block.alpha_1 );
				}
			}

			// Ok, now we've built the reference values, process the indexes
			for ( var i = 0; i < 16; ++i )
			{
				var baseByte = ( i * 3 ) / 8;
				var baseBit = ( i * 3 ) % 8;
				var bits = (byte)block.indexes[ baseByte ] >> baseBit & 0x7;
				// do we need to stitch in next byte too?
				if ( baseBit > 5 )
				{
					var extraBits = (byte)( ( block.indexes[ baseByte + 1 ] << ( 8 - baseBit ) ) & 0xFF );
					bits |= extraBits & 0x7;
				}
				pCol[ i ].a = derivedAlphas[ bits ];
			}
		}

		/// <see cref="Axiom.Media.Codec.Decode" />
		[OgreVersion( 1, 7, 2 )]
		public override Codec.DecodeResult Decode( Stream input )
		{
			using ( var br = new BinaryReader( input ) )
			{
				// Read 4 character code
				var fileType = br.ReadInt32();
				using ( var wrap = BufferBase.Wrap( fileType ) )
				{
					_flipEndian( wrap, sizeof ( uint ), 1 );
				}

				if ( FOURCC( 'D', 'D', 'S', ' ' ) != fileType )
				{
					throw new AxiomException( "This is not a DDS file!" );
				}

				// Read header in full
				var header = DDSHeader.Read( br );

				// Endian flip if required, all 32-bit values
				using ( var wrap = BufferBase.Wrap( header ) )
				{
					_flipEndian( wrap, 4, Memory.SizeOf( typeof ( DDSHeader ) ) / 4 );
				}

				// Check some sizes
				if ( header.size != DDS_HEADER_SIZE )
				{
					throw new AxiomException( "DDS header size mismatch!" );
				}

				if ( header.pixelFormat.size != DDS_PIXELFORMAT_SIZE )
				{
					throw new AxiomException( "DDS header size mismatch!" );
				}

				var imgData = new ImageData();

				imgData.depth = 1; // (deal with volume later)
				imgData.width = header.width;
				imgData.height = header.height;
				var numFaces = 1; // assume one face until we know otherwise

				if ( ( header.caps.caps1 & DDSCAPS_MIPMAP ) != 0 )
				{
					imgData.numMipMaps = header.mipMapCount - 1;
				}
				else
				{
					imgData.numMipMaps = 0;
				}

				imgData.flags = 0;

				var decompressDXT = false;
				// Figure out basic image type
				if ( ( header.caps.caps2 & DDSCAPS2_CUBEMAP ) != 0 )
				{
					imgData.flags |= ImageFlags.CubeMap;
					numFaces = 6;
				}
				else if ( ( header.caps.caps2 & DDSCAPS2_VOLUME ) != 0 )
				{
					imgData.flags |= ImageFlags.Volume;
					imgData.depth = header.depth;
				}
				// Pixel format
				var sourceFormat = PixelFormat.Unknown;

				if ( ( header.pixelFormat.flags & DDPF_FOURCC ) != 0 )
				{
					sourceFormat = _convertFourCCFormat( header.pixelFormat.fourCC );
				}
				else
				{
					sourceFormat = _convertPixelFormat( header.pixelFormat.rgbBits, header.pixelFormat.redMask, header.pixelFormat.greenMask, header.pixelFormat.blueMask, ( header.pixelFormat.flags & DDPF_ALPHAPIXELS ) != 0 ? header.pixelFormat.alphaMask : 0 );
				}

				if ( PixelUtil.IsCompressed( sourceFormat ) )
				{
					if ( !Root.Instance.RenderSystem.Capabilities.HasCapability( Capabilities.TextureCompressionDXT ) )
					{
						// We'll need to decompress
						decompressDXT = true;
						// Convert format
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
								var block = DXTColorBlock.Read( br );
								using ( var wrap = BufferBase.Wrap( block.colour_0 ) )
								{
									_flipEndian( wrap, sizeof ( ushort ), 1 );
								}

								using ( var wrap = BufferBase.Wrap( block.colour_1 ) )
								{
									_flipEndian( wrap, sizeof ( ushort ), 1 );
								}
								// skip back since we'll need to read this again
								br.BaseStream.Seek( 0 - (long)Memory.SizeOf( typeof ( DXTColorBlock ) ), SeekOrigin.Current );
								// colour_0 <= colour_1 means transparency in DXT1
								if ( block.colour_0 <= block.colour_1 )
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
						}
					}
					else
					{
						// Use original format
						imgData.format = sourceFormat;
						// Keep DXT data compressed
						imgData.flags |= ImageFlags.Compressed;
					}
				}
				else // not compressed
				{
					// Don't test against DDPF_RGB since greyscale DDS doesn't set this
					// just derive any other kind of format
					imgData.format = sourceFormat;
				}

				// Calculate total size from number of mipmaps, faces and size
				imgData.size = Image.CalculateSize( imgData.numMipMaps, numFaces, imgData.width, imgData.height, imgData.depth, imgData.format );

				// Now deal with the data
				var dest = new byte[ imgData.size ];
				var destBuffer = BufferBase.Wrap( dest );

				// all mips for a face, then each face
				for ( var i = 0; i < numFaces; ++i )
				{
					var width = imgData.width;
					var height = imgData.height;
					var depth = imgData.depth;

					for ( var mip = 0; mip <= imgData.numMipMaps; ++mip )
					{
						var dstPitch = width * PixelUtil.GetNumElemBytes( imgData.format );

						if ( PixelUtil.IsCompressed( sourceFormat ) )
						{
							// Compressed data
							if ( decompressDXT )
							{
								DXTColorBlock col;
								DXTInterpolatedAlphaBlock iAlpha;
								DXTExplicitAlphaBlock eAlpha;
								// 4x4 block of decompressed colour
								var tempColours = new ColorEx[ 16 ];
								var destBpp = PixelUtil.GetNumElemBytes( imgData.format );
								var sx = Utility.Min( width, 4 );
								var sy = Utility.Min( height, 4 );
								var destPitchMinus4 = dstPitch - destBpp * sx;
								// slices are done individually
								for ( var z = 0; z < depth; ++z )
								{
									// 4x4 blocks in x/y
									for ( var y = 0; y < height; y += 4 )
									{
										for ( var x = 0; x < width; x += 4 )
										{
											if ( sourceFormat == PixelFormat.DXT2 || sourceFormat == PixelFormat.DXT3 )
											{
												// explicit alpha
												eAlpha = DXTExplicitAlphaBlock.Read( br );
												using ( var wrap = BufferBase.Wrap( eAlpha.alphaRow ) )
												{
													_flipEndian( wrap, sizeof ( ushort ), 4 );
												}
												_unpackDXTAlpha( eAlpha, tempColours );
											}
											else if ( sourceFormat == PixelFormat.DXT4 || sourceFormat == PixelFormat.DXT5 )
											{
												// interpolated alpha
												iAlpha = DXTInterpolatedAlphaBlock.Read( br );
												using ( var wrap = BufferBase.Wrap( iAlpha.alpha_0 ) )
												{
													_flipEndian( wrap, sizeof ( ushort ), 1 );
												}

												using ( var wrap = BufferBase.Wrap( iAlpha.alpha_1 ) )
												{
													_flipEndian( wrap, sizeof ( ushort ), 1 );
												}
												_unpackDXTAlpha( iAlpha, tempColours );
											}
											// always read colour
											col = DXTColorBlock.Read( br );

											using ( var wrap = BufferBase.Wrap( col.colour_0 ) )
											{
												_flipEndian( wrap, sizeof ( ushort ), 1 );
											}

											using ( var wrap = BufferBase.Wrap( col.colour_1 ) )
											{
												_flipEndian( wrap, sizeof ( ushort ), 1 );
											}
											_unpackDXTColor( sourceFormat, col, tempColours );

											// write 4x4 block to uncompressed version
											for ( var by = 0; by < sy; ++by )
											{
												for ( var bx = 0; bx < sx; ++bx )
												{
													PixelConverter.PackColor( tempColours[ by * 4 + bx ], imgData.format, destBuffer );
													destBuffer += destBpp;
												}
												// advance to next row
												destBuffer += destPitchMinus4;
											}
											// next block. Our dest pointer is 4 lines down
											// from where it started
											if ( x + 4 >= width )
											{
												// Jump back to the start of the line
												destBuffer += -destPitchMinus4;
											}
											else
											{
												// Jump back up 4 rows and 4 pixels to the
												// right to be at the next block to the right
												destBuffer += -( dstPitch * sy + destBpp * sx );
											}
										}
									}
								}
							}
							else
							{
								// load directly
								// DDS format lies! sizeOrPitch is not always set for DXT!!
								var dxtSize = PixelUtil.GetMemorySize( width, height, depth, imgData.format );
								using ( var src = BufferBase.Wrap( br.ReadBytes( dxtSize ) ) )
								{
									Memory.Copy( src, destBuffer, dxtSize );
								}
								destBuffer += dxtSize;
							}
						}
						else
						{
							// Final data - trim incoming pitch
							int srcPitch;
							if ( ( header.flags & DDSD_PITCH ) != 0 )
							{
								srcPitch = header.sizeOrPitch / Utility.Max( 1, mip * 2 );
							}
							else
							{
								// assume same as final pitch
								srcPitch = dstPitch;
							}
							Contract.Requires( dstPitch <= srcPitch );
							var srcAdvance = (long)( srcPitch - dstPitch );

							for ( var z = 0; z < imgData.depth; ++z )
							{
								for ( var y = 0; y < imgData.height; ++y )
								{
									using ( var src = BufferBase.Wrap( br.ReadBytes( dstPitch ) ) )
									{
										Memory.Copy( src, destBuffer, dstPitch );
									}

									if ( srcAdvance > 0 )
									{
										br.BaseStream.Seek( srcAdvance, SeekOrigin.Current );
									}

									destBuffer += dstPitch;
								}
							}
						}

						// Next mip
						if ( width != 1 )
						{
							width /= 2;
						}

						if ( height != 1 )
						{
							height /= 2;
						}

						if ( depth != 1 )
						{
							depth /= 2;
						}
					}
				}

				destBuffer.Dispose();
				return new DecodeResult( new MemoryStream( dest ), imgData );
			}
		}

		[OgreVersion( 1, 7, 2 )]
		private void _flipEndian( BufferBase pData, int size, int count )
		{
#if AXIOM_BIG_ENDIAN
            for ( var index = 0; index < count; index++ )
            {
                using ( var data = pData + ( index * size ) )
                    _flipEndian( data, size );
            }
#endif
		}

		[OgreVersion( 1, 7, 2 )]
		private void _flipEndian( BufferBase pData, int size )
		{
#if AXIOM_BIG_ENDIAN
            byte swapByte;
#if !AXIOM_SAFE_ONLY
            unsafe
#endif
            {
                var ptr = pData.ToBytePointer();
                for ( var byteIndex = 0; byteIndex < size / 2; byteIndex++ )
                {
                    swapByte = ptr[ byteIndex ];
                    ptr[ byteIndex ] = ptr[ size - byteIndex - 1 ];
                    ptr[ size - byteIndex - 1 ] = swapByte;
                }
            }
#endif
		}

		/// <see cref="Axiom.Media.Codec.MagicNumberToFileExt" />
		[OgreVersion( 1, 7, 2 )]
		public override string MagicNumberToFileExt( byte[] magicBuf, int maxbytes )
		{
			if ( maxbytes >= sizeof ( int ) )
			{
				var fileType = BitConverter.ToInt32( magicBuf, 0 );
				using ( var data = BufferBase.Wrap( fileType ) )
				{
					_flipEndian( data, sizeof ( int ), 1 );
				}

				if ( this.DDS_MAGIC == fileType )
				{
					return "dds";
				}
			}

			return string.Empty;
		}
	};
}
