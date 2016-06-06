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
using System.Collections.Generic;
using System.Diagnostics;
using Axiom.Collections;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Math;
using System.Collections;
using Axiom.Core.Collections;

#endregion Namespace Declarations

namespace OctreeZone
{
	public enum Neighbor
	{
		NORTH = 0,
		SOUTH = 1,
		EAST = 2,
		WEST = 3,
		HERE = 4
	};

	public class TerrainZoneRenderable : SimpleRenderable
	{
		public static short MAIN_BINDING = 0;
		public static short DELTA_BINDING = 1;

		public static int STITCH_NORTH_SHIFT = 0;
		public static int STITCH_SOUTH_SHIFT = 8;
		public static int STITCH_WEST_SHIFT = 16;
		public static int STITCH_EAST_SHIFT = 24;

		public static int STITCH_NORTH = 128 << STITCH_NORTH_SHIFT;
		public static int STITCH_SOUTH = 128 << STITCH_SOUTH_SHIFT;
		public static int STITCH_WEST = 128 << STITCH_WEST_SHIFT;
		public static int STITCH_EAST = 128 << STITCH_EAST_SHIFT;

		public static int MORPH_CUSTOM_PARAM_ID = 77;


		/// Parent Zone
		private readonly TerrainZone mTerrainZone;

		/// Link to shared options
		private readonly TerrainZoneOptions mOptions;

		private VertexData mTerrain;

		/// The current LOD level
		private int mRenderLevel;

		/// The previous 'next' LOD level down, for frame coherency
		private int mLastNextLevel;

		/// The morph factor between this and the next LOD level down
		private Real mLODMorphFactor;

		/// List of squared distances at which LODs change
		private Real[] mMinLevelDistSqr;

		/// Connection to tiles four neighbours
		private readonly TerrainZoneRenderable[] mNeighbors;

		/// Whether light list need to re-calculate
		private bool mLightListDirty;

		/// Cached light list
		private readonly LightList mLightList = new LightList();

		/// The bounding radius of this tile
		//Real mBoundingRadius;
		/// Bounding box of this tile
		private AxisAlignedBox mBounds;

		/// The center point of this tile
		private Vector3 mCenter;

		/// The MovableObject type
		//static string mType;
		/// Current material used by this tile
		//Material mMaterial;
		/// Whether this tile has been initialised
		private bool mInit;

		/// The buffer with all the renderable geometry in it
		private HardwareVertexBuffer mMainBuffer;

		/// Optional set of delta buffers, used to morph from one LOD to the next
		private AxiomSortedCollection<int, HardwareVertexBuffer> mDeltaBuffers =
			new AxiomSortedCollection<int, HardwareVertexBuffer>();

		/// System-memory buffer with just positions in it, for CPU operations
		private float[] mPositionBuffer;

		/// Forced rendering LOD level, optional
		private readonly int mForcedRenderLevel;

		/// Array of LOD indexes specifying which LOD is the next one down
		/// (deals with clustered error metrics which cause LODs to be skipped)
		private readonly int[] mNextLevelDown = new int[10];

		private Real boundingRadius;

		//private bool castsShadows;

		//private Material material;

		//private Technique technique;

		private bool normalizeNormals;

		private ushort numWorldTransforms;

		private bool useIdentityProjection;

		private bool useIdentityView;

		private bool polygonModeOverrideable = true;

		private Quaternion worldOrientation;

		private Vector3 worldPosition;

		/// Bounding box of this tile
		public override AxisAlignedBox BoundingBox
		{
			get
			{
				return this.mBounds;
			}
		}

		/// <summary>
		///		An abstract method required by subclasses to return the bounding box of this object in local coordinates.
		/// </summary>
		public override Real BoundingRadius
		{
			get
			{
				return this.boundingRadius;
			}
		}

		/** Returns the index into the height array for the given coords. */

		public ushort Index( int x, int z )
		{
			return (ushort)( x + z*this.mOptions.tileSize );
		}

		/** Returns the  vertex coord for the given coordinates */

		public float Vertex( int x, int z, int n )
		{
			return this.mPositionBuffer[ x*3 + z*this.mOptions.tileSize*3 + n ];
		}

		public int NumNeighbors()
		{
			var n = 0;

			for ( var i = 0; i < 4; i++ )
			{
				if ( this.mNeighbors[ i ] != null )
				{
					n++;
				}
			}

			return n;
		}

		public bool HasNeighborRenderLevel( int i )
		{
			for ( var j = 0; j < 4; j++ )
			{
				if ( this.mNeighbors[ j ] != null && this.mNeighbors[ j ].mRenderLevel == i )
				{
					return true;
				}
			}

			return false;
		}

		public TerrainZoneRenderable GetNeighbor( Neighbor neighbor )
		{
			return this.mNeighbors[ (int)neighbor ];
		}

		public void SetNeighbor( Neighbor n, TerrainZoneRenderable t )
		{
			this.mNeighbors[ (int)n ] = t;
		}

		public TerrainZoneRenderable( string name, TerrainZone tsm )
			: base()
		{
			this.name = name;
			this.mTerrainZone = tsm;
			this.mTerrain = null;
			this.mPositionBuffer = null;
			this.mForcedRenderLevel = -1;
			this.mLastNextLevel = -1;
			this.mMinLevelDistSqr = null;
			this.mInit = false;
			this.mLightListDirty = true;
			castShadows = false;
			this.mNeighbors = new TerrainZoneRenderable[4];

			this.mOptions = this.mTerrainZone.Options;
		}

