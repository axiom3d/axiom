using System;
using System.Collections;
using RealmForge;
using T = RealmForge.IIdentifiable;

namespace RealmForge.Collections
{
    /// <summary>
    /// Provides the light-weight base class for strongly-typed tables of T's keyed to their ID's
    /// </summary>
    /// <remarks>The underlying collection is a SortedList so this is light-weight and geared towards smaller and infrequently modified collections.</remarks>
    public class IdentifiableCollectionBase : IList, IDictionary
    {

        #region Fields and Properties
        protected SortedList list;
        public int Count
        {
            get
            {
                return list.Count;
            }
        }

        public ICollection Values
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
        public object this[int index]
        {
            get
            {
                return list.GetByIndex( index );
            }
            set
            {
                list.SetByIndex( index, value );
            }
        }

        #endregion

        #region Constructors

        public IdentifiableCollectionBase()
        {
            this.list = new SortedList();
        }
        public IdentifiableCollectionBase( SortedList list )
        {
            this.list = list;
        }
        #endregion

        #region Methods
        public Array ToArray( Type arrayType )
        {
            Array array = Array.CreateInstance( arrayType, list.Count );
            list.Values.CopyTo( array, 0 );
            return array;
        }
        /*
		public T[] ToArray() 
		{
			T[] array = new T[list.Count];
			list.CopyTo(array,0);
			return array;
		}
		*/
        public virtual void Clear()
        {
            list.Clear();
        }
        public void CopyTo( Array array, int index )
        {
            list.Values.CopyTo( array, index );
        }

                public virtual bool Add( T value )
                {
                        return Add( value.ID, value, true, false );
                }
                public virtual bool Remove( string id )
        {
                        object val = list[id];
                        if ( val == null )
                return false;
            return Remove( (T)val );
        }
        public virtual bool Remove( T value )
        {
            ValidateItem( value );
            if ( !OnRemoving( value ) )
                return false;
            int index = list.IndexOfKey( value.ID );
            if ( index == -1 )
                return false;
            list.RemoveAt( index );
            OnRemove( value );
            return true;
        }
        public bool Contains( string id )
        {
            return list.Contains( id );
        }
        public bool Contains( T value )
        {
            return list.ContainsValue( value );
        }
        public int IndexOf( string id )
        {
            return list.IndexOfKey( id );
        }
        public int IndexOf( T value )
        {
            return list.IndexOfValue( value );
        }
        public IEnumerator GetEnumerator()
        {
            return list.Values.GetEnumerator();
        }
        public virtual void RemoveAt( int index )
        {
            Errors.AssertValidIndex( list.Count, index );
            Remove( (T)list.GetByIndex( index ) );
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

        void IList.Insert( int index, object value )
        {
            ValidateItem( value );
            Add( (T)value );
        }

        void IList.Remove( object value )
        {
            ValidateItem( value );
            Remove( (T)value );
        }

        bool IList.Contains( object value )
        {
            return list.Contains( ( (T)value ).ID );
        }

        int IList.IndexOf( object value )
        {
            return list.IndexOfValue( value );
        }

        int IList.Add( object value )
        {
            ValidateItem( value );
            T val = (T)value;
            string id = val.ID;
            if ( !Add( val ) )
                return -1;
            return list.IndexOfKey( id );
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

        bool IDictionary.IsReadOnly
        {
            get
            {
                return list.IsReadOnly;
            }
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
            if ( !( key is string ) )
                Errors.ArgumentNull( "Cannot add a item with a key that is not a string." );
            Remove( (string)key );
        }

        bool IDictionary.Contains( object key )
        {
            return list.Contains( key );
        }

        void IDictionary.Add( object key, object value )
        {
            if ( key == null )
                Errors.ArgumentNull( "Cannot add a item using a null key." );

            if ( !( key is string ) )
                Errors.ArgumentNull( "Cannot add a item with a key that is not a string." );

            Add( (string)key, value, true, false );
        }

        bool IDictionary.IsFixedSize
        {
            get
            {
                return list.IsFixedSize;
            }
        }
        #endregion

        #region Protected Methods

        protected virtual bool Add( string key, object value, bool errorIfAlreadyExists, bool replaceIfExists )
        {
            if ( key == null )
                Errors.ArgumentNull( "Cannot add a item using a null key." );
            if ( value == null )
                Errors.ArgumentNull( "Cannot add a null item." );
            ValidateItem( value );
            if ( list.Contains( key ) )
            {
                if ( replaceIfExists )
                    list.Remove( key );
                else if ( errorIfAlreadyExists )
                    Errors.Argument( "There is already an item with key '{0}' in the collection.", key );
                else
                    return false;
            }
            if ( !OnAdding( value ) )
                return false;
            list.Add( key, value );
            OnAdd( value );
            return true;
        }

        protected virtual void ValidateItem( object item )
        {
            if ( !( item is T ) )
                Errors.Argument( "Can only and and remove items that implement T" );
        }

        protected virtual void OnAdd( object item )
        {
        }

        protected virtual bool OnAdding( object item )
        {
            return true;
        }

        protected virtual void OnRemove( object item )
        {
        }
        protected virtual bool OnRemoving( object item )
        {
            return true;
        }

        #endregion
    }
}
