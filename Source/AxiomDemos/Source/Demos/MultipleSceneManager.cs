using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;

using Axiom.Animating;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Input;
using Axiom.Math;

namespace Axiom.Demos
{
	[Export( typeof ( TechDemo ) )]
	public class MultipleSceneManager : TechDemo
	{
		private const string CAMERA_NAME = "Camera";

		private SceneManager[] _sceneManagers = new SceneManager[ 3 ];
		private Camera[] _cameras = new Camera[ 3 ];

		private string bspPath;
		private string bspMap;
		private LoadingBar loadingBar = new LoadingBar();

		private AnimationState animationState = null;

		public override void ChooseSceneManager()
		{
			_sceneManagers[ 0 ] = Root.Instance.CreateSceneManager( SceneType.Generic, "primary" );
			_sceneManagers[ 1 ] = Root.Instance.CreateSceneManager( "TerrainSceneManager", "secondary" );
			_sceneManagers[ 2 ] = Root.Instance.CreateSceneManager( "BspSceneManager", "tertiary" );
			scene = _sceneManagers[ 0 ];
		}

		public override void SetupResources()
		{
			bspPath = "Media/Archives/chiropteraDM.zip";
			bspMap = "maps/chiropteradm.bsp";

			ResourceGroupManager.Instance.AddResourceLocation( bspPath, "ZipFile", ResourceGroupManager.Instance.WorldResourceGroupName, true, false );
		}

		protected override void LoadResources()
		{
			loadingBar.Start( Window, 1, 1, 0.75 );

			// Turn off rendering of everything except overlays
			_sceneManagers[ 2 ].SpecialCaseRenderQueueList.ClearRenderQueues();
			_sceneManagers[ 2 ].SpecialCaseRenderQueueList.AddRenderQueue( RenderQueueGroupID.Overlay );
			_sceneManagers[ 2 ].SpecialCaseRenderQueueList.RenderQueueMode = SpecialCaseRenderQueueMode.Include;

			// Set up the world geometry link
			Core.ResourceGroupManager.Instance.LinkWorldGeometryToResourceGroup( Core.ResourceGroupManager.Instance.WorldResourceGroupName, bspMap, _sceneManagers[ 2 ] );

			// Initialise the rest of the resource groups, parse scripts etc
			Core.ResourceGroupManager.Instance.InitializeAllResourceGroups();
			Core.ResourceGroupManager.Instance.LoadResourceGroup( Core.ResourceGroupManager.Instance.WorldResourceGroupName, false, true );

			// Back to full rendering
			_sceneManagers[ 2 ].SpecialCaseRenderQueueList.ClearRenderQueues();
			_sceneManagers[ 2 ].SpecialCaseRenderQueueList.RenderQueueMode = SpecialCaseRenderQueueMode.Exclude;

			loadingBar.Finish();
		}

