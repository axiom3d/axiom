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
using System.Diagnostics;
using Axiom.Animating;
using Axiom.Collections;
using Axiom.MathLib;
using Axiom.SubSystems.Rendering;

namespace Axiom.Core {
    /// <summary>
    ///    The Entity class serves as the base class for all objects in the engine.   
    ///    It represents the minimum functionality required for an object in a 3D SceneGraph.
    /// </summary>
    // TODO: Add LOD usage
    public class Entity : SceneObject, IDisposable {
        #region Member variables

        /// <summary>3D Mesh that represents this entity</summary>
        protected Mesh mesh;
        /// <summary>List of sub entities.</summary>
        protected SubEntityCollection subEntityList = new SubEntityCollection();
        /// <summary>SceneManager responsible for creating this entity.</summary>
        protected SceneManager sceneMgr;
        /// <summary>Name of the material to be used for this entity.</summary>
        protected string materialName;
        /// <summary>Bounding box that 'contains' all the meshes of each child entity</summary>
        protected AxisAlignedBox fullBoundingBox;
        /// <summary>State of animation for animable meshes</summary>
        protected AnimationStateCollection animationState = new AnimationStateCollection();
        /// <summary>Cached bone matrices, including and world transforms.</summary>
        protected internal Matrix4[] boneMatrices;
        /// <summary>Number of matrices associated with this entity.</summary>
        protected internal int numBoneMatrices;
        /// <summary>Flag determines whether or not to display skeleton</summary>
        protected bool displaySkeleton;
        /// <summary>The LOD number of the mesh to use, calculated by NotifyCurrentCamera</summary>
        protected int meshLodIndex;
        /// <summary>LOD bias factor, inverted for optimization when calculating adjusted depth.</summary>
        protected float meshLodFactorInv;
        /// <summary>Index of minimum detail LOD (higher index is lower detail)</summary>
        protected int minMeshLodIndex;
        /// <summary>Index of maximum detail LOD (lower index is higher detail)</summary>
        protected int maxMeshLodIndex;
        /// <summary>Flag indicating that mesh uses manual LOD and so might have multiple SubEntity versions</summary>
        protected bool usingManualLod;
        /// <summary>Render detail to be used for this entity (solid, wireframe, point).</summary>
        protected SceneDetailLevel renderDetail;

        #endregion

        #region Constructors

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="mesh"></param>
        /// <param name="creator"></param>
        internal Entity(string name, Mesh mesh, SceneManager creator) {
            this.name = name;
            this.mesh = mesh;
            this.sceneMgr = creator;

            BuildSubEntities();

            // LOD default settings
            meshLodFactorInv = 1.0f;
            // Backwards, remember low value = high detail
            minMeshLodIndex = 99;

            // init the AnimationState, if the mesh is animated
            if(mesh.HasSkeleton) {
                mesh.InitAnimationState(animationState);
                numBoneMatrices = mesh.BoneMatrixCount;
                boneMatrices = new Matrix4[numBoneMatrices];
            }
        }

        #endregion

        #region Properties
		
        /// <summary>
        ///    Local bounding radius of this entity.
        /// </summary>
        public override float BoundingRadius {
            get {
                float radius = mesh.BoundingSphereRadius;

                // scale by the largest scale factor
                if(parentNode != null) {
                    Vector3 s = parentNode.DerivedScale;
                    radius *= MathUtil.Max(s.x, MathUtil.Max(s.y, s.z));
                }

                return radius;
            }
        }

        /// <summary>
        ///    Gets/Sets the flag to render the skeleton of this entity.
        /// </summary>
        public bool DisplaySkeleton {
            get {
                return displaySkeleton;
            }
            set {
                displaySkeleton = value;
            }
        }
            
        /// <summary>
        /// 
        /// </summary>
        /// DOC
        public int MeshLodIndex {
            get { return meshLodIndex; }
            set { meshLodIndex = value; }
        }

