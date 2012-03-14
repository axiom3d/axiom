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
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using System.Reflection;

using Axiom.CrossPlatform;

#endregion Namespace Declarations

namespace Axiom.Media
{
	public static class BufferBaseExtensions
	{
#if AXIOM_SAFE_ONLY
        public static ITypePointer<Col3b> ToCol3BPointer(this BufferBase buffer)
        {
            if (buffer is ITypePointer<Col3b>)
                return buffer as ITypePointer<Col3b>;
            return new ManagedBufferCol3b(buffer as ManagedBuffer);
        }
#else
		public static unsafe Col3b* ToCol3BPointer( this BufferBase buffer )
		{
			return (Col3b*)buffer.Pin();
		}
#endif
	}

	public class ManagedBufferCol3b : ManagedBuffer, ITypePointer<Col3b>
	{
		public ManagedBufferCol3b( ManagedBuffer buffer )
			: base( buffer ) { }

		#region ITypePointer<Col3b> Members

		Col3b ITypePointer<Col3b>.this[ int index ]
		{
			get
			{
				byte[] buf = Buf;
				index *= 3;
				return new Col3b
					   {
						   x = buf[ index += IdxPtr ],
						   y = buf[ ++index ],
						   z = buf[ ++index ]
					   };
			}
			set
			{
				byte[] buf = Buf;
				index *= 3;
				buf[ index += IdxPtr ] = value.x;
				buf[ ++index ] = value.y;
				buf[ ++index ] = value.z;
			}
		}

		#endregion
	}

	/** Type for R8G8B8/B8G8R8 */

	public struct Col3b
	{
		public byte x, y, z;

		public Col3b( uint a, uint b, uint c )
		{
			this.x = (byte)a;
			this.y = (byte)b;
			this.z = (byte)c;
		}
	}

	/** Type for FLOAT32_RGB */

	public struct Col3f
	{
		public float b;
		public float g;
		public float r;

		public Col3f( float r, float g, float b )
		{
			this.r = r;
			this.g = g;
			this.b = b;
		}
	}

	/** Type for FLOAT32_RGBA */

	public struct Col4f
	{
		public float a;
		public float b;
		public float g;
		public float r;

		public Col4f( float r, float g, float b, float a )
		{
			this.r = r;
			this.g = g;
			this.b = b;
			this.a = a;
		}
	}

	///<summary>
	///    A class to convert/copy pixels of the same or different formats
	///</summary>
	public static class OptimizedPixelConversion
	{
		#region PixelFormat.A8R8G8B8 Converters

		#region Nested type: A8R8G8B8toA8B8G8R8Converter

		[PixelConverter( PixelFormat.A8R8G8B8, PixelFormat.A8B8G8R8 )]
		private class A8R8G8B8toA8B8G8R8Converter : IPixelConverter
		{
			#region IPixelConverter Members

			public void Convert( BufferBase input, BufferBase output, int offset )
			{
#if !AXIOM_SAFE_ONLY
				unsafe
#endif
				{
					uint* inputPtr = input.ToUIntPointer();
					uint* outputPtr = output.ToUIntPointer();
					uint inp = inputPtr[ offset ];

					outputPtr[ offset ] = ( ( inp & 0x000000FF ) << 16 ) | ( inp & 0xFF00FF00 ) | ( ( inp & 0x00FF0000 ) >> 16 );
				}
			}

			#endregion
		}

		#endregion

		#region Nested type: A8R8G8B8toB8G8R8A8Converter

		[PixelConverter( PixelFormat.A8R8G8B8, PixelFormat.B8G8R8A8 )]
		private class A8R8G8B8toB8G8R8A8Converter : IPixelConverter
		{
			#region IPixelConverter Members

			public void Convert( BufferBase input, BufferBase output, int offset )
			{
#if !AXIOM_SAFE_ONLY
				unsafe
#endif
				{
					uint* inputPtr = input.ToUIntPointer();
					uint* outputPtr = output.ToUIntPointer();
					uint inp = inputPtr[ offset ];

					outputPtr[ offset ] = ( ( inp & 0x000000FF ) << 24 ) | ( ( inp & 0x0000FF00 ) << 8 ) | ( ( inp & 0x00FF0000 ) >> 8 ) | ( ( inp & 0xFF000000 ) >> 24 );
				}
			}

			#endregion
		}

		#endregion

		#region Nested type: A8R8G8B8toB8G8R8Converter

		[PixelConverter( PixelFormat.A8R8G8B8, PixelFormat.B8G8R8 )]
		private class A8R8G8B8toB8G8R8Converter : IPixelConverter
		{
			#region IPixelConverter Members

			public void Convert( BufferBase input, BufferBase output, int offset )
			{
#if !AXIOM_SAFE_ONLY
				unsafe
#endif
				{
					uint* inputPtr = input.ToUIntPointer();
					Col3b* outputPtr = output.ToCol3BPointer();
					uint inp = inputPtr[ offset ];

					outputPtr[ offset ] = new Col3b
										  {
											  x = (byte)( ( inp >> 0 ) & 0xFF ),
											  y = (byte)( ( inp >> 8 ) & 0xFF ),
											  z = (byte)( ( inp >> 16 ) & 0xFF ),
										  };
				}
			}

