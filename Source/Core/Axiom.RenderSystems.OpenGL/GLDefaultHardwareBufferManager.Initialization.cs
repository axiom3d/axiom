using Axiom.Graphics;

namespace Axiom.RenderSystems.OpenGL
{
	public class GLDefaultHardwareBufferManagerBase : HardwareBufferManagerBase
	{
		protected override void dispose(bool disposeManagedResources)
		{
			if ( disposeManagedResources )
			{
				DestroyAllDeclarations();
				DestroyAllBindings();
			}
			base.dispose(disposeManagedResources);
		}

		public override HardwareIndexBuffer CreateIndexBuffer(IndexType type, int numIndices, BufferUsage usage, bool useShadowBuffer)
		{
			return new GLDefaultHardwareIndexBuffer( type, numIndices, usage );
		}

		public override HardwareVertexBuffer CreateVertexBuffer(VertexDeclaration vertexDeclaration, int numVerts, BufferUsage usage, bool useShadowBuffer)
		{
			return new GLDefaultHardwareVertexBuffer( vertexDeclaration, numVerts, usage );
		}
	}

	public class GLDefaultHardwareBufferManager : HardwareBufferManager
	{
		public GLDefaultHardwareBufferManager()
			: base( new GLDefaultHardwareBufferManagerBase() )
		{
		}
	}
}