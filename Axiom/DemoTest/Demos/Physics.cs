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
using System.Windows.Forms;
using Axiom.Core;
using Axiom.Enumerations;
using Axiom.MathLib;
using Axiom.Physics;
using Axiom.Scripting;
using Axiom.Utility;

namespace Demos
{
	/// <summary>
	/// 	Summary description for Physics.
	/// </summary>
	public class Physics : TechDemo
	{
		#region Member variables
		
		private IWorld world;
		private GameObject plasma;
		private GameObject plasma2;

		#endregion
		
		#region Constructors
		
		public Physics()
		{
			//
			// TODO: Add constructor logic here
			//
		}
		
		#endregion
		
		#region Methods

		protected override void CreateScene()
		{
			InitDynamics();

			// create a plane for the plane mesh
			Plane p = new Plane();
			p.Normal = Vector3.UnitY;
			p.D = 0;

			// create a plane mesh
			MeshManager.Instance.CreatePlane("GrassPlane", p, 20000, 20000, 50, 50, true, 2, 50, 50, Vector3.UnitZ);

			// create an entity to reference this mesh
			Entity planeEnt = sceneMgr.CreateEntity("Floor", "GrassPlane");
			planeEnt.MaterialName = "Example.GrassyPlane";
			((SceneNode)sceneMgr.RootSceneNode.CreateChild()).Objects.Add(planeEnt);

			// set ambient light to white
			sceneMgr.TargetRenderSystem.LightingEnabled = true;
			sceneMgr.AmbientLight = ColorEx.FromColor(System.Drawing.Color.Gray);

			plasma = new PlasmaGun(sceneMgr);
			plasma.Position = new Vector3(0, 400, 200);

			plasma2 = new PlasmaGun(sceneMgr);
			plasma2.Position = new Vector3(0, 100, 200);

			// HACK: Decouple this and register objects with the World
			plasma.RigidBody = world.CreateBody(plasma, DynamicsBodyType.Box, 0.0f);
			plasma2.RigidBody = world.CreateBody(plasma2, DynamicsBodyType.Box, 0.0f);

			// setup the skybox
			sceneMgr.SetSkyBox(true, "Skybox.CloudyHills", 2000.0f);
		}

		protected override bool OnFrameStarted(object source, FrameEventArgs e)
		{
			base.OnFrameStarted (source, e);

			float force = 30.0f;

			if(inputReader.IsKeyPressed(Keys.L))
				plasma.RigidBody.AddForce(force, 0, 0);
			if(inputReader.IsKeyPressed(Keys.J))
				plasma.RigidBody.AddForce(-force, 0, 0);
			if(inputReader.IsKeyPressed(Keys.I))
				plasma.RigidBody.AddForce(0, 0, -force);
			if(inputReader.IsKeyPressed(Keys.K))
				plasma.RigidBody.AddForce(0, 0, force);

			UpdateDynamics(3 * e.TimeSinceLastFrame);

			plasma.UpdateFromDynamics();
			plasma2.UpdateFromDynamics();

			return true;
		}

		private void InitDynamics()
		{
			// TODO: Make the dynamics system request from the engine, no longer a singleton
			// create a new world
			//world = DynamicsSystem.Instance.CreateWorld();

			world.Gravity = new Vector3(0, -9.81f, 0);
		}

		private void UpdateDynamics(float time)
		{
			world.Step(time);
		}
		
		#endregion
		
		#region Properties
		
		#endregion

	}
}