		public void DeleteGeometry()
		{
			if ( null != this.mTerrain )
			{
				this.mTerrain = null;
			}

			if ( null != this.mPositionBuffer )
			{
				this.mPositionBuffer = null;
			}

			if ( null != this.mMinLevelDistSqr )
			{
				this.mMinLevelDistSqr = null;
			}
		}

#if !AXIOM_UNSAFE_ONLY
		unsafe public void Initialize( int startx, int startz, Real[] pageHeightData )
#else
		public void Initialize( int startx, int startz, Real[] pageHeightData )
#endif
		{
			if ( this.mOptions.maxGeoMipMapLevel != 0 )
			{
				var i = (int)1 << ( this.mOptions.maxGeoMipMapLevel - 1 );

				if ( ( i + 1 ) > this.mOptions.tileSize )
				{
					LogManager.Instance.Write( "Invalid maximum mipmap specifed, must be n, such that 2^(n-1)+1 < tileSize \n" );
					return;
				}
			}

			DeleteGeometry();

			//calculate min and max heights;
			Real min = 256000, max = 0;

			this.mTerrain = new VertexData();
			this.mTerrain.vertexStart = 0;
			this.mTerrain.vertexCount = this.mOptions.tileSize*this.mOptions.tileSize;

			renderOperation.useIndices = true;
			renderOperation.operationType = this.mOptions.useTriStrips ? OperationType.TriangleStrip : OperationType.TriangleList;
			renderOperation.vertexData = this.mTerrain;
			renderOperation.indexData = GetIndexData();

			var decl = this.mTerrain.vertexDeclaration;
			var bind = this.mTerrain.vertexBufferBinding;

			// positions
			var offset = 0;
			decl.AddElement( MAIN_BINDING, offset, VertexElementType.Float3, VertexElementSemantic.Position );
			offset += VertexElement.GetTypeSize( VertexElementType.Float3 );
			if ( this.mOptions.lit )
			{
				decl.AddElement( MAIN_BINDING, offset, VertexElementType.Float3, VertexElementSemantic.Position );
				offset += VertexElement.GetTypeSize( VertexElementType.Float3 );
			}
			// texture coord sets
			decl.AddElement( MAIN_BINDING, offset, VertexElementType.Float2, VertexElementSemantic.TexCoords, 0 );
			offset += VertexElement.GetTypeSize( VertexElementType.Float2 );
			decl.AddElement( MAIN_BINDING, offset, VertexElementType.Float2, VertexElementSemantic.TexCoords, 1 );
			offset += VertexElement.GetTypeSize( VertexElementType.Float2 );
			if ( this.mOptions.coloured )
			{
				decl.AddElement( MAIN_BINDING, offset, VertexElementType.Color, VertexElementSemantic.Diffuse );
				offset += VertexElement.GetTypeSize( VertexElementType.Color );
			}

			// Create shared vertex buffer
			this.mMainBuffer = HardwareBufferManager.Instance.CreateVertexBuffer( decl.Clone( MAIN_BINDING ),
			                                                                      this.mTerrain.vertexCount,
			                                                                      BufferUsage.StaticWriteOnly );
			// Create system memory copy with just positions in it, for use in simple reads
			//mPositionBuffer = OGRE_ALLOC_T(float, mTerrain.vertexCount * 3, MEMCATEGORY_GEOMETRY);
			this.mPositionBuffer = new float[this.mTerrain.vertexCount*3];

			bind.SetBinding( MAIN_BINDING, this.mMainBuffer );

			if ( this.mOptions.lodMorph )
			{
				// Create additional element for delta
				decl.AddElement( DELTA_BINDING, 0, VertexElementType.Float1, VertexElementSemantic.BlendWeights );
				// NB binding is not set here, it is set when deriving the LOD
			}


			this.mInit = true;

			this.mRenderLevel = 0;

			this.mMinLevelDistSqr = new Real[this.mOptions.maxGeoMipMapLevel];

			var endx = startx + this.mOptions.tileSize;

			var endz = startz + this.mOptions.tileSize;

			Vector3 left, down, here;

			var poselem = decl.FindElementBySemantic( VertexElementSemantic.Position );
			var texelem0 = decl.FindElementBySemantic( VertexElementSemantic.TexCoords, 0 );
			var texelem1 = decl.FindElementBySemantic( VertexElementSemantic.TexCoords, 1 );
			//fixed ( float* pSysPos = mPositionBuffer )
			{
				var pos = 0;
				var pBase = this.mMainBuffer.Lock( BufferLocking.Discard );

				for ( var j = startz; j < endz; j++ )
				{
					for ( var i = startx; i < endx; i++ )
					{
						var pPos = ( pBase + poselem.Offset ).ToFloatPointer();
						var pTex0 = ( pBase + texelem0.Offset ).ToFloatPointer();
						var pTex1 = ( pBase + texelem1.Offset ).ToFloatPointer();
						//poselem.baseVertexPointerToElement(pBase, &pPos);

						//texelem0.baseVertexPointerToElement(pBase, &pTex0);
						//texelem1.baseVertexPointerToElement(pBase, &pTex1);

						var height = pageHeightData[ j*this.mOptions.pageSize + i ];
						height = height*this.mOptions.scale.y; // scale height

						//*pSysPos++ = *pPos++ = (float) i*mOptions.scale.x; //x
						//*pSysPos++ = *pPos++ = height; // y
						//*pSysPos++ = *pPos++ = (float) j*mOptions.scale.z; //z

						this.mPositionBuffer[ pos++ ] = pPos[ 0 ] = (float)i*this.mOptions.scale.x; //x
						this.mPositionBuffer[ pos++ ] = pPos[ 1 ] = height; // y
						this.mPositionBuffer[ pos++ ] = pPos[ 2 ] = (float)j*this.mOptions.scale.z; //z

						pTex0[ 0 ] = (float)i/(float)( this.mOptions.pageSize - 1 );
						pTex0[ 1 ] = (float)j/(float)( this.mOptions.pageSize - 1 );

						pTex1[ 0 ] = ( (float)i/(float)( this.mOptions.tileSize - 1 ) )*this.mOptions.detailTile;
						pTex1[ 1 ] = ( (float)j/(float)( this.mOptions.tileSize - 1 ) )*this.mOptions.detailTile;

						if ( height < min )
						{
							min = (Real)height;
						}

						if ( height > max )
						{
							max = (Real)height;
						}

						pBase += this.mMainBuffer.VertexSize;
					}
				}

				this.mMainBuffer.Unlock();
				this.mBounds = new AxisAlignedBox();
				this.mBounds.SetExtents( new Vector3( (Real)startx*this.mOptions.scale.x, min, (Real)startz*this.mOptions.scale.z ),
				                         new Vector3( (Real)( endx - 1 )*this.mOptions.scale.x, max,
				                                      (Real)( endz - 1 )*this.mOptions.scale.z ) );

				this.mCenter = new Vector3( ( startx*this.mOptions.scale.x + ( endx - 1 )*this.mOptions.scale.x )/2, ( min + max )/2,
				                            ( startz*this.mOptions.scale.z + ( endz - 1 )*this.mOptions.scale.z )/2 );
				this.boundingRadius =
					Math.Sqrt( Utility.Sqr( max - min ) + Utility.Sqr( ( endx - 1 - startx )*this.mOptions.scale.x ) +
					           Utility.Sqr( ( endz - 1 - startz )*this.mOptions.scale.z ) )/2;

				// Create delta buffer list if required to morph
				if ( this.mOptions.lodMorph )
				{
					// Create delta buffer for all except the lowest mip
					this.mDeltaBuffers = new AxiomSortedCollection<int, HardwareVertexBuffer>( this.mOptions.maxGeoMipMapLevel - 1 );
				}

				var C = CalculateCFactor();

				CalculateMinLevelDist2( C );
			}
		}

		public void AdjustRenderLevel( int i )
		{
			this.mRenderLevel = i;
		}

		public Real CalculateCFactor()
		{
			Real A, T;

			if ( null == this.mOptions.primaryCamera )
			{
				throw new AxiomException( "You have not created a camera yet! TerrainZoneRenderable._calculateCFactor" );
			}

			//A = 1 / Math::Tan(Math::AngleUnitsToRadians(opts.primaryCamera.getFOVy()));
			// Turn off detail compression at higher FOVs
			A = 1.0f;

			var vertRes = this.mOptions.primaryCamera.Viewport.ActualHeight;

			T = 2*(Real)this.mOptions.maxPixelError/(Real)vertRes;

			return A/T;
		}

