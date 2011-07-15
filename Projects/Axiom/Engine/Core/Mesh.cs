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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

using Axiom.Animating;
using Axiom.Collections;
using Axiom.Configuration;
using Axiom.Math;
using Axiom.Math.Collections;
using Axiom.Serialization;
using Axiom.Graphics;

using ResourceHandle = System.UInt64;
using Axiom.Core.Collections;

#endregion Namespace Declarations

namespace Axiom.Core
{
	/// <summary>
	///    Resource holding data about a 3D mesh.
	/// </summary>
	/// <remarks>
	///    This class holds the data used to represent a discrete
	///    3-dimensional object. Mesh data usually contains more
	///    than just vertices and triangle information; it also
	///    includes references to materials (and the faces which use them),
	///    level-of-detail reduction information, convex hull definition,
	///    skeleton/bones information, keyframe animation etc.
	///    However, it is important to note the emphasis on the word
	///    'discrete' here. This class does not cover the large-scale
	///    sprawling geometry found in level / landscape data.
	///    <p/>
	///    Multiple world objects can (indeed should) be created from a
	///    single mesh object - see the Entity class for more info.
	///    The mesh object will have its own default
	///    material properties, but potentially each world instance may
	///    wish to customize the materials from the original. When the object
	///    is instantiated into a scene node, the mesh material properties
	///    will be taken by default but may be changed. These properties
	///    are actually held at the SubMesh level since a single mesh may
	///    have parts with different materials.
	///    <p/>
	///    As described above, because the mesh may have sections of differing
	///    material properties, a mesh is inherently a compound construct,
	///    consisting of one or more SubMesh objects.
	///    However, it strongly 'owns' its SubMeshes such that they
	///    are loaded / unloaded at the same time. This is contrary to
	///    the approach taken to hierarchically related (but loosely owned)
	///    scene nodes, where data is loaded / unloaded separately. Note
	///    also that mesh sub-sections (when used in an instantiated object)
	///    share the same scene node as the parent.
	/// </remarks>
	public class Mesh : Resource
	{
		#region Fields and Properties

		#region SharedVertexData Property

		/// <summary>
		///		Shared vertex data between multiple meshes.
		///	</summary>
		private VertexData _sharedVertexData;
		/// <summary>
		///		Gets/Sets the shared VertexData for this mesh.
		/// </summary>
		public VertexData SharedVertexData
		{
			get
			{
				return _sharedVertexData;
			}
			set
			{
				_sharedVertexData = value;
			}
		}

		#endregion SharedVertexData Property

		#region SubMesh Properties

		/// <summary>
		///		Collection of sub meshes for this mesh.
		///	</summary>
		private SubMeshList _subMeshList = new SubMeshList();

		/// <summary>
		///    Gets the number of submeshes belonging to this mesh.
		/// </summary>
		public int SubMeshCount
		{
			get
			{
				return _subMeshList.Count;
			}
		}

		#endregion SubMesh Properties

		#region BoundingBox Property

		/// <summary>
		///		Local bounding box of this mesh.
		/// </summary>
		private AxisAlignedBox _boundingBox = AxisAlignedBox.Null;
		/// <summary>
		///		Gets/Sets the bounding box for this mesh.
		/// </summary>
		/// <remarks>
		///		Setting this property is required when building manual meshes now, because Axiom can no longer
		///		update the bounds for you, because it cannot necessarily read vertex data back from
		///		the vertex buffers which this mesh uses (they very well might be write-only, and even
		///		if they are not, reading data from a hardware buffer is a bottleneck).
		/// </remarks>
		public AxisAlignedBox BoundingBox
		{
			get
			{
				// OPTIMIZE: Cloning to prevent direct modification
				return (AxisAlignedBox)_boundingBox.Clone();
			}
			set
			{
				_boundingBox = value;

				float sqLen1 = _boundingBox.Minimum.LengthSquared;
				float sqLen2 = _boundingBox.Maximum.LengthSquared;

				// update the bounding sphere radius as well
				_boundingSphereRadius = Utility.Sqrt( Utility.Max( sqLen1, sqLen2 ) );
			}
		}

		#endregion BoundingBox Property

		#region BoundingSphereRadius Property

		/// <summary>
		///		Radius of this mesh's bounding sphere.
		/// </summary>
		private float _boundingSphereRadius;
		/// <summary>
		///    Bounding spehere radius from this mesh in local coordinates.
		/// </summary>
		public float BoundingSphereRadius
		{
			get
			{
				return _boundingSphereRadius;
			}
			set
			{
				_boundingSphereRadius = value;
			}
		}

		#endregion BoundingSphereRadius Property

		#region Skeleton Property

		/// <summary>Reference to the skeleton bound to this mesh.</summary>
		private Skeleton _skeleton;
		/// <summary>
		///    Gets the skeleton currently bound to this mesh.
		/// </summary>
		public Skeleton Skeleton
		{
			get
			{
				return _skeleton;
			}
			protected set
			{
				_skeleton = value;
			}
		}

		#endregion Skeleton Property

		#region SkeletonName Property

		/// <summary>Name of the skeleton bound to this mesh.</summary>
		private string _skeletonName;
		/// <summary>
		///    Get/Sets the name of the skeleton which will be bound to this mesh.
		/// </summary>
		public string SkeletonName
		{
			get
			{
				return _skeletonName;
			}
			set
			{
				_skeletonName = value;

				if ( _skeletonName == null || _skeletonName.Length == 0 )
				{
					_skeleton = null;
				}
				else
				{
					try
					{
						// load the skeleton
						_skeleton = (Skeleton)SkeletonManager.Instance.Load( _skeletonName, Group );
					}
					catch ( Exception ex )
					{
						LogManager.Instance.Write( "Unable to load skeleton " + _skeletonName + " for Mesh " + Name + ". This Mesh will not be animated. You can ignore this message if you are using an offline tool." );
					}
				}
			}
		}

		#endregion SkeletonName Property

		#region HasSkeleton Property

		/// <summary>
		///    Determins whether or not this mesh has a skeleton associated with it.
		/// </summary>
		public bool HasSkeleton
		{
			get
			{
				return ( _skeletonName.Length != 0 );
			}
		}

		#endregion HasSkeleton Property

		#region BoneAssignmentList Property

		/// <summary>List of bone assignment for this mesh.</summary>
		private Dictionary<int, List<VertexBoneAssignment>> _boneAssignmentList = new Dictionary<int, List<VertexBoneAssignment>>();
		/// <summary>
		///		Gets bone assigment list
		/// </summary>
		public Dictionary<int, List<VertexBoneAssignment>> BoneAssignmentList
		{
			get
			{
				return _boneAssignmentList;
			}
		}

		#endregion BoneAssignmentList Property

		/// <summary>Flag indicating that bone assignments need to be recompiled.</summary>
		protected bool boneAssignmentsOutOfDate;

		/// <summary>Number of blend weights that are assigned to each vertex.</summary>
		protected short numBlendWeightsPerVertex;

		/// <summary>Option whether to use software or hardware blending, there are tradeoffs to both.</summary>
		protected internal bool useSoftwareBlending;

		#region VertexBufferUsage Property

		/// <summary>
		///		Usage type for the vertex buffer.
		/// </summary>
		private BufferUsage _vertexBufferUsage;
		/// <summary>
		///    Gets the usage setting for this meshes vertex buffers.
		/// </summary>
		public BufferUsage VertexBufferUsage
		{
			get
			{
				return _vertexBufferUsage;
			}
			protected set
			{
				_vertexBufferUsage = value;
			}
		}

		#endregion VertexBufferUsage Property

		#region IndexBufferUsage Property

		/// <summary>
		///		Usage type for the index buffer.
		/// </summary>
		private BufferUsage _indexBufferUsage;
		/// <summary>
		///    Gets the usage setting for this meshes index buffers.
		/// </summary>
		public BufferUsage IndexBufferUsage
		{
			get
			{
				return _indexBufferUsage;
			}
			protected set
			{
				_indexBufferUsage = value;
			}
		}

		#endregion IndexBufferUsage Property

		#region UseVertexShadowBuffer Property

		/// <summary>
		///		Use a shadow buffer for the vertex data?
		/// </summary>
		private bool _useVertexShadowBuffer;
		/// <summary>
		///    Gets whether or not this meshes vertex buffers are shadowed.
		/// </summary>
		public bool UseVertexShadowBuffer
		{
			get
			{
				return _useVertexShadowBuffer;
			}
			protected set
			{
				_useVertexShadowBuffer = value;
			}
		}

		#endregion UseVertexShadowBuffer Property

		#region UseIndexShadowBuffer Property

		/// <summary>
		///		Use a shadow buffer for the index data?
		/// </summary>
		private bool _useIndexShadowBuffer;
		/// <summary>
		///    Gets whether or not this meshes index buffers are shadowed.
		/// </summary>
		public bool UseIndexShadowBuffer
		{
			get
			{
				return _useIndexShadowBuffer;
			}
			protected set
			{
				_useIndexShadowBuffer = value;
			}
		}

		#endregion UseIndexShadowBuffer Property

		#region IsPreparedForShadowVolumes Property

		/// <summary>
		///		Flag indicating whether precalculation steps to support shadows have been taken.
		/// </summary>
		private bool _isPreparedForShadowVolumes;
		/// <summary>
		///		Gets whether this mesh has already had its geometry prepared for use in
		///		rendering shadow volumes.
		/// </summary>
		public bool IsPreparedForShadowVolumes
		{
			get
			{
				return _isPreparedForShadowVolumes;
			}
		}

		#endregion IsPreparedForShadowVolumes Property

		#region AutoBuildEdgeLists Property

		/// <summary>
		///		Should edge lists be automatically built for this mesh?
		/// </summary>
		private bool _autoBuildEdgeLists;
		/// <summary>
		///		Gets/Sets whether or not this Mesh should automatically build edge lists
		///		when asked for them, or whether it should never build them if
		///		they are not already provided.
		/// </summary>
		public bool AutoBuildEdgeLists
		{
			get
			{
				return _autoBuildEdgeLists;
			}
			set
			{
				_autoBuildEdgeLists = value;
			}
		}

		#endregion AutoBuildEdgeLists Property

		#region IsEdgeListBuilt Property

		/// <summary>
		///     Have the edge lists been built for this mesh yet?
		/// </summary>
		private bool _edgeListsBuilt;
		/// <summary>
		///     Returns whether this mesh has an attached edge list.
		/// </summary>
		public bool IsEdgeListBuilt
		{
			get
			{
				return _edgeListsBuilt;
			}
			protected internal set
			{
				_edgeListsBuilt = value;
			}
		}

		#endregion IsEdgeListBuilt Property

		#region AttachmentPoints Property

		/// <summary>Internal list of named transforms attached to this mesh.</summary>
		private List<AttachmentPoint> _attachmentPoints = new List<AttachmentPoint>();
		/// <summary>
		/// Llist of named transforms attached to this mesh.
		/// </summary>
		/// <value>The attachment points.</value>
		public List<AttachmentPoint> AttachmentPoints
		{
			get
			{
				return _attachmentPoints;
			}
		}

		#endregion AttachmentPoints Property

		/// <summary>
		///     Storage of morph animations, lookup by name
		/// </summary>
		private Dictionary<string, Animation> _animationsList = new Dictionary<string, Animation>();
		/// <summary>
		///   The number of vertex animations in the mesh
		/// </summary>
		public int AnimationCount
		{
			get
			{
				return _animationsList.Count;
			}
		}

		/// <summary>Returns whether or not this mesh has some kind of vertex animation.</summary>
		public bool HasVertexAnimation
		{
			get
			{
				return _animationsList.Count > 0;
			}
		}

		#region SharedVertexDataAnimationType Property

