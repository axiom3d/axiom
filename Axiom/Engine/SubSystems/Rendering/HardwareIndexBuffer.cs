using System;

namespace Axiom.SubSystems.Rendering
{
	/// <summary>
	/// Summary description for IIndexBuffer.
	/// </summary>
	public abstract class HardwareIndexBuffer : HardwareBuffer
	{
		#region Member variables

		protected IndexType type;
		protected int numIndices;

		#endregion

		#region Constructors

		public HardwareIndexBuffer(IndexType type, int numIndices, BufferUsage usage, bool useSystemMemory, bool useShadowBuffer) 
			: base(usage, useSystemMemory, useShadowBuffer)
		{
			this.type = type;
			this.numIndices = numIndices;

			// calc the index buffer size
			sizeInBytes = numIndices;

			// unsafe block for sizeof
			unsafe
			{
				if(type == IndexType.Size32)
					sizeInBytes *= sizeof(int);
				else
					sizeInBytes *= sizeof(short);
			}
		}

		#endregion
	}
}
