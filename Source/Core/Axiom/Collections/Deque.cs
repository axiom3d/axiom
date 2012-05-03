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
using System.Collections;
using System.Collections.Generic;
using Axiom.Math;

#endregion Namespace Declarations

namespace Axiom.Collections
{
	/// <summary>
	/// A double-ended queue. Provides fast insertion and removal from
	/// the head or tail end,
	/// and fast indexed access.
	/// </summary>
	/// <typeparam name="T">The type of item to store in the deque.</typeparam>
	public class Deque<T> : ICollection<T>
	{
		#region Constants

		private const int MIN_CAPACITY = 4;

		#endregion Constants

		#region Fields

		private int _capacity = MIN_CAPACITY;
		private int _count;
		private T[] _data;
		private int _head = 0;
		private int _tail = 0;

		#endregion Fields

		#region Constructors

		/// <summary>
		/// Creates a new deque.
		/// </summary>
		public Deque()
		{
			_data = new T[_capacity];
		}

		/// <summary>
		/// Creates a new deque.
		/// </summary>
		/// <param name="capacity">The initial capacity to give the
		///deque.</param>
		public Deque( int capacity )
			: this()
		{
			_capacity = capacity;
		}

		/// <summary>
		/// Creates a new deque from a collection.
		/// </summary>
		/// <param name="collection">A collection of items of type T.</param>
		public Deque( ICollection<T> collection )
			: this()
		{
			Capacity = collection.Count;
			foreach ( var item in collection )
			{
				Add( item );
			}
		}

		#endregion Constructors

		#region Instance Properties

		/// <summary>
		/// Gets or sets the capacity of the deque. If the count exceeds the
		/// capacity, the capacity will be automatically increased.
		/// </summary>
		public int Capacity
		{
			get
			{
				return _capacity;
			}
			set
			{
				var previousCapacity = _capacity;
				_capacity = System.Math.Max( value, System.Math.Max( _count, MIN_CAPACITY ) );
				var temp = new T[_capacity];
				if ( _tail > _head )
				{
					Array.Copy( _data, _head, temp, 0, _tail + 1 - _head );
					_tail -= _head;
					_head = 0;
				}
				else
				{
					Array.Copy( _data, 0, temp, 0, _tail + 1 );
					var length = previousCapacity - _head;
					Array.Copy( _data, _head, temp, _capacity - length, length );
					_head = _capacity - length;
				}
				_data = temp;
			}
		}

		#endregion Instance Properties

		#region Instance Indexers

		/// <summary>
		/// Gets the item at the specified position.
		/// </summary>
		/// <param name="position">The position of the item to return.</param>
		/// <returns>An item of type T.</returns>
		public T this[ int position ]
		{
			get
			{
				if ( position >= _count )
				{
					throw new ArgumentOutOfRangeException( "position" );
				}
				return _data[ ( _head + position )%_capacity ];
			}
		}

		#endregion Instance Indexers

		#region Instance Methods

		/// <summary>
		/// Adds an item to the head of the deque.
		/// </summary>
		/// <param name="item">The item to be added.</param>
		public void AddToHead( T item )
		{
			_count++;
			if ( _count > _capacity )
			{
				Capacity *= 2;
			}
			if ( _count > 1 )
			{
				_head = Decrement( _head );
			}
			_data[ _head ] = item;
		}

		/// <summary>
		/// Adds an item to the tail end of the deque.
		/// </summary>
		/// <param name="item">The item to be added.</param>
		public void AddToTail( T item )
		{
			_count++;
			if ( _count > _capacity )
			{
				Capacity *= 2;
			}
			if ( _count > 1 )
			{
				_tail = Increment( _tail );
			}
			_data[ _tail ] = item;
		}

		/// <summary>
		/// Gets the item at the head of the deque.
		/// </summary>
		/// <returns>An item of type T.</returns>
		public T PeekHead()
		{
			return _data[ _head ];
		}

		/// <summary>
		/// Gets the item at the tail of the deque.
		/// </summary>
		/// <returns>An item of type T.</returns>
		public T PeekTail()
		{
			return _data[ _tail ];
		}

