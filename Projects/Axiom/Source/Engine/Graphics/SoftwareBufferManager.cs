
#region SVN Version Information
// <file>
//     <license see="http://axiomengine.sf.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

#endregion Namespace Declarations

namespace Axiom.Graphics
{
    /// <summary>
    /// 	Summary description for SoftwareBufferManager.
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

        #endregion

        #region Properties

        #endregion
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

        #endregion

        #region Methods

        public override IntPtr Lock( int offset, int length, BufferLocking locking )
        {
            Debug.Assert( !isLocked, "Cannot lock this buffer because it is already locked." );

            isLocked = true;

            return LockImpl( offset, length, locking );
        }

        protected override IntPtr LockImpl( int offset, int length, BufferLocking locking )
        {

            // return the offset into the array as a pointer
            handle = GCHandle.Alloc( data, GCHandleType.Pinned );
            return handle.AddrOfPinnedObject();
        }

        public override void ReadData( int offset, int length, IntPtr dest )
        {
            Debug.Assert( ( offset + length ) <= sizeInBytes, "Buffer overrun while trying to read a software buffer." );

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
            isLocked = false;

            UnlockImpl();
        }

        protected override void UnlockImpl()
        {

            handle.Free();
        }

        public override void WriteData( int offset, int length, IntPtr src, bool discardWholeBuffer )
        {
            Debug.Assert( ( offset + length ) <= sizeInBytes, "Buffer overrun while trying to write to a software buffer." );

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
        ///		buffer is software and not hardware.
        /// </summary>
        public IntPtr GetDataPointer( int offset )
        {
            return handle.AddrOfPinnedObject();
        }

		protected override void dispose( bool disposeManagedResources )
		{
			if ( !isDisposed )
			{
				if ( isLocked )
					Unlock();
				if ( disposeManagedResources )
				{
					data = null;
				}
			}
			isDisposed = true;

			// If it is available, make the call to the
			// base class's Dispose(Boolean) method
			base.dispose( disposeManagedResources );
        }
        #endregion
    }

    /// <summary>
    /// 
    /// </summary>
    public class SoftwareIndexBuffer : HardwareIndexBuffer
    {
        #region Member variables

        /// <summary>
        ///     Holds the buffer data.
        /// </summary>
		protected byte[] data;
        protected GCHandle handle;

        #endregion

        #region Constructors

        /// <summary>
        ///		
        /// </summary>
        /// <remarks>
        ///		This is already in system memory, so no need to use a shadow buffer.
        /// </remarks>
        /// <param name="type"></param>
        /// <param name="numIndices"></param>
        /// <param name="usage"></param>
        public SoftwareIndexBuffer( IndexType type, int numIndices, BufferUsage usage )
            : base( type, numIndices, usage, true, false )
        {
            data = new byte[ sizeInBytes ];
        }

        #endregion

        #region Methods

        public override IntPtr Lock( int offset, int length, BufferLocking locking )
        {
            Debug.Assert( !isLocked, "Cannot lock this buffer because it is already locked." );

            isLocked = true;

            return LockImpl( offset, length, locking );
        }


        protected override IntPtr LockImpl( int offset, int length, BufferLocking locking )
        {
            //isLocked = true;

            // return the offset into the array as a pointer
            handle = GCHandle.Alloc( data, GCHandleType.Pinned );
            return handle.AddrOfPinnedObject();
        }

        public override void ReadData( int offset, int length, IntPtr dest )
        {
            Debug.Assert( ( offset + length ) <= sizeInBytes, "Buffer overrun while trying to read a software buffer." );

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
            isLocked = false;

            UnlockImpl();
        }

        protected override void UnlockImpl()
        {

            handle.Free();
        }

        public override void WriteData( int offset, int length, IntPtr src, bool discardWholeBuffer )
        {
            Debug.Assert( ( offset + length ) <= sizeInBytes, "Buffer overrun while trying to write to a software buffer." );

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
        ///		buffer is software and not hardware.
        /// </summary>
        public IntPtr GetDataPointer( int offset )
        {
            return handle.AddrOfPinnedObject();
        }

		protected override void dispose( bool disposeManagedResources )
		{
			if ( !isDisposed )
			{
				if ( isLocked )
					Unlock();
				if ( disposeManagedResources )
				{
					data = null;
				}
			}
			isDisposed = true;

			// If it is available, make the call to the
			// base class's Dispose(Boolean) method
			base.dispose( disposeManagedResources );
		}

        #endregion
    }
}
