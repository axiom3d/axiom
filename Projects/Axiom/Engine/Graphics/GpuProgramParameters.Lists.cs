using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Axiom.Core;
using Axiom.CrossPlatform;

namespace Axiom.Graphics
{
    public partial class GpuProgramParameters
    {
        /// <summary>
        /// This class emulates the behaviour of a vector&lt;T&gt;
        /// allowing T* access as IntPtr of a specified element
        /// </summary>
        /// <typeparam name="T"></typeparam>
        [AxiomHelper(0, 8)]
        public class OffsetArray<T>: IList<T>
        {
            public T[] Data { get; protected set; } 

            public struct FixedPointer : IDisposable
            {
                public BufferBase Pointer;
                internal T[] Owner;

                public void Dispose()
                {
                    Memory.UnpinObject(Owner);
                }
            }

            private FixedPointer _ptr;

            private readonly int _size = Memory.SizeOf(typeof(T));

            public FixedPointer Fix(int offset)
            {
                _ptr.Owner = Data;
                _ptr.Pointer = Memory.PinObject(_ptr.Owner).Offset(_size * offset);
                return _ptr;
            }

            public IEnumerator<T> GetEnumerator()
            {
                for (var i = 0; i < Count; i++ )
                    yield return Data[i];
            }

            public OffsetArray()
            {
                Data = new T[16];
            }

            public int Capacity
            {
                get
                {
                    return Data.Length;
                }
            }

            private void Grow()
            {
                var tmp = Data;
                Array.Resize( ref tmp, Capacity + 16 );
                Data = tmp;
            }

            public void AddRange(IEnumerable<T> floatEntries)
            {
                foreach (var v in floatEntries)
                    Add( v );
            }

            public void RemoveRange(int index, int count)
            {
                var behind = Count - index - 1; // number of elements behind the to be moved region
                var next = index + count;       // index of the first element behind
                var todo = behind < count ? behind : count; // number of elements to shift down

                for (var i = 0; i < todo; i++)
                    Data[ index + i ] = Data[ next + i ];
                
                Count -= count;
            }

            #region IList<T> implementation

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public void Add( T item )
            {
                if (Count == Capacity)
                    Grow();
                Data[ Count++ ] = item;
            }

            public void Clear()
            {
                Count = 0;
            }

            public void CopyTo( T[] array, int arrayIndex )
            {
                for (var i = 0; i < Count; i++)
                    array[ arrayIndex + i ] = Data[ i ];
            }

            public int Count 
            { 
                get;
                private set;
            }

            public bool IsReadOnly
            {
                get
                {
                    return false;
                }
            }

            public void RemoveAt( int index )
            {
                RemoveRange( index, 1 );
            }

            public T this[int index]
            {
                get
                {
                    return Data[index];
                }
                set
                {
                    Data[index] = value;
                }
            }


            #region unimplemented operations

            public bool Contains(T item)
            {
                throw new NotImplementedException();
            }

            public bool Remove(T item)
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
           

            #endregion
        }

        /// <summary>
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        public class FloatConstantList : OffsetArray<float>
        {
            public void Resize( int size )
            {
                while ( Count < size )
                {
                    Add( 0.0f );
                }
            }

            public FloatConstantList()
            {
            }

            public FloatConstantList(FloatConstantList other)
            {
                Data = (float[])other.Data.Clone();
            }
        }

        /// <summary>
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        public class IntConstantList : OffsetArray<int>
        {
            public void Resize( int size )
            {
                while ( Count < size )
                {
                    Add( 0 );
                }
            }

            public IntConstantList()
            {
            }

            public IntConstantList(IntConstantList other)
            {
                Data = (int[])other.Data.Clone();
            }
        }
    }
}
