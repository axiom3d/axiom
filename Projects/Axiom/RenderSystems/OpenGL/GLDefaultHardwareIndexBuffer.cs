using System;
using Axiom.Graphics;

namespace Axiom.RenderSystems.OpenGL
{
    public class GLDefaultHardwareIndexBuffer: HardwareIndexBuffer
    {
        public GLDefaultHardwareIndexBuffer( HardwareBufferManagerBase manager, IndexType type, int numIndices, BufferUsage usage, bool useSystemMemory, bool useShadowBuffer ) : base( manager, type, numIndices, usage, useSystemMemory, useShadowBuffer )
        {
        }

        protected override IntPtr LockImpl( int offset, int length, BufferLocking locking )
        {
            throw new NotImplementedException();
        }

        protected override void UnlockImpl()
        {
            throw new NotImplementedException();
        }

        public override void ReadData( int offset, int length, IntPtr dest )
        {
            throw new NotImplementedException();
        }

        public override void WriteData( int offset, int length, IntPtr src, bool discardWholeBuffer )
        {
            throw new NotImplementedException();
        }

        public IntPtr DataPtr( int i )
        {
            throw new NotImplementedException();
        }
    }
}