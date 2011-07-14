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
using System.IO;
using System.Collections;
using System.Runtime.InteropServices;

using Axiom;
using Axiom.Core;
using Axiom.Math;
using Axiom.Graphics;
using Axiom.Collections;
using Axiom.SceneManagers.Bsp.Collections;
using Axiom.Scripting;

using ResourceHandle = System.UInt64;
using System.Collections.Generic;
#endregion Namespace Declarations

namespace Axiom.SceneManagers.Bsp
{
	/// <summary>
	///    Holds all the data associated with a Binary Space Parition
	///    (BSP) based indoor level.
	/// </summary>
	/// <remarks>
	///    The data used here is populated by loading level files via
	///    the BspLevelManager.Load method, although application users
	///    are more likely to call SceneManager.SetWorldGeometry which will
	///    automatically arrange the loading of the level. Note that this assumes
	///    that you have asked for an indoor-specialized SceneManager (specify
	///    SceneType.Indoor when calling Engine.GetSceneManager).</p>
	///    We currently only support loading from Quake3 Arena level files,
	///    although any source that can be converted into this classes structure
	///    could also be used. The Quake3 level load process is in a different
	///    class called Quake3Level to keep the specifics separate.</p>
	/// </remarks>
	public class BspLevel : Resource
	{
		private const int NUM_FACES_PER_PROGRESS_REPORT = 100;
		private const int NUM_NODES_PER_PROGRESS_REPORT = 50;
		private const int NUM_LEAVES_PER_PROGRESS_REPORT = 50;
		private const int NUM_BRUSHES_PER_PROGRESS_REPORT = 50;

		public const int LightmapSize = ( 128 * 128 * 3 );

		#region Protected members

		protected NameValuePairList createParam;
		protected BspNode[] nodes;
		protected int numLeaves;
		protected int leafStart;

		/// <summary>
		///		Vertex data holding all the data for the level, but able to render parts of it/
		/// </summary>
		protected VertexData vertexData;

		/// <summary>
		///		Array of indexes into the faceGroups array. This buffer is organised
		///		by leaf node so leaves can just use contiguous chunks of it and
		///		get repointed to the actual entries in faceGroups.
		/// </summary>
		protected int[] leafFaceGroups;

		/// <summary>
		///		Array of face groups, indexed into by contents of mLeafFaceGroups.
		///	</summary>
		protected BspStaticFaceGroup[] faceGroups;

		/// <summary>
		///		Storage of patches
		///	</summary>
		protected AxiomSortedCollection<long, PatchSurface> patches = new AxiomSortedCollection<long, PatchSurface>();

		/// <summary>
		///		Total number of vertices required for all patches.
		/// </summary>
		protected int patchVertexCount;
		/// <summary>
		///		Total number of indexes required for all patches.
		/// </summary>
		protected int patchIndexCount;

		/// <summary>
		///		Indexes for the whole level, will be copied to the real indexdata per frame.
		/// </summary>
		protected int numIndexes;
		protected HardwareIndexBuffer indexes;
		protected BspBrush[] brushes;
		protected List<ViewPoint> playerStarts = new List<ViewPoint>();
		protected VisData visData;
		internal protected MultiMap<MovableObject, BspNode> objectToNodeMap;
		protected BspOptions bspOptions = new BspOptions();

		#endregion Protected members

		#region Public properties
		public BspNode[] Nodes
		{
			get
			{
				return nodes;
			}
		}

		public BspNode RootNode
		{
			get
			{
				return nodes[ 0 ];
			}
		}

		public HardwareIndexBuffer Indexes
		{
			get
			{
				return indexes;
			}
		}

		public int NumLeaves
		{
			get
			{
				return numLeaves;
			}
		}

		public int NumIndexes
		{
			get
			{
				return numIndexes;
			}
		}

		public int NumNodes
		{
			get
			{
				return nodes.Length;
			}
		}

		public int LeafStart
		{
			get
			{
				return leafStart;
			}
		}

		public int[] LeafFaceGroups
		{
			get
			{
				return leafFaceGroups;
			}
		}

		public BspStaticFaceGroup[] FaceGroups
		{
			get
			{
				return faceGroups;
			}
		}

		public VertexData VertexData
		{
			get
			{
				return vertexData;
			}
		}

		public ViewPoint[] PlayerStarts
		{
			get
			{
				return playerStarts.ToArray();
			}
		}

		public BspOptions BspOptions
		{
			get
			{
				return bspOptions;
			}
		}
		#endregion Public properties

		#region Constructor
		/// <summary>
		///		Default constructor - used by BspResourceManager (do not call directly).
		/// </summary>
		/// <param name="name"></param>
		public BspLevel( ResourceManager parent, string name, ResourceHandle handle, string group, bool isManual, IManualResourceLoader loader, NameValuePairList createParams )
			: base( parent, name, handle, group, isManual, loader )
		{
			objectToNodeMap = new MultiMap<MovableObject, BspNode>();
			this.createParam = createParams;
		}
		#endregion Constructor

		#region Public methods

