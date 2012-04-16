#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2010 Axiom Project Team

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
using System.Text;

using Axiom.Animating;
using Axiom.Graphics;
using Axiom.Math;
using BoundingBox = Axiom.Math.AxisAlignedBox;

#endregion Namespace Declarations

namespace Axiom.Core
{
	/// <summary>
	/// Pre-transforms and batches up meshes for efficient use as instanced geometry
	///	in a scene
	/// <remarks>
	/// Shader instancing allows to save both memory and draw calls. While 
	///	StaticGeometry stores 500 times the same object in a batch to display 500 
	///	objects, this shader instancing implementation stores only 80 times the object, 
	///	and then re-uses the vertex data with different shader parameter.
	///	Although you save memory, you make more draw call. However, you still 
	///	make less draw calls than if you were rendering each object independently.
	///	Plus, you can move the batched objects independently of one another which 
	///	you cannot do with StaticGeometry.
	/// </remarks>
	/// </summary>
	public partial class InstancedGeometry
	{
		/// <summary>
		/// Struct holding geometry optimised per SubMesh / lod level, ready
		///	for copying to instances. 
		/// </summary>
		public class OptimisedSubMeshGeometry
		{
			public OptimisedSubMeshGeometry()
			{
				vertexData = null;
				indexData = null;
			}
			~OptimisedSubMeshGeometry()
			{
				vertexData = null;
				indexData = null;
			}
			public VertexData vertexData;
			public IndexData indexData;
		}
		///
		public struct SubMeshLodGeometryLink
		{
			public VertexData vertexData;
			public IndexData indexData;
		}

		/// Structure recording a queued submesh for the build
		public struct QueuedSubMesh
		{
			public SubMesh submesh;
			/// Link to LOD list of geometry, potentially optimised
			public List<SubMeshLodGeometryLink> geometryLodList;
			public String materialName;
			public Vector3 position;
			public Quaternion orientation;
			public Vector3 scale;
			/// Pre-transformed world AABB 
			public BoundingBox worldBounds;
			public int ID;
		}
		///Structure recording a queued geometry for low level builds
		public struct QueuedGeometry
		{
			SubMeshLodGeometryLink geometry;
			Vector3 position;
			Quaternion orientation;
			Vector3 scale;
			int ID;
		}

		/// <summary>
		///  A GeometryBucket is a the lowest level bucket where geometry with 
		///	the same vertex & index format is stored. It also acts as the 
		///	renderable.
		/// </summary>
		public class GeometryBucket : SimpleRenderable
		{
			/// Geometry which has been queued up pre-build (not for deallocation)
			List<QueuedGeometry> mQueuedGeometry;
			/// Pointer to the Batch
			InstancedGeometry mBatch;
			/// Pointer to parent bucket
			MaterialBucket mParent;
			/// String identifying the vertex / index format
			String mFormatString;
			/// Vertex information, includes current number of vertices
			/// committed to be a part of this bucket
			VertexData mVertexData;
			/// Index information, includes index type which limits the max
			/// number of vertices which are allowed in one bucket
			IndexData mIndexData;
			/// Size of indexes
			IndexType mIndexType;

			/// Maximum vertex indexable
			uint mMaxVertexIndex;
			///	Index of the Texcoord where the index is stored
			short mTexCoordIndex;
			BoundingBox mAABB;