		/// <summary>
		///     The vertex animation type associated with the shared vertex data
		/// </summary>
		private VertexAnimationType _sharedVertexDataAnimationType;
		/// <summary>
		///		Gets bone assigment list
		/// </summary>
		public VertexAnimationType SharedVertexDataAnimationType
		{
			get
			{
				if ( _animationTypesDirty )
					DetermineAnimationTypes();
				return _sharedVertexDataAnimationType;
			}
		}

		#endregion SharedVertexDataAnimationType Property

		#region AnimationTypesDirty Property

		/// <summary>
		///     Do we need to scan animations for animation types?
		/// </summary>
		private bool _animationTypesDirty;
		/// <summary>Are the derived animation types out of date?</summary>
		public bool AnimationTypesDirty
		{
			get
			{
				return _animationTypesDirty;
			}
		}

		#endregion AnimationTypesDirty Property

		#region PoseList Property

		/// <summary>
		///     List of available poses for shared and dedicated geometryPoseList
		/// </summary>
		private List<Pose> _poseList = new List<Pose>();
		/// <summary>
		///		Gets bone assigment list
		/// </summary>
		public List<Pose> PoseList
		{
			get
			{
				return _poseList;
			}
		}

		#endregion PoseList Property

		#region TriangleIntersector Property

		/// <summary>
		///     A list of triangles, plus machinery to determine the closest intersection point
		/// </summary>
		private TriangleIntersector _triangleIntersector = null;
		/// <summary>A list of triangles, plus machinery to determine the closest intersection point</summary>
		public TriangleIntersector TriangleIntersector
		{
			get
			{
				return _triangleIntersector;
			}
			set
			{
				_triangleIntersector = value;
			}
		}

		#endregion TriangleIntersector Property

		#endregion Fields and Properties

		#region Construction and Destruction

		public Mesh( ResourceManager parent, string name, ResourceHandle handle, string group, bool isManual, IManualResourceLoader loader )
			: base( parent, name, handle, group, isManual, loader )
		{

			// This will be set to false by serializers 1.3 and above
			_autoBuildEdgeLists = true;

			// default to static write only for speed
			_vertexBufferUsage = BufferUsage.StaticWriteOnly;
			_indexBufferUsage = BufferUsage.StaticWriteOnly;

			// default to having shadow buffers
			_useVertexShadowBuffer = true;
			_useIndexShadowBuffer = true;

			// Initialise to default strategy
			_lodStrategy = LodStrategyManager.Instance.DefaultStrategy;

			// Init first (manual) lod
			MeshLodUsage lod = new MeshLodUsage();
			lod.UserValue = float.NaN; // User value not used for base lod level
			lod.Value = _lodStrategy.BaseValue;
			lod.EdgeData = null;
			lod.ManualMesh = null;
			meshLodUsageList.Add( lod );


			// always use software blending for now
			useSoftwareBlending = true;

			this.SkeletonName = "";
		}

		#endregion Construction and Destruction

		#region Properties


		/// <summary>
		///		Gets the edge list for this mesh, building it if required.
		/// </summary>
		/// <returns>The edge list for mesh LOD 0.</returns>
		public EdgeData GetEdgeList()
		{
			return GetEdgeList( 0 );
		}

		/// <summary>
		///		Gets the edge list for this mesh, building it if required.
		/// </summary>
		/// <remarks>
		///		You must ensure that the Mesh as been prepared for shadow volume
		///		rendering if you intend to use this information for that purpose.
		/// </remarks>
		public EdgeData GetEdgeList( int lodIndex )
		{
			if ( !_edgeListsBuilt )
			{
				BuildEdgeList();
			}

			return GetLodLevel( lodIndex ).EdgeData;
		}

		#endregion Properties

		#region Methods

		/// <summary>
		///    Assigns a vertex to a bone with a given weight, for skeletal animation.
		/// </summary>
		/// <remarks>
		///    This method is only valid after setting SkeletonName.
		///    You should not need to modify bone assignments during rendering (only the positions of bones)
		///    and the engine reserves the right to do some internal data reformatting of this information,
		///    depending on render system requirements.
		/// </remarks>
		/// <param name="boneAssignment">Bone assignment to add.</param>
		public void AddBoneAssignment( VertexBoneAssignment boneAssignment )
		{
			if ( !_boneAssignmentList.ContainsKey( boneAssignment.vertexIndex ) )
				_boneAssignmentList[ boneAssignment.vertexIndex ] = new List<VertexBoneAssignment>();
			_boneAssignmentList[ boneAssignment.vertexIndex ].Add( boneAssignment );
			boneAssignmentsOutOfDate = true;
		}

		/// <summary>
		///    Adds the vertex and index sets necessary for a builder instance
		///    to iterate over the triangles in a mesh
		/// </summary>
		public void AddVertexAndIndexSets( AnyBuilder builder, int lodIndex )
		{
			int vertexSetCount = 0;

			if ( _sharedVertexData != null )
			{
				builder.AddVertexData( _sharedVertexData );
				vertexSetCount++;
			}

			// Prepare the builder using the submesh information
			for ( int i = 0; i < _subMeshList.Count; i++ )
			{
				SubMesh sm = _subMeshList[ i ];

				if ( sm.useSharedVertices )
				{
					// Use shared vertex data, index as set 0
					if ( lodIndex == 0 )
					{
						// Use shared vertex data, index as set 0
						builder.AddIndexData( sm.indexData, 0, sm.operationType );
					}
					else
					{
						builder.AddIndexData( sm.lodFaceList[ lodIndex - 1 ], 0, sm.operationType );
					}
				}
				else
				{
					// own vertex data, add it and reference it directly
					builder.AddVertexData( sm.vertexData );

					if ( lodIndex == 0 )
					{
						// base index data
						builder.AddIndexData( sm.indexData, vertexSetCount++, sm.operationType );
					}
					else
					{
						// LOD index data
						builder.AddIndexData( sm.lodFaceList[ lodIndex - 1 ], vertexSetCount++, sm.operationType );
					}
				}
			}
		}

		/// <summary>
		///		Builds an edge list for this mesh, which can be used for generating a shadow volume
		///		among other things.
		/// </summary>
		public void BuildEdgeList()
		{
			if ( _edgeListsBuilt )
			{
				return;
			}

			// loop over LODs
			for ( int lodIndex = 0; lodIndex < meshLodUsageList.Count; lodIndex++ )
			{
				// use getLodLevel to enforce loading of manual mesh lods
				MeshLodUsage usage = GetLodLevel( lodIndex );

				if ( _isLodManual && lodIndex != 0 )
				{
					// Delegate edge building to manual mesh
					// It should have already built its own edge list while loading
					usage.EdgeData = usage.ManualMesh.GetEdgeList( 0 );
				}
				else
				{
					EdgeListBuilder builder = new EdgeListBuilder();

					// Add this mesh's vertex and index data structures
					AddVertexAndIndexSets( builder, lodIndex );

					// build the edge data from all accumulate vertex/index buffers
					usage.EdgeData = builder.Build();
				}
			}

			_edgeListsBuilt = true;
		}

		public void FreeEdgeList()
		{
			if ( !_edgeListsBuilt )
				return;

			for ( int i = 0; i < meshLodUsageList.Count; ++i )
			{
				MeshLodUsage usage = meshLodUsageList[ i ];
				usage.EdgeData = null;
			}

			_edgeListsBuilt = false;
		}

		/// <summary>
		///     Create the list of triangles used to query mouse hits
		/// </summary>
		public void CreateTriangleIntersector()
		{
			// Create the TriangleListBuilder instance that will create the list of triangles for this mesh
			TriangleListBuilder builder = new TriangleListBuilder();
			// Add this mesh's vertex and index data structures for lod 0
			AddVertexAndIndexSets( builder, 0 );
			// Create the list of triangles
			_triangleIntersector = new TriangleIntersector( builder.Build() );
		}

