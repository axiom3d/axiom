using System;
using System.Drawing;
using System.IO;
using System.Collections;

using Axiom.Core;
using Axiom.Math;
using Axiom.Input;
using MouseButtons = Axiom.Input.MouseButtons;
using Vector3 = Axiom.Math.Vector3;
using Axiom.Graphics;
using Axiom.Overlays;

using Chess.AI;
using Chess.Coldet;
using Chess.Main;
using Chess.States;

namespace Chess.Main
{
    /// <summary>
    /// 	
    /// </summary>
    public class ChessApplication : Chess.Application
    {
        #region Fields
        protected Camera playerCamera;
        protected Camera reflectCamera;
        protected Camera reflectCameraGround;
        protected Viewport playerViewport;
        protected Viewport menuViewport;
        protected Viewport messageViewport;
        protected Overlay menuOverlay;
        protected Overlay debugOverlay;
        protected Overlay messageOverlay;
        protected StateManager stateManager;
        protected InputManager inputManager;
        protected GameOGRE game;
        protected GameAI gameAI;
        protected SceneNode gameRootNode;

        protected MovablePlane movablePlane;
        protected Entity boardEntity;
        protected bool showMessageOverlay;
        protected int screenshotCounter;

        #endregion

        #region Singleton implementation

        public ChessApplication()
        {
            if ( instance == null )
            {
                instance = this;
            }
        }

        private static ChessApplication instance;
        public static ChessApplication Instance
        {
            get
            {
                return instance;
            }
        }
        #endregion

        #region Properties

        public Overlay MenuOverlay
        {
            get
            {
                return menuOverlay;
            }
            set
            {
                menuOverlay = value;
            }
        }

        public bool ShowMessageOverlay
        {
            get
            {
                return showMessageOverlay;
            }
            set
            {
                showMessageOverlay = value;
            }
        }

        public InputReader Input
        {
            get
            {
                return input;
            }
        }

        #endregion

        #region Methods

        public void UpdateDebugOverlay()
        {
            UpdateStats();
        }

        public void SaveScreenshot()
        {

        }
        public SceneNode GetGameRootNode()
        {
            return gameRootNode;
        }

        public void AttachRootNode()
        {
            // Cleanup after rendering player viewport
            if ( gameRootNode.Parent == null )
            {
                sceneManager.RootSceneNode.AddChild( gameRootNode );
            }
        }

        public void DetachRootNode()
        {
            // Cleanup after rendering player viewport
            if ( gameRootNode.Parent != null )
            {
                sceneManager.RootSceneNode.RemoveChild( gameRootNode );
            }

        }

        public override void Delete()
        {
            movablePlane = null;

            if ( gameAI != null )
                gameAI.EndGame();
            inputManager.Delete();
            inputManager = null;
            stateManager = null;
            gameAI = null;
            game = null;
            base.Delete();
        }

        protected override bool Initialize()
        {
            this.configImage = "menu_logo.png";
            this.configTitle = "Chess Configuration";
            this.logFile = "Chess.log";
            this.configFile = "EngineConfig.xml";
            this.renderWindowTitle = "Chess for Axiom";
        
            bool flag = base.Initialize();

            if ( flag )
            {
                input.UseKeyboardEvents = true;
            }
            return flag;
        }

        protected override void OnFrameStarted( object source, FrameEventArgs e )
        {

        }

        protected override void ChooseSceneManager()
        {
            sceneManager = SceneManagerEnumerator.Instance.GetSceneManager( SceneType.Generic );
        }

