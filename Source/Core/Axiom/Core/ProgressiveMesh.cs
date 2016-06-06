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

// The underlying algorithms in this class are based heavily on:
/*
 *  Progressive Mesh type Polygon Reduction Algorithm
 *  by Stan Melax (c) 1998
 */

#endregion

#region SVN Version Information

// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Axiom.Animating;
using Axiom.Collections;
using Axiom.Configuration;
using Axiom.Math;
using Axiom.Serialization;
using Axiom.Graphics;

#endregion Namespace Declarations

namespace Axiom.Core
{
	/// <summary>
	///    Class for handling multiple levels of detail for a Mesh
	/// </summary>
	/// <remarks>
	///    This class reduces the complexity of the geometry it is given.
	///    This class is dedicated to reducing the number of triangles in a given mesh
	///    taking into account seams in both geometry and texture co-ordinates and meshes 
	///    which have multiple frames.
	///    <para/>
	///    The primary use for this is generating LOD versions of Mesh objects, but it can be
	///    used by any geometry provider. The only limitation at the moment is that the 
	///    provider uses a common vertex buffer for all LODs and one index buffer per LOD.
	///    Therefore at the moment this class can only handle indexed geometry.
	///    <para/>
	///    NB the interface of this class will certainly change when compiled vertex buffers are
	///    supported.
	/// </remarks>
	public class ProgressiveMesh
	{
		public enum VertexReductionQuota
		{
			Constant,
			Proportional
		}

		/// <summary>
		///    A vertex as used by a face. This records the index of the actual vertex which is used
		///    by the face, and a pointer to the common vertex used for surface evaluation.
		/// </summary>
		internal class PMFaceVertex
		{
			internal uint realIndex;
			internal PMVertex commonVertex;
		}

		/// <summary>
		///    A triangle in the progressive mesh, holds extra info like face normal.
		/// </summary>
		internal class PMTriangle
		{
			internal void SetDetails( uint index, PMFaceVertex v0, PMFaceVertex v1, PMFaceVertex v2 )
			{
				Debug.Assert( v0 != v1 && v1 != v2 && v2 != v0 );
				this.index = index;
				this.vertex[ 0 ] = v0;
				this.vertex[ 1 ] = v1;
				this.vertex[ 2 ] = v2;
				ComputeNormal();
				// Add tri to vertices
				// Also tell vertices they are neighbours
				for ( var i = 0; i < 3; i++ )
				{
					this.vertex[ i ].commonVertex.faces.Add( this );
					for ( var j = 0; j < 3; j++ )
					{
						if ( i != j )
						{
							this.vertex[ i ].commonVertex.AddIfNonNeighbor( this.vertex[ j ].commonVertex );
						}
					}
				}
			}

			private void ComputeNormal()
			{
				var v0 = this.vertex[ 0 ].commonVertex.position;
				var v1 = this.vertex[ 1 ].commonVertex.position;
				var v2 = this.vertex[ 2 ].commonVertex.position;
				// Cross-product 2 edges
				var e1 = v1 - v0;
				var e2 = v2 - v1;

				this.normal = e1.Cross( e2 );
				this.normal.Normalize();
			}

			internal void ReplaceVertex( PMFaceVertex vold, PMFaceVertex vnew )
			{
				Debug.Assert( vold == this.vertex[ 0 ] || vold == this.vertex[ 1 ] || vold == this.vertex[ 2 ] );
				Debug.Assert( vnew != this.vertex[ 0 ] && vnew != this.vertex[ 1 ] && vnew != this.vertex[ 2 ] );
				if ( vold == this.vertex[ 0 ] )
				{
					this.vertex[ 0 ] = vnew;
				}
				else if ( vold == this.vertex[ 1 ] )
				{
					this.vertex[ 1 ] = vnew;
				}
				else
				{
					this.vertex[ 2 ] = vnew;
				}
				vold.commonVertex.faces.Remove( this );
				vnew.commonVertex.faces.Add( this );
				for ( var i = 0; i < 3; i++ )
				{
					vold.commonVertex.RemoveIfNonNeighbor( this.vertex[ i ].commonVertex );
					this.vertex[ i ].commonVertex.RemoveIfNonNeighbor( vold.commonVertex );
				}
				for ( var i = 0; i < 3; i++ )
				{
					Debug.Assert( this.vertex[ i ].commonVertex.faces.Contains( this ) );
					for ( var j = 0; j < 3; j++ )
					{
						if ( i != j )
						{
							this.vertex[ i ].commonVertex.AddIfNonNeighbor( this.vertex[ j ].commonVertex );
						}
					}
				}
				ComputeNormal();
			}

