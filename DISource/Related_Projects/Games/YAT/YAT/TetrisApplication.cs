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

using System;
using System.Drawing;
using System.IO;
using System.Collections;

using Axiom;
using Axiom.Core;
using Axiom.MathLib;
using Axiom.Input;




using MouseButtons = Axiom.Input.MouseButtons;


namespace YAT 
{
	/// <summary>
	/// 	Sample class which shows the classic spinning triangle, done in the Axiom engine.
	/// </summary>
	public class TetrisApplication : YAT.Application 
	{	
		#region Fields
		protected Camera overlayCamera;
		protected Viewport playerViewport;
		protected Viewport nextPieceViewport;
		protected Viewport menuViewport;
		protected SceneNode levelRoot;
		protected SceneNode nextPieceRoot;
		protected Overlay menuOverlay;
		protected StateManager stateManager;
		protected Game game;
		#endregion

		#region Singleton implementation

		private static TetrisApplication instance;
		public TetrisApplication()
		{
			if (instance == null) 
			{
				instance = this;
			}
		}
		public static TetrisApplication Instance 
		{
			get 
			{
				return instance;
			}
		}
		#endregion

		public InputReader Input 
		{
			get 
			{
				return input;
			}
		}
		#region Methods

		protected override bool Setup() 
		{
			bool flag = base.Setup();

			if(flag) 
			{
				input.UseKeyboardEvents = false;
			}


			
			return flag;
		}

		protected override void OnFrameStarted(object source, FrameEventArgs e) 
		{
			OnFrameStart(e);
		}
		private void OnFrameStart(FrameEventArgs e)
		{

			float scaleMove = 200 * e.TimeSinceLastFrame;

			// reset acceleration zero
			camAccel = Vector3.Zero;

			// set the scaling of camera motion
			cameraScale = 100 * e.TimeSinceLastFrame;

			// TODO: Move this into an event queueing mechanism that is processed every frame
			input.Capture();

			if(input.IsKeyPressed(Axiom.Input.KeyCodes.Escape)) 
			{
				Root.Instance.QueueEndRendering();

				return;
			}

			// subtract the time since last frame to delay specific key presses
			toggleDelay -= e.TimeSinceLastFrame;

			#region toggle rendering mode
			if(input.IsKeyPressed(Axiom.Input.KeyCodes.R) && toggleDelay < 0) 
			{
				if(camera.SceneDetail == SceneDetailLevel.Points) 
				{
					camera.SceneDetail = SceneDetailLevel.Solid;
				}
				else if(camera.SceneDetail == SceneDetailLevel.Solid) 
				{
					camera.SceneDetail = SceneDetailLevel.Wireframe;
				}
				else 
				{
					camera.SceneDetail = SceneDetailLevel.Points;
				}

				Console.WriteLine("Rendering mode changed to '{0}'.", camera.SceneDetail);

				toggleDelay = 1;
			}
			#endregion

			#region toggle the texture settings
			if(input.IsKeyPressed(Axiom.Input.KeyCodes.T) && toggleDelay < 0) 
			{
				
				switch(filtering) 
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

				Console.WriteLine("Texture Filtering changed to '{0}'.", filtering);

				// set the new default
				MaterialManager.Instance.SetDefaultTextureFiltering(filtering);
				MaterialManager.Instance.DefaultAnisotropy = aniso;
            
				toggleDelay = 1;
			}
			#endregion

			#region Take Screenshot
			if(input.IsKeyPressed(Axiom.Input.KeyCodes.P)) 
			{
				string[] temp = Directory.GetFiles(Environment.CurrentDirectory, "screenshot*.jpg");
				string fileName = string.Format("screenshot{0}.jpg", temp.Length + 1);
            
				TakeScreenshot(fileName);

				// show briefly on the screen
				window.DebugText = string.Format("Wrote screenshot '{0}'.", fileName);

				// show for 2 seconds
				debugTextDelay = 2.0f;
			}
			#endregion

			#region ShowBounding boxes
			if(input.IsKeyPressed(Axiom.Input.KeyCodes.B)) 
			{
				scene.ShowBoundingBoxes = !scene.ShowBoundingBoxes;
			}
			#endregion

			#region hide all overlays, includes ones besides the debug overlay
			if(input.IsKeyPressed(Axiom.Input.KeyCodes.F)) 
			{
				
				viewport.OverlaysEnabled = !viewport.OverlaysEnabled;
			}
			#endregion


			if(input.IsKeyPressed(Axiom.Input.KeyCodes.F12)) 
			{
				TetrisApplication.Instance.ShowDebugOverlay = !TetrisApplication.Instance.ShowDebugOverlay;
			}

			#region Update performance stats once per second
			if(statDelay < 0.0f && showDebugOverlay) 
			{
				//UpdateStats();
				statDelay = 1.0f;
			}
			else 
			{
				statDelay -= e.TimeSinceLastFrame;
			}
			#endregion

			#region turn off debug text when delay ends
			if(debugTextDelay < 0.0f) 
			{
				debugTextDelay = 0.0f;
				window.DebugText = "";
			}
			else if(debugTextDelay > 0.0f) 
			{
				debugTextDelay -= e.TimeSinceLastFrame;
			}


			#endregion

		}


