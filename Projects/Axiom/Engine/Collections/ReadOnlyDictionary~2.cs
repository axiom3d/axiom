using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Axiom.Collections
{
	public class ReadOnlyDictionary<TKey, TValue> /*: ICollection, ICollection<KeyValuePair<TKey, TValue>>,
													IDictionary, IDictionary<TKey, TValue>,
													IEnumerable, IEnumerable<KeyValuePair<TKey, TValue>> */
	{
		private IDictionary<TKey, TValue> dictionary;

		public ReadOnlyDictionary( IDictionary<TKey, TValue> dictionary )
		{
			this.dictionary = dictionary;
		}

		public TValue this[ TKey index ]
		{
			get
			{
				return dictionary[ index ];
			}
		}
	}
}