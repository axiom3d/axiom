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
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id:"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;

using Axiom.Core;
using Axiom.Graphics;

using System.Runtime.InteropServices;
using System.Reflection;

using Axiom.Utilities;

using System.Diagnostics;

#endregion Namespace Declarations

namespace Axiom.Collections
{
	/// <summary>
	/// Class for performing a radix sort (fast comparison-less sort based on
	/// byte value) on various standard containers.
	/// </summary>
	/// <remarks>
	/// A radix sort is a very fast sort algorithm. It only uses a single comparison
	/// and thus is able to break the theoretical minimum O(N*logN) complexity.
	/// Radix sort is complexity O(k*N), where k is a constant. Note that radix
	/// sorting is not in-place, it requires additional storage, so it trades
	/// memory for speed. The overhead of copying means that it is only faster
	/// for fairly large datasets, so you are advised to only use it for collections
	/// of at least a few thousand items.
	/// <para/>
	/// This is a generic class to allow it to deal with a variety of containers,
	/// and a variety of value types to sort on. In addition to providing the
	/// container and value type on construction, you also need to supply a
	/// functor object which will retrieve the value to compare on for each item
	/// in the list. For example, if you had an <see cref="IList{T}" /> of instances
	/// of an object of class 'Bibble', and you wanted to sort on
	/// Bibble.Doobrie, you'd have to create a delegate like this:
	/// <code>
	/// class BibbleSortDelegate
	/// {
	///		static float Doobrie( Bibble val)
	///		{
	///			return val.Doobrie;
	///		}
	///	}
	/// </code>
	/// Then, you need to declare a RadixSort class which names the container type,
	/// the value type in the container, and the type of the value you want to
	/// sort by. You can then call the sort function. E.g.
	/// <code>
	/// RadixSortSingle&lt;BibbleList, Bibble&gt; radixSorter;
	///
	/// radixSorter.Sort(myBibbleList, BibbleSortDelegate.Doobrie);
	/// </code>
	/// You should try to reuse RadixSort instances, since repeated allocation of the
	/// internal storage is then avoided.
	///
	/// </remarks>
	public class RadixSortSingle<TContainer, TContainerValueType>
		where TContainer : IList<TContainerValueType>
	{
		#region Delegates and Events

		public delegate System.Single TFunctor( TContainerValueType value );

		#endregion Delegates and Events

		#region Constants and Enumerations

		#endregion Constants and Enumerations

		#region Classes and Structures

		protected struct SortEntry
		{
			public System.Single Key;
			public TContainerValueType Value;

			public SortEntry( System.Single k, TContainerValueType v )
			{
				Key = k;
				Value = v;
			}
		}

		#endregion Classes and Structures

		#region Fields and Properties

		/// Alpha-pass counters of values (histogram)
		/// 4 of them so we can radix sort a maximum of a 32bit value
		protected int[ , ] _counters = new int[ 4,256 ];

		/// Beta-pass offsets
		protected int[] _offsets = new int[ 256 ];

		/// Sort area size
		protected int _sortSize = 0;

		/// Number of passes for this type
		protected int _passCount = 4;

		protected float mask = 0x000000FF;

		protected SortEntry[] _sortArea1, _sortArea2;
		protected SortEntry[] _src, _dest;

		#endregion Fields and Properties

		#region Construction and Destruction

		#endregion Construction and Destruction

		#region Methods

		/// <summary>
		///  Main sort function
		/// </summary>
		/// <param name="container">A container of the type you declared when declaring</param>
		/// <param name="valueFunction">
		/// A delegate which returns the value for comparison when given a container value
		/// </param>
		public void Sort( TContainer container, TFunctor valueFunction )
		{
			if ( container.Count == 0 )
			{
				return;
			}

			// Setup container areas
			_sortSize = container.Count;
			_sortArea1 = new SortEntry[ _sortSize ];
			_sortArea2 = new SortEntry[ _sortSize ];

			// Perform alpha pass to count
			var prevValue = valueFunction( container[ 0 ] );
			var needsSorting = false;
			var u = 0;
			foreach ( var item in container )
			{
				// get sort value
				var val = valueFunction( item );
				// cheap check to see if needs sorting (temporal coherence)
				if ( !needsSorting && ( ( (IComparable<System.Single>)val ).CompareTo( prevValue ) < 0 ) )
				{
					needsSorting = true;
				}

				// Create a sort entry
				SortEntry ne;
				ne.Value = item;
				ne.Key = val;
				_sortArea1[ u++ ] = ne;

				// increase counters
				for ( var p = 0; p < _passCount; ++p )
				{
					_counters[ p, _getByte( p, val ) ]++;
				}

				prevValue = val;
			}

			// early exit if already sorted
			if ( !needsSorting )
			{
				return;
			}

			// Sort passes
			_src = _sortArea1;
			_dest = _sortArea2;

			for ( var p = 0; p < _passCount - 1; ++p )
			{
				_sortPass( p );
				// flip src/dst
				var tmp = _src;
				_src = _dest;
				_dest = tmp;
			}

			// Final Pass
			finalPass( _passCount - 1, prevValue );

			// Copy everything back
			for ( var c = 0; c < _sortSize; c++ )
			{
				container[ c ] = _dest[ c ].Value;
			}
		}

		protected void finalPass( int byteIndex, System.Single value )
		{
			// floats need to be special cased since negative numbers will come
			// after positives (high bit = sign) and will be in reverse order
			// (no ones-complement of the +ve value)
			var negativeCount = 0;
			// all negative values are in entries 128+ in most significant byte
			for ( var i = 128; i < 256; ++i )
			{
				negativeCount += _counters[ byteIndex, i ];
			}
			// Calculate offsets - positive ones start at the number of negatives
			// do positive numbers normally
			// negative numbers also need to invert ordering
			// In order to preserve the stability of the sort (essential since
			// we rely on previous bytes already being sorted) we have to count
			// backwards in our offsets
			_offsets[ 0 ] = negativeCount;
			_offsets[ 255 ] = _counters[ byteIndex, 255 ];
			for ( var i = 1; i < 128; ++i )
			{
				_offsets[ i ] = _offsets[ i - 1 ] + _counters[ byteIndex, i - 1 ];
				_offsets[ 255 - i ] = _offsets[ 255 - i + 1 ] + _counters[ byteIndex, 255 - i ];
			}

			// Sort pass
			foreach ( var item in _src )
			{
				var byteVal = _getByte( byteIndex, item.Key );
				if ( byteVal > 127 )
				{
					// -ve; pre-decrement since offsets set to count
					_dest[ --_offsets[ byteVal ] ] = item;
				}
				else
				{
					// +ve
					_dest[ _offsets[ byteVal ]++ ] = item;
				}
			}
		}

		private void _sortPass( int byteIndex )
		{
			// Calculate offsets
			// Basically this just leaves gaps for duplicate entries to fill
			_offsets[ 0 ] = 0;
			for ( var i = 1; i < 256; ++i )
			{
				_offsets[ i ] = _offsets[ i - 1 ] + _counters[ byteIndex, i - 1 ];
			}

			// Sort pass
			foreach ( var item in _src )
			{
				_dest[ _offsets[ _getByte( byteIndex, item.Key ) ]++ ] = item;
			}
		}

		private byte _getByte( int byteIndex, System.Single val )
		{
#if !AXIOM_USE_SAFE_CODE
			return BitConverter.GetBytes( val )[ byteIndex ];
#else

#if !AXIOM_SAFE_ONLY
			unsafe
#endif
			{
				return ((byte*)&val)[ byteIndex ];
			}
#endif
		}

		#endregion Methods
	}

