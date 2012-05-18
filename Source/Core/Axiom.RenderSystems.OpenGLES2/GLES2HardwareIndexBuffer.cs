#region MIT/X11 License

//Copyright © 2003-2012 Axiom 3D Rendering Engine Project
//
//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//THE SOFTWARE.

#endregion License

#region SVN Version Information

// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Axiom.Core;
using Axiom.Graphics;

using OpenGL = OpenTK.Graphics.ES20.GL;
using OpenGLOES = OpenTK.Graphics.ES20.GL.Oes;
using OpenTK.Graphics.ES20;

#endregion Namespace Declarations
			
namespace Axiom.RenderSystems.OpenGLES2
{
    internal class GLES2HardwareIndexBuffer : HardwareIndexBuffer
    {
        private const int MapBufferThreshold = 1024 * 32;
        private int _bufferId = 0;
        private BufferBase _scratchPtr;
        private bool _lockedToScratch;
        private bool _scratchUploadOnUnlock;
        private int _scratchOffset;
        private int _scratchSize;

        public int BufferID
        {
            get { return this._bufferId; }
        }

        public GLES2HardwareIndexBuffer(HardwareBufferManagerBase manager, IndexType type, int numIndices, BufferUsage usage, bool useShadowBuffer)
            : base(manager, type, numIndices, usage, false, useShadowBuffer)
        {
            if (type == IndexType.Size32)
            {
                throw new AxiomException("32 bit hardware buffers are not allowed in OpenGL ES.");
            }

            if (!useShadowBuffer)
            {
                throw new AxiomException("Only support with shadowBuffer");
            }

            OpenGL.GenBuffers(1, ref this._bufferId);
            GLES2Config.GlCheckError(this);
            if (this._bufferId == 0)
            {
                throw new AxiomException("Cannot create GL index buffer");
            }

            OpenGL.BindBuffer(All.ElementArrayBuffer, this._bufferId);
            GLES2Config.GlCheckError(this);
            OpenGL.BufferData(All.ElementArrayBuffer, new IntPtr(sizeInBytes), IntPtr.Zero, GLES2HardwareBufferManager.GetGLUsage(usage));
            GLES2Config.GlCheckError(this);
        }

        /// <summary>
        /// </summary>
        protected override void UnlockImpl()
        {
            if (this._lockedToScratch)
            {
                if (this._scratchUploadOnUnlock)
                {
                    // have to write the data back to vertex buffer
                    WriteData(this._scratchOffset, this._scratchSize, this._scratchPtr, this._scratchOffset == 0 && this._scratchSize == sizeInBytes);
                }
                // deallocate from scratch buffer
                ((GLES2HardwareBufferManager)HardwareBufferManager.Instance).DeallocateScratch(this._scratchPtr);
                this._lockedToScratch = false;
            }
            else
            {
                OpenGL.BindBuffer(All.ElementArrayBuffer, this._bufferId);
                if (!OpenGLOES.UnmapBuffer(All.ElementArrayBuffer))
                {
                    throw new AxiomException("Buffer data corrupted, please reload");
                }
            }
            isLocked = false;
        }

        /// <summary>
        /// </summary>
        /// <param name="offset"> </param>
        /// <param name="length"> </param>
        /// <param name="locking"> </param>
        /// <returns> </returns>
        protected override BufferBase LockImpl(int offset, int length, BufferLocking locking)
        {
            All access = 0;
            if (isLocked)
            {
                throw new AxiomException("Invalid attempt to lock an index buffer that has already been locked");
            }

            BufferBase retPtr = null;
            if (length < MapBufferThreshold)
            {
                retPtr = ((GLES2HardwareBufferManager)HardwareBufferManager.Instance).AllocateScratch(length);
                if (retPtr != null)
                {
                    this._lockedToScratch = true;
                    this._scratchOffset = offset;
                    this._scratchSize = length;
                    this._scratchPtr = retPtr;
                    this._scratchUploadOnUnlock = (locking != BufferLocking.ReadOnly);

                    if (locking != BufferLocking.Discard)
                    {
                        ReadData(offset, length, retPtr);
                    }
                }
            }
            else
            {
                throw new AxiomException("Invalid Buffer lockSize");
            }

            if (retPtr == null)
            {
                OpenGL.BindBuffer(All.ElementArrayBuffer, this._bufferId);
                // Use glMapBuffer
                if (locking == BufferLocking.Discard)
                {
                    OpenGL.BufferData(All.ElementArrayBuffer, new IntPtr(sizeInBytes), IntPtr.Zero, GLES2HardwareBufferManager.GetGLUsage(usage));
                }
                if ((usage & BufferUsage.WriteOnly) != 0)
                {
                    access = All.WriteOnlyOes;
                }

                IntPtr pBuffer = OpenGLOES.MapBuffer(All.ElementArrayBuffer, access);
                if (pBuffer == IntPtr.Zero)
                {
                    throw new AxiomException("Index Buffer: Out of memory");
                }
                unsafe
                {
                    // return offset
                    retPtr = BufferBase.Wrap(pBuffer);
                }

                this._lockedToScratch = false;
            }
            isLocked = true;

            return retPtr;
        }