			public GeometryBucket( MaterialBucket parent,
								  String formatString, VertexData vData,
								  IndexData iData )
				: base()
			{
				mParent = parent;
				mFormatString = formatString;
				mVertexData = null;
				mIndexData = null;
				mBatch = mParent.Parent.Parent.Parent;
				if ( mBatch.BaseSkeleton != null )
					SetCustomParameter( 0, new Vector4( mBatch.BaseSkeleton.BoneCount, 0, 0, 0 ) );

				mVertexData = vData.Clone( false );

				renderOperation.useIndices = true;
				renderOperation.indexData = new IndexData();

				renderOperation.indexData.indexCount = 0;
				renderOperation.indexData.indexStart = 0;
				renderOperation.vertexData = new VertexData();
				renderOperation.vertexData.vertexCount = 0;

				renderOperation.vertexData.vertexDeclaration = (VertexDeclaration)vData.vertexDeclaration.Clone();
				mIndexType = iData.indexBuffer.Type;
				// Derive the max vertices
				if ( mIndexType == IndexType.Size32 )
				{
					mMaxVertexIndex = 0xFFFFFFFF;
				}
				else
				{
					mMaxVertexIndex = 0xFFFF;
				}

				int offset = 0, tcOffset = 0;
				short texCoordOffset = 0;
				short texCoordSource = 0;
				for ( int i = 0; i < renderOperation.vertexData.vertexDeclaration.ElementCount; i++ )
				{

					if ( renderOperation.vertexData.vertexDeclaration.GetElement( i ).Semantic == VertexElementSemantic.TexCoords )
					{
						texCoordOffset++;
						texCoordSource = renderOperation.vertexData.vertexDeclaration.GetElement( i ).Source;
						tcOffset = renderOperation.vertexData.vertexDeclaration.GetElement( i ).Offset + VertexElement.GetTypeSize(
								renderOperation.vertexData.vertexDeclaration.GetElement( i ).Type );
					}
					offset += VertexElement.GetTypeSize( renderOperation.vertexData.vertexDeclaration.GetElement( i ).Type );
				}

				renderOperation.vertexData.vertexDeclaration.AddElement( texCoordSource, tcOffset, VertexElementType.Float1, VertexElementSemantic.TexCoords, texCoordOffset );

				mTexCoordIndex = texCoordOffset;
			}

			GeometryBucket( MaterialBucket parent, String formatString, GeometryBucket bucket )
				: base()
			{
				mParent = parent;
				mFormatString = formatString;
				mBatch = mParent.Parent.Parent.Parent;
				if ( mBatch.BaseSkeleton != null )
				{
					SetCustomParameter( 0, new Vector4( mBatch.BaseSkeleton.BoneCount, 0, 0, 0 ) );
				}
				renderOperation = bucket.RenderOperation;
				mVertexData = renderOperation.vertexData;
				mIndexData = renderOperation.indexData;
				this.BoundingBox = new BoundingBox( new Vector3( -10000, -10000, -10000 ), new Vector3( 10000, 10000, 10000 ) );
			}

			unsafe void CopyIndexes( void* src, void* dst, int count, int indexOffset )
			{
				if ( indexOffset == 0 )
				{
					Memory.Copy( new IntPtr( src ), new IntPtr( dst ), count );

				}
				else
				{
					//FIXME
					/*
					while( count-- != 0)
					{
						*dst++ = static_cast<T>(*src++ + indexOffset);
					}*/
				}
			}
			public override Material Material
			{
				get
				{
					return mParent.Material;
				}
			}

			public override bool CastShadows
			{
				get
				{
					return mParent.Parent.Parent.CastShadows;
				}
				set
				{
					base.CastShadows = value;
				}
			}
			public override float GetSquaredViewDepth( Camera camera )
			{
				return mParent.Parent.SquaredDistance;
			}

			public override RenderOperation RenderOperation
			{
				get
				{
					throw new NotImplementedException();
				}
			}

			public new AxisAlignedBox BoundingBox
			{
				get
				{
					return mAABB;
				}
				set
				{
					mAABB = value;
				}
			}

			public override float BoundingRadius
			{
				get
				{
					return 1;
				}
			}
		}


		public class InstancedObject
		{
			public enum TransformSpace
			{
				/// <summary>
				/// Transform is relative to the local space
				/// </summary>
				Local,
				/// <summary>
				/// Transform is relative to the space of the parent node
				/// </summary>
				Parent,
				/// <summary>
				/// Transform is relative to world space
				/// </summary>
				World
			}

			public void UpdateAnimation()
			{

			}
		}
		/// <summary>
		/// 
		/// </summary>
		public class MaterialBucket
		{
			/// Pointer to parent LODBucket
			protected LODBucket mParent;
			/// Material being used
			protected String mMaterialName;
			/// Pointer to material being used
			protected Material mMaterial;
			/// Active technique
			protected Technique mTechnique;
			protected int mLastIndex;
			/// list of Geometry Buckets in this BatchInstance
			protected List<GeometryBucket> mGeometryBucketList = new List<GeometryBucket>();
			// index to current Geometry Buckets for a given geometry format
			protected Dictionary<String, GeometryBucket> mCurrentGeometryMap = new Dictionary<string, GeometryBucket>();