        protected override void CreateViewports()
        {
            // Create player camera
            playerCamera = sceneManager.CreateCamera( "PlayerCamera" );
            playerCamera.Near = 1f;
            playerCamera.Far = ( 1000.0f );

            // Create player viewport filling the whole window
            playerViewport = window.AddViewport( playerCamera );
            playerViewport.BackgroundColor = ColorEx.Black;
            playerCamera.AspectRatio = ( (float)playerViewport.ActualWidth / (float)playerViewport.ActualHeight );

            // Create the camera to do the board reflection
            reflectCamera = sceneManager.CreateCamera( "ReflectCam" );
            reflectCamera.Near = ( playerCamera.Near );
            reflectCamera.Far = ( playerCamera.Far );
            reflectCamera.AspectRatio = ( (float)playerViewport.ActualWidth / (float)playerViewport.ActualHeight );

            // Create the camera to do the ground reflection
            reflectCameraGround = sceneManager.CreateCamera( "ReflectCam2" );
            reflectCameraGround.Near = ( playerCamera.Near );
            reflectCameraGround.Far = ( playerCamera.Far );
            reflectCameraGround.AspectRatio = ( (float)playerViewport.ActualWidth / (float)playerViewport.ActualHeight );

            // Create menu viewport filling the whole window. Use the player camera to get
            // the right aspect ratio. Only displays overlays so camera isn't that important.
            menuViewport = window.AddViewport( playerCamera, 1, 1, 1, 1, 3 );
            menuViewport.ClearEveryFrame = ( false );
            menuOverlay = null;

            // Create a message viewport in the same vein
            messageViewport = window.AddViewport( playerCamera, 1, 1, 1, 1, 2 );
            messageViewport.ClearEveryFrame = ( false );

            // Set the overlays that will be used in-game
            messageOverlay = OverlayManager.Instance.GetByName( "Core/Game" );
            debugOverlay = OverlayManager.Instance.GetByName( "Core/Debug" );



            // Register render target listener
            //			window.AddListener(this);
            window.BeforeUpdate += new RenderTargetUpdateEventHandler( preRenderTargetUpdate );
            window.BeforeViewportUpdate += new ViewportUpdateEventHandler( this.preViewportUpdate );
            window.AfterViewportUpdate += new ViewportUpdateEventHandler( this.postViewportUpdate );
            window.AfterUpdate += new RenderTargetUpdateEventHandler( postRenderTargetUpdate );
        }

        private void CreateMaterials()
        {


        }

