using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Axiom.Graphics;
using GL = OpenTK.Graphics.ES20.GL;
using GLenum = OpenTK.Graphics.ES20.All;

namespace Axiom.RenderSystems.OpenGLES2
{
    class GLES2HardwareBufferManagerBase : HardwareBufferManagerBase
    {
        string scratchBufferPool;
        object scratchMutex;
        int mapBufferThreshold;

        public GLES2HardwareBufferManagerBase()
        { }
        public override HardwareVertexBuffer CreateVertexBuffer(VertexDeclaration vertexDeclaration, int numVerts, BufferUsage usage, bool useShadowBuffer)
        {
            throw new NotImplementedException();
        }

        public override HardwareIndexBuffer CreateIndexBuffer(IndexType type, int numIndices, BufferUsage usage, bool useShadowBuffer)
        {
            
            throw new NotImplementedException();
        }

        public static GLenum GetGLUsage(int usage)
        { }
        public static GLenum GetGLType(int type)
        { }

        public void AllocateScratch(int size)
        { }
        public void DeallocateScratch(IntPtr ptr)
        { }

        public int GLMapBufferThreshold
        {
            get;
            set;
        }
       
        
    }
    class GLES2HardwareBufferManager : HardwareBufferManager
    {
        static GLES2HardwareBufferManager _instance = null;

        public GLES2HardwareBufferManager()
            : base(new GLES2HardwareBufferManagerBase())
        {
        }
        protected override void dispose(bool disposeManagedResources)
        {
            _instance = null;
            base.dispose(disposeManagedResources);
        }

        public static GLenum GetGLUsage(int usage)
        {
            return GLES2HardwareBufferManagerBase.GetGLUsage(usage);
        }
        public static GLenum GetGLType(int type)
        {
            return GLES2HardwareBufferManagerBase.GetGLType(type);
        }
        /// <summary>
        /// Allows us to use a pool of memory as a scratch 
        /// area for hardware buffers. This is because GL.MapBuffer is incredibly inefficient,
        /// seemingly no matter what options we give it. So for the period of lock/unlock, we will instead allocate a section of a local memory pool, and use GL.BufferSubDataARB / GL.GetBufferSubDataARB instead.
        /// </summary>
        /// <param name="size"></param>
        public void AllocateScratch(int size)
        { }
    }
}
