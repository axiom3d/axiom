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

	/// <summary>
	/// Delegate for scenemanager destroyed events
	/// </summary>
	/// <param name="manager"></param>
	public delegate void SceneManagerDestroyedEvent( SceneManager manager );

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
		private bool cameraRelativeRendering = false; // implement logic

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
		protected Real fogDensity;
		protected Real fogEnd;
		protected FogMode fogMode;
		protected Real fogStart;

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
				return specialCaseRenderQueueList;
			}
		}

		protected RenderQueueGroupID worldGeometryRenderQueueId = RenderQueueGroupID.WorldGeometryOne;

		public virtual RenderQueueGroupID WorldGeometryRenderQueueId
		{
			get
			{
				return worldGeometryRenderQueueId;
			}

			set
			{
				worldGeometryRenderQueueId = value;
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

		protected Entity[] skyBoxEntities = new Entity[6];
		protected SceneNode skyBoxNode;
		protected Quaternion skyBoxOrientation;
		protected Entity[] skyDomeEntities = new Entity[5];
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

		protected Matrix4[] xform = new Matrix4[256];

		#region MovableObjectfactory fields

		protected readonly Dictionary<string, MovableObjectCollection> movableObjectCollectionMap =
			new Dictionary<string, MovableObjectCollection>();

		protected NameGenerator<MovableObject> movableNameGenerator = new NameGenerator<MovableObject>();

		#endregion MovableObjectfactory fields

		/// <summary>
		/// If set, materials will be resolved from the materials at the
		/// pass-setting stage and not at the render queue building stage.
		/// This is useful when the active material scheme during the render
		/// queue building stage is different from the one during the rendering stage.
		/// </summary>
		public bool IsLateMaterialResolving { get; set; }

		public ICollection<Camera> Cameras
		{
			get
			{
				return cameraList.Values;
			}
		}

		/// <summary>A list of lights in the scene for easy lookup.</summary>
		public ICollection<MovableObject> Lights
		{
			get
			{
				return GetMovableObjectCollection( LightFactory.TypeName ).Values;
			}
		}

		public ICollection<SceneNode> SceneNodes
		{
			get
			{
				return sceneNodeList.Values;
			}
		}

		/// <summary>
		///		If true, the shadow technique is based on texture maps
		/// </summary>
		public bool IsShadowTechniqueStencilBased
		{
			get
			{
				return shadowTechnique == ShadowTechnique.StencilModulative || shadowTechnique == ShadowTechnique.StencilAdditive;
			}
		}

		/// <summary>
		///		If true, the shadow technique is based on texture maps
		/// </summary>
		public bool IsShadowTechniqueTextureBased
		{
			get
			{
				return shadowTechnique == ShadowTechnique.TextureModulative || shadowTechnique == ShadowTechnique.TextureAdditive;
			}
		}

		/// <summary>
		///		If true, the shadow technique is additive
		/// </summary>
		public bool IsShadowTechniqueAdditive
		{
			get
			{
				return shadowTechnique == ShadowTechnique.StencilAdditive || shadowTechnique == ShadowTechnique.TextureAdditive;
			}
		}

		/// <summary>
		///		If true, the shadow technique is modulative
		/// </summary>
		protected bool IsShadowTechniqueModulative
		{
			get
			{
				return shadowTechnique == ShadowTechnique.StencilModulative || shadowTechnique == ShadowTechnique.TextureModulative;
			}
		}

		/// <summary>
		///		Is the shadow technique is not "None"
		/// </summary>
		public bool IsShadowTechniqueInUse
		{
			get
			{
				return shadowTechnique != ShadowTechnique.None;
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

		private readonly ChainedEvent<BeginRenderQueueEventArgs> _queueStartedEvent =
			new ChainedEvent<BeginRenderQueueEventArgs>();

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

		/// <summary>
		/// Event notifying the listener of the SceneManager's destruction.
		/// </summary>
		public event SceneManagerDestroyedEvent SceneManagerDestroyed;

		#endregion Public events

		#region Constructors

		public AutoParamDataSource AutoParamData
		{
			get
			{
				return autoParamDataSource;
			}
		}

		public SceneManager( string name )
			: base()
		{
			cameraList = new CameraCollection();
			sceneNodeList = new SceneNodeCollection();
			animationList = new AnimationCollection();
			animationStateList = new AnimationStateSet();
			regionList = new List<StaticGeometry.Region>();

			shadowCasterQueryListener = new ShadowCasterSceneQueryListener( this );

			// create the root scene node
			rootSceneNode = new SceneNode( this, "Root" );
			rootSceneNode.SetAsRootNode();
			defaultRootNode = rootSceneNode;

			this.name = name;

			// default to no fog
			fogMode = FogMode.None;

			// no shadows by default
			shadowTechnique = ShadowTechnique.None;

			// setup default shadow camera setup
			_defaultShadowCameraSetup = new DefaultShadowCameraSetup();

			illuminationStage = IlluminationRenderStage.None;
			renderingNoShadowQueue = false;
			renderingMainGroup = false;
			shadowColor.a = shadowColor.r = shadowColor.g = shadowColor.b = 0.25f;
			shadowDirLightExtrudeDist = 10000;
			shadowIndexBufferSize = 51200;
			shadowTextureOffset = 0.6f;
			shadowTextureFadeStart = 0.7f;
			shadowTextureFadeEnd = 0.9f;
			shadowTextureSize = 512;
			shadowTextureCount = 1;
			findVisibleObjects = true;
			suppressRenderStateChanges = false;
			suppressShadows = false;
			shadowUseInfiniteFarPlane = true;
		}

		#endregion Constructors

		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				if ( disposeManagedResources )
				{
					if ( SceneManagerDestroyed != null )
					{
						SceneManagerDestroyed( this );
					}

					ClearScene();
					RemoveAllCameras();

					if ( op != null )
					{
						if ( !op.IsDisposed )
						{
							op.Dispose();
						}

						op = null;
					}

					if ( autoParamDataSource != null )
					{
						if ( !autoParamDataSource.IsDisposed )
						{
							autoParamDataSource.Dispose();
						}

						autoParamDataSource = null;
					}

					if ( rootSceneNode != null )
					{
						if ( !rootSceneNode.IsDisposed )
						{
							rootSceneNode.Dispose();
						}

						rootSceneNode = null;
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
			var node = new SceneNode( this );
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
		/// <returns></returns>
		public virtual SceneNode CreateSceneNode( string name )
		{
			var node = new SceneNode( this, name );
			sceneNodeList.Add( node );
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
			var anim = new Animation( name, length );
			animationList.Add( name, anim );

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
			if ( animationStateList.HasAnimationState( animationName ) )
			{
				throw new AxiomException( "Cannot create, AnimationState already exists: " + animationName );
			}

			if ( !animationList.ContainsKey( animationName ) )
			{
				throw new AxiomException(
					string.Format(
						"The name of a valid animation must be supplied when creating an AnimationState.  Animation '{0}' does not exist.",
						animationName ) );
			}

			// get a reference to the sepcified animation
			var anim = animationList[ animationName ];

			// create and return new animation state
			return animationStateList.CreateAnimationState( animationName, 0, anim.Length );
		}

		/// <summary>
		///	Creates a new BillboardSet for use with this scene manager.
		/// </summary>
		/// <remarks>
		/// This method creates a new BillboardSet which is registered with
		/// the SceneManager. The SceneManager will destroy this object when
		/// it shuts down or when the SceneManager::clearScene method is
		/// called, so the caller does not have to worry about destroying
		/// this object (in fact, it definitely should not do this).
		/// @par
		/// See the BillboardSet documentations for full details of the
		/// returned class.
		/// </remarks>
		/// <param name="name">The name to give to this billboard set. Must be unique.</param>
		/// <param name="poolSize">The initial size of the pool of billboards (see BillboardSet for more information)</param>
		/// <see cref="BillboardSet"/>
		[OgreVersion( 1, 7, 2 )]
		public virtual BillboardSet CreateBillboardSet( string name, uint poolSize )
		{
			var param = new NamedParameterList();
			param.Add( "poolSize", poolSize.ToString() );
			return (BillboardSet)CreateMovableObject( name, BillboardSetFactory.TypeName, param );
		}

		/// <summary>
		/// Creates a new BillboardSet for use with this scene manager, with a generated name.
		/// </summary>
		/// <param name="poolSize">The initial size of the pool of billboards (see BillboardSet for more information)</param>
		/// <see cref="BillboardSet"/>
		[OgreVersion( 1, 7, 2 )]
		public virtual BillboardSet CreateBillboardSet( uint poolSize )
		{
			string name = movableNameGenerator.GetNextUniqueName();
			return CreateBillboardSet( name, poolSize );
		}

		public BillboardSet CreateBillboardSet()
		{
			return CreateBillboardSet( 20 );
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
			var camera = new Camera( name, this );
			cameraList.Add( camera );

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
			var param = new NamedParameterList();
			param.Add( "mesh", meshName );
			return (Entity)CreateMovableObject( name, EntityFactory.TypeName, param );
		}

		/// <summary>
		///		Create an Entity (instance of a discrete mesh).
		/// </summary>
		/// <param name="name">The name to be given to the entity (must be unique).</param>
		/// <param name="mesh">The mesh to use.</param>
		/// <returns></returns>
		public virtual Entity CreateEntity( string name, Mesh mesh )
		{
			var param = new NamedParameterList();
			param.Add( "mesh", mesh );
			return (Entity)CreateMovableObject( name, EntityFactory.TypeName, param );
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
					return CreateEntity( name, "Prefab_Plane" );
				case PrefabEntity.Cube:
					return CreateEntity( name, "Prefab_Cube" );
				case PrefabEntity.Sphere:
					return CreateEntity( name, "Prefab_Sphere" );
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
		[OgreVersion( 1, 7, 2 )]
		public virtual Light CreateLight( string name )
		{
			return (Light)CreateMovableObject( name, LightFactory.TypeName, null );
		}

		/// <summary>
		///  Creates a light with a generated name.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public virtual Light CreateLight()
		{
			string name = movableNameGenerator.GetNextUniqueName();
			return CreateLight( name );
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
			return (MovableText)CreateMovableObject( name, MovableTextFactory.TypeName, new NamedParameterList()
			                                                                            {
			                                                                            	{
			                                                                            		"caption", caption
			                                                                            		},
			                                                                            	{
			                                                                            		"fontName", fontName
			                                                                            		}
			                                                                            } );
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
			return (MovableText)GetMovableObject( name, MovableTextFactory.TypeName );
		}

		/// <summary>
		///     Create a ManualObject, an object which you populate with geometry
		///     manually through a GL immediate-mode style interface.
		/// </summary>
		/// <param name="name">
		///     The name to be given to the object (must be unique).
		/// </param>
		[OgreVersion( 1, 7, 2 )]
		public virtual ManualObject CreateManualObject( string name )
		{
			return (ManualObject)CreateMovableObject( name, ManualObjectFactory.TypeName, null );
		}

		/// <summary>
		/// Create a ManualObject, an object which you populate with geometry
		/// manually through a GL immediate-mode style interface, generating the name.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public virtual ManualObject CreateManualObject()
		{
			string name = movableNameGenerator.GetNextUniqueName();
			return CreateManualObject( name );
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
			return (ManualObject)GetMovableObject( name, ManualObjectFactory.TypeName );
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
			var newOverlay = (Overlay)OverlayManager.Instance.Create( name );
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
			DestroyAllMovableObjects();

			if ( rootSceneNode != null )
			{
				rootSceneNode.RemoveAllChildren();
				rootSceneNode.DetachAllObjects();
			}

			// Delete all SceneNodes, except root that is
			foreach ( Node node in sceneNodeList )
			{
				foreach ( var currentNode in sceneNodeList.Values )
				{
					if ( !currentNode.IsDisposed )
					{
						currentNode.Dispose();
					}
				}
			}
			sceneNodeList.Clear();

			if ( autoTrackingSceneNodes != null )
			{
				autoTrackingSceneNodes.Clear();
			}

			// Clear animations
			DestroyAllAnimations();

			// Remove sky nodes since they've been deleted
			skyBoxNode = skyPlaneNode = skyDomeNode = null;
			isSkyBoxEnabled = isSkyPlaneEnabled = isSkyDomeEnabled = false;

			if ( renderQueue != null )
			{
				renderQueue.Clear();
			}
		}

		/// <summary>
		///    Destroys and removes a node from the scene.
		/// </summary>
		/// <param name="name"></param>
		public virtual void DestroySceneNode( string name )
		{
			SceneNode node;
			if ( !sceneNodeList.TryGetValue( name, out node ) )
			{
				throw new AxiomException( "SceneNode named '{0}' not found.", name );
			}

			DestroySceneNode( node );
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
			foreach ( var autoNode in autoTrackingSceneNodes.Values )
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
			animationStateList.RemoveAnimationState( name );
			var animation = animationList[ name ];
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
			var animationState = animationStateList.GetAnimationState( name );
			if ( animationState == null )
			{
				throw new AxiomException( "AnimationState named '{0}' not found.", name );
			}

			animationStateList.RemoveAnimationState( name );
		}

		/// <summary>
		///		Removes all animations created using this SceneManager.
		/// </summary>
		public virtual void DestroyAllAnimations()
		{
			// Destroy all states too, since they cannot reference destroyed animations
			DestroyAllAnimationStates();
			if ( animationList != null )
			{
				animationList.Clear();
			}
		}

		/// <summary>
		///		Removes all AnimationStates created using this SceneManager.
		/// </summary>
		public virtual void DestroyAllAnimationStates()
		{
			if ( animationStateList != null )
			{
				animationStateList.RemoveAllAnimationStates();
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
			var overlay = OverlayManager.Instance.GetByName( name );

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
			var camera = cameraList[ name ];
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
			return (Light)GetMovableObject( name, LightFactory.TypeName );
		}

		/// <summary>
		///     Retreives the BillboardSet with the specified name.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public virtual BillboardSet GetBillboardSet( string name )
		{
			return (BillboardSet)GetMovableObject( name, BillboardSetFactory.TypeName );
		}

		/// <summary>
		///     Retreives the animation with the specified name.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public virtual Animation GetAnimation( string name )
		{
			var animation = animationList[ name ];
			return animation;
		}

		/// <summary>
		///     Retreives the AnimationState with the specified name.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public virtual AnimationState GetAnimationState( string name )
		{
			var animationState = animationStateList.GetAnimationState( name );
			return animationState;
		}

		/// <summary>
		///		Gets a the named Overlay, previously created using CreateOverlay.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public virtual Overlay GetOverlay( string name )
		{
			var overlay = OverlayManager.Instance.GetByName( name );
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
			var node = sceneNodeList[ name ];
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
			return (Entity)GetMovableObject( name, EntityFactory.TypeName );
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

		public void ManualRender( RenderOperation op, Pass pass, Viewport vp, Matrix4 worldMatrix, Matrix4 viewMatrix,
		                          Matrix4 projMatrix )
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
		public virtual void ManualRender( RenderOperation op, Pass pass, Viewport vp, Matrix4 worldMatrix, Matrix4 viewMatrix,
		                                  Matrix4 projMatrix, bool doBeginEndFrame )
		{
			// configure all necessary parameters
			targetRenderSystem.Viewport = vp;
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

		public void ResetViewProjectionMode()
		{
			if ( lastViewWasIdentity )
			{
				// Coming back to normal from identity view
				targetRenderSystem.ViewMatrix = cameraInProgress.ViewMatrix;
				lastViewWasIdentity = false;
			}

			if ( lastProjectionWasIdentity )
			{
				// Coming back from flat projection
				targetRenderSystem.ProjectionMatrix = cameraInProgress.ProjectionMatrixRS;
				lastProjectionWasIdentity = false;
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
			lightsAffectingFrustum.Clear();

			var lightList = GetMovableObjectCollection( LightFactory.TypeName );

			// sphere to use for testing
			var sphere = new Sphere();

			foreach ( Light light in lightList.Values )
			{
				if ( cameraRelativeRendering )
				{
					light.CameraRelative = cameraInProgress;
				}
				else
				{
					light.CameraRelative = null;
				}

				if ( light.IsVisible )
				{
					if ( light.Type == LightType.Directional )
					{
						// Always visible
						lightsAffectingFrustum.Add( light );
					}
					else
					{
						// treating spotlight as point for simplicity
						// Just see if the lights attenuation range is within the frustum
						sphere.Center = light.GetDerivedPosition();
						sphere.Radius = light.AttenuationRange;

						if ( camera.IsObjectVisible( sphere ) )
						{
							lightsAffectingFrustum.Add( light );
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
			shadowCasterList.Clear();

			if ( light.Type == LightType.Directional )
			{
				// Basic AABB query encompassing the frustum and the extrusion of it
				var aabb = new AxisAlignedBox();
				var corners = camera.WorldSpaceCorners;
				Vector3 min, max;
				var extrude = light.DerivedDirection*-shadowDirLightExtrudeDist;
				// do first corner
				min = max = corners[ 0 ];
				min.Floor( corners[ 0 ] + extrude );
				max.Ceil( corners[ 0 ] + extrude );
				for ( var c = 1; c < 8; ++c )
				{
					min.Floor( corners[ c ] );
					max.Ceil( corners[ c ] );
					min.Floor( corners[ c ] + extrude );
					max.Ceil( corners[ c ] + extrude );
				}
				aabb.SetExtents( min, max );

				if ( shadowCasterAABBQuery == null )
				{
					shadowCasterAABBQuery = CreateAABBRegionQuery( aabb );
				}
				else
				{
					shadowCasterAABBQuery.Box = aabb;
				}
				// Execute, use callback
				shadowCasterQueryListener.Prepare( false, light.GetFrustumClipVolumes( camera ), light, camera, shadowCasterList,
				                                   light.ShadowFarDistanceSquared );
				shadowCasterAABBQuery.Execute( shadowCasterQueryListener );
			}
			else
			{
				var s = new Sphere( light.GetDerivedPosition(), light.AttenuationRange );

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
					var lightInFrustum = camera.IsObjectVisible( light.GetDerivedPosition() );

					PlaneBoundedVolumeList volumeList = null;

					// Only worth building an external volume list if
					// light is outside the frustum
					if ( !lightInFrustum )
					{
						volumeList = light.GetFrustumClipVolumes( camera );
					}

					// prepare the query and execute using the callback
					shadowCasterQueryListener.Prepare( lightInFrustum, volumeList, light, camera, shadowCasterList,
					                                   light.ShadowFarDistanceSquared );

					shadowCasterSphereQuery.Execute( shadowCasterQueryListener );
				}
			}

			return shadowCasterList;
		}

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
				InitShadowReceiverPass();
			}

			// InitShadowReceiverPass up spot shadow fade texture (loaded from code data block)
			var spotShadowFadeTex = TextureManager.Instance[ SPOT_SHADOW_FADE_IMAGE ];

			if ( spotShadowFadeTex == null )
			{
				// Load the manual buffer into an image
				var imgStream = new MemoryStream( SpotShadowFadePng.SPOT_SHADOW_FADE_PNG );
				var img = Image.FromStream( imgStream, "png" );
				spotShadowFadeTex = TextureManager.Instance.LoadImage( SPOT_SHADOW_FADE_IMAGE,
				                                                       ResourceGroupManager.InternalResourceGroupName, img,
				                                                       TextureType.TwoD );
			}

			shadowMaterialInitDone = true;
		}

		private void InitShadowReceiverPass()
		{
			var matShadRec = (Material)MaterialManager.Instance[ TEXTURE_SHADOW_RECEIVER_MATERIAL ];

			if ( matShadRec == null )
			{
				matShadRec =
					(Material)
					MaterialManager.Instance.Create( TEXTURE_SHADOW_RECEIVER_MATERIAL, ResourceGroupManager.InternalResourceGroupName );
				shadowReceiverPass = matShadRec.GetTechnique( 0 ).GetPass( 0 );
				shadowReceiverPass.SetSceneBlending( SceneBlendFactor.DestColor, SceneBlendFactor.Zero );
				// Don't set lighting and blending modes here, depends on additive / modulative
				var t = shadowReceiverPass.CreateTextureUnitState();
				t.SetTextureAddressingMode( TextureAddressing.Clamp );
			}
			else
			{
				shadowReceiverPass = matShadRec.GetTechnique( 0 ).GetPass( 0 );
			}
		}

		private void InitShadowCasterPass()
		{
			var matPlainBlack = (Material)MaterialManager.Instance[ TEXTURE_SHADOW_CASTER_MATERIAL ];

			if ( matPlainBlack == null )
			{
				matPlainBlack =
					(Material)
					MaterialManager.Instance.Create( TEXTURE_SHADOW_CASTER_MATERIAL, ResourceGroupManager.InternalResourceGroupName );
				shadowCasterPlainBlackPass = matPlainBlack.GetTechnique( 0 ).GetPass( 0 );
				// Lighting has to be on, because we need shadow coloured objects
				// Note that because we can't predict vertex programs, we'll have to
				// bind light values to those, and so we bind White to ambient
				// reflectance, and we'll set the ambient colour to the shadow colour
				shadowCasterPlainBlackPass.Ambient = ColorEx.White;
				shadowCasterPlainBlackPass.Diffuse = ColorEx.Black;
				shadowCasterPlainBlackPass.SelfIllumination = ColorEx.Black;
				shadowCasterPlainBlackPass.Specular = ColorEx.Black;
				// Override fog
				shadowCasterPlainBlackPass.SetFog( true, FogMode.None );
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
			var matModStencil = (Material)MaterialManager.Instance[ STENCIL_SHADOW_MODULATIVE_MATERIAL ];

			if ( matModStencil == null )
			{
				// Create
				matModStencil =
					(Material)
					MaterialManager.Instance.Create( STENCIL_SHADOW_MODULATIVE_MATERIAL, ResourceGroupManager.InternalResourceGroupName );

				shadowModulativePass = matModStencil.GetTechnique( 0 ).GetPass( 0 );
				shadowModulativePass.SetSceneBlending( SceneBlendFactor.DestColor, SceneBlendFactor.Zero );
				shadowModulativePass.LightingEnabled = false;
				shadowModulativePass.DepthWrite = false;
				shadowModulativePass.DepthCheck = false;
				var t = shadowModulativePass.CreateTextureUnitState();
				t.SetColorOperationEx( LayerBlendOperationEx.Modulate, LayerBlendSource.Manual, LayerBlendSource.Current,
				                       shadowColor );
				shadowModulativePass.CullingMode = CullingMode.None;
			}
			else
			{
				shadowModulativePass = matModStencil.GetTechnique( 0 ).GetPass( 0 );
			}
		}

		private void InitShadowDebugPass()
		{
			var matDebug = (Material)MaterialManager.Instance[ SHADOW_VOLUMES_MATERIAL ];

			if ( matDebug == null )
			{
				// Create
				matDebug =
					(Material)
					MaterialManager.Instance.Create( SHADOW_VOLUMES_MATERIAL, ResourceGroupManager.InternalResourceGroupName );
				shadowDebugPass = matDebug.GetTechnique( 0 ).GetPass( 0 );
				shadowDebugPass.SetSceneBlending( SceneBlendType.Add );
				shadowDebugPass.LightingEnabled = false;
				shadowDebugPass.DepthWrite = false;
				var t = shadowDebugPass.CreateTextureUnitState();
				t.SetColorOperationEx( LayerBlendOperationEx.Modulate, LayerBlendSource.Manual, LayerBlendSource.Current,
				                       new ColorEx( 0.7f, 0.0f, 0.2f ) );

				shadowDebugPass.CullingMode = CullingMode.None;

				if ( targetRenderSystem.Capabilities.HasCapability( Capabilities.VertexPrograms ) )
				{
					ShadowVolumeExtrudeProgram.Initialize();

					// Enable the (infinite) point light extruder for now, just to get some params
					shadowDebugPass.SetVertexProgram(
						ShadowVolumeExtrudeProgram.GetProgramName( ShadowVolumeExtrudeProgram.Programs.PointLight ) );

					infiniteExtrusionParams = shadowDebugPass.VertexProgramParameters;
					infiniteExtrusionParams.SetAutoConstant( 0, GpuProgramParameters.AutoConstantType.WorldViewProjMatrix );
					infiniteExtrusionParams.SetAutoConstant( 4, GpuProgramParameters.AutoConstantType.LightPositionObjectSpace );
					// Note ignored extra parameter - for compatibility with finite extrusion vertex program
					infiniteExtrusionParams.SetAutoConstant( 5, GpuProgramParameters.AutoConstantType.ShadowExtrusionDistance );
				}

				matDebug.Compile();
			}
			else
			{
				shadowDebugPass = matDebug.GetTechnique( 0 ).GetPass( 0 );
				if ( targetRenderSystem.Capabilities.HasCapability( Capabilities.VertexPrograms ) )
				{
					infiniteExtrusionParams = shadowDebugPass.VertexProgramParameters;
				}
			}
		}

		private void InitShadowStencilPass()
		{
			var matStencil = (Material)MaterialManager.Instance[ STENCIL_SHADOW_VOLUMES_MATERIAL ];

			if ( matStencil == null )
			{
				// Create
				matStencil =
					(Material)
					MaterialManager.Instance.Create( STENCIL_SHADOW_VOLUMES_MATERIAL, ResourceGroupManager.InternalResourceGroupName );
				shadowStencilPass = matStencil.GetTechnique( 0 ).GetPass( 0 );

				if ( targetRenderSystem.Capabilities.HasCapability( Capabilities.VertexPrograms ) )
				{
					// Enable the finite point light extruder for now, just to get some params
					shadowStencilPass.SetVertexProgram(
						ShadowVolumeExtrudeProgram.GetProgramName( ShadowVolumeExtrudeProgram.Programs.PointLightFinite ) );

					finiteExtrusionParams = shadowStencilPass.VertexProgramParameters;
					finiteExtrusionParams.SetAutoConstant( 0, GpuProgramParameters.AutoConstantType.WorldViewProjMatrix );
					finiteExtrusionParams.SetAutoConstant( 4, GpuProgramParameters.AutoConstantType.LightPositionObjectSpace );
					finiteExtrusionParams.SetAutoConstant( 5, GpuProgramParameters.AutoConstantType.ShadowExtrusionDistance );
				}
				matStencil.Compile();
				// Nothing else, we don't use this like a 'real' pass anyway,
				// it's more of a placeholder
			}
			else
			{
				shadowStencilPass = matStencil.GetTechnique( 0 ).GetPass( 0 );
				if ( targetRenderSystem.Capabilities.HasCapability( Capabilities.VertexPrograms ) )
				{
					finiteExtrusionParams = shadowStencilPass.VertexProgramParameters;
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
			if ( IsShadowTechniqueTextureBased )
			{
				Pass retPass;
				if ( pass.Parent.ShadowCasterMaterial != null )
				{
					retPass = pass.Parent.ShadowCasterMaterial.GetBestTechnique().GetPass( 0 );
				}
				else
				{
					retPass = ( shadowTextureCustomCasterPass != null ? shadowTextureCustomCasterPass : shadowCasterPlainBlackPass );
				}

				// Special case alpha-blended passes
				if ( ( pass.SourceBlendFactor == SceneBlendFactor.SourceAlpha &&
				       pass.DestinationBlendFactor == SceneBlendFactor.OneMinusSourceAlpha ) ||
				     pass.AlphaRejectFunction != CompareFunction.AlwaysPass )
				{
					// Alpha blended passes must retain their transparency
					retPass.SetAlphaRejectSettings( pass.AlphaRejectFunction, pass.AlphaRejectValue );
					retPass.SetSceneBlending( pass.SourceBlendFactor, pass.DestinationBlendFactor );
					retPass.Parent.Parent.TransparencyCastsShadows = true;

					// So we allow the texture units, but override the color functions
					// Copy texture state, shift up one since 0 is shadow texture
					var origPassTUCount = pass.TextureUnitStatesCount;
					for ( var t = 0; t < origPassTUCount; ++t )
					{
						TextureUnitState tex;
						if ( retPass.TextureUnitStatesCount <= t )
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
						tex.SetColorOperationEx( LayerBlendOperationEx.Source1, LayerBlendSource.Manual, LayerBlendSource.Current,
						                         IsShadowTechniqueAdditive ? ColorEx.Black : shadowColor );
					}
					// Remove any extras
					while ( retPass.TextureUnitStatesCount > origPassTUCount )
					{
						retPass.RemoveTextureUnitState( origPassTUCount );
					}
				}
				else
				{
					// reset
					retPass.SetSceneBlending( SceneBlendType.Replace );
					retPass.AlphaRejectFunction = CompareFunction.AlwaysPass;
					while ( retPass.TextureUnitStatesCount > 0 )
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
					var prg = retPass.VertexProgram;
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
					if ( retPass == shadowTextureCustomCasterPass )
					{
						if ( retPass.VertexProgramName != shadowTextureCustomCasterVertexProgram )
						{
							shadowTextureCustomCasterPass.SetVertexProgram( shadowTextureCustomCasterVertexProgram );
							if ( retPass.HasVertexProgram )
							{
								retPass.VertexProgramParameters = shadowTextureCustomCasterVPParams;
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
			if ( IsShadowTechniqueTextureBased )
			{
				Pass retPass;
				if ( pass.Parent.ShadowReceiverMaterial != null )
				{
					retPass = pass.Parent.ShadowReceiverMaterial.GetBestTechnique().GetPass( 0 );
				}
				else
				{
					retPass = ( shadowTextureCustomReceiverPass != null ? shadowTextureCustomReceiverPass : shadowReceiverPass );
				}

				// Does incoming pass have a custom shadow receiver program?
				if ( pass.ShadowReceiverVertexProgramName != "" )
				{
					retPass.SetVertexProgram( pass.ShadowReceiverVertexProgramName );
					var prg = retPass.VertexProgram;
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
					if ( retPass == shadowTextureCustomReceiverPass )
					{
						if ( shadowTextureCustomReceiverPass.VertexProgramName != shadowTextureCustomReceiverVertexProgram )
						{
							shadowTextureCustomReceiverPass.SetVertexProgram( shadowTextureCustomReceiverVertexProgram );
							if ( retPass.HasVertexProgram )
							{
								retPass.VertexProgramParameters = shadowTextureCustomReceiverVPParams;
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
				if ( IsShadowTechniqueAdditive )
				{
					keepTUCount = 1;
					retPass.LightingEnabled = true;
					retPass.Ambient = pass.Ambient;
					retPass.SelfIllumination = pass.SelfIllumination;
					retPass.Diffuse = pass.Diffuse;
					retPass.Specular = pass.Specular;
					retPass.Shininess = pass.Shininess;
					retPass.SetRunOncePerLight( pass.IteratePerLight, pass.RunOnlyOncePerLightType, pass.OnlyLightType );
					// We need to keep alpha rejection settings
					retPass.SetAlphaRejectSettings( pass.AlphaRejectFunction, pass.AlphaRejectValue );
					// Copy texture state, shift up one since 0 is shadow texture
					var origPassTUCount = pass.TextureUnitStatesCount;
					for ( var t = 0; t < origPassTUCount; ++t )
					{
						var targetIndex = t + 1;
						var tex = ( retPass.TextureUnitStatesCount <= targetIndex
						            	? retPass.CreateTextureUnitState()
						            	: retPass.GetTextureUnitState( targetIndex ) );
						pass.GetTextureUnitState( t ).CopyTo( tex );
						// If programmable, have to adjust the texcoord sets too
						// D3D insists that texcoordsets match tex unit in programmable mode
						if ( retPass.HasVertexProgram )
						{
							tex.TextureCoordSet = targetIndex;
						}
					}
					keepTUCount = origPassTUCount + 1;
				}
				else
				{
					// need to keep spotlight fade etc
					keepTUCount = retPass.TextureUnitStatesCount;
				}

				// Will also need fragment programs since this is a complex light setup
				if ( pass.ShadowReceiverFragmentProgramName != "" )
				{
					// Have to merge the shadow receiver vertex program in
					retPass.SetFragmentProgram( pass.ShadowReceiverFragmentProgramName );
					var prg = retPass.FragmentProgram;
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
					if ( retPass == shadowTextureCustomReceiverPass )
					{
						// reset fp?
						if ( retPass.FragmentProgramName != shadowTextureCustomReceiverFragmentProgram )
						{
							retPass.SetFragmentProgram( shadowTextureCustomReceiverFragmentProgram );
							if ( retPass.HasFragmentProgram )
							{
								retPass.FragmentProgramParameters = shadowTextureCustomReceiverFPParams;
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
				while ( retPass.TextureUnitStatesCount > keepTUCount )
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

		private readonly LightList tmpLightList = new LightList();

		/// <summary>
		///		Internal method for rendering all the objects for a given light into the stencil buffer.
		/// </summary>
		/// <param name="light">The light source.</param>
		/// <param name="camera">The camera being viewed from.</param>
		protected virtual void RenderShadowVolumesToStencil( Light light, Camera camera )
		{
			// get the shadow caster list
			var casters = FindShadowCastersForLight( light, camera );
			if ( casters.Count == 0 )
			{
				// No casters, just do nothing
				return;
			}

			// Set up scissor test (point & spot lights only)
			var scissored = false;
			if ( light.Type != LightType.Directional && targetRenderSystem.Capabilities.HasCapability( Capabilities.ScissorTest ) )
			{
				// Project the sphere onto the camera
				float left, right, top, bottom;
				var sphere = new Sphere( light.GetDerivedPosition(), light.AttenuationRange );
				if ( camera.ProjectSphere( sphere, out left, out top, out right, out bottom ) )
				{
					scissored = true;
					// Turn normalised device coordinates into pixels
					int iLeft, iTop, iWidth, iHeight;
					currentViewport.GetActualDimensions( out iLeft, out iTop, out iWidth, out iHeight );
					int szLeft, szRight, szTop, szBottom;

					szLeft = (int)( iLeft + ( ( left + 1 )*0.5f*iWidth ) );
					szRight = (int)( iLeft + ( ( right + 1 )*0.5f*iWidth ) );
					szTop = (int)( iTop + ( ( -top + 1 )*0.5f*iHeight ) );
					szBottom = (int)( iTop + ( ( -bottom + 1 )*0.5f*iHeight ) );

					targetRenderSystem.SetScissorTest( true, szLeft, szTop, szRight, szBottom );
				}
			}

			targetRenderSystem.UnbindGpuProgram( GpuProgramType.Fragment );

			// Can we do a 2-sided stencil?
			var stencil2sided = false;

			if ( targetRenderSystem.Capabilities.HasCapability( Capabilities.TwoSidedStencil ) &&
			     targetRenderSystem.Capabilities.HasCapability( Capabilities.StencilWrap ) )
			{
				// enable
				stencil2sided = true;
			}

			// Do we have access to vertex programs?
			var extrudeInSoftware = true;

			var finiteExtrude = !shadowUseInfiniteFarPlane ||
			                    !targetRenderSystem.Capabilities.HasCapability( Capabilities.InfiniteFarPlane );

			if ( targetRenderSystem.Capabilities.HasCapability( Capabilities.VertexPrograms ) )
			{
				extrudeInSoftware = false;
				EnableHardwareShadowExtrusion( light, finiteExtrude );
			}
			else
			{
				targetRenderSystem.UnbindGpuProgram( GpuProgramType.Vertex );
			}

			// Add light to internal list for use in render call
			tmpLightList.Clear();
			tmpLightList.Add( light );

			// Turn off color writing and depth writing
			targetRenderSystem.SetColorBufferWriteEnabled( false, false, false, false );
			targetRenderSystem.DepthBufferWriteEnabled = false;
			targetRenderSystem.StencilCheckEnabled = true;
			targetRenderSystem.DepthBufferFunction = CompareFunction.Less;

			// Calculate extrusion distance
			float extrudeDistance = 0;
			if ( light.Type == LightType.Directional )
			{
				extrudeDistance = shadowDirLightExtrudeDist;
			}

			// get the near clip volume
			var nearClipVol = light.GetNearClipVolume( camera );

			// Determine whether zfail is required
			// We need to use zfail for ALL objects if we find a single object which
			// requires it
			var zfailAlgo = false;

			CheckShadowCasters( casters, nearClipVol, light, extrudeInSoftware, finiteExtrude, zfailAlgo, camera, extrudeDistance,
			                    stencil2sided, tmpLightList );
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
			shadowStencilPass.SetVertexProgram( ShadowVolumeExtrudeProgram.GetProgramName( light.Type, finiteExtrude, false ) );

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
				shadowDebugPass.SetVertexProgram( ShadowVolumeExtrudeProgram.GetProgramName( light.Type, finiteExtrude, true ) );

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

		private void CheckShadowCasters( IList casters, PlaneBoundedVolume nearClipVol, Light light, bool extrudeInSoftware,
		                                 bool finiteExtrude, bool zfailAlgo, Camera camera, float extrudeDistance,
		                                 bool stencil2sided, LightList tmpLightList )
		{
			int flags;
			for ( var i = 0; i < casters.Count; i++ )
			{
				var caster = (ShadowCaster)casters[ i ];

				if ( nearClipVol.Intersects( caster.GetWorldBoundingBox() ) )
				{
					// We have a zfail case, we must use zfail for all objects
					zfailAlgo = true;

					break;
				}
			}

			for ( var ci = 0; ci < casters.Count; ci++ )
			{
				var caster = (ShadowCaster)casters[ ci ];
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
				if ( !( ( flags & (int)ShadowRenderableFlags.ExtrudeToInfinity ) != 0 && light.Type == LightType.Directional ) &&
				     camera.IsObjectVisible( caster.GetDarkCapBounds( light, extrudeDistance ) ) )
				{
					flags |= (int)ShadowRenderableFlags.IncludeDarkCap;
				}

				// get shadow renderables
				var renderables = caster.GetShadowVolumeRenderableEnumerator( shadowTechnique, light, shadowIndexBuffer,
				                                                              extrudeInSoftware, extrudeDistance, flags );

				// If using one-sided stencil, render the first pass of all shadow
				// renderables before all the second passes
				for ( var i = 0; i < ( stencil2sided ? 1 : 2 ); i++ )
				{
					if ( i == 1 )
					{
						renderables = caster.GetLastShadowVolumeRenderableEnumerator();
					}

					while ( renderables.MoveNext() )
					{
						var sr = (ShadowRenderable)renderables.Current;

						// omit hidden renderables
						if ( sr.IsVisible )
						{
							// render volume, including dark and (maybe) light caps
							RenderSingleShadowVolumeToStencil( sr, zfailAlgo, stencil2sided, tmpLightList, ( i > 0 ) );

							// optionally render separate light cap
							if ( sr.IsLightCapSeperate && ( ( flags & (int)ShadowRenderableFlags.IncludeLightCap ) ) > 0 )
							{
								// must always fail depth check
								targetRenderSystem.DepthBufferFunction = CompareFunction.AlwaysFail;

								Debug.Assert( sr.LightCapRenderable != null, "Shadow renderable is missing a separate light cap renderable!" );

								RenderSingleShadowVolumeToStencil( sr.LightCapRenderable, zfailAlgo, stencil2sided, tmpLightList, ( i > 0 ) );
								// reset depth function
								targetRenderSystem.DepthBufferFunction = CompareFunction.Less;
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
				targetRenderSystem.SetStencilBufferParams( CompareFunction.AlwaysPass, // always pass stencil check
				                                           0, // no ref value (no compare)
				                                           unchecked( (int)0xffffffff ), // no mask
				                                           StencilOperation.Keep, // stencil test will never fail
				                                           zfail
				                                           	? ( twoSided
				                                           	    	? StencilOperation.IncrementWrap
				                                           	    	: StencilOperation.Increment )
				                                           	: StencilOperation.Keep, // back face depth fail
				                                           zfail
				                                           	? StencilOperation.Keep
				                                           	: ( twoSided
				                                           	    	? StencilOperation.DecrementWrap
				                                           	    	: StencilOperation.Decrement ), // back face pass
				                                           twoSided );
			}
			else
			{
				targetRenderSystem.CullingMode = twoSided ? CullingMode.None : CullingMode.Clockwise;
				targetRenderSystem.SetStencilBufferParams( CompareFunction.AlwaysPass, // always pass stencil check
				                                           0, // no ref value (no compare)
				                                           unchecked( (int)0xffffffff ), // no mask
				                                           StencilOperation.Keep, // stencil test will never fail
				                                           zfail
				                                           	? ( twoSided
				                                           	    	? StencilOperation.DecrementWrap
				                                           	    	: StencilOperation.Decrement )
				                                           	: StencilOperation.Keep, // front face depth fail
				                                           zfail
				                                           	? StencilOperation.Keep
				                                           	: ( twoSided
				                                           	    	? StencilOperation.IncrementWrap
				                                           	    	: StencilOperation.Increment ), // front face pass
				                                           twoSided );
			}
		}

		/// <summary>
		///		Render a single shadow volume to the stencil buffer.
		/// </summary>
		protected void RenderSingleShadowVolumeToStencil( ShadowRenderable sr, bool zfail, bool stencil2sided,
		                                                  LightList manualLightList, bool isSecondPass )
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
			//If using late material resolving, swap now.
			if ( IsLateMaterialResolving )
			{
				Technique lateTech = pass.Parent.Parent.GetBestTechnique();
				if ( lateTech.PassCount > pass.Index )
				{
					pass = lateTech.GetPass( pass.Index );
				}
				//Should we warn or throw an exception if an illegal state was achieved?
			}

			if ( !suppressRenderStateChanges || evenIfSuppressed )
			{
				if ( illuminationStage == IlluminationRenderStage.RenderToTexture && shadowDerivation )
				{
					// Derive a special shadow caster pass from this one
					pass = DeriveShadowCasterPass( pass );
				}
				else if ( illuminationStage == IlluminationRenderStage.RenderReceiverPass )
				{
					pass = DeriveShadowReceiverPass( pass );
				}

				// Tell params about current pass
				autoParamDataSource.CurrentPass = pass;

				bool passSurfaceAndLightParams = true;
				bool passFogParams = true;

				if ( pass.HasVertexProgram )
				{
					targetRenderSystem.BindGpuProgram( pass.VertexProgram.BindingDelegate );
					// bind parameters later 
					// does the vertex program want surface and light params passed to rendersystem?
					passSurfaceAndLightParams = pass.VertexProgram.PassSurfaceAndLightStates;
				}
				else
				{
					// Unbind program?
					if ( targetRenderSystem.IsGpuProgramBound( GpuProgramType.Vertex ) )
					{
						targetRenderSystem.UnbindGpuProgram( GpuProgramType.Vertex );
					}
					// Set fixed-function vertex parameters
				}

				if ( pass.HasGeometryProgram )
				{
					targetRenderSystem.BindGpuProgram( pass.GeometryProgram.BindingDelegate );
					// bind parameters later 
				}
				else
				{
					// Unbind program?
					if ( targetRenderSystem.IsGpuProgramBound( GpuProgramType.Geometry ) )
					{
						targetRenderSystem.UnbindGpuProgram( GpuProgramType.Geometry );
					}
					// Set fixed-function vertex parameters
				}

				if ( passSurfaceAndLightParams )
				{
					// Set surface reflectance properties, only valid if lighting is enabled
					if ( pass.LightingEnabled )
					{
						targetRenderSystem.SetSurfaceParams( pass.Ambient, pass.Diffuse, pass.Specular, pass.SelfIllumination,
						                                     pass.Shininess, pass.VertexColorTracking );
					}
						// #if NOT_IN_OGRE
					else
					{
						// even with lighting off, we need ambient set to white
						targetRenderSystem.SetSurfaceParams( ColorEx.White, ColorEx.Black, ColorEx.Black, ColorEx.Black, 0,
						                                     TrackVertexColor.None );
					}
					// #endif

					// Dynamic lighting enabled?
					targetRenderSystem.LightingEnabled = pass.LightingEnabled;
				}

				// Using a fragment program?
				if ( pass.HasFragmentProgram )
				{
					targetRenderSystem.BindGpuProgram( pass.FragmentProgram.BindingDelegate );
					// bind parameters later 
					passFogParams = pass.FragmentProgram.PassFogStates;
				}
				else
				{
					// Unbind program?
					if ( targetRenderSystem.IsGpuProgramBound( GpuProgramType.Fragment ) )
					{
						targetRenderSystem.UnbindGpuProgram( GpuProgramType.Fragment );
					}

					// Set fixed-function fragment settings
				}

				if ( passFogParams )
				{
					// New fog params can either be from scene or from material
					FogMode newFogMode;
					ColorEx newFogColour;
					Real newFogStart, newFogEnd, newFogDensity;
					if ( pass.FogOverride )
					{
						// New fog params from material
						newFogMode = pass.FogMode;
						newFogColour = pass.FogColor;
						newFogStart = pass.FogStart;
						newFogEnd = pass.FogEnd;
						newFogDensity = pass.FogDensity;
					}
					else
					{
						// New fog params from scene
						newFogMode = fogMode;
						newFogColour = fogColor;
						newFogStart = fogStart;
						newFogEnd = fogEnd;
						newFogDensity = fogDensity;
					}

					/* In D3D, it applies to shaders prior
                    to version vs_3_0 and ps_3_0. And in OGL, it applies to "ARB_fog_XXX" in
                    fragment program, and in other ways, them maybe access by gpu program via
                    "state.fog.XXX".
                    */
					targetRenderSystem.SetFog( newFogMode, newFogColour, newFogDensity, newFogStart, newFogEnd );
				}

				// Tell params about ORIGINAL fog
				// Need to be able to override fixed function fog, but still have
				// original fog parameters available to a shader that chooses to use
				autoParamDataSource.SetFog( fogMode, fogColor, fogDensity, fogStart, fogEnd );

				// The rest of the settings are the same no matter whether we use programs or not

				// Set scene blending
				if ( pass.HasSeparateSceneBlending )
				{
					targetRenderSystem.SetSeparateSceneBlending( pass.SourceBlendFactor, pass.DestinationBlendFactor,
					                                             pass.SourceBlendFactorAlpha, pass.DestinationBlendFactorAlpha,
					                                             pass.SceneBlendingOperation,
					                                             pass.HasSeparateSceneBlendingOperations
					                                             	? pass.SceneBlendingOperation
					                                             	: pass.SceneBlendingOperationAlpha );
				}
				else
				{
					if ( pass.HasSeparateSceneBlendingOperations )
					{
						targetRenderSystem.SetSeparateSceneBlending( pass.SourceBlendFactor, pass.DestinationBlendFactor,
						                                             pass.SourceBlendFactor, pass.DestinationBlendFactor,
						                                             pass.SceneBlendingOperation, pass.SceneBlendingOperationAlpha );
					}
					else
					{
						targetRenderSystem.SetSceneBlending( pass.SourceBlendFactor, pass.DestinationBlendFactor,
						                                     pass.SceneBlendingOperation );
					}
				}

				//TODO Set point parameters
				//this.targetRenderSystem.SetPointParameters(
				//    pass.PointSize,
				//    pass.IsPointAttenuationEnabled,
				//    pass.PointAttenuationConstant,
				//    pass.PointAttenuationLinear,
				//    pass.PointAttenuationQuadratic,
				//    pass.PointMinSize,
				//    pass.PointMaxSize
				//    );

				if ( targetRenderSystem.Capabilities.HasCapability( Capabilities.PointSprites ) )
				{
					targetRenderSystem.PointSpritesEnabled = pass.PointSpritesEnabled;
				}

				//targetRenderSystem.PointSpritesEnabled = pass.PointSpritesEnabled;

				// TODO : Reset the shadow texture index for each pass
				//foreach ( TextureUnitState textureUnit in pass.TextureUnitStates )
				//{
				//}

				// set all required texture units for this pass, and disable ones not being used
				var numTextureUnits = targetRenderSystem.Capabilities.TextureUnitCount;
				if ( pass.HasFragmentProgram && pass.FragmentProgram.IsSupported )
				{
					// Axiom: This effectivley breaks GLSL.
					// besides this routine aint existing (anymore?) in 1.7.2
					// an upgrade of the scenemanager is recommended
					//numTextureUnits = pass.FragmentProgram.SamplerCount;
				}
				else if ( Config.MaxTextureLayers < targetRenderSystem.Capabilities.TextureUnitCount )
				{
					numTextureUnits = Config.MaxTextureLayers;
				}

				for ( var i = 0; i < numTextureUnits; i++ )
				{
					if ( i < pass.TextureUnitStatesCount )
					{
						var texUnit = pass.GetTextureUnitState( i );
						targetRenderSystem.SetTextureUnitSettings( i, texUnit );
						//this.targetRenderSystem.SetTextureUnit( i, texUnit, !pass.HasFragmentProgram );
					}
					else
					{
						// disable this unit
						if ( !pass.HasFragmentProgram )
						{
							targetRenderSystem.DisableTextureUnit( i );
						}
					}
				}

				// Disable remaining texture units
				targetRenderSystem.DisableTextureUnitsFrom( pass.TextureUnitStatesCount );

				// Depth Settings
				targetRenderSystem.DepthBufferWriteEnabled = pass.DepthWrite;
				targetRenderSystem.DepthBufferCheckEnabled = pass.DepthCheck;
				targetRenderSystem.DepthBufferFunction = pass.DepthFunction;
				targetRenderSystem.SetDepthBias( pass.DepthBiasConstant );

				// Aplha Reject Settings
				targetRenderSystem.SetAlphaRejectSettings( pass.AlphaRejectFunction, (byte)pass.AlphaRejectValue,
				                                           pass.IsAlphaToCoverageEnabled );

				// Color Write
				// right now only using on/off, not per channel
				var colWrite = pass.ColorWriteEnabled;
				targetRenderSystem.SetColorBufferWriteEnabled( colWrite, colWrite, colWrite, colWrite );

				// Culling Mode
				targetRenderSystem.CullingMode = pass.CullingMode;

				// Shading mode
				//this.targetRenderSystem.ShadingMode = pass.ShadingMode;

				// Polygon Mode
				targetRenderSystem.PolygonMode = pass.PolygonMode;

				// set pass number
				autoParamDataSource.PassNumber = pass.Index;
			}

			return pass;
		}

		/// <summary>
		///		If only the first parameter is supplied
		/// </summary>
		public virtual Pass SetPass( Pass pass )
		{
			return SetPass( pass, false, true );
		}

		/// <summary>
		///		If only the first two parameters are supplied
		/// </summary>
		public virtual Pass SetPass( Pass pass, bool evenIfSuppressed )
		{
			return SetPass( pass, evenIfSuppressed, true );
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
			var p = new Plane();
			var meshName = "SkyboxPlane_";
			var up = Vector3.Zero;

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
			p.Normal = orientation*p.Normal;
			up = orientation*up;

			var modelMgr = MeshManager.Instance;

			// see if this mesh exists
			var planeModel = (Mesh)modelMgr[ meshName ];

			// trash it if it already exists
			if ( planeModel != null )
			{
				modelMgr.Unload( planeModel );
			}

			var planeSize = distance*2;

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
		protected Mesh CreateSkyDomePlane( BoxPlane plane, float curvature, float tiling, float distance,
		                                   Quaternion orientation, string groupName )
		{
			var p = new Plane();
			var up = Vector3.Zero;
			var meshName = "SkyDomePlane_";

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
			p.Normal = orientation*p.Normal;
			up = orientation*up;

			// check to see if mesh exists
			var meshManager = MeshManager.Instance;
			var planeMesh = (Mesh)meshManager[ meshName ];

			// destroy existing
			if ( planeMesh != null )
			{
				meshManager.Unload( planeMesh );
				planeMesh.Dispose();
			}

			// create new
			var planeSize = distance*2;
			var segments = 16;
			planeMesh = meshManager.CreateCurvedIllusionPlane( meshName, groupName, p, planeSize, planeSize, curvature, segments,
			                                                   segments, false, 1, tiling, tiling, up, orientation,
			                                                   BufferUsage.DynamicWriteOnly, BufferUsage.StaticWriteOnly, true,
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
			var useIdentityView = renderable.UseIdentityView;
			if ( useIdentityView )
			{
				// Using identity view now, change it
				targetRenderSystem.ViewMatrix = Matrix4.Identity;
				lastViewWasIdentity = true;
			}

			// Projection
			var useIdentityProj = renderable.UseIdentityProjection;
			if ( useIdentityProj )
			{
				// Use identity projection matrix, still need to take RS depth into account
				Matrix4 mat;
				targetRenderSystem.ConvertProjectionMatrix( Matrix4.Identity, out mat );
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
			var e = new BeginRenderQueueEventArgs();
			e.RenderQueueId = group;
			e.Invocation = invocation;

			var skip = false;
			_queueStartedEvent.Fire( this, e, ( args ) =>
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
			var e = new EndRenderQueueEventArgs();
			e.RenderQueueId = group;
			e.Invocation = invocation;

			var repeat = false;
			_queueEndedEvent.Fire( this, e, ( args ) =>
			                                {
			                                	repeat |= args.RepeatInvocation;
			                                	return true;
			                                } );
			return repeat;
		}

		#endregion Protected methods

		#region Public methods

		#region RibbonTrail Management

		/// <summary>
		/// Create a RibbonTrail, an object which you can use to render
		/// a linked chain of billboards which follows one or more nodes.
		/// </summary>
		/// <param name="name">The name to be given to the object (must be unique).</param>
		[OgreVersion( 1, 7, 2 )]
		public virtual RibbonTrail CreateRibbonTrail( string name )
		{
			return (RibbonTrail)CreateMovableObject( name, RibbonTrailFactory.TypeName, null );
		}

		/// <summary>
		/// Create a RibbonTrail, an object which you can use to render
		/// a linked chain of billboards which follows one or more nodes, generating the name.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public virtual RibbonTrail CreateRibbonTrail()
		{
			string name = movableNameGenerator.GetNextUniqueName();
			return CreateRibbonTrail( name );
		}

		public virtual RibbonTrail GetRibbonTrail( string name )
		{
			return (RibbonTrail)GetMovableObject( name, RibbonTrailFactory.TypeName );
		}

		public virtual void RemoveAllRibonTrails()
		{
			DestroyAllMovableObjectsByType( RibbonTrailFactory.TypeName );
		}

		public virtual void RemoveRibbonTrail( RibbonTrail ribbonTrail )
		{
			DestroyMovableObject( ribbonTrail );
		}

		public virtual void RemoveRibbonTrail( string name )
		{
			DestroyMovableObject( name, RibbonTrailFactory.TypeName );
		}

		#endregion RibbonTrail Management

		public void RekeySceneNode( string oldName, SceneNode node )
		{
			if ( sceneNodeList[ oldName ] == node )
			{
				sceneNodeList.Remove( oldName );
				sceneNodeList.Add( node );
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
		public virtual AxisAlignedBoxRegionSceneQuery CreateAABBRegionQuery( AxisAlignedBox box, uint mask )
		{
			var query = new DefaultAxisAlignedBoxRegionSceneQuery( this );
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
			return CreateRayQuery( new Ray(), 0xffffffff );
		}

		/// <summary>
		///    Creates a query to return objects found along the ray.
		/// </summary>
		/// <param name="ray">Ray to use for the intersection query.</param>
		/// <returns>A specialized implementation of RaySceneQuery for this scene manager.</returns>
		public virtual RaySceneQuery CreateRayQuery( Ray ray )
		{
			return CreateRayQuery( ray, 0xffffffff );
		}

		/// <summary>
		///    Creates a query to return objects found along the ray.
		/// </summary>
		/// <param name="ray">Ray to use for the intersection query.</param>
		/// <param name="mask"></param>
		/// <returns>A specialized implementation of RaySceneQuery for this scene manager.</returns>
		public virtual RaySceneQuery CreateRayQuery( Ray ray, uint mask )
		{
			var query = new DefaultRaySceneQuery( this );
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
		public virtual SphereRegionSceneQuery CreateSphereRegionQuery( Sphere sphere, uint mask )
		{
			var query = new DefaultSphereRegionSceneQuery( this );
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
		public virtual PlaneBoundedVolumeListSceneQuery CreatePlaneBoundedVolumeQuery( PlaneBoundedVolumeList volumes,
		                                                                               uint mask )
		{
			var query = new DefaultPlaneBoundedVolumeListSceneQuery( this );
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
		public virtual IntersectionSceneQuery CreateIntersectionQuery( uint mask )
		{
			var query = new DefaultIntersectionSceneQuery( this );
			query.QueryMask = mask;
			return query;
		}

		/// <summary>
		///		Removes all cameras from the scene.
		/// </summary>
		public virtual void RemoveAllCameras()
		{
			if ( cameraList != null )
			{
				// notify the render system of each camera being removed
				foreach ( var cam in cameraList.Values )
				{
					targetRenderSystem.NotifyCameraRemoved( cam );

					if ( !cam.IsDisposed )
					{
						cam.Dispose();
					}
				}

				// clear the list
				cameraList.Clear();
			}
		}

		/// <summary>
		///		Removes all lights from the scene.
		/// </summary>
		public virtual void RemoveAllLights()
		{
			DestroyAllMovableObjectsByType( LightFactory.TypeName );
		}

		/// <summary>
		///		Removes all entities from the scene.
		/// </summary>
		public virtual void RemoveAllEntities()
		{
			DestroyAllMovableObjectsByType( EntityFactory.TypeName );
		}

		/// <summary>
		///		Removes all billboardsets from the scene.
		/// </summary>
		public virtual void RemoveAllBillboardSets()
		{
			DestroyAllMovableObjectsByType( BillboardSetFactory.TypeName );
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

			RemoveCamera( cameraList[ name ] );
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
			DestroyMovableObject( light );
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
			DestroyMovableObject( name, LightFactory.TypeName );
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
			DestroyMovableObject( billboardSet );
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
			DestroyMovableObject( name, BillboardSetFactory.TypeName );
		}

		/// <summary>
		///    Removes the specified entity from the scene.
		/// </summary>
		/// <param name="entity">Entity to remove from the scene.</param>
		public virtual void RemoveEntity( Entity entity )
		{
			DestroyMovableObject( entity );
		}

		/// <summary>
		///    Removes the entity with the specified name from the scene.
		/// </summary>
		/// <param name="name">Entity to remove from the scene.</param>
		public virtual void RemoveEntity( string name )
		{
			DestroyMovableObject( name, EntityFactory.TypeName );
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
		public virtual void SetFog( FogMode mode, ColorEx color, float density )
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
			SetSkyBox( enable, materialName, distance, true, Quaternion.Identity, ResourceGroupManager.DefaultResourceGroupName );
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
		public void SetSkyBox( bool enable, string materialName, float distance, bool drawFirst, Quaternion orientation,
		                       string groupName )
		{
			// enable the skybox?
			isSkyBoxEnabled = enable;

			if ( enable )
			{
				var m = (Material)MaterialManager.Instance[ materialName ];

				if ( m == null )
				{
					isSkyBoxEnabled = false;
					throw new AxiomException( string.Format( "Could not find skybox material '{0}'", materialName ) );
				}
				// Make sure the material doesn't update the depth buffer
				m.DepthWrite = false;
				// Ensure loaded
				m.Load();

				// ensure texture clamping to reduce fuzzy edges when using filtering
				m.GetTechnique( 0 ).GetPass( 0 ).GetTextureUnitState( 0 ).SetTextureAddressingMode( TextureAddressing.Clamp );

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
				for ( var i = 0; i < 6; i++ )
				{
					var planeModel = CreateSkyboxPlane( (BoxPlane)i, distance, orientation, groupName );
					var entityName = "SkyBoxPlane" + i;

					if ( skyBoxEntities[ i ] != null )
					{
						RemoveEntity( skyBoxEntities[ i ] );
					}

					// create an entity for this plane
					skyBoxEntities[ i ] = CreateEntity( entityName, planeModel.Name );

					// skyboxes need not cast shadows
					skyBoxEntities[ i ].CastShadows = false;

					// Have to create 6 materials, one for each frame
					// Used to use combined material but now we're using queue we can't split to change frame
					// This doesn't use much memory because textures aren't duplicated
					var boxMaterial = (Material)MaterialManager.Instance[ entityName ];

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

					skyBoxEntities[ i ].MaterialName = boxMaterial.Name;

					// Attach to node
					skyBoxNode.AttachObject( skyBoxEntities[ i ] );
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
			SetSkyDome( isEnabled, materialName, curvature, tiling, 4000, true, Quaternion.Identity,
			            ResourceGroupManager.DefaultResourceGroupName );
		}

		/// <summary>
		/// </summary>
		public void SetSkyDome( bool isEnabled, string materialName, float curvature, float tiling, float distance,
		                        bool drawFirst, Quaternion orientation, string groupName )
		{
			isSkyDomeEnabled = isEnabled;
			if ( isEnabled )
			{
				var material = (Material)MaterialManager.Instance[ materialName ];

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
				for ( var i = 0; i < 5; ++i )
				{
					var planeMesh = CreateSkyDomePlane( (BoxPlane)i, curvature, tiling, distance, orientation, groupName );
					var entityName = String.Format( "SkyDomePlane{0}", i );

					// create entity
					if ( skyDomeEntities[ i ] != null )
					{
						RemoveEntity( skyDomeEntities[ i ] );
					}

					skyDomeEntities[ i ] = CreateEntity( entityName, planeMesh.Name );
					skyDomeEntities[ i ].MaterialName = material.Name;
					// Sky entities need not cast shadows
					skyDomeEntities[ i ].CastShadows = false;

					// attach to node
					skyDomeNode.AttachObject( skyDomeEntities[ i ] );
				} // for each plane
			}
		}

		public virtual void SetShadowTextureSettings( ushort size, ushort count )
		{
			SetShadowTextureSettings( size, count, shadowTextureFormat );
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
			if ( shadowTextures.Count > 0 &&
			     ( count != shadowTextureCount || size != shadowTextureSize || format != shadowTextureFormat ) )
			{
				// recreate
				CreateShadowTextures( size, count, format );
			}
			shadowTextureCount = count;
			shadowTextureSize = size;
			shadowTextureFormat = format;
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
				foreach ( var shadowTexture in shadowTextures )
				{
					// Camera names are local to SM
					var camName = shadowTexture.Name + "Cam";
					// Material names are global to SM, make specific
					var matName = shadowTexture.Name + "Mat" + Name;

					var shadowRTT = shadowTexture.GetBuffer().GetRenderTarget();

					// Create camera for this texture, but note that we have to rebind
					// in PrepareShadowTextures to coexist with multiple SMs
					var cam = CreateCamera( camName );
					cam.AspectRatio = shadowTexture.Width/(Real)shadowTexture.Height;
					shadowTextureCameras.Add( cam );

					// Create a viewport, if not there already
					if ( shadowRTT.NumViewports == 0 )
					{
						// Note camera assignment is transient when multiple SMs
						var v = shadowRTT.AddViewport( cam );
						v.SetClearEveryFrame( true );
						// remove overlays
						v.ShowOverlays = false;
					}

					// Don't update automatically - we'll do it when required
					shadowRTT.IsAutoUpdated = false;

					// Also create corresponding Material used for rendering this shadow
					var mat = (Material)MaterialManager.Instance[ matName ];
					if ( mat == null )
					{
						mat = (Material)MaterialManager.Instance.Create( matName, ResourceGroupManager.InternalResourceGroupName );
					}
					var p = mat.GetTechnique( 0 ).GetPass( 0 );
					if ( p.TextureUnitStatesCount != 1 /* ||
						 p.GetTextureUnitState( 0 ).GetTexture( 0 ) != shadowTexture */ )
					{
						mat.GetTechnique( 0 ).GetPass( 0 ).RemoveAllTextureUnitStates();
						// create texture unit referring to render target texture
						var texUnit = p.CreateTextureUnitState( shadowTexture.Name );
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
					nullShadowTexture = shadowTextureConfigList.Count == 0
					                    	? null
					                    	: ShadowTextureManager.Instance.GetNullShadowTexture( shadowTextureConfigList[ 0 ].format );
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
				{
					throw new ArgumentException( "Cannot set the scene ambient light color to null" );
				}
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
		public AxiomCollection<object> Options
		{
			get
			{
				return optionList;
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
				return shadowColor;
			}
			set
			{
				shadowColor = value;

				if ( shadowModulativePass == null && shadowCasterPlainBlackPass == null )
				{
					InitShadowVolumeMaterials();
				}

				shadowModulativePass.GetTextureUnitState( 0 ).SetColorOperationEx( LayerBlendOperationEx.Modulate,
				                                                                   LayerBlendSource.Manual, LayerBlendSource.Current,
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
		/// Gets the proportional distance which a texture shadow which is generated from a
		/// directional light will be offset into the camera view to make best use of texture space.
		/// </summary>
		public Real ShadowDirectionalLightTextureOffset
		{
			get
			{
				return shadowTextureOffset;
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
				return shadowFarDistance;
			}
			set
			{
				shadowFarDistance = value;
				shadowFarDistanceSquared = shadowFarDistance*shadowFarDistance;
			}
		}

		public Real ShadowFarDistanceSquared
		{
			get
			{
				return shadowFarDistance;
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
					shadowIndexBuffer = HardwareBufferManager.Instance.CreateIndexBuffer( IndexType.Size16, shadowIndexBufferSize,
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
				CreateShadowTextures( value, shadowTextureCount, shadowTextureFormat );
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
				CreateShadowTextures( shadowTextureSize, value, shadowTextureFormat );
				shadowTextureCount = value;
			}
		}

		public PixelFormat ShadowTextureFormat
		{
			get
			{
				return shadowTextureFormat;
			}
			set
			{
				// possibly recreate
				CreateShadowTextures( shadowTextureSize, shadowTextureCount, value );
				shadowTextureFormat = value;
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
				return shadowTextureSelfShadow;
			}
			set
			{
				shadowTextureSelfShadow = value;
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
				if ( IsShadowTechniqueStencilBased )
				{
					// Firstly check that we have a stencil. Otherwise, forget it!
					if ( !targetRenderSystem.Capabilities.HasCapability( Capabilities.StencilBuffer ) )
					{
						LogManager.Instance.Write(
							"WARNING: Stencil shadows were requested, but the current hardware does not support them.  Disabling." );

						shadowTechnique = ShadowTechnique.None;
					}
					else if ( shadowIndexBuffer == null )
					{
						// create an shadow index buffer
						shadowIndexBuffer = HardwareBufferManager.Instance.CreateIndexBuffer( IndexType.Size16, shadowIndexBufferSize,
						                                                                      BufferUsage.DynamicWriteOnly, false );

						// tell all the meshes to prepare shadow volumes
						MeshManager.Instance.PrepareAllMeshesForShadowVolumes = true;
					}
				}

				// If Additive stencil, we need to split everything by illumination stage
				GetRenderQueue().SplitPassesByLightingType = ( shadowTechnique == ShadowTechnique.StencilAdditive );

				// If any type of shadowing is used, tell render queue to split off non-shadowable materials
				GetRenderQueue().SplitNoShadowPasses = IsShadowTechniqueInUse;

				// create new textures for texture based shadows
				if ( IsShadowTechniqueTextureBased )
				{
					CreateShadowTextures( shadowTextureSize, shadowTextureCount, shadowTextureFormat );
				}
			}
		}

		public string ShadowTextureCasterMaterial
		{
			get
			{
				return shadowTextureCasterMaterial;
			}
			set
			{
				// When rendering with a material that includes its own custom shadow caster
				// vertex program, the code that sets up the pass will replace the vertex program
				// in this material with the one from the object's own material.
				// We need to set it back before we switch materials, in case we need to use this
				// material again.
				if ( shadowTextureCustomCasterPass != null )
				{
					if ( shadowTextureCustomCasterPass.VertexProgramName != shadowTextureCustomCasterVertexProgram )
					{
						shadowTextureCustomCasterPass.SetVertexProgram( shadowTextureCustomCasterVertexProgram );
						if ( shadowTextureCustomCasterPass.HasVertexProgram )
						{
							shadowTextureCustomCasterPass.VertexProgramParameters = shadowTextureCustomCasterVPParams;
						}
					}
				}

				shadowTextureCasterMaterial = value;
				if ( value == "" )
				{
					shadowTextureCustomCasterPass = null;
					shadowTextureCustomCasterFragmentProgram = "";
					shadowTextureCustomCasterVertexProgram = "";
				}
				else
				{
					var material = (Material)MaterialManager.Instance[ value ];
					if ( material == null )
					{
						LogManager.Instance.Write(
							"Cannot use material '{0}' as the ShadowTextureCasterMaterial because the material doesn't exist.", value );
					}
					else
					{
						material.Load();
						shadowTextureCustomCasterPass = material.GetBestTechnique().GetPass( 0 );
						if ( shadowTextureCustomCasterPass.HasVertexProgram )
						{
							// Save vertex program and params in case we have to swap them out
							shadowTextureCustomCasterVertexProgram = shadowTextureCustomCasterPass.VertexProgramName;
							shadowTextureCustomCasterVPParams = shadowTextureCustomCasterPass.VertexProgramParameters;
						}
						else
						{
							shadowTextureCustomCasterVertexProgram = "";
						}
						if ( shadowTextureCustomCasterPass.HasFragmentProgram )
						{
							// Save fragment program and params in case we have to swap them out
							shadowTextureCustomCasterFragmentProgram = shadowTextureCustomCasterPass.FragmentProgramName;
							shadowTextureCustomCasterFPParams = shadowTextureCustomCasterPass.FragmentProgramParameters;
						}
						else
						{
							shadowTextureCustomCasterFragmentProgram = "";
						}
					}
				}
			}
		}

		public string ShadowTextureReceiverMaterial
		{
			get
			{
				return shadowTextureReceiverMaterial;
			}
			set
			{
				// When rendering with a material that includes its own custom shadow receiver
				// vertex program, the code that sets up the pass will replace the vertex program
				// in this material with the one from the object's own material.
				// We need to set it back before we switch materials, in case we need to use this
				// material again.
				if ( shadowTextureCustomReceiverPass != null )
				{
					if ( shadowTextureCustomReceiverPass.VertexProgramName != shadowTextureCustomReceiverVertexProgram )
					{
						shadowTextureCustomReceiverPass.SetVertexProgram( shadowTextureCustomReceiverVertexProgram );
						if ( shadowTextureCustomReceiverPass.HasVertexProgram )
						{
							shadowTextureCustomReceiverPass.VertexProgramParameters = shadowTextureCustomReceiverVPParams;
						}
					}
				}

				shadowTextureReceiverMaterial = value;
				if ( value == "" )
				{
					shadowTextureCustomReceiverPass = null;
					shadowTextureCustomReceiverVertexProgram = "";
					shadowTextureCustomReceiverFragmentProgram = "";
				}
				else
				{
					var material = (Material)MaterialManager.Instance[ value ];
					if ( material == null )
					{
						LogManager.Instance.Write(
							"Cannot use material '{0}' as the ShadowTextureReceiverMaterial because the material doesn't exist.", value );
					}
					else
					{
						material.Load();
						shadowTextureCustomReceiverPass = material.GetBestTechnique().GetPass( 0 );
						if ( shadowTextureCustomReceiverPass.HasVertexProgram )
						{
							// Save vertex program and params in case we have to swap them out
							shadowTextureCustomReceiverVertexProgram = shadowTextureCustomReceiverPass.VertexProgramName;
							shadowTextureCustomReceiverVPParams = shadowTextureCustomReceiverPass.VertexProgramParameters;
						}
						else
						{
							shadowTextureCustomReceiverVertexProgram = "";
						}
						if ( shadowTextureCustomReceiverPass.HasFragmentProgram )
						{
							// Save fragment program and params in case we have to swap them out
							shadowTextureCustomReceiverFragmentProgram = shadowTextureCustomReceiverPass.FragmentProgramName;
							shadowTextureCustomReceiverFPParams = shadowTextureCustomReceiverPass.FragmentProgramParameters;
						}
						else
						{
							shadowTextureCustomReceiverFragmentProgram = "";
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
				return shadowUseInfiniteFarPlane;
			}
			set
			{
				shadowUseInfiniteFarPlane = value;
			}
		}

		public bool SuppressRenderStateChanges
		{
			get
			{
				return suppressRenderStateChanges;
			}
			set
			{
				suppressRenderStateChanges = value;
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
			set
			{
				fogMode = value;
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
			set
			{
				fogStart = value;
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
			set
			{
				fogEnd = value;
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
			set
			{
				fogDensity = value;
			}
		}

		/// <summary>
		///		Gets the fog color that was set during the last call to SetFog.
		/// </summary>
		public virtual ColorEx FogColor
		{
			get
			{
				return fogColor;
			}
			set
			{
				fogColor = value;
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
				return currentViewport;
			}
		}

		/// <summary>
		///		Gets and sets the object visibility mask
		/// </summary>
		public ulong VisibilityMask
		{
			get
			{
				return visibilityMask;
			}
			set
			{
				visibilityMask = value;
			}
		}

		/// <summary>
		///		Gets ths combined object visibility mask of this scenemanager and the current viewport
		/// </summary>
		public ulong CombinedVisibilityMask
		{
			get
			{
				return currentViewport != null ? currentViewport.VisibilityMask & visibilityMask : visibilityMask;
			}
		}

		/// <summary>
		///		Gets and sets the object visibility mask
		/// </summary>
		public bool FindVisibleObjectsBool
		{
			get
			{
				return findVisibleObjects;
			}
			set
			{
				findVisibleObjects = value;
			}
		}

		/// <summary>
		///		Gets the instance name of this SceneManager.
		/// </summary>
		public string Name
		{
			get
			{
				return name;
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
		public abstract string TypeName { get; }

		protected ulong _lightsDirtyCounter;

		// TODO: implement logic
		[OgreVersion( 1, 7, 2790, "Implement logic for this" )] private GpuProgramParameters.GpuParamVariability
			_gpuParamsDirty = GpuProgramParameters.GpuParamVariability.All;

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

			if ( IsShadowTechniqueInUse )
			{
				// initialize shadow volume materials
				InitShadowVolumeMaterials();
			}

			// Perform a quick pre-check to see whether we should override far distance
			// When using stencil volumes we have to use infinite far distance
			// to prevent dark caps getting clipped
			if ( IsShadowTechniqueStencilBased && camera.Far != 0 &&
			     targetRenderSystem.Capabilities.HasCapability( Capabilities.InfiniteFarPlane ) && shadowUseInfiniteFarPlane )
			{
				// infinite far distance
				camera.Far = 0.0f;
			}

			cameraInProgress = camera;
			hasCameraChanged = true;

			// Update the scene, only do this once per frame
			var thisFrameNumber = Root.Instance.CurrentFrameCount;
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
			foreach ( var sn in autoTrackingSceneNodes.Values )
			{
				sn.AutoTrack();
			}

			// ask the camera to auto track if it has a target
			camera.AutoTrack();

			// Are we using any shadows at all?
			if ( IsShadowTechniqueInUse && illuminationStage != IlluminationRenderStage.RenderToTexture && viewport.ShowShadows &&
			     findVisibleObjects )
			{
				// Locate any lights which could be affecting the frustum
				FindLightsAffectingFrustum( camera );

				if ( IsShadowTechniqueTextureBased )
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
					cameraInProgress = camera;
					hasCameraChanged = true;
				}
			}

			// Invert vertex winding?
			targetRenderSystem.InvertVertexWinding = camera.IsReflected;

			// Tell params about viewport
			autoParamDataSource.CurrentViewport = viewport;
			// Set the viewport
			SetViewport( viewport );

			// set the current camera for use in the auto GPU program params
			autoParamDataSource.SetCurrentCamera( camera, cameraRelativeRendering );

			// Set autoparams for finite dir light extrusion
			autoParamDataSource.ShadowExtrusionDistance = shadowDirLightExtrudeDist;

			// sets the current ambient light color for use in auto GPU program params
			autoParamDataSource.AmbientLight = ambientColor;
			// Tell rendersystem
			targetRenderSystem.AmbientLight = ambientColor;

			// Tell params about render target
			autoParamDataSource.CurrentRenderTarget = viewport.Target;

			// set fog params
			var fogScale = 1f;
			if ( fogMode == FogMode.None )
			{
				fogScale = 0f;
			}
			autoParamDataSource.SetFog( fogMode, fogColor, fogScale, fogStart, fogEnd );

			// set the time in the auto param data source
			//autoParamDataSource.Time = ((float)Root.Instance.Timer.Milliseconds) / 1000f;

			// Set camera window clipping planes (if any)
			if ( targetRenderSystem.Capabilities.HasCapability( Capabilities.UserClipPlanes ) )
			{
				// TODO: Add ClipPlanes to RenderSystem.cs
				if ( camera.IsWindowSet )
				{
					targetRenderSystem.ResetClipPlanes();
					var planeList = camera.WindowPlanes;
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
			PrepareRenderQueue();

			// Parse the scene and tag visibles
			if ( findVisibleObjects )
			{
				if ( PreFindVisibleObjects != null )
				{
					PreFindVisibleObjects( this, illuminationStage, viewport );
				}
				FindVisibleObjects( camera, illuminationStage == IlluminationRenderStage.RenderToTexture );
				if ( PostFindVisibleObjects != null )
				{
					PostFindVisibleObjects( this, illuminationStage, viewport );
				}
			}

			// Add overlays, if viewport deems it
			if ( viewport.ShowOverlays && illuminationStage != IlluminationRenderStage.RenderToTexture )
			{
				// Queue overlays for rendering
				OverlayManager.Instance.QueueOverlaysForRendering( camera, GetRenderQueue(), viewport );
			}

			// queue overlays and skyboxes for rendering
			if ( viewport.ShowSkies && findVisibleObjects && illuminationStage != IlluminationRenderStage.RenderToTexture )
			{
				QueueSkiesForRendering( camera );
			}

			// begin frame geometry count
			targetRenderSystem.BeginGeometryCount();

			// clear the device if need be
			if ( viewport.ClearEveryFrame )
			{
				targetRenderSystem.ClearFrameBuffer( viewport.ClearBuffers, viewport.BackgroundColor );
			}

			// being a frame of animation
			targetRenderSystem.BeginFrame();

			// use the camera's current scene detail level
			targetRenderSystem.PolygonMode = camera.PolygonMode;

			// Set initial camera state
			targetRenderSystem.ProjectionMatrix = camera.ProjectionMatrixRS;
			targetRenderSystem.ViewMatrix = camera.ViewMatrix;

			// render all visible objects
			RenderVisibleObjects();

			// end the current frame
			targetRenderSystem.EndFrame();

			// Notify camera of the number of rendered faces
			camera.NotifyRenderedFaces( targetRenderSystem.FaceCount );

			// Notify camera of the number of rendered batches
			camera.NotifyRenderedBatches( targetRenderSystem.BatchCount );
		}

		private void PrepareRenderQueue()
		{
			// Clear the render queue
			GetRenderQueue().Clear();

			// Global split options
			UpdateRenderQueueSplitOptions();
		}

		private void UpdateRenderQueueSplitOptions()
		{
			var q = GetRenderQueue();
			if ( IsShadowTechniqueStencilBased )
			{
				// Casters can always be receivers
				q.ShadowCastersCannotBeReceivers = false;
			}
			else // texture based
			{
				q.ShadowCastersCannotBeReceivers = !shadowTextureSelfShadow;
			}

			if ( IsShadowTechniqueAdditive && currentViewport.ShowShadows )
			{
				// Additive lighting, we need to split everything by illumination stage
				q.SplitPassesByLightingType = true;
			}
			else
			{
				q.SplitPassesByLightingType = false;
			}

			if ( IsShadowTechniqueInUse && currentViewport.ShowShadows )
			{
				// Tell render queue to split off non-shadowable materials
				q.SplitNoShadowPasses = true;
			}
			else
			{
				q.SplitNoShadowPasses = false;
			}
		}

		private void UpdateRenderQueueGroupSplitOptions( RenderQueueGroup group, bool suppressShadows,
		                                                 bool suppressRenderState )
		{
			if ( IsShadowTechniqueStencilBased )
			{
				// Casters can always be receivers
				group.ShadowCastersCannotBeReceivers = false;
			}
			else if ( IsShadowTechniqueTextureBased )
			{
				group.ShadowCastersCannotBeReceivers = !shadowTextureSelfShadow;
			}

			if ( !suppressShadows && currentViewport.ShowShadows && IsShadowTechniqueAdditive )
			{
				// Additive lighting, we need to split everything by illumination stage
				group.SplitPassesByLightingType = true;
			}
			else
			{
				group.SplitPassesByLightingType = false;
			}

			if ( !suppressShadows && currentViewport.ShowShadows && IsShadowTechniqueInUse )
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
			foreach ( var animState in animationStateList.Values )
			{
				// get this states animation
				var anim = animationList[ animState.Name ];

				// loop through all node tracks and reset their nodes initial state
				foreach ( var nodeTrack in anim.NodeTracks.Values )
				{
					var node = nodeTrack.TargetNode;
					node.ResetToInitialState();
				}

				// loop through all node tracks and reset their nodes initial state
				foreach ( var numericTrack in anim.NumericTracks.Values )
				{
					var animable = numericTrack.TargetAnimable;
					animable.ResetToBaseValue();
				}

				// apply the animation
				anim.Apply( animState.Time, animState.Weight, false, 1.0f );
			}
		}

		protected internal void DestroyShadowTextures()
		{
			for ( var i = 0; i < shadowTextures.Count; ++i )
			{
				var shadowTex = shadowTextures[ i ];
				// TODO: It would be useful to have a reference count for
				// these textures.  They should only be removed from the
				// resource manager if nobody else is using them.
				TextureManager.Instance.Remove( shadowTex.Name );
				// destroy texture
				// TODO: Should I really destroy this texture here?
				if ( !shadowTex.IsDisposed )
				{
					shadowTex.Dispose();
				}

				DestroyCamera( shadowTextureCameras[ i ] );
			}
			shadowTextures.Clear();
			shadowTextureCameras.Clear();
		}

		public void DestroyCamera( Camera camera )
		{
			cameraList.Remove( camera.Name );
			targetRenderSystem.NotifyCameraRemoved( camera );

			if ( !camera.IsDisposed )
			{
				camera.Dispose();
			}
		}

		/// <summary>
		///     Destroy all cameras managed by this SceneManager
		/// <remarks>
		///     Method added with MovableObject Factories.
		/// </remarks>
		/// </summary>
		public void DestroyAllCameras()
		{
			foreach ( var camera in cameraList.Values )
			{
				targetRenderSystem.NotifyCameraRemoved( camera );

				if ( !camera.IsDisposed )
				{
					camera.Dispose();
				}
			}

			cameraList.Clear();
		}

		/// <summary>
		/// Internal method for creating shadow textures (texture-based shadows).
		/// </summary>
		protected internal virtual void CreateShadowTextures( ushort size, ushort count, PixelFormat format )
		{
			var baseName = "Axiom/ShadowTexture";

			if ( !IsShadowTechniqueTextureBased ||
			     shadowTextures.Count > 0 && count == shadowTextureCount && size == shadowTextureSize &&
			     format == shadowTextureFormat )
			{
				// no change
				return;
			}

			// destroy existing
			DestroyShadowTextures();

			// Recreate shadow textures
			for ( ushort t = 0; t < count; ++t )
			{
				var targName = string.Format( "{0}{1}", baseName, t );
				var matName = string.Format( "{0}Mat{1}", baseName, t );
				var camName = string.Format( "{0}Cam{1}", baseName, t );

				// try to get existing texture first, since we share these between
				// potentially multiple SMs
				var shadowTex = (Texture)TextureManager.Instance[ targName ];
				if ( shadowTex == null )
				{
					shadowTex = TextureManager.Instance.CreateManual( targName, ResourceGroupManager.InternalResourceGroupName,
					                                                  TextureType.TwoD, size, size, 0, format,
					                                                  TextureUsage.RenderTarget );
				}
				else if ( shadowTex.Width != size || shadowTex.Height != size || shadowTex.Format != format )
				{
					LogManager.Instance.Write(
						"Warning: shadow texture #{0} is shared " + "between scene managers but the sizes / formats " +
						"do not agree. Consider rationalizing your scene manager " + "shadow texture settings.", t );
				}
				shadowTex.Load();

				var shadowRTT = shadowTex.GetBuffer().GetRenderTarget();

				// Create camera for this texture, but note that we have to rebind
				// in prepareShadowTextures to coexist with multiple SMs
				var cam = CreateCamera( camName );
				cam.AspectRatio = 1.0f;
				// Don't use rendering distance for light cameras; we don't want shadows
				// for visible objects disappearing, especially for directional lights
				cam.UseRenderingDistance = false;
				shadowTextureCameras.Add( cam );

				// Create a viewport, if not there already
				if ( shadowRTT.NumViewports == 0 )
				{
					// Note camera assignment is transient when multiple SMs
					var view = shadowRTT.AddViewport( cam );
					view.SetClearEveryFrame( true );
					// remove overlays
					view.ShowOverlays = false;
				}

				// Don't update automatically - we'll do it when required
				shadowRTT.IsAutoUpdated = false;
				shadowTextures.Add( shadowTex );

				// Also create corresponding Material used for rendering this shadow
				var mat = (Material)MaterialManager.Instance[ matName ];
				if ( mat == null )
				{
					mat = (Material)MaterialManager.Instance.Create( matName, ResourceGroupManager.InternalResourceGroupName );
				}

				// create texture unit referring to render target texture
				var texUnit = mat.GetTechnique( 0 ).GetPass( 0 ).CreateTextureUnitState( targName );
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
			var savedStage = illuminationStage;
			illuminationStage = IlluminationRenderStage.RenderToTexture;

			// Determine far shadow distance
			var shadowDist = shadowFarDistance;
			if ( shadowDist == 0.0f )
			{
				// need a shadow distance, make one up
				shadowDist = camera.Near*300;
			}
			// set fogging to hide the shadow edge
			var shadowOffset = shadowDist*shadowTextureOffset;
			// Precalculate fading info
			var shadowEnd = shadowDist + shadowOffset;
			var fadeStart = shadowEnd*shadowTextureFadeStart;
			var fadeEnd = shadowEnd*shadowTextureFadeEnd;
			// Additive lighting should not use fogging, since it will overbrighten; use border clamp
			if ( !IsShadowTechniqueAdditive )
			{
				shadowReceiverPass.SetFog( true, FogMode.Linear, ColorEx.White, 0, fadeStart, fadeEnd );
				// if we have a custom receiver material, then give it the fog params too
				if ( shadowTextureCustomReceiverPass != null )
				{
					shadowTextureCustomReceiverPass.SetFog( true, FogMode.Linear, ColorEx.White, 0, fadeStart, fadeEnd );
				}
			}
			else
			{
				// disable fogging explicitly
				shadowReceiverPass.SetFog( true, FogMode.None );
				// if we have a custom receiver material, then give it the fog params too
				if ( shadowTextureCustomReceiverPass != null )
				{
					shadowTextureCustomReceiverPass.SetFog( true, FogMode.None );
				}
			}

			// Iterate over the lights we've found, max out at the limit of light textures
			var sti = 0;
			foreach ( var light in lightsAffectingFrustum )
			{
				// Check limit reached
				if ( sti == shadowTextures.Count )
				{
					break;
				}

				// Skip non-shadowing lights
				if ( !light.CastShadows )
				{
					continue;
				}

				var shadowTex = shadowTextures[ sti ];
				RenderTarget shadowRTT = shadowTex.GetBuffer().GetRenderTarget();
				var shadowView = shadowRTT.GetViewport( 0 );
				var texCam = shadowTextureCameras[ sti ];

				// rebind camera, incase another SM in use which has switched to its cam
				shadowView.Camera = texCam;

				// Associate main view camera as LOD camera
				texCam.LodCamera = camera;

				//Vector3 dir;

				// set base
				if ( light.Type == LightType.Point )
				{
					texCam.Direction = light.DerivedDirection;
				}
				if ( light.Type == LightType.Directional )
				{
					texCam.Position = light.GetDerivedPosition();
				}

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
			illuminationStage = savedStage;

			//fireShadowTexturesUpdated( std::min(mLightsAffectingFrustum.size(), mShadowTextures.size()));
		}

		/// <summary>
		///		Internal method for setting the destination viewport for the next render.
		/// </summary>
		/// <param name="viewport"></param>
		protected virtual void SetViewport( Viewport viewport )
		{
			currentViewport = viewport;
			// Set viewport in render system
			targetRenderSystem.Viewport = viewport;
			// Set the active material scheme for this viewport
			MaterialManager.Instance.ActiveScheme = viewport.MaterialScheme;
		}


		[OgreVersion( 1, 7, 2790, "Implement _gpuParamsDirty logic" )]
		protected virtual void UpdateGpuProgramParameters( Pass pass )
		{
			if ( pass.IsProgrammable )
			{
				if ( _gpuParamsDirty == 0 )
				{
					return;
				}

				if ( _gpuParamsDirty != 0 )
				{
					pass.UpdateAutoParams( autoParamDataSource, _gpuParamsDirty );
				}

				if ( pass.HasVertexProgram )
				{
					targetRenderSystem.BindGpuProgramParameters( GpuProgramType.Vertex, pass.VertexProgramParameters, _gpuParamsDirty );
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
		protected virtual void RenderSingleObject( IRenderable renderable, Pass pass, bool doLightIteration,
		                                           LightList manualLightList )
		{
			ushort numMatrices = 0;

			// grab the current scene detail level
			var camPolyMode = cameraInProgress.PolygonMode;

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
				targetRenderSystem.WorldMatrix = xform[ 0 ];
			}

			// issue view/projection changes (if any)
			UseRenderableViewProjection( renderable );

			if ( !suppressRenderStateChanges )
			{
				var passSurfaceAndLightParams = true;
				if ( pass.IsProgrammable )
				{
					// Tell auto params object about the renderable change
					autoParamDataSource.CurrentRenderable = renderable;
					//pass.UpdateAutoParamsNoLights( this.autoParamDataSource );

					if ( pass.HasVertexProgram )
					{
						passSurfaceAndLightParams = pass.VertexProgram.PassSurfaceAndLightStates;
					}
				}

				// issue texture units that depend on updated view matrix
				// reflective env mapping is one case
				for ( var i = 0; i < pass.TextureUnitStatesCount; i++ )
				{
					var texUnit = pass.GetTextureUnitState( i );

					if ( texUnit.HasViewRelativeTexCoordGen )
					{
						targetRenderSystem.SetTextureUnitSettings( i, texUnit );
						//this.targetRenderSystem.SetTextureUnit( i, texUnit, !pass.HasFragmentProgram );
					}
				}

				// Normalize normals
				var thisNormalize = renderable.NormalizeNormals;

				if ( thisNormalize != normalizeNormals )
				{
					targetRenderSystem.NormalizeNormals = thisNormalize;
					normalizeNormals = thisNormalize;
				}

				// Set up the solid / wireframe override
				var requestedMode = pass.PolygonMode;
				if ( renderable.PolygonModeOverrideable == true )
				{
					// check camera detial only when render detail is overridable
					if ( requestedMode > camPolyMode )
					{
						// only downgrade detail; if cam says wireframe we don't go up to solid
						requestedMode = camPolyMode;
					}
				}

				if ( requestedMode != lastPolyMode )
				{
					targetRenderSystem.PolygonMode = requestedMode;
					lastPolyMode = requestedMode;
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
					var rendLightList = renderable.Lights;
					var iteratePerLight = pass.IteratePerLight;
					var numIterations = iteratePerLight ? rendLightList.Count : 1;
					LightList lightListToUse = null;

					for ( var i = 0; i < numIterations; i++ )
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
							autoParamDataSource.SetCurrentLightList( lightListToUse );
							//pass.UpdateAutoParamsLightsOnly( this.autoParamDataSource );

							UpdateGpuProgramParameters( pass );
						}

						// Do we need to update light states?
						// Only do this if fixed-function vertex lighting applies
						if ( pass.LightingEnabled && passSurfaceAndLightParams )
						{
							targetRenderSystem.UseLights( lightListToUse, pass.MaxSimultaneousLights );
						}
						targetRenderSystem.CurrentPassIterationCount = pass.IterationCount;
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
							//pass.UpdateAutoParamsLightsOnly( this.autoParamDataSource );
						}

						UpdateGpuProgramParameters( pass );
					}

					// Use manual lights if present, and not using vertex programs
					if ( manualLightList != null && pass.LightingEnabled && passSurfaceAndLightParams )
					{
						targetRenderSystem.UseLights( manualLightList, pass.MaxSimultaneousLights );
					}
					targetRenderSystem.CurrentPassIterationCount = pass.IterationCount;
					// issue the render op
					targetRenderSystem.Render( op );
				}
			}
			else
			{
				// suppressRenderStateChanges
				// Just render
				targetRenderSystem.CurrentPassIterationCount = 1;
				targetRenderSystem.Render( op );
			}

			// Reset view / projection changes if any
			ResetViewProjectionMode();
		}

		/// <summary>
		///		Renders a set of solid objects.
		/// </summary>
		protected virtual void RenderSolidObjects( System.Collections.SortedList list, bool doLightIteration,
		                                           LightList manualLightList )
		{
			// ----- SOLIDS LOOP -----
			for ( var i = 0; i < list.Count; i++ )
			{
				var renderables = (RenderableList)list.GetByIndex( i );

				// bypass if this group is empty
				if ( renderables.Count == 0 )
				{
					continue;
				}

				var pass = (Pass)list.GetKey( i );

				// Give SM a chance to eliminate this pass
				if ( !ValidatePassForRendering( pass ) )
				{
					continue;
				}

				// For solids, we try to do each pass in turn
				var usedPass = SetPass( pass );

				// render each object associated with this rendering pass
				for ( var r = 0; r < renderables.Count; r++ )
				{
					var renderable = (IRenderable)renderables[ r ];

					// Give SM a chance to eliminate
					if ( !ValidateRenderableForRendering( usedPass, renderable ) )
					{
						continue;
					}

					// Render a single object, this will set up auto params if required
					RenderSingleObject( renderable, usedPass, doLightIteration, manualLightList );
				}
			}
		}

		protected void RenderSolidObjects( System.Collections.SortedList list, bool doLightIteration )
		{
			RenderSolidObjects( list, doLightIteration, null );
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
			for ( var i = 0; i < list.Count; i++ )
			{
				var rp = (RenderablePass)list[ i ];

				// set the pass first
				SetPass( rp.pass );

				// render the transparent object
				RenderSingleObject( rp.renderable, rp.pass, doLightIteration, manualLightList );
			}
		}

		protected void RenderTransparentObjects( List<RenderablePass> list, bool doLightIteration )
		{
			RenderTransparentObjects( list, doLightIteration, null );
		}

		/// <summary>
		///		Render a group with the added complexity of additive stencil shadows.
		/// </summary>
		/// <param name="group">Render queue group.</param>
		protected virtual void RenderAdditiveStencilShadowedQueueGroupObjects( RenderQueueGroup group )
		{
			var tempLightList = new LightList();

			foreach ( var priorityGroup in group.PriorityGroups.Values )
			{
				// sort the group first
				priorityGroup.Sort( cameraInProgress );

				// Clear light list
				tempLightList.Clear();

				// Render all the ambient passes first, no light iteration, no lights
				illuminationStage = IlluminationRenderStage.Ambient;
				RenderSolidObjects( priorityGroup.solidPasses, false, tempLightList );
				// Also render any objects which have receive shadows disabled
				renderingNoShadowQueue = true;
				RenderSolidObjects( priorityGroup.solidPassesNoShadow, true );
				renderingNoShadowQueue = false;

				// Now iterate per light
				illuminationStage = IlluminationRenderStage.PerLight;

				foreach ( var light in lightsAffectingFrustum )
				{
					// Set light state

					if ( light.CastShadows )
					{
						// Clear stencil
						targetRenderSystem.ClearFrameBuffer( FrameBufferType.Stencil );
						RenderShadowVolumesToStencil( light, cameraInProgress );
						// turn stencil check on
						targetRenderSystem.StencilCheckEnabled = true;
						// NB we render where the stencil is equal to zero to render lit areas
						targetRenderSystem.SetStencilBufferParams( CompareFunction.Equal, 0 );
					}

					// render lighting passes for this light
					tempLightList.Clear();
					tempLightList.Add( light );

					RenderSolidObjects( priorityGroup.solidPassesDiffuseSpecular, false, tempLightList );

					// Reset stencil params
					targetRenderSystem.SetStencilBufferParams();
					targetRenderSystem.StencilCheckEnabled = false;
					targetRenderSystem.SetDepthBufferParams();
				} // for each light

				// Now render decal passes, no need to set lights as lighting will be disabled
				illuminationStage = IlluminationRenderStage.Decal;
				RenderSolidObjects( priorityGroup.solidPassesDecal, false );
			} // for each priority

			// reset lighting stage
			illuminationStage = IlluminationRenderStage.None;

			// Iterate again
			foreach ( var priorityGroup in group.PriorityGroups.Values )
			{
				// Do transparents
				RenderTransparentObjects( priorityGroup.transparentPasses, true );
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
			foreach ( var priorityGroup in group.PriorityGroups.Values )
			{
				// sort the group first
				priorityGroup.Sort( cameraInProgress );

				// do solids
				RenderSolidObjects( priorityGroup.solidPasses, true );
			}

			// iterate over lights, rendering all volumes to the stencil buffer
			foreach ( var light in lightsAffectingFrustum )
			{
				if ( light.CastShadows )
				{
					// clear the stencil buffer
					targetRenderSystem.ClearFrameBuffer( FrameBufferType.Stencil );
					RenderShadowVolumesToStencil( light, cameraInProgress );

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
			} // for each light

			foreach ( var priorityGroup in group.PriorityGroups.Values )
			{
				// Do non-shadowable solids
				renderingNoShadowQueue = true;
				RenderSolidObjects( priorityGroup.solidPassesNoShadow, true );
				renderingNoShadowQueue = false;
			} // for each priority

			foreach ( var priorityGroup in group.PriorityGroups.Values )
			{
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
			if ( IsShadowTechniqueAdditive )
			{
				autoParamDataSource.AmbientLight = ColorEx.Black;
				targetRenderSystem.AmbientLight = ColorEx.Black;
			}
			else
			{
				autoParamDataSource.AmbientLight = shadowColor;
				targetRenderSystem.AmbientLight = shadowColor;
			}

			// Iterate through priorities
			foreach ( var priorityGroup in group.PriorityGroups.Values )
			{
				// Sort the queue first
				priorityGroup.Sort( cameraInProgress );

				// Do solids, override light list in case any vertex programs use them
				RenderSolidObjects( priorityGroup.solidPasses, false, nullLightList );
				renderingNoShadowQueue = true;
				RenderSolidObjects( priorityGroup.solidPassesNoShadow, false, nullLightList );
				renderingNoShadowQueue = false;

				// Do transparents that cast shadows
				RenderTransparentShadowCasterObjects( priorityGroup.transparentPasses, false, nullLightList );
			} // for each priority

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
			Now, this means we are going to reorder things more, but that is required
			if the shadows are to look correct. The overall order is preserved anyway,
			it's just that all the transparents are at the end instead of them being
			interleaved as in the normal rendering loop.
			*/
			// Iterate through priorities
			foreach ( var priorityGroup in group.PriorityGroups.Values )
			{
				// Sort the queue first
				priorityGroup.Sort( cameraInProgress );

				// Do solids
				RenderSolidObjects( priorityGroup.solidPasses, true );
				renderingNoShadowQueue = true;
				RenderSolidObjects( priorityGroup.solidPassesNoShadow, true );
				renderingNoShadowQueue = false;
			}

			// Iterate over lights, render received shadows
			// only perform this if we're in the 'normal' render stage, to avoid
			// doing it during the render to texture
			if ( illuminationStage == IlluminationRenderStage.None )
			{
				illuminationStage = IlluminationRenderStage.RenderReceiverPass;

				var sti = 0;
				foreach ( var light in lightsAffectingFrustum )
				{
					// Check limit reached
					if ( sti == shadowTextures.Count )
					{
						break;
					}

					if ( !light.CastShadows )
					{
						continue;
					}

					var shadowTex = shadowTextures[ sti ];
					var cam = shadowTex.GetBuffer().GetRenderTarget().GetViewport( 0 ).Camera;

					// Hook up receiver texture
					var targetPass = shadowTextureCustomReceiverPass != null ? shadowTextureCustomReceiverPass : shadowReceiverPass;
					var textureUnit = targetPass.GetTextureUnitState( 0 );
					textureUnit.SetTextureName( shadowTex.Name );

					// Hook up projection frustum if fixed-function, but also need to
					// disable it explicitly for program pipeline.
					textureUnit.SetProjectiveTexturing( !targetPass.HasVertexProgram, cam );

					// clamp to border color in case this is a custom material
					textureUnit.SetTextureAddressingMode( TextureAddressing.Border );
					textureUnit.TextureBorderColor = ColorEx.White;

					autoParamDataSource.SetTextureProjector( cam );

					// if this light is a spotlight, we need to add the spot fader layer
					// BUT not if using a custom projection matrix, since then it will be
					// inappropriately shaped most likely
					if ( light.Type == LightType.Spotlight && !cam.IsCustomProjectionMatrixEnabled )
					{
						// remove all TUs except 0 & 1
						// (only an issue if additive shadows have been used)
						while ( targetPass.TextureUnitStatesCount > 2 )
						{
							targetPass.RemoveTextureUnitState( 2 );
						}

						// Add spot fader if not present already
						if ( targetPass.TextureUnitStatesCount == 2 &&
						     targetPass.GetTextureUnitState( 1 ).TextureName == "spot_shadow_fade.png" )
						{
							// Just set
							var tex = targetPass.GetTextureUnitState( 1 );
							tex.SetProjectiveTexturing( !targetPass.HasVertexProgram, cam );
						}
						else
						{
							// Remove any non-conforming spot layers
							while ( targetPass.TextureUnitStatesCount > 1 )
							{
								targetPass.RemoveTextureUnitState( 1 );
							}

							var tex = targetPass.CreateTextureUnitState( "spot_shadow_fade.png" );
							tex.SetProjectiveTexturing( !targetPass.HasVertexProgram, cam );
							tex.SetColorOperation( LayerBlendOperation.Add );
							tex.SetTextureAddressingMode( TextureAddressing.Clamp );
						}
					}
					else
					{
						// remove all TUs except 0 including spot
						while ( targetPass.TextureUnitStatesCount > 1 )
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

					RenderTextureShadowReceiverQueueGroupObjects( group );
					++sti;
				} // for each light

				illuminationStage = IlluminationRenderStage.None;
			}

			// Iterate again
			foreach ( var priorityGroup in group.PriorityGroups.Values )
			{
				// Do transparents
				RenderTransparentObjects( priorityGroup.transparentPasses, true );
			} // for each priority
		}

		/// <summary>
		///		Render a group with the added complexity of additive texture shadows.
		/// </summary>
		/// <param name="group">Render queue group.</param>
		private void RenderAdditiveTextureShadowedQueueGroupObjects( RenderQueueGroup group )
		{
			var tempLightList = new LightList();
			foreach ( var priorityGroup in group.PriorityGroups.Values )
			{
				// Sort the queue first
				priorityGroup.Sort( cameraInProgress );

				// Clear light list
				tempLightList.Clear();

				// Render all the ambient passes first, no light iteration, no lights
				RenderSolidObjects( priorityGroup.solidPasses, false, tempLightList );
				// Also render any objects which have receive shadows disabled
				renderingNoShadowQueue = true;
				RenderSolidObjects( priorityGroup.solidPassesNoShadow, true );
				renderingNoShadowQueue = false;

				// only perform this next part if we're in the 'normal' render stage, to avoid
				// doing it during the render to texture
				if ( illuminationStage == IlluminationRenderStage.None )
				{
					// Iterate over lights, render masked
					var sti = 0;
					foreach ( var light in lightsAffectingFrustum )
					{
						// Set light state
						if ( light.CastShadows && sti < shadowTextures.Count )
						{
							var shadowTex = shadowTextures[ sti ];
							// Get camera for current shadow texture
							var camera = shadowTex.GetBuffer().GetRenderTarget().GetViewport( 0 ).Camera;
							// Hook up receiver texture
							var targetPass = shadowTextureCustomReceiverPass != null ? shadowTextureCustomReceiverPass : shadowReceiverPass;
							targetPass.GetTextureUnitState( 0 ).SetTextureName( shadowTex.Name );
							// Hook up projection frustum
							targetPass.GetTextureUnitState( 0 ).SetProjectiveTexturing( true, camera );
							autoParamDataSource.SetTextureProjector( camera );
							// Remove any spot fader layer
							if ( targetPass.TextureUnitStatesCount > 1 &&
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
							illuminationStage = IlluminationRenderStage.RenderReceiverPass;
						}
						else
						{
							illuminationStage = IlluminationRenderStage.None;
						}

						// render lighting passes for this light
						tempLightList.Clear();
						tempLightList.Add( light );

						RenderSolidObjects( priorityGroup.solidPassesDiffuseSpecular, false, tempLightList );
					} // for each light
					illuminationStage = IlluminationRenderStage.None;

					// Now render decal passes, no need to set lights as lighting will be disabled
					RenderSolidObjects( priorityGroup.solidPassesDecal, false );
				}
			} // for each priority

			foreach ( var priorityGroup in group.PriorityGroups.Values )
			{
				// Do transparents
				RenderTransparentObjects( priorityGroup.transparentPasses, true );
			} // for each priority
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
			foreach ( var priorityGroup in group.PriorityGroups.Values )
			{
				// Do solids, override light list in case any vertex programs use them
				RenderSolidObjects( priorityGroup.solidPasses, false, nullLightList );

				// Don't render transparents or passes which have shadow receipt disabled
			} // for each priority

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
			if ( !suppressShadows && currentViewport.ShowShadows &&
			     ( ( IsShadowTechniqueModulative && illuminationStage == IlluminationRenderStage.RenderReceiverPass ) ||
			       illuminationStage == IlluminationRenderStage.RenderToTexture || suppressRenderStateChanges ) && pass.Index > 0 )
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
			if ( !suppressShadows && currentViewport.ShowShadows && IsShadowTechniqueTextureBased )
			{
				if ( illuminationStage == IlluminationRenderStage.RenderReceiverPass && renderable.CastsShadows &&
				     !shadowTextureSelfShadow )
				{
					return false;
				}
				// Some duplication here with validatePassForRendering, for transparents
				if ( ( ( IsShadowTechniqueModulative && illuminationStage == IlluminationRenderStage.RenderReceiverPass ) ||
				       illuminationStage == IlluminationRenderStage.RenderToTexture || suppressRenderStateChanges ) &&
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
			var doShadows = group.ShadowsEnabled && currentViewport.ShowShadows && !suppressShadows &&
			                !suppressRenderStateChanges;
			if ( doShadows && shadowTechnique == ShadowTechnique.StencilAdditive )
			{
				RenderAdditiveStencilShadowedQueueGroupObjects( group );
			}
			else if ( doShadows && shadowTechnique == ShadowTechnique.StencilModulative )
			{
				RenderModulativeStencilShadowedQueueGroupObjects( group );
			}
			else if ( IsShadowTechniqueTextureBased )
			{
				// Modulative texture shadows in use
				if ( illuminationStage == IlluminationRenderStage.RenderToTexture )
				{
					// Shadow caster pass
					if ( currentViewport.ShowShadows && !suppressShadows && !suppressRenderStateChanges )
					{
						RenderTextureShadowCasterQueueGroupObjects( group );
					}
				}
				else
				{
					// Ordinary + receiver pass
					if ( doShadows )
					{
						if ( IsShadowTechniqueAdditive )
						{
							RenderAdditiveTextureShadowedQueueGroupObjects( group );
						}
						else
						{
							RenderModulativeTextureShadowedQueueGroupObjects( group );
						}
					}
					else
					{
						RenderBasicQueueGroupObjects( group );
					}
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
			foreach ( var priorityGroup in group.PriorityGroups.Values )
			{
				// Sort the queue first
				priorityGroup.Sort( cameraInProgress );

				// Do solids
				RenderSolidObjects( priorityGroup.solidPasses, true );

				// Do transparents
				RenderTransparentObjects( priorityGroup.transparentPasses, true );
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
		protected virtual void RenderTransparentShadowCasterObjects( List<RenderablePass> list, bool doLightIteration,
		                                                             LightList manualLightList )
		{
			// ----- TRANSPARENT LOOP as in RenderTransparentObjects, but changed a bit -----
			for ( var i = 0; i < list.Count; i++ )
			{
				var rp = list[ i ];

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
			for ( var i = 0; i < GetRenderQueue().NumRenderQueueGroups; i++ )
			{
				var queueID = GetRenderQueue().GetRenderQueueGroupID( i );
				var queueGroup = GetRenderQueue().GetQueueGroupByIndex( i );

				if ( !specialCaseRenderQueueList.IsRenderQueueToBeProcessed( queueID ) )
				{
					continue;
				}

				if ( queueID == RenderQueueGroupID.Main )
				{
					renderingMainGroup = true;
				}
				var repeatQueue = false;

				// repeat
				do
				{
					if ( OnRenderQueueStarted( queueID,
					                           illuminationStage == IlluminationRenderStage.RenderToTexture
					                           	? String.Empty
					                           	: String.Empty ) )
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
					repeatQueue = OnRenderQueueEnded( queueID,
					                                  illuminationStage == IlluminationRenderStage.RenderToTexture
					                                  	? String.Empty
					                                  	: String.Empty );
				}
				while ( repeatQueue );

				renderingMainGroup = false;
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

				for ( var plane = 0; plane < 6; plane++ )
				{
					GetRenderQueue().AddRenderable( skyBoxEntities[ plane ].GetSubEntity( 0 ), 1, qid );
				}
			}

			// if the skydome is enabled, queue up all the planes
			if ( isSkyDomeEnabled )
			{
				qid = isSkyDomeDrawnFirst ? RenderQueueGroupID.SkiesEarly : RenderQueueGroupID.SkiesLate;

				for ( var plane = 0; plane < 5; ++plane )
				{
					GetRenderQueue().AddRenderable( skyDomeEntities[ plane ].GetSubEntity( 0 ), 1, qid );
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
			var squaredRadius = radius*radius;

			var lightList = GetMovableObjectCollection( LightFactory.TypeName );

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
						var range = light.AttenuationRange;
						if ( light.tempSquaredDist <= ( range*range ) )
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
		public virtual void SetSkyPlane( bool enable, Plane plane, string materialName, float scale, float tiling,
		                                 bool drawFirst, float bow, string groupName )
		{
			isSkyPlaneEnabled = enable;

			if ( enable )
			{
				var meshName = "SkyPlane";
				skyPlane = plane;

				var m = (Material)MaterialManager.Instance[ materialName ];

				if ( m == null )
				{
					throw new AxiomException( string.Format( "Skyplane material '{0}' not found.", materialName ) );
				}

				// make sure the material doesn't update the depth buffer
				m.DepthWrite = false;
				m.Load();

				isSkyPlaneDrawnFirst = drawFirst;

				// set up the place
				var planeMesh = (Mesh)MeshManager.Instance[ meshName ];

				// unload the old one if it exists
				if ( planeMesh != null )
				{
					MeshManager.Instance.Unload( planeMesh );
				}

				// create up vector
				var up = plane.Normal.Cross( Vector3.UnitX );
				if ( up == Vector3.Zero )
				{
					up = plane.Normal.Cross( -Vector3.UnitZ );
				}

				if ( bow > 0 )
				{
					planeMesh = MeshManager.Instance.CreateCurvedIllusionPlane( meshName, groupName, plane, scale*100, scale*100,
					                                                            scale*bow*100, 6, 6, false, 1, tiling, tiling, up );
				}
				else
				{
					planeMesh = MeshManager.Instance.CreatePlane( meshName, groupName, plane, scale*100, scale*100, 1, 1, false, 1,
					                                              tiling, tiling, up );
				}

				if ( skyPlaneEntity != null )
				{
					RemoveEntity( skyPlaneEntity );
				}

				// create entity for the plane, using the mesh name
				skyPlaneEntity = CreateEntity( meshName, meshName );
				skyPlaneEntity.MaterialName = materialName;
				// sky entities need not cast shadows
				skyPlaneEntity.CastShadows = false;

				if ( skyPlaneNode == null )
				{
					skyPlaneNode = CreateSceneNode( meshName + "Node" );
				}
				else
				{
					skyPlaneNode.DetachAllObjects();
				}

				// attach the skyplane to the new node
				skyPlaneNode.AttachObject( skyPlaneEntity );
			}
		}

		/// <summary>
		///		Overload.
		/// </summary>
		public virtual void SetSkyPlane( bool enable, Plane plane, string materialName )
		{
			// call the overloaded method
			SetSkyPlane( enable, plane, materialName, 1000.0f, 10.0f, true, 0, ResourceGroupManager.DefaultResourceGroupName );
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
			rootSceneNode = node;
		}

		public void RestoreRootSceneNode()
		{
			rootSceneNode = defaultRootNode;
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
			var usedPass = SetPass( pass, false, shadowDerivation );
			RenderSingleObject( rend, usedPass, false );
		}

		public virtual void InjectRenderWithPass( Pass pass, IRenderable rend )
		{
			InjectRenderWithPass( pass, rend, true );
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
			if ( staticGeometryList.ContainsKey( name ) )
			{
				throw new AxiomException( "StaticGeometry with name '" + name + "' already exists!" );
			}
			var geometry = new StaticGeometry( this, name, logLevel );
			staticGeometryList[ name ] = geometry;
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
			if ( !staticGeometryList.TryGetValue( name, out geometry ) )
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
			return staticGeometryList.ContainsKey( name );
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
			if ( !staticGeometryList.TryGetValue( name, out geometry ) )
			{
				throw new AxiomException( "StaticGeometry with name '" + name + "' not found!" );
			}
			else
			{
				staticGeometryList.Remove( name );
				geometry.Destroy();
			}
		}

		/// <summary>
		///     Destroy all StaticGeometry instances.
		/// </summary>
		public void DestroyAllStaticGeometry()
		{
			foreach ( var geometry in staticGeometryList.Values )
			{
				geometry.Destroy();
			}
			staticGeometryList.Clear();
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
			public void Prepare( bool lightInFrustum, PlaneBoundedVolumeList lightClipVolumes, Light light, Camera camera,
			                     List<ShadowCaster> shadowCasterList, float farDistSquared )
			{
				casterList = shadowCasterList;
				isLightInFrustum = lightInFrustum;
				lightClipVolumeList = lightClipVolumes;
				this.camera = camera;
				this.light = light;
				this.farDistSquared = farDistSquared;
			}

			#endregion Methods

			#region ISceneQueryListener Members

			public bool OnQueryResult( MovableObject sceneObject )
			{
				if ( sceneObject.CastShadows && sceneObject.IsVisible &&
				     sceneManager.SpecialCaseRenderQueueList.IsRenderQueueToBeProcessed( sceneObject.RenderQueueGroup ) )
				{
					if ( farDistSquared > 0 )
					{
						// Check object is within the shadow far distance
						var toObj = sceneObject.ParentNode.DerivedPosition - camera.DerivedPosition;
						float radius = sceneObject.GetWorldBoundingSphere().Radius;
						float dist = toObj.LengthSquared;

						if ( dist - ( radius*radius ) > farDistSquared )
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
						for ( var i = 0; i < lightClipVolumeList.Count; i++ )
						{
							var pbv = (PlaneBoundedVolume)lightClipVolumeList[ i ];

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
			return EstimateWorldGeometry( stream, string.Empty );
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
				return movableObjectCollectionMap;
			}
		}

		public MovableObjectCollection GetMovableObjectCollection( string typeName )
		{
			// lock collection mutex
			lock ( movableObjectCollectionMap )
			{
				if ( movableObjectCollectionMap.ContainsKey( typeName ) )
				{
					return movableObjectCollectionMap[ typeName ];
				}
				else
				{
					var newCol = new MovableObjectCollection();
					movableObjectCollectionMap.Add( typeName, newCol );
					return newCol;
				}
			}
		}

		/// <summary>
		/// Create a movable object of the type specified.
		/// </summary>
		/// <remarks>
		/// This is the generalised form of MovableObject creation where you can
		/// create a MovableObject of any specialised type generically, including
		/// any new types registered using plugins.
		/// </remarks>
		/// <param name="name">The name to give the object. Must be unique within type.</param>
		/// <param name="typeName">The type of object to create</param>
		/// <param name="para">Optional name/value pair list to give extra parameters to the created object.</param>
		[OgreVersion( 1, 7, 2 )]
		public virtual MovableObject CreateMovableObject( string name, string typeName, NamedParameterList para )
		{
			// Nasty hack to make generalized Camera functions work without breaking add-on SMs
			if ( typeName == "Camera" )
			{
				return CreateCamera( name );
			}

			// Check for duplicate names

			var objectMap = GetMovableObjectCollection( typeName );

			if ( objectMap.ContainsKey( name ) )
			{
				throw new AxiomException( "An object with the name {0} already exists in the list.", name );
			}

			var factory = Root.Instance.GetMovableObjectFactory( typeName );

			var newObj = factory.CreateInstance( name, this, para );
			objectMap.Add( name, newObj );
			return newObj;
		}

		public MovableObject CreateMovableObject( string name, string typeName )
		{
			return CreateMovableObject( name, typeName, null );
		}

		/// <summary>
		/// Create a movable object of the type specified without a name.
		/// </summary>
		/// <remarks>
		/// This is the generalised form of MovableObject creation where you can
		/// create a MovableObject of any specialised type generically, including
		/// any new types registered using plugins. The name is generated automatically.
		/// </remarks>
		/// <param name="typeName">The type of object to create</param>
		/// <param name="para">Optional name/value pair list to give extra parameters to the created object.</param>
		[OgreVersion( 1, 7, 2 )]
		public virtual MovableObject CreateMovableObject( string typeName, NamedParameterList para )
		{
			string name = movableNameGenerator.GetNextUniqueName();
			return CreateMovableObject( name, typeName, para );
		}

		public void DestroyMovableObject( string name, string typeName )
		{
			// Nasty hack to make generalized Camera functions work without breaking add-on SMs
			if ( typeName == "Camera" )
			{
				DestroyCamera( cameraList[ name ] );
				return;
			}

			var objectMap = GetMovableObjectCollection( typeName );

			if ( !objectMap.ContainsKey( name ) )
			{
				throw new AxiomException( "The object with the name " + name + " is not in the list." );
			}
			var factory = Root.Instance.GetMovableObjectFactory( typeName );
			var item = objectMap[ name ];
			objectMap.Remove( item.Name );
			factory.DestroyInstance( ref item );
		}

		public void DestroyAllMovableObjectsByType( string typeName )
		{
			// Nasty hack to make generalized Camera functions work without breaking add-on SMs
			if ( typeName == "Camera" )
			{
				DestroyAllCameras();
				return;
			}

			var objectMap = GetMovableObjectCollection( typeName );

			var factory = Root.Instance.GetMovableObjectFactory( typeName );

			lock ( objectMap )
			{
				foreach ( var movableObject in objectMap.Values )
				{
					if ( movableObject.Manager == this )
					{
						var tmp = movableObject;
						factory.DestroyInstance( ref tmp );
					}
				}
				objectMap.Clear();
			}
		}

		public void DestroyAllMovableObjects()
		{
			foreach ( var col in movableObjectCollectionMap )
			{
				var coll = col.Value;
				lock ( coll )
				{
					if ( Root.Instance.HasMovableObjectFactory( col.Key ) )
					{
						// Only destroy if we have a factory instance; otherwise must be injected
						var factory = Root.Instance.GetMovableObjectFactory( col.Key );
						foreach ( var movableObject in coll.Values )
						{
							if ( movableObject.Manager == this )
							{
								var tmp = movableObject;
								factory.DestroyInstance( ref tmp );
							}
						}
						coll.Clear();
					}
				}
			}
			movableObjectCollectionMap.Clear();
		}

		public MovableObject GetMovableObject( string name, string typeName )
		{
			// Nasty hack to make generalized Camera functions work without breaking add-on SMs
			if ( typeName == "Camera" )
			{
				return cameraList[ name ];
			}

			if ( !movableObjectCollectionMap.ContainsKey( typeName ) )
			{
				throw new AxiomException( "The factory for the type " + typeName + " does not exists in the collection." );
			}

			if ( !movableObjectCollectionMap[ typeName ].ContainsKey( name ) )
			{
				throw new AxiomException( "The object with the name " + name + " does not exists in the collection." );
			}

			return movableObjectCollectionMap[ typeName ][ name ];
		}

		public bool HasMovableObject( string name, string typeName )
		{
			// Nasty hack to make generalized Camera functions work without breaking add-on SMs
			if ( typeName == "Camera" )
			{
				return cameraList.ContainsKey( name );
			}

			return movableObjectCollectionMap.ContainsKey( typeName ) &&
			       movableObjectCollectionMap[ typeName ].ContainsKey( name );
		}

		public void DestroyMovableObject( MovableObject m )
		{
			DestroyMovableObject( m.Name, m.MovableType );
		}

		public void InjectMovableObject( MovableObject m )
		{
			var objectMap = GetMovableObjectCollection( m.MovableType );
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
			var objectMap = GetMovableObjectCollection( typeName );
			lock ( objectMap )
			{
				objectMap.Remove( name );
			}
		}

		public void ExtractMovableObject( MovableObject m )
		{
			ExtractMovableObject( m.Name, m.MovableType );
		}

		public void ExtractAllMovableObjectsByType( string typeName )
		{
			var objectMap = GetMovableObjectCollection( typeName );
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
			AddWorldFragmentType( WorldFragmentType.None );
		}

		public override void Execute( ISceneQueryListener listener )
		{
			var factories = Root.Instance.MovableObjectFactories;
			foreach ( var map in factories )
			{
				var movableObjects = creator.GetMovableObjectCollection( map.Value.Type );
				foreach ( var movableObject in movableObjects.Values )
				{
					// skip group if query type doesn't match
					if ( ( QueryTypeMask & movableObject.TypeFlags ) == 0 )
					{
						break;
					}

					// skip if unattached or filtered out by query flags
					if ( !movableObject.IsAttached || ( movableObject.QueryFlags & queryMask ) == 0 )
					{
						continue;
					}

					if ( box.Intersects( movableObject.GetWorldBoundingBox() ) )
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
			AddWorldFragmentType( WorldFragmentType.None );
		}

		public override void Execute( IRaySceneQueryListener listener )
		{
			var factories = Root.Instance.MovableObjectFactories;
			foreach ( var map in factories )
			{
				var movableObjects = creator.GetMovableObjectCollection( map.Value.Type );
				foreach ( var movableObject in movableObjects.Values )
				{
					// skip group if query type doesn't match
					if ( ( QueryTypeMask & movableObject.TypeFlags ) == 0 )
					{
						break;
					}

					// skip if unattached or filtered out by query flags
					if ( !movableObject.IsAttached || ( movableObject.QueryFlags & queryMask ) == 0 )
					{
						continue;
					}

					// test the intersection against the world bounding box of the entity
					var results = Utility.Intersects( ray, movableObject.GetWorldBoundingBox() );

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
			AddWorldFragmentType( WorldFragmentType.None );
		}

		public override void Execute( ISceneQueryListener listener )
		{
			var testSphere = new Sphere();

			var factories = Root.Instance.MovableObjectFactories;
			foreach ( var map in factories )
			{
				var movableObjects = creator.GetMovableObjectCollection( map.Value.Type );
				foreach ( var movableObject in movableObjects.Values )
				{
					// skip group if query type doesn't match
					if ( ( QueryTypeMask & movableObject.TypeFlags ) == 0 )
					{
						break;
					}

					// skip if unattached or filtered out by query flags
					if ( !movableObject.IsAttached || ( movableObject.QueryFlags & queryMask ) == 0 )
					{
						continue;
					}

					testSphere.Center = movableObject.ParentNode.DerivedPosition;
					testSphere.Radius = movableObject.BoundingRadius;

					// if the results came back positive, fire the event handler
					if ( sphere.Intersects( testSphere ) )
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
			AddWorldFragmentType( WorldFragmentType.None );
		}

		public override void Execute( ISceneQueryListener listener )
		{
			var factories = Root.Instance.MovableObjectFactories;
			foreach ( var map in factories )
			{
				var movableObjects = creator.GetMovableObjectCollection( map.Value.Type );
				foreach ( var movableObject in movableObjects.Values )
				{
					// skip group if query type doesn't match
					if ( ( QueryTypeMask & movableObject.TypeFlags ) == 0 )
					{
						break;
					}

					// skip if unattached or filtered out by query flags
					if ( !movableObject.IsAttached || ( movableObject.QueryFlags & queryMask ) == 0 )
					{
						continue;
					}

					for ( var v = 0; v < volumes.Count; v++ )
					{
						var volume = (PlaneBoundedVolume)volumes[ v ];
						// Do AABB / plane volume test
						if ( ( movableObject.QueryFlags & queryMask ) != 0 && volume.Intersects( movableObject.GetWorldBoundingBox() ) )
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
			AddWorldFragmentType( WorldFragmentType.None );
		}

		public override void Execute( IIntersectionSceneQueryListener listener )
		{
			var factories = Root.Instance.MovableObjectFactories;
			IEnumerator enumFactories = factories.GetEnumerator();
			while ( enumFactories.Current != null )
			{
				var map = (KeyValuePair<string, MovableObjectFactory>)enumFactories.Current;
				var movableObjects = creator.GetMovableObjectCollection( map.Value.Type );
				var enumA = movableObjects.GetEnumerator();
				while ( enumA.Current != null )
				{
					var objectA = (MovableObject)enumA.Current;
					// skip group if query type doesn't match
					if ( ( QueryTypeMask & objectA.TypeFlags ) == 0 )
					{
						break;
					}

					// skip if unattached or filtered out by query flags
					if ( !objectA.IsInScene || ( objectA.QueryFlags & queryMask ) == 0 )
					{
						continue;
					}

					// Check against later objects in the same group
					var enumB = enumA;
					while ( enumB.Current != null )
					{
						var objectB = (MovableObject)enumB.Current;
						if ( ( ( QueryMask & objectB.QueryFlags ) != 0 ) && objectB.IsInScene )
						{
							var box1 = objectA.GetWorldBoundingBox();
							var box2 = objectB.GetWorldBoundingBox();

							if ( box1.Intersects( box2 ) )
							{
								listener.OnQueryResult( objectA, objectB );
							}
						}
						enumB.MoveNext();
					}

					// Check  against later groups
					var enumFactoriesLater = enumFactories;
					while ( enumFactoriesLater.Current != null )
					{
						var mapLater = (KeyValuePair<string, MovableObjectFactory>)enumFactoriesLater.Current;

						var movableObjectsLater = creator.GetMovableObjectCollection( mapLater.Value.Type );
						var enumC = movableObjectsLater.GetEnumerator();
						while ( enumC.Current != null )
						{
							var objectC = (MovableObject)enumC.Current;
							// skip group if query type doesn't match
							if ( ( QueryTypeMask & objectC.TypeFlags ) == 0 )
							{
								break;
							}

							if ( ( ( QueryMask & objectC.QueryFlags ) != 0 ) && objectC.IsInScene )
							{
								var box1 = objectA.GetWorldBoundingBox();
								var box2 = objectC.GetWorldBoundingBox();

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
				if ( metaDataInit )
				{
					InitMetaData();
					metaDataInit = false;
				}
				return metaData;
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