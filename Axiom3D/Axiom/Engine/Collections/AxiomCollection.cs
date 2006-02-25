#region LGPL License
/*
Axiom Game Engine Library
Copyright (C) 2003  Axiom Project Team

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

#region Namespace declarations
using System;
using System.Collections.Generic;
#endregion Namespace declarations

namespace Axiom
{

    /// <summary>
    ///		Serves as a basis for strongly typed collections in the engine.
    /// </summary>
    public abstract class AxiomCollection<K, T> : ICollection<T>, IEnumerable<T>
    {
        /// <summary></summary>
        protected SortedList<K, T> objectList;
        /// <summary></summary>
        protected Object parent;
        static protected int nextUniqueKeyCounter;

        const int INITIAL_CAPACITY = 60;

        #region Constructors

        /// <summary>
        ///		
        /// </summary>
        public AxiomCollection()
        {
            this.parent = null;
            objectList = new SortedList<K, T>(INITIAL_CAPACITY);
        }

        /// <summary>
        ///		
        /// </summary>
        /// <param name="parent"></param>
        public AxiomCollection(object parent)
        {
            this.parent = parent;
            objectList = new SortedList<K, T>(INITIAL_CAPACITY);
        }

        #endregion

        /// <summary>
        ///		
        /// </summary>
        public T this[int index]
        {
            get
            {
                return objectList.Values[index];
            }
            set
            {
                objectList.Values[index] = value;
            }
        }

        public ICollection<T> Values { get { return objectList.Values; } }

        public ICollection<K> Keys { get { return objectList.Keys; } }

        /// <summary>
        ///		
        /// </summary>
        public T this[K key]
        {
            get { return objectList[key]; }
            set { objectList[key] = value; }
        }

        /// <summary>
        ///		Accepts an unnamed object and names it manually.
        /// </summary>
        /// <param name="item"></param>
        public abstract void Add(T item);

        /// <summary>
        ///		Adds a named object to the collection.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="item"></param>
        public void Add(K key, T item)
        {
            objectList.Add(key, item);
        }

        /// <summary>
        ///		Clears all objects from the collection.
        /// </summary>
        public void Clear()
        {
            objectList.Clear();
        }

        /// <summary>
        ///		Removes the item from the collection.
        /// </summary>
        /// <param name="item"></param>
        public void Remove(T item)
        {
            int index = objectList.IndexOfValue(item);

            if (index != -1)
                objectList.RemoveAt(index);
        }

        /// <summary>
        /// Removes the item from the collection
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public void Remove(K key)
        {
            objectList.Remove(key);
        }

        /// <summary>
        ///		Removes an item at the specified index.
        /// </summary>
        /// <param name="index"></param>
        public void RemoveAt(int index)
        {
            objectList.RemoveAt(index);
        }

        /// <summary>
        ///		Tests if there is a dupe entry in here.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool ContainsKey(K key)
        {
            return objectList.ContainsKey(key);
        }

        /// <summary>
        /// Returns the index at which the object with the given key resides
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public int IndexOf(K key)
        {
            return objectList.IndexOfKey(key);
        }

        /// <summary>
        /// Returns the index at which the given object resides
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public int IndexOf(T value)
        {
            return objectList.IndexOfValue(value);
        }

        #region Implementation of ICollection

        //public bool IsSynchronized {
        //    get {
        //        return true; // TODO вернуться сюда позже
        //    }
        //}

        public int Count
        {
            get
            {
                return objectList.Count;
            }
        }

        //public object SyncRoot {
        //    get {
        //        return objectList.SyncRoot;
        //    }
        //}

        #endregion

        #region Implementation of IEnumerator

        public class Enumerator : IEnumerator<T>
        {
            private int position = -1;
            private AxiomCollection<K, T> list;

            public Enumerator(AxiomCollection<K, T> list)
            {
                this.list = list;
            }

            /// <summary>
            ///		Resets the in progress enumerator.
            /// </summary>
            public void Reset()
            {
                // reset the enumerator position
                position = -1;
            }

            /// <summary>
            ///		Moves to the next item in the enumeration if there is one.
            /// </summary>
            /// <returns></returns>
            public bool MoveNext()
            {
                position += 1;

                if (position >= list.objectList.Count)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }

            /// <summary>
            ///		Returns the current object in the enumeration.
            /// </summary>
            public T Current
            {
                get
                {
                    return list.objectList.Values[position];
                }
            }

            #region IDisposable Members

            public void Dispose()
            {
                this.Reset();
            }

            #endregion

            #region IEnumerator Members

            object System.Collections.IEnumerator.Current
            {
                get { return list.objectList.Values[position]; }
            }

            #endregion
        }
        #endregion


        #region ICollection<T> Members


        public bool Contains(T item)
        {
            return objectList.ContainsValue(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            objectList.Values.CopyTo(array, arrayIndex);
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        bool ICollection<T>.Remove(T item)
        {
            int idx = objectList.IndexOfValue(item);

            if (idx < 0)
                return false;

            K key = objectList.Keys[idx];

            return objectList.Remove(key);
        }

        #endregion

        #region IEnumerable<T> Members

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            Enumerator etr = new Enumerator(this);

            return etr;
        }

        #endregion

        #region IEnumerable<T> Members

        public IEnumerator<T> GetEnumerator()
        {
            Enumerator etr = new Enumerator(this);

            return etr;
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            Enumerator etr = new Enumerator(this);

            return etr;
        }

        #endregion
    }
}
