using System.ComponentModel.Composition;

using Axiom.Animating;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Math;

namespace Axiom.Demos
{
	[Export( typeof( TechDemo ) )]
	public class MultipleSceneManager : TechDemo
	{
		private const string CAMERA_NAME = "Camera";

		private readonly Camera[] _cameras = new Camera[ 3 ];
		private readonly SceneManager[] _sceneManagers = new SceneManager[ 3 ];

		private readonly LoadingBar loadingBar = new LoadingBar();

		private AnimationState animationState;
		private string bspMap;
		private string bspPath;

		public override void ChooseSceneManager()
		{
			this._sceneManagers[ 0 ] = Root.Instance.CreateSceneManager( SceneType.Generic, "primary" );
			this._sceneManagers[ 1 ] = Root.Instance.CreateSceneManager( "TerrainSceneManager", "secondary" );
			this._sceneManagers[ 2 ] = Root.Instance.CreateSceneManager( "BspSceneManager", "tertiary" );
			scene = this._sceneManagers[ 0 ];
		}

		public override void SetupResources()
		{
			this.bspPath = "Media/Archives/chiropteraDM.zip";
			this.bspMap = "maps/chiropteradm.bsp";

			ResourceGroupManager.Instance.AddResourceLocation( this.bspPath, "ZipFile", ResourceGroupManager.Instance.WorldResourceGroupName, true, false );
		}

		protected override void LoadResources()
		{
			this.loadingBar.Start( Window, 1, 1, 0.75 );

			// Turn off rendering of everything except overlays
			this._sceneManagers[ 2 ].SpecialCaseRenderQueueList.ClearRenderQueues();
			this._sceneManagers[ 2 ].SpecialCaseRenderQueueList.AddRenderQueue( RenderQueueGroupID.Overlay );
			this._sceneManagers[ 2 ].SpecialCaseRenderQueueList.RenderQueueMode = SpecialCaseRenderQueueMode.Include;

			// Set up the world geometry link
			ResourceGroupManager.Instance.LinkWorldGeometryToResourceGroup( ResourceGroupManager.Instance.WorldResourceGroupName, this.bspMap, this._sceneManagers[ 2 ] );

			// Initialise the rest of the resource groups, parse scripts etc
			ResourceGroupManager.Instance.InitializeAllResourceGroups();
			ResourceGroupManager.Instance.LoadResourceGroup( ResourceGroupManager.Instance.WorldResourceGroupName, false, true );

			// Back to full rendering
			this._sceneManagers[ 2 ].SpecialCaseRenderQueueList.ClearRenderQueues();
			this._sceneManagers[ 2 ].SpecialCaseRenderQueueList.RenderQueueMode = SpecialCaseRenderQueueMode.Exclude;

			this.loadingBar.Finish();
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
			var plane = new Plane();
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
			var frame = (TransformKeyFrame)track.CreateKeyFrame( 0.0f );

			frame = (TransformKeyFrame)track.CreateKeyFrame( 2.5f );
			frame.Translate = new Vector3( 500, 500, -1000 );

			frame = (TransformKeyFrame)track.CreateKeyFrame( 5.0f );
			frame.Translate = new Vector3( -1500, 1000, -600 );

			frame = (TransformKeyFrame)track.CreateKeyFrame( 7.5f );
			frame.Translate = new Vector3( 0, -100, 0 );

			frame = (TransformKeyFrame)track.CreateKeyFrame( 10.0f );
			frame.Translate = Vector3.Zero;

			// create a new animation state to control the animation
			this.animationState = scene.CreateAnimationState( "OgreHeadAnimation" );

			// enable the animation
			this.animationState.IsEnabled = true;

			// turn on some fog
			scene.SetFog( FogMode.Exp, ColorEx.White, 0.0002f );

			#endregion Primary Scene

			#region Secondary Scene

			this._sceneManagers[ 1 ].SetWorldGeometry( "terrain.xml" );
			// Infinite far plane?
			if ( Root.Instance.RenderSystem.Capabilities.HasCapability( Capabilities.InfiniteFarPlane ) )
			{
				this._cameras[ 1 ].Far = 0;
			}
			// Set a nice viewpoint
			this._cameras[ 1 ].Position = new Vector3( 707, 3500, 528 );
			this._cameras[ 1 ].LookAt( Vector3.Zero );
			this._cameras[ 1 ].Near = 1;
			this._cameras[ 1 ].Far = 1000;

			#endregion Secondary Scene

			#region Tertiary Scene

			// modify camera for close work
			this._cameras[ 2 ].Near = 4;
			this._cameras[ 2 ].Far = 4000;

			// Also change position, and set Quake-type orientation
			// Get random player start point
			ViewPoint vp = this._sceneManagers[ 2 ].GetSuggestedViewpoint( true );
			this._cameras[ 2 ].Position = vp.position;
			this._cameras[ 2 ].Pitch( 90 ); // Quake uses X/Y horizon, Z up
			this._cameras[ 2 ].Rotate( vp.orientation );
			// Don't yaw along variable axis, causes leaning
			this._cameras[ 2 ].FixedYawAxis = Vector3.UnitZ;

			#endregion Tertiary Scene
		}

		protected override void OnFrameStarted( object source, FrameEventArgs evt )
		{
			// add time to the animation which is driven off of rendering time per frame
			this.animationState.AddTime( evt.TimeSinceLastFrame );

			base.OnFrameStarted( source, evt );
		}

		public override void CreateCamera()
		{
			this._cameras[ 0 ] = this._sceneManagers[ 0 ].CreateCamera( "Primary" + CAMERA_NAME );
			camera = this._cameras[ 0 ];
			this._cameras[ 1 ] = this._sceneManagers[ 1 ].CreateCamera( "Secondary" + CAMERA_NAME );
			this._cameras[ 2 ] = this._sceneManagers[ 2 ].CreateCamera( "Tertiary" + CAMERA_NAME );
		}

		public override void CreateViewports()
		{
			#region Primary ViewPort

			Viewport viewport = window.AddViewport( camera, 0, 0, 1.0f, 1.0f, 0 );
			viewport.BackgroundColor = ColorEx.Black;

			#endregion Primary ViewPort

			#region Secondary ViewPort

			Viewport vp = Window.AddViewport( this._cameras[ 1 ], 0.5f, 0, 0.5f, 0.5f, 1 );
			vp.ShowOverlays = false;

			// Fog
			// NB it's VERY important to set this before calling setWorldGeometry
			// because the vertex program picked will be different
			var fadeColour = new ColorEx( 0.93f, 0.86f, 0.76f );
			this._sceneManagers[ 1 ].SetFog( FogMode.Linear, fadeColour, .001f, 500, 1000 );
			vp.BackgroundColor = fadeColour;

			#endregion Secondary ViewPort

			#region Tertiary ViewPort

			vp = Window.AddViewport( this._cameras[ 2 ], 0.5f, 0.5f, 0.5f, 0.5f, 2 );
			vp.ShowOverlays = false;

			#endregion Tertiary ViewPort
		}
	}
}

/*




*/
