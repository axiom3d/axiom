using System;
using System.Runtime.InteropServices;
using Axiom.SubSystems.Rendering;
using Ext = RenderSystem_OpenGL.OpenGLExtensions;

namespace RenderSystem_OpenGL
{
	/// <summary>
	/// 	Summary description for GLHardwareVertexBuffer.
	/// </summary>
	public class GLHardwareVertexBuffer : HardwareVertexBuffer
	{
		#region Member variables

		/// <summary>Saves the GL buffer ID for this buffer.</summary>
		private uint bufferID;

		private Ext ext = new Ext();
		
		#endregion
		
		#region Constructors
		
		public GLHardwareVertexBuffer(int vertexSize, int numVertices, BufferUsage usage, bool useShadowBuffer)
			: base(vertexSize, numVertices, usage, false, useShadowBuffer)
		{
			unsafe
			{
				// generate the texture
				fixed(uint* pBufferID = &bufferID)
					ext.glGenBuffersARB(1, pBufferID);

				if(bufferID == 0)
					throw new Exception("Cannot create GL vertex buffer");

				ext.glBindBufferARB(OpenGLExtensions.GL_ARRAY_BUFFER_ARB, bufferID);

				// initialize this buffer.  we dont have data yet tho
				ext.glBufferDataARB(
					OpenGLExtensions.GL_ARRAY_BUFFER_ARB, 
					(uint)sizeInBytes, 
					null, 
					GLHelper.ConvertEnum(usage));
			}
		}

		~GLHardwareVertexBuffer()
		{
			unsafe
			{
				fixed(uint* pBufferID = &bufferID)
					ext.glDeleteBuffersARB(1, pBufferID);
			}
		}
		
		#endregion
		
		#region Methods

		/// <summary>
		/// 
		/// </summary>
		/// <param name="offset"></param>
		/// <param name="length"></param>
		/// <param name="locking"></param>
		/// <returns></returns>
		protected override IntPtr LockImpl(int offset, int length, BufferLocking locking)
		{
			uint access = 0;

			if(isLocked)
				throw new Exception("Invalid attempt to lock an index buffer that has already been locked.");

			unsafe
			{
				// bind this buffer
				ext.glBindBufferARB(OpenGLExtensions.GL_ARRAY_BUFFER_ARB, bufferID);

				if(locking == BufferLocking.Discard)
				{
					ext.glBufferDataARB(OpenGLExtensions.GL_ARRAY_BUFFER_ARB,
						(uint)sizeInBytes,
						null,
						GLHelper.ConvertEnum(usage));

					// find out how we shall access this buffer
					access = (usage & BufferUsage.Dynamic) > 0 ? 
						OpenGLExtensions.GL_READ_WRITE_ARB : OpenGLExtensions.GL_WRITE_ONLY_ARB;
				}
				else if(locking == BufferLocking.ReadOnly)
				{
					if((usage & BufferUsage.WriteOnly) > 0)
						throw new Exception("Invalid attempt to lock a write-only vertex buffer as read-only.");

					access = OpenGLExtensions.GL_READ_ONLY_ARB;
				}
				else if (locking == BufferLocking.Normal)
				{
					access = ((usage & BufferUsage.Dynamic) > 0) ?
						OpenGLExtensions.GL_READ_WRITE_ARB : OpenGLExtensions.GL_READ_ONLY_ARB;
				}

				IntPtr ptr = ext.glMapBufferARB(OpenGLExtensions.GL_ARRAY_BUFFER_ARB, access);

				if(ptr == IntPtr.Zero)
					throw new Exception("GL Vertex Buffer: Out of memory");

				isLocked = true;

				return ptr;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public override void UnlockImpl()
		{
			ext.glBindBufferARB(OpenGLExtensions.GL_ARRAY_BUFFER_ARB, bufferID);

			// TODO: Remap CsGL to return a val from this method (bool)
			ext.glUnmapBufferARB(OpenGLExtensions.GL_ARRAY_BUFFER_ARB);

			isLocked = false;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="offset"></param>
		/// <param name="length"></param>
		/// <param name="src"></param>
		/// <param name="discardWholeBuffer"></param>
		public override void WriteData(int offset, int length, IntPtr src, bool discardWholeBuffer)
		{
			ext.glBindBufferARB(OpenGLExtensions.GL_ARRAY_BUFFER_ARB, bufferID);

			unsafe
			{
				if(discardWholeBuffer)
				{
					ext.glBufferDataARB(OpenGLExtensions.GL_ARRAY_BUFFER_ARB,
						(uint)sizeInBytes,
						null,
						GLHelper.ConvertEnum(usage));
				}

				// TODO: Map CsGL to accept a IntPtr instead of a void* ?
				ext.glBufferSubDataARB(
					OpenGLExtensions.GL_ARRAY_BUFFER_ARB, 
					(uint)offset, 
					(uint)length, 
					src.ToPointer());
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="offset"></param>
		/// <param name="length"></param>
		/// <param name="dest"></param>
		public override void ReadData(int offset, int length, IntPtr dest)
		{
			ext.glBindBufferARB(OpenGLExtensions.GL_ARRAY_BUFFER_ARB, bufferID);

			unsafe
			{
				// TODO: Map CsGL to accept a IntPtr instead of void* ?
				ext.glGetBufferSubDataARB(
					OpenGLExtensions.GL_ARRAY_BUFFER_ARB, 
					(uint)offset, 
					(uint)length, 
					dest.ToPointer());
			}
		}


		#endregion
		
		#region Properties

		/// <summary>
		///		Gets the GL buffer ID for this buffer.
		/// </summary>
		public uint GLBufferID
		{
			get { return bufferID; }
		}
		
		#endregion

	}
}
