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
//     <id value="$Id: D3DHardwareBufferManager.cs 884 2006-09-14 06:32:07Z borrillis $"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;

using Axiom.Graphics;
using Axiom.Core;

using VertexDeclaration = Axiom.Graphics.VertexDeclaration;
using DX = SlimDX;
using D3D = SlimDX.Direct3D9;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.SlimDX9
{
    /// <summary>
    /// 	Summary description for SDXHardwareBufferManager.
    /// </summary>
    public class SDXHardwareBufferManager : HardwareBufferManager
    {
        #region Member variables

        protected D3D.Device device;

        #endregion

        #region Constructors

        /// <summary>
        ///		
        /// </summary>
        /// <param name="device"></param>
        public SDXHardwareBufferManager( D3D.Device device )
        {
            this.device = device;
        }

        #endregion

        #region Methods

        public override Axiom.Graphics.HardwareIndexBuffer CreateIndexBuffer( IndexType type, int numIndices, BufferUsage usage )
        {
            // call overloaded method with no shadow buffer
            return CreateIndexBuffer( type, numIndices, usage, false );
        }

        public override Axiom.Graphics.HardwareIndexBuffer CreateIndexBuffer( IndexType type, int numIndices, BufferUsage usage, bool useShadowBuffer )
        {
            SDXHardwareIndexBuffer buffer = new SDXHardwareIndexBuffer( type, numIndices, usage, device, false, useShadowBuffer );
            indexBuffers.Add( buffer );
            return buffer;
        }

        public override HardwareVertexBuffer CreateVertexBuffer( int vertexSize, int numVerts, BufferUsage usage )
        {
            // call overloaded method with no shadow buffer
            return CreateVertexBuffer( vertexSize, numVerts, usage, false );
        }

        public override HardwareVertexBuffer CreateVertexBuffer( int vertexSize, int numVerts, BufferUsage usage, bool useShadowBuffer )
        {
            SDXHardwareVertexBuffer buffer = new SDXHardwareVertexBuffer( vertexSize, numVerts, usage, device, false, useShadowBuffer );
            vertexBuffers.Add( buffer );
            return buffer;
        }

        public override Axiom.Graphics.VertexDeclaration CreateVertexDeclaration()
        {
            VertexDeclaration decl = new SDXVertexDeclaration( device );
            vertexDeclarations.Add( decl );
            return decl;
        }

        //-----------------------------------------------------------------------
        public void ReleaseDefaultPoolResources()
        {
            int iCount = 0;
            int vCount = 0;

            foreach ( SDXHardwareVertexBuffer buffer in vertexBuffers )
            {
                if ( buffer.ReleaseIfDefaultPool() )
                {
                    vCount++;
                }
            }

            foreach ( SDXHardwareIndexBuffer buffer in indexBuffers )
            {
                if ( buffer.ReleaseIfDefaultPool() )
                {
                    iCount++;
                }
            }

            LogManager.Instance.Write( "D3DHardwareBufferManager released:" );
            LogManager.Instance.Write( "\t{0} unmanaged vertex buffers.", vCount );
            LogManager.Instance.Write( "\t{0} unmanaged index buffers.", iCount );
        }

        //-----------------------------------------------------------------------
        public void RecreateDefaultPoolResources()
        {
            int iCount = 0;
            int vCount = 0;

            foreach ( SDXHardwareVertexBuffer buffer in vertexBuffers )
            {
                if ( buffer.RecreateIfDefaultPool( device ) )
                {
                    vCount++;
                }
            }

            foreach ( SDXHardwareIndexBuffer buffer in indexBuffers )
            {
                if ( buffer.RecreateIfDefaultPool( device ) )
                {
                    iCount++;
                }
            }

            LogManager.Instance.Write( "D3DHardwareBufferManager recreated:" );
            LogManager.Instance.Write( "\t{0} unmanaged vertex buffers.", vCount );
            LogManager.Instance.Write( "\t{0} unmanaged index buffers.", iCount );
        }

        // TODO: Disposal

        #endregion
    }
}