		public void Load( Stream stream )
		{
			if ( createParam.ContainsKey( "SetYAxisUp" ) )
				bool.TryParse( createParam[ "SetYAxisUp" ], out bspOptions.setYAxisUp );

			if ( createParam.ContainsKey( "Scale" ) )
				float.TryParse( createParam[ "Scale" ], out bspOptions.scale );

			Vector3 move = Vector3.Zero;
			if ( createParam.ContainsKey( "MoveX" ) )
				Real.TryParse( createParam[ "MoveX" ], out move.x );

			if ( createParam.ContainsKey( "MoveY" ) )
				Real.TryParse( createParam[ "MoveY" ], out move.y );

			if ( createParam.ContainsKey( "MoveZ" ) )
				Real.TryParse( createParam[ "MoveZ" ], out move.z );

			if ( createParam.ContainsKey( "UseLightmaps" ) )
				bool.TryParse( createParam[ "UseLightmaps" ], out bspOptions.useLightmaps );

			if ( createParam.ContainsKey( "AmbientEnabled" ) )
				bool.TryParse( createParam[ "AmbientEnabled" ], out bspOptions.ambientEnabled );

			if ( createParam.ContainsKey( "AmbientRatio" ) )
				float.TryParse( createParam[ "AmbientRatio" ], out bspOptions.ambientRatio );

			Quake3Level q3 = new Quake3Level( bspOptions );

			q3.LoadFromStream( stream );

			LoadQuake3Level( q3 );

		}

		/// <summary>
		///		Determines if one leaf node is visible from another.
		/// </summary>
		/// <param name="?"></param>
		/// <returns></returns>
		public bool IsLeafVisible( BspNode from, BspNode to )
		{
			if ( to.VisCluster == -1 )
				return false;
			if ( from.VisCluster == -1 )
				// Camera outside world?
				return true;

			if ( !from.IsLeaf || !to.IsLeaf )
				throw new AxiomException( "Both nodes must be leaf nodes for visibility testing." );

			// Use PVS to determine visibility

			/*
			// In wordier terms, the fairly cryptic (but fast) version is doing this:
			//   Could make it a macro for even more speed?

			// Row offset = from cluster number * row size
			int offset = from->mVisCluster*mVisData.rowLength;

			// Column offset (in bytes) = to cluster number divided by 8 (since 8 bits per bytes)
			offset += to->mVisCluster >> 3;

			// Get the right bit within the byte, i.e. bitwise 'and' with bit at remainder position
			int result = *(mVisData.tableData + offset) & (1 << (to->mVisCluster & 7));

			return (result != 0);
			*/

			byte visSet = visData.tableData[ ( from.VisCluster * visData.rowLength ) + ( to.VisCluster >> 3 ) ];
			int result = visSet & ( 1 << ( ( to.VisCluster ) & 7 ) );

			return ( result != 0 );
		}


		/// <summary>
		///		Walks the entire BSP tree and returns the leaf which contains the given point.
		/// </summary>r
		public BspNode FindLeaf( Vector3 point )
		{
			BspNode node = nodes[ 0 ];

			while ( !node.IsLeaf )
				node = node.GetNextNode( point );

			return node;
		}

		/// <summary>
		///		Ensures that the <see cref="Axiom.Core.SceneObject"/> is attached to the right leaves of the BSP tree.
		/// </summary>
		internal void NotifyObjectMoved( MovableObject obj, Vector3 pos )
		{
			IEnumerator objnodes = objectToNodeMap.Find( obj );

			if ( objnodes != null )
			{
				while ( objnodes.MoveNext() )
					( (BspNode)objnodes.Current ).RemoveObject( obj );

				objectToNodeMap.Clear( obj );
			}

			TagNodesWithObject( this.RootNode, obj, pos );
		}

		/// <summary>
		///		Internal method, makes sure an object is removed from the leaves when detached from a node.
		/// </summary>
		internal void NotifyObjectDetached( MovableObject obj )
		{
			IEnumerator objnodes = objectToNodeMap.Find( obj );

			if ( objnodes == null )
				return;

			while ( objnodes.MoveNext() )
				( (BspNode)objnodes.Current ).RemoveObject( obj );

			objectToNodeMap.Clear( obj );
		}
		#endregion Public methods

