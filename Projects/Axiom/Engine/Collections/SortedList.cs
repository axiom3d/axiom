using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Axiom.Collections
{
	public class SortedList : SortedList<object, object>
	{
		public SortedList( System.Collections.IComparer comparer, int capacity )
			: base( capacity, comparer )
		{

		}
	}
}