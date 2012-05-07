using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Axiom.Graphics;

namespace Axiom.RenderSystems.OpenGLES2
{
	internal class GLES2HardwareIndexBuffer : HardwareIndexBuffer
	{
		protected override Core.BufferBase LockImpl( int offset, int length, BufferLocking locking )
		{
			throw new NotImplementedException();
		}

		protected override void UnlockImpl()
		{
			throw new NotImplementedException();
		}

		public override void ReadData( int offset, int length, Core.BufferBase dest )
		{
			throw new NotImplementedException();
		}

		public override void WriteData( int offset, int length, Core.BufferBase src, bool discardWholeBuffer )
		{
			throw new NotImplementedException();
		}

		public int GLBufferID { get; set; }
	}
}