	/// <summary>
	/// Class for performing a radix sort (fast comparison-less sort based on
	/// byte value) on various standard containers.
	/// </summary>
	/// <remarks>
	/// A radix sort is a very fast sort algorithm. It only uses a single comparison
	/// and thus is able to break the theoretical minimum O(N*logN) complexity.
	/// Radix sort is complexity O(k*N), where k is a constant. Note that radix
	/// sorting is not in-place, it requires additional storage, so it trades
	/// memory for speed. The overhead of copying means that it is only faster
	/// for fairly large datasets, so you are advised to only use it for collections
	/// of at least a few thousand items.
	/// <para/>
	/// This is a generic class to allow it to deal with a variety of containers,
	/// and a variety of value types to sort on. In addition to providing the
	/// container and value type on construction, you also need to supply a
	/// functor object which will retrieve the value to compare on for each item
	/// in the list. For example, if you had an <see cref="IList{T}" /> of instances
	/// of an object of class 'Bibble', and you wanted to sort on
	/// Bibble.Doobrie, you'd have to create a delegate like this:
	/// <code>
	/// class BibbleSortDelegate
	/// {
	///		static int Doobrie( Bibble val)
	///		{
	///			return val.Doobrie;
	///		}
	///	}
	/// </code>
	/// Then, you need to declare a RadixSort class which names the container type,
	/// the value type in the container, and the type of the value you want to
	/// sort by. You can then call the sort function. E.g.
	/// <code>
	/// RadixSortInt32&lt;BibbleList, Bibble&gt; radixSorter;
	///
	/// radixSorter.Sort(myBibbleList, BibbleSortDelegate.Doobrie);
	/// </code>
	/// You should try to reuse RadixSort instances, since repeated allocation of the
	/// internal storage is then avoided.
	///
	/// </remarks>
	public class RadixSortInt32<TContainer, TContainerValueType>
		where TContainer : IList<TContainerValueType>
	{
		#region Delegates and Events

		public delegate System.Int32 TFunctor( TContainerValueType value );

		#endregion Delegates and Events

		#region Constants and Enumerations

		#endregion Constants and Enumerations

		#region Classes and Structures

		protected struct SortEntry
		{
			public System.Int32 Key;
			public TContainerValueType Value;

			public SortEntry( System.Int32 k, TContainerValueType v )
			{
				Key = k;
				Value = v;
			}
		}

		#endregion Classes and Structures

		#region Fields and Properties

		/// Alpha-pass counters of values (histogram)
		/// 4 of them so we can radix sort a maximum of a 32bit value
		protected int[ , ] _counters = new int[ 4,256 ];

		/// Beta-pass offsets
		protected int[] _offsets = new int[ 256 ];

		/// Sort area size
		protected int _sortSize = 0;

		/// Number of passes for this type
		protected int _passCount = 4;

		protected float mask = 0x000000FF;

		protected SortEntry[] _sortArea1, _sortArea2;
		protected SortEntry[] _src, _dest;

		#endregion Fields and Properties

		#region Construction and Destruction

		#endregion Construction and Destruction

		#region Methods

		/// <summary>
		///  Main sort function
		/// </summary>
		/// <param name="container">A container of the type you declared when declaring</param>
		/// <param name="valueFunction">
		/// A delegate which returns the value for comparison when given a container value
		/// </param>
		public void Sort( TContainer container, TFunctor valueFunction )
		{
			if ( container.Count == 0 )
			{
				return;
			}

			// Setup container areas
			_sortSize = container.Count;
			_sortArea1 = new SortEntry[ _sortSize ];
			_sortArea2 = new SortEntry[ _sortSize ];

			// Perform alpha pass to count
			var prevValue = valueFunction( container[ 0 ] );
			var needsSorting = false;
			var u = 0;
			foreach ( var item in container )
			{
				// get sort value
				var val = valueFunction( item );
				// cheap check to see if needs sorting (temporal coherence)
				if ( !needsSorting && ( ( (IComparable<System.Int32>)val ).CompareTo( prevValue ) < 0 ) )
				{
					needsSorting = true;
				}

				// Create a sort entry
				SortEntry ne;
				ne.Value = item;
				ne.Key = val;
				_sortArea1[ u++ ] = ne;

				// increase counters
				for ( var p = 0; p < _passCount; ++p )
				{
					_counters[ p, _getByte( p, val ) ]++;
				}

				prevValue = val;
			}

			// early exit if already sorted
			if ( !needsSorting )
			{
				return;
			}

			// Sort passes
			_src = _sortArea1;
			_dest = _sortArea2;

			for ( var p = 0; p < _passCount - 1; ++p )
			{
				_sortPass( p );
				// flip src/dst
				var tmp = _src;
				_src = _dest;
				_dest = tmp;
			}

			// Final pass

			finalPass( _passCount - 1, prevValue );

			// Copy everything back
			for ( var c = 0; c < _sortSize; c++ )
			{
				container[ c ] = _dest[ c ].Value;
			}
		}

		protected void finalPass( int byteIndex, System.Int32 value )
		{
			// floats need to be special cased since negative numbers will come
			// after positives (high bit = sign) and will be in reverse order
			// (no ones-complement of the +ve value)
			var negativeCount = 0;
			// all negative values are in entries 128+ in most significant byte
			for ( var i = 128; i < 256; ++i )
			{
				negativeCount += _counters[ byteIndex, i ];
			}
			// Calculate offsets - positive ones start at the number of negatives
			// do positive numbers normally
			// negative numbers also need to invert ordering
			// In order to preserve the stability of the sort (essential since
			// we rely on previous bytes already being sorted) we have to count
			// backwards in our offsets			_
			_offsets[ 0 ] = negativeCount;
			_offsets[ 255 ] = _counters[ byteIndex, 255 ];
			for ( var i = 1; i < 128; ++i )
			{
				_offsets[ i ] = _offsets[ i - 1 ] + _counters[ byteIndex, i - 1 ];
				_offsets[ 255 - i ] = _offsets[ 255 - i + 1 ] + _counters[ byteIndex, 255 - i ];
			}

			// Sort pass
			foreach ( var item in _src )
			{
				var byteVal = _getByte( byteIndex, item.Key );
				if ( byteVal > 127 )
				{
					// -ve; pre-decrement since offsets set to count
					_dest[ --_offsets[ byteVal ] ] = item;
				}
				else
				{
					// +ve
					_dest[ _offsets[ byteVal ]++ ] = item;
				}
			}
		}

		private void _sortPass( int byteIndex )
		{
			// Calculate offsets
			// Basically this just leaves gaps for duplicate entries to fill
			_offsets[ 0 ] = 0;
			for ( var i = 1; i < 256; ++i )
			{
				_offsets[ i ] = _offsets[ i - 1 ] + _counters[ byteIndex, i - 1 ];
			}

			// Sort pass
			foreach ( var item in _src )
			{
				_dest[ _offsets[ _getByte( byteIndex, item.Key ) ]++ ] = item;
			}
		}

		private byte _getByte( int byteIndex, System.Int32 val )
		{
#if !AXIOM_USE_SAFE_CODE
			return BitConverter.GetBytes( val )[ byteIndex ];
#else

#if !AXIOM_SAFE_ONLY
			unsafe
#endif
			{
				return ((byte*)&val)[ byteIndex ];
			}
#endif
		}

		#endregion Methods
	}

