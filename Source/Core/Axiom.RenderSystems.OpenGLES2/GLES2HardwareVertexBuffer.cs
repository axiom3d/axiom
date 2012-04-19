using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Axiom.Graphics;

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

        public GLES2HardwareVertexBuffer(HardwareBufferManager manager, int vertexSize, int numVertices, dynamic usage, bool useShadowBuffer)
        { }


        protected override BufferBase LockImpl(int offset, int length, BufferLocking locking)
        {
            throw new NotImplementedException();
        }

        protected override void UnlockImpl()
        {
            throw new NotImplementedException();
        }

        public override void ReadData(int offset, int length, BufferBase dest)
        {
            throw new NotImplementedException();
        }

        public override void WriteData(int offset, int length, BufferBase src, bool discardWholeBuffer)
        {
            throw new NotImplementedException();
        }

        protected override void UpdateFromShadow()
        {
            base.UpdateFromShadow();
        }

        public int GLBufferID
        {
            get { return _bufferID; }
        }
    }
}
