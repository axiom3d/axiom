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
using Axiom.Collections;
using Axiom.Configuration;
using Axiom.Controllers;

using Axiom.Exceptions;
using Axiom.Gui;
using Axiom.MathLib;
using Axiom.MathLib.Collections;
using Axiom.Graphics;

namespace Axiom.Core {

    #region Delegate declarations
    /// <summary>
    ///		Delegate for speicfying the method signature for a render queue event.
    /// </summary>
    public delegate bool RenderQueueEvent(RenderQueueGroupID priority);

    #endregion

    /// <summary>
    /// Manages the rendering of a 'scene' i.e. a collection of primitives.
    /// </summary>
    /// <remarks>
    ///		This class defines the basic behavior of the 'Scene Manager' family. These classes will
    ///		organise the objects in the scene and send them to the rendering system, a subclass of
    ///		RenderSystem. This basic superclass does no sorting, culling or organising of any sort.
    ///    <p/>
    ///		Subclasses may use various techniques to organise the scene depending on how they are
    ///		designed (e.g. BSPs, octrees etc). As with other classes, methods marked as interanl are 
    ///		designed to be called by other classes in the engine, not by user applications.
    ///	 </remarks>
    public class SceneManager {
        #region Member variables

        /// <summary>A queue of objects for rendering.</summary>
        protected RenderQueue renderQueue;
        /// <summary>A reference to the current active render system..</summary>
        protected RenderSystem targetRenderSystem;
        /// <summary>Denotes whether or not the camera has been changed.</summary>
        protected bool hasCameraChanged;
        /// <summary>The ambient color, cached from the RenderSystem</summary>
        protected ColorEx ambientColor;
        /// <summary>A list of the valid cameras for this scene for easy lookup.</summary>
        protected CameraCollection cameraList;
        /// <summary>A list of lights in the scene for easy lookup.</summary>
        protected LightList lightList;
        /// <summary>A list of entities in the scene for easy lookup.</summary>
        protected internal EntityCollection entityList;
        /// <summary>A list of scene nodes (includes all in the scene graph).</summary>
        protected SceneNodeCollection sceneNodeList;
        /// <summary>A list of billboard set for easy lookup.</summary>
        protected BillboardSetCollection billboardSetList;
        /// <summary>A list of animations for easy lookup.</summary>
        protected AnimationCollection animationList;
        /// <summary>A list of animation states for easy lookup.</summary>
        protected AnimationStateCollection animationStateList;

        protected Matrix4[] xform = new Matrix4[256];

        /// <summary>A reference to the current camera being used for rendering.</summary>
        protected Camera camInProgress;
        /// <summary>The root of the scene graph heirarchy.</summary>
        protected SceneNode rootSceneNode;
        /// <summary>
        ///    Utility class for updating automatic GPU program params.
        /// </summary>
        protected AutoParamDataSource autoParamDataSource = new AutoParamDataSource();

        protected Entity skyPlaneEntity;
        protected Entity[] skyDomeEntities = new Entity[5];
        protected Entity[] skyBoxEntities = new Entity[6];

        protected SceneNode skyPlaneNode;
        protected SceneNode skyDomeNode;
        protected SceneNode skyBoxNode;
        // Sky plane
        protected bool isSkyPlaneEnabled;
        protected bool isSkyPlaneDrawnFirst;
        protected Plane skyPlane;
        // Sky box
        protected bool isSkyBoxEnabled;
        protected bool isSkyBoxDrawnFirst;
        protected Quaternion skyBoxOrientation;
        // Sky dome
        protected bool isSkyDomeEnabled;
        protected bool isSkyDomeDrawnFirst;
        protected Quaternion skyDomeOrientation;
        // Fog
        protected FogMode fogMode;
        protected ColorEx fogColor;
        protected float fogStart;
        protected float fogEnd;
        protected float fogDensity;

        /// <summary>Flag indicating whether SceneNodes will be rendered as a set of 3 axes.</summary>
        protected bool displayNodes;
        /// <summary>Flag that specifies whether scene nodes will have their bounding boxes rendered as a wire frame.</summary>
        protected bool showBoundingBoxes;

        /// <summary>Hashtable of options that can be used by this or any other scene manager.</summary>
        protected Hashtable optionList = new Hashtable();

        protected bool lastUsedFallback;
        protected static int lastNumTexUnitsUsed = 0;
        protected static RenderOperation op = new RenderOperation();
        protected static bool lastUsedVertexProgram;
        protected static bool lastUsedFragmentProgram;

        // cached fog settings
        protected static FogMode oldFogMode;
        protected static ColorEx oldFogColor;
        protected static float oldFogStart, oldFogEnd, oldFogDensity;
        protected bool lastViewWasIdentity, lastProjectionWasIdentity;

        #endregion

        #region Public events
        /// <summary>An event that will fire when a render queue is starting to be rendered.</summary>
        public event RenderQueueEvent QueueStarted;
        /// <summary>An event that will fire when a render queue is finished being rendered.</summary>
        public event RenderQueueEvent QueueEnded;
        #endregion

        #region Constructors

        public SceneManager() {
            // initialize all collections
            renderQueue = new RenderQueue();
            cameraList = new CameraCollection();
            lightList = new LightList();
            entityList = new EntityCollection();
            sceneNodeList = new SceneNodeCollection();
            billboardSetList = new BillboardSetCollection();
            animationList = new AnimationCollection();
            animationStateList = new AnimationStateCollection();

            // create the root scene node
            rootSceneNode = new SceneNode(this, "Root");

            // default to no fog
            fogMode = FogMode.None;
        }

        #endregion

        #region Virtual methods

        /// <summary>
        ///		Creates an instance of a SceneNode.
        /// </summary>
        /// <remarks>
        ///    Note that this does not add the SceneNode to the scene hierarchy.
        ///		This method is for convenience, since it allows an instance to
        ///		be created for which the SceneManager is responsible for
        ///		allocating and releasing memory, which is convenient in complex
        ///		scenes.
        ///		<p/>
        ///	    To include the returned SceneNode in the scene, use the AddChild
        ///		method of the SceneNode which is to be it's parent.
        ///		<p/>
        ///     Note that this method takes no parameters, and the node created is unnamed (it is
        ///     actually given a generated name, which you can retrieve if you want).
        ///     If you wish to create a node with a specific name, call the alternative method
        ///     which takes a name parameter.
        /// </remarks>
        /// <returns></returns>
        public virtual SceneNode CreateSceneNode() {
            SceneNode node = new SceneNode(this);
            sceneNodeList.Add(node);
            return node;
        }

