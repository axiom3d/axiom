#region Namespace Declarations

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Axiom.Core;
using Axiom.CrossPlatform;
using Axiom.Utilities;

#endregion Namespace Declarations

namespace Axiom.Graphics
{
	public partial class GpuProgramParameters
	{
		#region Nested type: FloatConstantList

		/// <summary>
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public class FloatConstantList : OffsetArray<float>
		{
			public FloatConstantList() { }

			public FloatConstantList( FloatConstantList other )
			{
				Data = (float[])other.Data.Clone();
			}

			public override void Resize( int size )
			{
				Contract.Requires( size > Count );
				AddRange( Enumerable.Repeat( 0.0f, size - Count ) );
			}
		};

		#endregion

		#region Nested type: IntConstantList

		/// <summary>
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public class IntConstantList : OffsetArray<int>
		{
			public IntConstantList() { }

			public IntConstantList( IntConstantList other )
			{
				Data = (int[])other.Data.Clone();
			}

			public override void Resize( int size )
			{
				Contract.Requires( size > Count );
				AddRange( Enumerable.Repeat( 0, size - Count ) );
			}
		};

		#endregion

		#region Nested type: OffsetArray

		/// <summary>
		/// This class emulates the behaviour of a vector&lt;T&gt;
		/// allowing T* access as IntPtr of a specified element
		/// </summary>
		[AxiomHelper( 0, 9 )]
		public abstract class OffsetArray<T> : DisposableObject, IList<T>
		{
			#region Fields

			private readonly int _size = Memory.SizeOf( typeof( T ) );
			private FixedPointer _ptr;

			#endregion Fields

			#region Properties

			public T[] Data { get; protected set; }

			public int Capacity
			{
				get
				{
					return Data.Length;
				}
			}

			public int Count { get; private set; }

			public bool IsReadOnly
			{
				get
				{
					return false;
				}
			}

			public T this[ int index ]
			{
				get
				{
					return Data[ index ];
				}

				set
				{
					Data[ index ] = value;
				}
			}

			#endregion Properties

			#region Nested types

			public struct FixedPointer : IDisposable
			{
				internal T[] Owner;
				public BufferBase Pointer;

				#region IDisposable Members

				public void Dispose()
				{
					Memory.UnpinObject( this.Owner );
				}

				#endregion
			};

			#endregion Nested types

			public OffsetArray()
			{
				Data = new T[ 16 ];
			}

			#region IList<T> Members

			public IEnumerator<T> GetEnumerator()
			{
				for ( int i = 0; i < Count; i++ )
				{
					yield return Data[ i ];
				}
			}

			#endregion

			protected override void dispose( bool disposeManagedResources )
			{
				if ( !IsDisposed && disposeManagedResources )
				{
					this._ptr.SafeDispose();
				}

				base.dispose( disposeManagedResources );
			}

			public FixedPointer Fix( int offset )
			{
				this._ptr.Owner = Data;
				this._ptr.Pointer = Memory.PinObject( this._ptr.Owner ).Offset( this._size * offset );
				return this._ptr;
			}

			private void _grow()
			{
				T[] tmp = Data;
				Array.Resize( ref tmp, Capacity + 16 );
				Data = tmp;
			}

			public void AddRange( IEnumerable<T> entries )
			{
				foreach ( T v in entries )
				{
					Add( v );
				}
			}

			public void RemoveRange( int index, int count )
			{
				int behind = Count - index - 1; // number of elements behind the to be moved region
				int next = index + count; // index of the first element behind
				int todo = behind < count ? behind : count; // number of elements to shift down

				for ( int i = 0; i < todo; i++ )
				{
					Data[ index + i ] = Data[ next + i ];
				}

				Count -= count;
			}

			public abstract void Resize( int size );

			#region IList<T> implementation

			IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}

			public void Add( T item )
			{
				if ( Count == Capacity )
				{
					_grow();
				}
				Data[ Count++ ] = item;
			}

			public void Clear()
			{
				Count = 0;
			}

			public void CopyTo( T[] array, int arrayIndex )
			{
				for ( int i = 0; i < Count; i++ )
				{
					array[ arrayIndex + i ] = Data[ i ];
				}
			}

			public void RemoveAt( int index )
			{
				RemoveRange( index, 1 );
			}

			#region unimplemented operations

			public bool Contains( T item )
			{
				throw new NotImplementedException();
			}

			public bool Remove( T item )
			{
				throw new NotImplementedException();
			}

			public int IndexOf( T item )
			{
				throw new NotImplementedException();
			}

			public void Insert( int index, T item )
			{
				throw new NotImplementedException();
			}

			#endregion

			#endregion IList<T> implementation
		};

		#endregion
	}
}
