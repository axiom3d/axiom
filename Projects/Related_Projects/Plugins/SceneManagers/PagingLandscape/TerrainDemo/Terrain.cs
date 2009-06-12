using System;
using System.IO;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Math;
//using Axiom.Utility;
using Axiom.Input;

namespace TerrainDemo {
	/// <summary>
	/// Summary description for Terrain.
	/// </summary>
	public class Terrain : TechDemo, IRaySceneQueryListener {

        SceneNode waterNode;
        float flowAmount;
        bool flowUp = true;
        const float FLOW_HEIGHT = 0.8f;
        const float FLOW_SPEED = 0.2f;
		RaySceneQuery raySceneQuery = null;

		// move the Camera like a human at 3m/sec
		bool humanSpeed = false;

		// keep Camera 2m above the ground
		bool followTerrain = false;

        protected override void ChooseSceneManager() {
            SceneManager = Root.CreateSceneManager(SceneType.ExteriorFar, "TechDemoSMInstance");
            SceneManager.ClearScene();
        }

        protected override void CreateCamera() {
            Camera = SceneManager.CreateCamera("PlayerCam");

//            Camera.Position = new Vector3(128, 25, 128);
//            Camera.LookAt(new Vector3(0, 0, -300));
//            Camera.Near = 1;
//            Camera.Far = 384;

			Camera.Position = new Vector3(128, 400, 128);
			Camera.LookAt(new Vector3(0, 0, -300));
			Camera.Near = 1;
			Camera.Far = 100000;
        }

        protected override void CreateScene() {
            Viewport.BackgroundColor = ColorEx.White;

            SceneManager.AmbientLight = new ColorEx(0.5f, 0.5f, 0.5f);

            Light light = SceneManager.CreateLight("MainLight");
            light.Position = new Vector3(20, 80, 50);
            light.Diffuse = ColorEx.Blue;

            SceneManager.LoadWorldGeometry("Landscape.xml");
            
            // SceneManager.SetFog(FogMode.Exp2, ColorEx.White, .008f, 0, 250);

            // water plane setup
            Plane waterPlane = new Plane(Vector3.UnitY, 1.5f);

            MeshManager.Instance.CreatePlane(
                "WaterPlane",
				ResourceGroupManager.DefaultResourceGroupName,
                waterPlane,
                2800, 2800,
                20, 20,
                true, 1,
                10, 10,
                Vector3.UnitZ);

            Entity waterEntity  = SceneManager.CreateEntity("Water", "WaterPlane");
            waterEntity.MaterialName = "Terrain/WaterPlane";

            waterNode = SceneManager.RootSceneNode.CreateChildSceneNode("WaterNode");
            waterNode.AttachObject(waterEntity);
            waterNode.Translate(new Vector3(1000, 0, 1000));

			raySceneQuery = SceneManager.CreateRayQuery( new Ray(Camera.Position, Vector3.NegativeUnitY));
        }

