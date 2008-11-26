using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Configuration;
using Axiom.RenderSystems.Xna;
using Axiom.Controllers;
using Axiom.Controllers.Canned;
using Axiom.Overlays;
using System.Reflection;
using System.Collections;

namespace Axiom.Demos.CE
{
    /// <summary>
	/// This is the main type for your game
	/// </summary>
	public class Game1 : Microsoft.Xna.Framework.Game
	{
		protected const string CONFIG_FILE = @"EngineConfig.xml";
		private Root engine;
		private RenderSystem renderSystem;
		protected Camera camera;
		protected Axiom.Core.Viewport viewport;
		protected SceneManager scene;
		protected RenderWindow window;
        protected Axiom.Math.Vector3 camAccel = Axiom.Math.Vector3.Zero;
        protected Axiom.Math.Vector3 camVelocity = Axiom.Math.Vector3.Zero;
        protected float camSpeed = 2.5f;
        protected Axiom.Core.Light redLight;
        protected SceneNode redLightNode;
        protected Axiom.Core.Light blueLight;
        protected SceneNode blueLightNode;
        protected Axiom.Core.Light pointLight;
        protected SceneNode pointLightNode;
        protected Axiom.Core.Light dirLight;
        protected SceneNode dirLightNode;
        protected Axiom.Core.Light spotLight;
        protected SceneNode spotLightNode;
        OverlayElement currFPSElement;
        OverlayElement bestFPSElement;
        OverlayElement worstFPSElement;
        OverlayElement averageFPSElement;
        OverlayElement numTrisElement;
        protected float statDelay = 0.0f;

