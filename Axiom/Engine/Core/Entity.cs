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
using System.Collections;
using Axiom.Collections;
using Axiom.SubSystems.Rendering;
using Matrix4 = Axiom.MathLib.Matrix4;
using Vector3 = Axiom.MathLib.Vector3;

namespace Axiom.Core
{
	/// <summary>
	/// The Entity class serves as the base class for all objects in the engine.   
	/// It represents the minimum functionality required for an object in a 3D SceneGraph.
	/// </summary>
	// TODO: Add LOD and skeletal animation
	public class Entity : SceneObject, IDisposable
	{
		#region Member variables

		/// <summary>3D Mesh that represents this entity</summary>
		protected Mesh mesh;
		/// <summary>List of sub entities.</summary>
		protected SubEntityCollection subEntityList = new SubEntityCollection();
		/// <summary>SceneManager responsible for creating this entity.</summary>
		protected SceneManager sceneMgr;

		protected string materialName;

		protected int meshLODIndex;

		#endregion

		#region Constructors

		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <param name="mesh"></param>
		/// <param name="creator"></param>
		public Entity(String name, Mesh mesh, SceneManager creator)
		{
			this.name = name;
			this.mesh = mesh;
			this.sceneMgr = creator;

			BuildSubEntities();

			// TODO: Determine LOD usage
		}

		#endregion

		#region Properties
		
		/// <summary>
		/// 
		/// </summary>
		/// DOC
		public int MeshLODIndex
		{
			get { return meshLODIndex; }
			set { meshLODIndex = value; }
		}

		/// <summary>
		///		Gets the 3D mesh associated with this entity.
		/// </summary>
		public Mesh Mesh
		{
			get { return mesh; }
		}

		/// <summary>
		///		Gets the collection of sub entities belonging to this entity.
		/// </summary>
		public SubEntityCollection SubEntities
		{
			get { return subEntityList; }
		}

		/// <summary>
		/// 
		/// </summary>
		public String MaterialName
		{
			set
			{
				materialName = value;

				// assign the material name to all sub entities
				for(int i = 0; i < subEntityList.Count; i++)
					subEntityList[i].MaterialName = materialName;

			}
		}

		#endregion

		#region Private methods

		/// <summary>
		///		Used to build a list of sub-entities from the meshes located in the mesh.
		/// </summary>
		public void BuildSubEntities()
		{
			// loop through the models meshes and create sub entities from them
			for(int i = 0; i < mesh.SubMeshes.Count; i++)
			{
				SubMesh subMesh = mesh.SubMeshes[i];
				SubEntity sub = new SubEntity();
				sub.Parent = this;
				sub.SubMesh = subMesh;
				
				if(subMesh.IsMaterialInitialized)
					sub.MaterialName = subMesh.MaterialName;

				subEntityList.Add(sub);
			}
		}
			
		#endregion

		#region Implementation of IDisposable

		/// <summary>
		///		
		/// </summary>
		public void Dispose()
		{
		}

		#endregion

		#region Implementation of SceneObject

		internal override void NotifyCurrentCamera(Axiom.Core.Camera camera)
		{
			// TODO: Use camera to determine updated LOD info
		}

		/// <summary>
		///		
		/// </summary>
		/// <param name="queue"></param>
		internal override void UpdateRenderQueue(RenderQueue queue)
		{
			// add all sub entities to the render queue
			for(int i = 0; i < subEntityList.Count; i++)
				queue.AddRenderable(subEntityList[i], RenderQueue.DEFAULT_PRIORITY, renderQueueID);
		}

		public override Axiom.Core.AxisAlignedBox BoundingBox
		{
			// return the bounding box of our mesh
			get {	 return mesh.BoundingBox; }
		}

		#endregion

		#region ICloneable Members

		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public Entity Clone(string name)
		{
			// create a new entity using the current mesh (uses same instance, not a copy for speed)
			Entity clone = sceneMgr.CreateEntity(name, mesh.Name);

			// loop through each subentity and set the material up for the clone
			for(int i = 0; i < subEntityList.Count; i++)
			{
				SubEntity subEntity = subEntityList[i];
				clone.SubEntities[i].MaterialName = materialName;
			}

			return clone;
		}

		#endregion
	}
}