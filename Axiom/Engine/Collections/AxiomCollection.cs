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

namespace Axiom.Collections
{

	public delegate bool CollectionHandler(object source, System.EventArgs e);

	/// <summary>
	///		Serves as a basis for strongly typed collections in the engine.
	/// </summary>
	/// <remarks>
	///		<b>GODDAMIT WHY DO WE HAVE TO GO THROUGH THIS SHIT!</b>
	///		Can't wait for Generics in .Net Framework 2.0!   
	/// </remarks>
	// BUG: Problem with enumerators, fix later as they dont need to be used in the engine anyway.
	public abstract class AxiomCollection : ICollection, IEnumerable, IEnumerator
	{
		/// <summary></summary>
		protected SortedList objectList;
		/// <summary></summary>
		protected Object parent;
		static protected int nextUniqueKeyCounter;
		
		const int INITIAL_CAPACITY = 20;

		#region Constructors

		/// <summary>
		///		
		/// </summary>
		public AxiomCollection()
		{
			this.parent = null;
			objectList = new SortedList(INITIAL_CAPACITY);
		}

		/// <summary>
		///		
		/// </summary>
		/// <param name="parent"></param>
		public AxiomCollection(Object parent)
		{
			this.parent = parent;
			objectList = new SortedList(INITIAL_CAPACITY);
		}

		#endregion

		/// <summary>
		///		
		/// </summary>
		public object this[int index] 
		{ 
			get { return objectList.GetByIndex(index); } 
			set { objectList.SetByIndex(index, value); }
		}

		/// <summary>
		///		
		/// </summary>
		protected object this[object key] 
		{ 
			get { return objectList[key]; } 
			set { objectList[key] = value; }
		}

		/// <summary>
		///		Accepts an unnamed object and names it manually.
		/// </summary>
		/// <param name="item"></param>
		protected void Add(object item)
		{
			// fire the ItemAdded event for all handlers registered
			// don't add the item if true is returned from OnItemAdded
			if(OnItemAdded(item))
				return;

			objectList.Add("Object" + (nextUniqueKeyCounter++), item);
		}

		/// <summary>
		///		Adds a named object to the collection.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="item"></param>
		protected void Add(object key, object item)
		{
			// fire the ItemAdded event for all handlers registered
			// don't add the item if true is returned from OnItemAdded
			if(OnItemAdded(item))
				return;

			objectList.Add(key, item);
		}

		/// <summary>
		///		Clears all objects from the collection.
		/// </summary>
		public void Clear()
		{
			// fire the Cleared event for all that care
			// don't clear if events return true
			if(OnCleared())
				return;

			objectList.Clear();
		}

		/// <summary>
		///		Removes the item from the collection.
		/// </summary>
		/// <param name="item"></param>
		public void Remove(object item)
		{
			// fire the ItemRemoved event for all that care
			// dont remove the item if true is returned
			if(OnItemRemoved(item))
				return;

			int index = objectList.IndexOfValue(item);

			if(index != -1)
				objectList.RemoveAt(index);
		}

		/// <summary>
		///		Tests if there is a dupe entry in here.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public bool ContainsKey(object key)
		{
			return objectList.ContainsKey(key);
		}

		#region Implementation of ICollection

		public void CopyTo(System.Array array, int index)
		{
			objectList.CopyTo(array, index);
		}

		public bool IsSynchronized
		{
			get
			{
				return objectList.IsSynchronized;
			}
		}

		public int Count
		{
			get
			{
				return objectList.Count;
			}
		}

		public object SyncRoot
		{
			get
			{
				return objectList.SyncRoot;
			}
		}

		#endregion

		#region Implementation of IEnumerable

		public System.Collections.IEnumerator GetEnumerator()
		{
			return (IEnumerator)this;
		}

		#endregion

		#region Implementation of IEnumerator

		private int position = -1;

		/// <summary>
		///		Resets the in progress enumerator.
		/// </summary>
		public void Reset()
		{
			// reset the enumerator position
			position = -1;
		}

		/// <summary>
		///		Moves to the next item in the enumeration if there is one.
		/// </summary>
		/// <returns></returns>
		public bool MoveNext()
		{
			position += 1;

			if(position >= objectList.Count)
				return false;
			else
				return true;
		}

		/// <summary>
		///		Returns the current object in the enumeration.
		/// </summary>
		public object Current
		{
			get { return objectList.GetByIndex(position); }
		}
		#endregion

		#region Events

		/// <summary>An event that is fired when items are added to the collection. </summary>
		public event CollectionHandler ItemAdded;
		/// <summary>An event that is fired when items are removed from the collection. </summary>
		public event CollectionHandler ItemRemoved;
		/// <summary>An event that is fired when the collection is cleared. </summary>
		public event CollectionHandler Cleared;

		/// <summary>
		///		Called to fire the ItemAdded event.
		/// </summary>
		public bool OnItemAdded(object item)
		{
			if(ItemAdded != null)
				return ItemAdded(item, new EventArgs());

			return false;
		}

		/// <summary>
		///		Called to fire the ItemRemoved event.
		/// </summary>
		/// <param name="item"></param>
		public bool OnItemRemoved(object item)
		{
			if(ItemRemoved != null)
				return ItemRemoved(this, new EventArgs());

			return false;
		}

		/// <summary>
		///  Called to fire the Cleared event
		/// </summary>
		public bool OnCleared()
		{
			if(Cleared != null)
				return Cleared(this, new EventArgs());

			return false;
		}

		#endregion


	}
}
