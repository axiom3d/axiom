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
using System.Runtime.InteropServices;

using Axiom.Core;
using Axiom.MathLib;
using Axiom.Graphics;
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
		protected bool[] faceGroupUsed;
		protected RenderOperation renderOp = new RenderOperation();
		protected RenderOperation patchOp = new RenderOperation();
		protected bool showNodeAABs;
		protected RenderOperation aaBGeometry = new RenderOperation();

		protected Bsp.Collections.Map matFaceGroupMap = new Bsp.Collections.Map();
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
		public override void FindVisibleObjects(Camera camera, bool onlyShadowCasters)
		{
			// Clear unique list of movables for this frame
			objectsForRendering.Clear();

			// Walk the tree, tag static geometry, return camera's node (for info only)
			// Movables are now added to the render queue in processVisibleLeaf
			BspNode cameraNode = WalkTree(camera, false);
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
		}*/

		/// <summary>
		///		Creates a SphereSceneQuery for this scene manager. 
		/// </summary>
		/// <remarks>
		/// 	This method creates a new instance of a query object for this scene manager, 
		///		for a spherical region. See SceneQuery and SphereSceneQuery 
		///		for full details.
		/// </remarks>
		/// <param name="sphere">Details of the sphere which describes the region for this query.</param>
		/// <param name="mask">The query mask to apply to this query; can be used to filter out	certain objects; see SceneQuery for details.</param>
		public override SphereRegionSceneQuery CreateSphereRegionQuery(Sphere sphere, ulong mask)
		{
			BspSphereRegionSceneQuery q = new BspSphereRegionSceneQuery(this);
			q.Sphere = sphere;
			q.QueryMask = mask;

			return q;
		}

		/// <summary>
		///		Creates a RaySceneQuery for this scene manager. 
		/// </summary>
		/// <remarks>
		///		This method creates a new instance of a query object for this scene manager, 
		///		looking for objects which fall along a ray. See SceneQuery and RaySceneQuery 
		///		for full details.
		/// </remarks>
		/// <param name="ray">Details of the ray which describes the region for this query.</param>
		/// <param name="mask">The query mask to apply to this query; can be used to filter out certain objects; see SceneQuery for details.</param>
		public override RaySceneQuery CreateRayQuery(Ray ray, ulong mask)
		{
			BspRaySceneQuery q = new BspRaySceneQuery(this);
			q.Ray = ray;
			q.QueryMask = mask;

			return q;
		}

		/// <summary>
		///		Creates an IntersectionSceneQuery for this scene manager. 
		/// </summary>
		/// <remarks>
		///		This method creates a new instance of a query object for locating
		///		intersecting objects. See SceneQuery and IntersectionSceneQuery
		///		for full details.
		/// </remarks>
		/// <param name="mask">The query mask to apply to this query; can be used to filter out certain objects; see SceneQuery for details.</param>
		public override IntersectionSceneQuery CreateIntersectionQuery(ulong mask)
		{
			BspIntersectionSceneQuery q = new BspIntersectionSceneQuery(this);
			q.QueryMask = mask;

			return q;
		}
		#endregion

		#region Protected methods
		/// <summary>
		///		Walks the BSP tree looking for the node which the camera is in, and tags any geometry 
		///		which is in a visible leaf for later processing.
		/// </summary>
		protected BspNode WalkTree(Camera camera, bool onlyShadowCasters)
		{
			// Locate the leaf node where the camera is located
			BspNode cameraNode = level.FindLeaf(camera.DerivedPosition);

			matFaceGroupMap.Clear();
			faceGroupUsed = new bool[level.FaceGroups.Length];

			// Scan through all the other leaf nodes looking for visibles
			int i = level.NumNodes - level.LeafStart;
			int p = level.LeafStart;
			BspNode node;
			
			while(i-- > 0)
			{
				node = level.Nodes[p];
                
				if(level.IsLeafVisible(cameraNode, node))
				{
					// Visible according to PVS, check bounding box against frustum
					FrustumPlane plane;

					if(camera.IsObjectVisible(node.BoundingBox, out plane))
					{
						ProcessVisibleLeaf(node, camera, onlyShadowCasters);

						if(showNodeAABs)
							AddBoundingBox(node.BoundingBox, true);
					}
				}

				p++;
			}

			return cameraNode;
		}
		
		/// <summary>
		///		Tags geometry in the leaf specified for later rendering.
		/// </summary>
		protected void ProcessVisibleLeaf(BspNode leaf, Camera camera, bool onlyShadowCasters)
		{
			// Skip world geometry if we're only supposed to process shadow casters
			// World is pre-lit
			if (!onlyShadowCasters)
			{
				// Parse the leaf node's faces, add face groups to material map
				int numGroups = leaf.NumFaceGroups;
				int idx = leaf.FaceGroupStart;

				while(numGroups-- > 0)
				{
					int realIndex = level.LeafFaceGroups[idx++];
				
					// Check not already included
					if (faceGroupUsed[realIndex] == true) continue;
				
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

					faceGroupUsed[realIndex] = true;
					// Try to insert, will find existing if already there
					matFaceGroupMap.Insert(mat, faceGroup);
				}
			}

			// TODO BspNode.IntersectingObjectSet
			// Add movables to render queue, provided it hasn't been seen already.			
			for(int i = 0; i < leaf.Objects.Count; i++)
			{
				if(!objectsForRendering.ContainsKey(((SceneObject)leaf.Objects[i]).Name))
				{
					SceneObject obj = leaf.Objects[i];

					if(obj.IsVisible && 
						(!onlyShadowCasters || obj.CastShadows) &&
						camera.IsObjectVisible(obj.GetWorldBoundingBox()))
					{
						obj.NotifyCurrentCamera(camera);
						obj.UpdateRenderQueue(this.renderQueue);
						// Check if the bounding box should be shown.
						SceneNode node = (SceneNode)obj.ParentNode;
						if (node.ShowBoundingBox || this.showBoundingBoxes)
						{
							node.AddBoundingBoxToQueue(this.renderQueue);
						}
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
			else
			{
				// Unsupported face type
				return 0;
			}

			unsafe
			{
				uint *src = (uint*) level.Indexes.Lock(
					idxStart * sizeof(uint), 
					numIdx * sizeof(uint), 
					BufferLocking.ReadOnly);
				uint *pIndexes = (uint*) indexes;

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
			IEnumerator mapEnu = matFaceGroupMap.buckets.Keys.GetEnumerator();
            
			while(mapEnu.MoveNext())
			{
				// Get Material
				Material thisMaterial = (Material) mapEnu.Current;
				StaticFaceGroup[] faceGrp = (StaticFaceGroup[]) ((ArrayList) matFaceGroupMap.buckets[thisMaterial]).ToArray(typeof(StaticFaceGroup));

				// Empty existing cache
				renderOp.indexData.indexCount = 0;
            
				// lock index buffer ready to receive data
				unsafe
				{
					uint *pIdx = (uint *) renderOp.indexData.indexBuffer.Lock(BufferLocking.Discard);

					for(int i = 0; i < faceGrp.Length; i++)
					{
						// Cache each
						int numElems = CacheGeometry((IntPtr) pIdx, faceGrp[i]);
						renderOp.indexData.indexCount += numElems;
						pIdx += numElems;
					}

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

			//if(showNodeAABs)
			//	targetRenderSystem.Render(aaBGeometry);
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
	public class BspIntersectionSceneQuery : DefaultIntersectionSceneQuery
	{
		#region Constructor
		public BspIntersectionSceneQuery(SceneManager creator) : base(creator)
		{
			this.AddWorldFragmentType(WorldFragmentType.PlaneBoundedRegion);
		}
		#endregion	

		#region Public methods
		
		public override void Execute(IIntersectionSceneQueryListener listener)
		{
			//Go through each leaf node in BspLevel and check movables against each other and world
			//Issue: some movable-movable intersections could be reported twice if 2 movables
			//overlap 2 leaves?
			BspLevel lvl = ((BspSceneManager) this.creator).Level;
			int leafPoint = lvl.LeafStart;
			int numLeaves = lvl.NumLeaves;

			Bsp.Collections.Map objIntersections = new Bsp.Collections.Map();
			PlaneBoundedVolume boundedVolume = new PlaneBoundedVolume(PlaneSide.Positive);
        
			while ((numLeaves--) != 0)
			{
				BspNode leaf = lvl.Nodes[leafPoint];
				SceneObjectCollection objects = leaf.Objects;
				int numObjects = objects.Count;

				for(int a = 0; a < numObjects; a++)
				{
					SceneObject aObj = objects[a];
					// Skip this object if collision not enabled
					if((aObj.QueryFlags & queryMask) == 0)
						continue;

					if(a < (numObjects - 1))
					{
						// Check object against others in this node
						int b = a;
						for (++b; b < numObjects; ++b)
						{
							SceneObject bObj = objects[b];
							// Apply mask to b (both must pass)
							if ((bObj.QueryFlags & queryMask) != 0)
							{
								AxisAlignedBox box1 = aObj.GetWorldBoundingBox();
								AxisAlignedBox box2 = bObj.GetWorldBoundingBox();

								if (box1.Intersects(box2))
								{
									//Check if this pair is already reported
									bool alreadyReported = false;
									IList interObjList = objIntersections.FindBucket(aObj);
									if (interObjList != null)
										if (interObjList.Contains(bObj))
											alreadyReported = true;

									if (!alreadyReported)
									{
										objIntersections.Insert(aObj,bObj);
										listener.OnQueryResult(aObj,bObj);
									}
								}
							}
						}
					}
					// Check object against brushes

					/*----This is for bounding sphere-----
					float radius = aObj.BoundingRadius;
					//-------------------------------------------*/

					for (int brushPoint=0; brushPoint < leaf.SolidBrushes.Length; brushPoint++)
					{
						BspBrush brush = leaf.SolidBrushes[brushPoint];

						if (brush == null) continue;

						bool brushIntersect = true; // Assume intersecting for now

						/*----This is for bounding sphere-----
						IEnumerator planes = brush.Planes.GetEnumerator();

						while (planes.MoveNext())
						{
							float dist = ((Plane)planes.Current).GetDistance(pos);
							if (dist > radius)
							{
								// Definitely excluded
								brushIntersect = false;
								break;
							}
						}
						//-------------------------------------------*/

						boundedVolume.planes = brush.Planes;
						//Test object as bounding box
						if (!boundedVolume.Intersects(aObj.GetWorldBoundingBox()))
							brushIntersect = false;

						if (brushIntersect)
						{
							//Check if this pair is already reported
							bool alreadyReported = false;
							IList interObjList = objIntersections.FindBucket(aObj);
							if (interObjList != null)
								if (interObjList.Contains(brush))
									alreadyReported = true;

							if (!alreadyReported)
							{
								objIntersections.Insert(aObj,brush);
								// report this brush as it's WorldFragment
								brush.Fragment.FragmentType = WorldFragmentType.PlaneBoundedRegion;
								listener.OnQueryResult(aObj,brush.Fragment);
							}
						}
					}
				}
				++leafPoint;
			}
		}
		#endregion
	}

	/// <summary>
	///		BSP specialisation of RaySceneQuery.
	/// </summary>
	public class BspRaySceneQuery : DefaultRaySceneQuery
	{
		#region Constructor
		public BspRaySceneQuery(SceneManager creator) : base(creator)
		{
			this.AddWorldFragmentType(WorldFragmentType.PlaneBoundedRegion);
		}
		#endregion	

		#region Protected Members

		protected IRaySceneQueryListener listener;
		protected bool StopRayTracing;

		#endregion

		#region Public methods
		
		public override void Execute(IRaySceneQueryListener listener)
		{
			this.listener = listener;
			this.StopRayTracing = false;
            ProcessNode(((BspSceneManager)creator).Level.RootNode, ray, float.PositiveInfinity, 0);
		}
		#endregion

		#region Protected methods

		protected virtual void ProcessNode(BspNode node, Ray tracingRay, float maxDistance, float traceDistance)
		{
			if (StopRayTracing) return;

			if (node.IsLeaf)
			{
				ProcessLeaf(node, tracingRay, maxDistance, traceDistance);
				return;
			}

			IntersectResult result = tracingRay.Intersects(node.SplittingPlane);
			if (result.Hit)
			{
				if (result.Distance < maxDistance)
				{
					if (node.GetSide(tracingRay.Origin) == PlaneSide.Negative)
					{
						ProcessNode(node.BackNode, tracingRay, result.Distance, traceDistance);
						Vector3 splitPoint = tracingRay.Origin + tracingRay.Direction * result.Distance;
						ProcessNode(node.FrontNode, new Ray(splitPoint, tracingRay.Direction), maxDistance - result.Distance, traceDistance + result.Distance);
					}
					else
					{
						ProcessNode(node.FrontNode, tracingRay, result.Distance, traceDistance);
						Vector3 splitPoint = tracingRay.Origin + tracingRay.Direction * result.Distance;
						ProcessNode(node.BackNode, new Ray(splitPoint, tracingRay.Direction), maxDistance - result.Distance, traceDistance + result.Distance);
					}
				}
				else
					ProcessNode(node.GetNextNode(tracingRay.Origin), tracingRay, maxDistance, traceDistance);
			}
			else
				ProcessNode(node.GetNextNode(tracingRay.Origin), tracingRay, maxDistance, traceDistance);
		}

		protected virtual void ProcessLeaf(BspNode leaf, Ray tracingRay, float maxDistance, float traceDistance)
		{
			SceneObjectCollection objects = leaf.Objects;
			int numObjects = objects.Count;

			//Check ray against objects
			for(int a = 0; a < numObjects; a++)
			{
				SceneObject obj = objects[a];
				// Skip this object if collision not enabled
				if((obj.QueryFlags & queryMask) == 0)
					continue;

				//Test object as bounding box
				IntersectResult result = tracingRay.Intersects(obj.GetWorldBoundingBox());
				// if the result came back positive and intersection point is inside
				// the node, fire the event handler
				if(result.Hit && result.Distance <= maxDistance) 
				{
					listener.OnQueryResult(obj, result.Distance + traceDistance);
				}
			}

			PlaneBoundedVolume boundedVolume = new PlaneBoundedVolume(PlaneSide.Positive);
			BspBrush intersectBrush = null;
			float intersectBrushDist = float.PositiveInfinity;

			// Check ray against brushes
			for (int brushPoint=0; brushPoint < leaf.SolidBrushes.Length; brushPoint++)
			{
				BspBrush brush = leaf.SolidBrushes[brushPoint];

				if (brush == null) continue;

				boundedVolume.planes = brush.Planes;

				IntersectResult result = tracingRay.Intersects(boundedVolume);
				// if the result came back positive and intersection point is inside
				// the node, check if this brush is closer
				if(result.Hit && result.Distance <= maxDistance) 
				{
					if (result.Distance < intersectBrushDist)
					{
						intersectBrushDist = result.Distance;
						intersectBrush = brush;
					}
				}
			}

			if (intersectBrush != null)
			{
				listener.OnQueryResult(intersectBrush.Fragment, intersectBrushDist + traceDistance);
				StopRayTracing = true;
			}
		}
		#endregion
	}

	/// <summary>
	///		BSP specialisation of SphereRegionSceneQuery.
	/// </summary>
	public class BspSphereRegionSceneQuery : DefaultSphereRegionSceneQuery
	{
		#region Constructor
		public BspSphereRegionSceneQuery(SceneManager creator) : base(creator)
		{
			this.AddWorldFragmentType(WorldFragmentType.PlaneBoundedRegion);
		}
		#endregion	

		#region Protected Members

		protected ISceneQueryListener listener;
		protected ArrayList foundIntersections = new ArrayList();

		#endregion

		#region Public methods
		
		public override void Execute(ISceneQueryListener listener)
		{
			this.listener = listener;
			this.foundIntersections.Clear();
			ProcessNode(((BspSceneManager)creator).Level.RootNode);
		}
		#endregion

		#region Protected methods

		protected virtual void ProcessNode(BspNode node)
		{
			if (node.IsLeaf)
			{
				ProcessLeaf(node);
				return;
			}

			float distance = node.GetDistance(sphere.Center);

			if(MathUtil.Abs(distance) < sphere.Radius)
			{
				// Sphere crosses the plane, do both.
				ProcessNode(node.BackNode);
				ProcessNode(node.FrontNode);
			}
			else if(distance < 0)
			{
				// Do back.
				ProcessNode(node.BackNode);
			}
			else
			{
				// Do front.
				ProcessNode(node.FrontNode);
			}
		}

		protected virtual void ProcessLeaf(BspNode leaf)
		{
			SceneObjectCollection objects = leaf.Objects;
			int numObjects = objects.Count;

			//Check sphere against objects
			for(int a = 0; a < numObjects; a++)
			{
				SceneObject obj = objects[a];
				// Skip this object if collision not enabled
				if((obj.QueryFlags & queryMask) == 0)
					continue;

				//Test object as bounding box
				if(sphere.Intersects(obj.GetWorldBoundingBox())) 
				{
					if (!foundIntersections.Contains(obj))
					{
						listener.OnQueryResult(obj);
						foundIntersections.Add(obj);
					}
				}
			}

			PlaneBoundedVolume boundedVolume = new PlaneBoundedVolume(PlaneSide.Positive);

			// Check ray against brushes
			for (int brushPoint=0; brushPoint < leaf.SolidBrushes.Length; brushPoint++)
			{
				BspBrush brush = leaf.SolidBrushes[brushPoint];
				if (brush == null) continue;

				boundedVolume.planes = brush.Planes;
				if(boundedVolume.Intersects(sphere)) 
				{
					listener.OnQueryResult(brush.Fragment);
				}
			}
		}
		#endregion
	}
}