		#region Protected methods
		/// <summary>
		///		/** Internal utility function for loading data from Quake3.
		/// </summary>
		protected void LoadQuake3Level( Quake3Level q3lvl )
		{
			ResourceGroupManager rgm = ResourceGroupManager.Instance;
			rgm.notifyWorldGeometryStageStarted( "Parsing entities" );
			LoadEntities( q3lvl );
			rgm.notifyWorldGeometryStageEnded();

			rgm.notifyWorldGeometryStageStarted( "Extracting lightmaps" );
			q3lvl.ExtractLightmaps();
			rgm.notifyWorldGeometryStageEnded();

			//-----------------------------------------------------------------------
			// Vertices
			//-----------------------------------------------------------------------
			// Allocate memory for vertices & copy
			vertexData = new VertexData();

			// Create vertex declaration
			VertexDeclaration decl = vertexData.vertexDeclaration;
			int offset = 0;
			int lightTexOffset = 0;
			decl.AddElement( 0, offset, VertexElementType.Float3, VertexElementSemantic.Position );
			offset += VertexElement.GetTypeSize( VertexElementType.Float3 );
			decl.AddElement( 0, offset, VertexElementType.Float3, VertexElementSemantic.Normal );
			offset += VertexElement.GetTypeSize( VertexElementType.Float3 );
			decl.AddElement( 0, offset, VertexElementType.Float2, VertexElementSemantic.TexCoords, 0 );
			offset += VertexElement.GetTypeSize( VertexElementType.Float2 );
			decl.AddElement( 0, offset, VertexElementType.Float2, VertexElementSemantic.TexCoords, 1 );

			// Build initial patches - we need to know how big the vertex buffer needs to be
			// to accommodate the subdivision
			// we don't want to include the elements for texture lighting, so we clone it
			rgm.notifyWorldGeometryStageStarted( "Initializing patches" );
			InitQuake3Patches( q3lvl, (VertexDeclaration)decl.Clone() );
			rgm.notifyWorldGeometryStageEnded();

			// this is for texture lighting color and alpha
			decl.AddElement( 1, lightTexOffset, VertexElementType.Color, VertexElementSemantic.Diffuse );
			lightTexOffset += VertexElement.GetTypeSize( VertexElementType.Color );
			// this is for texture lighting coords
			decl.AddElement( 1, lightTexOffset, VertexElementType.Float2, VertexElementSemantic.TexCoords, 2 );

			rgm.notifyWorldGeometryStageStarted( "Setting up vertex data" );
			// Create the vertex buffer, allow space for patches
			HardwareVertexBuffer vbuf = HardwareBufferManager.Instance.CreateVertexBuffer( decl.Clone(0), q3lvl.NumVertices + patchVertexCount,	BufferUsage.StaticWriteOnly /* the vertices will be read often for texture lighting, use shadow buffer */, true );

			// Create the vertex buffer for texture lighting, allow space for patches
			HardwareVertexBuffer texLightBuf = HardwareBufferManager.Instance.CreateVertexBuffer( decl.Clone(1), q3lvl.NumVertices + patchVertexCount, BufferUsage.DynamicWriteOnly, false );

			// COPY static vertex data - Note that we can't just block-copy the vertex data because we have to reorder
			// our vertex elements; this is to ensure compatibility with older cards when using
			// hardware vertex buffers - Direct3D requires that the buffer format maps onto a
			// FVF in those older drivers.
			// Lock just the non-patch area for now.

			unsafe
			{
				BspVertex vert = new BspVertex();
				TextureLightMap texLightMap = new TextureLightMap();

				// Keep another base pointer for use later in patch building
				for ( int v = 0; v < q3lvl.NumVertices; v++ )
				{
					QuakeVertexToBspVertex( q3lvl.Vertices[ v ], out vert, out texLightMap );

					BspVertex* bvptr = &vert;
					TextureLightMap* tlptr = &texLightMap;

					vbuf.WriteData(
						v * sizeof( BspVertex ),
						sizeof( BspVertex ),
						(IntPtr)bvptr
						);

					texLightBuf.WriteData(
						v * sizeof( TextureLightMap ),
						sizeof( TextureLightMap ),
						(IntPtr)tlptr
						);

				}
			}

			// Setup binding
			vertexData.vertexBufferBinding.SetBinding( 0, vbuf );

			// Setup texture lighting binding
			vertexData.vertexBufferBinding.SetBinding( 1, texLightBuf );

			// Set other data
			vertexData.vertexStart = 0;
			vertexData.vertexCount = q3lvl.NumVertices + patchVertexCount;
			rgm.notifyWorldGeometryStageEnded();

			//-----------------------------------------------------------------------
			// Faces
			//-----------------------------------------------------------------------
			rgm.notifyWorldGeometryStageStarted( "Setting up face data" );
			leafFaceGroups = new int[ q3lvl.LeafFaces.Length ];
			Array.Copy( q3lvl.LeafFaces, 0, leafFaceGroups, 0, leafFaceGroups.Length );

			faceGroups = new BspStaticFaceGroup[ q3lvl.Faces.Length ];

			// Set up index buffer
			// NB Quake3 indexes are 32-bit
			// Copy the indexes into a software area for staging
			numIndexes = q3lvl.NumElements + patchIndexCount;

			// Create an index buffer manually in system memory, allow space for patches
			indexes = HardwareBufferManager.Instance.CreateIndexBuffer(
				IndexType.Size32,
				numIndexes,
				BufferUsage.Dynamic
				);

			// Write main indexes
			indexes.WriteData( 0, Marshal.SizeOf( typeof( uint ) ) * q3lvl.NumElements, q3lvl.Elements, true );
			rgm.notifyWorldGeometryStageEnded();

			// now build patch information
			rgm.notifyWorldGeometryStageStarted( "Building patches" );
			BuildQuake3Patches( q3lvl.NumVertices, q3lvl.NumElements );
			rgm.notifyWorldGeometryStageEnded();

			//-----------------------------------------------------------------------
			// Create materials for shaders
			//-----------------------------------------------------------------------
			// NB this only works for the 'default' shaders for now
			// i.e. those that don't have a .shader script and thus default
			// to just texture + lightmap
			// TODO: pre-parse all .shader files and create lookup for next stage (use ROGL shader_file_t)

			// Material names are shadername#lightmapnumber
			// This is because I like to define materials up front completely
			// rather than combine lightmap and shader dynamically (it's
			// more generic). It results in more materials, but they're small
			// beer anyway. Texture duplication is prevented by infrastructure.
			// To do this I actually need to parse the faces since they have the
			// shader/lightmap combo (lightmap number is not in the shader since
			// it can be used with multiple lightmaps)
			string shaderName;
			int face = q3lvl.Faces.Length;
			int progressCountdown = 100;
			int progressCount = 0;

			while ( face-- > 0 )
			{
				// Progress reporting
				if ( progressCountdown == 100 )
				{
					++progressCount;
					String str = String.Format( "Loading materials (phase {0})", progressCount );
					rgm.notifyWorldGeometryStageStarted( str );
				}
				else if ( progressCountdown == 0 )
				{
					// stage report
					rgm.notifyWorldGeometryStageEnded();
					progressCountdown = 100 + 1;
				}

				// Check to see if existing material
				// Format shader#lightmap
				int shadIdx = q3lvl.Faces[ face ].shader;

				shaderName = String.Format( "{0}#{1}", q3lvl.Shaders[ shadIdx ].name, q3lvl.Faces[ face ].lmTexture );
				Material shadMat = (Material)MaterialManager.Instance.GetByName( shaderName );

				if ( shadMat == null && !bspOptions.useLightmaps )
				{
					// try the no-lightmap material
					shaderName = String.Format( "{0}#n", q3lvl.Shaders[ shadIdx ].name );
					shadMat = (Material)MaterialManager.Instance.GetByName( shaderName );
				}

				if ( shadMat == null )
				{
					// Color layer
					// NB no extension in Q3A(doh), have to try shader, .jpg, .tga
					string tryName = q3lvl.Shaders[ shadIdx ].name;

					// Try shader first
					Quake3Shader shader = (Quake3Shader)Quake3ShaderManager.Instance.GetByName( tryName );

					if ( shader != null )
					{
						shadMat = shader.CreateAsMaterial( q3lvl.Faces[ face ].lmTexture );
					}
					else
					{
						// No shader script, try default type texture
						shadMat = (Material)MaterialManager.Instance.Create( shaderName, rgm.WorldResourceGroupName );
						Pass shadPass = shadMat.GetTechnique( 0 ).GetPass( 0 );

						// Try jpg
						TextureUnitState tex = null;
						if ( ResourceGroupManager.Instance.ResourceExists( rgm.WorldResourceGroupName, tryName + ".jpg" ) )
						{
							tex = shadPass.CreateTextureUnitState( tryName + ".jpg" );
						}
						if ( ResourceGroupManager.Instance.ResourceExists( rgm.WorldResourceGroupName, tryName + ".tga" ) )
						{
							tex = shadPass.CreateTextureUnitState( tryName + ".tga" );
						}

						if ( tex != null )
						{
							// Set replace on all first layer textures for now
							tex.SetColorOperation( LayerBlendOperation.Replace );
                            tex.SetTextureAddressingMode( TextureAddressing.Wrap );
							// for ambient lighting
							tex.ColorBlendMode.source2 = LayerBlendSource.Manual;
						}

						if ( bspOptions.useLightmaps && q3lvl.Faces[ face ].lmTexture != -1 )
						{
							// Add lightmap, additive blending
							tex = shadPass.CreateTextureUnitState( String.Format( "@lightmap{0}", q3lvl.Faces[ face ].lmTexture ) );

							// Blend
							tex.SetColorOperation( LayerBlendOperation.Modulate );

							// Use 2nd texture co-ordinate set
							tex.TextureCoordSet = 1;

							// Clamp
                            tex.SetTextureAddressingMode( TextureAddressing.Clamp );
						}

						shadMat.CullingMode = CullingMode.None;
						shadMat.Lighting = false;
					}
				}

				shadMat.Load();

				// Copy face data
				BspStaticFaceGroup dest = new BspStaticFaceGroup();
				InternalBspFace src = q3lvl.Faces[ face ];

				if ( ( q3lvl.Shaders[ src.shader ].surfaceFlags & SurfaceFlags.Sky ) == SurfaceFlags.Sky )
					dest.isSky = true;
				else
					dest.isSky = false;

				dest.materialHandle = shadMat.Handle;
				dest.elementStart = src.elemStart;
				dest.numElements = src.elemCount;
				dest.numVertices = src.vertCount;
				dest.vertexStart = src.vertStart;
				dest.plane = new Plane();

				if ( Quake3ShaderManager.Instance.GetByName( q3lvl.Shaders[ shadIdx ].name ) != null )
				{
					// it's a quake shader
					dest.isQuakeShader = true;
				}

				if ( src.type == BspFaceType.Normal )
				{
					dest.type = FaceGroup.FaceList;

					// Assign plane
					dest.plane.Normal = new Vector3(
						src.normal[ 0 ],
						src.normal[ 1 ],
						src.normal[ 2 ]
						);
					dest.plane.D = -dest.plane.Normal.Dot(
						new Vector3(
						src.org[ 0 ],
						src.org[ 1 ],
						src.org[ 2 ]
						)
						);

					// Don't rebase indexes here - Quake3 re-uses some indexes for multiple vertex
					// groups eg repeating small details have the same relative vertex data but
					// use the same index data.
				}
				else if ( src.type == BspFaceType.Patch )
				{
					// Seems to be some crap in the Q3 level where vertex count = 0 or num control points = 0?
					if ( ( dest.numVertices == 0 ) || ( src.meshCtrl[ 0 ] == 0 ) )
					{
						dest.type = FaceGroup.Unknown;
					}
					else
					{
						// Set up patch surface
						dest.type = FaceGroup.Patch;

						// Locate the patch we already built
						if ( !patches.ContainsKey( face ) )
							throw new AxiomException( "Patch not found from previous built state." );

						dest.patchSurf = (PatchSurface)patches[ face ];
					}
				}
				else if ( src.type == BspFaceType.Mesh )
				{
					dest.type = FaceGroup.FaceList;

					// Assign plane
					dest.plane.Normal = new Vector3( src.normal[ 0 ], src.normal[ 1 ], src.normal[ 2 ] );
					dest.plane.D = -dest.plane.Normal.Dot( new Vector3( src.org[ 0 ], src.org[ 1 ], src.org[ 2 ] ) );
				}
				else
				{
					LogManager.Instance.Write( "!!! Unknown face type !!!" );
				}

				faceGroups[ face ] = dest;
			}

			//-----------------------------------------------------------------------
			// Nodes
			//-----------------------------------------------------------------------
			// Allocate memory for all nodes (leaves and splitters)
			nodes = new BspNode[ q3lvl.NumNodes + q3lvl.NumLeaves ];
			numLeaves = q3lvl.NumLeaves;
			leafStart = q3lvl.NumNodes;

			// Run through and initialize the array so front/back node pointers
			// aren't null.
			for ( int i = 0; i < nodes.Length; i++ )
				nodes[ i ] = new BspNode();

			// Convert nodes
			// In our array, first q3lvl.NumNodes are non-leaf, others are leaves
			for ( int i = 0; i < q3lvl.NumNodes; i++ )
			{
				BspNode node = nodes[ i ];
				InternalBspNode q3node = q3lvl.Nodes[ i ];

				node.IsLeaf = false;
				node.Owner = this;

				Plane splitPlane = new Plane();

				// Set plane
				splitPlane.Normal = new Vector3(
					q3lvl.Planes[ q3node.plane ].normal[ 0 ],
					q3lvl.Planes[ q3node.plane ].normal[ 1 ],
					q3lvl.Planes[ q3node.plane ].normal[ 2 ]
					);
				splitPlane.D = -q3lvl.Planes[ q3node.plane ].distance;

				node.SplittingPlane = splitPlane;

				// Set bounding box
				node.BoundingBox = new AxisAlignedBox(
					new Vector3(
						q3node.bbox[ 0 ],
						q3node.bbox[ 1 ],
						q3node.bbox[ 2 ]
					),
					new Vector3(
						q3node.bbox[ 3 ],
						q3node.bbox[ 4 ],
						q3node.bbox[ 5 ]
					)
				);

				// Set back pointer
				// Negative indexes in Quake3 mean leaves.
				if ( q3node.back < 0 )
					node.BackNode = nodes[ leafStart + ( ~( q3node.back ) ) ];
				else
					node.BackNode = nodes[ q3node.back ];

				// Set front pointer
				// Negative indexes in Quake3 mean leaves
				if ( q3node.front < 0 )
					node.FrontNode = nodes[ leafStart + ( ~( q3node.front ) ) ];
				else
					node.FrontNode = nodes[ q3node.front ];
			}

			//-----------------------------------------------------------------------
			// Brushes
			//-----------------------------------------------------------------------
			// Reserve enough memory for all brushes, solid or not (need to maintain indexes)
			brushes = new BspBrush[ q3lvl.NumBrushes ];

			for ( int i = 0; i < q3lvl.NumBrushes; i++ )
			{
				InternalBspBrush q3brush = q3lvl.Brushes[ i ];

				// Create a new OGRE brush
				BspBrush brush = new BspBrush();
				int numBrushSides = q3brush.numSides;
				int brushSideIdx = q3brush.firstSide;

				// Iterate over the sides and create plane for each
				while ( numBrushSides-- > 0 )
				{
					InternalBspPlane side = q3lvl.Planes[ q3lvl.BrushSides[ brushSideIdx ].planeNum ];

					// Notice how we normally invert Q3A plane distances, but here we do not
					// Because we want plane normals pointing out of solid brushes, not in.
					Plane brushSide = new Plane(
						new Vector3(
							q3lvl.Planes[ q3lvl.BrushSides[ brushSideIdx ].planeNum ].normal[ 0 ],
							q3lvl.Planes[ q3lvl.BrushSides[ brushSideIdx ].planeNum ].normal[ 1 ],
							q3lvl.Planes[ q3lvl.BrushSides[ brushSideIdx ].planeNum ].normal[ 2 ]
						), q3lvl.Planes[ q3lvl.BrushSides[ brushSideIdx ].planeNum ].distance );

					brush.Planes.Add( brushSide );
					brushSideIdx++;
				}

				// Build world fragment
				brush.Fragment.FragmentType = WorldFragmentType.PlaneBoundedRegion;
				brush.Fragment.Planes = brush.Planes;

				brushes[ i ] = brush;
			}

			//-----------------------------------------------------------------------
			// Leaves
			//-----------------------------------------------------------------------
			for ( int i = 0; i < q3lvl.NumLeaves; ++i )
			{
				BspNode node = nodes[ i + this.LeafStart ];
				InternalBspLeaf q3leaf = q3lvl.Leaves[ i ];

				node.IsLeaf = true;
				node.Owner = this;

				// Set bounding box
				node.BoundingBox.Minimum = new Vector3(
					q3leaf.bbox[ 0 ],
					q3leaf.bbox[ 1 ],
					q3leaf.bbox[ 2 ]
					);
				node.BoundingBox.Maximum = new Vector3(
					q3leaf.bbox[ 3 ],
					q3leaf.bbox[ 4 ],
					q3leaf.bbox[ 5 ]
					);

				// Set faces
				node.FaceGroupStart = q3leaf.faceStart;
				node.NumFaceGroups = q3leaf.faceCount;
				node.VisCluster = q3leaf.cluster;

				// Load Brushes for this leaf
				int realBrushIdx = 0, solidIdx = 0;
				int brushCount = q3leaf.brushCount;
				int brushIdx = q3leaf.brushStart;

				node.SolidBrushes = new BspBrush[ brushCount ];

				while ( brushCount-- > 0 )
				{
					realBrushIdx = q3lvl.LeafBrushes[ brushIdx ];
					InternalBspBrush q3brush = q3lvl.Brushes[ realBrushIdx ];

					// Only load solid ones, we don't care about any other types
					// Shader determines this.
					InternalBspShader brushShader = q3lvl.Shaders[ q3brush.shaderIndex ];

					if ( ( brushShader.contentFlags & ContentFlags.Solid ) == ContentFlags.Solid )
						node.SolidBrushes[ solidIdx ] = brushes[ realBrushIdx ];

					brushIdx++;
					solidIdx++;
				}
			}

			// Vis - just copy
			visData.numClusters = q3lvl.VisData.clusterCount;
			visData.rowLength = q3lvl.VisData.rowSize;
			visData.tableData = new byte[ q3lvl.VisData.rowSize * q3lvl.VisData.clusterCount ];

			Array.Copy( q3lvl.VisData.data, 0, visData.tableData, 0, visData.tableData.Length );
		}

