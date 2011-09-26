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

#endregion

#region SVN Version Information

// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Diagnostics;
using Axiom.Core;
using Axiom.CrossPlatform;
using Axiom.Graphics;
using Microsoft.Xna.Framework.Graphics;
using BufferUsage = Axiom.Graphics.BufferUsage;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.Xna
{
    /// <summary>
    /// 	Summary description for XnaHardwareIndexBuffer.
    /// </summary>
    public class XnaHardwareIndexBuffer : HardwareIndexBuffer
    {
        #region Member variables

        protected GraphicsDevice _device;
        protected IndexBuffer _xnaBuffer;
        protected Array data;

        private readonly byte[] _bufferBytes;
        private readonly IndexElementSize _bufferType;
        private int _offset;
        private int _length;

        #endregion

        #region Constructors

        private static IndexType Validate(IndexType type)
        {
#if SILVERLIGHT
            if (type != IndexType.Size16)
                LogManager.Instance.Write("WARNING!!! Requested 32 bit indexes but Reach profile on only allows 16 bit indexes");
            return IndexType.Size16;
#else
            return type;
#endif
        }

        public XnaHardwareIndexBuffer( HardwareBufferManagerBase manager, IndexType type, int numIndices, BufferUsage usage, GraphicsDevice device, bool useSystemMemory, bool useShadowBuffer )
            : base(manager, Validate(type), numIndices, usage, useSystemMemory, useShadowBuffer)
        {
            if (this.type == IndexType.Size16)
                _bufferType = IndexElementSize.SixteenBits;
#if !SILVERLIGHT
            else
                _bufferType = IndexElementSize.ThirtyTwoBits;
#endif

            // create the buffer
            if ( usage == BufferUsage.Dynamic || usage == BufferUsage.DynamicWriteOnly )
            {
                _xnaBuffer = new IndexBuffer( device, _bufferType, numIndices, XnaHelper.Convert( usage ) );
            }
            else
            {
                _xnaBuffer = new IndexBuffer( device, _bufferType, numIndices,
                                              Microsoft.Xna.Framework.Graphics.BufferUsage.None );
            }

            _bufferBytes = new byte[sizeInBytes];
            _bufferBytes.Initialize();
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
        protected override BufferBase LockImpl( int offset, int length, BufferLocking locking )
        {
            _offset = offset;
            _length = length;
            return BufferBase.Wrap( _bufferBytes ).Offset( offset );
        }

        /// <summary>
        ///
        /// </summary>
        protected override void UnlockImpl()
        {
            //there is no unlock/lock system on XNA, just copy the byte buffer into the video card memory
            // d3dBuffer.SetData<byte>(bufferBytes);
            //this is a lot faster :)
            _xnaBuffer.SetData( _offset, _bufferBytes, _offset, _length );
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <param name="dest"></param>
        public override void ReadData( int offset, int length, BufferBase dest )
        {
            // lock the buffer for reading
            var src = Lock( offset, length, BufferLocking.ReadOnly );

            // copy that data in there
            Memory.Copy( src, dest, length );

            Unlock();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <param name="src"></param>
        /// <param name="discardWholeBuffer"></param>
        public override void WriteData( int offset, int length, BufferBase src, bool discardWholeBuffer )
        {
            // lock the buffer re al quick
            var dest = Lock( offset, length, discardWholeBuffer ? BufferLocking.Discard : BufferLocking.Normal );

            // copy that data in there
            Memory.Copy( src, dest, length );

            Unlock();
        }

        protected override void dispose( bool disposeManagedResources )
        {
            if ( !IsDisposed )
            {
                if ( disposeManagedResources )
                {
                }

                if ( _xnaBuffer != null )
                {
                    _xnaBuffer.Dispose();
                    _xnaBuffer = null;
                }
            }

            // If it is available, make the call to the
            // base class's Dispose(Boolean) method
            base.dispose( disposeManagedResources );
        }

        #endregion

        #region Properties

        /// <summary>
        ///		Gets the underlying Xna Vertex Buffer object.
        /// </summary>
        public IndexBuffer XnaIndexBuffer
        {
            get
            {
                return _xnaBuffer;
            }
        }

        #endregion
    }
}