			internal bool HasCommonVertex( PMVertex v )
			{
				return ( v == this.vertex[ 0 ].commonVertex || v == this.vertex[ 1 ].commonVertex ||
				         v == this.vertex[ 2 ].commonVertex );
			}

			private bool HasFaceVertex( PMFaceVertex v )
			{
				return ( v == this.vertex[ 0 ] || v == this.vertex[ 1 ] || v == this.vertex[ 2 ] );
			}

			internal PMFaceVertex GetFaceVertexFromCommon( PMVertex commonVert )
			{
				if ( this.vertex[ 0 ].commonVertex == commonVert )
				{
					return this.vertex[ 0 ];
				}
				if ( this.vertex[ 1 ].commonVertex == commonVert )
				{
					return this.vertex[ 1 ];
				}
				if ( this.vertex[ 2 ].commonVertex == commonVert )
				{
					return this.vertex[ 2 ];
				}

				return null;
			}

			internal void NotifyRemoved()
			{
				for ( var i = 0; i < 3; i++ )
				{
					// remove this tri from the vertices
					if ( this.vertex[ i ] != null )
					{
						this.vertex[ i ].commonVertex.faces.Remove( this );
					}
				}
				for ( var i = 0; i < 3; i++ )
				{
					var i2 = ( i + 1 )%3;
					if ( this.vertex[ i ] == null || this.vertex[ i2 ] == null )
					{
						continue;
					}
					// Check remaining vertices and remove if not neighbours anymore
					// NB May remain neighbours if other tris link them
					this.vertex[ i ].commonVertex.RemoveIfNonNeighbor( this.vertex[ i2 ].commonVertex );
					this.vertex[ i2 ].commonVertex.RemoveIfNonNeighbor( this.vertex[ i ].commonVertex );
				}

				this.removed = true;
			}

			internal PMFaceVertex[] vertex = new PMFaceVertex[3]; // the 3 points that make this tri
			internal Vector3 normal; // unit vector othogonal to this face
			internal bool removed = false; // true if this tri is now removed
			private uint index;
		}

		/// <summary>
		///    A vertex in the progressive mesh, holds info like collapse cost etc. 
		///    This vertex can actually represent several vertices in the final model, because
		///    vertices along texture seams etc will have been duplicated. In order to properly
		///    evaluate the surface properties, a single common vertex is used for these duplicates,
		///    and the faces hold the detail of the duplicated vertices.
		/// </summary>
		internal class PMVertex
		{
			#region Methods

			private void SetDetails( ref Vector3 v, uint index )
			{
				this.position = v;
				this.index = index;
			}

			internal void RemoveIfNonNeighbor( PMVertex n )
			{
				// removes n from neighbor list if n isn't a neighbor.
				if ( !this.neighbors.Contains( n ) )
				{
					return; // Not in neighbor list anyway
				}

				foreach ( var face in this.faces )
				{
					if ( face.HasCommonVertex( n ) )
					{
						return; // Still a neighbor
					}
				}
				this.neighbors.Remove( n );

				if ( this.neighbors.Count == 0 && !this.toBeRemoved )
				{
					// This vertex has been removed through isolation (collapsing around it)
					NotifyRemoved();
				}
			}

			internal void AddIfNonNeighbor( PMVertex n )
			{
				if ( this.neighbors.Contains( n ) )
				{
					return; // Already in neighbor list
				}
				this.neighbors.Add( n );
			}


			// is edge this->src a manifold edge?
			internal bool IsManifoldEdgeWith( PMVertex v )
			{
				// Check the sides involving both these verts
				// If there is only 1 this is a manifold edge
				ushort sidesCount = 0;
				foreach ( var face in this.faces )
				{
					if ( face.HasCommonVertex( v ) )
					{
						sidesCount++;
					}
				}

				return ( sidesCount == 1 );
			}