        protected override bool OnFrameStarted(object source, FrameEventArgs e) {
            //float moveScale;
            float waterFlow;

            waterFlow = FLOW_SPEED * e.TimeSinceLastFrame;

            if(waterNode != null) {
                if(flowUp) {
                    flowAmount += waterFlow;
                }
                else {
                    flowAmount -= waterFlow;
                }

                if(flowAmount >= FLOW_HEIGHT) {
                    flowUp = false;
                }
                else if(flowAmount <= 0.0f) {
                    flowUp = true;
                }

                waterNode.Translate(new Vector3(0, flowUp ? waterFlow : -waterFlow, 0));
            }

			float scaleMove = 200 * e.TimeSinceLastFrame;

			// reset acceleration zero
			camAccel = Vector3.Zero;

			// set the scaling of Camera motion
			cameraScale = 100 * e.TimeSinceLastFrame;

			// TODO: Move this into an event queueing mechanism that is processed every frame
			Input.Capture();

			if(Input.IsKeyPressed(KeyCodes.Escape)) 
			{
				Root.Instance.QueueEndRendering();

				return false;
			}

			if(Input.IsKeyPressed(KeyCodes.H) && toggleDelay < 0)
			{
				humanSpeed = !humanSpeed;
				toggleDelay = 1;
			}

			if(Input.IsKeyPressed(KeyCodes.G) && toggleDelay < 0)
			{
				followTerrain = !followTerrain;
				toggleDelay = 1;
			}

			if(Input.IsKeyPressed(KeyCodes.A)) 
			{
				camAccel.x = -0.5f;
			}

			if(Input.IsKeyPressed(KeyCodes.D)) 
			{
				camAccel.x = 0.5f;
			}

			if(Input.IsKeyPressed(KeyCodes.W)) 
			{
				camAccel.z = -1.0f;
			}

			if(Input.IsKeyPressed(KeyCodes.S)) 
			{
				camAccel.z = 1.0f;
			}

			camAccel.y += (float)(Input.RelativeMouseZ * 0.1f);

			if(Input.IsKeyPressed(KeyCodes.Left)) 
			{
				Camera.Yaw(cameraScale);
			}

			if(Input.IsKeyPressed(KeyCodes.Right)) 
			{
				Camera.Yaw(-cameraScale);
			}

			if(Input.IsKeyPressed(KeyCodes.Up)) 
			{
				Camera.Pitch(cameraScale);
			}

			if(Input.IsKeyPressed(KeyCodes.Down)) 
			{
				Camera.Pitch(-cameraScale);
			}

			// subtract the time since last frame to delay specific key presses
			toggleDelay -= e.TimeSinceLastFrame;

			// toggle rendering mode
			if(Input.IsKeyPressed(KeyCodes.R) && toggleDelay < 0) 
			{
				if ( Camera.PolygonMode == PolygonMode.Points ) 
				{
					Camera.PolygonMode = PolygonMode.Solid;
				}
				else if ( Camera.PolygonMode == PolygonMode.Solid ) 
				{
					Camera.PolygonMode = PolygonMode.Wireframe;
				}
				else 
				{
					Camera.PolygonMode = PolygonMode.Points;
				}

				Console.WriteLine( "Rendering mode changed to '{0}'.", Camera.PolygonMode );

				toggleDelay = 1;
			}

			if(Input.IsKeyPressed(KeyCodes.T) && toggleDelay < 0) 
			{
				// toggle the texture settings
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

			if(Input.IsKeyPressed(KeyCodes.P)) 
			{
				string[] temp = Directory.GetFiles(Environment.CurrentDirectory, "screenshot*.jpg");
				string fileName = string.Format("screenshot{0}.jpg", temp.Length + 1);
                
				// show briefly on the screen
				debugText = string.Format("Wrote screenshot '{0}'.", fileName);

				TakeScreenshot(fileName);

				// show for 2 seconds
				debugTextDelay = 2.0f;
			}

			if(Input.IsKeyPressed(KeyCodes.B)) 
			{
				SceneManager.ShowBoundingBoxes = !SceneManager.ShowBoundingBoxes;
			}

			if(Input.IsKeyPressed(KeyCodes.F)) 
			{
				// hide all overlays, includes ones besides the debug overlay
				Viewport.ShowOverlays = !Viewport.ShowOverlays;
			}

			if(!Input.IsMousePressed(MouseButtons.Left)) 
			{
				float cameraYaw = -Input.RelativeMouseX * .13f;
				float cameraPitch = -Input.RelativeMouseY * .13f;
                
				Camera.Yaw(cameraYaw);
				Camera.Pitch(cameraPitch);
			} 
			else 
			{
				cameraVector.x += Input.RelativeMouseX * 0.13f;
			}

			if ( humanSpeed ) 
			{
				camVelocity = camAccel * 7.0f;
				Camera.MoveRelative(camVelocity * e.TimeSinceLastFrame);
			} 
			else 
			{
				camVelocity += (camAccel * scaleMove * camSpeed);

				// move the Camera based on the accumulated movement vector
				Camera.MoveRelative(camVelocity * e.TimeSinceLastFrame);

				// Now dampen the Velocity - only if user is not accelerating
				if (camAccel == Vector3.Zero) 
				{ 
					camVelocity *= (1 - (6 * e.TimeSinceLastFrame)); 
				}
			}

			// update performance stats once per second
			if(statDelay < 0.0f && showDebugOverlay) 
			{
				//UpdateDebugOverlay(null, new Axiom.Core.FrameEventArgs());
				statDelay = 1.0f;
			}
			else 
			{
				statDelay -= e.TimeSinceLastFrame;
			}

			// turn off debug text when delay ends
			if(debugTextDelay < 0.0f) 
			{
				debugTextDelay = 0.0f;
				debugText = "";
			}
			else if(debugTextDelay > 0.0f) 
			{
				debugTextDelay -= e.TimeSinceLastFrame;
			}

			if ( followTerrain ) 
			{
				// adjust new Camera position to be a fixed distance above the ground
				raySceneQuery.Ray = new Ray(Camera.Position, Vector3.NegativeUnitY);
				raySceneQuery.Execute(this);
			}

            return base.OnFrameStarted(source, e);
            return true;
        }

		public bool OnQueryResult(SceneQuery.WorldFragment fragment, float distance) 
		{
			Camera.Position = new Vector3(Camera.Position.x, fragment.SingleIntersection.y + 2.0f, Camera.Position.z);
			return false;
		}
		public bool OnQueryResult(MovableObject sceneObject, float distance) 
		{
			return true;
		}
	}
}
