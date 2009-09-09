using System;
using System.Collections.Generic;
using System.Text;
using Axiom.Core;
namespace Axiom.Graphics
{
    public class DefaultHardwareVertexBuffer : HardwareVertexBuffer
    {
        byte[] mpData = null;
        public DefaultHardwareVertexBuffer(int vertexSize, int numVertices, BufferUsage usage)
            : base(vertexSize, numVertices, usage, true, false)// always software, never shadowed
        {
            mpData = new byte[base.sizeInBytes];
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <param name="dest"></param>
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
        /// <summary>
        /// 
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <param name="data"></param>
        /// <param name="discardWholeBuffer"></param>
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
        /// <summary>
        /// 
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <param name="src"></param>
        /// <param name="discardWholeBuffer"></param>
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
        /// <summary>
        /// 
        /// </summary>
        public override void Unlock()
        {
            base.isLocked = false;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <param name="locking"></param>
        /// <returns></returns>
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
        /// <summary>
        /// 
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <param name="locking"></param>
        /// <returns></returns>
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
        /// <summary>
        /// 
        /// </summary>
        protected override void UnlockImpl()
        {
            // nothing to do
        }
    }
}
