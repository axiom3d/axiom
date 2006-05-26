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

#region Namespace Declarations

using System;
using System.Runtime.InteropServices;

using Axiom;

using DX = Microsoft.DirectX;
using D3D = Microsoft.DirectX.Direct3D;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.DirectX9
{
    /// <summary>
    /// 	Summary description for D3DHardwareIndexBuffer.
    /// </summary>
    public class D3DHardwareIndexBuffer : HardwareIndexBuffer
    {
        #region Member variables

        protected D3D.Device device;
        protected D3D.IndexBuffer d3dBuffer;
        protected System.Array data;

        #endregion

        #region Constructors

        public D3DHardwareIndexBuffer( IndexType type, int numIndices, BufferUsage usage,
            D3D.Device device, bool useSystemMemory, bool useShadowBuffer )
            : base( type, numIndices, usage, useSystemMemory, useShadowBuffer )
        {

            bool is16bitbuffer = ( type == IndexType.Size16 );

            // create the buffer
            d3dBuffer = new D3D.IndexBuffer(
                device,
                sizeInBytes,
                D3DHelper.ConvertEnum( usage ),
                useSystemMemory ? D3D.Pool.SystemMemory : D3D.Pool.Default,
                is16bitbuffer,
                null );
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
            D3D.LockFlags d3dLocking = 0;

            if ( ( usage & BufferUsage.Dynamic ) == 0 &&
                //usage != BufferUsage.DynamicWriteOnly &&
                ( locking == BufferLocking.Discard ) )
            {

                // lock flags already 0 by default
            }
            else
            {
                // D3D doesnt like disard or no overrwrite on non dynamic buffers
                d3dLocking = D3DHelper.ConvertEnum( locking );
            }

            DX.GraphicsBuffer s = d3dBuffer.Lock( offset, length, d3dLocking );
            return s.DataBuffer;
        }

        /// <summary>
        /// 
        /// </summary>
        /// DOC
        public override void UnlockImpl()
        {
            // unlock the buffer
            d3dBuffer.Unlock();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <param name="dest"></param>
        /// DOC
        public override void ReadData( int offset, int length, IntPtr dest )
        {
            // lock the buffer for reading
            IntPtr src = this.Lock( offset, length, BufferLocking.ReadOnly );

            // copy that data in there
            Memory.Copy( src, dest, length );

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
        /// DOC
        public override void WriteData( int offset, int length, IntPtr src, bool discardWholeBuffer )
        {
            // lock the buffer real quick
            IntPtr dest = this.Lock( offset, length,
                discardWholeBuffer ? BufferLocking.Discard : BufferLocking.Normal );

            // copy that data in there
            Memory.Copy( src, dest, length );

            // unlock the buffer
            this.Unlock();
        }

        public override void Dispose()
        {
            d3dBuffer.Dispose();
        }

        #endregion

        #region Properties

        /// <summary>
        ///		Gets the underlying D3D Vertex Buffer object.
        /// </summary>
        public D3D.IndexBuffer D3DIndexBuffer
        {
            get
            {
                return d3dBuffer;
            }
        }

        #endregion
    }
}
