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

using Axiom.MathLib;

namespace Axiom
{
    /// <summary>
    ///    Defines an instance of a discrete, movable object based on a Mesh.
    /// </summary>
    /// <remarks>
    ///		Axiom generally divides renderable objects into 2 groups, discrete
    ///		(separate) and relatively small objects which move around the world,
    ///		and large, sprawling geometry which makes up generally immovable
    ///		scenery, aka 'level geometry'.
    ///		<para>
    ///		The <see cref="Mesh"/> and <see cref="SubMesh"/> classes deal with the definition of the geometry
    ///		used by discrete movable objects. Entities are actual instances of
    ///		objects based on this geometry in the world. Therefore there is
    ///		usually a single set <see cref="Mesh"/> for a car, but there may be multiple
    ///		entities based on it in the world. Entities are able to override
    ///		aspects of the Mesh it is defined by, such as changing material
    ///		properties per instance (so you can have many cars using the same
    ///		geometry but different textures for example). Because a <see cref="Mesh"/> is split
    ///		into a list of <see cref="SubMesh"/> objects for this purpose, the Entity class is a grouping class
    ///		(much like the <see cref="Mesh"/> class) and much of the detail regarding
    ///		individual changes is kept in the <see cref="SubEntity"/> class. There is a 1:1
    ///		relationship between <see cref="SubEntity"/> instances and the <see cref="SubMesh"/> instances
    ///		associated with the <see cref="Mesh"/> the Entity is based on.
    ///		</para>
    ///		<para>
    ///		Entity and <see cref="SubEntity"/> classes are never created directly. 
    ///		Use <see cref="SceneManager.CreateEntity"/> (passing a model name) to
    ///		create one.
    ///		</para>
    ///		<para>
    ///		Entities are included in the scene by using <see cref="SceneNode.AttachObject"/>
    ///		to associate them with a scene node.
    ///		</para>
    /// </remarks>
    public class Entity : MovableObject, IDisposable
    {
        #region Fields

        /// <summary>
        ///    3D Mesh that represents this entity.
        /// </summary>
        protected Mesh mesh;
        /// <summary>
        ///    List of sub entities.
        /// </summary>
        protected SubEntityCollection subEntityList = new SubEntityCollection();
        public SubEntityCollection SubEntities
        {
            get
            {
                return subEntityList;
            }
        }
        /// <summary>
        ///    SceneManager responsible for creating this entity.
        /// </summary>
        protected SceneManager sceneMgr;
        /// <summary>
        ///    Name of the material to be used for this entity.
        /// </summary>
        protected string materialName;
        /// <summary>
        ///    Bounding box that 'contains' all the meshes of each child entity.
        /// </summary>
        protected AxisAlignedBox fullBoundingBox;
        /// <summary>
        ///    State of animation for animable meshes.
        /// </summary>
        protected AnimationStateCollection animationState = new AnimationStateCollection();
        /// <summary>
        ///    Cached bone matrices, including and world transforms.
        /// </summary>
        protected internal Matrix4[] boneMatrices;
        /// <summary>
        ///    Number of matrices associated with this entity.
        /// </summary>
        protected internal int numBoneMatrices;
        /// <summary>
        ///    Flag that determines whether or not to display skeleton.
        /// </summary>
        protected bool displaySkeleton;
        /// <summary>
        ///    The LOD number of the mesh to use, calculated by NotifyCurrentCamera.
        /// </summary>
        protected int meshLodIndex;
        /// <summary>
        ///    LOD bias factor, inverted for optimization when calculating adjusted depth.
        /// </summary>
        protected float meshLodFactorInv;
        /// <summary>
        ///    Index of minimum detail LOD (higher index is lower detail).
        /// </summary>
        protected int minMeshLodIndex;
        /// <summary>
        ///    Index of maximum detail LOD (lower index is higher detail).
        /// </summary>
        protected int maxMeshLodIndex;
        /// <summary>
        ///    Flag indicating that mesh uses manual LOD and so might have multiple SubEntity versions.
        /// </summary>
        protected bool usingManualLod;
        /// <summary>
        ///    Render detail to be used for this entity (solid, wireframe, point).
        /// </summary>
        protected SceneDetailLevel renderDetail;
        /// <summary>
        ///		Temp blend buffer details for shared geometry.
        /// </summary>
        protected TempBlendedBufferInfo tempBlendedBuffer = new TempBlendedBufferInfo();
        /// <summary>
        ///		Temp blend buffer details for shared geometry.
        /// </summary>
        protected internal VertexData sharedBlendedVertexData;
        /// <summary>
        ///		Flag indicating whether hardware skinning is supported by this entity's materials.
        /// </summary>
        protected bool useHardwareSkinning;
        /// <summary>
        ///     Flag indicating whether we have a vertex program in use on any of our subentities.
        /// </summary>
        protected bool vertexProgramInUse;
        /// <summary>
        ///		Records the last frame in which animation was updated.
        /// </summary>
        protected ulong frameAnimationLastUpdated;
        /// <summary>
        ///		This entity's personal copy of a master skeleton.
        /// </summary>
        protected SkeletonInstance skeletonInstance;
        /// <summary>
        ///		List of child objects attached to this entity.
        /// </summary>
        protected MovableObjectCollection childObjectList = new MovableObjectCollection();
        /// <summary>
        ///		List of shadow renderables for this entity.
        /// </summary>
        protected ShadowRenderableList shadowRenderables = new ShadowRenderableList();
        /// <summary>
        ///		LOD bias factor, inverted for optimisation when calculating adjusted depth.
        /// </summary>
        protected float materialLodFactorInv;
        /// <summary>
        ///		Index of minimum detail LOD (NB higher index is lower detail).
        /// </summary>
        protected int minMaterialLodIndex;
        /// <summary>
        ///		Index of maximum detail LOD (NB lower index is higher detail).
        /// </summary>
        protected int maxMaterialLodIndex;
        /// <summary>
        ///     Frame the bones were last update.
        /// </summary>
        /// <remarks>
        ///     Stored as an array so the reference can be shared amongst skeleton instances.
        /// </remarks>
        protected ulong[] frameBonesLastUpdated = new ulong[] { 0 };
        /// <summary>
        ///     List of entities with various levels of detail.
        /// </summary>
        protected EntityList lodEntityList = new EntityList();
        public ICollection SubEntityMaterials
        {
            get
            {
                Material[] materials = new Material[subEntityList.Count];
                for ( int i = 0; i < subEntityList.Count; i++ )
                {
                    materials[i] = subEntityList[i].Material;
                }
                return materials;
            }
        }
        public ICollection SubEntityMaterialNames
        {
            get
            {
                string[] materials = new string[subEntityList.Count];
                for ( int i = 0; i < subEntityList.Count; i++ )
                {
                    materials[i] = subEntityList[i].MaterialName;
                }
                return materials;
            }
        }