		/// <summary>
		/// Gets and removes an item at the specified index.
		/// </summary>
		/// <param name="index">The index at which to remove the item.</param>
		/// <returns>An item of type T.</returns>
		public T RemoveAt( int index )
		{
			if ( index >= _count )
			{
				throw new ArgumentOutOfRangeException( "index" );
			}

			_count--;
			var i = ( _head + index )%_capacity;
			var item = _data[ i ];
			if ( i < _head )
			{
				Array.Copy( _data, i + 1, _data, i, _tail - i );
				_data[ _tail ] = default( T );
				_tail = Decrement( _tail );
			}
			else
			{
				Array.Copy( _data, _head, _data, _head + 1, i - _head );
				_data[ _head ] = default( T );
				_head = Increment( _head );
			}
			return item;
		}

		/// <summary>
		/// Removes an item from the head of the deque.
		/// </summary>
		/// <returns>An item of type T.</returns>
		public T RemoveFromHead()
		{
			_count--;
			if ( _count < 0 )
			{
				throw new InvalidOperationException( "DequeEmptyException" );
			}
			var item = _data[ _head ];
			_data[ _head ] = default( T );
			_head = Increment( _head );
			return item;
		}

		/// <summary>
		/// Removes an item from the tail of the deque.
		/// </summary>
		/// <returns>An item of type T.</returns>
		public T RemoveFromTail()
		{
			_count--;
			if ( _count < 0 )
			{
				throw new InvalidOperationException( "DequeEmptyException" );
			}
			var item = _data[ _tail ];
			_data[ _tail ] = default( T );
			if ( _count > 1 )
			{
				_tail = Decrement( _tail );
			}
			return item;
		}

		/// <summary>
		/// Creates an array of the items in the deque.
		/// </summary>
		/// <returns>An array of type T.</returns>
		public T[] ToArray()
		{
			var array = new T[_count];
			CopyTo( array, 0 );
			return array;
		}

		/// <summary>
		/// Decrements (and wraps if necessary) an index
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		private int Decrement( int index )
		{
			return ( index + _capacity - 1 )%_capacity;
		}

		/// <summary>
		/// Increments (and wraps if necessary) an index
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		private int Increment( int index )
		{
			return ( index + 1 )%_capacity;
		}

		#endregion Instance Methods

		#region ICollection<T> Members

		/// <summary>
		/// Gets the number of items in the deque.
		/// </summary>
		public int Count
		{
			get
			{
				return _count;
			}
		}

		/// <summary>
		/// Copies the deque to an array at the specified index.
		/// </summary>
		/// <param name="array">One dimensional array that is the
		/// destination of the copied elements.</param>
		/// <param name="arrayIndex">The zero-based index at which
		/// copying begins.</param>
		public void CopyTo( T[] array, int arrayIndex )
		{
			if ( _count == 0 )
			{
				return;
			}
			if ( _head < _tail )
			{
				Array.Copy( _data, _head, array, arrayIndex, _tail + 1 - _head );
			}
			else
			{
				var headLength = _capacity - _head;
				Array.Copy( _data, _head, array, arrayIndex, headLength );
				Array.Copy( _data, 0, array, arrayIndex + headLength, _tail + 1 );
			}
		}

		/// <summary>
		/// Clears all items from the deque.
		/// </summary>
		public void Clear()
		{
			Array.Clear( _data, 0, _capacity );
			_head = 0;
			_tail = 0;
			_count = 0;
		}

		/// <summary>
		/// Gets an enumerator for the deque.
		/// </summary>
		/// <returns>An IEnumerator of type T.</returns>
		public IEnumerator<T> GetEnumerator()
		{
			for ( var i = 0; i < _count; i++ )
			{
				yield return this[ i ];
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		/// <summary>
		/// Adds an item to the tail of the deque.
		/// </summary>
		/// <param name="item"></param>
		public void Add( T item )
		{
			AddToTail( item );
		}

		/// <summary>
		/// Checks to see if the deque contains the specified item.
		/// </summary>
		/// <param name="item">The item to search the deque for.</param>
		/// <returns>A boolean, true if deque contains item.</returns>
		public bool Contains( T item )
		{
			for ( var i = 0; i < Count; i++ )
			{
				if ( this[ i ].Equals( item ) )
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Gets whether or not the deque is readonly.
		/// </summary>
		public bool IsReadOnly
		{
			get
			{
				return false;
			}
		}

		/// <summary>
		/// Removes an item from the deque.
		/// </summary>
		/// <param name="item">The item to be removed.</param>
		/// <returns>Boolean true if the item was removed.</returns>
		public bool Remove( T item )
		{
			for ( var i = 0; i < Count; i++ )
			{
				if ( this[ i ].Equals( item ) )
				{
					RemoveAt( i );
					return true;
				}
			}
			return false;
		}

		#endregion ICollection<T> Members
	}
}