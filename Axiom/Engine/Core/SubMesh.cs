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
using Axiom.SubSystems.Rendering;

namespace Axiom.Core
{
	/// <summary>
	///		Defines a part of a complete 3D mesh.
	/// </summary>
	/// <remarks>
	///		Models which make up the definition of a discrete 3D object
	///		are made up of potentially multiple parts. This is because
	///		different parts of the mesh may use different materials or
	///		use different vertex formats, such that a rendering state
	///		change is required between them.
	///		<p/>
	///		Like the Mesh class, instatiations of 3D objects in the scene
	///		share the SubMesh instances, and have the option of overriding
	///		their material differences on a per-object basis if required.
	///		See the SubEntity class for more information.
	/// </remarks>
	public class SubMesh
	{
		#region Member variables

		/// <summary>The parent mesh that this subMesh belongs to.</summary>
		protected Mesh parent;
		/// <summary>Name of the material assigned to this subMesh.</summary>
		protected String materialName;
		/// <summary>Name of this SubMesh.</summary>
		protected String name;
		/// <summary>Indicates if this submesh shares vertex data with other meshes or whether it has it's own vertices.</summary>
		// TODO: Is this the right answer?
		protected internal bool useSharedVertices;
		/// <summary></summary>
		protected bool isMaterialInitialized;
		/// <summary>Number of faces in this subMesh.</summary>
		protected internal short numFaces;
		/// <summary>Indices to use for parent geometry when using shared vertices.</summary>
		protected internal short[] faceIndices;
		
		public VertexData vertexData;
		public IndexData indexData = new IndexData();

		#endregion
		
		#region Constructor

		/// <summary>
		///		Basic contructor.
		/// </summary>
		/// <param name="name"></param>
		public SubMesh(String name)
		{
			this.name = name;

			useSharedVertices = true;
		}

		#endregion

		#region Properties

		/// <summary>
		///		Gets/Sets the name of the material this SubMesh will be using.
		/// </summary>
		public String MaterialName
		{
			get { return materialName; }
			set { materialName = value; isMaterialInitialized = true; }
		}

		/// <summary>
		///		Gets/Sets the parent mode of this SubMesh.
		/// </summary>
		public Mesh Parent
		{
			get { return parent; }
			set { parent = value; }
		}

		/// <summary>
		///		Overloaded method.
		/// </summary>
		/// <param name="op"></param>
		/// <returns></returns>
		public void GetRenderOperation(RenderOperation op)
		{
			// call overloaded method with lod index of 0 by default
			GetRenderOperation(op, 0);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="op"></param>
		/// <param name="lodIndex"></param>
		/// <returns></returns>
		/// DOC
		public void GetRenderOperation(RenderOperation op, int lodIndex)
		{
			// meshes always use indices
			op.useIndices = true;

			if(lodIndex > 0)
			{
				// TODO: Use LOD index list
			}
			else
				op.indexData = indexData;
			
			// indexed meshes use tri lists
			op.operationType = RenderMode.TriangleList;

			// set the vertex data correctly
			op.vertexData = useSharedVertices? parent.SharedVertexData : vertexData;
		}

		/// <summary>
		///		Gets whether or not a material has been set for this subMesh.
		/// </summary>
		public bool IsMaterialInitialized
		{
			get { return isMaterialInitialized; }
		}

		#endregion
	}
}
