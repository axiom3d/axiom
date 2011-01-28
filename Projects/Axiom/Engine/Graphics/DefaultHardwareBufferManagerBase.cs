using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Axiom.Graphics
{
	/** Specialization of HardwareBufferManagerBase to emulate hardware buffers.
	@remarks
		You might want to instantiate this class if you want to utilize
		classes like MeshSerializer without having initialized the 
		rendering system (which is required to create a 'real' hardware
		buffer manager.
	*/
	public class DefaultHardwareBufferManagerBase : HardwareBufferManagerBase
	{
		public DefaultHardwareBufferManagerBase()
		{
		}

		~DefaultHardwareBufferManagerBase()
		{
		}

		/// Creates a vertex buffer
		public override HardwareVertexBuffer CreateVertexBuffer( int vertexSize, int numVerts, BufferUsage usage, bool useShadowBuffer )
		{
			DefaultHardwareVertexBuffer vb = new DefaultHardwareVertexBuffer( this, vertexSize, numVerts, usage );
			return vb;
		}

		/// Create a hardware vertex buffer
		public override HardwareIndexBuffer CreateIndexBuffer( IndexType itype, int numIndices, BufferUsage usage, bool useShadowBuffer )
		{
			DefaultHardwareIndexBuffer ib = new DefaultHardwareIndexBuffer( itype, numIndices, usage );
			return ib;
		}

		/// Create a hardware vertex buffer
		//RenderToVertexBuffer createRenderToVertexBuffer();

		protected override void dispose( bool disposeManagedResources )
		{
			if ( disposeManagedResources )
			{
				//DisposeAllDeclarations();
				//DisposeAllBindings();
			}
			base.dispose( disposeManagedResources );
		}
	};
}
