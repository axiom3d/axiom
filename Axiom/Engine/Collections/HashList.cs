using System;
using System.Collections;

namespace Axiom.Collections
{
	/// <summary>
	/// 	Summary description for HashList.
	/// </summary>
	public class HashList
	{
		Hashtable itemTable = new Hashtable();
		SortedList itemList = new SortedList();
		ArrayList itemKeys = new ArrayList();

		#region Member variables
		
		#endregion
		
		#region Constructors
		
		public HashList()
		{
			//
			// TODO: Add constructor logic here
			//
		}
		
		#endregion
		
		#region Methods
		
		public void Add(object key, object item)
		{
			itemTable.Add(key, item);
			itemList.Add(key, item);
			itemKeys.Add(key);
		}

		public object GetKeyAt(int index)
		{
			return itemKeys[index];
		}

		public object GetByKey(object key)
		{
			return itemTable[key];
		}

		public bool ContainsKey(object key)
		{
			return itemTable.ContainsKey(key);
		}

		public void Clear()
		{
			itemTable.Clear();
			itemList.Clear();
			itemKeys.Clear();
		}

		#endregion
		
		#region Properties
		
		public int Count
		{
			get { return itemList.Count; }
		}

		#endregion

		#region Operators

		public object this[int index]
		{
			get { return itemList.GetByIndex(index); }
		}

		public object this[object key]
		{
			get { return itemTable[key]; }
		}

		#endregion

	}
}
