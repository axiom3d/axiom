// 
// Axiom.Collections.SortedList.cs
// 
// Author:
//   Sergey Chaban (serge@wildwestsoftware.com)
//   Duncan Mak (duncan@ximian.com)
//   Herve Poussineau (hpoussineau@fr.st
//   Zoltan Varga (vargaz@gmail.com)
// 

//
// Copyright (C) 2004 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

#region Namespace Declarations

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

#endregion

namespace Axiom.Collections
{
    /// <summary>
    ///  Represents a collection of associated keys and values
    ///  that are sorted by the keys and are accessible by key
    ///  and by index.
    /// </summary>
    [ ComVisible( false ) ]
    public class SortedList<TKey, TValue> : IDictionary<TKey, TValue>,
                                            IDictionary
    {
        #region Readonly & Static Fields

        private static readonly int INITIAL_SIZE = 16;

        #endregion

        #region Fields

        private IComparer<TKey> comparer;
        private int defaultCapacity;

        private int inUse;
        private int modificationCount;
        private KeyValuePair<TKey, TValue>[] table;

        #endregion

        #region Constructors

        public SortedList()
            : this( INITIAL_SIZE, null )
        {
        }

        public SortedList( int capacity )
            : this( capacity, null )
        {
        }

        public SortedList( int capacity, IComparer<TKey> comparer )
        {
            if ( capacity < 0 )
            {
                throw new ArgumentOutOfRangeException( "initialCapacity" );
            }

            if ( capacity == 0 )
            {
                this.defaultCapacity = 0;
            }
            else
            {
                this.defaultCapacity = INITIAL_SIZE;
            }
            this.Init( comparer, capacity, true );
        }

        public SortedList( IComparer<TKey> comparer )
            : this( INITIAL_SIZE, comparer )
        {
        }

        public SortedList( IDictionary<TKey, TValue> dictionary )
            : this( dictionary, null )
        {
        }

        public SortedList( IDictionary<TKey, TValue> dictionary, IComparer<TKey> comparer )
        {
            if ( dictionary == null )
            {
                throw new ArgumentNullException( "dictionary" );
            }

            this.Init( comparer, dictionary.Count, true );

            foreach ( KeyValuePair<TKey, TValue> kvp in dictionary )
            {
                this.Add( kvp.Key, kvp.Value );
            }
        }

        #endregion

        #region Enums

        private enum EnumeratorMode : int
        {
            KEY_MODE = 0,
            VALUE_MODE,
            ENTRY_MODE
        }

        #endregion

        #region Instance Properties

        public int Capacity
        {
            get { return this.table.Length; }

            set
            {
                int current = this.table.Length;

                if ( this.inUse > value )
                {
                    throw new ArgumentOutOfRangeException( "capacity too small" );
                }
                else if ( value == 0 )
                {
                    // return to default size
                    KeyValuePair<TKey, TValue>[] newTable = new KeyValuePair<TKey, TValue>[this.defaultCapacity];
                    Array.Copy( this.table, newTable, this.inUse );
                    this.table = newTable;
                }
#if NET_1_0
				else if (current > defaultCapacity && value < current) {
                                        KeyValuePair<TKey, TValue> [] newTable = new KeyValuePair<TKey, TValue> [defaultCapacity];
                                        Array.Copy (table, newTable, inUse);
                                        this.table = newTable;
                                }
#endif
                else if ( value > this.inUse )
                {
                    KeyValuePair<TKey, TValue>[] newTable = new KeyValuePair<TKey, TValue>[value];
                    Array.Copy( this.table, newTable, this.inUse );
                    this.table = newTable;
                }
                else if ( value > current )
                {
                    KeyValuePair<TKey, TValue>[] newTable = new KeyValuePair<TKey, TValue>[value];
                    Array.Copy( this.table, newTable, current );
                    this.table = newTable;
                }
            }
        }

        public IComparer<TKey> Comparer
        {
            get { return this.comparer; }
        }

        public IList<TKey> Keys
        {
            get { return new ListKeys( this ); }
        }

        public IList<TValue> Values
        {
            get { return new ListValues( this ); }
        }

        #endregion

        #region Instance Methods

        public bool ContainsValue( TValue value )
        {
            return this.IndexOfValue( value ) >= 0;
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            for ( int i = 0; i < this.inUse; i ++ )
            {
                KeyValuePair<TKey, TValue> current = this.table[ i ];

                yield return new KeyValuePair<TKey, TValue>( current.Key, current.Value );
            }
        }

        public int IndexOfKey( TKey key )
        {
            if ( key == null )
            {
                throw new ArgumentNullException( "key" );
            }

            int indx = 0;
            try
            {
                indx = this.Find( key );
            }
            catch ( Exception )
            {
                throw new InvalidOperationException();
            }

            return ( indx | ( indx >> 31 ) );
        }

        public int IndexOfValue( TValue value )
        {
            if ( this.inUse == 0 )
            {
                return -1;
            }

            for ( int i = 0; i < this.inUse; i ++ )
            {
                KeyValuePair<TKey, TValue> current = this.table[ i ];

                if ( Equals( value, current.Value ) )
                {
                    return i;
                }
            }

            return -1;
        }

        public void RemoveAt( int index )
        {
            KeyValuePair<TKey, TValue>[] table = this.table;
            int cnt = this.Count;
            if ( index >= 0 && index < cnt )
            {
                if ( index != cnt - 1 )
                {
                    Array.Copy( table, index + 1, table, index, cnt - 1 - index );
                }
                else
                {
                    table[ index ] = default ( KeyValuePair<TKey, TValue> );
                }
                --this.inUse;
                ++this.modificationCount;
            }
            else
            {
                throw new ArgumentOutOfRangeException( "index out of range" );
            }
        }

        public void TrimExcess()
        {
            if ( this.inUse < this.table.Length * 0.9 )
            {
                this.Capacity = this.inUse;
            }
        }

        internal TKey KeyAt( int index )
        {
            if ( index >= 0 && index < this.Count )
            {
                return this.table[ index ].Key;
            }
            else
            {
                throw new ArgumentOutOfRangeException( "Index out of range" );
            }
        }

        internal TValue ValueAt( int index )
        {
            if ( index >= 0 && index < this.Count )
            {
                return this.table[ index ].Value;
            }
            else
            {
                throw new ArgumentOutOfRangeException( "Index out of range" );
            }
        }

        private void CopyToArray( Array arr, int i,
                                  EnumeratorMode mode )
        {
            if ( arr == null )
            {
                throw new ArgumentNullException( "arr" );
            }

            if ( i < 0 || i + this.Count > arr.Length )
            {
                throw new ArgumentOutOfRangeException( "i" );
            }

            IEnumerator it = new Enumerator( this, mode );

            while ( it.MoveNext() )
            {
                arr.SetValue( it.Current, i++ );
            }
        }

        private void EnsureCapacity( int n, int free )
        {
            KeyValuePair<TKey, TValue>[] table = this.table;
            KeyValuePair<TKey, TValue>[] newTable = null;
            int cap = this.Capacity;
            bool gap = ( free >= 0 && free < this.Count );

            if ( n > cap )
            {
                newTable = new KeyValuePair<TKey, TValue>[n << 1];
            }

            if ( newTable != null )
            {
                if ( gap )
                {
                    int copyLen = free;
                    if ( copyLen > 0 )
                    {
                        Array.Copy( table, 0, newTable, 0, copyLen );
                    }
                    copyLen = this.Count - free;
                    if ( copyLen > 0 )
                    {
                        Array.Copy( table, free, newTable, free + 1, copyLen );
                    }
                }
                else
                {
                    // Just a resizing, copy the entire table.
                    Array.Copy( table, newTable, this.Count );
                }
                this.table = newTable;
            }
            else if ( gap )
            {
                Array.Copy( table, free, table, free + 1, this.Count - free );
            }
        }

        private int Find( TKey key )
        {
            KeyValuePair<TKey, TValue>[] table = this.table;
            int len = this.Count;

            if ( len == 0 )
            {
                return ~0;
            }

            int left = 0;
            int right = len - 1;

            while ( left <= right )
            {
                int guess = ( left + right ) >> 1;

                int cmp = this.comparer.Compare( table[ guess ].Key, key );
                if ( cmp == 0 )
                {
                    return guess;
                }

                if ( cmp < 0 )
                {
                    left = guess + 1;
                }
                else
                {
                    right = guess - 1;
                }
            }

            return ~left;
        }

        private void Init( IComparer<TKey> comparer, int capacity, bool forceSize )
        {
            if ( comparer == null )
            {
                comparer = Comparer<TKey>.Default;
            }
            this.comparer = comparer;
            if ( !forceSize && ( capacity < this.defaultCapacity ) )
            {
                capacity = this.defaultCapacity;
            }
            this.table = new KeyValuePair<TKey, TValue>[capacity];
            this.inUse = 0;
            this.modificationCount = 0;
        }

        private void PutImpl( TKey key, TValue value, bool overwrite )
        {
            if ( key == null )
            {
                throw new ArgumentNullException( "null key" );
            }

            KeyValuePair<TKey, TValue>[] table = this.table;

            int freeIndx = -1;

            try
            {
                freeIndx = this.Find( key );
            }
            catch ( Exception )
            {
                throw new InvalidOperationException();
            }

            if ( freeIndx >= 0 )
            {
                if ( !overwrite )
                {
                    throw new ArgumentException( "element already exists" );
                }

                table[ freeIndx ] = new KeyValuePair<TKey, TValue>( key, value );
                ++this.modificationCount;
                return;
            }

            freeIndx = ~freeIndx;

            if ( freeIndx > this.Capacity + 1 )
            {
                throw new Exception( "SortedList::internal error (" + key + ", " + value + ") at [" + freeIndx + "]" );
            }

            this.EnsureCapacity( this.Count + 1, freeIndx );

            table = this.table;
            table[ freeIndx ] = new KeyValuePair<TKey, TValue>( key, value );

            ++this.inUse;
            ++this.modificationCount;
        }

        private TKey ToKey( object key )
        {
            if ( key == null )
            {
                throw new ArgumentNullException( "key" );
            }
            if ( !( key is TKey ) )
            {
                throw new ArgumentException( "The value \"" + key + "\" isn't of type \"" + typeof ( TKey ) + "\" and can't be used in this generic collection.", "key" );
            }
            return (TKey)key;
        }

        private TValue ToValue( object value )
        {
            if ( !( value is TValue ) )
            {
                throw new ArgumentException( "The value \"" + value + "\" isn't of type \"" + typeof ( TValue ) + "\" and can't be used in this generic collection.", "value" );
            }
            return (TValue)value;
        }

        #endregion

        #region Nested Struct: KeyEnumerator

        public struct KeyEnumerator : IEnumerator<TKey>, IDisposable
        {
            // this MUST be -1, because we depend on it in move next.
            // we just decr the size, so, 0 - 1 == FINISHED

            #region Constants

            private const int FINISHED = -1;
            private const int NOT_STARTED = -2;

            #endregion

            #region Fields

            private int idx;
            private SortedList<TKey, TValue> l;
            private int ver;

            #endregion

            #region Constructors

            internal KeyEnumerator( SortedList<TKey, TValue> l )
            {
                this.l = l;
                this.idx = NOT_STARTED;
                this.ver = l.modificationCount;
            }

            #endregion

            #region IEnumerator<TKey> Members

            public void Dispose()
            {
                this.idx = NOT_STARTED;
            }

            public bool MoveNext()
            {
                if ( this.ver != this.l.modificationCount )
                {
                    throw new InvalidOperationException( "Collection was modified after the enumerator was instantiated." );
                }

                if ( this.idx == NOT_STARTED )
                {
                    this.idx = this.l.Count;
                }

                return this.idx != FINISHED && -- this.idx != FINISHED;
            }

            public TKey Current
            {
                get
                {
                    if ( this.idx < 0 )
                    {
                        throw new InvalidOperationException();
                    }

                    return this.l.KeyAt( this.l.Count - 1 - this.idx );
                }
            }

            void IEnumerator.Reset()
            {
                if ( this.ver != this.l.modificationCount )
                {
                    throw new InvalidOperationException( "Collection was modified after the enumerator was instantiated." );
                }

                this.idx = NOT_STARTED;
            }

            object IEnumerator.Current
            {
                get { return this.Current; }
            }

            #endregion
        }

        #endregion

        #region Nested Struct: ValueEnumerator

        public struct ValueEnumerator : IEnumerator<TValue>, IDisposable
        {
            // this MUST be -1, because we depend on it in move next.
            // we just decr the size, so, 0 - 1 == FINISHED

            #region Constants

            private const int FINISHED = -1;
            private const int NOT_STARTED = -2;

            #endregion

            #region Fields

            private int idx;
            private SortedList<TKey, TValue> l;
            private int ver;

            #endregion

            #region Constructors

            internal ValueEnumerator( SortedList<TKey, TValue> l )
            {
                this.l = l;
                this.idx = NOT_STARTED;
                this.ver = l.modificationCount;
            }

            #endregion

            #region IEnumerator<TValue> Members

            public void Dispose()
            {
                this.idx = NOT_STARTED;
            }

            public bool MoveNext()
            {
                if ( this.ver != this.l.modificationCount )
                {
                    throw new InvalidOperationException( "Collection was modified after the enumerator was instantiated." );
                }

                if ( this.idx == NOT_STARTED )
                {
                    this.idx = this.l.Count;
                }

                return this.idx != FINISHED && -- this.idx != FINISHED;
            }

            public TValue Current
            {
                get
                {
                    if ( this.idx < 0 )
                    {
                        throw new InvalidOperationException();
                    }

                    return this.l.ValueAt( this.l.Count - 1 - this.idx );
                }
            }

            void IEnumerator.Reset()
            {
                if ( this.ver != this.l.modificationCount )
                {
                    throw new InvalidOperationException( "Collection was modified after the enumerator was instantiated." );
                }

                this.idx = NOT_STARTED;
            }

            object IEnumerator.Current
            {
                get { return this.Current; }
            }

            #endregion
        }

        #endregion

        #region Nested Class: Enumerator

        private sealed class Enumerator : IDictionaryEnumerator, IEnumerator
        {
            #region Readonly & Static Fields

            private static readonly string xstr = "SortedList.Enumerator: snapshot out of sync.";

            #endregion

            #region Fields

            private object currentKey;
            private object currentValue;
            private SortedList<TKey, TValue> host;

            private bool invalid = false;
            private EnumeratorMode mode;
            private int pos;
            private int size;
            private int stamp;

            #endregion

            #region Constructors

            public Enumerator( SortedList<TKey, TValue> host, EnumeratorMode mode )
            {
                this.host = host;
                this.stamp = host.modificationCount;
                this.size = host.Count;
                this.mode = mode;
                this.Reset();
            }

            public Enumerator( SortedList<TKey, TValue> host )
                : this( host, EnumeratorMode.ENTRY_MODE )
            {
            }

            #endregion

            #region IDictionaryEnumerator Members

            public void Reset()
            {
                if ( this.host.modificationCount != this.stamp || this.invalid )
                {
                    throw new InvalidOperationException( xstr );
                }

                this.pos = -1;
                this.currentKey = null;
                this.currentValue = null;
            }

            public bool MoveNext()
            {
                if ( this.host.modificationCount != this.stamp || this.invalid )
                {
                    throw new InvalidOperationException( xstr );
                }

                KeyValuePair<TKey, TValue>[] table = this.host.table;

                if ( ++this.pos < this.size )
                {
                    KeyValuePair<TKey, TValue> entry = table[ this.pos ];

                    this.currentKey = entry.Key;
                    this.currentValue = entry.Value;
                    return true;
                }

                this.currentKey = null;
                this.currentValue = null;
                return false;
            }

            public DictionaryEntry Entry
            {
                get
                {
                    if ( this.invalid || this.pos >= this.size || this.pos == -1 )
                    {
                        throw new InvalidOperationException( xstr );
                    }

                    return new DictionaryEntry( this.currentKey,
                                                this.currentValue );
                }
            }

            public Object Key
            {
                get
                {
                    if ( this.invalid || this.pos >= this.size || this.pos == -1 )
                    {
                        throw new InvalidOperationException( xstr );
                    }
                    return this.currentKey;
                }
            }

            public Object Value
            {
                get
                {
                    if ( this.invalid || this.pos >= this.size || this.pos == -1 )
                    {
                        throw new InvalidOperationException( xstr );
                    }
                    return this.currentValue;
                }
            }

            public Object Current
            {
                get
                {
                    if ( this.invalid || this.pos >= this.size || this.pos == -1 )
                    {
                        throw new InvalidOperationException( xstr );
                    }

                    switch ( this.mode )
                    {
                        case EnumeratorMode.KEY_MODE:
                            return this.currentKey;
                        case EnumeratorMode.VALUE_MODE:
                            return this.currentValue;
                        case EnumeratorMode.ENTRY_MODE:
                            return this.Entry;

                        default:
                            throw new NotSupportedException( this.mode + " is not a supported mode." );
                    }
                }
            }

            #endregion
        }

        #endregion

        #region Nested Class: ListKeys

        private class ListKeys : IList<TKey>, ICollection, IEnumerable
        {
            #region Fields

            private SortedList<TKey, TValue> host;

            #endregion

            #region Constructors

            public ListKeys( SortedList<TKey, TValue> host )
            {
                if ( host == null )
                {
                    throw new ArgumentNullException();
                }

                this.host = host;
            }

            #endregion

            #region ICollection Members

            public virtual bool IsSynchronized
            {
                get { return ( (ICollection)this.host ).IsSynchronized; }
            }

            public virtual Object SyncRoot
            {
                get { return ( (ICollection)this.host ).SyncRoot; }
            }

            public virtual void CopyTo( Array array, int arrayIndex )
            {
                this.host.CopyToArray( array, arrayIndex, EnumeratorMode.KEY_MODE );
            }

            #endregion

            #region IList<TKey> Members

            public virtual void Add( TKey item )
            {
                throw new NotSupportedException();
            }

            public virtual bool Remove( TKey key )
            {
                throw new NotSupportedException();
            }

            public virtual void Clear()
            {
                throw new NotSupportedException();
            }

            public virtual void CopyTo( TKey[] array, int arrayIndex )
            {
                if ( this.host.Count == 0 )
                {
                    return;
                }
                if ( array == null )
                {
                    throw new ArgumentNullException( "array" );
                }
                if ( arrayIndex < 0 )
                {
                    throw new ArgumentOutOfRangeException();
                }
                if ( arrayIndex >= array.Length )
                {
                    throw new ArgumentOutOfRangeException( "arrayIndex is greater than or equal to array.Length" );
                }
                if ( this.Count > ( array.Length - arrayIndex ) )
                {
                    throw new ArgumentOutOfRangeException( "Not enough space in array from arrayIndex to end of array" );
                }

                int j = arrayIndex;
                for ( int i = 0; i < this.Count; ++i )
                {
                    array[ j ++ ] = this.host.KeyAt( i );
                }
            }

            public virtual bool Contains( TKey item )
            {
                return this.host.IndexOfKey( item ) > -1;
            }

            public virtual int IndexOf( TKey item )
            {
                return this.host.IndexOfKey( item );
            }

            public virtual void Insert( int index, TKey item )
            {
                throw new NotSupportedException();
            }

            public virtual void RemoveAt( int index )
            {
                throw new NotSupportedException();
            }

            public virtual TKey this[ int index ]
            {
                get { return this.host.KeyAt( index ); }
                set { throw new NotSupportedException( "attempt to modify a key" ); }
            }

            public virtual IEnumerator<TKey> GetEnumerator()
            {
                /* We couldn't use yield as it does not support Reset () */
                return new KeyEnumerator( this.host );
            }

            public virtual int Count
            {
                get { return this.host.Count; }
            }

            public virtual bool IsReadOnly
            {
                get { return true; }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                for ( int i = 0; i < this.host.Count; ++i )
                {
                    yield return this.host.KeyAt( i );
                }
            }

            #endregion
        }

        #endregion

        #region Nested Class: ListValues

        private class ListValues : IList<TValue>, ICollection, IEnumerable
        {
            #region Fields

            private SortedList<TKey, TValue> host;

            #endregion

            #region Constructors

            public ListValues( SortedList<TKey, TValue> host )
            {
                if ( host == null )
                {
                    throw new ArgumentNullException();
                }

                this.host = host;
            }

            #endregion

            #region ICollection Members

            public virtual bool IsSynchronized
            {
                get { return ( (ICollection)this.host ).IsSynchronized; }
            }

            public virtual Object SyncRoot
            {
                get { return ( (ICollection)this.host ).SyncRoot; }
            }

            public virtual void CopyTo( Array array, int arrayIndex )
            {
                this.host.CopyToArray( array, arrayIndex, EnumeratorMode.VALUE_MODE );
            }

            #endregion

            #region IList<TValue> Members

            public virtual void Add( TValue item )
            {
                throw new NotSupportedException();
            }

            public virtual bool Remove( TValue value )
            {
                throw new NotSupportedException();
            }

            public virtual void Clear()
            {
                throw new NotSupportedException();
            }

            public virtual void CopyTo( TValue[] array, int arrayIndex )
            {
                if ( this.host.Count == 0 )
                {
                    return;
                }
                if ( array == null )
                {
                    throw new ArgumentNullException( "array" );
                }
                if ( arrayIndex < 0 )
                {
                    throw new ArgumentOutOfRangeException();
                }
                if ( arrayIndex >= array.Length )
                {
                    throw new ArgumentOutOfRangeException( "arrayIndex is greater than or equal to array.Length" );
                }
                if ( this.Count > ( array.Length - arrayIndex ) )
                {
                    throw new ArgumentOutOfRangeException( "Not enough space in array from arrayIndex to end of array" );
                }

                int j = arrayIndex;
                for ( int i = 0; i < this.Count; ++i )
                {
                    array[ j ++ ] = this.host.ValueAt( i );
                }
            }

            public virtual bool Contains( TValue item )
            {
                return this.host.IndexOfValue( item ) > -1;
            }

            public virtual int IndexOf( TValue item )
            {
                return this.host.IndexOfValue( item );
            }

            public virtual void Insert( int index, TValue item )
            {
                throw new NotSupportedException();
            }

            public virtual void RemoveAt( int index )
            {
                throw new NotSupportedException();
            }

            public virtual TValue this[ int index ]
            {
                get { return this.host.ValueAt( index ); }
                set { throw new NotSupportedException( "attempt to modify a key" ); }
            }

            public virtual IEnumerator<TValue> GetEnumerator()
            {
                /* We couldn't use yield as it does not support Reset () */
                return new ValueEnumerator( this.host );
            }

            public virtual int Count
            {
                get { return this.host.Count; }
            }

            public virtual bool IsReadOnly
            {
                get { return true; }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                for ( int i = 0; i < this.host.Count; ++i )
                {
                    yield return this.host.ValueAt( i );
                }
            }

            #endregion
        }

        #endregion

        #region IDictionary Members

        bool ICollection.IsSynchronized
        {
            get { return false; }
        }

        Object ICollection.SyncRoot
        {
            get { return this; }
        }

        bool IDictionary.IsFixedSize
        {
            get { return false; }
        }

        bool IDictionary.IsReadOnly
        {
            get { return false; }
        }

        object IDictionary.this[ object key ]
        {
            get
            {
                if ( !( key is TKey ) )
                {
                    return null;
                }
                else
                {
                    return this[ (TKey)key ];
                }
            }

            set { this[ this.ToKey( key ) ] = this.ToValue( value ); }
        }

        ICollection IDictionary.Keys
        {
            get { return new ListKeys( this ); }
        }

        ICollection IDictionary.Values
        {
            get { return new ListValues( this ); }
        }

        public void Clear()
        {
            this.defaultCapacity = INITIAL_SIZE;
            this.table = new KeyValuePair<TKey, TValue>[this.defaultCapacity];
            this.inUse = 0;
            this.modificationCount++;
        }

        void IDictionary.Add( object key, object value )
        {
            this.PutImpl( this.ToKey( key ), this.ToValue( value ), false );
        }

        bool IDictionary.Contains( object key )
        {
            if ( null == key )
            {
                throw new ArgumentNullException();
            }
            if ( !( key is TKey ) )
            {
                return false;
            }

            return ( this.Find( (TKey)key ) >= 0 );
        }

        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            return new Enumerator( this, EnumeratorMode.ENTRY_MODE );
        }