			/// <summary>
			/// Add children to the render queue
			/// </summary>
			/// <param name="queue"></param>
			/// <param name="group"></param>
			/// <param name="camSquaredDistance"></param>
			public void AddRenderables( RenderQueue queue, RenderQueueGroupID group, float camSquaredDistance )
			{
				// Determine the current material technique
				mTechnique = mMaterial.GetBestTechnique( mMaterial.GetLodIndex( camSquaredDistance ) );
				foreach ( GeometryBucket iter in mGeometryBucketList )
				{
					queue.AddRenderable( iter, group );
				}
			}

			/// <summary>
			/// Get the material for this bucket
			/// </summary>
			public Material Material
			{
				get
				{
					return mMaterial;
				}
			}

			/// <summary>
			/// Get the current Technique
			/// </summary>
			public Technique CurrentTechnique
			{
				get
				{
					return mTechnique;
				}
			}
			/// Get a packed string identifying the geometry format
			//TODO
			protected String GetGeometryFormatString( SubMeshLodGeometryLink geom )
			{
				return "";
			}

			public LODBucket Parent
			{
				get
				{
					return mParent;
				}
			}
		}

		/// <summary>
		/// A LODBucket is a collection of smaller buckets with the same LOD. 
		/// <remarks>
		/// LOD refers to Mesh LOD here. Material LOD can change separately
		///	at the next bucket down from this.
		/// </remarks>
		/// </summary>
		public class LODBucket
		{
			/// Pointer to parent BatchInstance
			protected BatchInstance mParent;
			/// LOD level (0 == full LOD)
			protected ushort mLod;
			/// distance at which this LOD starts to apply (squared)
			protected float mSquaredDistance;
			/// Lookup of Material Buckets in this BatchInstance
			protected Dictionary<string, MaterialBucket> mMaterialBucketMap = new Dictionary<string, MaterialBucket>();
			/// Geometry queued for a single LOD (deallocated here)
			protected List<QueuedGeometry> mQueuedGeometryList = new List<QueuedGeometry>();

			/// <summary>
			/// Add children to the render queue
			/// </summary>
			/// <param name="queue"></param>
			/// <param name="group"></param>
			/// <param name="camSquaredDistance"></param>
			public void AddRenderables( RenderQueue queue, RenderQueueGroupID group, float camSquaredDistance )
			{
				foreach ( MaterialBucket iter in mMaterialBucketMap.Values )
				{
					iter.AddRenderables( queue, group, camSquaredDistance );
				}
			}
			public BatchInstance Parent
			{
				get
				{
					return mParent;
				}
			}

			/// <summary>
			/// Get the lod index
			/// </summary>
			public ushort Lod
			{
				get
				{
					return mLod;
				}
			}
			/// <summary>
			/// Get the lod squared distance
			/// </summary>
			public float SquaredDistance
			{
				get
				{
					return mSquaredDistance;
				}
			}
		}

		public class BatchInstance : MovableObject
		{
			/// Parent static geometry
			protected InstancedGeometry mParent;
			/// Scene manager link
			protected SceneManager mSceneMgr;
			/// Scene node
			protected SceneNode mNode;
			/// Local list of queued meshes (not used for deallocation)
			protected List<QueuedSubMesh> mQueuedSubMeshes = new List<QueuedSubMesh>();
			/// Unique identifier for the BatchInstance
			protected uint mBatchInstanceID;

			protected Dictionary<int, InstancedObject> mInstancesMap = new Dictionary<int, InstancedObject>();

			/// List of LOD buckets			
			protected List<LODBucket> mLodBucketList = new List<LODBucket>();

			/// LOD distances (squared) as built up - use the max at each level
			public List<float> mLodSquaredDistances = new List<float>();
			/// Local AABB relative to BatchInstance centre
			public BoundingBox mAABB;
			/// Local bounding radius
			public float mBoundingRadius;
			/// The current lod level, as determined from the last camera
			public ushort mCurrentLod;
			/// Current camera distance, passed on to do material lod later
			public float mCamDistanceSquared;

			public override void UpdateRenderQueue( RenderQueue queue )
			{
				foreach ( InstancedObject iter in mInstancesMap.Values )
				{
					iter.UpdateAnimation();

				}

				mLodBucketList[ mCurrentLod ].AddRenderables( queue, base.renderQueueID, mCamDistanceSquared );
			}