			#endregion
		}

		#endregion

		#region Nested type: A8R8G8B8toL8Converter

		[PixelConverter( PixelFormat.A8R8G8B8, PixelFormat.L8 )]
		private class A8R8G8B8toL8Converter : IPixelConverter
		{
			#region IPixelConverter Members

			public void Convert( BufferBase input, BufferBase output, int offset )
			{
#if !AXIOM_SAFE_ONLY
				unsafe
#endif
				{
					uint* inputPtr = input.ToUIntPointer();
					byte* outputPtr = output.ToBytePointer();
					uint inp = inputPtr[ offset ];

					outputPtr[ offset ] = (byte)( ( inp & 0x00FF0000 ) >> 16 );
				}
			}

			#endregion
		}

		#endregion

		#region Nested type: A8R8G8B8toR8G8B8A8Converter

		[PixelConverter( PixelFormat.A8R8G8B8, PixelFormat.R8G8B8A8 )]
		private class A8R8G8B8toR8G8B8A8Converter : IPixelConverter
		{
			#region IPixelConverter Members

			public void Convert( BufferBase input, BufferBase output, int offset )
			{
#if !AXIOM_SAFE_ONLY
				unsafe
#endif
				{
					uint* inputPtr = input.ToUIntPointer();
					uint* outputPtr = output.ToUIntPointer();
					uint inp = inputPtr[ offset ];

					outputPtr[ offset ] = ( ( inp & 0x00FFFFFF ) << 8 ) | ( ( inp & 0xFF000000 ) >> 24 );
				}
			}

			#endregion
		}

		#endregion

		#region Nested type: A8R8G8B8toR8G8B8Converter

		[PixelConverter( PixelFormat.A8R8G8B8, PixelFormat.R8G8B8 )]
		private class A8R8G8B8toR8G8B8Converter : IPixelConverter
		{
			#region IPixelConverter Members

			public void Convert( BufferBase input, BufferBase output, int offset )
			{
#if !AXIOM_SAFE_ONLY
				unsafe
#endif
				{
					uint* inputPtr = input.ToUIntPointer();
					Col3b* outputPtr = output.ToCol3BPointer();
					uint inp = inputPtr[ offset ];

					outputPtr[ offset ] = new Col3b
										  {
											  x = (byte)( ( inp >> 16 ) & 0xFF ),
											  y = (byte)( ( inp >> 8 ) & 0xFF ),
											  z = (byte)( ( inp >> 0 ) & 0xFF ),
										  };
				}
			}

			#endregion
		}

		#endregion

		#endregion PixelFormat.A8R8G8B8 Converters

		#region PixelFormat.A8B8G8R8 Converters

		#region Nested type: A8B8G8R8toA8R8G8B8Converter

		[PixelConverter( PixelFormat.A8B8G8R8, PixelFormat.A8R8G8B8 )]
		private class A8B8G8R8toA8R8G8B8Converter : IPixelConverter
		{
			#region IPixelConverter Members

			public void Convert( BufferBase input, BufferBase output, int offset )
			{
#if !AXIOM_SAFE_ONLY
				unsafe
#endif
				{
					uint* inputPtr = input.ToUIntPointer();
					uint* outputPtr = output.ToUIntPointer();
					uint inp = inputPtr[ offset ];

					outputPtr[ offset ] = ( ( inp & 0x000000FF ) << 16 ) | ( inp & 0xFF00FF00 ) | ( ( inp & 0x00FF0000 ) >> 16 );
				}
			}

			#endregion
		}

		#endregion

		#region Nested type: A8B8G8R8toB8G8R8A8Converter

		[PixelConverter( PixelFormat.A8B8G8R8, PixelFormat.B8G8R8A8 )]
		private class A8B8G8R8toB8G8R8A8Converter : IPixelConverter
		{
			#region IPixelConverter Members

			public void Convert( BufferBase input, BufferBase output, int offset )
			{
#if !AXIOM_SAFE_ONLY
				unsafe
#endif
				{
					uint* inputPtr = input.ToUIntPointer();
					uint* outputPtr = output.ToUIntPointer();
					uint inp = inputPtr[ offset ];

					outputPtr[ offset ] = ( ( inp & 0x00FFFFFF ) << 8 ) | ( ( inp & 0xFF000000 ) >> 24 );
				}
			}

			#endregion
		}

		#endregion

		#region Nested type: A8B8G8R8toL8Converter

		[PixelConverter( PixelFormat.A8B8G8R8, PixelFormat.L8 )]
		private class A8B8G8R8toL8Converter : IPixelConverter
		{
			#region IPixelConverter Members

			public void Convert( BufferBase input, BufferBase output, int offset )
			{
#if !AXIOM_SAFE_ONLY
				unsafe
#endif
				{
					uint* inputPtr = input.ToUIntPointer();
					byte* outputPtr = output.ToBytePointer();
					uint inp = inputPtr[ offset ];

					outputPtr[ offset ] = (byte)( inp & 0x000000FF );
				}
			}