        protected override void CreateScene()
        {
            // Create root scene node
            gameRootNode = sceneManager.RootSceneNode.CreateChildSceneNode( "gameRoot" );

            // load the board
            boardEntity = sceneManager.CreateEntity( "TheBoard", "Board.mesh" );
            boardEntity.GetSubEntity( 1 ).MaterialName = "Chess/BoardSideMat";

            gameRootNode.AttachObject( boardEntity );

            // Set up the board's reflections  
            Plane plane = new Plane( Vector3.UnitY, 0 );
            plane.D = 0;
            MeshManager.Instance.CreatePlane( "board", plane, 160, 160, 1, 1, true, 1, 1, 1, Vector3.UnitZ );

			Texture texture = TextureManager.Instance.CreateManual( "RttTex", TextureType.TwoD, 1024, 1024, 0, Axiom.Media.PixelFormat.R8G8B8, TextureUsage.RenderTarget );
			RenderTarget rttTex = texture.GetBuffer().GetRenderTarget();

            Viewport v = rttTex.AddViewport( reflectCamera );
            v.ClearEveryFrame = true;
            v.BackgroundColor = ColorEx.Black;

            Material mat = (Material)MaterialManager.Instance.Create( "RttMat" );
            TextureUnitState t = mat.GetTechnique( 0 ).GetPass( 0 ).CreateTextureUnitState( "board.png" );
            t = mat.GetTechnique( 0 ).GetPass( 0 ).CreateTextureUnitState( "RttTex" );

            // Blend with base texture
            t.SetColorOperationEx( LayerBlendOperationEx.BlendManual, LayerBlendSource.Texture, LayerBlendSource.Current, ColorEx.White, ColorEx.White, 0.25f );
            t.SetProjectiveTexturing( true, reflectCamera );

            // add this object as a listener
            //			rttTex.AddListener(this);
            //			rttTex.BeforeViewportUpdate+=new ViewportUpdateEventHandler(this.preViewportUpdate);
            //			rttTex.BeforeUpdate +=new RenderTargetUpdateEventHandler(preRenderTargetUpdate);
            //			rttTex.AfterUpdate +=new RenderTargetUpdateEventHandler(postRenderTargetUpdate);
            //			rttTex.AfterViewportUpdate+=new ViewportUpdateEventHandler(this.postViewportUpdate);


            // Set up linked reflection
            reflectCamera.EnableReflection( plane );

            // Also clip
            reflectCamera.EnableCustomNearClipPlane( plane );

            // Give the plane a texture
            boardEntity.GetSubEntity( 0 ).MaterialName = "RttMat";

            // Create a prefab plane
            movablePlane = new MovablePlane( "ReflectPlane" );
            Entity mPlaneEnt;
            movablePlane.D = 0;
            movablePlane.Normal = Vector3.UnitY;
            MeshManager.Instance.CreatePlane( "ReflectionPlane", movablePlane.Plane, 600, 600, 1, 1, true, 1, 1, 1, Vector3.UnitZ );
            mPlaneEnt = sceneManager.CreateEntity( "Plane", "ReflectionPlane" );

            // Attach the rtt entity to the root of the scene  
            SceneNode mPlaneNode = gameRootNode.CreateChildSceneNode();

            // Attach both the plane entity, and the plane definition
            mPlaneNode.AttachObject( mPlaneEnt );
            mPlaneNode.AttachObject( movablePlane );
            mPlaneNode.Translate( new Vector3( 0, -15, 0 ) );

			Texture texturea = TextureManager.Instance.CreateManual( "RttTexa", TextureType.TwoD, 1024, 1024, 0, Axiom.Media.PixelFormat.R8G8B8, TextureUsage.RenderTarget );
			RenderTarget rttTexa = texturea.GetBuffer().GetRenderTarget();
            {
                Viewport va = rttTexa.AddViewport( reflectCameraGround );
                va.ClearEveryFrame = true;
                va.BackgroundColor = ColorEx.Black;

                Material mata = (Material)MaterialManager.Instance.Create( "RttMata" );
                TextureUnitState ta = mata.GetTechnique( 0 ).GetPass( 0 ).CreateTextureUnitState( "ground.png" );
                ta = mata.GetTechnique( 0 ).GetPass( 0 ).CreateTextureUnitState( "RttTexa" );
                mata.GetTechnique( 0 ).GetPass( 0 ).SetSceneBlending( SceneBlendType.TransparentAlpha );

                // Blend with base texture
                ta.SetColorOperationEx( LayerBlendOperationEx.BlendManual, LayerBlendSource.Texture, LayerBlendSource.Current, ColorEx.White, ColorEx.White, 0.15f );
                ta.SetProjectiveTexturing( true, reflectCameraGround );
                //				rttTexa.addListener(this);

                //				rttTexa.BeforeViewportUpdate+=new ViewportUpdateEventHandler(this.preViewportUpdate);
                //				rttTexa.BeforeUpdate +=new RenderTargetUpdateEventHandler(preRenderTargetUpdate);
                //				rttTexa.AfterUpdate +=new RenderTargetUpdateEventHandler(postRenderTargetUpdate);
                //				rttTexa.AfterViewportUpdate+=new ViewportUpdateEventHandler(this.postViewportUpdate);

                // Set up linked reflection
                reflectCameraGround.EnableReflection( movablePlane );

                // Also clip
                reflectCameraGround.EnableCustomNearClipPlane( movablePlane );
            }

            // Give the plane a texture
            mPlaneEnt.MaterialName = "RttMata";

            // Set the ambient light
            sceneManager.AmbientLight = new ColorEx( 0.9f, 0.6f, 0.2f );

            // and the other lights
            Light l;
            l = sceneManager.CreateLight( "Light1" );
            l.Position = new Vector3( -200, 200, -200 );
            l.Diffuse = new ColorEx( 0.5f, 0.5f, 0.5f );
            l.Specular = new ColorEx( 0.7f, 0.7f, 0.7f );
            l.CastShadows = false;

            l = sceneManager.CreateLight( "Light2" );
            l.Position = new Vector3( -200f, 200f, 200f );
            l.Diffuse = new ColorEx( 0.5f, 0.5f, 0.5f );
            l.Specular = new ColorEx( 0.7f, 0.7f, 0.7f );
            l.CastShadows = ( false );

            l = sceneManager.CreateLight( "Light3" );
            l.Position = new Vector3( 200f, 200f, -200f );
            l.Diffuse = new ColorEx( 0.5f, 0.5f, 0.5f );
            l.Specular = new ColorEx( 0.7f, 0.7f, 0.7f );
            l.CastShadows = false;

            l = sceneManager.CreateLight( "Light4" );
            l.Position = new Vector3( 200f, 200f, 200f );
            l.Diffuse = new ColorEx( 0.5f, 0.5f, 0.5f );
            l.Specular = new ColorEx( 0.7f, 0.7f, 0.7f );
            l.CastShadows = true;




        }

