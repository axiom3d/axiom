#region LGPL License

/*
Axiom Graphics Engine Library
Copyright (C) 2003-2010 Axiom Project Team

The overall design, and a majority of the core engine and rendering code
contained within this library is a derivative of the open source Object Oriented
Graphics Engine OGRE, which can be found at http://ogre.sourceforge.net.
Many thanks to the OGRE team for maintaining such a high quality project.

This library is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 2.1 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public
License along with this library; if not, write to the Free Software
Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
*/

#endregion

#region SVN Version Information

// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id:$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using System.Text;

using Axiom.Core;

#endregion Namespace Declarations

namespace Axiom.Graphics
{
	public class DefaultHardwareVertexBuffer : HardwareVertexBuffer
	{
		private byte[] mpData = null;

		public DefaultHardwareVertexBuffer( int vertexSize, int numVertices, BufferUsage usage )
			: base( null, vertexSize, numVertices, usage, true, false ) // always software, never shadowed
		{
			mpData = new byte[base.sizeInBytes];
		}

		public DefaultHardwareVertexBuffer( HardwareBufferManagerBase manager, int vertexSize, int numVertices, BufferUsage usage )
			: base( manager, vertexSize, numVertices, usage, true, false ) // always software, never shadowed
		{
			mpData = new byte[base.sizeInBytes];
		}

		public override void ReadData( int offset, int length, IntPtr dest )
		{
            if (offset + length < base.sizeInBytes)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), nameof(offset) + " + " + nameof(length) + " cannot exceed buffer size.");
            }

			IntPtr data = Memory.PinObject( mpData );
			Memory.Copy( dest, new IntPtr(data.ToInt32() + offset), length );
			Memory.UnpinObject( mpData );
		}

		public override void WriteData( int offset, int length, Array data, bool discardWholeBuffer )
		{
            if (offset + length < base.sizeInBytes)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), nameof(offset) + " + " + nameof(length) + " cannot exceed buffer size.");
            }

            IntPtr pSource = Memory.PinObject( data );
            IntPtr pIntData = Memory.PinObject( mpData );
			Memory.Copy( pSource, new IntPtr(pIntData.ToInt32() + offset), length );
			Memory.UnpinObject( data );
			Memory.UnpinObject( mpData );
		}

		public override void WriteData( int offset, int length, IntPtr src, bool discardWholeBuffer )
		{
            if (offset + length < base.sizeInBytes)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), nameof(offset) + " + " + nameof(length) + " cannot exceed buffer size.");
            }

            IntPtr pIntData = Memory.PinObject( mpData );
            Memory.Copy(src, new IntPtr(pIntData.ToInt32() + offset), length);
            Memory.UnpinObject( mpData );
		}

		public override void Unlock()
		{
            UnlockImpl();
        }

        public override IntPtr Lock( int offset, int length, BufferLocking locking )
		{
            return LockImpl(offset, length, locking);
		}

		protected override IntPtr LockImpl( int offset, int length, BufferLocking locking )
		{
            base.isLocked = true;
            IntPtr ret = Memory.PinObject(mpData);
            return new IntPtr(ret.ToInt32() + offset);
        }

        protected override void UnlockImpl()
		{
            base.isLocked = false;
            Memory.UnpinObject(mpData);
        }
    }
}