			#endregion
		}

		#endregion

		#region Nested type: A8B8G8R8toR8G8B8A8Converter

		[PixelConverter( PixelFormat.A8B8G8R8, PixelFormat.R8G8B8A8 )]
		private class A8B8G8R8toR8G8B8A8Converter : IPixelConverter
		{
			#region IPixelConverter Members

			public void Convert( BufferBase input, BufferBase output, int offset )
			{
#if !AXIOM_SAFE_ONLY
				unsafe
#endif
				{
					uint* inputPtr = input.ToUIntPointer();
					uint* outputPtr = output.ToUIntPointer();
					uint inp = inputPtr[ offset ];

					outputPtr[ offset ] = ( ( inp & 0x000000FF ) << 24 ) | ( ( inp & 0x0000FF00 ) << 8 ) | ( ( inp & 0x00FF0000 ) >> 8 ) | ( ( inp & 0xFF000000 ) >> 24 );
				}
			}

			#endregion
		}

		#endregion

		#endregion PixelFormat.A8B8G8R8 Converters

		#region PixelFormat.B8G8R8A8 Converters

		#region Nested type: B8G8R8A8toA8B8G8R8Converter

		[PixelConverter( PixelFormat.B8G8R8A8, PixelFormat.A8B8G8R8 )]
		private class B8G8R8A8toA8B8G8R8Converter : IPixelConverter
		{
			#region IPixelConverter Members

			public void Convert( BufferBase input, BufferBase output, int offset )
			{
#if !AXIOM_SAFE_ONLY
				unsafe
#endif
				{
					uint* inputPtr = input.ToUIntPointer();
					uint* outputPtr = output.ToUIntPointer();
					uint inp = inputPtr[ offset ];

					outputPtr[ offset ] = ( ( inp & 0x000000FF ) << 24 ) | ( ( inp & 0xFFFFFF00 ) >> 8 );
				}
			}

			#endregion
		}

		#endregion

		#region Nested type: B8G8R8A8toA8R8G8B8Converter

		[PixelConverter( PixelFormat.B8G8R8A8, PixelFormat.A8R8G8B8 )]
		private class B8G8R8A8toA8R8G8B8Converter : IPixelConverter
		{
			#region IPixelConverter Members

			public void Convert( BufferBase input, BufferBase output, int offset )
			{
#if !AXIOM_SAFE_ONLY
				unsafe
#endif
				{
					uint* inputPtr = input.ToUIntPointer();
					uint* outputPtr = output.ToUIntPointer();
					uint inp = inputPtr[ offset ];

					outputPtr[ offset ] = ( ( inp & 0x000000FF ) << 24 ) | ( ( inp & 0x0000FF00 ) << 8 ) | ( ( inp & 0x00FF0000 ) >> 8 ) | ( ( inp & 0xFF000000 ) >> 24 );
				}
			}

			#endregion
		}

		#endregion

		#region Nested type: B8G8R8A8toL8Converter

		[PixelConverter( PixelFormat.B8G8R8A8, PixelFormat.L8 )]
		private class B8G8R8A8toL8Converter : IPixelConverter
		{
			#region IPixelConverter Members

			public void Convert( BufferBase input, BufferBase output, int offset )
			{
#if !AXIOM_SAFE_ONLY
				unsafe
#endif
				{
					uint* inputPtr = input.ToUIntPointer();
					byte* outputPtr = output.ToBytePointer();
					uint inp = inputPtr[ offset ];

					outputPtr[ offset ] = (byte)( ( inp & 0x0000FF00 ) >> 8 );
				}
			}

			#endregion
		}

		#endregion

		#region Nested type: B8G8R8A8toR8G8B8A8Converter

		[PixelConverter( PixelFormat.B8G8R8A8, PixelFormat.R8G8B8A8 )]
		private class B8G8R8A8toR8G8B8A8Converter : IPixelConverter
		{
			#region IPixelConverter Members

			public void Convert( BufferBase input, BufferBase output, int offset )
			{
#if !AXIOM_SAFE_ONLY
				unsafe
#endif
				{
					uint* inputPtr = input.ToUIntPointer();
					uint* outputPtr = output.ToUIntPointer();
					uint inp = inputPtr[ offset ];

					outputPtr[ offset ] = ( ( inp & 0x0000FF00 ) << 16 ) | ( inp & 0x00FF00FF ) | ( ( inp & 0xFF000000 ) >> 16 );
				}
			}

			#endregion
		}

		#endregion

		#endregion PixelFormat.B8G8R8A8 Converters

		#region PixelFormat.R8G8B8A8 Converters

		#region Nested type: R8G8B8A8toA8B8G8R8Converter

		[PixelConverter( PixelFormat.R8G8B8A8, PixelFormat.A8B8G8R8 )]
		private class R8G8B8A8toA8B8G8R8Converter : IPixelConverter
		{
			#region IPixelConverter Members

