#region MIT/X11 License

//Copyright © 2003-2012 Axiom 3D Rendering Engine Project
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

using Axiom.Core;
using Axiom.Core.Collections;
using Axiom.CrossPlatform;
using Axiom.Graphics;
using Axiom.Math;
using Axiom.Serialization;

#endregion Namespace Declarations

namespace Axiom.Components.Terrain
{
	[OgreVersion( 1, 7, 2 )]
	public struct LodLevel
	{
		/// <summary>
		/// Number of vertices rendered down one side (not including skirts)
		/// </summary>
		public ushort BatchSize;

		/// <summary>
		/// Temp calc area for max height delta
		/// </summary>
		public Real CalcMaxHeightDelta;

		/// <summary>
		/// Index data on the gpu
		/// </summary>
		public IndexData GpuIndexData;

		/// <summary>
		/// The cFactor value used to calculate transitionDist
		/// </summary>
		public Real LastCFactor;

		/// <summary>
		/// The most recently calculated transition distance
		/// </summary>
		public Real LastTransitionDist;

		/// <summary>
		/// Maximum delta height between this and the next lower lod
		/// </summary>
		public Real MaxHeightDelta;
	}

	[OgreVersion( 1, 7, 2 )]
	public class VertexDataRecord
	{
		public VertexData CpuVertexData;
		public VertexData GpuVertexData;

		/// <summary>
		/// Is the GPU vertex data out of date?
		/// </summary>
		public bool IsGpuVertexDataDirty;

		/// <summary>
		/// Number of rows and columns of skirts
		/// </summary>
		public ushort NumSkirtRowsCols;

		/// <summary>
		/// resolution of the data compared to the base terrain data (NOT number of vertices!)
		/// </summary>
		public ushort Resolution;

		/// <summary>
		/// size of the data along one edge
		/// </summary>
		public ushort Size;

		/// <summary>
		/// The number of rows / cols to skip in between skirts
		/// </summary>
		public ushort SkirtRowColSkip;

		/// <summary>
		/// Number of quadtree levels (including this one) this data applies to
		/// </summary>
		public ushort TreeLevels;

		public VertexDataRecord() { }

		[OgreVersion( 1, 7, 2 )]
		public VertexDataRecord( ushort res, ushort sz, ushort lvls )
		{
			this.CpuVertexData = null;
			this.GpuVertexData = null;
			this.Resolution = res;
			this.Size = sz;
			this.TreeLevels = lvls;
			this.IsGpuVertexDataDirty = false;
			this.NumSkirtRowsCols = 0;
			this.SkirtRowColSkip = 0;
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
	public class TerrainQuadTreeNode : DisposableObject
	{
		/// <summary>
		/// Buffer binding used for holding positions.
		/// </summary>
		public static short POSITION_BUFFER;

		/// <summary>
		/// Buffer binding used for holding delta values
		/// </summary>
		public static short DELTA_BUFFER = 1;

		/// <summary>
		/// relative to mLocalCentre
		/// </summary>
		protected AxisAlignedBox mAABB;

		protected ushort mBaseLod;

		protected ushort mBoundaryX;
		protected ushort mBoundaryY;

		/// <summary>
		/// relative to mLocalCentre
		/// </summary>
		protected Real mBoundingRadius;

		/// <summary>
		/// The child with the largest height delta 
		/// </summary>
		protected TerrainQuadTreeNode mChildWithMaxHeightDelta;

		protected TerrainQuadTreeNode[] mChildren = new TerrainQuadTreeNode[ 4 ];

		/// <summary>
		/// -1 = none (do not render)
		/// </summary>
		protected int mCurrentLod;

		protected ushort mDepth;

		/// <summary>
		/// relative to terrain centre
		/// </summary>
		protected Vector3 mLocalCentre;

		protected SceneNode mLocalNode;
		protected List<LodLevel> mLodLevels = new List<LodLevel>();

		/// <summary>
		/// // 0-1 transition to lower LOD
		/// </summary>
		protected float mLodTransition;

		protected ushort mMaterialLodIndex;
		protected Movable mMovable;
		protected TerrainQuadTreeNode mNodeWithVertexData;
		protected ushort mOffsetX;
		protected ushort mOffsetY;
		protected TerrainQuadTreeNode mParent;
		protected ushort mQuadrant;
		protected Rend mRend;
		protected bool mSelfOrChildRendered;
		protected ushort mSize;
		protected Terrain mTerrain;
		protected VertexDataRecord mVertexDataRecord;

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
		[OgreVersion( 1, 7, 2 )]
		public TerrainQuadTreeNode( Terrain terrain, TerrainQuadTreeNode parent, ushort xOff, ushort yOff, ushort size, ushort lod, ushort depth, ushort quadrant )
		{
			this.mTerrain = terrain;
			this.mParent = parent;
			this.mOffsetX = xOff;
			this.mOffsetY = yOff;
			this.mBoundaryX = (ushort)( xOff + size );
			this.mBoundaryY = (ushort)( yOff + size );
			this.mSize = size;
			this.mBaseLod = lod;
			this.mDepth = depth;
			this.mQuadrant = quadrant;
			this.mBoundingRadius = 0;
			this.mCurrentLod = -1;
			this.mMaterialLodIndex = 0;
			this.mLodTransition = 0;
			this.mChildWithMaxHeightDelta = null;
			this.mSelfOrChildRendered = false;
			this.mNodeWithVertexData = null;
			this.mAABB = new AxisAlignedBox();
			if ( this.mTerrain.MaxBatchSize < size )
			{
				var childSize = (ushort)( ( ( size - 1 ) * 0.5f ) + 1 );
				var childOff = (ushort)( childSize - 1 );
				var childLod = (ushort)( lod - 1 ); // LOD levels decrease down the tree (higher detail)
				var childDepth = (ushort)( depth + 1 );
				// create children
				this.mChildren[ 0 ] = new TerrainQuadTreeNode( this.mTerrain, this, xOff, yOff, childSize, childLod, childDepth, 0 );
				this.mChildren[ 1 ] = new TerrainQuadTreeNode( this.mTerrain, this, (ushort)( xOff + childOff ), yOff, childSize, childLod, childDepth, 1 );
				this.mChildren[ 2 ] = new TerrainQuadTreeNode( this.mTerrain, this, xOff, (ushort)( yOff + childOff ), childSize, childLod, childDepth, 2 );
				this.mChildren[ 3 ] = new TerrainQuadTreeNode( this.mTerrain, this, (ushort)( xOff + childOff ), (ushort)( yOff + childOff ), childSize, childLod, childDepth, 3 );

				var ll = new LodLevel();
				// non-leaf nodes always render with minBatchSize vertices
				ll.BatchSize = this.mTerrain.MinBatchSize;
				ll.MaxHeightDelta = 0;
				ll.CalcMaxHeightDelta = 0;
				this.mLodLevels.Add( ll );
			}
			else
			{
				//no children
				Array.Clear( this.mChildren, 0, this.mChildren.Length );
				// this is a leaf node and may have internal LODs of its own
				ushort ownLod = this.mTerrain.NumLodLevelsPerLeaf;

				Debug.Assert( lod == ( ownLod - 1 ), "The lod passed in should reflect the number of lods in a leaf" );
				// leaf nodes always have a base LOD of 0, because they're always handling
				// the highest level of detail
				this.mBaseLod = 0;
				ushort sz = this.mTerrain.MaxBatchSize;

				while ( ownLod-- != 0 )
				{
					var ll = new LodLevel();
					ll.BatchSize = sz;
					ll.MaxHeightDelta = 0;
					ll.CalcMaxHeightDelta = 0;
					this.mLodLevels.Add( ll );
					if ( ownLod != 0 )
					{
						sz = (ushort)( ( ( sz - 1 ) * 0.5 ) + 1 );
					}
				}
				Debug.Assert( sz == this.mTerrain.MinBatchSize );
			}

			// local centre calculation
			// because of pow2 +1 there is always a middle point
			var midoffset = (ushort)( ( size - 1 ) / 2 );
			var midpointX = (ushort)( this.mOffsetX + midoffset );
			var midpointY = (ushort)( this.mOffsetY + midoffset );

			//derive the local centry, but give it a height if 0
			//TODO: - what if we actually centred this at the terrain height at this point?
			//would this be better?
			this.mTerrain.GetPoint( midpointX, midpointY, 0, ref this.mLocalCentre );
			this.mMovable = new Movable( this );
			this.mRend = new Rend( this );
		}

		public VertexDataRecord VertextDataRecord
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return ( this.mNodeWithVertexData != null ) ? this.mNodeWithVertexData.mVertexDataRecord : null;
			}
		}