		/// <summary>
		///		Internal method for parsing chosen entities.
		/// </summary>
		protected void LoadEntities( Quake3Level q3lvl )
		{
			Vector3 origin = new Vector3();
			float angle = 0;
			bool isPlayerStart = false;
			string[] entities = q3lvl.Entities.Split( '\n' );

			for ( int i = 0; i < entities.Length; i++ )
			{
				// Remove whitespace and quotes.
				entities[ i ] = entities[ i ].Trim().Replace( "\"", "" );

				if ( entities[ i ].Length == 0 )
					continue;

				string[] paramList = entities[ i ].Split( ' ' );

				if ( paramList[ 0 ] == "origin" )
				{
					float[] vector = new float[ 3 ];
					for ( int v = 0; v < 3; v++ )
						vector[ v ] = StringConverter.ParseFloat( paramList[ v + 1 ] );

					q3lvl.TransformVector( vector );

					origin = new Vector3( vector[ 0 ], vector[ 1 ], vector[ 2 ] );
				}

				if ( paramList[ 0 ] == "angle" )
					angle = StringConverter.ParseFloat( paramList[ 1 ] );

				if ( ( paramList[ 0 ] == "classname" ) && ( paramList[ 1 ] == "info_player_deathmatch" ) )
					isPlayerStart = true;

				if ( paramList[ 0 ] == "}" )
				{
					if ( isPlayerStart )
					{
						ViewPoint vp = new ViewPoint();
						vp.position = origin;

						if ( q3lvl.Options.setYAxisUp )
							vp.orientation = Quaternion.FromAngleAxis( Utility.DegreesToRadians( angle ), Vector3.UnitY );
						else
							vp.orientation = Quaternion.FromAngleAxis( Utility.DegreesToRadians( angle ), Vector3.UnitZ );

						playerStarts.Add( vp );
					}

					isPlayerStart = false;
				}
			}
		}

