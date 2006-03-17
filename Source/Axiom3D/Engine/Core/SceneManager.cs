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

#region Namespace Declarations

using System;
using System.Collections;
using System.Diagnostics;
using System.IO;

using Axiom.MathLib;
using Axiom.MathLib.Collections;
// This is coming from RealmForge.Utility
using Axiom.Core;

#endregion Namespace Declarations

#region Versioning Information
/// File								Revision
/// ===============================================
/// OgreSceneManager.h					1.80
/// OgreSceneManager.cpp				1.163
/// 
#endregion

namespace Axiom
{

    #region Delegate Declarations

    /// <summary>
    ///		Delegate for specifying the method signature for a render queue event.
    /// </summary>
    public delegate bool RenderQueueEvent( RenderQueueGroupID priority );

    #endregion Delegate Declarations

    #region ViewPoint Declaration
    /// <summary>
    ///     Structure for holding a position & orientation pair.
    /// </summary>
    public struct ViewPoint
    {
        public Vector3 position;
        public Quaternion orientation;
    }
    #endregion ViewPoint Declaration

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
    /// TODO Thoroughly review node removal/cleanup.
    /// TODO Review of method visibility/virtuality to ensure consistency.
	/// TODO Review Create...( string name ) methods to verify name is used as key when adding to collection
    public class SceneManager
    {
        #region Fields

        /// <summary>A queue of objects for rendering.</summary>
        protected RenderQueue renderQueue;
        /// <summary>A reference to the current active render system..</summary>
        protected RenderSystem targetRenderSystem;
        /// <summary>Denotes whether or not the camera has been changed.</summary>
        protected bool hasCameraChanged;
        /// <summary>The ambient color, cached from the RenderSystem</summary>
        /// <remarks>Default to a semi-bright white (gray) light to prevent being null</remarks>
        protected ColorEx ambientColor = ColorEx.Gray;
        /// <summary>A list of the valid cameras for this scene for easy lookup.</summary>
        protected CameraList cameraList;
        /// <summary>A list of lights in the scene for easy lookup.</summary>
        protected LightList lightList;
        /// <summary>A list of entities in the scene for easy lookup.</summary>
        protected internal EntityList entityList;
        /// <summary>A list of scene nodes (includes all in the scene graph).</summary>
        protected SceneNodeCollection sceneNodeList;
        /// <summary>A list of billboard sets for easy lookup.</summary>
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
        ///		A pass designed to let us render shadow colour on white for texture shadows
        /// </summary>
        protected Pass shadowCasterPlainBlackPass;
        /// <summary>
        ///		A pass designed to let us render shadow receivers for texture shadows
        /// </summary>
        protected Pass shadowReceiverPass;
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
        /// <summary>
        ///		Explicit extrusion distance for directional lights.
        /// </summary>
        protected float shadowDirLightExtrudeDist;
        /// <summary>
        ///		Sphere region query to find shadow casters within the attenuation range of a point/spot light.
        /// </summary>
        protected SphereRegionSceneQuery shadowCasterSphereQuery;
        /// <summary>
        /// 
        /// </summary>
        protected ColorEx shadowColor;
        /// <summary>
        ///		AxisAlignedBox region query to find shadow casters within the attenuation range of a directional light.
        /// </summary>
        protected AxisAlignedBoxRegionSceneQuery shadowCasterAABBQuery;
        /// <summary>
        ///		Listener to use when finding shadow casters for a light within a scene.
        /// </summary>
        protected ShadowCasterSceneQueryListener shadowCasterQueryListener = new ShadowCasterSceneQueryListener();
        /// <summary>
        ///		Farthest distance from the camera at which to render shadows.
        /// </summary>
        protected float shadowFarDistance;
        /// <summary>
        ///		shadowFarDistance ^ 2
        /// </summary>
        protected float shadowFarDistanceSquared;
        /// <summary>
        ///		The maximum size of the index buffer used to render shadow primitives.
        /// </summary>
        protected int shadowIndexBufferSize;
        /// <summary>
        ///		Current stage of rendering.
        /// </summary>
        protected IlluminationRenderStage illuminationStage;
        /// <summary>
        ///		Proportion of texture offset in view direction e.g. 0.4
        /// </summary>
        protected float shadowTextureOffset;
        /// <summary>
        ///		As a proportion e.g. 0.6
        /// </summary>
        protected float shadowTextureFadeStart;
        /// <summary>
        ///		As a proportion e.g. 0.9
        /// </summary>
        protected float shadowTextureFadeEnd;
        /// <summary>
        /// 
        /// </summary>
        protected ushort shadowTextureCount;
        /// <summary>
        /// 
        /// </summary>
        protected ushort shadowTextureSize;
        /// <summary>
        ///		Current list of shadow textures.
        /// </summary>
        protected RenderTextureList shadowTextures = new RenderTextureList();
        /// <summary>
        ///		Whether we should override far distance when using stencil volumes
        /// </summary>
        protected bool shadowUseInfiniteFarPlane;
        /// <summary>
        ///		Program parameters for infinite extrusion programs.
        /// </summary>
        protected GpuProgramParameters infiniteExtrusionParams;
        /// <summary>
        ///		Program parameters for finite extrusion programs.
        /// </summary>
        protected GpuProgramParameters finiteExtrusionParams;
        /// <summary>
        ///		If set, only this scene node (and children) will be rendered.
        ///		If null, root node is used.
        /// </summary>
        protected SceneNode defaultRootNode;
        /// <summary>
        ///		For the RenderTextureShadowCasterQueueGroupObjects and
        ///		RenderTextureShadowReceiverQueueGroupObjects methods.
        /// </summary>
        protected bool shadowMaterialInitDone = false;
        protected LightList nullLightList = new LightList();
        protected ulong lastFrameNumber;
        protected SceneDetailLevel lastDetailLevel = SceneDetailLevel.Points;
        protected Viewport currentViewport;

        #endregion Fields

        #region Public Events
        /// <summary>An event that will fire when a render queue is starting to be rendered.</summary>
        public event RenderQueueEvent QueueStarted;
        /// <summary>An event that will fire when a render queue is finished being rendered.</summary>
        public event RenderQueueEvent QueueEnded;
        #endregion Public Events

        #region Constructors

        public SceneManager()
        {
            cameraList = new CameraList();
            lightList = new LightList();
            entityList = new EntityList();
            sceneNodeList = new SceneNodeCollection();
            billboardSetList = new BillboardSetCollection();
            animationList = new AnimationCollection();
            animationStateList = new AnimationStateCollection();

            // create the root scene node
            rootSceneNode = new SceneNode( this, "Root" );
            defaultRootNode = rootSceneNode;

            // default to no fog
            fogMode = FogMode.None;

            // no shadows by default
            shadowTechnique = ShadowTechnique.None;

            illuminationStage = IlluminationRenderStage.None;

            shadowColor = new ColorEx( 0.25f, 0.25f, 0.25f );
            shadowDirLightExtrudeDist = 10000;
            shadowIndexBufferSize = 51200;
            shadowTextureOffset = 0.6f;
            shadowTextureFadeStart = 0.7f;
            shadowTextureFadeEnd = 0.9f;
            shadowTextureSize = 512;
            shadowTextureCount = 1;

            shadowUseInfiniteFarPlane = true;
        }

        ~SceneManager()
        {
            ClearScene();
            RemoveAllCameras();
        }

        #endregion Constructors

