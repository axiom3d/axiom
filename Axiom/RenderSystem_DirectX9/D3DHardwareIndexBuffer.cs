using System;
using System.Runtime.InteropServices;
using Microsoft.DirectX.Direct3D;
using D3D = Microsoft.DirectX.Direct3D;
using Axiom.SubSystems.Rendering;

namespace RenderSystem_DirectX9
{
	/// <summary>
	/// 	Summary description for D3DHardwareIndexBuffer.
	/// </summary>
	public class D3DHardwareIndexBuffer : HardwareIndexBuffer
	{
		#region Member variables

		protected D3D.Device device;
		protected D3D.IndexBuffer d3dBuffer;
		
		#endregion
		
		#region Constructors
		
		public D3DHardwareIndexBuffer(IndexType type, int numIndices, BufferUsage usage, 
			D3D.Device device, bool useSystemMemory, bool useShadowBuffer) 
			: base(type, numIndices, usage, useSystemMemory, useShadowBuffer)
		{
			// HACK: Forcing data type specification here, hardcoded to short for now.  Other consotructors dont work
			d3dBuffer = new IndexBuffer(typeof(short), numIndices, device, D3DHelper.ConvertEnum(usage), useSystemMemory ? Pool.SystemMemory : Pool.Default);

			// create the index buffer
			//d3dBuffer = new IndexBuffer(device, 
			//												sizeInBytes, 
			//												D3DHelper.ConvertEnum(usage),
			//												useSystemMemory ? Pool.SystemMemory : Pool.Default, 
			//												type == IndexType.Size16 ? true : false);
		}
		
		~D3DHardwareIndexBuffer()
		{
			d3dBuffer.Dispose();
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
		/// DOC
		protected override IntPtr LockImpl(int offset, int length, BufferLocking locking)
		{
			// TODO: Find right overload to use, length being ignored right now

			// lock the buffer and get an array of the data
			System.Array data = d3dBuffer.Lock(offset, D3DHelper.ConvertEnum(locking));

			return Marshal.UnsafeAddrOfPinnedArrayElement(data, 0);
		}

		/// <summary>
		/// 
		/// </summary>
		/// DOC
		public override void UnlockImpl()
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
		/// DOC
		public override void ReadData(int offset, int length, IntPtr dest)
		{
			// lock the buffer for reading
			IntPtr src = this.Lock(offset, length, BufferLocking.ReadOnly);
			
			// copy that data in there
			//Array.Copy(src, dest, length);
			PointerCopy(src, dest, length);

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
		/// DOC
		public override void WriteData(int offset, int length, IntPtr src, bool discardWholeBuffer)
		{
			// lock the buffer real quick
			IntPtr dest = this.Lock(offset, length, 
				discardWholeBuffer ? BufferLocking.Discard : BufferLocking.Normal);
			
			// copy that data in there
			//Array.Copy(src, dest, length);
			PointerCopy(src, dest, length);

			// unlock the buffer
			this.Unlock();
		}

		#endregion
		
		#region Properties
		
		/// <summary>
		///		Gets the underlying D3D Vertex Buffer object.
		/// </summary>
		public D3D.IndexBuffer D3DIndexBuffer
		{
			get { return d3dBuffer; }
		}

		#endregion

	}
}
