#region LGPL License

// Axiom Graphics Engine Library
// Copyright (C) 2003-2009 Axiom Project Team
// 
// The overall design, and a majority of the core engine and rendering code 
// contained within this library is a derivative of the open source Object Oriented 
// Graphics Engine OGRE, which can be found at http://ogre.sourceforge.net.  
// Many thanks to the OGRE team for maintaining such a high quality project.
// 
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
// 
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
// Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

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

#endregion

namespace Axiom.Collections
{
    /// <summary>
    ///     The MultiMap is a C# conversion of the std::buckets container from the C++ 
    ///     standard library.  
    /// </summary>
    /// <remarks>
    ///     A buckets allows multiple values per key, unlike IDictionary<TKey, TValue> which only allows
    ///     unique keys and only a single value per key.  Multiple values assigned to the same
    ///     key are placed in a "bucket", which in this case is a List<T>.
    ///     <p/>
    ///     An example of values in a buckets would look like this:
    ///     Key     Value
    ///     "a"     "Alan"
    ///     "a"     "Adam"
    ///     "b"     "Brien"
    ///     "c"     "Chris"
    ///     "c"     "Carl"
    ///     etc
    ///     <p/>
    ///     Currently, enumeration is the only way to iterate through the values, which is
    ///     more pratical in terms of how the MultiMap works internally anyway.  Intial testing showed
    ///     that inserting and iterating through 100,000 items, the Inserts took ~260ms and a full
    ///     enumeration of them all (with unboxing of the value type stored in the buckets) took between 16-30ms.
    /// </remarks>
    public class MultiMap<K, T> : IDictionary<K, List<T>>,
                             ICollection<KeyValuePair<K, List<T>>>,
                             IEnumerable<KeyValuePair<K, List<T>>>,
                             IDictionary, ICollection, IEnumerable, IEnumerable<T>, IEnumerable<List<T>>
    {
        #region Fields

        private Dictionary<K, List<T>> buckets;

        /// <summary>
        ///     Number of total items currently in this buckets.
        /// </summary>
        private int count;

        #endregion

        #region Constructors

        /// <summary>
        ///     Default constructor.
        /// </summary>
        public MultiMap()
        {
            this.buckets = new Dictionary<K, List<T>>();
        }

        public MultiMap(IEqualityComparer<K> comparer)
        {
            this.buckets = new Dictionary<K, List<T>>(comparer);
        }

        #endregion

        #region Instance Properties

        public int Count
        {
            get { return this.buckets.Count; }
        }

        /// <summary>
        ///		Gets the number of keys in this map.
        /// </summary>
        public int KeyCount
        {
            get { return this.buckets.Count; }
        }

        public int TotalCount
        {
            get { return this.count; }
        }

        #endregion

        #region Instance Methods

        /// <summary>
        ///     Inserts a value into a bucket that is specified by the
        ///     key.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="val"></param>
        public void Add(K key, T value)
        {
            List<T> container = null;

            if (!this.buckets.ContainsKey(key))
            {
                container = new List<T>();
                this.buckets.Add(key, container);
            }
            else
            {
                container = this.buckets[key];
            }

            // TODO: Doing the contains check is extremely slow, so for now duplicate items are allowed
            //if (!container.Contains(value))
            //{
            container.Add(value);
            this.count++;
            //}
        }

        /// <summary>
        ///     Gets the count of objects mapped to the specified key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public int BucketCount(K key)
        {
            if (this.buckets.ContainsKey(key))
            {
                return this.buckets[key].Count;
            }

            return 0;
        }

        public void Clear()
        {
            this.buckets.Clear();
            this.count = 0;
        }

        public void Clear(K key)
        {
            if (this.buckets.ContainsKey(key))
            {
                this.count -= this.buckets[key].Count;
            }
            this.buckets[key].Clear();
        }

        /// <summary>
        ///     Given a key, Find will return an IEnumerator that allows
        ///     you to iterate over all items in the bucket associated
        ///     with the key.
        /// </summary>
        /// <param name="key">Key for look for.</param>
        /// <returns>IEnumerator to go through the items assigned to the key.</returns>
        public IEnumerator<T> Find(K key)
        {
            if (this.buckets.ContainsKey(key))
            {
                return this.buckets[key].GetEnumerator();
                //int length = buckets[key].Count;
                //IList<T> bucket = buckets[key];
                //for (int i = 0; i < length; i++)
                //{
                //    yield return bucket[i];
                //}
            }
            return null;
        }

        public List<T> FindBucket(K key)
        {
            if (!this.buckets.ContainsKey(key))
            {
                return null;
            }
            return this.buckets[key];
        }

        /// <summary>
        ///     Given a key, FindFirst will return the first item in the bucket
        ///     associated with the key.
        /// </summary>
        /// <param name="key">Key to look for.</param>
        public object FindFirst(K key)
        {
            if (!this.buckets.ContainsKey(key))
            {
                return null;
            }
            else
            {
                return (this.buckets[key])[0];
            }
        }

        public IEnumerator<KeyValuePair<K, List<T>>> GetBucketsEnumerator()
        {
            foreach (KeyValuePair<K, List<T>> item in this.buckets)
            {
                yield return item;
            }
        }

        private void Add(K key, IList<T> value)
        {
            List<T> container = null;

            if (!this.buckets.ContainsKey(key))
            {
                container = new List<T>();
                this.buckets.Add(key, container);
            }
            else
            {
                container = this.buckets[key];
            }

            foreach (T i in value)
            {
                // TODO: Doing the contains check is extremely slow, so for now duplicate items are allowed
                if (!container.Contains(i))
                {
                    container.Add(i);
                    this.count++;
                }
            }
        }

        #endregion

        #region IDictionary Members

        void IDictionary.Add(object key, object value)
        {
            if (key is K & value is IList<T>)
            {
                this.Add((K)key, (IList<T>)value);
            }
        }

        void IDictionary.Clear()
        {
            this.Clear();
        }

        bool IDictionary.Contains(object key)
        {
            if (key is K)
            {
                return this.ContainsKey((K)key);
            }
            return false;
        }

        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        bool IDictionary.IsFixedSize
        {
            get { return (this.buckets as IDictionary).IsFixedSize; }
        }

        bool IDictionary.IsReadOnly
        {
            get { return (this.buckets as IDictionary).IsReadOnly; }
        }

        ICollection IDictionary.Keys
        {
            get { return this.buckets.Keys; }
        }

        void IDictionary.Remove(object key)
        {
            if (key is K)
            {
                this.Remove((K)key);
            }
        }

        ICollection IDictionary.Values
        {
            get { return this.buckets.Values; }
        }

        object IDictionary.this[object key]
        {
            get
            {
                if (key is K)
                {
                    return this[(K)key];
                }
                return null;
            }
            set
            {
                if (value is IList<T>)
                {
                    this[(K)key] = (List<T>)value;
                }
                else
                {
                    throw new ArgumentException("The key must be of type " + typeof(List<T>).ToString(), "value");
                }
            }
        }

        void ICollection.CopyTo(Array array, int index)
        {
            throw new NotImplementedException();
        }

        int ICollection.Count
        {
            get { return this.count; }
        }

        bool ICollection.IsSynchronized
        {
            get { return (this.buckets as ICollection).IsSynchronized; }
        }

        object ICollection.SyncRoot
        {
            get { return (this.buckets as ICollection).SyncRoot; }
        }

        #endregion

        #region IDictionary<K,List<T>> Members

        void IDictionary<K, List<T>>.Add(K key, List<T> value)
        {
            Add(key, value);
        }

        public bool ContainsKey(K key)
        {
            return this.buckets.ContainsKey(key);
        }

        public ICollection<K> Keys
        {
            get { return this.buckets.Keys; }
        }

        public bool Remove(K key)
        {
            bool removed = this.buckets.Remove(key);
            if (removed)
            {
                this.count--;
                return true;
            }
            return false;
        }

        public bool TryGetValue(K key, out List<T> value)
        {
            List<T> tvalue;
            this.buckets.TryGetValue(key, out tvalue);
            value = tvalue;
            if (tvalue == null)
            {
                return false;
            }
            return true;
        }

        public ICollection<List<T>> Values
        {
            get { return this.buckets.Values; }
        }

        public List<T> this[K key]
        {
            get { return this.buckets[key]; }
            set { this.buckets[key] = value; }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        void ICollection<KeyValuePair<K, List<T>>>.Add(KeyValuePair<K, List<T>> item)
        {
            Add(item.Key, item.Value);
        }

        void ICollection<KeyValuePair<K, List<T>>>.Clear()
        {
            this.Clear();
        }

        bool ICollection<KeyValuePair<K, List<T>>>.Contains(KeyValuePair<K, List<T>> item)
        {
            return this.buckets.ContainsKey(item.Key);
        }

        void ICollection<KeyValuePair<K, List<T>>>.CopyTo(KeyValuePair<K, List<T>>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        int ICollection<KeyValuePair<K, List<T>>>.Count
        {
            get { return this.Count; }
        }

        bool ICollection<KeyValuePair<K, List<T>>>.IsReadOnly
        {
            get { return (this.buckets as IDictionary).IsReadOnly; }
        }

        bool ICollection<KeyValuePair<K, List<T>>>.Remove(KeyValuePair<K, List<T>> item)
        {
            return this.Remove(item.Key);
        }

        IEnumerator<KeyValuePair<K, List<T>>> IEnumerable<KeyValuePair<K, List<T>>>.GetEnumerator()
        {
            return this.buckets.GetEnumerator();
        }

        #endregion

        #region IEnumerable<List<T>> Members

        IEnumerator<List<T>> IEnumerable<List<T>>.GetEnumerator()
        {
            foreach (KeyValuePair<K, List<T>> item in this.buckets)
            {
                yield return item.Value;
            }
        }

        #endregion

        #region IEnumerable<T> Members

        public IEnumerator<T> GetEnumerator()
        {
            foreach (IList<T> item in this.buckets.Values)
            {
                int length = item.Count;
                for (int i = 0; i < length; i++)
                {
                    yield return item[i];
                }
            }
        }

        #endregion
    }
}