			public void Convert( BufferBase input, BufferBase output, int offset )
			{
#if !AXIOM_SAFE_ONLY
				unsafe
#endif
				{
					uint* inputPtr = input.ToUIntPointer();
					uint* outputPtr = output.ToUIntPointer();
					uint inp = inputPtr[ offset ];

					outputPtr[ offset ] = ( ( inp & 0x000000FF ) << 24 ) | ( ( inp & 0x0000FF00 ) << 8 ) | ( ( inp & 0x00FF0000 ) >> 8 ) | ( ( inp & 0xFF000000 ) >> 24 );
				}
			}

			#endregion
		}

		#endregion

		#region Nested type: R8G8B8A8toA8R8G8B8Converter

		[PixelConverter( PixelFormat.R8G8B8A8, PixelFormat.A8R8G8B8 )]
		private class R8G8B8A8toA8R8G8B8Converter : IPixelConverter
		{
			#region IPixelConverter Members

			public void Convert( BufferBase input, BufferBase output, int offset )
			{
#if !AXIOM_SAFE_ONLY
				unsafe
#endif
				{
					uint* inputPtr = input.ToUIntPointer();
					uint* outputPtr = output.ToUIntPointer();
					uint inp = inputPtr[ offset ];

					outputPtr[ offset ] = ( ( inp & 0x000000FF ) << 24 ) | ( ( inp & 0xFFFFFF00 ) >> 8 );
				}
			}

			#endregion
		}

		#endregion

		#region Nested type: R8G8B8A8toB8G8R8A8Converter

		[PixelConverter( PixelFormat.R8G8B8A8, PixelFormat.B8G8R8A8 )]
		private class R8G8B8A8toB8G8R8A8Converter : IPixelConverter
		{
			#region IPixelConverter Members

			public void Convert( BufferBase input, BufferBase output, int offset )
			{
#if !AXIOM_SAFE_ONLY
				unsafe
#endif
				{
					uint* inputPtr = input.ToUIntPointer();
					uint* outputPtr = output.ToUIntPointer();
					uint inp = inputPtr[ offset ];

					outputPtr[ offset ] = ( ( inp & 0x0000FF00 ) << 16 ) | ( inp & 0x00FF00FF ) | ( ( inp & 0xFF000000 ) >> 16 );
				}
			}

			#endregion
		}

		#endregion

		#endregion PixelFormat.R8G8B8A8 Converters

		#region PixelFormat.L8 Converters

		#region Nested type: L8toA8B8G8R8Converter

		[PixelConverter( PixelFormat.L8, PixelFormat.A8B8G8R8 )]
		private class L8toA8B8G8R8Converter : IPixelConverter
		{
			#region IPixelConverter Members

			public void Convert( BufferBase input, BufferBase output, int offset )
			{
#if !AXIOM_SAFE_ONLY
				unsafe
#endif
				{
					byte* inputPtr = input.ToBytePointer();
					uint* outputPtr = output.ToUIntPointer();
					byte inp = inputPtr[ offset ];

					outputPtr[ offset ] = 0xFF000000 | ( ( (uint)inp ) << 0 ) | ( ( (uint)inp ) << 8 ) | ( ( (uint)inp ) << 16 );
				}
			}

			#endregion
		}

		#endregion

		#region Nested type: L8toA8R8G8B8Converter

		[PixelConverter( PixelFormat.L8, PixelFormat.A8R8G8B8 )]
		private class L8toA8R8G8B8Converter : IPixelConverter
		{
			#region IPixelConverter Members

			public void Convert( BufferBase input, BufferBase output, int offset )
			{
#if !AXIOM_SAFE_ONLY
				unsafe
#endif
				{
					byte* inputPtr = input.ToBytePointer();
					uint* outputPtr = output.ToUIntPointer();
					byte inp = inputPtr[ offset ];

					outputPtr[ offset ] = 0xFF000000 | ( ( (uint)inp ) << 0 ) | ( ( (uint)inp ) << 8 ) | ( ( (uint)inp ) << 16 );
				}
			}

			#endregion
		}

		#endregion

		#region Nested type: L8toB8G8R8A8Converter

		[PixelConverter( PixelFormat.L8, PixelFormat.B8G8R8A8 )]
		private class L8toB8G8R8A8Converter : IPixelConverter
		{
			#region IPixelConverter Members

			public void Convert( BufferBase input, BufferBase output, int offset )
			{
#if !AXIOM_SAFE_ONLY
				unsafe
#endif
				{
					byte* inputPtr = input.ToBytePointer();
					uint* outputPtr = output.ToUIntPointer();
					byte inp = inputPtr[ offset ];

					outputPtr[ offset ] = 0x000000FF | ( ( (uint)inp ) << 8 ) | ( ( (uint)inp ) << 16 ) | ( ( (uint)inp ) << 24 );
				}
			}

			#endregion
		}

		#endregion

		#region Nested type: L8toL16Converter

		[PixelConverter( PixelFormat.L8, PixelFormat.L16 )]
		private class L8toL16Converter : IPixelConverter
		{
			#region IPixelConverter Members