		/// <summary>
		/// Get the horizontal offset into the main terrain data of this node
		/// </summary>
		public int XOffeset
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return this.mOffsetX;
			}
		}

		/// <summary>
		/// Get the vertical offset into the main terrain data of this node
		/// </summary>
		public int YOffset
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return this.mOffsetY;
			}
		}

		/// <summary>
		/// Get the base LOD level this node starts at (the highest LOD it handles)
		/// </summary>
		public int BaseLod
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return this.mBaseLod;
			}
		}

		/// <summary>
		/// Get the number of LOD levels this node can represent itself (only > 1 for leaf nodes)
		/// </summary>
		public ushort LodCount
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return (ushort)this.mLodLevels.Count;
			}
		}

		/// <summary>
		/// Get ultimate parent terrain
		/// </summary>
		public Terrain Terrain
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return this.mTerrain;
			}
		}

		/// <summary>
		/// Get parent node
		/// </summary>
		public TerrainQuadTreeNode Parent
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return this.mParent;
			}
		}

		/// <summary>
		/// Is this a leaf node (no children)
		/// </summary>
		public bool IsLeaf
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return this.mChildren[ 0 ] == null;
			}
		}

		/// <summary>
		/// Get the AABB (local coords) of this node
		/// </summary>
		public AxisAlignedBox AABB
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return this.mAABB;
			}
		}

		/// <summary>
		/// Get the bounding radius of this node
		/// </summary>
		public Real BoundingRadius
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return this.mBoundingRadius;
			}
		}

		/// <summary>
		/// Get the minimum height of the node
		/// </summary>
		public Real MinHeight
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				switch ( this.mTerrain.Alignment )
				{
					case Alignment.Align_X_Y:
					default:
						return this.mAABB.Minimum.z;
					case Alignment.Align_X_Z:
						return this.mAABB.Minimum.y;
					case Alignment.Align_Y_Z:
						return this.mAABB.Minimum.x;
				}
			}
		}

		/// <summary>
		/// Get the maximum height of the node
		/// </summary>
		public Real MaxHeight
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				switch ( this.mTerrain.Alignment )
				{
					case Alignment.Align_X_Y:
					default:
						return this.mAABB.Maximum.z;
					case Alignment.Align_X_Z:
						return this.mAABB.Maximum.y;
					case Alignment.Align_Y_Z:
						return this.mAABB.Maximum.x;
				}
			}
		}

		/// <summary>
		/// Get the current LOD index (only valid after calculateCurrentLod)
		/// </summary>
		public int CurrentLod
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return this.mCurrentLod;
			}

			[OgreVersion( 1, 7, 2 )]
			set
			{
				this.mCurrentLod = value;
				this.mRend.SetCustomParameter( Terrain.LOD_MORPH_CUSTOM_PARAM, new Vector4( this.mLodTransition, this.mCurrentLod + this.mBaseLod + 1, 0, 0 ) );
			}
		}

		/// <summary>
		/// Returns whether this node is rendering itself at the current LOD level
		/// </summary>
		public bool IsRenderedAtCurrentLod
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return this.mCurrentLod != -1;
			}
		}

		/// <summary>
		/// Returns whether this node or its children are being rendered at the current LOD level
		/// </summary>
		public bool IsSelfOrChildrenRenderedAtCurrentLod
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return this.mSelfOrChildRendered;
			}
		}

		/// <summary>
		/// Get the transition state between the current LOD and the next lower one (only valid after calculateCurrentLod)
		/// </summary>
		public float LodTransition
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return this.mLodTransition;
			}

			[OgreVersion( 1, 7, 2 )]
			set
			{
				this.mLodTransition = value;
				this.mRend.SetCustomParameter( Terrain.LOD_MORPH_CUSTOM_PARAM, new Vector4( this.mLodTransition, this.mCurrentLod + this.mBaseLod + 1, 0, 0 ) );
			}
		}

		/// <summary>
		/// Get the local centre of this node, relative to parent terrain centre
		/// </summary>
		public Vector3 LocalCentre
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return this.mLocalCentre;
			}
		}

		public Material Material
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return this.mTerrain.Material;
			}
		}

		public Technique Technique
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return this.mTerrain.Material.GetBestTechnique( this.mMaterialLodIndex, this.mRend );
			}
		}

		public LightList Lights
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return this.mMovable.QueryLights();
			}
		}

		public RenderOperation RenderOperation
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				var op = new RenderOperation();
				this.mNodeWithVertexData.UpdateGpuVertexData();
				op.indexData = this.mLodLevels[ this.mCurrentLod ].GpuIndexData;
				op.operationType = OperationType.TriangleStrip;
				op.useIndices = true;
				op.vertexData = VertextDataRecord.GpuVertexData;

				return op;
			}
		}

		public bool CastsShadows
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return TerrainGlobalOptions.CastsDynamicShadows;
			}
		}

		[OgreVersion( 1, 7, 2, "~TerrainQuadTreeNode" )]
		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				if ( disposeManagedResources )
				{
					if ( this.mMovable != null )
					{
						if ( !this.mMovable.IsDisposed )
						{
							this.mMovable.Dispose();
						}

						this.mMovable = null;
					}

					if ( this.mRend != null )
					{
						this.mRend = null;
					}

					if ( this.mLocalNode != null )
					{
						this.mTerrain.RootSceneNode.RemoveAndDestroyChild( this.mLocalNode.Name );
						this.mLocalNode = null;
					}

					for ( int i = 0; i < this.mChildren.Length; i++ )
					{
						if ( this.mChildren[ i ] != null )
						{
							this.mChildren[ i ].Dispose();
						}
					}

					DestroyCpuVertexData();
					DestroyGpuVertexData();
					DestroyGpuIndexData();

					if ( this.mLodLevels != null )
					{
						this.mLodLevels.Clear();
						this.mLodLevels = null;
					}

					this.mVertexDataRecord = null;
				}
			}

			base.dispose( disposeManagedResources );
		}

		/// <summary>
		/// Get child node
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		[OgreVersion( 1, 7, 2 )]
		public TerrainQuadTreeNode GetChild( ushort child )
		{
			if ( IsLeaf || child >= 4 )
			{
				return null;
			}

			return this.mChildren[ child ];
		}

		/// <summary>
		/// Prepare node and children (perform CPU tasks, may be background thread)
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public void Prepare()
		{
			if ( IsLeaf )
			{
				return;
			}

			for ( int i = 0; i < this.mChildren.Length; i++ )
			{
				this.mChildren[ i ].Prepare();
			}
		}

		/// <summary>
		/// Prepare node from a stream
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public void Prepare( StreamSerializer stream )
		{
			// load LOD data we need
			for ( int i = 0; i < this.mLodLevels.Count; ++i )
			{
				LodLevel ll = this.mLodLevels[ i ];
				// only read 'calc' and then copy to final (separation is only for
				// real-time calculation
				// Basically this is what finaliseHeightDeltas does in calc path
				stream.Read( out ll.CalcMaxHeightDelta );
				ll.MaxHeightDelta = ll.CalcMaxHeightDelta;
				ll.LastCFactor = 0;
			}

			if ( !IsLeaf )
			{
				for ( int i = 0; i < 4; ++i )
				{
					this.mChildren[ i ].Prepare( stream );
				}
			}

			// If this is the root, do the post delta calc to finish
			if ( this.mParent == null )
			{
				var rect = new Rectangle();
				rect.Top = this.mOffsetY;
				rect.Bottom = this.mBoundaryY;
				rect.Left = this.mOffsetX;
				rect.Right = this.mBoundaryX;
				PostDeltaCalculation( rect );
			}
		}

		/// <summary>
		/// Save node to a stream
		/// </summary>
		/// <param name="stream"></param>
		[OgreVersion( 1, 7, 2 )]
		public void Save( StreamSerializer stream )
		{
			// save LOD data we need
			foreach ( LodLevel ll in this.mLodLevels )
			{
				stream.Write( ll.MaxHeightDelta );
			}

			if ( !IsLeaf )
			{
				for ( int i = 0; i < 4; ++i )
				{
					this.mChildren[ i ].Save( stream );
				}
			}
		}

		/// <summary>
		///  Load node and children (perform GPU tasks, will be render thread)
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public void Load()
		{
			CreateGpuVertexData();
			CreateGpuIndexData();

			if ( !IsLeaf )
			{
				for ( int i = 0; i < 4; i++ )
				{
					this.mChildren[ i ].Load();
				}
			}

			if ( this.mLocalNode == null )
			{
				this.mLocalNode = this.mTerrain.RootSceneNode.CreateChildSceneNode( this.mLocalCentre );
			}

			this.mLocalNode.AttachObject( this.mMovable );
		}

		/// <summary>
		/// Unload node and children (perform GPU tasks, will be render thread)
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public void Unload()
		{
			if ( !IsLeaf )
			{
				for ( int i = 0; i < 4; i++ )
				{
					this.mChildren[ i ].Unload();
				}
			}

			DestroyGpuVertexData();

			if ( this.mMovable.IsAttached )
			{
				this.mLocalNode.DetachObject( this.mMovable );
			}
		}

		/// <summary>
		/// Unprepare node and children (perform CPU tasks, may be background thread)
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public void Unprepare()
		{
			if ( !IsLeaf )
			{
				for ( int i = 0; i < 4; i++ )
				{
					this.mChildren[ i ].Unprepare();
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
		[OgreVersion( 1, 7, 2 )]
		public LodLevel GetLodLevel( ushort lod )
		{
			Debug.Assert( lod < this.mLodLevels.Count );

			return this.mLodLevels[ lod ];
		}

		/// <summary>
		///  Notify the node (and children) that deltas are going to be calculated for a given range.
		/// </summary>
		/// <param name="rect"></param>
		[OgreVersion( 1, 7, 2 )]
		public void PreDeltaCalculation( Rectangle rect )
		{
			if ( rect.Left <= this.mBoundaryX || rect.Right > this.mOffsetX || rect.Top <= this.mBoundaryY || rect.Bottom > this.mOffsetY )
			{
				// relevant to this node (overlaps)

				// if the rect covers the whole node, reset the max height
				// this means that if you recalculate the deltas progressively, end up keeping
				// a max height that's no longer the case (ie more conservative lod), 
				// but that's the price for not recaculating the whole node. If a 
				// complete recalculation is required, just dirty the entire node. (or terrain)

				// Note we use the 'calc' field here to avoid interfering with any
				// ongoing LOD calculations (this can be in the background)

				if ( rect.Left <= this.mOffsetX && rect.Right > this.mBoundaryX && rect.Top <= this.mOffsetY && rect.Bottom > this.mBoundaryY )
				{
					for ( int i = 0; i < this.mLodLevels.Count; i++ )
					{
						LodLevel tmp = this.mLodLevels[ i ];
						tmp.CalcMaxHeightDelta = 0.0f;
					}
				}

				//pass on to children
				if ( !IsLeaf )
				{
					for ( int i = 0; i < 4; i++ )
					{
						this.mChildren[ i ].PreDeltaCalculation( rect );
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
		[OgreVersion( 1, 7, 2 )]
		public void NotifyDelta( ushort x, ushort y, ushort lod, float delta )
		{
			if ( x >= this.mOffsetX && x < this.mBoundaryX && y >= this.mOffsetY && y < this.mBoundaryY )
			{
				// within our bounds, check it's our LOD level
				if ( lod >= this.mBaseLod && lod < this.mBaseLod + (ushort)( this.mLodLevels.Count ) )
				{
					int iter = 0;
					iter += lod - this.mBaseLod;
					LodLevel tmp = this.mLodLevels[ iter ];
					tmp.CalcMaxHeightDelta = System.Math.Max( tmp.CalcMaxHeightDelta, delta );
				}

				// pass to the children
				if ( !IsLeaf )
				{
					for ( int i = 0; i < 4; i++ )
					{
						this.mChildren[ i ].NotifyDelta( x, y, lod, delta );
					}
				}
			}
		}

		/// <summary>
		/// Notify the node (and children) that deltas are going to be calculated for a given range.
		/// </summary>
		/// <param name="rect"></param>
		[OgreVersion( 1, 7, 2 )]
		public void PostDeltaCalculation( Rectangle rect )
		{
			if ( rect.Left <= this.mBoundaryX || rect.Right > this.mOffsetX || rect.Top <= this.mBoundaryY || rect.Bottom > this.mOffsetY )
			{
				// relevant to this node (overlaps)

				// each non-leaf node should know which of its children transitions
				// to the lower LOD level last, because this is the one which controls
				// when the parent takes over
				if ( !IsLeaf )
				{
					float maxChildDelta = -1;
					TerrainQuadTreeNode childWithMaxHeightDelta = null;
					for ( int i = 0; i < 4; i++ )
					{
						TerrainQuadTreeNode child = this.mChildren[ i ];
						child.PostDeltaCalculation( rect );
						float childData = child.GetLodLevel( (ushort)( child.LodCount - 1 ) ).CalcMaxHeightDelta;

						if ( childData > maxChildDelta )
						{
							childWithMaxHeightDelta = child;
							maxChildDelta = childData;
						}
					}

					// make sure that our highest delta value is greater than all children's
					// otherwise we could have some crossover problems
					// for a non-leaf, there is only one LOD level
					LodLevel tmp = this.mLodLevels[ 0 ];
					tmp.CalcMaxHeightDelta = System.Math.Max( tmp.CalcMaxHeightDelta, maxChildDelta * (Real)1.05 );
					this.mChildWithMaxHeightDelta = childWithMaxHeightDelta;
				}
				else
				{
					// make sure own LOD levels delta values ascend
					for ( int i = 0; i < this.mLodLevels.Count - 1; i++ )
					{
						// the next LOD after this one should have a higher delta
						// otherwise it won't come into affect further back like it should!
						LodLevel tmp = this.mLodLevels[ i ];
						LodLevel tmpPlus = this.mLodLevels[ i + 1 ];
						tmpPlus.CalcMaxHeightDelta = System.Math.Max( tmpPlus.CalcMaxHeightDelta, tmp.CalcMaxHeightDelta * 1.05 );
					}
				}
			}
		}

		/// <summary>
		/// Promote the delta values calculated to the runtime ones (this must
		///	be called in the main thread). 
		/// </summary>
		/// <param name="rect"></param>
		[OgreVersion( 1, 7, 2 )]
		public void FinaliseDeltaValues( Rectangle rect )
		{
			if ( rect.Left <= this.mBoundaryX || rect.Right > this.mOffsetX || rect.Top <= this.mBoundaryY || rect.Bottom > this.mOffsetY )
			{
				// relevant to this node (overlaps)

				// Children
				if ( !IsLeaf )
				{
					for ( int i = 0; i < 4; i++ )
					{
						TerrainQuadTreeNode child = this.mChildren[ i ];
						child.FinaliseDeltaValues( rect );
					}
				}

				// self
				LodLevel[] lvls = this.mLodLevels.ToArray();
				for ( int i = 0; i < lvls.Length; i++ )
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
		[OgreVersion( 1, 7, 2 )]
		public void AssignVertexData( ushort treeDepthStart, ushort treeDepthEnd, ushort resolution, ushort sz )
		{
			Debug.Assert( treeDepthStart >= this.mDepth, "Should not be calling this" );

			if ( this.mDepth == treeDepthStart )
			{
				//we own this vertex data
				this.mNodeWithVertexData = this;
				this.mVertexDataRecord = new VertexDataRecord( resolution, sz, (ushort)( treeDepthEnd - treeDepthStart ) );

				CreateCpuVertexData();

				//pass on to children
				if ( !IsLeaf && treeDepthEnd > ( this.mDepth + 1 ) ) // treeDepthEnd is exclusive, and this is children
				{
					for ( int i = 0; i < 4; ++i )
					{
						this.mChildren[ i ].UseAncestorVertexData( this, treeDepthEnd, resolution );
					}
				}
			}
			else
			{
				Debug.Assert( !IsLeaf, "No more levels below this!" );

				for ( int i = 0; i < 4; ++i )
				{
					this.mChildren[ i ].AssignVertexData( treeDepthStart, treeDepthEnd, resolution, sz );
				}
			}
		}

		/// <summary>
		/// Tell a node that it should use an anscestor's vertex data.
		/// </summary>
		/// <param name="owner"></param>
		/// <param name="treeDepthEnd">The end of the depth that should use this data (exclusive)</param>
		/// <param name="resolution">The resolution of the data to use</param>
		[OgreVersion( 1, 7, 2 )]
		public void UseAncestorVertexData( TerrainQuadTreeNode owner, int treeDepthEnd, int resolution )
		{
			this.mNodeWithVertexData = owner;
			this.mVertexDataRecord = null;

			if ( !IsLeaf && treeDepthEnd > ( this.mDepth + 1 ) ) // treeDepthEnd is exclusive, and this is children
			{
				for ( int i = 0; i < 4; i++ )
				{
					this.mChildren[ i ].UseAncestorVertexData( owner, treeDepthEnd, resolution );
				}
			}
		}

		/// <summary>
		/// Tell the node to update its vertex data for a given region. 
		/// </summary>
		/// <param name="positions"></param>
		/// <param name="deltas"></param>
		/// <param name="rect"></param>
		/// <param name="cpuData"></param>
		[OgreVersion( 1, 7, 2 )]
		public void UpdateVertexData( bool positions, bool deltas, Rectangle rect, bool cpuData )
		{
			if ( rect.Left <= this.mBoundaryX || rect.Right > this.mOffsetX || rect.Top <= this.mBoundaryY || rect.Bottom > this.mOffsetY )
			{
				// Do we have vertex data?
				if ( this.mVertexDataRecord != null )
				{
					// Trim to our bounds
					var updateRect = new Rectangle( this.mOffsetX, this.mOffsetY, this.mBoundaryX, this.mBoundaryY );
					updateRect.Left = System.Math.Max( updateRect.Left, rect.Left );
					updateRect.Right = System.Math.Min( updateRect.Right, rect.Right );
					updateRect.Top = System.Math.Max( updateRect.Top, rect.Top );
					updateRect.Bottom = System.Math.Min( updateRect.Bottom, rect.Bottom );

					// update the GPU buffer directly
#warning TODO: do we have no use for CPU vertex data after initial load?
					// if so, destroy it to free RAM, this should be fast enough to 
					// to direct

					HardwareVertexBuffer posbuf = null, deltabuf = null;
					VertexData targetData = cpuData ? this.mVertexDataRecord.CpuVertexData : this.mVertexDataRecord.GpuVertexData;

					if ( positions )
					{
						posbuf = targetData.vertexBufferBinding.GetBuffer( POSITION_BUFFER );
					}
					if ( deltas )
					{
						deltabuf = targetData.vertexBufferBinding.GetBuffer( DELTA_BUFFER );
					}

					UpdateVertexBuffer( posbuf, deltabuf, updateRect );
				}

				// pass on to children
				if ( !IsLeaf )
				{
					for ( int i = 0; i < 4; ++i )
					{
						this.mChildren[ i ].UpdateVertexData( positions, deltas, rect, cpuData );

						// merge bounds from children
						var childBox = new AxisAlignedBox( this.mChildren[ i ].AABB );

						// this box is relative to child centre
						Vector3 boxoffset = this.mChildren[ i ].LocalCentre - LocalCentre;
						childBox.Minimum = childBox.Minimum + boxoffset;
						childBox.Maximum = childBox.Maximum + boxoffset;
						this.mAABB.Merge( childBox );
					}
				}

				// Make sure node knows to update
				if ( this.mMovable != null && this.mMovable.IsAttached )
				{
					this.mMovable.ParentSceneNode.NeedUpdate();
				}
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
		[OgreVersion( 1, 7, 2 )]
		public void MergeIntoBounds( long x, long y, Vector3 pos )
		{
			if ( PointIntersectsNode( x, y ) )
			{
				Vector3 localPos = pos - this.mLocalCentre;
				this.mAABB.Merge( localPos );
				this.mBoundingRadius = System.Math.Max( this.mBoundingRadius, localPos.Length );
				if ( !IsLeaf )
				{
					for ( int i = 0; i < 4; ++i )
					{
						this.mChildren[ i ].MergeIntoBounds( x, y, pos );
					}
				}
			}
		}

		/// <summary>
		/// Reset the bounds of this node and all its children for the region given.
		/// </summary>
		/// <param name="rect">The region for which bounds should be reset, in top-level terrain coords</param>
		[OgreVersion( 1, 7, 2 )]
		public void ResetBounds( Rectangle rect )
		{
			if ( RectContainsNode( rect ) )
			{
				this.mAABB.IsNull = true;
				this.mBoundingRadius = 0;

				if ( !IsLeaf )
				{
					for ( int i = 0; i < 4; ++i )
					{
						this.mChildren[ i ].ResetBounds( rect );
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
		[OgreVersion( 1, 7, 2 )]
		public bool RectContainsNode( Rectangle rect )
		{
			return ( rect.Left <= this.mOffsetX && rect.Right > this.mBoundaryX && rect.Top <= this.mOffsetY && rect.Bottom > this.mBoundaryY );
		}

		/// <summary>
		///  Returns true if the given rectangle overlaps the terrain area that
		///	 this node references.
		/// </summary>
		/// <param name="rect">The region in top-level terrain coords</param>
		/// <returns></returns>
		[OgreVersion( 1, 7, 2 )]
		public bool RectIntersectsNode( Rectangle rect )
		{
			return ( rect.Right >= this.mOffsetX && rect.Left <= this.mBoundaryX && rect.Bottom >= this.mOffsetY && rect.Top <= this.mBoundaryY );
		}

		/// <summary>
		/// Returns true if the given point is in the terrain area that
		/// this node references.
		/// </summary>
		/// <param name="x">The point in top-level terrain coords</param>
		/// <param name="y">The point in top-level terrain coords</param>
		/// <returns></returns>
		[OgreVersion( 1, 7, 2 )]
		public bool PointIntersectsNode( long x, long y )
		{
			return ( x >= this.mOffsetX && x < this.mBoundaryX && y >= this.mOffsetY && y < this.mBoundaryY );
		}

		/// <summary>
		/// Calculate appropriate LOD for this node and children
		/// </summary>
		/// <param name="cam">The camera to be used (this should already be the LOD camera)</param>
		/// <param name="cFactor">The cFactor which incorporates the viewport size, max pixel error and lod bias</param>
		/// <returns>true if this node or any of its children were selected for rendering</returns>
		[OgreVersion( 1, 7, 2 )]
		public bool CalculateCurrentLod( Camera cam, Real cFactor )
		{
			this.mSelfOrChildRendered = false;

			//check children first.
			int childrenRenderedOut = 0;
			if ( !IsLeaf )
			{
				for ( int i = 0; i < 4; ++i )
				{
					if ( this.mChildren[ i ].CalculateCurrentLod( cam, cFactor ) )
					{
						++childrenRenderedOut;
					}
				}
			}

			if ( childrenRenderedOut == 0 )
			{
				// no children were within their LOD ranges, so we should consider our own
				Vector3 localPos = cam.DerivedPosition - this.mLocalCentre - this.mTerrain.Position;
				Real dist;
				if ( TerrainGlobalOptions.UseRayBoxDistanceCalculation )
				{
					// Get distance to this terrain node (to closest point of the box)
					// head towards centre of the box (note, box may not cover mLocalCentre because of height)
					Vector3 dir = this.mAABB.Center - localPos;
					dir.Normalize();
					var ray = new Ray( localPos, dir );
					IntersectResult intersectRes = ray.Intersects( this.mAABB );

					// ray will always intersect, we just want the distance
					dist = intersectRes.Distance;
				}
				else
				{
					// distance to tile centre
					dist = localPos.Length;
					// deduct half the radius of the box, assume that on average the 
					// worst case is best approximated by this
					dist -= ( this.mBoundingRadius * 0.5f );
				}

				// Do material LOD
				Material material = Material;
				LodStrategy str = material.LodStrategy;
				Real lodValue = str.GetValue( this.mMovable, cam );
				// Get the index at this biased depth
				this.mMaterialLodIndex = (ushort)material.GetLodIndex( lodValue );


				// For each LOD, the distance at which the LOD will transition *downwards*
				// is given by 
				// distTransition = maxDelta * cFactor;
				int lodLvl = 0;
				this.mCurrentLod = -1;
				foreach ( LodLevel i in this.mLodLevels )
				{
					// If we have no parent, and this is the lowest LOD, we always render
					// this is the 'last resort' so to speak, we always enoucnter this last
					if ( lodLvl + 1 == this.mLodLevels.Count && this.mParent == null )
					{
						CurrentLod = lodLvl;
						this.mSelfOrChildRendered = true;
						this.mLodTransition = 0;
					}
					else
					{
						//check the distance
						LodLevel ll = i;
						// Calculate or reuse transition distance
						Real distTransition;
						if ( Utility.RealEqual( cFactor, ll.LastCFactor ) )
						{
							distTransition = ll.LastTransitionDist;
						}
						else
						{
							distTransition = ll.MaxHeightDelta * cFactor;
							ll.LastCFactor = cFactor;
							ll.LastTransitionDist = distTransition;
						}

						if ( dist < distTransition )
						{
							// we're within range of this LOD
							CurrentLod = lodLvl;
							this.mSelfOrChildRendered = true;

							if ( this.mTerrain.IsMorphRequired )
							{
								// calculate the transition percentage
								// we need a percentage of the total distance for just this LOD, 
								// which means taking off the distance for the next higher LOD
								// which is either the previous entry in the LOD list, 
								// or the largest of any children. In both cases these will
								// have been calculated before this point, since we process
								// children first. Distances at lower LODs are guaranteed
								// to be larger than those at higher LODs

								Real distTotal = distTransition;
								if ( IsLeaf )
								{
									// Any higher LODs?
									if ( !i.Equals( this.mLodLevels[ 0 ] ) )
									{
										int prev = lodLvl - 1;
										distTotal -= this.mLodLevels[ prev ].LastTransitionDist;
									}
								}
								else
								{
									// Take the distance of the lowest LOD of child
									LodLevel childLod = this.mChildWithMaxHeightDelta.GetLodLevel( (ushort)( this.mChildWithMaxHeightDelta.LodCount - 1 ) );
									distTotal -= childLod.LastTransitionDist;
								}
								// fade from 0 to 1 in the last 25% of the distance
								Real distMorphRegion = distTotal * 0.25f;
								Real distRemain = distTransition - dist;

								this.mLodTransition = 1.0f - ( distRemain / distMorphRegion );
								this.mLodTransition = System.Math.Min( 1.0f, this.mLodTransition );
								this.mLodTransition = System.Math.Max( 0.0f, this.mLodTransition );

								// Pass both the transition % and target LOD (GLOBAL current + 1)
								// this selectively applies the morph just to the
								// vertices which would drop out at this LOD, even 
								// while using the single shared vertex data
								this.mRend.SetCustomParameter( Terrain.LOD_MORPH_CUSTOM_PARAM, new Vector4( this.mLodTransition, this.mCurrentLod + this.mBaseLod + 1, 0, 0 ) );
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
				this.mCurrentLod = -1;
				this.mSelfOrChildRendered = true;
				if ( childrenRenderedOut < 4 )
				{
					// only *some* children decided to render on their own, but either 
					// none or all need to render, so set the others manually to their lowest
					for ( int i = 0; i < 4; ++i )
					{
						TerrainQuadTreeNode child = this.mChildren[ i ];
						if ( !child.IsSelfOrChildrenRenderedAtCurrentLod )
						{
							child.CurrentLod = child.LodCount - 1;
							child.LodTransition = 1.0f;
						}
					}
				} //(childRenderedCount < 4)
			} // (childRenderedCount == 0)

			return this.mSelfOrChildRendered;
		}

		/// <summary>
		/// Update the vertex buffers - the rect in question is relative to the whole terrain, 
		/// not the local vertex data (which may use a subset)
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		protected void UpdateVertexBuffer( HardwareVertexBuffer posBuf, HardwareVertexBuffer deltaBuf, Rectangle rect )
		{
			Debug.Assert( rect.Left >= this.mOffsetX && rect.Right <= this.mBoundaryX && rect.Top >= this.mOffsetY && rect.Bottom <= this.mBoundaryY );

			// potentially reset our bounds depending on coverage of the update
			ResetBounds( rect );

			//main data
			var inc = (ushort)( ( this.mTerrain.Size - 1 ) / ( this.mVertexDataRecord.Resolution - 1 ) );
			long destOffsetX = rect.Left <= this.mOffsetX ? 0 : ( rect.Left - this.mOffsetX ) / inc;
			long destOffsetY = rect.Top <= this.mOffsetY ? 0 : ( rect.Top - this.mOffsetY ) / inc;
			// Fill the buffers

			BufferLocking lockmode;
			if ( destOffsetX != 0 || destOffsetY != 0 || rect.Width < this.mSize || rect.Height < this.mSize )
			{
				lockmode = BufferLocking.Normal;
			}
			else
			{
				lockmode = BufferLocking.Discard;
			}
			Real uvScale = 1.0f / ( this.mTerrain.Size - 1 );
			BufferBase pBaseHeight = this.mTerrain.GetHeightData( rect.Left, rect.Top );
			BufferBase pBaseDelta = this.mTerrain.GetDeltaData( rect.Left, rect.Top );
			var rowskip = (ushort)( this.mTerrain.Size * inc );
			ushort destPosRowSkip = 0, destDeltaRowSkip = 0;
			BufferBase pRootPosBuf = null;
			BufferBase pRootDeltaBuf = null;
			BufferBase pRowPosBuf = null;
			BufferBase pRowDeltaBuf = null;

			if ( posBuf != null )
			{
				destPosRowSkip = (ushort)( this.mVertexDataRecord.Size * posBuf.VertexSize );
				pRootPosBuf = posBuf.Lock( lockmode );
				pRowPosBuf = pRootPosBuf;
				// skip dest buffer in by left/top
				pRowPosBuf += destOffsetY * destPosRowSkip + destOffsetX * posBuf.VertexSize;
			}
			if ( deltaBuf != null )
			{
				destDeltaRowSkip = (ushort)( this.mVertexDataRecord.Size * deltaBuf.VertexSize );
				pRootDeltaBuf = deltaBuf.Lock( lockmode );
				pRowDeltaBuf = pRootDeltaBuf;
				// skip dest buffer in by left/top
				pRowDeltaBuf += destOffsetY * destDeltaRowSkip + destOffsetX * deltaBuf.VertexSize;
			}
			Vector3 pos = Vector3.Zero;

			for ( var y = (ushort)rect.Top; y < rect.Bottom; y += inc )
			{
#if !AXIOM_SAFE_ONLY
				unsafe
#endif
				{
					float* pHeight = pBaseHeight.ToFloatPointer();
					int pHIdx = 0;
					float* pDelta = pBaseDelta.ToFloatPointer();
					int pDeltaIdx = 0;
					float* pPosBuf = pRowPosBuf != null ? pRowPosBuf.ToFloatPointer() : null;
					int pPosBufIdx = 0;
					float* pDeltaBuf = pRowDeltaBuf != null ? pRowDeltaBuf.ToFloatPointer() : null;
					int pDeltaBufIdx = 0;
					for ( var x = (ushort)rect.Left; x < rect.Right; x += inc )
					{
						if ( pPosBuf != null )
						{
							this.mTerrain.GetPoint( x, y, pHeight[ pHIdx ], ref pos );
							// Update bounds *before* making relative
							MergeIntoBounds( x, y, pos );
							// relative to local centre
							pos -= this.mLocalCentre;
							pHIdx += inc;

							pPosBuf[ pPosBufIdx++ ] = pos.x;
							pPosBuf[ pPosBufIdx++ ] = pos.y;
							pPosBuf[ pPosBufIdx++ ] = pos.z;

							// UVs - base UVs vary from 0 to 1, all other values
							// will be derived using scalings
							pPosBuf[ pPosBufIdx++ ] = x * uvScale;
							pPosBuf[ pPosBufIdx++ ] = 1.0f - ( y * uvScale );
						}

						if ( pDeltaBuf != null )
						{
							//delta
							pDeltaBuf[ pDeltaBufIdx++ ] = pDelta[ pDeltaIdx ];
							pDeltaIdx += inc;
							// delta LOD threshold
							// we want delta to apply to LODs no higher than this value
							// at runtime this will be combined with a per-renderable parameter
							// to ensure we only apply morph to the correct LOD
							pDeltaBuf[ pDeltaBufIdx++ ] = this.mTerrain.GetLODLevelWhenVertexEliminated( x, y ) - 1.0f;
						}
					} // end unsafe
				} //end for

				pBaseHeight += rowskip;
				pBaseDelta += rowskip;
				if ( pRowPosBuf != null )
				{
					pRowPosBuf += destPosRowSkip;
				}
				if ( pRowDeltaBuf != null )
				{
					pRowDeltaBuf += destDeltaRowSkip;
				}
			} //end for

			// Skirts now
			// skirt spacing based on top-level resolution (* inc to cope with resolution which is not the max)
			var skirtSpacing = (ushort)( this.mVertexDataRecord.SkirtRowColSkip * inc );
			Vector3 skirtOffset = Vector3.Zero;
			this.mTerrain.GetVector( 0, 0, -this.mTerrain.SkirtSize, ref skirtOffset );

			// skirt rows
			// clamp rows to skirt spacing (round up)
			long skirtStartX = rect.Left;
			long skirtStartY = rect.Top;
			// for rows, clamp Y to skirt frequency, X to inc (LOD resolution vs top)
			if ( skirtStartY % skirtSpacing != 0 )
			{
				skirtStartY += skirtSpacing - ( skirtStartY % skirtSpacing );
			}
			if ( skirtStartX % inc != 0 )
			{
				skirtStartX += inc - ( skirtStartX % inc );
			}

			skirtStartY = System.Math.Max( skirtStartY, this.mOffsetY );
			pBaseHeight = this.mTerrain.GetHeightData( skirtStartX, skirtStartY );
			if ( posBuf != null )
			{
				// position dest buffer just after the main vertex data
				pRowPosBuf = pRootPosBuf + posBuf.VertexSize * this.mVertexDataRecord.Size * this.mVertexDataRecord.Size;
				// move it onwards to skip the skirts we don't need to update
				pRowPosBuf += destPosRowSkip * ( ( skirtStartY - this.mOffsetY ) / skirtSpacing );
				pRowPosBuf += posBuf.VertexSize * ( skirtStartX - this.mOffsetX ) / inc;
			}
			if ( deltaBuf != null )
			{
				// position dest buffer just after the main vertex data
				pRowDeltaBuf = pRootDeltaBuf + deltaBuf.VertexSize * this.mVertexDataRecord.Size * this.mVertexDataRecord.Size;
				// move it onwards to skip the skirts we don't need to update
				pRowDeltaBuf += destDeltaRowSkip * ( skirtStartY - this.mOffsetY ) / skirtSpacing;
				pRowDeltaBuf += deltaBuf.VertexSize * ( skirtStartX - this.mOffsetX ) / inc;
			}

			for ( var y = (ushort)skirtStartY; y < (ushort)rect.Bottom; y += skirtSpacing )
			{
#if !AXIOM_SAFE_ONLY
				unsafe
#endif
				{
					float* pHeight = pBaseHeight.ToFloatPointer();
					int pHIdx = 0;
					float* pPosBuf = pRowPosBuf != null ? pRowPosBuf.ToFloatPointer() : null;
					int pPosIdx = 0;
					float* pDeltaBuf = pRowDeltaBuf != null ? pRowDeltaBuf.ToFloatPointer() : null;
					int pDeltaIdx = 0;
					for ( var x = (ushort)skirtStartX; x < (ushort)rect.Right; x += inc )
					{
						if ( pPosBuf != null )
						{
							this.mTerrain.GetPoint( x, y, pHeight[ pHIdx ], ref pos );
							// relative to local centre
							pos -= this.mLocalCentre;
							pHIdx += inc;
							pos += skirtOffset;
							pPosBuf[ pPosIdx++ ] = pos.x;
							pPosBuf[ pPosIdx++ ] = pos.y;
							pPosBuf[ pPosIdx++ ] = pos.z;

							// UVs - same as base
							pPosBuf[ pPosIdx++ ] = x * uvScale;
							pPosBuf[ pPosIdx++ ] = 1.0f - ( y * uvScale );
						}
						if ( pDeltaBuf != null )
						{
							// delta (none)
							pDeltaBuf[ pDeltaIdx++ ] = 0;
							// delta threshold (irrelevant)
							pDeltaBuf[ pDeltaIdx++ ] = 99;
						}
					} //end for
					pBaseHeight += this.mTerrain.Size * skirtSpacing;
					if ( pRowPosBuf != null )
					{
						pRowPosBuf += destPosRowSkip;
					}
					if ( pRowDeltaBuf != null )
					{
						pRowDeltaBuf += destDeltaRowSkip;
					}
				} // end unsafe
			} //end for

			// skirt cols
			// clamp cols to skirt spacing (round up)
			skirtStartX = rect.Left;
			if ( skirtStartX % skirtSpacing != 0 )
			{
				skirtStartX += skirtSpacing - ( skirtStartX % skirtSpacing );
			}
			// clamp Y to inc (LOD resolution vs top)
			skirtStartY = rect.Top;
			if ( skirtStartY % inc != 0 )
			{
				skirtStartY += inc - ( skirtStartY % inc );
			}
			skirtStartX = System.Math.Max( skirtStartX, this.mOffsetX );

			if ( posBuf != null )
			{
				// position dest buffer just after the main vertex data and skirt rows
				pRowPosBuf = pRootPosBuf + posBuf.VertexSize * this.mVertexDataRecord.Size * this.mVertexDataRecord.Size;
				// skip the row skirts
				pRowPosBuf += this.mVertexDataRecord.NumSkirtRowsCols * this.mVertexDataRecord.Size * posBuf.VertexSize;
				// move it onwards to skip the skirts we don't need to update
				pRowPosBuf += destPosRowSkip * ( skirtStartX - this.mOffsetX ) / skirtSpacing;
				pRowPosBuf += posBuf.VertexSize * ( skirtStartY - this.mOffsetY ) / inc;
			}
			if ( deltaBuf != null )
			{
				// Deltaition dest buffer just after the main vertex data and skirt rows
				pRowDeltaBuf = pRootDeltaBuf + deltaBuf.VertexSize * this.mVertexDataRecord.Size * this.mVertexDataRecord.Size;

				// skip the row skirts
				pRowDeltaBuf += this.mVertexDataRecord.NumSkirtRowsCols * this.mVertexDataRecord.Size * deltaBuf.VertexSize;
				// move it onwards to skip the skirts we don't need to update
				pRowDeltaBuf += destDeltaRowSkip * ( skirtStartX - this.mOffsetX ) / skirtSpacing;
				pRowDeltaBuf += deltaBuf.VertexSize * ( skirtStartY - this.mOffsetY ) / inc;
			}

			for ( var x = (ushort)skirtStartX; x < (ushort)rect.Right; x += skirtSpacing )
			{
#if !AXIOM_SAFE_ONLY
				unsafe
#endif
				{
					float* pPosBuf = pRowPosBuf != null ? pRowPosBuf.ToFloatPointer() : null;
					int pPosIdx = 0;
					float* pDeltaBuf = pRowDeltaBuf != null ? pRowDeltaBuf.ToFloatPointer() : null;
					int pDeltaIdx = 0;
					for ( var y = (ushort)skirtStartY; y < (ushort)rect.Bottom; y += inc )
					{
						if ( pPosBuf != null )
						{
							this.mTerrain.GetPoint( x, y, this.mTerrain.GetHeightAtPoint( x, y ), ref pos );
							// relative to local centre
							pos -= this.mLocalCentre;
							pos += skirtOffset;

							pPosBuf[ pPosIdx++ ] = pos.x;
							pPosBuf[ pPosIdx++ ] = pos.y;
							pPosBuf[ pPosIdx++ ] = pos.z;

							// UVs - same as base
							pPosBuf[ pPosIdx++ ] = x * uvScale;
							pPosBuf[ pPosIdx++ ] = 1.0f - ( y * uvScale );
						}
						if ( pDeltaBuf != null )
						{
							// delta (none)
							pDeltaBuf[ pDeltaIdx++ ] = 0;
							// delta threshold (irrelevant)
							pDeltaBuf[ pDeltaIdx++ ] = 99;
						}
					} //end for
					if ( pRowPosBuf != null )
					{
						pRowPosBuf += destPosRowSkip;
					}
					if ( pRowDeltaBuf != null )
					{
						pRowDeltaBuf += destDeltaRowSkip;
					}
				} // end unsafe
			} //end for

			if ( posBuf != null )
			{
				posBuf.Unlock();
			}
			if ( deltaBuf != null )
			{
				deltaBuf.Unlock();
			}
		}

		[OgreVersion( 1, 7, 2 )]
		protected void CreateCpuVertexData()
		{
			if ( this.mVertexDataRecord != null )
			{
				DestroyCpuVertexData();

				// create vertex structure, not using GPU for now (these are CPU structures)
				var dcl = new VertexDeclaration();
				var bufbind = new VertexBufferBinding();

				this.mVertexDataRecord.CpuVertexData = new VertexData( dcl, bufbind );

				// Vertex declaration
				// TODO: consider vertex compression
				int offset = 0;
				// POSITION 
				// float3(x, y, z)
				offset += dcl.AddElement( POSITION_BUFFER, offset, VertexElementType.Float3, VertexElementSemantic.Position ).Size;
				// UV0
				// float2(u, v)
				// TODO - only include this if needing fixed-function
				offset += dcl.AddElement( POSITION_BUFFER, offset, VertexElementType.Float2, VertexElementSemantic.TexCoords, 0 ).Size;
				// UV1 delta information
				// float2(delta, deltaLODthreshold)
				offset = 0;
				offset += dcl.AddElement( DELTA_BUFFER, offset, VertexElementType.Float2, VertexElementSemantic.TexCoords, 1 ).Size;

				// Calculate number of vertices
				// Base geometry size * size
				var baseNumVerts = (int)Utility.Sqr( this.mVertexDataRecord.Size );
				int numVerts = baseNumVerts;
				// Now add space for skirts
				// Skirts will be rendered as copies of the edge vertices translated downwards
				// Some people use one big fan with only 3 vertices at the bottom, 
				// but this requires creating them much bigger that necessary, meaning
				// more unnecessary overdraw, so we'll use more vertices 
				// You need 2^levels + 1 rows of full resolution (max 129) vertex copies, plus
				// the same number of columns. There are common vertices at intersections
				ushort levels = this.mVertexDataRecord.TreeLevels;
				this.mVertexDataRecord.NumSkirtRowsCols = (ushort)( System.Math.Pow( 2, levels ) + 1 );
				this.mVertexDataRecord.SkirtRowColSkip = (ushort)( ( this.mVertexDataRecord.Size - 1 ) / ( this.mVertexDataRecord.NumSkirtRowsCols - 1 ) );
				numVerts += this.mVertexDataRecord.Size * this.mVertexDataRecord.NumSkirtRowsCols;
				numVerts += this.mVertexDataRecord.Size * this.mVertexDataRecord.NumSkirtRowsCols;

				//manually create CPU-side buffer
				HardwareVertexBuffer posBuf = HardwareBufferManager.Instance.CreateVertexBuffer( dcl.Clone( POSITION_BUFFER ), numVerts, BufferUsage.StaticWriteOnly, false );
				HardwareVertexBuffer deltabuf = HardwareBufferManager.Instance.CreateVertexBuffer( dcl.Clone( DELTA_BUFFER ), numVerts, BufferUsage.StaticWriteOnly, false );

				this.mVertexDataRecord.CpuVertexData.vertexStart = 0;
				this.mVertexDataRecord.CpuVertexData.vertexCount = numVerts;

				var updateRect = new Rectangle( this.mOffsetX, this.mOffsetY, this.mBoundaryX, this.mBoundaryY );
				UpdateVertexBuffer( posBuf, deltabuf, updateRect );
				this.mVertexDataRecord.IsGpuVertexDataDirty = true;
				bufbind.SetBinding( POSITION_BUFFER, posBuf );
				bufbind.SetBinding( DELTA_BUFFER, deltabuf );
			}
		}

		[OgreVersion( 1, 7, 2 )]
		protected void PopulateIndexData( ushort batchSize, IndexData destData )
		{
			VertexDataRecord vdr = VertextDataRecord;

			// Ratio of the main terrain resolution in relation to this vertex data resolution
			int resolutionRatio = ( this.mTerrain.Size - 1 ) / ( vdr.Resolution - 1 );
			// At what frequency do we sample the vertex data we're using?
			// mSize is the coverage in terms of the original terrain data (not split to fit in 16-bit)
			int vertexIncrement = ( this.mSize - 1 ) / ( batchSize - 1 );
			// however, the vertex data we're referencing may not be at the full resolution anyway
			vertexIncrement /= resolutionRatio;
			var vdatasizeOffsetX = (ushort)( ( this.mOffsetX - this.mNodeWithVertexData.mOffsetX ) / resolutionRatio );
			var vdatasizeOffsetY = (ushort)( ( this.mOffsetY - this.mNodeWithVertexData.mOffsetY ) / resolutionRatio );

			destData.indexBuffer = this.mTerrain.GpuBufferAllocator.GetSharedIndexBuffer( batchSize, vdr.Size, vertexIncrement, vdatasizeOffsetX, vdatasizeOffsetY, vdr.NumSkirtRowsCols, vdr.SkirtRowColSkip );

			destData.indexStart = 0;
			destData.indexCount = destData.indexBuffer.IndexCount;

			// shared index buffer is pre-populated	
		}

		[OgreVersion( 1, 7, 2 )]
		protected void DestroyCpuVertexData()
		{
			if ( this.mVertexDataRecord != null && this.mVertexDataRecord.CpuVertexData != null )
			{
				// delete the bindings and declaration manually since not from a buf mgr
				if ( !this.mVertexDataRecord.CpuVertexData.vertexDeclaration.IsDisposed )
				{
					this.mVertexDataRecord.CpuVertexData.vertexDeclaration.Dispose();
				}
				this.mVertexDataRecord.CpuVertexData.vertexDeclaration = null;

				if ( !this.mVertexDataRecord.CpuVertexData.vertexBufferBinding.IsDisposed )
				{
					this.mVertexDataRecord.CpuVertexData.vertexBufferBinding.Dispose();
				}
				this.mVertexDataRecord.CpuVertexData.vertexBufferBinding = null;

				if ( !this.mVertexDataRecord.CpuVertexData.IsDisposed )
				{
					this.mVertexDataRecord.CpuVertexData.Dispose();
				}
				this.mVertexDataRecord.CpuVertexData = null;
			}
		}

		[OgreVersion( 1, 7, 2 )]
		protected void CreateGpuVertexData()
		{
			// TODO - mutex cpu data
			if ( this.mVertexDataRecord != null && this.mVertexDataRecord.CpuVertexData != null && this.mVertexDataRecord.GpuVertexData == null )
			{
				// copy data from CPU to GPU, but re-use vertex buffers (so don't use regular clone)
				this.mVertexDataRecord.GpuVertexData = new VertexData();
				VertexData srcData = this.mVertexDataRecord.CpuVertexData;
				VertexData destData = this.mVertexDataRecord.GpuVertexData;

				// copy vertex buffers
				// get new buffers
				HardwareVertexBuffer destPosBuf, destDeltaBuf;
				this.mTerrain.GpuBufferAllocator.AllocateVertexBuffers( this.mTerrain, srcData.vertexCount, out destPosBuf, out destDeltaBuf );

				// copy data
				destPosBuf.CopyTo( srcData.vertexBufferBinding.GetBuffer( POSITION_BUFFER ) );
				destDeltaBuf.CopyTo( srcData.vertexBufferBinding.GetBuffer( DELTA_BUFFER ) );

				// set bindings
				destData.vertexBufferBinding.SetBinding( POSITION_BUFFER, destPosBuf );
				destData.vertexBufferBinding.SetBinding( DELTA_BUFFER, destDeltaBuf );

				// Basic vertex info
				destData.vertexStart = srcData.vertexStart;
				destData.vertexCount = srcData.vertexCount;
				// Copy elements
				foreach ( VertexElement ei in srcData.vertexDeclaration.Elements )
				{
					destData.vertexDeclaration.AddElement( ei.Source, ei.Offset, ei.Type, ei.Semantic, ei.Index );
				}

				this.mVertexDataRecord.IsGpuVertexDataDirty = false;

				// We don't need the CPU copy anymore
				DestroyCpuVertexData();
			}
		}

		[OgreVersion( 1, 7, 2 )]
		protected void DestroyGpuVertexData()
		{
			if ( this.mVertexDataRecord != null && this.mVertexDataRecord.GpuVertexData != null )
			{
				// Before we delete, free up the vertex buffers for someone else
				this.mTerrain.GpuBufferAllocator.FreeVertexBuffers( this.mVertexDataRecord.GpuVertexData.vertexBufferBinding.GetBuffer( POSITION_BUFFER ), this.mVertexDataRecord.GpuVertexData.vertexBufferBinding.GetBuffer( DELTA_BUFFER ) );

				if ( !this.mVertexDataRecord.GpuVertexData.IsDisposed )
				{
					this.mVertexDataRecord.GpuVertexData.Dispose();
				}

				this.mVertexDataRecord.GpuVertexData = null;
			}
		}

		[OgreVersion( 1, 7, 2 )]
		protected void UpdateGpuVertexData()
		{
			if ( this.mVertexDataRecord != null && this.mVertexDataRecord.IsGpuVertexDataDirty )
			{
				this.mVertexDataRecord.GpuVertexData.vertexBufferBinding.GetBuffer( POSITION_BUFFER ).CopyTo( this.mVertexDataRecord.CpuVertexData.vertexBufferBinding.GetBuffer( POSITION_BUFFER ) );

				this.mVertexDataRecord.GpuVertexData.vertexBufferBinding.GetBuffer( DELTA_BUFFER ).CopyTo( this.mVertexDataRecord.CpuVertexData.vertexBufferBinding.GetBuffer( DELTA_BUFFER ) );

				this.mVertexDataRecord.IsGpuVertexDataDirty = false;
			}
		}

		[OgreVersion( 1, 7, 2 )]
		protected void CreateGpuIndexData()
		{
			for ( int lod = 0; lod < this.mLodLevels.Count; ++lod )
			{
				LodLevel ll = this.mLodLevels[ lod ];

				if ( ll.GpuIndexData == null )
				{
					// clone, using default buffer manager ie hardware
					ll.GpuIndexData = new IndexData();
					PopulateIndexData( ll.BatchSize, ll.GpuIndexData );
				}
				this.mLodLevels[ lod ] = ll;
			}
		}

		[OgreVersion( 1, 7, 2 )]
		protected void DestroyGpuIndexData()
		{
			for ( int lod = 0; lod < this.mLodLevels.Count; ++lod )
			{
				LodLevel ll = this.mLodLevels[ lod ];

				if ( ll.GpuIndexData != null )
				{
					if ( !ll.GpuIndexData.IsDisposed )
					{
						ll.GpuIndexData.Dispose();
					}

					ll.GpuIndexData = null;
				}

				this.mLodLevels[ lod ] = ll;
			}
		}

		[OgreVersion( 1, 7, 2 )]
		protected ushort CalcSkirtVertexIndex( ushort mainIndex, bool isCol )
		{
			VertexDataRecord vdr = VertextDataRecord;
			// row / col in main vertex resolution
			var row = (ushort)( mainIndex / vdr.Size );
			var col = (ushort)( mainIndex % vdr.Size );

			// skrits are after main vertices, so skip them
			var ubase = (ushort)( vdr.Size * vdr.Size );

			// The layout in vertex data is:
			// 1. row skirts
			//    numSkirtRowsCols rows of resolution vertices each
			// 2. column skirts
			//    numSkirtRowsCols cols of resolution vertices each

			// No offsets used here, this is an index into the current vertex data, 
			// which is already relative
			if ( isCol )
			{
				var skirtNum = (ushort)( col / vdr.SkirtRowColSkip );
				var colBase = (ushort)( vdr.NumSkirtRowsCols * vdr.Size );
				return (ushort)( ubase + colBase + vdr.Size * skirtNum + row );
			}
			else
			{
				var skirtNum = (ushort)( row / vdr.SkirtRowColSkip );
				return (ushort)( ubase + vdr.Size * skirtNum + col );
			}
		}

		[OgreVersion( 1, 7, 2 )]
		public void UpdateRenderQueue( RenderQueue queue )
		{
			if ( IsRenderedAtCurrentLod )
			{
				queue.AddRenderable( this.mRend, this.mTerrain.RenderQueueGroupID );
			}
		}

		public void VisitRenderables( bool debugRenderables )
		{
#warning: implement VisitRenderables
			throw new NotImplementedException();
			//visitor->visit( mRend, 0, false );
		}

		[OgreVersion( 1, 7, 2 )]
		public void GetWorldTransforms( Matrix4[] xform )
		{
			// the vertex data is relative to the node that owns the vertex data
			xform[ 0 ] = this.mNodeWithVertexData.mMovable.ParentNodeFullTransform;
		}

		[OgreVersion( 1, 7, 2 )]
		public Real GetSquaredViewDepth( Camera cam )
		{
			return this.mMovable.ParentSceneNode.GetSquaredViewDepth( cam );
		}

		#region - implementation of Movable -

		/// <summary>
		/// MovableObject implementation to provide the hook to the scene.
		/// </summary>
		/// <remarks>
		/// In one sense, it would be most convenient to have a single MovableObject
		///	to represent the whole Terrain object, and then internally perform
		///	some quadtree frustum culling to narrow down which specific tiles are rendered.
		///	However, the one major flaw with that is that exposing the bounds to 
		///	the SceneManager at that level prevents it from doing anything smarter
		///	in terms of culling - for example a portal or occlusion culling SceneManager
		///	would have no opportunity to process the leaf nodes in those terms, and
		///	a simple frustum cull may give significantly poorer results. 
		///
		/// @par
		/// Therefore, we in fact register a MovableObject at every node, and 
		///	use the LOD factor to determine which one is currently active. LODs
		///	must be mutually exclusive and to deal with precision errors, we really
		///	need to evaluate them all at once, rather than as part of the 
		///	_notifyCurrentCamera function. Therefore the root Terrain registers
		///	a SceneManager::Listener to precalculate which nodes will be displayed 
		///	when it comes to purely a LOD basis.
		/// </remarks>
		protected class Movable : MovableObject
		{
			protected TerrainQuadTreeNode mParent;

			#region - properties -

			public new string MovableType
			{
				[OgreVersion( 1, 7, 2 )]
				get
				{
					return "AxiomTerrainNodeMovable";
				}

				set
				{
					base.MovableType = value;
				}
			}

			public override AxisAlignedBox BoundingBox
			{
				[OgreVersion( 1, 7, 2 )]
				get
				{
					return this.mParent.AABB;
				}
			}

			public override Real BoundingRadius
			{
				[OgreVersion( 1, 7, 2 )]
				get
				{
					return this.mParent.BoundingRadius;
				}
			}

			public override bool IsVisible
			{
				[OgreVersion( 1, 7, 2 )]
				get
				{
					if ( this.mParent.CurrentLod == -1 )
					{
						return false;
					}
					else
					{
						return base.IsVisible;
					}
				}
				set
				{
					base.IsVisible = value;
				}
			}

			public new uint VisibilityFlags
			{
				[OgreVersion( 1, 7, 2 )]
				get
				{
					// Combine own vis (in case anyone sets this) and terrain overall
					return base.visibilityFlags & this.mParent.Terrain.VisibilityFlags;
				}
			}

			public override uint QueryFlags
			{
				[OgreVersion( 1, 7, 2 )]
				get
				{
					// Combine own vis (in case anyone sets this) and terrain overall
					return base.queryFlags & this.mParent.Terrain.QueryFlags;
				}
			}

			public override bool CastShadows
			{
				[OgreVersion( 1, 7, 2 )]
				get
				{
					return this.mParent.CastsShadows;
				}
				set
				{
					//DO NOTHING
				}
			}

			#endregion - properties -

			[OgreVersion( 1, 7, 2 )]
			public Movable( TerrainQuadTreeNode parent )
			{
				this.mParent = parent;
			}

			public override void UpdateRenderQueue( RenderQueue queue )
			{
				this.mParent.UpdateRenderQueue( queue );
			}

			//TODO
			//void TerrainQuadTreeNode::Movable::visitRenderables(Renderable::Visitor* visitor,  bool debugRenderables)
			//{
			//    mParent->visitRenderables(visitor, debugRenderables);	
			//}
		}

		#endregion - implementation of Movable -

		#region - Renderable -

		/// <summary>
		/// Hook to the render queue
		/// </summary>
		protected class Rend : IRenderable
		{
			protected Dictionary<int, Vector4> mCustomParameters = new Dictionary<int, Vector4>();
			protected TerrainQuadTreeNode mParent;
			protected bool mPolygoneModeOverridable = true;
			protected bool mUseIdentityProjection;
			protected bool mUseIdentityView;

			public Rend( TerrainQuadTreeNode parent )
			{
				this.mParent = parent;
			}

			#region IRenderable Members

			/// <summary>
			/// Retrieves a weak reference to the material this renderable object uses.
			/// </summary>
			public Material Material
			{
				[OgreVersion( 1, 7, 2 )]
				get
				{
					return this.mParent.Material;
				}
			}

			/// <summary>
			/// Retrieves a pointer to the Material Technique this renderable object uses.
			/// </summary>
			public Technique Technique
			{
				[OgreVersion( 1, 7, 2 )]
				get
				{
					return this.mParent.Technique;
				}
			}

			public RenderOperation RenderOperation
			{
				[OgreVersion( 1, 7, 2 )]
				get
				{
					return this.mParent.RenderOperation;
				}
			}

			[OgreVersion( 1, 7, 2 )]
			public void GetWorldTransforms( Matrix4[] transforms )
			{
				this.mParent.GetWorldTransforms( transforms );
			}

			[OgreVersion( 1, 7, 2 )]
			public Real GetSquaredViewDepth( Camera cam )
			{
				return this.mParent.GetSquaredViewDepth( cam );
			}

			/// <summary>
			/// Gets a list of lights, ordered relative to how close they are to this renderable.
			/// </summary>
			public LightList Lights
			{
				[OgreVersion( 1, 7, 2 )]
				get
				{
					return this.mParent.Lights;
				}
			}

			public bool CastsShadows
			{
				[OgreVersion( 1, 7, 2 )]
				get
				{
					return this.mParent.CastsShadows;
				}
			}

			public bool NormalizeNormals
			{
				get
				{
					return false;
				}
			}

			/// <summary>
			/// Returns the number of world transform matrices this renderable requires.
			/// </summary>
			public ushort NumWorldTransforms
			{
				get
				{
					return 1;
				}
			}

			public bool UseIdentityProjection
			{
				get
				{
					return this.mUseIdentityProjection;
				}
			}

			/// <summary>
			/// Get's whether or not to use an 'identity' view.
			/// </summary>
			public bool UseIdentityView
			{
				get
				{
					return this.mUseIdentityView;
				}
			}

			public bool PolygonModeOverrideable
			{
				get
				{
					return this.mPolygoneModeOverridable;
				}
			}

			public Quaternion WorldOrientation
			{
				get
				{
					return this.mParent.mMovable.ParentNode.DerivedOrientation;
				}
			}

			public Vector3 WorldPosition
			{
				get
				{
					return this.mParent.mMovable.ParentNode.DerivedPosition;
				}
			}

			public Vector4 GetCustomParameter( int parameter )
			{
				Vector4 retVal;
				if ( this.mCustomParameters.TryGetValue( parameter, out retVal ) )
				{
					return retVal;
				}
				else
				{
					throw new AxiomException( "Parameter at the given index was not found!\n" + "Renderable.GetCustomParameter" );
				}
			}

			public void SetCustomParameter( int parameter, Vector4 value )
			{
				this.mCustomParameters[ parameter ] = value;
			}

			/// <summary>
			/// Update a custom GpuProgramParameters constant which is derived from
			/// information only this Renderable knows.
			/// </summary>
			/// <param name="entry"></param>
			/// <param name="parameters"></param>
			public void UpdateCustomGpuParameter( GpuProgramParameters.AutoConstantEntry entry, GpuProgramParameters parameters )
			{
				//not implement now
			}

			#endregion
		}

		#endregion
	}
}
