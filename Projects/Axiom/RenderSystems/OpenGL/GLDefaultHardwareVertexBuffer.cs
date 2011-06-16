using System;
using Axiom.Graphics;

namespace Axiom.RenderSystems.OpenGL
{
    public class GLDefaultHardwareVertexBuffer : HardwareVertexBuffer
    {
        public GLDefaultHardwareVertexBuffer( HardwareBufferManagerBase manager, VertexDeclaration vertexDeclaration, int numVertices, BufferUsage usage, bool useSystemMemory, bool useShadowBuffer ) : base( manager, vertexDeclaration, numVertices, usage, useSystemMemory, useShadowBuffer )
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

        public IntPtr DataPtr( int offset )
        {
            throw new NotImplementedException();
        }
    }
}