		public float GetHeightAt( float x, float z )
		{
			Vector3 start;
			Vector3 end;

			start.x = Vertex( 0, 0, 0 );
			start.y = Vertex( 0, 0, 1 );
			start.z = Vertex( 0, 0, 2 );

			end.x = Vertex( this.mOptions.tileSize - 1, this.mOptions.tileSize - 1, 0 );
			end.y = Vertex( this.mOptions.tileSize - 1, this.mOptions.tileSize - 1, 1 );
			end.z = Vertex( this.mOptions.tileSize - 1, this.mOptions.tileSize - 1, 2 );

			/* Safety catch, if the point asked for is outside
			* of this tile, it will ask the appropriate tile
			*/

			if ( x < start.x )
			{
				if ( this.mNeighbors[ (int)Neighbor.WEST ] != null )
				{
					return this.mNeighbors[ (int)Neighbor.WEST ].GetHeightAt( x, z );
				}
				else
				{
					x = start.x;
				}
			}

			if ( x > end.x )
			{
				if ( this.mNeighbors[ (int)Neighbor.EAST ] != null )
				{
					return this.mNeighbors[ (int)Neighbor.EAST ].GetHeightAt( x, z );
				}
				else
				{
					x = end.x;
				}
			}

			if ( z < start.z )
			{
				if ( this.mNeighbors[ (int)Neighbor.NORTH ] != null )
				{
					return this.mNeighbors[ (int)Neighbor.NORTH ].GetHeightAt( x, z );
				}
				else
				{
					z = start.z;
				}
			}

			if ( z > end.z )
			{
				if ( this.mNeighbors[ (int)Neighbor.SOUTH ] != null )
				{
					return this.mNeighbors[ (int)Neighbor.SOUTH ].GetHeightAt( x, z );
				}
				else
				{
					z = end.z;
				}
			}


			float x_pct = ( x - start.x )/( end.x - start.x );
			float z_pct = ( z - start.z )/( end.z - start.z );

			var x_pt = x_pct*(float)( this.mOptions.tileSize - 1 );
			var z_pt = z_pct*(float)( this.mOptions.tileSize - 1 );

			var x_index = (int)x_pt;
			var z_index = (int)z_pt;

			// If we got to the far right / bottom edge, move one back
			if ( x_index == this.mOptions.tileSize - 1 )
			{
				--x_index;
				x_pct = 1.0f;
			}
			else
			{
				// get remainder
				x_pct = x_pt - x_index;
			}
			if ( z_index == this.mOptions.tileSize - 1 )
			{
				--z_index;
				z_pct = 1.0f;
			}
			else
			{
				z_pct = z_pt - z_index;
			}

			//bilinear interpolate to find the height.

			var t1 = Vertex( x_index, z_index, 1 );
			var t2 = Vertex( x_index + 1, z_index, 1 );
			var b1 = Vertex( x_index, z_index + 1, 1 );
			var b2 = Vertex( x_index + 1, z_index + 1, 1 );

			var midpoint = ( b1 + t2 )/2.0f;

			if ( x_pct + z_pct <= 1 )
			{
				b2 = midpoint + ( midpoint - t1 );
			}
			else
			{
				t1 = midpoint + ( midpoint - b2 );
			}

			var t = ( t1*( 1 - x_pct ) ) + ( t2*( x_pct ) );
			var b = ( b1*( 1 - x_pct ) ) + ( b2*( x_pct ) );

			var h = ( t*( 1 - z_pct ) ) + ( b*( z_pct ) );

			return h;
		}

		public void GetNormalAt( float x, float z, ref Vector3 result )
		{
			//Assert(mOptions.lit, "No normals present");

			Vector3 here, left, down;
			here = Vector3.Zero;
			left = Vector3.Zero;
			down = Vector3.Zero;
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

			// result.x = - result.x;
			// result.y = - result.y;
			// result.z = - result.z;
		}

		public bool IntersectSegment( Vector3 start, Vector3 end, ref Vector3 result )
		{
			var dir = end - start;
			var ray = start;

			//special case...
			if ( dir.x == 0 && dir.z == 0 )
			{
				if ( ray.y <= GetHeightAt( ray.x, ray.z ) )
				{
					//if ( result != Vector3.Zero )
					result = start;

					return true;
				}
			}

			dir.Normalize();

			//dir.x *= mScale.x;
			//dir.y *= mScale.y;
			//dir.z *= mScale.z;

			var box = BoundingBox;
			//start with the next one...
			ray += dir;


			while (
				!( ( ray.x < box.Minimum.x ) || ( ray.x > box.Maximum.x ) || ( ray.z < box.Minimum.z ) || ( ray.z > box.Maximum.z ) ) )
			{
				var h = GetHeightAt( ray.x, ray.z );

				if ( ray.y <= h )
				{
					//if ( result != Vector3.Zero )
					result = ray;

					return true;
				}

				else
				{
					ray += dir;
				}
			}

			if ( ray.x < box.Minimum.x && this.mNeighbors[ (int)Neighbor.WEST ] != null )
			{
				return this.mNeighbors[ (int)Neighbor.WEST ].IntersectSegment( ray, end, ref result );
			}
			else if ( ray.z < box.Minimum.z && this.mNeighbors[ (int)Neighbor.NORTH ] != null )
			{
				return this.mNeighbors[ (int)Neighbor.NORTH ].IntersectSegment( ray, end, ref result );
			}
			else if ( ray.x > box.Maximum.x && this.mNeighbors[ (int)Neighbor.EAST ] != null )
			{
				return this.mNeighbors[ (int)Neighbor.EAST ].IntersectSegment( ray, end, ref result );
			}
			else if ( ray.z > box.Maximum.z && this.mNeighbors[ (int)Neighbor.SOUTH ] != null )
			{
				return this.mNeighbors[ (int)Neighbor.SOUTH ].IntersectSegment( ray, end, ref result );
			}
			else
			{
				//if ( result != 0 )
				result = new Vector3( -1, -1, -1 );

				return false;
			}
		}

		public void GenerateVertexLighting( Vector3 sunlight, ColorEx ambient )
		{
			var pt = Vector3.Zero;
			var normal = Vector3.Zero;
			Vector3 light;

			var vbuf = this.mTerrain.vertexBufferBinding.GetBuffer( (short)MAIN_BINDING );

			var elem = this.mTerrain.vertexDeclaration.FindElementBySemantic( VertexElementSemantic.Diffuse );
			//for each point in the terrain, see if it's in the line of sight for the sun.
			for ( var i = 0; i < this.mOptions.tileSize; i++ )
			{
				for ( var j = 0; j < this.mOptions.tileSize; j++ )
				{
					//  printf( "Checking %f,%f,%f ", pt.x, pt.y, pt.z );
					pt.x = Vertex( i, j, 0 );
					pt.y = Vertex( i, j, 1 );
					pt.z = Vertex( i, j, 2 );

					light = sunlight - pt;

					light.Normalize();

					if ( !IntersectSegment( pt, sunlight, ref normal ) )
					{
						//
						GetNormalAt( Vertex( i, j, 0 ), Vertex( i, j, 2 ), ref normal );

						float l = light.Dot( normal );

						var v = new ColorEx();
						v.r = ambient.r + l;
						v.g = ambient.g + l;
						v.b = ambient.b + l;

						if ( v.r > 1 )
						{
							v.r = 1;
						}

						if ( v.g > 1 )
						{
							v.g = 1;
						}

						if ( v.b > 1 )
						{
							v.b = 1;
						}

						if ( v.r < 0 )
						{
							v.r = 0;
						}

						if ( v.g < 0 )
						{
							v.g = 0;
						}

						if ( v.b < 0 )
						{
							v.b = 0;
						}

						var colour = Root.Instance.ConvertColor( v );
						//Check: Should be a better way...
						var bufcolour = BufferBase.Wrap( colour, sizeof( int ) );
						vbuf.WriteData( ( Index( i, j )*vbuf.VertexSize ) + elem.Offset, sizeof ( int ), bufcolour );
					}

					else
					{
						var colour = Root.Instance.ConvertColor( ambient );
						var bufcolour = BufferBase.Wrap( colour, sizeof( int ) );
						vbuf.WriteData( ( Index( i, j )*vbuf.VertexSize ) + elem.Offset, sizeof ( int ), bufcolour );
					}
				}
			}
		}

