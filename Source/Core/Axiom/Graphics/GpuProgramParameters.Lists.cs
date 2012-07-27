#region Namespace Declarations

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Axiom.Core;
using Axiom.Utilities;

#endregion Namespace Declarations

namespace Axiom.Graphics
{
	public partial class GpuProgramParameters
	{
		/// <summary>
		/// This class emulates the behaviour of a vector&lt;T&gt;
		/// allowing T* access as IntPtr of a specified element
		/// </summary>
		[AxiomHelper( 0, 9 )]
		public abstract class OffsetArray<T> : DisposableObject, IList<T>
		{
			#region Fields

			private FixedPointer _ptr;
			private readonly int _size = Memory.SizeOf( typeof ( T ) );

			#endregion Fields

			#region Properties

			public T[] Data { get; protected set; }

			public int Count { get; private set; }

			public int Capacity
			{
				get
				{
					return Data.Length;
				}
			}

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
				public BufferBase Pointer;
				internal T[] Owner;

				public void Dispose()
				{
					Memory.UnpinObject( this.Owner );
				}
			};

			#endregion Nested types

			public OffsetArray()
				: base()
			{
				Data = new T[16];
			}

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

			public IEnumerator<T> GetEnumerator()
			{
				for ( var i = 0; i < Count; i++ )
				{
					yield return Data[ i ];
				}
			}

			private void _grow()
			{
				var tmp = Data;
				Array.Resize( ref tmp, Capacity + 16 );
				Data = tmp;
			}

			public void AddRange( IEnumerable<T> entries )
			{
				foreach ( var v in entries )
				{
					Add( v );
				}
			}

			public void RemoveRange( int index, int count )
			{
				var behind = Count - index - 1; // number of elements behind the to be moved region
				var next = index + count; // index of the first element behind
				var todo = behind < count ? behind : count; // number of elements to shift down

				for ( var i = 0; i < todo; i++ )
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
				for ( var i = 0; i < Count; i++ )
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

		/// <summary>
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public class FloatConstantList : OffsetArray<float>
		{
			public FloatConstantList()
				: base()
			{
			}

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

		/// <summary>
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public class IntConstantList : OffsetArray<int>
		{
			public IntConstantList()
				: base()
			{
			}

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
	}
}