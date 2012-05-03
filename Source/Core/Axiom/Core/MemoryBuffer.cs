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

#endregion Namespace Declarations

namespace Axiom.Core
{
	public interface IMemoryBuffer : IDisposable
	{
	}

	public interface IBitConverter
	{
		Array Convert( Array buffer, int startIndex );
	}

	public class MemoryManager : Singleton<MemoryManager>
	{
		private readonly List<IMemoryBuffer> _memoryPool = new List<IMemoryBuffer>();
		private static readonly Dictionary<Type, IBitConverter> _bitConverters;

		public Dictionary<Type, IBitConverter> BitConverters
		{
			get
			{
				return _bitConverters;
			}
		}

		static MemoryManager()
		{
			_bitConverters = new Dictionary<Type, IBitConverter>()
			                 {
			                 	{
			                 		typeof ( int ), new IntBitConverter()
			                 		},
			                 	{
			                 		typeof ( float ), new SingleBitConverter()
			                 		}
			                 };
		}

		public MemoryBuffer<T> Allocate<T>( long size ) where T : struct
		{
			var buffer = new MemoryBuffer<T>( this, size );
			_memoryPool.Add( buffer );
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

		protected override void dispose( bool disposeManagedResources )
		{
			base.dispose( disposeManagedResources );
		}

		private class IntBitConverter : IBitConverter
		{
			public Array Convert( Array buffer, int startIndex )
			{
				int[] retVal;
				var size = buffer.Length/4;
				retVal = new int[size];
				for ( var index = startIndex; index < size; index++, startIndex += 4 )
				{
					retVal[ index ] = BitConverter.ToInt32( (byte[])buffer, startIndex );
				}
				return retVal;
			}
		}

		private class SingleBitConverter : IBitConverter
		{
			public Array Convert( Array buffer, int startIndex )
			{
				float[] retVal;
				var size = buffer.Length/4;
				retVal = new float[size];
				for ( var index = startIndex; index < size; index++, startIndex += 4 )
				{
					retVal[ index ] = BitConverter.ToInt32( (byte[])buffer, startIndex );
				}
				return retVal;
			}
		}
	}

	public class MemoryBuffer<T> : DisposableObject, IMemoryBuffer
		where T : struct
	{
		private T[] _buffer;

		public MemoryManager Owner { get; private set; }

		public T this[ long index ]
		{
			get
			{
				return _buffer[ index ];
			}

			set
			{
				_buffer[ index ] = value;
			}
		}

		internal MemoryBuffer( MemoryManager owner )
		{
			IsDisposed = false;
			Owner = owner;
		}

		internal MemoryBuffer( MemoryManager owner, long size )
			: this( owner )
		{
			_buffer = new T[size];
		}

		public TDestType[] AsArray<TDestType>()
		{
			if ( Owner.BitConverters.ContainsKey( typeof ( TDestType ) ) )
			{
				return (TDestType[])( Owner.BitConverters[ typeof ( TDestType ) ].Convert( _buffer, 0 ) );
			}
			return new TDestType[0];
		}

		#region IDisposable Implementation

		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				if ( disposeManagedResources )
				{
					_buffer = null;
				}

				// There are no unmanaged resources to release, but
				// if we add them, they need to be released here.
			}
		}

		#endregion IDisposable Implementation
	}
}