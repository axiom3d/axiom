#region LGPL License
/*
Axiom Game Engine Library
Copyright (C) 2003  Axiom Project Team

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
#endregion

#region SVN Version Information
// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections;

#endregion

namespace Axiom
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
    /// <ogre name="SubMesh">
    ///     <file name="OgreSubMesh.h"   revision="1.19" lastUpdated="3/23/2005" lastUpdatedBy="DavidClifton" />
    ///     <file name="OgreSubMesh.cpp" revision="1.27" lastUpdated="3/23/2005" lastUpdatedBy="DavidClifton" />
    /// </ogre> 
    public class SubMesh
    {
        #region Fields and Properties


        /// <summary>Name of this SubMesh.</summary>
        internal string name;

        /// <summary>Number of faces in this subMesh.</summary>
        protected internal short numFaces;

        /// <summary>List of bone assignment for this mesh.</summary>
        protected Map boneAssignmentList = new Map();

        /// <summary>Flag indicating that bone assignments need to be recompiled.</summary>
        protected internal bool boneAssignmentsOutOfDate;

        /// <summary>Mode used for rendering this submesh.</summary>
        protected internal OperationType operationType;

        /// <summary>Vertex Data for the submesh</summary>
        public VertexData vertexData;

        /// <summary>Index Data for the submesh</summary>
        public IndexData indexData = new IndexData();

        /// <summary>Indicates if this submesh shares vertex data with other meshes or whether it has it's own vertices.</summary>
        public bool useSharedVertices;

        /// <summary>Indicates if this submesh shares vertex data with other meshes or whether it has it's own vertices.</summary>
        protected internal ArrayList lodFaceList = new ArrayList();

        #region MaterialName Property

        /// <summary>Name of the material assigned to this subMesh.</summary>
        protected string materialName;

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

        #endregion

        #region Parent Property

        /// <summary>The parent mesh that this subMesh belongs to.</summary>
        protected Mesh parent;

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
        #endregion Parent Property

        #region IsMaterialInitialized Property

        /// <summary>Boolean indicating if an appropriate material is applied</summary>
        protected bool isMaterialInitialized;

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

        #endregion IsMaterialInitialized Property

        #endregion Fields and Properties

        #region Constructor

        /// <summary>
        ///		Basic contructor requiring a name for the submesh
        /// </summary>
        /// <param name="name">Name used to identify the submesh</param>
        public SubMesh( string name )
        {
            this.name = name;
            useSharedVertices = true;
            operationType = OperationType.TriangleList;
        }

        #endregion

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
        public void AddBoneAssignment( ref VertexBoneAssignment boneAssignment )
        {
            if ( useSharedVertices )
            {
                throw new Exception( "Attempted to assign bones to submesh with shared vertices. Bones must be assigned at the Mesh level when shared vertices are used." );
            }
            boneAssignmentList.Insert( boneAssignment.vertexIndex, boneAssignment );
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
            int maxBones = parent.RationalizeBoneAssignments( vertexData.vertexCount, boneAssignmentList );

            // return if no bone assigments
            if ( maxBones == 0 )
            {
                return;
            }
            parent.CompileBoneAssignments( boneAssignmentList, maxBones, vertexData );
            boneAssignmentsOutOfDate = false;
        }

        /// <summary>
        ///		Fills a RenderOperation structure required to render this mesh, assuming a LOD level of 0
        /// </summary>
        /// <param name="op">Reference to a RenderOperation structure to populate.</param>
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
                op.indexData = ( IndexData )lodFaceList[lodIndex - 1];
            }
            else
            {
                op.indexData = indexData;
            }

            // set the operation type
            op.operationType = operationType;

            // set the vertex data correctly
            if ( useSharedVertices )
            {
                op.vertexData = parent.SharedVertexData;
            }
            else
            {
                op.vertexData = vertexData;
            }
        }

        #endregion Methods

    }

}