		protected void QuakeVertexToBspVertex( InternalBspVertex src, out BspVertex dest, out TextureLightMap texLightMap )
		{
			dest = new BspVertex();

			dest.position = new Vector3( src.point[ 0 ], src.point[ 1 ], src.point[ 2 ] );
			dest.normal = new Vector3( src.normal[ 0 ], src.normal[ 1 ], src.normal[ 2 ] );
			dest.texCoords = new Vector2( src.texture[ 0 ], src.texture[ 1 ] );
			dest.lightMap = new Vector2( src.lightMap[ 0 ], src.lightMap[ 1 ] );

			texLightMap = new TextureLightMap();
			texLightMap.color = src.color;
		}

		protected void TagNodesWithObject( BspNode node, MovableObject obj, Vector3 pos )
		{
			if ( node.IsLeaf )
			{
				// Add to movable->node map
				// Insert all the time, will get current if already there
				objectToNodeMap.Add( obj, node );
				node.AddObject( obj );
			}
			else
			{
				// Find distance to dividing plane
				float dist = node.GetDistance( pos );

				//CHECK: treat obj as bounding box?
				if ( Utility.Abs( dist ) < obj.BoundingRadius )
				{
					// Bounding sphere crosses the plane, do both.
					TagNodesWithObject( node.BackNode, obj, pos );
					TagNodesWithObject( node.FrontNode, obj, pos );
				}
				else if ( dist < 0 )
				{
					// Do back.
					TagNodesWithObject( node.BackNode, obj, pos );
				}
				else
				{
					// Do front.
					TagNodesWithObject( node.FrontNode, obj, pos );
				}
			}
		}