        #region Virtual Methods

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
        public virtual SceneNode CreateSceneNode()
        {
            SceneNode node = new SceneNode( this );
            sceneNodeList.Add( node );
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
        public virtual SceneNode CreateSceneNode( string name )
        {
            SceneNode node = new SceneNode( this, name );
            sceneNodeList.Add( name, node );
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
        public virtual Animation CreateAnimation( string name, float length )
        {
            if ( animationList.ContainsKey( name ) )
            {
                throw new AxiomException( string.Format( "An animation with the name '{0}' already exists in the scene.", name ) );
            }

            // create a new animation and record it locally
            Animation anim = new Animation( name, length );
            animationList.Add( anim.Name, anim );

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
        public virtual AnimationState CreateAnimationState( string animationName )
        {
            // do we have this already?
            if ( animationStateList.ContainsKey( animationName ) )
            {
                throw new AxiomException( "Cannot create, AnimationState already exists: " + animationName );
            }

            if ( !animationList.ContainsKey( animationName ) )
            {
                throw new AxiomException( string.Format( "The name of a valid animation must be supplied when creating an AnimationState.  Animation '{0}' does not exist.", animationName ) );
            }

            // get a reference to the sepcified animation
            Animation anim = animationList[animationName];

            // create new animation state
            AnimationState animState = new AnimationState( animationName, 0, anim.Length );

            // add it to our local list
            animationStateList.Add( animState );

            return animState;
        }

        /// <summary>
        ///		Overloaded method.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public virtual BillboardSet CreateBillboardSet( string name )
        {
            // return new billboardset with a default pool size of 20
            return CreateBillboardSet( name, 20 );
        }

        /// <summary>
        ///		Creates a billboard set which can be uses for particles, sprites, etc.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="poolSize"></param>
        /// <returns></returns>
        public virtual BillboardSet CreateBillboardSet( string name, int poolSize )
        {
            if ( billboardSetList.ContainsKey( name ) )
            {
                throw new AxiomException( string.Format( "A BillboardSet with the name '{0}' already exists in the scene.", name ) );
            }

            BillboardSet billboardSet = new BillboardSet( name, poolSize );

            // keep a local copy
            billboardSetList.Add( name, billboardSet );

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
        public virtual Camera CreateCamera( string name )
        {
            if ( cameraList.ContainsKey( name ) )
            {
                throw new AxiomException( string.Format( "A camera with the name '{0}' already exists in the scene.", name ) );
            }

            // create the camera and add it to our local list
            Camera camera = new Camera( name, this );
            cameraList.Add( name, camera );

            return camera;
        }

        /// <summary>
        ///		Create an Entity (instance of a discrete mesh).
        /// </summary>
        /// <param name="name">The name to be given to the entity (must be unique).</param>
        /// <param name="meshName">The name of the mesh to load.  Will be loaded if not already.</param>
        /// <returns></returns>
        public virtual Entity CreateEntity( string name, string meshName )
        {
            if ( entityList.ContainsKey( name ) )
                throw new AxiomException( string.Format( "An entity with the name '{0}' already exists in the scene.", name ) );

            Mesh mesh = MeshManager.Instance.Load( meshName );

            // create a new entitiy
            Entity entity = new Entity( name, mesh, this );

            // add it to our local list
            entityList.Add( entity );

            return entity;
        }

        /// <summary>
        ///		Create an Entity (instance of a discrete mesh).
        /// </summary>
        /// <param name="name">The name to be given to the entity (must be unique).</param>
        /// <param name="meshName">The name of the mesh to load.  Will be loaded if not already.</param>
        /// <returns></returns>
        public virtual Entity CreateEntity( string name, PrefabEntity prefab )
        {
            switch ( prefab )
            {
                case PrefabEntity.Plane:
                    return CreateEntity( name, "Prefab_Plane" );
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
        public virtual Light CreateLight( string name )
        {
            if ( lightList.ContainsKey( name ) )
            {
                throw new AxiomException( string.Format( "A light with the name '{0}' already exists in the scene.", name ) );
            }

            // create a new light and add it to our internal list
            Light light = new Light( name );

            // add the light to the list
            lightList.Add( name, light );

            return light;
        }

        /// <summary>
        ///		Creates a new (blank) material with the specified name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public virtual Material CreateMaterial( string name )
        {
            Material material = (Material)MaterialManager.Instance.Create( name );
            return material;
        }

        /// <summary>
        ///		Creates a new Overlay.
        /// </summary>
        /// <remarks>
        ///		<p>
        ///		Overlays can be used to render heads-up-displays (HUDs), menu systems,
        ///		cockpits and any other 2D or 3D object you need to appear above the
        ///		rest of the scene. See the Overlay class for more information.
        ///		</p>
        ///		<p>
        ///		NOTE: after creation, the Overlay is initially hidden. You can create
        ///		as many overlays as you like ready to be displayed whenever. Just call
        ///		Overlay.Show to display the overlay.
        ///		</p>
        /// </remarks>
        /// <param name="name">The name to give the overlay, must be unique.</param>
        /// <param name="zorder">The zorder of the overlay relative to it's peers, higher zorders appear on top of lower ones.</param>
        public virtual Overlay CreateOverlay( string name, int zorder )
        {
            Overlay newOverlay = (Overlay)OverlayManager.Instance.Create( name );
            newOverlay.ZOrder = zorder;

            return newOverlay;
        }

        /// <summary>
        ///		Empties the entire scene, inluding all SceneNodes, Entities, Lights, 
        ///		BillboardSets etc. Cameras are not deleted at this stage since
        ///		they are still referenced by viewports, which are not destroyed during
        ///		this process.
        /// </summary>
        public virtual void ClearScene()
        {
            // Delete all SceneNodes, except root that is
            sceneNodeList.Clear();
            autoTrackingSceneNodes.Clear();

            rootSceneNode.RemoveAllChildren();
            rootSceneNode.DetachAllObjects();

            RemoveAllEntities();
            RemoveAllBillboardSets();
            RemoveAllLights();

            // Clear animations
            DestroyAllAnimations();

            // Remove sky nodes since they've been deleted
            skyBoxNode = skyPlaneNode = skyDomeNode = null;
            isSkyBoxEnabled = isSkyPlaneEnabled = isSkyDomeEnabled = false;
        }

        /// <summary>
        ///    Destroys and removes a node from the scene.
        /// </summary>
        /// <param name="name"></param>
        public virtual void DestroySceneNode( string name )
        {
            if ( sceneNodeList[name] == null )
            {
                throw new AxiomException( "SceneNode named '{0}' not found.", name );
            }

            // grab the node from the list
            SceneNode node = (SceneNode)sceneNodeList[name];

            // Find any scene nodes which are tracking this node, and turn them off.
            for ( int i = 0; i < autoTrackingSceneNodes.Count; i++ )
            {
                SceneNode autoNode = autoTrackingSceneNodes[i];

                // Tracking this node
                if ( autoNode.AutoTrackTarget == node )
                {
                    // turn off, this will notify SceneManager to remove
                    autoNode.SetAutoTracking( false );
                }
                else if ( autoNode == node )
                {
                    // node being removed is a tracker
                    autoTrackingSceneNodes.Remove( autoNode );
                }
            }

            /* CMH 7/18/2004 */
            // Destroy child nodes
            // We can't use a for loop here, since the recursion mutates node.ChildCount
            while ( node.ChildCount > 0 )
            {
                DestroySceneNode( node.GetChild( 0 ).Name );
            }

            for ( int j = 0; j < node.ObjectCount; j++ )
            {
                MovableObject obj = node.GetObject( j );

                if ( obj is Camera )
                    cameraList.Remove( (Camera)obj );
                else if ( obj is Light )
                    lightList.Remove( (Light)obj );
                else if ( obj is Entity )
                    entityList.Remove( (Entity)obj );
                else if ( obj is BillboardSet )
                    billboardSetList.Remove( (BillboardSet)obj );
            }
            // Remove this node from its parent
            node.Parent.RemoveChild( node );

            // removes the node from the list
            sceneNodeList.Remove( node );
        }

        /// <summary>
        ///		Destroys an Animation. 
        /// </summary>
        /// <param name="name"></param>
        public virtual void DestroyAnimation( string name )
        {
            // Also destroy any animation states referencing this animation
            for ( int i = 0; i < animationStateList.Count; i++ )
            {
                AnimationState animState = animationStateList[i];

                if ( animState.Name == name )
                {
                    animationStateList.Remove( animState );
                }
            }

            Animation animation = animationList[name];
            if ( animation == null )
                throw new AxiomException( "Animation named '{0}' not found.", name );

            animationList.Remove( animation );
        }

        /// <summary>
        ///		Destroys an AnimationState. 
        /// </summary>
        /// <param name="name"></param>
        public virtual void DestroyAnimationState( string name )
        {
            AnimationState animationState = animationStateList[name];
            if ( animationState == null )
                throw new AxiomException( "AnimationState named '{0}' not found.", name );

            animationStateList.Remove( animationState );
        }

        /// <summary>
        ///		Removes all animations created using this SceneManager.
        /// </summary>
        public virtual void DestroyAllAnimations()
        {
            // Destroy all states too, since they cannot reference destroyed animations
            DestroyAllAnimationStates();
            animationList.Clear();
        }

        /// <summary>
        ///		Removes all AnimationStates created using this SceneManager.
        /// </summary>
        public virtual void DestroyAllAnimationStates()
        {
            animationStateList.Clear();
        }

        /// <summary>
        ///		Destroys all the overlays.
        /// </summary>
        public virtual void DestroyAllOverlays()
        {
            OverlayManager.Instance.UnloadAndDestroyAll();
        }

        /// <summary>
        ///		Destroys the named Overlay.
        /// </summary>
        public virtual void DestroyOverlay( string name )
        {
            Overlay overlay = OverlayManager.Instance.GetByName( name );

            if ( overlay == null )
                throw new AxiomException( "An overlay named " + name + " cannot be found to be destroyed." );

            OverlayManager.Instance.Unload( overlay );
        }

        /// <summary>
        ///     Retreives the camera with the specified name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public virtual Camera GetCamera( string name )
        {
            Camera camera = cameraList[name];
            if ( camera == null )
            {
                throw new AxiomException( "Camera named '{0}' not found.", name );
            }

            return camera;
        }

        /// <summary>
        ///     Retreives the light with the specified name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public virtual Light GetLight( string name )
        {
            Light light = lightList[name];
            if ( light == null )
            {
                throw new AxiomException( "Light named '{0}' not found.", name );
            }

            return light;
        }

        /// <summary>
        ///     Retreives the BillboardSet with the specified name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public virtual BillboardSet GetBillboardSet( string name )
        {
            BillboardSet billboardSet = billboardSetList[name];
            if ( billboardSet == null )
            {
                throw new AxiomException( "BillboardSet named '{0}' not found.", name );
            }

            return billboardSet;
        }

        /// <summary>
        ///     Retreives the animation with the specified name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public virtual Animation GetAnimation( string name )
        {
            Animation animation = animationList[name];
            /*
			if(animation == null) {
				throw new AxiomException("Animation named '{0}' not found.", name);
			}*/
            return animation;
        }

        /// <summary>
        ///     Retreives the AnimationState with the specified name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public virtual AnimationState GetAnimationState( string name )
        {
            AnimationState animationState = animationStateList[name];
            /*
			if(animationState == null) {
				throw new AxiomException("AnimationState named '{0}' not found.", name);
			}*/
            return animationState;
        }

        /// <summary>
        ///		Gets a the named Overlay, previously created using CreateOverlay.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public virtual Overlay GetOverlay( string name )
        {
            Overlay overlay = OverlayManager.Instance.GetByName( name );
            /*
			if (overlay == null)
				throw new AxiomException("An overlay named " + name + " cannot be found.");
			*/
            return overlay;
        }

        /// <summary>
        ///     Returns the material with the specified name.
        /// </summary>
        /// <param name="name">Name of the material to retrieve.</param>
        /// <returns>A reference to a Material.</returns>
        public virtual Material GetMaterial( string name )
        {
            return MaterialManager.Instance.GetByName( name );
        }

        /// <summary>
                ///     Returns the material with the specified handle.
                /// </summary>
                /// <param name="name">Handle of the material to retrieve.</param>
        /// <returns>A reference to a Material.</returns>
        public virtual Material GetMaterial( int handle )
        {
            return (Material)MaterialManager.Instance.GetByHandle( handle );
        }

        /// <summary>
        ///		Retrieves the internal render queue.
        /// </summary>
        /// <returns>
        ///		The render queue in use by this scene manager.
        ///		Note: The queue is created if it doesn't already exist.
        /// </returns>
                protected virtual RenderQueue GetRenderQueue()
        {
                        if ( renderQueue == null )
            {
                                InitRenderQueue();
            }

            return renderQueue;
        }

        /// <summary>
        ///		Internal method for initializing the render queue.
        /// </summary>
        /// <remarks>
        ///		Subclasses can use this to install their own <see cref="RenderQueue"/> implementation.
        /// </remarks>
        protected virtual void InitRenderQueue()
        {
                        renderQueue = new RenderQueue();

                        // init render queues that do not need shadows
            renderQueue.GetQueueGroup( RenderQueueGroupID.Background ).ShadowsEnabled = false;
            renderQueue.GetQueueGroup( RenderQueueGroupID.Overlay ).ShadowsEnabled = false;
            renderQueue.GetQueueGroup( RenderQueueGroupID.SkiesEarly ).ShadowsEnabled = false;
            renderQueue.GetQueueGroup( RenderQueueGroupID.SkiesLate ).ShadowsEnabled = false;
        }

        /// <summary>
        ///     Retreives the scene node with the specified name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public SceneNode GetSceneNode( string name )
        {
            SceneNode node = sceneNodeList[name];
            /*
			 * if(node == null)
			 *		throw new AxiomException("Scene node '{0}' could not be found.",name);
			 * */
            return node;
        }

        /// <summary>
        ///     Retreives the scene node with the specified name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public SceneNode GetSceneNode( int index )
        {
            int count = sceneNodeList.Count;
            if ( index > count || index < 0 )
                throw new ArgumentOutOfRangeException( "Scene node index is invalid." );
            SceneNode node = sceneNodeList[index];
            return node;
        }



        /// <summary>
        ///     Retreives the scene node with the specified name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Entity GetEntity( string name )
        {
            Entity entity = entityList[name];
            /*
			 * if(entity == null)
			 *		throw new AxiomException("Entity '{0}' could not be found.",name);
			 * */
            return entity;
        }

        /// <summary>
        ///     Retreives the scene node with the specified name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Entity GetEntity( int index )
        {
            int count = entityList.Count;
            if ( index > count || index < 0 )
                throw new ArgumentOutOfRangeException( "Entity index is invalid." );
            Entity entity = entityList[index];
            return entity;
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
        public virtual ViewPoint GetSuggestedViewpoint( bool random )
        {
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
        public virtual void LoadWorldGeometry( string fileName )
        {
            // This default implementation cannot handle world geometry
            throw new AxiomException( "World geometry is not supported by the generic SceneManager." );
        }

        public void ManualRender( RenderOperation op, Pass pass, Viewport vp,
            Matrix4 worldMatrix, Matrix4 viewMatrix, Matrix4 projMatrix )
        {

            ManualRender( op, pass, vp, worldMatrix, viewMatrix, projMatrix, false );
        }

        /// <summary>
        ///		Manual rendering method, for advanced users only.
        /// </summary>
        /// <remarks>
        ///		This method allows you to send rendering commands through the pipeline on
        ///		demand, bypassing any normal world processing. You should only use this if you
        ///		really know what you're doing; the engine does lots of things for you that you really should
        ///		let it do. However, there are times where it may be useful to have this manual interface,
        ///		for example overlaying something on top of the scene.
        ///		<p/>
        ///		Because this is an instant rendering method, timing is important. The best 
        ///		time to call it is from a RenderTarget event handler.
        ///		<p/>
        ///		Don't call this method a lot, it's designed for rare (1 or 2 times per frame) use. 
        ///		Calling it regularly per frame will cause frame rate drops!
        /// </remarks>
        /// <param name="op">A RenderOperation object describing the rendering op.</param>
        /// <param name="pass">The Pass to use for this render.</param>
        /// <param name="vp">Reference to the viewport to render to.</param>
        /// <param name="worldMatrix">The transform to apply from object to world space.</param>
        /// <param name="viewMatrix">The transform to apply from object to view space.</param>
        /// <param name="projMatrix">The transform to apply from view to screen space.</param>
        /// <param name="doBeginEndFrame">
        ///		If true, BeginFrame() and EndFrame() are called, otherwise not. 
        ///		You should leave this as false if you are calling this within the main render loop.
        /// </param>
        public virtual void ManualRender( RenderOperation op, Pass pass, Viewport vp,
            Matrix4 worldMatrix, Matrix4 viewMatrix, Matrix4 projMatrix, bool doBeginEndFrame )
        {

            // configure all necessary parameters
            targetRenderSystem.SetViewport( vp );
            targetRenderSystem.WorldMatrix = worldMatrix;
            targetRenderSystem.ViewMatrix = viewMatrix;
            targetRenderSystem.ProjectionMatrix = projMatrix;

            if ( doBeginEndFrame )
            {
                targetRenderSystem.BeginFrame();
            }

            // set the pass and render the object
            SetPass( pass );
            targetRenderSystem.Render( op );

            if ( doBeginEndFrame )
            {
                targetRenderSystem.EndFrame();
            }
        }

        #endregion Virtual Methods

        #region Protected Methods

        /// <summary>
        ///		Internal method for locating a list of lights which could be affecting the frustum.
        /// </summary>
        /// <remarks>
        ///		Custom scene managers are encouraged to override this method to make use of their
        ///		scene partitioning scheme to more efficiently locate lights, and to eliminate lights
        ///		which may be occluded by word geometry.
        /// </remarks>
        /// <param name="camera">Camera to find lights within it's view.</param>
        protected virtual void FindLightsAffectingFrustum( Camera camera )
        {
            // Basic iteration for this scene manager
            lightsAffectingFrustum.Clear();

            // sphere to use for testing
            Sphere sphere = new Sphere();

            for ( int i = 0; i < lightList.Count; i++ )
            {
                Light light = lightList[i];

                if ( light.Type == LightType.Directional )
                {
                    // Always visible
                    lightsAffectingFrustum.Add( light );
                }
                else
                {
                    // treating spotlight as point for simplicity
                    // Just see if the lights attenuation range is within the frustum
                    sphere.Center = light.DerivedPosition;
                    sphere.Radius = light.AttenuationRange;

                    if ( camera.IsObjectVisible( sphere ) )
                    {
                        lightsAffectingFrustum.Add( light );
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
        protected virtual IList FindShadowCastersForLight( Light light, Camera camera )
        {
            shadowCasterList.Clear();

            if ( light.Type == LightType.Directional )
            {
                // Basic AABB query encompassing the frustum and the extrusion of it
                AxisAlignedBox aabb = new AxisAlignedBox();
                Vector3[] corners = camera.WorldSpaceCorners;
                Vector3 min, max;
                Vector3 extrude = light.DerivedDirection * -shadowDirLightExtrudeDist;
                // do first corner
                min = max = corners[0];
                min.Floor( corners[0] + extrude );
                max.Ceil( corners[0] + extrude );
                for ( int c = 1; c < 8; ++c )
                {
                    min.Floor( corners[c] );
                    max.Ceil( corners[c] );
                    min.Floor( corners[c] + extrude );
                    max.Ceil( corners[c] + extrude );
                }
                aabb.SetExtents( min, max );

                if ( shadowCasterAABBQuery == null )
                    shadowCasterAABBQuery = CreateAABBRegionQuery( aabb );
                else
                    shadowCasterAABBQuery.Box = aabb;
                // Execute, use callback
                shadowCasterQueryListener.Prepare( false,
                    light.GetFrustumClipVolumes( camera ),
                    light, camera, shadowCasterList, shadowFarDistanceSquared );
                shadowCasterAABBQuery.Execute( shadowCasterQueryListener );
            }
            else
            {
                Sphere s = new Sphere( light.DerivedPosition, light.AttenuationRange );

                // eliminate early if camera cannot see light sphere
                if ( camera.IsObjectVisible( s ) )
                {
                    // create or init a sphere region query
                    if ( shadowCasterSphereQuery == null )
                    {
                        shadowCasterSphereQuery = CreateSphereRegionQuery( s );
                    }
                    else
                    {
                        shadowCasterSphereQuery.Sphere = s;
                    }

                    // check if the light is within view of the camera
                    bool lightInFrustum = camera.IsObjectVisible( light.DerivedPosition );

                    PlaneBoundedVolumeList volumeList = null;

                    // Only worth building an external volume list if
                    // light is outside the frustum
                    if ( !lightInFrustum )
                    {
                        volumeList = light.GetFrustumClipVolumes( camera );
                    }

                    // prepare the query and execute using the callback
                    shadowCasterQueryListener.Prepare(
                        lightInFrustum, volumeList,
                        light, camera,
                        shadowCasterList,
                        shadowFarDistanceSquared );

                    shadowCasterSphereQuery.Execute( shadowCasterQueryListener );
                }
            }

            return shadowCasterList;
        }


        protected const string SPOT_SHADOW_FADE_IMAGE = "spot_shadow_fade.png";
        protected const string TEXTURE_SHADOW_RECEIVER_MATERIAL = "Axiom/TextureShadowReceiver";
        protected const string TEXTURE_SHADOW_CASTER_MATERIAL = "Axiom/TextureShadowCaster";
        protected const string STENCIL_SHADOW_MODULATIVE_MATERIAL = "Axiom/StencilShadowModulationPass";
        protected const string STENCIL_SHADOW_VOLUMES_MATERIAL = "Axiom/StencilShadowVolumes";
        protected const string SHADOW_VOLUMES_MATERIAL = "Axiom/Debug/ShadowVolumes";

        /// <summary>
        ///		Internal method for setting up materials for shadows.
        /// </summary>
        protected virtual void InitShadowVolumeMaterials()
        {
            if ( shadowMaterialInitDone )
            {
                return;
            }

            if ( shadowDebugPass == null )
            {
                InitShadowDebugPass();
            }

            if ( shadowStencilPass == null )
            {
                InitShadowStencilPass();
            }


            if ( shadowModulativePass == null )
            {
                InitShadowModulativePass();
            }

            // Also init full screen quad while we're at it
            if ( fullScreenQuad == null )
            {
                fullScreenQuad = new Rectangle2D();
                fullScreenQuad.SetCorners( -1, 1, 1, -1 );
            }

            // Also init shadow caster material for texture shadows
            if ( shadowCasterPlainBlackPass == null )
            {
                InitShadowCasterPass();
            }

            if ( shadowReceiverPass == null )
            {
                InitShadowRecieverPass();
            }

            // Set up spot shadow fade texture (loaded from code data block)
            Texture spotShadowFadeTex = TextureManager.Instance.GetByName( SPOT_SHADOW_FADE_IMAGE );

            if ( spotShadowFadeTex == null )
            {
                // Load the manual buffer into an image
                MemoryStream imgStream = new MemoryStream( SpotShadowFadePng.SPOT_SHADOW_FADE_PNG );
                Image img = Image.FromStream( imgStream, "png" );
                spotShadowFadeTex =
                    TextureManager.Instance.LoadImage( SPOT_SHADOW_FADE_IMAGE, img, TextureType.TwoD );
            }

            shadowMaterialInitDone = true;
        }

        private void InitShadowRecieverPass()
        {
            Material matShadRec = MaterialManager.Instance.GetByName( TEXTURE_SHADOW_RECEIVER_MATERIAL );

            if ( matShadRec == null )
            {
                matShadRec = (Material)MaterialManager.Instance.Create( TEXTURE_SHADOW_RECEIVER_MATERIAL );
                shadowReceiverPass = matShadRec.GetTechnique( 0 ).GetPass( 0 );
                shadowReceiverPass.SetSceneBlending( SceneBlendFactor.DestColor, SceneBlendFactor.Zero );
                // No lighting, one texture unit 
                // everything else will be bound as needed during the receiver pass
                shadowReceiverPass.LightingEnabled = false;
                TextureUnitState t = shadowReceiverPass.CreateTextureUnitState();
                t.TextureAddressing = TextureAddressing.Clamp;
            }
            else
            {
                shadowReceiverPass = matShadRec.GetTechnique( 0 ).GetPass( 0 );
            }
        }

        private void InitShadowCasterPass()
        {
            Material matPlainBlack = MaterialManager.Instance.GetByName( TEXTURE_SHADOW_CASTER_MATERIAL );

            if ( matPlainBlack == null )
            {
                matPlainBlack = (Material)MaterialManager.Instance.Create( TEXTURE_SHADOW_CASTER_MATERIAL );
                shadowCasterPlainBlackPass = matPlainBlack.GetTechnique( 0 ).GetPass( 0 );
                // Lighting has to be on, because we need shadow coloured objects
                // Note that because we can't predict vertex programs, we'll have to
                // bind light values to those, and so we bind White to ambient
                // reflectance, and we'll set the ambient colour to the shadow colour
                shadowCasterPlainBlackPass.Ambient = ColorEx.White;
                shadowCasterPlainBlackPass.Diffuse = ColorEx.Black;
                shadowCasterPlainBlackPass.Emissive = ColorEx.Black;
                shadowCasterPlainBlackPass.Specular = ColorEx.Black;
                // no textures or anything else, we will bind vertex programs
                // every so often though
            }
            else
            {
                shadowCasterPlainBlackPass = matPlainBlack.GetTechnique( 0 ).GetPass( 0 );
            }
        }

        private void InitShadowModulativePass()
        {
            Material matModStencil =
                MaterialManager.Instance.GetByName( STENCIL_SHADOW_MODULATIVE_MATERIAL );

            if ( matModStencil == null )
            {
                // Create
                matModStencil =
                    (Material)MaterialManager.Instance.Create( STENCIL_SHADOW_MODULATIVE_MATERIAL );

                shadowModulativePass = matModStencil.GetTechnique( 0 ).GetPass( 0 );
                shadowModulativePass.SetSceneBlending( SceneBlendFactor.DestColor, SceneBlendFactor.Zero );
                shadowModulativePass.LightingEnabled = false;
                shadowModulativePass.DepthWrite = false;
                shadowModulativePass.DepthCheck = false;
                TextureUnitState t = shadowModulativePass.CreateTextureUnitState();
                t.SetColorOperationEx(
                    LayerBlendOperationEx.Modulate,
                    LayerBlendSource.Manual,
                    LayerBlendSource.Current,
                    shadowColor );
            }
            else
            {
                shadowModulativePass = matModStencil.GetTechnique( 0 ).GetPass( 0 );
            }
        }

        private void InitShadowDebugPass()
        {
            Material matDebug = MaterialManager.Instance.GetByName( SHADOW_VOLUMES_MATERIAL );

            if ( matDebug == null )
            {
                // Create
                matDebug = (Material)MaterialManager.Instance.Create( SHADOW_VOLUMES_MATERIAL );
                shadowDebugPass = matDebug.GetTechnique( 0 ).GetPass( 0 );
                shadowDebugPass.SetSceneBlending( SceneBlendType.Add );
                shadowDebugPass.LightingEnabled = false;
                shadowDebugPass.DepthWrite = false;
                TextureUnitState t = shadowDebugPass.CreateTextureUnitState();
                t.SetColorOperationEx(
                    LayerBlendOperationEx.Modulate,
                    LayerBlendSource.Manual,
                    LayerBlendSource.Current,
                    new ColorEx( 0.7f, 0.0f, 0.2f ) );

                shadowDebugPass.CullMode = CullingMode.None;

                if ( targetRenderSystem.Caps.CheckCap( Capabilities.VertexPrograms ) )
                {
                    ShadowVolumeExtrudeProgram.Initialize();

                    // Enable the (infinite) point light extruder for now, just to get some params
                    shadowDebugPass.SetVertexProgram(
                        ShadowVolumeExtrudeProgram.GetProgramName( ShadowVolumeExtrudeProgram.Programs.PointLight ) );

                    infiniteExtrusionParams = shadowDebugPass.VertexProgramParameters;
                    infiniteExtrusionParams.SetAutoConstant( 0, AutoConstants.WorldViewProjMatrix );
                    infiniteExtrusionParams.SetAutoConstant( 4, AutoConstants.LightPositionObjectSpace );
                }

                matDebug.Compile();
            }
            else
            {
                shadowDebugPass = matDebug.GetTechnique( 0 ).GetPass( 0 );
            }
        }

        private void InitShadowStencilPass()
        {
            Material matStencil = MaterialManager.Instance.GetByName( STENCIL_SHADOW_VOLUMES_MATERIAL );

            if ( matStencil == null )
            {
                // Create
                matStencil = (Material)MaterialManager.Instance.Create( STENCIL_SHADOW_VOLUMES_MATERIAL );
                shadowStencilPass = matStencil.GetTechnique( 0 ).GetPass( 0 );

                if ( targetRenderSystem.Caps.CheckCap( Capabilities.VertexPrograms ) )
                {
                    // Enable the finite point light extruder for now, just to get some params
                    shadowStencilPass.SetVertexProgram(
                        ShadowVolumeExtrudeProgram.GetProgramName( ShadowVolumeExtrudeProgram.Programs.PointLightFinite ) );

                    finiteExtrusionParams = shadowStencilPass.VertexProgramParameters;
                    finiteExtrusionParams.SetAutoConstant( 0, AutoConstants.WorldViewProjMatrix );
                    finiteExtrusionParams.SetAutoConstant( 4, AutoConstants.LightPositionObjectSpace );
                    finiteExtrusionParams.SetAutoConstant( 5, AutoConstants.ShadowExtrusionDistance );
                }
                matStencil.Compile();
                // Nothing else, we don't use this like a 'real' pass anyway,
                // it's more of a placeholder
            }
            else
            {
                shadowStencilPass = matStencil.GetTechnique( 0 ).GetPass( 0 );
            }
        }


        /// <summary>
        ///		Internal method for turning a regular pass into a shadow caster pass.
        /// </summary>
        /// <remarks>
        ///		This is only used for texture shadows, basically we're trying to
        ///		ensure that objects are rendered solid black.
        ///		This method will usually return the standard solid black pass for
        ///		all fixed function passes, but will merge in a vertex program
        ///		and fudge the AutpoParamDataSource to set black lighting for
        ///		passes with vertex programs. 
        /// </remarks>
        /// <param name="pass"></param>
        /// <returns></returns>
        protected virtual Pass DeriveShadowCasterPass( Pass pass )
        {
            switch ( shadowTechnique )
            {
                case ShadowTechnique.TextureModulative:
                    if ( pass.HasVertexProgram )
                    {
                        // TODO Add hardware ShadowCasterVertexProgram
                        /*
						// Have to merge the shadow caster vertex program in
						// This may in fact be blank, in which case it falls back on 
						// fixed function
						shadowCasterPlainBlackPass.VertexProgramName =
							pass.ShadowCasterVertexProgramName;
						// Did this result in a new vertex program?
						if (shadowCasterPlainBlackPass.HasVertexProgram)
						{
							GpuProgram prg = shadowCasterPlainBlackPass.VertexProgram;
							// Load this program if not done already
							if (!prg.IsLoaded)
								prg.Load();
							// Copy params
							shadowCasterPlainBlackPass.VertexProgramParameters =
								pass.ShadowCasterVertexProgramParameters;
						}
						*/
                        // Also have to hack the light autoparams, that is done later
                    }
                    else if ( shadowCasterPlainBlackPass.HasVertexProgram )
                    {
                        // reset
                        shadowCasterPlainBlackPass.SetVertexProgram( "" );
                    }
                    return shadowCasterPlainBlackPass;
                /*
					case SHADOWTYPE_TEXTURE_SHADOWMAP:
					// todo
					return pass;
					*/
                default:
                    return pass;
            }

        }

        /// <summary>
        ///		Internal method for turning a regular pass into a shadow receiver pass.
        /// </summary>
        /// <remarks>
        ///		This is only used for texture shadows, basically we're trying to
        ///		ensure that objects are rendered with a projective texture.
        ///		This method will usually return a standard single-texture pass for
        ///		all fixed function passes, but will merge in a vertex program
        ///		for passes with vertex programs. 
        /// </remarks>
        /// <param name="pass"></param>
        /// <returns></returns>
        protected virtual Pass DeriveShadowReceiverPass( Pass pass )
        {
            switch ( shadowTechnique )
            {
                case ShadowTechnique.TextureModulative:
                    if ( pass.HasVertexProgram )
                    {
                        // TODO Add hardware shadows
                        /*
						// Have to merge the receiver vertex program in
						// This may return "" which means fixed function will be used
						shadowReceiverPass.VertexProgramName =
							pass.ShadowReceiverVertexProgramName;
						// Did this result in a new vertex program?
						if (shadowReceiverPass.HasVertexProgram)
						{
							GpuProgram prg = shadowReceiverPass.VertexProgram;
							// Load this program if required
							if (!prg.IsLoaded)
								prg.Load();
							// Copy params
							shadowReceiverPass.VertexProgramParameters =
								pass.ShadowReceiverVertexProgramParameters;
						}
						*/
                        // Also have to hack the light autoparams, that is done later
                    }
                    else if ( shadowReceiverPass.HasVertexProgram )
                    {
                        // reset
                        shadowReceiverPass.SetVertexProgram( "" );
                    }

                    return shadowReceiverPass;
                /*
					case SHADOWTYPE_TEXTURE_SHADOWMAP:
					// todo
					return pass;
					*/
                default:
                    return pass;
            }
        }

        /// <summary>
        ///		Internal method for rendering all the objects for a given light into the stencil buffer.
        /// </summary>
        /// <param name="light">The light source.</param>
        /// <param name="camera">The camera being viewed from.</param>
        protected virtual void RenderShadowVolumesToStencil( Light light, Camera camera )
        {
            // Set up scissor test (point & spot lights only)
            bool scissored = false;

                        if ( light.Type != LightType.Directional &&
                                targetRenderSystem.Caps.CheckCap( Capabilities.ScissorTest ) )
            {
                                // Project the sphere onto the camera
                                float left, right, top, bottom;
                                Sphere sphere = new Sphere( light.DerivedPosition, light.AttenuationRange );
                                if ( camera.ProjectSphere( sphere, out left, out top, out right, out bottom ) )
                {
                                        scissored = true;
                                        // Turn normalised device coordinates into pixels
                                        int iLeft, iTop, iWidth, iHeight;
                                        currentViewport.GetActualDimensions( out iLeft, out iTop, out iWidth, out iHeight );
                                        int szLeft, szRight, szTop, szBottom;

                                        szLeft = (int)( iLeft + ( ( left + 1 ) * 0.5f * iWidth ) );
                                        szRight = (int)( iLeft + ( ( right + 1 ) * 0.5f * iWidth ) );
                                        szTop = (int)( iTop + ( ( -top + 1 ) * 0.5f * iHeight ) );
                                        szBottom = (int)( iTop + ( ( -bottom + 1 ) * 0.5f * iHeight ) );

                    targetRenderSystem.SetScissorTest( true, szLeft, szTop, szRight, szBottom );
                }
            }

            targetRenderSystem.UnbindGpuProgram( GpuProgramType.Fragment );

            // Can we do a 2-sided stencil?
            bool stencil2sided = false;

                    if ( targetRenderSystem.Caps.CheckCap( Capabilities.TwoSidedStencil ) &&
                            targetRenderSystem.Caps.CheckCap( Capabilities.StencilWrap ) )
            {
                            // enable
                stencil2sided = true;
            }

            // Do we have access to vertex programs?
            bool extrudeInSoftware = true;

            bool finiteExtrude = !shadowUseInfiniteFarPlane ||
                !targetRenderSystem.Caps.CheckCap( Capabilities.InfiniteFarPlane );

            if ( targetRenderSystem.Caps.CheckCap( Capabilities.VertexPrograms ) )
            {
                extrudeInSoftware = false;
                EnableHardwareShadowExtrusion( light, finiteExtrude );
            }
            else
            {
                targetRenderSystem.UnbindGpuProgram( GpuProgramType.Vertex );
            }

            // Add light to internal list for use in render call
            LightList tmpLightList = new LightList();
            tmpLightList.Add( light );

            // Turn off color writing and depth writing
            targetRenderSystem.SetColorBufferWriteEnabled( false, false, false, false );
            targetRenderSystem.DepthWrite = false;
            targetRenderSystem.StencilCheckEnabled = true;
            targetRenderSystem.DepthFunction = CompareFunction.Less;

                        // Calculate extrusion distance
                        float extrudeDistance = 0;
                        if ( light.Type == LightType.Directional )
            {
                                extrudeDistance = shadowDirLightExtrudeDist;
                        }

                        // get the near clip volume
                        PlaneBoundedVolume nearClipVol = light.GetNearClipVolume( camera );

                        // get the shadow caster list
                        IList casters = FindShadowCastersForLight( light, camera );

                        // Determine whether zfail is required
                        // We need to use zfail for ALL objects if we find a single object which
                        // requires it
                        bool zfailAlgo = false;
            int flags = 0;

            CheckShadowCasters( casters, nearClipVol, light, extrudeInSoftware, finiteExtrude, zfailAlgo, camera, extrudeDistance, stencil2sided, tmpLightList );
            // revert colour write state
            targetRenderSystem.SetColorBufferWriteEnabled( true, true, true, true );
            // revert depth state
            targetRenderSystem.SetDepthBufferParams();

            targetRenderSystem.StencilCheckEnabled = false;

                    targetRenderSystem.UnbindGpuProgram( GpuProgramType.Vertex );

                    if ( scissored )
            {
                            // disable scissor test
                targetRenderSystem.SetScissorTest( false );
            }
        }

        private void EnableHardwareShadowExtrusion( Light light, bool finiteExtrude )
        {
            // attach the appropriate extrusion vertex program
            // Note we never unset it because support for vertex programs is constant
            shadowStencilPass.SetVertexProgram(
                ShadowVolumeExtrudeProgram.GetProgramName( light.Type, finiteExtrude, false ) );

            // Set params
            if ( finiteExtrude )
            {
                shadowStencilPass.VertexProgramParameters = finiteExtrusionParams;
            }
            else
            {
                shadowStencilPass.VertexProgramParameters = infiniteExtrusionParams;
            }

            if ( showDebugShadows )
            {
                shadowDebugPass.SetVertexProgram(
                    ShadowVolumeExtrudeProgram.GetProgramName( light.Type, finiteExtrude, true ) );

                // Set params
                if ( finiteExtrude )
                {
                    shadowDebugPass.VertexProgramParameters = finiteExtrusionParams;
                }
                else
                {
                    shadowDebugPass.VertexProgramParameters = infiniteExtrusionParams;
                }
            }

            targetRenderSystem.BindGpuProgram( shadowStencilPass.VertexProgram );
        }

        private void CheckShadowCasters( IList casters, PlaneBoundedVolume nearClipVol, Light light, bool extrudeInSoftware, bool finiteExtrude, bool zfailAlgo, Camera camera, float extrudeDistance, bool stencil2sided, LightList tmpLightList )
        {
            int flags;
            for ( int i = 0; i < casters.Count; i++ )
            {
                ShadowCaster caster = (ShadowCaster)casters[i];

                if ( nearClipVol.Intersects( caster.GetWorldBoundingBox() ) )
                {
                    // We have a zfail case, we must use zfail for all objects
                    zfailAlgo = true;

                    break;
                }
            }

            for ( int ci = 0; ci < casters.Count; ci++ )
            {
                ShadowCaster caster = (ShadowCaster)casters[ci];
                flags = 0;

                if ( light.Type != LightType.Directional )
                {
                    extrudeDistance = caster.GetPointExtrusionDistance( light );
                }

                if ( !extrudeInSoftware && !finiteExtrude )
                {
                    // hardware extrusion, to infinity (and beyond!)
                    flags |= (int)ShadowRenderableFlags.ExtrudeToInfinity;
                }

                if ( zfailAlgo )
                {
                    // We need to include the light and / or dark cap
                    // But only if they will be visible
                    if ( camera.IsObjectVisible( caster.GetLightCapBounds() ) )
                    {
                        flags |= (int)ShadowRenderableFlags.IncludeLightCap;
                    }
                }

                // Dark cap (no dark cap for directional lights using 
                // hardware extrusion to infinity)
                if ( !( ( flags & (int)ShadowRenderableFlags.ExtrudeToInfinity ) != 0 &&
                     light.Type == LightType.Directional ) &&
                   camera.IsObjectVisible( caster.GetDarkCapBounds( light, extrudeDistance ) ) )
                {
                    flags |= (int)ShadowRenderableFlags.IncludeDarkCap;
                }

                // get shadow renderables
                IEnumerator renderables = caster.GetShadowVolumeRenderableEnumerator(
                    shadowTechnique, light, shadowIndexBuffer, extrudeInSoftware, extrudeDistance, flags );

                // If using one-sided stencil, render the first pass of all shadow
                // renderables before all the second passes
                for ( int i = 0; i < ( stencil2sided ? 1 : 2 ); i++ )
                {
                    if ( i == 1 )
                        renderables = caster.GetLastShadowVolumeRenderableEnumerator();

                    while ( renderables.MoveNext() )
                    {
                        ShadowRenderable sr = (ShadowRenderable)renderables.Current;

                        // omit hidden renderables
                        if ( sr.IsVisible )
                        {
                            // render volume, including dark and (maybe) light caps
                            RenderSingleShadowVolumeToStencil( sr, zfailAlgo, stencil2sided, tmpLightList, ( i > 0 ) );

                            // optionally render separate light cap
                            if ( sr.IsLightCapSeperate && ( ( flags & (int)ShadowRenderableFlags.IncludeLightCap ) ) > 0 )
                            {
                                // must always fail depth check
                                targetRenderSystem.DepthFunction = CompareFunction.AlwaysFail;

                                Debug.Assert( sr.LightCapRenderable != null, "Shadow renderable is missing a separate light cap renderable!" );

                                RenderSingleShadowVolumeToStencil( sr.LightCapRenderable, zfailAlgo, stencil2sided, tmpLightList, ( i > 0 ) );
                                // reset depth function
                                targetRenderSystem.DepthFunction = CompareFunction.Less;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        ///		Internal utility method for setting stencil state for rendering shadow volumes.
        /// </summary>
        /// <param name="secondPass">Is this the second pass?</param>
        /// <param name="zfail">Should we be using the zfail method?</param>
        /// <param name="twoSided">Should we use a 2-sided stencil?</param>
        protected virtual void SetShadowVolumeStencilState( bool secondPass, bool zfail, bool twoSided )
        {
            // First pass, do front faces if zpass
            // Second pass, do back faces if zpass
            // Invert if zfail
            // this is to ensure we always increment before decrement
            if ( ( secondPass ^ zfail ) )
            {
                targetRenderSystem.CullingMode = twoSided ? CullingMode.None : CullingMode.CounterClockwise;
                targetRenderSystem.SetStencilBufferParams(
                    CompareFunction.AlwaysPass, // always pass stencil check
                    0, // no ref value (no compare)
                    unchecked( (int)0xffffffff ), // no mask
                    StencilOperation.Keep, // stencil test will never fail
                    zfail ? ( twoSided ? StencilOperation.IncrementWrap : StencilOperation.Increment ) : StencilOperation.Keep, // back face depth fail
                    zfail ? StencilOperation.Keep : ( twoSided ? StencilOperation.DecrementWrap : StencilOperation.Decrement ), // back face pass
                    twoSided );
            }
            else
            {
                targetRenderSystem.CullingMode = twoSided ? CullingMode.None : CullingMode.Clockwise;
                targetRenderSystem.SetStencilBufferParams(
                    CompareFunction.AlwaysPass, // always pass stencil check
                    0, // no ref value (no compare)
                    unchecked( (int)0xffffffff ), // no mask
                    StencilOperation.Keep, // stencil test will never fail
                    zfail ? ( twoSided ? StencilOperation.DecrementWrap : StencilOperation.Decrement ) : StencilOperation.Keep, // front face depth fail
                    zfail ? StencilOperation.Keep : ( twoSided ? StencilOperation.IncrementWrap : StencilOperation.Increment ), // front face pass
                    twoSided );
            }
        }

        /// <summary>
        ///		Render a single shadow volume to the stencil buffer.
        /// </summary>
        /// <param name="sr"></param>
        /// <param name="zfail"></param>
        /// <param name="stencil2sided"></param>
        protected void RenderSingleShadowVolumeToStencil( ShadowRenderable sr, bool zfail, bool stencil2sided, LightList manualLightList, bool isSecondPass )
        {
            // Render a shadow volume here
            //  - if we have 2-sided stencil, one render with no culling
            //  - otherwise, 2 renders, one with each culling method and invert the ops
            if ( !isSecondPass )
            {
                SetShadowVolumeStencilState( false, zfail, stencil2sided );
                RenderSingleObject( sr, shadowStencilPass, false, manualLightList );
            }

            if ( !stencil2sided && isSecondPass )
            {
                // Second pass
                SetShadowVolumeStencilState( true, zfail, false );
                RenderSingleObject( sr, shadowStencilPass, false );
            }

            // Do we need to render a debug shadow marker?
            if ( showDebugShadows && ( isSecondPass || stencil2sided ) )
            {
                // reset stencil & colour ops
                targetRenderSystem.SetStencilBufferParams();
                SetPass( shadowDebugPass );
                RenderSingleObject( sr, shadowDebugPass, false, manualLightList );
                targetRenderSystem.SetColorBufferWriteEnabled( false, false, false, false );
            }
        }

        /// <summary>Internal method for setting up the renderstate for a rendering pass.</summary>
        /// <param name="pass">The Pass details to set.</param>
        /// <returns>
        ///		A Pass object that was used instead of the one passed in, can
        ///		happen when rendering shadow passes
        ///	</returns>
        protected virtual Pass SetPass( Pass pass )
        {
            if ( illuminationStage == IlluminationRenderStage.RenderToTexture )
            {
                // Derive a special shadow caster pass from this one
                pass = DeriveShadowCasterPass( pass );
            }
            else if ( illuminationStage == IlluminationRenderStage.RenderModulativePass )
            {
                pass = DeriveShadowReceiverPass( pass );
            }

            bool passSurfaceAndLightParams = true;

            if ( pass.HasVertexProgram )
            {
                targetRenderSystem.BindGpuProgram( pass.VertexProgram.BindingDelegate );
                // bind parameters later since they can be per-object
                lastUsedVertexProgram = true;
                // does the vertex program want surface and light params passed to rendersystem?
                passSurfaceAndLightParams = pass.VertexProgram.PassSurfaceAndLightStates;
            }
            else
            {
                // Unbind program?
                if ( lastUsedVertexProgram )
                {
                    targetRenderSystem.UnbindGpuProgram( GpuProgramType.Vertex );
                    lastUsedVertexProgram = false;
                }
                // Set fixed-function vertex parameters
            }

            if ( passSurfaceAndLightParams )
            {
                // Set surface reflectance properties, only valid if lighting is enabled
                if ( pass.LightingEnabled )
                {
                    targetRenderSystem.SetSurfaceParams(
                        pass.Ambient,
                        pass.Diffuse,
                        pass.Specular,
                        pass.Emissive,
                        pass.Shininess );
                }

                // Dynamic lighting enabled?
                targetRenderSystem.LightingEnabled = pass.LightingEnabled;
            }

            // Using a fragment program?
            if ( pass.HasFragmentProgram )
            {
                targetRenderSystem.BindGpuProgram( pass.FragmentProgram.BindingDelegate );
                // bind parameters later since they can be per-object
                lastUsedFragmentProgram = true;
            }
            else
            {
                // Unbind program?
                if ( lastUsedFragmentProgram )
                {
                    targetRenderSystem.UnbindGpuProgram( GpuProgramType.Fragment );
                    lastUsedFragmentProgram = false;
                }

                // Set fixed-function fragment settings

                // Fog (assumes we want pixel fog which is the usual)
                // New fog params can either be from scene or from material
                ColorEx newFogColor;
                FogMode newFogMode;
                float newFogDensity, newFogStart, newFogEnd;

                // does the pass want to override the fog mode?
                if ( pass.FogOverride )
                {
                    // New fog params from material
                    newFogMode = pass.FogMode;
                    newFogColor = pass.FogColor;
                    newFogDensity = pass.FogDensity;
                    newFogStart = pass.FogStart;
                    newFogEnd = pass.FogEnd;
                }
                else
                {
                    // New fog params from scene
                    newFogMode = fogMode;
                    newFogColor = fogColor;
                    newFogDensity = fogDensity;
                    newFogStart = fogStart;
                    newFogEnd = fogEnd;
                }

                // set fog using the render system
                targetRenderSystem.SetFog( newFogMode, newFogColor, newFogDensity, newFogStart, newFogEnd );
            }

            // The rest of the settings are the same no matter whether we use programs or not

            // Set scene blending
            targetRenderSystem.SetSceneBlending( pass.SourceBlendFactor, pass.DestBlendFactor );

            // set all required texture units for this pass, and disable ones not being used
            for ( int i = 0; i < targetRenderSystem.Caps.TextureUnitCount; i++ )
            {
                if ( i < pass.NumTextureUnitStages )
                {
                    TextureUnitState texUnit = pass.GetTextureUnitState( i );
                    targetRenderSystem.SetTextureUnit( i, texUnit );
                }
                else
                {
                    // disable this unit
                    targetRenderSystem.DisableTextureUnit( i );
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
            targetRenderSystem.SetColorBufferWriteEnabled( colWrite, colWrite, colWrite, colWrite );

            // Culling Mode
            targetRenderSystem.CullingMode = pass.CullMode;

            // Shading mode
            targetRenderSystem.ShadingMode = pass.ShadingMode;

            return pass;
        }

        /// <summary>
        ///		Utility method for creating the planes of a skybox.
        /// </summary>
        /// <param name="plane"></param>
        /// <param name="distance"></param>
        /// <param name="orientation"></param>
        /// <returns></returns>
        protected Mesh CreateSkyboxPlane( BoxPlane plane, float distance, Quaternion orientation )
        {
            Plane p = new Plane();
            string meshName = "SkyboxPlane_";
            Vector3 up = Vector3.Zero;

            // set the distance of the plane
            p.D = distance;

            switch ( plane )
            {
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
            Mesh planeModel = modelMgr.GetByName( meshName );

            // trash it if it already exists
            if ( planeModel != null )
                modelMgr.Unload( planeModel );

            float planeSize = distance * 2;

            // create and return the plane mesh
            return modelMgr.CreatePlane( meshName, p, planeSize, planeSize, 1, 1, false, 1, 1, 1, up );
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
        protected Mesh CreateSkyDomePlane( BoxPlane plane, float curvature, float tiling, float distance, Quaternion orientation )
        {
            Plane p = new Plane();
            Vector3 up = Vector3.Zero;
            string meshName = "SkyDomePlane_";

            // set up plane equation
            p.D = distance;

            switch ( plane )
            {
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
            Mesh planeMesh = meshManager.GetByName( meshName );

            // destroy existing
            if ( planeMesh != null )
            {
                meshManager.Unload( planeMesh );
                planeMesh.Dispose();
            }

            // create new
            float planeSize = distance * 2;
            int segments = 16;
            planeMesh =
                meshManager.CreateCurvedIllusionPlane(
                meshName, p, planeSize, planeSize, curvature, segments, segments,
                false, 1, tiling, tiling, up, orientation,
                BufferUsage.DynamicWriteOnly, BufferUsage.StaticWriteOnly, true, true );

            return planeMesh;
        }

        /// <summary>
        ///		Protected method used by RenderVisibleObjects to deal with renderables
        ///		which override the camera's own view / projection materices.
        /// </summary>
        /// <param name="renderable"></param>
        protected void UseRenderableViewProjection( IRenderable renderable )
        {
            bool useIdentityView = renderable.UseIdentityView;
            bool useIdentityProj = renderable.UseIdentityProjection;

            // View
            if ( useIdentityView && ( hasCameraChanged || !lastViewWasIdentity ) )
            {
                // using identity view now, so change it
                targetRenderSystem.ViewMatrix = Matrix4.Identity;
                lastViewWasIdentity = true;
            }
            else if ( !useIdentityView && ( hasCameraChanged || lastViewWasIdentity ) )
            {
                targetRenderSystem.ViewMatrix = camInProgress.ViewMatrix;
                lastViewWasIdentity = false;
            }

            // Projection
            if ( useIdentityProj && ( hasCameraChanged || !lastProjectionWasIdentity ) )
            {
                // using identity view now, so change it
                targetRenderSystem.ProjectionMatrix = Matrix4.Identity;
                lastProjectionWasIdentity = true;
            }
            else if ( !useIdentityProj && ( hasCameraChanged || lastProjectionWasIdentity ) )
            {
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
        protected bool OnRenderQueueStarted( RenderQueueGroupID group )
        {
            if ( QueueStarted != null )
            {
                return QueueStarted( group );
            }

            return false;
        }

        /// <summary>
        ///		Used to first the QueueEnded event.  
        /// </summary>
        /// <param name="group"></param>
        /// <returns>True if the queue should be repeated.</returns>
        protected bool OnRenderQueueEnded( RenderQueueGroupID group )
        {
            if ( QueueEnded != null )
            {
                return QueueEnded( group );
            }

            return false;
        }

        #endregion Protected Methods

        #region Public Methods

        public void RekeySceneNode( string oldName, SceneNode node )
        {
            if ( this.sceneNodeList[oldName] == node )
            {
                this.sceneNodeList.Remove( oldName );
                this.sceneNodeList.Add( node );
            }
        }


        /// <summary>
        ///		Creates a <see cref="AxisAlignedBoxRegionSceneQuery"/> for this scene manager. 
        /// </summary>
        /// <remarks>
        ///		This method creates a new instance of a query object for this scene manager, 
        ///		for querying for objects within a AxisAlignedBox region.
        /// </remarks>
        /// <returns>A specialized implementation of AxisAlignedBoxRegionSceneQuery for this scene manager.</returns>
        public AxisAlignedBoxRegionSceneQuery CreateAABBRegionQuery()
        {
            return CreateAABBRegionQuery( new AxisAlignedBox(), 0xffffffff );
        }

        /// <summary>
        ///		Creates a <see cref="AxisAlignedBoxRegionSceneQuery"/> for this scene manager. 
        /// </summary>
        /// <remarks>
        ///		This method creates a new instance of a query object for this scene manager, 
        ///		for querying for objects within a AxisAlignedBox region.
        /// </remarks>
        /// <param name="box">AxisAlignedBox to use for the region query.</param>
        /// <returns>A specialized implementation of AxisAlignedBoxRegionSceneQuery for this scene manager.</returns>
        public AxisAlignedBoxRegionSceneQuery CreateAABBRegionQuery( AxisAlignedBox box )
        {
            return CreateAABBRegionQuery( box, 0xffffffff );
        }

        /// <summary>
        ///		Creates a <see cref="AxisAlignedBoxRegionSceneQuery"/> for this scene manager. 
        /// </summary>
        /// <remarks>
        ///		This method creates a new instance of a query object for this scene manager, 
        ///		for querying for objects within a AxisAlignedBox region.
        /// </remarks>
        /// <param name="box">AxisAlignedBox to use for the region query.</param>
        /// <param name="mask">Custom user defined flags to use for the query.</param>
        /// <returns>A specialized implementation of AxisAlignedBoxRegionSceneQuery for this scene manager.</returns>
        public virtual AxisAlignedBoxRegionSceneQuery CreateAABBRegionQuery( AxisAlignedBox box, ulong mask )
        {
            DefaultAxisAlignedBoxRegionSceneQuery query = new DefaultAxisAlignedBoxRegionSceneQuery( this );
            query.Box = box;
            query.QueryMask = mask;

            return query;
        }

        /// <summary>
        ///    Creates a query to return objects found along the ray.
        /// </summary>
        /// <returns>A specialized implementation of RaySceneQuery for this scene manager.</returns>
        public RaySceneQuery CreateRayQuery()
        {
            return CreateRayQuery( new Ray(), 0xffffffff );
        }

        /// <summary>
        ///    Creates a query to return objects found along the ray.
        /// </summary>
        /// <param name="ray">Ray to use for the intersection query.</param>
        /// <returns>A specialized implementation of RaySceneQuery for this scene manager.</returns>
        public RaySceneQuery CreateRayQuery( Ray ray )
        {
            return CreateRayQuery( ray, 0xffffffff );
        }

        /// <summary>
        ///    Creates a query to return objects found along the ray. 
        /// </summary>
        /// <param name="ray">Ray to use for the intersection query.</param>
        /// <returns>A specialized implementation of RaySceneQuery for this scene manager.</returns>
        public virtual RaySceneQuery CreateRayQuery( Ray ray, ulong mask )
        {
            DefaultRaySceneQuery query = new DefaultRaySceneQuery( this );
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
        public SphereRegionSceneQuery CreateSphereRegionQuery()
        {
            return CreateSphereRegionQuery( new Sphere(), 0xffffffff );
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
        public SphereRegionSceneQuery CreateSphereRegionQuery( Sphere sphere )
        {
            return CreateSphereRegionQuery( sphere, 0xffffffff );
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
        public virtual SphereRegionSceneQuery CreateSphereRegionQuery( Sphere sphere, ulong mask )
        {
            DefaultSphereRegionSceneQuery query = new DefaultSphereRegionSceneQuery( this );
            query.Sphere = sphere;
            query.QueryMask = mask;

            return query;
        }

        /// <summary>
        ///		Creates a <see cref="PlaneBoundedVolumeListSceneQuery"/> for this scene manager. 
        /// </summary>
        /// <remarks>
        ///		This method creates a new instance of a query object for this scene manager, 
        ///		for querying for objects within a PlaneBoundedVolumes region.
        /// </remarks>
        /// <returns>A specialized implementation of PlaneBoundedVolumeListSceneQuery for this scene manager.</returns>
        public PlaneBoundedVolumeListSceneQuery CreatePlaneBoundedVolumeQuery()
        {
            return CreatePlaneBoundedVolumeQuery( new PlaneBoundedVolumeList(), 0xffffffff );
        }

        /// <summary>
        ///		Creates a <see cref="PlaneBoundedVolumeListSceneQuery"/> for this scene manager. 
        /// </summary>
        /// <remarks>
        ///		This method creates a new instance of a query object for this scene manager, 
        ///		for querying for objects within a PlaneBoundedVolumes region.
        /// </remarks>
        /// <param name="volumes">PlaneBoundedVolumeList to use for the region query.</param>
        /// <returns>A specialized implementation of PlaneBoundedVolumeListSceneQuery for this scene manager.</returns>
        public PlaneBoundedVolumeListSceneQuery CreatePlaneBoundedVolumeQuery( PlaneBoundedVolumeList volumes )
        {
            return CreatePlaneBoundedVolumeQuery( volumes, 0xffffffff );
        }

        /// <summary>
        ///		Creates a <see cref="PlaneBoundedVolumeListSceneQuery"/> for this scene manager. 
        /// </summary>
        /// <remarks>
        ///		This method creates a new instance of a query object for this scene manager, 
        ///		for querying for objects within a PlaneBoundedVolumes region.
        /// </remarks>
        /// <param name="volumes">PlaneBoundedVolumeList to use for the region query.</param>
        /// <param name="mask">Custom user defined flags to use for the query.</param>
        /// <returns>A specialized implementation of PlaneBoundedVolumeListSceneQuery for this scene manager.</returns>
        public virtual PlaneBoundedVolumeListSceneQuery CreatePlaneBoundedVolumeQuery( PlaneBoundedVolumeList volumes, ulong mask )
        {
            DefaultPlaneBoundedVolumeListSceneQuery query = new DefaultPlaneBoundedVolumeListSceneQuery( this );
            query.Volumes = volumes;
            query.QueryMask = mask;

            return query;
        }

        /// <summary>
        ///    Creates an IntersectionSceneQuery for this scene manager.
        /// </summary>
        /// <remarks>
        ///		This method creates a new instance of a query object for locating
        ///		intersecting objects. See SceneQuery and IntersectionSceneQuery
        ///		for full details.
        /// </remarks>
        /// <returns>A specialized implementation of IntersectionSceneQuery for this scene manager.</returns>
        public IntersectionSceneQuery CreateIntersectionQuery()
        {
            return CreateIntersectionQuery( 0xffffffff );
        }

        /// <summary>
        ///    Creates an IntersectionSceneQuery for this scene manager.
        /// </summary>
        /// <remarks>
        ///		This method creates a new instance of a query object for locating
        ///		intersecting objects. See SceneQuery and IntersectionSceneQuery
        ///		for full details.
        /// </remarks>
        /// <param name="mask">The query mask to apply to this query; can be used to filter out certain objects.</param>
        /// <returns>A specialized implementation of IntersectionSceneQuery for this scene manager.</returns>
        public virtual IntersectionSceneQuery CreateIntersectionQuery( ulong mask )
        {
            DefaultIntersectionSceneQuery query = new DefaultIntersectionSceneQuery( this );
            query.QueryMask = mask;
            return query;
        }

        /// <summary>
        ///		Removes all cameras from the scene.
        /// </summary>
        public virtual void RemoveAllCameras()
        {
            // notify the render system of each camera being removed
            for ( int i = 0; i < cameraList.Count; i++ )
            {
                targetRenderSystem.NotifyCameraRemoved( cameraList[i] );
            }

            // clear the list
            cameraList.Clear();
        }

        /// <summary>
        ///		Removes all lights from the scene.
        /// </summary>
        public virtual void RemoveAllLights()
        {
            // clear the list
            lightList.Clear();
        }

        /// <summary>
        ///		Removes all entities from the scene.
        /// </summary>
        public virtual void RemoveAllEntities()
        {
            // clear the list
            entityList.Clear();
        }

        /// <summary>
        ///		Removes all billboardsets from the scene.
        /// </summary>
        public virtual void RemoveAllBillboardSets()
        {
            // clear the list
            billboardSetList.Clear();
        }

        /// <summary>
        ///		Removes the specified camera from the scene.
        /// </summary>
        /// <remarks>
        ///		This method removes a previously added camera from the scene.
        /// </remarks>
        /// <param name="camera">Reference to the camera to remove.</param>
        public virtual void RemoveCamera( Camera camera )
        {
            cameraList.Remove( camera );

            // notify all render targets
            targetRenderSystem.NotifyCameraRemoved( camera );
        }

        /// <summary>
        ///		Removes a camera from the scene with the specified name.
        /// </summary>
        /// <remarks>
        ///		This method removes a previously added camera from the scene.
        /// </remarks>
        /// <param name="name">Name of the camera to remove.</param>
        public virtual void RemoveCamera( string name )
        {
            Debug.Assert( cameraList.ContainsKey( name ), string.Format( "Camera '{0}' does not exist in the scene.", name ) );

            RemoveCamera( cameraList[name] );
        }

        /// <summary>
        ///		Removes the specified light from the scene.
        /// </summary>
        /// <remarks>
        ///		This method removes a previously added light from the scene.
        /// </remarks>
        /// <param name="camera">Reference to the light to remove.</param>
        public virtual void RemoveLight( Light light )
        {
            lightList.Remove( light );
        }

        /// <summary>
        ///		Removes a light from the scene with the specified name.
        /// </summary>
        /// <remarks>
        ///		This method removes a previously added light from the scene.
        /// </remarks>
        /// <param name="name">Name of the light to remove.</param>
        public virtual void RemoveLight( string name )
        {
            Debug.Assert( lightList.ContainsKey( name ), string.Format( "Light '{0}' does not exist in the scene.", name ) );

            RemoveLight( lightList[name] );
        }

        /// <summary>
        ///		Removes the specified BillboardSet from the scene.
        /// </summary>
        /// <remarks>
        ///		This method removes a previously added BillboardSet from the scene.
        /// </remarks>
        /// <param name="camera">Reference to the BillboardSet to remove.</param>
        public virtual void RemoveBillboardSet( BillboardSet billboardSet )
        {
            billboardSetList.Remove( billboardSet );
        }

        /// <summary>
        ///		Removes a BillboardSet from the scene with the specified name.
        /// </summary>
        /// <remarks>
        ///		This method removes a previously added BillboardSet from the scene.
        /// </remarks>
        /// <param name="name">Name of the BillboardSet to remove.</param>
        public virtual void RemoveBillboardSet( string name )
        {
            Debug.Assert( billboardSetList.ContainsKey( name ), string.Format( "BillboardSet '{0}' does not exist in the scene.", name ) );

            RemoveBillboardSet( billboardSetList[name] );
        }

        /// <summary>
        ///    Removes the specified entity from the scene.
        /// </summary>
        /// <param name="entity">Entity to remove from the scene.</param>
        public virtual void RemoveEntity( Entity entity )
        {
            entityList.Remove( entity );
        }

        /// <summary>
        ///    Removes the entity with the specified name from the scene.
        /// </summary>
        /// <param name="entity">Entity to remove from the scene.</param>
        public virtual void RemoveEntity( string name )
        {
            Entity entity = entityList[name];
            if ( entity != null )
            {
                entityList.Remove( entity );
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
        public void SetFog( FogMode mode, ColorEx color, float density, float linearStart, float linearEnd )
        {
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
        public void SetFog( FogMode mode, ColorEx color, float density )
        {
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
        public void SetSkyBox( bool enable, string materialName, float distance )
        {
            SetSkyBox( enable, materialName, distance, true, Quaternion.Identity );
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
        public void SetSkyBox( bool enable, string materialName, float distance, bool drawFirst, Quaternion orientation )
        {
            // enable the skybox?
            isSkyBoxEnabled = enable;

            if ( enable )
            {
                Material m = MaterialManager.Instance.GetByName( materialName );

                if ( m == null )
                    throw new AxiomException( string.Format( "Could not find skybox material '{0}'", materialName ) );

                // Make sure the material doesn't update the depth buffer
                m.DepthWrite = false;
                // Ensure loaded
                m.Load();

                // ensure texture clamping to reduce fuzzy edges when using filtering
                m.GetTechnique( 0 ).GetPass( 0 ).GetTextureUnitState( 0 ).TextureAddressing = TextureAddressing.Clamp;

                isSkyBoxDrawnFirst = drawFirst;

                if ( skyBoxNode == null )
                {
                    skyBoxNode = CreateSceneNode( "SkyBoxNode" );
                }
                else
                {
                    skyBoxNode.DetachAllObjects();
                }

                // need to create 6 plane entities for each side of the skybox
                for ( int i = 0; i < 6; i++ )
                {
                    Mesh planeModel = CreateSkyboxPlane( (BoxPlane)i, distance, orientation );
                    string entityName = "SkyBoxPlane" + i;

                    if ( skyBoxEntities[i] != null )
                    {
                        RemoveEntity( skyBoxEntities[i] );
                    }

                    // create an entity for this plane
                    skyBoxEntities[i] = CreateEntity( entityName, planeModel.Name );

                    // skyboxes need not cast shadows
                    skyBoxEntities[i].CastShadows = false;

                    // Have to create 6 materials, one for each frame
                    // Used to use combined material but now we're using queue we can't split to change frame
                    // This doesn't use much memory because textures aren't duplicated
                    Material boxMaterial = MaterialManager.Instance.GetByName( entityName );

                    if ( boxMaterial == null )
                    {
                        // Create new by clone
                        boxMaterial = m.Clone( entityName );
                        boxMaterial.Load();
                    }
                    else
                    {
                        // Copy over existing
                        //must not copy over the name, otherwise the sky box entity will be set to use the origional m material instead of the copy for each entity which each uses a different frame
                        m.CopyTo( boxMaterial, false );
                        boxMaterial.Load();
                    }

                    // set the current frame
                    boxMaterial.GetTechnique( 0 ).GetPass( 0 ).GetTextureUnitState( 0 ).CurrentFrame = i;

                    skyBoxEntities[i].MaterialName = boxMaterial.Name;

                    // Attach to node
                    skyBoxNode.AttachObject( skyBoxEntities[i] );
                } // for each plane
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="isEnabled"></param>
        /// <param name="materialName"></param>
        /// <param name="curvature"></param>
        /// <param name="tiling"></param>
        public void SetSkyDome( bool isEnabled, string materialName, float curvature, float tiling )
        {
            SetSkyDome( isEnabled, materialName, curvature, tiling, 4000, true, Quaternion.Identity );
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
        public void SetSkyDome( bool isEnabled, string materialName, float curvature, float tiling, float distance, bool drawFirst, Quaternion orientation )
        {
            isSkyDomeEnabled = isEnabled;
            if ( isEnabled )
            {
                Material material = MaterialManager.Instance.GetByName( materialName );

                if ( material == null )
                {
                    throw new AxiomException( string.Format( "Could not find skydome material '{0}'", materialName ) );
                }

                // make sure the material doesn't update the depth buffer
                material.DepthWrite = false;
                // ensure loading
                material.Load();

                isSkyDomeDrawnFirst = drawFirst;

                // create node
                if ( skyDomeNode == null )
                {
                    skyDomeNode = CreateSceneNode( "SkyDomeNode" );
                }
                else
                {
                    skyDomeNode.DetachAllObjects();
                }

                // set up the dome (5 planes)
                for ( int i = 0; i < 5; ++i )
                {
                    Mesh planeMesh = CreateSkyDomePlane( (BoxPlane)i, curvature, tiling, distance, orientation );
                    string entityName = String.Format( "SkyDomePlame{0}", i );

                    // create entity
                    if ( skyDomeEntities[i] != null )
                    {
                        RemoveEntity( skyDomeEntities[i] );
                    }

                    skyDomeEntities[i] = CreateEntity( entityName, planeMesh.Name );
                    skyDomeEntities[i].MaterialName = material.Name;
                    // Sky entities need not cast shadows
                    skyDomeEntities[i].CastShadows = false;

                    // attach to node
                    skyDomeNode.AttachObject( skyDomeEntities[i] );
                } // for each plane
            }
        }

        /// <summary>
        ///		Sets the size and count of textures used in texture-based shadows. 
        /// </summary>
        /// <remarks>
        ///		See ShadowTextureSize and ShadowTextureCount for details, this
        ///		method just allows you to change both at once, which can save on 
        ///		reallocation if the textures have already been created.
        /// </remarks>
        /// <param name="size"></param>
        /// <param name="count"></param>
        public virtual void SetShadowTextureSettings( ushort size, ushort count )
        {
            if ( shadowTextures.Count > 0 &&
                ( count != shadowTextureCount ||
                size != shadowTextureSize ) )
            {
                // recreate
                CreateShadowTextures( size, count );
            }
            shadowTextureCount = count;
            shadowTextureSize = size;
        }

        #endregion Public Methods

        #region Properties

        /// <summary>
        /// Gets a list of the valid cameras for this scene for easy lookup.
        /// </summary>
        public CameraList Cameras
        {
            get
            {
                return cameraList;
            }
        }

        /// <summary>
        /// Gets a list of lights in the scene for easy lookup.
        /// </summary>
        public LightList Lights
        {
            get
            {
                return lightList;
            }
        }

        /// <summary>
        /// Gets a list of entities in the scene for easy lookup.
        /// </summary>
        public EntityList Entities
        {
            get
            {
                return entityList;
            }
        }

        /// <summary>
        /// Gets a list of scene nodes (includes all in the scene graph).
        /// </summary>
        public SceneNodeCollection SceneNodes
        {
            get
            {
                return sceneNodeList;
            }
        }

        /// <summary>
        /// Gets a list of billboard sets for easy lookup.
        /// </summary>
        public BillboardSetCollection BillboardSets
        {
            get
            {
                return billboardSetList;
            }
        }

        /// <summary>
        /// Gets a list of animations for easy lookup.
        /// </summary>
        public AnimationCollection Animations
        {
            get
            {
                return animationList;
            }
        }

        /// <summary>
        /// Gets a list of animation states for easy lookup.
        /// </summary>
        public AnimationStateCollection AnimationStates
        {
            get
            {
                return animationStateList;
            }
        }

        /// <summary>
        /// Gets/Sets the target render system that this scene manager should be using.
        /// </summary>
        public RenderSystem TargetRenderSystem
        {
            get
            {
                return targetRenderSystem;
            }
            set
            {
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
        public SceneNode RootSceneNode
        {
            get
            {
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
        public ColorEx AmbientLight
        {
            get
            {
                return ambientColor;
            }
            set
            {
                if ( value == null )
                    throw new ArgumentException( "Cannot set the scene ambient light color to null" );
                //it will cause the GpuProgramParameters.SetConstant() color overload to throw a null reference exception otherwise
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
        public Hashtable Options
        {
            get
            {
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
        public ColorEx ShadowColor
        {
            get
            {
                return shadowColor;
            }
            set
            {
                shadowColor = value;

                if ( shadowModulativePass == null && shadowCasterPlainBlackPass == null )
                {
                    InitShadowVolumeMaterials();
                }

                shadowModulativePass.GetTextureUnitState( 0 ).SetColorOperationEx(
                    LayerBlendOperationEx.Modulate,
                    LayerBlendSource.Manual,
                    LayerBlendSource.Current,
                    value );
            }
        }

        /// <summary>
        ///		Sets the distance a shadow volume is extruded for a directional light.
        /// </summary>
        /// <remarks>
        ///		Although directional lights are essentially infinite, there are many
        ///		reasons to limit the shadow extrusion distance to a finite number, 
        ///		not least of which is compatibility with older cards (which do not
        ///		support infinite positions), and shadow caster elimination.
        ///		<p/>
        ///		The default value is 10,000 world units. This does not apply to
        ///		point lights or spotlights, since they extrude up to their 
        ///		attenuation range.
        /// </remarks>
        public float ShadowDirectionalLightExtrusionDistance
        {
            get
            {
                return shadowDirLightExtrudeDist;
            }
            set
            {
                shadowDirLightExtrudeDist = value;
            }
        }

        /// <summary>
        ///		Sets the maximum distance away from the camera that shadows will be visible.
        /// </summary>
        /// <remarks>
        ///		Shadow techniques can be expensive, therefore it is a good idea
        ///		to limit them to being rendered close to the camera if possible,
        ///		and to skip the expense of rendering shadows for distance objects.
        ///		This method allows you to set the distance at which shadows will no
        ///		longer be rendered.
        ///		Note:
        ///		Each shadow technique can interpret this subtely differently.
        ///		For example, one technique may use this to eliminate casters,
        ///		another might use it to attenuate the shadows themselves.
        ///		You should tweak this value to suit your chosen shadow technique
        ///		and scene setup.
        /// </remarks>
        public float ShadowFarDistance
        {
            get
            {
                return shadowFarDistance;
            }
            set
            {
                shadowFarDistance = value;
                shadowFarDistanceSquared = shadowFarDistance * shadowFarDistance;
            }
        }

        /// <summary>
        ///		Sets the maximum size of the index buffer used to render shadow primitives.
        /// </summary>
        /// <remarks>
        ///		This property allows you to tweak the size of the index buffer used
        ///		to render shadow primitives (including stencil shadow volumes). The
        ///		default size is 51,200 entries, which is 100k of GPU memory, or
        ///		enough to render approximately 17,000 triangles. You can reduce this
        ///		as long as you do not have any models / world geometry chunks which 
        ///		could require more than the amount you set.
        ///		<p/>
        ///		The maximum number of triangles required to render a single shadow 
        ///		volume (including light and dark caps when needed) will be 3x the 
        ///		number of edges on the light silhouette, plus the number of 
        ///		light-facing triangles.	On average, half the 
        ///		triangles will be facing toward the light, but the number of 
        ///		triangles in the silhouette entirely depends on the mesh - 
        ///		angular meshes will have a higher silhouette tris/mesh tris
        ///		ratio than a smooth mesh. You can estimate the requirements for
        ///		your particular mesh by rendering it alone in a scene with shadows
        ///		enabled and a single light - rotate it or the light and make a note
        ///		of how high the triangle count goes (remembering to subtract the 
        ///		mesh triangle count)
        /// </remarks>
        public int ShadowIndexBufferSize
        {
            get
            {
                return shadowIndexBufferSize;
            }
            set
            {
                if ( shadowIndexBuffer != null || value != shadowIndexBufferSize )
                {

                    // create an shadow index buffer
                    shadowIndexBuffer =
                        HardwareBufferManager.Instance.CreateIndexBuffer(
                        IndexType.Size16, shadowIndexBufferSize,
                        BufferUsage.DynamicWriteOnly, false );
                }

                shadowIndexBufferSize = value;
            }
        }

        /// <summary>
        ///		Set the size of the texture used for texture-based shadows.
        /// </summary>
        /// <remarks>
        ///		The larger the shadow texture, the better the detail on 
        ///		texture based shadows, but obviously this takes more memory.
        ///		The default size is 512. Sizes must be a power of 2.
        ///	</remarks>
        public ushort ShadowTextureSize
        {
            get
            {
                return shadowTextureSize;
            }
            set
            {
                // possibly recreate
                CreateShadowTextures( value, shadowTextureCount );
                shadowTextureSize = value;
            }
        }

        /// <summary>
        ///		Set the number of textures allocated for texture-based shadows.
        /// </summary>
        /// <remarks>
        ///		The default number of textures assigned to deal with texture based
        ///		shadows is 1; however this means you can only have one light casting
        ///		shadows at the same time. You can increase this number in order to 
        ///		make this more flexible, but be aware of the texture memory it will use.
        ///	</remarks>
        public ushort ShadowTextureCount
        {
            get
            {
                return shadowTextureCount;
            }
            set
            {
                // possibly recreate
                CreateShadowTextures( shadowTextureSize, value );
                shadowTextureCount = value;
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
        public ShadowTechnique ShadowTechnique
        {
            get
            {
                return shadowTechnique;
            }
            set
            {
                shadowTechnique = value;

                // do initial setup for stencil shadows if needed
                if ( shadowTechnique == ShadowTechnique.StencilAdditive ||
                    shadowTechnique == ShadowTechnique.StencilModulative )
                {

                    // Firstly check that we have a stencil. Otherwise, forget it!
                    if ( !targetRenderSystem.Caps.CheckCap( Capabilities.StencilBuffer ) )
                    {
                        LogManager.Instance.Write( "WARNING: Stencil shadows were requested, but the current hardware does not support them.  Disabling." );

                        shadowTechnique = ShadowTechnique.None;
                    }
                    else if ( shadowIndexBuffer == null )
                    {
                        // create an shadow index buffer
                        shadowIndexBuffer =
                            HardwareBufferManager.Instance.CreateIndexBuffer(
                            IndexType.Size16, shadowIndexBufferSize,
                            BufferUsage.DynamicWriteOnly, false );

                        // tell all the meshes to prepare shadow volumes
                        MeshManager.Instance.PrepareAllMeshesForShadowVolumes = true;
                    }
                }

                // If Additive stencil, we need to split everything by illumination stage
                GetRenderQueue().SplitPassesByLightingType =
                    ( shadowTechnique == ShadowTechnique.StencilAdditive );

                // If any type of shadowing is used, tell render queue to split off non-shadowable materials
                GetRenderQueue().SplitNoShadowPasses =
                    ( shadowTechnique != ShadowTechnique.None );

                // create new textures for texture based shadows
                if ( shadowTechnique == ShadowTechnique.TextureModulative )
                {
                    CreateShadowTextures( shadowTextureSize, shadowTextureCount );
                }
            }
        }

        /// <summary>
        ///		Sets whether we should use an inifinite camera far plane
        ///		when rendering stencil shadows.
        /// </summary>
        /// <remarks>
        ///		Stencil shadow coherency is very reliant on the shadow volume
        ///		not being clipped by the far plane. If this clipping happens, you
        ///		get a kind of 'negative' shadow effect. The best way to achieve
        ///		coherency is to move the far plane of the camera out to infinity,
        ///		thus preventing the far plane from clipping the shadow volumes.
        ///		When combined with vertex program extrusion of the volume to 
        ///		infinity, which	Axiom does when available, this results in very
        ///		robust shadow volumes. For this reason, when you enable stencil 
        ///		shadows, Ogre automatically changes your camera settings to 
        ///		project to infinity if the card supports it. You can disable this
        ///		behavior if you like by setting this property; although you can 
        ///		never enable infinite projection if the card does not support it.
        ///		<p/>
        ///		If you disable infinite projection, or it is not available, 
        ///		you need to be far more careful with your light attenuation /
        ///		directional light extrusion distances to avoid clipping artefacts
        ///		at the far plane.
        ///		<p/>
        ///		Recent cards will generally support infinite far plane projection.
        ///		However, we have found some cases where they do not, especially
        ///		on Direct3D. There is no standard capability we can check to 
        ///		validate this, so we use some heuristics based on experience:
        ///		<UL>
        ///		<LI>OpenGL always seems to support it no matter what the card</LI>
        ///		<LI>Direct3D on non-vertex program capable systems (including 
        ///		vertex program capable cards on Direct3D7) does not
        ///		support it</LI>
        ///		<LI>Direct3D on GeForce3 and GeForce4 Ti does not seem to support
        ///		infinite projection<LI>
        ///		</UL>
        ///		Therefore in the RenderSystem implementation, we may veto the use
        ///		of an infinite far plane based on these heuristics. 
        /// </remarks>
        public bool ShadowUseInfiniteFarPlane
        {
            get
            {
                return shadowUseInfiniteFarPlane;
            }
            set
            {
                shadowUseInfiniteFarPlane = value;
            }
        }

        /// <summary>
        ///		Gets/Sets a value that forces all nodes to render their bounding boxes.
        /// </summary>
        public bool ShowBoundingBoxes
        {
            get
            {
                return showBoundingBoxes;
            }
            set
            {
                showBoundingBoxes = value;
            }
        }

        /// <summary>
        ///		Gets/Sets a flag that indicates whether debug shadow info (i.e. visible volumes)
        ///		will be displayed.
        /// </summary>
        public virtual bool ShowDebugShadows
        {
            get
            {
                return showDebugShadows;
            }
            set
            {
                showDebugShadows = value;
            }
        }

        /// <summary>
        ///		Gets/Sets whether or not to display the nodes themselves in addition to their objects.
        /// </summary>
        /// <remarks>
        ///		What will be displayed is the local axes of the node (for debugging mainly).
        /// </remarks>
        public bool DisplayNodes
        {
            get
            {
                return displayNodes;
            }
            set
            {
                displayNodes = value;
            }
        }

        /// <summary>
        ///		Gets the fog mode that was set during the last call to SetFog.
        /// </summary>
        public FogMode FogMode
        {
            get
            {
                return fogMode;
            }
        }

        /// <summary>
        ///		Gets the fog starting point that was set during the last call to SetFog.
        /// </summary>
        public float FogStart
        {
            get
            {
                return fogStart;
            }
        }

        /// <summary>
        ///		Gets the fog ending point that was set during the last call to SetFog.
        /// </summary>
        public float FogEnd
        {
            get
            {
                return fogEnd;
            }
        }

        /// <summary>
        ///		Gets the fog density that was set during the last call to SetFog.
        /// </summary>
        public float FogDensity
        {
            get
            {
                return fogDensity;
            }
        }

        /// <summary>
        ///		Gets the fog color that was set during the last call to SetFog.
        /// </summary>
        public ColorEx FogColor
        {
            get
            {
                return fogColor;
            }
        }

        /// <summary>
        ///		Returns a pointer to the default Material settings.
        /// </summary>
        ///	<remarks>
        ///		<p>
        ///		Axiom comes configured with a set of defaults for newly created
        ///		materials. If you wish to have a different set of defaults,
        ///		simply call this method and change the returned Material's
        ///		settings. All materials created from then on will be configured
        ///		with the new defaults you have specified.
        ///		</p>
        ///		<p>
        ///		The default settings begin as a single Technique with a single, non-programmable Pass:
        ///     <ul>
        ///     <li>ambient = ColourEx.White</li>
        ///     <li>diffuse = ColourEx.White</li>
        ///     <li>specular = ColourEx.Black</li>
        ///     <li>emmissive = ColourEx.Black</li>
        ///     <li>shininess = 0</li>
        ///     <li>No texture unit settings (& hence no textures)</li>
        ///     <li>SourceBlendFactor = SBF_ONE</li>
        ///     <li>DestBlendFactor = SBF_ZERO (no blend, replace with new colour)</li>
        ///     <li>Depth buffer checking on</li>
        ///     <li>Depth buffer writing on</li>
        ///     <li>Depth buffer comparison function = CMPF_LESS_EQUAL</li>
        ///     <li>Colour buffer writing on for all channels</li>
        ///     <li>Culling mode = CULL_CLOCKWISE</li>
        ///     <li>Ambient lighting = ColourValue(0.5, 0.5, 0.5) (mid-grey)</li>
        ///     <li>Dynamic lighting enabled</li>
        ///     <li>Gourad shading mode</li>
        ///     <li>Bilinear texture filtering</li>
        ///     </ul>
        ///		</p>
        ///	</remarks>
        public Material DefaultMaterialSettings
        {
            get
            {
                return Material.defaultSettings;
            }
        }

        #endregion Properties

        #region Internal Methods

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
        internal void RenderScene( Camera camera, Viewport viewport, bool showOverlays )
        {
            // let the engine know this is the current scene manager
            Root.Instance.SceneManager = this;

            // initialize shadow volume materials
            InitShadowVolumeMaterials();

            // Perform a quick pre-check to see whether we should override far distance
            // When using stencil volumes we have to use infinite far distance
            // to prevent dark caps getting clipped
            if ( ( shadowTechnique == ShadowTechnique.StencilAdditive ||
                shadowTechnique == ShadowTechnique.StencilModulative ) &&
                camera.Far != 0 &&
                targetRenderSystem.Caps.CheckCap( Capabilities.InfiniteFarPlane ) &&
                shadowUseInfiniteFarPlane )
            {

                // infinite far distance
                camera.Far = 0.0f;
            }

            camInProgress = camera;
            hasCameraChanged = true;

            // Update the scene, only do this once per frame
            ulong thisFrameNumber = Root.Instance.CurrentFrameCount;
            if ( thisFrameNumber != lastFrameNumber )
            {
                // Update animations
                ApplySceneAnimations();
                // Update controllers 
                ControllerManager.Instance.UpdateAll();
                lastFrameNumber = thisFrameNumber;
            }

            // Update scene graph for this camera (can happen multiple times per frame)
            UpdateSceneGraph( camera );

            // Auto-track nodes
            for ( int i = 0; i < autoTrackingSceneNodes.Count; i++ )
            {
                autoTrackingSceneNodes[i].AutoTrack();
            }

            // ask the camera to auto track if it has a target
            camera.AutoTrack();

            // Are we using any shadows at all?
            if ( shadowTechnique != ShadowTechnique.None && illuminationStage != IlluminationRenderStage.RenderToTexture )
            {
                // Locate any lights which could be affecting the frustum
                FindLightsAffectingFrustum( camera );

                if ( shadowTechnique == ShadowTechnique.TextureModulative
                    /* || mShadowTechnique == SHADOWTYPE_TEXTURE_SHADOWMAP */)
                {
                    // *******
                    // WARNING
                    // *******
                    // This call will result in re-entrant calls to this method
                    // therefore anything which comes before this is NOT 
                    // guaranteed persistent. Make sure that anything which 
                    // MUST be specific to this camera / target is done 
                    // AFTER THIS POINT
                    PrepareShadowTextures( camera, viewport );
                    // reset the cameras because of the re-entrant call
                    camInProgress = camera;
                    hasCameraChanged = true;
                }
            }

            // Invert vertex winding?
            targetRenderSystem.InvertVertexWinding = camera.IsReflected;

            // Set the viewport
            SetViewport( viewport );

            // set the current camera for use in the auto GPU program params
            autoParamDataSource.Camera = camera;

            // Set autoparams for finite dir light extrusion
            autoParamDataSource.SetShadowDirLightExtrusionDistance( shadowDirLightExtrudeDist );

            // sets the current ambient light color for use in auto GPU program params
            autoParamDataSource.AmbientLight = ambientColor;

            // Tell params about render target
            autoParamDataSource.RenderTarget = viewport.Target;

            // Set camera window clipping planes (if any)
            if ( targetRenderSystem.Caps.CheckCap( Capabilities.UserClipPlanes ) )
            {
                // TODO Add ClipPlanes to RenderSystem.cs
                /*
				if (camera.IsWindowSet)  
				{
					PlaneList planeList = camera.WindowPlanes;
					for (ushort i = 0; i < 4; ++i)
					{
						targetRenderSystem.EnableClipPlane(i, true);
						targetRenderSystem.SetClipPlane(i, planeList[i]);
					}
				}
				else
				{
					for (ushort i = 0; i < 4; ++i)
					{
						targetRenderSystem.EnableClipPlane(i, false);
					}
				}
				*/
            }

            // clear the current render queue
            GetRenderQueue().Clear();

            // Parse the scene and tag visibles
            FindVisibleObjects( camera,
                illuminationStage == IlluminationRenderStage.RenderToTexture ? true : false );

            // Add overlays, if viewport deems it
            if ( viewport.OverlaysEnabled )
            {
                // Queue overlays for rendering
                OverlayManager.Instance.QueueOverlaysForRendering( camera, GetRenderQueue(), viewport );
            }

            // queue overlays and skyboxes for rendering
            QueueSkiesForRendering( camera );

            // begin frame geometry count
            targetRenderSystem.BeginGeometryCount();

            // being a frame of animation
            targetRenderSystem.BeginFrame();

            // use the camera's current scene detail level
            targetRenderSystem.RasterizationMode = camera.SceneDetail;

            // render all visible objects
            RenderVisibleObjects();

            // end the current frame
            targetRenderSystem.EndFrame();

            // Notify camera of the number of rendered faces
            camera.NotifyRenderedFaces( targetRenderSystem.FacesRendered );
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
        protected internal virtual void UpdateSceneGraph( Camera camera )
        {
            // Cascade down the graph updating transforms & world bounds
            // In this implementation, just update from the root
            // Smarter SceneManager subclasses may choose to update only
            // certain scene graph branches based on space partioning info.
            rootSceneNode.Update( true, false );
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
        public virtual void FindVisibleObjects( Camera camera, bool onlyShadowCasters )
        {
            // ask the root node to iterate through and find visible objects in the scene
            rootSceneNode.FindVisibleObjects( camera, GetRenderQueue(), true, displayNodes, onlyShadowCasters );
        }

        /// <summary>
        ///		Internal method for applying animations to scene nodes.
        /// </summary>
        /// <remarks>
        ///		Uses the internally stored AnimationState objects to apply animation to SceneNodes.
        /// </remarks>
        internal virtual void ApplySceneAnimations()
        {
            for ( int i = 0; i < animationStateList.Count; i++ )
            {
                // get the current animation state
                AnimationState animState = animationStateList[i];

                // get this states animation
                Animation anim = animationList[animState.Name];

                // loop through all tracks and reset their nodes initial state
                for ( int j = 0; j < anim.Tracks.Count; j++ )
                {
                    Node node = anim.Tracks[j].AssociatedNode;
                    node.ResetToInitialState();
                }

                // apply the animation
                anim.Apply( animState.Time, animState.Weight, false, 1.0F, animState.FaceDirectionOfMotion );
            }
        }

        /// <summary>
        /// Internal method for creating shadow textures (texture-based shadows).
        /// </summary>
        /// <param name="size"></param>
        /// <param name="count"></param>
        protected internal virtual void CreateShadowTextures( ushort size, ushort count )
        {
            string baseName = "Axiom/ShadowTexture";

            if ( ( shadowTechnique != ShadowTechnique.TextureModulative
                /*&& shadowTechnique != ShadowTechnique.TextureShadowmap */) ||
                shadowTextures.Count > 0 &&
                count == shadowTextureCount &&
                size == shadowTextureSize )
            {
                // no change
                return;
            }

            // destroy existing
            for ( int i = 0; i < shadowTextures.Count; i++ )
            {
                RenderTexture shadowTex = (RenderTexture)shadowTextures[i];

                // remove camera and destroy texture
                RemoveCamera( shadowTex.GetViewport( 0 ).Camera );
                targetRenderSystem.DetachRenderTarget( shadowTex );
            }
            shadowTextures.Clear();

            // Recreate shadow textures
            for ( ushort t = 0; t < count; ++t )
            {
                string targName = string.Format( "{0}{1}", baseName, t );
                string matName = string.Format( "{0}{1}{2}", baseName, "Mat", t );
                string camName = string.Format( "{0}{1}{2}", baseName, "Cam", t );

                RenderTexture shadowTex = null;
                if ( shadowTechnique == ShadowTechnique.TextureModulative )
                {
                    shadowTex = targetRenderSystem.CreateRenderTexture(
                        targName, size, size );
                }
                /*
					else if (mShadowTechnique == SHADOWTYPE_TEXTURE_SHADOWMAP)
					{
					// todo
					}
					*/

                // Create a camera to go with this texture
                Camera cam = CreateCamera( camName );
                cam.AspectRatio = 1.0f;
                // Create a viewport
                Viewport view = shadowTex.AddViewport( cam );
                view.ClearEveryFrame = true;
                // remove overlays
                view.OverlaysEnabled = false;
                // Don't update automatically - we'll do it when required
                shadowTex.IsAutoUpdated = false;
                shadowTextures.Add( shadowTex );

                // Also create corresponding Material used for rendering this shadow
                Material mat = (Material)MaterialManager.Instance.GetByName( matName );
                if ( mat == null )
                {
                    mat = (Material)MaterialManager.Instance.Create( matName );
                }
                else
                {
                    mat.GetTechnique( 0 ).GetPass( 0 ).RemoveAllTextureUnitStates();
                }
                // create texture unit referring to render target texture
                TextureUnitState texUnit =
                    mat.GetTechnique( 0 ).GetPass( 0 ).CreateTextureUnitState( targName );
                // set projective based on camera
                texUnit.SetProjectiveTexturing( true, cam );
                texUnit.TextureAddressing = TextureAddressing.Clamp;
                mat.Touch();

            }
        }

        /// <summary>
        /// Internal method for preparing shadow textures ready for use in a regular render
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="viewPort"></param>
        protected internal virtual void PrepareShadowTextures( Camera camera, Viewport viewPort )
        {
            // Set the illumination stage, prevents recursive calls
            IlluminationRenderStage savedStage = illuminationStage;
            illuminationStage = IlluminationRenderStage.RenderToTexture;

            // Determine far shadow distance
            float shadowDist = shadowFarDistance;
            if ( shadowDist == 0.0f )
            {
                // need a shadow distance, make one up
                shadowDist = camera.Near * 300;
            }
            // set fogging to hide the shadow edge
            float shadowOffset = shadowDist * shadowTextureOffset;
            float shadowEnd = shadowDist + shadowOffset;
            shadowReceiverPass.SetFog( true, FogMode.Linear, ColorEx.White,
                0, shadowEnd * shadowTextureFadeStart, shadowEnd * shadowTextureFadeEnd );

            // Iterate over the lights we've found, max out at the limit of light textures
            for ( int i = 0, sti = 0;
                i < lightsAffectingFrustum.Count && sti < shadowTextures.Count; i++ )
            {
                Light light = lightsAffectingFrustum[i];
                RenderTexture shadowTex = (RenderTexture)shadowTextures[sti];
                // Skip non-shadowing lights
                if ( !light.CastShadows )
                    continue;

                // Directional lights 
                if ( light.Type == LightType.Directional )
                {

                    // set up the shadow texture
                    Camera texCam = shadowTex.GetViewport( 0 ).Camera;
                    // Set ortho projection
                    texCam.ProjectionType = Projection.Orthographic;
                    // set easy FOV and near dist so that texture covers far dist
                    texCam.FOV = 90;
                    texCam.Near = shadowDist;

                    // Set size of projection

                    // Calculate look at position
                    // We want to look at a spot shadowOffset away from near plane
                    // 0.5 is a litle too close for angles
                    Vector3 target = camera.DerivedPosition +
                        ( camera.DerivedDirection * shadowOffset );

                    // Calculate position
                    // We want to be in the -ve direction of the light direction
                    // far enough to project for the dir light extrusion distance
                    Vector3 pos = target +
                        ( light.DerivedDirection * -shadowDirLightExtrudeDist );

                    // Calculate orientation
                    Vector3 dir = ( pos - target ); // backwards since point down -z
                    dir.Normalize();
                    /*
					// Next section (camera oriented shadow map) abandoned
					// Always point in the same direction, if we don't do this then
					// we get 'shadow swimming' as camera rotates
					// As it is, we get swimming on moving but this is less noticeable

					// calculate up vector, we want it aligned with cam direction
					Vector3 up = cam->getDerivedDirection();
					// Check it's not coincident with dir
					if (up.dotProduct(dir) >= 1.0f)
					{
					// Use camera up
					up = cam->getUp();
					}
					*/
                    Vector3 up = Vector3.UnitY;
                    // Check it's not coincident with dir
                    if ( up.DotProduct( dir ) >= 1.0f )
                    {
                        // Use camera up
                        up = Vector3.UnitZ;
                    }
                    // cross twice to rederive, only direction is unaltered
                    Vector3 left = dir.CrossProduct( up );
                    left.Normalize();
                    up = dir.CrossProduct( left );
                    up.Normalize();
                    // Derive quaternion from axes
                    Quaternion q = Quaternion.Zero;
                    q.FromAxes( left, up, dir );
                    texCam.Orientation = q;

                                        // Round local x/y position based on a world-space texel; this helps to reduce
                                        // jittering caused by the projection moving with the camera
                                        // Viewport is 2 * near clip distance across (90 degree fov)
                                        float worldTexelSize = ( texCam.Near * 20 ) / shadowTextureSize;
                                        pos.x -= pos.x % worldTexelSize;
                                        pos.y -= pos.y % worldTexelSize;
                                        pos.z -= pos.z % worldTexelSize;
                                        // Finally set position
                                        texCam.Position = pos;

                                        if ( shadowTechnique == ShadowTechnique.TextureModulative )
                                                shadowTex.GetViewport( 0 ).BackgroundColor = ColorEx.White;

                                        // Update target
                                        shadowTex.Update();

                    ++sti;
                }
                // Spotlight
                else if ( light.Type == LightType.Spotlight )
                {

                    // set up the shadow texture
                    Camera texCam = shadowTex.GetViewport( 0 ).Camera;
                    // Set perspective projection
                    texCam.ProjectionType = Projection.Perspective;
                    // set FOV slightly larger than the spotlight range to ensure coverage
                    texCam.FOV = light.SpotlightOuterAngle * 1.2f;
                    texCam.Position = light.DerivedPosition;
                    texCam.Direction = light.DerivedDirection;
                    // set near clip the same as main camera, since they are likely
                    // to both reflect the nature of the scene
                    texCam.Near = camera.Near;

                    if ( shadowTechnique == ShadowTechnique.TextureModulative )
                        shadowTex.GetViewport( 0 ).BackgroundColor = ColorEx.White;

                    // Update target
                    shadowTex.Update();

                    ++sti;
                }
            }
            // Set the illumination stage, prevents recursive calls
            illuminationStage = savedStage;
        }

        /// <summary>
        ///		Internal method for setting the destination viewport for the next render.
        /// </summary>
        /// <param name="viewport"></param>
        protected virtual void SetViewport( Viewport viewport )
        {
            currentViewport = viewport;
            // Set viewport in render system
            targetRenderSystem.SetViewport( viewport );
        }

        protected void RenderSingleObject( IRenderable renderable, Pass pass, bool doLightIteration )
        {
            RenderSingleObject( renderable, pass, doLightIteration, null );
        }

        /// <summary>
        ///		Internal utility method for rendering a single object.
        /// </summary>
        /// <param name="renderable">The renderable to issue to the pipeline.</param>
        /// <param name="pass">The pass which is being used.</param>
        /// <param name="doLightIteration">If true, this method will issue the renderable to
        /// the pipeline possibly multiple times, if the pass indicates it should be
        /// done once per light.</param>
        /// <param name="manualLightList">Only applicable if 'doLightIteration' is false, this
        /// method allows you to pass in a previously determined set of lights
        /// which will be used for a single render of this object.</param>
        protected virtual void RenderSingleObject( IRenderable renderable, Pass pass,
            bool doLightIteration, LightList manualLightList )
        {
            ushort numMatrices = 0;

            // grab the current scene detail level
            SceneDetailLevel camDetailLevel = camInProgress.SceneDetail;

            // update auto params if this is a programmable pass
            if ( pass.IsProgrammable )
            {
                autoParamDataSource.Renderable = renderable;
                pass.UpdateAutoParamsNoLights( autoParamDataSource );
            }

            // get the world matrices and the count
            renderable.GetWorldTransforms( xform );
            numMatrices = renderable.NumWorldTransforms;

            // set the world matrices in the render system
            if ( numMatrices > 1 )
            {
                targetRenderSystem.SetWorldMatrices( xform, numMatrices );
            }
            else
            {
                targetRenderSystem.WorldMatrix = xform[0];
            }

            // issue view/projection changes (if any)
            UseRenderableViewProjection( renderable );

            // issue texture units that depend on updated view matrix
            // reflective env mapping is one case
            for ( int i = 0; i < pass.NumTextureUnitStages; i++ )
            {
                TextureUnitState texUnit = pass.GetTextureUnitState( i );

                if ( texUnit.HasViewRelativeTexCoordGen )
                {
                    targetRenderSystem.SetTextureUnit( i, texUnit );
                }
            }

            // Normalize normals
            bool thisNormalize = renderable.NormalizeNormals;

            if ( thisNormalize != normalizeNormals )
            {
                targetRenderSystem.NormalizeNormals = thisNormalize;
                normalizeNormals = thisNormalize;
            }

            // Set up the solid / wireframe override
            SceneDetailLevel requestedDetail = renderable.RenderDetail;
            if ( requestedDetail != lastDetailLevel || requestedDetail != camDetailLevel )
            {
                if ( requestedDetail > camDetailLevel )
                {
                    // only downgrade detail; if cam says wireframe we don't go up to solid
                    requestedDetail = camDetailLevel;
                }
                targetRenderSystem.RasterizationMode = requestedDetail;
                lastDetailLevel = requestedDetail;

            }

            // TODO Add ClipPlanes to RenderSystem.cs
            //targetRenderSystem.ClipPlanes = renderable.ClipPlanes;

            // get the renderables render operation
            renderable.GetRenderOperation( op );
            // TODO Add srcRenderable to RenderOperation.cs
            //op.srcRenderable = renderable;

            if ( doLightIteration )
            {
                // Here's where we issue the rendering operation to the render system
                // Note that we may do this once per light, therefore it's in a loop
                // and the light parameters are updated once per traversal through the
                // loop
                LightList rendLightList = renderable.Lights;
                bool iteratePerLight = pass.RunOncePerLight;
                int numIterations = iteratePerLight ? rendLightList.Count : 1;
                LightList lightListToUse = null;

                for ( int i = 0; i < numIterations; i++ )
                {
                    // determine light list to use
                    if ( iteratePerLight )
                    {
                        localLightList.Clear();

                        // check whether we need to filter this one out
                        if ( pass.RunOnlyOncePerLightType && pass.OnlyLightType != rendLightList[i].Type )
                        {
                            // skip this one
                            continue;
                        }

                        localLightList.Add( rendLightList[i] );
                        lightListToUse = localLightList;
                    }
                    else
                    {
                        // use complete light list
                        lightListToUse = rendLightList;
                    }

                                        if ( pass.IsProgrammable )
                    {
                                                // Update any automatic gpu params for lights
                                                // Other bits of information will have to be looked up
                                                autoParamDataSource.SetCurrentLightList( lightListToUse );
                                                pass.UpdateAutoParamsLightsOnly( autoParamDataSource );

                        // note: parameters must be bound after auto params are updated
                        if ( pass.HasVertexProgram )
                        {
                            targetRenderSystem.BindGpuProgramParameters( GpuProgramType.Vertex, pass.VertexProgramParameters );
                        }
                        if ( pass.HasFragmentProgram )
                        {
                            targetRenderSystem.BindGpuProgramParameters( GpuProgramType.Fragment, pass.FragmentProgramParameters );
                        }
                    }

                    // Do we need to update light states? 
                    // Only do this if fixed-function vertex lighting applies
                    if ( pass.LightingEnabled && !pass.HasVertexProgram )
                    {
                        targetRenderSystem.UseLights( lightListToUse, pass.MaxLights );
                    }
                    // issue the render op		
                    targetRenderSystem.Render( op );
                } // iterate per light
            }
            else
            {
                // do we need to update GPU program parameters?
                if ( pass.IsProgrammable )
                {
                    // do we have a manual light list
                    if ( manualLightList != null )
                    {
                        // Update any automatic gpu params for lights
                        // Other bits of information will have to be looked up
                        autoParamDataSource.SetCurrentLightList( manualLightList );
                        pass.UpdateAutoParamsLightsOnly( autoParamDataSource );
                    }

                    // note: parameters must be bound after auto params are updated
                    if ( pass.HasVertexProgram )
                    {
                        targetRenderSystem.BindGpuProgramParameters( GpuProgramType.Vertex, pass.VertexProgramParameters );
                    }

                    if ( pass.HasFragmentProgram )
                    {
                        targetRenderSystem.BindGpuProgramParameters( GpuProgramType.Fragment, pass.FragmentProgramParameters );
                    }
                }

                // Use manual lights if present, and not using vertex programs
                if ( manualLightList != null && pass.LightingEnabled && !pass.HasVertexProgram )
                {
                    targetRenderSystem.UseLights( manualLightList, pass.MaxLights );
                }

                // issue the render op		
                targetRenderSystem.Render( op );
            }
        }

        /// <summary>
        ///		Renders a set of solid objects.
        /// </summary>
        /// <param name="list">List of solid objects.</param>
        protected virtual void RenderSolidObjects( SortedList list, bool doLightIteration,
            LightList manualLightList )
        {
            // ----- SOLIDS LOOP -----
            for ( int i = 0; i < list.Count; i++ )
            {
                RenderableList renderables = (RenderableList)list.GetByIndex( i );

                // bypass if this group is empty
                if ( renderables.Count == 0 )
                {
                    continue;
                }

                Pass pass = (Pass)list.GetKey( i );

                // Give SM a chance to eliminate this pass
                if ( !ValidatePassForRendering( pass ) )
                    continue;

                // For solids, we try to do each pass in turn
                Pass usedPass = SetPass( pass );

                // render each object associated with this rendering pass
                for ( int r = 0; r < renderables.Count; r++ )
                {
                    IRenderable renderable = (IRenderable)renderables[r];

                    // Give SM a chance to eliminate
                    if ( !ValidateRenderableForRendering( pass, renderable ) )
                        continue;

                    // Render a single object, this will set up auto params if required
                    RenderSingleObject( renderable, usedPass, doLightIteration, manualLightList );
                }
            }
        }

        protected void RenderSolidObjects( SortedList list, bool doLightIteration )
        {
            RenderSolidObjects( list, doLightIteration, null );
        }

        /// <summary>
        ///		Renders a set of transparent objects.
        /// </summary>
        /// <param name="list"></param>
        protected virtual void RenderTransparentObjects( ArrayList list, bool doLightIteration,
            LightList manualLightList )
        {
                        // ----- TRANSPARENT LOOP -----
                        // This time we render by Z, not by material
                        // The transparent objects set needs to be ordered first
                        for ( int i = 0; i < list.Count; i++ )
            {
                                RenderablePass rp = (RenderablePass)list[i];

                                // set the pass first
                                SetPass( rp.pass );

                                // render the transparent object
                                RenderSingleObject( rp.renderable, rp.pass, doLightIteration,
                                        manualLightList );
                        }
                }

        protected void RenderTransparentObjects( ArrayList list, bool doLightIteration )
        {
            RenderTransparentObjects( list, doLightIteration, null );
        }

        /// <summary>
        ///		Render a group with the added complexity of additive stencil shadows.
        /// </summary>
        /// <param name="group">Render queue group.</param>
        protected virtual void RenderAdditiveStencilShadowedQueueGroupObjects( RenderQueueGroup group )
        {
            LightList tempLightList = new LightList();

            for ( int i = 0; i < group.NumPriorityGroups; i++ )
            {
                RenderPriorityGroup priorityGroup = group.GetPriorityGroup( i );

                // sort the group first
                priorityGroup.Sort( camInProgress );

                // Clear light list
                tempLightList.Clear();

                // Render all the ambient passes first, no light iteration, no lights
                illuminationStage = IlluminationRenderStage.Ambient;
                RenderSolidObjects( priorityGroup.solidPasses, false, tempLightList );
                // Also render any objects which have receive shadows disabled
                RenderSolidObjects( priorityGroup.solidPassesNoShadow, true );

                // Now iterate per light
                illuminationStage = IlluminationRenderStage.PerLight;

                for ( int li = 0; li < lightsAffectingFrustum.Count; li++ )
                {
                    Light light = lightsAffectingFrustum[li];
                    // Set light state

                    if ( light.CastShadows )
                    {
                        // Clear stencil
                        targetRenderSystem.ClearFrameBuffer( FrameBuffer.Stencil );
                        RenderShadowVolumesToStencil( light, camInProgress );
                        // turn stencil check on
                        targetRenderSystem.StencilCheckEnabled = true;
                        // NB we render where the stencil is equal to zero to render lit areas
                        targetRenderSystem.SetStencilBufferParams( CompareFunction.Equal, 0 );
                    }

                    // render lighting passes for this light
                    if ( tempLightList.Count == 0 )
                    {
                        tempLightList.Add( light );
                    }
                    else
                    {
                        tempLightList[0] = light;
                    }

                    RenderSolidObjects( priorityGroup.solidPassesDiffuseSpecular, false, tempLightList );

                    // Reset stencil params
                    targetRenderSystem.SetStencilBufferParams();
                    targetRenderSystem.StencilCheckEnabled = false;
                    targetRenderSystem.SetDepthBufferParams();

                }// for each light


                // Now render decal passes, no need to set lights as lighting will be disabled
                illuminationStage = IlluminationRenderStage.Decal;
                RenderSolidObjects( priorityGroup.solidPassesDecal, false );


            }// for each priority

            // reset lighting stage
            illuminationStage = IlluminationRenderStage.None;

            // Iterate again
            for ( int i = 0; i < group.NumPriorityGroups; i++ )
            {
                RenderPriorityGroup priorityGroup = group.GetPriorityGroup( i );

                // Do transparents
                RenderTransparentObjects( priorityGroup.transparentPasses, true );

            }// for each priority
        }

        /// <summary>
        ///		Render a group with the added complexity of modulative stencil shadows.
        /// </summary>
        /// <param name="group">Render queue group.</param>
        protected virtual void RenderModulativeStencilShadowedQueueGroupObjects( RenderQueueGroup group )
        {
            /* For each light, we need to render all the solids from each group, 
			then do the modulative shadows, then render the transparents from
			each group.
			Now, this means we are going to reorder things more, but that it required
			if the shadows are to look correct. The overall order is preserved anyway,
			it's just that all the transparents are at the end instead of them being
			interleaved as in the normal rendering loop. 
			*/
            for ( int i = 0; i < group.NumPriorityGroups; i++ )
            {
                RenderPriorityGroup priorityGroup = group.GetPriorityGroup( i );

                // sort the group first
                priorityGroup.Sort( camInProgress );

                // do solids
                RenderSolidObjects( priorityGroup.solidPasses, true );
            }

            // iterate over lights, rendering all volumes to the stencil buffer
            for ( int i = 0; i < lightsAffectingFrustum.Count; i++ )
            {
                Light light = lightsAffectingFrustum[i];

                if ( light.CastShadows )
                {
                    // clear the stencil buffer
                    targetRenderSystem.ClearFrameBuffer( FrameBuffer.Stencil );
                    RenderShadowVolumesToStencil( light, camInProgress );

                    // render full-screen shadow modulator for all lights
                    SetPass( shadowModulativePass );

                    // turn the stencil check on
                    targetRenderSystem.StencilCheckEnabled = true;

                    // we render where the stencil is not equal to zero to render shadows, not lit areas
                    targetRenderSystem.SetStencilBufferParams( CompareFunction.NotEqual, 0 );
                    RenderSingleObject( fullScreenQuad, shadowModulativePass, false );

                    // reset stencil buffer params
                    targetRenderSystem.SetStencilBufferParams();
                    targetRenderSystem.StencilCheckEnabled = false;
                    targetRenderSystem.SetDepthBufferParams();
                }
            }// for each light

            for ( int i = 0; i < group.NumPriorityGroups; i++ )
            {
                RenderPriorityGroup priorityGroup = group.GetPriorityGroup( i );

                // Do non-shadowable solids
                RenderSolidObjects( priorityGroup.solidPassesNoShadow, true );

            }// for each priority

            for ( int i = 0; i < group.NumPriorityGroups; i++ )
            {
                RenderPriorityGroup priorityGroup = group.GetPriorityGroup( i );

                // Do transparents
                RenderTransparentObjects( priorityGroup.transparentPasses, true );

            } // for each priority
        }

        /// <summary>
        ///		Render a group rendering only shadow casters.
        /// </summary>
        /// <param name="group">Render queue group.</param>
        protected virtual void RenderTextureShadowCasterQueueGroupObjects( RenderQueueGroup group )
        {
            // This is like the basic group render, except we skip all transparents
            // and we also render any non-shadowed objects
            // Note that non-shadow casters will have already been eliminated during
            // FindVisibleObjects

            // Override auto param ambient to force vertex programs and fixed function to 
            // use shadow colour
            autoParamDataSource.AmbientLight = shadowColor;
            targetRenderSystem.AmbientLight = shadowColor;

            // Iterate through priorities
            for ( int i = 0; i < group.NumPriorityGroups; i++ )
            {
                RenderPriorityGroup priorityGroup = group.GetPriorityGroup( i );

                // Sort the queue first
                priorityGroup.Sort( camInProgress );

                // Do solids, override light list incase any vertex programs use them
                RenderSolidObjects( priorityGroup.solidPasses, false, nullLightList );
                RenderSolidObjects( priorityGroup.solidPassesNoShadow, false, nullLightList );

                // Do transparents that cast shadows
                RenderTransparentShadowCasterObjects( priorityGroup.transparentPasses, false, nullLightList );
            }// for each priority

            // reset ambient light
            autoParamDataSource.AmbientLight = ambientColor;
            targetRenderSystem.AmbientLight = ambientColor;
        }

        /// <summary>
        ///		Render a group with the added complexity of modulative texture shadows.
        /// </summary>
        /// <param name="group">Render queue group.</param>
        protected virtual void RenderModulativeTextureShadowedQueueGroupObjects( RenderQueueGroup group )
        {
            /* For each light, we need to render all the solids from each group, 
			then do the modulative shadows, then render the transparents from
			each group.
			Now, this means we are going to reorder things more, but that it required
			if the shadows are to look correct. The overall order is preserved anyway,
			it's just that all the transparents are at the end instead of them being
			interleaved as in the normal rendering loop. 
			*/
            // Iterate through priorities
            for ( int i = 0; i < group.NumPriorityGroups; i++ )
            {
                RenderPriorityGroup priorityGroup = group.GetPriorityGroup( i );

                // Sort the queue first
                priorityGroup.Sort( camInProgress );

                // Do solids
                RenderSolidObjects( priorityGroup.solidPasses, true );
                RenderSolidObjects( priorityGroup.solidPassesNoShadow, true );
            }

            // Iterate over lights, render received shadows
            // only perform this if we're in the 'normal' render stage, to avoid
            // doing it during the render to texture
            if ( illuminationStage == IlluminationRenderStage.None )
            {
                illuminationStage = IlluminationRenderStage.RenderModulativePass;

                for ( int i = 0, sti = 0;
                    i < lightsAffectingFrustum.Count && sti < shadowTextures.Count; i++ )
                {
                    Light light = lightsAffectingFrustum[i];

                    if ( !light.CastShadows )
                        continue;

                    RenderTexture shadowTex = (RenderTexture)shadowTextures[sti];
                    // Hook up receiver texture
                    shadowReceiverPass.GetTextureUnitState( 0 ).SetTextureName( shadowTex.Name );
                    // Hook up projection frustum
                    shadowReceiverPass.GetTextureUnitState( 0 ).SetProjectiveTexturing(
                        true, shadowTex.GetViewport( 0 ).Camera );
                    autoParamDataSource.TextureProjector = shadowTex.GetViewport( 0 ).Camera;

                    // if this light is a spotlight, we need to add the spot fader layer
                    if ( light.Type == LightType.Spotlight )
                    {
                        // Add spot fader if not present already
                        if ( shadowReceiverPass.NumTextureUnitStages == 1 )
                        {
                            TextureUnitState tex =
                                shadowReceiverPass.CreateTextureUnitState( "spot_shadow_fade.png" );

                            tex.SetProjectiveTexturing( true, shadowTex.GetViewport( 0 ).Camera );

                            tex.SetColorOperation( LayerBlendOperation.Add );
                            tex.TextureAddressing = TextureAddressing.Clamp;
                        }
                        else
                        {
                            // Just set projector
                            TextureUnitState tex =
                                shadowReceiverPass.GetTextureUnitState( 1 );
                            tex.SetProjectiveTexturing(
                                true, shadowTex.GetViewport( 0 ).Camera );
                        }
                    }
                    else if ( shadowReceiverPass.NumTextureUnitStages > 1 )
                    {
                        // remove spot fader layer
                        shadowReceiverPass.RemoveTextureUnitState( 1 );
                    }

                    shadowReceiverPass.Load();

                    if ( light.CastShadows && group.ShadowsEnabled )
                    {
                        RenderTextureShadowReceiverQueueGroupObjects( group );
                    }

                    ++sti;
                }// for each light

                illuminationStage = IlluminationRenderStage.None;
            }

            // Iterate again
            for ( int i = 0; i < group.NumPriorityGroups; i++ )
            {
                RenderPriorityGroup priorityGroup = group.GetPriorityGroup( i );

                // Do transparents
                RenderTransparentObjects( priorityGroup.transparentPasses, true );
            }// for each priority
        }

        /// <summary>
        ///		Render a group rendering only shadow receivers.
        /// </summary>
        /// <param name="group">Render queue group.</param>
        protected virtual void RenderTextureShadowReceiverQueueGroupObjects( RenderQueueGroup group )
        {
            // Override auto param ambient to force vertex programs to go full-bright
            autoParamDataSource.AmbientLight = ColorEx.White;
            targetRenderSystem.AmbientLight = ColorEx.White;

            // Iterate through priorities
            for ( int i = 0; i < group.NumPriorityGroups; i++ )
            {
                RenderPriorityGroup priorityGroup = group.GetPriorityGroup( i );

                // Do solids, override light list incase any vertex programs use them
                RenderSolidObjects( priorityGroup.solidPasses, false, nullLightList );

                // Don't render transparents or passes which have shadow receipt disabled

            }// for each priority

            // reset ambient
            autoParamDataSource.AmbientLight = ambientColor;
            targetRenderSystem.AmbientLight = ambientColor;
        }

        /// <summary>
        ///		Internal method to validate whether a Pass should be allowed to render.
        /// </summary>
        /// <remarks>
        ///		Called just before a pass is about to be used for rendering a group to
        ///		allow the SceneManager to omit it if required. A return value of false
        ///		skips this pass. 
        /// </remarks>
        protected virtual bool ValidatePassForRendering( Pass pass )
        {
            // Bypass if we're doing a texture shadow render and 
            // this pass is after the first (only 1 pass needed for shadow texture)
            if ( ( illuminationStage == IlluminationRenderStage.RenderToTexture ||
                illuminationStage == IlluminationRenderStage.RenderModulativePass ) &&
                pass.Index > 0 )
            {
                return false;
            }

            return true;
        }

        /// <summary>
        ///		Internal method to validate whether a Renderable should be allowed to render.
        /// </summary>
        /// <remarks>
        ///		Called just before a pass is about to be used for rendering a Renderable to
        ///		allow the SceneManager to omit it if required. A return value of false
        ///		skips it. 
        /// </remarks>
        protected virtual bool ValidateRenderableForRendering( Pass pass, IRenderable renderable )
        {
            // Skip this renderable if we're doing texture shadows, it casts shadows
            // and we're doing the render receivers pass
            if ( shadowTechnique == ShadowTechnique.TextureModulative &&
                illuminationStage == IlluminationRenderStage.RenderModulativePass && renderable.CastsShadows )
            {
                return false;
            }

            return true;
        }

        /// <summary>
        ///		Render the objects in a given queue group.
        /// </summary>
        /// <param name="group">Group containing the objects to render.</param>
        protected virtual void RenderQueueGroupObjects( RenderQueueGroup group )
        {
            // Redirect to alternate versions if stencil shadows in use
            if ( group.ShadowsEnabled && shadowTechnique == ShadowTechnique.StencilAdditive )
            {
                RenderAdditiveStencilShadowedQueueGroupObjects( group );
            }
            else if ( group.ShadowsEnabled && shadowTechnique == ShadowTechnique.StencilModulative )
            {
                RenderModulativeStencilShadowedQueueGroupObjects( group );
            }
            else if ( shadowTechnique == ShadowTechnique.TextureModulative )
            {
                // Modulative texture shadows in use
                if ( illuminationStage == IlluminationRenderStage.RenderToTexture )
                {
                    // Shadow caster pass
                    if ( group.ShadowsEnabled )
                    {
                        RenderTextureShadowCasterQueueGroupObjects( group );
                    }
                }
                else
                {
                    // Ordinary pass
                    RenderModulativeTextureShadowedQueueGroupObjects( group );
                }
            }
            else
            {
                // No shadows, ordinary pass
                RenderBasicQueueGroupObjects( group );
            }
        }

        /// <summary>
        ///		Render a group in the ordinary way
        /// </summary>
        /// <param name="group">Group containing the objects to render.</param>
        protected virtual void RenderBasicQueueGroupObjects( RenderQueueGroup group )
        {
            // Basic render loop
            // Iterate through priorities
            for ( int i = 0; i < group.NumPriorityGroups; i++ )
            {
                RenderPriorityGroup priorityGroup = group.GetPriorityGroup( i );

                // Sort the queue first
                priorityGroup.Sort( camInProgress );

                // Do solids
                RenderSolidObjects( priorityGroup.solidPasses, true );

                // Do transparents
                RenderTransparentObjects( priorityGroup.transparentPasses, true );
            }// for each priority
        }

        /// <summary>
        ///		Render those objects in the transparent pass list which have shadow casting forced on
        /// </summary>
        /// <remarks>
        ///		This function is intended to be used to render the shadows of transparent objects which have
        ///		transparency_casts_shadows set to 'on' in their material
        /// </remarks>
        /// <param name="list"></param>
        /// <param name="doLightIteration"></param>
        /// <param name="manualLightList"></param>
        protected virtual void RenderTransparentShadowCasterObjects( ArrayList list,
            bool doLightIteration, LightList manualLightList )
        {
            // ----- TRANSPARENT LOOP as in RenderTransparentObjects, but changed a bit -----
            for ( int i = 0; i < list.Count; i++ )
            {
                RenderablePass rp = (RenderablePass)list[i];

                // only render this pass if it's being forced to cast shadows
                if ( rp.pass.Parent.Parent.TransparencyCastsShadows )
                {
                    SetPass( rp.pass );
                    RenderSingleObject( rp.renderable, rp.pass, doLightIteration, manualLightList );
                }
            }
        }

        /// <summary>
        ///		Sends visible objects found in <see cref="FindVisibleObjects"/> to the rendering engine.
        /// </summary>
        protected internal virtual void RenderVisibleObjects()
        {
            // loop through each main render group ( which is already sorted)
            for ( int i = 0; i < GetRenderQueue().NumRenderQueueGroups; i++ )
            {
                RenderQueueGroupID queueID = GetRenderQueue().GetRenderQueueGroupID( i );
                RenderQueueGroup queueGroup = GetRenderQueue().GetQueueGroupByIndex( i );

                bool repeatQueue = false;

                // repeat
                do
                {
                    if ( OnRenderQueueStarted( queueID ) )
                    {
                        // someone requested we skip this queue
                        continue;
                    }

                                        if ( queueGroup.NumPriorityGroups > 0 )
                    {
                                                // render objects in all groups
                                                RenderQueueGroupObjects( queueGroup );
                                        }

                                        // true if someone requested that we repeat this queue
                    repeatQueue = OnRenderQueueEnded( queueID );

                } while ( repeatQueue );
            } // for each queue group
        }

        /// <summary>
        ///		Internal method for queueing the sky objects with the params as 
        ///		previously set through SetSkyBox, SetSkyPlane and SetSkyDome.
        /// </summary>
        /// <param name="camera"></param>
        internal virtual void QueueSkiesForRendering( Camera camera )
        {
            // translate the skybox by cam position
            if ( skyPlaneNode != null )
            {
                skyPlaneNode.Position = camera.DerivedPosition;
            }

            if ( skyBoxNode != null )
            {
                skyBoxNode.Position = camera.DerivedPosition;
            }

            if ( skyDomeNode != null )
            {
                skyDomeNode.Position = camera.DerivedPosition;
            }

            RenderQueueGroupID qid;

            // if the skyplane is enabled, queue up the single plane
            if ( isSkyPlaneEnabled )
            {
                qid = isSkyPlaneDrawnFirst ? RenderQueueGroupID.SkiesEarly : RenderQueueGroupID.SkiesLate;
                GetRenderQueue().AddRenderable( skyPlaneEntity.GetSubEntity( 0 ), 1, qid );
            }

            // if the skybox is enabled, queue up all the planes
            if ( isSkyBoxEnabled )
            {
                qid = isSkyBoxDrawnFirst ? RenderQueueGroupID.SkiesEarly : RenderQueueGroupID.SkiesLate;

                for ( int plane = 0; plane < 6; plane++ )
                {
                    GetRenderQueue().AddRenderable( skyBoxEntities[plane].GetSubEntity( 0 ), 1, qid );
                }
            }

            // if the skydome is enabled, queue up all the planes
            if ( isSkyDomeEnabled )
            {
                qid = isSkyDomeDrawnFirst ? RenderQueueGroupID.SkiesEarly : RenderQueueGroupID.SkiesLate;

                for ( int plane = 0; plane < 5; ++plane )
                {
                    GetRenderQueue().AddRenderable( skyDomeEntities[plane].GetSubEntity( 0 ), 1, qid );
                }
            }
        }

        /// <summary>
        ///		Populate a light list with an ordered set of the lights which are closest 
        /// </summary>
        /// <remarks>
        ///		<p>
        ///		Note that since directional lights have no position, they are always considered
        ///		closer than any point lights and as such will always take precedence.
        ///		</p>
        ///		<p>
        ///		Subclasses of the default SceneManager may wish to take into account other issues
        ///		such as possible visibility of the light if that information is included in their
        ///		data structures. This basic scenemanager simply orders by distance, eliminating
        ///		those lights which are out of range.
        ///		</p>
        ///		<p>
        ///		The number of items in the list max exceed the maximum number of lights supported
        ///		by the renderer, but the extraneous ones will never be used. In fact the limit will
        ///		be imposed by Pass::getMaxSimultaneousLights.
        ///		</p>
        /// </remarks>
        /// <param name="position">The position at which to evaluate the list of lights</param>
        /// <param name="radius">The bounding radius to test</param>
        /// <param name="destList">List to be populated with ordered set of lights; will be cleared by this method before population.</param>
        protected internal virtual void PopulateLightList( Vector3 position, float radius, LightList destList )
        {
            // Really basic trawl of the lights, then sort
            // Subclasses could do something smarter
            destList.Clear();
            float squaredRadius = radius * radius;

            // loop through the scene lights an add ones in range
            for ( int i = 0; i < lightList.Count; i++ )
            {
                Light light = lightList[i];

                if ( light.IsVisible )
                {
                    if ( light.Type == LightType.Directional )
                    {
                        // no distance
                        light.tempSquaredDist = 0.0f;
                        destList.Add( light );
                    }
                    else
                    {
                        light.tempSquaredDist = ( light.DerivedPosition - position ).LengthSquared;
                        light.tempSquaredDist -= squaredRadius;
                        // only add in-range lights
                        float range = light.AttenuationRange;
                        if ( light.tempSquaredDist <= ( range * range ) )
                        {
                            destList.Add( light );
                        }
                    }
                } // if
            } // for

                        // Sort Destination light list.
                        // TODO Not needed yet since the current LightList is a sorted list under the hood already
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
        public virtual void SetSkyPlane( bool enable, Plane plane, string materialName, float scale, float tiling, bool drawFirst, float bow, int xsegments, int ysegments )
        {
            isSkyPlaneEnabled = enable;

            if ( enable )
            {
                string meshName = "SkyPlane";
                skyPlane = plane;

                Material m = MaterialManager.Instance.GetByName( materialName );

                if ( m == null )
                    throw new AxiomException( string.Format( "Skyplane material '{0}' not found.", materialName ) );

                // make sure the material doesn't update the depth buffer
                m.DepthWrite = false;
                m.Load();

                isSkyPlaneDrawnFirst = drawFirst;

                // set up the place
                Mesh planeMesh = MeshManager.Instance.GetByName( meshName );

                // unload the old one if it exists
                if ( planeMesh != null )
                    MeshManager.Instance.Unload( planeMesh );

                // create up vector
                Vector3 up = plane.Normal.CrossProduct( Vector3.UnitX );
                if ( up == Vector3.Zero )
                    up = plane.Normal.CrossProduct( -Vector3.UnitZ );

                if ( bow > 0 )
                {
                    planeMesh = MeshManager.Instance.CreateCurvedIllusionPlane(
                        meshName,
                        plane,
                        scale * 100,
                        scale * 100,
                        scale * bow * 100,
                        xsegments, ysegments, false, 1, tiling, tiling, up );
                }
                else
                {
                    planeMesh = MeshManager.Instance.CreatePlane( meshName, plane, scale * 100, scale * 100, xsegments, ysegments, false, 1, tiling, tiling, up );
                }

                if ( skyPlaneEntity != null )
                {
                    entityList.Remove( skyPlaneEntity );
                }

                // create entity for the plane, using the mesh name
                skyPlaneEntity = CreateEntity( meshName, meshName );
                skyPlaneEntity.MaterialName = materialName;
                // sky entities need not cast shadows
                skyPlaneEntity.CastShadows = false;

                if ( skyPlaneNode == null )
                    skyPlaneNode = CreateSceneNode( meshName + "Node" );
                else
                    skyPlaneNode.DetachAllObjects();

                // attach the skyplane to the new node
                skyPlaneNode.AttachObject( skyPlaneEntity );
            }
        }

        /// <summary>
        ///		Overload.
        /// </summary>
        /// <param name="enable"></param>
        /// <param name="plane"></param>
        public virtual void SetSkyPlane( bool enable, Plane plane, string materialName )
        {
            // call the overloaded method
            SetSkyPlane( enable, plane, materialName, 1000.0f, 10.0f, true, 0, 1, 1 );
        }

        /// <summary>
        ///		Internal method for notifying the manager that a SceneNode is autotracking.
        /// </summary>
        /// <param name="node">Scene node that is auto tracking another scene node.</param>
        /// <param name="autoTrack">True if tracking, false if it is stopping tracking.</param>
        internal void NotifyAutoTrackingSceneNode( SceneNode node, bool autoTrack )
        {
            if ( autoTrack )
            {
                autoTrackingSceneNodes.Add( node );
            }
            else
            {
                autoTrackingSceneNodes.Remove( node );
            }
        }

        /// <summary>
        /// If set, only the selected node is rendered.
        /// To render all nodes, set to null.
        /// </summary>
        /// 
        public void OverrideRootSceneNode( SceneNode node )
        {
            rootSceneNode = node;
        }

        public void RestoreRootSceneNode()
        {
            rootSceneNode = defaultRootNode;
        }

        #endregion Internal Methods

        #region ShadowCasterSceneQueryListener Class
        /// <summary>
        ///		Nested class to use as a callback for shadow caster scene query.
        /// </summary>
        protected class ShadowCasterSceneQueryListener : ISceneQueryListener
        {
            #region Fields

            protected ArrayList casterList = new ArrayList();
            protected bool isLightInFrustum;
            protected PlaneBoundedVolumeList lightClipVolumeList = new PlaneBoundedVolumeList();
            protected Camera camera;
            protected Light light;
            protected float farDistSquared;

            #endregion Fields

            #region Methods

            /// <summary>
            ///		Prepare the listener for use with a set of parameters.
            /// </summary>
            /// <param name="lightInFrustum"></param>
            /// <param name="lightClipVolumes"></param>
            /// <param name="light"></param>
            /// <param name="camera"></param>
            /// <param name="shadowCasterList"></param>
            /// <param name="farDistSquared"></param>
            public void Prepare( bool lightInFrustum, PlaneBoundedVolumeList lightClipVolumes, Light light,
                Camera camera, ArrayList shadowCasterList, float farDistSquared )
            {

                this.casterList = shadowCasterList;
                this.isLightInFrustum = lightInFrustum;
                this.lightClipVolumeList = lightClipVolumes;
                this.camera = camera;
                this.light = light;
                this.farDistSquared = farDistSquared;
            }

            #endregion Methods

            #region ISceneQueryListener Members

            public bool OnQueryResult( MovableObject sceneObject )
            {
                if ( sceneObject.CastShadows && sceneObject.IsVisible )
                {
                    if ( farDistSquared > 0 )
                    {
                        // Check object is within the shadow far distance
                        Vector3 toObj = sceneObject.ParentNode.DerivedPosition - camera.DerivedPosition;
                        float radius = sceneObject.GetWorldBoundingSphere().Radius;
                        float dist = toObj.LengthSquared;

                        if ( dist - ( radius * radius ) > farDistSquared )
                        {
                            // skip, beyond max range
                            return true;
                        }
                    }

                    // If the object is in the frustum, we can always see the shadow
                    if ( camera.IsObjectVisible( sceneObject.GetWorldBoundingBox() ) )
                    {
                        casterList.Add( sceneObject );
                        return true;
                    }

                    // Otherwise, object can only be casting a shadow into our view if
                    // the light is outside the frustum (or it's a directional light, 
                    // which are always outside), and the object is intersecting
                    // on of the volumes formed between the edges of the frustum and the
                    // light
                    if ( !isLightInFrustum || light.Type == LightType.Directional )
                    {
                        // Iterate over volumes
                        for ( int i = 0; i < lightClipVolumeList.Count; i++ )
                        {
                            PlaneBoundedVolume pbv = (PlaneBoundedVolume)lightClipVolumeList[i];

                            if ( pbv.Intersects( sceneObject.GetWorldBoundingBox() ) )
                            {
                                casterList.Add( sceneObject );
                                return true;
                            }
                        }
                    }
                }

                return true;
            }

            public bool OnQueryResult( SceneQuery.WorldFragment fragment )
            {
                // don't deal with world fragments by default
                return true;
            }

            #endregion ISceneQueryListener Members

        }

        #endregion ShadowCasterSceneQueryListener Class
    }

    #region Default SceneQuery Implementations


    /// <summary>
    ///		Default implementation of a AxisAlignedBoxRegionSceneQuery.
    /// </summary>
    public class DefaultAxisAlignedBoxRegionSceneQuery : AxisAlignedBoxRegionSceneQuery
    {
        internal protected DefaultAxisAlignedBoxRegionSceneQuery( SceneManager creator ) : base( creator )
        {
        }

        public override void Execute( ISceneQueryListener listener )
        {
            // TODO BillboardSets? Will need per-billboard collision most likely
            // Entities only for now
            for ( int i = 0; i < creator.entityList.Count; i++ )
            {
                Entity entity = creator.entityList[i];

                // skip if unattached or filtered out by query flags
                if ( !entity.IsAttached || ( entity.QueryFlags & queryMask ) == 0 )
                {
                    continue;
                }

                if ( box.Intersects( entity.GetWorldBoundingBox() ) )
                {
                    listener.OnQueryResult( entity );
                }
            }
        }
    }

    /// <summary>
    ///    Default implementation of RaySceneQuery.
    /// </summary>
    public class DefaultRaySceneQuery : RaySceneQuery
    {
        internal protected DefaultRaySceneQuery( SceneManager creator ) : base( creator )
        {
        }

        public override void Execute( IRaySceneQueryListener listener )
        {
            // Note that becuase we have no scene partitioning, we actually
            // perform a complete scene search even if restricted results are
            // requested; smarter scene manager queries can utilise the paritioning 
            // of the scene in order to reduce the number of intersection tests 
            // required to fulfil the query

            // TODO BillboardSets? Will need per-billboard collision most likely
            // Entities only for now
            for ( int i = 0; i < creator.entityList.Count; i++ )
            {
                Entity entity = creator.entityList[i];

                // skip if unattached or filtered out by query flags
                if ( !entity.IsAttached || ( entity.QueryFlags & queryMask ) == 0 )
                {
                    continue;
                }

                // test the intersection against the world bounding box of the entity
                IntersectResult results = MathUtil.Intersects( ray, entity.GetWorldBoundingBox() );

                // if the results came back positive, fire the event handler
                if ( results.Hit == true )
                {
                    listener.OnQueryResult( entity, results.Distance );
                }
            }
        }
    }

    /// <summary>
    ///		Default implementation of a SphereRegionSceneQuery.
    /// </summary>
    public class DefaultSphereRegionSceneQuery : SphereRegionSceneQuery
    {
        internal protected DefaultSphereRegionSceneQuery( SceneManager creator ) : base( creator )
        {
        }

        public override void Execute( ISceneQueryListener listener )
        {
            // TODO BillboardSets? Will need per-billboard collision most likely
            // Entities only for now
            Sphere testSphere = new Sphere();

            for ( int i = 0; i < creator.entityList.Count; i++ )
            {
                Entity entity = creator.entityList[i];

                // skip if unattached or filtered out by query flags
                if ( !entity.IsAttached || ( entity.QueryFlags & queryMask ) == 0 )
                {
                    continue;
                }

                testSphere.Center = entity.ParentNode.DerivedPosition;
                testSphere.Radius = entity.BoundingRadius;

                // if the results came back positive, fire the event handler
                if ( sphere.Intersects( testSphere ) )
                {
                    listener.OnQueryResult( entity );
                }
            }
        }
    }

    /// <summary>
    ///		Default implementation of a PlaneBoundedVolumeListSceneQuery.
    /// </summary>
    public class DefaultPlaneBoundedVolumeListSceneQuery : PlaneBoundedVolumeListSceneQuery
    {
        internal protected DefaultPlaneBoundedVolumeListSceneQuery( SceneManager creator ) : base( creator )
        {
        }

        public override void Execute( ISceneQueryListener listener )
        {
            // Entities only for now
            for ( int i = 0; i < creator.entityList.Count; i++ )
            {
                Entity entity = creator.entityList[i];

                // skip if unattached or filtered out by query flags
                if ( !entity.IsAttached || ( entity.QueryFlags & queryMask ) == 0 )
                {
                    continue;
                }

                for ( int v = 0; v < volumes.Count; v++ )
                {
                    PlaneBoundedVolume volume = (PlaneBoundedVolume)volumes[v];
                    // Do AABB / plane volume test
                    if ( ( entity.QueryFlags & queryMask ) != 0 && volume.Intersects( entity.GetWorldBoundingBox() ) )
                    {
                        listener.OnQueryResult( entity );
                        break;
                    }
                }
            }
        }
    }

    /// <summary>
    ///    Default implementation of IntersectionSceneQuery.
    /// </summary>
    public class DefaultIntersectionSceneQuery : IntersectionSceneQuery
    {
        internal protected DefaultIntersectionSceneQuery( SceneManager creator )
            : base( creator )
        {
            // No world geometry results supported
            this.AddWorldFragmentType( WorldFragmentType.None );
        }

        public override void Execute( IIntersectionSceneQueryListener listener )
        {
            // TODO BillboardSets? Will need per-billboard collision most likely
            // Entities only for now
            int numEntities = creator.entityList.Count;
            for ( int a = 0; a < ( numEntities - 1 ); a++ )
            {
                Entity aent = creator.entityList[a];

                // skip if unattached or filtered out by query flags
                if ( !aent.IsAttached || ( aent.QueryFlags & queryMask ) == 0 )
                {
                    continue;
                }

                // Loop b from a+1 to last
                int b = a;
                for ( ++b; b != ( numEntities - 1 ); ++b )
                {
                    Entity bent = creator.entityList[b];

                    // skip if unattached or filtered out by query flags
                    if ( !bent.IsAttached || ( bent.QueryFlags & queryMask ) == 0 )
                    {
                        continue;
                    }

                    // Apply mask to b (both must pass)
                    if ( ( bent.QueryFlags & this.queryMask ) != 0 )
                    {
                        AxisAlignedBox box1 = aent.GetWorldBoundingBox();
                        AxisAlignedBox box2 = bent.GetWorldBoundingBox();

                        if ( box1.Intersects( box2 ) )
                        {
                            listener.OnQueryResult( aent, bent );
                        }
                    }

                }
            }
        }
    }

    #endregion Default SceneQuery Implementations

}

