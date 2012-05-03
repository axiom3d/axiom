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

using Axiom.Animating;
using Axiom.Graphics;

#endregion Namespace Declarations

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
	public class SubMesh : DisposableObject
	{
		#region Member variables

		/// <summary>The parent mesh that this subMesh belongs to.</summary>
		protected Mesh parent;

		/// <summary>Name of the material assigned to this subMesh.</summary>
		protected string materialName;

		/// <summary>Name of this SubMesh.</summary>
		internal string name;

		/// <summary></summary>
		protected bool isMaterialInitialized;

		/// <summary>List of bone assignment for this mesh.</summary>
		protected Dictionary<int, List<VertexBoneAssignment>> boneAssignmentList = new Dictionary<int, List<VertexBoneAssignment>>();

		/// <summary>Flag indicating that bone assignments need to be recompiled.</summary>
		protected internal bool boneAssignmentsOutOfDate;

		/// <summary>Mode used for rendering this submesh.</summary>
		protected internal Axiom.Graphics.OperationType operationType;

		public VertexData vertexData;
		public IndexData indexData = new IndexData();

		/// <summary>Indicates if this submesh shares vertex data with other meshes or whether it has it's own vertices.</summary>
		public bool useSharedVertices;

		protected internal List<IndexData> lodFaceList = new List<IndexData>();

		/// <summary>Type of vertex animation for dedicated vertex data (populated by Mesh)</summary>
		protected VertexAnimationType vertexAnimationType = VertexAnimationType.None;

		#endregion Member variables

		#region Constructor

		/// <summary>
		///		Basic contructor.
		/// </summary>
		public SubMesh( /*string name*/ )
			: base()
		{
			//this.name = name;

			useSharedVertices = true;

			operationType = OperationType.TriangleList;
		}

		#endregion Constructor

		#region Methods

		/// <summary>
		///    Assigns a vertex to a bone with a given weight, for skeletal animation.
		/// </summary>
		/// <remarks>
		///    This method is only valid after setting the SkeletonName property.
		///    You should not need to modify bone assignments during rendering (only the positions of bones)
		///    and the engine reserves the right to do some internal data reformatting of this information,
		///    depending on render system requirements.
		/// </remarks>
		/// <param name="boneAssignment"></param>
		public void AddBoneAssignment( VertexBoneAssignment boneAssignment )
		{
			if ( !boneAssignmentList.ContainsKey( boneAssignment.vertexIndex ) )
			{
				boneAssignmentList[ boneAssignment.vertexIndex ] = new List<VertexBoneAssignment>();
			}
			boneAssignmentList[ boneAssignment.vertexIndex ].Add( boneAssignment );
			boneAssignmentsOutOfDate = true;
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
			boneAssignmentList.Clear();
			boneAssignmentsOutOfDate = true;
		}

		/// <summary>
		///    Must be called once to compile bone assignments into geometry buffer.
		/// </summary>
		protected internal void CompileBoneAssignments()
		{
			var maxBones = parent.RationalizeBoneAssignments( vertexData.vertexCount, boneAssignmentList );

			// return if no bone assigments
			if ( maxBones != 0 )
			{
				// FIXME: For now, to support hardware skinning with a single shader,
				// we always want to have 4 bones. (robin@multiverse.net)
				// maxBones = 4;
				parent.CompileBoneAssignments( boneAssignmentList, maxBones, vertexData );
			}
			boneAssignmentsOutOfDate = false;
		}

		public void RemoveLodLevels()
		{
			lodFaceList.Clear();
		}

		#endregion Methods

		#region Properties

		/// <summary>
		///		Gets/Sets the name of this SubMesh.
		/// </summary>
		public string Name
		{
			get
			{
				return name;
			}
			set
			{
				name = value;
			}
		}

		/// <summary>
		///		Gets/Sets the name of the material this SubMesh will be using.
		/// </summary>
		public string MaterialName
		{
			get
			{
				return materialName;
			}
			set
			{
				materialName = value;
				isMaterialInitialized = true;
			}
		}

		/// <summary>
		///		Gets/Sets the parent mode of this SubMesh.
		/// </summary>
		public Mesh Parent
		{
			get
			{
				return parent;
			}
			set
			{
				parent = value;
			}
		}

		/// <summary>
		///		Overloaded method.
		/// </summary>
		/// <param name="op"></param>
		/// <returns></returns>
		public void GetRenderOperation( RenderOperation op )
		{
			// call overloaded method with lod index of 0 by default
			GetRenderOperation( op, 0 );
		}

		/// <summary>
		///    Fills a RenderOperation structure required to render this mesh.
		/// </summary>
		/// <param name="op">Reference to a RenderOperation structure to populate.</param>
		/// <param name="lodIndex">The index of the LOD to use.</param>
		public void GetRenderOperation( RenderOperation op, int lodIndex )
		{
			// meshes always use indices
			op.useIndices = true;

			// use lod face list if requested, else pass the normal face list
			if ( lodIndex > 0 && ( lodIndex - 1 ) < lodFaceList.Count )
			{
				// Use the set of indices defined for this LOD level
				op.indexData = lodFaceList[ lodIndex - 1 ];
			}
			else
			{
				op.indexData = indexData;
			}

			// set the operation type
			op.operationType = operationType;

			// set the vertex data correctly
			op.vertexData = useSharedVertices ? parent.SharedVertexData : vertexData;
		}

		/// <summary>
		///		Gets whether or not a material has been set for this subMesh.
		/// </summary>
		public bool IsMaterialInitialized
		{
			get
			{
				return isMaterialInitialized;
			}
		}

		/// <summary>
		///		Gets bone assigment list
		/// </summary>
		public Dictionary<int, List<VertexBoneAssignment>> BoneAssignmentList
		{
			get
			{
				return boneAssignmentList;
			}
		}

		public int NumFaces
		{
			get
			{
				var numFaces = 0;
				if ( indexData == null )
				{
					return 0;
				}
				if ( operationType == OperationType.TriangleList )
				{
					numFaces = indexData.indexCount / 3;
				}
				else
				{
					numFaces = indexData.indexCount - 2;
				}
				return numFaces;
			}
		}

		public OperationType OperationType
		{
			get
			{
				return operationType;
			}
			set
			{
				operationType = value;
			}
		}

		public VertexAnimationType VertexAnimationType
		{
			get
			{
				if ( parent.AnimationTypesDirty )
				{
					parent.DetermineAnimationTypes();
				}
				return vertexAnimationType;
			}
			set
			{
				vertexAnimationType = value;
			}
		}

		public VertexAnimationType CurrentVertexAnimationType
		{
			get
			{
				return vertexAnimationType;
			}
		}

		public List<IndexData> LodFaceList
		{
			get
			{
				return lodFaceList;
			}
		}

		public VertexData VertexData
		{
			get
			{
				return vertexData;
			}
		}

		public IndexData IndexData
		{
			get
			{
				return indexData;
			}
		}

		#endregion Properties

		#region DisposableObject Implementation

		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				if ( disposeManagedResources )
				{
					// Dispose managed resources.
					if ( indexData != null )
					{
						if ( !indexData.IsDisposed )
						{
							indexData.Dispose();
						}
					}

					if ( vertexData != null )
					{
						if ( !vertexData.IsDisposed )
						{
							vertexData.Dispose();
						}
					}

					foreach ( var data in lodFaceList )
					{
						if ( !data.IsDisposed )
						{
							data.Dispose();
						}
					}
				}
				// There are no unmanaged resources to release, but
				// if we add them, they need to be released here.
			}

			base.dispose( disposeManagedResources );
		}

		#endregion DisposableObject Implementation
	}
}
