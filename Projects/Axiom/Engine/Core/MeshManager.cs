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
using System.Collections.Generic;
using Axiom.Graphics;
using Axiom.Math;
using ResourceHandle = System.UInt64;

#endregion Namespace Declarations

namespace Axiom.Core
{
	/// <summary>
	///		Handles the management of mesh resources.
	/// </summary>
	public class MeshManager : ResourceManager, IManualResourceLoader
	{

		#region Enumerations and Structures

		/// <summary>
		/// Enum identifying the types of manual mesh built by this manager
		/// </summary>
		enum MeshBuildType
		{
			Plane,
			CurvedIllusionPlane,
			CurvedPlane
		};

		/// <summary>
		/// Saved parameters used to (re)build a manual mesh built by this class
		/// </summary>
		struct MeshBuildParams
		{
			public MeshBuildType Type;
			public Plane Plane;
			public Real Width;
			public Real Height;
			public Real Curvature;
			public int XSegments;
			public int YSegments;
			public bool Normals;
			public int TexCoordSetCount;
			public Real XTile;
			public Real YTile;
			public Vector3 UpVector;
			public Quaternion Orientation;
			public BufferUsage VertexBufferUsage;
			public BufferUsage IndexBufferUsage;
			public bool VertexShadowBuffer;
			public bool IndexShadowBuffer;
			public int YSegmentsToKeep;
		};

		#endregion Enumerations and Structures

		#region Singleton implementation

		/// <summary>
		///     Gets the singleton instance of this class.
		/// </summary>
		public static MeshManager Instance
		{
			get
			{
				return Singleton<MeshManager>.Instance;
			}
		}

		#endregion Singleton implementation

		#region Fields and Properties

		//the factor by which the bounding box of an entity is padded
		Real _boundsPaddingFactor;
		public Real BoundsPaddingFactor
		{
			get
			{
				return _boundsPaddingFactor;
			}
			set
			{
				_boundsPaddingFactor = value;
			}
		}

		private readonly ChainedEvent<MeshSerializerArgs> _processMaterialNameEvent = new ChainedEvent<MeshSerializerArgs>();
		/// <summary>
		/// This event allows users to hook into the mesh loading process and
		///	modify references within the mesh as they are loading. Material 
		///	references can be processed using this event which allows
		///	finer control over resources.
		/// </summary>
		public event EventHandler<MeshSerializerArgs> ProcessMaterialName
		{
			add
			{
				_processMaterialNameEvent.EventSinks += value;
			}
			remove
			{
				_processMaterialNameEvent.EventSinks -= value;
			}
		}

		private readonly ChainedEvent<MeshSerializerArgs> _processSkeletonNameEvent = new ChainedEvent<MeshSerializerArgs>();
		/// <summary>
		/// This event allows users to hook into the mesh loading process and
		///	modify references within the mesh as they are loading. Skeletal 
		///	references can be processed using this event which allows
		///	finer control over resources.
		/// </summary>
		public event EventHandler<MeshSerializerArgs> ProcessSkeletonName
		{
			add
			{
				_processSkeletonNameEvent.EventSinks += value;
			}
			remove
			{
				_processSkeletonNameEvent.EventSinks -= value;
			}
		}

		private Dictionary<Mesh, MeshBuildParams> _meshBuildParams = new Dictionary<Mesh, MeshBuildParams>();

		#region PrepareAllMeshesForShadowVolumes Property

		/// <summary>
		///		Flag indicating whether newly loaded meshes should also be prepared for
		///		shadow volumes.
		/// </summary>
		private bool _prepAllMeshesForShadowVolumes = false;
		/// <summary>
		///		Tells the mesh manager that all future meshes should prepare themselves for
		///		shadow volumes on loading.
		/// </summary>
		public bool PrepareAllMeshesForShadowVolumes
		{
			get
			{
				return _prepAllMeshesForShadowVolumes;
			}
			set
			{
				_prepAllMeshesForShadowVolumes = value;
			}
		}

		#endregion PrepareAllMeshesForShadowVolumes Property

		#endregion Fields and Properties

		#region Construction and Destruction

		/// <summary>
		///     Internal constructor.  This class cannot be instantiated externally.
		/// </summary>
		public MeshManager()
		{
			_boundsPaddingFactor = 0.01f;

			// Loading order
			LoadingOrder = 350.0f;

			// Resource type
			ResourceType = "Mesh";

			// Register with resource group manager
			ResourceGroupManager.Instance.RegisterResourceManager( ResourceType, this );
		}

		#endregion Construction and Destruction

		#region Methods

		/// <summary>
		///		Called internally to initialize this manager.
		/// </summary>
		internal void Initialize()
		{
			_createPrefabPlane();
			_createPrefabCube();
			_createPrefabSphere();
		}

		/// <summary>
		///		Creates a barebones Mesh object that can be used to manually define geometry later on.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public Mesh CreateManual( string name, string group, IManualResourceLoader loader )
		{
			return (Mesh)Create( name, group, true, loader, null );
		}

		#region Load Method

		/// <summary>
		/// </summary>
		public new Mesh Load( string name, string group )
		{
			return Load( name, group, BufferUsage.StaticWriteOnly, BufferUsage.StaticWriteOnly, true, true, 1 );
		}

		/// <summary>
		/// </summary>
		public Mesh Load( string name, string group, BufferUsage vertexBufferUsage, BufferUsage indexBufferUsage )
		{
			return Load( name, group, vertexBufferUsage, indexBufferUsage, true, true, 1 );
		}

