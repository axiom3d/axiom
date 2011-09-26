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
//     <id value="$Id: ILImageCodec.cs 1110 2007-09-18 19:45:15Z borrillis $"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.IO;

using Axiom.Core;
using Axiom.CrossPlatform;
using Axiom.Math.Collections;
using Axiom.Media;

using Tao.DevIl;

#endregion Namespace Declarations

namespace Axiom.Plugins.DevILCodecs
{
	internal class ILUtil
	{
		/// <summary>
		/// Structure that encapsulates a devIL image format definition
		/// </summary>
		internal sealed class ILFormat
		{
			private int _channels;
			/// <summary>
			/// Number of channels, usually 3 or 4
			/// </summary>
			public int Channels
			{
				get
				{
					return _channels;
				}
			}

			private int _format = -1;
			/// <summary>
			/// IL_RGBA,IL_BGRA,IL_DXTx, ...
			/// </summary>
			public int Format
			{
				get
				{
					return _format;
				}
			}

			private int _type = -1;
			/// <summary>
			/// IL_UNSIGNED_BYTE, IL_UNSIGNED_SHORT, ... may be -1 for compressed formats
			/// </summary>
			public int Type
			{
				get
				{
					return _type;
				}
			}

			/// <summary>
			/// Construct an invalidated ILFormat structure
			/// </summary>
			public ILFormat()
			{
			}

			/// <summary>
			/// Construct a ILFormat from parameters
			/// </summary>
			/// <param name="channels"></param>
			/// <param name="format"></param>
			public ILFormat( int channels, int format )
				: this( channels, format, -1 )
			{
			}

			/// <summary>
			/// Construct a ILFormat from parameters
			/// </summary>
			/// <param name="channels"></param>
			/// <param name="format"></param>
			/// <param name="type"></param>
			public ILFormat( int channels, int format, int type )
			{
				_channels = channels;
				_format = format;
				_type = type;
			}

			/// <summary>
			/// Return wether this structure represents a valid DevIL format
			/// </summary>
			public bool IsValid
			{
				get
				{
					return ( _format != -1 );
				}
			}

		}

		/// <summary>
		///    Converts a PixelFormat enum to a pair with DevIL format enum and bytesPerPixel.
		/// </summary>
		/// <param name="format"></param>
		/// <returns></returns>
		static public ILFormat Convert( PixelFormat format )
		{
			switch ( format )
			{
				case PixelFormat.BYTE_L:
				case PixelFormat.BYTE_A:
					return new ILFormat( 1, Il.IL_LUMINANCE, Il.IL_UNSIGNED_BYTE );
				case PixelFormat.SHORT_L:
					return new ILFormat( 1, Il.IL_LUMINANCE, Il.IL_UNSIGNED_SHORT );
				case PixelFormat.BYTE_LA:
					return new ILFormat( 2, Il.IL_LUMINANCE_ALPHA, Il.IL_UNSIGNED_BYTE );
				case PixelFormat.BYTE_RGB:
					return new ILFormat( 3, Il.IL_RGB, Il.IL_UNSIGNED_BYTE );
				case PixelFormat.BYTE_RGBA:
					return new ILFormat( 4, Il.IL_RGBA, Il.IL_UNSIGNED_BYTE );
				case PixelFormat.BYTE_BGR:
					return new ILFormat( 3, Il.IL_BGR, Il.IL_UNSIGNED_BYTE );
				case PixelFormat.BYTE_BGRA:
					return new ILFormat( 4, Il.IL_BGRA, Il.IL_UNSIGNED_BYTE );
				case PixelFormat.SHORT_RGBA:
					return new ILFormat( 4, Il.IL_RGBA, Il.IL_UNSIGNED_SHORT );
				case PixelFormat.FLOAT32_RGB:
					return new ILFormat( 3, Il.IL_RGB, Il.IL_FLOAT );
				case PixelFormat.FLOAT32_RGBA:
					return new ILFormat( 4, Il.IL_RGBA, Il.IL_FLOAT );
				case PixelFormat.DXT1:
					return new ILFormat( 0, Il.IL_DXT1 );
				case PixelFormat.DXT2:
					return new ILFormat( 0, Il.IL_DXT2 );
				case PixelFormat.DXT3:
					return new ILFormat( 0, Il.IL_DXT3 );
				case PixelFormat.DXT4:
					return new ILFormat( 0, Il.IL_DXT4 );
				case PixelFormat.DXT5:
					return new ILFormat( 0, Il.IL_DXT5 );
				default:
					return new ILFormat();

			}
		}

