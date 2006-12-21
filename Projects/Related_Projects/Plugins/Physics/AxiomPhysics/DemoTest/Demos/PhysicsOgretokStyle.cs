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

/*
 * This file is based on the sourcecode of the ogretokamak physics add-on to ogre.
 * All the simulations you will find here are a ported from the simulations done there.
 */

using System;
using System.Diagnostics;
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
	/// <summary>
	/// Demo of Axiom physics extension.
	/// This is a port of the ogretok contribution.
	/// </summary>
	public class PhysicsOgretokStyle : TechDemo 
	{
		private IWorld world = null;

		private float timeUntilNextAction = 0;
		private float physicsSpeed = 1.0f;

		private DynamicObject bullet;

		protected override void CreateScene() 
		{
			//scene.ShadowTechnique = ShadowTechnique.StencilModulative ;

			// setup the camera
			camera.Position = new Vector3(0, 200, 750);
			camera.LookAt(new Vector3(0, 200, -300));
			camera.Near = 5;

			// set ambient light
			scene.AmbientLight = new ColorEx (0.5f, 0.5f, 0.5f);

			// set skydome
			scene.SetSkyDome (true, "Examples/CloudySky", 5, 8);

			// setup light
			Light light = scene.CreateLight("Sun");
			light.Diffuse = ColorEx.LightGoldenrodYellow;
			light.Type = LightType.Directional;
			light.Direction = new Vector3(-0.3f, -0.3f, -0.3f);

			// setup overlay 
			Overlay o = OverlayManager.Instance.GetByName("AxiomPhysics/BasicOverlay");
			o.Show();

			// setup the basic testsim
			setupTestSim1(scene.RootSceneNode);
		}

		private void testBreakageCallback (IJoint j, DynamicObject o1, DynamicObject o2)
		{
			GuiElement element = GuiManager.Instance.GetElement("AxiomPhysics/StatsPanel/Stat2");
			element.Text = o2.Name + " breaks from " + o1.Name;
		}

		private DynamicObject setupTestSim1(SceneNode rootnode)
		{
			GuiElement element = GuiManager.Instance.GetElement("Core/DebugText");
			element.Text = "Test Sim #1: A 5 story building and arbitrary obstacles.";

			bullet = null;

			if (world != null)
				world.Shutdown(scene);

			world = new OdeWorld();

			world.Gravity = new Vector3(0, -490.0f, 0);

			world.CreateStaticBox (scene,
				"ground",							// Name
				"",									// Mesh
				"",									// Material
				new Vector3(0.0f, -2.5f, 0.0f),		// Position
				Quaternion.Identity,				// Orientation
				"",									// Surface
				new Vector3(2500.0f, 5.0f, 2500.0f),// Size
				true);								// Create a debug object


			world.CreateStaticSphere(scene,
				"obstacle1",
				"",
				"",
				new Vector3(450.0f, 250.0f, 0.0f),
				Quaternion.Identity,
				"",
				500,
				true);

			world.CreateStaticBox(scene,
				"obstacle2",
				"",
				"",
				new Vector3(-550.0f, 75.0f, -450.0f),
				Quaternion.Identity,
				"",
				new Vector3(500.0f, 150.0f, 750.0f),
				true);
			
			world.CreateStaticCapsule(scene,
				"obstacle3",
				"",
				"",
				new Vector3(-500.0f, 525.0f, 100.0f),
				Quaternion.Identity,
				"",
				50.0f, 1000.0f,
				true);

			// Construct a building
			constructBuilding("NormalBuilding",
						new Vector3(0.0f, 0.0f, 0.0f),
						new Vector3(50.0f, 90.0f, 50.0f),
						25.0f,
						"",
						new Vector3(300.0f,10.0f,300.0f),
						25.0f,
						"",
						5);

			bullet = world.CreateDynamicSphere(scene,
				"bullet",								// Name
				"sphere100.mesh",						// Mesh
				"Examples/RustySteel",							// Material
				new Vector3(1000.0f, 25.0f, 1000.0f),	// Position
				Quaternion.Identity,					// Orientation
				"",										// Surface
				50.0f,									// Diameter
				25.0f,									// Mass
				false);									// Create a debug object
			bullet.Scale (50.0f / 100.0f, 50.0f / 100.0f, 50.0f / 100.0f);

			// Prevent the bullet from rolling forever
			bullet.AngularDamping=0.0025f;	

			return bullet;
		}

		private DynamicObject setupTestSim2(SceneNode rootnode)
		{
			GuiElement element = GuiManager.Instance.GetElement("Core/DebugText");
			element.Text = "Test Sim #2: A 10 story building and arbitrary obstacles.";

			bullet = null;

			if (world != null)
				world.Shutdown(scene);

			world = new OdeWorld();

			world.Gravity = new Vector3(0, -490.0f, 0);

			world.CreateStaticBox (scene,
				"ground",							// Name
				"",									// Mesh
				"",									// Material
				new Vector3(0.0f, -2.5f, 0.0f),		// Position
				Quaternion.Identity,				// Orientation
				"",									// Surface
				new Vector3(2500.0f, 5.0f, 2500.0f),// Size
				true);								// Create a debug object


			world.CreateStaticBox(scene,
				"obstacle1",
				"",
				"",
				new Vector3(400.0f, 250.0f, 0.0f),
				Quaternion.Identity,
				"",
				new Vector3(50.0f, 500.0f, 500.0f),
				true);

			world.CreateStaticBox(scene,
				"obstacle2",
				"",
				"",
				new Vector3(-550.0f, 300.0f, -450.0f),
				Quaternion.Identity,
				"",
				new Vector3(500.0f, 150.0f, 750.0f),
				true);

			world.CreateStaticCapsule(scene,
				"obstacle3",
				"",
				"",
				new Vector3(-500.0f, 525.0f, 100.0f),
				Quaternion.Identity,
				"",
				50.0f, 1000.0f,
				true);
			
			bullet = world.CreateDynamicSphere(scene,
				"bullet",								// Name
				"",										// Mesh
				"",										// Material
				new Vector3(1000.0f, 25.0f, 1000.0f),	// Position
				Quaternion.Identity,					// Orientation
				"",										// Surface
				50.0f,									// Diameter
				25.0f,									// Mass
				true);									// Create a debug object

			constructBuilding("FatBuilding",
				new Vector3(0.0f, 0.0f, 0.0f),
				new Vector3(50.0f, 100.0f, 50.0f),
				25.0f,
				"",
				new Vector3(250.0f,50.0f,250.0f),
				25.0f,
				"",
				10);

			// Prevent the bullet from rolling forever
			bullet.AngularDamping=0.0025f;	

			return bullet;
		}

		private DynamicObject setupTestSim3(SceneNode rootnode)
		{
			GuiElement element = GuiManager.Instance.GetElement("Core/DebugText");
			element.Text = "Test Sim #3: An extremely thin 5 story building and arbitrary obstacles.";

			bullet = null;

			if (world != null)
				world.Shutdown(scene);

			world = new OdeWorld();

			world.Gravity = new Vector3(0, -490.0f, 0);

			world.CreateStaticBox (scene,
				"ground",							// Name
				"",									// Mesh
				"",									// Material
				new Vector3(0.0f, -2.5f, 0.0f),		// Position
				Quaternion.Identity,				// Orientation
				"",									// Surface
				new Vector3(2500.0f, 5.0f, 2500.0f),// Size
				true);								// Create a debug object


			// Create a few arbitrary obstacles
			world.CreateStaticBox(scene,
									"rearWall",
									"",
									"",
									new Vector3(0.0f, 75.0f, -75.0f),
									Quaternion.Identity,
									"",
									new Vector3(150.0f, 150.0f, 10.0f),
									true);
			world.CreateStaticBox(scene,
									"frontWall",
									"",
									"",
									new Vector3(0.0f, 15.0f, 75.0f),
									Quaternion.Identity,
									"",
									new Vector3(150.0f, 30.0f, 10.0f),
									true);
			world.CreateStaticBox(scene,
									"rightWall",
									"",
									"",
									new Vector3(75.0f, 150.0f, 0.0f),
									Quaternion.Identity,
									"",
									new Vector3(10.0f, 300.0f, 130.0f),
									true);
			world.CreateStaticBox(scene,
									"leftWall",
									"",
									"",
									new Vector3(-75.0f, 50.0f, 0.0f),
									Quaternion.Identity,
									"",
									new Vector3(10.0f, 100.0f, 130.0f),
									true);

			// Create the bullet
			bullet = world.CreateDynamicSphere(scene,
										"bullet",
										"",
										"",
										new Vector3(1000.0f, 25.0f, 1000.0f),
										Quaternion.Identity,
										"",
										50.0f,
										25.0f,
										true);
	
			// Construct a building (impossibly thin)
			constructBuilding("ThinBuilding",
						new Vector3(0.0f, 0.0f, 0.0f),
						new Vector3(5.0f, 200.0f, 5.0f),
						25.0f,
						"",
						new Vector3(100.0f, 10.0f, 100.0f),
						25.0f,
						"",
						5);

			// Prevent the bullet from rolling forever
			bullet.AngularDamping=0.0025f;	

			// Return the bullet
			return bullet;
		}

		private DynamicObject setupTestSim4(SceneNode rootnode)
		{
			GuiElement element = GuiManager.Instance.GetElement("Core/DebugText");
			element.Text = "Test Sim #4: Four, small 5 story buildings. May lag momentarily. :)";

			bullet = null;

			if (world != null)
				world.Shutdown(scene);

			world = new OdeWorld();

			world.Gravity = new Vector3(0, -490.0f, 0);

			// Create the ground
			world.CreateStaticBox(scene,
									"ground",
									"",
									"",
									new Vector3(0.0f, -2.5f, 0.0f),
									Quaternion.Identity,
									"",
									new Vector3(2500.0f, 5.0f, 2500.0f),
									true);

			// Create the bullet
			bullet = world.CreateDynamicSphere(scene,
												"bullet",
												"",
												"",
												new Vector3(1000.0f, 25.0f, 1000.0f),
												Quaternion.Identity,
												"",
												50.0f,
												25.0f,
												true);
	
			// Prevent the bullet from rolling forever
			bullet.AngularDamping=0.0025f;	

			// Construct a building (small)
			constructBuilding("SmallBuilding1",
								new Vector3(-200.0f, 0.0f, -200.0f),
								new Vector3(25.0f, 50.0f, 25.0f),
								25.0f,
								"",
								new Vector3(200.0f, 25.0f, 200.0f),
								25.0f,
								"",
								5);

	
			// Construct a building (small)
			constructBuilding("SmallBuilding2",
								new Vector3(200.0f, 0.0f, -200.0f),
								new Vector3(25.0f, 50.0f, 25.0f),
								25.0f,
								"",
								new Vector3(200.0f, 25.0f, 200.0f),
								25.0f,
								"",
								5);
	
			// Construct a building (small)
			constructBuilding("SmallBuilding3",
								new Vector3(-200.0f, 0.0f, 200.0f),
								new Vector3(25.0f, 50.0f, 25.0f),
								25.0f,
								"",
								new Vector3(200.0f, 25.0f, 200.0f),
								25.0f,
								"",
								5);
	
			// Construct a building (small)
			constructBuilding("SmallBuilding4",
								new Vector3(200.0f, 0.0f, 200.0f),
								new Vector3(25.0f, 50.0f, 25.0f),
								25.0f,
								"",
								new Vector3(200.0f, 25.0f, 200.0f),
								25.0f,
								"",
								5);

			// Return the bullet
			return bullet;
		}

		private DynamicObject setupTestSim5(SceneNode rootnode)
		{
			GuiElement element = GuiManager.Instance.GetElement("Core/DebugText");
			element.Text = "Test Sim #5: A 10 box, ball and socket, jointed chain.";

			bullet = null;

			if (world != null)
				world.Shutdown(scene);

			world = new OdeWorld();

			world.Gravity = new Vector3(0, -490.0f, 0);

			// Create the ground
			world.CreateStaticBox(scene,
									"ground",
									"",
									"",
									new Vector3(0.0f, -2.5f, 0.0f),
									Quaternion.Identity,
									"",
									new Vector3(2500.0f, 5.0f, 2500.0f),
									true);
	
			// Create the bullet
			bullet = world.CreateDynamicSphere(scene,
												"bullet",
												"",
												"",
												new Vector3(1000.0f, 25.0f, 1000.0f),
												Quaternion.Identity,	
												"",
												50.0f,
												25.0f,
												true);
	
			// Prevent the bullet from rolling forever
			bullet.AngularDamping=0.0025f;	

			// Resusable pointers
			DynamicObject newBox = null;
			DynamicObject lastBox = null;

			// Create the first box
			lastBox = world.CreateDynamicBox(scene,
												"box0",
												"",
												"",
												new Vector3(0.0f, 25.0f + 50.0f, 0.0f),
												Quaternion.Identity,
												"",
												new Vector3(49.0f, 49.0f, 49.0f),
												1.0f,
												true);

			lastBox.AngularDamping=0.0025f;
			lastBox.LinearDamping=0.0025f;

			// Create the other 9 boxes
			for(int i = 1; i < 10; i++)
			{
				// Create the next box in the chain
				newBox = world.CreateDynamicBox(scene,
												"box" + i.ToString(),
												"",
												"",
												new Vector3(0.0f, (i * 50.0f) + 25.0f + 50.0f, 0.0f),
												Quaternion.Identity,
												"",
												new Vector3(49.0f, 49.0f, 49.0f),
												1.0f,
												true);

				lastBox.AngularDamping=0.0025f;
				lastBox.LinearDamping=0.0025f;

				// Links the boxes together
				world.CreateBallSocket(scene,
										"joint" + i.ToString(),
										lastBox, newBox,
										new Vector3(0.0f, (i * 50.0f) + 50.0f, 0.0f),
										Quaternion.Identity);

				// Set the last box for the next joint
				lastBox = newBox;
			}

			// Links the last box to the world
			world.CreateBallSocket(scene,
									"jointToWorld",
									lastBox,
									null,
									new Vector3(0.0f, ((10 * 50.0f) + 25.0f + 50.0f), 0.0f),
									Quaternion.Identity);
	
			// Return the bullet
			return bullet;
		}

		private DynamicObject setupTestSim6(SceneNode rootnode)
		{
			GuiElement element = GuiManager.Instance.GetElement("Core/DebugText");
			element.Text = "Test Sim #6: A 10 box, Hinge jointed chain, with limiters.";

			bullet = null;

			if (world != null)
				world.Shutdown(scene);

			world = new OdeWorld();

			world.Gravity = new Vector3(0, -490.0f, 0);

			// Create the ground
			world.CreateStaticBox(scene,
									"ground",
									"",
									"",
									new Vector3(0.0f, -2.5f, 0.0f),
									Quaternion.Identity,
									"",
									new Vector3(2500.0f, 5.0f, 2500.0f),
									true);
	

			// Create the bullet
			bullet = world.CreateDynamicSphere(scene,
												"bullet",
												"",
												"",
												new Vector3(1000.0f, 25.0f, 1000.0f),
												Quaternion.Identity,
												"",
												50.0f,
												25.0f,
												true);
	
			// Prevent the bullet from rolling forever
			bullet.AngularDamping=0.0025f;	

			// Resusable pointers
			DynamicObject newBox = null;
			DynamicObject lastBox = null;

			IJoint joint;

			// Get a quaternion for the rotation of the joint
			Matrix3 angleMat = new Matrix3();
			angleMat.FromEulerAnglesXYZ(0.0f, 0.0f, (float)(Math.PI / 2.0f));
			Quaternion angleQuat = new Quaternion();
			angleQuat.FromRotationMatrix(angleMat);

			// Create the first box
			lastBox = world.CreateDynamicBox(scene,
									"box0",
									"",
									"",
									new Vector3(0.0f, 25.0f + 50.0f, 0.0f),
									Quaternion.Identity,
									"",
									new Vector3(49.0f, 49.0f, 49.0f),
									1.0f,
									true);

			lastBox.AngularDamping=0.0025f;
			lastBox.LinearDamping=0.0025f;

			// Create the other 9 boxes
			for(int i = 1; i < 10; i++)
			{
				// Create the next box in the chain
				newBox = world.CreateDynamicBox(scene,
												"box" + i.ToString(),
												"",
												"",
												new Vector3(0.0f, (i * 50.0f) + 25.0f + 50.0f, 0.0f),
												Quaternion.Identity,
												"",
												new Vector3(49.0f, 49.0f, 49.0f),
												1.0f,
												true);

				lastBox.AngularDamping=0.0025f;
				lastBox.LinearDamping=0.0025f;

				// Links the boxes together
				joint = world.CreateHinge(scene,
											"joint" + i.ToString(),
											lastBox, newBox,
											50.0f,
											new Vector3(0.0f, (i * 50.0f) + 50.0f, 0.0f),
											angleQuat);

				// Limit the motion of the joint
				//joint.setLowerLimit(-Math.PI/8);
				//joint.setUpperLimit(Math.PI/8);
				//joint.setLimitEnabled(true);

				// Set the last box for the next joint
				lastBox = newBox;
			}
	
			// Links the last box to the world
			world.CreateBallSocket(scene,
									"jointToWorld",
									lastBox,
									null,
									new Vector3(0.0f, ((10 * 50.0f) + 25.0f + 50.0f), 0.0f),
									Quaternion.Identity);
	
			// Return the bullet
			return bullet;
		}

		private DynamicObject setupTestSim7(SceneNode rootnode)
		{
			const int breakimpulse = 15000;

			GuiElement element = GuiManager.Instance.GetElement("Core/DebugText");
			element.Text = "Test Sim #7: Not yet implemented ....";

			element = GuiManager.Instance.GetElement("AxiomPhysics/StatsPanel/Stat2");
			element.Text = "";

			bullet = null;

			if (world != null)
				world.Shutdown(scene);

			world = new OdeWorld();

			world.Gravity = new Vector3(0, -490.0f, 0);

			// Enable breakage callbacks
			//Simulation::getSingleton().setBreakageCallback(testBreakageCallback);

			// Create the ground
			world.CreateStaticBox(scene,
									"ground",
									"",
									"",
									new Vector3(0.0f, -2.5f, 0.0f),
									Quaternion.Identity,
									"",
									new Vector3(2500.0f, 5.0f, 2500.0f),
									true);
	
			// Add a few Elements to the complex
			DynamicObject box1 = world.CreateDynamicBox (scene,
									"box1",
									"",
									"",
									new Vector3(-50.0f, 75.0f, 0.0f),
									Quaternion.Identity,
									"",
									new Vector3(50.0f, 150.0f, 50.0f),
									25f,
									true);

			DynamicObject box2 = world.CreateDynamicBox (scene,
									"box2",
									"",
									"",
									new Vector3(0.0f, 125.0f, 0.0f),
									Quaternion.Identity,
									"",
									new Vector3(50.0f, 50.0f, 50.0f),
									25f,
									true);

			IJoint joint1 = world.CreateFixed (scene, "joint", box1, box2);
			joint1.SetBreakForce2 (breakimpulse);
			joint1.BreakageCallback = new BreakageCallbackFunction(testBreakageCallback);

			DynamicObject box3 = world.CreateDynamicBox (scene,
									"box3",
									"",
									"",
									new Vector3(0.0f, 175.0f, 0.0f),
									Quaternion.Identity,
									"",
									new Vector3(50.0f, 50.0f, 50.0f),
									25f,
									true);

			IJoint joint2 = world.CreateFixed (scene, "joint", box1, box3);
			joint2.SetBreakForce2(breakimpulse);
			joint2.BreakageCallback = new BreakageCallbackFunction(testBreakageCallback);

			DynamicObject box4 = world.CreateDynamicBox (scene, 
									"box4",
									"",
									"",
									new Vector3(-50.0f, 175.0f, 0.0f),
									Quaternion.Identity,
									"",
									new Vector3(50.0f, 50.0f, 50.0f),
									25,
									true);

			IJoint joint3 = world.CreateFixed (scene, "joint", box1, box4);
			joint3.SetBreakForce2(breakimpulse);
			joint3.BreakageCallback = new BreakageCallbackFunction(testBreakageCallback);

			DynamicObject box5 = world.CreateDynamicBox (scene,
									"box5",
									"",
									"",
									new Vector3(-100.0f, 175.0f, 0.0f),
									Quaternion.Identity,
									"",
									new Vector3(50.0f, 50.0f, 50.0f),
									25f,
									true);

			IJoint joint4 = world.CreateFixed (scene, "joint", box1, box5);
			joint4.SetBreakForce2(breakimpulse);
			joint4.BreakageCallback = new BreakageCallbackFunction(testBreakageCallback);

			DynamicObject box6 = world.CreateDynamicBox (scene,
									"box6",
									"",
									"",
									new Vector3(-100.0f, 125.0f, 0.0f),
									Quaternion.Identity,
									"",
									new Vector3(50.0f, 50.0f, 50.0f),
									25f,
									true);

			IJoint joint5 = world.CreateFixed (scene, "joint", box1, box6);
			joint5.SetBreakForce2(breakimpulse);
			joint5.BreakageCallback = new BreakageCallbackFunction(testBreakageCallback);

			// Create the bullet (with exceptional mass)
			bullet = world.CreateDynamicSphere(scene,
				"bullet",
				"",
				"",
				new Vector3(1000.0f, 25.0f, 1000.0f),
				Quaternion.Identity,
				"",
				50.0f,
				25.0f,
				true);
	
			// Prevent the bullet from rolling forever
			bullet.AngularDamping=0.0025f;	

			// Return the bullet
			return bullet;
		}

		private DynamicObject setupTestSim8(SceneNode rootnode)
		{
			GuiElement element = GuiManager.Instance.GetElement("Core/DebugText");
			element.Text = "Test Sim #8: Twenty-five stacks of five blocks each, with a heavy bullet.";

			bullet = null;

			if (world != null)
				world.Shutdown(scene);

			world = new OdeWorld();

			world.Gravity = new Vector3(0, -490.0f, 0);

			// Create the ground
			world.CreateStaticBox(scene,
									"ground",
									"",
									"",
									new Vector3(0.0f, -2.5f, 0.0f),
									Quaternion.Identity,
									"",
									new Vector3(2500.0f, 5.0f, 2500.0f),
									true);

			// Create a new surface for the blocks
			world.createSurface("slipperyBrick", 0.1f, 0.0f);
	
			// Row sizes
			const int numXRows = 5;
			const int numZRows = 5;
			const int numYRows = 5;

			// Loop through X rows
			for(int x = 0; x < numXRows; x++)
			{
				// Loop through Z rows
				for(int z = 0; z < numZRows; z++)
				{
					// Loop through Y rows (stacks)
					for(int y = 0; y < numYRows; y++)
					{
						// Create a box
						world.CreateDynamicBox(scene,
							"box_" + x.ToString() + "_" + z.ToString() + "_" + y.ToString(),
							"",
							"",
							new Vector3((x * 50.0f * 2.0f) - (numXRows / 2 * 50.0f * 2.0f), (y * 50.0f) + 25.0f, (z * 50.0f * 2.0f) - (numZRows / 2 * 50.0f * 2.0f)),
							Quaternion.Identity,
							"slipperyBrick",
							new Vector3(50.0f, 50.0f, 50.0f),
							25.0f,
							true);
					}
				}
			}

			// Create the bullet (with exceptional mass)
			bullet = world.CreateDynamicSphere(scene,
													"bullet",
													"",
													"",
													new Vector3(1000.0f, 25.0f, 1000.0f),
													Quaternion.Identity,
													"",
													50.0f,
													1000.0f,
													true);
	
			// Prevent the bullet from rolling forever
			bullet.AngularDamping=0.0025f;	

			// Return the bullet
			return bullet;
		}

		private DynamicObject setupTestSim9(SceneNode rootnode)
		{
			GuiElement element = GuiManager.Instance.GetElement("Core/DebugText");
			element.Text = "Test Sim #9: TriMesh collision! Two 5000 poly TriMeshes and 25 blocks.";

			bullet = null;

			if (world != null)
				world.Shutdown(scene);

			world = new OdeWorld();

			world.Gravity = new Vector3(0, -490.0f, 0);

			// Create a new surface for the scaley terrain
			world.createSurface("scaley", 0.1f, 0.5f);
			world.createSurface("rocky", 0.7f, 0.2f);

			// Manually load the terrain mesh without the WRITE_ONLY flag, for direct access
			//MeshManager::getSingleton().load("terrain.mesh", HardwareBuffer::HBU_STATIC, HardwareBuffer::HBU_STATIC, false, false);
	
			// Rotate one of the TriMeshes
			Matrix3 angleMat = new Matrix3();
			angleMat.FromEulerAnglesXYZ(0.0f, (float)(Math.PI/4), 0.0f);
			Quaternion angleQuat = new Quaternion();
			angleQuat.FromRotationMatrix(angleMat);

			// Create the TriMeshes
			world.CreateTriMesh(scene,
								"terrain",
								"terrain.mesh",
								"AxiomPhysics/Scales",
								new Vector3(500.0f, -500.0f, 0.0f),
								Quaternion.Identity,
								"scaley");
			world.CreateTriMesh(scene,
								"terrain2",												
								"terrain.mesh",
								"AxiomPhysics/Rocky",
								new Vector3(-400.0f, -750.0f, -200.0f),
								angleQuat,
								"rocky");
	
			// Create a few extra obstacles
			world.CreateStaticBox(scene,
									"obstacle1",
									"",
									"",
									new Vector3(-250.0f, -500.0f, -250.0f),
									Quaternion.Identity,
									"",
									new Vector3(100.0f, 500.0f, 100.0f),
									true);
			world.CreateStaticBox(scene,
									"obstacle2",
									"",
									"",
									new Vector3(-250.0f, -500.0f, 250.0f),
									Quaternion.Identity,
									"",
									new Vector3(100.0f, 500.0f, 100.0f),
									true);
			world.CreateStaticBox(scene,
									"obstacle3",
									"",
									"",
									new Vector3(250.0f, -500.0f, 250.0f),
									Quaternion.Identity,
									"",
									new Vector3(100.0f, 500.0f, 100.0f),
									true);
			world.CreateStaticBox(scene,
									"obstacle4",
									"",
									"",
									new Vector3(250.0f, -500.0f, -250.0f),
									Quaternion.Identity,
									"",
									new Vector3(100.0f, 500.0f, 100.0f),
									true);
			world.CreateStaticBox(scene,
									"obstacle5",
									"",
									"",
									new Vector3(0.0f, -500.0f, 0.0f),
									Quaternion.Identity,
									"",
									new Vector3(100.0f, 500.0f, 100.0f),
									true);

			// Row sizes
			const int numXRows = 5;
			const int numZRows = 5;
			const int numYRows = 1;

			// Total number of objects created
			int totalCount = 0;

			// The object being created
			DynamicObject temp = null;

			// Loop through X rows
			for(int x = 0; x < numXRows; x++)
			{
				// Loop through Z rows
				for(int z = 0; z < numZRows; z++)
				{
					// Loop through Y rows (stacks)
					for(int y = 0; y < numYRows; y++)
					{
						// First object is a box
						if(totalCount % 3 == 0)
						{
							// Create a box
							temp = world.CreateDynamicBox(scene,
															"box_" + x.ToString() + "_" + z.ToString() + "_" + y.ToString(),
															"",
															"",
															new Vector3((x * 50.0f * 2.0f) - (numXRows / 2 * 50.0f * 2.0f), (y * 50.0f) + 25.0f, (z * 50.0f * 2.0f) - (numZRows / 2 * 50.0f * 2.0f)),
															Quaternion.Identity,
															"",
															new Vector3(50.0f, 50.0f, 50.0f),
															25.0f,
															true);
						}
						// Second object is a sphere
						else if(totalCount % 3 == 1)
						{
							// Create a box
							temp = world.CreateDynamicSphere(scene,
															"sphere_" + x.ToString() + "_" + z.ToString() + "_" + y.ToString(),
															"",
															"",
															new Vector3((x * 50.0f * 2.0f) - (numXRows / 2 * 50.0f * 2.0f), (y * 50.0f) + 25.0f, (z * 50.0f * 2.0f) - (numZRows / 2 * 50.0f * 2.0f)),
															Quaternion.Identity,
															"",
															50.0f,
															25.0f,
															true);
						}
						// Third object is a cylinder
						else if(totalCount % 3 == 2)
						{
							//Create a capsule
							temp = world.CreateDynamicCapsule(scene,
															"cylinder_" + x.ToString() + "_" + z.ToString() + "_" + y.ToString(),
															"",
															"",
															new Vector3((x * 50.0f * 2.0f) - (numXRows / 2 * 50.0f * 2.0f), (y * 150.0f) + 25.0f, (z * 50.0f * 2.0f) - (numZRows / 2 * 50.0f * 2.0f)),
															Quaternion.Identity,
															"",
															50.0f, 100.0f,
															25.0f,
															true);
						}

						// Apply some damping (so the objects will come to a complete stop)
						temp.AngularDamping=0.0025f;
						temp.LinearDamping=0.0025f;

						// Total number of objects created
						totalCount++;
					}
				}
			}

			// Create the bullet
			bullet = world.CreateDynamicSphere(scene,
												"bullet",
												"",
												"",
												new Vector3(1000.0f, 25.0f, 1000.0f),
												Quaternion.Identity,
												"",
												50.0f,
												25.0f,
												true);
	
			// Prevent the bullet from rolling forever
			bullet.AngularDamping=0.0025f;	

			// Return the bullet
			return bullet;
		}

		private DynamicObject setupTestSim0(SceneNode rootnode)
		{
			GuiElement element = GuiManager.Instance.GetElement("Core/DebugText");
			element.Text = "Test Sim #0: Ragdolls! Based on code by KayNine. :)";

			bullet = null;

			if (world != null)
				world.Shutdown(scene);

			world = new OdeWorld();

			world.Gravity = new Vector3(0, -980.0f, 0);

			// Create the TriMesh
			world.CreateTriMesh(scene,
								"terrain",
								"terrain.mesh",
								"AxiomPhysics/Scales",
								new Vector3(0.0f, -600.0f, -750.0f),
								Quaternion.Identity,
								"");
	
			// Create the steps
			for(int i = 0; i < 10; i++)
			{
				world.CreateStaticBox(scene,
										"step" + i.ToString(),
										"",
										"AxiomPhysics/Rocky",
										new Vector3(0.0f, (-280.0f + (i * 50.0f)), (-1000.0f - (i * 40.0f))),
										Quaternion.Identity,
										"",
										new Vector3(1000.0f, 40.0f, 80.0f),
										true);
			}

			// Create ragdolls
			Random r = new Random();
			for(int i = 0; i < 5; i++)
			{
				constructRagDoll("doll_" + i.ToString(),
							new Vector3((float)r.Next(-250, 250), (500.0f + (i * 250.0f)), -1250.0f),
							40.0f,
							8.0f,
							4);
			}

			// Pin the first doll up by his arm/hand
			IJoint pin = world.CreateBallSocket(scene,
												"pin",
												world.GetDynamicObject("doll_0_leftForearm"),
												null,
												world.GetDynamicObject("doll_0_leftForearm").Position,
												Quaternion.Identity);
	
			// Create the bullet
			bullet = world.CreateDynamicSphere(scene,
											"bullet",
											"",
											"AxiomPhysics/Scales",
											new Vector3(1000.0f, 25.0f, 1000.0f),
											Quaternion.Identity,
											"",
											50.0f,
											1000.0f,
											true);
   
			// Prevent the bullet from rolling forever
			bullet.AngularDamping=0.0025f;	

			// Return the bullet
			return bullet;
		}

		private void constructBuilding(string name,	Vector3 basePosition, Vector3 cornerSize, float cornerMass, string cornerSurface, Vector3 floorSize, float floorMass, string floorSurface, int numFloors)
		{
			// Temporary box object
			DynamicObject tempBox;

			// Compute coeffecients once
			float negX = basePosition.x + (cornerSize.x / 2) - (floorSize.x / 2);
			float posX = basePosition.x + (floorSize.x / 2) - (cornerSize.x / 2);
			float negZ = basePosition.z + (cornerSize.z / 2) - (floorSize.z / 2);
			float posZ = basePosition.z + (floorSize.z / 2) - (cornerSize.z / 2);
			float cornerOffset = cornerSize.y / 2;
			float floorHeight = cornerSize.y + floorSize.y;
			float floorOffset = floorSize.y / 2;

			// Loop through number of floors
			for(int floor = 0; floor < numFloors; floor++)
			{
				// Create the center pillar
				tempBox = world.CreateDynamicBox(scene, 
					name + "_Floor_" + floor.ToString() + "_CenterPillar",
					"", 
					"", 
					new Vector3(basePosition.x, basePosition.y + cornerOffset + (floor * floorHeight), basePosition.z),
					Quaternion.Identity,
					cornerSurface,
					cornerSize,
					cornerMass,
					true);

				// Create the NegativeX/NegativeZ corner
				tempBox = world.CreateDynamicBox(scene,
					name + "_Floor_" + floor.ToString() + "_Corner_NegXNegZ",
					"", 
					"", 
					new Vector3(negX, basePosition.y + cornerOffset + (floor * floorHeight), negZ),
					Quaternion.Identity,
					cornerSurface,
					cornerSize,
					cornerMass,
					true);
		
				// Create the PositiveX/NegativeZ corner
				tempBox = world.CreateDynamicBox(scene,
					name + "_Floor_" + floor.ToString() + "_Corner_PosXNegZ",
					"", 
					"", 
					new Vector3(posX, basePosition.y + cornerOffset + (floor * floorHeight), negZ),
					Quaternion.Identity,
					cornerSurface,
					cornerSize,
					cornerMass,
					true);
		
				// Create the NegativeX/PositiveZ corner
				tempBox = world.CreateDynamicBox(scene, 
					name + "_Floor_" + floor.ToString()+ "_Corner_NegXPosZ",
					"", 
					"", 
					new Vector3(negX, basePosition.y + cornerOffset + (floor * floorHeight), posZ),
					Quaternion.Identity,
					cornerSurface,
					cornerSize,
					cornerMass,
					true);
		
				// Create the PositiveX/PositiveZ corner
				tempBox = world.CreateDynamicBox(scene,
					name + "_Floor_" + floor.ToString() + "_Corner_PosXPosZ",
					"", 
					"", 
					new Vector3(posX, basePosition.y + cornerOffset + (floor * floorHeight), posZ),
					Quaternion.Identity,
					cornerSurface,
					cornerSize,
					cornerMass,
					true);
		
				// Create the floor
				tempBox = world.CreateDynamicBox(scene,
					name + "_Floor_" + floor.ToString(),
					"", 
					"", 
					new Vector3(basePosition.x, basePosition.y + floorOffset + (floor * (floorHeight)) + cornerSize.y, basePosition.z),
					Quaternion.Identity,
					floorSurface,
					floorSize,
					floorMass,
					true);
			}

		}

		private void constructRagDoll(String name,Vector3 basePosition, float bodyMass, float limbMass, int jointIterations)
		{
			// Quaternion to rotate the bodyparts/joints
			Quaternion rotation = new Quaternion();

			// Create the body parts
			//====================================================================================

			// Body
			DynamicObject body = world.CreateDynamicBox(scene,
			name + "_body",
			"",
			"",
			basePosition + new Vector3(0.0f, 0.0f, 0.0f),
			Quaternion.Identity,
			"",
			new Vector3(55.0f, 70.0f, 30.0f),
			bodyMass,
			true);

			// Head
			DynamicObject head = world.CreateDynamicSphere(scene,
			name + "_head",
			"",
			"",
			basePosition + new Vector3(0.0f, 55.0f, 0.0f),
			Quaternion.Identity,
			"",
			30.0f,
			limbMass,
			true);

			// Right Arm
			rotation = Quaternion.FromAngleAxis((float)(-Math.PI / 2.0f), new Vector3(0.0f, 0.0f, 1.0f));
			DynamicObject rightArm = world.CreateDynamicCapsule(scene,
			name + "_rightArm",
			"",
			"",
			basePosition + new Vector3(-45.0f, 28.0f, 0.0f),
			rotation,
			"",
			25.0f, 30.0f,
			limbMass,
			true);

			// Left Arm
			rotation = Quaternion.FromAngleAxis((float)(Math.PI / 2.0f), new Vector3(0.0f, 0.0f, 1.0f));
			DynamicObject leftArm = world.CreateDynamicCapsule(scene,
			name + "_leftArm",
			"",
			"",
			basePosition + new Vector3(45.0f, 28.0f, 0.0f),
			rotation,
			"",
			25.0f, 30.0f,
			limbMass,
			true);

			// Right Forearm
			rotation = Quaternion.FromAngleAxis((float)(-Math.PI / 2.0f), new Vector3(0.0f, 0.0f, 1.0f));
			DynamicObject rightForearm = world.CreateDynamicBox(scene,
			name + "_rightForearm",
			"",
			"",
			basePosition + new Vector3(-90.0f, 28.0f, 0.0f),
			rotation,
			"",
			new Vector3(24.0f, 60.0f, 24.0f),
			limbMass,
			true);

			// Left Forearm
			rotation = Quaternion.FromAngleAxis((float)(Math.PI / 2.0f), new Vector3(0.0f, 0.0f, 1.0f));
			DynamicObject leftForearm = world.CreateDynamicBox(scene,
			name + "_leftForearm",   
			"",                  
			"",                  
			basePosition + new Vector3(90.0f, 28.0f, 0.0f),   
			rotation,           
			"",
			new Vector3(24.0f, 60.0f, 24.0f),
			limbMass,
			true);

			// Right Thigh
			DynamicObject rightThigh = world.CreateDynamicCapsule(scene,
			name + "_rightThigh",
			"",
			"",
			basePosition + new Vector3(-20.0f, -60.0f, 0.0f),
			Quaternion.Identity,
			"",
			27.0f, 50.0f,
			limbMass,
			true);

			// Left Thigh
			DynamicObject leftThigh = world.CreateDynamicCapsule(scene,
			name + "leftThigh",
			"",
			"",
			basePosition + new Vector3(20.0f, -60.0f, 0.0f),
			Quaternion.Identity,
			"",
			27.0f, 50.0f,
			limbMass,
			true);

			// Right Leg
			DynamicObject rightLeg = world.CreateDynamicBox(scene,
			name + "_rightLeg",
			"",
			"",
			basePosition + new Vector3(-20.0f, -130.0f, 0.0f),
			Quaternion.Identity,
			"",
			new Vector3(30.0f, 80.0f, 30.0f),
			8.0f,
			true);

			// Left Left
			DynamicObject leftLeg = world.CreateDynamicBox(scene,
			name + "_leftLeg",
			"",
			"",
			basePosition + new Vector3(20.0f, -130.0f, 0.0f),
			Quaternion.Identity,
			"",
			new Vector3(30.0f, 80.0f, 30.0f),
			8.0f,
			true);


			// Create the Joints
			//====================================================================================

			// Head - Body joint
			IJoint bodyHeadJoint = world.CreateHinge(scene,
			name + "_BodyHeadJoint",
			body, head,
			10.0f,
			basePosition + new Vector3(0.0f, 35.0f, 0.0f),
			Quaternion.Identity);

			// Right Arm - Body joint
			IJoint rightArmBodyJoint = world.CreateBallSocket(scene,
			name + "_RightArmBodyJoint",
			rightArm,
			body,
			basePosition + new Vector3(-22.0f, 28.0f, 0.0f),
			Quaternion.Identity);

			// Left Arm - Body joint
			IJoint leftArmBodyJoint = world.CreateBallSocket(scene,
			name + "_LeftArmBodyJoint",
			leftArm,
			body,
			basePosition + new Vector3(22.0f, 28.0f, 0.0f),
			Quaternion.Identity);

			// Right Forearm - Arm Joint
			rotation.FromAxes(new Vector3(-1.0f, 0.0f, 0.0f), new Vector3(0.0f, -1.0f, 0.0f), new Vector3(0.0f, 0.0f, 1.0f));
			IJoint rightForearmArmJoint = world.CreateHinge(scene,
			name + "_RightForearmArmJoint",
			rightArm,
			rightForearm,
			10.0f,
			basePosition + new Vector3(-65.0f, 28.0f, 0.0f),
			rotation);

			// Left Forearm - Arm Joint
			rotation.FromAxes(new Vector3(-1.0f, 0.0f, 0.0f), new Vector3(0.0f, -1.0f, 0.0f), new Vector3(0.0f, 0.0f, 1.0f));
			IJoint leftForearmArmJoint = world.CreateHinge(scene,
			name + "_LeftForearmArmJoint",
			leftForearm,
			leftArm,
			10.0f,
			basePosition + new Vector3(65.0f, 28.0f, 0.0f),
			rotation);

			// Right Thigh - Body Joint
			rotation = Quaternion.FromAngleAxis((float)(Math.PI * 6.0f / 8.0f), new Vector3(0.0f, 0.0f, 1.0f));
			IJoint rightThighBodyJoint = world.CreateBallSocket(scene,
			name + "_RightThighBodyJoint",
			rightThigh,
			body,
			basePosition + new Vector3(-20.0f, -32.0f, 0.0f),
			rotation);

			// Left Thigh - Body Joint
			rotation = Quaternion.FromAngleAxis((float)(Math.PI * 6.0f / 8.0f), new Vector3(0.0f, 0.0f, 1.0f));
			IJoint leftThighBodyJoint = world.CreateBallSocket(scene,
			name + "_LeftThighBodyJoint",
			leftThigh,
			body,
			basePosition + new Vector3(20.0f, -32.0f, 0.0f),
			Quaternion.Identity);

			// Right Leg - Thigh Joint
			rotation = Quaternion.FromAngleAxis((float)(-Math.PI * 0.5f), new Vector3(0.0f, 0.0f, 1.0f));
			IJoint rightLegThighJoint = world.CreateHinge(scene,
			name + "_RightLegThighJoint",
			rightLeg,
			rightThigh,
			15.0f,
			basePosition + new Vector3(-20.0f, -95.0f, 0.0f),
			rotation);

			// Left Leg - Thigh Joint
			rotation = Quaternion.FromAngleAxis((float)(-Math.PI * 0.5f), new Vector3(0.0f, 0.0f, 1.0f));
			IJoint leftLegThighJoint = world.CreateHinge(scene,
			name + "_LeftLegThighJoint",
			leftLeg,
			leftThigh,
			15.0f,
			basePosition + new Vector3(20.0f, -95.0f, 0.0f),
			rotation);
		}
	
		protected override void OnFrameStarted(object source, FrameEventArgs e) 
		{
			if (timeUntilNextAction > 0)
				timeUntilNextAction -= e.TimeSinceLastFrame;

			if (!ProcessUnbufferedKeyInput(e))
				return;

			if (!ProcessUnbufferedMouseInput())
				return;

			base.OnFrameStarted (source, e);

			world.Update(e.TimeSinceLastFrame * physicsSpeed);
		}

		private bool ProcessUnbufferedMouseInput()
		{
			if(input.IsMousePressed(MouseButtons.Button0) && (timeUntilNextAction <= 0.0f))
			{
				Debug.WriteLine ("Mouse !");

				bullet.RigidBody.Position = camera.Position;
				bullet.RigidBody.LinearVelocity = camera.Direction * 2000;
				bullet.RigidBody.AngularVelocity = new Vector3(0,0,0);

				timeUntilNextAction = 0.25f;
			}

			return true;
		}

		private bool ProcessUnbufferedKeyInput(FrameEventArgs e)
		{
			// TODO: Move this into an event queueing mechanism that is processed every frame
			input.Capture();

			if (timeUntilNextAction <= 0.0f)
			{
				if(input.IsKeyPressed(KeyCodes.Escape)) 
				{
					// we need the shutdown and a new construction
					// to get a graceful ode shutdown
					if (world != null)
						world.Shutdown(scene);
					world = new OdeWorld();

					e.RequestShutdown = true;               
						
					return true;
				}

				// Key F1 is down and it has been sufficient time since the last action
				if (input.IsKeyPressed(KeyCodes.F1))
				{
					// Decrease bullet mass
					if(bullet.RigidBody.Mass >= 10.0f)
					{
						
						bullet.RigidBody.Mass -= 5.0f;
					}

					// Wait at least 0.1 seconds to perform another mode action
					timeUntilNextAction = 0.1f;
				}

				// Key F2 is down and it has been sufficient time since the last action
				if (input.IsKeyPressed(KeyCodes.F2))
				{
					// Increase bullet mass
					bullet.RigidBody.Mass += 5.0f;

					// Wait at least 0.1 seconds to perform another mode action
					timeUntilNextAction = 0.1f;
				}

				// Key F3 is down and it has been sufficient time since the last action
				if (input.IsKeyPressed(KeyCodes.F3))
				{
					// Decrease the speed of the Simulation
					if(physicsSpeed >= 0.4f)
					{
						physicsSpeed -= 0.2f;
					}

					// Wait at least 0.25 seconds to perform another mode action
					timeUntilNextAction = 0.25f;
				}

				// Key F4 is down and it has been sufficient time since the last action
				if (input.IsKeyPressed(KeyCodes.F4))
				{
					// Increase the speed of the Simulation
					if(physicsSpeed <= 4.8f)
					{
						physicsSpeed += 0.2f;
					}

					// Wait at least 0.25 seconds to perform another mode action
					timeUntilNextAction = 0.25f;
				}

				// Key F5 is to toggle shadows
				if (input.IsKeyPressed(KeyCodes.F5))
				{
					if (scene.ShadowTechnique == ShadowTechnique.None)
						scene.ShadowTechnique = ShadowTechnique.StencilModulative;
					else
						scene.ShadowTechnique = ShadowTechnique.None;

					// Wait at least 0.25 seconds to perform another mode action
					timeUntilNextAction = 0.25f;
				}

				// Key F6 is down and it has been sufficient time since the last action
				if (input.IsKeyPressed(KeyCodes.F6))
				{
					// Decrease the gravity (or increase) ...
					Vector3 gravity = world.Gravity;
					gravity -= new Vector3(0.0f, 10.0f, 0.0f);
					world.Gravity = gravity;

					// Wait at least 0.25 seconds to perform another mode action
					timeUntilNextAction = 0.25f;
				}

				// Key F7 is down and it has been sufficient time since the last action
				if (input.IsKeyPressed(KeyCodes.F7))
				{
					// Increase the gravity (or decrease) ...
					Vector3 gravity = world.Gravity;
					gravity += new Vector3(0.0f, 10.0f, 0.0f);
					world.Gravity = gravity;

					// Wait at least 0.25 seconds to perform another mode action
					timeUntilNextAction = 0.25f;
				}

				// Key '1' is down and it has been sufficient time since the last action
				if (input.IsKeyPressed(KeyCodes.D1))
				{
					// Setup test sim #1
					bullet = setupTestSim1(scene.RootSceneNode);

					// Wait at least 0.25 seconds to perform another action
					timeUntilNextAction = 0.25f;
				}

				// Key '2' is down and it has been sufficient time since the last action
				if (input.IsKeyPressed(KeyCodes.D2))
				{
					// Setup test sim #2
					bullet = setupTestSim2(scene.RootSceneNode);

					// Wait at least 0.25 seconds to perform another action
					timeUntilNextAction = 0.25f;
				}

				// Key '3' is down and it has been sufficient time since the last action
				if (input.IsKeyPressed(KeyCodes.D3))
				{
					// Setup test sim #3
					bullet = setupTestSim3(scene.RootSceneNode);

					// Wait at least 0.25 seconds to perform another action
					timeUntilNextAction = 0.25f;
				}

				// Key '4' is down and it has been sufficient time since the last action
				if (input.IsKeyPressed(KeyCodes.D4))
				{
					// Setup test sim #4
					bullet = setupTestSim4(scene.RootSceneNode);

					// Wait at least 0.25 seconds to perform another action
					timeUntilNextAction = 0.25f;
				}

				// Key '5' is down and it has been sufficient time since the last action
				if (input.IsKeyPressed(KeyCodes.D5))
				{
					// Setup test sim #5
					bullet = setupTestSim5(scene.RootSceneNode);

					// Wait at least 0.25 seconds to perform another action
					timeUntilNextAction = 0.25f;
				}

				// Key '6' is down and it has been sufficient time since the last action
				if (input.IsKeyPressed(KeyCodes.D6))
				{
					// Setup test sim #6
					bullet = setupTestSim6(scene.RootSceneNode);

					// Wait at least 0.25 seconds to perform another action
					timeUntilNextAction = 0.25f;
				}

				// Key '7' is down and it has been sufficient time since the last action
				if (input.IsKeyPressed(KeyCodes.D7))
				{
					// Setup test sim #7
					bullet = setupTestSim7(scene.RootSceneNode);

					// Wait at least 0.25 seconds to perform another action
					timeUntilNextAction = 0.25f;
				}

				// Key '8' is down and it has been sufficient time since the last action
				if (input.IsKeyPressed(KeyCodes.D8))
				{
					// Setup test sim #8
					bullet = setupTestSim8(scene.RootSceneNode);

					// Wait at least 0.25 seconds to perform another action
					timeUntilNextAction = 0.25f;
				}

				// Key '9' is down and it has been sufficient time since the last action
				if (input.IsKeyPressed(KeyCodes.D9))
				{
					// Setup test sim #9
					bullet = setupTestSim9(scene.RootSceneNode);

					// Wait at least 0.25 seconds to perform another action
					timeUntilNextAction = 0.25f;
				}

				// Key '0' is down and it has been sufficient time since the last action
				if (input.IsKeyPressed(KeyCodes.D0))
				{
					// Setup test sim #6
					bullet = setupTestSim0(scene.RootSceneNode);

					// Wait at least 0.25 seconds to perform another action
					timeUntilNextAction = 0.25f;
				}
			}

			return true;
		}

		protected override void OnFrameEnded(object source, FrameEventArgs e)
		{
			updateStats();
			base.OnFrameEnded (source, e);
		}

		private void updateStats()
		{
			GuiElement element = GuiManager.Instance.GetElement("AxiomPhysics/StatsPanel/Stat1");
			element.Text = "Mem. Usage: ";

			element = GuiManager.Instance.GetElement("AxiomPhysics/StatsPanel/Stat2");
			//element.Text = "";

			element = GuiManager.Instance.GetElement("AxiomPhysics/StatsPanel/Stat3");
			element.Text = "";

			element = GuiManager.Instance.GetElement("AxiomPhysics/StatsPanel/Stat4");
			element.Text = "Simulation Speed: " + physicsSpeed.ToString() + " (F3/F4)";

			element = GuiManager.Instance.GetElement("AxiomPhysics/StatsPanel/Stat5");
			element.Text = "Gravity: " + world.Gravity.y.ToString() + " (F6/F7)";

			element = GuiManager.Instance.GetElement("AxiomPhysics/StatsPanel/Stat6");
			element.Text = "Bullet Mass: " + bullet.RigidBody.Mass.ToString() + " (F1/F2)";

			element = GuiManager.Instance.GetElement("AxiomPhysics/StatsPanel/Stat7");
			element.Text = "Static Objects: " + world.GetStaticObjectCount();

			element = GuiManager.Instance.GetElement("AxiomPhysics/StatsPanel/Stat8");
			element.Text = "Dynamic Objects: " + world.GetDynamicObjectCount();

			element = GuiManager.Instance.GetElement("AxiomPhysics/StatsPanel/Stat9");
			element.Text = "Joints: " + world.GetJointCount();

			element = GuiManager.Instance.GetElement("AxiomPhysics/StatsPanel/Stat10");
			element.Text = "";
		}

	}
}