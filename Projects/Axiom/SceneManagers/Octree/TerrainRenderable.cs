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
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using System.Xml.Serialization;

using Axiom.Core;
using Axiom.Core.Collections;
using Axiom.CrossPlatform;
using Axiom.Graphics;
using Axiom.Math;

#endregion Namespace Declarations

namespace Axiom.SceneManagers.Octree
{
	/// <summary>
	/// Summary description for TerrainRenderable.
	/// </summary>
	public class TerrainRenderable : MovableObject, IRenderable
	{
		#region Fields

		private const int POSITION = 0;
		private const int NORMAL = 1;
		private const int TEXCOORD = 2;
		private const int COLORS = 3;

		private float[] _vertex = new float[ 1 ]; //for GetVertex() buffer retrieval
		protected AxisAlignedBox box = new AxisAlignedBox();
		protected Vector3 center;
		protected float currentL;
		protected List<Vector4> customParams = new List<Vector4>();
		protected int forcedRenderLevel;
		protected IndexData[ , ] levelIndex = new IndexData[ 16, 16 ];
		protected Material material;
		protected float[] minLevelDistSqr;
		protected TerrainRenderable[] neighbors = new TerrainRenderable[ 4 ];
		protected int numMipMaps;
		protected TerrainOptions options;
		protected int renderLevel;
		protected int size;
		protected VertexData terrain;

		#endregion Fields

		protected RenderOperation renderOperation = new RenderOperation();

		/// <summary>
		///     Default constructor.
		/// </summary>
		public TerrainRenderable()
			: this( string.Empty ) { }

		public TerrainRenderable( string name )
			: base( name )
		{
			this.renderLevel = 1;
			this.forcedRenderLevel = -1;
		}

		#region Methods

		public void SetMaterial( Material mat )
		{
			this.material = mat;
		}

		public void Init( TerrainOptions options )
		{
			this.options = options;

			this.numMipMaps = options.maxMipmap;
			this.size = options.size;

			this.terrain = new VertexData();
			this.terrain.vertexStart = 0;

			this.terrain.vertexCount = options.size * options.size;

			VertexDeclaration decl = this.terrain.vertexDeclaration;
			VertexBufferBinding binding = this.terrain.vertexBufferBinding;

			int offset = 0;

			// Position/Normal
			decl.AddElement( POSITION, 0, VertexElementType.Float3, VertexElementSemantic.Position );
			decl.AddElement( NORMAL, 0, VertexElementType.Float3, VertexElementSemantic.Normal );

			// TexCoords
			decl.AddElement( TEXCOORD, offset, VertexElementType.Float2, VertexElementSemantic.TexCoords, 0 );
			offset += VertexElement.GetTypeSize( VertexElementType.Float2 );
			decl.AddElement( TEXCOORD, offset, VertexElementType.Float2, VertexElementSemantic.TexCoords, 1 );
			offset += VertexElement.GetTypeSize( VertexElementType.Float2 );
			// TODO: Color

			HardwareVertexBuffer buffer = HardwareBufferManager.Instance.CreateVertexBuffer( decl.Clone( POSITION ), this.terrain.vertexCount, BufferUsage.StaticWriteOnly, true );

			binding.SetBinding( POSITION, buffer );

			buffer = HardwareBufferManager.Instance.CreateVertexBuffer( decl.Clone( NORMAL ), this.terrain.vertexCount, BufferUsage.StaticWriteOnly, true );

			binding.SetBinding( NORMAL, buffer );

			buffer = HardwareBufferManager.Instance.CreateVertexBuffer( decl.Clone( TEXCOORD ), this.terrain.vertexCount, BufferUsage.StaticWriteOnly, true );

			binding.SetBinding( TEXCOORD, buffer );

			this.minLevelDistSqr = new float[ this.numMipMaps ];

			int endx = options.startx + options.size;
			int endz = options.startz + options.size;

			// TODO: name buffers different so we can unlock
			HardwareVertexBuffer posBuffer = binding.GetBuffer( POSITION );
			BufferBase pos = posBuffer.Lock( BufferLocking.Discard );

			HardwareVertexBuffer texBuffer = binding.GetBuffer( TEXCOORD );
			BufferBase tex = texBuffer.Lock( BufferLocking.Discard );

			float min = 99999999, max = 0;

#if !AXIOM_SAFE_ONLY
			unsafe
#endif
			{
				float* posPtr = pos.ToFloatPointer();
				float* texPtr = tex.ToFloatPointer();

				int posCount = 0;
				int texCount = 0;

				for ( int j = options.startz; j < endz; j++ )
				{
					for ( int i = options.startx; i < endx; i++ )
					{
						float height = options.GetWorldHeight( i, j ) * options.scaley;

						posPtr[ posCount++ ] = i * options.scalex;
						posPtr[ posCount++ ] = height;
						posPtr[ posCount++ ] = j * options.scalez;

						texPtr[ texCount++ ] = (float)i / options.worldSize;
						texPtr[ texCount++ ] = (float)j / options.worldSize;

						texPtr[ texCount++ ] = ( (float)i / options.size ) * options.detailTile;
						texPtr[ texCount++ ] = ( (float)j / options.size ) * options.detailTile;

						if ( height < min )
						{
							min = height;
						}

						if ( height > max )
						{
							max = height;
						}
					} // for i
				} // for j
			} // unsafe

			// unlock the buffers
			posBuffer.Unlock();
			texBuffer.Unlock();

			this.box.SetExtents( new Vector3( options.startx * options.scalex, min, options.startz * options.scalez ), new Vector3( ( endx - 1 ) * options.scalex, max, ( endz - 1 ) * options.scalez ) );

			this.center = new Vector3( ( options.startx * options.scalex + endx - 1 ) / 2, ( min + max ) / 2, ( options.startz * options.scalez + endz - 1 ) / 2 );

			float C = CalculateCFactor();

			CalculateMinLevelDist2( C );
		}

