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
//     <id value="$Id: Mesh.cs 1044 2007-05-05 21:01:55Z borrillis $"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using Axiom.Graphics;
using Axiom.Math;

#endregion Namespace Declarations

namespace Axiom.Core
{
	/// <summary>
	/// A factory class that can create various mesh prefabs.
	/// </summary>
	/// <remarks>
	/// This class is used by MeshManager to offload the loading of various prefab types
	/// to a central location.
	/// </remarks>
	public class PrefabFactory
	{

		/// <summary>
		/// If the given mesh has a known prefab resource name (e.g "Prefab_Plane")
		/// then this prefab will be created as a submesh of the given mesh.
		/// </summary>
		/// <param name="mesh">The mesh that the potential prefab will be created in.</param>
		/// <returns><c>true</c> if a prefab has been created, otherwise <c>false</c>.</returns>
		public static bool Create( Mesh mesh )
		{
			switch ( mesh.Name )
			{
				case "Prefab_Plane":
					_createPlane( mesh );
					return true;
				case "Prefab_Cube":
					_createCube( mesh );
					return true;
				case "Prefab_Sphere":
					_createSphere( mesh );
					return true;
			}

			return false;
		}

		/// <summary>
		/// Creates a plane as a submesh of the given mesh
		/// </summary>
		private static void _createPlane( Mesh mesh )
		{

			SubMesh sub = mesh.CreateSubMesh();
			float[] vertices = new float[ 32 ] {
				-100, -100, 0,	// pos
				0,0,1,			// normal
				0,1,			// texcoord
				100, -100, 0,
				0,0,1,
				1,1,
				100,  100, 0,
				0,0,1,
				1,0,
				-100,  100, 0 ,
				0,0,1,
				0,0
			};

			mesh.SharedVertexData = new VertexData();
			mesh.SharedVertexData.vertexCount = 4;
			VertexDeclaration decl = mesh.SharedVertexData.vertexDeclaration;
			VertexBufferBinding binding = mesh.SharedVertexData.vertexBufferBinding;

			int offset = 0;
			decl.AddElement( 0, offset, VertexElementType.Float3, VertexElementSemantic.Position );
			offset += VertexElement.GetTypeSize( VertexElementType.Float3 );
			decl.AddElement( 0, offset, VertexElementType.Float3, VertexElementSemantic.Normal );
			offset += VertexElement.GetTypeSize( VertexElementType.Float3 );
			decl.AddElement( 0, offset, VertexElementType.Float2, VertexElementSemantic.TexCoords, 0 );
			offset += VertexElement.GetTypeSize( VertexElementType.Float2 );

			HardwareVertexBuffer vbuf = HardwareBufferManager.Instance.CreateVertexBuffer( decl, 4, BufferUsage.StaticWriteOnly );
			binding.SetBinding( 0, vbuf );

			vbuf.SetData( vertices, 0, vbuf.Length, true );

			sub.useSharedVertices = true;
			HardwareIndexBuffer ibuf = HardwareBufferManager.Instance.CreateIndexBuffer( IndexType.Size16, 6, BufferUsage.StaticWriteOnly );

			short[] faces = new short[ 6 ] { 0, 1, 2, 0, 2, 3 };
			sub.IndexData.indexBuffer = ibuf;
			sub.IndexData.indexCount = 6;
			sub.IndexData.indexStart = 0;
			ibuf.SetData( faces, 0, ibuf.Length, true );

			mesh.BoundingBox = new AxisAlignedBox( new Vector3( -100, -100, 0 ), new Vector3( 100, 100, 0 ) );
			mesh.BoundingSphereRadius = Utility.Sqrt( 100 * 100 + 100 * 100 );
		}

