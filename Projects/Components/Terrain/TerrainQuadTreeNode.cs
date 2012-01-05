#region MIT/X11 License

//Copyright © 2003-2011 Axiom 3D Rendering Engine Project
//
//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//THE SOFTWARE.

#endregion License

#region Namespace Declarations

using System;
using System.Diagnostics;
using System.Collections.Generic;

using Axiom.Core;
using Axiom.Graphics;
using Axiom.Math;
using Axiom.Collections;
using Axiom.Utilities;

#endregion Namespace Declarations

namespace Axiom.Components.Terrain
{
	/// <summary>
	/// 
	/// </summary>
	public struct LodLevel
	{
		/// <summary>
		/// Number of vertices rendered down one side (not including skirts)
		/// </summary>
		public ushort BatchSize;

		/// <summary>
		/// index data referencing the main vertex data but in CPU buffers (built in background)
		/// </summary>
		public IndexData CpuIndexData;

		/// <summary>
		/// "Real" index data on the gpu
		/// </summary>
		public IndexData GpuIndexData;

		/// <summary>
		/// Maximum delta height between this and the next lower lod
		/// </summary>
		public float MaxHeightDelta;

		/// <summary>
		/// Temp calc area for max height delta
		/// </summary>
		public float CalcMaxHeightDelta;

		/// <summary>
		/// The most recently calculated transition distance
		/// </summary>
		public float LastTranistionDist;

		/// <summary>
		/// The cFactor value used to calculate transitionDist
		/// </summary>
		public float LastCFactor;
	}

	/// <summary>
	/// 
	/// </summary>
	public class VertexDataRecord
	{
		/// <summary>
		/// 
		/// </summary>
		public VertexData CpuVertexData;

		/// <summary>
		/// 
		/// </summary>
		public VertexData GpuVertexData;

		/// <summary>
		/// resolution of the data compared to the base terrain data (NOT number of vertices!)
		/// </summary>
		public ushort Resolution;

		/// <summary>
		/// size of the data along one edge
		/// </summary>
		public ushort Size;

		/// <summary>
		/// Number of quadtree levels (including this one) this data applies to
		/// </summary>
		public ushort TreeLevels;

		/// <summary>
		/// Number of rows and columns of skirts
		/// </summary>
		public ushort NumSkirtRowsCols;

		/// <summary>
		/// The number of rows / cols to skip in between skirts
		/// </summary>
		public ushort SkirtRowColSkip;

		/// <summary>
		/// Is the GPU vertex data out of date?
		/// </summary>
		public bool IsGpuVertexDataDirty;

		public VertexDataRecord() {}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="res"></param>
		/// <param name="sz"></param>
		/// <param name="lvls"></param>
		public VertexDataRecord( ushort res, ushort sz, ushort lvls )
		{
			CpuVertexData = null;
			GpuVertexData = null;
			Resolution = res;
			Size = sz;
			TreeLevels = lvls;
			IsGpuVertexDataDirty = false;
			NumSkirtRowsCols = 0;
			SkirtRowColSkip = 0;
		}
	}

	/// <summary>
	/// A node in a quad tree used to store a patch of terrain.
	/// </summary>
	/// <remarks>
	/// <b>Algorithm overview:</b>
	/// Our goal is to perform traditional chunked LOD with geomorphing. But, 
	///	instead of just dividing the terrain into tiles, we will divide them into
	///	a hierarchy of tiles, a quadtree, where any level of the quadtree can 
	///	be a rendered tile (to the exclusion of its children). The idea is to 
	///	collect together children into a larger batch with their siblings as LOD 
	///	decreases, to improve performance.
	///	
	/// The minBatchSize and maxBatchSize parameters on Terrain a key to 
	///	defining this behaviour. Both values are expressed in vertices down one axis.
	///	maxBatchSize determines the number of tiles on one side of the terrain,
	///	which is numTiles = (terrainSize-1) / (maxBatchSize-1). This in turn determines the depth
	///	of the quad tree, which is sqrt(numTiles). The minBatchSize determines
	///	the 'floor' of how low the number of vertices can go in a tile before it
	///	has to be grouped together with its siblings to drop any lower. We also do not group 
	///	a tile with its siblings unless all of them are at this minimum batch size, 
	///	rather than trying to group them when they all end up on the same 'middle' LOD;
	///	this is for several reasons; firstly, tiles hitting the same 'middle' LOD is
	///	less likely and more transient if they have different levels of 'roughness',
	///	and secondly since we're sharing a vertex / index pool between all tiles, 
	///	only grouping at the min level means that the number of combinations of 
	///	buffer sizes for any one tile is greatly simplified, making it easier to 
	///	pool data. To be more specific, any tile / quadtree node can only have
	///	log2(maxBatchSize-1) - log2(minBatchSize-1) + 1 LOD levels (and if you set them 
	///	to the same value, LOD can only change by going up/down the quadtree).
	///	The numbers of vertices / indices in each of these levels is constant for
	///	the same (relative) LOD index no matter where you are in the tree, therefore
	///	buffers can potentially be reused more easily.
	/// </remarks>
	public class TerrainQuadTreeNode : IDisposable
	{
		protected TerrainRendable mRend;

		/// <summary>
		/// Buffer binding used for holding positions.
		/// </summary>
		public static ushort POSITION_BUFFER = 0;

		/// <summary>
		/// Buffer binding used for holding delta values
		/// </summary>
		public static ushort DELTA_BUFFER = 1;

		/// <summary>
		/// 
		/// </summary>
		protected List<LodLevel> mLodLevels = new List<LodLevel>();

		/// <summary>
		/// 
		/// </summary>
		protected Terrain mTerrain;

		/// <summary>
		/// 
		/// </summary>
		protected TerrainQuadTreeNode mParent;

		/// <summary>
		/// 
		/// </summary>
		protected TerrainQuadTreeNode[] mChildren = new TerrainQuadTreeNode[4];

		/// <summary>
		/// 
		/// </summary>
		protected ushort mOffsetX;

		/// <summary>
		/// 
		/// </summary>
		protected ushort mOffsetY;

		/// <summary>
		/// 
		/// </summary>
		protected ushort mBoundaryX;

		/// <summary>
		/// 
		/// </summary>
		protected ushort mBoundaryY;

		/// <summary>
		/// 
		/// </summary>
		protected ushort mSize;

		/// <summary>
		/// 
		/// </summary>
		protected ushort mBaseLod;

		/// <summary>
		/// 
		/// </summary>
		protected ushort mDepth;

		/// <summary>
		/// 
		/// </summary>
		protected ushort mQuadrant;

		/// <summary>
		/// relative to terrain centre
		/// </summary>
		protected Vector3 mLocalCentre;

		/// <summary>
		/// relative to mLocalCentre
		/// </summary>
		protected AxisAlignedBox mAABB;

		/// <summary>
		/// relative to mLocalCentre
		/// </summary>
		protected float mBoundingRadius;

		/// <summary>
		/// -1 = none (do not render)
		/// </summary>
		protected int mCurrentLod;

		/// <summary>
		/// // 0-1 transition to lower LOD
		/// </summary>
		protected float mLodTransition;

		/// <summary>
		/// The child with the largest height delta 
		/// </summary>
		protected TerrainQuadTreeNode mChildWithMaxHeightDelta;

		/// <summary>
		/// 
		/// </summary>
		protected bool mSelfOrChildRendered;

		/// <summary>
		/// 
		/// </summary>
		protected ushort mMaterialLodIndex;

		/// <summary>
		/// 
		/// </summary>
		protected TerrainQuadTreeNode mNodeWithVertexData;

		/// <summary>
		/// 
		/// </summary>
		protected VertexDataRecord mVertexDataRecord;

		/// <summary>
		/// 
		/// </summary>
		//    protected Movable mMovable;
		/// <summary>
		/// 
		/// </summary>
		//    protected Rend mRend;
		public VertexDataRecord VertextDataRecord { get { return ( mNodeWithVertexData != null ) ? mNodeWithVertexData.mVertexDataRecord : null; } }

		/// <summary>
		/// Get the horizontal offset into the main terrain data of this node
		/// </summary>
		public int XOffeset { get { return mOffsetX; } }

		/// <summary>
		/// Get the vertical offset into the main terrain data of this node
		/// </summary>
		public int YOffset { get { return mOffsetY; } }

		/// <summary>
		/// Get the base LOD level this node starts at (the highest LOD it handles)
		/// </summary>
		public int BaseLod { get { return mBaseLod; } }

		/// <summary>
		/// Get the number of LOD levels this node can represent itself (only > 1 for leaf nodes)
		/// </summary>
		public ushort LodCount { get { return (ushort)mLodLevels.Count; } }

		/// <summary>
		/// Get ultimate parent terrain
		/// </summary>
		public Terrain Terrain { get { return mTerrain; } }

		/// <summary>
		/// Get parent node
		/// </summary>
		public TerrainQuadTreeNode Parent { get { return mParent; } }

		/// <summary>
		/// Is this a leaf node (no children)
		/// </summary>
		public bool IsLeaf { get { return mChildren[ 0 ] == null; } }

		/// <summary>
		/// Get the AABB (local coords) of this node
		/// </summary>
		public AxisAlignedBox AABB { get { return mAABB; } }

		/// <summary>
		/// Get the bounding radius of this node
		/// </summary>
		public float BoundingRadius { get { return mBoundingRadius; } }

		/// <summary>
		/// Get the minimum height of the node
		/// </summary>
		public float MinHeight
		{
			get
			{
				switch( mTerrain.Alignment )
				{
					case Alignment.Align_X_Y:
					default:
						return mAABB.Minimum.z;
					case Alignment.Align_X_Z:
						return mAABB.Minimum.y;
					case Alignment.Align_Y_Z:
						return mAABB.Minimum.x;
				}
			}
		}

		/// <summary>
		/// Get the maximum height of the node
		/// </summary>
		public float MaxHeight
		{
			get
			{
				switch( mTerrain.Alignment )
				{
					case Alignment.Align_X_Y:
					default:
						return mAABB.Maximum.z;
					case Alignment.Align_X_Z:
						return mAABB.Maximum.y;
					case Alignment.Align_Y_Z:
						return mAABB.Maximum.x;
				}
			}
		}

		/// <summary>
		/// Get the current LOD index (only valid after calculateCurrentLod)
		/// </summary>
		public int CurentLod
		{
			get { return mCurrentLod; }
			set
			{
				mCurrentLod = value;
				mRend.SetCustomParameter( Terrain.LOD_MORPH_CUSTOM_PARAM,
				                          new Vector4( mLodTransition, mCurrentLod + mBaseLod + 1, 0, 0 ) );
			}
		}

