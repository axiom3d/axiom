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
using System.Runtime.InteropServices;
using Axiom.Core;
using Axiom.CrossPlatform;
using Axiom.Graphics;
using Axiom.Math;
using SlimDX;
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

        [OgreVersion(1, 7, 2790)]
        protected class BufferResources
        {
            public VertexBuffer Buffer;
            public bool OutOfDate;
            public int LockOffset;
            public int LockLength;
            public BufferLocking LockOptions;
            public int LastUsedFrame;
        };

        [OgreVersion(1, 7, 2790)]
        protected class DeviceToBufferResourcesMap: Dictionary<Device, BufferResources> 
        {
        }

        #endregion

        /// <summary>
        /// Map between device to buffer resources.
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        protected DeviceToBufferResourcesMap mapDeviceToBufferResources = new DeviceToBufferResourcesMap();

        /// <summary>
        /// Buffer description.
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        protected VertexBufferDescription bufferDesc;

        /// <summary>
        /// Source buffer resources when working with multiple devices.
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        protected BufferResources sourceBuffer;

        /// <summary>
        /// Source buffer locked bytes.
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        private BufferBase sourceLockedBytes;

        /// <summary>
        /// Consistent system memory buffer for multiple devices support in case of write only buffers.
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        private BufferBase systemMemoryBuffer;


        #region Member variables

        [OgreVersion(1, 7, 2790)]
	    private static readonly object SDeviceAccessMutex = new object();

        [AxiomHelper(0, 8, "Holding a reference to SlimDX buffer in order to release it properly later")]
	    private DataStream _pSourceBytes;

	    #endregion Member variables

		#region Constructors

		public D3DHardwareVertexBuffer( HardwareBufferManagerBase manager, VertexDeclaration vertexDeclaration, int numVertices, BufferUsage usage, D3D.Device device, bool useSystemMemory, bool useShadowBuffer )
			: base( manager, vertexDeclaration, numVertices, usage, useSystemMemory, useShadowBuffer )
		{
            lock (SDeviceAccessMutex)
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
		        sourceLockedBytes  = null;

		        // Allocate the system memory buffer.
		        if (((usage & BufferUsage.WriteOnly) != 0) && D3DRenderSystem.ResourceManager.AutoHardwareBufferManagement)
		        {
                    systemMemoryBuffer = BufferBase.Wrap(new byte[Size]);
		        }
		        else
		        {			
			        systemMemoryBuffer = null;
		        }

                // Create buffer resource(s).
                foreach ( Device d3d9Device in D3DRenderSystem.ResourceCreationDevices )
                {
                    CreateBuffer(d3d9Device, eResourcePool);
                }
            }
		}

		#endregion Constructors

		#region Methods

        #region CreateBuffer

        /// <summary>
        /// Create the actual vertex buffer.
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        public void CreateBuffer(Device d3d9Device, Pool ePool)
        {
            lock(SDeviceAccessMutex)
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
        [OgreVersion(1, 7, 2790)]
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
				    UpdateBufferResources(sourceLockedBytes, bufferResources);
				    UnlockBuffer(sourceBuffer);
				    sourceLockedBytes = null;
			    }			
		    }
	    }

        protected void UpdateBufferResources(char[] p0, BufferResources bufferResources)
        {
            var ptr = Memory.PinObject( p0 );
            UpdateBufferResources( ptr, bufferResources );
            Memory.UnpinObject( ptr );
        }

        protected void UpdateBufferResources(BufferBase p0, BufferResources bufferResources)
	    {
	        throw new NotImplementedException();
	    }

	    protected void UnlockBuffer( BufferResources bufferResources )
	    {
	        bufferResources.Buffer.Unlock();

            // Reset attributes.
            bufferResources.OutOfDate = false;
            bufferResources.LockOffset = sizeInBytes;
            bufferResources.LockLength = 0;
            bufferResources.LockOptions = BufferLocking.Normal;

	        _pSourceBytes.Dispose();
	        _pSourceBytes = null;
	    }

        #region LockBuffer

        protected BufferBase LockBuffer(BufferResources bufferResources, int offset, int length)
	    {
	        _pSourceBytes = bufferResources.Buffer.Lock(
	            offset,
	            length,
	            D3DHelper.ConvertEnum(sourceBuffer.LockOptions, usage) );
            return BufferBase.Wrap(_pSourceBytes.DataPointer, length);
	    }

        #endregion

        #endregion

        #region LockImpl

        [OgreVersion(1, 7, 2790)]
        protected override BufferBase LockImpl(int offset, int length, BufferLocking options)
		{
			lock(SDeviceAccessMutex)
			{
                foreach (var it in mapDeviceToBufferResources)
			    {
			        var bufferResources = it.Value;

			        if ( options != BufferLocking.ReadOnly )
			            bufferResources.OutOfDate = true;

			        // Case it is the first buffer lock in this frame.
			        if ( bufferResources.LockLength == 0 )
			        {
			            if ( offset < bufferResources.LockOffset )
			                bufferResources.LockOffset = offset;
			            if ( length > bufferResources.LockLength )
			                bufferResources.LockLength = length;
			        }

			            // Case buffer already locked in this frame.
			        else
			        {
			            var highPoint = Utility.Max(offset + length,
			                                         bufferResources.LockOffset + bufferResources.LockLength );
			            bufferResources.LockOffset = Utility.Min( bufferResources.LockOffset, offset );
			            bufferResources.LockLength = highPoint - bufferResources.LockOffset;
			        }

			        bufferResources.LockOptions = options;
			    }

			    // Case we use system memory buffer -> just return it
			    if ( systemMemoryBuffer != null)
			    {
                    return systemMemoryBuffer.Offset(offset);
			    }
			    else
			    {
			        // Lock the source buffer.
			        sourceLockedBytes = LockBuffer( sourceBuffer, sourceBuffer.LockOffset, sourceBuffer.LockLength );

			        return sourceLockedBytes;
			    }
			}
		}

        #endregion

		protected override void UnlockImpl()
		{
		    lock ( SDeviceAccessMutex )
		    {
		        var nextFrameNumber = Root.Instance.NextFrameNumber;

                foreach (var it in mapDeviceToBufferResources)
		        {
		            var bufferResources = it.Value;

		            if ( bufferResources.OutOfDate &&
		                 bufferResources.Buffer != null &&
		                 nextFrameNumber - bufferResources.LastUsedFrame <= 1 )
		            {
		                if ( systemMemoryBuffer != null )
		                {
		                    UpdateBufferResources( systemMemoryBuffer.Offset(bufferResources.LockOffset), bufferResources );
		                }
		                else if ( sourceBuffer != bufferResources )
		                {
		                    UpdateBufferResources( sourceLockedBytes, bufferResources );
		                }
		            }
		        }

		        // Unlock the source buffer.
		        if ( systemMemoryBuffer == null )
		        {
		            UnlockBuffer( sourceBuffer );
		            sourceLockedBytes = null;
		        }
		    }
		}

        public override void ReadData(int offset, int length, BufferBase dest)
		{
			// lock the buffer for reading
			var src = this.Lock( offset, length, BufferLocking.ReadOnly );

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
        public override void WriteData(int offset, int length, BufferBase src, bool discardWholeBuffer)
		{
			// lock the buffer real quick
			var dest = Lock( offset, length, discardWholeBuffer ? BufferLocking.Discard : BufferLocking.Normal );
			// copy that data in there
			Memory.Copy( src, dest, length );

			// unlock the buffer
			this.Unlock();
		}

		//---------------------------------------------------------------------
		public bool ReleaseIfDefaultPool()
		{
		    throw new NotImplementedException();
		}

		//---------------------------------------------------------------------
		public bool RecreateIfDefaultPool( D3D.Device device )
		{
            throw new NotImplementedException();
		}

		protected override void dispose( bool disposeManagedResources )
		{
            if (systemMemoryBuffer != null)
            {
                systemMemoryBuffer = null;
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
		public VertexBuffer D3DVertexBuffer
		{
			get
			{
                var d3D9Device = D3DRenderSystem.ActiveD3D9Device;
	
			    BufferResources it;

                // Case vertex buffer was not found for the current device -> create it.		
                if (!mapDeviceToBufferResources.TryGetValue(d3D9Device, out it) || it.Buffer == null)
		        {						
			        CreateBuffer(d3D9Device, bufferDesc.Pool);
			        it = mapDeviceToBufferResources[d3D9Device];			
		        }

		        // Make sure that the buffer content is updated.
		        UpdateBufferContent(it);
		
		        it.LastUsedFrame = Root.Instance.NextFrameNumber;

		        return it.Buffer;
			}
		}

		#endregion Properties
	}
}