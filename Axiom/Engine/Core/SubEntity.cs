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
using System.Diagnostics;
using Axiom.Enumerations;
using Axiom.MathLib;
using Axiom.SubSystems.Rendering;

namespace Axiom.Core {
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
    public class SubEntity : IRenderable {
        #region Member variables

        /// <summary>Reference to the parent Entity.</summary>
        private Entity parent;
        /// <summary>Name of the material being used.</summary>
        private String materialName;
        /// <summary>Reference to the material being used by this SubEntity.</summary>
        private Material material;
        /// <summary>Reference to the subMesh that represents the geometry for this SubEntity.</summary>
        private SubMesh subMesh;
        /// <summary>Detail to be used for rendering this sub entity.</summary>
        private SceneDetailLevel renderDetail;

        #endregion

        #region Constructor

        /// <summary>
        ///		Internal constructor, only allows creation of SubEntities within the engine core.
        /// </summary>
        internal SubEntity() {
            material = MaterialManager.Instance["BaseWhite"];
            renderDetail = SceneDetailLevel.Solid;
        }

        #endregion

        #region Properties

        /// <summary>
        ///		Gets/Sets the name of the material used for this SubEntity.
        /// </summary>
        public String MaterialName {
            get { 
                return materialName; 
            }
            set { 
                materialName = value; 

                // load the material from the material manager (it should already exist
                material = MaterialManager.Instance[materialName];

                if(material == null) {
                    System.Diagnostics.Trace.Write(
                        String.Format("Cannot assign material '{0}' to SubEntity '{1}' because the material doesn't exist.", materialName, parent.Name));

                    // give it base white so we can continue
                    material = MaterialManager.Instance["BaseWhite"];
                }

                // ensure the material is loaded.  It will skip it if it already is
                material.Load();
            }
        }

        /// <summary>
        ///		Gets/Sets the subMesh to be used for rendering this SubEntity.
        /// </summary>
        public SubMesh SubMesh {
            get { return subMesh; }
            set { subMesh = value; }
        }

        /// <summary>
        ///		Gets/Sets the parent entity of this SubEntity.
        /// </summary>
        public Entity Parent {
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
        public Material Material {
            get { return material; }
            set { material = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="op"></param>
        /// DOC
        public void GetRenderOperation(RenderOperation op) {
            subMesh.GetRenderOperation(op, parent.MeshLODIndex);
        }

        Material IRenderable.Material {
            get { return material; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="matrices"></param>
        public void GetWorldTransforms(Matrix4[] matrices) {
            if(parent.numBoneMatrices == 0) {
                matrices[0] = parent.ParentFullTransform;
            }
            else {
                // use cached bone matrices of the parent entity
                for(int i = 0; i < parent.numBoneMatrices; i++) {
                    matrices[i] = parent.boneMatrices[i];
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public ushort NumWorldTransforms {
            get {
                if(parent.numBoneMatrices == 0) {
                    return 1;
                }
                else {
                    return (ushort)parent.numBoneMatrices;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool UseIdentityProjection {
            get {
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool UseIdentityView {
            get {
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public SceneDetailLevel RenderDetail {
            get { 
                return renderDetail;	
            }
            set {
                renderDetail = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="camera"></param>
        /// <returns></returns>
        public float GetSquaredViewDepth(Camera camera) {
            // get the parent entitie's parent node
            Node node = parent.ParentNode;

            Debug.Assert(node != null);

            return node.GetSquaredViewDepth(camera);
        }

        #endregion
    }
}