		public void InitQuake3Patches( Quake3Level q3lvl, VertexDeclaration decl )
		{
			int face;

			patchVertexCount = 0;
			patchIndexCount = 0;

			// We're just building the patch here to get a hold on the size of the mesh
			// although we'll reuse this information later
			face = q3lvl.NumFaces;

			while ( face-- > 0 )
			{
				InternalBspFace src = q3lvl.Faces[ face ];

				if ( src.type == BspFaceType.Patch )
				{
					// Seems to be some crap in the Q3 level where vertex count = 0 or num control points = 0?
					if ( ( src.vertCount == 0 ) || ( src.meshCtrl[ 0 ] == 0 ) )
						continue;

					PatchSurface ps = new PatchSurface();

					// Set up control points & format.
					// Reuse the vertex declaration.
					// Copy control points into a buffer so we can convert their format.
					BspVertex[] controlPoints = new BspVertex[ src.vertCount ];
					TextureLightMap texLightMap;

					for ( int v = 0; v < src.vertCount; v++ )
						QuakeVertexToBspVertex( q3lvl.Vertices[ src.vertStart + v ], out controlPoints[ v ], out texLightMap );

					// Define the surface, but don't build it yet (no vertex / index buffer)
					ps.DefineSurface(
						controlPoints,
						decl,
						src.meshCtrl[ 0 ],
						src.meshCtrl[ 1 ]
						);

					// Get stats
					patchVertexCount += ps.RequiredVertexCount;
					patchIndexCount += ps.RequiredIndexCount;

					// Save the surface for later
					patches.Add( face, ps );
				}
			}
		}
		public void BuildQuake3Patches( int vertOffset, int indexOffset )
		{
			// Loop through the patches
			int currVertOffset = vertOffset;
			int currIndexOffset = indexOffset;

			HardwareVertexBuffer vbuf = vertexData.vertexBufferBinding.GetBuffer( 0 );

			foreach ( PatchSurface ps in patches.Values )
			{
				ps.Build( vbuf, currVertOffset, indexes, currIndexOffset );

				currVertOffset += ps.RequiredVertexCount;
				currIndexOffset += ps.RequiredIndexCount;
			}
		}
		#endregion Protected methods

