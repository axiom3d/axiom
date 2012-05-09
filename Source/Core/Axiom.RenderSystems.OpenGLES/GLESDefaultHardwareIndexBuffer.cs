#region LGPL License

/*
Axiom Graphics Engine Library
Copyright (C) 2003-2010 Axiom Project Team
This file is part of Axiom.RenderSystems.OpenGLES
C# version developed by bostich.

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
//     <license see="http://axiomengine.sf.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;

using Axiom.Core;
using Axiom.Graphics;
using Axiom.Utilities;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGLES
{
	/// <summary>
	/// </summary>
	public class GLESDefaultHardwareIndexBuffer : HardwareIndexBuffer
	{
		protected byte[] _data;
		protected BufferBase _dataPtr;

		/// <summary>
		/// </summary>
		/// <param name="idxType"> </param>
		/// <param name="numIndexes"> </param>
		/// <param name="usage"> </param>
		public GLESDefaultHardwareIndexBuffer( HardwareBufferManagerBase manager, IndexType idxType, int numIndexes, BufferUsage usage )
			: base( manager, idxType, numIndexes, usage, true, false ) // always software, never shadowed
		{
			if ( idxType == IndexType.Size32 )
			{
				throw new AxiomException( "32 bit hardware buffers are not allowed in OpenGL ES." );
			}

			this._data = new byte[ sizeInBytes ];
			this._dataPtr = Memory.PinObject( this._data );
		}

		/// <summary>
		/// </summary>
		/// <param name="offset"> </param>
		public BufferBase GetData( int offset )
		{
			return this._dataPtr.Clone() + offset );
		}

		/// <summary>
		/// </summary>
		/// <param name="offset"> </param>
		/// <param name="length"> </param>
		/// <param name="locking"> </param>
		/// <returns> </returns>
		protected override IntPtr LockImpl( int offset, int length, BufferLocking locking )
		{
			// Only for use internally, no 'locking' as such
			return GetData( offset );
		}

		/// <summary>
		/// </summary>
		protected override void UnlockImpl()
		{
			// Nothing to do
		}

		/// <summary>
		/// </summary>
		/// <param name="offset"> </param>
		/// <param name="length"> </param>
		/// <param name="locking"> </param>
		/// <returns> </returns>
		public override IntPtr Lock( int offset, int length, BufferLocking locking )
		{
			isLocked = true;
			return GetData( offset );
		}

		/// <summary>
		/// </summary>
		public override void Unlock()
		{
			isLocked = false;
			// Nothing to do
		}

		/// <summary>
		/// </summary>
		/// <param name="offset"> </param>
		/// <param name="length"> </param>
		/// <param name="dest"> </param>
		public override void ReadData( int offset, int length, BufferBase dest )
		{
			Contract.Requires( ( offset + length ) <= sizeInBytes );
			Memory.Copy( GetData( offset ), dest, length );
		}

		/// <summary>
		/// </summary>
		/// <param name="offset"> </param>
		/// <param name="length"> </param>
		/// <param name="src"> </param>
		/// <param name="discardWholeBuffer"> </param>
		public override void WriteData( int offset, int length, BufferBase src, bool discardWholeBuffer )
		{
			Contract.Requires( ( offset + length ) <= sizeInBytes );
			// ignore discard, memory is not guaranteed to be zeroised
			Memory.Copy( src, GetData( offset ), length );
		}

		/// <summary>
		/// </summary>
		/// <param name="disposeManagedResources"> </param>
		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				if ( disposeManagedResources )
				{
					if ( this._data != null )
					{
						Memory.UnpinObject( this._data );
						this._data = null;
					}
				}
			}

			// If it is available, make the call to the
			// base class's Dispose(Boolean) method
			base.dispose( disposeManagedResources );
		}
	}
}
