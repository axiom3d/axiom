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
using System.Collections;
using System.Diagnostics;
using Axiom.Collections;
using Axiom.Core;
using Axiom.Math;
using Axiom.Math.Collections;
using System.Collections.Generic;
using static Axiom.Math.Utility;

#endregion Namespace Declarations

namespace Axiom.Graphics
{
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
    public class EdgeListBuilder : AnyBuilder
    {
        #region Fields

        /// <summary>
        ///		List of common vertices.
        /// </summary>
        protected CommonVertexList vertices = new CommonVertexList();

        /// <summary>
        ///		Underlying edge data to use for building.
        /// </summary>
        protected EdgeData edgeData = new EdgeData();

        /// <summary>
        ///		Unique edges, used to detect whether there are too many triangles on an edge
        /// </summary>
        protected UniqueEdgeList uniqueEdges = new UniqueEdgeList();

        /// <summary>
        ///		Do we weld common vertices at all?
        /// </summary>
        protected bool weldVertices;

        /// <summary>
        ///		Should we treat coincident vertices from different vertex sets as one?
        /// </summary>
        protected bool weldVerticesAcrossVertexSets;

        /// <summary>
        ///		Should we treat coincident vertices referenced from different index sets as one?
        /// </summary>
        protected bool weldVerticesAcrossIndexSets;

        #endregion Fields

        #region Methods

        /// <summary>
        ///     Builds the edge information based on the information built up so far.
        /// </summary>
        /// <returns>All edge data from the vertex/index data recognized by the builder.</returns>
        public EdgeData Build()
        {
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


            /* 
			There is a major consideration: 'What is a common vertex'? This is a
			crucial decision, since to form a completely close hull, you need to treat
			vertices which are not physically the same as equivalent. This is because
			there will be 'seams' in the model, where discrepancies in vertex components
			other than position (such as normals or texture coordinates) will mean
			that there are 2 vertices in the same place, and we MUST 'weld' them
			into a single common vertex in order to have a closed hull. Just looking
			at the unique vertex indices is not enough, since these seams would render
			the hull invalid.

			So, we look for positions which are the same across vertices, and treat 
			those as as single vertex for our edge calculation. However, this has
			it's own problems. There are OTHER vertices which may have a common 
			position that should not be welded. Imagine 2 cubes touching along one
			single edge. The common vertices on that edge, if welded, will cause 
			an ambiguous hull, since the edge will have 4 triangles attached to it,
			whilst a manifold mesh should only have 2 triangles attached to each edge.
			This is a problem.

			We deal with this with fallback techniques. We try the following approaches,
			in order, falling back on the next approach if the current one results in
			an ambiguous hull:
		
			1. Weld all vertices at the same position across all vertex and index sets. 
			2. Weld vertices at the same position if they are in the same vertex set, 
			   but regardless of the index set
			3. Weld vertices at the same position if they were first referred to in 
			   the same index set, but regardless of the vertex set.
			4. Weld vertices only if they are in the same vertex set AND they are first
			   referenced in the same index set.
			5. Never weld vertices at the same position. This will only result in a
			   valid hull if there are no seams in the mesh (perfect vertex sharing)

			If all these techniques fail, the hull cannot be built. 

			Therefore, when you have a model which has a potentially ambiguous hull,
			(meeting at edges), you MUST EITHER:

			   A. differentiate the individual sub-hulls by separating them by 
				  vertex set or by index set.
			or B. ensure that you have no seams, ie you have perfect vertex sharing.
				  This is typically only feasible when you have no textures and 
				  completely smooth shading
			*/

            var technique = 1;
            var validHull = false;

            while (!validHull && technique <= 5)
            {
                switch (technique)
                {
                    case 1: // weld across everything
                        this.weldVertices = true;
                        this.weldVerticesAcrossVertexSets = true;
                        this.weldVerticesAcrossIndexSets = true;
                        break;
                    case 2: // weld across index sets only
                        this.weldVertices = true;
                        this.weldVerticesAcrossVertexSets = false;
                        this.weldVerticesAcrossIndexSets = true;
                        break;
                    case 3: // weld across vertex sets only
                        this.weldVertices = true;
                        this.weldVerticesAcrossVertexSets = true;
                        this.weldVerticesAcrossIndexSets = false;
                        break;
                    case 4: // weld within same index & vertex set only
                        this.weldVertices = true;
                        this.weldVerticesAcrossVertexSets = false;
                        this.weldVerticesAcrossIndexSets = false;
                        break;
                    case 5: // never weld
                        this.weldVertices = false;
                        this.weldVerticesAcrossVertexSets = false;
                        this.weldVerticesAcrossIndexSets = false;
                        break;
                } // switch

                // Log alternate techniques
                if (technique > 1)
                {
                    LogManager.Instance.Write("Trying alternative edge building technique {0}", technique);
                }

                try
                {
                    AttemptBuild();

                    // if we got here with no exceptions, we're done
                    validHull = true;
                }
                catch (Exception)
                {
                    // Ambiguous hull, try next technique
                    technique++;
                }
            }

            return this.edgeData;
        }

