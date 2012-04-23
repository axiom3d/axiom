using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Axiom.Core;
using Axiom.Graphics;
using GL = OpenTK.Graphics.ES20.GL;
using GLenum = OpenTK.Graphics.ES20.All;

namespace Axiom.RenderSystems.OpenGLES2
{
    class GLES2HardwareVertexBuffer : HardwareVertexBuffer
    {
        private int _bufferID;
        //Scratch buffer handling
        private bool _lockedToScratch;
        private int _scratchOffset, _scratchSize;
        private IntPtr _scratchPtr;
        bool _scratchUploadOnUnlock;

        public GLES2HardwareVertexBuffer(HardwareBufferManagerBase manager, int vertexSize, int numVertices,  BufferUsage usage, bool useShadowBuffer)
            :base(manager, vertexSize, numVertices, usage, false, useShadowBuffer)
        {
            if (!useShadowBuffer)
            {
                throw new AxiomException("Only supported with shadowBuffer");
            }

            GL.GenBuffers(1, ref _bufferID);

            if (_bufferID == 0)
            {
                throw new AxiomException("Cannot create GL ES vertex buffer");
            }

            (Root.Instance.RenderSystem as GLES2RenderSystem).BindGLBuffer(GLenum.ArrayBuffer, _bufferID);
            GL.BufferData(GLenum.ArrayBuffer, sizeInBytes, null, GLES2HardwareBufferManager.GetGLUsage(usage));

        }
        protected override void dispose(bool disposeManagedResources)
        {
            GL.DeleteBuffers(1, ref _bufferID);

            (Root.Instance.RenderSystem as GLES2RenderSystem).DeleteGLBuffer(GLenum.ArrayBuffer, _bufferID);

            base.dispose(disposeManagedResources);
        }

        protected override BufferBase LockImpl(int offset, int length, BufferLocking locking)
        {
            if (IsLocked)
            {
                throw new AxiomException("Invalid attempt to lock an index buffer that has already been locked.");

            }
            IntPtr retPtr = IntPtr.Zero;
            GLES2HardwareBufferManager glBufManager = (HardwareBufferManager.Instance as GLES2HardwareBufferManager);

            //Try to use scratch buffers for smaller buffers
            if (length < glBufManager.GLMapBufferThreshold)
            {
                retPtr = glBufManager.AllocateScratch(length);

                if (retPtr != null)
                {
                    _lockedToScratch = true;
                    _scratchOffset = offset;
                    _scratchSize = length;
                    _scratchPtr = retPtr;
                    _scratchUploadOnUnlock = (locking != BufferLocking.ReadOnly);

                    if (locking != BufferLocking.Discard)
                    {
                        //have to read back the data before returning the pointer
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
                GLenum access = GLenum.Zero;
                (Root.Instance.RenderSystem as GLES2RenderSystem).BindGLBuffer(GLenum.ArrayBuffer, _bufferID);

                if (locking == BufferLocking.Discard)
                {
                    //Discard the buffer
                    GL.BufferData(GLenum.ArrayBuffer, sizeInBytes, null, GLES2HardwareBufferManager.GetGLUsage(usage));
                }
                if ((usage & BufferUsage.WriteOnly) == BufferUsage.WriteOnly)
                    access = GLenum.WriteOnlyOes;

                var pbuffer = GL.Oes.MapBuffer(GLenum.ArrayBuffer, access);

                if (pbuffer == null)
                {
                    throw new AxiomException("Vertex Buffer: Out of memory");
                }

                //return offsetted
                //todo: need to return BufferBase, not IntPtr
                retPtr = pbuffer + offset;
                _lockedToScratch = false;
            }
            isLocked = true;
            return retPtr;
        }

        protected override void UnlockImpl()
        {
            if (_lockedToScratch)
            {
                if (_scratchUploadOnUnlock)
                {
                    //have to write the ata back to vertex buffer
                    WriteData(_scratchOffset, _scratchSize, _scratchPtr, _scratchOffset == 0 && _scratchSize == sizeInBytes);
                }

                (HardwareBufferManager.Instance as GLES2HardwareBufferManager).DeallocateScratch(_scratchPtr);

                _lockedToScratch = false;
            }
            else
            {
                (Root.Instance.RenderSystem as GLES2RenderSystem).BindGLBuffer(GLenum.ArrayBuffer, _bufferID);

                if (!GL.Oes.UnmapBuffer(GLenum.ArrayBuffer))
                {
                    throw new AxiomException("Buffer data corrupted, please reload");
                }
            }
            isLocked = false;
        }

        //public override void ReadData(int offset, int length, out BufferBase dest)
        //{
        //    if (useShadowBuffer)
        //    {
        //        var srcData = shadowBuffer.Lock(offset, length, BufferLocking.ReadOnly);
        //        dest = srcData;
        //        shadowBuffer.Unlock();
        //    }
        //    else
        //    {
        //        throw new AxiomException("Read hardware buffer is not supported");
        //    }
        //}
        public override void WriteData(int offset, int length, BufferBase src, bool discardWholeBuffer)
        {
            //Update the shadow buffer
            if (useShadowBuffer)
            {
                var destData = shadowBuffer.Lock(offset, length, discardWholeBuffer ? BufferLocking.Discard : BufferLocking.Normal);
                src = destData;
                shadowBuffer.Unlock();
            }

            if (offset == 0 && length == sizeInBytes)
            {
                GL.BufferData(GLenum.ArrayBuffer, sizeInBytes, src, GLES2HardwareBufferManager.getGLUsage(usage));

            }
            else
            {
                if (discardWholeBuffer)
                {
                    GL.BufferData(GLenum.ArrayBuffer, sizeInBytes, null, GLES2HardwareBufferManager.GetGLUsage(usage));
                }
                GL.BufferSubData(GLenum.ArrayBuffer, offset, length, src);
            }
        }

        protected override void UpdateFromShadow()
        {
            if (useShadowBuffer && shadowUpdated && !suppressHardwareUpdate)
            {
                var srcData = shadowBuffer.Lock(lockStart, lockSize, BufferLocking.ReadOnly);

                (Root.Instance.RenderSystem as GLES2RenderSystem).BindGLBuffer(GLenum.ArrayBuffer, _bufferID);

                //Update whole buffer if possible, otherwise normal
                if (lockStart == 0 && lockSize == sizeInBytes)
                {
                    GL.BufferData(GLenum.ArrayBuffer, sizeInBytes, srcData, GLES2HardwareBufferManager.GetGLUsage(usage));
                }
                else
                {
                    //Ogre FIXME: GPU frequently stalls here - DJR
                    GL.BufferSubData(GLenum.ArrayBuffer, lockStart, lockSize, srcData);
                }

                shadowBuffer.Unlock();
                shadowUpdated = false;
            }
        }

        public int GLBufferID
        {
            get { return _bufferID; }
        }

        public override void ReadData(int offset, int length, BufferBase dest)
        {
            throw new NotImplementedException();
        }
    }
}