		public override void CreateScene()
		{
			#region Primary Scene

			// set some ambient light
			scene.AmbientLight = new ColorEx( 1.0f, 0.2f, 0.2f, 0.2f );

			// create a skydome
			scene.SetSkyDome( true, "Examples/CloudySky", 5, 8 );

			// create a simple default point light
			Light light = scene.CreateLight( "MainLight" );
			light.Position = new Vector3( 20, 80, 50 );

			// create a plane for the plane mesh
			Plane plane = new Plane();
			plane.Normal = Vector3.UnitY;
			plane.D = 200;

			// create a plane mesh
			MeshManager.Instance.CreatePlane( "FloorPlane", ResourceGroupManager.DefaultResourceGroupName, plane, 200000, 200000, 20, 20, true, 1, 50, 50, Vector3.UnitZ );

			// create an entity to reference this mesh
			Entity planeEntity = scene.CreateEntity( "Floor", "FloorPlane" );
			planeEntity.MaterialName = "Examples/RustySteel";
			scene.RootSceneNode.CreateChildSceneNode().AttachObject( planeEntity );

			// create an entity to have follow the path
			Entity ogreHead = scene.CreateEntity( "OgreHead", "ogrehead.mesh" );

			// create a scene node for the entity and attach the entity
			SceneNode headNode = scene.RootSceneNode.CreateChildSceneNode( "OgreHeadNode", Vector3.Zero, Quaternion.Identity );
			headNode.AttachObject( ogreHead );

			// make sure the camera tracks this node
			camera.SetAutoTracking( true, headNode, Vector3.Zero );

			// create a scene node to attach the camera to
			SceneNode cameraNode = scene.RootSceneNode.CreateChildSceneNode( "CameraNode" );
			cameraNode.AttachObject( camera );

			// create new animation
			Animation animation = scene.CreateAnimation( "OgreHeadAnimation", 10.0f );

			// nice smooth animation
			animation.InterpolationMode = InterpolationMode.Spline;

			// create the main animation track
			AnimationTrack track = animation.CreateNodeTrack( 0, cameraNode );

			// create a few keyframes to move the camera around
			TransformKeyFrame frame = (TransformKeyFrame)track.CreateKeyFrame( 0.0f );

			frame = (TransformKeyFrame)track.CreateKeyFrame( 2.5f );
			frame.Translate = new Vector3( 500, 500, -1000 );

			frame = (TransformKeyFrame)track.CreateKeyFrame( 5.0f );
			frame.Translate = new Vector3( -1500, 1000, -600 );

			frame = (TransformKeyFrame)track.CreateKeyFrame( 7.5f );
			frame.Translate = new Vector3( 0, -100, 0 );

			frame = (TransformKeyFrame)track.CreateKeyFrame( 10.0f );
			frame.Translate = Vector3.Zero;

			// create a new animation state to control the animation
			animationState = scene.CreateAnimationState( "OgreHeadAnimation" );

			// enable the animation
			animationState.IsEnabled = true;

			// turn on some fog
			scene.SetFog( FogMode.Exp, ColorEx.White, 0.0002f );

			#endregion Primary Scene

			#region Secondary Scene

			_sceneManagers[ 1 ].SetWorldGeometry( "terrain.xml" );
			// Infinite far plane?
			if ( Root.Instance.RenderSystem.Capabilities.HasCapability( Capabilities.InfiniteFarPlane ) )
			{
				_cameras[ 1 ].Far = 0;
			}
			// Set a nice viewpoint
			_cameras[ 1 ].Position = new Vector3( 707, 3500, 528 );
			_cameras[ 1 ].LookAt( Vector3.Zero );
			_cameras[ 1 ].Near = 1;
			_cameras[ 1 ].Far = 1000;

			#endregion Secondary Scene

			#region Tertiary Scene

			// modify camera for close work
			_cameras[ 2 ].Near = 4;
			_cameras[ 2 ].Far = 4000;

			// Also change position, and set Quake-type orientation
			// Get random player start point
			ViewPoint vp = _sceneManagers[ 2 ].GetSuggestedViewpoint( true );
			_cameras[ 2 ].Position = vp.position;
			_cameras[ 2 ].Pitch( 90 ); // Quake uses X/Y horizon, Z up
			_cameras[ 2 ].Rotate( vp.orientation );
			// Don't yaw along variable axis, causes leaning
			_cameras[ 2 ].FixedYawAxis = Vector3.UnitZ;

			#endregion Tertiary Scene
		}

		protected override void OnFrameStarted( object source, FrameEventArgs evt )
		{
			// add time to the animation which is driven off of rendering time per frame
			animationState.AddTime( evt.TimeSinceLastFrame );

			base.OnFrameStarted( source, evt );
		}

		public override void CreateCamera()
		{
			_cameras[ 0 ] = _sceneManagers[ 0 ].CreateCamera( "Primary" + CAMERA_NAME );
			camera = _cameras[ 0 ];
			_cameras[ 1 ] = _sceneManagers[ 1 ].CreateCamera( "Secondary" + CAMERA_NAME );
			_cameras[ 2 ] = _sceneManagers[ 2 ].CreateCamera( "Tertiary" + CAMERA_NAME );
		}

		public override void CreateViewports()
		{
			#region Primary ViewPort

			Viewport viewport = window.AddViewport( camera, 0, 0, 1.0f, 1.0f, 0 );
			viewport.BackgroundColor = ColorEx.Black;

			#endregion Primary ViewPort

			#region Secondary ViewPort

			Viewport vp = Window.AddViewport( _cameras[ 1 ], 0.5f, 0, 0.5f, 0.5f, 1 );
			vp.ShowOverlays = false;

			// Fog
			// NB it's VERY important to set this before calling setWorldGeometry
			// because the vertex program picked will be different
			ColorEx fadeColour = new ColorEx( 0.93f, 0.86f, 0.76f );
			_sceneManagers[ 1 ].SetFog( FogMode.Linear, fadeColour, .001f, 500, 1000 );
			vp.BackgroundColor = fadeColour;

			#endregion Secondary ViewPort

			#region Tertiary ViewPort

			vp = Window.AddViewport( _cameras[ 2 ], 0.5f, 0.5f, 0.5f, 0.5f, 2 );
			vp.ShowOverlays = false;

			#endregion Tertiary ViewPort
		}
	}
}

/*




*/
