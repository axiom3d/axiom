using System;
using System.Diagnostics;

namespace Axiom.SubSystems.Rendering
{
	/// <summary>
	/// Summary description for HardwareBuffer.
	/// </summary>
	public abstract class HardwareBuffer
	{
		#region Member variables
		
		protected int sizeInBytes;
		protected BufferUsage usage;
		protected bool isLocked;
		protected int lockStart;
		protected int lockSize;
		protected bool useSystemMemory;
		protected bool useShadowBuffer;
		protected HardwareBuffer shadowBuffer;
		protected bool shadowUpdated;
		
		#endregion

		#region Constructors

		internal HardwareBuffer(BufferUsage usage, bool useSystemMemory, bool useShadowBuffer)
		{
			this.usage = usage;
			this.useSystemMemory = useSystemMemory;
			this.useShadowBuffer = useShadowBuffer;
		}

		#endregion
			
		#region Methods

		/// <summary>
		///		Used to lock a vertex buffer in hardware memory in order to make modifications.
		/// </summary>
		/// <param name="offset">Starting index in the buffer to lock.</param>
		/// <param name="length">Nunber of bytes to lock after the offset.</param>
		/// <param name="locking">Specifies how to lock the buffer.</param>
		/// <returns>An array of the <code>System.Type</code> associated with this VertexBuffer.</returns>
		public virtual IntPtr Lock(int offset, int length, BufferLocking locking)
		{
			Debug.Assert(!isLocked, "Cannot lock this buffer because it is already locked.");

			IntPtr data = IntPtr.Zero;

			if(useShadowBuffer)
			{
				if(locking != BufferLocking.ReadOnly)
				{
					// we have to assume a read / write lock so we use the shadow buffer
					// and tag for sync on Unlock()
					shadowUpdated = true;
				}

				data = shadowBuffer.Lock(offset, length, locking);
			}
			else
			{
				// lock the real deal and flag it as locked
				data = this.LockImpl(offset, length, locking);
				isLocked = true;
			}

			lockStart = offset;
			lockSize = length;

			return data;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="offset"></param>
		/// <param name="length"></param>
		/// <param name="locking"></param>
		/// <returns></returns>
		/// DOC
		protected abstract IntPtr LockImpl(int offset, int length, BufferLocking locking);

		/// <summary>
		///		Must be called after a call to <code>Lock</code>.  Unlocks the vertex buffer in the hardware
		///		memory.
		/// </summary>
		public virtual void Unlock()
		{
			Debug.Assert(isLocked, "Cannot unlock this buffer if it isn't locked to begin with.");

			if(useShadowBuffer && shadowBuffer.IsLocked)
			{
				shadowBuffer.Unlock();

				// potentially update the real buffer from the shadow buffer
				UpdateFromShadow();
			}
			else
			{
				// unlock the real deal
				this.UnlockImpl();

				isLocked = false;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// DOC
		public abstract void UnlockImpl();

		/// <summary>
		/// 
		/// </summary>
		/// DOC
		protected void UpdateFromShadow()
		{
			if(useShadowBuffer && shadowUpdated)
			{
				// do this manually to avoid locking problems
				IntPtr src = shadowBuffer.LockImpl(lockStart, lockSize, BufferLocking.ReadOnly);
				IntPtr dest = this.LockImpl(lockStart, lockSize, BufferLocking.Discard);

				// copy the src to the dest
				//Array.Copy(src, dest, lockSize);

				PointerCopy(src, dest, sizeInBytes);

				// unlock both buffers to commit the write
				this.UnlockImpl();
				shadowBuffer.UnlockImpl();

				shadowUpdated = false;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="offset"></param>
		/// <param name="length"></param>
		/// <param name="dest"></param>
		/// DOC
		public abstract void ReadData(int offset, int length, IntPtr dest);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="offset"></param>
		/// <param name="length"></param>
		/// <param name="src"></param>
		/// DOC
		public void WriteData(int offset, int length, IntPtr src)
		{
			WriteData(offset, length, src, false);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="offset"></param>
		/// <param name="length"></param>
		/// <param name="src"></param>
		/// <param name="discardWholeBuffer"></param>
		/// DOC
		public abstract void WriteData(int offset, int length, IntPtr src, bool discardWholeBuffer);

		/// <summary>
		///		Overloaded method.
		/// </summary>
		/// <param name="srcBuffer"></param>
		/// <param name="srcOffset"></param>
		/// <param name="destOffset"></param>
		/// <param name="length"></param>
		public virtual void CopyData(HardwareBuffer srcBuffer, int srcOffset, int destOffset, int length)
		{
			// call the overloaded method
			CopyData(srcBuffer, srcOffset, destOffset, length, false);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="srcBuffer"></param>
		/// <param name="srcOffset"></param>
		/// <param name="destOffset"></param>
		/// <param name="length"></param>
		/// <param name="discardWholeBuffer"></param>
		/// DOC
		public virtual void CopyData(HardwareBuffer srcBuffer, int srcOffset, int destOffset, int length, bool discardWholeBuffer)
		{
			// lock the source buffer
			IntPtr srcData = srcBuffer.Lock(srcOffset, length, BufferLocking.ReadOnly);

			// write the data to this buffer
			this.WriteData(destOffset, length, srcData, discardWholeBuffer);

			// unlock the source buffer
			srcBuffer.Unlock();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="src"></param>
		/// <param name="dest"></param>
		/// <param name="length"></param>
		public static unsafe void PointerCopy(IntPtr src, IntPtr dest, int length)
		{
			byte* pSrc = (byte*)src.ToPointer();
			byte* pDest = (byte*)dest.ToPointer();

			for(int i = 0; i < length; i++)
				*pDest = *pSrc;
		}

		#endregion

		#region Properties

		/// <summary>
		///		Gets whether or not this buffer is currently locked.
		/// </summary>
		public bool IsLocked 
		{ 
			get { return isLocked || (useShadowBuffer && shadowBuffer.IsLocked); } 
		}

		/// <summary>
		///		Gets whether this buffer is held in system memory.
		/// </summary>
		public bool IsSystemMemory
		{
			get { return useSystemMemory; }
		}

		/// <summary>
		///		Gets the size (in bytes) for this buffer.
		/// </summary>
		public int Size 
		{ 
			get { return sizeInBytes; } 
		}

		/// <summary>
		///		Gets the usage 
		/// </summary>
		public BufferUsage Usage 
		{ 
			get { return usage; } 
		}

		#endregion
	}
}
