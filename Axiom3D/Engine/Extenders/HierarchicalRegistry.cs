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

using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;

namespace Axiom
{
    /// <summary>
    /// Represents a registry of objects that supports hierarchical layout
    /// and provides means of delegating namespace enumeration to third-party
    /// classes
    /// </summary>
    /// <typeparam name="T">contained object type</typeparam>
    /// <remarks>
    /// As a side effect of being able to extend the registry with custom namespaces,
    /// the hierarchical registry is not limited to containing objects only of type
    /// T. For example, the particle system manager extends its default plugin namespace
    /// (/Axiom/Plugins/ParticleFX to /Axiom/Plugins/ParticleFX/Emitters and
    /// /Axiom/Plugins/ParticleFX/Affectors
    /// </remarks>
    public class HierarchicalRegistry<T> : Registry<string, T>
    {
        /// <summary>
        /// Allows to iterate thru all contained objects which keys begin with the 
        /// same string 
        /// </summary>
        /// <param name="prefix">key prefix</param>
        /// <returns>IEnumerable</returns>
        /// <remarks>The main purpose of this method is to allow the following
        /// code snippet:
        /// foreach(IPlugin plug in plugins.Subtree("/Axiom/RenderSystems")
        /// </remarks>
        /// <todo>
        /// 1. Allow simple pattern matching (* and ?)
        /// </todo>
        public IEnumerable<K> Subtree<K>(string prefix)
        {
            // check for consistent input parameters
            if (!prefix.EndsWith("/"))
                throw new ArgumentException("Namespace prefix must end with a '/' symbol");

            // check if the passed namespace is actually extender-provided
            if (extenders.ContainsKey(prefix))
            {
                IEnumerable<K> extenderNamespace = extenders[prefix].Subtree<K>();
                foreach (K obj in extenderNamespace)
                    yield return obj;

                yield break;
            }

            IEnumerator enu = ((IEnumerable)objectList).GetEnumerator();

            while (enu.MoveNext())
            {
                KeyValuePair<string, K> de = (KeyValuePair<string, K>)enu.Current;
                string key = de.Key;
                K val = de.Value;

                if (key.StartsWith(prefix))
                    yield return val;
            }
        }

        #region namespace extender implementation
        /// <summary>
        /// Namespace extender container
        /// </summary>
        /// <remarks>Key is namespace, value - <see cref="INamespaceExtender"/> 
        /// object</remarks>
        protected Dictionary<string, INamespaceExtender> extenders =
            new Dictionary<string, INamespaceExtender>();

        /// <summary>
        /// Registers a new namespace extender with the registry
        /// </summary>
        /// <param name="extender">namespace extender object</param>
        public void RegisterNamespace(INamespaceExtender extender)
        {
            if (!extender.Namespace.EndsWith("/"))
                throw new ArgumentException("The passed INamespaceExtender implementation is " +
                    "invalid because the extended namespace does not end with a '/' symbol");

            extenders.Add(extender.Namespace, extender);
        }
        #endregion
    }
}
