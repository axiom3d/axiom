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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Axiom.Animating;
using Axiom.Animating.Collections;
using Axiom.Collections;
using Axiom.Configuration;
using Axiom.Controllers;
using Axiom.Core.Collections;
using Axiom.Graphics;
using Axiom.Graphics.Collections;
using Axiom.Math;
using Axiom.Math.Collections;
using Axiom.Media;
using Axiom.Overlays;
using ResourceHandle = System.UInt64;

#endregion Namespace Declarations

namespace Axiom.Core
{
	#region Delegate declarations

	/// <summary>
	///		Delegate for speicfying the method signature for a render queue event.
	/// </summary>
	public delegate bool RenderQueueEvent( RenderQueueGroupID priority );

	/// <summary>
	/// Delegate for FindVisibleObject events
	/// </summary>
	/// <param name="manager"></param>
	/// <param name="stage"></param>
	/// <param name="view"></param>
	public delegate void FindVisibleObjectsEvent( SceneManager manager, IlluminationRenderStage stage, Viewport view );

	#endregion Delegate declarations

	/// <summary>
	///		Manages the organization and rendering of a 'scene' i.e. a collection
	///		of objects and potentially world geometry.
	/// </summary>
	/// <remarks>
	///		This class defines the interface and the basic behaviour of a
	///		'Scene Manager'. A SceneManager organizes the culling and rendering of
	///		the scene, in conjunction with the <see cref="RenderQueue"/>. This class is designed
	///		to be extended through subclassing in order to provide more specialized
	///		scene organization structures for particular needs. The default
	///		SceneManager culls based on a hierarchy of node bounding boxes, other
	///		implementations can use an octree (<see name="OctreeSceneManager"/>), a BSP
    ///		tree (<see name="BspSceneManager"/>), and many other options. New SceneManager
	///		implementations can be added at runtime by plugins, see <see cref="SceneManagerEnumerator"/>
	///		for the interfaces for adding new SceneManager types.
	///   <p/>
	///		There is a distinction between 'objects' (which subclass <see cref="MovableObject"/>,
	///		and are movable, discrete objects in the world), and 'world geometry',
	///		which is large, generally static geometry. World geometry tends to
	///		influence the SceneManager organizational structure (e.g. lots of indoor
	///		static geometry might result in a spatial tree structure) and as such
	///		world geometry is generally tied to a given SceneManager implementation,
	///		whilst MovableObject instances can be used with any SceneManager.
	///		Subclasses are free to define world geometry however they please.
	///  <p/>
	///		Multiple SceneManager instances can exist at one time, each one with
	///		a distinct scene. Which SceneManager is used to render a scene is
	///		dependent on the <see cref="Camera"/>, which will always call back the SceneManager
	///		which created it to render the scene.
	///	 </remarks>
	/// TODO: Thoroughly review node removal/cleanup.
	/// TODO: Review of method visibility/virtuality to ensure consistency.
    /// 
	public abstract class SceneManager : DisposableObject
	{
        bool cameraRelativeRendering = false; // implement logic

		#region Fields

		/// <summary>
		///
		/// </summary>
		protected CompositorChain _activeCompositorChain;
		protected static int lastNumTexUnitsUsed = 0;

		/// <summary>
		///    Local light list for use during rendering passes.
		/// </summary>
		protected static LightList localLightList = new LightList();

		/// <summary>
		///    Whether normals are currently being normalized.
		/// </summary>
		protected static bool normalizeNormals;

		protected static ColorEx oldFogColor;
		protected static float oldFogDensity;
		protected static float oldFogEnd;
		protected static FogMode oldFogMode;
		protected static float oldFogStart;
		protected static RenderOperation op = new RenderOperation();

		/// <summary>The ambient color, cached from the RenderSystem</summary>
		/// <remarks>Default to a semi-bright white (gray) light to prevent being null</remarks>
		protected ColorEx ambientColor = ColorEx.Gray;

		/// <summary>A list of animations for easy lookup.</summary>
		protected AnimationCollection animationList;

		/// <summary>A list of animation states for easy lookup.</summary>
		protected AnimationStateSet animationStateList;

		//public ICollection AnimationStates { get { return animationStateList.Values; } }

		/// <summary>
		///    Utility class for updating automatic GPU program params.
		/// </summary>
		protected AutoParamDataSource autoParamDataSource = new AutoParamDataSource();

		/// <summary>
		///		Active list of nodes tracking other nodes.
		/// </summary>
		protected SceneNodeCollection autoTrackingSceneNodes = new SceneNodeCollection();

		/// <summary>A reference to the current camera being used for rendering.</summary>
		protected Camera cameraInProgress;

		/// <summary>A list of the valid cameras for this scene for easy lookup.</summary>
		protected CameraCollection cameraList;

		protected Viewport currentViewport;

		/// <summary>
		///		If set, only this scene node (and children) will be rendered.
		///		If null, root node is used.
		/// </summary>
		protected SceneNode defaultRootNode;

		/// <summary>Flag indicating whether SceneNodes will be rendered as a set of 3 axes.</summary>
		protected bool displayNodes;

		/// <summary>
		///     Find visible objects?
		/// </summary>
		protected bool findVisibleObjects;

		/// <summary>
		///		Program parameters for finite extrusion programs.
		/// </summary>
		protected GpuProgramParameters finiteExtrusionParams;

		protected ColorEx fogColor;
		protected float fogDensity;
		protected float fogEnd;
		protected FogMode fogMode;
		protected float fogStart;

		/// <summary>
		///		Full screen rectangle to use for rendering stencil shadows.
		/// </summary>
		protected Rectangle2D fullScreenQuad;

		/// <summary>Denotes whether or not the camera has been changed.</summary>
		protected bool hasCameraChanged;

		/// <summary>
		///		Current stage of rendering.
		/// </summary>
		protected IlluminationRenderStage illuminationStage;

		/// <summary>
		///		Program parameters for infinite extrusion programs.
		/// </summary>
		protected GpuProgramParameters infiniteExtrusionParams;

		protected bool isSkyBoxDrawnFirst;
		protected bool isSkyBoxEnabled;
		protected bool isSkyDomeDrawnFirst;
		protected bool isSkyDomeEnabled;
		protected bool isSkyPlaneDrawnFirst;
		protected bool isSkyPlaneEnabled;
		protected ulong lastFrameNumber;
		protected PolygonMode lastPolyMode = PolygonMode.Points;
		protected bool lastProjectionWasIdentity;

		protected bool lastUsedFallback;
		protected bool lastViewWasIdentity;

		/// <summary>
		///		List of lights in view that could cast shadows.
		/// </summary>
		protected LightList lightsAffectingFrustum = new LightList();

		/// <summary>  The instance name of this scene manager.</summary>
		protected string name;

		protected LightList nullLightList = new LightList();

		/// <summary>Hashtable of options that can be used by this or any other scene manager.</summary>
		protected AxiomCollection<object> optionList = new AxiomCollection<object>();

		/// <summary>
		///    A list of the Regions.
		///    TODO: Is there any point to having this region list?
		/// </summary>
		protected List<StaticGeometry.Region> regionList;

		/// <summary>
		/// True when the main priority group is rendering.
		/// </summary>
		protected bool renderingMainGroup;

		/// <summary>
		/// True when calling RenderSolidObjects with the noShadow queue.
		/// </summary>
		protected bool renderingNoShadowQueue;

		/// <summary>A queue of objects for rendering.</summary>
		protected RenderQueue renderQueue;

		/// <summary>
		/// A List of RenderQueues to either Include or Exclude in the rendering sequence.
		/// </summary>
		protected readonly SpecialCaseRenderQueue specialCaseRenderQueueList = new SpecialCaseRenderQueue();

		public SpecialCaseRenderQueue SpecialCaseRenderQueueList
		{
			get
			{
				return this.specialCaseRenderQueueList;
			}
		}

		protected RenderQueueGroupID worldGeometryRenderQueueId = RenderQueueGroupID.WorldGeometryOne;

		public virtual RenderQueueGroupID WorldGeometryRenderQueueId
		{
			get
			{
				return this.worldGeometryRenderQueueId;
			}

			set
			{
				this.worldGeometryRenderQueueId = value;
			}
		}

		/// <summary>The root of the scene graph heirarchy.</summary>
		protected SceneNode rootSceneNode;

		/// <summary>A list of scene nodes (includes all in the scene graph).</summary>
		protected SceneNodeCollection sceneNodeList;

		/// <summary>
		///		AxisAlignedBox region query to find shadow casters within the attenuation range of a directional light.
		/// </summary>
		protected AxisAlignedBoxRegionSceneQuery shadowCasterAABBQuery;

		/// <summary>
		///		Current list of shadow casters within the view of the camera.
		/// </summary>
		protected List<ShadowCaster> shadowCasterList = new List<ShadowCaster>();
		/// <summary>
		///		A pass designed to let us render shadow colour on white for texture shadows
		/// </summary>
		protected Pass shadowCasterPlainBlackPass;

		/// <summary>
		///		Listener to use when finding shadow casters for a light within a scene.
		/// </summary>
		protected ShadowCasterSceneQueryListener shadowCasterQueryListener;

		/// <summary>
		///		Sphere region query to find shadow casters within the attenuation range of a point/spot light.
		/// </summary>
		protected SphereRegionSceneQuery shadowCasterSphereQuery;

		/// <summary>
		///
		/// </summary>
		protected ColorEx shadowColor;

		/// <summary>
		///		Pass to use for rendering debug shadows.
		/// </summary>
		protected Pass shadowDebugPass;

		/// <summary>
		///		Explicit extrusion distance for directional lights.
		/// </summary>
		protected float shadowDirLightExtrudeDist;

		/// <summary>
		///		Farthest distance from the camera at which to render shadows.
		/// </summary>
		protected float shadowFarDistance;

		/// <summary>
		///		shadowFarDistance ^ 2
		/// </summary>
		protected float shadowFarDistanceSquared;

		/// <summary>
		///		buffer to use for indexing shadow geometry required for certain techniques.
		/// </summary>
		protected HardwareIndexBuffer shadowIndexBuffer;

		/// <summary>
		///		The maximum size of the index buffer used to render shadow primitives.
		/// </summary>
		protected int shadowIndexBufferSize;

		/// <summary>
		///		For the RenderTextureShadowCasterQueueGroupObjects and
		///		RenderTextureShadowReceiverQueueGroupObjects methods.
		/// </summary>
		protected bool shadowMaterialInitDone = false;

		/// <summary>
		///		Pass to use while rendering the full screen quad for modulative shadows.
		/// </summary>
		protected Pass shadowModulativePass;

		/// <summary>
		///		A pass designed to let us render shadow receivers for texture shadows
		/// </summary>
		protected Pass shadowReceiverPass;

		/// <summary>
		///
		/// </summary>
		protected Pass shadowStencilPass;

		/// <summary>
		///		Current shadow technique in use in the scene.
		/// </summary>
		protected ShadowTechnique shadowTechnique;

		/// <summary>
		///     Current list of shadow texture cameras.  There is one camera
		///     for each shadow texture.
		/// </summary>
		protected List<Camera> shadowTextureCameras = new List<Camera>();

		/// <summary>
		///		The material file to be use for shadow casters, if any
		/// </summary>
		protected string shadowTextureCasterMaterial;

		/// <summary>
		///
		/// </summary>
		protected ushort shadowTextureCount;

		/// <summary>
		///		The parameters of the pixel program that renders custom shadow casters, or null
		/// </summary>
		protected GpuProgramParameters shadowTextureCustomCasterFPParams;

		/// <summary>
		///		The name of the pixel program that renders custom shadow casters, or null
		/// </summary>
		protected string shadowTextureCustomCasterFragmentProgram;

		/// <summary>
		///		The pass that renders custom texture casters, or null
		/// </summary>
		protected Pass shadowTextureCustomCasterPass;

		/// <summary>
		///		The name of the vertex program that renders custom shadow casters, or null
		/// </summary>
		protected string shadowTextureCustomCasterVertexProgram;

		/// <summary>
		///		The parameters of the vertex program that renders custom shadow casters, or null
		/// </summary>
		protected GpuProgramParameters shadowTextureCustomCasterVPParams;

		/// <summary>
		///		The parameters of the pixel program that renders custom shadow casters, or null
		/// </summary>
		protected GpuProgramParameters shadowTextureCustomReceiverFPParams;

		/// <summary>
		///		The name of the pixel program that renders custom shadow receivers, or null
		/// </summary>
		protected string shadowTextureCustomReceiverFragmentProgram;

		/// <summary>
		///		The material file to be use for shadow receivers, if any
		/// </summary>
		protected Pass shadowTextureCustomReceiverPass;

		/// <summary>
		///		The name of the vertex program that renders custom shadow receivers, or null
		/// </summary>
		protected string shadowTextureCustomReceiverVertexProgram;

		/// <summary>
		///		The parameters of the vertex program that renders custom shadow casters, or null
		/// </summary>
		protected GpuProgramParameters shadowTextureCustomReceiverVPParams;

		/// <summary>
		///		As a proportion e.g. 0.9
		/// </summary>
		protected float shadowTextureFadeEnd;

		/// <summary>
		///		As a proportion e.g. 0.6
		/// </summary>
		protected float shadowTextureFadeStart;

		/// <summary>
		///
		/// </summary>
		protected PixelFormat shadowTextureFormat = PixelFormat.A8R8G8B8;

		/// <summary>
		///		Proportion of texture offset in view direction e.g. 0.4
		/// </summary>
		protected float shadowTextureOffset;

		/// <summary>
		///		The material file to be use for shadow receivers, if any
		/// </summary>
		protected string shadowTextureReceiverMaterial;

		/// <summary>
		///		Current list of shadow textures.
		/// </summary>
		protected List<Texture> shadowTextures = new List<Texture>();

		/// <summary>
		///	    The default implementation of texture shadows uses a fixed-function
		///    	color texture projection approach for maximum compatibility, and
		///     as such cannot support self-shadowing. However, if you decide to
		///	    implement a more complex shadowing technique using
		///	    ShadowTextureCasterMaterial and ShadowTextureReceiverMaterial
		///	    there is a possibility you may be able to support
		///	    self-shadowing (e.g by implementing a shader-based shadow map). In
		///	    this case you might want to enable this option.
		/// </summary>
		protected bool shadowTextureSelfShadow;

		/// <summary>
		///
		/// </summary>
		protected ushort shadowTextureSize;

		/// <summary>
		///		Whether we should override far distance when using stencil volumes
		/// </summary>
		protected bool shadowUseInfiniteFarPlane;

		/// <summary>
		///		If true, shadow volumes will be visible in the scene.
		/// </summary>
		protected bool showDebugShadows;

		protected bool shadowTextureConfigDirty;
		protected List<ShadowTextureConfig> shadowTextureConfigList = new List<ShadowTextureConfig>();
		protected Texture nullShadowTexture;
		protected Dictionary<Camera, Light> shadowCameraLightMapping = new Dictionary<Camera, Light>();

		/// <summary>Flag that specifies whether scene nodes will have their bounding boxes rendered as a wire frame.</summary>
		protected bool showBoundingBoxes;

		protected Entity[] skyBoxEntities = new Entity[ 6 ];
		protected SceneNode skyBoxNode;
		protected Quaternion skyBoxOrientation;
		protected Entity[] skyDomeEntities = new Entity[ 5 ];
		protected SceneNode skyDomeNode;
		protected Quaternion skyDomeOrientation;
		protected Plane skyPlane;
		protected Entity skyPlaneEntity;
		protected SceneNode skyPlaneNode;

		/// <summary>
		///		The list of static geometry instances maintained by
		///     the scene manager
		///</summary>
		protected Dictionary<string, StaticGeometry> staticGeometryList = new Dictionary<string, StaticGeometry>();

		/// <summary>
		///     Suppress render state changes?
		/// </summary>
		protected bool suppressRenderStateChanges;

		/// <summary>
		///     Suppress shadows?
		/// </summary>
		protected bool suppressShadows;

		/// <summary>A reference to the current active render system..</summary>
		protected RenderSystem targetRenderSystem;

		/// <summary>
		///		Used by compositing layer
		///</summary>
		protected ulong visibilityMask = 0xFFFFFFFF;

		protected Matrix4[] xform = new Matrix4[ 256 ];

		#region MovableObjectfactory fields

		protected readonly Dictionary<string, MovableObjectCollection> movableObjectCollectionMap =
			new Dictionary<string, MovableObjectCollection>();

		#endregion MovableObjectfactory fields

		/// <summary>
		/// If set, materials will be resolved from the materials at the
		/// pass-setting stage and not at the render queue building stage.
		/// This is useful when the active material scheme during the render
		/// queue building stage is different from the one during the rendering stage.
		/// </summary>
		public bool IsLateMaterialResolving
		{
			get;
			set;
		}

		public ICollection<Camera> Cameras
		{
			get
			{
				return this.cameraList.Values;
			}
		}

		/// <summary>A list of lights in the scene for easy lookup.</summary>
		public ICollection<MovableObject> Lights
		{
			get
			{
				return this.GetMovableObjectCollection( LightFactory.TypeName ).Values;
			}
		}

		public ICollection<SceneNode> SceneNodes
		{
			get
			{
				return this.sceneNodeList.Values;
			}
		}

		/// <summary>
		///		If true, the shadow technique is based on texture maps
		/// </summary>
		public bool IsShadowTechniqueStencilBased
		{
			get
			{
				return this.shadowTechnique == ShadowTechnique.StencilModulative
					   || this.shadowTechnique == ShadowTechnique.StencilAdditive;
			}
		}

		/// <summary>
		///		If true, the shadow technique is based on texture maps
		/// </summary>
		public bool IsShadowTechniqueTextureBased
		{
			get
			{
				return this.shadowTechnique == ShadowTechnique.TextureModulative
					   || this.shadowTechnique == ShadowTechnique.TextureAdditive;
			}
		}

		/// <summary>
		///		If true, the shadow technique is additive
		/// </summary>
		public bool IsShadowTechniqueAdditive
		{
			get
			{
				return this.shadowTechnique == ShadowTechnique.StencilAdditive
					   || this.shadowTechnique == ShadowTechnique.TextureAdditive;
			}
		}

		/// <summary>
		///		If true, the shadow technique is modulative
		/// </summary>
		protected bool IsShadowTechniqueModulative
		{
			get
			{
				return this.shadowTechnique == ShadowTechnique.StencilModulative
					   || this.shadowTechnique == ShadowTechnique.TextureModulative;
			}
		}

		/// <summary>
		///		Is the shadow technique is not "None"
		/// </summary>
		public bool IsShadowTechniqueInUse
		{
			get
			{
				return this.shadowTechnique != ShadowTechnique.None;
			}
		}

		#endregion Fields

		#region Public events

		public class RenderEventArgs : EventArgs
		{
			public RenderQueueGroupID RenderQueueId;
			public string Invocation;
		}

		public class BeginRenderQueueEventArgs : RenderEventArgs
		{
			public bool SkipInvocation;
		}

		public class EndRenderQueueEventArgs : RenderEventArgs
		{
			public bool RepeatInvocation;
		}

		private readonly ChainedEvent<BeginRenderQueueEventArgs> _queueStartedEvent = new ChainedEvent<BeginRenderQueueEventArgs>();
		/// <summary>
		/// Fired when a render queue is starting to be rendered.
		/// </summary>
		public event EventHandler<BeginRenderQueueEventArgs> QueueStarted
		{
			add
			{
				_queueStartedEvent.EventSinks += value;
			}
			remove
			{
				_queueStartedEvent.EventSinks -= value;
			}
		}

		private readonly ChainedEvent<EndRenderQueueEventArgs> _queueEndedEvent = new ChainedEvent<EndRenderQueueEventArgs>();
		/// <summary>
		/// Fired when a render queue is finished being rendered.
		/// </summary>
		public event EventHandler<EndRenderQueueEventArgs> QueueEnded
		{
			add
			{
				_queueEndedEvent.EventSinks += value;
			}
			remove
			{
				_queueEndedEvent.EventSinks -= value;
			}
		}

		/// <summary>Will fire before FindVisibleObjects is called</summary>
		public event FindVisibleObjectsEvent PreFindVisibleObjects;

		/// <summary>Will fire after FindVisibleObjects is called</summary>
		public event FindVisibleObjectsEvent PostFindVisibleObjects;

		#endregion Public events

		#region Constructors

        public AutoParamDataSource AutoParamData
        {
            get
            {
                return this.autoParamDataSource;
            }
        }

        public SceneManager( string name )
			: base()
		{
			this.cameraList = new CameraCollection();
			this.sceneNodeList = new SceneNodeCollection();
			this.animationList = new AnimationCollection();
			this.animationStateList = new AnimationStateSet();
			this.regionList = new List<StaticGeometry.Region>();

			this.shadowCasterQueryListener = new ShadowCasterSceneQueryListener( this );

			// create the root scene node
			this.rootSceneNode = new SceneNode( this, "Root" );
			this.rootSceneNode.SetAsRootNode();
			this.defaultRootNode = this.rootSceneNode;

			this.name = name;

			// default to no fog
			this.fogMode = FogMode.None;

			// no shadows by default
			this.shadowTechnique = ShadowTechnique.None;

			// setup default shadow camera setup
			this._defaultShadowCameraSetup = new DefaultShadowCameraSetup();

			this.illuminationStage = IlluminationRenderStage.None;
			this.renderingNoShadowQueue = false;
			this.renderingMainGroup = false;
			this.shadowColor.a = this.shadowColor.r = this.shadowColor.g = this.shadowColor.b = 0.25f;
			this.shadowDirLightExtrudeDist = 10000;
			this.shadowIndexBufferSize = 51200;
			this.shadowTextureOffset = 0.6f;
			this.shadowTextureFadeStart = 0.7f;
			this.shadowTextureFadeEnd = 0.9f;
			this.shadowTextureSize = 512;
			this.shadowTextureCount = 1;
			this.findVisibleObjects = true;
			this.suppressRenderStateChanges = false;
			this.suppressShadows = false;
			this.shadowUseInfiniteFarPlane = true;
		}

		#endregion Constructors

