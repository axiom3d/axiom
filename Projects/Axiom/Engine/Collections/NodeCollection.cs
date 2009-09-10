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

using Axiom.Core;

#endregion

namespace Axiom.Collections
{
    /// <summary>
    ///	Represents a collection of <see cref="Node">Nodes</see> that are sorted by name.
    /// </summary>
    [ Serializable ]
    public class NodeCollection : AxiomCollection<Node>
    {
        #region Instance Methods

        /// <summary>
        ///	Adds a <see cref="Node"/> to the collection and uses its name automatically as key.
        /// </summary>
        /// <param name="item">A <see cref="Node"/> to add to the collection.</param>
        public void Add( Node item )
        {
            Add( item.Name, item );
        }

        /// <summary>
        /// Determines whether the collection contains the specified <see cref="Node"/>.
        /// </summary>
        /// <param name="item">A <see cref="Node"/>.</param>
        /// <returns>
        /// 	<c>true</c> if the collection contains the specified <see cref="Node"/>; otherwise, <c>false</c>.
        /// </returns>
        public bool Contains( Node item )
        {
            return ContainsValue( item );
        }

        /// <summary>
        /// Determines whether the collection contains a <see cref="Node"/> with the specified name.
        /// </summary>
        /// <param name="key">The name of a <see cref="Node"/>.</param>
        /// <returns>
        /// 	<c>true</c> if the collection contains a <see cref="Node"/> with the specified name; otherwise, <c>false</c>.
        /// </returns>
        public bool Contains( string key )
        {
            return ContainsKey( key );
        }

        /// <summary>
        /// Searches for the specified name of a <see cref="Node"/> and returns the zero-based index within the entire collection.
        /// </summary>
        /// <param name="key">The name of a <see cref="Node"/> to locate in the collection.</param>
        /// <returns>The zero-based index of the <see cref="Node"/> with the specified name within the entire collection, if found;  otherwise, -1.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is a null reference.</exception>
        public int IndexOf( string key )
        {
            return IndexOfKey( key );
        }

        /// <summary>
        /// Searches for the specified <see cref="Node"/> and returns the zero-based index of the first occurrence within the entire collection.
        /// </summary>
        /// <param name="item">The <see cref="Node"/> to locate in the collection. The <see cref="Node"/> can be a null reference.</param>
        /// <returns>The zero-based index of the first occurrence of the <see cref="Node"/> within the entire collection, if found; otherwise, -1.</returns>
        public int IndexOf( Node item )
        {
            return IndexOfValue( item );
        }

        /// <summary>
        /// Removes the specified <see cref="Node"/>.
        /// </summary>
        /// <param name="item">The <see cref="Node"/> to remove.</param>
        public void Remove( Node item )
        {
            base.Remove( item.Name );
        }

        /// <summary>
        /// Removes the <see cref="Node"/> with the specified key.
        /// </summary>
        /// <param name="key">The name of the <see cref="Node"/> to remove.</param>
        public void Remove( string key )
        {
            base.RemoveByKey( key );
        }

        #endregion
    }
}