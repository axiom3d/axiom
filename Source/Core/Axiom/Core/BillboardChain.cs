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
using Axiom.Math;
using Axiom.Graphics;
using Axiom.Collections;
using Axiom.Core.Collections;

#endregion Namespace Declarations

namespace Axiom.Core
{
	public class BillboardChain : MovableObject, IRenderable
	{
		public class Element
		{
			#region Fields

			// U or V texture coord depending on options

			#endregion Fields

			#region Constructors

			public Element()
			{
			}

			public Element( Vector3 position, float width, float texCoord, ColorEx color )
			{
				Position = position;
				Width = width;
				TexCoord = texCoord;
				Color = color;
			}

			#endregion Constructors

			#region Properties

			public Vector3 Position { get; set; }

			public float Width { get; set; }

			public float TexCoord { get; set; }

			public ColorEx Color { get; set; }

			#endregion Properties
		}

		public class ChainSegment
		{
			public int start;
			public int head;
			public int tail;
		}

		public enum TexCoordDirection
		{
			U,
			V
		}

		public const int SEGMENT_EMPTY = int.MaxValue;

		#region Fields

		protected int maxElementsPerChain;
		protected int chainCount;
		protected bool useTexCoords;
		protected bool useVertexColor;
		protected bool dynamic;
		protected VertexData vertexData;
		protected IndexData indexData;
		protected bool vertexDeclDirty;
		protected bool buffersNeedRecreating;
		protected bool boundsDirty;
		protected bool indexContentDirty;
		protected AxisAlignedBox aabb = new AxisAlignedBox();
		protected Real radius;
		protected string materialName;
		protected Material material;
		protected TexCoordDirection texCoordDirection;
		protected float[] otherTexCoordRange = new float[2];

		protected List<Element> chainElementList;

		protected List<ChainSegment> chainSegmentList;

		protected List<Vector4> customParams = new List<Vector4>( 20 );

		#endregion Fields

		#region Properties

		public virtual int MaxChainElements
		{
			get
			{
				return maxElementsPerChain;
			}
			set
			{
				maxElementsPerChain = value;
				SetupChainContainers();
				buffersNeedRecreating = indexContentDirty = true;
			}
		}

		public virtual int NumberOfChains
		{
			get
			{
				return chainCount;
			}
			set
			{
				chainCount = value;
				SetupChainContainers();
				buffersNeedRecreating = indexContentDirty = true;
			}
		}

		public virtual bool UseTextureCoords
		{
			get
			{
				return useTexCoords;
			}
			set
			{
				useTexCoords = value;
				vertexDeclDirty = true;
				buffersNeedRecreating = indexContentDirty = true;
			}
		}

		public virtual TexCoordDirection TextureCoordDirection
		{
			get
			{
				return texCoordDirection;
			}
			set
			{
				texCoordDirection = value;
			}
		}

		public virtual float[] OtherTexCoordRange
		{
			get
			{
				return otherTexCoordRange;
			}
			set
			{
				otherTexCoordRange = value;
			}
		}

		public virtual bool UseVertexColors
		{
			get
			{
				return useVertexColor;
			}
			set
			{
				useVertexColor = value;
				vertexDeclDirty = true;
				buffersNeedRecreating = indexContentDirty = true;
			}
		}

		public virtual bool Dynamic
		{
			get
			{
				return dynamic;
			}
			set
			{
				dynamic = value;
				buffersNeedRecreating = true;
				indexContentDirty = true;
			}
		}

		public virtual string MaterialName
		{
			get
			{
				return materialName;
			}
			set
			{
				materialName = value;
				material = (Material)MaterialManager.Instance[ value ];
				if ( material == null )
				{
					LogManager.Instance.Write(
						"Can't assign material {0} to BillboardChain {1} because this " +
						"Material does not exist. Have you forgotten to define it in a .material script?", materialName, Name );

					material = (Material)MaterialManager.Instance[ "BaseWhiteNoLighting" ];
					if ( material == null )
					{
						throw new Exception(
							String.Format(
								"Can't assign default material to BillboardChain of {0}. Did " +
								"you forget to call MaterialManager::initialise()?", Name ) );
					}
				}
			}
		}

		#endregion Properties

		#region Constructors

