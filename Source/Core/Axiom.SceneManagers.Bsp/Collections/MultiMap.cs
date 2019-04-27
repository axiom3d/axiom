#region LGPL License

// Axiom Graphics Engine Library
// Copyright © 2003-2011 Axiom Project Team
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
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections;
using System.Collections.Generic;

#endregion

namespace Axiom.SceneManagers.Bsp.Collections
{
    /// <summary>
    ///     The MultiMap is a C# conversion of the std::buckets container from the C++ 
    ///     standard library.  
    /// </summary>
    /// <remarks>
    ///     A buckets allows multiple values per key, unlike IDictionary<TKey, TValue> which only allows
    ///     unique keys and only a single value per key.  Multiple values assigned to the same
    ///     key are placed in a "bucket", which in this case is a List<TValue>.
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
    public class MultiMap<TKey, TValue>
        : IDictionary, IDictionary<TKey, List<TValue>>, IEnumerable<TValue>, IEnumerable<List<TValue>>
    {
        #region Fields

        private readonly Dictionary<TKey, List<TValue>> buckets;

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
            this.buckets = new Dictionary<TKey, List<TValue>>();
        }

        public MultiMap(IEqualityComparer<TKey> comparer)
        {
            this.buckets = new Dictionary<TKey, List<TValue>>(comparer);
        }

        #endregion

        #region Instance Properties

        public int Count
        {
            get
            {
                return this.buckets.Count;
            }
        }

        /// <summary>
        ///		Gets the number of keys in this map.
        /// </summary>
        public int KeyCount
        {
            get
            {
                return this.buckets.Count;
            }
        }

        public int TotalCount
        {
            get
            {
                return this.count;
            }
        }

        #endregion

        #region Instance Methods

