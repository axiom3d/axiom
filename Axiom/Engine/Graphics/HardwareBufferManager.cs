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
using System.Collections;

namespace Axiom.Graphics {
    /// <summary>
    /// 	Abstract singleton class for managing hardware buffers, a concrete instance
    ///		of this will be created by the RenderSystem.
    /// </summary>
    public abstract class HardwareBufferManager {
        #region Singleton implementation

        static HardwareBufferManager() { Init(); }
        protected HardwareBufferManager() { instance = this; }
        protected static HardwareBufferManager instance;

        public static HardwareBufferManager Instance {
            get { return instance; }
        }

        public static void Init() {
            instance = null;
        }
		
        #endregion

        #region Member variables

        protected ArrayList vertexDeclarations = new ArrayList();
        protected ArrayList vertexBufferBindings = new ArrayList();
		
        #endregion
		
        #region Methods

        /// <summary>
        ///		Overloaded method.
        /// </summary>
        /// <param name="vertexSize"></param>
        /// <param name="numVerts"></param>
        /// <param name="usage"></param>
        public abstract HardwareVertexBuffer CreateVertexBuffer(int vertexSize, int numVerts, BufferUsage usage);

        /// <summary>
        ///		Create a hardware vertex buffer.
        /// </summary>
        /// <remarks>
        ///		This method creates a new vertex buffer; this will act as a source of geometry
        ///		data for rendering objects. Note that because the meaning of the contents of
        ///		the vertex buffer depends on the usage, this method does not specify a
        ///		vertex format; the user of this buffer can actually insert whatever data 
        ///		they wish, in any format. However, in order to use this with a RenderOperation,
        ///		the data in this vertex buffer will have to be associated with a semantic element
        ///		of the rendering pipeline, e.g. a position, or texture coordinates. This is done 
        ///		using the VertexDeclaration class, which itself contains VertexElement structures
        ///		referring to the source data.
        ///		<p/>
        ///		Note that because vertex buffers can be shared, they are reference
        ///		counted so you do not need to worry about destroying themm this will be done
        ///		automatically.
        /// </remarks>
        /// <param name="vertexSize">The size in bytes of each vertex in this buffer; you must calculate
        ///		this based on the kind of data you expect to populate this buffer with.</param>
        /// <param name="numVerts">The number of vertices in this buffer.</param>
        /// <param name="usage">One or more members of the BufferUsage enumeration; you are
        ///		strongly advised to use StaticWriteOnly wherever possible, if you need to 
        ///		update regularly, consider WriteOnly and useShadowBuffer=true.</param>
        /// <param name="useShadowBuffer"></param>
        public abstract HardwareVertexBuffer CreateVertexBuffer(int vertexSize, int numVerts, BufferUsage usage, bool useShadowBuffer);
		
        /// <summary>
        ///		Overloaded method.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="numIndices"></param>
        /// <param name="usage"></param>
        /// <param name="useShadowBuffer"></param>
        /// <returns></returns>
        /// DOC
        public abstract HardwareIndexBuffer CreateIndexBuffer(IndexType type, int numIndices, BufferUsage usage);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="numIndices"></param>
        /// <param name="usage"></param>
        /// <param name="useShadowBuffer"></param>
        /// <returns></returns>
        /// DOC
        public abstract HardwareIndexBuffer CreateIndexBuffer(IndexType type, int numIndices, BufferUsage usage, bool useShadowBuffer);

        /// <summary>
        /// 
        /// </summary>
        /// DOC
        public virtual VertexDeclaration CreateVertexDeclaration() {
            VertexDeclaration decl = new VertexDeclaration();
            vertexDeclarations.Add(decl);
            return decl;
        }

        /// <summary>
        /// 
        /// </summary>
        /// DOC
        public virtual VertexBufferBinding CreateVertexBufferBinding() {
            VertexBufferBinding binding = new VertexBufferBinding();
            vertexBufferBindings.Add(binding);
            return binding;
        }

        #endregion
		
        #region Properties
		
        #endregion
    }
}