		/// <summary>
		/// Returns whether this node is rendering itself at the current LOD level
		/// </summary>
		public bool IsRenderedAtCurrentLod { get { return mCurrentLod != -1; } }

		/// <summary>
		/// Returns whether this node or its children are being rendered at the current LOD level
		/// </summary>
		public bool IsSelfOrChildrenRenderedAtCurrentLod { get { return mSelfOrChildRendered; } }

		/// <summary>
		/// Get the transition state between the current LOD and the next lower one (only valid after calculateCurrentLod)
		/// </summary>
		public float LodTransition
		{
			get { return mLodTransition; }
			set
			{
				mLodTransition = value;
				mRend.SetCustomParameter( Terrain.LOD_MORPH_CUSTOM_PARAM,
				                          new Vector4( mLodTransition, mCurrentLod + mBaseLod + 1, 0, 0 ) );
			}
		}

		public Vector3 LocalCentre { get { return mLocalCentre; } }

		/// <summary>
		/// 
		/// </summary>
		public Material Material { get { return mTerrain.Material; } }

		/// <summary>
		/// 
		/// </summary>
		public Technique Technique { get { return mTerrain.Material.GetBestTechnique( mMaterialLodIndex, mRend ); } }

		/// <summary>
		/// Default constructor.
		/// </summary>
		/// <param name="terrain">The ultimate parent terrain</param>
		/// <param name="parent">ptional parent node (in which case xoff, yoff are 0 and size must be entire terrain)</param>
		/// <param name="xOff">Offsets from the start of the terrain data in 2D</param>
		/// <param name="yOff">Offsets from the start of the terrain data in 2D</param>
		/// <param name="size">The size of the node in vertices at the highest LOD</param>
		/// <param name="lod">The base LOD level</param>
		/// <param name="depth">The depth that this node is at in the tree (or convenience)</param>
		/// <param name="quadrant">The index of the quadrant (0, 1, 2, 3)</param>
		public TerrainQuadTreeNode( Terrain terrain, TerrainQuadTreeNode parent, ushort xOff, ushort yOff,
		                            ushort size, ushort lod, ushort depth, ushort quadrant )
		{
			mTerrain = terrain;
			mParent = parent;
			mOffsetX = xOff;
			mOffsetY = yOff;
			mBoundaryX = (ushort)( xOff + size );
			mBoundaryY = (ushort)( yOff + size );
			mSize = size;
			mBaseLod = lod;
			mDepth = depth;
			mQuadrant = quadrant;
			mBoundingRadius = 0;
			mCurrentLod = -1;
			mMaterialLodIndex = 0;
			mLodTransition = 0;
			mChildWithMaxHeightDelta = null;
			mSelfOrChildRendered = false;
			mNodeWithVertexData = null;
			// mMovable = null;
			mRend = null;
			mAABB = new AxisAlignedBox();
			if( mTerrain.MaxBatchSize < size )
			{
				ushort childSize = (ushort)( ( ( size - 1 ) * 0.5f ) + 1 );
				ushort childOff = (ushort)( childSize - 1 );
				ushort childLod = (ushort)( lod - 1 );
				ushort childDepth = (ushort)( depth + 1 );

				mChildren[ 0 ] = new TerrainQuadTreeNode( mTerrain, this, xOff, yOff, childSize, childLod, childDepth, 0 );
				mChildren[ 1 ] = new TerrainQuadTreeNode( mTerrain, this, (ushort)( xOff + childOff ), yOff, childSize, childLod, childDepth, 1 );
				mChildren[ 2 ] = new TerrainQuadTreeNode( mTerrain, this, xOff, (ushort)( yOff + childOff ), childSize, childLod, childDepth, 2 );
				mChildren[ 3 ] = new TerrainQuadTreeNode( mTerrain, this, (ushort)( xOff + childOff ), (ushort)( yOff + childOff ), childSize, childLod, childDepth, 3 );

				LodLevel ll = new LodLevel();
				// non-leaf nodes always render with minBatchSize vertices
				ll.BatchSize = mTerrain.MinBatchSize;
				ll.MaxHeightDelta = 0;
				ll.CalcMaxHeightDelta = 0;
				mLodLevels.Add( ll );
			}
			else
			{
				//no children
				Array.Clear( mChildren, 0, mChildren.Length );
				// this is a leaf node and may have internal LODs of its own
				ushort ownLod = mTerrain.LodLevelsPerLeafCount;

				Debug.Assert( lod == ( ownLod - 1 ),
				              "The lod passed in should reflect the number of lods in a leaf" );
				// leaf nodes always have a base LOD of 0, because they're always handling
				// the highest level of detail
				mBaseLod = 0;
				ushort sz = mTerrain.MaxBatchSize;

				while( ownLod-- != 0 )
				{
					LodLevel ll = new LodLevel();
					ll.BatchSize = sz;
					ll.MaxHeightDelta = 0;
					ll.CalcMaxHeightDelta = 0;
					mLodLevels.Add( ll );
					if( ownLod != 0 )
					{
						sz = (ushort)( ( ( sz - 1 ) * 0.5 ) + 1 );
					}
				}
				Debug.Assert( sz == mTerrain.MinBatchSize );
			}

			// local centre calculation
			// because of pow2 +1 there is always a middle point
			ushort midoffset = (ushort)( ( size - 1 ) / 2 );
			ushort midpointX = (ushort)( mOffsetX + midoffset );
			ushort midpointY = (ushort)( mOffsetY + midoffset );

			//derive the local centry, but give it a height if 0
			//TODO: - what if we actually centred this at the terrain height at this point?
			//would this be better?
			mTerrain.GetPoint( midpointX, midpointY, 0, ref mLocalCentre );
			/*mRend = new Rend(this);
            mMovable = new Movable(this,mRend);*/
			mRend = new TerrainRendable( this );
			SceneNode sn = mTerrain.RootSceneNode.CreateChildSceneNode( mLocalCentre );
			sn.AttachObject( mRend );
			// sn.AttachObject(mRend);
		}

		/// <summary>
		/// 
		/// </summary>
		public void Dispose()
		{
			/* if (mMovable != null)
            {
                mMovable.Dispose();
                mMovable = null;
            }
            if (mRend != null)
            {
                mRend.Dispose();
                mRend = null;
            }*/
			for( int i = 0; i < mChildren.Length; i++ )
			{
				if( mChildren[ i ] != null )
				{
					mChildren[ i ].Dispose();
				}
			}

			DestroyCpuVertexData();
			DestroyCpuIndexData();
			DestroyGpuVertexData();
			DestroyGpuIndexData();

			if( mLodLevels != null )
			{
				mLodLevels.Clear();
				mLodLevels = null;
			}
		}

		/// <summary>
		/// Get child node
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public TerrainQuadTreeNode GetChild( ushort child )
		{
			if( IsLeaf || child >= 4 )
			{
				return null;
			}

			return mChildren[ child ];
		}

		/// <summary>
		/// Prepare node and children (perform CPU tasks, may be background thread)
		/// </summary>
		public void Prepare()
		{
			if( IsLeaf )
			{
				return;
			}

			for( int i = 0; i < mChildren.Length; i++ )
			{
				mChildren[ i ].Prepare();
			}
		}

		/// <summary>
		///  Load node and children (perform GPU tasks, will be render thread)
		/// </summary>
		public void Load()
		{
			CreateGpuVertexData();
			CreateGpuIndexData();

			if( !IsLeaf )
			{
				for( int i = 0; i < 4; i++ )
				{
					mChildren[ i ].Load();
				}
			}
		}

		/// <summary>
		/// Unload node and children (perform GPU tasks, will be render thread)
		/// </summary>
		public void Unload()
		{
			if( !IsLeaf )
			{
				for( int i = 0; i < 4; i++ )
				{
					mChildren[ i ].Unload();
				}
			}

			DestroyGpuVertexData();
		}

		/// <summary>
		/// Unprepare node and children (perform CPU tasks, may be background thread)
		/// </summary>
		public void Unprepare()
		{
			if( !IsLeaf )
			{
				for( int i = 0; i < 4; i++ )
				{
					mChildren[ i ].Unprepare();
				}
			}

			DestroyCpuVertexData();
		}

		/// <summary>
		/// Get the LodLevel information for a given lod.
		/// </summary>
		/// <param name="lod">
		/// The lod level index relative to this classes own list; if you
		///	want to use a global lod level, subtract getBaseLod() first. Higher
		///	LOD levels are lower detail.
		/// </param>
		/// <returns></returns>
		public LodLevel GetLodLevel( ushort lod )
		{
			Debug.Assert( lod < mLodLevels.Count );

			return mLodLevels[ lod ];
		}

		/// <summary>
		///  Notify the node (and children) that deltas are going to be calculated for a given range.
		/// </summary>
		/// <param name="rect"></param>
		public void PreDeltaCalculation( Rectangle rect )
		{
			if( rect.Left <= mBoundaryX || rect.Right > mOffsetX
			    || rect.Top <= mBoundaryY || rect.Bottom > mOffsetY )
			{
				// relevant to this node (overlaps)

				// if the rect covers the whole node, reset the max height
				// this means that if you recalculate the deltas progressively, end up keeping
				// a max height that's no longer the case (ie more conservative lod), 
				// but that's the price for not recaculating the whole node. If a 
				// complete recalculation is required, just dirty the entire node. (or terrain)

				// Note we use the 'calc' field here to avoid interfering with any
				// ongoing LOD calculations (this can be in the background)

				if( rect.Left <= mOffsetX && rect.Right > mBoundaryX
				    && rect.Top <= mOffsetY && rect.Bottom > mBoundaryY )
				{
					for( int i = 0; i < mLodLevels.Count; i++ )
					{
						LodLevel tmp = mLodLevels[ i ];
						tmp.CalcMaxHeightDelta = 0.0f;
					}
				}

				//pass on to children
				if( !IsLeaf )
				{
					for( int i = 0; i < 4; i++ )
					{
						mChildren[ i ].PreDeltaCalculation( rect );
					}
				}
			}
		}

		/// <summary>
		/// Notify the node (and children) of a height delta value.
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="lod"></param>
		/// <param name="delta"></param>
		public void NotifyDelta( ushort x, ushort y, ushort lod, float delta )
		{
			if( x >= mOffsetX && x < mBoundaryX
			    && y >= mOffsetY && y < mBoundaryY )
			{
				// within our bounds, check it's our LOD level
				if( lod >= mBaseLod && lod < mBaseLod + (ushort)( mLodLevels.Count ) )
				{
					int iter = 0;
					iter += lod - mBaseLod;
					LodLevel tmp = mLodLevels[ iter ];
					tmp.CalcMaxHeightDelta = System.Math.Max( tmp.CalcMaxHeightDelta, delta );
				}

				// pass to the children
				if( !IsLeaf )
				{
					for( int i = 0; i < 4; i++ )
					{
						mChildren[ i ].NotifyDelta( x, y, lod, delta );
					}
				}
			}
		}