		/// <summary>
		/// Creates a 100x100x100 cube as a submesh of the given mesh
		/// </summary>
		private static void _createCube( Mesh mesh )
		{
			SubMesh sub = mesh.CreateSubMesh();

			const int NUM_VERTICES = 4 * 6; // 4 vertices per side * 6 sides
			const int NUM_ENTRIES_PER_VERTEX = 8;
			const int NUM_VERTEX_ENTRIES = NUM_VERTICES * NUM_ENTRIES_PER_VERTEX;
			const int NUM_INDICES = 3 * 2 * 6; // 3 indices per face * 2 faces per side * 6 sides

			const float CUBE_SIZE = 100.0f;
			const float CUBE_HALF_SIZE = CUBE_SIZE / 2.0f;

			// Create 4 vertices per side instead of 6 that are shared for the whole cube.
			// The reason for this is with only 6 vertices the normals will look bad
			// since each vertex can "point" in a different direction depending on the face it is included in.
			float[] vertices = new float[ NUM_VERTEX_ENTRIES ] {
				// front side
				-CUBE_HALF_SIZE, -CUBE_HALF_SIZE, CUBE_HALF_SIZE,	// pos
				0,0,1,	// normal
				0,1,	// texcoord
				CUBE_HALF_SIZE, -CUBE_HALF_SIZE, CUBE_HALF_SIZE,
				0,0,1,
				1,1,
				CUBE_HALF_SIZE,  CUBE_HALF_SIZE, CUBE_HALF_SIZE,
				0,0,1,
				1,0,
				-CUBE_HALF_SIZE,  CUBE_HALF_SIZE, CUBE_HALF_SIZE ,
				0,0,1,
				0,0,

				// back side
				CUBE_HALF_SIZE, -CUBE_HALF_SIZE, -CUBE_HALF_SIZE,
				0,0,-1,
				0,1,
				-CUBE_HALF_SIZE, -CUBE_HALF_SIZE, -CUBE_HALF_SIZE,
				0,0,-1,
				1,1,
				-CUBE_HALF_SIZE, CUBE_HALF_SIZE, -CUBE_HALF_SIZE,
				0,0,-1,
				1,0,
				CUBE_HALF_SIZE, CUBE_HALF_SIZE, -CUBE_HALF_SIZE,
				0,0,-1,
				0,0,

				// left side
				-CUBE_HALF_SIZE, -CUBE_HALF_SIZE, -CUBE_HALF_SIZE,
				-1,0,0,
				0,1,
				-CUBE_HALF_SIZE, -CUBE_HALF_SIZE, CUBE_HALF_SIZE,
				-1,0,0,
				1,1,
				-CUBE_HALF_SIZE, CUBE_HALF_SIZE, CUBE_HALF_SIZE,
				-1,0,0,
				1,0,
				-CUBE_HALF_SIZE, CUBE_HALF_SIZE, -CUBE_HALF_SIZE,
				-1,0,0,
				0,0,

				// right side
				CUBE_HALF_SIZE, -CUBE_HALF_SIZE, CUBE_HALF_SIZE,
				1,0,0,
				0,1,
				CUBE_HALF_SIZE, -CUBE_HALF_SIZE, -CUBE_HALF_SIZE,
				1,0,0,
				1,1,
				CUBE_HALF_SIZE, CUBE_HALF_SIZE, -CUBE_HALF_SIZE,
				1,0,0,
				1,0,
				CUBE_HALF_SIZE, CUBE_HALF_SIZE, CUBE_HALF_SIZE,
				1,0,0,
				0,0,

				// up side
				-CUBE_HALF_SIZE, CUBE_HALF_SIZE, CUBE_HALF_SIZE,
				0,1,0,
				0,1,
				CUBE_HALF_SIZE, CUBE_HALF_SIZE, CUBE_HALF_SIZE,
				0,1,0,
				1,1,
				CUBE_HALF_SIZE, CUBE_HALF_SIZE, -CUBE_HALF_SIZE,
				0,1,0,
				1,0,
				-CUBE_HALF_SIZE, CUBE_HALF_SIZE, -CUBE_HALF_SIZE,
				0,1,0,
				0,0,

				// down side
				-CUBE_HALF_SIZE, -CUBE_HALF_SIZE, -CUBE_HALF_SIZE,
				0,-1,0,
				0,1,
				CUBE_HALF_SIZE, -CUBE_HALF_SIZE, -CUBE_HALF_SIZE,
				0,-1,0,
				1,1,
				CUBE_HALF_SIZE, -CUBE_HALF_SIZE, CUBE_HALF_SIZE,
				0,-1,0,
				1,0,
				-CUBE_HALF_SIZE, -CUBE_HALF_SIZE, CUBE_HALF_SIZE,
				0,-1,0,
				0,0
			};

			mesh.SharedVertexData = new VertexData();
			mesh.SharedVertexData.vertexCount = NUM_VERTICES;
			VertexDeclaration decl = mesh.SharedVertexData.vertexDeclaration;
			VertexBufferBinding bind = mesh.SharedVertexData.vertexBufferBinding;

			int offset = 0;
			decl.AddElement( 0, offset, VertexElementType.Float3, VertexElementSemantic.Position );
			offset += VertexElement.GetTypeSize( VertexElementType.Float3 );
			decl.AddElement( 0, offset, VertexElementType.Float3, VertexElementSemantic.Normal );
			offset += VertexElement.GetTypeSize( VertexElementType.Float3 );
			decl.AddElement( 0, offset, VertexElementType.Float2, VertexElementSemantic.TexCoords, 0 );
			offset += VertexElement.GetTypeSize( VertexElementType.Float2 );

			HardwareVertexBuffer vbuf = HardwareBufferManager.Instance.CreateVertexBuffer( decl, NUM_VERTICES, BufferUsage.StaticWriteOnly );
			bind.SetBinding( 0, vbuf );

			vbuf.SetData( vertices, 0, vbuf.Length, true );

			sub.useSharedVertices = true;
			HardwareIndexBuffer ibuf = HardwareBufferManager.Instance.CreateIndexBuffer( IndexType.Size16, NUM_INDICES, BufferUsage.StaticWriteOnly );

			short[] faces = new short[ NUM_INDICES ] {
				// front
				0,1,2,
				0,2,3,

				// back
				4,5,6,
				4,6,7,

				// left
				8,9,10,
				8,10,11,

				// right
				12,13,14,
				12,14,15,

				// up
				16,17,18,
				16,18,19,

				// down
				20,21,22,
				20,22,23
			};

			sub.IndexData.indexBuffer = ibuf;
			sub.IndexData.indexCount = NUM_INDICES;
			sub.IndexData.indexStart = 0;
			ibuf.SetData( faces, 0, ibuf.Length, true );

			mesh.BoundingBox = new AxisAlignedBox( new Vector3( -CUBE_HALF_SIZE, -CUBE_HALF_SIZE, -CUBE_HALF_SIZE ),
												   new Vector3( CUBE_HALF_SIZE, CUBE_HALF_SIZE, CUBE_HALF_SIZE ) );

			mesh.BoundingSphereRadius = CUBE_HALF_SIZE;

		}