		public Mesh Load( string name, string group, BufferUsage vertexBufferUsage, BufferUsage indexBufferUsage, bool vertexBufferShadowed, bool indexBufferShadowed, int priority )
		{
			Mesh mesh = null;

			// if the resource isn't cached, create it
			if ( !resources.ContainsKey( name ) )
			{
				mesh = (Mesh)Create( name, group );
				mesh.SetVertexBufferPolicy( vertexBufferUsage, vertexBufferShadowed );
				mesh.SetIndexBufferPolicy( indexBufferUsage, indexBufferShadowed );
			}
			else
			{
				// get the cached version
				mesh = (Mesh)resources[ name ];
			}
			mesh.Load();

			return mesh;
		}

		#endregion Load Method

		#region CreatePlane Method

		/// <summary>
		///		Overloaded method.
		/// </summary>
		/// <param name="name">Name of the plane mesh.</param>
		/// <param name="plane">Plane to use for distance and orientation of the mesh.</param>
		/// <param name="width">Width in world coordinates.</param>
		/// <param name="height">Height in world coordinates.</param>
		/// <returns></returns>
		public Mesh CreatePlane( string name, string group, Plane plane, int width, int height )
		{
			return CreatePlane( name, group, plane, width, height, 1, 1, true, 1, 1.0f, 1.0f, Vector3.UnitY, BufferUsage.StaticWriteOnly, BufferUsage.StaticWriteOnly, true, true );
		}