		protected override void ChooseSceneManager() 
		{
			scene = SceneManagerEnumerator.Instance.GetSceneManager(SceneType.Generic);
		}
		protected override void CreateCamera() 
		{
//			camera = scene.CreateCamera("PlayerCam");
//
//			camera.Position = new Vector3(128, 25, 0);
//			camera.LookAt(new Vector3(0, 0, 0));
//			camera.Near = 1;
//			camera.Far = 1000;

		}

		#endregion

		protected override void CreateViewports()
		{
//			base.CreateViewports ();
			// Create player camera
			camera = scene.CreateCamera("PlayerCamera");
			camera.Near = 1.0f;
			camera.Far= 1000.0f;

			// Create player viewport filling the whole window
			playerViewport = window.AddViewport(camera, 0, 0, 1.0f, 1.0f, 1);
//			playerViewport = window.AddViewport(camera);
			playerViewport.BackgroundColor = new ColorEx(0.922f, 0.910f, 0.844f);
			camera.AspectRatio = ((float)playerViewport.ActualWidth/playerViewport.ActualHeight);

			

			// Create overlay camera
			overlayCamera = scene.CreateCamera("OverlayCamera");
			overlayCamera.Near = 1.0f;
			overlayCamera.Far= 1000.0f;

			// Create next piece overlay viewport
			nextPieceViewport = window.AddViewport(overlayCamera,0.81641f, 0.09635f, 0.18359f, 0.1377f,2);
			nextPieceViewport.ClearEveryFrame = false;
			overlayCamera.AspectRatio = ((float)nextPieceViewport.ActualWidth/nextPieceViewport.ActualHeight);

			// Create menu viewport filling the whole window. Use the player camera to get
			// the rigth aspect ratio. Only displays overlays so camera isn't that important.
			menuViewport = window.AddViewport(camera,0,0,1.0f, 1.0f,3);
			menuViewport.ClearEveryFrame = false;
			menuOverlay = null;

			// Register render target listeners
			window.AfterViewportUpdate+=new ViewportUpdateEventHandler(window_AfterViewportUpdate);
			window.BeforeViewportUpdate+=new ViewportUpdateEventHandler(window_BeforeViewportUpdate);

			viewport = playerViewport;


		}
		private void CreateMaterials()
		{

			Material material;

			ColorEx[] pieceColour = new ColorEx[Globals.NUM_PIECES];
			// Initialize piece colour values
			pieceColour[0] = new ColorEx(0.8f, 0.1f, 0.1f, 1.0f);
			pieceColour[1] = new ColorEx(0.1f, 0.1f, 0.8f, 1.0f);
			pieceColour[2] = new ColorEx(0.8f, 0.8f, 0.1f, 1.0f);
			pieceColour[3] = new ColorEx(0.8f, 0.1f, 0.8f, 1.0f);
			pieceColour[4] = new ColorEx(0.1f, 0.8f, 0.8f, 1.0f);
			pieceColour[5] = new ColorEx(0.1f, 0.8f, 0.1f, 1.0f);
			pieceColour[6] = new ColorEx(0.8f, 0.5f, 0.1f, 1.0f);

			// Create brick materials
			for (int i = 0; i < Globals.NUM_PIECES; ++i)
			{
				material = (Material)MaterialManager.Instance.Create("Brick"+ ((int)(i+1)).ToString());
				material.Ambient = (pieceColour[i]);
				material.Diffuse = (pieceColour[i]);
			}


			ColorEx[] pieceHighLightColour = new ColorEx[Globals.NUM_PIECES];
			// Initialize piece colour values
			pieceHighLightColour[0] = new ColorEx(0.8f, 0.1f, 0.1f, 1.0f);
			pieceHighLightColour[1] = new ColorEx(0.1f, 0.1f, 0.8f, 1.0f);
			pieceHighLightColour[2] = new ColorEx(0.8f, 0.8f, 0.1f, 1.0f);
			pieceHighLightColour[3] = new ColorEx(0.8f, 0.1f, 0.8f, 1.0f);
			pieceHighLightColour[4] = new ColorEx(0.1f, 0.8f, 0.8f, 1.0f);
			pieceHighLightColour[5] = new ColorEx(0.1f, 0.8f, 0.1f, 1.0f);
			pieceHighLightColour[6] = new ColorEx(0.8f, 0.5f, 0.1f, 1.0f);
			// Create highlighted brick materials
			for (int i = 0; i < Globals.NUM_PIECES; ++i)
			{
				material = (Material)MaterialManager.Instance.Create("BrickHighlight"+ ((int)(i+1)).ToString());
				material.Ambient = pieceHighLightColour[i];
				material.Diffuse = pieceHighLightColour[i];
			}
		}
		protected override void CreateScene() 
		{

			//this.CreateMaterials();
			
			Vector3 direction = new Vector3(-7.0f, -6.0f, -5.0f);
			Light light;
			Entity entity;

			// Setup lighting

			scene.AmbientLight = new ColorEx(0.5f, 0.5f, 0.5f, 1.0f);
			light = scene.CreateLight("Light");
			light.Diffuse = new ColorEx(0.8f, 0.8f, 0.8f);
			light.Type = LightType.Directional;
			light.Direction = direction;

			light = scene.CreateLight("BackLight");
			light.Diffuse = new ColorEx(0.1f, 0.1f, 0.1f);
			light.Type = LightType.Directional;
			light.Direction = -direction;

			// Create root scene nodes for level and next piece overlay
			levelRoot = scene.CreateSceneNode("LevelRoot");
			nextPieceRoot = scene.CreateSceneNode("NextPieceRoot");

			// Setup level
			entity = scene.CreateEntity("Level", "level.mesh");
			levelRoot.AttachObject(entity);


			// Create state manager
			stateManager = new StateManager(window);

			// Create game object
			game = new Game(camera, levelRoot, nextPieceRoot);

			window.DebugText = "Yet Another Tetris in Axiom";
		}

