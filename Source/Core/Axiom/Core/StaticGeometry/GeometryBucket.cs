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

#endregion LGPL License

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
using System.Text;
using System.IO;

using Axiom.CrossPlatform;
using Axiom.Graphics;
using Axiom.Math;
using Axiom.Collections;
using Axiom.Core.Collections;

#endregion Namespace Declarations

namespace Axiom.Core
{
	public partial class StaticGeometry
	{
		///<summary>
		///    A GeometryBucket is a the lowest level bucket where geometry with
		///    the same vertex &amp; index format is stored. It also acts as the
		///    renderable.
		///</summary>
		public class GeometryBucket : DisposableObject, IRenderable
		{
			#region Fields and Properties

			// Geometry which has been queued up pre-build (not for deallocation)
			protected List<QueuedGeometry> queuedGeometry;
			// Pointer to parent bucket
			protected MaterialBucket parent;
			// String identifying the vertex / index format
			protected string formatString;
			// Vertex information, includes current number of vertices
			// committed to be a part of this bucket
			protected VertexData vertexData;
			// Index information, includes index type which limits the max
			// number of vertices which are allowed in one bucket
			protected IndexData indexData;
			// Size of indexes
			protected IndexType indexType;
			// Maximum vertex indexable
			protected int maxVertexIndex;

			protected List<Vector4> customParams = new List<Vector4>();

			public MaterialBucket Parent
			{
				get
				{
					return parent;
				}
			}

			// Get the vertex data for this geometry
			public VertexData VertexData
			{
				get
				{
					return vertexData;
				}
			}

			// Get the index data for this geometry
			public IndexData IndexData
			{
				get
				{
					return indexData;
				}
			}

			// @copydoc Renderable::getMaterial
			public Material Material
			{
				get
				{
					return parent.Material;
				}
			}

			public Technique Technique
			{
				get
				{
					return parent.CurrentTechnique;
				}
			}

			public Quaternion WorldOrientation
			{
				get
				{
					return Quaternion.Identity;
				}
			}

			public Vector3 WorldPosition
			{
				get
				{
					return parent.Parent.Parent.Center;
				}
			}

			public LightList Lights
			{
				get
				{
					return parent.Parent.Parent.Lights;
				}
			}

			public bool CastsShadows
			{
				get
				{
					return parent.Parent.Parent.CastShadows;
				}
			}

			#endregion Fields and Properties

			#region Constructors

			public GeometryBucket( MaterialBucket parent, string formatString, VertexData vData, IndexData iData )
				: base()
			{
				// Clone the structure from the example
				this.parent = parent;
				this.formatString = formatString;
				vertexData = vData.Clone( false );
				indexData = iData.Clone( false );
				vertexData.vertexCount = 0;
				vertexData.vertexStart = 0;
				indexData.indexCount = 0;
				indexData.indexStart = 0;
				indexType = indexData.indexBuffer.Type;
				queuedGeometry = new List<QueuedGeometry>();
				// Derive the max vertices
				if ( indexType == IndexType.Size32 )
				{
					maxVertexIndex = int.MaxValue;
				}
				else
				{
					maxVertexIndex = ushort.MaxValue;
				}

				// Check to see if we have blend indices / blend weights
				// remove them if so, they can try to blend non-existent bones!
				var blendIndices = vertexData.vertexDeclaration.FindElementBySemantic( VertexElementSemantic.BlendIndices );
				var blendWeights = vertexData.vertexDeclaration.FindElementBySemantic( VertexElementSemantic.BlendWeights );
				if ( blendIndices != null && blendWeights != null )
				{
					Debug.Assert( blendIndices.Source == blendWeights.Source, "Blend indices and weights should be in the same buffer" );
					// Get the source
					var source = blendIndices.Source;
					Debug.Assert( blendIndices.Size + blendWeights.Size == vertexData.vertexBufferBinding.GetBuffer( source ).VertexSize, "Blend indices and blend buffers should have buffer to themselves!" );
					// Unset the buffer
					vertexData.vertexBufferBinding.UnsetBinding( source );
					// Remove the elements
					vertexData.vertexDeclaration.RemoveElement( VertexElementSemantic.BlendIndices );
					vertexData.vertexDeclaration.RemoveElement( VertexElementSemantic.BlendWeights );
				}
			}

			#endregion Constructors

			#region Public Methods