		/// <summary>
		///     Builds tangent space vector required for accurate bump mapping.
		/// </summary>
		/// <remarks>
		///    Adapted from bump mapping tutorials at:
		///    http://www.paulsprojects.net/tutorials/simplebump/simplebump.html
		///    author : paul.baker@univ.ox.ac.uk
		///    <p/>
		///    Note: Only the tangent vector is calculated, it is assumed the binormal
		///    will be calculated in a vertex program.
		/// </remarks>
		/// <param name="sourceTexCoordSet">Source texcoord set that holds the current UV texcoords.</param>
		/// <param name="destTexCoordSet">Destination texcoord set to hold the tangent vectors.</param>
		public void BuildTangentVectors( short sourceTexCoordSet, short destTexCoordSet )
		{
			if ( destTexCoordSet == 0 )
			{
				throw new AxiomException( "Destination texture coordinate set must be greater than 0." );
			}

			// temp data buffers
			ushort[] vertIdx = new ushort[ 3 ];
			Vector3[] vertPos = new Vector3[ 3 ];
			float[] u = new float[ 3 ];
			float[] v = new float[ 3 ];

			// setup a new 3D texture coord-set buffer for every sub mesh
			int numSubMeshes = this.SubMeshCount;

			bool sharedGeometryDone = false;

			unsafe
			{
				// setup a new 3D tex coord buffer for every submesh
				for ( int sm = 0; sm < numSubMeshes; sm++ )
				{
					// the face indices buffer, read only
					ushort* pIdx = null;
					// pointer to 2D tex.coords, read only
					float* p2DTC = null;
					// pointer to 3D tex.coords, write/read (discard)
					float* p3DTC = null;
					// vertex position buffer, read only
					float* pVPos = null;

					SubMesh subMesh = GetSubMesh( sm );

					// get index buffer pointer
					IndexData idxData = subMesh.indexData;
					HardwareIndexBuffer buffIdx = idxData.indexBuffer;
					IntPtr indices = buffIdx.Lock( BufferLocking.ReadOnly );
					pIdx = (ushort*)indices.ToPointer();

					// get vertex pointer
					VertexData usedVertexData;

					if ( subMesh.useSharedVertices )
					{
						// don't do shared geometry more than once
						if ( sharedGeometryDone )
						{
							continue;
						}

						usedVertexData = _sharedVertexData;
						sharedGeometryDone = true;
					}
					else
					{
						usedVertexData = subMesh.vertexData;
					}

					VertexDeclaration decl = usedVertexData.vertexDeclaration;
					VertexBufferBinding binding = usedVertexData.vertexBufferBinding;

					// make sure we have a 3D coord to place data in
					OrganizeTangentsBuffer( usedVertexData, destTexCoordSet );

					// get the target element
					VertexElement destElem = decl.FindElementBySemantic( VertexElementSemantic.TexCoords, destTexCoordSet );
					// get the source element
					VertexElement srcElem = decl.FindElementBySemantic( VertexElementSemantic.TexCoords, sourceTexCoordSet );

					if ( srcElem == null || srcElem.Type != VertexElementType.Float2 )
					{
						// TODO: SubMesh names
						throw new AxiomException( "SubMesh '{0}' of Mesh '{1}' has no 2D texture coordinates at the selected set, therefore we cannot calculate tangents.", "<TODO: SubMesh name>", Name );
					}

					HardwareVertexBuffer srcBuffer = null, destBuffer = null, posBuffer = null;

					IntPtr srcPtr, destPtr, posPtr;
					int srcInc, destInc, posInc;

					srcBuffer = binding.GetBuffer( srcElem.Source );

					// Is the source and destination buffer the same?
					if ( srcElem.Source == destElem.Source )
					{
						// lock source for read and write
						srcPtr = srcBuffer.Lock( BufferLocking.Normal );

						srcInc = srcBuffer.VertexSize;
						destPtr = srcPtr;
						destInc = srcInc;
					}
					else
					{
						srcPtr = srcBuffer.Lock( BufferLocking.ReadOnly );
						srcInc = srcBuffer.VertexSize;
						destBuffer = binding.GetBuffer( destElem.Source );
						destInc = destBuffer.VertexSize;
						destPtr = destBuffer.Lock( BufferLocking.Normal );
					}

					VertexElement elemPos = decl.FindElementBySemantic( VertexElementSemantic.Position );

					if ( elemPos.Source == srcElem.Source )
					{
						posPtr = srcPtr;
						posInc = srcInc;
					}
					else if ( elemPos.Source == destElem.Source )
					{
						posPtr = destPtr;
						posInc = destInc;
					}
					else
					{
						// a different buffer
						posBuffer = binding.GetBuffer( elemPos.Source );
						posPtr = posBuffer.Lock( BufferLocking.ReadOnly );
						posInc = posBuffer.VertexSize;
					}

					// loop through all faces to calculate the tangents and normals
					int numFaces = idxData.indexCount / 3;
					int vCount = 0;

					// loop through all faces to calculate the tangents
					for ( int n = 0; n < numFaces; n++ )
					{
						int i = 0;

						for ( i = 0; i < 3; i++ )
						{
							// get indices of vertices that form a polygon in the position buffer
							vertIdx[ i ] = pIdx[ vCount++ ];

							IntPtr tmpPtr = new IntPtr( posPtr.ToInt64() + elemPos.Offset + ( posInc * vertIdx[ i ] ) );

							pVPos = (float*)tmpPtr.ToPointer();

							// get the vertex positions from the position buffer
							vertPos[ i ].x = pVPos[ 0 ];
							vertPos[ i ].y = pVPos[ 1 ];
							vertPos[ i ].z = pVPos[ 2 ];

							// get the vertex tex coords from the 2D tex coord buffer
							tmpPtr = new IntPtr( srcPtr.ToInt64() + srcElem.Offset + ( srcInc * vertIdx[ i ] ) );
							p2DTC = (float*)tmpPtr.ToPointer();

							u[ i ] = p2DTC[ 0 ];
							v[ i ] = p2DTC[ 1 ];
						} // for v = 1 to 3

						// calculate the tangent space vector
						Vector3 tangent =
							Utility.CalculateTangentSpaceVector(
								vertPos[ 0 ], vertPos[ 1 ], vertPos[ 2 ],
								u[ 0 ], v[ 0 ], u[ 1 ], v[ 1 ], u[ 2 ], v[ 2 ] );

						// write new tex.coords
						// note we only write the tangent, not the binormal since we can calculate
						// the binormal in the vertex program
						byte* vBase = (byte*)destPtr.ToPointer();

						for ( i = 0; i < 3; i++ )
						{
							// write values (they must be 0 and we must add them so we can average
							// all the contributions from all the faces
							IntPtr tmpPtr = new IntPtr( destPtr.ToInt64() + destElem.Offset + ( destInc * vertIdx[ i ] ) );

							p3DTC = (float*)tmpPtr.ToPointer();

							p3DTC[ 0 ] += tangent.x;
							p3DTC[ 1 ] += tangent.y;
							p3DTC[ 2 ] += tangent.z;
						} // for v = 1 to 3
					} // for each face

					int numVerts = usedVertexData.vertexCount;

					int offset = 0;

					byte* qBase = (byte*)destPtr.ToPointer();

					// loop through and normalize all 3d tex coords
					for ( int n = 0; n < numVerts; n++ )
					{
						IntPtr tmpPtr = new IntPtr( destPtr.ToInt64() + destElem.Offset + offset );

						p3DTC = (float*)tmpPtr.ToPointer();

						// read the 3d tex coord
						Vector3 temp = new Vector3( p3DTC[ 0 ], p3DTC[ 1 ], p3DTC[ 2 ] );

						// normalize the tex coord
						temp.Normalize();

						// write it back to the buffer
						p3DTC[ 0 ] = temp.x;
						p3DTC[ 1 ] = temp.y;
						p3DTC[ 2 ] = temp.z;

						offset += destInc;
					}

					// unlock all used buffers
					srcBuffer.Unlock();

					if ( destBuffer != null )
					{
						destBuffer.Unlock();
					}

					if ( posBuffer != null )
					{
						posBuffer.Unlock();
					}

					buffIdx.Unlock();
				} // for each subMesh
			} // unsafe
		}

		/// <summary>
		///     Builds tangent space vector required for accurate bump mapping.
		/// </summary>
		/// <remarks>
		///    Adapted from bump mapping tutorials at:
		///    http://www.paulsprojects.net/tutorials/simplebump/simplebump.html
		///    author : paul.baker@univ.ox.ac.uk
		///    <p/>
		///    Note: Only the tangent vector is calculated, it is assumed the binormal
		///    will be calculated in a vertex program.
		/// </remarks>
		public void BuildTangentVectors()
		{
			// default using the first tex coord set and stuffing the tangent vectors in the
			BuildTangentVectors( 0, 1 );
		}

		/// <summary>
		///    Removes all bone assignments for this mesh.
		/// </summary>
		/// <remarks>
		///    This method is for modifying weights to the shared geometry of the Mesh. To assign
		///    weights to the per-SubMesh geometry, see the equivalent methods on SubMesh.
		/// </remarks>
		public void ClearBoneAssignments()
		{
			_boneAssignmentList.Clear();
			boneAssignmentsOutOfDate = true;
		}

		/// <summary>
		///    Compile bone assignments into blend index and weight buffers.
		/// </summary>
		protected internal void CompileBoneAssignments()
		{
			int maxBones = RationalizeBoneAssignments( _sharedVertexData.vertexCount, _boneAssignmentList );

			// check for no bone assignments
			if ( maxBones == 0 )
			{
				return;
			}

			CompileBoneAssignments( _boneAssignmentList, maxBones, _sharedVertexData );

			boneAssignmentsOutOfDate = false;
		}

		/// <summary>
		///    Software blending oriented bone assignment compilation.
		/// </summary>
		protected internal void CompileBoneAssignments( Dictionary<int, List<VertexBoneAssignment>> boneAssignments,
													   int numBlendWeightsPerVertex, VertexData targetVertexData )
		{
			// Create or reuse blend weight / indexes buffer
			// Indices are always a UBYTE4 no matter how many weights per vertex
			// Weights are more specific though since they are Reals
			VertexDeclaration decl = targetVertexData.vertexDeclaration;
			VertexBufferBinding bind = targetVertexData.vertexBufferBinding;
			short bindIndex;

			VertexElement testElem = decl.FindElementBySemantic( VertexElementSemantic.BlendIndices );

			if ( testElem != null )
			{
				// Already have a buffer, unset it & delete elements
				bindIndex = testElem.Source;

				// unset will cause deletion of buffer
				bind.UnsetBinding( bindIndex );
				decl.RemoveElement( VertexElementSemantic.BlendIndices );
				decl.RemoveElement( VertexElementSemantic.BlendWeights );
			}
			else
			{
				// Get new binding
				bindIndex = bind.NextIndex;
			}

			int bufferSize = Marshal.SizeOf( typeof( byte ) ) * 4;
			bufferSize += Marshal.SizeOf( typeof( float ) ) * numBlendWeightsPerVertex;

			HardwareVertexBuffer vbuf =
				HardwareBufferManager.Instance.CreateVertexBuffer(
					bufferSize,
					targetVertexData.vertexCount,
					BufferUsage.StaticWriteOnly,
					true ); // use shadow buffer

			// bind new buffer
			bind.SetBinding( bindIndex, vbuf );

			VertexElement idxElem, weightElem;

			VertexElement firstElem = decl.GetElement( 0 );

			// add new vertex elements
			// Note, insert directly after position to abide by pre-Dx9 format restrictions
			if ( firstElem.Semantic == VertexElementSemantic.Position )
			{
				int insertPoint = 1;

				while ( insertPoint < decl.ElementCount &&
					decl.GetElement( insertPoint ).Source == firstElem.Source )
				{

					insertPoint++;
				}

				idxElem = decl.InsertElement( insertPoint, bindIndex, 0, VertexElementType.UByte4,
					VertexElementSemantic.BlendIndices );

				weightElem = decl.InsertElement( insertPoint + 1, bindIndex, Marshal.SizeOf( typeof( byte ) ) * 4,
					VertexElement.MultiplyTypeCount( VertexElementType.Float1, numBlendWeightsPerVertex ),
					VertexElementSemantic.BlendWeights );
			}
			else
			{
				// Position is not the first semantic, therefore this declaration is
				// not pre-Dx9 compatible anyway, so just tack it on the end
				idxElem = decl.AddElement( bindIndex, 0, VertexElementType.UByte4, VertexElementSemantic.BlendIndices );
				weightElem = decl.AddElement( bindIndex, Marshal.SizeOf( typeof( byte ) ) * 4,
					VertexElement.MultiplyTypeCount( VertexElementType.Float1, numBlendWeightsPerVertex ),
					VertexElementSemantic.BlendWeights );
			}


			// Assign data
			IntPtr ptr = vbuf.Lock( BufferLocking.Discard );

			unsafe
			{
				byte* pBase = (byte*)ptr.ToPointer();

				// Iterate by vertex
				float* pWeight;
				byte* pIndex;

				//for ( int v = 0; v < targetVertexData.vertexCount; v++ )
				foreach ( KeyValuePair<int, List<VertexBoneAssignment>> boneAssignment in boneAssignments )
				{
					/// Convert to specific pointers
					pWeight = (float*)( (byte*)pBase + weightElem.Offset );
					pIndex = pBase + idxElem.Offset;

					// get the bone assignment enumerator and move to the first one in the list
					//List<VertexBoneAssignment> vbaList = boneAssignments[ v ];
					List<VertexBoneAssignment> vbaList = boneAssignment.Value;

					for ( int bone = 0; bone < numBlendWeightsPerVertex; bone++ )
					{
						// Do we still have data for this vertex?
						if ( bone < vbaList.Count )
						{
							VertexBoneAssignment ba = vbaList[ bone ];
							// If so, write weight
							*pWeight++ = ba.weight;
							*pIndex++ = (byte)ba.boneIndex;
						}
						else
						{
							// Ran out of assignments for this vertex, use weight 0 to indicate empty
							*pWeight++ = 0.0f;
							*pIndex++ = 0;
						}
					}

					pBase += vbuf.VertexSize;
				}
			}

			vbuf.Unlock();
		}