        void IDictionary.Remove( object key )
        {
            if ( null == key )
            {
                throw new ArgumentNullException( "key" );
            }
            if ( !( key is TKey ) )
            {
                return;
            }
            int i = this.IndexOfKey( (TKey)key );
            if ( i >= 0 )
            {
                this.RemoveAt( i );
            }
        }

        void ICollection.CopyTo( Array array, int arrayIndex )
        {
            if ( this.Count == 0 )
            {
                return;
            }

            if ( null == array )
            {
                throw new ArgumentNullException();
            }

            if ( arrayIndex < 0 )
            {
                throw new ArgumentOutOfRangeException();
            }

            if ( array.Rank > 1 )
            {
                throw new ArgumentException( "array is multi-dimensional" );
            }
            if ( arrayIndex >= array.Length )
            {
                throw new ArgumentNullException( "arrayIndex is greater than or equal to array.Length" );
            }
            if ( this.Count > ( array.Length - arrayIndex ) )
            {
                throw new ArgumentNullException( "Not enough space in array from arrayIndex to end of array" );
            }

            IEnumerator<KeyValuePair<TKey, TValue>> it = this.GetEnumerator();
            int i = arrayIndex;

            while ( it.MoveNext() )
            {
                array.SetValue( it.Current, i++ );
            }
        }

