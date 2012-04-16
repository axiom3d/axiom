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

using Axiom.CrossPlatform;

#endregion Namespace Declarations

namespace Axiom.Media
{
	///<summary>
	///  Class for manipulating bit patterns.
	///</summary>
	public static class Bitwise
	{
		///<summary>
		///  Returns the most significant bit set in a value.
		///</summary>
		[OgreVersion( 1, 7, 2 )]
		public static uint MostSignificantBitSet( uint value )
		{
			uint result = 0;
			while ( value != 0 )
			{
				++result;
				value >>= 1;
			}
			return result - 1;
		}

		///<summary>
		///  Returns the closest power-of-two number greater or equal to value.
		///</summary>
		///<remarks>
		///  0 and 1 are powers of two, so firstPO2From(0)==0 and firstPO2From(1)==1.
		///</remarks>
		[OgreVersion( 1, 7, 2 )]
		public static uint FirstPO2From( uint n )
		{
			--n;
			n |= n >> 16;
			n |= n >> 8;
			n |= n >> 4;
			n |= n >> 2;
			n |= n >> 1;
			++n;
			return n;
		}

		/// <summary>
		///   Determines whether the number is power-of-two or not.
		/// </summary>
		/// <remarks>
		///   0 and 1 are tread as power of two.
		/// </remarks>
		/// <returns> true if the number is a power of two otherwise false </returns>
		[OgreVersion( 1, 7, 2 )]
		public static bool IsPow2( int n )
		{
			return ( n & ( n - 1 ) ) == 0;
		}

		/// <summary>
		///   Returns the number of bits a pattern must be shifted right by to remove right-hand zeros.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public static int GetBitShift( int mask )
		{
			if ( mask == 0 )
			{
				return 0;
			}

			var result = 0;
			while ( ( mask & 1 ) == 0 )
			{
				++result;
				mask >>= 1;
			}
			return result;
		}


		/// <summary>
		///   Takes a value with a given src bit mask, and produces another value with a desired bit mask.
		/// </summary>
		/// <remarks>
		///   This routine is useful for colour conversion.
		/// </remarks>
		[OgreVersion( 1, 7, 2 )]
		public static int ConvertBitPattern( int srcValue, int srcBitMask, int destBitMask )
		{
			// Mask off irrelevant source value bits (if any)
			srcValue = srcValue & srcBitMask;

			// Shift source down to bottom of DWORD
			var srcBitShift = GetBitShift( srcBitMask );
			srcValue >>= srcBitShift;

			// Get max value possible in source from srcMask
			var srcMax = srcBitMask >> srcBitShift;

			// Get max available in dest
			var destBitShift = GetBitShift( destBitMask );
			var destMax = destBitMask >> destBitShift;

			// Scale source value into destination, and shift back
			var destValue = ( srcValue * destMax ) / srcMax;
			return ( destValue << destBitShift );
		}

		///<summary>
		///  Convert N bit colour channel value to P bits. It fills P bits with the bit pattern repeated. (this is /((1&lt;&lt;n)-1) in fixed point)
		///</summary>
		[OgreVersion( 1, 7, 2 )]
		public static uint FixedToFixed( uint value, int n, int p )
		{
			if ( n > p )
			{
				// Less bits required than available; this is easy
				value >>= n - p;
			}
			else if ( n < p )
			{
				// More bits required than are there, do the fill
				// Use old fashioned division, probably better than a loop
				if ( value == 0 )
				{
					value = 0;
				}
				else if ( value == ( (uint)( 1 ) << n ) - 1 )
				{
					value = ( 1u << p ) - 1;
				}
				else
				{
					value = value * ( 1u << p ) / ( ( 1u << n ) - 1u );
				}
			}
			return value;
		}

		///<summary>
		///  Convert floating point colour channel value between 0.0 and 1.0 (otherwise clamped) to integer of a certain number of bits. Works for any value of bits between 0 and 31.
		///</summary>
		[OgreVersion( 1, 7, 2 )]
		public static uint FloatToFixed( float value, int bits )
		{
			if ( value <= 0.0f )
			{
				return 0;
			}
			else if ( value >= 1.0f )
			{
				return ( 1u << bits ) - 1;
			}
			else
			{
				return (uint)( value * ( 1u << bits ) );
			}
		}

		///<summary>
		///  Fixed point to float
		///</summary>
		[OgreVersion( 1, 7, 2 )]
		public static float FixedToFloat( uint value, int bits )
		{
			return (float)value / (float)( ( 1u << bits ) - 1 );
		}

		/// <summary>
		///   Write a n*8 bits integer value to memory in native endian.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public static void IntWrite( BufferBase dest, int n, uint value )
		{
#if !AXIOM_SAFE_ONLY
			unsafe
#endif
			{
				switch ( n )
				{
					case 1:
						dest.ToBytePointer()[ 0 ] = (byte)value;
						break;

					case 2:
						dest.ToUShortPointer()[ 0 ] = (ushort)value;
						break;

					case 3:
						var d = dest.ToBytePointer();
#if AXIOM_BIG_ENDIAN
                        d[ 0 ] = (byte)( ( value >> 16 ) & 0xFF );
                        d[ 1 ] = (byte)( ( value >> 8 ) & 0xFF );
                        d[ 2 ] = (byte)( value & 0xFF );
#else
						d[ 2 ] = (byte)( ( value >> 16 ) & 0xFF );
						d[ 1 ] = (byte)( ( value >> 8 ) & 0xFF );
						d[ 0 ] = (byte)( value & 0xFF );
#endif
						break;

					case 4:
						dest.ToUIntPointer()[ 0 ] = value;
						break;
				}
			}
		}