			public override void NotifyCurrentCamera( Camera cam )
			{
				// Calculate squared view depth
				Vector3 diff = cam.LodCamera.DerivedPosition;
				float squaredDepth = diff.LengthSquared;

				// Determine whether to still render
				float renderingDist = mParent.RenderingDistance;
				if ( renderingDist > 0 )
				{
					// Max distance to still render
					float maxDist = renderingDist + mBoundingRadius;
					if ( squaredDepth > Utility.Sqr( maxDist ) )
					{
						beyondFarDistance = true;
						return;
					}
				}

				beyondFarDistance = false;

				// Distance from the edge of the bounding sphere
				mCamDistanceSquared = squaredDepth - mBoundingRadius * mBoundingRadius;
				// Clamp to 0
				mCamDistanceSquared = Utility.Max( 0.0f, mCamDistanceSquared );

				// Determine active lod
				mCurrentLod = (ushort)( mLodSquaredDistances.Count - 1 );
				Debug.Assert( mLodSquaredDistances.Count != 0 );
				mCurrentLod = (ushort)( mLodSquaredDistances.Count - 1 );

				for ( ushort i = 0; i < mLodSquaredDistances.Count; ++i )
				{
					if ( mLodSquaredDistances[ i ] > mCamDistanceSquared )
					{
						mCurrentLod = (ushort)( i - 1 );
						break;
					}
				}
			}

			public override BoundingBox BoundingBox
			{
				get
				{
					return mAABB;
				}
			}

			public override float BoundingRadius
			{
				get
				{
					return mBoundingRadius;
				}
			}

			// more fields can be added in subclasses
			public InstancedGeometry Parent
			{
				get
				{
					return mParent;
				}
			}

			/// <summary>
			/// Get the BatchInstance ID of this BatchInstance
			/// </summary>
			public uint ID
			{
				get
				{
					return mBatchInstanceID;
				}
			}
		}

		//Fields

		// General state & settings
		protected SceneManager mOwner;
		protected String mName;
		protected bool mBuilt;
		protected float mUpperDistance;
		protected float mSquaredUpperDistance;
		protected bool mCastShadows;
		protected Vector3 mBatchInstanceDimensions;
		protected Vector3 mHalfBatchInstanceDimensions;
		protected Vector3 mOrigin;
		protected bool mVisible;
		/// The render queue to use when rendering this object
		byte mRenderQueueID;
		/// Flags whether the RenderQueue's default should be used.
		bool mRenderQueueIDSet;
		/// number of objects in the batch
		int mObjectCount;


		BatchInstance mInstancedGeometryInstance;
		/// <summary>
		/// this is just a pointer to the base skeleton that will be used for each animated object in the batches
		/// This pointer has a value only during the creation of the InstancedGeometry
		/// </summary>
		Skeleton mBaseSkeleton;
		SkeletonInstance mSkeletonInstance;
		/// <summary>
		/// This is the main animation state. All "objects" in the batch will use an instance of this animation state
		/// </summary>
		AnimationStateSet mAnimationState;

		List<QueuedSubMesh> mQueuedSubMeshes = new List<QueuedSubMesh>();
		/// List of geometry which has been optimised for SubMesh use
		/// This is the primary storage used for cleaning up later
		List<OptimisedSubMeshGeometry> mOptimisedSubMeshGeometryList = new List<OptimisedSubMeshGeometry>();

		/// <summary>
		/// Cached links from SubMeshes to (potentially optimised) geometry
		///	This is not used for deletion since the lookup may reference
		///	original vertex data
		/// </summary>
		Dictionary<SubMesh, List<SubMeshLodGeometryLink>> mSubMeshGeometryLookup = new Dictionary<SubMesh, List<SubMeshLodGeometryLink>>();

		/// <summary>
		/// Map of BatchInstances
		/// </summary>
		Dictionary<int, BatchInstance> mBatchInstanceMap = new Dictionary<int, BatchInstance>();
		/// <summary>
		/// This vector stores all the renderOperation used in the batch. 
		/// </summary>
		List<RenderOperation> mRenderOps;

		const int BatchInstance_RANGE = 1024;
		const int BatchInstance_HALF_RANGE = 512;
		const int BatchInstance_MAX_INDEX = 511;
		const int BatchInstance_MIN_INDEX = -512;

		public InstancedGeometry( SceneManager owner, String name )
		{
			mOwner = owner;
			mName = name;
			mBuilt = false;
			mUpperDistance = 0.0f;
			mSquaredUpperDistance = 0.0f;
			mCastShadows = false;
			mBatchInstanceDimensions = new Vector3( 1000, 1000, 1000 );
			mHalfBatchInstanceDimensions = new Vector3( 500, 500, 500 );
			mOrigin = new Vector3( 0, 0, 0 );
			mVisible = true;
			mRenderQueueID = (byte)RenderQueueGroupID.Main;
			mRenderQueueIDSet = false;
			mObjectCount = 0;
			mInstancedGeometryInstance = null;
			mSkeletonInstance = null;
			mBaseSkeleton = null;
		}

