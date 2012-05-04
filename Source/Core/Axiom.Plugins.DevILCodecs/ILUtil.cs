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
using Axiom.Core;
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
			/// <summary>
			/// Number of channels, usually 3 or 4
			/// </summary>
			public int Channels { get; private set; }

			/// <summary>
			/// IL_RGBA,IL_BGRA,IL_DXTx, ...
			/// </summary>
			public int Format { get; private set; }

			/// <summary>
			/// IL_UNSIGNED_BYTE, IL_UNSIGNED_SHORT, ... may be -1 for compressed formats
			/// </summary>
			public int Type { get; private set; }

			/// <summary>
			/// Return wether this structure represents a valid DevIL format
			/// </summary>
			[OgreVersion( 1, 7, 2 )]
			public bool IsValid
			{
				get
				{
					return ( Format != -1 );
				}
			}

			/// <summary>
			/// Construct an invalidated ILFormat structure
			/// </summary>
			public ILFormat()
				: this( 0, -1, -1 )
			{
			}

			/// <summary>
			/// Construct an invalidated ILFormat structure
			/// </summary>
			public ILFormat( int channels )
				: this( channels, -1, -1 )
			{
			}

			/// <summary>
			/// Construct a ILFormat from parameters
			/// </summary>
			public ILFormat( int channels, int format )
				: this( channels, format, -1 )
			{
			}

			/// <summary>
			/// Construct a ILFormat from parameters
			/// </summary>
			[OgreVersion( 1, 7, 2 )]
			public ILFormat( int channels, int format, int type )
			{
				Channels = channels;
				Format = format;
				Type = type;
			}
		};

		/// <summary>
		/// Get Axiom format to which a given IL format can be most optimally converted.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public static PixelFormat Convert( int imageFormat, int imageType )
		{
			var fmt = PixelFormat.Unknown;

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

		/// <summary>
		/// Get IL format that matches a given Axiom format exactly in memory.
		/// </summary>
		/// <remarks>
		/// Returns an invalid ILFormat (IsValid == false) when
		/// there is no IL format that matches this.
		/// </remarks>
		[OgreVersion( 1, 7, 2 )]
		public static ILFormat Convert( PixelFormat format )
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
			;
		}

		/// <summary>
		/// "Packed" helper function for DevIL to Axiom conversion
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		private static void _packI<T>( T r, T g, T b, T a, PixelFormat pf, BufferBase dest )
		{
			var destType = typeof ( T );

			if ( destType == typeof ( byte ) )
			{
				PixelConverter.PackColor( (uint)System.Convert.ChangeType( r, typeof ( uint ) ),
				                          (uint)System.Convert.ChangeType( g, typeof ( uint ) ),
				                          (uint)System.Convert.ChangeType( b, typeof ( uint ) ),
				                          (uint)System.Convert.ChangeType( a, typeof ( uint ) ), pf, dest );
			}
			else if ( destType == typeof ( ushort ) )
			{
				PixelConverter.PackColor( (float)System.Convert.ChangeType( r, destType )/65535.0f,
				                          (float)System.Convert.ChangeType( g, destType )/65535.0f,
				                          (float)System.Convert.ChangeType( b, destType )/65535.0f,
				                          (float)System.Convert.ChangeType( a, destType )/65535.0f, pf, dest );
			}
			else if ( destType == typeof ( float ) )
			{
				PixelConverter.PackColor( (float)System.Convert.ChangeType( r, destType ),
				                          (float)System.Convert.ChangeType( g, destType ),
				                          (float)System.Convert.ChangeType( b, destType ),
				                          (float)System.Convert.ChangeType( a, destType ), pf, dest );
			}
			else
			{
				throw new AxiomException( "Unsupported type!" );
			}
		}

		[OgreVersion( 1, 7, 2 )]
		private static void _ilToAxiomInternal<T>( BufferBase tar, PixelFormat fmt, T r, T g, T b, T a )
		{
			if ( !typeof ( T ).IsPrimitive )
			{
				throw new AxiomException( "Invalid type!" );
			}

			var ilfmt = Il.ilGetInteger( Il.IL_IMAGE_FORMAT );
			var src = Il.ilGetData();
			var srcend = Il.ilGetData().Offset( Il.ilGetInteger( Il.IL_IMAGE_SIZE_OF_DATA ) );
			var elemSize = PixelUtil.GetNumElemBytes( fmt );

			while ( (int)src < (int)srcend )
			{
				using ( var srcBuf = BufferBase.Wrap( src, (int)srcend - (int)src ) )
				{
					var srcPtr = srcBuf as ITypePointer<T>;

					switch ( ilfmt )
					{
						case Il.IL_RGB:
							r = srcPtr[ 0 ];
							g = srcPtr[ 1 ];
							b = srcPtr[ 2 ];
							src = src.Offset( 3 );
							break;

						case Il.IL_BGR:
							b = srcPtr[ 0 ];
							g = srcPtr[ 1 ];
							r = srcPtr[ 2 ];
							src = src.Offset( 3 );
							break;

						case Il.IL_LUMINANCE:
							r = srcPtr[ 0 ];
							g = srcPtr[ 0 ];
							b = srcPtr[ 0 ];
							src = src.Offset( 1 );
							break;

						case Il.IL_LUMINANCE_ALPHA:
							r = srcPtr[ 0 ];
							g = srcPtr[ 0 ];
							b = srcPtr[ 0 ];
							a = srcPtr[ 1 ];
							src = src.Offset( 2 );
							break;

						case Il.IL_RGBA:
							r = srcPtr[ 0 ];
							g = srcPtr[ 1 ];
							b = srcPtr[ 2 ];
							a = srcPtr[ 3 ];
							src = src.Offset( 4 );
							break;

						case Il.IL_BGRA:
							b = srcPtr[ 0 ];
							g = srcPtr[ 1 ];
							r = srcPtr[ 2 ];
							a = srcPtr[ 3 ];
							src = src.Offset( 4 );
							break;

						default:
							return;
					}
					;

					_packI<T>( r, g, b, a, fmt, tar );
					tar += elemSize;
				}
			}
		}

		/// <summary>
		/// Utility function to convert IL data types to UNSIGNED_
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		private static int _iLabs( int val )
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

		[OgreVersion( 1, 7, 2, "Original name was toOgre" )]
		public static void ConvertFromIL( PixelBox dst )
		{
			if ( !dst.IsConsecutive )
			{
				throw new NotImplementedException( "Destination must currently be consecutive" );
			}

			if ( dst.Width != Il.ilGetInteger( Il.IL_IMAGE_WIDTH ) || dst.Height != Il.ilGetInteger( Il.IL_IMAGE_HEIGHT ) ||
			     dst.Depth != Il.ilGetInteger( Il.IL_IMAGE_DEPTH ) )
			{
				throw new AxiomException( "Destination dimensions must equal IL dimension" );
			}

			var ilfmt = Il.ilGetInteger( Il.IL_IMAGE_FORMAT );
			var iltp = Il.ilGetInteger( Il.IL_IMAGE_TYPE );

			// Check if in-memory format just matches
			// If yes, we can just copy it and save conversion
			var ifmt = Convert( dst.Format );
			if ( ifmt.Format == ilfmt && _iLabs( ifmt.Type ) == _iLabs( iltp ) )
			{
				var size = Il.ilGetInteger( Il.IL_IMAGE_SIZE_OF_DATA );
				using ( var src = BufferBase.Wrap( Il.ilGetData(), size ) )
				{
					Memory.Copy( src, dst.Data, size );
				}
				return;
			}

			// Try if buffer is in a known Axiom format so we can use its conversion routines
			var bufFmt = Convert( (int)ilfmt, (int)iltp );
			ifmt = Convert( bufFmt );

			if ( ifmt.Format == ilfmt && _iLabs( ifmt.Type ) == _iLabs( iltp ) )
			{
				using ( var dstbuf = BufferBase.Wrap( Il.ilGetData(), Il.ilGetInteger( Il.IL_IMAGE_SIZE_OF_DATA ) ) )
				{
					// IL format matches another Axiom format
					var src = new PixelBox( dst.Width, dst.Height, dst.Depth, bufFmt, dstbuf );
					PixelConverter.BulkPixelConversion( src, dst );
				}
				return;
			}

			// Thee extremely slow method
			if ( iltp == Il.IL_UNSIGNED_BYTE || iltp == Il.IL_BYTE )
			{
				_ilToAxiomInternal<byte>( dst.Data, dst.Format, 0x00, 0x00, 0x00, 0xFF );
			}
			else if ( iltp == Il.IL_FLOAT )
			{
				_ilToAxiomInternal<float>( dst.Data, dst.Format, 0.0f, 0.0f, 0.0f, 1.0f );
			}
			else if ( iltp == Il.IL_SHORT || iltp == Il.IL_UNSIGNED_SHORT )
			{
				_ilToAxiomInternal<ushort>( dst.Data, dst.Format, 0x0000, 0x0000, 0x0000, 0xFFFF );
			}
			else
			{
				throw new NotImplementedException( "Cannot convert this DevIL type." );
			}
		}

		[OgreVersion( 1, 7, 2, "Original name was fromOgre" )]
		public static void ConvertToIL( PixelBox src )
		{
			// ilTexImage http://openil.sourceforge.net/docs/il/f00059.htm
			var ifmt = Convert( src.Format );
			if ( src.IsConsecutive && ifmt.IsValid )
			{
				// The easy case, the buffer is laid out in memory just like 
				// we want it to be and is in a format DevIL can understand directly
				// We could even save the copy if DevIL would let us
				Il.ilTexImage( src.Width, src.Height, src.Depth, (byte)ifmt.Channels, ifmt.Format, ifmt.Type, src.Data.Pin() );
				src.Data.UnPin();
			}
			else if ( ifmt.IsValid )
			{
				// The format can be understood directly by DevIL. The only 
				// problem is that ilTexImage expects our image data consecutively 
				// so we cannot use that directly.

				// Let DevIL allocate the memory for us, and copy the data consecutively
				// to its memory
				Il.ilTexImage( src.Width, src.Height, src.Depth, (byte)ifmt.Channels, ifmt.Format, ifmt.Type, IntPtr.Zero );
				using ( var dstbuf = BufferBase.Wrap( Il.ilGetData(), Il.ilGetInteger( Il.IL_IMAGE_SIZE_OF_DATA ) ) )
				{
					var dst = new PixelBox( src.Width, src.Height, src.Depth, src.Format, dstbuf );
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
				var fmt = PixelUtil.HasAlpha( src.Format ) ? PixelFormat.FLOAT32_RGBA : PixelFormat.FLOAT32_RGB;

				// Make up a pixel format
				// We don't have to consider luminance formats as they have
				// straight conversions to DevIL, just weird permutations of RGBA an LA
				var depths = PixelUtil.GetBitDepths( src.Format );

				// Native endian format with all bit depths<8 can safely and quickly be 
				// converted to 24/32 bit
				if ( PixelUtil.IsNativeEndian( src.Format ) && depths[ 0 ] <= 8 && depths[ 1 ] <= 8 && depths[ 2 ] <= 8 &&
				     depths[ 3 ] <= 8 )
				{
					if ( PixelUtil.HasAlpha( src.Format ) )
					{
						fmt = PixelFormat.A8R8G8B8;
					}
					else
					{
						fmt = PixelFormat.R8G8B8;
					}
				}

				// Let DevIL allocate the memory for us, then do the conversion ourselves
				ifmt = Convert( fmt );
				Il.ilTexImage( src.Width, src.Height, src.Depth, (byte)ifmt.Channels, ifmt.Format, ifmt.Type, IntPtr.Zero );
				// TAO 2.0
				//Il.ilTexImage( src.Width, src.Height, src.Depth, (byte)ifmt.Channels, ifmt.Format, ifmt.Type, null );
				using ( var dstbuf = BufferBase.Wrap( Il.ilGetData(), Il.ilGetInteger( Il.IL_IMAGE_SIZE_OF_DATA ) ) )
				{
					var dst = new PixelBox( src.Width, src.Height, src.Depth, fmt, dstbuf );
					PixelConverter.BulkPixelConversion( src, dst );
				}
			}
		}
	};
}