		public override void NotifyCurrentCamera( Camera cam )
		{
			if ( this.mForcedRenderLevel >= 0 )
			{
				this.mRenderLevel = this.mForcedRenderLevel;
				return;
			}


			var cpos = cam.DerivedPosition;
			var aabb = GetWorldBoundingBox( true );
			var diff = new Vector3( 0, 0, 0 );
			diff.Floor( cpos - aabb.Minimum );
			diff.Ceil( cpos - aabb.Maximum );

			var L = diff.LengthSquared;

			this.mRenderLevel = -1;

			for ( var i = 0; i < this.mOptions.maxGeoMipMapLevel; i++ )
			{
				if ( this.mMinLevelDistSqr[ i ] > L )
				{
					this.mRenderLevel = i - 1;
					break;
				}
			}

			if ( this.mRenderLevel < 0 )
			{
				this.mRenderLevel = this.mOptions.maxGeoMipMapLevel - 1;
			}

			if ( this.mOptions.lodMorph )
			{
				// Get the next LOD level down
				var nextLevel = this.mNextLevelDown[ this.mRenderLevel ];
				if ( nextLevel == 0 )
				{
					// No next level, so never morph
					this.mLODMorphFactor = 0;
				}
				else
				{
					// Set the morph such that the morph happens in the last 0.25 of
					// the distance range
					var range = this.mMinLevelDistSqr[ nextLevel ] - this.mMinLevelDistSqr[ this.mRenderLevel ];
					if ( range > 0 )
					{
						var percent = ( L - this.mMinLevelDistSqr[ this.mRenderLevel ] )/range;
						// scale result so that msLODMorphStart == 0, 1 == 1, clamp to 0 below that
						var rescale = 1.0f/( 1.0f - this.mOptions.lodMorphStart );
						this.mLODMorphFactor = Math.Max( ( percent - this.mOptions.lodMorphStart )*rescale, 0.0 );
					}
					else
					{
						// Identical ranges
						this.mLODMorphFactor = 0.0f;
					}

					//assert(mLODMorphFactor >= 0 && mLODMorphFactor <= 1);
				}

				// Bind the correct delta buffer if it has changed
				// nextLevel - 1 since the first entry is for LOD 1 (since LOD 0 never needs it)
				if ( this.mLastNextLevel != nextLevel )
				{
					if ( nextLevel > 0 )
					{
						this.mTerrain.vertexBufferBinding.SetBinding( (short)DELTA_BINDING, this.mDeltaBuffers[ nextLevel - 1 ] );
					}
					else
					{
						// bind dummy (incase bindings checked)
						this.mTerrain.vertexBufferBinding.SetBinding( (short)DELTA_BINDING, this.mDeltaBuffers[ 0 ] );
					}
				}
				this.mLastNextLevel = nextLevel;
			}
		}

		private bool added = false;

		public override void UpdateRenderQueue( RenderQueue queue )
		{
			// Notify need to calculate light list when our sending to render queue
			this.mLightListDirty = true;

			//if ( !added )
			{
				queue.AddRenderable( this, renderQueueID );
				this.added = true;
			}
		}

		//---------------------------------------------------------------------
		//public void visitRenderables(Visitor visitor, bool debugRenderables)
		//{
		//    visitor.visit(this, 0, false);
		//}
		//-----------------------------------------------------------------------
		//public void GetRenderOperation( ref RenderOperation op )
		//{
		//    //setup indexes for vertices and uvs...

		//    Debug.Assert( mInit, "Uninitialized. TerrainZoneRenderable.GetRenderOperation" );

		//    op.useIndices = true;
		//    op.operationType = mOptions.useTriStrips ? OperationType.TriangleStrip : OperationType.TriangleList;
		//    op.vertexData = mTerrain;
		//    op.indexData = GetIndexData();
		//}

		//-----------------------------------------------------------------------
		public Quaternion GetWorldOrientation()
		{
			return parentNode.DerivedOrientation;
		}

		//-----------------------------------------------------------------------
		public Vector3 GetWorldPosition()
		{
			return parentNode.DerivedPosition;
		}

		//-----------------------------------------------------------------------
		public bool CheckSize( int n )
		{
			for ( var i = 0; i < 10; i++ )
			{
				if ( ( ( 1 << i ) + 1 ) == n )
				{
					return true;
				}
			}

			return false;
		}

#if !AXIOM_UNSAFE_ONLY
		unsafe public void CalculateNormals()
#else
		public void CalculateNormals()
#endif
		{
			var norm = Vector3.Zero;

			Debug.Assert( this.mOptions.lit, "No normals present" );

			var vbuf = this.mTerrain.vertexBufferBinding.GetBuffer( (short)MAIN_BINDING );
			var elem = this.mTerrain.vertexDeclaration.FindElementBySemantic( VertexElementSemantic.Normal );
			var pBase = vbuf.Lock( BufferLocking.Discard );

			for ( var j = 0; j < this.mOptions.tileSize; j++ )
			{
				for ( var i = 0; i < this.mOptions.tileSize; i++ )
				{
					GetNormalAt( Vertex( i, j, 0 ), Vertex( i, j, 2 ), ref norm );

					//  printf( "Normal = %5f,%5f,%5f\n", norm.x, norm.y, norm.z );
					//elem.baseVertexPointerToElement(pBase, &pNorm);
					var pNorm = ( pBase + elem.Offset ).ToFloatPointer();

					pNorm[ 0 ] = norm.x;
					pNorm[ 1 ] = norm.y;
					pNorm[ 2 ] = norm.z;
					pBase += vbuf.VertexSize;
				}
			}
			vbuf.Unlock();
		}

#if !AXIOM_UNSAFE_ONLY
		unsafe public void CalculateMinLevelDist2( Real C )
#else
		public void CalculateMinLevelDist2( Real C )
#endif
		{
			//level 0 has no delta.
			this.mMinLevelDistSqr[ 0 ] = 0;

			int i, j;

			for ( var level = 1; level < this.mOptions.maxGeoMipMapLevel; level++ )
			{
				this.mMinLevelDistSqr[ level ] = 0;

				var step = 1 << level;
				// The step of the next higher LOD
				var higherstep = step >> 1;

#if AXIOM_SAFE_ONLY
					ITypePointer<float> pDeltas = null;
#else
				float* pDeltas = null;
#endif
				BufferBase dataPtr;
				if ( this.mOptions.lodMorph )
				{
					// Create a set of delta values (store at index - 1 since 0 has none)
					this.mDeltaBuffers[ level - 1 ] = CreateDeltaBuffer();
					// Lock, but don't discard (we want the pre-initialised zeros)

					dataPtr = this.mDeltaBuffers[ level - 1 ].Lock( BufferLocking.Normal );
					pDeltas = dataPtr.ToFloatPointer();
				}

				for ( j = 0; j < this.mOptions.tileSize - step; j += step )
				{
					for ( i = 0; i < this.mOptions.tileSize - step; i += step )
					{
						/* Form planes relating to the lower detail tris to be produced
						For tri lists and even tri strip rows, they are this shape:
						x---x
						| / |
						x---x
						For odd tri strip rows, they are this shape:
						x---x
						| \ |
						x---x
						*/

						var v1 = new Vector3( Vertex( i, j, 0 ), Vertex( i, j, 1 ), Vertex( i, j, 2 ) );
						var v2 = new Vector3( Vertex( i + step, j, 0 ), Vertex( i + step, j, 1 ), Vertex( i + step, j, 2 ) );
						var v3 = new Vector3( Vertex( i, j + step, 0 ), Vertex( i, j + step, 1 ), Vertex( i, j + step, 2 ) );
						var v4 = new Vector3( Vertex( i + step, j + step, 0 ), Vertex( i + step, j + step, 1 ),
						                      Vertex( i + step, j + step, 2 ) );

						Plane t1, t2;
						t1 = new Plane();
						t2 = new Plane();
						var backwardTri = false;
						if ( !this.mOptions.useTriStrips || j%2 == 0 )
						{
							t1.Redefine( v1, v3, v2 );
							t2.Redefine( v2, v3, v4 );
						}
						else
						{
							t1.Redefine( v1, v3, v4 );
							t2.Redefine( v1, v4, v2 );
							backwardTri = true;
						}

						// include the bottommost row of vertices if this is the last row
						var zubound = ( j == ( this.mOptions.tileSize - step ) ? step : step - 1 );
						for ( var z = 0; z <= zubound; z++ )
						{
							// include the rightmost col of vertices if this is the last col
							var xubound = ( i == ( this.mOptions.tileSize - step ) ? step : step - 1 );
							for ( var x = 0; x <= xubound; x++ )
							{
								var fulldetailx = i + x;
								var fulldetailz = j + z;
								if ( fulldetailx%step == 0 && fulldetailz%step == 0 )
								{
									// Skip, this one is a vertex at this level
									continue;
								}

								var zpct = (Real)z/(Real)step;
								var xpct = (Real)x/(Real)step;

								//interpolated height
								var actualPos = new Vector3( Vertex( fulldetailx, fulldetailz, 0 ), Vertex( fulldetailx, fulldetailz, 1 ),
								                             Vertex( fulldetailx, fulldetailz, 2 ) );
								Real interp_h;
								// Determine which tri we're on
								if ( ( xpct + zpct <= 1.0f && !backwardTri ) || ( xpct + ( 1 - zpct ) <= 1.0f && backwardTri ) )
								{
									// Solve for x/z
									interp_h = ( -( t1.Normal.x*actualPos.x ) - t1.Normal.z*actualPos.z - t1.D )/t1.Normal.y;
								}
								else
								{
									// Second tri
									interp_h = ( -( t2.Normal.x*actualPos.x ) - t2.Normal.z*actualPos.z - t2.D )/t2.Normal.y;
								}

								Real actual_h = Vertex( fulldetailx, fulldetailz, 1 );
								//Check: not sure about fabs used here...
								Real delta = Math.Abs( interp_h - actual_h );

								var D2 = delta*delta*C*C;

								if ( this.mMinLevelDistSqr[ level ] < D2 )
								{
									this.mMinLevelDistSqr[ level ] = D2;
								}

								// Should be save height difference?
								// Don't morph along edges
								if ( this.mOptions.lodMorph && fulldetailx != 0 && fulldetailx != ( this.mOptions.tileSize - 1 ) &&
								     fulldetailz != 0 &&
								     fulldetailz != ( this.mOptions.tileSize - 1 ) )
								{
									// Save height difference
									pDeltas[ (int)( fulldetailx + ( fulldetailz*this.mOptions.tileSize ) ) ] = interp_h - actual_h;
								}
							}
						}
					}
				}

				// Unlock morph deltas if required
				if ( this.mOptions.lodMorph )
				{
					this.mDeltaBuffers[ level - 1 ].Unlock();
				}
			}


			// Post validate the whole set
			for ( i = 1; i < this.mOptions.maxGeoMipMapLevel; i++ )
			{
				// Make sure no LOD transition within the tile
				// This is especially a problem when using large tiles with flat areas
				/* Hmm, this can look bad on some areas, disable for now
				Vector3 delta(_vertex(0,0,0), mCenter.y, _vertex(0,0,2));
				delta = delta - mCenter;
				Real minDist = delta.squaredLength();
				mMinLevelDistSqr[ i ] = std::max(mMinLevelDistSqr[ i ], minDist);
				*/

				//make sure the levels are increasing...
				if ( this.mMinLevelDistSqr[ i ] < this.mMinLevelDistSqr[ i - 1 ] )
				{
					this.mMinLevelDistSqr[ i ] = this.mMinLevelDistSqr[ i - 1 ];
				}
			}

			// Now reverse traverse the list setting the 'next level down'
			Real lastDist = -1;
			var lastIndex = 0;
			for ( i = this.mOptions.maxGeoMipMapLevel - 1; i >= 0; --i )
			{
				if ( i == this.mOptions.maxGeoMipMapLevel - 1 )
				{
					// Last one is always 0
					lastIndex = i;
					lastDist = this.mMinLevelDistSqr[ i ];
					this.mNextLevelDown[ i ] = 0;
				}
				else
				{
					this.mNextLevelDown[ i ] = lastIndex;
					if ( this.mMinLevelDistSqr[ i ] != lastDist )
					{
						lastIndex = i;
						lastDist = this.mMinLevelDistSqr[ i ];
					}
				}
			}
		}

