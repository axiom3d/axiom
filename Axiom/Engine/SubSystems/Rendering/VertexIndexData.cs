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

namespace Axiom.SubSystems.Rendering {
    /// <summary>
    /// 	Summary description for VertexIndexData.
    /// </summary>
    public class VertexData : ICloneable {
        #region Member variables
		
        public VertexDeclaration vertexDeclaration;
        public VertexBufferBinding vertexBufferBinding;
        public int vertexStart;
        public int vertexCount;

        // dont intialize, will only be done if the mesh being read has a skeleton
        public SoftwareBlendInfo softwareBlendInfo;

        #endregion

        public VertexData() {
            vertexDeclaration = HardwareBufferManager.Instance.CreateVertexDeclaration();
            vertexBufferBinding = HardwareBufferManager.Instance.CreateVertexBufferBinding();
        }

        #region ICloneable Members

        public object Clone() {
            // TODO:  Add VertexData.Clone implementation
            return null;
        }

        #endregion
    }

    /// <summary>
    ///    Software vertex blend information.
    /// </summary>
    /// <remarks>
    ///    This data is here in order to allow the creator of the VertexData 
    ///    to request a software vertex blend, ie a blend using information which you
    ///    do not want to be passed to the GPU.
    ///    <p/>
    ///    The assumption here is that you have a Postion and Normal elements in 
    ///    your declaration which you wish to update with a blended version of 
    ///    positions / normals from a system-memory location. We advise that 
    ///    if you're blending a lot, you set the hardware vertex buffer 
    ///    to DynamicWriteOnly, with no shadow buffer. 
    ///    <p/>
    ///    Note that future versions of the engine are likely to support vertex shader
    ///    based animation so there will be a hardware alternative; however, note that sometimes
    ///    you may still want to perform blending in software, for example when you need to read
    ///    back the blended positions in applications such as shadow volume construction.
    ///    <p/>
    ///    In order to apply this blending, the world matrices must be set and 
    ///    RenderSystem.SoftwareVertexBlend called. This is done automatically for skeletally
    ///    animated entities, but this can be done manually if required. After calling this
    ///    method, the vertex buffers are updated with the blended positions and the blend does
    ///    not need to be called again unless it's basis changes.
    /// </remarks>
    public class SoftwareBlendInfo {
        /// <summary>
        ///    If true, the RenderSystem will automatically apply the blend when rendering 
        ///    with this VertexData, otherwise the user of the vertex data must call 
        ///    RenderSystem.SofwareVertexBlend manually as required.
        /// </summary>
        public bool automaticBlend;
        /// <summary>
        ///    Array of source positions.
        /// </summary>
        public float[] srcPositions;
        /// <summary>
        ///    Array of source normals, could be null if vertex data does not include normals.
        /// </summary>
        public float[] srcNormals;
        /// <summary>
        ///    The number of blending weights per vertex
        /// </summary>
        public ushort numWeightsPerVertex;
        /// <summary>
        ///    Array of blend weights.
        /// </summary>
        public float[] blendWeights;
        /// <summary>
        ///    Array of blending indexes (index into world matrices)
        /// </summary>
        public byte[] blendIndices;

        /// <summary>
        ///    Default constructor.
        /// </summary>
        public SoftwareBlendInfo() {
            // default to automatic blending handled by the render system
            automaticBlend = true;
            numWeightsPerVertex = 1;
        }
    }

    /// <summary>
    /// 	Summary description for VertexIndexData.
    /// </summary>
    public class IndexData : ICloneable {
        #region Member variables

        public HardwareIndexBuffer indexBuffer;
        public int indexStart;
        public int indexCount;
		
        #endregion

        #region ICloneable Members

        public object Clone() {
            // TODO:  Add IndexData.Clone implementation
            return null;
        }

        #endregion
    }
}
