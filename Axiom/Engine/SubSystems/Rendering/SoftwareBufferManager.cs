using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Axiom.SubSystems.Rendering
{
	/// <summary>
	/// 	Summary description for SoftwareBufferManager.
	/// </summary>
	public class SoftwareBufferManager : HardwareBufferManager
	{
		#region Singleton implementation

		protected static SoftwareBufferManager Instance = new SoftwareBufferManager();

		static SoftwareBufferManager() {}

		protected SoftwareBufferManager() { }

		#endregion

		#region Member variables
		
		#endregion
		
		#region Methods

		/// <summary>
		/// 
		/// </summary>
		/// <param name="type"></param>
		/// <param name="numIndices"></param>
		/// <param name="usage"></param>
		/// <returns></returns>
		/// DOC
		public override HardwareIndexBuffer CreateIndexBuffer(IndexType type, int numIndices, BufferUsage usage)
		{
			return new SoftwareIndexBuffer(type, numIndices, usage);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="type"></param>
		/// <param name="numIndices"></param>
		/// <param name="usage"></param>
		/// <param name="useShadowBuffer"></param>
		/// <returns></returns>
		/// DOC
		public override HardwareIndexBuffer CreateIndexBuffer(IndexType type, int numIndices, BufferUsage usage, bool useShadowBuffer)
		{
			return new SoftwareIndexBuffer(type, numIndices, usage);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="vertexSize"></param>
		/// <param name="numVerts"></param>
		/// <param name="usage"></param>
		/// <returns></returns>
		/// DOC
		public override HardwareVertexBuffer CreateVertexBuffer(int vertexSize, int numVerts, BufferUsage usage)
		{
			return new SoftwareVertexBuffer(vertexSize, numVerts, usage);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="vertexSize"></param>
		/// <param name="numVerts"></param>
		/// <param name="usage"></param>
		/// <param name="useShadowBuffer"></param>
		/// <returns></returns>
		/// DOC
		public override HardwareVertexBuffer CreateVertexBuffer(int vertexSize, int numVerts, BufferUsage usage, bool useShadowBuffer)
		{
			return new SoftwareVertexBuffer(vertexSize, numVerts, usage);
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
		#region Member variables
		
		protected byte[] data;
		
		#endregion

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
		/// DOC
		public SoftwareVertexBuffer(int vertexSize, int numVertices, BufferUsage usage)
			: base(vertexSize, numVertices, usage, true, false)
		{
			data = new byte[sizeInBytes];
		}

		#endregion

		#region Methods

		public override IntPtr Lock(int offset, int length, BufferLocking locking)
		{
			byte[] tmpData = new byte[length];

			Array.Copy(data, offset, tmpData, 0, length);

			// TODO: Since what we are returning is temporary,
			// consider saving a reference to this and using it to update the local data on an unlock
			return Marshal.UnsafeAddrOfPinnedArrayElement(tmpData, 0);
		}

		protected override IntPtr LockImpl(int offset, int length, BufferLocking locking)
		{
			// do nothing
			return IntPtr.Zero;
		}

		public override void ReadData(int offset, int length, IntPtr dest)
		{
			/*
			Debug.Assert((offset + length) <= sizeInBytes, "Buffer overrun while trying to read a software buffer.");
 
			// copy the data to the dest array
			Array.Copy(dest, 0, data, offset, length);
			*/
		}

		public override void Unlock()
		{
			// TODO: Store temp data from lock and update local data here?
		}

		public override void UnlockImpl()
		{
			// do nothing
		}

		public override void WriteData(int offset, int length, IntPtr src, bool discardWholeBuffer)
		{
			Debug.Assert((offset + length) <= sizeInBytes, "Buffer overrun while trying to read a software buffer.");
 
			// copy the data to the local array
			/*Array.Copy(data, offset, src, 0, length);			*/
		}

		#endregion

	}

	/// <summary>
	/// 
	/// </summary>
	public class SoftwareIndexBuffer : HardwareIndexBuffer
	{
		#region Member variables
		
		protected byte[] data;
		
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
		/// DOC
		public SoftwareIndexBuffer(IndexType type, int numIndices, BufferUsage usage)
			: base(type, numIndices, usage, true, false)
		{
		}

		#endregion

		#region Methods

		public override IntPtr Lock(int offset, int length, BufferLocking locking)
		{
			byte[] tmpData = new byte[length];

			Array.Copy(data, offset, tmpData, 0, length);

			// TODO: Since what we are returning is temporary,
			// consider saving a reference to this and using it to update the local data on an unlock
			return Marshal.UnsafeAddrOfPinnedArrayElement(tmpData, 0);

		}


		protected override IntPtr LockImpl(int offset, int length, BufferLocking locking)
		{
			// do nothing
			return IntPtr.Zero;
		}

		public override void ReadData(int offset, int length, IntPtr dest)
		{
			Debug.Assert((offset + length) <= sizeInBytes, "Buffer overrun while trying to read a software buffer.");
 
			// copy the data to the dest array
			//Array.Copy(dest, 0, data, offset, length);
		}

		public override void Unlock()
		{
			// TODO: Store temp data from lock and update local data here?
		}

		public override void UnlockImpl()
		{
			// do nothing
		}

		public override void WriteData(int offset, int length, IntPtr src, bool discardWholeBuffer)
		{
			Debug.Assert((offset + length) <= sizeInBytes, "Buffer overrun while trying to read a software buffer.");
 
			// copy the data to the local array
			//Array.Copy(data, offset, src, 0, length);			
		}

		#endregion
	}
}