        private void AttemptBuild()
        {
            // reset
            this.vertices.Clear();
            this.uniqueEdges.Clear();

            this.edgeData = new EdgeData();

            // resize the edge group list to equal the number of vertex sets
            this.edgeData.edgeGroups.Capacity = vertexDataList.Count;

            // Initialize edge group data
            for (var i = 0; i < vertexDataList.Count; i++)
            {
                var group = new EdgeData.EdgeGroup();
                group.vertexSet = i;
                group.vertexData = (VertexData)vertexDataList[i];
                this.edgeData.edgeGroups.Add(group);
            }

            // Stage 1: Build triangles and initial edge list.
            for (int i = 0, indexSet = 0; i < indexDataList.Count; i++, indexSet++)
            {
                var vertexSet = (int)indexDataVertexDataSetList[i];

                BuildTrianglesEdges(indexSet, vertexSet);
            }

            // Stage 2: Link edges.
            ConnectEdges();

            //EdgeData.DebugLog(LogManager.Instance.CreateLog("EdgeListBuilder.log"));
            //DebugLog(LogManager.Instance.CreateLog("EdgeData.log"));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="indexSet"></param>
        /// <param name="vertexSet"></param>
        protected void BuildTrianglesEdges(int indexSet, int vertexSet)
        {
            var indexData = (IndexData)indexDataList[indexSet];
            var opType = operationTypes[indexSet];

            var iterations = 0;

            switch (opType)
            {
                case OperationType.TriangleList:
                    iterations = indexData.indexCount / 3;
                    break;

                case OperationType.TriangleFan:
                case OperationType.TriangleStrip:
                    iterations = indexData.indexCount - 2;
                    break;
            }

            // locate postion element & the buffer to go with it
            var vertexData = (VertexData)vertexDataList[vertexSet];
            var posElem = vertexData.vertexDeclaration.FindElementBySemantic(VertexElementSemantic.Position);

            var posBuffer = vertexData.vertexBufferBinding.GetBuffer(posElem.Source);
            var posPtr = posBuffer.Lock(BufferLocking.ReadOnly);
            var idxPtr = indexData.indexBuffer.Lock(BufferLocking.ReadOnly);

#if !AXIOM_SAFE_ONLY
            unsafe
#endif
            {
                var pBaseVertex = posPtr;

                var p16Idx = idxPtr.ToShortPointer();
                var p32Idx = idxPtr.ToIntPointer();

                // counters used for pointer indexing
                var count16 = 0;
                var count32 = 0;

                var triStart = this.edgeData.triangles.Count;

                // iterate over all the groups of 3 indices
                this.edgeData.triangles.Capacity = triStart + iterations;

                for (var t = 0; t < iterations; t++)
                {
                    var tri = new EdgeData.Triangle();
                    tri.indexSet = indexSet;
                    tri.vertexSet = vertexSet;

                    var index = new int[3];
                    var v = new Vector3[3];

                    for (var i = 0; i < 3; i++)
                    {
                        // Standard 3-index read for tri list or first tri in strip / fan
                        if (opType == OperationType.TriangleList || t == 0)
                        {
                            if (indexData.indexBuffer.Type == IndexType.Size32)
                            {
                                index[i] = p32Idx[count32++];
                            }
                            else
                            {
                                index[i] = p16Idx[count16++];
                            }
                        }
                        else
                        {
                            // Strips and fans are formed from last 2 indexes plus the 
                            // current one for triangles after the first
                            if (indexData.indexBuffer.Type == IndexType.Size32)
                            {
                                index[i] = p32Idx[i - 2];
                            }
                            else
                            {
                                index[i] = p16Idx[i - 2];
                            }

                            // Perform single-index increment at the last tri index
                            if (i == 2)
                            {
                                if (indexData.indexBuffer.Type == IndexType.Size32)
                                {
                                    count32++;
                                }
                                else
                                {
                                    count16++;
                                }
                            }
                        }

                        // populate tri original vertex index
                        tri.vertIndex[i] = index[i];

                        // Retrieve the vertex position
                        var pVertex = pBaseVertex + (index[i] * posBuffer.VertexSize);
                        var pReal = (pVertex + posElem.Offset).ToFloatPointer();

                        v[i].x = pReal[0];
                        v[i].y = pReal[1];
                        v[i].z = pReal[2];
                        // find this vertex in the existing vertex map, or create it
                        tri.sharedVertIndex[i] = FindOrCreateCommonVertex(v[i], vertexSet, indexSet, index[i]);
                    }

                    // Calculate triangle normal (NB will require recalculation for 
                    // skeletally animated meshes)
                    tri.normal = CalculateFaceNormal(v[0], v[1], v[2]);

                    // Add triangle to list
                    this.edgeData.triangles.Add(tri);

                    try
                    {
                        // create edges from common list
                        if (tri.sharedVertIndex[0] < tri.sharedVertIndex[1])
                        {
                            CreateEdge(vertexSet, triStart + t, tri.vertIndex[0], tri.vertIndex[1], tri.sharedVertIndex[0],
                                        tri.sharedVertIndex[1]);
                        }
                        if (tri.sharedVertIndex[1] < tri.sharedVertIndex[2])
                        {
                            CreateEdge(vertexSet, triStart + t, tri.vertIndex[1], tri.vertIndex[2], tri.sharedVertIndex[1],
                                        tri.sharedVertIndex[2]);
                        }
                        if (tri.sharedVertIndex[2] < tri.sharedVertIndex[0])
                        {
                            CreateEdge(vertexSet, triStart + t, tri.vertIndex[2], tri.vertIndex[0], tri.sharedVertIndex[2],
                                        tri.sharedVertIndex[0]);
                        }
                    }
                    catch (Exception ex)
                    {
                        //Debug.WriteLine(ex.ToString());
                        //Debug.WriteLine(ex.StackTrace);
                        // unlock those buffers!
                        indexData.indexBuffer.Unlock();
                        posBuffer.Unlock();

                        throw ex;
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
        /// <param name="vertexSet"></param>
        /// <param name="triangleIndex"></param>
        /// <param name="vertexIndex0"></param>
        /// <param name="vertexIndex1"></param>
        /// <param name="sharedVertIndex0"></param>
        /// <param name="sharedVertIndex1"></param>
        protected void CreateEdge(int vertexSet, int triangleIndex, int vertexIndex0, int vertexIndex1, int sharedVertIndex0,
                                   int sharedVertIndex1)
        {
            var vertPair = new UniqueEdge();
            vertPair.vertexIndex1 = sharedVertIndex0;
            vertPair.vertexIndex2 = sharedVertIndex1;

            if (this.uniqueEdges.Contains(vertPair))
            {
                throw new AxiomException("Edge is shared by too many triangles.");
            }

            this.uniqueEdges.Add(vertPair);

            // create a new edge and initialize as degenerate
            var e = new EdgeData.Edge();
            e.isDegenerate = true;

            // set only first tri, the other will be completed in ConnectEdges
            e.triIndex[0] = triangleIndex;
            e.sharedVertIndex[0] = sharedVertIndex0;
            e.sharedVertIndex[1] = sharedVertIndex1;
            e.vertIndex[0] = vertexIndex0;
            e.vertIndex[1] = vertexIndex1;

            ((EdgeData.EdgeGroup)this.edgeData.edgeGroups[vertexSet]).edges.Add(e);
        }

        /// <summary>
        ///     
        /// </summary>
        protected void ConnectEdges()
        {
            var triIndex = 0;

            for (var i = 0; i < this.edgeData.triangles.Count; i++, triIndex++)
            {
                var tri = (EdgeData.Triangle)this.edgeData.triangles[i];
                EdgeData.Edge e = null;

                if (tri.sharedVertIndex[0] > tri.sharedVertIndex[1])
                {
                    e = FindEdge(tri.sharedVertIndex[1], tri.sharedVertIndex[0]);

                    if (e != null)
                    {
                        e.triIndex[1] = triIndex;
                        e.isDegenerate = false;
                    }
                }
                if (tri.sharedVertIndex[1] > tri.sharedVertIndex[2])
                {
                    // Find the existing edge (should be reversed order)
                    e = FindEdge(tri.sharedVertIndex[2], tri.sharedVertIndex[1]);

                    if (e != null)
                    {
                        e.triIndex[1] = triIndex;
                        e.isDegenerate = false;
                    }
                }
                if (tri.sharedVertIndex[2] > tri.sharedVertIndex[0])
                {
                    e = FindEdge(tri.sharedVertIndex[0], tri.sharedVertIndex[2]);

                    if (e != null)
                    {
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
        protected EdgeData.Edge FindEdge(int sharedIndex1, int sharedIndex2)
        {
            // Iterate over the existing edges
            for (var i = 0; i < this.edgeData.edgeGroups.Count; i++)
            {
                var edgeGroup = (EdgeData.EdgeGroup)this.edgeData.edgeGroups[i];

                for (var j = 0; j < edgeGroup.edges.Count; j++)
                {
                    var edge = (EdgeData.Edge)edgeGroup.edges[j];

                    if (edge.sharedVertIndex[0] == sharedIndex1 && edge.sharedVertIndex[1] == sharedIndex2)
                    {
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
        /// <returns></returns>
        protected int FindOrCreateCommonVertex(Vector3 vec, int vertexSet, int indexSet, int originalIndex)
        {
            for (var index = 0; index < this.vertices.Count; index++)
            {
                var commonVec = (CommonVertex)this.vertices[index];

                if (RealEqual(vec.x, commonVec.position.x, 1e-04f) &&
                     RealEqual(vec.y, commonVec.position.y, 1e-04f) &&
                     RealEqual(vec.z, commonVec.position.z, 1e-04f) &&
                     (commonVec.vertexSet == vertexSet || this.weldVerticesAcrossVertexSets) &&
                     (commonVec.indexSet == indexSet || this.weldVerticesAcrossIndexSets) &&
                     (commonVec.originalIndex == originalIndex || this.weldVertices))
                {
                    return index;
                }
            }

            // Not found, insert
            var newCommon = new CommonVertex();
            newCommon.index = this.vertices.Count;
            newCommon.position = vec;
            newCommon.vertexSet = vertexSet;
            newCommon.indexSet = indexSet;
            newCommon.originalIndex = originalIndex;
            this.vertices.Add(newCommon);

            return newCommon.index;
        }

        public void DebugLog(Log log)
        {
            log.Write("EdgeListBuilder Log");
            log.Write("-------------------");
            log.Write("Number of vertex sets: {0}", vertexDataList.Count);
            log.Write("Number of index sets: {0}", indexDataList.Count);

            int i, j;

            // Log original vertex data
            for (i = 0; i < vertexDataList.Count; i++)
            {
                var vData = (VertexData)vertexDataList[i];
                log.Write(".");
                log.Write("Original vertex set {0} - vertex count {1}", i, vData.vertexCount);

                var posElem = vData.vertexDeclaration.FindElementBySemantic(VertexElementSemantic.Position);
                var vbuf = vData.vertexBufferBinding.GetBuffer(posElem.Source);

                // lock the buffer for reading
                var basePtr = vbuf.Lock(BufferLocking.ReadOnly);

                var pBaseVertex = basePtr;

#if !AXIOM_SAFE_ONLY
                unsafe
#endif
                {
                    for (j = 0; j < vData.vertexCount; j++)
                    {
                        var pReal = (pBaseVertex + posElem.Offset).ToFloatPointer();

                        log.Write("Vertex {0}: ({1}, {2}, {3})", j, pReal[0], pReal[1], pReal[2]);

                        pBaseVertex += vbuf.VertexSize;
                    }
                }

                vbuf.Unlock();
            }

            // Log original index data
            for (i = 0; i < indexDataList.Count; i += 3)
            {
                var iData = (IndexData)indexDataList[i];
                log.Write(".");
                log.Write("Original triangle set {0} - index count {1} - vertex set {2})", i, iData.indexCount,
                           indexDataVertexDataSetList[i]);

                var idxPtr = iData.indexBuffer.Lock(BufferLocking.ReadOnly);

#if !AXIOM_SAFE_ONLY
                unsafe
#endif
                {
                    // Get the indexes ready for reading
                    var idx = 0;
                    var p32Idx = idxPtr.ToIntPointer();
                    var p16Idx = idxPtr.ToShortPointer();

                    for (j = 0; j < iData.indexCount / 3; j++)
                    {
                        if (iData.indexBuffer.Type == IndexType.Size32)
                        {
                            log.Write("Triangle {0}: ({1}, {2}, {3})", j, p32Idx[idx++], p32Idx[idx++], p32Idx[idx++]);
                        }
                        else
                        {
                            log.Write("Triangle {0}: ({1}, {2}, {3})", j, p16Idx[idx++], p16Idx[idx++], p16Idx[idx++]);
                        }
                    }
                }

                iData.indexBuffer.Unlock();

                // Log common vertex list
                log.Write(".");
                log.Write("Common vertex list - vertex count {0}", this.vertices.Count);

                for (i = 0; i < this.vertices.Count; i++)
                {
                    var c = (CommonVertex)this.vertices[i];

                    log.Write("Common vertex {0}: (vertexSet={1}, originalIndex={2}, position={3}", i, c.vertexSet, c.index,
                               c.position);
                }
            }
        }

        #endregion Methods

        #region Structs

        /// <summary>
        ///     A vertex can actually represent several vertices in the final model, because
        ///     vertices along texture seams etc will have been duplicated. In order to properly
        ///     evaluate the surface properties, a single common vertex is used for these duplicates,
        ///     and the faces hold the detail of the duplicated vertices.
        /// </summary>
        public struct CommonVertex
        {
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

            /// <summary>
            ///     The index set this was referenced (first) from.
            /// </summary>
            public int indexSet;

            /// <summary>
            ///     Place of vertex in original vertex set.
            /// </summary>
            public int originalIndex;
        }

        public struct UniqueEdge
        {
            public int vertexIndex1;
            public int vertexIndex2;
        }

        public class CommonVertexList : List<CommonVertex>
        {
        }

        public class UniqueEdgeList : List<UniqueEdge>
        {
        }

        #endregion Structs
    }
}