        protected override void PreStartRendering()
        {
            // Setup viewports, board and lights
            CreateViewports();
            CreateScene();

            // Create managers and game object
            stateManager = new StateManager( window );
            inputManager = new InputManager();
            gameAI = new GameAI();
            game = new GameOGRE( playerCamera, reflectCamera, reflectCameraGround, sceneManager, window );
        }


        public override void UpdateStats()
        {
            if ( showDebugOverlay )
            {
                OverlayElement element;
                element = OverlayElementManager.Instance.GetElement( "Core/CurrentFPS" );
                element.Text = string.Format( "Current FPS: {0:#.00}", Root.Instance.CurrentFPS );

                element = OverlayElementManager.Instance.GetElement( "Core/Triangles" );
                element.Text = string.Format( "Triangles: {0}", sceneManager.TargetRenderSystem.FacesRendered );
            }
        }

        #endregion

        #region RenderTargetListener overrides
        public virtual void preViewportUpdate( object sender, ViewportUpdateEventArgs evt )
        {


            if ( evt.Viewport == playerViewport )
            {
                // Create a skybox
                sceneManager.SetSkyBox( true, "Chess/Sunset", 50 );

                AttachRootNode();

                //				// Show game overlays
                //				Overlay overlay;
                //				overlay = OverlayManager.Instance.GetByName("Game/Statistics");
                //				overlay.Show();
                //
                //				// Show debug overlay if needed
                //				if (showDebugOverlay)
                //				{
                //					overlay = OverlayManager.Instance.GetByName("Core/Debug");
                //					overlay.Show();
                //				}
            }
            else if ( evt.Viewport == messageViewport )
            {
                // Show debug overlay if needed
                if ( showDebugOverlay )
                    debugOverlay.Show();

                // Show game message if needed
                if ( showMessageOverlay )
                    messageOverlay.Show();
            }
            else if ( evt.Viewport == menuViewport )
            {
                // Setup for rendering menu viewport
                if ( menuOverlay != null )
                    menuOverlay.Show();
            }
        }
        public virtual void postViewportUpdate( object sender, ViewportUpdateEventArgs evt )
        {
            if ( evt.Viewport == playerViewport )
            {
                // Turn off a da skybox
                //sceneManager.SetSkyBox(false, "Chess/Sunset", 50);	   
                DetachRootNode();
            }
            else if ( evt.Viewport == messageViewport )
            {
                // Hide debug overlay    
                //				if (showDebugOverlay)
                //					debugOverlay.Hide();	
                //
                //				// Hide game message if needed
                //				if (showMessageOverlay)
                //					messageOverlay.Hide();
            }
            else if ( evt.Viewport == menuViewport )
            {
                // Cleanup after rendering menu viewport
                //				if (menuOverlay != null)
                //					menuOverlay.Hide();
            }
        }
        public virtual void preRenderTargetUpdate( object sender, RenderTargetUpdateEventArgs e )
        {
            AttachRootNode();
        }
        public virtual void postRenderTargetUpdate( object sender, RenderTargetUpdateEventArgs e )
        {
            DetachRootNode();
        }

        #endregion

    }




}
