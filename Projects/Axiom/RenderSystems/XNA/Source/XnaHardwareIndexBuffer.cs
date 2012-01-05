#region LGPL License

/*
Axiom Graphics Engine Library
Copyright © 2003-2011 Axiom Project Team

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
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;

using Axiom.Core;
using Axiom.Graphics;

using XFG = Microsoft.Xna.Framework.Graphics;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.Xna
{
	/// <summary>
	/// 	Summary description for XnaHardwareIndexBuffer.
	/// </summary>
	public class XnaHardwareIndexBuffer : HardwareIndexBuffer
	{
		#region Member variables

		protected XFG.GraphicsDevice _device;
		protected XFG.IndexBuffer _xnaBuffer;
		protected System.Array data;

		private byte[] _bufferBytes;
		private XFG.IndexElementSize _bufferType;
		private int _offset;
		private int _length;

		#endregion

		#region Constructors

		unsafe public XnaHardwareIndexBuffer( HardwareBufferManagerBase manager, IndexType type, int numIndices, BufferUsage usage, XFG.GraphicsDevice device, bool useSystemMemory, bool useShadowBuffer )
			: base( manager, type, numIndices, usage, useSystemMemory, useShadowBuffer )
		{
			_bufferType = ( type == IndexType.Size16 ) ? XFG.IndexElementSize.SixteenBits : XFG.IndexElementSize.ThirtyTwoBits;

			// create the buffer
			if( usage == BufferUsage.Dynamic || usage == BufferUsage.DynamicWriteOnly )
			{
				_xnaBuffer = new XFG.DynamicIndexBuffer( device, sizeInBytes, XnaHelper.Convert( usage ), _bufferType );
			}
			else
			{
				_xnaBuffer = new XFG.IndexBuffer( device, sizeInBytes, XnaHelper.Convert( usage ), _bufferType );
			}

			_bufferBytes = new byte[sizeInBytes];
			_bufferBytes.Initialize();
		}

		#endregion

		#region Methods

		/// <summary>
		///
		/// </summary>
		/// <param name="offset"></param>
		/// <param name="length"></param>
		/// <param name="locking"></param>
		/// <returns></returns>
		unsafe protected override IntPtr LockImpl( int offset, int length, BufferLocking locking )
		{
			_offset = offset;
			_length = length;
			fixed( byte* bytes = &_bufferBytes[ offset ] )
			{
				return new IntPtr( bytes );
			}
		}

		/// <summary>
		///
		/// </summary>
		protected override void UnlockImpl()
		{
			//there is no unlock/lock system on XNA, just copy the byte buffer into the video card memory
			// d3dBuffer.SetData<byte>(bufferBytes);
			//this is a lot faster :)
			_xnaBuffer.SetData<byte>( _offset, _bufferBytes, _offset, _length );
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="offset"></param>
		/// <param name="length"></param>
		/// <param name="dest"></param>
		unsafe public override void ReadData( int offset, int length, IntPtr dest )
		{
			// lock the buffer for reading
			IntPtr src = this.Lock( offset, length, BufferLocking.ReadOnly );

			// copy that data in there
			Memory.Copy( src, dest, length );

			this.Unlock();
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="offset"></param>
		/// <param name="length"></param>
		/// <param name="src"></param>
		/// <param name="discardWholeBuffer"></param>
		public override void WriteData( int offset, int length, IntPtr src, bool discardWholeBuffer )
		{
			// lock the buffer re al quick
			IntPtr dest = this.Lock( offset, length, discardWholeBuffer ? BufferLocking.Discard : BufferLocking.Normal );

			// copy that data in there
			Memory.Copy( src, dest, length );

			this.Unlock();
		}

		protected override void dispose( bool disposeManagedResources )
		{
			if( !IsDisposed )
			{
				if( disposeManagedResources ) {}

				if( _xnaBuffer != null )
				{
					_xnaBuffer.Dispose();
					_xnaBuffer = null;
				}
			}

			// If it is available, make the call to the
			// base class's Dispose(Boolean) method
			base.dispose( disposeManagedResources );
		}

		#endregion

		#region Properties

		/// <summary>
		///		Gets the underlying Xna Vertex Buffer object.
		/// </summary>
		public XFG.IndexBuffer XnaIndexBuffer { get { return _xnaBuffer; } }

		#endregion
	}
}
