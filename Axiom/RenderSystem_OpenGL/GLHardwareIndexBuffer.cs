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
using Axiom.SubSystems.Rendering;
using Tao.OpenGl;
using CsGL.OpenGL;

namespace RenderSystem_OpenGL {
    /// <summary>
    /// 	Summary description for GLHardwareIndexBuffer.
    /// </summary>
    public class GLHardwareIndexBuffer : HardwareIndexBuffer {
        #region Member variables
		
        /// <summary>Saves the GL buffer ID for this buffer.</summary>
        private uint bufferID;

        private Ext Ext = new Ext();

        #endregion
		
        #region Constructors
		
        public GLHardwareIndexBuffer(IndexType type, int numIndices, BufferUsage usage, bool useShadowBuffer) 
            : base(type, numIndices, usage, false, useShadowBuffer) {
            unsafe {
                // generate the texture
                fixed(uint* pBufferID = &bufferID)
                    Ext.glGenBuffersARB(1, pBufferID);

                if(bufferID == 0)
                    throw new Exception("Cannot create GL vertex buffer");

                Ext.glBindBufferARB(Gl.GL_ARRAY_BUFFER_ARB, bufferID);

                // initialize this buffer.  we dont have data yet tho
                Ext.glBufferDataARB(
                    Gl.GL_ARRAY_BUFFER_ARB, 
                    (uint)sizeInBytes, 
                    null, 
                    (uint)GLHelper.ConvertEnum(usage));
            }
        }

        ~GLHardwareIndexBuffer() {
            unsafe {
                fixed(uint* pBufferID = &bufferID)
                    Ext.glDeleteBuffersARB(1, pBufferID);
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
        protected override IntPtr LockImpl(int offset, int length, BufferLocking locking) {
            int access = 0;

            if(isLocked)
                throw new Exception("Invalid attempt to lock an index buffer that has already been locked.");

            unsafe {
                // bind this buffer
                Ext.glBindBufferARB(Gl.GL_ELEMENT_ARRAY_BUFFER_ARB, bufferID);

                if(locking == BufferLocking.Discard) {
                    Ext.glBufferDataARB(Gl.GL_ELEMENT_ARRAY_BUFFER_ARB,
                        (uint)sizeInBytes,
                        null,
                        (uint)GLHelper.ConvertEnum(usage));

                    // find out how we shall access this buffer
                    access = (usage & BufferUsage.Dynamic) > 0 ? 
                        Gl.GL_READ_WRITE_ARB : Gl.GL_WRITE_ONLY_ARB;
                }
                else if(locking == BufferLocking.ReadOnly) {
                    if((usage & BufferUsage.WriteOnly) > 0)
                        throw new Exception("Invalid attempt to lock a write-only vertex buffer as read-only.");

                    access = Gl.GL_READ_ONLY_ARB;
                }
                else if (locking == BufferLocking.Normal) {
                    access = ((usage & BufferUsage.Dynamic) > 0) ?
                        Gl.GL_READ_WRITE_ARB : Gl.GL_READ_ONLY_ARB;
                }

                IntPtr ptr = Ext.glMapBufferARB(Gl.GL_ELEMENT_ARRAY_BUFFER_ARB, (uint)access);

                if(ptr == IntPtr.Zero)
                    throw new Exception("GL Vertex Buffer: Out of memory");

                isLocked = true;

                return ptr;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public override void UnlockImpl() {
            Ext.glBindBufferARB(Gl.GL_ELEMENT_ARRAY_BUFFER_ARB, bufferID);

            // TODO: Remap CsGL to return a val from this method (bool)
            Ext.glUnmapBufferARB(Gl.GL_ELEMENT_ARRAY_BUFFER_ARB);

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
            Ext.glBindBufferARB(Gl.GL_ELEMENT_ARRAY_BUFFER_ARB, bufferID);

            unsafe {
                if(discardWholeBuffer) {
                    Ext.glBufferDataARB(Gl.GL_ELEMENT_ARRAY_BUFFER_ARB,
                        (uint)sizeInBytes,
                        null,
                        (uint)GLHelper.ConvertEnum(usage));
                }

                // TODO: Map CsGL to accept a IntPtr instead of a void* ?
                Ext.glBufferSubDataARB(
                    Gl.GL_ELEMENT_ARRAY_BUFFER_ARB, 
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
        public override void ReadData(int offset, int length, IntPtr dest) {
            Ext.glBindBufferARB(Gl.GL_ELEMENT_ARRAY_BUFFER_ARB, bufferID);

            unsafe {
                // TODO: Map CsGL to accept a IntPtr instead of void* ?
                Ext.glGetBufferSubDataARB(
                    Gl.GL_ELEMENT_ARRAY_BUFFER_ARB, 
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
        public uint GLBufferID {
            get { return bufferID; }
        }
		
        #endregion
    }
}