		public float GetHeightAt( float x, float z )
		{
			Vector3 start, end;

			start.x = GetVertex( 0, 0, 0 );
			start.y = GetVertex( 0, 0, 1 );
			start.z = GetVertex( 0, 0, 2 );

			end.x = GetVertex( this.options.size - 1, this.options.size - 1, 0 );
			end.y = GetVertex( this.options.size - 1, this.options.size - 1, 1 );
			end.z = GetVertex( this.options.size - 1, this.options.size - 1, 2 );

			// safety catch.  if the point asked for is outside of this tile, ask a neighbor

			if ( x < start.x )
			{
				if ( GetNeighbor( Neighbor.West ) != null )
				{
					return GetNeighbor( Neighbor.West ).GetHeightAt( x, z );
				}
				else
				{
					x = start.x;
				}
			}

			if ( x > end.x )
			{
				if ( GetNeighbor( Neighbor.East ) != null )
				{
					return GetNeighbor( Neighbor.East ).GetHeightAt( x, z );
				}
				else
				{
					x = end.x;
				}
			}

			if ( z < start.z )
			{
				if ( GetNeighbor( Neighbor.North ) != null )
				{
					return GetNeighbor( Neighbor.North ).GetHeightAt( x, z );
				}
				else
				{
					z = start.z;
				}
			}

			if ( z > end.z )
			{
				if ( GetNeighbor( Neighbor.South ) != null )
				{
					return GetNeighbor( Neighbor.South ).GetHeightAt( x, z );
				}
				else
				{
					z = end.z;
				}
			}

			float xPct = ( x - start.x ) / ( end.x - start.x );
			float zPct = ( z - start.z ) / ( end.z - start.z );

			float xPt = xPct * ( this.options.size - 1 );
			float zPt = zPct * ( this.options.size - 1 );

			var xIndex = (int)xPt;
			var zIndex = (int)zPt;

			xPct = xPt - xIndex;
			zPct = zPt - zIndex;

			// bilinear interpolcation to find the height
			float t1 = GetVertex( xIndex, zIndex, 1 );
			float t2 = GetVertex( xIndex + 1, zIndex, 1 );
			float b1 = GetVertex( xIndex, zIndex + 1, 1 );
			float b2 = GetVertex( xIndex + 1, zIndex + 1, 1 );

			float midpoint = ( b1 + b2 ) / 2;

			if ( ( xPct + zPct ) <= 1 )
			{
				b2 = midpoint + ( midpoint - t1 );
			}
			else
			{
				t1 = midpoint + ( midpoint - b2 );
			}

			float t = ( t1 * ( 1 - xPct ) ) + ( t2 * ( xPct ) );
			float b = ( b1 * ( 1 - xPct ) ) + ( b2 * ( xPct ) );
			float h = ( t * ( 1 - zPct ) ) + ( b * ( zPct ) );

			return h;
		}

