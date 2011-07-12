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

#endregion

#region SVN Version Information
// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id:$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;

#endregion Namespace Declarations

namespace Axiom.Core
{
    public interface IBitConverter
    {
        Array Convert(Array buffer, int startIndex);
    }

    public abstract class MemoryBuffer : DisposableObject, IMemoryBuffer
    {
        #region Implementation of IMemoryBuffer

        /// <summary>
        /// Copies data into an array.
        /// </summary>
        /// <typeparam name="T">The type of the element</typeparam>
        /// <param name="data">The array to receive  data.</param>
        public virtual void GetData<T>(T[] data) where T : struct
        {
            GetData(data, 0, data.Length);
        }

        /// <summary>
        /// Copies data into an array.
        /// </summary>
        /// <typeparam name="T">The type of the element</typeparam>
        /// <param name="data">The array to receive  data.</param>
        /// <param name="startIndex">The index of the first element in the array to start from.</param>
        /// <param name="elementCount">The number of elements to copy.</param>
        public virtual void GetData<T>(T[] data, int startIndex, int elementCount) where T : struct
        {
            GetData(0, data, startIndex, elementCount);
        }

        /// <summary>
        /// Copies data into an array.
        /// </summary>
        /// <param name="offset">The index of the first element in the buffer to retrieve</param>
        /// <typeparam name="T">The type of the element</typeparam>
        /// <param name="data">The array to receive  data.</param>
        /// <param name="startIndex">The index of the first element in the array to start from.</param>
        /// <param name="elementCount">The number of elements to copy.</param>
        public abstract void GetData<T>(int offset, T[] data, int startIndex, int elementCount) where T : struct;

        /// <summary>
        ///  Sets data.
        /// </summary>
        /// <typeparam name="T">The type of the element</typeparam>
        /// <param name="data">The array of data.</param>
        public virtual void SetData<T>(T[] data) where T : struct
        {
            SetData(0, data, 0, data.Length);
        }

        /// <summary>
        /// Sets data.
        /// </summary>
        /// <typeparam name="T">The type of the element</typeparam>
        /// <param name="data">The array of data.</param>
        /// <param name="startIndex">The index of the first element in the array to start from.</param>
        /// <param name="elementCount">The number of elements to copy.</param>
        public virtual void SetData<T>(T[] data, int startIndex, int elementCount) where T : struct
        {
            SetData(0, data, startIndex, elementCount);
        }

        /// <summary>
        /// Sets data.
        /// </summary>
        /// <param name="offset">The index of the first element in the buffer to write to</param>
        /// <typeparam name="T">The type of the element</typeparam>
        /// <param name="data">The array of data.</param>
        /// <param name="startIndex">The index of the first element in the array to start from.</param>
        /// <param name="elementCount">The number of elements to copy.</param>
        public abstract void SetData<T>(int offset, T[] data, int startIndex, int elementCount) where T : struct;

        #endregion
    }

    public class SafeMemoryBuffer<T> : MemoryBuffer
        where T : struct
    {
        private T[] _buffer;

        public MemoryManager Owner { get; private set; }

        public T this[int index]
        {
            get
            {
                return _buffer[index];
            }

            set
            {
                _buffer[index] = value;
            }
        }

        internal SafeMemoryBuffer(MemoryManager owner)
        {
            this.Owner = owner;
        }

        internal SafeMemoryBuffer(MemoryManager owner, T[] data) :
            this(owner)
        {
            _buffer = data;
        }

        internal SafeMemoryBuffer(MemoryManager owner, long size) :
            this(owner)
        {
            _buffer = new T[size];
        }

        public ToType[] AsArray<ToType>()
        {
            if (Owner.BitConverters.ContainsKey(typeof(T)))
            {
                var converters = Owner.BitConverters[typeof(T)];
                if (converters.ContainsKey(typeof(ToType)))
                    return (ToType[])(converters[typeof(ToType)].Convert(_buffer, 0));
            }
            return new ToType[0];
        }

        #region IDisposable Members

        /// <summary>
        /// Copies data into an array.
        /// </summary>
        /// <typeparam name="TDest">The type of the element</typeparam>
        /// <param name="data">The array to receive  data.</param>
        /// <param name="startIndex">The index of the first element in the array to start from.</param>
        /// <param name="elementCount">The number of elements to copy.</param>
        public override void GetData<TDest>(int offset, TDest[] data, int startIndex, int elementCount)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Sets data.
        /// </summary>
        /// <typeparam name="TSource">The type of the element</typeparam>
        /// <param name="data">The array of data.</param>
        /// <param name="startIndex">The index of the first element in the array to start from.</param>
        /// <param name="elementCount">The number of elements to copy.</param>
        public override void SetData<TSource>(int offset, TSource[] data, int startIndex, int elementCount)
        {
            throw new NotImplementedException();
        }

        #endregion IDisposable Members
    }

#if !AXIOM_SAFE_ONLY
    public class UnsafeMemoryBuffer : MemoryBuffer
    {
        private IntPtr _buffer;

        public MemoryManager Owner { get; private set; }

        public byte this[int index]
        {
            get
            {
                unsafe
                {
                    return ((byte*)_buffer.ToPointer())[index];
                }
            }

            set
            {
                unsafe
                {
                    ((byte*)_buffer.ToPointer())[index] = value;
                }
            }
        }

        internal UnsafeMemoryBuffer(MemoryManager owner, IntPtr data)
        {
            this.Owner = owner;
            _buffer = data;
        }

        /// <summary>
        /// Copies data into an array.
        /// </summary>
        /// <typeparam name="TDest">The type of the element</typeparam>
        /// <param name="data">The array to receive  data.</param>
        /// <param name="startIndex">The index of the first element in the array to start from.</param>
        /// <param name="elementCount">The number of elements to copy.</param>
        public override void GetData<TDest>(int offset, TDest[] data, int startIndex, int elementCount)
        {
            IntPtr pin = MemoryManager.PinMemory(data);
            MemoryManager.Copy(_buffer, pin, offset * MemoryManager.SizeOf(typeof(TDest)), startIndex * MemoryManager.SizeOf(typeof(TDest)), elementCount);
            MemoryManager.UnpinMemory(data);
        }

        /// <summary>
        /// Sets data.
        /// </summary>
        /// <typeparam name="TSource">The type of the element</typeparam>
        /// <param name="data">The array of data.</param>
        /// <param name="startIndex">The index of the first element in the array to start from.</param>
        /// <param name="elementCount">The number of elements to copy.</param>
        public override void SetData<TSource>(int offset, TSource[] data, int startIndex, int elementCount)
        {
            IntPtr pin = MemoryManager.PinMemory(data);
            MemoryManager.Copy(pin, _buffer, startIndex * MemoryManager.SizeOf(typeof(TSource)), offset * MemoryManager.SizeOf(typeof(TSource)), elementCount);
            MemoryManager.UnpinMemory(data);
        }
    }
#endif
}