		public BillboardChain( string name, int maxElements, int numberOfChains, bool useTextureCoords, bool useColors,
		                       bool dynamic )
			: base( name )
		{
			maxElementsPerChain = maxElements;
			chainCount = numberOfChains;
			useTexCoords = useTextureCoords;
			useVertexColor = useColors;
			this.dynamic = dynamic;

			vertexDeclDirty = true;
			buffersNeedRecreating = true;
			boundsDirty = true;
			indexContentDirty = true;
			radius = 0.0f;
			texCoordDirection = TexCoordDirection.U;

			vertexData = new VertexData();
			indexData = new IndexData();

			otherTexCoordRange[ 0 ] = 0.0f;
			otherTexCoordRange[ 1 ] = 1.0f;

			SetupChainContainers();

			vertexData.vertexStart = 0;
			// index data setup later
			// set basic white material
			MaterialName = "BaseWhiteNoLighting";
		}

		public BillboardChain( string name, int maxElements, int numberOfChains, bool useTextureCoords, bool useColors )
			: this( name, maxElements, numberOfChains, useTextureCoords, useColors, true )
		{
		}

		public BillboardChain( string name, int maxElements, int numberOfChains, bool useTextureCoords )
			: this( name, maxElements, numberOfChains, useTextureCoords, true, true )
		{
		}

		public BillboardChain( string name, int maxElements, int numberOfChains )
			: this( name, maxElements, numberOfChains, true, true, true )
		{
		}

		public BillboardChain( string name, int maxElements )
			: this( name, maxElements, 1, true, true, true )
		{
		}

		public BillboardChain( string name )
			: this( name, 20, 1, true, true, true )
		{
		}

		#endregion Constructors

		#region Protected Virtual Methods

		protected virtual void SetupChainContainers()
		{
			// allocate enough space for everything
			chainElementList = new List<Element>( chainCount*maxElementsPerChain );

			for ( var i = 0; i < chainCount*maxElementsPerChain; ++i )
			{
				chainElementList.Add( new Element() );
			}

			vertexData.vertexCount = chainElementList.Capacity*2;

			// configure chains
			chainSegmentList = new List<ChainSegment>( chainCount );
			for ( var i = 0; i < chainCount; ++i )
			{
				chainSegmentList.Add( new ChainSegment() );
				chainSegmentList[ i ].start = i*maxElementsPerChain;
				chainSegmentList[ i ].tail = chainSegmentList[ i ].head = SEGMENT_EMPTY;
			}
		}

		protected virtual void SetupVertexDeclaration()
		{
			if ( vertexDeclDirty )
			{
				var decl = vertexData.vertexDeclaration;
				decl.RemoveAllElements();

				var offset = 0;
				// Add a description for the buffer of the positions of the vertices
				decl.AddElement( 0, offset, VertexElementType.Float3, VertexElementSemantic.Position );
				offset += VertexElement.GetTypeSize( VertexElementType.Float3 );

				if ( useVertexColor )
				{
					decl.AddElement( 0, offset, VertexElementType.Color, VertexElementSemantic.Diffuse );
					offset += VertexElement.GetTypeSize( VertexElementType.Color );
				}

				if ( useTexCoords )
				{
					decl.AddElement( 0, offset, VertexElementType.Float2, VertexElementSemantic.TexCoords );
					offset += VertexElement.GetTypeSize( VertexElementType.Float2 );
				}

				if ( !useTexCoords && !useVertexColor )
				{
					LogManager.Instance.Write( "Error - BillboardChain '{0}' is using neither texture " +
					                           "coordinates or vertex colors; it will not be visible " +
					                           "on some rendering API's so you should change this so you " + "use one or the other." );
				}
				vertexDeclDirty = false;
			}
		}