			internal void NotifyRemoved()
			{
				foreach ( var vertex in this.neighbors )
				{
					// Remove me from neighbor
					vertex.neighbors.Remove( this );
				}
				this.removed = true;
				this.collapseTo = null;
				this.collapseCost = float.MaxValue;
			}

			#endregion

			#region Properties

			/// <summary>
			///    Determine if this vertex is on the edge of an open geometry patch
			/// </summary>
			/// <returns>tru if this vertex is on the edge of an open geometry patch</returns>
			internal bool IsBorder
			{
				get
				{
					// Look for edges which only have one tri attached, this is a border

					// Loop for each neighbor
					foreach ( var neighbor in this.neighbors )
					{
						// Count of tris shared between the edge between this and neighbor
						ushort count = 0;
						// Loop over each face, looking for shared ones
						foreach ( var face in this.faces )
						{
							if ( face.HasCommonVertex( neighbor ) )
							{
								// Shared tri
								count++;
							}
						}
						// Debug.Assert(count > 0); // Must be at least one!
						// This edge has only 1 tri on it, it's a border
						if ( count == 1 )
						{
							return true;
						}
					}
					return false;
				}
			}

			#endregion

			#region Fields

			internal Vector3 position; // location of point in euclidean space
			internal uint index; // place of vertex in original list
			internal List<PMVertex> neighbors = new List<PMVertex>(); // adjacent vertices
			internal List<PMTriangle> faces = new List<PMTriangle>(); // adjacent triangles

			internal float collapseCost; // cached cost of collapsing edge
			internal PMVertex collapseTo; // candidate vertex for collapse
			internal bool removed = false; // true if this vert is now removed
			internal bool toBeRemoved; // debug

			internal bool seam;

			/// true if this vertex is on a model seam where vertices are duplicated

			#endregion
			internal void SetDetails( Vector3 pos, uint numCommon )
			{
				this.position = pos;
				this.index = numCommon;
			}
		}

		private class PMWorkingData
		{
			internal PMTriangle[] triList = null;
			internal PMFaceVertex[] faceVertList = null;
			internal PMVertex[] vertList = null;
		}

		#region Fields

		private readonly VertexData vertexData;
		private readonly IndexData indexData;
		private uint currNumIndexes;
		private uint numCommonVertices;

		/// Multiple copies, 1 per vertex buffer
		private readonly List<PMWorkingData> workingDataList = new List<PMWorkingData>();

		/// The worst collapse cost from all vertex buffers for each vertex
		private float[] worstCosts;

		#endregion

		/// <summary>
		///    Constructor, takes the geometry data and index buffer. 
		/// </summary>
		/// <remarks>
		///    DO NOT pass write-only, unshadowed buffers to this method! They will not
		///    work. Pass only shadowed buffers, or better yet perform mesh reduction as
		///    an offline process using DefaultHardwareBufferManager to manage vertex
		///    buffers in system memory.
		/// </remarks>
		/// <param name="vertexData"></param>
		/// <param name="indexData"></param>
		public ProgressiveMesh( VertexData vertexData, IndexData indexData )
		{
			AddWorkingData( vertexData, indexData );
			this.vertexData = vertexData;
			this.indexData = indexData;
		}

		/// <summary>
		///    Adds an extra vertex position buffer. 
		/// </summary>
		/// <remarks>
		///    As well as the main vertex buffer, the client of this class may add extra versions
		///    of the vertex buffer which will also be taken into account when the cost of 
		///    simplifying the mesh is taken into account. This is because the cost of
		///    simplifying an animated mesh cannot be calculated from just the reference position,
		///    multiple positions needs to be assessed in order to find the best simplification option.
		///    <p/>
		///    DO NOT pass write-only, unshadowed buffers to this method! They will not
		///    work. Pass only shadowed buffers, or better yet perform mesh reduction as
		///    an offline process using DefaultHardwareBufferManager to manage vertex
		///    buffers in system memory.
		/// </remarks>
		/// <param name="vertexData">buffer Pointer to x/y/z buffer with vertex positions.
		///    The number of vertices must be the same as in the original GeometryData passed to the constructor.
		/// </param>
		public void AddExtraVertexPositionBuffer( VertexData vertexData )
		{
			AddWorkingData( vertexData, this.indexData );
		}