		public TerrainRenderable GetNeighbor( Neighbor n )
		{
			return this.neighbors[ (int)n ];
		}

		/// <summary>
		///     Returns the vertex coord for the given coordinates.
		/// </summary>
		/// <param name="x"></param>
		/// <param name="z"></param>
		/// <param name="n"></param>
		/// <returns></returns>
		public float GetVertex( int x, int z, int n )
		{
			HardwareVertexBuffer buffer = this.terrain.vertexBufferBinding.GetBuffer( POSITION );

			var vertex = new float[ 1 ];

			BufferBase ptr = Memory.PinObject( vertex );

			int offset = ( x * 3 + z * this.options.size * 3 + n ) * 4;

			buffer.ReadData( offset, 4, ptr );

			Memory.UnpinObject( vertex );

			return vertex[ 0 ];
		}

		public void SetNeighbor( Neighbor n, TerrainRenderable t )
		{
			this.neighbors[ (int)n ] = t;
		}

		public void AdjustRenderLevel( int i )
		{
			this.renderLevel = i;
			AlignNeighbors();
		}

		public void AlignNeighbors()
		{
			//ensure that there aren't any gaps...
			for ( int i = 0; i < 4; i++ )
			{
				if ( this.neighbors[ i ] != null && this.neighbors[ i ].renderLevel + 1 < this.renderLevel )
				{
					this.neighbors[ i ].AdjustRenderLevel( this.renderLevel - 1 );
				}
			}
		}

		public float CalculateCFactor()
		{
			float A, T;

			A = (float)this.options.nearPlane / Utility.Abs( this.options.topCoord );

			T = 2 * (float)this.options.maxPixelError / this.options.vertRes;

			return A / T;
		}

		public void CalculateMinLevelDist2( float C )
		{
			// level 1 has no delta
			this.minLevelDistSqr[ 0 ] = 0;

			for ( int level = 1; level < this.numMipMaps; level++ )
			{
				this.minLevelDistSqr[ level ] = 0;

				int step = 1 << level;

				for ( int j = 0; j < this.size - step; j += step )
				{
					for ( int i = 0; i < this.size - step; i += step )
					{
						//check each height inbetween the steps.
						float h1 = GetVertex( i, j, 1 );
						float h2 = GetVertex( i + step, j, 1 );
						float h3 = GetVertex( i + step, j + step, 1 );
						float h4 = GetVertex( i, j + step, 1 );

						for ( int z = 1; z < step; z++ )
						{
							for ( int x = 1; x < step; x++ )
							{
								float zpct = z / step;
								float xpct = x / step;

								//interpolated height
								float top = h3 * ( 1.0f - xpct ) + xpct * h4;
								float bottom = h1 * ( 1.0f - xpct ) + xpct * h2;

								float interp_h = top * ( 1.0f - zpct ) + zpct * bottom;

								float actual_h = GetVertex( i + x, j + z, 1 );
								float delta = Utility.Abs( interp_h - actual_h );

								float D2 = delta * delta * C * C;

								if ( this.minLevelDistSqr[ level ] < D2 )
								{
									this.minLevelDistSqr[ level ] = D2;
								}
							}
						}
					}
				}
			}

			//make sure the levels are increasing...
			for ( int i = 1; i < this.numMipMaps; i++ )
			{
				if ( this.minLevelDistSqr[ i ] < this.minLevelDistSqr[ i - 1 ] )
				{
					this.minLevelDistSqr[ i ] = this.minLevelDistSqr[ i - 1 ] + 1;
				}
			}
		}

