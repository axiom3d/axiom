using System;
using System.Collections;

using Axiom.Core;
using Axiom.Math;
using Axiom.Input;
using Axiom.Graphics;

using Chess.AI;
using Chess.Coldet;
using Chess.Main;
using Chess.States;
using Axiom.Animating;
using System.Drawing;

namespace Chess.Main
{
	/// <summary>
	/// Summary description for Class1.
	/// </summary>
	public class GameOGRE
	{
		#region Fields
		public Axiom.Math.Vector3 cameraTarget;
		public float cameraWantedYaw;
		public float cameraWantedPitch;
		public float cameraWantedDistance;
		public float cameraCorrection;	          

		
		protected ArrayList states = new ArrayList();	  
		protected Camera camera; 
		protected Camera reflectCamera;
		protected Camera reflectCameraGround;  
		protected SceneManager sceneManager;	       
		protected RenderWindow window;
		protected bool isPaused;
		#endregion

		#region Constructors
		#region Singleton implementation

		private static GameOGRE instance;
		public GameOGRE(Camera camera, Camera reflectcamera, Camera reflectcamera2, SceneManager scenemanager, RenderWindow window)
		{
			if (instance == null) 
			{
				instance = this;
				this.camera = camera;
				this.reflectCamera = reflectcamera;
				this.reflectCameraGround = reflectcamera2;
				this.sceneManager = scenemanager;
				this.window = window;
				this.isPaused = false;
				// Initialize camera
				cameraTarget =  new Axiom.Math.Vector3(0.0f, 2.0f, 15.0f); //
				cameraWantedYaw = 0f; 
				cameraWantedPitch = -60f; 
				cameraWantedDistance = 250.0f;
				cameraCorrection = 15.0f;

				camera.Orientation = (Quaternion.FromAngleAxis(Axiom.Math.Utility.DegreesToRadians(cameraWantedYaw), Axiom.Math.Vector3.UnitY) 
					* Quaternion.FromAngleAxis(Axiom.Math.Utility.DegreesToRadians(cameraWantedPitch), Axiom.Math.Vector3.UnitX));
				camera.Position = (cameraTarget - (cameraWantedDistance*camera.Direction));

				

				// Setup animation default
				Animation.DefaultInterpolationMode = InterpolationMode.Linear;
				//Axiom.Animating.Animation.DefaultRotationInterpolationMode = Axiom.Animating.RotationMode.Linear;

				// Create game states
				states.Add(new CPUvCPUState());
				states.Add(new PvCPUState());
				states.Add(new PvPState());

				// Create menu states
				states.Add(new MainMenuState());
				states.Add(new ColourState());
				states.Add(new OptionState());
				states.Add(new HelpState());
				states.Add(new GameMenuState());
				states.Add(new PromotionMenuState());
				states.Add(new GameOverState());

				// Set up state stack
				StateManager.Instance.AddState(CPUvCPUState.Instance);
				StateManager.Instance.AddState(MainMenuState.Instance);


				// start the demo mode off 
				GameAI.Instance.Start();  
			}
		}
		public static GameOGRE Instance 
		{
			get 
			{
				return instance;
			}
		}
		#endregion
		#endregion
		
		#region Methods	
		public void Delete()
		{
			State state;
			for (int x = 0;x < states.Count;x++)
			{
				state = (State)states[x];
				state.Delete();
			}
		}

		// Update functions
		public void Invalidate(int updateFlags)
		{

		}
		public void Update(float dt)
		{
			// animate hands
			if (!isPaused)    
				Hand.UpdateHands(dt);

			// Update camera position
			float t = dt * cameraCorrection;
			Quaternion wantedOrientation;
			float currentDistance;
			float distance;

			// Clamp to one, so camera doesn't move too far
			if (t > 1.0f)
				t = 1.0f;

			// Create quaternion describing wanted orientation
			wantedOrientation = Quaternion.FromAngleAxis(Axiom.Math.Utility.DegreesToRadians(cameraWantedYaw), Axiom.Math.Vector3.UnitY) 
			* Quaternion.FromAngleAxis(Axiom.Math.Utility.DegreesToRadians(cameraWantedPitch), Axiom.Math.Vector3.UnitX);


			Axiom.Math.Vector3 v = (camera.Position - cameraTarget);
			currentDistance = v.Length;
			distance = currentDistance + t*(cameraWantedDistance - currentDistance);

			// Move camera towards wanted position
			camera.Orientation = (Quaternion.Slerp(t, camera.Orientation, wantedOrientation, true));
			camera.Position = (cameraTarget - distance*camera.Direction);

			reflectCamera.Orientation = camera.Orientation;
			reflectCamera.Position = camera.Position;

			reflectCameraGround.Orientation=camera.Orientation;   
			reflectCameraGround.Position = camera.Position;
		}
		public void Pause(bool toggle) 
		{
			isPaused = toggle;
		}  
		public Ray CreateRay()
		{
			PointF tMousePos = CeGui.MouseCursor.Instance.Position; 
			tMousePos.X /= GameOGRE.Instance.window.Width; 
			tMousePos.Y /= GameOGRE.Instance.window.Height; 

			//tMousePos.x = ChessApplication.Instance.Input.RelativeMouseX;
			//tMousePos.y = ChessApplication.Instance.Input.RelativeMouseY;

			Ray mouseRay = camera.GetCameraToViewportRay(tMousePos.X, tMousePos.Y);        
			return mouseRay;
		}
		#endregion
	}
}