		public HardwareVertexBuffer CreateDeltaBuffer()
		{
			// Delta buffer is a 1D float buffer of height offsets
			var decl = HardwareBufferManager.Instance.CreateVertexDeclaration();
			decl.AddElement( 0, 0, VertexElementType.Float1, VertexElementSemantic.Position );
			var buf = HardwareBufferManager.Instance.CreateVertexBuffer( decl, this.mOptions.tileSize*this.mOptions.tileSize,
			                                                             BufferUsage.WriteOnly );
			// Fill the buffer with zeros, we will only fill in delta
			var pVoid = buf.Lock( BufferLocking.Discard );
			Memory.Set( pVoid, 0, ( this.mOptions.tileSize*this.mOptions.tileSize )*sizeof ( float ) );
			//memset(pVoid, 0, mOptions.tileSize*mOptions.tileSize*sizeof (float));
			buf.Unlock();

			return buf;
		}

		public IndexData GetIndexData()
		{
			long stitchFlags = 0;

			if ( this.mNeighbors[ (int)Neighbor.EAST ] != null &&
			     this.mNeighbors[ (int)Neighbor.EAST ].mRenderLevel > this.mRenderLevel )
			{
				stitchFlags |= STITCH_EAST;
				stitchFlags |= ( this.mNeighbors[ (int)Neighbor.EAST ].mRenderLevel - this.mRenderLevel ) << STITCH_EAST_SHIFT;
			}

			if ( this.mNeighbors[ (int)Neighbor.WEST ] != null &&
			     this.mNeighbors[ (int)Neighbor.WEST ].mRenderLevel > this.mRenderLevel )
			{
				stitchFlags |= STITCH_WEST;
				stitchFlags |= ( this.mNeighbors[ (int)Neighbor.WEST ].mRenderLevel - this.mRenderLevel ) << STITCH_WEST_SHIFT;
			}

			if ( this.mNeighbors[ (int)Neighbor.NORTH ] != null &&
			     this.mNeighbors[ (int)Neighbor.NORTH ].mRenderLevel > this.mRenderLevel )
			{
				stitchFlags |= STITCH_NORTH;
				stitchFlags |= ( this.mNeighbors[ (int)Neighbor.NORTH ].mRenderLevel - this.mRenderLevel ) << STITCH_NORTH_SHIFT;
			}

			if ( this.mNeighbors[ (int)Neighbor.SOUTH ] != null &&
			     this.mNeighbors[ (int)Neighbor.SOUTH ].mRenderLevel > this.mRenderLevel )
			{
				stitchFlags |= STITCH_SOUTH;
				stitchFlags |= ( this.mNeighbors[ (int)Neighbor.SOUTH ].mRenderLevel - this.mRenderLevel ) << STITCH_SOUTH_SHIFT;
			}

			// Check preexisting
			var levelIndex = this.mTerrainZone.LevelIndex;
			//IndexMap::iterator ii = levelIndex[ mRenderLevel ].find( stitchFlags );
			IndexData indexData;

			if ( null == levelIndex[ this.mRenderLevel ] ||
			     ( ( (KeyValuePair<uint, IndexData>)levelIndex[ this.mRenderLevel ] ).Key & stitchFlags ) == 0 )
			{
				// Create
				if ( this.mOptions.useTriStrips )
				{
					indexData = GenerateTriStripIndexes( (uint)stitchFlags );
				}
				else
				{
					indexData = GenerateTriListIndexes( (uint)stitchFlags );
				}
				levelIndex[ this.mRenderLevel ] = new KeyValuePair<uint, IndexData>( (uint)stitchFlags, indexData );
			}
			else
			{
				indexData = ( (KeyValuePair<uint, IndexData>)levelIndex[ this.mRenderLevel ] ).Value;
			}


			return indexData;
		}