		public void CalculateNormals()
		{
#if !AXIOM_SAFE_ONLY
			unsafe
#endif
			{
				Vector3 normal;

				HardwareVertexBuffer buffer = this.terrain.vertexBufferBinding.GetBuffer( NORMAL );

				BufferBase norm = buffer.Lock( BufferLocking.Discard );

				float* normPtr = norm.ToFloatPointer();
				int count = 0;

				for ( int j = 0; j < this.size; j++ )
				{
					for ( int i = 0; i < this.size; i++ )
					{
						GetNormalAt( GetVertex( i, j, 0 ), GetVertex( i, j, 2 ), out normal );

						normPtr[ count++ ] = normal.x;
						normPtr[ count++ ] = normal.y;
						normPtr[ count++ ] = normal.z;
					}
				}

				buffer.Unlock();
			}
		}

		public void GetNormalAt( float x, float z, out Vector3 result )
		{
			Vector3 here, left, down;
			here.x = x;
			here.y = GetHeightAt( x, z );
			here.z = z;

			left.x = x - 1;
			left.y = GetHeightAt( x - 1, z );
			left.z = z;

			down.x = x;
			down.y = GetHeightAt( x, z + 1 );
			down.z = z + 1;

			left = left - here;

			down = down - here;

			left.Normalize();
			down.Normalize();

			result = left.Cross( down );
			result.Normalize();
		}

		public Vector3 IntersectSegment( Vector3 start, Vector3 end )
		{
			Vector3 dir = end - start;
			Vector3 ray = start;

			//special case...
			if ( dir.x == 0 && dir.z == 0 )
			{
				if ( ray.y <= GetHeightAt( ray.x, ray.z ) )
				{
					return start;
				}
			}

			dir.Normalize();

			//dir.x *= mScale.x;
			//dir.y *= mScale.y;
			//dir.z *= mScale.z;

			AxisAlignedBox box = BoundingBox;
			//start with the next one...
			ray += dir;

			// traverse down the ray until we are
			while ( !( ( ray.x < box.Minimum.x ) || ( ray.x > box.Maximum.x ) || ( ray.z < box.Minimum.z ) || ( ray.z > box.Maximum.z ) ) )
			{
				float h = GetHeightAt( ray.x, ray.z );

				if ( ray.y <= h )
				{
					return ray;
				}

				else
				{
					ray += dir;
				}
			}

			if ( ray.x < box.Minimum.x && GetNeighbor( Neighbor.West ) != null )
			{
				return GetNeighbor( Neighbor.West ).IntersectSegment( ray, end );
			}
			else if ( ray.z < box.Minimum.z && GetNeighbor( Neighbor.North ) != null )
			{
				return GetNeighbor( Neighbor.North ).IntersectSegment( ray, end );
			}
			else if ( ray.x > box.Maximum.x && GetNeighbor( Neighbor.East ) != null )
			{
				return GetNeighbor( Neighbor.East ).IntersectSegment( ray, end );
			}
			else if ( ray.z > box.Maximum.z && GetNeighbor( Neighbor.South ) != null )
			{
				return GetNeighbor( Neighbor.South ).IntersectSegment( ray, end );
			}
			else
			{
				return new Vector3( -1, -1, -1 );
			}
		}

		#endregion Methods

		public override AxisAlignedBox BoundingBox
		{
			get
			{
				return this.box;
			}
		}

		public override Real BoundingRadius
		{
			get
			{
				return 0;
			}
		}

		/// <summary>
		/// Get the 'type flags' for this <see cref="TerrainRenderable"/>.
		/// </summary>
		/// <seealso cref="MovableObject.TypeFlags"/>
		public override uint TypeFlags
		{
			get
			{
				return (uint)SceneQueryTypeMask.WorldGeometry;
			}
		}

		#region IRenderable Members

		public bool CastsShadows
		{
			get
			{
				return false;
			}
		}

		public Real GetSquaredViewDepth( Camera camera )
		{
			Vector3 diff = this.center - camera.DerivedPosition;

			return diff.LengthSquared;
		}

		public bool UseIdentityView
		{
			get
			{
				return false;
			}
		}

		public bool UseIdentityProjection
		{
			get
			{
				return false;
			}
		}

		public Vector3 WorldPosition
		{
			get
			{
				return parentNode.DerivedPosition;
			}
		}