		/// <summary>
		/// Adds an Entity to the static geometry.
		/// <remarks>
		/// This method takes an existing Entity and adds its details to the 
		///	list of	elements to include when building. Note that the Entity
		///	itself is not copied or referenced in this method; an Entity is 
		///	passed simply so that you can change the materials of attached 
		///	SubEntity objects if you want. You can add the same Entity 
		///	instance multiple times with different material settings 
		///	completely safely, and destroy the Entity before destroying 
		///	this InstancedGeometry if you like. The Entity passed in is simply 
		/// 
		/// Must be called before 'Build'.
		/// </remarks>
		/// </summary>
		/// <param name="ent">The Entity to use as a definition (the Mesh and Materials 
		///	referenced will be recorded for the build call).</param>
		/// <param name="position">The world position at which to add this Entity</param>
		/// <param name="orientation">The world orientation at which to add this Entity</param>
		/// <param name="scale"></param>
		public virtual void AddEntity( Entity ent, Vector3 position, Quaternion orientation, Vector3 scale )
		{
			Mesh msh = ent.Mesh;

			// Validate
			if ( msh.IsLodManual )
			{
				LogManager.Instance.Write( "(InstancedGeometry): Manual LOD is not supported. Using only highest LOD level for mesh " + msh.Name );
			}

			//get the skeleton of the entity, if that's not already done
			if ( ent.Mesh.Skeleton != null && mBaseSkeleton == null )
			{
				mBaseSkeleton = ent.Mesh.Skeleton;
				mSkeletonInstance = new SkeletonInstance( mBaseSkeleton );
				mSkeletonInstance.Load();
				mAnimationState = ent.GetAllAnimationStates();
			}

			BoundingBox sharedWorldBounds;
			// queue this entities submeshes and choice of material
			// also build the lists of geometry to be used for the source of lods


			for ( int i = 0; i < ent.SubEntityCount; ++i )
			{
				SubEntity se = ent.GetSubEntity( i );
				QueuedSubMesh q = new QueuedSubMesh();

				// Get the geometry for this SubMesh
				q.submesh = se.SubMesh;
				q.geometryLodList = DetermineGeometry( q.submesh );
				q.materialName = se.MaterialName;
				q.orientation = orientation;
				q.position = position;
				q.scale = scale;
				q.ID = mObjectCount;
			}

			mObjectCount++;
		}
		public void AddEntity( Entity ent, Vector3 position )
		{
			AddEntity( ent, position, Quaternion.Identity, Vector3.UnitScale );
		}

		public void AddEntity( Entity ent, Vector3 position, Quaternion orientation )
		{
			AddEntity( ent, position, orientation, Vector3.UnitScale );
		}

		public void AddEntity( Entity ent, Vector3 position, Vector3 scale )
		{
			AddEntity( ent, position, Quaternion.Identity, scale );
		}
		/// <summary>
		/// adds all the Entity objects attached to a SceneNode and all it's
		///	children to the static geometry.
		/// <remarks>
		/// This method performs just like addEntity, except it adds all the 
		///	entities attached to an entire sub-tree to the geometry. 
		///	The position / orientation / scale parameters are taken from the
		///	node structure instead of being specified manually. 
		/// </remarks>
		/// </summary>
		/// <param name="node"></param>
		public virtual void AddSceneNode( SceneNode node )
		{
			// Iterate through all attached object
			foreach ( MovableObject iter in node.Objects )
			{
				if ( iter.MovableType == "Entity" )
				{
					AddEntity( (Entity)iter,
								node.DerivedPosition,
								node.DerivedOrientation,
								node.DerivedScale );
				}
			}

			// Iterate through all the child-nodes
			foreach ( Node iter in node.Children )
			{
				SceneNode sceneNode = (SceneNode)iter;
				AddSceneNode( sceneNode );
			}
		}

