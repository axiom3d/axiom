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
using Axiom.Gui;
using System.Collections;

namespace Demos 
{
	/// <summary>Demo of ODE Real-Time Physics Engine</summary>
	public class Physics : TechDemo {
		#region Member variables
		private IWorld world = null;

		private DynamicObject dynaBox1;
		private DynamicObject dynaBox2;
		private DynamicObject dynaBall;

		private ArrayList dynaobjects = new ArrayList();

		#endregion
		
		#region Constructors
		public Physics() { }
		#endregion
		
		#region Methods
		private void CreatePlayGround(string material, int distance, int factor)
		{
			#region ground
			// Create Plane Mesh for ground
			Plane p = new Plane();  
			p.Normal = Vector3.UnitY;  
			p.D = 0;

			MeshManager.Instance.CreatePlane("GrassPlane", p, 2 * distance, 2 * distance, distance / factor, distance / factor, true, 2, distance / factor, distance / factor, Vector3.UnitZ);

			// Create Ground Entity attached to this Mesh
			Entity planeEnt = scene.CreateEntity("Floor", "GrassPlane");
			planeEnt.MaterialName = material;
			planeEnt.CastShadows = false;

			// add the plane as a plane for the physics simulation
			world.CreateStaticPlane (new StaticObject(scene, planeEnt, "ground", "rocky"), p);
			#endregion

			#region borders
			//
			// Create Borders in the same manner ...
			//
			Plane p1 = new Plane(); p1.Normal = Vector3.UnitX; p1.D = distance;
			MeshManager.Instance.CreatePlane ("GroundBorder1", p1, 2 * distance, 400, distance / factor, 400 / factor, true, 2, distance / factor, 400 / factor, Vector3.UnitY);
			Entity p1Ent = scene.CreateEntity ("Border1", "GroundBorder1");
			p1Ent.MaterialName = material;
			p1Ent.CastShadows = false;		
			world.CreateStaticPlane (new StaticObject(scene, p1Ent, "wall1", ""), p1);

			Plane p2 = new Plane(); p2.Normal = Vector3.UnitZ; p2.D = distance;
			MeshManager.Instance.CreatePlane ("GroundBorder2", p2, 2 * distance, 400, distance / factor, 400 / factor, true, 2, distance / factor, 400 / factor, Vector3.UnitY);
			Entity p2Ent = scene.CreateEntity ("Border2", "GroundBorder2");
			p2Ent.MaterialName = material;
			scene.RootSceneNode.CreateChildSceneNode().AttachObject(p2Ent);
			p2Ent.CastShadows = false;
			world.CreateStaticPlane (new StaticObject(scene, p2Ent, "wall2", ""), p2);


			Plane p3 = new Plane(); p3.Normal = -1 * Vector3.UnitX; p3.D = distance;
			MeshManager.Instance.CreatePlane ("GroundBorder3", p3, 2 * distance, 400, distance / factor, 400 / factor, true, 2, distance / factor, 400 / factor, Vector3.UnitY);
			Entity p3Ent = scene.CreateEntity ("Border3", "GroundBorder3");
			p3Ent.MaterialName = material;
			scene.RootSceneNode.CreateChildSceneNode().AttachObject(p3Ent);
			p3Ent.CastShadows = false;
			world.CreateStaticPlane (new StaticObject(scene, p3Ent, "wall3", ""), p3);

		
			Plane p4 = new Plane(); p4.Normal = -1 * Vector3.UnitZ; p4.D = distance;
			MeshManager.Instance.CreatePlane ("GroundBorder4", p4, 2 * distance, 400, distance / factor, 400 / factor, true, 2, distance / factor, 400 / factor, Vector3.UnitY);
			Entity p4Ent = scene.CreateEntity ("Border4", "GroundBorder4");
			p4Ent.MaterialName = material;
			scene.RootSceneNode.CreateChildSceneNode().AttachObject(p4Ent);
			p4Ent.CastShadows = false;
			world.CreateStaticPlane (new StaticObject(scene, p4Ent, "wall4", ""), p4);
			#endregion
		}