	/// <summary>
	/// Class for performing a radix sort (fast comparison-less sort based on
	/// byte value) on various standard containers.
	/// </summary>
	/// <remarks>
	/// A radix sort is a very fast sort algorithm. It only uses a single comparison
	/// and thus is able to break the theoretical minimum O(N*logN) complexity.
	/// Radix sort is complexity O(k*N), where k is a constant. Note that radix
	/// sorting is not in-place, it requires additional storage, so it trades
	/// memory for speed. The overhead of copying means that it is only faster
	/// for fairly large datasets, so you are advised to only use it for collections
	/// of at least a few thousand items.
	/// <para/>
	/// This is a generic class to allow it to deal with a variety of containers,
	/// and a variety of value types to sort on. In addition to providing the
	/// container and value type on construction, you also need to supply a
	/// functor object which will retrieve the value to compare on for each item
	/// in the list. For example, if you had an <see cref="IList{T}" /> of instances
	/// of an object of class 'Bibble', and you wanted to sort on
	/// Bibble.Doobrie, you'd have to create a delegate like this:
	/// <code>
	/// class BibbleSortDelegate
	/// {
	///		static uint Doobrie( Bibble val)
	///		{
	///			return val.Doobrie;
	///		}
	///	}
	/// </code>
	/// Then, you need to declare a RadixSort class which names the container type,
	/// the value type in the container, and the type of the value you want to
	/// sort by. You can then call the sort function. E.g.
	/// <code>
	/// RadixSortUInt32&lt;BibbleList, Bibble&gt; radixSorter;
	///
	/// radixSorter.Sort(myBibbleList, BibbleSortDelegate.Doobrie);
	/// </code>
	/// You should try to reuse RadixSort instances, since repeated allocation of the
	/// internal storage is then avoided.
	///
	/// </remarks>
	public class RadixSortUInt32<TContainer, TContainerValueType>
		where TContainer : IList<TContainerValueType>
	{
		#region Delegates and Events

		public delegate System.UInt32 TFunctor( TContainerValueType value );

		#endregion Delegates and Events

		#region Constants and Enumerations

		#endregion Constants and Enumerations

		#region Classes and Structures

		protected struct SortEntry
		{
			public System.UInt32 Key;
			public TContainerValueType Value;

			public SortEntry( System.UInt32 k, TContainerValueType v )
			{
				Key = k;
				Value = v;
			}
		}

		#endregion Classes and Structures

		#region Fields and Properties

		/// Alpha-pass counters of values (histogram)
		/// 4 of them so we can radix sort a maximum of a 32bit value
		protected int[ , ] _counters = new int[ 4,256 ];

		/// Beta-pass offsets
		protected int[] _offsets = new int[ 256 ];

		/// Sort area size
		protected int _sortSize = 0;

		/// Number of passes for this type
		protected int _passCount = 4;

		protected float mask = 0x000000FF;

		protected SortEntry[] _sortArea1, _sortArea2;
		protected SortEntry[] _src, _dest;

		#endregion Fields and Properties

		#region Construction and Destruction

		#endregion Construction and Destruction

		#region Methods

		/// <summary>
		///  Main sort function
		/// </summary>
		/// <param name="container">A container of the type you declared when declaring</param>
		/// <param name="valueFunction">
		/// A delegate which returns the value for comparison when given a container value
		/// </param>
		public void Sort( TContainer container, TFunctor valueFunction )
		{
			if ( container.Count == 0 )
			{
				return;
			}

			// Setup container areas
			_sortSize = container.Count;
			_sortArea1 = new SortEntry[ _sortSize ];
			_sortArea2 = new SortEntry[ _sortSize ];

			// Perform alpha pass to count
			var prevValue = valueFunction( container[ 0 ] );
			var needsSorting = false;
			var u = 0;
			foreach ( var item in container )
			{
				// get sort value
				var val = valueFunction( item );
				// cheap check to see if needs sorting (temporal coherence)
				if ( !needsSorting && ( ( (IComparable<System.UInt32>)val ).CompareTo( prevValue ) < 0 ) )
				{
					needsSorting = true;
				}

				// Create a sort entry
				SortEntry ne;
				ne.Value = item;
				ne.Key = val;
				_sortArea1[ u++ ] = ne;

				// increase counters
				for ( var p = 0; p < _passCount; ++p )
				{
					_counters[ p, _getByte( p, val ) ]++;
				}

				prevValue = val;
			}

			// early exit if already sorted
			if ( !needsSorting )
			{
				return;
			}

			// Sort passes
			_src = _sortArea1;
			_dest = _sortArea2;

			for ( var p = 0; p < _passCount - 1; ++p )
			{
				_sortPass( p );
				// flip src/dst
				var tmp = _src;
				_src = _dest;
				_dest = tmp;
			}

			// Final pass

			finalPass( _passCount - 1, prevValue );

			// Copy everything back
			for ( var c = 0; c < _sortSize; c++ )
			{
				container[ c ] = _dest[ c ].Value;
			}
		}

		protected void finalPass( int byteIndex, System.UInt32 value )
		{
			// floats need to be special cased since negative numbers will come
			// after positives (high bit = sign) and will be in reverse order
			// (no ones-complement of the +ve value)
			var negativeCount = 0;
			// all negative values are in entries 128+ in most significant byte
			for ( var i = 128; i < 256; ++i )
			{
				negativeCount += _counters[ byteIndex, i ];
			}
			// Calculate offsets - positive ones start at the number of negatives
			// do positive numbers normally
			// negative numbers also need to invert ordering
			// In order to preserve the stability of the sort (essential since
			// we rely on previous bytes already being sorted) we have to count
			// backwards in our offsets			_
			_offsets[ 0 ] = negativeCount;
			_offsets[ 255 ] = _counters[ byteIndex, 255 ];
			for ( var i = 1; i < 128; ++i )
			{
				_offsets[ i ] = _offsets[ i - 1 ] + _counters[ byteIndex, i - 1 ];
				_offsets[ 255 - i ] = _offsets[ 255 - i + 1 ] + _counters[ byteIndex, 255 - i ];
			}

			// Sort pass
			foreach ( var item in _src )
			{
				var byteVal = _getByte( byteIndex, item.Key );
				if ( byteVal > 127 )
				{
					// -ve; pre-decrement since offsets set to count
					_dest[ --_offsets[ byteVal ] ] = item;
				}
				else
				{
					// +ve
					_dest[ _offsets[ byteVal ]++ ] = item;
				}
			}
		}

		private void _sortPass( int byteIndex )
		{
			// Calculate offsets
			// Basically this just leaves gaps for duplicate entries to fill
			_offsets[ 0 ] = 0;
			for ( var i = 1; i < 256; ++i )
			{
				_offsets[ i ] = _offsets[ i - 1 ] + _counters[ byteIndex, i - 1 ];
			}

			// Sort pass
			foreach ( var item in _src )
			{
				_dest[ _offsets[ _getByte( byteIndex, item.Key ) ]++ ] = item;
			}
		}

		private byte _getByte( int byteIndex, System.UInt32 val )
		{
#if !AXIOM_USE_SAFE_CODE
			return BitConverter.GetBytes( val )[ byteIndex ];
#else

#if !AXIOM_SAFE_ONLY
			unsafe
#endif
			{
				return ((byte*)&val)[ byteIndex ];
			}
#endif
		}

		#endregion Methods
	}
}