		/// <summary>
		/// 
		/// </summary>
		/// <param name="disposeManagedResources"></param>
		protected override void dispose( bool disposeManagedResources )
		{
			if ( !this.IsDisposed )
			{
				if ( disposeManagedResources )
				{
					this.ClearScene();
					this.RemoveAllCameras();

					if ( op != null )
					{
						if ( !op.IsDisposed )
							op.Dispose();

						op = null;
					}

					if ( this.autoParamDataSource != null )
					{
						if ( !this.autoParamDataSource.IsDisposed )
							this.autoParamDataSource.Dispose();

						this.autoParamDataSource = null;
					}

					if ( this.rootSceneNode != null )
					{
						if ( !this.rootSceneNode.IsDisposed )
							this.rootSceneNode.Dispose();

						this.rootSceneNode = null;
					}
				}
			}

			base.dispose( disposeManagedResources );
		}

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
		public virtual SceneNode CreateSceneNode()
		{
			SceneNode node = new SceneNode( this );
			this.sceneNodeList.Add( node );
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
		/// <returns></returns>
		public virtual SceneNode CreateSceneNode( string name )
		{
			SceneNode node = new SceneNode( this, name );
			this.sceneNodeList.Add( node );
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
			if ( this.animationList.ContainsKey( name ) )
			{
				throw new AxiomException( string.Format(
											  "An animation with the name '{0}' already exists in the scene.", name ) );
			}

			// create a new animation and record it locally
			Animation anim = new Animation( name, length );
			this.animationList.Add( name, anim );

			return anim;
		}

		/// <summary>
		///		Create an AnimationState object for managing application of animations.
		/// </summary>
		/// <remarks>
		///		<para>
		///		You can create Animation objects for animating SceneNode obejcts using the
		///		CreateAnimation method. However, in order to actually apply those animations
		///		you have to call methods on Node and Animation in a particular order (namely
		///		Node.ResetToInitialState and Animation.Apply). To make this easier and to
		///		help track the current time position of animations, the AnimationState object
		///		is provided.
		///		</para>
		///		<para>
		///		So if you don't want to control animation application manually, call this method,
		///		update the returned object as you like every frame and let SceneManager apply
		///		the animation state for you.
		///		</para>
		///		<para>
		///		Remember, AnimationState objects are disabled by default at creation time.
		///		Turn them on when you want them using their Enabled property.
		///		</para>
		///		<para>
		///		Note that any SceneNode affected by this automatic animation will have it's state
		///		reset to it's initial position before application of the animation. Unless specifically
		///		modified using Node.SetInitialState the Node assumes it's initial state is at the
		///		origin. If you want the base state of the SceneNode to be elsewhere, make your changes
		///		to the node using the standard transform methods, then call SetInitialState to
		///		'bake' this reference position into the node.
		///		</para>
		/// </remarks>
		/// <param name="animationName"></param>
		/// <returns></returns>
		public virtual AnimationState CreateAnimationState( string animationName )
		{
			// do we have this already?
			if ( this.animationStateList.HasAnimationState( animationName ) )
			{
				throw new AxiomException( "Cannot create, AnimationState already exists: " + animationName );
			}

			if ( !this.animationList.ContainsKey( animationName ) )
			{
				throw new AxiomException(
					string.Format(
						"The name of a valid animation must be supplied when creating an AnimationState.  Animation '{0}' does not exist.",
						animationName ) );
			}

			// get a reference to the sepcified animation
			Animation anim = this.animationList[ animationName ];

			// create and return new animation state
			return this.animationStateList.CreateAnimationState( animationName, 0, anim.Length );
		}

		/// <summary>
		///		Overloaded method.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public virtual BillboardSet CreateBillboardSet( string name )
		{
			// return new billboardset with a default pool size of 20
			return this.CreateBillboardSet( name, 20 );
		}

		/// <summary>
		///		Creates a billboard set which can be uses for particles, sprites, etc.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="poolSize"></param>
		/// <returns></returns>
		public virtual BillboardSet CreateBillboardSet( string name, int poolSize )
		{
			NamedParameterList param = new NamedParameterList();
			param.Add( "poolSize", poolSize.ToString() );
			return (BillboardSet)this.CreateMovableObject( name, BillboardSetFactory.TypeName, param );
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
			if ( this.cameraList.ContainsKey( name ) )
			{
				throw new AxiomException( string.Format( "A camera with the name '{0}' already exists in the scene.",
														 name ) );
			}

			// create the camera and add it to our local list
			Camera camera = new Camera( name, this );
			this.cameraList.Add( camera );

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
			NamedParameterList param = new NamedParameterList();
			param.Add( "mesh", meshName );
			return (Entity)this.CreateMovableObject( name, EntityFactory.TypeName, param );
		}

		/// <summary>
		///		Create an Entity (instance of a discrete mesh).
		/// </summary>
		/// <param name="name">The name to be given to the entity (must be unique).</param>
		/// <param name="mesh">The mesh to use.</param>
		/// <returns></returns>
		public virtual Entity CreateEntity( string name, Mesh mesh )
		{
			NamedParameterList param = new NamedParameterList();
			param.Add( "mesh", mesh );
			return (Entity)this.CreateMovableObject( name, EntityFactory.TypeName, param );
		}

		/// <summary>
		///		Create an Entity (instance of a discrete mesh).
		/// </summary>
		/// <param name="name">The name to be given to the entity (must be unique).</param>
        /// <param name="prefab">The name of the mesh to load.  Will be loaded if not already.</param>
		/// <returns></returns>
		public virtual Entity CreateEntity( string name, PrefabEntity prefab )
		{
			switch ( prefab )
			{
				case PrefabEntity.Plane:
					return this.CreateEntity( name, "Prefab_Plane" );
				case PrefabEntity.Cube:
					return this.CreateEntity( name, "Prefab_Cube" );
				case PrefabEntity.Sphere:
					return this.CreateEntity( name, "Prefab_Sphere" );
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
			return (Light)this.CreateMovableObject( name, LightFactory.TypeName, null );
		}

		/// <summary>
		///     Create MovableText, 3D floating text which can me moved around your scene
		/// </summary>
		/// <param name="name">
		///     The name to be given to the object (must be unique).
		/// </param>
		/// <param name="caption">
		/// The text tyo display
		/// </param>
		/// <param name="fontName">
		/// The font to use for the text, must be already loaded as a resource.
		/// </param>
		public MovableText CreateMovableText( string name, string caption, string fontName )
		{
			return (MovableText)this.CreateMovableObject( name, MovableTextFactory.TypeName, new NamedParameterList() { { "caption", caption }, { "fontName", fontName } } );
		}

		/// <summary>
		///     Retrieves the named MovableText.
		/// </summary>
		/// <param name="name">
		///     The name of the object to retrieve.
		/// </param>
		/// <returns>
		///     An instance of MovablText.
		/// </returns>
		/// <exception cref="AxiomException">
		///     Thrown if the names does not exists in the collection.
		/// </exception>
		public MovableText GetMovableText( string name )
		{
			return (MovableText)this.GetMovableObject( name, MovableTextFactory.TypeName );
		}

		/// <summary>
		///     Create a ManualObject, an object which you populate with geometry
		///     manually through a GL immediate-mode style interface.
		/// </summary>
		/// <param name="name">
		///     The name to be given to the object (must be unique).
		/// </param>
		public ManualObject CreateManualObject( string name )
		{
			return (ManualObject)this.CreateMovableObject( name, ManualObjectFactory.TypeName, null );
		}

		/// <summary>
		///     Retrieves the named ManualObject.
		/// </summary>
		/// <param name="name">
		///     The name of the object to retrieve.
		/// </param>
		/// <returns>
		///     An instance of ManualObject.
		/// </returns>
		/// <exception cref="AxiomException">
		///     Thrown if the names does not exists in the collection.
		/// </exception>
		public ManualObject GetManualObject( string name )
		{
			return (ManualObject)this.GetMovableObject( name, ManualObjectFactory.TypeName );
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
			DestroyAllStaticGeometry();
			this.DestroyAllMovableObjects();

			if ( this.rootSceneNode != null )
			{
				this.rootSceneNode.RemoveAllChildren();
				this.rootSceneNode.DetachAllObjects();
			}

			// Delete all SceneNodes, except root that is
			foreach ( Node node in sceneNodeList )
			{
				foreach ( SceneNode currentNode in this.sceneNodeList.Values )
				{
					if ( !currentNode.IsDisposed )
						currentNode.Dispose();
				}
			}
			this.sceneNodeList.Clear();

			if ( this.autoTrackingSceneNodes != null )
			{
				this.autoTrackingSceneNodes.Clear();
			}

			// Clear animations
			this.DestroyAllAnimations();

			// Remove sky nodes since they've been deleted
			this.skyBoxNode = this.skyPlaneNode = this.skyDomeNode = null;
			this.isSkyBoxEnabled = this.isSkyPlaneEnabled = this.isSkyDomeEnabled = false;

			if ( renderQueue != null )
				renderQueue.Clear();
		}

		/// <summary>
		///    Destroys and removes a node from the scene.
		/// </summary>
		/// <param name="name"></param>
		public virtual void DestroySceneNode( string name )
		{
			SceneNode node;
			if ( !this.sceneNodeList.TryGetValue( name, out node ) )
			{
				throw new AxiomException( "SceneNode named '{0}' not found.", name );
			}

			this.DestroySceneNode( node );
		}

		/// <summary>
		///    Destroys and removes a node from the scene.
		/// </summary>
		/// <param name="node">A SceneNode</param>
		public virtual void DestroySceneNode( SceneNode node )
		{
			DestroySceneNode( node, true );
		}

		/// <summary>
		/// Internal method to destroy and remove a node from the scene.
		/// Do not remove from parent by option.
		/// </summary>
		/// <param name="node"></param>
		/// <param name="removeFromParent"></param>
		internal void DestroySceneNode( SceneNode node, bool removeFromParent )
		{
			// Find any scene nodes which are tracking this node, and turn them off.
			foreach ( SceneNode autoNode in this.autoTrackingSceneNodes.Values )
			{
				// Tracking this node
				if ( autoNode.AutoTrackTarget == node )
				{
					// turn off, this will notify SceneManager to remove
					autoNode.SetAutoTracking( false );
				}
				else if ( autoNode == node )
				{
					// node being removed is a tracker
					autoTrackingSceneNodes.Remove( name );
				}
			}

			if ( removeFromParent && node.Parent != null )
			{
				node.Parent.RemoveChild( node );
			}

			// removes the node from the list
			sceneNodeList.Remove( node.Name );
		}

		/// <summary>
		///		Destroys an Animation.
		/// </summary>
		/// <param name="name"></param>
		public virtual void DestroyAnimation( string name )
		{
			// Also destroy any animation states referencing this animation
			this.animationStateList.RemoveAnimationState( name );
			Animation animation = this.animationList[ name ];
			if ( animation == null )
			{
				throw new AxiomException( "Animation named '{0}' not found.", name );
			}
			animationList.Remove( name );
		}

		/// <summary>
		///		Destroys an AnimationState.
		/// </summary>
		/// <param name="name"></param>
		public virtual void DestroyAnimationState( string name )
		{
			AnimationState animationState = this.animationStateList.GetAnimationState( name );
			if ( animationState == null )
			{
				throw new AxiomException( "AnimationState named '{0}' not found.", name );
			}

			this.animationStateList.RemoveAnimationState( name );
		}

		/// <summary>
		///		Removes all animations created using this SceneManager.
		/// </summary>
		public virtual void DestroyAllAnimations()
		{
			// Destroy all states too, since they cannot reference destroyed animations
			this.DestroyAllAnimationStates();
			if ( this.animationList != null )
			{
				this.animationList.Clear();
			}
		}

		/// <summary>
		///		Removes all AnimationStates created using this SceneManager.
		/// </summary>
		public virtual void DestroyAllAnimationStates()
		{
			if ( this.animationStateList != null )
			{
				this.animationStateList.RemoveAllAnimationStates();
			}
		}

		/// <summary>
		///		Destroys all the overlays.
		/// </summary>
		public virtual void DestroyAllOverlays()
		{
			OverlayManager.Instance.DestroyAll();
		}

		/// <summary>
		///		Destroys the named Overlay.
		/// </summary>
		public virtual void DestroyOverlay( string name )
		{
			Overlay overlay = OverlayManager.Instance.GetByName( name );

			if ( overlay == null )
			{
				throw new AxiomException( "An overlay named " + name + " cannot be found to be destroyed." );
			}

			OverlayManager.Instance.Destroy( overlay );
		}

		/// <summary>
		///     Retreives the camera with the specified name.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public virtual Camera GetCamera( string name )
		{
			Camera camera = this.cameraList[ name ];
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
			return (Light)this.GetMovableObject( name, LightFactory.TypeName );
		}

		/// <summary>
		///     Retreives the BillboardSet with the specified name.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public virtual BillboardSet GetBillboardSet( string name )
		{
			return (BillboardSet)this.GetMovableObject( name, BillboardSetFactory.TypeName );
		}

		/// <summary>
		///     Retreives the animation with the specified name.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public virtual Animation GetAnimation( string name )
		{
			Animation animation = this.animationList[ name ];
			return animation;
		}

		/// <summary>
		///     Retreives the AnimationState with the specified name.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public virtual AnimationState GetAnimationState( string name )
		{
			AnimationState animationState = this.animationStateList.GetAnimationState( name );
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
			return overlay;
		}

		/// <summary>
		///     Returns the material with the specified name.
		/// </summary>
		/// <param name="name">Name of the material to retrieve.</param>
		/// <returns>A reference to a Material.</returns>
		public virtual Material GetMaterial( string name )
		{
			return (Material)MaterialManager.Instance[ name ];
		}

		/// <summary>
		///     Returns the material with the specified handle.
		/// </summary>
        /// <param name="handle">Handle of the material to retrieve.</param>
		/// <returns>A reference to a Material.</returns>
		public virtual Material GetMaterial( ResourceHandle handle )
		{
			return (Material)MaterialManager.Instance[ handle ];
		}

		/// <summary>
		///		Retrieves the internal render queue.
		/// </summary>
		/// <returns>
		///		The render queue in use by this scene manager.
		///		Note: The queue is created if it doesn't already exist.
		/// </returns>
		public virtual RenderQueue GetRenderQueue()
		{
			if ( this.renderQueue == null )
			{
				this.InitRenderQueue();
			}

			return this.renderQueue;
		}

		/// <summary>
		///		Internal method for initializing the render queue.
		/// </summary>
		/// <remarks>
		///		Subclasses can use this to install their own <see cref="RenderQueue"/> implementation.
		/// </remarks>
		protected virtual void InitRenderQueue()
		{
			this.renderQueue = new RenderQueue();

			// init render queues that do not need shadows
			this.renderQueue.GetQueueGroup( RenderQueueGroupID.Background ).ShadowsEnabled = false;
			this.renderQueue.GetQueueGroup( RenderQueueGroupID.Overlay ).ShadowsEnabled = false;
			this.renderQueue.GetQueueGroup( RenderQueueGroupID.SkiesEarly ).ShadowsEnabled = false;
			this.renderQueue.GetQueueGroup( RenderQueueGroupID.SkiesLate ).ShadowsEnabled = false;
		}

		/// <summary>
		///     Retreives the scene node with the specified name.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public SceneNode GetSceneNode( string name )
		{
			SceneNode node = this.sceneNodeList[ name ];
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
		public Entity GetEntity( string name )
		{
			//Entity entity = entityList[ name ];
			/*
			 * if(entity == null)
			 *		throw new AxiomException("Entity '{0}' could not be found.",name);
			 * */
			//return entity;
			return (Entity)this.GetMovableObject( name, EntityFactory.TypeName );
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

		public void ManualRender( RenderOperation op,
								  Pass pass,
								  Viewport vp,
								  Matrix4 worldMatrix,
								  Matrix4 viewMatrix,
								  Matrix4 projMatrix )
		{
			this.ManualRender( op, pass, vp, worldMatrix, viewMatrix, projMatrix, false );
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
		public virtual void ManualRender( RenderOperation op,
										  Pass pass,
										  Viewport vp,
										  Matrix4 worldMatrix,
										  Matrix4 viewMatrix,
										  Matrix4 projMatrix,
										  bool doBeginEndFrame )
		{
			// configure all necessary parameters
			this.targetRenderSystem.Viewport = vp;
			this.targetRenderSystem.WorldMatrix = worldMatrix;
			this.targetRenderSystem.ViewMatrix = viewMatrix;
			this.targetRenderSystem.ProjectionMatrix = projMatrix;

			if ( doBeginEndFrame )
			{
				this.targetRenderSystem.BeginFrame();
			}

			// set the pass and render the object
			this.SetPass( pass );
			this.targetRenderSystem.Render( op );

			if ( doBeginEndFrame )
			{
				this.targetRenderSystem.EndFrame();
			}
		}

		public void ResetViewProjectionMode()
		{
			if ( this.lastViewWasIdentity )
			{
				// Coming back to normal from identity view
				this.targetRenderSystem.ViewMatrix = this.cameraInProgress.ViewMatrix;
				this.lastViewWasIdentity = false;
			}

			if ( this.lastProjectionWasIdentity )
			{
				// Coming back from flat projection
				this.targetRenderSystem.ProjectionMatrix = this.cameraInProgress.ProjectionMatrixRS;
				this.lastProjectionWasIdentity = false;
			}
		}

		#endregion Virtual methods

		#region Protected methods

		protected const string SHADOW_VOLUMES_MATERIAL = "Axiom/Debug/ShadowVolumes";
		protected const string SPOT_SHADOW_FADE_IMAGE = "spot_shadow_fade.png";
		protected const string STENCIL_SHADOW_MODULATIVE_MATERIAL = "Axiom/StencilShadowModulationPass";
		protected const string STENCIL_SHADOW_VOLUMES_MATERIAL = "Axiom/StencilShadowVolumes";
		protected const string TEXTURE_SHADOW_CASTER_MATERIAL = "Axiom/TextureShadowCaster";
		protected const string TEXTURE_SHADOW_RECEIVER_MATERIAL = "Axiom/TextureShadowReceiver";

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
			this.lightsAffectingFrustum.Clear();

			MovableObjectCollection lightList = this.GetMovableObjectCollection( LightFactory.TypeName );

			// sphere to use for testing
			Sphere sphere = new Sphere();

			foreach ( Light light in lightList.Values )
			{
                if (cameraRelativeRendering)
                    light.CameraRelative = cameraInProgress;
                else
                    light.CameraRelative = null;

				if ( light.IsVisible )
				{
					if ( light.Type == LightType.Directional )
					{
						// Always visible
						this.lightsAffectingFrustum.Add( light );
					}
					else
					{
						// treating spotlight as point for simplicity
						// Just see if the lights attenuation range is within the frustum
						sphere.Center = light.GetDerivedPosition();
						sphere.Radius = light.AttenuationRange;

						if ( camera.IsObjectVisible( sphere ) )
						{
							this.lightsAffectingFrustum.Add( light );
						}
					}
				}
			}

			// notify light dirty, so all movable objects will re-populate
			// their light list next time
			NotifyLightsDirty();
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
			this.shadowCasterList.Clear();

			if ( light.Type == LightType.Directional )
			{
				// Basic AABB query encompassing the frustum and the extrusion of it
				AxisAlignedBox aabb = new AxisAlignedBox();
				Vector3[] corners = camera.WorldSpaceCorners;
				Vector3 min, max;
				Vector3 extrude = light.DerivedDirection * -this.shadowDirLightExtrudeDist;
				// do first corner
				min = max = corners[ 0 ];
				min.Floor( corners[ 0 ] + extrude );
				max.Ceil( corners[ 0 ] + extrude );
				for ( int c = 1; c < 8; ++c )
				{
					min.Floor( corners[ c ] );
					max.Ceil( corners[ c ] );
					min.Floor( corners[ c ] + extrude );
					max.Ceil( corners[ c ] + extrude );
				}
				aabb.SetExtents( min, max );

				if ( this.shadowCasterAABBQuery == null )
				{
					this.shadowCasterAABBQuery = this.CreateAABBRegionQuery( aabb );
				}
				else
				{
					this.shadowCasterAABBQuery.Box = aabb;
				}
				// Execute, use callback
				this.shadowCasterQueryListener.Prepare( false,
														light.GetFrustumClipVolumes( camera ),
														light,
														camera,
														this.shadowCasterList,
														light.ShadowFarDistanceSquared );
				this.shadowCasterAABBQuery.Execute( this.shadowCasterQueryListener );
			}
			else
			{
				Sphere s = new Sphere( light.GetDerivedPosition(), light.AttenuationRange );

				// eliminate early if camera cannot see light sphere
				if ( camera.IsObjectVisible( s ) )
				{
					// create or init a sphere region query
					if ( this.shadowCasterSphereQuery == null )
					{
						this.shadowCasterSphereQuery = this.CreateSphereRegionQuery( s );
					}
					else
					{
						this.shadowCasterSphereQuery.Sphere = s;
					}

					// check if the light is within view of the camera
					bool lightInFrustum = camera.IsObjectVisible( light.GetDerivedPosition() );

					PlaneBoundedVolumeList volumeList = null;

					// Only worth building an external volume list if
					// light is outside the frustum
					if ( !lightInFrustum )
					{
						volumeList = light.GetFrustumClipVolumes( camera );
					}

					// prepare the query and execute using the callback
					this.shadowCasterQueryListener.Prepare(
						lightInFrustum,
						volumeList,
						light,
						camera,
						this.shadowCasterList,
						light.ShadowFarDistanceSquared );

					this.shadowCasterSphereQuery.Execute( this.shadowCasterQueryListener );
				}
			}

			return this.shadowCasterList;
		}

		/// <summary>
		///		Internal method for setting up materials for shadows.
		/// </summary>
		protected virtual void InitShadowVolumeMaterials()
		{
			if ( this.shadowMaterialInitDone )
			{
				return;
			}

			if ( this.shadowDebugPass == null )
			{
				this.InitShadowDebugPass();
			}

			if ( this.shadowStencilPass == null )
			{
				this.InitShadowStencilPass();
			}

			if ( this.shadowModulativePass == null )
			{
				this.InitShadowModulativePass();
			}

			// Also init full screen quad while we're at it
			if ( this.fullScreenQuad == null )
			{
				this.fullScreenQuad = new Rectangle2D();
				this.fullScreenQuad.SetCorners( -1, 1, 1, -1 );
			}

			// Also init shadow caster material for texture shadows
			if ( this.shadowCasterPlainBlackPass == null )
			{
				this.InitShadowCasterPass();
			}

			if ( this.shadowReceiverPass == null )
			{
				this.InitShadowReceiverPass();
			}

			// InitShadowReceiverPass up spot shadow fade texture (loaded from code data block)
			Texture spotShadowFadeTex = TextureManager.Instance[ SPOT_SHADOW_FADE_IMAGE ];

			if ( spotShadowFadeTex == null )
			{
				// Load the manual buffer into an image
				MemoryStream imgStream = new MemoryStream( SpotShadowFadePng.SPOT_SHADOW_FADE_PNG );
				Image img = Image.FromStream( imgStream, "png" );
				spotShadowFadeTex = TextureManager.Instance.LoadImage( SPOT_SHADOW_FADE_IMAGE,
																	   ResourceGroupManager.InternalResourceGroupName,
																	   img,
																	   TextureType.TwoD );
			}

			this.shadowMaterialInitDone = true;
		}

		private void InitShadowReceiverPass()
		{
			Material matShadRec = (Material)MaterialManager.Instance[ TEXTURE_SHADOW_RECEIVER_MATERIAL ];

			if ( matShadRec == null )
			{
				matShadRec =
					(Material)
					MaterialManager.Instance.Create( TEXTURE_SHADOW_RECEIVER_MATERIAL,
													 ResourceGroupManager.InternalResourceGroupName );
				this.shadowReceiverPass = matShadRec.GetTechnique( 0 ).GetPass( 0 );
				this.shadowReceiverPass.SetSceneBlending( SceneBlendFactor.DestColor, SceneBlendFactor.Zero );
				// Don't set lighting and blending modes here, depends on additive / modulative
				TextureUnitState t = this.shadowReceiverPass.CreateTextureUnitState();
                t.SetTextureAddressingMode( TextureAddressing.Clamp );
			}
			else
			{
				this.shadowReceiverPass = matShadRec.GetTechnique( 0 ).GetPass( 0 );
			}
		}

		private void InitShadowCasterPass()
		{
			Material matPlainBlack = (Material)MaterialManager.Instance[ TEXTURE_SHADOW_CASTER_MATERIAL ];

			if ( matPlainBlack == null )
			{
				matPlainBlack =
					(Material)
					MaterialManager.Instance.Create( TEXTURE_SHADOW_CASTER_MATERIAL,
													 ResourceGroupManager.InternalResourceGroupName );
				this.shadowCasterPlainBlackPass = matPlainBlack.GetTechnique( 0 ).GetPass( 0 );
				// Lighting has to be on, because we need shadow coloured objects
				// Note that because we can't predict vertex programs, we'll have to
				// bind light values to those, and so we bind White to ambient
				// reflectance, and we'll set the ambient colour to the shadow colour
				this.shadowCasterPlainBlackPass.Ambient = ColorEx.White;
				this.shadowCasterPlainBlackPass.Diffuse = ColorEx.Black;
				this.shadowCasterPlainBlackPass.SelfIllumination = ColorEx.Black;
				this.shadowCasterPlainBlackPass.Specular = ColorEx.Black;
				// Override fog
				this.shadowCasterPlainBlackPass.SetFog( true, FogMode.None );
				// no textures or anything else, we will bind vertex programs
				// every so often though
			}
			else
			{
				this.shadowCasterPlainBlackPass = matPlainBlack.GetTechnique( 0 ).GetPass( 0 );
			}
		}

		private void InitShadowModulativePass()
		{
			Material matModStencil = (Material)MaterialManager.Instance[ STENCIL_SHADOW_MODULATIVE_MATERIAL ];

			if ( matModStencil == null )
			{
				// Create
				matModStencil =
					(Material)
					MaterialManager.Instance.Create( STENCIL_SHADOW_MODULATIVE_MATERIAL,
													 ResourceGroupManager.InternalResourceGroupName );

				this.shadowModulativePass = matModStencil.GetTechnique( 0 ).GetPass( 0 );
				this.shadowModulativePass.SetSceneBlending( SceneBlendFactor.DestColor, SceneBlendFactor.Zero );
				this.shadowModulativePass.LightingEnabled = false;
				this.shadowModulativePass.DepthWrite = false;
				this.shadowModulativePass.DepthCheck = false;
				TextureUnitState t = this.shadowModulativePass.CreateTextureUnitState();
				t.SetColorOperationEx(
					LayerBlendOperationEx.Modulate,
					LayerBlendSource.Manual,
					LayerBlendSource.Current,
					this.shadowColor );
				this.shadowModulativePass.CullingMode = CullingMode.None;
			}
			else
			{
				this.shadowModulativePass = matModStencil.GetTechnique( 0 ).GetPass( 0 );
			}
		}

		private void InitShadowDebugPass()
		{
			Material matDebug = (Material)MaterialManager.Instance[ SHADOW_VOLUMES_MATERIAL ];

			if ( matDebug == null )
			{
				// Create
				matDebug =
					(Material)
					MaterialManager.Instance.Create( SHADOW_VOLUMES_MATERIAL,
													 ResourceGroupManager.InternalResourceGroupName );
				this.shadowDebugPass = matDebug.GetTechnique( 0 ).GetPass( 0 );
				this.shadowDebugPass.SetSceneBlending( SceneBlendType.Add );
				this.shadowDebugPass.LightingEnabled = false;
				this.shadowDebugPass.DepthWrite = false;
				TextureUnitState t = this.shadowDebugPass.CreateTextureUnitState();
				t.SetColorOperationEx(
					LayerBlendOperationEx.Modulate,
					LayerBlendSource.Manual,
					LayerBlendSource.Current,
					new ColorEx( 0.7f, 0.0f, 0.2f ) );

				this.shadowDebugPass.CullingMode = CullingMode.None;

				if ( this.targetRenderSystem.Capabilities.HasCapability( Capabilities.VertexPrograms ) )
				{
					ShadowVolumeExtrudeProgram.Initialize();

					// Enable the (infinite) point light extruder for now, just to get some params
					this.shadowDebugPass.SetVertexProgram(
						ShadowVolumeExtrudeProgram.GetProgramName( ShadowVolumeExtrudeProgram.Programs.PointLight ) );

					this.infiniteExtrusionParams = this.shadowDebugPass.VertexProgramParameters;
					this.infiniteExtrusionParams.SetAutoConstant( 0, GpuProgramParameters.AutoConstantType.WorldViewProjMatrix );
					this.infiniteExtrusionParams.SetAutoConstant( 4, GpuProgramParameters.AutoConstantType.LightPositionObjectSpace );
					// Note ignored extra parameter - for compatibility with finite extrusion vertex program
					this.infiniteExtrusionParams.SetAutoConstant( 5, GpuProgramParameters.AutoConstantType.ShadowExtrusionDistance );
				}

				matDebug.Compile();
			}
			else
			{
				this.shadowDebugPass = matDebug.GetTechnique( 0 ).GetPass( 0 );
				if ( this.targetRenderSystem.Capabilities.HasCapability( Capabilities.VertexPrograms ) )
				{
					this.infiniteExtrusionParams = this.shadowDebugPass.VertexProgramParameters;
				}
			}
		}

		private void InitShadowStencilPass()
		{
			Material matStencil = (Material)MaterialManager.Instance[ STENCIL_SHADOW_VOLUMES_MATERIAL ];

			if ( matStencil == null )
			{
				// Create
				matStencil =
					(Material)
					MaterialManager.Instance.Create( STENCIL_SHADOW_VOLUMES_MATERIAL,
													 ResourceGroupManager.InternalResourceGroupName );
				this.shadowStencilPass = matStencil.GetTechnique( 0 ).GetPass( 0 );

				if ( this.targetRenderSystem.Capabilities.HasCapability( Capabilities.VertexPrograms ) )
				{
					// Enable the finite point light extruder for now, just to get some params
					this.shadowStencilPass.SetVertexProgram(
						ShadowVolumeExtrudeProgram.GetProgramName(
							ShadowVolumeExtrudeProgram.Programs.PointLightFinite ) );

					this.finiteExtrusionParams = this.shadowStencilPass.VertexProgramParameters;
					this.finiteExtrusionParams.SetAutoConstant( 0, GpuProgramParameters.AutoConstantType.WorldViewProjMatrix );
					this.finiteExtrusionParams.SetAutoConstant( 4, GpuProgramParameters.AutoConstantType.LightPositionObjectSpace );
					this.finiteExtrusionParams.SetAutoConstant( 5, GpuProgramParameters.AutoConstantType.ShadowExtrusionDistance );
				}
				matStencil.Compile();
				// Nothing else, we don't use this like a 'real' pass anyway,
				// it's more of a placeholder
			}
			else
			{
				this.shadowStencilPass = matStencil.GetTechnique( 0 ).GetPass( 0 );
				if ( this.targetRenderSystem.Capabilities.HasCapability( Capabilities.VertexPrograms ) )
				{
					this.finiteExtrusionParams = this.shadowStencilPass.VertexProgramParameters;
				}
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
			if ( this.IsShadowTechniqueTextureBased )
			{
				Pass retPass;
				if ( pass.Parent.ShadowCasterMaterial != null )
				{
					retPass = pass.Parent.ShadowCasterMaterial.GetBestTechnique().GetPass( 0 );
				}
				else
				{
					retPass = ( this.shadowTextureCustomCasterPass != null ? this.shadowTextureCustomCasterPass : this.shadowCasterPlainBlackPass );
				}

				// Special case alpha-blended passes
				if ( ( pass.SourceBlendFactor == SceneBlendFactor.SourceAlpha &&
					   pass.DestinationBlendFactor == SceneBlendFactor.OneMinusSourceAlpha )
					|| pass.AlphaRejectFunction != CompareFunction.AlwaysPass )
				{
					// Alpha blended passes must retain their transparency
					retPass.SetAlphaRejectSettings( pass.AlphaRejectFunction, pass.AlphaRejectValue );
					retPass.SetSceneBlending( pass.SourceBlendFactor, pass.DestinationBlendFactor );
					retPass.Parent.Parent.TransparencyCastsShadows = true;

					// So we allow the texture units, but override the color functions
					// Copy texture state, shift up one since 0 is shadow texture
					int origPassTUCount = pass.TextureUnitStageCount;
					for ( int t = 0; t < origPassTUCount; ++t )
					{
						TextureUnitState tex;
						if ( retPass.TextureUnitStageCount <= t )
						{
							tex = retPass.CreateTextureUnitState();
						}
						else
						{
							tex = retPass.GetTextureUnitState( t );
						}
						// copy base state
						pass.GetTextureUnitState( t ).CopyTo( tex );
						// override colour function
						tex.SetColorOperationEx( LayerBlendOperationEx.Source1, LayerBlendSource.Manual, LayerBlendSource.Current, this.IsShadowTechniqueAdditive ? ColorEx.Black : shadowColor );
					}
					// Remove any extras
					while ( retPass.TextureUnitStageCount > origPassTUCount )
					{
						retPass.RemoveTextureUnitState( origPassTUCount );
					}
				}
				else
				{
					// reset
					retPass.SetSceneBlending( SceneBlendType.Replace );
					retPass.AlphaRejectFunction = CompareFunction.AlwaysPass;
					while ( retPass.TextureUnitStageCount > 0 )
					{
						retPass.RemoveTextureUnitState( 0 );
					}
				}

				// Propogate culling modes
				retPass.CullingMode = pass.CullingMode;
				retPass.ManualCullingMode = pass.ManualCullingMode;

				// Does incoming pass have a custom shadow caster program?
				if ( pass.ShadowCasterVertexProgramName != "" )
				{
					retPass.SetVertexProgram( pass.ShadowCasterVertexProgramName, false );
					GpuProgram prg = retPass.VertexProgram;
					// Load this program if not done already
					if ( !prg.IsLoaded )
					{
						prg.Load();
					}
					// Copy params
					retPass.VertexProgramParameters = pass.ShadowCasterVertexProgramParameters;
					// Also have to hack the light autoparams, that is done later
				}
				else
				{
					// reset vp?
					if ( retPass == this.shadowTextureCustomCasterPass )
					{
						if ( retPass.VertexProgramName != this.shadowTextureCustomCasterVertexProgram )
						{
							this.shadowTextureCustomCasterPass.SetVertexProgram( this.shadowTextureCustomCasterVertexProgram );
							if ( retPass.HasVertexProgram )
							{
								retPass.VertexProgramParameters = this.shadowTextureCustomCasterVPParams;
							}
						}
					}
					else
					{
						// Standard shadow caster pass, reset to no vp
						retPass.SetVertexProgram( "" );
					}
				}

				return retPass;
			}
			else
			{
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
			if ( this.IsShadowTechniqueTextureBased )
			{
				Pass retPass;
				if ( pass.Parent.ShadowReceiverMaterial != null )
				{
					retPass = pass.Parent.ShadowReceiverMaterial.GetBestTechnique().GetPass( 0 );
				}
				else
				{
					retPass = ( this.shadowTextureCustomReceiverPass != null ? this.shadowTextureCustomReceiverPass : this.shadowReceiverPass );
				}

				// Does incoming pass have a custom shadow receiver program?
				if ( pass.ShadowReceiverVertexProgramName != "" )
				{
					retPass.SetVertexProgram( pass.ShadowReceiverVertexProgramName );
					GpuProgram prg = retPass.VertexProgram;
					// Load this program if not done already
					if ( !prg.IsLoaded )
					{
						prg.Load();
					}
					// Copy params
					retPass.VertexProgramParameters = pass.ShadowReceiverVertexProgramParameters;
					// Also have to hack the light autoparams, that is done later
				}
				else
				{
					if ( retPass == this.shadowTextureCustomReceiverPass )
					{
						if ( this.shadowTextureCustomReceiverPass.VertexProgramName != this.shadowTextureCustomReceiverVertexProgram )
						{
							this.shadowTextureCustomReceiverPass.SetVertexProgram( this.shadowTextureCustomReceiverVertexProgram );
							if ( retPass.HasVertexProgram )
							{
								retPass.VertexProgramParameters = this.shadowTextureCustomReceiverVPParams;
							}
						}
					}
					else
					{
						retPass.SetVertexProgram( "" );
					}
				}
				int keepTUCount;
				// If additive, need lighting parameters & standard programs
				if ( this.IsShadowTechniqueAdditive )
				{
					keepTUCount = 1;
					retPass.LightingEnabled = true;
					retPass.Ambient = pass.Ambient;
					retPass.SelfIllumination = pass.SelfIllumination;
					retPass.Diffuse = pass.Diffuse;
					retPass.Specular = pass.Specular;
					retPass.Shininess = pass.Shininess;
					retPass.SetRunOncePerLight( pass.IteratePerLight,
												pass.RunOnlyOncePerLightType,
												pass.OnlyLightType );
					// We need to keep alpha rejection settings
					retPass.SetAlphaRejectSettings( pass.AlphaRejectFunction, pass.AlphaRejectValue );
					// Copy texture state, shift up one since 0 is shadow texture
					int origPassTUCount = pass.TextureUnitStageCount;
					for ( int t = 0; t < origPassTUCount; ++t )
					{
						int targetIndex = t + 1;
						TextureUnitState tex = ( retPass.TextureUnitStageCount <= targetIndex
													 ?
														 retPass.CreateTextureUnitState()
													 :
														 retPass.GetTextureUnitState( targetIndex ) );
						pass.GetTextureUnitState( t ).CopyTo( tex );
						// If programmable, have to adjust the texcoord sets too
						// D3D insists that texcoordsets match tex unit in programmable mode
						if ( retPass.HasVertexProgram )
							tex.TextureCoordSet = targetIndex;
					}
					keepTUCount = origPassTUCount + 1;
				}
				else
				{
					// need to keep spotlight fade etc
					keepTUCount = retPass.TextureUnitStageCount;
				}

				// Will also need fragment programs since this is a complex light setup
				if ( pass.ShadowReceiverFragmentProgramName != "" )
				{
					// Have to merge the shadow receiver vertex program in
					retPass.SetFragmentProgram( pass.ShadowReceiverFragmentProgramName );
					GpuProgram prg = retPass.FragmentProgram;
					// Load this program if not done already
					if ( !prg.IsLoaded )
					{
						prg.Load();
					}
					// Copy params
					retPass.FragmentProgramParameters = pass.ShadowReceiverFragmentProgramParameters;
					// Did we bind a shadow vertex program?
					if ( pass.HasVertexProgram && !retPass.HasVertexProgram )
					{
						// We didn't bind a receiver-specific program, so bind the original
						retPass.SetVertexProgram( pass.VertexProgramName );
						prg = retPass.VertexProgram;
						// Load this program if required
						if ( !prg.IsLoaded )
						{
							prg.Load();
						}
						// Copy params
						retPass.VertexProgramParameters = pass.VertexProgramParameters;
					}
				}
				else
				{
					// Reset any merged fragment programs from last time
					if ( retPass == this.shadowTextureCustomReceiverPass )
					{
						// reset fp?
						if ( retPass.FragmentProgramName != this.shadowTextureCustomReceiverFragmentProgram )
						{
							retPass.SetFragmentProgram( this.shadowTextureCustomReceiverFragmentProgram );
							if ( retPass.HasFragmentProgram )
							{
								retPass.FragmentProgramParameters = this.shadowTextureCustomReceiverFPParams;
							}
						}
					}
					else
					{
						// Standard shadow receiver pass, reset to no fp
						retPass.SetFragmentProgram( "" );
					}
				}

				// Remove any extra texture units
				while ( retPass.TextureUnitStageCount > keepTUCount )
				{
					retPass.RemoveTextureUnitState( keepTUCount );
				}
				retPass.Load();
				return retPass;
			}
			else
			{
				return pass;
			}
		}

		LightList tmpLightList = new LightList();

		/// <summary>
		///		Internal method for rendering all the objects for a given light into the stencil buffer.
		/// </summary>
		/// <param name="light">The light source.</param>
		/// <param name="camera">The camera being viewed from.</param>
		protected virtual void RenderShadowVolumesToStencil( Light light, Camera camera )
		{
			// get the shadow caster list
			IList casters = this.FindShadowCastersForLight( light, camera );
			if ( casters.Count == 0 )
			{
				// No casters, just do nothing
				return;
			}

			// Set up scissor test (point & spot lights only)
			bool scissored = false;
			if ( light.Type != LightType.Directional &&
				 this.targetRenderSystem.Capabilities.HasCapability( Capabilities.ScissorTest ) )
			{
				// Project the sphere onto the camera
				float left, right, top, bottom;
				Sphere sphere = new Sphere( light.GetDerivedPosition(), light.AttenuationRange );
				if ( camera.ProjectSphere( sphere, out left, out top, out right, out bottom ) )
				{
					scissored = true;
					// Turn normalised device coordinates into pixels
					int iLeft, iTop, iWidth, iHeight;
					this.currentViewport.GetActualDimensions( out iLeft, out iTop, out iWidth, out iHeight );
					int szLeft, szRight, szTop, szBottom;

					szLeft = (int)( iLeft + ( ( left + 1 ) * 0.5f * iWidth ) );
					szRight = (int)( iLeft + ( ( right + 1 ) * 0.5f * iWidth ) );
					szTop = (int)( iTop + ( ( -top + 1 ) * 0.5f * iHeight ) );
					szBottom = (int)( iTop + ( ( -bottom + 1 ) * 0.5f * iHeight ) );

					this.targetRenderSystem.SetScissorTest( true, szLeft, szTop, szRight, szBottom );
				}
			}

			this.targetRenderSystem.UnbindGpuProgram( GpuProgramType.Fragment );

			// Can we do a 2-sided stencil?
			bool stencil2sided = false;

			if ( this.targetRenderSystem.Capabilities.HasCapability( Capabilities.TwoSidedStencil ) &&
				 this.targetRenderSystem.Capabilities.HasCapability( Capabilities.StencilWrap ) )
			{
				// enable
				stencil2sided = true;
			}

			// Do we have access to vertex programs?
			bool extrudeInSoftware = true;

			bool finiteExtrude = !this.shadowUseInfiniteFarPlane ||
								 !this.targetRenderSystem.Capabilities.HasCapability(
									  Capabilities.InfiniteFarPlane );

			if ( this.targetRenderSystem.Capabilities.HasCapability( Capabilities.VertexPrograms ) )
			{
				extrudeInSoftware = false;
				this.EnableHardwareShadowExtrusion( light, finiteExtrude );
			}
			else
			{
				this.targetRenderSystem.UnbindGpuProgram( GpuProgramType.Vertex );
			}

			// Add light to internal list for use in render call
			tmpLightList.Clear();
			tmpLightList.Add( light );

			// Turn off color writing and depth writing
			this.targetRenderSystem.SetColorBufferWriteEnabled( false, false, false, false );
			this.targetRenderSystem.DepthBufferWriteEnabled = false;
			this.targetRenderSystem.StencilCheckEnabled = true;
			this.targetRenderSystem.DepthBufferFunction = CompareFunction.Less;

			// Calculate extrusion distance
			float extrudeDistance = 0;
			if ( light.Type == LightType.Directional )
			{
				extrudeDistance = this.shadowDirLightExtrudeDist;
			}

			// get the near clip volume
			PlaneBoundedVolume nearClipVol = light.GetNearClipVolume( camera );

			// Determine whether zfail is required
			// We need to use zfail for ALL objects if we find a single object which
			// requires it
			bool zfailAlgo = false;

			this.CheckShadowCasters( casters,
									 nearClipVol,
									 light,
									 extrudeInSoftware,
									 finiteExtrude,
									 zfailAlgo,
									 camera,
									 extrudeDistance,
									 stencil2sided,
									 tmpLightList );
			// revert colour write state
			this.targetRenderSystem.SetColorBufferWriteEnabled( true, true, true, true );
			// revert depth state
			this.targetRenderSystem.SetDepthBufferParams();

			this.targetRenderSystem.StencilCheckEnabled = false;

			this.targetRenderSystem.UnbindGpuProgram( GpuProgramType.Vertex );

			if ( scissored )
			{
				// disable scissor test
				this.targetRenderSystem.SetScissorTest( false );
			}
		}

		private void EnableHardwareShadowExtrusion( Light light, bool finiteExtrude )
		{
			// attach the appropriate extrusion vertex program
			// Note we never unset it because support for vertex programs is constant
			this.shadowStencilPass.SetVertexProgram(
				ShadowVolumeExtrudeProgram.GetProgramName( light.Type, finiteExtrude, false ) );

			// Set params
			if ( finiteExtrude )
			{
				this.shadowStencilPass.VertexProgramParameters = this.finiteExtrusionParams;
			}
			else
			{
				this.shadowStencilPass.VertexProgramParameters = this.infiniteExtrusionParams;
			}

			if ( this.showDebugShadows )
			{
				this.shadowDebugPass.SetVertexProgram(
					ShadowVolumeExtrudeProgram.GetProgramName( light.Type, finiteExtrude, true ) );

				// Set params
				if ( finiteExtrude )
				{
					this.shadowDebugPass.VertexProgramParameters = this.finiteExtrusionParams;
				}
				else
				{
					this.shadowDebugPass.VertexProgramParameters = this.infiniteExtrusionParams;
				}
			}

			this.targetRenderSystem.BindGpuProgram( this.shadowStencilPass.VertexProgram );
		}

		private void CheckShadowCasters( IList casters,
										 PlaneBoundedVolume nearClipVol,
										 Light light,
										 bool extrudeInSoftware,
										 bool finiteExtrude,
										 bool zfailAlgo,
										 Camera camera,
										 float extrudeDistance,
										 bool stencil2sided,
										 LightList tmpLightList )
		{
			int flags;
			for ( int i = 0; i < casters.Count; i++ )
			{
				ShadowCaster caster = (ShadowCaster)casters[ i ];

				if ( nearClipVol.Intersects( caster.GetWorldBoundingBox() ) )
				{
					// We have a zfail case, we must use zfail for all objects
					zfailAlgo = true;

					break;
				}
			}

			for ( int ci = 0; ci < casters.Count; ci++ )
			{
				ShadowCaster caster = (ShadowCaster)casters[ ci ];
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
					this.shadowTechnique, light, this.shadowIndexBuffer, extrudeInSoftware, extrudeDistance, flags );

				// If using one-sided stencil, render the first pass of all shadow
				// renderables before all the second passes
				for ( int i = 0; i < ( stencil2sided ? 1 : 2 ); i++ )
				{
					if ( i == 1 )
					{
						renderables = caster.GetLastShadowVolumeRenderableEnumerator();
					}

					while ( renderables.MoveNext() )
					{
						ShadowRenderable sr = (ShadowRenderable)renderables.Current;

						// omit hidden renderables
						if ( sr.IsVisible )
						{
							// render volume, including dark and (maybe) light caps
							this.RenderSingleShadowVolumeToStencil( sr,
																	zfailAlgo,
																	stencil2sided,
																	tmpLightList,
																	( i > 0 ) );

							// optionally render separate light cap
							if ( sr.IsLightCapSeperate
								 && ( ( flags & (int)ShadowRenderableFlags.IncludeLightCap ) ) > 0 )
							{
								// must always fail depth check
								this.targetRenderSystem.DepthBufferFunction = CompareFunction.AlwaysFail;

								Debug.Assert( sr.LightCapRenderable != null,
											  "Shadow renderable is missing a separate light cap renderable!" );

								this.RenderSingleShadowVolumeToStencil( sr.LightCapRenderable,
																		zfailAlgo,
																		stencil2sided,
																		tmpLightList,
																		( i > 0 ) );
								// reset depth function
                                this.targetRenderSystem.DepthBufferFunction = CompareFunction.Less;
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
				this.targetRenderSystem.CullingMode = twoSided ? CullingMode.None : CullingMode.CounterClockwise;
				this.targetRenderSystem.SetStencilBufferParams(
					CompareFunction.AlwaysPass,
					// always pass stencil check
					0,
					// no ref value (no compare)
					unchecked( (int)0xffffffff ),
					// no mask
					StencilOperation.Keep,
					// stencil test will never fail
					zfail
						? ( twoSided ? StencilOperation.IncrementWrap : StencilOperation.Increment )
						: StencilOperation.Keep,
					// back face depth fail
					zfail
						? StencilOperation.Keep
						: ( twoSided ? StencilOperation.DecrementWrap : StencilOperation.Decrement ),
					// back face pass
					twoSided );
			}
			else
			{
				this.targetRenderSystem.CullingMode = twoSided ? CullingMode.None : CullingMode.Clockwise;
				this.targetRenderSystem.SetStencilBufferParams(
					CompareFunction.AlwaysPass,
					// always pass stencil check
					0,
					// no ref value (no compare)
					unchecked( (int)0xffffffff ),
					// no mask
					StencilOperation.Keep,
					// stencil test will never fail
					zfail
						? ( twoSided ? StencilOperation.DecrementWrap : StencilOperation.Decrement )
						: StencilOperation.Keep,
					// front face depth fail
					zfail
						? StencilOperation.Keep
						: ( twoSided ? StencilOperation.IncrementWrap : StencilOperation.Increment ),
					// front face pass
					twoSided );
			}
		}

		/// <summary>
		///		Render a single shadow volume to the stencil buffer.
		/// </summary>
		protected void RenderSingleShadowVolumeToStencil( ShadowRenderable sr,
														  bool zfail,
														  bool stencil2sided,
														  LightList manualLightList,
														  bool isSecondPass )
		{
			// Render a shadow volume here
			//  - if we have 2-sided stencil, one render with no culling
			//  - otherwise, 2 renders, one with each culling method and invert the ops
			if ( !isSecondPass )
			{
				this.SetShadowVolumeStencilState( false, zfail, stencil2sided );
				this.RenderSingleObject( sr, this.shadowStencilPass, false, manualLightList );
			}

			if ( !stencil2sided && isSecondPass )
			{
				// Second pass
				this.SetShadowVolumeStencilState( true, zfail, false );
				this.RenderSingleObject( sr, this.shadowStencilPass, false );
			}

			// Do we need to render a debug shadow marker?
			if ( this.showDebugShadows && ( isSecondPass || stencil2sided ) )
			{
				// reset stencil & colour ops
				this.targetRenderSystem.SetStencilBufferParams();
				this.SetPass( this.shadowDebugPass );
				this.RenderSingleObject( sr, this.shadowDebugPass, false, manualLightList );
				this.targetRenderSystem.SetColorBufferWriteEnabled( false, false, false, false );
			}
		}

		/// <summary>Internal method for setting up the renderstate for a rendering pass.</summary>
		/// <param name="pass">The Pass details to set.</param>
		/// <param name="evenIfSuppressed">
		///    Sets the pass details even if render state
		///    changes are suppressed; if you are using this to manually set state
		///    when render state changes are suppressed, you should set this to true.
		/// </param>
		/// <param name="shadowDerivation">
		///    If false, disables the derivation of shadow passes from original passes
		/// </param>
		/// <returns>
		///		A Pass object that was used instead of the one passed in, can
		///		happen when rendering shadow passes
		///	</returns>
		public virtual Pass SetPass( Pass pass, bool evenIfSuppressed, bool shadowDerivation )
		{
			if ( !this.suppressRenderStateChanges || evenIfSuppressed )
			{
				if ( this.illuminationStage == IlluminationRenderStage.RenderToTexture && shadowDerivation )
				{
					// Derive a special shadow caster pass from this one
					pass = this.DeriveShadowCasterPass( pass );
				}
				else if ( this.illuminationStage == IlluminationRenderStage.RenderReceiverPass )
				{
					pass = this.DeriveShadowReceiverPass( pass );
				}

				//TODO :autoParamDataSource.SetPass( pass );

				bool passSurfaceAndLightParams = true;

				if ( pass.HasVertexProgram )
				{
					this.targetRenderSystem.BindGpuProgram( pass.VertexProgram.BindingDelegate );
					// bind parameters later since they can be per-object
					// does the vertex program want surface and light params passed to rendersystem?
					passSurfaceAndLightParams = pass.VertexProgram.PassSurfaceAndLightStates;
				}
				else
				{
					// Unbind program?
					if ( this.targetRenderSystem.IsGpuProgramBound( GpuProgramType.Vertex ) )
					{
						this.targetRenderSystem.UnbindGpuProgram( GpuProgramType.Vertex );
					}
					// Set fixed-function vertex parameters
				}

				if ( passSurfaceAndLightParams )
				{
					// Set surface reflectance properties, only valid if lighting is enabled
					if ( pass.LightingEnabled )
					{
						this.targetRenderSystem.SetSurfaceParams( pass.Ambient,
																  pass.Diffuse,
																  pass.Specular,
																  pass.Emissive,
																  pass.Shininess,
																  pass.VertexColorTracking );
					}
					// #if NOT_IN_OGRE
					else
					{
						// even with lighting off, we need ambient set to white
						this.targetRenderSystem.SetSurfaceParams( ColorEx.White,
																  ColorEx.Black,
																  ColorEx.Black,
																  ColorEx.Black,
																  0,
																  TrackVertexColor.None );
					}
					// #endif
					// Dynamic lighting enabled?
					this.targetRenderSystem.LightingEnabled = pass.LightingEnabled;
				}

				// Using a fragment program?
				if ( pass.HasFragmentProgram )
				{
					this.targetRenderSystem.BindGpuProgram( pass.FragmentProgram.BindingDelegate );
					// bind parameters later since they can be per-object
				}
				else
				{
					// Unbind program?
					if ( this.targetRenderSystem.IsGpuProgramBound( GpuProgramType.Fragment ) )
					{
						this.targetRenderSystem.UnbindGpuProgram( GpuProgramType.Fragment );
					}
				}
				// Set fixed-function fragment settings

				//We need to set fog properties always. In D3D, it applies to shaders prior
				//to version vs_3_0 and ps_3_0. And in OGL, it applies to "ARB_fog_XXX" in
				//fragment program, and in other ways, they maybe accessed by gpu program via
				//"state.fog.XXX".

				// New fog params can either be from scene or from material

				// jsw - set the fog for both fixed function and fragment programs
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
					newFogMode = this.fogMode;
					newFogColor = this.fogColor;
					newFogDensity = this.fogDensity;
					newFogStart = this.fogStart;
					newFogEnd = this.fogEnd;
				}

				// set fog params
                /*
				float fogScale = 1f;
				if ( newFogMode == FogMode.None )
				{
					fogScale = 0f;
				}
                 */

				// set fog using the render system
				this.targetRenderSystem.SetFog( newFogMode, newFogColor, newFogDensity, newFogStart, newFogEnd );

				// Tell params about ORIGINAL fog
				// Need to be able to override fixed function fog, but still have
				// original fog parameters available to a shader that chooses to use
				// TODO: autoParamDataSource.SetFog( fogMode, fogColor, fogDensity, fogStart, fogEnd );

				// The rest of the settings are the same no matter whether we use programs or not

				// Set scene blending
				this.targetRenderSystem.SetSceneBlending( pass.SourceBlendFactor, pass.DestinationBlendFactor );

				// TODO : Set point parameters
				//targetRenderSystem.SetPointParameters(
				//                                        pass.PointSize,
				//                                        pass.IsPointAttenuationEnabled,
				//                                        pass.PointAttenuationConstant,
				//                                        pass.PointAttenuationLinear,
				//                                        pass.PointAttenuationQuadratic,
				//                                        pass.PointMinSize,
				//                                        pass.PointMaxSize
				//                                        );

				//targetRenderSystem.PointSpritesEnabled = pass.PointSpritesEnabled;

				// TODO : Reset the shadow texture index for each pass
				//foreach ( TextureUnitState textureUnit in pass.TextureUnitStates )
				//{
				//}

				// set all required texture units for this pass, and disable ones not being used
				int numTextureUnits = this.targetRenderSystem.Capabilities.TextureUnitCount;
				if ( pass.HasFragmentProgram  && pass.FragmentProgram.IsSupported )
				{
                    // Axiom: This effectivley breaks GLSL.
                    // besides this routine aint existing (anymore?) in 1.7.2
                    // an upgrade of the scenemanager is recommended
					//numTextureUnits = pass.FragmentProgram.SamplerCount;
				}
				else if ( Config.MaxTextureLayers < this.targetRenderSystem.Capabilities.TextureUnitCount )
				{
					numTextureUnits = Config.MaxTextureLayers;
				}

				for ( int i = 0; i < numTextureUnits; i++ )
				{
					if ( i < pass.TextureUnitStageCount )
					{
						TextureUnitState texUnit = pass.GetTextureUnitState( i );
					    targetRenderSystem.SetTextureUnitSettings( i, texUnit );
						//this.targetRenderSystem.SetTextureUnit( i, texUnit, !pass.HasFragmentProgram );
					}
					else
					{
						// disable this unit
						if ( !pass.HasFragmentProgram )
						{
							this.targetRenderSystem.DisableTextureUnit( i );
						}
					}
				}

                // Disable remaining texture units
                targetRenderSystem.DisableTextureUnitsFrom(pass.TextureUnitStageCount);

				// Depth Settings
				this.targetRenderSystem.DepthBufferWriteEnabled = pass.DepthWrite;
				this.targetRenderSystem.DepthBufferCheckEnabled = pass.DepthCheck;
				this.targetRenderSystem.DepthBufferFunction = pass.DepthFunction;
				this.targetRenderSystem.SetDepthBias(pass.DepthBiasConstant);

				// Aplha Reject Settings
				this.targetRenderSystem.SetAlphaRejectSettings( pass.AlphaRejectFunction, (byte)pass.AlphaRejectValue, pass.IsAlphaToCoverageEnabled );

				// Color Write
				// right now only using on/off, not per channel
				bool colWrite = pass.ColorWriteEnabled;
				this.targetRenderSystem.SetColorBufferWriteEnabled( colWrite, colWrite, colWrite, colWrite );

				// Culling Mode
				this.targetRenderSystem.CullingMode = pass.CullingMode;

				// Shading mode
                //this.targetRenderSystem.ShadingMode = pass.ShadingMode;

				// Polygon Mode
				this.targetRenderSystem.PolygonMode = pass.PolygonMode;

				// set pass number
				this.autoParamDataSource.PassNumber = pass.Index;
			}

			return pass;
		}

		/// <summary>
		///		If only the first parameter is supplied
		/// </summary>
		public virtual Pass SetPass( Pass pass )
		{
			return this.SetPass( pass, false, true );
		}

		/// <summary>
		///		If only the first two parameters are supplied
		/// </summary>
		public virtual Pass SetPass( Pass pass, bool evenIfSuppressed )
		{
			return this.SetPass( pass, evenIfSuppressed, true );
		}

		/// <summary>
		/// 	Utility method for creating the planes of a skybox.
		/// </summary>
		/// <param name="plane"></param>
		/// <param name="distance"></param>
		/// <param name="orientation"></param>
		/// <param name="groupName"></param>
		/// <returns></returns>
		protected Mesh CreateSkyboxPlane( BoxPlane plane, float distance, Quaternion orientation, string groupName )
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
			Mesh planeModel = (Mesh)modelMgr[ meshName ];

			// trash it if it already exists
			if ( planeModel != null )
			{
				modelMgr.Unload( planeModel );
			}

			float planeSize = distance * 2;

			// create and return the plane mesh
			return modelMgr.CreatePlane( meshName, groupName, p, planeSize, planeSize, 1, 1, false, 1, 1, 1, up );
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="plane"></param>
		/// <param name="curvature"></param>
		/// <param name="tiling"></param>
		/// <param name="distance"></param>
		/// <param name="orientation"></param>
		/// <param name="groupName"></param>
		/// <returns></returns>
		protected Mesh CreateSkyDomePlane( BoxPlane plane,
										   float curvature,
										   float tiling,
										   float distance,
										   Quaternion orientation,
										   string groupName )
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
			Mesh planeMesh = (Mesh)meshManager[ meshName ];

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
					meshName,
					groupName,
					p,
					planeSize,
					planeSize,
					curvature,
					segments,
					segments,
					false,
					1,
					tiling,
					tiling,
					up,
					orientation,
					BufferUsage.DynamicWriteOnly,
					BufferUsage.StaticWriteOnly,
					true,
					true );

			return planeMesh;
		}

		/// <summary>
		///		Protected method used by RenderVisibleObjects to deal with renderables
		///		which override the camera's own view / projection materices.
		/// </summary>
		/// <param name="renderable"></param>
		protected void UseRenderableViewProjection( IRenderable renderable )
		{
			// View
			bool useIdentityView = renderable.UseIdentityView;
			if ( useIdentityView )
			{
				// Using identity view now, change it
				this.targetRenderSystem.ViewMatrix = Matrix4.Identity;
				this.lastViewWasIdentity = true;
			}

			// Projection
			bool useIdentityProj = renderable.UseIdentityProjection;
			if ( useIdentityProj )
			{
				// Use identity projection matrix, still need to take RS depth into account
			    Matrix4 mat;
                targetRenderSystem.ConvertProjectionMatrix(Matrix4.Identity, out mat);
				targetRenderSystem.ProjectionMatrix = mat;
				lastProjectionWasIdentity = true;
			}
		}

		/// <summary>
		///		Used to first the QueueStarted event.
		/// </summary>
		/// <returns>True if the queue should be skipped.</returns>
		protected virtual bool OnRenderQueueStarted( RenderQueueGroupID group, string invocation )
		{
			BeginRenderQueueEventArgs e = new BeginRenderQueueEventArgs();
			e.RenderQueueId = group;
			e.Invocation = invocation;

			bool skip = false;
			this._queueStartedEvent.Fire( this, e, ( args ) =>
			{
				skip |= args.SkipInvocation;
				return true;
			} );
			return skip;
		}

		/// <summary>
		///		Used to first the QueueEnded event.
		/// </summary>
		/// <returns>True if the queue should be repeated.</returns>
		protected virtual bool OnRenderQueueEnded( RenderQueueGroupID group, string invocation )
		{
			EndRenderQueueEventArgs e = new EndRenderQueueEventArgs();
			e.RenderQueueId = group;
			e.Invocation = invocation;

			bool repeat = false;
			this._queueEndedEvent.Fire( this, e, ( args ) =>
			{
				repeat |= args.RepeatInvocation;
				return true;
			} );
			return repeat;
		}

		#endregion Protected methods

		#region Public methods

		#region RibbonTrail Management

		public virtual RibbonTrail CreateRibbonTrail( string name )
		{
			return (RibbonTrail)this.CreateMovableObject( name, RibbonTrailFactory.TypeName, null );
		}

		public virtual RibbonTrail GetRibbonTrail( string name )
		{
			return (RibbonTrail)this.GetMovableObject( name, RibbonTrailFactory.TypeName );
		}

		public virtual void RemoveAllRibonTrails()
		{
			this.DestroyAllMovableObjectsByType( RibbonTrailFactory.TypeName );
		}

		public virtual void RemoveRibbonTrail( RibbonTrail ribbonTrail )
		{
			this.DestroyMovableObject( ribbonTrail );
		}

		public virtual void RemoveRibbonTrail( string name )
		{
			this.DestroyMovableObject( name, RibbonTrailFactory.TypeName );
		}

		#endregion RibbonTrail Management

		public void RekeySceneNode( string oldName, SceneNode node )
		{
			if ( this.sceneNodeList[ oldName ] == node )
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
			return this.CreateAABBRegionQuery( new AxisAlignedBox(), 0xffffffff );
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
			return this.CreateAABBRegionQuery( box, 0xffffffff );
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
		public virtual AxisAlignedBoxRegionSceneQuery CreateAABBRegionQuery( AxisAlignedBox box, uint mask )
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
		public virtual RaySceneQuery CreateRayQuery()
		{
			return this.CreateRayQuery( new Ray(), 0xffffffff );
		}

		/// <summary>
		///    Creates a query to return objects found along the ray.
		/// </summary>
		/// <param name="ray">Ray to use for the intersection query.</param>
		/// <returns>A specialized implementation of RaySceneQuery for this scene manager.</returns>
		public virtual RaySceneQuery CreateRayQuery( Ray ray )
		{
			return this.CreateRayQuery( ray, 0xffffffff );
		}

	    /// <summary>
	    ///    Creates a query to return objects found along the ray.
	    /// </summary>
	    /// <param name="ray">Ray to use for the intersection query.</param>
	    /// <param name="mask"></param>
	    /// <returns>A specialized implementation of RaySceneQuery for this scene manager.</returns>
	    public virtual RaySceneQuery CreateRayQuery( Ray ray, uint mask )
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
			return this.CreateSphereRegionQuery( new Sphere(), 0xffffffff );
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
			return this.CreateSphereRegionQuery( sphere, 0xffffffff );
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
		public virtual SphereRegionSceneQuery CreateSphereRegionQuery( Sphere sphere, uint mask )
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
			return this.CreatePlaneBoundedVolumeQuery( new PlaneBoundedVolumeList(), 0xffffffff );
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
			return this.CreatePlaneBoundedVolumeQuery( volumes, 0xffffffff );
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
		public virtual PlaneBoundedVolumeListSceneQuery CreatePlaneBoundedVolumeQuery( PlaneBoundedVolumeList volumes,
																					   uint mask )
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
			return this.CreateIntersectionQuery( 0xffffffff );
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
		public virtual IntersectionSceneQuery CreateIntersectionQuery( uint mask )
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
			if ( this.cameraList != null )
			{
				// notify the render system of each camera being removed
				foreach ( Camera cam in this.cameraList.Values )
				{
					this.targetRenderSystem.NotifyCameraRemoved( cam );

					if ( !cam.IsDisposed )
						cam.Dispose();
				}

				// clear the list
				this.cameraList.Clear();
			}
		}

		/// <summary>
		///		Removes all lights from the scene.
		/// </summary>
		public virtual void RemoveAllLights()
		{
			this.DestroyAllMovableObjectsByType( LightFactory.TypeName );
		}

		/// <summary>
		///		Removes all entities from the scene.
		/// </summary>
		public virtual void RemoveAllEntities()
		{
			this.DestroyAllMovableObjectsByType( EntityFactory.TypeName );
		}

		/// <summary>
		///		Removes all billboardsets from the scene.
		/// </summary>
		public virtual void RemoveAllBillboardSets()
		{
			this.DestroyAllMovableObjectsByType( BillboardSetFactory.TypeName );
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
			cameraList.Remove( camera.Name );

			// notify all render targets
			this.targetRenderSystem.NotifyCameraRemoved( camera );
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
			Debug.Assert( this.cameraList.ContainsKey( name ),
						  string.Format( "Camera '{0}' does not exist in the scene.", name ) );

			this.RemoveCamera( this.cameraList[ name ] );
		}

		/// <summary>
		///		Removes the specified light from the scene.
		/// </summary>
		/// <remarks>
		///		This method removes a previously added light from the scene.
		/// </remarks>
        /// <param name="light">Reference to the light to remove.</param>
		public virtual void RemoveLight( Light light )
		{
			this.DestroyMovableObject( light );
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
			this.DestroyMovableObject( name, LightFactory.TypeName );
		}

		/// <summary>
		///		Removes the specified BillboardSet from the scene.
		/// </summary>
		/// <remarks>
		///		This method removes a previously added BillboardSet from the scene.
		/// </remarks>
        /// <param name="billboardSet">Reference to the BillboardSet to remove.</param>
		public virtual void RemoveBillboardSet( BillboardSet billboardSet )
		{
			this.DestroyMovableObject( billboardSet );
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
			this.DestroyMovableObject( name, BillboardSetFactory.TypeName );
		}

		/// <summary>
		///    Removes the specified entity from the scene.
		/// </summary>
		/// <param name="entity">Entity to remove from the scene.</param>
		public virtual void RemoveEntity( Entity entity )
		{
			this.DestroyMovableObject( entity );
		}

		/// <summary>
		///    Removes the entity with the specified name from the scene.
		/// </summary>
		/// <param name="name">Entity to remove from the scene.</param>
		public virtual void RemoveEntity( string name )
		{
			this.DestroyMovableObject( name, EntityFactory.TypeName );
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
		public virtual void SetFog( FogMode mode, ColorEx color, float density, float linearStart, float linearEnd )
		{
			// set all the fog information
			this.fogMode = mode;
			this.fogColor = color;
			this.fogDensity = density;
			this.fogStart = linearStart;
			this.fogEnd = linearEnd;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="mode"></param>
		/// <param name="color"></param>
		/// <param name="density"></param>
		public virtual void SetFog( FogMode mode, ColorEx color, float density )
		{
			// set all the fog information
			this.fogMode = mode;
			this.fogColor = color;
			this.fogDensity = density;
			this.fogStart = 0.0f;
			this.fogEnd = 1.0f;
		}

		/// <summary>
		///		Overloaded method.
		/// </summary>
		/// <param name="enable"></param>
		/// <param name="materialName"></param>
		/// <param name="distance"></param>
		public void SetSkyBox( bool enable, string materialName, float distance )
		{
			this.SetSkyBox( enable,
							materialName,
							distance,
							true,
							Quaternion.Identity,
							ResourceGroupManager.DefaultResourceGroupName );
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
	    ///<param name="groupName"></param>
	    public void SetSkyBox( bool enable,
							   string materialName,
							   float distance,
							   bool drawFirst,
							   Quaternion orientation,
							   string groupName )
		{
			// enable the skybox?
			this.isSkyBoxEnabled = enable;

			if ( enable )
			{
				Material m = (Material)MaterialManager.Instance[ materialName ];

				if ( m == null )
				{
					this.isSkyBoxEnabled = false;
					throw new AxiomException( string.Format( "Could not find skybox material '{0}'", materialName ) );
				}
				// Make sure the material doesn't update the depth buffer
				m.DepthWrite = false;
				// Ensure loaded
				m.Load();

				// ensure texture clamping to reduce fuzzy edges when using filtering
                m.GetTechnique( 0 ).GetPass( 0 ).GetTextureUnitState( 0 ).SetTextureAddressingMode( TextureAddressing.Clamp );

				this.isSkyBoxDrawnFirst = drawFirst;

				if ( this.skyBoxNode == null )
				{
					this.skyBoxNode = this.CreateSceneNode( "SkyBoxNode" );
				}
				else
				{
					this.skyBoxNode.DetachAllObjects();
				}

				// need to create 6 plane entities for each side of the skybox
				for ( int i = 0; i < 6; i++ )
				{
					Mesh planeModel = this.CreateSkyboxPlane( (BoxPlane)i, distance, orientation, groupName );
					string entityName = "SkyBoxPlane" + i;

					if ( this.skyBoxEntities[ i ] != null )
					{
						this.RemoveEntity( this.skyBoxEntities[ i ] );
					}

					// create an entity for this plane
					this.skyBoxEntities[ i ] = CreateEntity( entityName, planeModel.Name );

					// skyboxes need not cast shadows
					this.skyBoxEntities[ i ].CastShadows = false;

					// Have to create 6 materials, one for each frame
					// Used to use combined material but now we're using queue we can't split to change frame
					// This doesn't use much memory because textures aren't duplicated
					Material boxMaterial = (Material)MaterialManager.Instance[ entityName ];

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

					this.skyBoxEntities[ i ].MaterialName = boxMaterial.Name;

					// Attach to node
					this.skyBoxNode.AttachObject( this.skyBoxEntities[ i ] );
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
			this.SetSkyDome( isEnabled,
							 materialName,
							 curvature,
							 tiling,
							 4000,
							 true,
							 Quaternion.Identity,
							 ResourceGroupManager.DefaultResourceGroupName );
		}

		/// <summary>
		/// </summary>
		public void SetSkyDome( bool isEnabled,
								string materialName,
								float curvature,
								float tiling,
								float distance,
								bool drawFirst,
								Quaternion orientation,
								string groupName )
		{
			this.isSkyDomeEnabled = isEnabled;
			if ( isEnabled )
			{
				Material material = (Material)MaterialManager.Instance[ materialName ];

				if ( material == null )
				{
					throw new AxiomException( string.Format( "Could not find skydome material '{0}'", materialName ) );
				}

				// make sure the material doesn't update the depth buffer
				material.DepthWrite = false;
				// ensure loading
				material.Load();

				this.isSkyDomeDrawnFirst = drawFirst;

				// create node
				if ( this.skyDomeNode == null )
				{
					this.skyDomeNode = this.CreateSceneNode( "SkyDomeNode" );
				}
				else
				{
					this.skyDomeNode.DetachAllObjects();
				}

				// set up the dome (5 planes)
				for ( int i = 0; i < 5; ++i )
				{
					Mesh planeMesh = this.CreateSkyDomePlane( (BoxPlane)i, curvature, tiling, distance, orientation, groupName );
					string entityName = String.Format( "SkyDomePlane{0}", i );

					// create entity
					if ( this.skyDomeEntities[ i ] != null )
					{
						this.RemoveEntity( this.skyDomeEntities[ i ] );
					}

					this.skyDomeEntities[ i ] = CreateEntity( entityName, planeMesh.Name );
					this.skyDomeEntities[ i ].MaterialName = material.Name;
					// Sky entities need not cast shadows
					this.skyDomeEntities[ i ].CastShadows = false;

					// attach to node
					this.skyDomeNode.AttachObject( this.skyDomeEntities[ i ] );
				} // for each plane
			}
		}

		public virtual void SetShadowTextureSettings( ushort size, ushort count )
		{
			this.SetShadowTextureSettings( size, count, this.shadowTextureFormat );
		}

		/// <summary>
		///		Sets the size and count of textures used in texture-based shadows.
		/// </summary>
		/// <remarks>
		///		See ShadowTextureSize and ShadowTextureCount for details, this
		///		method just allows you to change both at once, which can save on
		///		reallocation if the textures have already been created.
		/// </remarks>
		public virtual void SetShadowTextureSettings( ushort size, ushort count, PixelFormat format )
		{
			if ( this.shadowTextures.Count > 0 &&
				 ( count != this.shadowTextureCount ||
				   size != this.shadowTextureSize ||
				   format != this.shadowTextureFormat ) )
			{
				// recreate
				this.CreateShadowTextures( size, count, format );
			}
			this.shadowTextureCount = count;
			this.shadowTextureSize = size;
			this.shadowTextureFormat = format;
		}

		protected virtual void EnsureShadowTexturesCreated()
		{
			if ( shadowTextureConfigDirty )
			{
				DestroyShadowTextures();
				ShadowTextureManager.Instance.GetShadowTextures( shadowTextureConfigList, shadowTextures );

				// clear shadow cam - light mapping
				shadowCameraLightMapping.Clear();

				// Recreate shadow textures
				foreach ( Texture shadowTexture in shadowTextures )
				{
					// Camera names are local to SM
					String camName = shadowTexture.Name + "Cam";
					// Material names are global to SM, make specific
					String matName = shadowTexture.Name + "Mat" + this.Name;

					RenderTexture shadowRTT = shadowTexture.GetBuffer().GetRenderTarget();

					// Create camera for this texture, but note that we have to rebind
					// in PrepareShadowTextures to coexist with multiple SMs
					Camera cam = CreateCamera( camName );
					cam.AspectRatio = shadowTexture.Width / (Real)shadowTexture.Height;
					shadowTextureCameras.Add( cam );

					// Create a viewport, if not there already
					if ( shadowRTT.NumViewports == 0 )
					{
						// Note camera assignment is transient when multiple SMs
						Viewport v = shadowRTT.AddViewport( cam );
						v.SetClearEveryFrame(true);
						// remove overlays
						v.ShowOverlays = false;
					}

					// Don't update automatically - we'll do it when required
					shadowRTT.IsAutoUpdated = false;

					// Also create corresponding Material used for rendering this shadow
					Material mat = (Material)MaterialManager.Instance[ matName ];
					if ( mat == null )
					{
						mat = (Material)MaterialManager.Instance.Create( matName, ResourceGroupManager.InternalResourceGroupName );
					}
					Pass p = mat.GetTechnique( 0 ).GetPass( 0 );
					if ( p.TextureUnitStageCount != 1 /* ||
						 p.GetTextureUnitState( 0 ).GetTexture( 0 ) != shadowTexture */ )
					{
						mat.GetTechnique( 0 ).GetPass( 0 ).RemoveAllTextureUnitStates();
						// create texture unit referring to render target texture
						TextureUnitState texUnit = p.CreateTextureUnitState( shadowTexture.Name );
						// set projective based on camera
						texUnit.SetProjectiveTexturing( !p.HasVertexProgram, cam );
						// clamp to border colour
                        texUnit.SetTextureAddressingMode( TextureAddressing.Border );
						texUnit.TextureBorderColor = ColorEx.White;
						mat.Touch();
					}

					// insert dummy camera-light combination
					shadowCameraLightMapping.Add( cam, null );

					// Get null shadow texture
					nullShadowTexture = shadowTextureConfigList.Count == 0 ?
										null :
										ShadowTextureManager.Instance.GetNullShadowTexture( shadowTextureConfigList[ 0 ].format );
				}
				shadowTextureConfigDirty = false;
			}
		}

		#endregion Public methods

		#region Properties

		/// <summary>
		/// Sets the active compositor chain of the current scene being rendered.
		/// </summary>
		/// <note>
		/// CompositorChain does this automatically, no need to call manually.
		/// </note>
		public CompositorChain ActiveCompositorChain
		{
			get
			{
				return _activeCompositorChain;
			}
			set
			{
				_activeCompositorChain = value;
			}
		}

		/// <summary>
		/// Gets/Sets the target render system that this scene manager should be using.
		/// </summary>
		public RenderSystem TargetRenderSystem
		{
			get
			{
				return this.targetRenderSystem;
			}
			set
			{
				this.targetRenderSystem = value;
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
				return this.rootSceneNode;
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
				return this.ambientColor;
			}
			set
			{
				if ( value == null )
				{
					throw new ArgumentException( "Cannot set the scene ambient light color to null" );
				}
				//it will cause the GpuProgramParameters.SetConstant() color overload to throw a null reference exception otherwise
				this.ambientColor = value;
				// change ambient color of current render system
				this.targetRenderSystem.AmbientLight = this.ambientColor;
			}
		}

		/// <summary>
		///		Method for setting a specific option of the Scene Manager. These options are usually
		///		specific for a certain implementation of the Scene Manager class, and may (and probably
		///		will) not exist across different implementations.
		/// </summary>
		public AxiomCollection<object> Options
		{
			get
			{
				return this.optionList;
			}
		}

		/// <summary>
		/// the default shadow camera setup used for all lights which don't have
		/// their own shadow camera setup.
		/// </summary>
		private IShadowCameraSetup _defaultShadowCameraSetup;

		/// <summary>
		/// Get/Set the shadow camera setup in use for all lights which don't have
		/// their own shadow camera setup.
		/// </summary>
		public IShadowCameraSetup ShadowCameraSetup
		{
			get
			{
				return _defaultShadowCameraSetup;
			}
			set
			{
				_defaultShadowCameraSetup = value;
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
				return this.shadowColor;
			}
			set
			{
				this.shadowColor = value;

				if ( this.shadowModulativePass == null && this.shadowCasterPlainBlackPass == null )
				{
					this.InitShadowVolumeMaterials();
				}

				this.shadowModulativePass.GetTextureUnitState( 0 ).SetColorOperationEx(
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
				return this.shadowDirLightExtrudeDist;
			}
			set
			{
				this.shadowDirLightExtrudeDist = value;
			}
		}

		/// <summary>
		/// Gets the proportional distance which a texture shadow which is generated from a
		/// directional light will be offset into the camera view to make best use of texture space.
		/// </summary>
		public Real ShadowDirectionalLightTextureOffset
		{
			get
			{
				return this.shadowTextureOffset;
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
		public Real ShadowFarDistance
		{
			get
			{
				return this.shadowFarDistance;
			}
			set
			{
				this.shadowFarDistance = value;
				this.shadowFarDistanceSquared = this.shadowFarDistance * this.shadowFarDistance;
			}
		}

		public Real ShadowFarDistanceSquared
		{
			get
			{
				return this.shadowFarDistance;
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
				return this.shadowIndexBufferSize;
			}
			set
			{
				if ( this.shadowIndexBuffer != null || value != this.shadowIndexBufferSize )
				{
					// create an shadow index buffer
					this.shadowIndexBuffer =
						HardwareBufferManager.Instance.CreateIndexBuffer(
							IndexType.Size16,
							this.shadowIndexBufferSize,
							BufferUsage.DynamicWriteOnly,
							false );
				}

				this.shadowIndexBufferSize = value;
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
				return this.shadowTextureSize;
			}
			set
			{
				// possibly recreate
				this.CreateShadowTextures( value, this.shadowTextureCount, this.shadowTextureFormat );
				this.shadowTextureSize = value;
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
				return this.shadowTextureCount;
			}
			set
			{
				// possibly recreate
				this.CreateShadowTextures( this.shadowTextureSize, value, this.shadowTextureFormat );
				this.shadowTextureCount = value;
			}
		}

		public PixelFormat ShadowTextureFormat
		{
			get
			{
				return this.shadowTextureFormat;
			}
			set
			{
				// possibly recreate
				this.CreateShadowTextures( this.shadowTextureSize, this.shadowTextureCount, value );
				this.shadowTextureFormat = value;
			}
		}

		/// <summary>
		///		Determines whether we're supporting self-shadowing
		/// </summary>
		/// <remarks>
		///		The default number of textures assigned to deal with texture based
		///		shadows is 1; however this means you can only have one light casting
		///		shadows at the same time. You can increase this number in order to
		///		make this more flexible, but be aware of the texture memory it will use.
		///	</remarks>
		public bool ShadowTextureSelfShadow
		{
			get
			{
				return this.shadowTextureSelfShadow;
			}
			set
			{
				this.shadowTextureSelfShadow = value;
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
				return this.shadowTechnique;
			}
			set
			{
				this.shadowTechnique = value;

				// do initial setup for stencil shadows if needed
				if ( this.IsShadowTechniqueStencilBased )
				{
					// Firstly check that we have a stencil. Otherwise, forget it!
					if ( !this.targetRenderSystem.Capabilities.HasCapability( Capabilities.StencilBuffer ) )
					{
						LogManager.Instance.Write(
							"WARNING: Stencil shadows were requested, but the current hardware does not support them.  Disabling." );

						this.shadowTechnique = ShadowTechnique.None;
					}
					else if ( this.shadowIndexBuffer == null )
					{
						// create an shadow index buffer
						this.shadowIndexBuffer =
							HardwareBufferManager.Instance.CreateIndexBuffer(
								IndexType.Size16,
								this.shadowIndexBufferSize,
								BufferUsage.DynamicWriteOnly,
								false );

						// tell all the meshes to prepare shadow volumes
						MeshManager.Instance.PrepareAllMeshesForShadowVolumes = true;
					}
				}

				// If Additive stencil, we need to split everything by illumination stage
				this.GetRenderQueue().SplitPassesByLightingType =
					( this.shadowTechnique == ShadowTechnique.StencilAdditive );

				// If any type of shadowing is used, tell render queue to split off non-shadowable materials
				this.GetRenderQueue().SplitNoShadowPasses = this.IsShadowTechniqueInUse;

				// create new textures for texture based shadows
				if ( this.IsShadowTechniqueTextureBased )
				{
					this.CreateShadowTextures( this.shadowTextureSize, this.shadowTextureCount, this.shadowTextureFormat );
				}
			}
		}

		public string ShadowTextureCasterMaterial
		{
			get
			{
				return this.shadowTextureCasterMaterial;
			}
			set
			{
				// When rendering with a material that includes its own custom shadow caster
				// vertex program, the code that sets up the pass will replace the vertex program
				// in this material with the one from the object's own material.
				// We need to set it back before we switch materials, in case we need to use this
				// material again.
				if ( this.shadowTextureCustomCasterPass != null )
				{
					if ( this.shadowTextureCustomCasterPass.VertexProgramName !=
						 this.shadowTextureCustomCasterVertexProgram )
					{
						this.shadowTextureCustomCasterPass.SetVertexProgram( this.shadowTextureCustomCasterVertexProgram );
						if ( this.shadowTextureCustomCasterPass.HasVertexProgram )
						{
							this.shadowTextureCustomCasterPass.VertexProgramParameters =
								this.shadowTextureCustomCasterVPParams;
						}
					}
				}

				this.shadowTextureCasterMaterial = value;
				if ( value == "" )
				{
					this.shadowTextureCustomCasterPass = null;
					this.shadowTextureCustomCasterFragmentProgram = "";
					this.shadowTextureCustomCasterVertexProgram = "";
				}
				else
				{
					Material material = (Material)MaterialManager.Instance[ value ];
					if ( material == null )
					{
						LogManager.Instance.Write(
							"Cannot use material '{0}' as the ShadowTextureCasterMaterial because the material doesn't exist.",
							value );
					}
					else
					{
						material.Load();
						this.shadowTextureCustomCasterPass = material.GetBestTechnique().GetPass( 0 );
						if ( this.shadowTextureCustomCasterPass.HasVertexProgram )
						{
							// Save vertex program and params in case we have to swap them out
							this.shadowTextureCustomCasterVertexProgram =
								this.shadowTextureCustomCasterPass.VertexProgramName;
							this.shadowTextureCustomCasterVPParams =
								this.shadowTextureCustomCasterPass.VertexProgramParameters;
						}
						else
						{
							this.shadowTextureCustomCasterVertexProgram = "";
						}
						if ( this.shadowTextureCustomCasterPass.HasFragmentProgram )
						{
							// Save fragment program and params in case we have to swap them out
							this.shadowTextureCustomCasterFragmentProgram =
								this.shadowTextureCustomCasterPass.FragmentProgramName;
							this.shadowTextureCustomCasterFPParams =
								this.shadowTextureCustomCasterPass.FragmentProgramParameters;
						}
						else
						{
							this.shadowTextureCustomCasterFragmentProgram = "";
						}
					}
				}
			}
		}

		public string ShadowTextureReceiverMaterial
		{
			get
			{
				return this.shadowTextureReceiverMaterial;
			}
			set
			{
				// When rendering with a material that includes its own custom shadow receiver
				// vertex program, the code that sets up the pass will replace the vertex program
				// in this material with the one from the object's own material.
				// We need to set it back before we switch materials, in case we need to use this
				// material again.
				if ( this.shadowTextureCustomReceiverPass != null )
				{
					if ( this.shadowTextureCustomReceiverPass.VertexProgramName !=
						 this.shadowTextureCustomReceiverVertexProgram )
					{
						this.shadowTextureCustomReceiverPass.SetVertexProgram(
							this.shadowTextureCustomReceiverVertexProgram );
						if ( this.shadowTextureCustomReceiverPass.HasVertexProgram )
						{
							this.shadowTextureCustomReceiverPass.VertexProgramParameters =
								this.shadowTextureCustomReceiverVPParams;
						}
					}
				}

				this.shadowTextureReceiverMaterial = value;
				if ( value == "" )
				{
					this.shadowTextureCustomReceiverPass = null;
					this.shadowTextureCustomReceiverVertexProgram = "";
					this.shadowTextureCustomReceiverFragmentProgram = "";
				}
				else
				{
					Material material = (Material)MaterialManager.Instance[ value ];
					if ( material == null )
					{
						LogManager.Instance.Write(
							"Cannot use material '{0}' as the ShadowTextureReceiverMaterial because the material doesn't exist.",
							value );
					}
					else
					{
						material.Load();
						this.shadowTextureCustomReceiverPass = material.GetBestTechnique().GetPass( 0 );
						if ( this.shadowTextureCustomReceiverPass.HasVertexProgram )
						{
							// Save vertex program and params in case we have to swap them out
							this.shadowTextureCustomReceiverVertexProgram =
								this.shadowTextureCustomReceiverPass.VertexProgramName;
							this.shadowTextureCustomReceiverVPParams =
								this.shadowTextureCustomReceiverPass.VertexProgramParameters;
						}
						else
						{
							this.shadowTextureCustomReceiverVertexProgram = "";
						}
						if ( this.shadowTextureCustomReceiverPass.HasFragmentProgram )
						{
							// Save fragment program and params in case we have to swap them out
							this.shadowTextureCustomReceiverFragmentProgram =
								this.shadowTextureCustomReceiverPass.FragmentProgramName;
							this.shadowTextureCustomReceiverFPParams =
								this.shadowTextureCustomReceiverPass.FragmentProgramParameters;
						}
						else
						{
							this.shadowTextureCustomReceiverFragmentProgram = "";
						}
					}
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
		///		infinite projection</LI>
		///		</UL>
		///		Therefore in the RenderSystem implementation, we may veto the use
		///		of an infinite far plane based on these heuristics.
		/// </remarks>
		public bool ShadowUseInfiniteFarPlane
		{
			get
			{
				return this.shadowUseInfiniteFarPlane;
			}
			set
			{
				this.shadowUseInfiniteFarPlane = value;
			}
		}

		public bool SuppressRenderStateChanges
		{
			get
			{
				return this.suppressRenderStateChanges;
			}
			set
			{
				this.suppressRenderStateChanges = value;
			}
		}

		/// <summary>
		///		Gets/Sets a value that forces all nodes to render their bounding boxes.
		/// </summary>
		public bool ShowBoundingBoxes
		{
			get
			{
				return this.showBoundingBoxes;
			}
			set
			{
				this.showBoundingBoxes = value;
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
				return this.showDebugShadows;
			}
			set
			{
				this.showDebugShadows = value;
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
				return this.displayNodes;
			}
			set
			{
				this.displayNodes = value;
			}
		}

		/// <summary>
		///		Gets the fog mode that was set during the last call to SetFog.
		/// </summary>
		public FogMode FogMode
		{
			get
			{
				return this.fogMode;
			}
			set
			{
				this.fogMode = value;
			}
		}

		/// <summary>
		///		Gets the fog starting point that was set during the last call to SetFog.
		/// </summary>
		public float FogStart
		{
			get
			{
				return this.fogStart;
			}
			set
			{
				this.fogStart = value;
			}
		}

		/// <summary>
		///		Gets the fog ending point that was set during the last call to SetFog.
		/// </summary>
		public float FogEnd
		{
			get
			{
				return this.fogEnd;
			}
			set
			{
				this.fogEnd = value;
			}
		}

		/// <summary>
		///		Gets the fog density that was set during the last call to SetFog.
		/// </summary>
		public float FogDensity
		{
			get
			{
				return this.fogDensity;
			}
			set
			{
				this.fogDensity = value;
			}
		}

		/// <summary>
		///		Gets the fog color that was set during the last call to SetFog.
		/// </summary>
		public virtual ColorEx FogColor
		{
			get
			{
				return this.fogColor;
			}
			set
			{
				this.fogColor = value;
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
		///     <li>No texture unit settings (&amp; hence no textures)</li>
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

		/// <summary>
		///		Gets the current viewport - - needed by compositor
		/// </summary>
		public Viewport CurrentViewport
		{
			get
			{
				return this.currentViewport;
			}
		}

		/// <summary>
		///		Gets and sets the object visibility mask
		/// </summary>
		public ulong VisibilityMask
		{
			get
			{
				return this.visibilityMask;
			}
			set
			{
				this.visibilityMask = value;
			}
		}

		/// <summary>
		///		Gets ths combined object visibility mask of this scenemanager and the current viewport
		/// </summary>
		public ulong CombinedVisibilityMask
		{
			get
			{
				return currentViewport != null ? currentViewport.VisibilityMask & this.visibilityMask : this.visibilityMask;
			}
		}

		/// <summary>
		///		Gets and sets the object visibility mask
		/// </summary>
		public bool FindVisibleObjectsBool
		{
			get
			{
				return this.findVisibleObjects;
			}
			set
			{
				this.findVisibleObjects = value;
			}
		}

		/// <summary>
		///		Gets the instance name of this SceneManager.
		/// </summary>
		public string Name
		{
			get
			{
				return this.name;
			}
		}

		/// <summary>
		///		Retrieves the type name of this scene manager.
		/// </summary>
		/// <remarks>
		///		This method has to be implemented by subclasses. It should
		///		return the type name of this SceneManager which agrees with
		///		the type name of the SceneManagerFactory which created it.
		///</remarks>
		public abstract string TypeName
		{
			get;
		}

		protected ulong _lightsDirtyCounter;

        // TODO: implement logic
        [OgreVersion(1, 7, 2790, "Implement logic for this")]
        private GpuProgramParameters.GpuParamVariability _gpuParamsDirty = GpuProgramParameters.GpuParamVariability.All;

	    /// <summary>
		/// Gets the lights dirty counter.
		/// </summary>
		/// <remarks>
		/// Scene manager tracking lights that affecting the frustum, if changes
		/// detected (the changes includes light list itself and the light's position
		/// and attenuation range), then increase the lights dirty counter.
		/// <para />
		/// When implementing customise lights finding algorithm relied on either
		/// <see cref="lightsAffectingFrustum"/> or <see cref="PopulateLightList"/>,
		/// might check this value for sure that the light list are really need to
		/// re-populate, otherwise, returns cached light list (if exists) for better
		/// performance.
		/// </remarks>
		public ulong LightsDirtyCounter
		{
			get
			{
				return _lightsDirtyCounter;
			}
		}

		/// <summary>
		/// Advance method to increase the lights dirty counter due lights changed.
		/// </summary>
		/// <remarks>
		/// Scene manager tracking lights that affecting the frustum, if changes
		/// detected (the changes includes light list itself and the light's position
		/// and attenuation range), then increase the lights dirty counter.
		/// <para />
		/// For some reason, you can call this method to force whole scene objects
		/// re-populate their light list. But near in mind, call to this method
		/// will harm performance, so should avoid if possible.
		/// </remarks>
		protected internal virtual void NotifyLightsDirty()
		{
			++_lightsDirtyCounter;
		}

		#endregion Properties

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
		protected internal void RenderScene( Camera camera, Viewport viewport, bool showOverlays )
		{
			// let the engine know this is the current scene manager
			Root.Instance.SceneManager = this;

			if ( this.IsShadowTechniqueInUse )
			{
				// initialize shadow volume materials
				this.InitShadowVolumeMaterials();
			}

			// Perform a quick pre-check to see whether we should override far distance
			// When using stencil volumes we have to use infinite far distance
			// to prevent dark caps getting clipped
			if ( this.IsShadowTechniqueStencilBased &&
				 camera.Far != 0 &&
				 this.targetRenderSystem.Capabilities.HasCapability( Capabilities.InfiniteFarPlane ) &&
				 this.shadowUseInfiniteFarPlane )
			{
				// infinite far distance
				camera.Far = 0.0f;
			}

			this.cameraInProgress = camera;
			this.hasCameraChanged = true;

			// Update the scene, only do this once per frame
			ulong thisFrameNumber = Root.Instance.CurrentFrameCount;
			if ( thisFrameNumber != this.lastFrameNumber )
			{
				// Update animations
				this.ApplySceneAnimations();
				// Update controllers
				ControllerManager.Instance.UpdateAll();
				this.lastFrameNumber = thisFrameNumber;
			}

			// Update scene graph for this camera (can happen multiple times per frame)
			this.UpdateSceneGraph( camera );

			// Auto-track nodes
			foreach ( SceneNode sn in autoTrackingSceneNodes.Values )
			{
				sn.AutoTrack();
			}

			// ask the camera to auto track if it has a target
			camera.AutoTrack();

			// Are we using any shadows at all?
			if ( this.IsShadowTechniqueInUse && this.illuminationStage != IlluminationRenderStage.RenderToTexture &&
				 viewport.ShowShadows && this.findVisibleObjects )
			{
				// Locate any lights which could be affecting the frustum
				this.FindLightsAffectingFrustum( camera );

				if ( this.IsShadowTechniqueTextureBased )
				{
					// *******
					// WARNING
					// *******
					// This call will result in re-entrant calls to this method
					// therefore anything which comes before this is NOT
					// guaranteed persistent. Make sure that anything which
					// MUST be specific to this camera / target is done
					// AFTER THIS POINT
					this.PrepareShadowTextures( camera, viewport );
					// reset the cameras because of the re-entrant call
					this.cameraInProgress = camera;
					this.hasCameraChanged = true;
				}
			}

			// Invert vertex winding?
			this.targetRenderSystem.InvertVertexWinding = camera.IsReflected;

			// Tell params about viewport
			this.autoParamDataSource.Viewport = viewport;
			// Set the viewport
			this.SetViewport( viewport );

			// set the current camera for use in the auto GPU program params
			this.autoParamDataSource.Camera = camera;

			// Set autoparams for finite dir light extrusion
			this.autoParamDataSource.SetShadowDirLightExtrusionDistance( this.shadowDirLightExtrudeDist );

			// sets the current ambient light color for use in auto GPU program params
			this.autoParamDataSource.AmbientLight = this.ambientColor;
			// Tell rendersystem
			this.targetRenderSystem.AmbientLight = this.ambientColor;

			// Tell params about render target
			this.autoParamDataSource.RenderTarget = viewport.Target;

			// set fog params
			float fogScale = 1f;
			if ( this.fogMode == FogMode.None )
			{
				fogScale = 0f;
			}
			this.autoParamDataSource.FogParams = new Vector4( this.fogStart, this.fogEnd, fogScale, 0 );

			// set the time in the auto param data source
			//autoParamDataSource.Time = ((float)Root.Instance.Timer.Milliseconds) / 1000f;

			// Set camera window clipping planes (if any)
			if ( this.targetRenderSystem.Capabilities.HasCapability( Capabilities.UserClipPlanes ) )
			{
				// TODO: Add ClipPlanes to RenderSystem.cs
				if ( camera.IsWindowSet )
				{
				    targetRenderSystem.ResetClipPlanes();
					IList<Plane> planeList = camera.WindowPlanes;
					for ( ushort i = 0; i < 4; ++i )
					{
					    targetRenderSystem.AddClipPlane( planeList[ i ] );

						//this.targetRenderSystem.EnableClipPlane( i, true );
						//this.targetRenderSystem.SetClipPlane( i, planeList[ i ] );
					}
				}
				// this disables any user-set clipplanes... this should be done manually
				//else
				//{
				//    for (ushort i = 0; i < 4; ++i)
				//    {
				//        targetRenderSystem.EnableClipPlane(i, false);
				//    }
				//}
			}

			// Prepare render queue for receiving new objects
			this.PrepareRenderQueue();

			// Parse the scene and tag visibles
			if ( this.findVisibleObjects )
			{
				if ( this.PreFindVisibleObjects != null )
					PreFindVisibleObjects( this, this.illuminationStage, viewport );
				this.FindVisibleObjects( camera, this.illuminationStage == IlluminationRenderStage.RenderToTexture );
				if ( this.PostFindVisibleObjects != null )
					PostFindVisibleObjects( this, this.illuminationStage, viewport );
			}

			// Add overlays, if viewport deems it
			if ( viewport.ShowOverlays && this.illuminationStage != IlluminationRenderStage.RenderToTexture )
			{
				// Queue overlays for rendering
				OverlayManager.Instance.QueueOverlaysForRendering( camera, this.GetRenderQueue(), viewport );
			}

			// queue overlays and skyboxes for rendering
			if ( viewport.ShowSkies && this.findVisibleObjects &&
				 this.illuminationStage != IlluminationRenderStage.RenderToTexture )
			{
				this.QueueSkiesForRendering( camera );
			}

			// begin frame geometry count
			this.targetRenderSystem.BeginGeometryCount();

			// clear the device if need be
			if ( viewport.ClearEveryFrame )
			{
				this.targetRenderSystem.ClearFrameBuffer( viewport.ClearBuffers, viewport.BackgroundColor );
			}

			// being a frame of animation
			this.targetRenderSystem.BeginFrame();

			// use the camera's current scene detail level
			this.targetRenderSystem.PolygonMode = camera.PolygonMode;

			// Set initial camera state
			this.targetRenderSystem.ProjectionMatrix = camera.ProjectionMatrixRS;
			this.targetRenderSystem.ViewMatrix = camera.ViewMatrix;

			// render all visible objects
			this.RenderVisibleObjects();

			// end the current frame
			this.targetRenderSystem.EndFrame();

			// Notify camera of the number of rendered faces
			camera.NotifyRenderedFaces( this.targetRenderSystem.FaceCount );

			// Notify camera of the number of rendered batches
			camera.NotifyRenderedBatches( this.targetRenderSystem.BatchCount );
		}

		private void PrepareRenderQueue()
		{
			// Clear the render queue
			this.GetRenderQueue().Clear();

			// Global split options
			this.UpdateRenderQueueSplitOptions();
		}

		private void UpdateRenderQueueSplitOptions()
		{
			RenderQueue q = this.GetRenderQueue();
			if ( this.IsShadowTechniqueStencilBased )
			{
				// Casters can always be receivers
				q.ShadowCastersCannotBeReceivers = false;
			}
			else // texture based
			{
				q.ShadowCastersCannotBeReceivers = !this.shadowTextureSelfShadow;
			}

			if ( this.IsShadowTechniqueAdditive && this.currentViewport.ShowShadows )
			{
				// Additive lighting, we need to split everything by illumination stage
				q.SplitPassesByLightingType = true;
			}
			else
			{
				q.SplitPassesByLightingType = false;
			}

			if ( this.IsShadowTechniqueInUse && this.currentViewport.ShowShadows )
			{
				// Tell render queue to split off non-shadowable materials
				q.SplitNoShadowPasses = true;
			}
			else
			{
				q.SplitNoShadowPasses = false;
			}
		}

		private void UpdateRenderQueueGroupSplitOptions( RenderQueueGroup group,
														 bool suppressShadows,
														 bool suppressRenderState )
		{
			if ( this.IsShadowTechniqueStencilBased )
			{
				// Casters can always be receivers
				group.ShadowCastersCannotBeReceivers = false;
			}
			else if ( this.IsShadowTechniqueTextureBased )
			{
				group.ShadowCastersCannotBeReceivers = !this.shadowTextureSelfShadow;
			}

			if ( !suppressShadows && this.currentViewport.ShowShadows && this.IsShadowTechniqueAdditive )
			{
				// Additive lighting, we need to split everything by illumination stage
				group.SplitPassesByLightingType = true;
			}
			else
			{
				group.SplitPassesByLightingType = false;
			}

			if ( !suppressShadows && this.currentViewport.ShowShadows && this.IsShadowTechniqueInUse )
			{
				// Tell render queue to split off non-shadowable materials
				group.SplitNoShadowPasses = true;
			}
			else
			{
				group.SplitNoShadowPasses = false;
			}
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
			// Process queued needUpdate calls
			Node.ProcessQueuedUpdates();

			// Cascade down the graph updating transforms & world bounds
			// In this implementation, just update from the root
			// Smarter SceneManager subclasses may choose to update only
			// certain scene graph branches based on space partioning info.
			this.rootSceneNode.Update( true, false );
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
		public virtual void FindVisibleObjects( Camera camera, bool onlyShadowCasters )
		{
			// ask the root node to iterate through and find visible objects in the scene
			this.rootSceneNode.FindVisibleObjects( camera,
												   this.GetRenderQueue(),
												   true,
												   this.displayNodes,
												   onlyShadowCasters );
		}

		/// <summary>
		///		Internal method for applying animations to scene nodes.
		/// </summary>
		/// <remarks>
		///		Uses the internally stored AnimationState objects to apply animation to SceneNodes.
		/// </remarks>
		internal virtual void ApplySceneAnimations()
		{
			foreach ( AnimationState animState in this.animationStateList.Values )
			{
				// get this states animation
				Animation anim = this.animationList[ animState.Name ];

				// loop through all node tracks and reset their nodes initial state
				foreach ( NodeAnimationTrack nodeTrack in anim.NodeTracks.Values )
				{
					Node node = nodeTrack.TargetNode;
					node.ResetToInitialState();
				}

				// loop through all node tracks and reset their nodes initial state
				foreach ( NumericAnimationTrack numericTrack in anim.NumericTracks.Values )
				{
					AnimableValue animable = numericTrack.TargetAnimable;
					animable.ResetToBaseValue();
				}

				// apply the animation
				anim.Apply( animState.Time, animState.Weight, false, 1.0f );
			}
		}

		protected internal void DestroyShadowTextures()
		{
			for ( int i = 0; i < this.shadowTextures.Count; ++i )
			{
				Texture shadowTex = this.shadowTextures[ i ];
				// TODO: It would be useful to have a reference count for
				// these textures.  They should only be removed from the
				// resource manager if nobody else is using them.
				TextureManager.Instance.Remove( shadowTex.Name );
				// destroy texture
				// TODO: Should I really destroy this texture here?
				if ( !shadowTex.IsDisposed )
					shadowTex.Dispose();

				this.DestroyCamera( this.shadowTextureCameras[ i ] );
			}
			this.shadowTextures.Clear();
			this.shadowTextureCameras.Clear();
		}

		public void DestroyCamera( Camera camera )
		{
			cameraList.Remove( camera.Name );
			this.targetRenderSystem.NotifyCameraRemoved( camera );

			if ( !camera.IsDisposed )
				camera.Dispose();
		}

		/// <summary>
		///     Destroy all cameras managed by this SceneManager
		/// <remarks>
		///     Method added with MovableObject Factories.
		/// </remarks>
		/// </summary>
		public void DestroyAllCameras()
		{
			foreach ( Camera camera in this.cameraList.Values )
			{
				this.targetRenderSystem.NotifyCameraRemoved( camera );

				if ( !camera.IsDisposed )
					camera.Dispose();
			}

			this.cameraList.Clear();
		}

		/// <summary>
		/// Internal method for creating shadow textures (texture-based shadows).
		/// </summary>
		protected internal virtual void CreateShadowTextures( ushort size, ushort count, PixelFormat format )
		{
			string baseName = "Axiom/ShadowTexture";

			if ( !this.IsShadowTechniqueTextureBased ||
				 this.shadowTextures.Count > 0 &&
				 count == this.shadowTextureCount &&
				 size == this.shadowTextureSize &&
				 format == this.shadowTextureFormat )
			{
				// no change
				return;
			}

			// destroy existing
			this.DestroyShadowTextures();

			// Recreate shadow textures
			for ( ushort t = 0; t < count; ++t )
			{
				string targName = string.Format( "{0}{1}", baseName, t );
				string matName = string.Format( "{0}Mat{1}", baseName, t );
				string camName = string.Format( "{0}Cam{1}", baseName, t );

				// try to get existing texture first, since we share these between
				// potentially multiple SMs
				Texture shadowTex = (Texture)TextureManager.Instance[ targName ];
				if ( shadowTex == null )
				{
					shadowTex = TextureManager.Instance.CreateManual( targName,
																	  ResourceGroupManager.InternalResourceGroupName,
																	  TextureType.TwoD,
																	  size,
																	  size,
																	  0,
																	  format,
																	  TextureUsage.RenderTarget );
				}
				else if ( shadowTex.Width != size ||
						  shadowTex.Height != size ||
						  shadowTex.Format != format )
				{
					LogManager.Instance.Write( "Warning: shadow texture #{0} is shared " +
											   "between scene managers but the sizes / formats " +
											   "do not agree. Consider rationalizing your scene manager " +
											   "shadow texture settings.",
											   t );
				}
				shadowTex.Load();

				RenderTexture shadowRTT = shadowTex.GetBuffer().GetRenderTarget();

				// Create camera for this texture, but note that we have to rebind
				// in prepareShadowTextures to coexist with multiple SMs
				Camera cam = this.CreateCamera( camName );
				cam.AspectRatio = 1.0f;
				// Don't use rendering distance for light cameras; we don't want shadows
				// for visible objects disappearing, especially for directional lights
				cam.UseRenderingDistance = false;
				this.shadowTextureCameras.Add( cam );

				// Create a viewport, if not there already
				if ( shadowRTT.NumViewports == 0 )
				{
					// Note camera assignment is transient when multiple SMs
					Viewport view = shadowRTT.AddViewport( cam );
					view.SetClearEveryFrame(true);
					// remove overlays
					view.ShowOverlays = false;
				}

				// Don't update automatically - we'll do it when required
				shadowRTT.IsAutoUpdated = false;
				this.shadowTextures.Add( shadowTex );

				// Also create corresponding Material used for rendering this shadow
				Material mat = (Material)MaterialManager.Instance[ matName ];
				if ( mat == null )
				{
					mat = (Material)MaterialManager.Instance.Create( matName, ResourceGroupManager.InternalResourceGroupName );
				}

				// create texture unit referring to render target texture
				TextureUnitState texUnit = mat.GetTechnique( 0 ).GetPass( 0 ).CreateTextureUnitState( targName );
				// set projective based on camera
				texUnit.SetProjectiveTexturing( true, cam );
                texUnit.SetTextureAddressingMode( TextureAddressing.Border );
				texUnit.TextureBorderColor = ColorEx.White;
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
			IlluminationRenderStage savedStage = this.illuminationStage;
			this.illuminationStage = IlluminationRenderStage.RenderToTexture;

			// Determine far shadow distance
			float shadowDist = this.shadowFarDistance;
			if ( shadowDist == 0.0f )
			{
				// need a shadow distance, make one up
				shadowDist = camera.Near * 300;
			}
			// set fogging to hide the shadow edge
			float shadowOffset = shadowDist * this.shadowTextureOffset;
			// Precalculate fading info
			float shadowEnd = shadowDist + shadowOffset;
			float fadeStart = shadowEnd * this.shadowTextureFadeStart;
			float fadeEnd = shadowEnd * this.shadowTextureFadeEnd;
			// Additive lighting should not use fogging, since it will overbrighten; use border clamp
			if ( !this.IsShadowTechniqueAdditive )
			{
				this.shadowReceiverPass.SetFog( true,
												FogMode.Linear,
												ColorEx.White,
												0,
												fadeStart,
												fadeEnd );
				// if we have a custom receiver material, then give it the fog params too
				if ( this.shadowTextureCustomReceiverPass != null )
				{
					this.shadowTextureCustomReceiverPass.SetFog( true,
																 FogMode.Linear,
																 ColorEx.White,
																 0,
																 fadeStart,
																 fadeEnd );
				}
			}
			else
			{
				// disable fogging explicitly
				this.shadowReceiverPass.SetFog( true, FogMode.None );
				// if we have a custom receiver material, then give it the fog params too
				if ( this.shadowTextureCustomReceiverPass != null )
				{
					this.shadowTextureCustomReceiverPass.SetFog( true, FogMode.None );
				}
			}

			// Iterate over the lights we've found, max out at the limit of light textures
			int sti = 0;
			foreach ( Light light in this.lightsAffectingFrustum )
			{
				// Check limit reached
				if ( sti == this.shadowTextures.Count )
					break;

				// Skip non-shadowing lights
				if ( !light.CastShadows )
				{
					continue;
				}

				Texture shadowTex = this.shadowTextures[ sti ];
				RenderTarget shadowRTT = shadowTex.GetBuffer().GetRenderTarget();
				Viewport shadowView = shadowRTT.GetViewport( 0 );
				Camera texCam = this.shadowTextureCameras[ sti ];

				// rebind camera, incase another SM in use which has switched to its cam
				shadowView.Camera = texCam;

				// Associate main view camera as LOD camera
				texCam.LodCamera = camera;

				//Vector3 dir;

				// set base
				if ( light.Type == LightType.Point )
					texCam.Direction = light.DerivedDirection;
				if ( light.Type == LightType.Directional )
					texCam.Position = light.GetDerivedPosition();

				// Use the material scheme of the main viewport
				// This is required to pick up the correct shadow_caster_material and similar properties.
				shadowView.MaterialScheme = viewPort.MaterialScheme;

				if ( light.CustomShadowCameraSetup == null )
				{
					_defaultShadowCameraSetup.GetShadowCamera( this, camera, viewPort, light, texCam, sti );
				}
				else
				{
					light.CustomShadowCameraSetup.GetShadowCamera( this, camera, viewPort, light, texCam, sti );
				}

				shadowView.BackgroundColor = ColorEx.White;

				// Fire shadow caster update, callee can alter camera settings
				// fireShadowTexturesPreCaster(light, texCam);

				// Update target
				shadowRTT.Update();

				++sti;
			}
			// Set the illumination stage, prevents recursive calls
			this.illuminationStage = savedStage;

			//fireShadowTexturesUpdated( std::min(mLightsAffectingFrustum.size(), mShadowTextures.size()));
		}

		/// <summary>
		///		Internal method for setting the destination viewport for the next render.
		/// </summary>
		/// <param name="viewport"></param>
		protected virtual void SetViewport( Viewport viewport )
		{
			this.currentViewport = viewport;
			// Set viewport in render system
			this.targetRenderSystem.Viewport = viewport;
			// Set the active material scheme for this viewport
			MaterialManager.Instance.ActiveScheme = viewport.MaterialScheme;
		}


        [OgreVersion(1, 7, 2790, "Implement _gpuParamsDirty logic")]
        protected virtual void UpdateGpuProgramParameters(Pass pass)
        {
            if ( pass.IsProgrammable )
            {
                if (_gpuParamsDirty == 0)
                	return;

                if (_gpuParamsDirty != 0)
                	pass.UpdateAutoParams(autoParamDataSource, _gpuParamsDirty);

                if ( pass.HasVertexProgram )
                {
                    targetRenderSystem.BindGpuProgramParameters( GpuProgramType.Vertex, pass.VertexProgramParameters,
                                                                 _gpuParamsDirty );
                }

                if ( pass.HasGeometryProgram )
                {
                    targetRenderSystem.BindGpuProgramParameters( GpuProgramType.Geometry, pass.GeometryProgramParameters,
                                                                 _gpuParamsDirty );
                }

                if ( pass.HasFragmentProgram )
                {
                    targetRenderSystem.BindGpuProgramParameters( GpuProgramType.Fragment, pass.FragmentProgramParameters,
                                                                 _gpuParamsDirty );
                }

                //_gpuParamsDirty = 0;
            }

        }

	    protected void RenderSingleObject( IRenderable renderable, Pass pass, bool doLightIteration )
		{
			this.RenderSingleObject( renderable, pass, doLightIteration, null );
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
		protected virtual void RenderSingleObject( IRenderable renderable,
												   Pass pass,
												   bool doLightIteration,
												   LightList manualLightList )
		{
			ushort numMatrices = 0;

			// grab the current scene detail level
			PolygonMode camPolyMode = this.cameraInProgress.PolygonMode;

			// get the world matrices and the count
			renderable.GetWorldTransforms( this.xform );
			numMatrices = renderable.NumWorldTransforms;

			// set the world matrices in the render system
			if ( numMatrices > 1 )
			{
				this.targetRenderSystem.SetWorldMatrices( this.xform, numMatrices );
			}
			else
			{
				this.targetRenderSystem.WorldMatrix = this.xform[ 0 ];
			}

			// issue view/projection changes (if any)
			this.UseRenderableViewProjection( renderable );

			if ( !this.suppressRenderStateChanges )
			{
				bool passSurfaceAndLightParams = true;
				if ( pass.IsProgrammable )
				{
					// Tell auto params object about the renderable change
					this.autoParamDataSource.Renderable = renderable;
					//pass.UpdateAutoParamsNoLights( this.autoParamDataSource );

                    if ( pass.HasVertexProgram )
					{
						passSurfaceAndLightParams = pass.VertexProgram.PassSurfaceAndLightStates;
					}
				}

				// issue texture units that depend on updated view matrix
				// reflective env mapping is one case
				for ( int i = 0; i < pass.TextureUnitStageCount; i++ )
				{
					TextureUnitState texUnit = pass.GetTextureUnitState( i );

					if ( texUnit.HasViewRelativeTexCoordGen )
					{
					    targetRenderSystem.SetTextureUnitSettings( i, texUnit );
					    //this.targetRenderSystem.SetTextureUnit( i, texUnit, !pass.HasFragmentProgram );
					}
				}

				// Normalize normals
				bool thisNormalize = renderable.NormalizeNormals;

				if ( thisNormalize != normalizeNormals )
				{
					this.targetRenderSystem.NormalizeNormals = thisNormalize;
					normalizeNormals = thisNormalize;
				}

				// Set up the solid / wireframe override
				PolygonMode requestedMode = pass.PolygonMode;
				if ( renderable.PolygonModeOverrideable == true )
				{
					// check camera detial only when render detail is overridable
					if ( requestedMode > camPolyMode )
					{
						// only downgrade detail; if cam says wireframe we don't go up to solid
						requestedMode = camPolyMode;
					}
				}

				if ( requestedMode != this.lastPolyMode )
				{
					this.targetRenderSystem.PolygonMode = requestedMode;
					this.lastPolyMode = requestedMode;
				}

				// TODO: Add ClipPlanes to RenderSystem.cs
				// This is removed in OGRE 1.6.0... no need to port - J. Price
				//targetRenderSystem.ClipPlanes = renderable.ClipPlanes;

				// get the renderables render operation
				op = renderable.RenderOperation;
				// TODO: Add srcRenderable to RenderOperation.cs
				//op.srcRenderable = renderable;

				if ( doLightIteration )
				{
					// Here's where we issue the rendering operation to the render system
					// Note that we may do this once per light, therefore it's in a loop
					// and the light parameters are updated once per traversal through the
					// loop
					LightList rendLightList = renderable.Lights;
					bool iteratePerLight = pass.IteratePerLight;
					int numIterations = iteratePerLight ? rendLightList.Count : 1;
					LightList lightListToUse = null;

					for ( int i = 0; i < numIterations; i++ )
					{
						// determine light list to use
						if ( iteratePerLight )
						{
							localLightList.Clear();

							// check whether we need to filter this one out
							if ( pass.RunOnlyOncePerLightType && pass.OnlyLightType != rendLightList[ i ].Type )
							{
								// skip this one
								continue;
							}

							localLightList.Add( rendLightList[ i ] );
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
							this.autoParamDataSource.SetCurrentLightList( lightListToUse );
							//pass.UpdateAutoParamsLightsOnly( this.autoParamDataSource );

						    UpdateGpuProgramParameters( pass );
						}

						// Do we need to update light states?
						// Only do this if fixed-function vertex lighting applies
						if ( pass.LightingEnabled && passSurfaceAndLightParams )
						{
							this.targetRenderSystem.UseLights( lightListToUse, pass.MaxSimultaneousLights );
						}
                        this.targetRenderSystem.CurrentPassIterationCount = pass.IterationCount;
						// issue the render op
						this.targetRenderSystem.Render( op );
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
							this.autoParamDataSource.SetCurrentLightList( manualLightList );
							//pass.UpdateAutoParamsLightsOnly( this.autoParamDataSource );
						}

					    UpdateGpuProgramParameters( pass );
					}

					// Use manual lights if present, and not using vertex programs
					if ( manualLightList != null && pass.LightingEnabled && passSurfaceAndLightParams )
					{
						this.targetRenderSystem.UseLights( manualLightList, pass.MaxSimultaneousLights );
					}
                    this.targetRenderSystem.CurrentPassIterationCount = pass.IterationCount;
					// issue the render op
					this.targetRenderSystem.Render( op );
				}
			}
			else
			{
				// suppressRenderStateChanges
				// Just render
                this.targetRenderSystem.CurrentPassIterationCount = 1;
				this.targetRenderSystem.Render( op );
			}

			// Reset view / projection changes if any
			this.ResetViewProjectionMode();
		}

		/// <summary>
		///		Renders a set of solid objects.
		/// </summary>
		protected virtual void RenderSolidObjects( System.Collections.SortedList list,
												   bool doLightIteration,
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
				if ( !this.ValidatePassForRendering( pass ) )
				{
					continue;
				}

				// For solids, we try to do each pass in turn
				Pass usedPass = this.SetPass( pass );

				// render each object associated with this rendering pass
				for ( int r = 0; r < renderables.Count; r++ )
				{
					IRenderable renderable = (IRenderable)renderables[ r ];

					// Give SM a chance to eliminate
					if ( !this.ValidateRenderableForRendering( usedPass, renderable ) )
					{
						continue;
					}

					// Render a single object, this will set up auto params if required
					this.RenderSingleObject( renderable, usedPass, doLightIteration, manualLightList );
				}
			}
		}

		protected void RenderSolidObjects( System.Collections.SortedList list, bool doLightIteration )
		{
			this.RenderSolidObjects( list, doLightIteration, null );
		}

		/// <summary>
		///		Renders a set of transparent objects.
		/// </summary>
		protected virtual void RenderTransparentObjects( List<RenderablePass> list, bool doLightIteration,
														 LightList manualLightList )
		{
			// ----- TRANSPARENT LOOP -----
			// This time we render by Z, not by material
			// The transparent objects set needs to be ordered first
			for ( int i = 0; i < list.Count; i++ )
			{
				RenderablePass rp = (RenderablePass)list[ i ];

				// set the pass first
				this.SetPass( rp.pass );

				// render the transparent object
				this.RenderSingleObject( rp.renderable,
										 rp.pass,
										 doLightIteration,
										 manualLightList );
			}
		}

		protected void RenderTransparentObjects( List<RenderablePass> list, bool doLightIteration )
		{
			this.RenderTransparentObjects( list, doLightIteration, null );
		}

		/// <summary>
		///		Render a group with the added complexity of additive stencil shadows.
		/// </summary>
		/// <param name="group">Render queue group.</param>
		protected virtual void RenderAdditiveStencilShadowedQueueGroupObjects( RenderQueueGroup group )
		{
			LightList tempLightList = new LightList();

			foreach ( RenderPriorityGroup priorityGroup in group.PriorityGroups.Values )
			{
				// sort the group first
				priorityGroup.Sort( this.cameraInProgress );

				// Clear light list
				tempLightList.Clear();

				// Render all the ambient passes first, no light iteration, no lights
				this.illuminationStage = IlluminationRenderStage.Ambient;
				this.RenderSolidObjects( priorityGroup.solidPasses, false, tempLightList );
				// Also render any objects which have receive shadows disabled
				this.renderingNoShadowQueue = true;
				this.RenderSolidObjects( priorityGroup.solidPassesNoShadow, true );
				this.renderingNoShadowQueue = false;

				// Now iterate per light
				this.illuminationStage = IlluminationRenderStage.PerLight;

				foreach ( Light light in lightsAffectingFrustum )
				{
					// Set light state

					if ( light.CastShadows )
					{
						// Clear stencil
						this.targetRenderSystem.ClearFrameBuffer( FrameBufferType.Stencil );
						this.RenderShadowVolumesToStencil( light, this.cameraInProgress );
						// turn stencil check on
						this.targetRenderSystem.StencilCheckEnabled = true;
						// NB we render where the stencil is equal to zero to render lit areas
						this.targetRenderSystem.SetStencilBufferParams( CompareFunction.Equal, 0 );
					}

					// render lighting passes for this light
					tempLightList.Clear();
					tempLightList.Add( light );

					this.RenderSolidObjects( priorityGroup.solidPassesDiffuseSpecular, false, tempLightList );

					// Reset stencil params
					this.targetRenderSystem.SetStencilBufferParams();
					this.targetRenderSystem.StencilCheckEnabled = false;
					this.targetRenderSystem.SetDepthBufferParams();
				} // for each light

				// Now render decal passes, no need to set lights as lighting will be disabled
				this.illuminationStage = IlluminationRenderStage.Decal;
				this.RenderSolidObjects( priorityGroup.solidPassesDecal, false );
			} // for each priority

			// reset lighting stage
			this.illuminationStage = IlluminationRenderStage.None;

			// Iterate again
			foreach ( RenderPriorityGroup priorityGroup in group.PriorityGroups.Values )
			{
				// Do transparents
				this.RenderTransparentObjects( priorityGroup.transparentPasses, true );
			} // for each priority
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
			foreach ( RenderPriorityGroup priorityGroup in group.PriorityGroups.Values )
			{
				// sort the group first
				priorityGroup.Sort( this.cameraInProgress );

				// do solids
				this.RenderSolidObjects( priorityGroup.solidPasses, true );
			}

			// iterate over lights, rendering all volumes to the stencil buffer
			foreach ( Light light in this.lightsAffectingFrustum )
			{
				if ( light.CastShadows )
				{
					// clear the stencil buffer
					this.targetRenderSystem.ClearFrameBuffer( FrameBufferType.Stencil );
					this.RenderShadowVolumesToStencil( light, this.cameraInProgress );

					// render full-screen shadow modulator for all lights
					this.SetPass( this.shadowModulativePass );

					// turn the stencil check on
					this.targetRenderSystem.StencilCheckEnabled = true;

					// we render where the stencil is not equal to zero to render shadows, not lit areas
					this.targetRenderSystem.SetStencilBufferParams( CompareFunction.NotEqual, 0 );
					this.RenderSingleObject( this.fullScreenQuad, this.shadowModulativePass, false );

					// reset stencil buffer params
					this.targetRenderSystem.SetStencilBufferParams();
					this.targetRenderSystem.StencilCheckEnabled = false;
					this.targetRenderSystem.SetDepthBufferParams();
				}
			} // for each light

			foreach ( RenderPriorityGroup priorityGroup in group.PriorityGroups.Values )
			{
				// Do non-shadowable solids
				this.renderingNoShadowQueue = true;
				this.RenderSolidObjects( priorityGroup.solidPassesNoShadow, true );
				this.renderingNoShadowQueue = false;
			} // for each priority

			foreach ( RenderPriorityGroup priorityGroup in group.PriorityGroups.Values )
			{
				// Do transparents
				this.RenderTransparentObjects( priorityGroup.transparentPasses, true );
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
			if ( this.IsShadowTechniqueAdditive )
			{
				this.autoParamDataSource.AmbientLight = ColorEx.Black;
				this.targetRenderSystem.AmbientLight = ColorEx.Black;
			}
			else
			{
				this.autoParamDataSource.AmbientLight = this.shadowColor;
				this.targetRenderSystem.AmbientLight = this.shadowColor;
			}

			// Iterate through priorities
			foreach ( RenderPriorityGroup priorityGroup in group.PriorityGroups.Values )
			{
				// Sort the queue first
				priorityGroup.Sort( this.cameraInProgress );

				// Do solids, override light list in case any vertex programs use them
				this.RenderSolidObjects( priorityGroup.solidPasses, false, this.nullLightList );
				this.renderingNoShadowQueue = true;
				this.RenderSolidObjects( priorityGroup.solidPassesNoShadow, false, this.nullLightList );
				this.renderingNoShadowQueue = false;

				// Do transparents that cast shadows
				this.RenderTransparentShadowCasterObjects( priorityGroup.transparentPasses, false, this.nullLightList );
			} // for each priority

			// reset ambient light
			this.autoParamDataSource.AmbientLight = this.ambientColor;
			this.targetRenderSystem.AmbientLight = this.ambientColor;
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
			Now, this means we are going to reorder things more, but that is required
			if the shadows are to look correct. The overall order is preserved anyway,
			it's just that all the transparents are at the end instead of them being
			interleaved as in the normal rendering loop.
			*/
			// Iterate through priorities
			foreach ( RenderPriorityGroup priorityGroup in group.PriorityGroups.Values )
			{
				// Sort the queue first
				priorityGroup.Sort( this.cameraInProgress );

				// Do solids
				this.RenderSolidObjects( priorityGroup.solidPasses, true );
				this.renderingNoShadowQueue = true;
				this.RenderSolidObjects( priorityGroup.solidPassesNoShadow, true );
				this.renderingNoShadowQueue = false;
			}

			// Iterate over lights, render received shadows
			// only perform this if we're in the 'normal' render stage, to avoid
			// doing it during the render to texture
			if ( this.illuminationStage == IlluminationRenderStage.None )
			{
				this.illuminationStage = IlluminationRenderStage.RenderReceiverPass;

				int sti = 0;
				foreach ( Light light in this.lightsAffectingFrustum )
				{
					// Check limit reached
					if ( sti == this.shadowTextures.Count )
						break;

					if ( !light.CastShadows )
					{
						continue;
					}

					Texture shadowTex = this.shadowTextures[ sti ];
					Camera cam = shadowTex.GetBuffer().GetRenderTarget().GetViewport( 0 ).Camera;

					// Hook up receiver texture
					Pass targetPass = this.shadowTextureCustomReceiverPass != null
										  ? this.shadowTextureCustomReceiverPass
										  : this.shadowReceiverPass;
					TextureUnitState textureUnit = targetPass.GetTextureUnitState( 0 );
					textureUnit.SetTextureName( shadowTex.Name );

					// Hook up projection frustum if fixed-function, but also need to
					// disable it explicitly for program pipeline.
					textureUnit.SetProjectiveTexturing( !targetPass.HasVertexProgram, cam );

					// clamp to border color in case this is a custom material
                    textureUnit.SetTextureAddressingMode( TextureAddressing.Border );
					textureUnit.TextureBorderColor = ColorEx.White;

					this.autoParamDataSource.TextureProjector = cam;

					// if this light is a spotlight, we need to add the spot fader layer
					// BUT not if using a custom projection matrix, since then it will be
					// inappropriately shaped most likely
					if ( light.Type == LightType.Spotlight && !cam.IsCustomProjectionMatrixEnabled )
					{
						// remove all TUs except 0 & 1
						// (only an issue if additive shadows have been used)
						while ( targetPass.TextureUnitStageCount > 2 )
						{
							targetPass.RemoveTextureUnitState( 2 );
						}

						// Add spot fader if not present already
						if ( targetPass.TextureUnitStageCount == 2 &&
							 targetPass.GetTextureUnitState( 1 ).TextureName == "spot_shadow_fade.png" )
						{
							// Just set
							TextureUnitState tex = targetPass.GetTextureUnitState( 1 );
							tex.SetProjectiveTexturing( !targetPass.HasVertexProgram, cam );
						}
						else
						{
							// Remove any non-conforming spot layers
							while ( targetPass.TextureUnitStageCount > 1 )
							{
								targetPass.RemoveTextureUnitState( 1 );
							}

							TextureUnitState tex = targetPass.CreateTextureUnitState( "spot_shadow_fade.png" );
							tex.SetProjectiveTexturing( !targetPass.HasVertexProgram, cam );
							tex.SetColorOperation( LayerBlendOperation.Add );
                            tex.SetTextureAddressingMode( TextureAddressing.Clamp );
						}
					}
					else
					{
						// remove all TUs except 0 including spot
						while ( targetPass.TextureUnitStageCount > 1 )
						{
							targetPass.RemoveTextureUnitState( 1 );
						}
					}

					// Set lighting / blending modes
					targetPass.SetSceneBlending( SceneBlendFactor.DestColor, SceneBlendFactor.Zero );
					targetPass.LightingEnabled = false;

					targetPass.Load();

					// Fire pre-reciever event
					// fireShadowTexturesPreReceiver(light, cam);

					this.RenderTextureShadowReceiverQueueGroupObjects( group );
					++sti;
				} // for each light

				this.illuminationStage = IlluminationRenderStage.None;
			}

			// Iterate again
			foreach ( RenderPriorityGroup priorityGroup in group.PriorityGroups.Values )
			{
				// Do transparents
				this.RenderTransparentObjects( priorityGroup.transparentPasses, true );
			} // for each priority
		}

		/// <summary>
		///		Render a group with the added complexity of additive texture shadows.
		/// </summary>
		/// <param name="group">Render queue group.</param>
		private void RenderAdditiveTextureShadowedQueueGroupObjects( RenderQueueGroup group )
		{
			LightList tempLightList = new LightList();
			foreach ( RenderPriorityGroup priorityGroup in group.PriorityGroups.Values )
			{
				// Sort the queue first
				priorityGroup.Sort( this.cameraInProgress );

				// Clear light list
				tempLightList.Clear();

				// Render all the ambient passes first, no light iteration, no lights
				this.RenderSolidObjects( priorityGroup.solidPasses, false, tempLightList );
				// Also render any objects which have receive shadows disabled
				this.renderingNoShadowQueue = true;
				this.RenderSolidObjects( priorityGroup.solidPassesNoShadow, true );
				this.renderingNoShadowQueue = false;

				// only perform this next part if we're in the 'normal' render stage, to avoid
				// doing it during the render to texture
				if ( this.illuminationStage == IlluminationRenderStage.None )
				{
					// Iterate over lights, render masked
					int sti = 0;
					foreach ( Light light in this.lightsAffectingFrustum )
					{
						// Set light state
						if ( light.CastShadows && sti < this.shadowTextures.Count )
						{
							Texture shadowTex = this.shadowTextures[ sti ];
							// Get camera for current shadow texture
							Camera camera = shadowTex.GetBuffer().GetRenderTarget().GetViewport( 0 ).Camera;
							// Hook up receiver texture
							Pass targetPass = this.shadowTextureCustomReceiverPass != null
												  ? this.shadowTextureCustomReceiverPass
												  : this.shadowReceiverPass;
							targetPass.GetTextureUnitState( 0 ).SetTextureName( shadowTex.Name );
							// Hook up projection frustum
							targetPass.GetTextureUnitState( 0 ).SetProjectiveTexturing( true, camera );
							this.autoParamDataSource.TextureProjector = camera;
							// Remove any spot fader layer
							if ( targetPass.TextureUnitStageCount > 1 &&
								 targetPass.GetTextureUnitState( 1 ).TextureName == "spot_shadow_fade.png" )
							{
								// remove spot fader layer (should only be there if
								// we previously used modulative shadows)
								targetPass.RemoveTextureUnitState( 1 );
							}
							// Set lighting / blending modes
							targetPass.SetSceneBlending( SceneBlendFactor.One, SceneBlendFactor.One );
							targetPass.LightingEnabled = true;
							targetPass.Load();
							// increment shadow texture since used
							++sti;
							this.illuminationStage = IlluminationRenderStage.RenderReceiverPass;
						}
						else
						{
							this.illuminationStage = IlluminationRenderStage.None;
						}

						// render lighting passes for this light
						tempLightList.Clear();
						tempLightList.Add( light );

						this.RenderSolidObjects( priorityGroup.solidPassesDiffuseSpecular, false, tempLightList );
					} // for each light
					this.illuminationStage = IlluminationRenderStage.None;

					// Now render decal passes, no need to set lights as lighting will be disabled
					this.RenderSolidObjects( priorityGroup.solidPassesDecal, false );
				}
			} // for each priority

			foreach ( RenderPriorityGroup priorityGroup in group.PriorityGroups.Values )
			{
				// Do transparents
				this.RenderTransparentObjects( priorityGroup.transparentPasses, true );
			} // for each priority
		}

		/// <summary>
		///		Render a group rendering only shadow receivers.
		/// </summary>
		/// <param name="group">Render queue group.</param>
		protected virtual void RenderTextureShadowReceiverQueueGroupObjects( RenderQueueGroup group )
		{
			// Override auto param ambient to force vertex programs to go full-bright
			this.autoParamDataSource.AmbientLight = ColorEx.White;
			this.targetRenderSystem.AmbientLight = ColorEx.White;

			// Iterate through priorities
			foreach ( RenderPriorityGroup priorityGroup in group.PriorityGroups.Values )
			{
				// Do solids, override light list in case any vertex programs use them
				this.RenderSolidObjects( priorityGroup.solidPasses, false, this.nullLightList );

				// Don't render transparents or passes which have shadow receipt disabled
			} // for each priority

			// reset ambient
			this.autoParamDataSource.AmbientLight = this.ambientColor;
			this.targetRenderSystem.AmbientLight = this.ambientColor;
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
			if ( !this.suppressShadows && this.currentViewport.ShowShadows &&
				 ( ( this.IsShadowTechniqueModulative
					 && this.illuminationStage == IlluminationRenderStage.RenderReceiverPass ) ||
				   this.illuminationStage == IlluminationRenderStage.RenderToTexture || this.suppressRenderStateChanges ) &&
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
			if ( !this.suppressShadows && this.currentViewport.ShowShadows && this.IsShadowTechniqueTextureBased )
			{
				if ( this.illuminationStage == IlluminationRenderStage.RenderReceiverPass &&
					 renderable.CastsShadows && !this.shadowTextureSelfShadow )
				{
					return false;
				}
				// Some duplication here with validatePassForRendering, for transparents
				if ( ( ( this.IsShadowTechniqueModulative
						 && this.illuminationStage == IlluminationRenderStage.RenderReceiverPass )
					   || this.illuminationStage == IlluminationRenderStage.RenderToTexture
					   || this.suppressRenderStateChanges ) &&
					 pass.Index > 0 )
				{
					return false;
				}
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
			bool doShadows = group.ShadowsEnabled && this.currentViewport.ShowShadows &&
							 !this.suppressShadows && !this.suppressRenderStateChanges;
			if ( doShadows && this.shadowTechnique == ShadowTechnique.StencilAdditive )
			{
				this.RenderAdditiveStencilShadowedQueueGroupObjects( group );
			}
			else if ( doShadows && this.shadowTechnique == ShadowTechnique.StencilModulative )
			{
				this.RenderModulativeStencilShadowedQueueGroupObjects( group );
			}
			else if ( this.IsShadowTechniqueTextureBased )
			{
				// Modulative texture shadows in use
				if ( this.illuminationStage == IlluminationRenderStage.RenderToTexture )
				{
					// Shadow caster pass
					if ( this.currentViewport.ShowShadows && !this.suppressShadows && !this.suppressRenderStateChanges )
					{
						this.RenderTextureShadowCasterQueueGroupObjects( group );
					}
				}
				else
				{
					// Ordinary + receiver pass
					if ( doShadows )
					{
						if ( this.IsShadowTechniqueAdditive )
						{
							this.RenderAdditiveTextureShadowedQueueGroupObjects( group );
						}
						else
						{
							this.RenderModulativeTextureShadowedQueueGroupObjects( group );
						}
					}
					else
					{
						this.RenderBasicQueueGroupObjects( group );
					}
				}
			}
			else
			{
				// No shadows, ordinary pass
				this.RenderBasicQueueGroupObjects( group );
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
			foreach ( RenderPriorityGroup priorityGroup in group.PriorityGroups.Values )
			{
				// Sort the queue first
				priorityGroup.Sort( this.cameraInProgress );

				// Do solids
				this.RenderSolidObjects( priorityGroup.solidPasses, true );

				// Do transparents
				this.RenderTransparentObjects( priorityGroup.transparentPasses, true );
			} // for each priority
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
		protected virtual void RenderTransparentShadowCasterObjects( List<RenderablePass> list,
																	 bool doLightIteration,
																	 LightList manualLightList )
		{
			// ----- TRANSPARENT LOOP as in RenderTransparentObjects, but changed a bit -----
			for ( int i = 0; i < list.Count; i++ )
			{
				RenderablePass rp = list[ i ];

				// only render this pass if it's being forced to cast shadows
				if ( rp.pass.Parent.Parent.TransparencyCastsShadows )
				{
					this.SetPass( rp.pass );
					this.RenderSingleObject( rp.renderable, rp.pass, doLightIteration, manualLightList );
				}
			}
		}

		/// <summary>
		///		Sends visible objects found in <see cref="FindVisibleObjects"/> to the rendering engine.
		/// </summary>
		protected internal virtual void RenderVisibleObjects()
		{
			// loop through each main render group ( which is already sorted)
			for ( int i = 0; i < this.GetRenderQueue().NumRenderQueueGroups; i++ )
			{
				RenderQueueGroupID queueID = this.GetRenderQueue().GetRenderQueueGroupID( i );
				RenderQueueGroup queueGroup = this.GetRenderQueue().GetQueueGroupByIndex( i );

				if ( !this.specialCaseRenderQueueList.IsRenderQueueToBeProcessed( queueID ) )
					continue;

				if ( queueID == RenderQueueGroupID.Main )
				{
					this.renderingMainGroup = true;
				}
				bool repeatQueue = false;

				// repeat
				do
				{
					if ( this.OnRenderQueueStarted( queueID, illuminationStage == IlluminationRenderStage.RenderToTexture ?
															 String.Empty :
															 String.Empty ) )
					{
						// someone requested we skip this queue
						continue;
					}

					if ( queueGroup.NumPriorityGroups > 0 )
					{
						// render objects in all groups
						this.RenderQueueGroupObjects( queueGroup );
					}

					// true if someone requested that we repeat this queue
					repeatQueue = this.OnRenderQueueEnded( queueID, illuminationStage == IlluminationRenderStage.RenderToTexture ?
																	 String.Empty :
																	 String.Empty );
				} while ( repeatQueue );

				this.renderingMainGroup = false;
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
			if ( this.skyPlaneNode != null )
			{
				this.skyPlaneNode.Position = camera.DerivedPosition;
			}

			if ( this.skyBoxNode != null )
			{
				this.skyBoxNode.Position = camera.DerivedPosition;
			}

			if ( this.skyDomeNode != null )
			{
				this.skyDomeNode.Position = camera.DerivedPosition;
			}

			RenderQueueGroupID qid;

			// if the skyplane is enabled, queue up the single plane
			if ( this.isSkyPlaneEnabled )
			{
				qid = this.isSkyPlaneDrawnFirst ? RenderQueueGroupID.SkiesEarly : RenderQueueGroupID.SkiesLate;
				this.GetRenderQueue().AddRenderable( this.skyPlaneEntity.GetSubEntity( 0 ), 1, qid );
			}

			// if the skybox is enabled, queue up all the planes
			if ( this.isSkyBoxEnabled )
			{
				qid = this.isSkyBoxDrawnFirst ? RenderQueueGroupID.SkiesEarly : RenderQueueGroupID.SkiesLate;

				for ( int plane = 0; plane < 6; plane++ )
				{
					this.GetRenderQueue().AddRenderable( this.skyBoxEntities[ plane ].GetSubEntity( 0 ), 1, qid );
				}
			}

			// if the skydome is enabled, queue up all the planes
			if ( this.isSkyDomeEnabled )
			{
				qid = this.isSkyDomeDrawnFirst ? RenderQueueGroupID.SkiesEarly : RenderQueueGroupID.SkiesLate;

				for ( int plane = 0; plane < 5; ++plane )
				{
					this.GetRenderQueue().AddRenderable( this.skyDomeEntities[ plane ].GetSubEntity( 0 ), 1, qid );
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
		public virtual void PopulateLightList( Vector3 position, float radius, LightList destList )
		{
			// Really basic trawl of the lights, then sort
			// Subclasses could do something smarter
			destList.Clear();
			float squaredRadius = radius * radius;

			MovableObjectCollection lightList = this.GetMovableObjectCollection( LightFactory.TypeName );

			// loop through the scene lights an add ones in range
			foreach ( Light light in lightList.Values )
			{
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
						light.tempSquaredDist = ( light.GetDerivedPosition() - position ).LengthSquared;
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
	    /// <param name="groupName"></param>
	    public virtual void SetSkyPlane( bool enable,
										 Plane plane,
										 string materialName,
										 float scale,
										 float tiling,
										 bool drawFirst,
										 float bow,
										 string groupName )
		{
			this.isSkyPlaneEnabled = enable;

			if ( enable )
			{
				string meshName = "SkyPlane";
				this.skyPlane = plane;

				Material m = (Material)MaterialManager.Instance[ materialName ];

				if ( m == null )
				{
					throw new AxiomException( string.Format( "Skyplane material '{0}' not found.", materialName ) );
				}

				// make sure the material doesn't update the depth buffer
				m.DepthWrite = false;
				m.Load();

				this.isSkyPlaneDrawnFirst = drawFirst;

				// set up the place
				Mesh planeMesh = (Mesh)MeshManager.Instance[ meshName ];

				// unload the old one if it exists
				if ( planeMesh != null )
				{
					MeshManager.Instance.Unload( planeMesh );
				}

				// create up vector
				Vector3 up = plane.Normal.Cross( Vector3.UnitX );
				if ( up == Vector3.Zero )
				{
					up = plane.Normal.Cross( -Vector3.UnitZ );
				}

				if ( bow > 0 )
				{
					planeMesh = MeshManager.Instance.CreateCurvedIllusionPlane(
						meshName,
						groupName,
						plane,
						scale * 100,
						scale * 100,
						scale * bow * 100,
						6,
						6,
						false,
						1,
						tiling,
						tiling,
						up );
				}
				else
				{
					planeMesh = MeshManager.Instance.CreatePlane( meshName,
																  groupName,
																  plane,
																  scale * 100,
																  scale * 100,
																  1,
																  1,
																  false,
																  1,
																  tiling,
																  tiling,
																  up );
				}

				if ( this.skyPlaneEntity != null )
				{
					this.RemoveEntity( this.skyPlaneEntity );
				}

				// create entity for the plane, using the mesh name
				this.skyPlaneEntity = CreateEntity( meshName, meshName );
				this.skyPlaneEntity.MaterialName = materialName;
				// sky entities need not cast shadows
				this.skyPlaneEntity.CastShadows = false;

				if ( this.skyPlaneNode == null )
				{
					this.skyPlaneNode = this.CreateSceneNode( meshName + "Node" );
				}
				else
				{
					this.skyPlaneNode.DetachAllObjects();
				}

				// attach the skyplane to the new node
				this.skyPlaneNode.AttachObject( this.skyPlaneEntity );
			}
		}

		/// <summary>
		///		Overload.
		/// </summary>
		public virtual void SetSkyPlane( bool enable, Plane plane, string materialName )
		{
			// call the overloaded method
			this.SetSkyPlane( enable,
							  plane,
							  materialName,
							  1000.0f,
							  10.0f,
							  true,
							  0,
							  ResourceGroupManager.DefaultResourceGroupName );
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
				this.autoTrackingSceneNodes.Add( node );
			}
			else
			{
				autoTrackingSceneNodes.Remove( node.Name );
			}
		}

		/// <summary>
		/// If set, only the selected node is rendered.
		/// To render all nodes, set to null.
		/// </summary>
		///
		public void OverrideRootSceneNode( SceneNode node )
		{
			this.rootSceneNode = node;
		}

		public void RestoreRootSceneNode()
		{
			this.rootSceneNode = this.defaultRootNode;
		}

		/// <summary>
		///     Render something as if it came from the current queue.
		/// </summary>
		///<param name="pass">Material pass to use for setting up this quad.</param>
		///<param name="rend">Renderable to render</param>
		///<param name="shadowDerivation">Whether passes should be replaced with shadow caster / receiver passes</param>
		public virtual void InjectRenderWithPass( Pass pass, IRenderable rend, bool shadowDerivation )
		{
			// render something as if it came from the current queue
			Pass usedPass = this.SetPass( pass, false, shadowDerivation );
			this.RenderSingleObject( rend, usedPass, false );
		}

		public virtual void InjectRenderWithPass( Pass pass, IRenderable rend )
		{
			this.InjectRenderWithPass( pass, rend, true );
		}

	    /// <summary>
	    ///     Creates a StaticGeometry instance suitable for use with this
	    ///     SceneManager.
	    /// </summary>
	    /// <remarks>
	    ///     StaticGeometry is a way of batching up geometry into a more
	    ///     efficient form at the expense of being able to move it. Please
	    ///     read the StaticGeometry class documentation for full information.
	    /// </remarks>
	    /// <param name="name">The name to give the new object</param>
	    /// <param name="logLevel"></param>
	    /// <returns>The new StaticGeometry instance</returns>
	    public StaticGeometry CreateStaticGeometry( string name, int logLevel )
		{
			// Check not existing
			if ( this.staticGeometryList.ContainsKey( name ) )
			{
				throw new AxiomException( "StaticGeometry with name '" + name + "' already exists!" );
			}
			StaticGeometry geometry = new StaticGeometry( this, name, logLevel );
			this.staticGeometryList[ name ] = geometry;
			return geometry;
		}

		/// <summary>
		///     Retrieve a previously created StaticGeometry instance.
		/// </summary>
		/// <note>
		///     Throws an exception if the named instance does not exist
		/// </note>
		public StaticGeometry GetStaticGeometry( string name )
		{
			StaticGeometry geometry;
			if ( !this.staticGeometryList.TryGetValue( name, out geometry ) )
			{
				throw new AxiomException( "StaticGeometry with name '" + name + "' not found!" );
			}
			else
			{
				return geometry;
			}
		}

		/// <summary>
		///     Returns whether a static geometry instance with the given name exists. */
		/// </summary>
		public bool HasStaticGeometry( string name )
		{
			return this.staticGeometryList.ContainsKey( name );
		}

		/// <summary>
		///     Remove &amp; destroy a StaticGeometry instance.
		/// </summary>
		public void DestroyStaticGeometry( StaticGeometry geom )
		{
			DestroyStaticGeometry( geom.Name );
		}

		/// <summary>
		///     Remove &amp; destroy a StaticGeometry instance.
		/// </summary>
		public void DestroyStaticGeometry( string name )
		{
			StaticGeometry geometry;
			if ( !this.staticGeometryList.TryGetValue( name, out geometry ) )
			{
				throw new AxiomException( "StaticGeometry with name '" + name + "' not found!" );
			}
			else
			{
				this.staticGeometryList.Remove( name );
				geometry.Destroy();
			}
		}

		/// <summary>
		///     Destroy all StaticGeometry instances.
		/// </summary>
		public void DestroyAllStaticGeometry()
		{
			foreach ( StaticGeometry geometry in this.staticGeometryList.Values )
			{
				geometry.Destroy();
			}
			this.staticGeometryList.Clear();
		}

		#endregion Internal methods

		#region ShadowCasterSceneQueryListener Class

		/// <summary>
		///		Nested class to use as a callback for shadow caster scene query.
		/// </summary>
		protected class ShadowCasterSceneQueryListener : ISceneQueryListener
		{
			#region Fields

			protected SceneManager sceneManager;
			protected Camera camera;
			protected List<ShadowCaster> casterList = new List<ShadowCaster>();
			protected float farDistSquared;
			protected bool isLightInFrustum;
			protected Light light;
			protected PlaneBoundedVolumeList lightClipVolumeList = new PlaneBoundedVolumeList();

			#endregion Fields

			#region Constructor

			public ShadowCasterSceneQueryListener( SceneManager sceneManager )
			{
				this.sceneManager = sceneManager;
			}

			#endregion Constructor

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
			public void Prepare( bool lightInFrustum,
								 PlaneBoundedVolumeList lightClipVolumes,
								 Light light,
								 Camera camera,
								 List<ShadowCaster> shadowCasterList,
								 float farDistSquared )
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
				if ( sceneObject.CastShadows && sceneObject.IsVisible
					&& sceneManager.SpecialCaseRenderQueueList.IsRenderQueueToBeProcessed( sceneObject.RenderQueueGroup ) )
				{
					if ( this.farDistSquared > 0 )
					{
						// Check object is within the shadow far distance
						Vector3 toObj = sceneObject.ParentNode.DerivedPosition - this.camera.DerivedPosition;
						float radius = sceneObject.GetWorldBoundingSphere().Radius;
						float dist = toObj.LengthSquared;

						if ( dist - ( radius * radius ) > this.farDistSquared )
						{
							// skip, beyond max range
							return true;
						}
					}

					// If the object is in the frustum, we can always see the shadow
					if ( this.camera.IsObjectVisible( sceneObject.GetWorldBoundingBox() ) )
					{
						this.casterList.Add( sceneObject );
						return true;
					}

					// Otherwise, object can only be casting a shadow into our view if
					// the light is outside the frustum (or it's a directional light,
					// which are always outside), and the object is intersecting
					// on of the volumes formed between the edges of the frustum and the
					// light
					if ( !this.isLightInFrustum || this.light.Type == LightType.Directional )
					{
						// Iterate over volumes
						for ( int i = 0; i < this.lightClipVolumeList.Count; i++ )
						{
							PlaneBoundedVolume pbv = (PlaneBoundedVolume)this.lightClipVolumeList[ i ];

							if ( pbv.Intersects( sceneObject.GetWorldBoundingBox() ) )
							{
								this.casterList.Add( sceneObject );
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

		#region WorldGeometry

		/// <summary>
		/// Estimate the number of loading stages required to load the named
		/// world geometry.
		/// </summary>
		/// <remarks>
		/// This method should be overridden by SceneManagers that provide
		/// custom world geometry that can take some time to load. They should
		/// return from this method a count of the number of stages of progress
		/// they can report on whilst loading. During real loading (setWorldGeomtry),
		/// they should call <see name="ResourceGroupManager.notifyWorlGeometryProgress"/> exactly
		/// that number of times when loading the geometry for real.
		/// </remarks>
		/// <param name="fileName">Name of the file.</param>
		/// <returns>
		/// The default is to return 0, ie to not report progress.
		/// </returns>
		public virtual int EstimateWorldGeometry( string fileName )
		{
			return 0;
		}

		/// <summary>
		/// Estimate the number of loading stages required to load the named world geometry.
		/// </summary>
		/// <remarks>
		/// Operates just like the version of this method which takes a
		/// filename, but operates on a stream instead. Note that since the
		/// stream is updated, you'll need to reset the stream or reopen it
		/// when it comes to loading it for real.
		/// </remarks>
		/// <param name="stream">Data stream containing data to load</param>
		/// <returns></returns>
		public int EstimateWorldGeometry( Stream stream )
		{
			return this.EstimateWorldGeometry( stream, string.Empty );
		}

		/// <summary>
		/// Estimates the world geometry.
		/// </summary>
		/// <remarks>
		/// Operates just like the version of this method which takes a
		/// filename, but operates on a stream instead. Note that since the
		/// stream is updated, you'll need to reset the stream or reopen it
		/// when it comes to loading it for real.
		/// </remarks>
		/// <param name="stream">Data stream containing data to load</param>
		/// <param name="typeName">Identifies the type of world geometry
		/// contained in the stream - not required if this manager only
		/// supports one type of world geometry.</param>
		/// <returns></returns>
		public virtual int EstimateWorldGeometry( Stream stream, string typeName )
		{
			return 0;
		}

        /// <summary>
        /// Sets the source of the 'world' geometry, i.e. the large, mainly static geometry
        /// making up the world e.g. rooms, landscape etc.
        /// This function can be called before setWorldGeometry in a background thread, do to
        /// some slow tasks (e.g. IO) that do not involve the backend render system.
        /// </summary>
        /// <remarks>
        /// Depending on the type of SceneManager (subclasses will be specialised
        /// for particular world geometry types) you have requested via the Root or
        /// SceneManagerEnumerator classes, you can pass a filename to this method and it
        /// will attempt to load the world-level geometry for use. If you try to load
        /// an inappropriate type of world data an exception will be thrown. The default
        /// SceneManager cannot handle any sort of world geometry and so will always
        /// throw an exception. However subclasses like BspSceneManager can load
        /// particular types of world geometry e.g. "q3dm1.bsp".
        /// </remarks>
        /// <param name="filename"></param>
        public virtual void PrepareWorldGeometry( string filename )
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Sets the source of the 'world' geometry, i.e. the large, mainly static geometry
        /// making up the world e.g. rooms, landscape etc.
        /// This function can be called before setWorldGeometry in a background thread, do to
        /// some slow tasks (e.g. IO) that do not involve the backend render system.
        /// </summary>
        /// <remarks>
        /// Depending on the type of SceneManager (subclasses will be 
        ///	specialised for particular world geometry types) you have 
        ///	requested via the Root or SceneManagerEnumerator classes, you 
        ///	can pass a stream to this method and it will attempt to load 
        ///	the world-level geometry for use. If the manager can only 
        ///	handle one input format the typeName parameter is not required.
        ///	The stream passed will be read (and it's state updated). 
        /// </remarks>
        /// <param name="stream">Data stream containing data to load</param>
        /// <param name="typeName">String identifying the type of world geometry
        ///	contained in the stream - not required if this manager only 
        ///	supports one type of world geometry.
        ///	</param>
        public virtual void PrepareWorldGeometry( Stream stream, string typeName )
        {
            throw new NotImplementedException();
        }

		public virtual void SetWorldGeometry( string filename )
		{
		}

		public void SetWorldGeometry( Stream stream )
		{
		}

		public virtual void SetWorldGeometry( Stream stream, string typeName )
		{
		}

        #endregion WorldGeometry

		#region MovableObjectFactory methods

		public Dictionary<string, MovableObjectCollection> MovableObjectCollectionMap
		{
			get
			{
				return this.movableObjectCollectionMap;
			}
		}

		public MovableObjectCollection GetMovableObjectCollection( string typeName )
		{
			// lock collection mutex
			lock ( this.movableObjectCollectionMap )
			{
				if ( this.movableObjectCollectionMap.ContainsKey( typeName ) )
				{
					return this.movableObjectCollectionMap[ typeName ];
				}
				else
				{
					MovableObjectCollection newCol = new MovableObjectCollection();
					this.movableObjectCollectionMap.Add( typeName, newCol );
					return newCol;
				}
			}
		}

		public MovableObject CreateMovableObject( string name, string typeName, NamedParameterList para )
		{
			// Nasty hack to make generalized Camera functions work without breaking add-on SMs
			if ( typeName == "Camera" )
			{
				return this.CreateCamera( name );
			}

			// Check for duplicate names

			MovableObjectCollection objectMap = this.GetMovableObjectCollection( typeName );

			if ( objectMap.ContainsKey( name ) )
			{
				throw new AxiomException( "An object with the name " + name + " already exists in the list." );
			}

			MovableObjectFactory factory = Root.Instance.GetMovableObjectFactory( typeName );

			MovableObject newObj = factory.CreateInstance( name, this, para );
			objectMap.Add( name, newObj );
			return newObj;
		}

		public void DestroyMovableObject( string name, string typeName )
		{
			// Nasty hack to make generalized Camera functions work without breaking add-on SMs
			if ( typeName == "Camera" )
			{
				this.DestroyCamera( this.cameraList[ name ] );
				return;
			}

			MovableObjectCollection objectMap = this.GetMovableObjectCollection( typeName );

			if ( !objectMap.ContainsKey( name ) )
			{
				throw new AxiomException( "The object with the name " + name + " is not in the list." );
			}
			MovableObjectFactory factory = Root.Instance.GetMovableObjectFactory( typeName );
			MovableObject item = objectMap[ name ];
			objectMap.Remove( item.Name );
			factory.DestroyInstance( ref item );
		}

		public void DestroyAllMovableObjectsByType( string typeName )
		{
			// Nasty hack to make generalized Camera functions work without breaking add-on SMs
			if ( typeName == "Camera" )
			{
				this.DestroyAllCameras();
				return;
			}

			MovableObjectCollection objectMap = this.GetMovableObjectCollection( typeName );

			MovableObjectFactory factory = Root.Instance.GetMovableObjectFactory( typeName );

			lock ( objectMap )
			{
				foreach ( MovableObject movableObject in objectMap.Values )
				{
					if ( movableObject.Manager == this )
					{
						MovableObject tmp = movableObject;
						factory.DestroyInstance( ref tmp );
					}
				}
				objectMap.Clear();
			}
		}

		public void DestroyAllMovableObjects()
		{
			foreach ( KeyValuePair<string, MovableObjectCollection> col in this.movableObjectCollectionMap )
			{
				MovableObjectCollection coll = col.Value;
				lock ( coll )
				{
					if ( Root.Instance.HasMovableObjectFactory( col.Key ) )
					{
						// Only destroy if we have a factory instance; otherwise must be injected
						MovableObjectFactory factory = Root.Instance.GetMovableObjectFactory( col.Key );
						foreach ( MovableObject movableObject in coll.Values )
						{
							if ( movableObject.Manager == this )
							{
								MovableObject tmp = movableObject;
								factory.DestroyInstance( ref tmp );
							}
						}
						coll.Clear();
					}
				}
			}
			this.movableObjectCollectionMap.Clear();
		}

		public MovableObject GetMovableObject( string name, string typeName )
		{
			// Nasty hack to make generalized Camera functions work without breaking add-on SMs
			if ( typeName == "Camera" )
			{
				return this.cameraList[ name ];
			}

			if ( !this.movableObjectCollectionMap.ContainsKey( typeName ) )
			{
				throw new AxiomException( "The factory for the type " + typeName + " does not exists in the collection." );
			}

			if ( !this.movableObjectCollectionMap[ typeName ].ContainsKey( name ) )
			{
				throw new AxiomException( "The object with the name " + name + " does not exists in the collection." );
			}

			return this.movableObjectCollectionMap[ typeName ][ name ];
		}

		public bool HasMovableObject( string name, string typeName )
		{
			// Nasty hack to make generalized Camera functions work without breaking add-on SMs
			if ( typeName == "Camera" )
			{
				return this.cameraList.ContainsKey( name );
			}

			return this.movableObjectCollectionMap.ContainsKey( typeName )
				   && this.movableObjectCollectionMap[ typeName ].ContainsKey( name );
		}

		public void DestroyMovableObject( MovableObject m )
		{
			this.DestroyMovableObject( m.Name, m.MovableType );
		}

		public void InjectMovableObject( MovableObject m )
		{
			MovableObjectCollection objectMap = this.GetMovableObjectCollection( m.MovableType );
			{
				lock ( objectMap )
				{
					if ( !objectMap.ContainsKey( m.Name ) )
					{
						objectMap.Add( m );
					}
				}
			}
		}

		public void ExtractMovableObject( string name, string typeName )
		{
			MovableObjectCollection objectMap = this.GetMovableObjectCollection( typeName );
			lock ( objectMap )
			{
				objectMap.Remove( name );
			}
		}

		public void ExtractMovableObject( MovableObject m )
		{
			this.ExtractMovableObject( m.Name, m.MovableType );
		}

		public void ExtractAllMovableObjectsByType( string typeName )
		{
			MovableObjectCollection objectMap = this.GetMovableObjectCollection( typeName );
			lock ( objectMap )
			{
				objectMap.Clear();
			}
		}

		#endregion MovableObjectFactory methods
    }

	#region Default SceneQuery Implementations

	/// <summary>
	///		Default implementation of a AxisAlignedBoxRegionSceneQuery.
	/// </summary>
	public class DefaultAxisAlignedBoxRegionSceneQuery : AxisAlignedBoxRegionSceneQuery
	{
		protected internal DefaultAxisAlignedBoxRegionSceneQuery( SceneManager creator )
			: base( creator )
		{
			// No world geometry results supported
			this.AddWorldFragmentType( WorldFragmentType.None );
		}

		public override void Execute( ISceneQueryListener listener )
		{
			MovableObjectFactoryMap factories = Root.Instance.MovableObjectFactories;
			foreach ( KeyValuePair<string, MovableObjectFactory> map in factories )
			{
				MovableObjectCollection movableObjects = this.creator.GetMovableObjectCollection( map.Value.Type );
				foreach ( MovableObject movableObject in movableObjects.Values )
				{
					// skip group if query type doesn't match
					if ( ( this.QueryTypeMask & movableObject.TypeFlags ) == 0 )
					{
						break;
					}

					// skip if unattached or filtered out by query flags
					if ( !movableObject.IsAttached || ( movableObject.QueryFlags & this.queryMask ) == 0 )
					{
						continue;
					}

					if ( this.box.Intersects( movableObject.GetWorldBoundingBox() ) )
					{
						listener.OnQueryResult( movableObject );
					}
				}
			}
		}
	}

	/// <summary>
	///    Default implementation of RaySceneQuery.
	/// </summary>
	/// <remarks>
	/// Note that becuase we have no scene partitioning, we actually
	/// perform a complete scene search even if restricted results are
	/// requested; smarter scene manager queries can utilise the paritioning
	/// of the scene in order to reduce the number of intersection tests
	/// required to fulfil the query
	/// </remarks>
	public class DefaultRaySceneQuery : RaySceneQuery
	{
		protected internal DefaultRaySceneQuery( SceneManager creator )
			: base( creator )
		{
			// No world geometry results supported
			this.AddWorldFragmentType( WorldFragmentType.None );
		}

		public override void Execute( IRaySceneQueryListener listener )
		{
			MovableObjectFactoryMap factories = Root.Instance.MovableObjectFactories;
			foreach ( KeyValuePair<string, MovableObjectFactory> map in factories )
			{
				MovableObjectCollection movableObjects = this.creator.GetMovableObjectCollection( map.Value.Type );
				foreach ( MovableObject movableObject in movableObjects.Values )
				{
					// skip group if query type doesn't match
					if ( ( this.QueryTypeMask & movableObject.TypeFlags ) == 0 )
					{
						break;
					}

					// skip if unattached or filtered out by query flags
					if ( !movableObject.IsAttached || ( movableObject.QueryFlags & this.queryMask ) == 0 )
					{
						continue;
					}

					// test the intersection against the world bounding box of the entity
					IntersectResult results = Utility.Intersects( this.ray, movableObject.GetWorldBoundingBox() );

					// if the results came back positive, fire the event handler
					if ( results.Hit == true )
					{
						listener.OnQueryResult( movableObject, results.Distance );
					}
				}
			}
		}
	}

	/// <summary>
	///		Default implementation of a SphereRegionSceneQuery.
	/// </summary>
	public class DefaultSphereRegionSceneQuery : SphereRegionSceneQuery
	{
		protected internal DefaultSphereRegionSceneQuery( SceneManager creator )
			: base( creator )
		{
			// No world geometry results supported
			this.AddWorldFragmentType( WorldFragmentType.None );
		}

		public override void Execute( ISceneQueryListener listener )
		{
			Sphere testSphere = new Sphere();

			MovableObjectFactoryMap factories = Root.Instance.MovableObjectFactories;
			foreach ( KeyValuePair<string, MovableObjectFactory> map in factories )
			{
				MovableObjectCollection movableObjects = this.creator.GetMovableObjectCollection( map.Value.Type );
				foreach ( MovableObject movableObject in movableObjects.Values )
				{
					// skip group if query type doesn't match
					if ( ( this.QueryTypeMask & movableObject.TypeFlags ) == 0 )
					{
						break;
					}

					// skip if unattached or filtered out by query flags
					if ( !movableObject.IsAttached || ( movableObject.QueryFlags & this.queryMask ) == 0 )
					{
						continue;
					}

					testSphere.Center = movableObject.ParentNode.DerivedPosition;
					testSphere.Radius = movableObject.BoundingRadius;

					// if the results came back positive, fire the event handler
					if ( this.sphere.Intersects( testSphere ) )
					{
						listener.OnQueryResult( movableObject );
					}
				}
			}
		}
	}

	/// <summary>
	///		Default implementation of a PlaneBoundedVolumeListSceneQuery.
	/// </summary>
	public class DefaultPlaneBoundedVolumeListSceneQuery : PlaneBoundedVolumeListSceneQuery
	{
		protected internal DefaultPlaneBoundedVolumeListSceneQuery( SceneManager creator )
			: base( creator )
		{
			// No world geometry results supported
			this.AddWorldFragmentType( WorldFragmentType.None );
		}

		public override void Execute( ISceneQueryListener listener )
		{
			MovableObjectFactoryMap factories = Root.Instance.MovableObjectFactories;
			foreach ( KeyValuePair<string, MovableObjectFactory> map in factories )
			{
				MovableObjectCollection movableObjects = this.creator.GetMovableObjectCollection( map.Value.Type );
				foreach ( MovableObject movableObject in movableObjects.Values )
				{
					// skip group if query type doesn't match
					if ( ( this.QueryTypeMask & movableObject.TypeFlags ) == 0 )
					{
						break;
					}

					// skip if unattached or filtered out by query flags
					if ( !movableObject.IsAttached || ( movableObject.QueryFlags & this.queryMask ) == 0 )
					{
						continue;
					}

					for ( int v = 0; v < this.volumes.Count; v++ )
					{
						PlaneBoundedVolume volume = (PlaneBoundedVolume)this.volumes[ v ];
						// Do AABB / plane volume test
						if ( ( movableObject.QueryFlags & this.queryMask ) != 0
							 && volume.Intersects( movableObject.GetWorldBoundingBox() ) )
						{
							listener.OnQueryResult( movableObject );
							break;
						}
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
		protected internal DefaultIntersectionSceneQuery( SceneManager creator )
			: base( creator )
		{
			// No world geometry results supported
			this.AddWorldFragmentType( WorldFragmentType.None );
		}

		public override void Execute( IIntersectionSceneQueryListener listener )
		{
			MovableObjectFactoryMap factories = Root.Instance.MovableObjectFactories;
			IEnumerator enumFactories = factories.GetEnumerator();
			while ( enumFactories.Current != null )
			{
				KeyValuePair<string, MovableObjectFactory> map = (KeyValuePair<string, MovableObjectFactory>)enumFactories.Current;
				MovableObjectCollection movableObjects = this.creator.GetMovableObjectCollection( map.Value.Type );
				IEnumerator enumA = movableObjects.GetEnumerator();
				while ( enumA.Current != null )
				{
					MovableObject objectA = (MovableObject)enumA.Current;
					// skip group if query type doesn't match
					if ( ( this.QueryTypeMask & objectA.TypeFlags ) == 0 )
					{
						break;
					}

					// skip if unattached or filtered out by query flags
					if ( !objectA.IsInScene || ( objectA.QueryFlags & this.queryMask ) == 0 )
					{
						continue;
					}

					// Check against later objects in the same group
					IEnumerator enumB = enumA;
					while ( enumB.Current != null )
					{
						MovableObject objectB = (MovableObject)enumB.Current;
						if ( ( ( this.QueryMask & objectB.QueryFlags ) != 0 ) && objectB.IsInScene )
						{
							AxisAlignedBox box1 = objectA.GetWorldBoundingBox();
							AxisAlignedBox box2 = objectB.GetWorldBoundingBox();

							if ( box1.Intersects( box2 ) )
							{
								listener.OnQueryResult( objectA, objectB );
							}
						}
						enumB.MoveNext();
					}

					// Check  against later groups
					IEnumerator enumFactoriesLater = enumFactories;
					while ( enumFactoriesLater.Current != null )
					{
						KeyValuePair<string, MovableObjectFactory> mapLater = (KeyValuePair<string, MovableObjectFactory>)enumFactoriesLater.Current;

						MovableObjectCollection movableObjectsLater = this.creator.GetMovableObjectCollection( mapLater.Value.Type );
						IEnumerator enumC = movableObjectsLater.GetEnumerator();
						while ( enumC.Current != null )
						{
							MovableObject objectC = (MovableObject)enumC.Current;
							// skip group if query type doesn't match
							if ( ( this.QueryTypeMask & objectC.TypeFlags ) == 0 )
							{
								break;
							}

							if ( ( ( this.QueryMask & objectC.QueryFlags ) != 0 ) && objectC.IsInScene )
							{
								AxisAlignedBox box1 = objectA.GetWorldBoundingBox();
								AxisAlignedBox box2 = objectC.GetWorldBoundingBox();

								if ( box1.Intersects( box2 ) )
								{
									listener.OnQueryResult( objectA, objectC );
								}
							}
							enumC.MoveNext();
						}
						enumFactoriesLater.MoveNext();
					}
					enumA.MoveNext();
				}
				enumFactories.MoveNext();
			}
		}
	}

	#endregion Default SceneQuery Implementations

	/// <summary>
	///     Structure for holding a position &amp; orientation pair.
	/// </summary>
	public struct ViewPoint
	{
		public Quaternion orientation;
		public Vector3 position;
	}

	/// <summary>
	///		Structure containing information about a scene manager.
	/// </summary>
	public struct SceneManagerMetaData
	{
		/// <summary>
		///		A text description of the scene manager.
		/// </summary>
		public string description;

		/// <summary>
		///		A mask describing which sorts of scenes this manager can handle.
		/// </summary>
		public SceneType sceneTypeMask;

		/// <summary>
		///		A globally unique string identifying the scene manager type.
		/// </summary>
		public string typeName;

		///<summary>
		///		Flag indicating whether world geometry is supported.
		/// </summary>
		public bool worldGeometrySupported;
	}

	/// <summary>
	///		Class which will create instances of a given SceneManager.
	/// </summary>
	public abstract class SceneManagerFactory
	{
		/// <summary>
		///
		/// </summary>
		protected SceneManagerMetaData metaData;

		/// <summary>
		///
		/// </summary>
		protected bool metaDataInit = true;

		/// <summary>
		///		Gets information about the SceneManager type created by this factory.
		/// </summary>
		public virtual SceneManagerMetaData MetaData
		{
			get
			{
				if ( this.metaDataInit )
				{
					this.InitMetaData();
					this.metaDataInit = false;
				}
				return this.metaData;
			}
		}

		/// <summary>
		///		Internal method to initialise the metadata, must be implemented.
		/// </summary>
		protected abstract void InitMetaData();

		/// <summary>
		///		Creates a new instance of a SceneManager.
		/// </summary>
		/// <remarks>
		///		Don't call directly, use SceneManagerEnumerator.CreateSceneManager.
		/// </remarks>
		/// <param name="name"></param>
		/// <returns></returns>
		public abstract SceneManager CreateInstance( string name );

		/// <summary>
		///		Destroys an instance of a SceneManager.
		/// </summary>
		/// <param name="instance"></param>
		public abstract void DestroyInstance( SceneManager instance );
	}
}