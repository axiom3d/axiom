#region LGPL License
/*
Axiom Game Engine Library
Copyright (C) 2003  Axiom Project Team

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
using Axiom.Enumerations;
using Axiom.MathLib;
using Axiom.SubSystems.Rendering;

namespace Axiom.Core
{
	/// <summary>
	/// Summary description for MeshManager.
	/// </summary>
	public class MeshManager : ResourceManager
	{
		#region Singleton implementation

		static MeshManager() { Init(); }
		protected MeshManager() {}
		protected static MeshManager instance;

		public static MeshManager Instance
		{
			get { return instance; }
		}

		public static void Init()
		{
			instance = new MeshManager();
		}
		
		#endregion

		/// <summary>
		///	
		/// </summary>
		public void Initialize()
		{
			// TODO: Setup Prefab nodes here
		}
	
		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public override Resource Create(string name)
		{
			return new Mesh(name);
		}

		/// <summary>
		///		Creates a barebones Mesh object that can be used to manually define geometry later on.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public Mesh CreateManual(String name)
		{
			Mesh mesh = (Mesh)MeshManager.Instance[name];

			if(mesh == null)
			{
				mesh = (Mesh)Create(name);
				mesh.ManuallyDefined = true;
				base.Load(mesh, 0);
			}

			return mesh;
		}

		/// <summary>
		///		Overloaded method.
		/// </summary>
		/// <param name="name">Name of the plane mesh.</param>
		/// <param name="plane">Plane to use for distance and orientation of the mesh.</param>
		/// <param name="width">Width in world coordinates.</param>
		/// <param name="height">Height in world coordinates.</param>
		/// <returns></returns>
		public Mesh CreatePlane(String name, Plane plane, int width, int height)
		{
			return CreatePlane(name, plane, width, height, 1, 1, true, 1, 1.0f, 1.0f, Vector3.UnitY, BufferUsage.StaticWriteOnly, BufferUsage.StaticWriteOnly, false, false);
		}

		public Mesh CreatePlane(String name, Plane plane, float width, float height, int xSegments, int ySegments, bool normals, int numTexCoordSets, float uTile, float vTile, Vector3 upVec)
		{
			return CreatePlane(name, plane, width, height, xSegments, ySegments, normals, numTexCoordSets, uTile, vTile, upVec, BufferUsage.StaticWriteOnly, BufferUsage.StaticWriteOnly, false, false);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="name">Name of the plane mesh.</param>
		/// <param name="plane">Plane to use for distance and orientation of the mesh.</param>
		/// <param name="width">Width in world coordinates.</param>
		/// <param name="height">Height in world coordinates.</param>
		/// <param name="xSegments">Number of x segments for tesselation.</param>
		/// <param name="ySegments">Number of y segments for tesselation.</param>
		/// <param name="normals">If true, plane normals are created.</param>
		/// <param name="numTexCoordSets">Number of 2d texture coord sets to use.</param>
		/// <param name="uTile">Number of times the texture should be repeated in the u direction.</param>
		/// <param name="vTile">Number of times the texture should be repeated in the v direction.</param>
		/// <param name="upVec">The up direction of the plane.</param>
		/// <returns></returns>
		/// DOC: Add new params
		public Mesh CreatePlane(String name, Plane plane, float width, float height, int xSegments, int ySegments, bool normals, int numTexCoordSets, float uTile, float vTile, Vector3 upVec,
			BufferUsage vertexBufferUsage, BufferUsage indexBufferUsage, bool vertexShadowBuffer, bool indexShadowBuffer )
		{
			Mesh mesh = CreateManual(name);
			SubMesh subMesh = mesh.CreateSubMesh(name + "SubMesh");

			mesh.SharedVertexData = new VertexData();
			VertexData vertexData = mesh.SharedVertexData;

			VertexDeclaration decl = vertexData.vertexDeclaration;
			int currOffset = 0;

			// add position data
			decl.AddElement(new VertexElement(0, currOffset, VertexElementType.Float3, VertexElementSemantic.Position));
			currOffset += VertexElement.GetTypeSize(VertexElementType.Float3);

			// normals are optional
			if(normals)
			{
				decl.AddElement(new VertexElement(0, currOffset, VertexElementType.Float3, VertexElementSemantic.Normal));
				currOffset += VertexElement.GetTypeSize(VertexElementType.Float3);
			}

			// add texture coords
			for(ushort i = 0; i < numTexCoordSets; i++)
			{
				decl.AddElement(new VertexElement(0, currOffset, VertexElementType.Float2, VertexElementSemantic.TexCoords, i));
				currOffset += VertexElement.GetTypeSize(VertexElementType.Float2);
			}

			vertexData.vertexCount = (xSegments + 1) * (ySegments + 1);

			// create a new vertex buffer (based on current API)
			HardwareVertexBuffer vbuf = 
				HardwareBufferManager.Instance.CreateVertexBuffer(decl.GetVertexSize(0), vertexData.vertexCount, vertexBufferUsage, vertexShadowBuffer);
			
			// get a reference to the vertex buffer binding
			VertexBufferBinding binding = vertexData.vertexBufferBinding;

			// bind the first vertex buffer
			binding.SetBinding(0, vbuf);

			// transform the plane based on its plane def
			Matrix4 translate = Matrix4.Identity;
			Matrix4 transform = Matrix4.Zero;
			Matrix4 rotation = Matrix4.Identity;
			Matrix3 rot3x3 = Matrix3.Zero;

			Vector3 xAxis, yAxis, zAxis;
			zAxis = plane.Normal;
			zAxis.Normalize();
			yAxis = upVec;
			yAxis.Normalize();
			xAxis = yAxis.Cross(zAxis);

			if(xAxis.Length == 0)
				throw new Axiom.Exceptions.AxiomException("The up vector for a plane cannot be parallel to the planes normal.");

			rot3x3.FromAxes(xAxis, yAxis, zAxis);
			rotation = rot3x3;

			// set up transform from origin
			translate.Translation = plane.Normal * -plane.D;

			transform = translate * rotation;

			float xSpace = width / xSegments;
			float ySpace = height / ySegments;
			float halfWidth = width / 2;
			float halfHeight = height / 2;
			float xTexCoord = (1.0f * uTile) / xSegments;
			float yTexCoord = (1.0f * vTile) / ySegments;
			Vector3 vec = Vector3.Zero;
			Vector3 min = Vector3.Zero;
			Vector3 max = Vector3.Zero;
			float maxSquaredLength = 0;
			bool firstTime = true;

			// generate vertex data
			unsafe
			{
				// lock the vertex buffer
				IntPtr data = vbuf.Lock(0, vbuf.Size, BufferLocking.Discard);

				float* pData = (float*)data.ToPointer();

				for(int y = 0; y <= ySegments; y++)
				{
					for(int x = 0; x <= xSegments; x++)
					{
						int index = (((y * (xSegments + 1)) + x) * 3);
						// centered on origin
						vec.x = (x * xSpace) - halfWidth;
						vec.y = (y * ySpace) - halfHeight;
						vec.z = 0.0f;

						vec = transform * vec;

						*pData++ = vec.x;
						*pData++ = vec.y;
						*pData++ = vec.z;

						// Build bounds as we go
						if (firstTime)
						{
							min = vec;
							max = vec;
							maxSquaredLength = vec.LengthSquared;
							firstTime = false;
						}
						else
						{
							min.Floor(vec);
							max.Ceil(vec);
							maxSquaredLength = MathUtil.Max(maxSquaredLength, vec.LengthSquared);
						}

						if(normals)
						{
							vec = Vector3.UnitZ;
							vec = rotation * vec;

							*pData++ = vec.x;
							*pData++ = vec.y;
							*pData++ = vec.z;
						}

						for(int i = 0; i < numTexCoordSets; i++)
						{
							*pData++ = x * xTexCoord;
							*pData++ = y * yTexCoord;
						} // for texCoords
					} // for x
				} // for y

				// unlock the buffer
				vbuf.Unlock();

				subMesh.useSharedVertices = true;

			} // unsafe

			// generate face list
			Tesselate2DMesh(subMesh, xSegments + 1, ySegments + 1, false, indexBufferUsage, indexShadowBuffer);

			// generate bounds for the mesh
			mesh.BoundingBox = new AxisAlignedBox(min, max);
			mesh.BoundingSphereRadius = MathUtil.Sqrt(maxSquaredLength);

			return mesh;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <param name="priority"></param>
		// TODO: Indexer?
		public Mesh Load(string name, int priority)
		{
			Mesh mesh = null;

			// if the resource isn't cached, create it
			if(!resourceList.ContainsKey(name))
			{
				mesh = (Mesh)Create(name);
				base.Load(mesh, priority);
			}
			else
			{
				// get the cached version
				mesh = (Mesh)resourceList[name];
			}

			return mesh;
		}

		/// <summary>
		///		Used to generate a face list based on vertices.
		/// </summary>
		/// <param name="subMesh"></param>
		/// <param name="xSegments"></param>
		/// <param name="ySegments"></param>
		/// <param name="doubleSided"></param>
		private void Tesselate2DMesh(SubMesh subMesh, int width, int height, bool doubleSided, BufferUsage indexBufferUsage, bool indexShadowBuffer)
		{
			int vInc, uInc, v, u, iterations;
			int vCount, uCount;

			vInc = 1;
			v = 0;

			iterations = doubleSided ? 2 : 1;

			// setup index count
			subMesh.indexData.indexCount = (width - 1) * (height - 1) * 2 * iterations * 3;

			// create the index buffer using the current API
			subMesh.indexData.indexBuffer = 
				HardwareBufferManager.Instance.CreateIndexBuffer(IndexType.Size16, subMesh.indexData.indexCount, indexBufferUsage, indexShadowBuffer);

			short v1, v2, v3;

			// grab a reference for easy access
			HardwareIndexBuffer idxBuffer = subMesh.indexData.indexBuffer;

			// lock the whole index buffer
			IntPtr data = idxBuffer.Lock(0, idxBuffer.Size, BufferLocking.Discard);

			unsafe
			{
				short* pIndex = (short*)data.ToPointer();

				while(0 < iterations--)
				{
					// make tris in a zigzag pattern (strip compatible)
					u = 0;
					uInc = 1;

					vCount = height - 1;

					while(0 < vCount--)
					{
						uCount = width - 1;

						while(0 < uCount--)
						{
							// First Tri in cell
							// -----------------
							v1 = (short)(((v + vInc) * width) + u);
							v2 = (short)((v * width) + u);
							v3 = (short)(((v + vInc) * width) + (u + uInc));
							// Output indexes
							*pIndex++ = v1;
							*pIndex++ = v2;
							*pIndex++ = v3;
							// Second Tri in cell
							// ------------------
							v1 = (short)(((v + vInc) * width) + (u + uInc));
							v2 = (short)((v * width) + u);
							v3 = (short)((v * width) + (u + uInc));
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
					vInc = - vInc;
				} // while iterations
			}// unsafe

			// unlock the buffer
			idxBuffer.Unlock();
		}

	}
}
