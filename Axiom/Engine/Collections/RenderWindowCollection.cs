#region LGPL License
/*
Axiom Game Engine Library
Copyright (C) 2003  Axiom Project Team

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
using System.Collections;
using System.Diagnostics;

using Axiom.Core;

// used to alias a type in the code for easy copying and pasting.  Come on generics!!
using T = Axiom.SubSystems.Rendering.RenderWindow;
// used to alias a key value in the code for easy copying and pasting.  Come on generics!!
using K = System.String;
// used to alias a parent type in the code for easy copying and pasting.  Come on generics!!
//using P = Axiom.Core.Entity;

namespace Axiom.Collections
{
	/// <summary>
	/// Summary description for RenderWindowCollection.
	/// </summary>
	public class RenderWindowCollection : AxiomCollection
	{
		#region Constructors

		/// <summary>
		///		Default constructor.
		/// </summary>
		public RenderWindowCollection() : base() {}

		/// <summary>
		///		Constructor that takes a parent object to, and calls the base class constructor to 
		/// </summary>
		/// <param name="entity"></param>
		//public RenderWindowCollection(P parent) : base(parent) {}

		#endregion

		#region Strongly typed methods and indexers

		/// <summary>
		///		Get/Set indexer that allows access to the collection by index.
		/// </summary>
		new public T this[int index]
		{
			get { return (T)base[index]; }
			set { base[index] = value; }
		}

		/// <summary>
		///		Get/Set indexer that allows access to the collection by key value.
		/// </summary>
		public T this[K key]
		{
			get { return (T)base[key]; }
			set { base[key] = value; }
		}

		/// <summary>
		///		Adds an object to the collection.
		/// </summary>
		/// <param name="item"></param>
		public void Add(T item)
		{
			Add(item.Name, item);
		}

		/// <summary>
		///		Adds a named object to the collection.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="item"></param>
		public void Add(K key, T item)
		{
			base.Add(key, item);
		}

		#endregion

	}
}
