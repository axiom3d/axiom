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
using System.IO;
using Axiom.Collections;
using Axiom.MathLib;
using Axiom.Serialization;
using Axiom.SubSystems.Rendering;

namespace Axiom.Core
{
	/// <summary>
	/// Summary description for Mesh.
	/// </summary>
	public class Mesh : Resource
	{
		#region Member variables

		/// <summary>Shared vertex data between multiple meshes.</summary>
		protected VertexData sharedVertexData = new VertexData();
		/// <summary>Collection of sub meshes for this mesh.</summary>
		protected SubMeshCollection subMeshList = new SubMeshCollection();
		/// <summary>Flag that states whether or not the bounding box for this mesh needs to be re-calced.</summary>
		protected bool updateBounds = true;
		/// <summary>Flag that states whether or not this mesh will be loaded from a file, or constructed manually.</summary>
		protected bool manuallyDefined = false;
		protected AxisAlignedBox boundingBox = AxisAlignedBox.Null;
		protected float boundingSphereRadius;

		// TODO: Make private, add properties
		public BufferUsage vertexBufferUsage;
		public BufferUsage indexBufferUsage;
		public bool vertexShadowBuffer;
		public bool indexShadowBuffer;

		#endregion

		#region Constructors

		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		public Mesh(string name)
		{
			this.name = name;

			vertexBufferUsage = BufferUsage.StaticWriteOnly;
			indexBufferUsage = BufferUsage.StaticWriteOnly;
		}

		#endregion

		#region Properties

		/// <summary>
		/// 
		/// </summary>
		/// DOC
		public float BoundingSphereRadius
		{
			// TODO: Implement BoundingSphereRadius
			get { return boundingSphereRadius; }
			set { boundingSphereRadius = value; }
		}

		/// <summary>
		///		Defines whether this mesh is to be loaded from a resource, or created manually at runtime.
		/// </summary>
		public bool ManuallyDefined
		{
			get { return manuallyDefined; }
			set { manuallyDefined = value; }
		}

		#endregion

		#region Implementation of Resource

		/// <summary>
		///		
		/// </summary>
		public override void Load()
		{
			// unload this first if it is already loaded
			if(isLoaded)
			{
				Unload();
				isLoaded = false;
			}

			// load this bad boy if it is not to be manually defined
			if(!manuallyDefined)
			{
				// get the resource data from MeshManager
				Stream data = MeshManager.Instance.FindResourceData(name);

				// instantiate a mesh reader and pass in the stream data
				//XmlMeshReader meshReader = new XmlMeshReader(data);
				OgreMeshReader meshReader = new OgreMeshReader(data);

				// mesh loading stats
				int before, after;

				// get the tick count before loading the mesh
				before = Environment.TickCount;

				// import the XML mesh
				meshReader.Import(this);
				
				// get the tick count after loading the mesh
				after = Environment.TickCount;

				// record the time elapsed while loading the mesh
				System.Diagnostics.Trace.WriteLine(String.Format("Mesh: Loaded '{0}', took {1}ms", this.name,  (after - before)));

				// close the stream (we don't need to leave it open here)
				data.Close();
			}
	}

		/// <summary>
		///		
		/// </summary>
		public override void Unload()
		{
		
		}

