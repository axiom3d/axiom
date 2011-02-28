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
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System.Collections.Generic;
using System.Diagnostics;

using Axiom.Core;
using Axiom.Collections;

#endregion

namespace Axiom.Collections
{
	/// <summary>
	/// Represents a collection of Viewports that are sorted by zOrder key based on the associated <see cref="IComparer"/> implementation.
	/// </summary>
	public class ViewportCollection : AxiomSortedCollection<int, Viewport>
	{
		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="T:System.Collections.Generic.SortedList`2"/> class that is empty, has the default initial capacity, and uses the default <see cref="T:System.Collections.Generic.IComparer`1"/>.
		/// </summary>
		public ViewportCollection()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:System.Collections.Generic.SortedList`2"/> class that is empty, has the specified initial capacity, and uses the default <see cref="T:System.Collections.Generic.IComparer`1"/>.
		/// </summary>
		/// <param name="capacity">The initial number of elements that the <see cref="T:System.Collections.Generic.SortedList`2"/> can contain.
		/// </param>
		/// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="capacity"/> is less than zero.
		/// </exception>
		public ViewportCollection( int capacity )
			: base( capacity )
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:System.Collections.Generic.SortedList`2"/> class that is empty, has the default initial capacity, and uses the specified <see cref="T:System.Collections.Generic.IComparer`1"/>.
		/// </summary>
		/// <param name="comparer">The <see cref="T:System.Collections.Generic.IComparer`1"/> implementation to use when comparing keys.
		/// -or-
		/// null to use the default <see cref="T:System.Collections.Generic.Comparer`1"/> for the type of the key.
		/// </param>
		public ViewportCollection( IComparer<int> comparer )
			: base( comparer )
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:System.Collections.Generic.SortedList`2"/> class that is empty, has the specified initial capacity, and uses the specified <see cref="T:System.Collections.Generic.IComparer`1"/>.
		/// </summary>
		/// <param name="capacity">The initial number of elements that the <see cref="T:System.Collections.Generic.SortedList`2"/> can contain.
		/// </param>
		/// <param name="comparer">The <see cref="T:System.Collections.Generic.IComparer`1"/> implementation to use when comparing keys.
		/// -or-
		/// null to use the default <see cref="T:System.Collections.Generic.Comparer`1"/> for the type of the key.
		/// </param>
		/// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="capacity"/> is less than zero.
		/// </exception>
		public ViewportCollection( int capacity, IComparer<int> comparer )
			: base( capacity, comparer )
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:System.Collections.Generic.SortedList`2"/> class that contains elements copied from the specified <see cref="T:System.Collections.Generic.IDictionary`2"/>, has sufficient capacity to accommodate the number of elements copied, and uses the default <see cref="T:System.Collections.Generic.IComparer`1"/>.
		/// </summary>
		/// <param name="dictionary">The <see cref="T:System.Collections.Generic.IDictionary`2"/> whose elements are copied to the new <see cref="T:System.Collections.Generic.SortedList`2"/>.
		/// </param>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="dictionary"/> is null.
		/// </exception>
		/// <exception cref="T:System.ArgumentException"><paramref name="dictionary"/> contains one or more duplicate keys.
		/// </exception>
		public ViewportCollection( IDictionary<int, Viewport> dictionary )
			: base( dictionary )
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:System.Collections.Generic.SortedList`2"/> class that contains elements copied from the specified <see cref="T:System.Collections.Generic.IDictionary`2"/>, has sufficient capacity to accommodate the number of elements copied, and uses the specified <see cref="T:System.Collections.Generic.IComparer`1"/>.
		/// </summary>
		/// <param name="dictionary">The <see cref="T:System.Collections.Generic.IDictionary`2"/> whose elements are copied to the new <see cref="T:System.Collections.Generic.SortedList`2"/>.
		/// </param><param name="comparer">The <see cref="T:System.Collections.Generic.IComparer`1"/> implementation to use when comparing keys.
		/// -or-
		/// null to use the default <see cref="T:System.Collections.Generic.Comparer`1"/> for the type of the key.
		/// </param>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="dictionary"/> is null.
		/// </exception>
		/// <exception cref="T:System.ArgumentException"><paramref name="dictionary"/> contains one or more duplicate keys.
		/// </exception>
		public ViewportCollection( IDictionary<int, Viewport> dictionary, IComparer<int> comparer )
			: base( dictionary, comparer )
		{
		}

		#endregion

		#region Instance Methods

		/// <summary>
		///	Adds a Viewport into the SortedList, automatically using its zOrder as key.
		/// </summary>
		/// <param name="item">A Viewport</param>
		public void Add( Viewport item )
		{
			Debug.Assert( !ContainsKey( item.ZOrder ), "A viewport with the specified ZOrder " + item.ZOrder + " already exists." );

			// Add the viewport
			Add( item.ZOrder, item );
		}

		#endregion
	}
}
