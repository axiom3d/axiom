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
using Axiom.Core;
using Axiom.MathLib;

namespace Axiom.Graphics {
	/// <summary>
	///     General utility class for building edge lists for geometry.
	/// </summary>
	/// <remarks>
	///     You can add multiple sets of vertex and index data to build and edge list. 
	///     Edges will be built between the various sets as well as within sets; this allows 
	///     you to use a model which is built from multiple SubMeshes each using 
	///     separate index and (optionally) vertex data and still get the same connectivity 
	///     information. It's important to note that the indexes for the edge will be constrained
	///     to a single vertex buffer though (this is required in order to render the edge).
	/// </remarks>
	public class EdgeListBuilder {
        #region Fields

        protected IndexDataList indexDataList = new IndexDataList();
        protected IntList indexDataVertexDataSetList = new IntList();
        protected VertexDataList vertexDataList = new VertexDataList();
        protected CommonVertexList vertices = new CommonVertexList();
        protected EdgeData edgeData = new EdgeData();

        // TODO: CommonVertexMap <Vector3, size_t, vectorLess>

        /** Comparator for unique vertex list
        struct vectorLess {
            _OgreExport bool operator()(const Vector3& v1, const Vector3& v2) const {
                            if (v1.x < v2.x) return true;
                            if (v1.x == v2.x && v1.y < v2.y) return true;
                            if (v1.x == v2.x && v1.y == v2.y && v1.z < v2.z) return true;

                            return false;
                        }
                        };
                */

        #endregion Fields

        #region Methods

        /// <summary>
        ///     Add a set of vertex geometry data to the edge builder.
        /// </summary>
        /// <remarks>
        ///     You must add at least one set of vertex data to the builder before invoking the
        ///     <see cref="Build"/> method.
        /// </remarks>
        /// <param name="vertexData">Vertex data to consider for edge detection.</param>
        public void AddVertexData(VertexData vertexData) {
            vertexDataList.Add(vertexData);
        }

        /// <summary>
        ///     Add a set of index geometry data to the edge builder.
        /// </summary>
        /// <remarks>
        ///     You must add at least one set of index data to the builder before invoking the
        ///     <see cref="Build"/> method.
        /// </remarks>
        /// <param name="indexData">The index information which describes the triangles.</param>
        public void AddIndexData(IndexData indexData) {
            AddIndexData(indexData, 0);
        }

        /// <summary>
        ///     Add a set of index geometry data to the edge builder.
        /// </summary>
        /// <remarks>
        ///     You must add at least one set of index data to the builder before invoking the
        ///     <see cref="Build"/> method.
        /// </remarks>
        /// <param name="indexData">The index information which describes the triangles.</param>
        /// <param name="vertexSet">
        ///     The vertex data set this index data refers to; you only need to alter this
        ///     if you have added multiple sets of vertices.
        /// </param>
        public void AddIndexData(IndexData indexData, int vertexSet) {
            indexDataList.Add(indexData);
            indexDataVertexDataSetList.Add(vertexSet);
        }

        /// <summary>
        ///     Builds the edge information based on the information built up so far.
        /// </summary>
        /// <returns>All edge data from the vertex/index data recognized by the builder.</returns>
        public EdgeData Build() {
            // TODO: Implementation

            /* Ok, here's the algorithm:
            For each set of indices in turn
              // First pass, create triangles and create edges
              For each set of 3 indexes
                Create a new Triangle entry in the list
                For each vertex referenced by the tri indexes
                  Get the position of the vertex as a Vector3 from the correct vertex buffer
                  Attempt to locate this position in the existing common vertex set
                  If not found
                    Create a new common vertex entry in the list
                  End If
                  Populate the original vertex index and common vertex index 
                Next vertex
                If commonIndex[0] < commonIndex[1]
                    Create a new edge 
                End If
                If commonIndex[1] < commonIndex[2]
                    Create a new edge 
                End If
                If commonIndex[2] < commonIndex[0]
                    Create a new edge 
                End If
              Next set of 3 indexes
            Next index set
            // Identify shared edges (works across index sets)
            For each triangle in the common triangle list
            If commonIndex[0] > commonIndex[1]
                Find existing edge and update with second side
            End If
            If commonIndex[1] > commonIndex[2]
                Find existing edge and update with second side
            End If
            If commonIndex[2] > commonIndex[0]
                Find existing edge and update with second side
            End If
            Next triangle

            Note that all edges 'belong' to the index set which originally caused them
            to be created, which also means that the 2 vertices on the edge are both referencing the 
            vertex buffer which this index set uses.
            */

            return new EdgeData();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="indexSet"></param>
        /// <param name="vertexSet"></param>
        protected void BuildTrianglesEdges(int indexSet, int vertexSet) {
            // TODO: Implementation
        }

        /// <summary>
        ///     
        /// </summary>
        protected void ConnectEdges() {
            // TODO: Implementation
        }

        /// <summary>
        ///     
        /// </summary>
        /// <param name="sharedIndex1"></param>
        /// <param name="sharedIndex2"></param>
        /// <returns></returns>
        protected EdgeData.Edge FindEdge(int sharedIndex1, int sharedIndex2) {
            // TODO: Implementation
            return new EdgeData.Edge();
        }

        #endregion Methods

        #region Structs

        /// <summary>
        ///     A vertex can actually represent several vertices in the final model, because
        ///     vertices along texture seams etc will have been duplicated. In order to properly
        ///     evaluate the surface properties, a single common vertex is used for these duplicates,
        ///     and the faces hold the detail of the duplicated vertices.
        /// </summary>
        protected struct CommonVertex {
            /// <summary>
            ///     Location of point in euclidean space.
            /// </summary>
            public Vector3 position;
            /// <summary>
            ///     Place of vertex in original vertex set.
            /// </summary>
            public int index;
            /// <summary>
            ///      The vertex set this came from.
            /// </summary>
            public int vertexSet;
        }

        /// <summary>
        ///     Generics: List<EdgeListBuilder.CommonVertex>
        /// </summary>
        public class CommonVertexList : ArrayList {}

        #endregion Structs
	}
}