        /// <summary>
        /// </summary>
        /// <param name="offset"> </param>
        /// <param name="length"> </param>
        /// <param name="dest"> </param>
        public override void ReadData(int offset, int length, BufferBase dest)
        {
            if (useShadowBuffer)
            {
                var srcData = shadowBuffer.Lock(offset, length, BufferLocking.ReadOnly);
                Memory.Copy(srcData, dest, length);
                shadowBuffer.Unlock();
            }
            else
            {
                throw new AxiomException("Reading hardware buffer is not supported.");
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="offset"> </param>
        /// <param name="length"> </param>
        /// <param name="src"> </param>
        /// <param name="discardWholeBuffer"> </param>
        public override void WriteData(int offset, int length, BufferBase src, bool discardWholeBuffer)
        {
            OpenGL.BindBuffer(All.ElementArrayBuffer, this._bufferId);
            GLES2Config.GlCheckError(this);
            // Update the shadow buffer
            if (useShadowBuffer)
            {
                var destData = shadowBuffer.Lock(offset, length, discardWholeBuffer ? BufferLocking.Discard : BufferLocking.Normal);
                Memory.Copy(src, destData, length);
                shadowBuffer.Unlock();
            }

            var srcPtr = src.Ptr;
            if (offset == 0 && length == sizeInBytes)
            {
                OpenGL.BufferData(All.ElementArrayBuffer, new IntPtr(sizeInBytes), ref srcPtr, GLES2HardwareBufferManager.GetGLUsage(usage));
                GLES2Config.GlCheckError(this);
            }
            else
            {
                if (discardWholeBuffer)
                {
                    OpenGL.BufferData(All.ElementArrayBuffer, new IntPtr(sizeInBytes), IntPtr.Zero, GLES2HardwareBufferManager.GetGLUsage(usage));
                }
                // Now update the real buffer
                OpenGL.BufferSubData(All.ElementArrayBuffer, new IntPtr(offset), new IntPtr(length), ref srcPtr);
                GLES2Config.GlCheckError(this);
            }

            if (src.Ptr != srcPtr)
            {
                LogManager.Instance.Write( "[GLES2] HardwareIndexBuffer.WriteData - buffer pointer modified by GL.BufferData." );
            }
        }

        /// <summary>
        /// </summary>
        protected override void UpdateFromShadow()
        {
            if (useShadowBuffer && shadowUpdated && !suppressHardwareUpdate)
            {
                var srcData = shadowBuffer.Lock(lockStart, lockSize, BufferLocking.ReadOnly);
                OpenGL.BindBuffer(All.ElementArrayBuffer, this._bufferId);
                GLES2Config.GlCheckError(this);

                var srcPtr = new IntPtr(srcData.Ptr);
                // Update whole buffer if possible, otherwise normal
                if (lockStart == 0 && lockSize == sizeInBytes)
                {
                    OpenGL.BufferData(All.ElementArrayBuffer, new IntPtr(sizeInBytes), srcPtr, GLES2HardwareBufferManager.GetGLUsage(usage));
                    GLES2Config.GlCheckError(this);
                }
                else
                {
                    OpenGL.BufferSubData(All.ElementArrayBuffer, new IntPtr(lockStart), new IntPtr(lockSize), srcPtr);
                    GLES2Config.GlCheckError(this);
                }
                shadowBuffer.Unlock();
                shadowUpdated = false;
            }
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
                    OpenGL.DeleteBuffers(1, ref this._bufferId);
                    GLES2Config.GlCheckError(this);
                }
            }

            // If it is available, make the call to the
            // base class's Dispose(Boolean) method
            base.dispose(disposeManagedResources);
        }
    }
}