		public Game1()
		{
            try
            {
                engine = new Root( CONFIG_FILE, "AxiomDemos.log" );
                Axiom.RenderSystems.Xna.Plugin renderSystemPlugin = new Axiom.RenderSystems.Xna.Plugin();
                renderSystemPlugin.Start();
                engine.RenderSystem = engine.RenderSystems[ 0 ];
                ResourceManager.AddCommonArchive( "Content", "Folder" );
                window = engine.Initialize( false );
                window = Root.Instance.CreateRenderWindow( "Main", this.Window.ClientBounds.Width, this.Window.ClientBounds.Height, 32, true );

                scene = engine.SceneManagers.GetSceneManager( SceneType.Generic );
                scene.ClearScene();
                // create a camera and initialize its position
                camera = scene.CreateCamera( "MainCamera" );
                camera.Position = new Axiom.Math.Vector3( 0, 0, 500 );
                camera.LookAt( new Axiom.Math.Vector3( 0, 0, -300 ) );
                // set the near clipping plane to be very close
                camera.Near = 0.1f;
                //camera.Far = 100000;

                viewport = window.AddViewport( camera, 0, 0, 1.0f, 1.0f, 100 );
                viewport.BackgroundColor = ColorEx.Black;

                TextureManager.Instance.DefaultNumMipMaps = 5;

                Content.RootDirectory = "Content";

                scene.AmbientLight = new ColorEx(0.25f, 0.25f, 0.25f);

                SceneNode mainNode = scene.RootSceneNode.CreateChildSceneNode();
                Entity entity = scene.CreateEntity("Test", "dragon.mesh");
                mainNode.AttachObject(entity);
                //mainNode.Position = new Axiom.Math.Vector3(0, 50, 0);
                camera.MoveRelative(new Axiom.Math.Vector3(50, 0, 20));
                camera.LookAt(new Axiom.Math.Vector3(0, 0, 0));

                redLightNode = scene.RootSceneNode.CreateChildSceneNode();

                Axiom.Math.Vector3 redLightPos = new Axiom.Math.Vector3(78, -8, -70);

                redLight = scene.CreateLight("RedLight");
                redLight.Position = redLightPos;
                redLight.Type = LightType.Point;
                redLight.Diffuse = ColorEx.Red;
                redLight.SetAttenuation(1000.0f, 0.0f, 0.005f, 0.0f);
                redLightNode.AttachObject(redLight);

                BillboardSet redLightBillboardset = scene.CreateBillboardSet("RedLightBillboardSet", 5);
                redLightBillboardset.MaterialName = "Flare";
                Billboard redLightBillboard = redLightBillboardset.CreateBillboard(redLightPos, ColorEx.Red);
                redLightNode.AttachObject(redLightBillboardset);

                blueLightNode = scene.RootSceneNode.CreateChildSceneNode();

                Axiom.Math.Vector3 blueLightPos = new Axiom.Math.Vector3(50, 70, 80);

                blueLight = scene.CreateLight("BlueLight");
                blueLight.Position = blueLightPos;
                blueLight.Type = LightType.Point;
                blueLight.Diffuse = ColorEx.Blue;
                blueLight.SetAttenuation(1000.0f, 0.0f, 0.005f, 0.0f);
                blueLightNode.AttachObject(blueLight);

                BillboardSet blueLightBillboardset = scene.CreateBillboardSet("BlueLightBillboardSet", 5);
                blueLightBillboardset.MaterialName = "Flare";
                Billboard blueLightBillboard = blueLightBillboardset.CreateBillboard(blueLightPos, ColorEx.Blue);
                blueLightNode.AttachObject(blueLightBillboardset);

                pointLightNode = scene.RootSceneNode.CreateChildSceneNode();

                pointLight = scene.CreateLight("PointLight");
                pointLight.Type = LightType.Point;
                pointLight.Diffuse = ColorEx.White;
                pointLight.SetAttenuation(1000.0f, 0.0f, 0.005f, 0.0f);
                pointLightNode.AttachObject(pointLight);

                BillboardSet pointLightBillboardset = scene.CreateBillboardSet("PointLightBillboardSet", 5);
                pointLightBillboardset.MaterialName = "Flare";
                Billboard pointLightBillboard = pointLightBillboardset.CreateBillboard(Axiom.Math.Vector3.Zero, ColorEx.White);
                pointLightNode.AttachObject(pointLightBillboardset);
                pointLightNode.Visible = false;
                pointLight.IsVisible = false;

                dirLightNode = scene.RootSceneNode.CreateChildSceneNode();

                dirLight = scene.CreateLight("DirLight");
                dirLight.Type = LightType.Directional;
                dirLight.Diffuse = ColorEx.White;
                dirLightNode.AttachObject(dirLight);

                BillboardSet dirLightBillboardset = scene.CreateBillboardSet("DirLightBillboardSet", 5);
                dirLightBillboardset.MaterialName = "Flare";
                Billboard dirLightBillboard = dirLightBillboardset.CreateBillboard(Axiom.Math.Vector3.Zero, ColorEx.White);
                dirLightNode.AttachObject(dirLightBillboardset);
                dirLightNode.Visible = false;
                dirLight.IsVisible = false;

                spotLightNode = scene.RootSceneNode.CreateChildSceneNode();

                spotLight = scene.CreateLight("SpotLight");
                spotLight.Type = LightType.Spotlight;
                spotLight.Diffuse = ColorEx.White;
                spotLight.SetAttenuation(1000.0f, 0.0f, 0.005f, 0.0f);
                spotLight.SetSpotlightRange(15.0f, 20.0f);
                spotLightNode.AttachObject(spotLight);

                BillboardSet spotLightBillboardset = scene.CreateBillboardSet("SpotLightBillboardSet", 5);
                spotLightBillboardset.MaterialName = "Flare";
                Billboard spotLightBillboard = spotLightBillboardset.CreateBillboard(Axiom.Math.Vector3.Zero, ColorEx.White);
                spotLightNode.AttachObject(spotLightBillboardset);
                spotLightNode.Visible = false;
                spotLight.IsVisible = false;

                Overlay o = OverlayManager.Instance.GetByName("Core/DebugOverlay");
                o.Show();

                currFPSElement = OverlayElementManager.Instance.GetElement("Core/CurrFps");
                bestFPSElement = OverlayElementManager.Instance.GetElement("Core/BestFps");
                worstFPSElement = OverlayElementManager.Instance.GetElement("Core/WorstFps");
                averageFPSElement = OverlayElementManager.Instance.GetElement("Core/AverageFps");
                numTrisElement = OverlayElementManager.Instance.GetElement("Core/NumTris");
            }
            catch ( Exception ex )
            {
                string msg = ex.Message;
            }
		}

		/// <summary>
		/// Allows the game to perform any initialization it needs to before starting to run.
		/// This is where it can query for any required services and load any non-graphic
		/// related content.  Calling base.Initialize will enumerate through any components
		/// and initialize them as well.
		/// </summary>
		protected override void Initialize()
		{
			// TODO: Add your initialization logic here

			base.Initialize();
		}

		/// <summary>
		/// LoadContent will be called once per game and is the place to load
		/// all of your content.
		/// </summary>
		protected override void LoadContent()
		{
			// TODO: use this.Content to load your game content here
		}