		/// <summary>
		/// Look up or calculate the geometry data to use for this SubMesh
		/// </summary>
		/// <param name="sm"></param>
		/// <returns></returns>
		public List<SubMeshLodGeometryLink> DetermineGeometry( SubMesh sm )
		{
			// First, determine if we've already seen this submesh before
			if ( mSubMeshGeometryLookup.ContainsKey( sm ) )
			{
				return mSubMeshGeometryLookup[ sm ];
			}

			// Otherwise, we have to create a new one
			List<SubMeshLodGeometryLink> lodList = new List<SubMeshLodGeometryLink>();
			mSubMeshGeometryLookup[ sm ] = lodList;

			int numLods = sm.Parent.IsLodManual ? 1 : sm.Parent.LodLevelCount;
			lodList.Capacity = numLods;

			for ( int lod = 0; lod < numLods; ++lod )
			{
				SubMeshLodGeometryLink geomLink = lodList[ lod ];
				IndexData lodIndexData;
				if ( lod == 0 )
				{
					lodIndexData = sm.IndexData;
				}
				else
				{
					lodIndexData = sm.LodFaceList[ lod - 1 ];
				}
				// Can use the original mesh geometry?
				if ( sm.useSharedVertices )
				{
					if ( sm.Parent.SubMeshCount == 1 )
					{
						// Ok, this is actually our own anyway
						geomLink.vertexData = sm.Parent.SharedVertexData;
						geomLink.indexData = lodIndexData;
					}
					else
					{
						// We have to split it
						SplitGeometry( sm.Parent.SharedVertexData,
							lodIndexData, ref geomLink );
					}
				}
				else
				{
					if ( lod == 0 )
					{
						// Ok, we can use the existing geometry; should be in full
						// use by just this SubMesh
						geomLink.vertexData = sm.vertexData;
						geomLink.indexData = sm.indexData;
					}
					else
					{
						// We have to split it
						SplitGeometry( sm.vertexData,
							lodIndexData, ref geomLink );
					}
				}

				Debug.Assert( geomLink.vertexData.vertexStart == 0, "Cannot use vertexStart > 0 on indexed geometry due to rendersystem incompatibilities - see the docs!" );
			}

			return lodList;
		}

