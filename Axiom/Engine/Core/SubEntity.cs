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
using System.Diagnostics;
using Axiom.Enumerations;
using Axiom.MathLib;
using Axiom.SubSystems.Rendering;

namespace Axiom.Core
{
	/// <summary>
	///		Utility class which defines the sub-parts of an Entity.
	/// </summary>
	/// <remarks>
	///		Just as models are split into meshes, an Entity is made up of
	///		potentially multiple SubEntities. These are mainly here to provide the
	///		link between the Material which the SubEntity uses (which may be the
	///		default Material for the SubMesh or may have been changed for this
	///		object) and the SubMesh data.
	///		<p/>
	///		SubEntity instances are never created manually. They are created at
	///		the same time as their parent Entity by the SceneManager method
	///		CreateEntity.
	/// </remarks>
	public class SubEntity : IRenderable
	{
		#region Member variables

		/// <summary>Reference to the parent Entity.</summary>
		private Entity parent;
		/// <summary>Name of the material being used.</summary>
		private String materialName;
		/// <summary>Reference to the material being used by this SubEntity.</summary>
		private Material material;
		/// <summary>Reference to the subMesh that represents the geometry for this SubEntity.</summary>
		private SubMesh subMesh;
		/// <summary></summary>
		private SceneDetailLevel renderDetail;

		#endregion

		#region Constructor

		/// <summary>
		///		Internal constructor, only allows creation of SubEntities within the engine core.
		/// </summary>
		internal SubEntity()
		{
			renderDetail = SceneDetailLevel.Solid;
		}

		#endregion

		#region Properties

		/// <summary>
		///		Gets/Sets the name of the material used for this SubEntity.
		/// </summary>
		public String MaterialName
		{
			get { return materialName; }
			// TODO: Implement setter on MaterialName to load material from material manager.
			set 
			{ 
				materialName = value; 

				// load the material from the material manager (it should already exist
				material = (Material)MaterialManager.Instance[materialName];

				if(material == null)
					throw new Axiom.Exceptions.AxiomException(String.Format("Cannot assign material '{0}' to SubEntity '{1}' because the material doesn't exist.", materialName, parent.Name));

				// ensure the material is loaded.  It will skip it if it already is
				material.Load();
			}
		}

		/// <summary>
		///		Gets/Sets the subMesh to be used for rendering this SubEntity.
		/// </summary>
		public SubMesh SubMesh
		{
			get { return subMesh; }
			set { subMesh = value; }
		}

		/// <summary>
		///		Gets/Sets the parent entity of this SubEntity.
		/// </summary>
		public Entity Parent
		{
			get { return parent; }
			set { parent = value; }
		}

		#endregion

		#region IRenderable Members

		/// <summary>
		///		Gets/Sets a reference to the material being used by this SubEntity.
		/// </summary>
		/// <remarks>
		///		By default, the SubEntity will use the material defined by the SubMesh.  However,
		///		this can be overridden by the SubEntity in the case where several entities use the
		///		same SubMesh instance, but want to shade it different.
		/// </remarks>
		public Material Material
		{
			get { return material; }
			set { material = value; }
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="op"></param>
		/// DOC
		public void GetRenderOperation(RenderOperation op)
		{
			subMesh.GetRenderOperation(op, parent.MeshLODIndex);
		}

		Material IRenderable.Material
		{
			get { return material; }
		}

		public Axiom.MathLib.Matrix4[] WorldTransforms
		{
			get
			{
				return new Matrix4[] { parent.ParentNode.FullTransform };
			}
		}

		public ushort NumWorldTransforms
		{
			get
			{
				// TODO:  Add SubEntity.NumWorldTransforms getter implementation
				return 1;
			}
		}

		public bool UseIdentityProjection
		{
			get
			{
				// TODO:  Add SubEntity.UseIdentityProjection getter implementation
				return false;
			}
		}

		public bool UseIdentityView
		{
			get
			{
				// TODO:  Add SubEntity.UseIdentityView getter implementation
				return false;
			}
		}

		public SceneDetailLevel RenderDetail
		{
			get { return SceneDetailLevel.Solid;	}
		}

		public float GetSquaredViewDepth(Camera camera)
		{
			// get the parent entitie's parent node
			Node node = parent.ParentNode;

			Debug.Assert(node != null);

			return node.GetSquaredViewDepth(camera);
		}

		#endregion
	}
}
