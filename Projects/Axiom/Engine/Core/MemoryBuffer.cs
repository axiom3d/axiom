using System;
using System.Collections.Generic;

namespace Axiom.Core
{
    public interface IMemoryBuffer : IDisposable
    {
    }

    public interface IBitConverter
    {
        Array Convert(Array buffer, int startIndex);
    }

    public class MemoryManager : Singleton<MemoryManager>
    {
        private List<IMemoryBuffer> _memoryPool = new List<IMemoryBuffer>();
        private static Dictionary<Type, IBitConverter> _bitConverters;
        public Dictionary<Type, IBitConverter> BitConverters { get { return _bitConverters; } }

        static MemoryManager()
        {
            _bitConverters = new Dictionary<Type, IBitConverter>()
                                 {
                                     {typeof (int), new IntBitConverter()},
                                     {typeof (float), new SingleBitConverter()}
                                 };
        }

        public MemoryBuffer<T> Allocate<T>(long size)
            where T : struct
        {
            MemoryBuffer<T> buffer = new MemoryBuffer<T>(this, size);
            this._memoryPool.Add(buffer);
            return buffer;
        }

        public void Deallocate(IMemoryBuffer buffer)
        {
            if (_memoryPool.Contains(buffer))
            {
                _memoryPool.Remove(buffer);
                buffer.Dispose();
            }
        }

        protected override void dispose(bool disposeManagedResources)
        {
            base.dispose(disposeManagedResources);
        }

        private class IntBitConverter : IBitConverter
        {
            public Array Convert(Array buffer, int startIndex)
            {
                int[] retVal;
                int size = buffer.Length / 4;
                retVal = new int[size];
                for (int index = startIndex; index < size; index++, startIndex += 4)
                {
                    retVal[index] = BitConverter.ToInt32((byte[])buffer, startIndex);
                }
                return retVal;
            }
        }

        private class SingleBitConverter : IBitConverter
        {
            public Array Convert(Array buffer, int startIndex)
            {
                float[] retVal;
                int size = buffer.Length / 4;
                retVal = new float[size];
                for (int index = startIndex; index < size; index++, startIndex += 4)
                {
                    retVal[index] = BitConverter.ToInt32((byte[])buffer, startIndex);
                }
                return retVal;
            }
        }
    }

    public class MemoryBuffer<T> : IMemoryBuffer
        where T : struct
    {
        private T[] _buffer;

        public MemoryManager Owner { get; private set; }

        /// <summary>
        /// Provides the stacktrace when this buffer was created
        /// </summary>
        public string StackTrace { get; set; }

        public T this[long index]
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

        internal MemoryBuffer(MemoryManager owner)
        {
            IsDisposed = false;
            this.Owner = owner;
            this.StackTrace = Environment.StackTrace;
        }

        internal MemoryBuffer(MemoryManager owner, long size) :
            this(owner)
        {
            _buffer = new T[size];
        }

        public T[] AsArray<T>()
        {
            if (Owner.BitConverters.ContainsKey(typeof(T)))
                return (T[])(Owner.BitConverters[typeof(T)].Convert(_buffer, 0));
            return new T[0];
        }

        #region IDisposable Implementation

        #region IsDisposed Property

        /// <summary>
        /// Determines if this instance has been disposed of already.
        /// </summary>
        protected bool IsDisposed { get; set; }

        #endregion IsDisposed Property

        /// <summary>
        /// Class level dispose method
        /// </summary>
        /// <remarks>
        /// When implementing this method in an inherited class the following template should be used;
        /// protected override void dispose( bool disposeManagedResources )
        /// {
        /// 	if ( !isDisposed )
        /// 	{
        /// 		if ( disposeManagedResources )
        /// 		{
        /// 			// Dispose managed resources.
        /// 		}
        ///
        /// 		// There are no unmanaged resources to release, but
        /// 		// if we add them, they need to be released here.
        /// 	}
        ///
        /// 	// If it is available, make the call to the
        /// 	// base class's Dispose(Boolean) method
        /// 	base.dispose( disposeManagedResources );
        /// }
        /// </remarks>
        /// <param name="disposeManagedResources">True if Unmanaged resources should be released.</param>
        protected virtual void dispose(bool disposeManagedResources)
        {
            if (!IsDisposed)
            {
                if (disposeManagedResources)
                {
                }

                // There are no unmanaged resources to release, but
                // if we add them, they need to be released here.
            }
            IsDisposed = true;
        }

        public void Dispose()
        {
            dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable Implementation
    }
}