		/// <summary>
		///    Internal method for making the space for a 3D texture coord buffer to hold tangents.
		/// </summary>
		/// <param name="vertexData">Target vertex data.</param>
		/// <param name="destCoordSet">Destination texture coordinate set.</param>
		protected void OrganizeTangentsBuffer( VertexData vertexData, short destCoordSet )
		{
			bool needsToBeCreated = false;

			// grab refs to the declarations and bindings
			VertexDeclaration decl = vertexData.vertexDeclaration;
			VertexBufferBinding binding = vertexData.vertexBufferBinding;

			// see if we already have a 3D tex coord buffer
			VertexElement tex3d = decl.FindElementBySemantic( VertexElementSemantic.TexCoords, destCoordSet );

			if ( tex3d == null )
			{
				needsToBeCreated = true;
			}
			else if ( tex3d.Type != VertexElementType.Float3 )
			{
				// tex coord buffer exists, but is not 3d.
				throw new AxiomException( "Texture coordinate set {0} already exists but is not 3D, therefore cannot contain tangents. Pick an alternative destination coordinate set.", destCoordSet );
			}

			if ( needsToBeCreated )
			{
				// What we need to do, to be most efficient with our vertex streams,
				// is to tack the new 3D coordinate set onto the same buffer as the
				// previous texture coord set
				VertexElement prevTexCoordElem =
					vertexData.vertexDeclaration.FindElementBySemantic(
						VertexElementSemantic.TexCoords, (short)( destCoordSet - 1 ) );

				if ( prevTexCoordElem == null )
				{
					throw new AxiomException( "Cannot locate the texture coordinate element preceding the destination texture coordinate set to which to append the new tangents." );
				}

				// find the buffer associated with this element
				HardwareVertexBuffer origBuffer = vertexData.vertexBufferBinding.GetBuffer( prevTexCoordElem.Source );

				// Now create a new buffer, which includes the previous contents
				// plus extra space for the 3D coords
				HardwareVertexBuffer newBuffer = HardwareBufferManager.Instance.CreateVertexBuffer(
					origBuffer.VertexSize + ( 3 * Marshal.SizeOf( typeof( float ) ) ),
					vertexData.vertexCount,
					origBuffer.Usage,
					origBuffer.HasShadowBuffer );

				// add the new element
				decl.AddElement(
					prevTexCoordElem.Source,
					origBuffer.VertexSize,
					VertexElementType.Float3,
					VertexElementSemantic.TexCoords,
					destCoordSet );

				// now copy the original data across
				IntPtr srcPtr = origBuffer.Lock( BufferLocking.ReadOnly );
				IntPtr destPtr = newBuffer.Lock( BufferLocking.Discard );

				int vertSize = origBuffer.VertexSize;

				// size of the element to skip
				int elemSize = Marshal.SizeOf( typeof( float ) ) * 3;

				for ( int i = 0, srcOffset = 0, dstOffset = 0; i < vertexData.vertexCount; i++ )
				{
					// copy original vertex data
					Memory.Copy( srcPtr, destPtr, srcOffset, dstOffset, vertSize );

					srcOffset += vertSize;
					dstOffset += vertSize;

					// Set the new part to 0 since we'll accumulate in this
					Memory.Set( destPtr, dstOffset, elemSize );
					dstOffset += elemSize;
				}

				// unlock those buffers!
				origBuffer.Unlock();
				newBuffer.Unlock();

				// rebind the new buffer
				binding.SetBinding( prevTexCoordElem.Source, newBuffer );
			}
		}

		/// <summary>
		///     Ask the mesh to suggest parameters to a future <see cref="BuildTangentVectors"/> call.
		/// </summary>
		/// <remarks>
		///     This helper method will suggest source and destination texture coordinate sets
		///     for a call to <see cref="BuildTangentVectors"/>. It will detect when there are inappropriate
		///     conditions (such as multiple geometry sets which don't agree).
		///     Moreover, it will return 'true' if it detects that there are aleady 3D
		///     coordinates in the mesh, and therefore tangents may have been prepared already.
		/// </remarks>
		/// <param name="sourceCoordSet">A source texture coordinate set which will be populated.</param>
		/// <param name="destCoordSet">A destination texture coordinate set which will be populated.</param>
		public bool SuggestTangentVectorBuildParams( out short sourceCoordSet, out short destCoordSet )
		{
			// initialize out params
			sourceCoordSet = 0;
			destCoordSet = 0;

			// Go through all the vertex data and locate source and dest (must agree)
			bool sharedGeometryDone = false;
			bool foundExisting = false;
			bool firstOne = true;

			for ( int i = 0; i < _subMeshList.Count; i++ )
			{
				SubMesh sm = _subMeshList[ i ];

				VertexData vertexData;

				if ( sm.useSharedVertices )
				{
					if ( sharedGeometryDone )
					{
						continue;
					}

					vertexData = _sharedVertexData;
					sharedGeometryDone = true;
				}
				else
				{
					vertexData = sm.vertexData;
				}

				VertexElement sourceElem = null;

				short t = 0;

				for ( ; t < Config.MaxTextureCoordSets; t++ )
				{
					VertexElement testElem =
						vertexData.vertexDeclaration.FindElementBySemantic( VertexElementSemantic.TexCoords, t );

					if ( testElem == null )
					{
						// finish if we've run out, t will be the target
						break;
					}

					if ( sourceElem == null )
					{
						// We're still looking for the source texture coords
						if ( testElem.Type == VertexElementType.Float2 )
						{
							// ok, we found it!
							sourceElem = testElem;
						}
					}
					else
					{
						// We're looking for the destination
						// Check to see if we've found a possible
						if ( testElem.Type == VertexElementType.Float3 )
						{
							// This is a 3D set, might be tangents
							foundExisting = true;
						}
					}
				} // for t

				// After iterating, we should have a source and a possible destination (t)
				if ( sourceElem == null )
				{
					throw new AxiomException( "Cannot locate an appropriate 2D texture coordinate set for all the vertex data in this mesh to create tangents from." );
				}

				// Check that we agree with previous decisions, if this is not the first one
				if ( !firstOne )
				{
					if ( sourceElem.Index != sourceCoordSet )
					{
						throw new AxiomException( "Multiple sets of vertex data in this mesh disagree on the appropriate index to use for the source texture coordinates. This ambiguity must be rectified before tangents can be generated." );
					}
					if ( t != destCoordSet )
					{
						throw new AxiomException( "Multiple sets of vertex data in this mesh disagree on the appropriate index to use for the target texture coordinates. This ambiguity must be rectified before tangents can be generated." );
					}
				}

				// Otherwise, save this result
				sourceCoordSet = (short)sourceElem.Index;
				destCoordSet = t;

				firstOne = false;
			} // for i

			return foundExisting;
		}

		/// <summary>
		///    Gets the sub mesh at the specified index.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public SubMesh GetSubMesh( int index )
		{
			Debug.Assert( index < _subMeshList.Count, "index < subMeshList.Count" );

			return _subMeshList[ index ];
		}

		/// <summary>
		///   Gets the animation track handle for a named submesh.
		/// </summary>
		/// <param name="name">The name of the submesh</param>
		/// <returns>The track handle to use for animation tracks associated with the give submesh</returns>
		public int GetTrackHandle( string name )
		{
			for ( int i = 0; i < _subMeshList.Count; i++ )
			{
				if ( _subMeshList[ i ].name == name )
				{
					return i + 1;
				}
			}

			// not found
			throw new AxiomException( "A SubMesh with the name '{0}' does not exist in mesh '{1}'", name, this.Name );
		}

		/// <summary>
		///     Gets the sub mesh with the specified name.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public SubMesh GetSubMesh( string name )
		{
			for ( int i = 0; i < _subMeshList.Count; i++ )
			{
				SubMesh sub = _subMeshList[ i ];

				if ( sub.name == name )
				{
					return sub;
				}
			}

			// not found
			throw new AxiomException( "A SubMesh with the name '{0}' does not exist in mesh '{1}'", name, this.Name );
		}

		/// <summary>
		///    Remove the sub mesh with the given name.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public void RemoveSubMesh( string name )
		{
			for ( int i = 0; i < _subMeshList.Count; i++ )
			{
				SubMesh sub = _subMeshList[ i ];

				if ( sub.name == name )
				{
					_subMeshList.RemoveAt( i );
					return;
				}
			}

			// not found
			throw new AxiomException( "A SubMesh with the name '{0}' does not exist in mesh '{1}'", name, this.Name );
		}


		/// <summary>
		///    Initialise an animation set suitable for use with this mesh.
		/// </summary>
		/// <remarks>
		///    Only recommended for use inside the engine, not by applications.
		/// </remarks>
		/// <param name="animSet"></param>
		public void InitAnimationState( AnimationStateSet animSet )
		{
			//             Debug.Assert(skeleton != null, "Skeleton not present.");

			if ( HasSkeleton )
			{
				// delegate the animation set to the skeleton
				_skeleton.InitAnimationState( animSet );

				// Take the opportunity to update the compiled bone assignments
				if ( boneAssignmentsOutOfDate )
				{
					CompileBoneAssignments();
				}

				// compile bone assignments for each sub mesh
				for ( int i = 0; i < _subMeshList.Count; i++ )
				{
					SubMesh subMesh = _subMeshList[ i ];

					if ( subMesh.boneAssignmentsOutOfDate )
					{
						subMesh.CompileBoneAssignments();
					}
				} // for
			}

			// Animation states for vertex animation
			foreach ( Animation animation in _animationsList.Values )
			{
				// Only create a new animation state if it doesn't exist
				// We can have the same named animation in both skeletal and vertex
				// with a shared animation state affecting both, for combined effects
				// The animations should be the same length if this feature is used!
				if ( !HasAnimationState( animSet, animation.Name ) )
				{
					animSet.CreateAnimationState( animation.Name, 0.0f, animation.Length );
				}
			}
		}

		/// <summary>Returns whether or not this mesh has some kind of vertex animation.</summary>
		public bool HasAnimationState( AnimationStateSet animSet, string name )
		{
			return animSet.HasAnimationState( name );
		}

		/// <summary>Returns whether or not this mesh has the named vertex animation.</summary>
		public bool ContainsAnimation( string name )
		{
			return _animationsList.ContainsKey( name );
		}

		/// <summary>
		///    Internal notification, used to tell the Mesh which Skeleton to use without loading it.
		/// </summary>
		/// <remarks>
		///    This is only here for unusual situation where you want to manually set up a
		///    Skeleton. Best to let the engine deal with this, don't call it yourself unless you
		///    really know what you're doing.
		/// </remarks>
		/// <param name="skeleton"></param>
		public void NotifySkeleton( Skeleton skeleton )
		{
			this._skeleton = skeleton;
			_skeletonName = skeleton.Name;
		}

		/// <summary>
		///		This method prepares the mesh for generating a renderable shadow volume.
		/// </summary>
		/// <remarks>
		///		Preparing a mesh to generate a shadow volume involves firstly ensuring that the
		///		vertex buffer containing the positions for the mesh is a standalone vertex buffer,
		///		with no other components in it. This method will therefore break apart any existing
		///		vertex buffers this mesh holds if position is sharing a vertex buffer.
		///		Secondly, it will double the size of this vertex buffer so that there are 2 copies of
		///		the position data for the mesh. The first half is used for the original, and the second
		///		half is used for the 'extruded' version of the mesh. The vertex count of the main
		///		<see cref="VertexData"/> used to render the mesh will remain the same though, so as not to add any
		///		overhead to regular rendering of the object.
		///		Both copies of the position are required in one buffer because shadow volumes stretch
		///		from the original mesh to the extruded version.
		///		<p/>
		///		Because shadow volumes are rendered in turn, no additional
		///		index buffer space is allocated by this method, a shared index buffer allocated by the
		///		shadow rendering algorithm is used for addressing this extended vertex buffer.
		/// </remarks>
		public void PrepareForShadowVolume()
		{
			if ( _isPreparedForShadowVolumes )
			{
				return;
			}

			if ( _sharedVertexData != null )
			{
				_sharedVertexData.PrepareForShadowVolume();
			}

			for ( int i = 0; i < _subMeshList.Count; i++ )
			{
				SubMesh sm = _subMeshList[ i ];

				if ( !sm.useSharedVertices )
				{
					sm.vertexData.PrepareForShadowVolume();
				}
			}

			_isPreparedForShadowVolumes = true;
		}

