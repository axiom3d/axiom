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

using System;
using System.Collections;
using Axiom.Animating;
using Axiom.Configuration;
using Axiom.Enumerations;
using Axiom.SubSystems.Rendering;

namespace Axiom.Core {
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
    public class SubMesh {
        #region Member variables

        /// <summary>The parent mesh that this subMesh belongs to.</summary>
        protected Mesh parent;
        /// <summary>Name of the material assigned to this subMesh.</summary>
        protected String materialName;
        /// <summary>Name of this SubMesh.</summary>
        protected String name;
        /// <summary>Indicates if this submesh shares vertex data with other meshes or whether it has it's own vertices.</summary>
        protected internal bool useSharedVertices;
        /// <summary></summary>
        protected bool isMaterialInitialized;
        /// <summary>Number of faces in this subMesh.</summary>
        protected internal short numFaces;
        /// <summary>Indices to use for parent geometry when using shared vertices.</summary>
        protected internal short[] faceIndices;
		
        /// <summary>List of bone assignment for this mesh.</summary>
        protected SortedList boneAssignmentList = new SortedList();
        /// <summary>Flag indicating that bone assignments need to be recompiled.</summary>
        protected internal bool boneAssignmentsOutOfDate;

        /// <summary>Mode used for rendering this submesh.</summary>
        protected internal Axiom.SubSystems.Rendering.RenderMode operationType;
        public VertexData vertexData;
        public IndexData indexData = new IndexData();

        #endregion
		
        #region Constructor

        /// <summary>
        ///		Basic contructor.
        /// </summary>
        /// <param name="name"></param>
        public SubMesh(String name) {
            this.name = name;

            useSharedVertices = true;

            operationType = RenderMode.TriangleList;
        }

        #endregion

        #region Methods

        /// <summary>
        ///    Assigns a vertex to a bone with a given weight, for skeletal animation. 
        /// </summary>
        /// <remarks>
        ///    This method is only valid after calling setSkeletonName.
        ///    You should not need to modify bone assignments during rendering (only the positions of bones) 
        ///    and the engine reserves the right to do some internal data reformatting of this information, 
        ///    depending on render system requirements.
        /// </remarks>
        /// <param name="boneAssignment"></param>
        public void AddBoneAssignment(ref VertexBoneAssignment boneAssignment) {
            boneAssignmentList.Add(boneAssignment.vertexIndex, boneAssignment);
            boneAssignmentsOutOfDate = true;
        }

        /// <summary>
        ///    Removes all bone assignments for this mesh. 
        /// </summary>
        /// <remarks>
        ///    This method is for modifying weights to the shared geometry of the Mesh. To assign
        ///    weights to the per-SubMesh geometry, see the equivalent methods on SubMesh.
        /// </remarks>
        public void ClearBoneAssignments() {
            boneAssignmentList.Clear();
            boneAssignmentsOutOfDate = true;
        }

        /// <summary>
        ///    Must be called once to compile bone assignments into geometry buffer.
        /// </summary>
        protected internal void CompileBoneAssignments() {
            short maxBones = 0;
            short currentBones = 0;
            ushort lastVertexIndex = ushort.MaxValue;

            // find the largest number of bones per vertex
            for(int i = 0; i < boneAssignmentList.Count; i++) {
                VertexBoneAssignment boneAssignment =
                    (VertexBoneAssignment)boneAssignmentList.GetByIndex(i);

                if(lastVertexIndex != boneAssignment.vertexIndex) {
                    if(maxBones < currentBones) {
                        maxBones = currentBones;
                    }
                    currentBones = 0;
                } // if

                currentBones++;

                lastVertexIndex = (ushort)boneAssignment.vertexIndex;
            } // for

            if(maxBones > Config.MaxBlendWeights) {
                throw new Exception("SubMesh '" + name + "' has too many bone assignments per vertex.");
            }

            // no bone assignments?  get outta here
            if(maxBones == 0) {
                return;
            }

            // figure out which method of bone assignment compilation to use
            if(parent.useSoftwareBlending) {
                parent.CompileBoneAssignmentsSoftware(boneAssignmentList, maxBones, vertexData);
            }
            else {
                parent.CompileBoneAssignmentsHardware(boneAssignmentList, maxBones, vertexData);
            }

            boneAssignmentsOutOfDate = false;
        }

        #endregion Methods

        #region Properties

        /// <summary>
        ///		Gets/Sets the name of the material this SubMesh will be using.
        /// </summary>
        public String MaterialName {
            get { return materialName; }
            set { materialName = value; isMaterialInitialized = true; }
        }

        /// <summary>
        ///		Gets/Sets the parent mode of this SubMesh.
        /// </summary>
        public Mesh Parent {
            get { return parent; }
            set { parent = value; }
        }

        /// <summary>
        ///		Overloaded method.
        /// </summary>
        /// <param name="op"></param>
        /// <returns></returns>
        public void GetRenderOperation(RenderOperation op) {
            // call overloaded method with lod index of 0 by default
            GetRenderOperation(op, 0);
        }

        /// <summary>
        ///    Fills a RenderOperation structure required to render this mesh.
        /// </summary>
        /// <param name="op">Reference to a RenderOperation structure to populate.</param>
        /// <param name="lodIndex">The index of the LOD to use.</param>
        public void GetRenderOperation(RenderOperation op, int lodIndex) {
            // meshes always use indices
            op.useIndices = true;

            if(lodIndex > 0) {
                // TODO: Use LOD index list
            }
            else
                op.indexData = indexData;
			
            // set the operation type
            op.operationType = operationType;

            // set the vertex data correctly
            op.vertexData = useSharedVertices? parent.SharedVertexData : vertexData;
        }

        /// <summary>
        ///		Gets whether or not a material has been set for this subMesh.
        /// </summary>
        public bool IsMaterialInitialized {
            get { return isMaterialInitialized; }
        }

        #endregion
    }
}