        /// <summary>
        ///		Gets the 3D mesh associated with this entity.
        /// </summary>
        public Mesh Mesh {
            get { return mesh; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string MaterialName {
            set {
                materialName = value;

                // assign the material name to all sub entities
                for(int i = 0; i < subEntityList.Count; i++)
                    subEntityList[i].MaterialName = materialName;
            }
        }

        /// <summary>
        ///    Sets the rendering detail of this entire entity (solid, wireframe etc).
        /// </summary>
        public SceneDetailLevel RenderDetail {
            get {
                return renderDetail;
            }
            set {
                renderDetail = value;

                // also set for all sub entities
                for(int i = 0; i < subEntityList.Count; i++) {
                    GetSubEntity(i).RenderDetail = renderDetail;
                }
            }
        }

        /// <summary>
        ///    Gets the number of sub entities that belong to this entity.
        /// </summary>
        public int SubEntityCount {
            get {
                return subEntityList.Count;
            }
        }

        #endregion

        #region Private methods

        /// <summary>
        ///		Used to build a list of sub-entities from the meshes located in the mesh.
        /// </summary>
        public void BuildSubEntities() {
            // loop through the models meshes and create sub entities from them
            for(int i = 0; i < mesh.SubMeshCount; i++) {
                SubMesh subMesh = mesh.GetSubMesh(i);
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
        public void Dispose() {
        }

        #endregion

        #region Implementation of SceneObject

        /// <summary>
        ///    
        /// </summary>
        protected void CacheBoneMatrices() {
            Mesh theMesh = null;

            // Get the appropriate meshes skeleton here
            // Can use lower LOD mesh skeleton if mesh LOD is manual
            // We make the assumption that lower LOD meshes will have
            //   fewer bones than the full LOD, therefore marix stack will be
            //   big enough.

            // TODO: Check for LOD usage

            theMesh = this.mesh;

            // tell the skeleton who is making a call to update it
            theMesh.Skeleton.CurrentEntity = this;

            theMesh.GetBoneMatrices(animationState, boneMatrices);

            // reset the skeleton to 'no caller'
            theMesh.Skeleton.CurrentEntity = null;

            // apply the current world transforms to these too, since these are used as
            // replacement world matrices
            Matrix4 worldXform = this.ParentFullTransform;
            numBoneMatrices = theMesh.BoneMatrixCount;

            for(int i = 0; i < numBoneMatrices; i++) {
                boneMatrices[i] = worldXform * boneMatrices[i];
            }
        }

        /// <summary>
        ///    For entities based on animated meshes, gets the AnimationState object for a single animation.
        /// </summary>
        /// <remarks>
        ///    You animate an entity by updating the animation state objects. Each of these represents the
        ///    current state of each animation available to the entity. The AnimationState objects are
        ///    initialized from the Mesh object.
        /// </remarks>
        /// <param name="name"></param>
        /// <returns></returns>
        public AnimationStateCollection GetAllAnimationStates() {
            return animationState;
        }

        /// <summary>
        ///    For entities based on animated meshes, gets the AnimationState object for a single animation.
        /// </summary>
        /// <remarks>
        ///    You animate an entity by updating the animation state objects. Each of these represents the
        ///    current state of each animation available to the entity. The AnimationState objects are
        ///    initialized from the Mesh object.
        /// </remarks>
        /// <param name="name"></param>
        /// <returns></returns>
        public AnimationState GetAnimationState(string name) {
            Debug.Assert(animationState.ContainsKey(name), "animationState.ContainsKey(name)");

            return animationState[name];
        }

        internal override void NotifyCurrentCamera(Axiom.Core.Camera camera) {
            if(parentNode != null) {
                float squaredDepth = parentNode.GetSquaredViewDepth(camera);

                // Adjust this depth by the entity bias factor
                squaredDepth = squaredDepth * meshLodFactorInv;

                // Now adjust it by the camera bias
                squaredDepth = squaredDepth * camera.InverseLodBias;
                
                // Get the index at this biased depth
                meshLodIndex = mesh.GetLodIndexSquaredDepth(squaredDepth);
                
                // Apply maximum detail restriction (remember lower = higher detail)
                meshLodIndex = (int)MathUtil.Max(maxMeshLodIndex, meshLodIndex);
                
                // Apply minimum detail restriction (remember higher = lower detail)
                meshLodIndex = (int)MathUtil.Min(minMeshLodIndex, meshLodIndex);
            }

            // TODO: Notify child objects (tag points)
        }

        /// <summary>
        ///    Merge all the child object Bounds and return it.
        /// </summary>
        /// <returns></returns>
        public AxisAlignedBox GetChildObjectsBoundingBox() {
            // TODO: Implement Entity.GetChildObjectsBoundingBox after adding TagPoints
            return AxisAlignedBox.Null;
        }

        /// <summary>
        ///    Gets the SubEntity at the specified index.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public SubEntity GetSubEntity(int index) {
            Debug.Assert(index < subEntityList.Count, "index < subEntityList.Count");

            return subEntityList[index];
        }

        /// <summary>
        ///    Sets a level-of-detail bias on this entity.
        /// </summary>
        /// <remarks>
        ///    Level of detail reduction is normally applied automatically based on the Mesh 
        ///    settings. However, it is possible to influence this behavior for this entity
        ///    by adjusting the LOD bias. This 'nudges' the level of detail used for this 
        ///    entity up or down depending on your requirements. You might want to use this
        ///    if there was a particularly important entity in your scene which you wanted to
        ///    detail better than the others, such as a player model.
        ///    <p/>
        ///    There are three parameters to this method; the first is a factor to apply; it 
        ///    defaults to 1.0 (no change), by increasing this to say 2.0, this model would 
        ///    take twice as long to reduce in detail, whilst at 0.5 this entity would use lower
        ///    detail versions twice as quickly. The other 2 parameters are hard limits which 
        ///    let you set the maximum and minimum level-of-detail version to use, after all
        ///    other calculations have been made. This lets you say that this entity should
        ///    never be simplified, or that it can only use LODs below a certain level even
        ///    when right next to the camera.
        /// </remarks>
        /// <param name="factor">Proportional factor to apply to the distance at which LOD is changed. 
        ///    Higher values increase the distance at which higher LODs are displayed (2.0 is 
        ///    twice the normal distance, 0.5 is half).</param>
        /// <param name="maxDetailIndex">The index of the maximum LOD this entity is allowed to use (lower
        ///    indexes are higher detail: index 0 is the original full detail model).</param>
        /// <param name="minDetailIndex">The index of the minimum LOD this entity is allowed to use (higher
        ///    indexes are lower detail. Use something like 99 if you want unlimited LODs (the actual
        ///    LOD will be limited by the number in the Mesh)</param>
        public void SetLoadBias(float factor, int maxDetailIndex, int minDetailIndex) {
            Debug.Assert(factor > 0.0f, "Bias factor must be > 0!");
            meshLodFactorInv = 1.0f / factor;
            maxMeshLodIndex = maxDetailIndex;
            minMeshLodIndex = minDetailIndex;
        }

        /// <summary>
        ///		
        /// </summary>
        /// <param name="queue"></param>
        internal override void UpdateRenderQueue(RenderQueue queue) {
            // add all sub entities to the render queue
            for(int i = 0; i < subEntityList.Count; i++)
                queue.AddRenderable(subEntityList[i], RenderQueue.DEFAULT_PRIORITY, renderQueueID);

            // Since we know we're going to be rendered, take this opportunity to 
            // cache bone matrices & apply world matrix to them
            if(mesh.HasSkeleton) {
                CacheBoneMatrices();

                // TODO: Update render queue with child objects (tag points)
            }

            // TODO: Add skeleton itself to the render queue
        }

        public override Axiom.Core.AxisAlignedBox BoundingBox {
            // return the bounding box of our mesh
            get {	 
                fullBoundingBox = mesh.BoundingBox;
                fullBoundingBox.Merge(GetChildObjectsBoundingBox());

                // scale by parent
                if(parentNode != null) {
                    fullBoundingBox.Scale(parentNode.DerivedScale);
                }

                return fullBoundingBox;
            }
        }

        #endregion

        #region ICloneable Members

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Entity Clone(string name) {
            // create a new entity using the current mesh (uses same instance, not a copy for speed)
            Entity clone = sceneMgr.CreateEntity(name, mesh.Name);

            // loop through each subentity and set the material up for the clone
            for(int i = 0; i < subEntityList.Count; i++) {
                SubEntity subEntity = subEntityList[i];
                clone.GetSubEntity(i).MaterialName = materialName;
            }

            // TODO: Make sure this is the desired effect, since all clones share the same state
            clone.animationState = animationState;

            return clone;
        }

        #endregion
    }
}