		/// <summary>
		///     Rationalizes the passed in bone assignment list.
		/// </summary>
		/// <remarks>
		///     We support up to 4 bone assignments per vertex. The reason for this limit
		///     is that this is the maximum number of assignments that can be passed into
		///     a hardware-assisted blending algorithm. This method identifies where there are
		///     more than 4 bone assignments for a given vertex, and eliminates the bone
		///     assignments with the lowest weights to reduce to this limit. The remaining
		///     weights are then re-balanced to ensure that they sum to 1.0.
		/// </remarks>
		/// <param name="vertexCount">The number of vertices.</param>
		/// <param name="assignments">
		///     The bone assignment list to rationalize. This list will be modified and
		///     entries will be removed where the limits are exceeded.
		/// </param>
		/// <returns>The maximum number of bone assignments per vertex found, clamped to [1-4]</returns>
		internal int RationalizeBoneAssignments( int vertexCount, Dictionary<int, List<VertexBoneAssignment>> assignments )
		{
			int maxBones = 0;
			int currentBones = 0;

			//for ( int i = 0; i < vertexCount; i++ )
			foreach ( KeyValuePair<int, List<VertexBoneAssignment>> assignment in assignments )
			{
				// gets the numbers of assignments for the current vertex
				currentBones = assignment.Value.Count;

				// Deal with max bones update
				// (note this will record maxBones even if they exceed limit)
				if ( maxBones < currentBones )
				{
					maxBones = currentBones;
				}

				// does the number of bone assignments exceed limit?
				if ( currentBones > Config.MaxBlendWeights )
				{
					//List<VertexBoneAssignment> sortedList = assignments[ i ];
					List<VertexBoneAssignment> sortedList = assignment.Value;
					IComparer<VertexBoneAssignment> comp = new VertexBoneAssignmentWeightComparer();
					sortedList.Sort( comp );
					sortedList.RemoveRange( 0, currentBones - Config.MaxBlendWeights );
				}

				float totalWeight = 0.0f;

				// Make sure the weights are normalised
				// Do this irrespective of whether we had to remove assignments or not
				//   since it gives us a guarantee that weights are normalised
				//  We assume this, so it's a good idea since some modellers may not
				//List<VertexBoneAssignment> vbaList = assignments[ i ];
				List<VertexBoneAssignment> vbaList = assignment.Value;

				foreach ( VertexBoneAssignment vba in vbaList )
				{
					totalWeight += vba.weight;
				}

				// Now normalise if total weight is outside tolerance
				float delta = 1.0f / ( 1 << 24 );
				if ( !Utility.RealEqual( totalWeight, 1.0f, delta ) )
				{
					foreach ( VertexBoneAssignment vba in vbaList )
					{
						vba.weight /= totalWeight;
					}
				}
			}

			// Warn that we've reduced bone assignments
			if ( maxBones > Config.MaxBlendWeights )
			{
				LogManager.Instance.Write( "WARNING: Mesh '{0}' includes vertices with more than {1} bone assignments.  The lowest weighted assignments beyond this limit have been removed.", Name, Config.MaxBlendWeights );

				maxBones = Config.MaxBlendWeights;
			}

			return maxBones;
		}

		/// <summary>
		///		Creates a new <see cref="SubMesh"/> and gives it a name.
		/// </summary>
		/// <param name="name">Name of the new <see cref="SubMesh"/>.</param>
		/// <returns>A new <see cref="SubMesh"/> with this Mesh as its parent.</returns>
		public SubMesh CreateSubMesh( string name )
		{
			SubMesh subMesh = new SubMesh();
			subMesh.Name = name;

			// set the parent of the subMesh to us
			subMesh.Parent = this;

			// add to the list of child meshes
			_subMeshList.Add( subMesh );

			return subMesh;
		}

		/// <summary>
		///		Creates a new <see cref="SubMesh"/>.
		/// </summary>
		/// <remarks>
		///		Method for manually creating geometry for the mesh.
		///		Note - use with extreme caution - you must be sure that
		///		you have set up the geometry properly.
		/// </remarks>
		/// <returns>A new SubMesh with this Mesh as its parent.</returns>
		public SubMesh CreateSubMesh()
		{
			return CreateSubMesh( String.Empty );
		}

		/// <summary>
		///		Sets the policy for the vertex buffers to be used when loading this Mesh.
		/// </summary>
		/// <remarks>
		///		By default, when loading the Mesh, static, write-only vertex and index buffers
		///		will be used where possible in order to improve rendering performance.
		///		However, such buffers
		///		cannot be manipulated on the fly by CPU code (although shader code can). If you
		///		wish to use the CPU to modify these buffers, you should call this method. Note,
		///		however, that it only takes effect after the Mesh has been reloaded. Note that you
		///		still have the option of manually repacing the buffers in this mesh with your
		///		own if you see fit too, in which case you don't need to call this method since it
		///		only affects buffers created by the mesh itself.
		///		<p/>
		///		You can define the approach to a Mesh by changing the default parameters to
		///		<see cref="MeshManager.Load"/> if you wish; this means the Mesh is loaded with those options
		///		the first time instead of you having to reload the mesh after changing these options.
		/// </remarks>
		/// <param name="usage">The usage flags, which by default are <see cref="BufferUsage.StaticWriteOnly"/></param>
		/// <param name="useShadowBuffer">
		///		If set to true, the vertex buffers will be created with a
		///		system memory shadow buffer. You should set this if you want to be able to
		///		read from the buffer, because reading from a hardware buffer is a no-no.
		/// </param>
		public void SetVertexBufferPolicy( BufferUsage usage, bool useShadowBuffer )
		{
			_vertexBufferUsage = usage;
			_useVertexShadowBuffer = useShadowBuffer;
		}

		/// <summary>
		///		Sets the policy for the index buffers to be used when loading this Mesh.
		/// </summary>
		/// <remarks>
		///		By default, when loading the Mesh, static, write-only vertex and index buffers
		///		will be used where possible in order to improve rendering performance.
		///		However, such buffers
		///		cannot be manipulated on the fly by CPU code (although shader code can). If you
		///		wish to use the CPU to modify these buffers, you should call this method. Note,
		///		however, that it only takes effect after the Mesh has been reloaded. Note that you
		///		still have the option of manually repacing the buffers in this mesh with your
		///		own if you see fit too, in which case you don't need to call this method since it
		///		only affects buffers created by the mesh itself.
		///		<p/>
		///		You can define the approach to a Mesh by changing the default parameters to
		///		<see cref="MeshManager.Load"/> if you wish; this means the Mesh is loaded with those options
		///		the first time instead of you having to reload the mesh after changing these options.
		/// </remarks>
		/// <param name="usage">The usage flags, which by default are <see cref="BufferUsage.StaticWriteOnly"/></param>
		/// <param name="useShadowBuffer">
		///		If set to true, the index buffers will be created with a
		///		system memory shadow buffer. You should set this if you want to be able to
		///		read from the buffer, because reading from a hardware buffer is a no-no.
		/// </param>
		public void SetIndexBufferPolicy( BufferUsage usage, bool useShadowBuffer )
		{
			_indexBufferUsage = usage;
			_useIndexShadowBuffer = useShadowBuffer;
		}

		/// <summary>
		///   This method is fairly internal, and is used to add new manual lod info
		/// </summary>
		/// <param name="manualLodEntries"></param>
		public void AddManualLodEntries( List<MeshLodUsage> manualLodEntries )
		{
			Debug.Assert( meshLodUsageList.Count == 1 );
			_isLodManual = true;
			foreach ( MeshLodUsage usage in manualLodEntries )
				meshLodUsageList.Add( usage );
		}

		/// <summary>
		///   TODO: should this replace an existing attachment point with the same name?
		/// </summary>
		/// <param name="name"></param>
		/// <param name="rotation"></param>
		/// <param name="translation"></param>
		/// <returns></returns>
		public virtual AttachmentPoint CreateAttachmentPoint( string name, Quaternion rotation, Vector3 translation )
		{
			AttachmentPoint ap = new AttachmentPoint( name, null, rotation, translation );
			_attachmentPoints.Add( ap );
			return ap;
		}


		/// <summary>
		///	    Internal method which, if animation types have not been determined,
		///	    scans any vertex animations and determines the type for each set of
		///	    vertex data (cannot have 2 different types).
		/// </summary>
		public void DetermineAnimationTypes()
		{
			// Don't check flag here; since detail checks on track changes are not
			// done, allow caller to force if they need to

			// Initialise all types to nothing
			_sharedVertexDataAnimationType = VertexAnimationType.None;
			for ( int sm = 0; sm < this.SubMeshCount; sm++ )
			{
				SubMesh subMesh = GetSubMesh( sm );
				subMesh.VertexAnimationType = VertexAnimationType.None;
			}

			// Scan all animations and determine the type of animation tracks
			// relating to each vertex data
			foreach ( Animation anim in _animationsList.Values )
			{
				foreach ( VertexAnimationTrack track in anim.VertexTracks.Values )
				{
					ushort handle = track.Handle;
					if ( handle == 0 )
					{
						// shared data
						if ( _sharedVertexDataAnimationType != VertexAnimationType.None &&
							_sharedVertexDataAnimationType != track.AnimationType )
						{
							// Mixing of morph and pose animation on same data is not allowed
							throw new Exception( "Animation tracks for shared vertex data on mesh "
												+ Name + " try to mix vertex animation types, which is " +
												"not allowed, in Mesh.DetermineAnimationTypes" );
						}
						_sharedVertexDataAnimationType = track.AnimationType;
					}
					else
					{
						// submesh index (-1)
						SubMesh sm = GetSubMesh( handle - 1 );
						if ( sm.CurrentVertexAnimationType != VertexAnimationType.None &&
							sm.CurrentVertexAnimationType != track.AnimationType )
						{
							// Mixing of morph and pose animation on same data is not allowed
							throw new Exception( string.Format( "Animation tracks for dedicated vertex data {0}  on mesh {1}",
															  handle - 1, Name ) +
												" try to mix vertex animation types, which is " +
												"not allowed, in Mesh.DetermineAnimationTypes" );
						}
						sm.VertexAnimationType = track.AnimationType;
					}
				}
			}

			_animationTypesDirty = false;
		}

		/// <summary>
		///     Creates a new Animation object for vertex animating this mesh.
		/// </summary>
		/// <param name="name">The name of this animation</param>
		/// <param name="length">The length of the animation in seconds</param>
		public Animation CreateAnimation( string name, float length )
		{
			// Check name not used
			if ( _animationsList.ContainsKey( name ) )
			{
				throw new Exception( "An animation with the name " + name + " already exists" +
									", in Mesh.CreateAnimation" );
			}
			Animation ret = new Animation( name, length );
			// Add to list
			_animationsList[ name ] = ret;
			// Mark animation types dirty
			_animationTypesDirty = true;
			return ret;
		}

		/// <summary>
		///     Returns the named vertex Animation object.
		/// </summary>
		/// <param name="name">The name of the animation</param>
		public Animation GetAnimation( string name )
		{
			Animation ret;
			if ( !_animationsList.TryGetValue( name, out ret ) )
				return null;
			return ret;
		}

		/// <summary>Gets a single morph animation by index.</summary>
		// ??? Not sure this is right - - it's depending on the order of
		// ??? insertion, which seems really wrong for a dictionary
		public Animation GetAnimation( ushort index )
		{
			// If you hit this assert, then the index is out of bounds.
			Debug.Assert( index < _animationsList.Count );
			// ??? The only way I can figure out to do this is with
			// ??? a loop over the elements.
			ushort i = 0;
			foreach ( Animation animation in _animationsList.Values )
			{
				if ( i == index )
					return animation;
				i++;
			}
			// Make compiler happy
			return null;
		}

		/// <summary>Returns whether this mesh contains the named vertex animation.</summary>
		public bool HasAnimation( string name )
		{
			return _animationsList.ContainsKey( name );
		}

		/// <summary>Removes vertex Animation from this mesh.</summary>
		public void RemoveAnimation( string name )
		{
			if ( !HasAnimation( name ) )
			{
				throw new Exception( "No animation entry found named " + name +
									", in Mesh.RemoveAnimation" );
			}
			_animationsList.Remove( name );
			_animationTypesDirty = true;
		}