		/// <summary>
		/// Split some shared geometry into dedicated geometry.
		/// </summary>
		/// <param name="vd"></param>
		/// <param name="id"></param>
		/// <param name="targetGeomLink"></param>
		public unsafe void SplitGeometry( VertexData vd, IndexData id, ref SubMeshLodGeometryLink targetGeomLink )
		{
			// Firstly we need to scan to see how many vertices are being used
			// and while we're at it, build the remap we can use later
			bool use32bitIndexes = id.indexBuffer.Type == IndexType.Size32;

			Dictionary<int, int> indexRemap = new Dictionary<int, int>();

			if ( use32bitIndexes )
			{
				var p32 = id.indexBuffer.Lock( id.indexStart, id.indexCount * id.indexBuffer.IndexSize,	BufferLocking.ReadOnly );
				BuildIndexRemap( p32, id.indexCount, ref indexRemap );
				id.indexBuffer.Unlock();
			}
			else
			{
				var p16 = (ushort*)id.indexBuffer.Lock(	id.indexStart, id.indexCount * id.indexBuffer.IndexSize, BufferLocking.ReadOnly );
				BuildIndexRemap( p16, id.indexCount, ref indexRemap );
				id.indexBuffer.Unlock();
			}

			if ( indexRemap.Count == vd.vertexCount )
			{
				// ha, complete usage after all
				targetGeomLink.vertexData = vd;
				targetGeomLink.indexData = id;
				return;
			}

			// Create the new vertex data records
			targetGeomLink.vertexData = vd.Clone( false );
			// Convenience
			VertexData newvd = targetGeomLink.vertexData;
			//IndexData* newid = targetGeomLink->indexData;
			// Update the vertex count
			newvd.vertexCount = indexRemap.Count;

			int numvbufs = vd.vertexBufferBinding.BindingCount;

			// Copy buffers from old to new
			for ( int b = 0; b < numvbufs; ++b )
			{
				// Lock old buffer
				HardwareVertexBuffer oldBuf = vd.vertexBufferBinding.GetBuffer( (short)b );
				// Create new buffer
				HardwareVertexBuffer newBuf =
					HardwareBufferManager.Instance.CreateVertexBuffer(
						oldBuf.VertexDeclaration,
						indexRemap.Count,
						BufferUsage.Static );
				// rebind
				newvd.vertexBufferBinding.SetBinding( (short)b, newBuf );

				// Copy all the elements of the buffer across, by iterating over
				// the IndexRemap which describes how to move the old vertices
				// to the new ones. By nature of the map the remap is in order of
				// indexes in the old buffer, but note that we're not guaranteed to
				// address every vertex (which is kinda why we're here)
				byte* pSrcBase = (byte*)oldBuf.Lock( BufferLocking.ReadOnly );
				byte* pDstBase = (byte*)newBuf.Lock( BufferLocking.Discard );
				int vertexSize = oldBuf.VertexSize;
				// Buffers should be the same size
				Debug.Assert( vertexSize == newBuf.VertexSize );

				foreach ( KeyValuePair<int, int> r in indexRemap )
				{
					Debug.Assert( r.Key < oldBuf.VertexCount );
					Debug.Assert( r.Value < newBuf.VertexCount );

					void* pSrc = pSrcBase + r.Key * vertexSize;
					void* pDst = pDstBase + r.Value * vertexSize;
					IntPtr pSrcPtr = new IntPtr( pSrc );
					IntPtr pDstPtr = new IntPtr( pDst );
					Memory.Copy( pDstPtr, pSrcPtr, vertexSize );
				}
				// unlock
				oldBuf.Unlock();
				newBuf.Unlock();

			}

			// Now create a new index buffer
			HardwareIndexBuffer ibuf = HardwareBufferManager.Instance.CreateIndexBuffer(
					id.indexBuffer.Type, id.indexCount, BufferUsage.Static );

			if ( use32bitIndexes )
			{
				uint* pSrc32, pDst32;
				pSrc32 = (uint*)id.indexBuffer.Lock(
					id.indexStart, id.indexCount * id.indexBuffer.IndexSize, BufferLocking.ReadOnly );
				pDst32 = (uint*)ibuf.Lock( BufferLocking.Discard );
				RemapIndexes( pSrc32, pDst32, ref indexRemap, id.indexCount );
				id.indexBuffer.Unlock();
				ibuf.Unlock();
			}
			else
			{
				ushort* pSrc16, pDst16;
				pSrc16 = (ushort*)id.indexBuffer.Lock(
					id.indexStart, id.indexCount * id.indexBuffer.IndexSize, BufferLocking.ReadOnly );
				pDst16 = (ushort*)ibuf.Lock( BufferLocking.Discard );
				RemapIndexes( pSrc16, pDst16, ref indexRemap, id.indexCount );
				id.indexBuffer.Unlock();
				ibuf.Unlock();
			}

			targetGeomLink.indexData = new IndexData();
			targetGeomLink.indexData.indexStart = 0;
			targetGeomLink.indexData.indexCount = id.indexCount;
			targetGeomLink.indexData.indexBuffer = ibuf;

			// Store optimised geometry for deallocation later
			OptimisedSubMeshGeometry optGeom = new OptimisedSubMeshGeometry();
			optGeom.indexData = targetGeomLink.indexData;
			optGeom.vertexData = targetGeomLink.vertexData;
			mOptimisedSubMeshGeometryList.Add( optGeom );
		}

		internal unsafe void BuildIndexRemap( uint* pBuffer, int numIndexes, ref Dictionary<int, int> remap )
		{
			remap.Clear();
			for ( int i = 0; i < numIndexes; ++i )
			{
				// use insert since duplicates are silently discarded
				remap.Add( (int)*pBuffer++, remap.Count );
				// this will have mapped oldindex -> new index IF oldindex
				// wasn't already there
			}
		}

		internal unsafe void BuildIndexRemap( BufferBase pBuffer, int numIndexes, ref Dictionary<int, int> remap )
		{
			remap.Clear();
			for ( int i = 0; i < numIndexes; ++i )
			{
				// use insert since duplicates are silently discarded
				remap.Add( pBuffer++, remap.Count );
				// this will have mapped oldindex -> new index IF oldindex
				// wasn't already there
			}
		}

		internal unsafe void RemapIndexes( uint* src, uint* dst, ref Dictionary<int, int> remap, int numIndexes )
		{
			for ( int i = 0; i < numIndexes; ++i )
			{
				int searchIdx = (int)*src++;
				// look up original and map to target
				Debug.Assert( remap.ContainsKey( searchIdx ) );

				*dst++ = (uint)remap[ searchIdx ];
			}
		}

