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

namespace Axiom.Graphics {
	/// <summary>
    ///     This class contains the information required to describe the edge connectivity of a
    ///     given set of vertices and indexes. 
	/// </summary>
	public class EdgeData {
        #region Fields

        /// <summary>
        ///     List of triangles.
        /// </summary>
        protected TriangleList triangles = new TriangleList();
        /// <summary>
        ///     List of edge groups.
        /// </summary>
        protected EdgeGroupList edgeGroups = new EdgeGroupList();

        #endregion Fields
		
        #region Methods

        /// <summary>
        ///     Calculate the light facing state of the triangles in this edge list.
        /// </summary>
        /// <remarks>
        ///     This is normally the first stage of calculating a silhouette, ie
        ///     establishing which tris are facing the light and which are facing
        ///     away. This state is stored in the 'lightFacing' flag in each 
        ///     Triangle.
        /// </remarks>
        /// <param name="lightPos">
        ///     4D position of the light in object space, note that 
        ///     for directional lights (which have no position), the w component
        ///     is 0 and the x/y/z position are the direction.
        /// </param>
        public void UpdateTriangleLightFacing(Vector4 lightPos) {
            for(int i = 0; i < triangles.Count; i++) {
                Triangle tri = (Triangle)triangles[i];

                float dot = tri.normal.Dot(lightPos);
                tri.lightFacing = dot > 0 ? true : false;

                // since tri is a struct, stick it back in at the current position
                triangles[i] = tri;
            }
        }

        #endregion Methods

        #region Structs

        /// <summary>
        ///     Basic triangle structure.
        /// </summary>
        public struct Triangle {
            #region Fields

            /// <summary>
            ///     The set of indexes this triangle came from (NB it is possible that the triangles on 
            ///     one side of an edge are using a different vertex buffer from those on the other side.)
            /// </summary>
            public int indexSet; 
            /// <summary>
            ///     The vertex set these vertices came from.
            /// </summary>
            public int vertexSet;
            /// <summary>
            ///     Vertex indexes, relative to the original buffer.
            /// </summary>
            public int[] vertIndex;
            /// <summary>
            ///     Vertex indexes, relative to a shared vertex buffer with 
            //      duplicates eliminated (this buffer is not exposed).
            /// </summary>
            public int[] sharedVertIndex;
            /// <summary>
            ///      Unit vector othogonal to this face, plus distance from origin.
            /// </summary>
            public Vector4 normal;
            /// <summary>
            ///     Working vector used when calculating the silhouette.
            /// </summary>
            public bool lightFacing;

            #endregion Fields
        }

        /// <summary>
        ///     Edge data.
        /// </summary>
        public struct Edge {
            #region Fields

            /// <summary>
            ///     The indexes of the 2 tris attached, note that tri 0 is the one where the 
            ///     indexes run *counter* clockwise along the edge. Indexes must be
            ///     reversed for tri 1.
            /// </summary>
            int[] triIndex;
            /// <summary>
            ///     The vertex indices for this edge. Note that both vertices will be in the vertex
            ///     set as specified in 'vertexSet', which will also be the same as tri 0.
            /// </summary>
            int[] vertIndex;
            /// <summary>
            ///     Vertex indices as used in the shared vertex list, not exposed.
            /// </summary>
            int[] sharedVertIndex;

            #endregion Fields
        }

        /// <summary>
        ///     A group of edges sharing the same vertex data.
        /// </summary>
        struct EdgeGroup {
            #region Fields

            /// <summary>
            ///     The vertex set index that contains the vertices for this edge group.
            /// </summary>
            public int vertexSet;
            /// <summary>
            ///     Reference to vertex data used by this edge group.
            /// </summary>
            public VertexData vertexData;
            /// <summary>
            ///     The edges themselves.
            /// </summary>
            public EdgeList edges;

            #endregion Fields
        }

        #endregion Structs
	}
}