		/// <summary>Removes all morph Animations from this mesh.</summary>
		public void RemoveAllAnimations()
		{
			_animationsList.Clear();
			_animationTypesDirty = true;
		}

		/// <summary>
		///     Gets a pointer to a vertex data element based on a morph animation
		///    	track handle.
		/// </summary>
		/// <remarks>
		///	    0 means the shared vertex data, 1+ means a submesh vertex data (index+1)
		/// </remarks>
		public VertexData GetVertexDataByTrackHandle( ushort handle )
		{
			if ( handle == 0 )
				return _sharedVertexData;
			else
				return GetSubMesh( handle - 1 ).vertexData;
		}

		/// <summary>
		///     Create a new Pose for this mesh or one of its submeshes.
		/// </summary>
		/// <param name="target">
		///     The target geometry index; 0 is the shared Mesh geometry, 1+ is the
		///    	dedicated SubMesh geometry belonging to submesh index + 1.
		/// </param>
		/// <param name="name">Name to give the pose, which is optional</param>
		/// <returns>A new Pose ready for population</returns>
		public Pose CreatePose( ushort target, string name )
		{
			Pose retPose = new Pose( target, name );
			PoseList.Add( retPose );
			return retPose;
		}

		/// <summary>Retrieve an existing Pose by index.</summary>
		public Pose GetPose( ushort index )
		{
			if ( index >= PoseList.Count )
				throw new Exception( "Index out of bounds, in Mesh.GetPose" );
			return _poseList[ index ];
		}

		/// <summary>Retrieve an existing Pose by name.</summary>
		public Pose GetPose( string name )
		{
			foreach ( Pose pose in PoseList )
			{
				if ( pose.Name == name )
					return pose;
			}
			throw new Exception( "No pose called " + name + " found in Mesh " + name +
								", in Mesh.GetPose" );
		}
		/// <summary>Retrieve an existing Pose index by name.</summary>
		public ushort GetPoseIndex( string name )
		{
			for ( ushort i = 0; i < PoseList.Count; i++ )
			{
				if ( PoseList[ i ].Name == name )
					return i;
			}
			throw new Exception( "No pose called " + name + " found in Mesh " + this.Name +
								", in Mesh.GetPoseIndex" );
		}

		/// <summary>Destroy a pose by index.</summary>
		/// <remarks>This will invalidate any animation tracks referring to this pose or those after it.</remarks>
		public void RemovePose( ushort index )
		{
			if ( index >= _poseList.Count )
			{
				throw new Exception( "Index out of bounds, in Mesh.RemovePose" );
			}
			PoseList.RemoveAt( index );
		}

		/// <summary>Destroy a pose by name.</summary>
		/// <remarks>This will invalidate any animation tracks referring to this pose or those after it.</remarks>
		public void RemovePose( string name )
		{
			for ( int i = 0; i < _poseList.Count; i++ )
			{
				Pose pose = PoseList[ i ];
				if ( pose.Name == name )
				{
					PoseList.RemoveAt( i );
					return;
				}
			}
			throw new Exception( "No pose called " + name + " found in Mesh " + name +
								"Mesh.RemovePose" );
		}

		/// <summary>Destroy all poses.</summary>
		public void RemoveAllPoses()
		{
			_poseList.Clear();
		}

		#endregion Methods

		#region Mesh Level of Detail

		#region IsLodManual Property

		/// <summary>
		///	Flag indicating the use of manually created LOD meshes.
		/// </summary>
		private bool _isLodManual;
		/// <summary>
		/// Returns true if this mesh is using manual LOD.
		/// </summary>
		/// <remarks>
		/// A mesh can either use automatically generated LOD, or it can use alternative
		/// meshes as provided by an artist. A mesh can only use either all manual LODs
		/// or all generated LODs, not a mixture of both.
		/// </remarks>
		public bool IsLodManual
		{
			get
			{
				return _isLodManual;
			}
			protected internal set
			{
				_isLodManual = value;
			}
		}

		#endregion IsLodManual Property

		#region LodStrategy Property

		private LodStrategy _lodStrategy;
		public LodStrategy LodStrategy
		{
			get
			{
				return _lodStrategy;
			}
			set
			{
				_lodStrategy = value;
				Debug.Assert( this.meshLodUsageList.Count > 0 );

				this.meshLodUsageList[ 0 ].Value = this._lodStrategy.BaseValue;

				// Re-transform user lod values (starting at index 1, no need to transform base value)
				foreach ( MeshLodUsage meshLodUsage in meshLodUsageList )
				{
					meshLodUsage.Value = this._lodStrategy.TransformUserValue( meshLodUsage.UserValue );
				}
			}
		}

		#endregion LodStrategy Property

		/// <summary>
		///	List of data structures describing LOD usage.
		/// </summary>
		protected MeshLodUsageList meshLodUsageList = new MeshLodUsageList();

		public MeshLodUsageList MeshLodUsageList
		{
			get
			{
				return meshLodUsageList;
			}
		}
		/// <summary>
		///	Gets the current number of Lod levels associated with this mesh.
		/// </summary>
		public int LodLevelCount
		{
			get
			{
				return meshLodUsageList.Count;
			}
		}

		public void GenerateLodLevels( LodValueList lodValues, ProgressiveMesh.VertexReductionQuota reductionMethod, Real reductionValue )
		{
			RemoveLodLevels();

			LogManager.Instance.Write( "Generating {0} lower LODs for mesh {1}.", lodValues.Count, Name );

			foreach ( SubMesh subMesh in _subMeshList )
			{
				// check if triangles are present
				if ( subMesh.IndexData.indexCount > 0 )
				{
					// Set up data for reduction
					VertexData vertexData = subMesh.useSharedVertices ? _sharedVertexData : subMesh.vertexData;

					ProgressiveMesh pm = new ProgressiveMesh( vertexData, subMesh.indexData );
					pm.Build( (ushort)lodValues.Count, subMesh.lodFaceList, reductionMethod, reductionValue );
				}
				else
				{
					// create empty index data for each lod
					for ( int i = 0; i < lodValues.Count; ++i )
					{
						subMesh.LodFaceList.Add( new IndexData() );
					}
				}
			}

			// Iterate over the lods and record usage
			foreach ( Real value in lodValues )
			{
				// Record usage
				MeshLodUsage lod = new MeshLodUsage();
				lod.UserValue = value;
				lod.Value = _lodStrategy.TransformUserValue( value );
				lod.EdgeData = null;
				lod.ManualMesh = null;
				meshLodUsageList.Add( lod );
			}
		}

		public void RemoveLodLevels()
		{
			if ( !this.IsLodManual )
			{
				foreach ( SubMesh subMesh in this._subMeshList )
					subMesh.RemoveLodLevels();
			}

			FreeEdgeList();
			this.meshLodUsageList.Clear();
			MeshLodUsage lod = new MeshLodUsage();
			lod.UserValue = float.NaN;
			lod.Value = _lodStrategy.BaseValue;
			lod.EdgeData = null;
			lod.ManualMesh = null;
			this.meshLodUsageList.Add( lod );
			this._isLodManual = false;
		}

		/// <summary>
		///    Retrieves the level of detail index for the given lod value.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public int GetLodIndex( Real value )
		{
			return _lodStrategy.GetIndex( value, meshLodUsageList );
		}

		/// <summary>
		///    Gets the mesh lod level at the specified index.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public MeshLodUsage GetLodLevel( int index )
		{
			Debug.Assert( index < meshLodUsageList.Count, "index < lodUsageList.Count" );

			MeshLodUsage usage = meshLodUsageList[ index ];

			// load the manual lod mesh for this level if not done already
			if ( _isLodManual && index > 0 && usage.ManualMesh == null )
			{
				usage.ManualMesh = MeshManager.Instance.Load( usage.ManualName, Group );

				// get the edge data, if required
				if ( !_autoBuildEdgeLists )
				{
					usage.EdgeData = usage.ManualMesh.GetEdgeList( 0 );
				}
			}

			return usage;
		}

		public void SetLodInfo( int levelCount, bool isManual )
		{
#warning Implement Mesh.edgeListsBuilt
			//Debug.Assert( !this.edgeListsBuilt, "Can't modify LOD after edge lists built.");\

			// Basic prerequisites
			Debug.Assert( levelCount > 0, "Must be at least one level (full detail level must exist)" );

			//mNumLods = numLevels;
			//mMeshLodUsageList.resize( numLevels );
			//// Resize submesh face data lists too
			//for ( SubMeshList::iterator i = mSubMeshList.begin(); i != mSubMeshList.end(); ++i )
			//{
			//    ( *i )->mLodFaceList.resize( numLevels - 1 );
			//}
			IsLodManual = isManual;

		}
		#endregion Mesh Level of Detail

		#region Static Methods

