using System;
using System.Collections;
using System.Diagnostics;

using Axiom.MathLib;

// used to alias a type in the code for easy copying and pasting.  Come on generics!!
using T = Axiom.MathLib.Vector3;

namespace Axiom.MathLib.Collections {
    /// <summary>
    /// Summary description for Vector3Collection.
    /// </summary>
    public class Vector3Collection : BaseCollection {
        #region Constructors

        /// <summary>
        ///		Default constructor.
        /// </summary>
        public Vector3Collection() : base() {}

        #endregion

        #region Strongly typed methods and indexers

        /// <summary>
        ///		Get/Set indexer that allows access to the collection by index.
        /// </summary>
        new public T this[int index] {
            get { return (T)base[index]; }
            set { base[index] = value; }
        }

        /// <summary>
        ///		Adds an object to the collection.
        /// </summary>
        /// <param name="item"></param>
        public void Add(T item) {
            base.Add(item);
        }

        #endregion

    }
}
