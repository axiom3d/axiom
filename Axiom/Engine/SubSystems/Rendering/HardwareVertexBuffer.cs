using System;
using Axiom.MathLib;

namespace Axiom.SubSystems.Rendering
{
	/// <summary>
	///		Describes the graphics API independent functionality required by a hardware
	///		vertex buffer.  
	/// </summary>
	/// <remarks>
	///		
	/// </remarks>
	public abstract class HardwareVertexBuffer : HardwareBuffer
	{

		#region Member variables

		protected int numVertices;
		protected int vertexSize;

		#endregion

		#region Constructors
		
		public HardwareVertexBuffer(int vertexSize, int numVertices, BufferUsage usage, bool useSystemMemory, bool useShadowBuffer) 
			: base(usage, useSystemMemory, useShadowBuffer)
		{	
			this.vertexSize = vertexSize;
			this.numVertices = numVertices;

			// calculate the size in bytes of this buffer
			sizeInBytes = vertexSize * numVertices;

			// create a shadow buffer if required
			if(useShadowBuffer)
			{
				shadowBuffer = new SoftwareVertexBuffer(vertexSize, numVertices, BufferUsage.Dynamic);
			}
		}
		
		#endregion

		#region Properties

		/// <summary>
		/// 
		/// </summary>
		/// DOC
		public int VertexSize
		{
			get { return vertexSize; }
		}

		public int VertexCount
		{
			get { return numVertices; }
		}

		#endregion
	}
}