		public void Build( ushort numLevels, List<IndexData> lodFaceList )
		{
			Build( numLevels, lodFaceList, VertexReductionQuota.Proportional );
		}

		public void Build( ushort numLevels, List<IndexData> lodFaceList, VertexReductionQuota quota )
		{
			Build( numLevels, lodFaceList, quota, 0.5f );
		}

		public void Build( ushort numLevels, List<IndexData> lodFaceList, float reductionValue )
		{
			Build( numLevels, lodFaceList, VertexReductionQuota.Proportional, reductionValue );
		}

		/// <summary>
		///    Builds the progressive mesh with the specified number of levels.
		/// </summary>
		public void Build( ushort numLevels, List<IndexData> lodFaceList, VertexReductionQuota quota, float reductionValue )
		{
			ComputeAllCosts();
			// Init
			this.currNumIndexes = (uint)this.indexData.indexCount;
			// Use COMMON vert count, not original vert count
			// Since collapsing 1 common vert position is equivalent to collapsing them all
			var numVerts = this.numCommonVertices;

			uint numCollapses = 0;
			var abandon = false;

			while ( numLevels-- != 0 )
			{
				// NB if 'abandon' is set, we stop reducing 
				// However, we still bake the number of LODs requested, even if it 
				// means they are the same
				if ( !abandon )
				{
					if ( quota == VertexReductionQuota.Proportional )
					{
						numCollapses = (uint)( numVerts*reductionValue );
					}
					else
					{
						numCollapses = (uint)reductionValue;
					}
					// Minimum 3 verts!
					if ( ( numVerts - numCollapses ) < 3 )
					{
						numCollapses = numVerts - 3;
					}
					// Store new number of verts
					numVerts = numVerts - numCollapses;

					Debug.Assert( numVerts >= 3 );
					while ( numCollapses-- != 0 && !abandon )
					{
						var nextIndex = GetNextCollapser();
						// Collapse on every buffer
						foreach ( var data in this.workingDataList )
						{
							var collapser = data.vertList[ nextIndex ];
							// This will reduce currNumIndexes and recalc costs as required
							if ( collapser.collapseTo == null )
							{
								// Must have run out of valid collapsables
								abandon = true;
								break;
							}
							Debug.Assert( collapser.collapseTo.removed == false );

							Collapse( collapser );
						}
					}
				}


				// Bake a new LOD and add it to the list
				var newLod = new IndexData();
				BakeNewLOD( newLod );
				lodFaceList.Add( newLod );
			}
		}

		#region protected methods

