#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2006 Axiom Project Team

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
//     <license see="http://axiomengine.sf.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Runtime.InteropServices;

using Axiom.Core;
using Axiom.Graphics;

using D3D = Microsoft.DirectX.Direct3D;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.DirectX9
{
    /// <summary>
    /// 	Summary description for D3DHardwareVertexBuffer.
    /// </summary>
    public class D3DHardwareVertexBuffer : HardwareVertexBuffer
    {
        #region Member variables

        protected D3D.VertexBuffer d3dBuffer;
		protected D3D.Pool d3dPool;

		TimingMeter vbufferLockTimer = MeterManager.GetMeter( "Buffer Lock", "Axiom.RenderSystems.DirectX9" );

        #endregion

        #region Constructors

        public D3DHardwareVertexBuffer( int vertexSize, int numVertices, BufferUsage usage,
            D3D.Device device, bool useSystemMemory, bool useShadowBuffer )
            : base( vertexSize, numVertices, usage, useSystemMemory, useShadowBuffer )
        {
#if !NO_OGRE_D3D_MANAGE_BUFFERS
			d3dPool = useSystemMemory ? D3D.Pool.SystemMemory :
				// If not system mem, use managed pool UNLESS buffer is discardable
				// if discardable, keeping the software backing is expensive
				( ( usage & BufferUsage.Discardable ) != 0 ) ? D3D.Pool.Default : D3D.Pool.Managed;
#else
            d3dPool = useSystemMemory ? Pool.SystemMemory : Pool.Default;
#endif
            // Create the d3d vertex buffer
			d3dBuffer = new D3D.VertexBuffer( device,
                sizeInBytes,
                D3DHelper.ConvertEnum( usage ),
				D3D.VertexFormats.None,
				d3dPool );
		}

		~D3DHardwareVertexBuffer()
		{
			if ( d3dBuffer != null )
			{
				d3dBuffer.Dispose();
			}
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
        protected override IntPtr LockImpl( int offset, int length, BufferLocking locking )
        {
			D3D.LockFlags d3dLocking = D3DHelper.ConvertEnum( locking, usage );
            Microsoft.DirectX.GraphicsStream s = d3dBuffer.Lock( offset, length, d3dLocking );
            return s.InternalData;
        }

        /// <summary>
        /// 
        /// </summary>
        protected override void UnlockImpl()
        {
            // unlock the buffer
            d3dBuffer.Unlock();
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
			vbufferLockTimer.Enter();
            // lock the buffer real quick
            IntPtr dest = this.Lock( offset, length, discardWholeBuffer ? BufferLocking.Discard : BufferLocking.Normal );
			vbufferLockTimer.Exit();
            // copy that data in there
            Memory.Copy( src, dest, length );

            // unlock the buffer
            this.Unlock();
        }

		//---------------------------------------------------------------------
		public bool ReleaseIfDefaultPool()
		{
			if ( d3dPool == D3D.Pool.Default )
			{
				if ( d3dBuffer != null )
				{
					d3dBuffer.Dispose();
					d3dBuffer = null;
				}
				return true;
			}
			return false;
		}

		//---------------------------------------------------------------------
		public bool RecreateIfDefaultPool( D3D.Device device )
		{
			if ( d3dPool == D3D.Pool.Default )
			{
				// Create the d3d vertex buffer
				d3dBuffer = new D3D.VertexBuffer(
					typeof( byte ),
					sizeInBytes,
					device,
					D3DHelper.ConvertEnum( usage ),
					D3D.VertexFormats.None,
					d3dPool );
				return true;
			}
			return false;
		}

		protected override void dispose( bool disposeManagedResources )
		{
			if ( !isDisposed )
			{
				if ( disposeManagedResources )
				{
				}

				if ( d3dBuffer != null )
				{
					d3dBuffer.Dispose();
					d3dBuffer = null;
				}

			}

			// If it is available, make the call to the
			// base class's Dispose(Boolean) method
			base.dispose( disposeManagedResources );
		}

        #endregion

        #region Properties

        /// <summary>
        ///		Gets the underlying D3D Vertex Buffer object.
        /// </summary>
        public D3D.VertexBuffer D3DVertexBuffer
        {
            get
            {
                return d3dBuffer;
            }
        }

        #endregion
    }
}
