#region MIT/X11 License

//Copyright © 2003-2012 Axiom 3D Rendering Engine Project
//
//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//THE SOFTWARE.

#endregion License

#region SVN Version Information

// <file>
//     <license see="http://axiomengine.sf.net/wiki/index.php/license.txt"/>
//     <id value="$Id: GLES2DefaultHardwareVertexBuffer.cs 2805 2011-08-17 16:26:56Z borrillis $"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;

using Axiom.Core;
using Axiom.Graphics;
using Axiom.Utilities;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGLES2
{
	/// <summary>
	/// </summary>
	[OgreVersion( 1, 8, 0, "It's from trunk rev.'b0d2092773fb'" )]
	public class GLES2DefaultHardwareVertexBuffer : HardwareVertexBuffer
	{
		/// <summary>
		/// </summary>
		protected BufferBase _dataPtr;

		protected byte[] _data;

		/// <summary>
		/// </summary>
		/// <param name="vertexSize"> </param>
		/// <param name="numVertices"> </param>
		/// <param name="usage"> </param>
		public GLES2DefaultHardwareVertexBuffer( VertexDeclaration declaration, int numVertices, BufferUsage usage )
			: base( null, declaration, numVertices, usage, true, false )
		{
			this._data = new byte[ declaration.GetVertexSize() * numVertices ];
			this._dataPtr = BufferBase.Wrap( this._data );
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
			Memory.Copy( src, this._dataPtr + offset, length );
		}

		/// <summary>
		/// </summary>
		/// <param name="offset"> </param>
		/// <param name="length"> </param>
		/// <param name="dest"> </param>
		public override void ReadData( int offset, int length, BufferBase dest )
		{
			Contract.Requires( ( offset + length ) <= sizeInBytes );
			Memory.Copy( this._dataPtr + offset, dest, length );
		}

		/// <summary>
		/// </summary>
		/// <param name="offset"> </param>
		/// <param name="length"> </param>
		/// <param name="locking"> </param>
		/// <returns> </returns>
		protected override BufferBase LockImpl( int offset, int length, BufferLocking locking )
		{
			return this._dataPtr + offset;
		}

		/// <summary>
		/// </summary>
		/// <param name="offset"> </param>
		/// <param name="length"> </param>
		/// <param name="locking"> </param>
		/// <returns> </returns>
		public override BufferBase Lock( int offset, int length, BufferLocking locking )
		{
			isLocked = true;
			return this._dataPtr + offset;
		}

		/// <summary>
		/// </summary>
		protected override void UnlockImpl()
		{
			//nothing todo
		}

		/// <summary>
		/// </summary>
		public override void Unlock()
		{
			isLocked = false;
			//nothing todo
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
						this._dataPtr.SafeDispose();
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
