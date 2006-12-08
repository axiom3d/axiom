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
//     <id value="$Id: $"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations
using System;

using Axiom.Core;
using Axiom.Graphics;

using XnaF = Microsoft.Xna.Framework;
using System.Runtime.InteropServices;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.Xna
{
    class XnaHardwareVertexBuffer: HardwareVertexBuffer
    {
        #region Member variables

        protected XnaF.Graphics.VertexBuffer xnaBuffer;
        private Byte[] _buffer;
        private GCHandle _gcHandle;

        #endregion

        #region Constructors

        public XnaHardwareVertexBuffer( int vertexSize, int numVertices, BufferUsage usage, XnaF.Graphics.GraphicsDevice device, bool useSystemMemory, bool useShadowBuffer )
            : base( vertexSize, numVertices, usage, useSystemMemory, useShadowBuffer )
        {
            // Create the XNA vertex buffer
            xnaBuffer = new XnaF.Graphics.VertexBuffer( device, sizeInBytes, XnaF.Graphics.ResourceUsage.None, XnaF.Graphics.ResourceManagementMode.Automatic );
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
        /// DOC
        protected override IntPtr LockImpl( int offset, int length, BufferLocking locking )
        {
            _buffer = new Byte[ length ];
            xnaBuffer.GetData<Byte>( _buffer );
            return Marshal.UnsafeAddrOfPinnedArrayElement( _buffer, 0 );
            _gcHandle = GCHandle.Alloc( xnaBuffer, GCHandleType.Weak );
            return GCHandle.ToIntPtr( _gcHandle );
        }

        /// <summary>
        /// 
        /// </summary>
        /// DOC
        public override void UnlockImpl()
        {
            //_gcHandle.Free();
            //_buffer = null;
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
        }

        public override void Dispose()
        {
            xnaBuffer.Dispose();
        }

        #endregion

        #region Properties

        /// <summary>
        ///		Gets the underlying D3D Vertex Buffer object.
        /// </summary>
        public XnaF.Graphics.VertexBuffer XnaVertexBuffer
        {
            get
            {
                return xnaBuffer;
            }
        }

        #endregion

    }
}
