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
#if !USE_CUSTOM_SORTEDLIST
using System.Collections;
using System.Collections.Generic;
#endif

#endregion

namespace Axiom.Collections
{
    /// <summary>
    ///	Serves as a basis for strongly typed collections in the engine.
    /// </summary>
    public abstract class AxiomCollection<T> : SortedList<string, T>
    {
        #region Constants

        private const int InitialCapacity = 60;

        #endregion

        #region Readonly & Static Fields

        protected static int nextUniqueKeyCounter;
        protected string typeName;

        #endregion

        #region Fields

        protected Object parent;

        #endregion

        #region Constructors

        /// <summary>
        ///		
        /// </summary>
        protected AxiomCollection()
            : base( InitialCapacity )
        {
            this.parent = null;
            this.typeName = typeof( T ).Name;
        }

        /// <summary>
        ///		
        /// </summary>
        /// <param name="parent"></param>
        protected AxiomCollection( Object parent )
            : base( InitialCapacity )
        {
            this.parent = parent;
            this.typeName = typeof( T ).Name;
        }

        #endregion

        #region Instance Indexers

        /// <summary>
        ///		
        /// </summary>
        public virtual T this[ int index ]
        {
            get { return Values[ index ]; }
            set { base[ Keys[ index ] ] = value; }
        }

        #endregion

        #region Instance Methods

        /// <summary>
        ///	Adds an unnamed object to the <see cref="AxiomCollection{T}"/> and names it manually.
        /// </summary>
        /// <param name="item">The object to add.</param>
        public virtual void Add( T item )
        {
            Add( typeName + ( nextUniqueKeyCounter++ ), item );
        }

        /// <summary>
        /// eturns an enumerator that iterates through the <see cref="AxiomCollection{T}"/>.
        /// </summary>
        /// <returns>An <see cref="IEnumerator{T}"/> for the <see cref="AxiomCollection{T}"/> values.</returns>
        public virtual IEnumerator GetEnumerator()
        {
            return Values.GetEnumerator();
        }

        /// <summary>
        ///	Removes the item with the specified key from the collection.
        /// </summary>
        /// <param name="item">The key of the item to remove.</param>
        public virtual void RemoveByKey( string item )
        {
            int index = IndexOfKey( item );

            if ( index != -1 )
            {
                RemoveAt( index );
            }
        }

        #endregion
    }


    /// <summary>
    ///	Serves as a basis for strongly typed collections in the engine.
    /// </summary>
    public class AxiomSortedCollection<TKey, TValue> : SortedList<TKey, TValue>
    {
        #region Constants

        private const int InitialCapacity = 60;

        #endregion

        #region Fields

        protected Object parent;

        #endregion

        #region Constructors

        /// <summary>
        ///		
        /// </summary>
        public AxiomSortedCollection()
            : base(InitialCapacity)
        {
            this.parent = null;
        }

        /// <summary>
        ///		
        /// </summary>
        /// <param name="parent"></param>
        public AxiomSortedCollection(Object parent)
            : base(InitialCapacity)
        {
            this.parent = parent;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Collections.Generic.SortedList`2"/> class that is empty, has the specified initial capacity, and uses the specified <see cref="T:System.Collections.Generic.IComparer`1"/>.
        /// </summary>
        /// <param name="capacity">The initial number of elements that the <see cref="T:System.Collections.Generic.SortedList`2"/> can contain.
        /// </param>
        /// <param name="comparer">The <see cref="T:System.Collections.Generic.IComparer`1"/> implementation to use when comparing keys.
        /// -or-
        /// null to use the default <see cref="T:System.Collections.Generic.Comparer`1"/> for the type of the key.
        /// </param>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="capacity"/> is less than zero.
        /// </exception>
        public AxiomSortedCollection( int capacity, IComparer<TKey> comparer )
            : base( capacity, comparer )
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Collections.Generic.SortedList`2"/> class that contains elements copied from the specified <see cref="T:System.Collections.Generic.IDictionary`2"/>, has sufficient capacity to accommodate the number of elements copied, and uses the specified <see cref="T:System.Collections.Generic.IComparer`1"/>.
        /// </summary>
        /// <param name="dictionary">The <see cref="T:System.Collections.Generic.IDictionary`2"/> whose elements are copied to the new <see cref="T:System.Collections.Generic.SortedList`2"/>.
        /// </param><param name="comparer">The <see cref="T:System.Collections.Generic.IComparer`1"/> implementation to use when comparing keys.
        /// -or-
        /// null to use the default <see cref="T:System.Collections.Generic.Comparer`1"/> for the type of the key.
        /// </param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="dictionary"/> is null.
        /// </exception>
        /// <exception cref="T:System.ArgumentException"><paramref name="dictionary"/> contains one or more duplicate keys.
        /// </exception>
        public AxiomSortedCollection( IDictionary<TKey, TValue> dictionary, IComparer<TKey> comparer )
            : base( dictionary, comparer )
        {
        }
        #endregion

        #region Instance Indexers

        /// <summary>
        ///		
        /// </summary>
        public virtual TValue this[int index]
        {
            get { return this.Values[index]; }
            set { base[Keys[index]] = value; }
        }

        #endregion

        #region Instance Methods

        /// <summary>
        /// eturns an enumerator that iterates through the <see cref="AxiomCollection{T}"/>.
        /// </summary>
        /// <returns>An <see cref="IEnumerator{T}"/> for the <see cref="AxiomCollection{T}"/> values.</returns>
        public virtual IEnumerator GetEnumerator()
        {
            return Values.GetEnumerator();
        }

        /// <summary>
        ///	Removes the item with the specified key from the collection.
        /// </summary>
        /// <param name="item">The key of the item to remove.</param>
        public virtual void RemoveByKey(TKey item)
        {
            int index = IndexOfKey(item);

            if (index != -1)
            {
                RemoveAt(index);
            }
        }

        #endregion
    }
}