		/// <summary>
		///		Performs a software indexed vertex blend, of the kind used for
		///		skeletal animation although it can be used for other purposes.
		/// </summary>
		/// <remarks>
		///		This function is supplied to update vertex data with blends
		///		done in software, either because no hardware support is available,
		///		or that you need the results of the blend for some other CPU operations.
		/// </remarks>
		/// <param name="sourceVertexData">
		///		<see cref="VertexData"/> class containing positions, normals, blend indices and blend weights.
		///	</param>
		/// <param name="targetVertexData">
		///		<see cref="VertexData"/> class containing target position
		///		and normal buffers which will be updated with the blended versions.
		///		Note that the layout of the source and target position / normal
		///		buffers must be identical, ie they must use the same buffer indexes.
		/// </param>
		/// <param name="matrices">An array of matrices to be used to blend.</param>
		/// <param name="blendNormals">If true, normals are blended as well as positions.</param>
		public static void SoftwareVertexBlend( VertexData sourceVertexData, VertexData targetVertexData, Matrix4[] matrices, bool blendNormals, bool blendTangents, bool blendBinorms )
		{
			// Source vectors
			Vector3 sourcePos = Vector3.Zero;
			Vector3 sourceNorm = Vector3.Zero;
			Vector3 sourceTan = Vector3.Zero;
			Vector3 sourceBinorm = Vector3.Zero;
			// Accumulation vectors
			Vector3 accumVecPos = Vector3.Zero;
			Vector3 accumVecNorm = Vector3.Zero;
			Vector3 accumVecTan = Vector3.Zero;
			Vector3 accumVecBinorm = Vector3.Zero;

			HardwareVertexBuffer srcPosBuf = null, srcNormBuf = null, srcTanBuf = null, srcBinormBuf = null;
			HardwareVertexBuffer destPosBuf = null, destNormBuf = null, destTanBuf = null, destBinormBuf = null;
			HardwareVertexBuffer srcIdxBuf = null, srcWeightBuf = null;

			bool weightsIndexesShareBuffer = false;

			IntPtr ptr = IntPtr.Zero;

			// Get elements for source
			VertexElement srcElemPos =
				sourceVertexData.vertexDeclaration.FindElementBySemantic( VertexElementSemantic.Position );
			VertexElement srcElemNorm =
				sourceVertexData.vertexDeclaration.FindElementBySemantic( VertexElementSemantic.Normal );
			VertexElement srcElemTan =
				sourceVertexData.vertexDeclaration.FindElementBySemantic( VertexElementSemantic.Tangent );
			VertexElement srcElemBinorm =
				sourceVertexData.vertexDeclaration.FindElementBySemantic( VertexElementSemantic.Binormal );
			VertexElement srcElemBlendIndices =
				sourceVertexData.vertexDeclaration.FindElementBySemantic( VertexElementSemantic.BlendIndices );
			VertexElement srcElemBlendWeights =
				sourceVertexData.vertexDeclaration.FindElementBySemantic( VertexElementSemantic.BlendWeights );

			Debug.Assert( srcElemPos != null && srcElemBlendIndices != null && srcElemBlendWeights != null, "You must supply at least positions, blend indices and blend weights" );

			// Get elements for target
			VertexElement destElemPos =
				targetVertexData.vertexDeclaration.FindElementBySemantic( VertexElementSemantic.Position );
			VertexElement destElemNorm =
				targetVertexData.vertexDeclaration.FindElementBySemantic( VertexElementSemantic.Normal );
			VertexElement destElemTan =
				targetVertexData.vertexDeclaration.FindElementBySemantic( VertexElementSemantic.Tangent );
			VertexElement destElemBinorm =
				targetVertexData.vertexDeclaration.FindElementBySemantic( VertexElementSemantic.Binormal );

			// Do we have normals and want to blend them?
			bool includeNormals = blendNormals && ( srcElemNorm != null ) && ( destElemNorm != null );
			bool includeTangents = blendTangents && ( srcElemTan != null ) && ( destElemTan != null );
			bool includeBinormals = blendBinorms && ( srcElemBinorm != null ) && ( destElemBinorm != null );

			// Get buffers for source
			srcPosBuf = sourceVertexData.vertexBufferBinding.GetBuffer( srcElemPos.Source );
			srcIdxBuf = sourceVertexData.vertexBufferBinding.GetBuffer( srcElemBlendIndices.Source );
			srcWeightBuf = sourceVertexData.vertexBufferBinding.GetBuffer( srcElemBlendWeights.Source );
			if ( includeNormals )
				srcNormBuf = sourceVertexData.vertexBufferBinding.GetBuffer( srcElemNorm.Source );
			if ( includeTangents )
				srcTanBuf = sourceVertexData.vertexBufferBinding.GetBuffer( srcElemTan.Source );
			if ( includeBinormals )
				srcBinormBuf = sourceVertexData.vertexBufferBinding.GetBuffer( srcElemBinorm.Source );

			// note: reference comparison
			weightsIndexesShareBuffer = ( srcIdxBuf == srcWeightBuf );

			// Get buffers for target
			destPosBuf = targetVertexData.vertexBufferBinding.GetBuffer( destElemPos.Source );
			if ( includeNormals )
				destNormBuf = targetVertexData.vertexBufferBinding.GetBuffer( destElemNorm.Source );
			if ( includeTangents )
				destTanBuf = targetVertexData.vertexBufferBinding.GetBuffer( destElemTan.Source );
			if ( includeBinormals )
				destBinormBuf = targetVertexData.vertexBufferBinding.GetBuffer( destElemBinorm.Source );

			// Lock source buffers for reading
			Debug.Assert( srcElemPos.Offset == 0, "Positions must be first element in dedicated buffer!" );

			unsafe
			{
				float* pSrcPos = null, pSrcNorm = null, pSrcTan = null, pSrcBinorm = null;
				float* pDestPos = null, pDestNorm = null, pDestTan = null, pDestBinorm = null;
				float* pBlendWeight = null;
				byte* pBlendIdx = null;

				ptr = srcPosBuf.Lock( BufferLocking.ReadOnly );
				pSrcPos = (float*)ptr.ToPointer();

				if ( includeNormals )
				{
					if ( srcNormBuf == srcPosBuf )
						pSrcNorm = pSrcPos;
					else
					{
						ptr = srcNormBuf.Lock( BufferLocking.ReadOnly );
						pSrcNorm = (float*)ptr.ToPointer();
					}
				}
				if ( includeTangents )
				{
					if ( srcTanBuf == srcPosBuf )
						pSrcTan = pSrcPos;
					else if ( srcTanBuf == srcNormBuf )
						pSrcTan = pSrcNorm;
					else
					{
						ptr = srcTanBuf.Lock( BufferLocking.ReadOnly );
						pSrcTan = (float*)ptr.ToPointer();
					}
				}
				if ( includeBinormals )
				{
					if ( srcBinormBuf == srcPosBuf )
						pSrcBinorm = pSrcPos;
					else if ( srcBinormBuf == srcNormBuf )
						pSrcBinorm = pSrcNorm;
					else if ( srcBinormBuf == srcTanBuf )
						pSrcBinorm = pSrcTan;
					else
					{
						ptr = srcBinormBuf.Lock( BufferLocking.ReadOnly );
						pSrcBinorm = (float*)ptr.ToPointer();
					}
				}

				// Indices must be 4 bytes
				Debug.Assert( srcElemBlendIndices.Type == VertexElementType.UByte4,
					"Blend indices must be VET_UBYTE4" );

				ptr = srcIdxBuf.Lock( BufferLocking.ReadOnly );
				pBlendIdx = (byte*)ptr.ToPointer();

				if ( srcWeightBuf == srcIdxBuf )
					pBlendWeight = (float*)pBlendIdx;
				else
				{
					// Lock buffer
					ptr = srcWeightBuf.Lock( BufferLocking.ReadOnly );
					pBlendWeight = (float*)ptr.ToPointer();
				}

				int numWeightsPerVertex = VertexElement.GetTypeCount( srcElemBlendWeights.Type );

				// Lock destination buffers for writing
				ptr = destPosBuf.Lock( BufferLocking.Discard );
				pDestPos = (float*)ptr.ToPointer();

				if ( includeNormals )
				{
					if ( destNormBuf == destPosBuf )
						pDestNorm = pDestPos;
					else
					{
						ptr = destNormBuf.Lock( BufferLocking.Discard );
						pDestNorm = (float*)ptr.ToPointer();
					}
				}
				if ( includeTangents )
				{
					if ( destTanBuf == destPosBuf )
						pDestTan = pDestPos;
					else if ( destTanBuf == destNormBuf )
						pDestTan = pDestNorm;
					else
					{
						ptr = destTanBuf.Lock( BufferLocking.Discard );
						pDestTan = (float*)ptr.ToPointer();
					}
				}
				if ( includeBinormals )
				{
					if ( destBinormBuf == destPosBuf )
						pDestBinorm = pDestPos;
					else if ( destBinormBuf == destNormBuf )
						pDestBinorm = pDestNorm;
					else if ( destBinormBuf == destTanBuf )
						pDestBinorm = pDestTan;
					else
					{
						ptr = destBinormBuf.Lock( BufferLocking.Discard );
						pDestBinorm = (float*)ptr.ToPointer();
					}
				}

				// Loop per vertex
				for ( int vertIdx = 0; vertIdx < targetVertexData.vertexCount; vertIdx++ )
				{
					int srcPosOffset = ( vertIdx * srcPosBuf.VertexSize + srcElemPos.Offset ) / 4;
					// Load source vertex elements
					sourcePos.x = pSrcPos[ srcPosOffset ];
					sourcePos.y = pSrcPos[ srcPosOffset + 1 ];
					sourcePos.z = pSrcPos[ srcPosOffset + 2 ];

					if ( includeNormals )
					{
						int srcNormOffset = ( vertIdx * srcNormBuf.VertexSize + srcElemNorm.Offset ) / 4;
						sourceNorm.x = pSrcNorm[ srcNormOffset ];
						sourceNorm.y = pSrcNorm[ srcNormOffset + 1 ];
						sourceNorm.z = pSrcNorm[ srcNormOffset + 2 ];
					}

					if ( includeTangents )
					{
						int srcTanOffset = ( vertIdx * srcTanBuf.VertexSize + srcElemTan.Offset ) / 4;
						sourceTan.x = pSrcTan[ srcTanOffset ];
						sourceTan.y = pSrcTan[ srcTanOffset + 1 ];
						sourceTan.z = pSrcTan[ srcTanOffset + 2 ];
					}

					if ( includeBinormals )
					{
						int srcBinormOffset = ( vertIdx * srcBinormBuf.VertexSize + srcElemBinorm.Offset ) / 4;
						sourceBinorm.x = pSrcBinorm[ srcBinormOffset ];
						sourceBinorm.y = pSrcBinorm[ srcBinormOffset + 1 ];
						sourceBinorm.z = pSrcBinorm[ srcBinormOffset + 2 ];
					}

					// Load accumulators
					accumVecPos = Vector3.Zero;
					accumVecNorm = Vector3.Zero;
					accumVecTan = Vector3.Zero;
					accumVecBinorm = Vector3.Zero;

					int blendWeightOffset = ( vertIdx * srcWeightBuf.VertexSize + srcElemBlendWeights.Offset ) / 4;
					int blendMatrixOffset = vertIdx * srcIdxBuf.VertexSize + srcElemBlendIndices.Offset;
					// Loop per blend weight
					for ( int blendIdx = 0; blendIdx < numWeightsPerVertex; blendIdx++ )
					{
						float blendWeight = pBlendWeight[ blendWeightOffset + blendIdx ];
						int blendMatrixIdx = pBlendIdx[ blendMatrixOffset + blendIdx ];
						// Blend by multiplying source by blend matrix and scaling by weight
						// Add to accumulator
						// NB weights must be normalised!!
						if ( blendWeight != 0.0f )
						{
							// Blend position, use 3x4 matrix
							Matrix4 mat = matrices[ blendMatrixIdx ];
							BlendPosVector( ref accumVecPos, ref mat, ref sourcePos, blendWeight );

							if ( includeNormals )
							{
								// Blend normal
								// We should blend by inverse transpose here, but because we're assuming the 3x3
								// aspect of the matrix is orthogonal (no non-uniform scaling), the inverse transpose
								// is equal to the main 3x3 matrix
								// Note because it's a normal we just extract the rotational part, saves us renormalising here
								BlendDirVector( ref accumVecNorm, ref mat, ref sourceNorm, blendWeight );
							}
							if ( includeTangents )
							{
								BlendDirVector( ref accumVecTan, ref mat, ref sourceTan, blendWeight );
							}
							if ( includeBinormals )
							{
								BlendDirVector( ref accumVecBinorm, ref mat, ref sourceBinorm, blendWeight );
							}

						}
					}

					// Stored blended vertex in hardware buffer
					int dstPosOffset = ( vertIdx * destPosBuf.VertexSize + destElemPos.Offset ) / 4;
					pDestPos[ dstPosOffset ] = accumVecPos.x;
					pDestPos[ dstPosOffset + 1 ] = accumVecPos.y;
					pDestPos[ dstPosOffset + 2 ] = accumVecPos.z;

					// Stored blended vertex in temp buffer
					if ( includeNormals )
					{
						// Normalise
						accumVecNorm.Normalize();
						int dstNormOffset = ( vertIdx * destNormBuf.VertexSize + destElemNorm.Offset ) / 4;
						pDestNorm[ dstNormOffset ] = accumVecNorm.x;
						pDestNorm[ dstNormOffset + 1 ] = accumVecNorm.y;
						pDestNorm[ dstNormOffset + 2 ] = accumVecNorm.z;
					}
					// Stored blended vertex in temp buffer
					if ( includeTangents )
					{
						// Normalise
						accumVecTan.Normalize();
						int dstTanOffset = ( vertIdx * destTanBuf.VertexSize + destElemTan.Offset ) / 4;
						pDestTan[ dstTanOffset ] = accumVecTan.x;
						pDestTan[ dstTanOffset + 1 ] = accumVecTan.y;
						pDestTan[ dstTanOffset + 2 ] = accumVecTan.z;
					}
					// Stored blended vertex in temp buffer
					if ( includeBinormals )
					{
						// Normalise
						accumVecBinorm.Normalize();
						int dstBinormOffset = ( vertIdx * destBinormBuf.VertexSize + destElemBinorm.Offset ) / 4;
						pDestBinorm[ dstBinormOffset ] = accumVecBinorm.x;
						pDestBinorm[ dstBinormOffset + 1 ] = accumVecBinorm.y;
						pDestBinorm[ dstBinormOffset + 2 ] = accumVecBinorm.z;
					}

				}
				// Unlock source buffers
				srcPosBuf.Unlock();
				srcIdxBuf.Unlock();

				if ( srcWeightBuf != srcIdxBuf )
				{
					srcWeightBuf.Unlock();
				}

				if ( includeNormals &&
					srcNormBuf != srcPosBuf )
				{
					srcNormBuf.Unlock();
				}
				if ( includeTangents &&
					srcTanBuf != srcPosBuf &&
					srcTanBuf != srcNormBuf )
				{
					srcTanBuf.Unlock();
				}
				if ( includeBinormals &&
					srcBinormBuf != srcPosBuf &&
					srcBinormBuf != srcNormBuf &&
					srcBinormBuf != srcTanBuf )
				{
					srcBinormBuf.Unlock();
				}

				// Unlock destination buffers
				destPosBuf.Unlock();

				if ( includeNormals &&
					destNormBuf != destPosBuf )
				{
					destNormBuf.Unlock();
				}
				if ( includeTangents &&
					destTanBuf != destPosBuf &&
					destTanBuf != destNormBuf )
				{
					destTanBuf.Unlock();
				}
				if ( includeBinormals &&
					destBinormBuf != destPosBuf &&
					destBinormBuf != destNormBuf &&
					destBinormBuf != destTanBuf )
				{
					destBinormBuf.Unlock();
				}

			} // unsafe
		}