        /// <summary>
        ///     Inserts a value into a bucket that is specified by the
        ///     key.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="val"></param>
        public void Add(TKey key, TValue value)
        {
            List<TValue> container;

            if (!this.buckets.ContainsKey(key))
            {
                container = new List<TValue>();
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
        public int BucketCount(TKey key)
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

        public void Clear(TKey key)
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
        public IEnumerator<TValue> Find(TKey key)
        {
            if (this.buckets.ContainsKey(key))
            {
                return this.buckets[key].GetEnumerator();
                //int length = buckets[key].Count;
                //IList<TValue> bucket = buckets[key];
                //for (int i = 0; i < length; i++)
                //{
                //    yield return bucket[i];
                //}
            }
            return null;
        }

        public List<TValue> FindBucket(TKey key)
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
        public object FindFirst(TKey key)
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

        public IEnumerator<KeyValuePair<TKey, List<TValue>>> GetBucketsEnumerator()
        {
            foreach (var item in this.buckets)
            {
                yield return item;
            }
        }

        private void Add(TKey key, IList<TValue> value)
        {
            List<TValue> container;

            if (!this.buckets.ContainsKey(key))
            {
                container = new List<TValue>();
                this.buckets.Add(key, container);
            }
            else
            {
                container = this.buckets[key];
            }

            foreach (TValue i in value)
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
            if (key is TKey & value is IList<TValue>)
            {
                Add((TKey)key, (IList<TValue>)value);
            }
        }

        void IDictionary.Clear()
        {
            Clear();
        }

        bool IDictionary.Contains(object key)
        {
            if (key is TKey)
            {
                return ContainsKey((TKey)key);
            }
            return false;
        }

        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        bool IDictionary.IsFixedSize
        {
            get
            {
                return (this.buckets as IDictionary).IsFixedSize;
            }
        }

        bool IDictionary.IsReadOnly
        {
            get
            {
                return (this.buckets as IDictionary).IsReadOnly;
            }
        }

        ICollection IDictionary.Keys
        {
            get
            {
                return this.buckets.Keys;
            }
        }

        void IDictionary.Remove(object key)
        {
            if (key is TKey)
            {
                Remove((TKey)key);
            }
        }

        ICollection IDictionary.Values
        {
            get
            {
                return this.buckets.Values;
            }
        }

        object IDictionary.this[object key]
        {
            get
            {
                if (key is TKey)
                {
                    return this[(TKey)key];
                }
                return null;
            }
            set
            {
                if (value is IList<TValue>)
                {
                    this[(TKey)key] = (List<TValue>)value;
                }
                else
                {
                    throw new ArgumentException("The key must be of type " + typeof(List<TValue>).ToString(), "value");
                }
            }
        }

        void ICollection.CopyTo(Array array, int index)
        {
            throw new NotImplementedException();
        }

        int ICollection.Count
        {
            get
            {
                return this.count;
            }
        }

        bool ICollection.IsSynchronized
        {
            get
            {
                return (this.buckets as ICollection).IsSynchronized;
            }
        }

        object ICollection.SyncRoot
        {
            get
            {
                return (this.buckets as ICollection).SyncRoot;
            }
        }

        #endregion

        #region IDictionary<TKey,List<TValue>> Members

        void IDictionary<TKey, List<TValue>>.Add(TKey key, List<TValue> value)
        {
            Add(key, value);
        }

        public bool ContainsKey(TKey key)
        {
            return this.buckets.ContainsKey(key);
        }

        public ICollection<TKey> Keys
        {
            get
            {
                return this.buckets.Keys;
            }
        }

        public bool Remove(TKey key)
        {
            bool removed = this.buckets.Remove(key);
            if (removed)
            {
                this.count--;
                return true;
            }
            return false;
        }

        public bool TryGetValue(TKey key, out List<TValue> value)
        {
            List<TValue> tvalue;
            this.buckets.TryGetValue(key, out tvalue);
            value = tvalue;
            if (tvalue == null)
            {
                return false;
            }
            return true;
        }

        public ICollection<List<TValue>> Values
        {
            get
            {
                return this.buckets.Values;
            }
        }

        public List<TValue> this[TKey key]
        {
            get
            {
                return this.buckets[key];
            }
            set
            {
                this.buckets[key] = value;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        void ICollection<KeyValuePair<TKey, List<TValue>>>.Add(KeyValuePair<TKey, List<TValue>> item)
        {
            Add(item.Key, item.Value);
        }

        void ICollection<KeyValuePair<TKey, List<TValue>>>.Clear()
        {
            Clear();
        }

        bool ICollection<KeyValuePair<TKey, List<TValue>>>.Contains(KeyValuePair<TKey, List<TValue>> item)
        {
            return this.buckets.ContainsKey(item.Key);
        }

        void ICollection<KeyValuePair<TKey, List<TValue>>>.CopyTo(KeyValuePair<TKey, List<TValue>>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        int ICollection<KeyValuePair<TKey, List<TValue>>>.Count
        {
            get
            {
                return Count;
            }
        }

        bool ICollection<KeyValuePair<TKey, List<TValue>>>.IsReadOnly
        {
            get
            {
                return (this.buckets as IDictionary).IsReadOnly;
            }
        }

        bool ICollection<KeyValuePair<TKey, List<TValue>>>.Remove(KeyValuePair<TKey, List<TValue>> item)
        {
            return Remove(item.Key);
        }

        IEnumerator<KeyValuePair<TKey, List<TValue>>> IEnumerable<KeyValuePair<TKey, List<TValue>>>.GetEnumerator()
        {
            return this.buckets.GetEnumerator();
        }

        #endregion

        #region IEnumerable<List<TValue>> Members

        IEnumerator<List<TValue>> IEnumerable<List<TValue>>.GetEnumerator()
        {
            foreach (var item in this.buckets)
            {
                yield return item.Value;
            }
        }

        #endregion

        #region IEnumerable<TValue> Members

        public IEnumerator<TValue> GetEnumerator()
        {
            foreach (IList<TValue> item in this.buckets.Values)
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