			public Real GetSquaredViewDepth( Camera cam )
			{
				return parent.Parent.SquaredDistance;
			}

			protected RenderOperation renderOperation = new RenderOperation();

			public RenderOperation RenderOperation
			{
				get
				{
					renderOperation.indexData = this.indexData;
					renderOperation.operationType = OperationType.TriangleList;
					//op.srcRenderable = this;
					renderOperation.useIndices = true;
					renderOperation.vertexData = this.vertexData;
					return renderOperation;
				}
			}

			public void GetWorldTransforms( Matrix4[] xform )
			{
				// Should be the identity transform, but lets allow transformation of the
				// nodes the regions are attached to for kicks
				xform[ 0 ] = parent.Parent.Parent.ParentNodeFullTransform;
			}


			public bool Assign( QueuedGeometry qgeom )
			{
				// do we have enough space
				if ( vertexData.vertexCount + qgeom.geometry.vertexData.vertexCount > maxVertexIndex )
				{
					return false;
				}

				queuedGeometry.Add( qgeom );
				vertexData.vertexCount += qgeom.geometry.vertexData.vertexCount;
				indexData.indexCount += qgeom.geometry.indexData.indexCount;

				return true;
			}

			public void Build( bool stencilShadows, int logLevel )
			{
				// Ok, here's where we transfer the vertices and indexes to the shared buffers
				var dcl = vertexData.vertexDeclaration;
				var binds = vertexData.vertexBufferBinding;

				// create index buffer, and lock
				if ( logLevel <= 1 )
				{
					LogManager.Instance.Write( "GeometryBucket.Build: Creating index buffer indexType {0} indexData.indexCount {1}", indexType, indexData.indexCount );
				}
				indexData.indexBuffer = HardwareBufferManager.Instance.CreateIndexBuffer( indexType, indexData.indexCount, BufferUsage.StaticWriteOnly );
				var indexBufferIntPtr = indexData.indexBuffer.Lock( BufferLocking.Discard );

				// create all vertex buffers, and lock
				short b;
				var posBufferIdx = dcl.FindElementBySemantic( VertexElementSemantic.Position ).Source;

				var bufferElements = new List<List<VertexElement>>();

#if !AXIOM_SAFE_ONLY
				unsafe
#endif
				{
					var destBufferPtrs = new BufferBase[ binds.BindingCount ];
					for ( b = 0; b < binds.BindingCount; ++b )
					{
						var vertexCount = vertexData.vertexCount;
						if ( logLevel <= 1 )
						{
							LogManager.Instance.Write( "GeometryBucket.Build b {0}, binds.BindingCount {1}, vertexCount {2}, dcl.GetVertexSize(b) {3}", b, binds.BindingCount, vertexCount, dcl.GetVertexSize( b ) );
						}
						// Need to double the vertex count for the position buffer
						// if we're doing stencil shadows
						if ( stencilShadows && b == posBufferIdx )
						{
							vertexCount = vertexCount * 2;
							if ( vertexCount > maxVertexIndex )
							{
								throw new Exception( "Index range exceeded when using stencil shadows, consider " + "reducing your region size or reducing poly count" );
							}
						}
						var vbuf = HardwareBufferManager.Instance.CreateVertexBuffer( dcl.Clone( b ), vertexCount, BufferUsage.StaticWriteOnly );
						binds.SetBinding( b, vbuf );
						var pLock = vbuf.Lock( BufferLocking.Discard );
						destBufferPtrs[ b ] = pLock;
						// Pre-cache vertex elements per buffer
						bufferElements.Add( dcl.FindElementBySource( b ) );
					}

					// iterate over the geometry items
					var indexOffset = 0;
					IEnumerator iter = queuedGeometry.GetEnumerator();
					var regionCenter = parent.Parent.Parent.Center;
					var pDestInt = indexBufferIntPtr.ToIntPointer();
					var pDestUShort = indexBufferIntPtr.ToUShortPointer();
					foreach ( var geom in queuedGeometry )
					{
						// copy indexes across with offset
						var srcIdxData = geom.geometry.indexData;
						var srcIntPtr = srcIdxData.indexBuffer.Lock( BufferLocking.ReadOnly );
						if ( indexType == IndexType.Size32 )
						{
							var pSrcInt = srcIntPtr.ToIntPointer();
							for ( var i = 0; i < srcIdxData.indexCount; i++ )
							{
								pDestInt[ i ] = pSrcInt[ i ] + indexOffset;
							}
						}
						else
						{
							var pSrcUShort = srcIntPtr.ToUShortPointer();
							for ( var i = 0; i < srcIdxData.indexCount; i++ )
							{
								pDestUShort[ i ] = (ushort)( pSrcUShort[ i ] + indexOffset );
							}
						}

						srcIdxData.indexBuffer.Unlock();

						// Now deal with vertex buffers
						// we can rely on buffer counts / formats being the same
						var srcVData = geom.geometry.vertexData;
						var srcBinds = srcVData.vertexBufferBinding;
						for ( b = 0; b < binds.BindingCount; ++b )
						{
							// Iterate over vertices
							destBufferPtrs[ b ].Ptr = CopyVertices( srcBinds.GetBuffer( b ), destBufferPtrs[ b ], bufferElements[ b ], geom, regionCenter );
						}
						indexOffset += geom.geometry.vertexData.vertexCount;
					}
				}

				// unlock everything
				indexData.indexBuffer.Unlock();
				for ( b = 0; b < binds.BindingCount; ++b )
				{
					binds.GetBuffer( b ).Unlock();
				}

				// If we're dealing with stencil shadows, copy the position data from
				// the early half of the buffer to the latter part
				if ( stencilShadows )
				{
#if !AXIOM_SAFE_ONLY
					unsafe
#endif
					{
						var buf = binds.GetBuffer( posBufferIdx );
						var src = buf.Lock( BufferLocking.Normal );
						var pSrc = src.ToBytePointer();
						// Point dest at second half (remember vertexcount is original count)
						var pDst = ( src + ( buf.VertexSize * vertexData.vertexCount ) ).ToBytePointer();

						var count = buf.VertexSize * buf.VertexCount;
						while ( count-- > 0 )
						{
							pDst[ count ] = pSrc[ count ];
						}
						buf.Unlock();

						// Also set up hardware W buffer if appropriate
						var rend = Root.Instance.RenderSystem;
						if ( null != rend && rend.Capabilities.HasCapability( Capabilities.VertexPrograms ) )
						{
							var decl = HardwareBufferManager.Instance.CreateVertexDeclaration();
							decl.AddElement( 0, 0, VertexElementType.Float1, VertexElementSemantic.Position );
							buf = HardwareBufferManager.Instance.CreateVertexBuffer( decl, vertexData.vertexCount * 2, BufferUsage.StaticWriteOnly, false );
							// Fill the first half with 1.0, second half with 0.0
							var pbuf = buf.Lock( BufferLocking.Discard ).ToFloatPointer();
							var pW = 0;
							for ( var v = 0; v < vertexData.vertexCount; ++v )
							{
								pbuf[ pW++ ] = 1.0f;
							}
							for ( var v = 0; v < vertexData.vertexCount; ++v )
							{
								pbuf[ pW++ ] = 0.0f;
							}
							buf.Unlock();
							vertexData.hardwareShadowVolWBuffer = buf;
						}
					}
				}
			}

