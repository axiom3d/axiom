#region LGPL License

/*
Axiom Graphics Engine Library
Copyright © 2003-2011 Axiom Project Team

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

#endregion LGPL License

#region SVN Version Information

// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using Axiom.Core;
using Axiom.Graphics;
using Tao.OpenGl;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGL
{
    /// <summary>
    ///   Summary description for GLHardwareIndexBuffer.
    /// </summary>
    public class GLHardwareIndexBuffer : HardwareIndexBuffer
    {
        /// <summary>
        ///   Saves the GL buffer ID for this buffer.
        /// </summary>
        private int _bufferId;

        ///<summary>
        ///  Gets the GL buffer ID for this buffer.
        ///</summary>
        public int GLBufferID
        {
            get
            {
                return this._bufferId;
            }
        }

        /// <summary>
        ///   Constructor.
        /// </summary>
        /// <param name="manager"> the HardwareBufferManager instance that created this buffer. </param>
        /// <param name="type"> Index type (16 or 32 bit). </param>
        /// <param name="numIndices"> Number of indices in the buffer. </param>
        /// <param name="usage"> Usage flags. </param>
        /// <param name="useShadowBuffer"> Should this buffer be backed by a software shadow buffer? </param>
        public GLHardwareIndexBuffer(HardwareBufferManagerBase manager, IndexType type, int numIndices, BufferUsage usage,
                                      bool useShadowBuffer)
            : base(manager, type, numIndices, usage, false, useShadowBuffer)
        {
            // generate the buffer
            Gl.glGenBuffersARB(1, out this._bufferId);

            if (this._bufferId == 0)
            {
                throw new Exception("OGL: Cannot create GL index buffer");
            }

            Gl.glBindBufferARB(Gl.GL_ELEMENT_ARRAY_BUFFER_ARB, this._bufferId);

            // initialize this buffer.  we dont have data yet tho
            Gl.glBufferDataARB(Gl.GL_ELEMENT_ARRAY_BUFFER_ARB, new IntPtr(sizeInBytes), IntPtr.Zero,
                                GLHelper.ConvertEnum(usage));
            LogManager.Instance.Write("OGL: Created IndexBuffer[{0}].", this._bufferId);
        }

        /// <summary>
        ///   Called to destroy this buffer.
        /// </summary>
        protected override void dispose(bool disposeManagedResources)
        {
            if (!IsDisposed)
            {
                if (disposeManagedResources)
                {
                }

                try
                {
                    Gl.glDeleteBuffersARB(1, ref this._bufferId);
                    LogManager.Instance.Write("OGL: Deleted IndexBuffer[{0}].", this._bufferId);
                }
                catch (AccessViolationException ave)
                {
                    LogManager.Instance.Write("OGL: Failed to delete IndexBuffer[{0}].", this._bufferId);
                }
            }

            // If it is available, make the call to the
            // base class's Dispose(Boolean) method
            base.dispose(disposeManagedResources);
        }

        ///<summary>
        ///</summary>
        ///<param name="offset"> </param>
        ///<param name="length"> </param>
        ///<param name="locking"> </param>
        ///<returns> </returns>
        protected override BufferBase LockImpl(int offset, int length, BufferLocking locking)
        {
            int access = 0;

            if (isLocked)
            {
                throw new Exception("Invalid attempt to lock an index buffer that has already been locked.");
            }

            // bind this buffer
            Gl.glBindBufferARB(Gl.GL_ELEMENT_ARRAY_BUFFER_ARB, this._bufferId);

            if (locking == BufferLocking.Discard)
            {
                // commented out to fix ATI issues
                /*Gl.glBufferDataARB(Gl.GL_ELEMENT_ARRAY_BUFFER_ARB,
					 sizeInBytes,
					 IntPtr.Zero,
					 GLHelper.ConvertEnum(usage));
				 */

                // find out how we shall access this buffer
                access = (usage == BufferUsage.Dynamic) ? Gl.GL_READ_WRITE_ARB : Gl.GL_WRITE_ONLY_ARB;
            }
            else if (locking == BufferLocking.ReadOnly)
            {
                if (usage == BufferUsage.WriteOnly)
                {
                    LogManager.Instance.Write("OGL : Invalid attempt to lock a write-only vertex buffer as read-only.");
                }

                access = Gl.GL_READ_ONLY_ARB;
            }
            else if (locking == BufferLocking.Normal || locking == BufferLocking.NoOverwrite)
            {
                access = (usage == BufferUsage.Dynamic) ? Gl.GL_READ_WRITE_ARB : Gl.GL_WRITE_ONLY_ARB;
            }

            IntPtr ptr = Gl.glMapBufferARB(Gl.GL_ELEMENT_ARRAY_BUFFER_ARB, access);

            if (ptr == IntPtr.Zero)
            {
                throw new Exception("OGL: Vertex Buffer: Out of memory");
            }

            isLocked = true;

            return BufferBase.Wrap(new IntPtr(ptr.ToInt64() + offset), length);
        }

        ///<summary>
        ///</summary>
        protected override void UnlockImpl()
        {
            Gl.glBindBufferARB(Gl.GL_ELEMENT_ARRAY_BUFFER_ARB, this._bufferId);

            if (Gl.glUnmapBufferARB(Gl.GL_ELEMENT_ARRAY_BUFFER_ARB) == 0)
            {
                throw new Exception("OGL: Buffer data corrupted!");
            }

            isLocked = false;
        }

        ///<summary>
        ///</summary>
        ///<param name="offset"> </param>
        ///<param name="length"> </param>
        ///<param name="src"> </param>
        ///<param name="discardWholeBuffer"> </param>
        public override void WriteData(int offset, int length, BufferBase src, bool discardWholeBuffer)
        {
            Gl.glBindBufferARB(Gl.GL_ELEMENT_ARRAY_BUFFER_ARB, this._bufferId);

            if (useShadowBuffer)
            {
                // lock the buffer for reading
                var dest = shadowBuffer.Lock(offset, length, discardWholeBuffer ? BufferLocking.Discard : BufferLocking.Normal);

                // copy that data in there
                Memory.Copy(src, dest, length);

                // unlock the buffer
                shadowBuffer.Unlock();
            }

            if (discardWholeBuffer)
            {
                Gl.glBufferDataARB(Gl.GL_ELEMENT_ARRAY_BUFFER_ARB, new IntPtr(sizeInBytes), IntPtr.Zero,
                                    GLHelper.ConvertEnum(usage));
            }

            Gl.glBufferSubDataARB(Gl.GL_ELEMENT_ARRAY_BUFFER_ARB, new IntPtr(offset), new IntPtr(length), src.Pin());
            src.UnPin();
        }

        ///<summary>
        ///</summary>
        ///<param name="offset"> </param>
        ///<param name="length"> </param>
        ///<param name="dest"> </param>
        public override void ReadData(int offset, int length, BufferBase dest)
        {
            if (useShadowBuffer)
            {
                // lock the buffer for reading
                var src = shadowBuffer.Lock(offset, length, BufferLocking.ReadOnly);

                // copy that data in there
                Memory.Copy(src, dest, length);

                // unlock the buffer
                shadowBuffer.Unlock();
            }
            else
            {
                Gl.glBindBufferARB(Gl.GL_ELEMENT_ARRAY_BUFFER_ARB, this._bufferId);
                Gl.glGetBufferSubDataARB(Gl.GL_ELEMENT_ARRAY_BUFFER_ARB, new IntPtr(offset), new IntPtr(length), dest.Pin());
                dest.UnPin();
            }
        }
    }
}