#region LGPL License
/*
Axiom Game Engine Library
Copyright (C) 2003  Axiom Project Team

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
using Axiom.Collections;
using Axiom.Controllers;
using Axiom.Animating;
using Axiom.Enumerations;
using Axiom.Exceptions;
using Axiom.SubSystems.Rendering;
using Axiom.MathLib;

namespace Axiom.Core
{

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
	///		This class defines the basic behaviour of the 'Scene Manager' family. These classes will
    ///		organise the objects in the scene and send them to the rendering system, a subclass of
    ///		RenderSystem. This basic superclass does no sorting, culling or organising of any sort.
    ///    <p/>
	///		Subclasses may use various techniques to organise the scene depending on how they are
    ///		designed (e.g. BSPs, octrees etc). As with other classes, methods marked as interanl are 
    ///		designed to be called by other classes in the engine, not by user applications.
    ///	 </remarks>
	// INC: In progress
	public class SceneManager
	{
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
		protected LightCollection lightList;
		/// <summary>A list of entities in the scene for easy lookup.</summary>
		protected EntityCollection entityList;
		/// <summary>A list of scene nodes (includes all in the scene graph).</summary>
		protected SceneNodeCollection sceneNodeList;
		/// <summary>A list of billboard set for easy lookup.</summary>
		protected BillboardSetCollection billboardSetList;
		/// <summary>A list of animations for easy lookup.</summary>
		protected AnimationCollection animationList;
		/// <summary>A list of animation states for easy lookup.</summary>
		protected AnimationStateCollection animationStateList;

		/// <summary>A reference to the current camera being used for rendering.</summary>
		protected Camera camInProgress;
		/// <summary>The root of the scene graph heirarchy.</summary>
		protected SceneNode rootSceneNode;

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

		/// <summary>Cache the last material used during SetMaterial so we can comapare and reduce state changes.</summary>
		private static Material lastMaterialUsed;
		private static bool firstTime = true;
		protected bool lastUsedFallback;
		private static int lastNumTexUnitsUsed = 0;

		// cached fog settings
		private static FogMode oldFogMode;
		private static ColorEx oldFogColor;
		private static float oldFogStart, oldFogEnd, oldFogDensity;

		#endregion

		#region Public events
		/// <summary>An event that will fire when a render queue is starting to be rendered.</summary>
		public event RenderQueueEvent QueueStarted;
		/// <summary>An event that will fire when a render queue is finished being rendered.</summary>
		public event RenderQueueEvent QueueEnded;
		#endregion

		#region Constructors

		public SceneManager()
		{
			// initialize all collections
			renderQueue = new RenderQueue();
			cameraList = new CameraCollection();
			lightList = new LightCollection();
			entityList = new EntityCollection();
			sceneNodeList = new SceneNodeCollection();
			billboardSetList = new BillboardSetCollection();
			animationList = new AnimationCollection();
			animationStateList = new AnimationStateCollection();

			// create the root scene node
			rootSceneNode = new SceneNode(this, "Root");

			// default to no fog
			fogMode = FogMode.None;

			// light list events
			lightList.Cleared +=new CollectionHandler(lightList_Cleared);
			lightList.ItemAdded += new CollectionHandler(lightList_ItemAdded);
			lightList.ItemRemoved += new CollectionHandler(lightList_ItemRemoved);
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
		public virtual SceneNode CreateSceneNode()
		{
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
		public virtual SceneNode CreateSceneNode(String name)
		{
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
		public virtual Animation CreateAnimation(String name, float length)
		{
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
		public virtual AnimationState CreateAnimationState(String animationName)
		{
			// do we have this already?
			if(animationStateList.ContainsKey(animationName))
				throw new Axiom.Exceptions.AxiomException("Cannot create a duplicate AnimationState for an Animation.");

			if(!animationList.ContainsKey(animationName))
				throw new Axiom.Exceptions.AxiomException(String.Format("The name of a valid animation must be supplied when creating an AnimationState.  Animation '{0}' does not exist.", animationName));

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
		public virtual BillboardSet CreateBillboardSet(String name)
		{
			// return new billboardset with a default pool size of 20
			return CreateBillboardSet(name, 20);
		}

		/// <summary>
		///		Creates a billboard set which can be uses for particles, sprites, etc.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="poolSize"></param>
		/// <returns></returns>
		public virtual BillboardSet CreateBillboardSet(String name, int poolSize)
		{
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
		public virtual Camera CreateCamera(String name)
		{
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
		public virtual Entity CreateEntity(String name, String meshName)
		{
			if(entityList.ContainsKey(name))
				throw new Axiom.Exceptions.AxiomException(String.Format("An entity with the name '{0}' already exists in the scene.", name));

			Mesh mesh = MeshManager.Instance.Load(meshName, 1);

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
		public virtual Entity CreateEntity(String name, PrefabEntity prefab)
		{
			// TODO: Implement CreateEntity
			return null;
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
		/// <param name="name"></param>
		/// <returns></returns>
		public virtual Light CreateLight(String name)
		{
			// create a new light and add it to our internal list
			Light light = new Light(name);
			
			// adding the light to the list fire an event which will add it to the render system
			lightList.Add(name, light);

			return light;
		}

		/// <summary>
		///		Creates a new (blank) material with the specified name.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public virtual Material CreateMaterial(String name)
		{
			// TODO: Implement CreateMaterial
			return null;
		}

		/// <summary>
		///		Empties the entire scene, inluding all SceneNodes, Cameras, Entities and Lights etc.
		/// </summary>
		public virtual void ClearScene()
		{
			// TODO: Implement ClearScene
		}

		/// <summary>
		///		Loads the source of the 'world' geometry, i.e. the large, mainly static geometry
		///		making up the world e.g. rooms, landscape etc.
		/// </summary>
		/// <remarks>
		///		Depending on the type of SceneManager (subclasses will be specialised
		///		for particular world geometry types) you have requested via the Root or
		///		SceneManagerEnumerator classes, you can pass a filename to this method and it
		///		will attempt to load the world-level geometry for use. If you try to load
		///		an inappropriate type of world data an exception will be thrown. The default
		///		SceneManager cannot handle any sort of world geometry and so will always
		///		throw an exception. However subclasses like BspSceneManager can load
		///		particular types of world geometry e.g. "q3dm1.bsp".
		/// </remarks>
		/// <param name="fileName"></param>
		public virtual void LoadWorldGeometry(String fileName)
		{
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
		protected int SetMaterial(Material material, int numLayersLeft)
		{
			// TODO: Complete Implementation of SetMaterial

			// Surface
			if(firstTime || !material.CompareSurfaceParams(lastMaterialUsed))
			{
				// set the surface params of the render system
				targetRenderSystem.SetSurfaceParams(material.Ambient, material.Diffuse, material.Specular, material.Emissive, material.Shininess);
			}

			// Scene Blending
			if(firstTime || lastUsedFallback ||
				lastMaterialUsed.SourceBlendFactor != material.SourceBlendFactor ||
				lastMaterialUsed.DestBlendFactor != material.DestBlendFactor)
			{
				targetRenderSystem.SetSceneBlending(material.SourceBlendFactor, material.DestBlendFactor);
			}

			// FOG
			ColorEx newFogColor;
			FogMode newFogMode;
			float newFogDensity, newFogStart, newFogEnd;

			// does the material wan't to override the fog mode?
			if(material.FogOverride)
			{
				newFogMode = material.FogMode;
				newFogColor = material.FogColor;
				newFogDensity = material.FogDensity;
				newFogStart = material.FogStart;
				newFogEnd = material.FogEnd;
			}
			else
			{
				newFogMode = fogMode;
				newFogColor = fogColor;
				newFogDensity = fogDensity;
				newFogStart = fogStart;
				newFogEnd = fogEnd;
			}

			// check to see if fog needs to be changed
			if(firstTime || newFogMode != oldFogMode || newFogColor != oldFogColor || 
				newFogStart != oldFogStart || newFogEnd != oldFogEnd || newFogDensity != oldFogDensity)
			{
				// set fog using the render system
				targetRenderSystem.SetFog(newFogMode, newFogColor, newFogDensity, newFogStart, newFogEnd);
				oldFogMode = newFogMode;
				oldFogColor = newFogColor;
				oldFogDensity = newFogDensity;
				oldFogStart = newFogStart;
				oldFogEnd = newFogEnd;
}

			// Texture layers
			int texLayer = material.NumTextureLayers - numLayersLeft;
			int requestedUnits = numLayersLeft;
			int texUnits = targetRenderSystem.Caps.NumTextureUnits;

			lastUsedFallback = false;

			int unit;

			for(unit = 0; 
				(unit < lastNumTexUnitsUsed || unit < requestedUnits) && unit < texUnits; unit++, texLayer++)
			{
				if(unit >= requestedUnits)
				{
					// ran out of texture layers before we ran out of units
					// disable texturing for this unit
					targetRenderSystem.DisableTextureUnit(unit);
				}
				else
				{
					TextureLayer layer = material.TextureLayers[texLayer];

					// still have texture layers to add to this unit
					if(unit == 0 && requestedUnits > 0 && requestedUnits < material.NumTextureLayers)
					{
						// if we got here, we are on the second or more pass of the render
						
						lastUsedFallback = true;

						TextureLayer tmpLayer = (TextureLayer)layer.Clone();

						tmpLayer.SetColorOperation(LayerBlendOperation.Replace);

						targetRenderSystem.SetSceneBlending(tmpLayer.ColorBlendFallbackSource, tmpLayer.ColorBlendFallbackDest);

						// set the texture layer for the current unit
						targetRenderSystem.SetTextureUnit(unit, tmpLayer);

					}
					else
					{
						if(lastUsedFallback)
							layer.SetAlphaOperation(LayerBlendOperationEx.Add);

						// set the texture layer for the current unit
						targetRenderSystem.SetTextureUnit(unit, layer);
					}

					numLayersLeft--;
				} // if (unit....
			} // for

			// DEPTH SETTINGS
			if(firstTime || lastMaterialUsed.DepthWrite != material.DepthWrite)
			{
				targetRenderSystem.DepthWrite = material.DepthWrite;
			}

			// TODO: CULLING

			// lighting enabled?
			if(firstTime || lastMaterialUsed.Lighting != material.Lighting)
			{
				targetRenderSystem.LightingEnabled = material.Lighting;
			}

			// TODO: SHADING MODE

			// texture filtering
			if(firstTime || lastMaterialUsed.TextureFiltering != material.TextureFiltering)
				targetRenderSystem.TextureFiltering = material.TextureFiltering;

			// save the last material used for comparison next time this is called
			lastMaterialUsed = material;

			// remeber how many layers this material had for the next material so we can disable
			// texture units no longer in use
			lastNumTexUnitsUsed = unit;

			return numLayersLeft;
		}

		/// <summary>
		///		Utility method for creating the planes of a skybox.
		/// </summary>
		/// <param name="plane"></param>
		/// <param name="distance"></param>
		/// <param name="orientation"></param>
		/// <returns></returns>
		protected Mesh CreateSkyboxPlane(BoxPlane plane, float distance, Quaternion orientation)
		{
			Plane p = new Plane();
			string meshName = "SkyboxPlane_";
			Vector3 up = Vector3.Zero;

			// set the distance of the plane
			p.D = distance;

			switch(plane)
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
			Mesh planeModel = (Mesh)modelMgr[meshName];

			// trash it if it already exists
			if(planeModel != null)
				modelMgr.Unload(planeModel);

			float planeSize = distance * 2;

			// create and return the plane mesh
			return modelMgr.CreatePlane(meshName, p, planeSize, planeSize, 1, 1, false, 1, 1, 1, up);
		}

		/// <summary>
		///		Utility method for creating the planes of a skydome.
		/// </summary>
		/// <param name="plane"></param>
		/// <param name="distance"></param>
		/// <param name="orientation"></param>
		/// <returns></returns>
		protected SubMesh CreateSkydomePlane(BoxPlane plane, float curvature, float tiling, float distance, Quaternion orientation)
		{
			// TODO: Implemenation of CreateSkydomePlane
			return null;
		}

		/// <summary>
		///		Protected method used by RenderVisibleObjects to deal with renderables
		///		which override the camera's own view / projection materices.
		/// </summary>
		/// <param name="renderable"></param>
		protected void UseRenderableViewProjection(IRenderable renderable)
		{
			// only change view/proj if the cam has changed 
			if(hasCameraChanged)
			{
				targetRenderSystem.ViewMatrix = camInProgress.ViewMatrix;
				targetRenderSystem.ProjectionMatrix = camInProgress.ProjectionMatrix;
			}

			// reset this flag so the view/proj wont be updated again this frame
			hasCameraChanged = false;
		}

		/// <summary>
		///		Used to first the QueueStarted event.  
		/// </summary>
		/// <param name="group"></param>
		/// <returns>True if the queue should be skipped.</returns>
		protected bool OnRenderQueueStarted(RenderQueueGroupID group)
		{
			// TODO: Implementation of OnRenderQueueStarted
			return false;
		}

		/// <summary>
		///		Used to first the QueueEnded event.  
		/// </summary>
		/// <param name="group"></param>
		/// <returns>True if the queue should be repeated.</returns>
		protected bool OnRenderQueueEnded(RenderQueueGroupID group)
		{
			// TODO: Implementation of OnRenderQueueEnded
			return false;
		}

		#endregion

		#region Public methods

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
		public void SetFog(FogMode mode, ColorEx color, float density, float linearStart, float linearEnd)
		{
			// set all the fog information
			fogMode = mode;
			fogColor = color;
			fogDensity = density;
			fogStart = linearStart;
			fogEnd = linearEnd;
		}

		/// <summary>
		///		Overloaded method.
		/// </summary>
		/// <param name="enable"></param>
		/// <param name="materialName"></param>
		/// <param name="distance"></param>
		public void SetSkyBox(bool enable, string materialName, float distance)
		{
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
		public void SetSkyBox(bool enable, string materialName, float distance, bool drawFirst, Quaternion orientation)
		{
			// enable the skybox?
			isSkyBoxEnabled = enable;

			if(enable)
			{
				Material s = (Material)MaterialManager.Instance[materialName];

				if(s == null)
					throw new AxiomException(String.Format("Could not find skybox material '{0}'", materialName));

				// dont update the depth buffer
				s.DepthWrite = false;

				// ensure texture clamping to reduce fuzzy edges when using filtering
				s.TextureLayers[0].TextureAddressing = TextureAddressing.Clamp;

				// load yourself numbnuts!
				s.Load();
	
				isSkyBoxDrawnFirst = drawFirst;

				if(skyBoxNode == null)
					skyBoxNode = CreateSceneNode("SkyBoxNode");
				else
					skyBoxNode.Objects.Clear();
	
				MaterialManager materialMgr = MaterialManager.Instance;

				// need to create 6 plane entities for each side of the skybox
				for(int i = 0; i < 6; i++)
				{
					Mesh planeModel = CreateSkyboxPlane((BoxPlane)i, distance, orientation);
					string entityName = "SkyBoxPlane" + i;

					if(skyBoxEntities[i] != null)
					{
						// TODO: Remove the entity dammit
					}

					// create an entity for this plane
					skyBoxEntities[i] = CreateEntity(entityName, planeModel.Name);

					// find the material for this plane
					// TODO: can we assume there is never a case where it already exists?  it shouldnt
					//Material boxMaterial = (Material)materialMgr[entityName];

					// close the material
					Material boxMaterial = (Material)s.Clone(entityName);

					// set the current frame
					// first 2 cases swap the front and back textures
					if(i == (int)TextureCubeFace.Back)
						boxMaterial.TextureLayers[0].CurrentFrame = (int)TextureCubeFace.Front;
					else if(i == (int)TextureCubeFace.Front)
						boxMaterial.TextureLayers[0].CurrentFrame = (int)TextureCubeFace.Back;
					else
						boxMaterial.TextureLayers[0].CurrentFrame = i;

					skyBoxEntities[i].MaterialName = boxMaterial.Name;

					skyBoxNode.Objects.Add(skyBoxEntities[i]);
				} // for
			}
		}

		#endregion

		#region Properties

		/// <summary>
		/// Gets/Sets the target render system that this scene manager should be using.
		/// </summary>
		public RenderSystem TargetRenderSystem
		{
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
		public SceneNode RootSceneNode
		{
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
		public ColorEx AmbientLight
		{
			get { return ambientColor; }
			set 
			{ 
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
			get { return optionList; }
		}

		/// <summary>
		///		Gets/Sets a value that forces all nodes to render their bounding boxes.
		/// </summary>
		public bool ShowBoundingBoxes
		{
			get { return showBoundingBoxes; }
			set { showBoundingBoxes = value; }
		}

		/// <summary>
		///		Gets/Sets whether or not to display the nodes themselves in addition to their objects.
		/// </summary>
		/// <remarks>
		///		What will be displayed is the local axes of the node (for debugging mainly).
		/// </remarks>
		public bool DisplayNodes
		{
			get { return displayNodes; }
			set { displayNodes = value; }
		}

		/// <summary>
		///		Gets the fog mode that was set during the last call to SetFog.
		/// </summary>
		public FogMode FogMode
		{
			get { return fogMode; }
		}

		/// <summary>
		///		Gets the fog starting point that was set during the last call to SetFog.
		/// </summary>
		public float FogStart
		{
			get { return fogStart; }
		}

		/// <summary>
		///		Gets the fog ending point that was set during the last call to SetFog.
		/// </summary>
		public float FogEnd
		{
			get { return fogEnd; }
		}

		/// <summary>
		///		Gets the fog density that was set during the last call to SetFog.
		/// </summary>
		public float FogDensity
		{
			get { return fogDensity; }
		}

		/// <summary>
		///		Gets the fog color that was set during the last call to SetFog.
		/// </summary>
		public ColorEx FogColor
		{
			get { return fogColor; }
		}

		public LightCollection Lights
		{
			get { return lightList; }
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
		internal void RenderScene(Camera camera, Viewport viewport, bool showOverlays)
        {
			// TODO: Complete RenderScene implementation
			camInProgress = camera;
			hasCameraChanged = true;

			// use this viewport for the rendering systems current pass
			targetRenderSystem.SetViewport(viewport);

			// apply animations
			ApplySceneAnimations();

			// update scene graph
			UpdateSceneGraph(camera);

			// update dynamic lights
			UpdateDynamicLights();

			// ask the camera to auto track if it has a target
			camera.AutoTrack();

			// clear the current render queue
			renderQueue.Clear();

			// find camera's visible objects
			FindVisibleObjects(camera);

			// TODO: Queue overlays

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
		internal virtual void UpdateSceneGraph(Camera camera)
		{
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
		internal virtual void FindVisibleObjects(Camera camera)
		{
			// ask the root node to iterate through and find visible objects in the scene
			rootSceneNode.FindVisibleObjects(camera, renderQueue, true, displayNodes);
		}

		/// <summary>
		///		Internal method for applying animations to scene nodes.
		/// </summary>
		/// <remarks>
		///		Uses the internally stored AnimationState objects to apply animation to SceneNodes.
		/// </remarks>
		internal virtual void ApplySceneAnimations()
		{
			for(int i = 0; i < animationStateList.Count; i++)
			{
				// get the current animation state
				AnimationState animState = animationStateList[i];

				// get this states animation
				Animation anim = animationList[animState.Name];

				// loop through all tracks and reset their nodes initial state
				for(int j = 0; j < anim.Tracks.Count; j++)
				{
					Node node = anim.Tracks[j].TargetNode;
					node.ResetToInitialState();
				}

				// apply the animation
				anim.Apply(animState.Time, animState.Weight, false);
			}
		}

		/// <summary>
		///		Sends visible objects found in FindVisibleObjects to the rendering engine.
		/// </summary>
		internal virtual void RenderVisibleObjects()
		{
			int renderCount = 0;

			// grab the current scene detail level and init the last detail level used
			SceneDetailLevel camDetailLevel = camInProgress.SceneDetail;
			SceneDetailLevel lastDetailLevel = camDetailLevel;

			// loop through each main render group ( which is already sorted)
			for(int i = 0; i < renderQueue.QueueGroups.Count; i++)
			{
				
				// get the current queue values
				RenderQueueGroupID groupID = (RenderQueueGroupID)renderQueue.QueueGroups.GetKeyAt(i);
				RenderQueueGroup group = (RenderQueueGroup)renderQueue.QueueGroups[i];

				// listeners to render queue events can return true, which signifies the queue should be repeated
				bool repeatQueue = false;

				// do as long as the current queue is to be repeated
				do
				{
					// if an event handler returns true, this queue will be skipped
					if(OnRenderQueueStarted(groupID))
						continue;

					// iterate through priority groups
					for(int j = 0; j < group.PriorityGroups.Count; j++)
					{
						// get the current priorty group
						RenderPriorityGroup priority = (RenderPriorityGroup)group.PriorityGroups[j];

						renderCount++;

						Matrix4[] xform;
						RenderOperation op = new RenderOperation();
						int materialLayersLeft;
						Material currentMaterial = null;
						ushort numMatrices;

						// *** Non Transparent Entity Loop ***
						//for(int k = 0; k < priority.MaterialGroups.Count; k++)
						foreach(DictionaryEntry materialGroup in priority.MaterialGroups)
						{

							// get material info for the current iteration
							currentMaterial = (Material)materialGroup.Key;

							//currentMaterial = (Material)priority.MaterialGroups.GetKeyAt(k);
							materialLayersLeft = currentMaterial.NumTextureLayers;

							// do at least one rendering pass, even if no texture layers.  Not all materials have textures.
							do
							{
								// returns non-zero if multipass required, so loop will continue
								materialLayersLeft = SetMaterial(currentMaterial, materialLayersLeft);

								// get list of renderables from the material group
								ArrayList renderableList = (ArrayList)materialGroup.Value;

								// iterate through renderables and render
								// may happen multiple times for multipass
								for(int l = 0; l < renderableList.Count; l++)
								{
									IRenderable renderable = (IRenderable)renderableList[l];

									// get world transforms
									xform = renderable.WorldTransforms;
									numMatrices = renderable.NumWorldTransforms;

									// set the world matrices in the render system
									if(numMatrices > 1)
										targetRenderSystem.SetWorldMatrices(xform, numMatrices);
									else
										targetRenderSystem.WorldMatrix = xform[0];

									// use the renderable view/projection (if any)
									UseRenderableViewProjection(renderable);

									// override solid/wireframe rendering
									SceneDetailLevel requestedDetail = renderable.RenderDetail;

									if(requestedDetail != lastDetailLevel)
									{
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

								} // foreach renderableList

							} while(materialLayersLeft > 0);

						} // foreach MaterialGroups

						// *** Transparent Entity Loop ***
						// sort the transparent objects
						priority.SortTransparentObjects(camInProgress);

						// loop through transparent object groups
						for(int t = 0; t < priority.TransparentObjects.Count; t++)
						{
							IRenderable transObject = (IRenderable)priority.TransparentObjects[t];

							// get current iteration info
							currentMaterial = transObject.Material;
							materialLayersLeft = currentMaterial.NumTextureLayers;

							// do at least one pass (no layers in untextured objects)
							do
							{
								// returns non-zero if multipass is required
								materialLayersLeft = SetMaterial(currentMaterial, materialLayersLeft);

								// set world transforms
								// get world transforms
								xform = transObject.WorldTransforms;
								numMatrices = transObject.NumWorldTransforms;

								// set the world matrices in the render system
								if(numMatrices > 1)
								{
									targetRenderSystem.SetWorldMatrices(xform, numMatrices);
								}
								else
								{
									targetRenderSystem.WorldMatrix = xform[0];
								}

								// use the renderable view/projection (if any)
								UseRenderableViewProjection(transObject);

								// override solid/wireframe rendering
								SceneDetailLevel requestedDetail = transObject.RenderDetail;

								if(requestedDetail != lastDetailLevel)
								{
									// dont go from wireframe to solid, only downgrade
									if(requestedDetail > camDetailLevel)
										requestedDetail = camDetailLevel;
										
									// update the render systems rasterization mode
									targetRenderSystem.RasterizationMode = requestedDetail;

									lastDetailLevel = requestedDetail;
								}

								// get the renderables vertex buffer
								transObject.GetRenderOperation(op);

								// render the object as long as it has vertices
								if(op.vertexData.vertexCount > 0)
									targetRenderSystem.Render(op);

							} while(materialLayersLeft > 0);

						} // foreach TransparencyGroups

					} // foreach PriorityGroups

					// true if we need to repeat this queue, false otherwise
					repeatQueue = OnRenderQueueEnded(groupID);

				} while (repeatQueue);
			}
		}

		/// <summary>
		///		Internal method for queueing the sky objects with the params as 
		///		previously set through SetSkyBox, SetSkyPlane and SetSkyDome.
		/// </summary>
		/// <param name="camera"></param>
		internal virtual void QueueSkiesForRendering(Camera camera)
		{
			// translate the skybox by cam position
			if(skyPlaneNode != null)
				skyPlaneNode.Position = camera.DerivedPosition;

			if(skyBoxNode != null)
				skyBoxNode.Position = camera.DerivedPosition;

			if(skyDomeNode != null)
				skyDomeNode.Position = camera.DerivedPosition;

			RenderQueueGroupID qid;

			if(isSkyPlaneEnabled)
			{
				qid = isSkyPlaneDrawnFirst ? RenderQueueGroupID.One : RenderQueueGroupID.Nine;
				renderQueue.AddRenderable(skyPlaneEntity.SubEntities[0], 1, qid);
			}

			if(isSkyBoxEnabled)
			{
				qid = isSkyBoxDrawnFirst ? RenderQueueGroupID.One : RenderQueueGroupID.Nine;

				for(int plane = 0; plane < 6; plane++)
					renderQueue.AddRenderable(skyBoxEntities[plane].SubEntities[0], 1, qid);
			}

			if(isSkyDomeEnabled)
			{
				qid = isSkyDomeDrawnFirst ? RenderQueueGroupID.One : RenderQueueGroupID.Nine;

				for(int plane = 0; plane < 5; plane++)
					renderQueue.AddRenderable(skyDomeEntities[plane].SubEntities[0], 1, qid);
			}
		}

		/// <summary>
		///		Internal method for issuing geometry for a subMesh to the RenderSystem pipeline.
		/// </summary>
		/// <remarks>
		///		Not recommended for manual usage, leave the engine to use this one as appropriate!
		///		<p/>
		///		It's assumed that material and world / view / projection transforms have already been set.
		/// </remarks>
		/// <param name="subMesh"></param>
		internal virtual void RenderSubMesh(SubMesh subMesh)
		{
			// TODO: Implement RenderSubMesh
		}

		/// <summary>
		///		Sends any updates to the dynamic lights in the world to the renderer.
		/// </summary>
		internal virtual void UpdateDynamicLights()
		{
			for(int i = 0; i < lightList.Count; i++)
			{
				Light light = lightList[i];

				if(light.IsModified)
					targetRenderSystem.UpdateLight(light);
			}
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
		public virtual void SetSkyPlane(bool enable, Plane plane, String materialName, float scale, float tiling, bool drawFirst, float bow)
		{
			isSkyPlaneEnabled = enable;

			if(enable)
			{
				string meshName = "SkyPlane";
				skyPlane = plane;

				Material m = (Material)MaterialManager.Instance[materialName];

				if(m == null)
					throw new Exception(string.Format("Skyplane material '{0}' not found.", materialName));

				// make sure the material doesn't update the depth buffer
				m.DepthWrite = false;
				m.Load();

				isSkyPlaneDrawnFirst = drawFirst;

				// set up the place
				Mesh planeMesh = (Mesh)MeshManager.Instance[meshName];

				// unload the old one if it exists
				if(planeMesh != null)
					MeshManager.Instance.Unload(planeMesh);

				// create up vector
				Vector3 up = plane.Normal.Cross(Vector3.UnitX);
				if(up == Vector3.Zero)
					up = plane.Normal.Cross(-Vector3.UnitZ);

				if(bow > 0)
				{
					// TODO: Add MeshManager.CreateCurvedPlane
				}
				else
				{
					planeMesh = MeshManager.Instance.CreatePlane(meshName, plane, scale * 100, scale * 100, 1, 1, false, 1, tiling, tiling, up);
				}

				if(skyPlaneEntity != null)
				{
					entityList.Remove(skyPlaneEntity);
				}

				// create entity for the plane, using the mesh name
				skyPlaneEntity = CreateEntity(meshName, meshName);
				skyPlaneEntity.MaterialName = materialName;

				if(skyPlaneNode == null)
					skyPlaneNode = CreateSceneNode(meshName + "Node");
				else
					skyPlaneNode.Objects.Clear();

				// attach the skyplane to the new node
				skyPlaneNode.Objects.Add(skyPlaneEntity);
			}
		}

		/// <summary>
		///		Overload.
		/// </summary>
		/// <param name="enable"></param>
		/// <param name="plane"></param>
		public virtual void SetSkyPlane(bool enable, Plane plane, String materialName)
		{
			// call the overloaded method
			SetSkyPlane(enable, plane, materialName, 1000.0f, 10.0f, true, 0);
		}

		#endregion

		#region Light collection event handlers

		virtual protected bool lightList_Cleared(object source, EventArgs e)
		{
			return false;
		}

		virtual protected bool lightList_ItemAdded(object source, EventArgs e)
		{
			Light light = source as Light;

			// add the light to the target render system
			targetRenderSystem.AddLight(light);

			return false;
		}

		virtual protected bool lightList_ItemRemoved(object source, EventArgs e)
		{
			return false;
		}

		#endregion
	}
}