		#region Resource implementation
		/// <summary>
		///		Generic load - called by <see cref="Plugin_BSPSceneManager.BspResourceManager"/>.
		/// </summary>
		protected override void load()
		{
			//if ( createParam.ContainsKey( "SetYAxisUp" ) )
			//    bool.TryParse( createParam[ "SetYAxisUp" ], out bspOptions.setYAxisUp);

			//if ( createParam.ContainsKey( "Scale" ) )
			//    float.TryParse( createParam[ "Scale" ], out bspOptions.scale );

			//Vector3 move = Vector3.Zero;
			//if ( createParam.ContainsKey( "MoveX" ) )
			//     float.TryParse( createParam[ "MoveX" ], out move.x );

			//if ( createParam.ContainsKey("MoveY" ) )
			//    float.TryParse( createParam["MoveY"], out move.y );

			//if ( createParam.ContainsKey( "MoveZ" ) )
			//    float.TryParse( createParam["MoveZ"], out move.z );

			//if ( createParam.ContainsKey( "UseLightmaps" ) )
			//     bool.TryParse( createParam[ "UseLightmaps" ], out bspOptions.useLightmaps );

			//if ( createParam.ContainsKey( "AmbientEnabled" ) )
			//     bool.TryParse( createParam[ "AmbientEnabled" ], out bspOptions.ambientEnabled );

			//if ( createParam.ContainsKey( "AmbientRatio") )
			//     float.TryParse( createParam[ "AmbientRatio" ], out bspOptions.ambientRatio );

			Quake3Level q3 = new Quake3Level( bspOptions );

			Stream chunk = ResourceGroupManager.Instance.OpenResource( Name, ResourceGroupManager.Instance.WorldResourceGroupName );
			q3.LoadFromStream( chunk );
			LoadQuake3Level( q3 );
			chunk.Close();

		}