		/// <summary>
		///		
		/// </summary>
		public override void Dispose()
		{
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public SubMesh CreateSubMesh(String name)
		{
			SubMesh subMesh = new SubMesh(name);

			// set the parent of the subMesh to us
			subMesh.Parent = this;

			// add to the list of child meshes
			subMeshList.Add(subMesh);

			return subMesh;
		}

		/// <summary>
		///		Gets/Sets the shared VertexData for this mesh.
		/// </summary>
		public VertexData SharedVertexData
		{
			get { return sharedVertexData; }
			set { sharedVertexData = value; }
		}

		// Gets the collection of meshes within this mesh.
		public SubMeshCollection SubMeshes
		{
			get { return subMeshList; }
		}

		public AxisAlignedBox BoundingBox
		{
			get
			{
				//if(updateBounds)
				//	UpdateBounds();

				// OPTIMIZE: Cloning to prevent direct modification
				return (AxisAlignedBox)boundingBox.Clone();
			}
			set
			{
				boundingBox = value;

				float sqLen1 = boundingBox.Minimum.LengthSquared;
				float sqLen2 = boundingBox.Maximum.LengthSquared;

				// update the bounding sphere radius as well
				boundingSphereRadius = MathUtil.Sqrt(MathUtil.Max(sqLen1, sqLen2));
			}
		}

		/*internal void UpdateBounds()
		{
			Vector3 min = new Vector3();
			Vector3 max = new Vector3();

			bool first = true;
			bool useShared = false;
			int vert = 0;

			// loop through sub meshes and get their bound info
			for(int i = 0; i < meshList.Count; i++)
			{
				SubMesh subMesh = meshList[i];

				if(subMesh.useSharedVertices)
				{
					// skip this step and move on to use the shared vertex buffer
					useShared = true;
				}
				else
				{
					for (vert = 0; vert < subMesh.vertexBuffer.numVertices * 3; vert += (3 + subMesh.vertexBuffer.vertexStride))
					{
						if (first || mesh.vertexBuffer.vertices[vert] < min.x)
						{
							min.x = mesh.vertexBuffer.vertices[vert];
						}
						if (first || mesh.vertexBuffer.vertices[vert+1] < min.y)
						{
							min.y = mesh.vertexBuffer.vertices[vert+1];
						}
						if (first || mesh.vertexBuffer.vertices[vert+2] < min.z)
						{
							min.z = mesh.vertexBuffer.vertices[vert+2];
						}
						if (first || mesh.vertexBuffer.vertices[vert] > max.x)
						{
							max.x = mesh.vertexBuffer.vertices[vert];
						}
						if (first || mesh.vertexBuffer.vertices[vert+1] > max.y)
						{
							max.y = mesh.vertexBuffer.vertices[vert+1];
						}
						if (first || mesh.vertexBuffer.vertices[vert+2] > max.z)
						{
							max.z = mesh.vertexBuffer.vertices[vert+2];
						}

						first = false;
					} // end for
				} // end if

				if(useShared)
				{
					for (vert = 0; vert < sharedBuffer.numVertices * 3; vert += (3 + sharedBuffer.vertexStride))
					{
						if (first || sharedBuffer.vertices[vert] < min.x)
						{
							min.x = sharedBuffer.vertices[vert];
						}
						if (first || sharedBuffer.vertices[vert + 1] < min.y)
						{
							min.y = sharedBuffer.vertices[vert + 1];
						}
						if (first || sharedBuffer.vertices[vert + 2] < min.z)
						{
							min.z = sharedBuffer.vertices[vert + 2];
						}
						if (first || sharedBuffer.vertices[vert] > max.x)
						{
							max.x = sharedBuffer.vertices[vert];
						}
						if (first || sharedBuffer.vertices[vert + 1] > max.y)
						{
							max.y = sharedBuffer.vertices[vert + 1];
						}
						if (first || sharedBuffer.vertices[vert + 2] > max.z)
						{
							max.z = sharedBuffer.vertices[vert + 2];
						}

						first = false;
					}
				}
			} // end for

			// set the extents of the bounding box
			boundingBox.SetExtents(min, max);
			updateBounds = false;
		} */

		/// <summary>
		/// 
		/// </summary>
		/// <param name="usage"></param>
		/// <param name="useShadowBuffer"></param>
		/// DOC
		public void SetVertexBufferPolicy(BufferUsage usage, bool useShadowBuffer)
		{
			vertexBufferUsage = usage;
			vertexShadowBuffer = useShadowBuffer;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="usage"></param>
		/// <param name="useShadowBuffer"></param>
		/// DOC
		public void SetIndexBufferPolicy(BufferUsage usage, bool useShadowBuffer)
		{
			indexBufferUsage = usage;
			indexShadowBuffer = useShadowBuffer;
		}

		#endregion
	}
}