		/// <summary>
		///    Converts a DevIL format enum to a PixelFormat enum.
		/// </summary>
		/// <param name="format"></param>
		/// <param name="bytesPerPixel"></param>
		/// <returns></returns>
		static public PixelFormat Convert( int imageFormat, int imageType )
		{
			PixelFormat fmt = PixelFormat.Unknown;
			switch ( imageFormat )
			{
				/* Compressed formats -- ignore type */
				case Il.IL_DXT1:
					fmt = PixelFormat.DXT1;
					break;
				case Il.IL_DXT2:
					fmt = PixelFormat.DXT2;
					break;
				case Il.IL_DXT3:
					fmt = PixelFormat.DXT3;
					break;
				case Il.IL_DXT4:
					fmt = PixelFormat.DXT4;
					break;
				case Il.IL_DXT5:
					fmt = PixelFormat.DXT5;
					break;
				/* Normal formats */
				case Il.IL_RGB:
					switch ( imageType )
					{
						case Il.IL_FLOAT:
							fmt = PixelFormat.FLOAT32_RGB;
							break;
						case Il.IL_UNSIGNED_SHORT:
						case Il.IL_SHORT:
							fmt = PixelFormat.SHORT_RGBA;
							break;
						default:
							fmt = PixelFormat.BYTE_RGB;
							break;
					}
					break;
				case Il.IL_BGR:
					switch ( imageType )
					{
						case Il.IL_FLOAT:
							fmt = PixelFormat.FLOAT32_RGB;
							break;
						case Il.IL_UNSIGNED_SHORT:
						case Il.IL_SHORT:
							fmt = PixelFormat.SHORT_RGBA;
							break;
						default:
							fmt = PixelFormat.BYTE_BGR;
							break;
					}
					break;
				case Il.IL_RGBA:
					switch ( imageType )
					{
						case Il.IL_FLOAT:
							fmt = PixelFormat.FLOAT32_RGBA;
							break;
						case Il.IL_UNSIGNED_SHORT:
						case Il.IL_SHORT:
							fmt = PixelFormat.SHORT_RGBA;
							break;
						default:
							fmt = PixelFormat.BYTE_RGBA;
							break;
					}
					break;
				case Il.IL_BGRA:
					switch ( imageType )
					{
						case Il.IL_FLOAT:
							fmt = PixelFormat.FLOAT32_RGBA;
							break;
						case Il.IL_UNSIGNED_SHORT:
						case Il.IL_SHORT:
							fmt = PixelFormat.SHORT_RGBA;
							break;
						default:
							fmt = PixelFormat.BYTE_BGRA;
							break;
					}
					break;
				case Il.IL_LUMINANCE:
					switch ( imageType )
					{
						case Il.IL_BYTE:
						case Il.IL_UNSIGNED_BYTE:
							fmt = PixelFormat.L8;
							break;
						default:
							fmt = PixelFormat.L16;
							break;
					}
					break;
				case Il.IL_LUMINANCE_ALPHA:
					fmt = PixelFormat.BYTE_LA;
					break;
			}
			return fmt;
		}


		static public void ConvertFromIL( PixelBox dst )
		{
			if ( !dst.IsConsecutive )
				throw new Exception( "Destination must currently be consecutive" );

			if ( dst.Width != Il.ilGetInteger( Il.IL_IMAGE_WIDTH ) ||
				dst.Height != Il.ilGetInteger( Il.IL_IMAGE_HEIGHT ) ||
				dst.Depth != Il.ilGetInteger( Il.IL_IMAGE_DEPTH ) )
				throw new Exception( "Destination dimensions must equal IL dimension" );

			int ilfmt = Il.ilGetInteger( Il.IL_IMAGE_FORMAT );
			int iltp = Il.ilGetInteger( Il.IL_IMAGE_TYPE );

			// Check if in-memory format just matches
			// If yes, we can just copy it and save conversion
			ILFormat ifmt = Convert( dst.Format );
			if ( ifmt.Format == ilfmt && ILabs( ifmt.Type ) == ILabs( iltp ) )
			{
			    var size = Il.ilGetInteger( Il.IL_IMAGE_SIZE_OF_DATA );
                Memory.Copy(BufferBase.Wrap(Il.ilGetData(), size), dst.Data, size);
				return;
			}

			// Try if buffer is in a known Axiom format so we can use its conversion routines
			PixelFormat bufFmt = Convert( (int)ilfmt, (int)iltp );
			ifmt = Convert( bufFmt );

			if ( ifmt.Format == ilfmt && ILabs( ifmt.Type ) == ILabs( iltp ) )
                using (var dstbuf = BufferBase.Wrap(Il.ilGetData(), Il.ilGetInteger(Il.IL_IMAGE_SIZE_OF_DATA)))
                {
                    // IL format matches another Axiom format
                    PixelBox src = new PixelBox( dst.Width, dst.Height, dst.Depth, bufFmt, dstbuf );
                    PixelConverter.BulkPixelConversion( src, dst );
                    return;
                }

		    // Thee extremely slow method
			if ( iltp == Il.IL_UNSIGNED_BYTE || iltp == Il.IL_BYTE )
			{
				throw new NotImplementedException( "Cannot convert this DevIL type." );
				//ilToOgreInternal( static_cast<uint8*>( dst.data ), dst.format, (uint8)0x00, (uint8)0x00, (uint8)0x00, (uint8)0xFF );
			}
			else if ( iltp == Il.IL_FLOAT )
			{
				throw new NotImplementedException( "Cannot convert this DevIL type." );
				//ilToOgreInternal( static_cast<uint8*>( dst.data ), dst.format, 0.0f, 0.0f, 0.0f, 1.0f );
			}
			else if ( iltp == Il.IL_SHORT || iltp == Il.IL_UNSIGNED_SHORT )
			{
				throw new NotImplementedException( "Cannot convert this DevIL type." );
				//ilToOgreInternal( static_cast<uint8*>( dst.data ), dst.format, (uint16)0x0000, (uint16)0x0000, (uint16)0x0000, (uint16)0xFFFF );
			}
			else
			{
				throw new Exception( "Cannot convert this DevIL type." );
			}
		}