		/// <summary>
		/// UnloadContent will be called once per game and is the place to unload
		/// all content.
		/// </summary>
		protected override void UnloadContent()
		{
			// TODO: Unload any non ContentManager content here
		}

		/// <summary>
		/// Allows the game to run logic such as updating the world,
		/// checking for collisions, gathering input, and playing audio.
		/// </summary>
		/// <param name="gameTime">Provides a snapshot of timing values.</param>
		protected override void Update(GameTime gameTime)
		{
            GamePadState state = GamePad.GetState(PlayerIndex.One);

            // Allows the game to exit
            if(state.Buttons.Back == ButtonState.Pressed)
                this.Exit();

            float dt = ((float)gameTime.ElapsedRealTime.Milliseconds) * 0.001f;
            //clarabie - temporary. Something's wrong with the dt calculation above
            //so forcing it to 0.033 (30 fps)
            dt = 0.033f;

            camAccel = Axiom.Math.Vector3.Zero; // reset acceleration zero
            float scaleMove = 10 * dt; // motion scalar
            float scaleTurn = 20 * dt; // turn rate scalar

            camAccel.z = -1.0f * state.ThumbSticks.Left.Y;
            camAccel.x = state.ThumbSticks.Left.X;

            camVelocity += (camAccel * scaleMove * camSpeed);
            camera.MoveRelative(camVelocity);

            if (camAccel == Axiom.Math.Vector3.Zero)
            {
                camVelocity *= (1 - (4 * dt));
            }

            float yaw = -1.0f * scaleTurn * state.ThumbSticks.Right.X;
            float pitch = scaleTurn * state.ThumbSticks.Right.Y;
            if (yaw != 0)
            {
                camera.Yaw(yaw);
            }
            camera.Pitch(pitch);

            engine.RenderOneFrame();

            //update light positions
            redLightNode.Yaw(10 * dt);
            blueLightNode.Pitch(20 * dt);

            //if A is pressed, hide all lights and show white point light at camera position
            if(state.IsButtonDown(Buttons.A))
            {
                HideAllLights();
                //change point light position
                pointLightNode.Visible = true;
                pointLight.IsVisible = true;
                pointLightNode.Position = camera.Position;
            }

            //if X is pressed, hide all lights and show white directional light facing camera forward direction
            if(state.IsButtonDown(Buttons.X))
            {
                HideAllLights();
                dirLightNode.Visible = true;
                dirLight.IsVisible = true;
                dirLightNode.Position = camera.Position; //doesn't really matter for directional light
                dirLight.Direction = camera.Direction; 
            }

            if(state.IsButtonDown(Buttons.Y))
            {
                HideAllLights();
                spotLightNode.Visible = true;
                spotLight.IsVisible = true;
                spotLightNode.Position = camera.Position;
                spotLight.Direction = camera.Direction;
            }

            // update performance stats once per second
            if (statDelay < 0.0f)
            {
                UpdateStats();
                statDelay = 1.0f;
            }
            else
            {
                statDelay -= dt;
            }

            base.Update(gameTime); 
		}

        protected void UpdateStats()
        {
            currFPSElement.Text = string.Format("Current FPS: {0:#.00}", Root.Instance.CurrentFPS);
            bestFPSElement.Text = string.Format("Best FPS: {0:#.00}", Root.Instance.BestFPS);
            worstFPSElement.Text = string.Format("Worst FPS: {0:#.00}", Root.Instance.WorstFPS);
            averageFPSElement.Text = string.Format("Average FPS: {0:#.00}", Root.Instance.AverageFPS);
            numTrisElement.Text = string.Format("Triangle Count: {0}", scene.TargetRenderSystem.FacesRendered);
        }

        protected void HideAllLights()
        {
            redLightNode.Visible = false;
            redLight.IsVisible = false;
            blueLightNode.Visible = false;
            blueLight.IsVisible = false;
            pointLightNode.Visible = false;
            pointLight.IsVisible = false;
            dirLightNode.Visible = false;
            dirLight.IsVisible = false;
            spotLightNode.Visible = false;
            spotLight.IsVisible = false;
        }

		/// <summary>
		/// This is called when the game should draw itself.
		/// </summary>
		/// <param name="gameTime">Provides a snapshot of timing values.</param>
		protected override void Draw(GameTime gameTime)
		{
			//GraphicsDevice.Clear(Color.CornflowerBlue);

			// TODO: Add your drawing code here

			//base.Draw(gameTime);
            //engine.RenderOneFrame();
		}
	}
}