			public void Convert( BufferBase input, BufferBase output, int offset )
			{
#if !AXIOM_SAFE_ONLY
				unsafe
#endif
				{
					byte* inputPtr = input.ToBytePointer();
					ushort* outputPtr = output.ToUShortPointer();
					byte inp = inputPtr[ offset ];

					outputPtr[ offset ] = (ushort)( ( ( (uint)inp ) << 8 ) | ( ( inp ) ) );
				}
			}

			#endregion
		}

		#endregion

		#endregion PixelFormat.L8 Converters

		#region PixelFormat.L16 Converters

		[PixelConverter( PixelFormat.L16, PixelFormat.L8 )]
		private class L16toL8Converter : IPixelConverter
		{
			#region IPixelConverter Members

			public void Convert( BufferBase input, BufferBase output, int offset )
			{
#if !AXIOM_SAFE_ONLY
				unsafe
#endif
				{
					ushort* inputPtr = input.ToUShortPointer();
					byte* outputPtr = output.ToBytePointer();
					ushort inp = inputPtr[ offset ];

					outputPtr[ offset ] = (byte)( inp >> 8 );
				}
			}

			#endregion
		}

		#endregion PixelFormat.L16 Converters

		#region PixelFormat.B8R8G8 Converters

		#region Nested type: B8G8R8toA8B8G8R8Converter

		[PixelConverter( PixelFormat.B8G8R8, PixelFormat.A8B8G8R8 )]
		private class B8G8R8toA8B8G8R8Converter : IPixelConverter
		{
			#region IPixelConverter Members

			public void Convert( BufferBase input, BufferBase output, int offset )
			{
#if !AXIOM_SAFE_ONLY
				unsafe
#endif
				{
					Col3b* inputPtr = input.ToCol3BPointer();
					uint* outputPtr = output.ToUIntPointer();
					Col3b inp = inputPtr[ offset ];

					//int xshift = 8, yshift = 16, zshift = 24, ashift = 0; //BUG: NRSC: Alpha was on wrong side
					int xshift = 16, yshift = 8, zshift = 0, ashift = 24;

#if AXIOM_BIG_ENDIAN
				    outputPtr[ offset ] = ( (uint)( 0xFF << ashift ) ) | ( ( (uint)inp.x ) << xshift ) | 
                                          ( ( (uint)inp.y ) << yshift ) | ( ( (uint)inp.z ) << zshift );
#else
					outputPtr[ offset ] = ( (uint)( 0xFF << ashift ) ) | ( ( (uint)inp.x ) << zshift ) | ( ( (uint)inp.y ) << yshift ) | ( ( (uint)inp.z ) << xshift );
#endif
				}
			}

			#endregion
		}

		#endregion

		#region Nested type: B8G8R8toA8R8G8B8Converter

		[PixelConverter( PixelFormat.B8G8R8, PixelFormat.A8R8G8B8 )]
		private class B8G8R8toA8R8G8B8Converter : IPixelConverter
		{
			#region IPixelConverter Members

			public void Convert( BufferBase input, BufferBase output, int offset )
			{
#if !AXIOM_SAFE_ONLY
				unsafe
#endif
				{
					Col3b* inputPtr = input.ToCol3BPointer();
					uint* outputPtr = output.ToUIntPointer();
					Col3b inp = inputPtr[ offset ];

					int xshift = 0, yshift = 8, zshift = 16, ashift = 24;

#if AXIOM_BIG_ENDIAN
				    outputPtr[ offset ] = ( (uint)( 0xFF << ashift ) ) | ( ( (uint)inp.x ) << xshift ) |
                                          ( ( (uint)inp.y ) << yshift ) | ( ( (uint)inp.z ) << zshift );
#else
					outputPtr[ offset ] = ( (uint)( 0xFF << ashift ) ) | ( ( (uint)inp.x ) << zshift ) | ( ( (uint)inp.y ) << yshift ) | ( ( (uint)inp.z ) << xshift );
#endif
				}
			}

			#endregion
		}

		#endregion

		#region Nested type: B8G8R8toB8G8R8A8Converter

		[PixelConverter( PixelFormat.B8G8R8, PixelFormat.B8G8R8A8 )]
		private class B8G8R8toB8G8R8A8Converter : IPixelConverter
		{
			#region IPixelConverter Members

			public void Convert( BufferBase input, BufferBase output, int offset )
			{
#if !AXIOM_SAFE_ONLY
				unsafe
#endif
				{
					Col3b* inputPtr = input.ToCol3BPointer();
					uint* outputPtr = output.ToUIntPointer();
					Col3b inp = inputPtr[ offset ];

					int xshift = 24, yshift = 16, zshift = 8, ashift = 0;

#if AXIOM_BIG_ENDIAN
				    outputPtr[ offset ] = ( (uint)( 0xFF << ashift ) ) | ( ( (uint)inp.x ) << xshift ) |
                                          ( ( (uint)inp.y ) << yshift ) | ( ( (uint)inp.z ) << zshift );
#else
					outputPtr[ offset ] = ( (uint)( 0xFF << ashift ) ) | ( ( (uint)inp.x ) << zshift ) | ( ( (uint)inp.y ) << yshift ) | ( ( (uint)inp.z ) << xshift );
#endif
				}
			}

			#endregion
		}

