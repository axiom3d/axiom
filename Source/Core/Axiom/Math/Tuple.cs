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

#endregion

#region Namespace Declarations

using System;
using System.Collections.Generic;

#endregion Namespace Declarations

namespace Axiom.Math
{
	/// <summary>
	///	Represents two related values
	/// </summary>
	public class Tuple<A, B> : IEquatable<Tuple<A, B>>
	{
		#region Fields and Properties

		/// <summary></summary>
		public readonly A First;

		/// <summary></summary>
		public readonly B Second;

		#endregion Fields and Properties

		#region Construction and Destruction

		public Tuple( A first, B second )
		{
			this.First = first;
			this.Second = second;
		}

		#endregion Construction and Destruction

		#region IEquatable<Tuple<A,B>> Implementation

		public bool Equals( Tuple<A, B> other )
		{
			return this.First.Equals( other.First ) && this.Second.Equals( other.Second );
		}

		public override bool Equals( object other )
		{
			if ( other is Tuple<A, B> )
			{
				return Equals( (Tuple<A, B>)other );
			}
			return false;
		}

		#endregion IEquatable<Tuple<A,B>> Implementation
	}

	/// <summary>
	/// Represents three related values
	/// </summary>
	/// <typeparam name="A"></typeparam>
	/// <typeparam name="B"></typeparam>
	/// <typeparam name="C"></typeparam>
	public struct Tuple<A, B, C> : IEquatable<Tuple<A, B, C>>
	{
		#region Fields and Properties

		/// <summary></summary>
		public readonly A First;

		/// <summary></summary>
		public readonly B Second;

		/// <summary></summary>
		public readonly C Third;

		#endregion Fields and Properties

		#region Construction and Destruction

		public Tuple( A first, B second, C Third )
		{
			this.First = first;
			this.Second = second;
			this.Third = Third;
		}

		#endregion Construction and Destruction

		#region IEquatable<Tuple<A,B,C>> Implementation

		public bool Equals( Tuple<A, B, C> other )
		{
			return this.First.Equals( other.First ) && this.Second.Equals( other.Second ) && this.Third.Equals( other.Third );
		}

		public override bool Equals( object other )
		{
			if ( other is Tuple<A, B, C> )
			{
				return Equals( (Tuple<A, B, C>)other );
			}
			return false;
		}

		#endregion IEquatable<Tuple<A,B,C>> Implementation
	}

	/// <summary>
	/// Represents four related values
	/// </summary>
	/// <typeparam name="A"></typeparam>
	/// <typeparam name="B"></typeparam>
	/// <typeparam name="C"></typeparam>
	/// <typeparam name="D"></typeparam>
	public struct Tuple<A, B, C, D> : IEquatable<Tuple<A, B, C, D>>
	{
		#region Fields and Properties

		/// <summary></summary>
		public readonly A First;

		/// <summary></summary>
		public readonly B Second;

		/// <summary></summary>
		public readonly C Third;

		/// <summary></summary>
		public readonly D Fourth;

		#endregion Fields and Properties

		#region Construction and Destruction

		public Tuple( A first, B second, C third, D fourth )
		{
			this.First = first;
			this.Second = second;
			this.Third = third;
			this.Fourth = fourth;
		}

		#endregion Construction and Destruction

		#region IEquatable<Tuple<A,B,C,D>> Implementation

		public bool Equals( Tuple<A, B, C, D> other )
		{
			return this.First.Equals( other.First ) && this.Second.Equals( other.Second ) && this.Third.Equals( other.Third );
		}

		public override bool Equals( object other )
		{
			if ( other is Tuple<A, B, C, D> )
			{
				return Equals( (Tuple<A, B, C, D>)other );
			}
			return false;
		}

		#endregion IEquatable<Tuple<A,B,C,D>> Implementation
	}
}