			public void Dump()
			{
				LogManager.Instance.Write( "Geometry Bucket" );
				LogManager.Instance.Write( "---------------" );
				LogManager.Instance.Write( "Format string: {0}", formatString );
				LogManager.Instance.Write( "Geometry items: {0}", queuedGeometry.Count );
				LogManager.Instance.Write( "Vertex count: {0}", vertexData.vertexCount );
				LogManager.Instance.Write( "Index count: {0}", indexData.indexCount );
				LogManager.Instance.Write( "---------------" );
			}

			#endregion Public Methods

			#region Protected Methods

			protected int CopyVertices( HardwareVertexBuffer srcBuf, BufferBase pDst, List<VertexElement> elems, QueuedGeometry geom, Vector3 regionCenter )
			{
#if !AXIOM_SAFE_ONLY
				unsafe
#endif
				{
					// lock source
					var src = srcBuf.Lock( BufferLocking.ReadOnly );
					var bufInc = srcBuf.VertexSize;

					var temp = Vector3.Zero;

					// Calculate elem sizes outside the loop
					var elemSizes = new int[ elems.Count ];
					for ( var i = 0; i < elems.Count; i++ )
					{
						elemSizes[ i ] = VertexElement.GetTypeSize( elems[ i ].Type );
					}

					// Move the position offset calculation outside the loop
					var positionDelta = geom.position - regionCenter;

					for ( var v = 0; v < geom.geometry.vertexData.vertexCount; ++v )
					{
						// iterate over vertex elements
						for ( var i = 0; i < elems.Count; i++ )
						{
							var elem = elems[ i ];
							var pSrcReal = ( src + elem.Offset ).ToFloatPointer();
							var pDstReal = ( pDst + elem.Offset ).ToFloatPointer();

							switch ( elem.Semantic )
							{
								case VertexElementSemantic.Position:
									temp.x = pSrcReal[ 0 ];
									temp.y = pSrcReal[ 1 ];
									temp.z = pSrcReal[ 2 ];
									// transform
									temp = ( geom.orientation * ( temp * geom.scale ) );
									pDstReal[ 0 ] = temp.x + positionDelta.x;
									pDstReal[ 1 ] = temp.y + positionDelta.y;
									pDstReal[ 2 ] = temp.z + positionDelta.z;
									break;
								case VertexElementSemantic.Normal:
								case VertexElementSemantic.Tangent:
								case VertexElementSemantic.Binormal:
									temp.x = pSrcReal[ 0 ];
									temp.y = pSrcReal[ 1 ];
									temp.z = pSrcReal[ 2 ];
									// rotation only
									temp = geom.orientation * temp;
									pDstReal[ 0 ] = temp.x;
									pDstReal[ 1 ] = temp.y;
									pDstReal[ 2 ] = temp.z;
									break;
								default:
									// just raw copy
									var size = elemSizes[ i ];
									// Optimize the loop for the case that
									// these things are in units of 4
									if ( ( size & 0x3 ) == 0x3 )
									{
										var cnt = size / 4;
										while ( cnt-- > 0 )
										{
											pDstReal[ cnt ] = pSrcReal[ cnt ];
										}
									}
									else
									{
										// Fall back to the byte-by-byte copy
										var pbSrc = ( src + elem.Offset ).ToBytePointer();
										var pbDst = ( pDst + elem.Offset ).ToBytePointer();
										while ( size-- > 0 )
										{
											pbDst[ size ] = pbSrc[ size ];
										}
									}
									break;
							}
						}

						// Increment both pointers
						pDst.Ptr += bufInc;
						src.Ptr += bufInc;
					}

					srcBuf.Unlock();
					return pDst.Ptr;
				}
			}

