using System;
using System.Collections;

namespace Chronos.Diagnostics
{
	/// <summary>
	/// Provides a thread-safe list of ILogListener objects.
	/// </summary>
	public class LogListenerCollection : IEnumerable, ICollection, IList
	{
		private ArrayList list;

		public LogListenerCollection()
		{ 
			list = ArrayList.Synchronized(new ArrayList());
		}

		public ILogListener this[int index] 
		{
			get { return (ILogListener) list[index]; }
			set { list[index] = value; }
		}
  
		public void Add(ILogListener value)
		{
			list.Add(value);
		}
  
		public void AddRange(LogListenerCollection listeners)
		{
			list.AddRange(listeners);
		}
  
		public int BinarySearch(ILogListener value)
		{
			return list.BinarySearch(value);
		}
  
		public int BinarySearch(ILogListener value, IComparer comparer)
		{
			return list.BinarySearch(value, comparer);
		}
  
		public int BinarySearch(int index, int count, ILogListener value, IComparer comparer)
		{
			return list.BinarySearch(index, count, value, comparer);
		}
  
		public bool Contains(ILogListener item)
		{
			return list.Contains(item);
		}
  
		public int IndexOf(ILogListener value)
		{
			return list.IndexOf(value);
		}
  
		public int IndexOf(ILogListener value, int startIndex)
		{
			return list.IndexOf(value, startIndex);
		}
  
		public int IndexOf(ILogListener value, int startIndex, int count)
		{
			return list.IndexOf(value, startIndex, count);
		}
  
		public void Insert(int index, ILogListener value)
		{
			list.Insert(index, value);
		}
  
		public int LastIndexOf(ILogListener value)
		{
			return list.LastIndexOf(value);
		}
  
		public int LastIndexOf(ILogListener value, int startIndex)
		{
			return list.LastIndexOf(value, startIndex);
		}
  
		public int LastIndexOf(ILogListener value, int startIndex, int count)
		{
			return list.LastIndexOf(value, startIndex, count);
		}
  
		public void Remove(ILogListener value)
		{
			list.Remove(value);
		}
  
		public ILogListener[] ToArray()
		{
			return (ILogListener[]) list.ToArray();
		}
  
		#region LogListenerCollectionEnumerator
  
		public class LogListenerCollectionEnumerator : IEnumerator
		{
			private int index;
			private ILogListener current;
			private LogListenerCollection collection;
    
			internal LogListenerCollectionEnumerator(LogListenerCollection collection)
				: base()
			{
				this.index = -1;
				this.collection = collection;
			}
  
			public object Current 
			{
				get 
				{
					if(index == -1 || index >= collection.Count) 
					{
						throw new IndexOutOfRangeException("Enumerator not started.");
					}
					return current;
				}
			}
  
			public void Reset()
			{
				index = -1;
				current = null;      
			}
  
			public bool MoveNext()
			{
				if(index < (collection.Count - 1)) 
				{
					index++;
					current = collection[index];
					return true;
				} 
				else 
				{
					index = collection.Count;
					return false;
				}
			}
		}
  
		#endregion

		#region IEnumerable Members

		IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return new LogListenerCollectionEnumerator(this);
		}

		#endregion

		#region ICollection Members

		public bool IsSynchronized 
		{
			get { return list.IsSynchronized; }
		}

		public int Count 
		{
			get { return list.Count; }
		}

		public void CopyTo(Array array, int index)
		{
			list.CopyTo(array, index);
		}

		public object SyncRoot 
		{
			get { return list.SyncRoot; }
		}

		#endregion

		#region IList Members

		bool System.Collections.IList.IsReadOnly 
		{
			get { return list.IsReadOnly; }
		}

		object System.Collections.IList.this[int index] 
		{
			get { return (LogListener) list[index]; }
			set { list[index] = value; }
		}

		void System.Collections.IList.RemoveAt(int index)
		{
			list.RemoveAt(index);
		}

		void System.Collections.IList.Insert(int index, object value)
		{
			list.Insert(index, value);
		}

		void System.Collections.IList.Remove(object value)
		{
			list.Remove(value);
		}

		bool System.Collections.IList.Contains(object value)
		{
			return list.Contains(value);
		}

		void System.Collections.IList.Clear()
		{
			list.Clear();
		}

		int System.Collections.IList.IndexOf(object value)
		{
			return list.IndexOf(value);
		}

		int System.Collections.IList.Add(object value)
		{
			return list.Add(value);
		}

		bool System.Collections.IList.IsFixedSize 
		{
			get { return list.IsFixedSize; }
		}

		#endregion
	}
}
