using System;
using System.Collections;
using System.Text;
using System.IO;

using Axiom.Graphics;
using Axiom.MathLib;
using Axiom.Collections;

namespace Axiom.Core
{
	
	#region Strongly Types Collections
	//TODO: Modify for Generics
	public class QueuedGeometryList : ArrayList {}
	public class GeometryBucketList : ArrayList {}
	public class CurrentGeometryMap : Hashtable {}
	public class QueuedSubMeshList : ArrayList {}
	public class OptimisedSubMeshGeometryList : ArrayList {}
	public class SubMeshLodGeometryLinkList : ArrayList {}
	public class SubMeshGeometryLookup : Hashtable {}
	public class LODBucketList : ArrayList {}
	public class MaterialBucketMap : Hashtable {}
	public class RegionMap : Hashtable {}

	#endregion

	#region Structs
	
	public struct OptimisedSubMeshGeometry
	{
		public VertexData vertexData;
		public IndexData indexData;
	}

	public struct SubMeshLodGeometryLink
	{
		public VertexData vertexData;
		public IndexData indexData;
	}

	public struct QueuedSubMesh
	{
		public SubMesh submesh;
		public SubMeshLodGeometryLinkList geometryLodList;
		public string materialName;
		public Vector3 position;
		public Quaternion orientation;
		public Vector3 scale;
		public AxisAlignedBox worldBounds;
	}

	public struct QueuedGeometry
	{
		public SubMeshLodGeometryLink geometry;
		public Vector3 position;
		public Quaternion orientation;
		public Vector3 scale;
	}
	#endregion

	/// <summary>
	/// Pre-transforms and batches up meshes for efficient use as static geometry in a scene.
	/// </summary>
	/// <remarks>
	/// Modern graphics cards (GPUs) prefer to receive geometry in large
	/// batches. It is orders of magnitude faster to render 10 batches
	/// of 10,000 triangles than it is to render 10,000 batches of 10 
	/// triangles, even though both result in the same number of on-screen
	/// triangles.
	/// <br>
	/// Therefore it is important when you are rendering a lot of geometry to 
	/// batch things up into as few rendering calls as possible. This
	/// class allows you to build a batched object from a series of entities 
	/// in order to benefit from this behaviour.
	/// Batching has implications of it's own though:
	/// <ul>
	/// <li> Batched geometry cannot be subdivided; that means that the whole
	/// 	group will be displayed, or none of it will. This obivously has
	/// 	culling issues.
	/// <li> A single world transform must apply to the entire batch. Therefore
	/// 	once you have batched things, you can't move them around relative to
	/// 	each other. That's why this class is most useful when dealing with 
	/// 	static geometry (hence the name). In addition, geometry is 
	/// 	effectively duplicated, so if you add 3 entities based on the same 
	/// 	mesh in different positions, they will use 3 times the geometry 
	/// 	space than the movable version (which re-uses the same geometry). 
	/// 	So you trade memory	and flexibility of movement for pure speed when
	/// 	using this class.
	/// <li> A single material must apply for each batch. In fact this class 
	/// 	allows you to use multiple materials, but you should be aware that 
	/// 	internally this means that there is one batch per material. 
	/// 	Therefore you won't gain as much benefit from the batching if you 
	/// 	use many different materials; try to keep the number down.
	/// </ul>
	/// <br>
	/// In order to retain some sort of culling, this class will batch up 
	/// meshes in localised regions. The size and shape of these blocks is
	/// controlled by the SceneManager which contructs this object, since it
	/// makes sense to batch things up in the most appropriate way given the 
	/// existing partitioning of the scene. 
	/// <br>
	/// The LOD settings of both the Mesh and the Materials used in 
	/// constructing this static geometry will be respected. This means that 
	/// if you use meshes/materials which have LOD, batches in the distance 
	/// will have a lower polygon count or material detail to those in the 
	/// foreground. Since each mesh might have different LOD distances, during 
	/// build the furthest distance at each LOD level from all meshes  
	/// in that region is used. This means all the LOD levels change at the 
	/// same time, but at the furthest distance of any of them (so quality is 
	/// not degraded). Be aware that using Mesh LOD in this class will 
	/// further increase the memory required. Only generated LOD
	/// is supported for meshes.
	/// <br>
	/// There are 2 ways you can add geometry to this class; you can add
	/// Entity objects directly with predetermined positions, scales and 
	/// orientations, or you can add an entire SceneNode and it's subtree, 
	/// including all the objects attached to it. Once you've added everthing
	/// you need to, you have to call build() the fix the geometry in place. 
	/// <br>
	/// This class is not a replacement for world geometry (see 
	/// SceneManager.WorldGeometry). The single most efficient way to 
	/// render large amounts of static geometry is to use a SceneManager which 
	/// is specialised for dealing with that particular world structure. 
	/// However, this class does provide you with a good 'halfway house'
	/// between generalised movable geometry (Entity) which works with all 
	/// SceneManagers but isn't efficient when using very large numbers, and 
	/// highly specialised world geometry which is extremely fast but not 
	/// generic and typically requires custom world editors.
	/// <br>
	/// You should not construct instances of this class directly; instead, call
	/// SceneManager.CreateStaticGeometry, which gives the SceneManager the 
	/// option of providing you with a specialised version of this class if it
	/// wishes, and also handles the memory management for you like other 
	/// classes.
	/// </remarks>
	/// Port started by jwace81
	/// OGRE Source File: http://cvs.sourceforge.net/viewcvs.py/ogre/ogrenew/OgreMain/src/OgreStaticGeometry.cpp?rev=1.22&view=auto
	/// OGRE Header File: http://cvs.sourceforge.net/viewcvs.py/ogre/ogrenew/OgreMain/include/OgreStaticGeometry.h?rev=1.14&view=auto
	public class StaticGeometry
	{
		#region Fields and Properties
		
		protected SceneManager owner;
		protected string name;
		protected bool built;
		protected float upperDistance;
		protected float squaredUpperDistance;
		protected bool castShadows;
		protected Vector3 regionDimensions;
		protected Vector3 halfRegionDimensions;
		protected Vector3 origin;
		protected bool visible;
		protected RenderQueueGroupID renderQueueID;
		protected bool renderQueueIDSet;
		protected QueuedSubMeshList queuedSubMeshes;
		protected OptimisedSubMeshGeometryList optimisedSubMeshGeometryList;
		protected SubMeshGeometryLookup subMeshGeometryLookup;
		protected RegionMap regionMap;
		public float SquaredRenderingDistance { get { throw new NotSupportedException(); } }

		#endregion

		#region Constructors
		public StaticGeometry()
		{
		}
		#endregion
	}
}
