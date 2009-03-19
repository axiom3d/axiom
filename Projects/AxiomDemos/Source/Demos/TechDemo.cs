#region Namespace Declarations

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

using Axiom.Configuration;
using Axiom.Core;
using Axiom.Input;
using Axiom.Overlays;
using Axiom.Math;
using Axiom.Graphics;
using MouseButtons = Axiom.Input.MouseButtons;

#endregion Namespace Declarations

namespace Axiom.Demos
{
    /// <summary>
    ///     Base class for Axiom examples.
    /// </summary>
    public abstract class TechDemo : IDisposable
    {
        #region Protected Fields

        protected Root engine;
        public Root Engine
        {
            get
            {
                return engine;
            }
            set
            {
                engine = value;
            }
        }
        protected Camera camera;
        protected Viewport viewport;
        protected SceneManager scene;
        protected RenderWindow window;
        public RenderWindow Window
        {
            get
            {
                return window;
            }
            set
            {
                window = value;
            }
        }
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

        #endregion Protected Fields

        #region Protected Methods

        //protected bool Configure()
        //{
        //    // HACK: Temporary
        //    //RenderSystem renderSystem = Root.Instance.RenderSystems[ 0 ];
        //    //Root.Instance.RenderSystem = renderSystem;
        //    //EngineConfig.DisplayModeRow mode = renderSystem.ConfigOptions.DisplayMode[ 0 ];
        //    //mode.FullScreen = true;
        //    //mode.Selected = true;

        //    window = Root.Instance.Initialize( true, "Axiom Engine Window" );

        //    ShowDebugOverlay( showDebugOverlay );

        //    return true;
        //}

