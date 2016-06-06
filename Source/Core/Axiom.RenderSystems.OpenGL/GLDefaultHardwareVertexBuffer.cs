using System;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Utilities;

namespace Axiom.RenderSystems.OpenGL
{
	public class GLDefaultHardwareVertexBuffer : HardwareVertexBuffer
	{
		protected byte[] _data;
		protected BufferBase _dataPtr;

		/// <summary>
		/// </summary>
		/// <param name="vertexSize"> </param>
		/// <param name="numVertices"> </param>
		/// <param name="usage"> </param>
		public GLDefaultHardwareVertexBuffer(VertexDeclaration declaration, int numVertices, BufferUsage usage)
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

		public IntPtr DataPtr( int offset )
		{
			return new IntPtr( (this._dataPtr + offset).Ptr );
		}
	}
}