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
using Microsoft.DirectX.Direct3D;
using D3D = Microsoft.DirectX.Direct3D;
using Axiom.SubSystems.Rendering;
using VertexDeclaration = Axiom.SubSystems.Rendering.VertexDeclaration;

namespace RenderSystem_DirectX9 {
    /// <summary>
    /// 	Summary description for D3DHardwareBufferManager.
    /// </summary>
    public class D3DHardwareBufferManager : HardwareBufferManager {
        #region Member variables

        protected D3D.Device device;
		
        #endregion
		
        #region Constructors
		
        /// <summary>
        ///		
        /// </summary>
        /// <param name="device"></param>
        public D3DHardwareBufferManager(D3D.Device device) {
            this.device = device;


        }
		
        #endregion
		
        #region Methods
		
        public override Axiom.SubSystems.Rendering.HardwareIndexBuffer CreateIndexBuffer(IndexType type, int numIndices, BufferUsage usage) {
            // call overloaded method with no shadow buffer
            return CreateIndexBuffer(type, numIndices, usage, false);
        }

        public override Axiom.SubSystems.Rendering.HardwareIndexBuffer CreateIndexBuffer(IndexType type, int numIndices, BufferUsage usage, bool useShadowBuffer) {
            return new D3DHardwareIndexBuffer(type, numIndices, usage, device, false, useShadowBuffer);
        }

        public override HardwareVertexBuffer CreateVertexBuffer(int vertexSize, int numVerts, BufferUsage usage) {
            // call overloaded method with no shadow buffer
            return CreateVertexBuffer(vertexSize, numVerts, usage, false);
        }

        public override HardwareVertexBuffer CreateVertexBuffer(int vertexSize, int numVerts, BufferUsage usage, bool useShadowBuffer) {
            return new D3DHardwareVertexBuffer(vertexSize, numVerts, usage, device, false, useShadowBuffer);
        }

        public override Axiom.SubSystems.Rendering.VertexDeclaration CreateVertexDeclaration() {
            VertexDeclaration decl = new D3DVertexDeclaration(device);
            vertexDeclarations.Add(decl);
            return decl;
        }

        // TODO: Disposal

        #endregion
		
        #region Properties
		
        #endregion

    }
}