		/// <summary>
		///    Internal method for building PMWorkingData from geometry data
		/// </summary>
		private void AddWorkingData( VertexData vertexData, IndexData indexData )
		{
			// Insert blank working data, then fill
			var work = new PMWorkingData();
			this.workingDataList.Add( work );

			// Build vertex list
			// Resize face list (this will always be this big)
			work.faceVertList = new PMFaceVertex[vertexData.vertexCount];
			// Also resize common vert list to max, to avoid reallocations
			work.vertList = new PMVertex[vertexData.vertexCount];

			// locate position element & the buffer to go with it
			var posElem = vertexData.vertexDeclaration.FindElementBySemantic( VertexElementSemantic.Position );
			var vbuf = vertexData.vertexBufferBinding.GetBuffer( posElem.Source );

			// lock the buffer for reading
			var bufPtr = vbuf.Lock( BufferLocking.ReadOnly );

			uint numCommon = 0;
#if !AXIOM_SAFE_ONLY
			unsafe
#endif
			{
				var pVertex = bufPtr;

				Vector3 pos;
				// Map for identifying duplicate position vertices
				var commonVertexMap = new Dictionary<Vector3, uint>();
				for ( uint i = 0; i < vertexData.vertexCount; ++i, pVertex += vbuf.VertexSize )
				{
					var pFloat = ( pVertex + posElem.Offset ).ToFloatPointer();
					pos.x = pFloat[ 0 ];
					pos.y = pFloat[ 1 ];
					pos.z = pFloat[ 2 ];

					work.faceVertList[ (int)i ] = new PMFaceVertex();

					// Try to find this position in the existing map 
					if ( !commonVertexMap.ContainsKey( pos ) )
					{
						// Doesn't exist, so create it
						var commonVert = new PMVertex();
						commonVert.SetDetails( pos, numCommon );
						commonVert.removed = false;
						commonVert.toBeRemoved = false;
						commonVert.seam = false;
						// Add it to our working set
						work.vertList[ (int)numCommon ] = commonVert;

						// Enter it in the map
						commonVertexMap.Add( pos, numCommon );
						// Increment common index
						++numCommon;

						work.faceVertList[ (int)i ].commonVertex = commonVert;
						work.faceVertList[ (int)i ].realIndex = i;
					}
					else
					{
						// Exists already, reference it
						var existingVert = work.vertList[ (int)commonVertexMap[ pos ] ];

						work.faceVertList[ (int)i ].commonVertex = existingVert;
						work.faceVertList[ (int)i ].realIndex = i;

						// Also tag original as a seam since duplicates at this location
						work.faceVertList[ (int)i ].commonVertex.seam = true;
					}
				}
			}
			vbuf.Unlock();

			this.numCommonVertices = numCommon;

			// Build tri list
			var numTris = (uint)indexData.indexCount/3;
			var ibuf = indexData.indexBuffer;
			var use32bitindexes = ( ibuf.Type == IndexType.Size32 );
			var indexBufferPtr = ibuf.Lock( BufferLocking.ReadOnly );
#if !AXIOM_SAFE_ONLY
			unsafe
#endif
			{
				var pInt = indexBufferPtr.ToUIntPointer();
				var pShort = indexBufferPtr.ToUShortPointer();
				var idx = 0;
				work.triList = new PMTriangle[(int)numTris]; // assumed tri list
				for ( uint i = 0; i < numTris; ++i )
				{
					// use 32-bit index always since we're not storing
					var vindex = use32bitindexes ? pInt[ idx++ ] : pShort[ idx++ ];
					var v0 = work.faceVertList[ (int)vindex ];
					vindex = use32bitindexes ? pInt[ idx++ ] : pShort[ idx++ ];
					var v1 = work.faceVertList[ (int)vindex ];
					vindex = use32bitindexes ? pInt[ idx++ ] : pShort[ idx++ ];
					var v2 = work.faceVertList[ (int)vindex ];

					work.triList[ (int)i ] = new PMTriangle();
					work.triList[ (int)i ].SetDetails( i, v0, v1, v2 );
					work.triList[ (int)i ].removed = false;
				}
			}
			ibuf.Unlock();
		}

		/// Internal method for initialising the edge collapse costs
		private void InitialiseEdgeCollapseCosts()
		{
			this.worstCosts = new float[this.vertexData.vertexCount];
			foreach ( var data in this.workingDataList )
			{
				for ( var i = 0; i < data.vertList.Length; ++i )
				{
					// Typically, at the end, there are a bunch of null entries to represent vertices
					// that were common.  Make sure we have a vertex here
					if ( data.vertList[ i ] == null )
					{
						data.vertList[ i ] = new PMVertex();
					}
					var vertex = data.vertList[ i ];
					vertex.collapseTo = null;
					vertex.collapseCost = float.MaxValue;
				}
			}
		}

