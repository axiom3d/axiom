using System;

namespace Axiom.SubSystems.Rendering
{
	/// <summary>
	/// 	Summary description for VertexIndexData.
	/// </summary>
	public class VertexData : ICloneable
	{
		#region Member variables
		
		public VertexDeclaration vertexDeclaration;
		public VertexBufferBinding vertexBufferBinding;
		public int vertexStart;
		public int vertexCount;

		#endregion

		public VertexData()
		{
			vertexDeclaration = HardwareBufferManager.Instance.CreateVertexDeclaration();
			vertexBufferBinding = HardwareBufferManager.Instance.CreateVertexBufferBinding();
		}

		#region ICloneable Members

		public object Clone()
		{
			// TODO:  Add VertexData.Clone implementation
			return null;
		}

		#endregion
	}

	/// <summary>
	/// 	Summary description for VertexIndexData.
	/// </summary>
	public class IndexData : ICloneable
	{
		#region Member variables

		public HardwareIndexBuffer indexBuffer;
		public int indexStart;
		public int indexCount;
		
		#endregion

		#region ICloneable Members

		public object Clone()
		{
			// TODO:  Add IndexData.Clone implementation
			return null;
		}

		#endregion
	}
}