		///<summary>
		///  Read a n*8 bits integer value to memory in native endian.
		///</summary>
		[OgreVersion( 1, 7, 2 )]
		public static uint IntRead( BufferBase src, int n )
		{
#if !AXIOM_SAFE_ONLY
			unsafe
#endif
			{
				switch ( n )
				{
					case 1:
						return src.ToBytePointer()[ 0 ];

					case 2:
						return src.ToUShortPointer()[ 0 ];

					case 3:
						var s = src.ToBytePointer();
#if AXIOM_BIG_ENDIAN
                        return (uint)( s[ 0 ] << 16 |
                                       ( s[ 1 ] << 8 ) |
                                       ( s[ 2 ] ) );
#else
						return (uint)( s[ 0 ] | ( s[ 1 ] << 8 ) | ( s[ 2 ] << 16 ) );
#endif
					case 4:
						return src.ToUIntPointer()[ 0 ];
				}

				return 0; // ?
			}
		}

		///<summary>
		///  Convert a float32 to a float16 (NV_half_float) Courtesy of OpenEXR
		///</summary>
		[OgreVersion( 1, 7, 2 )]
		public static ushort FloatToHalf( float f )
		{
			return FloatToHalfI( new FourByte
			                     {
			                     	Float = f
			                     }.UInt );
		}

		///<summary>
		///  Converts float in uint format to a a half in ushort format
		///</summary>
		[OgreVersion( 1, 7, 2 )]
		public static ushort FloatToHalfI( uint i )
		{
			var s = (int)( i >> 16 ) & 0x00008000;
			var e = (int)( ( i >> 23 ) & 0x000000ff ) - ( 127 - 15 );
			var m = (int)i & 0x007fffff;

			if ( e <= 0 )
			{
				if ( e < -10 )
				{
					return 0;
				}

				m = ( m | 0x00800000 ) >> ( 1 - e );
				return (ushort)( s | ( m >> 13 ) );
			}
			else if ( e == 0xff - ( 127 - 15 ) )
			{
				if ( m == 0 ) // Inf
				{
					return (ushort)( s | 0x7c00 );
				}
				else // NAN
				{
					m >>= 13;
					return (ushort)( (uint)s | 0x7c00 | (uint)m | ( m == 0 ? 1u : 0u ) );
				}
			}
			else
			{
				if ( e > 30 ) // Overflow
				{
					return (ushort)( s | 0x7c00 );
				}

				return (ushort)( s | ( e << 10 ) | ( m >> 13 ) );
			}
		}

		///<summary>
		///  Convert a float16 (NV_half_float) to a float32 Courtesy of OpenEXR
		///</summary>
		[OgreVersion( 1, 7, 2 )]
		public static float HalfToFloat( ushort y )
		{
			return new FourByte
			       {
			       	UInt = HalfToFloatI( y )
			       }.Float;
		}

		///<summary>
		///  Converts a half in ushort format to a float in uint format
		///</summary>
		[OgreVersion( 1, 7, 2 )]
		public static uint HalfToFloatI( ushort y )
		{
			var yuint = (uint)y;
			var s = ( yuint >> 15 ) & 0x00000001;
			var e = ( yuint >> 10 ) & 0x0000001f;
			var m = yuint & 0x000003ff;

			if ( e == 0 )
			{
				if ( m == 0 ) // Plus or minus zero
				{
					return s << 31;
				}
				else
				{
					// Denormalized number -- renormalize it
					while ( ( m & 0x00000400 ) == 0 )
					{
						m <<= 1;
						e -= 1;
					}
					e += 1;
					m &= 0xFFFFFBFF; // ~0x00000400;
				}
			}
			else if ( e == 31 )
			{
				if ( m == 0 ) // Inf
				{
					return ( s << 31 ) | 0x7f800000;
				}
				else // NaN
				{
					return ( s << 31 ) | 0x7f800000 | ( m << 13 );
				}
			}

			e = e + ( 127 - 15 );
			m = m << 13;

			return ( s << 31 ) | ( e << 23 ) | m;
		}

		///<summary>
		///  Convert N bit colour channel value to 8 bits, and return as a byte. It fills P bits with thebit pattern repeated. (this is /((1&lt;&lt;n)-1) in fixed point)
		///</summary>
		public static byte FixedToByteFixed( uint value, int p )
		{
			return (byte)FixedToFixed( value, 8, p );
		}

		///<summary>
		///  Convert floating point colour channel value between 0.0 and 1.0 (otherwise clamped) to an 8-bit integer, and return as a byte.
		///</summary>
		public static byte FloatToByteFixed( float value )
		{
			return (byte)FloatToFixed( value, 8 );
		}
	};
}