			#endregion Protected Methods

			#region IRenderable members

			public bool NormalizeNormals
			{
				get
				{
					return false;
				}
			}

			public ushort NumWorldTransforms
			{
				get
				{
					return parent.Parent.Parent.NumWorldTransforms;
				}
			}

			public bool UseIdentityProjection
			{
				get
				{
					return false;
				}
			}

			public bool UseIdentityView
			{
				get
				{
					return false;
				}
			}

			public virtual bool PolygonModeOverrideable
			{
				get
				{
					return true;
				}
			}

			public Vector4 GetCustomParameter( int index )
			{
				if ( customParams[ index ] == null )
				{
					throw new Exception( "A parameter was not found at the given index" );
				}
				else
				{
					return (Vector4)customParams[ index ];
				}
			}

			public void SetCustomParameter( int index, Vector4 val )
			{
				while ( customParams.Count <= index )
				{
					customParams.Add( Vector4.Zero );
				}
				customParams[ index ] = val;
			}

			public void UpdateCustomGpuParameter( GpuProgramParameters.AutoConstantEntry entry, GpuProgramParameters gpuParams )
			{
				if ( customParams[ entry.Data ] != null )
				{
					gpuParams.SetConstant( entry.PhysicalIndex, (Vector4)customParams[ entry.Data ] );
				}
			}

			/// <summary>
			///     Dispose the hardware index and vertex buffers
			/// </summary>
			protected override void dispose( bool disposeManagedResources )
			{
				if ( !this.IsDisposed )
				{
					if ( disposeManagedResources )
					{
						if ( indexData != null )
						{
							if ( !indexData.IsDisposed )
							{
								indexData.Dispose();
							}

							indexData = null;
						}

						if ( vertexData != null )
						{
							if ( !vertexData.IsDisposed )
							{
								this.vertexData.Dispose();
							}

							vertexData = null;
						}
					}
				}
				base.dispose( disposeManagedResources );
			}

			#endregion IRenderable members
		}
	}
}
