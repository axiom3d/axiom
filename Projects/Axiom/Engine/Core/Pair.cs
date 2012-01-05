#region LGPL License

/*
Axiom Graphics Engine Library
Copyright © 2003-2011 Axiom Project Team

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

#endregion LGPL License

#region SVN Version Information

// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <id value="$Id: Pair.cs 1537 2009-03-30 19:25:01Z borrillis $"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;

using Axiom.Math;

#endregion Namespace Declarations

namespace Axiom.Core
{
	/// <summary>
	/// 	A simple container class for returning a pair of objects from a method call
	/// 	(similar to std::pair).
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class Pair<T> : IEquatable<Pair<T>>
	{
		private Tuple<T, T> data;

		public T First { get { return data.First; } set { data = new Tuple<T, T>( value, data.Second ); } }

		public T Second { get { return data.Second; } set { data = new Tuple<T, T>( data.First, value ); } }

		public Pair( T first, T second )
		{
			data = new Tuple<T, T>( first, second );
		}

		#region IEquatable<Pair<T>> Implementation

		public bool Equals( Pair<T> other )
		{
			return this.data.Equals( other.data );
		}

		public override bool Equals( object other )
		{
			if( other is Pair<T> )
			{
				return this.Equals( (Pair<T>)other );
			}
			return false;
		}

		#endregion IEquatable<Pair<T>> Implementation

		#region System.Object Implementation

		public override int GetHashCode()
		{
			return this.First.GetHashCode() ^ this.Second.GetHashCode();
		}

		#endregion System.Object Implementation
	}
}