		protected virtual void SetupBuffers()
		{
			SetupVertexDeclaration();

			if ( buffersNeedRecreating )
			{
				// Create the vertex buffer (always dynamic due to the camera adjust)
				var buffer = HardwareBufferManager.Instance.CreateVertexBuffer( vertexData.vertexDeclaration.Clone( 0 ),
				                                                                vertexData.vertexCount, BufferUsage.DynamicWriteOnly );

				// (re)Bind the buffer
				// Any existing buffer will lose its reference count and be destroyed
				vertexData.vertexBufferBinding.SetBinding( 0, buffer );

				indexData.indexBuffer = HardwareBufferManager.Instance.CreateIndexBuffer( IndexType.Size16,
				                                                                          chainCount*maxElementsPerChain*6
				                                                                          /* max we can use */,
				                                                                          dynamic
				                                                                          	? BufferUsage.DynamicWriteOnly
				                                                                          	: BufferUsage.StaticWriteOnly );
				// NB we don't set the indexCount on IndexData here since we will
				// probably use less than the maximum number of indices

				buffersNeedRecreating = false;
			}
		}

		protected virtual void UpdateVertexBuffer( Camera camera )
		{
			SetupBuffers();
			var buffer = vertexData.vertexBufferBinding.GetBuffer( 0 );
			var bufferPtr = buffer.Lock( BufferLocking.Discard );

			var camPosition = camera.DerivedPosition;
			var eyePosition = ParentNode.DerivedOrientation.Inverse()*( camPosition - ParentNode.DerivedPosition )/
			                  ParentNode.DerivedScale;

			Vector3 chainTangent;

#if !AXIOM_SAFE_ONLY
			unsafe
#endif
			{
				var bufferStart = bufferPtr;

				foreach ( var segment in chainSegmentList )
				{
					// Skip 0 or 1 element segment counts
					if ( segment.head != SEGMENT_EMPTY && segment.head != segment.tail )
					{
						var laste = segment.head;
						for ( var e = segment.head;; ++e )
						{
							// Wrap forwards
							if ( e == maxElementsPerChain )
							{
								e = 0;
							}

							var element = chainElementList[ e + segment.start ];
							var baseIndex = (ushort)( ( e + segment.start )*2 );

							// Determine base pointer to vertex #1
							var pBase = bufferStart + buffer.VertexSize*baseIndex;

							// Get index of next item
							var nexte = e + 1;
							if ( nexte == maxElementsPerChain )
							{
								nexte = 0;
							}

							if ( e == segment.head )
							{
								// no laste, use next item
								chainTangent = chainElementList[ nexte + segment.start ].Position - element.Position;
							}
							else if ( e == segment.tail )
							{
								// no nexte, use only last item
								chainTangent = element.Position - chainElementList[ laste + segment.start ].Position;
							}
							else
							{
								// a mid position, use tangent across both prev and next
								chainTangent = chainElementList[ nexte + segment.start ].Position -
								               chainElementList[ laste + segment.start ].Position;
							}

							var p1ToEye = eyePosition - element.Position;
							var perpendicular = chainTangent.Cross( p1ToEye );
							perpendicular.Normalize();
							perpendicular *= ( element.Width*0.5f );

							var pos0 = element.Position - perpendicular;
							var pos1 = element.Position + perpendicular;

							var pFloat = pBase.ToFloatPointer();
							// pos1
							pFloat[ 0 ] = pos0.x;
							pFloat[ 1 ] = pos0.y;
							pFloat[ 2 ] = pos0.z;

							pBase += sizeof ( float )*3;

							if ( useVertexColor )
							{
								var pColor = pBase.ToIntPointer();
								pColor[ 0 ] = Root.Instance.ConvertColor( element.Color );
								pBase += sizeof ( int );
							}

							if ( useTexCoords )
							{
								pFloat = pBase.ToFloatPointer();
								if ( texCoordDirection == TexCoordDirection.U )
								{
									pFloat[ 0 ] = element.TexCoord;
									pFloat[ 1 ] = otherTexCoordRange[ 0 ];
								}
								else
								{
									pFloat[ 0 ] = otherTexCoordRange[ 0 ];
									pFloat[ 1 ] = element.TexCoord;
								}
								pBase += sizeof ( float )*2;
							}

							pFloat = pBase.ToFloatPointer();

							// pos2
							pFloat[ 0 ] = pos1.x;
							pFloat[ 1 ] = pos1.y;
							pFloat[ 2 ] = pos1.z;

							pBase += sizeof ( float )*3;

							if ( useVertexColor )
							{
								var pColor = pBase.ToIntPointer();
								pColor[ 0 ] = Root.Instance.ConvertColor( element.Color );
								pBase += sizeof ( int );
							}

							if ( useTexCoords )
							{
								pFloat = pBase.ToFloatPointer();
								if ( texCoordDirection == TexCoordDirection.U )
								{
									pFloat[ 0 ] = element.TexCoord;
									pFloat[ 1 ] = otherTexCoordRange[ 0 ];
								}
								else
								{
									pFloat[ 0 ] = otherTexCoordRange[ 0 ];
									pFloat[ 1 ] = element.TexCoord;
								}
								pBase += sizeof ( float )*2;
							}

							if ( e == segment.tail )
							{
								break;
							}
							laste = e;
						}
					}
				}
			}
			buffer.Unlock();
		}

