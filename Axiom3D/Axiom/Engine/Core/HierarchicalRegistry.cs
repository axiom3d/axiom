// arilou

using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;

namespace Axiom
{
    /// <summary>
    /// Represents a registry of objects that supports hierarchical layout
    /// </summary>
    /// <typeparam name="T">contained object type</typeparam>
    public class HierarchicalRegistry<T> : Registry<string, T>
    {
        /// <summary>
        /// Allows to iterate thru all contained objects which keys begin with the 
        /// same string 
        /// </summary>
        /// <param name="prefix">key prefix</param>
        /// <returns>IEnumerable of T</returns>
        /// <remarks>The main purpose of this method is to allow the following
        /// code snippets:
        /// foreach(IPlugin plug in plugins.Subtree("/Axiom/RenderSystems")
        /// </remarks>
        /// <todo>
        /// 1. Allow simple pattern matching (* and ?)
        /// </todo>
        public IEnumerable<T> Subtree(string prefix)
        {
            IEnumerator enu = ((IEnumerable)objectList).GetEnumerator();

            while (enu.MoveNext())
            {
                KeyValuePair<string, T> de = (KeyValuePair<string, T>)enu.Current;
                string key = de.Key;
                T val = de.Value;

                if (key.StartsWith(prefix))
                    yield return val;
            }
        }
    }
}
