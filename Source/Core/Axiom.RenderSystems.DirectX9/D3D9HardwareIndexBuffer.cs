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
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Utilities;
using D3D9 = SharpDX.Direct3D9;
using DX = SharpDX;

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
	/// Summary description for D3DHardwareIndexBuffer.
	/// </summary>
	public sealed class D3D9HardwareIndexBuffer : HardwareIndexBuffer, ID3D9Resource
	{
		#region Nested Types

		[OgreVersion( 1, 7, 2 )]
		private class BufferResources
		{
			public D3D9.IndexBuffer IndexBuffer;
			public bool IsOutOfDate;
			public int LockOffset;
			public int LockLength;
			public BufferLocking LockOptions;
			public int LastUsedFrame;
		};

		#endregion Nested Types

		#region Member variables

		/// <summary>
		/// Map between device to buffer resources.
		/// </summary>
		private readonly Dictionary<D3D9.Device, BufferResources> _mapDeviceToBufferResources;

		/// <summary>
		/// Buffer description.
		/// </summary>
		private D3D9.IndexBufferDescription _bufferDesc;

		/// <summary>
		/// Consistent system memory buffer for multiple devices support.
		/// </summary>
		private readonly BufferBase _systemMemoryBuffer;

		#endregion Member variables

		#region Properties

		/// <summary>
		///	Gets the underlying D3D Vertex Buffer object.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public D3D9.IndexBuffer D3DIndexBuffer
		{
			get
			{
				var d3d9Device = D3D9RenderSystem.ActiveD3D9Device;

				// Find the index buffer of this device.
				BufferResources resources;
				var wasBufferFound = this._mapDeviceToBufferResources.TryGetValue( d3d9Device, out resources );

				// Case index buffer was not found for the current device -> create it.
				if ( !wasBufferFound || resources.IndexBuffer == null )
				{
					CreateBuffer( d3d9Device, this._bufferDesc.Pool );
					resources = this._mapDeviceToBufferResources[ d3d9Device ];
				}

				if ( resources.IsOutOfDate )
					_updateBufferResources( this._systemMemoryBuffer, ref resources );

				resources.LastUsedFrame = Root.Instance.NextFrameNumber;

				return resources.IndexBuffer;
			}
		}

		#endregion Properties

		#region Construction and destruction

		[OgreVersion( 1, 7, 2 )]
		public D3D9HardwareIndexBuffer( HardwareBufferManagerBase manager, IndexType type, int numIndices, BufferUsage usage,
										bool useSystemMemory, bool useShadowBuffer )
			: base( manager, type, numIndices, usage, useSystemMemory, useShadowBuffer )
		{
			//Entering critical section
			this.LockDeviceAccess();

			this._mapDeviceToBufferResources = new Dictionary<D3D9.Device, BufferResources>();

#if AXIOM_D3D_MANAGE_BUFFERS
			var eResourcePool = useSystemMemory
									? D3D9.Pool.SystemMemory
									: // If not system mem, use managed pool UNLESS buffer is discardable
								// if discardable, keeping the software backing is expensive
								( ( usage & BufferUsage.Discardable ) != 0 ) ? D3D9.Pool.Default : D3D9.Pool.Managed;
#else
			var eResourcePool = useSystemMemory ? D3D9.Pool.SystemMemory : D3D9.Pool.Default;
#endif
			// Set the desired memory pool.
			this._bufferDesc.Pool = eResourcePool;

			// Allocate the system memory buffer.
			this._systemMemoryBuffer = BufferBase.Wrap( new byte[sizeInBytes] );

			// Case we have to create this buffer resource on loading.
			if ( D3D9RenderSystem.ResourceManager.CreationPolicy == D3D9ResourceManager.ResourceCreationPolicy.CreateOnAllDevices )
			{
				foreach ( var d3d9Device in D3D9RenderSystem.ResourceCreationDevices )
				{
					CreateBuffer( d3d9Device, this._bufferDesc.Pool );
				}
			}

			D3D9RenderSystem.ResourceManager.NotifyResourceCreated( this );

			//Leaving critical section
			this.UnlockDeviceAccess();
		}

		[OgreVersion( 1, 7, 2, "~D3D9HardwareIndexBuffer" )]
		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				if ( disposeManagedResources )
				{
					//Entering critical section
					this.LockDeviceAccess();

					foreach ( var it in this._mapDeviceToBufferResources.Values )
					{
						it.IndexBuffer.SafeDispose();
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
					var bufferResources = it.Value;
					bufferResources.IsOutOfDate = true;

					if ( bufferResources.LockLength > 0 )
					{
						var highPoint = Math.Utility.Max( offset + length, bufferResources.LockOffset + bufferResources.LockLength );
						bufferResources.LockOffset = Math.Utility.Min( bufferResources.LockOffset, offset );
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

			var nextFrameNumber = Root.Instance.NextFrameNumber;

			foreach ( var it in this._mapDeviceToBufferResources )
			{
				var bufferResources = it.Value;

				if ( bufferResources.IsOutOfDate && bufferResources.IndexBuffer != null &&
					 nextFrameNumber - bufferResources.LastUsedFrame <= 1 )
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
			var src = Lock( offset, length, BufferLocking.ReadOnly );

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
			var dest = Lock( offset, length, discardWholeBuffer ? BufferLocking.Discard : BufferLocking.Normal );

			// copy that data in there
			Memory.Copy( src, dest, length );

			// unlock the buffer
			Unlock();
		}

		/// <summary>
		/// Create the actual index buffer.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public void CreateBuffer( D3D9.Device d3d9Device, D3D9.Pool ePool )
		{
			//Entering critical section
			this.LockDeviceAccess();

			BufferResources bufferResources;

			// Find the vertex buffer of this device.
			if ( this._mapDeviceToBufferResources.TryGetValue( d3d9Device, out bufferResources ) )
			{
				bufferResources.IndexBuffer.SafeDispose();
			}
			else
			{
				bufferResources = new BufferResources();
				this._mapDeviceToBufferResources.Add( d3d9Device, bufferResources );
			}

			bufferResources.IndexBuffer = null;
			bufferResources.IsOutOfDate = true;
			bufferResources.LockOffset = 0;
			bufferResources.LockLength = sizeInBytes;
			bufferResources.LockOptions = BufferLocking.Normal;
			bufferResources.LastUsedFrame = Root.Instance.NextFrameNumber;

			// Create the Index buffer
			try
			{
				bufferResources.IndexBuffer = new D3D9.IndexBuffer( d3d9Device, sizeInBytes, D3D9Helper.ConvertEnum( usage ), ePool,
																	D3D9Helper.ConvertEnum( type ) );
			}
			catch ( Exception ex )
			{
				throw new AxiomException( "Cannot create D3D9 Index buffer", ex );
			}

			this._bufferDesc = bufferResources.IndexBuffer.Description;

			//Leaving critical section
			this.UnlockDeviceAccess();
		}

		/// <summary>
		/// Updates buffer resources from system memory buffer.
		/// </summary>
		private bool _updateBufferResources( BufferBase systemMemoryBuffer, ref BufferResources bufferResources )
		{
			Contract.RequiresNotNull( bufferResources, "Cannot update BufferResources in D3D9HardwareIndexBuffer!" );
			Contract.RequiresNotNull( bufferResources.IndexBuffer, "Cannot update BufferResources in D3D9HardwareIndexBuffer!" );
			Contract.Requires( bufferResources.IsOutOfDate );

			DX.DataStream dstBytes;

			// Lock the buffer
			try
			{
				dstBytes = bufferResources.IndexBuffer.Lock( bufferResources.LockOffset, bufferResources.LockLength, D3D9Helper.ConvertEnum( bufferResources.LockOptions, usage ) );
			}
			catch ( Exception ex )
			{
				throw new AxiomException( "Cannot lock D3D9 index buffer!", ex );
			}

			var src = systemMemoryBuffer.Offset( bufferResources.LockOffset );
			{
				using ( var dest = BufferBase.Wrap( dstBytes.DataPointer, bufferResources.LockLength ) )
				{
					Memory.Copy( src, dest, bufferResources.LockLength );
				}
			}

			// Unlock the buffer.
			var hr = bufferResources.IndexBuffer.Unlock();
			if ( hr.Failure )
			{
				throw new AxiomException( "Cannot unlock D3D9 index buffer: {0}", hr.ToString() );
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
		public void NotifyOnDeviceCreate( D3D9.Device d3d9Device )
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
		public void NotifyOnDeviceDestroy( D3D9.Device d3d9Device )
		{
			//Entering critical section
			this.LockDeviceAccess();

			if ( this._mapDeviceToBufferResources.ContainsKey( d3d9Device ) )
			{
				this._mapDeviceToBufferResources[ d3d9Device ].IndexBuffer.SafeDispose();
				this._mapDeviceToBufferResources[ d3d9Device ].SafeDispose();
				this._mapDeviceToBufferResources.Remove( d3d9Device );
			}

			//Leaving critical section
			this.UnlockDeviceAccess();
		}

		/// <see cref="ID3D9Resource.NotifyOnDeviceLost"/>
		[OgreVersion( 1, 7, 2 )]
		public void NotifyOnDeviceLost( D3D9.Device d3d9Device )
		{
			//Entering critical section
			this.LockDeviceAccess();

			if ( this._bufferDesc.Pool == D3D9.Pool.Default )
			{
				if ( this._mapDeviceToBufferResources.ContainsKey( d3d9Device ) )
				{
					this._mapDeviceToBufferResources[ d3d9Device ].IndexBuffer.SafeDispose();
				}
			}

			//Leaving critical section
			this.UnlockDeviceAccess();
		}

		/// <see cref="ID3D9Resource.NotifyOnDeviceReset"/>
		[OgreVersion( 1, 7, 2 )]
		public void NotifyOnDeviceReset( D3D9.Device d3d9Device )
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

		#endregion ID3D9Resource Members
	};
}