		#endregion

		#region Nested type: B8G8R8toR8G8B8Converter

		[PixelConverter( PixelFormat.B8G8R8, PixelFormat.R8G8B8 )]
		private class B8G8R8toR8G8B8Converter : IPixelConverter
		{
			#region IPixelConverter Members

			public void Convert( BufferBase input, BufferBase output, int offset )
			{
#if !AXIOM_SAFE_ONLY
				unsafe
#endif
				{
					Col3b* inputPtr = input.ToCol3BPointer();
					Col3b* outputPtr = output.ToCol3BPointer();
					outputPtr[ offset ] = inputPtr[ offset ];
				}
			}

			#endregion
		}

		#endregion

		#endregion PixelFormat.B8R8G8 Converters

		#region PixelFormat.R8G8B8 Converters

		#region Nested type: R8G8B8toA8B8G8R8Converter

		[PixelConverter( PixelFormat.R8G8B8, PixelFormat.A8B8G8R8 )]
		private class R8G8B8toA8B8G8R8Converter : IPixelConverter
		{
			#region IPixelConverter Members

			public void Convert( BufferBase input, BufferBase output, int offset )
			{
#if !AXIOM_SAFE_ONLY
				unsafe
#endif
				{
					Col3b* inputPtr = input.ToCol3BPointer();
					uint* outputPtr = output.ToUIntPointer();
					Col3b inp = inputPtr[ offset ];

					int xshift = 0, yshift = 8, zshift = 16, ashift = 24;

#if AXIOM_BIG_ENDIAN
				    outputPtr[ offset ] = ( (uint)( 0xFF << ashift ) ) | ( ( (uint)inp.x ) << xshift ) |
                                          ( ( (uint)inp.y ) << yshift ) | ( ( (uint)inp.z ) << zshift );
#else
					outputPtr[ offset ] = ( (uint)( 0xFF << ashift ) ) | ( ( (uint)inp.x ) << zshift ) | ( ( (uint)inp.y ) << yshift ) | ( ( (uint)inp.z ) << xshift );
#endif
				}
			}

			#endregion
		}

		#endregion

		#region Nested type: R8G8B8toA8R8G8B8Converter

		[PixelConverter( PixelFormat.R8G8B8, PixelFormat.A8R8G8B8 )]
		private class R8G8B8toA8R8G8B8Converter : IPixelConverter
		{
			#region IPixelConverter Members

			public void Convert( BufferBase input, BufferBase output, int offset )
			{
#if !AXIOM_SAFE_ONLY
				unsafe
#endif
				{
					Col3b* inputPtr = input.ToCol3BPointer();
					uint* outputPtr = output.ToUIntPointer();
					Col3b inp = inputPtr[ offset ];

					int xshift = 16, yshift = 8, zshift = 0, ashift = 24;

#if AXIOM_BIG_ENDIAN
				    outputPtr[ offset ] = ( (uint)( 0xFF << ashift ) ) | ( ( (uint)inp.x ) << xshift ) | 
                                          ( ( (uint)inp.y ) << yshift ) | ( ( (uint)inp.z ) << zshift );
#else
					outputPtr[ offset ] = ( (uint)( 0xFF << ashift ) ) | ( ( (uint)inp.x ) << zshift ) | ( ( (uint)inp.y ) << yshift ) | ( ( (uint)inp.z ) << xshift );
#endif
				}
			}

			#endregion
		}

		#endregion

		#region Nested type: R8G8B8toB8G8R8A8Converter

		[PixelConverter( PixelFormat.R8G8B8, PixelFormat.B8G8R8A8 )]
		private class R8G8B8toB8G8R8A8Converter : IPixelConverter
		{
			#region IPixelConverter Members

			public void Convert( BufferBase input, BufferBase output, int offset )
			{
#if !AXIOM_SAFE_ONLY
				unsafe
#endif
				{
					Col3b* inputPtr = input.ToCol3BPointer();
					uint* outputPtr = output.ToUIntPointer();
					Col3b inp = inputPtr[ offset ];

					int xshift = 8, yshift = 16, zshift = 24, ashift = 0;

#if AXIOM_BIG_ENDIAN
				    outputPtr[ offset ] = ( (uint)( 0xFF << ashift ) ) | ( ( (uint)inp.x ) << xshift ) |
                                          ( ( (uint)inp.y ) << yshift ) | ( ( (uint)inp.z ) << zshift );
#else
					outputPtr[ offset ] = ( (uint)( 0xFF << ashift ) ) | ( ( (uint)inp.x ) << zshift ) | ( ( (uint)inp.y ) << yshift ) | ( ( (uint)inp.z ) << xshift );
#endif
				}
			}

			#endregion
		}

		#endregion

		#region Nested type: R8G8B8toB8G8R8Converter

		[PixelConverter( PixelFormat.R8G8B8, PixelFormat.B8G8R8 )]
		private class R8G8B8toB8G8R8Converter : IPixelConverter
		{
			#region IPixelConverter Members

