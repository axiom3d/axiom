using System;
using System.Runtime.InteropServices;
using Microsoft.DirectX.Direct3D;
using D3D = Microsoft.DirectX.Direct3D;
using Axiom.SubSystems.Rendering;

namespace RenderSystem_DirectX9
{
	/// <summary>
	/// 	Summary description for D3DHardwareVertexBuffer.
	/// </summary>
	public class D3DHardwareVertexBuffer : HardwareVertexBuffer
	{
		#region Member variables

		protected D3D.VertexBuffer d3dBuffer;
		
		#endregion
		
		#region Constructors
		
		public D3DHardwareVertexBuffer(int vertexSize, int numVertices, BufferUsage usage, 
			D3D.Device device, bool useSystemMemory, bool useShadowBuffer) 
			: base(vertexSize, numVertices, usage, useSystemMemory, useShadowBuffer)
		{
			// Create the d3d vertex buffer
			d3dBuffer = new D3D.VertexBuffer(typeof(byte), 
																		numVertices * vertexSize, 
																		device,
																		D3DHelper.ConvertEnum(usage), 
																		0, 
																		useSystemMemory ? Pool.SystemMemory : Pool.Default);
		}

		~D3DHardwareVertexBuffer()
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
			// use the graphics stream overload to supply offset and length
			GraphicsStream gs = d3dBuffer.Lock(0, length, D3DHelper.ConvertEnum(locking));
			
			// return the graphics streams internal data
			// TODO: Beware if this is taken out of future versions
			return gs.InternalData;

			// lock the buffer and get an array of the data
			//System.Array data = d3dBuffer.Lock(offset, D3DHelper.ConvertEnum(locking));

			//return Marshal.UnsafeAddrOfPinnedArrayElement(data, 0);
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
			PointerCopy(src, dest, length);

			// unlock the buffer
			this.Unlock();
		}

		#endregion
		
		#region Properties

		/// <summary>
		///		Gets the underlying D3D Vertex Buffer object.
		/// </summary>
		public D3D.VertexBuffer D3DVertexBuffer
		{
			get { return d3dBuffer; }
		}
		
		#endregion

	}
}