		protected virtual void UpdateIndexBuffer()
		{
			SetupBuffers();

			if ( indexContentDirty )
			{
				var pBufferBase = indexData.indexBuffer.Lock( BufferLocking.Discard );
				indexData.indexCount = 0;

#if !AXIOM_SAFE_ONLY
				unsafe
#endif
				{
					var pShort = pBufferBase.ToUShortPointer();
					var idx = 0;
					// indexes
					foreach ( var segment in chainSegmentList )
					{
						// Skip 0 or 1 element segment counts
						if ( segment.head != SEGMENT_EMPTY && segment.head != segment.tail )
						{
							// Start from head + 1 since it's only useful in pairs
							var laste = segment.head;

							while ( true )
							{
								var e = laste + 1;
								// Wrap Forwards
								if ( e == maxElementsPerChain )
								{
									e = 0;
								}
								// indexes of this element are (e * 2) and (e * 2) + 1
								// indexes of the last element are the same, -2
								var baseIndex = (ushort)( ( e + segment.start )*2 );
								var lastBaseIndex = (ushort)( ( laste + segment.start )*2 );

								pShort[ idx++ ] = lastBaseIndex;
								pShort[ idx++ ] = (ushort)( lastBaseIndex + 1 );
								pShort[ idx++ ] = baseIndex;
								pShort[ idx++ ] = (ushort)( lastBaseIndex + 1 );
								pShort[ idx++ ] = (ushort)( baseIndex + 1 );
								pShort[ idx++ ] = baseIndex;

								indexData.indexCount += 6;

								if ( e == segment.tail )
								{
									break;
								}

								laste = e;
							}
						}
					}
				}

				indexData.indexBuffer.Unlock();
				indexContentDirty = false;
			}
		}

		protected virtual void UpdateBoundingBox()
		{
			if ( boundsDirty )
			{
				aabb.IsNull = true;
				Vector3 widthVector;

				foreach ( var segment in chainSegmentList )
				{
					if ( segment.head != SEGMENT_EMPTY )
					{
						for ( var i = segment.head;; ++i )
						{
							// Wrap forwards
							if ( i == maxElementsPerChain )
							{
								i = 0;
							}

							var element = chainElementList[ segment.start + i ];

							widthVector.x = widthVector.y = widthVector.z = element.Width;
							aabb.Merge( element.Position - widthVector );
							aabb.Merge( element.Position + widthVector );

							if ( i == segment.tail )
							{
								break;
							}
						}
					}
				}

				if ( aabb.IsNull )
				{
					radius = 0.0f;
				}
				else
				{
					radius = (float)Utility.Sqrt( Utility.Max( aabb.Minimum.LengthSquared, aabb.Maximum.LengthSquared ) );
				}
				boundsDirty = false;
			}
		}

		#endregion Protected Virtual Methods

		#region Public Virtual Methods

		public virtual void AddChainElement( int chainIndex, Element billboardChainElement )
		{
			if ( chainIndex >= chainCount )
			{
				throw new IndexOutOfRangeException();
			}
			var segment = chainSegmentList[ chainIndex ];
			if ( segment.head == SEGMENT_EMPTY )
			{
				// Tail starts at end, head grows backwards
				segment.tail = maxElementsPerChain - 1;
				segment.head = segment.tail;
				indexContentDirty = true;
			}
			else
			{
				if ( segment.head == 0 )
				{
					// Wrap backwards
					segment.head = maxElementsPerChain - 1;
				}
				else
				{
					// just step backwards
					--segment.head;
				}
				// Run out of elements?
				if ( segment.head == segment.tail )
				{
					// Move tail backwards too, losing the end of the segment and re-using
					// it in the head
					if ( segment.head == 0 )
					{
						segment.tail = maxElementsPerChain - 1;
					}
					else
					{
						--segment.tail;
					}
				}
			}

			// set the details
			chainElementList[ segment.start + segment.head ] = billboardChainElement;

			indexContentDirty = true;
			boundsDirty = true;

			// tell parent node to update bounds
			if ( ParentNode != null )
			{
				ParentNode.NeedUpdate();
			}
		}

