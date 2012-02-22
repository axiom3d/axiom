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

using Axiom.Collections;
using Axiom.Core;
using Axiom.Math;
using Axiom.Core.Collections;
using Axiom.Graphics.Collections;

#endregion Namespace Declarations

namespace Axiom.Graphics
{
	/// <summary>
	/// Base class for classes that iterate over the vertices in a mesh
	/// </summary>
	public class AnyBuilder : DisposableObject
	{
		#region Fields

		/// <summary>
		/// List of objects that will provide index data to the build process.
		/// </summary>
		protected IndexDataList indexDataList = new IndexDataList();
		/// <summary>
		/// Mapping of index data sets to vertex data sets.
		/// </summary>
		protected IntList indexDataVertexDataSetList = new IntList();
		/// <summary>
		/// List of vertex data objects.
		/// </summary>
		protected VertexDataList vertexDataList = new VertexDataList();
		/// <summary>
		/// Mappings of operation type to vertex data.
		/// </summary>
		protected OperationTypeList operationTypes = new OperationTypeList();
		/// <summary>
		/// List of software index buffers that were created and to be disposed by this class.
		/// </summary>
		protected List<DefaultHardwareIndexBuffer> customIndexBufferList = new List<DefaultHardwareIndexBuffer>();

		#endregion Fields

		#region Methods

		/// <summary>
		/// Add a set of vertex geometry data to the edge builder.
		/// </summary>
		/// <remarks>
		/// You must add at least one set of vertex data to the builder before invoking the
		/// <see name="Build"/> method.
		/// </remarks>
		/// <param name="vertexData">Vertex data to consider for edge detection.</param>
		public void AddVertexData( VertexData vertexData )
		{
			vertexDataList.Add( vertexData );
		}

		/// <summary>
		/// Add a set of index geometry data to the edge builder.
		/// </summary>
		/// <remarks>
		/// You must add at least one set of index data to the builder before invoking the
        /// <see name="Build"/> method.
		/// </remarks>
		/// <param name="indexData">The index information which describes the triangles.</param>
		public void AddIndexData( IndexData indexData )
		{
			AddIndexData( indexData, 0, OperationType.TriangleList );
		}

		public void AddIndexData( IndexData indexData, int vertexSet )
		{
			AddIndexData( indexData, vertexSet, OperationType.TriangleList );
		}

	    /// <summary>
	    /// Add a set of index geometry data to the edge builder.
	    /// </summary>
	    /// <remarks>
	    /// You must add at least one set of index data to the builder before invoking the
	    /// <see name="Build"/> method.
	    /// </remarks>
	    /// <param name="indexData">The index information which describes the triangles.</param>
	    /// <param name="vertexSet">
	    /// The vertex data set this index data refers to; you only need to alter this
	    /// if you have added multiple sets of vertices.
	    /// </param>
	    /// <param name="opType"></param>
	    public void AddIndexData( IndexData indexData, int vertexSet, OperationType opType )
		{
			indexDataList.Add( indexData );
			indexDataVertexDataSetList.Add( vertexSet );
			operationTypes.Add( opType );
		}