		/// <summary>
		///		Generic unload - called by <see cref="BspResourceManager"/>.
		/// </summary>
		protected override void unload()
		{
			nodes = null;
			faceGroups = null;
			leafFaceGroups = null;
			brushes = null;
		}
		#endregion Resource implementation

		#region Sub types
		/// <summary>
		///		Internal lookup table to determine visibility between leaves.
		/// </summary>
		/// <remarks>
		///		Leaf nodes are assigned to 'clusters' of nodes, which are used to group nodes together for
		///		visibility testing. This data holds a lookup table which is used to determine if one cluster of leaves
		///		is visible from another cluster. Whilst it would be possible to expand all this out so that
		///		each node had a list of pointers to other visible nodes, this would be very expensive in terms
		///		of storage (using the cluster method there is a table which is 1-bit squared per cluster, rounded
		///		up to the nearest byte obviously, which uses far less space than 4-bytes per linked node per source
		///		node). Of course the limitation here is that you have to each leaf in turn to determine if it is visible
		///		rather than just following a list, but since this is only done once per frame this is not such a big
		///		overhead.
		///		<p/>
		///		Each row in the table is a 'from' cluster, with each bit in the row corresponding to a 'to' cluster,
		///		both ordered based on cluster index. A 0 in the bit indicates the 'to' cluster is not visible from the
		///		'from' cluster, whilst a 1 indicates it is.
		///		<p/>
		///		As many will notice, this is lifted directly from the Quake implementation of PVS.
		///	</remarks>
		[StructLayout( LayoutKind.Sequential )]
		public struct VisData
		{
			public byte[] tableData;
			public int numClusters;
			public int rowLength;
		}
		#endregion Sub types

		public static int CalculateLoadingStages( string filename )
		{
			Stream stream = ResourceGroupManager.Instance.OpenResource( filename, ResourceGroupManager.Instance.WorldResourceGroupName );
			return calculateLoadingStages( stream );
		}
		//-----------------------------------------------------------------------
		private static int calculateLoadingStages( Stream stream )
		{
			Quake3Level q3 = new Quake3Level( null );

			// Load header only
			q3.LoadHeaderFromStream( stream );

			// Ok, count up the things that we will report
			int stages = 0;

			// loadEntities (1 stage)
			++stages;
			// extractLightmaps (external, 1 stage)
			++stages;
			// initQuake3Patches
			++stages;
			// vertex setup
			++stages;
			// face setup
			++stages;
			// patch building
			++stages;
			// material setup
			// this is not strictly based on load, since we only know the number
			// of faces, not the number of materials
			// raise one event for every 50 faces, plus one at the end
			//stages += (q3.Faces.Length / NUM_FACES_PER_PROGRESS_REPORT) + 1;
			// node setup
			//stages += (q3.Nodes.Length / NUM_NODES_PER_PROGRESS_REPORT) + 1;
			// brush setup
			//stages += (q3.Brushes.Length / NUM_BRUSHES_PER_PROGRESS_REPORT) + 1;
			// leaf setup
			//stages += (q3.Leaves.Length / NUM_LEAVES_PER_PROGRESS_REPORT) + 1;
			// vis
			++stages;

			return stages;

		}
	}

	/// <summary>
	///		Vertex format for fixed geometry.
	/// </summary>
	/// <remarks>
	///		Note that in this case vertex components (position, normal, texture coords etc)
	///		are held interleaved in the same buffer. However, the format here is different from
	///		the format used by Quake because older Direct3d drivers like the vertex elements
	///		to be in a particular order within the buffer. See VertexDeclaration for full
	///		details of this marvellous(not) feature.
	///	</remarks>
	[StructLayout( LayoutKind.Sequential )]
	public struct BspVertex
	{
		public Vector3 position;
		public Vector3 normal;
		public Vector2 texCoords;
		public Vector2 lightMap;
	}

	public class BspOptions
	{
		public bool setYAxisUp;
		public float scale;
		public Vector3 move;
		public bool useLightmaps;
		public bool ambientEnabled;
		public float ambientRatio;

		public BspOptions()
		{
			setYAxisUp = false;
			scale = 1f;
			move = Vector3.Zero;
			useLightmaps = true;
			ambientEnabled = false;
			ambientRatio = 1;
		}
	}
}