		public IndexData GenerateTriStripIndexes( uint stitchFlags )
		{
			// The step used for the current level
			var step = 1 << this.mRenderLevel;
			// The step used for the lower level
			var lowstep = 1 << ( this.mRenderLevel + 1 );

			var numIndexes = 0;

			// Calculate the number of indexes required
			// This is the number of 'cells' at this detail level x 2
			// plus 3 degenerates to turn corners
			var numTrisAcross = ( ( ( this.mOptions.tileSize - 1 )/step )*2 ) + 3;
			// Num indexes is number of tris + 2
			var new_length = numTrisAcross*( ( this.mOptions.tileSize - 1 )/step ) + 2;
			//this is the maximum for a level.  It wastes a little, but shouldn't be a problem.

			var indexData = new IndexData();
			indexData.indexBuffer = HardwareBufferManager.Instance.CreateIndexBuffer( IndexType.Size16, new_length,
			                                                                          BufferUsage.StaticWriteOnly ); //, false);

			this.mTerrainZone.IndexCache.mCache.Add( indexData );
#if !AXIOM_SAFE_ONLY
			unsafe
#endif
			{
				var pIdx = indexData.indexBuffer.Lock( 0, indexData.indexBuffer.Size, BufferLocking.Discard ).ToUShortPointer();
				var idx = 0;

				// Stripified mesh
				for ( var j = 0; j < this.mOptions.tileSize - 1; j += step )
				{
					int i;
					// Forward strip
					// We just do the |/ here, final | done after
					for ( i = 0; i < this.mOptions.tileSize - 1; i += step )
					{
						var x = new int[4];
						var y = new int[4];
						x[ 0 ] = x[ 1 ] = i;
						x[ 2 ] = x[ 3 ] = i + step;
						y[ 0 ] = y[ 2 ] = j;
						y[ 1 ] = y[ 3 ] = j + step;

						if ( j == 0 && ( stitchFlags & STITCH_NORTH ) != 0 )
						{
							// North reduction means rounding x[0] and x[2]
							if ( x[ 0 ]%lowstep != 0 )
							{
								// Since we know we only drop down one level of LOD,
								// removing 1 step of higher LOD should return to lower
								x[ 0 ] -= step;
							}
							if ( x[ 2 ]%lowstep != 0 )
							{
								x[ 2 ] -= step;
							}
						}

						// Never get a south tiling on a forward strip (always finish on
						// a backward strip)

						if ( i == 0 && ( stitchFlags & STITCH_WEST ) != 0 )
						{
							// West reduction means rounding y[0] / y[1]
							if ( y[ 0 ]%lowstep != 0 )
							{
								y[ 0 ] -= step;
							}
							if ( y[ 1 ]%lowstep != 0 )
							{
								y[ 1 ] -= step;
							}
						}
						if ( i == ( this.mOptions.tileSize - 1 - step ) && ( stitchFlags & STITCH_EAST ) != 0 )
						{
							// East tiling means rounding y[2] & y[3]
							if ( y[ 2 ]%lowstep != 0 )
							{
								y[ 2 ] -= step;
							}
							if ( y[ 3 ]%lowstep != 0 )
							{
								y[ 3 ] -= step;
							}
						}

						//triangles
						if ( i == 0 )
						{
							// Starter
							pIdx[ idx++ ] = (ushort)Index( x[ 0 ], y[ 0 ] );
							numIndexes++;
						}
						pIdx[ idx++ ] = (ushort)Index( x[ 1 ], y[ 1 ] );
						numIndexes++;
						pIdx[ idx++ ] = (ushort)Index( x[ 2 ], y[ 2 ] );
						numIndexes++;

						if ( i == this.mOptions.tileSize - 1 - step )
						{
							// Emit extra index to finish row
							pIdx[ idx++ ] = (ushort)Index( x[ 3 ], y[ 3 ] );
							numIndexes++;
							if ( j < this.mOptions.tileSize - 1 - step )
							{
								// Emit this index twice more (this is to turn around without
								// artefacts)
								// ** Hmm, looks like we can drop this and it's unnoticeable
								// *pIdx++ = ( ushort ) Index( x[ 3 ], y[ 3 ] ); numIndexes++;
								// *pIdx++ = ( ushort ) Index( x[ 3 ], y[ 3 ] ); numIndexes++;
							}
						}
					}
					// Increment row
					j += step;
					// Backward strip
					for ( i = this.mOptions.tileSize - 1; i > 0; i -= step )
					{
						var x = new int[4];
						var y = new int[4];
						x[ 0 ] = x[ 1 ] = i;
						x[ 2 ] = x[ 3 ] = i - step;
						y[ 0 ] = y[ 2 ] = j;
						y[ 1 ] = y[ 3 ] = j + step;

						// Never get a north tiling on a backward strip (always
						// start on a forward strip)
						if ( j == ( this.mOptions.tileSize - 1 - step ) && ( stitchFlags & STITCH_SOUTH ) != 0 )
						{
							// South reduction means rounding x[1] / x[3]
							if ( x[ 1 ]%lowstep != 0 )
							{
								x[ 1 ] -= step;
							}
							if ( x[ 3 ]%lowstep != 0 )
							{
								x[ 3 ] -= step;
							}
						}

						if ( i == step && ( stitchFlags & STITCH_WEST ) != 0 )
						{
							// West tiling on backward strip is rounding of y[2] / y[3]
							if ( y[ 2 ]%lowstep != 0 )
							{
								y[ 2 ] -= step;
							}
							if ( y[ 3 ]%lowstep != 0 )
							{
								y[ 3 ] -= step;
							}
						}
						if ( i == this.mOptions.tileSize - 1 && ( stitchFlags & STITCH_EAST ) != 0 )
						{
							// East tiling means rounding y[0] and y[1] on backward strip
							if ( y[ 0 ]%lowstep != 0 )
							{
								y[ 0 ] -= step;
							}
							if ( y[ 1 ]%lowstep != 0 )
							{
								y[ 1 ] -= step;
							}
						}

						//triangles
						if ( i == this.mOptions.tileSize )
						{
							// Starter
							pIdx[ idx++ ] = (ushort)Index( x[ 0 ], y[ 0 ] );
							numIndexes++;
						}
						pIdx[ idx++ ] = (ushort)Index( x[ 1 ], y[ 1 ] );
						numIndexes++;
						pIdx[ idx++ ] = (ushort)Index( x[ 2 ], y[ 2 ] );
						numIndexes++;

						if ( i == step )
						{
							// Emit extra index to finish row
							pIdx[ idx++ ] = (ushort)Index( x[ 3 ], y[ 3 ] );
							numIndexes++;
							if ( j < this.mOptions.tileSize - 1 - step )
							{
								// Emit this index once more (this is to turn around)
								pIdx[ idx++ ] = (ushort)Index( x[ 3 ], y[ 3 ] );
								numIndexes++;
							}
						}
					}
				}
			}


			indexData.indexBuffer.Unlock();
			indexData.indexCount = numIndexes;
			indexData.indexStart = 0;

			return indexData;
		}

#if !AXIOM_UNSAFE_ONLY
		unsafe public IndexData GenerateTriListIndexes( uint stitchFlags )
#else
		public IndexData GenerateTriListIndexes( uint stitchFlags )
#endif
		{
			var numIndexes = 0;
			var step = 1 << this.mRenderLevel;

			IndexData indexData;

			var north = ( stitchFlags & STITCH_NORTH ) != 0 ? step : 0;
			var south = ( stitchFlags & STITCH_SOUTH ) != 0 ? step : 0;
			var east = ( stitchFlags & STITCH_EAST ) != 0 ? step : 0;
			var west = ( stitchFlags & STITCH_WEST ) != 0 ? step : 0;

			var new_length = ( this.mOptions.tileSize/step )*( this.mOptions.tileSize/step )*2*2*2;
			//this is the maximum for a level.  It wastes a little, but shouldn't be a problem.

			indexData = new IndexData();
			indexData.indexBuffer = HardwareBufferManager.Instance.CreateIndexBuffer( IndexType.Size16, new_length,
			                                                                          BufferUsage.StaticWriteOnly ); //, false);

			this.mTerrainZone.IndexCache.mCache.Add( indexData );

			var ppIdx = indexData.indexBuffer.Lock( 0, indexData.indexBuffer.Size, BufferLocking.Discard );
			var pIdx = ppIdx.ToUShortPointer();
			var idx = 0;

			// Do the core vertices, minus stitches
			for ( var j = north; j < this.mOptions.tileSize - 1 - south; j += step )
			{
				for ( var i = west; i < this.mOptions.tileSize - 1 - east; i += step )
				{
					//triangles
					pIdx[ idx++ ] = Index( i, j + step );
					numIndexes++; // original order: 2
					pIdx[ idx++ ] = Index( i + step, j );
					numIndexes++; // original order: 3
					pIdx[ idx++ ] = Index( i, j );
					numIndexes++; // original order: 1

					pIdx[ idx++ ] = Index( i + step, j + step );
					numIndexes++; // original order: 2
					pIdx[ idx++ ] = Index( i + step, j );
					numIndexes++; // original order: 3
					pIdx[ idx++ ] = Index( i, j + step );
					numIndexes++; // original order: 1
				}
			}

			ppIdx.Ptr += idx*sizeof ( ushort );

			// North stitching
			if ( north > 0 )
			{
				numIndexes += StitchEdge( Neighbor.NORTH, this.mRenderLevel, this.mNeighbors[ (int)Neighbor.NORTH ].mRenderLevel,
				                          west > 0,
				                          east > 0, ppIdx );
			}
			// East stitching
			if ( east > 0 )
			{
				numIndexes += StitchEdge( Neighbor.EAST, this.mRenderLevel, this.mNeighbors[ (int)Neighbor.EAST ].mRenderLevel,
				                          north > 0,
				                          south > 0, ppIdx );
			}
			// South stitching
			if ( south > 0 )
			{
				numIndexes += StitchEdge( Neighbor.SOUTH, this.mRenderLevel, this.mNeighbors[ (int)Neighbor.SOUTH ].mRenderLevel,
				                          east > 0,
				                          west > 0, ppIdx );
			}
			// West stitching
			if ( west > 0 )
			{
				numIndexes += StitchEdge( Neighbor.WEST, this.mRenderLevel, this.mNeighbors[ (int)Neighbor.WEST ].mRenderLevel,
				                          south > 0,
				                          north > 0, ppIdx );
			}


			indexData.indexBuffer.Unlock();
			indexData.indexCount = numIndexes;
			indexData.indexStart = 0;

			return indexData;
		}

