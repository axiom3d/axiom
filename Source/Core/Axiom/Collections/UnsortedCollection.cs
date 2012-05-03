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
//     <id value="$Id: Driver.cs 884 2006-09-14 06:32:07Z borrillis $"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using System.Diagnostics;

#endregion

namespace Axiom.Collections
{
	/// <summary>
	/// Represents a strongly typed list of objects that can be accessed by index. Provides methods to search, sort, and manipulate lists.
	/// </summary>
	public class UnsortedCollection<T> : List<T>
	{
		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="T:System.Collections.Generic.List`1"/> class that is empty and has the default initial capacity.
		/// </summary>
		public UnsortedCollection() {}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:System.Collections.Generic.List`1"/> class that is empty and has the specified initial capacity.
		/// </summary>
		/// <param name="capacity">The number of elements that the new list can initially store.
		/// </param>
		/// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="capacity"/> is less than 0. 
		/// </exception>
		public UnsortedCollection( int capacity )
			: base( capacity ) {}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:System.Collections.Generic.List`1"/> class that contains elements copied from the specified collection and has sufficient capacity to accommodate the number of elements copied.
		/// </summary>
		/// <param name="collection">The collection whose elements are copied to the new list.
		/// </param>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="collection"/> is null.
		/// </exception>
		public UnsortedCollection( IEnumerable<T> collection )
			: base( collection ) {}

		#endregion
	}
}