		internal unsafe void RemapIndexes( ushort* src, ushort* dst, ref Dictionary<int, int> remap, int numIndexes )
		{
			for ( int i = 0; i < numIndexes; ++i )
			{
				int searchIdx = (int)*src++;
				// look up original and map to target
				Debug.Assert( remap.ContainsKey( searchIdx ) );

				*dst++ = (ushort)remap[ searchIdx ];
			}
		}

		/// <summary>
		/// Get the name of this object
		/// </summary>
		public String Name
		{
			get
			{
				return mName;
			}
		}

		/// <summary>
		/// Return the skeleton that is shared by all instanced objects.
		/// </summary>
		public Skeleton BaseSkeleton
		{
			get
			{
				return mBaseSkeleton;
			}
		}

		/// <summary>
		/// Return the animation state that will be cloned each time an InstancedObject is made
		/// </summary>
		public AnimationStateSet BaseAnimationState
		{
			get
			{
				return mAnimationState;
			}
		}

		/// <summary>
		/// Gets the distance at which batches are no longer rendered.
		/// </summary>
		public virtual float RenderingDistance
		{
			get
			{
				return mUpperDistance;
			}
		}

		/// <summary>
		/// Gets the squared distance at which batches are no longer rendered. 
		/// </summary>
		public virtual float SquaredRenderingDistance
		{
			get
			{
				return mSquaredUpperDistance;
			}
			set
			{
				mUpperDistance = value;
				mSquaredUpperDistance = mUpperDistance * mUpperDistance;
			}
		}

		/// <summary>
		/// Hides or shows all the batches.
		/// </summary>
		public virtual bool Visible
		{
			get
			{
				return mVisible;
			}
			set
			{
				mVisible = value;
				// tell any existing BatchInstances
				foreach ( BatchInstance ri in mBatchInstanceMap.Values )
				{
					ri.IsVisible = value;
				}
			}
		}

		/// <summary>
		/// Gets/Sets whether this geometry should cast shadows.
		/// <remarks>
		/// No matter what the settings on the original entities,
		//	the InstancedGeometry class defaults to not casting shadows. 
		///	This is because, being static, unless you have moving lights
		///	you'd be better to use precalculated shadows of some sort.
		///	However, if you need them, you can enable them using this
		///	method. If the SceneManager is set up to use stencil shadows,
		///	edge lists will be copied from the underlying meshes on build.
		///	It is essential that all meshes support stencil shadows in this
		///	case.
		/// </remarks>
		/// </summary>
		public virtual bool CastShadows
		{
			get
			{
				return mCastShadows;
			}
			set
			{
				mCastShadows = value;
				// tell any existing BatchInstances
				foreach ( BatchInstance ri in mBatchInstanceMap.Values )
				{
					ri.CastShadows = value;
				}
			}
		}

		/// <summary>
		///  Gets/Sets the size of a single BatchInstance of geometry.
		/// </summary>
		/// <remarks>
		/// This method allows you to configure the physical world size of 
		/// each BatchInstance, so you can balance culling against batch size. Entities
		/// will be fitted within the batch they most closely fit, and the 
		/// eventual bounds of each batch may well be slightly larger than this
		/// if they overlap a little. The default is Vector3(1000, 1000, 1000).
		/// </remarks>
		/// <remarks>
		/// Must be called before 'build'.
		/// </remarks>
		public virtual Vector3 BatchInstanceDimensions
		{
			get
			{
				return mBatchInstanceDimensions;
			}
			set
			{
				mBatchInstanceDimensions = value;
				mHalfBatchInstanceDimensions = value * 0.5;
			}
		}

		/// <summary>
		///  Gets/Sets the origin of the geometry.
		/// </summary>
		/// <remarks>
		/// This method allows you to configure the world centre of the geometry,
		/// thus the place which all BatchInstances surround. You probably don't need 
		/// to mess with this unless you have a seriously large world, since the
		/// default set up can handle an area 1024 * mBatchInstanceDimensions, and 
		/// the sparseness of population is no issue when it comes to rendering.
		/// The default is Vector3(0,0,0).
		/// </remarks>
		/// <remarks>
		/// Must be called before 'build'.
		/// </remarks>
		public virtual Vector3 Origin
		{
			get
			{
				return mOrigin;
			}
			set
			{
				mOrigin = value;
			}
		}

		/// <summary>
		/// Return the total number of object that are in all the batches
		/// </summary>
		public int ObjectCount
		{
			get
			{
				return mObjectCount;
			}
		}
	}
}
