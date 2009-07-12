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

#endregion Namespace Declarations

namespace Axiom.Collections
{

    /// <summary>
    ///		Serves as a basis for strongly typed collections in the engine.
    /// </summary>
    public abstract class AxiomCollection<K, T>
        : /*SortedList<K, T>//,*/ ICollection, IEnumerable, ICollection<T>, IEnumerable<T>
    {

        #region Custom


        Dictionary<K, T> _dictionary;
        ValuesCollection _values;
        static protected int nextUniqueKeyCounter;
        List<K> _list;

        const int INITIAL_CAPACITY = 60;

        #region Constructors

        /// <summary>
        ///		
        /// </summary>
        public AxiomCollection()
        {
            _dictionary = new Dictionary<K, T>( INITIAL_CAPACITY );
            _values = new ValuesCollection( this );
            _list = new List<K>( INITIAL_CAPACITY );

        }

        /// <summary>
        ///		
        /// </summary>
        /// <param name="capacity"></param>
        public AxiomCollection( int capacity )
        {
            _dictionary = new Dictionary<K, T>( capacity );
            _values = new ValuesCollection( this );
            _list = new List<K>( capacity );
        }


        #endregion


        public T this[ K key ]
		{
			get
			{

                return _dictionary[ key ];

			}
			set
			{
                _dictionary[ key ] = value;
                if ( !_list.Contains( key ) )
                {
                    _list.Add( key );
			}
		}
        }

        public void Add( K key, T value )
		{
            _list.Add( key );
            _dictionary.Add( key, value );
        }

        public bool Remove( K key )
			{
            _dictionary.Remove( key );
            _list.Remove( key );
            return true;
			}

        /// <summary>
        /// Removes the item from the collection.
        /// </summary>
        /// <param name="item"></param>
        public virtual bool Remove( T item )
        {
            foreach ( KeyValuePair<K, T> i in this._dictionary )
            {
                if ( i.Value.Equals( item ) )
                {
                    _dictionary.Remove( i.Key );
                    _list.Remove( i.Key );
                    return true;
            }
        }
            return false;
        }

        public bool RemoveAt( int index )
        {
            _dictionary.Remove( _list[ index ] );
            _list.RemoveAt( index );
            return true;
        }

        public int IndexOf( K key )
        {
            return _list.IndexOf( key );
        }

        /// <summary>
        /// Very slow method.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public int IndexOf( T item )
        {
            foreach ( KeyValuePair<K, T> i in _dictionary )
            {
                if ( i.Value.Equals(item) )
                {
                    return _list.IndexOf( i.Key );
            }
            }
            return -1;
        }

        public IList<T> Values
            {
            get
            {
                return _values;
            }

        }

        public Dictionary<K, T>.KeyCollection Keys
        {
            get { return _dictionary.Keys; }
        }

        /// <summary>
        /// Tests if there is a dupe entry in here.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool ContainsKey( K key )
        {
            return _dictionary.ContainsKey( key );
        }

        /// <summary>
        /// Accepts an unnamed object and names it manually.
        /// </summary>
        /// <param name="item"></param>
        public virtual void Add( T item )
        {

            K key = default( K );

            if ( typeof( K ) == typeof( string ) )
            {
                key = (K)Convert.ChangeType( typeof( T ).Name + nextUniqueKeyCounter++, typeof( K ) );
        }
            else
            {
                key = (K)Convert.ChangeType( nextUniqueKeyCounter++, typeof( K ) );
            }
            Add( key, item );
        }

        #region ICollection<T> Members

        void ICollection<T>.Add( T item )
        {
            Add( item );
        }

        /// <summary>
        ///		Clears all objects from the collection.
        /// </summary>
        public void Clear()
        {
            _dictionary.Clear();
            _list.Clear();
        }

        public bool Contains( T item )
        {
            return _dictionary.ContainsValue( item );
        }

        void ICollection<T>.CopyTo( T[] array, int arrayIndex )
        {
            _dictionary.Values.CopyTo( array, arrayIndex );
        }

        public int Count
        {
            get { return _dictionary.Count; }
        }

        bool ICollection<T>.IsReadOnly
		{
            get { return ( _dictionary as ICollection<T> ).IsReadOnly; }
        }

        bool ICollection<T>.Remove( T item )
        {
            return Remove( item );
		}

        #endregion

        #region IEnumerable<T> Members

        public IEnumerator<T> GetEnumerator()
        {
            return _dictionary.Values.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region ICollection Members

        void ICollection.CopyTo( Array array, int index )
        {
            _dictionary.Values.CopyTo( (T[])array, index );
        }

        int ICollection.Count
        {
            get { return Count; }
        }

        bool ICollection.IsSynchronized
            {
            get { return ( _dictionary.Values as ICollection ).IsSynchronized; }
            }

        object ICollection.SyncRoot
        {
            get { return ( _dictionary.Values as ICollection ).SyncRoot; }
        }

        #endregion

        private class ValuesCollection : IList<T>
        {
            AxiomCollection<K, T> _collection;

            public ValuesCollection( AxiomCollection<K, T> collection )
            {
                _collection = collection;
            }
            public T this[ int index ]
        {
            get
            {
                    return _collection._dictionary[ _collection._list[ index ] ];
            }
                set
                {
                    throw new NotImplementedException();
        }
            }

            #region IList<T> Members

            int IList<T>.IndexOf( T item )
            {
                throw new NotImplementedException();
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
                get { return _collection._dictionary.Count; }
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

            IEnumerator<T> IEnumerable<T>.GetEnumerator()
            {
                return _collection._dictionary.Values.GetEnumerator();
            }

            #endregion

            #region IEnumerable Members

            IEnumerator IEnumerable.GetEnumerator()
                {
                throw new NotImplementedException();
                }

            #endregion
            }



        #endregion



        //#region Encapsulated



        ///// <summary></summary>
        //private SortedList<K, T> objectList;

        //static protected int nextUniqueKeyCounter;

        //const int INITIAL_CAPACITY = 60;

        //#region Constructors

        ///// <summary>
        /////		
        ///// </summary>
        //public AxiomCollection()
        //{
        //    objectList = new SortedList<K, T>(INITIAL_CAPACITY);

        //}

        ///// <summary>
        /////
        ///// </summary>
        ///// <param name="capacity"></param>
        //public AxiomCollection(int capacity)
        //{
        //    objectList = new SortedList<K, T>(capacity);
        //}


        //#endregion

        //public IList<K> Keys
        //{
        //    get { return objectList.Keys; }
        //}

        //public IList<T> Values
        //{
        //    get { return objectList.Values; }
        //}

        ///// <summary>
        /////		
        ///// </summary>
        //public T this[K key]
        //{
        //    get { return objectList[key]; }
        //    set { objectList[key] = value; }
        //}

        ///// <summary>
        ///// Adds a named object to the collection.
        ///// </summary>
        ///// <param name="key"></param>
        ///// <param name="item"></param>
        //public virtual void Add(K key, T item)
        //{
        //    objectList.Add(key, item);
        //}

        ///// <summary>
        ///// Accepts an unnamed object and names it manually.
        ///// </summary>
        ///// <param name="item"></param>
        //public virtual void Add(T item)
        //{

        //    K key = default(K);

        //    if (typeof(K) == typeof(string))
        //    {
        //        key = (K)Convert.ChangeType(typeof(T).Name + nextUniqueKeyCounter++, typeof(K));
        //    }
        //    else
        //    {
        //        key = (K)Convert.ChangeType(nextUniqueKeyCounter++, typeof(K));
        //    }
        //    objectList.Add(key, item);

        //}

        ///// <summary>
        ///// Removes the item from the collection by key.
        ///// </summary>
        ///// <param name="item"></param>
        //public virtual bool Remove(K key)
        //{
        //    return objectList.Remove(key);
        //}

        ///// <summary>
        ///// Removes the item from the collection.
        ///// </summary>
        ///// <param name="item"></param>
        //public virtual bool Remove(T item)
        //{
        //    int i = objectList.IndexOfValue(item);
        //    if (i > -1)
        //    {
        //        objectList.RemoveAt(i);
        //        return true;
        //    }
        //    else
        //    {
        //        return false;
        //    }
        //}

        ///// <summary>
        ///// Removes an item at the specified index.
        ///// </summary>
        ///// <param name="index"></param>
        //public virtual void RemoveAt(int index)
        //{
        //    objectList.RemoveAt(index);
        //}

        ///// <summary>
        ///// Tests if there is a dupe entry in here.
        ///// </summary>
        ///// <param name="key"></param>
        ///// <returns></returns>
        //public bool ContainsKey(K key)
        //{
        //    return objectList.ContainsKey(key);
        //}

        //public int IndexOf(K key)
        //{
        //    return objectList.Keys.IndexOf(key);
        //}

        //#region ICollection<T> Members

        //void ICollection<T>.Add(T item)
        //{
        //    Add(item);
        //}

        ///// <summary>
        ///// Clears all objects from the collection.
        ///// </summary>
        //public void Clear()
        //{
        //    objectList.Clear();
        //}

        //public bool Contains(T item)
        //{
        //    return objectList.Values.Contains(item);
        //}

        //void ICollection<T>.CopyTo(T[] array, int arrayIndex)
        //{
        //    objectList.Values.CopyTo(array, arrayIndex);
        //}

        //public int Count
        //{
        //    get { return objectList.Count; }
        //}

        //bool ICollection<T>.IsReadOnly
        //{
        //    get { return (objectList as ICollection<T>).IsReadOnly; }
        //}

        //bool ICollection<T>.Remove(T item)
        //{
        //    return Remove(item);
        //}

        //#endregion

        //#region IEnumerable<T> Members

        //public IEnumerator<T> GetEnumerator()
        //{
        //    return objectList.Values.GetEnumerator();
        //}

        //#endregion

        //#region IEnumerable Members

        //IEnumerator IEnumerable.GetEnumerator()
        //{
        //    return GetEnumerator();
        //}

        //#endregion

        //#region ICollection Members

        //void ICollection.CopyTo(Array array, int index)
        //{
        //    objectList.Values.CopyTo((T[])array, index);
        //}

        //int ICollection.Count
        //{
        //    get { return Count; }
        //}

        //bool ICollection.IsSynchronized
        //{
        //    get { return (objectList.Values as ICollection).IsSynchronized; }
        //}

        //object ICollection.SyncRoot
        //{
        //    get { return (objectList.Values as ICollection).SyncRoot; }
        //}

        //#endregion





        //#endregion




        //#region Derived



        ////############### Override

        //static protected int nextUniqueKeyCounter;

        ///// <summary>
        ///// Accepts an unnamed object and names it manually.
        ///// </summary>
        ///// <param name="item"></param>
        //public virtual void Add(T item)
        //{

        //    K key = default(K);

        //    if (typeof(K) == typeof(string))
        //    {
        //        key = (K)Convert.ChangeType(typeof(T).Name + nextUniqueKeyCounter++, typeof(K));
        //    }
        //    else
        //    {
        //        key = (K)Convert.ChangeType(nextUniqueKeyCounter++, typeof(K));
        //    }
        //    base.Add(key, item);

        //}

        //public int IndexOf(K key)
        //{
        //    return base.Keys.IndexOf(key);
        //}

        //public bool Contains(T item)
        //{
        //    return base.ContainsValue(item);
        //}

        ///// <summary>
        ///// Removes the item from the collection.
        ///// </summary>
        ///// <param name="item"></param>
        //public virtual bool Remove(T item)
        //{
        //    foreach (T i in base.Values)
        //    {
        //        if (i.Equals(item))
        //        {
        //            return true;
        //        }
        //    }
        //    return false;
        //}

        //#region IEnumerable<T> Members

        //public IEnumerator<T> GetEnumerator()
        //{
        //    return base.Values.GetEnumerator();
        //}

        //#endregion

        //#endregion

    }
}