		public virtual void RemoveChainElement( int chainIndex )
		{
			if ( chainIndex >= chainCount )
			{
				throw new IndexOutOfRangeException();
			}
			var segment = chainSegmentList[ chainIndex ];
			if ( segment.head == SEGMENT_EMPTY )
			{
				return; // nothing to remove
			}

			if ( segment.tail == segment.head )
			{
				// last item
				segment.head = segment.tail = SEGMENT_EMPTY;
			}
			else if ( segment.tail == 0 )
			{
				segment.tail = maxElementsPerChain - 1;
			}
			else
			{
				--maxElementsPerChain;
			}

			// we removed an entry so indexes need updating
			indexContentDirty = true;
			boundsDirty = true;
			// tell parent node to update bounds
			if ( ParentNode != null )
			{
				ParentNode.NeedUpdate();
			}
		}

		public virtual void UpdateChainElement( int chainIndex, int elementIndex, Element billboardChainElement )
		{
			if ( chainIndex >= chainCount )
			{
				throw new IndexOutOfRangeException();
			}
			var segment = chainSegmentList[ chainIndex ];
			if ( segment.head == SEGMENT_EMPTY )
			{
				throw new Exception( "Chain segement is empty" );
			}

			var index = segment.head + elementIndex;
			// adjust for the edge and start
			index = ( index%maxElementsPerChain ) + segment.start;

			chainElementList[ index ] = billboardChainElement;

			boundsDirty = true;
			// tell parent node to update bounds
			if ( ParentNode != null )
			{
				ParentNode.NeedUpdate();
			}
		}

		public virtual Element GetChainElement( int chainIndex, int elementIndex )
		{
			if ( chainIndex >= chainCount )
			{
				throw new IndexOutOfRangeException();
			}
			var segment = chainSegmentList[ chainIndex ];

			var index = segment.head + elementIndex;
			// adjust for the edge and start
			index = ( index%maxElementsPerChain ) + segment.start;

			return chainElementList[ index ];
		}

		#endregion Public Virtual Methods

		#region Overriden Methods

		public override void NotifyCurrentCamera( Camera camera )
		{
			UpdateVertexBuffer( camera );
		}

		public override void UpdateRenderQueue( RenderQueue queue )
		{
			UpdateIndexBuffer();

			if ( indexData.indexCount > 0 )
			{
				queue.AddRenderable( this );
			}
		}

		public override AxisAlignedBox BoundingBox
		{
			get
			{
				UpdateBoundingBox();
				return aabb;
			}
		}

		public override Real BoundingRadius
		{
			get
			{
				return radius;
			}
		}

		public bool CastShadows
		{
			get
			{
				return false;
			}
		}

		#endregion Overriden Methods

		#region IRenderable Implementation

		public bool NormalizeNormals
		{
			get
			{
				return false;
			}
		}

		public bool CastsShadows
		{
			get
			{
				return false;
			}
		}

		public Material Material
		{
			get
			{
				return material;
			}
		}

		public Technique Technique
		{
			get
			{
				return material.GetBestTechnique();
			}
		}

		/// <summary>
		///
		/// </summary>
		public virtual ushort NumWorldTransforms
		{
			get
			{
				return 1;
			}
		}

		/// <summary>
		///
		/// </summary>
		public bool UseIdentityProjection
		{
			get
			{
				return false;
			}
		}

		/// <summary>
		///
		/// </summary>
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

		/// <summary>
		///
		/// </summary>
		public Quaternion WorldOrientation
		{
			get
			{
				return parentNode.DerivedOrientation;
			}
		}