		static public void ConvertToIL( PixelBox src )
		{
			// ilTexImage http://openil.sourceforge.net/docs/il/f00059.htm
			ILFormat ifmt = Convert( src.Format );
			if ( src.IsConsecutive && ifmt.IsValid )
			{
				// The easy case, the buffer is laid out in memory just like 
				// we want it to be and is in a format DevIL can understand directly
				// We could even save the copy if DevIL would let us
				Il.ilTexImage( src.Width, src.Height, src.Depth, (byte)ifmt.Channels, ifmt.Format, ifmt.Type, src.Data.Pin() );
			    src.Data.UnPin();
			}
            else if (ifmt.IsValid)
            {
                // The format can be understood directly by DevIL. The only 
                // problem is that ilTexImage expects our image data consecutively 
                // so we cannot use that directly.

                // Let DevIL allocate the memory for us, and copy the data consecutively
                // to its memory
                Il.ilTexImage( src.Width, src.Height, src.Depth, (byte)ifmt.Channels, ifmt.Format, ifmt.Type,
                               IntPtr.Zero );
                using (var dstbuf = BufferBase.Wrap(Il.ilGetData(), Il.ilGetInteger(Il.IL_IMAGE_SIZE_OF_DATA)))
                {
                    PixelBox dst = new PixelBox( src.Width, src.Height, src.Depth, src.Format, dstbuf );
                    PixelConverter.BulkPixelConversion( src, dst );
                }
            }
            else
            {
                // Here it gets ugly. We're stuck with a pixel format that DevIL
                // can't do anything with. We will do a bulk pixel conversion and
                // then feed it to DevIL anyway. The problem is finding the best
                // format to convert to.

                // most general format supported by Axiom and DevIL
                PixelFormat fmt = PixelUtil.HasAlpha(src.Format) ? PixelFormat.FLOAT32_RGBA : PixelFormat.FLOAT32_RGB;

                // Make up a pixel format
                // We don't have to consider luminance formats as they have
                // straight conversions to DevIL, just weird permutations of RGBA an LA
                int[] depths = PixelUtil.GetBitDepths(src.Format);

                // Native endian format with all bit depths<8 can safely and quickly be 
                // converted to 24/32 bit
                if (PixelUtil.IsNativeEndian(src.Format) &&
                    depths[0] <= 8 && depths[1] <= 8 && depths[2] <= 8 && depths[3] <= 8)
                {
                    if (PixelUtil.HasAlpha(src.Format))
                    {
                        fmt = PixelFormat.A8R8G8B8;
                    }
                    else
                    {
                        fmt = PixelFormat.R8G8B8;
                    }
                }

                // Let DevIL allocate the memory for us, then do the conversion ourselves
                ifmt = Convert(fmt);
                Il.ilTexImage(src.Width, src.Height, src.Depth, (byte)ifmt.Channels, ifmt.Format, ifmt.Type, IntPtr.Zero); // TAO 2.0
                //Il.ilTexImage( src.Width, src.Height, src.Depth, (byte)ifmt.Channels, ifmt.Format, ifmt.Type, null );
                using (var dstbuf = BufferBase.Wrap(Il.ilGetData(), Il.ilGetInteger(Il.IL_IMAGE_SIZE_OF_DATA)))
                {
                    PixelBox dst = new PixelBox( src.Width, src.Height, src.Depth, fmt, dstbuf );
                    PixelConverter.BulkPixelConversion( src, dst );
                }
            }
		}

		/// Utility function to convert IL data types to UNSIGNED_
		static private int ILabs( int val )
		{
			switch ( val )
			{
				case Il.IL_INT:
					return Il.IL_UNSIGNED_INT;
				case Il.IL_BYTE:
					return Il.IL_UNSIGNED_BYTE;
				case Il.IL_SHORT:
					return Il.IL_UNSIGNED_SHORT;
				default:
					return val;
			}
		}

	}
}