        public virtual void CreateCamera()
        {
            // create a camera and initialize its position
            camera = scene.CreateCamera( "MainCamera" );
            camera.Position = new Vector3( 0, 0, 500 );
            camera.LookAt( new Vector3( 0, 0, -300 ) );

            // set the near clipping plane to be very close
            camera.Near = 5;
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

        protected void TakeScreenshot( string fileName )
        {
            window.Save( fileName );
        }

        #endregion Protected Methods

        #region Protected Virtual Methods

        public virtual void ChooseSceneManager()
        {
            // Get the SceneManager, a generic one by default
            scene = engine.SceneManagers.GetSceneManager( SceneType.Generic );
            scene.ClearScene();
        }

        public virtual void CreateViewports()
        {
            Debug.Assert( window != null, "Attempting to use a null RenderWindow." );

            // create a new viewport and set it's background color
            viewport = window.AddViewport( camera, 0, 0, 1.0f, 1.0f, 100 );
            viewport.BackgroundColor = ColorEx.Black;
        }

        protected virtual bool Setup()
        {
            // instantiate the Root singleton
            //engine = new Root( "EngineConfig.xml", "AxiomEngine.log" );
            engine = Root.Instance;

            // add event handlers for frame events
            engine.FrameStarted += new FrameEvent( OnFrameStarted );
            engine.FrameEnded += new FrameEvent( OnFrameEnded );

            // allow for setting up resource gathering
            //SetupResources();

            //show the config dialog and collect options
            //if ( !Configure() )
            //{
            //    // shutting right back down
            //    engine.Shutdown();

            //    return false;
            //}
            window = Root.Instance.Initialize( true, "Axiom Engine Demo Window" );

            ShowDebugOverlay( showDebugOverlay );

            ChooseSceneManager();
            CreateCamera();
            CreateViewports();

            // set default mipmap level
            TextureManager.Instance.DefaultNumMipMaps = 5;

            // call the overridden CreateScene method
            CreateScene();
#if !( XBOX || XBOX360 )
            // retrieve and initialize the input system
            input = PlatformManager.Instance.CreateInputReader();
            input.Initialize( window, true, true, false, false );
#endif
            return true;
        }

        /// <summary>
        ///		Loads default resource configuration if one exists.
        /// </summary>
        //protected virtual void SetupResources()
        //{
        //    string resourceConfigPath = Path.GetFullPath( "EngineConfig.xml" );

        //    if ( File.Exists( resourceConfigPath ) )
        //    {
        //        EngineConfig config = new EngineConfig();

        //        // load the config file
        //        // relative from the location of debug and releases executables
        //        config.ReadXml( "EngineConfig.xml" );

        //        // interrogate the available resource paths
        //        foreach ( EngineConfig.FilePathRow row in config.FilePath )
        //        {
        //            ResourceManager.AddCommonArchive( row.src, row.type );
        //        }
        //    }
        //}

        #endregion Protected Virtual Methods

        #region Protected Abstract Methods

        /// <summary>
        /// 
        /// </summary>
        public abstract void CreateScene();

        #endregion Protected Abstract Methods

        #region Public Methods

        public void Start()
        {
            try
            {
                if ( Setup() )
                {
                    // start the engines rendering loop
                    engine.StartRendering();
                }
            }
            catch ( Exception ex )
            {
                // try logging the error here first, before Root is disposed of
                if ( LogManager.Instance != null )
                {
                    LogManager.Instance.Write( LogManager.BuildExceptionString( ex ) );
                }
            }
        }

        public void Dispose()
        {
            if ( engine != null )
            {
                // remove event handlers
                engine.FrameStarted -= new FrameEvent( OnFrameStarted );
                engine.FrameEnded -= new FrameEvent( OnFrameEnded );

                //engine.Dispose();
            }
            scene.RemoveAllCameras();
            scene.RemoveCamera( camera );
            camera = null;
            Root.Instance.RenderSystem.DetachRenderTarget( window );
            window.Dispose();
        }

        #endregion Public Methods

        #region Event Handlers

        protected virtual void OnFrameEnded( Object source, FrameEventArgs e )
        {
        }

        protected virtual void OnFrameStarted( Object source, FrameEventArgs e )
        {
            float scaleMove = 200 * e.TimeSinceLastFrame;

            // reset acceleration zero
            camAccel = Vector3.Zero;

            // set the scaling of camera motion
            cameraScale = 100 * e.TimeSinceLastFrame;



#if !( XBOX || XBOX360 )
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

            if ( input.IsKeyPressed( KeyCodes.P ) )
            {
                string[] temp = Directory.GetFiles( Environment.CurrentDirectory, "screenshot*.jpg" );
                string fileName = string.Format( "screenshot{0}.jpg", temp.Length + 1 );

                TakeScreenshot( fileName );

                // show briefly on the screen
                window.DebugText = string.Format( "Wrote screenshot '{0}'.", fileName );

                // show for 2 seconds
                debugTextDelay = 2.0f;
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

            if ( !input.IsMousePressed( MouseButtons.Left ) )
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
#endif
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
                window.DebugText = "";
            }
            else if ( debugTextDelay > 0.0f )
            {
                debugTextDelay -= e.TimeSinceLastFrame;
            }

            OverlayElement element = OverlayElementManager.Instance.GetElement( "Core/DebugText" );
            element.Text = window.DebugText;
        }

        protected void UpdateStats()
        {
            // TODO: Replace with CEGUI
            OverlayElement element = OverlayElementManager.Instance.GetElement( "Core/CurrFps" );
            element.Text = string.Format( "Current FPS: {0:#.00}", Root.Instance.CurrentFPS );

            element = OverlayElementManager.Instance.GetElement( "Core/BestFps" );
            element.Text = string.Format( "Best FPS: {0:#.00}", Root.Instance.BestFPS );

            element = OverlayElementManager.Instance.GetElement( "Core/WorstFps" );
            element.Text = string.Format( "Worst FPS: {0:#.00}", Root.Instance.WorstFPS );

            element = OverlayElementManager.Instance.GetElement( "Core/AverageFps" );
            element.Text = string.Format( "Average FPS: {0:#.00}", Root.Instance.AverageFPS );

            element = OverlayElementManager.Instance.GetElement( "Core/NumTris" );
            element.Text = string.Format( "Triangle Count: {0}", scene.TargetRenderSystem.FacesRendered );
            LogManager.Instance.Write( "Engine Statistics: Count: {5}  FPS <C,B,W,A>: {0:#.00} {1:#.00} {2:#.00} {3:#.00}  Trias: {4} ", Root.Instance.CurrentFPS, Root.Instance.BestFPS, Root.Instance.WorstFPS, Root.Instance.AverageFPS, scene.TargetRenderSystem.FacesRendered, Root.Instance.CurrentFrameCount );
        }

        #endregion Event Handlers
    }
}