		/// <summary>
		///
		/// </summary>
		public Vector3 WorldPosition
		{
			get
			{
				return parentNode.DerivedPosition;
			}
		}

		public LightList Lights
		{
			get
			{
				return QueryLights();
			}
		}

		protected RenderOperation renderOperation = new RenderOperation();

		public RenderOperation RenderOperation
		{
			get
			{
				renderOperation.indexData = indexData;
				renderOperation.operationType = OperationType.TriangleList;
				renderOperation.useIndices = true;
				renderOperation.vertexData = vertexData;
				return renderOperation;
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="matrices"></param>
		public virtual void GetWorldTransforms( Matrix4[] matrices )
		{
			matrices[ 0 ] = parentNode.FullTransform;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="camera"></param>
		/// <returns></returns>
		public virtual Real GetSquaredViewDepth( Camera camera )
		{
			Debug.Assert( parentNode != null, "BillboardSet must have a parent scene node to get the squared view depth." );

			return parentNode.GetSquaredViewDepth( camera );
		}

		public Vector4 GetCustomParameter( int index )
		{
			if ( customParams[ index ] == null )
			{
				throw new AxiomException( "A parameter was not found at the given index" );
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

		#endregion IRenderable Implementation

		#region IDisposable Implementation

		/// <summary>
		/// Class level dispose method
		/// </summary>
		/// <remarks>
		/// When implementing this method in an inherited class the following template should be used;
		/// protected override void dispose( bool disposeManagedResources )
		/// {
		/// 	if ( !isDisposed )
		/// 	{
		/// 		if ( disposeManagedResources )
		/// 		{
		/// 			// Dispose managed resources.
		/// 		}
		///
		/// 		// There are no unmanaged resources to release, but
		/// 		// if we add them, they need to be released here.
		/// 	}
		///
		/// 	// If it is available, make the call to the
		/// 	// base class's Dispose(Boolean) method
		/// 	base.dispose( disposeManagedResources );
		/// }
		/// </remarks>
		/// <param name="disposeManagedResources">True if Unmanaged resources should be released.</param>
		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				if ( disposeManagedResources )
				{
					// Dispose managed resources.
					if ( renderOperation != null )
					{
						if ( !renderOperation.IsDisposed )
						{
							renderOperation.Dispose();
						}

						renderOperation = null;
					}

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
							vertexData.Dispose();
						}

						vertexData = null;
					}
				}

				// There are no unmanaged resources to release, but
				// if we add them, they need to be released here.
			}

			base.dispose( disposeManagedResources );
		}

		#endregion IDisposable Implementation
	}

	public class BillboardChainFactory : MovableObjectFactory
	{
		public new const string TypeName = "BillboardChain";

		public BillboardChainFactory()
			: base()
		{
			base.Type = BillboardChainFactory.TypeName;
			base.TypeFlag = (uint)SceneQueryTypeMask.Fx;
		}

		protected override MovableObject _createInstance( string name, NamedParameterList param )
		{
			var maxElements = 20;
			var numberOfChains = 1;
			var useTextureCoords = true;
			var useVertexColors = true;
			var isDynamic = true;

			// optional parameters
			if ( param != null )
			{
				if ( param.ContainsKey( "maxElements" ) )
				{
					maxElements = Convert.ToInt32( param[ "maxElements" ] );
				}
				if ( param.ContainsKey( "numberOfChains" ) )
				{
					numberOfChains = Convert.ToInt32( param[ "numberOfChains" ] );
				}
				if ( param.ContainsKey( "useTextureCoords" ) )
				{
					useTextureCoords = Convert.ToBoolean( param[ "useTextureCoords" ] );
				}
				if ( param.ContainsKey( "useVertexColours" ) )
				{
					useVertexColors = Convert.ToBoolean( param[ "useVertexColours" ] );
				}
				else if ( param.ContainsKey( "useVertexColors" ) )
				{
					useVertexColors = Convert.ToBoolean( param[ "useVertexColors" ] );
				}
				if ( param.ContainsKey( "isDynamic" ) )
				{
					isDynamic = Convert.ToBoolean( param[ "isDynamic" ] );
				}
			}

			return new BillboardChain( name, maxElements, numberOfChains, useTextureCoords, useVertexColors, isDynamic );
		}
	}
}