		public Mesh CreatePlane( string name, string group, Plane plane, float width, float height, int xSegments, int ySegments, bool normals, int texCoordSetCount, float uTile, float vTile, Vector3 upVec )
		{
			return CreatePlane( name, group, plane, width, height, xSegments, ySegments, normals, texCoordSetCount, uTile, vTile, upVec, BufferUsage.StaticWriteOnly, BufferUsage.StaticWriteOnly, true, true );
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="name">Name of the plane mesh.</param>
		/// <param name="group"></param>
		/// <param name="plane">Plane to use for distance and orientation of the mesh.</param>
		/// <param name="width">Width in world coordinates.</param>
		/// <param name="height">Height in world coordinates.</param>
		/// <param name="xSegments">Number of x segments for tesselation.</param>
		/// <param name="ySegments">Number of y segments for tesselation.</param>
		/// <param name="normals">If true, plane normals are created.</param>
		/// <param name="texCoordSetCount">Number of 2d texture coord sets to use.</param>
		/// <param name="uTile">Number of times the texture should be repeated in the u direction.</param>
		/// <param name="vTile">Number of times the texture should be repeated in the v direction.</param>
		/// <param name="upVec">The up direction of the plane.</param>
		/// <param name="vertexBufferUsage"></param>
		/// <param name="indexBufferUsage"></param>
		/// <param name="vertexShadowBuffer"></param>
		/// <param name="indexShadowBuffer"></param>
		/// <returns></returns>
		public Mesh CreatePlane( string name, string group, Plane plane, float width, float height, int xSegments, int ySegments, bool normals, int texCoordSetCount, float uTile, float vTile, Vector3 upVec,
			BufferUsage vertexBufferUsage, BufferUsage indexBufferUsage, bool vertexShadowBuffer, bool indexShadowBuffer )
		{
			// Create manual mesh which calls back self to load
			Mesh mesh = CreateManual( name, group, this );
			// Planes can never be manifold
			mesh.AutoBuildEdgeLists = false;
			// store parameters
			MeshBuildParams meshParams = new MeshBuildParams();
			meshParams.Type = MeshBuildType.Plane;
			meshParams.Plane = plane;
			meshParams.Width = width;
			meshParams.Height = height;
			meshParams.XSegments = xSegments;
			meshParams.YSegments = ySegments;
			meshParams.Normals = normals;
			meshParams.TexCoordSetCount = texCoordSetCount;
			meshParams.XTile = uTile;
			meshParams.YTile = vTile;
			meshParams.UpVector = upVec;
			meshParams.VertexBufferUsage = vertexBufferUsage;
			meshParams.IndexBufferUsage = indexBufferUsage;
			meshParams.VertexShadowBuffer = vertexShadowBuffer;
			meshParams.IndexShadowBuffer = indexShadowBuffer;
			_meshBuildParams.Add( mesh, meshParams );

			// to preserve previous behaviour, load immediately
			mesh.Load();

			return mesh;
		}

		#endregion CreatePlane Method

		#region CreateCurvedPlane Method

		/// <summary>
		/// </summary>
		public Mesh CreateCurvedPlane( string name, string group, Plane plane, float width, float height )
		{
			return CreateCurvedPlane( name, group, plane, width, height, 0.5f, 1, 1, false, 1, 1.0f, 1.0f, Vector3.UnitY, BufferUsage.StaticWriteOnly, BufferUsage.StaticWriteOnly, true, true );
		}

		/// <summary>
		/// </summary>
		public Mesh CreateCurvedPlane( string name, string group, Plane plane, float width, float height, Real bow, int xSegments, int ySegments, bool normals, int texCoordSetCount, float xTile, float yTile, Vector3 upVec )
		{
			return CreateCurvedPlane( name, group, plane, width, height, bow, xSegments, ySegments, normals, texCoordSetCount, xTile, yTile, upVec, BufferUsage.StaticWriteOnly, BufferUsage.StaticWriteOnly, true, true );
		}

		/// <summary>
		/// </summary>
		public Mesh CreateCurvedPlane( string name, string group, Plane plane, float width, float height, Real bow, int xSegments, int ySegments, bool normals, int texCoordSetCount, float xTile, float yTile, Vector3 upVector, BufferUsage vertexBufferUsage, BufferUsage indexBufferUsage, bool vertexShadowBuffer, bool indexShadowBuffer )
		{
			// Create manual mesh which calls back self to load
			Mesh mesh = CreateManual( name, group, this );
			// Planes can never be manifold
			mesh.AutoBuildEdgeLists = false;
			// store parameters
			MeshBuildParams meshParams = new MeshBuildParams();
			meshParams.Type = MeshBuildType.CurvedPlane;
			meshParams.Plane = plane;
			meshParams.Width = width;
			meshParams.Height = height;
			meshParams.Curvature = bow;
			meshParams.XSegments = xSegments;
			meshParams.YSegments = ySegments;
			meshParams.Normals = normals;
			meshParams.TexCoordSetCount = texCoordSetCount;
			meshParams.XTile = xTile;
			meshParams.YTile = yTile;
			meshParams.UpVector = upVector;
			meshParams.VertexBufferUsage = vertexBufferUsage;
			meshParams.IndexBufferUsage = indexBufferUsage;
			meshParams.VertexShadowBuffer = vertexShadowBuffer;
			meshParams.IndexShadowBuffer = indexShadowBuffer;
			_meshBuildParams.Add( mesh, meshParams );

			// to preserve previous behaviour, load immediately
			mesh.Load();

			return mesh;
		}

		#endregion CreateCurvedPlane Method

		#region CreateCurvedIllusionPlane Method

		/// <summary>
		/// </summary>
		public Mesh CreateCurvedIllusionPlane( string name, string group, Plane plane, float width, float height, float curvature, int xSegments, int ySegments, bool normals, int texCoordSetCount, float xTiles, float yTiles, Vector3 upVector )
		{
			return CreateCurvedIllusionPlane( name, group, plane, width, height, curvature, xSegments, ySegments, normals, texCoordSetCount, xTiles, yTiles, upVector, Quaternion.Identity, BufferUsage.StaticWriteOnly, BufferUsage.StaticWriteOnly, true, true, -1 );
		}

		/// <summary>
		/// </summary>
		public Mesh CreateCurvedIllusionPlane( string name, string group, Plane plane, float width, float height, float curvature, int xSegments, int ySegments, bool normals, int texCoordSetCount, float xTiles, float yTiles, Vector3 upVector, Quaternion orientation, BufferUsage vertexBufferUsage, BufferUsage indexBufferUsage, bool vertexShadowBuffer, bool indexShadowBuffer )
		{
			return CreateCurvedIllusionPlane( name, group, plane, width, height, curvature, xSegments, ySegments, normals, texCoordSetCount, xTiles, yTiles, upVector, Quaternion.Identity, BufferUsage.StaticWriteOnly, BufferUsage.StaticWriteOnly, vertexShadowBuffer, indexShadowBuffer, -1 );
		}

		/// <summary>
		/// </summary>
		public Mesh CreateCurvedIllusionPlane( string name, string group, Plane plane, float width, float height, float curvature, int xSegments, int ySegments, bool normals, int texCoordSetCount, float xTiles, float yTiles, Vector3 upVector, Quaternion orientation, BufferUsage vertexBufferUsage, BufferUsage indexBufferUsage, bool vertexShadowBuffer, bool indexShadowBuffer, int ySegmentsToKeep )
		{

			// Create manual mesh which calls back self to load
			Mesh mesh = CreateManual( name, group, this );
			// Planes can never be manifold
			mesh.AutoBuildEdgeLists = false;
			// store parameters
			MeshBuildParams meshParams = new MeshBuildParams();
			meshParams.Type = MeshBuildType.Plane;
			meshParams.Plane = plane;
			meshParams.Width = width;
			meshParams.Height = height;
			meshParams.Curvature = curvature;
			meshParams.XSegments = xSegments;
			meshParams.YSegments = ySegments;
			meshParams.Normals = normals;
			meshParams.TexCoordSetCount = texCoordSetCount;
			meshParams.XTile = xTiles;
			meshParams.YTile = yTiles;
			meshParams.Orientation = orientation;
			meshParams.UpVector = upVector;
			meshParams.VertexBufferUsage = vertexBufferUsage;
			meshParams.IndexBufferUsage = indexBufferUsage;
			meshParams.VertexShadowBuffer = vertexShadowBuffer;
			meshParams.IndexShadowBuffer = indexShadowBuffer;
			meshParams.YSegmentsToKeep = ySegmentsToKeep;
			_meshBuildParams.Add( mesh, meshParams );

			// to preserve previous behaviour, load immediately
			mesh.Load();

			return mesh;
		}

		#endregion CreateCurvedIllusionPlane Method

		/// <summary>
		///     Creates a Bezier patch based on an array of control vertices.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="controlPointBuffer"></param>
		/// <param name="declaration"></param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="uMaxSubdivisionLevel"></param>
		/// <param name="vMaxSubdivisionLevel"></param>
		/// <param name="visibleSide"></param>
		/// <param name="vbUsage"></param>
		/// <param name="ibUsage"></param>
		/// <param name="vbUseShadow"></param>
		/// <param name="ibUseShadow"></param>
		/// <returns></returns>
		public PatchMesh CreateBezierPatch( string name, string group, Array controlPointBuffer, VertexDeclaration declaration,
			int width, int height, int uMaxSubdivisionLevel, int vMaxSubdivisionLevel, VisibleSide visibleSide,
			BufferUsage vbUsage, BufferUsage ibUsage, bool vbUseShadow, bool ibUseShadow )
		{
			if ( width < 3 || height < 3 )
			{
				throw new Exception( "Bezier patch requires at least 3x3 control points." );
			}
			PatchMesh mesh = (PatchMesh)this[ name ];

			if ( mesh != null )
			{
				throw new AxiomException( "A mesh with the name {0} already exists!", name );
			}

			mesh = new PatchMesh( this, name, nextHandle, group );

			mesh.Define( controlPointBuffer, declaration, width, height, uMaxSubdivisionLevel, vMaxSubdivisionLevel, visibleSide, vbUsage, ibUsage, vbUseShadow, ibUseShadow );

			mesh.Load();

			_add( mesh );

			return mesh;
		}

		/// <summary>
		///		Used to generate a face list based on vertices.
		/// </summary>
		private void _tesselate2DMesh( SubMesh subMesh, int width, int height, bool doubleSided, BufferUsage indexBufferUsage, bool indexShadowBuffer )
		{
			int vInc, uInc, v, u, iterations;
			int vCount, uCount;

			vInc = 1;
			v = 0;

			iterations = doubleSided ? 2 : 1;

			// setup index count
			subMesh.indexData.indexCount = ( width - 1 ) * ( height - 1 ) * 2 * iterations * 3;

			// create the index buffer using the current API
			subMesh.indexData.indexBuffer =
				HardwareBufferManager.Instance.CreateIndexBuffer( IndexType.Size16, subMesh.indexData.indexCount, indexBufferUsage, indexShadowBuffer );

			short v1, v2, v3;

			// grab a reference for easy access
			HardwareIndexBuffer idxBuffer = subMesh.indexData.indexBuffer;

			// lock the whole index buffer
			IntPtr data = idxBuffer.Lock( BufferLocking.Discard );

			unsafe
			{
				short* pIndex = (short*)data.ToPointer();

				while ( 0 < iterations-- )
				{
					// make tris in a zigzag pattern (strip compatible)
					u = 0;
					uInc = 1;

					vCount = height - 1;

					while ( 0 < vCount-- )
					{
						uCount = width - 1;

						while ( 0 < uCount-- )
						{
							// First Tri in cell
							// -----------------
							v1 = (short)( ( ( v + vInc ) * width ) + u );
							v2 = (short)( ( v * width ) + u );
							v3 = (short)( ( ( v + vInc ) * width ) + ( u + uInc ) );
							// Output indexes
							*pIndex++ = v1;
							*pIndex++ = v2;
							*pIndex++ = v3;
							// Second Tri in cell
							// ------------------
							v1 = (short)( ( ( v + vInc ) * width ) + ( u + uInc ) );
							v2 = (short)( ( v * width ) + u );
							v3 = (short)( ( v * width ) + ( u + uInc ) );
							// Output indexes
							*pIndex++ = v1;
							*pIndex++ = v2;
							*pIndex++ = v3;

							// Next column
							u += uInc;

						} // while uCount

						v += vInc;
						u = 0;

					} // while vCount

					v = height - 1;
					vInc = -vInc;
				} // while iterations
			}// unsafe

			// unlock the buffer
			idxBuffer.Unlock();
		}

		private void _generatePlaneVertexData( HardwareVertexBuffer vbuf, int ySegments, int xSegments, float xSpace, float halfWidth, float ySpace, float halfHeight, Matrix4 transform, bool firstTime, bool normals, Matrix4 rotation, int numTexCoordSets, float xTexCoord, float yTexCoord, SubMesh subMesh, ref Vector3 min, ref Vector3 max, ref float maxSquaredLength )
		{
			Vector3 vec;
			unsafe
			{
				// lock the vertex buffer
				IntPtr data = vbuf.Lock( BufferLocking.Discard );

				float* pData = (float*)data.ToPointer();

				for ( int y = 0; y <= ySegments; y++ )
				{
					for ( int x = 0; x <= xSegments; x++ )
					{
						// centered on origin
						vec.x = ( x * xSpace ) - halfWidth;
						vec.y = ( y * ySpace ) - halfHeight;
						vec.z = 0.0f;

						vec = transform.TransformAffine( vec );

						*pData++ = vec.x;
						*pData++ = vec.y;
						*pData++ = vec.z;

						// Build bounds as we go
						if ( firstTime )
						{
							min = vec;
							max = vec;
							maxSquaredLength = vec.LengthSquared;
							firstTime = false;
						}
						else
						{
							min.Floor( vec );
							max.Ceil( vec );
							maxSquaredLength = Utility.Max( maxSquaredLength, vec.LengthSquared );
						}

						if ( normals )
						{
							vec = Vector3.UnitZ;
							vec = rotation.TransformAffine( vec );

							*pData++ = vec.x;
							*pData++ = vec.y;
							*pData++ = vec.z;
						}

						for ( int i = 0; i < numTexCoordSets; i++ )
						{
							*pData++ = x * xTexCoord;
							*pData++ = 1 - ( y * yTexCoord );
						} // for texCoords
					} // for x
				} // for y

				// unlock the buffer
				vbuf.Unlock();

				subMesh.useSharedVertices = true;

			} // unsafe
		}

		private void _generateCurvedPlaneVertexData( HardwareVertexBuffer vbuf, int ySegments, int xSegments, float xSpace, float halfWidth, float ySpace, float halfHeight, Matrix4 transform, bool firstTime, bool normals, Matrix4 rotation, float curvature, int numTexCoordSets, float xTexCoord, float yTexCoord, SubMesh subMesh, ref Vector3 min, ref Vector3 max, ref float maxSquaredLength )
		{
			Vector3 vec;
			unsafe
			{
				// lock the vertex buffer
				IntPtr data = vbuf.Lock( BufferLocking.Discard );

				float* pData = (float*)data.ToPointer();

				for ( int y = 0; y <= ySegments; y++ )
				{
					for ( int x = 0; x <= xSegments; x++ )
					{
						// centered on origin
						vec.x = ( x * xSpace ) - halfWidth;
						vec.y = ( y * ySpace ) - halfHeight;

						// Here's where curved plane is different from standard plane.  Amazing, I know.
						Real diff_x = ( x - ( (Real)xSegments / 2 ) ) / (Real)xSegments;
						Real diff_y = ( y - ( (Real)ySegments / 2 ) ) / (Real)ySegments;
						Real dist = Utility.Sqrt( diff_x * diff_x + diff_y * diff_y );
						vec.z = ( -Utility.Sin( ( 1 - dist ) * ( Utility.PI / 2 ) ) * curvature ) + curvature;

						// Transform by orientation and distance
						Vector3 pos = transform.TransformAffine( vec );

						*pData++ = pos.x;
						*pData++ = pos.y;
						*pData++ = pos.z;

						// Build bounds as we go
						if ( firstTime )
						{
							min = vec;
							max = vec;
							maxSquaredLength = vec.LengthSquared;
							firstTime = false;
						}
						else
						{
							min.Floor( vec );
							max.Ceil( vec );
							maxSquaredLength = Utility.Max( maxSquaredLength, vec.LengthSquared );
						}

						if ( normals )
						{
							// This part is kinda 'wrong' for curved planes... but curved planes are
							//   very valuable outside sky planes, which don't typically need normals
							//   so I'm not going to mess with it for now.

							// Default normal is along unit Z
							//vec = Vector3::UNIT_Z;
							// Rotate
							vec = rotation.TransformAffine( vec );

							*pData++ = vec.x;
							*pData++ = vec.y;
							*pData++ = vec.z;
						}

						for ( int i = 0; i < numTexCoordSets; i++ )
						{
							*pData++ = x * xTexCoord;
							*pData++ = 1 - ( y * yTexCoord );
						} // for texCoords
					} // for x
				} // for y

				// unlock the buffer
				vbuf.Unlock();

				subMesh.useSharedVertices = true;

			} // unsafe
		}

		private void _generateCurvedIllusionPlaneVertexData( HardwareVertexBuffer vertexBuffer, int ySegments, int xSegments, float xSpace, float halfWidth, float ySpace, float halfHeight, Matrix4 xform, bool firstTime, bool normals, Quaternion orientation, float curvature, float uTiles, float vTiles, int numberOfTexCoordSets, ref Vector3 min, ref Vector3 max, ref float maxSquaredLength )
		{
			// Imagine a large sphere with the camera located near the top
			// The lower the curvature, the larger the sphere
			// Use the angle from viewer to the points on the plane
			// Credit to Aftershock for the general approach
			Real cameraPosition;      // Camera position relative to sphere center

			// Derive sphere radius
			Vector3 vertPos;  // position relative to camera
			Real sphDist;      // Distance from camera to sphere along box vertex vector
			// Vector3 camToSph; // camera position to sphere
			Real sphereRadius;// Sphere radius
			// Actual values irrelevant, it's the relation between sphere radius and camera position that's important
			Real sphRadius = 100.0f;
			Real camDistance = 5.0f;

			sphereRadius = sphRadius - curvature;
			cameraPosition = sphereRadius - camDistance;

			Vector3 vec;
			Vector3 norm;
			float sphereDistance;
			unsafe
			{
				// lock the vertex buffer
				IntPtr data = vertexBuffer.Lock( BufferLocking.Discard );

				float* pData = (float*)data.ToPointer();

				for ( int y = 0; y < ySegments + 1; ++y )
				{
					for ( int x = 0; x < xSegments + 1; ++x )
					{
						// centered on origin
						vec.x = ( x * xSpace ) - halfWidth;
						vec.y = ( y * ySpace ) - halfHeight;
						vec.z = 0.0f;

						// transform by orientation and distance
						vec = xform * vec;

						// assign to geometry
						*pData++ = vec.x;
						*pData++ = vec.y;
						*pData++ = vec.z;

						// build bounds as we go
						if ( firstTime )
						{
							min = vec;
							max = vec;
							maxSquaredLength = vec.LengthSquared;
							firstTime = false;
						}
						else
						{
							min.Floor( vec );
							max.Ceil( vec );
							maxSquaredLength = Utility.Max( maxSquaredLength, vec.LengthSquared );
						}

						if ( normals )
						{
							norm = Vector3.UnitZ;
							norm = orientation * norm;

							*pData++ = vec.x;
							*pData++ = vec.y;
							*pData++ = vec.z;
						}

						// generate texture coordinates, normalize position, modify by orientation to return +y up
						vec = orientation.Inverse() * vec;
						vec.Normalize();

						// find distance to sphere
						sphereDistance = Utility.Sqrt( cameraPosition * cameraPosition * ( vec.y * vec.y - 1.0f ) + sphereRadius * sphereRadius ) - cameraPosition * vec.y;

						vec.x *= sphereDistance;
						vec.z *= sphereDistance;

						// use x and y on sphere as texture coordinates, tiled
						float s = vec.x * ( 0.01f * uTiles );
						float t = vec.z * ( 0.01f * vTiles );
						for ( int i = 0; i < numberOfTexCoordSets; i++ )
						{
							*pData++ = s;
							*pData++ = ( 1 - t );
						}
					} // x
				} // y

				// unlock the buffer
				vertexBuffer.Unlock();
			} // unsafe
		}

		private void _getVertices( ref Vector3[] points, Axiom.Animating.Bone bone )
		{
			Vector3 boneBase = bone.DerivedPosition;
			foreach ( Axiom.Animating.Bone childBone in bone.Children )
			{
				// The tip of the bone:
				Vector3 boneTip = childBone.DerivedPosition;
				// the base of the bone
				Vector3 arm = boneTip - boneBase;
				Vector3 perp1 = arm.Perpendicular();
				Vector3 perp2 = arm.Cross( perp1 );
				perp1.Normalize();
				perp2.Normalize();
				float boneLen = arm.Length;
				int offset = 6 * childBone.Handle;
				points[ offset + 0 ] = boneTip;
				points[ offset + 1 ] = boneBase;
				points[ offset + 2 ] = boneBase + boneLen / 10 * perp1;
				points[ offset + 3 ] = boneBase + boneLen / 10 * perp2;
				points[ offset + 4 ] = boneBase - boneLen / 10 * perp1;
				points[ offset + 5 ] = boneBase - boneLen / 10 * perp2;
				_getVertices( ref points, childBone );
			}
		}

		#region _createPrefab* Methods

		private void _createPrefabPlane()
		{
			Mesh mesh = (Mesh)Create( "Prefab_Plane", ResourceGroupManager.InternalResourceGroupName, true, this, null );
			// Planes can never be manifold
			mesh.AutoBuildEdgeLists = false;
			// to preserve previous behaviour, load immediately
			mesh.Load();
		}

		private void _createPrefabCube()
		{
			Mesh mesh = (Mesh)Create( "Prefab_Cube", ResourceGroupManager.InternalResourceGroupName, true, this, null );
			// to preserve previous behaviour, load immediately
			mesh.Load();
		}

		private void _createPrefabSphere()
		{
			Mesh mesh = (Mesh)Create( "Prefab_Sphere", ResourceGroupManager.InternalResourceGroupName, true, this, null );
			// Planes can never be manifold
			mesh.AutoBuildEdgeLists = false;
			// to preserve previous behaviour, load immediately
			mesh.Load();
		}

		#endregion _createPrefab* Methods

		private void _loadManual( Mesh mesh, MeshBuildParams mbp )
		{
			SubMesh subMesh = mesh.CreateSubMesh();

			// Set up vertex data
			// Use a single shared buffer
			mesh.SharedVertexData = new VertexData();
			VertexData vertexData = mesh.SharedVertexData;

			// Set up Vertex Declaration
			VertexDeclaration decl = vertexData.vertexDeclaration;
			int currOffset = 0;

			// add position data
			// We always need positions
			decl.AddElement( 0, currOffset, VertexElementType.Float3, VertexElementSemantic.Position );
			currOffset += VertexElement.GetTypeSize( VertexElementType.Float3 );

			// normals are optional
			if ( mbp.Normals )
			{
				decl.AddElement( 0, currOffset, VertexElementType.Float3, VertexElementSemantic.Normal );
				currOffset += VertexElement.GetTypeSize( VertexElementType.Float3 );
			}

			// add texture coords
			for ( ushort i = 0; i < mbp.TexCoordSetCount; i++ )
			{
				decl.AddElement( 0, currOffset, VertexElementType.Float2, VertexElementSemantic.TexCoords, i );
				currOffset += VertexElement.GetTypeSize( VertexElementType.Float2 );
			}

			vertexData.vertexCount = ( mbp.XSegments + 1 ) * ( mbp.YSegments + 1 );

			// create a new vertex buffer (based on current API)
			HardwareVertexBuffer vbuf = HardwareBufferManager.Instance.CreateVertexBuffer( decl.Clone( 0 ), vertexData.vertexCount, mbp.VertexBufferUsage, mbp.VertexShadowBuffer );

			// get a reference to the vertex buffer binding
			VertexBufferBinding binding = vertexData.vertexBufferBinding;

			// bind the first vertex buffer
			binding.SetBinding( 0, vbuf );

			// transform the plane based on its plane def
			Matrix4 translate = Matrix4.Identity;
			Matrix4 transform = Matrix4.Zero;
			Matrix4 rotation = Matrix4.Identity;
			Matrix3 rot3x3 = Matrix3.Zero;

			Vector3 xAxis, yAxis, zAxis;
			zAxis = mbp.Plane.Normal;
			zAxis.Normalize();
			yAxis = mbp.UpVector;
			yAxis.Normalize();
			xAxis = yAxis.Cross( zAxis );

			if ( xAxis.Length == 0 )
			{
				throw new AxiomException( "The up vector for a plane cannot be parallel to the planes normal." );
			}

			rot3x3.FromAxes( xAxis, yAxis, zAxis );
			rotation = rot3x3;

			// set up transform from origin
			translate.Translation = mbp.Plane.Normal * -mbp.Plane.D;

			transform = translate * rotation;

			float xSpace = mbp.Width / mbp.XSegments;
			float ySpace = mbp.Height / mbp.YSegments;
			float halfWidth = mbp.Width / 2;
			float halfHeight = mbp.Height / 2;
			float xTexCoord = ( 1.0f * mbp.XTile ) / mbp.XSegments;
			float yTexCoord = ( 1.0f * mbp.YTile ) / mbp.YSegments;
			Vector3 vec = Vector3.Zero;
			Vector3 min = Vector3.Zero;
			Vector3 max = Vector3.Zero;
			float maxSquaredLength = 0;
			bool firstTime = true;

			// generate vertex data
			switch ( mbp.Type )
			{
				case MeshBuildType.Plane:
					_generatePlaneVertexData( vbuf, mbp.YSegments, mbp.XSegments, xSpace, halfWidth, ySpace, halfHeight, transform, firstTime, mbp.Normals, rotation, mbp.TexCoordSetCount, xTexCoord, yTexCoord, subMesh, ref min, ref max, ref maxSquaredLength );
					break;
				case MeshBuildType.CurvedPlane:
					_generateCurvedPlaneVertexData( vbuf, mbp.YSegments, mbp.XSegments, xSpace, halfWidth, ySpace, halfHeight, transform, firstTime, mbp.Normals, rotation, mbp.Curvature, mbp.TexCoordSetCount, xTexCoord, yTexCoord, subMesh, ref min, ref max, ref maxSquaredLength );
					break;
				case MeshBuildType.CurvedIllusionPlane:
					_generateCurvedIllusionPlaneVertexData( vbuf, mbp.YSegments, mbp.XSegments, xSpace, halfWidth, ySpace, halfHeight, transform, firstTime, mbp.Normals, mbp.Orientation, mbp.Curvature, xTexCoord, yTexCoord, mbp.TexCoordSetCount, ref min, ref max, ref maxSquaredLength );
					break;
				default:
					throw new Exception( "" );
			}

			// generate face list
			_tesselate2DMesh( subMesh, mbp.XSegments + 1, mbp.YSegments + 1, false, mbp.IndexBufferUsage, mbp.IndexShadowBuffer );

			// generate bounds for the mesh
			mesh.BoundingBox = new AxisAlignedBox( min, max );
			mesh.BoundingSphereRadius = Utility.Sqrt( maxSquaredLength );

		}

		protected internal void FireProcessMaterialName( Mesh mesh, string name )
		{
			_processMaterialNameEvent.Fire(this, new MeshSerializerArgs { Mesh = mesh, Name = name}, (args) => { return true; });
		}

		protected internal void FireProcessSkeletonName( Mesh mesh, string name )
		{
			_processSkeletonNameEvent.Fire(this, new MeshSerializerArgs { Mesh = mesh, Name = name}, (args) => { return true; });
		}

		#endregion Methods

		#region ResourceManager Implementation

		protected override Resource _create( string name, ulong handle, string group, bool isManual, IManualResourceLoader loader, Axiom.Collections.NameValuePairList createParams )
		{
			return new Mesh( this, name, handle, group, isManual, loader );
		}

		public override void ParseScript( System.IO.Stream stream, string groupName, string fileName )
		{
			throw new Exception( "The method or operation is not implemented." );
		}

		#endregion ResourceManager Implementation

		#region CreateBoneMesh
#if NOT
		public Mesh CreateBoneMesh( string name )
		{
			Mesh mesh = CreateManual( name );
			mesh.SkeletonName = name + ".skeleton";
			SubMesh subMesh = mesh.CreateSubMesh( "BoneSubMesh" );
			subMesh.useSharedVertices = true;
			subMesh.MaterialName = "BaseWhite";

			// short[] faces = { 0, 2, 3, 0, 3, 4, 0, 4, 5, 0, 5, 2, 1, 2, 5, 1, 5, 4, 1, 4, 3, 1, 3, 2 };
			// short[] faces = { 0, 3, 2, 0, 4, 3, 0, 5, 4, 0, 2, 5, 1, 5, 2, 1, 4, 5, 1, 3, 4, 1, 2, 3 };
			short[] faces = { 0, 2, 3, 0, 3, 4, 0, 4, 5, 0, 5, 2, 1, 2, 5, 1, 5, 4, 1, 4, 3, 1, 3, 2,
							  0, 3, 2, 0, 4, 3, 0, 5, 4, 0, 2, 5, 1, 5, 2, 1, 4, 5, 1, 3, 4, 1, 2, 3 };
			int faceCount = faces.Length / 3; // faces per bone
			int vertexCount = 6; // vertices per bone

			// set up vertex data, use a single shared buffer
			mesh.SharedVertexData = new VertexData();
			VertexData vertexData = mesh.SharedVertexData;

			// set up vertex declaration
			VertexDeclaration vertexDeclaration = vertexData.vertexDeclaration;
			int currentOffset = 0;

			// always need positions
			vertexDeclaration.AddElement( 0, currentOffset, VertexElementType.Float3, VertexElementSemantic.Position );
			currentOffset += VertexElement.GetTypeSize( VertexElementType.Float3 );
			vertexDeclaration.AddElement( 0, currentOffset, VertexElementType.Float3, VertexElementSemantic.Normal );
			currentOffset += VertexElement.GetTypeSize( VertexElementType.Float3 );

			int boneCount = mesh.Skeleton.BoneCount;

			// I want 6 vertices per bone - exclude the root bone
			vertexData.vertexCount = boneCount * vertexCount;

			// allocate vertex buffer
			HardwareVertexBuffer vertexBuffer = HardwareBufferManager.Instance.CreateVertexBuffer( vertexDeclaration.GetVertexSize( 0 ), vertexData.vertexCount, BufferUsage.StaticWriteOnly );

			// set up the binding, one source only
			VertexBufferBinding binding = vertexData.vertexBufferBinding;
			binding.SetBinding( 0, vertexBuffer );

			Vector3[] vertices = new Vector3[ vertexData.vertexCount ];
			_getVertices( ref vertices, mesh.Skeleton.RootBone );

			// Generate vertex data
			unsafe
			{
				// lock the vertex buffer
				IntPtr data = vertexBuffer.Lock( BufferLocking.Discard );

				float* pData = (float*)data.ToPointer();

				foreach ( Vector3 vec in vertices )
				{
					// assign to geometry
					*pData++ = vec.x;
					*pData++ = vec.y;
					*pData++ = vec.z;
					// fake normals
					*pData++ = 0;
					*pData++ = 1;
					*pData++ = 0;
				}

				// unlock the buffer
				vertexBuffer.Unlock();
			} // unsafe


			// Generate index data
			HardwareIndexBuffer indexBuffer = HardwareBufferManager.Instance.CreateIndexBuffer( IndexType.Size16, faces.Length * boneCount, BufferUsage.StaticWriteOnly );
			subMesh.indexData.indexBuffer = indexBuffer;
			subMesh.indexData.indexCount = faces.Length * boneCount;
			subMesh.indexData.indexStart = 0;
			for ( ushort boneIndex = 0; boneIndex < mesh.Skeleton.BoneCount; ++boneIndex )
			{
				Axiom.Animating.Bone bone = mesh.Skeleton.GetBone( boneIndex );
				short[] tmpFaces = new short[ faces.Length ];
				for ( int tmp = 0; tmp < faces.Length; ++tmp )
					tmpFaces[ tmp ] = (short)( faces[ tmp ] + vertexCount * bone.Handle );
				indexBuffer.WriteData( faces.Length * bone.Handle * sizeof( short ), tmpFaces.Length * sizeof( short ), tmpFaces, true );
			}

			for ( ushort boneIndex = 0; boneIndex < mesh.Skeleton.BoneCount; ++boneIndex )
			{
				Axiom.Animating.Bone bone = mesh.Skeleton.GetBone( boneIndex );
				Axiom.Animating.Bone parentBone = bone;
				if ( bone.Parent != null )
					parentBone = (Axiom.Animating.Bone)bone.Parent;
				for ( int vertexIndex = 0; vertexIndex < vertexCount; ++vertexIndex )
				{
					Axiom.Animating.VertexBoneAssignment vba = new Axiom.Animating.VertexBoneAssignment();
					// associate the base of the joint display with the bone's parent,
					// and the rest of the points with the bone.
					vba.boneIndex = parentBone.Handle;
					vba.weight = 1.0f;
					vba.vertexIndex = vertexCount * bone.Handle + vertexIndex;
					mesh.AddBoneAssignment( vba );
				}
			}

			mesh.Load();
			mesh.Touch();

			return mesh;
		}
#endif
		#endregion CreateBoneMesh

		public new Mesh this[ string name ]
		{
			get
			{
				return (Mesh)base[ name ];
			}
		}

		public new Mesh this[ ResourceHandle handle ]
		{
			get
			{
				return (Mesh)base[ handle ];
			}
		}


		#region IManualResourceLoader Implementation

		public void LoadResource( Resource resource )
		{
			Mesh mesh = (Mesh)resource;

			bool prefab = PrefabFactory.Create( mesh );

			if ( !prefab )
			{
				MeshBuildParams mbp = _meshBuildParams[ mesh ];
				_loadManual( mesh, mbp );
			}
		}

		#endregion IManualResourceLoader Implementation

		#region IDisposable Implementation

		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				if ( disposeManagedResources )
				{
					// Unregister with resource group manager
					ResourceGroupManager.Instance.UnregisterResourceManager( ResourceType );

					Singleton<MeshManager>.Destroy();
				}

				// There are no unmanaged resources to release, but
				// if we add them, they need to be released here.
			}

			// If it is available, make the call to the
			// base class's Dispose(Boolean) method
			base.dispose( disposeManagedResources );
		}
		#endregion IDisposable Implementation
	}

	#region MeshSerializer Events

	/// <summary>
	///	Used to supply info to the ProcessMaterialName and ProcessSkeletonName events.
	/// </summary>
	public class MeshSerializerArgs : EventArgs
	{
		/// <summary>
		/// The mesh being serialized
		/// </summary>
		public Mesh Mesh;
		/// <summary>
		/// The name of the the Mesh/Skeleton to process
		/// </summary>
		public string Name;
	}

	#endregion MeshSerializer Events
}