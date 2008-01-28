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
//     <id value="$Id: D3DHardwareIndexBuffer.cs 884 2006-09-14 06:32:07Z borrillis $"/>
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
    /// <summary>
    /// 	Summary description for XnaHardwareIndexBuffer.
    /// </summary>
    public class XnaHardwareIndexBuffer : HardwareIndexBuffer
    {
        #region Member variables

		protected XFG.GraphicsDevice device;
		protected XFG.IndexBuffer d3dBuffer;
        protected System.Array data;

        IntPtr bufferPtr;
        byte[] bufferBytes;
        int size;
		XFG.IndexElementSize bufferType;
        int indexSize;
        int Boffset;
        int Blenght;
#endregion

        #region Constructors

       unsafe public XnaHardwareIndexBuffer( IndexType type, int numIndices, BufferUsage usage,
		   XFG.GraphicsDevice device, bool useSystemMemory, bool useShadowBuffer )
            : base( type, numIndices, usage, useSystemMemory, useShadowBuffer )
        {

            bufferType = (type == IndexType.Size16) ? XFG.IndexElementSize.SixteenBits : XFG.IndexElementSize.ThirtyTwoBits;
          
            // create the buffer
			d3dBuffer = new XFG.IndexBuffer(
                device,
                sizeInBytes,XnaHelper.ConvertEnum( usage ),bufferType);
                
            size = sizeInBytes;
            bufferBytes = new byte[sizeInBytes];
            bufferBytes.Initialize();
            indexSize = (type == IndexType.Size16) ? indexSize = 4 : indexSize = 8;
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
       unsafe protected override IntPtr LockImpl( int offset, int length, BufferLocking locking )
       {
           Boffset = offset;
           Blenght = length;
           fixed ( byte* bytes =&bufferBytes[offset] )
           {
               return new IntPtr(bytes);
           }
        }

        /// <summary>
        /// 
        /// </summary>
        /// DOC
        public override void UnlockImpl()
        {
            //there is no unlock/lock system on XNA, just copy the byte buffer into the video card memory
            // d3dBuffer.SetData<byte>(bufferBytes);
            //this is a lot faster :)
            d3dBuffer.SetData<byte>(Boffset,bufferBytes, Boffset , Blenght);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <param name="dest"></param>
        /// DOC
        unsafe public override void ReadData( int offset, int length, IntPtr dest )
        {
            // lock the buffer for reading
            IntPtr src = this.Lock( offset, length, BufferLocking.ReadOnly );

            // copy that data in there
            Memory.Copy( src, dest, length );
            
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
            // lock the buffer re al quick
            IntPtr dest = this.Lock( offset, length,
                discardWholeBuffer ? BufferLocking.Discard : BufferLocking.Normal );
            
            // copy that data in there
            Memory.Copy(src, dest, length);

           /* unsafe
           {
               for (int i = 0; i < length; i++)
                   bufferBytes[i] = (byte)((short*)src.ToPointer())[i];
               // Memory.Copy(src, te, length);
           //    d3dBuffer.SetData<byte>(bufferBytes, offset, length);
           }*/
           
           this.Unlock();

        }

        public override void Dispose()
        {
            d3dBuffer.Dispose();
        }

        #endregion

        #region Properties

        public byte[] IndexBufferBytes
        {
            get
            {
                return bufferBytes;
            }
        }
        /// <summary>
        ///		Gets the underlying D3D Vertex Buffer object.
        /// </summary>
        public XFG.IndexBuffer D3DIndexBuffer
        {
            get
            {                
                return d3dBuffer;
            }
        }

        #endregion
    }
}