		public RenderOperation RenderOperation
		{
			get
			{
				IndexData indexData = GetIndexData();

				this.renderOperation.useIndices = true;
				this.renderOperation.operationType = OperationType.TriangleList;
				this.renderOperation.vertexData = this.terrain;
				this.renderOperation.indexData = indexData;
				return this.renderOperation;
				//renderedTris += ( indexData->indexCount / 3 );

				//mRenderLevelChanged = false;
			}
		}

		public void GetWorldTransforms( Matrix4[] matrices )
		{
			// TODO: Add Node.FullTransform?
			parentNode.GetWorldTransforms( matrices );
		}

		public Quaternion WorldOrientation
		{
			get
			{
				return parentNode.DerivedOrientation;
			}
		}

		public virtual bool PolygonModeOverrideable
		{
			get
			{
				return true;
			}
		}

		public Material Material
		{
			get
			{
				return this.material;
			}
		}

		public LightList Lights
		{
			get
			{
				return QueryLights();
			}
		}

		public Technique Technique
		{
			get
			{
				return this.material.GetBestTechnique();
			}
		}

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
				return 1;
			}
		}

		public Vector4 GetCustomParameter( int index )
		{
			if ( this.customParams[ index ] == null )
			{
				throw new Exception( "A parameter was not found at the given index" );
			}
			else
			{
				return this.customParams[ index ];
			}
		}

		public void SetCustomParameter( int index, Vector4 val )
		{
			while ( this.customParams.Count <= index )
			{
				this.customParams.Add( Vector4.Zero );
			}
			this.customParams[ index ] = val;
		}

		public void UpdateCustomGpuParameter( GpuProgramParameters.AutoConstantEntry entry, GpuProgramParameters gpuParams )
		{
			if ( this.customParams[ entry.Data ] != null )
			{
				gpuParams.SetConstant( entry.PhysicalIndex, this.customParams[ entry.Data ] );
			}
		}

		#endregion

		public override void NotifyCurrentCamera( Camera camera )
		{
			if ( this.forcedRenderLevel >= 0 )
			{
				this.renderLevel = this.forcedRenderLevel;
				return;
			}

			int oldLevel = this.renderLevel;

			Vector3 cpos = camera.Position;
			Vector3 diff = this.center - cpos;

			float L = diff.LengthSquared;

			this.currentL = L;

			this.renderLevel = -1;

			for ( int i = 0; i < this.numMipMaps; i++ )
			{
				if ( this.minLevelDistSqr[ i ] > L )
				{
					this.renderLevel = i - 1;
					break;
				}
			}

			if ( this.renderLevel < 0 )
			{
				this.renderLevel = this.numMipMaps - 1;
			}
		}

		public override void UpdateRenderQueue( RenderQueue queue )
		{
			queue.AddRenderable( this );
		}

		private IndexData GetIndexData()
		{
			int east = 0, west = 0, north = 0, south = 0;

			int step = 1 << this.renderLevel;

			int indexArray = 0;

			int numIndexes = 0;

			if ( this.neighbors[ (int)Neighbor.East ] != null && this.neighbors[ (int)Neighbor.East ].renderLevel > this.renderLevel )
			{
				east = step;
				indexArray |= (int)Tile.East;
			}

			if ( this.neighbors[ (int)Neighbor.West ] != null && this.neighbors[ (int)Neighbor.West ].renderLevel > this.renderLevel )
			{
				west = step;
				indexArray |= (int)Tile.West;
			}

			if ( this.neighbors[ (int)Neighbor.North ] != null && this.neighbors[ (int)Neighbor.North ].renderLevel > this.renderLevel )
			{
				north = step;
				indexArray |= (int)Tile.North;
			}

			if ( this.neighbors[ (int)Neighbor.South ] != null && this.neighbors[ (int)Neighbor.South ].renderLevel > this.renderLevel )
			{
				south = step;
				indexArray |= (int)Tile.South;
			}

			IndexData indexData = null;

			if ( this.levelIndex[ this.renderLevel, indexArray ] != null )
			{
				indexData = this.levelIndex[ this.renderLevel, indexArray ];
			}
			else
			{
				int newLength = ( this.size / step ) * ( this.size / step ) * 2 * 2 * 2;
				//this is the maximum for a level.  It wastes a little, but shouldn't be a problem.

				indexData = new IndexData();
				indexData.indexBuffer = HardwareBufferManager.Instance.CreateIndexBuffer( IndexType.Size16, newLength, BufferUsage.StaticWriteOnly );

				//indexCache.Add(indexData);

				numIndexes = 0;

				BufferBase idx = indexData.indexBuffer.Lock( BufferLocking.Discard );
#if !AXIOM_SAFE_ONLY
				unsafe
#endif
				{
					short* idxPtr = idx.ToShortPointer();
					int count = 0;

					for ( int j = north; j < this.size - 1 - south; j += step )
					{
						for ( int i = west; i < this.size - 1 - east; i += step )
						{
							//triangles
							idxPtr[ count++ ] = GetIndex( i, j );
							numIndexes++;
							idxPtr[ count++ ] = GetIndex( i, j + step );
							numIndexes++;
							idxPtr[ count++ ] = GetIndex( i + step, j );
							numIndexes++;

							idxPtr[ count++ ] = GetIndex( i, j + step );
							numIndexes++;
							idxPtr[ count++ ] = GetIndex( i + step, j + step );
							numIndexes++;
							idxPtr[ count++ ] = GetIndex( i + step, j );
							numIndexes++;
						}
					}

					int substep = step << 1;

					if ( west > 0 )
					{
						for ( int j = 0; j < this.size - 1; j += substep )
						{
							//skip the first bit of the corner if the north side is a different level as well.
							if ( j > 0 || north == 0 )
							{
								idxPtr[ count++ ] = GetIndex( 0, j );
								numIndexes++;
								idxPtr[ count++ ] = GetIndex( step, j + step );
								numIndexes++;
								idxPtr[ count++ ] = GetIndex( step, j );
								numIndexes++;
							}

							idxPtr[ count++ ] = GetIndex( step, j + step );
							numIndexes++;
							idxPtr[ count++ ] = GetIndex( 0, j );
							numIndexes++;
							idxPtr[ count++ ] = GetIndex( 0, j + step + step );
							numIndexes++;

							if ( j < this.options.size - 1 - substep || south == 0 )
							{
								idxPtr[ count++ ] = GetIndex( step, j + step );
								numIndexes++;
								idxPtr[ count++ ] = GetIndex( 0, j + step + step );
								numIndexes++;
								idxPtr[ count++ ] = GetIndex( step, j + step + step );
								numIndexes++;
							}
						}
					}

					if ( east > 0 )
					{
						int x = this.options.size - 1;

						for ( int j = 0; j < this.size - 1; j += substep )
						{
							//skip the first bit of the corner if the north side is a different level as well.
							if ( j > 0 || north == 0 )
							{
								idxPtr[ count++ ] = GetIndex( x, j );
								numIndexes++;
								idxPtr[ count++ ] = GetIndex( x - step, j );
								numIndexes++;
								idxPtr[ count++ ] = GetIndex( x - step, j + step );
								numIndexes++;
							}

							idxPtr[ count++ ] = GetIndex( x, j );
							numIndexes++;
							idxPtr[ count++ ] = GetIndex( x - step, j + step );
							numIndexes++;
							idxPtr[ count++ ] = GetIndex( x, j + step + step );
							numIndexes++;

							if ( j < this.options.size - 1 - substep || south == 0 )
							{
								idxPtr[ count++ ] = GetIndex( x, j + step + step );
								numIndexes++;
								idxPtr[ count++ ] = GetIndex( x - step, j + step );
								numIndexes++;
								idxPtr[ count++ ] = GetIndex( x - step, j + step + step );
								numIndexes++;
							}
						}
					}

					if ( south > 0 )
					{
						int x = this.options.size - 1;

						for ( int j = 0; j < this.size - 1; j += substep )
						{
							//skip the first bit of the corner if the north side is a different level as well.
							if ( j > 0 || west == 0 )
							{
								idxPtr[ count++ ] = GetIndex( j, x - step );
								numIndexes++;
								idxPtr[ count++ ] = GetIndex( j, x );
								numIndexes++;
								idxPtr[ count++ ] = GetIndex( j + step, x - step );
								numIndexes++;
							}

							idxPtr[ count++ ] = GetIndex( j + step, x - step );
							numIndexes++;
							idxPtr[ count++ ] = GetIndex( j, x );
							numIndexes++;
							idxPtr[ count++ ] = GetIndex( j + step + step, x );
							numIndexes++;

							if ( j < this.options.size - 1 - substep || east == 0 )
							{
								idxPtr[ count++ ] = GetIndex( j + step, x - step );
								numIndexes++;
								idxPtr[ count++ ] = GetIndex( j + step + step, x );
								numIndexes++;
								idxPtr[ count++ ] = GetIndex( j + step + step, x - step );
								numIndexes++;
							}
						}
					}

					if ( north > 0 )
					{
						for ( int j = 0; j < this.size - 1; j += substep )
						{
							//skip the first bit of the corner if the north side is a different level as well.
							if ( j > 0 || west == 0 )
							{
								idxPtr[ count++ ] = GetIndex( j, 0 );
								numIndexes++;
								idxPtr[ count++ ] = GetIndex( j, step );
								numIndexes++;
								idxPtr[ count++ ] = GetIndex( j + step, step );
								numIndexes++;
							}

							idxPtr[ count++ ] = GetIndex( j, 0 );
							numIndexes++;
							idxPtr[ count++ ] = GetIndex( j + step, step );
							numIndexes++;
							idxPtr[ count++ ] = GetIndex( j + step + step, 0 );
							numIndexes++;

							if ( j < this.options.size - 1 - substep || east == 0 )
							{
								idxPtr[ count++ ] = GetIndex( j + step + step, 0 );
								numIndexes++;
								idxPtr[ count++ ] = GetIndex( j + step, step );
								numIndexes++;
								idxPtr[ count++ ] = GetIndex( j + step + step, step );
								numIndexes++;
							}
						}
					}
				}
				indexData.indexBuffer.Unlock();
				indexData.indexCount = numIndexes;
				indexData.indexStart = 0;

				this.levelIndex[ this.renderLevel, indexArray ] = indexData;
			}
			return indexData;
		}

		public short GetIndex( int x, int z )
		{
			return (short)( x + z * this.options.size );
		}
	}

	[XmlRoot( ElementName = "TerrainConfig", IsNullable = false )]
	public class TerrainOptions
	{
		[XmlElement( ElementName = "DetailTexture" )]
		public string DetailTexture;

		[XmlElement( ElementName = "MaterialName" )]
		public string MaterialName;

		[XmlElement( ElementName = "Terrain" )]
		public string Terrain;

		[XmlElement( ElementName = "WorldTexture" )]
		public string WorldTexture;

		[XmlIgnore]
		public Real[] data; //pointer to the world 2D data.

		[XmlElement( ElementName = "DetailTile" )]
		public int detailTile;

		[XmlElement( ElementName = "VertexNormals" )]
		public bool isLit;

		[XmlElement( ElementName = "MaxMipMapLevel" )]
		public int maxMipmap; //max mip_map level

		[XmlElement( ElementName = "MaxPixelError" )]
		public int maxPixelError;

		[XmlIgnore]
		public int nearPlane;

		[XmlElement( ElementName = "ScaleX" )]
		public float scalex;

		[XmlElement( ElementName = "ScaleY" )]
		public float scaley;

		[XmlElement( ElementName = "ScaleZ" )]
		public float scalez;

		[XmlElement( ElementName = "TileSize" )]
		public int size; //size of this square block

		[XmlIgnore]
		public int startx;

		[XmlIgnore]
		public int startz; //starting coords of this block.

		[XmlIgnore]
		public float topCoord;

		[XmlIgnore]
		public int vertRes;

		[XmlElement( ElementName = "WorldSize" )]
		public int worldSize; //size of the world.

		public TerrainOptions()
		{
			this.size = 0;
			this.worldSize = 0;
			this.startx = 0;
			this.startz = 0;
			this.maxMipmap = 0;
			this.scalex = 1;
			this.scaley = 1;
			this.scalez = 1;
			this.maxPixelError = 4;
			this.vertRes = 768;
			this.topCoord = 1;
			this.nearPlane = 1;
			this.detailTile = 1;
			this.isLit = false;
		}

		public Real GetWorldHeight( int x, int z )
		{
			return this.data[ ( ( z * this.worldSize ) + x ) ];
		}
	}
}
