using System;
using Axiom.SubSystems.Rendering;

namespace RenderSystem_OpenGL
{
	/// <summary>
	/// 	Summary description for GLHardwareBufferManager.
	/// </summary>
	public class GLHardwareBufferManager : HardwareBufferManager
	{
		#region Member variables
		
		#endregion
		
		#region Constructors
		
		public GLHardwareBufferManager()
		{
		}
		
		#endregion
		
		#region Methods

		/// <summary>
		/// 
		/// </summary>
		/// <param name="type"></param>
		/// <param name="numIndices"></param>
		/// <param name="usage"></param>
		/// <returns></returns>
		public override HardwareIndexBuffer CreateIndexBuffer(IndexType type, int numIndices, BufferUsage usage)
		{
			return CreateIndexBuffer(type, numIndices, usage, false);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="type"></param>
		/// <param name="numIndices"></param>
		/// <param name="usage"></param>
		/// <param name="useShadowBuffer"></param>
		/// <returns></returns>
		public override HardwareIndexBuffer CreateIndexBuffer(IndexType type, int numIndices, BufferUsage usage, bool useShadowBuffer)
		{
			return new GLHardwareIndexBuffer(type, numIndices, usage, useShadowBuffer);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="vertexSize"></param>
		/// <param name="numVerts"></param>
		/// <param name="usage"></param>
		/// <returns></returns>
		public override HardwareVertexBuffer CreateVertexBuffer(int vertexSize, int numVerts, BufferUsage usage)
		{
			return CreateVertexBuffer(vertexSize, numVerts, usage, false);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="vertexSize"></param>
		/// <param name="numVerts"></param>
		/// <param name="usage"></param>
		/// <param name="useShadowBuffer"></param>
		/// <returns></returns>
		public override HardwareVertexBuffer CreateVertexBuffer(int vertexSize, int numVerts, BufferUsage usage, bool useShadowBuffer)
		{
			return new GLHardwareVertexBuffer(vertexSize, numVerts, usage, useShadowBuffer);
		}

		
		#endregion
		
		#region Properties
		
		#endregion

	}
}