		/// Internal calculation method for deriving a collapse cost  from u to v
		private float ComputeEdgeCollapseCost( PMVertex src, PMVertex dest )
		{
			// if we collapse edge uv by moving src to dest then how 
			// much different will the model change, i.e. how much "error".
			// The method of determining cost was designed in order 
			// to exploit small and coplanar regions for
			// effective polygon reduction.
			var edgeVector = src.position - dest.position;

			float cost;
			var curvature = 0.001f;

			// find the "sides" triangles that are on the edge uv
			var sides = new List<PMTriangle>();
			// Iterate over src's faces and find 'sides' of the shared edge which is being collapsed
			foreach ( var srcFace in src.faces )
			{
				// Check if this tri also has dest in it (shared edge)
				if ( srcFace.HasCommonVertex( dest ) )
				{
					sides.Add( srcFace );
				}
			}
			// Special cases
			// If we're looking at a border vertex

			if ( src.IsBorder )
			{
				if ( sides.Count > 1 )
				{
					// src is on a border, but the src-dest edge has more than one tri on it
					// So it must be collapsing inwards
					// Mark as very high-value cost
					// curvature = 1.0f;
					cost = 1.0f;
				}
				else
				{
					// Collapsing ALONG a border
					// We can't use curvature to measure the effect on the model
					// Instead, see what effect it has on 'pulling' the other border edges
					// The more colinear, the less effect it will have
					// So measure the 'kinkiness' (for want of a better term)
					// Normally there can be at most 1 other border edge attached to this
					// However in weird cases there may be more, so find the worst
					Vector3 collapseEdge, otherBorderEdge;
					float kinkiness, maxKinkiness;
					maxKinkiness = 0.0f;
					edgeVector.Normalize();
					collapseEdge = edgeVector;
					foreach ( var neighbor in src.neighbors )
					{
						if ( neighbor != dest && neighbor.IsManifoldEdgeWith( src ) )
						{
							otherBorderEdge = src.position - neighbor.position;
							otherBorderEdge.Normalize();
							// This time, the nearer the dot is to -1, the better, because that means
							// the edges are opposite each other, therefore less kinkiness
							// Scale into [0..1]
							kinkiness = ( otherBorderEdge.Dot( collapseEdge ) + 1.002f )*0.5f;
							maxKinkiness = Utility.Max( kinkiness, maxKinkiness );
						}
					}

					cost = maxKinkiness;
				}
			}
			else // not a border
			{
				// Standard inner vertex
				// Calculate curvature
				// use the triangle facing most away from the sides 
				// to determine our curvature term
				// Iterate over src's faces again
				foreach ( var srcFace in src.faces )
				{
					var mincurv = 1.0f; // curve for face i and closer side to it
					// Iterate over the sides
					foreach ( var sideFace in sides )
					{
						// Dot product of face normal gives a good delta angle
						float dotprod = srcFace.normal.Dot( sideFace.normal );
						// NB we do (1-..) to invert curvature where 1 is high curvature [0..1]
						// Whilst dot product is high when angle difference is low
						mincurv = Utility.Min( mincurv, ( 1.002f - dotprod )*0.5f );
					}
					curvature = Utility.Max( curvature, mincurv );
				}
				cost = curvature;
			}

			// check for texture seam ripping
			if ( src.seam && !dest.seam )
			{
				cost = 1.0f;
			}

			// Check for singular triangle destruction
			// If src and dest both only have 1 triangle (and it must be a shared one)
			// then this would destroy the shape, so don't do this
			if ( src.faces.Count == 1 && dest.faces.Count == 1 )
			{
				cost = float.MaxValue;
			}


			// Degenerate case check
			// Are we going to invert a face normal of one of the neighbouring faces?
			// Can occur when we have a very small remaining edge and collapse crosses it
			// Look for a face normal changing by > 90 degrees
			foreach ( var srcFace in src.faces )
			{
				// Ignore the deleted faces (those including src & dest)
				if ( !srcFace.HasCommonVertex( dest ) )
				{
					// Test the new face normal
					PMVertex v0, v1, v2;
					// Replace src with dest wherever it is
					v0 = ( srcFace.vertex[ 0 ].commonVertex == src ) ? dest : srcFace.vertex[ 0 ].commonVertex;
					v1 = ( srcFace.vertex[ 1 ].commonVertex == src ) ? dest : srcFace.vertex[ 1 ].commonVertex;
					v2 = ( srcFace.vertex[ 2 ].commonVertex == src ) ? dest : srcFace.vertex[ 2 ].commonVertex;

					// Cross-product 2 edges
					var e1 = v1.position - v0.position;
					var e2 = v2.position - v1.position;

					var newNormal = e1.Cross( e2 );
					newNormal.Normalize();

					// Dot old and new face normal
					// If < 0 then more than 90 degree difference
					if ( newNormal.Dot( srcFace.normal ) < 0.0f )
					{
						// Don't do it!
						cost = float.MaxValue;
						break; // No point continuing
					}
				}
			}

			Debug.Assert( cost >= 0 );
			return cost;
		}