        /// <summary>
        ///		Creates an instance of a SceneNode with a given name.
        /// </summary>
        /// <remarks>
        ///		Note that this does not add the SceneNode to the scene hierarchy.
        ///		This method is for convenience, since it allows an instance to
        ///		be created for which the SceneManager is responsible for
        ///		allocating and releasing memory, which is convenient in complex
        ///		scenes.
        ///		<p/>
        ///		To include the returned SceneNode in the scene, use the AddChild
        ///		method of the SceneNode which is to be it's parent.
        ///		<p/>
        ///		Note that this method takes a name parameter, which makes the node easier to
        ///		retrieve directly again later.
        /// </remarks>
        /// <param name="pName"></param>
        /// <returns></returns>
        public virtual SceneNode CreateSceneNode(string name) {
            SceneNode node = new SceneNode(this, name);
            sceneNodeList.Add(node);
            return node;
        }

        /// <summary>
        ///		Creates an animation which can be used to animate scene nodes.
        /// </summary>
        /// <remarks>
        ///		An animation is a collection of 'tracks' which over time change the position / orientation
        ///		of Node objects. In this case, the animation will likely have tracks to modify the position
        ///		/ orientation of SceneNode objects, e.g. to make objects move along a path.
        ///		<p/>
        ///		You don't need to use an Animation object to move objects around - you can do it yourself
        ///		using the methods of the Node in your application. However, when you need relatively
        ///		complex scripted animation, this is the class to use since it will interpolate between
        ///		keyframes for you and generally make the whole process easier to manage.
        ///		<p/>
        ///		A single animation can affect multiple Node objects (each AnimationTrack affects a single Node).
        ///		In addition, through animation blending a single Node can be affected by multiple animations,
        ///		although this is more useful when performing skeletal animation (see Skeleton.CreateAnimation).
        /// </remarks>
        /// <param name="name"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public virtual Animation CreateAnimation(string name, float length) {
            // create a new animation and record it locally
            Animation anim = new Animation(name, length);
            animationList.Add(anim);

            return anim;
        }

        /// <summary>
        ///		Create an AnimationState object for managing application of animations.
        /// </summary>
        /// <remarks>
        ///		You can create Animation objects for animating SceneNode obejcts using the
        ///		CreateAnimation method. However, in order to actually apply those animations
        ///		you have to call methods on Node and Animation in a particular order (namely
        ///		Node.ResetToInitialState and Animation.Apply). To make this easier and to
        ///		help track the current time position of animations, the AnimationState object
        ///		is provided. 
        ///		</p>
        ///		So if you don't want to control animation application manually, call this method,
        ///		update the returned object as you like every frame and let SceneManager apply 
        ///		the animation state for you.
        ///		<p/>
        ///		Remember, AnimationState objects are disabled by default at creation time. 
        ///		Turn them on when you want them using their Enabled property.
        ///		<p/>
        ///		Note that any SceneNode affected by this automatic animation will have it's state
        ///		reset to it's initial position before application of the animation. Unless specifically
        ///		modified using Node.SetInitialState the Node assumes it's initial state is at the
        ///		origin. If you want the base state of the SceneNode to be elsewhere, make your changes
        ///		to the node using the standard transform methods, then call SetInitialState to 
        ///		'bake' this reference position into the node.
        /// </remarks>
        /// <param name="animationName"></param>
        /// <returns></returns>
        public virtual AnimationState CreateAnimationState(string animationName) {
            // do we have this already?
            if(animationStateList.ContainsKey(animationName))
                throw new Axiom.Exceptions.AxiomException("Cannot create a duplicate AnimationState for an Animation.");

            if(!animationList.ContainsKey(animationName))
                throw new Axiom.Exceptions.AxiomException(string.Format("The name of a valid animation must be supplied when creating an AnimationState.  Animation '{0}' does not exist.", animationName));

            // get a reference to the sepcified animation
            Animation anim = animationList[animationName];

            // create new animation state
            AnimationState animState = new AnimationState(animationName, 0, anim.Length);

            // add it to our local list
            animationStateList.Add(animState);

            return animState;
        }

