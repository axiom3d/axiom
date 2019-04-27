using System;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Utilities;

namespace Axiom.RenderSystems.OpenGL
{
    public class GLDefaultHardwareIndexBuffer : HardwareIndexBuffer
    {
        protected byte[] _data;
        protected BufferBase _dataPtr;

        /// <summary>
        /// </summary>
        /// <param name="idxType"> </param>
        /// <param name="numIndexes"> </param>
        /// <param name="usage"> </param>
        public GLDefaultHardwareIndexBuffer(IndexType idxType, int numIndexes, BufferUsage usage)
            : base(null, idxType, numIndexes, usage, true, false) // always software, never shadowed
        {
            this._data = new byte[sizeInBytes];
            this._dataPtr = BufferBase.Wrap(this._data);
        }

        /// <summary>
        /// </summary>
        /// <param name="disposeManagedResources"> </param>
        protected override void dispose(bool disposeManagedResources)
        {
            if (!IsDisposed)
            {
                if (disposeManagedResources)
                {
                    if (this._data != null)
                    {
                        this._dataPtr.SafeDispose();
                        this._data = null;
                    }
                }
            }

            // If it is available, make the call to the
            // base class's Dispose(Boolean) method
            base.dispose(disposeManagedResources);
        }

        /// <summary>
        /// </summary>
        /// <param name="offset"> </param>
        /// <param name="length"> </param>
        /// <param name="locking"> </param>
        /// <returns> </returns>
        protected override BufferBase LockImpl(int offset, int length, BufferLocking locking)
        {
            // Only for use internally, no 'locking' as such
            return this._dataPtr + offset;
        }

        /// <summary>
        /// </summary>
        protected override void UnlockImpl()
        {
            // Nothing to do
        }

        /// <summary>
        /// </summary>
        /// <param name="offset"> </param>
        /// <param name="length"> </param>
        /// <param name="locking"> </param>
        /// <returns> </returns>
        public override BufferBase Lock(int offset, int length, BufferLocking locking)
        {
            isLocked = true;
            return this._dataPtr + offset;
        }

        /// <summary>
        /// </summary>
        public override void Unlock()
        {
            isLocked = false;
            // Nothing to do
        }

        /// <summary>
        /// </summary>
        /// <param name="offset"> </param>
        /// <param name="length"> </param>
        /// <param name="dest"> </param>
        public override void ReadData(int offset, int length, BufferBase dest)
        {
            Contract.Requires((offset + length) <= sizeInBytes);

            Memory.Copy(this._dataPtr + offset, dest, length);
        }

        /// <summary>
        /// </summary>
        /// <param name="offset"> </param>
        /// <param name="length"> </param>
        /// <param name="src"> </param>
        /// <param name="discardWholeBuffer"> </param>
        public override void WriteData(int offset, int length, BufferBase src, bool discardWholeBuffer)
        {
            Contract.Requires((offset + length) <= sizeInBytes);
            // ignore discard, memory is not guaranteed to be zeroised
            Memory.Copy(src, this._dataPtr + offset, length);
        }

        public IntPtr DataPtr(int offset)
        {
            return new IntPtr((this._dataPtr + offset).Ptr);
        }
    }
}