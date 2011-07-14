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
//     <id value="$Id:$"/>
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
		TerrainZone mTerrainZone;
		/// Link to shared options
		TerrainZoneOptions mOptions;

		VertexData mTerrain;
		/// The current LOD level
		int mRenderLevel;
		/// The previous 'next' LOD level down, for frame coherency
		int mLastNextLevel;
		/// The morph factor between this and the next LOD level down
		Real mLODMorphFactor;
		/// List of squared distances at which LODs change
		Real[] mMinLevelDistSqr;
		/// Connection to tiles four neighbours
		TerrainZoneRenderable[] mNeighbors;
		/// Whether light list need to re-calculate
		bool mLightListDirty;
		/// Cached light list
		LightList mLightList = new LightList();
		/// The bounding radius of this tile
		//Real mBoundingRadius;
		/// Bounding box of this tile
		private AxisAlignedBox mBounds;
		/// The center point of this tile
		Vector3 mCenter;
		/// The MovableObject type
		//static string mType;
		/// Current material used by this tile
		//Material mMaterial;
		/// Whether this tile has been initialised
		bool mInit;
		/// The buffer with all the renderable geometry in it
		private HardwareVertexBuffer mMainBuffer;
		/// Optional set of delta buffers, used to morph from one LOD to the next
		private AxiomSortedCollection<int, HardwareVertexBuffer> mDeltaBuffers = new AxiomSortedCollection<int, HardwareVertexBuffer>();
		/// System-memory buffer with just positions in it, for CPU operations
		float[] mPositionBuffer;
		/// Forced rendering LOD level, optional
		int mForcedRenderLevel;
		/// Array of LOD indexes specifying which LOD is the next one down
		/// (deals with clustered error metrics which cause LODs to be skipped)
		int[] mNextLevelDown = new int[ 10 ];

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
				return mBounds;
			}
		}

		/// <summary>
		///		An abstract method required by subclasses to return the bounding box of this object in local coordinates.
		/// </summary>
		public override float BoundingRadius
		{
			get
			{
				return boundingRadius;
			}
		}

		/** Returns the index into the height array for the given coords. */
		public ushort Index( int x, int z )
		{
			return (ushort)( x + z * mOptions.tileSize );
		}

		/** Returns the  vertex coord for the given coordinates */
		public float Vertex( int x, int z, int n )
		{
			return mPositionBuffer[ x * 3 + z * mOptions.tileSize * 3 + n ];
		}

		public int NumNeighbors()
		{
			int n = 0;

			for ( int i = 0; i < 4; i++ )
			{
				if ( mNeighbors[ i ] != null )
					n++;
			}

			return n;
		}

		public bool HasNeighborRenderLevel( int i )
		{
			for ( int j = 0; j < 4; j++ )
			{
				if ( mNeighbors[ j ] != null && mNeighbors[ j ].mRenderLevel == i )
					return true;
			}

			return false;

		}

		public TerrainZoneRenderable GetNeighbor( Neighbor neighbor )
		{
			return mNeighbors[ (int)neighbor ];
		}

		public void SetNeighbor( Neighbor n, TerrainZoneRenderable t )
		{
			mNeighbors[ (int)n ] = t;
		}

		public TerrainZoneRenderable( string name, TerrainZone tsm )
			: base()
		{
			this.name = name;
			mTerrainZone = tsm;
			mTerrain = null;
			mPositionBuffer = null;
			mForcedRenderLevel = -1;
			mLastNextLevel = -1;
			mMinLevelDistSqr = null;
			mInit = false;
			mLightListDirty = true;
			castShadows = false;
			mNeighbors = new TerrainZoneRenderable[ 4 ];

			mOptions = mTerrainZone.Options;
		}

		public void DeleteGeometry()
		{
			if ( null != mTerrain )
				mTerrain = null;

			if ( null != mPositionBuffer )
				mPositionBuffer = null;

			if ( null != mMinLevelDistSqr )
				mMinLevelDistSqr = null;
		}

		public unsafe void Initialize( int startx, int startz, Real[] pageHeightData )
		{

			if ( mOptions.maxGeoMipMapLevel != 0 )
			{
				int i = (int)1 << ( mOptions.maxGeoMipMapLevel - 1 );

				if ( ( i + 1 ) > mOptions.tileSize )
				{
					LogManager.Instance.Write( "Invalid maximum mipmap specifed, must be n, such that 2^(n-1)+1 < tileSize \n" );
					return;
				}
			}

			DeleteGeometry();

			//calculate min and max heights;
			Real min = 256000, max = 0;

			mTerrain = new VertexData();
			mTerrain.vertexStart = 0;
			mTerrain.vertexCount = mOptions.tileSize * mOptions.tileSize;

			renderOperation.useIndices = true;
			renderOperation.operationType = mOptions.useTriStrips ? OperationType.TriangleStrip : OperationType.TriangleList;
			renderOperation.vertexData = mTerrain;
			renderOperation.indexData = GetIndexData();

			VertexDeclaration decl = mTerrain.vertexDeclaration;
			VertexBufferBinding bind = mTerrain.vertexBufferBinding;

			// positions
			int offset = 0;
			decl.AddElement( MAIN_BINDING, offset, VertexElementType.Float3, VertexElementSemantic.Position );
			offset += VertexElement.GetTypeSize( VertexElementType.Float3 );
			if ( mOptions.lit )
			{
				decl.AddElement( MAIN_BINDING, offset, VertexElementType.Float3, VertexElementSemantic.Position );
				offset += VertexElement.GetTypeSize( VertexElementType.Float3 );
			}
			// texture coord sets
			decl.AddElement( MAIN_BINDING, offset, VertexElementType.Float2, VertexElementSemantic.TexCoords, 0 );
			offset += VertexElement.GetTypeSize( VertexElementType.Float2 );
			decl.AddElement( MAIN_BINDING, offset, VertexElementType.Float2, VertexElementSemantic.TexCoords, 1 );
			offset += VertexElement.GetTypeSize( VertexElementType.Float2 );
			if ( mOptions.coloured )
			{
				decl.AddElement( MAIN_BINDING, offset, VertexElementType.Color, VertexElementSemantic.Diffuse );
				offset += VertexElement.GetTypeSize( VertexElementType.Color );
			}

			// Create shared vertex buffer
			mMainBuffer =
				HardwareBufferManager.Instance.CreateVertexBuffer( decl.Clone( MAIN_BINDING ), mTerrain.vertexCount, BufferUsage.StaticWriteOnly );
			// Create system memory copy with just positions in it, for use in simple reads
			//mPositionBuffer = OGRE_ALLOC_T(float, mTerrain.vertexCount * 3, MEMCATEGORY_GEOMETRY);
			mPositionBuffer = new float[ mTerrain.vertexCount * 3 ];

			bind.SetBinding( MAIN_BINDING, mMainBuffer );

			if ( mOptions.lodMorph )
			{
				// Create additional element for delta
				decl.AddElement( DELTA_BINDING, 0, VertexElementType.Float1, VertexElementSemantic.BlendWeights );
				// NB binding is not set here, it is set when deriving the LOD
			}


			mInit = true;

			mRenderLevel = 0;

			mMinLevelDistSqr = new Real[ mOptions.maxGeoMipMapLevel ];

			int endx = startx + mOptions.tileSize;

			int endz = startz + mOptions.tileSize;

			Vector3 left, down, here;

			VertexElement poselem = decl.FindElementBySemantic( VertexElementSemantic.Position );
			VertexElement texelem0 = decl.FindElementBySemantic( VertexElementSemantic.TexCoords, 0 );
			VertexElement texelem1 = decl.FindElementBySemantic( VertexElementSemantic.TexCoords, 1 );
			//fixed ( float* pSysPos = mPositionBuffer )
			{
				int pos = 0;
				byte* pBase = (byte*)mMainBuffer.Lock( BufferLocking.Discard );

				for ( int j = startz; j < endz; j++ )
				{
					for ( int i = startx; i < endx; i++ )
					{
						float* pPos = (float*)( pBase + poselem.Offset );
						float* pTex0 = (float*)( pBase + texelem0.Offset );
						float* pTex1 = (float*)( pBase + texelem1.Offset );
						//poselem.baseVertexPointerToElement(pBase, &pPos);

						//texelem0.baseVertexPointerToElement(pBase, &pTex0);
						//texelem1.baseVertexPointerToElement(pBase, &pTex1);

						Real height = pageHeightData[ j * mOptions.pageSize + i ];
						height = height * mOptions.scale.y; // scale height

						//*pSysPos++ = *pPos++ = (float) i*mOptions.scale.x; //x
						//*pSysPos++ = *pPos++ = height; // y
						//*pSysPos++ = *pPos++ = (float) j*mOptions.scale.z; //z

						mPositionBuffer[ pos++ ] = *pPos++ = (float)i * mOptions.scale.x; //x
						mPositionBuffer[ pos++ ] = *pPos++ = height; // y
						mPositionBuffer[ pos++ ] = *pPos++ = (float)j * mOptions.scale.z; //z

						*pTex0++ = (float)i / (float)( mOptions.pageSize - 1 );
						*pTex0++ = (float)j / (float)( mOptions.pageSize - 1 );

						*pTex1++ = ( (float)i / (float)( mOptions.tileSize - 1 ) ) * mOptions.detailTile;
						*pTex1++ = ( (float)j / (float)( mOptions.tileSize - 1 ) ) * mOptions.detailTile;

						if ( height < min )
							min = (Real)height;

						if ( height > max )
							max = (Real)height;

						pBase += mMainBuffer.VertexSize;
					}
				}

				mMainBuffer.Unlock();
				mBounds = new AxisAlignedBox();
				mBounds.SetExtents( new Vector3( (Real)startx * mOptions.scale.x, min, (Real)startz * mOptions.scale.z ),
									new Vector3( (Real)( endx - 1 ) * mOptions.scale.x, max,
												 (Real)( endz - 1 ) * mOptions.scale.z ) );

				mCenter = new Vector3( ( startx * mOptions.scale.x + ( endx - 1 ) * mOptions.scale.x ) / 2,
									   ( min + max ) / 2,
									   ( startz * mOptions.scale.z + ( endz - 1 ) * mOptions.scale.z ) / 2 );
				boundingRadius = Math.Sqrt(
									  Utility.Sqr( max - min ) +
									  Utility.Sqr( ( endx - 1 - startx ) * mOptions.scale.x ) +
									  Utility.Sqr( ( endz - 1 - startz ) * mOptions.scale.z ) ) / 2;

				// Create delta buffer list if required to morph
				if ( mOptions.lodMorph )
				{
					// Create delta buffer for all except the lowest mip
					mDeltaBuffers = new AxiomSortedCollection<int, HardwareVertexBuffer>( mOptions.maxGeoMipMapLevel - 1 );
				}

				Real C = CalculateCFactor();

				CalculateMinLevelDist2( C );
			}
		}

		public void AdjustRenderLevel( int i )
		{

			mRenderLevel = i;
		}

		public Real CalculateCFactor()
		{
			Real A, T;

			if ( null == mOptions.primaryCamera )
			{
				throw new AxiomException( "You have not created a camera yet! TerrainZoneRenderable._calculateCFactor" );
			}

			//A = 1 / Math::Tan(Math::AngleUnitsToRadians(opts.primaryCamera.getFOVy()));
			// Turn off detail compression at higher FOVs
			A = 1.0f;

			int vertRes = mOptions.primaryCamera.Viewport.ActualHeight;

			T = 2 * (Real)mOptions.maxPixelError / (Real)vertRes;

			return A / T;
		}

		public float GetHeightAt( float x, float z )
		{
			Vector3 start;
			Vector3 end;

			start.x = Vertex( 0, 0, 0 );
			start.y = Vertex( 0, 0, 1 );
			start.z = Vertex( 0, 0, 2 );

			end.x = Vertex( mOptions.tileSize - 1, mOptions.tileSize - 1, 0 );
			end.y = Vertex( mOptions.tileSize - 1, mOptions.tileSize - 1, 1 );
			end.z = Vertex( mOptions.tileSize - 1, mOptions.tileSize - 1, 2 );

			/* Safety catch, if the point asked for is outside
			* of this tile, it will ask the appropriate tile
			*/

			if ( x < start.x )
			{
				if ( mNeighbors[ (int)Neighbor.WEST ] != null )
					return mNeighbors[ (int)Neighbor.WEST ].GetHeightAt( x, z );
				else
					x = start.x;
			}

			if ( x > end.x )
			{
				if ( mNeighbors[ (int)Neighbor.EAST ] != null )
					return mNeighbors[ (int)Neighbor.EAST ].GetHeightAt( x, z );
				else
					x = end.x;
			}

			if ( z < start.z )
			{
				if ( mNeighbors[ (int)Neighbor.NORTH ] != null )
					return mNeighbors[ (int)Neighbor.NORTH ].GetHeightAt( x, z );
				else
					z = start.z;
			}

			if ( z > end.z )
			{
				if ( mNeighbors[ (int)Neighbor.SOUTH ] != null )
					return mNeighbors[ (int)Neighbor.SOUTH ].GetHeightAt( x, z );
				else
					z = end.z;
			}



			float x_pct = ( x - start.x ) / ( end.x - start.x );
			float z_pct = ( z - start.z ) / ( end.z - start.z );

			float x_pt = x_pct * (float)( mOptions.tileSize - 1 );
			float z_pt = z_pct * (float)( mOptions.tileSize - 1 );

			int x_index = (int)x_pt;
			int z_index = (int)z_pt;

			// If we got to the far right / bottom edge, move one back
			if ( x_index == mOptions.tileSize - 1 )
			{
				--x_index;
				x_pct = 1.0f;
			}
			else
			{
				// get remainder
				x_pct = x_pt - x_index;
			}
			if ( z_index == mOptions.tileSize - 1 )
			{
				--z_index;
				z_pct = 1.0f;
			}
			else
			{
				z_pct = z_pt - z_index;
			}

			//bilinear interpolate to find the height.

			float t1 = Vertex( x_index, z_index, 1 );
			float t2 = Vertex( x_index + 1, z_index, 1 );
			float b1 = Vertex( x_index, z_index + 1, 1 );
			float b2 = Vertex( x_index + 1, z_index + 1, 1 );

			float midpoint = ( b1 + t2 ) / 2.0f;

			if ( x_pct + z_pct <= 1 )
			{
				b2 = midpoint + ( midpoint - t1 );
			}
			else
			{
				t1 = midpoint + ( midpoint - b2 );
			}

			float t = ( t1 * ( 1 - x_pct ) ) + ( t2 * ( x_pct ) );
			float b = ( b1 * ( 1 - x_pct ) ) + ( b2 * ( x_pct ) );

			float h = ( t * ( 1 - z_pct ) ) + ( b * ( z_pct ) );

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
			Vector3 dir = end - start;
			Vector3 ray = start;

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

			AxisAlignedBox box = BoundingBox;
			//start with the next one...
			ray += dir;


			while ( !( ( ray.x < box.Minimum.x ) ||
				( ray.x > box.Maximum.x ) ||
				( ray.z < box.Minimum.z ) ||
				( ray.z > box.Maximum.z ) ) )
			{


				float h = GetHeightAt( ray.x, ray.z );

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

			if ( ray.x < box.Minimum.x && mNeighbors[ (int)Neighbor.WEST ] != null )
				return mNeighbors[ (int)Neighbor.WEST ].IntersectSegment( ray, end, ref result );
			else if ( ray.z < box.Minimum.z && mNeighbors[ (int)Neighbor.NORTH ] != null )
				return mNeighbors[ (int)Neighbor.NORTH ].IntersectSegment( ray, end, ref result );
			else if ( ray.x > box.Maximum.x && mNeighbors[ (int)Neighbor.EAST ] != null )
				return mNeighbors[ (int)Neighbor.EAST ].IntersectSegment( ray, end, ref result );
			else if ( ray.z > box.Maximum.z && mNeighbors[ (int)Neighbor.SOUTH ] != null )
				return mNeighbors[ (int)Neighbor.SOUTH ].IntersectSegment( ray, end, ref result );
			else
			{
				//if ( result != 0 )
				result = new Vector3( -1, -1, -1 );

				return false;
			}
		}

		public void GenerateVertexLighting( Vector3 sunlight, ColorEx ambient )
		{

			Vector3 pt = Vector3.Zero;
			Vector3 normal = Vector3.Zero;
			Vector3 light;

			HardwareVertexBuffer vbuf = mTerrain.vertexBufferBinding.GetBuffer( (short)MAIN_BINDING );

			VertexElement elem = mTerrain.vertexDeclaration.FindElementBySemantic( VertexElementSemantic.Diffuse );
			//for each point in the terrain, see if it's in the line of sight for the sun.
			for ( int i = 0; i < mOptions.tileSize; i++ )
			{
				for ( int j = 0; j < mOptions.tileSize; j++ )
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

						ColorEx v = new ColorEx();
						v.r = ambient.r + l;
						v.g = ambient.g + l;
						v.b = ambient.b + l;

						if ( v.r > 1 )
							v.r = 1;

						if ( v.g > 1 )
							v.g = 1;

						if ( v.b > 1 )
							v.b = 1;

						if ( v.r < 0 )
							v.r = 0;

						if ( v.g < 0 )
							v.g = 0;

						if ( v.b < 0 )
							v.b = 0;

						IntPtr colour = new IntPtr( Root.Instance.ConvertColor( v ) );
						//Check: Should be a better way...
						vbuf.WriteData(
							( Index( i, j ) * vbuf.VertexSize ) + elem.Offset,
							sizeof( int ), colour );
					}

					else
					{
						IntPtr colour = new IntPtr( Root.Instance.ConvertColor( ambient ) );

						vbuf.WriteData(
							( Index( i, j ) * vbuf.VertexSize ) + elem.Offset,
							sizeof( int ), colour );
					}

				}

			}
		}

		public override void NotifyCurrentCamera( Camera cam )
		{
			if ( mForcedRenderLevel >= 0 )
			{
				mRenderLevel = mForcedRenderLevel;
				return;
			}


			Vector3 cpos = cam.DerivedPosition;
			AxisAlignedBox aabb = GetWorldBoundingBox( true );
			Vector3 diff = new Vector3( 0, 0, 0 );
			diff.Floor( cpos - aabb.Minimum );
			diff.Ceil( cpos - aabb.Maximum );

			Real L = diff.LengthSquared;

			mRenderLevel = -1;

			for ( int i = 0; i < mOptions.maxGeoMipMapLevel; i++ )
			{
				if ( mMinLevelDistSqr[ i ] > L )
				{
					mRenderLevel = i - 1;
					break;
				}
			}

			if ( mRenderLevel < 0 )
				mRenderLevel = mOptions.maxGeoMipMapLevel - 1;

			if ( mOptions.lodMorph )
			{
				// Get the next LOD level down
				int nextLevel = mNextLevelDown[ mRenderLevel ];
				if ( nextLevel == 0 )
				{
					// No next level, so never morph
					mLODMorphFactor = 0;
				}
				else
				{
					// Set the morph such that the morph happens in the last 0.25 of
					// the distance range
					Real range = mMinLevelDistSqr[ nextLevel ] - mMinLevelDistSqr[ mRenderLevel ];
					if ( range > 0 )
					{
						Real percent = ( L - mMinLevelDistSqr[ mRenderLevel ] ) / range;
						// scale result so that msLODMorphStart == 0, 1 == 1, clamp to 0 below that
						Real rescale = 1.0f / ( 1.0f - mOptions.lodMorphStart );
						mLODMorphFactor = Math.Max( ( percent - mOptions.lodMorphStart ) * rescale, 0.0 );
					}
					else
					{
						// Identical ranges
						mLODMorphFactor = 0.0f;
					}

					//assert(mLODMorphFactor >= 0 && mLODMorphFactor <= 1);
				}

				// Bind the correct delta buffer if it has changed
				// nextLevel - 1 since the first entry is for LOD 1 (since LOD 0 never needs it)
				if ( mLastNextLevel != nextLevel )
				{
					if ( nextLevel > 0 )
					{
						mTerrain.vertexBufferBinding.SetBinding( (short)DELTA_BINDING, mDeltaBuffers[ nextLevel - 1 ] );
					}
					else
					{
						// bind dummy (incase bindings checked)
						mTerrain.vertexBufferBinding.SetBinding( (short)DELTA_BINDING,
							mDeltaBuffers[ 0 ] );
					}
				}
				mLastNextLevel = nextLevel;

			}

		}

		private bool added = false;
		public override void UpdateRenderQueue( RenderQueue queue )
		{
			// Notify need to calculate light list when our sending to render queue
			mLightListDirty = true;

			//if ( !added )
			{
				queue.AddRenderable( this, renderQueueID );
				added = true;
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
			for ( int i = 0; i < 10; i++ )
			{
				if ( ( ( 1 << i ) + 1 ) == n )
					return true;
			}

			return false;
		}

		public unsafe void CalculateNormals()
		{

			Vector3 norm = Vector3.Zero;

			Debug.Assert( mOptions.lit, "No normals present" );

			HardwareVertexBuffer vbuf = mTerrain.vertexBufferBinding.GetBuffer( (short)MAIN_BINDING );
			VertexElement elem = mTerrain.vertexDeclaration.FindElementBySemantic( VertexElementSemantic.Normal );
			char* pBase = (char*)vbuf.Lock( BufferLocking.Discard );
			float* pNorm = null;

			for ( int j = 0; j < mOptions.tileSize; j++ )
			{
				for ( int i = 0; i < mOptions.tileSize; i++ )
				{

					GetNormalAt( Vertex( i, j, 0 ), Vertex( i, j, 2 ), ref norm );

					//  printf( "Normal = %5f,%5f,%5f\n", norm.x, norm.y, norm.z );
					//elem.baseVertexPointerToElement(pBase, &pNorm);
					//Check: Is this right?
					*pNorm = ( *pBase ) + elem.Offset;

					*pNorm++ = norm.x;
					*pNorm++ = norm.y;
					*pNorm++ = norm.z;
					pBase += vbuf.VertexSize;
				}

			}
			vbuf.Unlock();
		}

		unsafe public void CalculateMinLevelDist2( Real C )
		{
			//level 0 has no delta.
			mMinLevelDistSqr[ 0 ] = 0;

			int i, j;

			for ( int level = 1; level < mOptions.maxGeoMipMapLevel; level++ )
			{
				mMinLevelDistSqr[ level ] = 0;

				int step = 1 << level;
				// The step of the next higher LOD
				int higherstep = step >> 1;

				float* pDeltas = null;
				IntPtr dataPtr;
				if ( mOptions.lodMorph )
				{
					// Create a set of delta values (store at index - 1 since 0 has none)
					mDeltaBuffers[ level - 1 ] = CreateDeltaBuffer();
					// Lock, but don't discard (we want the pre-initialised zeros)

					dataPtr = mDeltaBuffers[ level - 1 ].Lock( BufferLocking.Normal );

					pDeltas = (float*)dataPtr.ToPointer();
				}

				for ( j = 0; j < mOptions.tileSize - step; j += step )
				{
					for ( i = 0; i < mOptions.tileSize - step; i += step )
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

						Vector3 v1 = new Vector3( Vertex( i, j, 0 ), Vertex( i, j, 1 ), Vertex( i, j, 2 ) );
						Vector3 v2 = new Vector3( Vertex( i + step, j, 0 ), Vertex( i + step, j, 1 ), Vertex( i + step, j, 2 ) );
						Vector3 v3 = new Vector3( Vertex( i, j + step, 0 ), Vertex( i, j + step, 1 ), Vertex( i, j + step, 2 ) );
						Vector3 v4 = new Vector3( Vertex( i + step, j + step, 0 ), Vertex( i + step, j + step, 1 ), Vertex( i + step, j + step, 2 ) );

						Plane t1, t2;
						t1 = new Plane();
						t2 = new Plane();
						bool backwardTri = false;
						if ( !mOptions.useTriStrips || j % 2 == 0 )
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
						int zubound = ( j == ( mOptions.tileSize - step ) ? step : step - 1 );
						for ( int z = 0; z <= zubound; z++ )
						{
							// include the rightmost col of vertices if this is the last col
							int xubound = ( i == ( mOptions.tileSize - step ) ? step : step - 1 );
							for ( int x = 0; x <= xubound; x++ )
							{
								int fulldetailx = i + x;
								int fulldetailz = j + z;
								if ( fulldetailx % step == 0 &&
									fulldetailz % step == 0 )
								{
									// Skip, this one is a vertex at this level
									continue;
								}

								Real zpct = (Real)z / (Real)step;
								Real xpct = (Real)x / (Real)step;

								//interpolated height
								Vector3 actualPos = new Vector3(
									Vertex( fulldetailx, fulldetailz, 0 ),
									Vertex( fulldetailx, fulldetailz, 1 ),
									Vertex( fulldetailx, fulldetailz, 2 ) );
								Real interp_h;
								// Determine which tri we're on
								if ( ( xpct + zpct <= 1.0f && !backwardTri ) ||
									( xpct + ( 1 - zpct ) <= 1.0f && backwardTri ) )
								{
									// Solve for x/z
									interp_h =
										( -( t1.Normal.x * actualPos.x )
										- t1.Normal.z * actualPos.z
										- t1.D ) / t1.Normal.y;
								}
								else
								{
									// Second tri
									interp_h =
										( -( t2.Normal.x * actualPos.x )
										- t2.Normal.z * actualPos.z
										- t2.D ) / t2.Normal.y;
								}

								Real actual_h = Vertex( fulldetailx, fulldetailz, 1 );
								//Check: not sure about fabs used here...
								Real delta = Math.Abs( interp_h - actual_h );

								Real D2 = delta * delta * C * C;

								if ( mMinLevelDistSqr[ level ] < D2 )
									mMinLevelDistSqr[ level ] = D2;

								// Should be save height difference?
								// Don't morph along edges
								if ( mOptions.lodMorph &&
									fulldetailx != 0 && fulldetailx != ( mOptions.tileSize - 1 ) &&
									fulldetailz != 0 && fulldetailz != ( mOptions.tileSize - 1 ) )
								{
									// Save height difference
									pDeltas[ (int)( fulldetailx + ( fulldetailz * mOptions.tileSize ) ) ] =
										interp_h - actual_h;
								}

							}

						}
					}
				}

				// Unlock morph deltas if required
				if ( mOptions.lodMorph )
				{
					mDeltaBuffers[ level - 1 ].Unlock();
				}
			}



			// Post validate the whole set
			for ( i = 1; i < mOptions.maxGeoMipMapLevel; i++ )
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
				if ( mMinLevelDistSqr[ i ] < mMinLevelDistSqr[ i - 1 ] )
				{
					mMinLevelDistSqr[ i ] = mMinLevelDistSqr[ i - 1 ];
				}
			}

			// Now reverse traverse the list setting the 'next level down'
			Real lastDist = -1;
			int lastIndex = 0;
			for ( i = mOptions.maxGeoMipMapLevel - 1; i >= 0; --i )
			{
				if ( i == mOptions.maxGeoMipMapLevel - 1 )
				{
					// Last one is always 0
					lastIndex = i;
					lastDist = mMinLevelDistSqr[ i ];
					mNextLevelDown[ i ] = 0;
				}
				else
				{
					mNextLevelDown[ i ] = lastIndex;
					if ( mMinLevelDistSqr[ i ] != lastDist )
					{
						lastIndex = i;
						lastDist = mMinLevelDistSqr[ i ];
					}
				}

			}


		}

		public HardwareVertexBuffer CreateDeltaBuffer()
		{
			// Delta buffer is a 1D float buffer of height offsets
            VertexDeclaration decl = HardwareBufferManager.Instance.CreateVertexDeclaration();
            decl.AddElement(0, 0, VertexElementType.Float1, VertexElementSemantic.Position);
			HardwareVertexBuffer buf = HardwareBufferManager.Instance.CreateVertexBuffer( decl, mOptions.tileSize * mOptions.tileSize, BufferUsage.WriteOnly );
			// Fill the buffer with zeros, we will only fill in delta
			IntPtr pVoid = buf.Lock( BufferLocking.Discard );
			Memory.Set( pVoid, 0, ( mOptions.tileSize * mOptions.tileSize ) * sizeof( float ) );
			//memset(pVoid, 0, mOptions.tileSize*mOptions.tileSize*sizeof (float));
			buf.Unlock();

			return buf;
		}

		public IndexData GetIndexData()
		{
			long stitchFlags = 0;

			if ( mNeighbors[ (int)Neighbor.EAST ] != null &&
				mNeighbors[ (int)Neighbor.EAST ].mRenderLevel > mRenderLevel )
			{
				stitchFlags |= STITCH_EAST;
				stitchFlags |=
					( mNeighbors[ (int)Neighbor.EAST ].mRenderLevel - mRenderLevel ) << STITCH_EAST_SHIFT;
			}

			if ( mNeighbors[ (int)Neighbor.WEST ] != null &&
				mNeighbors[ (int)Neighbor.WEST ].mRenderLevel > mRenderLevel )
			{
				stitchFlags |= STITCH_WEST;
				stitchFlags |=
					( mNeighbors[ (int)Neighbor.WEST ].mRenderLevel - mRenderLevel ) << STITCH_WEST_SHIFT;
			}

			if ( mNeighbors[ (int)Neighbor.NORTH ] != null &&
				mNeighbors[ (int)Neighbor.NORTH ].mRenderLevel > mRenderLevel )
			{
				stitchFlags |= STITCH_NORTH;
				stitchFlags |=
					( mNeighbors[ (int)Neighbor.NORTH ].mRenderLevel - mRenderLevel ) << STITCH_NORTH_SHIFT;
			}

			if ( mNeighbors[ (int)Neighbor.SOUTH ] != null &&
				mNeighbors[ (int)Neighbor.SOUTH ].mRenderLevel > mRenderLevel )
			{
				stitchFlags |= STITCH_SOUTH;
				stitchFlags |=
					( mNeighbors[ (int)Neighbor.SOUTH ].mRenderLevel - mRenderLevel ) << STITCH_SOUTH_SHIFT;
			}

			// Check preexisting
			Hashtable levelIndex = mTerrainZone.LevelIndex;
			//IndexMap::iterator ii = levelIndex[ mRenderLevel ].find( stitchFlags );
			IndexData indexData;

			if ( null == levelIndex[ mRenderLevel ] || ( ( (KeyValuePair<uint, IndexData>)levelIndex[ mRenderLevel ] ).Key & stitchFlags ) == 0 )
			{
				// Create
				if ( mOptions.useTriStrips )
				{
					indexData = GenerateTriStripIndexes( (uint)stitchFlags );
				}
				else
				{
					indexData = GenerateTriListIndexes( (uint)stitchFlags );
				}
				levelIndex[ mRenderLevel ] = new KeyValuePair<uint, IndexData>( (uint)stitchFlags, indexData );
			}
			else
			{
				indexData = ( (KeyValuePair<uint, IndexData>)levelIndex[ mRenderLevel ] ).Value;
			}


			return indexData;
		}

		public IndexData GenerateTriStripIndexes( uint stitchFlags )
		{
			// The step used for the current level
			int step = 1 << mRenderLevel;
			// The step used for the lower level
			int lowstep = 1 << ( mRenderLevel + 1 );

			int numIndexes = 0;

			// Calculate the number of indexes required
			// This is the number of 'cells' at this detail level x 2
			// plus 3 degenerates to turn corners
			int numTrisAcross = ( ( ( mOptions.tileSize - 1 ) / step ) * 2 ) + 3;
			// Num indexes is number of tris + 2
			int new_length = numTrisAcross * ( ( mOptions.tileSize - 1 ) / step ) + 2;
			//this is the maximum for a level.  It wastes a little, but shouldn't be a problem.

			IndexData indexData = new IndexData();
			indexData.indexBuffer =
				HardwareBufferManager.Instance.CreateIndexBuffer(
				IndexType.Size16,
				new_length, BufferUsage.StaticWriteOnly );//, false);

			mTerrainZone.IndexCache.mCache.Add( indexData );
			unsafe
			{


				ushort* pIdx = (ushort*)indexData.indexBuffer.Lock( 0, indexData.indexBuffer.Size, BufferLocking.Discard );

				// Stripified mesh
				for ( int j = 0; j < mOptions.tileSize - 1; j += step )
				{
					int i;
					// Forward strip
					// We just do the |/ here, final | done after
					for ( i = 0; i < mOptions.tileSize - 1; i += step )
					{
						int[] x = new int[ 4 ];
						int[] y = new int[ 4 ];
						x[ 0 ] = x[ 1 ] = i;
						x[ 2 ] = x[ 3 ] = i + step;
						y[ 0 ] = y[ 2 ] = j;
						y[ 1 ] = y[ 3 ] = j + step;

						if ( j == 0 && ( stitchFlags & STITCH_NORTH ) != 0 )
						{
							// North reduction means rounding x[0] and x[2]
							if ( x[ 0 ] % lowstep != 0 )
							{
								// Since we know we only drop down one level of LOD,
								// removing 1 step of higher LOD should return to lower
								x[ 0 ] -= step;
							}
							if ( x[ 2 ] % lowstep != 0 )
							{
								x[ 2 ] -= step;
							}
						}

						// Never get a south tiling on a forward strip (always finish on
						// a backward strip)

						if ( i == 0 && ( stitchFlags & STITCH_WEST ) != 0 )
						{
							// West reduction means rounding y[0] / y[1]
							if ( y[ 0 ] % lowstep != 0 )
							{
								y[ 0 ] -= step;
							}
							if ( y[ 1 ] % lowstep != 0 )
							{
								y[ 1 ] -= step;
							}
						}
						if ( i == ( mOptions.tileSize - 1 - step ) && ( stitchFlags & STITCH_EAST ) != 0 )
						{
							// East tiling means rounding y[2] & y[3]
							if ( y[ 2 ] % lowstep != 0 )
							{
								y[ 2 ] -= step;
							}
							if ( y[ 3 ] % lowstep != 0 )
							{
								y[ 3 ] -= step;
							}
						}

						//triangles
						if ( i == 0 )
						{
							// Starter
							*pIdx++ = (ushort)Index( x[ 0 ], y[ 0 ] );
							numIndexes++;
						}
						*pIdx++ = (ushort)Index( x[ 1 ], y[ 1 ] );
						numIndexes++;
						*pIdx++ = (ushort)Index( x[ 2 ], y[ 2 ] );
						numIndexes++;

						if ( i == mOptions.tileSize - 1 - step )
						{
							// Emit extra index to finish row
							*pIdx++ = (ushort)Index( x[ 3 ], y[ 3 ] );
							numIndexes++;
							if ( j < mOptions.tileSize - 1 - step )
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
					for ( i = mOptions.tileSize - 1; i > 0; i -= step )
					{
						int[] x = new int[ 4 ];
						int[] y = new int[ 4 ];
						x[ 0 ] = x[ 1 ] = i;
						x[ 2 ] = x[ 3 ] = i - step;
						y[ 0 ] = y[ 2 ] = j;
						y[ 1 ] = y[ 3 ] = j + step;

						// Never get a north tiling on a backward strip (always
						// start on a forward strip)
						if ( j == ( mOptions.tileSize - 1 - step ) && ( stitchFlags & STITCH_SOUTH ) != 0 )
						{
							// South reduction means rounding x[1] / x[3]
							if ( x[ 1 ] % lowstep != 0 )
							{
								x[ 1 ] -= step;
							}
							if ( x[ 3 ] % lowstep != 0 )
							{
								x[ 3 ] -= step;
							}
						}

						if ( i == step && ( stitchFlags & STITCH_WEST ) != 0 )
						{
							// West tiling on backward strip is rounding of y[2] / y[3]
							if ( y[ 2 ] % lowstep != 0 )
							{
								y[ 2 ] -= step;
							}
							if ( y[ 3 ] % lowstep != 0 )
							{
								y[ 3 ] -= step;
							}
						}
						if ( i == mOptions.tileSize - 1 && ( stitchFlags & STITCH_EAST ) != 0 )
						{
							// East tiling means rounding y[0] and y[1] on backward strip
							if ( y[ 0 ] % lowstep != 0 )
							{
								y[ 0 ] -= step;
							}
							if ( y[ 1 ] % lowstep != 0 )
							{
								y[ 1 ] -= step;
							}
						}

						//triangles
						if ( i == mOptions.tileSize )
						{
							// Starter
							*pIdx++ = (ushort)Index( x[ 0 ], y[ 0 ] );
							numIndexes++;
						}
						*pIdx++ = (ushort)Index( x[ 1 ], y[ 1 ] );
						numIndexes++;
						*pIdx++ = (ushort)Index( x[ 2 ], y[ 2 ] );
						numIndexes++;

						if ( i == step )
						{
							// Emit extra index to finish row
							*pIdx++ = (ushort)Index( x[ 3 ], y[ 3 ] );
							numIndexes++;
							if ( j < mOptions.tileSize - 1 - step )
							{
								// Emit this index once more (this is to turn around)
								*pIdx++ = (ushort)Index( x[ 3 ], y[ 3 ] );
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

		public unsafe IndexData GenerateTriListIndexes( uint stitchFlags )
		{

			int numIndexes = 0;
			int step = 1 << mRenderLevel;

			IndexData indexData;

			int north = ( stitchFlags & STITCH_NORTH ) != 0 ? step : 0;
			int south = ( stitchFlags & STITCH_SOUTH ) != 0 ? step : 0;
			int east = ( stitchFlags & STITCH_EAST ) != 0 ? step : 0;
			int west = ( stitchFlags & STITCH_WEST ) != 0 ? step : 0;

			int new_length = ( mOptions.tileSize / step ) * ( mOptions.tileSize / step ) * 2 * 2 * 2;
			//this is the maximum for a level.  It wastes a little, but shouldn't be a problem.

			indexData = new IndexData();
			indexData.indexBuffer = HardwareBufferManager.Instance.CreateIndexBuffer(
				IndexType.Size16,
				new_length, BufferUsage.StaticWriteOnly );//, false);

			mTerrainZone.IndexCache.mCache.Add( indexData );

			ushort* pIdx = (ushort*)indexData.indexBuffer.Lock( 0,
				indexData.indexBuffer.Size,
				BufferLocking.Discard );

			// Do the core vertices, minus stitches
			for ( int j = north; j < mOptions.tileSize - 1 - south; j += step )
			{
				for ( int i = west; i < mOptions.tileSize - 1 - east; i += step )
				{
					//triangles
					*pIdx++ = Index( i, j + step );
					numIndexes++; // original order: 2
					*pIdx++ = Index( i + step, j );
					numIndexes++; // original order: 3
					*pIdx++ = Index( i, j );
					numIndexes++; // original order: 1

					*pIdx++ = Index( i + step, j + step );
					numIndexes++; // original order: 2
					*pIdx++ = Index( i + step, j );
					numIndexes++; // original order: 3
					*pIdx++ = Index( i, j + step );
					numIndexes++; // original order: 1
				}
			}

			// North stitching
			if ( north > 0 )
			{
				numIndexes += StitchEdge( Neighbor.NORTH, mRenderLevel, mNeighbors[ (int)Neighbor.NORTH ].mRenderLevel,
					west > 0, east > 0, &pIdx );
			}
			// East stitching
			if ( east > 0 )
			{
				numIndexes += StitchEdge( Neighbor.EAST, mRenderLevel, mNeighbors[ (int)Neighbor.EAST ].mRenderLevel,
					north > 0, south > 0, &pIdx );
			}
			// South stitching
			if ( south > 0 )
			{
				numIndexes += StitchEdge( Neighbor.SOUTH, mRenderLevel, mNeighbors[ (int)Neighbor.SOUTH ].mRenderLevel,
					east > 0, west > 0, &pIdx );
			}
			// West stitching
			if ( west > 0 )
			{
				numIndexes += StitchEdge( Neighbor.WEST, mRenderLevel, mNeighbors[ (int)Neighbor.WEST ].mRenderLevel,
					south > 0, north > 0, &pIdx );
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
		public new void UpdateCustomGpuParameter( GpuProgramParameters.AutoConstantEntry constantEntry, GpuProgramParameters param )
		{
			if ( constantEntry.Data == MORPH_CUSTOM_PARAM_ID )
			{
				// Update morph LOD factor
				param.SetConstant( constantEntry.PhysicalIndex, mLODMorphFactor );
				//_writeRawConstant(constantEntry.PhysicalIndex, mLODMorphFactor);
			}
			else
			{
				base.UpdateCustomGpuParameter( constantEntry, param );
			}

		}

		public unsafe int StitchEdge( Neighbor neighbor, int hiLOD, int loLOD, bool omitFirstTri, bool omitLastTri, ushort** ppIdx )
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
			ushort* pIdx = *ppIdx;

			// Work out the steps ie how to increment indexes
			// Step from one vertex to another in the high detail version
			int step = 1 << hiLOD;
			// Step from one vertex to another in the low detail version
			int superstep = 1 << loLOD;
			// Step half way between low detail steps
			int halfsuperstep = superstep >> 1;

			// Work out the starting points and sign of increments
			// We always work the strip clockwise
			int startx, starty, endx, rowstep;
			startx = starty = endx = rowstep = 0;
			bool horizontal = false;
			switch ( neighbor )
			{
				case Neighbor.NORTH:
					startx = starty = 0;
					endx = mOptions.tileSize - 1;
					rowstep = step;
					horizontal = true;
					break;
				case Neighbor.SOUTH:
					// invert x AND y direction, helps to keep same winding
					startx = starty = mOptions.tileSize - 1;
					endx = 0;
					rowstep = -step;
					step = -step;
					superstep = -superstep;
					halfsuperstep = -halfsuperstep;
					horizontal = true;
					break;
				case Neighbor.EAST:
					startx = 0;
					endx = mOptions.tileSize - 1;
					starty = mOptions.tileSize - 1;
					rowstep = -step;
					horizontal = false;
					break;
				case Neighbor.WEST:
					startx = mOptions.tileSize - 1;
					endx = 0;
					starty = 0;
					rowstep = step;
					step = -step;
					superstep = -superstep;
					halfsuperstep = -halfsuperstep;
					horizontal = false;
					break;
			};

			int numIndexes = 0;

			for ( int j = startx; j != endx; j += superstep )
			{
				int k;
				for ( k = 0; k != halfsuperstep; k += step )
				{
					int jk = j + k;
					//skip the first bit of the corner?
					if ( j != startx || k != 0 || !omitFirstTri )
					{
						if ( horizontal )
						{
							*pIdx++ = Index( jk, starty + rowstep );
							numIndexes++; // original order: 2
							*pIdx++ = Index( jk + step, starty + rowstep );
							numIndexes++; // original order: 3
							*pIdx++ = Index( j, starty );
							numIndexes++; // original order: 1
						}
						else
						{
							*pIdx++ = Index( starty + rowstep, jk );
							numIndexes++; // original order: 2
							*pIdx++ = Index( starty + rowstep, jk + step );
							numIndexes++; // original order: 3
							*pIdx++ = Index( starty, j );
							numIndexes++; // original order: 1
						}
					}
				}

				// Middle tri
				if ( horizontal )
				{
					*pIdx++ = Index( j + halfsuperstep, starty + rowstep );
					numIndexes++; // original order: 2
					*pIdx++ = Index( j + superstep, starty );
					numIndexes++; // original order: 3
					*pIdx++ = Index( j, starty );
					numIndexes++; // original order: 1
				}
				else
				{
					*pIdx++ = Index( starty + rowstep, j + halfsuperstep );
					numIndexes++; // original order: 2
					*pIdx++ = Index( starty, j + superstep );
					numIndexes++; // original order: 3
					*pIdx++ = Index( starty, j );
					numIndexes++; // original order: 1
				}

				for ( k = halfsuperstep; k != superstep; k += step )
				{
					int jk = j + k;
					if ( j != endx - superstep || k != superstep - step || !omitLastTri )
					{
						if ( horizontal )
						{
							*pIdx++ = Index( jk, starty + rowstep );
							numIndexes++; // original order: 2
							*pIdx++ = Index( jk + step, starty + rowstep );
							numIndexes++; // original order: 3
							*pIdx++ = Index( j + superstep, starty );
							numIndexes++; // original order: 1
						}
						else
						{
							*pIdx++ = Index( starty + rowstep, jk );
							numIndexes++; // original order: 2
							*pIdx++ = Index( starty + rowstep, jk + step );
							numIndexes++; // original order: 3
							*pIdx++ = Index( starty, j + superstep );
							numIndexes++; // original order: 1
						}
					}
				}
			}

			*ppIdx = pIdx;

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
				if ( mLightListDirty )
				{
					ParentSceneNode.Creator.PopulateLightList(
						mCenter, this.BoundingRadius, mLightList );
					mLightListDirty = false;
				}
				return mLightList;
			}

		}

		/// <summary>
		///    Returns whether or not this Renderable wishes the hardware to normalize normals.
		/// </summary>
		public new bool NormalizeNormals
		{
			get
			{
				return normalizeNormals;
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
				return numWorldTransforms;
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
				return useIdentityProjection;
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
				return useIdentityView;
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
				return polygonModeOverrideable;
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
				return worldOrientation;
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
				return worldPosition;
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
		public override float GetSquaredViewDepth( Camera camera )
		{
			Vector3 diff = mCenter - camera.DerivedPosition;
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
			mCache.Clear();
		}

		~TerrainBufferCache()
		{
			shutdown();
		}

		internal List<IndexData> mCache = new List<IndexData>();
	};

}