		/// <summary>
		///		Update a custom GpuProgramParameters constant which is derived from
		///		information only this Renderable knows.
		/// </summary>
		/// <remarks>
		///		This method allows a Renderable to map in a custom GPU program parameter
		///		based on it's own data. This is represented by a GPU auto parameter
		///		of AutoConstantType.Custom, and to allow there to be more than one of these per
		///		Renderable, the 'data' field on the auto parameter will identify
		///		which parameter is being updated. The implementation of this method
		///		must identify the parameter being updated, and call a 'SetConstant'
		///		method on the passed in <see cref="GpuProgramParameters"/> object, using the details
		///		provided in the incoming auto constant setting to identify the index
		///		at which to set the parameter.
		/// </remarks>
		/// <param name="constantEntry">The auto constant entry referring to the parameter being updated.</param>
		/// <param name="param">The parameters object which this method should call to set the updated parameters.</param>
		public new void UpdateCustomGpuParameter( GpuProgramParameters.AutoConstantEntry constantEntry,
		                                          GpuProgramParameters param )
		{
			if ( constantEntry.Data == MORPH_CUSTOM_PARAM_ID )
			{
				// Update morph LOD factor
				param.SetConstant( constantEntry.PhysicalIndex, this.mLODMorphFactor );
				//_writeRawConstant(constantEntry.PhysicalIndex, mLODMorphFactor);
			}
			else
			{
				base.UpdateCustomGpuParameter( constantEntry, param );
			}
		}

#if !AXIOM_UNSAFE_ONLY
		unsafe public int StitchEdge( Neighbor neighbor, int hiLOD, int loLOD, bool omitFirstTri, bool omitLastTri, BufferBase ppIdx )
#else
		public int StitchEdge( Neighbor neighbor, int hiLOD, int loLOD, bool omitFirstTri, bool omitLastTri, BufferBase ppIdx )
#endif
		{
			Debug.Assert( loLOD > hiLOD, "TerrainZoneRenderable.StitchEdge" );
			/*
			Now do the stitching; we can stitch from any level to any level.
			The stitch pattern is like this for each pair of vertices in the lower LOD
			(excuse the poor ascii art):

			lower LOD
			*-----------*
			|\  \ 3 /  /|
			|1\2 \ / 4/5|
			*--*--*--*--*
			higher LOD

			The algorithm is, for each pair of lower LOD vertices:
			1. Iterate over the higher LOD vertices, generating tris connected to the
			first lower LOD vertex, up to and including 1/2 the span of the lower LOD
			over the higher LOD (tris 1-2). Skip the first tri if it is on the edge
			of the tile and that edge is to be stitched itself.
			2. Generate a single tri for the middle using the 2 lower LOD vertices and
			the middle vertex of the higher LOD (tri 3).
			3. Iterate over the higher LOD vertices from 1/2 the span of the lower LOD
			to the end, generating tris connected to the second lower LOD vertex
			(tris 4-5). Skip the last tri if it is on the edge of a tile and that
			edge is to be stitched itself.

			The same algorithm works for all edges of the patch; stitching is done
			clockwise so that the origin and steps used change, but the general
			approach does not.
			*/

			// Get pointer to be updated
			var pIdx = ppIdx.ToUShortPointer();
			var idx = 0;

			// Work out the steps ie how to increment indexes
			// Step from one vertex to another in the high detail version
			var step = 1 << hiLOD;
			// Step from one vertex to another in the low detail version
			var superstep = 1 << loLOD;
			// Step half way between low detail steps
			var halfsuperstep = superstep >> 1;

			// Work out the starting points and sign of increments
			// We always work the strip clockwise
			int startx, starty, endx, rowstep;
			startx = starty = endx = rowstep = 0;
			var horizontal = false;
			switch ( neighbor )
			{
				case Neighbor.NORTH:
					startx = starty = 0;
					endx = this.mOptions.tileSize - 1;
					rowstep = step;
					horizontal = true;
					break;
				case Neighbor.SOUTH:
					// invert x AND y direction, helps to keep same winding
					startx = starty = this.mOptions.tileSize - 1;
					endx = 0;
					rowstep = -step;
					step = -step;
					superstep = -superstep;
					halfsuperstep = -halfsuperstep;
					horizontal = true;
					break;
				case Neighbor.EAST:
					startx = 0;
					endx = this.mOptions.tileSize - 1;
					starty = this.mOptions.tileSize - 1;
					rowstep = -step;
					horizontal = false;
					break;
				case Neighbor.WEST:
					startx = this.mOptions.tileSize - 1;
					endx = 0;
					starty = 0;
					rowstep = step;
					step = -step;
					superstep = -superstep;
					halfsuperstep = -halfsuperstep;
					horizontal = false;
					break;
			}

			var numIndexes = 0;

			for ( var j = startx; j != endx; j += superstep )
			{
				int k;
				for ( k = 0; k != halfsuperstep; k += step )
				{
					var jk = j + k;
					//skip the first bit of the corner?
					if ( j != startx || k != 0 || !omitFirstTri )
					{
						if ( horizontal )
						{
							pIdx[ idx++ ] = Index( jk, starty + rowstep );
							numIndexes++; // original order: 2
							pIdx[ idx++ ] = Index( jk + step, starty + rowstep );
							numIndexes++; // original order: 3
							pIdx[ idx++ ] = Index( j, starty );
							numIndexes++; // original order: 1
						}
						else
						{
							pIdx[ idx++ ] = Index( starty + rowstep, jk );
							numIndexes++; // original order: 2
							pIdx[ idx++ ] = Index( starty + rowstep, jk + step );
							numIndexes++; // original order: 3
							pIdx[ idx++ ] = Index( starty, j );
							numIndexes++; // original order: 1
						}
					}
				}

				// Middle tri
				if ( horizontal )
				{
					pIdx[ idx++ ] = Index( j + halfsuperstep, starty + rowstep );
					numIndexes++; // original order: 2
					pIdx[ idx++ ] = Index( j + superstep, starty );
					numIndexes++; // original order: 3
					pIdx[ idx++ ] = Index( j, starty );
					numIndexes++; // original order: 1
				}
				else
				{
					pIdx[ idx++ ] = Index( starty + rowstep, j + halfsuperstep );
					numIndexes++; // original order: 2
					pIdx[ idx++ ] = Index( starty, j + superstep );
					numIndexes++; // original order: 3
					pIdx[ idx++ ] = Index( starty, j );
					numIndexes++; // original order: 1
				}

				for ( k = halfsuperstep; k != superstep; k += step )
				{
					var jk = j + k;
					if ( j != endx - superstep || k != superstep - step || !omitLastTri )
					{
						if ( horizontal )
						{
							pIdx[ idx++ ] = Index( jk, starty + rowstep );
							numIndexes++; // original order: 2
							pIdx[ idx++ ] = Index( jk + step, starty + rowstep );
							numIndexes++; // original order: 3
							pIdx[ idx++ ] = Index( j + superstep, starty );
							numIndexes++; // original order: 1
						}
						else
						{
							pIdx[ idx++ ] = Index( starty + rowstep, jk );
							numIndexes++; // original order: 2
							pIdx[ idx++ ] = Index( starty + rowstep, jk + step );
							numIndexes++; // original order: 3
							pIdx[ idx++ ] = Index( starty, j + superstep );
							numIndexes++; // original order: 1
						}
					}
				}
			}

			ppIdx.Ptr += idx*sizeof ( ushort );

			return numIndexes;
		}

