using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using Axiom.Core;
using Axiom.Graphics;
using Axiom.Math;
using Axiom.Input;
using Axiom.Overlays;
using System.IO;
using Axiom.Configuration;

namespace Axiom.Samples.MulitpleRenderWindows
{
    public partial class Form1 : Form
    {
        protected Root engine;
        protected Camera camera;
        protected Viewport viewport;
        protected Camera cameraTwo;
        protected Viewport viewport2;
        protected SceneManager scene;
        protected RenderWindow windowOne;
        protected RenderWindow windowTwo;
        protected InputReader input;
        protected Vector3 cameraVector = Vector3.Zero;
        protected float cameraScale;
        protected bool showDebugOverlay = true;
        protected float statDelay = 0.0f;
        protected float debugTextDelay = 0.0f;
        protected float toggleDelay = 0.0f;
        protected Vector3 camVelocity = Vector3.Zero;
        protected Vector3 camAccel = Vector3.Zero;
        protected float camSpeed = 2.5f;
        protected int aniso = 1;
        protected TextureFiltering filtering = TextureFiltering.Bilinear;
        private SceneNode headNode = null;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load( object sender, EventArgs e )
        {
            this.Show();
            
            engine = new Root( "EngineConfig.xml", "AxiomEngine.log" );
            _setupResources();
            engine.RenderSystem = engine.RenderSystems[ 0 ];

            engine.FrameStarted += new FrameEvent( OnFrameStarted );

            engine.Initialize( false );

            Control target = this.viewportOne;
            windowOne = engine.CreateRenderWindow( "SampleOne", target.Width, target.Height, 32, false, 0, 0, true, true, target );

            target = this.viewportTwo;
            windowTwo = engine.CreateRenderWindow( "SampleTwo", target.Width, target.Height, 32, false, 0, 0, true, true, target );

            ShowDebugOverlay( showDebugOverlay );

            // Get the SceneManager, a generic one by default
            scene = engine.SceneManagers.GetSceneManager( SceneType.Generic );
            scene.ClearScene();

            // create a camera and initialize its position
            camera = scene.CreateCamera( "MainCamera" );
            camera.Position = new Vector3( 0, 0, 500 );
            camera.LookAt( new Vector3( 0, 0, -300 ) );

            // set the near clipping plane to be very close
            camera.Near = 5;

            // create a new viewport and set it's background color
            viewport = windowOne.AddViewport( camera, 0, 0, 1.0f, 1.0f, 100 );
            viewport.BackgroundColor = ColorEx.Blue;

            // create a camera and initialize its position
            cameraTwo = scene.CreateCamera( "CameraTwo" );
            cameraTwo.Position = new Vector3( 500, 0, 250 );
            cameraTwo.LookAt( new Vector3( 0, 0, -300 ) );

            // set the near clipping plane to be very close
            cameraTwo.Near = 5;

            // create a new viewport and set it's background color
            viewport2 = windowTwo.AddViewport( cameraTwo, 0, 0, 1.0f, 1.0f, 99);
            viewport2.BackgroundColor = ColorEx.Blue;

            // set default mipmap level
            TextureManager.Instance.DefaultNumMipMaps = 5;

            // call the overridden CreateScene method
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
            MeshManager.Instance.CreatePlane( "FloorPlane", plane, 200000, 200000, 20, 20, true, 1, 50, 50, Vector3.UnitZ );

            // create an entity to reference this mesh
            Entity planeEntity = scene.CreateEntity( "Floor", "FloorPlane" );
            planeEntity.MaterialName = "Examples/RustySteel";
            scene.RootSceneNode.CreateChildSceneNode().AttachObject( planeEntity );

            // create an entity to have follow the path
            Entity ogreHead = scene.CreateEntity( "OgreHead", "ogrehead.mesh" );

            // create a scene node for the entity and attach the entity
            headNode = scene.RootSceneNode.CreateChildSceneNode( "OgreHeadNode", Vector3.Zero, Quaternion.Identity );
            headNode.AttachObject( ogreHead );

            // retreive and initialize the input system
            input = PlatformManager.Instance.CreateInputReader();
            input.Initialize( windowOne, true, true, false, false );

            engine.StartRendering();

        }

