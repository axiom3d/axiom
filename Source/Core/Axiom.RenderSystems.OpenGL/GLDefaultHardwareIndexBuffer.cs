using System;

using Axiom.Core;
using Axiom.Graphics;

namespace Axiom.RenderSystems.OpenGL
{
	public class GLDefaultHardwareIndexBuffer : HardwareIndexBuffer
	{
		public GLDefaultHardwareIndexBuffer( HardwareBufferManagerBase manager, IndexType type, int numIndices, BufferUsage usage, bool useSystemMemory, bool useShadowBuffer )
			: base( manager, type, numIndices, usage, useSystemMemory, useShadowBuffer ) {}

		protected override BufferBase LockImpl( int offset, int length, BufferLocking locking )
		{
			throw new NotImplementedException();
		}

		protected override void UnlockImpl()
		{
			throw new NotImplementedException();
		}

		public override void ReadData( int offset, int length, BufferBase dest )
		{
			throw new NotImplementedException();
		}

		public override void WriteData( int offset, int length, BufferBase src, bool discardWholeBuffer )
		{
			throw new NotImplementedException();
		}

		public IntPtr DataPtr( int i )
		{
			throw new NotImplementedException();
		}
	}
}
