#region SVN Version Information

// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Axiom.Core;

#endregion Namespace Declarations

namespace Axiom.Graphics
{
	/// <summary>
	/// 	The SoftwareBufferManager is responsible for creation of software vertex and index buffers.
	/// <remarks>
	///     Software buffers are located in system memory and are often used as shadow copies of hardware buffers.
	///     A software buffer can be used independently of a hardware buffer, but cannot have a shadow buffer themselves.
	/// </remarks>
	/// </summary>
	public class SoftwareBufferManager : HardwareBufferManager
	{
		#region Methods

		/// <summary>
		///
		/// </summary>
		/// <param name="type"></param>
		/// <param name="numIndices"></param>
		/// <param name="usage"></param>
		/// <returns></returns>
		public override HardwareIndexBuffer CreateIndexBuffer( IndexType type, int numIndices, BufferUsage usage )
		{
			return new SoftwareIndexBuffer( type, numIndices, usage );
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="type"></param>
		/// <param name="numIndices"></param>
		/// <param name="usage"></param>
		/// <param name="useShadowBuffer"></param>
		/// <returns></returns>
		public override HardwareIndexBuffer CreateIndexBuffer( IndexType type, int numIndices, BufferUsage usage, bool useShadowBuffer )
		{
			return new SoftwareIndexBuffer( type, numIndices, usage );
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="vertexSize"></param>
		/// <param name="numVerts"></param>
		/// <param name="usage"></param>
		/// <returns></returns>
		public override HardwareVertexBuffer CreateVertexBuffer( int vertexSize, int numVerts, BufferUsage usage )
		{
			return new SoftwareVertexBuffer( vertexSize, numVerts, usage );
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="vertexSize"></param>
		/// <param name="numVerts"></param>
		/// <param name="usage"></param>
		/// <param name="useShadowBuffer"></param>
		/// <returns></returns>
		public override HardwareVertexBuffer CreateVertexBuffer( int vertexSize, int numVerts, BufferUsage usage, bool useShadowBuffer )
		{
			return new SoftwareVertexBuffer( vertexSize, numVerts, usage );
		}

		#endregion Methods

		#region Properties

		#endregion Properties
	}

	/// <summary>
	///
	/// </summary>
	public class SoftwareVertexBuffer : HardwareVertexBuffer
	{
		#region Fields

		/// <summary>
		///     Holds the buffer data.
		/// </summary>
		protected byte[] data;

		/// <summary>
		///     The handle used to pin buffer data.
		/// </summary>
		protected GCHandle handle;

		#endregion Fields

		#region Constructors

		/// <summary>
		///
		/// </summary>
		/// <remarks>
		///		This is already in system memory, so no need to use a shadow buffer.
		/// </remarks>
		/// <param name="vertexSize"></param>
		/// <param name="numVertices"></param>
		/// <param name="usage"></param>
		public SoftwareVertexBuffer( int vertexSize, int numVertices, BufferUsage usage )
			: base( vertexSize, numVertices, usage, true, false )
		{
			data = new byte[ sizeInBytes ];
		}

		#endregion Constructors

		#region Methods

		public override IntPtr Lock( int offset, int length, BufferLocking locking )
		{
			//Debug.Assert( !isLocked, "Cannot lock this buffer because it is already locked." );
			Debug.Assert( offset >= 0 && ( offset + length ) <= sizeInBytes, "The data area to be locked exceeds the buffer." );

			isLocked = true;

			return LockImpl( offset, length, locking );
		}

		protected override IntPtr LockImpl( int offset, int length, BufferLocking locking )
		{
			//Debug.Assert( !handle.IsAllocated, "Internal error, data being pinned twice." );

			// return the offset into the array as a pointer
			return GetDataPointer( offset );
		}

		public override void ReadData( int offset, int length, IntPtr dest )
		{
			//Debug.Assert(!isLocked, "Cannot lock this buffer because it is already locked."); //imitating render system specific hardware buffer behaviour
			Debug.Assert( offset >= 0 && ( offset + length ) <= sizeInBytes, "Buffer overrun while trying to read a software buffer." );

			unsafe
			{
				// get a pointer to the destination intptr
				byte* pDest = (byte*)dest.ToPointer();

				// copy the src data to the destination buffer
				for ( int i = 0; i < length; i++ )
				{
					pDest[ offset + i ] = data[ offset + i ];
				}
			}
		}

		public override void Unlock()
		{
			//Debug.Assert(isLocked, "Cannot unlock buffer if it wasn't locked.");

			isLocked = false;

			UnlockImpl();
		}

		protected override void UnlockImpl()
		{
			Memory.UnpinObject( data );
		}

		public override void WriteData( int offset, int length, IntPtr src, bool discardWholeBuffer )
		{
			//Debug.Assert( !isLocked, "Cannot lock this buffer because it is already locked." ); //imitating render system specific hardware buffer behaviour
			Debug.Assert( offset >= 0 && ( offset + length ) <= sizeInBytes, "Buffer overrun while trying to write to a software buffer." );

			unsafe
			{
				// get a pointer to the destination intptr
				byte* pSrc = (byte*)src.ToPointer();

				// copy the src data to the destination buffer
				for ( int i = 0; i < length; i++ )
				{
					data[ offset + i ] = pSrc[ offset + i ];
				}
			}
		}

		/// <summary>
		///		Allows direct access to the software buffer data in cases when it is known that the underlying
		///		buffer is software and not hardware. The buffer must be locked prior to operation.
		/// </summary>
		/// <remarks>
		/// The caller is responible for calling <see>Unlock</see> when they are done using this pointer
		/// </remarks>
		public IntPtr GetDataPointer( int offset )
		{
			//Debug.Assert( isLocked, "Cannot get data pointer if the buffer wasn't locked." );
			Debug.Assert( offset >= 0 && offset < sizeInBytes, "Offset into buffer out of range." );
			IntPtr result = Memory.PinObject( data );
			unsafe
			{
				result = (IntPtr)( (byte*)result + offset );
			}
			return result;
		}

		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				if ( IsLocked )
				{
					Unlock();
				}

				if ( disposeManagedResources )
				{
					data = null;
				}

				base.dispose( disposeManagedResources ); //isDisposed = true;
			}
		}

		#endregion Methods
	}