        #endregion Fields

        #region Constructors

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="mesh"></param>
        /// <param name="creator"></param>
        internal Entity( string name, Mesh mesh, SceneManager creator )
        {
            this.name = name;
            this.sceneMgr = creator;
            //defaults to Points if not set
            this.renderDetail = SceneDetailLevel.Solid;

            SetMesh( mesh );


        }

        protected void SetMesh( Mesh mesh )
        {
            this.mesh = mesh;


            if ( mesh.HasSkeleton && mesh.Skeleton != null )
            {
                skeletonInstance = new SkeletonInstance( mesh.Skeleton );
                skeletonInstance.Load();
            }
            else
                skeletonInstance = null;

            this.subEntityList.Clear();
            BuildSubEntities();

            lodEntityList.Clear();
            // Check if mesh is using manual LOD
            if ( mesh.IsLodManual )
            {
                for ( int i = 1; i < mesh.LodLevelCount; i++ )
                {
                    MeshLodUsage usage = mesh.GetLodLevel( i );

                    // manually create entity
                    Entity lodEnt = new Entity( string.Format( "{0}Lod{1}", name, i ), usage.manualMesh, sceneMgr );
                    lodEntityList.Add( lodEnt );
                }
            }

            animationState.Clear();
            // init the AnimationState, if the mesh is animated
            if ( mesh.HasSkeleton )
            {
                mesh.InitAnimationState( animationState );
                numBoneMatrices = skeletonInstance.BoneCount;
                boneMatrices = new Matrix4[numBoneMatrices];
                PrepareTempBlendedBuffers();
            }

            ReevaluateVertexProcessing();


            // LOD default settings
            meshLodFactorInv = 1.0f;
            // Backwards, remember low value = high detail
            minMeshLodIndex = 99;
            maxMeshLodIndex = 0;

            // Material LOD default settings
            materialLodFactorInv = 1.0f;
            maxMaterialLodIndex = 0;
            minMaterialLodIndex = 99;

            // Do we have a mesh where edge lists are not going to be available?
            if ( !mesh.IsEdgeListBuilt && !mesh.AutoBuildEdgeLists )
            {
                this.CastShadows = false;
            }
        }

        #endregion

        #region Properties

        public SkeletonInstance Skeleton
        {
            get
            {
                return this.skeletonInstance;
            }
        }

        /// <summary>
        ///    Local bounding radius of this entity.
        /// </summary>
        public override float BoundingRadius
        {
            get
            {
                float radius = mesh.BoundingSphereRadius;

                // scale by the largest scale factor
                if ( parentNode != null )
                {
                    Vector3 s = parentNode.DerivedScale;
                    radius *= MathUtil.Max( s.x, MathUtil.Max( s.y, s.z ) );
                }

                return radius;
            }
        }

        /// <summary>
        ///    Merge all the child object Bounds and return it.
        /// </summary>
        /// <returns></returns>
        public AxisAlignedBox ChildObjectsBoundingBox
        {
            get
            {
                AxisAlignedBox box;
                AxisAlignedBox fullBox = AxisAlignedBox.Null;

                for ( int i = 0; i < childObjectList.Count; i++ )
                {
                    MovableObject child = childObjectList[i];
                    box = child.BoundingBox;
                    TagPoint tagPoint = (TagPoint)child.ParentNode;

                    box.Transform( tagPoint.FullLocalTransform );

                    fullBox.Merge( box );
                }

                return fullBox;
            }
        }

        /// <summary>
        ///    Gets/Sets the flag to render the skeleton of this entity.
        /// </summary>
        public bool DisplaySkeleton
        {
            get
            {
                return displaySkeleton;
            }
            set
            {
                displaySkeleton = value;
            }
        }


        /// <summary>
        ///		Returns true if this entity has a skeleton.
        /// </summary>
        public bool HasSkeleton
        {
            get
            {
                return skeletonInstance != null;
            }
        }

        /// <summary>
        ///		Returns whether or not hardware skinning is enabled.
        /// </summary>
        /// <remarks>
        ///		Because fixed-function indexed vertex blending is rarely supported
        ///		by existing graphics cards, hardware skinning can only be done if
        ///		the vertex programs in the materials used to render an entity support
        ///		it. Therefore, this method will only return true if all the materials
        ///		assigned to this entity have vertex programs assigned, and all those
        ///		vertex programs must support 'include_skeletal_animation true'.
        /// </remarks>
        public bool IsHardwareSkinningEnabled
        {
            get
            {
                return useHardwareSkinning;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// DOC
        public int MeshLodIndex
        {
            get
            {
                return meshLodIndex;
            }
            set
            {
                meshLodIndex = value;
            }
        }

        /// <summary>
        ///		Gets the 3D mesh associated with this entity.
        /// </summary>
        public Mesh Mesh
        {
            get
            {
                return mesh;
            }
            set
            {
                SetMesh( value );
            }
        }

        /// <summary>
        /// 
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
                //if null or empty string then reset the material to that defined by the mesh
                if ( value == null || value == string.Empty )
                {
                    foreach ( SubEntity ent in subEntityList.Values )
                    {
                        string defaultMaterial = ent.SubMesh.MaterialName;
                        if ( defaultMaterial != null && defaultMaterial != string.Empty )
                            ent.MaterialName = defaultMaterial;
                    }
                }
                else
                {

                    // assign the material name to all sub entities
                    for ( int i = 0; i < subEntityList.Count; i++ )
                    {
                        subEntityList[i].MaterialName = materialName;
                    }
                }
            }
        }

