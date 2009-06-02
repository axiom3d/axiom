#region LGPL License

/*
Axiom Graphics Engine Library
Copyright (C) 2003-2006 Axiom Project Team

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
//     <license see="http://axiomengine.sf.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections;
using System.Diagnostics;

using Axiom.Animating;
using Axiom.Collections;
using Axiom.Graphics;
using Axiom.Math;

#endregion

namespace Axiom.Core
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
        ///    State of animation for animable meshes.
        /// </summary>
        protected AnimationStateSet animationState = new AnimationStateSet();

        /// <summary>
        ///    Cached bone matrices, including and world transforms.
        /// </summary>
        protected internal Matrix4[] boneMatrices;

        /// <summary>
        ///		List of child objects attached to this entity.
        /// </summary>
        protected MovableObjectCollection childObjectList = new MovableObjectCollection();

        /// <summary>
        ///    Flag that determines whether or not to display skeleton.
        /// </summary>
        protected bool displaySkeleton;

        /// <summary>
        ///		Records the last frame in which animation was updated.
        /// </summary>
        protected ulong frameAnimationLastUpdated;

        /// <summary>
        ///     Frame the bones were last update.
        /// </summary>
        /// <remarks>
        ///     Stored as an array so the reference can be shared amongst skeleton instances.
        /// </remarks>
        protected ulong[] frameBonesLastUpdated = new ulong[] { 0 };

        /// <summary>
        ///    Bounding box that 'contains' all the meshes of each child entity.
        /// </summary>
        protected AxisAlignedBox fullBoundingBox;

        /// <summary>
        ///     Flag indicating whether hardware animation is supported by this entities materials
        /// </summary>
        /// <remarks>
        ///     Because fixed-function indexed vertex blending is rarely supported
        ///     by existing graphics cards, hardware animation can only be done if
        ///     the vertex programs in the materials used to render an entity support
        ///     it. Therefore, this method will only return true if all the materials
        ///     assigned to this entity have vertex programs assigned, and all those
        ///     vertex programs must support 'includes_morph_animation true' if using
        ///     morph animation, 'includes_pose_animation true' if using pose animation
        ///     and 'includes_skeletal_animation true' if using skeletal animation.
        /// </remarks>
        private bool hardwareAnimation;

        /// <summary>
        ///     Number of hardware poses supported by materials
        /// </summary>
        private ushort hardwarePoseCount;

        /// <summary>
        ///     Vertex data details for hardware vertex anim of shared geometry
        ///     - separate since we need to s/w anim for shadows whilst still altering
        ///     the vertex data for hardware morphing (pos2 binding)
        /// </summary>
        protected internal VertexData hardwareVertexAnimVertexData;

        /// <summary>
        ///		The most recent parent transform applied during animation
        /// </summary>
        protected Matrix4 lastParentXform;

        /// <summary>
        ///     List of entities with various levels of detail.
        /// </summary>
        protected EntityList lodEntityList = new EntityList();

        /// <summary>
        ///		LOD bias factor, inverted for optimisation when calculating adjusted depth.
        /// </summary>
        protected float materialLodFactorInv;

        /// <summary>
        ///    Name of the material to be used for this entity.
        /// </summary>
        protected string materialName;

        /// <summary>
        ///		Index of maximum detail LOD (NB lower index is higher detail).
        /// </summary>
        protected int maxMaterialLodIndex;

        /// <summary>
        ///    Index of maximum detail LOD (lower index is higher detail).
        /// </summary>
        protected int maxMeshLodIndex;

        /// <summary>
        ///    3D Mesh that represents this entity.
        /// </summary>
        protected Mesh mesh;

        /// <summary>
        ///    LOD bias factor, inverted for optimization when calculating adjusted depth.
        /// </summary>
        protected float meshLodFactorInv;

        /// <summary>
        ///    The LOD number of the mesh to use, calculated by NotifyCurrentCamera.
        /// </summary>
        protected int meshLodIndex;

        /// <summary>
        ///		Index of minimum detail LOD (NB higher index is lower detail).
        /// </summary>
        protected int minMaterialLodIndex;

        /// <summary>
        ///    Index of minimum detail LOD (higher index is lower detail).
        /// </summary>
        protected int minMeshLodIndex;

        /// <summary>
        ///    Number of matrices associated with this entity.
        /// </summary>
        protected internal int numBoneMatrices;

        /// <summary>
        ///    SceneManager responsible for creating this entity.
        /// </summary>
        protected SceneManager sceneMgr;

        /// <summary>
        ///		List of shadow renderables for this entity.
        /// </summary>
        protected ShadowRenderableList shadowRenderables = new ShadowRenderableList();

        /// <summary>
        ///     Vertex data details for software skeletal anim of shared geometry
        /// </summary>
        protected internal VertexData skelAnimVertexData;

        /// <summary>
        ///		This entity's personal copy of a master skeleton.
        /// </summary>
        protected SkeletonInstance skeletonInstance;

        /// <summary>
        ///     Counter indicating number of requests for software blended normals.
        /// </summary>
        /// <remarks>
        ///     If non-zero, and getSoftwareAnimationRequests() also returns non-zero,
        ///     then software animation of normals will be performed in updateAnimation
        ///     regardless of the current setting of isHardwareAnimationEnabled or any
        ///     internal optimise for eliminate software animation. Currently it is not
        ///     possible to force software animation of only normals. Consequently this
        ///     value is always less than or equal to that returned by getSoftwareAnimationRequests().
        ///     Requests for software animation of normals are made by calling the 
        ///     addSoftwareAnimationRequest() method with 'true' as the parameter.
        /// </remarks>
        protected internal int softwareAnimationNormalsRequests;

        /// <summary>
        ///     Counter indicating number of requests for software animation.
        /// </summary>
        /// <remarks>
        ///    If non-zero then software animation will be performed in updateAnimation
        ///    regardless of the current setting of isHardwareAnimationEnabled or any
        ///    internal optimise for eliminate software animation. Requests for software
        ///    animation are made by calling the AddSoftwareAnimationRequest() method.
        /// </remarks>
        protected internal int softwareAnimationRequests;

        /// <summary>
        ///     Vertex data details for software vertex anim of shared geometry
        /// </summary>
        protected internal VertexData softwareVertexAnimVertexData;

        /// <summary>
        ///    List of sub entities.
        /// </summary>
        protected SubEntityCollection subEntityList = new SubEntityCollection();

        /// <summary>
        ///		Temp blend buffer details for shared geometry.
        /// </summary>
        protected TempBlendedBufferInfo tempSkelAnimInfo = new TempBlendedBufferInfo();

        /// Data for vertex animation
        /// <summary>
        ///     Temp buffer details for software vertex anim of shared geometry
        /// </summary>
        protected internal TempBlendedBufferInfo tempVertexAnimInfo;

        /// <summary>
        ///    Flag indicating that mesh uses manual LOD and so might have multiple SubEntity versions.
        /// </summary>
        protected bool usingManualLod;

        /// <summary>
        ///     Have we applied any vertex animation to shared geometry?
        /// </summary>
        protected internal bool vertexAnimationAppliedThisFrame;

        /// <summary>
        ///     Flag indicating whether we have a vertex program in use on any of our subentities
        /// </summary>
        private bool vertexProgramInUse;

        public ICollection SubEntities
        {
            get
            {
                return this.subEntityList;
            }
        }

        public ICollection SubEntityMaterials
        {
            get
            {
                Material[] materials = new Material[this.subEntityList.Count];
                for ( int i = 0; i < this.subEntityList.Count; i++ )
                {
                    materials[ i ] = this.subEntityList[ i ].Material;
                }
                return materials;
            }
        }

        public ICollection SubEntityMaterialNames
        {
            get
            {
                string[] materials = new string[this.subEntityList.Count];
                for ( int i = 0; i < this.subEntityList.Count; i++ )
                {
                    materials[ i ] = this.subEntityList[ i ].MaterialName;
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

            this.SetMesh( mesh );
        }

        protected void SetMesh( Mesh mesh )
        {
            this.mesh = mesh;

            if ( mesh.HasSkeleton && mesh.Skeleton != null )
            {
                this.skeletonInstance = new SkeletonInstance( mesh.Skeleton );
                this.skeletonInstance.Load();
            }
            else
            {
                this.skeletonInstance = null;
            }

            this.subEntityList.Clear();
            this.BuildSubEntities();

            this.lodEntityList.Clear();
            // Check if mesh is using manual LOD
            if ( mesh.IsLodManual )
            {
                for ( int i = 1; i < mesh.LodLevelCount; i++ )
                {
                    MeshLodUsage usage = mesh.GetLodLevel( i );

                    // manually create entity
                    Entity lodEnt = new Entity( string.Format( "{0}Lod{1}", this.name, i ),
                                                usage.manualMesh,
                                                this.sceneMgr );
                    this.lodEntityList.Add( lodEnt );
                }
            }

            this.animationState.RemoveAllAnimationStates();
            // init the AnimationState, if the mesh is animated
            if ( this.HasSkeleton )
            {
                this.numBoneMatrices = this.skeletonInstance.BoneCount;
                this.boneMatrices = new Matrix4[this.numBoneMatrices];
            }
            if ( this.HasSkeleton || mesh.HasVertexAnimation )
            {
                mesh.InitAnimationState( this.animationState );
                this.PrepareTempBlendedBuffers();
            }

            this.ReevaluateVertexProcessing();

            // LOD default settings
            this.meshLodFactorInv = 1.0f;
            // Backwards, remember low value = high detail
            this.minMeshLodIndex = 99;
            this.maxMeshLodIndex = 0;

            // Material LOD default settings
            this.materialLodFactorInv = 1.0f;
            this.maxMaterialLodIndex = 0;
            this.minMaterialLodIndex = 99;

            // Do we have a mesh where edge lists are not going to be available?
            //if ( ( ( this.sceneMgr.ShadowTechnique == ShadowTechnique.StencilAdditive )
            //       || ( this.sceneMgr.ShadowTechnique == ShadowTechnique.StencilModulative ) ) &&
            //     !mesh.IsEdgeListBuilt && !mesh.AutoBuildEdgeLists )
            //{
            //    this.CastShadows = false;
            //}
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
                float radius = this.mesh.BoundingSphereRadius;

                // scale by the largest scale factor
                if ( this.parentNode != null )
                {
                    Vector3 s = this.parentNode.DerivedScale;
                    radius *= Utility.Max( s.x, Utility.Max( s.y, s.z ) );
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

                for ( int i = 0; i < this.childObjectList.Count; i++ )
                {
                    MovableObject child = this.childObjectList[ i ];
                    box = child.BoundingBox;
                    TagPoint tagPoint = (TagPoint) child.ParentNode;

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
                return this.displaySkeleton;
            }
            set
            {
                this.displaySkeleton = value;
            }
        }

        /// <summary>
        ///		Returns true if this entity has a skeleton.
        /// </summary>
        public bool HasSkeleton
        {
            get
            {
                return this.skeletonInstance != null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public int MeshLodIndex
        {
            get
            {
                return this.meshLodIndex;
            }
            set
            {
                this.meshLodIndex = value;
            }
        }

        /// <summary>
        ///		Gets the 3D mesh associated with this entity.
        /// </summary>
        public Mesh Mesh
        {
            get
            {
                return this.mesh;
            }
            set
            {
                this.SetMesh( value );
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public string MaterialName
        {
            get
            {
                return this.materialName;
            }
            set
            {
                this.materialName = value;
                //if null or empty string then reset the material to that defined by the mesh
                if ( value == null || value == string.Empty )
                {
                    foreach ( SubEntity ent in this.subEntityList )
                    {
                        string defaultMaterial = ent.SubMesh.MaterialName;
                        if ( defaultMaterial != null && defaultMaterial != string.Empty )
                        {
                            ent.MaterialName = defaultMaterial;
                            break;
                        }
                    }
                }
                else
                {
                    // assign the material name to all sub entities
                    for ( int i = 0; i < this.subEntityList.Count; i++ )
                    {
                        this.subEntityList[ i ].MaterialName = this.materialName;
                    }
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
                return this.subEntityList.Count;
            }
        }

        /// <summary>
        ///    Advanced method to get the temporarily blended software vertex animation information
        /// </summary>
        /// <remarks>
        ///     Internal engine will eliminate software animation if possible, this
        ///     information is unreliable unless added request for software animation
        ///     via addSoftwareAnimationRequest.
        /// </remarks>
        public VertexData SoftwareVertexAnimVertexData
        {
            get
            {
                return this.softwareVertexAnimVertexData;
            }
        }

        /// <summary>
        ///    Advanced method to get the hardware morph vertex information
        /// </summary>
        public VertexData HardwareVertexAnimVertexData
        {
            get
            {
                return this.hardwareVertexAnimVertexData;
            }
        }

        /// <summary>
        ///		Are buffers already marked as vertex animated?
        /// </summary>
        public bool BuffersMarkedForAnimation
        {
            get
            {
                return this.vertexAnimationAppliedThisFrame;
            }
        }

        /// <summary>
        ///		Is hardware animation enabled for this entity?
        /// </summary>
        public bool IsHardwareAnimationEnabled
        {
            get
            {
                return this.hardwareAnimation;
            }
        }

        #endregion

        #region Methods

        private static TimingMeter blendedVertexMeter = MeterManager.GetMeter( "Blended Vertex", "Animation Update" );
        private static TimingMeter boneGetBonesMeter = MeterManager.GetMeter( "Bone Get Bones", "Cache Bones" );
        private static TimingMeter boneSetAnimStateMeter = MeterManager.GetMeter( "Bone Set Anim State", "Cache Bones" );
        private static TimingMeter boneTransformMeter = MeterManager.GetMeter( "Bone Transform", "Cache Bones" );
        private static TimingMeter copyBoneMeter = MeterManager.GetMeter( "Copy Bone", "Animation Update" );
        private static TimingMeter entitySWBlendMeter = MeterManager.GetMeter( "Entity SW Blend", "Animation Update" );

        private static TimingMeter subEntitySWBlendMeter = MeterManager.GetMeter( "SubEntity SW Blend",
                                                                                  "Animation Update" );

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
            return this.AttachObjectToBone( boneName, sceneObject, Quaternion.Identity );
        }

        /// <summary>
        ///		Attaches another object to a certain bone of the skeleton which this entity uses.
        /// </summary>
        /// <param name="boneName">The name of the bone (in the skeleton) to attach this object.</param>
        /// <param name="sceneObject">Reference to the object to attach.</param>
        /// <param name="offsetOrientation">An adjustment to the orientation of the attached object, relative to the bone.</param>
        public TagPoint AttachObjectToBone( string boneName, MovableObject sceneObject, Quaternion offsetOrientation )
        {
            return this.AttachObjectToBone( boneName, sceneObject, Quaternion.Identity, Vector3.Zero );
        }

        /// <summary>
        ///		Attaches another object to a certain bone of the skeleton which this entity uses.
        /// </summary>
        /// <param name="boneName">The name of the bone (in the skeleton) to attach this object.</param>
        /// <param name="sceneObject">Reference to the object to attach.</param>
        /// <param name="offsetOrientation">An adjustment to the orientation of the attached object, relative to the bone.</param>
        /// <param name="offsetPosition">An adjustment to the position of the attached object, relative to the bone.</param>
        public TagPoint AttachObjectToBone( string boneName,
                                            MovableObject sceneObject,
                                            Quaternion offsetOrientation,
                                            Vector3 offsetPosition )
        {
            if ( this.childObjectList.ContainsKey( sceneObject.Name ) )
            {
                throw new AxiomException( "An object with the name {0} is already attached.", sceneObject.Name );
            }

            if ( sceneObject.IsAttached )
            {
                throw new AxiomException( "MovableObject '{0}' is already attached to '{1}'",
                                          sceneObject.Name,
                                          sceneObject.ParentNode.Name );
            }

            if ( !this.HasSkeleton )
            {
                throw new AxiomException( "Entity '{0}' has no skeleton to attach an object to.", this.name );
            }

            Bone bone = this.skeletonInstance.GetBone( boneName );
            if ( bone == null )
            {
                throw new AxiomException( "Entity '{0}' does not have a skeleton with a bone named '{1}'.",
                                          this.name,
                                          boneName );
            }

            TagPoint tagPoint = this.skeletonInstance.CreateTagPointOnBone( bone, offsetOrientation, offsetPosition );

            tagPoint.ParentEntity = this;
            tagPoint.ChildObject = sceneObject;

            this.AttachObjectImpl( sceneObject, tagPoint );

            // Trigger update of bounding box if necessary
            if ( this.parentNode != null )
            {
                this.parentNode.NeedUpdate();
            }

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
            this.childObjectList[ sceneObject.Name ] = sceneObject;
            sceneObject.NotifyAttached( tagPoint, true );
        }

        public MovableObject DetachObjectFromBone( string name )
        {
            MovableObject obj = this.childObjectList[ name ];
            if ( obj == null )
            {
                throw new AxiomException( "Child object named '{0}' not found.  Entity.DetachObjectFromBone", name );
            }

            this.DetachObjectImpl( obj );
            this.childObjectList.Remove( obj );

            return obj;
        }

        /// <summary>
        /// Detaches an object by reference.
        /// </summary>
        /// <param name="obj"></param>
        /// <remarks>
        /// Use this method to destroy a MovableObject which is attached to a bone of belonging this entity.
        /// But sometimes the object may be not in the child object list because it is a lod entity,
        /// this method can safely detect and ignore in this case and won't raise an exception.
        /// </remarks>
        public void DetachObjectFromBone( MovableObject obj )
        {
            for ( int i = 0; i < this.childObjectList.Count; i++ )
            {
                MovableObject child = this.childObjectList[ i ];
                if ( child == obj )
                {
                    this.DetachObjectImpl( obj );
                    this.childObjectList.Remove( obj );

                    // Trigger update of bounding box if necessary
                    if ( this.parentNode != null )
                    {
                        this.parentNode.NeedUpdate();
                    }
                    break;
                }
            }
        }

        public void DetachAllObjectsFromBone()
        {
            this.DetachAllObjectsImpl();

            // Trigger update of bounding box if necessary
            if ( this.parentNode != null )
            {
                this.parentNode.NeedUpdate();
            }
        }

        /// <summary>
        ///		Internal implementation of detaching a 'child' object from this entity and 
        ///		clearing the assignment of the parent node to the child entity.
        /// </summary>
        /// <param name="sceneObject">Object to detach.</param>
        protected void DetachObjectImpl( MovableObject pObject )
        {
            TagPoint tagPoint = (TagPoint) pObject.ParentNode;

            // free the TagPoint so we can reuse it later
            //TODO: NO idea what this does!
            this.skeletonInstance.FreeTagPoint( tagPoint );

            pObject.NotifyAttached( tagPoint, true );
        }

        protected void DetachAllObjectsImpl()
        {
            for ( int i = 0; i < this.childObjectList.Count; i++ )
            {
                MovableObject child = this.childObjectList[ i ];
                this.DetachObjectImpl( child );
            }
            this.childObjectList.Clear();
        }

        protected void AddSoftwareAnimationRequest( bool normalsAlso )
        {
            this.softwareAnimationRequests++;
            if ( normalsAlso )
            {
                this.softwareAnimationNormalsRequests++;
            }
        }

        protected void RemoveSoftwareAnimationRequest( bool normalsAlso )
        {
            if ( this.softwareAnimationRequests == 0 ||
                 ( normalsAlso && this.softwareAnimationNormalsRequests == 0 ) )
            {
                throw new Exception( "Attempt to remove nonexistant request, in Entity.RemoveSoftwareAnimationRequest" );
            }
            this.softwareAnimationRequests--;
            if ( normalsAlso )
            {
                this.softwareAnimationNormalsRequests--;
            }
        }

        /// <summary>
        ///		Internal method called to notify the object that it has been attached to a node.
        /// </summary>
        /// <param name="node">Scene node to which we are being attached.</param>
        internal override void NotifyAttached( Node node, bool isTagPoint )
        {
            base.NotifyAttached( node, isTagPoint );
            // Also notify LOD entities
            foreach ( Entity lodEntity in this.lodEntityList )
            {
                lodEntity.NotifyAttached( node, isTagPoint );
            }
        }

        /// <summary>
        ///		Used to build a list of sub-entities from the meshes located in the mesh.
        /// </summary>
        protected void BuildSubEntities()
        {
            // loop through the models meshes and create sub entities from them
            for ( int i = 0; i < this.mesh.SubMeshCount; i++ )
            {
                SubMesh subMesh = this.mesh.GetSubMesh( i );
                SubEntity sub = new SubEntity();
                sub.Parent = this;
                sub.SubMesh = subMesh;

                if ( subMesh.IsMaterialInitialized )
                {
                    sub.MaterialName = subMesh.MaterialName;
                }

                this.subEntityList.Add( sub );
            }
        }

        /// <summary>
        ///    Protected method to cache bone matrices from a skeleton.
        /// </summary>
        protected void CacheBoneMatrices()
        {
            ulong currentFrameCount = Root.Instance.CurrentFrameCount;

            if ( this.frameBonesLastUpdated[ 0 ] == currentFrameCount )
            {
                return;
            }

            // Get the appropriate meshes skeleton here
            // Can use lower LOD mesh skeleton if mesh LOD is manual
            // We make the assumption that lower LOD meshes will have
            //   fewer bones than the full LOD, therefore marix stack will be
            //   big enough.

            // Check for LOD usage
            if ( this.mesh.IsLodManual && this.meshLodIndex > 1 )
            {
                // use lower detail skeleton
                Mesh lodMesh = this.mesh.GetLodLevel( this.meshLodIndex ).manualMesh;

                if ( !lodMesh.HasSkeleton )
                {
                    this.numBoneMatrices = 0;
                    return;
                }
            }

            boneSetAnimStateMeter.Enter();
            this.skeletonInstance.SetAnimationState( this.animationState );
            boneSetAnimStateMeter.Exit();

            boneGetBonesMeter.Enter();
            this.skeletonInstance.GetBoneMatrices( this.boneMatrices );
            boneGetBonesMeter.Exit();
            this.frameBonesLastUpdated[ 0 ] = currentFrameCount;

            // TODO: Skeleton instance sharing

            // update the child object's transforms
            for ( int i = 0; i < this.childObjectList.Count; i++ )
            {
                MovableObject child = this.childObjectList[ i ];
                child.ParentNode.Update( true, true );
            }

            // apply the current world transforms to these too, since these are used as
            // replacement world matrices
            Matrix4 worldXform = this.ParentNodeFullTransform;
            this.numBoneMatrices = this.skeletonInstance.BoneCount;

            boneTransformMeter.Enter();
            for ( int i = 0; i < this.numBoneMatrices; i++ )
            {
                this.boneMatrices[ i ] = worldXform * this.boneMatrices[ i ];
            }
            boneTransformMeter.Exit();
        }

        /// <summary>
        ///		Internal method - given vertex data which could be from the <see cref="Mesh"/> or 
        ///		any <see cref="SubMesh"/>, finds the temporary blend copy.
        /// </summary>
        /// <param name="originalData"></param>
        /// <returns></returns>
        protected VertexData FindBlendedVertexData( VertexData originalData )
        {
            if ( originalData == this.mesh.SharedVertexData )
            {
                return this.HasSkeleton ? this.skelAnimVertexData : this.softwareVertexAnimVertexData;
            }

            for ( int i = 0; i < this.subEntityList.Count; i++ )
            {
                SubEntity se = this.subEntityList[ i ];
                if ( originalData == se.SubMesh.vertexData )
                {
                    return this.HasSkeleton ? se.SkelAnimVertexData : se.SoftwareVertexAnimVertexData;
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
            if ( original == this.mesh.SharedVertexData )
            {
                return null;
            }

            for ( int i = 0; i < this.subEntityList.Count; i++ )
            {
                SubEntity subEnt = this.subEntityList[ i ];

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
        public void UpdateAnimation()
        {
          
            if ( !this.HasSkeleton && !this.mesh.HasVertexAnimation ) 
            { 
                return; 
            }

            // we only do these tasks if they have not already been done this frame
            Root root = Root.Instance;
            ulong currentFrameNumber = root.CurrentFrameCount;
            bool stencilShadows = false;
            if ( this.CastShadows && root.SceneManager != null )
            {
                stencilShadows = root.SceneManager.IsShadowTechniqueStencilBased;
            }
            bool swAnimation = !this.hardwareAnimation || stencilShadows || this.softwareAnimationRequests > 0;
            // Blend normals in s/w only if we're not using h/w animation,
            // since shadows only require positions
            bool blendNormals = !this.hardwareAnimation || this.softwareAnimationNormalsRequests > 0;
            bool animationDirty = this.frameAnimationLastUpdated != currentFrameNumber
                    // 				                  || (HasSkeleton && Skeleton.ManualBonesDirty)
                    ;
            if ( animationDirty ||
                 ( swAnimation && this.mesh.HasVertexAnimation && !this.TempVertexAnimBuffersBound() ) ||
                 ( swAnimation && this.HasSkeleton && !this.TempSkelAnimBuffersBound( blendNormals ) ) )
            {
                if ( this.mesh.HasVertexAnimation )
                {
                    if ( swAnimation )
                    {
                        // grab & bind temporary buffer for positions
                        if ( this.softwareVertexAnimVertexData != null &&
                             this.mesh.SharedVertexDataAnimationType != VertexAnimationType.None )
                        {
                            this.tempVertexAnimInfo.CheckoutTempCopies( true, false, false, false );
                            // NB we suppress hardware upload while doing blend if we're
                            // hardware animation, because the only reason for doing this
                            // is for shadow, which need only be uploaded then
                            this.tempVertexAnimInfo.BindTempCopies( this.softwareVertexAnimVertexData,
                                                                    this.hardwareAnimation );
                        }
                        for ( int i = 0; i < this.subEntityList.Count; i++ )
                        {
                            SubEntity subEntity = this.subEntityList[ i ];
                            if ( subEntity.IsVisible && subEntity.SoftwareVertexAnimVertexData != null &&
                                 subEntity.SubMesh.VertexAnimationType != VertexAnimationType.None )
                            {
                                subEntity.TempVertexAnimInfo.CheckoutTempCopies( true, false, false, false );
                                subEntity.TempVertexAnimInfo.BindTempCopies( subEntity.SoftwareVertexAnimVertexData,
                                                                             this.hardwareAnimation );
                            }
                        }
                    }
                    this.ApplyVertexAnimation( this.hardwareAnimation, stencilShadows );
                }
                if ( this.HasSkeleton )
                {
                    copyBoneMeter.Enter();
                    this.CacheBoneMatrices();
                    copyBoneMeter.Exit();

                    if ( swAnimation )
                    {
                        bool blendTangents = blendNormals;
                        bool blendBinormals = blendNormals;
                        blendedVertexMeter.Enter();
                        if ( this.skelAnimVertexData != null )
                        {
                            // Blend shared geometry
                            // NB we suppress hardware upload while doing blend if we're
                            // hardware animation, because the only reason for doing this
                            // is for shadow, which need only be uploaded then
                            entitySWBlendMeter.Enter();
                            this.tempSkelAnimInfo.CheckoutTempCopies( true, blendNormals, blendTangents, blendBinormals );
                            this.tempSkelAnimInfo.BindTempCopies( this.skelAnimVertexData, this.hardwareAnimation );
                            // Blend, taking source from either mesh data or morph data
                            Mesh.SoftwareVertexBlend(
                                    ( this.mesh.SharedVertexDataAnimationType != VertexAnimationType.None
                                              ?
                                                      this.softwareVertexAnimVertexData
                                              : this.mesh.SharedVertexData ),
                                    this.skelAnimVertexData,
                                    this.boneMatrices,
                                    blendNormals,
                                    blendTangents,
                                    blendBinormals );
                            entitySWBlendMeter.Exit();
                        }
                        blendedVertexMeter.Exit();

                        // Now check the per subentity vertex data to see if it needs to be
                        // using software blend
                        for ( int i = 0; i < this.subEntityList.Count; i++ )
                        {
                            // Blend dedicated geometry
                            SubEntity subEntity = this.subEntityList[ i ];
                            if ( subEntity.IsVisible && subEntity.SkelAnimVertexData != null )
                            {
                                subEntitySWBlendMeter.Enter();
                                subEntity.TempSkelAnimInfo.CheckoutTempCopies( true,
                                                                               blendNormals,
                                                                               blendTangents,
                                                                               blendBinormals );
                                subEntity.TempSkelAnimInfo.BindTempCopies( subEntity.SkelAnimVertexData,
                                                                           this.hardwareAnimation );
                                // Blend, taking source from either mesh data or morph data
                                Mesh.SoftwareVertexBlend(
                                        ( subEntity.SubMesh.VertexAnimationType != VertexAnimationType.None
                                                  ?
                                                          subEntity.SoftwareVertexAnimVertexData
                                                  : subEntity.SubMesh.vertexData ),
                                        subEntity.SkelAnimVertexData,
                                        this.boneMatrices,
                                        blendNormals,
                                        blendTangents,
                                        blendBinormals );
                                subEntitySWBlendMeter.Exit();
                            }
                        }
                    }
                }

                // trigger update of bounding box if necessary
                if ( this.childObjectList.Count != 0 )
                {
                    this.parentNode.NeedUpdate();
                }

                // remember the last frame count
                this.frameAnimationLastUpdated = currentFrameNumber;
            }
            // 			// Need to update the child object's transforms when animation dirty
            // 			// or parent node transform has altered.
            // 			if (HasSkeleton &&
            // 				(animationDirty || lastParentXform != ParentNodeFullTransform)) {
            // 				// Cache last parent transform for next frame use too.
            // 				lastParentXform = ParentNodeFullTransform;

            // 				// update the child object's transforms
            // 				for(int i = 0; i < childObjectList.Count; i++) {
            // 					MovableObject child = childObjectList[i];
            // 					child.ParentNode.Update(true, true);
            // 				}

            // 				// Also calculate bone world matrices, since are used as replacement world matrices,
            // 				// but only if it's used (when using hardware animation and skeleton animated).
            // 				if (hwAnimation && skeletonAnimated) {
            // 					numBoneMatrices = skeletonInstance.BoneCount;

            // 					// Allocate bone world matrices on demand, for better memory footprint
            // 					// when using software animation.
            // 					if (boneWorldMatrices) {
            // 						boneWorldMatrices = new Matrix4[numBoneMatrices];
            // 					}
            // 					for(int i = 0; i < numBoneMatrices; i++) {
            // 						boneWorldMatrices[i] = lastParentWorldXform * boneMatrices[i];
            // 					}
            // 				}
            // 			}
        }

        /// <summary>
        ///     Initialise the hardware animation elements for given vertex data
        /// </summary>
        private void InitHardwareAnimationElements( VertexData vdata, ushort numberOfElements )
        {
            if ( vdata.HWAnimationDataList.Count < numberOfElements )
            {
                vdata.AllocateHardwareAnimationElements( numberOfElements );
            }
            // Initialise parametrics incase we don't use all of them
            for ( int i = 0; i < vdata.HWAnimationDataList.Count; i++ )
            {
                vdata.HWAnimationDataList[ i ].Parametric = 0.0f;
            }
            // reset used count
            vdata.HWAnimDataItemsUsed = 0;
        }

        /// <summary>
        ///     Apply vertex animation
        /// </summary>
        private void ApplyVertexAnimation( bool hardwareAnimation, bool stencilShadows )
        {
            bool swAnim = !hardwareAnimation || stencilShadows || ( this.softwareAnimationRequests > 0 );

            // make sure we have enough hardware animation elements to play with
            if ( hardwareAnimation )
            {
                if ( this.hardwareVertexAnimVertexData != null &&
                     this.mesh.SharedVertexDataAnimationType != VertexAnimationType.None )
                {
                    this.InitHardwareAnimationElements( this.hardwareVertexAnimVertexData,
                                                        ( this.mesh.SharedVertexDataAnimationType
                                                          == VertexAnimationType.Pose )
                                                                ?
                                                                        this.hardwarePoseCount
                                                                : (ushort) 1 );
                }
                for ( int i = 0; i < this.subEntityList.Count; i++ )
                {
                    SubEntity subEntity = this.subEntityList[ i ];
                    SubMesh subMesh = subEntity.SubMesh;
                    VertexAnimationType type = subMesh.VertexAnimationType;
                    if ( type != VertexAnimationType.None && !subMesh.useSharedVertices )
                    {
                        this.InitHardwareAnimationElements( subEntity.HardwareVertexAnimVertexData,
                                                            ( type == VertexAnimationType.Pose )
                                                                    ? subEntity.HardwarePoseCount
                                                                    : (ushort) 1 );
                    }
                }
            }
            else
            {
                // May be blending multiple poses in software
                // Suppress hardware upload of buffers
                if ( this.softwareVertexAnimVertexData != null &&
                     this.mesh.SharedVertexDataAnimationType == VertexAnimationType.Pose )
                {
                    VertexElement elem =
                            this.softwareVertexAnimVertexData.vertexDeclaration.FindElementBySemantic(
                                    VertexElementSemantic.Position );
                    HardwareVertexBuffer buf =
                            this.softwareVertexAnimVertexData.vertexBufferBinding.GetBuffer( elem.Source );
                    buf.SuppressHardwareUpdate( true );
                }
                for ( int i = 0; i < this.subEntityList.Count; i++ )
                {
                    SubEntity subEntity = this.subEntityList[ i ];
                    SubMesh subMesh = subEntity.SubMesh;
                    if ( !subMesh.useSharedVertices && subMesh.VertexAnimationType == VertexAnimationType.Pose )
                    {
                        VertexData data = subEntity.SoftwareVertexAnimVertexData;
                        VertexElement elem =
                                data.vertexDeclaration.FindElementBySemantic( VertexElementSemantic.Position );
                        HardwareVertexBuffer buf = data.vertexBufferBinding.GetBuffer( elem.Source );
                        buf.SuppressHardwareUpdate( true );
                    }
                }
            }

            // Now apply the animation(s)
            // Note - you should only apply one morph animation to each set of vertex data
            // at once; if you do more, only the last one will actually apply
            this.MarkBuffersUnusedForAnimation();
            foreach ( AnimationState state in this.animationState.EnabledAnimationStates )
            {
                Animation anim = this.mesh.GetAnimation( state.Name );
                if ( anim != null )
                {
                    anim.Apply( this,
                                state.Time,
                                state.Weight,
                                swAnim,
                                hardwareAnimation );
                }
            }
            // Deal with cases where no animation applied
            this.RestoreBuffersForUnusedAnimation( hardwareAnimation );

            // Unsuppress hardware upload if we suppressed it
            if ( !hardwareAnimation )
            {
                if ( this.softwareVertexAnimVertexData != null &&
                     this.mesh.SharedVertexDataAnimationType == VertexAnimationType.Pose )
                {
                    VertexElement elem =
                            this.softwareVertexAnimVertexData.vertexDeclaration.FindElementBySemantic(
                                    VertexElementSemantic.Position );
                    HardwareVertexBuffer buf =
                            this.softwareVertexAnimVertexData.vertexBufferBinding.GetBuffer( elem.Source );
                    buf.SuppressHardwareUpdate( false );
                }
                for ( int i = 0; i < this.subEntityList.Count; i++ )
                {
                    SubEntity subEntity = this.subEntityList[ i ];
                    SubMesh subMesh = subEntity.SubMesh;
                    if ( !subMesh.useSharedVertices &&
                         subMesh.VertexAnimationType == VertexAnimationType.Pose )
                    {
                        VertexData data = subEntity.SoftwareVertexAnimVertexData;
                        VertexElement elem =
                                data.vertexDeclaration.FindElementBySemantic( VertexElementSemantic.Position );
                        HardwareVertexBuffer buf = data.vertexBufferBinding.GetBuffer( elem.Source );
                        buf.SuppressHardwareUpdate( false );
                    }
                }
            }
        }

        /// <summary>
        ///     Mark all vertex data as so far unanimated.
        /// </summary>
        protected void MarkBuffersUnusedForAnimation()
        {
            this.vertexAnimationAppliedThisFrame = false;
            for ( int i = 0; i < this.subEntityList.Count; i++ )
            {
                SubEntity subEntity = this.subEntityList[ i ];
                subEntity.MarkBuffersUnusedForAnimation();
            }
        }

        /// <summary>
        ///     Mark just this vertex data as animated. 
        /// </summary>
        public void MarkBuffersUsedForAnimation()
        {
            this.vertexAnimationAppliedThisFrame = true;
            // no cascade
        }

        /// <summary>
        ///     Internal method to restore original vertex data where we didn't 
        ///     perform any vertex animation this frame.
        /// </summary>
        protected void RestoreBuffersForUnusedAnimation( bool hardwareAnimation )
        {
            // Rebind original positions if:
            //  We didn't apply any animation and
            //    We're morph animated (hardware binds keyframe, software is missing)
            //    or we're pose animated and software (hardware is fine, still bound)
            if ( this.mesh.SharedVertexData != null &&
                 !this.vertexAnimationAppliedThisFrame &&
                 ( !hardwareAnimation || this.mesh.SharedVertexDataAnimationType == VertexAnimationType.Morph ) )
            {
                VertexElement srcPosElem =
                        this.mesh.SharedVertexData.vertexDeclaration.FindElementBySemantic(
                                VertexElementSemantic.Position );
                HardwareVertexBuffer srcBuf =
                        this.mesh.SharedVertexData.vertexBufferBinding.GetBuffer( srcPosElem.Source );

                // Bind to software
                VertexElement destPosElem =
                        this.softwareVertexAnimVertexData.vertexDeclaration.FindElementBySemantic(
                                VertexElementSemantic.Position );
                this.softwareVertexAnimVertexData.vertexBufferBinding.SetBinding( destPosElem.Source, srcBuf );
            }

            for ( int i = 0; i < this.subEntityList.Count; i++ )
            {
                SubEntity subEntity = this.subEntityList[ i ];
                subEntity.RestoreBuffersForUnusedAnimation( hardwareAnimation );
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
        public AnimationStateSet GetAllAnimationStates()
        {
            return this.animationState;
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
            Debug.Assert( this.animationState.HasAnimationState( name ), "animationState.ContainsKey(name)" );

            return this.animationState.GetAnimationState( name );
        }

        public bool TempVertexAnimBuffersBound()
        {
            // Do we still have temp buffers for software vertex animation bound?
            bool ret = true;
            if ( this.mesh.SharedVertexData != null &&
                 this.mesh.SharedVertexDataAnimationType != VertexAnimationType.None )
            {
                ret = ret && this.tempVertexAnimInfo.BuffersCheckedOut( true, false );
            }
            for ( int i = 0; i < this.subEntityList.Count; i++ )
            {
                SubEntity subEntity = this.subEntityList[ i ];
                if ( !subEntity.SubMesh.useSharedVertices &&
                     subEntity.SubMesh.VertexAnimationType != VertexAnimationType.None )
                {
                    ret = ret && subEntity.TempVertexAnimInfo.BuffersCheckedOut( true, false );
                }
            }
            return ret;
        }

        public bool TempSkelAnimBuffersBound( bool requestNormals )
        {
            // Do we still have temp buffers for software skeleton animation bound?
            if ( this.skelAnimVertexData != null )
            {
                if ( !this.tempSkelAnimInfo.BuffersCheckedOut( true, requestNormals ) )
                {
                    return false;
                }
            }
            for ( int i = 0; i < this.subEntityList.Count; i++ )
            {
                SubEntity subEntity = this.subEntityList[ i ];
                if ( subEntity.IsVisible && subEntity.skelAnimVertexData != null )
                {
                    if ( !subEntity.TempSkelAnimInfo.BuffersCheckedOut( true, requestNormals ) )
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public VertexData GetVertexDataForBinding()
        {
            VertexDataBindChoice c =
                    this.ChooseVertexDataForBinding( this.mesh.SharedVertexDataAnimationType != VertexAnimationType.None );
            switch ( c )
            {
                case VertexDataBindChoice.Original:
                    return this.mesh.SharedVertexData;
                case VertexDataBindChoice.HardwareMorph:
                    return this.hardwareVertexAnimVertexData;
                case VertexDataBindChoice.SoftwareMorph:
                    return this.softwareVertexAnimVertexData;
                case VertexDataBindChoice.SoftwareSkeletal:
                    return this.skelAnimVertexData;
            }
            ;
            // keep compiler happy
            return this.mesh.SharedVertexData;
        }

        //-----------------------------------------------------------------------
        public VertexDataBindChoice ChooseVertexDataForBinding( bool vertexAnim )
        {
            if ( this.HasSkeleton )
            {
                if ( !this.hardwareAnimation )
                {
                    // all software skeletal binds same vertex data
                    // may be a 2-stage s/w transform including morph earlier though
                    return VertexDataBindChoice.SoftwareSkeletal;
                }
                else if ( vertexAnim )
                {
                    // hardware morph animation
                    return VertexDataBindChoice.HardwareMorph;
                }
                else
                {
                    // hardware skeletal, no morphing
                    return VertexDataBindChoice.Original;
                }
            }
            else if ( vertexAnim )
            {
                // morph only, no skeletal
                if ( this.hardwareAnimation )
                {
                    return VertexDataBindChoice.HardwareMorph;
                }
                else
                {
                    return VertexDataBindChoice.SoftwareMorph;
                }
            }
            else
            {
                return VertexDataBindChoice.Original;
            }
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

        private static TimingMeter copyAnimationMeter = MeterManager.GetMeter( "Copy Animation", "Entity Queue" );
        private static TimingMeter updateAnimationMeter = MeterManager.GetMeter( "Update Animation", "Entity Queue" );
        private static TimingMeter updateChildMeter = MeterManager.GetMeter( "Update Child", "Entity Queue" );

        public override EdgeData GetEdgeList( int lodIndex )
        {
            return this.mesh.GetEdgeList( lodIndex );
        }

        public override void NotifyCurrentCamera( Camera camera )
        {
            if ( this.parentNode != null )
            {
                float squaredDepth = this.parentNode.GetSquaredViewDepth( camera );

                // Adjust this depth by the entity bias factor
                float temp = squaredDepth * this.meshLodFactorInv;

                // Now adjust it by the camera bias
                temp = temp * camera.InverseLodBias;

                // Get the index at this biased depth
                this.meshLodIndex = this.mesh.GetLodIndexSquaredDepth( temp );

                // Apply maximum detail restriction (remember lower = higher detail)
                this.meshLodIndex = (int) Utility.Max( this.maxMeshLodIndex, this.meshLodIndex );

                // Apply minimum detail restriction (remember higher = lower detail)
                this.meshLodIndex = (int) Utility.Min( this.minMeshLodIndex, this.meshLodIndex );

                // now do material LOD
                // adjust this depth by the entity bias factor
                temp = squaredDepth * this.materialLodFactorInv;

                // now adjust it by the camera bias
                temp = temp * camera.InverseLodBias;

                // apply the material LOD to all sub entities
                for ( int i = 0; i < this.subEntityList.Count; i++ )
                {
                    // get the index at this biased depth
                    SubEntity subEnt = this.subEntityList[ i ];

                    int idx = subEnt.Material.GetLodIndexSquaredDepth( temp );

                    // Apply maximum detail restriction (remember lower = higher detail)
                    idx = (int) Utility.Max( this.maxMaterialLodIndex, idx );
                    // Apply minimum detail restriction (remember higher = lower detail)
                    subEnt.materialLodIndex = (int) Utility.Min( this.minMaterialLodIndex, idx );
                }
            }

            // Notify child objects (tag points)
            for ( int i = 0; i < this.childObjectList.Count; i++ )
            {
                MovableObject child = this.childObjectList[ i ];
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
            Debug.Assert( index < this.subEntityList.Count, "index < subEntityList.Count" );

            return this.subEntityList[ index ];
        }

        /// <summary>
        ///     Gets a sub entity of this mesh with the specified name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public SubEntity GetSubEntity( string name )
        {
            for ( int i = 0; i < this.subEntityList.Count; i++ )
            {
                SubEntity sub = this.subEntityList[ i ];

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
            this.hardwareAnimation = false;
            this.vertexProgramInUse = false; // assume false because we just assign this
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
                    this.vertexProgramInUse = true;

                    if ( this.HasSkeleton )
                    {
                        // All materials must support skinning for us to consider using
                        // hardware animation - if one fails we use software
                        bool skeletallyAnimated = p.VertexProgram.IsSkeletalAnimationIncluded;
                        subEntity.HardwareSkinningEnabled = skeletallyAnimated;
                        subEntity.VertexProgramInUse = true;
                        if ( firstPass )
                        {
                            this.hardwareAnimation = skeletallyAnimated;
                            firstPass = false;
                        }
                        else
                        {
                            this.hardwareAnimation = this.hardwareAnimation && skeletallyAnimated;
                        }
                    }

                    VertexAnimationType animType = VertexAnimationType.None;
                    if ( subEntity.SubMesh.useSharedVertices )
                    {
                        animType = this.mesh.SharedVertexDataAnimationType;
                    }
                    else
                    {
                        animType = subEntity.SubMesh.VertexAnimationType;
                    }
                    if ( animType == VertexAnimationType.Morph )
                    {
                        // All materials must support morph animation for us to consider using
                        // hardware animation - if one fails we use software
                        if ( firstPass )
                        {
                            this.hardwareAnimation = p.VertexProgram.IsMorphAnimationIncluded;
                            firstPass = false;
                        }
                        else
                        {
                            this.hardwareAnimation = this.hardwareAnimation && p.VertexProgram.IsMorphAnimationIncluded;
                        }
                    }
                    else if ( animType == VertexAnimationType.Pose )
                    {
                        // All materials must support pose animation for us to consider using
                        // hardware animation - if one fails we use software
                        if ( firstPass )
                        {
                            this.hardwareAnimation = p.VertexProgram.PoseAnimationCount > 0;
                            if ( subEntity.SubMesh.useSharedVertices )
                            {
                                this.hardwarePoseCount = p.VertexProgram.PoseAnimationCount;
                            }
                            else
                            {
                                subEntity.HardwarePoseCount = p.VertexProgram.PoseAnimationCount;
                            }
                            firstPass = false;
                        }
                        else
                        {
                            this.hardwareAnimation = this.hardwareAnimation && p.VertexProgram.PoseAnimationCount > 0;
                            if ( subEntity.SubMesh.useSharedVertices )
                            {
                                this.hardwarePoseCount =
                                        (ushort)
                                        Utility.Max( this.hardwarePoseCount, p.VertexProgram.PoseAnimationCount );
                            }
                            else
                            {
                                subEntity.HardwarePoseCount = (ushort) Utility.Max( subEntity.HardwarePoseCount,
                                                                                    p.VertexProgram.PoseAnimationCount );
                            }
                        }
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
            this.materialLodFactorInv = 1.0f / factor;
            this.maxMaterialLodIndex = maxDetailIndex;
            this.minMaterialLodIndex = minDetailIndex;
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
            this.meshLodFactorInv = 1.0f / factor;
            this.maxMeshLodIndex = maxDetailIndex;
            this.minMeshLodIndex = minDetailIndex;
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
        public void CopyAnimationStateSubset( AnimationStateSet target, AnimationStateSet source )
        {
            foreach ( AnimationState targetState in target.Values )
            {
                AnimationState sourceState = source.GetAnimationState( targetState.Name );

                if ( sourceState == null )
                {
                    throw new AxiomException( "No animation entry found named '{0}'.", targetState.Name );
                }
                else
                {
                    targetState.CopyFrom( sourceState );
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
            if ( this.meshLodIndex > 0 && this.mesh.IsLodManual )
            {
                Debug.Assert( this.meshLodIndex - 1 < this.lodEntityList.Count,
                              "No LOD EntityList - did you build the manual LODs after creating the entity?" );

                Entity lodEnt = this.lodEntityList[ this.meshLodIndex - 1 ];

                // index - 1 as we skip index 0 (original LOD)
                if ( this.HasSkeleton && lodEnt.HasSkeleton )
                {
                    // Copy the animation state set to lod entity, we assume the lod
                    // entity only has a subset animation states
                    copyAnimationMeter.Enter();
                    this.CopyAnimationStateSubset( lodEnt.animationState, this.animationState );
                    copyAnimationMeter.Exit();
                }

                lodEnt.UpdateRenderQueue( queue );
                return;
            }

            // add all visible sub entities to the render queue
            for ( int i = 0; i < this.subEntityList.Count; i++ )
            {
                if ( this.subEntityList[ i ].IsVisible )
                {
                    queue.AddRenderable( this.subEntityList[ i ], RenderQueue.DEFAULT_PRIORITY, this.renderQueueID );
                }
            }

            // Since we know we're going to be rendered, take this opportunity to 
            // update the animation
            if ( this.HasSkeleton || this.mesh.HasVertexAnimation )
            {
                updateAnimationMeter.Enter();
                if ( MeterManager.Collecting )
                {
                    MeterManager.AddInfoEvent( string.Format( "Updating animation for mesh {0}, skeleton {1}",
                                                              this.mesh.Name,
                                                              this.mesh.SkeletonName ) );
                }
                this.UpdateAnimation();
                updateAnimationMeter.Exit();

                // Update render queue with child objects (tag points)
                for ( int i = 0; i < this.childObjectList.Count; i++ )
                {
                    MovableObject child = this.childObjectList[ i ];

                    if ( child.IsVisible )
                    {
                        updateChildMeter.Enter();
                        child.UpdateRenderQueue( queue );
                        updateChildMeter.Exit();
                    }
                }
            }
        }

        /// <summary>
        ///		Internal method for preparing this Entity for use in animation.
        /// </summary>
        protected internal void PrepareTempBlendedBuffers()
        {
            if ( this.skelAnimVertexData != null )
            {
                this.skelAnimVertexData = null;
            }
            if ( this.softwareVertexAnimVertexData != null )
            {
                this.softwareVertexAnimVertexData = null;
            }
            if ( this.hardwareVertexAnimVertexData != null )
            {
                this.hardwareVertexAnimVertexData = null;
            }

            if ( this.mesh.HasVertexAnimation )
            {
                // Shared data
                if ( this.mesh.SharedVertexData != null &&
                     this.mesh.SharedVertexDataAnimationType != VertexAnimationType.None )
                {
                    // Create temporary vertex blend info
                    // Prepare temp vertex data if needed
                    // Clone without copying data, don't remove any blending info
                    // (since if we skeletally animate too, we need it)
                    this.softwareVertexAnimVertexData = this.mesh.SharedVertexData.Clone( false );
                    this.ExtractTempBufferInfo( this.softwareVertexAnimVertexData, this.tempVertexAnimInfo );

                    // Also clone for hardware usage, don't remove blend info since we'll
                    // need it if we also hardware skeletally animate
                    this.hardwareVertexAnimVertexData = this.mesh.SharedVertexData.Clone( false );
                }
            }

            if ( this.HasSkeleton )
            {
                // shared data
                if ( this.mesh.SharedVertexData != null )
                {
                    // Create temporary vertex blend info
                    // Prepare temp vertex data if needed
                    // Clone without copying data, remove blending info
                    // (since blend is performed in software)
                    this.skelAnimVertexData = this.CloneVertexDataRemoveBlendInfo( this.mesh.SharedVertexData );
                    this.ExtractTempBufferInfo( this.skelAnimVertexData, this.tempSkelAnimInfo );
                }
            }

            // prepare temp blending buffers for subentites as well
            for ( int i = 0; i < this.SubEntityCount; i++ )
            {
                this.subEntityList[ i ].PrepareTempBlendBuffers();
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
            info.ExtractFrom( sourceData );
        }

        public override IEnumerator GetShadowVolumeRenderableEnumerator( ShadowTechnique technique,
                                                                         Light light,
                                                                         HardwareIndexBuffer indexBuffer,
                                                                         bool extrudeVertices,
                                                                         float extrusionDistance,
                                                                         int flags )
        {
            Debug.Assert( indexBuffer != null, "Only external index buffers are supported right now" );
            Debug.Assert( indexBuffer.Type == IndexType.Size16, "Only 16-bit indexes supported for now" );

            // Potentially delegate to LOD entity
            if ( this.meshLodIndex > 0 && this.mesh.IsLodManual )
            {
                Debug.Assert( this.meshLodIndex - 1 < this.lodEntityList.Count,
                              "No LOD EntityList - did you build the manual LODs after creating the entity?" );

                Entity lodEnt = this.lodEntityList[ this.meshLodIndex - 1 ];

                // index - 1 as we skip index 0 (original LOD)
                if ( this.HasSkeleton && lodEnt.HasSkeleton )
                {
                    // Copy the animation state set to lod entity, we assume the lod
                    // entity only has a subset animation states
                    copyAnimationMeter.Enter();
                    this.CopyAnimationStateSubset( lodEnt.animationState, this.animationState );
                    copyAnimationMeter.Exit();
                }

                return lodEnt.GetShadowVolumeRenderableEnumerator( technique,
                                                                   light,
                                                                   indexBuffer,
                                                                   extrudeVertices,
                                                                   extrusionDistance,
                                                                   flags );
            }

            // Prep mesh if required
            // NB This seems to result in memory corruptions, having problems
            // tracking them down. For now, ensure that shadows are enabled
            // before any entities are created
            if ( !this.mesh.IsPreparedForShadowVolumes )
            {
                this.mesh.PrepareForShadowVolume();
                // reset frame last updated to force update of buffers
                this.frameAnimationLastUpdated = 0;
                // re-prepare buffers
                this.PrepareTempBlendedBuffers();
            }

            // Update any animation 
            this.UpdateAnimation();

            // Calculate the object space light details
            Vector4 lightPos = light.GetAs4DVector();

            // Only use object-space light if we're not doing transforms
            // Since when animating the positions are already transformed into 
            // world space so we need world space light position
            bool isAnimated = this.HasSkeleton || this.mesh.HasVertexAnimation;
            if ( !isAnimated )
            {
                Matrix4 world2Obj = this.parentNode.FullTransform.Inverse();

                lightPos = world2Obj * lightPos;
            }

            // We need to search the edge list for silhouette edges
            EdgeData edgeList = this.GetEdgeList();

            // Init shadow renderable list if required
            bool init = ( this.shadowRenderables.Count == 0 );

            if ( init )
            {
                this.shadowRenderables.Capacity = edgeList.edgeGroups.Count;
            }

            bool updatedSharedGeomNormals = false;

            EntityShadowRenderable esr = null;
            EdgeData.EdgeGroup egi;

            // note: using capacity for the loop since no items are in the list yet.
            // capacity is set to how large the collection will be in the end
            for ( int i = 0; i < this.shadowRenderables.Capacity; i++ )
            {
                egi = (EdgeData.EdgeGroup) edgeList.edgeGroups[ i ];
                VertexData data = ( isAnimated
                                            ? this.FindBlendedVertexData( egi.vertexData )
                                            :
                                                    egi.vertexData );
                if ( init )
                {
                    // Try to find corresponding SubEntity; this allows the 
                    // linkage of visibility between ShadowRenderable and SubEntity
                    SubEntity subEntity = this.FindSubEntityForVertexData( egi.vertexData );

                    // Create a new renderable, create a separate light cap if
                    // we're using hardware skinning since otherwise we get
                    // depth-fighting on the light cap
                    esr = new EntityShadowRenderable( this,
                                                      indexBuffer,
                                                      data,
                                                      subEntity.VertexProgramInUse || !extrudeVertices,
                                                      subEntity );

                    this.shadowRenderables.Add( esr );
                }
                else
                {
                    esr = (EntityShadowRenderable) this.shadowRenderables[ i ];

                    if ( this.HasSkeleton )
                    {
                        // If we have a skeleton, we have no guarantee that the position
                        // buffer we used last frame is the same one we used last frame
                        // since a temporary buffer is requested each frame
                        // therefore, we need to update the EntityShadowRenderable
                        // with the current position buffer
                        esr.RebindPositionBuffer( data, isAnimated );
                    }
                }

                // For animated entities we need to recalculate the face normals
                if ( isAnimated )
                {
                    if ( egi.vertexData != this.mesh.SharedVertexData || !updatedSharedGeomNormals )
                    {
                        // recalculate face normals
                        edgeList.UpdateFaceNormals( egi.vertexSet, esr.PositionBuffer );

                        // If we're not extruding in software we still need to update 
                        // the latter part of the buffer (the hardware extruded part)
                        // with the latest animated positions
                        if ( !extrudeVertices )
                        {
                            IntPtr srcPtr = esr.PositionBuffer.Lock( BufferLocking.Normal );
                            IntPtr destPtr = new IntPtr( srcPtr.ToInt64() + ( egi.vertexData.vertexCount * 12 ) );

                            // 12 = sizeof(float) * 3
                            Memory.Copy( srcPtr, destPtr, 12 * egi.vertexData.vertexCount );

                            esr.PositionBuffer.Unlock();
                        }

                        if ( egi.vertexData == this.mesh.SharedVertexData )
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
            this.UpdateEdgeListLightFacing( edgeList, lightPos );

            // Generate indexes and update renderables
            this.GenerateShadowVolume( edgeList, indexBuffer, light, this.shadowRenderables, flags );

            return this.shadowRenderables.GetEnumerator();
        }

        public override IEnumerator GetLastShadowVolumeRenderableEnumerator()
        {
            return this.shadowRenderables.GetEnumerator();
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
                return this.numBoneMatrices;
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
                this.fullBoundingBox = this.mesh.BoundingBox;
                this.fullBoundingBox.Merge( this.ChildObjectsBoundingBox );

                // don't need to scale here anymore

                return this.fullBoundingBox;
            }
        }

        public VertexData SkelAnimVertexData
        {
            get
            {
                return this.skelAnimVertexData;
            }
        }

        public bool IsSkeletonAnimated
        {
            get
            {
                return this.skeletonInstance != null &&
                       ( this.HasEnabledAnimationState
                       // 				 || skeletonInstance.HasManualBones
                       );
            }
        }

        public bool HasEnabledAnimationState
        {
            get
            {
                foreach ( AnimationState state in this.animationState.Values )
                {
                    if ( state.IsEnabled )
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        #endregion Properties

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Entity Clone( string newName )
        {
            // create a new entity using the current mesh (uses same instance, not a copy for speed)
            Entity clone = this.sceneMgr.CreateEntity( newName, this.mesh.Name );

            // loop through each subentity and set the material up for the clone
            for ( int i = 0; i < this.subEntityList.Count; i++ )
            {
                SubEntity subEntity = this.subEntityList[ i ];
                SubEntity cloneSubEntity = clone.GetSubEntity( i );
                cloneSubEntity.MaterialName = subEntity.MaterialName;
                cloneSubEntity.IsVisible = subEntity.IsVisible;
            }

            // copy the animation state as well
            if ( this.animationState != null )
            {
                clone.animationState = this.animationState.Clone();
            }

            return clone;
        }

        #region Nested Classes

        /// <summary>
        ///		Nested class to allow entity shadows.
        /// </summary>
        protected class EntityShadowRenderable : ShadowRenderable
        {
            #region Fields

            /// <summary>
            ///		Link to current vertex data used to bind (maybe changes)
            /// </summary>
            protected VertexData currentVertexData;

            /// <summary>
            ///		Original position buffer source binding.
            /// </summary>
            protected short originalPosBufferBinding;

            protected Entity parent;

            /// <summary>
            ///		Shared ref to the position buffer.
            /// </summary>
            protected HardwareVertexBuffer positionBuffer;

            /// <summary>
            ///		Link to SubEntity, only present if SubEntity has it's own geometry.
            /// </summary>
            protected SubEntity subEntity;

            /// <summary>
            ///		Shared ref to w-coord buffer (optional).
            /// </summary>
            protected HardwareVertexBuffer wBuffer;

            #endregion Fields

            #region Constructor

            public EntityShadowRenderable( Entity parent,
                                           HardwareIndexBuffer indexBuffer,
                                           VertexData vertexData,
                                           bool createSeperateLightCap,
                                           SubEntity subEntity )
                    : this( parent, indexBuffer, vertexData, createSeperateLightCap, subEntity, false )
            {
            }

            public EntityShadowRenderable( Entity parent,
                                           HardwareIndexBuffer indexBuffer,
                                           VertexData vertexData,
                                           bool createSeparateLightCap,
                                           SubEntity subEntity,
                                           bool isLightCap )
            {
                this.parent = parent;

                // Save link to vertex data
                this.currentVertexData = vertexData;

                // Initialise render op
                this.renderOp.indexData = new IndexData();
                this.renderOp.indexData.indexBuffer = indexBuffer;
                this.renderOp.indexData.indexStart = 0;
                // index start and count are sorted out later

                // Create vertex data which just references position component (and 2 component)
                this.renderOp.vertexData = new VertexData();
                this.renderOp.vertexData.vertexDeclaration =
                        HardwareBufferManager.Instance.CreateVertexDeclaration();
                this.renderOp.vertexData.vertexBufferBinding =
                        HardwareBufferManager.Instance.CreateVertexBufferBinding();

                // Map in position data
                this.renderOp.vertexData.vertexDeclaration.AddElement( 0,
                                                                       0,
                                                                       VertexElementType.Float3,
                                                                       VertexElementSemantic.Position );
                this.originalPosBufferBinding =
                        vertexData.vertexDeclaration.FindElementBySemantic( VertexElementSemantic.Position ).Source;

                this.positionBuffer = vertexData.vertexBufferBinding.GetBuffer( this.originalPosBufferBinding );
                this.renderOp.vertexData.vertexBufferBinding.SetBinding( 0, this.positionBuffer );

                // Map in w-coord buffer (if present)
                if ( vertexData.hardwareShadowVolWBuffer != null )
                {
                    this.renderOp.vertexData.vertexDeclaration.AddElement( 1,
                                                                           0,
                                                                           VertexElementType.Float1,
                                                                           VertexElementSemantic.TexCoords,
                                                                           0 );
                    this.wBuffer = vertexData.hardwareShadowVolWBuffer;
                    this.renderOp.vertexData.vertexBufferBinding.SetBinding( 1, this.wBuffer );
                }

                // Use same vertex start as input
                this.renderOp.vertexData.vertexStart = vertexData.vertexStart;

                if ( isLightCap )
                {
                    // Use original vertex count, no extrusion
                    this.renderOp.vertexData.vertexCount = vertexData.vertexCount;
                }
                else
                {
                    // Vertex count must take into account the doubling of the buffer,
                    // because second half of the buffer is the extruded copy
                    this.renderOp.vertexData.vertexCount = vertexData.vertexCount * 2;

                    if ( createSeparateLightCap )
                    {
                        // Create child light cap
                        this.lightCap = new EntityShadowRenderable( parent,
                                                                    indexBuffer,
                                                                    vertexData,
                                                                    false,
                                                                    subEntity,
                                                                    true );
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
                    return this.positionBuffer;
                }
            }

            /// <summary>
            ///		Gets a reference to the w-buffer in use by this renderable.
            /// </summary>
            public HardwareVertexBuffer WBuffer
            {
                get
                {
                    return this.wBuffer;
                }
            }

            #endregion Properties

            #region Methods

            /// <summary>
            ///		Rebind the source positions for temp buffer users.
            /// </summary>
            public void RebindPositionBuffer( VertexData vertexData, bool force )
            {
                if ( force || this.currentVertexData != vertexData )
                {
                    this.currentVertexData = vertexData;
                    this.positionBuffer =
                            this.currentVertexData.vertexBufferBinding.GetBuffer( this.originalPosBufferBinding );
                    this.renderOp.vertexData.vertexBufferBinding.SetBinding( 0, this.positionBuffer );
                    if ( this.lightCap != null )
                    {
                        ( (EntityShadowRenderable) this.lightCap ).RebindPositionBuffer( vertexData, force );
                    }
                }
            }

            #endregion Methods

            public override Quaternion WorldOrientation
            {
                get
                {
                    return this.parent.ParentNode.DerivedOrientation;
                }
            }

            public override Vector3 WorldPosition
            {
                get
                {
                    return this.parent.ParentNode.DerivedPosition;
                }
            }

            public override bool IsVisible
            {
                get
                {
                    if ( this.subEntity != null )
                    {
                        return this.subEntity.IsVisible;
                    }

                    return base.IsVisible;
                }
            }

            public override void GetWorldTransforms( Matrix4[] matrices )
            {
                if ( this.parent.BoneMatrixCount == 0 )
                {
                    matrices[ 0 ] = this.parent.ParentNodeFullTransform;
                }
                else
                {
                    // pretransformed
                    matrices[ 0 ] = Matrix4.Identity;
                }
            }
        }

        #endregion Nested Classes
    }

    #region MovableObjectFactory implementation

    public class EntityFactory : MovableObjectFactory
    {
        public const string TypeName = "Entity";

        public EntityFactory()
        {
            this.Type = EntityFactory.TypeName;
        }

        protected override MovableObject _createInstance( string name, NamedParameterList param )
        {
            // must have mesh parameter
            Mesh pMesh = null;
            if ( param != null )
            {
                if ( param.ContainsKey( "mesh" ) )
                {
                    if ( param[ "mesh" ] is Mesh )
                    {
                        pMesh = (Mesh) param[ "mesh" ];
                    }
                    else
                    {
                        pMesh = MeshManager.Instance.Load( param[ "mesh" ].ToString(), ResourceGroupManager.AutoDetectResourceGroupName );
                    }
                }
            }
            if ( pMesh == null )
            {
                throw new AxiomException( "'mesh' parameter required when constructing an Entity." );
            }
            Entity ent = new Entity( name, pMesh, null );
            ent.MovableType = this.Type;
            return ent;
        }

        public override void DestroyInstance( MovableObject obj )
        {
            ( (Entity) obj ).Dispose();
            obj = null;
        }
    }

    #endregion MovableObjectFactory implementation
}