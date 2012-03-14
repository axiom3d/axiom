#region MIT/X11 License

//Copyright � 2003-2012 Axiom 3D Rendering Engine Project
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
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;

using Axiom.Core;
using Axiom.CrossPlatform;
using Axiom.Graphics;
using Axiom.Math;
using Axiom.Utilities;

using SharpDX;
using SharpDX.Direct3D9;

using D3D9 = SharpDX.Direct3D9;
using DX = SharpDX;
using VertexDeclaration = Axiom.Graphics.VertexDeclaration;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.DirectX9
{
	/// <summary>
	/// Should we ask D3D to manage vertex/index buffers automatically?
	/// Doing so avoids lost devices, but also has a performance impact
	/// which is unacceptably bad when using very large buffers
	/// </summary>
	/// AXIOM_D3D_MANAGE_BUFFERS
	/// <summary>
	/// Specialisation of HardwareVertexBuffer for D3D9
	/// </summary>
	public sealed class D3D9HardwareVertexBuffer : HardwareVertexBuffer, ID3D9Resource
	{
		#region Nested Types

		[OgreVersion( 1, 7, 2790 )]
		private class BufferResources
		{
			public bool IsOutOfDate;
			public int LastUsedFrame;
			public int LockLength;
			public int LockOffset;
			public BufferLocking LockOptions;
			public VertexBuffer VertexBuffer;
		};

		#endregion Nested Types

		#region Member variables

		/// <summary>
		/// Map between device to buffer resources.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		private readonly Dictionary<Device, BufferResources> _mapDeviceToBufferResources;

		/// <summary>
		/// Consistent system memory buffer for multiple devices support in case of write only buffers.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		private readonly BufferBase _systemMemoryBuffer;

		/// <summary>
		/// Buffer description.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		private VertexBufferDescription _bufferDesc;

		#endregion Member variables

		#region Properties

		/// <summary>
		///	Gets the underlying D3D Vertex Buffer object.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public VertexBuffer D3DVertexBuffer
		{
			get
			{
				Device d3D9Device = D3D9RenderSystem.ActiveD3D9Device;

				// Find the index buffer of this device.
				BufferResources it;
				bool wasBufferFound = this._mapDeviceToBufferResources.TryGetValue( d3D9Device, out it );

				// Case vertex buffer was not found for the current device -> create it.		
				if ( !wasBufferFound || it.VertexBuffer == null )
				{
					CreateBuffer( d3D9Device, this._bufferDesc.Pool );
					it = this._mapDeviceToBufferResources[ d3D9Device ];
				}

				if ( it.IsOutOfDate )
				{
					_updateBufferResources( this._systemMemoryBuffer, ref it );
				}

				it.LastUsedFrame = Root.Instance.NextFrameNumber;

				return it.VertexBuffer;
			}
		}

		#endregion Properties

		#region Construction and destruction

		[OgreVersion( 1, 7, 2 )]
		public D3D9HardwareVertexBuffer( HardwareBufferManagerBase manager, VertexDeclaration vertexDeclaration, int numVertices, BufferUsage usage, bool useSystemMemory, bool useShadowBuffer )
			: base( manager, vertexDeclaration, numVertices, usage, useSystemMemory, useShadowBuffer )
		{
			//Entering critical section
			this.LockDeviceAccess();

			this._mapDeviceToBufferResources = new Dictionary<Device, BufferResources>();

#if AXIOM_D3D_MANAGE_BUFFERS
			Pool eResourcePool = useSystemMemory ? Pool.SystemMemory : // If not system mem, use managed pool UNLESS buffer is discardable
				// if discardable, keeping the software backing is expensive
								 ( ( usage & BufferUsage.Discardable ) != 0 ) ? Pool.Default : Pool.Managed;
#else
			var eResourcePool = useSystemMemory ? D3D9.Pool.SystemMemory : D3D9.Pool.Default;
#endif

			// Set the desired memory pool.
			this._bufferDesc.Pool = eResourcePool;

			// Allocate the system memory buffer.
			this._systemMemoryBuffer = BufferBase.Wrap( new byte[ sizeInBytes ] );

			// Case we have to create this buffer resource on loading.
			if ( D3D9RenderSystem.ResourceManager.CreationPolicy == D3D9ResourceManager.ResourceCreationPolicy.CreateOnAllDevices )
			{
				foreach ( Device d3d9Device in D3D9RenderSystem.ResourceCreationDevices )
				{
					CreateBuffer( d3d9Device, this._bufferDesc.Pool );
				}
			}

			D3D9RenderSystem.ResourceManager.NotifyResourceCreated( this );

			//Leaving critical section
			this.UnlockDeviceAccess();
		}

		[OgreVersion( 1, 7, 2, "~D3D9HardwareVertexBuffer" )]
		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				if ( disposeManagedResources )
				{
					//Entering critical section
					this.LockDeviceAccess();

					foreach ( BufferResources it in this._mapDeviceToBufferResources.Values )
					{
						it.VertexBuffer.SafeDispose();
						it.SafeDispose();
					}
					this._mapDeviceToBufferResources.Clear();
					this._systemMemoryBuffer.SafeDispose();

					D3D9RenderSystem.ResourceManager.NotifyResourceDestroyed( this );

					//Leaving critical section
					this.UnlockDeviceAccess();
				}
			}

			// If it is available, make the call to the
			// base class's Dispose(Boolean) method
			base.dispose( disposeManagedResources );
		}

		#endregion Construction and destruction

		#region Methods

		/// <see cref="Axiom.Graphics.HardwareBuffer.LockImpl"/>
		[OgreVersion( 1, 7, 2 )]
		protected override BufferBase LockImpl( int offset, int length, BufferLocking options )
		{
			//Entering critical section
			this.LockDeviceAccess();

			if ( options != BufferLocking.ReadOnly )
			{
				foreach ( var it in this._mapDeviceToBufferResources )
				{
					BufferResources bufferResources = it.Value;
					bufferResources.IsOutOfDate = true;

					if ( bufferResources.LockLength > 0 )
					{
						int highPoint = Utility.Max( offset + length, bufferResources.LockOffset + bufferResources.LockLength );
						bufferResources.LockOffset = Utility.Min( bufferResources.LockOffset, offset );
						bufferResources.LockLength = highPoint - bufferResources.LockOffset;
					}
					else
					{
						if ( offset < bufferResources.LockOffset )
						{
							bufferResources.LockOffset = offset;
						}

						if ( length > bufferResources.LockLength )
						{
							bufferResources.LockLength = length;
						}
					}

					if ( bufferResources.LockOptions != BufferLocking.Discard )
					{
						bufferResources.LockOptions = options;
					}
				}
			}

			//Leaving critical section
			this.UnlockDeviceAccess();

			return this._systemMemoryBuffer + offset;
		}

		/// <see cref="Axiom.Graphics.HardwareBuffer.UnlockImpl"/>
		[OgreVersion( 1, 7, 2 )]
		protected override void UnlockImpl()
		{
			//Entering critical section
			this.LockDeviceAccess();

			int nextFrameNumber = Root.Instance.NextFrameNumber;

			foreach ( var it in this._mapDeviceToBufferResources )
			{
				BufferResources bufferResources = it.Value;

				if ( bufferResources.IsOutOfDate && bufferResources.VertexBuffer != null && nextFrameNumber - bufferResources.LastUsedFrame <= 1 )
				{
					_updateBufferResources( this._systemMemoryBuffer, ref bufferResources );
				}
			}

			//Leaving critical section
			this.UnlockDeviceAccess();
		}

		/// <see cref="Axiom.Graphics.HardwareBuffer.ReadData"/>
		[OgreVersion( 1, 7, 2 )]
		public override void ReadData( int offset, int length, BufferBase dest )
		{
			// There is no functional interface in D3D, just do via manual 
			// lock, copy & unlock

			// lock the buffer for reading
			BufferBase src = Lock( offset, length, BufferLocking.ReadOnly );

			// copy that data in there
			Memory.Copy( src, dest, length );

			// unlock the buffer
			Unlock();
		}

		/// <see cref="Axiom.Graphics.HardwareBuffer.WriteData(int, int, BufferBase, bool)"/>
		[OgreVersion( 1, 7, 2 )]
		public override void WriteData( int offset, int length, BufferBase src, bool discardWholeBuffer )
		{
			// There is no functional interface in D3D, just do via manual 
			// lock, copy & unlock

			// lock the buffer real quick
			BufferBase dest = Lock( offset, length, discardWholeBuffer ? BufferLocking.Discard : BufferLocking.Normal );
			// copy that data in there
			Memory.Copy( src, dest, length );

			// unlock the buffer
			Unlock();
		}

		/// <summary>
		/// Create the actual vertex buffer.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public void CreateBuffer( Device d3d9Device, Pool ePool )
		{
			// Find the vertex buffer of this device.
			BufferResources bufferResources;
			if ( this._mapDeviceToBufferResources.TryGetValue( d3d9Device, out bufferResources ) )
			{
				bufferResources.VertexBuffer.SafeDispose();
			}
			else
			{
				bufferResources = new BufferResources();
				this._mapDeviceToBufferResources.Add( d3d9Device, bufferResources );
			}

			bufferResources.VertexBuffer = null;
			bufferResources.IsOutOfDate = true;
			bufferResources.LockOffset = 0;
			bufferResources.LockLength = sizeInBytes;
			bufferResources.LockOptions = BufferLocking.Normal;
			bufferResources.LastUsedFrame = Root.Instance.NextFrameNumber;

			// Create the vertex buffer
			try
			{
				bufferResources.VertexBuffer = new VertexBuffer( d3d9Device, sizeInBytes, D3D9Helper.ConvertEnum( usage ), 0, // No FVF here, thank you.
																 ePool );
			}
			catch ( Exception ex )
			{
				throw new AxiomException( "Cannot restore D3D9 vertex buffer", ex );
			}

			this._bufferDesc = bufferResources.VertexBuffer.Description;
		}

		/// <summary>
		/// Update the given buffer content.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		private bool _updateBufferResources( BufferBase systemMemoryBuffer, ref BufferResources bufferResources )
		{
			Contract.RequiresNotNull( bufferResources, "Cannot update BufferResources in D3D9HardwareVertexBuffer!" );
			Contract.RequiresNotNull( bufferResources.VertexBuffer, "Cannot update BufferResources in D3D9HardwareVertexBuffer!" );
			Contract.Requires( bufferResources.IsOutOfDate );

			DataStream dstBytes;

			// Lock the buffer
			try
			{
				dstBytes = bufferResources.VertexBuffer.Lock( bufferResources.LockOffset, bufferResources.LockLength, D3D9Helper.ConvertEnum( bufferResources.LockOptions, usage ) );
			}
			catch ( Exception ex )
			{
				throw new AxiomException( "Cannot lock D3D9 vertex buffer!", ex );
			}

			using ( BufferBase src = systemMemoryBuffer + bufferResources.LockOffset )
			{
				using ( BufferBase dest = BufferBase.Wrap( dstBytes.DataPointer, (int)dstBytes.Length ) )
				{
					Memory.Copy( src, dest, bufferResources.LockLength );
				}
			}

			// Unlock the buffer.
			Result hr = bufferResources.VertexBuffer.Unlock();
			if ( hr.Failure )
			{
				throw new AxiomException( "Cannot unlock D3D9 vertex buffer: {0}", hr.ToString() );
			}

			bufferResources.IsOutOfDate = false;
			bufferResources.LockOffset = sizeInBytes;
			bufferResources.LockLength = 0;
			bufferResources.LockOptions = BufferLocking.Normal;

			return true;
		}

		#endregion Methods

		#region ID3D9Resource Members

		/// <see cref="ID3D9Resource.NotifyOnDeviceCreate"/>
		[OgreVersion( 1, 7, 2 )]
		public void NotifyOnDeviceCreate( Device d3d9Device )
		{
			//Entering critical section
			this.LockDeviceAccess();

			if ( D3D9RenderSystem.ResourceManager.CreationPolicy == D3D9ResourceManager.ResourceCreationPolicy.CreateOnAllDevices )
			{
				CreateBuffer( d3d9Device, this._bufferDesc.Pool );
			}

			//Leaving critical section
			this.UnlockDeviceAccess();
		}

		/// <see cref="ID3D9Resource.NotifyOnDeviceDestroy"/>
		[OgreVersion( 1, 7, 2 )]
		public void NotifyOnDeviceDestroy( Device d3d9Device )
		{
			//Entering critical section
			this.LockDeviceAccess();

			if ( this._mapDeviceToBufferResources.ContainsKey( d3d9Device ) )
			{
				this._mapDeviceToBufferResources[ d3d9Device ].VertexBuffer.SafeDispose();
				this._mapDeviceToBufferResources[ d3d9Device ].SafeDispose();
				this._mapDeviceToBufferResources.Remove( d3d9Device );
			}

			//Leaving critical section
			this.UnlockDeviceAccess();
		}

		/// <see cref="ID3D9Resource.NotifyOnDeviceLost"/>
		[OgreVersion( 1, 7, 2 )]
		public void NotifyOnDeviceLost( Device d3d9Device )
		{
			//Entering critical section
			this.LockDeviceAccess();

			if ( this._bufferDesc.Pool == Pool.Default )
			{
				if ( this._mapDeviceToBufferResources.ContainsKey( d3d9Device ) )
				{
					this._mapDeviceToBufferResources[ d3d9Device ].VertexBuffer.SafeDispose();
				}
			}

			//Leaving critical section
			this.UnlockDeviceAccess();
		}

		/// <see cref="ID3D9Resource.NotifyOnDeviceReset"/>
		[OgreVersion( 1, 7, 2 )]
		public void NotifyOnDeviceReset( Device d3d9Device )
		{
			//Entering critical section
			this.LockDeviceAccess();

			if ( D3D9RenderSystem.ResourceManager.CreationPolicy == D3D9ResourceManager.ResourceCreationPolicy.CreateOnAllDevices )
			{
				CreateBuffer( d3d9Device, this._bufferDesc.Pool );
			}

			//Leaving critical section
			this.UnlockDeviceAccess();
		}

		#endregion
	};
}