		/// <summary>
		/// Creates a sphere with a diameter of 100 units as a submesh of the given mesh
		/// </summary>
		private static void _createSphere( Mesh mesh )
		{
			// sphere creation code taken from the DeferredShading sample, originally from the [Ogre] wiki
			SubMesh pSphereVertex = mesh.CreateSubMesh();

			const int NUM_SEGMENTS = 16;
			const int NUM_RINGS = 16;
			const float SPHERE_RADIUS = 50.0f;

			mesh.SharedVertexData = new VertexData();
			VertexData vertexData = mesh.SharedVertexData;

			// define the vertex format
			VertexDeclaration vertexDecl = vertexData.vertexDeclaration;
			int offset = 0;
			// positions
			vertexDecl.AddElement( 0, offset, VertexElementType.Float3, VertexElementSemantic.Position );
			offset += VertexElement.GetTypeSize( VertexElementType.Float3 );
			// normals
			vertexDecl.AddElement( 0, offset, VertexElementType.Float3, VertexElementSemantic.Normal );
			offset += VertexElement.GetTypeSize( VertexElementType.Float3 );
			// two dimensional texture coordinates
			vertexDecl.AddElement( 0, offset, VertexElementType.Float2, VertexElementSemantic.TexCoords, 0 );
			offset += VertexElement.GetTypeSize( VertexElementType.Float2 );

			// allocate the vertex buffer
			vertexData.vertexCount = ( NUM_RINGS + 1 ) * ( NUM_SEGMENTS + 1 );

			HardwareVertexBuffer vBuf = HardwareBufferManager.Instance.CreateVertexBuffer( vertexDecl.Clone( 0 ), vertexData.vertexCount, BufferUsage.StaticWriteOnly, false );

			VertexBufferBinding binding = vertexData.vertexBufferBinding;
			binding.SetBinding( 0, vBuf );

			// allocate index buffer
			pSphereVertex.IndexData.indexCount = 6 * NUM_RINGS * ( NUM_SEGMENTS + 1 );

			pSphereVertex.IndexData.indexBuffer = HardwareBufferManager.Instance.CreateIndexBuffer( IndexType.Size16, pSphereVertex.IndexData.indexCount, BufferUsage.StaticWriteOnly, false );
			HardwareIndexBuffer iBuf = pSphereVertex.IndexData.indexBuffer;

			float[] pVertex = new float[ vBuf.Length / sizeof( float ) ];
			vBuf.GetData( pVertex );

			ushort[] pIndices = new ushort[ iBuf.Length / sizeof( ushort ) ];
			iBuf.GetData( pIndices );

			float fDeltaRingAngle = ( Utility.PI / NUM_RINGS );
			float fDeltaSegAngle = ( 2 * Utility.PI / NUM_SEGMENTS );
			ushort wVerticeIndex = 0;

			// Generate the group of rings for the sphere
			for ( int ring = 0; ring <= NUM_RINGS; ring++ )
			{
				float r0 = SPHERE_RADIUS * Utility.Sin( ring * fDeltaRingAngle );
				float y0 = SPHERE_RADIUS * Utility.Cos( ring * fDeltaRingAngle );

				// Generate the group of segments for the current ring
				for ( int seg = 0; seg <= NUM_SEGMENTS; seg++ )
				{
					float x0 = r0 * Utility.Sin( seg * fDeltaSegAngle );
					float z0 = r0 * Utility.Cos( seg * fDeltaSegAngle );

					// Add one vertex to the strip which makes up the sphere
					pVertex[ seg * 8 ] = x0;
					pVertex[ seg * 8 + 1 ] = y0;
					pVertex[ seg * 8 + 2 ] = z0;

					Vector3 vNormal = new Vector3( x0, y0, z0 );
					vNormal.Normalize();

					pVertex[ seg * 8 + 3 ] = vNormal.x;
					pVertex[ seg * 8 + 4 ] = vNormal.y;
					pVertex[ seg * 8 + 5 ] = vNormal.z;

					pVertex[ seg * 8 + 6 ] = (float)seg / (float)NUM_SEGMENTS;
					pVertex[ seg * 8 + 7 ] = (float)ring / (float)NUM_RINGS;

					if ( ring != NUM_RINGS )
					{
						// each vertex (except the last) has six indicies pointing to it
						pIndices[ seg * 6 ] = (ushort)( wVerticeIndex + NUM_SEGMENTS + 1 );
						pIndices[ seg * 6 + 1 ] = (ushort)( wVerticeIndex );
						pIndices[ seg * 6 + 2 ] = (ushort)( wVerticeIndex + NUM_SEGMENTS );
						pIndices[ seg * 6 + 3 ] = (ushort)( wVerticeIndex + NUM_SEGMENTS + 1 );
						pIndices[ seg * 6 + 4 ] = (ushort)( wVerticeIndex + 1 );
						pIndices[ seg * 6 + 5 ] = (ushort)( wVerticeIndex );
						wVerticeIndex++;
					}
				}; // end for seg
			} // end for ring

			vBuf.SetData( pVertex );
			iBuf.SetData( pIndices );

			// Generate face list
			pSphereVertex.useSharedVertices = true;

			// the original code was missing this line:
			mesh.BoundingBox = new AxisAlignedBox( new Vector3( -SPHERE_RADIUS, -SPHERE_RADIUS, -SPHERE_RADIUS ),
												   new Vector3( SPHERE_RADIUS, SPHERE_RADIUS, SPHERE_RADIUS ) );

			mesh.BoundingSphereRadius = SPHERE_RADIUS;
		}

	}
}