        /// <summary>
        ///    Sets the rendering detail of this entire entity (solid, wireframe etc).
        /// </summary>
        public SceneDetailLevel RenderDetail
        {
            get
            {
                return renderDetail;
            }
            set
            {
                renderDetail = value;

                // also set for all sub entities
                for ( int i = 0; i < subEntityList.Count; i++ )
                {
                    GetSubEntity( i ).RenderDetail = renderDetail;
                }
            }
        }

        /// <summary>
        ///    Gets the number of sub entities that belong to this entity.
        /// </summary>
        public int SubEntityCount
        {
            get
            {
                return subEntityList.Count;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        ///		Attaches another object to a certain bone of the skeleton which this entity uses.
        /// </summary>
        /// <remarks>
        ///		This method can be used to attach another object to an animated part of this entity,
        ///		by attaching it to a bone in the skeleton (with an offset if required). As this entity 
        ///		is animated, the attached object will move relative to the bone to which it is attached.
        /// </remarks>
        /// <param name="boneName">The name of the bone (in the skeleton) to attach this object.</param>
        /// <param name="sceneObject">Reference to the object to attach.</param>
        public TagPoint AttachObjectToBone( string boneName, MovableObject sceneObject )
        {
            return AttachObjectToBone( boneName, sceneObject, Quaternion.Identity );
        }

        /// <summary>
        ///		Attaches another object to a certain bone of the skeleton which this entity uses.
        /// </summary>
        /// <param name="boneName">The name of the bone (in the skeleton) to attach this object.</param>
        /// <param name="sceneObject">Reference to the object to attach.</param>
        /// <param name="offsetOrientation">An adjustment to the orientation of the attached object, relative to the bone.</param>
        public TagPoint AttachObjectToBone( string boneName, MovableObject sceneObject, Quaternion offsetOrientation )
        {
            return AttachObjectToBone( boneName, sceneObject, Quaternion.Identity, Vector3.UnitScale );
        }

        /// <summary>
        ///		Attaches another object to a certain bone of the skeleton which this entity uses.
        /// </summary>
        /// <param name="boneName">The name of the bone (in the skeleton) to attach this object.</param>
        /// <param name="sceneObject">Reference to the object to attach.</param>
        /// <param name="offsetOrientation">An adjustment to the orientation of the attached object, relative to the bone.</param>
        /// <param name="offsetPosition">An adjustment to the position of the attached object, relative to the bone.</param>
        public TagPoint AttachObjectToBone( string boneName, MovableObject sceneObject, Quaternion offsetOrientation, Vector3 offsetPosition )
        {
            if ( sceneObject.IsAttached )
            {
                throw new AxiomException( "SceneObject '{0}' is already attached to '{1}'", sceneObject.Name, sceneObject.ParentNode.Name );
            }

            if ( !this.HasSkeleton )
            {
                throw new AxiomException( "Entity '{0}' has no skeleton to attach an object to.", this.name );
            }

            Bone bone = skeletonInstance.GetBone( boneName );

            if ( bone == null )
            {
                throw new AxiomException( "Entity '{0}' does not have a skeleton with a bone named '{1}'.", this.name, boneName );
            }

            TagPoint tagPoint =
                skeletonInstance.CreateTagPointOnBone( bone, offsetOrientation, offsetPosition );

            tagPoint.ParentEntity = this;
            tagPoint.ChildObject = sceneObject;

            AttachObjectImpl( sceneObject, tagPoint );

            return tagPoint;
        }

        /// <summary>
        ///		Internal implementation of attaching a 'child' object to this entity and assign 
        ///		the parent node to the child entity.
        /// </summary>
        /// <param name="sceneObject">Object to attach.</param>
        /// <param name="tagPoint">TagPoint to attach the object to.</param>
        protected void AttachObjectImpl( MovableObject sceneObject, TagPoint tagPoint )
        {
            childObjectList[sceneObject.Name] = sceneObject;
            sceneObject.NotifyAttached( tagPoint, true );
        }
		public MovableObject DetachObjectFromBone(string name)
		{

			MovableObject obj = childObjectList[name];
			if (obj == null)
			{
				throw new AxiomException("Child object named '{0}' not found.  Entity.DetachObjectFromBone", name);
			}

			DetachObjectImpl(obj);
			childObjectList.Remove(obj);

			return obj;
		}
		//-----------------------------------------------------------------------
		public void DetachObjectFromBone(MovableObject obj)
		{
			for(int i = 0; i < childObjectList.Count; i++) 
			{
				MovableObject child = childObjectList[i];
				if (child == obj)
				{
					DetachObjectImpl(obj);
					childObjectList.Remove(obj);

					// Trigger update of bounding box if necessary
					if (this.parentNode != null)
						parentNode.NeedUpdate();
					break;
				}
			}
		}

		public void DetachAllObjectsFromBone()
		{
			DetachAllObjectsImpl();

			// Trigger update of bounding box if necessary
			if (this.parentNode != null)
				parentNode.NeedUpdate();
		}

		public void DetachObjectImpl(MovableObject pObject)
		{
			TagPoint tagPoint = (TagPoint)pObject.ParentNode;


			// free the TagPoint so we can reuse it later
			//TODO: NO idea what this does!
			//skeletonInstance.freeTagPoint(tp);

			pObject.NotifyAttached(tagPoint, true);
		}
		/// <summary>
		/// 
		/// </summary>
		public void DetachAllObjectsImpl()
		{
			for(int i = 0; i < childObjectList.Count; i++) 
			{
				MovableObject child = childObjectList[i];
				DetachObjectImpl(child);
			}
			childObjectList.Clear();
		}


        /// <summary>
        ///		Used to build a list of sub-entities from the meshes located in the mesh.
        /// </summary>
        protected void BuildSubEntities()
        {
            // loop through the models meshes and create sub entities from them
            for ( int i = 0; i < mesh.SubMeshCount; i++ )
            {
                SubMesh subMesh = mesh.GetSubMesh( i );
                SubEntity sub = new SubEntity();
                sub.Parent = this;
                sub.SubMesh = subMesh;

                if ( subMesh.IsMaterialInitialized )
                {
                    sub.MaterialName = subMesh.MaterialName;
                }

                subEntityList.Add( sub );
            }
        }

        /// <summary>
        ///    Protected method to cache bone matrices from a skeleton.
        /// </summary>
        protected void CacheBoneMatrices()
        {
            ulong currentFrameCount = Root.Instance.CurrentFrameCount;

            if ( frameBonesLastUpdated[0] == currentFrameCount )
            {
                return;
            }

            // Get the appropriate meshes skeleton here
            // Can use lower LOD mesh skeleton if mesh LOD is manual
            // We make the assumption that lower LOD meshes will have
            //   fewer bones than the full LOD, therefore marix stack will be
            //   big enough.

            // Check for LOD usage
            if ( mesh.IsLodManual && meshLodIndex > 1 )
            {
                // use lower detail skeleton
                Mesh lodMesh = mesh.GetLodLevel( meshLodIndex ).manualMesh;

                if ( !lodMesh.HasSkeleton )
                {
                    numBoneMatrices = 0;
                    return;
                }
            }

            skeletonInstance.SetAnimationState( animationState );
            skeletonInstance.GetBoneMatrices( boneMatrices );
            frameBonesLastUpdated[0] = currentFrameCount;

            // TODO Skeleton instance sharing

            // update the child object's transforms
            for ( int i = 0; i < childObjectList.Count; i++ )
            {
                MovableObject child = childObjectList[i];
                child.ParentNode.Update( true, true );
            }

            // apply the current world transforms to these too, since these are used as
            // replacement world matrices
            Matrix4 worldXform = this.ParentNodeFullTransform;
            numBoneMatrices = skeletonInstance.BoneCount;

            for ( int i = 0; i < numBoneMatrices; i++ )
            {
                boneMatrices[i] = worldXform * boneMatrices[i];
            }
        }

        /// <summary>
        ///		Internal method - given vertex data which could be from the <see cref="Mesh"/> or 
        ///		any <see cref="SubMesh"/>, finds the temporary blend copy.
        /// </summary>
        /// <param name="originalData"></param>
        /// <returns></returns>
        protected VertexData FindBlendedVertexData( VertexData originalData )
        {
            if ( originalData == mesh.SharedVertexData )
            {
                return sharedBlendedVertexData;
            }

            for ( int i = 0; i < subEntityList.Count; i++ )
            {
                SubEntity se = subEntityList[i];
                if ( originalData == se.SubMesh.vertexData )
                {
                    return se.blendedVertexData;
                }
            }

            throw new Exception( "Cannot find blended version of the vertex data specified." );
        }

        /// <summary>
        ///		Internal method - given vertex data which could be from the	Mesh or 
        ///		any SubMesh, finds the corresponding SubEntity.
        /// </summary>
        /// <param name="original"></param>
        /// <returns></returns>
        protected SubEntity FindSubEntityForVertexData( VertexData original )
        {
            if ( original == mesh.SharedVertexData )
            {
                return null;
            }

            for ( int i = 0; i < subEntityList.Count; i++ )
            {
                SubEntity subEnt = subEntityList[i];

                if ( original == subEnt.SubMesh.vertexData )
                {
                    return subEnt;
                }
            }

            // none found
            return null;
        }

        /// <summary>
        ///		Perform all the updates required for an animated entity.
        /// </summary>
        protected void UpdateAnimation()
        {
            // we only do these tasks if they have not already been done this frame
            ulong currentFrameNumber = Root.Instance.CurrentFrameCount;

            if ( frameAnimationLastUpdated != currentFrameNumber )
            {
                CacheBoneMatrices();

                // software blend?
                bool hwSkinning = this.IsHardwareSkinningEnabled;

                if ( !hwSkinning
                    || Root.Instance.SceneManager.ShadowTechnique == ShadowTechnique.StencilAdditive
                    || Root.Instance.SceneManager.ShadowTechnique == ShadowTechnique.StencilModulative )
                {

                    // Ok, we need to do a software blend
                    // Blend normals in s/w only if we're not using h/w skinning,
                    // since shadows only require positions
                    bool blendNormals = !hwSkinning;

                    if ( sharedBlendedVertexData != null )
                    {
                        // blend shared geometry
                        tempBlendedBuffer.CheckoutTempCopies( true, blendNormals );
                        tempBlendedBuffer.BindTempCopies( sharedBlendedVertexData, hwSkinning );

                        Mesh.SoftwareVertexBlend( mesh.SharedVertexData, sharedBlendedVertexData, boneMatrices, blendNormals );
                    }

                    // blend dedicated geometry for each submesh if need be
                    for ( int i = 0; i < subEntityList.Count; i++ )
                    {
                        SubEntity subEntity = subEntityList[i];

                        if ( subEntity.IsVisible && subEntity.blendedVertexData != null )
                        {
                            subEntity.tempBlendedBuffer.CheckoutTempCopies( true, blendNormals );
                            subEntity.tempBlendedBuffer.BindTempCopies( subEntity.blendedVertexData, hwSkinning );

                            Mesh.SoftwareVertexBlend( subEntity.SubMesh.vertexData, subEntity.blendedVertexData, boneMatrices, blendNormals );
                        }
                    }
                }

                // trigger update of bounding box if necessary
                if ( childObjectList.Count != 0 )
                {
                    parentNode.NeedUpdate();
                }

                // remember the last frame count
                frameAnimationLastUpdated = currentFrameNumber;
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
        /// <returns></returns>
        public AnimationStateCollection GetAllAnimationStates()
        {
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
        public AnimationState GetAnimationState( string name )
        {
            Debug.Assert( animationState.ContainsKey( name ), "animationState.ContainsKey(name)" );

            return animationState[name];
        }

        #endregion Methods

        #region Implementation of IDisposable

        /// <summary>
        ///		
        /// </summary>
        public void Dispose()
        {
        }

        #endregion

        #region Implementation of SceneObject

        public override EdgeData GetEdgeList( int lodIndex )
        {
            return mesh.GetEdgeList( lodIndex );
        }

        public override void NotifyCurrentCamera( Camera camera )
        {
            if ( parentNode != null )
            {
                float squaredDepth = parentNode.GetSquaredViewDepth( camera );

                // Adjust this depth by the entity bias factor
                float temp = squaredDepth * meshLodFactorInv;

                // Now adjust it by the camera bias
                temp = temp * camera.InverseLodBias;

                // Get the index at this biased depth
                meshLodIndex = mesh.GetLodIndexSquaredDepth( temp );

                // Apply maximum detail restriction (remember lower = higher detail)
                meshLodIndex = (int)MathUtil.Max( maxMeshLodIndex, meshLodIndex );

                // Apply minimum detail restriction (remember higher = lower detail)
                meshLodIndex = (int)MathUtil.Min( minMeshLodIndex, meshLodIndex );

                // now do material LOD
                // adjust this depth by the entity bias factor
                temp = squaredDepth * materialLodFactorInv;

                // now adjust it by the camera bias
                temp = temp * camera.InverseLodBias;

                // apply the material LOD to all sub entities
                for ( int i = 0; i < subEntityList.Count; i++ )
                {
                    // get the index at this biased depth
                    SubEntity subEnt = subEntityList[i];

                    int idx = subEnt.Material.GetLodIndexSquaredDepth( temp );

                    // Apply maximum detail restriction (remember lower = higher detail)
                    idx = (int)MathUtil.Max( maxMaterialLodIndex, idx );
                    // Apply minimum detail restriction (remember higher = lower detail)
                    subEnt.materialLodIndex = (int)MathUtil.Min( minMaterialLodIndex, idx );
                }
            }

            // Notify child objects (tag points)
            for ( int i = 0; i < childObjectList.Count; i++ )
            {
                MovableObject child = childObjectList[i];
                child.NotifyCurrentCamera( camera );
            }
        }

        /// <summary>
        ///    Gets the SubEntity at the specified index.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public SubEntity GetSubEntity( int index )
        {
            Debug.Assert( index < subEntityList.Count, "index < subEntityList.Count" );

            return subEntityList[index];
        }

        /// <summary>
        ///     Gets a sub entity of this mesh with the specified name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public SubEntity GetSubEntity( string name )
        {
            for ( int i = 0; i < subEntityList.Count; i++ )
            {
                SubEntity sub = subEntityList[i];

                if ( sub.SubMesh.name == name )
                {
                    return sub;
                }
            }

            // not found
            throw new AxiomException( "A SubEntity with the name '{0}' does not exist in Entity '{1}'", name, this.name );
        }

        /// <summary>
        ///		Trigger an evaluation of whether hardware skinning is supported for this entity.
        /// </summary>
        protected internal void ReevaluateVertexProcessing()
        {
            // init
            useHardwareSkinning = false;
            vertexProgramInUse = false;

            bool firstPass = true;

            // check for each sub entity
            for ( int i = 0; i < this.SubEntityCount; i++, firstPass = false )
            {
                SubEntity subEntity = GetSubEntity( i );

                // grab the material and make sure it is loaded first
                Material m = subEntity.Material;
                m.Load();

                Technique t = m.GetBestTechnique();

                if ( t == null )
                {
                    // no supported techniques
                    continue;
                }

                Pass p = t.GetPass( 0 );

                if ( p == null )
                {
                    // no passes, so invalid
                    continue;
                }

                if ( p.HasVertexProgram )
                {
                    // If one material uses a vertex program, set this flag 
                    // Causes some special processing like forcing a separate light cap
                    vertexProgramInUse = true;

                    // All materials must support skinning for us to consider using
                    // hardware skinning - if one fails we use software
                    if ( firstPass )
                    {
                        useHardwareSkinning = p.VertexProgram.IsSkeletalAnimationIncluded;
                    }
                    else
                    {
                        useHardwareSkinning = useHardwareSkinning && p.VertexProgram.IsSkeletalAnimationIncluded;
                    }
                }
            }
        }

        /// <summary>
        ///    Sets a level-of-detail bias for the material detail of this entity.
        /// </summary>
        /// <remarks>
        ///    Level of detail reduction is normally applied automatically based on the Material 
        ///    settings. However, it is possible to influence this behavior for this entity
        ///    by adjusting the LOD bias. This 'nudges' the material level of detail used for this 
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
        ///    LOD will be limited by the number in the material)</param>
        public void SetMaterialLodBias( float factor, int maxDetailIndex, int minDetailIndex )
        {
            Debug.Assert( factor > 0.0f, "Bias factor must be > 0!" );
            materialLodFactorInv = 1.0f / factor;
            maxMaterialLodIndex = maxDetailIndex;
            minMaterialLodIndex = minDetailIndex;
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
        public void SetMeshLodBias( float factor, int maxDetailIndex, int minDetailIndex )
        {
            Debug.Assert( factor > 0.0f, "Bias factor must be > 0!" );
            meshLodFactorInv = 1.0f / factor;
            maxMeshLodIndex = maxDetailIndex;
            minMeshLodIndex = minDetailIndex;
        }

        /// <summary>
        ///     Copies a subset of animation states from source to target.
        /// </summary>
        /// <remarks>
        ///     This routine assume target is a subset of source, it will copy all animation state
        ///     of the target with the settings from source.
        /// </remarks>
        /// <param name="target">Reference to animation state set which will receive the states.</param>
        /// <param name="source">Reference to animation state set which will use as source.</param>
        public void CopyAnimationStateSubset( AnimationStateCollection target, AnimationStateCollection source )
        {
            for ( int i = 0; i < target.Count; i++ )
            {
                AnimationState targetState = target[i];
                AnimationState sourceState = source[targetState.Name];

                if ( sourceState == null )
                {
                    throw new AxiomException( "No animation entry found named '{0}'.", targetState.Name );
                }
                else
                {
                    sourceState.CopyTo( targetState );
                }
            }
        }

        /// <summary>
        ///		
        /// </summary>
        /// <param name="queue"></param>
        public override void UpdateRenderQueue( RenderQueue queue )
        {
            // Manual LOD sub entities
            if ( meshLodIndex > 0 && mesh.IsLodManual )
            {
                Debug.Assert( meshLodIndex - 1 < lodEntityList.Count, "No LOD EntityList - did you build the manual LODs after creating the entity?" );

                Entity lodEnt = lodEntityList[meshLodIndex - 1];

                // index - 1 as we skip index 0 (original LOD)
                if ( this.HasSkeleton && lodEnt.HasSkeleton )
                {
                    // Copy the animation state set to lod entity, we assume the lod
                    // entity only has a subset animation states
                    CopyAnimationStateSubset( lodEnt.animationState, animationState );
                }

                lodEnt.UpdateRenderQueue( queue );
                return;
            }

            // add all visible sub entities to the render queue
            for ( int i = 0; i < subEntityList.Count; i++ )
            {
                if ( subEntityList[i].IsVisible )
                {
                    queue.AddRenderable( subEntityList[i], RenderQueue.DEFAULT_PRIORITY, renderQueueID );
                }
            }

            // Since we know we're going to be rendered, take this opportunity to 
            // update the animation
            if ( mesh.HasSkeleton )
            {
                UpdateAnimation();

                // Update render queue with child objects (tag points)
                for ( int i = 0; i < childObjectList.Count; i++ )
                {
                    MovableObject child = childObjectList[i];

                    if ( child.IsVisible )
                    {
                        child.UpdateRenderQueue( queue );
                    }
                }
            }

            // TODO Add skeleton itself to the render queue
        }

        /// <summary>
        ///		Internal method for preparing this Entity for use in animation.
        /// </summary>
        protected internal void PrepareTempBlendedBuffers()
        {
            if ( this.HasSkeleton )
            {
                // shared data
                if ( mesh.SharedVertexData != null )
                {
                    // Create temporary vertex blend info
                    // Prepare temp vertex data if needed
                    // Clone without copying data, remove blending info
                    // (since blend is performed in software)
                    sharedBlendedVertexData = CloneVertexDataRemoveBlendInfo( mesh.SharedVertexData );
                    ExtractTempBufferInfo( sharedBlendedVertexData, tempBlendedBuffer );
                }

                // prepare temp blending buffers for subentites as well
                for ( int i = 0; i < this.SubEntityCount; i++ )
                {
                    subEntityList[i].PrepareTempBlendBuffers();
                }
            }
        }

        /// <summary>
        ///		Internal method to clone vertex data definitions but to remove blend buffers.
        /// </summary>
        /// <param name="sourceData">Vertex data to clone.</param>
        /// <returns>A cloned instance of 'source' without blending information.</returns>
        protected internal VertexData CloneVertexDataRemoveBlendInfo( VertexData source )
        {
            // Clone without copying data
            VertexData ret = source.Clone( false );
            VertexElement blendIndexElem =
                source.vertexDeclaration.FindElementBySemantic( VertexElementSemantic.BlendIndices );
            VertexElement blendWeightElem =
                source.vertexDeclaration.FindElementBySemantic( VertexElementSemantic.BlendWeights );

            // Remove blend index
            if ( blendIndexElem != null )
            {
                // Remove buffer reference
                ret.vertexBufferBinding.UnsetBinding( blendIndexElem.Source );
            }

            if ( blendWeightElem != null &&
                blendWeightElem.Source != blendIndexElem.Source )
            {

                // Remove buffer reference
                ret.vertexBufferBinding.UnsetBinding( blendWeightElem.Source );
            }
            // remove elements from declaration
            ret.vertexDeclaration.RemoveElement( VertexElementSemantic.BlendIndices );
            ret.vertexDeclaration.RemoveElement( VertexElementSemantic.BlendWeights );

            // copy reference to w-coord buffer
            if ( source.hardwareShadowVolWBuffer != null )
            {
                ret.hardwareShadowVolWBuffer = source.hardwareShadowVolWBuffer;
            }

            return ret;
        }

        /// <summary>
        ///		Internal method for extracting metadata out of source vertex data
        ///		for fast assignment of temporary buffers later.
        /// </summary>
        /// <param name="sourceData"></param>
        /// <param name="info"></param>
        protected internal void ExtractTempBufferInfo( VertexData sourceData, TempBlendedBufferInfo info )
        {
            VertexDeclaration decl = sourceData.vertexDeclaration;
            VertexBufferBinding bind = sourceData.vertexBufferBinding;
            VertexElement posElem = decl.FindElementBySemantic( VertexElementSemantic.Position );
            VertexElement normElem = decl.FindElementBySemantic( VertexElementSemantic.Normal );

            Debug.Assert( posElem != null, "Positions are required!" );

            info.posBindIndex = posElem.Source;
            info.srcPositionBuffer = bind.GetBuffer( info.posBindIndex );

            if ( normElem == null )
            {
                info.posNormalShareBuffer = false;
                info.srcNormalBuffer = null;
            }
            else
            {
                info.normBindIndex = normElem.Source;

                if ( info.normBindIndex == info.posBindIndex )
                {
                    info.posNormalShareBuffer = true;
                    info.srcNormalBuffer = null;
                }
                else
                {
                    info.posNormalShareBuffer = false;
                    info.srcNormalBuffer = bind.GetBuffer( info.normBindIndex );
                }
            }
        }

        public override IEnumerator GetShadowVolumeRenderableEnumerator( ShadowTechnique technique, Light light,
            HardwareIndexBuffer indexBuffer, bool extrudeVertices, float extrusionDistance, int flags )
        {

            Debug.Assert( indexBuffer != null, "Only external index buffers are supported right now" );
            Debug.Assert( indexBuffer.Type == IndexType.Size16, "Only 16-bit indexes supported for now" );

            // Prep mesh if required
            // NB This seems to result in memory corruptions, having problems
            // tracking them down. For now, ensure that shadows are enabled
            // before any entities are created
            if ( !mesh.IsPreparedForShadowVolumes )
            {
                mesh.PrepareForShadowVolume();
                // reset frame last updated to force update of buffers
                frameAnimationLastUpdated = 0;
                // re-prepare buffers
                PrepareTempBlendedBuffers();
            }

            // Update any animation 
            if ( this.HasSkeleton )
            {
                UpdateAnimation();
            }

            // Calculate the object space light details
            Vector4 lightPos = light.GetAs4DVector();

            // Only use object-space light if we're not doing transforms
            // Since when animating the positions are already transformed into 
            // world space so we need world space light position
            if ( !this.HasSkeleton )
            {
                Matrix4 world2Obj = parentNode.FullTransform.Inverse();

                lightPos = world2Obj * lightPos;
            }

            // We need to search the edge list for silhouette edges
            EdgeData edgeList = GetEdgeList();

            // Init shadow renderable list if required
            bool init = ( shadowRenderables.Count == 0 );

            if ( init )
            {
                shadowRenderables.Capacity = edgeList.edgeGroups.Count;
            }

            bool updatedSharedGeomNormals = false;

            EntityShadowRenderable esr = null;
            EdgeData.EdgeGroup egi;

            // note: using capacity for the loop since no items are in the list yet.
            // capacity is set to how large the collection will be in the end
            for ( int i = 0; i < shadowRenderables.Capacity; i++ )
            {
                egi = (EdgeData.EdgeGroup)edgeList.edgeGroups[i];

                if ( init )
                {
                    VertexData data = null;

                    if ( this.HasSkeleton )
                    {
                        // Use temp buffers
                        data = FindBlendedVertexData( egi.vertexData );
                    }
                    else
                    {
                        data = egi.vertexData;
                    }

                    // Try to find corresponding SubEntity; this allows the 
                    // linkage of visibility between ShadowRenderable and SubEntity
                    SubEntity subEntity = FindSubEntityForVertexData( egi.vertexData );

                    // Create a new renderable, create a separate light cap if
                    // we're using hardware skinning since otherwise we get
                    // depth-fighting on the light cap
                    esr = new EntityShadowRenderable( this, indexBuffer, data, vertexProgramInUse || !extrudeVertices, subEntity );

                    shadowRenderables.Add( esr );
                }
                else
                {
                    esr = (EntityShadowRenderable)shadowRenderables[i];

                    if ( this.HasSkeleton )
                    {
                        // If we have a skeleton, we have no guarantee that the position
                        // buffer we used last frame is the same one we used last frame
                        // since a temporary buffer is requested each frame
                        // therefore, we need to update the EntityShadowRenderable
                        // with the current position buffer
                        esr.RebindPositionBuffer();
                    }
                }

                // For animated entities we need to recalculate the face normals
                if ( this.HasSkeleton )
                {
                    if ( egi.vertexData != mesh.SharedVertexData || !updatedSharedGeomNormals )
                    {
                        // recalculate face normals
                        edgeList.UpdateFaceNormals( egi.vertexSet, esr.PositionBuffer );

                        // If we're not extruding in software we still need to update 
                        // the latter part of the buffer (the hardware extruded part)
                        // with the latest animated positions
                        if ( !extrudeVertices )
                        {
                            IntPtr srcPtr = esr.PositionBuffer.Lock( BufferLocking.Normal );
                            IntPtr destPtr = new IntPtr( srcPtr.ToInt32() + ( egi.vertexData.vertexCount * 12 ) );

                            // 12 = sizeof(float) * 3
                            Memory.Copy( srcPtr, destPtr, 12 * egi.vertexData.vertexCount );

                            esr.PositionBuffer.Unlock();
                        }

                        if ( egi.vertexData == mesh.SharedVertexData )
                        {
                            updatedSharedGeomNormals = true;
                        }
                    }
                }
                // Extrude vertices in software if required
                if ( extrudeVertices )
                {
                    ExtrudeVertices( esr.PositionBuffer, egi.vertexData.vertexCount, lightPos, extrusionDistance );
                }

                // Stop suppressing hardware update now, if we were
                esr.PositionBuffer.SuppressHardwareUpdate( false );
            }

            // Calc triangle light facing
            UpdateEdgeListLightFacing( edgeList, lightPos );

            // Generate indexes and update renderables
            GenerateShadowVolume( edgeList, indexBuffer, light, shadowRenderables, flags );

            return shadowRenderables.GetEnumerator();
        }

        public override IEnumerator GetLastShadowVolumeRenderableEnumerator()
        {
            return shadowRenderables.GetEnumerator();
        }

        #endregion Methods

        #region Properties

        /// <summary>
        ///		Gets the number of bone matrices for this entity if it has a skeleton attached.
        /// </summary>
        public int BoneMatrixCount
        {
            get
            {
                return numBoneMatrices;
            }
        }

        /// <summary>
        ///		Gets the full local bounding box of this entity.
        /// </summary>
        public override AxisAlignedBox BoundingBox
        {
            // return the bounding box of our mesh
            get
            {
                fullBoundingBox = mesh.BoundingBox;
                fullBoundingBox.Merge( this.ChildObjectsBoundingBox );

                // don't need to scale here anymore

                return fullBoundingBox;
            }
        }

        #endregion Properties

        #region ICloneable Members

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Entity Clone( string newName )
        {
            // create a new entity using the current mesh (uses same instance, not a copy for speed)
            Entity clone = sceneMgr.CreateEntity( newName, mesh.Name );

            // loop through each subentity and set the material up for the clone
            for ( int i = 0; i < subEntityList.Count; i++ )
            {
                SubEntity subEntity = subEntityList[i];
                clone.GetSubEntity( i ).MaterialName = subEntity.MaterialName;
            }

            // copy the animation state as well
            if ( animationState != null )
            {
                clone.animationState = animationState.Clone();
            }

            return clone;
        }

        #endregion

        #region Nested Classes

        /// <summary>
        ///		Nested class to allow entity shadows.
        /// </summary>
        protected class EntityShadowRenderable : ShadowRenderable
        {
            #region Fields

            protected Entity parent;
            /// <summary>
            ///		Shared ref to the position buffer.
            /// </summary>
            protected HardwareVertexBuffer positionBuffer;
            /// <summary>
            ///		Shared ref to w-coord buffer (optional).
            /// </summary>
            protected HardwareVertexBuffer wBuffer;
            /// <summary>
            ///		Ref to original vertex data.
            /// </summary>
            protected VertexData originalVertexData;
            /// <summary>
            ///		Original position buffer source binding.
            /// </summary>
            protected short originalPosBufferBinding;
            /// <summary>
            ///		Link to SubEntity, only present if SubEntity has it's own geometry.
            /// </summary>
            protected SubEntity subEntity;

            #endregion Fields

            #region Constructor

            public EntityShadowRenderable( Entity parent, HardwareIndexBuffer indexBuffer,
                VertexData vertexData, bool createSeperateLightCap, SubEntity subEntity )
                : this( parent, indexBuffer, vertexData, createSeperateLightCap, subEntity, false )
            {
            }

            public EntityShadowRenderable( Entity parent, HardwareIndexBuffer indexBuffer,
                VertexData vertexData, bool createSeparateLightCap, SubEntity subEntity, bool isLightCap )
            {

                this.parent = parent;

                // Save link to vertex data
                originalVertexData = vertexData;

                // Initialise render op
                renderOp.indexData = new IndexData();
                renderOp.indexData.indexBuffer = indexBuffer;
                renderOp.indexData.indexStart = 0;
                // index start and count are sorted out later

                // Create vertex data which just references position component (and 2 component)
                renderOp.vertexData = new VertexData();
                renderOp.vertexData.vertexDeclaration =
                    HardwareBufferManager.Instance.CreateVertexDeclaration();
                renderOp.vertexData.vertexBufferBinding =
                    HardwareBufferManager.Instance.CreateVertexBufferBinding();

                // Map in position data
                renderOp.vertexData.vertexDeclaration.AddElement( 0, 0, VertexElementType.Float3, VertexElementSemantic.Position );
                originalPosBufferBinding =
                    vertexData.vertexDeclaration.FindElementBySemantic( VertexElementSemantic.Position ).Source;

                positionBuffer = vertexData.vertexBufferBinding.GetBuffer( originalPosBufferBinding );
                renderOp.vertexData.vertexBufferBinding.SetBinding( 0, positionBuffer );

                // Map in w-coord buffer (if present)
                if ( vertexData.hardwareShadowVolWBuffer != null )
                {
                    renderOp.vertexData.vertexDeclaration.AddElement( 1, 0, VertexElementType.Float1, VertexElementSemantic.TexCoords, 0 );
                    wBuffer = vertexData.hardwareShadowVolWBuffer;
                    renderOp.vertexData.vertexBufferBinding.SetBinding( 1, wBuffer );
                }

                // Use same vertex start as input
                renderOp.vertexData.vertexStart = vertexData.vertexStart;

                if ( isLightCap )
                {
                    // Use original vertex count, no extrusion
                    renderOp.vertexData.vertexCount = vertexData.vertexCount;
                }
                else
                {
                    // Vertex count must take into account the doubling of the buffer,
                    // because second half of the buffer is the extruded copy
                    renderOp.vertexData.vertexCount = vertexData.vertexCount * 2;

                    if ( createSeparateLightCap )
                    {
                        // Create child light cap
                        lightCap = new EntityShadowRenderable( parent, indexBuffer, vertexData, false, subEntity, true );
                    }
                }
            }

            #endregion Constructor

            #region Properties

            /// <summary>
            ///		Gets a reference to the position buffer in use by this renderable.
            /// </summary>
            public HardwareVertexBuffer PositionBuffer
            {
                get
                {
                    return positionBuffer;
                }
            }

            /// <summary>
            ///		Gets a reference to the w-buffer in use by this renderable.
            /// </summary>
            public HardwareVertexBuffer WBuffer
            {
                get
                {
                    return wBuffer;
                }
            }

            #endregion Properties

            #region Methods

            /// <summary>
            ///		Rebind the source positions for temp buffer users.
            /// </summary>
            public void RebindPositionBuffer()
            {
                positionBuffer = originalVertexData.vertexBufferBinding.GetBuffer( originalPosBufferBinding );
                renderOp.vertexData.vertexBufferBinding.SetBinding( 0, positionBuffer );

                if ( lightCap != null )
                {
                    ( (EntityShadowRenderable)lightCap ).RebindPositionBuffer();
                }
            }

            #endregion Methods

            #region ShadowRenderable Members

            public override void GetWorldTransforms( Matrix4[] matrices )
            {
                if ( parent.BoneMatrixCount == 0 )
                {
                    matrices[0] = parent.ParentNodeFullTransform;
                }
                else
                {
                    // pretransformed
                    matrices[0] = Matrix4.Identity;
                }
            }

            public override Quaternion WorldOrientation
            {
                get
                {
                    return parent.ParentNode.DerivedOrientation;
                }
            }

            public override Vector3 WorldPosition
            {
                get
                {
                    return parent.ParentNode.DerivedPosition;
                }
            }

            public override bool IsVisible
            {
                get
                {
                    if ( subEntity != null )
                    {
                        return subEntity.IsVisible;
                    }

                    return base.IsVisible;
                }
            }

            #endregion ShadowRenderable Members
        }

        #endregion Nested Classes
    }
}
