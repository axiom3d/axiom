using System;
using System.Collections.Generic;
using System.Text;
using Axiom.Core;
namespace Axiom.Graphics
{
    public class DefaultHardwareIndexBuffer : HardwareIndexBuffer
    {
        protected byte[] mpData;

        public DefaultHardwareIndexBuffer(IndexType idxType, int numIndexes, BufferUsage usage)
            : base(idxType, numIndexes, usage, true, false)
        {
            mpData = new byte[base.sizeInBytes];
        }
        protected override IntPtr LockImpl(int offset, int length, BufferLocking locking)
        {
            IntPtr ret = Memory.PinObject(mpData);
            unsafe
            {
                fixed (byte* pdataF = mpData)
                {
                    byte* pData = pdataF + offset;
                }
            }
            Memory.UnpinObject(mpData);
            return ret;
        }
        public override void Unlock()
        {
            base.isLocked = false;
        }
        public override void ReadData(int offset, int length, IntPtr dest)
        {
            unsafe
            {
                fixed (byte* pdataF = mpData)
                {
                    byte* pData = pdataF + offset;
                }
            }
            IntPtr data = Memory.PinObject(mpData);
            Memory.Copy(dest, data, length);
            Memory.UnpinObject(mpData);
        }
        public override void WriteData(int offset, int length, Array data, bool discardWholeBuffer)
        {
            IntPtr pSource = Memory.PinObject(data);
            unsafe
            {
                fixed (byte* pdataF = mpData)
                {
                    byte* pData = pdataF + offset;
                }
            }
            IntPtr pIntData = Memory.PinObject(mpData);
            Memory.Copy(pSource, pIntData, length);
            Memory.UnpinObject(data);
            Memory.UnpinObject(mpData);
        }
        public override void WriteData(int offset, int length, IntPtr src, bool discardWholeBuffer)
        {
            unsafe
            {
                fixed (byte* pdataF = mpData)
                {
                    byte* pData = pdataF + offset;
                }
            }
            IntPtr pIntData = Memory.PinObject(mpData);
            Memory.Copy(src, pIntData, length);
            Memory.UnpinObject(mpData);
        }
        public override IntPtr Lock(int offset, int length, BufferLocking locking)
        {
            base.isLocked = true;
            IntPtr ret = Memory.PinObject(mpData);
            unsafe
            {
                fixed (byte* pdataF = mpData)
                {
                    byte* pData = pdataF + offset;
                }
            }
            Memory.UnpinObject(mpData);
            return ret;
        }
        protected override void UnlockImpl()
        {
            //nothing to do
        }
    }
}
