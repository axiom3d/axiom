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
using System.Diagnostics;
using Axiom.Collections;
using Axiom.Core;
using Axiom.MathLib;
using Axiom.MathLib.Collections;

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

            edgeData = new EdgeData();
            // resize the edge group list to equal the number of vertex sets
            edgeData.edgeGroups.Capacity = vertexDataList.Count;

            // Initialize edge group data
            for(int i = 0; i < vertexDataList.Count; i++) {
                EdgeData.EdgeGroup group = new EdgeData.EdgeGroup();
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

			//edgeData.DebugLog();
			//DebugLog();

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
				byte* pBaseVertex = (byte*)posPtr.ToPointer();

                short* p16Idx = null;
                int* p32Idx = null;

                if(indexData.indexBuffer.Type == IndexType.Size16) {
                    p16Idx = (short*)idxPtr.ToPointer();
                }
                else {
                    p32Idx = (int*)idxPtr.ToPointer();
                }

                float* pReal = null;

				int triStart = edgeData.triangles.Count;

                // iterate over all the groups of 3 indices
                edgeData.triangles.Capacity = triStart + iterations;

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
                        tri.vertIndex[i] = index[i];

						// Retrieve the vertex position
						byte* pVertex = pBaseVertex + (index[i] * posBuffer.VertexSize);
						pReal = (float*)(pVertex + posElem.Offset);
						v[i].x = *pReal++;
						v[i].y = *pReal++;
						v[i].z = *pReal++;
						// find this vertex in the existing vertex map, or create it
						tri.sharedVertIndex[i] = FindOrCreateCommonVertex(v[i], vertexSet);
                    }

					// Calculate triangle normal (NB will require recalculation for 
					// skeletally animated meshes)
					tri.normal = MathUtil.CalculateFaceNormal(v[0], v[1], v[2]);
					// Add triangle to list
					edgeData.triangles.Add(tri);
					// Create edges from common list
					EdgeData.Edge e = new EdgeData.Edge();
					e.isDegenerate = true; // initialise as degenerate

					if (tri.sharedVertIndex[0] < tri.sharedVertIndex[1]) {
						// Set only first tri, the other will be completed in connectEdges
						e.triIndex[0] = triStart + t;
						e.sharedVertIndex[0] = tri.sharedVertIndex[0];
						e.sharedVertIndex[1] = tri.sharedVertIndex[1];
						e.vertIndex[0] = tri.vertIndex[0];
						e.vertIndex[1] = tri.vertIndex[1];
						((EdgeData.EdgeGroup)edgeData.edgeGroups[vertexSet]).edges.Add(e);

						e = new EdgeData.Edge();
						//e.isDegenerate = true;
					}
					if (tri.sharedVertIndex[1] < tri.sharedVertIndex[2]) {
						// Set only first tri, the other will be completed in connectEdges
						e.triIndex[0] = triStart + t;
						e.sharedVertIndex[0] = tri.sharedVertIndex[1];
						e.sharedVertIndex[1] = tri.sharedVertIndex[2];
						e.vertIndex[0] = tri.vertIndex[1];
						e.vertIndex[1] = tri.vertIndex[2];
						((EdgeData.EdgeGroup)edgeData.edgeGroups[vertexSet]).edges.Add(e);

						e = new EdgeData.Edge();
						//e.isDegenerate = true;
					}
					if (tri.sharedVertIndex[2] < tri.sharedVertIndex[0]) {
						// Set only first tri, the other will be completed in connectEdges
						e.triIndex[0] = triStart + t;
						e.sharedVertIndex[0] = tri.sharedVertIndex[2];
						e.sharedVertIndex[1] = tri.sharedVertIndex[0];
						e.vertIndex[0] = tri.vertIndex[2];
						e.vertIndex[1] = tri.vertIndex[0];
						((EdgeData.EdgeGroup)edgeData.edgeGroups[vertexSet]).edges.Add(e);

						e = new EdgeData.Edge();
						//e.isDegenerate = true;
					}
                } // for iterations
            } // unsafe

			// unlock those buffers!
			indexData.indexBuffer.Unlock();
			posBuffer.Unlock();
        }

        /// <summary>
        ///     
        /// </summary>
		protected void ConnectEdges() {
			int triIndex = 0;

			for (int i = 0; i < edgeData.triangles.Count; i++, triIndex++) {
				EdgeData.Triangle tri = (EdgeData.Triangle)edgeData.triangles[i];
				EdgeData.Edge e = null;

				if (tri.sharedVertIndex[0] > tri.sharedVertIndex[1]) {
					e = FindEdge(tri.sharedVertIndex[1], tri.sharedVertIndex[0]);
					if(e != null) {
						e.triIndex[1] = triIndex;
						e.isDegenerate = false;
					}
				}
				if (tri.sharedVertIndex[1] > tri.sharedVertIndex[2]) {
					// Find the existing edge (should be reversed order)
					e = FindEdge(tri.sharedVertIndex[2], tri.sharedVertIndex[1]);
					if(e != null) {
						e.triIndex[1] = triIndex;
						e.isDegenerate = false;
					}
				}
				if (tri.sharedVertIndex[2] > tri.sharedVertIndex[0]) {
					e = FindEdge(tri.sharedVertIndex[0], tri.sharedVertIndex[2]);
					if(e != null) {
						e.triIndex[1] = triIndex;
						e.isDegenerate = false;
					}
				}
			}
		}

        /// <summary>
        ///     
        /// </summary>
        /// <param name="sharedIndex1"></param>
        /// <param name="sharedIndex2"></param>
        /// <returns></returns>
        protected EdgeData.Edge FindEdge(int sharedIndex1, int sharedIndex2) {
			// Iterate over the existing edges
			for(int i = 0; i < edgeData.edgeGroups.Count; i++) {
				EdgeData.EdgeGroup edgeGroup = (EdgeData.EdgeGroup)edgeData.edgeGroups[i]; 

				for(int j = 0; j < edgeGroup.edges.Count; j++) {
					EdgeData.Edge edge = (EdgeData.Edge)edgeGroup.edges[j];

					if (edge.sharedVertIndex[0] == sharedIndex1 && 
						edge.sharedVertIndex[1] == sharedIndex2) {

						return edge;
					}
				}
			}
	        
			// no edge found
			return null;
        }

		/// <summary>
		///		Finds an existing common vertex, or inserts a new one.
		/// </summary>
		/// <param name="vec"></param>
		/// <param name="vertexSet"></param>
		/// <returns></returns>
		protected int FindOrCreateCommonVertex(Vector3 vec, int vertexSet) {
			for (int index = 0; index < vertices.Count; index++) {
				CommonVertex commonVec = (CommonVertex)vertices[index];

				if (MathUtil.FloatEqual(vec.x, commonVec.position.x, 1e-04f) && 
					MathUtil.FloatEqual(vec.y, commonVec.position.y, 1e-04f) && 
					MathUtil.FloatEqual(vec.z, commonVec.position.z, 1e-04f)) {

					return index;
				}
			}

			// Not found, insert
			CommonVertex newCommon = new CommonVertex();
			newCommon.index = vertices.Count;
			newCommon.position = vec;
			newCommon.vertexSet = vertexSet;
			vertices.Add(newCommon);

			return newCommon.index;
		}

		public unsafe void DebugLog() {
			WL("EdgeListBuilder Log");
			WL("-------------------");
			WL("Number of vertex sets: {0}", vertexDataList.Count);
			WL("Number of index sets: {0}", indexDataList.Count);
	        
			int i, j;

			// Log original vertex data
			for(i = 0; i < vertexDataList.Count; i++) {
				VertexData vData = (VertexData)vertexDataList[i];
				WL(".");
				WL("Original vertex set {0} - vertex count {1}", i, vData.vertexCount);

				VertexElement posElem = 
					vData.vertexDeclaration.FindElementBySemantic(VertexElementSemantic.Position);
				HardwareVertexBuffer vbuf = 
					vData.vertexBufferBinding.GetBuffer(posElem.Source);

				// lock the buffer for reading
				IntPtr basePtr = vbuf.Lock(BufferLocking.ReadOnly);

				byte* pBaseVertex = (byte*)basePtr.ToPointer();

				float* pReal;

				for (j = 0; j < vData.vertexCount; j++) {
					pReal = (float*)(pBaseVertex + posElem.Offset);

					WL("Vertex {0}: ({1}, {2}, {3})", j, pReal[0], pReal[1], pReal[2]);

					pBaseVertex += vbuf.VertexSize;
				}

				vbuf.Unlock();
			}

			// Log original index data
			for(i = 0; i < indexDataList.Count; i += 3) {
				IndexData iData = (IndexData)indexDataList[i];
				WL(".");
				WL("Original triangle set {0} - index count {1} - vertex set {2})", 
					i, iData.indexCount, indexDataVertexDataSetList[i]);

				// Get the indexes ready for reading
				short* p16Idx = null;
				int* p32Idx = null;

				IntPtr idxPtr = iData.indexBuffer.Lock(BufferLocking.ReadOnly);

				if (iData.indexBuffer.Type == IndexType.Size32) {
					p32Idx = (int*)idxPtr.ToPointer();
				}
				else {
					p16Idx = (short*)idxPtr.ToPointer();
				}

				for (j = 0; j < iData.indexCount / 3; j++) {
					if (iData.indexBuffer.Type == IndexType.Size32) {
						WL("Triangle {0}: ({1}, {2}, {3})", j, *p32Idx++, *p32Idx++, *p32Idx++);
					}
					else {
						WL("Triangle {0}: ({1}, {2}, {3})", j, *p16Idx++, *p16Idx++, *p16Idx++);
					}
				}

				iData.indexBuffer.Unlock();

				// Log common vertex list
				WL(".");
				WL("Common vertex list - vertex count {0}", vertices.Count);

				for (i = 0; i < vertices.Count; i++) {
					CommonVertex c = (CommonVertex)vertices[i];

					WL("Common vertex {0}: (vertexSet={1}, originalIndex={2}, position={3}", 
						i, c.vertexSet, c.index, c.position);
				}
			}
		}

		private void WL(string msg, params object[] args) {
			Debug.WriteLine(string.Format(msg, args));
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

        public class CommonVertexList : ArrayList {}

        #endregion Structs
	}
}
