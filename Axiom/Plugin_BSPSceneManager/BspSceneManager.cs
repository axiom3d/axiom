#region LGPL License
/*
Axiom Game Engine Library
Copyright (C) 2003  Axiom Project Team

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

using System;
using System.IO;
using System.Collections;

using Axiom.Core;
using Axiom.MathLib;
using Axiom.Graphics;
using Axiom.Exceptions;
using Axiom.Collections;
using Axiom.MathLib.Collections;

namespace Axiom.SceneManagers.Bsp
{
	/// <summary>
	///		Specialisation of the SceneManager class to deal with indoor scenes based on a BSP tree.
	///	</summary>
	///	<remarks>
	///		This class refines the behaviour of the default SceneManager to manage
	///		a scene whose bulk of geometry is made up of an indoor environment which
	///		is organised by a Binary Space Partition (BSP) tree. 
	///		<p/>
	///		A BSP tree progressively subdivides the space using planes which are the nodes of the tree.
	///		At some point we stop subdividing and everything in the remaining space is part of a 'leaf' which
	///		contains a number of polygons. Typically we traverse the tree to locate the leaf in which a
	///		point in space is (say the camera origin) and work from there. A second structure, the
	///		Potentially Visible Set, tells us which other leaves can been seen from this
	///		leaf, and we test their bounding boxes against the camera frustum to see which
	///		we need to draw. Leaves are also a good place to start for collision detection since
	///		they divide the level into discrete areas for testing.
	///		<p/>
	///		This BSP and PVS technique has been made famous by engines such as Quake and Unreal. Ogre
	///		provides support for loading Quake3 level files to populate your world through this class,
	///		by calling the BspSceneManager::setWorldGeometry. Note that this interface is made
	///		available at the top level of the SceneManager class so you don't have to write your code
	///		specifically for this class - just call Root::getSceneManager passing a SceneType of ST_INDOOR
	///		and in the current implementation you will get a BspSceneManager silently disguised as a
	///		standard SceneManager.
	/// </remarks>
	public class BspSceneManager : SceneManager 
	{
		#region Protected members
		protected BspLevel level;
		protected ArrayList faceGroupSet = new ArrayList();
		protected RenderOperation renderOp = new RenderOperation();
		protected RenderOperation patchOp = new RenderOperation();
		protected bool showNodeAABs;
		protected RenderOperation aaBGeometry = new RenderOperation();


		protected Map matFaceGroupMap = new Map();
		protected SceneObjectCollection objectsForRendering = new SceneObjectCollection();
		#endregion

		#region Public properties
		public BspLevel Level
		{
			get { return level; }
		}

		public bool ShowNodeBoxes
		{
			get { return showNodeAABs; }
			set { showNodeAABs = value; }
		}
		#endregion

		#region Constructor
		public BspSceneManager()
		{
			// Set features for debugging render
			showNodeAABs = false;

			// No sky by default
			isSkyPlaneEnabled = false;
			isSkyBoxEnabled = false;
			isSkyDomeEnabled = false;

			level = null;
		}
		#endregion

		#region Public methods
		/// <summary>
		///		Specialised from SceneManager to support Quake3 bsp files.
		/// </summary>
		public override void LoadWorldGeometry(string filename)
		{
			if(Path.GetExtension(filename) != ".bsp")
				throw new Exception("Unable to load world geometry. Invalid extension (must be .bsp).");

			// Load using resource manager
			level = BspResourceManager.Instance.Load(filename);

			// Init static render operation
			renderOp.vertexData = new VertexData();
			renderOp.vertexData = level.VertexData;
			
			// index data is per-frame
			renderOp.indexData = new IndexData();
			renderOp.indexData.indexStart = 0;
			renderOp.indexData.indexCount = 0;

			// Create enough index space to render whole level
			renderOp.indexData.indexBuffer = HardwareBufferManager.Instance.CreateIndexBuffer(
				IndexType.Size32,
				level.NumIndexes,
				BufferUsage.Dynamic, false
				);
			renderOp.operationType = RenderMode.TriangleList;
			renderOp.useIndices = true;
		}

		/// <summary>
		///		Specialised to suggest viewpoints.
		/// </summary>
		public override ViewPoint GetSuggestedViewpoint(bool random)
		{
			
			if((level == null) || (level.PlayerStarts.Length == 0))
			{
				return base.GetSuggestedViewpoint(random);
			}
			else
			{
				if(random)
					return level.PlayerStarts[(int) (MathUtil.UnitRandom() * level.PlayerStarts.Length)];
				else
					return level.PlayerStarts[0];

			}
		}

		/// <summary>
		///		Overriden from SceneManager.
		/// </summary>
		public override void FindVisibleObjects(Camera camera)
		{
			// Clear unique list of movables for this frame
			objectsForRendering.Clear();

			// Walk the tree, tag static geometry, return camera's node (for info only)
			// Movables are now added to the render queue in processVisibleLeaf
			BspNode cameraNode = WalkTree(camera);
		}

		/// <summary>
		///		Creates a specialized <see cref="Plugin_BSPSceneManager.BspSceneNode"/>.
		/// </summary>
		public override SceneNode CreateSceneNode()
		{
			BspSceneNode node = new BspSceneNode(this);
			this.sceneNodeList[node.Name] = node;

			return node;
		}

		/// <summary>
		///		Creates a specialized <see cref="Plugin_BSPSceneManager.BspSceneNode"/>.
		/// </summary>
		public override SceneNode CreateSceneNode(string name)
		{
			BspSceneNode node = new BspSceneNode(this, name);
			this.sceneNodeList[node.Name] = node;

			return node;
		}

		/// <summary>
		///		Internal method for tagging <see cref="Plugin_BSPSceneManager.BspNode"/'s with objects which intersect them.
		/// </summary>
		public void NotifyObjectMoved(SceneObject obj, Vector3 pos)
		{
			level.NotifyObjectMoved(obj, pos);
		}

		/// <summary>
		///		Internal method for notifying the level that an object has been detached from a node.
		/// </summary>
		public void NotifyObjectDetached(SceneObject obj)
		{
			level.NotifyObjectDetached(obj);
		}

		// TODO: Scene queries.
		/// <summary>
		///		Creates an AxisAlignedBoxSceneQuery for this scene manager. 
		/// </summary>
		/// <remarks>
		///		This method creates a new instance of a query object for this scene manager, 
		///		for an axis aligned box region. See SceneQuery and AxisAlignedBoxSceneQuery 
		///		for full details.
		///		<p/>
		///		The instance returned from this method must be destroyed by calling
		///		SceneManager.DestroyQuery when it is no longer required.
		/// </remarks>
		/// <param name="box">Details of the box which describes the region for this query.</param>
		/*public virtual AxisAlignedBoxSceneQuery CreateAABBQuery(AxisAlignedBox box)
		{
			return CreateAABBQuery(box, 0xFFFFFFFF);
		}

		/// <summary>
		///		Creates an AxisAlignedBoxSceneQuery for this scene manager. 
		/// </summary>
		/// <remarks>
		///		This method creates a new instance of a query object for this scene manager, 
		///		for an axis aligned box region. See SceneQuery and AxisAlignedBoxSceneQuery 
		///		for full details.
		///		<p/>
		///		The instance returned from this method must be destroyed by calling
		///		SceneManager.DestroyQuery when it is no longer required.
		/// </remarks>
		/// <param name="box">Details of the box which describes the region for this query.</param>
		/// <param name="mask">The query mask to apply to this query; can be used to filter out certain objects; see SceneQuery for details.</param>
		public virtual AxisAlignedBoxSceneQuery CreateAABBQuery(AxisAlignedBox box, ulong mask)
		{
			// TODO:
			return null;
		}

		/// <summary>
		///		Creates a SphereSceneQuery for this scene manager. 
		/// </summary>
		/// <remarks>
		/// 	This method creates a new instance of a query object for this scene manager, 
		///		for a spherical region. See SceneQuery and SphereSceneQuery 
		///		for full details.
		///		<p/>
		///		The instance returned from this method must be destroyed by calling
		///		SceneManager.DestroyQuery when it is no longer required.
		/// </remarks>
		/// <param name="sphere">Details of the sphere which describes the region for this query.</param>
		public virtual SphereSceneQuery CreateSphereQuery(Sphere sphere)
		{
			return CreateSphereQuery(sphere, 0xFFFFFFFF);
		}

		/// <summary>
		///		Creates a SphereSceneQuery for this scene manager. 
		/// </summary>
		/// <remarks>
		/// 	This method creates a new instance of a query object for this scene manager, 
		///		for a spherical region. See SceneQuery and SphereSceneQuery 
		///		for full details.
		///		<p/>
		///		The instance returned from this method must be destroyed by calling
		///		SceneManager.DestroyQuery when it is no longer required.
		/// </remarks>
		/// <param name="sphere">Details of the sphere which describes the region for this query.</param>
		/// <param name="mask">The query mask to apply to this query; can be used to filter out	certain objects; see SceneQuery for details.</param>
		public virtual SphereSceneQuery CreateSphereQuery(Sphere sphere, ulong mask)
		{
			// TODO:
			return null;
		}

		/// <summary>
		///		Creates a RaySceneQuery for this scene manager. 
		/// </summary>
		/// <remarks>
		///		This method creates a new instance of a query object for this scene manager, 
		///		looking for objects which fall along a ray. See SceneQuery and RaySceneQuery 
		///		for full details.
		///		<p/>
		///		The instance returned from this method must be destroyed by calling
		///		SceneManager.DestroyQuery when it is no longer required.
		/// </remarks>
		/// <param name="ray">Details of the ray which describes the region for this query.</param>
		/// <param name="mask">The query mask to apply to this query; can be used to filter out certain objects; see SceneQuery for details.</param>
		public virtual RaySceneQuery CreateRayQuery(Ray ray)
		{
			return CreateRayQuery(ray, 0xFFFFFFFF);
		}

		/// <summary>
		///		Creates a RaySceneQuery for this scene manager. 
		/// </summary>
		/// <remarks>
		///		This method creates a new instance of a query object for this scene manager, 
		///		looking for objects which fall along a ray. See SceneQuery and RaySceneQuery 
		///		for full details.
		///		<p/>
		///		The instance returned from this method must be destroyed by calling
		///		SceneManager.DestroyQuery when it is no longer required.
		/// </remarks>
		/// <param name="ray">Details of the ray which describes the region for this query.</param>
		/// <param name="mask">The query mask to apply to this query; can be used to filter out certain objects; see SceneQuery for details.</param>
		public virtual RaySceneQuery CreateRayQuery(Ray ray, ulong mask)
		{
			// TODO:
			return null;
		}

		/// <summary>
		///		Creates an IntersectionSceneQuery for this scene manager. 
		/// </summary>
		/// <remarks>
		///		This method creates a new instance of a query object for locating
		///		intersecting objects. See SceneQuery and IntersectionSceneQuery
		///		for full details.
		///		<p/>
		///		The instance returned from this method must be destroyed by calling
		///		SceneManager.DestroyQuery when it is no longer required.
		/// </remarks>
		/// <param name="mask">The query mask to apply to this query; can be used to filter out certain objects; see SceneQuery for details.</param>
		public virtual IntersectionSceneQuery CreateIntersectionQuery()
		{
			return CreateIntersectionQuery(0xFFFFFFFF);
		}

		/// <summary>
		///		Creates an IntersectionSceneQuery for this scene manager. 
		/// </summary>
		/// <remarks>
		///		This method creates a new instance of a query object for locating
		///		intersecting objects. See SceneQuery and IntersectionSceneQuery
		///		for full details.
		///		<p/>
		///		The instance returned from this method must be destroyed by calling
		///		SceneManager.DestroyQuery when it is no longer required.
		/// </remarks>
		/// <param name="mask">The query mask to apply to this query; can be used to filter out certain objects; see SceneQuery for details.</param>
		public virtual IntersectionSceneQuery CreateIntersectionQuery(ulong mask)
		{
			BspIntersectionSceneQuery q= new BspIntersectionSceneQuery(this);
			q.QueryMask = mask;

			return q;
		}*/
		#endregion

		#region Protected methods
		/// <summary>
		///		Walks the BSP tree looking for the node which the camera is in, and tags any geometry 
		///		which is in a visible leaf for later processing.
		/// </summary>
		protected BspNode WalkTree(Camera camera)
		{
			// Locate the leaf node where the camera is located
			BspNode cameraNode = level.FindLeaf(camera.DerivedPosition);

			matFaceGroupMap.Clear();
			faceGroupSet.Clear();

			// Scan through all the other leaf nodes looking for visibles
			int i = level.NumNodes - level.LeafStart;
			BspNode node = level.Nodes[level.LeafStart];
			
			while(i-- > 0)
			{
				if(level.IsLeafVisible(cameraNode, node))
				{
					// Visible according to PVS, check bounding box against frustum
					FrustumPlane plane;

					if(camera.IsObjectVisible(node.BoundingBox, out plane))
					{
						ProcessVisibleLeaf(node, camera);

						if(showNodeAABs)
							AddBoundingBox(node.BoundingBox, true);
					}
				}

				node = level.Nodes[level.LeafStart + i];
			}

			return cameraNode;
		}
		
		/// <summary>
		///		Tags geometry in the leaf specified for later rendering.
		/// </summary>
		protected void ProcessVisibleLeaf(BspNode leaf, Camera camera)
		{
			// Parse the leaf node's faces, add face groups to material map
			int numGroups = leaf.NumFaceGroups;
			int idx = leaf.FaceGroupStart;

			while(numGroups-- > 0)
			{
				int realIndex = level.LeafFaceGroups[idx++];
				
				// Check not already included
				if(faceGroupSet.Contains(realIndex))
					continue;
				
				StaticFaceGroup faceGroup = level.FaceGroups[realIndex];
				
				// Get Material reference by handle
				Material mat = GetMaterial(faceGroup.materialHandle);
			
				// Check normal (manual culling)
				ManualCullingMode cullMode = mat.GetTechnique(0).GetPass(0).ManualCullMode;

				if(cullMode != ManualCullingMode.None)
				{
					float dist = faceGroup.plane.GetDistance(camera.DerivedPosition);
					
					if(((dist < 0) && (cullMode == ManualCullingMode.Back)) ||
						((dist > 0) && (cullMode == ManualCullingMode.Front)))
						continue;
				}

				faceGroupSet.Add(realIndex);

				// Try to insert, will find existing if already there
				matFaceGroupMap.Insert(mat, faceGroup);
			}

			// Add movables to render queue, provided it hasn't been seen already.			
			for(int i = 0; i < leaf.Objects.Count; i++)
			{
				if(!objectsForRendering.ContainsKey(leaf.Objects[i]))
				{
					SceneObject obj = leaf.Objects[i];

					if(obj.IsVisible && camera.IsVisible)
					{
						obj.NotifyCurrentCamera(camera);
						obj.UpdateRenderQueue(this.renderQueue);

						objectsForRendering.Add(obj);
					}
				}
			}
		}

		/// <summary>
		///		Caches a face group for imminent rendering.
		/// </summary>
		protected int CacheGeometry(IntPtr indexes, StaticFaceGroup faceGroup)
		{
			// Skip sky always
			if(faceGroup.isSky)
				return 0;

			int idxStart = 0;
			int numIdx = 0;
			int vertexStart = 0;

			if(faceGroup.type == FaceGroup.FaceList)
			{
				idxStart = faceGroup.elementStart;
				numIdx = faceGroup.numElements;
				vertexStart = faceGroup.vertexStart;
			}
			else if(faceGroup.type == FaceGroup.Patch)
			{
				idxStart = faceGroup.patchSurf.IndexOffset;
				numIdx = faceGroup.patchSurf.CurrentIndexCount;
				vertexStart = faceGroup.patchSurf.VertexOffset;
			}

			unsafe
			{
				uint *src = (uint*) level.Indexes.Lock(idxStart * 4, numIdx * 4, BufferLocking.ReadOnly).ToPointer();
				uint *pIndexes = (uint*) indexes.ToPointer();

				// Offset the indexes here
				// we have to do this now rather than up-front because the 
				// indexes are sometimes reused to address different vertex chunks
				for(int i = 0; i < numIdx; i++)
					*pIndexes++ = (uint) (*src++ + vertexStart);

				level.Indexes.Unlock();
			}
			
			// return number of elements
			return numIdx;
		}

		/// <summary>
		///		Adds a bounding box to draw if turned on.
		/// </summary>
		protected void AddBoundingBox(AxisAlignedBox aab, bool visible)
		{
		}

		/// <summary>
		///		Renders the static level geometry tagged in <see cref="Plugin_BSPSceneManager.BspSceneManager.WalkTree"/>.
		/// </summary>
		protected void RenderStaticGeometry()
		{
			// no world transform required
			targetRenderSystem.WorldMatrix = Matrix4.Identity;

			// Set view / proj
			targetRenderSystem.ViewMatrix = camInProgress.ViewMatrix;
			targetRenderSystem.ProjectionMatrix = camInProgress.ProjectionMatrix;

			// For each material in turn, cache rendering data & render
			IEnumerator mapEnu = matFaceGroupMap.GetEnumerator();

			while(mapEnu.MoveNext())
			{
				// Get Material
				Material thisMaterial = (Material) ((Pair) mapEnu.Current).first;
				StaticFaceGroup faceGrp = (StaticFaceGroup)((Pair) mapEnu.Current).second;

				// Empty existing cache
				renderOp.indexData.indexCount = 0;
            
				// lock index buffer ready to receive data
				unsafe
				{
					uint *pIdx = (uint *) renderOp.indexData.indexBuffer.Lock(BufferLocking.Discard).ToPointer();

					//for(int i = 0; i < faceGrp.Length; i++)
					//{
						// Cache each
						int numElems = CacheGeometry((IntPtr) pIdx, faceGrp);
						renderOp.indexData.indexCount += numElems;
						pIdx += numElems;
					//}

					// Unlock the buffer
					renderOp.indexData.indexBuffer.Unlock();
				}
            
				// Skip if no faces to process (we're not doing flare types yet)
				if(renderOp.indexData.indexCount == 0)
					continue;

				for(int i = 0; i < thisMaterial.GetTechnique(0).NumPasses; i++)
				{
					SetPass(thisMaterial.GetTechnique(0).GetPass(i));
					targetRenderSystem.Render(renderOp);
				}
			}

			if(showNodeAABs)
				targetRenderSystem.Render(aaBGeometry);
		}

		/// <summary>
		///		Overriden from SceneManager.
		/// </summary>
		protected override void RenderVisibleObjects()
		{
			// Render static level geometry first
			RenderStaticGeometry();

			base.RenderVisibleObjects();
		}
		#endregion
	}

	/// <summary>
	///		BSP specialisation of IntersectionSceneQuery.
	/// </summary>
	// TODO: Scene queries.
	/*public class BspIntersectionSceneQuery : IntersectionSceneQuery
	{
		#region Constructor
		public BspIntersectionSceneQuery(SceneManager creator)
		{
			supportedWorldFragments.Add(WorldFragmentType.PlaneBoundedRegion);
		}
		#endregion	

		#region Public methods
		public void Execute(IntersectionQuerySceneListener listener)
		{
			/*
			Go through each leaf node in BspLevel and check movables against each other and world
			Issue: some movable-movable intersections could be reported twice if 2 movables
			overlap 2 leaves?
			*/
			/*BspLevel lvl = ((BspSceneManager) parentSceneMgr).Level;
			BspNode leaf = lvl.Nodes[lvl.LeafStart];
			int numLeaves = lvl.NumLeaves;
        
			while (numLeaves--)
			{
				SceneObjectCollection objects = leaf.Objects;

				for(int i = 0; i < objects.Count; i++)
				{
					if((objects[i].QueryFlags & queryMask) != 0)
						continue;

					if(i < (objects.Count - 1))
					{
						// Check object against others in this node

						b = a;
						for (++b; b != theEnd; ++b)
						{
							const MovableObject* bObj = *b;
							// Apply mask to b (both must pass)
							if ( bObj->getQueryFlags() & mQueryMask)
							{
								const AxisAlignedBox& box1 = aObj->getWorldBoundingBox();
								const AxisAlignedBox& box2 = bObj->getWorldBoundingBox();

								if (box1.intersects(box2))
								{
									listener->queryResult(const_cast<MovableObject*>(aObj), 
										const_cast<MovableObject*>(bObj)); // hacky
								}
							}
						}
					}
					// Check object against brushes
					const BspNode::NodeBrushList& brushes = leaf->getSolidBrushes();
				BspNode::NodeBrushList::const_iterator bi, biend;
					biend = brushes.end();
					Real radius = aObj->getBoundingRadius();
					const Vector3& pos = aObj->getParentNode()->_getDerivedPosition();

					for (bi = brushes.begin(); bi != biend; ++bi)
					{
					std::list<Plane>::const_iterator planeit, planeitend;
						planeitend = (*bi)->planes.end();
						bool brushIntersect = true; // Assume intersecting for now

						for (planeit = (*bi)->planes.begin(); planeit != planeitend; ++planeit)
						{
							Real dist = planeit->getDistance(pos);
							if (dist > radius)
							{
								// Definitely excluded
								brushIntersect = false;
								break;
							}
						}
						if (brushIntersect)
						{
							// report this brush as it's WorldFragment
							assert((*bi)->fragment.fragmentType == SceneQuery::WFT_PLANE_BOUNDED_REGION);
							listener->queryResult(const_cast<MovableObject*>(aObj), // hacky
								const_cast<WorldFragment*>(&((*bi)->fragment))); 
						}

					}


				}

				++leaf;
			}
		}
		#endregion
	}*/
}