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

#endregion LGPL License

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

using Axiom.Core;
using Axiom.Graphics;

using XFG = Microsoft.Xna.Framework.Graphics;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.Xna
{
	/// <summary>
	/// 	Summary description for XnaHardwareVertexBuffer.
	/// </summary>
	///
	//there is no XNA buffer locking system, copy first into a byte array and when the unlock function is called, fill in memory with setdata<byte>(...)
	unsafe public class XnaHardwareVertexBuffer : HardwareVertexBuffer
	{
		#region Member variables

		protected XFG.VertexBuffer _buffer;
		protected XFG.GraphicsDevice _device;

		private byte[] _bufferBytes;

		private int _offset;
		private int _length;

		#endregion Member variables

		#region Constructors

		public XnaHardwareVertexBuffer( HardwareBufferManagerBase manager, VertexDeclaration vertexDeclaration, int numVertices, BufferUsage usage, XFG.GraphicsDevice dev, bool useSystemMemory, bool useShadowBuffer )
			: base( manager, vertexDeclaration, numVertices, usage, useSystemMemory, useShadowBuffer )
		{
			_device = dev;
			if ( !( vertexDeclaration is XnaVertexDeclaration ) )
			{
				throw new AxiomException ("Invalid VertexDeclaration supplied, must be created by HardwareBufferManager.CreateVertexDeclaration()" );
			}
			if (usage == BufferUsage.Dynamic || usage == BufferUsage.DynamicWriteOnly)
			{
				_buffer = new XFG.DynamicVertexBuffer(_device, ( (XnaVertexDeclaration)vertexDeclaration ).XFGVertexDeclaration , numVertices, XnaHelper.Convert(usage));
			}
			else
				_buffer = new XFG.VertexBuffer(_device, ( (XnaVertexDeclaration)vertexDeclaration ).XFGVertexDeclaration, numVertices, XnaHelper.Convert(usage));

			_bufferBytes = new byte[ vertexDeclaration.GetVertexSize() * numVertices];
			_bufferBytes.Initialize();
		}

		#endregion Constructors

		#region Methods

		/// <summary>
		///
		/// </summary>
		/// <param name="offset"></param>
		/// <param name="length"></param>
		/// <param name="locking"></param>
		/// <returns></returns>
		protected override IntPtr LockImpl( int offset, int length, BufferLocking locking )
		{
			_offset = offset;
			_length = length;
			fixed ( byte* bytes = &_bufferBytes[ offset ] )
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
			// _buffer.SetData<byte>(bufferBytes);
			//this is faster :)
			_buffer.SetData<byte>( _offset, _bufferBytes, _offset, _length, 0 );
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="offset"></param>
		/// <param name="length"></param>
		/// <param name="dest"></param>
		public override void ReadData( int offset, int length, IntPtr dest )
		{
			// lock the buffer for reading
			IntPtr src = this.Lock( offset, length, BufferLocking.ReadOnly );

			// copy that data in there
			Memory.Copy( src, dest, length );

			// unlock the buffer
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
			// lock the buffer real quick
			IntPtr dest = this.Lock( offset, length,
				discardWholeBuffer ? BufferLocking.Discard : BufferLocking.Normal );

			// copy that data in there
			Memory.Copy( src, dest, length );

			this.Unlock();
		}

		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				if ( disposeManagedResources )
				{
				}

				if ( _buffer != null )
				{
					_buffer.Dispose();
					_buffer = null;
				}
			}

			// If it is available, make the call to the
			// base class's Dispose(Boolean) method
			base.dispose( disposeManagedResources );
		}

		#endregion Methods

		#region Properties

		/// <summary>
		///		Gets the underlying Xna Vertex Buffer object.
		/// </summary>
		public XFG.VertexBuffer XnaVertexBuffer
		{
			get
			{
				return _buffer;
			}
		}

		#endregion Properties
	}
}