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
    ///     Manages the rendering of a 'scene' i.e. a collection of primitives.
    /// </summary>
    /// <remarks>
    ///		This class defines the basic behavior of the 'Scene Manager' family. These classes will
    ///		organize the objects in the scene and send them to the rendering system, a subclass of
    ///		RenderSystem. This basic superclass only does basic bounding box frustum culling.
    ///    <p/>
    ///		Subclasses may use various techniques to organise the scene depending on how they are
    ///		designed (e.g. BSPs, octrees etc). As with other classes, methods marked as internal are 
    ///		designed to be called by other classes in the engine, not by user applications.
    ///	 </remarks>
    /// TODO: Thoroughly review node removal/cleanup.
    public class SceneManager {
        #region Fields

        /// <summary>A queue of objects for rendering.</summary>
        protected RenderQueue renderQueue;
        /// <summary>A reference to the current active render system..</summary>
        protected RenderSystem targetRenderSystem;
        /// <summary>Denotes whether or not the camera has been changed.</summary>
        protected bool hasCameraChanged;
        /// <summary>The ambient color, cached from the RenderSystem</summary>
        protected ColorEx ambientColor;
        /// <summary>A list of the valid cameras for this scene for easy lookup.</summary>
        protected CameraList cameraList;
        /// <summary>A list of lights in the scene for easy lookup.</summary>
        protected LightList lightList;
        /// <summary>A list of entities in the scene for easy lookup.</summary>
        protected internal EntityList entityList;
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
        /// <summary>
        ///    Local light list for use during rendering passes.
        /// </summary>
        protected static LightList localLightList = new LightList();
        /// <summary>
        ///    Whether normals are currently being normalized.
        /// </summary>
        protected static bool normalizeNormals;

        protected static bool lastUsedVertexProgram;
        protected static bool lastUsedFragmentProgram;

        // cached fog settings
        protected static FogMode oldFogMode;
        protected static ColorEx oldFogColor;
        protected static float oldFogStart, oldFogEnd, oldFogDensity;
        protected bool lastViewWasIdentity, lastProjectionWasIdentity;
		/// <summary>
		///		Active list of nodes tracking other nodes.
		/// </summary>
		protected SceneNodeCollection autoTrackingSceneNodes = new SceneNodeCollection();

		/// <summary>
		///		Current shadow technique in use in the scene.
		/// </summary>
		protected ShadowTechnique shadowTechnique;
		/// <summary>
		///		If true, shadow volumes will be visible in the scene.
		/// </summary>
		protected bool showDebugShadows;
		/// <summary>
		///		Pass to use for rendering debug shadows.
		/// </summary>
		protected Pass shadowDebugPass;
		/// <summary>
		///		
		/// </summary>
		protected Pass shadowStencilPass;
		/// <summary>
		///		Pass to use while rendering the full screen quad for modulative shadows.
		/// </summary>
		protected Pass shadowModulativePass;
		/// <summary>
		///		List of lights in view that could cast shadows.
		/// </summary>
		protected LightList lightsAffectingFrustum = new LightList();
		/// <summary>
		///		Full screen rectangle to use for rendering stencil shadows.
		/// </summary>
		protected Rectangle2D fullScreenQuad;
		/// <summary>
		///		buffer to use for indexing shadow geometry required for certain techniques.
		/// </summary>
		protected HardwareIndexBuffer shadowIndexBuffer;
		/// <summary>
		///		Current list of shadow casters within the view of the camera.
		/// </summary>
		protected ArrayList shadowCasterList = new ArrayList();

        #endregion Fields

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
            cameraList = new CameraList();
            lightList = new LightList();
            entityList = new EntityList();
            sceneNodeList = new SceneNodeCollection();
            billboardSetList = new BillboardSetCollection();
            animationList = new AnimationCollection();
            animationStateList = new AnimationStateCollection();

            // create the root scene node
            rootSceneNode = new SceneNode(this, "Root");

            // default to no fog
            fogMode = FogMode.None;

			// no shadows by default
			shadowTechnique = ShadowTechnique.None;

			renderQueue.GetQueueGroup(RenderQueueGroupID.Background).ShadowsEnabled = false;
			renderQueue.GetQueueGroup(RenderQueueGroupID.Overlay).ShadowsEnabled = false;
			renderQueue.GetQueueGroup(RenderQueueGroupID.SkiesEarly).ShadowsEnabled = false;
			renderQueue.GetQueueGroup(RenderQueueGroupID.SkiesLate).ShadowsEnabled = false;
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
            // TODO: Finish ClearScene
            rootSceneNode.Clear();
            cameraList.Clear();
            entityList.Clear();
            lightList.Clear();
            animationStateList.Clear();
			billboardSetList.Clear();
			sceneNodeList.Clear();
			autoTrackingSceneNodes.Clear();
        }

        /// <summary>
        ///    Destroys and removes a node from the scene.
        /// </summary>
        /// <param name="name"></param>
        public virtual void DestroySceneNode(string name) {
            Debug.Assert(sceneNodeList.ContainsKey(name), "Scene node not found.");

			// grab the node from the list
			SceneNode node = (SceneNode)sceneNodeList[name];

			// Find any scene nodes which are tracking this node, and turn them off.
			for(int i = 0; i < autoTrackingSceneNodes.Count; i++) {
				SceneNode autoNode = autoTrackingSceneNodes[i];

				if(autoNode.AutoTrackTarget == autoNode) {
					// turn off, this will notify SceneManager to remove
					autoNode.SetAutoTracking(false);
				}
				else if(autoNode == node) {
					// node being removed is a tracker
					autoTrackingSceneNodes.Remove(autoNode);
				}
			}

            // removes the node from the list
            sceneNodeList.Remove(node);
        }

        /// <summary>
        ///     Retreives the camera with the specified name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Camera GetCamera(string name) {
            if(cameraList[name] == null) {
                throw new AxiomException("SceneNode named '{0}' not found.", name);
            }

            return cameraList[name];
        }

        /// <summary>
        ///     Returns the material with the specified name.
        /// </summary>
        /// <param name="name">Name of the material to retrieve.</param>
        /// <returns></returns>
        public Material GetMaterial(string name) {
            return MaterialManager.Instance.GetByName(name);
        }

        /// <summary>
        ///     Retreives the scene node with the specified name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public SceneNode GetSceneNode(string name) {
            if(sceneNodeList[name] == null) {
                throw new AxiomException("SceneNode named '{0}' not found.", name);
            }

            return sceneNodeList[name];
        }

        /// <summary>
        ///     Asks the SceneManager to provide a suggested viewpoint from which the scene should be viewed.
        /// </summary>
        /// <remarks>
        ///     Typically this method returns the origin unless a) world geometry has been loaded using
        ///     <see cref="SceneManager.LoadWorldGeometry"/> and b) that world geometry has suggested 'start' points.
        ///     If there is more than one viewpoint which the scene manager can suggest, it will always suggest
        ///     the first one unless the random parameter is true.
        /// </remarks>
        /// <param name="random">
        ///     If true, and there is more than one possible suggestion, a random one will be used. If false
        ///     the same one will always be suggested.
        /// </param>
        /// <returns>Optimal ViewPoint defined by the scene manager.</returns>
        public virtual ViewPoint GetSuggestedViewpoint(bool random) {
            ViewPoint vp;

            // by default, return the origin.  leave up to subclasses to define
            vp.position = Vector3.Zero;
            vp.orientation = Quaternion.Identity;

            return vp;
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

		/// <summary>
		///		Internal method for locating a list of lights which could be affecting the frustum.
		/// </summary>
		/// <remarks>
		///		Custom scene managers are encouraged to override this method to make use of their
		///		scene partitioning scheme to more efficiently locate lights, and to eliminate lights
		///		which may be occluded by word geometry.
		/// </remarks>
		/// <param name="camera">Camera to find lights within it's view.</param>
		protected virtual void FindLightsAffectingFrustum(Camera camera) {
			// Basic iteration for this scene manager
			lightsAffectingFrustum.Clear();

			// sphere to use for testing
			Sphere sphere = new Sphere();

			for (int i = 0; i < lightList.Count; i++) {
				Light light = lightList[i];

				if(light.Type == LightType.Directional) {
					// Always visible
					lightsAffectingFrustum.Add(light);
				}
				else {
					// treating spotlight as point for simplicity
					// Just see if the lights attenuation range is within the frustum
					sphere.Center = light.DerivedPosition;
					sphere.Radius = light.AttenuationRange;

					if (camera.IsObjectVisible(sphere)) {
						lightsAffectingFrustum.Add(light);
					}
				}
			}
		}

		/// <summary>
		///		Internal method for locating a list of shadow casters which 
		///		could be affecting the frustum for a given light. 
		/// </summary>
		/// <remarks>
		///		Custom scene managers are encouraged to override this method to add optimizations, 
		///		and to add their own custom shadow casters (perhaps for world geometry)
		/// </remarks>
		/// <param name="light"></param>
		/// <param name="camera"></param>
		protected virtual IList FindShadowCastersForLight(Light light, Camera camera) {
			shadowCasterList.Clear();

			if (light.Type == LightType.Directional) {
				// Hmm, how to efficiently locate shadow casters for an infinite light?
				// TODO
			}
			else {
				Sphere s = new Sphere(light.DerivedPosition, light.AttenuationRange);

				// eliminate early if camera cannot see light sphere
				if (camera.IsObjectVisible(s)) {
					// HACK: Bypassing for testing, adding em all for now
					for(int i = 0; i < entityList.Count; i++) {
						if(entityList[i].CastShadows)
							shadowCasterList.Add(entityList[i]);
					}

//					if (!mShadowCasterSphereQuery) {
//						shadowCasterSphereQuery = CreateSphereRegionQuery(s);
//					}
//					else {
//						mShadowCasterSphereQuery->setSphere(s);
//					}
//
//					// Determine if light is inside or outside the frustum
//					bool lightInFrustum = camera->isVisible(light->getPosition());
//					const PlaneBoundedVolumeList* volList = 0;
//					if (!lightInFrustum)
//					{
//						// Only worth building an external volume list if
//						// light is outside the frustum
//						volList = &(light->_getFrustumClipVolumes(camera));
//					}
//
//					// Execute, use callback
//					mShadowCasterQueryListener.prepare(lightInFrustum, 
//						volList, camera, &mShadowCasterList);
//					mShadowCasterSphereQuery->execute(&mShadowCasterQueryListener);
				}
			}

			return shadowCasterList;
		}

		/// <summary>
		///		Internal method for setting up materials for shadows.
		/// </summary>
		protected virtual void InitShadowVolumeMaterials() {
			Material matDebug = MaterialManager.Instance.GetByName("Ogre/Debug/ShadowVolumes");

			if(matDebug == null) {
				// Create
				matDebug = (Material)MaterialManager.Instance.Create("Ogre/Debug/ShadowVolumes");
				shadowDebugPass = matDebug.CreateTechnique().CreatePass();
				shadowDebugPass.SetSceneBlending(SceneBlendType.Add); 
				shadowDebugPass.LightingEnabled = false;
				shadowDebugPass.DepthWrite = false;
				TextureUnitState t = shadowDebugPass.CreateTextureUnitState();
				t.SetColorOperationEx(
					LayerBlendOperationEx.Modulate, 
					LayerBlendSource.Manual, 
					LayerBlendSource.Current, 
					new ColorEx(0.7f, 0.0f, 0.2f));

				shadowDebugPass.CullMode = CullingMode.None;

				if(targetRenderSystem.Caps.CheckCap(Capabilities.VertexPrograms)) {
					// TODO, add hardware extrusion program
				}

				matDebug.Compile();
			}

			Material matStencil = MaterialManager.Instance.GetByName("Ogre/StencilShadowVolumes");

			if(matStencil == null) {
				// Create
				matStencil = (Material)MaterialManager.Instance.Create("Ogre/StencilShadowVolumes");
				shadowStencilPass = matStencil.CreateTechnique().CreatePass();

				if(targetRenderSystem.Caps.CheckCap(Capabilities.VertexPrograms)) {
					// TODO, add hardware extrusion program
				}

				// Nothing else, we don't use this like a 'real' pass anyway,
				// it's more of a placeholder
			}

			Material matModStencil = 
				MaterialManager.Instance.GetByName("Ogre/StencilShadowModulationPass");

			if(matModStencil == null) {
				// Create
				matModStencil = 
					(Material)MaterialManager.Instance.Create("Ogre/StencilShadowModulationPass");

				shadowModulativePass = matModStencil.CreateTechnique().CreatePass();
				shadowModulativePass.SetSceneBlending(SceneBlendFactor.DestColor, SceneBlendFactor.Zero); 
				shadowModulativePass.LightingEnabled = false;
				shadowModulativePass.DepthWrite = false;
				shadowModulativePass.DepthCheck = false;
				TextureUnitState t = shadowModulativePass.CreateTextureUnitState();
				t.SetColorOperationEx(
					LayerBlendOperationEx.Modulate, 
					LayerBlendSource.Manual, 
					LayerBlendSource.Current, 
					new ColorEx(0.25f, 0.25f, 0.25f));
			}

			// Also init full screen quad while we're at it
			if(fullScreenQuad == null) {
				fullScreenQuad = new Rectangle2D();
				fullScreenQuad.SetCorners(-1, 1, 1, -1);
			}
		}

		/// <summary>
		///		Internal method for rendering all the objects for a given light into the stencil buffer.
		/// </summary>
		/// <param name="light">The light source.</param>
		/// <param name="camera">The camera being viewed from.</param>
		protected virtual void RenderShadowVolumesToStencil(Light light, Camera camera) {
			// must disable gpu programs for the time being
			targetRenderSystem.UnbindGpuProgram(GpuProgramType.Vertex);
			targetRenderSystem.UnbindGpuProgram(GpuProgramType.Fragment);

			// Can we do a 2-sided stencil?
			bool stencil2sided = false;

			if (targetRenderSystem.Caps.CheckCap(Capabilities.TwoSidedStencil) && 
				targetRenderSystem.Caps.CheckCap(Capabilities.StencilWrap)) {
				// enable
				stencil2sided = true;
			}

			// Do we have access to vertex programs?
			bool extrudeInSoftware = true;
			if (targetRenderSystem.Caps.CheckCap(Capabilities.VertexPrograms)) {
				// TODO
				//extrudeInSoftware = false;
			}

			// Turn off color writing and depth writing
			targetRenderSystem.SetColorBufferWriteEnabled(false, false, false, false);
			targetRenderSystem.DepthWrite = false;
			targetRenderSystem.StencilCheckEnabled = true;
			targetRenderSystem.DepthFunction = CompareFunction.Less;

			// get the near clip volume
			PlaneBoundedVolume nearClipVol = light.GetNearClipVolume(camera);

//			Console.WriteLine("---");
//			foreach(Plane plane in nearClipVol.planes) {
//				Console.WriteLine("Normal {0} D: {1}", plane.Normal, plane.D);
//			}
//
//			Console.WriteLine("---");

			// get the shadow caster list
			IList casters = FindShadowCastersForLight(light, camera);

			// Determine whether zfail is required
			// We need to use zfail for ALL objects if we find a single object which
			// requires it
			bool zfailAlgo = false;
			int flags = 0;

			for(int i = 0; i < casters.Count; i++) {
				ShadowCaster caster = (ShadowCaster)casters[i];

				if(nearClipVol.Intersects(caster.GetWorldBoundingBox())) {
					// We have a zfail case, we must use zfail for all objects
					zfailAlgo = true;
					break;
				}
			}

			for(int i = 0; i < casters.Count; i++) {
				ShadowCaster caster = (ShadowCaster)casters[i];

				if(zfailAlgo) {
					// We need to include the light and / or dark cap
					// But only if they will be visible
					if(camera.IsObjectVisible(caster.GetLightCapBounds())) {
						flags |= (int)ShadowRenderableFlags.IncludeLightCap;
					}
					// Dark caps are not needed for directional lights if
					// extrusion is done in hardware (since extruded to infinity)
					if((light.Type != LightType.Directional || extrudeInSoftware)
						&& camera.IsObjectVisible(caster.GetDarkCapBounds(light))) {

						flags |= (int)ShadowRenderableFlags.IncludeDarkCap;
					}
				} // if zfail

				// get shadow renderables
				IEnumerator renderables = caster.GetShadowVolumeRenderableEnumerator(
					shadowTechnique, light, shadowIndexBuffer, extrudeInSoftware, flags);

				while(renderables.MoveNext()) {
					ShadowRenderable sr = (ShadowRenderable)renderables.Current;

					// render volume, including dark and (maybe) light caps
					RenderSingleShadowVolumeToStencil(sr, zfailAlgo, stencil2sided);

					// optionally render separate light cap
					if (sr.IsLightCapSeperate && ((flags & (int)ShadowRenderableFlags.IncludeLightCap)) > 0) {
						// must always fail depth check
						targetRenderSystem.DepthFunction = CompareFunction.AlwaysFail;

						Debug.Assert(sr.LightCapRenderable != null, "Shadow renderable is missing a separate light cap renderable!");

						RenderSingleShadowVolumeToStencil(sr.LightCapRenderable, zfailAlgo, stencil2sided);
						// reset depth function
						targetRenderSystem.DepthFunction = CompareFunction.Less;
					}
				}
			}
			// revert colour write state
			targetRenderSystem.SetColorBufferWriteEnabled(true, true, true, true);
			// revert depth state
			//targetRenderSystem.SetDepthBufferParams();
			targetRenderSystem.DepthCheck = true;
			targetRenderSystem.DepthWrite = true;
			targetRenderSystem.DepthFunction = CompareFunction.LessEqual;

			targetRenderSystem.StencilCheckEnabled = false;
		}

		/// <summary>
		///		Internal utility method for setting stencil state for rendering shadow volumes.
		/// </summary>
		/// <param name="secondPass">Is this the second pass?</param>
		/// <param name="zfail">Should we be using the zfail method?</param>
		/// <param name="twoSided">Should we use a 2-sided stencil?</param>
		protected virtual void SetShadowVolumeStencilState(bool secondPass, bool zfail, bool twoSided) {
			// First pass, do front faces if zpass
			// Second pass, do back faces if zpass
			// Invert if zfail
			// this is to ensure we always increment before decrement
			if ((secondPass ^ zfail)) {
				targetRenderSystem.CullingMode = twoSided ? CullingMode.None : CullingMode.CounterClockwise;
				targetRenderSystem.SetStencilBufferParams(
					CompareFunction.AlwaysPass, // always pass stencil check
					0, // no ref value (no compare)
					unchecked((int)0xffffffff), // no mask
					StencilOperation.Keep, // stencil test will never fail
					zfail ? (twoSided ? StencilOperation.IncrementWrap : StencilOperation.Increment) : StencilOperation.Keep, // back face depth fail
					zfail ? StencilOperation.Keep : (twoSided ? StencilOperation.DecrementWrap : StencilOperation.Decrement), // back face pass
					twoSided);
			}
			else {
				targetRenderSystem.CullingMode = twoSided ? CullingMode.None : CullingMode.Clockwise;
				targetRenderSystem.SetStencilBufferParams(
					CompareFunction.AlwaysPass, // always pass stencil check
					0, // no ref value (no compare)
					unchecked((int)0xffffffff), // no mask
					StencilOperation.Keep, // stencil test will never fail
					zfail ? (twoSided ? StencilOperation.DecrementWrap : StencilOperation.Decrement) : StencilOperation.Keep, // front face depth fail
					zfail ? StencilOperation.Keep : (twoSided ? StencilOperation.IncrementWrap : StencilOperation.Increment), // front face pass
					twoSided);
			}
		}

		/// <summary>
		///		Render a single shadow volume to the stencil buffer.
		/// </summary>
		/// <param name="sr"></param>
		/// <param name="zfail"></param>
		/// <param name="stencil2sided"></param>
		protected void RenderSingleShadowVolumeToStencil(ShadowRenderable sr, bool zfail, bool stencil2sided) {
			// Render a shadow volume here
			//  - if we have 2-sided stencil, one render with no culling
			//  - otherwise, 2 renders, one with each culling method and invert the ops
			SetShadowVolumeStencilState(false, zfail, stencil2sided);
			RenderSingleObject(sr, shadowStencilPass);//, false);

			if (!stencil2sided) {
				// Second pass
				SetShadowVolumeStencilState(true, zfail, false);
				RenderSingleObject(sr, shadowStencilPass);//, false);
			}

			// Do we need to render a debug shadow marker?
			if(showDebugShadows) {
				// reset stencil & colour ops
				targetRenderSystem.SetStencilBufferParams();
				SetPass(shadowDebugPass);
				RenderSingleObject(sr, shadowDebugPass); //, false);
				targetRenderSystem.SetColorBufferWriteEnabled(false, false, false, false);
			}
		}

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

            // Color Write
            // right now only using on/off, not per channel
            bool colWrite = pass.ColorWrite;
            targetRenderSystem.SetColorBufferWriteEnabled(colWrite, colWrite, colWrite, colWrite);

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
			planeMesh = 
				meshManager.CreateCurvedIllusionPlane(
					meshName, p, planeSize, planeSize, curvature, segments, segments, 
					false, 1, tiling, tiling, up, orientation, 
					BufferUsage.DynamicWriteOnly, BufferUsage.StaticWriteOnly, true, true);

            return planeMesh;
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
		///    Creates a query to return objects found along the ray.
		/// </summary>
		/// <returns>A specialized implementation of RaySceneQuery for this scene manager.</returns>
		public RaySceneQuery CreateRayQuery() {
			return CreateRayQuery(new Ray(), 0xffffffff);
		}

		/// <summary>
		///    Creates a query to return objects found along the ray.
		/// </summary>
		/// <param name="ray">Ray to use for the intersection query.</param>
		/// <returns>A specialized implementation of RaySceneQuery for this scene manager.</returns>
		public RaySceneQuery CreateRayQuery(Ray ray) {
			return CreateRayQuery(ray, 0xffffffff);
		}

		/// <summary>
		///    Creates a query to return objects found along the ray. 
		/// </summary>
		/// <param name="ray">Ray to use for the intersection query.</param>
		/// <returns>A specialized implementation of RaySceneQuery for this scene manager.</returns>
		public virtual RaySceneQuery CreateRayQuery(Ray ray, ulong mask) {
			DefaultRaySceneQuery query = new DefaultRaySceneQuery(this);
			query.Ray = ray;
			query.QueryMask = mask;
			return query;
		}

		/// <summary>
		///		Creates a <see cref="SphereRegionSceneQuery"/> for this scene manager. 
		/// </summary>
		/// <remarks>
		///		This method creates a new instance of a query object for this scene manager, 
		///		for querying for objects within a spherical region.
		/// </remarks>
		/// <returns>A specialized implementation of SphereRegionSceneQuery for this scene manager.</returns>
		public SphereRegionSceneQuery CreateSphereRegionQuery() {
			return CreateSphereRegionQuery(new Sphere(), 0xffffffff);
		}

		/// <summary>
		///		Creates a <see cref="SphereRegionSceneQuery"/> for this scene manager. 
		/// </summary>
		/// <remarks>
		///		This method creates a new instance of a query object for this scene manager, 
		///		for querying for objects within a spherical region.
		/// </remarks>
		/// <param name="sphere">Sphere to use for the region query.</param>
		/// <returns>A specialized implementation of SphereRegionSceneQuery for this scene manager.</returns>
		public SphereRegionSceneQuery CreateSphereRegionQuery(Sphere sphere) {
			return CreateSphereRegionQuery(sphere, 0xffffffff);
		}

		/// <summary>
		///		Creates a <see cref="SphereRegionSceneQuery"/> for this scene manager. 
		/// </summary>
		/// <remarks>
		///		This method creates a new instance of a query object for this scene manager, 
		///		for querying for objects within a spherical region.
		/// </remarks>
		/// <param name="sphere">Sphere to use for the region query.</param>
		/// <param name="mask">Custom user defined flags to use for the query.</param>
		/// <returns>A specialized implementation of SphereRegionSceneQuery for this scene manager.</returns>
		public virtual SphereRegionSceneQuery CreateSphereRegionQuery(Sphere sphere, ulong mask) {
			DefaultSphereRegionSceneQuery query = new DefaultSphereRegionSceneQuery(this);
			query.Sphere = sphere;
			query.QueryMask = mask;

			return query;
		}

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

					// skyboxes need not cast shadows
					skyBoxEntities[i].CastShadows = false;

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
					// Sky entities need not cast shadows
					skyDomeEntities[i].CastShadows = false;

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
            get { 
                return targetRenderSystem; 
            }
            set { 
                targetRenderSystem = value; 
            }
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
            get { 
                return rootSceneNode; 
            }
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
            get { 
                return ambientColor; 
            }
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
            get { 
                return optionList; 
            }
        }

		/// <summary>
		///		Gets/Sets the color used to modulate areas in shadow.
		/// </summary>
		/// <remarks>
		///		This is only applicable for shadow techniques which involve 
		///		darkening the area in shadow, as opposed to masking out the light. 
		///		This color provided is used as a modulative value to darken the
		///		areas.
		/// </remarks>
		public ColorEx ShadowColor {
			get {
				if(shadowModulativePass == null) {
					return ColorEx.Black;
				}

				return shadowModulativePass.GetTextureUnitState(0).ColorBlendMode.colorArg1;
			}
			set {
				if(shadowModulativePass == null) {
					InitShadowVolumeMaterials();
				}

				shadowModulativePass.GetTextureUnitState(0).SetColorOperationEx(
					LayerBlendOperationEx.Modulate, 
					LayerBlendSource.Manual, 
					LayerBlendSource.Current, 
					value);
			}
		}

		/// <summary>
		///		Sets the general shadow technique to be used in this scene.
		/// </summary>
		/// <remarks>
		///		There are multiple ways to generate shadows in a scene, and each has 
		///		strengths and weaknesses. 
		///		<ul><li>Stencil-based approaches can be used to 
		///		draw very long, extreme shadows without loss of precision and the 'additive'
		///		version can correctly show the shadowing of complex effects like bump mapping
		///		because they physically exclude the light from those areas. However, the edges
		///		are very sharp and stencils cannot handle transparency, and they involve a 
		///		fair amount of CPU work in order to calculate the shadow volumes, especially
		///		when animated objects are involved.</li>
		///		<li>Texture-based approaches are good for handling transparency (they can, for
		///		example, correctly shadow a mesh which uses alpha to represent holes), and they
		///		require little CPU overhead, and can happily shadow geometry which is deformed
		///		by a vertex program, unlike stencil shadows. However, they have a fixed precision 
		///		which can introduce 'jaggies' at long range and have fillrate issues of their own.</li>
		///		</ul>
		///		<p/>
		///		We support 2 kinds of stencil shadows, and 2 kinds of texture-based shadows, and one
		///		simple decal approach. The 2 stencil approaches differ in the amount of multipass work 
		///		that is required - the modulative approach simply 'darkens' areas in shadow after the 
		///		main render, which is the least expensive, whilst the additive approach has to perform 
		///		a render per light and adds the cumulative effect, whcih is more expensive but more 
		///		accurate. The texture based shadows both work in roughly the same way, the only difference is
		///		that the shadowmap approach is slightly more accurate, but requires a more recent
		///		graphics card.
		///		<p/>
		///		Note that because mixing many shadow techniques can cause problems, only one technique
		///		is supported at once. Also, you should call this method at the start of the 
		///		scene setup. 
		/// </remarks>
		public ShadowTechnique ShadowTechnique {
			get {
				return shadowTechnique;
			}
			set {
				shadowTechnique = value;

				if(shadowTechnique == ShadowTechnique.StencilAdditive ||
					shadowTechnique == ShadowTechnique.StencilModulative) {

					// create an estimated sized shadow index buffer
					shadowIndexBuffer = 
						HardwareBufferManager.Instance.CreateIndexBuffer(
							IndexType.Size16, 50000,
							BufferUsage.DynamicWriteOnly, false);

					// tell all the meshes to prepare shadow volumes
					MeshManager.Instance.PrepareAllMeshesForShadowVolumes = true;
				}
			}
		}

        /// <summary>
        ///		Gets/Sets a value that forces all nodes to render their bounding boxes.
        /// </summary>
        public bool ShowBoundingBoxes {
            get { 
                return showBoundingBoxes; 
            }
            set { 
                showBoundingBoxes = value; 
            }
        }

		/// <summary>
		///		Gets/Sets a flag that indicates whether debug shadow info (i.e. visible volumes)
		///		will be displayed.
		/// </summary>
		public virtual bool ShowDebugShadows {
			get {
				return showDebugShadows;
			}
			set {
				showDebugShadows = value;
			}
		}

        /// <summary>
        ///		Gets/Sets whether or not to display the nodes themselves in addition to their objects.
        /// </summary>
        /// <remarks>
        ///		What will be displayed is the local axes of the node (for debugging mainly).
        /// </remarks>
        public bool DisplayNodes {
            get { 
                return displayNodes; 
            }
            set { 
                displayNodes = value; 
            }
        }

        /// <summary>
        ///		Gets the fog mode that was set during the last call to SetFog.
        /// </summary>
        public FogMode FogMode {
            get { 
                return fogMode; 
            }
        }

        /// <summary>
        ///		Gets the fog starting point that was set during the last call to SetFog.
        /// </summary>
        public float FogStart {
            get { 
                return fogStart; 
            }
        }

        /// <summary>
        ///		Gets the fog ending point that was set during the last call to SetFog.
        /// </summary>
        public float FogEnd {
            get { 
                return fogEnd; 
            }
        }

        /// <summary>
        ///		Gets the fog density that was set during the last call to SetFog.
        /// </summary>
        public float FogDensity {
            get { 
                return fogDensity; 
            }
        }

        /// <summary>
        ///		Gets the fog color that was set during the last call to SetFog.
        /// </summary>
        public ColorEx FogColor {
            get { 
                return fogColor; 
            }
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
			// let the engine know this is the current scene manager
			Engine.Instance.SceneManager = this;

			// initialize shadow volume materials
			InitShadowVolumeMaterials();

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

			// auto track nodes
			for(int i = 0; i < autoTrackingSceneNodes.Count; i++) {
				autoTrackingSceneNodes[i].AutoTrack();
			}

            // ask the camera to auto track if it has a target
            camera.AutoTrack();

            // handle a reflected camera
            targetRenderSystem.InvertVertexWinding = camera.IsReflected;

            // clear the current render queue
            renderQueue.Clear();

			// Are we using any shadows at all?
			if(shadowTechnique != ShadowTechnique.None) {
				// Locate any lights which could be affecting the frustum
				FindLightsAffectingFrustum(camera);
			}

			// Deal with shadow setup
			if (shadowTechnique == ShadowTechnique.StencilAdditive) {
				// Additive stencil, we need to split everything by light
				// TODO: add a different queue handler to do this
			}

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
        protected internal virtual void UpdateSceneGraph(Camera camera) {
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
        public virtual void FindVisibleObjects(Camera camera) {
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
                pass.UpdateAutoParamsNoLights(autoParamDataSource);
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

            // set up the texture units for this pass
            for(int i = 0; i < pass.NumTextureUnitStages; i++) {
                TextureUnitState texUnit = pass.GetTextureUnitState(i);

                // issue texture units that depend on updated view matrix
                // reflective env mapping is one case
                if(texUnit.HasViewRelativeTexCoordGen) {
                    targetRenderSystem.SetTextureUnit(i, texUnit);
                }
            }

            // Normalize normals
            bool thisNormalize = renderable.NormalizeNormals;

            if(thisNormalize != normalizeNormals) {
                targetRenderSystem.NormalizeNormals = thisNormalize;
                normalizeNormals = thisNormalize;
            }

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

            // Here's where we issue the rendering operation to the render system
            // Note that we may do this once per light, therefore it's in a loop
            // and the light parameters are updated once per traversal through the
            // loop

            LightList rendLightList = renderable.Lights;
            bool iteratePerLight = pass.RunOncePerLight;
            int numIterations = iteratePerLight ? rendLightList.Count : 1;
            LightList lightListToUse = null;

            // get the renderables render operation
            renderable.GetRenderOperation(op);

            for(int i = 0; i < numIterations; i++) {
                // determine light list to use
                if(iteratePerLight) {
                    localLightList.Clear();

                    // check whether we need to filter this one out
                    if(pass.RunOnlyOncePerLightType && pass.OnlyLightType != rendLightList[i].Type) {
                        // skip this one
                        continue;
                    }

                    localLightList.Add(rendLightList[i]);
                    lightListToUse = localLightList;
                }
                else {
                    // use complete light list
                    lightListToUse = rendLightList;
                }

                if(pass.IsProgrammable) {
                    // Update any automatic gpu params for lights
                    // Other bits of information will have to be looked up
                    autoParamDataSource.SetCurrentLightList(lightListToUse);
                    pass.UpdateAutoParamsLightsOnly(autoParamDataSource);

                    // note: parameters must be bound after auto params are updated
                    if(pass.HasVertexProgram) {
                        targetRenderSystem.BindGpuProgramParameters(GpuProgramType.Vertex, pass.VertexProgramParameters);
                    }
                    if(pass.HasFragmentProgram) {
                        targetRenderSystem.BindGpuProgramParameters(GpuProgramType.Fragment, pass.FragmentProgramParameters);
                    }
                }

                // Do we need to update light states? 
                // Only do this if fixed-function vertex lighting applies
                if(pass.LightingEnabled && !pass.HasVertexProgram) {
                    targetRenderSystem.UseLights(renderable.Lights, pass.MaxLights);
                }

                // render the object as long as it has vertices
                if(op.vertexData.vertexCount > 0)
                    targetRenderSystem.Render(op);
            } // iterate per light
        }

		/// <summary>
		///		Renders a set of solid objects.
		/// </summary>
		/// <param name="list">List of solid objects.</param>
		protected virtual void RenderSolidObjects(SortedList list) {
			// ----- SOLIDS LOOP -----
			for(int i = 0; i < list.Count; i++) {
				RenderableList renderables = (RenderableList)list.GetByIndex(i);

				// bypass if this group is empty
				if(renderables.Count == 0) {
					continue;
				}

				Pass pass = (Pass)list.GetKey(i);

				// set the pass for the list of renderables to be processed
				SetPass(pass);

				// render each object associated with this rendering pass
				for(int r = 0; r < renderables.Count; r++) {
					IRenderable renderable = (IRenderable)renderables[r];

					// Render a single object, this will set up auto params if required
					RenderSingleObject(renderable, pass);
				}
			}
		}

		/// <summary>
		///		Renders a set of transparent objects.
		/// </summary>
		/// <param name="list"></param>
		protected virtual void RenderTransparentObjects(ArrayList list) {
			// ----- TRANSPARENT LOOP -----
			// This time we render by Z, not by material
			// The transparent objects set needs to be ordered first
			for(int i = 0; i < list.Count; i++) {
				RenderablePass rp = (RenderablePass)list[i];

				// set the pass first
				SetPass(rp.pass);

				// render the transparent object
				RenderSingleObject(rp.renderable, rp.pass);
			}
		}

		/// <summary>
		///		Render a group with the added complexity of additive stencil shadows.
		/// </summary>
		/// <param name="group">Render queue group.</param>
		protected virtual void RenderAdditiveStencilShadowedQueueGroupObjects(RenderQueueGroup group) {
			throw new NotImplementedException("Additive stencil shadows are not yet implemented.");
		}

		/// <summary>
		///		Render a group with the added complexity of modulative stencil shadows.
		/// </summary>
		/// <param name="group">Render queue group.</param>
		protected virtual void RenderModulativeStencilShadowedQueueGroupObjects(RenderQueueGroup group) {
			/* For each light, we need to render all the solids from each group, 
			then do the modulative shadows, then render the transparents from
			each group.
			Now, this means we are going to reorder things more, but that it required
			if the shadows are to look correct. The overall order is preserved anyway,
			it's just that all the transparents are at the end instead of them being
			interleaved as in the normal rendering loop. 
			*/
			for(int i = 0; i < group.NumPriorityGroups; i++) {
				RenderPriorityGroup priorityGroup = group.GetPriorityGroup(i);

				// sort the group first
				priorityGroup.Sort(camInProgress);

				// do solids
				RenderSolidObjects(priorityGroup.solidPassMap);
			}

			// iterate over lights, rendering all volumes to the stencil buffer
			for(int i = 0; i < lightsAffectingFrustum.Count; i++) {
				Light light = lightsAffectingFrustum[i];

				if(light.CastShadows) {
					// clear the stencil buffer
					targetRenderSystem.ClearFrameBuffer(FrameBuffer.Stencil);
					RenderShadowVolumesToStencil(light, camInProgress);

					// render full-screen shadow modulator for all lights
					SetPass(shadowModulativePass);

					// turn the stencil check on
					targetRenderSystem.StencilCheckEnabled = true;

					// we render where the stencil is not equal to zero to render shadows, not lit areas
					targetRenderSystem.SetStencilBufferParams(CompareFunction.NotEqual, 0);
					RenderSingleObject(fullScreenQuad, shadowModulativePass);

					// reset stencil buffer params
					targetRenderSystem.SetStencilBufferParams();
					targetRenderSystem.StencilCheckEnabled = false;

					// reset depth buffer params
					// TODO: Add RenderSystem.SetDepthBufferParams for convenience
					targetRenderSystem.DepthCheck = true;
					targetRenderSystem.DepthWrite = true;
					targetRenderSystem.DepthFunction = CompareFunction.LessEqual;
				}
			}

			for(int i = 0; i < group.NumPriorityGroups; i++) {
				RenderPriorityGroup priorityGroup = group.GetPriorityGroup(i);

				// do transparents
				RenderTransparentObjects(priorityGroup.transparentPasses);
			} // for each priority
		}

		/// <summary>
		///		Render the objects in a given queue group.
		/// </summary>
		/// <param name="group">Group containing the objects to render.</param>
		protected virtual void RenderQueueGroupObjects(RenderQueueGroup group) {
			// Redirect to alternate versions if stencil shadows in use
			if(group.ShadowsEnabled && shadowTechnique == ShadowTechnique.StencilAdditive) {
				RenderAdditiveStencilShadowedQueueGroupObjects(group);
			}
			else if(group.ShadowsEnabled && shadowTechnique == ShadowTechnique.StencilModulative) {
				RenderModulativeStencilShadowedQueueGroupObjects(group);
			}
			else {
				// Basic render loop
				// Iterate through priorities
				for(int i = 0; i < group.NumPriorityGroups; i++) {
					RenderPriorityGroup priorityGroup = group.GetPriorityGroup(i);

					// sort the group first
					priorityGroup.Sort(camInProgress);

					// do solids
					RenderSolidObjects(priorityGroup.solidPassMap);

					// do transparents
					RenderTransparentObjects(priorityGroup.transparentPasses);
				} // for each priority
			}
		}

        /// <summary>
        ///		Sends visible objects found in <see cref="FindVisibleObjects"/> to the rendering engine.
        /// </summary>
        protected internal virtual void RenderVisibleObjects() {
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

					if(queueGroup.NumPriorityGroups > 0) {
						// render objects in all groups
						RenderQueueGroupObjects(queueGroup);
					}

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
                qid = isSkyPlaneDrawnFirst ? RenderQueueGroupID.SkiesEarly : RenderQueueGroupID.SkiesLate;
                renderQueue.AddRenderable(skyPlaneEntity.GetSubEntity(0), 1, qid);
            }

            if(isSkyBoxEnabled) {
                qid = isSkyBoxDrawnFirst ? RenderQueueGroupID.SkiesEarly : RenderQueueGroupID.SkiesLate;

                for(int plane = 0; plane < 6; plane++)
                    renderQueue.AddRenderable(skyBoxEntities[plane].GetSubEntity(0), 1, qid);
            }

            if(isSkyDomeEnabled) {
                qid = isSkyDomeDrawnFirst ? RenderQueueGroupID.SkiesEarly : RenderQueueGroupID.SkiesLate;

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
                    planeMesh = MeshManager.Instance.CreateCurvedIllusionPlane(
                        meshName, 
                        plane, 
                        scale * 100, 
                        scale * 100, 
                        scale * bow * 100,
                        6, 6, false, 1, tiling, tiling, up);
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
				// sky entities need not cast shadows
				skyPlaneEntity.CastShadows = false;

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

		/// <summary>
		///		Internal method for notifying the manager that a SceneNode is autotracking.
		/// </summary>
		/// <param name="node">Scene node that is auto tracking another scene node.</param>
		/// <param name="autoTrack">True if tracking, false if it is stopping tracking.</param>
		internal void NotifyAutoTrackingSceneNode(SceneNode node, bool autoTrack) {
			if(autoTrack) {
				autoTrackingSceneNodes.Add(node);
			}
			else {
				autoTrackingSceneNodes.Remove(node);
			}
		}

        #endregion
    }

	#region Default SceneQuery Implementations

    /// <summary>
    ///    Default implementation of RaySceneQuery.
    /// </summary>
    public class DefaultRaySceneQuery : RaySceneQuery {
        internal DefaultRaySceneQuery(SceneManager creator) : base(creator) {}

        public override void Execute(IRaySceneQueryListener listener) {
			// Note that becuase we have no scene partitioning, we actually
			// perform a complete scene search even if restricted results are
			// requested; smarter scene manager queries can utilise the paritioning 
			// of the scene in order to reduce the number of intersection tests 
			// required to fulfil the query

			// TODO: BillboardSets? Will need per-billboard collision most likely
			// Entities only for now
            for(int i = 0; i < creator.entityList.Count; i++) {
                Entity entity = creator.entityList[i];

                // test the intersection against the world bounding box of the entity
                Pair results = MathUtil.Intersects(ray, entity.GetWorldBoundingBox());

                // if the results came back positive, fire the event handler
                if((bool)results.first == true) {
                    listener.OnQueryResult(entity, (float)results.second);
                }
            }
        }
    }

	/// <summary>
	///		Default implementation of a SphereRegionSceneQuery.
	/// </summary>
	public class DefaultSphereRegionSceneQuery : SphereRegionSceneQuery {
        internal DefaultSphereRegionSceneQuery(SceneManager creator) : base(creator) {}

		public override void Execute(ISceneQueryListener listener) {
			// TODO: BillboardSets? Will need per-billboard collision most likely
			// Entities only for now
			Sphere testSphere = new Sphere();

			for(int i = 0; i < creator.entityList.Count; i++) {
				Entity entity = creator.entityList[i];

				testSphere.Center = entity.ParentNode.DerivedPosition;
				testSphere.Radius = entity.BoundingRadius;

				// if the results came back positive, fire the event handler
				if(sphere.Intersects(testSphere)) {
					listener.OnQueryResult(entity);
				}
			}
		}

	}

	#endregion Default SceneQuery Implementations

    /// <summary>
    ///     Structure for holding a position & orientation pair.
    /// </summary>
    public struct ViewPoint {
        public Vector3 position;
        public Quaternion orientation;
    }
}