			public void Convert( BufferBase input, BufferBase output, int offset )
			{
#if !AXIOM_SAFE_ONLY
				unsafe
#endif
				{
					Col3b* inputPtr = input.ToCol3BPointer();
					Col3b* outputPtr = output.ToCol3BPointer();
					outputPtr[ offset ] = inputPtr[ offset ];
				}
			}

			#endregion
		}

		#endregion

		#endregion PixelFormat.R8G8B8 Converters

		#region PixelFormat.X8R8G8B8 Converters

		#region Nested type: X8R8G8B8toA8B8G8R8Converter

		[PixelConverter( PixelFormat.X8R8G8B8, PixelFormat.A8B8G8R8 )]
		private class X8R8G8B8toA8B8G8R8Converter : IPixelConverter
		{
			#region IPixelConverter Members

			public void Convert( BufferBase input, BufferBase output, int offset )
			{
#if !AXIOM_SAFE_ONLY
				unsafe
#endif
				{
					uint* inputPtr = input.ToUIntPointer();
					uint* outputPtr = output.ToUIntPointer();
					uint inp = inputPtr[ offset ];

					outputPtr[ offset ] = ( ( inp & 0x0000FF ) << 16 ) | ( ( inp & 0xFF0000 ) >> 16 ) | ( inp & 0x00FF00 ) | 0xFF000000;
				}
			}

			#endregion
		}

		#endregion

		#region Nested type: X8R8G8B8toA8R8G8B8Converter

		[PixelConverter( PixelFormat.X8R8G8B8, PixelFormat.A8R8G8B8 )]
		private class X8R8G8B8toA8R8G8B8Converter : IPixelConverter
		{
			#region IPixelConverter Members

			public void Convert( BufferBase input, BufferBase output, int offset )
			{
#if !AXIOM_SAFE_ONLY
				unsafe
#endif
				{
					uint* inputPtr = input.ToUIntPointer();
					uint* outputPtr = output.ToUIntPointer();
					uint inp = inputPtr[ offset ];

					outputPtr[ offset ] = inp | 0xFF000000;
				}
			}

			#endregion
		}

		#endregion

		#region Nested type: X8R8G8B8toB8G8R8A8Converter

		[PixelConverter( PixelFormat.X8R8G8B8, PixelFormat.B8G8R8A8 )]
		private class X8R8G8B8toB8G8R8A8Converter : IPixelConverter
		{
			#region IPixelConverter Members

			public void Convert( BufferBase input, BufferBase output, int offset )
			{
#if !AXIOM_SAFE_ONLY
				unsafe
#endif
				{
					uint* inputPtr = input.ToUIntPointer();
					uint* outputPtr = output.ToUIntPointer();
					uint inp = inputPtr[ offset ];

					outputPtr[ offset ] = ( ( inp & 0x0000FF ) << 24 ) | ( ( inp & 0xFF0000 ) >> 8 ) | ( ( inp & 0x00FF00 ) << 8 ) | 0x000000FF;
				}
			}

			#endregion
		}

		#endregion

		#region Nested type: X8R8G8B8toR8G8B8A8Converter

		[PixelConverter( PixelFormat.X8R8G8B8, PixelFormat.R8G8B8A8 )]
		private class X8R8G8B8toR8G8B8A8Converter : IPixelConverter
		{
			#region IPixelConverter Members

			public void Convert( BufferBase input, BufferBase output, int offset )
			{
#if !AXIOM_SAFE_ONLY
				unsafe
#endif
				{
					uint* inputPtr = input.ToUIntPointer();
					uint* outputPtr = output.ToUIntPointer();
					uint inp = inputPtr[ offset ];

					outputPtr[ offset ] = ( ( inp & 0xFFFFFF ) << 8 ) | 0x000000FF;
				}
			}

			#endregion
		}

		#endregion

		#endregion PixelFormat.X8R8G8B8 Converters

		#region PixelFormat.X8B8G8R8 Converters

		#region Nested type: X8B8G8R8toA8B8G8R8Converter

		[PixelConverter( PixelFormat.X8B8G8R8, PixelFormat.A8B8G8R8 )]
		private class X8B8G8R8toA8B8G8R8Converter : IPixelConverter
		{
			#region IPixelConverter Members

			public void Convert( BufferBase input, BufferBase output, int offset )
			{
#if !AXIOM_SAFE_ONLY
				unsafe
#endif
				{
					uint* inputPtr = input.ToUIntPointer();
					uint* outputPtr = output.ToUIntPointer();
					uint inp = inputPtr[ offset ];

					outputPtr[ offset ] = inp | 0xFF000000;
				}
			}

			#endregion
		}

		#endregion

		#region Nested type: X8B8G8R8toA8R8G8B8Converter

		[PixelConverter( PixelFormat.X8B8G8R8, PixelFormat.A8R8G8B8 )]
		private class X8B8G8R8toA8R8G8B8Converter : IPixelConverter
		{
			#region IPixelConverter Members