		private void CreateBoxArray(int count)
		{
			int yScale = 4;
			int distance = 300;

			for (int i = 0; i < count; i++)
			{
				Entity cube = scene.CreateEntity ("cube" + i.ToString(), "cube.mesh");
				cube.MaterialName = "Ground_Grass_sub2";
				StaticObject statCube = new StaticObject(scene, cube, "cube" + i.ToString(), "");
				statCube.Scale (1, yScale, 1);
				statCube.Position = new Vector3(distance*(count/2-i),yScale*(cube.BoundingBox.Maximum.y-cube.BoundingBox.Minimum.y)/2,-2000);

				Vector3 size = cube.BoundingBox.Maximum-cube.BoundingBox.Minimum;
				size.y *= yScale;

				world.CreateStaticBox (statCube, size);
			}
		}

		private void CreateBoxStack(int rows, int basecount)
		{
			float boxwidth = 100;

			for (int i = 0; i < rows; i++)
			{
				int boxesinarow = basecount - i;
				for (int j = 0; j < boxesinarow; j++)
				{
					Entity box = scene.CreateEntity ("boxstack" + i.ToString() + "-" + j.ToString(), "cube.mesh");
					DynamicObject dyna = new DynamicObject(scene, box, "box" + i.ToString() + "-" + j.ToString(), 15, "");
					dyna.Position = new Vector3(2000, i * boxwidth + boxwidth/2, 1.1f * boxwidth * (j - boxesinarow / 2.0f));
					world.CreateDynamicBox (dyna, box.BoundingBox.Maximum-box.BoundingBox.Minimum);

					dynaobjects.Add (dyna);
				}
			}
		}

