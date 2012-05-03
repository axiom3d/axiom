using System;
using System.Collections.Generic;
using System.Linq;

namespace Axiom.Core.Collections
{
	public class MultiMap<TKey, TValue> : Dictionary<TKey, List<TValue>>
	{
		public void Add( TKey key, TValue value )
		{
			List<TValue> values;
			if ( !TryGetValue( key, out values ) )
			{
				values = new List<TValue>();
				Add( key, values );
			}
			values.Add( value );
		}

		public void RemoveWhere( Func<TKey, TValue, bool> predicate )
		{
			foreach ( var values in this )
			{
				var key = values.Key;
				values.Value.RemoveAll( v => predicate( key, v ) );
			}
		}
	}
}