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

#endregion Namespace Declarations

namespace Axiom.Media
{
	/// <summary>
	/// Codec specialized in loading PVRTC (PowerVR) images.
	/// </summary>
	/// <remarks>
	/// We implement our own codec here since we need to be able to keep PVRTC
	/// data compressed if the card supports it.
	/// </remarks>
	public class PVRTCCodec : ImageCodec
	{
		private const int PVR_TEXTURE_FLAG_TYPE_MASK = 0xff;
		private const uint kPVRTextureFlagTypePVRTC_2 = 24;
		private const uint kPVRTextureFlagTypePVRTC_4 = 25;

		/// <summary>
		/// Single registered codec instance
		/// </summary>
		private static PVRTCCodec _instance;

		private readonly int PVR_MAGIC = FOURCC( 'P', 'V', 'R', '!' );

		[OgreVersion( 1, 7, 2 )]
		public override string Type
		{
			get
			{
				return "pvr";
			}
		}

		[OgreVersion( 1, 7, 2, "FOURCC precompiler define" )]
		private static int FOURCC( char c0, char c1, char c2, char c3 )
		{
			return ( c0 | ( c1 << 8 ) | ( c2 << 16 ) | ( c3 << 24 ) );
		}

		/// <summary>
		/// Static method to startup and register the PVRTC codec
		/// </summary>
		[OgreVersion( 1, 7, 2, "Original name was startup" )]
		public static void Initialize()
		{
			if ( _instance == null )
			{
				LogManager.Instance.Write( "PVRTC codec registering" );
				_instance = new PVRTCCodec();
				CodecManager.Instance.RegisterCodec( _instance );
			}
		}

		/// <summary>
		/// Static method to shutdown and unregister the PVRTC codec
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

		/// <see cref="Axiom.Media.Codec.Encode"/>
		[OgreVersion( 1, 7, 2 )]
		public override Stream Encode( Stream input, CodecData data )
		{
			throw new NotImplementedException( "PVRTC encoding not supported" );
		}

		/// <see cref="Axiom.Media.Codec.EncodeToFile"/>
		[OgreVersion( 1, 7, 2 )]
		public override void EncodeToFile( Stream input, string outFileName, CodecData data )
		{
			throw new NotImplementedException( "PVRTC encoding not supported" );
		}

		/// <see cref="Axiom.Media.Codec.Decode"/>
		[OgreVersion( 1, 7, 2 )]
		public override DecodeResult Decode( Stream input )
		{
			using ( var br = new BinaryReader( input ) )
			{
				int numFaces = 1; // Assume one face until we know otherwise
				var imgData = new ImageData();

				// Read the PVRTC header
				PVRTCTexHeader header = PVRTCTexHeader.Read( br );

				// Get the file type identifier
				int pvrTag = header.pvrTag;

				if ( this.PVR_MAGIC != pvrTag )
				{
					throw new AxiomException( "This is not a PVR file!" );
				}

				// Get format flags
				int flags = header.flags;
				using ( BufferBase wrap = BufferBase.Wrap( flags ) )
				{
					_flipEndian( wrap, sizeof( int ) );
				}
				int formatFlags = flags & PVR_TEXTURE_FLAG_TYPE_MASK;

				int bitmaskAlpha = header.bitmaskAlpha;
				using ( BufferBase wrap = BufferBase.Wrap( bitmaskAlpha ) )
				{
					_flipEndian( wrap, sizeof( int ) );
				}

				if ( formatFlags == kPVRTextureFlagTypePVRTC_4 || formatFlags == kPVRTextureFlagTypePVRTC_2 )
				{
					if ( formatFlags == kPVRTextureFlagTypePVRTC_4 )
					{
						imgData.format = bitmaskAlpha != 0 ? PixelFormat.PVRTC_RGBA4 : PixelFormat.PVRTC_RGB4;
					}
					else if ( formatFlags == kPVRTextureFlagTypePVRTC_2 )
					{
						imgData.format = bitmaskAlpha != 0 ? PixelFormat.PVRTC_RGBA2 : PixelFormat.PVRTC_RGB2;
					}

					imgData.depth = 1;
					imgData.width = header.width;
					imgData.height = header.height;
					imgData.numMipMaps = header.numMipmaps;

					// PVRTC is a compressed format
					imgData.flags |= ImageFlags.Compressed;
				}

				// Calculate total size from number of mipmaps, faces and size
				imgData.size = Image.CalculateSize( imgData.numMipMaps, numFaces, imgData.width, imgData.height, imgData.depth, imgData.format );

				// Now deal with the data
				byte[] dest = br.ReadBytes( imgData.size );
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

		/// <see cref="Axiom.Media.Codec.MagicNumberToFileExt"/>
		[OgreVersion( 1, 7, 2 )]
		public override string MagicNumberToFileExt( byte[] magicNumberBuf, int maxbytes )
		{
			if ( maxbytes >= sizeof( int ) )
			{
				int fileType = BitConverter.ToInt32( magicNumberBuf, 0 );
				using ( BufferBase data = BufferBase.Wrap( fileType ) )
				{
					_flipEndian( data, sizeof( int ), 1 );
				}

				if ( this.PVR_MAGIC == fileType )
				{
					return "pvr";
				}
			}

			return string.Empty;
		}

		#region Nested type: PVRTCTexHeader

		private struct PVRTCTexHeader
		{
			public int bitmaskAlpha;
			public int bitmaskBlue;
			public int bitmaskGreen;
			public int bitmaskRed;
			public int bpp;
			public int dataLength;
			public int flags;
			public int headerLength;
			public int height;
			public int numMipmaps;
			public int numSurfs;
			public int pvrTag;
			public int width;

			internal static PVRTCTexHeader Read( BinaryReader br )
			{
				var h = new PVRTCTexHeader();

				h.headerLength = br.ReadInt32();
				h.height = br.ReadInt32();
				h.width = br.ReadInt32();
				h.numMipmaps = br.ReadInt32();
				h.flags = br.ReadInt32();
				h.dataLength = br.ReadInt32();
				h.bpp = br.ReadInt32();
				h.bitmaskRed = br.ReadInt32();
				h.bitmaskGreen = br.ReadInt32();
				h.bitmaskBlue = br.ReadInt32();
				h.bitmaskAlpha = br.ReadInt32();
				h.pvrTag = br.ReadInt32();
				h.numSurfs = br.ReadInt32();

				return h;
			}
		};

		#endregion
	};
}