		/// <summary>
		///    Internal method evaluates all collapse costs from this vertex and picks the lowest for a single buffer
		/// </summary>
		private float ComputeEdgeCostAtVertexForBuffer( PMWorkingData workingData, uint vertIndex )
		{
			// compute the edge collapse cost for all edges that start
			// from vertex v.  Since we are only interested in reducing
			// the object by selecting the min cost edge at each step, we
			// only cache the cost of the least cost edge at this vertex
			// (in member variable collapse) as well as the value of the 
			// cost (in member variable objdist).

			var v = workingData.vertList[ (int)vertIndex ];

			if ( v.neighbors.Count == 0 )
			{
				// v doesn't have neighbors so nothing to collapse
				v.NotifyRemoved();
				return v.collapseCost;
			}

			// Init metrics
			v.collapseCost = float.MaxValue;
			v.collapseTo = null;

			// search all neighboring edges for "least cost" edge
			foreach ( var neighbor in v.neighbors )
			{
				var cost = ComputeEdgeCollapseCost( v, neighbor );
				if ( ( v.collapseTo == null ) || cost < v.collapseCost )
				{
					v.collapseTo = neighbor; // candidate for edge collapse
					v.collapseCost = cost; // cost of the collapse
				}
			}

			return v.collapseCost;
		}

		/// Internal method evaluates all collapse costs from this vertex for every buffer and returns the worst
		private void ComputeEdgeCostAtVertex( uint vertIndex )
		{
			// Call computer for each buffer on this vertex
			var worstCost = -0.01f;
			foreach ( var data in this.workingDataList )
			{
				worstCost = Utility.Max( worstCost, ComputeEdgeCostAtVertexForBuffer( data, vertIndex ) );
			}
			this.worstCosts[ (int)vertIndex ] = worstCost;
		}

		/// Internal method to compute edge collapse costs for all buffers /
		private void ComputeAllCosts()
		{
			InitialiseEdgeCollapseCosts();
			for ( uint i = 0; i < this.vertexData.vertexCount; ++i )
			{
				ComputeEdgeCostAtVertex( i );
			}
		}

		/// Internal method for getting the index of next best vertex to collapse
		private int GetNextCollapser()
		{
			// Scan
			// Not done as a sort because want to keep the lookup simple for now
			var bestVal = float.MaxValue;
			var bestIndex = 0; // NB this is ok since if nothing is better than this, nothing will collapse
			for ( var i = 0; i < this.numCommonVertices; ++i )
			{
				if ( this.worstCosts[ i ] < bestVal )
				{
					bestVal = this.worstCosts[ i ];
					bestIndex = i;
				}
			}
			return bestIndex;
		}

		/// <summary>
		///    Internal method builds an new LOD based on the current state
		/// </summary>
		/// <param name="indexData">Index data which will have an index buffer created and initialized</param>
		private void BakeNewLOD( IndexData indexData )
		{
			Debug.Assert( this.currNumIndexes > 0, "No triangles to bake!" );
			// Zip through the tri list of any working data copy and bake
			indexData.indexCount = (int)this.currNumIndexes;
			indexData.indexStart = 0;
			// Base size of indexes on original 
			var use32bitindexes = ( this.indexData.indexBuffer.Type == IndexType.Size32 );

			// Create index buffer, we don't need to read it back or modify it a lot
			indexData.indexBuffer = HardwareBufferManager.Instance.CreateIndexBuffer( this.indexData.indexBuffer.Type,
			                                                                          indexData.indexCount,
			                                                                          BufferUsage.StaticWriteOnly, false );

			var bufPtr = indexData.indexBuffer.Lock( BufferLocking.Discard );

#if !AXIOM_SAFE_ONLY
			unsafe
#endif
			{
				var idx = 0;
				var pShort = bufPtr.ToUShortPointer();
				var pInt = bufPtr.ToUIntPointer();
				// Use the first working data buffer, they are all the same index-wise
				var work = this.workingDataList[ 0 ];
				foreach ( var tri in work.triList )
				{
					if ( !tri.removed )
					{
						if ( use32bitindexes )
						{
							pInt[ idx++ ] = tri.vertex[ 0 ].realIndex;
							pInt[ idx++ ] = tri.vertex[ 1 ].realIndex;
							pInt[ idx++ ] = tri.vertex[ 2 ].realIndex;
						}
						else
						{
							pShort[ idx++ ] = (ushort)tri.vertex[ 0 ].realIndex;
							pShort[ idx++ ] = (ushort)tri.vertex[ 1 ].realIndex;
							pShort[ idx++ ] = (ushort)tri.vertex[ 2 ].realIndex;
						}
					}
				}
			}

			indexData.indexBuffer.Unlock();
		}