			public void Convert( BufferBase input, BufferBase output, int offset )
			{
#if !AXIOM_SAFE_ONLY
				unsafe
#endif
				{
					uint* inputPtr = input.ToUIntPointer();
					uint* outputPtr = output.ToUIntPointer();
					uint inp = inputPtr[ offset ];

					outputPtr[ offset ] = ( ( inp & 0x0000FF ) << 16 ) | ( ( inp & 0xFF0000 ) >> 16 ) | ( inp & 0x00FF00 ) | 0xFF000000;
				}
			}

			#endregion
		}

		#endregion

		#region Nested type: X8B8G8R8toB8G8R8A8Converter

		[PixelConverter( PixelFormat.X8B8G8R8, PixelFormat.B8G8R8A8 )]
		private class X8B8G8R8toB8G8R8A8Converter : IPixelConverter
		{
			#region IPixelConverter Members

			public void Convert( BufferBase input, BufferBase output, int offset )
			{
#if !AXIOM_SAFE_ONLY
				unsafe
#endif
				{
					uint* inputPtr = input.ToUIntPointer();
					uint* outputPtr = output.ToUIntPointer();
					uint inp = inputPtr[ offset ];

					outputPtr[ offset ] = ( ( inp & 0xFFFFFF ) << 8 ) | 0x000000FF;
				}
			}

			#endregion
		}

		#endregion

		#region Nested type: X8B8G8R8toR8G8B8A8Converter

		[PixelConverter( PixelFormat.X8B8G8R8, PixelFormat.R8G8B8A8 )]
		private class X8B8G8R8toR8G8B8A8Converter : IPixelConverter
		{
			#region IPixelConverter Members

			public void Convert( BufferBase input, BufferBase output, int offset )
			{
#if !AXIOM_SAFE_ONLY
				unsafe
#endif
				{
					uint* inputPtr = input.ToUIntPointer();
					uint* outputPtr = output.ToUIntPointer();
					uint inp = inputPtr[ offset ];

					outputPtr[ offset ] = ( ( inp & 0x0000FF ) << 24 ) | ( ( inp & 0xFF0000 ) >> 8 ) | ( ( inp & 0x00FF00 ) << 8 ) | 0x000000FF;
				}
			}

			#endregion
		}

		#endregion

		#endregion PixelFormat.X8B8G8R8 Converters

		private static readonly Dictionary<int, IPixelConverter> _supportedConversions;

		static OptimizedPixelConversion()
		{
			_supportedConversions = new Dictionary<int, IPixelConverter>();
			Type t = Assembly.GetExecutingAssembly().GetType( "Axiom.Media.OptimizedPixelConversion" );

			foreach ( Type converter in t.GetNestedTypes( BindingFlags.NonPublic ) )
			{
				object[] attribs = converter.GetCustomAttributes( typeof( PixelConverterAttribute ), false );
				if ( attribs.Length != 0 )
				{
					var attrib = (PixelConverterAttribute)attribs[ 0 ];
					object instance = Assembly.GetExecutingAssembly().CreateInstance( converter.FullName );
					_supportedConversions.Add( attrib.Id, (IPixelConverter)instance );
				}
			}
		}

		public static bool DoOptimizedConversion( PixelBox src, PixelBox dst )
		{
			int conversion = ( (int)src.Format << 8 ) | (int)dst.Format;

			if ( _supportedConversions.ContainsKey( conversion ) )
			{
				PixelBoxConverter.Convert( src, dst, _supportedConversions[ conversion ] );
				return true;
			}
			return false;
		}

		#region Nested type: IPixelConverter

		private interface IPixelConverter
		{
			void Convert( BufferBase input, BufferBase output, int offset );
		}

		#endregion

		#region Nested type: PixelBoxConverter

		private static class PixelBoxConverter
		{
			public static void Convert( PixelBox src, PixelBox dst, IPixelConverter pixelConverter )
			{
				{
					var srcptr = (BufferBase)src.Data.Clone();
					var dstptr = (BufferBase)dst.Data.Clone();
					int srcSliceSkip = src.SliceSkip;
					int dstSliceSkip = dst.SliceSkip;
					int k = src.Right - src.Left;

					for ( int z = src.Front; z < src.Back; z++ )
					{
						for ( int y = src.Top; y < src.Bottom; y++ )
						{
							for ( int x = 0; x < k; x++ )
							{
								pixelConverter.Convert( srcptr, dstptr, x );
							}
							srcptr.Ptr += src.RowPitch * PixelUtil.GetNumElemBytes( src.Format );
							dstptr.Ptr += dst.RowPitch * PixelUtil.GetNumElemBytes( dst.Format );
						}
						srcptr.Ptr += srcSliceSkip;
						dstptr.Ptr += dstSliceSkip;
					}
				}
			}
		}

		#endregion

		#region Nested type: PixelConverterAttribute

		private class PixelConverterAttribute : Attribute
		{
			private readonly PixelFormat _dstFormat;
			private readonly PixelFormat _srcFormat;

			public PixelConverterAttribute( PixelFormat srcFormat, PixelFormat dstFormat )
			{
				this._srcFormat = srcFormat;
				this._dstFormat = dstFormat;
			}

			public int Id
			{
				get
				{
					return ( (int)this._srcFormat << 8 ) + (int)this._dstFormat;
				}
			}
		}

		#endregion
	}
}