		public static void BlendDirVector( ref Vector3 accumVec, ref Matrix4 mat, ref Vector3 srcVec, float blendWeight )
		{
			accumVec.x +=
				( mat.m00 * srcVec.x +
				 mat.m01 * srcVec.y +
				 mat.m02 * srcVec.z )
				* blendWeight;

			accumVec.y +=
				( mat.m10 * srcVec.x +
				 mat.m11 * srcVec.y +
				 mat.m12 * srcVec.z )
				* blendWeight;

			accumVec.z +=
				( mat.m20 * srcVec.x +
				 mat.m21 * srcVec.y +
				 mat.m22 * srcVec.z )
				* blendWeight;
		}

		public static void BlendPosVector( ref Vector3 accumVec, ref Matrix4 mat, ref Vector3 srcVec, float blendWeight )
		{
			accumVec.x +=
				( mat.m00 * srcVec.x +
				 mat.m01 * srcVec.y +
				 mat.m02 * srcVec.z +
				 mat.m03 )
				* blendWeight;

			accumVec.y +=
				( mat.m10 * srcVec.x +
				 mat.m11 * srcVec.y +
				 mat.m12 * srcVec.z +
				 mat.m13 )
				* blendWeight;

			accumVec.z +=
				( mat.m20 * srcVec.x +
				 mat.m21 * srcVec.y +
				 mat.m22 * srcVec.z +
				 mat.m23 )
				* blendWeight;
		}

		/// <summary>
		///     Performs a software vertex morph, of the kind used for
		///     morph animation although it can be used for other purposes.
		/// </summary>
		/// <remarks>
		///   	This function will linearly interpolate positions between two
		/// 	source buffers, into a third buffer.
		/// </remarks>
		/// <param name="t">Parametric distance between the start and end buffer positions</param>
		/// <param name="b1">Vertex buffer containing VertexElementType.Float3 entries for the start positions</param>
		/// <param name="b2">Vertex buffer containing VertexElementType.Float3 entries for the end positions</param>
		/// <param name="targetVertexData" VertexData destination; assumed to have a separate position
		///	     buffer already bound, and the number of vertices must agree with the
		///   number in start and end
		/// </param>
		public static void SoftwareVertexMorph( float t, HardwareVertexBuffer b1,
											   HardwareVertexBuffer b2, VertexData targetVertexData )
		{
			unsafe
			{
				float* pb1 = (float*)b1.Lock( BufferLocking.ReadOnly );
				float* pb2 = (float*)b2.Lock( BufferLocking.ReadOnly );
				VertexElement posElem =
					targetVertexData.vertexDeclaration.FindElementBySemantic( VertexElementSemantic.Position );
				Debug.Assert( posElem != null );
				HardwareVertexBuffer destBuf = targetVertexData.vertexBufferBinding.GetBuffer( posElem.Source );
				Debug.Assert( posElem.Size == destBuf.VertexSize,
							 "Positions must be in a buffer on their own for morphing" );
				float* pdst = (float*)destBuf.Lock( BufferLocking.Discard );
				for ( int i = 0; i < targetVertexData.vertexCount; ++i )
				{
					// x
					*pdst++ = *pb1 + t * ( *pb2 - *pb1 );
					++pb1;
					++pb2;
					// y
					*pdst++ = *pb1 + t * ( *pb2 - *pb1 );
					++pb1;
					++pb2;
					// z
					*pdst++ = *pb1 + t * ( *pb2 - *pb1 );
					++pb1;
					++pb2;
				}
				destBuf.Unlock();
				b1.Unlock();
				b2.Unlock();
			}
		}

		/// <summary>
		///     Performs a software vertex pose blend, of the kind used for
		///     morph animation although it can be used for other purposes.
		/// </summary>
		/// <remarks>
		///     This function will apply a weighted offset to the positions in the
		///     incoming vertex data (therefore this is a read/write operation, and
		///     if you expect to call it more than once with the same data, then
		///     you would be best to suppress hardware uploads of the position buffer
		///     for the duration)
		/// </remarks>
		/// <param name="weight"Parametric weight to scale the offsets by</param>
		/// <param name="vertexOffsetMap" Potentially sparse map of vertex index -> offset</param>
		/// <param name="targetVertexData" VertexData destination; assumed to have a separate position
		///	    buffer already bound, and the number of vertices must agree with the
		///	    number in start and end
		/// </param>
		public static void SoftwareVertexPoseBlend( float weight, Dictionary<int, Vector3> vertexOffsetMap,
												   VertexData targetVertexData )
		{
			// Do nothing if no weight
			if ( weight == 0.0f )
				return;

			VertexElement posElem = targetVertexData.vertexDeclaration.FindElementBySemantic( VertexElementSemantic.Position );
			Debug.Assert( posElem != null );
			HardwareVertexBuffer destBuf = targetVertexData.vertexBufferBinding.GetBuffer( posElem.Source );
			Debug.Assert( posElem.Size == destBuf.VertexSize,
						 "Positions must be in a buffer on their own for pose blending" );
			// Have to lock in normal mode since this is incremental
			unsafe
			{
				float* pBase = (float*)destBuf.Lock( BufferLocking.Normal );
				// Iterate over affected vertices
				foreach ( KeyValuePair<int, Vector3> pair in vertexOffsetMap )
				{
					// Adjust pointer
					float* pdst = pBase + pair.Key * 3;
					*pdst = *pdst + ( pair.Value.x * weight );
					++pdst;
					*pdst = *pdst + ( pair.Value.y * weight );
					++pdst;
					*pdst = *pdst + ( pair.Value.z * weight );
					++pdst;
				}
				destBuf.Unlock();
			}
		}

		#endregion Static Methods

		#region Implementation of Resource

		//public override void Preload()
		//{
		//    //if (isPreloaded) {
		//    //    return;
		//    //}

		//    // load this bad boy if it is not to be manually defined
		//    if ( !isManuallyDefined )
		//    {
		//        MeshSerializer serializer = new MeshSerializer();

		//        // get the resource data from MeshManager
		//        Stream data = MeshManager.Instance.FindResourceData( name );

		//        string extension = Path.GetExtension( name );

		//        if ( extension != ".mesh" )
		//        {
		//            data.Close();

		//            throw new AxiomException( "Unsupported mesh format '{0}'", extension );
		//        }

		//        // fetch the .mesh dependency info
		//        serializer.GetDependencyInfo( data, this );

		//        // close the stream (we don't need to leave it open here)
		//        data.Close();
		//    }

		//}

		/// <summary>
		///		Loads the mesh data.
		/// </summary>
		protected override void load()
		{
			// unload this first if it is already loaded
			if ( IsLoaded )
			{
				Unload();
			}

			// I should eventually call Preload here, and then use
			// the preloaded data to make future loads faster, but
			// I haven't finished the Preload stuff yet.
			// Preload();

			// load this bad boy if it is not to be manually defined
			if ( !IsManuallyLoaded )
			{
				MeshSerializer serializer = new MeshSerializer();

				// get the resource data from MeshManager
				Stream data = ResourceGroupManager.Instance.OpenResource( Name, Group, true, this );

				string extension = Path.GetExtension( Name );

				if ( extension != ".mesh" )
				{
					data.Close();

					throw new AxiomException( "Unsupported mesh format '{0}'", extension );
				}

				// import the .mesh file
				serializer.ImportMesh( data, this );

				// close the stream (we don't need to leave it open here)
				data.Close();
			}

			// prepare the mesh for a shadow volume?
			if ( MeshManager.Instance.PrepareAllMeshesForShadowVolumes )
			{
				if ( _edgeListsBuilt || _autoBuildEdgeLists )
				{
					PrepareForShadowVolume();
				}
				if ( !_edgeListsBuilt && _autoBuildEdgeLists )
				{
					BuildEdgeList();
				}
			}

			// The loading process accesses lod usages directly, so
			// transformation of user values must occur after loading is complete.

			// Transform user lod values
			foreach ( MeshLodUsage mlu in meshLodUsageList )
				mlu.Value = _lodStrategy.TransformUserValue( mlu.UserValue );

			// meshLoadMeter.Exit();
		}

		/// <summary>
		///		Unloads the mesh data.
		/// </summary>
		protected override void unload()
		{
            // Dispose managed resources.
            if (_skeleton != null)
            {
                if (!this.Skeleton.IsDisposed)
                    this._skeleton.Dispose();

                this._skeleton = null;
            }

            foreach (SubMesh subMesh in _subMeshList)
            {
                if (!subMesh.IsDisposed)
                    subMesh.Dispose();
            }
            _subMeshList.Clear();

            if (this._sharedVertexData != null)
            {
                if (!this._sharedVertexData.IsDisposed)
                    this._sharedVertexData.Dispose();

                this._sharedVertexData = null;
            }

            _isPreparedForShadowVolumes = false;
			
            //// TODO: SubMeshNameCount
            //// TODO: Remove LOD levels
		}

		#endregion Implementation of Resource

		#region IDisposable Implementation

		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				if ( disposeManagedResources )
				{
                    if (this.IsLoaded)
                        this.unload();
				}

				// There are no unmanaged resources to release, but
				// if we add them, they need to be released here.
			}

            base.dispose(disposeManagedResources);
		}


		#endregion IDisposable Implementation
	}

	/// <summary>
	/// A way of recording the way each LOD is recorded this Mesh.
	/// </summary>
	public class MeshLodUsage
	{
		/// <summary>
		/// User-supplied values used to determine when th is lod applies.
		/// </summary>
		/// <remarks>
		/// This is required in case the lod strategy changes.
		/// </remarks>
		public Real UserValue;
		/// <summary>
		/// Value used by to determine when this lod applies.
		/// </summary>
		/// <remarks>
		/// May be interpretted differently by different strategies.
		/// Transformed from user-supplied values with <see cref="LodStrategy.TransformUserValue"/>.
		/// </remarks>
		public Real Value;
		/// <summary>
		///	Only relevant if isLodManual is true, the name of the alternative mesh to use.
		/// </summary>
		public string ManualName;
		///	<summary>
		///		Reference to the manual mesh to avoid looking up each time.
		///	</summary>
		public Mesh ManualMesh;
		/// <summary>
		///		Edge list for this LOD level (may be derived from manual mesh).
		/// </summary>
		public EdgeData EdgeData;
	}
}