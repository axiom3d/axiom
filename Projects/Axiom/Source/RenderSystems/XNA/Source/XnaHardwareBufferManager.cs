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
using VertexDeclaration = Axiom.Graphics.VertexDeclaration;

using XNA = Microsoft.Xna.Framework.Graphics;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.Xna
{
    /// <summary>
    /// 	Summary description for XnaHardwareBufferManager.
    /// </summary>
    public class XnaHardwareBufferManager : HardwareBufferManager
    {
        #region Member variables

        protected XNA.GraphicsDevice device;

        #endregion

        #region Constructors

        /// <summary>
        ///		
        /// </summary>
        /// <param name="device"></param>
        public XnaHardwareBufferManager(XNA.GraphicsDevice device)
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
            XnaHardwareIndexBuffer buffer = new XnaHardwareIndexBuffer( type, numIndices, usage, device, false, useShadowBuffer );
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
            XnaHardwareVertexBuffer buffer = new XnaHardwareVertexBuffer( vertexSize, numVerts, usage, device, false, useShadowBuffer );
            vertexBuffers.Add( buffer );
            return buffer;
        }

        public override Axiom.Graphics.VertexDeclaration CreateVertexDeclaration()
        {
            VertexDeclaration decl = new XnaVertexDeclaration( device );
            vertexDeclarations.Add( decl );
            return decl;
        }

        // TODO: Disposal

        #endregion

        #region Properties

        #endregion

    }
}