		/// <summary>
		/// Notify the node (and children) that deltas are going to be calculated for a given range.
		/// </summary>
		/// <param name="rect"></param>
		public void PostDeltaCalculation( Rectangle rect )
		{
			if( rect.Left <= mBoundaryX || rect.Right > mOffsetX
			    || rect.Top <= mBoundaryY || rect.Bottom > mOffsetY )
			{
				// relevant to this node (overlaps)

				// each non-leaf node should know which of its children transitions
				// to the lower LOD level last, because this is the one which controls
				// when the parent takes over
				if( !IsLeaf )
				{
					float maxChildDelta = -1;
					TerrainQuadTreeNode childWithMaxHeightDelta = null;
					for( int i = 0; i < 4; i++ )
					{
						TerrainQuadTreeNode child = mChildren[ i ];
						child.PostDeltaCalculation( rect );
						float childData = child.GetLodLevel( (ushort)( child.LodCount - 1 ) ).CalcMaxHeightDelta;
						if( childData != 0 ) {}
						if( childData > maxChildDelta )
						{
							childWithMaxHeightDelta = child;
							maxChildDelta = childData;
						}
					}

					// make sure that our highest delta value is greater than all children's
					// otherwise we could have some crossover problems
					// for a non-leaf, there is only one LOD level
					LodLevel tmp = mLodLevels[ 0 ];
					tmp.CalcMaxHeightDelta = System.Math.Max( tmp.CalcMaxHeightDelta, maxChildDelta * 1.05f );
					mChildWithMaxHeightDelta = childWithMaxHeightDelta;
				}
				else
				{
					// make sure own LOD levels delta values ascend
					for( int i = 0; i < mLodLevels.Count - 1; i++ )
					{
						// the next LOD after this one should have a higher delta
						// otherwise it won't come into affect further back like it should!
						LodLevel tmp = mLodLevels[ i ];
						LodLevel tmpPlus = mLodLevels[ i + 1 ];
						tmpPlus.CalcMaxHeightDelta = System.Math.Max( tmpPlus.CalcMaxHeightDelta, tmp.CalcMaxHeightDelta * 1.05f );
					}
				}
			}
		}

		/// <summary>
		/// Promote the delta values calculated to the runtime ones (this must
		///	be called in the main thread). 
		/// </summary>
		/// <param name="rect"></param>
		public void FinaliseDeltaValues( Rectangle rect )
		{
			if( rect.Left <= mBoundaryX || rect.Right > mOffsetX
			    || rect.Top <= mBoundaryY || rect.Bottom > mOffsetY )
			{
				// relevant to this node (overlaps)

				// Children
				if( !IsLeaf )
				{
					for( int i = 0; i < 4; i++ )
					{
						TerrainQuadTreeNode child = mChildren[ i ];
						child.FinaliseDeltaValues( rect );
					}
				}

				// self
				LodLevel[] lvls = mLodLevels.ToArray();
				for( int i = 0; i < lvls.Length; i++ )
				{
					LodLevel tmp = lvls[ i ];
					// copy from 'calc' area to runtime value
					tmp.MaxHeightDelta = tmp.CalcMaxHeightDelta;
					// also trash stored cfactor
					tmp.LastCFactor = 0;
				}
			}
		}

		/// <summary>
		/// Assign vertex data to the tree, from a depth and at a given resolution.
		/// </summary>
		/// <param name="treeDeptStart">The first depth of tree that should use this data, owns the data</param>
		/// <param name="treeDepthEnd">The end of the depth that should use this data (exclusive)</param>
		/// <param name="resolution">The resolution of the data to use (compared to full terrain)</param>
		/// <param name="sz">The size of the data along one edge</param>
		public void AssignVertexData( ushort treeDepthStart, ushort treeDepthEnd, ushort resolution, ushort sz )
		{
			Debug.Assert( treeDepthStart >= mDepth, "Should not be calling this" );

			if( mDepth == treeDepthStart )
			{
				//we own this vertex data
				mNodeWithVertexData = this;
				mVertexDataRecord = new VertexDataRecord( resolution, sz, (ushort)( treeDepthEnd - treeDepthStart ) );

				CreateCpuVertexData();
				CreateCpuIndexData();

				//pass on to children
				if( !IsLeaf && treeDepthEnd > ( mDepth + 1 ) ) // treeDepthEnd is exclusive, and this is children
				{
					for( int i = 0; i < 4; i++ )
					{
						mChildren[ i ].UseAncestorVertexData( this, treeDepthEnd, resolution );
					}
				}
			}
			else
			{
				Debug.Assert( !IsLeaf, "No more levels below this!" );

				for( int i = 0; i < 4; i++ )
				{
					mChildren[ i ].AssignVertexData( treeDepthStart, treeDepthEnd, resolution, sz );
				}
			}
		}

		/// <summary>
		/// Tell a node that it should use an anscestor's vertex data.
		/// </summary>
		/// <param name="owner"></param>
		/// <param name="treeDepthEnd">The end of the depth that should use this data (exclusive)</param>
		/// <param name="resolution">The resolution of the data to use</param>
		public void UseAncestorVertexData( TerrainQuadTreeNode owner, int treeDepthEnd, int resolution )
		{
			mNodeWithVertexData = owner;
			mVertexDataRecord = null;

			if( !IsLeaf && treeDepthEnd > ( mDepth + 1 ) ) // treeDepthEnd is exclusive, and this is children
			{
				for( int i = 0; i < 4; i++ )
				{
					mChildren[ i ].UseAncestorVertexData( owner, treeDepthEnd, resolution );
				}
			}
			CreateCpuIndexData();
		}

		/// <summary>
		/// Tell the node to update its vertex data for a given region. 
		/// </summary>
		/// <param name="positions"></param>
		/// <param name="deltas"></param>
		/// <param name="rect"></param>
		/// <param name="cpuData"></param>
		public void UpdateVertexData( bool positions, bool deltas, Rectangle rect, bool cpuData )
		{
			if( rect.Left <= mBoundaryX || rect.Right > mOffsetX
			    || rect.Top <= mBoundaryY || rect.Bottom > mOffsetY )
			{
				// Do we have vertex data?
				if( mVertexDataRecord != null )
				{
					// Trim to our bounds
					Rectangle updateRect = new Rectangle( mOffsetX, mOffsetY, mBoundaryX, mBoundaryY );
					updateRect.Left = System.Math.Max( updateRect.Left, rect.Left );
					updateRect.Right = System.Math.Min( updateRect.Right, rect.Right );
					updateRect.Top = System.Math.Max( updateRect.Top, rect.Top );
					updateRect.Bottom = System.Math.Min( updateRect.Bottom, rect.Bottom );
					// update the GPU buffer directly
#warning TODO: do we have no use for CPU vertex data after initial load?
					// if so, destroy it to free RAM, this should be fast enough to 
					// to direct

					HardwareVertexBuffer posbuf = null, deltabuf = null;
					VertexData targetData = cpuData ?
					                                	mVertexDataRecord.CpuVertexData : mVertexDataRecord.GpuVertexData;

					if( positions )
					{
						posbuf = targetData.vertexBufferBinding.GetBuffer( (short)POSITION_BUFFER );
					}
					if( deltas )
					{
						deltabuf = targetData.vertexBufferBinding.GetBuffer( (short)DELTA_BUFFER );
					}

					UpdateVertexBuffer( posbuf, deltabuf, updateRect );
				}

				// pass on to children
				if( !IsLeaf )
				{
					for( int i = 0; i < 4; i++ )
					{
						mChildren[ i ].UpdateVertexData( positions, deltas, rect, cpuData );
						// merge bounds from children

						AxisAlignedBox childBox = new AxisAlignedBox( mChildren[ i ].AABB );

						// this box is relative to child centre
						Vector3 boxoffset = mChildren[ i ].LocalCentre - LocalCentre;
						childBox.Minimum = childBox.Minimum + boxoffset;
						childBox.Maximum = childBox.Maximum + boxoffset;
						mAABB.Merge( childBox );
					}
				}
				if( mRend != null )
				{
					mRend.ParentSceneNode.NeedUpdate();
				}
				// Make sure node knows to update
				/*  if (mMovable != null)
                    mMovable.ParentSceneNode.NeedUpdate();*/
			}
		}

		/// <summary>
		/// Merge a point (relative to terrain node) into the local bounds, 
		///	and that of children if applicable.
		/// </summary>
		/// <param name="x">The point on the terrain to which this position corresponds 
		///	(affects which nodes update their bounds)</param>
		/// <param name="y">The point on the terrain to which this position corresponds 
		///	(affects which nodes update their bounds)</param>
		/// <param name="pos">The position relative to the terrain centre</param>
		public void MergeIntoBounds( long x, long y, Vector3 pos )
		{
			if( PointIntersectsNode( x, y ) )
			{
				Vector3 localPos = pos - mLocalCentre;
				mAABB.Merge( localPos );
				mBoundingRadius = System.Math.Max( mBoundingRadius, localPos.Length );
				if( !IsLeaf )
				{
					for( int i = 0; i < 4; ++i )
					{
						mChildren[ i ].MergeIntoBounds( x, y, pos );
					}
				}
			}
		}

		/// <summary>
		/// Reset the bounds of this node and all its children for the region given.
		/// </summary>
		/// <param name="rect">The region for which bounds should be reset, in top-level terrain coords</param>
		public void ResetBounds( Rectangle rect )
		{
			if( RectContainsNode( rect ) )
			{
				mAABB = new AxisAlignedBox();
				mBoundingRadius = 0;

				if( !IsLeaf )
				{
					for( int i = 0; i < 4; ++i )
					{
						mChildren[ i ].ResetBounds( rect );
					}
				}
			}
		}

		/// <summary>
		/// Returns true if the given rectangle completely contains the terrain area that
		/// this node references.
		/// </summary>
		/// <param name="rect">The region in top-level terrain coords</param>
		/// <returns></returns>
		public bool RectContainsNode( Rectangle rect )
		{
			return ( rect.Left <= mOffsetX && rect.Right > mBoundaryX &&
			         rect.Top <= mOffsetY && rect.Bottom > mBoundaryY );
		}

		/// <summary>
		///  Returns true if the given rectangle overlaps the terrain area that
		///	 this node references.
		/// </summary>
		/// <param name="rect">The region in top-level terrain coords</param>
		/// <returns></returns>
		public bool RectIntersectsNode( Rectangle rect )
		{
			return ( rect.Right >= mOffsetX && rect.Left <= mBoundaryX &&
			         rect.Bottom >= mOffsetY && rect.Top <= mBoundaryY );
		}

