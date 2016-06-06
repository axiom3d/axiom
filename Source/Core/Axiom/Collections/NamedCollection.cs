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
using System.Collections;
using System.Collections.Generic;

using Axiom.Core;

#endregion Namespace Declarations

namespace Axiom.Collections
{
	///<summary>
	///  Serves as a basis for strongly typed collections in the engine. Items of this collection must implement INamable interface.
	///</summary>
	public abstract class NamedCollection<T> : ICollection, IEnumerable, ICollection<T>, IEnumerable<T>
		where T : Axiom.Core.INamable
	{
		#region INamable

		private readonly Dictionary<string, T> _dictionary;
		private readonly ValuesCollection _values;
		private readonly List<INamable> _list;

		private const int INITIAL_CAPACITY = 60;

		#region Constructors

		///<summary>
		///</summary>
		public NamedCollection()
		{
			this._dictionary = new Dictionary<string, T>( INITIAL_CAPACITY );
			this._values = new ValuesCollection( this );
			this._list = new List<INamable>( INITIAL_CAPACITY );
		}

		///<summary>
		///</summary>
		///<param name="capacity"> </param>
		public NamedCollection( int capacity )
		{
			this._dictionary = new Dictionary<string, T>( capacity );
			this._values = new ValuesCollection( this );
			this._list = new List<INamable>( capacity );
		}

		#endregion

		public T this[ string key ]
		{
			get { return this._dictionary[ key ]; }
			set
			{
				this._dictionary[ key ] = value;
				int i = this._list.IndexOf( value );
				if ( i >= 0 )
				{
					this._list[ i ] = value;
				}
				else
				{
					this._list.Add( value );
				}
			}
		}

		public void Add( string key, T value )
		{
			this._list.Add( value );
			this._dictionary.Add( key, value );
		}

		/// <summary>
		///   Removes the value with the specified key from the <see cref="NamedCollection{T}" /> .
		/// </summary>
		/// <param name="key"> The key of the element to remove. </param>
		/// <returns> <c>true</c> if the element is successfully found and removed; otherwise, false. This method returns false if key is not found in the <see
		///    cref="NamedCollection{T}" /> . </returns>
		public bool Remove( string key )
		{
			bool foundAndRemoved = false;

			if ( this._dictionary.ContainsKey( key ) )
			{
				T value = this._dictionary[ key ];
				this._dictionary.Remove( key );
				foundAndRemoved = this._list.Remove( value );
			}

			return foundAndRemoved;
		}

		/// <summary>
		///   Removes the object from the <see cref="NamedCollection{T}" /> .
		/// </summary>
		/// <param name="item"> The object to remove from the <see cref="NamedCollection{T}" /> . The value can be <c>null</c> for reference types. </param>
		/// <returns> <c>true</c> if the element is successfully found and removed; otherwise, false. This method returns false if key is not found in the <see
		///    cref="NamedCollection{T}" /> . </returns>
		public virtual bool Remove( T item )
		{
			bool foundAndRemoved = false;

			if ( this._dictionary.Remove( item.Name ) )
			{
				foundAndRemoved = this._list.Remove( item );
			}

			return foundAndRemoved;
		}

		/// <summary>
		///   Removes the element at the specified index of the <see cref="NamedCollection{T}" /> .
		/// </summary>
		/// <param name="index"> The zero-based index of the element to remove. </param>
		/// <exception cref="ArgumentOutOfRangeException">
		///   <paramref name="index" />
		///   is less than 0 or
		///   <paramref name="index" />
		///   is equal to or greater than
		///   <see cref="NamedCollection{T}.Count" />
		///   .</exception>
		public void RemoveAt( int index )
		{
			this._dictionary.Remove( this._list[ index ].Name );
			this._list.RemoveAt( index );
		}

		public int IndexOf( string key )
		{
			return this._list.IndexOf( this._dictionary[ key ] );
		}

		public int IndexOf( T item )
		{
			return this._list.IndexOf( item );
		}

		public IList<T> Values
		{
			get { return this._values; }
		}

		public Dictionary<string, T>.KeyCollection Keys
		{
			get { return this._dictionary.Keys; }
		}

		/// <summary>
		///   Tests if there is a dupe entry in here.
		/// </summary>
		/// <param name="key"> </param>
		/// <returns> </returns>
		public bool ContainsKey( string key )
		{
			return this._dictionary.ContainsKey( key );
		}

		/// <summary>
		///   Accepts an unnamed object and names it manually.
		/// </summary>
		/// <param name="item"> </param>
		public virtual void Add( T item )
		{
			Add( item.Name, item );
		}

		#region ICollection<T> Members

		//void Add( T item )
		//{
		//    Add( item );
		//}

		/// <summary>
		///   Clears all objects from the collection.
		/// </summary>
		public void Clear()
		{
			this._dictionary.Clear();
			this._list.Clear();
		}

		public bool Contains( T item )
		{
			return this._dictionary.ContainsValue( item );
		}

		public void CopyTo( T[] array, int arrayIndex )
		{
			this._dictionary.Values.CopyTo( array, arrayIndex );
		}

		public int Count
		{
			get { return this._dictionary.Count; }
		}

		public bool IsReadOnly
		{
			get { return ( this._dictionary as ICollection<T> ).IsReadOnly; }
		}

		//bool Remove( T item )
		//{
		//    return Remove( item );
		//}

		#endregion

		#region IEnumerable<T> Members

		public IEnumerator<T> GetEnumerator()
		{
			return this._dictionary.Values.GetEnumerator();
		}

		#endregion

		#region IEnumerable Members

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion

		#region ICollection Members

		public void CopyTo( Array array, int index )
		{
			this._dictionary.Values.CopyTo( (T[]) array, index );
		}

		//int ICollection.Count
		//{
		//    get { return Count; }
		//}

		public bool IsSynchronized
		{
			get { return ( this._dictionary.Values as ICollection ).IsSynchronized; }
		}

		public object SyncRoot
		{
			get { return ( this._dictionary.Values as ICollection ).SyncRoot; }
		}

		#endregion

		private class ValuesCollection : IList<T>
		{
			private readonly NamedCollection<T> _collection;

			public ValuesCollection( NamedCollection<T> collection )
			{
				this._collection = collection;
			}

			public T this[ int index ]
			{
				get { return (T) this._collection._list[ index ]; }
				set { this._collection._list[ index ] = value; }
			}

			#region IList<T> Members

			public int IndexOf( T item )
			{
				return this._collection._list.IndexOf( item );
			}

			void IList<T>.Insert( int index, T item )
			{
				throw new NotImplementedException();
			}

			void IList<T>.RemoveAt( int index )
			{
				throw new NotImplementedException();
			}

			#endregion

			#region ICollection<T> Members

			void ICollection<T>.Add( T item )
			{
				throw new NotImplementedException();
			}

			void ICollection<T>.Clear()
			{
				throw new NotImplementedException();
			}

			bool ICollection<T>.Contains( T item )
			{
				throw new NotImplementedException();
			}

			void ICollection<T>.CopyTo( T[] array, int arrayIndex )
			{
				throw new NotImplementedException();
			}

			int ICollection<T>.Count
			{
				get { return this._collection._dictionary.Count; }
			}

			bool ICollection<T>.IsReadOnly
			{
				get { throw new NotImplementedException(); }
			}

			bool ICollection<T>.Remove( T item )
			{
				throw new NotImplementedException();
			}

			#endregion

			#region IEnumerable<T> Members

			public IEnumerator<T> GetEnumerator()
			{
				return this._collection._dictionary.Values.GetEnumerator();
			}

			#endregion

			#region IEnumerable Members

			IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}

			#endregion
		}

		#endregion
	}
}