        /// <summary>
        ///		Overloaded method.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public virtual BillboardSet CreateBillboardSet(string name) {
            // return new billboardset with a default pool size of 20
            return CreateBillboardSet(name, 20);
        }

        /// <summary>
        ///		Creates a billboard set which can be uses for particles, sprites, etc.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="poolSize"></param>
        /// <returns></returns>
        public virtual BillboardSet CreateBillboardSet(string name, int poolSize) {
            BillboardSet billboardSet = new BillboardSet(name, poolSize);

            // keep a local copy
            billboardSetList.Add(name, billboardSet);

            return billboardSet;
        }

        /// <summary>
        ///		Creates a camera to be managed by this scene manager.
        /// </summary>
        /// <remarks>
        ///		This camera can be added to the scene at a later time using
        ///		the AttachObject method of the SceneNode class.
        ///	 </remarks>
        ///	 <param name="name"></param>
        /// <returns></returns>
        public virtual Camera CreateCamera(string name) {
            // create the camera and add it to our local list
            Camera camera = new Camera(name, this);
            cameraList.Add(camera);

            return camera;
        }

        /// <summary>
        ///		Create an Entity (instance of a discrete mesh).
        /// </summary>
        /// <param name="name">The name to be given to the entity (must be unique).</param>
        /// <param name="meshName">The name of the mesh to load.  Will be loaded if not already.</param>
        /// <returns></returns>
        public virtual Entity CreateEntity(string name, string meshName) {
            if(entityList.ContainsKey(name))
                throw new Axiom.Exceptions.AxiomException(string.Format("An entity with the name '{0}' already exists in the scene.", name));

            Mesh mesh = MeshManager.Instance.Load(meshName);

            // create a new entitiy
            Entity entity = new Entity(name, mesh, this);

            // add it to our local list
            entityList.Add(entity);

            return entity;
        }

        /// <summary>
        ///		Create an Entity (instance of a discrete mesh).
        /// </summary>
        /// <param name="name">The name to be given to the entity (must be unique).</param>
        /// <param name="meshName">The name of the mesh to load.  Will be loaded if not already.</param>
        /// <returns></returns>
        public virtual Entity CreateEntity(string name, PrefabEntity prefab) {
            switch(prefab) {
                case PrefabEntity.Plane:
                    return CreateEntity(name, "Prefab_Plane");
                default:
                    return null;
            }        
        }

        /// <summary>
        ///		Creates a light that will be managed by this scene manager.
        /// </summary>
        /// <remarks>
        ///		Lights can either be in a fixed position and independent of the
        ///		scene graph, or they can be attached to SceneNodes so they derive
        ///		their position from the parent node. Either way, they are created
        ///		using this method so that the SceneManager manages their
        ///		existence.
        /// </remarks>
        /// <param name="name">Name of the light to create.</param>
        /// <returns></returns>
        public virtual Light CreateLight(string name) {
            // create a new light and add it to our internal list
            Light light = new Light(name);
			
            // add the light to the list
            lightList.Add(name, light);

            return light;
        }

        /// <summary>
        ///		Creates a new (blank) material with the specified name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public virtual Material CreateMaterial(string name) {
            Material material = (Material) MaterialManager.Instance.Create(name);
            material.CreateTechnique().CreatePass();
            return material;
        }

        /// <summary>
        ///		Empties the entire scene, inluding all SceneNodes, Cameras, Entities and Lights etc.
        /// </summary>
        public virtual void ClearScene() {
            // TODO: Implement ClearScene
            rootSceneNode.Clear();
            cameraList.Clear();
            entityList.Clear();
            lightList.Clear();
        }

        /// <summary>
        ///		Loads the source of the 'world' geometry, i.e. the large, mainly static geometry
        ///		making up the world e.g. rooms, landscape etc.
        /// </summary>
        /// <remarks>
        ///		Depending on the type of SceneManager (subclasses will be specialized
        ///		for particular world geometry types) you have requested via the Root or
        ///		SceneManagerEnumerator classes, you can pass a filename to this method and it
        ///		will attempt to load the world-level geometry for use. If you try to load
        ///		an inappropriate type of world data an exception will be thrown. The default
        ///		SceneManager cannot handle any sort of world geometry and so will always
        ///		throw an exception. However subclasses like BspSceneManager can load
        ///		particular types of world geometry e.g. "q3dm1.bsp".
        /// </remarks>
        /// <param name="fileName"></param>
        public virtual void LoadWorldGeometry(string fileName) {
            // TODO: Implement SceneManager.LoadWorldGeometry
        }

        #endregion

        #region Protected methods

        /// <summary>Internal method for setting a material for subsequent rendering.</summary>
        /// <remarks>
        ///		If this method returns a non-zero value, it means that not all
        ///		the remaining texture layers can be rendered in one pass, and a
        ///		subset of them have been set up in the RenderSystem for the first
        ///		pass - the caller should render the geometry then call this
        ///		method again to set the remaining texture layers and re-render
        ///		the geometry again.
        /// </remarks>
        /// <param name="material">The material to set.</param>
        /// <param name="numLayers">
        ///		The top 'n' number of layers to be processed,
        ///    will only be less than total layers if a previous call
        ///    resulted in a multipass render being required.
        ///	 </param>
        /// <returns>
        ///		The number of layers unprocessed because of insufficient
        ///		available texture units in the hardware.
        ///	</returns>
        protected void SetPass(Pass pass) {
            // vertex pipline
            if(pass.HasVertexProgram) {
                targetRenderSystem.BindGpuProgram(pass.VertexProgram.BindingDelegate);
                targetRenderSystem.BindGpuProgramParameters(GpuProgramType.Vertex, pass.VertexProgramParameters);
                lastUsedVertexProgram = true;
            }
            else {
                if(lastUsedVertexProgram) {
                    targetRenderSystem.UnbindGpuProgram(GpuProgramType.Vertex);
                    lastUsedVertexProgram = false;
                }

                // set the material surface params, only if lighting is enabled
                if(pass.LightingEnabled) {
                    // set the surface params of the render system
                    targetRenderSystem.SetSurfaceParams(pass.Ambient, pass.Diffuse, pass.Specular, pass.Emissive, pass.Shininess);
                }

                // dynamic lighting
                targetRenderSystem.LightingEnabled = pass.LightingEnabled;
            }

            // fragment pipeline
            if(pass.HasFragmentProgram) {
                targetRenderSystem.BindGpuProgram(pass.FragmentProgram.BindingDelegate);
                targetRenderSystem.BindGpuProgramParameters(GpuProgramType.Fragment, pass.FragmentProgramParameters);
                lastUsedFragmentProgram = true;
            }
            else {
                if(lastUsedFragmentProgram) {
                    targetRenderSystem.UnbindGpuProgram(GpuProgramType.Fragment);
                    lastUsedFragmentProgram = false;
                }

                // Fog
                ColorEx newFogColor;
                FogMode newFogMode;
                float newFogDensity, newFogStart, newFogEnd;

                // does the pass want to override the fog mode?
                if(pass.FogOverride) {
                    newFogMode = pass.FogMode;
                    newFogColor = pass.FogColor;
                    newFogDensity = pass.FogDensity;
                    newFogStart = pass.FogStart;
                    newFogEnd = pass.FogEnd;
                }
                else {
                    newFogMode = fogMode;
                    newFogColor = fogColor;
                    newFogDensity = fogDensity;
                    newFogStart = fogStart;
                    newFogEnd = fogEnd;
                }

                // set fog using the render system
                targetRenderSystem.SetFog(newFogMode, newFogColor, newFogDensity, newFogStart, newFogEnd);
            }

            // Scene Blending
            targetRenderSystem.SetSceneBlending(pass.SourceBlendFactor, pass.DestBlendFactor);

            // set all required texture units for this pass, and disable ones not being used
            for(int i = 0; i < targetRenderSystem.Caps.NumTextureUnits; i++) {
                if(i < pass.NumTextureUnitStages) {
                    TextureUnitState texUnit = pass.GetTextureUnitState(i);

                    // issue settings for texture units that don't rely on an updated view matrix
                    // those will be handled in RenderSingleObject specifically
                    if(!texUnit.HasViewRelativeTexCoordGen) {
                        targetRenderSystem.SetTextureUnit(i, texUnit);
                    }
                }
                else {
                    // disable this unit
                    targetRenderSystem.DisableTextureUnit(i);
                }
            }

            // Depth Settings
            targetRenderSystem.DepthWrite = pass.DepthWrite;
            targetRenderSystem.DepthCheck = pass.DepthCheck;
            targetRenderSystem.DepthFunction = pass.DepthFunction;
            targetRenderSystem.DepthBias = pass.DepthBias;

            // Culling Mode
            targetRenderSystem.CullingMode = pass.CullMode;

            // Shading mode
            targetRenderSystem.ShadingMode = pass.ShadingMode;
        }

        /// <summary>
        ///		Utility method for creating the planes of a skybox.
        /// </summary>
        /// <param name="plane"></param>
        /// <param name="distance"></param>
        /// <param name="orientation"></param>
        /// <returns></returns>
        protected Mesh CreateSkyboxPlane(BoxPlane plane, float distance, Quaternion orientation) {
            Plane p = new Plane();
            string meshName = "SkyboxPlane_";
            Vector3 up = Vector3.Zero;

            // set the distance of the plane
            p.D = distance;

            switch(plane) {
                case BoxPlane.Front:
                    p.Normal = Vector3.UnitZ;
                    up = Vector3.UnitY;
                    meshName += "Front";
                    break;
                case BoxPlane.Back:
                    p.Normal = -Vector3.UnitZ;
                    up = Vector3.UnitY;
                    meshName += "Back";
                    break;
                case BoxPlane.Left:
                    p.Normal = Vector3.UnitX;
                    up = Vector3.UnitY;
                    meshName += "Left";
                    break;
                case BoxPlane.Right:
                    p.Normal = -Vector3.UnitX;
                    up = Vector3.UnitY;
                    meshName += "Right";
                    break;
                case BoxPlane.Up:
                    p.Normal = -Vector3.UnitY;
                    up = Vector3.UnitZ;
                    meshName += "Up";
                    break;
                case BoxPlane.Down:
                    p.Normal = Vector3.UnitY;
                    up = -Vector3.UnitZ;
                    meshName += "Down";
                    break;
            }

            // modify by orientation
            p.Normal = orientation * p.Normal;
            up = orientation * up;

            MeshManager modelMgr = MeshManager.Instance;

            // see if this mesh exists
            Mesh planeModel = modelMgr.GetByName(meshName);

            // trash it if it already exists
            if(planeModel != null)
                modelMgr.Unload(planeModel);

            float planeSize = distance * 2;

            // create and return the plane mesh
            return modelMgr.CreatePlane(meshName, p, planeSize, planeSize, 1, 1, false, 1, 1, 1, up);
        }

        /// <summary>
        ///    Creates 
        /// </summary>
        /// <param name="ray"></param>
        /// <returns></returns>
        public RaySceneQuery CreateRayQuery(Ray ray) {
            DefaultRaySceneQuery query = new DefaultRaySceneQuery(this, ray);
            // default to return all results
            query.QueryMask = 0xffffffff;
            return query;
        }

        /// <summary>
        ///    Creates 
        /// </summary>
        /// <param name="ray"></param>
        /// <returns></returns>
        public RaySceneQuery CreateRayQuery(Ray ray, ulong mask) {
            DefaultRaySceneQuery query = new DefaultRaySceneQuery(this, ray);
            query.QueryMask = mask;
            return query;
        }

        /// <summary>
        ///    
        /// </summary>
        /// <param name="plane"></param>
        /// <param name="curvature"></param>
        /// <param name="tiling"></param>
        /// <param name="distance"></param>
        /// <param name="orientation"></param>
        /// <returns></returns>
        protected Mesh CreateSkyDomePlane(BoxPlane plane, float curvature, float tiling, float distance, Quaternion orientation) {
            Plane p = new Plane();
            Vector3 up = Vector3.Zero;
            string meshName = "SkyDomePlane_";

            // set up plane equation
            p.D = distance;

            switch(plane) {
                case BoxPlane.Front:
                    p.Normal = Vector3.UnitZ;
                    up = Vector3.UnitY;
                    meshName += "Front";
                    break;
                case BoxPlane.Back:
                    p.Normal = -Vector3.UnitZ;
                    up = Vector3.UnitY;
                    meshName += "Back";
                    break;
                case BoxPlane.Left:
                    p.Normal = Vector3.UnitX;
                    up = Vector3.UnitY;
                    meshName += "Left";
                    break;
                case BoxPlane.Right:
                    p.Normal = -Vector3.UnitX;
                    up = Vector3.UnitY;
                    meshName += "Right";
                    break;
                case BoxPlane.Up:
                    p.Normal = -Vector3.UnitY;
                    up = Vector3.UnitZ;
                    meshName += "Up";
                    break;
                case BoxPlane.Down:
                    return null;
            }

            // modify orientation
            p.Normal = orientation * p.Normal;
            up = orientation * up;

            // check to see if mesh exists
            MeshManager meshManager = MeshManager.Instance;
            Mesh planeMesh = meshManager.GetByName(meshName);

            // destroy existing
            if(planeMesh != null) {
                meshManager.Unload(planeMesh);
                planeMesh.Dispose();
            }

            // create new
            float planeSize = distance * 2;
            int segments = 16;
            return planeMesh = meshManager.CreateCurvedIllusionPlane(meshName, p, planeSize, planeSize, curvature, segments, segments, false, 1, tiling, tiling, up, orientation, BufferUsage.DynamicWriteOnly, BufferUsage.StaticWriteOnly, false, false);
        }

        /// <summary>
        ///		Protected method used by RenderVisibleObjects to deal with renderables
        ///		which override the camera's own view / projection materices.
        /// </summary>
        /// <param name="renderable"></param>
        protected void UseRenderableViewProjection(IRenderable renderable) {
            bool useIdentityView = renderable.UseIdentityView;
            bool useIdentityProj = renderable.UseIdentityProjection;

            // View
            if(useIdentityView && (hasCameraChanged || !lastViewWasIdentity)) {
                // using identity view now, so change it
                targetRenderSystem.ViewMatrix = Matrix4.Identity;
                lastViewWasIdentity = true;
            }
            else if (!useIdentityView && (hasCameraChanged || lastViewWasIdentity)){
                targetRenderSystem.ViewMatrix = camInProgress.ViewMatrix;
                lastViewWasIdentity = false;
            }

            // Projection
            if(useIdentityProj && (hasCameraChanged || !lastProjectionWasIdentity)) {
                // using identity view now, so change it
                targetRenderSystem.ProjectionMatrix = Matrix4.Identity;
                lastProjectionWasIdentity = true;
            }
            else if(!useIdentityProj && (hasCameraChanged || lastProjectionWasIdentity)) {
                targetRenderSystem.ProjectionMatrix = camInProgress.ProjectionMatrix;
                lastProjectionWasIdentity = false;
            }

            // reset this flag so the view/proj wont be updated again this frame
            hasCameraChanged = false;
        }

        /// <summary>
        ///		Used to first the QueueStarted event.  
        /// </summary>
        /// <param name="group"></param>
        /// <returns>True if the queue should be skipped.</returns>
        protected bool OnRenderQueueStarted(RenderQueueGroupID group) {
            if(QueueStarted != null) {
                return QueueStarted(group);
            }

            return false;
        }

        /// <summary>
        ///		Used to first the QueueEnded event.  
        /// </summary>
        /// <param name="group"></param>
        /// <returns>True if the queue should be repeated.</returns>
        protected bool OnRenderQueueEnded(RenderQueueGroupID group) {
            if(QueueEnded != null) {
                return QueueEnded(group);
            }

            return false;
        }

        #endregion

        #region Public methods

        /// <summary>
        ///    Removes the specified entity from the scene.
        /// </summary>
        /// <param name="entity"></param>
        public void RemoveEntity(Entity entity) {
            entityList.Remove(entity);
        }

        /// <summary>
        ///    Removes the entity with the specified name from the scene.
        /// </summary>
        /// <param name="entity"></param>
        public void RemoveEntity(string name) {
            Entity entity = entityList[name];
            if(entity != null) {
                entityList.Remove(entity);
            }
        }

        /// <summary>
        ///		Sets the fogging mode applied to the scene.
        /// </summary>
        /// <remarks>
        ///		This method sets up the scene-wide fogging effect. These settings
        ///		apply to all geometry rendered, UNLESS the material with which it
        ///		is rendered has it's own fog settings (see Material.SetFog).
        /// </remarks>
        /// <param name="mode">Set up the mode of fog as described in the FogMode
        ///		enum, or set to FogMode.None to turn off.</param>
        /// <param name="color">The color of the fog. Either set this to the same
        ///		as your viewport background color, or to blend in with a skydome or skybox.</param>
        /// <param name="density">The density of the fog in Exp or Exp2.
        ///		mode, as a value between 0 and 1. The default is 0.001. </param>
        /// <param name="linearStart">Distance in world units at which linear fog starts to
        ///		encroach. Only applicable if mode is</param>
        /// <param name="linearEnd">Distance in world units at which linear fog becomes completely
        ///		opaque. Only applicable if mode is</param>
        public void SetFog(FogMode mode, ColorEx color, float density, float linearStart, float linearEnd) {
            // set all the fog information
            fogMode = mode;
            fogColor = color;
            fogDensity = density;
            fogStart = linearStart;
            fogEnd = linearEnd;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="color"></param>
        /// <param name="density"></param>
        public void SetFog(FogMode mode, ColorEx color, float density) {
            // set all the fog information
            fogMode = mode;
            fogColor = color;
            fogDensity = density;
            fogStart = 0.0f;
            fogEnd = 1.0f;
        }

        /// <summary>
        ///		Overloaded method.
        /// </summary>
        /// <param name="enable"></param>
        /// <param name="materialName"></param>
        /// <param name="distance"></param>
        public void SetSkyBox(bool enable, string materialName, float distance) {
            SetSkyBox(enable, materialName, distance, true, Quaternion.Identity);
        }

        /// <summary>
        ///		Enables / disables a 'sky box' i.e. a 6-sided box at constant
        ///		distance from the camera representing the sky.
        /// </summary>
        /// <remarks>
        ///		You could create a sky box yourself using the standard mesh and
        ///		entity methods, but this creates a plane which the camera can
        ///		never get closer or further away from - it moves with the camera.
        ///		(you could create this effect by creating a world box which
        ///		was attached to the same SceneNode as the Camera too, but this
        ///		would only apply to a single camera whereas this skybox applies
        ///		to any camera using this scene manager).
        ///		<p/>
        ///		The material you use for the skybox can either contain layers
        ///		which are single textures, or they can be cubic textures, i.e.
        ///		made up of 6 images, one for each plane of the cube. See the
        ///		TextureLayer class for more information.
        /// </remarks>
        /// <param name="enable">True to enable the skybox, false to disable it</param>
        /// <param name="materialName">The name of the material the box will use.</param>
        /// <param name="distance">Distance in world coordinates from the camera to each plane of the box. </param>
        /// <param name="drawFirst">
        ///		If true, the box is drawn before all other
        ///		geometry in the scene, without updating the depth buffer.
        ///		This is the safest rendering method since all other objects
        ///		will always appear in front of the sky. However this is not
        ///		the most efficient way if most of the sky is often occluded
        ///		by other objects. If this is the case, you can set this
        ///		parameter to false meaning it draws <em>after</em> all other
        ///		geometry which can be an optimisation - however you must
        ///		ensure that the distance value is large enough that no
        ///		objects will 'poke through' the sky box when it is rendered.
        /// </param>
        /// <param name="orientation">
        ///		Specifies the orientation of the box. By default the 'top' of the box is deemed to be
        ///		in the +y direction, and the 'front' at the -z direction.
        ///		You can use this parameter to rotate the sky if you want.
        /// </param>
        public void SetSkyBox(bool enable, string materialName, float distance, bool drawFirst, Quaternion orientation) {
            // enable the skybox?
            isSkyBoxEnabled = enable;

            if(enable) {
                Material m = MaterialManager.Instance.GetByName(materialName);

                if(m == null)
                    throw new AxiomException(string.Format("Could not find skybox material '{0}'", materialName));

                // dont update the depth buffer
                //m.DepthWrite = false;

                // ensure texture clamping to reduce fuzzy edges when using filtering
                m.GetTechnique(0).GetPass(0).GetTextureUnitState(0).TextureAddressing = TextureAddressing.Clamp;

                // load yourself numbnuts!
                m.Load();
	
                isSkyBoxDrawnFirst = drawFirst;

                if(skyBoxNode == null)
                    skyBoxNode = CreateSceneNode("SkyBoxNode");
                else
                    skyBoxNode.DetachAllObjects();

                // need to create 6 plane entities for each side of the skybox
                for(int i = 0; i < 6; i++) {
                    Mesh planeModel = CreateSkyboxPlane((BoxPlane)i, distance, orientation);
                    string entityName = "SkyBoxPlane" + i;

                    if(skyBoxEntities[i] != null) {
                        RemoveEntity(skyBoxEntities[i]);
                    }

                    // create an entity for this plane
                    skyBoxEntities[i] = CreateEntity(entityName, planeModel.Name);

                    Material boxMaterial = MaterialManager.Instance.GetByName(entityName);

                    // if already exists, remove it first
                    if(boxMaterial != null) {
                        MaterialManager.Instance.Unload(boxMaterial);
                    }

                    // clone the material
                    boxMaterial = (Material)m.Clone(entityName);

                    // set the current frame
                    boxMaterial.GetTechnique(0).GetPass(0).GetTextureUnitState(0).CurrentFrame = i;

                    skyBoxEntities[i].MaterialName = boxMaterial.Name;

                    skyBoxNode.AttachObject(skyBoxEntities[i]);
                } // for
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="isEnabled"></param>
        /// <param name="materialName"></param>
        /// <param name="curvature"></param>
        /// <param name="tiling"></param>
        public void SetSkyDome(bool isEnabled, string materialName, float curvature, float tiling) {
            SetSkyDome(isEnabled, materialName, curvature, tiling, 4000, true, Quaternion.Identity);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="isEnabled"></param>
        /// <param name="materialName"></param>
        /// <param name="curvature"></param>
        /// <param name="tiling"></param>
        /// <param name="distance"></param>
        /// <param name="drawFirst"></param>
        /// <param name="orientation"></param>
        public void SetSkyDome(bool isEnabled, string materialName, float curvature, float tiling, float distance, bool drawFirst, Quaternion orientation) {
            isSkyDomeEnabled = isEnabled;
            if(isEnabled) {
                Material material = MaterialManager.Instance.GetByName(materialName);

                if(material == null) {
                    throw new AxiomException(string.Format("Could not find skydome material '{0}'", materialName));
                }

                // make sure the material doesn't update the depth buffer
                material.DepthWrite = false;
                // ensure loading
                material.Load();

                isSkyDomeDrawnFirst = drawFirst;

                // create node
                if(skyDomeNode == null) {
                    skyDomeNode = CreateSceneNode("SkyDomeNode");
                }
                else {
                    skyDomeNode.DetachAllObjects();
                }

                // set up the dome (5 planes)
                for(int i = 0; i < 5; ++i) {
                    Mesh planeMesh = CreateSkyDomePlane((BoxPlane) i, curvature, tiling, distance, orientation);
                    string entityName = "SkyDomePlame" + i.ToString();

                    // create entity
                    if(skyDomeEntities[i] != null) {
                        // TODO: Remove the entity damn it
                    }

                    skyDomeEntities[i] = CreateEntity(entityName, planeMesh.Name);
                    skyDomeEntities[i].MaterialName = material.Name;

                    // attach to node
                    skyDomeNode.AttachObject(skyDomeEntities[i]);
                }
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets/Sets the target render system that this scene manager should be using.
        /// </summary>
        public RenderSystem TargetRenderSystem {
            get { return targetRenderSystem; }
            set { targetRenderSystem = value; }
        }

        /// <summary>
        ///		Gets the SceneNode at the root of the scene hierarchy.
        /// </summary>
        /// <remarks>
        ///		The entire scene is held as a hierarchy of nodes, which
        ///		allows things like relative transforms, general changes in
        ///		rendering state etc (See the SceneNode class for more info).
        ///		In this basic SceneManager class, you are free to
        ///		structure this hierarchy however you like, since 
        ///		it has no real significance apart from making transforms
        ///		relative to each node (more specialised subclasses will
        ///		provide utility methods for building specific node structures
        ///		e.g. loading a BSP tree).
        /// </remarks>
        public SceneNode RootSceneNode {
            get { return rootSceneNode; }
        }

        /// <summary>
        ///		Gets/Sets the ambient light level to be used for the scene.
        /// </summary>
        /// <remarks>
        ///		This sets the color and intensity of the ambient light in the scene, i.e. the
        ///		light which is 'sourceless' and illuminates all objects equally.
        ///		The color of an object is affected by a combination of the light in the scene,
        ///		and the amount of light that object reflects (in this case based on the Material.Ambient
        ///		property).
        ///		<p/>
        ///		By default the ambient light in the scene is Black, i.e. no ambient light. This
        ///		means that any objects rendered with a Material which has lighting enabled 
        ///		(see Material.LightingEnabled) will not be visible unless you have some dynamic lights in your scene.
        /// </remarks>
        public ColorEx AmbientLight {
            get { return ambientColor; }
            set { 
                ambientColor = value; 
                // change ambient color of current render system
                targetRenderSystem.AmbientLight = ambientColor;
            }
        }

        /// <summary>
        ///		Method for setting a specific option of the Scene Manager. These options are usually
        ///		specific for a certain implementation of the Scene Manager class, and may (and probably
        ///		will) not exist across different implementations.
        /// </summary>
        public Hashtable Options {
            get { return optionList; }
        }

        /// <summary>
        ///		Gets/Sets a value that forces all nodes to render their bounding boxes.
        /// </summary>
        public bool ShowBoundingBoxes {
            get { return showBoundingBoxes; }
            set { showBoundingBoxes = value; }
        }

        /// <summary>
        ///		Gets/Sets whether or not to display the nodes themselves in addition to their objects.
        /// </summary>
        /// <remarks>
        ///		What will be displayed is the local axes of the node (for debugging mainly).
        /// </remarks>
        public bool DisplayNodes {
            get { return displayNodes; }
            set { displayNodes = value; }
        }

        /// <summary>
        ///		Gets the fog mode that was set during the last call to SetFog.
        /// </summary>
        public FogMode FogMode {
            get { return fogMode; }
        }

        /// <summary>
        ///		Gets the fog starting point that was set during the last call to SetFog.
        /// </summary>
        public float FogStart {
            get { return fogStart; }
        }

        /// <summary>
        ///		Gets the fog ending point that was set during the last call to SetFog.
        /// </summary>
        public float FogEnd {
            get { return fogEnd; }
        }

        /// <summary>
        ///		Gets the fog density that was set during the last call to SetFog.
        /// </summary>
        public float FogDensity {
            get { return fogDensity; }
        }

        /// <summary>
        ///		Gets the fog color that was set during the last call to SetFog.
        /// </summary>
        public ColorEx FogColor {
            get { return fogColor; }
        }

        #endregion

        #region Internal methods

        /// <summary>
        ///		Prompts the class to send its contents to the renderer.
        /// </summary>
        /// <remarks>
        ///		This method prompts the scene manager to send the
        ///		contents of the scene it manages to the rendering
        ///		pipeline, possibly preceded by some sorting, culling
        ///		or other scene management tasks. Note that this method is not normally called
        ///		directly by the user application; it is called automatically
        ///		by the engine's rendering loop.
        /// </remarks>
        /// <param name="camera">Pointer to a camera from whose viewpoint the scene is to be rendered.</param>
        /// <param name="viewport">The target viewport</param>
        /// <param name="showOverlays">Whether or not any overlay objects should be rendered</param>
        internal void RenderScene(Camera camera, Viewport viewport, bool showOverlays) {
            camInProgress = camera;
            hasCameraChanged = true;

            // use this viewport for the rendering systems current pass
            targetRenderSystem.SetViewport(viewport);

            // set the current camera for use in the auto GPU program params
            autoParamDataSource.Camera = camera;

            // sets the current ambient light color for use in auto GPU program params
            autoParamDataSource.AmbientLight = ambientColor;

            // apply animations
            ApplySceneAnimations();

            // update scene graph
            UpdateSceneGraph(camera);

            // ask the camera to auto track if it has a target
            camera.AutoTrack();

            // clear the current render queue
            renderQueue.Clear();

            // find camera's visible objects
            FindVisibleObjects(camera);

            if(viewport.OverlaysEnabled) {
                // Queue overlays for rendering
                OverlayManager.Instance.QueueOverlaysForRendering(camera, renderQueue, viewport);
            }

            // queue overlays and skyboxes for rendering
            QueueSkiesForRendering(camera);

            // begin frame geometry count
            targetRenderSystem.BeginGeometryCount();
			
            // being a frame of animation
            targetRenderSystem.BeginFrame();

            // use the camera's current scene detail level
            targetRenderSystem.RasterizationMode = camera.SceneDetail;

            // update all controllers
            ControllerManager.Instance.UpdateAll();

            // render all visible objects
            RenderVisibleObjects();

            // end the current frame
            targetRenderSystem.EndFrame();

            // Notify camera of the number of rendered faces
            camera.NotifyRenderedFaces(targetRenderSystem.FacesRendered);
        }

        /// <summary>
        ///		Internal method for updating the scene graph ie the tree of SceneNode instances managed by this class.
        /// </summary>
        /// <remarks>
        ///		This must be done before issuing objects to the rendering pipeline, since derived transformations from
        ///		parent nodes are not updated until required. This SceneManager is a basic implementation which simply
        ///		updates all nodes from the root. This ensures the scene is up to date but requires all the nodes
        ///		to be updated even if they are not visible. Subclasses could trim this such that only potentially visible
        ///		nodes are updated.
        /// </remarks>
        /// <param name="camera"></param>
        internal virtual void UpdateSceneGraph(Camera camera) {
            // Cascade down the graph updating transforms & world bounds
            // In this implementation, just update from the root
            // Smarter SceneManager subclasses may choose to update only
            // certain scene graph branches based on space partioning info.
            rootSceneNode.Update(true, false);
        }

        /// <summary>
        ///		Internal method which parses the scene to find visible objects to render.
        /// </summary>
        /// <remarks>
        ///		If you're implementing a custom scene manager, this is the most important method to
        ///		override since it's here you can apply your custom world partitioning scheme. Once you
        ///		have added the appropriate objects to the render queue, you can let the default
        ///		SceneManager objects RenderVisibleObjects handle the actual rendering of the objects
        ///		you pick.
        ///		<p/>
        ///		Any visible objects will be added to a rendering queue, which is indexed by material in order
        ///		to ensure objects with the same material are rendered together to minimise render state changes.
        /// </remarks>
        /// <param name="camera"></param>
        internal virtual void FindVisibleObjects(Camera camera) {
            // ask the root node to iterate through and find visible objects in the scene
            rootSceneNode.FindVisibleObjects(camera, renderQueue, true, displayNodes);
        }

        /// <summary>
        ///		Internal method for applying animations to scene nodes.
        /// </summary>
        /// <remarks>
        ///		Uses the internally stored AnimationState objects to apply animation to SceneNodes.
        /// </remarks>
        internal virtual void ApplySceneAnimations() {
            for(int i = 0; i < animationStateList.Count; i++) {
                // get the current animation state
                AnimationState animState = animationStateList[i];

                // get this states animation
                Animation anim = animationList[animState.Name];

                // loop through all tracks and reset their nodes initial state
                for(int j = 0; j < anim.Tracks.Count; j++) {
                    Node node = anim.Tracks[j].TargetNode;
                    node.ResetToInitialState();
                }

                // apply the animation
                anim.Apply(animState.Time, animState.Weight, false);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="renderable"></param>
        /// <param name="pass"></param>
        protected virtual void RenderSingleObject(IRenderable renderable, Pass pass) {
            ushort numMatrices = 0;
                
            // grab the current scene detail level and init the last detail level used
            SceneDetailLevel camDetailLevel = camInProgress.SceneDetail;
            SceneDetailLevel lastDetailLevel = camDetailLevel;
        
            // update auto params if this is a programmable pass
            if(pass.IsProgrammable) {
                autoParamDataSource.Renderable = renderable;
                pass.UpdateAutoParams(autoParamDataSource);
            }

            // get the world matrices and the count
            renderable.GetWorldTransforms(xform);
            numMatrices = renderable.NumWorldTransforms;

            // set the world matrices in the render system
            if(numMatrices > 1)
                targetRenderSystem.SetWorldMatrices(xform, numMatrices);
            else
                targetRenderSystem.WorldMatrix = xform[0];        

            // issue view/projection changes (if any)
            UseRenderableViewProjection(renderable);

            // Do we need to update light states? 
            // Only do this if fixed-function vertex lighting applies
            if(pass.LightingEnabled && !pass.HasVertexProgram) {
                targetRenderSystem.UseLights(renderable.Lights, pass.MaxLights);
            }

            // set up the texture units for this pass
            for(int i = 0; i < pass.NumTextureUnitStages; i++) {
                TextureUnitState texUnit = pass.GetTextureUnitState(i);

                // issue texture units that depend on updated view matrix
                // reflective env mapping is one case
                if(texUnit.HasViewRelativeTexCoordGen) {
                    targetRenderSystem.SetTextureUnit(i, texUnit);
                }
            }

            // TODO: Normalize normals!

            // override solid/wireframe rendering
            SceneDetailLevel requestedDetail = renderable.RenderDetail;

            if(requestedDetail != lastDetailLevel) {
                // dont go from wireframe to solid, only downgrade
                if(requestedDetail > camDetailLevel)
                    requestedDetail = camDetailLevel;
										
                // update the render systems rasterization mode
                targetRenderSystem.RasterizationMode = requestedDetail;

                lastDetailLevel = requestedDetail;
            }

            // get the renderables render operation
            renderable.GetRenderOperation(op);

            // render the object as long as it has vertices
            if(op.vertexData.vertexCount > 0)
                targetRenderSystem.Render(op);
        }

        /// <summary>
        ///		Sends visible objects found in FindVisibleObjects to the rendering engine.
        /// </summary>
        internal virtual void RenderVisibleObjects() {
            int renderCount = 0;

            // loop through each main render group ( which is already sorted)
            for(int i = 0; i < renderQueue.NumRenderQueueGroups; i++) {
                RenderQueueGroupID queueID = renderQueue.GetRenderQueueGroupID(i);
                RenderQueueGroup queueGroup = renderQueue.GetRenderQueueGroup(i);

                bool repeatQueue = false;

                // repeat
                do { 
                    if(OnRenderQueueStarted(queueID)) {
                        // someone requested we skip this queue
                        continue;
                    }

                    // iterate through the priority queues
                    for(int j = 0; j < queueGroup.NumPriorityGroups; j++) {
                        renderCount++;
                        
                        RenderPriorityGroup priorityGroup = queueGroup.GetPriorityGroup(j);

                        // sort the current priorty groups
                        priorityGroup.Sort(camInProgress);

                        // ----- SOLIDS LOOP -----
                        for(int k = 0; k < priorityGroup.NumSolidPasses; k++) {
                            // skip this iteration if there are no solid passes
                            if(priorityGroup.NumSolidPasses == 0) {
                                continue;
                            }

                            Pass pass = priorityGroup.GetSolidPass(k);
                            ArrayList renderables = priorityGroup.GetSolidPassRenderables(k);

                            // set the pass for the list of renderables to be processed
                            SetPass(pass);

                            // render each object associated with this rendering pass
                            for(int r = 0; r < renderables.Count; r++) {
                                IRenderable renderable = (IRenderable)renderables[r];
                                RenderSingleObject(renderable, pass);
                            }
                        }

                        // ----- TRANSPARENT LOOP -----
                        // This time we render by Z, not by material
                        // The mTransparentObjects set needs to be ordered first
                        for(int k = 0; k < priorityGroup.NumTransparentPasses; k++) {
                            RenderablePass rp = priorityGroup.GetTransparentPass(k);

                            // set the pass first
                            SetPass(rp.pass);

                            // render the transparent object
                            RenderSingleObject(rp.renderable, rp.pass);
                        }
                    } // for each priority

                    // true if someone requested that we repeat this queue
                    repeatQueue = OnRenderQueueEnded(queueID);

                } while(repeatQueue);
            } // for each queue group
        }

        /// <summary>
        ///		Internal method for queueing the sky objects with the params as 
        ///		previously set through SetSkyBox, SetSkyPlane and SetSkyDome.
        /// </summary>
        /// <param name="camera"></param>
        internal virtual void QueueSkiesForRendering(Camera camera) {
            // translate the skybox by cam position
            if(skyPlaneNode != null)
                skyPlaneNode.Position = camera.DerivedPosition;

            if(skyBoxNode != null)
                skyBoxNode.Position = camera.DerivedPosition;

            if(skyDomeNode != null)
                skyDomeNode.Position = camera.DerivedPosition;

            RenderQueueGroupID qid;

            if(isSkyPlaneEnabled) {
                qid = isSkyPlaneDrawnFirst ? RenderQueueGroupID.One : RenderQueueGroupID.Nine;
                renderQueue.AddRenderable(skyPlaneEntity.GetSubEntity(0), 1, qid);
            }

            if(isSkyBoxEnabled) {
                qid = isSkyBoxDrawnFirst ? RenderQueueGroupID.One : RenderQueueGroupID.Nine;

                for(int plane = 0; plane < 6; plane++)
                    renderQueue.AddRenderable(skyBoxEntities[plane].GetSubEntity(0), 1, qid);
            }

            if(isSkyDomeEnabled) {
                qid = isSkyDomeDrawnFirst ? RenderQueueGroupID.One : RenderQueueGroupID.Nine;

                for(int plane = 0; plane < 5; ++plane)
                    renderQueue.AddRenderable(skyDomeEntities[plane].GetSubEntity(0), 1, qid);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="position"></param>
        /// <param name="destList"></param>
        internal void PopulateLightList(Vector3 position, LightList destList) {
            // clear the list first
            destList.Clear();

            // loop through the scene lights an add ones in range
            for(int i = 0; i < lightList.Count; i++) {
                Light light = lightList[i];

                if(light.IsVisible) {
                    if(light.Type == LightType.Directional) {
                        // no distance
                        light.tempSquaredDist = 0.0f;
                        destList.Add(light);
                    }
                    else {
                        light.tempSquaredDist = (light.DerivedPosition - position).LengthSquared;
                        float range = light.AttenuationRange;

                        // square range for even comparison and compare
                        if(light.tempSquaredDist <= (range * range)) {
                            destList.Add(light);
                        }
                    }
                } // if
            } // for

            // Sort Destination light list.
            // TODO: Not needed yet since the current LightList is a sorted list under the hood already
            //destList.Sort();
        }
            
        /// <summary>
        ///		Enables / disables a 'sky plane' i.e. a plane at constant
        ///		distance from the camera representing the sky.
        /// </summary>
        /// <param name="enable">True to enable the plane, false to disable it.</param>
        /// <param name="plane">Details of the plane, i.e. it's normal and it's distance from the camera.</param>
        /// <param name="materialName">The name of the material the plane will use.</param>
        /// <param name="scale">The scaling applied to the sky plane - higher values mean a bigger sky plane.</param>
        /// <param name="tiling">How many times to tile the texture across the sky.</param>
        /// <param name="drawFirst">
        ///		If true, the plane is drawn before all other geometry in the scene, without updating the depth buffer.
        ///		This is the safest rendering method since all other objects
        ///		will always appear in front of the sky. However this is not
        ///		the most efficient way if most of the sky is often occluded
        ///		by other objects. If this is the case, you can set this
        ///		parameter to false meaning it draws <em>after</em> all other
        ///		geometry which can be an optimisation - however you must
        ///		ensure that the plane.d value is large enough that no objects
        ///		will 'poke through' the sky plane when it is rendered.
        ///	 </param>
        /// <param name="bow">
        ///		If above zero, the plane will be curved, allowing
        ///		the sky to appear below camera level.  Curved sky planes are 
        ///		simular to skydomes, but are more compatable with fog.
        /// </param>
        public virtual void SetSkyPlane(bool enable, Plane plane, string materialName, float scale, float tiling, bool drawFirst, float bow) {
            isSkyPlaneEnabled = enable;

            if(enable) {
                string meshName = "SkyPlane";
                skyPlane = plane;

                Material m = MaterialManager.Instance.GetByName(materialName);

                if(m == null)
                    throw new Exception(string.Format("Skyplane material '{0}' not found.", materialName));

                // make sure the material doesn't update the depth buffer
                m.DepthWrite = false;
                m.Load();

                isSkyPlaneDrawnFirst = drawFirst;

                // set up the place
                Mesh planeMesh = MeshManager.Instance.GetByName(meshName);

                // unload the old one if it exists
                if(planeMesh != null)
                    MeshManager.Instance.Unload(planeMesh);

                // create up vector
                Vector3 up = plane.Normal.Cross(Vector3.UnitX);
                if(up == Vector3.Zero)
                    up = plane.Normal.Cross(-Vector3.UnitZ);

                if(bow > 0) {
                    // TODO: Add MeshManager.CreateCurvedPlane
                }
                else {
                    planeMesh = MeshManager.Instance.CreatePlane(meshName, plane, scale * 100, scale * 100, 1, 1, false, 1, tiling, tiling, up);
                }

                if(skyPlaneEntity != null) {
                    entityList.Remove(skyPlaneEntity);
                }

                // create entity for the plane, using the mesh name
                skyPlaneEntity = CreateEntity(meshName, meshName);
                skyPlaneEntity.MaterialName = materialName;

                if(skyPlaneNode == null)
                    skyPlaneNode = CreateSceneNode(meshName + "Node");
                else
                    skyPlaneNode.DetachAllObjects();

                // attach the skyplane to the new node
                skyPlaneNode.AttachObject(skyPlaneEntity);
            }
        }

        /// <summary>
        ///		Overload.
        /// </summary>
        /// <param name="enable"></param>
        /// <param name="plane"></param>
        public virtual void SetSkyPlane(bool enable, Plane plane, string materialName) {
            // call the overloaded method
            SetSkyPlane(enable, plane, materialName, 1000.0f, 10.0f, true, 0);
        }

        #endregion
    }

    /// <summary>
    ///    Default implementation of RaySceneQuery.
    /// </summary>
    public class DefaultRaySceneQuery : RaySceneQuery {
        public DefaultRaySceneQuery(SceneManager creator, Ray ray) : base(creator, ray) {}

        /// <summary>
        /// 
        /// </summary>
        public override void Execute() {
            for(int i = 0; i < creator.entityList.Count; i++) {
                Entity entity = creator.entityList[i];

                // test the intersection against the world bounding box of the entity
                Pair results = MathUtil.Intersects(ray, entity.GetWorldBoundingBox());

                // if the results came back positive, fire the event handler
                if((bool)results.object1 == true) {
                    OnQueryResult(creator, new RayQueryResultEventArgs(entity, (float)results.object2));
                }
            }
        }

    }
}
