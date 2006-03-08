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

namespace Axiom
{
    /// <summary>
    /// Specifies how a class should extend the <see cref="HierarchicalRegistry"/> 
    /// namespace
    /// </summary>
    /// <remarks>The underlying implementation can support any source of 
    /// contained data. The only requirement enforced by the interface is that
    /// the namespace should be (naturally) enumerable</remarks>
    public interface INamespaceExtender
    {
        /// <summary>
        /// Allows to iterate thru the objects this namespace extender is supposed
        /// to contain filtered by object type
        /// </summary>
        /// <remarks>
        /// The returned list should contain object of :
        /// a) K -OR-
        /// b) Subclass of K -OR-
        /// c) If K is an interface, then implementation of K
        /// </remarks>
        IEnumerable<K> Subtree<K>();

        /// <summary>
        /// Gets the namespace that is extended using this extender
        /// </summary>
        string Namespace { get; }

        /// <summary>
        /// Returns the object contained in the namespace
        /// </summary>
        /// <typeparam name="K">type to convert the object to</typeparam>
        /// <param name="objectName">namespace qualified object name</param>
        /// <returns>object reference</returns>
        /// <remarks>
        /// The object should be returned if
        /// a) Its type equals K -OR-
        /// b) Its type is subclass of K -OR-
        /// c) If K is an interface, then if object implements it
        /// </remarks>
        K GetObject<K>(string objectName);
    }
}