		/// <summary>
		///    Internal method, collapses vertex onto it's saved collapse target. 
		/// </summary>
		/// <remarks>
		///    This updates the working triangle list to drop a triangle and recalculates
		///    the edge collapse costs around the collapse target. 
		///    This also updates all the working vertex lists for the relevant buffer. 
		/// </remarks>
		/// <pram name="src">the collapser</pram>
		private void Collapse( PMVertex src )
		{
			var dest = src.collapseTo;
			var recomputeSet = new List<PMVertex>();

			// Abort if we're never supposed to collapse
			if ( src.collapseCost == float.MaxValue )
			{
				return;
			}

			// Remove this vertex from the running for the next check
			src.collapseTo = null;
			src.collapseCost = float.MaxValue;
			this.worstCosts[ (int)src.index ] = float.MaxValue;

			// Collapse the edge uv by moving vertex u onto v
			// Actually remove tris on uv, then update tris that
			// have u to have v, and then remove u.
			if ( dest == null )
			{
				// src is a vertex all by itself 
				return;
			}

			// Add dest and all the neighbours of source and dest to recompute list
			recomputeSet.Add( dest );

			foreach ( var neighbor in src.neighbors )
			{
				if ( !recomputeSet.Contains( neighbor ) )
				{
					recomputeSet.Add( neighbor );
				}
			}
			foreach ( var neighbor in dest.neighbors )
			{
				if ( !recomputeSet.Contains( neighbor ) )
				{
					recomputeSet.Add( neighbor );
				}
			}

			// delete triangles on edge src-dest
			// Notify others to replace src with dest
			// Queue of faces for removal / replacement
			// prevents us screwing up the iterators while we parse
			var faceRemovalList = new List<PMTriangle>();
			var faceReplacementList = new List<PMTriangle>();

			foreach ( var face in src.faces )
			{
				if ( face.HasCommonVertex( dest ) )
				{
					// Tri is on src-dest therefore is gone
					faceRemovalList.Add( face );
					// Reduce index count by 3 (useful for quick allocation later)
					this.currNumIndexes -= 3;
				}
				else
				{
					// Only src involved, replace with dest
					faceReplacementList.Add( face );
				}
			}

			src.toBeRemoved = true;
			// Replace all the faces queued for replacement
			foreach ( var face in faceReplacementList )
			{
				/* Locate the face vertex which corresponds with the common 'dest' vertex
				   To to this, find a removed face which has the FACE vertex corresponding with
				   src, and use it's FACE vertex version of dest.
				*/
				var srcFaceVert = face.GetFaceVertexFromCommon( src );
				PMFaceVertex destFaceVert = null;
				foreach ( var removed in faceRemovalList )
				{
					//if (removed.HasFaceVertex(srcFaceVert))
					//{
					destFaceVert = removed.GetFaceVertexFromCommon( dest );
					//}
				}

				Debug.Assert( destFaceVert != null );

				face.ReplaceVertex( srcFaceVert, destFaceVert );
			}
			// Remove all the faces queued for removal
			foreach ( var face in faceRemovalList )
			{
				face.NotifyRemoved();
			}

			// Notify the vertex that it is gone
			src.NotifyRemoved();

			// recompute costs
			foreach ( var recomp in recomputeSet )
			{
				ComputeEdgeCostAtVertex( recomp.index );
			}
		}

		#endregion
	}
}