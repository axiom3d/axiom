#region LGPL License

// Axiom Graphics Engine Library
// Copyright © 2003-2011 Axiom Project Team
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

#endregion Namespace Declarations

namespace Axiom.Collections
{
	/// <summary>
	/// Represents a collection of key/value pairs that are sorted by the keys and are accessible by key and by index.
	/// </summary>
	public class HashList<TKey, TValue> : AxiomSortedCollection<TKey, TValue>
	{
		#region Instance Indexers

		/// <summary>
		/// Gets the <see cref="TValue"/> at the specified index.
		/// </summary>
		/// <value>A <see cref="TValue"/>.</value>
		public TValue this[ int index ] { get { return Values[ index ]; } }

		#endregion

		#region Instance Methods

		/// <summary>
		/// Gets a <see cref="TValue"/> by key.
		/// </summary>
		/// <param name="key">The key.</param>
		/// <returns>The <see cref="TValue"/> that corresponds to the specified key.</returns>
		public TValue GetByKey( TKey key )
		{
			return base[ key ];
		}

		/// <summary>
		/// Gets the key at the specified index.
		/// </summary>
		/// <param name="index">The index.</param>
		/// <returns>The key at the specified index.</returns>
		public TKey GetKeyAt( int index )
		{
			return Keys[ index ];
		}

		#endregion
	}
}