		/// <summary>
		/// Returns true if the given point is in the terrain area that
		/// this node references.
		/// </summary>
		/// <param name="x">The point in top-level terrain coords</param>
		/// <param name="y">The point in top-level terrain coords</param>
		/// <returns></returns>
		public bool PointIntersectsNode( long x, long y )
		{
			return ( x >= mOffsetX && x < mBoundaryX &&
			         y >= mOffsetY && y < mBoundaryY );
		}

		/// <summary>
		/// Calculate appropriate LOD for this node and children
		/// </summary>
		/// <param name="cam">The camera to be used (this should already be the LOD camera)</param>
		/// <param name="cFactor">The cFactor which incorporates the viewport size, max pixel error and lod bias</param>
		/// <returns>true if this node or any of its children were selected for rendering</returns>
		public bool CalculateCurrentLod( Camera cam, float cFactor )
		{
			mSelfOrChildRendered = false;

			//check children first.
			int childrenRenderedOut = 0;
			if( !IsLeaf )
			{
				for( int i = 0; i < 4; ++i )
				{
					if( mChildren[ i ].CalculateCurrentLod( cam, cFactor ) )
					{
						++childrenRenderedOut;
					}
				}
			}

			if( childrenRenderedOut == 0 )
			{
				// no children were within their LOD ranges, so we should consider our own
				Vector3 localPos = cam.DerivedPosition - mLocalCentre - mTerrain.Position;
				float dist;
				if( TerrainGlobalOptions.IsUseRayBoxDistanceCalculation )
				{
					// Get distance to this terrain node (to closest point of the box)
					// head towards centre of the box (note, box may not cover mLocalCentre because of height)
					Vector3 dir = mAABB.Center - localPos;
					dir.Normalize();
					Ray ray = new Ray( localPos, dir );
					IntersectResult intersectRes = ray.Intersects( mAABB );

					// ray will always intersect, we just want the distance
					dist = intersectRes.Distance;
				}
				else
				{
					// distance to tile centre
					dist = localPos.Length;
					// deduct half the radius of the box, assume that on average the 
					// worst case is best approximated by this
					dist -= ( mBoundingRadius * 0.5f );
				}

				// Do material LOD
				Material material = this.Material;
				Axiom.Core.LodStrategy str = material.LodStrategy;
				float lodValue = str.GetValue( mRend, cam );
				mMaterialLodIndex = (ushort)material.GetLodIndex( lodValue );
				// For each LOD, the distance at which the LOD will transition *downwards*
				// is given by 
				// distTransition = maxDelta * cFactor;
				int lodLvl = 0;
				mCurrentLod = -1;
				foreach( LodLevel l in mLodLevels )
				{
					// If we have no parent, and this is the lowest LOD, we always render
					// this is the 'last resort' so to speak, we always enoucnter this last
					if( lodLvl + 1 == mLodLevels.Count && mParent == null )
					{
						CurentLod = lodLvl;
						mSelfOrChildRendered = true;
						mLodTransition = 0;
					}
					else
					{
						//check the distance
						LodLevel ll = l;
						// Calculate or reuse transition distance
						float distTransition;
						if( Utility.RealEqual( cFactor, ll.LastTranistionDist ) )
						{
							distTransition = ll.LastTranistionDist;
						}
						else
						{
							distTransition = ll.MaxHeightDelta * cFactor;
							ll.LastCFactor = cFactor;
							ll.LastTranistionDist = distTransition;
						}

						if( dist < distTransition )
						{
							// we're within range of this LOD
							CurentLod = lodLvl;
							mSelfOrChildRendered = true;

							if( mTerrain.IsMorphRequired )
							{
								// calculate the transition percentage
								// we need a percentage of the total distance for just this LOD, 
								// which means taking off the distance for the next higher LOD
								// which is either the previous entry in the LOD list, 
								// or the largest of any children. In both cases these will
								// have been calculated before this point, since we process
								// children first. Distances at lower LODs are guaranteed
								// to be larger than those at higher LODs

								float distTotal = distTransition;
								if( IsLeaf )
								{
									// Any higher LODs?
#warning: check if this if is correct!
									if( lodLvl < mLodLevels.Count )
									{
										int prec = lodLvl - 1;
										distTotal -= mLodLevels[ lodLvl ].LastTranistionDist;
									}
								}
								else
								{
									// Take the distance of the lowest LOD of child
									LodLevel childLod = mChildWithMaxHeightDelta.GetLodLevel(
									                                                         (ushort)( mChildWithMaxHeightDelta.LodCount - 1 ) );
									distTotal -= childLod.LastTranistionDist;
								}
								// fade from 0 to 1 in the last 25% of the distance
								float distMorphRegion = distTotal * 0.25f;
								float distRemain = distTransition - dist;

								mLodTransition = 1.0f - ( distRemain / distMorphRegion );
								mLodTransition = System.Math.Min( 1.0f, mLodTransition );
								mLodTransition = System.Math.Max( 0.0f, mLodTransition );

								// Pass both the transition % and target LOD (GLOBAL current + 1)
								// this selectively applies the morph just to the
								// vertices which would drop out at this LOD, even 
								// while using the single shared vertex data
								mRend.SetCustomParameter( Terrain.LOD_MORPH_CUSTOM_PARAM,
								                          new Vector4( mLodTransition, mCurrentLod + mBaseLod + 1, 0, 0 ) );
							} //end if

							// since LODs are ordered from highest to lowest detail, 
							// we can stop looking now
							break;
						} //end if
					} //end else
					++lodLvl;
				} //end for each
			} //end if
			else
			{
				// we should not render ourself
				mCurrentLod = -1;
				mSelfOrChildRendered = true;
				if( childrenRenderedOut < 4 )
				{
					// only *some* children decided to render on their own, but either 
					// none or all need to render, so set the others manually to their lowest
					for( int i = 0; i < 4; ++i )
					{
						TerrainQuadTreeNode child = mChildren[ i ];
						if( !child.IsSelfOrChildrenRenderedAtCurrentLod )
						{
							child.CurentLod = child.LodCount - 1;
							child.LodTransition = 1.0f;
						}
					}
				} //(childRenderedCount < 4)
			} // (childRenderedCount == 0)

			return mSelfOrChildRendered;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="posBuff"></param>
		/// <param name="deltaBuf"></param>
		/// <param name="rect"></param>
		unsafe protected void UpdateVertexBuffer( HardwareVertexBuffer posBuff, HardwareVertexBuffer deltaBuf, Rectangle rect )
		{
			unsafe
			{
				Debug.Assert( rect.Left >= mOffsetX && rect.Right <= mBoundaryX &&
				              rect.Top >= mOffsetY && rect.Bottom <= mBoundaryY );
				// potentially reset our bounds depending on coverage of the update
				ResetBounds( rect );
				//main data
				ushort inc = (ushort)( ( mTerrain.Size - 1 ) / ( mVertexDataRecord.Resolution - 1 ) );
				long destOffsetX = rect.Left <= mOffsetX ? 0 : ( rect.Left - mOffsetX ) / inc;
				long destOffsetY = rect.Top <= mOffsetY ? 0 : ( rect.Top - mOffsetY ) / inc;
				// Fill the buffers

				BufferLocking lockmode;
				if( destOffsetX != 0 || destOffsetY != 0 || rect.Right - rect.Left < mSize
				    || rect.Bottom - rect.Top < mSize )
				{
					lockmode = BufferLocking.Normal;
				}
				else
				{
					lockmode = BufferLocking.Discard;
				}
				float uvScale = 1.0f / ( mTerrain.Size - 1 );
				float* pBaseHeight = (float*)mTerrain.GetHeightData( rect.Left, rect.Top );
				float* pBaseDelta = (float*)mTerrain.GetDeltaData( rect.Left, rect.Top );
				ushort rowskip = (ushort)( mTerrain.Size * inc );
				ushort destPosRowSkip = 0, destDeltaRowSkip = 0;
				byte* pRootPosBuf = (byte*)0;
				byte* pRootDeltaBuf = (byte*)0;
				byte* pRowPosBuf = (byte*)0;
				byte* pRowDeltaBuf = (byte*)0;
				if( posBuff != null )
				{
					destPosRowSkip = (ushort)( mVertexDataRecord.Size * posBuff.VertexSize );
					pRootPosBuf = (byte*)posBuff.Lock( lockmode );
					pRowPosBuf = pRootPosBuf;
					// skip dest buffer in by left/top
					pRowPosBuf += destOffsetY * destPosRowSkip + destOffsetX * posBuff.VertexSize;
				}
				if( deltaBuf != null )
				{
					destDeltaRowSkip = (ushort)( mVertexDataRecord.Size * deltaBuf.VertexSize );
					pRootDeltaBuf = (byte*)deltaBuf.Lock( lockmode );
					pRowDeltaBuf = pRootDeltaBuf;
					// skip dest buffer in by left/top
					pRowDeltaBuf += destOffsetY * destDeltaRowSkip + destOffsetX * deltaBuf.VertexSize;
				}
				Vector3 pos = Vector3.Zero;

				for( ushort y = (ushort)rect.Top; y < rect.Bottom; y += inc )
				{
					float* pHeight = (float*)pBaseHeight;
					float* pDelta = (float*)pBaseDelta;
					float* pPosBuf = (float*)pRowPosBuf;
					float* pDeltaBuf = (float*)pRowDeltaBuf;
					for( ushort x = (ushort)rect.Left; x < rect.Right; x += inc )
					{
						if( pPosBuf != (float*)IntPtr.Zero )
						{
							if( *pHeight != 0 )
							{
								float val = *pHeight;
								// LogManager.Instance.Write("GetPoint(" + x.ToString() + " " + y.ToString() + " " + val.ToString());
							}
							mTerrain.GetPoint( x, y, *pHeight, ref pos );
							// Update bounds *before* making relative
							MergeIntoBounds( x, y, pos );
							// relative to local centre
							pos -= mLocalCentre;
							pHeight += inc;

							*pPosBuf++ = pos.x;
							*pPosBuf++ = pos.y;
							*pPosBuf++ = pos.z;

							// UVs - base UVs vary from 0 to 1, all other values
							// will be derived using scalings
							*pPosBuf++ = x * uvScale;
							*pPosBuf++ = 1.0f - ( y * uvScale );
						}

						if( pDeltaBuf != (float*)IntPtr.Zero )
						{
							//delta
							*pDeltaBuf++ = *pDelta;
							pDelta += inc;
							// delta LOD threshold
							// we want delta to apply to LODs no higher than this value
							// at runtime this will be combined with a per-renderable parameter
							// to ensure we only apply morph to the correct LOD
							*pDeltaBuf++ = (float)mTerrain.GetLODLevelWhenVertexEliminated( x, y ) - 1.0f;
						}
					} //end for

					pBaseHeight += rowskip;
					pBaseDelta += rowskip;
					if( pRowPosBuf != (byte*)IntPtr.Zero )
					{
						pRowPosBuf += destPosRowSkip;
					}
					if( pRowDeltaBuf != (byte*)IntPtr.Zero )
					{
						pRowDeltaBuf += destDeltaRowSkip;
					}
				} //end for

				// Skirts now
				// skirt spacing based on top-level resolution (* inc to cope with resolution which is not the max)
				ushort skirtSpacing = (ushort)( mVertexDataRecord.SkirtRowColSkip * inc );
				Vector3 skirtOffset = Vector3.Zero;
				mTerrain.GetVector( 0, 0, -mTerrain.SkirtSize, ref skirtOffset );
				// skirt rows
				// clamp rows to skirt spacing (round up)
				long skirtStartX = rect.Left;
				long skirtStartY = rect.Top;
				if( skirtStartY % skirtSpacing != 0 )
				{
					skirtStartY += skirtSpacing - ( skirtStartY % skirtSpacing );
				}

				skirtStartY = System.Math.Max( skirtStartY, (long)mOffsetY );
				pBaseHeight = (float*)mTerrain.GetHeightData( skirtStartX, skirtStartY );
				if( posBuff != null )
				{
					// position dest buffer just after the main vertex data
					pRowPosBuf = pRootPosBuf + posBuff.VertexSize *
					             mVertexDataRecord.Size * mVertexDataRecord.Size;
					// move it onwards to skip the skirts we don't need to update
					pRowPosBuf += destPosRowSkip * ( skirtStartY - mOffsetY ) / skirtSpacing;
					pRowPosBuf += posBuff.VertexSize * ( skirtStartX - mOffsetX );
				}
				if( deltaBuf != null )
				{
					// position dest buffer just after the main vertex data
					pRowDeltaBuf = pRootDeltaBuf + deltaBuf.VertexSize
					               * mVertexDataRecord.Size * mVertexDataRecord.Size;
					// move it onwards to skip the skirts we don't need to update
					pRowDeltaBuf += destDeltaRowSkip * ( skirtStartY - mOffsetY ) / skirtSpacing;
					pRowDeltaBuf += deltaBuf.VertexSize * ( skirtStartX - mOffsetX );
				}

				for( ushort y = (ushort)skirtStartY; y < (ushort)rect.Bottom; y += skirtSpacing )
				{
					float* pHeight = (float*)pBaseHeight;
					float* pPosBuf = (float*)pRowPosBuf;
					float* pDeltaBuf = (float*)pRowDeltaBuf;
					for( ushort x = (ushort)skirtStartX; x < (ushort)rect.Right; x += inc )
					{
						if( pPosBuf != (float*)IntPtr.Zero )
						{
							mTerrain.GetPoint( x, y, *pHeight, ref pos );
							// relative to local centre
							pos -= mLocalCentre;
							pHeight += inc;
							pos += skirtOffset;
							*pPosBuf++ = pos.x;
							*pPosBuf++ = pos.y;
							*pPosBuf++ = pos.z;

							// UVs - same as base
							*pPosBuf++ = x * uvScale;
							*pPosBuf++ = 1.0f - ( y * uvScale );
						}
						if( pDeltaBuf != (float*)IntPtr.Zero )
						{
							// delta (none)
							*pDeltaBuf++ = 0;
							// delta threshold (irrelevant)
							*pDeltaBuf++ = 99;
						}
					} //end for
					pBaseHeight += mTerrain.Size * skirtSpacing;
					if( pRowPosBuf != (byte*)IntPtr.Zero )
					{
						pRowPosBuf += destPosRowSkip;
					}
					if( pRowDeltaBuf != (byte*)IntPtr.Zero )
					{
						pRowDeltaBuf += destDeltaRowSkip;
					}
				} //end for

				// skirt cols
				// clamp cols to skirt spacing (round up)
				skirtStartX = rect.Left;
				if( skirtStartX % skirtSpacing != 0 )
				{
					skirtStartX += skirtSpacing - ( skirtStartX % skirtSpacing );
				}
				skirtStartY = rect.Top;
				skirtStartX = System.Math.Max( skirtStartX, (long)mOffsetX );

				if( posBuff != null )
				{
					// position dest buffer just after the main vertex data and skirt rows
					pRowPosBuf = pRootPosBuf + posBuff.VertexSize
					             * mVertexDataRecord.Size * mVertexDataRecord.Size;
					// skip the row skirts
					pRowPosBuf += mVertexDataRecord.NumSkirtRowsCols * mVertexDataRecord.Size * posBuff.VertexSize;
					// move it onwards to skip the skirts we don't need to update
					pRowPosBuf += destPosRowSkip * ( skirtStartX - mOffsetX ) / skirtSpacing;
					pRowPosBuf += posBuff.VertexSize * ( skirtStartY - mOffsetY );
				}
				if( deltaBuf != null )
				{
					// Deltaition dest buffer just after the main vertex data and skirt rows
					pRowDeltaBuf = pRootDeltaBuf + deltaBuf.VertexSize
					               * mVertexDataRecord.Size * mVertexDataRecord.Size;

					// skip the row skirts
					pRowDeltaBuf += mVertexDataRecord.NumSkirtRowsCols * mVertexDataRecord.Size * deltaBuf.VertexSize;
					// move it onwards to skip the skirts we don't need to update
					pRowDeltaBuf += destDeltaRowSkip * ( skirtStartX - mOffsetX ) / skirtSpacing;
					pRowDeltaBuf += deltaBuf.VertexSize * ( skirtStartY - mOffsetY );
				}
				for( ushort x = (ushort)skirtStartX; x < (ushort)rect.Right; x += skirtSpacing )
				{
					float* pPosBuf = (float*)pRowPosBuf;
					float* pDeltaBuf = (float*)pRowDeltaBuf;
					for( ushort y = (ushort)skirtStartY; y < (ushort)rect.Bottom; y += inc )
					{
						if( pPosBuf != (float*)IntPtr.Zero )
						{
							//float* hTmp = (float*)mTerrain.GetHeightData(x, y);

							mTerrain.GetPoint( x, y, mTerrain.GetHeightAtPoint( x, y ), ref pos );
							// relative to local centre
							pos -= mLocalCentre;
							pos += skirtOffset;

							*pPosBuf++ = pos.x;
							*pPosBuf++ = pos.y;
							*pPosBuf++ = pos.z;

							// UVs - same as base
							*pPosBuf++ = x * uvScale;
							*pPosBuf++ = 1.0f - ( y * uvScale );
						}
						if( pDeltaBuf != (float*)IntPtr.Zero )
						{
							// delta (none)
							*pDeltaBuf++ = 0;
							// delta threshold (irrelevant)
							*pDeltaBuf++ = 99;
						}
					} //end for
					if( pRowPosBuf != (byte*)IntPtr.Zero )
					{
						pRowPosBuf += destPosRowSkip;
					}
					if( pRowDeltaBuf != (byte*)IntPtr.Zero )
					{
						pRowDeltaBuf += destDeltaRowSkip;
					}
				} //end for
				if( posBuff != null )
				{
					posBuff.Unlock();
				}
				if( deltaBuf != null )
				{
					deltaBuf.Unlock();
				}
			}
		}

		unsafe private void UpdateVertexBufferr( HardwareVertexBuffer posbuf, HardwareVertexBuffer deltabuf, Rectangle rect )
		{
			// potentially reset our bounds depending on coverage of the update
			ResetBounds( rect );

			// Main data
			ushort inc = (ushort)( ( mTerrain.Size - 1 ) / ( mVertexDataRecord.Resolution - 1 ) );
			long destOffsetX = rect.Left <= mOffsetX ? 0 : ( rect.Left - mOffsetX ) / inc;
			long destOffsetY = rect.Top <= mOffsetY ? 0 : ( rect.Top - mOffsetY ) / inc;
			// Fill the buffers

			BufferLocking lockMode;
			if( destOffsetX != 0 || destOffsetY != 0 || rect.Right - rect.Left < mSize
			    || rect.Bottom - rect.Top < mSize )
			{
				lockMode = BufferLocking.Normal;
			}
			else
			{
				lockMode = BufferLocking.Discard;
			}

			Real uvScale = 1.0 / ( mTerrain.Size - 1 );
			float* pBaseHeight = (float*)mTerrain.GetHeightData( rect.Left, rect.Top );
			float* pBaseDelta = (float*)mTerrain.GetDeltaData( rect.Left, rect.Top );
			ushort rowskip = (ushort)( mTerrain.Size * inc );
			ushort destPosRowSkip = 0, destDeltaRowSkip = 0;
			byte* pRootPosBuf = (byte*)IntPtr.Zero;
			byte* pRootDeltaBuf = (byte*)IntPtr.Zero;
			byte* pRowPosBuf = (byte*)IntPtr.Zero;
			byte* pRowDeltaBuf = (byte*)IntPtr.Zero;

			if( posbuf != null )
			{
				destPosRowSkip = (ushort)( mVertexDataRecord.Size * posbuf.VertexSize );
				pRootPosBuf = (byte*)( posbuf.Lock( lockMode ) );
				pRowPosBuf = pRootPosBuf;
				// skip dest buffer in by left/top
				pRowPosBuf += destOffsetY * destPosRowSkip + destOffsetX * posbuf.VertexSize;
			}
			if( deltabuf != null )
			{
				destDeltaRowSkip = (ushort)( mVertexDataRecord.Size * deltabuf.VertexSize );
				pRootDeltaBuf = (byte*)( deltabuf.Lock( lockMode ) );
				pRowDeltaBuf = pRootDeltaBuf;
				// skip dest buffer in by left/top
				pRowDeltaBuf += destOffsetY * destDeltaRowSkip + destOffsetX * deltabuf.VertexSize;
			}
			Vector3 pos = Vector3.Zero;

			int posIndex = 0;
			for( ushort y = (ushort)rect.Top; y < rect.Bottom; y += inc )
			{
				float* pHeight = pBaseHeight;
				float* pDelta = pBaseDelta;
				float* pPosBuf = (float*)( pRowPosBuf );
				float* pDeltaBuf = (float*)( pRowDeltaBuf );
				for( ushort x = (ushort)rect.Left; x < rect.Right; x += inc )
				{
					if( pPosBuf != (float*)IntPtr.Zero )
					{
						mTerrain.GetPoint( x, y, pBaseHeight[ posIndex ], ref pos );
						// Update bounds *before* making relative
						MergeIntoBounds( x, y, pos );
						// relative to local centre
						pos -= mLocalCentre;

						//pHeight += inc;
						posIndex += inc;

						*pPosBuf++ = pos.x;
						*pPosBuf++ = pos.y;
						*pPosBuf++ = pos.z;

						// UVs - base UVs vary from 0 to 1, all other values
						// will be derived using scalings
						*pPosBuf++ = x * uvScale;
						*pPosBuf++ = 1.0 - ( y * uvScale );
					}

					if( pDeltaBuf != (float*)IntPtr.Zero )
					{
						// delta
						*pDeltaBuf++ = *pDelta;
						pDelta += inc;
						// delta LOD threshold
						// we want delta to apply to LODs no higher than this value
						// at runtime this will be combined with a per-renderable parameter
						// to ensure we only apply morph to the correct LOD
						*pDeltaBuf++ = (float)mTerrain.GetLODLevelWhenVertexEliminated( x, y ) - 1.0f;
					}
				}
				//pBaseHeight += rowskip;
				posIndex += rowskip;
				pBaseDelta += rowskip;
				if( pRowPosBuf != (float*)IntPtr.Zero )
				{
					pRowPosBuf += destPosRowSkip;
				}
				if( pRowDeltaBuf != (float*)IntPtr.Zero )
				{
					pRowDeltaBuf += destDeltaRowSkip;
				}
			}

			// Skirts now
			// skirt spacing based on top-level resolution (* inc to cope with resolution which is not the max)
			ushort skirtSpacing = (ushort)( mVertexDataRecord.SkirtRowColSkip * inc );
			Vector3 skirtOffset = Vector3.Zero;
			mTerrain.GetVector( 0, 0, -mTerrain.SkirtSize, ref skirtOffset );

			// skirt rows
			// clamp rows to skirt spacing (round up)
			long skirtStartX = rect.Left;
			long skirtStartY = rect.Top;
			if( skirtStartY % skirtSpacing != 0 )
			{
				skirtStartY += skirtSpacing - ( skirtStartY % skirtSpacing );
			}
			skirtStartY = System.Math.Max( skirtStartY, (long)mOffsetY );
			pBaseHeight = (float*)mTerrain.GetHeightData( skirtStartX, skirtStartY );
			if( posbuf != null )
			{
				// position dest buffer just after the main vertex data
				pRowPosBuf = pRootPosBuf + posbuf.VertexSize
				             * mVertexDataRecord.Size * mVertexDataRecord.Size;
				// move it onwards to skip the skirts we don't need to update
				pRowPosBuf += destPosRowSkip * ( skirtStartY - mOffsetY ) / skirtSpacing;
				pRowPosBuf += posbuf.VertexSize * ( skirtStartX - mOffsetX );
			}
			if( deltabuf != null )
			{
				// position dest buffer just after the main vertex data
				pRowDeltaBuf = pRootDeltaBuf + deltabuf.VertexSize
				               * mVertexDataRecord.Size * mVertexDataRecord.Size;
				// move it onwards to skip the skirts we don't need to update
				pRowDeltaBuf += destDeltaRowSkip * ( skirtStartY - mOffsetY ) / skirtSpacing;
				pRowDeltaBuf += deltabuf.VertexSize * ( skirtStartX - mOffsetX );
			}
			for( ushort y = (ushort)skirtStartY; y < rect.Bottom; y += skirtSpacing )
			{
				float* pHeight = pBaseHeight;
				float* pPosBuf = (float*)( pRowPosBuf );
				float* pDeltaBuf = (float*)( pRowDeltaBuf );
				for( ushort x = (ushort)skirtStartX; x < rect.Right; x += inc )
				{
					if( pPosBuf != (float*)IntPtr.Zero )
					{
						mTerrain.GetPoint( x, y, *pHeight, ref pos );
						// relative to local centre
						pos -= mLocalCentre;
						pHeight += inc;

						pos += skirtOffset;

						*pPosBuf++ = pos.x;
						*pPosBuf++ = pos.y;
						*pPosBuf++ = pos.z;

						// UVs - same as base
						*pPosBuf++ = x * uvScale;
						*pPosBuf++ = 1.0 - ( y * uvScale );
					}

					if( pDeltaBuf != (float*)IntPtr.Zero )
					{
						// delta (none)
						*pDeltaBuf++ = 0;
						// delta threshold (irrelevant)
						*pDeltaBuf++ = 99;
					}
				}
				pBaseHeight += mTerrain.Size * skirtSpacing;
				if( pRowPosBuf != (byte*)IntPtr.Zero )
				{
					pRowPosBuf += destPosRowSkip;
				}
				if( pRowDeltaBuf != (byte*)IntPtr.Zero )
				{
					pRowDeltaBuf += destDeltaRowSkip;
				}
			}
			// skirt cols
			// clamp cols to skirt spacing (round up)
			skirtStartX = rect.Left;
			if( skirtStartX % skirtSpacing != 0 )
			{
				skirtStartX += skirtSpacing - ( skirtStartX % skirtSpacing );
			}
			skirtStartY = rect.Top;
			skirtStartX = System.Math.Max( skirtStartX, (long)mOffsetX );
			if( posbuf != null )
			{
				// position dest buffer just after the main vertex data and skirt rows
				pRowPosBuf = pRootPosBuf + posbuf.VertexSize
				             * mVertexDataRecord.Size * mVertexDataRecord.Size;
				// skip the row skirts
				pRowPosBuf += mVertexDataRecord.NumSkirtRowsCols * mVertexDataRecord.Size * posbuf.VertexSize;
				// move it onwards to skip the skirts we don't need to update
				pRowPosBuf += destPosRowSkip * ( skirtStartX - mOffsetX ) / skirtSpacing;
				pRowPosBuf += posbuf.VertexSize * ( skirtStartY - mOffsetY );
			}
			if( deltabuf != null )
			{
				// Deltaition dest buffer just after the main vertex data and skirt rows
				pRowDeltaBuf = pRootDeltaBuf + deltabuf.VertexSize
				               * mVertexDataRecord.Size * mVertexDataRecord.Size;
				// skip the row skirts
				pRowDeltaBuf += mVertexDataRecord.NumSkirtRowsCols * mVertexDataRecord.Size * deltabuf.VertexSize;
				// move it onwards to skip the skirts we don't need to update
				pRowDeltaBuf += destDeltaRowSkip * ( skirtStartX - mOffsetX ) / skirtSpacing;
				pRowDeltaBuf += deltabuf.VertexSize * ( skirtStartY - mOffsetY );
			}

			for( ushort x = (ushort)skirtStartX; x < rect.Right; x += skirtSpacing )
			{
				float* pPosBuf = (float*)( pRowPosBuf );
				float* pDeltaBuf = (float*)( pRowDeltaBuf );
				for( ushort y = (ushort)skirtStartY; y < rect.Bottom; y += inc )
				{
					if( pPosBuf != (float*)IntPtr.Zero )
					{
						mTerrain.GetPoint( x, y, mTerrain.GetHeightAtPoint( x, y ), ref pos );
						// relative to local centre
						pos -= mLocalCentre;
						pos += skirtOffset;

						*pPosBuf++ = pos.x;
						*pPosBuf++ = pos.y;
						*pPosBuf++ = pos.z;

						// UVs - same as base
						*pPosBuf++ = x * uvScale;
						*pPosBuf++ = 1.0 - ( y * uvScale );
					}
					if( pDeltaBuf != (float*)IntPtr.Zero )
					{
						// delta (none)
						*pDeltaBuf++ = 0;
						// delta threshold (irrelevant)
						*pDeltaBuf++ = 99;
					}
				}
				if( pRowPosBuf != (byte*)IntPtr.Zero )
				{
					pRowPosBuf += destPosRowSkip;
				}
				if( pRowDeltaBuf != (byte*)IntPtr.Zero )
				{
					pRowDeltaBuf += destDeltaRowSkip;
				}
			}

			if( posbuf != null )
			{
				posbuf.Unlock();
			}
			if( deltabuf != null )
			{
				deltabuf.Unlock();
			}
		}

		/// <summary>
		/// 
		/// </summary>
		protected void CreateCpuVertexData()
		{
			if( mVertexDataRecord != null )
			{
				DestroyCpuVertexData();

				// create vertex structure, not using GPU for now (these are CPU structures)
				VertexDeclaration dcl = new VertexDeclaration();
				VertexBufferBinding bufbind = new VertexBufferBinding();

				mVertexDataRecord.CpuVertexData = new VertexData();
				mVertexDataRecord.CpuVertexData.vertexBufferBinding = bufbind;
				mVertexDataRecord.CpuVertexData.vertexDeclaration = dcl;
				// Vertex declaration
				// TODO: consider vertex compression
				int offset = 0;
				// POSITION 
				// float3(x, y, z)
				offset += dcl.AddElement( (short)POSITION_BUFFER, offset, VertexElementType.Float3, VertexElementSemantic.Position ).Size;
				// UV0
				// float2(u, v)
				// TODO - only include this if needing fixed-function
				offset += dcl.AddElement( (short)POSITION_BUFFER, offset, VertexElementType.Float2, VertexElementSemantic.TexCoords, 0 ).Size;
				// UV1 delta information
				// float2(delta, deltaLODthreshold)
				offset = 0;
				offset += dcl.AddElement( (short)DELTA_BUFFER, offset, VertexElementType.Float2, VertexElementSemantic.TexCoords, 1 ).Size;

				// Calculate number of vertices
				// Base geometry size * size

				int baseNumVerts = (int)( mVertexDataRecord.Size * mVertexDataRecord.Size );
				int numVerts = baseNumVerts;
				// Now add space for skirts
				// Skirts will be rendered as copies of the edge vertices translated downwards
				// Some people use one big fan with only 3 vertices at the bottom, 
				// but this requires creating them much bigger that necessary, meaning
				// more unnecessary overdraw, so we'll use more vertices 
				// You need 2^levels + 1 rows of full resolution (max 129) vertex copies, plus
				// the same number of columns. There are common vertices at intersections
				ushort levels = mVertexDataRecord.TreeLevels;
				mVertexDataRecord.NumSkirtRowsCols = (ushort)( System.Math.Pow( 2, levels ) + 1 );
				mVertexDataRecord.SkirtRowColSkip = (ushort)( ( mVertexDataRecord.Size - 1 ) / ( mVertexDataRecord.NumSkirtRowsCols - 1 ) );
				numVerts += mVertexDataRecord.Size * mVertexDataRecord.NumSkirtRowsCols;
				numVerts += mVertexDataRecord.Size * mVertexDataRecord.NumSkirtRowsCols;

				HardwareVertexBuffer def = HardwareBufferManager.Instance.CreateVertexBuffer( dcl.GetVertexSize( (short)POSITION_BUFFER ), numVerts, BufferUsage.StaticWriteOnly, false ); //new DefaultHardwareVertexBuffer(dcl.GetVertexSize((short)POSITION_BUFFER), numVerts, BufferUsage.StaticWriteOnly);
				//manually create CPU-side buffer
				HardwareVertexBuffer posBuf = def;
				def = HardwareBufferManager.Instance.CreateVertexBuffer( dcl.GetVertexSize( (short)DELTA_BUFFER ), numVerts, BufferUsage.StaticWriteOnly, false ); //new DefaultHardwareVertexBuffer(dcl.GetVertexSize((short)DELTA_BUFFER),
				//numVerts, BufferUsage.StaticWriteOnly);

				HardwareVertexBuffer deltabuf = def;
				mVertexDataRecord.CpuVertexData.vertexStart = 0;
				mVertexDataRecord.CpuVertexData.vertexCount = numVerts;

				Rectangle updateRect = new Rectangle( mOffsetX, mOffsetY, mBoundaryX, mBoundaryY );
				UpdateVertexBuffer( posBuf, deltabuf, updateRect );
				mVertexDataRecord.IsGpuVertexDataDirty = true;
				bufbind.SetBinding( (short)POSITION_BUFFER, posBuf );
				bufbind.SetBinding( (short)DELTA_BUFFER, deltabuf );
			}
		}

		/// <summary>
		/// 
		/// </summary>
		protected void CreateCpuIndexData()
		{
			for( int lod = 0; lod < mLodLevels.Count; lod++ )
			{
				LodLevel ll = mLodLevels[ lod ];
				ll.CpuIndexData = null;
				ll.CpuIndexData = new IndexData();
				CreateTriangleStripBuffer( ll.BatchSize, ll.CpuIndexData );
				mLodLevels[ lod ] = ll;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		protected void DestroyCpuVertexData()
		{
			if( mVertexDataRecord != null && mVertexDataRecord.CpuVertexData != null )
			{
				// delete the bindings and declaration manually since not from a buf mgr
				mVertexDataRecord.CpuVertexData.vertexDeclaration = null;
				mVertexDataRecord.CpuVertexData.vertexBufferBinding = null;

				mVertexDataRecord.CpuVertexData = null;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		protected void DestroyCpuIndexData()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// 
		/// </summary>
		protected void CreateGpuVertexData()
		{
#warning: // TODO - mutex cpu data
			if( mVertexDataRecord != null && mVertexDataRecord.CpuVertexData != null
			    && mVertexDataRecord.GpuVertexData == null )
			{
				// clone CPU data into GPU data
				// default is to create new declarations from hardware manager
				mVertexDataRecord.GpuVertexData = mVertexDataRecord.CpuVertexData.Clone();
				mVertexDataRecord.IsGpuVertexDataDirty = false;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		protected void DestroyGpuVertexData()
		{
			if( mVertexDataRecord != null && mVertexDataRecord.GpuVertexData != null )
			{
				mVertexDataRecord.GpuVertexData = null;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		protected void UpdateGpuVertexData()
		{
#warning: // TODO - mutex cpu data
			if( mVertexDataRecord != null && mVertexDataRecord.IsGpuVertexDataDirty )
			{
				mVertexDataRecord.GpuVertexData.vertexBufferBinding.GetBuffer( 0 )
					.CopyData( mVertexDataRecord.CpuVertexData.vertexBufferBinding.GetBuffer( 0 ), 0, 0, mVertexDataRecord.CpuVertexData.vertexBufferBinding.GetBuffer( 0 ).Size );

				mVertexDataRecord.GpuVertexData.vertexBufferBinding.GetBuffer( 1 )
					.CopyData( mVertexDataRecord.CpuVertexData.vertexBufferBinding.GetBuffer( 1 ), 1, 1, mVertexDataRecord.CpuVertexData.vertexBufferBinding.GetBuffer( 1 ).Size );
			}
		}

		/// <summary>
		/// 
		/// </summary>
		protected void CreateGpuIndexData()
		{
			for( int lod = 0; lod < mLodLevels.Count; ++lod )
			{
				LodLevel ll = mLodLevels[ lod ];

				if( ll.GpuIndexData == null )
				{
					// clone, using default buffer manager ie hardware
					ll.GpuIndexData = ll.CpuIndexData.Clone();
				}
				mLodLevels[ lod ] = ll;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		protected void DestroyGpuIndexData()
		{
			for( int lod = 0; lod < mLodLevels.Count; ++lod )
			{
				LodLevel ll = mLodLevels[ lod ];
				ll.CpuIndexData = null;
				mLodLevels[ lod ] = ll;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="batchSize"></param>
		/// <param name="destData"></param>
		protected void CreateTriangleStripBuffer( ushort batchSize, IndexData destData )
		{
			/* For even / odd tri strip rows, triangles are this shape:
		    6---7---8
		    | \ | \ |
		    3---4---5
		    | / | / |
		    0---1---2
		    Note how vertex rows count upwards. In order to match up the anti-clockwise
		    winding and this upward transitioning list, we need to start from the
		    right hand side. So we get (2,5,1,4,0,3) etc on even lines (right-left)
		    and (3,6,4,7,5,8) etc on odd lines (left-right). At the turn, we emit the end index 
		    twice, this forms a degenerate triangle, which lets us turn without any artefacts. 
		    So the full list in this simple case is (2,5,1,4,0,3,3,6,4,7,5,8)

		    Skirts are part of the same strip, so after finishing on 8, where sX is
		     the skirt vertex corresponding to main vertex X, we go
		     anticlockwise around the edge, (s8,7,s7,6,s6) to do the top skirt, 
		    then (3,s3,0,s0),(1,s1,2,s2),(5,s5,8,s8) to finish the left, bottom, and
		     right skirts respectively.
		    */

			// to issue a complete row, it takes issuing the upper and lower row
			// and one extra index, which is the degenerate triangle and also turning
			// around the winding
			VertexDataRecord vdr = this.VertextDataRecord;
			int mainIndexPerRow = batchSize * 2 + 1;
			int numRows = batchSize - 1;
			int mainIndexCount = mainIndexPerRow * numRows;
			// skirts share edges, so they take 1 less row per side than batchSize, 
			// but with 2 extra at the end (repeated) to finish the strip
			// * 2 for the vertical line, * 4 for the sides, +2 to finish
			int skirtIndexCount = ( batchSize - 1 ) * 2 * 4 + 2;

			destData.indexStart = 0;
			destData.indexCount = mainIndexCount + skirtIndexCount;
			destData.indexBuffer = HardwareBufferManager.Instance.CreateIndexBuffer( IndexType.Size16, destData.indexCount, BufferUsage.Static, false );

#warning IndexBuffer.Bind(DefaultHardwareIndexBuffer);
			unsafe
			{
				ushort* pI = (ushort*)( destData.indexBuffer.Lock( BufferLocking.Discard ) );
				ushort* basepI = pI;

				// Ratio of the main terrain resolution in relation to this vertex data resolution
				int resolutionRatio = ( mTerrain.Size - 1 ) / ( vdr.Resolution - 1 );
				// At what frequency do we sample the vertex data we're using?
				// mSize is the coverage in terms of the original terrain data (not split to fit in 16-bit)
				int vertexIncrement = ( mSize - 1 ) / ( batchSize - 1 );
				// however, the vertex data we're referencing may not be at the full resolution anyway
				vertexIncrement /= resolutionRatio;
				int rowSize = vdr.Size * vertexIncrement;

				// Start on the right
				ushort currentVertex = (ushort)( ( batchSize - 1 ) * vertexIncrement );
				// but, our quad area might not start at 0 in this vertex data
				// offsets are at main terrain resolution, remember
				ushort columnStart = (ushort)( ( mOffsetX - mNodeWithVertexData.mOffsetX ) / resolutionRatio );
				ushort rowStart = (ushort)( ( mOffsetY - mNodeWithVertexData.mOffsetY ) / resolutionRatio );
				currentVertex += (ushort)( ( rowStart * vdr.Size ) + columnStart );
				bool rightToLeft = true;
				for( ushort r = 0; r < (ushort)numRows; ++r )
				{
					for( ushort c = 0; c < batchSize; ++c )
					{
						*pI++ = currentVertex;
						*pI++ = (ushort)( currentVertex + rowSize );

						// don't increment / decrement at a border, keep this vertex for next
						// row as we 'snake' across the tile
						if( c + 1 < batchSize )
						{
							currentVertex = rightToLeft ?
							                            	(ushort)( currentVertex - vertexIncrement ) : (ushort)( currentVertex + vertexIncrement );
						}
					}
					rightToLeft = !rightToLeft;
					currentVertex += (ushort)rowSize;
					// issue one extra index to turn winding around
					*pI++ = currentVertex;
				}

				// Skirts
				for( ushort s = 0; s < 4; ++s )
				{
					// edgeIncrement is the index offset from one original edge vertex to the next
					// in this row or column. Columns skip based on a row size here
					// skirtIncrement is the index offset from one skirt vertex to the next, 
					// because skirts are packed in rows/cols then there is no row multiplier for
					// processing columns
					int edgeIncrement = 0, skirtIncrement = 0;
					switch( s )
					{
						case 0: //top
							edgeIncrement = -(int)vertexIncrement;
							skirtIncrement = -(int)vertexIncrement;
							break;
						case 1: // left
							edgeIncrement = -(int)rowSize;
							skirtIncrement = -(int)vertexIncrement;
							break;
						case 2: //bottom
							edgeIncrement = (int)vertexIncrement;
							skirtIncrement = (int)vertexIncrement;
							break;
						case 3: //right
							edgeIncrement = (int)rowSize;
							skirtIncrement = (int)vertexIncrement;
							break;
					}
					// Skirts are stored in contiguous rows / columns (rows 0/2, cols 1/3)
					ushort skirtIndex = CalcSkirtVertexIndex( currentVertex, ( s % 2 ) != 0 );

					for( ushort c = 0; c < (ushort)( batchSize - 1 ); ++c )
					{
						*pI++ = currentVertex;
						*pI++ = skirtIndex;

						currentVertex += (ushort)edgeIncrement;
						skirtIndex += (ushort)skirtIncrement;
					}
					if( s == 3 )
					{
						// we issue an extra 2 indices to finish the skirt off
						*pI++ = currentVertex;
						*pI++ = skirtIndex;
						currentVertex += (ushort)edgeIncrement;
						skirtIndex += (ushort)skirtIncrement;
					}
				} //end for
				ushort val = (ushort)( pI - basepI );
				//Debug.Assert(val == (ushort)destData.indexCount, "wrong indices");
			} //end unsafe
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="mainIndex"></param>
		/// <param name="isCol"></param>
		/// <returns></returns>
		protected ushort CalcSkirtVertexIndex( ushort mainIndex, bool isCol )
		{
			VertexDataRecord vdr = this.VertextDataRecord;
			// row / col in main vertex resolution
			ushort row = (ushort)( mainIndex / vdr.Size );
			ushort col = (ushort)( mainIndex % vdr.Size );

			// skrits are after main vertices, so skip them
			ushort ubase = (ushort)( vdr.Size * vdr.Size );

			// The layout in vertex data is:
			// 1. row skirts
			//    numSkirtRowsCols rows of resolution vertices each
			// 2. column skirts
			//    numSkirtRowsCols cols of resolution vertices each

			// No offsets used here, this is an index into the current vertex data, 
			// which is already relative
			if( isCol )
			{
				ushort skirtNum = (ushort)( col / vdr.SkirtRowColSkip );
				ushort colBase = (ushort)( vdr.NumSkirtRowsCols * vdr.Size );
				return (ushort)( ubase + colBase + vdr.Size * skirtNum + row );
			}
			else
			{
				ushort skirtNum = (ushort)( row / vdr.SkirtRowColSkip );
				return (ushort)( ubase + vdr.Size * skirtNum + col );
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="queue"></param>
		public void UpdateRenderQueue( RenderQueue queue )
		{
			if( IsRenderedAtCurrentLod )
			{
				queue.AddRenderable( mRend, mTerrain.RenderQueueGroupID );
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="debugRenderables"></param>
		public void VisitRenderables( bool debugRenderables )
		{
#warning: implement VisitRenderables
			throw new NotImplementedException();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="op"></param>
		public RenderOperation RenderOperation
		{
			get
			{
				Graphics.RenderOperation op = new RenderOperation();
				mNodeWithVertexData.UpdateGpuVertexData();
				op.indexData = mLodLevels[ mCurrentLod ].CpuIndexData;
				op.operationType = OperationType.TriangleStrip;
				op.useIndices = true;
				op.vertexData = this.VertextDataRecord.GpuVertexData;

				return op;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="xform"></param>
		public void GetWorldTransforms( Matrix4[] xform )
		{
			xform[ 0 ] = mNodeWithVertexData.mRend.ParentNodeFullTransform;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="cam"></param>
		/// <returns></returns>
		public float GetSquaredViewDepth( Camera cam )
		{
			return mRend.ParentSceneNode.GetSquaredViewDepth( cam ); //mMovable.ParentSceneNode.GetSquaredViewDepth(cam);
		}

		/// <summary>
		/// 
		/// </summary>
		public bool CastsShadows { get { return TerrainGlobalOptions.CastsDynamicShadows; } }

#if false
        #region - implementation of movable -
        /// <summary>
        /// 
        /// </summary>
        protected class Movable : SimpleRenderable,IDisposable
        {
            #region - fields -
            /// <summary>
            /// 
            /// </summary>
            protected TerrainQuadTreeNode mParent;
            #endregion

            #region - properties -
            /// <summary>
            /// 
            /// </summary>
            public override AxisAlignedBox BoundingBox
            {
                get { return mParent.AABB; }
            }
            /// <summary>
            /// 
            /// </summary>
            public override string MovableType
            {
                get
                {
                    return "AxiomTerrainNodeMovable";
                }
                set
                {
                    base.MovableType = value;
                }
            }
            /// <summary>
            /// 
            /// </summary>
            public override float BoundingRadius
            {
                get { return mParent.BoundingRadius; }
            }
            /// <summary>
            /// 
            /// </summary>
            public override bool IsVisible
            {
                get
                {
                    if (mParent.CurentLod == -1)
                        return false;
                    else
                        return base.IsVisible;
                }
                set
                {
                    base.IsVisible = value;
                }
            }
            #endregion
            protected Rend mRend;
            #region - constructor -
            /// <summary>
            /// 
            /// </summary>
            /// <param name="parent"></param>
            public Movable(TerrainQuadTreeNode parent, Rend renderable)
            {
                mParent = parent;
                mRend = renderable;
            }
            #endregion

            public override void GetRenderOperation(RenderOperation op)
            {
                mRend.GetRenderOperation(op);
            }
            public override void GetRenderOperation(ref RenderOperation op)
            {
                mRend.GetRenderOperation(op);
            }
            public override float GetSquaredViewDepth(Camera camera)
            {
                return mRend.GetSquaredViewDepth(camera);
            }
            #region - functions -
            public void Dispose()
            {
            }
#if true
            /// <summary>
            /// 
            /// </summary>
            /// <param name="camera"></param>
            public override void NotifyCurrentCamera(Camera camera)
            {

            }
#endif
            /// <summary>
            /// 
            /// </summary>
            /// <param name="queue"></param>
            public override void UpdateRenderQueue(RenderQueue queue)
            {
                mParent.UpdateRenderQueue(queue);
            }
            #endregion
        }
        #endregion

        #region - Renderable -
        /// <summary>
        /// 
        /// </summary>
        protected class Rend : IRenderable, IDisposable
        {
            /// <summary>
            /// 
            /// </summary>
            protected Dictionary<int, Vector4> mCustomParameters = new Dictionary<int, Vector4>();
            /// <summary>
            /// 
            /// </summary>
            protected TerrainQuadTreeNode mParent;
            /// <summary>
            /// 
            /// </summary>
            protected bool mPolygoneModeOverridable = true;
            /// <summary>
            /// 
            /// </summary>
            protected bool mUseIdentityProjection;
            /// <summary>
            /// 
            /// </summary>
            protected bool mUseIdentityView;
            /// <summary>
            /// 
            /// </summary>
            public bool CastsShadows
            {
                get { return mParent.CastsShadows; }
            }
            /// <summary>
            /// Retrieves a weak reference to the material this renderable object uses.
            /// </summary>
            public Material Material
            {
                get { return mParent.Material; }
            }
            /// <summary>
            /// Retrieves a pointer to the Material Technique this renderable object uses.
            /// </summary>
            public Technique Technique
            {
                get { return mParent.Technique; }
            }
            /// <summary>
            /// Gets a list of lights, ordered relative to how close they are to this renderable.
            /// </summary>
            public LightList Lights
            {
                get { return mParent.Lights; }
            }
            /// <summary>
            /// 
            /// </summary>
            public bool NormalizeNormals
            {
                get { return false; }
            }
            /// <summary>
            /// Returns the number of world transform matrices this renderable requires.
            /// </summary>
            public ushort NumWorldTransforms
            {
                get { return 1; }
            }
            /// <summary>
            /// 
            /// </summary>
            public bool UseIdentityProjection
            {
                get { return mUseIdentityProjection; }
            }
            /// <summary>
            /// Get's whether or not to use an 'identity' view.
            /// </summary>
            public bool UseIdentityView
            {
                get { return mUseIdentityView; }
            }
            /// <summary>
            /// 
            /// </summary>
            public bool PolygonModeOverrideable
            {
                get { return mPolygoneModeOverridable; }
            }
            /// <summary>
            /// 
            /// </summary>
            public Quaternion WorldOrientation
            {
                get { return mParent.mMovable.ParentNode.DerivedOrientation; }
            }
            /// <summary>
            /// 
            /// </summary>
            public Vector3 WorldPosition
            {
                get { return mParent.mMovable.ParentNode.DerivedPosition; }
            }
            /// <summary>
            /// 
            /// </summary>
            /// <param name="parent"></param>
            public Rend(TerrainQuadTreeNode parent)
            {
                mParent = parent;
                
            }
            public void Dispose()
            {
                //
            }
         
            /// <summary>
            /// 
            /// </summary>
            /// <param name="transforms"></param>
            public void GetWorldTransforms(Matrix4[] transforms)
            {
                mParent.GetWorldTransforms(transforms);
            }
            /// <summary>
            /// 
            /// </summary>
            /// <param name="op"></param>
            public void GetRenderOperation(RenderOperation op)
            {
                mParent.GetRenderOperation(op);
            }
            /// <summary>
            /// 
            /// </summary>
            /// <param name="camera"></param>
            public float GetSquaredViewDepth(Camera camera)
            {
                return mParent.GetSquaredViewDepth(camera);
            }
            /// <summary>
            /// 
            /// </summary>
            /// <param name="parameter"></param>
            public Vector4 GetCustomParameter(int parameter)
            {
                Vector4 retVal;
                if (mCustomParameters.TryGetValue(parameter, out retVal))
                    return retVal;
                else
                {
                    throw new Exception("Parameter at the given index was not found!\n" +
                        "Renderable.GetCustomParameter");
                }
            }
            /// <summary>
            /// 
            /// </summary>
            /// <param name="parameter"></param>
            /// <param name="value"></param>
            public void SetCustomParameter(int parameter, Vector4 value)
            {
                mCustomParameters[parameter] = value;
            }
            /// <summary>
            /// Update a custom GpuProgramParameters constant which is derived from
            /// information only this Renderable knows.
            /// </summary>
            /// <param name="entry"></param>
            /// <param name="parameters"></param>
            public void UpdateCustomGpuParameter(GpuProgramParameters.AutoConstantEntry entry,
                GpuProgramParameters parameters)
            {
               //not implement now
            }
        }
        #endregion

#endif

		public class TerrainRendable : SimpleRenderable
		{
			private TerrainQuadTreeNode mParent;

			public TerrainRendable( TerrainQuadTreeNode parent )
			{
				mParent = parent;
				this.MovableType = "AxiomTerrainNodeMovable";
			}

			public override Technique Technique { get { return mParent.Technique; } }
			public override AxisAlignedBox BoundingBox { get { return mParent.AABB; } }

			/// <summary>
			/// 
			/// </summary>
			public override Material Material { get { return mParent.Material; } set { base.Material = value; } }

			public override float BoundingRadius { get { return mParent.BoundingRadius; } }

			public override bool IsVisible
			{
				get
				{
					if( mParent.CurentLod == -1 )
					{
						return false;
					}

					return base.IsVisible;
				}
				set { base.IsVisible = value; }
			}

			public override bool CastShadows { get { return mParent.CastsShadows; } set { base.CastShadows = value; } }

			public override void GetWorldTransforms( Matrix4[] matrices )
			{
				mParent.GetWorldTransforms( matrices );
			}

			public override void UpdateRenderQueue( RenderQueue queue )
			{
				mParent.UpdateRenderQueue( queue );
			}

			public override float GetSquaredViewDepth( Camera camera )
			{
				return mParent.GetSquaredViewDepth( camera );
			}

			public override RenderOperation RenderOperation { get { return mParent.RenderOperation; } }
		}
	}
}