		/// <summary>
		/// Populate with data as obtained from an IRenderable.
        /// </summary>
		/// <remarks>
		/// Will share the buffers.
		/// In case there are no index data associated with the <see cref="IRenderable"/>, i.e. <see cref="RenderOperation.useIndices"/> is false,
		/// custom software index buffer is created to provide default index data to the builder.
		/// This makes it possible for derived classes to handle the data in a convenient way.
		/// </remarks>
		public void AddObject( IRenderable obj )
		{
			if ( obj == null )
				throw new ArgumentNullException();

			var renderOp = obj.RenderOperation;

			IndexData indexData;
			if ( renderOp.useIndices )
			{
				indexData = renderOp.indexData;
			}
			else
			{
				//Create custom index buffer
				var vertexCount = renderOp.vertexData.vertexCount;
				var itype = vertexCount > UInt16.MaxValue ?
				IndexType.Size32 : IndexType.Size16;

				var ibuf = new DefaultHardwareIndexBuffer( itype, vertexCount, BufferUsage.Static );
				customIndexBufferList.Add( ibuf ); //to be disposed later

				indexData = new IndexData();
				indexData.indexBuffer = ibuf;
				indexData.indexCount = vertexCount;
				indexData.indexStart = 0;

				//Fill buffer with indices
                var ibuffer = indexData.indexBuffer.Lock( BufferLocking.Normal );
				try
				{
#if !AXIOM_SAFE_ONLY
                    unsafe
#endif
                    {
						var ibuf16 = ibuffer.ToShortPointer();
						var ibuf32 = ibuffer.ToIntPointer();
						for ( var i = 0; i < indexData.indexCount; i++ )
						{
							if ( itype == IndexType.Size16 )
							{
								ibuf16[ i ] = (Int16)i;
							}
							else
							{
								ibuf32[ i ] = i;
							}
						}
					} //unsafe
				}
				finally
				{
					indexData.indexBuffer.Unlock();
				}
			}

			AddVertexData( renderOp.vertexData );
			AddIndexData( indexData, vertexDataList.Count - 1,
			renderOp.operationType );
		}


		/// <summary>
		/// Add vertex and index sets of a mesh to the builder.
		/// </summary>
		/// <param name="mesh">The mesh object.</param>
		/// <param name="lodIndex">The LOD level to be processed.</param>
		public void AddObject( Mesh mesh, int lodIndex )
		{
			if ( mesh == null )
				throw new ArgumentNullException();

			//mesh.AddVertexAndIndexSets(this, lodIndex);

			//NOTE: The Mesh.AddVertexAndIndexSets() assumes there weren't any vertex data added to the builder yet.
			//For this class I decided to break that assumption, thus using following replacement for the commented call.
			//I guess rarely, but still you might want to add vertex/index sets of several objects to one builder.
			//Maybe AddVertexAndIndexSets could be removed and this method utilized back in Mesh.

			//TODO: find out whether custom index buffer needs to be created in cases (like in the AddObject(IRenderable)).
			//Borrilis, do you know?

			var vertexSetCount = vertexDataList.Count;
			var indexOfSharedVertexSet = vertexSetCount;

			if ( mesh.SharedVertexData != null )
			{
				AddVertexData( mesh.SharedVertexData );
				vertexSetCount++;
			}

			// Prepare the builder using the submesh information
			for ( var i = 0; i < mesh.SubMeshCount; i++ )
			{
				var sm = mesh.GetSubMesh( i );

				if ( sm.useSharedVertices )
				{
					// Use shared vertex data
					if ( lodIndex == 0 )
					{
						AddIndexData( sm.IndexData, indexOfSharedVertexSet,
						sm.OperationType );
					}
					else
					{
						AddIndexData( sm.LodFaceList[ lodIndex - 1 ],
						indexOfSharedVertexSet, sm.OperationType );
					}
				}
				else
				{
					// own vertex data, add it and reference it directly
					AddVertexData( sm.VertexData );

					if ( lodIndex == 0 )
					{
						// base index data
						AddIndexData( sm.IndexData, vertexSetCount++,
						sm.OperationType );
					}
					else
					{
						// LOD index data
						AddIndexData( sm.LodFaceList[ lodIndex - 1 ],
						vertexSetCount++, sm.OperationType );
					}
				}
			}
		}

		#endregion Methods

		#region IDisposable Implementation
        /// <summary>
        /// 
        /// </summary>
        /// <param name="disposeManagedResources"></param>
        protected override void dispose(bool disposeManagedResources)
        {
			if ( !this.IsDisposed )
			{
				if ( disposeManagedResources )
				{
					foreach ( var buf in customIndexBufferList )
					{
                        buf.SafeDispose();
                        DefaultHardwareBufferManager.Instance.NotifyIndexBufferDestroyed( buf );
					}
				}
			}
		}

		#endregion

	}
}