		protected override void CreateScene() {
			//scene.ShadowTechnique = ShadowTechnique.StencilModulative ;

			InitDynamics();

			world.createSurface ("slippery", 0.1f, 0f);
			world.createSurface ("rocky", 10f, 0.2f);

			CreatePlayGround("Examples/RustySteel", 10000, 200);

			this.camSpeed = 6.0f; // Scale up the camera speed - need it fast

			Light light = scene.CreateLight("Sun");
			light.Diffuse = ColorEx.LightGoldenrodYellow;
			light.Type = LightType.Directional;
			light.Direction = new Vector3(-0.3f, -0.3f, -0.3f);

			// set ambient light to white
			scene.TargetRenderSystem.LightingEnabled = true;
			scene.AmbientLight = ColorEx.Gray;

			// the ramp ...
			Entity ramp = scene.CreateEntity ("ramp", "ramp.mesh");
			ramp.MaterialName = "Ground_Grass_sub2";
			StaticObject statRamp = new StaticObject(scene, ramp, "ramp", "slippery");
			statRamp.Position = new Vector3(100,0,100);
			world.CreateTriMesh (statRamp, ramp.Mesh);

			// ... and a ball ...
			Entity ball = scene.CreateEntity ("ball", "ball.mesh");
			ball.MaterialName = "Ogre/Skin";
			dynaBall = new DynamicObject(scene, ball, "ball", 15, "rocky");
			dynaBall.Position = new Vector3 (-400, (ball.BoundingBox.Maximum.y-ball.BoundingBox.Minimum.y)/2, -400);
			world.CreateDynamicSphere (dynaBall, ball.BoundingRadius);
			dynaobjects.Add (dynaBall);

			// ... and the ogre head ...
			Entity ogre = scene.CreateEntity ("ogre", "ogrehead.mesh");
			DynamicObject dynaOgre = new DynamicObject(scene, ogre, "ogre", 5, "");
			dynaOgre.Position = new Vector3 (-800, 600, 200);
			world.CreateDynamicMesh (dynaOgre, ogre.Mesh, ogre.BoundingRadius);
			dynaobjects.Add (dynaOgre);

			// ... and the robot ...
			Entity robot = scene.CreateEntity ("robot", "robot.mesh");
			dynaOgre = new DynamicObject(scene, robot, "robot", 5, "");
			dynaOgre.Position = new Vector3 (-1200, 400, 400);
			world.CreateDynamicMesh (dynaOgre, robot.Mesh, robot.BoundingBox.Maximum - robot.BoundingBox.Minimum);
			dynaobjects.Add (dynaOgre);

			// the staic box array ...
			CreateBoxArray(8);

			// the box stack ...
			CreateBoxStack(4, 6);

			// ground made of a box
			// this is for testing, because meshes fall through planes ...
			Entity cube = scene.CreateEntity ("boxground", "cube.mesh");
			cube.MaterialName = "Ground_Grass_sub2";
			StaticObject statCube = new StaticObject(scene, cube, "boxground", "slippery");
			statCube.Scale (20, 0.02f, 20);
			statCube.Position = new Vector3(-800,0,200);
			Vector3 size = new Vector3(2000,2f,2000);
			world.CreateStaticBox (statCube, size);

			// Create Boxes and set initial position
			Entity box1 = scene.CreateEntity ("box1", "cube.mesh");
			dynaBox1 = new DynamicObject(scene, box1, "box1", 25, "");
			dynaBox1.Position = new Vector3(0, 210, 200);
			world.CreateDynamicBox (dynaBox1, box1.BoundingBox.Maximum-box1.BoundingBox.Minimum);
			dynaobjects.Add (dynaBox1);

			Entity box2 = scene.CreateEntity ("box2", "cube.mesh");
			dynaBox2 = new DynamicObject(scene, box2, "box2", 25, "");
			dynaBox2.Position = new Vector3(0, 300, 200);
			world.CreateDynamicBox (dynaBox2, box2.BoundingBox.Maximum-box1.BoundingBox.Minimum);
			dynaobjects.Add (dynaBox2);


			// Init SkyBox and Camera
			scene.SetSkyBox(true, "Skybox/CloudyHills", 2000.0f);
			camera.Position = new Vector3(-1500, 500, 1500);
			camera.LookAt(Vector3.Zero);

			// Put Message to Show Controls
			window.DebugText = "Press J,I,K,L,U,M, or Right-Click to push a box around.";
		}

		protected override void OnFrameStarted(object source, FrameEventArgs e) {
			base.OnFrameStarted (source, e);

			// Keep camera above ground level for this demo
			if (camera.Position.y < 10) camera.Move(Vector3.UnitY * (10.0f - camera.Position.y));

			Vector3 forceVect = new Vector3(0,0,0); // Horizontal Force Vector 
			float scalar = 10000.0f; // force amplitude
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
			dynaBox1.RigidBody.AddForce(forceVect.x, (mz * scalar), forceVect.z);
			UpdateDynamics(e.TimeSinceLastFrame);

			foreach (DynamicObject o in dynaobjects)
			{
				o.UpdateFromDynamics();
			}
		}

		private void InitDynamics() {
			// TODO: Make the dynamics system request from the engine, no longer a singleton
			// create a new world
			world = new OdeWorld();//DynamicsSystem.Instance.CreateWorld();
			world.Gravity = new Vector3(0, -98.1f*2, 0);

			world.Collision += new CollisionNotifier(CollisionHandler);
		}

		private void CollisionHandler(PhysicalObject object1, PhysicalObject object2, Vector3 position)
		{
			// sort out collisions we are not interested in
			if ((object1.Name == "ground") || (object2.Name == "ground"))
				return;

			if ((object1.Name == "boxground") || (object2.Name == "boxground"))
				return;

			GuiElement element = GuiManager.Instance.GetElement("Core/DebugText");
			element.Text = (object1.Name + " collides with " + object2.Name);
		}

		private void UpdateDynamics(float time) 
		{
			if (time == 0)
				return;
			else
				time = time;

			if (time > 0.1f)
				time = 0.1f;
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
