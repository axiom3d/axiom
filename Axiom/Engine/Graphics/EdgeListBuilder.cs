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
using Axiom.Collections;
using Axiom.Core;
using Axiom.MathLib;

namespace Axiom.Graphics {
	/// <summary>
	///     General utility class for building edge lists for geometry.
	/// </summary>
	/// <remarks>
	///     You can add multiple sets of vertex and index data to build an edge list. 
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
        protected Map vertexLookup = new Map(new Vector3Comparer());
        protected CommonVertexList vertices = new CommonVertexList();
        protected EdgeData edgeData = new EdgeData();

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

            vertexLookup.Clear();
            edgeData = new EdgeData();
            // resize the edge group list to equal the number of vertex sets
            edgeData.edgeGroups.Capacity = vertexDataList.Count;

            // Initialize edge group data
            for(int i = 0; i < vertexDataList.Count; i++) {
                EdgeGroup group = new EdgeGroup();
                group.vertexSet = i;
                group.vertexData = (VertexData)vertexDataList[i];
                edgeData.edgeGroups.Add(group);
            }

            // Stage 1: Build triangles and initial edge list.
            for(int i = 0, indexSet = 0; i < indexDataList.Count; i++, indexSet++) {
                int vertexSet = (int)indexDataVertexDataSetList[i];

                BuildTrianglesEdges(indexSet, vertexSet);
            }

            // Stage 2: Link edges.
            ConnectEdges();

            return edgeData;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="indexSet"></param>
        /// <param name="vertexSet"></param>
        protected void BuildTrianglesEdges(int indexSet, int vertexSet) {
            IndexData indexData = (IndexData)indexDataList[indexSet];
            int iterations = indexData.indexCount / 3;

            // locate postion element & the buffer to go with it
            VertexData vertexData = (VertexData)vertexDataList[vertexSet];
            VertexElement posElem = 
                vertexData.vertexDeclaration.FindElementBySemantic(VertexElementSemantic.Position);

            HardwareVertexBuffer posBuffer = vertexData.vertexBufferBinding.GetBuffer(posElem.Source);
            IntPtr posPtr = posBuffer.Lock(BufferLocking.ReadOnly);
            IntPtr idxPtr = indexData.indexBuffer.Lock(BufferLocking.ReadOnly);

            unsafe {
                short* p16Idx = null;
                int* p32Idx = null;

                if(indexData.indexBuffer.Type == IndexType.Size16) {
                    p16Idx = (short*)idxPtr.ToPointer();
                }
                else {
                    p32Idx = (int*)idxPtr.ToPointer();
                }

                float* pReal = null;

                // iterate over all the groups of 3 indices
                // TODO: Ogre has this as count + iterations, warn them
                edgeData.triangles.Capacity = edgeData.triangles.Count * iterations;

                for(int t = 0; t < iterations; t++) {
                    EdgeData.Triangle tri = new EdgeData.Triangle();
                    tri.indexSet = indexSet;
                    tri.vertexSet = vertexSet;
                    
                    int[] index = new int[3];
                    Vector3[] v = new Vector3[3];

                    for(int i = 0; i < 3; i++) {
                        if(indexData.indexBuffer.Type == IndexType.Size32) {
                            index[i] = *p32Idx++;
                        }
                        else {
                            index[i] = *p16Idx++;
                        }

                        // populate tri original vertex index
                        // TODO: Tri arrays need to be initialized first!!
                        tri.vertIndex[i] = index[i];
                    }
                }
            }

            // TODO: Implementation
            throw new NotImplementedException();
        }

        /// <summary>
        ///     
        /// </summary>
        protected void ConnectEdges() {
            // TODO: Implementation
            throw new NotImplementedException();
        }

        /// <summary>
        ///     
        /// </summary>
        /// <param name="sharedIndex1"></param>
        /// <param name="sharedIndex2"></param>
        /// <returns></returns>
        protected EdgeData.Edge FindEdge(int sharedIndex1, int sharedIndex2) {
            // TODO: Implementation
            throw new NotImplementedException();
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
        ///     A group of edges sharing the same vertex data.
        /// </summary>
        protected struct EdgeGroup {
            /// <summary>
            ///     The vertex set index that contains the vertices for this edge group.
            /// </summary>
            public int vertexSet;
            /// <summary>
            ///     Reference to the vertex data used by this edge group.
            /// </summary>
            public VertexData vertexData;
            /// <summary>
            ///     The edges themselves.
            /// </summary>
            public EdgeList edges;

        }

        /// <summary>
        ///     Class used to compare 2 vectors.
        /// </summary>
        /// TODO: Move into MathLib.
        public class Vector3Comparer : IComparer {
            #region IComparer Members

            /// <summary>
            ///     Compares 2 vectors.
            /// </summary>
            /// <param name="x">Vector 1.</param>
            /// <param name="y">Vector 2</param>
            /// <returns>-1 if x is less than y.  0 if equal, or 1 if y > x.</returns>
            public int Compare(object x, object y) {
                Vector3 v1 = (Vector3)x;
                Vector3 v2 = (Vector3)y;

                if (v1.x < v2.x) return -1;
                if (v1.x == v2.x && v1.y < v2.y) return -1;
                if (v1.x == v2.x && v1.y == v2.y && v1.z < v2.z) return -1;
                if(v1 == v2) return 0;

                return 1;
            }

            #endregion
        }

        public class CommonVertexList : ArrayList {}

        #endregion Structs
	}
}
