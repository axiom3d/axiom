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

#region Namespace Declarations

using System;
using System.Collections;
using System.Diagnostics;
using Axiom.MathLib.Collections;

#endregion Namespace Declarations
			
namespace Axiom {
    /// <summary>
    ///     The Map is a C# conversion of the std::map container from the C++ 
    ///     standard library.  
    /// </summary>
    /// <remarks>
    ///     A map allows multiple values per key, unlike the Hashtable which only allows
    ///     unique keys and only a single value per key.  Multiple values assigned to the same
    ///     key are placed in a "bucket", which in this case is an ArrayList.
    ///     <p/>
    ///     An example of values in a map would look like this:
    ///     Key     Value
    ///     "a"     "Alan"
    ///     "a"     "Adam"
    ///     "b"     "Brien"
    ///     "c"     "Chris"
    ///     "c"     "Carl"
    ///     etc
    ///     <p/>
    ///     Currently, enumeration is the only way to iterate through the values, which is
    ///     more pratical in terms of how the Map works internally anyway.  Intial testing showed
    ///     that inserting and iterating through 100,000 items, the Inserts took ~260ms and a full
    ///     enumeration of them all (with unboxing of the value type stored in the map) took between 16-30ms.
    /// </remarks>
    public class Map : IEnumerable {
        #region Fields

        /// <summary>
        ///     Number of total items currently in this map.
        /// </summary>
        protected int count;

        /// <summary>
        ///     A sorted list of buckets.
        /// </summary>
        protected SortedList buckets;

        #endregion Fields

        #region Constructor

        /// <summary>
        ///     Default constructor.
        /// </summary>
        public Map() {
            buckets = new SortedList();
        }

        /// <summary>
        ///     Constructor, takes the comparer to use for the bucket list.
        /// </summary>
        /// <param name="comparer">Custom <see cref="IComparable"/>implmentation to use to sort.</param>
        public Map(IComparer comparer) {
            buckets = new SortedList(comparer);
        }

        #endregion Constructor

        /// <summary>
        ///     Clears this map of all contained objects.
        /// </summary>
        public void Clear() {
            buckets.Clear();
			count = 0;
        }

		public object GetKey(int index) {
			Debug.Assert(index < buckets.Keys.Count);

			return buckets.GetKey(index);
		}

        /// <summary>
        ///     Given a key, Find will return an IEnumerator that allows
        ///     you to iterate over all items in the bucket associated
        ///     with the key.
        /// </summary>
        /// <param name="key">Key for look for.</param>
        /// <returns>IEnumerator to go through the items assigned to the key.</returns>
        public IEnumerator Find(object key) {
            if(buckets[key] == null) {
                return null;
            }
            else {
                return ((ArrayList)buckets[key]).GetEnumerator();
            }
        }

		public IList FindBucket(object key) {
			if(buckets[key] == null) {
				return null;
			}
			else {
				return (ArrayList)buckets[key];
			}
		}

        /// <summary>
        ///     Gets the count of objects mapped to the specified key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public int Count(object key) {
            if(buckets[key] != null) {
                return ((ArrayList)buckets[key]).Count;
            }

            return 0;
        }

        /// <summary>
        ///     Inserts a value into a bucket that is specified by the
        ///     key.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="val"></param>
        public void Insert(object key, object val) {
            ArrayList container = null;

            if(buckets[key] == null) {
                container = new ArrayList();
                buckets.Add(key, container);
            }
            else {
                container = (ArrayList)buckets[key];
            }

            // TODO: Doing the contains check is extremely slow, so for now duplicate items are allowed
            //if(!container.Contains(val)) {
            container.Add(val);
            count++;
            //}
        }

        /// <summary>
        ///     Gets the total count of all items contained within the map.
        /// </summary>
        public int TotalCount {
            get {
                return count;
            }
            set {
                count = value;
            }
        }

		/// <summary>
		///		Gets the number of keys in this map.
		/// </summary>
		public int KeyCount {
			get {
				return buckets.Count;
			}
		}

        #region IEnumerable Members

        /// <summary>
        ///     Gets an appropriate enumerator for the map, customized to go
        ///     through each item in the map and return a Pair of the key and
        ///     value.
        /// </summary>
        /// <returns></returns>
        public IEnumerator GetEnumerator() {
            return new MapEnumerator(this);
        }

        /// <summary>
        ///     Private class serving as the custom Enumerator for the map
        ///     collection.
        /// </summary>
        private class MapEnumerator : IEnumerator {
            #region Fields

            /// <summary>
            ///     Overall position from the beginning of the map.
            /// </summary>
            int totalPos;

            /// <summary>
            ///     Current index of the current bucket.
            /// </summary>
            int bucketIndex;

            /// <summary>
            ///     Current bucket position.
            /// </summary>
            int bucketPos;

            /// <summary>
            ///     This is the current bucket being used in the enumeration.
            /// </summary>
            ArrayList currentBucket;

            /// <summary>
            ///     Reference to the map that we are iterating over.
            /// </summary>
            Map map;

            #endregion Fields

            /// <summary>
            ///     Constructor.
            /// </summary>
            /// <param name="map">The map this enumerator will enumerate over.</param>
            public MapEnumerator(Map map) {
                this.map = map;
            }

            #region IEnumerator Members

            /// <summary>
            ///     Resets all current state of the enumeration process.
            /// </summary>
            public void Reset() {
                totalPos = 0;
                bucketIndex = 0;
                bucketPos = 0;
            }

            /// <summary>
            ///     Gets a Pair containing the key and value at the current state
            ///     of the enumeration.
            /// </summary>
            public object Current {
                get {
                    object key = map.buckets.GetKey(bucketIndex);
                    object val = currentBucket[bucketPos];
                    return new Pair(key, val);
                }
            }

            /// <summary>
            ///     Moves to the next position in the enumeration.
            /// </summary>
            /// <returns></returns>
            public bool MoveNext() {
				if(map.buckets.Count == 0) {
					return false;
				}

                // we've reached the end
                if((totalPos + 1) == map.count) {
                    return false;
                }

                // if there is a current bucket
                if(currentBucket != null) {
                    // if we have reached the end of the current bucket, get the next
                    // and reset the bucket position
                    if(bucketPos == (currentBucket.Count - 1)) {
                        currentBucket = (ArrayList)map.buckets.GetByIndex(++bucketIndex);
                        bucketPos = 0;
                    }
                    else {
                        // increment the position within the current bucket
                        bucketPos++;
                    }

                    // increment the total overall position
                    totalPos++;
                }
                else {
                    // should only happen the first time
                    currentBucket = (ArrayList)map.buckets.GetByIndex(bucketIndex);
                }

                return true;
            }

            #endregion
        }

        #endregion
    }
}
