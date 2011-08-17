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
using Axiom.Graphics;
using Axiom.Core;
using Axiom.Utilities;
#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGLES
{
	public class GLESDefaultHardwareVertexBuffer : HardwareVertexBuffer
	{
		/// <summary>
		/// 
		/// </summary>
		protected IntPtr _dataPtr;
		protected byte[] _data;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="vertexSize"></param>
		/// <param name="numVertices"></param>
		/// <param name="usage"></param>
		public GLESDefaultHardwareVertexBuffer( VertexDeclaration declaration, int numVertices, BufferUsage usage )
			: base( null, declaration, numVertices, usage, true, false )
		{
			_data = new byte[ declaration.GetVertexSize() * numVertices ];
			_dataPtr = Memory.PinObject( _data );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="offset"></param>
		/// <returns></returns>
		public IntPtr GetData( int offset )
		{
			return new IntPtr( _dataPtr.ToInt32() + offset );
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
			Contract.Requires( ( offset + length ) <= sizeInBytes );
			// ignore discard, memory is not guaranteed to be zeroised
			Memory.Copy( src, GetData( offset ), length );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="offset"></param>
		/// <param name="length"></param>
		/// <param name="dest"></param>
		public override void ReadData( int offset, int length, IntPtr dest )
		{
			Contract.Requires( ( offset + length ) <= sizeInBytes );
			Memory.Copy( GetData( offset ), dest, length );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="offset"></param>
		/// <param name="length"></param>
		/// <param name="locking"></param>
		/// <returns></returns>
		protected override IntPtr LockImpl( int offset, int length, BufferLocking locking )
		{
			LogManager.Instance.Write( "WRONG LOCK" );
			return GetData( offset );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="offset"></param>
		/// <param name="length"></param>
		/// <param name="locking"></param>
		/// <returns></returns>
		public override IntPtr Lock( int offset, int length, BufferLocking locking )
		{
			LogManager.Instance.Write( "WRONG LOCK" );
			isLocked = true;
			return GetData( offset );
		}

		/// <summary>
		/// 
		/// </summary>
		protected override void UnlockImpl()
		{
			//nothing todo
		}

		/// <summary>
		/// 
		/// </summary>
		public override void Unlock()
		{
			isLocked = false;
			//nothing todo
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="disposeManagedResources"></param>
		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				if ( disposeManagedResources )
				{
					if ( _data != null )
					{
						Memory.UnpinObject( _data );
					}
				}
			}

			// If it is available, make the call to the
			// base class's Dispose(Boolean) method
			base.dispose( disposeManagedResources );
		}
	}
}