	/// <summary>
	///
	/// </summary>
	public class SoftwareIndexBuffer : HardwareIndexBuffer
	{
		#region Fields

		/// <summary>
		///     Holds the buffer data.
		/// </summary>
		protected byte[] data;

		/// <summary>
		///     The handle used to pin buffer data.
		/// </summary>
		protected GCHandle handle;

		#endregion Fields

		#region Constructors

		/// <summary>
		///
		/// </summary>
		/// <remarks>
		///		This is already in system memory, so no need to use a shadow buffer.
		/// </remarks>
		/// <param name="vertexSize"></param>
		/// <param name="numVertices"></param>
		/// <param name="usage"></param>
		public SoftwareIndexBuffer( IndexType type, int numIndices, BufferUsage usage )
			: base( type, numIndices, usage, true, false )
		{
			data = new byte[ sizeInBytes ];
		}

		#endregion Constructors

		#region Methods

		public override IntPtr Lock( int offset, int length, BufferLocking locking )
		{
			//Debug.Assert( !isLocked, "Cannot lock this buffer because it is already locked." );
			Debug.Assert( offset >= 0 && ( offset + length ) <= sizeInBytes, "The data area to be locked exceeds the buffer." );

			isLocked = true;

			return LockImpl( offset, length, locking );
		}

		protected override IntPtr LockImpl( int offset, int length, BufferLocking locking )
		{
			return GetDataPointer( offset );
		}

		public override void ReadData( int offset, int length, IntPtr dest )
		{
			//Debug.Assert( !isLocked, "Cannot lock this buffer because it is already locked." ); //imitating render system specific hardware buffer behaviour
			Debug.Assert( offset >= 0 && ( offset + length ) <= sizeInBytes, "Buffer overrun while trying to read a software buffer." );

			unsafe
			{
				// get a pointer to the destination intptr
				byte* pDest = (byte*)dest.ToPointer();

				// copy the src data to the destination buffer
				// TODO: use Memory.Copy() as soon as it provides a faster solution
				for ( int i = 0; i < length; i++ )
				{
					pDest[ offset + i ] = data[ offset + i ];
				}
			}
		}

		public override void Unlock()
		{
			//Debug.Assert(isLocked, "Cannot unlock buffer if it wasn't locked.");

			isLocked = false;

			UnlockImpl();
		}

		protected override void UnlockImpl()
		{
			Memory.UnpinObject( data );
		}

		public override void WriteData( int offset, int length, IntPtr src, bool discardWholeBuffer )
		{
			//Debug.Assert( !isLocked, "Cannot lock this buffer because it is already locked." ); //imitating render system specific hardware buffer behaviour
			Debug.Assert( offset >= 0 && ( offset + length ) <= sizeInBytes, "Buffer overrun while trying to write to a software buffer." );

			unsafe
			{
				// get a pointer to the destination intptr
				byte* pSrc = (byte*)src.ToPointer();

				// copy the src data to the destination buffer
				for ( int i = 0; i < length; i++ )
				{
					data[ offset + i ] = pSrc[ offset + i ];
				}
			}
		}

		/// <summary>
		///		Allows direct access to the software buffer data in cases when it is known that the underlying
		///		buffer is software and not hardware. The buffer must be locked prior to operation.
		/// </summary>
		/// <remarks>
		/// The caller is responible for calling <see>Unlock</see> when they are done using this pointer
		/// </remarks>
		public IntPtr GetDataPointer( int offset )
		{
			//Debug.Assert( isLocked, "Cannot get data pointer if the buffer wasn't locked." );
			Debug.Assert( offset >= 0 && offset < sizeInBytes, "Offset into buffer out of range." );
			IntPtr result = Memory.PinObject( data );
			unsafe
			{
				result = (IntPtr)( (byte*)result + offset );
			}
			return result;
		}

		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				if ( IsLocked )
				{
					Unlock();
				}

				if ( disposeManagedResources )
				{
					data = null;
				}

				base.dispose( disposeManagedResources ); //isDisposed = true;
			}
		}

		#endregion Methods
	}
}