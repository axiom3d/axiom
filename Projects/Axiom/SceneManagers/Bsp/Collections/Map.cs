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

using System.Collections;

#endregion Namespace Declarations

namespace Axiom.SceneManagers.Bsp.Collections
{
	/// <summary>
	///		Map class, a little different from the Axiom.Collections.Map class
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
	public class Map
	{
		#region Fields

		/// <summary>
		///     A list of buckets.
		/// </summary>
		public Hashtable buckets;

		/// <summary>
		///     Number of total items currently in this map.
		/// </summary>
		protected int count;

		#endregion Fields

		#region Constructor

		/// <summary>
		///     Default constructor.
		/// </summary>
		public Map()
		{
			this.buckets = new Hashtable();
		}

		#endregion Constructor

		/// <summary>
		///     Gets the total count of all items contained within the map.
		/// </summary>
		public int TotalCount
		{
			get
			{
				return this.count;
			}
		}

		/// <summary>
		///     Clears this map of all contained objects.
		/// </summary>
		public void Clear()
		{
			this.buckets.Clear();
			this.count = 0;
		}

		/// <summary>
		///     Clears the bucket with given key.
		/// </summary>
		public void Clear( object key )
		{
			var bucket = (ArrayList)this.buckets[ key ];
			if ( bucket != null )
			{
				this.count -= bucket.Count;
				this.buckets.Remove( key );
			}
		}

		/// <summary>
		///     Given a key, Find will return an IEnumerator that allows
		///     you to iterate over all items in the bucket associated
		///     with the key.
		/// </summary>
		/// <param name="key">Key for look for.</param>
		/// <returns>IEnumerator to go through the items assigned to the key.</returns>
		public IEnumerator Find( object key )
		{
			if ( this.buckets[ key ] == null )
			{
				return null;
			}
			else
			{
				return ( (ArrayList)this.buckets[ key ] ).GetEnumerator();
			}
		}

		public IList FindBucket( object key )
		{
			if ( this.buckets[ key ] == null )
			{
				return null;
			}
			else
			{
				return (ArrayList)this.buckets[ key ];
			}
		}

		/// <summary>
		///     Given a key, FindFirst will return the first item in the bucket
		///     associated with the key.
		/// </summary>
		/// <param name="key">Key to look for.</param>
		public object FindFirst( object key )
		{
			if ( this.buckets[ key ] == null )
			{
				return null;
			}
			else
			{
				return ( (ArrayList)this.buckets[ key ] )[ 0 ];
			}
		}

		/// <summary>
		///     Gets the count of objects mapped to the specified key.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public int Count( object key )
		{
			if ( this.buckets[ key ] != null )
			{
				return ( (ArrayList)this.buckets[ key ] ).Count;
			}

			return 0;
		}

		/// <summary>
		///     Inserts a value into a bucket that is specified by the
		///     key.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="val"></param>
		public void Insert( object key, object val )
		{
			ArrayList container = null;

			if ( this.buckets[ key ] == null )
			{
				container = new ArrayList();
				this.buckets.Add( key, container );
			}
			else
			{
				container = (ArrayList)this.buckets[ key ];
			}

			// TODO: Doing the contains check is extremely slow, so for now duplicate items are allowed
			//if(!container.Contains(val)) {
			container.Add( val );
			this.count++;
			//}
		}

		/// <summary>
		///     Gets an appropriate enumerator for the map, customized to go
		///     through each key in the map and return a Pair of the key and
		///     an ArrayList of the values associated with it.
		/// </summary>
		/// <returns></returns>
		public IEnumerator GetBucketEnumerator()
		{
			return this.buckets.Keys.GetEnumerator();
		}
	}
}
