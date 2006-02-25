// arilou

using System;
using System.Collections.Generic;
using System.Text;

namespace Axiom
{
    /// <summary>
    /// ObjectResolve event arguments
    /// </summary>
    public class ObjectResolveEventArgs<K, T> 
    {
        private T _objectRef = default(T);
        /// <summary>
        /// Found object 
        /// </summary>
        public T ResolvedObject
        {
          get { return _objectRef; }
          set { _objectRef = value; }
        }

        private K _objectKey = default(K);
        /// <summary>
        /// Key for the object being looked up
        /// </summary>
        public K Key
        {
            get { return _objectKey; }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="objectKey">Key for the object being looked up</param>
        public ObjectResolveEventArgs(K objectKey)
        {
            _objectKey = objectKey;
        }
    }

    /// <summary>
    /// <see cref="Registry.ObjectResolve"/> event delegate type
    /// </summary>
    /// <typeparam name="T">object type</typeparam>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void ObjectResolveEventHandler<K, T>(object sender, ObjectResolveEventArgs<K, T> e);

    /// <summary>
    /// Named object registry with lazy-load behaviour
    /// </summary>
    public class Registry<K, T> : IEnumerable<T>
    {
        /// <summary>
        /// Registry contents
        /// </summary>
        protected Dictionary<K, T> objectList = new Dictionary<K, T>();

        /// <summary>
        /// Fires when a queried object is not found in the registry
        /// </summary>
        /// <remarks>This event allows to implement a lazy loading mechanism</remarks>
        public event ObjectResolveEventHandler<K, T> ObjectResolve;
        
        /// <summary>
        /// Fires the <see cref="ObjectResolve" /> event
        /// </summary>
        /// <param name="e">event arguments</param>
        protected virtual void OnObjectResolve(ObjectResolveEventArgs<K, T> e)
        {
            if (ObjectResolve != null)
                ObjectResolve(this, e);
        }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <param name="key">key</param>
        /// <returns></returns>
        /// <remarks>If the objected is not already contained in the registry,
        /// the <see cref="ObjectResolve"/> event is fired allowing to load the
        /// object from an external source</remarks>
        public T this[K key]
        {
            get
            {
                T obj = default(T);

                objectList.TryGetValue(key, out obj);

                if (object.ReferenceEquals(obj, default(T)))
                {
                    ObjectResolveEventArgs<K, T> args = new ObjectResolveEventArgs<K, T>(key);
                    OnObjectResolve(args);
                    obj = args.ResolvedObject;

                    if (args.ResolvedObject != null)
                        objectList.Add(key, args.ResolvedObject);
                }

                return obj;
            }
        }

        /// <summary>
        /// Adds a new object to the registry
        /// </summary>
        /// <param name="key">Key to store the object with</param>
        /// <param name="value">Object being stored</param>
        public virtual void Add(K key, T value)
        {
            objectList.Add(key, value);
        }

        /// <summary>
        /// Removes an object from the registry
        /// </summary>
        /// <param name="key">Key of the object being removed</param>
        public virtual void Remove(K key)
        {
            objectList.Remove(key);
        }

        #region IEnumerable<T> Members

        /// <summary>
        /// Gets the strongly-typed enumerator for the registry contents
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerator<T> GetEnumerator()
        {
            return objectList.Values.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return objectList.Values.GetEnumerator();
        }

        #endregion
    }
}
