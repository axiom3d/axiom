#region LGPL License
/*
Axiom Game Engine Library
Copyright (C) 2003  Axiom Project Team

The overall design, and a majority of the core engine and rendering code 
contained within this library is a derivative of the open source Object Oriented 
Graphics Engine OGRE, which can be found at http://ogre.sourceforge.net.  
Many thanks to the OGRE team for maintaining such a high quality project.

This library is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 2.1 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public
License along with this library; if not, write to the Free Software
Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
*/
#endregion

using System;
using System.Runtime.InteropServices;
using Axiom.Graphics;
using Tao.OpenGl;

namespace Axiom.RenderSystems.OpenGL {
    /// <summary>
    /// 	Summary description for GLHardwareVertexBuffer.
    /// </summary>
    public class GLHardwareVertexBuffer : HardwareVertexBuffer {
        #region Member variables

        /// <summary>Saves the GL buffer ID for this buffer.</summary>
        private int bufferID;
		
        #endregion
		
        #region Constructors
		
        public GLHardwareVertexBuffer(int vertexSize, int numVertices, BufferUsage usage, bool useShadowBuffer)
            : base(vertexSize, numVertices, usage, false, useShadowBuffer) {

                Ext.glGenBuffersARB(1, out bufferID);

				if(bufferID == 0) {
					throw new Exception("Cannot create GL vertex buffer");
				}

                Ext.glBindBufferARB(Gl.GL_ARRAY_BUFFER_ARB, bufferID);

                // initialize this buffer.  we dont have data yet tho
                Ext.glBufferDataARB(Gl.GL_ARRAY_BUFFER_ARB, sizeInBytes, IntPtr.Zero, GLHelper.ConvertEnum(usage));
        }

        ~GLHardwareVertexBuffer() {
            Ext.glDeleteBuffersARB(1, ref bufferID);
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
        protected override IntPtr LockImpl(int offset, int length, BufferLocking locking) {
            int access = 0;

            if(isLocked)
                throw new Exception("Invalid attempt to lock an index buffer that has already been locked.");

            // bind this buffer
            Ext.glBindBufferARB(Gl.GL_ARRAY_BUFFER_ARB, bufferID);

            if(locking == BufferLocking.Discard) {
                // fixes issues with ATI cards
                //Ext.glBufferDataARB(Gl.GL_ARRAY_BUFFER_ARB, length, IntPtr.Zero, GLHelper.ConvertEnum(usage));

                // find out how we shall access this buffer
                access = (usage == BufferUsage.Dynamic || usage == BufferUsage.DynamicWriteOnly) ? 
                    Gl.GL_READ_WRITE_ARB : Gl.GL_WRITE_ONLY_ARB;
            }
            else if(locking == BufferLocking.ReadOnly) {
                if(usage == BufferUsage.WriteOnly ||
                    usage == BufferUsage.StaticWriteOnly ||
                    usage == BufferUsage.DynamicWriteOnly)
                    
					// TODO: Log this instead, don't throw an exception
					//throw new Exception("Invalid attempt to lock a write-only vertex buffer as read-only.");

                access = Gl.GL_READ_ONLY_ARB;
            }
            else if(locking == BufferLocking.Normal || locking == BufferLocking.NoOverwrite) {
                access = (usage == BufferUsage.Dynamic || usage == BufferUsage.DynamicWriteOnly) ?
                    Gl.GL_READ_WRITE_ARB : Gl.GL_WRITE_ONLY_ARB;
            }

            IntPtr ptr = Ext.glMapBufferARB(Gl.GL_ARRAY_BUFFER_ARB, access);

            if(ptr == IntPtr.Zero)
                throw new Exception("GL Vertex Buffer: Out of memory");

            isLocked = true;

            return new IntPtr(ptr.ToInt32() + offset);
        }

        /// <summary>
        /// 
        /// </summary>
        public override void UnlockImpl() {
            Ext.glBindBufferARB(Gl.GL_ARRAY_BUFFER_ARB, bufferID);

            // Unmap the buffer to unlock it
            Ext.glUnmapBufferARB(Gl.GL_ARRAY_BUFFER_ARB);

            isLocked = false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <param name="src"></param>
        /// <param name="discardWholeBuffer"></param>
        public override void WriteData(int offset, int length, IntPtr src, bool discardWholeBuffer) {
            Ext.glBindBufferARB(Gl.GL_ARRAY_BUFFER_ARB, bufferID);

            if(discardWholeBuffer) {
                Ext.glBufferDataARB(Gl.GL_ARRAY_BUFFER_ARB, sizeInBytes, IntPtr.Zero, GLHelper.ConvertEnum(usage));
            }

            Ext.glBufferSubDataARB(Gl.GL_ARRAY_BUFFER_ARB, offset, length, src);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <param name="dest"></param>
        public override void ReadData(int offset, int length, IntPtr dest) {
//            Ext.glBindBufferARB(Gl.GL_ARRAY_BUFFER_ARB, bufferID);
//
//            Ext.glGetBufferSubDataARB(Gl.GL_ARRAY_BUFFER_ARB, offset, length, dest);

            // lock the buffer for reading
            IntPtr src = this.Lock(offset, length, BufferLocking.ReadOnly);
			
            // copy that data in there
            PointerCopy(src, dest, length);

            // unlock the buffer
            this.Unlock();
        }


        #endregion
		
        #region Properties

        /// <summary>
        ///		Gets the GL buffer ID for this buffer.
        /// </summary>
        public int GLBufferID {
            get { return bufferID; }
        }
		
        #endregion
    }
}
