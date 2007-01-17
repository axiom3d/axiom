#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2006 Axiom Project Team

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

#region SVN Version Information
// <file>
//     <license see="http://axiomengine.sf.net/wiki/index.php/license.txt"/>
//     <id value="$Id:"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations
using System;
using System.Runtime.InteropServices;

using Axiom.Core;
using Axiom.Graphics;

using XNA = Microsoft.Xna.Framework;
using XFG = Microsoft.Xna.Framework.Graphics;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.Xna
{
    class XnaHardwareIndexBuffer: HardwareIndexBuffer
    {
        #region Member variables

        protected XFG.GraphicsDevice device;
        protected XFG.IndexBuffer xnaBuffer;
        private Byte[] _buffer;
        private GCHandle _gcHandle;

        #endregion

        #region Constructors

        public XnaHardwareIndexBuffer( IndexType type, int numIndices, BufferUsage usage,
            XFG.GraphicsDevice device, bool useSystemMemory, bool useShadowBuffer )
            : base( type, numIndices, usage, useSystemMemory, useShadowBuffer )
        {

            XFG.IndexElementSize elementSize = ( type == IndexType.Size16 ? XFG.IndexElementSize.SixteenBits : XFG.IndexElementSize.ThirtyTwoBits);

            // create the buffer
            xnaBuffer = new XFG.IndexBuffer( device, sizeInBytes, XnaHelper.ConvertEnum( usage ), elementSize );
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
        protected override IntPtr LockImpl( int offset, int length, BufferLocking locking )
        {
            _buffer = new Byte[ length ];
            xnaBuffer.GetData<Byte>( _buffer );
            return Marshal.UnsafeAddrOfPinnedArrayElement( _buffer, 0 );
            //_gcHandle = GCHandle.Alloc( xnaBuffer, GCHandleType.Weak );
            //return GCHandle.ToIntPtr( _gcHandle );
        }

        /// <summary>
        /// 
        /// </summary>
        /// DOC
        public override void UnlockImpl()
        {
            //if ( this.isLocked &&  _gcHandle.IsAllocated )
            //{
                xnaBuffer.SetData<Byte>( _buffer );
                //_gcHandle.Free();
                _buffer = null;
            //}
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <param name="dest"></param>
        public override void ReadData( int offset, int length, IntPtr dest )
        {
            // lock the buffer for reading
            IntPtr src = this.Lock(offset, length, BufferLocking.ReadOnly);

            // copy that data in there
            Memory.Copy(src, dest, length);

            // unlock the buffer
            this.Unlock();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <param name="src"></param>
        /// <param name="discardWholeBuffer"></param>
        public override void WriteData(int offset, int length, IntPtr src, bool discardWholeBuffer) { throw new NotImplementedException(); }
        public override void WriteData(int offset, int length, System.Array src, bool discardWholeBuffer)
        {
            switch ((src.GetValue(0)).GetType().Name)
            {
                case "Single":
                    xnaBuffer.SetData<float>((float[])src);
                    break;
                case "Vector3":
                    xnaBuffer.SetData<Axiom.Math.Vector3>((Axiom.Math.Vector3[])src);
                    break;
                case "Int32":
                    xnaBuffer.SetData<int>((int[])src);
                    break;
                case "Int16":
                    xnaBuffer.SetData<System.Int16>((System.Int16[])src);
                    break;
                default:
                    throw new AxiomException("XNA RenderSystem: Index Type {0} Not Supported.", (src.GetValue(0)).GetType().Name);
                    break;
            }

        }

        public override void Dispose()
        {
            xnaBuffer.Dispose();
        }

        #endregion

        #region Properties

        /// <summary>
        ///		Gets the underlying XNA Vertex Buffer object.
        /// </summary>
        public XFG.IndexBuffer XnaIndexBuffer
        {
            get
            {
                return xnaBuffer;
            }
        }

        #endregion
    }
}
