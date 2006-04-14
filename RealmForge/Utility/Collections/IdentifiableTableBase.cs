using System;
using System.Collections;
using RealmForge;

namespace RealmForge.Collections
{
    /// <summary>
    /// Provides the light-weight base class for strongly-typed tables of IIdentifiable's keyed to their ID's
    /// </summary>
    /// <remarks>The underlying collection is a SortedList so this is light-weight and geared towards smaller and infrequently modified collections.</remarks>
    public class IdentifiableTableBase : IList, IDictionary
    {

        #region Fields and Properties
        protected Hashtable list = new Hashtable();
        public int Count
        {
            get
            {
                return list.Count;
            }
        }

        ICollection Values
        {
            get
            {
                return list.Values;
            }
        }

        public ICollection Keys
        {
            get
            {
                return list.Keys;
            }
        }

        public object this[string id]
        {
            get
            {
                return list[id];
            }
            set
            {
                list[id] = value;
            }
        }
        #endregion

        #region Constructors
        public IdentifiableTableBase()
        {
        }
        #endregion

        #region Methods
        public void Clear()
        {
            list.Clear();
        }
        public void CopyTo( Array array, int index )
        {
            list.Values.CopyTo( array, index );
        }
        public void Remove( string id )
        {
            list.Remove( id );
        }
        public bool Contains( string id )
        {
            return list.Contains( id );
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return list.Values.GetEnumerator();
        }

        #endregion

        #region ICollection Members
        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            return list.GetEnumerator();
        }

        bool ICollection.IsSynchronized
        {
            get
            {
                return list.IsSynchronized;
            }
        }

        object ICollection.SyncRoot
        {
            get
            {
                return list.SyncRoot;
            }
        }

        #endregion

        #region IList Members

        bool IList.IsReadOnly
        {
            get
            {
                return list.IsReadOnly;
            }
        }

        object IList.this[int index]
        {
            get
            {
                throw new NotSupportedException( "Cannot get item by index" );
            }
            set
            {
                throw new NotSupportedException( "Cannot set item by index" );
            }
        }

        void IList.RemoveAt( int index )
        {
            throw new NotSupportedException( "Cannot remove item by index" );
        }

        void IList.Insert( int index, object value )
        {
            list.Add( ( (IIdentifiable)value ).ID, value );
        }

        void IList.Remove( object value )
        {
            list.Remove( ( (IIdentifiable)value ).ID );
        }

        bool IList.Contains( object value )
        {
            return list.Contains( ( (IIdentifiable)value ).ID );
        }

        int IList.IndexOf( object value )
        {
            throw new NotSupportedException( "Cannot get index of item" );
        }

        int IList.Add( object value )
        {
            string id = ( (IIdentifiable)value ).ID;
            ( (IDictionary)this ).Add( id, value );
            return -1;
        }

        bool IList.IsFixedSize
        {
            get
            {
                return list.IsFixedSize;
            }
        }

        #endregion

        #region IDictionary Members

        ICollection IDictionary.Values
        {
            get
            {
                return list.Values;
            }
        }
        bool IDictionary.IsReadOnly
        {
            get
            {
                return list.IsReadOnly;
            }
        }


        public IEnumerator GetEnumerator()
        {
            return list.Values.GetEnumerator();
        }

        object IDictionary.this[object key]
        {
            get
            {
                return list[key];
            }
            set
            {
                list[key] = value;
            }
        }

        void IDictionary.Remove( object key )
        {
            list.Remove( key );
        }

        bool IDictionary.Contains( object key )
        {
            return list.Contains( key );
        }

        void IDictionary.Add( object key, object value )
        {
            if ( list.Contains( key ) )
                Errors.Argument( "There is already an item with the key {0} in the list", key );
            list.Add( key, value );
        }

        bool IDictionary.IsFixedSize
        {
            get
            {
                return list.IsFixedSize;
            }
        }

        #endregion
    }
}
