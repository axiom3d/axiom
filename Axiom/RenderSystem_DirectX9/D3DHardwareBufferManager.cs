using System;
using Microsoft.DirectX.Direct3D;
using D3D = Microsoft.DirectX.Direct3D;
using Axiom.SubSystems.Rendering;
using VertexDeclaration = Axiom.SubSystems.Rendering.VertexDeclaration;

namespace RenderSystem_DirectX9
{
	/// <summary>
	/// 	Summary description for D3DHardwareBufferManager.
	/// </summary>
	public class D3DHardwareBufferManager : HardwareBufferManager 
	{
		#region Member variables

		protected D3D.Device device;
		
		#endregion
		
		#region Constructors
		
		/// <summary>
		///		
		/// </summary>
		/// <param name="device"></param>
		public D3DHardwareBufferManager(D3D.Device device)
		{
			this.device = device;


		}
		
		#endregion
		
		#region Methods
		
		public override Axiom.SubSystems.Rendering.HardwareIndexBuffer CreateIndexBuffer(IndexType type, int numIndices, BufferUsage usage)
		{
			// call overloaded method with no shadow buffer
			return CreateIndexBuffer(type, numIndices, usage, false);
		}

		public override Axiom.SubSystems.Rendering.HardwareIndexBuffer CreateIndexBuffer(IndexType type, int numIndices, BufferUsage usage, bool useShadowBuffer)
		{
			return new D3DHardwareIndexBuffer(type, numIndices, usage, device, false, useShadowBuffer);
		}

		public override HardwareVertexBuffer CreateVertexBuffer(int vertexSize, int numVerts, BufferUsage usage)
		{
			// call overloaded method with no shadow buffer
			return CreateVertexBuffer(vertexSize, numVerts, usage, false);
		}

		public override HardwareVertexBuffer CreateVertexBuffer(int vertexSize, int numVerts, BufferUsage usage, bool useShadowBuffer)
		{
			return new D3DHardwareVertexBuffer(vertexSize, numVerts, usage, device, false, useShadowBuffer);
		}

		public override Axiom.SubSystems.Rendering.VertexDeclaration CreateVertexDeclaration()
		{
			VertexDeclaration decl = new D3DVertexDeclaration(device);
			vertexDeclarations.Add(decl);
			return decl;
		}

		// TODO: Disposal

		#endregion
		
		#region Properties
		
		#endregion

	}
}