        #endregion

        #region IDictionary<TKey,TValue> Members

        public int Count
        {
            get { return this.inUse; }
        }

        public TValue this[ TKey key ]
        {
            get
            {
                if ( key == null )
                {
                    throw new ArgumentNullException( "key" );
                }

                int i = this.Find( key );

                if ( i >= 0 )
                {
                    return this.table[ i ].Value;
                }
                else
                {
                    throw new KeyNotFoundException();
                }
            }
            set
            {
                if ( key == null )
                {
                    throw new ArgumentNullException( "key" );
                }

                this.PutImpl( key, value, true );
            }
        }

        ICollection<TKey> IDictionary<TKey, TValue>.Keys
        {
            get { return this.Keys; }
        }

        ICollection<TValue> IDictionary<TKey, TValue>.Values
        {
            get { return this.Values; }
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly
        {
            get { return false; }
        }

        public void Add( TKey key, TValue value )
        {
            if ( key == null )
            {
                throw new ArgumentNullException( "key" );
            }

            this.PutImpl( key, value, false );
        }

        public bool ContainsKey( TKey key )
        {
            if ( key == null )
            {
                throw new ArgumentNullException( "key" );
            }

            return ( this.Find( key ) >= 0 );
        }

        public bool Remove( TKey key )
        {
            if ( key == null )
            {
                throw new ArgumentNullException( "key" );
            }

            int i = this.IndexOfKey( key );
            if ( i >= 0 )
            {
                this.RemoveAt( i );
                return true;
            }
            else
            {
                return false;
            }
        }

        void ICollection<KeyValuePair<TKey, TValue>>.Clear()
        {
            this.defaultCapacity = INITIAL_SIZE;
            this.table = new KeyValuePair<TKey, TValue>[this.defaultCapacity];
            this.inUse = 0;
            this.modificationCount++;
        }

        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo( KeyValuePair<TKey, TValue>[] array, int arrayIndex )
        {
            if ( this.Count == 0 )
            {
                return;
            }

            if ( null == array )
            {
                throw new ArgumentNullException();
            }

            if ( arrayIndex < 0 )
            {
                throw new ArgumentOutOfRangeException();
            }

            if ( arrayIndex >= array.Length )
            {
                throw new ArgumentNullException( "arrayIndex is greater than or equal to array.Length" );
            }
            if ( this.Count > ( array.Length - arrayIndex ) )
            {
                throw new ArgumentNullException( "Not enough space in array from arrayIndex to end of array" );
            }

            int i = arrayIndex;
            foreach ( KeyValuePair<TKey, TValue> pair in this )
            {
                array[ i++ ] = pair;
            }
        }

        void ICollection<KeyValuePair<TKey, TValue>>.Add( KeyValuePair<TKey, TValue> keyValuePair )
        {
            this.Add( keyValuePair.Key, keyValuePair.Value );
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Contains( KeyValuePair<TKey, TValue> keyValuePair )
        {
            int i = this.Find( keyValuePair.Key );

            if ( i >= 0 )
            {
                return Comparer<KeyValuePair<TKey, TValue>>.Default.Compare( this.table[ i ], keyValuePair ) == 0;
            }
            else
            {
                return false;
            }
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Remove( KeyValuePair<TKey, TValue> keyValuePair )
        {
            int i = this.Find( keyValuePair.Key );

            if ( i >= 0 && ( Comparer<KeyValuePair<TKey, TValue>>.Default.Compare( this.table[ i ], keyValuePair ) == 0 ) )
            {
                this.RemoveAt( i );
                return true;
            }
            else
            {
                return false;
            }
        }

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        {
            for ( int i = 0; i < this.inUse; i ++ )
            {
                KeyValuePair<TKey, TValue> current = this.table[ i ];

                yield return new KeyValuePair<TKey, TValue>( current.Key, current.Value );
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public bool TryGetValue( TKey key, out TValue value )
        {
            if ( key == null )
            {
                throw new ArgumentNullException( "key" );
            }

            int i = this.Find( key );

            if ( i >= 0 )
            {
                value = this.table[ i ].Value;
                return true;
            }
            else
            {
                value = default ( TValue );
                return false;
            }
        }

        #endregion
    }
}
