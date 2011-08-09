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
//     <id value="$Id:$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;

#if !AXIOM_SAFE_ONLY
using System.Runtime.InteropServices;
#endif

#endregion Namespace Declarations


namespace Axiom.Core
{
	public class MemoryManager : Singleton<MemoryManager>
	{
		private readonly List<IMemoryBuffer> _memoryPool = new List<IMemoryBuffer>();
		private readonly static Dictionary<Type, Dictionary<Type, IBitConverter>> _bitConverters;
		public Dictionary<Type, Dictionary<Type, IBitConverter>> BitConverters { get { return _bitConverters; } }

		static MemoryManager()
		{
			_bitConverters = new Dictionary<Type, Dictionary<Type, IBitConverter>>()
							 {
								{
									typeof( byte ),
									new Dictionary<Type, IBitConverter>()
									{
										{typeof( int ), new Byte2IntBitConverter()},
										{typeof( float ), new Byte2SingleBitConverter()}
									}
									},
								{
									typeof( int ),
									new Dictionary<Type, IBitConverter>()
									{
										{typeof( byte ), new Int2ByteBitConverter()},
									}
								}

							 }; ;
		}

#if !AXIOM_SAFE_ONLY

		#region Copy Method
		/// <summary>
		///	Method for copying data from one IntPtr to another.
		/// </summary>
		/// <param name="src">Source pointer.</param>
		/// <param name="dest">Destination pointer.</param>
		/// <param name="length">Length of data (in bytes) to copy.</param>
		public static void Copy( IntPtr src, IntPtr dest, int length )
		{
			Copy( src, dest, 0, 0, length );
		}

		/// <summary>
		///	Method for copying data from one IntPtr to another.
		/// </summary>
		/// <param name="src">Source pointer.</param>
		/// <param name="dest">Destination pointer.</param>
		/// <param name="srcOffset">Offset (in bytes) at which to copy from the source pointer.</param>
		/// <param name="destOffset">Offset (in bytes) at which to begin copying to the destination pointer.</param>
		/// <param name="length">Length of data (in bytes) to copy.</param>
		public static void Copy( byte[] src, ref byte[] dest, int srcOffset, int destOffset, int length )
		{
			// TODO: Block copy would be faster, find a cross platform way to do it
			for ( int i = 0; i < length; i++ )
			{
				dest[ i + destOffset ] = src[ i + srcOffset ];
			}
		}
        #endregion Copy Method

		/// <summary>
		///     Sets the memory to 0 starting at the specified offset for the specified byte length.
		/// </summary>
		/// <param name="dest">Destination pointer.</param>
		/// <param name="offset">Byte offset to start.</param>
		/// <param name="length">Number of bytes to set.</param>
		public static void Set( ref byte[] dest, int offset, int length )
		{
			for ( int i = 0; i < length; i++ )
			{
				dest[ i + offset ] = 0;
			}
		}

		public static int SizeOf( Type type )
		{
			return Marshal.SizeOf( type );
		}

        #region Pinned Object Access

		private static Dictionary<object, GCHandle> _pinnedReferences = new Dictionary<object, GCHandle>();
		public static IntPtr PinMemory( object obj )
		{
			GCHandle handle;
			if ( _pinnedReferences.ContainsKey( obj ) )
			{
				handle = _pinnedReferences[ obj ];
			}
			else
			{
				handle = GCHandle.Alloc( obj, GCHandleType.Pinned );
				_pinnedReferences.Add( obj, handle );
			}
			return handle.AddrOfPinnedObject();
		}

		public static void UnpinMemory( object key )
		{
			if ( _pinnedReferences.ContainsKey( key ) )
			{
				GCHandle handle = _pinnedReferences[ key ];
				handle.Free();
				_pinnedReferences.Remove( key );
			}
			else
			{
				// LogManager.Instance.Write("MemoryManager : Attempted to unpin memory that wasn't pinned.");
			}
		}

        #endregion Pinned Object Access

		public IMemoryBuffer Allocate( IntPtr pinnedMemory, long size )
		{
			UnsafeMemoryBuffer buffer = new UnsafeMemoryBuffer( this, pinnedMemory );
			this._memoryPool.Add( buffer );
			return buffer;
		}

#endif

        public IMemoryBuffer Allocate( Array data )
		{
			Type t = data.GetValue( 0 ).GetType();

			return null;
		}

		public IMemoryBuffer Allocate<T>( T[] buf )
			where T : struct
		{
			SafeMemoryBuffer<T> buffer = new SafeMemoryBuffer<T>( this, buf );
			this._memoryPool.Add( buffer );
			return buffer;
		}

		public IMemoryBuffer Allocate<T>( long size )
			where T : struct
		{
			SafeMemoryBuffer<T> buffer = new SafeMemoryBuffer<T>( this, size );
			this._memoryPool.Add( buffer );
			return buffer;
		}

		public void Deallocate( IMemoryBuffer buffer )
		{
			if ( _memoryPool.Contains( buffer ) )
			{
				_memoryPool.Remove( buffer );
				buffer.Dispose();
			}
		}

		private class Byte2IntBitConverter : IBitConverter
		{
			public Array Convert( Array buffer, int startIndex )
			{
				int[] retVal;
				int size = buffer.Length / 4;
				retVal = new int[ size ];
				for ( int index = startIndex; index < size; index++, startIndex += 4 )
				{
					retVal[ index ] = BitConverter.ToInt32( (byte[])buffer, startIndex );
				}
				return retVal;
			}
		}

		private class Byte2SingleBitConverter : IBitConverter
		{
			public Array Convert( Array buffer, int startIndex )
			{
				float[] retVal;
				int size = buffer.Length / 4;
				retVal = new float[ size ];
				for ( int index = startIndex; index < size; index++, startIndex += 4 )
				{
					retVal[ index ] = BitConverter.ToInt32( (byte[])buffer, startIndex );
				}
				return retVal;
			}
		}

		private class Int2ByteBitConverter : IBitConverter
		{
			public Array Convert( Array buffer, int startIndex )
			{
				byte[] retVal;
				int size = buffer.Length * 4;
				retVal = new byte[ size ];
				for ( int index = startIndex; index < buffer.Length; index++, startIndex += 4 )
				{
					byte[] tmp = BitConverter.GetBytes( (int)( buffer.GetValue( index ) ) );
					for ( int i = 0; i < 4; i++ )
						retVal[ i + startIndex ] = tmp[ i ];
				}
				return retVal;
			}
		}

	}
}