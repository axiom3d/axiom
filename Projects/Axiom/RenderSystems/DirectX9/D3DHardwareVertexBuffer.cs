#region LGPL License

/*
Axiom Graphics Engine Library
Copyright � 2003-2011 Axiom Project Team

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
using Axiom.Core;
using Axiom.Graphics;
using SlimDX.Direct3D9;
using DX = SlimDX;
using D3D = SlimDX.Direct3D9;
using VertexDeclaration = Axiom.Graphics.VertexDeclaration;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.DirectX9
{
	/// <summary>
	/// 	Summary description for D3DHardwareVertexBuffer.
	/// </summary>
	public class D3DHardwareVertexBuffer : HardwareVertexBuffer
    {
        #region internal classes

        [OgreVersion(1, 7)]
        protected class BufferResources
        {
            public VertexBuffer Buffer;
            public bool OutOfDate;
            public int LockOffset;
            public int LockLength;
            public BufferLocking LockOptions;
            public int LastUsedFrame;
        };

        [OgreVersion(1, 7)]
        protected class DeviceToBufferResourcesMap: Dictionary<Device, BufferResources> 
        {
        }

        #endregion

        /// <summary>
        /// Map between device to buffer resources.
        /// </summary>
        [OgreVersion(1, 7)]
        protected DeviceToBufferResourcesMap mapDeviceToBufferResources = new DeviceToBufferResourcesMap();

        /// <summary>
        /// Buffer description.
        /// </summary>
        [OgreVersion(1, 7)]
        protected VertexBufferDescription bufferDesc;

        /// <summary>
        /// Source buffer resources when working with multiple devices.
        /// </summary>
        [OgreVersion(1, 7)]
        protected BufferResources sourceBuffer;

        /// <summary>
        /// Source buffer locked bytes.
        /// </summary>
        [OgreVersion(1, 7)]
        private IntPtr sourceLockedBytes;

        /// <summary>
        /// Consistent system memory buffer for multiple devices support in case of write only buffers.
        /// </summary>
        [OgreVersion(1, 7)]
        private char[] systemMemoryBuffer;


        #region Member variables

        protected D3D.VertexBuffer d3dBuffer;
	    private static object sDeviceAccessMutex = new object();

	    #endregion Member variables

		#region Constructors

		public D3DHardwareVertexBuffer( HardwareBufferManagerBase manager, VertexDeclaration vertexDeclaration, int numVertices, BufferUsage usage, D3D.Device device, bool useSystemMemory, bool useShadowBuffer )
			: base( manager, vertexDeclaration, numVertices, usage, useSystemMemory, useShadowBuffer )
		{
            lock (sDeviceAccessMutex)
            {
#if !NO_AXIOM_D3D_MANAGE_BUFFERS
                var eResourcePool = useSystemMemory
                                        ? Pool.SystemMemory
                                        : // If not system mem, use managed pool UNLESS buffer is discardable
                                    // if discardable, keeping the software backing is expensive
                                    ( ( usage & BufferUsage.Discardable ) != 0 ) ? Pool.Default : Pool.Managed;
#else
			    var eResourcePool = useSystemMemory ? Pool.SystemMemory : Pool.Default;
#endif

                // Set the desired memory pool.
		        bufferDesc.Pool = eResourcePool;

		        // Set source buffer to NULL.
		        sourceBuffer = null;
		        sourceLockedBytes  = IntPtr.Zero;

		        // Allocate the system memory buffer.
		        if (usage & BufferUsage.WriteOnly && D3DRenderSystem.ResourceManager.AutoHardwareBufferManagement)
		        {
		            systemMemoryBuffer = new char[Size];
		        }
		        else
		        {			
			        systemMemoryBuffer = null;
		        }

                // Create buffer resource(s).
                foreach ( Device d3d9Device in D3DRenderSystem.ResourceCreationDevice )
                {
                    CreateBuffer(d3d9Device, eResourcePool);
                }
            }
		}

		~D3DHardwareVertexBuffer()
		{
			if ( d3dBuffer != null )
			{
				d3dBuffer.Dispose();
			}
		}

		#endregion Constructors

		#region Methods

        #region CreateBuffer

        /// <summary>
        /// Create the actual vertex buffer.
        /// </summary>
        [OgreVersion(1, 7)]
        public void CreateBuffer(Device d3d9Device, Pool ePool)
        {
            lock(sDeviceAccessMutex)
            {
                // Find the vertex buffer of this device.
                BufferResources bufferResources;
                if (mapDeviceToBufferResources.TryGetValue(d3d9Device, out bufferResources))
                {
                    if (bufferResources.Buffer != null)
                    {
                        bufferResources.Buffer.Dispose();
                        bufferResources.Buffer = null;
                    }
                }
                else
                {
                    bufferResources = new BufferResources();
                    mapDeviceToBufferResources.Add( d3d9Device, bufferResources );
                }
                
                bufferResources.Buffer = null;
                bufferResources.OutOfDate = true;
                bufferResources.LockOffset = 0;
                bufferResources.LockLength = Size;
                bufferResources.LockOptions = BufferLocking.Normal;
                bufferResources.LastUsedFrame = Root.Instance.NextFrameNumber;

                // Create the vertex buffer


                bufferResources.Buffer = new VertexBuffer( d3d9Device,
                                                           sizeInBytes,
                                                           D3DHelper.ConvertEnum( usage ),
                                                           0, // No FVF here, thank you.
                                                           ePool );

                bufferDesc = bufferResources.Buffer.Description;

                // Update source buffer if need to.
                if ( sourceBuffer == null )
                {
                    sourceBuffer = bufferResources;
                }

                    // This is a new buffer and source buffer exists we must update the content now 
                    // to prevent situation where the source buffer will be destroyed and we won't be able to restore its content.
                else
                {
                    UpdateBufferContent( bufferResources );
                }
            }
        }

        #endregion

        #region UpdateBufferContent

        /// <summary>
        /// Update the given buffer content.
        /// </summary>
        [OgreVersion(1, 7)]
	    protected void UpdateBufferContent( BufferResources bufferResources )
	    {
	        if (bufferResources.OutOfDate)
		    {
			    if (systemMemoryBuffer != null)
			    {
				    UpdateBufferResources(systemMemoryBuffer, bufferResources);
			    }

			    else if (sourceBuffer != bufferResources && (usage & BufferUsage.WriteOnly) == 0)
			    {				
				    sourceBuffer.LockOptions = BufferLocking.ReadOnly;
				    sourceLockedBytes = LockBuffer(sourceBuffer, 0, Size);
				    UpdateBufferResources(mSourceLockedBytes, bufferResources);
				    UnlockBuffer(sourceBuffer);
				    sourceLockedBytes = null;
			    }			
		    }
	    }

        #endregion

        /// <summary>
		/// </summary>
		protected override IntPtr LockImpl( int offset, int length, BufferLocking locking )
		{
			D3D.LockFlags d3dLocking = D3DHelper.ConvertEnum( locking, usage );
			DX.DataStream s = d3dBuffer.Lock( offset, length, d3dLocking );
			return s.DataPointer;
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
			// lock the buffer real quick
			IntPtr dest = this.Lock( offset, length, discardWholeBuffer ? BufferLocking.Discard : BufferLocking.Normal );
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
					device,
					sizeInBytes,
					D3DHelper.ConvertEnum( usage ),
					D3D.VertexFormat.None,
					d3dPool );
				return true;
			}
			return false;
		}

		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				if ( disposeManagedResources )
				{
					if ( d3dBuffer != null && !d3dBuffer.Disposed )
					{
						d3dBuffer.Dispose();
						d3dBuffer = null;
					}
				}
			}

			// If it is available, make the call to the
			// base class's Dispose(Boolean) method
			base.dispose( disposeManagedResources );
		}

		#endregion Methods

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

		#endregion Properties
	}
}