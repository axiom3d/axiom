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
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Input;
using Axiom.MathLib;
using Axiom.Physics;
using Axiom.Scripting;
using Axiom.Utility;
using Axiom.Dynamics.ODE;

namespace Demos {
	/// <summary>Demo of ODE Real-Time Physics Engine</summary>
	public class Physics : TechDemo {
		#region Member variables
		private IWorld world = null;
		private GameObject box;
		private GameObject box2;
		#endregion
		
		#region Constructors
		public Physics() { }
		#endregion
		
		#region Methods
		protected override void CreateScene() {
			scene.ShadowTechnique = ShadowTechnique.StencilModulative;

			InitDynamics();
			this.camSpeed = 6.0f; // Scale up the camera speed - need it fast

			Light light = scene.CreateLight("Sun");
			light.Diffuse = ColorEx.LightGoldenrodYellow;
			light.Type = LightType.Directional;
			light.Direction = new Vector3(-0.3f, -0.3f, -0.3f);

			// Create Plane Mesh for ground
			Plane p = new Plane();  
			p.Normal = Vector3.UnitY;  
			p.D = 0;

			MeshManager.Instance.CreatePlane("GrassPlane", p, 20000, 20000, 50, 50, true, 2, 50, 50, Vector3.UnitZ);

			// Create Ground Entity attached to this Mesh
			Entity planeEnt = scene.CreateEntity("Floor", "GrassPlane");
			planeEnt.MaterialName = "Examples/RustySteel";
			planeEnt.CastShadows = false;
			scene.RootSceneNode.CreateChildSceneNode().AttachObject(planeEnt);

			// set ambient light to white
			scene.TargetRenderSystem.LightingEnabled = true;
			scene.AmbientLight = ColorEx.Gray;

			// Create Boxes and set initial position
			box = new Box(scene);  box.Position = new Vector3(0, 300, 200);
			box2 = new Box(scene); box2.Position = new Vector3(0, 100, 200);

			// HACK: Decouple this and register objects with the World
			box.RigidBody = world.CreateBody(box, DynamicsBodyType.Box, 0.0f);
			box2.RigidBody = world.CreateBody(box2, DynamicsBodyType.Box, 0.0f);

			// Init SkyBox and Camera
			scene.SetSkyBox(true, "Skybox/CloudyHills", 2000.0f);
			camera.Position = new Vector3(0, 200, 700);
			camera.LookAt(Vector3.Zero);

			// Put Message to Show Controls
			window.DebugText = "Press J,I,K,L,U,M, or Right-Click to push a box around.";
		}

		protected override void OnFrameStarted(object source, FrameEventArgs e) {
			base.OnFrameStarted (source, e);

			// Keep camera above ground level for this demo
			if (camera.Position.y < 10) camera.Move(Vector3.UnitY * (10.0f - camera.Position.y));

			Vector3 forceVect = new Vector3(0,0,0); // Horizontal Force Vector 
			float scalar = 500.0f; // force amplitude
			float mx, my, mz; mx = my = mz = 0.0f; // right, back, up forces

			// Calculate Forces from Keyboard Inputs
			if(input.IsKeyPressed(KeyCodes.L)) mx += 1; // Right
			if(input.IsKeyPressed(KeyCodes.J)) mx -= 1; // Left
			if(input.IsKeyPressed(KeyCodes.I)) my -= 1; // Forward
			if(input.IsKeyPressed(KeyCodes.K)) my += 1; // Back
			if(input.IsKeyPressed(KeyCodes.U)) mz += 1; // Up
			if(input.IsKeyPressed(KeyCodes.M)) mz -= 1; // Down

			// Right-mouse click changes mouse to apply a force instead of camera motion.
			if(input.IsMousePressed(MouseButtons.Button1)) { 
				mx += 0.25f * input.RelativeMouseX; // Right/Left
				my += 0.25f * input.RelativeMouseY; // Forward/Backward
				mz += 0.15f * input.RelativeMouseZ; // Wheel Up/Down
			}

			// Calculate Horizontal Force Impact (forces are relative to camera orientation)
			if (mx!=0 || my!=0) {
				float forceYaw = (float)Math.Atan2(-mx,-my);  // Calculate horizontal direction
				forceVect = Quaternion.FromAngleAxis(forceYaw, camera.FixedYawAxis) * 
					camera.DerivedOrientation * -Vector3.UnitZ * scalar *
					((float)Math.Sqrt((mx*mx) + (my*my)));
			}

			// Apply Relative Force to Box and then Update Dynamics
			box.RigidBody.AddForce(forceVect.x, (mz * scalar), forceVect.z);
			UpdateDynamics(e.TimeSinceLastFrame);
			box.UpdateFromDynamics();
			box2.UpdateFromDynamics();
		}

		private void InitDynamics() {
			// TODO: Make the dynamics system request from the engine, no longer a singleton
			// create a new world
			world = new OdeWorld();//DynamicsSystem.Instance.CreateWorld();
			world.Gravity = new Vector3(0, -98.1f*2, 0);
		}

		private void UpdateDynamics(float time) {
			world.Step(time);
		}
		#endregion
	}

	/// <summary>Box Game Object Class.</summary>   
	public class Box : GameObject {
		public static int nextNum = 0;
		/// <summary>Construct Box Game object.</summary>   
		public Box(SceneManager sceneManager): base(sceneManager) {
			sceneObject = sceneMgr.CreateEntity("Box" + nextNum++, "cube.mesh");
			node = sceneMgr.RootSceneNode.CreateChildSceneNode("BoxEntNode" + nextNum);
			node.AttachObject(sceneObject);
			NotifySceneObject(sceneObject);
		}
	}
}
