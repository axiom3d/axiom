using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Axiom.Core;
using Axiom.Graphics;

using GL = OpenTK.Graphics.ES20.GL;
using GLenum = OpenTK.Graphics.ES20.All;

namespace Axiom.RenderSystems.OpenGLES2
{
	internal class GLES2HardwareBufferManagerBase : HardwareBufferManagerBase
	{
		private const int DefaultMapBufferThreshold = ( 1024 * 32 );
		private byte[] _scratchBufferPool;
		private object _scratchMutex;

		public GLES2HardwareBufferManagerBase()
		{
		}

		public override HardwareVertexBuffer CreateVertexBuffer( VertexDeclaration vertexDeclaration, int numVerts, BufferUsage usage, bool useShadowBuffer )
		{
			var vertexBuffer = new GLES2HardwareVertexBuffer( this, vertexDeclaration, numVerts, usage, true );
			lock ( VertexBuffersMutex )
			{
				vertexBuffers.Add( vertexBuffer );
			}
			return vertexBuffer;
		}

		public override HardwareIndexBuffer CreateIndexBuffer( IndexType type, int numIndices, BufferUsage usage, bool useShadowBuffer )
		{
			var indexBuffer = new GLES2HardwareIndexBuffer( this, type, numIndices, usage, true );
			lock ( IndexBuffersMutex )
			{
				indexBuffers.Add( indexBuffer );
			}
			return indexBuffer;
		}

		//public override RenderToVertexBuffer CreateRenderToVertexBuffer( )
		//{
		//    throw new NotImplementedException();
		//}

		public static GLenum GetGLUsage( BufferUsage usage )
		{
			switch ( usage )
			{
				case BufferUsage.Static:
				case BufferUsage.StaticWriteOnly:
					return GLenum.StaticDraw;
				case BufferUsage.Dynamic:
				case BufferUsage.DynamicWriteOnly:
					return GLenum.DynamicDraw;
				case BufferUsage.DynamicWriteOnlyDiscardable:
					return GLenum.StreamDraw;
				default:
					return GLenum.DynamicDraw;
			}
		}

		public static GLenum GetGLType( VertexElementType type )
		{
			switch ( type )
			{
				case VertexElementType.Float1:
				case VertexElementType.Float2:
				case VertexElementType.Float3:
				case VertexElementType.Float4:
					return GLenum.Float;
				case VertexElementType.Short1:
				case VertexElementType.Short2:
				case VertexElementType.Short3:
				case VertexElementType.Short4:
					return GLenum.Short;
				case VertexElementType.Color:
				case VertexElementType.Color_ARGB:
				case VertexElementType.Color_ABGR:
				case VertexElementType.UByte4:
					return GLenum.UnsignedByte;
				default:
					return 0;
			}
		}

		public BufferBase AllocateScratch( int size )
		{
			return null;
		}

		public void DeallocateScratch( BufferBase ptr ) {}

		public int MapBufferThreshold { get; set; }
	}

	internal class GLES2HardwareBufferManager : HardwareBufferManager
	{
		public GLES2HardwareBufferManager()
			: base( new GLES2HardwareBufferManagerBase() ) {}

		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
				if ( disposeManagedResources )
					_baseInstance.Dispose();

			base.dispose( disposeManagedResources );
		}

		public static GLenum GetGLUsage( BufferUsage usage )
		{
			return GLES2HardwareBufferManagerBase.GetGLUsage( usage );
		}

		public static GLenum GetGLType( VertexElementType type )
		{
			return GLES2HardwareBufferManagerBase.GetGLType( type );
		}

		/// <summary>
		/// Allows us to use a pool of memory as a scratch area for hardware buffers. 
		/// This is because GL.MapBuffer is incredibly inefficient, seemingly no matter 
		/// what options we give it. So for the period of lock/unlock, we will instead 
		/// allocate a section of a local memory pool, and use GL.BufferSubDataARB / GL.GetBufferSubDataARB instead.
		/// </summary>
		/// <param name="size"></param>
		public BufferBase AllocateScratch( int size )
		{
			return ( (GLES2HardwareBufferManagerBase) _baseInstance ).AllocateScratch( size );
		}

		public void DeallocateScratch( BufferBase ptr )
		{
			( (GLES2HardwareBufferManagerBase) _baseInstance ).DeallocateScratch( ptr );
		}

		public int MapBufferThreshold
		{
			get { return ( (GLES2HardwareBufferManagerBase) _baseInstance ).MapBufferThreshold; }
			set { ( (GLES2HardwareBufferManagerBase) _baseInstance ).MapBufferThreshold = value; }
		}
	}
}