		#region Implementation of IRenderable

		/// <summary>
		///    Gets the world transform matrix / matrices for this renderable object.
		/// </summary>
		/// <remarks>
		///    If the object has any derived transforms, these are expected to be up to date as long as
		///    all the SceneNode structures have been updated before this is called.
		///  <p/>
		///    This method will populate xform with 1 matrix if it does not use vertex blending. If it
		///    does use vertex blending it will fill the passed in pointer with an array of matrices,
		///    the length being the value returned from getNumWorldTransforms.
		/// </remarks>
		public override void GetWorldTransforms( Matrix4[] matrices )
		{
			parentNode.GetWorldTransforms( matrices );
		}

		/// <summary>
		///    Gets a list of lights, ordered relative to how close they are to this renderable.
		/// </summary>
		/// <remarks>
		///    Directional lights, which have no position, will always be first on this list.
		/// </remarks>
		public new LightList Lights
		{
			get
			{
				if ( this.mLightListDirty )
				{
					ParentSceneNode.Creator.PopulateLightList( this.mCenter, BoundingRadius, this.mLightList );
					this.mLightListDirty = false;
				}
				return this.mLightList;
			}
		}

		/// <summary>
		///    Returns whether or not this Renderable wishes the hardware to normalize normals.
		/// </summary>
		public new bool NormalizeNormals
		{
			get
			{
				return this.normalizeNormals;
			}
		}

		/// <summary>
		///    Gets the number of world transformations that will be used for this object.
		/// </summary>
		/// <remarks>
		///    When a renderable uses vertex blending, it uses multiple world matrices instead of a single
		///    one. Each vertex sent to the pipeline can reference one or more matrices in this list
		///    with given weights.
		///    If a renderable does not use vertex blending this method returns 1, which is the default for
		///    simplicity.
		/// </remarks>
		public new ushort NumWorldTransforms
		{
			get
			{
				return this.numWorldTransforms;
			}
		}

		/// <summary>
		///    Returns whether or not to use an 'identity' projection.
		/// </summary>
		/// <remarks>
		///    Usually IRenderable objects will use a projection matrix as determined
		///    by the active camera. However, if they want they can cancel this out
		///    and use an identity projection, which effectively projects in 2D using
		///    a {-1, 1} view space. Useful for overlay rendering. Normal renderables need
		///    not override this.
		/// </remarks>
		public override bool UseIdentityProjection
		{
			get
			{
				return this.useIdentityProjection;
			}
		}

		/// <summary>
		///    Returns whether or not to use an 'identity' projection.
		/// </summary>
		/// <remarks>
		///    Usually IRenderable objects will use a view matrix as determined
		///    by the active camera. However, if they want they can cancel this out
		///    and use an identity matrix, which means all geometry is assumed
		///    to be relative to camera space already. Useful for overlay rendering.
		///    Normal renderables need not override this.
		/// </remarks>
		public override bool UseIdentityView
		{
			get
			{
				return this.useIdentityView;
			}
		}

		/// <summary>
		/// Gets whether this renderable's chosen detail level can be
		///	overridden (downgraded) by the camera setting.
		/// override true means that a lower camera detail will override this
		/// renderables detail level, false means it won't.
		/// </summary>
		public override bool PolygonModeOverrideable
		{
			get
			{
				return this.polygonModeOverrideable;
			}
		}

		/// <summary>
		///    Gets the worldspace orientation of this renderable; this is used in order to
		///    more efficiently update parameters to vertex & fragment programs, since inverting Quaterion
		///    and Vector in order to derive object-space positions / directions for cameras and
		///    lights is much more efficient than inverting a complete 4x4 matrix, and also
		///    eliminates problems introduced by scaling.
		/// </summary>
		public override Quaternion WorldOrientation
		{
			get
			{
				return this.worldOrientation;
			}
		}

		/// <summary>
		///    Gets the worldspace position of this renderable; this is used in order to
		///    more efficiently update parameters to vertex & fragment programs, since inverting Quaterion
		///    and Vector in order to derive object-space positions / directions for cameras and
		///    lights is much more efficient than inverting a complete 4x4 matrix, and also
		///    eliminates problems introduced by scaling.
		/// </summary>
		public override Vector3 WorldPosition
		{
			get
			{
				return this.worldPosition;
			}
		}

		/// <summary>
		///		Returns the camera-relative squared depth of this renderable.
		/// </summary>
		/// <remarks>
		///		Used to sort transparent objects. Squared depth is used rather than
		///		actual depth to avoid having to perform a square root on the result.
		/// </remarks>
		/// <param name="camera"></param>
		/// <returns></returns>
		public override Real GetSquaredViewDepth( Camera camera )
		{
			var diff = this.mCenter - camera.DerivedPosition;
			// Use squared length to avoid square root
			return diff.LengthSquared;
		}

		/// <summary>
		/// Get the 'type flags' for this <see cref="TerrainZoneRenderable"/>.
		/// </summary>
		/// <seealso cref="MovableObject.TypeFlags"/>
		public override uint TypeFlags
		{
			get
			{
				return (uint)SceneQueryTypeMask.WorldGeometry;
			}
		}

		#endregion Implementation of IRenderable
	}

	public class TerrainBufferCache
	{
		public void shutdown()
		{
			this.mCache.Clear();
		}

		~TerrainBufferCache()
		{
			shutdown();
		}

		internal List<IndexData> mCache = new List<IndexData>();
	};
}