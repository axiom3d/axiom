using System;
using System.Collections;

namespace Axiom.MathLib.Collections
{
    /// <summary>
    ///		Serves as a basis for strongly typed collections in the math lib.
    /// </summary>
    /// <remarks>
    ///		Can't wait for Generics in .Net Framework 2.0!   
    /// </remarks>
    public abstract class BaseCollection : ICollection, IEnumerable, IEnumerator
    {
        /// <summary></summary>
        protected ArrayList objectList;
        //		protected int nextUniqueKeyCounter;

        const int INITIAL_CAPACITY = 50;

        #region Constructors

        /// <summary>
        ///		
        /// </summary>
        public BaseCollection()
        {
            objectList = new ArrayList( INITIAL_CAPACITY );
        }

        #endregion

        /// <summary>
        ///		
        /// </summary>
        public object this[int index]
        {
            get
            {
                return objectList[index];
            }
            set
            {
                objectList[index] = value;
            }
        }

        /// <summary>
        ///		Adds an item to the collection.
        /// </summary>
        /// <param name="item"></param>
        protected void Add( object item )
        {
            objectList.Add( item );
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
        public void Remove( object item )
        {
            int index = objectList.IndexOf( item );

            if ( index != -1 )
                objectList.RemoveAt( index );
        }

        #region Implementation of ICollection

        /// <summary>
        /// 
        /// </summary>
        /// <param name="array"></param>
        /// <param name="index"></param>
        public void CopyTo( System.Array array, int index )
        {
            objectList.CopyTo( array, index );
        }

        /// <summary>
        /// 
        /// </summary>
        public bool IsSynchronized
        {
            get
            {
                return objectList.IsSynchronized;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public int Count
        {
            get
            {
                return objectList.Count;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public object SyncRoot
        {
            get
            {
                return objectList.SyncRoot;
            }
        }

        #endregion

        #region Implementation of IEnumerable

        public System.Collections.IEnumerator GetEnumerator()
        {
            return (IEnumerator)this;
        }

        #endregion

        #region Implementation of IEnumerator

        private int position = -1;

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

            if ( position >= objectList.Count )
                return false;
            else
                return true;
        }

        /// <summary>
        ///		Returns the current object in the enumeration.
        /// </summary>
        public object Current
        {
            get
            {
                return objectList[position];
            }
        }
        #endregion
    }
}