		public void setMenuOverlay(Overlay menuOverlay)
		{
			this.menuOverlay = menuOverlay;
		}


		public override void UpdateStats()
		{
			if (showDebugOverlay)
			{
				OverlayElement element;
				element = OverlayElementManager.Instance.GetElement("Core/CurrentFPS");
				element.Text = string.Format("Current FPS: {0}", Root.Instance.CurrentFPS);

				element = OverlayElementManager.Instance.GetElement("Core/Triangles");
				element.Text = string.Format("Triangles: {0}", scene.TargetRenderSystem.FacesRendered);
			}
		}
		protected override void RenderDebugOverlay(bool show) 
		{
//			// gets a reference to the default overlay
//			Overlay o = OverlayManager.Instance.GetByName("Core/DebugOverlay");
//
//			if(o == null) 
//			{
//				throw new Exception(string.Format("Could not find overlay named '{0}'.", "Core/DebugOverlay"));
//			}
//
//			if(show) 
//			{
//				o.Show();
//			}
//			else 
//			{
//				o.Hide();
//			}
		}


		private void window_AfterViewportUpdate(object sender, ViewportUpdateEventArgs e)
		{
				SceneNode root = scene.RootSceneNode;

				Overlay overlay;
				if (e.Viewport == playerViewport)
				{
					// Cleanup after rendering player viewport
					root.RemoveChild(levelRoot);

					// Hide game overlays
					overlay = OverlayManager.Instance.GetByName("Game/Statistics");
					overlay.Hide();

					// Hide debug overlay
					overlay = OverlayManager.Instance.GetByName("Core/Debug");
					overlay.Hide();
				}
				else if (e.Viewport == nextPieceViewport)
				{
					// Cleanup after rendering next piece viewport
					root.RemoveChild(nextPieceRoot);
				}
				else if (e.Viewport == menuViewport)
				{
					// Cleanup after rendering menu viewport
					if (menuOverlay != null)
						menuOverlay.Hide();
				}
		}

		private void window_BeforeViewportUpdate(object sender, ViewportUpdateEventArgs e)
		{
				SceneNode root = scene.RootSceneNode;

				Overlay overlay;
			if (e.Viewport == playerViewport)
			{
				// Setup for rendering player viewport
				root.AddChild(levelRoot);

				// Show game overlays
				overlay = OverlayManager.Instance.GetByName("Game/Statistics");
				overlay.Show();

				// Show debug overlay if needed
				if (showDebugOverlay)
				{
					overlay = OverlayManager.Instance.GetByName("Core/Debug");
					overlay.Show();
				}
			}
			else if (e.Viewport == nextPieceViewport)
			{
				// Setup for rendering next piece viewport
				root.AddChild(nextPieceRoot);
			}
			else if (e.Viewport == menuViewport)
			{
				// Setup for rendering menu viewport
				if (menuOverlay != null)
					menuOverlay.Show();
			}
			else
			{

			}
		}
	}




}