        /// <summary>
        ///    Shows the debug overlay, which displays performance statistics.
        /// </summary>
        protected void ShowDebugOverlay( bool show )
        {
            // gets a reference to the default overlay
            Overlay o = OverlayManager.Instance.GetByName( "Core/DebugOverlay" );

            if ( o == null )
            {
                throw new Exception( string.Format( "Could not find overlay named '{0}'.", "Core/DebugOverlay" ) );
            }

            if ( show )
            {
                o.Show();
            }
            else
            {
                o.Hide();
            }
        }
        /// <summary>
        ///		Loads default resource configuration if one exists.
        /// </summary>
        private void _setupResources()
        {
            string resourceConfigPath = Path.GetFullPath( "EngineConfig.xml" );

            if ( File.Exists( resourceConfigPath ) )
            {
                EngineConfig config = new EngineConfig();

                // load the config file
                // relative from the location of debug and releases executables
                config.ReadXml( "EngineConfig.xml" );

                // interrogate the available resource paths
                foreach ( EngineConfig.FilePathRow row in config.FilePath )
                {
                    ResourceManager.AddCommonArchive( row.src, row.type );
                }
            }
        }

        private void Form1_FormClosing( object sender, FormClosingEventArgs e )
        {
            engine.Shutdown();
        }
        protected void UpdateStats()
        {
            // TODO: Replace with CEGUI
            OverlayElement element = OverlayElementManager.Instance.GetElement( "Core/CurrFps" );
            element.Text = string.Format( "Current FPS: {0}", Root.Instance.CurrentFPS );

            element = OverlayElementManager.Instance.GetElement( "Core/BestFps" );
            element.Text = string.Format( "Best FPS: {0}", Root.Instance.BestFPS );

            element = OverlayElementManager.Instance.GetElement( "Core/WorstFps" );
            element.Text = string.Format( "Worst FPS: {0}", Root.Instance.WorstFPS );

            element = OverlayElementManager.Instance.GetElement( "Core/AverageFps" );
            element.Text = string.Format( "Average FPS: {0}", Root.Instance.AverageFPS );

            element = OverlayElementManager.Instance.GetElement( "Core/NumTris" );
            element.Text = string.Format( "Triangle Count: {0}", scene.TargetRenderSystem.FacesRendered );
        }
        protected virtual void OnFrameStarted( Object source, FrameEventArgs e )
        {
            float scaleMove = 200 * e.TimeSinceLastFrame;

            // reset acceleration zero
            camAccel = Vector3.Zero;

            // set the scaling of camera motion
            cameraScale = 100 * e.TimeSinceLastFrame;

            // TODO: Move this into an event queueing mechanism that is processed every frame
            input.Capture();

            if ( input.IsKeyPressed( KeyCodes.Escape ) )
            {
                Root.Instance.QueueEndRendering();

                return;
            }

            if ( input.IsKeyPressed( KeyCodes.A ) )
            {
                camAccel.x = -0.5f;
            }

            if ( input.IsKeyPressed( KeyCodes.D ) )
            {
                camAccel.x = 0.5f;
            }

            if ( input.IsKeyPressed( KeyCodes.W ) )
            {
                camAccel.z = -1.0f;
            }

            if ( input.IsKeyPressed( KeyCodes.S ) )
            {
                camAccel.z = 1.0f;
            }

            camAccel.y += (float)( input.RelativeMouseZ * 0.1f );

            if ( input.IsKeyPressed( KeyCodes.Left ) )
            {
                camera.Yaw( cameraScale );
            }

            if ( input.IsKeyPressed( KeyCodes.Right ) )
            {
                camera.Yaw( -cameraScale );
            }

            if ( input.IsKeyPressed( KeyCodes.Up ) )
            {
                camera.Pitch( cameraScale );
            }

            if ( input.IsKeyPressed( KeyCodes.Down ) )
            {
                camera.Pitch( -cameraScale );
            }

            // subtract the time since last frame to delay specific key presses
            toggleDelay -= e.TimeSinceLastFrame;

            // toggle rendering mode
            if ( input.IsKeyPressed( KeyCodes.R ) && toggleDelay < 0 )
            {
                if ( camera.SceneDetail == SceneDetailLevel.Points )
                {
                    camera.SceneDetail = SceneDetailLevel.Solid;
                }
                else if ( camera.SceneDetail == SceneDetailLevel.Solid )
                {
                    camera.SceneDetail = SceneDetailLevel.Wireframe;
                }
                else
                {
                    camera.SceneDetail = SceneDetailLevel.Points;
                }

                Console.WriteLine( "Rendering mode changed to '{0}'.", camera.SceneDetail );

                toggleDelay = 1;
            }

            if ( input.IsKeyPressed( KeyCodes.T ) && toggleDelay < 0 )
            {
                // toggle the texture settings
                switch ( filtering )
                {
                    case TextureFiltering.Bilinear:
                        filtering = TextureFiltering.Trilinear;
                        aniso = 1;
                        break;
                    case TextureFiltering.Trilinear:
                        filtering = TextureFiltering.Anisotropic;
                        aniso = 8;
                        break;
                    case TextureFiltering.Anisotropic:
                        filtering = TextureFiltering.Bilinear;
                        aniso = 1;
                        break;
                }

                Console.WriteLine( "Texture Filtering changed to '{0}'.", filtering );

                // set the new default
                MaterialManager.Instance.SetDefaultTextureFiltering( filtering );
                MaterialManager.Instance.DefaultAnisotropy = aniso;

                toggleDelay = 1;
            }

            if ( input.IsKeyPressed( KeyCodes.B ) )
            {
                scene.ShowBoundingBoxes = !scene.ShowBoundingBoxes;
            }

            if ( input.IsKeyPressed( KeyCodes.F ) )
            {
                // hide all overlays, includes ones besides the debug overlay
                viewport.OverlaysEnabled = !viewport.OverlaysEnabled;
            }

            if ( !input.IsMousePressed( Axiom.Input.MouseButtons.Left ) )
            {
                float cameraYaw = -input.RelativeMouseX * .13f;
                float cameraPitch = -input.RelativeMouseY * .13f;

                camera.Yaw( cameraYaw );
                camera.Pitch( cameraPitch );
            }
            else
            {
                cameraVector.x += input.RelativeMouseX * 0.13f;
            }

            camVelocity += ( camAccel * scaleMove * camSpeed );

            // move the camera based on the accumulated movement vector
            camera.MoveRelative( camVelocity * e.TimeSinceLastFrame );

            // Now dampen the Velocity - only if user is not accelerating
            if ( camAccel == Vector3.Zero )
            {
                camVelocity *= ( 1 - ( 6 * e.TimeSinceLastFrame ) );
            }

            // update performance stats once per second
            if ( statDelay < 0.0f && showDebugOverlay )
            {
                UpdateStats();
                statDelay = 1.0f;
            }
            else
            {
                statDelay -= e.TimeSinceLastFrame;
            }

            // turn off debug text when delay ends
            if ( debugTextDelay < 0.0f )
            {
                debugTextDelay = 0.0f;
                windowOne.DebugText = "";
            }
            else if ( debugTextDelay > 0.0f )
            {
                debugTextDelay -= e.TimeSinceLastFrame;
            }

            OverlayElement element = OverlayElementManager.Instance.GetElement( "Core/DebugText" );
            element.Text = windowOne.DebugText;
        }

    }
}